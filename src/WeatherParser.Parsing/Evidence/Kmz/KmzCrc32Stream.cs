using System.IO;
using WeatherParser.Parsing.Catalogs;

namespace WeatherParser.Parsing.Evidence.Kmz;

/// <summary>
/// Computes the CRC-32 (IEEE 802.3) of the unpacked .kml member while streaming and compares it with the central-directory value at the end of the member; the .NET runtime does not verify it when reading. Used only by the MOSMIX forecast reader (contract MF-06); the P4 evidence readers do not enable it.
/// </summary>
internal sealed class KmzCrc32Stream : ReadOnlyStreamBase
{
    private readonly Stream _inner;
    private readonly uint _expected;
    private readonly Func<string, Exception> _invalid;
    private readonly uint[] _table;
    private uint _crc = 0xFFFFFFFFu;
    private bool _checked;

    /// <summary>
    /// Initializes a new instance of the <see cref="KmzCrc32Stream"/> class.
    /// </summary>
    /// <param name="inner">The inner stream to read from.</param>
    /// <param name="expectedCrc">The expected CRC-32 value.</param>
    /// <param name="invalid">A function that creates an exception for invalid cases.</param>
    public KmzCrc32Stream(Stream inner, uint expectedCrc, Func<string, Exception> invalid)
    {
        _inner = inner;
        _expected = expectedCrc;
        _invalid = invalid;

        _table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint c = i;
            for (int j = 0; j < 8; j++)
            {
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            }
            _table[i] = c;
        }
    }

    /// <summary>
    /// The CRC-32 of the bytes read so far.
    /// </summary>
    public uint Value => _crc ^ 0xFFFFFFFFu;

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = _inner.Read(buffer, offset, count);
        for (int i = 0; i < read; i++)
        {
            byte b = buffer[offset + i];
            _crc = _table[(_crc ^ b) & 0xFF] ^ (_crc >> 8);
        }

        if (read == 0 && count > 0 && !_checked)
        {
            _checked = true;
            if (Value != _expected)
            {
                throw _invalid("the .kml member fails its CRC-32 check");
            }
        }

        return read;
    }
}
