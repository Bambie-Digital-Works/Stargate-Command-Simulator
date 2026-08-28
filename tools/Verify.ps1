[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $GodotPath,

    [string] $DotnetPath,

    [string] $ExportDirectory = 'build/package',

    [switch] $SkipExport
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$godotExecutable = (Resolve-Path -LiteralPath $GodotPath).Path
$dotnetExecutable = if ([string]::IsNullOrWhiteSpace($DotnetPath)) {
    (Get-Command dotnet -ErrorAction Stop).Source
} else {
    (Resolve-Path -LiteralPath $DotnetPath).Path
}
$expectedSdk = (Get-Content -LiteralPath (Join-Path $repositoryRoot 'global.json') -Raw | ConvertFrom-Json).sdk.version
$actualSdk = (& $dotnetExecutable --version).Trim()
if ($actualSdk -ne $expectedSdk) {
    throw "Expected .NET SDK $expectedSdk but found $actualSdk."
}

$godotVersion = (& $godotExecutable --version).Trim()
if ($godotVersion -notlike '4.7.2.stable.mono*') {
    throw "Expected Godot 4.7.2 stable .NET but found $godotVersion."
}

Push-Location $repositoryRoot
try {
    $projects = @(
        'WormholeWorldsSimulator.csproj',
        'tools/AssetLedgerValidator/AssetLedgerValidator.csproj',
        'tools/ContentValidator/ContentValidator.csproj',
        'tests/WormholeWorlds.Tests.csproj'
    )

    foreach ($project in $projects) {
        & $dotnetExecutable restore $project --locked-mode
        if ($LASTEXITCODE -ne 0) { throw "Locked restore failed for $project." }
        & $dotnetExecutable format $project --verify-no-changes --no-restore
        if ($LASTEXITCODE -ne 0) { throw "Formatting verification failed for $project." }
    }

    & $dotnetExecutable build WormholeWorldsSimulator.csproj --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
    & $dotnetExecutable test tests/WormholeWorlds.Tests.csproj --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Automated tests failed.' }
    & $dotnetExecutable run --project tools/AssetLedgerValidator/AssetLedgerValidator.csproj --configuration Release --no-build -- --root .
    if ($LASTEXITCODE -ne 0) { throw 'Asset ledger validation failed.' }
    & $dotnetExecutable build tools/ContentValidator/ContentValidator.csproj --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Content validator build failed.' }
    & $dotnetExecutable run --project tools/ContentValidator/ContentValidator.csproj --configuration Release --no-build -- --root .
    if ($LASTEXITCODE -ne 0) { throw 'Content validation failed.' }

    & $dotnetExecutable build WormholeWorldsSimulator.csproj --configuration Debug --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Debug build for the headless smoke test failed.' }

    $editorOutput = @(& $godotExecutable --headless --path . --editor --quit 2>&1)
    $editorOutput | Write-Output
    if ($LASTEXITCODE -ne 0 -or ($editorOutput -join "`n") -match 'ERROR:') {
        throw 'Godot editor import reported an error.'
    }

    $bootOutput = @(& $godotExecutable --headless --path . --quit-after 2 2>&1)
    $bootOutput | Write-Output
    if ($LASTEXITCODE -ne 0 -or ($bootOutput -join "`n") -match 'ERROR:' -or ($bootOutput -join "`n") -notmatch 'Boot complete: operator shell is ready\.') {
        throw 'Godot headless boot did not reach the operator shell cleanly.'
    }

    if (-not $SkipExport) {
        $exportPath = if ([IO.Path]::IsPathRooted($ExportDirectory)) {
            [IO.Path]::GetFullPath($ExportDirectory)
        } else {
            [IO.Path]::GetFullPath((Join-Path $repositoryRoot $ExportDirectory))
        }
        New-Item -ItemType Directory -Force -Path $exportPath | Out-Null
        $executablePath = Join-Path $exportPath 'WormholeWorldsSimulator.exe'
        $exportOutput = @(& $godotExecutable --headless --path . --export-release 'Windows Desktop' $executablePath 2>&1)
        $exportOutput | Write-Output
        if ($LASTEXITCODE -ne 0 -or ($exportOutput -join "`n") -match 'ERROR:|completed with warnings' -or -not (Test-Path -LiteralPath $executablePath)) {
            throw 'Windows export failed.'
        }

        $processInfo = [Diagnostics.ProcessStartInfo]::new()
        $processInfo.FileName = $executablePath
        $processInfo.Arguments = '--headless --quit-after 2'
        $processInfo.UseShellExecute = $false
        $processInfo.RedirectStandardOutput = $true
        $processInfo.RedirectStandardError = $true
        $exportedProcess = [Diagnostics.Process]::Start($processInfo)
        $exportedStandardOutput = $exportedProcess.StandardOutput.ReadToEnd()
        $exportedStandardError = $exportedProcess.StandardError.ReadToEnd()
        $exportedProcess.WaitForExit()
        $exportedBootOutput = "$exportedStandardOutput`n$exportedStandardError"
        $exportedBootOutput | Write-Output
        if ($exportedProcess.ExitCode -ne 0 -or $exportedBootOutput -match 'ERROR:|WARNING:' -or $exportedBootOutput -notmatch 'Boot complete: operator shell is ready\.') {
            throw 'Exported Windows application did not reach the operator shell cleanly.'
        }

        [xml] $project = Get-Content -LiteralPath (Join-Path $repositoryRoot 'WormholeWorldsSimulator.csproj') -Raw
        $versionPrefix = $project.SelectSingleNode('//VersionPrefix').InnerText
        $versionSuffix = $project.SelectSingleNode('//VersionSuffix').InnerText
        $productVersion = "$versionPrefix-$versionSuffix"
        $channel = if ([string]::IsNullOrWhiteSpace($env:BuildChannel)) { 'development' } else { $env:BuildChannel }
        $buildId = if ([string]::IsNullOrWhiteSpace($env:BuildId)) { 'local' } else { $env:BuildId }
        $commit = if ([string]::IsNullOrWhiteSpace($env:CommitSha)) { 'unknown' } else { $env:CommitSha }
        $buildUtc = if ([string]::IsNullOrWhiteSpace($env:BuildUtc)) { '1970-01-01T00:00:00Z' } else { $env:BuildUtc }
        $manifest = [ordered]@{
            productVersion = $productVersion
            channel = $channel
            buildId = $buildId
            commitSha = $commit
            buildUtc = $buildUtc
            godotVersion = $godotVersion
            architecture = 'x86_64'
            signed = $false
        }
        $manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $exportPath 'build-manifest.json') -Encoding utf8

        $hashLines = Get-ChildItem -LiteralPath $exportPath -File -Recurse |
            Where-Object Name -ne 'SHA256SUMS.txt' |
            Sort-Object FullName |
            ForEach-Object {
                $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
                $relativePath = $_.FullName.Substring($exportPath.Length).TrimStart('\', '/').Replace('\', '/')
                "$hash  $relativePath"
            }
        $hashLines | Set-Content -LiteralPath (Join-Path $exportPath 'SHA256SUMS.txt') -Encoding ascii

        $safeVersion = $productVersion.Replace('+', '-')
        $artifactName = "Wormhole-Worlds-Simulator-$safeVersion-$channel-build-$buildId-Windows-x64"
        if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_OUTPUT)) {
            "artifact_name=$artifactName" | Add-Content -LiteralPath $env:GITHUB_OUTPUT
            "artifact_path=$exportPath" | Add-Content -LiteralPath $env:GITHUB_OUTPUT
        }

        Write-Output "Windows artifact ready: $artifactName"
    }
}
finally {
    Pop-Location
}
