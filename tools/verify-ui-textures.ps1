param(
    [Parameter(Mandatory = $true)]
    [string]$AssemblyPath
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$assembly = [System.Reflection.Assembly]::LoadFile((Resolve-Path -LiteralPath $AssemblyPath).Path)
$prefix = 'MewUI.Resources.'
$resources = @($assembly.GetManifestResourceNames() | Where-Object { $_.StartsWith($prefix) })
if ($resources.Count -eq 0) { throw 'No embedded UI images found.' }

# Check literal image requests and generated pet animation sequences against the
# actual DLL. A successful build alone does not catch a wrong manifest name.
$required = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$sources = @('CombatControlWindow.cs', 'MewUILayer.cs', 'MewUISplashWindow.cs')
foreach ($source in $sources) {
    $text = Get-Content -LiteralPath (Join-Path $projectRoot "Reaper/MewUI/Common/UI/$source") -Raw -Encoding UTF8
    foreach ($match in [regex]::Matches($text, '"(?<name>(?:pet\.|loader\.)?[A-Za-z0-9_]+\.(?:png|jpg|jpeg|webp))"')) {
        $name = $match.Groups['name'].Value
        if (-not ($name.EndsWith('.png') -or $name.EndsWith('.jpg') -or $name.EndsWith('.jpeg'))) {
            throw "UI still requests an optional-codec image: $name"
        }
        [void]$required.Add($prefix + $name)
    }
    foreach ($match in [regex]::Matches($text, 'Frames\("(?<prefix>pet\.[^"]+)",\s*(?<count>\d+)\)')) {
        if (-not $text.Contains('_{i + 1:00}.png')) { throw 'Generated animation names must use PNG.' }
        for ($i = 1; $i -le [int]$match.Groups['count'].Value; $i++) {
            [void]$required.Add($prefix + $match.Groups['prefix'].Value + ('_{0:00}.png' -f $i))
        }
    }
}
foreach ($name in $required) {
    if ($resources -cnotcontains $name) { throw "Missing embedded UI image: $name" }
}

# WPF BitmapDecoder uses Windows Imaging Component, as does Dalamud's image
# loading path. Decode from the manifest bytes instead of trusting extensions.
Add-Type -AssemblyName PresentationCore
$decoded = 0
foreach ($name in $resources) {
    $isPng = $name.EndsWith('.png')
    $isJpeg = $name.EndsWith('.jpg') -or $name.EndsWith('.jpeg')
    if (-not ($isPng -or $isJpeg)) { throw "UI resource must use a native PNG/JPEG codec: $name" }
    if ($name.StartsWith($prefix + 'pet.') -and -not $isPng) { throw "Pet frames must keep PNG transparency: $name" }
    $stream = $assembly.GetManifestResourceStream($name)
    try {
        $header = [byte[]]::new(8)
        if ($stream.Read($header, 0, 8) -ne 8) { throw "Truncated resource: $name" }
        if ($isPng -and [Convert]::ToHexString($header) -ne '89504E470D0A1A0A') {
            throw "Resource has no PNG signature: $name"
        }
        if ($isJpeg -and ($header[0] -ne 0xFF -or $header[1] -ne 0xD8 -or $header[2] -ne 0xFF)) {
            throw "Resource has no JPEG signature: $name"
        }
        $stream.Position = 0
        $decoder = [System.Windows.Media.Imaging.BitmapDecoder]::Create(
            $stream,
            [System.Windows.Media.Imaging.BitmapCreateOptions]::PreservePixelFormat,
            [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
        $frame = $decoder.Frames[0]
        if ($frame.PixelWidth -le 1 -or $frame.PixelHeight -le 1) { throw "Empty UI image: $name" }
        if ($required.Contains($name) -and $name.StartsWith($prefix + 'pet.') -and
            ($frame.PixelWidth -ne 120 -or $frame.PixelHeight -ne 120)) {
            throw "Pet image must be 120x120: $name"
        }
        $stride = [int][Math]::Ceiling($frame.PixelWidth * $frame.Format.BitsPerPixel / 8.0)
        $pixels = [byte[]]::new($stride * $frame.PixelHeight)
        $frame.CopyPixels($pixels, $stride, 0)
        $decoded++
    }
    catch {
        throw "WIC decode failed for ${name}: $($_.Exception.Message)"
    }
    finally {
        if ($null -ne $stream) { $stream.Dispose() }
    }
}

Write-Host "Verified $($required.Count) referenced UI images; decoded $decoded embedded PNG/JPEG resources with Windows WIC."
