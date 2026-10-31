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
.PARAMETER WorkRoot
Parent folder of the temporary work folder; default: the system temp folder.
#>
[CmdletBinding()]
param(
    [string]$Repository = (Split-Path -Parent $PSScriptRoot),
    [Parameter(Mandatory)][string]$Commit,
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [string]$Dotnet = 'dotnet',
    [string]$RuntimeIdentifier = 'linux-arm64',
    [string]$WorkRoot = [IO.Path]::GetTempPath()
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:Work = $null
$script:ExitCode = 0

function Fail([int]$Code, [string]$Line) {
    # Written to the console directly: Fail is also called inside Invoke-Dotnet, whose pipeline output callers discard.
    [Console]::Out.WriteLine($Line)
    $script:ExitCode = $Code
    throw [System.OperationCanceledException]::new('release-build-failed')
}

function Invoke-Dotnet([string[]]$Arguments, [int]$FailCode, [string]$FailLine) {
    # The compiler-runtime pin (cause 5) applies to the calls that produce artifacts; dotnet test runs the tests on the installed runtime,
    # because the test host targets the runtime band (10.0.0) and cannot start with roll-forward disabled.
    if ($Arguments[0] -eq 'test') {
        Remove-Item Env:DOTNET_ROLL_FORWARD -ErrorAction SilentlyContinue
    }
    else {
        $env:DOTNET_ROLL_FORWARD = 'Disable'
    }

    Push-Location -LiteralPath $script:Clone
    try {
        $output = & $Dotnet @Arguments
        $exitCode = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }
    if ($exitCode -ne 0) {
        [Console]::Error.WriteLine("dotnet $($Arguments -join ' ') exited with ${exitCode}:")
        foreach ($outputLine in @($output)) { [Console]::Error.WriteLine([string]$outputLine) }
        Fail $FailCode $FailLine
    }
    return $output
}

try {
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
$script:Work = Join-Path $WorkRoot ("wp-release-work-" + [guid]::NewGuid().ToString('N'))
$script:Clone = Join-Path $script:Work 'src'
git clone -q --no-hardlinks $Repository $script:Clone
if ($LASTEXITCODE -ne 0) {
    Fail 2 "Input error: clone failed."
}
git -C $script:Clone checkout -q $fullCommit
if ($LASTEXITCODE -ne 0) {
    Fail 2 "Input error: checkout failed."
}
# NuGet.Config names the local feed artifacts/local-nuget; a fresh clone does not contain it.
New-Item -ItemType Directory -Force -Path (Join-Path $script:Clone 'artifacts/local-nuget') | Out-Null

# Step 3: Version check
$projectVersion = ([xml](Get-Content -LiteralPath (Join-Path $script:Clone 'src/WeatherParser.Parsing/WeatherParser.Parsing.csproj') -Raw)).SelectNodes('/Project/PropertyGroup/Version') | ForEach-Object { $_.InnerText.Trim() } | Where-Object { $_ } | Select-Object -First 1
if ($projectVersion -ne $Version) {
    Fail 3 "VERSION-MISMATCH project $projectVersion requested $Version"
}

# Step 4: Set environment variables (DOTNET_ROLL_FORWARD is set per call in Invoke-Dotnet)
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
$assetsPath = Join-Path $script:Clone 'examples/WeatherParser.Cli/obj/project.assets.json'
if (-not (Test-Path -LiteralPath $assetsPath -PathType Leaf)) {
    Fail 5 'PACKAGE-MISMATCH the CLI restore assets cannot be read'
}
$assets = Get-Content -LiteralPath $assetsPath -Raw | ConvertFrom-Json -AsHashtable
if (-not ($assets -is [System.Collections.IDictionary] -and $assets.Contains('libraries') -and $assets['libraries'] -is [System.Collections.IDictionary])) {
    Fail 5 'PACKAGE-MISMATCH the CLI restore assets cannot be read'
}
$libraries = @($assets['libraries'].Keys | Where-Object { $_.StartsWith('WeatherParser.Parsing/', [System.StringComparison]::Ordinal) })
if ($libraries.Count -ne 1 -or $libraries[0] -cne "WeatherParser.Parsing/$Version") {
    Fail 5 'PACKAGE-MISMATCH the CLI did not resolve the same-run package'
}
$library = $assets['libraries'][$libraries[0]]
$actual = if ($library -is [System.Collections.IDictionary] -and $library.Contains('sha512')) { [string]$library['sha512'] } else { '' }
if ($actual -cne $expected) {
    Fail 5 'PACKAGE-MISMATCH the CLI did not resolve the same-run package'
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
$stage = Join-Path $script:Work 'stage'
New-Item -ItemType Directory -Force $stage | Out-Null
Copy-Item -LiteralPath $nupkg, $snupkg, $zipPath -Destination $stage

# SHA256SUMS
$artifactNames = [string[]]@("WeatherParser.Parsing.$Version.nupkg", "WeatherParser.Parsing.$Version.snupkg", $zipName)
[Array]::Sort($artifactNames, [StringComparer]::Ordinal)
$sumLines = foreach ($name in $artifactNames) {
    '{0}  {1}' -f (Get-FileHash -LiteralPath (Join-Path $stage $name) -Algorithm SHA256).Hash.ToLowerInvariant(), $name
}
[IO.File]::WriteAllText((Join-Path $stage 'SHA256SUMS'), (($sumLines -join "`n") + "`n"), [Text.UTF8Encoding]::new($false))

# evidence.json
$artifacts = foreach ($name in $artifactNames) {
    [ordered]@{ name = $name; sha256 = (Get-FileHash -LiteralPath (Join-Path $stage $name) -Algorithm SHA256).Hash.ToLowerInvariant() }
}
$evidence = [ordered]@{
    commit = $fullCommit
    tree = $tree
    version = $Version
    sdk = $sdk
    runtimeIdentifier = $RuntimeIdentifier
    dotnetRollForward = 'Disable'
    runId = if ($env:GITHUB_RUN_ID) { [string]$env:GITHUB_RUN_ID } else { $null }
    createdUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ss', [Globalization.CultureInfo]::InvariantCulture)
    artifacts = $artifacts
}
[IO.File]::WriteAllText((Join-Path $stage 'evidence.json'), (ConvertTo-Json -InputObject $evidence -Depth 4), [Text.UTF8Encoding]::new($false))

# Expose the complete set only now: the three artifacts, SHA256SUMS and evidence.json.
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$movedFiles = New-Object System.Collections.Generic.List[string]
try {
    foreach ($stagedName in @($artifactNames) + @('SHA256SUMS', 'evidence.json')) {
        $destination = Join-Path $OutputDirectory $stagedName
        Move-Item -LiteralPath (Join-Path $stage $stagedName) -Destination $destination
        $movedFiles.Add($destination)
    }
}
catch {
    foreach ($file in $movedFiles) {
        Remove-Item -LiteralPath $file -Force -ErrorAction SilentlyContinue
    }
    throw
}

# Step 13: Cleanup and exit
Write-Output "RELEASE-BUILT $Version $fullCommit"
}
catch {
    if ($_.Exception -is [System.OperationCanceledException] -and $_.Exception.Message -eq 'release-build-failed') {
        # Do not overwrite the exit code or write UNEXPECTED for this marker.
    }
    else {
        Write-Output "UNEXPECTED $($_.Exception.Message)"
        $script:ExitCode = 6
    }
}
finally {
    Set-Location -LiteralPath ([IO.Path]::GetTempPath())
    if ($script:Work -and (Test-Path -LiteralPath $script:Work)) {
        Get-ChildItem -LiteralPath $script:Work -Recurse -File -Force | ForEach-Object { $_.Attributes = 'Normal' }
        Remove-Item -LiteralPath $script:Work -Recurse -Force -ErrorAction SilentlyContinue
    }
}
exit $script:ExitCode
