# Build e Release

## Requisitos locais

- Windows 10 ou 11 para desenvolvimento.
- Visual Studio 2022+ com **.NET desktop development**.
- .NET Framework 4.8 Developer/Targeting Pack.
- Inno Setup 6.6+ somente se quiser gerar o instalador localmente.

## Build local

```powershell
msbuild RenomearTudo.sln /t:Restore /p:Configuration=Release
msbuild RenomearTudo.sln /m /p:Configuration=Release
```

## Testes

```powershell
.\tests\RenomearTudo.SmokeTests\bin\Release\net48\RenomearTudo.SmokeTests.exe
.\src\RenomearTudo.App\bin\Release\net48\RenomearTudo.exe --startup-check
```

O segundo comando é importante: além do motor, ele inicializa de verdade WPF, XAML, recursos, ViewModel, cria uma linha temporária no DataGrid e abre a janela principal. Assim erros que só aparecem ao abrir o programa impedem uma Release.

## GitHub Actions: somente manual

Workflow: `.github/workflows/build-release.yml`.

Ele possui apenas `workflow_dispatch`; portanto, **não roda em push, PR ou criação de tag**.

Para publicar:

1. GitHub → **Actions**.
2. Abra **Build Release (Manual)**.
3. **Run workflow**.
4. Digite `2.1.0` (ou outra versão `X.Y.Z`).

Se todas as etapas passarem, o segundo job **Publish GitHub Release** roda obrigatoriamente. Se o job de build/testes falhar, o GitHub marcará o job de Release como `skipped` de propósito, para nunca publicar um executável não validado. Não existe mais a antiga condição `startsWith(github.ref, 'refs/tags/v')`, que fazia o job ser pulado em execuções manuais.

## Etapas do workflow

1. valida a versão;
2. restaura e compila a solução;
3. executa os smoke tests;
4. executa `RenomearTudo.exe --startup-check`;
5. monta a versão portátil;
6. baixa o instalador offline oficial do .NET Framework 4.8;
7. valida a assinatura Authenticode da Microsoft;
8. compila `installer/RenomearTudo.iss` com Inno Setup;
9. instala o Setup silenciosamente em uma pasta temporária e executa a aplicação instalada com `--startup-check`;
10. verifica os artefatos com Microsoft Defender;
11. gera `SHA256SUMS.txt`;
12. envia o Artifact;
13. cria ou atualiza a GitHub Release `vX.Y.Z`. Se a tag informada já existir em outro commit, o workflow interrompe a publicação e exige uma nova versão, evitando associar binários novos a uma tag antiga.

## Artefatos

```text
RenomearTudo-Setup-X.Y.Z.exe
RenomearTudo-Portable-X.Y.Z.zip
SHA256SUMS.txt
```

O **Setup** é a distribuição recomendada. Ele contém o runtime offline do .NET Framework 4.8 e instala o pré-requisito apenas quando necessário.

A versão portátil é mantida por conveniência, mas exige .NET Framework 4.8 ou superior já instalado.

## Windows 7

O instalador exige Windows 7 SP1 ou superior. Windows 7 está fora de suporte; use a compatibilidade apenas quando realmente necessária e mantenha o sistema com as últimas atualizações disponíveis.

## Actions usadas

- `actions/checkout@v7`
- `microsoft/setup-msbuild@v3`
- `actions/upload-artifact@v7`
- `actions/download-artifact@v8`
