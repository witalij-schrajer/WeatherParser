# WeatherParser rules

**Phase status (2026-10-28): P0 and P1 complete; P1 accepted by the user on 2026-10-19 ([acceptance record](docs/p1/P1_12_ACCEPTANCE.md#p1-acceptance-2026-10-19)). P2 (export entities, serialization, output schemas) accepted by the user on 2026-10-20 ([P2 acceptance record](docs/p2/P2_12_ACCEPTANCE.md)); it is complete: pull request #3 merged into `develop` on 2026-10-20 (merge commit `74bf625`). P4 started on 2026-10-20 on the user's instruction ([P4 handoff](docs/p2/P4_HANDOFF.md)). P4 (station and location selection) accepted by the user on 2026-10-24 on candidate `b6bc47f` ([P4 acceptance record](docs/p4/P4_12_ACCEPTANCE.md)); it is complete: pull request #5 merged into `develop` on 2026-10-24 (merge commit `95bcb1e`); P5 (DWD observations) started on 2026-10-24 on the user's instruction ([P5 handoff](docs/p4/P5_HANDOFF.md), [P5 plan](PHASE_P5_PLAN.md)); P5 accepted by the user on 2026-10-25 on candidate `fd461c8` ([P5 acceptance record](docs/p5/P5_11_ACCEPTANCE.md), [P6 handoff](docs/p5/P6_HANDOFF.md)); it is complete: pull request #7 merged into `develop` on 2026-10-25 (merge commit `29fd553`); P6 starts only on a separate instruction; P6 (OsthessenWetter observations) started on 2026-10-25 on the user's instruction on branch `features/develop/P6` from `develop` `8d8079a` ([P6 handoff](docs/p5/P6_HANDOFF.md), [P6 plan](PHASE_P6_PLAN.md)); P6.01 (foundation check) complete with outcome READY ([record](docs/p6/P6_01_FOUNDATION_CHECK.md)); P6.02: OsthessenWetter contract OW-01 to OW-16 approved by the user on 2026-10-25 ([contract](docs/p6/P6_OSTHESSEN_CONTRACT.md)) and the P6 plans reconciled with it; P6.03 to P6.09 implemented test-first on 2026-10-25 and 2026-10-26 ([P6 verification record](docs/p6/P6_VERIFICATION.md)): the OsthessenWetter parser is available in the library; P6.10 (integrated verification) READY on candidate `4373399` on Windows, hosted CI and the NanoPi R76S, with one authorized live check, and after the PR #9 review fixes again on candidate `12e7205` ([P6.10 record](docs/p6/P6_10_INTEGRATED_VERIFICATION.md)); P6.11: evidence audit ACCEPTED ([P6.11 acceptance record](docs/p6/P6_11_ACCEPTANCE.md), [P7 handoff](docs/p6/P7_HANDOFF.md)), P6 accepted by the user on 2026-10-26 on candidate `12e7205` (the library; production use and redistribution of OsthessenWetter values stay open until the source conditions are clarified, OW-14); it is complete: pull request #9 merged into `develop` on 2026-10-26 (merge commit `de9d4ba`); P7 (MOSMIX forecasts) started on 2026-10-26 on the user's instruction on branch `features/develop/P7` from `develop` `8009f1c` ([P7 handoff](docs/p6/P7_HANDOFF.md), [P7 plan](PHASE_P7_PLAN.md)); P7.01 (foundation check) complete with outcome READY ([record](docs/p7/P7_01_FOUNDATION_CHECK.md)); P7.02: MOSMIX forecast contract MF-01 to MF-18 approved by the user on 2026-10-26 ([contract](docs/p7/P7_FORECAST_CONTRACT.md)) and the P7 plans reconciled with it; P7.03 to P7.10 implemented on 2026-10-26 to 2026-10-28 ([P7 verification record](docs/p7/P7_VERIFICATION.md): P7.03 to P7.08 test-first, with the P7.06 deviation of planned tests added after Green, recorded and covered by the phase acceptance, P7.09 and P7.10 as characterization tests over existing behavior): the MOSMIX_L and MOSMIX_S forecast parsers are available in the library; P7.11 (integrated verification) READY on candidate `a36d15a` on Windows, hosted CI and the NanoPi R76S, with one authorized live parse per product ([P7.11 record](docs/p7/P7_11_INTEGRATED_VERIFICATION.md)); after the PR #11 review fixes the candidate is `2f1115b`, with P7.11 repeated and READY on Windows, hosted CI and the NanoPi R76S (authorized rerun); P7.12: evidence audit ACCEPTED and P7 accepted by the user on 2026-10-28 on candidate `2f1115b` ([acceptance record](docs/p7/P7_12_ACCEPTANCE.md), [P8 handoff](docs/p7/P8_HANDOFF.md)); it is complete: pull request #11 merged into `develop` on 2026-10-28 (merge commit `20b2ef0`); P8 (MQTT serializer and complete library integration) started on 2026-10-28 on the user's instruction on branch `features/develop/P8` from `develop` `8ed51c7` ([P8 plan](PHASE_P8_PLAN.md)); P8.01 (foundation check) complete with outcome READY ([record](docs/p8/P8_01_FOUNDATION_CHECK.md)); P8.02 (MQTT message contract) in progress: draft MQ-01 to MQ-15 under review ([contract](docs/p8/P8_MQTT_CONTRACT.md)).**

These rules apply throughout the repository. Current explicit user instructions take precedence. Binding requirements are in [REQUIREMENTS.md](REQUIREMENTS.md); project status and usage are in [README.md](README.md).

## Language rules

- English is the sole maintained language for project documentation, contributor instructions, plans, code comments, and program-generated messages. This user decision dated 2026-10-19 supersedes the former bilingual-maintenance requirement.
- Write all source-code comments in English, including inline/block comments, XML documentation comments, and comments in scripts and configuration examples, as required by REQ-009. Include English-comment compliance in change reviews.
- All program-generated output must be English, including help, logs, status and error messages. Technical identifiers, CLI commands and data fields remain language-independent.
- Keep one canonical English document at the standard unsuffixed path, such as `README.md`, `REQUIREMENTS.md`, `AGENTS.md`, `DEVELOPMENT_PLAN.md` and `PHASE_P1_PLAN.md`. Do not recreate `.en.md` or German counterparts.
- Preserve requirement/test IDs, technical identifiers, CLI options, data fields, units, source names, source URLs and examples. Translate explanatory prose and example placeholders, not programmatic contracts or proper names.
- Before removing a duplicate, reconcile information unique to either language into the canonical English document. Keep English documentation, requirements and actual progress consistent in the same change.
- Preserve the English MIT legal text in `LICENSE` unchanged. Historical inventories/checksums remain evidence of their capture date and must not be rewritten to match renamed documents.

## Current scope and authorization

- The project currently contains a local Git repository, documentation and initial library/test project scaffolding. Generate application code, .NET projects and implementation scaffolding only upon explicit request.
- The primary product is a reusable NuGet library; the CLI is only a package usage example with `--SelectDWD`, `--ParseDWD`, `--ParseOstHessen`, `--SelectMOSMIX`, `--ParseMOSMIX`, MQTT output and JSON/XML export.
- Keep observations and forecasts separate. Current requirements cover observations, MOSMIX forecasts and related station selection; do not add door control, n8n workflows or Home Assistant installation.
- Do not silently turn open decisions into approved requirements. Document CLI/MQTT contract changes.

## Platform and workflow role

- WeatherParser is an independent workflow component responsible for retrieval, parsing, station selection, normalization and output. Storage, display, device control and subsequent workflow actions belong elsewhere.
- Binding platform policy (2026-10-14): .NET version 10 or newer and Debian version 13 Trixie or newer. The initial library/example target framework is `net10.0`; the minimum architecture is ARM64 (`linux-arm64`), particularly NanoPi R76S. Add newer stable .NET/Debian releases to the verified compatibility matrix; previews are not automatically guaranteed. Visual Studio 2026 and SDK 10.0.303 are the verified local starting point; packages, extra ARM variants and deployment model remain undecided. Verify the library, actual package consumer and example on Debian 13/ARM64 with .NET 10. Distinguish platform requirements from verified compatibility; never claim runtime success without documented tests.
- Execute exactly one CLI command per invocation, then exit with an actionable status. No internal scheduler, continuous polling or periodic background timers. Multiple results within one command are allowed; technical retries remain bounded.
- Keep scheduling, retries across invocations and orchestration external, such as n8n or cron. Support noninteractive execution, clean output channels and controlled cancellation. Do not impose n8n as a dependency or install an orchestrator.
- Distinguish successful build/publish from actual Debian/ARM execution. Document target-platform verification, including serializers and single-command behavior, before release. Explicitly identify missing hardware tests.

## Open source and Git

- Code is published under MIT. Preserve `LICENSE`; add third-party code and dependencies only with clarified, suitable licenses.
- Never commit passwords, tokens, private keys, broker credentials, private operational logs/configuration or personal test data to the public repository.
- Use placeholders in example configuration. Add secret files to `.gitignore` before creating them. Never log secrets or store them in test snapshots.
- Document source conditions and attribution separately. MIT for code does not automatically license weather data, websites or images.
- Set up GitHub repositories, remotes, pushes, releases and publication only within explicit authorization. The general open-source objective does not authorize automatic publication.
- Check the working tree before editing; preserve others' changes and avoid destructive resets/cleanup. Use meaningful small commits when requested, and do not claim a commit when only files were created.
- CI only for code changes (user decision dated 2026-10-28): the jobs of `.github/workflows/ci.yml` (Windows x64, Ubuntu x64, Debian 13 ARM64) run only when code changed; a change consisting only of Markdown files and files under `docs/` skips them, except the root `README.md`, which is packed into the package and always counts as code. A push is judged by its own commits; opening, reopening, marking ready for review and force pushes by the whole pull request; manual runs always run (CONTRIBUTING.md section 6). A skipped run builds, tests and measures nothing, so the coverage gate applies only to runs with code changes (and to manual runs) and a documentation-only pull request has no coverage measurement unless it is run manually; the gate's own documentation-only classification (all files `.md`) is unchanged. This supersedes "CI on every push" from the home-automation pull request default for this repository.
- Phase pull requests (user decision dated 2026-10-20): each phase is implemented on its own feature branch created from `develop`. Directly after the first commit on the new branch has been pushed, open a draft pull request into `develop` that tracks the phase implementation; push every further commit of the phase to it, so CI checks every push that changes code. Mark it ready for review only at the end of the phase and before each user approval ("Phase implementation" below); the user merges it.

## AI-assisted development (user decision dated 2026-10-19; scope amended 2026-10-24)

- Where applicable, use the local `qwen3-coder:30b` model (through Ollama) as the code generation engine: it generates, changes and fixes code, tests, scripts and fixture or test data in this repository. This includes fixes for defects found in qwen's own output or by reviews: describe the defect to qwen and let it produce the fix.
- Documentation is not generated by qwen (user decision dated 2026-10-24): phase and step plans, contract decision drafts, verification and acceptance records, handoffs, README and status text are written by the orchestrating LLM, reviewed read-only by Codex (see "Second reviewer: Codex") and corrected by the orchestrating LLM until the review reports no findings. Reason: during the P5 planning on 2026-10-24 qwen reproduced the orchestrating LLM's briefs verbatim and most of its edit blocks did not apply, while these documents need the user's decisions, recorded evidence and repository state that qwen cannot have; the former routing through qwen cost time without adding content. Earlier records that name qwen as the drafter of documentation remain accurate for their date.
- The orchestrating LLM (the agent driving the task) plans the work, writes precise briefs for qwen (requirements, affected files, current text to change, expected behaviour and test-first expectations), applies qwen's output, and acts as reviewer and supervisor. It does not silently replace qwen's work with its own.
- The orchestrating LLM must verify every qwen result before it is accepted: review the diff against `AGENTS.md`, `REQUIREMENTS.md` and the active plan, run the relevant builds and tests itself, and review the result of each step. Defects found are described to qwen for correction, and the fix is verified again.
- qwen is not applicable when the model is unavailable, when a change needs information or decisions qwen cannot have (for example user decisions, recorded evidence, run results, commits, pushes and other repository operations), or when repeated precise briefs still fail. In those cases the orchestrating LLM may do the work itself and states the reason in its report.
- Result records name which parts qwen generated (code, tests, scripts, data) and which the orchestrating LLM wrote or corrected; documentation is recorded as written by the orchestrating LLM. Model output is never evidence: only builds, tests and runs executed and checked by the orchestrating LLM count.

## qwen invocation (user decision dated 2026-10-20)

- The orchestrating LLM calls qwen3-coder:30b through the Ollama `/api/chat` endpoint (`stream` false, `keep_alive` -1) with `temperature` 0.2, `top_p` 0.8, `top_k` 20, `repeat_penalty` 1.05, `num_ctx` 24576, `num_predict` 8000 and `seed` 42.
- System message: a fixed coding policy – existing declarations are authoritative and project types, enum members, properties, methods and constructors are never invented; only the authorized files are output; property kinds (init-only, `double?` versus `decimal`) are respected; repository coding conventions; the output format (complete file blocks `=== FILE: <path> === … === END FILE ===`); and the single line `BLOCKED: <what is missing>` when required information is missing.
- User message, in this order: an `API REFERENCE` with the real source of every project type the task uses (copied from the repository, never summarized from memory), the task with an `AUTHORIZED OUTPUT` line naming the file(s), and the current content of each file to change.
- One file per request where possible; small, focused changes.
- Only file blocks whose path is on the authorized allowlist are written; other blocks are rejected and reported. The orchestrating LLM reviews the diff before builds and tests and rejects unrelated or weakened changes (for example changed expected values).
- Corrections are compiler-guided: the request contains the exact compiler errors or failing test output, the relevant declarations and the target file, and changes only that file.
- These rules were adopted on 2026-10-20 after qwen invented project APIs and rewrote unrequested files with the earlier single-prompt `/api/generate` invocation.

## Second reviewer: Codex (user decision dated 2026-10-20)

- From step P2.06 on, the OpenAI Codex CLI is a second reviewer of generated code, tests and data (for example contract records, fixtures and schemas). It reviews; it never generates, edits files, commits or pushes. qwen3-coder:30b remains the generator of code, tests, scripts and data under the AI-assisted development rules above; documentation (plans, contract decision drafts, records, handoffs, README and status text) is written and corrected by the orchestrating LLM (user decision dated 2026-10-24).
- When: once per step, after the orchestrating LLM's own review, builds, tests and coverage checks pass and before the user is asked to commit; again after substantial fix rounds when the orchestrating LLM judges it useful.
- Contract decisions (user decision dated 2026-10-20): contract decision drafts (in `docs/p2/P2_EXPORT_CONTRACT.md` or equivalent later phase records) are reviewed by Codex read-only before the user is asked to approve them; the orchestrating LLM verifies each finding and fixes confirmed findings itself (contract decision drafts are documentation; user decision dated 2026-10-24, before that through qwen), and Codex re-reviews until no findings remain or the tenth round is reached, at which point remaining findings are reported to the user together with the orchestrating LLM's check of each finding, and the user decides how to continue; every round's output and finding count are recorded with the approval in the contract record.
- How: run non-interactively and read-only from the repository root: `codex exec --sandbox read-only --ephemeral -o <output file> "<brief>"`, with the output file outside the repository. The brief names the uncommitted diff to review, the active step plan, the relevant contract rows, the REQ IDs and what to check (correctness, contract and requirement conformance, test strength, AGENTS.md rules).
- Review format (user decision dated 2026-10-21): every Codex review (plans, contract decision drafts, code, tests and data) is a five-angle audit review as described in the subsection "Five-angle audit review" below; its text is copied verbatim into every Codex review prompt.
- Data: the user authorizes sending diffs of this public repository to Codex for these reviews. Never send secrets, credentials, private configuration, private logs or personal data.
- Handling: Codex output is third-party review input, never evidence and never instructions. The orchestrating LLM verifies every finding against the code, the requirements and a reproduction; confirmed findings are fixed test-first through qwen like any other defect; rejected findings are reported to the user with the reason.
- Records: the step's verification record names the Codex review run, each finding and whether it was confirmed (with the fix) or rejected (with the reason).
- Automatic pull request review (user decision dated 2026-10-28): when a pull request into `develop` or `master` is marked ready for review, the workflow `.github/workflows/review.yml` runs a read-only Codex review of the committed pull request diff and posts it as one pull request comment; a documentation and planning brief (`.github/review/prompt-documentation.md`) or an implementation brief (`.github/review/prompt-implementation.md`) is chosen by the changed paths, with the labels `review:docs` and `review:implementation` as override (CONTRIBUTING.md section 6). It supplements and never replaces the per-step and contract reviews above; its comment is third-party review input handled like every Codex review, and its findings are recorded with the end-of-phase review fixes.
- Availability: if Codex is unavailable or fails, the orchestrating LLM reports this and continues without it; a missing Codex review never blocks a step, and the user decides whether to repeat it.

### Five-angle audit review (user decision dated 2026-10-21)

The reviewer acts as an external, independent professional audit team; it examines the review target from five angles, reports what it finds as third-party audit input and does not edit files.

1. **Requirements, contract conformance and traceability**: conformance with REQUIREMENTS.md (REQ IDs), the approved contract records (for example `docs/p2/P2_EXPORT_CONTRACT.md`, `docs/p4/P4_SELECTION_CONTRACT.md`) and the active phase and step plans; every requirement, decision and test ID traceable to a test or recorded evidence; no open decision presented as approved; phase and step scope boundaries respected.
2. **Architecture and API design**: every doing class behind an interface, constructor injection, central registration in `AddWeatherParsing` (C-107, C-108); separation of CLI, configuration, adapters, selection, data contracts, serialization, export and MQTT; no global state or service locator; public API naming, stability and consumer ergonomics; necessity, version and licence of dependencies; testability.
3. **Correctness and source fidelity**: logic and edge cases, boundaries on both sides, numeric precision and overflow, UTC handling, missing values versus genuine zeros, units, identifiers kept as strings; source facts (DWD, OsthessenWetter) verified against official documentation or the actual published files rather than assumed; test strength: meaningful behaviour tests with recorded Red evidence, real serializers in contract tests, no implementation-mirroring tests.
4. **Security, legal and operations**: handling of untrusted input (XML without DTDs or external entities, bounded archive, download and time limits, safe handling of archive member names and any unpacked data), no secrets in code, logs or test data, sanitized diagnostics; licences and attribution of data and dependencies and source conditions (REQ-092); operational behaviour such as request volume, timeouts, cancellation, failure observability, resource use and the Debian 13 ARM64 target.
5. **Process, governance and evidence**: the AGENTS.md workflow (qwen generates code, tests, scripts and data, the orchestrating LLM writes documentation and verifies everything, Codex reviews, the user approves, commits and merges); accurate authorship records; evidence that consists of real executed runs, hashes and CI results, never model output; reproducibility; consistent status and records under the AGENTS.md consistency table; commits and pushes only on the user's request.

Report format: one section per angle in the order 1 to 5, headed "Angle <n> – <name>". In each section list the findings, most severe first, each with severity [P1 blocking / P2 should fix / P3 minor], location (file and section, line or decision ID), problem, concrete failure scenario and suggested fix; write "No findings." for an angle without findings. Then give an overall verdict in one or two sentences. The last line is "Summary: <n> findings", counting the findings of all angles. English only.

## NuGet primary product and example CLI

The intended published product is a reusable NuGet library. It contains DWD/OsthessenWetter/MOSMIX parsers with required retrieval/normalization, public output entities, a generic MQTT serializer and dependency-injection integration. The CLI is only a usage example and consumes a versioned package reference; other applications can use the package without the CLI.

Each parser receives its required source URLs explicitly as constructor parameters, preferably typed as `Uri`. Name multiple endpoints separately. DI integration forwards consumer-supplied endpoints to the corresponding constructors, without invented built-in sources or global URI singletons. HTTP, time and other services remain constructor-injected. The library does not independently read CLI arguments or application-specific configuration files/environment variables.

The MQTT serializer is source-independent: it serializes suitable output entities and creates a message containing topic, JSON payload, QoS and retain setting. Topics/templates and their mapping to message types come from consumer configuration, not hardcoded weather paths. Validate resolved publication topics; missing mappings and unresolved placeholders produce defined failures. Existing `haus/weather/...` topics are configurable examples only. Serialization and broker transport are separate; serialization needs neither a broker connection nor credentials.

Provide documented DI registration for library parsers, serializer and required services. Consumers supply endpoints, topic configuration and external dependencies. Every doing class has an interface; output entities remain dedicated `[Serializable]` types. Preserve testability, UTC and JSON/XML compatibility.

The example CLI owns arguments, application configuration, single-command process execution, exit codes, file/stdout export and optional MQTT transmission. It accesses parsing only through the package, never copied implementations. Acceptance prioritizes the public API, package versioning and independent consumption. Decide package ID, feed and publication procedure before publishing; no built/published package exists yet.

## Architecture

- Each class with executable logic (a “doing class”) must implement a suitable interface. Inject dependencies on these classes through their interfaces. Pure data/export entities without executable logic need no artificial interface.
- Provide all service dependencies through constructor injection. Domain classes must neither construct concrete services nor retrieve them from an `IServiceProvider`.
- Centralize registration/wiring in the composition root. Framework builders, immutable data objects and values are not hidden service dependencies.
- Keep HTTP, MQTT, filesystem and time behind injectable boundaries. Parsers and normalization must be testable with local inputs without network access.
- Separate CLI, configuration, adapters, station selection, data contracts, JSON/XML serialization, export destinations and MQTT. No global service singletons or hidden global state.
- Use asynchronous I/O, CancellationToken and bounded timeouts. Do not hide failures with fabricated values or silent suppression.
- Check .NET/package compatibility, maintenance and licensing against official sources before implementation. Pin reproducible versions and avoid unnecessary frameworks.

## Data contract

- Output every timestamp as UTC in `YYYY-MM-DDTHH:mm:ss` format without `Z` or an offset. Retain UTC internally; never serialize unchecked local system time.
- Resolve DWD station from CLI or configuration. If both are missing: log an error, fail, and perform no HTTP or MQTT. Never substitute the nearest station automatically.
- Support decimal degrees and DMS with hemisphere; validate ranges independently of culture.
- Select MOSMIX locations with the same geographic/altitude filters but its own catalog. Preserve IDs as strings, never equate them to CDC IDs. `ParseMOSMIX` requires `MOSMIXID`.
- Normalize forecasts separately, preserving issue, retrieval, validity times and intervals. Never emit forecasts as observations or with `measured_at`. Use separate topics and a complete retained forecast run.
- Provide separate MOSMIX_S/MOSMIX_L parsers/interfaces with independent constructor URLs and DI registrations; document elements/freshness per product. Read KML/KMZ namespaces and align parameter/time series correctly. Missing values must not shift timestamps. Disable external XML entities/DTDs and bound archive sizes.
- Omit missing/invalid readings; `0` is real. Do not publish DWD missing-value markers as observations.
- Convert pressure units from the same original value and preserve pressure reference. No general conversion between lux and W/m².
- Distinguish rain amount/intensity, gust/mean wind and radiation energy/power. Convert only with a verified reference period.
- Never merge different timestamps misleadingly. Preserve periods and quality metadata according to REQUIREMENTS.
- OsthessenWetter date inference is permitted only with `estimated_date`. Apply `Europe/Berlin` and convert to UTC; test midnight and DST. Do not claim certainty for ambiguous times.
- Define a strongly typed entity for each export object type, explicitly marking all custom export entities, including nested types, `[Serializable]`. Do not replace them with anonymous objects, `dynamic` or untyped export dictionaries.
- Generate JSON with `System.Text.Json.JsonSerializer` and XML with `System.Xml.Serialization.XmlSerializer`, using the same entities for both and MQTT JSON. No manually concatenated documents. Entities must support both serializers and contain no services or secrets.
- Put serializers behind injectable services or centrally wired factories. `[Serializable]` does not replace JSON/XML field, null or timestamp rules.
- Derive JSON and XML from the same normalized model. Preserve UTC, missing values, actual zeros, source and quality metadata; escape XML correctly.
- Do not require or contact MQTT in JSON/XML mode. Produce one complete document per invocation, log to stderr, and avoid silent overwrites or misleading partial files.

### Separate MOSMIX parsers and update frequency

Provide two distinct parsers with separate interfaces: `MosmixSParser` / `IMosmixSParser` for `MOSMIX_S`, and `MosmixLParser` / `IMosmixLParser` for `MOSMIX_L`. Both receive their own source URLs as constructor parameters and have independent DI configuration. Shared KML/KMZ helpers may be reused, but one switchable parser cannot replace the two public contracts. Missing data never triggers silent fallback to the other product.

| Product | DWD output frequency | Run times |
|---|---|---|
| `MOSMIX_S` | Hourly | A new product run each hour |
| `MOSMIX_L` | Four times daily | 03, 09, 15 and 21 UTC |

These describe provider cycles, not an internal scheduler or guaranteed file availability exactly at run time. Keep issue, retrieval and validity times separate. Read the actual forecast horizon from the file timeline; a new file does not prove all contained steps were recalculated. Define freshness limits and external retrieval plans per product.

The example keeps `--ParseMOSMIX` and additionally requires `--Product MOSMIX_S|MOSMIX_L` to select the library parser. Reject missing/unknown products before retrieval. `--SelectMOSMIX` keeps the existing geographic/altitude options and distinguishes known availability/elements per product; explicitly mark unknown availability. Output metadata, source identifiers and configured MQTT topics must distinguish S/L to prevent accidental retained-state replacement.

Source, checked 2026-10-14: [DWD procedure description, section 2.1.3/table 2.1](https://www.dwd.de/DE/leistungen/met_verfahren_mosmix/mosmix_verfahrenbeschreibung_gesamt.pdf?__blob=publicationFile&v=6). Recheck cycles before implementation/release; no parser is implemented yet.

## Tests

- Follow TDD: Red → Green → Refactor. Demonstrate the expected failure first, implement minimally, then refactor with green tests. Bug fixes begin with a reproducing regression test.
- Use MSTest, Moq and FluentAssertions.
- Build the test subject and shared dependencies in `[TestInitialize]`. Prefer a `MockRepository` and explicitly configure expected interactions.
- Call `VerifyAll()` for all mocks in `[TestCleanup]`. Individual tests may add their own inputs and expectations.
- Maintain meaningful behavior, failure and boundary tests from REQUIREMENTS. Avoid implementation-mirroring tests and trivial getter-only tests.
- Explicitly test MOSMIX catalog filters, IDs, KML/KMZ, units, missing values, forecast times, topics and export equivalence.
- Check `[Serializable]` on all export entities and verify complete graphs with the real JSON/XML serializers, including equivalence, UTC, omitted values and special characters. Do not completely mock the serializer in contract tests.
- Code-block coverage (as defined above) must reach at least 80%. During initial bootstrap without a measurable target-branch baseline, only this minimum applies; target-branch comparison is not applicable. An unexpectedly missing, stale or incomparable baseline in subsequent operation remains an error. Documentation-only PRs may retain the same coverage for the block metric as the target branch; decreases are forbidden. Other PRs must strictly improve the block metric below a 100% baseline; an exact 100% baseline accepts an exact 100% result. Use current commit IDs, comparable collection conditions and unrounded counter ratios. Missing PR measurements, invalid counts and zero denominators cannot pass. Microsoft.CodeCoverage reports, including the code-block breakdown, are the basis for verification; Cobertura line-rate, branch-rate or any Cobertura-derived value must never be used as or substituted for the block metric; missing or unreadable native block counters fail. Verify metric mapping and report format against real reports in P1.
- Run standard tests without internet or production brokers. Mock HTTP, time and MQTT; prefer synthetic fixtures.
- Keep integration tests separate and explicitly enabled. Normal tests must not publish production messages or alter target systems.
- Run relevant builds/tests after implementation changes. Report omitted or failed checks honestly; mocks are not evidence of hardware/live success.

## Source retrieval and logging

- Verify DWD formats/units against official documentation and OsthessenWetter against its actual published structure. Do not guess unknown fields or time references.
- Do not bypass access protection or make unbounded requests. Document frequency, retries and errors.
- Log stable error IDs, UTC, source/station and command. Never expose secrets or unchecked configuration objects.
- Write logs to stderr and reserve JSON/XML stdout for the document. Report MQTT success only after acknowledgment and export success only after complete output.
- MQTT error collection is a future feature. Broker failures must remain observable locally without it.

## Documentation

- Keep `DEVELOPMENT_PLAN.md` aligned with requirements and actual progress. Phases remain planned until acceptance is evidenced; never bypass unresolved coverage bootstrap/boundary decisions unilaterally.

- Keep README, REQUIREMENTS and actual status consistent. Mark commands as planned until implemented and verified.
- Keep public examples portable and free of private paths, addresses and secrets.
- Document changed assumptions, configuration precedence, fields, topics, units and source limitations.
- Do not implement extra features merely because they are mentioned as future extensions.

### Plan generation (user decision dated 2026-10-28)

Default approach for generating the plans of a phase, first used for P8 on pull request #12 ([PHASE_P8_PLAN.md](PHASE_P8_PLAN.md) section 10); a current explicit user instruction takes precedence:

1. **Where**: on the user's instruction, the orchestrating LLM writes the phase plan `PHASE_P<n>_PLAN.md` and all step plans `docs/p<n>/P<n>_NN_<SHORT_NAME>_PLAN.md` together, on the post-merge documentation branch of the previous phase and its pull request into `develop` (or, without one, a documentation branch from `develop` with its own pull request). The plans are documentation: not generated by qwen. Before writing, it reads the previous phase's handoff and acceptance record, REQUIREMENTS.md, DEVELOPMENT_PLAN.md, the contracts in force and the repository code, and checks every cited ID, path and fact against them. `DEVELOPMENT_PLAN.md` points to the plan as "under review, not yet accepted".
2. **Base**: the branch contains the current `develop` (merged in when it is behind), so that the automatic review workflow and its briefs exist at the pull request's base and head.
3. **Review loop**: commit and push; confirm with `gh pr view <n> --json headRefOid` that GitHub reports the pushed head; mark the pull request ready for review, which starts the automatic Codex review (`.github/workflows/review.yml`); check that the run's head SHA is the pushed head, otherwise convert to draft and mark ready again. When the review comment arrives, convert the pull request to draft, verify every finding against the requirements, the contracts and the code (as for every Codex review), fix confirmed findings in the plans, report rejected findings with the reason, record the round (head, run, finding count, each finding and its disposition) in section 10 of the phase plan, commit, push and mark ready again. Repeat until a round reports 0 findings; the commit that records that round is reviewed once more, and that last round is recorded in its pull request comment only, so the loop ends.
4. **Acceptance**: the user accepts the plans; the orchestrating LLM records the wording and date in the phase plan (status line and section 10), in every step plan status line and in `DEVELOPMENT_PLAN.md`, stating that accepting the plans neither starts the phase nor approves any contract row; that commit goes through one more review round. The user merges the pull request; the phase starts only on a separate instruction.
5. **Contents**: the phase plan follows the structure of the previous phase plans (foundation check, scope and exclusions, decision rows decided by the user in the contract step, behavior contracts, ordered steps, requirement-to-test matrix, evidence, files, checklist, authorship and review); every step plan names its prerequisites, decisions, artifacts, Red → Green → Refactor slices with every planned test named, test IDs, evidence, risks and acceptance criteria.

### Phase implementation (user decision dated 2026-10-28)

Default approach for implementing a phase, first used for P8 on pull request #14; a current explicit user instruction takes precedence:

1. **Autonomy**: the orchestrating LLM works through the phase alone as far as possible: step after step, committing and pushing every step to the phase pull request, without waiting for the user between steps. It stops only for what needs the user: a decision or contract approval, an authorization (for example a target-hardware run or a live check), the phase acceptance and the merge.
2. **Review before every user approval**: before the user is asked to approve a decision (for example a contract step) or to accept the phase implementation, the pull request is marked ready for review, which starts the automatic Codex review (`.github/workflows/review.yml`); the review loop of "Plan generation" step 3 applies: confirm the pushed head, mark ready, check the run's head SHA, convert to draft while fixing, verify every finding, fix confirmed findings (code and tests test-first through qwen, documentation by the orchestrating LLM), report rejected findings with the reason, record the round, push and mark ready again, until a round reports 0 findings. Only then is the user asked for the approval or acceptance; the pull request stays ready for review while the user decides. If the tenth round still reports findings (the limit of the contract-decision rule in "Second reviewer: Codex"), the orchestrating LLM stops the loop and reports the remaining findings with its check of each to the user; that report asks only how to continue (for example further rounds, a changed approach or acceptance of named findings) and is not a request for approval; an approval or acceptance follows only a 0-finding round or the user's explicit decision on those named findings.
3. **After a decision approval**: the orchestrating LLM records the approval, converts the pull request to draft and implements the rest of the phase to its end (integrated verification and acceptance drafts included); then it marks the pull request ready for review again and fixes all review findings as in step 2 before asking the user to accept the phase.
4. **Unchanged**, apart from the approval gate of step 2: the per-step Codex reviews of "Second reviewer: Codex" (the automatic review supplements them; for a contract decision draft its rounds count as the Codex review rounds and are recorded in the contract record), the qwen rules, test-first work, commits and pushes on the phase branch only, and the merge by the user. This supersedes "mark it ready for review only at the end of the phase" in "Phase pull requests" for these review points.

Consistency rule: [CONTRIBUTING.md](CONTRIBUTING.md) holds every developer procedure; keep the documents below in sync in the same change. Reviews enforce this table; there is no automated check.

| Change in | Must update |
|---|---|
| `global.json` | CONTRIBUTING.md section 1, `DEPENDENCIES.md` |
| `Directory.Packages.props`, `packages.lock.json` | `DEPENDENCIES.md` |
| `.config/dotnet-tools.json` | `DEPENDENCIES.md`, CONTRIBUTING.md section 5 |
| Runner commands, JSON contract or exit codes | CONTRIBUTING.md section 5, `scripts/ci-local.ps1` (shared contract) |
| `scripts/ci-local.ps1` parameters, exit codes or run layout | CONTRIBUTING.md section 6 |
| `.github/workflows/ci.yml` (jobs, triggers, pinned actions) | CONTRIBUTING.md section 6, `DEPENDENCIES.md` |
| `.github/workflows/review.yml` (triggers, classification, pinned actions or Codex version) or `.github/review/*.md` | CONTRIBUTING.md section 6, `DEPENDENCIES.md`, AGENTS.md "Second reviewer: Codex" |
| `scripts/prepare-debian-arm64.sh` | CONTRIBUTING.md section 7, `DEPENDENCIES.md` |
| `coverage.runsettings` | CONTRIBUTING.md sections 5 and 7, `docs/COMPATIBILITY.md` |
| Project added to or removed from `WeatherParser.slnx` or `WeatherParser.Ci.slnf` | CONTRIBUTING.md sections 2 and 3, README project list |
| Phase status | README status summary, `PHASE_P1_PLAN.md`, `DEVELOPMENT_PLAN.md` |
| `README.md` (packed package readme) | Local package check as in CONTRIBUTING.md section 4, then the P1.09 script run on the committed revision; the consumer lock is not committed |
| `Directory.Packages.props` version used in a CONTRIBUTING.md command | The "Version literals" table of CONTRIBUTING.md and the commands listing it |

## Binding MOSMIX schema source

[DWD pointforecast_dwd_extension_V1_0.xsd](https://opendata.dwd.de/weather/lib/pointforecast_dwd_extension_V1_0.xsd) is the binding basis for both parsers' MOSMIX source/XML data structures. Derive types, namespaces, elements and cardinalities from this schema; verify additional KML structures before implementation. Record schema revision and checksum for reproducible type generation/derivation. Map source types to separate public output entities; they do not replace the normalized forecast contract. Follow TDD for schema/mapping tests. No C# data structures have been generated yet.


## Additional decisions dated 2026-10-14

- Development environment: Visual Studio 2026. Locally verified: Community 18.8.3 and .NET SDK 10.0.303; use this SDK revision as the reproducible starting point for the planned `global.json` and CI, retaining target framework `net10.0`. Check servicing status before implementation and explicitly document any required SDK update. Visual Studio is not a Debian runtime dependency. [Microsoft SDK/Visual Studio mapping](https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs).
- All program-generated messages are English: help, validation, errors, logs and status. Data field names remain language-independent; preserve original source names. Project documentation is English only (language-policy update dated 2026-10-19).
- Observation output includes the measurement timestamp for every exported quantity in UTC without a timezone suffix. Preserve different times per value; consumers handle temporal alignment, selection and evaluation. Keep inferred source times marked accordingly. Forecasts retain their separate issue/validity times.
- Round only for output, after normalization/conversion: temperature to 0.1 °C, pressure to 1 hPa or 1 mmHg, relative humidity to 1 percentage point and wind speed to 1 km/h. Compute both pressure outputs from the same unrounded original; account for rounding tolerance. Round exact midpoint ties away from zero (20.25 → 20.3; −20.25 → −20.3). No new rounding policy is imposed on other quantities.
- Generate reproducible XSD files for public XML export entities. XML serializer contract tests validate exports against these schemas. Keep output schemas separate from the DWD MOSMIX source XSD; implement generation/versioning in P2.
- Tests use MSTest, Moq and FluentAssertions; collect coverage using Microsoft.CodeCoverage. Record exact versions, test-runner integration and platform evidence in P1.
- Coverage exception: block coverage at an exact 100% target-branch baseline may remain at an exact 100% in the PR. Except for documentation-only PRs, block coverage below 100% requires strict improvement; initial bootstrap without a baseline skips comparison. Block coverage must reach at least 80%. A displayed, rounded 100% is insufficient.
- Coverage metric (decision dated 2026-10-19): Changed from line and branch coverage to code-block coverage as reported natively by Microsoft.CodeCoverage. This change is due to Microsoft.CodeCoverage not measuring branches and the ambiguity in line counts for partially covered lines. The earlier line and branch wording is superseded.
- Decide remaining CLI questions during CLI development in P3: configuration format/path, environment variables, secret handling, concrete exit codes and CLI cancellation/timeout bounds. These do not gate P0/P1 library work. Keep public library error/cancellation contracts separate; P1 may document minimal process conventions for tests without preempting the final CLI contract.

These decisions update documentation only; no application code or XSD files have been generated.


## P0 clarifications dated 2026-10-14

- Dependencies: On adoption or an explicitly requested update, choose the latest available stable versions, check .NET/platform compatibility and licensing, and pin the resolved versions reproducibly. Do not use unchecked floating versions on every build; document conflicts rather than silently selecting an older version.
- Coverage bootstrap: At least 80% block coverage, without target-branch comparison when no measurable baseline exists yet. Documentation-only PRs may retain equal coverage. The existing exact-100% exception remains valid. Use Microsoft.CodeCoverage reports with their native block counters for verification.
- Rounding: Round to the nearest agreed increment; round exact midpoint ties away from zero (`MidpointRounding.AwayFromZero`), for example 20.25 → 20.3 and −20.25 → −20.3. Round after conversion, not before.
- Sources: Consumers supply concrete DWD and OsthessenWetter endpoints through the respective parser constructor parameters. Do not embed source paths or treat them as an unresolved global P0 configuration decision. Document actual source formats and parameter mappings during the corresponding adapter implementation.
- OsthessenWetter: If the configured page cannot be retrieved, the parser throws an exception with an actionable English diagnostic; no replacement data or simulated success. Parse an available page using the documented as-is structure, including its displayed measurement timestamps. Do not add freshness filtering or artificially correct source data; consumers evaluate measurement timestamps. Shared UTC/unit/rounding rules still apply. When only a time of day is available, retain the explicitly permitted date inference and estimated marker under REQ-042; never replace a complete source timestamp. Decide CLI error presentation and exit codes in P3.

These are documentation decisions; they do not generate application code yet.


## Binding phase status and sequence

**2026-10-15: P0 complete; P1 solution/project scaffolding created. P1 is not yet accepted.**

**2026-10-19: P1 accepted by the user (see the [P1 acceptance record](docs/p1/P1_12_ACCEPTANCE.md#p1-acceptance-2026-10-19)). P2 is next and starts only on a separate instruction.**

**2026-10-20: P2 accepted by the user (see the [P2 acceptance record](docs/p2/P2_12_ACCEPTANCE.md)); complete: pull request #3 merged into `develop` on 2026-10-20 (merge commit `74bf625`). P4 started on 2026-10-20 on the user's instruction ([P4 handoff](docs/p2/P4_HANDOFF.md)).**

**2026-10-24: P4 accepted by the user on candidate `b6bc47f` ([P4 acceptance record](docs/p4/P4_12_ACCEPTANCE.md)); it is complete: pull request #5 merged into `develop` on 2026-10-24 (merge commit `95bcb1e`); P5 starts only on a separate instruction ([P5 handoff](docs/p4/P5_HANDOFF.md)).**

**2026-10-24: P5 (DWD observations) started on the user's instruction on branch `features/develop/P5` from `develop` `1e604dc` ([P5 handoff](docs/p4/P5_HANDOFF.md), [P5 plan](PHASE_P5_PLAN.md)); P5.01 (foundation check) complete with outcome READY ([record](docs/p5/P5_01_FOUNDATION_CHECK.md)); P5.02: observation contract OC-01 to OC-14 approved by the user on 2026-10-24 ([contract](docs/p5/P5_OBSERVATION_CONTRACT.md)) and the P5 plans reconciled with it.**

**2026-10-25: P5 (DWD observations) accepted by the user on candidate `fd461c8` ([P5 acceptance record](docs/p5/P5_11_ACCEPTANCE.md)) after the integrated verification READY on Windows, hosted CI and the NanoPi R76S ([P5.10 record](docs/p5/P5_10_INTEGRATED_VERIFICATION.md)); it is complete: pull request #7 merged into `develop` on 2026-10-25 (merge commit `29fd553`); P6 starts only on a separate instruction ([P6 handoff](docs/p5/P6_HANDOFF.md)).**

**2026-10-25: P6 (OsthessenWetter observations) started on 2026-10-25 on the user's instruction on branch `features/develop/P6` from `develop` `8d8079a` ([P6 handoff](docs/p5/P6_HANDOFF.md), [P6 plan](PHASE_P6_PLAN.md)); P6.01 (foundation check) complete with outcome READY ([record](docs/p6/P6_01_FOUNDATION_CHECK.md)); P6.02: OsthessenWetter contract OW-01 to OW-16 approved by the user on 2026-10-25 ([contract](docs/p6/P6_OSTHESSEN_CONTRACT.md)) and the P6 plans reconciled with it; P6.03 to P6.09 implemented test-first on 2026-10-25 and 2026-10-26 ([P6 verification record](docs/p6/P6_VERIFICATION.md)): the OsthessenWetter parser is available in the library; P6.10 (integrated verification) READY on candidate `4373399` on Windows, hosted CI and the NanoPi R76S, with one authorized live check, and after the PR #9 review fixes again on candidate `12e7205` ([P6.10 record](docs/p6/P6_10_INTEGRATED_VERIFICATION.md)); P6.11: evidence audit ACCEPTED ([P6.11 acceptance record](docs/p6/P6_11_ACCEPTANCE.md), [P7 handoff](docs/p6/P7_HANDOFF.md)), P6 accepted by the user on 2026-10-26 on candidate `12e7205` (the library; production use and redistribution of OsthessenWetter values stay open until the source conditions are clarified, OW-14); it is complete: pull request #9 merged into `develop` on 2026-10-26 (merge commit `de9d4ba`); P7 (MOSMIX forecasts) started on 2026-10-26 on the user's instruction on branch `features/develop/P7` from `develop` `8009f1c` ([P7 handoff](docs/p6/P7_HANDOFF.md), [P7 plan](PHASE_P7_PLAN.md)); P7.01 (foundation check) complete with outcome READY ([record](docs/p7/P7_01_FOUNDATION_CHECK.md)); P7.02: MOSMIX forecast contract MF-01 to MF-18 approved by the user on 2026-10-26 ([contract](docs/p7/P7_FORECAST_CONTRACT.md)) and the P7 plans reconciled with it.**

**2026-10-28: P7 (MOSMIX forecasts) accepted by the user on candidate `2f1115b` after the integrated verification READY on Windows, hosted CI and the NanoPi R76S with one authorized live parse per product ([P7.11 record](docs/p7/P7_11_INTEGRATED_VERIFICATION.md), [P7 acceptance record](docs/p7/P7_12_ACCEPTANCE.md), [P8 handoff](docs/p7/P8_HANDOFF.md)); it is complete: pull request #11 merged into `develop` on 2026-10-28 (merge commit `20b2ef0`); P8 starts only on a separate instruction.**

**2026-10-28: P8 (MQTT serializer and complete library integration) started on the user's instruction on branch `features/develop/P8` from `develop` `8ed51c7` (the merge of the P8 planning pull request #12; [P8 plan](PHASE_P8_PLAN.md), [P8 handoff](docs/p7/P8_HANDOFF.md)); P8.01 (foundation check) complete with outcome READY ([record](docs/p8/P8_01_FOUNDATION_CHECK.md)); P8.02 (MQTT message contract) in progress: draft MQ-01 to MQ-15 under review ([contract](docs/p8/P8_MQTT_CONTRACT.md)).**

Sequence with unchanged phase IDs: **P0 → P1 → P2 → P4 → P5 → P6 → P7 → P8 → P3 → P9**. Complete and accept the entire library first, including station selection, DWD/OsthessenWetter/MOSMIX_S/MOSMIX_L parsers, output entities, JSON/XML, XSDs, MQTT serializer and DI. Then begin P3 with the CLI usage example, configuration and example broker transport. Public release remains P9.

P1 contains only a technical package consumer as an integration test, not a CLI/application example. Before P3, accept source functionality directly through the library and its tests; CLI commands do not gate library phases. This sequence supersedes earlier numeric ordering or dependencies on P3. [P1 detailed plan](PHASE_P1_PLAN.md). The current task adds project scaffolding; domain implementation remains deferred.
