# P4.08 – Catalog Retrieval Boundary: Implementation Plan

**Status: plan only (2026-10-21); nothing is implemented or verified yet.** Refines the P4.08 row of section 5 and the "Retrieval" row of section 4 of [PHASE_P4_PLAN.md](../../PHASE_P4_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief; reviewed read-only by the OpenAI Codex CLI (rounds: 6, 3, 2, 2, 0); every finding was verified by the orchestrating agent and fixed by qwen unless stated otherwise; a cross-plan Codex review of all P4 plans ended with 0 findings (rounds: 5, 2, 0). Later: source results with diagnostics, no derivation (cross-plan review). The orchestrating agent restored six S3 tests from the previous version and inserted the package-preparation paragraph itself after failed qwen corrections.

## 1. Goal and Scope

Goal: injectable catalog sources that receive every required endpoint as an explicit `Uri` through the consumer-supplied endpoint configuration approved in the service-registration SC row (C-107 forbids factory and instance registrations), validate those endpoints at construction without any retrieval, download catalog and availability data through an injectable HTTP boundary with bounded timeout and bounded size, honor cancellation, surface failures with actionable English messages and never fabricate data or fall back to other endpoints, and hand the downloaded content to the P4.05/P4.06/P4.07 readers; all tests offline with a mocked `HttpMessageHandler`.

In scope:
- HTTP download service (`ICatalogDownloader`/`CatalogDownloader`) using the SC-approved `HttpClient` acquisition
- Timeout via the SC bound with `CancellationTokenSource` linking so that caller cancellation (`OperationCanceledException` with the caller's token) and timeout (SC-defined timeout exception) are distinguishable
- Size bound checked against `Content-Length` before reading and enforced while streaming (a server that lies about or omits `Content-Length` is still stopped at the bound)
- Non-success status codes, connection failures and truncated bodies surfaced per SC
- Content handed to the readers through a callback API so the downloader observes reader completion: Proposed `DownloadAsync<T>(Uri uri, Func<Stream, CancellationToken, Task<T>> read, CancellationToken cancellationToken)`; the downloader keeps the response and the bounded stream alive while `read` runs and disposes both afterwards on success, reader failure, cancellation and size-limit failure (final API per the public-API SC row)
- Catalog sources (`ICdcCatalogSource`/`CdcCatalogSource`, `IMosmixCatalogSource`/`MosmixCatalogSource`) that combine endpoint configuration, downloader and readers
- Endpoint options types (`CdcCatalogEndpoints`, `MosmixCatalogEndpoints`) per the service-registration SC row
- Registration per slice

Out of scope (owner):
- Parsing formats → P4.05–P4.07
- Selection, filters, list assembly, clock and empty-result notice → P4.09
- DI wiring examples and package consumer → P4.10
- Live calls against DWD → P4.11 (explicitly enabled, separate)
- CLI configuration and exit codes → P3
- Scheduling and retries across invocations → external orchestration (AGENTS.md)

## 2. Preconditions and Inputs

- P4.05, P4.06 and P4.07 complete
- P4.02 approved the retrieval-boundary, catalog-failure-semantics, public-API and service-registration SC rows
- Existing declarations to reuse:
  - `IEndpointUriValidator.IsValid(Uri?)` (absolute HTTP(S) URI check)
  - `IParserEndpointValidator.ValidateAndMap(IReadOnlyCollection<ParserEndpointDefinition>)` (validates and maps named endpoints; throws `ParserEndpointValidationException` for invalid URIs and duplicate names)
  - `ParserEndpointDefinition(string Name, Uri Uri)`

## 3. Decisions Required

| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| HTTP abstraction, timeout, size bound, retries, redirects, compression, error types | — | As approved in the retrieval-boundary SC row; this step only implements it | User (P4.02) | S1–S3 |
| Endpoint configuration under C-107 | — | As approved in the service-registration SC row (for example options types registered by the consumer) | User (P4.02) | S4 |
| Interface and type names | Proposed `ICatalogDownloader`, `ICdcCatalogSource`, `IMosmixCatalogSource`, `CdcCatalogEndpoints`, `MosmixCatalogEndpoints`, Proposed exceptions `CatalogRetrievalException` (with an English message and the endpoint name) and `CatalogTooLargeException` / names from the approved public-API SC row | The approved SC row wins | User (P4.02) | S1–S4 |
| Test HTTP double | strict Moq mock of `HttpMessageHandler` (`Protected().Setup<Task<HttpResponseMessage>>("SendAsync", …)`) / a hand-written test handler | Strict Moq mock from the shared `MockRepository`, explicit expected requests, `VerifyAll()` in `[TestCleanup]`; a hand-written handler only for blocking or slow streams, recorded | Orchestrator, recorded | S1–S4 |

## 4. Artifacts

| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Catalogs/ICatalogDownloader.cs` | Create | Proposed interface |
| `src/WeatherParser.Parsing/Catalogs/CatalogDownloader.cs` | Create | Proposed implementation |
| `src/WeatherParser.Parsing/Catalogs/ICdcCatalogSource.cs` | Create | Proposed interface |
| `src/WeatherParser.Parsing/Catalogs/CdcCatalogSource.cs` | Create | Proposed implementation |
| `src/WeatherParser.Parsing/Catalogs/IMosmixCatalogSource.cs` | Create | Proposed interface |
| `src/WeatherParser.Parsing/Catalogs/MosmixCatalogSource.cs` | Create | Proposed implementation |
| `src/WeatherParser.Parsing/Catalogs/CdcCatalogEndpoints.cs` | Create | Proposed options type |
| `src/WeatherParser.Parsing/Catalogs/MosmixCatalogEndpoints.cs` | Create | Proposed options type |
| `src/WeatherParser.Parsing/Catalogs/CatalogRetrievalException.cs` | Create | Proposed exception |
| `src/WeatherParser.Parsing/Catalogs/CatalogTooLargeException.cs` | Create | Proposed exception |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Registration in the slice that adds each interface |
| `src/WeatherParser.Parsing/Parsing/ParserEndpointValidator.cs` | Change | Sanitize password and query values from `ParserEndpointValidationException` messages |
| `tests/WeatherParser.Parsing.Tests/Catalogs/CatalogDownloaderTests.cs` | Create | Tests for S1–S2 |
| `tests/WeatherParser.Parsing.Tests/Catalogs/CdcCatalogSourceTests.cs` | Create | Tests for S4 |
| `tests/WeatherParser.Parsing.Tests/Catalogs/MosmixCatalogSourceTests.cs` | Create | Tests for S4 |
| `tests/WeatherParser.Parsing.Tests/ServiceCollectionExtensionsTests.cs` | Extend | Tests for S1, S4 registration; add synthetic endpoint options and the approved HTTP dependency before building validated containers |
| `tests/WeatherParser.Parsing.Tests/Architecture/ServiceRegistrationCompletenessTests.cs` | Change | Add synthetic endpoint options and the approved HTTP dependency before building validated containers |
| `tests/WeatherParser.Parsing.Tests/Architecture/NoSharedStateTests.cs` | Change | Add synthetic endpoint options and the approved HTTP dependency before building validated containers |
| `Directory.Packages.props`, `src/WeatherParser.Parsing/WeatherParser.Parsing.csproj`, affected `packages.lock.json` files, `DEPENDENCIES.md` | Change | Only if the retrieval-boundary SC row approved a new package (for example `Microsoft.Extensions.Http`); otherwise no package change |
| `docs/p4/P4_VERIFICATION.md` | Append | P4.08 section |

## 5. Procedure

### Slice S1 Download success and size bound

Package preparation (only if approved): version, license and .NET 10 compatibility check against official sources; add the central version and the project reference; run `dotnet restore WeatherParser.Ci.slnf --force-evaluate` once to regenerate the lock files, review the lock diff, update `DEPENDENCIES.md`; afterwards every restore uses `--locked-mode`. If the package changes the consumer's dependency graph: pack the updated library into `artifacts/local-nuget` (CONTRIBUTING.md section 4), refresh `tests/WeatherParser.Package.Tests` against that package (it is not part of `WeatherParser.Ci.slnf`), review the consumer lock diff, run `dotnet restore tests/WeatherParser.Package.Tests --locked-mode` and the consumer tests while the refreshed hash is still present, and only afterwards restore the committed `WeatherParser.Parsing` `contentHash` while keeping the reviewed structural lock changes for the checkpoint commit (C-116); P4.10 adds the new consumer scenarios.

**Red**
- `DownloadAsync_Success_ReturnsContent` (the exact request URI and method are verified)
- `DownloadAsync_ContentLengthAboveBound_FailsBeforeReadingBody`
- `DownloadAsync_BodyAboveBoundWithoutContentLength_StopsAtBound`
- `DownloadAsync_BodyAboveBoundWithWrongContentLength_StopsAtBound`
- `DownloadAsync_BodyExactlyAtBound_Succeeds`
- `DownloadAsync_StreamReadableDuringReader_DisposedAfterSuccess`
- `AddWeatherParsing_ResolvesCatalogDownloader` (validated container, C-108)
- Completeness test red when the interface appears (assertion failure; behavior tests fail with `NotImplementedException`)

**Green**
- Download with bound
- Before registering the downloader, add the approved HTTP dependency (as the retrieval-boundary SC row defines) to the validated containers in `tests/WeatherParser.Parsing.Tests/Architecture/ServiceRegistrationCompletenessTests.cs`, `tests/WeatherParser.Parsing.Tests/Architecture/NoSharedStateTests.cs` and the successful-container fixtures of `tests/WeatherParser.Parsing.Tests/ServiceCollectionExtensionsTests.cs`
- Apply the same HTTP dependency setup to the existing package-consumer containers in `tests/WeatherParser.Package.Tests/PackageServiceResolutionTests.cs`, `PackageSchemaDeliveryTests.cs` and `PackageSerializationTests.cs` (new consumer scenarios stay in P4.10)
- Registration

**Refactor**
- Remove duplication with all tests green

**Roles**
- qwen generates tests first, then minimal implementation and refactoring, from briefs with the real declarations (`IEndpointUriValidator`, `IParserEndpointValidator`, `ParserEndpointDefinition`, `ParserEndpointValidationException`, the P4.05–P4.07 reader interfaces, `ServiceCollectionExtensions`) and the SC rows
- orchestrating agent writes the briefs, checks that no test touches the network (all HTTP through the mocked handler), runs each Red, build, tests and coverage, reviews the diff and records authorship

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### Slice S2 Failures, timeout and cancellation

**Red**
- `DownloadAsync_NonSuccessStatus_ThrowsWithEndpointNameAndStatus` (for example 404 and 503; English message, sanitized URI, no query values or user info)
- `DownloadAsync_ConnectionFailure_ThrowsRetrievalException` (inner `HttpRequestException` preserved)
- `DownloadAsync_TruncatedBody_FollowsFailureSemantics`
- `DownloadAsync_BodyStallsAfterHeaders_TimesOut`
- `DownloadAsync_BodyStallsAfterHeaders_CallerCancellation_ThrowsWithCallerToken`
- `DownloadAsync_Timeout_ThrowsTimeoutDistinctFromCancellation` (a blocking test handler; the SC timeout elapses)
- `DownloadAsync_CallerCancellation_ThrowsOperationCanceledWithCallerToken`
- `DownloadAsync_NoRetry_SingleRequestOnFailure` (exactly one request, `Times.Once()`, unless the SC row decides bounded retries — then the exact bound is tested)
- `DownloaderTransport_RedirectAndDecompressionSettings_FollowSc` (offline configuration check of the transport settings the library owns, for example `AllowAutoRedirect` and `AutomaticDecompression` on the handler the library creates, as the SC row decides; settings of a consumer-supplied `HttpClient` are documented as consumer responsibility in the README in P4.10; redirect and compression responses returned by the mocked handler test only the downloader's own handling of status codes and content)
- `DownloadAsync_ReaderFailureCancellationOrSizeLimit_DisposesResponseAndStream`
- `DownloadAsync_TransportFailure_MessageIsSanitized` (an `HttpRequestException` whose message contains the synthetic secret URI: the thrown retrieval exception's message is sanitized; the inner exception is preserved only as decided in the retrieval-boundary SC row and the plan states that consumers must not log inner exception messages unfiltered — record this as a limitation in section 8 if the SC row keeps the inner exception)

Expected failure: S1 implementation lets raw exceptions escape or never times out; tests that already pass are recorded as existing behavior.

**Green**
- Error mapping, timeout linking, cancellation

**Refactor**
- Remove duplication with all tests green

**Roles**
- Same as S1

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### Slice S3 Endpoint options, source skeletons and validation without retrieval

Introduces `CdcCatalogEndpoints`, `MosmixCatalogEndpoints`, the interfaces `ICdcCatalogSource` and `IMosmixCatalogSource` and their classes with constructors that validate the endpoints; method bodies still throw `NotImplementedException` until S4.

**Red**
- `Constructor_InvalidOrRelativeEndpoint_ThrowsBeforeAnyRequest` (`ParserEndpointValidationException` via `IParserEndpointValidator`; the handler mock receives no call)
- `Constructor_MissingEndpoint_Throws`
- `Constructor_DuplicateEndpointName_Throws`

- `Validator_InvalidEndpointWithCredentials_MessageIsSanitized` (synthetic `ftp://user:password@example.org/catalog?token=secret`: the `ParserEndpointValidationException` message contains the endpoint name but neither `password` nor `secret`)
- `AddWeatherParsing_ResolvesCdcCatalogSource` and `AddWeatherParsing_ResolvesMosmixCatalogSource` (validated container with consumer-registered endpoint options as the SC row defines)
- `Container_MissingEndpointOptions_FailsAtValidation` (C-108)
- Completeness test red when each interface appears (assertion failure on the missing registration)

Expected failure: endpoints not validated until first use; the validator message contains the full URI; registration and completeness assertions fail before registration.

**Green**
- Validation at construction through the existing validators; minimal sanitization change in `ParserEndpointValidator`
- Add synthetic endpoint options (`https://example.invalid/...`) to the same container fixtures (the HTTP dependency was added in S1), before the source registrations make those containers fail
- Add the synthetic endpoint options to the same three package-consumer containers
- Registration of both sources with consumer endpoint options

**Refactor**
- Remove duplication with all tests green

**Roles**
- Same as S1

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### Slice S4 Catalog source composition

**Red**
- `CdcCatalogSource_DownloadsAndReadsCatalogAndMetadata` (strict mocks of `ICatalogDownloader`, `ICdcStationCatalogReader` and the metadata reader per P4.06; each configured endpoint requested once; returns a Proposed `CdcCatalogSourceResult` with the catalog records, the parsed product metadata and all reader diagnostics; no availability derivation here — P4.09 invokes `ICdcAvailabilityDeriver` with its sampled time)
- `MosmixCatalogSource_DownloadsAndReadsCatalogAndEvidence` (returns a Proposed `MosmixCatalogSourceResult` with the selectable locations, the availability evidence and all reader diagnostics; no availability resolution here — P4.09 invokes `IMosmixAvailabilityResolver`)
- `Sources_PreserveAllReaderDiagnostics` (diagnostics of skipped records are returned unchanged and exactly once; the sources do not log them)
- `Sources_PropagateReaderFormatFailure` (no successful empty catalog)
- `Sources_PropagateRetrievalFailure_WithoutPartialResult`
- `Sources_PassCancellationTokenThrough`
- `Sources_NeverUseOtherCatalogEndpoints` (CDC source never requests MOSMIX endpoints and vice versa)
- `Endpoints_AreUsedUnchanged` (the request URI equals the configured URI exactly, including path and query)
- `NoBuiltInOrFallbackEndpoint` (when the configured endpoint fails, no other URI is requested)

Expected failure: the S3 skeleton methods throw `NotImplementedException`.

**Green**
- Composition of downloader and readers into the source results (data plus diagnostics)

**Refactor**
- Remove duplication with all tests green

**Roles**
- Same as S1

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### Slice S5 Review and evidence

**No Red: review slice.**

Pre-commit coverage on the working tree per CONTRIBUTING.md section 5 (changed-file list from `git diff --name-only <integration-target-sha>` plus `git ls-files --others --exclude-standard`; baseline via `write-baseline` from a `coverage.xml` of the integration target collected the same way in a separate `git worktree`; runner `evaluate`; native covered/total block counters, baseline counters and runner result; exit 0 before Codex). If a package was added: locked restore, `DEPENDENCIES.md` and lock files updated per the AGENTS.md consistency table. Then Codex reviews the uncommitted diff read-only; qwen fixes confirmed findings test-first and the orchestrating agent reruns build, tests and coverage; checkpoint commit when the user requests it; `pwsh ./scripts/ci-local.ps1 -Commit <commit-sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings` on that commit; push and hosted CI record. Package consumer: pack the library and run the existing package-consumer tests against the freshly packed library per CONTRIBUTING.md section 4 (consumer lock refresh discarded unless a structural change was approved), before the Codex review.

## 6. Tests and Test IDs

| Step-local case | T-P4 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P4-10 | REQ-014, REQ-102 | Download success; size bound before and while reading, with missing or wrong `Content-Length`; exact bound accepted; response disposed |
| S2 | T-P4-10 | REQ-014 | Status and connection failures with actionable English messages and sanitized URIs; truncated bodies; timeout distinct from caller cancellation; retries as decided |
| S3 | T-P4-10 | REQ-014, REQ-016 | Endpoints validated at construction without retrieval; missing, invalid and duplicate endpoints; URI sanitization |
| S4 | T-P4-10 | REQ-014, REQ-102 | Sources combine download and readers; failures propagate without partial results; cancellation passed through; CDC and MOSMIX endpoints separate; URIs used unchanged; no fallback |
| S1, S3 | T-P4-14 (partial) | REQ-016 | Services resolved through their interfaces with consumer endpoint options; registration completeness (C-107); endpoint validation |
| S1–S4 | T-P4-15 | REQ-085 | Recorded Red → Green → Refactor evidence |

## 7. Evidence to Record

- SC rows covered by each test
- Proof that standard tests are offline (all requests through the mocked handler)
- Red names and observed failures
- Existing-behavior records
- Commands
- Test counts
- Native block counters and runner result
- Package decision and `DEPENDENCIES.md` change if any
- Codex review and dispositions
- Authorship

## 8. Risks and Limitations

- (a) `HttpClient` lifetime misuse (socket exhaustion or stale DNS) — acquisition follows the SC row
- (b) unbounded memory for large catalogs — the bound is enforced while streaming, not only via `Content-Length`
- (c) timeouts reported as user cancellation — linked tokens and separate exception types are tested
- (d) secrets in URIs logged — messages use the endpoint name and a sanitized URI
- (e) tests that accidentally reach the network — strict handler mock without a default response
- (f) inner exceptions not filtered — when the retrieval-boundary SC row keeps the inner exception, consumers must not log it unfiltered

## 9. Acceptance Criteria

- [ ] Each behavior increment has recorded Red evidence
- [ ] All T-P4-10 cases pass offline (all HTTP through the mocked handler)
- [ ] No built-in, fallback or global URIs; endpoints validated at construction without retrieval
- [ ] Timeout bound enforced while waiting for headers and while reading the body; body size bound enforced by `Content-Length` and streamed bytes; response-header size limit as the retrieval-boundary SC row decides (configuration check for a library-owned handler, documented consumer responsibility otherwise); caller cancellation and timeout distinguishable
- [ ] Diagnostics never contain credentials or query values (validation and retrieval)
- [ ] Response and stream disposed after the reader on every path
- [ ] Failures never produce empty successful catalogs
- [ ] Sources registered with consumer endpoint options; existing DI fixtures updated; completeness tests green
- [ ] Package changes (if any) consistent with the AGENTS.md table
- [ ] Build without warnings
- [ ] Codex review with 0 open findings, or a recorded Codex unavailability or failure reported to the user (AGENTS.md)
- [ ] No selection, list assembly or CLI code added
