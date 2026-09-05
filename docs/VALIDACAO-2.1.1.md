# Validação 2.1.1 — correção de bindings e gate de UI

## Erro reproduzido pelos logs

O build 2.1.0 concluiu com `0 Warning(s)` e `0 Error(s)`, e os smoke tests do Core passaram. A falha ocorreu no startup-check do WPF ao materializar a área de detalhes:

`A TwoWay or OneWayToSource binding cannot work on the read-only property 'Size' of type 'RenomearTudo.App.Models.BindableFileItem'.`

A causa era um `Run.Text` usando o modo padrão de binding sobre propriedades somente leitura.

## Correções

- `SelectedFile.Size`, `SelectedFile.Modified`, `SelectedFile.Metadata.Artist` e `SelectedFile.Metadata.Album` usam `Mode=OneWay` explicitamente.
- Demais bindings de exibição críticos foram explicitados como OneWay onde aplicável.
- O startup-check agora injeta metadados de teste, cria uma entrada de Histórico, renderiza as duas páginas e abre os ComboBoxes principais.
- O startup-check alterna `Escuro`, `Claro` e `Sistema` e restaura a preferência anterior.
- O startup-check registra um listener para erros de data binding do WPF e só conclui com `Binding diagnostics: OK.`.
- O GitHub Actions exige `Binding diagnostics: OK.`, `Theme switch check: OK.` e `Startup check: OK.` tanto no executável de build quanto na cópia instalada pelo Setup.
- Foi adicionado um gate estático no workflow que recusa `Run.Text` com Binding sem `Mode=OneWay`.

## Validações estáticas executadas antes do ZIP

- XML/XAML, `.csproj`, `.props` e `App.config`: válidos.
- YAML do GitHub Actions: válido.
- Todos os `StaticResource`/`DynamicResource` usados pela UI têm definição correspondente no projeto/ThemeService.
- Handlers declarados no XAML existem no code-behind.
- Solution e `ProjectReference` apontam para arquivos existentes.
- Bindings críticos read-only estão em OneWay.
- Nenhum `setup.exe`, pasta `Código`/`Codigo`, `.obj`, `.bpl`, `.bpi` ou `.tds` está incluído.
- Não há `bin`, `obj`, `dist` ou `.vs` empacotados.
- `RenomearTudo.Core`, `MainViewModel.cs`, `BindableFileItem.cs` e `BindableRenameRule.cs` permanecem idênticos à 2.1.0.
- Versão sincronizada em `Directory.Build.props`, UI, Inno Setup e workflow: `2.1.1`.
- Verificação estrutural dos arquivos C#: OK.

## Validação Windows no CI

O ambiente de geração deste pacote não executa WPF/.NET Framework. Por isso o workflow Windows é o gate final e obrigatório antes da Release:

1. Restore.
2. Rebuild com warnings como erros.
3. Smoke tests do Core.
4. Startup-check da UI com arquivos/detalhes/Histórico/popups/temas.
5. Empacotamento portátil.
6. Download e validação do runtime oficial .NET Framework 4.8.
7. Build do instalador Inno Setup.
8. Instalação silenciosa em diretório temporário.
9. Startup-check da cópia instalada.
10. Microsoft Defender.
11. SHA-256.
12. Artifact e GitHub Release.
