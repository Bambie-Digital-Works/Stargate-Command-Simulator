using FacilityCommand.Application.Configuration;
using FacilityCommand.Application.Logging;
using FacilityCommand.Application.Operations;
using FacilityCommand.Application.Personnel;
using FacilityCommand.Application.Security;
using FacilityCommand.Application.Survey;
using FacilityCommand.Application.Transit;
using FacilityCommand.Core.Destinations;
using FacilityCommand.Core.Security;
using FacilityCommand.Core.Survey;
using FacilityCommand.Core.Transit;
using FacilityCommand.Infrastructure.Configuration;
using FacilityCommand.Infrastructure.Content;
using FacilityCommand.Infrastructure.Diagnostics;
using FacilityCommand.Infrastructure.Input;
using FacilityCommand.Infrastructure.Logging;
using FacilityCommand.Infrastructure.Simulation;
using FacilityCommand.Presentation;
using Godot;

namespace FacilityCommand.Bootstrap;

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

            InputBindingStore inputBindingStore = new(ProjectSettings.GlobalizePath("user://input_bindings.v1.json"));
            GodotInputBindingService inputBindings = new(inputBindingStore, logger);
            inputBindings.Initialize();

            string destinationsJson = Godot.FileAccess.GetFileAsString("res://content/destinations.v1.json");
            DestinationRegistry destinations = new DestinationRegistryLoader().Load(destinationsJson);
            string surveyJson = Godot.FileAccess.GetFileAsString("res://content/survey_profiles.v1.json");
            SurveyTelemetryCatalog surveyCatalog = new SurveyTelemetryCatalogLoader().Load(surveyJson);
            FacilityResourcePool resources = new(
                configuration.Simulation.AvailablePowerUnits,
                configuration.Simulation.AvailableCoolingUnits);
            OutgoingConnection outgoing = new(destinations, resources);
            TransitSimulationService transit = new(outgoing, destinations, resources);
            ReturnSecurityService security = new(new ReturnCredentialVerifier(), new ContainmentShutter());
            ManualSimulationClock clock = new();
            ExpeditionRosterService roster = ExpeditionRosterService.CreateDefault(clock);
            SurveyTelemetryService survey = new(new SurveyDrone(), surveyCatalog, roster, transit, clock);
            roster.BindSurvey(survey);
            OperationsBoardService operations = new(transit, security, roster, survey, clock, resources);

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
            shell.Initialize(metadata, configuration, logger, inputBindings, operations);

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
