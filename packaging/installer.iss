#ifndef AppVersion
 #define AppVersion "1.7.4"
#endif
#ifndef TargetArchitecture
 #define TargetArchitecture "x64"
#endif
#ifndef BuildRoot
 #error BuildRoot must point to the validated self-contained publication folder.
#endif
[Setup]
AppId={{1E53A5FD-8245-4EB8-9689-94653316E765}
AppName=Windows YouTube Widget
AppVersion={#AppVersion}
AppPublisher=Showmetech
AppPublisherURL=https://showmetech.github.io/windows-youtube-widget/
AppSupportURL=https://github.com/showmetech/windows-youtube-widget/issues
DefaultDirName={localappdata}\Programs\WindowsYouTubeWidget
DefaultGroupName=Windows YouTube Widget
PrivilegesRequired=lowest
MinVersion=10.0.19041
#if TargetArchitecture == "arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#elif TargetArchitecture == "x64"
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#else
ArchitecturesAllowed=x86compatible
#endif
OutputDir=..\artifacts
OutputBaseFilename=Windows-YouTube-Widget-{#AppVersion}-{#TargetArchitecture}-Setup
Compression=lzma2/fast
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\WindowsYouTubeWidget.exe
[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
[Files]
Source: "{#BuildRoot}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb,tests.json,*verification.json,*.log,TestProfile\*,SuiteTestProfile\*,.widget-activation-state.json,bridge-*.json"
[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "WindowsYouTubeWidget"; ValueType: string; ValueData: """{app}\WindowsYouTubeWidget.exe"" --startup"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Google\Chrome\NativeMessagingHosts\com.bruno.youtube_widget"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Microsoft\Edge\NativeMessagingHosts\com.bruno.youtube_widget"; Flags: uninsdeletekey
[Icons]
Name: "{group}\YouTube Widget"; Filename: "{app}\WindowsYouTubeWidget.exe"; IconFilename: "{app}\Assets\App-{#AppVersion}.ico"
Name: "{autodesktop}\YouTube Widget"; Filename: "{app}\WindowsYouTubeWidget.exe"; IconFilename: "{app}\Assets\App-{#AppVersion}.ico"
[Run]
Filename: "{app}\WindowsYouTubeWidget.exe"; Description: "Abrir YouTube Widget"; Flags: nowait postinstall skipifsilent
[UninstallDelete]
Type: files; Name: "{app}\native-host.json"
Type: files; Name: "{localappdata}\WindowsYouTubeWidget\NativeMessaging\native-host.json"
[Code]
procedure DisableManifest(Path: String);
begin
 if FileExists(Path) then begin
  if FileExists(Path + '.update-backup') then DeleteFile(Path + '.update-backup');
  if not RenameFile(Path, Path + '.update-backup') then RaiseException('Feche a conexão do widget antes de atualizar.');
 end;
end;
procedure RestoreManifest(Path: String);
begin
 if FileExists(Path + '.update-backup') then begin
  if FileExists(Path) then DeleteFile(Path + '.update-backup')
  else RenameFile(Path + '.update-backup', Path);
 end;
end;
function PrepareToInstall(var NeedsRestart: Boolean): String;
var Code: Integer;
begin
 Result := '';
 DisableManifest(ExpandConstant('{app}\native-host.json'));
 DisableManifest(ExpandConstant('{localappdata}\WindowsYouTubeWidget\NativeMessaging\native-host.json'));
 if FileExists(ExpandConstant('{app}\WindowsYouTubeWidget.exe')) then begin
  Exec(ExpandConstant('{app}\WindowsYouTubeWidget.exe'), '--exit', '', SW_HIDE, ewWaitUntilTerminated, Code);
  Sleep(5500);
 end;
end;
procedure CurStepChanged(CurStep: TSetupStep);
var Code: Integer;
begin
 if CurStep = ssPostInstall then begin
  if not Exec(ExpandConstant('{app}\WindowsYouTubeWidget.exe'), '--register-native-host', '', SW_HIDE, ewWaitUntilTerminated, Code) or (Code <> 0) then
   RaiseException('Não foi possível registrar a conexão local com o navegador.');
 end;
end;
procedure DeinitializeSetup();
begin
 RestoreManifest(ExpandConstant('{app}\native-host.json'));
 RestoreManifest(ExpandConstant('{localappdata}\WindowsYouTubeWidget\NativeMessaging\native-host.json'));
end;
