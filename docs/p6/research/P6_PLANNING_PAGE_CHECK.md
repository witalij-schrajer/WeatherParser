# P6 Planning Check – OsthessenWetter Page Structure and Source Conditions

Planning input for [PHASE_P6_PLAN.md](../../development/PHASE_P6_PLAN.md), written by the orchestrating agent on 2026-10-25. It records what one manual retrieval showed. It is not the P6.02 source-fact record: P6.02 re-checks every fact below on its own retrieval date and records the result in the OsthessenWetter contract.

The retrieved pages are not committed. The site reserves its copyright (see section 4), and REQ-083 requires synthetic fixtures. This note therefore records only structural facts, sizes and hashes, not page content beyond short identifiers needed to describe the structure, such as header labels and station names.

## 1. Retrieval

One request per URL on 2026-10-25 at about 08:34Z, with `curl` and the User-Agent `WeatherParser-planning/0.1 (one-off structure check)`, no retries:

| URL | HTTP status | Content type | Bytes | SHA-256 |
| --- | --- | --- | --- | --- |
| https://osthessen-wetter.de/robots.txt | 200 | `text/plain; charset=utf-8` | 222 | `bb68faf029190614e64249d19ea5382aaefae6aae962c317ed666a8f811ec14a` |
| https://osthessen-wetter.de/messwerte/ (REQUIREMENTS.md reference) | 200, no redirect | `text/html; charset=UTF-8` | 106558 | `5e47b731d87ccab56752c2b74d7331539d91fe72667a96de84170164fc7d2e87` |
| https://osthessen-wetter.de/impressum-datenschutzerklaerung/ | 200 | `text/html` | 96593 | `6fbf8a8f71ac631e508ea3397d61250cdde0cea4e1312477111ede9d197c8447` |

## 2. Page structure of `/messwerte/`

- The page is a WordPress page (generator meta tag `WordPress 6.6.2`) declared as UTF-8 (`<meta charset="UTF-8">`). It contains navigation, two station tables, a summary table, two iframes and two more lists of daily extremes.
- **Overview table** "Messwerte (Überblick) Wetternetz Osthessen": `<table width='100%' class='tg'>` with 20 station rows. Header cells, in order: an empty status column, `Wetterstation`, `Temp.2m` with the sub-label `Änderung`, `Temp.-Trend`, `Tmax/Tmin`, `Spitzenböe`, `Niederschlag`, `Luftfeuchte`.
- **Wind table** "Messwerte (Wind) Wetternetz Osthessen": the same table markup with 20 station rows. Header cells: an empty status column, `Wetterstation` with the sub-label `letzte Daten`, `Spitzenböe heute`, `Spitzenböe verg.10Min.`, `Wind Ø 10 Min.`, `Wind Ø 60 Min.`.
- **Row layout** (both tables):
  - first cell: an image whose `title` is `Station online` or `Station offline`;
  - second cell: the station name in a `<font size='2em'>` element, then `<br>` and the time in a `<font size='1'>` element as `HH:MM Uhr`, without a date;
  - the value cells: a number with `.` as the decimal separator followed by a unit in a nested `<font>` element (`°C`, `°C/h`, `km/h`, `mm`, `%`);
  - an empty cell where a station has no value: two stations had no precipitation, one of them also no humidity, and several stations had no 60-minute mean.
- **Offline rows**: 3 of 20 rows were marked `Station offline`. They had no time and no values.
- **Times**: only the time of day is shown. The rows showed different times (09:25 to 09:31 local time), so the times are per station, not per page.
- **Markup is not well-formed**: there are unclosed `<b>` and `<font>` elements, stray `</td>` and `</th>` end tags, header cells outside `<tr>`, and named character references without a terminating semicolon (`&szlig`, `&uumltte;`). The text is decoded correctly only by the HTML5 parsing rules for legacy character references; a regular-expression or XML parser does not do this.
- **Names differ between the two tables**: the overview table says `Roßbach` and `Müs-Hohhütte`, while the wind table says `Rossbach` and decodes to `Müs-Hohhütte;` because of the malformed reference. The row order also differs: two neighbouring stations are swapped. Rows can therefore not be joined by position.
- **No station identifiers and no coordinates** are on the page. The navigation labels show no page that lists station coordinates; the linked pages were not opened. The export contract requires coordinates (C-51), which states that OsthessenWetter cannot produce a dataset until P6 supplies them.
- **Units as displayed**: temperature °C, humidity %, wind and gusts km/h, precipitation mm. The page does not state the reference period of `Niederschlag` (it may be a daily total since local midnight; this is unverified) or of `Spitzenböe` (labelled "heute", today).
- **Error text inside the page**: the two lists "Heutige Spitzenböen" and "Heutige Niederschlagsmenge" showed a database connection error message that contained a database user and a host name instead of values. A parser must not read or log these sections.

## 3. robots.txt

`User-agent: *` with an empty `Disallow:` (Yoast block) and two sitemap lines. Crawling is not restricted for any path. robots.txt grants no licence to reuse content.

## 4. Source conditions (imprint)

- The imprint has a liability disclaimer: no warranty for currency, correctness, completeness or quality of the information.
- It states that the copyright for objects the author created remains with the author. It also states that reproducing or using such graphics, sound documents, video sequences and texts in other electronic or printed publications is not permitted without the author's express consent.
- No licence for the measured values, no attribution rule and no request-frequency rule were found. No machine-readable interface is offered (REQ-092 already states this).
- The imprint names a private person as the operator with a postal address and an e-mail address. This note does not repeat them (no personal data in the repository); P6.02 refers to "the operator named in the imprint".

Consequence for planning: the library may be implemented and tested with synthetic fixtures. Using the page in production, and redistributing its values (for example through MQTT), needs the conditions clarified with the operator first (REQ-092, DEVELOPMENT_PLAN.md P6 acceptance: "Source access without clarified conditions is not production acceptance"). Contacting the operator is the user's decision and action.
