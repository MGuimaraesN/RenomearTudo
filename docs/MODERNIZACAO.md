# Modernização — Renomear Tudo 2.0

A modernização foi feita sem apagar nem alterar os fontes C++Builder originais. A pasta histórica continua intacta e a implementação nova fica isolada em `src/`.

| # | Ideia | Implementação |
|---|---|---|
| 1 | UI moderna/minimalista | WPF, cartões discretos, Segoe UI, foco em regras + prévia |
| 2 | Claro/escuro | Sistema, Escuro e Claro; segue preferência do Windows quando disponível |
| 3 | Prévia em tempo real | Atualização automática com debounce de 140 ms |
| 4 | Original → Novo → Status | DataGrid virtualizado |
| 5 | Drag & Drop | Arquivos e pastas aceitos na janela |
| 6 | Arquivos/pastas separados | Botões dedicados no cabeçalho |
| 7 | Barra de regras | Painel lateral permanente |
| 8 | Regras empilháveis | Pipeline ordenado de regras |
| 9 | Reordenar regras | Arrastar e soltar + ↑/↓ |
| 10 | Conflitos | Duplicidade, destino existente, nome inválido e caminho incompatível |
| 11 | Resumo de execução | Total/prontos/conflitos e confirmação antes do rename |
| 12 | Desfazer | Undo transacional em duas fases |
| 13 | Histórico | XML em `%LOCALAPPDATA%\\RenomearTudo`, até 50 operações |
| 14 | Presets | Salvar/carregar/excluir conjuntos de regras |
| 15 | Busca/filtros | Busca textual, alterados, conflitos, válidos, ignorados e ordenações |
| 16 | Numeração | Início, incremento, padding e template |
| 17 | Templates | `{nome}`, `{numero}`, `{total}`, `{data}`, `{pasta}` e tags MP3 |
| 18 | Localizar/substituir | Simples ou Regex, primeira ocorrência e ignore-case |
| 19 | Detalhes | Caminho, tamanho, data e ID3v1 sob demanda |
| 20 | Progresso | Barra, arquivo atual e cancelamento com rollback |

## Funcionalidades herdadas do conceito original

- Prefixo e sufixo.
- Alteração de extensão.
- Maiúsculas, minúsculas e título.
- Inserção por posição.
- Remoção de texto.
- Remoção de acentos.
- Remoção de caracteres especiais.
- Ordenação alfabética, data, tamanho e aleatória.
- Expressões regulares.
- Templates baseados em metadados MP3.
- Ajuste manual do novo nome diretamente na tabela.
- Exportação de relatório CSV.

## Decisões de segurança

- O app não renomeia itens marcados como conflito/inválidos.
- O destino nunca é sobrescrito silenciosamente.
- Trocas como `A → B` e `B → A` usam nomes temporários únicos.
- Em falha/cancelamento, o motor tenta rollback.
- O desfazer valida todas as origens/destinos antes de iniciar.
- Nomes reservados do Windows são bloqueados.
- O caminho usa limite conservador para manter compatibilidade com Windows 7.
- Nenhum binário legado é usado pela aplicação moderna.

## Otimização

- DataGrid com virtualização de linhas e colunas.
- Prévia com debounce para evitar recalcular a cada evento de teclado.
- ID3 só é lido quando um arquivo MP3 é selecionado ou quando alguma regra usa tokens de metadados.
- Sem dependências NuGet de runtime.
- Build Release com otimização e compilação determinística.
