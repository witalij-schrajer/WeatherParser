# P4.11 – Integrated Coverage and Platform Verification: Record

**Status (2026-10-23): S0 complete (live-check runner, test code only); S1 to S5 pending on the candidate that contains S0.**

Plan: [P4_11_INTEGRATED_VERIFICATION_PLAN.md](P4_11_INTEGRATED_VERIFICATION_PLAN.md).

## 1. Decisions

- **User decisions (2026-10-23, in chat)**:
  - The NanoPi R76S run (S3) is authorized: SSH access as `ssh R76S`, transfer of a git bundle of the candidate and of `develop`, the `ci-local.ps1` run in the home directory, copying the run directory back, cleanup of the working directory; tool installation only if `prepare-debian-arm64.sh` finds something missing, then the orchestrating agent stops and asks.
  - The optional live check is enabled: build the runner test-first (S0), commit it, then one live run (S4).
- **Orchestrator decisions (recorded)**:
  - Opt-in through the environment variable `WEATHERPARSER_LIVE_CHECK` with the exact value `1`; without it `LiveCheckRunner.RunIfEnabledAsync` returns before any other work and the live test `LiveCheck_DwdSources_OnlyWhenExplicitlyEnabled` (`[TestCategory("Live")]`) ends with `Assert.Inconclusive`, so default runs report it as skipped and make no network access.
  - Request budget = request count of one MOSMIX_S refresh, one MOSMIX selection and one CDC selection with `MaxEvidenceCandidates` 10: 1 + 1 + 10 + 4 + 40 = 56, enforced by `RequestBudgetHandler` (refuses the request beyond the budget, records URI, status, declared `Content-Length` and bytes actually read).
  - The live transport is `LiveCheckRunner.CreateTransport()`, a `SocketsHttpHandler` with automatic redirects disabled, so every HTTP request passes the budget and a redirect fails as `CATALOG-RETRIEVAL-FAILED`.
  - The runner resolves every service before the first request.
  - Sequence: MOSMIX_S refresh into a temporary cache directory, MOSMIX selection, CDC selection, both documents serialized to JSON and XML and validated against the v1 schema.
  - Endpoints from the SC source register (`LiveCheckSettings.Dwd`).
  - Search area 50.55, 9.68 (Fulda), 280 m, 15 km, 300 m altitude difference.

## 2. S0 Live-check runner

- **Scope**: Test code only, no production code; live runs only with the explicit opt-in.
- **Design**: The runner is implemented in `LiveCheckRunner.cs`, with test scaffolding in `LiveCheckRunnerTests.cs` and `LiveCheckRunnerFailureTests.cs`.
- **Coverage split and output**: the runner tests cover the opt-in guard, the request budget, the complete sequence, missing, oversized, invalid and stale indexes, refresh and selection deadlines, the request timeout, too many candidates, invalid limits, a server error without retry, a too large and a malformed catalog and caller cancellation; the library bounds at their exact limits (1 MiB, 8 MiB, 64 MiB) stay covered by P4.08. The live test writes the four documents and `summary.txt` (budget, requests with status, declared length and bytes read, counts, index timestamps, schema events, notices) to a temporary directory outside the repository and deletes its cache directory.
- **Red**: 26 failures on the runner stub (`NotImplementedException`), among them the live test; `RequestBudgetHandler_RefusesTheRequestBeyondTheBudget` passed on its first run because the handler was part of the scaffold.
- **Green**: 1607 Parsing tests pass, the live test is skipped; three test defects fixed by qwen (issue time of the MOSMIX_S fixture is 2026-10-23T03:00:00, the stale-index assertion targets the MOSMIX_S item, four HTTP 404 responses instead of five — two of them brief errors of the orchestrating agent).
- **Codex review**: Round 1, 3 findings, all confirmed and fixed test-first: response sizes only from `Content-Length` (now bytes read are counted; Red `RequestBudgetHandler_ResponseWithoutContentLength_CountsBytesRead`), three fixture URLs with wrong file names (corrected, exact evidence URLs asserted), automatic redirects bypassing the budget (`CreateTransport` disables them; `CreateTransport_DisablesAutomaticRedirects` passed on its first run because qwen implemented the method instead of the briefed stub). Round 2: 0 findings.
- **Checks**: `dotnet build WeatherParser.Ci.slnf -c Release --no-incremental` 0 warnings; Parsing 1609 pass and 1 skipped (live test), SchemaGeneration 46, Coverage 99, Coverage.Runner 56; pre-commit coverage gate before the round-1 fixes 6484/6487 above 3035/3037, exit 0 (test code is excluded from the block metric).
- **Authorship**: qwen3-coder:30b generated the runner, the tests and this record text from the orchestrating agent's briefs; the orchestrating agent wrote the briefs, ran every build, test, gate and review, reviewed each diff and added three missing `using` directives and fixed one typo (`EvidenceCacheCacheException`) by hand.

## 3. S1 to S5

Pending; recorded after the S0 commit.
