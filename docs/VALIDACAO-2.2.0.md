# Validação 2.2.0 — responsividade e otimização

## Escopo

A versão 2.2.0 altera somente a camada da aplicação WPF e infraestrutura de coleção/diagnóstico. O projeto `RenomearTudo.Core` não foi alterado.

## Breakpoints testados pelo startup-check

- 1360 × 820: layout desktop, navegação completa e painel duplo.
- 1040 × 700: layout compacto, navegação por ícones e painel duplo reduzido.
- 840 × 600: layout estreito, cards 2x2 e alternância Arquivos/Regras.

O startup-check também materializa DataGrid, detalhes, Histórico, ComboBoxes e alterna Claro/Escuro/Sistema.

## Otimizações

- `BulkObservableCollection<T>` reduz eventos de coleção em adições e ordenações em massa.
- DataGrid/ListBox usam virtualização e recycling.
- Busca e prévia possuem debounce adaptativo.
- Adição de arquivos e recarga após rename fazem leitura de `FileInfo`/ID3 fora da thread da UI quando iniciadas pelo usuário.
- Ordenação recalcula a prévia apenas uma vez.

## Compatibilidade

Mantida a base WPF + .NET Framework 4.8, `AnyCPU`, DPI awareness PerMonitorV2/PerMonitor e instalador com requisito mínimo Windows 7 SP1.
