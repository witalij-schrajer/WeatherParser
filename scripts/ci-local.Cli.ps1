# Example CLI stage and CLI coverage gate of ci-local.ps1 (P3.03, CL-01, CL-19 option C, REQ-086 amendment). Dot-sourced; not run on its own.
$script:CliProjectPath = 'examples/WeatherParser.Cli/WeatherParser.Cli.csproj'
$script:CliTestsProject = 'tests/WeatherParser.Cli.Tests'
$script:CliLockFiles = @('examples/WeatherParser.Cli/packages.lock.json', 'tests/WeatherParser.Cli.Tests/packages.lock.json')
$script:LibraryGatedInputs = @('src/', 'Directory.Build.props', 'Directory.Packages.props', 'global.json', 'NuGet.Config', '.editorconfig', 'coverage.runsettings')
$script:CliGateExclusions = @('--exclude', '*.Tests.dll', '--exclude', 'WeatherParser.Coverage*.dll', '--exclude', 'WeatherParser.Parsing.dll')

function Test-CliPresent([string]$CloneDir) {
    return Test-Path -LiteralPath (Join-Path $CloneDir $script:CliProjectPath) -PathType Leaf
}

function Get-GatedInputArguments() {
    $result = @()
    foreach ($gatedInput in $script:LibraryGatedInputs) {
        $result += @('--gated-input', $gatedInput)
    }
    return $result
}

function Get-LibraryVersion([string]$CloneDir) {
    $version = ([xml](Get-Content (Join-Path $CloneDir "src/WeatherParser.Parsing/WeatherParser.Parsing.csproj"))).Project.PropertyGroup.Version
    if ($version -is [array]) {
        $version = ($version | Where-Object { $_ -ne '' } | Select-Object -First 1)
    }
    return $version
}

function Invoke-CliStage([string]$CloneDir, [string]$PassDir, [string]$NupkgPath, [string]$PackageHash, [string]$NuGetPackagesPath, [string]$Version) {
    Write-CiLog "=== CLI ==="
    $script:CurrentStageCode = 75
    New-CiDirectory $PassDir

    $cliProjectContent = [IO.File]::ReadAllText((Join-Path $CloneDir $script:CliProjectPath))
    if ($cliProjectContent -match '<ProjectReference') {
        Fail 75 "The example CLI must not use a ProjectReference (CL-01, T-P3-01)"
    }

    $localNuGetPath = Join-Path $CloneDir "artifacts/local-nuget"
    New-CiDirectory $localNuGetPath
    Copy-Item $NupkgPath $localNuGetPath

    foreach ($lockJsonPath in $script:CliLockFiles) {
        $lockJsonContent = [IO.File]::ReadAllText((Join-Path $CloneDir $lockJsonPath))
        $pattern = '(?s)("WeatherParser\.Parsing"\s*:\s*\{.*?"contentHash"\s*:\s*")[^"]*(")'
        $lockJsonContent = [regex]::new($pattern).Replace($lockJsonContent, '${1}' + $PackageHash + '${2}', 1)
        [IO.File]::WriteAllText((Join-Path $CloneDir $lockJsonPath), $lockJsonContent, [Text.UTF8Encoding]::new($false))
    }

    $previousPackages = $env:NUGET_PACKAGES
    $env:NUGET_PACKAGES = $NuGetPackagesPath
    New-CiDirectory $NuGetPackagesPath

    try {
        Invoke-Native 75 'dotnet' @('restore', $script:CliTestsProject, '--locked-mode') -WorkingDirectory $CloneDir -AllowedExitCodes @(0)

        foreach ($lockJsonPath in $script:CliLockFiles) {
            $oldLockJson = (Invoke-Native 75 'git' @('show', "HEAD:$lockJsonPath") -WorkingDirectory $CloneDir).Output -join "`n"
            $newLockJson = [IO.File]::ReadAllText((Join-Path $CloneDir $lockJsonPath))

            $oldJson = $oldLockJson | ConvertFrom-Json -AsHashtable
            $newJson = $newLockJson | ConvertFrom-Json -AsHashtable

            $newContentHash = $newJson.dependencies.'net10.0'.'WeatherParser.Parsing'.contentHash

            if ($newContentHash -ne $PackageHash) {
                Fail 75 "CLI lock file $($lockJsonPath): contentHash not updated correctly"
            }

            $oldJson.dependencies.'net10.0'.'WeatherParser.Parsing'.contentHash = ''
            $newJson.dependencies.'net10.0'.'WeatherParser.Parsing'.contentHash = ''

            $oldJsonStr = ConvertTo-Json -Depth 20 -Compress -InputObject $oldJson
            $newJsonStr = ConvertTo-Json -Depth 20 -Compress -InputObject $newJson

            if ($oldJsonStr -ne $newJsonStr) {
                Fail 75 "CLI lock file $($lockJsonPath) differs beyond the WeatherParser.Parsing contentHash"
            }
        }

        $status = Invoke-Native 75 'git' @('status', '--porcelain') -WorkingDirectory $CloneDir -AllowedExitCodes @(0)
        $statusLines = @($status.Output | Where-Object { "$_".Trim() -ne '' })

        foreach ($line in $statusLines) {
            if ($line -notmatch '^ M (examples/WeatherParser.Cli/packages\.lock\.json|tests/WeatherParser.Cli.Tests/packages\.lock\.json|tests/WeatherParser.Package.Tests/packages\.lock\.json)$') {
                Fail 75 "Unexpected working-tree change after the CLI restore: $line"
            }
        }

        $expectedSha512 = $PackageHash
        $sha512Path = Join-Path "$NuGetPackagesPath" "weatherparser.parsing/$Version/weatherparser.parsing.$Version.nupkg.sha512"
        if (!(Test-Path $sha512Path)) {
            Fail 75 "Package SHA512 file not found: $sha512Path"
        }
        $actualSha512 = Get-Content $sha512Path
        if ($expectedSha512 -ne $actualSha512) {
            Fail 75 "Package SHA512 mismatch"
        }

        Invoke-Native 75 'dotnet' @('build', $script:CliTestsProject, '-c', 'Release', '--no-restore') -WorkingDirectory $CloneDir -AllowedExitCodes @(0)

        $rawDir = Join-Path $PassDir 'cli-coverage/raw'
        $trxDir = Join-Path $PassDir 'trx'
        New-CiDirectory $rawDir
        New-CiDirectory $trxDir

        $testArgs = @('test', $script:CliTestsProject, '-c', 'Release', '--no-build', '--logger', 'trx;LogFileName=WeatherParser.Cli.Tests.trx', '--results-directory', $rawDir, '--collect', 'Code Coverage')
        if ($script:RunSettingsCopy) {
            $testArgs += @('--settings', $script:RunSettingsCopy)
        }

        try {
            Invoke-Native 75 'dotnet' $testArgs -WorkingDirectory $CloneDir -AllowedExitCodes @(0)
        } finally {
            $trxFiles = Get-ChildItem -Path $rawDir -Filter "*.trx" -Recurse -File
            foreach ($trxFile in $trxFiles) {
                Move-Item -LiteralPath $trxFile.FullName -Destination (Join-Path $trxDir $trxFile.Name) -Force
            }
        }

        # Collector report handling (same rules as Invoke-TestStage)
        $guidPattern = '^[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$'
        $rawRoot = (Get-Item -LiteralPath $rawDir).FullName
        $allCoverage = @(Get-ChildItem -Path $rawDir -Filter '*.coverage' -Recurse -File)
        $collectorReports = @($allCoverage | Where-Object { $_.Directory.Name -match $guidPattern -and $_.Directory.Parent.FullName -eq $rawRoot })
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
                Fail 75 "Unexpected CLI .coverage file $($copy.FullName) differs from every collector report"
            }

            $attachmentDir = $copy.Directory
            while ($attachmentDir.Parent.FullName -ne $rawRoot) {
                $attachmentDir = $attachmentDir.Parent
            }
            Remove-Item -Recurse -Force $attachmentDir.FullName
            Write-CiLog "Removed TRX attachment copy $($copy.FullName) (identical to collector report)"
        }

        $allCoverage = @(Get-ChildItem -Path $rawDir -Filter '*.coverage' -Recurse -File)
        $coverageFiles = @($allCoverage | Where-Object { $_.Directory.Name -match $guidPattern -and $_.Directory.Parent.FullName -eq $rawRoot })
        if ($coverageFiles.Count -ne 1 -or $allCoverage.Count -ne 1) {
            Fail 75 "Expected exactly one CLI collector .coverage file, found $($allCoverage.Count)"
        }



        # Read test counts and add to test-counts.json
        $cliTrxPath = Join-Path $trxDir 'WeatherParser.Cli.Tests.trx'
        if (Test-Path $cliTrxPath) {
            try {
                $xml = [xml](Get-Content $cliTrxPath)
                $counters = $xml.TestRun.ResultSummary.Counters
                if ($counters) {
                    $testCounts = @{}
                    $testCountsPath = Join-Path $PassDir 'test-counts.json'
                    if (Test-Path $testCountsPath) {
                        $testCounts = Get-Content $testCountsPath | ConvertFrom-Json -AsHashtable
                    }
                    $testCounts['WeatherParser.Cli.Tests'] = @{
                        total = [int]$counters.total
                        passed = [int]$counters.passed
                        failed = [int]$counters.failed
                    }
                    $testCounts | ConvertTo-Json -Depth 10 | Set-Content $testCountsPath
                    Write-CiLog "WeatherParser.Cli.Tests: total=$($testCounts['WeatherParser.Cli.Tests'].total) passed=$($testCounts['WeatherParser.Cli.Tests'].passed) failed=$($testCounts['WeatherParser.Cli.Tests'].failed)"
                }
            } catch {
                Write-CiLog "Warning: Failed to parse CLI TRX file $($cliTrxPath): $_"
            }
        }

        $coverageXmlPath = Join-Path $PassDir 'cli-coverage/coverage.xml'
        New-CiDirectory (Split-Path $coverageXmlPath)
        Invoke-Native 75 'dotnet' @('tool', 'run', 'dotnet-coverage', 'merge', $coverageFiles[0].FullName, '-f', 'xml', '-o', $coverageXmlPath) -WorkingDirectory $CloneDir -AllowedExitCodes @(0)

    } finally {
        $env:NUGET_PACKAGES = $previousPackages
    }

    Write-CiLog "CLI complete"
}

function Invoke-CliTargetStage([string]$TargetDir, [string]$ToolVersion) {
    Write-CiLog "=== CLI TARGET ==="
    $script:CurrentStageCode = 75
    $passDir = Join-Path $script:RunDir 'target'
    $packageDir = Join-Path $passDir 'package'
    New-CiDirectory $packageDir

    Invoke-Native 75 'dotnet' @('pack', 'src/WeatherParser.Parsing', '-c', 'Release', '--no-build', '-o', $packageDir) -WorkingDirectory $TargetDir -AllowedExitCodes @(0)

    $nupkgFiles = Get-ChildItem -Path $packageDir -Filter "*.nupkg" -File
    if ($nupkgFiles.Count -ne 1) {
        Fail 75 "Expected exactly one .nupkg file, found $($nupkgFiles.Count)"
    }

    $nupkgPath = $nupkgFiles[0].FullName

    # Get the SHA-512 hash of the nupkg file
    $bytes = [IO.File]::ReadAllBytes($nupkgPath)
    $packageHash = [Convert]::ToBase64String([System.Security.Cryptography.SHA512]::HashData($bytes))

    Invoke-CliStage -CloneDir $TargetDir -PassDir $passDir -NupkgPath $nupkgPath -PackageHash $packageHash -NuGetPackagesPath (Join-Path $script:WorkDir 'nuget-packages-target') -Version (Get-LibraryVersion $TargetDir)

    Invoke-Native 91 'dotnet' (@('run', '--project', "$script:WorkDir/src/tools/WeatherParser.Coverage.Runner", '-c', 'Release', '--no-build', '--', 'write-baseline', '--report', (Join-Path $passDir 'cli-coverage/coverage.xml'), '--commit', $script:TargetSha, '--tool-name', 'Microsoft.CodeCoverage', '--tool-version', $ToolVersion) + $script:CliGateExclusions + @('--output', (Join-Path $passDir 'cli-baseline.json'))) -WorkingDirectory "$script:WorkDir/src" -AllowedExitCodes @(0)

    Write-CiLog "CLI target complete"
}

function Invoke-CliGate([string]$ToolVersion) {
    Write-CiLog "=== CLI GATE ==="
    $script:CurrentStageCode = 90

    $baseline = Join-Path $script:RunDir 'target/cli-baseline.json'
    $gateArgs = @('run', '--project', "$script:WorkDir/src/tools/WeatherParser.Coverage.Runner", '-c', 'Release', '--no-build', '--', 'evaluate', '--report', (Join-Path $script:RunDir 'current/cli-coverage/coverage.xml'), '--changed-files', (Join-Path $script:RunDir 'changed-files.txt'), '--tool-name', 'Microsoft.CodeCoverage', '--tool-version', $ToolVersion) + $script:CliGateExclusions

    if (Test-Path -LiteralPath $baseline) {
        $gateArgs += @('--baseline', $baseline, '--expected-baseline-commit', $script:TargetSha)
        Write-CiLog "CLI gate: comparison with the CLI baseline of the target"
    } else {
        $probe = Invoke-Native 91 'git' @('cat-file', '-e', "$($script:TargetSha):$($script:CliProjectPath)") -WorkingDirectory "$script:WorkDir/src" -AllowedExitCodes @(0, 1, 128)
        if ($probe.ExitCode -eq 0) {
            Fail 91 "The target contains the example CLI, but no CLI baseline was measured (only the two-pass mode measures it)"
        } else {
            $gateArgs += @('--bootstrap')
            Write-CiLog "CLI gate: the target has no CLI assembly; at least 80% without comparison (REQ-086 amendment)"
        }
    }

    $gate = Invoke-Native 90 'dotnet' $gateArgs -WorkingDirectory "$script:WorkDir/src" -AllowedExitCodes @(0, 1, 2) -CaptureStdOut (Join-Path $script:RunDir 'cli-gate-result.json')

    if ($gate.ExitCode -eq 1) {
        $result = Get-Content (Join-Path $script:RunDir 'cli-gate-result.json') | ConvertFrom-Json
        $failedRules = $result.rules | Where-Object { $_.verdict -eq $false } | ForEach-Object { $_.ruleId }
        Fail 90 "CLI coverage gate failed: $($failedRules -join ', ')"
    } elseif ($gate.ExitCode -eq 2) {
        Fail 91 "CLI coverage gate input error"
    }

    Write-CiLog "CLI gate complete"
}
