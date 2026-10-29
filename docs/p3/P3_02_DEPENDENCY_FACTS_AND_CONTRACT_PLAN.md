# P3.02 – Dependency Facts and CLI Contract: Implementation Plan

**Status: planned on 2026-10-29; accepted by the user on 2026-10-29 with the phase plan ("Accept the plans"); not started: P3 starts only on a separate instruction, and the CL rows are decided in P3.02.** Refines section 3 and the P3.02 row of section 5 of [PHASE_P3_PLAN.md](../../PHASE_P3_PLAN.md).

## 1. Goal and Scope
Goal: record the facts of every candidate dependency of the CLI from official sources, decide CL-01 to CL-20 with the user, and reconcile the phase plan and the step plans P3.03 to P3.12 with the approved contract.
In scope: dependency facts (MQTT client, argument parsing, logging; for each: latest stable version, licence, target frameworks including `net10.0`, `linux-arm64` support, maintenance status, transitive dependencies and their licences); the contract record `docs/p3/P3_CLI_CONTRACT.md` with CL-01 to CL-20, options, recommendation, rejected options with reasons and blocked steps; the review loop through the automatic Codex review of the phase pull request; user approval; for CL-19 option C, the REQ-086 amendment text in REQUIREMENTS.md, AGENTS.md "Tests" and DEVELOPMENT_PLAN.md; reconciliation; the P3.02 section of `docs/p3/P3_VERIFICATION.md`.
Out of scope with owner: production code, tests and package additions → P3.03 onwards (a dependency is added in the step that first uses it).

## 2. Preconditions and Inputs
- P3.01 READY.
- Inputs: PHASE_P3_PLAN.md section 3; REQ-002, REQ-006 to REQ-011, REQ-030, REQ-060 to REQ-073, REQ-083, REQ-084, REQ-086, REQ-103, REQ-111; C-83 to C-87, C-112; SC-22; OW-14; MQ-01, MQ-02, MQ-08, MQ-13; the P3.01 extraction (consumed API, error and notice IDs, the `DeleteTemporaryFile` assessment); AGENTS.md "P0 clarifications" (latest stable versions, pinned, licence checked).
- Retrievals: the official package pages (nuget.org metadata, the project repository's licence file and release notes) of each candidate; no source code is downloaded into the repository; no broker contact.

## 3. Decisions Required
The decisions are the rows CL-01 to CL-20 of PHASE_P3_PLAN.md section 3; each is decided by the user in this step. The orchestrating agent additionally records:

| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Dependency evidence | Official package metadata and licence files / secondary sources | Only official sources, each with URL, retrieval date, checked version and licence identifier; a candidate whose licence is not MIT-compatible or that lacks a stable `net10.0`-compatible release is rejected and the alternative row applies | Orchestrator, recorded | S1 |
| MQTT protocol version | 3.1.1 / 5.0 / configurable | Decided with CL-13 against the MQ-01 facts and the chosen client's support; the payload bound of MQ-11 already holds for both | User (CL-13) | S2 |
| Coverage option | CL-19 A, B or C | Decided by the user with the P3.01 assessment of the library block; option C only together with the recorded REQ-086 amendment | User (CL-19) | S2, S4 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p3/P3_CLI_CONTRACT.md` | Create | Section 1 approval record, section 2 dependency facts with sources, section 3 decisions CL-01 to CL-20, section 4 review rounds |
| `PHASE_P3_PLAN.md` | Change | "Approved contract" paragraph after section 3; status line |
| `docs/p3/P3_03_*` to `docs/p3/P3_12_*` | Change | Reconciliation with the approved rows (names, IDs, defaults, exit codes, the coverage option) |
| `REQUIREMENTS.md`, `AGENTS.md`, `DEVELOPMENT_PLAN.md` | Change only for CL-19 option C | The approved REQ-086 amendment, dated, with the user's wording |
| `docs/p3/P3_VERIFICATION.md` | Change | P3.02 section |

## 5. Procedure
No slice changes behavior, so there is no Red step.

### S1 Dependency facts
- For each candidate (the MQTT client of CL-13, `System.CommandLine` for CL-02 option B, `Microsoft.Extensions.Logging` and `Microsoft.Extensions.Logging.Console` for CL-08): retrieve the package metadata and the licence; record version, licence, frameworks, dependencies, last release date and the URLs; for the MQTT client also its support for QoS 1 acknowledgment, TLS with custom CA, MQTT 3.1.1 and 5.0, timeouts and cancellation tokens; for `System.CommandLine` its handling of case-insensitive option names (REQ-011).

### S2 Contract draft
- Write CL-01 to CL-20 from section 3 of the phase plan, refined by S1 and the P3.01 extraction (the CL-10 list completed with every library error ID the CLI passes through); every proposed name marked (Proposed) or fixed; every default and range of CL-14 and CL-18 stated with its unit.

### S3 Review loop
- The pull request is marked ready for review (AGENTS.md "Phase implementation" step 2); the automatic Codex review runs; the pull request returns to draft while findings are fixed; each finding is verified against the requirements, the contracts and the code; confirmed findings are fixed by the orchestrating agent (the contract is documentation); rejected findings are reported with the reason; every round (head, run, finding count, each finding and its disposition) is recorded in the contract section 4; repeated until a round reports 0 findings or the tenth round, after which the remaining findings and the agent's check of each go to the user, who decides how to continue.

### S4 User approval
- The user decides every row; the approval wording and date are recorded verbatim in the contract section 1; for CL-19 option C the amendment is written into the requirement documents in the same commit and recorded with the approval.

### S5 Reconciliation
- Update the phase plan and the step plans P3.03 to P3.12 with the approved rows; the pull request returns to draft; the reconciliation commit is reviewed in the next ready-for-review round of the phase.

## 6. Tests and Test IDs
| Step-local case | T-P3 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1, S2 | T-P3-02 to T-P3-20 (inputs) | REQ-010, REQ-063, REQ-086 | The exit codes, IDs, defaults, ranges and coverage rules of the later tests come from the approved contract |

## 7. Evidence to Record
- Dependency URLs, retrieval dates, versions and licences; review rounds and findings; the user's approval wording and date; for option C the amendment commit

## 8. Risks and Limitations
- (a) A candidate's documentation can lag its release; the version and licence recorded are those of the package metadata at the retrieval date, and the pinned version is rechecked when it is added (P3.03 or P3.08)
- (b) Codex cannot retrieve the package pages in its sandbox; its fact checks are consistency checks against the recorded facts

## 9. Acceptance Criteria
- [ ] Every candidate dependency recorded from official sources
- [ ] CL-01 to CL-20 recorded; review loop to 0 findings or the user's decision on the remaining findings
- [ ] Every row approved by the user and recorded verbatim
- [ ] Phase plan and step plans reconciled; for CL-19 option C the REQ-086 amendment recorded
