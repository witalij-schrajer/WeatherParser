# WeatherParser – Anforderungen

[Deutsch](REQUIREMENTS.md) | [English](REQUIREMENTS.en.md)

**Phasenstatus (15.10.2026): P0 abgeschlossen; P1 mit Solution-/Projektstruktur begonnen. Fachimplementierung, Tests und Coverage-Abnahme stehen aus.**

Stand: 14.10.2026. Status: Anforderungen und Architekturgrundlage; noch keine Implementierung. „Muss“ bezeichnet eine verbindliche Anforderung. Als offen gekennzeichnete Punkte sind vor dem betreffenden Implementierungsschritt zu entscheiden.

## 1. Ziel und Umfang

**REQ-001** WeatherParser muss primär eine veröffentlichbare C#-NuGet-Bibliothek mit Parsern, Ausgabeentitäten, generischem MQTT-Serializer und DI-Integration bereitstellen. Eine separate CLI konsumiert das Paket als Nutzungsbeispiel und demonstriert Quellenabruf, Messdaten/Vorhersagen und MQTT-/JSON-/XML-Ausgabe.

**REQ-002** Der geplante Umfang umfasst DWD-Messdaten, OsthessenWetter und DWD-MOSMIX-Vorhersagen mit Bibliotheksfunktionen, die das CLI-Beispiel durch die fünf Befehle `SelectDWD`, `ParseDWD`, `ParseOstHessen`, `SelectMOSMIX` und `ParseMOSMIX`. Ein eigener dauerhaft laufender Hintergrunddienst und interne Zeitplanung gehören nicht zur Anwendung. Zeitplanung und Workflow-Orchestrierung erfolgen extern. Datenhistorie, weitere Vorhersagequellen und MQTT-Fehlersammlung sind spätere Erweiterungen. Die gemeinsamen Messdatentypen müssen auch für spätere Zimmerstationen geeignet sein; deren Geräteanbindung gehört nicht zu diesem Parser.

**REQ-003** Der Quellcode soll öffentlich auf GitHub unter MIT veröffentlicht werden. Initial erfolgt die Entwicklung im lokalen Repository. GitHub-Repository, Remote und Veröffentlichung werden erst durch einen gesonderten Auftrag eingerichtet.

### Zielplattform und Rolle im Gesamtworkflow

**REQ-004** Verbindliche Plattformvorgabe (14.10.2026): .NET ab Version 10 und Debian ab Version 13 Trixie. Initiales Ziel-Framework für Bibliothek und CLI-Beispiel ist `net10.0`; Mindestarchitektur ist ARM64 (`linux-arm64`), insbesondere NanoPi R76S. Neuere stabile .NET-/Debian-Versionen werden in die geprüfte Kompatibilitätsmatrix aufgenommen; Preview-Versionen sind nicht automatisch zugesichert. Visual Studio 2026 und SDK 10.0.303 sind als lokale Ausgangsbasis festgestellt; Paketversionen, weitere ARM-Varianten und Deployment-Modell bleiben festzulegen. Bibliothek, echter Paketkonsument und Beispiel müssen auf Debian 13/ARM64 mit .NET 10 geprüft werden. Plattformanforderung und tatsächlich nachgewiesene Kompatibilität bleiben getrennt; ohne dokumentierten Test kein behaupteter Laufzeiterfolg.

**REQ-005** Bibliotheken, native Abhängigkeiten, Dateipfade, Zeichenkodierung, Zeitzonenbehandlung und Serializer müssen mit dem Debian-/ARM-Ziel kompatibel sein. Build, Paketierung und erforderliche Laufzeitabhängigkeiten müssen reproduzierbar dokumentiert werden. Ein erfolgreicher Build auf dem Entwicklungsrechner oder Cross-Publish allein gilt nicht als Nachweis der Ausführbarkeit auf ARM.

**REQ-006** Das CLI-Nutzungsbeispiel arbeitet als kurzlebiger Prozess: Ein Aufruf validiert seine Eingaben, führt genau einen ausgewählten Befehl aus, schließt Veröffentlichung bzw. Export und Protokollierung ab, gibt Ressourcen frei und beendet sich mit einem nachvollziehbaren Exitstatus. Kein dauerhaftes Polling, keine internen periodischen Timer, keine Warteschleife auf den nächsten Aufruf und kein eingebauter Scheduler. Ein Befehl darf die zu ihm gehörenden mehreren Stationen oder Vorhersagezeitschritte verarbeiten; dies sind keine zusätzlichen CLI-Befehle. Netzwerk-Timeouts und begrenzte technische Wiederholungen innerhalb dieses einen Aufrufs bleiben zulässig.

**REQ-007** Die zeitliche Steuerung der Beispiel-CLI erfolgt ausschließlich über einen externen Dienst wie n8n oder cron. Für reguläre Aufrufe darf keine interaktive Eingabe erforderlich sein. Konfiguration, Standardausgabe, Standardfehler und Exitstatus müssen auch ohne interaktives Terminal nutzbar sein. Erfolg, Teilfehler und vollständiger Fehlschlag werden für den aufrufenden Dienst eindeutig erkennbar dokumentiert. Abbruchsignale müssen eine kontrollierte, zeitlich begrenzte Beendigung ermöglichen; ein abgebrochener Export darf nicht als vollständig erfolgreich erscheinen.

**REQ-008** WeatherParser ist ein eigenständiger Baustein im gesamten Daten- und Automationsworkflow. Seine Verantwortung endet bei Quellenabruf, Parsing, Stationsauswahl bzw. Normalisierung und der angeforderten MQTT-/JSON-/XML-Ausgabe. Der externe Workflow übernimmt Aufrufzeitpunkte, laufübergreifende Wiederholungsstrategien, Verknüpfung mit nachfolgenden Schritten, Speicherung, Anzeige und weitere Aktionen. Der Parser benötigt keine feste Kopplung an n8n; er muss auch von cron, einem Shell-Skript oder einem anderen Orchestrator aufgerufen werden können. Die Anwendung installiert oder startet keinen Orchestrator und übernimmt keine Gerätesteuerung. Beispiele des Ablaufs: externer Aufruf → WeatherParser → MQTT → weitere Verbraucher oder externer Aufruf → WeatherParser → Export → nachgelagerter Verarbeitungsschritt.

**REQ-009** Alle Kommentare im Quellcode müssen auf Englisch verfasst sein. Dies gilt für Inline-/Blockkommentare, XML-Dokumentationskommentare sowie Kommentare in Skripten und Konfigurationsbeispielen. Technische Bezeichner, CLI-Befehle und Datenfelder bleiben sprachunabhängig unverändert. Alle vom Programm erzeugten Ausgaben, einschließlich Hilfe, Logs, Status- und Fehlermeldungen, sind Englisch.

## 2. Kommandozeilenschnittstelle

**REQ-010** Die Syntax muss `WeatherParser --<command> [optionen]` sein. Pro Aufruf ist genau ein Befehl zulässig. Fehlender/unbekannter Befehl, doppelte oder widersprüchliche Optionen und ungültige Werte müssen vor Netzwerkzugriff zu einem verständlichen Fehler führen. Eine Hilfe muss Befehle, Einheiten und Beispiele erklären.

**REQ-011** Optionsnamen werden ohne Beachtung der Groß-/Kleinschreibung verarbeitet. Verbindliche Schreibweise ist `Longitude`; im ursprünglichen Entwurf stand `Langitude`. Neue Dokumentation und Ausgabefelder müssen die korrigierte Schreibweise verwenden. Ein Tippfehler-Alias ist keine Pflicht.

### Bibliothek, Paketgrenze und Konstruktorparameter

**REQ-012** Das NuGet-Paket ist das Hauptprodukt und enthält die Parser für DWD, OsthessenWetter und MOSMIX, benötigte Quellenabruf-/Normalisierungslogik, öffentliche Ausgabeentitäten, einen generischen MQTT-Serializer und Dependency-Injection-Integration. Es muss ohne CLI, n8n, cron oder Brokerverbindung als Bibliothek nutzbar sein. Die CLI ist ein separates Nutzungsbeispiel. CLI-Argumente, Prozess-Exitcodes, Anwendungskonfiguration, Datei-/Konsolenausgabe und Brokertransport gehören nicht in die Parser. Öffentliche Paketverträge und Beispiele müssen diese Grenze dokumentieren.

**REQ-013** Die Beispiel-CLI muss die Parserbibliothek über eine versionierte `PackageReference` konsumieren. Eine bloße `ProjectReference` oder kopierte Parserdateien erfüllt diesen Nachweis nicht. Lokale Entwicklung und CI bauen eine `.nupkg` und verwenden bei Bedarf eine isolierte lokale Paketquelle, ohne öffentliche Veröffentlichung. Paket-ID, Versionierung, Ziel-Frameworks und öffentliche API werden vor Implementierung festgelegt; Paketfeed und Veröffentlichung werden separat beauftragt. Tests müssen einen unabhängigen Konsumenten mit dem tatsächlich gebauten Paket prüfen.

**REQ-014** Jeder Parser muss sämtliche benötigten Quell-URLs als explizite Konstruktorparameter erhalten, vorzugsweise als `Uri`. Bei mehreren Quellen werden Katalog-, Daten- oder Basisendpunkte klar getrennt übergeben. Keine fest kodierten vollständigen Quell-URLs, stillschweigenden Ersatzendpunkte oder selbstständige Konfigurationssuche im Parser. Validierung umfasst fehlende/ungültige Endpunkte und dokumentierte zulässige Schemas; Fehler dürfen keinen Quellenabruf auslösen. Die DI-Integration leitet konfigurierte URLs unverändert an die jeweiligen Parser-Konstruktoren weiter. Weitere Service-Abhängigkeiten werden weiterhin über Interfaces per Konstruktor injiziert.

**REQ-015** Der MQTT-Serializer des Pakets muss generell und unabhängig von Wetteranbieter oder CLI sein. Er serialisiert geeignete Ausgabeentitäten zu JSON und liefert eine Nachricht mit konfiguriertem Topic, Payload, QoS und Retain-Einstellung. Topics oder Topic-Vorlagen und ihre Zuordnung zu Nachrichtentypen werden vom Konsumenten bereitgestellt, einschließlich konfigurierbarer Stations-/Quellenbestandteile. Keine fest eingebauten `haus/weather/...`-Pfade im Bibliotheksverhalten. Fehlende Zuordnungen, unaufgelöste Platzhalter und ungültige Publish-Topics müssen ohne Brokerzugriff fehlschlagen. Quellen-IDs dürfen keine unbeabsichtigten zusätzlichen Topic-Ebenen oder Publish-Wildcards einschleusen. Der Serializer verwaltet keine Brokerverbindung oder Zugangsdaten; Transport ist eine getrennte Aufgabe des Konsumenten. Die vereinbarte QoS-/Retain-Semantik der Beispiel-CLI bleibt erhalten.

**REQ-016** Die Bibliothek muss eine dokumentierte DI-Integration bereitstellen, welche ihre Parser-/Serializer-Interfaces mit Implementierungen registriert und die vom Konsumenten gelieferten URLs, Topic-Einstellungen und weiteren Services zuordnet. Keine globalen `Uri`-Registrierungen, die Endpunkte verschiedener Parser verwechseln, und keine Service-Locator-Aufrufe in Fachklassen. Tests prüfen voneinander unabhängige Parser-Konfigurationen, URL-Weitergabe, Topic-Konfiguration, öffentliche API-Verfügbarkeit im Paket sowie die Trennung von CLI und Bibliothek. Ausgabeentitäten bleiben `[Serializable]`; TDD, englische Kommentare, zweisprachige Dokumentation, ARM-Kompatibilität und Coverage-Regeln gelten weiter.

**REQ-017** Eine explizite .NET-/Debian-/ARM-Kompatibilitätsmatrix muss gewünschte Unterstützung, offiziellen Herstellersupport und tatsächlich nachgewiesene Projektkompatibilität trennen. Vor Release sind .NET 10 als Mindestversion und Debian 13/ARM64 mit echtem NuGet-Paket, Abhängigkeitsauflösung, Serializern, Zeitzonen und DI zu prüfen. Ohne diesen Nachweis bleibt die Laufzeitabnahme offen. Neuere stabile Versionen werden anhand dokumentierter Tests aufgenommen; eine automatische Bindung der Debian-Mindestversion an den LTS-Wechsel besteht nicht.

### SelectDWD

**REQ-020** Erforderliche Optionen:

| Option | Bedeutung | Eingabe/Einheit |
|---|---|---|
| `Latitude` | Geografische Breite des Bezugsstandorts | Dezimalgrad oder Grad/Minute/Sekunde mit N/S |
| `Longitude` | Geografische Länge des Bezugsstandorts | Dezimalgrad oder Grad/Minute/Sekunde mit E/W |
| `Altitude` | Bezugshöhe über Normalniveau | Meter |
| `MaxDistance` | Maximale horizontale Entfernung | Kilometer, mindestens 0 |
| `MaxAltDifference` | Maximaler absoluter Höhenunterschied | Meter, mindestens 0 |

**REQ-021** Koordinaten müssen mindestens die Formate `50.5549`, `9.6738`, `50° 33′ 18″ N` und `9° 40′ 26″ E` unterstützen. DMS-Eingaben werden als ein zusammenhängendes, gegebenenfalls quotiertes Argument übergeben. Breitengrad: −90 bis +90; Längengrad: −180 bis +180; Minuten und Sekunden: jeweils 0 bis kleiner 60. Nicht endliche Zahlen, falsche Himmelsrichtungen und widersprüchliche Vorzeichen müssen abgewiesen werden. Die Interpretation darf nicht von der Betriebssystemsprache abhängen.

**REQ-022** Stationsauswahl muss beide Grenzen einschließlich Randwert einhalten: Entfernung ≤ `MaxDistance` und absoluter Höhenunterschied ≤ `MaxAltDifference`. Horizontale Entfernung wird auf Basis von WGS84-Koordinaten mit einem dokumentierten geodätischen Verfahren berechnet. Die Höhe wird separat gefiltert, nicht in eine euklidische Grad-Distanz eingerechnet. Negative Bezugshöhen sind zulässig.

**REQ-023** Ausgabe ist eine Stationsliste gemäß gewähltem Ausgabemodus: MQTT-Nachricht oder JSON-/XML-Export. Jede Station enthält:

| Feld | Bedeutung |
|---|---|
| `station_id` | DWD-ID als Zeichenkette; führende Nullen bleiben erhalten |
| `name` | Stationsname |
| `latitude`, `longitude` | Dezimalgrad |
| `altitude_m` | Stationshöhe in Metern |
| `distance_km` | Entfernung zum Bezugsstandort |
| `altitude_difference_m` | Vorzeichenbehaftet: Stationshöhe minus Bezugshöhe |
| `available_measurements` | Nachgewiesene Messgrößen/Datentypen der Station |

Die Nachricht enthält außerdem `schema_version`, Quellenkennung, UTC-Abrufzeitpunkt und Suchparameter. Die Liste wird aufsteigend nach Entfernung und bei Gleichstand nach ID sortiert. Kein Treffer ergibt eine gültige leere Liste mit Hinweis im Log, keine erfundene Ersatzstation.

**REQ-024** Messgrößenverfügbarkeit muss aus den jeweiligen DWD-Produkten bzw. Metadaten abgeleitet werden. Ein Eintrag in einem Stationsverzeichnis beweist weder die Verfügbarkeit aller Größen noch einen aktuell gültigen Einzelmesswert. Inaktive Stationen ohne aktuellen Datenbezug dürfen nicht stillschweigend als aktuell geeignet erscheinen. Stationsauswahl verändert die Konfiguration nicht automatisch.

### ParseDWD

**REQ-030** `StationID` kann explizit übergeben werden. Andernfalls muss die konfigurierte Bezugsmessstation verwendet werden. CLI hat Vorrang. Fehlen beide Angaben, muss ein Fehler mit stabiler Kennung im Log erscheinen; Exitstatus ungleich 0, kein Abruf, keine MQTT-Veröffentlichung und keine automatische Stationswahl.

**REQ-031** Ungültige oder unbekannte Stations-IDs müssen verständlich gemeldet werden. Messdaten müssen ausschließlich zur aufgelösten Station gehören. Ein fehlendes optionales Messprodukt darf nicht mit Daten einer anderen Station ergänzt werden.

**REQ-032** Ausgabe sind Messdatensätze gemäß gemeinsamem Schema, wahlweise als MQTT-Nachrichten oder JSON-/XML-Export. Die erste Anbindung soll die dokumentierten DWD-10-Minuten-Produkte untersuchen und verfügbare Größen nutzen. Fehlwertkennungen wie `-999`, Qualitätskennzeichen, Aktualität, UTC und unterschiedliche Messzeiträume müssen berücksichtigt werden. Fehlwerte dürfen nicht als reale Temperatur/Druck-/Windwerte erscheinen.

### ParseOstHessen

**REQ-040** Erforderliche Option ist `StationNamePattern`. Das Muster verwendet Glob-Semantik: `*` für beliebig viele Zeichen, `?` für genau ein Zeichen; Vergleich ohne Beachtung der Groß-/Kleinschreibung. Es ist kein beliebiger regulärer Ausdruck. Ohne Platzhalter ist ein vollständiger Stationsname gemeint.

**REQ-041** Alle passenden Stationen werden verarbeitet, jede mit eigener Quellenkennung und eigenem Messdatensatz; im MQTT-Modus mit eigener Nachricht. Kein Treffer muss als Fehler enden. Mehrere Treffer dürfen nicht stillschweigend auf die erste Station reduziert werden. Originalnamen mit Umlauten bleiben im Datensatz erhalten; Topic-IDs werden stabil und kollisionsfrei gebildet.

**REQ-042** Das angezeigte Datum darf anhand des Abrufzeitpunkts in `Europe/Berlin` abgeleitet werden, wenn die Quelle nur eine Uhrzeit liefert. Das Datum ist dabei eine Annahme, kein nachgewiesener Messzeitpunkt. Das Ergebnis muss nach UTC umgerechnet und mit `timestamp_quality: "estimated_date"` sowie `source_timezone: "Europe/Berlin"` gekennzeichnet werden. Bei eindeutig vollständigem Quellzeitpunkt gilt `timestamp_quality: "source"`.

**REQ-043** Messzeitangaben wie in der Quelle angegeben auslesen; vollständige Zeitpunkte erhalten und nach UTC konvertieren. Keine Ersetzung durch Abrufzeit und keine zusätzliche Aktualitätsfilterung. Der Konsument bewertet die Zeitpunkte. Bei ausschließlich einer Uhrzeit gilt die ausdrücklich erlaubte Datumsableitung nach REQ-042 mit Schätzkennzeichnung und Mitternachtsbehandlung; mehrdeutige/nicht existierende Sommerzeitangaben nicht willkürlich auflösen.

**REQ-044** Änderungen am Seitenformat, Offline-Stationen, fehlende Uhrzeit und unlesbare Zahlen müssen erkannt und protokolliert werden. Abrufzeit darf nicht als vermeintlicher Messzeitpunkt eingesetzt werden. Spitzenböe ist nicht mittlere Windgeschwindigkeit; Regenmenge ist nicht Regenintensität. Der Adapter darf solche Werte nicht falsch zuordnen.

**REQ-045** Die OsthessenWetter-Seitenstruktur wird „as is“ in der zum Umsetzungszeitpunkt vorgefundenen Fassung verwendet. Bei der Implementierung sind Umsetzungsdatum, Quell-URL und die für das Parsing relevanten Strukturannahmen zu dokumentieren. Die Unterstützung bezieht sich auf diese dokumentierte Fassung; spätere Strukturänderungen können eine Parseranpassung erfordern. Erkennbare Formatabweichungen werden gemäß REQ-044 protokolliert; die Regelungen zu Zeitqualität und Quellenbedingungen bleiben bestehen.

**REQ-046** Die OsthessenWetter-Quell-URL wird wie bei DWD über den Parser-Konstruktor übergeben. Ist die Seite nicht abrufbar, eine Exception mit nachvollziehbarer englischer Diagnose auslösen. Ansonsten Werte und Messzeitangaben aus der dokumentierten As-is-Seitenstruktur auslesen; gemeinsame Einheiten-/UTC-/Rundungsregeln ohne erfundene Ersatzwerte anwenden.

### SelectMOSMIX

**REQ-100** `--SelectMOSMIX` muss MOSMIX-Vorhersagestandorte aus dem offiziellen DWD-MOSMIX-Stationskatalog auswählen. Die erforderlichen Parameter sind identisch mit `SelectDWD`: `Latitude`, `Longitude`, `Altitude`, `MaxDistance` und `MaxAltDifference`. Koordinatenformate, Eingabevalidierung, Entfernungsberechnung und inklusive Grenzwerte aus REQ-020 bis REQ-022 gelten unverändert. Ein MOSMIX-Vorhersagestandort ist nicht mit einer CDC-Messstation oder einer dort vorhandenen Messausstattung gleichzusetzen.

**REQ-101** Ausgabe ist eine MOSMIX-Stationsliste im gewählten MQTT-/JSON-/XML-Modus. Jeder Eintrag enthält `mosmix_id`, `name`, `latitude`, `longitude`, `altitude_m`, `distance_km`, `altitude_difference_m` und `available_forecast_elements`. Die MOSMIX-ID ist eine eigenständige Zeichenkette; führende Nullen und alphanumerische IDs müssen erhalten bleiben. DWD-CDC-Messstations-IDs dürfen nicht ungeprüft als MOSMIX-IDs verwendet werden. Die Nachricht enthält `schema_version`, Quellenkennung, UTC-Abrufzeitpunkt und Suchparameter. Sortierung: Entfernung aufsteigend, dann MOSMIX-ID; kein Treffer ergibt eine gültige leere Liste mit Loghinweis.

**REQ-102** Koordinaten und Höhen müssen entsprechend dem tatsächlich dokumentierten Katalogformat normalisiert werden. Fehlende Höhen dürfen nicht durch 0 ersetzt werden; ein Standort ohne prüfbare Höhe wird bei der erforderlichen Höhenfilterung mit Hinweis ausgeschlossen. Verfügbare Vorhersageelemente müssen für die gewählte MOSMIX-Produktvariante nachgewiesen werden. Ist die standortspezifische Verfügbarkeit noch unbekannt, wird sie ausdrücklich als unbekannt gekennzeichnet statt alle Elemente zu behaupten. Auswahl verändert keine Konfiguration automatisch und greift nicht ersatzweise auf das CDC-Stationsverzeichnis zurück.

### ParseMOSMIX

**REQ-103** `--ParseMOSMIX` benötigt `--MOSMIXID <id>` und `--Product MOSMIX_S|MOSMIX_L`. Fehlende, ungültige oder unbekannte IDs müssen mit stabiler Fehlerkennung und Fehlerstatus gemeldet werden. Eine fehlende oder syntaktisch ungültige ID wird vor Quellenabruf abgewiesen. Keine automatische Übernahme der DWD-Bezugsmessstation, keine stillschweigende Auswahl eines Nachbarstandorts und keine automatische Gleichsetzung der ID-Systeme. `ParseMOSMIX` ist die verbindliche Schreibweise; `PasreMOSMIX` ist ein Tippfehler und kein zusätzlicher Befehl.

**REQ-104** MOSMIX_S und MOSMIX_L müssen jeweils einen eigenen Parser gemäß REQ-109 erhalten. Jeder ruft den neuesten verfügbaren Lauf der angegebenen ID über seine per Konstruktor übergebenen Endpunkte ab und verarbeitet KML/KMZ. Quellenpfade, Elementabbildung und produktbezogene Aktualisierung/Aktualitätsgrenzen werden vor Implementierung dokumentiert; kein stillschweigender Produktwechsel. Namensräume, Standort, Ausgabezeit und Parameter-/Zeitachsen-Zuordnung prüfen. Veraltete Läufe nicht ungekennzeichnet als aktuell ausgeben.

**REQ-105** Ausgabe ist ein eigener Vorhersagedatensatz mit mindestens `schema_version`, `dataset_type: "forecast"`, `source`, `mosmix_id`, `location`, `product`, `issued_at`, `retrieved_at` und einer zeitlich aufsteigend sortierten Sammlung `forecasts`. Jeder Eintrag enthält `valid_at` und `values` mit den verfügbaren normalisierten Wettergrößen. Für Intervallgrößen müssen zusätzlich der Beginn und das Ende des Bezugszeitraums erkennbar sein. `issued_at` bezeichnet den Quell-Ausgabezeitpunkt, `valid_at` den vorhergesagten Zeitpunkt; `retrieved_at` den Abruf. Kein `measured_at` für Vorhersagen und keine Veröffentlichung als Messdaten. Alle Zeitangaben folgen REQ-051: UTC ohne `Z` oder Offset.

**REQ-106** Die Wettergrößen verwenden, soweit vorhanden und fachlich passend, dieselben Feldnamen und Ausgabeeinheiten wie REQ-052. Quelleneinheiten und Druckreferenzen müssen aus den MOSMIX-Elementdefinitionen übernommen und kontrolliert normalisiert werden. Nicht angebotene Größen bleiben ausgelassen; beispielsweise dürfen fehlende Feuchtigkeit oder Helligkeit nicht durch 0 ersetzt werden. Wahrscheinlichkeiten, Niederschlagssummen, Böen und Energiegrößen sind keine direkten Ersatzwerte für Feuchtigkeit, Regenintensität, Mittelwind oder Strahlungsleistung. Ableitungen und Umrechnungen benötigen dokumentierte Formeln und Bezugszeiträume.

**REQ-107** Jeder Parameterwert muss dem korrekten Vorhersagezeitpunkt bzw. Zeitraum zugeordnet sein. Unterschiedliche Reihenlängen, beschädigte Archive, falsche Standort-ID, fehlender Ausgabezeitpunkt und unbekannte notwendige Strukturen müssen erkannt werden. MOSMIX-Fehlwertkennungen werden ausgelassen, ohne nachfolgende Werte auf andere Zeitpunkte zu verschieben. Ein Lauf ohne verwertbare Vorhersagewerte endet mit Fehlerstatus. Metadaten mehrerer Läufe dürfen nicht zu einem scheinbar einheitlichen Lauf vermischt werden.

**REQ-108** Forecast-Parsing und Datenabruf müssen wie Messdatenadapter per Konstruktor injizierbar und offline testbar sein. Archive erhalten begrenzte Download-/Entpackgrößen; XML-Verarbeitung darf keine externen Entitäten, DTDs oder referenzierten Ressourcen nachladen. JSON-/XML-Export erfolgt aus dem normalisierten Vorhersagemodell, nicht durch bloßes Weiterreichen der rohen KML-Datei.

**REQ-109** Die NuGet-Bibliothek stellt getrennte Verträge `MosmixSParser` / `IMosmixSParser` und `MosmixLParser` / `IMosmixLParser` bereit. Jeder Parser erhält eigene Konstruktor-URLs und eine separate DI-Registrierung. Gemeinsame technische Parsing-Logik ist zulässig; ein einzelner Produktumschalter oder stillschweigender Fallback ersetzt die beiden Parser nicht. Tests prüfen beide Produkte unabhängig, gleichzeitige Konfiguration, korrekte Endpunktnutzung und keine gegenseitige Zustandsbeeinflussung.

**REQ-110** Die DWD-Aktualisierungshäufigkeit wird Deutsch/Englisch dokumentiert: `MOSMIX_S` stündlich; `MOSMIX_L` vier Läufe täglich um 03, 09, 15 und 21 UTC. Laufzyklen sind von tatsächlicher Dateiverfügbarkeit, Abrufplanung und Vorhersagegültigkeit zu trennen. Den Horizont aus tatsächlichen Dateizeitstempeln statt einer angenommenen festen Länge ableiten. Vor Release anhand der zitierten offiziellen DWD-Beschreibung prüfen; Zeitplanung bleibt extern, Aktualitätsprüfungen sind produktbezogen.

**REQ-111** Das Beispiel `--ParseMOSMIX` verlangt `--Product MOSMIX_S|MOSMIX_L`; fehlende/unbekannte Werte vor Abruf abweisen. `--SelectMOSMIX` behält seine Geo-Parameter und kennzeichnet geprüfte Elementverfügbarkeit je Produkt einschließlich unbekannter Zustände. S-/L-Läufe nicht in Metadaten oder Datensätzen vermischen. Konsumentenseitig konfigurierte Quellen-/Topic-Zuordnungen unterscheiden die Produkte. Beide werden mit MQTT, JSON und XML bei unverändertem Vorhersagevertrag und gleicher Command-Anzahl geprüft.

### Getrennte MOSMIX-Parser und Aktualisierung

Die Bibliothek bietet zwei eigenständige Parser mit eigenen Interfaces: `MosmixSParser` / `IMosmixSParser` für `MOSMIX_S` und `MosmixLParser` / `IMosmixLParser` für `MOSMIX_L`. Beide erhalten ihre jeweiligen Quell-URLs über Konstruktorparameter und werden separat über die DI-Integration konfiguriert. Gemeinsame KML-/KMZ-Hilfslogik darf wiederverwendet werden; ein einzelner umschaltbarer Parser ersetzt die beiden öffentlichen Verträge nicht. Bei fehlenden Daten gibt es keinen stillschweigenden Wechsel zum anderen Produkt.

| Produkt | DWD-Ausgabehäufigkeit | Laufzeitangaben |
|---|---|---|
| `MOSMIX_S` | Stündlich | Neuer Produktlauf je Stunde |
| `MOSMIX_L` | Viermal täglich | 03, 09, 15 und 21 UTC |

Diese Angaben beschreiben die DWD-Produktzyklen, keinen internen Scheduler und keine garantierte Dateiverfügbarkeit exakt zur Laufzeit. Ausgabezeitpunkt, Abrufzeitpunkt und Vorhersagegültigkeit bleiben getrennt. Der tatsächliche Vorhersagehorizont kommt aus der Datei-Zeitachse; eine neue Dateiausgabe beweist nicht die Neuberechnung sämtlicher darin enthaltener Zeitschritte. Aktualitätsgrenzen und externe Abrufpläne müssen produktbezogen festgelegt werden.

Das CLI-Beispiel behält `--ParseMOSMIX` und verlangt zusätzlich `--Product MOSMIX_S|MOSMIX_L` zur eindeutigen Auswahl des Bibliotheksparsers. Fehlende oder unbekannte Produkte sind Fehler vor Quellenabruf. `--SelectMOSMIX` behält die bisherigen Geo-/Höhenparameter und weist die bekannte Verfügbarkeit/Elemente getrennt nach Produkt aus; unbekannte Verfügbarkeit bleibt gekennzeichnet. Ausgabemetadaten, Quellenkennung und konfigurierte MQTT-Topics müssen S und L unterscheiden, damit ihre retained-Nachrichten nicht unbeabsichtigt einander ersetzen.

Quelle, geprüft am 14.10.2026: [DWD-Verfahrensbeschreibung, Abschnitt 2.1.3 und Tabelle 2.1](https://www.dwd.de/DE/leistungen/met_verfahren_mosmix/mosmix_verfahrenbeschreibung_gesamt.pdf?__blob=publicationFile&v=6). Produktzyklen vor Implementierung/Release erneut prüfen; die Bibliothek enthält derzeit noch keinen implementierten Parser.

**REQ-112** [pointforecast_dwd_extension_V1_0.xsd](https://opendata.dwd.de/weather/lib/pointforecast_dwd_extension_V1_0.xsd) ist die verbindliche Grundlage der MOSMIX-Quell-/XML-Typen beider Parser für MOSMIX_S und MOSMIX_L. Datenstrukturen daraus ableiten, Namensräume/Kardinalitäten erhalten und ergänzende KML-Typen prüfen. Schemafassung/Prüfsumme und reproduzierbare Generierung bzw. manuelle Ableitung sind vor Implementierung zu dokumentieren. Quelltypen bleiben von öffentlichen `[Serializable]`-Ausgabeentitäten getrennt; Mapping mit TDD prüfen. Diese Anweisung hält die Schemaquelle fest und erzeugt noch keinen Code.

## 3. Gemeinsamer Messdatenvertrag

**REQ-050** MQTT-Messdaten- und Vorhersagenachrichten müssen JSON sein. Derselbe fachliche Datenvertrag gilt für JSON- und XML-Exporte. Messdatensätze müssen mindestens `schema_version`, `source`, `location`, `measured_at`, `retrieved_at`, `timestamp_quality` und `measurements` enthalten. Vorhersagen verwenden den getrennten Vertrag aus REQ-105. Eine Quellenkennung muss Anbieter und Station eindeutig identifizieren. Ein Messdatensatz ohne verwertbare Messgröße darf nicht als erfolgreicher Messdatensatz veröffentlicht werden.

**REQ-051** Sämtliche Zeitstempel müssen UTC im Format `YYYY-MM-DDTHH:mm:ss` sein, ohne `Z` und ohne Offset. Intern muss der UTC-Bezug eindeutig erhalten bleiben. Lokale Quellzeiten werden vor der Serialisierung konvertiert. JSON- und XML-Zahlen werden kulturunabhängig ohne Einheitensuffix ausgegeben.

**REQ-052** Verbindliche Messgrößen; dieselben Feldnamen und Einheiten werden für fachlich entsprechende Vorhersagewerte verwendet:

| Feld | Einheit | Bedeutung |
|---|---|---|
| `temperature_c` | °C | Lufttemperatur |
| `pressure_hpa` | hPa | Luftdruck mit ausgewiesener Druckreferenz |
| `pressure_mmhg` | mmHg | Derselbe Luftdruck in anderer Einheit |
| `humidity_percent` | % | Relative Feuchtigkeit, 0–100 |
| `wind_direction_deg` | Grad | Meteorologische Herkunftsrichtung, 0 = Nord, 90 = Ost |
| `wind_speed_kmh` | km/h | Windgeschwindigkeit, nicht ungekennzeichnete Spitzenböe |
| `illuminance_lux` | Lux | Beleuchtungsstärke |
| `solar_irradiance_w_m2` | W/m² | Sonnenstrahlungsleistung je Fläche |
| `rain_rate_mm_h` | mm/h | Regenintensität |

**REQ-053** Nicht gelieferte bzw. ungültige Werte werden weggelassen. `0` ist kein Fehlwert. Nicht endliche Zahlen sind unzulässig. Normalisierung und Plausibilitätsprüfung müssen feldspezifisch sein; negative Temperaturen sind erlaubt. Bei Windstille ohne verlässliche Richtung wird keine Nordrichtung erfunden.

**REQ-054** Umrechnung: `mmHg = hPa × 0.7500617`; `hPa = mmHg × 1.3332239`. Ein Quellwert ist maßgeblich; Rundung erfolgt erst bei der Ausgabe mit dokumentierter Genauigkeit. Beide veröffentlichten Werte müssen konsistent sein. `pressure_reference` kennzeichnet `sensor` oder `sea_level`; ist die Referenz nicht nachgewiesen, `unknown`. Kein stillschweigendes Umrechnen der Druckreferenz.

**REQ-055** Lux und W/m² dürfen nicht pauschal ineinander umgerechnet werden. Strahlungsenergie über einen Zeitraum darf nur mit bekanntem Zeitraum in mittlere W/m² umgerechnet werden. Regenmenge über einen Zeitraum darf nur mit bekanntem Zeitraum in mittlere mm/h umgerechnet werden. Mittelungsart und Bezugszeitraum müssen im Datenvertrag erkennbar sein.

**REQ-056** Jede ausgegebene Beobachtungsgröße enthält ihren Messzeitpunkt. Unterschiedliche Quellzeitpunkte bleiben je Wert erhalten; zeitliche Zusammenführung und Bewertung übernimmt der Konsument. UTC-Format, Quellen-/Qualitätsmetadaten und Kennzeichnung geschätzter Zeiten erhalten. Vorhersagen behalten ihren eigenen Ausgabe-/Gültigkeitsvertrag.

## 4. MQTT und Konfiguration

**REQ-060** Brokeradresse, Port, TLS-Einstellungen, Zugangsdaten und Topic-Präfix müssen konfigurierbar sein. Zugangsdaten dürfen nicht fest im Code stehen, in Logs erscheinen oder als notwendige CLI-Passwortargumente Prozesslisten ausgesetzt werden. Lokale Secret-Konfigurationen müssen von Git ausgeschlossen sein.

**REQ-061** Vorgesehene, vom Konsumenten konfigurierte Beispieltopics sind `haus/weather/measurements/<source>/state` für Messdaten, `haus/weather/forecasts/<source>/state` für MOSMIX-Vorhersagen, `haus/weather/stations/dwd/selection` für CDC-Stationslisten und `haus/weather/stations/mosmix/selection` für MOSMIX-Stationslisten. Topics/Vorlagen und Präfix sind konfigurierbar; diese Beispiele sind keine fest kodierten Bibliotheksvorgaben. Vorhersage-Quellenkennungen müssen Anbieter, Produkt und MOSMIX-ID eindeutig bezeichnen. Messdatenbefehle veröffentlichen keine Vorhersagen; `ParseMOSMIX` veröffentlicht keine Messdaten. Jede Quelle benötigt eigene Schreibrechte; eine Station darf keine fremden Topics überschreiben.

**REQ-062** Messdaten und MOSMIX-Vorhersagen werden mit QoS 1 und retained veröffentlicht. Pro MOSMIX-Standort und Produkt wird der vollständige Vorhersagelauf als eine Nachricht veröffentlicht, damit nicht einzelne Zeitschritte einander im retained-Zustand ersetzen. QoS-1-Duplikate müssen anhand Quelle und Messzeitpunkt bzw. Vorhersagelauf behandelbar sein. Stationslisten werden mit QoS 1 ohne retained veröffentlicht, um eine frühere Suchantwort nicht als aktuelle Auswahl vorzutäuschen. TLS-Zertifikatsprüfung darf nicht pauschal deaktiviert werden. Unverschlüsselter Betrieb muss ausdrücklich konfiguriert und als solcher dokumentiert werden.

**REQ-063** Im MQTT-Modus darf Erfolg erst nach erfolgreicher MQTT-Bestätigung gemeldet werden. Verbindungsaufbau, Abruf und Veröffentlichung benötigen begrenzte Timeouts und kontrollierte Abbruchbehandlung. Wiederholungen sind begrenzt und dürfen nicht zu unkontrollierter Last führen. Teilfehler beim Abruf mehrerer Stationen werden zusammengefasst; Exitstatus ungleich 0, wenn angeforderte Stationen fehlgeschlagen sind, auch wenn andere veröffentlicht wurden.

**REQ-064** Konfigurationsformat, Speicherort und Umgebungsvariablen-Namen werden bei der CLI-Entwicklung in P3 festgelegt; sie blockieren die Bibliotheksentwicklung nicht. Die aufgelöste DWD-Station muss vor jedem `ParseDWD`-Abruf validiert werden. Die Anwendung muss ohne private Projektpfade, interne Netzwerkadressen oder lokale Benutzernamen aus dem öffentlichen Repository konfigurierbar sein.

### JSON- und XML-Export

**REQ-065** Alle fünf Befehle müssen die gemeinsame Option `--Output mqtt|json|xml` unterstützen. Ohne Angabe gilt `mqtt`. `json` und `xml` exportieren die fachlichen Ergebnisse ohne MQTT-Verbindung; dafür dürfen weder Brokerkonfiguration noch Brokerverfügbarkeit erforderlich sein. Ein unbekanntes Format muss vor Quellenabruf abgewiesen werden. Die fachlichen Eingabeprüfungen, insbesondere die DWD-Stationsauflösung und die erforderliche MOSMIX-ID, gelten in jedem Modus unverändert.

**REQ-066** Im JSON-/XML-Modus bestimmt die optionale Angabe `--OutputFile <pfad>` die Ausgabedatei. Ohne diese Option wird das Dokument auf Standardausgabe geschrieben. `OutputFile` im MQTT-Modus ist ungültig. Datei-Exporte verwenden UTF-8, prüfen den Zielpfad und melden Schreibfehler mit Fehlerstatus. Bestehende Dateien werden nicht stillschweigend überschrieben; bei vorhandenem Ziel wird vor Quellenabruf abgebrochen. Eine fehlgeschlagene Dateierstellung darf keine teilweise fertige Zieldatei als erfolgreichen Export hinterlassen.

**REQ-067** JSON und XML müssen denselben normalisierten Datenbestand, Schema-Version, Quellenkennungen, Mess- bzw. Vorhersagegrößen, Einheiten, UTC-Zeitangaben einschließlich Ausgabe- und Gültigkeitszeiten, Zeitqualitätskennzeichnungen und Qualitäts-/Zeitrauminformationen wiedergeben. Fehlende Größen bleiben in beiden Formaten ausgelassen; echte Nullwerte, führende Nullen in Stations-IDs und Umlaute bleiben erhalten. XML benötigt eine dokumentierte, versionierbare Abbildung der Feldnamen und Auflistungen; fehlende Werte dürfen nicht als leere numerische Elemente erscheinen.

**REQ-068** Jeder Exportaufruf muss genau ein syntaktisch vollständiges JSON- bzw. XML-Dokument liefern. Stationslisten, mehrere Stationsergebnisse und Vorhersagezeitschritte werden als Sammlungen innerhalb dieses Dokuments abgebildet, nicht als aneinandergehängte Einzeldokumente. Reihenfolge und Metadaten müssen nachvollziehbar und für gleiche Eingangsdaten deterministisch sein. Bei Teilfehlern darf ein dokumentiertes Ergebnis mit erfolgreichen Datensätzen und erkennbaren Fehlern exportiert werden; der Exitstatus bleibt ungleich 0. Fehler dürfen nicht als Messwerte erscheinen. Die genaue Dokumenthülle wird vor Implementierung festgelegt.

**REQ-069** Erfolg im Exportmodus darf erst nach vollständig erfolgreicher Serialisierung und Ausgabe gemeldet werden. JSON-/XML-Serializer und Datei-/Standardausgabezugriff sind per Konstruktor injizierbar und ohne Netzwerk testbar. Export darf keine erneuten Quellenabrufe für verschiedene Serialisierungen desselben Ergebnisses erzwingen. Der Bibliotheks-MQTT-Serializer verwendet weiterhin JSON; XML-Export ändert nicht stillschweigend den MQTT-Payload-Vertrag.

## 5. Architektur und Protokollierung

**REQ-070** Alle Abhängigkeiten von Anwendungsklassen müssen per Konstruktor injiziert werden. Keine Service-Locator-Aufrufe, statischen Service-Singletons oder direkte Konstruktion konkreter Dienste innerhalb von Fachklassen. DI-Registrierung erfolgt zentral im Composition Root. Werte, unveränderliche Datenobjekte und Framework-Builder sind von der Dienstverdrahtung zu unterscheiden; sie dürfen regulär erzeugt werden.

**REQ-071** Schnittstellen müssen CLI-Verarbeitung, Konfiguration, HTTP-Zugriff, Parser, Normalisierung, Stationsauswahl, Zeitversorgung, JSON-/XML-Serialisierung, Exportziele und MQTT trennen. Zeit und externe Ein-/Ausgaben müssen testbar injiziert sein. Klassen erhalten nur ihre benötigten Abhängigkeiten, keinen allgemeinen `IServiceProvider`.

**REQ-072** Fehlerlogs müssen UTC-Zeitpunkt, Schweregrad, stabile Fehlerkennung, Befehl und gegebenenfalls Quelle/Station enthalten. Zugangsdaten und ungefilterte Secret-Konfigurationen dürfen nie geloggt werden. Eine MQTT-Sammlung dieser Fehler ist ausdrücklich eine spätere Erweiterung; sie darf nicht vorausgesetzt werden, um einen MQTT-Verbindungsfehler melden zu können.

**REQ-073** CLI-Fehler und Logs werden auf Standardfehler ausgegeben. Im JSON-/XML-Modus ohne Ausgabedatei ist Standardausgabe ausschließlich dem vollständigen Exportdokument vorbehalten. Mit Ausgabedatei dürfen weder Logtext noch Erfolgsmeldungen das Exportdokument verunreinigen. MQTT bleibt der Standardmodus; Export benötigt keinen MQTT-Broker.

### Exportentitäten und Serializer

**REQ-074** Jeder fachliche Exportobjekttyp muss durch eine eigene, explizit definierte, stark typisierte C#-Entität repräsentiert sein. Dies umfasst die Dokumenthüllen, CDC- und MOSMIX-Stationslisteneinträge, Suchparameter, Messdatensätze mit ihren Messwerten, Vorhersageläufe mit ihren Zeitschritten und Wetterwerten sowie exportierte Qualitäts-, Zeitraum- und Fehlerobjekte. Alle selbst definierten Entitätstypen im Exportobjektgraph müssen ausdrücklich mit `[Serializable]` markiert sein, einschließlich verschachtelter Typen. „Eigene Entität“ bezeichnet einen Typ je fachlichem Objekt, nicht einen neuen Typ je Laufzeitinstanz. Wiederverwendbare Wertentitäten dürfen von mehreren Ausgaben genutzt werden; Messungen und Vorhersagen behalten getrennte Datensatztypen.

**REQ-075** JSON-Export muss mit `System.Text.Json.JsonSerializer`, XML-Export mit `System.Xml.Serialization.XmlSerializer` aus diesen Entitäten erfolgen. JSON und XML dürfen nicht durch manuelle Zeichenkettenverkettung oder eine parallel gepflegte zweite Datenzusammenstellung erzeugt werden. Beide Serializer erhalten denselben aufbereiteten fachlichen Objektgraph; Der generische MQTT-Serializer im NuGet verwendet ebenfalls die passenden Exportentitäten und den JSON-Serializer. Anonyme Objekte, `dynamic` oder untypisierte Objekt-/Dictionary-Konstruktionen dürfen die definierten Exportentitäten nicht ersetzen.

**REQ-076** Exportentitäten enthalten ausschließlich Exportdaten, keine Services, Brokerverbindungen, Logger oder Secret-Konfigurationen. Sie müssen mit beiden Serializern kompatibel sein: öffentlich zugängliche Typen und Eigenschaften, XML-kompatible typisierte Sammlungen und, wo vom XmlSerializer benötigt, öffentliche parameterlose Konstruktoren und beschreibbare Eigenschaften. Serialisierungsattribute und zentrale Serializer-Konfiguration steuern Feldnamen, XML-Wurzelelemente, Sammlungen und das Weglassen fehlender Werte. `[Serializable]` ist eine zusätzliche Projektvorgabe; es ersetzt nicht die spezifischen JSON-/XML-Regeln. Insbesondere müssen UTC ohne Zeitzonensuffix, kulturunabhängige Zahlen und Schema-Versionierung ausdrücklich erhalten bleiben.

**REQ-077** Serializer werden hinter per Konstruktor injizierten Exportdiensten bereitgestellt; gegebenenfalls benötigte typabhängige Serializer-Erzeugung erfolgt über eine zentral verdrahtete Factory. Fachklassen konstruieren keine konkreten Serializer-Dienste. Tests müssen die `[Serializable]`-Markierung aller Exportentitäten prüfen und repräsentative vollständige Objektgraphen mit den echten JSON-/XML-Serializern verifizieren. Dazu gehören beide Stationslisten, Messdaten, Vorhersagen, fehlende Werte, echte Nullwerte, führende Nullen, Sonderzeichen, UTC-Format und fachliche Gleichheit der beiden Ausgaben. Serializer-Aufrufe dürfen im jeweiligen Serializer-Vertragstest nicht vollständig durch Mocks ersetzt werden; externe I/O und die Verdrahtung bleiben separat mockbar.

**REQ-078** Jede „Doing-Klasse“, also jede Klasse mit ausführender Anwendungs- oder Fachlogik, muss ein zugehöriges, zweckbezogenes Interface implementieren. Dies gilt insbesondere für Command-Handler, CLI-Verarbeitung, Quellenzugriffe, Parser, Stationsauswahl, Normalisierung, Serializer-/Exportdienste und MQTT-Veröffentlichung. Abhängigkeiten auf solche Klassen werden als Interfaces per Konstruktor injiziert und im Composition Root zugeordnet. Interfaces sollen den tatsächlich benötigten Vertrag abbilden. Reine Daten-/Exportentitäten benötigen kein künstliches Interface, solange sie keine ausführende Logik übernehmen.

## 6. Tests und Abnahme

**REQ-080** Testframework ist MSTest. Mocks werden mit Moq erzeugt; Assertions verwenden FluentAssertions. Verwendete Versionen müssen zur gewählten .NET-Version und zum MIT-Projekt passen; Lizenzbedingungen sind vor Aufnahme zu prüfen. Coverage wird mit Microsoft.CodeCoverage erfasst.

**REQ-081** Gemeinsamer Testobjekt- und Mock-Aufbau erfolgt in `[TestInitialize]`. `[TestCleanup]` ruft für alle verwendeten Mocks `VerifyAll()` auf, zum Beispiel über eine gemeinsame `MockRepository`. Produktionsdienste werden nicht in jedem Test neu verdrahtet. Testdaten dürfen pro Test gezielt variieren; erwartete Mock-Interaktionen werden explizit eingerichtet.

**REQ-082** Erforderliche Testfälle:

- Dezimal- und DMS-Koordinaten, S/W-Richtung, Grenzwerte, fehlerhafte Eingaben und Kulturunabhängigkeit.
- Entfernung/Höhenfilter einschließlich Randwerten, stabile Sortierung, leere Stationsliste und produktabhängige Messgrößen.
- DWD-Stationsauflösung: CLI-Vorrang, Konfigurationsfallback, fehlende/ungültige ID und kein Abruf/Publish bei Konfigurationsfehler.
- DWD-CSV/ZIP-Verarbeitung, fehlende Produkte, Fehlwertkennungen, Quellenzugehörigkeit und getrennte Messzeitpunkte.
- MOSMIX-Katalogauswahl mit gleichen Geo-/Höhenfiltern, alphanumerischen IDs, führenden Nullen, fehlenden Höhen, stabiler Sortierung und leeren Ergebnissen.
- MOSMIX-KML/KMZ, Namensräume, Standort-/Produktprüfung, Ausgabe- und Gültigkeitszeiten, UTC-Konvertierung, Parameter-/Zeitachsen-Zuordnung, fehlende Werte und Reihenlängenfehler.
- Vorhersage-Normalisierung einschließlich Einheiten/Intervallen, Trennung von Messdaten, vollständiger retained-Lauf, Export ohne MQTT, abgewiesene externe XML-Entitäten und übergroße Archive.
- OsthessenWetter-Muster mit einem/mehreren/keinem Treffer, Umlaute, Formatänderungen und Offline-Zustände.
- UTC-Ausgabe ohne Offset, Mitternacht, Sommerzeitlücke/-mehrdeutigkeit und Kennzeichnung geschätzter Daten.
- Einheitenumrechnung, echte Nullwerte, fehlende Messgrößen, Druckreferenz sowie Regenmenge/Böe versus Intensität/Mittelwind.
- MQTT-Topics, QoS/retained, Authentifizierungs-/TLS-Fehler, Timeouts, Abbruch, Teilfehler und korrekter Exitstatus.
- Fehlerlogs ohne Secrets und erfolgreicher Publish erst nach Bestätigung.
- Unabhängiger NuGet-Konsument, versionierter Paketbezug der Beispiel-CLI, Parser-URLs über Konstruktoren, getrennte DI-Konfigurationen und generische konfigurierbare MQTT-Topics gemäß REQ-012 bis REQ-016.
- Exportentitäten mit `[Serializable]`, vollständige Objektgraphen und echte JSON-/XML-Serializer gemäß REQ-074 bis REQ-077.
- JSON-/XML-Export aller fünf Befehle: gleiche fachliche Inhalte, UTC ohne Offset, fehlende Werte, Nullwerte, Stations-IDs mit führenden Nullen, Umlaute und XML-Escaping.
- Vollständige Dokumente bei mehreren Stationen und leerer Stationsliste, Datei-/Standardausgabe, getrennte Logs, kein MQTT-Zugriff im Exportmodus, ungültige Format-/Zielkombinationen, bestehende Dateien, Schreibfehler und Teilfehler.

**REQ-083** Automatisierte Standardtests müssen offline und ohne produktiven Broker reproduzierbar laufen. Kleine synthetische Fixtures sind bevorzugt. Live- und Broker-Integrationstests werden getrennt, ausdrücklich aktiviert und mit ihrem Zweck dokumentiert. Fremde Webseitenkopien dürfen nicht ungeprüft ins Repository aufgenommen werden.

**REQ-084** Vor Freigabe muss die Anwendung auf dem dokumentierten Debian-/ARM64-Ziel ausgeführt und geprüft werden. Die Abnahme umfasst nichtinteraktive Ein-Befehl-Aufrufe, kontrolliertes Prozessende, Exitstatus und getrennte Ausgabekanäle, JSON-/XML-Serializer, UTC-/Zeitzonenverarbeitung und Ressourcenfreigabe. Zusätzlich sind externe Zeitplanung und Workflow-Verkettung exemplarisch zu dokumentieren; ein dauerhafter Hintergrundprozess darf dafür nicht vorausgesetzt werden. Hardware-/ARM-Laufzeitprüfungen werden getrennt von Offline-Unit-Tests ausgewiesen. Ein nicht durchgeführter Zielplattformtest bleibt ausdrücklich offen.

**REQ-085** Entwicklung muss testgetrieben erfolgen (TDD): Zuerst einen aussagekräftigen Test für das gewünschte Verhalten oder den Fehlerfall erstellen und sein Fehlschlagen aus dem erwarteten Grund nachweisen (Red), dann die minimale Implementierung bis zum Bestehen ergänzen (Green), anschließend unter grünen Tests refaktorieren (Refactor). Dies gilt auch für Fehlerkorrekturen mit einem reproduzierenden Regressionstest. MSTest, Moq, FluentAssertions, TestInitialize und TestCleanup/VerifyAll bleiben verbindlich. Tests sollen fachliches Verhalten statt Implementierungsdetails prüfen. Die Reihenfolge gilt ab Beginn der ausdrücklich beauftragten Implementierung; aktuell ist die Projektstruktur beauftragt, fachliche Implementierung bleibt vertagt.

**REQ-086** Zeilen- und Branch-Coverage müssen jeweils mindestens 80 % erreichen. Beim erstmaligen Aufbau ohne messbare Zielbranch-Baseline gilt nur diese Mindestgrenze; der Zielbranch-Vergleich ist nicht anwendbar. Eine unerwartet fehlende, veraltete oder nicht vergleichbare Baseline im späteren Betrieb ist weiterhin ein Fehler. Reine Dokumentations-PRs dürfen je Kennzahl die gleiche Coverage wie der Zielbranch haben; Rückgänge sind nicht erlaubt. Für andere PRs muss jede Kennzahl unter 100 % Baseline strikt steigen; bei exakt 100 % Baseline werden exakt 100 % akzeptiert. Aktuelle Commit-IDs, vergleichbare Messbedingungen und ungerundete Zählerverhältnisse verwenden. Fehlende PR-Messungen, ungültige Zähler und Null-Nenner gelten nicht als Erfolg. Microsoft.CodeCoverage-Reports einschließlich der Codeblock-Aufschlüsselung sind Grundlage der Prüfung; Codeblock- und Branch-Coverage sind nicht gleichzusetzen. Messabbildung und Reportformat werden in P1 anhand echter Reports verifiziert. CI verwendet Microsoft.CodeCoverage und erzeugt maschinenlesbare Ergebnisse sowie lesbare Berichte als Build-Artefakte. Ausschlüsse eng begrenzen und begründen; keine Fachlogik ausblenden. Coverage ergänzt Verhaltens-/Regressionstests. Diese Anforderung autorisiert weder Veröffentlichung noch Codegenerierung.

## 7. Open Source und nächste Schritte

**REQ-090** README, Anforderungsliste, AGENTS-Regeln und MIT-Lizenz gehören ins Repository. README muss tatsächlichen Implementierungsstand und geplante Funktionen unterscheiden. Für übernommene Abhängigkeiten und Daten gelten deren eigene Bedingungen; die MIT-Lizenz des Codes ersetzt diese nicht.

**REQ-091** Vor dem jeweiligen Implementierungsschritt (CLI-Entscheidungen erst in P3): bestätigte .NET-/Debian-Mindestversionen in die Testmatrix übernehmen; SDK-Patch, Paketversionen, Bereitstellungsart, Konfiguration, MQTT-Datenvertrag und JSON-/XML-Exporthülle einschließlich Messzeiträumen und Fehler-/Exitcodes festlegen. Zusätzlich beide MOSMIX-Parser, Produktabbildungen, Katalogformat, Parameterabbildung und Vorhersagevertrag festlegen. Zusätzlich Paket-API, Konstruktor-URL-Verträge, Topic-Konfiguration und DI-Integration festlegen. Danach Paket-/Beispielstruktur und Tests, DWD-/MOSMIX-Stationsauswahl, DWD-Messdaten, MOSMIX-Vorhersagen, OsthessenWetter-Adapter und Ausgabeintegration schrittweise implementieren und prüfen. Anwendungscode wird erst nach ausdrücklichem Auftrag erzeugt.

**REQ-092** Vor produktivem Abruf müssen Quellenzugang, Attribution, Abrufhäufigkeit und Bedingungen geprüft werden. Für OsthessenWetter ist keine freigegebene maschinenlesbare API nachgewiesen; HTML-Parsing bleibt bis dahin formatabhängig. Keine Umgehung von Zugriffsschutz oder Abrufbeschränkungen.

**REQ-093** Die Projektdokumentation muss vollständig auf Deutsch und Englisch gepflegt werden. Dokumentpaare sind `README.md` / `README.en.md`, `REQUIREMENTS.md` / `REQUIREMENTS.en.md`, `AGENTS.md` / `AGENTS.en.md` und `DEVELOPMENT_PLAN.md` / `DEVELOPMENT_PLAN.en.md`. Beide Sprachfassungen sind gegenseitig verlinkt und müssen fachlich gleichwertig sein; Anforderungen behalten identische IDs, Befehle, Feldnamen, Einheiten und Beispiele. Jede inhaltliche Änderung muss beide Fassungen im selben Arbeitsstand aktualisieren. Neue Dokumente erhalten ebenfalls eine entsprechende Sprachfassung. Die unveränderte englische MIT-Lizenz in `LICENSE` bleibt der rechtliche Lizenztext. Sprachregel und Dokumentgleichheit gehören zur Änderungsprüfung.

Die geplante Umsetzung und die noch offenen Vorentscheidungen stehen in [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md).

Referenzen: [DWD CDC](https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/), [DWD-Datensatzbeschreibung](https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/air_temperature/DESCRIPTION_obsgermany_climate_10min_air_temperature_en.pdf), [OsthessenWetter-Messwerte](https://osthessen-wetter.de/messwerte/).

MOSMIX-Referenzen: [DWD Open Data – Formatbeschreibungen, Elementdefinitionen und Stationskatalog](https://www.dwd.de/DE/leistungen/opendata/hilfe.html), [MOSMIX_L – Einzelstandorte](https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_L/single_stations/), [MOSMIX_S – Vorhersagedaten](https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_S/).

Serializer-Referenzen: [System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/overview), [XmlSerializer und XML-Entitäten](https://learn.microsoft.com/en-us/dotnet/standard/serialization/introducing-xml-serialization), [SerializableAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.serializableattribute).

Quellen (geprüft am 14.10.2026): [.NET-Support](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), [.NET-10-OS-Matrix](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md), [Debian-Versionen](https://www.debian.org/releases/).

## Verbindliche MOSMIX-Schemaquelle

[DWD pointforecast_dwd_extension_V1_0.xsd](https://opendata.dwd.de/weather/lib/pointforecast_dwd_extension_V1_0.xsd) ist die verbindliche Grundlage für die MOSMIX-Quell-/XML-Datenstrukturen beider Parser. Typen, Namensräume, Elemente und Kardinalitäten werden daraus abgeleitet; ergänzende KML-Strukturen werden vor Implementierung geprüft. Die Schemafassung und ihre Prüfsumme sind für reproduzierbare Typenerzeugung bzw. Typableitung festzuhalten. Diese Quelltypen werden auf die separaten öffentlichen Ausgabeentitäten abgebildet; sie ersetzen nicht den normalisierten Forecast-Vertrag. Schema-/Mappingtests erfolgen testgetrieben. Es wurden noch keine C#-Datenstrukturen erzeugt.


**REQ-057** Ausgabepräzision: Temperatur 0,1 °C; Druck 1 hPa und 1 mmHg; Feuchtigkeit 1 Prozentpunkt; Windgeschwindigkeit 1 km/h. Vor Rundung aus ungerundeten Werten umrechnen; Exakte Mittelpunktwerte vom Nullpunkt weg runden (20,25 → 20,3; −20,25 → −20,3).

**REQ-079** Für öffentliche XML-Exportentitäten reproduzierbar XSD-Dateien generieren und echte XML-Exporte dagegen validieren. Diese Schemas von der MOSMIX-Eingabe-XSD getrennt halten.


## Ergänzende Entscheidungen vom 14.10.2026

- Entwicklungsumgebung: Visual Studio 2026. Lokal nachgewiesen: Community 18.8.3 und .NET SDK 10.0.303; dieser SDK-Stand ist die reproduzierbare Ausgangsbasis für die geplante `global.json` und CI, Ziel-Framework bleibt `net10.0`. Vor Implementierung Wartungsstand prüfen und ein erforderliches SDK-Update ausdrücklich dokumentieren. Visual Studio ist keine Laufzeitabhängigkeit unter Debian. [Microsoft SDK-/Visual-Studio-Zuordnung](https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs).
- Sämtliche vom Programm erzeugten Meldungen sind Englisch: Hilfe, Validierungs-, Fehler-, Log- und Statusmeldungen. Datenfeldnamen bleiben sprachunabhängig; Originalnamen aus Quellen bleiben erhalten. Projektdokumentation bleibt Deutsch/Englisch.
- Beobachtungsdaten enthalten den Messzeitpunkt jeder ausgegebenen Messgröße in UTC ohne Zeitzonensuffix. Unterschiedliche Messzeitpunkte bleiben je Wert erhalten; zeitliche Zusammenführung, Auswahl und Bewertung übernimmt der Konsument. Geschätzte Quellzeiten bleiben entsprechend gekennzeichnet. Vorhersagen behalten ihre eigenen Ausgabe-/Gültigkeitszeiten.
- Erst nach Normalisierung und Umrechnung für die Ausgabe runden: Temperatur auf 0,1 °C, Druck auf 1 hPa bzw. 1 mmHg, relative Feuchtigkeit auf 1 Prozentpunkt und Windgeschwindigkeit auf 1 km/h. Beide Druckwerte aus demselben ungerundeten Ausgangswert berechnen; Rundungstoleranzen berücksichtigen. Exakte Mittelpunktwerte vom Nullpunkt weg runden (20,25 → 20,3; −20,25 → −20,3). Andere Größen erhalten keine neue Rundungsvorgabe.
- Für die öffentlichen XML-Exportentitäten werden reproduzierbar XSD-Dateien generiert. XML-Serializer-Vertragstests validieren Exporte gegen diese Schemas. Diese Ausgabeschemas sind von der DWD-MOSMIX-Quell-XSD getrennt; Generierung/Versionierung wird in P2 umgesetzt.
- Tests verwenden MSTest, Moq und FluentAssertions; Coverage wird mit Microsoft.CodeCoverage erfasst. Konkrete Versionen, Test-Runner-Integration und Plattformnachweise werden in P1 festgehalten.
- Coverage-Ausnahme: Jede Kennzahl mit exakt 100 % im Zielbranch darf im PR bei exakt 100 % bleiben. Für andere als reine Dokumentations-PRs bleibt unter 100 % der strikte Anstieg erforderlich; beim erstmaligen Start ohne Baseline entfällt der Vergleich. Jeweils mindestens 80 %. Angezeigte gerundete 100 % reichen nicht.
- Verbleibende CLI-Fragen werden bei der CLI-Entwicklung in P3 entschieden: Konfigurationsformat/-pfad, Umgebungsvariablen, Secret-Verwaltung, konkrete Exitcodes und CLI-Abbruch-/Timeoutgrenzen. Sie sind keine P0-/P1-Voraussetzung für die Bibliothek. Öffentliche Bibliotheks-Fehler-/Cancellation-Verträge bleiben davon getrennt; P1 darf für Testzwecke minimale Prozesskonventionen dokumentieren, ohne den endgültigen CLI-Vertrag vorwegzunehmen.

Diese Entscheidungen aktualisieren ausschließlich die Dokumentation; es wurden weder Anwendungscode noch XSD-Dateien generiert.


## P0-Präzisierungen vom 14.10.2026

- Abhängigkeiten: Bei der Aufnahme bzw. einem beauftragten Update die aktuellsten verfügbaren stabilen Versionen wählen, auf .NET-/Plattformkompatibilität und Lizenzbedingungen prüfen und die konkret aufgelösten Versionen reproduzierbar fixieren. Keine ungeprüften Floating-Versionen bei jedem Build; ein Konflikt wird dokumentiert statt unbemerkt eine ältere Version zu wählen.
- Coverage-Start: Mindestens 80 % je Kennzahl, ohne Zielbranch-Vergleich bei noch nicht vorhandener messbarer Baseline. Dokumentations-PRs dürfen Gleichstand haben. Die bestehende exakte 100-%-Ausnahme bleibt gültig. Reports von Microsoft.CodeCoverage mit Codeblock-Aufschlüsselung werden zur Prüfung verwendet.
- Rundung: Auf den nächstgelegenen vereinbarten Schritt runden; bei genau halben Schritten vom Nullpunkt weg (`MidpointRounding.AwayFromZero`), beispielsweise 20,25 → 20,3 und −20,25 → −20,3. Nach Umrechnung runden, nicht vorher.
- Quellen: Die konkreten DWD- und OsthessenWetter-Endpunkte liefert der Konsument über die Konstruktorparameter des jeweiligen Parsers. Quellenpfade werden weder fest eingebaut noch als offene globale P0-Konfigurationsentscheidung behandelt. Konkrete Quellenformate und Parameterabbildungen sind bei der jeweiligen Adapterimplementierung zu dokumentieren.
- OsthessenWetter: Ist die konfigurierte Seite nicht abrufbar, löst der Parser eine Exception mit nachvollziehbarer englischer Diagnose aus; keine Ersatzdaten oder vorgetäuschten Erfolge. Eine abrufbare Seite wird in der dokumentierten As-is-Struktur ausgelesen, einschließlich der dort angegebenen Messzeitpunkte. Keine zusätzliche Aktualitätsbewertung oder künstliche Korrektur der Quelldaten; der Konsument bewertet die Messzeitpunkte. Gemeinsame UTC-/Einheiten-/Rundungsregeln bleiben gültig. Bei nur einer Uhrzeit bleibt die ausdrücklich erlaubte Datumsableitung nach REQ-042 als geschätzt gekennzeichnet; ein vollständiger Quellzeitpunkt wird nicht ersetzt. CLI-Fehlerdarstellung und Exitcodes werden in P3 festgelegt.

Diese Festlegungen sind Dokumentationsentscheidungen; sie erzeugen noch keinen Anwendungscode.


## Verbindlicher Phasenstatus und Ablauf

**15.10.2026: P0 abgeschlossen; P1-Solution-/Projektstruktur erstellt. P1 ist noch nicht abgenommen.**

Reihenfolge bei unveränderten Phasen-IDs: **P0 → P1 → P2 → P4 → P5 → P6 → P7 → P8 → P3 → P9**. Erst die vollständige Bibliothek einschließlich Stationsauswahl, DWD-/OsthessenWetter-/MOSMIX_S-/MOSMIX_L-Parsern, Ausgabeentitäten, JSON/XML, XSDs, MQTT-Serializer und DI fertigstellen und abnehmen. Danach P3 mit CLI-Nutzungsbeispiel, Konfiguration und Beispiel-Brokertransport beginnen. Öffentliches Release bleibt P9.

P1 enthält nur einen technischen Paketkonsumenten als Integrationstest, kein CLI-/Anwendungsbeispiel. Vor P3 Quellenfunktionen direkt über die Bibliothek und ihre Tests abnehmen; kein CLI-Aufruf ist Voraussetzung für die Bibliotheksphasen. Die Phasenreihenfolge ersetzt frühere numerische Reihenfolge oder Abhängigkeiten auf P3. [Detailplan P1](PHASE_P1_PLAN.md). Der aktuelle Auftrag ergänzt die Projektstruktur; Fachimplementierung bleibt vertagt.
