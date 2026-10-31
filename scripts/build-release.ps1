#Requires -Version 7.0

<#
.SYNOPSIS
Builds the release artifacts of one candidate commit once, without any publication (P9 release contract RL-02 A, RL-03 A, RL-06 A, RL-08 A).

.DESCRIPTION
Exit codes:
  0 Success
  1 Internal error
  2 Input error
  3 Version mismatch
  4 Test failure
  5 Package mismatch
  6 Tool failure

.PARAMETER Repository
The path to the repository root. Defaults to the parent of the script's directory.
.PARAMETER Commit
The commit hash to build.
.PARAMETER Version
The version to build.
.PARAMETER OutputDirectory
The directory where artifacts will be placed.
.PARAMETER Dotnet
The dotnet executable to use. Defaults to 'dotnet'.
.PARAMETER RuntimeIdentifier
The runtime identifier for publishing the CLI. Defaults to 'linux-arm64'.
#>
[CmdletBinding()]
param(
    [string]$Repository = (Split-Path -Parent $PSScriptRoot),
    [Parameter(Mandatory)][string]$Commit,
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [string]$Dotnet = 'dotnet',
    [string]$RuntimeIdentifier = 'linux-arm64'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:Work = $null

function Fail([int]$Code, [string]$Line) {
    Write-Output $Line
    Set-Location -LiteralPath ([IO.Path]::GetTempPath())
    if ($script:Work -and (Test-Path -LiteralPath $script:Work)) {
        Get-ChildItem -LiteralPath $script:Work -Recurse -File -Force | ForEach-Object { $_.Attributes = 'Normal' }
        Remove-Item -LiteralPath $script:Work -Recurse -Force -ErrorAction SilentlyContinue
    }
    exit $Code
}

function Invoke-Dotnet([string[]]$Arguments, [int]$FailCode, [string]$FailLine) {
    Push-Location -LiteralPath $script:Clone
    try {
        $output = & $Dotnet @Arguments
        $exitCode = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }
    if ($exitCode -ne 0) {
        [Console]::Error.WriteLine("dotnet $($Arguments -join ' ') exited with $exitCode:")
        foreach ($outputLine in @($output)) { [Console]::Error.WriteLine([string]$outputLine) }
        Fail $FailCode $FailLine
    }
    return $output
}

# Step 1: Input validation and commit resolution
if (Test-Path -LiteralPath $OutputDirectory) {
    if (@(Get-ChildItem -LiteralPath $OutputDirectory -Force).Count -ne 0) {
        [Console]::Error.WriteLine("Input error: the output folder '$OutputDirectory' is not empty.")
        exit 2
    }
}

$fullCommit = git -C $Repository rev-parse --verify --quiet "$Commit^{commit}"
if (!$fullCommit) {
    Fail 2 "Input error: commit '$Commit' does not exist."
}
$fullCommit = $fullCommit.Trim()
$tree = git -C $Repository rev-parse "$fullCommit^{tree}"
$commitTime = [DateTime]::Parse((git -C $Repository show -s --format=%cI $fullCommit), [Globalization.CultureInfo]::InvariantCulture).ToUniversalTime()

# Step 2: Clone repository
$script:Work = Join-Path ([IO.Path]::GetTempPath()) ("wp-release-work-" + [guid]::NewGuid().ToString('N'))
$script:Clone = Join-Path $script:Work 'src'
git clone -q --no-hardlinks $Repository $script:Clone
if ($LASTEXITCODE -ne 0) {
    Fail 2 "Input error: clone failed."
}
git -C $script:Clone checkout -q $fullCommit
if ($LASTEXITCODE -ne 0) {
    Fail 2 "Input error: checkout failed."
}

# Step 3: Version check
$projectVersion = ([xml](Get-Content -LiteralPath (Join-Path $script:Clone 'src/WeatherParser.Parsing/WeatherParser.Parsing.csproj') -Raw)).SelectNodes('/Project/PropertyGroup/Version') | ForEach-Object { $_.InnerText.Trim() } | Where-Object { $_ } | Select-Object -First 1
if ($projectVersion -ne $Version) {
    Fail 3 "VERSION-MISMATCH project $projectVersion requested $Version"
}

# Step 4: Set environment variables
$env:DOTNET_ROLL_FORWARD = 'Disable'
$env:NUGET_PACKAGES = Join-Path $script:Work 'nuget'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'

# Step 5: Get SDK version
$sdk = (Invoke-Dotnet @('--version') 6 'TOOL-FAILURE dotnet --version' | Select-Object -First 1).ToString().Trim()

# Step 6: Build and test library and tests
$null = Invoke-Dotnet @('restore', 'src/WeatherParser.Parsing', '--locked-mode') 6 'TOOL-FAILURE restore src/WeatherParser.Parsing'
$null = Invoke-Dotnet @('restore', 'tests/WeatherParser.Parsing.Tests', '--locked-mode') 6 'TOOL-FAILURE restore tests/WeatherParser.Parsing.Tests'
$null = Invoke-Dotnet @('build', 'tests/WeatherParser.Parsing.Tests', '-c', 'Release', '--no-restore') 6 'TOOL-FAILURE build tests/WeatherParser.Parsing.Tests'
$null = Invoke-Dotnet @('test', 'tests/WeatherParser.Parsing.Tests', '-c', 'Release', '--no-build') 4 'TEST-FAILURE library tests'

# Step 7: Set input times
$files = @()
$files += Join-Path $script:Clone 'README.md'
$files += Join-Path $script:Clone 'LICENSE'
$files += Join-Path $script:Clone 'schemas/export/v1/weatherparser-export-v1.xsd'
$binDir = Join-Path $script:Clone 'src/WeatherParser.Parsing/bin/Release'
if (Test-Path $binDir) {
    $files += Get-ChildItem -Path $binDir -Recurse -File | ForEach-Object { $_.FullName }
}
foreach ($file in $files) {
    if (Test-Path $file) {
        $item = Get-Item $file
        $item.LastWriteTimeUtc = $commitTime
    }
}

# Step 8: Pack library
$pack = Join-Path $script:Work 'pack'
$null = Invoke-Dotnet @('pack', 'src/WeatherParser.Parsing', '-c', 'Release', '--no-build', '-o', $pack) 6 'TOOL-FAILURE pack src/WeatherParser.Parsing'
$nupkg = Join-Path $pack "WeatherParser.Parsing.$Version.nupkg"
$snupkg = Join-Path $pack "WeatherParser.Parsing.$Version.snupkg"
if (!(Test-Path $nupkg) -or !(Test-Path $snupkg)) {
    Fail 6 'TOOL-FAILURE pack produced no package'
}

# Step 9: Package consumer test
$localNuget = Join-Path $script:Clone 'artifacts/local-nuget'
if (!(Test-Path $localNuget)) {
    New-Item -ItemType Directory -Force $localNuget | Out-Null
}
Copy-Item -Path $nupkg -Destination $localNuget
$null = Invoke-Dotnet @('restore', 'tests/WeatherParser.Package.Tests', '--force-evaluate') 6 'TOOL-FAILURE restore tests/WeatherParser.Package.Tests'
$null = Invoke-Dotnet @('test', 'tests/WeatherParser.Package.Tests', '-c', 'Release', '--no-restore') 4 'TEST-FAILURE package tests'

# Step 10: CLI acceptance artifact
$null = Invoke-Dotnet @('restore', 'examples/WeatherParser.Cli', '--force-evaluate', '-r', $RuntimeIdentifier) 6 'TOOL-FAILURE restore examples/WeatherParser.Cli'
$cli = Join-Path $script:Work 'cli'
$null = Invoke-Dotnet @('publish', 'examples/WeatherParser.Cli', '-c', 'Release', '-r', $RuntimeIdentifier, '--self-contained', 'false', '--no-restore', '-o', $cli) 6 'TOOL-FAILURE publish examples/WeatherParser.Cli'

# Same-run check
$expected = [Convert]::ToBase64String([Security.Cryptography.SHA512]::HashData([IO.File]::ReadAllBytes($nupkg)))
$restoredFile = Join-Path $env:NUGET_PACKAGES "weatherparser.parsing/$($Version.ToLowerInvariant())/weatherparser.parsing.$($Version.ToLowerInvariant()).nupkg.sha512"
if (!(Test-Path $restoredFile) -or (Get-Content $restoredFile).Trim() -ne $expected) {
    Fail 5 'PACKAGE-MISMATCH the CLI did not restore the same-run package'
}

# Step 11: Create CLI archive
$zipName = "WeatherParser.Cli.$Version.$RuntimeIdentifier.zip"
$zipPath = Join-Path $script:Work $zipName
Add-Type -AssemblyName System.IO.Compression.FileSystem
$files = Get-ChildItem -LiteralPath $cli -Recurse -File |
    ForEach-Object { [pscustomobject]@{ Name = [IO.Path]::GetRelativePath($cli, $_.FullName).Replace('\', '/'); Path = $_.FullName } }
$files = [System.Linq.Enumerable]::OrderBy([object[]]$files, [Func[object, string]] { param($f) $f.Name }, [StringComparer]::Ordinal)
$zip = [IO.Compression.ZipFile]::Open($zipPath, 'Create')
try {
    foreach ($file in $files) {
        $entry = $zip.CreateEntry($file.Name)
        $entry.LastWriteTime = [DateTimeOffset]$commitTime
        $target = $entry.Open()
        $source = [IO.File]::OpenRead($file.Path)
        try { $source.CopyTo($target) } finally { $source.Dispose(); $target.Dispose() }
    }
}
finally {
    $zip.Dispose()
}

# Step 12: Output artifacts
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
Copy-Item -Path $nupkg, $snupkg, $zipPath -Destination $OutputDirectory

# SHA256SUMS
$artifactNames = [string[]]@("WeatherParser.Parsing.$Version.nupkg", "WeatherParser.Parsing.$Version.snupkg", $zipName)
[Array]::Sort($artifactNames, [StringComparer]::Ordinal)
$sumLines = foreach ($name in $artifactNames) {
    '{0}  {1}' -f (Get-FileHash -LiteralPath (Join-Path $OutputDirectory $name) -Algorithm SHA256).Hash.ToLowerInvariant(), $name
}
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'SHA256SUMS'), (($sumLines -join "`n") + "`n"), [Text.UTF8Encoding]::new($false))

# evidence.json
$artifacts = foreach ($name in $artifactNames) {
    @{ name = $name; sha256 = (Get-FileHash -LiteralPath (Join-Path $OutputDirectory $name) -Algorithm SHA256).Hash.ToLowerInvariant() }
}
$evidence = @{
    commit = $fullCommit
    tree = $tree
    version = $Version
    sdk = $sdk
    runtimeIdentifier = $RuntimeIdentifier
    dotnetRollForward = 'Disable'
    runId = if ($env:GITHUB_RUN_ID) { [int]$env:GITHUB_RUN_ID } else { $null }
    createdUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ss', [Globalization.CultureInfo]::InvariantCulture)
    artifacts = $artifacts
}
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'evidence.json'), (ConvertTo-Json -InputObject $evidence -Depth 4), [Text.UTF8Encoding]::new($false))

# Step 13: Cleanup and exit
if ($script:Work -and (Test-Path -LiteralPath $script:Work)) {
    Get-ChildItem -LiteralPath $script:Work -Recurse -File -Force | ForEach-Object { $_.Attributes = 'Normal' }
    Remove-Item -LiteralPath $script:Work -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Output "RELEASE-BUILT $Version $fullCommit"
exit 0
