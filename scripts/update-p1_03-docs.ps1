<#
    Updates the canonical English docs/p1/P1_03_BUILD_SETUP.md record after the
    Visual Studio / MSBuild NETSDK1141 issue has been resolved and verified.
    Clean-checkout reproducibility remains open. No translation files are written.
    This is a historical maintenance helper; its guarded anchors deliberately
    reject an unexpected input document rather than rewriting unrelated content.
#>

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$enPath = Join-Path $repoRoot 'docs/p1/P1_03_BUILD_SETUP.md'

$changedFiles = @()

# ---------------------------------------------------------------------------
# 1. Canonical English file
# ---------------------------------------------------------------------------
$enContent = Get-Content -Path $enPath -Raw

$enStatusOld = '**Status: completed on 2026-10-15, with one open limitation (see below).**'
$enStatusNew = '**Status: completed on 2026-10-15. The Visual Studio build limitation has been resolved; clean-checkout reproducibility remains outstanding (see below).**'
if ($enContent.Contains($enStatusOld)) {
    $enContent = $enContent.Replace($enStatusOld, $enStatusNew)
} else {
    throw "Anchor not found in $enPath (status sentence). Aborting to avoid corrupting the file."
}

$enLimitationOld = @'
## Open limitation: Visual Studio build

Building through the MSBuild embedded in Visual Studio Community 2026 (18.11.0) fails with `NETSDK1141` (`GlobalJsonSDKResolutionFailed`), even though `dotnet build` via the CLI succeeds with the identical `global.json`. The likely cause is a stale SDK resolver cache in the running IDE instance, which has not yet re-resolved the newly referenced SDK band. Restarting Visual Studio was not performed in this non-interactive check and is therefore not proven.

**Next step before full P1.03 acceptance:** restart Visual Studio (or fully stop MSBuild/VS processes) and re-run the solution build through the IDE. Record the outcome here once confirmed.
'@

$enLimitationNew = @'
## Resolved: Visual Studio build

Building through the MSBuild embedded in Visual Studio Community 2026 (18.11.0) previously failed with `NETSDK1141` (`GlobalJsonSDKResolutionFailed`), even though `dotnet build` via the CLI succeeded with the identical `global.json`. The cause was a stale SDK resolver / build-server state in the running IDE instance.

**Resolution:** running `dotnet build-server shutdown` to terminate cached MSBuild/VBCSCompiler server processes, followed by a full restart of Visual Studio (all `devenv.exe` processes terminated and the IDE restarted), resolved the issue. A subsequent solution build executed through Visual Studio/MSBuild succeeded with no `NETSDK1141` error. This finding is now closed.
'@

if ($enContent.Contains($enLimitationOld)) {
    $enContent = $enContent.Replace($enLimitationOld, $enLimitationNew)
} else {
    throw "Anchor not found in $enPath (Open limitation section). Aborting to avoid corrupting the file."
}

$enHandoffOld = '**P1.03 complete for the CLI build; IDE build evidence and clean-checkout reproducibility remain outstanding.** The next prerequisite for P1.04 (isolated package consumer) remains satisfiable since the library builds. The pending IDE evidence does not block P1.04 but should be closed before the overall P1 acceptance (P1.12).'
$enHandoffNew = '**P1.03 complete for the CLI build and the IDE build; clean-checkout reproducibility remains outstanding.** The next prerequisite for P1.04 (isolated package consumer) remains satisfiable since the library builds. The pending clean-checkout evidence does not block P1.04 but should be closed before the overall P1 acceptance (P1.12).'
if ($enContent.Contains($enHandoffOld)) {
    $enContent = $enContent.Replace($enHandoffOld, $enHandoffNew)
} else {
    throw "Anchor not found in $enPath (handoff paragraph). Aborting to avoid corrupting the file."
}

Set-Content -Path $enPath -Value $enContent -NoNewline
$changedFiles += $enPath

Write-Output 'Updated the canonical English P1.03 record.'
Write-Output 'Clean-checkout reproducibility remains open.'
