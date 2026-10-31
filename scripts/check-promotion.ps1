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
  5 - MISSING-ARTIFACT, CHECKSUM-MISMATCH, MANIFEST-MISMATCH, or EVIDENCE-COMMIT-MISMATCH

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
$ErrorActionPreference = 'Stop'

function Fail-Input([string]$Message) {
    [Console]::Error.WriteLine("Input error: $Message")
    exit 2
}

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

if (-not (Test-Path -LiteralPath $sha256SumsPath)) {
    Fail-Input "'$sha256SumsPath' does not exist."
}

if (-not (Test-Path -LiteralPath $evidencePath)) {
    Fail-Input "'$evidencePath' does not exist."
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

# Manifest: at least one line, every line "<64 hex>  <file name>", no repeated name.
$lines = @(Get-Content -LiteralPath $sha256SumsPath | Where-Object { $_ -ne '' })
if ($lines.Count -eq 0) {
    Fail-Input "'$sha256SumsPath' lists no artifact."
}

$manifest = [System.Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
foreach ($line in $lines) {
    if ($line -cnotmatch '^(?<hash>[0-9a-fA-F]{64})  (?<name>[^/\\]+)$') {
        Fail-Input "'$sha256SumsPath' has a malformed line: $line"
    }

    $name = $Matches['name']
    if ($manifest.ContainsKey($name)) {
        Fail-Input "'$sha256SumsPath' lists '$name' twice."
    }

    $manifest.Add($name, $Matches['hash'].ToLowerInvariant())
}

# Evidence: valid JSON with a string commit and an array of artifacts with string name and sha256.
try {
    $evidence = Get-Content -LiteralPath $evidencePath -Raw | ConvertFrom-Json -AsHashtable
}
catch {
    Fail-Input "'$evidencePath' is not valid JSON."
}

$validEvidence = $evidence -is [System.Collections.IDictionary] -and
    $evidence.Contains('commit') -and $evidence['commit'] -is [string] -and
    $evidence.Contains('artifacts') -and $evidence['artifacts'] -is [System.Collections.IList]
if (-not $validEvidence) {
    Fail-Input "'$evidencePath' does not have the expected commit and artifacts."
}

$evidenceArtifacts = [System.Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
foreach ($artifact in $evidence['artifacts']) {
    $validArtifact = $artifact -is [System.Collections.IDictionary] -and
        $artifact.Contains('name') -and $artifact['name'] -is [string] -and
        $artifact.Contains('sha256') -and $artifact['sha256'] -is [string]
    if (-not $validArtifact) {
        Fail-Input "'$evidencePath' does not have the expected artifact structure."
    }

    $evidenceArtifacts[$artifact['name']] = $artifact['sha256'].ToLowerInvariant()
}

# Artifacts: every manifest entry exists with its hash; manifest and evidence list the same artifacts; the evidence names the candidate.
$findings = [System.Collections.Generic.List[string]]::new()
foreach ($name in $manifest.Keys) {
    $filePath = Join-Path -Path $ArtifactDirectory -ChildPath $name
    if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) {
        $findings.Add("MISSING-ARTIFACT $name")
        continue
    }

    $fileHash = (Get-FileHash -LiteralPath $filePath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($fileHash -cne $manifest[$name]) {
        $findings.Add("CHECKSUM-MISMATCH $name")
    }
}

$allNames = [System.Collections.Generic.SortedSet[string]]::new([StringComparer]::Ordinal)
foreach ($name in $manifest.Keys) { [void]$allNames.Add($name) }
foreach ($name in $evidenceArtifacts.Keys) { [void]$allNames.Add($name) }
foreach ($name in $allNames) {
    if (-not $manifest.ContainsKey($name) -or -not $evidenceArtifacts.ContainsKey($name) -or $manifest[$name] -cne $evidenceArtifacts[$name]) {
        $findings.Add("MANIFEST-MISMATCH $name")
    }
}

if ($evidence['commit'] -ine $fullCandidate) {
    $findings.Add('EVIDENCE-COMMIT-MISMATCH')
}

if ($findings.Count -gt 0) {
    foreach ($finding in $findings) {
        Write-Output $finding
    }

    exit 5
}

Write-Output "PROMOTABLE"
exit 0
