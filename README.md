# Luli Reaper PR

PromeRotation ACR for Reaper (RPR), migrated from the AE rotation logic.

## Local build

```powershell
dotnet build Luli.csproj --configuration Release
```

The project uses the installed Dalamud and PromeRotation references by default. CI overrides those paths with the checked-in `lib` directory.

## Release

Push a version tag such as `v1.0.0.0` or run the `Build and Release` workflow manually. The workflow packages only the ACR assembly and its dependency manifest.
