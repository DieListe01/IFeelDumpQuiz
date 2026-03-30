#ifndef MyAppName
  #define MyAppName "IFeelDump Quiz"
#endif

#ifndef MyAppVersion
  #define MyAppVersion "0.0.1"
#endif

#ifndef MyBuildNumber
  #define MyBuildNumber ""
#endif

#ifndef MyAppPublisher
  #define MyAppPublisher "DieListe01"
#endif

#ifndef MyAppExeName
  #define MyAppExeName "IFeelDumpQuiz.exe"
#endif

[Setup]
AppId={{8DBEF7FB-13C0-4E85-9C2E-68F62C700001}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
VersionInfoVersion={#MyAppVersion}
DefaultDirName={localappdata}\Programs\IFeelDump Quiz
DefaultGroupName=IFeelDump Quiz
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir=dist\installer
OutputBaseFilename=IFeelDump-Setup-{#MyAppVersion}
LicenseFile=docs\LICENSE_INSTALLER.txt
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
CloseApplications=yes
CloseApplicationsFilter={#MyAppExeName}
RestartApplications=no
SetupLogging=yes
DisableProgramGroupPage=yes
AppVerName={#MyAppName} {#MyAppVersion}
PublisherURL=https://github.com/DieListe01
AppSupportURL=https://github.com/DieListe01/IFeelDumpQuiz
AppUpdatesURL=https://github.com/DieListe01/IFeelDumpQuiz/releases

[Files]
Source: "dist\windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "VERSION"; DestDir: "{app}"
Source: "BUILD"; DestDir: "{app}"; Flags: skipifsourcedoesntexist

[Tasks]
Name: "desktopicon"; Description: "Desktop-Verknuepfung erstellen"; GroupDescription: "Zusaetzliche Aufgaben:"

[Icons]
Name: "{group}\IFeelDump Quiz"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\IFeelDump Quiz"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "IFeelDump Quiz starten"; Flags: nowait postinstall skipifsilent
