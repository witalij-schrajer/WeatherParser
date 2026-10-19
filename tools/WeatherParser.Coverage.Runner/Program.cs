using WeatherParser.Coverage.Runner;
using System;
using System.Threading;
using System.Threading.Tasks;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    cts.Cancel();
    e.Cancel = true;
};

return await RunnerComposition.Create(Console.Out, Console.Error).RunAsync(args, cts.Token);
