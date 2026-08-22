#define public Dependency_Path_NetCoreCheck "dependencies\"
#define CompileMode "Debug"  
#define Version "0.7.0.0"

#include "CodeDependencies.iss"

[Setup]
AppId=KonoobiVoiceTrigger
AppName=Voice Trigger
AppVersion={#Version}
WizardStyle=modern
DefaultDirName={autopf}\Voice Trigger
DefaultGroupName=Sandcorp
UninstallDisplayIcon={app}\VoiceTrigger.exe
Compression=lzma2
SolidCompression=yes
OutputDir=Setup
OutputBaseFilename=Voice Trigger v{#Version}
SetupIconFile=icon.ico 
LanguageDetectionMethod=none

PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: en; MessagesFile: "compiler:Default.isl"

[Files]
// Source: "VoiceTrigger\bin\{#CompileMode}\net7.0-windows\win-x64\ECPlayer.exe"; DestDir: "{app}"
// Source: "VoiceTrigger\bin\{#CompileMode}\net7.0-windows\win-x64\ffmpeg\x64\*"; DestDir: "{app}\ffmpeg\x64"
// Source: "VoiceTrigger\bin\{#CompileMode}\net7.0-windows\win-x64\ffmpeg\x86\*"; DestDir: "{app}\ffmpeg\x86";
Source: "bin\{#CompileMode}\net9.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs


[Tasks]
Name: startup; Description: "Automatically start on login"; GroupDescription: "{cm:AdditionalIcons}"

[Icons]
Name: "{commonprograms}\Voice Trigger"; Filename: "{app}\VoiceTrigger.exe"; Tasks: startup

[Code]

{ ///////////////////////////////////////////////////////////////////// }
function InitializeSetup: Boolean;
begin
  Dependency_AddDotNet90Desktop;
  Result := true;
end;

{ ///////////////////////////////////////////////////////////////////// }
function GetUninstallString(): String;
var
  sUnInstPath: String;
  sUnInstallString: String;
begin
  sUnInstPath := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#emit SetupSetting("AppId")}_is1');
  sUnInstallString := '';
  if not RegQueryStringValue(HKLM, sUnInstPath, 'UninstallString', sUnInstallString) then
    RegQueryStringValue(HKCU, sUnInstPath, 'UninstallString', sUnInstallString);
  Result := sUnInstallString;
end;


{ ///////////////////////////////////////////////////////////////////// }
function IsUpgrade(): Boolean;
begin
  Result := (GetUninstallString() <> '');
end;


{ ///////////////////////////////////////////////////////////////////// }
function UnInstallOldVersion(): Integer;
var
  sUnInstallString: String;
  iResultCode: Integer;
begin
{ Return Values: }
{ 1 - uninstall string is empty }
{ 2 - error executing the UnInstallString }
{ 3 - successfully executed the UnInstallString }

  { default return value }
  Result := 0;

  { get the uninstall string of the old app }
  sUnInstallString := GetUninstallString();
  if sUnInstallString <> '' then begin
    sUnInstallString := RemoveQuotes(sUnInstallString);
    if Exec(sUnInstallString, '/SILENT /NORESTART /SUPPRESSMSGBOXES','', SW_HIDE, ewWaitUntilTerminated, iResultCode) then
      Result := 3
    else
      Result := 2;
  end else
    Result := 1;
end;

{ ///////////////////////////////////////////////////////////////////// }
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep=ssInstall) then
  begin
    if (IsUpgrade()) then
    begin
      UnInstallOldVersion();
    end;
  end;
end;
