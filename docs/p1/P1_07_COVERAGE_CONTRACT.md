# P1.07 – Microsoft.CodeCoverage: Raw Report and Report Contract

## Goal

Prove that Microsoft.CodeCoverage produces a real raw report with code block and line breakdown,
that this report can be converted to an evaluable format, and that the counters are traceably
attributed to the production assembly (`WeatherParser.Parsing.dll`) and the test assembly
(`WeatherParser.Parsing.Tests.dll`). P1.07 establishes only the report contract; evaluation
including the 80 % rule and exceptions is assigned to P1.08.

## Procedure

Verified commit: `1962d6b437cc52186b1fb0f7bbdc061c22ba97a2` (as of 2026-10-16).

1. Test run with the Microsoft.CodeCoverage collector:

   ```powershell
   dotnet test tests\WeatherParser.Parsing.Tests\WeatherParser.Parsing.Tests.csproj `
     --collect:"Code Coverage" --results-directory .\TestResults
   ```

   Result: 8/8 tests passed; a binary `.coverage` raw report artifact is produced under
   `TestResults/<guid>/*.coverage` (`XPlat Code Coverage`, the coverlet collector name, is not
   registered for this project because the library references `Microsoft.CodeCoverage` instead of
   `coverlet.collector`; the correct collector name is `Code Coverage`).

2. Conversion of the raw report with the `dotnet-coverage` tool (installed globally) into two
   human-readable formats:

   ```powershell
   dotnet-coverage merge <guid>.coverage -f xml      -o TestResults\coverage.xml
   dotnet-coverage merge <guid>.coverage -f cobertura -o TestResults\coverage.cobertura.xml
   ```

## Proven Report Content

### Code block breakdown (`coverage.xml`, native Microsoft format)

The report contains, per module, a list of functions, and per function a list of `<ranges>` with
line/column range and `covered="yes|no"` marking – this is the code block level. Example from
`ParserEndpointValidatorTests.TestInitialize()`:

```xml
<function id="8930" name="TestInitialize()" type_name="ParserEndpointValidatorTests"
          block_coverage="100.00" line_coverage="100.00"
          blocks_covered="4" blocks_not_covered="0">
  <ranges>
    <range source_id="1" start_line="15" end_line="15" start_column="5" end_column="6" covered="yes" />
    <range source_id="1" start_line="16" end_line="16" start_column="9" end_column="82" covered="yes" />
  </ranges>
</function>
```

### Module summary (proven counter mapping per assembly)

| Module | block_coverage | line_coverage | blocks_covered/not_covered | lines_covered/partial/not |
|---|---|---|---|---|
| `WeatherParser.Parsing.Tests.dll` | 97.93 % | 94.67 % | 189 / 4 | 71 / 4 / 0 |
| `WeatherParser.Parsing.dll` (production code) | 63.33 % | 50.00 % | 57 / 33 | 31 / 3 / 28 |

Both assemblies appear as separate `<module>` entries with their own module ID (SHA-1 hash of the
module); no double counting of the library assembly between source and package tests, since only
`WeatherParser.Parsing.Tests` (source reference) participates in this run.

### Cobertura format (`coverage.cobertura.xml`, tool compatibility)

The same counters are additionally available in the widely used Cobertura schema (`<package>` per
assembly, `<class>` per type, `<method>`/`<line hits="..." branch="...">` per line), e.g.:

```xml
<package line-rate="0.5483870967741935" branch-rate="1" complexity="14" name="WeatherParser.Parsing">
```

This confirms that the same raw report result can be represented losslessly in two independent,
tool-compatible formats.

## Findings

- The correct collector name for `Microsoft.CodeCoverage` is `"Code Coverage"`, not
  `"XPlat Code Coverage"` (that is the `coverlet.collector` name and does not apply to this project,
  since `coverlet.collector` is not referenced).
- The native `.coverage` format is already the proven raw report artifact; `dotnet-coverage merge`
  converts it losslessly into human-/tool-readable XML/Cobertura formats.
- Production assembly and test assembly appear as separate modules with their own counter mapping;
  no mixing of test code and production code counters.
- The current production code metric (`WeatherParser.Parsing.dll`: 63.33 % block, 50.00 % line
  coverage) is below the 80 % threshold required by section 5 of the detailed plan. This
  evaluation, as well as the 80 %/100 %/documentation exception rules, is explicitly assigned to
  P1.08 and is not claimed as passed here.

## Acceptance Criteria (P1.07)

- [x] Microsoft.CodeCoverage produces a real raw report (`.coverage`).
- [x] The report contains a code block breakdown (function/range level) in addition to the line
      level.
- [x] Counters are traceably attributed per assembly (production vs. test code), without double
      counting.
- [x] Report is available in at least one tool-compatible, evaluable format (Cobertura).
- [ ] Coverage evaluation including minimum threshold and exceptions (P1.08, not part of this
      step).

## Artifacts

- `TestResults/<guid>/*.coverage` – native raw report artifact (local, not versioned).
- `TestResults/coverage.xml` – converted raw report with code block breakdown.
- `TestResults/coverage.cobertura.xml` – converted report in Cobertura schema.

All `TestResults` artifacts are local run outputs and are not committed to the repository.
