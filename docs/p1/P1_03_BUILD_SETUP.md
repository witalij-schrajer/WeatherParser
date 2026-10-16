```markdown
# P1.03 – Minimale Solution, Bibliothek und Testprojekte vorbereiten

[Deutsch](P1_03_BUILD_SETUP.md) | [English](P1_03_BUILD_SETUP.en.md)

**Status: abgeschlossen am 15.10.2026. Die Einschränkung des Visual-Studio-Builds wurde behoben; die Reproduzierbarkeit auf sauberem Checkout steht weiterhin aus (siehe unten).** Umfang: SDK-Befund aus P1.02 schließen, reproduzierbaren Restore/Build mit dem ausgewählten SDK nachweisen, Build-/Secret-Ausschlüsse prüfen. Keine neue Fachimplementierung, keine Parser, kein CLI in diesem Schritt.

## Durchgeführte Schritte

| Schritt | Ergebnis |
|---|---|
| SDK-Verfügbarkeit | `dotnet --list-sdks` zeigt `10.0.402 [C:\Program Files\dotnet\sdk]`; lokal bereits installiert |
| `global.json` | `version` von `10.0.303` auf `10.0.402` umgestellt; `rollForward: disable` und `allowPrerelease: false` unverändert |
| `dotnet --version` im Repo-Root | Liefert `10.0.402`; SDK-Auflösung über `global.json` bestätigt |
| Locked Restore | `dotnet restore WeatherParser.slnx --locked-mode` erfolgreich; `packages.lock.json` beider Projekte unverändert, da keine Paketversionen angefasst wurden |
| Release-Build (dotnet-CLI) | `dotnet build WeatherParser.slnx -c Release` erfolgreich für `WeatherParser.Parsing` und `WeatherParser.Parsing.Tests`; 0 Fehler |
| Build-/Secret-Ausschlüsse | `.gitignore` deckt `bin/`, `obj/`, `TestResults/`, `artifacts/`, `*.nupkg`/`*.snupkg`, `*.coverage(xml)`, `.env*`, `secrets/`, `*.pfx`/`*.p12`/`*.key` bereits ab; keine Änderung erforderlich |

## Behoben: Visual-Studio-Build

Der Build über das in Visual Studio Community 2026 (18.11.0) eingebettete MSBuild schlug zuvor mit `NETSDK1141` (`GlobalJsonSDKResolutionFailed`) fehl, obwohl `dotnet build` über die CLI mit identischer `global.json` erfolgreich war. Ursache war ein veralteter SDK-Resolver-/Build-Server-Zustand der laufenden IDE-Instanz.

**Behebung:** Durch Ausführen von `dotnet build-server shutdown` (Beenden gecachter MSBuild-/VBCSCompiler-Serverprozesse) und einen vollständigen Neustart von Visual Studio (alle `devenv.exe`-Prozesse beendet, IDE neu gestartet) wurde das Problem behoben. Ein anschließender Solution-Build über Visual Studio/MSBuild war erfolgreich, ohne `NETSDK1141`-Fehler. Dieser Befund gilt als geschlossen.

## Reproduzierbarkeit auf sauberem Checkout

Das Repository hat noch keinen Git-Commit (`git log` meldet „does not have any commits yet“, wie bereits in P1.01 festgestellt). Ein isolierter Worktree-/Klon-Nachweis (entspricht T-P1-08) ist daher aktuell nicht durchführbar und wird nicht behauptet. Dieser Nachweis ist einem künftigen Schritt nach dem ersten Commit zugeordnet.

## Prüfungen und Übergabe

- SDK-Band aktualisiert und `global.json` fixiert; SDK-Befund aus P1.02 geschlossen.
- Locked Restore und CLI-Release-Build mit dem neuen SDK erfolgreich nachgewiesen.
- Bestehende `.gitignore`-Ausschlüsse als ausreichend geprüft; keine Änderung notwendig.
- Keine Paketversionen, Testcode oder Coverage-Werte in diesem Schritt verändert oder behauptet.

**P1.03 im CLI-Build und im IDE-Build abgeschlossen; Reproduzierbarkeit auf sauberem Checkout steht noch aus.** Nächste Voraussetzung für P1.04 (isolierter Paketkonsument) ist unverändert erfüllbar, da die Bibliothek baut. Der ausstehende Clean-Checkout-Nachweis blockiert P1.04 nicht, sollte aber vor der P1-Gesamtabnahme (P1.12) nachgeholt werden.
```
