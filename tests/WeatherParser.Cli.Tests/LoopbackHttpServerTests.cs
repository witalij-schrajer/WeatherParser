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
}
