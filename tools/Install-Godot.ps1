[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $Destination,

    [switch] $IncludeExportTemplates
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$manifestPath = Join-Path $repositoryRoot 'tools\toolchain-checksums.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$destinationPath = [IO.Path]::GetFullPath($Destination)
$downloadsPath = Join-Path $destinationPath 'downloads'
$editorExtractPath = Join-Path $destinationPath 'editor'
New-Item -ItemType Directory -Force -Path $downloadsPath, $editorExtractPath | Out-Null

function Get-VerifiedArchive {
    param(
        [Parameter(Mandatory = $true)] $Asset
    )

    $archivePath = Join-Path $downloadsPath $Asset.fileName
    if (-not (Test-Path -LiteralPath $archivePath)) {
        Invoke-WebRequest -Uri $Asset.url -OutFile $archivePath
    }

    $actualHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualHash -ne $Asset.sha256) {
        throw "Checksum mismatch for $($Asset.fileName). Expected $($Asset.sha256); received $actualHash."
    }

    return $archivePath
}

$editorArchive = Get-VerifiedArchive -Asset $manifest.editor
$editorExecutable = Get-ChildItem -LiteralPath $editorExtractPath -Recurse -Filter '*_console.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
if ($null -eq $editorExecutable) {
    Expand-Archive -LiteralPath $editorArchive -DestinationPath $editorExtractPath -Force
    $editorExecutable = Get-ChildItem -LiteralPath $editorExtractPath -Recurse -Filter '*_console.exe' | Select-Object -First 1
}

if ($null -eq $editorExecutable) {
    throw 'The verified Godot editor archive did not contain a console executable.'
}

$editorDirectory = $editorExecutable.Directory.FullName
$selfContainedMarker = Join-Path $editorDirectory '_sc_'
if (-not (Test-Path -LiteralPath $selfContainedMarker)) {
    New-Item -ItemType File -Path $selfContainedMarker | Out-Null
}

if ($IncludeExportTemplates) {
    $templatesArchive = Get-VerifiedArchive -Asset $manifest.exportTemplates
    $templateVersion = $manifest.godotVersion.Replace('-', '.') + '.mono'
    $installedTemplatesPath = Join-Path $editorDirectory "editor_data\export_templates\$templateVersion"
    $releaseTemplate = Join-Path $installedTemplatesPath 'windows_release_x86_64.exe'
    if (-not (Test-Path -LiteralPath $releaseTemplate)) {
        $templateExtractPath = Join-Path $destinationPath 'templates-extracted'
        New-Item -ItemType Directory -Force -Path $templateExtractPath | Out-Null
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [IO.Compression.ZipFile]::ExtractToDirectory($templatesArchive, $templateExtractPath, $true)
        $templateSource = Join-Path $templateExtractPath 'templates'
        if (-not (Test-Path -LiteralPath $templateSource)) {
            throw 'The verified export-template archive did not contain a templates directory.'
        }

        New-Item -ItemType Directory -Force -Path $installedTemplatesPath | Out-Null
        Copy-Item -Path (Join-Path $templateSource '*') -Destination $installedTemplatesPath -Recurse -Force
    }
}

Write-Output $editorExecutable.FullName
