# Eine Datenquelle hinzufügen

> Deutsche Übersetzung von [docs/EXTENDING_DATA_SOURCES.md](../EXTENDING_DATA_SOURCES.md). Maßgeblich ist die englische Fassung; die Übersetzung wird vor jeder neuen Version mit ihr abgeglichen und kann bis dahin zurückliegen. Übersetzungsstand: englische Fassung von `features/develop/release-1.0.0` `8659f5b` (2026-11-03, Version 1.0.0). Die verlinkten Verträge und Entwicklerdokumente sind englisch.

Diese Anleitung richtet sich an Entwickler, die möchten, dass WeatherParser eine weitere Wetterdatenquelle liest. Sie nennt, was vor dem ersten Code entschieden werden muss, wie die bestehenden Quellen aufgebaut sind, welche Tests und Nachweise erforderlich sind und was dokumentiert werden muss. Sie beschreibt Anforderungen und bestehende Muster. Sie genehmigt keine neue Quelle, und außer den DWD-CDC-Beobachtungen, OsthessenWetter und DWD MOSMIX gibt es keine Quelle in der Bibliothek.

## 1. Zwei Wege, eine Quelle hinzuzufügen

| Weg | Wo der Code liegt | Registrierung | Wann Sie ihn wählen |
| --- | --- | --- | --- |
| **Beitrag zur Bibliothek** | `src/WeatherParser.Parsing`, über einen Pull Request nach `develop` | Zentral in `AddWeatherParsing` | Die Quelle ist für alle Konsumenten nützlich, ihre Bedingungen erlauben es, und Sie folgen dem Arbeitsablauf des Repositorys ([CONTRIBUTING.md](../../CONTRIBUTING.md), [AGENTS.md](../../AGENTS.md)) |
| **Adapter in Ihrer Anwendung** | Ihr eigenes Projekt, das das Paket `WeatherParser.Parsing` nutzt | Ihr Kompositionsstamm, neben der [Registrierung der Bibliothek](LIBRARY_USAGE.md#3-die-bibliothek-registrieren) | Sie brauchen die Quelle jetzt oder nur für Ihre eigene Verwendung |

Die Bibliothek hat keine Plugin-API, keine universelle Quellschnittstelle und keine automatische Erkennung: Nichts durchsucht Assemblies oder lädt Adapter automatisch. Ein Adapter Ihrer Anwendung ist eine gewöhnliche Klasse Ihrer Anwendung, die die öffentlichen Bausteine der Bibliothek verwendet (Abschnitt 4) und die Exportentitäten der Bibliothek erzeugt, sodass die Serialisierer der Bibliothek ihre Ergebnisse akzeptieren.

## 2. Um welche Art von Änderung handelt es sich?

| Änderung | Was sie erfordert |
| --- | --- |
| Ein neuer **Beobachtungs**anbieter, dessen Größen es im Exportvertrag bereits gibt (Temperatur, Luftfeuchte, Luftdruck, Windgeschwindigkeit und -richtung, Regenrate, Globalstrahlung, Beleuchtungsstärke) | Einen Quellenvertrag (Abschnitt 3) und eine Implementierung, die über `IObservationDatasetBuilder` auf `ObservationDataset` abbildet |
| Eine neue **Größe**, ein neues Element einer Exportentität oder ein neuer Wert einer bestehenden Enumeration | Eine Änderung des Exportvertrags mit eigener Entscheidung und eigenem Review: Version 1 akzeptiert nur optionale Ergänzungen ([Exportvertrag](../p2/P2_EXPORT_CONTRACT.md)), das XML-Schema wird neu erzeugt, und Konsumenten mit vollständigen Fallunterscheidungen sind betroffen |
| Ein neuer **Vorhersage**anbieter | Ebenfalls eine Vertragsänderung: `ForecastRun.Product` ist die Enumeration `MosmixProduct`, und `IForecastRunBuilder` bildet `source` nur als `dwd-mosmix-l:<id>` oder `dwd-mosmix-s:<id>`; eine Vorhersage eines anderen Anbieters lässt sich also ohne Erweiterung des Vertrags nicht ausdrücken |
| Ein neuer Typ von **Stationsliste** | Eine neue Export-Wurzel, also eine Vertragsänderung, und ein neuer MQTT-Nachrichtentyp |

Verwenden Sie ein bestehendes Exportelement nie für eine andere physikalische Größe. Beleuchtungsstärke in Lux ist zum Beispiel keine Globalstrahlung in W/m², und eine Böe ist keine mittlere Windgeschwindigkeit.

## 3. Zuerst den Quellenvertrag festhalten

Halten Sie vor der Umsetzung die Fakten der Quelle und die daraus folgenden Entscheidungen fest, wie es die bestehenden Verträge tun ([DWD-Beobachtungen](../p5/P5_OBSERVATION_CONTRACT.md), [OsthessenWetter](../p6/P6_OSTHESSEN_CONTRACT.md), [MOSMIX-Vorhersagen](../p7/P7_FORECAST_CONTRACT.md)). Jeder Fakt stammt aus der offiziellen Dokumentation des Anbieters oder aus den tatsächlich veröffentlichten Dateien; halten Sie die URL, das Abrufdatum und bei Beispieldateien die Größe und den SHA-256 fest. Schreiben Sie „nicht dokumentiert“, wo die Quelle nichts sagt, und füllen Sie die Lücke nie mit einer Annahme, die als Fakt dargestellt wird.

- **Identität und Umfang**: Name des Anbieters, Beobachtung oder Vorhersage, die Kennung der Station oder des Standorts (als Zeichenkette gehalten, mit führenden Nullen) und der Wert `source`, den Ihre Ergebnisse tragen. Beobachtungsdatensätze tragen `<provider>:<station_id>`, zum Beispiel `dwd:00044` oder `osthessenwetter:<Stations-ID>`.
- **Zugang und Bedingungen** (REQ-092): Lizenz, Text der Quellenangabe, Regeln zur Weiterverbreitung, ob ein automatisierter Abruf erlaubt ist, dokumentierte Ratenbegrenzungen oder eine klar als solche gekennzeichnete Empfehlung des Projekts, und was der Anbieter über Anfragende protokolliert. Eine Quelle ohne geklärte Bedingungen darf mit synthetischen Daten umgesetzt und getestet werden, ihre produktive Nutzung bleibt aber offen, wie bei OsthessenWetter (OW-14).
- **Endpunkte und Authentifizierung**: jede URL, die der Parser benötigt, als eigene benannte Option; wer sie bereitstellt (immer die nutzende Anwendung, nie ein eingebauter Standard); gegebenenfalls Zugangsdaten und wie Diagnosen vermeiden, sie oder sensible Query-Strings preiszugeben.
- **Format**: Medientyp, Kodierung, Schema- oder Formatversion, Archivstruktur und Elementnamen, beobachtete Höchstgrößen und wie eine Formatänderung erkannt wird.
- **Werte**: Einheit, Genauigkeit, Kennungen für fehlende Werte, Qualitätskennzeichen und ob jeder Wert ein Momentanwert, ein Mittel, ein Maximum oder eine Summe ist, über welchen Zeitraum.
- **Zeiten**: Zeitzone, Genauigkeit, ob das Datum angezeigt wird, wie Mess-, Ausgabe-, Abruf- und Gültigkeitszeiten zusammenhängen. Lokale Zeiten werden nach UTC umgerechnet; ein abgeleitetes Datum wird als `estimated_date` gekennzeichnet (REQ-042).
- **Abbildung und Fehler**: das Exportelement je Quellwert, Umrechnungen und ihre Bezugszeiträume, welche Werte weggelassen werden und warum, was den ganzen Aufruf fehlschlagen lässt, was ein Teilfehler ist, und die stabilen Fehler- und Hinweis-IDs.

In diesem Repository ist ein Quellenvertrag ein Entscheidungsprotokoll, das der Maintainer vor der Umsetzung nach seinen Review-Runden genehmigt.

## 4. Auf den bestehenden Mustern aufbauen

Jede Klasse mit Logik implementiert ihre eigene zweckbestimmte Schnittstelle und erhält ihre Abhängigkeiten über den Konstruktor (REQ-070, REQ-071, REQ-078). Abruf, Lesen des Formats, Abbildung und Steuerung sind getrennt, wo sich ihre Aufgaben unterscheiden. Teilen Sie eine Klasse nur, wo eine Aufgabe es rechtfertigt, nicht um einem Diagramm zu entsprechen. Die bestehenden Quellen zeigen drei Formen:

| Quelle | Einstiegspunkt | Abruf | Lesen und Abbilden | Ergebnis |
| --- | --- | --- | --- | --- |
| DWD-10-Minuten-Beobachtungen | `IDwdObservationParser.ParseAsync(stationId, ct)` | `IDwdObservationSource` (vier Stationslisten und bis zu vier Archive) | `IDwdStationResolver`, `IDwdObservationMapper` | `ObservationDataset` |
| OsthessenWetter | `IOsthessenWetterParser.ParseAsync(stationNamePattern, ct)` | `IOsthessenWetterPageSource` (eine Seite) | `IOsthessenWetterPageReader`, `IOsthessenWetterValueReader`, `IOsthessenWetterTimeReader`, `IOsthessenWetterStationIdentity`, `IOsthessenWetterObservationMapper` | `OsthessenWetterParseResult` (Datensätze und Fehler je Station) |
| MOSMIX_L und MOSMIX_S | `IMosmixLParser`, `IMosmixSParser` (getrennt, ohne Produktschalter) | `IMosmixLForecastSource`, `IMosmixSForecastSource` | `IMosmixForecastDocumentReader`, `IMosmixElementMapper`, `IMosmixUnitScaler` hinter der gemeinsamen `IMosmixParsePipeline` | `MosmixParseResult` |

Jede Parserschnittstelle hat ihre eigene Signatur und ihr eigenes Ergebnismodell. Eine neue Quelle erhält ihre eigene Schnittstelle, die zu ihren Kennungen und ihrem Fehlermodell passt; sie muss nicht wie die anderen aussehen.

Verwenden Sie diese öffentlichen Bausteine wieder, statt ihre Regeln zu kopieren:

| Baustein | Zweck |
| --- | --- |
| `ICatalogDownloader` mit `CatalogRequest` | Ein begrenzter GET: maximale Bytes, Anfrage-Timeout, Frist-Token, optional „404 bedeutet nicht vorhanden“, stabile Abruffehler, keine Wiederholungen |
| `IParserEndpointValidator`, `IEndpointUriValidator` | Validierung der Endpunkt-URIs bei der Konstruktion |
| `ISourceTimeNormalizer`, `ITimeZoneResolver` | Umrechnung vollständiger oder lokaler Quellzeiten nach UTC und Ableitung des Datums mit Schätzkennzeichen |
| `IQuantityValidator`, `IPressureConverter`, `IPeriodRateConverter`, `IWindSpeedConverter`, `IOutputRounding` | Plausibilität, Druckeinheiten aus einem Originalwert, Umrechnung von Mengen in Raten mit ihrem Zeitraum, Windeinheiten, Ausgaberundung mit Mitten von null weg |
| `IObservationDatasetBuilder` mit `ObservationInput` und `ObservationReading` | Baut das `ObservationDataset` (Quelle, Standort, UTC-Zeiten je Größe, Zeiträume, Qualität, Auslassungen) und meldet `OmittedReadings` |
| `IJsonExportSerializer`, `IXmlExportSerializer`, `IMqttMessageSerializer` | Quellunabhängige Ausgabe; eine neue Quelle braucht nie einen eigenen Serialisierer |

HTTP, Zeit, Dateisystem und Logging werden injiziert (`HttpClient`, `TimeProvider`, `ILogger<T>`). Die Bibliothek liest keine Konfigurationsdateien, Umgebungsvariablen oder Befehlszeilenargumente; Endpunkte und Optionen kommen von der Anwendung als registrierte Optionsobjekte.

### Abbildungsregeln

- Zeitstempel sind intern und in der Ausgabe UTC (`YYYY-MM-DDTHH:mm:ss`, ohne Suffix). Jede Größe behält ihre eigene Messzeit; Werte mit unterschiedlichen Zeiten werden nie zu einem irreführenden Zeitstempel zusammengeführt.
- Ein fehlender oder ungültiger Wert wird mit Grund weggelassen; `0` ist ein echter Wert. Die Kennung eines Anbieters für fehlende Werte wird nie exportiert.
- Rechnen Sie aus dem Originalwert um und runden Sie nur für die Ausgabe. Luftdruck in hPa und mmHg stammt aus demselben ungerundeten Wert mit seiner Referenz. Rechnen Sie Raten und Energien nur mit einem verifizierten Bezugszeitraum um.
- Behalten Sie Qualitätsangaben so, wie die Quelle sie angibt, und halten Sie Kennungen als Zeichenketten.

### Abruf- und Ressourcengrenzen

- Jede Anfrage ist in Größe und Zeit begrenzt und beachtet das `CancellationToken` des Aufrufers. Ein Parse-Vorgang hat eine Option für eine Gesamtfrist, die bei der Konstruktion validiert wird. Der Abbruch durch den Aufrufer hat Vorrang vor der Frist, die wiederum Vorrang vor jedem anderen Fehler hat.
- Keine Wiederholungen innerhalb der Bibliothek, kein Scheduler, kein Hintergrund-Timer. Ein Aufruf ist ein Abruf; Zeitplanung und Wiederholungen gehören zur nutzenden Anwendung.
- XML wird ohne DTD-Verarbeitung und ohne externe Entitäten gelesen. Archive werden mit begrenzter Anzahl, Größe und Expansion der Elemente gelesen, und Elementnamen werden nie als Dateipfade verwendet. Wenden Sie auf andere Formate gleichwertige Grenzen an, zum Beispiel HTML, das ohne Skripte und ohne Laden von Ressourcen geparst wird, wie bei OsthessenWetter.
- Protokollieren Sie stabile Ereignisnamen mit der Quelle, der Kennung und UTC-Zeiten, nie Messwerte, Zugangsdaten, Nutzdaten oder Query-Strings.

## 5. Registrierung

- **Beitrag zur Bibliothek**: Registrieren Sie jeden neuen Dienst in `AddWeatherParsing`. Weil `AddWeatherParsing` alle Quellen registriert und die Container-Validierung sie alle prüft, wird jeder neue Options- oder Endpunkttyp zu einer Pflichtregistrierung für jeden Konsumenten, auch für Konsumenten, die die neue Quelle nie verwenden. Führen Sie ihn in der [Anleitung zur Bibliotheksnutzung](LIBRARY_USAGE.md#3-die-bibliothek-registrieren) und in ihrer Beispielregistrierung (die die Pakettests prüfen), in der README-Referenz und im Changelog auf, als Breaking Change für Konsumenten, die ihren Container validieren.
- **Adapter Ihrer Anwendung**: Registrieren Sie Ihre Klassen in Ihrem Kompositionsstamm nach `AddMyWeatherParsing` (oder Ihrer Entsprechung) und lösen Sie die verwendeten Bausteine der Bibliothek über ihre Schnittstellen auf.

Die Bibliothek unterstützt nicht, nur einige Quellen zu registrieren; dokumentieren Sie keine Teilregistrierung und bauen Sie nicht darauf auf.

## 6. Ausgabe, MQTT und die Beispiel-CLI

Die Serialisierer sind quellunabhängig: Der JSON- und der XML-Serialisierer akzeptieren die Export-Wurzeln `ObservationDataset`, `ForecastRun`, `CdcStationList`, `MosmixLocationList` und den Umschlag `ExportDocument`, der MQTT-Serialisierer dieselben Wurzeln ohne `ExportDocument` (ein Umschlag schlägt mit `MQTT-MESSAGE-TYPE-UNSUPPORTED` fehl; veröffentlichen Sie seine Datensätze und Läufe einzeln). Unabhängig vom Anbieter wird ein gültiger Datensatz akzeptiert; ein neuer Beobachtungsanbieter braucht also keine Änderung am Serialisierer. Der MQTT-Platzhalter `{provider}` wird aus dem Text von `source` vor dem ersten `:` gebildet; Konsumenten können dem neuen Anbieter also ein eigenes Topic geben. Ein neuer CLI-Befehl oder ein MQTT-Brokerclient gehört nicht zum Hinzufügen einer Quelle; das sind eigene Änderungen der Beispiel-CLI mit eigenen Vertragsentscheidungen.

## 7. Tests und Nachweise

In diesem Repository wird jede Änderung testgetrieben umgesetzt (Red, Green, Refactor) mit MSTest, Moq und FluentAssertions, und Standardtests laufen ohne Netzwerkzugriff ([CONTRIBUTING.md](../../CONTRIBUTING.md)). Für eine neue Quelle:

- **Offline-Verhaltenstests** mit einem Fake-HTTP-Handler, einem festen `TimeProvider` und synthetischen oder passend lizenzierten, bereinigten Testdaten: normale Daten, fehlende Felder und Kennungen, fehlerhafte Daten, unerwartete Einheiten und Zeitzonen, Grenzwerte auf beiden Seiten, Überlauf wo relevant, Abbruch, Fristen, HTTP-Fehler und Timeouts, zu große Antworten und Teilfehler.
- **Prüfungen der Abbildung**: umgerechnete Werte, Rundung, Zeitstempel und ihre Herkunft, Zeiträume, fehlend gegenüber null und die protokollierten IDs.
- **Vertragstests mit den echten Serialisierern**: JSON und XML desselben Ergebnisses sind gleichwertig, das XML ist gegen das mitgelieferte Schema gültig, und MQTT-Nachrichten lösen ihre Topics auf.
- **Paket- und Containertests**: Die neuen Dienste lassen sich aus der gepackten Bibliothek mit `ValidateOnBuild` auflösen, das [Beispiel der Anleitung zur Bibliotheksnutzung](LIBRARY_USAGE.md#10-wie-diese-anleitung-verifiziert-wird) baut und validiert weiterhin, und die Tests der bestehenden Quellen bestehen weiterhin.
- **Coverage und CI**: Es gelten die Gates der nativen Blockabdeckung und die CI-Jobs aus [CONTRIBUTING.md](../../CONTRIBUTING.md) Abschnitt 6. Plattformnachweise für Debian 13 ARM64 stammen aus Läufen auf dem Zielsystem, nicht aus Mocks.
- **Live-Prüfungen** gegen die echte Quelle sind getrennt, ausdrücklich genehmigt und festgehalten; sie laufen nie in den Standardtests. Simulierte Tests zeigen nicht, dass die echte Quelle erreichbar oder unverändert ist.

## 8. Dokumentation

Dokumentieren Sie in derselben Änderung: die Quelle in der Bibliotheksreferenz der README (Aufruf, Optionen, Grenzen, exportierte Größen, Fehler- und Hinweis-IDs, Quellenbedingungen und Abrufhinweise), die neuen Registrierungen in der [Anleitung zur Bibliotheksnutzung](LIBRARY_USAGE.md), die Datenquelle und ihre Quellenangabe im README-Abschnitt „Data sources“, die Änderung in [CHANGELOG.md](../../CHANGELOG.md) und jede neue oder geänderte Anforderung in [REQUIREMENTS.md](../../REQUIREMENTS.md). Eine offene Entscheidung bleibt als offen gekennzeichnet. Maßgeblich sind dabei die englischen Dokumente; die deutschen Übersetzungen in `docs/de/` werden vor der nächsten Version nachgezogen.

## 9. Eine Quelle vorschlagen

Wenn Sie eine Quelle in der Bibliothek wünschen, sie aber nicht selbst umsetzen, öffnen Sie einen Wunsch nach einer Datenquelle wie in [REPORTING_ISSUES.md](REPORTING_ISSUES.md#eine-neue-datenquelle-anfragen) beschrieben. Eine Anfrage sammelt die Fakten aus Abschnitt 3; sie ist keine Freigabe, die Quelle umzusetzen.
