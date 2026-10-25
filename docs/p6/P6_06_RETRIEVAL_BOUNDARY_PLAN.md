# P6.06 – Retrieval Boundary: Implementation Plan

**Status: plan only (2026-10-25); nothing is implemented yet.** Refines the P6.06 row of section 5 and the "Retrieval" row of section 4 of [PHASE_P6_PLAN.md](../../PHASE_P6_PLAN.md). Reconciled on 2026-10-25 with the OsthessenWetter contract approved by the user on 2026-10-25 ([contract](P6_OSTHESSEN_CONTRACT.md), option A in every row): OW-10 `PageDeadline` default 60 s (instead of 30 s) and `MaxBytes` 2097152; OW-13 as planned.

## 1. Goal and Scope
Goal: an injectable page source that retrieves the consumer-supplied page once per call through the existing bounded downloader and classifies every failure, with consumer endpoints and options validated at construction.
In scope: `OsthessenWetterEndpoints` and the deadline part of `OsthessenWetterOptions` with validation at construction (`OSTHESSEN-OPTIONS-INVALID`), including endpoints with user information; an additive `CatalogDeadlineKind.OsthessenWetter` mapped by `CatalogDownloader` to `OSTHESSEN-DEADLINE-EXCEEDED`; one request with `MaxBytes` 2 MiB and `NotFoundIsAbsent` false; mapping of retrieval failures to `OSTHESSEN-PAGE-UNAVAILABLE`; sanitized diagnostics (endpoint name and host, no query); registration.
Out of scope with owner: the per-parse deadline token and failure precedence at parser level → P6.07; the metadata part of the options → P6.05 S2.

## 2. Preconditions and Inputs
- P6.02 approved OW-10 and OW-13.
- Existing types: `ICatalogDownloader`, `CatalogRequest`, `CatalogDeadlineKind`, `CatalogDownloader`, `CatalogRetrievalException`, `IParserEndpointValidator`, `IEndpointUriValidator`, the P5 `DwdObservationOptions` validation pattern.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Interface (Proposed) | `IOsthessenWetterPageSource` with `Task<T> ReadAsync<T>(Func<Stream, CancellationToken, Task<T>> read, CancellationToken deadline, CancellationToken cancellationToken)` / returning a buffered byte array | Streamed read inside the downloader's bound, as in P5 | Orchestrator, recorded | S2 |
| Deadline kind | Additive `CatalogDeadlineKind.OsthessenWetter` / reuse `Observation` | Additive kind, so the error ID names the provider; P4 and P5 mappings unchanged and re-tested | User (OW-10) | S2 |
| Endpoint validation | Reuse `IEndpointUriValidator` plus a check that rejects user information / own validator | Reuse plus the user-information check | Orchestrator, recorded | S1 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Observations/Osthessen/OsthessenWetterEndpoints.cs` | Create | `PageUri` |
| `src/WeatherParser.Parsing/Observations/Osthessen/OsthessenWetterOptions.cs` | Change | `PageDeadline` and validation |
| `src/WeatherParser.Parsing/Observations/Osthessen/IOsthessenWetterPageSource.cs`, `OsthessenWetterPageSource.cs` | Create | Source |
| `src/WeatherParser.Parsing/Catalogs/CatalogDeadlineKind.cs`, `CatalogDownloader.cs` | Change | Additive kind and mapping |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Registration |
| `tests/WeatherParser.Parsing.Tests/TestConsumerServices.cs`, `tests/WeatherParser.Package.Tests/TestConsumerServices.cs`, `tests/WeatherParser.Package.Tests/ConsumerSelectionSupport.cs`, `tests/WeatherParser.Parsing.Tests/Selection/ConsumerBoundary.cs` and both container setups in `tests/WeatherParser.Parsing.Tests/Live/LiveCheckRunner.cs` | Change | Register a valid synthetic `OsthessenWetterEndpoints` in the same slice that makes the page source depend on it, so every existing `ValidateOnBuild` container stays green |
| `tests/WeatherParser.Parsing.Tests/Observations/Osthessen/OsthessenWetterPageSourceTests.cs`, `OsthessenWetterOptionsTests.cs` | Create | Behavior tests |
| `tests/WeatherParser.Parsing.Tests/Catalogs/CatalogDownloader*Tests.cs` | Change | The new kind; existing kinds unchanged |
| `docs/p6/P6_VERIFICATION.md` | Change | P6.06 section |

## 5. Procedure
Every slice: roles and checks as in P6.03 section 5. HTTP is mocked through `HttpMessageHandler`; time through the existing test `ManualTimeProvider` or a strict `Mock<TimeProvider>`; no network.

### S1 Endpoints and options
**Red**: `Construct_AbsoluteHttpsUri_Accepted`, `Construct_HttpUri_Accepted`, `Construct_NullOrRelativeUri_ThrowsOptionsInvalid`, `Construct_UriWithUserInfo_ThrowsOptionsInvalid` (the message contains neither the user name nor the password), `PageDeadline_Default60Seconds`, `PageDeadline_OneTick_Accepted`, `PageDeadline_OneHour_Accepted`, `PageDeadline_Zero_Negative_Infinite_AboveOneHour_MaxValue_ThrowOptionsInvalid` (each before any timer or request), `Construct_NoRequestMade`, and metadata validation at source construction (validation logic of P6.05 S2, invoked by the source constructor): `Construct_MetadataLatitudeOrLongitudeOutOfRange_ThrowsOptionsInvalid`, `Construct_MetadataDuplicateName_ThrowsOptionsInvalid`, `Construct_MetadataEmptyName_ThrowsOptionsInvalid`, `Construct_MetadataNullEntry_ThrowsOptionsInvalid`, each before any request; `AddWeatherParsing_ResolvesOsthessenWetterPageSource`, and `ServiceRegistrationCompletenessTests` red with its missing-registration assertion while the interface is unregistered.
**Green**: validation in the constructor; singleton registration of the page source, before S2 starts. S2 keeps both registration checks green and adds no registration.

### S2 Retrieval and classification
**Red**: `Read_Success_OneRequestStreamRead`, `Read_404_ThrowsPageUnavailable`, `Read_500_503_ThrowPageUnavailable`, `Read_TransportFailure_ThrowsPageUnavailable`, `Read_HttpClientTimeout_ThrowsPageUnavailable` (timeout before response headers complete), `Read_OverMaxBytes_ThrowsPageUnavailable` (2 MiB + 1 byte; exactly 2 MiB accepted), `Read_DeadlineExpired_ThrowsDeadlineExceeded`, `Read_DefaultOptions_StalledRequestEndsAsPageUnavailableAt30Seconds` (with the default 60-second `PageDeadline`, the downloader's 30-second request timeout fires first and maps to `OSTHESSEN-PAGE-UNAVAILABLE`, OW-10), `Read_CallerCancelled_ThrowsOperationCanceled` (precedence over a simultaneous deadline), `Read_NoRetry` (exactly one request after a failure), `Read_Diagnostic_NoQueryString` (an endpoint `…/messwerte/?token=x` yields messages without `token`), `Downloader_ExistingKinds_Unchanged` (Selection, Refresh, Observation still map to their IDs); body stalled after the headers, through the real `CatalogDownloader` and a deterministic response body whose pending `ReadAsync` ends only when its token is cancelled, with a read callback that consumes the body asynchronously as the P6.04 reader does: `Read_BodyStalled_CallerCancelled_ThrowsOperationCanceled`, `Read_BodyStalled_PageDeadline_ThrowsDeadlineExceeded`, `Read_BodyStalled_DownloaderRequestTimeout_ThrowsPageUnavailable` (keep the caller token uncancelled and the page deadline beyond 30 seconds; advance the injected `ManualTimeProvider` to the downloader's 30-second request timeout and assert that it cancels the pending body read and maps to `OSTHESSEN-PAGE-UNAVAILABLE`), each ending without waiting for the body.
**Green**: request construction, failure mapping, additive kind.

**Refactor**: remove duplication with all tests green.

## 6. Tests and Test IDs
| Step-local case | T-P6 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P6-09 (options), T-P6-19 (endpoint) | REQ-014, REQ-063, REQ-072 | Validation at construction, deadline bounds, user information rejected |
| S1 | T-P6-08 (validation at construction) | C-51 | Metadata options validated when the source is constructed (validation logic from P6.05 S2) |
| S2 | T-P6-09 | REQ-046, REQ-063 | One request, size bound, no retries, failure classification, precedence |
| all | T-P6-13 (registration), T-P6-15 | REQ-078, REQ-085 | Registration; Red evidence |

## 7. Evidence to Record
- Red names and failures, counts, coverage, authorship, Codex rounds; the unchanged P4/P5 downloader tests listed

## 8. Risks and Limitations
- (a) The site answers with a non-HTML success page (for example a maintenance page) — not a retrieval failure; P6.04 reports it as `OSTHESSEN-FORMAT-CHANGED`
- (b) The library sets no User-Agent; the consumer's `HttpClient` does (README, P6.09)

## 9. Acceptance Criteria
- [ ] Every listed test green with recorded Red; P4 and P5 downloader behavior unchanged
- [ ] No retries; one request per call; size bound enforced
- [ ] No query string or user information in any message
- [ ] Registration and architecture tests green; coverage gate passed; Codex 0 findings or reported
