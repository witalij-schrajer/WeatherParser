# Ein Problem melden

> Deutsche Übersetzung von [docs/REPORTING_ISSUES.md](../REPORTING_ISSUES.md). Maßgeblich ist die englische Fassung; die Übersetzung wird vor jeder neuen Version mit ihr abgeglichen und kann bis dahin zurückliegen. Übersetzungsstand: englische Fassung von `features/develop/release-1.0.0` `fde4c24` (2026-11-03, Version 1.0.0).

## Wo melden

Melden Sie Probleme und Wünsche in den GitHub-Issues dieses Repositorys: <https://github.com/witalij-schrajer/WeatherParser/issues>. Das Repository ist öffentlich; Issues sind also für alle sichtbar, und zum Öffnen eines Issues ist ein GitHub-Konto nötig.

Unter **New issue** stehen zwei Formulare zur Verfügung:

- **Bug report** (Fehlermeldung): Etwas in der Bibliothek oder der Beispiel-CLI verhält sich anders als dokumentiert.
- **Data source request** (Wunsch nach einer Datenquelle): Sie möchten, dass WeatherParser eine weitere Quelle unterstützt.

Eine Antwortzeit wird nicht zugesagt.

Bevor Sie ein Issue öffnen:

- Durchsuchen Sie die bestehenden Issues, offene und geschlossene.
- Lesen Sie die bekannten Einschränkungen in [CHANGELOG.md](../../CHANGELOG.md) (Englisch) und die Fehler-ID Ihres Problems in der [Bibliotheksreferenz der README](README.md#bibliotheksreferenz) oder in den Fehler-IDs der [Beispiel-CLI](README.md#beispiel-cli).

## Was Sie weglassen müssen

Issues sind für alle sichtbar. Veröffentlichen Sie niemals:

- Passwörter, Tokens, API-Schlüssel, Broker-Zugangsdaten oder Dateien mit Zugangsdaten;
- private URLs, Hostnamen, IP-Adressen oder Dateipfade Ihrer Installation;
- private Betriebsdaten oder ungeprüfte Logs;
- Kopien von Webseiten oder Datendateien, deren Lizenz das nicht erlaubt. OsthessenWetter-Seiten zum Beispiel dürfen nicht wiedergegeben werden.

Ersetzen Sie solche Werte durch Platzhalter wie `<Broker-Host>`, bevor Sie Konfiguration oder Logs einfügen. Log-Meldungen der Bibliothek enthalten keine Messwerte, Zugangsdaten oder Query-Strings, die Logs Ihrer Anwendung aber möglicherweise schon.

## Sicherheitsprobleme

Beschreiben Sie eine Sicherheitslücke nicht in einem Issue. Melden Sie sie vertraulich über die private Meldung von Sicherheitslücken von GitHub: Öffnen Sie auf der Seite des Repositorys **Security** und dann **Report a vulnerability** (<https://github.com/witalij-schrajer/WeatherParser/security/advisories/new>). Die Meldung ist nur für die Maintainer des Repositorys sichtbar.

## Einen Fehler melden

Eine Meldung, mit der sich das Problem nachvollziehen lässt, enthält:

| Was | Beispiel |
| --- | --- |
| Paketversion und ihre Herkunft | `WeatherParser.Parsing` 1.0.0, `SHA256SUMS` des GitHub-Releases geprüft; oder ein Build aus dem Quellcode von Commit `<ID>` |
| Ihre Laufzeit und Plattform | .NET-Laufzeit 10.0.14, Debian 13 ARM64 |
| Betroffener Teil | `IDwdObservationParser`; oder der CLI-Befehl `--ParseMOSMIX --Product MOSMIX_S` |
| Nur Bibliothek, nur CLI oder beides | |
| Minimale Reproduktion | Die Registrierung (ohne Geheimnisse) und der Aufruf, oder die CLI-Befehlszeile mit Platzhaltern |
| Erwartetes und tatsächliches Verhalten | |
| Stabile Fehler-ID oder Ausnahmetyp | `CATALOG-TIMEOUT`, `MosmixForecastException` |
| Relevante Log-Zeilen, bereinigt | Die Ereignisnamen und Meldungen rund um den Fehler |
| Wie oft es auftritt | Bei jedem Aufruf; einmal in 50 stündlichen Läufen |
| Eingabe der Quelle | Eine kleine Testdatei, die Sie teilen dürfen, oder die öffentliche URL und die UTC-Zeit des Abrufs, wenn der Inhalt nicht geteilt werden darf |

Hilfreiche Angaben zur Quelle:

- **Beobachtungen**: Anbieter und Stations-ID mit ihren führenden Nullen (`00044`), `measured_at` und `retrieved_at` des Datensatzes.
- **Vorhersagen**: Produkt (`MOSMIX_L` oder `MOSMIX_S`), MOSMIX-ID, `issued_at` und die betroffenen `valid_at`-Schritte.

Sie benötigen keinen Coverage-Bericht, keine Korrektur und keinen Pull Request. Wenn Sie unsicher sind, ob die Ursache in der Bibliothek, der Quelle oder Ihrer Einrichtung liegt, melden Sie es trotzdem und sagen Sie das dazu.

Eine Quelle, die nicht erreichbar ist, Fehler liefert oder ihr Format geändert hat, wird wie ein Fehler gemeldet, mit der Fehler-ID, zum Beispiel `OSTHESSEN-FORMAT-CHANGED`, und der UTC-Zeit. Die Bibliothek umgeht weder Ausfälle noch Zugriffsbeschränkungen.

## Eine neue Datenquelle anfragen

Beschreiben Sie, soweit bekannt, was [EXTENDING_DATA_SOURCES.md](EXTENDING_DATA_SOURCES.md#3-zuerst-den-quellenvertrag-festhalten) benötigt:

- den Anbieter und Links zu seiner offiziellen Formatdokumentation;
- Beobachtungen oder Vorhersagen und die benötigten Größen;
- die Lizenz, Quellenangabe und Zugangsbedingungen sowie etwaige Ratenbegrenzungen;
- ob eine kleine Beispieldatei geteilt werden darf, mit ihrer Quelle und Lizenz.

Eine Anfrage ist keine Freigabe, die Quelle umzusetzen. Der Maintainer entscheidet, ob und wann eine Quelle hinzugefügt wird.
