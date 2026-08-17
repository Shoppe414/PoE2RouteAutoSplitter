#ifndef MyAppVersion
  #define MyAppVersion "2.2.1"
#endif
#ifndef StageRoot
  #error StageRoot must point to the assembled portable package root.
#endif
#ifndef InstallerOutputDir
  #define InstallerOutputDir "Output"
#endif
#ifndef VcRedistPath
  #error VcRedistPath must point to vc_redist.x64.exe.
#endif

#define MyAppName "PoE2 Route AutoSplitter"
#define MyAppPublisher "PoE2 Route AutoSplitter"
#define MyAppExeName "PoE2RouteSetup.exe"
#define MyAppId "{{E370EB7D-5B92-48D6-B132-88DB38B19380}"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} v{#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\PoE2RouteAutoSplitter
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir={#InstallerOutputDir}
OutputBaseFilename=PoE2AS-v{#MyAppVersion}-Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\1 - User Setup\{#MyAppExeName}
CloseApplications=yes
RestartApplications=no
UsePreviousAppDir=yes
VersionInfoVersion={#MyAppVersion}.0
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[InstallDelete]
; Refresh application/support files on upgrade, but deliberately preserve the
; user's generated LiveSplit Target directory.
Type: files; Name: "{app}\1 - User Setup\PoE2RouteSetup.exe"
Type: filesandordirs; Name: "{app}\2 - Support Files"

[Dirs]
Name: "{app}\1 - User Setup\LiveSplit Target"

[Files]
Source: "{#StageRoot}\1 - User Setup\PoE2RouteSetup.exe"; DestDir: "{app}\1 - User Setup"; Flags: ignoreversion
Source: "{#StageRoot}\2 - Support Files\*"; DestDir: "{app}\2 - Support Files"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#VcRedistPath}"; DestDir: "{tmp}"; DestName: "vc_redist.x64.exe"; Flags: deleteafterinstall

[Icons]
Name: "{group}\PoE2 Route AutoSplitter"; Filename: "{app}\1 - User Setup\{#MyAppExeName}"; WorkingDir: "{app}\1 - User Setup"
Name: "{userdesktop}\PoE2 Route AutoSplitter"; Filename: "{app}\1 - User Setup\{#MyAppExeName}"; WorkingDir: "{app}\1 - User Setup"; Tasks: desktopicon

[Run]
; TesseractOCR's native components require the Microsoft VC++ x64 runtime.
; Running the redistributable is safe when the same/newer runtime is present.
Filename: "{tmp}\vc_redist.x64.exe"; Parameters: "/install /quiet /norestart"; StatusMsg: "Installing Microsoft Visual C++ runtime..."; Flags: waituntilterminated runhidden
Filename: "{app}\1 - User Setup\{#MyAppExeName}"; Description: "Launch PoE2 Route AutoSplitter"; WorkingDir: "{app}\1 - User Setup"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; The installer intentionally leaves LiveSplit Target contents to the user.
Type: files; Name: "{app}\1 - User Setup\PoE2RouteSetup-crash.log"
