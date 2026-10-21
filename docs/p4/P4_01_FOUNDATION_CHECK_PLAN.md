# P4.01 – Foundation Check and Plan Adjustment: Implementation Plan

**Status: executed (2026-10-21); outcome READY ([record](P4_01_FOUNDATION_CHECK.md)).** Original planning note: plan only on 2026-10-20. Refines section 1 and the P4.01 row of section 5 of [PHASE_P4_PLAN.md](../../PHASE_P4_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief; reviewed read-only by the OpenAI Codex CLI (rounds: 4, 0); every finding was verified by the orchestrating agent and fixed by qwen unless stated otherwise; a cross-plan Codex review of all P4 plans ended with 0 findings (rounds: 5, 2, 0). Later: Codex-outage acceptance wording (batch after P4.07) and the Codex-unavailability exception in S5 (cross-plan review).

## 1. Goal and Scope
Goal: confirm that P2 is accepted and merged, extract the accepted foundation that P4 builds on, remeasure the native block coverage of the integration target as the P4 comparison baseline, open the P4 draft pull request under the AGENTS.md phase-PR rule, and update `PHASE_P4_PLAN.md` with the extracted facts before P4.02 starts.

In scope:
- Re-read `docs/p2/P2_12_ACCEPTANCE.md` (sections 8, 10, 11, 12), `docs/p2/P4_HANDOFF.md`, `docs/p2/P2_VERIFICATION.md` and `PHASE_P2_PLAN.md` status
- Check the merge commit `74bf625` is an ancestor of `develop`
- Extract the fields of the extraction table in section 2
- Record the commits on `develop` after `74bf625` (at planning time only `6793fdc` and merge `b7da828`, AGENTS.md-only) and classify them (documentation-only or product change)
- SDK servicing check for SDK 10.0.402 against the official .NET release notes (report only; an SDK update needs a separate user decision)
- Remeasure the integration target with the local CI script
- First commit and push of the branch with the P2 completion records and this plan set, then the draft pull request into `develop` (only on the user's request)
- Update the phase plan section 1 with an extraction table; write the record `docs/p4/P4_01_FOUNDATION_CHECK.md`

Out of scope:
- Source-format research and contract decisions → P4.02
- Any production code or tests → P4.03 onward
- NanoPi target runs → P4.11
- Changes to the frozen export contract v1 → only via recorded additive decision (C-03)

## 2. Preconditions and Inputs
- P2 accepted (A7) and merged (A11, merge commit `74bf625eb4b2fbf49cbce7d1cee42b6302f2e47b`)
- The P2 completion records exist in the working tree of `features/develop/P4`
- `PHASE_P4_PLAN.md` reviewed by Codex with 0 open findings
- User instruction to start P4 (given 2026-10-20)

### Extraction table
| Field | Source | Required evidence |
|---|---|---|
| Integration target SHA | `develop` head when P4.01 runs (at planning time `b7da828`; P2 merge `74bf625`) | `git rev-parse origin/develop`, ancestry check |
| Package identity | P4 handoff section 1 | `WeatherParser.Parsing` `0.1.0-initial`, unpublished |
| SDK, runtime and coverage tools | `global.json`, `DEPENDENCIES.md`, `docs/p2/P2_VERIFICATION.md` | Exact versions |
| Export contract | `docs/p2/P2_EXPORT_CONTRACT.md` | v1 frozen, namespace `urn:weatherparser:export:v1`, C-01 to C-116, additive rule C-03/C-105 |
| Helpers for P4 | P4 handoff section 2 | List roots, `IListEntryOrdering`, serializers, `AddWeatherParsing`, schema validation settings (C-104) |
| P2 coverage at acceptance | `docs/p2/P2_12_ACCEPTANCE.md` | 3035/3037 on Windows and the NanoPi R76S (candidate `bced7be`) |
| Remeasured baseline | S4 run | Native block counters of the integration target |
| Inherited limitations | P4 handoff section 6 | Each limitation with its owner |

## 3. Decisions Required

| Decision | Options | Recommended | Decided by | Blocks |
| --- | --- | --- | --- | --- |
| Baseline commit for the P4 coverage comparison | `develop` head at P4.01 / `74bf625` | `develop` head, because hosted CI compares against the PR base, which is the `develop` head; `b7da828` differs from `74bf625` only in AGENTS.md | Orchestrator, recorded | S4 |
| SDK update if a newer 10.0 servicing SDK exists | stay on 10.0.402 / update | Stay; report the finding, the user decides any update separately | User | none in P4.01 |
| First push and draft pull request | now (after S1–S2) / later | After the user requests the first commit and push; then open the draft PR into `develop` immediately (AGENTS.md "Phase pull requests") | User | S3 |

## 4. Artifacts

| Path | Action | Content |
| --- | --- | --- |
| `docs/p4/P4_01_FOUNDATION_CHECK.md` | Create | Authorization, records rechecked, commits since `74bf625`, field-by-field extraction, SDK servicing result, baseline run, findings, plan reconciliation, outcome READY/NOT_READY, authorship |
| `PHASE_P4_PLAN.md` | Update section 1 | Extraction table and any plan adjustments with the evidence behind each |
| `docs/p4/P4_VERIFICATION.md` | Create section for P4.01 | Commands with exit codes, SHAs, counters, gate result file hash, run directory, PR number and CI run ID, SDK check URL and date, Codex review runs and findings, authorship |

## 5. Procedure

### S1 Records recheck
No Red: no behavior change.
**Roles:** qwen drafts the record text; the orchestrating agent confirms acceptance, merge and P4 handoff; checks `git merge-base --is-ancestor 74bf625 origin/develop`; lists `git log --oneline 74bf625..origin/develop` and classifies each commit.
**Check:** Read `docs/p2/P2_12_ACCEPTANCE.md` (sections 8, 10, 11, 12), `docs/p2/P4_HANDOFF.md`, `docs/p2/P2_VERIFICATION.md` and `PHASE_P2_PLAN.md` status; confirm P2 acceptance and merge; verify `74bf625` is ancestor of `develop`; list commits since `74bf625`.

### S2 Extraction and SDK check
No Red: no behavior change.
**Roles:** qwen drafts the extraction table; the orchestrating agent fills it with values from sources listed in section 2; checks SDK servicing status against official .NET release information.
**Check:** Fill the extraction table (section 2); check SDK servicing status (record URL and date).

### S3 Seed commit, push and draft PR
No Red: no behavior change.
**Roles:** qwen drafts commit messages and the PR description; the orchestrating agent runs the package check, git and gh commands and records the results.
**Check:** Confirm the README package check of CONTRIBUTING.md section 4 for the P2 completion records (ran on the working tree on 2026-10-20; repeat if `README.md` changed since); commit 1 = the P2 completion records (without the refreshed consumer lock); commit 2 = `PHASE_P4_PLAN.md` and the Codex-reviewed step plans; push; open the draft pull request into `develop` immediately; record both SHAs, the PR number and the first hosted CI run.

### S4 Baseline run on the committed revision
No Red: no behavior change.
**Roles:** qwen drafts the record text from them; the orchestrating agent runs the command and extracts the counters.
**Check:** Run `pwsh ./scripts/ci-local.ps1 -Commit <commit-2-sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings`; the candidate is documentation-only, so equal block coverage passes; it also serves as the CONTRIBUTING.md section 4 committed-revision run for the README change; record both counters, the gate result file hash and the run directory.

### S5 Plan reconciliation, review and record commit
No Red: no behavior change.
**Roles:** qwen drafts the `PHASE_P4_PLAN.md` section 1 extraction table, the adjustments and the record `docs/p4/P4_01_FOUNDATION_CHECK.md` plus the P4.01 section of `docs/p4/P4_VERIFICATION.md`; the orchestrating agent verifies every value; then Codex reviews these uncommitted changes read-only; qwen fixes confirmed findings and Codex re-reviews until 0 findings; only then is the user asked to commit; after the commit and push to the draft pull request the hosted CI run is recorded. If Codex is unavailable or fails, the orchestrating agent records the failed attempt, reports it to the user, and the user decides whether to repeat the review; a missing review does not block the commit request (AGENTS.md).
**Check:** Update `PHASE_P4_PLAN.md` section 1 with extraction table and adjustments; Codex reviews read-only; qwen fixes confirmed findings; outcome READY when every field is resolved and baseline run passed, otherwise NOT_READY.

## 6. Tests and Test IDs

| Step-local case | T-P4 ID | Requirement IDs | Behavior and boundary |
| --- | --- | --- | --- |
| S4 baseline | T-P4-16 | REQ-086 | Remeasured integration-target block counters under comparable conditions; documentation-only candidate passes with equal coverage (prepares the P4.11 comparison) |
| S1–S2 | none | REQ-017, REQ-085 | Foundation facts and inherited limitations recorded; no behavior tested |

## 7. Evidence to Record
- `docs/p4/P4_01_FOUNDATION_CHECK.md`: commands with exit codes, SHAs, counters, gate result file hash, run directory, PR number and CI run ID, SDK check URL and date, Codex review runs and findings, authorship
- `docs/p4/P4_VERIFICATION.md`: section for P4.01 with the same items

## 8. Risks and Limitations
- (a) `develop` moves during P4 — the baseline is remeasured against the then-current PR base in P4.11
- (b) the CI baseline pass may fail to check out a target that is not a local branch (P2.01 history; CONTRIBUTING.md section 7 requires the local `ci-commit`/`ci-target` branches) — fetch and create local refs first
- (c) an available SDK servicing update could tempt a silent change — report only

## 9. Acceptance Criteria
- [ ] All records rechecked and cited
- [ ] Merge ancestry confirmed
- [ ] Every field of the extraction table (section 2) filled with its source and evidence
- [ ] SDK check recorded
- [ ] Baseline run passed with recorded counters
- [ ] README package check confirmed before commit 1; draft PR opened directly after the first push (when the user requested it)
- [ ] Phase plan section 1 updated
- [ ] Codex review of the uncommitted record and plan change with 0 open findings before the record commit, or a recorded Codex unavailability or failure reported to the user (AGENTS.md)
- [ ] Outcome recorded
