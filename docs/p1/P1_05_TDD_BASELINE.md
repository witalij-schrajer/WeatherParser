# P1.05 – TDD Baseline: Parser Endpoint Validation

## Goal

Establish a genuine, offline, Red→Green→Refactor-driven TDD baseline using MSTest, Moq and
FluentAssertions, and prove it on a real library behavior: validating and mapping parser
endpoint definitions (`T-P1-01` through `T-P1-04`, `T-P1-06`, `T-P1-07`).

## Behavior under test

A parser is configured with one or more named endpoints (`ParserEndpointDefinition`, each with
a `Name` and a `Uri`). The library must:

- Reject an endpoint whose URI is not a valid absolute HTTP/HTTPS URI, with a defined English
  error message and without attempting any network access (`T-P1-02`).
- Reject two endpoints that share the same name, since this would otherwise allow one endpoint
  configuration to silently replace another (`T-P1-03`).
- Map each valid, uniquely named endpoint definition by its name without confusing different
  endpoints with each other (`T-P1-03`).

## Design

- `IEndpointUriValidator` / `EndpointUriValidator` – validates that a URI is absolute and uses
  the `http` or `https` scheme.
- `ParserEndpointDefinition` – an immutable record describing a named endpoint and its URI.
- `IParserEndpointValidator` / `ParserEndpointValidator` – takes a collection of endpoint
  definitions, validates each URI via `IEndpointUriValidator`, and returns a map from endpoint
  name to definition. Throws `ParserEndpointValidationException` (English message) when a URI is
  invalid or a name is duplicated.

All dependencies are injected through the constructor (`T-P1-04`); there are no hidden service
lookups or static dependencies.

## Red → Green → Refactor

1. **Red**: `ParserEndpointValidatorTests` was written first against the not-yet-existing
   `ParserEndpointValidator` / `IEndpointUriValidator` contracts, covering a single valid
   endpoint, two distinct valid endpoints, an invalid URI, and a duplicate endpoint name. These
   tests failed to compile until the production types existed.
2. **Green**: `IEndpointUriValidator`, `EndpointUriValidator`, `ParserEndpointDefinition`,
   `IParserEndpointValidator`, `ParserEndpointValidator` and
   `ParserEndpointValidationException` were implemented with the minimal logic needed to satisfy
   the tests. All 6 tests pass.
3. **Refactor**: No further refactor was required; the implementation is already minimal and
   each class has a single, clear responsibility.

## Test setup and verification discipline (T-P1-06)

`ParserEndpointValidatorTests` uses `[TestInitialize]` to create a strict Moq mock of
`IEndpointUriValidator` and the system under test, and `[TestCleanup]` to call `VerifyAll()` on
the mock, ensuring every configured expectation was actually invoked.

## Dependency injection (T-P1-04 scope boundary, forward reference to P1.06)

`ParserEndpointValidatorDiTests` demonstrates that `IParserEndpointValidator` and
`IEndpointUriValidator` can be resolved from a real `Microsoft.Extensions.DependencyInjection`
`ServiceProvider` when properly registered, and that resolution fails with
`InvalidOperationException` when a required dependency (`IEndpointUriValidator`) is missing.
This is a narrow forward-looking check only; full DI registration for the whole application
remains in scope for P1.06 (`T-P1-05`).

## Offline execution (T-P1-07)

No test in this baseline performs any HTTP, MQTT or device access. All dependencies are either
mocked (Moq) or resolved in-memory via `ServiceCollection`.

## Verification

```powershell
dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj
```

Result: 6 tests executed, 6 passed, 0 failed.
