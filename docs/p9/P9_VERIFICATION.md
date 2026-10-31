# P9 Verification Record

**Status (2026-10-31): P9.01 complete, outcome READY (section 1); P9.02 complete: release contract RL-01 to RL-20 approved by the user on 2026-10-31, RL-19 open, plans reconciled (section 2).**

- Integration target: `ed7fc7a` (merge of pull request #17; documentation-only successor of the P3 merge `2ba5502`).
- Branch `features/develop/P9`.

## 1. P9.01 – Foundation check

- User instruction (2026-10-31, chat): "P9 Execution".
- Record: [P9_01_FOUNDATION_CHECK.md](P9_01_FOUNDATION_CHECK.md), written by the orchestrating agent; no qwen output in this step (documentation and repository operations only, AGENTS.md). Scratch scripts for the pack comparison and the public API listing were written by the orchestrating agent outside the repository; they are evidence tools, not repository code.
- S1: commit classification of `2ba5502..ed7fc7a`: Markdown only.
- S2: branch `features/develop/P9` from `origin/develop` `ed7fc7a`; status passages "P9 started" in AGENTS.md, PHASE_STATUS.md, DEVELOPMENT_PLAN.md, PHASE_P1_PLAN.md and the status line of PHASE_P9_PLAN.md. Seed commit `ad2df07febdc2998ee6a95cfbfe2549c345f37f4`, pushed; draft pull request #18 into `develop` opened; Auto-fix on. The `pull_request` run 41173407004 skipped the three jobs (Markdown only); the manual run 41173405524 (`workflow_dispatch`, `target_ref` `ed7fc7a`) completed with success on windows-x64, ubuntu-x64 and debian13-arm64.
- S3: extraction and pack comparison (record sections 2 and 3): two clean Windows packs of `ed7fc7a` not byte-identical; four packages of `ad2df07` (local, hosted Windows, Ubuntu, Debian ARM64) not byte-identical; four causes named.
- S4: servicing facts (record section 4): SDK 10.0.403 and runtime 10.0.14 (security release) available; `Microsoft.Extensions.*` 10.0.14; MQTTnet, Microsoft.NET.Test.Sdk, Microsoft.CodeCoverage, Moq and FluentAssertions unchanged; AngleSharp 1.9.0, MSTest 4.5.2 and dotnet-coverage 18.12.0 against the pins 1.8.5, 4.5.1 and 18.11.0.
- S5: `ci-local.ps1` on `ad2df07` against `ed7fc7a`: exit 0; library gate 11102/11103, CLI gate 2872/2917, both passed (record section 5).
- S6: item mapping complete, plan adjustments in PHASE_P9_PLAN.md section 1 ("P9.01 extraction") and RL-09; outcome READY.
- Codex review of P9.01 (read-only, five-angle format, `codex exec --sandbox read-only --ephemeral`, on the uncommitted diff on top of `ad2df07`):
  - round 1: 3 findings, all verified and confirmed: [P2] the servicing check omitted the test pins (MSTest, Microsoft.NET.Test.Sdk, Microsoft.CodeCoverage, Moq, FluentAssertions) and the coverage tool; added (record section 4); [P2] the Linux half of the two-pack comparison was substituted without a recorded deviation; recorded with limited conclusions (record section 3); [P2] S5 requires every uncovered block, the CLI inventory was missing; added, 45 blocks in ten functions (record section 5);
  - round 2: 1 finding, verified by reading the informational version from the packed DLLs and confirmed: [P2] cause 4 ignored that the commit ID is also embedded in the DLL's `AssemblyInformationalVersionAttribute`; cause 4 and the deviation paragraph corrected;
  - round 3: 0 findings.

## 2. P9.02 – Release facts and release contract

- Contract draft: [P9_RELEASE_CONTRACT.md](P9_RELEASE_CONTRACT.md), written by the orchestrating agent; facts F1 to F15 retrieved on 2026-10-31 from the sources named in its section 2 (one page request each to nuget.org, Microsoft Learn, GitHub Docs, the AngleSharp repository, dwd.de, opendata.dwd.de and the OsthessenWetter imprint; `gh api` reads of the repository settings). Added rows: RL-19 (repository visibility: the repository is private, F4) and RL-20 (coverage gates for pull request #18).
- Review: automatic Codex review of pull request #18 (rounds recorded in section 6 of the contract).
- Review: automatic Codex review rounds 1 to 5 of pull request #18 (2, 1, 2, 2 and 0 findings, all confirmed and fixed) and a 0-finding review of the recording commit `19b6da3` (run 41175204215).
- Approval (S4): "Approve all" (chat, 2026-10-31): the recommended option in every row; RL-19 has no single recommendation and stays open until the user chooses, before P9.06.
- Reconciliation (S5): approved-contract note in PHASE_P9_PLAN.md section 3; section 10 in the step plans P9.03 to P9.12; P9.04 moved before P9.03.
