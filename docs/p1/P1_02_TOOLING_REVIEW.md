# P1.02 – Werkzeuge, Abhängigkeiten und Kompatibilität

[Deutsch](P1_02_TOOLING_REVIEW.md) | [English](P1_02_TOOLING_REVIEW.en.md)

**Prüfung abgeschlossen am 15.10.2026; offener Befund: SDK-Aktualisierung vor Werkzeugfreigabe.** P1.02 prüft und dokumentiert den Werkzeugstand. Es wurden keine Pakete, SDKs oder Visual-Studio-Installationen geändert. Die Prüfung ist kein vollständiger P1- oder ARM64-Laufzeitnachweis.

## Werkzeugentscheidung

| Gegenstand | Ergebnis | Konsequenz |
|---|---|---|
| Installiertes SDK | 10.0.303, in `global.json` fixiert | Bisherige lokale Build-Basis; keine aktuelle Wartungsfreigabe |
| Unterstütztes SDK-Update | 10.0.402, veröffentlicht 13.10.2026; Runtime 10.0.13 | Für die nächste Werkzeugaktualisierung ausgewählt, noch nicht installiert |
| SDK-Wartung | 10.0.3xx endete August 2026; 10.0.4xx wird weiter gewartet | SDK-Befund vor P1.03-Freigabe schließen |
| IDE | Visual Studio Community 2026, 18.8.3 | `net10.0` erfordert mindestens VS 18.0; ein IDE-Build mit dem aktualisierten SDK ist noch zu prüfen |
| Ziel | .NET ≥10, Debian ≥13, ARM64 | Offiziell unterstützte Kombination; tatsächliche Projektlaufzeit noch nicht nachgewiesen |

Quellen: [Microsoft SDK-Download](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), [SDK-/Visual-Studio-Zuordnung und Wartung](https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs), [.NET-10-OS-Matrix](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md). Angaben am 15.10.2026 geprüft. Die beim SDK genannte mitgelieferte Visual-Studio-Version ist nicht automatisch die minimale kompatible IDE-Version.

Die Arbeitskopie bleibt mit SDK 10.0.303 baubar. `global.json` wird erst nach Installation und Prüfung des ausgewählten Updates umgestellt, damit das bloße Audit keinen fehlenden-SDK-Fehler erzeugt. Dieser bewusst erhaltene Ausgangsstand ist keine Zustimmung zum dauerhaften Betrieb eines abgelaufenen SDK-Bandes. Vor Freigabe in P1.03: SDK aktualisieren, `global.json` fixieren, Locked-Restore und Release-Build sowie IDE-Nutzung prüfen.

## Vollständiger Paketgraph

Die Bibliothek hat aktuell **keine externen Laufzeitpakete**. Das Testprojekt enthält **6 direkte und 10 transitive Pakete**. Alle sechs direkten Referenzen verwenden `PrivateAssets="all"`; das Testprojekt ist nicht packbar. Die [Abhängigkeitsliste](../../DEPENDENCIES.md) enthält alle 16 Pakete, Versionen, Lizenzen und Quellen. Die [maschinenlesbare Inventur](P1_02_DEPENDENCY_INVENTORY.json) ergänzt Metadaten-/Lizenzprüfsummen und die tatsächliche Auswahl von Compile-/Runtime-Assets.

Alle sechs direkten Versionen entsprechen dem jeweils neuesten stabilen Stand des offiziellen NuGet-Versionsindex. Transitive Versionen werden entsprechend den Paketanforderungen aufgelöst und durch Lockdateien fixiert; sie werden nicht unabhängig vom Elternpaket ohne Bedarf hochgesetzt. Insbesondere kommt `System.Diagnostics.EventLog` 6.0.0 über Castle.Core. Die NuGet-Abfragen für bekannte Schwachstellen und Abkündigungen waren erfolgreich und meldeten keine betroffenen Pakete. Das ist eine zeitpunktbezogene Quellenprüfung, keine Garantie gegen unbekannte Schwachstellen.

## Lizenzen und Verteilung

Die `.nuspec`-Lizenzangaben aller 16 tatsächlich aufgelösten Pakete wurden geprüft; die eingebettete FluentAssertions-Lizenz wurde gelesen. Die Pakete stehen unter MIT, BSD-3-Clause, Apache-2.0 oder der Xceed Community License. Die heutige private, nichtkommerzielle Entwicklung des Open-Source-Projekts mit testexklusiven Werkzeugen ist berücksichtigt; daraus folgt keine pauschale Freigabe für beliebige kommerzielle Mitwirkende.

FluentAssertions 8.11.0 ist **nicht MIT**. Die [versionsgebundene Lizenz](https://www.nuget.org/packages/FluentAssertions/8.11.0/License) nennt Open-Source-/persönliche Entwicklung als erlaubten Kontext und enthält Einschränkungen für kommerzielle Nutzung und Weiterverteilung. WeatherParser darf seine eigenen Quellen weiterhin unter MIT führen; fremde Pakete werden dadurch nicht umlizenziert. Drittanbieterhinweise in Microsoft.CodeCoverage und TestHost erhalten, falls diese Werkzeuge später weiterverteilt werden. Die derzeitige Bibliothek verteilt weder diese Testpakete noch deren Binärdateien.

## Plattform- und Testwerkzeugprüfung

| Bereich | Nachweis | Verbleibende Laufzeitprüfung |
|---|---|---|
| `net10.0`-Paketauflösung | Assets aus `net6.0`, `net8.0`, `net9.0` und `netstandard2.0` wurden kompatibel ausgewählt | Sinnvolle Verhaltenstests in P1.05/P1.06 |
| Test-Runner | Microsoft.NET.Test.Sdk und MSTest.TestAdapter für VSTest vorhanden; Microsoft.CodeCoverage ist der gewählte Collector | Echte Testausführung und Reports in P1.05/P1.07 |
| Debian/ARM64 | .NET 10 offiziell unterstützt; Bibliothek derzeit ohne externe/native Pakete | Tatsächlicher Paketkonsum/Lauf auf Debian 13 ARM64 in P1.10/P9 |
| Coverage auf Debian/ARM64 | Statische verwaltete Instrumentierung offiziell unterstützt; dynamischer Linux-ARM64-Profiler nicht unterstützt | Statische Instrumentierung und echte Reportkennzahlen in P1.07/P1.10 verifizieren |
| EventLog | Windows-spezifische Funktion, nur im transitiven Testgraphen | Unter Debian keine Windows-EventLog-Funktionen voraussetzen |

Quellen: [Microsoft-CodeCoverage-Plattformmatrix](https://github.com/microsoft/codecoverage/blob/main/docs/supported-os.md), [Instrumentierungsarten](https://github.com/microsoft/codecoverage/blob/main/docs/instrumentation.md). Die ARM64-DLLs im untersuchten Paket sind Windows-Binärdateien; daraus wird kein Linux-ARM64-Profiler abgeleitet. Statische Instrumentierung benötigt diesen dynamischen Profiler nicht. Zeilen-/Block-/Branch-Kennzahlen werden weiterhin nicht gleichgesetzt.

MSTest bringt die transitive Kette Telemetry → ApplicationInsights mit. Das Vorhandensein allein beweist keine Übertragung. Für die Diagnosen wurden `DOTNET_CLI_TELEMETRY_OPTOUT=1` und `TESTINGPLATFORM_TELEMETRY_OPTOUT=1` gesetzt; beide Einstellungen sind auch für künftige lokale/CI-Tests dokumentiert. [Microsoft-Telemetrieeinstellungen](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-telemetry).

## Prüfungen und Übergabe

- Installierten SDK-/Runtime-/Visual-Studio-Stand erfasst; SDK-Wartung mit Primärquellen abgeglichen.
- Beide Lockdateien, zentral fixierte Versionen, Test-Referenzgrenzen, Metadaten, Lizenzen und tatsächlich gewählte Assets geprüft.
- Aktuelle direkte NuGet-Versionen sowie Schwachstellen-/Abkündigungsabfragen geprüft.
- Keine Build-Wiederholung allein wegen Dokumentationsänderungen: Der vorherige erfolgreiche Locked-Restore/Release-Build gehört zum unveränderten Ausgangsstand. Keine Testmethoden oder Coverage-Werte erfunden.
- Paket-, Projekt- und SDK-Dateien unverändert erhalten; kein Commit, Push oder Zielsystemzugriff ausgeführt.

**P1.02-Prüfung abgeschlossen, SDK-Befund offen.** Nächste Voraussetzung für P1.03 ist die kontrollierte SDK-Aktualisierung und Prüfung. Lizenz-/Versionsergebnisse gelten für die erfassten Pakete; neue DI-/Fachabhängigkeiten werden erst bei Aufnahme erneut geprüft. Coverage-Abnahme bleibt P1.07/P1.08, echte ARM64-Abnahme P1.10/P9.
