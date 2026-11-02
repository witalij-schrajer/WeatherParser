# P4.02 – Source Facts and Selection Contract: Implementation Plan

**Status: complete (2026-10-21): S1 to S5 — selection contract approved by the user on 2026-10-21 ([record](../../p4/P4_SELECTION_CONTRACT.md)); S6 — phase plan and step plans P4.03 to P4.12 reconciled with it ([verification](../../p4/P4_VERIFICATION.md) section 2); committed in `5c7f1f2`, hosted CI run 39676704275 green.** Original planning note: plan only on 2026-10-20. Refines section 3 and the P4.02 row of section 5 of [PHASE_P4_PLAN.md](../PHASE_P4_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief; reviewed read-only by the OpenAI Codex CLI (rounds: 5, 0); every finding was verified by the orchestrating agent and fixed by qwen unless stated otherwise; a cross-plan Codex review of all P4 plans ended with 0 findings (rounds: 5, 2, 0). Later: SC rows for list source literals and the time source (cross-plan review).

## 1. Goal and Scope
Goal: establish the verified source facts for the CDC station catalog, CDC availability metadata and the MOSMIX location catalog from official DWD sources, and record every selection decision of phase-plan section 3 in a new contract record `docs/p4/P4_SELECTION_CONTRACT.md` with decision IDs `SC-01` onward (Proposed prefix), reviewed by Codex and approved by the user, so that P4.03–P4.10 can start their Red tests.

In scope:
- Source research (official DWD documentation pages, published description files, the DWD procedure description for MOSMIX, the MOSMIX station catalog as published) with URL, check date and, for each downloaded reference file, size and SHA-256 recorded (the files themselves are not committed, REQ-083); source conditions per REQ-092 (access, attribution, retrieval frequency, licence/terms of use) for every endpoint; decisions for all twelve section 3 rows; mapping of C-21 (`available_measurements` REQ-052 names) to DWD products and of C-24 (DWD element names) to the MOSMIX availability evidence; synthetic fixture design rules (derived from the verified formats, small, no copied third-party rows); dependency check for any new package (version, licence, .NET 10 compatibility) without adding it; Codex contract review loop (up to 10 rounds); user approval.

Out of scope with owner:
- Code and tests → P4.03–P4.10
- Adding packages to `Directory.Packages.props` → the step that first uses them
- Live retrieval tests → P4.11
- MOSMIX forecast content (KML/KMZ, `pointforecast_dwd_extension_V1_0.xsd`) → P7
- DWD observation data parsing → P5
- Any change to export contract v1 → only via a recorded additive decision under C-03/C-105 in `docs/p2/P2_EXPORT_CONTRACT.md` (no such change is planned).

## 2. Preconditions and Inputs
- P4.01 outcome READY; integration target recorded; network access to official DWD pages for reading (manual research, not product code).
- The orchestrating agent performs the research (qwen cannot access sources) and writes the facts list.
- qwen drafts the record prose from that list.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
| --- | --- | --- | --- | --- |
| Contract record and ID scheme | new `docs/p4/P4_SELECTION_CONTRACT.md` with `SC-xx` / extend `docs/p2/P2_EXPORT_CONTRACT.md` | New record: selection behavior is not part of the frozen export contract; export changes, if any, stay in the P2 record | Orchestrator, recorded | S3 |
| Which decisions need user approval | all `SC-xx` / only public-behavior ones | All `SC-xx`, in one approval after the Codex loop (as in P2) | User | S5 |
| Use of a downloaded reference file during research | read-only inspection with hash recorded, not committed / committed copy | Not committed (REQ-083); synthetic fixtures are written from the verified format | Orchestrator, recorded | S1–S2 |

## 4. Artifacts
| Path | Action | Content |
| --- | --- | --- |
| `docs/p4/P4_SELECTION_CONTRACT.md` | Create | Status, approval table, source register (URL, check date, size, SHA-256, conditions), decision register `SC-01`… (one row per phase-plan section 3 decision, at least: CDC catalog source and format; CDC availability derivation; MOSMIX catalog source and format; MOSMIX per-product availability; geodetic method and tolerance; coordinate grammar; logging of notices; retrieval boundary; catalog failure semantics; public selection API; service registration under C-107/C-108; list metadata and time source (the `source` literal of each list root under C-18; the time abstraction, who registers it, when it is sampled during a selection, and the whole-second conversion for `ExportTimestamp.FromUtc`); live verification), fixture rules, open items, review rounds |
| `docs/p4/P4_VERIFICATION.md` | Append | P4.02 section with research log, Codex rounds with finding counts and dispositions, approval wording |
| `PHASE_P4_PLAN.md` | Update | Where approved decision changes scope; Codex reviews these changes read-only |

## 5. Procedure
1. **S1 CDC source facts**
   - **No Red:** no behavior change.
   - Identify the official CDC station description file(s) and the product metadata usable for availability (REQ-024).
   - Record the exact documented format (columns, widths or delimiters, encoding, date fields, ID width), URLs, check date, size/SHA-256 of inspected files, and REQ-092 conditions.
   - Output: source register rows and the facts for SC rows on CDC catalog and availability.
   - Roles: the orchestrating agent performs the research (qwen cannot access sources) and writes the facts list; qwen drafts the record prose from that list.

2. **S2 MOSMIX source facts**
   - **No Red:** no behavior change.
   - Identify the official MOSMIX location catalog and the evidence for per-product (`MOSMIX_S`, `MOSMIX_L`) availability and elements.
   - Record the coordinate/altitude format (for example degrees and minutes), ID format (alphanumeric, leading zeros), missing-altitude representation, encoding, URLs, conditions.
   - Same output and roles as S1.

3. **S3 Decision drafting**
   - **No Red:** no behavior change.
   - qwen drafts `docs/p4/P4_SELECTION_CONTRACT.md` with every SC row as Proposed, from the orchestrator's brief containing the S1/S2 facts, the phase-plan section 3 rows, C-03, C-21, C-23, C-24, C-25, C-26, C-27, C-104, C-105, C-107, C-108 and the existing `ExportTimestamp`, `SearchParameters`, `IListEntryOrdering` declarations.
   - Geodetic method: name a published method and published reference distances for T-P4-04; tolerance stated in metres or kilometres.
   - Coordinate grammar: list accepted symbols for degree, minute (prime `′` and apostrophe `'`) and second (double prime `″` and quotation mark `"`), whitespace and hemisphere letters, sign/hemisphere conflicts, and that parsing uses the invariant culture.
   - Logging: package, version and licence check result.
   - Retrieval: HTTP abstraction, timeout and size bounds, no library retries unless decided, cancellation, error types with English messages.
   - Failure semantics: fail versus skip, all-invalid input, missing availability metadata, empty selection versus failure.
   - Public API: interfaces and result types.
   - List metadata and time source: `source` literals for `CdcStationList` and `MosmixLocationList`; time abstraction (for example the BCL `TimeProvider` abstract class or a library interface), registration responsibility, sampling point and whole-second conversion.
   - Registration: compliance with C-107/C-108 or a recorded amendment.
   - Output: complete draft.
   - Roles: qwen drafts; the orchestrating agent reviews every row against REQUIREMENTS.md, AGENTS.md and the facts and sends defects back to qwen.

4. **S4 Codex contract loop**
   - **No Red:** no behavior change.
   - Codex reviews the draft read-only (`codex exec --sandbox read-only --ephemeral -o <file outside the repository> "<brief>"`).
   - The orchestrating agent verifies each finding.
   - qwen fixes confirmed findings.
   - Repeat until 0 findings or round 10, then report remaining findings with the orchestrator's check to the user.
   - If Codex is unavailable or fails, record the failed attempt separately from completed rounds, report the missing review to the user, and the user decides whether to repeat it before approval (AGENTS.md); a missing review does not block the step.
   - Output: round log with finding counts in the contract record.
   - Roles: Codex reviews read-only; orchestrator verifies findings; qwen fixes.

5. **S5 User approval**
   - **No Red:** no behavior change.
   - Present the SC rows and open choices.
   - Record the user's exact wording and date in the approval table.
   - Mark rows Approved.
   - No later step starts its Red test before its SC rows are approved.
   - Roles: orchestrator presents; user approves.

6. **S6 Plan reconciliation**
   - **No Red:** no behavior change.
   - Update `PHASE_P4_PLAN.md` and the affected step plans where approved decisions change scope.
   - Codex reviews these changes read-only.
   - Checkpoint commit when the user requests it.
   - Roles: qwen drafts the plan revisions from the orchestrator's brief and fixes review findings; the orchestrating agent verifies and applies them, runs the Codex review and records authorship.

## 6. Tests and Test IDs
| Step-local case | T-P4 ID | Requirement IDs | Behavior and boundary |
| --- | --- | --- | --- |
| S1–S2 | T-P4-18 (preparation) | REQ-092, REQ-083 | Source conditions recorded; reference files not committed; live check only after conditions are clear |
| S3 | T-P4-01 to T-P4-14 (preparation) | REQ-014, REQ-020–024, REQ-100–102 | Each T-P4 row has the SC decisions it depends on; no test is written in this step |

## 7. Evidence to Record
- Source register with URL, check date, size and SHA-256 of each inspected file, and conditions (REQ-092).
- Codex rounds with output file names, finding counts and dispositions.
- User approval wording and date.
- Authorship (orchestrator research, qwen drafting).

## 8. Risks and Limitations
- (a) The DWD documentation may not state per-station availability. For MOSMIX the decision records `unknown` (C-23) or a documented derivation, never an assumption (REQ-102). For CDC, `available_measurements` holds only REQ-052 names (C-21) and has no `unknown` value: missing evidence is resolved through the approved catalog failure semantics, never by an invented vocabulary item or by reporting an empty collection as a successful derivation (REQ-024).
- (b) Catalog formats may change after the check date — the check date and hash are recorded and P4.11 compares a live check.
- (c) A needed package may lack a suitable licence — record the conflict and choose an alternative, do not add it silently.
- (d) qwen may invent formats — every source fact comes from the orchestrator's verified list and is checked line by line.

## 9. Acceptance Criteria
- [x] Every phase-plan section 3 decision has an SC row with value and source.
- [x] Every source has URL, date, conditions.
- [x] Every decision that blocks P4.03–P4.10 is resolved and approved; any remaining OPEN item is non-blocking and has a named owner, and the steps it would affect are recorded as blocked.
- [x] Codex loop ended with 0 findings, or the user decided at round 10, or the user decided after a recorded Codex failure.
- [x] User approval recorded.
- [x] No code or package added.
