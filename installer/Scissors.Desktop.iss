; CODEX-GENERATED: the contents of this file were fully constructed by a Codex agent and not a human.

#ifndef AppVersion
  #define AppVersion "0.0.0-dev"
#endif

#define AppName "Scissors"
#define AppPublisher "Scissors"
#define AppExeName "Scissors.Desktop.exe"
#define ProjectRoot ".."
#define PublishDir ProjectRoot + "\artifacts\desktop-publish"
#define InstallerOutputDir ProjectRoot + "\artifacts\desktop-installer"
#define AppIcon ProjectRoot + "\Scissors.Desktop\Assets\avalonia-logo.ico"

[Setup]
AppId={{B7D9F7E2-0BB9-4E3B-9B2A-9EBA5A5F3B0A}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
OutputDir={#InstallerOutputDir}
OutputBaseFilename=Scissors-Desktop-Setup-{#AppVersion}
SetupIconFile={#AppIcon}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#AppExeName}
ChangesAssociations=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent
