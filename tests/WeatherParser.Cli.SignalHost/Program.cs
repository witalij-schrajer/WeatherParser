using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using WeatherParser.Cli.Lifetime;

// Test host of CL-18: the real shutdown signal and process terminator with work that ignores cancellation, so only a second signal or the end of the grace period can end the process.
// Arguments: <ready file> <grace period in seconds>; the ready file is written once the signals are registered.
using var signal = new ShutdownSignal(new PosixSignalRegistrar(), new ProcessTerminator(), TimeProvider.System, OperatingSystem.IsWindows());
signal.Configure(TimeSpan.FromSeconds(int.Parse(args[1], CultureInfo.InvariantCulture)), NullLogger.Instance);
File.WriteAllText(args[0], "ready");
Thread.Sleep(Timeout.Infinite);
return 0;
