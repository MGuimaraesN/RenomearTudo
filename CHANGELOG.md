# Changelog

## 2.0.3 - correção definitiva de inicialização e validação de Release

- Corrigida a falha real que impedia a janela WPF de abrir: `ProgressBar.Value` agora usa binding explícito `Mode=OneWay` para a propriedade somente leitura `ProgressValue`.
- Auditados os demais bindings editáveis para evitar outro binding TwoWay em propriedade sem setter.
- O `--startup-check` agora carrega um arquivo temporário e força a criação de uma linha real no DataGrid, testando também bindings que não existem quando a janela está vazia.
- Validação de nomes reforçada antes de montar o caminho de destino; entradas inválidas deixam de poder gerar exceção durante a prévia.
- Detectado conflito quando já existe uma **pasta** com o mesmo nome do destino, além de arquivo existente.
- Adicionado limite explícito de 255 caracteres por componente de nome.
- Smoke tests ampliados para arquivo/pasta existente, cadeia de conflitos, caracteres inválidos, nomes longos e conflito de pasta durante o Undo.
- Build de CI alterado para `Rebuild` e warnings tratados como erros.
- Workflow valida a estrutura do repositório e rejeita `setup.exe`, pasta `Codigo/Código` e artefatos legados `.obj/.bpl/.bpi/.tds`.
- Adicionado teste de integração do instalador: instala em pasta temporária, verifica os arquivos e executa o aplicativo instalado com `--startup-check` antes da Release.
- Download do .NET Framework 4.8 offline possui fallback, tentativas e validação de assinatura Authenticode da Microsoft.
- Publicação da Release agora verifica erros do `gh` e impede reutilizar a mesma tag para um commit diferente.
- Workflow permanece exclusivamente manual (`workflow_dispatch`).

## 2.0.2 - correção do teste de GUI e pipeline de Release

- Corrigido falso negativo do `GUI startup test`: aplicativos WPF `WinExe` não fornecem `$LASTEXITCODE` de forma confiável quando iniciados diretamente pelo PowerShell.
- O teste agora usa `Start-Process -PassThru`, aguarda o processo, valida o `ExitCode` real e exige `Startup check: OK.` no log.
- Adicionado timeout de 30 segundos para evitar workflow preso caso a interface não finalize o teste.
- O Inno Setup passa a ser instalado via Chocolatey somente se `ISCC.exe` não existir no runner.
- Instalador usa `WizardStyle=modern` para maior compatibilidade entre versões do Inno Setup.
- Adicionada opção de abrir o aplicativo ao finalizar a instalação.
- Workflow continua exclusivamente manual (`workflow_dispatch`), sem build automático em push ou pull request.

## 2.0.1 - correções de distribuição e inicialização

- Workflow alterado para execução exclusivamente manual (`workflow_dispatch`).
- Release manual não é mais pulada por depender de tag `v*`.
- Workflow cria/atualiza automaticamente a tag e a GitHub Release da versão informada.
- Adicionado teste real de inicialização WPF antes da publicação.
- Adicionado diagnóstico de falhas de startup em `%LOCALAPPDATA%\RenomearTudo\logs\startup.log`.
- Adicionado instalador Inno Setup moderno.
- Setup final contém o runtime offline oficial do .NET Framework 4.8.
- O runtime baixado no CI tem assinatura Authenticode da Microsoft validada antes de ser incorporado.
- Instalador suporta Windows 7 SP1, Windows 10 e Windows 11 dentro das limitações do .NET Framework 4.8.

## 2.0.0 - modernização inicial

- Estrutura modernizada limpa, sem artefatos/fontes legados no pacote.
- Corrigido empacotamento do GitHub Actions.
- Actions atualizadas para runtimes atuais do GitHub.
- Release idempotente: reexecuções atualizam os arquivos existentes.
- Nova aplicação C# / WPF para .NET Framework 4.8.
- Nova UI minimalista com tema claro/escuro/sistema.
- Motor de preview e regras empilháveis.
- Rename transacional em duas fases e rollback.
- Conflitos e nomes inválidos detectados antes da execução.
- Undo persistente e histórico.
- Presets.
- Templates e suporte ID3v1.
- Drag & drop.
- Busca, filtros e ordenação.
- Ajuste manual da prévia.
- Exportação CSV.
- Smoke tests.
- GitHub Actions para build, testes, Artifact, SHA-256 e GitHub Releases.
