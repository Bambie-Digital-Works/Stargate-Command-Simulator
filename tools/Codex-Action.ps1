[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Build', 'Test', 'Verify', 'Run')]
    [string] $Action
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$userDotnetDirectory = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet'
if (Test-Path -LiteralPath (Join-Path $userDotnetDirectory 'dotnet.exe')) {
    $env:PATH = "$userDotnetDirectory;$env:PATH"
}

Push-Location $repositoryRoot

try {
    $dotnet = (Get-Command dotnet -ErrorAction Stop).Source
    $expectedSdk = (Get-Content -Raw -LiteralPath 'global.json' | ConvertFrom-Json).sdk.version
    $actualSdk = (& $dotnet --version).Trim()
    if ($actualSdk -ne $expectedSdk) {
        throw "Expected .NET SDK $expectedSdk but found $actualSdk. Run ./tools/Setup-Codex.ps1 for diagnostics."
    }

    switch ($Action) {
        'Build' {
            & $dotnet restore WormholeWorldsSimulator.csproj --locked-mode
            if ($LASTEXITCODE -ne 0) { throw 'Locked restore failed.' }
            & $dotnet build WormholeWorldsSimulator.csproj --configuration Debug --no-restore
            if ($LASTEXITCODE -ne 0) { throw 'Debug build failed.' }
        }
        'Test' {
            & $dotnet restore tests/WormholeWorlds.Tests.csproj --locked-mode
            if ($LASTEXITCODE -ne 0) { throw 'Locked test restore failed.' }
            & $dotnet test tests/WormholeWorlds.Tests.csproj --configuration Release --no-restore
            if ($LASTEXITCODE -ne 0) { throw 'Automated tests failed.' }
        }
        'Verify' {
            $godot = ./tools/Install-Godot.ps1 -Destination ./.ci-tools/godot
            ./tools/Verify.ps1 -GodotPath $godot -SkipExport
            if ($LASTEXITCODE -ne 0) { throw 'CI-equivalent verification failed.' }
        }
        'Run' {
            & (Join-Path $PSScriptRoot 'Codex-Action.ps1') -Action Build
            if ($LASTEXITCODE -ne 0) { throw 'Build before launch failed.' }
            $godot = ./tools/Install-Godot.ps1 -Destination ./.ci-tools/godot
            Start-Process -FilePath $godot -ArgumentList @('--editor', '--path', $repositoryRoot)
            Write-Host 'Godot editor launched.'
        }
    }
}
finally {
    Pop-Location
}
