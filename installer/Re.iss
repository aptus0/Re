#define MyAppName "Re"
#define MyAppVersion "1.0.1"
#define MyAppPublisher "ReSoft"
#define MyAppExeName "Re.exe"

[Setup]
AppId={{A64141BC-9918-49DC-BCE5-51F1BF37E51F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\ReSoft\Re
DefaultGroupName=ReSoft
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=..\artifacts\installer
OutputBaseFilename=Re-Setup-Windows-x64
SetupIconFile=..\Re_ERP_Logo.ico
UninstallDisplayIcon={app}\Re.exe
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
LicenseFile=..\LICENSE
CloseApplications=yes
RestartApplications=no
VersionInfoVersion={#MyAppVersion}.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=ReSoft Re Windows Installer
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\artifacts\publish-v1.0.1\Re\*"; DestDir: "{app}"; Excludes: "*.pdb,*.xml,logs\*"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\SECURITY.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Re"; Filename: "{app}\Re.exe"
Name: "{autodesktop}\Re"; Filename: "{app}\Re.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Re.exe"; Description: "{cm:LaunchProgram,Re}"; Flags: nowait postinstall skipifsilent
