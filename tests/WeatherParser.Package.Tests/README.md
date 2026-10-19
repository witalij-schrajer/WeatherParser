# Package consumer tests – P1.04

This MSTest project was created and added to `WeatherParser.slnx` in P1.04. It tests the actual locally built `WeatherParser.Parsing` package, version `0.1.0-initial`, using a versioned `PackageReference` and no `ProjectReference` to parser source. The version is declared centrally in `Directory.Packages.props`; `NuGet.Config` includes the project-local `artifacts/local-nuget` source.

The two tests in `PackageConsumptionTests.cs` verify that dependency metadata identifies the library as a restored package and that the expected library assembly exposes no CLI/broker-transport assembly references. P1.04 records 2 of 2 tests passing; see the [completion record](../../docs/p1/P1_04_PACKAGE_CONSUMER.md). Version, isolated-source and PDB/source provenance remain part of package verification; the two tests alone do not prove every provenance condition. This is the technical package consumer; the CLI usage example follows the completed library in P3.

From the repository root, the intended sequence builds the local package before restoring the consumer:

```powershell
dotnet restore src/WeatherParser.Parsing/WeatherParser.Parsing.csproj --locked-mode
dotnet pack src/WeatherParser.Parsing/WeatherParser.Parsing.csproj --configuration Release --no-restore --output artifacts/local-nuget
dotnet restore tests/WeatherParser.Package.Tests/WeatherParser.Package.Tests.csproj --locked-mode
dotnet test tests/WeatherParser.Package.Tests/WeatherParser.Package.Tests.csproj --configuration Release --no-restore
```

**Current reproducibility limitation (2026-10-19):** `packages.lock.json` records an earlier artifact of the same `0.1.0-initial` version. A fresh-cache locked restore against the rebuilt package fails content-hash validation (`NU1403`). Reconcile package version/content and the consumer lock file in a reviewed packaging change before claiming the sequence above works from a clean cache. The documentation migration leaves lock files unchanged.

The recorded P1.04 result is historical. During the 2026-10-19 documentation migration, a freshly packed artifact passed both assertions in a separate console harness using a newly resolved temporary consumer, but the standard MSTest runner was blocked by a local testhost socket permission error; no new MSTest pass or successful fresh-cache locked restore is claimed.
