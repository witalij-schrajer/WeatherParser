# P1.06 – Minimal Required Package/DI Contracts

## Goal

Secure the minimal dependency injection surface of the library through TDD: provide a single,
public registration entry point for `Microsoft.Extensions.DependencyInjection` consumers, and
prove with real `ServiceProvider` resolution that missing registrations are detected rather than
silently tolerated (`T-P1-04`, `T-P1-05`).

## Design

- The library (`src/WeatherParser.Parsing`) now references only
  `Microsoft.Extensions.DependencyInjection.Abstractions` (not the full DI container package),
  since production code only needs `IServiceCollection`. The concrete container
  (`Microsoft.Extensions.DependencyInjection`) remains a test-only dependency used to build a
  real `ServiceProvider` in tests.
- `ServiceCollectionExtensions.AddWeatherParsing(this IServiceCollection services)` is the single
  public registration entry point. It registers:
  - `IEndpointUriValidator` → `EndpointUriValidator` (singleton)
  - `IParserEndpointValidator` → `ParserEndpointValidator` (singleton)

No other services exist yet, so no other registrations were added; the extension method will
grow alongside future behavior (parsers, serializers) in later phases.

## Red → Green → Refactor

1. **Red**: `ServiceCollectionExtensionsTests` was written first, asserting that resolving
   `IParserEndpointValidator` from an empty `ServiceCollection`, and from a `ServiceCollection`
   with only `IParserEndpointValidator` registered (missing its `IEndpointUriValidator`
   dependency), both throw `InvalidOperationException`. The test file failed to compile because
   `AddWeatherParsing` did not exist yet.
2. **Green**: `ServiceCollectionExtensions.AddWeatherParsing` was implemented with the minimal
   registrations needed. Two further tests were added and passed: resolving
   `IParserEndpointValidator` succeeds after calling `AddWeatherParsing`, and the resolved
   instance performs real, correct endpoint validation (`ValidateAndMap`).
3. **Refactor**: The former `ParserEndpointValidatorDiTests` (from P1.05) duplicated coverage now
   provided more completely by `ServiceCollectionExtensionsTests` and was removed to avoid
   redundant tests.

## Test cases

| Test | Behavior |
|---|---|
| `GetRequiredService_WithoutAnyRegistration_ThrowsInvalidOperationException` | No registration at all → missing registration detected |
| `GetRequiredService_WithOnlyParserEndpointValidatorRegistered_ThrowsInvalidOperationException` | Partial registration (missing `IEndpointUriValidator`) → missing registration detected |
| `AddWeatherParsing_WithoutFurtherRegistration_ResolvesParserEndpointValidator` | Full registration via the public entry point resolves successfully |
| `AddWeatherParsing_ResolvedValidator_PerformsRealValidation` | The resolved instance is not just resolvable, but functionally correct |

All tests run against an in-memory `ServiceProvider`; no network, file, or device access occurs.

## Acceptance criteria

- The library references only DI abstractions, not the concrete DI container, in production code.
- `AddWeatherParsing` is the single public registration entry point for the services introduced
  so far.
- Missing registrations are provably detected via `InvalidOperationException` (no silent `null`
  resolution).
- Full registration resolves a functionally correct `IParserEndpointValidator`.
- All library, unit test, and package-consumer test projects build successfully; all 8 unit
  tests pass.

## Verification

```powershell
dotnet build src/WeatherParser.Parsing/WeatherParser.Parsing.csproj
dotnet build tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj
dotnet build tests/WeatherParser.Package.Tests/WeatherParser.Package.Tests.csproj
dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj
```

Result: all projects build; 8 tests executed, 8 passed, 0 failed.
