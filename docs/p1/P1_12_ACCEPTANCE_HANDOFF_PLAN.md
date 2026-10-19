# P1.12 – Phase Acceptance and P2 Handoff: Execution Plan

**Status: plan only (2026-10-19). No phase acceptance or P2 implementation is claimed or authorized by this document.**

Basis: [repository rules](../../AGENTS.md), [requirements](../../REQUIREMENTS.md), [P1 detailed plan](../../PHASE_P1_PLAN.md) sections 4–8, and [development plan](../../DEVELOPMENT_PLAN.md). Prerequisites include the [P1.08 closure plan](P1_08B_COVERAGE_GATE_COMPLETION_PLAN.md), [P1.09 plan](P1_09_LOCAL_CI_PLAN.md), [P1.10 plan](P1_10_DEBIAN_ARM64_PLAN.md), and [P1.11 developer-instructions plan](P1_11_DEVELOPER_INSTRUCTIONS_PLAN.md).

## 1. Goal and Scope

Audit the package/test/coverage foundation against the binding P1 acceptance checklist, produce a traceable acceptance record, and describe the next P2 work without implementing it. A plan, checked box, existing file or historical passing test count is not proof that the current candidate passes.

In scope:

- Reconcile P1.01–P1.11 results, all T-P1-01–T-P1-24 references, artifacts and remaining limitations.
- Verify one explicitly identified candidate and comparison target through the accepted local CI procedure.
- Check package identity, dependency reproducibility, coverage rules and report provenance.
- Consume P1.11's result record, verify its part (b) completion, and decide only the named open items handed to P1.12.
- Classify platform evidence and record any permitted target-runtime deferral separately.
- Write the P1 acceptance decision and a bounded P2 handoff.

Out of scope: new application/tool behavior, new test projects, source retrieval, target-system access or installation, hosted workflow activation, commits, pushes, releases and package publication. Use existing implementation and verification procedures. Return defects to their owning work packages; do not weaken acceptance rules to close P1. P2 implementation requires a subsequent instruction.

## 2. Preconditions and Current Limitations

As checked on 2026-10-19, the P1.08 closure implementation, including the runner, is committed in `b6100e2`, with the classifier correction in `a562f63`. The user accepted P1.08 as complete, expressly accepting its missing original Red-step evidence; see the [acceptance record](P1_08_COVERAGE_EVALUATION.md#p108-acceptance-2026-10-19). `scripts/ci-local.ps1` is absent. P1.09, P1.10 and P1.11 plans exist, but their presence does not establish completion or acceptance. Re-inspect the repository when executing this plan; do not freeze today's state as a future completion claim.

- P1.01–P1.07 have historical records. Preserve those records and their dates; verify that their conclusions still apply to the candidate.
- P1.08 closure must have runner/fixture/tool-pinning/self-coverage evidence, not just the evaluator's earlier unit-test result.
- P1.09 must have a result record, reproducible clean-clone execution, same-run package consumption and fault-injection evidence.
- P1.10 must have a result record distinguishing Windows build evidence from actual Debian 13 ARM64 execution or explicitly OPEN runtime checks.
- P1.11 owns the developer-documentation audit and records it in `docs/p1/P1_11_DEVELOPER_INSTRUCTIONS.md`. Require its part (b) entries for P1.08 closure, P1.09 and P1.10, including walkthrough/check results, the packed-README check and a named handover of permitted OPEN platform items. A plan or interim part (a) delivery is insufficient. Any remaining Planned item or unverified non-platform procedure blocks completion. P1.12 consumes this evidence rather than repeating the entire audit.
- Acceptance must identify committed candidate and target SHAs. Uncommitted implementation cannot be certified by a clean-clone run. Preserve it and report NOT_READY until an authorized author supplies a committed candidate; this plan does not authorize making that commit.

## 3. Records to Produce

| Path | Action | Content |
|---|---|---|
| `docs/p1/P1_12_ACCEPTANCE.md` | New at execution | Candidate/target identities, evidence index, work-package and test-ID matrix, checklist outcomes, platform limitations and acceptance decision |
| `docs/p1/P2_HANDOFF.md` | New at execution | Accepted foundation or blocking prerequisites, P2 scope and initial test-first work, inherited constraints and deferred checks |
| `PHASE_P1_PLAN.md` | Update at execution | Evidence-backed work-package status and acceptance checklist, with links to the result and handoff |
| `README.md`, `DEVELOPMENT_PLAN.md`, `AGENTS.md` | Update as needed | Consistent current phase/scope; retain dated historical context |
| `DEPENDENCIES.md`, `docs/COMPATIBILITY.md` | Reference and align current acceptance status as needed | Consume P1.10's compatibility matrix; if absent or incomplete, return the item to P1.10. P1.12 does not create the matrix or substitute its own platform verification |
| Existing ignored `artifacts/ci/` run directories | Retain/index | Logs, test reports, package/hash, native coverage, baseline/settings, changed files and gate JSON produced by the accepted CI script |

No new acceptance tool, runtime class or interface is needed. Corrections to historical records are append-only explanations of later findings; historical inventories/checksums and `LICENSE` remain unchanged.

## 4. Acceptance Rules

The record uses **ACCEPTED** or **NOT_READY** for the P1 foundation, plus a separate platform-runtime status. It never converts a failed gate into a pass.

- **ACCEPTED:** every mandatory P1 foundation criterion has current, traceable passing evidence. Any unperformed ARM64 runtime checks are explicitly deferred only within the existing P1 scope allowance in the detailed plan section 7; their record names the remaining checks and the P1.10/P9 follow-up. This is acceptance of P1 foundations, not ARM64 runtime acceptance or release readiness.
- **NOT_READY:** a mandatory criterion fails, lacks evidence, is incomparable, or depends on uncommitted/unverified implementation. List each blocker and its owning package. A prepared P2 handoff then remains blocked.
- **Platform status:** PROVEN only for the checks actually executed on the documented Debian 13 ARM64 target; otherwise OPEN or PARTIAL, with exact omissions. Windows cross-builds, x64 and emulation cannot close ARM64 runtime checks.
- P1.10's EXPECTED-COMPARISON-FAILURE explains an exit 90; it does not satisfy P1's passing coverage-gate requirement. Resolve it through meaningful tests or a separately authorized policy change before acceptance. An OPEN platform comparison cannot substitute for mandatory Windows foundation/gate evidence.
- Use native block counters, the minimum 80%, exact ratios and the unchanged bootstrap/documentation/exact-100% rules. No baseline selection, exclusion change or documentation classification may hide a failing implementation change. Record the complete evaluated change scope; a later documentation-only run does not erase an unresolved earlier code gate failure.
- Hosted CI activation and full target release checks remain outside acceptance. A local pipeline result is never described as a successful hosted GitHub run.

### Initial bootstrap decision

**Decision (2026-10-19):** the branch's first accepted, fault-free P1.09 full-pipeline run is the genuine initial bootstrap, provided the evidence inventory confirms that no previously accepted integrated baseline exists. Earlier evaluator/runner tests and the 57/63 measurement at `b6100e2` do not by themselves establish that baseline. If an accepted baseline is discovered, use it and do not bootstrap again.

- Identify one committed seed candidate `C`. Run P1.09 with `-Commit C -TargetRef C -Bootstrap`; record its full SHA, script/tool/settings versions, package identity, complete run artifacts and initial-baseline justification in the P1.09 result record.
- Require every normal stage and the minimum coverage rule to pass; no fault-injected run establishes the seed. Preserve failed attempts. Bootstrap is not a remedy for an existing failed comparison.
- After that successful gate run, use `write-baseline` on its native report to retain `current/bootstrap-baseline.json` for commit `C`, together with collection settings and provenance. P1.09 owns producing and recording these artifacts. Until they exist, the bootstrap evidence is incomplete.
- P1.12 may consume that accepted run when its candidate is exactly `C`; it does not require a contradictory non-bootstrap comparison of `C` against itself. A later candidate must use normal comparison against a deliberate, comparable target, such as `C` remeasured by the two-pass procedure. Never repeat bootstrap for a later code candidate, ARM64 measurement, missing tooling or a lost baseline.
- Later code/package/configuration changes below a 100% baseline must strictly improve coverage through meaningful tests. Documentation-only equality remains allowed only for the complete actual diff. A README-related consumer lock update is a package change and must not be hidden by a documentation-only label. If an earlier accepted baseline already exists or a later candidate cannot meet these rules, acceptance requires the normal passing comparison; otherwise report NOT_READY.

This identifies when the existing bootstrap exception applies; it does not waive the minimum, comparability, exact-100% or subsequent improvement rules. No run is declared accepted merely by recording this decision.

## 5. Execution Procedure

1. **Inventory without modifying implementation.** Read instructions and working-tree status. Resolve candidate and target SHAs and record their relationship, branch names for context, and any uncommitted changes excluded from verification. Read all P1 records and the actual project/tool/lock configuration. Identify absent prerequisites before attempting commands; missing CI tooling means NOT_READY, not an invented substitute pipeline.
2. **Build the evidence index.** For P1.01–P1.11, record status, completion record/section, implementation revision, run path, limitations and any superseding evidence. Resolve stale summaries against files and actual results. Earlier test counts and SDK versions remain historical, not today's measurement. Track P1.03 clean-checkout closure through P1.09's current evidence.
3. **Verify the candidate.** Once P1.09 is implemented, establish or locate its accepted initial run under section 4's bootstrap decision. Consume that run if its seed SHA is exactly the acceptance candidate; otherwise invoke the accepted script for the full later candidate SHA and deliberate comparable target SHA using normal baseline rules. Record the exact invocation and script revision. Do not use bootstrap simply because an older target lacks tooling. No target-system action is implied by this local step.
4. **Audit the run, not only its exit code.** Require passing restore/build/tests/package-consumer stages and a passed gate. Confirm test projects are all represented, test failures are not skipped, and P1.09 fault cases fail at script level with preserved diagnostics. Verify resolved dependency locks; allow only the documented single consumer-package `contentHash` rewrite inside the disposable clone. Compare the packed and restored package hashes, package entries and canonical English README/license. Index the raw native coverage, current/target settings, baseline identity, exclusions and result JSON. Reject missing, stale or mixed-run artifacts.
5. **Check coverage and provenance.** Reconcile native module identity and counters with the source build and package built in that run. Retain available assembly/PDB/source mapping evidence; explicitly document fields absent from the native report and how build/package identity is established independently. Do not claim PDB identity from a module name alone. Verify production counters exclude tests/tool modules as documented, do not double-count the package assembly, and are aggregated as counts. Record checker/runner self-coverage and meta-tests separately from the product gate.
6. **Review platform evidence.** Consume the P1.10 result, compatibility matrix, target environment, SHAs, test counts, package identity and gate status. For conversion on Windows, require the tested collect/resume flow, complete target-side package-consumer evidence, exact runner/tool identities, and the deleted-work-directory/tamper checks. Keep collection host and conversion/gate host distinct. Windows-versus-ARM64 numbers are descriptive; target gating uses a target-measured baseline. If hardware is unavailable, preserve OPEN rows with the permitted deferral described in section 4.
7. **Consume the P1.11 handover.** Read `P1_11_DEVELOPER_INSTRUCTIONS.md` and verify that part (b) is recorded for P1.08 closure, P1.09 and P1.10. Confirm its evidence covers English-only rules; inline/block comments, XML documentation comments, and comments in scripts/configuration examples; current status and commands; versions/package sources; links; the 84 requirement IDs/order; license/inventory preservation; and the package-readme check. Reference those results without repeating the full documentation audit. Audit only named OPEN items handed over and changes after the recorded P1.11 revision. Decide each item's acceptance impact or return it to its owner. Missing P1.11 checks or remaining Planned items return to P1.11 and block its completion; P1.12 must not invent completion evidence.
8. **Write the decision and handoff.** Populate every checklist item with evidence or a specific blocker; never mark all boxes by inference. Write the P1.12 result and P2 handoff, then align current status documents. If a documentation correction changes the evaluated revision, identify the tested implementation SHA and the later documentation revision separately. A changed packaged README requires a new pack/content check for the final candidate. Do not claim clean-clone verification of uncommitted documentation. Any implementation correction returns to its owner and requires appropriate re-verification before the decision changes.

Do not rerun historical Red failures by breaking the current code. Use preserved TDD evidence; where it is missing, identify the gap and the responsible package. Documentation-only acceptance work does not require artificial TDD tests.

## 6. Test-ID Evidence Matrix

The final record contains a row for every binding ID below. Link actual test names, result/run files and revisions; the mapping itself is not a test pass.

| ID | Evidence to audit |
|---|---|
| T-P1-01 | Meaningful original Red→Green→Refactor evidence and current behavior tests |
| T-P1-02 | Invalid endpoint rejection with English diagnostic and no retrieval |
| T-P1-03 | Distinct endpoint mapping and duplicate-name rejection |
| T-P1-04 | Interfaces, constructor injection and dependency boundaries |
| T-P1-05 | Real DI resolution plus missing/partial-registration failures |
| T-P1-06 | TestInitialize and TestCleanup/VerifyAll discipline |
| T-P1-07 | Offline tests without production HTTP/MQTT/device access |
| T-P1-08 | Clean-clone locked restore, reviewed consumer hash exception and drift detection |
| T-P1-09 | Exactly 80% passes; lower coverage fails; actual candidate minimum passes |
| T-P1-10 | Code equality below 100% fails; strict improvement passes |
| T-P1-11 | Native block metric required; Cobertura substitution rejected |
| T-P1-12 | Rounded 100% cannot replace exact 100% |
| T-P1-13 | Missing/malformed reports fail with diagnostics |
| T-P1-14 | Genuine bootstrap behavior and later missing-baseline failures |
| T-P1-15 | Expected baseline commit checked; stale/wrong commit rejected |
| T-P1-16 | Tool/settings/exclusion/format comparability and mismatch failures |
| T-P1-17 | Counter aggregation and duplicate production-module rejection |
| T-P1-18 | Exact-100% and documentation equality exceptions; decreases fail |
| T-P1-19 | Script-level test/coverage/gate failure propagation and retained diagnostics |
| T-P1-20 | Separate build/restore and actual Debian ARM64 runtime evidence or explicit deferral |
| T-P1-21 | Isolated consumer uses the same-run package revision/hash |
| T-P1-22 | Package boundary, allowed contents and no consumer ProjectReference |
| T-P1-23 | Full changed-file scope classified; code/build/package/coverage/CI receive no docs bypass |
| T-P1-24 | Native counters, module/build/package/PDB/source provenance with explicit limitations |

For each failed or unperformed row, specify whether it blocks P1 foundation acceptance or belongs to the explicitly allowed platform-runtime deferral. Do not introduce new test IDs or silently redefine the existing ones.

## 7. P2 Handoff Content

The handoff identifies the accepted foundation SHA, package identity, SDK/runtime/tool versions, canonical build/test/gate entry points, the baseline policy, and inherited limitations. A NOT_READY foundation blocks implementation handoff; a list of proposed P2 tasks can still be prepared.

P2 owns public export entities, UTC/unit/rounding logic, real JSON/XML serializer contracts and reproducible output XSD generation. Reintroduce the previously removed entity scaffolding one behavior at a time through tests; do not restore all old types as presumed accepted contracts.

Initial sequence:

1. Select the first required export contract from REQUIREMENTS, define exact fields/nullability/units/time semantics and its requirement traceability before naming final types.
2. Write a meaningful failing serializer-contract test for that minimal typed entity graph. Specify present/missing values and true zeros, identifiers including leading zeros, UTC formatting without suffix, and escaping/special characters as applicable to that graph.
3. Implement minimally using `[Serializable]` on every custom export entity, real `System.Text.Json.JsonSerializer` and `System.Xml.Serialization.XmlSerializer`, and equivalent normalized meaning. Executable serializer services use interfaces and constructor injection; pure data objects need no artificial interfaces.
4. Extend tests incrementally for per-value measurement times, estimated-date markers, observation/forecast separation, source/quality/period metadata, pressure conversion/reference and midpoint rounding away from zero. Preserve issue/validity times for forecasts.
5. Define a reproducible output-XSD generation/versioning procedure, then validate real XML exports against generated schemas. Output schemas remain separate from the DWD MOSMIX source XSD.

Keep P2 offline. No parser retrieval, broker transport or CLI is introduced. Preserve phase order **P0 → P1 → P2 → P4 → P5 → P6 → P7 → P8 → P3 → P9**. Plan P2's package-consumer and coverage evidence with the existing CI foundation; implementation remains a separate request.

## 8. Acceptance Record Fields

- Review date, reviewer, candidate/target full SHAs, script revision, working-tree exclusions and the exact command/run ID.
- Actual SDK and runtime versions separately; collector/converter versions, host/RID, settings/exclusion hashes and baseline metadata.
- Stage exit codes, test counts per project, warning count, package SHA-512/content list, native block counts and every gate verdict.
- P1.01–P1.11 evidence index and all 24 test-ID rows; historical versus current evidence clearly labeled.
- P1.11 result revision, part (b) entries and check results; named handover items and their dispositions; any checks of subsequent documentation changes. Include the initial bootstrap decision/run/seed artifacts or the existing accepted baseline used instead.
- Platform-runtime status and omitted checks, with owner/work package and follow-up acceptance condition.
- ACCEPTED or NOT_READY, rationale, blockers and links to the P2 handoff. No blank evidence cells presented as completion.

## 9. Completion Checklist for P1.12

- [ ] A final candidate and deliberate target are identified; uncommitted implementation is not certified.
- [ ] Every mandatory P1 foundation item has current passing evidence, or the decision is NOT_READY.
- [ ] The actual package and isolated consumer are traceable to the candidate; clean-clone reproducibility is demonstrated.
- [ ] Native block minimum/comparison rules and script-level failures are proven; expected gate failure is never labeled PASSED.
- [ ] Every T-P1-01–T-P1-24 row has an outcome and evidence/limitation classification.
- [ ] P1.11's result and part (b) completion are verified; only named handover items and subsequent changes are audited here.
- [ ] The accepted initial bootstrap run and seed artifacts, or the existing accepted baseline, are identified; later candidates use normal comparison.
- [ ] ARM64 runtime proof is separated from cross-build evidence and permitted deferred checks.
- [ ] `P1_12_ACCEPTANCE.md`, `P2_HANDOFF.md` and current status documents agree with the evidence.
- [ ] P2 scope, prerequisites and inherited limitations are clear; no P2 implementation or publication is claimed.

Preparing this plan satisfies none of these execution checkboxes by itself.
