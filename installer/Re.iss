#define MyAppName "Re"
#define MyAppVersion "1.1.0"
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
PrivilegesRequired=admin
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
Source: "..\artifacts\publish-v1.1.0\Re\*"; DestDir: "{app}"; Excludes: "*.pdb,*.xml,logs\*"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\SECURITY.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Re"; Filename: "{app}\Re.exe"
Name: "{autodesktop}\Re"; Filename: "{app}\Re.exe"; Tasks: desktopicon

[Run]
Filename: "{sys}\sc.exe"; Parameters: "stop Re.Api"; Flags: runhidden waituntilterminated ignoreerrors
Filename: "{sys}\sc.exe"; Parameters: "delete Re.Api"; Flags: runhidden waituntilterminated ignoreerrors
Filename: "{sys}\sc.exe"; Parameters: "create Re.Api binPath= ""{quote}{app}\Api\Re.Api.exe{quote}"" start= delayed-auto DisplayName= ""Re ERP Local API"" depend= Tcpip"; Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "description Re.Api ""Re ERP secure local application service"""; Flags: runhidden waituntilterminated ignoreerrors
Filename: "{sys}\sc.exe"; Parameters: "failure Re.Api reset= 86400 actions= restart/5000/restart/15000/restart/60000"; Flags: runhidden waituntilterminated ignoreerrors
Filename: "{sys}\sc.exe"; Parameters: "failureflag Re.Api 1"; Flags: runhidden waituntilterminated ignoreerrors
Filename: "{sys}\sc.exe"; Parameters: "start Re.Api"; Flags: runhidden waituntilterminated
Filename: "{app}\Re.exe"; Description: "{cm:LaunchProgram,Re}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\sc.exe"; Parameters: "stop Re.Api"; Flags: runhidden waituntilterminated ignoreerrors
Filename: "{sys}\sc.exe"; Parameters: "delete Re.Api"; Flags: runhidden waituntilterminated ignoreerrors

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
  OldDatabase: String;
  ServiceDatabase: String;
begin
  { Stop/remove an older development or installed service before replacing files. }
  Exec(ExpandConstant('{sys}\sc.exe'), 'stop Re.Api', '', SW_HIDE,
    ewWaitUntilTerminated, ResultCode);
  Exec(ExpandConstant('{sys}\sc.exe'), 'delete Re.Api', '', SW_HIDE,
    ewWaitUntilTerminated, ResultCode);
  Sleep(1000);

  { Preserve an existing per-user SQLite database on the first service upgrade. }
  OldDatabase := ExpandConstant('{localappdata}\ReSoft\Re\Data\Re.db');
  ServiceDatabase := ExpandConstant('{commonappdata}\ReSoft\Re\Data\Re.db');
  if FileExists(OldDatabase) and not FileExists(ServiceDatabase) then
  begin
    ForceDirectories(ExtractFileDir(ServiceDatabase));
    FileCopy(OldDatabase, ServiceDatabase, False);
  end;
  Result := '';
end;
