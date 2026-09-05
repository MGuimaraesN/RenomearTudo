#ifndef MyAppVersion
  #define MyAppVersion "2.2.0"
#endif

#define MyAppName "Renomear Tudo"
#define MyAppExeName "RenomearTudo.exe"
#define DotNetInstaller "NDP48-x86-x64-AllOS-ENU.exe"

[Setup]
AppId={{9B8D31BA-21C5-4A41-95CF-4D3BCA9F30B0}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
VersionInfoVersion={#MyAppVersion}
VersionInfoDescription={#MyAppName} Installer
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
DefaultDirName={autopf}\Renomear Tudo
DefaultGroupName=Renomear Tudo
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=RenomearTudo-Setup-{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
MinVersion=6.1sp1
PrivilegesRequired=admin
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupLogging=yes
CloseApplications=yes
RestartApplications=no
AllowNoIcons=yes

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na Área de Trabalho"; GroupDescription: "Atalhos:"; Flags: unchecked

[Files]
; O runtime oficial da Microsoft é baixado pelo GitHub Actions durante o build e
; fica EMBUTIDO no Setup final. Ele vem primeiro para extração rápida com solid compression.
Source: "prerequisites\{#DotNetInstaller}"; Flags: dontcopy
Source: "..\src\RenomearTudo.App\bin\Release\net48\RenomearTudo.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\src\RenomearTudo.App\bin\Release\net48\RenomearTudo.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\src\RenomearTudo.App\bin\Release\net48\RenomearTudo.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\NOTICE.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE-ORIGINAL.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Renomear Tudo"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\Renomear Tudo"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Code]
const
  DotNet48Release = 528040;

function GetDotNetRelease(var Release: Cardinal): Boolean;
var
  Value32, Value64: Cardinal;
  Found32, Found64: Boolean;
begin
  Value32 := 0;
  Value64 := 0;
  Found32 := RegQueryDWordValue(
    HKLM32,
    'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full',
    'Release',
    Value32);

  Found64 := False;
  if IsWin64 then
    Found64 := RegQueryDWordValue(
      HKLM64,
      'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full',
      'Release',
      Value64);

  if Found64 and (Value64 > Value32) then
    Release := Value64
  else
    Release := Value32;

  Result := Found32 or Found64;
end;

function IsDotNet48OrHigher(): Boolean;
var
  Release: Cardinal;
begin
  Result := GetDotNetRelease(Release) and (Release >= DotNet48Release);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
  InstallerPath: String;
begin
  Result := '';

  if IsDotNet48OrHigher() then
  begin
    Log('.NET Framework 4.8 ou superior já está instalado.');
    Exit;
  end;

  WizardForm.StatusLabel.Caption := 'Preparando Microsoft .NET Framework 4.8...';
  ExtractTemporaryFile('{#DotNetInstaller}');
  InstallerPath := ExpandConstant('{tmp}\{#DotNetInstaller}');

  Log('Instalando pré-requisito: ' + InstallerPath);
  if not Exec(
    InstallerPath,
    '/q /norestart',
    '',
    SW_SHOW,
    ewWaitUntilTerminated,
    ResultCode) then
  begin
    Result := 'Não foi possível iniciar o instalador do Microsoft .NET Framework 4.8.';
    Exit;
  end;

  Log('Instalador do .NET Framework retornou: ' + IntToStr(ResultCode));

  if (ResultCode = 3010) or (ResultCode = 1641) then
  begin
    NeedsRestart := True;
    Result := 'O Microsoft .NET Framework 4.8 foi instalado e o Windows precisa ser reiniciado. ' +
      'Após reiniciar, execute este instalador novamente para concluir a instalação do Renomear Tudo.';
    Exit;
  end;

  if ResultCode <> 0 then
  begin
    Result := 'A instalação do Microsoft .NET Framework 4.8 falhou com o código ' +
      IntToStr(ResultCode) + '. O Renomear Tudo não foi instalado.';
    Exit;
  end;

  if not IsDotNet48OrHigher() then
  begin
    NeedsRestart := True;
    Result := 'O Microsoft .NET Framework 4.8 não foi detectado após a instalação. ' +
      'Reinicie o Windows e execute este instalador novamente.';
  end;
end;

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir o Renomear Tudo"; Flags: nowait postinstall skipifsilent
