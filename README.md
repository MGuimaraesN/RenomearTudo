# Renomear Tudo — modernização para Windows

> Modernização cuidadosa do projeto original **Renomear Tudo** (2004-2005), preservando seu propósito: renomear arquivos em massa com segurança, rapidez e controle.

## Regra de ouro

**Não fazer alterações desnecessárias que possam quebrar o projeto. Preservar a estrutura e o comportamento útil existentes e aplicar mudanças mínimas, isoladas e verificáveis.**

Por isso, os fontes Borland/C++Builder originais permanecem no repositório como referência. A aplicação moderna vive em `src/` e não depende dos binários antigos.

## Nova aplicação

- C# com linguagem moderna, compilada para **.NET Framework 4.8**.
- Interface **WPF** moderna e minimalista.
- Compatível com Windows 10 e Windows 11.
- Compatibilidade de execução com **Windows 7 SP1 + .NET Framework 4.8** mantida como best effort, pois o Windows 7 não recebe mais suporte da Microsoft.
- Sem dependências NuGet de runtime: reduz superfície de ataque e simplifica a distribuição.
- Unicode nativo.
- Operações de arquivo com validação e renomeação em duas fases para evitar colisões.

## Funcionalidades implementadas

1. UI moderna e minimalista.
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
├─ C#U00f3digo/                 # código original preservado
├─ src/
│  ├─ RenomearTudo.Core/        # regras, validação, rename engine, histórico
│  └─ RenomearTudo.App/         # WPF / UI
├─ tests/
│  └─ RenomearTudo.SmokeTests/  # testes sem framework externo
├─ .github/workflows/
│  └─ build-release.yml
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

`.github/workflows/build-release.yml`:

- compila em `windows-2025` (runner fixado para evitar mudanças inesperadas do alias `windows-latest`);
- executa smoke tests;
- cria `RenomearTudo-Windows.zip`;
- gera `SHA256SUMS.txt`;
- tenta executar Microsoft Defender no conteúdo gerado;
- envia o pacote como Artifact em pushes/PRs;
- em tags `v*`, publica automaticamente os arquivos em **GitHub Releases**.

Para publicar uma versão:

```bash
git tag v2.0.0
git push origin v2.0.0
```

## Windows 7

O alvo escolhido é `.NET Framework 4.8` porque ele pode ser instalado no **Windows 7 SP1**, além de funcionar em Windows 10/11. Windows 7 está fora de suporte; portanto, não é possível prometer segurança do próprio sistema operacional. O projeto evita APIs modernas exclusivas de Windows 10/11 e mantém um limite de caminho conservador para esse modo de compatibilidade.

## Projeto original

O projeto original e sua autoria permanecem preservados. Consulte `C#U00f3digo/LICENCA.TXT` e `NOTICE.md` antes de redistribuir publicamente uma versão modificada.
