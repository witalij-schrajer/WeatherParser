# P9.08 – Operating Guides and Release Documentation: Implementation Plan

**Status: planned on 2026-10-31 on the user's instruction ("Generate the step plans for P9"); reviewed through the automatic Codex review of pull request #17 to 0 findings; accepted by the user on 2026-10-31 ("Accept the plans"); accepting the plans neither starts P9 nor approves any RL row; P9 starts only on a separate instruction.** Refines the P9.08 row of section 5 of [PHASE_P9_PLAN.md](../PHASE_P9_PLAN.md).

## 1. Goal and Scope
Goal: English operating and release documentation that lets an operator deploy, schedule, monitor and integrate the CLI and lets a developer consume the package, with source conditions, OW-14 and every known limitation stated.
In scope: the operating guide (RL-12, Proposed `docs/OPERATIONS.md`) completed from the P9.07 draft: configuration and secrets, scheduling with cron and with the n8n Execute Command node (examples, no installation), exit codes for callers, log handling, request volume per source with recommended intervals bounded by the product cycles (MOSMIX_S hourly, MOSMIX_L at 03, 09, 15 and 21 UTC) and the source conditions, broker set-up (dedicated credentials, TLS with validation, scoped topic permissions, retained-state handling), estimated dates and freshness, limitations; README release sections (package installation from the feed as planned until P9.12, planned versus implemented, REQ-090); `CHANGELOG.md` and release notes (RL-17); source conditions and OW-14 (RL-16); known limitations (RL-18); `docs/COMPATIBILITY.md` prepared for the P9.10 evidence.
Out of scope with owner: claims of publication or target success → only after P9.10 and P9.12 evidence; every example is marked with its verification status until then.

## 2. Preconditions and Inputs
- P9.07 complete; P9.02 approved RL-12 and RL-16 to RL-18.
- Inputs: REQ-007, REQ-008, REQ-061 to REQ-063, REQ-090, REQ-092, REQ-093; OW-14; the README sections of the library and the example CLI; the limitations of the P3.12 and P8.10 records; the CONTRIBUTING.md "Status values".

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Verification status of examples | Unmarked / marked with the CONTRIBUTING.md status values | Every command and configuration example carries its status (for example verified on the target in P9.10, or not verified) and is verified in P9.10 S5 | Orchestrator, recorded | S1 |
| README documentation tests | Extend the existing `ReadmeCliSectionTests` / no test | Extend only where an existing test covers the section (README is packed; CONTRIBUTING.md section 4 check after every change) | Orchestrator, recorded | S2 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/OPERATIONS.md` (Proposed) | Change | Complete operating guide |
| `README.md` | Change | Release sections, link to the guide and the changelog, limitations summary |
| `CHANGELOG.md` (Proposed) | Create | First version section, known limitations, compatibility |
| `docs/COMPATIBILITY.md` | Change | Release rows prepared (evidence filled in P9.10) |
| `docs/p9/P9_VERIFICATION.md` | Change | P9.08 section |

## 5. Procedure
Documentation by the orchestrating agent; a test change for the README by qwen.

### S1 Operating guide
Write the guide; every example uses placeholders (`example.invalid`, invented IDs), no private path, address or credential; request-volume guidance cites the source conditions as recorded in the README and P9.02.

### S2 README, changelog and release notes
Update the README (the packed readme: run the local package check of CONTRIBUTING.md section 4 and the package-content test of P9.05); write `CHANGELOG.md`; state OW-14, DWD attribution and the data licences separate from the MIT code licence.

### S3 Consistency and review
Check English-only text, local links, the planned versus implemented markers and the AGENTS.md consistency table; per-step Codex review; commit and push.

## 6. Tests and Test IDs
| Step-local case | T-P9 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P9-09 (prepared; executed in P9.10) | REQ-007, REQ-008 | Guide examples listed for target verification |
| S2, S3 | T-P9-15, T-P9-02 | REQ-090, REQ-092, REQ-093 | Documentation complete and consistent; packed README check |

## 7. Evidence to Record
- Link check, package readme check, list of examples with their verification status, authorship, Codex round

## 8. Risks and Limitations
- (a) Request-volume guidance cannot guarantee source conditions; it states them and leaves the schedule to the operator (REQ-008)
- (b) The README is packed: a later README change after the candidate is a build-input change (RL-06 promotion check)

## 9. Acceptance Criteria
- [ ] Guide, README, changelog and compatibility rows written; OW-14 and source conditions stated
- [ ] Package readme check passed; links valid; English only
- [ ] Codex 0 findings or reported

## 10. Reconciliation with the approved contract

The release contract [P9_RELEASE_CONTRACT.md](../../p9/P9_RELEASE_CONTRACT.md) was approved by the user on 2026-10-31 ("Approve all"; RL-19 open). Where this plan and the contract differ, the contract wins. Changes for this step:
- RL-12 A `docs/OPERATIONS.md`; RL-17 A `CHANGELOG.md`; RL-16 with the facts F14 (CC BY 4.0, attribution, 7-day IP storage of the open-data server) and F15 (OsthessenWetter imprint, OW-14); RL-18 A including the broker-disconnect and MQTT 3.1.1 limitations; the release documentation names runtime 10.0.14 or newer, the open SDK update (RL-09 A) and, while RL-19 A applies, that the repository is private.
