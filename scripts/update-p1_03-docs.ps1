<#
    Updates docs/p1/P1_03_BUILD_SETUP.en.md first, then the German counterpart
    docs/p1/P1_03_BUILD_SETUP.md, to reflect that the Visual Studio / MSBuild
    NETSDK1141 issue has been resolved (verified via dotnet build-server shutdown
    + full Visual Studio restart + successful solution build).

    The Git clean-checkout reproducibility finding remains OPEN and must not be
    marked as resolved by this script.
#>

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$enPath = Join-Path $repoRoot 'docs/p1/P1_03_BUILD_SETUP.en.md'
$dePath = Join-Path $repoRoot 'docs/p1/P1_03_BUILD_SETUP.md'

$changedFiles = @()

# ---------------------------------------------------------------------------
# 1. English file (source of truth, updated first)
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

# ---------------------------------------------------------------------------
# 2. German file (synchronized manually from the English changes above)
# ---------------------------------------------------------------------------
$deContent = Get-Content -Path $dePath -Raw

$deStatusOld = '**Status: abgeschlossen am 15.10.2026, mit einer offenen Einschränkung (siehe unten).**'
$deStatusNew = '**Status: abgeschlossen am 15.10.2026. Die Einschränkung des Visual-Studio-Builds wurde behoben; die Reproduzierbarkeit auf sauberem Checkout steht weiterhin aus (siehe unten).**'
if ($deContent.Contains($deStatusOld)) {
    $deContent = $deContent.Replace($deStatusOld, $deStatusNew)
} else {
    throw "Anchor not found in $dePath (status sentence). Aborting to avoid corrupting the file."
}

$deLimitationOld = @'
## Offene Einschränkung: Visual-Studio-Build

Der Build über das in Visual Studio Community 2026 (18.11.0) eingebettete MSBuild schlägt mit `NETSDK1141` (`GlobalJsonSDKResolutionFailed`) fehl, obwohl `dotnet build` über die CLI mit identischer `global.json` erfolgreich ist. Ursache ist vermutlich ein veralteter SDK-Resolver-Cache der laufenden IDE-Instanz, die das neu referenzierte SDK-Band noch nicht neu aufgelöst hat. Ein Neustart von Visual Studio wurde in dieser nicht-interaktiven Prüfung nicht ausgeführt und ist daher nicht nachgewiesen.

**Nächster Schritt vor vollständiger P1.03-Abnahme:** Visual Studio neu starten (oder MSBuild-/VS-Prozesse vollständig beenden) und den Solution-Build erneut über die IDE ausführen. Ergebnis in diesem Dokument nachtragen, sobald bestätigt.
'@

$deLimitationNew = @'
## Behoben: Visual-Studio-Build

Der Build über das in Visual Studio Community 2026 (18.11.0) eingebettete MSBuild schlug zuvor mit `NETSDK1141` (`GlobalJsonSDKResolutionFailed`) fehl, obwohl `dotnet build` über die CLI mit identischer `global.json` erfolgreich war. Ursache war ein veralteter SDK-Resolver-/Build-Server-Zustand der laufenden IDE-Instanz.

**Behebung:** Durch Ausführen von `dotnet build-server shutdown` (Beenden gecachter MSBuild-/VBCSCompiler-Serverprozesse) und einen vollständigen Neustart von Visual Studio (alle `devenv.exe`-Prozesse beendet, IDE neu gestartet) wurde das Problem behoben. Ein anschließender Solution-Build über Visual Studio/MSBuild war erfolgreich, ohne `NETSDK1141`-Fehler. Dieser Befund gilt als geschlossen.
'@

if ($deContent.Contains($deLimitationOld)) {
    $deContent = $deContent.Replace($deLimitationOld, $deLimitationNew)
} else {
    throw "Anchor not found in $dePath (Offene Einschränkung section). Aborting to avoid corrupting the file."
}

$deHandoffOld = '**P1.03 im CLI-Build abgeschlossen; IDE-Build-Nachweis und Reproduzierbarkeit auf sauberem Checkout stehen noch aus.** Nächste Voraussetzung für P1.04 (isolierter Paketkonsument) ist unverändert erfüllbar, da die Bibliothek baut. Der ausstehende IDE-Nachweis blockiert P1.04 nicht, sollte aber vor der P1-Gesamtabnahme (P1.12) nachgeholt werden.'
$deHandoffNew = '**P1.03 im CLI-Build und im IDE-Build abgeschlossen; Reproduzierbarkeit auf sauberem Checkout steht noch aus.** Nächste Voraussetzung für P1.04 (isolierter Paketkonsument) ist unverändert erfüllbar, da die Bibliothek baut. Der ausstehende Clean-Checkout-Nachweis blockiert P1.04 nicht, sollte aber vor der P1-Gesamtabnahme (P1.12) nachgeholt werden.'
if ($deContent.Contains($deHandoffOld)) {
    $deContent = $deContent.Replace($deHandoffOld, $deHandoffNew)
} else {
    throw "Anchor not found in $dePath (handoff paragraph). Aborting to avoid corrupting the file."
}

Set-Content -Path $dePath -Value $deContent -NoNewline
$changedFiles += $dePath

Write-Output "Updated files:"
$changedFiles | ForEach-Object { Write-Output " - $_" }
Write-Output "Note: the clean-checkout / missing Git commit finding was intentionally left OPEN and was not modified."
