# P9.04 – Coverage Gate Amendment: Implementation Plan

**Status: planned on 2026-10-31 on the user's instruction ("Generate the step plans for P9"); reviewed through the automatic Codex review of pull request #17 to 0 findings; accepted by the user on 2026-10-31 ("Accept the plans"); accepting the plans neither starts P9 nor approves any RL row; P9 starts only on a separate instruction. Conditional: applies only if the user approves RL-11 option A in P9.02; with option B or C the step is recorded as not applicable and the approved rule is applied per pull request.** Refines the P9.04 row of section 5 of [PHASE_P9_PLAN.md](../../PHASE_P9_PLAN.md).

## 1. Goal and Scope
Goal: remove the CL-19 C asymmetry: a pull request that leaves the example CLI and its build inputs unchanged may retain equal CLI coverage, as the library gate already allows for its own gated inputs.
In scope: the requirement amendment (REQUIREMENTS.md REQ-086 amendment text, AGENTS.md "Tests", DEVELOPMENT_PLAN.md) first; then the CLI gate's gated-input list in the coverage tooling (`tools/WeatherParser.Coverage`, `tools/WeatherParser.Coverage.Runner`, the gate invocation in `scripts/ci-local.ps1` and `scripts/ci-local.Cli.ps1`) and their tests (`tests/WeatherParser.Coverage.Tests`, `tests/WeatherParser.Coverage.Runner.Tests`); CONTRIBUTING.md sections 5 and 6.
Out of scope with owner: the library gate rule → unchanged; a change of the block metric or the 80 % minimum → not planned.

## 2. Preconditions and Inputs
- P9.02 approved RL-11 A with the exact list of CLI gated inputs.
- Inputs: REQ-086 and its amendment; CL-19 C; the existing generic gated-input classification (`PullRequestClassifier.TouchesGatedInputs`, `PullRequestCategory.GatedInputsUnchanged`, `CoverageGateGatedInputsTests`, `GatedInputsClassifierTests`).

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| CLI gated inputs | As approved in RL-11 | Proposed for the contract: everything under `examples/`, `tests/WeatherParser.Cli.Tests/`, `tests/WeatherParser.Cli.SignalHost/`, and the root build inputs already listed for the library gate (`Directory.Build.props`, `Directory.Packages.props`, `global.json`, `NuGet.Config`, `.editorconfig`, `coverage.runsettings`), plus `src/` because the CLI consumes the same-run package | User (RL-11) | S2 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `REQUIREMENTS.md`, `AGENTS.md` "Tests", `DEVELOPMENT_PLAN.md` | Change | The amendment text with date and the user's wording |
| `tools/WeatherParser.Coverage*/…`, `scripts/ci-local.ps1`, `scripts/ci-local.Cli.ps1` | Change | The CLI gate's gated-input list and its use |
| `tests/WeatherParser.Coverage.Tests/…`, `tests/WeatherParser.Coverage.Runner.Tests/…` | Create or change | Tests of the CLI gated-input rule (in a new test file with its own fixture) |
| `CONTRIBUTING.md` sections 5 and 6 | Change | The CLI gate rule |
| `docs/p9/P9_VERIFICATION.md` | Change | P9.04 section |

## 5. Procedure
Code, tests and scripts by qwen from briefs with the real declarations (AGENTS.md "qwen invocation"); documentation by the orchestrating agent; every diff read in full.

### S1 Amendment
Record the amendment in REQUIREMENTS.md, AGENTS.md and DEVELOPMENT_PLAN.md before any code change; commit.

### S2 Gate rule
**Red**: `CliGate_CliInputsUnchanged_EqualCoverageAccepted`, `CliGate_CliInputsUnchanged_DecreaseRejected`, `CliGate_ExampleFileChanged_StrictImprovementRequired`, `CliGate_SrcChanged_StrictImprovementRequired`, `CliGate_DocumentationOnly_EqualCoverageAccepted` (unchanged rule), `CliGate_Bootstrap_TargetWithoutCliAssembly_MinimumOnly` (unchanged rule), `LibraryGate_Unchanged_ByCliRuleChange` (regression guard), `CiLocal_PassesCliGatedInputsToGate` (script contract test, if the runner tests cover script arguments).
**Green**: the CLI gate receives its own gated-input list; the library gate keeps its list.
**Refactor**: with all tests green.

### S3 Verification and review
`ci-local.ps1` on the change (this change touches neither `examples/` nor `src/`, so it exercises the new rule in its own run as far as its inputs allow; recorded as it is); hosted CI; per-step Codex review; commit and push.

## 6. Tests and Test IDs
| Step-local case | T-P9 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S2 | T-P9-07 | REQ-086 (amended) | CLI gate equal coverage only with unchanged CLI inputs; strict improvement otherwise; library gate unchanged |
| all | T-P9-16 | REQ-085 | Red evidence |

## 7. Evidence to Record
- Amendment text and date, Red names and failures, test counts, both gates' counters, CI runs, authorship, Codex round

## 8. Risks and Limitations
- (a) The coverage tooling gates its own changes; the tools' own tests are the guard against a weakened rule, and the diff review rejects any change of expected values
- (b) A too-wide gated-input list would make the exception unreachable; a too-narrow one would let CLI changes pass with equal coverage; the list is the user's decision

## 9. Acceptance Criteria
- [ ] Amendment recorded before the code change
- [ ] Every listed test green with recorded Red; library gate rule unchanged
- [ ] CONTRIBUTING.md updated; Codex 0 findings or reported
