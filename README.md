# Luli Reaper PR

PromeRotation ACR for Reaper (RPR), migrated from the AE rotation logic.

## Local build

```powershell
dotnet build Luli.csproj --configuration Release
```

The project uses the installed Dalamud and PromeRotation references by default. CI overrides those paths with the checked-in `lib` directory.

## Remote ACR manifest

After publishing a GitHub Release, add this URL to PromeRotation's remote ACR list:

`https://github.com/Lulimmm/LuliPR/releases/latest/download/repo.json`

The checked-in [`repo.json`](repo.json) is a metadata template. The release workflow replaces it in the release artifact with the actual package SHA-256 value.

## Release

Push a version tag such as `v1.0.0.0` or run the `Build and Release` workflow manually. The workflow packages only the ACR assembly and its dependency manifest.
