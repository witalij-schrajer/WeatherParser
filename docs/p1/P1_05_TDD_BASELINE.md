# P1.05 – TDD-Basislinie: Validierung von Parser-Endpunkten

## Ziel

Aufbau einer echten, offline ausführbaren, nach Red→Green→Refactor entwickelten TDD-Basis mit
MSTest, Moq und FluentAssertions, belegt an einem realen Bibliotheksverhalten: Validierung und
Zuordnung von Parser-Endpunktdefinitionen (`T-P1-01` bis `T-P1-04`, `T-P1-06`, `T-P1-07`).

## Getestetes Verhalten

Ein Parser wird mit einem oder mehreren benannten Endpunkten konfiguriert
(`ParserEndpointDefinition`, jeweils mit `Name` und `Uri`). Die Bibliothek muss:

- einen Endpunkt ablehnen, dessen URI keine gültige absolute HTTP/HTTPS-URI ist, mit einem
  definierten englischen Fehlertext und ohne jeden Netzwerkzugriff (`T-P1-02`),
- zwei Endpunkte mit demselben Namen ablehnen, da sonst eine Endpunktkonfiguration eine andere
  stillschweigend ersetzen könnte (`T-P1-03`),
- jede gültige, eindeutig benannte Endpunktdefinition ihrem Namen zuordnen, ohne unterschiedliche
  Endpunkte zu verwechseln (`T-P1-03`).

## Entwurf

- `IEndpointUriValidator` / `EndpointUriValidator` – prüft, ob eine URI absolut ist und das
  Schema `http` oder `https` verwendet.
- `ParserEndpointDefinition` – ein unveränderlicher Record, der einen benannten Endpunkt und
  seine URI beschreibt.
- `IParserEndpointValidator` / `ParserEndpointValidator` – nimmt eine Sammlung von
  Endpunktdefinitionen entgegen, validiert jede URI über `IEndpointUriValidator` und liefert eine
  Zuordnung vom Endpunktnamen zur Definition. Wirft `ParserEndpointValidationException`
  (englischer Fehlertext) bei ungültiger URI oder doppeltem Namen.

Alle Abhängigkeiten werden per Konstruktor injiziert (`T-P1-04`); es gibt keine versteckten
Service-Lookups oder statischen Abhängigkeiten.

## Red → Green → Refactor

1. **Red**: `ParserEndpointValidatorTests` wurde zuerst gegen die noch nicht existierenden
   Verträge `ParserEndpointValidator` / `IEndpointUriValidator` geschrieben und deckt einen
   einzelnen gültigen Endpunkt, zwei unterschiedliche gültige Endpunkte, eine ungültige URI und
   einen doppelten Endpunktnamen ab. Diese Tests ließen sich erst nach Erstellung der
   Produktionstypen kompilieren.
2. **Green**: `IEndpointUriValidator`, `EndpointUriValidator`, `ParserEndpointDefinition`,
   `IParserEndpointValidator`, `ParserEndpointValidator` und `ParserEndpointValidationException`
   wurden mit der minimal notwendigen Logik implementiert. Alle 6 Tests sind grün.
3. **Refactor**: Ein weiteres Refactoring war nicht erforderlich; die Implementierung ist bereits
   minimal, und jede Klasse hat eine einzige, klar abgegrenzte Verantwortung.

## Testaufbau und Verify-Disziplin (T-P1-06)

`ParserEndpointValidatorTests` verwendet `[TestInitialize]`, um ein strenges Moq-Mock von
`IEndpointUriValidator` sowie das zu testende Objekt zu erzeugen, und `[TestCleanup]`, um
`VerifyAll()` auf dem Mock aufzurufen, damit sichergestellt ist, dass jede konfigurierte Erwartung
tatsächlich aufgerufen wurde.

## Dependency Injection (Abgrenzung zu T-P1-04/P1.06)

`ParserEndpointValidatorDiTests` zeigt, dass `IParserEndpointValidator` und
`IEndpointUriValidator` aus einem echten `Microsoft.Extensions.DependencyInjection`-
`ServiceProvider` aufgelöst werden können, wenn sie korrekt registriert sind, und dass die
Auflösung mit `InvalidOperationException` fehlschlägt, wenn eine benötigte Abhängigkeit
(`IEndpointUriValidator`) fehlt. Dies ist nur eine eng begrenzte, vorausschauende Prüfung; die
vollständige DI-Registrierung der gesamten Anwendung bleibt Aufgabe von P1.06 (`T-P1-05`).

## Offline-Ausführung (T-P1-07)

Kein Test dieser Basislinie führt HTTP-, MQTT- oder Gerätezugriffe aus. Alle Abhängigkeiten
werden entweder gemockt (Moq) oder über `ServiceCollection` im Arbeitsspeicher aufgelöst.

## Verifikation

```powershell
dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj
```

Ergebnis: 6 Tests ausgeführt, 6 bestanden, 0 fehlgeschlagen.
