# Reporting a problem

## Where to report

Report problems and requests in the GitHub issues of this repository: <https://github.com/witalij-schrajer/WeatherParser/issues>. The repository is private, so only people with access to it can open and read issues. If you use the library without repository access, ask the person who gave you the package to report it for you.

Two forms are available under **New issue**:

- **Bug report**: something in the library or the example CLI behaves differently from its documentation.
- **Data source request**: you want WeatherParser to support another source.

No response time is promised.

Before you open an issue:

- Search the existing issues, open and closed.
- Read the known limitations in [CHANGELOG.md](../CHANGELOG.md) and the error ID of your problem in the [README library reference](../README.md#library-reference) or the error IDs of the [example CLI](../README.md#example-cli).

## What to leave out

Issues are visible to everyone with access to the repository. Never post:

- passwords, tokens, API keys, broker credentials or credential files;
- private URLs, host names, IP addresses or file paths of your installation;
- private operational data or logs that have not been checked;
- copies of web pages or data files whose licence does not allow it. OsthessenWetter pages, for example, must not be reproduced.

Replace such values with placeholders such as `<broker host>` before you paste configuration or logs. Library log messages contain no measurement values, credentials or query strings, but your application's logs may.

## Security problems

There is no confidential reporting channel yet: GitHub's private vulnerability reporting was not available for this private repository when checked on 2026-11-02, and no other channel has been set up. Do not describe a vulnerability in an issue. If you found one, open an issue with the title "Security: request for a private contact" and write nothing else about the problem; share the details only after a private contact has been agreed in that issue.

## Report a bug

A report that lets someone reproduce the problem contains:

| What | Example |
| --- | --- |
| Package version and where it came from | `WeatherParser.Parsing` 0.1.0, release build `SHA256SUMS` checked; or a source build of commit `<id>` |
| Your runtime and platform | .NET runtime 10.0.14, Debian 13 ARM64 |
| Affected part | `IDwdObservationParser`; or the CLI command `--ParseMOSMIX --Product MOSMIX_S` |
| Library only, CLI only, or both | |
| Minimal reproduction | The registration (without secrets) and the call, or the CLI command line with placeholders |
| Expected and actual behavior | |
| Stable error ID or exception type | `CATALOG-TIMEOUT`, `MosmixForecastException` |
| Relevant log lines, sanitized | The event names and messages around the failure |
| How often it happens | Every call; once in 50 hourly runs |
| Source input | A small fixture you may share, or the public URL and the UTC time of retrieval when the content may not be shared |

Source details that help:

- **Observations**: provider and station ID, with its leading zeros (`00044`), the dataset's `measured_at` and `retrieved_at`.
- **Forecasts**: product (`MOSMIX_L` or `MOSMIX_S`), MOSMIX ID, `issued_at` and the affected `valid_at` steps.

You do not need a coverage report, a fix or a pull request. If you are unsure whether the cause is the library, the source or your setup, report it anyway and say so.

A source that is unreachable, returns errors or has changed its format is reported like a bug, with the error ID, for example `OSTHESSEN-FORMAT-CHANGED`, and the UTC time. The library does not work around outages or access restrictions.

## Request a new data source

Describe what [EXTENDING_DATA_SOURCES.md](EXTENDING_DATA_SOURCES.md#3-record-the-source-contract-first) needs as far as you know it:

- the provider and links to its official format documentation;
- observations or forecasts, and the quantities you need;
- the licence, attribution and access conditions, and any rate limits;
- whether a small sample file may be shared, with its source and licence.

A request is not an approval to implement the source. The maintainer decides whether and when a source is added.
