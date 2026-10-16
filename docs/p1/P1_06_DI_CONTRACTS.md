# P1.06 – Minimal benötigte Paket-/DI-Verträge

## Ziel

Testgetriebene Absicherung der minimal benötigten Dependency-Injection-Oberfläche der
Bibliothek: ein einzelner, öffentlicher Registrierungs-Einstiegspunkt für
`Microsoft.Extensions.DependencyInjection`-Konsumenten, belegt durch echte
`ServiceProvider`-Auflösung, die nachweist, dass fehlende Registrierungen erkannt und nicht
stillschweigend toleriert werden (`T-P1-04`, `T-P1-05`).

## Entwurf

- Die Bibliothek (`src/WeatherParser.Parsing`) referenziert jetzt nur noch
  `Microsoft.Extensions.DependencyInjection.Abstractions` (nicht das volle DI-Container-Paket),
  da produktiver Code lediglich `IServiceCollection` benötigt. Der konkrete Container
  (`Microsoft.Extensions.DependencyInjection`) bleibt eine reine Testabhängigkeit, um in Tests
  einen echten `ServiceProvider` aufzubauen.
- `ServiceCollectionExtensions.AddWeatherParsing(this IServiceCollection services)` ist der
  einzige öffentliche Registrierungs-Einstiegspunkt. Er registriert:
  - `IEndpointUriValidator` → `EndpointUriValidator` (Singleton)
  - `IParserEndpointValidator` → `ParserEndpointValidator` (Singleton)

Es existieren noch keine weiteren Services, daher wurden keine weiteren Registrierungen
ergänzt; die Erweiterungsmethode wächst mit zukünftigem Verhalten (Parser, Serializer) in
späteren Phasen mit.

## Red → Green → Refactor

1. **Red**: `ServiceCollectionExtensionsTests` wurde zuerst geschrieben und prüft, dass die
   Auflösung von `IParserEndpointValidator` aus einer leeren `ServiceCollection` sowie aus einer
   `ServiceCollection` mit nur `IParserEndpointValidator` (ohne dessen Abhängigkeit
   `IEndpointUriValidator`) jeweils eine `InvalidOperationException` wirft. Die Testdatei ließ
   sich erst nicht kompilieren, da `AddWeatherParsing` noch nicht existierte.
2. **Green**: `ServiceCollectionExtensions.AddWeatherParsing` wurde mit den minimal notwendigen
   Registrierungen implementiert. Zwei weitere Tests wurden ergänzt und bestanden: Die Auflösung
   von `IParserEndpointValidator` gelingt nach Aufruf von `AddWeatherParsing`, und die aufgelöste
   Instanz führt eine echte, korrekte Endpunktvalidierung (`ValidateAndMap`) durch.
3. **Refactor**: Die frühere `ParserEndpointValidatorDiTests` (aus P1.05) duplizierte Abdeckung,
   die nun vollständiger durch `ServiceCollectionExtensionsTests` bereitgestellt wird, und wurde
   entfernt, um redundante Tests zu vermeiden.

## Testfälle

| Test | Verhalten |
|---|---|
| `GetRequiredService_WithoutAnyRegistration_ThrowsInvalidOperationException` | Keine Registrierung → fehlende Registrierung erkannt |
| `GetRequiredService_WithOnlyParserEndpointValidatorRegistered_ThrowsInvalidOperationException` | Teilweise Registrierung (fehlender `IEndpointUriValidator`) → fehlende Registrierung erkannt |
| `AddWeatherParsing_WithoutFurtherRegistration_ResolvesParserEndpointValidator` | Vollständige Registrierung über den öffentlichen Einstiegspunkt löst erfolgreich auf |
| `AddWeatherParsing_ResolvedValidator_PerformsRealValidation` | Die aufgelöste Instanz ist nicht nur auflösbar, sondern funktional korrekt |

Alle Tests laufen gegen einen In-Memory-`ServiceProvider`; es findet kein Netzwerk-, Datei- oder
Gerätezugriff statt.

## Akzeptanzkriterien

- Die Bibliothek referenziert in produktivem Code nur DI-Abstraktionen, nicht den konkreten
  DI-Container.
- `AddWeatherParsing` ist der einzige öffentliche Registrierungs-Einstiegspunkt für die bisher
  eingeführten Services.
- Fehlende Registrierungen werden nachweislich über `InvalidOperationException` erkannt (keine
  stille `null`-Auflösung).
- Vollständige Registrierung löst einen funktional korrekten `IParserEndpointValidator` auf.
- Bibliotheks-, Unit-Test- und Paketkonsument-Projekte bauen erfolgreich; alle 8 Unit-Tests
  bestehen.

## Verifikation

```powershell
dotnet build src/WeatherParser.Parsing/WeatherParser.Parsing.csproj
dotnet build tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj
dotnet build tests/WeatherParser.Package.Tests/WeatherParser.Package.Tests.csproj
dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj
```

Ergebnis: alle Projekte bauen; 8 Tests ausgeführt, 8 bestanden, 0 fehlgeschlagen.
