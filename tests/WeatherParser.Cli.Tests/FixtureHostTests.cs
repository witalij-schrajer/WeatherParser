using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace WeatherParser.Cli.Tests;

/// <summary>
/// Tests the fixture host behavior.
/// </summary>
[TestClass]
[TestCategory("T-P9-13")]
public sealed class FixtureHostTests
{
    private string _root = null!;
    private string _outsideRoot = null!;
    private Process? _process;
    private StringBuilder _stderr = null!;
    private List<string> _stdoutLines = null!;

    /// <summary>
    /// Initializes the test fixture.
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "weatherparser-fixturehost-" + Guid.NewGuid().ToString("N"));
        _outsideRoot = _root + "-outside";
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(Path.Combine(_root, "data"));
        Directory.CreateDirectory(_outsideRoot);
        File.WriteAllBytes(Path.Combine(_root, "data", "sample.txt"), ProcessFixtures.Utf8("fixture-bytes-1"));
        File.WriteAllBytes(Path.Combine(_outsideRoot, "secret.txt"), ProcessFixtures.Utf8("outside"));
        _stderr = new StringBuilder();
        _stdoutLines = new List<string>();
    }

    /// <summary>
    /// Cleans up the test fixture.
    /// </summary>
    [TestCleanup]
    public void TestCleanup()
    {
        if (_process != null)
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                }
                _process.WaitForExit();
            }
            finally
            {
                _process.Dispose();
            }
        }

        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // ignore
        }

        try
        {
            Directory.Delete(_outsideRoot, recursive: true);
        }
        catch
        {
            // ignore
        }
    }

    /// <summary>
    /// Starts the fixture host process.
    /// </summary>
    private async Task<Uri> StartHostAsync(string root)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = new UTF8Encoding(false),
            StandardErrorEncoding = new UTF8Encoding(false)
        };

        startInfo.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "WeatherParser.Cli.FixtureHost.dll"));
        startInfo.ArgumentList.Add(root);
        startInfo.ArgumentList.Add("0");

        _process = Process.Start(startInfo)!;
        _process.StandardInput.Close();
        _process.ErrorDataReceived += ErrorDataReceived;
        _process.BeginErrorReadLine();

        var stdoutLines = new List<string>();
        string? line;
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
        {
            while ((line = await _process.StandardOutput.ReadLineAsync(cts.Token)) != null)
            {
                lock (_stderr)
                {
                    _stdoutLines.Add(line);
                }

                if (line.StartsWith("LISTENING "))
                {
                    break;
                }
            }

            if (line == null)
            {
                Assert.Fail($"Expected LISTENING line, got: {string.Join("\n", _stdoutLines)}");
            }

            if (!line.StartsWith("LISTENING "))
            {
                Assert.Fail($"Expected LISTENING line, got: {string.Join("\n", _stdoutLines)}");
            }
        }

        var uri = new Uri(line.Substring("LISTENING ".Length));
        return uri;
    }

    /// <summary>
    /// Performs a raw HTTP GET request.
    /// </summary>
    private async Task<int> RawGetAsync(Uri baseUri, string rawPath)
    {
        using var client = new TcpClient();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await client.ConnectAsync(IPAddress.Loopback, baseUri.Port, cts.Token);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            Assert.Fail("Connection timed out");
        }
        catch (SocketException)
        {
            Assert.Fail("Connection failed");
        }

        using var stream = client.GetStream();

        var request = $"GET {rawPath} HTTP/1.1\r\nHost: 127.0.0.1\r\nConnection: close\r\n\r\n";
        var requestBytes = Encoding.ASCII.GetBytes(request);
        await stream.WriteAsync(requestBytes);

        var response = new List<byte>();
        var buffer = new byte[1024];
        int read;
        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            response.AddRange(buffer.Take(read));
        }

        var responseString = Encoding.ASCII.GetString(response.ToArray());
        var firstLine = responseString.Substring(0, responseString.IndexOf('\r'));
        var statusCode = int.Parse(firstLine.Split(' ')[1]);
        return statusCode;
    }

    /// <summary>
    /// Performs a raw HTTP request with a custom request string.
    /// </summary>
    private async Task<int> RawRequestAsync(Uri baseUri, string request)
    {
        using var client = new TcpClient();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await client.ConnectAsync(IPAddress.Loopback, baseUri.Port, cts.Token);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            Assert.Fail("Connection timed out");
        }
        catch (SocketException)
        {
            Assert.Fail("Connection failed");
        }

        using var stream = client.GetStream();

        var requestBytes = Encoding.ASCII.GetBytes(request);
        await stream.WriteAsync(requestBytes);

        var response = new List<byte>();
        var buffer = new byte[1024];
        int read;
        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            response.AddRange(buffer.Take(read));
        }

        var responseString = Encoding.ASCII.GetString(response.ToArray());
        var firstLine = responseString.Substring(0, responseString.IndexOf('\r'));
        var statusCode = int.Parse(firstLine.Split(' ')[1]);
        return statusCode;
    }

    /// <summary>
    /// Creates a request head of exactly the specified length.
    /// </summary>
    private static string HeadOfLength(int length)
    {
        var head = "GET /data/sample.txt HTTP/1.1\r\nHost: 127.0.0.1\r\nConnection: close\r\nX-Pad: ";
        var padLength = length - head.Length - 4; // -4 for \r\n\r\n
        if (padLength < 0)
        {
            Assert.Fail($"Cannot create a head of {length} bytes, minimum is {head.Length + 4}");
        }

        return head + new string('a', padLength) + "\r\n\r\n";
    }

    /// <summary>
    /// Verifies that a request head of exactly 8192 bytes, the limit, is accepted.
    /// </summary>
    [TestMethod]
    [Timeout(90000)]
    public async Task FixtureHost_HeadOf8192Bytes_Accepted()
    {
        var uri = await StartHostAsync(_root);
        var request = HeadOfLength(8192);
        request.Length.Should().Be(8192);
        var statusCode = await RawRequestAsync(uri, request);
        statusCode.Should().Be(200);
    }

    /// <summary>
    /// Verifies that a request head one byte over the 8192-byte limit is rejected with 400.
    /// </summary>
    [TestMethod]
    [Timeout(90000)]
    public async Task FixtureHost_HeadOf8193Bytes_Rejected()
    {
        var uri = await StartHostAsync(_root);
        var request = HeadOfLength(8193);
        request.Length.Should().Be(8193);
        var statusCode = await RawRequestAsync(uri, request);
        statusCode.Should().Be(400);
    }

    /// <summary>
    /// Verifies that the fixture host serves files only on loopback.
    /// </summary>
    [TestMethod]
    [Timeout(90000)]
    public async Task FixtureHost_ServesFileFromDirectory_OnLoopbackOnly()
    {
        var uri = await StartHostAsync(_root);
        uri.Host.Should().Be("127.0.0.1");

        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(new Uri(uri, "data/sample.txt"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsByteArrayAsync();
        content.Should().Equal(ProcessFixtures.Utf8("fixture-bytes-1"));

        // Check that it's not listening on other interfaces
        foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (adapter.OperationalStatus == OperationalStatus.Up && adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            {
                foreach (var unicast in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        using var client = new TcpClient();
                        using var connectTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        var connected = false;
                        try
                        {
                            await client.ConnectAsync(unicast.Address, uri.Port, connectTimeout.Token);
                            connected = true;
                        }
                        catch (SocketException)
                        {
                            // Expected: nothing listens on this address.
                        }
                        catch (OperationCanceledException)
                        {
                            // Expected: the connection attempt did not succeed within the bound.
                        }

                        connected.Should().BeFalse("the host must listen on the loopback interface only, not on " + unicast.Address);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Verifies that the fixture host returns 404 for unknown paths.
    /// </summary>
    [TestMethod]
    [Timeout(90000)]
    public async Task FixtureHost_UnknownPath_Returns404()
    {
        var uri = await StartHostAsync(_root);

        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(new Uri(uri, "data/missing.txt"));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        response = await httpClient.GetAsync(new Uri(uri, "data/"));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Verifies that the fixture host rejects path traversal attempts.
    /// </summary>
    [TestMethod]
    [Timeout(90000)]
    public async Task FixtureHost_PathTraversal_Rejected()
    {
        var uri = await StartHostAsync(_root);

        var statusCode = await RawGetAsync(uri, "/../" + Path.GetFileName(_outsideRoot) + "/secret.txt");
        statusCode.Should().Be(400);

        statusCode = await RawGetAsync(uri, "/data/%2e%2e/%2e%2e/" + Path.GetFileName(_outsideRoot) + "/secret.txt");
        statusCode.Should().Be(400);
    }

    /// <summary>
    /// Verifies that the fixture host announces its port on stdout.
    /// </summary>
    [TestMethod]
    [Timeout(90000)]
    public async Task FixtureHost_AnnouncesPortOnStdout()
    {
        var uri = await StartHostAsync(_root);

        uri.Scheme.Should().Be("http");
        uri.Host.Should().Be("127.0.0.1");
        uri.Port.Should().BeGreaterThan(0);
        uri.Port.Should().BeLessThan(65536);
        uri.AbsolutePath.Should().Be("/");

        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(new Uri(uri, "data/sample.txt"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // The host logs a request after its response, so the line is awaited while the host still runs.
        var found = false;
        for (int i = 0; i < 200 && !found; i++)
        {
            lock (_stderr)
            {
                found = _stderr.ToString().Contains("GET /data/sample.txt 200");
            }

            if (!found)
            {
                await Task.Delay(50);
            }
        }

        found.Should().BeTrue("the host logs every request on standard error");

        _process!.Kill(entireProcessTree: true);
        await _process.WaitForExitAsync();

        var stdout = await _process.StandardOutput.ReadToEndAsync();
        var lines = stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
        foreach (var line in lines)
        {
            lock (_stderr)
            {
                _stdoutLines.Add(line);
            }
        }

        var announcedUriString = _stdoutLines.Single(l => l.StartsWith("LISTENING ")).Substring("LISTENING ".Length);
        var announcedUri = new Uri(announcedUriString);
        announcedUriString.Should().MatchRegex(@"^http://127\.0\.0\.1:[0-9]+/$");

        _stdoutLines.Should().ContainSingle().Which.Should().MatchRegex(@"^LISTENING http://127\.0\.0\.1:[0-9]+/$");
    }

    /// <summary>
    /// Verifies that the fixture host exits with code 0 on SIGTERM.
    /// </summary>
    [TestMethod]
    [Timeout(90000)]
    public async Task FixtureHost_Sigterm_ExitsZero()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Inconclusive("POSIX signals are sent to a child process on Linux only; on Windows the host is stopped by Ctrl+C in a console, which a test cannot send to a redirected child.");
        }

        var uri = await StartHostAsync(_root);
        PosixSignalSender.Send(_process!.Id, PosixSignalSender.Sigterm);
        using var exitTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await _process.WaitForExitAsync(exitTimeout.Token);
        _process.ExitCode.Should().Be(0);
    }

    /// <summary>
    /// Verifies that the fixture host exits with code 0 on SIGINT.
    /// </summary>
    [TestMethod]
    [Timeout(90000)]
    public async Task FixtureHost_Sigint_ExitsZero()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Inconclusive("POSIX signals are sent to a child process on Linux only; on Windows the host is stopped by Ctrl+C in a console, which a test cannot send to a redirected child.");
        }

        var uri = await StartHostAsync(_root);
        PosixSignalSender.Send(_process!.Id, PosixSignalSender.Sigint);
        using var exitTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await _process.WaitForExitAsync(exitTimeout.Token);
        _process.ExitCode.Should().Be(0);
    }

    /// <summary>
    /// Verifies that the CLI produces a document when selecting DWD data against the fixture host.
    /// </summary>
    [TestMethod]
    [Timeout(90000)]
    public async Task Cli_SelectDwd_AgainstFixtureHost_ProducesDocument()
    {
        var workingDir = Path.Combine(Path.GetTempPath(), "weatherparser-cli-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDir);

        try
        {
            var uri = await StartHostAsync(Path.Combine(AppContext.BaseDirectory, "fixtures"));

            var configPath = Path.Combine(workingDir, "config.json");
            File.WriteAllText(configPath, ProcessFixtures.DwdConfiguration(uri));

            var result = await CliProcess.RunCliAsync(
                new[] { "--SelectDWD", "--Latitude", "50.55", "--Longitude", "9.67", "--Altitude", "300", "--MaxDistance", "50", "--MaxAltDifference", "200", "--Output", "json", "--Config", "config.json" },
                workingDir,
                TimeSpan.FromSeconds(60),
                new Dictionary<string, string?> { ["WEATHERPARSER_CONFIG"] = null });

            result.TimedOut.Should().BeFalse();
            result.ExitCode.Should().Be(0);
            var parse = () => { using var document = JsonDocument.Parse(result.StandardOutput); };
            parse.Should().NotThrow();
            using (var document = JsonDocument.Parse(result.StandardOutput))
            {
                document.RootElement.GetProperty("stations").GetArrayLength().Should().Be(0, "a static fixture set has no now rows of the last 3 hours, so no station is active");
            }

            string hostLog;
            lock (_stderr)
            {
                hostLog = _stderr.ToString();
            }

            hostLog.Should().Contain("GET /cdc/air_temperature/now/zehn_now_tu_Beschreibung_Stationen.txt 200");
            hostLog.Should().Contain("GET /cdc/wind/now/zehn_now_ff_Beschreibung_Stationen.txt 200");
            hostLog.Should().Contain("GET /cdc/precipitation/now/zehn_now_rr_Beschreibung_Stationen.txt 200");
            hostLog.Should().Contain("GET /cdc/solar/now/zehn_now_sd_Beschreibung_Stationen.txt 200");
            result.StandardError.Should().NotContain(" ERROR ");
        }
        finally
        {
            Directory.Delete(workingDir, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that --ParseDWD against the fixture host, whose station 90001 has no now archives, reports an item failure in the export document (CL-09 amendment of 2026-10-31).
    /// </summary>
    [TestMethod]
    [Timeout(90000)]
    public async Task Cli_ParseDwd_AgainstFixtureHost_NoUsableReading_ItemFailure()
    {
        var workingDir = Path.Combine(Path.GetTempPath(), "weatherparser-cli-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDir);

        try
        {
            var uri = await StartHostAsync(Path.Combine(AppContext.BaseDirectory, "fixtures"));

            var configPath = Path.Combine(workingDir, "config.json");
            File.WriteAllText(configPath, ProcessFixtures.DwdConfiguration(uri));

            var result = await CliProcess.RunCliAsync(
                new[] { "--ParseDWD", "--StationID", "90001", "--Output", "json", "--Config", "config.json" },
                workingDir,
                TimeSpan.FromSeconds(60),
                new Dictionary<string, string?> { ["WEATHERPARSER_CONFIG"] = null });

            result.TimedOut.Should().BeFalse();
            result.ExitCode.Should().Be(1);
            var parse = () => { using var document = JsonDocument.Parse(result.StandardOutput); };
            parse.Should().NotThrow();
            using (var document = JsonDocument.Parse(result.StandardOutput))
            {
                var errors = document.RootElement.GetProperty("errors");
                errors.GetArrayLength().Should().Be(1);
                var error = errors[0];
                error.GetProperty("error_id").GetString().Should().Be("OBSERVATION-NO-USABLE-READING");
                error.GetProperty("source").GetString().Should().Be("dwd:90001");
                document.RootElement.GetProperty("observations").GetArrayLength().Should().Be(0);
            }

            result.StandardError.Should().Contain(" ERROR OBSERVATION-NO-USABLE-READING ");
            result.StandardError.Should().NotContain("CLI-UNEXPECTED");
        }
        finally
        {
            Directory.Delete(workingDir, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that the CLI produces a non-empty OsthessenWetter document from the synthetic page served by the fixture host.
    /// </summary>
    [TestMethod]
    [Timeout(90000)]
    public async Task Cli_ParseOstHessen_AgainstFixtureHost_ProducesDocument()
    {
        var workingDir = Path.Combine(Path.GetTempPath(), "weatherparser-cli-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDir);

        try
        {
            var uri = await StartHostAsync(Path.Combine(AppContext.BaseDirectory, "fixtures"));
            var configuration = ProcessFixtures.OsthessenConfiguration(uri, mqtt: false, publicationPermitted: false).Replace("osthessen/\"", "osthessen/index.html\"", StringComparison.Ordinal);
            File.WriteAllText(Path.Combine(workingDir, "config.json"), configuration);

            var result = await CliProcess.RunCliAsync(
                new[] { "--ParseOstHessen", "--StationNamePattern", "M*", "--Output", "json", "--Config", "config.json" },
                workingDir,
                TimeSpan.FromSeconds(60),
                new Dictionary<string, string?> { ["WEATHERPARSER_CONFIG"] = null });

            result.TimedOut.Should().BeFalse();
            result.ExitCode.Should().Be(0, result.StandardError);
            using (var document = JsonDocument.Parse(result.StandardOutput))
            {
                document.RootElement.GetProperty("observations").GetArrayLength().Should().BeGreaterThan(0);
            }

            result.StandardOutput.Should().Contain("Müs-Hohhütte");
            result.StandardError.Should().NotContain(" ERROR ");
        }
        finally
        {
            Directory.Delete(workingDir, recursive: true);
        }
    }

    /// <summary>
    /// Handles error data received from the process.
    /// </summary>
    private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data != null)
        {
            lock (_stderr)
            {
                _stderr.AppendLine(e.Data);
            }
        }
    }
}
