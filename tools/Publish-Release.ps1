[CmdletBinding()]
param(
    [string] $ReleaseDirectory = 'dist/v0.8.0-beta.1',
    [string] $Tag = 'v0.8.0-beta.1'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$releasePath = (Resolve-Path -LiteralPath (Join-Path $repositoryRoot $ReleaseDirectory)).Path
$required = @(
    'Wormhole-Worlds-Simulator-0.8.0-beta.1-Windows-x64-Setup.exe',
    'Wormhole-Worlds-Simulator-0.8.0-beta.1-Windows-x64-Portable.zip',
    'update-preview.json',
    'build-manifest.json',
    'SHA256SUMS.txt',
    'RELEASE_NOTES.md',
    'THIRD-PARTY-NOTICES.md'
)
foreach ($fileName in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $releasePath $fileName))) {
        throw "Release artifact is missing: $fileName"
    }
}

$gh = (Get-Command gh -ErrorAction Stop).Source
$existingTag = & git -C $repositoryRoot tag --list $Tag
if ([string]::IsNullOrWhiteSpace($existingTag)) {
    throw "Create and push immutable tag $Tag before publishing."
}

$assets = Get-ChildItem -LiteralPath $releasePath -File | ForEach-Object FullName
& $gh release create $Tag @assets `
    --repo 'Bambie-Digital-Works/Stargate-Command-Simulator' `
    --prerelease `
    --verify-tag `
    --title 'Wormhole Worlds Simulator 0.8.0-beta.1' `
    --notes-file (Join-Path $repositoryRoot 'docs/RELEASE_NOTES_0.8.0-beta.1.md')
if ($LASTEXITCODE -ne 0) { throw 'GitHub prerelease publication failed.' }
