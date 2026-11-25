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

## Ninja manual Suiton

The hotkey panel's `预备水遁` queues one Ten → Chi → Jin → Suiton group per click,
using the same priority, verification and five-second request debounce as manual
Raiton. It does not enable an automatic preparation toggle. It waits for a legal
GCD submission window and rejects requests during countdowns, fixed openers,
another mudra sequence, Kassatsu, an existing Suiton buff, or insufficient mudra
charges. Opener countdown actions remain independent.

```powershell
./tools/verify-ninja-suiton-hotkey.ps1
./tools/verify-ninja-debug.ps1
```

## Ninja delay burst

The `延后爆发` QT defaults to off and affects normal rotation decisions.
When enabled, it holds Suiton, Dokumori, Kunai's Bane,
Ten Chi Jin, Dream Within a Dream, Bunshin and Kassatsu. Raiton spends mudra only
at 1.8 charges or more, and its Fleeting Raiju proc remains usable without waiting
for a burst window. This mode enables those overflow Raiton/Raiju decisions even
if their individual QTs are off. Fixed opener/countdown sequences and manual
hotkeys keep their own behavior.

```powershell
./tools/verify-ninja-delay-burst.ps1
```

## Ninja post-ninjutsu weaving

Normal rotation decisions allow one ability after Suiton, Hyosho Ranryu or
Raiton, then wait for the next GCD. The shared confirmation deduplication stays
in place; Suiton and Hyosho no longer have a separate 2.5-second deduplication
window. The single-weave counter advances on confirmed action effects.

## Remote ACR manifest

After publishing a GitHub Release, add this URL to PromeRotation's remote ACR list:

`https://github.com/Lulimmm/LuliPR/releases/latest/download/repo.json`

The checked-in [`repo.json`](repo.json) is a metadata template. The release workflow replaces it in the release artifact with the actual package SHA-256 value.

## Timeline maintenance

The shared UI framework is in `Reaper/MewUI`. Timeline sources are in `Timeline/NewTimeline`.

See [时间轴维护说明](Timeline/维护说明.md) for the GitHub update path, runtime installation paths, version rules, MewUI management page, packaging, and troubleshooting.

## Release

Push a version tag such as `v1.1.0` or run the `Build and Release` workflow manually. The workflow builds the ACR assembly and packages it with its dependency manifest.
