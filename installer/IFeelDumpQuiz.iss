#define MyAppName "IFeelDump Quiz"
#define MyAppVersion Trim(FileRead("VERSION"))
#define MyAppPublisher "DieListe01"
#define MyAppExeName "IFeelDumpQuiz.exe"

[Setup]
AppId={{8DBEF7FB-13C0-4E85-9C2E-68F62C702A89}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
VersionInfoVersion={#MyAppVersion}
DefaultDirName={localappdata}\Programs\IFeelDump Quiz
DefaultGroupName=IFeelDump Quiz
UninstallDisplayIcon={app}\{#MyAppExeName}
AppPublisherURL=https://github.com/DieListe01/IFeelDumpQuiz
AppSupportURL=https://github.com/DieListe01/IFeelDumpQuiz/issues
AppUpdatesURL=https://github.com/DieListe01/IFeelDumpQuiz/releases
Compression=lzma
SolidCompression=yes
WizardStyle=modern
OutputDir=dist\installer
OutputBaseFilename=IFeelDumpQuiz-Setup
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
CloseApplications=yes
CloseApplicationsFilter={#MyAppExeName}
RestartApplications=no
SetupLogging=yes
DisableProgramGroupPage=yes
AppVerName={#MyAppName} {#MyAppVersion}
LicenseFile=docs\LICENSE_INSTALLER.txt

[Files]
Source: "dist\windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Tasks]
Name: "desktopicon"; Description: "Desktop-Verknuepfung erstellen"; GroupDescription: "Zusaetzliche Symbole:"

[Icons]
Name: "{group}\IFeelDump Quiz"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\IFeelDump Quiz"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "IFeelDump Quiz starten"; Flags: nowait postinstall skipifsilent
