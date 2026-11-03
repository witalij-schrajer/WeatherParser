# Library-user documentation review and improvement plan

Review date: 2026-11-02. Reviewed branch: `features/develop/docs-development-layout`, starting at `d6a31996149df799dd92f974879c7e62ffef0ca9`.

Status: documentation analysis and proposed acceptance criteria; not implementation of the proposed README or guides. Implementation update: the plan was implemented on 2026-11-02 on the same branch (pull request #21); see section 10. The review was written by the orchestrating agent from repository evidence. No production-code, dependency or release-policy change is proposed or authorized here.

## 1. User goals and conclusion

The documentation must let a library user answer three questions:

1. What does WeatherParser provide, and how do I integrate it into my application?
2. How can I add another data source without breaking the library's contracts?
3. Where and how should I report a problem?

The existing documentation supplies substantial source-specific technical detail, but does not yet provide one complete library-consumer path. The README prioritizes the example CLI, consumer configuration is assembled across multiple sections, and a dedicated extension guide and issue-reporting procedure are missing. This is primarily an organization and onboarding problem, not missing implementation evidence.

The newer branch already improves project status, links CHANGELOG.md and docs/OPERATIONS.md, and documents local-package consumption while public publication is deferred. Preserve these improvements; the outdated status findings from the earlier `78690d0` checkout do not apply to the revised project-status section.

## 2. Findings

| ID | Priority | Evidence | Library-user impact | Recommended correction |
| --- | --- | --- | --- | --- |
| D1 | High | README opens with development-status navigation, repeats the package/CLI distinction, and presents the complete CLI reference before library registration | A developer has to navigate operator and contributor material before reaching application integration | Put purpose, capabilities, current availability and a library quickstart first; put the CLI behind a clearly identified secondary path |
| D2 | High | README consumer-registration sections for selection, DWD, OsthessenWetter, MOSMIX and MQTT are cumulative | The first registration example is not a complete standalone container setup; users can miss dependencies for sources they did not intend to use | Provide one complete setup and explicitly explain AddWeatherParsing's whole-library singleton registration and the consumer-supplied dependencies |
| D3 | High | Release/installation section states 0.1.0 is unpublished and points to contributor build procedures, but no consolidated new-application walkthrough follows | It is unclear how to go from an authorized local nupkg to a working application; a plain nuget.org install is not currently a valid starting point | Document local-feed installation, application dependencies, complete Program.cs, invocation, cancellation, output and expected outcomes; distinguish source-build preparation from package consumption |
| D4 | High | No canonical guide for adding a data source; requirements are scattered through AGENTS.md, REQUIREMENTS.md and phase/source contracts | A contributor cannot identify the required adapter boundaries, mapping decisions, tests and compatibility checks without reconstructing project history | Add docs/EXTENDING_DATA_SOURCES.md with the requirements and sequence in section 5 |
| D5 | High | No issue-reporting section in README/CONTRIBUTING and no .github/ISSUE_TEMPLATE files or SUPPORT.md | Users lack a discoverable reporting route and a reproducible-report checklist | Add a short README route, a detailed issue guide and bug/new-source templates; verify the repository's issue feature before promising its availability |
| D6 | Medium | Library registration samples omit application scaffolding and are separated from parsing/serialization contracts | Knowing the names of interfaces is not enough to produce the first useful result | Include imports, package versions, complete registration, typed resolution at composition root, parser invocation and JSON output in one example; use constructor injection in application services |
| D7 | Medium | README contains contributor language rules, phase references, source-specific error tables and a Windows developer quickstart | Product guidance competes with implementation history; library consumers may assume PowerShell and the entire repository are runtime requirements | Keep essential user contracts in README; link detailed usage/reference, CONTRIBUTING, operations and development history separately |
| D8 | Medium | README.md is the packed PackageReadmeFile; existing CLI documentation tests inspect README sections | Reorganizing documentation can break package readme navigation or existing tests even without changing product behavior | Verify the packed readme and rendered repository/package links; preserve or deliberately update documentation tests when moving sections |

Priorities describe user impact; they do not assert a runtime defect in the library.

### Concrete DI finding

`AddWeatherParsing` registers all library services. With `ValidateOnBuild`, the selection-registration snippet alone lacks at least DwdObservationOptions, OsthessenWetterOptions, MosmixLForecastEndpoints, MosmixLForecastOptions, MosmixSForecastEndpoints, MosmixSForecastOptions and MqttTopicOptions. The README later adds these through cumulative sections; that design needs an explicit complete example rather than treating each fragment as standalone. Endpoint registrations such as OsthessenWetterEndpoints are also supplied later and must appear in the consolidated configuration.

An isolated consumer check against the earlier local `0.1.0-initial` package reproduced those seven missing dependency types. That check predates switching to this branch and is not claimed as validation of the current 0.1.0 package. Inspection confirms ServiceCollectionExtensions is unchanged between the earlier checkout and this branch. A new current-version consumer check is part of implementation acceptance below.

## 3. Proposed README user journey

Recommended order:

1. **What WeatherParser does.** One concise paragraph: .NET library for selecting stations, retrieving observations/forecasts, normalizing values, exporting JSON/XML and creating MQTT messages.
2. **Capabilities.** A table mapping DWD observations, OsthessenWetter observations and MOSMIX L/S forecasts to their primary interfaces and result types. State that the CLI is optional and MQTT serialization does not connect to a broker.
3. **Availability and prerequisites.** Version 0.1.0, net10.0, public publication deferred, authorized access to the local/release package, and a link to the verified platform matrix. Do not imply that ARM64-only CLI deployment policy prevents consumption in a supported .NET application on a verified other platform.
4. **Use the library.** A short installation command that explicitly names the local feed and version, followed by a minimal integration excerpt and a link to the complete runnable sample. Explain that endpoints are supplied by the application and placeholders do not retrieve real data.
5. **Important result semantics.** Missing versus zero, UTC wire timestamps without suffix, observations versus forecasts, pressure reference, estimated dates, stale runs, cancellations and source-specific partial failures.
6. **Add another source.** Link the extension guide and summarize the required interface, constructor injection, normalized output and tests. Clarify that extension is a code contribution/application adapter, not an existing automatic plugin loader.
7. **Report a problem.** Link the confirmed reporting route and issue guide. Request a minimal reproduction and prohibit credentials/private data in reports.
8. **Optional CLI.** Explain its purpose and link commands/operations. If moving the current CLI reference out of README, update its existing tests in the same authorized change rather than silently invalidating them.
9. **Documentation map, license and data rights.** Link library usage, source reference, extension guide, support, CONTRIBUTING, operations, compatibility, changelog and development history. Distinguish MIT code licensing from rights to weather data.

The opening screen should make the package purpose, current installation route and next action visible without requiring knowledge of P1–P9. Avoid release-history paragraphs in the quickstart.

## 4. Complete library-usage guide requirements

Proposed file: `docs/LIBRARY_USAGE.md`.

The first walkthrough should choose one source and one output, preferably DWD observations to JSON, and provide:

- A checksummed local/release nupkg route and a pinned `dotnet add package ... --source <local-feed>` example. Show where the feed comes from and reference the existing supported source-build procedure. Do not imply that a nuget.org publication exists or invent another public package feed.
- A new net10.0 console application, the exact application-level DI/logging package dependencies and all necessary namespaces. The consuming application does not need MSTest, coverage tooling, PowerShell or the source checkout if it already has the package and its dependencies.
- One full Program.cs with every registration required for container validation, explicit endpoint/options configuration, HTTP ownership and TimeProvider/logging. Describe singleton lifetimes and the need for thread-safe injected services.
- A host-owned HttpClient, supported timeout/handler behavior, existing writable evidence-cache directory and relevant resource/deadline limits. Validation-only setup must make no source or broker request. Example paths must work on the stated platform.
- A constructor-injected application class that receives IDwdObservationParser and IJsonExportSerializer; IServiceProvider resolution belongs only to the composition root. Run one ParseAsync call with a station ID kept as a string and pass a CancellationToken through parsing and serialization.
- Clear treatment of example.invalid endpoints: replace them using a documented configuration table. Either offer an offline fixture-backed first run or clearly mark live retrieval as requiring real endpoints and appropriate source permissions. A sample that compiles but cannot retrieve from its placeholder host is not a verified successful live example.
- A small representative JSON result whose omission, timestamps and units match the actual serializers; document the sample's provenance without presenting invented measurements as live evidence.
- Source-specific additions for station selection, OsthessenWetter matching/results, MOSMIX freshness/omissions and MQTT message creation. Do not imply every source has the same signature or error/partial-failure model.
- Failure handling: cancellation versus retrieval/format/no-data errors, stable IDs where supplied, and retry decisions owned by the application. Document that source conditions must permit automated access.

The README may show an excerpt, but must identify it as an excerpt and link directly to this complete example. Avoid several mutually dependent snippets with no final assembled version.

## 5. New-data-source extension guide requirements

Proposed file: `docs/EXTENDING_DATA_SOURCES.md`. The guide should explicitly distinguish a new provider using existing normalized types from adding a new quantity or export schema feature. The latter needs a separate contract/versioning review.

### Required source contract before implementation

Record:

- Provider identity, observation versus forecast scope, station/location identity and an unambiguous source identifier.
- Authoritative endpoint/format documentation, retrieval permissions, attribution, redistribution conditions, supported request frequency and rate limits (REQ-092).
- Required URLs/options/authentication mechanism, which application owns them, and how diagnostics avoid leaking credentials or sensitive query data.
- Schema/format version, encoding, archive structure where applicable, sample provenance and appropriately licensed/redacted fixtures.
- Source units, missing markers, raw quality flags, timestamp timezone/precision, issue/retrieval/valid times and interval meanings. State explicitly where information is absent; never fabricate metadata.
- Normalized mapping, omission/failure behavior, source-specific stable diagnostic IDs and partial-failure semantics.

### Required architecture

1. Define a purpose-specific async parser interface. Show IDwdObservationParser and IOsthessenWetterParser as real examples of different result models; do not invent an existing universal IWeatherSource or plugin API.
2. Separate retrieval, source-format reading, normalized mapping and orchestration where responsibilities justify it. Reuse existing applicable value/normalization/build/serialization services instead of copying their rules. Do not require an arbitrary number of classes solely to match a diagram.
3. Constructor-inject HTTP, time, filesystem and other services through their supported boundaries (REQ-070/071/078). Keep application configuration/environment reading and broker transport outside the library adapter.
4. Map to ObservationDataset or ForecastRun through existing builders where applicable. Preserve UTC, source identity, missing-versus-zero, rounding and interval semantics. Do not rename/overload an existing export field to represent a different physical quantity.
5. Define bounded download, parsing and archive behavior, cancellation, deadline precedence, error reporting and concurrency behavior. Preserve XML entity restrictions/size limits for XML/archives; apply equivalent resource bounds appropriate to other formats.
6. Register contributed built-in adapters centrally in AddWeatherParsing. Document the added dependency requirements for all consumers using whole-container validation. Application-owned external adapters are registered by the host and are not automatically discovered. Do not promise partial registration or plugin discovery that the library does not implement.
7. Keep source-independent JSON/XML/MQTT serialization and the separate MOSMIX L/S contracts. Adding a source does not automatically require a new CLI command or broker client; treat those as explicit separate scope.

### Required evidence before acceptance

- Deterministic offline behavior tests with fake HTTP/time/filesystem, actual parser/mapper and real serializers for contract tests. Use the repository's MSTest/Moq/FluentAssertions conventions and test-first workflow.
- Normal data, missing fields/markers, malformed data, wrong units/timezones, bounds, overflow where relevant, cancellation, timeouts, HTTP errors and partial failures.
- Mapping assertions covering normalized units, precision, timestamp provenance, missing versus zero and stable diagnostic behavior.
- JSON/XML equivalence and generated-schema validation; MQTT contract checks if the new source uses MQTT output.
- Package-consumer/DI validation and regression tests for existing sources. A source addition must not make the documented existing quickstart fail because a new options registration was omitted.
- Current native block-coverage gates, compatibility evidence and required platform CI; live checks remain separate and explicitly authorized. Do not treat mocked tests as proof of real data-source access.
- Source overview, options/dependency table, example usage, errors/limits, attribution/conditions and changelog updates. Record new/changed requirements and approved contracts without silently relaxing existing schema stability.

An example provider walkthrough should reuse real existing adapter patterns, clearly label hypothetical names and avoid claiming a new provider has been implemented or approved.

## 6. Issue-reporting requirements

Proposed files: `docs/REPORTING_ISSUES.md`, `.github/ISSUE_TEMPLATE/bug_report.yml` and `.github/ISSUE_TEMPLATE/data_source_request.yml`. A short root SUPPORT.md can serve as a discoverable pointer if desired; it should not duplicate the detailed instructions.

Proposed normal route: this repository's GitHub Issues at `https://github.com/witalij-schrajer/WeatherParser/issues`. The repository is private under RL-19, so users need repository access and the issue feature must be enabled. The review did not query GitHub settings or verify an external reporting channel. Before publishing the instructions, verify that route with an authorized account. If intended users lack access, the maintainer must designate a usable contact/support route; do not invent an email address, Discussions forum or public tracker.

Bug reports should contain:

- Package version and source, application/runtime and OS/architecture; commit if using a source build.
- Affected source/product and parser interface; whether the problem is library-only, CLI-only or both.
- Minimal reproducer with dependency registrations and non-secret options; expected and actual behavior.
- Exact stable error ID/exception type, sanitized relevant logs and reproduction frequency.
- A small legally shareable source fixture, or a public URL and retrieval timestamp when sharing the content is not permitted. Redact sensitive URL parameters, identities and payloads where necessary.
- For forecasts, product, location ID, issue time and valid times; for observations, source/station ID and measurement/retrieval times where useful. Preserve leading zeros.

Document checking existing issues and known limitations first; ordinary bugs should not require a full production dataset, coverage report, proposed fix or pull request. Distinguish reproducible parser defects, new-source requests and source outages/permission problems without refusing an uncertain report.

New-source requests should describe provider/format documentation, required quantities, observation/forecast scope, legal/access conditions and redacted/licensed sample availability. They do not constitute implementation approval.

Tell users not to post tokens, passwords, broker credentials, private URLs, private operational data or unlicensed website dumps. Security vulnerabilities need a confirmed confidential reporting route. GitHub private vulnerability reporting is an option only if enabled; this review does not assert that it is. Do not direct users to publish exploit details or secrets in a normal issue, or promise a response SLA that the maintainer has not agreed to.

## 7. Implementation sequence and acceptance

1. Establish one complete current-version library consumer example and verify registration, offline parsing and JSON serialization from the local package. Preserve the current no-publication/private-repository status.
2. Rework the README opening and navigation around library use, retaining a clear optional CLI route and links to detailed reference material. Move rather than delete useful technical detail where necessary.
3. Write the extension guide from section 5, referencing existing contracts/types and distinguishing requirements from hypothetical examples.
4. Add the issue-reporting guide and templates from section 6; confirm tracker accessibility and any confidential route before stating they are available.
5. Add links from README/CONTRIBUTING, verify the documentation layout, and perform the required independent documentation review. This task does not start a release or approve publication.

Acceptance checks:

- A developer can identify capabilities, current availability and a working package-consumption path from the README without reading phase plans.
- The complete library example compiles against the current packaged version, passes ValidateOnBuild/ValidateScopes and performs an offline parse/serialization with asserted output. Label live examples with their actual verification status.
- All registration requirements are consolidated, not inferred from scattered sections. Document the external-adapter route and whole-library registration limitation honestly.
- A new-source contributor has an explicit source-contract, implementation, testing, legal and documentation checklist, with no imaginary plugin API.
- A user can find the reporting route from README and provide an actionable, sanitized report; intended users can access the route.
- Verify relative links and anchors from both repository and packed NuGet-readme contexts; use an actual repository URL with appropriate version/ref when the package renderer cannot resolve a relative link. Do not assume this private repository's guides are public.
- Run existing ReadmeCliSectionTests if README is edited. If sections move, update their documentation tests in the same authorized implementation following AGENTS.md. Check the packed README content; README edits are build inputs under the repository's CI rules.
- No production behavior, frozen schema, source permissions, publication state or phase acceptance is changed by editorial cleanup.

## 8. Scope of this review

Inspected README.md, CONTRIBUTING.md, CHANGELOG.md, docs/OPERATIONS.md, source/DI/serializer interfaces, package metadata, existing README tests, P9 release documentation plans and the RL-19 publication/access policy. No new live-source request, issue creation, external message, commit or push was performed. The issue-feature/private-reporting settings were not inspected. Earlier local-package snippet validation is explicitly identified as earlier-version evidence above, not a current-version smoke test.


## 9. Review and validation record

The orchestrating agent checked the findings against the selected branch, its source interfaces/DI and release-policy records. A separate read-only Codex CLI review was attempted under AGENTS.md; the proxy rejected connections to the review service with HTTP 403, so no independent verdict was obtained. The retrying review process was stopped. This is a review-availability limitation, not an approval or a successful independent review. AGENTS.md explicitly permits continuing when that reviewer is unavailable. No network-policy change was made.

The new document was checked for whitespace and required-topic coverage. No application test was needed for this analysis-only Markdown addition. The README and the guides/templates proposed above have not been rewritten or created by this review.

## 10. Implementation record (2026-11-02)

Implemented on the user's instruction ("Examine it and execute the plan") on branch `features/develop/docs-development-layout` (pull request #21, draft), after commit `d6a3199`.

| Step of section 7 | Result |
| --- | --- |
| 1. Complete current-version consumer example | [docs/LIBRARY_USAGE.md](LIBRARY_USAGE.md) with the registration of every required type (`tests/WeatherParser.Package.Tests/LibraryUsage/WeatherParserRegistration.cs`), a constructor-injected application class (`IDwdJsonExporter.cs`, `DwdJsonExporter.cs`), `Program.cs`, a sample output, errors, lifetimes and the other sources. `LibraryUsageExampleTests` (four tests) compiles the example against the packed library, validates the container (`ValidateOnBuild`, `ValidateScopes`), resolves every entry point without a request, runs the export offline against a fake handler for the DWD URIs, and checks that the guide shows the three files verbatim and that its JSON equals the serializer output. |
| 2. README around library use | Capabilities, project status, getting started, result semantics, extension and reporting summaries and a library reference index come first; the example CLI follows the library reference; the contributor sections and a documentation map come last. Sections were moved, not deleted; headings are unchanged, so in-page anchors and `ReadmeCliSectionTests` keep working. Links to other files outside the "Example CLI" section are absolute links to the `develop` branch, so they also work from the packed readme for readers with repository access. |
| 3. Extension guide | [docs/EXTENDING_DATA_SOURCES.md](EXTENDING_DATA_SOURCES.md). Finding while writing it: `IForecastRunBuilder` forms `source` only for `MOSMIX_L` and `MOSMIX_S` and `ForecastRun.Product` is `MosmixProduct`, so a forecast provider other than MOSMIX needs a contract change; the guide says so. |
| 4. Issue guide and templates | [docs/REPORTING_ISSUES.md](REPORTING_ISSUES.md), `.github/ISSUE_TEMPLATE/bug_report.yml`, `.github/ISSUE_TEMPLATE/data_source_request.yml`. Checked on 2026-11-02 with `gh`: issues are enabled, the repository is private, discussions are disabled, and the private vulnerability reporting endpoint returned HTTP 404; the guide therefore states that no confidential channel exists yet. No SUPPORT.md was added. |
| 5. Links and review | CONTRIBUTING.md and CHANGELOG.md link the new guides. All relative links and anchors of the Markdown files were checked by script, and the absolute README links were mapped back to this branch and checked. Codex reviews below. |

Authorship: qwen3-coder:30b (Ollama `/api/chat`, AGENTS.md parameters) generated `LibraryUsageExampleTests.cs`, the three files under `LibraryUsage/` and the `Program.cs` shown in the guide, with three correction rounds (missing `using MyWeatherApp;`, assertions placed after the `Substring` they guard, implicit usings and comment placement, two missing comments in `Program.cs`). The orchestrating LLM wrote the guides, the README restructuring (by a script that moves whole sections), the CHANGELOG and CONTRIBUTING lines and the issue forms (documentation forms, not code or test data), and reviewed every generated file.

Evidence (Windows, SDK 10.0.402):

- Red: before the example files existed the package test project failed to compile (`CS0246` for `IDwdJsonExporter` and `DwdJsonExporter`, `CS1061` for `AddMyWeatherParsing`); with the example and without the guide, the two example tests passed and the two documentation tests failed. Green: 4 of 4. A mutation of the guide (one option value, one JSON value) made both documentation tests fail.
- Package tests 141 of 141 (137 before), `ReadmeCliSectionTests` 6 of 6, package check of the changed README (packed README, LICENSE and schema byte-identical, restored package hash equal).
- A new console application outside the repository, following the commands of the guide with an empty package cache, restored the local `0.1.0` package and its dependencies from nuget.org and built `Program.cs` with the example files without warnings. It was not run, because running it contacts the DWD server; no live request was made.
- The checks above ran on the working tree that was committed unchanged as `ac37bd8`. The P1.09 script then ran on the committed revision: `scripts/ci-local.ps1` against `origin/develop` `69604b9`, commit `ac37bd8`, two-pass mode, 2026-11-02T21:36:15Z to 21:39:18Z, exit code 0, run directory `artifacts/ci/ac37bd8-20261102T213615Z` (local, not committed). Tests: Parsing 3213 passed of 3220 (0 failed, the rest skipped), Coverage 170 of 170, Coverage.Runner 72 of 72, SchemaGeneration 46 of 46, Package 141 of 141, CLI 719 passed of 733 (0 failed, 14 skipped). Library gate (`gate-result.json`) 11102 of 11103 blocks and CLI gate (`cli-gate-result.json`) 2875 of 2920 blocks, each equal to its baseline and accepted because the gated inputs are unchanged. The packed package's hash (`package-hash.txt`) is `hvLyYQQlmzenvI40FYnCDIQdG0JcQQFEzSJPS7ueLG8lwcLVXm3Eis7YFG8FDwhAn20HUypJPPD2BFrnZPRgNw==`.
- Hosted CI on `ac37bd8`: run 41529831257 succeeded (Windows x64, Ubuntu x64, Debian 13 ARM64).

Codex reviews (read-only, five-angle audit frame), local CLI runs on the uncommitted changes that became `ac37bd8`, on top of `d6a3199`. The outputs were written outside the repository and are not committed; the table records each round's finding count and disposition:

| Round | Findings | Disposition |
| --- | --- | --- |
| 1 | 5 | All confirmed and fixed: relative guide links unusable from the packed readme (a note was added); serializer support overstated for the parser result wrappers (the five export roots are now named); `--source` restricted the restore to the local folder (removed; verified with an empty cache); `ErrorId` claimed for every exception (`ObservationDatasetRejectedException` and cancellation named separately); the security route promised a maintainer action (now states that no confidential channel exists) |
| 2 | 1 | Confirmed and fixed: the packed readme still linked other files relatively; they are now absolute `develop` links, except in the "Example CLI" section, whose links stay relative because `ReadmeCliSectionTests` allows only source hosts there and the CLI is built from a checkout |
| 3 | 1 | Confirmed and fixed: the extension guide said every serializer accepts every export root; the MQTT serializer rejects `ExportDocument` (`MQTT-MESSAGE-TYPE-UNSUPPORTED`), which the guide now states |
| 4 | 0 | No findings |

Automatic pull request review (`.github/workflows/review.yml`) of pull request #21:

| Round | Head | Run | Findings | Disposition |
| --- | --- | --- | --- | --- |
| 1 | `ac37bd8` | 41590374029 | 2 | Both confirmed and fixed: this record did not name the revisions, runs and hashes of its evidence and lacked the P1.09 run on the committed revision (added above); the change of `.github/review/prompt-documentation.md` had no companion note in CONTRIBUTING.md section 6, `DEPENDENCIES.md` and AGENTS.md "Second reviewer: Codex" as the consistency table requires (added: the brief names the status documents at their `docs/development/` paths; triggers, classification, pinned actions and the Codex version are unchanged) |
| 2 | `9ec34a9` | 41590759751 | 0 | No findings; the commit recording this round is reviewed once more, and that round is recorded in its pull request comment only |

Open items:

- The absolute README links point to the `develop` branch. A release (P9.12) should pin them to its tag, so that the packed readme of a version links the documentation of that version.
- The issue route needs repository access while the repository is private; no confidential security channel exists.
- The anchor `AGENTS.md#ai-assisted-development-user-decision-dated-2026-10-19` in `docs/development/PHASE_P2_PLAN.md` has been broken since the heading was amended on 2026-10-24; the historical plan was not changed.

## 11. Pull request review fixes (2026-11-03)

The user's review of pull request #21 on head `74ee824` ("[P2] Finish navigation from the packed README") found that seven links of the README section "Example CLI" were still relative, so they cannot resolve from the packed readme, which leaves the acceptance check of section 7 on both link contexts incomplete. Confirmed: the section linked `docs/p3/P3_CLI_CONTRACT.md` (twice), `docs/p3/P3_VERIFICATION.md`, `docs/p9/P9_09_BROKER_AND_SHUTDOWN_CHECKS.md`, `docs/p3/P3_11_INTEGRATED_VERIFICATION.md`, `CONTRIBUTING.md#example-cli-examplesweatherparsercli-p3` and `docs/OPERATIONS.md` relatively. This supersedes the exception for the "Example CLI" section in section 10 (step 2 and review round 2).

Fix:

- README: the seven links are absolute links to the `develop` branch, like every other file link of the README; the intro no longer says that the section's links are relative.
- `tests/WeatherParser.Cli.Tests/ReadmeCliSectionTests.cs`, `Readme_NoPrivatePathsOrAddresses` (generated by qwen3-coder:30b in three rounds: the change, then ordinal comparison, the original host extraction and braces, then the comment wording): a URL of the section skips the host check only when it starts with `https://github.com/witalij-schrajer/WeatherParser/blob/develop/` or `.../tree/develop/`; every other URL keeps the check against the source hosts, and the private address and path checks are unchanged.

Coverage decision: the test file is a gated input of the CLI gate, so the gate requires a strict improvement of the CLI block coverage, which a test-only change of the README check cannot give. Asked how to resolve this, the user chose on 2026-11-03 "Test change + reconciliation": change the test as suggested and approve a recorded baseline reconciliation for the CLI gate of pull request #21. The entry in `coverage-reconciliation.json` names the gate `cli`, target commit `69604b934e1b40f1e22ce83b308aa561e2636bbf`, pull request 21, head repository `witalij-schrajer/WeatherParser`, head branch `features/develop/docs-development-layout`, approval date 2026-11-03 and this section as the record. It accepts CLI coverage at or above the baseline; a decrease still fails. The entry was written by the orchestrating LLM, because it records a user decision, and is removed after the pull request is merged. The library gate needs no entry: no library input changed.

Evidence (Windows, SDK 10.0.402):

- Red: with the absolute links and the unchanged test, `Readme_NoPrivatePathsOrAddresses` failed with "URL host 'github.com' is not allowed".
- Green: `ReadmeCliSectionTests` 6 of 6. Mutations of the section, each reverted: links to `https://github.com/someone-else/WeatherParser/blob/develop/`, to the branch `main` and with `http://` each made the test fail; a link to `.../blob/develop/docs/OPERATIONS.md` passed.
- P1.09 script on the committed revision `7b9e496`: `scripts/ci-local.ps1` against `origin/develop` `69604b9` with `-PullRequestHeadRepository witalij-schrajer/WeatherParser -PullRequestBranch features/develop/docs-development-layout`, two-pass mode, 2026-11-03T07:27:56Z to 07:31:11Z, exit code 0, run directory `artifacts/ci/7b9e496-20261103T072756Z` (local, not committed). Tests: Parsing 3213 passed of 3220 (0 failed), Coverage 170 of 170, Coverage.Runner 72 of 72, SchemaGeneration 46 of 46, Package 141 of 141, CLI 719 passed of 733 (0 failed, 14 skipped). Library gate 11102 of 11103 blocks, equal to the baseline with unchanged gated inputs; CLI gate 2875 of 2920 blocks, equal to the baseline, accepted by the rule `Reconciliation` naming the entry above. Package hash `+Ko2MDqryHoBvlwzseW+BD1Wntt441iLH1UF9lkQw5+hGEkQHHJCC7gHvZuPt5Q+jYz+YkE6BozJDIttg2accg==`; the packed `README.md` is byte-identical to the committed file (SHA-256 `86F59909F5279DB171C9015F9B52A7F1755CD2E08AAD9DFF04E8DC497D7BA883`).
- Links: all relative links and anchors of the Markdown files checked by script; no new problem (only the historical P2 plan anchor of section 10).
