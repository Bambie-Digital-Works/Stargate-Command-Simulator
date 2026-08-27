[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $GodotPath,

    [Parameter(Mandatory = $true)]
    [string] $DotnetPath,

    [Parameter(Mandatory = $true)]
    [string] $IsccPath,

    [string] $OutputDirectory = 'dist/v0.8.0-beta.1',

    [string] $SignToolName
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$outputPath = [IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputDirectory))
$allowedRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot 'dist'))
if (-not $outputPath.StartsWith($allowedRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Release output must be a child of $allowedRoot."
}

$dotnetExecutable = (Resolve-Path -LiteralPath $DotnetPath).Path
$godotExecutable = (Resolve-Path -LiteralPath $GodotPath).Path
$isccExecutable = (Resolve-Path -LiteralPath $IsccPath).Path

[xml] $project = Get-Content -LiteralPath (Join-Path $repositoryRoot 'WormholeWorldsSimulator.csproj') -Raw
$versionPrefix = $project.SelectSingleNode('//VersionPrefix').InnerText
$versionSuffix = $project.SelectSingleNode('//VersionSuffix').InnerText
$numericVersion = $project.SelectSingleNode('//FileVersion').InnerText
$productVersion = "$versionPrefix-$versionSuffix"
$installerBaseName = "Wormhole-Worlds-Simulator-$productVersion-Windows-x64-Setup"
$portableName = "Wormhole-Worlds-Simulator-$productVersion-Windows-x64-Portable.zip"

if (Test-Path -LiteralPath $outputPath) {
    $resolvedOutput = (Resolve-Path -LiteralPath $outputPath).Path
    if (-not $resolvedOutput.StartsWith($allowedRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clear release output outside $allowedRoot."
    }
    Remove-Item -LiteralPath $resolvedOutput -Recurse -Force
}

$appDirectory = Join-Path $outputPath 'app'
New-Item -ItemType Directory -Force -Path $appDirectory | Out-Null
$env:BuildChannel = 'preview'
$env:BuildId = if ([string]::IsNullOrWhiteSpace($env:GITHUB_RUN_NUMBER)) { 'local' } else { $env:GITHUB_RUN_NUMBER }
$env:CommitSha = (& git -C $repositoryRoot rev-parse HEAD).Trim()
$env:BuildUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')

& (Join-Path $PSScriptRoot 'Verify.ps1') `
    -GodotPath $godotExecutable `
    -DotnetPath $dotnetExecutable `
    -ExportDirectory $appDirectory
if ($LASTEXITCODE -ne 0) { throw 'Verification and Windows export failed.' }

Copy-Item -LiteralPath (Join-Path $repositoryRoot 'NOTICE.md') -Destination (Join-Path $appDirectory 'THIRD-PARTY-NOTICES.md')
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'LICENSE') -Destination (Join-Path $appDirectory 'LICENSE.txt')

$portablePath = Join-Path $outputPath $portableName
Compress-Archive -Path (Join-Path $appDirectory '*') -DestinationPath $portablePath -CompressionLevel Optimal

$compilerArguments = @(
    "/DAppSource=$appDirectory",
    "/DOutputDir=$outputPath",
    "/DProductVersion=$productVersion",
    "/DNumericVersion=$numericVersion",
    "/DInstallerBaseName=$installerBaseName"
)
if (-not [string]::IsNullOrWhiteSpace($SignToolName)) {
    $compilerArguments += "/DSignToolName=$SignToolName"
}
$compilerArguments += (Join-Path $repositoryRoot 'installer/WormholeWorldsSimulator.iss')
& $isccExecutable @compilerArguments
if ($LASTEXITCODE -ne 0) { throw 'Inno Setup compilation failed.' }

$installerPath = Join-Path $outputPath "$installerBaseName.exe"
if (-not (Test-Path -LiteralPath $installerPath)) {
    throw 'Expected installer output was not created.'
}

Copy-Item -LiteralPath (Join-Path $appDirectory 'build-manifest.json') -Destination (Join-Path $outputPath 'build-manifest.json') -Force
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'docs/RELEASE_NOTES_0.8.0-beta.1.md') -Destination (Join-Path $outputPath 'RELEASE_NOTES.md')
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'NOTICE.md') -Destination (Join-Path $outputPath 'THIRD-PARTY-NOTICES.md')

$installerHash = (Get-FileHash -LiteralPath $installerPath -Algorithm SHA256).Hash.ToLowerInvariant()
$updateManifest = [ordered]@{
    schemaVersion = 1
    channel = 'preview'
    version = $productVersion
    architecture = 'x86_64'
    releasePage = "https://github.com/Bambie-Digital-Works/Stargate-Command-Simulator/releases/tag/v$productVersion"
    installer = [ordered]@{
        fileName = [IO.Path]::GetFileName($installerPath)
        sizeBytes = (Get-Item -LiteralPath $installerPath).Length
        sha256 = $installerHash
    }
}
$updateManifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $outputPath 'update-preview.json') -Encoding utf8

$hashLines = Get-ChildItem -LiteralPath $outputPath -File |
    Where-Object Name -ne 'SHA256SUMS.txt' |
    Sort-Object Name |
    ForEach-Object {
        $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        "$hash  $($_.Name)"
    }
$hashLines | Set-Content -LiteralPath (Join-Path $outputPath 'SHA256SUMS.txt') -Encoding ascii

if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_OUTPUT)) {
    "release_path=$outputPath" | Add-Content -LiteralPath $env:GITHUB_OUTPUT
    "product_version=$productVersion" | Add-Content -LiteralPath $env:GITHUB_OUTPUT
}

Write-Output "Release artifacts ready: $outputPath"
