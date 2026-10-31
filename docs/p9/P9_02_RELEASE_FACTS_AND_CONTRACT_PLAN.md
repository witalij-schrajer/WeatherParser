# P9.02 – Release Facts and Release Contract: Implementation Plan

**Status: planned on 2026-10-31 on the user's instruction ("Generate the step plans for P9"); reviewed through the automatic Codex review of pull request #17 to 0 findings; accepted by the user on 2026-10-31 ("Accept the plans"); accepting the plans neither starts P9 nor approves any RL row. P9 started on 2026-10-31 on the user's instruction ("P9 Execution"); this step is complete: the [release contract](P9_RELEASE_CONTRACT.md) was reviewed to 0 findings on pull request #18 and approved by the user on 2026-10-31 ("Approve all"; RL-19 open), and the P9 plans are reconciled with it.** Refines the P9.02 row of section 5 and section 3 of [PHASE_P9_PLAN.md](../../PHASE_P9_PLAN.md).

## 1. Goal and Scope
Goal: the release contract RL-01 to RL-18, based on facts retrieved from official sources, reviewed through the automatic Codex review to 0 findings and approved by the user, with the P9 plans reconciled to it.
In scope: the facts record; `docs/p9/P9_RELEASE_CONTRACT.md` with one row per RL decision (options, recommendation, consequences, affected steps and tests); the review loop; the user's approval; the reconciliation section 10 of every affected step plan.
Out of scope with owner: every implementation → P9.03 onwards; publication → P9.12 with authorization.

## 2. Preconditions and Inputs
- P9.01 complete with outcome READY.
- Inputs: section 3 of the phase plan; the P9.01 extraction (package differences, API listing, servicing facts, baseline); REQ-003, REQ-004, REQ-005, REQ-013, REQ-016, REQ-017, REQ-086, REQ-090 to REQ-092; CL-04, CL-06, CL-19, CL-22; OW-14.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| RL-01 to RL-18 | As in section 3 of the phase plan, refined by the facts | The recommended options of the phase plan unless a fact contradicts them; a contradicted recommendation is changed in the contract with the fact named | User | P9.03 onwards |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p9/P9_RELEASE_CONTRACT.md` | Create | Facts (S1), RL-01 to RL-18, review rounds, the approval quoted verbatim |
| `PHASE_P9_PLAN.md`, `docs/p9/P9_03` to `P9_12` step plans | Change | Approved-contract note in section 3 of the phase plan; section 10 "Reconciliation with the approved contract" in each affected step plan |
| `docs/p9/P9_VERIFICATION.md` | Change | P9.02 section |

## 5. Procedure
Documentation only: written by the orchestrating agent; no production code.

### S1 Facts
Retrieve and record with URL, date and checked version: the name availability of the package ID on nuget.org and the nuget.org publication and API-key scoping rules (RL-01, RL-02); GitHub Packages consumption requirements (RL-02); the SDK's Source Link, `ContinuousIntegrationBuild`, symbol package (`.snupkg`) and embedded-PDB behavior (RL-03); SDK package validation and its baseline settings, and the licence and maintenance status of `Microsoft.CodeAnalysis.PublicApiAnalyzers` (RL-04); GitHub artifact attestations availability for the repository (RL-06); the .NET 10 runtime installation options on Debian 13 ARM64 from Microsoft's documentation (RL-07); the latest servicing SDK, runtime and `Microsoft.Extensions.*` versions and the MQTTnet latest stable version, licence and changelog since 5.2.0.1603 (RL-09, RL-10); the DWD terms and the OsthessenWetter conditions as recorded in the README, rechecked against the source pages (RL-16). No fact is taken from memory or model output.

### S2 Contract draft
Write RL-01 to RL-18 with the facts; name for each row its effect on the coverage gates (RL-09 changes gated library inputs; RL-11 changes the CLI rule) and on the consistency table of AGENTS.md.

### S3 Review loop
Mark the pull request ready for review; the automatic Codex review rounds count as the contract's Codex rounds (AGENTS.md "Second reviewer: Codex"); convert to draft while fixing; verify every finding; fix confirmed findings; report rejected ones with the reason; record each round in the contract; repeat until 0 findings or the tenth round (then report to the user and ask only how to continue).

### S4 User approval
Present every row with its recommendation; record the user's answer verbatim with the date; the pull request stays ready for review while the user decides; after the approval convert to draft.

### S5 Reconciliation
Add the approved-contract note to section 3 of the phase plan and a section 10 to every step plan whose content the contract changes (conditional steps P9.03 and P9.04 become "not applicable" records when their row is decided against); commit and push.

## 6. Tests and Test IDs
| Step-local case | T-P9 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 to S5 | all T-P9 rows (traceability) | REQ-091 | Every T-P9 row maps to an approved RL row or a requirement |

No automated tests: P9.02 changes no code.

## 7. Evidence to Record
- Every fact with URL, date and version; every review round (head, run, finding count, dispositions); the approval verbatim

## 8. Risks and Limitations
- (a) A package name already taken on nuget.org forces RL-01 to an alternative ID; the CLI's package reference, the README and the consumer tests then change in P9.05
- (b) The tenth review round with remaining findings stops the loop; the user decides

## 9. Acceptance Criteria
- [ ] Facts recorded from official sources
- [ ] Contract reviewed to 0 findings (or the user's decision on named findings) and approved by the user, recorded verbatim
- [ ] Plans reconciled; Codex rounds recorded
