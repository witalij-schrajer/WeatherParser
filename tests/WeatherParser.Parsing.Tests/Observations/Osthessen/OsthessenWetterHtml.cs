using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherParser.Parsing.Tests.Observations.Osthessen;

internal static class OsthessenWetterHtml
{
    public static string OverviewHeader() => "<th class='tg-iohx'>&nbsp;</th> <th class='tg-iohx'>Wetterstation<br></th><th>Temp.2m<br><font size='-2'></b>&Auml;nderung<b></th</font><th class='tg-iohx'>Temp.-<br>Trend</th><th class='tg-iohx'>Tmax/<br>Tmin</th><th class='tg-iohx'> Spitzen-<br>b&ouml;e</th><th class='tg-iohx'>Nieder-<br>schlag</th> <th class='tg-iohx'>Luft-<br>feuchte</th> </tr>";

    public static string WindHeader() => "<th class='tg-iohx'>&nbsp;</th> <th class='tg-iohx'>Wetterstation<br><font size='1em'>letzte Daten</font></th><th>Spitzen-<br>b&ouml;e<br><font size='1em'>heute</font></th><th class='tg-iohx'>Spitzen-<br>b&ouml;e<br><font size='1em'>verg.10Min.</font></th><th class='tg-iohx'>Wind<br>&#216;<br><font size='1em'>10 Min.</font></th><th class='tg-iohx'>Wind<br>&#216;<br><font size='1em'>60 Min.</font></th></tr>";

    public static string Value(string number, string unit) => string.IsNullOrEmpty(number) ? "" : number + "<font size='-2'>" + unit + "</font>";

    public static string OverviewRow(string nameHtml, string status = "Station online", string time = "09:31 Uhr", string temperature = "10.3", string change = "+1", string humidity = "93")
    {
        return $"<tr> <td class='tg-iohx'><img src='/uploads/s.jpg' title='{status}' width='18' height='18' border='0'></td> " +
               $"<td align=center><b><font size='2em'>{nameHtml}</font><br><font size='1'>{time}</font></td> " +
               $"<td class='tg-iohx'><font size='3'><b>{Value(temperature, "&deg;C")}</b></font><br><font size='1'>{Value(change, "&deg;C/h")}</b></font></td> " +
               $"<td class='tg-iohx'><img src='/uploads/t.gif' title='{change}&deg;C/h' width='30' height='27' border='0'></td> " +
               $"<td class='tg-iohx'><font color=#FF0000>11.1</font><font size='-2' color=#FF0000>&deg;C</font><br><font color=#0000FF>6.8</font><font size='-2' color=#0000FF>&deg;C</font></td></td> " +
               $"<td class='tg-iohx'>57.9<font size='-2'>km/h</font></td> " +
               $"<td class='tg-iohx'>8.4<font size='-2'>mm</font></td> " +
               $"<td class='tg-iohx'>" + Value(humidity, "%") + "</td> </tr>";
    }

    public static string WindRow(string nameHtml, string status = "Station online", string time = "09:31 Uhr", string mean10 = "23.1")
    {
        return $"<tr> " +
               $"<td class='tg-iohx'><img src='/uploads/s.jpg' title='{status}' width='18' height='18' border='0'></td> " +
               $"<td align=center><b><font size='2em'>{nameHtml}</font><br><font size='1'>{time}</font></td> " +
               $"<td class='tg-iohx'><font size='3'><b>57.9<font size='-2'>km/h</font></b></font></td> " +
               $"<td class='tg-iohx'>30.6<font size='-2'>km/h</font></td> " +
               $"<td class='tg-iohx'>{Value(mean10, "km/h")}</td></td> " +
               $"<td class='tg-iohx'>24.0<font size='-2'>km/h</font></td> </tr>";
    }

    public static string Table(string header, params string[] rows)
    {
        return "<table width='100%' class='tg'> " + header + string.Concat(rows) + "</table>";
    }

    public const string ErrorText = "Connection failed: Access denied for user 'web000'@'db.example.invalid' (using password: YES)";

    public static string Page(string? overviewTable, string? windTable)
    {
        var overviewHeaderEscaped = OverviewHeader().Replace("\"", "\\\"");
        return "<!DOCTYPE html><html><head><meta charset=\"UTF-8\"><title>Messwerte</title><link rel='stylesheet' href='http://unreachable.invalid/a.css'><script>document.write(\"<table width='100%' class='tg'>" + overviewHeaderEscaped + "<tr><td><img title='Station online'></td><td><font size='2em'>Injected</font><br><font size='1'>09:00 Uhr</font></td><td></td><td></td><td></td><td></td><td></td><td></td></tr></table>\");</script></head><body><img src='http://unreachable.invalid/i.png'><iframe src='http://unreachable.invalid/f.html'></iframe>" +
               (overviewTable ?? "") +
               "<table width='100%' class='tg'> <th class='tg-031e' colspan='6'><b>&#216;-Schneefallgrenze in Osthessen: </b></td><td class='tg-iohx'>1977 m</td></th></tr></table>" +
               (windTable ?? "") +
               "<p>Heutige Spitzenb&ouml;en " + ErrorText + "</p>" +
               "</body></html>";
    }

    public static string DefaultPage() => Page(
        Table(OverviewHeader(),
            OverviewRow("Ro&szligbach"),
            OverviewRow("M&#252;s-Hohh&#252;tte", time: "09:30 Uhr", temperature: "-4.2", change: "-0.5", humidity: "0"),
            OverviewRow("Langenbieber", status: "Station offline", time: "", temperature: "", change: "", humidity: "")),
        Table(WindHeader(),
            WindRow("M&uuml;s-Hohh&uumltte;", time: "09:30 Uhr", mean10: "0.0"),
            WindRow("Rossbach", mean10: "12.5"),
            WindRow("Langenbieber", status: "Station offline", time: "", mean10: "")));

    public static MemoryStream ToStream(string html) => new(Encoding.UTF8.GetBytes(html));

    internal sealed class AsyncOnlyStream : Stream
    {
        private readonly MemoryStream _inner;

        public AsyncOnlyStream(byte[] bytes)
        {
            _inner = new MemoryStream(bytes);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            // Does nothing as per requirements
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("Synchronous read.");
        }

        public override int Read(Span<byte> buffer)
        {
            throw new InvalidOperationException("Synchronous read.");
        }

        public override int ReadByte()
        {
            throw new InvalidOperationException("Synchronous read.");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new ValueTask<int>(_inner.Read(buffer.Span));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    internal sealed class PendingStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("Synchronous read.");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var task = Task.Delay(Timeout.Infinite, cancellationToken);
            await task.ConfigureAwait(false);
            throw new OperationCanceledException(cancellationToken);
        }
    }
}
