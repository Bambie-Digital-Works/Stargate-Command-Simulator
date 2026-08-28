#ifndef AppSource
  #error AppSource must point to the exported game directory.
#endif
#ifndef OutputDir
  #error OutputDir must point to the release output directory.
#endif
#ifndef ProductVersion
  #define ProductVersion "0.8.0-beta.1"
#endif
#ifndef NumericVersion
  #define NumericVersion "0.8.0.1"
#endif
#ifndef InstallerBaseName
  #define InstallerBaseName "Wormhole-Worlds-Simulator-0.8.0-beta.1-Windows-x64-Setup"
#endif

[Setup]
AppId={{999D88AD-B8FD-4441-AEDD-0F033B22F5C4}
AppName=Wormhole Worlds
AppVersion={#NumericVersion}
AppVerName=Wormhole Worlds {#ProductVersion}
AppPublisher=Bambie Digital Works
AppPublisherURL=https://github.com/Bambie-Digital-Works/Wormhole-Worlds
AppSupportURL=https://github.com/Bambie-Digital-Works/Wormhole-Worlds/issues
AppUpdatesURL=https://github.com/Bambie-Digital-Works/Wormhole-Worlds/releases
DefaultDirName={localappdata}\Programs\Wormhole Worlds
DefaultGroupName=Wormhole Worlds
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
CloseApplicationsFilter=WormholeWorldsSimulator.exe
RestartApplications=no
AppMutex=BambieDigitalWorks.WormholeWorldsSimulator
OutputDir={#OutputDir}
OutputBaseFilename={#InstallerBaseName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
SetupLogging=yes
UninstallDisplayIcon={app}\WormholeWorldsSimulator.exe
VersionInfoCompany=Bambie Digital Works
VersionInfoDescription=Wormhole Worlds installer
VersionInfoProductName=Wormhole Worlds
VersionInfoProductVersion={#NumericVersion}
VersionInfoVersion={#NumericVersion}
#ifdef SignToolName
SignTool={#SignToolName}
SignedUninstaller=yes
#endif

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#AppSource}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Wormhole Worlds"; Filename: "{app}\WormholeWorldsSimulator.exe"
Name: "{autodesktop}\Wormhole Worlds"; Filename: "{app}\WormholeWorldsSimulator.exe"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Bambie Digital Works\Wormhole Worlds"; ValueType: string; ValueName: "SemanticVersion"; ValueData: "{#ProductVersion}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Bambie Digital Works\Wormhole Worlds"; ValueType: string; ValueName: "NumericVersion"; ValueData: "{#NumericVersion}"; Flags: uninsdeletekey

[Run]
Filename: "{app}\WormholeWorldsSimulator.exe"; Description: "Launch Wormhole Worlds"; Flags: nowait postinstall skipifsilent

[Code]
function NextVersionPart(var Value: String): Integer;
var
  Separator: Integer;
  Part: String;
begin
  Separator := Pos('.', Value);
  if Separator = 0 then
  begin
    Part := Value;
    Value := '';
  end
  else
  begin
    Part := Copy(Value, 1, Separator - 1);
    Delete(Value, 1, Separator);
  end;
  Result := StrToIntDef(Part, 0);
end;

function CompareNumericVersions(Left, Right: String): Integer;
var
  Index: Integer;
  LeftPart: Integer;
  RightPart: Integer;
begin
  Result := 0;
  for Index := 1 to 4 do
  begin
    LeftPart := NextVersionPart(Left);
    RightPart := NextVersionPart(Right);
    if LeftPart < RightPart then
    begin
      Result := -1;
      Exit;
    end;
    if LeftPart > RightPart then
    begin
      Result := 1;
      Exit;
    end;
  end;
end;

function InitializeSetup(): Boolean;
var
  InstalledVersion: String;
  Comparison: Integer;
begin
  Result := True;
  if not RegQueryStringValue(
    HKCU,
    'Software\Bambie Digital Works\Wormhole Worlds',
    'NumericVersion',
    InstalledVersion) then
    Exit;

  Comparison := CompareNumericVersions(InstalledVersion, '{#NumericVersion}');
  if Comparison > 0 then
  begin
    MsgBox(
      'A newer version of Wormhole Worlds is already installed. This installer will not downgrade it.',
      mbError,
      MB_OK);
    Result := False;
  end
  else if (Comparison = 0) and (not WizardSilent) then
    Result := MsgBox(
      'This version is already installed. Repair or reinstall it now?',
      mbConfirmation,
      MB_YESNO) = IDYES
  else if (Comparison < 0) and (not WizardSilent) then
    Result := MsgBox(
      'An older version is installed. Update it now? Player saves and settings will be preserved.',
      mbConfirmation,
      MB_YESNO) = IDYES;
end;
