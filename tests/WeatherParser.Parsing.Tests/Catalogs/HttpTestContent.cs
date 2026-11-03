namespace WeatherParser.Parsing.Tests.Catalogs;

using System.Net;

internal static class HttpTestContent
{
    public static byte[] Bytes(int count) => Enumerable.Range(0, count).Select(i => (byte)(i % 251)).ToArray();

    public static HttpResponseMessage Ok(HttpContent content) => new(HttpStatusCode.OK) { Content = content };

    public static HttpContent Known(byte[] body) => new ByteArrayContent(body);

    public static HttpContent WithLength(byte[] body, long contentLength)
    {
        var content = new ByteArrayContent(body);
        content.Headers.ContentLength = contentLength;
        return content;
    }

    public static HttpContent Unknown(byte[] body) => new StreamContent(new NonSeekableStream(body));

    internal sealed class NonSeekableStream(byte[] body) : MemoryStream(body)
    {
        public override bool CanSeek => false;
    }

    internal sealed class TrackingStream(byte[] body) : MemoryStream(body)
    {
        public bool Disposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
