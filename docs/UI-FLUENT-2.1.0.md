# UI Fluent 2.1.0

A versão 2.1.0 substitui o layout visual da 2.0.x sem alterar o motor de renomeação.

## Objetivos

- aparência coerente com o Windows UI / Fluent Design;
- menos ruído visual e melhor hierarquia;
- tema claro/escuro consistente em controles e popups;
- preservar Windows 7 SP1, Windows 10 e Windows 11;
- não adicionar dependências NuGet ou runtimes desnecessários;
- manter prévia, regras, histórico, presets, drag & drop, undo e segurança de renomeio.

## Estrutura visual

- barra de título customizada e tematizada;
- navegação lateral semelhante ao NavigationView;
- página principal com CommandBar, indicadores e workspace em duas colunas;
- editor de transformações dedicado;
- tabela de prévia como foco principal;
- empty state para drag & drop;
- detalhes do arquivo selecionado em faixa compacta;
- action bar fixa com progresso e botão Renomear;
- histórico em página própria.

## Tema

O tema foi implementado sobre DynamicResource do WPF. `ThemeService` controla uma paleta completa de superfícies, bordas, texto, estados e cores sem trocar o motor do programa.

A preferência é persistida em:

```text
%LOCALAPPDATA%\RenomearTudo\theme.txt
```

No modo `Sistema`, o programa acompanha `AppsUseLightTheme` quando disponível. Em Windows 7, onde essa preferência de aplicativos não existe, o modo Sistema usa claro por padrão; Claro e Escuro continuam disponíveis manualmente.

## Compatibilidade

WinUI 3 não é usado diretamente porque ele exige versões modernas do Windows. A interface usa a linguagem visual Fluent em WPF/.NET Framework 4.8 para preservar o alvo Windows 7 SP1 + Windows 10/11 já definido pelo projeto.
