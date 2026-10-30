using System.Net;
using System.Text;
using FluentAssertions;

namespace WeatherParser.Cli.Tests;

[TestClass]
public sealed class LoopbackHttpServerTests
{
    [TestMethod]
    [TestCategory("T-P3-20")]
    public async Task Server_CountsRequestsAndUsesResponder()
    {
        await using var server = new LoopbackHttpServer { Responder = path => (200, Encoding.UTF8.GetBytes(path)) };

        using var client = new HttpClient();
        var response = await client.GetAsync(new Uri(server.BaseUri, "a/b?c=1"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("/a/b?c=1");
        server.RequestCount.Should().Be(1);
    }

    [TestMethod]
    [TestCategory("T-P3-20")]
    public async Task Server_Redirect_SendsLocationAndRecordsPath()
    {
        await using var server = new LoopbackHttpServer { Redirect = path => path == "/moved" ? "/elsewhere" : null };
        using var client = new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false });
        var response = await client.GetAsync(new Uri(server.BaseUri, "moved"));
        ((int)response.StatusCode).Should().Be(302);
        response.Headers.Location!.OriginalString.Should().Be("/elsewhere");
        server.RequestedPaths.Should().Equal("/moved");
    }

    [TestMethod]
    [TestCategory("T-P3-20")]
    public async Task Server_BaseUri_IsNumericLoopbackAddress()
    {
        await using var server = new LoopbackHttpServer();
        server.BaseUri.Host.Should().Be("127.0.0.1");
        IPAddress.Parse(server.BaseUri.Host).Should().Be(IPAddress.Loopback);
    }
}
