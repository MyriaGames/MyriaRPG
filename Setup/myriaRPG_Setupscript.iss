; MyriaRPG Inno Setup Script
; Build command: dotnet publish -c Release -p:PublishProfile=FolderProfile
; (self-contained single-file win-x64 - no .NET runtime required on the target machine)
;
; MyAppVersion is normally passed in by release.ps1 via /DMyAppVersion=x.y.z
; so it always matches MyriaRPG.csproj's <Version>. The fallback below is only
; used when compiling this script directly in the Inno Setup IDE.
#ifndef MyAppVersion
  #define MyAppVersion "0.2.0"
#endif

#define MyAppName "Myria RPG (Alpha)"
#define MyAppPublisher "Rhyen"
#define MyAppExeName "MyriaRPG.exe"
#define PublishDir "..\bin\Release\net8.0-windows\publish"

[Setup]
AppId={{371DF85A-2C8C-4A38-8C5F-EA1B0AD06A41}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\MyriaRPG
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=.
OutputBaseFilename=MyriaRPG_Setup
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\Assets\App.ico
WizardImageFile=wizard-images\WizardImage.bmp
WizardSmallImageFile=wizard-images\WizardSmallImage.bmp
InfoBeforeFile=AlphaDisclaimer.txt

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Single wildcard copies everything from publish — including the Data\ subfolder
; with its correct subdirectory structure (Data\common\, Data\locales\, etc.)
; overwritereadonly: without it, Inno Setup silently SKIPS any destination file that's
; marked read-only instead of overwriting it - ignoreversion only affects version-checked
; files (like the exe), it does nothing for that. This is exactly the kind of bug that
; would let the exe update fine on every release while some data JSON quietly never does.
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion overwritereadonly recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\MyriaRPG"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\MyriaRPG"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
