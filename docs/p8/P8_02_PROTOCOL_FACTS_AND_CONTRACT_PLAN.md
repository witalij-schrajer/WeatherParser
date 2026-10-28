# P8.02 – Protocol Facts and MQTT Message Contract: Implementation Plan

**Status: planned on 2026-10-28; accepted by the user on 2026-10-28 with the phase plan; not started.** Refines section 3 and the P8.02 row of section 5 of [PHASE_P8_PLAN.md](../../PHASE_P8_PLAN.md).

## 1. Goal and Scope
Goal: record the MQTT protocol facts the library must respect, decide MQ-01 to MQ-15 with the user, and reconcile the phase plan and the step plans P8.03 to P8.10 with the approved contract.
In scope: retrieval and record of the official MQTT 3.1.1 and 5.0 specifications (URL, date, size, SHA-256; not committed); the contract record `docs/p8/P8_MQTT_CONTRACT.md` with MQ-01 to MQ-15, options, recommendation and rationale; the Codex review loop of the contract draft (up to 10 rounds, AGENTS.md "Contract decisions"); user approval; reconciliation; the P8.02 section of `docs/p8/P8_VERIFICATION.md`.
Out of scope with owner: production code and tests → P8.03 onwards; broker behavior, TLS and authentication → P3.

## 2. Preconditions and Inputs
- P8.01 READY.
- Inputs: REQ-015, REQ-016, REQ-050, C-55 (duplicate handling left to P8), REQ-061, REQ-062, REQ-069, REQ-075; C-01, C-11, C-15, C-50, C-68, C-88, C-90, C-92, C-107 to C-109; OW-03, OW-14; MF-03; the export entities and `IJsonExportSerializer`; the OASIS specifications MQTT Version 3.1.1 (OASIS Standard, 2014) and MQTT Version 5.0 (OASIS Standard, 2019).
- One retrieval per specification document, authorized by this step plan; no broker contact.

## 3. Decisions Required
The decisions are the rows MQ-01 to MQ-15 of PHASE_P8_PLAN.md section 3; each is decided by the user in this step. The contract states, for every row, the options, the recommendation, the rejected options with the reason and the step it blocks. The orchestrating agent additionally records:

| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Specification version basis | 3.1.1 only / 5.0 only / the rules both share | The rules both versions share; differences recorded with section references | User (MQ-01) | S2 |
| Payload bound arithmetic | The protocol remaining-length maximum minus the encoded topic (2 + topic bytes), the packet identifier of QoS 1 and 2 (2 bytes) and, for MQTT 5.0, at least the 1-byte empty property length / a round lower bound | The exact arithmetic recorded with the section references, the default `MaxPayloadBytes` derived per message and the configurable value capped by it | User (MQ-11) | S2 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p8/P8_MQTT_CONTRACT.md` | Create | Section 1 approval record, section 2 protocol facts with sources, section 3 decisions MQ-01 to MQ-15, section 4 review rounds |
| `PHASE_P8_PLAN.md` | Change | "Approved contract" paragraph after section 3; status line |
| `docs/p8/P8_03_*` to `docs/p8/P8_10_*` | Change | Reconciliation with the approved rows (names, IDs, defaults) |
| `docs/p8/P8_VERIFICATION.md` | Change | P8.02 section |

## 5. Procedure
No slice changes behavior, so there is no Red step.

### S1 Protocol facts
- Retrieve both specifications from docs.oasis-open.org into a scratch directory outside the repository; record URL, date, size and SHA-256; extract with section references: UTF-8 string encoding and its prohibited code points, topic name length, wildcard characters, the `$` prefix rule, empty levels, the remaining-length maximum, the PUBLISH header fields per QoS, the retain flag semantics.

### S2 Contract draft
- Write MQ-01 to MQ-15 from section 3 of the phase plan, refined by S1; every number traceable to a specification section; every planning name marked (Proposed) or fixed.

### S3 Codex review loop
- Since the user decision dated 2026-10-28 (AGENTS.md "Phase implementation"): the loop runs through the automatic Codex review of pull request #14 (marked ready for review; draft while fixing; repeated until a round reports 0 findings), and each round is recorded in the contract section 6; the user is asked for the approval only after the 0-finding round; after the approval the pull request returns to draft for P8.03 onwards.
- Five-angle read-only review of the draft; each finding verified against the specifications, the requirements and the code; confirmed findings fixed by the orchestrating agent; rounds and counts recorded in the contract until 0 findings or the tenth round (then the remaining findings go to the user with the agent's assessment).

### S4 User approval
- The user decides every row; the approval wording and date are recorded verbatim in the contract section 1.

### S5 Reconciliation
- Update the phase plan and the step plans P8.03 to P8.10 with the approved rows; Codex review of the reconciliation until 0 findings; commit and push on the user's instruction.

## 6. Tests and Test IDs
| Step-local case | T-P8 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1, S2 | T-P8-01 to T-P8-08 (inputs) | REQ-015, REQ-062 | The boundary values of the later tests come from the recorded facts |

## 7. Evidence to Record
- Specification URLs, retrieval dates, sizes and SHA-256; Codex rounds and findings; the user's approval wording and date

## 8. Risks and Limitations
- (a) Brokers may enforce lower limits than the specification (for example a configured maximum packet size); the library bound is the protocol bound and the consumer's configurable value, broker limits stay with the transport (P3)
- (b) Codex cannot retrieve the specifications in its sandbox; its fact checks are consistency checks against the recorded excerpts

## 9. Acceptance Criteria
- [ ] Protocol facts recorded with sources and hashes
- [ ] MQ-01 to MQ-15 approved by the user, wording and date recorded
- [ ] Phase plan and step plans reconciled; Codex 0 findings or reported
