using System;

namespace WeatherParser.Cli;

public sealed class EnvironmentReader : IEnvironmentReader
{
    public string? Get(string name) => Environment.GetEnvironmentVariable(name);
}
