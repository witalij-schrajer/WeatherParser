# Entwicklungsabhängigkeiten

[Deutsch](DEPENDENCIES.md) | [English](DEPENDENCIES.en.md)

Prüfstand: 15.10.2026. P1.02-Prüfung abgeschlossen; SDK-Aktualisierung offen. Die Bibliothek besitzt keine externen Laufzeitpakete. Tests lösen sechs direkte und zehn transitive Pakete auf. Alle direkten Versionen entsprechen beim Prüfstand den neuesten stabilen NuGet-Versionen. Aufgelöste Versionen/Hashes bleiben in den Projekt-Lockdateien fixiert.

| Paket | Version | Umfang | Deklarierte Lizenz |
|---|---|---|---|
| [FluentAssertions](https://www.nuget.org/packages/FluentAssertions/8.11.0) | 8.11.0 | Direkt, Tests | Xceed Community License (non-commercial) |
| [Microsoft.CodeCoverage](https://www.nuget.org/packages/Microsoft.CodeCoverage/18.11.0) | 18.11.0 | Direkt, Tests | MIT |
| [Microsoft.NET.Test.Sdk](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.11.0) | 18.11.0 | Direkt, Tests | MIT |
| [Moq](https://www.nuget.org/packages/Moq/4.21.0) | 4.21.0 | Direkt, Tests | BSD-3-Clause |
| [MSTest.TestAdapter](https://www.nuget.org/packages/MSTest.TestAdapter/4.5.1) | 4.5.1 | Direkt, Tests | MIT |
| [MSTest.TestFramework](https://www.nuget.org/packages/MSTest.TestFramework/4.5.1) | 4.5.1 | Direkt, Tests | MIT |
| [Castle.Core](https://www.nuget.org/packages/Castle.Core/5.2.1) | 5.2.1 | Transitiv, Tests | Apache-2.0 |
| [Microsoft.ApplicationInsights](https://www.nuget.org/packages/Microsoft.ApplicationInsights/2.23.0) | 2.23.0 | Transitiv, Tests | MIT |
| [Microsoft.Testing.Extensions.Telemetry](https://www.nuget.org/packages/Microsoft.Testing.Extensions.Telemetry/2.5.1) | 2.5.1 | Transitiv, Tests | MIT |
| [Microsoft.Testing.Extensions.TrxReport.Abstractions](https://www.nuget.org/packages/Microsoft.Testing.Extensions.TrxReport.Abstractions/2.5.1) | 2.5.1 | Transitiv, Tests | MIT |
| [Microsoft.Testing.Platform](https://www.nuget.org/packages/Microsoft.Testing.Platform/2.5.1) | 2.5.1 | Transitiv, Tests | MIT |
| [Microsoft.Testing.Platform.MSBuild](https://www.nuget.org/packages/Microsoft.Testing.Platform.MSBuild/2.5.1) | 2.5.1 | Transitiv, Tests | MIT |
| [Microsoft.TestPlatform.ObjectModel](https://www.nuget.org/packages/Microsoft.TestPlatform.ObjectModel/18.11.0) | 18.11.0 | Transitiv, Tests | MIT |
| [Microsoft.TestPlatform.TestHost](https://www.nuget.org/packages/Microsoft.TestPlatform.TestHost/18.11.0) | 18.11.0 | Transitiv, Tests | MIT |
| [MSTest.Analyzers](https://www.nuget.org/packages/MSTest.Analyzers/4.5.1) | 4.5.1 | Transitiv, Tests | MIT |
| [System.Diagnostics.EventLog](https://www.nuget.org/packages/System.Diagnostics.EventLog/6.0.0) | 6.0.0 | Transitiv, Tests | MIT |

## Entscheidung und Grenzen

FluentAssertions 8 ist nicht MIT. Aktuelle private/nichtkommerzielle Open-Source-Testnutzung wurde anhand der [versionsgebundenen Lizenz](https://www.nuget.org/packages/FluentAssertions/8.11.0/License) geprüft; kommerzielle Kontexte und Weiterverteilung separat bewerten. Fremde Lizenzen/Hinweise erhalten. Die Werkzeuge bleiben ausschließlich Testabhängigkeiten (`PrivateAssets="all"`, nicht packbares Testprojekt), außerhalb der Bibliotheks-Laufzeitabhängigkeiten.

NuGet-Schwachstellen-/Abkündigungsabfragen waren erfolgreich und meldeten am Prüftag keine betroffenen Pakete. Transitive Versionen folgen den Elternanforderungen und bleiben fixiert; keine unabhängigen Updates erzwingen. Neue Abhängigkeiten benötigen eine erneute Versions-/Lizenzprüfung.

Installiertes und fixiertes SDK: 10.0.303, dessen Featureband nicht mehr gewartet wird. Ausgewähltes Update: **10.0.402**, noch nicht installiert. `global.json` bis zur Installation und Prüfung unverändert lassen. Diesen Befund vor der Werkzeugfreigabe in P1.03 schließen. [Microsoft-Wartungsmatrix](https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs), [SDK-Downloads](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).

Visual Studio 2026 18.8.3 erfüllt das dokumentierte net10.0-Minimum; ein IDE-Build nach SDK-Update ist noch nicht nachgewiesen. Debian 13 ARM64 wird von .NET 10 offiziell unterstützt. Microsoft.CodeCoverage dokumentiert dort verwaltete statische Erfassung; dynamische Linux-ARM64-Erfassung wird nicht unterstützt. Tatsächlicher Projektlauf und Coverage-Nachweis bleiben Folgearbeit und ergeben sich nicht allein aus kompatiblen NuGet-Assets.

Vollständige Nachweise, Asset-Auswahl, Lizenzgrenzen, Telemetrieeinstellungen und Folgearbeiten: [P1.02-Prüfbericht](docs/p1/P1_02_TOOLING_REVIEW.md), [Inventur](docs/p1/P1_02_DEPENDENCY_INVENTORY.json).