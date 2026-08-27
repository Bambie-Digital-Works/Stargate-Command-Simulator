[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
Push-Location $repositoryRoot

try {
    $expectedSdk = (Get-Content -Raw -LiteralPath 'global.json' | ConvertFrom-Json).sdk.version
    $userDotnetDirectory = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet'
    $userDotnet = Join-Path $userDotnetDirectory 'dotnet.exe'

    $dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($null -eq $dotnetCommand -and (Test-Path -LiteralPath $userDotnet)) {
        $env:PATH = "$userDotnetDirectory;$env:PATH"
        $dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
    }

    if ($null -eq $dotnetCommand) {
        throw "The pinned .NET SDK $expectedSdk is not installed. Install the x64 SDK for the current Windows user from https://dotnet.microsoft.com/download/dotnet/8.0, then restart Codex."
    }

    $actualSdk = (& $dotnetCommand.Source --version).Trim()
    if ($actualSdk -ne $expectedSdk) {
        throw "Expected .NET SDK $expectedSdk from global.json, but '$($dotnetCommand.Source) --version' returned $actualSdk. Install the pinned x64 SDK and ensure it is selected before reopening Codex."
    }

    $godot = ./tools/Install-Godot.ps1 -Destination ./.ci-tools/godot

    $projects = @(
        'WormholeWorldsSimulator.csproj',
        'tools/AssetLedgerValidator/AssetLedgerValidator.csproj',
        'tests/WormholeWorlds.Tests.csproj'
    )

    foreach ($project in $projects) {
        & $dotnetCommand.Source restore $project --locked-mode
        if ($LASTEXITCODE -ne 0) { throw "Locked restore failed for $project." }
    }

    & $dotnetCommand.Source build WormholeWorldsSimulator.csproj --configuration Debug --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Initial Debug build failed.' }

    $godotVersion = (& $godot --version).Trim()
    Write-Host "Codex environment ready: .NET $actualSdk; Godot $godotVersion"
}
finally {
    Pop-Location
}
