# P1.04 – Lokales NuGet-Pack und isolierter Paketkonsument

[Deutsch](P1_04_PACKAGE_CONSUMER.md) | [English](P1_04_PACKAGE_CONSUMER.en.md)

**Status: abgeschlossen am 16.10.2026.** Umfang: tatsächlichen Paketaufbau nachweisen, isolierten Konsumenten ohne Quellreferenz anlegen und über eine echte `PackageReference` auf eine lokal gebaute `.nupkg` absichern. Keine öffentliche NuGet-Veröffentlichung, kein CLI, keine Wetterquellen-Parser in diesem Schritt.

## Durchgeführte Schritte

| Schritt | Ergebnis |
|---|---|
| Paketversion | `WeatherParser.Parsing.csproj`: `Version` von `0.1.0-local` auf `0.1.0-initial` gesetzt (letzter Teil des aktuellen Branchnamens `features/develop/initial`) |
| Lokaler Paketordner | `artifacts/local-nuget/` angelegt; bereits über `.gitignore`-Eintrag `artifacts/` ausgeschlossen |
| Lokales Pack | `dotnet pack src/WeatherParser.Parsing/WeatherParser.Parsing.csproj -c Release -o artifacts/local-nuget` erfolgreich; Ergebnis `WeatherParser.Parsing.0.1.0-initial.nupkg` |
| Lokale Paketquelle | `nuget.config` um projektlokale Quelle `local-weatherparser` (`artifacts/local-nuget`) ergänzt; kein globaler `nuget sources add`, keine Veränderung der Benutzerumgebung |
| Zentrale Paketversion | `Directory.Packages.props` um `PackageVersion Include="WeatherParser.Parsing" Version="0.1.0-initial"` ergänzt, da zentrale Versionsverwaltung aktiv ist |
| Neues Testprojekt | `tests/WeatherParser.Package.Tests/WeatherParser.Package.Tests.csproj` angelegt; referenziert die Bibliothek ausschließlich über `PackageReference`, **keine** `ProjectReference` |
| Solution | Projekt per `dotnet sln WeatherParser.slnx add ...` hinzugefügt |
| Restore | `dotnet restore WeatherParser.slnx` löst `WeatherParser.Parsing` aus der lokalen Quelle auf; `dotnet restore WeatherParser.slnx --locked-mode` erfolgreich, `packages.lock.json` für das neue Projekt automatisch erzeugt |
| Build (CLI) | `dotnet build WeatherParser.slnx -c Release` erfolgreich für alle drei Projekte, 0 Fehler |
| Tests | `dotnet test tests/WeatherParser.Package.Tests -c Release`: 2 von 2 Tests erfolgreich |

## Nachweistests (T-P1-21, T-P1-22)

`PackageConsumptionTests.cs`:

- **`Deps_file_declares_the_library_as_a_restored_package_not_a_project_reference`** (T-P1-21): Liest `WeatherParser.Package.Tests.deps.json` des Testhosts und prüft, dass der Eintrag `WeatherParser.Parsing/0.1.0-initial` `"type": "package"` mit einem `.nupkg.sha512`-Hashpfad trägt. Eine `ProjectReference` würde stattdessen `"type": "project"` ohne Pakethash erzeugen. Ein erster Versuch, stattdessen den Assembly-Pfad auf das Wort „packages“ zu prüfen, war unzuverlässig (PackageReference-Assemblies werden ebenfalls lokal kopiert) und wurde verworfen.
- **`Package_exposes_only_the_expected_library_assembly`** (T-P1-22): Prüft, dass die geladene Assembly exakt `WeatherParser.Parsing` heißt und keine referenzierte Assembly mit CLI-/MQTT-Bezug enthält.

## Prüfungen und Übergabe

- Paketversion, lokale Quelle und Testprojekt ausschließlich projektlokal konfiguriert; keine globalen NuGet- oder Benutzereinstellungen verändert.
- Locked Restore und Release-Build über die `dotnet`-CLI erfolgreich; der aus P1.03 bekannte IDE-Build-Befund (`NETSDK1141`) ist inzwischen behoben (siehe [P1.03-Protokoll](P1_03_BUILD_SETUP.md)).
- Keine Coverage-Werte behauptet; Coverage-Erfassung bleibt P1.07/P1.08 zugeordnet.
- Kein Git-Commit in diesem Schritt erstellt; Repository hat weiterhin keinen initialen Commit.

**P1.04 abgeschlossen.** Nächste Voraussetzung für P1.05 (TDD-Testbasis) und P1.06 (Paket-/DI-Verträge) ist erfüllt, da sowohl Quell- als auch Pakettests nun nebeneinander bestehen und bauen.
