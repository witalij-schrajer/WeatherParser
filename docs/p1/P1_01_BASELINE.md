# P1.01 – Ausgangsprotokoll

[Deutsch](P1_01_BASELINE.md) | [English](P1_01_BASELINE.en.md)

**Status: abgeschlossen am 15.10.2026.** Umfang: Arbeitsstand und P0-Abschluss prüfen, P1-Umfang bestätigen. Die vorhandene Projektstruktur wurde geprüft und erhalten; sie wurde in diesem Schritt nicht neu erzeugt.

## Ausgangsstand

| Merkmal | Festgestellter Stand |
|---|---|
| Git-Branch | `master` |
| HEAD | Noch kein Commit; alle 26 erfassten Projektdateien sind untracked |
| Git-Remotes | Keine |
| Solution | `WeatherParser.slnx`, zwei Projekte |
| Bibliothek | `src/WeatherParser.Parsing/WeatherParser.Parsing.csproj` |
| Unit-Testprojekt | `tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj`, Quellreferenz auf die Bibliothek |
| Paketkonsument | Bereich mit zweisprachiger Dokumentation reserviert; noch kein Projekt |
| SDK / Framework | SDK 10.0.303 fixiert; `net10.0` |
| Restore | Lockdateien für beide Projekte vorhanden |
| Fachimplementierung | Keine versionierbaren C#-Quelldateien und keine Testmethoden; generierte Builddateien gehören nicht zu diesem Befund |
| Anforderungen | 84 eindeutige IDs, in Deutsch und Englisch in gleicher Reihenfolge |

Die [Ausgangsinventur](P1_01_BASELINE.json) enthält relative Dateipfade und SHA-256-Prüfsummen der 26 Projektdateien vor den Dokumentationsänderungen dieses Audits. Git-Metadaten sowie ignorierte Build-/Testausgaben sind nicht enthalten. Die Inventur ist eine Arbeitsstandsaufnahme, kein Git-Commit und kein Nachweis einer unveränderlichen Code-Baseline. Spätere beauftragte Änderungen führen erwartungsgemäß zu anderen Prüfsummen.

## P0-Abschluss und P1-Grenzen

P0 bleibt abgeschlossen. Die Dokumentation ist konsistent zu NuGet als Hauptprodukt, .NET ≥10, Debian ≥13/ARM64, Konstruktor-Endpunkten, generischem MQTT-Serializer, DI, eigenen Exportentitäten und getrennten MOSMIX_S-/L-Parsern. Zeitangaben bleiben UTC ohne Suffix, Messzeitpunkte je Wert erhalten, exakte Mittelpunktwerte werden vom Nullpunkt weg gerundet. Programmausgaben und Kommentare sind Englisch, Dokumentation Deutsch/Englisch.

Die Coverage-Entscheidungen sind dokumentiert: mindestens 80 % je geforderter Kennzahl; beim initialen Fehlen einer messbaren Baseline kein Zielbranch-Vergleich; Dokumentations-PRs dürfen Gleichstand haben; exakt 100 % dürfen bei exakt 100 % Baseline bleiben. Der Microsoft-CodeCoverage-Report und die tatsächlich nachweisbaren Kennzahlen werden erst in P1.07/P1.08 geprüft. Block-Coverage wird nicht als Branch-Coverage ausgegeben.

Reihenfolge: **P0 → P1 → P2 → P4 → P5 → P6 → P7 → P8 → P3 → P9**. Das CLI-Beispiel entsteht erst nach vollständiger Bibliotheksabnahme. P1 liefert die Build-/Test-/Paket-/CI-Grundlage; Parser, Entitäten und echte Serializer folgen in ihren vorgesehenen Phasen. Kein Zielsystemzugriff oder produktiver Dienst ist für P1.01 erforderlich.

## Durchgeführte Prüfungen

- Repository-Regeln, Anforderungen, Gesamtplan und P1-Plan gelesen und abgeglichen.
- Git-Branch, Commit-/Remote-Zustand sowie nicht ignorierte Dateien erfasst.
- Gleichheit und Eindeutigkeit der Anforderungs-IDs geprüft.
- Solution-Projektpfade, XML-Projektdateien, Unit-Test-Quellreferenz und beide `net10.0`-Lockdateien geprüft.
- Ausgangsdateien vor Änderungen per Prüfsumme gesichert; Quell-/Projekt-/Build-/Paketdateien erhalten.
- P1-Status und nächsten Arbeitsschritt in beiden Sprachfassungen aktualisiert.

Ergebnis: Kein ungeklärter grundlegender P0-Punkt blockiert die Eingangsprüfung. Es wurde kein Commit erstellt und kein Push ausgeführt. Restore und Release-Build waren beim vorherigen Anlegen der Struktur erfolgreich; sie wurden in dieser reinen Eingangsprüfung nicht wiederholt. Es wurden keine neuen Tests ausgeführt und keine Coverage gemessen.

## Übergabe

**P1.01 abgeschlossen; P1 insgesamt offen.** Nächster Schritt ist P1.02: vollständige Werkzeug-/Abhängigkeits-, Lizenz- und Kompatibilitätsprüfung auf Basis der bestehenden Versionsauswahl. Danach den Struktur-/Reproduzierbarkeitsanteil P1.03 abschließen. Der isolierte Paketkonsument bleibt P1.04, TDD-Verhaltenstests P1.05, Coverage P1.07/P1.08 und Debian-/ARM64-Nachweis P1.10/P9 zugeordnet.

Die verbliebenen Nachweise sind Aufgaben ihrer Phasen und eröffnen P0 nicht erneut. Der aktuelle Auftrag führt ausschließlich P1.01 aus; nachfolgende Arbeitspakete werden nicht durch dieses Protokoll als umgesetzt oder abgenommen erklärt.
