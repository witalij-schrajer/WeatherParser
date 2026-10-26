# P6.11 – Acceptance and P7 Handoff: Implementation Plan

**Status: A1 to A6, A8 and A9 executed on 2026-10-26 ([record](P6_11_ACCEPTANCE.md)); GitHub recorded A9 at 2026-10-26T01:48:01Z on head `a40b214ce4a39201f0b9151cd2dc12ffc3e12adc`, and hosted CI run [40255978200](https://github.com/witalij-schrajer/WeatherParser/actions/runs/40255978200) passed on that head; A7 (the user's acceptance), A10 (review) and A11 (merge) pending.** Refines the P6.11 row of section 5 and section 9 of [PHASE_P6_PLAN.md](../../PHASE_P6_PLAN.md).

## 1. Goal and Scope
Goal: audit the P6 evidence, record the limitations and the source-condition status, obtain the user's acceptance, update the status documents, write the P7 handoff, and take the pull request through review to the user's merge.
In scope: actions A1 preconditions; A2 evidence matrix T-P6-01 to T-P6-19; A3 contract summary (OW rows; export contract unchanged or additive decisions); A4 limitations and open items with owners, including the inherited items mapped in P6.01 and the source-condition status; A5 audit decision; A6 P7 handoff `docs/p6/P7_HANDOFF.md`; A7 user acceptance; A8 status updates; A9 ready for review; A10 review with the new-candidate rule; A11 merge by the user and the post-merge records.
Out of scope with owner: P7 execution → only on the user's separate instruction.

## 2. Preconditions and Inputs
- P6.10 outcome READY.
- Required user decisions: acceptance (A7); the wording on production acceptance if the source conditions are still unclarified (A4, A7); the home of the post-merge records (A11); the merge.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Production acceptance | Accepted for production use (only with recorded clarified conditions) / library accepted, production use open until the conditions are clarified | The second, unless the user has recorded the operator's consent (REQ-092, DEVELOPMENT_PLAN.md P6 acceptance) | User | A7 |
| Home of the post-merge records | Separate documentation pull request / first commit of the P7 branch | Ask at A11 (precedent: separate pull requests after P4 and P5) | User | A11 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p6/P6_11_ACCEPTANCE.md` | Create | Review identity, evidence matrix, contract summary, limitations, audit decision, user decision, A9–A11 |
| `docs/p6/P7_HANDOFF.md` | Create | Accepted foundation, what P6 provides, P7 responsibilities, reuse obligations, open items |
| `README.md`, `AGENTS.md`, `DEVELOPMENT_PLAN.md`, `PHASE_P1_PLAN.md`, `PHASE_P6_PLAN.md`, `docs/p6/P6_OSTHESSEN_CONTRACT.md`, `docs/p6/P6_VERIFICATION.md` | Change | Status passages |

## 5. Procedure
No action changes behavior. The orchestrating agent writes all records; Codex reviews A2 to A6 and A8; the user decides A7 and A11.

- **A1**: candidate and base verified against the P6.10 record and `gh pr view`.
- **A2**: every T-P6 row with its evidence; OPEN only with a user-approved amendment; the live row separately.
- **A3**: OW-01 to OW-16 with their implementation; export contract unchanged or the additive decisions listed.
- **A4**: limitations with owners: supported page revision (REQ-045), stale displays, precipitation not exported, consumer-supplied coordinates, the source-condition status, the registration consequence for consumers, and the inherited items (builder duplication guard, XML cancellation, hosted ARM64 evidence, package reproducibility, DI under load).
- **A5**: audit decision ACCEPTED or NOT ACCEPTED with the gaps.
- **A6**: P7 handoff.
- **A7**: the user's acceptance wording and date recorded.
- **A8**: status passages ("P6 accepted; complete only once the user merges the pull request"); README package check; `ci-local.ps1` on the committed revision.
- **A9**: pull request marked ready for review on the user's instruction; CI run recorded.
- **A10**: review findings verified and fixed test-first; a production change makes a new candidate and reruns P6.10 S1 to S3.
- **A11**: the user merges; the merge SHA, the P7 handoff integration target and "P6 complete" recorded where the user decides.

## 6. Tests and Test IDs
No new automated test. A2 traces T-P6-01 to T-P6-19.

## 7. Evidence to Record
- Matrix with evidence links; audit decision; user decisions verbatim; Codex rounds; CI runs; merge SHA

## 8. Risks and Limitations
- (a) Review findings change production code after acceptance — the new-candidate rule applies and A7 is repeated if evidence changed materially
- (b) Source conditions still unclarified at acceptance — recorded as open for production; P8 and P3 must not redistribute the values until clarified

## 9. Acceptance Criteria
- [ ] Preconditions recorded; candidate and base verified
- [ ] Evidence matrix complete; live row recorded separately
- [ ] Contract summary and limitations complete, including the source-condition status
- [ ] Audit decision recorded; P7 handoff written and Codex-reviewed
- [ ] User acceptance recorded with date and wording
- [ ] Status documents consistent; package check and committed-revision run passed
- [ ] Pull request ready for review; review findings resolved; new-candidate rule applied where production code changed
- [ ] Merged into `develop` by the user; post-merge records in the chosen home
