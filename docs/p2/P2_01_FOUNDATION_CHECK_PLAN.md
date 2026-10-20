# P2.01 – Check P1, Extract and Revise: Implementation Plan

**Status: plan only (2026-10-20); nothing is implemented or verified yet.** This plan refines [PHASE_P2_PLAN.md](../../PHASE_P2_PLAN.md) section 1 and the P2.01 row of section 5. Drafted by the local qwen3-coder:30b model over three review rounds; the orchestrating agent wrote the briefs, reviewed each draft against AGENTS.md, REQUIREMENTS.md and the phase plan, and itself restructured section 1 into scope lists, restored the SDK-update decision row and corrected one decision owner after qwen did not apply those corrections.

## 1. Goal and Scope
Goal: revalidate P1 completion, extract the accepted evidence, reconcile the phase plan with the current repository state and record the user's P2 implementation authorization before any P2 work starts.

In scope:
- Recheck the accepted P1 records.
- Revalidate the fields of the phase plan's section 1 extraction table.
- SDK servicing check.
- Sanity run on the current branch.
- Record the user's P2 implementation authorization.
- Reconcile `PHASE_P2_PLAN.md` section 1.
- Write `docs/p2/P2_01_FOUNDATION_CHECK.md` with the READY/NOT_READY decision.

Out of scope:
- P2 behavior, entities and tests → P2.02–P2.10.
- Contract decisions and the first serializer slice → P2.02.
- Integrated coverage/platform verification → P2.11.
- SDK or package updates → separate user instruction.
- Changes to `docs/p1/` records → never (historical evidence).

## 2. Preconditions and Inputs
- P1 acceptance records must be available: `PHASE_P1_PLAN.md`, `docs/p1/P1_12_ACCEPTANCE.md` (section "P1 acceptance 2026-10-19"), `docs/p1/P2_HANDOFF.md`, and linked P1.01–P1.11 records in `docs/p1/`.
- Repository state must be at or after commit `2cbee612df1159c8d1a9415223d3a1db95f979ca` (the P1 acceptance document revision inspected).
- The current branch is `features/develop/P2`.
- AGENTS.md requires SDK servicing status check and dependency updates only on explicit request.
- Existing repository facts: solution filter `WeatherParser.Ci.slnf`, test projects, package identity `WeatherParser.Parsing` version `0.1.0-initial`, SDK 10.0.402 pinned by `global.json`.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| User authorization for P2 implementation | Yes / No | — | User | P2.02 |
| Sanity run scope | Restore/build/tests only / Full two-pass CI run | Full two-pass CI run against PR base (also remeasures the integration target for P2.02's first candidate) | User | P2.02 |
| Handling of an available SDK servicing update | Keep 10.0.402 and record / Update via separate instruction | OPEN until the check result is known | User | P2.02 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p2/P2_01_FOUNDATION_CHECK.md` | Create | Revalidation results, sanity-run outcomes, SDK servicing check, user authorization and READY/NOT_READY decision |
| `PHASE_P2_PLAN.md` | Update | Extraction table and inherited limitations with current facts |

## 5. Procedure
TDD slices do not apply: P2.01 changes no product code or tests.

1. Record the user's explicit P2 implementation authorization (date, wording). Without it, stop: outcome NOT_READY.
2. Confirm the accepted records are unchanged: `git diff --stat 2cbee61 HEAD -- docs/p1 PHASE_P1_PLAN.md` must be empty; list commits since `2cbee61` with `git log --oneline 2cbee61..HEAD` and classify each (known at planning time: `c1d3b43`, `b4df185` hosted CI; `f433d94`, `0dc3f4c` phase plan).
3. Revalidate each field of the phase plan's section 1 extraction table against its source record: accepted SHA `6ac88e7…`, package SHA-512 and SHA-256, SDK 10.0.402 / runtime 10.0.13, coverage tool 18.11.0, run-settings SHA-256 (`Get-FileHash coverage.runsettings -Algorithm SHA256` must equal `dc6f662e4f6faaa853c33ed5497f182a31527fd126961411a604b683ffcf05d2`), exclusions, test counts, platform evidence. If the gitignored retained run directory `artifacts/ci/6ac88e7-20261019T213031Z/` is missing on the executing machine, rely on the hashes recorded in `docs/p1/P1_12_ACCEPTANCE.md` and note that.
4. SDK servicing check: compare the pinned SDK 10.0.402 with the current official .NET 10 servicing release; record the result and source URL; if an update is available or required, record it as an OPEN user decision and do not change `global.json`.
5. Sanity run on the current `features/develop/P2` HEAD: locked restore, Release build, unit tests; and (recommended) the full two-pass run `pwsh ./scripts/ci-local.ps1 -Commit <HEAD-sha> -TargetRef 22b3ae2 -RunSettings coverage.runsettings`, which also remeasures the integration target for P2.02. Record exit codes, test counts, block counters for current and target.
6. Reconcile `PHASE_P2_PLAN.md` section 1 (table and inherited limitations) with the results, citing evidence for each change.
7. Write `docs/p2/P2_01_FOUNDATION_CHECK.md` and state READY or NOT_READY; on NOT_READY list each missing prerequisite with its owner and stop before P2.02.

**Roles:** AGENTS.md makes qwen not applicable to recorded evidence, run results, user decisions and repository operations, which form most of this step. The orchestrating LLM records the authorization, runs every command, verifies every value against its source record, performs all repository operations and makes the READY/NOT_READY decision; qwen only drafts the prose of the record and the reconciliation edits from facts the orchestrator supplies; the orchestrator reviews the drafts and sends defects back to qwen.

**Check:**
- `git log --oneline 2cbee61..HEAD` to confirm repository changes since inspection.
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`.
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.
- `pwsh ./scripts/ci-local.ps1 -Commit <HEAD-sha> -TargetRef 22b3ae2 -RunSettings coverage.runsettings` (recommended full two-pass run; scope per section 3).

## 6. Tests and Test IDs
No T-P2 test IDs assigned to this step.

## 7. Evidence to Record
All evidence goes into `docs/p2/P2_01_FOUNDATION_CHECK.md`:
- Authorization record (date, wording).
- Commit classification since `2cbee61`.
- Field-by-field revalidation with source records.
- SDK servicing check result with source URL and date.
- Sanity-run commands, exit codes and test counts.
- Block counters for current and target if the two-pass run is executed.
- Plan reconciliation diff summary.
- Authorship (qwen-drafted prose, orchestrator-verified values).
- READY/NOT_READY decision with missing prerequisites and owners.

## 8. Risks and Limitations
- Repository changes since the last inspection may affect the validity of the P1 evidence.
- A required SDK update could delay P2 implementation if not addressed promptly.
- The sanity run might reveal issues with the current branch that were not present in the accepted P1 foundation.
- The retained run directory is local and gitignored and may be absent; platform evidence from hosted ARM64 job is a container on a GitHub-hosted runner and is not NanoPi R76S evidence.
- The sanity run is not P2 acceptance evidence; a newer servicing SDK changes the comparable conditions for coverage comparison, so an SDK change must happen before the target is remeasured.

## 9. Acceptance Criteria
- [ ] All P1 acceptance records are validated and consistent.
- [ ] Retained run's metadata, package hash, SDK/runtime versions, coverage settings, test counts, platform evidence are verified.
- [ ] Sanity run (restore/build/tests) outcome is recorded.
- [ ] If the two-pass run is executed, its exit code and block counters are recorded; a failure yields NOT_READY.
- [ ] SDK servicing check is performed and any required updates are documented.
- [ ] User authorization for P2 implementation is recorded with date and wording.
- [ ] READY/NOT_READY decision is clearly stated.
- [ ] No file under `docs/p1/` modified.
- [ ] `global.json` and `Directory.Packages.props` unchanged unless the user decided an update.
