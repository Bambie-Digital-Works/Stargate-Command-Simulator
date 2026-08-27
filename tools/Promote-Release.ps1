[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $SourceReleaseDirectory,

    [Parameter(Mandatory = $true)]
    [string] $DestinationTag,

    [Parameter(Mandatory = $true)]
    [ValidateSet('stable', 'legacy')]
    [string] $Channel
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$sourcePath = [IO.Path]::GetFullPath((Join-Path $repositoryRoot $SourceReleaseDirectory))
$allowedRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot 'dist'))
if (-not $sourcePath.StartsWith($allowedRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Promotion source must be a child of $allowedRoot."
}
if (-not (Test-Path -LiteralPath $sourcePath -PathType Container)) {
    throw "Promotion source does not exist: $sourcePath"
}

$required = @(
    'build-manifest.json',
    'RELEASE_NOTES.md',
    'THIRD-PARTY-NOTICES.md',
    'SHA256SUMS.txt',
    'update-preview.json'
)
foreach ($fileName in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $sourcePath $fileName) -PathType Leaf)) {
        throw "Promotion source is missing: $fileName"
    }
}

$manifest = Get-Content -LiteralPath (Join-Path $sourcePath 'update-preview.json') -Raw | ConvertFrom-Json
$installerName = [string]$manifest.installer.fileName
$installerPath = Join-Path $sourcePath $installerName
if ([string]::IsNullOrWhiteSpace($installerName) -or -not (Test-Path -LiteralPath $installerPath -PathType Leaf)) {
    throw 'The source update manifest does not identify a local installer.'
}
$actualHash = (Get-FileHash -LiteralPath $installerPath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualHash -ne ([string]$manifest.installer.sha256).ToLowerInvariant()) {
    throw 'The source installer does not match its update manifest.'
}

$git = (Get-Command git -ErrorAction Stop).Source
$gh = (Get-Command gh -ErrorAction Stop).Source
$tagCommit = & $git -C $repositoryRoot rev-parse --verify "refs/tags/$DestinationTag"
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($tagCommit)) {
    throw "Create and push immutable tag $DestinationTag before promotion."
}
& $gh release view $DestinationTag --repo 'Bambie-Digital-Works/Stargate-Command-Simulator' *> $null
if ($LASTEXITCODE -eq 0) {
    throw "Release $DestinationTag already exists; promotion refuses to replace it."
}

$promotionPath = Join-Path ([IO.Path]::GetTempPath()) "wormhole-worlds-promotion-$([Guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Force -Path $promotionPath | Out-Null
try {
    Get-ChildItem -LiteralPath $sourcePath -File |
        Where-Object Name -notin @('update-preview.json', 'SHA256SUMS.txt') |
        Copy-Item -Destination $promotionPath

    $promotedManifest = [ordered]@{
        schemaVersion = $manifest.schemaVersion
        channel = $Channel
        version = $manifest.version
        architecture = $manifest.architecture
        releasePage = "https://github.com/Bambie-Digital-Works/Stargate-Command-Simulator/releases/tag/$DestinationTag"
        installer = [ordered]@{
            fileName = $installerName
            sizeBytes = (Get-Item -LiteralPath $installerPath).Length
            sha256 = $actualHash
        }
    }
    $manifestName = "update-$Channel.json"
    $promotedManifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $promotionPath $manifestName) -Encoding utf8

    $hashLines = Get-ChildItem -LiteralPath $promotionPath -File |
        Sort-Object Name |
        ForEach-Object {
            $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            "$hash  $($_.Name)"
        }
    $hashLines | Set-Content -LiteralPath (Join-Path $promotionPath 'SHA256SUMS.txt') -Encoding ascii

    $assets = Get-ChildItem -LiteralPath $promotionPath -File | ForEach-Object FullName
    $title = "Wormhole Worlds Simulator $($manifest.version) $Channel"
    $arguments = @(
        'release', 'create', $DestinationTag
    ) + $assets + @(
        '--repo', 'Bambie-Digital-Works/Stargate-Command-Simulator',
        '--verify-tag',
        '--title', $title,
        '--notes-file', (Join-Path $sourcePath 'RELEASE_NOTES.md')
    )
    if ($Channel -eq 'stable') {
        $arguments += '--latest'
    }
    & $gh @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Release promotion failed for $DestinationTag."
    }
}
finally {
    if (Test-Path -LiteralPath $promotionPath) {
        Remove-Item -LiteralPath $promotionPath -Recurse -Force
    }
}
