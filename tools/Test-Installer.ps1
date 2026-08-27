[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $InstallerPath,

    [Parameter(Mandatory)]
    [string] $IsccPath,

    [string] $AppSource = 'dist/v0.8.0-beta.1/app',

    [string] $TestDirectory = 'dist/installer-matrix'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$installer = (Resolve-Path -LiteralPath (Join-Path $repositoryRoot $InstallerPath)).Path
$iscc = (Resolve-Path -LiteralPath (Join-Path $repositoryRoot $IsccPath)).Path
$app = (Resolve-Path -LiteralPath (Join-Path $repositoryRoot $AppSource)).Path
$testRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot $TestDirectory))
$distRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot 'dist'))
$versionKey = 'HKCU:\Software\Bambie Digital Works\Wormhole Worlds Simulator'
$uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{999D88AD-B8FD-4441-AEDD-0F033B22F5C4}_is1'

if (-not $testRoot.StartsWith($distRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Installer test directory must remain beneath the repository dist directory.'
}
if ((Test-Path -LiteralPath $versionKey) -or (Test-Path -LiteralPath $uninstallKey)) {
    throw 'A Wormhole Worlds Simulator installation already exists. The isolated installer matrix will not replace it.'
}

function Invoke-Installer {
    param(
        [Parameter(Mandatory)][string] $Path,
        [Parameter(Mandatory)][string] $InstallPath,
        [Parameter(Mandatory)][string] $LogPath
    )

    $arguments = @(
        '/VERYSILENT',
        '/SUPPRESSMSGBOXES',
        '/NORESTART',
        '/CURRENTUSER',
        "/DIR=`"$InstallPath`"",
        "/LOG=`"$LogPath`""
    )
    $process = Start-Process -FilePath $Path -ArgumentList $arguments -PassThru
    if (-not $process.WaitForExit(180000)) {
        $process.Kill($true)
        throw "Installer timed out: $Path"
    }
    return $process.ExitCode
}

function Assert-Version {
    param([Parameter(Mandatory)][string] $Expected)

    $actual = (Get-ItemProperty -LiteralPath $versionKey -Name NumericVersion).NumericVersion
    if ($actual -ne $Expected) {
        throw "Expected installed version $Expected but found $actual."
    }
}

function Build-MatrixInstaller {
    param(
        [Parameter(Mandatory)][string] $ProductVersion,
        [Parameter(Mandatory)][string] $NumericVersion,
        [Parameter(Mandatory)][string] $BaseName,
        [Parameter(Mandatory)][string] $OutputPath
    )

    New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null
    & $iscc "/DAppSource=$app" "/DOutputDir=$OutputPath" "/DProductVersion=$ProductVersion" `
        "/DNumericVersion=$NumericVersion" "/DInstallerBaseName=$BaseName" `
        (Join-Path $repositoryRoot 'installer/WormholeWorldsSimulator.iss') | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Failed to compile matrix installer $ProductVersion." }
    return (Join-Path $OutputPath "$BaseName.exe")
}

if (Test-Path -LiteralPath $testRoot) {
    Remove-Item -LiteralPath $testRoot -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $testRoot | Out-Null

$installPath = Join-Path $testRoot 'Install Ω Beta'
$profilePath = Join-Path $testRoot 'profile\AppData\Roaming'
$originalAppData = $env:APPDATA
$env:APPDATA = $profilePath
$oldInstaller = Build-MatrixInstaller '0.7.0-beta.1' '0.7.0.1' 'matrix-old' (Join-Path $testRoot 'old')
$newerInstaller = Build-MatrixInstaller '0.9.0-beta.1' '0.9.0.1' 'matrix-newer' (Join-Path $testRoot 'newer')
$installedByTest = $false

try {
    if ((Invoke-Installer $oldInstaller $installPath (Join-Path $testRoot 'clean-install.log')) -ne 0) {
        throw 'Clean installation failed.'
    }
    $installedByTest = $true
    Assert-Version '0.7.0.1'

    $playerData = Join-Path $profilePath 'Godot\app_userdata\WormholeWorldsSimulator'
    New-Item -ItemType Directory -Force -Path $playerData | Out-Null
    $preservationMarker = Join-Path $playerData 'installer-preservation.marker'
    Set-Content -LiteralPath $preservationMarker -Value 'preserve-me' -Encoding utf8

    $lockedExecutable = Join-Path $installPath 'WormholeWorldsSimulator.exe'
    $lock = [IO.File]::Open($lockedExecutable, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $lockedExitCode = Invoke-Installer $installer $installPath (Join-Path $testRoot 'locked-executable.log')
        if ($lockedExitCode -eq 0) { throw 'Installer unexpectedly replaced a locked executable.' }
        Assert-Version '0.7.0.1'
    }
    finally {
        $lock.Dispose()
    }

    if ((Invoke-Installer $installer $installPath (Join-Path $testRoot 'upgrade.log')) -ne 0) {
        throw 'Older-to-current upgrade failed.'
    }
    Assert-Version '0.8.0.1'
    if (-not (Test-Path -LiteralPath $preservationMarker)) { throw 'Upgrade removed player data.' }

    if ((Invoke-Installer $installer $installPath (Join-Path $testRoot 'repair.log')) -ne 0) {
        throw 'Same-version silent repair failed.'
    }
    Assert-Version '0.8.0.1'

    $bootLog = Join-Path $testRoot 'installed-game.log'
    $game = Start-Process -FilePath (Join-Path $installPath 'WormholeWorldsSimulator.exe') `
        -ArgumentList @('--headless', '--quit-after', '2') -RedirectStandardOutput $bootLog -PassThru
    if (-not $game.WaitForExit(60000)) {
        $game.Kill($true)
        throw 'Installed game boot timed out.'
    }
    if ($game.ExitCode -ne 0) { throw "Installed game boot failed with exit code $($game.ExitCode)." }

    if ((Invoke-Installer $newerInstaller $installPath (Join-Path $testRoot 'newer-install.log')) -ne 0) {
        throw 'Newer-version setup failed.'
    }
    Assert-Version '0.9.0.1'

    $downgradeExitCode = Invoke-Installer $installer $installPath (Join-Path $testRoot 'downgrade-rejection.log')
    if ($downgradeExitCode -eq 0) { throw 'Current installer did not reject a newer installed version.' }
    Assert-Version '0.9.0.1'

    $uninstaller = Join-Path $installPath 'unins000.exe'
    $uninstall = Start-Process -FilePath $uninstaller `
        -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART') -PassThru
    if (-not $uninstall.WaitForExit(120000)) {
        $uninstall.Kill($true)
        throw 'Uninstaller timed out.'
    }
    if ($uninstall.ExitCode -ne 0) { throw "Uninstall failed with exit code $($uninstall.ExitCode)." }
    $installedByTest = $false
    if (Test-Path -LiteralPath (Join-Path $installPath 'WormholeWorldsSimulator.exe')) {
        throw 'Uninstall left the game executable behind.'
    }
    if (-not (Test-Path -LiteralPath $preservationMarker)) { throw 'Uninstall removed player data.' }
    if (Test-Path -LiteralPath $versionKey) { throw 'Uninstall left the product version key behind.' }

    Write-Host 'Installer matrix passed: clean install, locked file, upgrade, repair, boot, downgrade rejection, Unicode path, player-data preservation, and uninstall.'
}
finally {
    $env:APPDATA = $originalAppData
    if ($installedByTest -and (Test-Path -LiteralPath (Join-Path $installPath 'unins000.exe'))) {
        $cleanup = Start-Process -FilePath (Join-Path $installPath 'unins000.exe') `
            -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART') -PassThru -Wait
        if ($cleanup.ExitCode -ne 0) {
            Write-Warning 'Installer matrix cleanup did not complete successfully.'
        }
    }
}
