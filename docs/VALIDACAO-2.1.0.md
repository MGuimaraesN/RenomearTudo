# Validação 2.1.0 — UI Fluent

Esta revisão é focada exclusivamente na camada visual e de tema.

## Regra de ouro

O diretório `src/RenomearTudo.Core` e os arquivos de comportamento principal do ViewModel/modelos foram comparados com a versão 2.0.3 e não foram alterados.

## Validações realizadas

- `App.xaml`: XML bem formado.
- `MainWindow.xaml`: XML bem formado.
- todos os handlers declarados no XAML existem no code-behind.
- todos os `StaticResource` usados pela janela possuem uma chave definida.
- todos os `DynamicResource` usados pela UI possuem valor inicial e/ou são controlados pelo `ThemeService`.
- workflow YAML parseável.
- workflow permanece exclusivamente manual (`workflow_dispatch`).
- versão sincronizada em `Directory.Build.props`, Inno Setup e GitHub Actions: `2.1.0`.
- nenhum `setup.exe`, `Codigo/Código`, `.obj`, `.bpl`, `.bpi` ou `.tds` legado está presente.
- `--startup-check` agora exercita alternância Escuro → Claro → tema anterior.
- CI exige `Theme switch check: OK.` antes de publicar a Release.

## Observação sobre compilação

O ambiente de preparação é Linux e não possui o toolchain WPF/.NET Framework para executar o build Windows localmente. O workflow já existente continua responsável pelo `Rebuild`, smoke tests, inicialização real da GUI, teste de troca de tema, geração/instalação do Setup e validação da aplicação instalada antes da Release.
