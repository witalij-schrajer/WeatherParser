#Requires -Version 7.0
<#
.SYNOPSIS
Local CI script for WeatherParser repository.
.DESCRIPTION
Runs the complete CI sequence locally from a fresh clone of a given commit,
producing all artifacts in one run directory, and fails with a stage-specific exit code.
.PARAMETER Commit
Commit to verify (default: HEAD of the original repository).
.PARAMETER TargetRef
Comparison target for the baseline and the changed-file list (required).
.PARAMETER BaselinePath
External baseline JSON; skips the baseline pass.
.PARAMETER Bootstrap
Explicit initial bootstrap: no baseline, minimum rule only.
.PARAMETER CollectOnly
Run every stage except coverage conversion, baseline writing and gate; write the resume package and exit 0 with the gate pending.
.PARAMETER KeepWorkDir
Keep clones and the isolated NuGet folder.
.PARAMETER RunSettings
Run settings file for coverage collection; copied once into the run directory and used by both passes.
.PARAMETER FaultInjection
Injects one fault inside the clone or the run directory.
.PARAMETER ResumeFrom
Run directory of a -CollectOnly run: verify its resume package, convert both raw reports with the pinned tool, write the baseline and run the gate.
.PARAMETER PullRequestHeadRepository
Head repository of the pull request as owner/name (REQ-086 amendment 2); together with -PullRequestBranch it selects the recorded baseline reconciliations of coverage-reconciliation.json in the commit under test. Without both, no reconciliation applies.
.PARAMETER PullRequestBranch
Head branch of the pull request (REQ-086 amendment 2); see -PullRequestHeadRepository.
.EXAMPLE
ci-local.ps1 -Commit 2f8c670 -TargetRef HEAD
.EXAMPLE
ci-local.ps1 -Commit 2f8c670 -TargetRef HEAD -Bootstrap
#>
[CmdletBinding()]
param(
    [string]$Commit = 'HEAD',
    [string]$TargetRef,
    [string]$BaselinePath,
    [string]$RunSettings,
    [switch]$Bootstrap,
    [switch]$CollectOnly,
    [switch]$KeepWorkDir,
    [ValidateSet('SdkMismatch','LockDrift','FailingTest','MissingCoverage','ConsumerLockDrift','IncomparableTarget','GateFailure','RunSettingsTamper','InvalidReconciliation')]
    [string]$FaultInjection,
    [string]$ResumeFrom,
    [string]$PullRequestHeadRepository,
    [string]$PullRequestBranch
)

$script:BoundParameterNames = @($PSBoundParameters.Keys)

class CiFailure : System.Exception {
    [int]$ExitCode;
    CiFailure([int]$code,[string]$msg) : base($msg) { $this.ExitCode = $code }
}

function New-CiDirectory([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Get-FileSha256([string]$Path) {
    $bytes = [IO.File]::ReadAllBytes($Path)
    $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
    return [BitConverter]::ToString($hash).Replace('-', '').ToLowerInvariant()
}

function Get-ScriptSha256([string]$Path) {
    # line endings are normalized because Git may check the same commit out with CRLF on Windows and LF on Linux
    $content = [IO.File]::ReadAllText($Path)
    $normalizedContent = $content -replace "`r`n", "`n"
    $bytes = [Text.Encoding]::UTF8.GetBytes($normalizedContent)
    $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
    return [BitConverter]::ToString($hash).Replace('-', '').ToLowerInvariant()
}

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
Set-StrictMode -Version Latest

# Set required environment variables
$env:DOTNET_CLI_UI_LANGUAGE='en'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:TESTINGPLATFORM_TELEMETRY_OPTOUT='1'
$env:DOTNET_NOLOGO='1'

$script:CurrentStageCode = 0
$script:StartTime = [DateTime]::UtcNow

# Initialize these before main try block to avoid errors in catch
$script:RunDir = $null
$script:WorkDir = $null
$exit = 20

function Write-CiLog([string]$Message) {
    $timestamp = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
    $logLine = "$timestamp $Message"
    [Console]::Error.WriteLine($logLine)
    if ($script:RunDir) {
        $logPath = Join-Path $script:RunDir 'ci.log'
        Add-Content -Path $logPath -Value $logLine
    }
}

function Fail([int]$Code, [string]$Message) {
    Write-CiLog "FAIL: $Message"
    throw [CiFailure]::new($Code, $Message)
}

function Invoke-Native(
    [int]$Stage,
    [string]$FilePath,
    [string[]]$Arguments,
    [string]$WorkingDirectory = $script:WorkDir,
    [int[]]$AllowedExitCodes = @(0),
    [string]$CaptureStdOut = $null,
    [switch]$AllowAnyExitCode
) {
    $script:CurrentStageCode = $Stage
    $cmdLine = "$FilePath $($Arguments -join ' ')"
    Write-CiLog "EXEC: $cmdLine"
    if ($WorkingDirectory) {
        Push-Location $WorkingDirectory
    }
    try {
        $errFile = [IO.Path]::GetTempFileName()
        try {
            $stdoutLines = @(& $FilePath @Arguments 2> $errFile)
            $exitCode = $LASTEXITCODE
            $stderrLines = @(Get-Content -LiteralPath $errFile)
            
            if ($CaptureStdOut) {
                $stdoutLines | Out-File -FilePath $CaptureStdOut -Encoding utf8NoBOM
            }
            
            $logPath = Join-Path $script:RunDir 'ci.log'
            $stdoutLines | ForEach-Object { Add-Content -Path $logPath -Value "$_" }
            $stderrLines | ForEach-Object { Add-Content -Path $logPath -Value "$_" }
            $stderrLines | ForEach-Object { [Console]::Error.WriteLine($_) }
            
            if (!$AllowAnyExitCode -and $AllowedExitCodes -notcontains $exitCode) {
                # Find error lines for actionable message
                $errorLines = @()
                $allOutput = $stdoutLines + $stderrLines
                foreach ($line in $allOutput) {
                    if ($line -match 'error' -and $errorLines.Count -lt 3) {
                        $errorLines += $line.Trim()
                    }
                }
                $errorMsg = "failed with exit code $exitCode"
                if ($errorLines.Count -gt 0) {
                    $errorMsg += ": $((($errorLines | ForEach-Object { $_ }) -join ' | '))"
                }
                Fail $Stage "$cmdLine $errorMsg"
            }
            
            return [pscustomobject]@{
                ExitCode = $exitCode
                Output = $stdoutLines
            }
        } finally {
            Remove-Item -LiteralPath $errFile -ErrorAction SilentlyContinue
        }
    } finally {
        if ($WorkingDirectory) {
            Pop-Location
        }
    }
}

function Invoke-RestoreStage([string]$CloneDir, [string]$PassDir, [string]$ProjectScope = 'Full') {
    Write-CiLog "=== RESTORE ==="
    $script:CurrentStageCode = 30
    
    New-CiDirectory $PassDir
    
    $env:NUGET_PACKAGES = Join-Path $script:WorkDir 'nuget-packages'
    # The tool resolver cache lives under DOTNET_CLI_HOME and must be isolated together with the package folder.
    $env:DOTNET_CLI_HOME = Join-Path $script:WorkDir 'cli-home'
    New-CiDirectory $env:DOTNET_CLI_HOME
    
    # Create local package source directory if it doesn't exist
    $localNuGetPath = Join-Path $CloneDir "artifacts/local-nuget"
    if (!(Test-Path $localNuGetPath)) {
        New-CiDirectory $localNuGetPath
        Write-CiLog "Created empty local package source $localNuGetPath (gitignored, absent in fresh clones)"
    }
    
    if ($FaultInjection -eq 'LockDrift' -and $PassDir -eq (Join-Path $script:RunDir 'current')) {
        Write-CiLog "FAULT INJECTION: LockDrift"
        $lockJsonPath = Join-Path $CloneDir "src\WeatherParser.Parsing\packages.lock.json"
        $lockJsonContent = [IO.File]::ReadAllText($lockJsonPath)
        $lockJsonContent = $lockJsonContent -replace '"resolved": "10\.0\.0"', '"resolved": "10.0.1"'
        [IO.File]::WriteAllText($lockJsonPath, $lockJsonContent, [Text.UTF8Encoding]::new($false))
    }

    if ($ProjectScope -eq 'Full') {
        Invoke-Native 30 'dotnet' @('restore', 'WeatherParser.Ci.slnf', '--locked-mode') -WorkingDirectory $CloneDir -AllowedExitCodes @(0)
    } else {
        # Product scope
        Invoke-Native 30 'dotnet' @('restore', 'tests/WeatherParser.Parsing.Tests', '--locked-mode') -WorkingDirectory $CloneDir -AllowedExitCodes @(0)
    }
    
    $status = Invoke-Native 30 'git' @('status', '--porcelain') -WorkingDirectory $CloneDir -AllowedExitCodes @(0)
    if ($status.Output) {
        Fail 30 "Lock-file drift detected after restore"
    }
    Write-CiLog "Restore complete"
}

function Invoke-BuildStage([string]$CloneDir, [string]$PassDir, [string]$ProjectScope = 'Full') {
    Write-CiLog "=== BUILD ==="
    $script:CurrentStageCode = 40
    
    New-CiDirectory $PassDir
    
    if ($FaultInjection -eq 'FailingTest' -and $PassDir -eq (Join-Path $script:RunDir 'current')) {
        Write-CiLog "FAULT INJECTION: FailingTest"
        $testPath = Join-Path $CloneDir "tests\WeatherParser.Parsing.Tests\InjectedFailureTests.cs"
        $testContent = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WeatherParser.Parsing.Tests
{
    [TestClass]
    public class InjectedFailureTests
    {
        [TestMethod]
        public void AssertFail()
        {
            Assert.Fail("Injected failure");
        }
    }
}
"@
        Set-Content -Path $testPath -Value $testContent
    }

    if ($ProjectScope -eq 'Full') {
        $buildOutput = Invoke-Native 40 'dotnet' @('build', 'WeatherParser.Ci.slnf', '-c', 'Release', '--no-restore') -WorkingDirectory $CloneDir -AllowedExitCodes @(0)
    } else {
        # Product scope
        $buildOutput = Invoke-Native 40 'dotnet' @('build', 'tests/WeatherParser.Parsing.Tests', '-c', 'Release', '--no-restore') -WorkingDirectory $CloneDir -AllowedExitCodes @(0)
    }
    
    $warningCount = 0
    $warningMatch = [regex]::Match(($buildOutput.Output -join "`n"), '(\d+) Warning\(s\)')
    if ($warningMatch.Success) { 
        $warningCount = [int]$warningMatch.Groups[1].Value 
    }
    "$warningCount" | Out-File -FilePath (Join-Path $PassDir 'build-warnings.txt') -Encoding utf8NoBOM
    Write-CiLog "Build complete with $warningCount warnings"
}

function Invoke-TestStage([string]$CloneDir, [string]$PassDir, [string]$ProjectScope = 'Full') {
    Write-CiLog "=== TESTS ==="
    $script:CurrentStageCode = 50
    
    New-CiDirectory $PassDir
    
    Assert-RunSettingsUnchanged
    
    $coverageRawDir = Join-Path $PassDir 'coverage/raw'
    New-CiDirectory $coverageRawDir

    $trxDir = Join-Path $PassDir 'trx'
    New-CiDirectory $trxDir

    if ($ProjectScope -eq 'Full') {
        # Run product tests with coverage
        try {
            $testArgs = @('test', 'tests/WeatherParser.Parsing.Tests', '-c', 'Release', '--no-build', '--logger', 'trx;LogFileName=WeatherParser.Parsing.Tests.trx', '--results-directory', $coverageRawDir, '--collect', 'Code Coverage')
            if ($script:RunSettingsCopy) {
                $testArgs += @('--settings', $script:RunSettingsCopy)
            }
            Invoke-Native 50 'dotnet' $testArgs -WorkingDirectory $CloneDir -AllowedExitCodes @(0)
        } finally {
            # Move TRX files from raw to trx directory
            $rawTrxFiles = @(Get-ChildItem -Path $coverageRawDir -Filter "*.trx" -File)
            foreach ($rawTrx in $rawTrxFiles) {
                Move-Item $rawTrx.FullName (Join-Path $trxDir $rawTrx.Name) -Force
            }
        }
        
        # Run coverage tests without coverage
        Invoke-Native 50 'dotnet' @('test', 'tests/WeatherParser.Coverage.Tests', '-c', 'Release', '--no-build', '--logger', 'trx;LogFileName=WeatherParser.Coverage.Tests.trx', '--results-directory', $trxDir) -WorkingDirectory $CloneDir -AllowedExitCodes @(0)
        
        # Run coverage runner tests without coverage
        Invoke-Native 50 'dotnet' @('test', 'tests/WeatherParser.Coverage.Runner.Tests', '-c', 'Release', '--no-build', '--logger', 'trx;LogFileName=WeatherParser.Coverage.Runner.Tests.trx', '--results-directory', $trxDir) -WorkingDirectory $CloneDir -AllowedExitCodes @(0)
        
        # Run schema generation tests without coverage (golden file, P2.09)
        Invoke-Native 50 'dotnet' @('test', 'tests/WeatherParser.SchemaGeneration.Tests', '-c', 'Release', '--no-build', '--logger', 'trx;LogFileName=WeatherParser.SchemaGeneration.Tests.trx', '--results-directory', $trxDir) -WorkingDirectory $CloneDir -AllowedExitCodes @(0)
    } else {
        # Product scope
        try {
            $testArgs = @('test', 'tests/WeatherParser.Parsing.Tests', '-c', 'Release', '--no-build', '--logger', 'trx;LogFileName=WeatherParser.Parsing.Tests.trx', '--results-directory', $coverageRawDir, '--collect', 'Code Coverage')
            if ($script:RunSettingsCopy) {
                $testArgs += @('--settings', $script:RunSettingsCopy)
            }
            Invoke-Native 50 'dotnet' $testArgs -WorkingDirectory $CloneDir -AllowedExitCodes @(0)
        } finally {
            # Move TRX files from raw to trx directory
            $rawTrxFiles = @(Get-ChildItem -Path $coverageRawDir -Filter "*.trx" -File)
            foreach ($rawTrx in $rawTrxFiles) {
                Move-Item $rawTrx.FullName (Join-Path $trxDir $rawTrx.Name) -Force
            }
        }
    }

    # Handle TRX attachment copies
    $guidPattern = '^[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$'
    $allCoverage = @(Get-ChildItem -Path $coverageRawDir -Filter '*.coverage' -Recurse -File)
    $collectorReports = @($allCoverage | Where-Object { $_.Directory.Name -match $guidPattern -and $_.Directory.Parent.FullName -eq (Get-Item $coverageRawDir).FullName })
    $copies = @($allCoverage | Where-Object { $collectorReports.FullName -notcontains $_.FullName })

    foreach ($copy in $copies) {
        $copySha256 = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData([IO.File]::ReadAllBytes($copy.FullName)))
        $isIdentical = $false
        foreach ($report in $collectorReports) {
            $reportSha256 = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData([IO.File]::ReadAllBytes($report.FullName)))
            if ($copySha256 -eq $reportSha256) {
                $isIdentical = $true
                break
            }
        }
        if (!$isIdentical) {
            Fail 80 "Unexpected .coverage file $($copy.FullName) differs from every collector report"
        }
        
        # Find TRX attachment directory (direct child of coverageRawDir)
        $attachmentDir = $copy.Directory
        while ($attachmentDir.Parent.FullName -ne (Get-Item $coverageRawDir).FullName) {
            $attachmentDir = $attachmentDir.Parent
        }
        Remove-Item -Recurse -Force $attachmentDir.FullName
        Write-CiLog "Removed TRX attachment copy $($copy.FullName) (identical to collector report)"
    }

    if ($FaultInjection -eq 'MissingCoverage' -and $PassDir -eq (Join-Path $script:RunDir 'current')) {
        Write-CiLog "FAULT INJECTION: MissingCoverage"
        # Re-read collector reports after cleanup
        $allCoverage = @(Get-ChildItem -Path $coverageRawDir -Filter '*.coverage' -Recurse -File)
        $collectorReports = @($allCoverage | Where-Object { $_.Directory.Name -match $guidPattern -and $_.Directory.Parent.FullName -eq (Get-Item $coverageRawDir).FullName })
        if ($collectorReports.Count -gt 0) {
            Remove-Item $collectorReports[0].FullName
        }
    }

    # Re-read after potential deletion
    $allCoverage = @(Get-ChildItem -Path $coverageRawDir -Filter '*.coverage' -Recurse -File)
    $collectorReports = @($allCoverage | Where-Object { $_.Directory.Name -match $guidPattern -and $_.Directory.Parent.FullName -eq (Get-Item $coverageRawDir).FullName })
    
    if ($collectorReports.Count -ne 1) {
        Fail 80 "Expected exactly one collector .coverage file, found $($collectorReports.Count)"
    }

    # Move TRX files from raw to trx directory and log test counts
    $rawTrxFiles = @(Get-ChildItem -Path $coverageRawDir -Filter "*.trx" -File)
    foreach ($rawTrx in $rawTrxFiles) {
        Move-Item $rawTrx.FullName (Join-Path $trxDir $rawTrx.Name) -Force
    }
    
    $testCounts = @{}
    $allTrxFiles = @(Get-ChildItem -Path $trxDir -Filter "*.trx" -File)
    foreach ($trxFile in $allTrxFiles) {
        try {
            $xml = [xml](Get-Content $trxFile.FullName)
            $counters = $xml.TestRun.ResultSummary.Counters
            if ($counters) {
                $projectName = $trxFile.BaseName
                $testCounts[$projectName] = @{
                    total = [int]$counters.total
                    passed = [int]$counters.passed
                    failed = [int]$counters.failed
                }
                Write-CiLog "${projectName}: total=$($testCounts[$projectName].total) passed=$($testCounts[$projectName].passed) failed=$($testCounts[$projectName].failed)"
            }
        } catch {
            Write-CiLog "Warning: Failed to parse TRX file $($trxFile.FullName): $_"
        }
    }

    $testCounts | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $PassDir 'test-counts.json')

    Write-CiLog "Tests complete"
}

function Invoke-PackStage([string]$CloneDir, [string]$PassDir) {
    Write-CiLog "=== PACK ==="
    $script:CurrentStageCode = 60
    
    New-CiDirectory $PassDir
    
    $version = ([xml](Get-Content (Join-Path $CloneDir "src\WeatherParser.Parsing\WeatherParser.Parsing.csproj"))).SelectNodes('/Project/PropertyGroup/Version') | ForEach-Object { $_.InnerText.Trim() } | Where-Object { $_ } | Select-Object -First 1
    if ($version -is [array]) {
        $version = ($version | Where-Object { $_ -ne '' } | Select-Object -First 1)
    }
    
    $nupkgPath = Join-Path $script:RunDir "WeatherParser.Parsing.$version.nupkg"
    Invoke-Native 60 'dotnet' @('pack', 'src/WeatherParser.Parsing', '-c', 'Release', '--no-build', '-o', $script:RunDir) -WorkingDirectory $CloneDir -AllowedExitCodes @(0)

    if (!(Test-Path $nupkgPath)) {
        Fail 60 "Pack failed, no .nupkg created"
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = $null
    try {
        $zip = [IO.Compression.ZipFile]::OpenRead($nupkgPath)
        $expectedEntries = @(
            'lib/net10.0/WeatherParser.Parsing.dll',
            'lib/net10.0/WeatherParser.Parsing.xml',
            'schemas/export/v1/weatherparser-export-v1.xsd',
            'README.md',
            'LICENSE',
            'WeatherParser.Parsing.nuspec',
            '[Content_Types].xml',
            '_rels/.rels'
        )
        $psmdcpPattern = '^package/services/metadata/core-properties/[^/]+\.psmdcp$'

        $actualEntries = @()
        foreach ($entry in $zip.Entries) {
            $actualEntries += $entry.FullName
        }

        $missingEntries = @()
        $unexpectedEntries = @()
        $psmdcpEntries = @()

        foreach ($expectedEntry in $expectedEntries) {
            if ($actualEntries -notcontains $expectedEntry) {
                $missingEntries += $expectedEntry
            }
        }

        foreach ($actualEntry in $actualEntries) {
            if ($actualEntry -match $psmdcpPattern) {
                $psmdcpEntries += $actualEntry
            } elseif ($expectedEntries -notcontains $actualEntry) {
                $unexpectedEntries += $actualEntry
            }
        }

        if ($missingEntries.Count -gt 0) {
            Fail 60 "Missing entries in .nupkg: $($missingEntries -join ', ')"
        }

        if ($unexpectedEntries.Count -gt 0) {
            Fail 60 "Unexpected entries in .nupkg: $($unexpectedEntries -join ', ')"
        }

        if ($psmdcpEntries.Count -ne 1) {
            Fail 60 "Expected exactly one psmdcp entry, found $($psmdcpEntries.Count): $($psmdcpEntries -join ', ')"
        }

        # The symbols package (RL-03 A) is produced next to the package and carries exactly the library PDB besides the NuGet metadata.
        $snupkgPath = Join-Path $script:RunDir "WeatherParser.Parsing.$version.snupkg"
        if (!(Test-Path $snupkgPath)) {
            Fail 60 "Pack failed, no .snupkg created"
        }
        $symbolZip = [IO.Compression.ZipFile]::OpenRead($snupkgPath)
        try {
            $symbolEntries = @($symbolZip.Entries | ForEach-Object { $_.FullName } | Where-Object { $_ -notmatch $psmdcpPattern -and $_ -notin @('WeatherParser.Parsing.nuspec', '[Content_Types].xml', '_rels/.rels') })
        } finally {
            $symbolZip.Dispose()
        }
        if (($symbolEntries -join ',') -ne 'lib/net10.0/WeatherParser.Parsing.pdb') {
            Fail 60 "Unexpected entries in .snupkg: $($symbolEntries -join ', ')"
        }

        $hash = [Convert]::ToBase64String([System.Security.Cryptography.SHA512]::HashData([IO.File]::ReadAllBytes($nupkgPath)))
        $hash | Out-File -FilePath (Join-Path $script:RunDir 'package-hash.txt') -Encoding utf8NoBOM
    } finally {
        if ($zip) {
            $zip.Dispose()
        }
    }

    Write-CiLog "Pack complete"
}

function Invoke-ConsumerStage([string]$CloneDir, [string]$PassDir) {
    Write-CiLog "=== CONSUMER ==="
    $script:CurrentStageCode = 70
    
    New-CiDirectory $PassDir
    
    # Check for ProjectReference in consumer project
    $consumerProjectPath = Join-Path $CloneDir "tests/WeatherParser.Package.Tests/WeatherParser.Package.Tests.csproj"
    $consumerProjectContent = [IO.File]::ReadAllText($consumerProjectPath)
    if ($consumerProjectContent -match '<ProjectReference') {
        Fail 70 "Package consumer must not use a ProjectReference (T-P1-22)"
    }

    $version = ([xml](Get-Content (Join-Path $CloneDir "src\WeatherParser.Parsing\WeatherParser.Parsing.csproj"))).SelectNodes('/Project/PropertyGroup/Version') | ForEach-Object { $_.InnerText.Trim() } | Where-Object { $_ } | Select-Object -First 1
    if ($version -is [array]) {
        $version = ($version | Where-Object { $_ -ne '' } | Select-Object -First 1)
    }
    
    $nupkgPath = Join-Path $script:RunDir "WeatherParser.Parsing.$version.nupkg"
    $localNuGetPath = Join-Path $CloneDir "artifacts/local-nuget"
    New-CiDirectory $localNuGetPath
    Copy-Item $nupkgPath $localNuGetPath

    $lockJsonPath = Join-Path $CloneDir "tests/WeatherParser.Package.Tests/packages.lock.json"
    $lockJsonContent = [IO.File]::ReadAllText($lockJsonPath)
    
    # Targeted text replacement for contentHash
    $contentHash = Get-Content (Join-Path $script:RunDir 'package-hash.txt')
    $contentHash = $contentHash.Trim()
    $pattern = '(?s)("WeatherParser\.Parsing"\s*:\s*\{.*?"contentHash"\s*:\s*")[^"]*(")'
    $lockJsonContent = [regex]::new($pattern).Replace($lockJsonContent, '${1}' + $contentHash + '${2}', 1)

    if ($FaultInjection -eq 'ConsumerLockDrift') {
        Write-CiLog "FAULT INJECTION: ConsumerLockDrift"
        $lockJsonContent = $lockJsonContent -replace '"resolved": "8\.11\.0"', '"resolved": "8.11.1"'
    }

    [IO.File]::WriteAllText($lockJsonPath, $lockJsonContent, [Text.UTF8Encoding]::new($false))

    Invoke-Native 70 'dotnet' @('restore', 'tests/WeatherParser.Package.Tests', '--locked-mode') -WorkingDirectory $CloneDir -AllowedExitCodes @(0)

    # Verify only the lock file was changed
    $numStat = Invoke-Native 70 'git' @('diff', '--numstat') -WorkingDirectory $CloneDir -AllowedExitCodes @(0)
    $numStatLines = @($numStat.Output | Where-Object { "$_".Trim() -ne '' })
    $status = Invoke-Native 70 'git' @('status', '--porcelain') -WorkingDirectory $CloneDir -AllowedExitCodes @(0)
    $statusLines = @($status.Output | Where-Object { "$_".Trim() -ne '' })
    
    # Accept either no change or exactly one line change
    if ($numStatLines.Count -eq 0 -and $statusLines.Count -eq 0) {
        Write-CiLog "Consumer lock file already matches the packed package hash"
    } elseif ($numStatLines.Count -eq 1 -and $numStatLines[0] -eq "1	1	tests/WeatherParser.Package.Tests/packages.lock.json" -and 
              $statusLines.Count -eq 1 -and $statusLines[0] -eq " M tests/WeatherParser.Package.Tests/packages.lock.json") {
        Write-CiLog "Consumer lock file contentHash rewritten (one line)"
    } else {
        Fail 70 "Consumer lock file diff not exactly one line with 1 1"
    }

    # Parse old and new JSON to verify only contentHash changed
    $oldLockJson = (Invoke-Native 70 'git' @('show', 'HEAD:tests/WeatherParser.Package.Tests/packages.lock.json') -WorkingDirectory $CloneDir).Output -join "`n"
    $newLockJson = [IO.File]::ReadAllText($lockJsonPath)
    
    $oldJson = $oldLockJson | ConvertFrom-Json -AsHashtable
    $newJson = $newLockJson | ConvertFrom-Json -AsHashtable

    # Verify only contentHash changed
    $oldContentHash = $oldJson.dependencies.'net10.0'.'WeatherParser.Parsing'.contentHash
    $newContentHash = $newJson.dependencies.'net10.0'.'WeatherParser.Parsing'.contentHash
    
    if ($newContentHash -ne $contentHash) {
        Fail 70 "Consumer lock file contentHash not updated correctly"
    }

    # Set both contentHash fields to empty for comparison
    $oldJson.dependencies.'net10.0'.'WeatherParser.Parsing'.contentHash = ''
    $newJson.dependencies.'net10.0'.'WeatherParser.Parsing'.contentHash = ''

    $oldJsonStr = ConvertTo-Json -Depth 20 -Compress -InputObject $oldJson
    $newJsonStr = ConvertTo-Json -Depth 20 -Compress -InputObject $newJson

    if ($oldJsonStr -ne $newJsonStr) {
        Fail 70 "Consumer lock file differs beyond the WeatherParser.Parsing contentHash"
    }

    # Verify package hash matches
    $expectedSha512 = Get-Content (Join-Path $script:RunDir 'package-hash.txt')
    $sha512Path = Join-Path "$script:WorkDir/nuget-packages" "weatherparser.parsing/$version/weatherparser.parsing.$version.nupkg.sha512"
    if (!(Test-Path $sha512Path)) {
        Fail 70 "Package SHA512 file not found: $sha512Path"
    }
    $actualSha512 = Get-Content $sha512Path
    if ($expectedSha512 -ne $actualSha512) {
        Fail 70 "Package SHA512 mismatch"
    }

    # Build and test consumer (without coverage)
    $trxDir = Join-Path $PassDir 'trx'
    New-CiDirectory $trxDir
    
    Invoke-Native 70 'dotnet' @('build', 'tests/WeatherParser.Package.Tests', '-c', 'Release', '--no-restore') -WorkingDirectory $CloneDir -AllowedExitCodes @(0)
    
    Invoke-Native 70 'dotnet' @('test', 'tests/WeatherParser.Package.Tests', '-c', 'Release', '--no-build', '--logger', 'trx;LogFileName=WeatherParser.Package.Tests.trx', '--results-directory', $trxDir) -WorkingDirectory $CloneDir -AllowedExitCodes @(0)
    
    # Read consumer test results and add to test-counts.json
    $consumerTrxPath = Join-Path $trxDir 'WeatherParser.Package.Tests.trx'
    if (Test-Path $consumerTrxPath) {
        try {
            $xml = [xml](Get-Content $consumerTrxPath)
            $counters = $xml.TestRun.ResultSummary.Counters
            if ($counters) {
                $testCounts = Get-Content (Join-Path $PassDir 'test-counts.json') | ConvertFrom-Json -AsHashtable
                $testCounts['WeatherParser.Package.Tests'] = @{
                    total = [int]$counters.total
                    passed = [int]$counters.passed
                    failed = [int]$counters.failed
                }
                $testCounts | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $PassDir 'test-counts.json')
                Write-CiLog "WeatherParser.Package.Tests: total=$($testCounts['WeatherParser.Package.Tests'].total) passed=$($testCounts['WeatherParser.Package.Tests'].passed) failed=$($testCounts['WeatherParser.Package.Tests'].failed)"
            }
        } catch {
            Write-CiLog "Warning: Failed to parse consumer TRX file $($consumerTrxPath): $_"
        }
    }

    Write-CiLog "Consumer complete"
}

function Write-CollectionSettings([string]$CloneDir, [string]$PassDir, [string]$ConverterToolVersion, [string]$ConverterRid) {
    Write-CiLog "=== COLLECTION SETTINGS ==="
    $script:CurrentStageCode = 80
    
    New-CiDirectory $PassDir
    
    # Get commit, SDK version, Microsoft.CodeCoverage version, RID
    $commit = git -C $CloneDir rev-parse HEAD
    
    $sdkVersion = Invoke-Native 80 'dotnet' @('--version') -WorkingDirectory $CloneDir -AllowedExitCodes @(0,1,145,150,155)
    $sdkVersion = if ($sdkVersion.Output.Count -gt 0) { $sdkVersion.Output[0].Trim() } else { '' }
    
    # Get Microsoft.CodeCoverage version from Directory.Packages.props
    $microsoftCodeCoverageVersion = ([xml](Get-Content (Join-Path $CloneDir "Directory.Packages.props"))).Project.ItemGroup.PackageVersion | Where-Object Include -eq 'Microsoft.CodeCoverage' | Select-Object -ExpandProperty Version
    
    # Get dotnet-tools.json version for manifest version
    $dotnetToolsJson = Get-Content (Join-Path $CloneDir ".config/dotnet-tools.json") | ConvertFrom-Json
    $dotnetCoverageManifestVersion = $dotnetToolsJson.tools.'dotnet-coverage'.version
    
    # Get RID from dotnet --info
    $dotnetInfo = Invoke-Native 80 'dotnet' @('--info') -WorkingDirectory $CloneDir -AllowedExitCodes @(0)
    $ridLine = $dotnetInfo.Output | Where-Object { $_ -like "*RID:*" }
    $rid = $ridLine -split ':' | Select-Object -Last 1
    $rid = $rid.Trim()

    $collectionSettings = @{
        commit = $commit
        sdkVersion = $sdkVersion
        microsoftCodeCoverageVersion = $microsoftCodeCoverageVersion
        dotnetCoverageManifestVersion = $dotnetCoverageManifestVersion
        dotnetCoverageToolVersion = $ConverterToolVersion
        collector = "Code Coverage"
        runSettingsSha256 = $script:RunSettingsSha256
        exclusions = @("*.Tests.dll", "WeatherParser.Coverage*.dll")
        rid = $rid
        configuration = 'Release'
        converterRid = $ConverterRid
    }

    $collectionSettings | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $PassDir 'collection-settings.json')
}

function Invoke-ConversionStage([string]$CloneDir, [string]$PassDir) {
    Write-CiLog "=== CONVERSION ==="
    $script:CurrentStageCode = 80
    
    New-CiDirectory $PassDir
    
    $coverageRawDir = Join-Path $PassDir 'coverage/raw'
    $coverageXmlPath = Join-Path $PassDir 'coverage/coverage.xml'

    New-CiDirectory (Split-Path $coverageXmlPath)

    Invoke-Native 80 'dotnet' @('tool', 'restore') -WorkingDirectory $CloneDir -AllowedExitCodes @(0)
    
    $dotnetCoverageVersion = Invoke-Native 80 'dotnet' @('tool', 'run', 'dotnet-coverage', '--version') -WorkingDirectory $CloneDir -AllowedExitCodes @(0) -CaptureStdOut (Join-Path $PassDir 'dotnet-coverage-version.txt')
    $dotnetCoverageVersion = $dotnetCoverageVersion.Output[0] -split '\+' | Select-Object -First 1

    $coverageFiles = @(Get-ChildItem -Path $coverageRawDir -Filter "*.coverage" -Recurse -File)
    if ($coverageFiles.Count -ne 1) {
        Fail 80 "Expected exactly one .coverage file, found $($coverageFiles.Count)"
    }

    Invoke-Native 80 'dotnet' @('tool', 'run', 'dotnet-coverage', 'merge', $coverageFiles[0].FullName, '-f', 'xml', '-o', $coverageXmlPath) -WorkingDirectory $CloneDir -AllowedExitCodes @(0)

    if ($FaultInjection -eq 'GateFailure' -and $PassDir -eq (Join-Path $script:RunDir 'current')) {
        Write-CiLog "FAULT INJECTION: GateFailure"
        $fixturePath = Join-Path $CloneDir "tests/WeatherParser.Coverage.Tests/Fixtures/report-real-p1-07.xml"
        Copy-Item $fixturePath $coverageXmlPath
    }

    Write-CollectionSettings $CloneDir $PassDir $dotnetCoverageVersion ([System.Runtime.InteropServices.RuntimeInformation]::RuntimeIdentifier)

    Write-CiLog "Conversion complete"
}

function Assert-RunSettingsUnchanged() {
    if ($script:RunSettingsCopy) {
        $runSettingsBytes = [IO.File]::ReadAllBytes($script:RunSettingsCopy)
        $runSettingsSha256 = [System.Security.Cryptography.SHA256]::HashData($runSettingsBytes)
        $actualSha256 = [BitConverter]::ToString($runSettingsSha256).Replace('-', '').ToLowerInvariant()
        if ($actualSha256 -ne $script:RunSettingsSha256) {
            Fail 85 "Run settings copy changed: expected SHA-256 $($script:RunSettingsSha256), found $actualSha256"
        }
    }
}

function Assert-ComparableSettings([string]$CurrentSettingsPath, [string]$BaselineSettingsPath) {
    $currentSettings = Get-Content $CurrentSettingsPath | ConvertFrom-Json
    $baselineSettings = Get-Content $BaselineSettingsPath | ConvertFrom-Json

    # Get all field names from both objects (excluding commit)
    $currentFields = @($currentSettings.PSObject.Properties.Name | Where-Object { $_ -ne 'commit' })
    $baselineFields = @($baselineSettings.PSObject.Properties.Name | Where-Object { $_ -ne 'commit' })
    $allFields = @($currentFields + $baselineFields) | Sort-Object -Unique

    foreach ($field in $allFields) {
        $currentProp = $currentSettings.PSObject.Properties[$field]
        $currentValue = if ($currentProp) { $currentProp.Value } else { $null }
        
        $baselineProp = $baselineSettings.PSObject.Properties[$field]
        $baselineValue = if ($baselineProp) { $baselineProp.Value } else { $null }
        
        # Convert arrays to comma-separated strings for comparison
        $currentStr = if ($currentValue -is [array]) { @($currentValue) -join ',' } else { "$currentValue" }
        $baselineStr = if ($baselineValue -is [array]) { @($baselineValue) -join ',' } else { "$baselineValue" }
        
        if ($currentStr -ne $baselineStr) {
            Fail 85 "Incomparable measurement: field '$field' differs (current: '$currentStr', baseline: '$baselineStr')"
        }
    }
}

function Invoke-BaselinePass() {
    Write-CiLog "=== BASELINE PASS ==="
    $script:CurrentStageCode = 85
    if ($Bootstrap -or $BaselinePath) {
        Write-CiLog "Skipping baseline pass"
        return
    }

    $targetDir = Join-Path $script:WorkDir 'target'
    Invoke-Native 20 'git' @('clone', '--no-local', '--quiet', $script:RepoRoot, "$targetDir") -WorkingDirectory $script:RepoRoot -AllowedExitCodes @(0)
    Invoke-Native 20 'git' @('checkout', '--detach', '--quiet', $script:TargetSha) -WorkingDirectory $targetDir -AllowedExitCodes @(0)

    # Check if target commit has the required tool manifest
    if (!(Test-Path (Join-Path $targetDir ".config/dotnet-tools.json"))) {
        Fail 91 "Target commit $($script:TargetSha) lacks the pinned dotnet-coverage tool manifest; it cannot produce a comparable baseline."
    }

    if ($FaultInjection -eq 'RunSettingsTamper') {
        Write-CiLog "FAULT INJECTION: RunSettingsTamper"
        Add-Content -LiteralPath $script:RunSettingsCopy -Value "<!-- RunSettingsTamper fault injection -->"
    }

    # Reuse the pass functions for target
    Invoke-RestoreStage -CloneDir $targetDir -PassDir (Join-Path $script:RunDir 'target') -ProjectScope 'Product'
    Invoke-BuildStage -CloneDir $targetDir -PassDir (Join-Path $script:RunDir 'target') -ProjectScope 'Product'
    Invoke-TestStage -CloneDir $targetDir -PassDir (Join-Path $script:RunDir 'target') -ProjectScope 'Product'
    
    if ($CollectOnly) {
        Write-CollectionSettings $targetDir (Join-Path $script:RunDir 'target') 'not-run' 'not-run'
        Assert-ComparableSettings (Join-Path $script:RunDir 'current/collection-settings.json') (Join-Path $script:RunDir 'target/collection-settings.json')
        return
    } else {
        Invoke-ConversionStage -CloneDir $targetDir -PassDir (Join-Path $script:RunDir 'target')

        $currentSettings = Get-Content (Join-Path $script:RunDir 'current/collection-settings.json') | ConvertFrom-Json
        $targetSettings = Get-Content (Join-Path $script:RunDir 'target/collection-settings.json') | ConvertFrom-Json

        if ($FaultInjection -eq 'IncomparableTarget') {
            Write-CiLog "FAULT INJECTION: IncomparableTarget"
            $targetSettings.dotnetCoverageToolVersion = "0.0.0-injected"
            $targetSettings | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $script:RunDir 'target/collection-settings.json')
        }

        Assert-ComparableSettings (Join-Path $script:RunDir 'current/collection-settings.json') (Join-Path $script:RunDir 'target/collection-settings.json')

        # Write baseline using the current clone's runner
        $baselinePath = Join-Path $script:RunDir 'target/baseline.json'
        Invoke-Native 91 'dotnet' @('run', '--project', "$script:WorkDir/src/tools/WeatherParser.Coverage.Runner", '-c', 'Release', '--no-build', '--', 'write-baseline', '--report', (Join-Path $script:RunDir 'target/coverage/coverage.xml'), '--commit', $script:TargetSha, '--tool-name', 'Microsoft.CodeCoverage', '--tool-version', $targetSettings.microsoftCodeCoverageVersion, '--exclude', '*.Tests.dll', '--exclude', 'WeatherParser.Coverage*.dll', '--output', $baselinePath) -WorkingDirectory "$script:WorkDir/src" -AllowedExitCodes @(0)
        if ($script:CliPresent -and (Test-CliPresent $targetDir)) {
            Invoke-CliTargetStage -TargetDir $targetDir -ToolVersion $targetSettings.microsoftCodeCoverageVersion
        }
    }

    Write-CiLog "Baseline pass complete"
}

function Write-ChangedFiles() {
    Write-CiLog "=== CHANGED FILES ==="
    $script:CurrentStageCode = 90
    
    $changedFiles = Join-Path $script:RunDir 'changed-files.txt'
    Invoke-Native 90 'git' @('diff', '--name-only', '--no-renames', "$($script:TargetSha)...$($script:CommitSha)") -WorkingDirectory "$script:WorkDir/src" -AllowedExitCodes @(0) -CaptureStdOut $changedFiles
}

function New-ResumePackage() {
    Write-CiLog "=== RESUME PACKAGE ==="
    $script:CurrentStageCode = 80
    
    $resumeDir = Join-Path $script:RunDir 'resume'
    New-CiDirectory $resumeDir
    
    # Publish runner
    $runnerDir = Join-Path $resumeDir 'runner'
    New-CiDirectory $runnerDir
    Invoke-Native 80 'dotnet' @('publish', 'tools/WeatherParser.Coverage.Runner', '-c', 'Release', '--no-restore', '--no-self-contained', '-o', $runnerDir) -WorkingDirectory "$script:WorkDir/src" -AllowedExitCodes @(0)
    
    # Copy dotnet-tools.json
    Copy-Item (Join-Path "$script:WorkDir/src" ".config/dotnet-tools.json") (Join-Path $resumeDir 'dotnet-tools.json')
    
    # Create resume.json
    $dotnetToolsSha256 = Get-FileSha256 (Join-Path $resumeDir 'dotnet-tools.json')
    
    $runnerFiles = [ordered]@{}
    $runnerEntries = @(Get-ChildItem -Path $runnerDir -Recurse -File | ForEach-Object {
        [pscustomobject]@{ RelativePath = $_.FullName.Substring($runnerDir.Length + 1).Replace('\', '/'); FullName = $_.FullName }
    } | Sort-Object -Property RelativePath)
    foreach ($entry in $runnerEntries) {
        $runnerFiles[$entry.RelativePath] = Get-FileSha256 $entry.FullName
    }
    
    $collectionSettings = Get-Content (Join-Path $script:RunDir 'current/collection-settings.json') | ConvertFrom-Json
    
    # Compute input hashes for resume verification
    $changedFilesSha256 = Get-FileSha256 (Join-Path $script:RunDir 'changed-files.txt')
    
    $rawReports = [ordered]@{}
    foreach ($pass in @('current', 'target')) {
        $rawDir = Join-Path $script:RunDir "$pass/coverage/raw"
        if (Test-Path $rawDir) {
            $passReports = @(Get-ChildItem -Path $rawDir -Filter "*.coverage" -Recurse -File | Sort-Object FullName)
            foreach ($report in $passReports) {
                $relativePath = "$pass/coverage/raw/" + $report.FullName.Substring($rawDir.Length + 1).Replace('\', '/')
                $rawReports[$relativePath] = Get-FileSha256 $report.FullName
            }
        }
    }
    
    $resumeJson = @{
        commit = $script:CommitSha
        targetCommit = $script:TargetSha
        mode = if ($Bootstrap) { "bootstrap" } elseif ($BaselinePath) { "baseline-path" } else { "two-pass" }
        collectOnly = [bool]$CollectOnly
        scriptSha256 = Get-ScriptSha256 $PSCommandPath
        dotnetToolsSha256 = $dotnetToolsSha256
        runnerFiles = $runnerFiles
        toolName = 'Microsoft.CodeCoverage'
        exclusions = @('*.Tests.dll','WeatherParser.Coverage*.dll')
        microsoftCodeCoverageVersion = $collectionSettings.microsoftCodeCoverageVersion
        runSettingsSha256 = $script:RunSettingsSha256
        changedFiles = 'changed-files.txt'
        changedFilesSha256 = $changedFilesSha256
        rawReports = $rawReports
        createdUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
    }
    
    $resumeJson | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $resumeDir 'resume.json')
    
    Write-CiLog "Resume package written: $($runnerFiles.Count) runner files"
}

function Invoke-GateStage() {
    Write-CiLog "=== GATE ==="
    $script:CurrentStageCode = 90

    if ($FaultInjection -eq 'InvalidReconciliation') {
        # Verification only: a malformed reconciliation file in the commit under test must fail the gate as an input error (exit code 91).
        Write-CiLog "FAULT INJECTION: InvalidReconciliation"
        Set-Content -LiteralPath (Join-Path "$script:WorkDir/src" 'coverage-reconciliation.json') -Value '{ not json'
    }
    
    $changedFiles = Join-Path $script:RunDir 'changed-files.txt'
    
    $coverageXmlPath = Join-Path $script:RunDir 'current/coverage/coverage.xml'

    $microsoftCodeCoverageVersion = ([xml](Get-Content (Join-Path "$script:WorkDir/src" "Directory.Packages.props"))).Project.ItemGroup.PackageVersion | Where-Object Include -eq 'Microsoft.CodeCoverage' | Select-Object -ExpandProperty Version

    if ($Bootstrap) {
        Write-CiLog "Running bootstrap gate"
        $gate = Invoke-Native 90 'dotnet' (@('run', '--project', "$script:WorkDir/src/tools/WeatherParser.Coverage.Runner", '-c', 'Release', '--no-build', '--', 'evaluate', '--report', $coverageXmlPath, '--changed-files', $changedFiles, '--tool-name', 'Microsoft.CodeCoverage', '--tool-version', $microsoftCodeCoverageVersion, '--exclude', '*.Tests.dll', '--exclude', 'WeatherParser.Coverage*.dll', '--bootstrap') + (Get-GateArguments 'library')) -WorkingDirectory "$script:WorkDir/src" -AllowedExitCodes @(0, 1, 2) -CaptureStdOut (Join-Path $script:RunDir 'gate-result.json')
        switch ($gate.ExitCode) {
            1 {
                $gateResult = Get-Content (Join-Path $script:RunDir 'gate-result.json') | ConvertFrom-Json
                $failedRules = $gateResult.rules | Where-Object { $_.verdict -eq $false } | ForEach-Object { $_.ruleId }
                Fail 90 "Coverage gate failed: $($failedRules -join ', ')"
            }
            2 {
                Fail 91 "Coverage gate input error"
            }
        }
    } elseif ($BaselinePath) {
        Write-CiLog "Running gate with external baseline"
        
        # Validate external baseline settings
        $baselineDir = Join-Path $script:RunDir 'baseline-input'
        New-CiDirectory $baselineDir
        
        $copiedBaseline = Join-Path $baselineDir ([IO.Path]::GetFileName($BaselinePath))
        $copiedSettings = Join-Path $baselineDir 'collection-settings.json'
        
        Copy-Item $BaselinePath $copiedBaseline
        Copy-Item $script:BaselineSettingsPath $copiedSettings
        
        Assert-ComparableSettings (Join-Path $script:RunDir 'current/collection-settings.json') $copiedSettings
        
        # Get SHA-256 of the baseline file
        $baselineSha256 = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData([IO.File]::ReadAllBytes($copiedBaseline)))
        Write-CiLog "External baseline SHA-256: $baselineSha256"
        
        # Get SHA-256 of the settings file
        $settingsSha256 = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData([IO.File]::ReadAllBytes($copiedSettings)))
        Write-CiLog "External baseline settings SHA-256: $settingsSha256"
        
        $gate = Invoke-Native 90 'dotnet' (@('run', '--project', "$script:WorkDir/src/tools/WeatherParser.Coverage.Runner", '-c', 'Release', '--no-build', '--', 'evaluate', '--report', $coverageXmlPath, '--changed-files', $changedFiles, '--tool-name', 'Microsoft.CodeCoverage', '--tool-version', $microsoftCodeCoverageVersion, '--exclude', '*.Tests.dll', '--exclude', 'WeatherParser.Coverage*.dll', '--baseline', $copiedBaseline, '--expected-baseline-commit', $script:TargetSha) + (Get-GateArguments 'library')) -WorkingDirectory "$script:WorkDir/src" -AllowedExitCodes @(0, 1, 2) -CaptureStdOut (Join-Path $script:RunDir 'gate-result.json')
        switch ($gate.ExitCode) {
            1 {
                $gateResult = Get-Content (Join-Path $script:RunDir 'gate-result.json') | ConvertFrom-Json
                $failedRules = $gateResult.rules | Where-Object { $_.verdict -eq $false } | ForEach-Object { $_.ruleId }
                Fail 90 "Coverage gate failed: $($failedRules -join ', ')"
            }
            2 {
                Fail 91 "Coverage gate input error"
            }
        }
    } else {
        Write-CiLog "Running gate with two-pass baseline"
        $gate = Invoke-Native 90 'dotnet' (@('run', '--project', "$script:WorkDir/src/tools/WeatherParser.Coverage.Runner", '-c', 'Release', '--no-build', '--', 'evaluate', '--report', $coverageXmlPath, '--changed-files', $changedFiles, '--tool-name', 'Microsoft.CodeCoverage', '--tool-version', $microsoftCodeCoverageVersion, '--exclude', '*.Tests.dll', '--exclude', 'WeatherParser.Coverage*.dll', '--baseline', (Join-Path $script:RunDir 'target/baseline.json'), '--expected-baseline-commit', $script:TargetSha) + (Get-GateArguments 'library')) -WorkingDirectory "$script:WorkDir/src" -AllowedExitCodes @(0, 1, 2) -CaptureStdOut (Join-Path $script:RunDir 'gate-result.json')
        switch ($gate.ExitCode) {
            1 {
                $gateResult = Get-Content (Join-Path $script:RunDir 'gate-result.json') | ConvertFrom-Json
                $failedRules = $gateResult.rules | Where-Object { $_.verdict -eq $false } | ForEach-Object { $_.ruleId }
                Fail 90 "Coverage gate failed: $($failedRules -join ', ')"
            }
            2 {
                Fail 91 "Coverage gate input error"
            }
        }
    }

    if ($Bootstrap -and !$FaultInjection) {
        Write-CiLog "Writing bootstrap baseline"
        Invoke-Native 91 'dotnet' @('run', '--project', "$script:WorkDir/src/tools/WeatherParser.Coverage.Runner", '-c', 'Release', '--no-build', '--', 'write-baseline', '--report', (Join-Path $script:RunDir 'current/coverage/coverage.xml'), '--commit', $script:CommitSha, '--tool-name', 'Microsoft.CodeCoverage', '--tool-version', $microsoftCodeCoverageVersion, '--exclude', '*.Tests.dll', '--exclude', 'WeatherParser.Coverage*.dll', '--output', (Join-Path $script:RunDir 'current/bootstrap-baseline.json')) -WorkingDirectory "$script:WorkDir/src" -AllowedExitCodes @(0)

        if (!(Test-Path (Join-Path $script:RunDir 'current/bootstrap-baseline.json'))) {
            Fail 91 "Failed to write bootstrap baseline"
        }

        $baseline = Get-Content (Join-Path $script:RunDir 'current/bootstrap-baseline.json') | ConvertFrom-Json
        if ($null -eq $baseline.commitId) {
            Fail 91 "Bootstrap baseline missing commitId"
        }
        
        # Verify the baseline matches expected values and commit ID
        $gateResult = Get-Content (Join-Path $script:RunDir 'gate-result.json') | ConvertFrom-Json
        if ($baseline.blocksCovered -ne $gateResult.current.covered -or $baseline.blocksNotCovered -ne $gateResult.current.notCovered) {
            Fail 91 "Bootstrap baseline values do not match gate result"
        }
        
        if ($baseline.commitId -ne $script:CommitSha) {
            Fail 91 "Bootstrap baseline commit ID does not match expected commit"
        }
    }

    if ($script:CliPresent) {
        Invoke-CliGate -ToolVersion $microsoftCodeCoverageVersion
    }

    Write-CiLog "Gate complete"
}

function Invoke-FinishStage() {
    Write-CiLog "=== FINISH ==="
    try {
        if ($script:WorkDir -and (Test-Path "$script:WorkDir/src")) {
            $packagesOutput = Invoke-Native 0 'dotnet' @('list', 'WeatherParser.Ci.slnf', 'package', '--include-transitive') -WorkingDirectory "$script:WorkDir/src" -AllowAnyExitCode -CaptureStdOut (Join-Path $script:RunDir 'packages.txt')
            if ($packagesOutput.ExitCode -ne 0) {
                Write-CiLog "Warning: packages.txt not written: dotnet list package exit code $($packagesOutput.ExitCode)"
            }
        }

        $duration = [DateTime]::UtcNow - $script:StartTime
        $durationStr = $duration.ToString('hh\:mm\:ss')
        Write-CiLog "Run completed in $durationStr"

        if ($script:WorkDir -and !$KeepWorkDir) {
            Remove-Item -Recurse -Force $script:WorkDir
            Write-CiLog "Deleted work directory: $script:WorkDir"
        } else {
            Write-CiLog "Kept work directory: $script:WorkDir"
        }
    } catch {
        Write-CiLog "Error in finish stage: $_"
    }
}

function Invoke-ResumeFrom() {
    Write-CiLog "=== RESUME ==="
    
    # Validate forbidden parameters
    $forbiddenParams = @($script:BoundParameterNames | Where-Object { $_ -notin @('ResumeFrom', 'KeepWorkDir') })
    if ($forbiddenParams.Count -gt 0) {
        Fail 20 "-ResumeFrom cannot be combined with: $($forbiddenParams -join ', ')"
    }

    # Validate run directory
    if (!(Test-Path -LiteralPath $ResumeFrom)) {
        Fail 20 "Resume directory does not exist: $ResumeFrom"
    }
    
    $runDirectory = (Resolve-Path -LiteralPath $ResumeFrom).Path
    $resumeDir = Join-Path $runDirectory 'resume'
    if (!(Test-Path -LiteralPath $resumeDir)) {
        Fail 20 "Resume directory missing resume subdirectory: $resumeDir"
    }
    
    $resumeJsonPath = Join-Path $resumeDir 'resume.json'
    if (!(Test-Path -LiteralPath $resumeJsonPath)) {
        Fail 20 "Resume directory missing resume.json: $resumeJsonPath"
    }
    
    $script:RunDir = $runDirectory
    Write-CiLog "Resuming from run directory: $script:RunDir"

    # Read resume.json
    $resumeJson = Get-Content $resumeJsonPath | ConvertFrom-Json -AsHashtable
    
    if ($resumeJson.collectOnly -ne $true) {
        Fail 20 "Run directory was not produced by -CollectOnly"
    }
    
    if ($resumeJson.mode -ne 'two-pass') {
        Fail 20 "Resume is only supported for two-pass mode"
    }

    # Hash checks
    $scriptSha256 = Get-ScriptSha256 $PSCommandPath
    if ($scriptSha256 -ne $resumeJson.scriptSha256) {
        Fail 20 "ci-local.ps1 script hash mismatch: expected $($resumeJson.scriptSha256), found $scriptSha256"
    }

    $dotnetToolsSha256 = Get-FileSha256 (Join-Path $resumeDir 'dotnet-tools.json')
    if ($dotnetToolsSha256 -ne $resumeJson.dotnetToolsSha256) {
        Fail 20 "dotnet-tools.json hash mismatch: expected $($resumeJson.dotnetToolsSha256), found $dotnetToolsSha256"
    }

    # Check runner files
    $runnerDir = Join-Path $resumeDir 'runner'
    $expectedRunnerFiles = @($resumeJson.runnerFiles.Keys | Sort-Object)
    $actualRunnerFiles = @(Get-ChildItem -Path $runnerDir -Recurse -File | ForEach-Object {
        $_.FullName.Substring($runnerDir.Length + 1).Replace('\', '/')
    } | Sort-Object)
    
    if ($expectedRunnerFiles.Count -ne $actualRunnerFiles.Count) {
        Fail 20 "Runner file count mismatch: expected $($expectedRunnerFiles.Count), found $($actualRunnerFiles.Count)"
    }
    
    # Compare expected and actual files
    for ($i = 0; $i -lt $expectedRunnerFiles.Count; $i++) {
        $file = $expectedRunnerFiles[$i]
        if ($actualRunnerFiles[$i] -ne $file) {
            Fail 20 "Runner file mismatch at index ${i}: expected '$file', found '$($actualRunnerFiles[$i])'"
        }
        
        $expectedHash = $resumeJson.runnerFiles[$file]
        $actualPath = Join-Path $runnerDir $file
        if (!(Test-Path -LiteralPath $actualPath)) {
            Fail 20 "Missing runner file: $file"
        }
        $actualHash = Get-FileSha256 $actualPath
        if ($expectedHash -ne $actualHash) {
            Fail 20 "Runner file hash mismatch for ${file}: expected $expectedHash, found $actualHash"
        }
    }

    # Check for extra files in runner directory
    $extraFiles = @($actualRunnerFiles | Where-Object { $expectedRunnerFiles -notcontains $_ })
    if ($extraFiles.Count -gt 0) {
        Fail 20 "Extra runner files: $($extraFiles -join ', ')"
    }

    # Check runsettings if present
    if ($resumeJson.runSettingsSha256 -ne 'none') {
        $runSettingsPath = Join-Path $script:RunDir 'coverage.runsettings'
        if (!(Test-Path -LiteralPath $runSettingsPath)) {
            Fail 20 "Run settings file missing: $runSettingsPath"
        }
        $actualRunSettingsSha256 = Get-FileSha256 $runSettingsPath
        if ($actualRunSettingsSha256 -ne $resumeJson.runSettingsSha256) {
            Fail 20 "Run settings hash mismatch: expected $($resumeJson.runSettingsSha256), found $actualRunSettingsSha256"
        }
    }

    # Check changed-files.txt and the raw coverage reports against the recorded hashes
    $changedFilesPath = Join-Path $script:RunDir 'changed-files.txt'
    if (!(Test-Path -LiteralPath $changedFilesPath)) {
        Fail 20 "Changed files list missing: $changedFilesPath"
    }
    if (!$resumeJson.ContainsKey('changedFilesSha256') -or !$resumeJson.ContainsKey('rawReports')) {
        Fail 20 "Resume package lacks input hashes; collect again with the current script"
    }
    $actualChangedFilesSha256 = Get-FileSha256 $changedFilesPath
    if ($actualChangedFilesSha256 -ne $resumeJson.changedFilesSha256) {
        Fail 20 "changed-files.txt hash mismatch: expected $($resumeJson.changedFilesSha256), found $actualChangedFilesSha256"
    }
    $expectedReports = @($resumeJson.rawReports.Keys | Sort-Object)
    $actualReports = @(foreach ($pass in @('current', 'target')) {
        $rawDir = Join-Path $script:RunDir "$pass/coverage/raw"
        if (Test-Path -LiteralPath $rawDir) {
            Get-ChildItem -Path $rawDir -Filter '*.coverage' -Recurse -File | ForEach-Object {
                "$pass/coverage/raw/" + $_.FullName.Substring($rawDir.Length + 1).Replace('\', '/')
            }
        }
    }) | Sort-Object
    $actualReports = @($actualReports)
    foreach ($missing in @($expectedReports | Where-Object { $actualReports -notcontains $_ })) {
        Fail 20 "Raw coverage report missing: $missing"
    }
    foreach ($extra in @($actualReports | Where-Object { $expectedReports -notcontains $_ })) {
        Fail 20 "Unexpected raw coverage report: $extra"
    }
    foreach ($report in $expectedReports) {
        $actualReportSha256 = Get-FileSha256 (Join-Path $script:RunDir $report)
        if ($actualReportSha256 -ne $resumeJson.rawReports[$report]) {
            Fail 20 "Raw coverage report hash mismatch for ${report}: expected $($resumeJson.rawReports[$report]), found $actualReportSha256"
        }
    }

    Write-CiLog "Resume package verified"

    # Restore pinned tool
    $script:ResumeWorkDir = Join-Path ([IO.Path]::GetTempPath()) "weatherparser-resume-$([guid]::NewGuid())"
    $env:NUGET_PACKAGES = Join-Path $script:ResumeWorkDir 'nuget-packages'
    $env:DOTNET_CLI_HOME = Join-Path $script:ResumeWorkDir 'cli-home'
    New-CiDirectory $env:NUGET_PACKAGES
    New-CiDirectory $env:DOTNET_CLI_HOME
    Write-CiLog "Resume work directory: $script:ResumeWorkDir"
    Invoke-Native 80 'dotnet' @('tool', 'restore', '--tool-manifest', (Join-Path $resumeDir 'dotnet-tools.json')) -WorkingDirectory $resumeDir -AllowedExitCodes @(0)

    # Converter (stage 80)
    $dotnetCoverageVersion = Invoke-Native 80 'dotnet' @('tool', 'run', 'dotnet-coverage', '--version') -WorkingDirectory $resumeDir -AllowedExitCodes @(0) -CaptureStdOut (Join-Path $script:RunDir 'current/dotnet-coverage-version.txt')
    $dotnetCoverageVersion = $dotnetCoverageVersion.Output[0] -split '\+' | Select-Object -First 1
    $script:ResumeConverterVersion = $dotnetCoverageVersion
    $script:ResumeConverterRid = [System.Runtime.InteropServices.RuntimeInformation]::RuntimeIdentifier
    Copy-Item (Join-Path $script:RunDir 'current/dotnet-coverage-version.txt') (Join-Path $script:RunDir 'target/dotnet-coverage-version.txt')

    # Process each pass
    foreach ($pass in @('current', 'target')) {
        $coverageRawDir = Join-Path $script:RunDir "$pass/coverage/raw"
        $coverageFiles = @(Get-ChildItem -Path $coverageRawDir -Filter "*.coverage" -Recurse -File)
        if ($coverageFiles.Count -ne 1) {
            Fail 80 "Expected exactly one .coverage file in $pass, found $($coverageFiles.Count)"
        }

        $coverageXmlPath = Join-Path $script:RunDir "$pass/coverage/coverage.xml"
        New-CiDirectory (Split-Path $coverageXmlPath)
        
        Invoke-Native 80 'dotnet' @('tool', 'run', 'dotnet-coverage', 'merge', $coverageFiles[0].FullName, '-f', 'xml', '-o', $coverageXmlPath) -WorkingDirectory $resumeDir -AllowedExitCodes @(0)

        # Update collection-settings.json
        $settingsPath = Join-Path $script:RunDir "$pass/collection-settings.json"
        $settings = Get-Content $settingsPath | ConvertFrom-Json -AsHashtable
        $settings['dotnetCoverageToolVersion'] = $dotnetCoverageVersion
        $settings['converterRid'] = $script:ResumeConverterRid
        $settings | ConvertTo-Json -Depth 10 | Set-Content $settingsPath
    }

    # Assert comparable settings
    Assert-ComparableSettings (Join-Path $script:RunDir 'current/collection-settings.json') (Join-Path $script:RunDir 'target/collection-settings.json')

    # Baseline (stage 91)
    $baselinePath = Join-Path $script:RunDir 'target/baseline.json'
    $runnerDll = Join-Path $resumeDir 'runner/WeatherParser.Coverage.Runner.dll'
    $targetSettings = Get-Content (Join-Path $script:RunDir 'target/collection-settings.json') | ConvertFrom-Json
    $currentSettings = Get-Content (Join-Path $script:RunDir 'current/collection-settings.json') | ConvertFrom-Json
    
    $excludeArgs = @()
    foreach ($exclusion in $resumeJson.exclusions) {
        $excludeArgs += @('--exclude', $exclusion)
    }
    
    $baselineArgs = @($runnerDll, 'write-baseline', '--report', (Join-Path $script:RunDir 'target/coverage/coverage.xml'), '--commit', $resumeJson.targetCommit, '--tool-name', $resumeJson.toolName, '--tool-version', $targetSettings.microsoftCodeCoverageVersion) + $excludeArgs + @('--output', $baselinePath)
    Invoke-Native 91 'dotnet' $baselineArgs -WorkingDirectory $script:RunDir -AllowedExitCodes @(0)

    # Gate (stage 90)
    $changedFiles = Join-Path $script:RunDir 'changed-files.txt'
    $coverageXmlPath = Join-Path $script:RunDir 'current/coverage/coverage.xml'

    $evaluateArgs = @($runnerDll, 'evaluate', '--report', $coverageXmlPath, '--changed-files', $changedFiles, '--tool-name', $resumeJson.toolName, '--tool-version', $currentSettings.microsoftCodeCoverageVersion) + $excludeArgs + @('--baseline', $baselinePath, '--expected-baseline-commit', $resumeJson.targetCommit)
    $gate = Invoke-Native 90 'dotnet' $evaluateArgs -WorkingDirectory $script:RunDir -AllowedExitCodes @(0, 1, 2) -CaptureStdOut (Join-Path $script:RunDir 'gate-result.json')
    
    switch ($gate.ExitCode) {
        1 {
            $gateResult = Get-Content (Join-Path $script:RunDir 'gate-result.json') | ConvertFrom-Json
            $failedRules = $gateResult.rules | Where-Object { $_.verdict -eq $false } | ForEach-Object { $_.ruleId }
            Fail 90 "Coverage gate failed: $($failedRules -join ', ')"
        }
        2 {
            Fail 91 "Coverage gate input error"
        }
    }

    Write-CiLog "Gate passed (resumed)"
}

. (Join-Path $PSScriptRoot 'ci-local.Cli.ps1')

if ($ResumeFrom) {
    $script:ResumeConverterVersion = $null
    $script:ResumeConverterRid = $null
    $script:ResumeWorkDir = $null
    try {
        Invoke-ResumeFrom
        $exit = 0
    } catch [CiFailure] {
        $exit = $_.Exception.ExitCode
    } catch {
        Write-CiLog "UNEXPECTED: $($_.Exception.Message)"
        $exit = if ($script:CurrentStageCode -gt 0) { $script:CurrentStageCode } else { 20 }
    } finally {
        try {
            if ($script:RunDir) {
                $duration = [DateTime]::UtcNow - $script:StartTime
                $durationStr = $duration.ToString('hh\:mm\:ss')
                Write-CiLog "Resume completed in $durationStr"
                
                $resumeResult = @{
                    startedUtc = $script:StartTime.ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
                    finishedUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
                    exitCode = $exit
                    converterRid = $script:ResumeConverterRid
                    converterToolVersion = $script:ResumeConverterVersion
                }
                
                $resumeResult | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $script:RunDir 'resume/resume-result.json')
                if ($script:ResumeWorkDir -and !$KeepWorkDir -and (Test-Path -LiteralPath $script:ResumeWorkDir)) {
                    Remove-Item -Recurse -Force -LiteralPath $script:ResumeWorkDir
                    Write-CiLog "Deleted resume work directory: $script:ResumeWorkDir"
                }
            }
        } catch {
            Write-CiLog "Error in cleanup: $_"
        }
    }
    exit $exit
}

try {
    # Setup
    $script:RepoRoot = git -C $PSScriptRoot rev-parse --show-toplevel
    
    if (!$TargetRef) { Fail 20 "-TargetRef is required" }
    
    # Resolve SHAs
    $commitSha = git -C $script:RepoRoot rev-parse --verify --quiet "$Commit^{commit}"
    if (!$commitSha) {
        Fail 20 "Invalid commit reference: $Commit"
    }
    
    $targetSha = git -C $script:RepoRoot rev-parse --verify --quiet "$TargetRef^{commit}"
    if (!$targetSha) {
        Fail 20 "Invalid target reference: $TargetRef"
    }
    
    $script:CommitSha = $commitSha
    $script:TargetSha = $targetSha
    
    if ($commitSha -eq $targetSha -and !$Bootstrap) {
        Fail 20 "TargetRef equals Commit without Bootstrap"
    }

    if ($BaselinePath -and $Bootstrap) {
        Fail 20 "Both -BaselinePath and -Bootstrap specified"
    }

    if ($CollectOnly -and ($Bootstrap -or $BaselinePath)) {
        Fail 20 "CollectOnly requires two-pass mode (no BaselinePath and no Bootstrap)"
    }

    if ($FaultInjection -in @('GateFailure', 'IncomparableTarget', 'InvalidReconciliation') -and $CollectOnly) {
        Fail 20 "FaultInjection $FaultInjection requires a run with conversion and gate"
    }

    if ($BaselinePath -and !(Test-Path $BaselinePath)) {
        Fail 20 "BaselinePath file does not exist: $BaselinePath"
    }

    if ($BaselinePath) {
        $BaselinePath = (Resolve-Path -LiteralPath $BaselinePath).Path
        $script:BaselineSettingsPath = Join-Path (Split-Path -Parent $BaselinePath) 'collection-settings.json'
        if (!(Test-Path $script:BaselineSettingsPath)) {
            Fail 20 "External baseline requires a sibling collection-settings.json: $script:BaselineSettingsPath"
        }
        
        # Validate baseline commit matches target
        $baseline = Get-Content -Raw $BaselinePath | ConvertFrom-Json
        $settings = Get-Content $script:BaselineSettingsPath | ConvertFrom-Json
        
        if ($baseline.commitId -ne $settings.commit) {
            Fail 20 "External baseline JSON and settings commit mismatch: baseline has '$($baseline.commitId)', settings have '$($settings.commit)'"
        }
        
        if ($baseline.commitId -ne $targetSha) {
            Fail 20 "External baseline commit '$($baseline.commitId)' does not match TargetRef commit '$targetSha'"
        }
    }

    if ($RunSettings) {
        if (!(Test-Path -LiteralPath $RunSettings)) {
            Fail 20 "RunSettings file does not exist: $RunSettings"
        }
        $RunSettings = (Resolve-Path -LiteralPath $RunSettings).Path
    }

    if ($FaultInjection -eq 'IncomparableTarget' -and ($Bootstrap -or $BaselinePath)) {
        Fail 20 "IncomparableTarget requires two-pass mode (no BaselinePath and no Bootstrap)"
    }
    
    if ($FaultInjection -eq 'RunSettingsTamper' -and ($Bootstrap -or $BaselinePath -or !$RunSettings)) {
        Fail 20 "RunSettingsTamper requires two-pass mode and -RunSettings"
    }
    
    # Create run directory
    $commitShort = git -C "$script:RepoRoot" rev-parse --short $commitSha
    $script:RunDir = Join-Path $script:RepoRoot "artifacts/ci/$commitShort-$([DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ'))"
    New-CiDirectory $script:RunDir
    Write-CiLog "Run directory: $script:RunDir"
    
    # Setup run settings
    if ($RunSettings) {
        $script:RunSettingsCopy = Join-Path $script:RunDir 'coverage.runsettings'
        Copy-Item -LiteralPath $RunSettings -Destination $script:RunSettingsCopy
        $runSettingsBytes = [IO.File]::ReadAllBytes($script:RunSettingsCopy)
        $runSettingsSha256 = [System.Security.Cryptography.SHA256]::HashData($runSettingsBytes)
        $script:RunSettingsSha256 = [BitConverter]::ToString($runSettingsSha256).Replace('-', '').ToLowerInvariant()
        Write-CiLog "Run settings: $RunSettings copied to $script:RunSettingsCopy, SHA-256 $($script:RunSettingsSha256)"
    } else {
        $script:RunSettingsCopy = $null
        $script:RunSettingsSha256 = 'none'
    }
    
    $commitsJson = @{
        commit = $commitSha
        commitShort = $commitShort
        targetRef = $TargetRef
        targetCommit = $targetSha
        mode = if ($Bootstrap) { "bootstrap" } elseif ($BaselinePath) { "baseline-path" } else { "two-pass" }
        collectOnly = [bool]$CollectOnly
        faultInjection = if ($FaultInjection) { $FaultInjection } else { $null }
        startedUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
    }

    $commitsJson | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $script:RunDir 'commits.json')

    # Create work directory
    $script:WorkDir = Join-Path ([IO.Path]::GetTempPath()) "weatherparser-ci-$([guid]::NewGuid())"
    New-CiDirectory $script:WorkDir
    Write-CiLog "Work directory: $script:WorkDir"

    # Clone and checkout
    Invoke-Native 20 'git' @('clone', '--no-local', '--quiet', $script:RepoRoot, "$script:WorkDir/src") -WorkingDirectory $script:RepoRoot -AllowedExitCodes @(0)
    Invoke-Native 20 'git' @('checkout', '--detach', '--quiet', $commitSha) -WorkingDirectory "$script:WorkDir/src" -AllowedExitCodes @(0)

    if ($FaultInjection -eq 'SdkMismatch') {
        Write-CiLog "FAULT INJECTION: SdkMismatch"
        $globalJsonPath = Join-Path "$script:WorkDir/src" "global.json"
        $globalJsonContent = [IO.File]::ReadAllText($globalJsonPath)
        $globalJsonContent = $globalJsonContent -replace '"version": "[^"]*"', '"version": "0.0.0"'
        [IO.File]::WriteAllText($globalJsonPath, $globalJsonContent, [Text.UTF8Encoding]::new($false))
    }

    # SDK check
    $dotnetVersion = Invoke-Native 10 'dotnet' @('--version') -WorkingDirectory "$script:WorkDir/src" -AllowAnyExitCode
    $globalJson = Get-Content (Join-Path "$script:WorkDir/src" "global.json") | ConvertFrom-Json
    $reportedVersion = if ($dotnetVersion.Output.Count -gt 0) { "$($dotnetVersion.Output[0])".Trim() } else { '' }
    if ($dotnetVersion.ExitCode -ne 0 -or $reportedVersion -ne $globalJson.sdk.version) {
        $sdks = Invoke-Native 10 'dotnet' @('--list-sdks') -WorkingDirectory ([IO.Path]::GetTempPath()) -AllowedExitCodes @(0)
        Fail 10 "DotNet SDK version mismatch: global.json requires $($globalJson.sdk.version); 'dotnet --version' returned exit code $($dotnetVersion.ExitCode) and output '$reportedVersion'. Installed SDKs: $($sdks.Output -join '; ')"
    }

    $dotnetInfo = Invoke-Native 10 'dotnet' @('--info') -WorkingDirectory "$script:WorkDir/src" -AllowedExitCodes @(0)
    $dotnetInfo.Output | Out-File -FilePath (Join-Path $script:RunDir 'dotnet-info.txt') -Encoding utf8NoBOM

    # Run stages
    Invoke-RestoreStage -CloneDir "$script:WorkDir/src" -PassDir (Join-Path $script:RunDir 'current')
    Invoke-BuildStage -CloneDir "$script:WorkDir/src" -PassDir (Join-Path $script:RunDir 'current')
    Invoke-TestStage -CloneDir "$script:WorkDir/src" -PassDir (Join-Path $script:RunDir 'current')
    Invoke-PackStage -CloneDir "$script:WorkDir/src" -PassDir (Join-Path $script:RunDir 'current')
    Invoke-ConsumerStage -CloneDir "$script:WorkDir/src" -PassDir (Join-Path $script:RunDir 'current')
    $script:CliPresent = Test-CliPresent "$script:WorkDir/src"
    if ($script:CliPresent -and $CollectOnly) {
        Fail 20 "-CollectOnly does not support the example CLI coverage gate (CL-19); run the two-pass mode"
    }
    
    if ($CollectOnly) {
        Write-CollectionSettings "$script:WorkDir/src" (Join-Path $script:RunDir 'current') 'not-run' 'not-run'
    } else {
        Invoke-ConversionStage -CloneDir "$script:WorkDir/src" -PassDir (Join-Path $script:RunDir 'current')
    }

    if ($script:CliPresent) {
        $libraryVersion = Get-LibraryVersion "$script:WorkDir/src"
        $packageHash = (Get-Content (Join-Path $script:RunDir 'package-hash.txt')).Trim()
        Invoke-CliStage -CloneDir "$script:WorkDir/src" -PassDir (Join-Path $script:RunDir 'current') -NupkgPath (Join-Path $script:RunDir "WeatherParser.Parsing.$libraryVersion.nupkg") -PackageHash $packageHash -NuGetPackagesPath (Join-Path $script:WorkDir 'nuget-packages') -Version $libraryVersion
    }
    
    Invoke-BaselinePass
    
    if ($CollectOnly) {
        Write-ChangedFiles
        New-ResumePackage
        Write-CiLog "collection complete, gate pending"
        $exit = 0
    } else {
        Write-ChangedFiles
        New-ResumePackage
        Invoke-GateStage
        $exit = 0
    }
} catch [CiFailure] {
    $exit = $_.Exception.ExitCode
} catch {
    Write-CiLog "UNEXPECTED: $($_.Exception.Message)"
    $exit = if ($script:CurrentStageCode -gt 0) { $script:CurrentStageCode } else { 20 }
} finally {
    try {
        if ($script:RunDir) {
            $commitsJson = Get-Content (Join-Path $script:RunDir 'commits.json') | ConvertFrom-Json -AsHashtable
            $commitsJson['finishedUtc'] = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
            $commitsJson['exitCode'] = $exit
            $commitsJson | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $script:RunDir 'commits.json')
        }
    } catch {
        Write-CiLog "Error in cleanup: $_"
    }
    Invoke-FinishStage
}

exit $exit
