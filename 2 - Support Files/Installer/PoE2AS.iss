#ifndef MyAppVersion
  #define MyAppVersion "3.0.0"
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
; Refresh application/support files on upgrade. Deliberately preserve the
; user's generated LiveSplit Target, run-verification history under directory 3,
; and diagnostics under directory 4.
Type: files; Name: "{app}\1 - User Setup\PoE2RouteSetup.exe"
Type: filesandordirs; Name: "{app}\2 - Support Files"

[Dirs]
Name: "{app}\1 - User Setup\LiveSplit Target"
Name: "{app}\4-README's_and_Diagnostics\Diagnostics"
Name: "{app}\4-README's_and_Diagnostics\Diagnostics\images"

[Files]
Source: "{#StageRoot}\1 - User Setup\PoE2RouteSetup.exe"; DestDir: "{app}\1 - User Setup"; Flags: ignoreversion
Source: "{#StageRoot}\1 - User Setup\PoE2AS-Settings.json"; DestDir: "{app}\1 - User Setup"; Flags: onlyifdoesntexist uninsneveruninstall
Source: "{#StageRoot}\1 - User Setup\SETTINGS-README.txt"; DestDir: "{app}\1 - User Setup"; Flags: ignoreversion
Source: "{#StageRoot}\2 - Support Files\*"; DestDir: "{app}\2 - Support Files"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#StageRoot}\3 - verification files\*"; DestDir: "{app}\3 - verification files"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#StageRoot}\4-README's_and_Diagnostics\*"; DestDir: "{app}\4-README's_and_Diagnostics"; Flags: ignoreversion recursesubdirs createallsubdirs
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

[Code]
var
  AppLanguagePage: TInputOptionWizardPage;
  LanguageSelectionInitialized: Boolean;

procedure InitializeWizard;
begin
  AppLanguagePage := CreateInputOptionPage(
    wpSelectDir,
    'Application Language',
    'Choose the default SetupUI language',
    'This controls the language used when PoE2 Route AutoSplitter starts. You can change it later in Settings.',
    True,
    False);

  { Keep this in the same order as PoE2GameLanguages.All. Both SetupUI and
    Game Language intentionally expose only the current PoE2-supported set. }
  AppLanguagePage.Add('English');
  AppLanguagePage.Add('Français');
  AppLanguagePage.Add('Deutsch');
  AppLanguagePage.Add('Español (España)');
  AppLanguagePage.Add('日本語');
  AppLanguagePage.Add('한국어');
  AppLanguagePage.Add('Português (Brasil)');
  AppLanguagePage.Add('Русский');
  AppLanguagePage.Add('ไทย');
  AppLanguagePage.SelectedValueIndex := 0;
  LanguageSelectionInitialized := False;
end;

function LanguageIndexForCode(Code: String): Integer;
begin
  if Code = 'fr' then Result := 1
  else if Code = 'de' then Result := 2
  else if Code = 'es-ES' then Result := 3
  else if Code = 'ja' then Result := 4
  else if Code = 'ko' then Result := 5
  else if Code = 'pt-BR' then Result := 6
  else if Code = 'ru' then Result := 7
  else if Code = 'th' then Result := 8
  else Result := 0;
end;

function ReadExistingAppLanguageCode: String;
var
  SettingsPath: String;
  RawContents: AnsiString;
  Contents: String;
  Key: String;
  StartPos: Integer;
  EndOffset: Integer;
  Tail: String;
begin
  Result := '';
  SettingsPath := ExpandConstant('{app}\1 - User Setup\PoE2AS-Settings.json');
  if not FileExists(SettingsPath) then exit;
  if not LoadStringFromFile(SettingsPath, RawContents) then exit;
  Contents := String(RawContents);

  Key := '"DefaultLanguage": "';
  StartPos := Pos(Key, Contents);
  if StartPos = 0 then exit;

  StartPos := StartPos + Length(Key);
  Tail := Copy(Contents, StartPos, Length(Contents));
  EndOffset := Pos('"', Tail);
  if EndOffset > 0 then
    Result := Copy(Contents, StartPos, EndOffset - 1);
end;

procedure SelectExistingAppLanguageIfPresent;
var
  ExistingCode: String;
begin
  if LanguageSelectionInitialized then exit;
  ExistingCode := ReadExistingAppLanguageCode;
  if ExistingCode <> '' then
    AppLanguagePage.SelectedValueIndex := LanguageIndexForCode(ExistingCode);
  LanguageSelectionInitialized := True;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = wpSelectDir then
    SelectExistingAppLanguageIfPresent;
end;

function SelectedAppLanguageCode: String;
begin
  case AppLanguagePage.SelectedValueIndex of
    1: Result := 'fr';
    2: Result := 'de';
    3: Result := 'es-ES';
    4: Result := 'ja';
    5: Result := 'ko';
    6: Result := 'pt-BR';
    7: Result := 'ru';
    8: Result := 'th';
  else
    Result := 'en';
  end;
end;

procedure WriteSetupUiDefaultLanguage;
var
  SettingsPath: String;
  RawContents: AnsiString;
  Contents: String;
  Key: String;
  Code: String;
  StartPos: Integer;
  EndOffset: Integer;
  Tail: String;
  DeveloperFalse: String;
  DeveloperTrue: String;
begin
  SettingsPath := ExpandConstant('{app}\1 - User Setup\PoE2AS-Settings.json');
  if not FileExists(SettingsPath) then
    exit;

  if not LoadStringFromFile(SettingsPath, RawContents) then
    exit;
  Contents := String(RawContents);

  Code := SelectedAppLanguageCode;
  Key := '"DefaultLanguage": "';
  StartPos := Pos(Key, Contents);

  if StartPos > 0 then
  begin
    StartPos := StartPos + Length(Key);
    Tail := Copy(Contents, StartPos, Length(Contents));
    EndOffset := Pos('"', Tail);
    if EndOffset > 0 then
    begin
      Delete(Contents, StartPos, EndOffset - 1);
      Insert(Code, Contents, StartPos);
    end;
  end
  else
  begin
    { Preserve existing user settings on upgrade and add only the missing language field. }
    DeveloperFalse := '"DeveloperConsoleDefault": false';
    DeveloperTrue := '"DeveloperConsoleDefault": true';
    if Pos(DeveloperFalse, Contents) > 0 then
      StringChangeEx(Contents, DeveloperFalse, DeveloperFalse + ',' + #13#10 + '    "DefaultLanguage": "' + Code + '"', True)
    else if Pos(DeveloperTrue, Contents) > 0 then
      StringChangeEx(Contents, DeveloperTrue, DeveloperTrue + ',' + #13#10 + '    "DefaultLanguage": "' + Code + '"', True);
  end;

  SaveStringToFile(SettingsPath, AnsiString(Contents), False);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssInstall) and WizardSilent then
    SelectExistingAppLanguageIfPresent;
  if CurStep = ssPostInstall then
    WriteSetupUiDefaultLanguage;
end;

