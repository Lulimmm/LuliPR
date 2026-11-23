# Luli PR ACR

PromeRotation ACR for Reaper (RPR) and Ninja (NIN), migrated from the AE rotation logic.

## Local build

```powershell
dotnet build Luli.csproj --configuration Release
```

The project uses the installed Dalamud and PromeRotation references by default. CI overrides those paths with the checked-in `lib` directory.

UI images stay embedded in the DLL: transparent pet/loader frames use PNG, and opaque backgrounds use high-quality JPEG at their original resolution. Both use built-in Windows WIC codecs without a WebP extension. Verify the actual DLL's image names and decoding after changing assets:

```powershell
./tools/verify-ui-textures.ps1 -AssemblyPath <path-to-LuliReaperPR.dll>
```

## Remote ACR manifest

After publishing a GitHub Release, add this URL to PromeRotation's remote ACR list:

`https://github.com/Lulimmm/LuliPR/releases/latest/download/repo.json`

The checked-in [`repo.json`](repo.json) is a metadata template. The release workflow replaces it in the release artifact with the actual package SHA-256 value.

## Timeline maintenance

The shared UI framework is in `Reaper/MewUI`. Timeline sources are in `Timeline/NewTimeline`.

See [时间轴维护说明](Timeline/维护说明.md) for the GitHub update path, runtime installation paths, version rules, MewUI management page, packaging, and troubleshooting.

## Release

Push a version tag such as `v1.1.0` or run the `Build and Release` workflow manually. The workflow builds the ACR assembly and packages it with its dependency manifest.
