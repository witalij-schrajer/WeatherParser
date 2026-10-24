# P4.08 – Catalog Retrieval Boundary: Implementation Plan

**Status: complete (2026-10-23): implemented (S1 to S6), committed as `97ced0b`, hosted CI run 39982260348 green; records in [P4_VERIFICATION.md](P4_VERIFICATION.md) section 8.** Original planning note: plan only on 2026-10-21. Refines the P4.08 row of section 5 and the "Retrieval" row of section 4 of [PHASE_P4_PLAN.md](../../PHASE_P4_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief; reviewed read-only by the OpenAI Codex CLI (rounds: 6, 3, 2, 2, 0); every finding was verified by the orchestrating agent and fixed by qwen unless stated otherwise; a cross-plan Codex review of all P4 plans ended with 0 findings (rounds: 5, 2, 0). Later: source results with diagnostics, no derivation (cross-plan review). The orchestrating agent restored six S3 tests from the previous version and inserted the package-preparation paragraph itself after failed qwen corrections. Reconciled with the approved selection contract (SC-09, SC-12, SC-17, SC-18, SC-22) on 2026-10-21 (P4.02 S6). S6 record (2026-10-21): qwen reconciled this plan from the orchestrating agent's change list and fixed the confirmed review findings section by section; the orchestrating agent edited these parts itself after failed qwen attempts, for regressions or for recorded design decisions: S4 source composition and CDC file names; artifact paths; removal of package preparation; section 9 criteria; the `CatalogRequest` downloader call (orchestrator decision); precedence tests; `TimeProvider` registration with the refresher (cross-plan). Codex five-angle review rounds: 8, 7, 6, 4, 3, 1, 1, 1, 0 findings; cross-plan five-angle review of all P4 plans: rounds 4, 3, 1, 1, 0 findings.

## 1. Goal and Scope

Goal: injectable catalog sources that receive every required endpoint as an explicit `Uri` through the consumer-supplied endpoint configuration approved in the service-registration SC row (C-107 forbids factory and instance registrations), validate those endpoints at construction without any retrieval, download catalog and availability data through an injectable HTTP boundary with bounded timeout and bounded size, honor cancellation, surface failures with actionable English messages and never fabricate data or fall back to other endpoints, and hand the downloaded content to the P4.05/P4.06/P4.07 readers; all tests offline with a mocked `HttpMessageHandler`.

In scope:
- HTTP download service (`ICatalogDownloader`/`CatalogDownloader`) using the SC-approved `HttpClient` acquisition
- Timeout via the SC bound with `CancellationTokenSource` linking so that caller cancellation (`OperationCanceledException` with the caller's token) and timeout (SC-defined timeout exception) are distinguishable
- Size bound checked against `Content-Length` before reading and enforced while streaming (a server that lies about or omits `Content-Length` is still stopped at the bound)
- Non-success status codes, connection failures and truncated bodies surfaced per SC
- Content handed to the readers through a callback API so the downloader observes reader completion: **Proposed** `DownloadAsync<T>(CatalogRequest request, Func<Stream, CancellationToken, Task<T>> read, CancellationToken cancellationToken)`; the downloader keeps the response and the bounded stream alive while `read` runs and disposes both afterwards on success, reader failure, cancellation and size-limit failure (signature decided by the orchestrating agent in this step and recorded in section 3; it is not one of the public selection interfaces of SC-11)
- Catalog sources (`ICdcCatalogSource`/`CdcCatalogSource`, `IMosmixCatalogSource`/`MosmixCatalogSource`) that combine endpoint configuration, downloader and readers
- Endpoint options types (`CdcCatalogEndpoints`, `MosmixCatalogEndpoints`) as decided in SC-12, with the limits of SC-18 and the MOSMIX_S settings of SC-22
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
| HTTP abstraction, timeout, size bound, retries, redirects, compression, error types | — | As approved in the retrieval-boundary SC row (SC-09); this step only implements it. No `Microsoft.Extensions.Http` dependency; the library adds no handler, so redirects, decompression, header limits and proxy are consumer responsibility documented in README.md (P4.10). Bounds: 30 s per request through a token linked to the caller's token; 8 MiB per list or catalog response, 1 MiB per per-station file, 64 MiB for the MOSMIX_S all-stations file, enforced by `Content-Length` and while streaming; no retries. Classification precedence (first match wins): caller cancellation → `OperationCanceledException` with the caller's token; selection deadline → `SelectionLimitException` `SELECTION-DEADLINE-EXCEEDED`; refresh deadline → `EvidenceCacheException` `MOSMIX-S-REFRESH-DEADLINE-EXCEEDED`; other request timeout → `CatalogRetrievalException` `CATALOG-TIMEOUT`; non-success status (except the HTTP 404 cases), connection failure or a short body → `CATALOG-RETRIEVAL-FAILED`; bound exceeded → `CatalogRetrievalException` `CATALOG-TOO-LARGE`; reader format failures unchanged. Messages: endpoint name and sanitized URI (scheme, host, path), inner exception not attached, only its type name. Tests for each precedence pair. | User (P4.02, approved 2026-10-21) | S1–S3 |
| Endpoint configuration under C-107 | — | As approved in the service-registration SC row (SC-12); for example options types registered by the consumer. `CdcCatalogEndpoints` with eight `Uri` properties (station list and `now` data directory of air temperature, wind, precipitation and solar) and the limits `MaxEvidenceCandidates` (`int`, default 100, 1 to 10000) and `SelectionDeadline` (`TimeSpan`, default 120 s, > 0 and ≤ 1 h); `MosmixCatalogEndpoints` with the station catalog URI, the MOSMIX_L `single_stations/` directory, the same limits, `MosmixSAllStationsUri`, `EvidenceCacheDirectory` (absolute local path, exclusive to the cache), `MosmixSEvidenceMaxAge` (required, 1 minute to 365 days, no default) and `MosmixSRefreshDeadline` (`TimeSpan`, default 10 minutes, > 0 and ≤ 1 h). Validation at construction without network or file-system access: endpoint errors `ParserEndpointValidationException` with the new additive `ErrorId` `ENDPOINT-INVALID` (regression test first on the existing P2 type), limit errors `SelectionLimitException` `SELECTION-LIMIT-INVALID`, cache-setting errors `EvidenceCacheException` `EVIDENCE-CACHE-SETTINGS-INVALID`; boundary tests on both sides of each range. URI rules: list, catalog and file URIs absolute HTTP(S), used unchanged (query allowed); directory URIs absolute HTTP(S), path ending with `/`, no query, no fragment; child URIs append the file name or `<id>/kml/<file>` to the path, the ID percent-encoded as a path segment; tests for the composed request URIs of all eight CDC endpoints and MOSMIX_L. | User (P4.02, approved 2026-10-21) | S4 |
| Downloader call | — | `DownloadAsync<T>(CatalogRequest request, Func<Stream, CancellationToken, Task<T>> read, CancellationToken cancellationToken)`; `cancellationToken` is the caller's token. `CatalogRequest` (Proposed immutable record) carries the endpoint name, the request URI, the size bound (8 MiB, 1 MiB or 64 MiB), whether HTTP 404 means "absent", the deadline token of the running selection or refresh and its kind (`Selection` or `Refresh`). The downloader links the caller token, the deadline token and its own 30 s token and, on cancellation, checks them in SC-09 order: caller token first (rethrown with the caller token), then the deadline token by kind, then its own timeout | Orchestrator, recorded | S1–S3, S5 |
| Interface and type names | — | The approved SC row wins: `ICatalogDownloader`, `ICdcCatalogSource`, `IMosmixCatalogSource`, `CdcCatalogEndpoints`, `MosmixCatalogEndpoints`, exceptions `CatalogRetrievalException` (with an English message and the endpoint name) and `SelectionLimitException` / names from the approved public-API SC row | User (P4.02, approved 2026-10-21) | S1–S4 |
| Test HTTP double | strict Moq mock of `HttpMessageHandler` (`Protected().Setup<Task<HttpResponseMessage>>("SendAsync", …)`) / a hand-written test handler | Strict Moq mock from the shared `MockRepository`, explicit expected requests, `VerifyAll()` in `[TestCleanup]`; a hand-written handler only for blocking or slow streams, recorded | Orchestrator, recorded | S1–S4 |

## 4. Artifacts

| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Catalogs/ICatalogDownloader.cs` | Create | Interface |
| `src/WeatherParser.Parsing/Catalogs/CatalogDownloader.cs` | Create | Implementation |
| `src/WeatherParser.Parsing/Catalogs/ICdcCatalogSource.cs` | Create | Interface |
| `src/WeatherParser.Parsing/Catalogs/CdcCatalogSource.cs` | Create | Implementation |
| `src/WeatherParser.Parsing/Catalogs/IMosmixCatalogSource.cs` | Create | Interface |
| `src/WeatherParser.Parsing/Catalogs/MosmixCatalogSource.cs` | Create | Implementation |
| `src/WeatherParser.Parsing/Catalogs/IMosmixSEvidenceRefresher.cs` | Create | Interface |
| `src/WeatherParser.Parsing/Catalogs/MosmixSEvidenceRefresher.cs` | Create | Implementation |
| `src/WeatherParser.Parsing/Catalogs/CdcCatalogEndpoints.cs` | Create | Options type with validation rules from SC-12; reuse `MosmixCatalogEndpoints` from P4.07 |
| `src/WeatherParser.Parsing/Evidence/MosmixS/MosmixCatalogEndpoints.cs` | Reuse | Created and validated in P4.07; not changed here |
| `src/WeatherParser.Parsing/Catalogs/CatalogRetrievalException.cs` | Create | Exception with `ErrorId` `CATALOG-RETRIEVAL-FAILED`, `CATALOG-TIMEOUT`, and `CATALOG-TOO-LARGE` |
| `src/WeatherParser.Parsing/Exceptions/EvidenceCacheException.cs` | Reuse | Created in P4.07; thrown here with `MOSMIX-S-REFRESH-DEADLINE-EXCEEDED` and `EVIDENCE-CACHE-FAILED` |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Registration in the slice that adds each interface; register `IMosmixSEvidenceRefresher` as singleton by implementation type |
| `src/WeatherParser.Parsing/Parsing/ParserEndpointValidator.cs` | Change | Sanitize password and query values from `ParserEndpointValidationException` messages |
| `tests/WeatherParser.Parsing.Tests/Catalogs/CatalogDownloaderTests.cs` | Create | Tests for S1–S2 |
| `tests/WeatherParser.Parsing.Tests/Catalogs/CdcCatalogSourceTests.cs` | Create | Tests for S4 |
| `tests/WeatherParser.Parsing.Tests/Catalogs/MosmixCatalogSourceTests.cs` | Create | Tests for S4 |
| `tests/WeatherParser.Parsing.Tests/Catalogs/MosmixSEvidenceRefresherTests.cs` | Create | Tests for S5 |
| `tests/WeatherParser.Parsing.Tests/ServiceCollectionExtensionsTests.cs` | Extend | Tests for S1, S4 registration; add synthetic endpoint options and the approved HTTP dependency before building validated containers |
| `tests/WeatherParser.Parsing.Tests/Architecture/ServiceRegistrationCompletenessTests.cs` | Change | Add synthetic endpoint options and the approved HTTP dependency before building validated containers |
| `tests/WeatherParser.Parsing.Tests/Architecture/NoSharedStateTests.cs` | Change | Add synthetic endpoint options and the approved HTTP dependency before building validated containers |
| `src/WeatherParser.Parsing/Parsing/ParserEndpointValidationException.cs` | Change | Add `ErrorId` `ENDPOINT-INVALID` |
| `tests/WeatherParser.Parsing.Tests/ParserEndpointValidatorTests.cs` | Change (extend) | Tests for invalid URI and duplicate name paths returning `ENDPOINT-INVALID` |
| `Directory.Packages.props`, `src/WeatherParser.Parsing/WeatherParser.Parsing.csproj`, affected `packages.lock.json` files, `DEPENDENCIES.md` | Change | Only if the retrieval-boundary SC row approved a new package; otherwise no package change |
| `docs/p4/P4_VERIFICATION.md` | Append | P4.08 section |

## 5. Procedure

### Slice S1 Download success and size bound

No package preparation: SC-09 adds no dependency (the consumer registers the `HttpClient`; no `Microsoft.Extensions.Http`), so `Directory.Packages.props`, the project file and the lock files stay unchanged.

**Red**
- `DownloadAsync_Success_ReturnsContent` (the exact request URI and method are verified)
- `DownloadAsync_ContentLengthAboveBound_FailsBeforeReadingBody`
- `DownloadAsync_BodyAboveBoundWithoutContentLength_StopsAtBound`
- `DownloadAsync_BodyAboveBoundWithWrongContentLength_StopsAtBound`
- `DownloadAsync_BodyExactlyAtBound_Succeeds`
- `DownloadAsync_StreamReadableDuringReader_DisposedAfterSuccess`
- `AddWeatherParsing_ResolvesCatalogDownloader` (validated container, C-108)
- Completeness test red when the interface appears (assertion failure; behavior tests fail with `NotImplementedException`)
- `DownloadAsync_UsesInjectedHttpClient_Unchanged` (new test to prove injected HttpClient is used unchanged)

**Green**
- Download with bound
- Before registering the downloader, add the approved HTTP dependency (as the retrieval-boundary SC row defines) to the validated containers in `tests/WeatherParser.Parsing.Tests/Architecture/ServiceRegistrationCompletenessTests.cs`, `tests/WeatherParser.Parsing.Tests/Architecture/NoSharedStateTests.cs` and the successful-container fixtures of `tests/WeatherParser.Parsing.Tests/ServiceCollectionExtensionsTests.cs`
- Apply the same HTTP dependency setup to the existing package-consumer containers in `tests/WeatherParser.Package.Tests/PackageServiceResolutionTests.cs`, `PackageSchemaDeliveryTests.cs` and `PackageSerializationTests.cs` (new consumer scenarios stay in P4.10)
- Registration
- No retries are performed by the library, as decided in SC-09

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
- `DownloadAsync_ConnectionFailure_ThrowsRetrievalException` (inner exception is null, only type name in message)
- `DownloadAsync_TruncatedBody_FollowsFailureSemantics`
- `DownloadAsync_BodyStallsAfterHeaders_TimesOut`
- `DownloadAsync_BodyStallsAfterHeaders_CallerCancellation_ThrowsWithCallerToken`
- `DownloadAsync_Timeout_ThrowsTimeoutDistinctFromCancellation` (a blocking test handler; the SC timeout elapses)
- `DownloadAsync_CallerCancellation_ThrowsOperationCanceledWithCallerToken`
- `DownloadAsync_NoRetry_SingleRequestOnFailure` (exactly one request, `Times.Once()`, no retries permitted)
- `DownloadAsync_ReaderFailureCancellationOrSizeLimit_DisposesResponseAndStream`
- `DownloadAsync_TransportFailure_MessageIsSanitized` (an `HttpRequestException` whose message contains the synthetic secret URI: the thrown `CatalogRetrievalException` message names the endpoint and the sanitized URI (scheme, host, path; no user info, no query); the inner exception is null and the message contains the type name `HttpRequestException`)
- `DownloadAsync_CallerCancellation_ThrowsOperationCanceledWithCallerToken`
- `Timeout_ThrowsCatalogRetrievalException` (30-second timeout, error ID `CATALOG-TIMEOUT`)
- `RefreshDeadlineExpiry_ThrowsEvidenceCacheException` (refresh deadline exceeded, error ID `MOSMIX-S-REFRESH-DEADLINE-EXCEEDED`)
- `CancellationBeforeRefreshDeadline_ThrowsOperationCanceledWithCallerToken` (caller token cancellation before refresh deadline, error ID `OperationCanceledException` with caller token)
- `SelectionDeadlineExpiry_ThrowsSelectionLimitException` (selection deadline exceeded, error ID `SELECTION-DEADLINE-EXCEEDED`)
- `SelectionAndTimeout_ThrowsSelectionLimitException` (selection deadline and timeout active, error ID `SELECTION-DEADLINE-EXCEEDED`, winner is selection deadline)
- `CallerCancellationAndSelectionDeadline_ThrowsOperationCanceledWithCallerToken` (both fired: caller token wins, SC-09 precedence 1 before 2)
- `CallerCancellationAndRequestTimeout_ThrowsOperationCanceledWithCallerToken` (precedence 1 before 4)
- `CallerCancellationAndRefreshDeadline_ThrowsOperationCanceledWithCallerToken` (both fired: caller token wins, SC-09 precedence 1 before 3)
- `ConsumerHttpClientTimeoutFirst_ThrowsCatalogTimeout` (the consumer's `HttpClient.Timeout` shorter than 30 s expires first: `CatalogRetrievalException` `CATALOG-TIMEOUT`, never a bare `OperationCanceledException`)
- A request belongs either to a selection or to a refresh, never both (`CatalogRequest` carries one deadline and its kind), so the selection/refresh pair does not arise
- `RefreshDeadlineAndRequestTimeout_RefreshDeadlineWins` (refresh deadline and request timeout both expired: `EvidenceCacheException` `MOSMIX-S-REFRESH-DEADLINE-EXCEEDED`, SC-09 precedence 3 before 4)

Expected failure: S1 implementation lets raw exceptions escape or never times out; tests that already pass are recorded as existing behavior.

**Green**
- Error mapping, timeout linking, cancellation
- Exception types: `CatalogRetrievalException` with `CATALOG-TOO-LARGE` for size limit exceeded
- Exception types: `EvidenceCacheException` with `MOSMIX-S-REFRESH-DEADLINE-EXCEEDED` for refresh deadline expiry
- Precedence tests: cancellation token → `OperationCanceledException`, selection deadline → `SelectionLimitException`, refresh deadline → `EvidenceCacheException`, timeout → `CatalogRetrievalException`
- Specific precedence test cases assert exact error IDs and caller token
- Simultaneous condition pairs for all precedence combinations

**Refactor**
- Remove duplication with all tests green

**Roles**
- Same as S1

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### Slice S3 Endpoint options, source skeletons and validation without retrieval

Introduces `CdcCatalogEndpoints`, the interface `ICdcCatalogSource` and its class with constructor that validates the endpoints; method bodies still throw `NotImplementedException` until S4.

**Red**
- `Constructor_InvalidOrRelativeEndpoint_ThrowsBeforeAnyRequest` (`ParserEndpointValidationException` via `IParserEndpointValidator`; the handler mock receives no call)
- `Constructor_MissingEndpoint_Throws`
- `Constructor_DuplicateEndpointName_Throws`
- `Constructor_InvalidDirectoryUri_TrailingSlashQueryFragment_Throws` (`ParserEndpointValidationException` with error ID `ENDPOINT-INVALID`)
- `Constructor_InvalidMaxEvidenceCandidates_ValueOutOfRange_Throws` (`SelectionLimitException` with error ID `SELECTION-LIMIT-INVALID`)
- `Constructor_InvalidSelectionDeadline_ValueOutOfRange_Throws` (`SelectionLimitException` with error ID `SELECTION-LIMIT-INVALID`)
- `Constructor_DefaultsAreApplied` (validates that default values are applied for `MaxEvidenceCandidates` and `SelectionDeadline`)
- `Constructor_BothSidesOfNumericRange_InvalidAndValid` (tests both bounds of `MaxEvidenceCandidates` range)
- `Constructor_BothSidesOfTimeSpanRange_InvalidAndValid` (tests both bounds of `SelectionDeadline` range)
- `Validator_InvalidEndpointWithCredentials_MessageIsSanitized` (synthetic `ftp://user:password@example.org/catalog?token=secret`: the `ParserEndpointValidationException` message contains the endpoint name but neither `password` nor `secret`)
- `AddWeatherParsing_ResolvesCdcCatalogSource` and `AddWeatherParsing_ResolvesMosmixCatalogSource` (validated container with consumer-registered endpoint options as the SC row defines)
- `Container_MissingEndpointOptions_FailsAtValidation` (C-108)
- `ParserEndpointValidationException_HasErrorId_EndpointInvalid` (new test for the additive error ID `ENDPOINT-INVALID`)
- `ParserEndpointValidator_InvalidDirectoryUri_TrailingSlashQueryFragment_ThrowsEndpointInvalid` (test for the specific validation path returning `ENDPOINT-INVALID`)
- `ParserEndpointValidator_DuplicateEndpointName_ThrowsEndpointInvalid` (test for the duplicate name path returning `ENDPOINT-INVALID`)
- Completeness test red when each interface appears (assertion failure on the missing registration)

Expected failure: endpoints not validated until first use; the validator message contains the full URI; registration and completeness assertions fail before registration.

**Green**
- Validation at construction through the existing validators; minimal sanitization change in `ParserEndpointValidator`
- Add synthetic endpoint options (`https://example.invalid/...`) to the same container fixtures (the HTTP dependency was added in S1), before the source registrations make those containers fail
- Add the synthetic endpoint options to the same three package-consumer containers
- Registration of both sources with consumer endpoint options
- Add `ENDPOINT-INVALID` error ID to `ParserEndpointValidationException`
- Add validation for `MaxEvidenceCandidates` and `SelectionDeadline` limits in constructor
- Apply defaults for `MaxEvidenceCandidates` (100) and `SelectionDeadline` (120 seconds)
- Validate numeric and time range boundaries for `MaxEvidenceCandidates` and `SelectionDeadline`

**Refactor**
- Remove duplication with all tests green

**Roles**
- Same as S1

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### Slice S4 Catalog source composition

**Red**
- `CdcCatalogSource_DownloadsAndReadsTheFourStationLists` (strict mocks of `ICatalogDownloader` and `ICdcStationCatalogReader`; each of the four station-list endpoints requested exactly once, the four `now` data directory endpoints never requested directly; no separate metadata reader exists (P4.06); returns a Proposed `CdcCatalogSourceResult` with the parsed lists and all reader diagnostics; no availability derivation here — P4.09 invokes `ICdcAvailabilityDeriver` with its sampled time)
- `MosmixCatalogSource_DownloadsAndReadsCatalog` (only the station-catalog endpoint is requested; MOSMIX_L evidence is requested separately per location, see below, and MOSMIX_S evidence comes from the index; returns a Proposed `MosmixCatalogSourceResult` with the selectable locations and all reader diagnostics; no availability resolution here — P4.09 invokes `IMosmixAvailabilityResolver`)
- `Sources_PreserveAllReaderDiagnostics` (diagnostics of skipped records are returned unchanged and exactly once; the sources do not log them)
- `Sources_PropagateReaderFormatFailure` (no successful empty catalog)
- `Sources_PropagateRetrievalFailure_WithoutPartialResult`
- `Sources_PassCancellationTokenThrough`
- `Sources_NeverUseOtherCatalogEndpoints` (CDC source never requests MOSMIX endpoints and vice versa)
- `Endpoints_AreUsedUnchanged` (the request URI equals the configured URI exactly, including path and query)
- `NoBuiltInOrFallbackEndpoint` (when the configured endpoint fails, no other URI is requested)
- `CdcCatalogSource_DownloadsPerStationFilesOnDemand` (per station, for each candidate product in the search area, the station's `now` file is downloaded from the configured directory endpoint; the file names are `10minutenwerte_TU_<id>_now.zip`, `10minutenwerte_wind_<id>_now.zip`, `10minutenwerte_nieder_<id>_now.zip` and `10minutenwerte_SOLAR_<id>_now.zip` for the air-temperature, wind, precipitation and solar directories (SC-03), appended to the directory URI with the ID percent-encoded as a path segment (SC-12); at most 4 requests in parallel (SC-18); the file size is bounded at 1 MiB; HTTP 404 returns "absent")
- `MosmixL_CatalogSource_DownloadsPerLocationKmlOnDemand` (for each location inside the search area, the file `MOSMIX_L_LATEST_<id>.kmz` is requested from the configured `single_stations/` directory; the child URI is `<id>/kml/MOSMIX_L_LATEST_<id>.kmz`; HTTP 404 returns "absent")

Expected failure: the S3 skeleton methods throw `NotImplementedException`.

**Green**
- Composition of downloader and readers into the source results (data plus diagnostics)
- Per-station file download orchestration with 1 MiB bounds, on-demand requests, exact filenames, contextual 404 handling

**Refactor**
- Remove duplication with all tests green

**Roles**
- Same as S1

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### Slice S5 MOSMIX_S evidence refresh

**Red**
- `MosmixSEvidenceRefresher_RefreshAsync_Success` (downloads, stream-parses the KML under SC-20 rules with all-stations replacements, records `downloaded_at`, writes index atomically through `IMosmixEvidenceCache`)
- `MosmixSEvidenceRefresher_RefreshAsync_DuplicateIdInPlacemark_InvalidatesFile` (duplicate ID in placemark invalidates the file)
- `MosmixSEvidenceRefresher_RefreshAsync_DuplicateIdInOrder_InvalidatesFile` (duplicate ID in order invalidates the file)
- `MosmixSEvidenceRefresher_RefreshAsync_OversizedDownload_ThrowsCatalogRetrievalException` with `CATALOG-TOO-LARGE`
- `MosmixSEvidenceRefresher_RefreshAsync_Timeout_ThrowsEvidenceCacheException` with `MOSMIX-S-REFRESH-DEADLINE-EXCEEDED`
- `MosmixSEvidenceRefresher_RefreshAsync_CacheDirectoryFailure_ThrowsEvidenceCacheException` with `EVIDENCE-CACHE-FAILED`
- `MosmixSEvidenceRefresher_RefreshAsync_PreviousIndexPreservedOnFailure`
- `MosmixSEvidenceRefresher_RefreshAsync_DownloadedAtFromTimeProvider` (the `downloaded_at` is taken from the `TimeProvider` at download completion)
- `RefreshAsync_CacheDirectoryNotWritable_ThrowsEvidenceCacheFailed` (`EvidenceCacheException` `EVIDENCE-CACHE-FAILED`)
- `RefreshAsync_DeadlineExceeded_ThrowsRefreshDeadlineExceeded` (`MOSMIX-S-REFRESH-DEADLINE-EXCEEDED`)
- `RefreshAsync_OversizedDownload_ThrowsTooLarge` (`CATALOG-TOO-LARGE`)
- `RefreshAsync_AnyFailure_KeepsPreviousIndex`
- `MosmixSEvidenceRefresher_RefreshAsync_TooManyPlacemarks_ThrowsCatalogFormatException` (1–20000 placemarks, SC-22)
- `MosmixSEvidenceRefresher_RefreshAsync_InvalidPlacemarkId_ThrowsCatalogFormatException` (ID grammar, SC-22)
- `MosmixSEvidenceRefresher_RefreshAsync_TooManyForecastsPerPlacemark_ThrowsCatalogFormatException` (200 forecasts per placemark, SC-22)
- `MosmixSEvidenceRefresher_RefreshAsync_OversizedMember_ThrowsCatalogFormatException` (2 GiB streamed-member bound, SC-22)
- `MosmixSEvidenceRefresher_RefreshAsync_InvalidIssueTime_ThrowsCatalogFormatException` (SC-20 XML structure and time format)
- `MosmixSEvidenceRefresher_RefreshAsync_MalformedXml_ThrowsCatalogFormatException` (SC-20 XML protections)
- `MosmixSEvidenceRefresher_RefreshAsync_NoPlacemarks_ThrowsCatalogFormatException` (zero placemarks, SC-22)
- `MosmixSEvidenceRefresher_RefreshAsync_MaxPlacemarks_Success` (20000 placemarks, SC-22)
- `MosmixSEvidenceRefresher_RefreshAsync_MaxForecastsPerPlacemark_Success` (200 forecasts per placemark, SC-22)
- `MosmixSEvidenceRefresher_RefreshAsync_MaxMemberSize_Success` (2 GiB streamed-member bound, SC-22)
- `MosmixSEvidenceRefresher_RefreshAsync_InvalidForecastElementName_ThrowsCatalogFormatException` (element name grammar, SC-22)
- `MosmixSEvidenceRefresher_RefreshAsync_DuplicatePlacemarkId_FirstOrder_ThrowsCatalogFormatException` (duplicate ID in first order, SC-22)
- `MosmixSEvidenceRefresher_RefreshAsync_DuplicatePlacemarkId_SecondOrder_ThrowsCatalogFormatException` (duplicate ID in second order, SC-22)
- `MosmixSEvidenceRefresher_RefreshAsync_RequestTimeout_ThrowsCatalogRetrievalException` with `CATALOG-TIMEOUT` (30-second request timeout, SC-09)
- `MosmixSEvidenceRefresher_RefreshAsync_CallerTokenCancellation_ThrowsOperationCanceledException` with caller token (SC-09)
- **Expected failure: `MosmixSEvidenceRefresher_RefreshAsync_InvalidForecastValue_ThrowsCatalogFormatException`** (SC-20 numeric grammar and token count, SC-22 replacements)  
- **Expected failure: `MosmixSEvidenceRefresher_RefreshAsync_MissingRequiredNode_ThrowsCatalogFormatException`** (SC-20 required node cardinalities, SC-22 replacements)
- **Expected failure: `MosmixSEvidenceRefresher_RefreshAsync_InvalidNamespace_ThrowsCatalogFormatException`** (SC-20 namespace handling, SC-22 replacements)

**Green**
- Implementation of `IMosmixSEvidenceRefresher.RefreshAsync(CancellationToken)` with all SC-22 rules
- The refresher takes the consumer-registered BCL `TimeProvider` (SC-13) for `downloaded_at`; this slice registers a `TimeProvider` (`TimeProvider.System` or a fake clock) in the validated containers of the architecture, parsing and package-consumer tests together with the refresher registration, so `ValidateOnBuild` stays green; P4.09 reuses these registrations
- Cancellation precedence: caller token → `OperationCanceledException`; selection deadline → `SelectionLimitException`; refresh deadline → `EvidenceCacheException` `MOSMIX-S-REFRESH-DEADLINE-EXCEEDED`; request timeout → `CatalogRetrievalException` `CATALOG-TIMEOUT`

**Refactor**
- Remove duplication with all tests green

**Roles**
- qwen generates tests first, then minimal implementation and refactoring, from briefs with the real declarations (`IMosmixSEvidenceRefresher`, `IMosmixEvidenceCache`, `TimeProvider`, `HttpClient`) and the SC rows
- orchestrating agent writes the briefs, checks that no test touches the network (all HTTP through the mocked handler), runs each Red, build, tests and coverage, reviews the diff and records authorship

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### Slice S6 Review and evidence

**No Red: review slice.**

Pre-commit coverage on the working tree per CONTRIBUTING.md section 5 (changed-file list from `git diff --name-only <integration-target-sha>` plus `git ls-files --others --exclude-standard`; baseline via `write-baseline` from a `coverage.xml` of the integration target collected the same way in a separate `git worktree`; runner `evaluate`; native covered/total block counters, baseline counters and runner result; exit 0 before Codex). If a package was added: locked restore, `DEPENDENCIES.md` and lock files updated per the AGENTS.md consistency table. Then Codex reviews the uncommitted diff read-only; qwen fixes confirmed findings test-first and the orchestrating agent reruns build, tests and coverage; checkpoint commit when the user requests it; `pwsh ./scripts/ci-local.ps1 -Commit <commit-sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings` on that commit; push and hosted CI record. Package consumer: pack the library and run the existing package-consumer tests against the freshly packed library per CONTRIBUTING.md section 4 (consumer lock refresh discarded unless a structural change was approved), before the Codex review.

## 6. Tests and Test IDs

| Step-local case | T-P4 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P4-10 | REQ-014, REQ-102 | Download success; size bound before and while reading, with missing or wrong `Content-Length`; exact bound accepted; response disposed |
| S2 | T-P4-10 | REQ-014 | Status and connection failures with actionable English messages and sanitized URIs; truncated bodies; timeout distinct from caller cancellation; retries as decided |
| S3 | T-P4-10 | REQ-014, REQ-016 | Endpoints validated at construction without retrieval; missing, invalid and duplicate endpoints; URI sanitization |
| S4 | T-P4-10 | REQ-014, REQ-102 | Sources combine download and readers; failures propagate without partial results; cancellation passed through; CDC and MOSMIX endpoints separate; URIs used unchanged; no fallback |
| S5 | T-P4-19 | REQ-101, REQ-102, REQ-083 | MOSMIX_S evidence refresh with all SC-22 rules; index written atomically; previous index preserved on failure; refresh traceability and acceptance verified |
| S1, S3 | T-P4-14 (partial) | REQ-016 | Services resolved through their interfaces with consumer endpoint options; registration completeness (C-107); endpoint validation |
| S1–S5 | T-P4-15 | REQ-085 | Recorded Red → Green → Refactor evidence |

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
- (f) inner exceptions not filtered — **Decided: No inner exceptions are attached to retrieval-boundary exceptions, only their type name appears in the sanitized diagnostic** (SC-09); **User (P4.02, approved 2026-10-21)**

## 9. Acceptance Criteria

- [x] Each behavior increment has recorded Red evidence
- [x] All T-P4-10 cases pass offline (all HTTP through the mocked handler)
- [x] No built-in, fallback or global URIs; endpoints validated at construction without retrieval
- [x] Timeout bound enforced while waiting for headers and while reading the body; body size bound enforced by `Content-Length` and streamed bytes; response-header size limit as the retrieval-boundary SC row decides (consumer responsibility for transport settings, documented in README.md); caller cancellation and timeout distinguishable (the README documentation of the consumer's transport settings is P4.10 work)
- [x] Diagnostics never contain credentials or query values (validation and retrieval)
- [x] Response and stream disposed after the reader on every path
- [x] Failures never produce empty successful catalogs
- [x] Sources registered with consumer endpoint options; existing DI fixtures updated; completeness tests green
- [x] Package changes (if any) consistent with the AGENTS.md table
- [x] Build without warnings
- [x] Codex review with 0 open findings, or a recorded Codex unavailability or failure reported to the user (AGENTS.md)
- [x] No selection, list assembly or CLI code added
- [x] Injected `HttpClient` used unchanged; test proving this requirement
- [x] T-P4-19 requirements mapped to REQ-101/102/083 and P4.08 portion explicitly required in acceptance
- [x] MOSMIX_S refresh operation completes within `MosmixSRefreshDeadline`
- [x] The refresh writes the index only through the P4.07 `IMosmixEvidenceCache`; stale and missing index handling in selections is P4.09 work
- [x] The refresh stream-parses the all-stations file under the SC-20 rules with the SC-22 replacements (1 to 20000 placemarks, unique IDs `^[A-Z0-9]{4,5}$`, at most 200 forecasts per placemark with names `^[A-Za-z0-9_]{1,16}$`, member at most 2 GiB, 64 MiB download), each bound tested on both sides (the 2 GiB bound through the internal test seam; the 64 MiB bound is enforced and tested generically by the downloader's size-bound tests)
- [x] Any refresh failure throws its SC-17 error ID and leaves the previous index unchanged
- [x] A non-writable cache directory makes the refresh fail with `EVIDENCE-CACHE-FAILED`
- [x] Prerequisites from P4.07 (`MosmixCatalogEndpoints`, `IMosmixEvidenceCache`, SC-20 reader internals) reused unchanged
