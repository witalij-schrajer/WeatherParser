# WeatherParser-Betrieb

> Deutsche Übersetzung von [docs/OPERATIONS.md](../OPERATIONS.md). Maßgeblich ist die englische Fassung; die Übersetzung wird vor jeder neuen Version mit ihr abgeglichen und kann bis dahin zurückliegen. Übersetzungsstand: englische Fassung von `features/develop/release-1.0.0` `8659f5b` (2026-11-03, Version 1.0.0). Die verlinkten Protokolle sind englisch.

**Status (2026-10-31): geschrieben in P9.07 (Bereitstellung) und P9.08 (der Rest der Anleitung); die Beispiele tragen ihren Verifikationsstatus (Abschnitt 11); auf dem NanoPi R76S in P9.10 am Release-Kandidaten `86e77eb` geprüft (2026-11-01 bis 2026-11-02).** Diese Anleitung behandelt die Beispiel-CLI `WeatherParser` auf Debian 13 ARM64 (`linux-arm64`, zum Beispiel der NanoPi R76S). Die Bibliothek selbst wird als NuGet-Paket genutzt und benötigt keine eigene Bereitstellung; ihre Konsumenten folgen den README-Abschnitten der jeweiligen Quelle. Jeder Host, jede ID, jeder Pfad und jedes Topic unten ist ein Platzhalter.

## 1. Bereitstellung (RL-07 A)

Alle Pfade, der Benutzer und die Gruppe unten sind dokumentierte Beispiele, keine Standardwerte: Die CLI liest von sich aus keine Konfiguration und benötigt für jeden Befehl `--Config <Pfad>` oder `WEATHERPARSER_CONFIG` (CL-04, REQ-064).

### Laufzeit

Die CLI ist ein frameworkabhängiges `linux-arm64`-Publish und benötigt die .NET-Laufzeit 10.0.14 oder neuer (`Microsoft.NETCore.App`; die ASP.NET-Core-Laufzeit wird nicht benötigt). Installieren Sie sie aus dem Microsoft-Paketfeed für Debian 13 und halten Sie sie mit den Systemaktualisierungen aktuell:

```bash
sudo apt-get install dotnet-runtime-10.0
dotnet --list-runtimes
```

Folgen Sie zuerst Microsofts Anleitung zum Hinzufügen des Paketfeeds unter Debian 13 ([.NET unter Debian installieren](https://learn.microsoft.com/dotnet/core/install/linux-debian)). Eine auf anderem Weg installierte Laufzeit (zum Beispiel mit dem Skript `dotnet-install` in einen Benutzerordner) funktioniert ebenfalls, wenn `DOTNET_ROOT` auf sie zeigt; Installation und Aktualisierung liegen dann in der Verantwortung des Betreibers.

### Die CLI aus dem getaggten Quellcode bauen (RL-08 A)

Die CLI ist keinem Release beigefügt. Ein Betreiber baut sie aus dem Release-Tag gegen das veröffentlichte Paket, mit dem .NET-SDK, das `global.json` festlegt (`rollForward` `disable`; CONTRIBUTING.md Abschnitt 1, Englisch), auf jeder Maschine mit diesem SDK; die Ausgabe ist frameworkabhängig für `linux-arm64` und läuft auf dem Zielsystem mit der Laufzeit oben. Bis das Paket veröffentlicht ist, ist die Release-`.nupkg` (geprüft gegen die `SHA256SUMS` des Releases) die einzige Kopie von `WeatherParser.Parsing` in der lokalen Paketquelle `artifacts/local-nuget`, die `NuGet.Config` nennt; nach einer Veröffentlichung kommt sie von nuget.org. Ein frischer Paket-Cache verhindert, dass ein veralteter früherer `0.1.0`-Build verwendet wird (CONTRIBUTING.md Abschnitt 4):

```bash
git clone <Repository-URL> weatherparser
cd weatherparser
git checkout --detach v<Version>
mkdir -p artifacts/local-nuget
cp <Release-Ordner>/WeatherParser.Parsing.<Version>.nupkg artifacts/local-nuget/
export NUGET_PACKAGES="$PWD/.nuget-packages"
dotnet restore examples/WeatherParser.Cli -r linux-arm64 --force-evaluate
dotnet publish examples/WeatherParser.Cli -c Release -r linux-arm64 --self-contained false --no-restore -o publish
```

Prüfen Sie, dass die CLI genau das veröffentlichte Paket aufgelöst hat: `examples/WeatherParser.Cli/obj/project.assets.json` muss die Bibliothek `WeatherParser.Parsing/<Version>` mit einem `sha512` aufführen, der dem Base64-SHA-512 der Release-`.nupkg` entspricht (der Release-Build macht dieselbe Prüfung, `PACKAGE-MISMATCH`):

```bash
python3 -c "import json,sys; print(json.load(open(sys.argv[1]))['libraries']['WeatherParser.Parsing/'+sys.argv[2]]['sha512'])" examples/WeatherParser.Cli/obj/project.assets.json <Version>
openssl dgst -sha512 -binary artifacts/local-nuget/WeatherParser.Parsing.<Version>.nupkg | base64 -w0; echo
```

Die beiden Zeilen müssen gleich sein. Der Ordner `publish` tritt dann in den Befehlen unten an die Stelle des entpackten Archivs (`sudo cp -r publish/. /opt/weatherparser/<Version>/` statt der Zeile `python3 … extractall`). Das private CLI-Abnahmeartefakt des Release-Builds, `WeatherParser.Cli.<Version>.linux-arm64.zip`, wird auf dieselbe Weise gebaut; es dient der Release-Abnahme (P9.07 bis P9.10) und ist kein Release-Download.

### Struktur, Benutzer und Berechtigungen

Die CLI, entweder der oben gebaute Ordner `publish` oder, für die Release-Abnahme, das CLI-Abnahmeartefakt `WeatherParser.Cli.<Version>.linux-arm64.zip`, geprüft gegen seine Zeile in `SHA256SUMS`, kommt in einen versionierten Ordner; ein stabiler symbolischer Link benennt die aktuelle Version, ein Update ist also Entpacken plus Ändern des Links, und ein Rollback ist der umgekehrte Weg:

```bash
sha256sum -c --ignore-missing SHA256SUMS
sudo useradd --system --no-create-home --shell /usr/sbin/nologin weatherparser
sudo install -d -m 0755 /opt/weatherparser/<Version>
sudo python3 -c "import zipfile,sys; zipfile.ZipFile(sys.argv[1]).extractall(sys.argv[2])" WeatherParser.Cli.<Version>.linux-arm64.zip /opt/weatherparser/<Version>
sudo chmod 0755 /opt/weatherparser/<Version>/WeatherParser
sudo ln -sfn /opt/weatherparser/<Version> /opt/weatherparser/current
sudo install -d -m 0750 -o root -g weatherparser /etc/weatherparser
```

(`unzip -d` funktioniert genauso, wo es installiert ist; `zipfile` übernimmt keine Berechtigungsbits, daher das `chmod` des App-Hosts.)

- Konfigurationsdateien unter `/etc/weatherparser/`: Eigentümer `root`, Gruppe `weatherparser`, Modus `0640`. Kopieren Sie `examples/weatherparser.example.json` und ersetzen Sie die Platzhalterendpunkte; überschreiben Sie nie eine bestehende Konfiguration.
- Datei mit den Broker-Zugangsdaten (nur MQTT-Modus): Eigentümer `weatherparser`, Modus `0600` (CL-06); die CLI weist eine Datei, die einem anderen Benutzer gehört oder auf die Gruppe oder andere zugreifen können, vor jeder Anfrage mit `CLI-CREDENTIALS-INVALID` und Exit-Code 2 zurück.

  Nur bei der Erstinstallation; der Befehl weigert sich, eine bestehende Datei zu ersetzen, sodass seine Wiederholung bei einem Update nie funktionierende Zugangsdaten leert:

```bash
sudo test ! -e /etc/weatherparser/broker.credentials.json && sudo install -m 0600 -o weatherparser -g weatherparser /dev/null /etc/weatherparser/broker.credentials.json
```

  Schreiben Sie dann die Elemente `Username` und `Password` mit einem als root gestarteten Editor hinein; schreiben Sie Zugangsdaten nie in eine Befehlszeile oder in die Konfigurationsdatei.

- MOSMIX_S-Nachweiscache (nur für `--SelectMOSMIX`): ein existierendes Verzeichnis, das nur der Cache verwendet, für den Dienstbenutzer beschreibbar, absolut in `Mosmix.Catalog.EvidenceCacheDirectory` angegeben:

```bash
sudo install -d -m 0750 -o weatherparser -g weatherparser /var/lib/weatherparser/mosmix-evidence
```

### Updates

Ein Update entpackt die neue Version in ihren eigenen Ordner und verschiebt den symbolischen Link (`sudo ln -sfn /opt/weatherparser/<neue Version> /opt/weatherparser/current`); es berührt `/etc/weatherparser/` nicht. Konfigurationsdateien und die Datei mit den Zugangsdaten werden nur bei der Erstinstallation angelegt und nie über eine bestehende Datei kopiert; vergleichen Sie eine neue Beispielkonfiguration von Hand mit der bestehenden. Lesen Sie zuerst das [Changelog](../../CHANGELOG.md) (Englisch) der neuen Version.

### Aufruf

Jeder Aufruf führt genau einen Befehl aus und endet; es gibt keinen Dienst, keinen Timer und keinen Daemon zu installieren. Ein Scheduler startet sie als Dienstbenutzer ohne Terminal, zum Beispiel:

```bash
sudo -u weatherparser /opt/weatherparser/current/WeatherParser --ParseDWD --Output json --Config /etc/weatherparser/weatherparser.json < /dev/null
```

Das Dokument geht auf die Standardausgabe (oder in `--OutputFile`), Logs gehen auf die Standardfehlerausgabe, und der Exit-Code meldet das Ergebnis (Abschnitt 4).

## 2. Konfiguration und Geheimnisse

- Eine JSON-Konfigurationsdatei je Verwendung, angegeben mit `--Config` oder `WEATHERPARSER_CONFIG`; es gibt keinen impliziten Suchpfad (CL-04). Ihre Abschnitte, Elemente, Standardwerte und Bereiche stehen in der README im Abschnitt „Beispiel-CLI“, Unterabschnitt „Konfiguration“ ([README](README.md#konfiguration)). Unbekannte Elemente, Kommentare und abschließende Kommas werden abgelehnt (`CLI-CONFIG-INVALID`, Exit-Code 2), ein Tippfehler fällt also nie still auf einen Standardwert zurück.
- Mehrere Dateien sind in Ordnung, zum Beispiel eine je Befehl oder je Quelle: Jeder Aufruf liest genau die eine, die ihm angegeben wird.
- Geheimnisse sind nur die Broker-Zugangsdaten. Sie stammen entweder aus der Datei mit den Zugangsdaten (Abschnitt 1) oder aus den Umgebungsvariablen `WEATHERPARSER_MQTT_USERNAME` und `WEATHERPARSER_MQTT_PASSWORD`, nie aus beiden und nie aus Argumenten oder der Konfigurationsdatei. Bevorzugen Sie die Datei: Umgebungsvariablen eines geplanten Jobs sind für den Eigentümer des Jobs und für root sichtbar und können in den eigenen Logs des Schedulers landen.
- Der JSON- und der XML-Modus lesen keine Zugangsdaten und kontaktieren nie einen Broker.
- `OsthessenWetter.PublicationPermitted` bleibt `false` (Abschnitt 9).
- Setzen Sie `Http.UserAgent` auf einen Wert, der die Installation des Betreibers kennzeichnet (zum Beispiel `WeatherParser/<version> (contact: ops@example.invalid)`); der Standard ist `WeatherParser/<version>`.

## 3. Zeitplanung

Zeitplanung und Wiederholungen über Aufrufe hinweg gehören zum Aufrufer (REQ-007, REQ-008); für WeatherParser selbst muss nichts installiert werden, und weder Cron noch n8n ist eine Abhängigkeit.

### Cron

Eine Datei in `/etc/cron.d/` nennt den Benutzer, der Job läuft also als Dienstbenutzer (Platzhalterpfade und -IDs; ein Befehl je Zeile):

```text
# /etc/cron.d/weatherparser
WEATHERPARSER_CONFIG=/etc/weatherparser/weatherparser.json
*/10 * * * * weatherparser /opt/weatherparser/current/WeatherParser --ParseDWD --StationID <DWD-STATIONS-ID> < /dev/null >> /var/log/weatherparser/weatherparser.log 2>&1 || logger -t weatherparser "ParseDWD failed with exit code $?"
50 * * * * weatherparser /opt/weatherparser/current/WeatherParser --ParseMOSMIX --Product MOSMIX_S --MOSMIXID <MOSMIX-ID> < /dev/null >> /var/log/weatherparser/weatherparser.log 2>&1 || logger -t weatherparser "ParseMOSMIX MOSMIX_S failed with exit code $?"
25 4,10,16,22 * * * weatherparser /opt/weatherparser/current/WeatherParser --ParseMOSMIX --Product MOSMIX_L --MOSMIXID <MOSMIX-ID> < /dev/null >> /var/log/weatherparser/weatherparser.log 2>&1 || logger -t weatherparser "ParseMOSMIX MOSMIX_L failed with exit code $?"
```

Cron plant in der Systemzeitzone des Hosts (`timedatectl`), und eine Einstellung `TZ` in der Crontab ändert das nicht. Die MOSMIX_L-Zeile oben setzt einen Host in UTC voraus; auf einem Host in `Europe/Berlin` verwenden Sie `30 0,6,12,18 * * *`, was sowohl im Winter (MEZ) als auch im Sommer (MESZ) nach der Veröffentlichung jedes Laufs liegt. Die anderen Zeilen hängen nicht von der Zeitzone ab. Cron führt jede Zeile mit `/bin/sh` aus; `|| logger …` meldet jeden Exit-Code außer 0 an das Systemprotokoll (`$?` ist dort der Exit-Code der CLI). Das Log-Verzeichnis wird einmal mit `sudo install -d -m 0750 -o weatherparser -g weatherparser /var/log/weatherparser` angelegt und wie in Abschnitt 5 rotiert; um stattdessen das Journal zu verwenden, ersetzen Sie die Umleitung durch `2>&1 | systemd-cat -t weatherparser` (der Exit-Code ist dann der der Pipe und wird nicht gemeldet). Im MQTT-Modus (dem Standard) bleibt die Standardausgabe leer; im JSON- oder XML-Modus verwenden Sie `--OutputFile` mit einem neuen Dateinamen je Lauf, weil die CLI eine bestehende Datei nie überschreibt (`CLI-OUTPUT-EXISTS`).

### n8n

Ein Knoten „Execute Command“ führt die CLI auf der Maschine aus, auf der n8n läuft, zum Beispiel `/opt/weatherparser/current/WeatherParser --ParseDWD --StationID <DWD-STATIONS-ID> --Output json --Config /etc/weatherparser/weatherparser.json`, übernimmt das JSON-Dokument von der Standardausgabe und verzweigt nach dem Exit-Code (Abschnitt 4). Der Knoten läuft als n8n-Benutzer: Dieser Benutzer benötigt dann Lesezugriff auf die Konfiguration (zum Beispiel durch Mitgliedschaft in der Gruppe `weatherparser`) und muss im MQTT-Modus Eigentümer der Datei mit den Zugangsdaten sein (CL-06); geben Sie n8n deshalb eine eigene Konfiguration und eine eigene Datei mit Zugangsdaten, statt die des Cron-Benutzers mitzubenutzen. Der Zeitplan-Trigger von n8n ersetzt Cron; WeatherParser selbst plant oder wiederholt nie etwas.

## 4. Exit-Codes für Aufrufer

| Exit-Code | Bedeutung | Typische Reaktion des Aufrufers |
|---|---|---|
| `0` | Vollständiger Erfolg, auch eine leere Auswahl und `--Help` | Das Ergebnis verwenden |
| `1` | Vollständiger Fehlschlag zur Laufzeit (Abruf, Ausgabe, Verbindung oder Veröffentlichung; eine abgelaufene Befehlsfrist ohne veröffentlichte Nachricht) | Beim nächsten geplanten Lauf erneut versuchen; nach wiederholten Fehlschlägen alarmieren |
| `2` | Bedien- oder Konfigurationsfehler, vor jedem Abruf erkannt | Konfiguration oder Aufruf korrigieren; eine Wiederholung hilft nicht |
| `3` | Teilweiser Fehlschlag: Mindestens ein Eintrag war erfolgreich, und mindestens einer schlug fehl | Die erfolgreichen Einträge verwenden; die Liste `errors` des Dokuments und das Log nennen die fehlgeschlagenen |
| `4` | Abgebrochen durch SIGINT, SIGTERM oder Strg+C, auch wenn vor dem Signal einige Einträge erfolgreich waren | Der Befehl wurde unterbrochen: Vor dem Signal bestätigte Nachrichten bleiben veröffentlicht (und retained), es erscheint keine Ausgabedatei; die Zeile `CLI-PUBLICATION-SUMMARY` nennt die Anzahlen. Der nächste Lauf startet normal und kann dieselben Werte erneut veröffentlichen |

Ein teilweiser Fehlschlag ist bei Befehlen mit mehreren Einträgen normal, zum Beispiel bei einer OsthessenWetter-Station, die die Seite als offline kennzeichnet. Hinweise wie `SELECTION-EMPTY`, `MOSMIX-RUN-STALE` oder `MOSMIX-S-EVIDENCE-STALE` werden protokolliert und lassen den Exit-Code unverändert; prüfen Sie die Aktualität in den Daten (Abschnitt 8), nicht im Exit-Code. Die Fehler-IDs stehen in der README im Abschnitt „Beispiel-CLI“, Unterabschnitt „Fehler-IDs der CLI“ ([README](README.md#fehler-ids-der-cli)).

## 5. Logs

Logs gehen nur auf die Standardfehlerausgabe, eine Zeile je Eintrag: `<UTC-Zeit> UTC <LEVEL> <ID> command=<Befehl> [source=<Quelle>] <Meldung>`. Sie enthalten keine Zugangsdaten, keine Konfigurationsobjekte und keine Stacktraces. Bewahren Sie sie dort auf, wohin der Scheduler sie schreibt:

- im systemd-Journal (`systemd-cat -t weatherparser`, gelesen mit `journalctl -t weatherparser`), für das dessen Größengrenzen gelten; oder
- in einer Datei, an die der Job anhängt, rotiert von `logrotate`, zum Beispiel `/etc/logrotate.d/weatherparser` (eine Direktive je Zeile; prüfen Sie sie mit `sudo logrotate --debug /etc/logrotate.d/weatherparser`):

```text
/var/log/weatherparser/*.log {
    weekly
    rotate 8
    compress
    missingok
    notifempty
    su weatherparser weatherparser
}
```

`--Verbose` ergänzt Debug-Zeilen; verwenden Sie es zur Diagnose, nicht in geplanten Jobs.

## 6. Anfragevolumen und Intervalle

Das Projekt empfiehlt diese Intervalle; sie richten sich nach den Zyklen und Bedingungen der Anbieter, nicht nach einer Regel der Anbieter (der DWD dokumentiert keine Begrenzung der Anfragerate; OsthessenWetter dokumentiert keine). Der Zeitplan ist die Entscheidung des Betreibers (REQ-008).

| Befehl | Anfragen je Lauf | Empfohlenes Intervall |
|---|---|---|
| `--ParseDWD` | Die Stationslisten und die `now`-Dateien einer Station (einige kleine Dateien) | Höchstens einmal alle 10 Minuten je Station; der DWD aktualisiert die 10-Minuten-`now`-Daten innerhalb der Stunde |
| `--ParseMOSMIX --Product MOSMIX_S` | Ein Download der vollständigen MOSMIX_S-Datei (etwa 36 MB) je Standort | Höchstens einmal je stündlichem Lauf, etwa 45 Minuten nach der vollen Stunde |
| `--ParseMOSMIX --Product MOSMIX_L` | Eine Datei je Standort (etwa 17 KB) | Höchstens einmal je Hauptlauf, nach etwa 04:20, 10:15, 16:20 und 22:15 UTC (Läufe um 03, 09, 15 und 21 UTC) |
| `--ParseOstHessen` | Eine Seite | Höchstens einmal alle 10 Minuten; eine produktive Nutzung wird nicht akzeptiert, solange OW-14 gilt (Abschnitt 9) |
| `--SelectDWD`, `--SelectMOSMIX` | Die Kataloge und der Nachweis je Station | Nur zur Konfigurationszeit: wenn eine Station oder ein Standort gewählt oder überprüft wird, höchstens einmal pro Tag und Suche |
| `--SelectMOSMIX --RefreshMosmixSEvidence` | Ein vollständiger MOSMIX_S-Download | Selten, zum Beispiel einmal pro Tag vor einer Auswahl |

Mehrere MOSMIX_S-Standorte laden jeweils die ganze Datei herunter; parsen Sie nur die benötigten Standorte. Der DWD speichert die anfragende IP-Adresse höchstens 7 Tage (Abschnitt 9).

## 7. Einrichtung des Brokers

- **Eigene Zugangsdaten**: ein Brokerbenutzer je CLI-Installation (und ein eigener für eine von n8n gesteuerte Installation), mit einem langen zufälligen Passwort, gespeichert wie in Abschnitt 1.
- **TLS mit Prüfung**: Lassen Sie TLS eingeschaltet (der Standard) und geben Sie `Mqtt.Broker.Tls.CaFile` die PEM-Datei einer privaten CA, wenn das Brokerzertifikat nicht im Vertrauensspeicher des Systems liegt; die CLI prüft immer die Kette und den Hostnamen und hat keinen Schalter, um das abzuschalten. `AllowUnencrypted: true` ist nur für ein vertrauenswürdiges lokales Netz gedacht und protokolliert je Lauf eine Warnung.
- **Eingeschränkte Berechtigungen**: Erlauben Sie dem Brokerbenutzer der CLI, nur unterhalb seiner konfigurierten Topic-Bäume zu veröffentlichen und nichts zu abonnieren. Für Mosquitto zum Beispiel eine ACL-Datei mit `user weatherparser`, gefolgt von `topic write haus/weather/measurements/#`, `topic write haus/weather/forecasts/#` und `topic write haus/weather/stations/#` (Platzhalter-Topics der Beispielkonfiguration).
- **MQTT 5.0**: Bevorzugen Sie Protokoll 5.0; mit 3.1.1 kann ein Broker eine Nachricht bestätigen, die er dann verwirft (zum Beispiel wegen einer fehlenden Berechtigung), was die CLI nicht erkennen kann (Abschnitt 10).
- **Retained-Zustand**: Beobachtungen und vollständige Vorhersageläufe werden mit Retain veröffentlicht, Stationslisten ohne; QoS ist für alle 1. Ein neuer Lauf derselben Quelle ersetzt die Retained-Nachricht seines Topics; die Topic-Vorlagen müssen Quellen und Produkte deshalb trennen (zum Beispiel `{source}` in der Vorlage und getrennte Vorlagen für Beobachtungen und Vorhersagen; MOSMIX_S und MOSMIX_L haben unterschiedliche Quellen). Um eine Retained-Nachricht zu löschen, veröffentlichen Sie mit einem Broker-Werkzeug eine leere Retained-Nachricht auf ihr Topic. Eine wiederholte QoS-1-Nachricht kann Abonnenten zweimal erreichen.

## 8. Geschätzte Daten und Aktualität

- **DWD-Beobachtungen** tragen die Messzeit jedes Werts in UTC; die CLI filtert alte Werte nicht. Konsumenten vergleichen `measured_at` mit ihrer eigenen Uhr.
- **OsthessenWetter** zeigt nur eine Uhrzeit. Das Datum wird in `Europe/Berlin` aus der Abrufzeit abgeleitet, und jeder Messwert wird mit `timestamp_quality` `estimated_date` gekennzeichnet (REQ-042); eine Anzeige, die älter als ein Tag ist, lässt sich nicht erkennen. Messwerte in einer Lücke oder Überlappung der Sommerzeitumstellung werden nicht exportiert (`OSTHESSEN-TIME-UNRESOLVED`).
- **MOSMIX**-Läufe halten `issued_at`, `retrieved_at` und die Gültigkeit jedes Schritts getrennt. Ein Lauf, der älter als `Mosmix.L.MaxRunAge` oder `Mosmix.S.MaxRunAge` ist, wird trotzdem exportiert, mit einer Warnung `MOSMIX-RUN-STALE` und dem Aktualitätskennzeichen im Dokument. Richtwert, kein Standard: MOSMIX_S 2 Stunden, MOSMIX_L 8 Stunden.
- **MOSMIX_S-Nachweisindex**: Ein Index, der älter als `MosmixSEvidenceMaxAge` ist, wird weiter verwendet, mit einer Warnung `MOSMIX-S-EVIDENCE-STALE`; aktualisieren Sie ihn mit `--RefreshMosmixSEvidence`.

## 9. Quellenbedingungen

- **DWD** (Beobachtungen, MOSMIX, Kataloge): offene Daten unter [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) mit Quellenangabe ([Rechtliche Hinweise des DWD](https://www.dwd.de/EN/service/legal_notice/legal_notice_node.html)). Überall, wo Werte angezeigt oder weiterveröffentlicht werden, zum Beispiel über MQTT oder in Dateien, behalten Sie die Quellenangabe „Deutscher Wetterdienst (DWD)“ bei. Der Open-Data-Server speichert die anfragende IP-Adresse höchstens 7 Tage.
- **OsthessenWetter** veröffentlicht keine Lizenz für seine Messwerte und keine Schnittstelle, und sein Impressum untersagt die Verwendung seiner Inhalte in anderen Veröffentlichungen ohne ausdrückliche Zustimmung des Autors. **Produktive Nutzung und Weiterverbreitung seiner Werte werden** von diesem Projekt **nicht akzeptiert**, bis die Zustimmung des Betreibers oder anderweitig geklärte Bedingungen festgehalten sind (Vertrag OW-14): Lassen Sie `OsthessenWetter.PublicationPermitted` auf `false`, wodurch `--ParseOstHessen --Output mqtt` vor jeder Anfrage fehlschlägt, planen Sie `--ParseOstHessen` nicht für den Produktivbetrieb ein, und verwenden Sie JSON- und XML-Ausgabe nur für den lokalen Benutzer, nie zur Weiterverbreitung. OW-14 wird nur durch eine festgehaltene Entscheidung nach der Zustimmung des Betreibers oder anderweitig geklärten Bedingungen aufgehoben. Nennen Sie die Quelle „Osthessen-Wetter.de“ überall, wo Werte angezeigt werden.
- Die MIT-Lizenz von WeatherParser umfasst den Code, nicht die Daten oder die Websites.

## 10. Einschränkungen

Die bekannten Einschränkungen des Releases mit ihren Verantwortlichen stehen im [Changelog](../../CHANGELOG.md) (Englisch). Für den Betrieb insbesondere:

- TLS und Authentifizierung wurden gegen ein Brokerprodukt (Mosquitto 2.1.2) mit einer selbst signierten Test-CA in `Tls.CaFile` verifiziert ([P9.09-Protokoll](../p9/P9_09_BROKER_AND_SHUTDOWN_CHECKS.md)); der Vertrauensspeicher des Systems und eine öffentliche CA wurden nicht gezeigt.
- Mit MQTT 3.1.1 kann eine verworfene Nachricht bestätigt aussehen; verwenden Sie 5.0.
- Nach einem Abbruch oder einer abgelaufenen Frist ist die Trennung vom Broker durch `Limits.ConnectTimeout` begrenzt, der Prozess kann also bis zu dieser Dauer nach `Limits.CommandDeadline` enden.
- Windows ist eine Entwicklungsplattform, kein Release-Ziel; ein Herunterfahren von Windows während eines Laufs ist nicht getestet.
- Der OsthessenWetter-Parser unterstützt nur den dokumentierten Stand der Seite; eine geänderte Seite schlägt mit `OSTHESSEN-FORMAT-CHANGED` fehl.

## 11. Verifikationsstatus der Beispiele

Statuswerte wie in CONTRIBUTING.md („Status values“, Englisch).

| Beispiel | Status |
|---|---|
| Abschnitt 1, Prüfsumme, Entpacken, symbolischer Link, Modi von Konfiguration und Zugangsdaten, Aufruf ohne Terminal | Verifiziert auf dem NanoPi R76S in P9.07 in einem Wegwerf-Home-Ordner als Anmeldebenutzer, ohne root ([P9-Verifikationsprotokoll](../p9/P9_VERIFICATION.md) Abschnitt 8, [Lauf auf dem Zielsystem](../p9/P9_07_TARGET_RUN.md)), und in P9.10 mit dem Dienstbenutzer, den Systempfaden, der Datei mit den Zugangsdaten und der Laufzeit 10.0.14 ([P9.10-Protokoll](../p9/P9_10_ARM64_RELEASE_ACCEPTANCE.md) Abschnitte 4 und 7) |
| Abschnitt 1, Bauen der CLI aus dem getaggten Quellcode | Geplant für P9.12 (die Prüfung nach der Veröffentlichung gemäß RL-08 A wiederholt diesen Build einmal); dieselbe Wiederherstellung, dasselbe Publish und dieselbe Paketprüfung laufen in jedem Release-Build (`scripts/build-release.ps1`, verifiziert in P9.06 und P9.10) |
| Abschnitt 1, Verzeichnis des MOSMIX_S-Nachweiscaches; Updates und Rollback | Verzeichnis des Nachweiscaches verifiziert in P9.10 (mit seinem Eigentümer angelegt, gefüllt durch das Live-`--SelectMOSMIX --RefreshMosmixSEvidence`); Updates und Rollback verifiziert in P9.10 durch Verschieben von `current` auf einen zweiten Versionsordner und zurück, mit unverändertem `/etc/weatherparser` und unveränderter Datei mit den Zugangsdaten (dasselbe Artefakt in beiden Ordnern; [P9.10-Protokoll](../p9/P9_10_ARM64_RELEASE_ACCEPTANCE.md) Abschnitt 7) |
| Abschnitt 3, Cron-Einträge; n8n | Cron verifiziert in P9.10: die Cron-Zeile der Anleitung mit `logger` unter `/bin/sh` und eine Serie der dokumentierten Einträge über 36,5 Stunden, MOSMIX_L um `30 0,6,12,18` auf einem Host in `Europe/Berlin` ([P9.10-Protokoll](../p9/P9_10_ARM64_RELEASE_ACCEPTANCE.md) Abschnitte 5 und 7); n8n ist nur ein Beispiel: seine Befehlszeile lief als Shell-Befehl, n8n selbst nicht verifiziert |
| Abschnitt 4, Exit-Codes | Verifiziert durch die CLI-Tests (P3) und auf dem NanoPi in P3.11 und P9.07 |
| Abschnitt 5, Journal und logrotate | Journal verifiziert in P9.10; der logrotate-Abschnitt mit `logrotate --debug` geprüft (Exit 0, keine Fehler), keine Rotation durchgeführt ([P9.10-Protokoll](../p9/P9_10_ARM64_RELEASE_ACCEPTANCE.md) Abschnitt 7) |
| Abschnitt 7, Einrichtung des Brokers mit TLS, Zugangsdaten und ACL | Verifiziert in P9.09 mit Mosquitto 2.1.2, einer Test-CA in `Tls.CaFile`, einer Datei mit Zugangsdaten und einer Herausgeber-Identität nur mit Schreibrecht, unter Windows und auf dem NanoPi R76S ([P9.09-Protokoll](../p9/P9_09_BROKER_AND_SHUTDOWN_CHECKS.md)); der Vertrauensspeicher des Systems nicht gezeigt |
