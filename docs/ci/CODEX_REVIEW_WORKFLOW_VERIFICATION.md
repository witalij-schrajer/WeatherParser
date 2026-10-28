# Automatic Codex review workflow: verification record

Change: pull request #13 (`features/develop/codex-review-ci` into `develop`), which adds `.github/workflows/review.yml` and the briefs `.github/review/prompt-documentation.md` and `.github/review/prompt-implementation.md`. The user made the decision on 2026-10-28 and it is recorded in AGENTS.md, "Second reviewer: Codex". The procedure is in CONTRIBUTING.md section 6, "Automatic Codex review (GitHub Actions)".

## User decisions (2026-10-28)

- Purpose: when a pull request's status changes to ready for review, CI starts a review with two different prompts, one for documentation and planning pull requests and one for implementation pull requests.
- Reviewer engine: Codex through `openai/codex-action` (the alternative offered was Claude through `anthropics/claude-code-action`).
- Classification: by changed paths (only Markdown files and `docs/` mean documentation), with the labels `review:docs` and `review:implementation` as override.
- The user created the repository secret `OPENAI_API_KEY` (restricted key: Responses write, List models read) and marked pull request #13 ready for review.

## Authorship

| Part | Author |
|---|---|
| `.github/workflows/review.yml` | Orchestrating LLM. **Exception to the qwen rule** (AGENTS.md, "AI-assisted development"): the workflow depends on the user's decisions above and on facts that qwen cannot have and that were checked against external sources during the work: action commits and inputs on GitHub, npm package versions, and the behaviour of the local Codex CLI (`project_doc_max_bytes`). |
| `.github/review/prompt-documentation.md`, `.github/review/prompt-implementation.md` | Orchestrating LLM (review briefs are documentation). |
| AGENTS.md, CONTRIBUTING.md, DEPENDENCIES.md changes, this record | Orchestrating LLM (documentation). |
| `tests/WeatherParser.Parsing.Tests/ParserEndpointValidatorTests.cs`, test `Constructor_WithNullUriValidator_ThrowsArgumentNullException` | qwen3-coder:30b through Ollama `/api/chat` with the AGENTS.md invocation settings; reviewed by the orchestrating LLM, whose diff check showed exactly one added test method and no other change. |

## Local Codex reviews before the first commit

Codex CLI 0.163.0, `codex exec --sandbox read-only --ephemeral`, run against the uncommitted diff on top of `develop` `20b2ef0`, using the five-angle audit format. The outputs were ephemeral and kept outside the repository; the findings and their dispositions are recorded here.

| Round | Findings | Finding | Disposition |
|---|---|---|---|
| 1 | 8 | Documentation brief asserted that the pull request contains no code, scripts or data, although the label override and `docs/` data allow both | Confirmed; the brief now presents its checks as emphasis and requires every changed file to be examined |
| 1 | | `printf … \| head -c 60000` under `pipefail` can end the comment step with SIGPIPE (exit 141) | Confirmed; the review is written to a file and truncated from the file |
| 1 | | Git quotes non-ASCII file names, so Markdown files with such names were classified as implementation | Confirmed; `core.quotePath=false`; names Git still quotes fall back to implementation |
| 1 | | Labels came from the event payload, so a re-run after a label change still saw the old labels | Confirmed; labels are read through `gh pr view` when the job runs (`pull-requests: read`) |
| 1 | | The bootstrap fallback also took the five-angle audit section from the head commit | Confirmed; the audit section is always read from the base commit |
| 1 | | `codex-version` was empty, so the action installed floating `@openai/codex` and proxy versions | Confirmed; pinned (first 0.162.1, then 0.165.0 after the hosted finding below) |
| 1 | | `.codex-review/` is a hidden directory, which `upload-artifact` excludes by default | Confirmed; renamed to `codex-review/` |
| 1 | | AGENTS.md did not record the automated review or a consistency-table row | Confirmed; AGENTS.md now records the decision and the row |
| 2 | 3 | `awk … { exit }` closes the pipe early and can end `git show` with SIGPIPE | Confirmed; awk reads to the end of its input |
| 2 | | With a label set, the empty-diff check was skipped | Confirmed; the empty-diff check now runs first |
| 2 | | Codex loads `AGENTS.md` or `AGENTS.override.md` from the pull request head, so a pull request can instruct its own reviewer | Confirmed; `codex-args` sets `project_doc_max_bytes=0`. Local probe with CLI 0.163.0: without the option Codex reports the AGENTS.md heading as loaded, with the option it reports none. Both briefs name the base commit's rules. |
| 3 | 0 | – | – |

## Executed checks

- shellcheck (`-S warning`) on all five `run:` scripts of the workflow: no findings, before and after the fixes.
- Classification rule applied to the real history: pull request #10 gives documentation (23 of 23 files are documentation), pull request #11 gives implementation (103 of 128 files are not).
- Five-angle section extraction from AGENTS.md: unchanged output after the awk change (diff empty).
- Hosted CI run [40756012114](https://github.com/witalij-schrajer/WeatherParser/actions/runs/40756012114) on `230205c`: all tests passed on the three runners, but the coverage gate failed with exit 90. Block coverage was 10246/10249, equal to the `develop` baseline, and the gate classifies a pull request as documentation-only only when every changed file is `.md`, so it requires a strict increase here.
- Coverage fix `b4e09ae`: the qwen test covers the `ArgumentNullException` guard of the `ParserEndpointValidator` constructor (2 of the 3 uncovered blocks; the third is the best-effort `catch` in `FileSystemMosmixEvidenceCache.DeleteTemporaryFile`). Red: with the guard removed, 1 of 5 `ParserEndpointValidatorTests` failed. Green: with the source restored, 10 of 10 `ParserEndpointValidator` tests passed (local Release run). Hosted CI run [40757308498](https://github.com/witalij-schrajer/WeatherParser/actions/runs/40757308498) on `b4e09ae` succeeded on all three runners.

## Hosted Codex review runs

| Run | Head | Result | Findings | Disposition |
|---|---|---|---|---|
| [40758968836](https://github.com/witalij-schrajer/WeatherParser/actions/runs/40758968836) (ready for review, 2026-10-28) | `b4e09ae` | Success. The review job took 1 min 7 s and the comment job posted [comment 6479186819](https://github.com/witalij-schrajer/WeatherParser/pull/13#issuecomment-6479186819). Classification: implementation (2 of 7 files are not documentation). The brief was read from the head commit, the intended bootstrap case, and the artifact `codex-review` was uploaded. Codex version 0.162.1. | 2 | Angle 2, P2: 0.162.1 contradicted the latest-stable rule of the P0 clarifications. Confirmed; pinned to 0.165.0, the npm `latest` of both packages. Angle 5, P2: review dispositions and the workflow authorship exception were not recorded. Confirmed; this record. |
| [40759491202](https://github.com/witalij-schrajer/WeatherParser/actions/runs/40759491202) (converted to draft and marked ready again, 2026-10-28) | `5944184` | Success, with Codex 0.165.0. Codex posted [comment 6479258707](https://github.com/witalij-schrajer/WeatherParser/pull/13#issuecomment-6479258707). Classification: implementation (2 of 8 files are not documentation). | 0 | – |

The second hosted run reviewed the fixes for the first run's findings with Codex 0.165.0 and reported no findings.

## CI only for code changes (2026-10-28)

At the user's request, `.github/workflows/ci.yml` has a new first job, `changes`, written by the orchestrating LLM. Under the same exception as the review workflow, it depends on the user's decision and on the repository's packaging (the root `README.md` is packed). The three CI jobs run only when code changed; the rules are in CONTRIBUTING.md section 6 and AGENTS.md. `cancel-in-progress` was removed, so that a documentation-only push cannot cancel a run that still verifies code.

Executed checks: shellcheck on the detection script found nothing. The script was run locally against real history with the following results:

| Case | Range | Result |
|---|---|---|
| Push of a test file | `230205c..b4e09ae` | run |
| Push of workflow and Markdown files | `b4e09ae..5944184` | run |
| Documentation-only push | `717b3f5..28a1858` | skip |
| Pull request #12 opened (changes the root `README.md`) | `20b2ef0...28a1858` | run |
| Pull request #13 marked ready | `20b2ef0...5944184` | run |
| Force push with an unknown previous head | whole pull request | run |
| Commit `58813a3` touching `README.md` | `58813a3~1..58813a3` | run |
| Manual run | – | run |
| Code file renamed into `docs/` (temporary local commit) | rename | run |

Local Codex review of this change (CLI 0.163.0, read-only, five-angle format): round 1 reported 3 findings, all confirmed. (1) The CI and coverage-gate classifications differ: documented in CONTRIBUTING.md and AGENTS.md, gate unchanged. (2) `--name-only` hides the old path of a rename: fixed with `--no-renames` and verified with the rename case above. (3) The consistency table requires `DEPENDENCIES.md`: updated. Round 2 reported 0 findings.

Hosted runs: [40760414865](https://github.com/witalij-schrajer/WeatherParser/actions/runs/40760414865) on `06369f0`, a push that changes `ci.yml`. `changes` succeeded and the three CI jobs ran and passed (Windows x64, Ubuntu x64, Debian 13 ARM64). The next push changes only this record and is the first documentation-only push; its run is the evidence for skipped jobs and is named in the pull request conversation.

## Limits

- Model output is review input, not evidence; only the executed runs listed above count.
- actionlint was not run. The workflow syntax is evidenced by the successful hosted run 40758968836.
- When the base contains no brief yet (this pull request), the brief comes from the head commit; from the next pull request on, briefs and the audit section come from the base.
