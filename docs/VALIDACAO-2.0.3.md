# Validação 2.0.3

Esta revisão foi feita a partir dos logs do GitHub Actions enviados após a versão 2.0.2.

## Causa confirmada da falha anterior

O log mostrava que Restore, Build e Smoke Tests concluíam com **0 warnings e 0 errors**. A falha real ocorria ao abrir a janela WPF:

```text
System.InvalidOperationException: A TwoWay or OneWayToSource binding cannot work
on the read-only property 'ProgressValue'.
```

A correção aplicada define explicitamente `Mode=OneWay` no `ProgressBar.Value` e audita os demais bindings editáveis.

## Gates da versão 2.0.3

O workflow é exclusivamente manual e só publica uma Release depois de:

1. validar a versão e a estrutura do repositório;
2. executar Restore + Rebuild com warnings como erros;
3. passar os smoke tests do motor;
4. executar `RenomearTudo.exe --startup-check`;
5. criar a versão portátil;
6. baixar e validar o runtime offline oficial do .NET Framework 4.8;
7. compilar o Setup com Inno Setup;
8. instalar o Setup em pasta temporária;
9. executar o `--startup-check` da cópia instalada;
10. passar pelo Microsoft Defender quando disponível no runner;
11. gerar SHA-256;
12. fazer upload dos artefatos;
13. publicar/atualizar a GitHub Release.

O `--startup-check` cria um arquivo temporário, adiciona uma linha real ao DataGrid, força o layout da janela e limpa o arquivo ao finalizar. Isso testa bindings que só existem quando há um arquivo listado.

## Validações estáticas executadas nesta revisão

- XAML/XML bem formados.
- YAML do workflow analisado com jobs `build` e `release`.
- referências `.sln` e `ProjectReference` existentes.
- versão padrão consistente em `Directory.Build.props`, workflow e Inno Setup.
- workflow sem gatilhos `push`, `pull_request` ou `schedule`.
- `ProgressValue` e coluna `OriginalName` explicitamente OneWay onde necessário.
- ausência de `setup.exe` legado.
- ausência de pasta `Codigo`/`Código`.
- ausência de `.obj`, `.bpl`, `.bpi`, `.tds`, `.exe` e `.dll` versionados no pacote-fonte.
- ausência de pastas geradas `bin`, `obj`, `dist` e `.vs`.
- ausência de marcadores `TODO`, `FIXME` e `HACK`.

## Observação de ambiente

O ambiente em que esta revisão foi empacotada é Linux e não possui MSBuild/WPF do .NET Framework. Por isso, a compilação Windows final não é executável localmente aqui. O log anterior já comprovou que a base 2.0.2 compilava com 0 warnings/0 errors; o workflow 2.0.3 foi reforçado para impedir qualquer Release se as alterações atuais não compilarem, se a GUI não iniciar ou se o instalador gerado não funcionar.
