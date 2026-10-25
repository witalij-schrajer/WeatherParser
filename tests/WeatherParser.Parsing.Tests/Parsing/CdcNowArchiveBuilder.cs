using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace WeatherParser.Parsing.Tests.Parsing;

internal static class CdcNowArchiveBuilder
{
    public static string Header(string productCode)
    {
        return productCode switch
        {
            "tu" => "STATIONS_ID;MESS_DATUM;  QN;PP_10;TT_10;TM5_10;RF_10;TD_10;eor",
            "ff" => "STATIONS_ID;MESS_DATUM;  QN;FF_10;DD_10;eor",
            "rr" => "STATIONS_ID;MESS_DATUM;  QN;RWS_DAU_10;RWS_10;RWS_IND_10;eor",
            "sd" => "STATIONS_ID;MESS_DATUM;  QN;DS_10;GS_10;SD_10;LS_10;eor",
            _ => throw new ArgumentException("Unknown product code.", nameof(productCode))
        };
    }

    public static string Row(string stationId, string messDatum, params string[] values)
    {
        return $"{stationId,10};{messDatum};    3;" + string.Join(";", values.Select(v => v.PadLeft(8))) + ";eor";
    }

    public static string RowWithQn(string stationId, string messDatum, string qn, params string[] values)
    {
        return $"{stationId,10};{messDatum};{qn,5};" + string.Join(";", values.Select(v => v.PadLeft(8))) + ";eor";
    }

    public static string MemberName(string productCode, string stationId)
    {
        return $"produkt_zehn_now_{productCode}_20250420_20261021_{stationId}.txt";
    }

    public static byte[] Content(string header, IEnumerable<string> rows, string lineEnding = "\r\n")
    {
        var sb = new StringBuilder();
        sb.Append(header);
        sb.Append(lineEnding);
        foreach (var row in rows)
        {
            sb.Append(row);
            sb.Append(lineEnding);
        }

        return Encoding.Latin1.GetBytes(sb.ToString());
    }

    public static byte[] Zip(params (string Name, byte[] Content)[] members)
    {
        return Zip(CompressionLevel.Optimal, members);
    }

    public static byte[] Zip(CompressionLevel level, params (string Name, byte[] Content)[] members)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in members)
            {
                var entry = archive.CreateEntry(name, level);
                using var entryStream = entry.Open();
                entryStream.Write(content, 0, content.Length);
            }
        }

        return ms.ToArray();
    }

    public static byte[] Valid(string productCode = "tu", string stationId = "00044", int rowCount = 3)
    {
        var header = Header(productCode);
        var rows = new List<string>();
        for (int i = 0; i < rowCount; i++)
        {
            var messDatum = new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc).AddMinutes(10 * i).ToString("yyyyMMddHHmm");
            var values = productCode switch
            {
                "tu" => new[] { "1001.2", "12.3", "10.1", "85.0", "8.9" },
                "ff" => new[] { "3.4", "270" },
                "rr" => new[] { "10", "0.0", "0" },
                "sd" => new[] { "0.0", "12.5", "0.1", "500" },
                _ => throw new ArgumentException("Unknown product code.", nameof(productCode))
            };
            rows.Add(Row(stationId, messDatum, values));
        }

        return Zip((MemberName(productCode, stationId), Content(header, rows)));
    }

    public static string Pad(int length) => new string('x', length);

    public static byte[] ContentOfLength(int targetLength, int rowCount = 1000)
    {
        var header = Header("tu").Replace(";eor", ";XX;eor");

        string RowWith(int i, int padLength) => Row(
            "44",
            new DateTime(2026, 10, 21, 0, 0, 0).AddMinutes(10 * i).ToString("yyyyMMddHHmm"),
            "1001.2", "12.3", "10.1", "85.0", "8.9", Pad(padLength));

        // With a padding of 8 characters PadLeft(8) adds nothing, so every further padding character adds exactly one byte.
        var baseLength = Content(header, Enumerable.Range(0, rowCount).Select(i => RowWith(i, 8))).Length;
        var free = targetLength - baseLength;
        if (free < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetLength));
        }

        var share = free / rowCount;
        var extra = free % rowCount;
        var content = Content(header, Enumerable.Range(0, rowCount).Select(i => RowWith(i, 8 + share + (i == rowCount - 1 ? extra : 0))));
        if (content.Length != targetLength)
        {
            throw new InvalidOperationException("Synthetic content does not have the requested length.");
        }

        return content;
    }
}
