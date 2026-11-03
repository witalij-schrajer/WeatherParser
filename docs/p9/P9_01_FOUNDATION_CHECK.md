# P9.01 – Foundation Check

**Status (2026-10-31): complete, outcome READY (section 7).** Plan: [P9_01_FOUNDATION_CHECK_PLAN.md](../development/p9/P9_01_FOUNDATION_CHECK_PLAN.md).

User instruction (2026-10-31, chat): "P9 Execution".

## 1. Records recheck and commit classification (S1)

- P3 accepted by the user on 2026-10-31 on candidate `ec915fc` ([P3.12 acceptance record](../p3/P3_12_ACCEPTANCE.md), A11); pull request #16 merged into `develop`, merge commit `2ba5502b63729120e284c873a72b418149505a90` (`gh pr view 16`: state MERGED, merged at 2026-10-30T23:34:28Z, that is 2026-10-31 00:34 local time).
- Pull request #17 (P3 post-merge records and the P9 plans) merged by the user; merge commit `ed7fc7a` is the `origin/develop` head at branch creation.
- `git diff --name-only 2ba5502 ed7fc7a` lists Markdown files only (20 files: AGENTS.md, DEVELOPMENT_PLAN.md, PHASE_P1_PLAN.md, PHASE_STATUS.md, PHASE_P9_PLAN.md, the P3 records and handoff and the twelve P9 step plans); the root `README.md` is not among them. No production code, test, tool, script, CI configuration or package pin changed since the P3 merge.

| Commits | Subject | Classification |
| --- | --- | --- |
| 17 commits from the P3 post-merge checkpoint to `2cc09c5` | P3 post-merge records, P9 phase plan, P9 step plans, their review rounds and the plan acceptance | Documentation |
| `ed7fc7a` | Merge pull request #17 | Merge |

- Effect on the contracts: none. The handoff assumption "`2ba5502` or its documentation-only successor" holds: the integration target is `ed7fc7a`.

## 2. Extraction (S3)

### P9.01 extraction

| Field | Value on `ed7fc7a` | Source |
| --- | --- | --- |
| Integration target | `ed7fc7a03db2f5554c490451f918da9fc6828c84` (`origin/develop` at branch creation) | `git rev-parse origin/develop` |
| Package identity | `PackageId` `WeatherParser.Parsing`, `Version` `0.1.0-initial`, `Authors` "WeatherParser contributors", `PackageLicenseExpression` MIT, `PackageReadmeFile` `README.md` | `src/WeatherParser.Parsing/WeatherParser.Parsing.csproj` |
| Package description | "Work-in-progress weather parsing library. Parser implementations are not available yet." — stale since P5 (all parsers are implemented and accepted); input to RL-01 | Same file, packed `.nuspec` |
| Repository, project URL, tags, release notes | Not set in the project. The packed `.nuspec` nevertheless contains `<repository type="git" commit="ed7fc7a…" />`: the SDK's built-in Source Link support sets `RepositoryCommit` from the clone; the repository URL is absent because the clones of section 3 have a local path as origin. The package bytes therefore depend on the commit ID (input to RL-03 and RL-06) | Packed `.nuspec` |
| Build settings | `Directory.Build.props`: `TargetFramework` `net10.0`, `Deterministic` true, `RestorePackagesWithLockFile` true, `IsPackable` false (the library sets true); `ContinuousIntegrationBuild`, `PathMap`, `GenerateDocumentationFile`, `IncludeSymbols`, `EmbedUntrackedSources` and `EnablePackageValidation` are not set | `Directory.Build.props`, project file |
| XML documentation | Not generated and not packed (`GenerateDocumentationFile` not set), although the library source has 3922 `///` comment lines in 270 files; completeness for public members not yet measured (input to RL-01, P9.05) | `git ls-files`, `Select-String` |
| Package contents | `lib/net10.0/WeatherParser.Parsing.dll`, `README.md`, `LICENSE`, `schemas/export/v1/weatherparser-export-v1.xsd`, `WeatherParser.Parsing.nuspec`, `[Content_Types].xml`, `_rels/.rels`, `package/services/metadata/core-properties/nuget.psmdcp` | Section 3 |
| Pack-stage entry list | `Invoke-PackStage` of `scripts/ci-local.ps1` requires exactly the seven fixed entries above plus one `package/services/metadata/core-properties/*.psmdcp` | `scripts/ci-local.ps1` |
| Package dependencies | `AngleSharp` 1.8.5, `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.13, `Microsoft.Extensions.Logging.Abstractions` 10.0.13 (group `net10.0`) | Packed `.nuspec` |
| Consumer pin | `WeatherParser.Parsing` `0.1.0-initial` in `Directory.Packages.props`; consumer lock files follow it | `Directory.Packages.props` |
| Public API surface | 252 exported types (public, and nested public or protected in exported types), 1382 public or protected methods (accessors and constructors included), 165 public or protected fields (enum members included) in 17 namespaces under `WeatherParser.Parsing` (`Catalogs`, `Evidence.MosmixL`, `Evidence.MosmixS`, `Exceptions`, `Export`, `Forecasts`, `Forecasts.Mosmix`, `Geography`, `Mqtt`, `Observations`, `Observations.Dwd`, `Observations.Osthessen`, `Parsing`, `Quantities`, `Selection`, `Serialization`, `Time`); listing read from the packed assembly's metadata with `System.Reflection.Metadata` (a scratch script outside the repository; listing SHA-256 `88F24E92AE5BE21FF2A109A335C9BB65EE214953107EF09C577F8FE56BA8B0F6`). Input to RL-04; the checked-in listing format is decided in P9.05 | Packed assembly |
| SDK | `global.json` 10.0.402, `rollForward` `disable`, `allowPrerelease` false; installed runtime 10.0.13 | `global.json`, `dotnet --list-runtimes` |
| Root pins | `AngleSharp` 1.8.5; `Microsoft.Extensions.DependencyInjection`, `.DependencyInjection.Abstractions`, `.Logging.Abstractions` 10.0.13; MSTest 4.5.1; `Microsoft.NET.Test.Sdk` and `Microsoft.CodeCoverage` 18.11.0; Moq 4.21.0; FluentAssertions 8.11.0 | `Directory.Packages.props` |
| CLI-only pins | `Microsoft.Extensions.Logging` 10.0.13, MQTTnet 5.2.0.1603 | `examples/Directory.Packages.props` |
| Line endings | `.gitattributes` pins only `schemas/**/*.xsd` to LF; `README.md` and `LICENSE` have no attribute, so a Windows clone with `core.autocrlf=true` checks them out with CRLF | `.gitattributes`, `git ls-files --eol` |
| Branches | `develop`, `master` (and the merged feature branches) | `git branch -r` |
| Workflows | `ci.yml` (`pull_request` into `develop` and `master` with types opened, synchronize, reopened, ready_for_review; `workflow_dispatch` with `target_ref`; three jobs after the `changes` job; uploads `artifacts/ci/` for 30 days), `review.yml` (automatic Codex review on ready for review) | `.github/workflows/` |

## 3. Pack comparison (S3)

Method (plan decision "Pack comparison method": both): the byte comparison decides reproducibility; the member comparison (name, size, SHA-256 and zip timestamp of every member, and the `.nuspec` text) names the causes. Scratch scripts outside the repository.

**Windows, two clean clones of `ed7fc7a`** (2026-10-31, SDK 10.0.402, `git clone --no-hardlinks` of the local repository into two directories, `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build` and `dotnet pack` of the library project with `-c Release`):

| Package | SHA-256 | Bytes |
| --- | --- | --- |
| Clone A | `49D36AC26666F06DD03D79F4ABF1F5E02E7D3B625AB08ACDB591867C4720CF02` | 185543 |
| Clone B | `83E1A6062BED665EA20C0A3A8CD97B73D31DEE1F448710D0729E73D3E09AB01A` | 185542 |

Not byte-identical. Every member has a different zip timestamp; the content of every member except `lib/net10.0/WeatherParser.Parsing.dll` is identical. The two DLLs (407040 bytes each) differ in 70 bytes: the PE header timestamp field (offsets `0x88` to `0x8B`), the module version ID, the CodeView debug entry (PDB ID and the absolute PDB path `…\clone-a\src\WeatherParser.Parsing\obj\Release\net10.0\WeatherParser.Parsing.pdb` against `…\clone-b\…`). With `Deterministic` the timestamp, the module version ID and the PDB ID are derived from a hash of the compilation inputs, which include the paths. Control experiment: deleting `bin` and `obj` of clone A and building again in the same directory gave a byte-identical DLL (SHA-256 `0100A356A96CACADC96846053B06FEDCDB76D9B69CD886FAF72547F71578FC09` before and after).

**Windows and Linux, the seed commit `ad2df07`**: the packages built by `ci-local.ps1` locally (section 5) and by the manual hosted run 41173405524 (`workflow_dispatch`, head `ad2df07`, target `ed7fc7a`, jobs windows-x64, ubuntu-x64 and debian13-arm64 all success; artifacts downloaded with `gh run download`):

| Package | SHA-256 | Bytes |
| --- | --- | --- |
| Local Windows | `6961CB7010BF4C80A09034E93F9DBE7A8A855CC495289A2300180AA2850555D6` | 185499 |
| Hosted windows-x64 | `60851B087F72CE56E7530B88951E697F783593CE9A22E55D279B57A7E1D099F2` | 185506 |
| Hosted ubuntu-x64 | `BE68A734F6806249DF82EAB72A338DBB63859C270D059882FDF2EB9EA8669185` | 185414 |
| Hosted debian13-arm64 (container, not target evidence) | `BB0407E6241E7A23A79667815DA5D949E7E693D667E8A9E62856AD5A62DFE03F` | 185410 |

| Member | Windows (both) | Linux (both) |
| --- | --- | --- |
| `README.md` | 86205 bytes (CRLF) | 85556 bytes (LF, equal to the committed blob, SHA-256 `b61c51ea…`) |
| `LICENSE` | 1104 bytes (CRLF) | 1083 bytes (LF, equal to the committed blob) |
| `schemas/export/v1/weatherparser-export-v1.xsd` | 16039 bytes | identical (LF pinned by `.gitattributes`) |
| `WeatherParser.Parsing.nuspec`, `_rels/.rels`, `[Content_Types].xml`, `…/nuget.psmdcp` | 1046, 488, 659, 719 bytes | 1027, 484, 650, 711 bytes; identical between the two Linux jobs and between the two Windows builds |
| `lib/net10.0/WeatherParser.Parsing.dll` | 407040 bytes, differs between the two builds | 407040 bytes, differs between all four builds |

Named causes (input to RL-03):
1. **Build path**: the absolute PDB path in the DLL's CodeView entry and the deterministic values derived from the paths (PE timestamp, module version ID, PDB ID); `ci-local.ps1` clones into a new temporary directory per run, so its DLL differs on every run and platform. Same path → same DLL.
2. **Zip entry timestamps**: NuGet stores each member's file modification time (checkout time for README, LICENSE and the XSD, build time for the DLL, pack time for the generated metadata).
3. **Line endings across platforms**: `README.md` and `LICENSE` have no `.gitattributes` rule and are checked out with CRLF on Windows (`core.autocrlf=true`, the Git for Windows default); NuGet writes the generated `.nuspec`, `.rels`, `[Content_Types].xml` and `.psmdcp` with the platform's newline.
4. **Commit ID**: the `.nuspec` carries `<repository type="git" commit="<commit>" />`, and the DLL's generated `AssemblyInformationalVersionAttribute` carries `<version>+<commit>` (both from the SDK's Source Link support, section 2; read from the packed DLLs: `0.1.0-initial+ed7fc7a03db2f5554c490451f918da9fc6828c84` in the packs of `ed7fc7a`, `0.1.0-initial+ad2df07febdc2998ee6a95cfbfe2549c345f37f4` in the packs of `ad2df07`); two builds of the same commit agree, builds of different commits differ in both the `.nuspec` and the DLL even with identical source files and build paths (expected, RL-06).

The `.psmdcp` content contains no random value: it is identical between builds on the same platform. No other cause was found.

**Deviation from S3 (round 1 of the P9.01 Codex review):** the plan asks for two clean packs of the integration target on Windows *and on Linux*. No Linux environment with the .NET SDK was available to the orchestrating agent (WSL has no SDK installed; installing one would be a download, and a NanoPi run needs the user's authorization), so the Linux evidence is one pack each from the two hosted Linux jobs of the seed commit `ad2df07` instead of two packs of `ed7fc7a` in one Linux environment. The checked-in build inputs of `ad2df07` equal those of `ed7fc7a` (the seed commit changed Markdown files only, not `README.md`); the generated revision metadata differs (cause 4: the commit ID in the `.nuspec` and in the DLL's informational version), so the two revisions are not compilation-equivalent, and the Linux packages are compared only with each other, not with the packs of `ed7fc7a`. The two Linux DLLs differ in 101 bytes, all in the same three regions as on Windows (PE timestamp `0x88` to `0x8B`, module version ID at `0x59348`, the CodeView entry with the PDB ID and the PDB path at `0x62B98` to `0x62CAD`); their PDB paths are `/tmp/weatherparser-ci-<GUID>/src/src/WeatherParser.Parsing/obj/Release/net10.0/WeatherParser.Parsing.pdb` with different GUIDs. This supports cause 1 on Linux, but because the jobs also differ in architecture (x64, ARM64 container), it does not prove that a same-path rebuild on Linux is byte-identical. That proof belongs to P9.05 (T-P9-03: two clean builds on Windows and on Linux after the decided remedies); the conclusions of this section are limited accordingly. The packed `README.md` of a Windows build is therefore not byte-identical to the committed `README.md`, which matters for T-P9-02 ("byte-identical to the committed `README.md`") and for the promotion check of RL-06.

## 4. Servicing and latest versions (S4)

Retrieved on 2026-10-31 from the official .NET release metadata (`https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json` and `.../10.0/releases.json`) and the nuget.org flat container (`https://api.nuget.org/v3-flatcontainer/<id>/index.json`, stable versions only). The pins stay; the update is decided in P9.02 (RL-09, RL-10).

| Item | Pinned | Latest stable | Note |
| --- | --- | --- | --- |
| .NET 10 SDK | 10.0.402 | 10.0.403 (release 10.0.14 of 2026-10-25) | 10.0 is LTS, support phase active; 10.0.14 is marked as a security release (CVE-2026-111497, CVE-2026-70197, CVE-2026-102870, CVE-2026-114148) |
| .NET 10 runtime | 10.0.13 (with SDK 10.0.402) | 10.0.14 | As above |
| .NET 11 | — | 11.0.0-rc.3 (go-live, STS) | Not stable; no change (AGENTS.md: previews are not guaranteed) |
| `Microsoft.Extensions.DependencyInjection`, `.DependencyInjection.Abstractions`, `.Logging.Abstractions`, `.Logging` | 10.0.13 | 10.0.14 | CL-22 |
| MQTTnet | 5.2.0.1603 | 5.2.0.1603 | Unchanged |
| AngleSharp | 1.8.5 | 1.9.0 | **Not in the plan's item list**: a newer minor version of a library dependency (packed as a package dependency); added to RL-09 as an input for the user; licence and changelog are checked in P9.02 |
| MSTest.TestFramework, MSTest.TestAdapter | 4.5.1 | 4.5.2 | Test-only pins (root `Directory.Packages.props`, a gated library input); input to RL-09 |
| Microsoft.NET.Test.Sdk, Microsoft.CodeCoverage | 18.11.0 | 18.11.0 | Unchanged |
| Moq | 4.21.0 | 4.21.0 | Unchanged |
| FluentAssertions | 8.11.0 | 8.11.0 | Unchanged |
| dotnet-coverage (local tool, `.config/dotnet-tools.json`) | 18.11.0 | 18.12.0 | Coverage tool; a change alters the collection conditions of both gates (comparability); input to RL-09 |
| `WeatherParser.Parsing` on nuget.org | — | The flat container returns 404 Not Found | Only an indication that no package with this ID is published; name availability, reserved prefixes and ownership are checked in P9.02 (RL-01) |

## 5. Baseline (S5)

- `pwsh ./scripts/ci-local.ps1 -Commit ad2df07febdc2998ee6a95cfbfe2549c345f37f4 -TargetRef ed7fc7a -RunSettings coverage.runsettings`: exit 0, run directory `artifacts/ci/ad2df07-20261031T053851Z` (gitignored), two-pass; Parsing 3213 of 3220 passed, 0 failed (seven opt-in checks not executed), SchemaGeneration 46, Coverage 118, Coverage.Runner 61, package consumer 61, CLI 707 of 719 passed, 0 failed (twelve opt-in or platform-specific checks not executed).
- Library gate (`WeatherParser.Parsing.dll`): passed, 11102/11103 at or above the baseline 11102/11103 of `ed7fc7a` (`gate-result.json` SHA-256 `6c3c1112dbe482712cee5205a84b266ea71ed626db9743550c0a9ab183073308`).
- CLI gate (`WeatherParser.dll`): passed, 2872/2917 at or above the baseline 2872/2917 of `ed7fc7a` (`cli-gate-result.json` SHA-256 `aa2bf575c59c2c92fd97408298836c9ed4eca3ee02a2f8a0a0626bfcf3bb53f6`).
- **Baselines for P9 on Windows: library 11102/11103, CLI 2872/2917** native blocks for the target `ed7fc7a`; valid while the pull-request base is `ed7fc7a` and `coverage.runsettings` is unchanged. The uncovered library block is unchanged (`FileSystemMosmixEvidenceCache.DeleteTemporaryFile`, the `catch`, P3.01 section 5). The CLI gate is evaluated per platform against the target measured on the same platform; the hosted run 41173405524 passed both gates on all three jobs.
- Uncovered blocks of `WeatherParser.dll` in the target's CLI report (`target/cli-coverage/coverage.xml`, functions with `blocks_not_covered` > 0), 45 in ten functions:

| Function | Source | Not covered |
| --- | --- | --- |
| `FileModeReader.Read(SafeFileHandle)` | `examples/WeatherParser.Cli/Configuration/FileModeReader.cs` line 27 | 25 |
| `MqttResultWriter.SourceOf(object)` | `examples/WeatherParser.Cli/Output/MqttResultWriter.cs` line 39 | 6 |
| `MqttPublicationSettingsFactory.Create(CliConfiguration, BrokerCredentials)` | `examples/WeatherParser.Cli/Mqtt/MqttPublicationSettingsFactory.cs` line 15 | 3 |
| `PublicationCancelledException(PublicationReport, OperationCanceledException, CancellationToken)` | `examples/WeatherParser.Cli/Output/PublicationCancelledException.cs` line 23 | 2 |
| `ProcessTerminator.Terminate(int)` | `examples/WeatherParser.Cli/Lifetime/ProcessTerminator.cs` line 11 | 2 |
| `FileModeReader.GetEffectiveUserId()` | `examples/WeatherParser.Cli/Configuration/FileModeReader.cs` line 60 | 2 |
| `ParseDwdHandler.HandleAsync` (state machine `MoveNext`) | `examples/WeatherParser.Cli/Commands/ParseDwdHandler.cs` line 51 | 2 |
| `ShutdownSignal.Terminate(string)` | `examples/WeatherParser.Cli/Lifetime/ShutdownSignal.cs` line 128 | 1 |
| `ShutdownSignal.Dispose()` | `examples/WeatherParser.Cli/Lifetime/ShutdownSignal.cs` line 159 | 1 |
| `ConfigurationValidator.Validate(IServiceProvider, CliConfiguration, CommandRequest)` | `examples/WeatherParser.Cli/Configuration/ConfigurationValidator.cs` line 52 | 1 |

  `FileModeReader` (Linux `statx` and `geteuid`) is Linux-only code and is uncovered on Windows by design; `ProcessTerminator.Terminate` ends the process (`Environment.Exit`). The other blocks are not classified here; the inventory is the reference for RL-11 and later baseline reconciliations. The table is the Windows measurement; the Linux jobs measure their own target.
- Hosted CI on the seed commit: the `pull_request` run of `ad2df07` skipped the jobs (Markdown only); the manual run 41173405524 executed all three jobs with success.

## 6. Item mapping and plan adjustments (S6)

Every open item of [P3_12_ACCEPTANCE.md](../p3/P3_12_ACCEPTANCE.md) section 4, [P9_HANDOFF.md](../p3/P9_HANDOFF.md) sections 3 and 6 and the table of PHASE_P9_PLAN.md section 1, plus the items found in this check:

| Item | Source | P9 handling |
| --- | --- | --- |
| Package ID, versioning, feed, publication, release artifacts and checksums | Phase plan section 1 | RL-01, RL-02, RL-05, RL-06; P9.05, P9.06, P9.12 |
| Package not byte-reproducible; causes now named (section 3) | P8.10, P3.12, this check | RL-03; P9.05 |
| Stale package `Description` | This check (section 2) | RL-01; P9.05 |
| `RepositoryCommit` embedded by the SDK's Source Link support | This check (section 2) | RL-03, RL-06 (already assumed by RL-06 A); P9.05, P9.06 |
| XML documentation not generated | Phase plan section 1, this check | RL-01; P9.05 |
| Deployment model of the example CLI | P9 handoff section 3 | RL-07, RL-08; P9.07 |
| Operating guides | P9 handoff section 3 | RL-12; P9.08 |
| TLS and authenticated broker connections not verified | P3.12 section 4 | RL-13; P9.09 |
| Windows system shutdown not provoked | P3.12 section 4 | RL-14; P9.09 |
| "DI under load", long runs | P8.10 section 4 | RL-15; P9.10 |
| CL-22: SDK 10.0.403, runtime 10.0.14, `Microsoft.Extensions.*` 10.0.14 | P3.12 section 4 | RL-09; P9.03 |
| AngleSharp 1.9.0 available (pin 1.8.5); MSTest 4.5.2 (pin 4.5.1); dotnet-coverage 18.12.0 (pin 18.11.0) | This check (section 4) | RL-09 (added as input); P9.03 |
| CLI-only pins need confirmation | P3.12 section 4 | RL-10; P9.02, P9.03 |
| CL-19 C asymmetry | P3.12 section 4 | RL-11; P9.04 |
| OW-14 | P3.12 section 4 | RL-16; P9.08 |
| Source conditions and attribution | P9 handoff section 3 | RL-16; P9.08 |
| Inherited library limitations (builder and root-validation duplication, coarse XML cancellation C-10, hosted ARM64 container not target evidence, P7 limitations) | P8.10, P3.12 section 4 | RL-18; P9.08 |
| Broker disconnect after cancellation bounded by `ConnectTimeout`; MQTT 3.1.1 acknowledgment limitation | P3.12 section 4, P9 handoff section 6 | Recorded; listed as known limitations in the release documentation (RL-18; P9.08) |
| CL-06 owner tests run only as root | P3.12 section 4 | Recorded; the NanoPi release acceptance checks the credential-file rejection as the service user (T-P9-08, P9.10) |
| `DeleteTemporaryFile` failure block reachable only on Windows (library gate 11102/11103) | P3.12 section 4 | Recorded; baseline input (section 5) |
| P3.11 S3 to S5 not rerun on `ec915fc`; the extra `--SelectMOSMIX` live run; the missing MQTT `retrieved_at` | P3.12 section 4 | Decided by the user with the P3 acceptance; no P9 action (P9.10 runs on the release candidate anyway) |
| CL-20 wording (`ClosingTcpListener` instead of a fake publisher); P3 process deviations | P3.12 section 4 | Recorded; no P9 action |

No item is unmapped.

Plan adjustments: the "P9.01 extraction" table is added to PHASE_P9_PLAN.md section 1 as a reference to section 2 of this record; RL-01 names the stale description, RL-03 the three named causes of section 3, RL-09 the AngleSharp minor update. The rows stay proposals until P9.02.

## 7. Outcome

**READY.** No production change since the P3 merge (section 1), every item mapped (section 6), both baselines measured (section 5), the causes of package differences named (section 3). P9.02 (release facts and release contract) is next.
