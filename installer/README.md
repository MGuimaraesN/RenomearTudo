# Instalador

O instalador do Renomear Tudo é gerado com Inno Setup pelo workflow manual do GitHub Actions.

O Setup final contém o **Microsoft .NET Framework 4.8 Runtime Offline** oficial da Microsoft. Se o computador já tiver .NET Framework 4.8 ou superior, o pré-requisito é ignorado. Caso contrário, ele é instalado antes dos arquivos do aplicativo.

Compatibilidade pretendida:

- Windows 7 SP1 (x86/x64)
- Windows 10 (x86/x64)
- Windows 11 (x64; o aplicativo gerenciado também pode funcionar sob as camadas de compatibilidade do Windows em outras arquiteturas)

O Windows 7 está fora de suporte da Microsoft; a compatibilidade é mantida em modo best effort.

O arquivo `prerequisites/NDP48-x86-x64-AllOS-ENU.exe` não é versionado no repositório. O GitHub Actions baixa o instalador offline diretamente da Microsoft, valida a assinatura Authenticode e o incorpora no Setup.

Antes da publicação, o workflow instala silenciosamente o Setup gerado em uma pasta temporária e executa `RenomearTudo.exe --startup-check`. Uma Release só é publicada se o executável instalado abrir e inicializar WPF/XAML corretamente.
