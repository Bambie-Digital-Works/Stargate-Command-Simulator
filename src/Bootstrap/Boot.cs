using Godot;
using WormholeWorlds.Application.Accessibility;
using WormholeWorlds.Application.Configuration;
using WormholeWorlds.Application.Incidents;
using WormholeWorlds.Application.Logging;
using WormholeWorlds.Application.Operations;
using WormholeWorlds.Application.Personnel;
using WormholeWorlds.Application.Security;
using WormholeWorlds.Application.Shift;
using WormholeWorlds.Application.Survey;
using WormholeWorlds.Application.Systems;
using WormholeWorlds.Application.Transit;
using WormholeWorlds.Core.Destinations;
using WormholeWorlds.Core.Incidents;
using WormholeWorlds.Core.Security;
using WormholeWorlds.Core.Shift;
using WormholeWorlds.Core.Survey;
using WormholeWorlds.Core.Transit;
using WormholeWorlds.Infrastructure.Accessibility;
using WormholeWorlds.Infrastructure.Configuration;
using WormholeWorlds.Infrastructure.Content;
using WormholeWorlds.Infrastructure.Diagnostics;
using WormholeWorlds.Infrastructure.Input;
using WormholeWorlds.Infrastructure.Logging;
using WormholeWorlds.Infrastructure.Persistence;
using WormholeWorlds.Infrastructure.Simulation;
using WormholeWorlds.Infrastructure.Updates;
using WormholeWorlds.Presentation;

namespace WormholeWorlds.Bootstrap;

/// <summary>
/// Composition root for the executable. It may assemble application services and
/// presentation scenes, but domain rules must remain outside this layer.
/// </summary>
public partial class Boot : Node
{
    [Export]
    public PackedScene? OperatorShellScene { get; set; }

    [Export]
    public PackedScene? StartupFailureScene { get; set; }

    public override void _Ready()
    {
        if (OperatorShellScene is null || StartupFailureScene is null)
        {
            GD.PushError("Boot cannot start: required startup scenes are not configured.");
            GetTree().Quit(1);
            return;
        }

        try
        {
            string currentUserDirectory = ProjectSettings.GlobalizePath("user://");
            string legacyUserDirectory = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                "Godot",
                "app_userdata",
                "Stargate Command Simulator");
            UserDataMigrationResult migration = new UserDataMigration().Migrate(
                currentUserDirectory,
                legacyUserDirectory);

            ConfigurationLoader configurationLoader = new();
            string defaultsJson = Godot.FileAccess.GetFileAsString("res://config/defaults.json");
            string? userJson = Godot.FileAccess.FileExists("user://settings.json")
                ? Godot.FileAccess.GetFileAsString("user://settings.json")
                : null;
            AppConfiguration configuration = configurationLoader.Load(defaultsJson, userJson);

            AssemblyBuildMetadataProvider metadataProvider = new();
            var metadata = metadataProvider.Get();
            JsonLinesApplicationLogger logger = new(
                ProjectSettings.GlobalizePath("user://logs"),
                configuration.Logging.MinimumLevel,
                configuration.Logging.MaxFiles,
                configuration.Logging.MaxFileBytes,
                configuration.Diagnostics.RecentLogEntries);

            if (migration.MigratedFiles.Count > 0 || migration.Warnings.Count > 0)
            {
                logger.Log(
                    migration.Warnings.Count == 0 ? ApplicationLogLevel.Information : ApplicationLogLevel.Warning,
                    "application.user-data-migration",
                    migration.Warnings.Count == 0
                        ? $"Imported {migration.MigratedFiles.Count} legacy player data files."
                        : "Some legacy player data files could not be imported.",
                    new Dictionary<string, string>
                    {
                        ["migratedCount"] = migration.MigratedFiles.Count.ToString(),
                        ["warningCount"] = migration.Warnings.Count.ToString(),
                    });
            }

            InputBindingStore inputBindingStore = new(ProjectSettings.GlobalizePath("user://input_bindings.v1.json"));
            GodotInputBindingService inputBindings = new(inputBindingStore, logger);
            inputBindings.Initialize();

            string destinationsJson = Godot.FileAccess.GetFileAsString("res://content/destinations.v1.json");
            DestinationRegistry destinations = new DestinationRegistryLoader().Load(destinationsJson);
            string surveyJson = Godot.FileAccess.GetFileAsString("res://content/survey_profiles.v1.json");
            SurveyTelemetryCatalog surveyCatalog = new SurveyTelemetryCatalogLoader().Load(surveyJson);
            string incidentsJson = Godot.FileAccess.GetFileAsString("res://content/incidents.v1.json");
            IncidentCatalog incidentCatalog = new IncidentCatalogLoader().Load(incidentsJson);
            string briefJson = Godot.FileAccess.GetFileAsString("res://content/shift_brief.v1.json");
            ShiftBriefDefinition shiftBrief = new ShiftBriefLoader().Load(briefJson);
            FacilityResourcePool resources = new(
                configuration.Simulation.AvailablePowerUnits,
                configuration.Simulation.AvailableCoolingUnits);
            OutgoingConnection outgoing = new(destinations, resources);
            TransitSimulationService transit = new(outgoing, destinations, resources);
            ReturnSecurityService security = new(new ReturnCredentialVerifier(), new ContainmentShutter());
            ManualSimulationClock clock = new();
            AccessibilitySettingsStore accessibilityStore = new(
                ProjectSettings.GlobalizePath("user://accessibility_settings.v1.json"));
            AccessibilityLoadResult accessibilityLoad = accessibilityStore.LoadOrDefault();
            AccessibilityPreferences accessibility = accessibilityLoad.Preferences;
            clock.SetTimeScale(accessibility.TimePressureScale);
            System.Net.Http.HttpClient updateHttpClient = new() { Timeout = TimeSpan.FromSeconds(20) };
            GitHubReleaseFeedClient releaseFeed = new(
                updateHttpClient,
                "Bambie-Digital-Works",
                "Stargate-Command-Simulator");
            UpdateCheckCoordinator updateCoordinator = new(
                releaseFeed,
                ProjectSettings.GlobalizePath("user://update_check.v1.json"));
            VerifiedInstallerDownloader installerDownloader = new(updateHttpClient);
            WindowsInstallerLauncher installerLauncher = new();
            ExpeditionRosterService roster = ExpeditionRosterService.CreateDefault(clock);
            SurveyTelemetryService survey = new(new SurveyDrone(), surveyCatalog, roster, transit, clock);
            roster.BindSurvey(survey);
            ShiftIncidentService incidents = new(incidentCatalog, transit, security, roster, survey, resources);
            SystemsBoardService systems = new(resources, incidents);
            OperationsBoardService operations = new(transit, security, roster, survey, incidents, clock, resources, systems);
            CampaignStateStore campaignStore = new(
                ProjectSettings.GlobalizePath("user://campaign_state.json"),
                metadata.SaveSchemaVersion);
            ShiftLifecycleService lifecycle = new(shiftBrief, incidents, roster, resources, campaignStore, operations);
            operations.BindLifecycle(lifecycle);

            if (!string.IsNullOrWhiteSpace(lifecycle.PersistenceGuidance))
            {
                logger.Log(
                    ApplicationLogLevel.Warning,
                    "campaign.save.recovery",
                    lifecycle.PersistenceGuidance,
                    new Dictionary<string, string>
                    {
                        ["saveSchema"] = metadata.SaveSchemaVersion.ToString(),
                    });
            }

            logger.Log(
                ApplicationLogLevel.Information,
                "application.started",
                "Application services initialized.",
                new Dictionary<string, string>
                {
                    ["version"] = metadata.ProductVersion,
                    ["channel"] = metadata.Channel,
                    ["buildId"] = metadata.BuildId,
                });

            Node shellNode = OperatorShellScene.Instantiate();
            if (shellNode is not OperatorShell shell)
            {
                shellNode.QueueFree();
                throw new InvalidOperationException("The configured operator shell has an invalid root type.");
            }

            shell.Name = "OperatorShell";
            AddChild(shell);
            shell.Initialize(
                metadata,
                configuration,
                logger,
                inputBindings,
                operations,
                accessibilityStore,
                accessibility,
                accessibilityLoad.RecoveryWarning,
                updateCoordinator,
                installerDownloader,
                installerLauncher,
                ProjectSettings.GlobalizePath("user://updates"));

            GD.Print("Boot complete: operator shell is ready.");
        }
        catch (ConfigurationException exception)
        {
            ShowStartupFailure(exception.SourceLabel, exception.Errors);
        }
        catch (Exception exception)
        {
            SensitiveDataSanitizer sanitizer = new();
            ShowStartupFailure("startup services", [sanitizer.Sanitize(exception.Message)]);
        }
    }

    private void ShowStartupFailure(string sourceLabel, IReadOnlyList<string> errors)
    {
        GD.PushError($"Startup failed: {sourceLabel}: {string.Join(" ", errors)}");
        Node failureNode = StartupFailureScene!.Instantiate();
        if (failureNode is not StartupFailurePanel failurePanel)
        {
            failureNode.QueueFree();
            GetTree().Quit(1);
            return;
        }

        AddChild(failurePanel);
        failurePanel.Initialize(sourceLabel, errors);
    }
}
