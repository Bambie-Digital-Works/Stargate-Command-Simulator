[CmdletBinding()]
param(
    [string] $Destination = '.ci-tools/inno-setup'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$destinationPath = [IO.Path]::GetFullPath((Join-Path $repositoryRoot $Destination))
$configuration = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'toolchain-checksums.json') -Raw | ConvertFrom-Json
$inno = $configuration.innoSetup
$compilerPath = Join-Path $destinationPath 'ISCC.exe'
if (Test-Path -LiteralPath $compilerPath) {
    Write-Output $compilerPath
    exit 0
}

$downloadDirectory = Join-Path $repositoryRoot '.ci-tools/downloads'
New-Item -ItemType Directory -Force -Path $downloadDirectory | Out-Null
$installerPath = Join-Path $downloadDirectory $inno.fileName
if (-not (Test-Path -LiteralPath $installerPath)) {
    Invoke-WebRequest -Uri $inno.url -OutFile $installerPath
}

$actualHash = (Get-FileHash -LiteralPath $installerPath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualHash -ne $inno.sha256) {
    throw "Inno Setup checksum mismatch. Expected $($inno.sha256), got $actualHash."
}

New-Item -ItemType Directory -Force -Path $destinationPath | Out-Null
$arguments = @(
    '/VERYSILENT',
    '/SUPPRESSMSGBOXES',
    '/NORESTART',
    '/CURRENTUSER',
    "/DIR=$destinationPath"
)
$process = Start-Process -FilePath $installerPath -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $compilerPath)) {
    throw "Inno Setup installation failed with exit code $($process.ExitCode)."
}

Write-Output $compilerPath
