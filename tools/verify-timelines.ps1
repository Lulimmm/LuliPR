param(
    [string]$PackageDirectory
)

$ErrorActionPreference = 'Stop'
$sourceDirectory = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\Timeline\NewTimeline'))
$files = @(Get-ChildItem -LiteralPath $sourceDirectory -File -Filter '*.json')
if ($files.Count -eq 0) { throw "No PTL files found in $sourceDirectory" }

$guids = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$catalog = Get-Content -LiteralPath (Join-Path $PSScriptRoot '..\Common\BundledTimelineSync.cs') -Encoding UTF8 -Raw
$catalogEntries = @([regex]::Matches($catalog, 'new\("(?<file>[^"]+\.json)",\s*"(?<guid>[0-9a-fA-F-]{36})",\s*"(?<name>[^"]+)"\)'))
if ($catalogEntries.Count -ne $files.Count) { throw 'Cloud catalog and source timeline counts differ' }
$metadata = foreach ($file in $files) {
    $document = Get-Content -LiteralPath $file.FullName -Encoding UTF8 -Raw | ConvertFrom-Json
    $guid = [guid]::Empty
    $version = $null
    if (-not [version]::TryParse([string]$document.Version, [ref]$version)) {
        throw "Invalid Version in $($file.Name)"
    }
    if (-not [guid]::TryParse([string]$document.Meta.TimelineGuid, [ref]$guid) -or $guid -eq [guid]::Empty) {
        throw "Invalid Meta.TimelineGuid in $($file.Name)"
    }
    if (-not $guids.Add($guid.ToString('D'))) {
        throw "Duplicate TimelineGuid in $($file.Name)"
    }
    $registered = @($catalogEntries | Where-Object { $_.Groups['file'].Value -ceq $file.Name })
    if ($registered.Count -ne 1 -or $registered[0].Groups['guid'].Value -ine $guid.ToString('D') -or
        $registered[0].Groups['name'].Value -cne $document.Meta.Name) {
        throw "Timeline identity does not match the cloud catalog: $($file.Name)"
    }
    if ($document.Meta.Format -ne 'PTL' -or [string]::IsNullOrWhiteSpace($document.Meta.Name)) {
        throw "Invalid PTL metadata in $($file.Name)"
    }
    if ($null -eq $document.Anchors -or $null -eq $document.Entries -or
        $document.Anchors -isnot [array] -or $document.Entries -isnot [array]) {
        throw "Missing Anchors or Entries array in $($file.Name)"
    }
    [PSCustomObject]@{
        File = $file.Name
        Version = $document.Version
        TimelineGuid = $guid.ToString('D')
        Sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    }
}

if (-not [string]::IsNullOrWhiteSpace($PackageDirectory)) {
    $package = (Resolve-Path -LiteralPath $PackageDirectory).ProviderPath
    foreach ($name in @('LuliReaperPR.dll', 'LuliReaperPR.deps.json')) {
        if (-not (Test-Path -LiteralPath (Join-Path $package $name) -PathType Leaf)) {
            throw "Missing package file: $name"
        }
    }
    if (Test-Path -LiteralPath (Join-Path $package 'Timelines')) {
        throw 'Timeline contents must not be generated or shipped beside the ACR DLL'
    }
    Write-Output "Package verified: $package"
}

$metadata | Select-Object File, Version, TimelineGuid | Format-Table -AutoSize
Write-Output "Verified $($files.Count) PTL timelines."
