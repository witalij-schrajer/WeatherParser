# Package consumer tests – P1.04

This MSTest project was created and added to `WeatherParser.slnx` in P1.04. It tests the actual locally built `WeatherParser.Parsing` package, version `0.1.0-initial`, using a versioned `PackageReference` and no `ProjectReference` to parser source. The version is declared centrally in `Directory.Packages.props`; `NuGet.Config` includes the project-local `artifacts/local-nuget` source.

The two tests in `PackageConsumptionTests.cs` verify that dependency metadata identifies the library as a restored package and that the expected library assembly exposes no CLI/broker-transport assembly references. P1.04 records 2 of 2 tests passing; see the [completion record](../../docs/p1/P1_04_PACKAGE_CONSUMER.md). Version, isolated-source and PDB/source provenance remain part of package verification; the two tests alone do not prove every provenance condition. This is the technical package consumer; the CLI usage example follows the completed library in P3.

Build, pack, consumer lock refresh and test procedure: [CONTRIBUTING.md, section 4](../../CONTRIBUTING.md#4-pack-and-package-consumer).
