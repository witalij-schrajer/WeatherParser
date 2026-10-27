# P7.06 – Retrieval Boundary and Options: Implementation Plan

**Status: implemented and verified on 2026-10-27; completion pending the user's disposition of the TDD deviation recorded in the [verification record](P7_VERIFICATION.md) section 6 (reconciled with the approved [P7 forecast contract](P7_FORECAST_CONTRACT.md) in P7.02 S6; [verification record](P7_VERIFICATION.md) section 6).** Refines the P7.06 row of section 5 and the "Retrieval" and "Separation" rows of section 4 of [PHASE_P7_PLAN.md](../../PHASE_P7_PLAN.md).

## 1. Goal and Scope
Goal: per-product consumer options and one bounded retrieval per parse for each product, with endpoints validated at construction, the MOSMIX_L file URI composed from the configured directory, an additive per-request bound for the shared downloader so that a long MOSMIX_S transfer is limited by its own bound and not by the fixed 30 seconds, and every retrieval failure classified with the MF-14 IDs.
In scope: the endpoint and option types of MF-04, MF-05 and MF-09 per product, validated at construction without retrieval (absolute HTTP(S), no user information, deadline, request bound and `MaxRunAge` ranges); the additive request bound on `CatalogRequest` (default 30 s, existing callers unchanged, MF-05); the additive `CatalogDeadlineKind` members per product and their mapping; product retrieval services that issue one request through `ICatalogDownloader` and hand the body to a caller-supplied asynchronous reader callback; HTTP 404 of the MOSMIX_L file → `MOSMIX-ID-UNKNOWN`; size, timeout, transport and status classification; sanitized diagnostics; registration.
Out of scope with owner: reading the body → P7.04 (invoked by the parsers); the parse deadline's creation and precedence at parser level → P7.07, P7.08; README guidance → P7.10.

## 2. Preconditions and Inputs
- P7.02 approved MF-04, MF-05, MF-09, MF-14 and MF-15 on 2026-10-26: `ParseDeadline` default 60 s (MOSMIX_L) and 5 minutes (MOSMIX_S), permitted (0, 1 h]; `MaxRunAge` required without default, permitted (0, 240 h]; bounds 1 MiB / 16 MiB (MOSMIX_L) and 64 MiB / 2 GiB (MOSMIX_S); every consumer of `AddWeatherParsing` registers the four option types.
- Existing: `ICatalogDownloader`/`CatalogDownloader` (fixed `RequestTimeoutSeconds = 30` over the whole request including the callback; `CatalogRequest` with `MaxBytes`, `NotFoundIsAbsent`, `DeadlineToken`, `DeadlineKind`), `CatalogDeadlineKind` (`Selection`, `Refresh`, `Observation`, `OsthessenWetter`), `IParserEndpointValidator`/`IEndpointUriValidator`, the MOSMIX_L URI composition of SC-12 (P4 `MosmixCatalogSource`/`MosmixLRetrieval`), the P6 `OsthessenWetterPageSource` pattern and its stalled-body tests.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Option types | Approved: `MosmixLForecastEndpoints` (`SingleStationsDirectory`), `MosmixLForecastOptions` (`ParseDeadline`, `MaxRunAge`), `MosmixSForecastEndpoints` (`AllStationsUri`), `MosmixSForecastOptions` (`ParseDeadline`, `MaxRunAge`); no separate request-bound option | Separate endpoint and option types, as P6 | User (MF-04, MF-05, MF-09, approved) | S1 |
| Request bound | Approved: additive init member `CatalogRequest.RequestTimeout` (`TimeSpan?`; absent = the existing 30 s, so the positional constructor and every existing caller are unchanged); the MOSMIX_S source sets it to `MosmixSForecastOptions.ParseDeadline`, the MOSMIX_L source leaves it absent | As approved; the downloader uses it instead of the constant when present | User (MF-05, approved) | S2 |
| Services (Implemented) | `IMosmixLForecastSource.RetrieveAsync<T>(string mosmixId, Func<Stream, CancellationToken, Task<T>> read, CancellationToken deadline, CancellationToken cancellationToken)` and `IMosmixSForecastSource.RetrieveAsync<T>(read, deadline, cancellationToken)` / one source with a product parameter (rejected: REQ-109) | Two sources; diagnostics name the endpoint (`MosmixLSingleStationsDirectory`, `MosmixSAllStationsUri`) and its sanitized URI (scheme, host, path; MF-15); caller cancellation, then the deadline, are checked before a retrieval failure is mapped (MF-14) | Orchestrator, recorded in execution | S3 |
| Error mapping | Approved: `MosmixForecastException` (`ErrorId`, `Product`, `OmittedValues`, `NotExportedElements`) with `MOSMIX-ID-UNKNOWN` (MOSMIX_L HTTP 404), `MOSMIX-RETRIEVAL-FAILED` (other status, transport failure, request bound), `MOSMIX-TOO-LARGE`, `MOSMIX-DEADLINE-EXCEEDED`, `MOSMIX-OPTIONS-INVALID`, mapped from the `CatalogRetrievalException` IDs; the P4, P5 and P6 mappings unchanged | As approved | User (MF-14, approved) | S3 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Forecasts/Mosmix/MosmixLForecastEndpoints.cs`, `MosmixLForecastOptions.cs`, `MosmixSForecastEndpoints.cs`, `MosmixSForecastOptions.cs`, `MosmixForecastException.cs`, `IMosmixLForecastSource.cs`, `MosmixLForecastSource.cs`, `IMosmixSForecastSource.cs`, `MosmixSForecastSource.cs` | Create | Options, exception, sources |
| `src/WeatherParser.Parsing/Catalogs/CatalogRequest.cs`, `CatalogDownloader.cs`, `CatalogDeadlineKind.cs` | Change | Additive request bound and deadline kinds |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Registration |
| `tests/WeatherParser.Parsing.Tests/Forecasts/Mosmix/MosmixForecastOptionsTests.cs`, `MosmixLForecastSourceTests.cs`, `MosmixSForecastSourceTests.cs`; `tests/WeatherParser.Parsing.Tests/Catalogs/*` | Create, Change | Option, source and downloader tests |
| `tests/WeatherParser.Parsing.Tests/TestConsumerServices.cs`, `tests/WeatherParser.Parsing.Tests/Selection/ConsumerBoundary.cs`, `tests/WeatherParser.Parsing.Tests/Live/LiveCheckRunner.cs`, `tests/WeatherParser.Package.Tests/TestConsumerServices.cs`, `tests/WeatherParser.Package.Tests/ConsumerSelectionSupport.cs`, and every other test setup that calls `AddWeatherParsing` without delegating to `AddTestConsumerServices` | Change | Consumer setups register the new mandatory options so existing containers keep building with `ValidateOnBuild` (P6 lesson); the full list is taken from a search for `AddWeatherParsing(` in both test projects at the start of S1 and recorded |
| `docs/p7/P7_VERIFICATION.md` | Change | P7.06 section |

## 5. Procedure
Every slice: roles and checks as in P7.03 section 5.

### S1 Options and endpoint validation
**Red**: per product `Construct_EndpointMissingRelativeNonHttpOrUserInfo_ThrowsOptionsInvalid`; for the MOSMIX_L directory the SC-12 directory rules: `Construct_DirectoryWithoutTrailingSlash_ThrowsOptionsInvalid` (`https://example.invalid/single_stations`), `Construct_DirectoryWithQuery_ThrowsOptionsInvalid`, `Construct_DirectoryWithFragment_ThrowsOptionsInvalid`, each without retrieval, `Construct_ValidDirectory_Accepted` with the composed path appended beneath it (`…/single_stations/<id>/kml/MOSMIX_L_LATEST_<id>.kmz`, never replacing the last segment); for the MOSMIX_S file URI the SC-12 list rule (used unchanged, a query allowed but never logged); `…_DefaultsAccepted`, `…_DeadlineSmallestAndLargestAccepted`, `…_DeadlineZeroNegativeInfiniteOneTickAboveMaxValue_ThrowsOptionsInvalid`, the same boundary set for `MaxRunAge` with its own limits (smallest positive and 240 h accepted; zero, negative, `Timeout.InfiniteTimeSpan`, one tick above 240 h and `TimeSpan.MaxValue` rejected; no default: unset → `MOSMIX-OPTIONS-INVALID`), `…_DefaultDeadlines_60SecondsAnd5Minutes`, `…_ValidationMakesNoRequest` (strict HTTP handler without setup), `…_MessageNamesOptionWithoutQuery`; `AddWeatherParsing_ResolvesBothSourcesWithIndependentOptions`.
**Green**: option types and validation; registration of both sources in this slice with every consumer setup of the artifact list updated; the existing container tests of those setups (selection, package, live runner) green unchanged; `ServiceRegistrationCompletenessTests` red while unregistered.

### S2 Additive request bound in the shared downloader
**Characterization (green before and after, no Red)**: `DownloadAsync_DefaultRequestTimeout_Is30Seconds` (existing behavior with a fake clock), `DownloadAsync_ExistingCallers_KeepThe30SecondBound`.
**Red**: `DownloadAsync_LongerRequestTimeout_CallbackBeyond30SecondsSucceeds`, `DownloadAsync_RequestTimeoutExpires_ClassifiedTimeout`, `DownloadAsync_DeadlineBeforeRequestTimeout_ClassifiedDeadline`, `DownloadAsync_RequestTimeoutEqualsDeadline_ClassifiedDeadline` (the tie of the MOSMIX_S configuration), `DownloadAsync_CallerCancellationFirst`, `DownloadAsync_NewDeadlineKinds_MappedToProductIds`; the existing P4, P5 and P6 downloader, selection, observation and OsthessenWetter tests green unchanged.
**Green**: the additive member and the deadline kinds.

### S3 Product sources
**Red**: `MosmixL_RetrieveAsync_ComposesSingleStationUri` (`<dir>/<id>/kml/MOSMIX_L_LATEST_<id>.kmz`, IDs with leading zeros and letters, no escaping surprises), `MosmixL_NotFound_ThrowsIdUnknown`, `MosmixS_RetrieveAsync_UsesConfiguredUriUnchanged`, per product `…_OneRequestNoRetry`, `…_StatusFailure_RetrievalFailed` (404 for MOSMIX_S, 500, 503), `…_TransportFailure_RetrievalFailed`, `…_ContentLengthAboveBound_TooLarge`, `…_StreamedBodyAboveBound_TooLarge`, `…_BodyStalledAfterHeaders_EndsByDeadline` (through the real downloader), `MosmixS_RequestCarriesTimeoutEqualToParseDeadline`, `MosmixL_RequestCarriesNoTimeout` (30 s default), `MosmixS_BodyStalledBeyond30Seconds_EndsByDeadline` (the request bound equals the deadline, so the stall ends as `MOSMIX-DEADLINE-EXCEEDED`, never at 30 s), `…_CallerCancellationWins`, `…_DiagnosticsSanitized` (endpoint with a query and user information never shown), `Sources_DoNotShareState` (two retrievals of different products in one container use only their own endpoint).
**Green**: sources and mapping.

**Refactor**: remove duplication with all tests green.

## 6. Tests and Test IDs
| Step-local case | T-P7 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P7-02 | REQ-014, REQ-016, REQ-109 | Options and endpoints at construction |
| S2, S3 | T-P7-03 | REQ-063, REQ-104, REQ-108 | Bounds, classification, request bound |
| S3 | T-P7-17 (endpoint) | REQ-072 | Sanitized diagnostics |
| S3 | T-P7-15 (source level) | REQ-109 | No shared state |
| all | T-P7-18 (registration), T-P7-20 | REQ-078, REQ-085, REQ-086 | Registration; Red evidence; coverage |

## 7. Evidence to Record
- Red names and failures, counts, coverage, authorship, Codex rounds; the unchanged counts of the existing downloader-dependent tests

## 8. Risks and Limitations
- (a) Consumers that do not use MOSMIX forecasts must still register the new options if they are mandatory (the P6 consequence); recorded for README
- (b) A long MOSMIX_S request bound keeps a connection open for minutes; bounded by the consumer's settings

## 9. Acceptance Criteria
- [ ] Every listed new-behavior test green with recorded Red; characterization tests (refactoring guards and existing behavior) green before and after and recorded as characterization, never with a manufactured Red; existing callers' behavior unchanged and proven
- [ ] Validation before any request; one request per retrieval; no retries
- [ ] Registration and architecture tests green; coverage gate passed; Codex 0 findings or reported
