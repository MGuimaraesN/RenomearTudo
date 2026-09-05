# Build e Release

## Requisitos locais

- Windows 10 ou 11 para desenvolvimento.
- Visual Studio 2022+ com **.NET desktop development**.
- .NET Framework 4.8 Developer/Targeting Pack.

## Build

```powershell
msbuild RenomearTudo.sln /t:Restore /p:Configuration=Release
msbuild RenomearTudo.sln /m /p:Configuration=Release
```

## Testes

```powershell
.\tests\RenomearTudo.SmokeTests\bin\Release\net48\RenomearTudo.SmokeTests.exe
```

Os smoke tests verificam preview, substituição case-insensitive, troca de nomes em duas fases, rename somente por caixa, conflitos duplicados, nomes reservados e remoção de acentos.

## GitHub Actions

Workflow: `.github/workflows/build-release.yml`.

- Runner fixado: `windows-2025`.
- PR/push: build + smoke tests + empacotamento limpo + Defender + Artifact.
- Tag `v*`: o job de Release baixa exatamente o Artifact testado e publica/atualiza o ZIP e o `SHA256SUMS.txt`.
- PRs não recebem token com permissão de escrita no repositório.

## Criar release

```bash
git tag v2.0.0
git push origin v2.0.0
```

Arquivos esperados na aba **Releases**:

```text
RenomearTudo-Windows.zip
SHA256SUMS.txt
```

## Actions usadas

- `actions/checkout@v7`
- `microsoft/setup-msbuild@v3`
- `actions/upload-artifact@v7`
- `actions/download-artifact@v8`
