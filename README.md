# Renomear Tudo — modernização para Windows

> Modernização cuidadosa do projeto original **Renomear Tudo** (2004-2005), preservando seu propósito: renomear arquivos em massa com segurança, rapidez e controle.

## Regra de ouro

**Não fazer alterações desnecessárias que possam quebrar o projeto. Preservar a estrutura e o comportamento útil existentes e aplicar mudanças mínimas, isoladas e verificáveis.**

Por isso, a implementação moderna fica isolada em `src/` e não depende de binários ou fontes legados. O repositório atual contém apenas a implementação moderna, testes, documentação e automação de build.

## Nova aplicação

- C# com linguagem moderna, compilada para **.NET Framework 4.8**.
- Interface **WPF com linguagem visual Fluent/Windows UI**, moderna, minimalista e sem dependência de WinUI em runtime.
- Compatível com Windows 10 e Windows 11.
- Compatibilidade de execução com **Windows 7 SP1 + .NET Framework 4.8** mantida como best effort, pois o Windows 7 não recebe mais suporte da Microsoft.
- Sem dependências NuGet de runtime: reduz superfície de ataque e simplifica a distribuição.
- Unicode nativo.
- Operações de arquivo com validação e renomeação em duas fases para evitar colisões.


## Design Fluent responsivo 2.2

A versão 2.2 mantém a linguagem visual Fluent e torna a camada de interface realmente adaptável, sem alterar o motor de renomeação. O layout segue os princípios do Windows UI/Fluent: navegação previsível, hierarquia tipográfica, espaçamento consistente, comandos claros, estados de hover/foco/seleção e superfícies discretas.

O projeto **não usa WinUI 3 em runtime** porque a compatibilidade solicitada inclui Windows 7. Em vez disso, o visual Fluent foi implementado diretamente em WPF/.NET Framework 4.8, mantendo a mesma base compatível com Windows 7 SP1, Windows 10 e Windows 11.

A interface trabalha com breakpoints internos: em desktop exibe navegação completa e Regras + Prévia lado a lado; em largura intermediária compacta a navegação; em janelas estreitas oferece alternância **Arquivos/Regras**, evitando controles esmagados. A janela também se ajusta à área útil do monitor e reduz elementos secundários quando a altura disponível é pequena.

Para listas grandes, a tabela usa virtualização/recycling, a busca tem debounce e a inclusão de arquivos/pastas é preparada fora da thread gráfica e aplicada em lote.

O tema Sistema/Claro/Escuro agora estiliza também controles que antes herdavam templates claros do Windows, incluindo ComboBox e seu popup, TextBox, CheckBox, ListBox, DataGrid, botões e barras de rolagem. A preferência de tema é salva localmente em `%LOCALAPPDATA%\RenomearTudo\theme.txt`.

## Funcionalidades implementadas

1. UI Fluent totalmente redesenhada, com NavigationView-like, CommandBar, cards e hierarquia visual no padrão Windows.
2. Tema Sistema / Escuro / Claro.
3. Prévia em tempo real.
4. Nome original → novo nome → status.
5. Drag & drop de arquivos e pastas.
6. Botões separados para arquivos e pasta.
7. Barra lateral de regras.
8. Regras empilháveis.
9. Reordenação das regras por arrastar ou pelos botões ↑/↓.
10. Indicadores de conflito, nome inválido e arquivo existente.
11. Resumo antes da operação e aplicação somente aos itens seguros.
12. Desfazer última operação.
13. Histórico persistente das operações.
14. Presets persistentes.
15. Pesquisa, filtros e ordenação.
16. Numeração configurável (início, incremento e quantidade de dígitos).
17. Templates de nome.
18. Localizar/substituir simples ou Regex.
19. Painel de detalhes + leitura ID3v1 para MP3.
20. Progresso e cancelamento.

Também foram preservadas ideias centrais do original: prefixo, sufixo, extensão, caixa alta/baixa, inserção em posição, remoção de texto, remoção de acentos/caracteres inválidos, ordenação, regex e relatório CSV.

## Templates

Tokens disponíveis:

```text
{nome}      nome atual sem extensão
{numero}    número da sequência
{total}     total de arquivos
{data}      data de modificação (AAAA-MM-DD)
{pasta}     nome da pasta
{ext}       extensão sem ponto
{artista}   ID3v1
{titulo}    ID3v1
{album}     ID3v1
{ano}       ID3v1
{genero}    ID3v1
{faixa}     ID3v1
```

Exemplos:

```text
{nome}_{numero}
{data}_{nome}
{pasta}_{numero}
{artista} - {titulo}
Foto_{numero}_de_{total}
```

## Segurança do renomeio

Antes de alterar qualquer arquivo, o programa verifica:

- nomes vazios;
- caracteres proibidos pelo Windows;
- nomes reservados (`CON`, `PRN`, `AUX`, `NUL`, `COM1`...);
- nome terminando em ponto/espaço;
- destinos duplicados;
- arquivo de destino já existente;
- caminhos incompatíveis com o limite conservador usado para Windows 7.

Trocas como `A.txt → B.txt` e `B.txt → A.txt` são executadas em duas fases com nomes temporários únicos. Se uma etapa falhar, o motor tenta restaurar o estado anterior.

## Estrutura

```text
RenomearTudo/
├─ src/
│  ├─ RenomearTudo.Core/        # regras, validação, rename engine, histórico
│  └─ RenomearTudo.App/         # WPF / UI
├─ tests/
│  └─ RenomearTudo.SmokeTests/  # testes sem framework externo
├─ installer/
│  ├─ RenomearTudo.iss           # instalador Inno Setup
│  └─ prerequisites/             # preenchido somente durante o build
├─ .github/workflows/
│  └─ build-release.yml          # build/release somente manual
├─ RenomearTudo.sln
├─ NOTICE.md
└─ README.md
```

## Compilar localmente

Recomendado: Windows 10/11 com Visual Studio 2022 e workload **.NET desktop development**, incluindo o targeting pack do .NET Framework 4.8.

```powershell
msbuild RenomearTudo.sln /t:Restore /p:Configuration=Release
msbuild RenomearTudo.sln /m /p:Configuration=Release
.\tests\RenomearTudo.SmokeTests\bin\Release\net48\RenomearTudo.SmokeTests.exe
```

Saída principal:

```text
src\RenomearTudo.App\bin\Release\net48\RenomearTudo.exe
```

## GitHub Actions e Releases

O workflow **não executa automaticamente** em `push` ou `pull_request`. Ele só roda quando você escolher **Run workflow** na aba Actions.

Fluxo manual:

1. Abra **Actions → Build Release (Manual)**.
2. Clique em **Run workflow**.
3. Informe a versão, por exemplo `2.2.0`.
4. O workflow compila, executa os testes do motor, faz um teste real de inicialização da janela, gera o instalador offline, instala e testa esse Setup em uma pasta temporária, verifica com Microsoft Defender, calcula SHA-256 e publica a Release.

A Release não depende mais de uma tag criada previamente. O próprio workflow cria a Release/tag `vX.Y.Z` para o commit que foi compilado.

Arquivos publicados:

```text
RenomearTudo-Setup-2.2.0.exe       # recomendado; instalador offline
RenomearTudo-Portable-2.2.0.zip    # versão portátil; requer .NET 4.8 já instalado
SHA256SUMS.txt
```

### Instalador e pré-requisitos

O Setup é criado com Inno Setup e contém o **Microsoft .NET Framework 4.8 Runtime Offline** oficial da Microsoft. O workflow baixa esse runtime diretamente de `download.microsoft.com`, valida a assinatura Authenticode da Microsoft e somente então o incorpora ao instalador.

Na instalação:

- se .NET Framework 4.8 ou superior já existir, nada extra é instalado;
- se estiver ausente, o runtime incluído no Setup é instalado sem precisar de internet;
- se o runtime exigir reinicialização, o Setup interrompe de forma segura e pede para executar novamente após reiniciar.

O aplicativo não usa Visual C++ Runtime nem outras bibliotecas nativas externas.

### Diagnóstico de inicialização

A inicialização possui tratamento de exceções e grava um log em:

```text
%LOCALAPPDATA%\RenomearTudo\logs\startup.log
```

Além disso, a Release só é gerada se `RenomearTudo.exe --startup-check` conseguir inicializar o WPF, os recursos XAML, o ViewModel e a janela principal no runner Windows.

## Windows 7

O alvo escolhido é `.NET Framework 4.8` porque ele pode ser instalado no **Windows 7 SP1**, além de funcionar em Windows 10/11. Windows 7 está fora de suporte; portanto, não é possível prometer segurança do próprio sistema operacional. O projeto evita APIs modernas exclusivas de Windows 10/11 e mantém um limite de caminho conservador para esse modo de compatibilidade.

## Projeto original

A autoria e as condições históricas de uso do projeto original permanecem registradas em `NOTICE.md` e `LICENSE-ORIGINAL.txt`. O pacote moderno não inclui o instalador nem os fontes/binários legados.
