# Package consumer tests – P1.04

[Deutsch](README.md) | [English](README.en.md)

This area is reserved for tests of the actual built NuGet package. Create the test project in P1.04 when local pack and isolated package restore are established together. It is not yet part of the solution.

The future consumer uses a versioned `PackageReference` to the local `.nupkg`, not a `ProjectReference` to parser source. Verify package version, isolated source and PDB/source identity. This is not the CLI usage example; that follows the completed library in P3.
