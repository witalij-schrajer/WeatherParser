#Requires -Version 7.0

<#
.SYNOPSIS
Checks that stored release-candidate artifacts may be promoted to a target commit (P9 release contract RL-06 A).

.DESCRIPTION
Exit codes:
  0 - PROMOTABLE
  2 - Input error
  3 - NOT-AN-ANCESTOR
  4 - CHANGED-INPUT
  5 - MISSING-ARTIFACT, CHECKSUM-MISMATCH, or EVIDENCE-COMMIT-MISMATCH

.PARAMETER Repository
Path to the Git repository.

.PARAMETER Candidate
The candidate commit hash.

.PARAMETER Target
The target commit hash.

.PARAMETER ArtifactDirectory
Path to the directory containing artifacts.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Repository,
    [Parameter(Mandatory)][string]$Candidate,
    [Parameter(Mandatory)][string]$Target,
    [Parameter(Mandatory)][string]$ArtifactDirectory
)
Set-StrictMode -Version Latest


function Get-CommitHash {
    param([string]$Repo, [string]$Commit)
    
    $result = git -C $Repo rev-parse --verify --quiet "$Commit^{commit}"
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($result)) {
        [Console]::Error.WriteLine("Input error: unknown commit '$Commit'.")
        exit 2
    }
    return $result.Trim()
}

$fullCandidate = Get-CommitHash -Repo $Repository -Commit $Candidate
$fullTarget = Get-CommitHash -Repo $Repository -Commit $Target

$sha256SumsPath = Join-Path $ArtifactDirectory "SHA256SUMS"
$evidencePath = Join-Path $ArtifactDirectory "evidence.json"

if (-not (Test-Path $sha256SumsPath)) {
    [Console]::Error.WriteLine("Input error: '$sha256SumsPath' does not exist.")
    exit 2
}

if (-not (Test-Path $evidencePath)) {
    [Console]::Error.WriteLine("Input error: '$evidencePath' does not exist.")
    exit 2
}

$mergeBaseResult = git -C $Repository merge-base --is-ancestor $fullCandidate $fullTarget
if ($LASTEXITCODE -eq 1) {
    Write-Output "NOT-AN-ANCESTOR"
    exit 3
}
if ($LASTEXITCODE -ne 0) {
    [Console]::Error.WriteLine("Input error: unknown commit '$Candidate'.")
    exit 2
}

$diffResult = git -C $Repository diff --name-only --no-renames $fullCandidate $fullTarget

$buildInputs = @()
foreach ($line in $diffResult -split "`n") {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    
    # A path is a build input unless it ends with .md (ordinal, case-insensitive) and is not exactly README.md
    $isMarkdown = $line -like "*.md" -or $line -like "*.MD"
    if ($isMarkdown -and $line -ne "README.md") {
        continue
    }
    
    $buildInputs += $line
}

if ($buildInputs.Count -gt 0) {
    foreach ($inputFile in $buildInputs) {
        Write-Output "CHANGED-INPUT $inputFile"
    }
    exit 4
}

$sha256Sums = Get-Content $sha256SumsPath | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

foreach ($line in $sha256Sums) {
    $parts = $line -split '\s{2,}', 2
    if ($parts.Count -ne 2) { continue }
    
    $hash = $parts[0]
    $name = $parts[1]
    
    $filePath = Join-Path $ArtifactDirectory $name
    
    if (-not (Test-Path $filePath)) {
        Write-Output "MISSING-ARTIFACT $name"
        exit 5
    }
    
    $fileHash = Get-FileHash -Path $filePath -Algorithm SHA256 | Select-Object -ExpandProperty Hash
    if ($hash.ToLower() -ne $fileHash.ToLower()) {
        Write-Output "CHECKSUM-MISMATCH $name"
        exit 5
    }
}

$evidence = Get-Content $evidencePath | ConvertFrom-Json

if (-not $evidence.commit -or $evidence.commit -ine $fullCandidate) {
    Write-Output "EVIDENCE-COMMIT-MISMATCH"
    exit 5
}

Write-Output "PROMOTABLE"
exit 0
