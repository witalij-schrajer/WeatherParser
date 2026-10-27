using System.IO;
using WeatherParser.Parsing.Catalogs;

namespace WeatherParser.Parsing.Evidence.Kmz;

// Bounds the length of every markup construct (a tag with its attributes, a CDATA section, a comment, a processing instruction or a declaration)
// in the unpacked member before XmlReader materializes it, so no single XML token can allocate more than MaxMarkupBytes (SC-22 streaming bound).
// Text content stays unbounded here; the parser reads it in chunks. '<', '>', quotes, '!', '?', '-' and ']' are single bytes in UTF-8 and ISO-8859-1.
internal sealed class KmzMarkupGuardStream : ReadOnlyStreamBase
{
    private const int MaxMarkupBytes = 1048576;
    private const int Text = 0;
    private const int AfterOpen = 1;
    private const int AfterBang = 2;
    private const int AfterBangDash = 3;
    private const int Tag = 4;
    private const int Cdata = 5;
    private const int Comment = 6;
    private const int Instruction = 7;

    private readonly Stream _inner;
    private readonly int _maxDepth;
    private readonly Func<string, Exception> _invalid;
    private int _state = Text;
    private long _length;
    private byte _quote;
    private int _closing;
    private int _prefixState;
    private int _bomBytes;
    private int _depth;
    private bool _endTag;
    private byte _previous;

    public KmzMarkupGuardStream(Stream inner, int maxDepth, Func<string, Exception> invalid)
    {
        _maxDepth = maxDepth;
        _invalid = invalid;
        _inner = inner;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = _inner.Read(buffer, offset, count);
        for (var i = offset; i < offset + read; i++)
        {
            Observe(buffer[i]);
        }

        return read;
    }

    private static ReadOnlySpan<byte> Utf8Bom => new byte[] { 0xEF, 0xBB, 0xBF };

    private void Observe(byte value)
    {
        if (_prefixState == 0)
        {
            // Optional UTF-8 byte order mark, then optional ASCII whitespace, then '<'.
            if (_bomBytes < 3 && value == Utf8Bom[_bomBytes] && (_bomBytes > 0 || _length == 0))
            {
                _bomBytes++;
                return;
            }

            if (value is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
            {
                return;
            }

            if (value != (byte)'<' || (_bomBytes is not (0 or 3)))
            {
                throw _invalid("the .kml member must be encoded in UTF-8 or ISO-8859-1");
            }

            _prefixState = 1;
            _state = AfterOpen;
            _length = 1;
            _endTag = false;
            _previous = 0;
            return;
        }
        else if (_state == Text)
        {
            if (value == (byte)'<')
            {
                _state = AfterOpen;
                _length = 1;
                _endTag = false;
                _previous = 0;
            }

            return;
        }

        _length++;
        if (_length > MaxMarkupBytes)
        {
            throw _invalid("a tag, CDATA section, comment or processing instruction exceeds 1 MiB");
        }

        switch (_state)
        {
            case AfterOpen:
                if (value == 0)
                {
                    throw _invalid("the .kml member must be encoded in UTF-8 or ISO-8859-1");
                }
                _endTag = value == (byte)'/';
                _state = value switch
                {
                    (byte)'!' => AfterBang,
                    (byte)'?' => Instruction,
                    _ => Tag,
                };
                ObserveTag(value);
                break;
            case AfterBang:
                _state = value switch
                {
                    (byte)'-' => AfterBangDash,
                    (byte)'[' => Cdata,
                    _ => Tag,
                };
                _closing = 0;
                ObserveTag(value);
                break;
            case AfterBangDash:
                _state = value == (byte)'-' ? Comment : Tag;
                _closing = 0;
                ObserveTag(value);
                break;
            case Tag:
                ObserveTag(value);
                break;
            case Cdata:
                ObserveTerminator(value, (byte)']');
                break;
            case Comment:
                ObserveTerminator(value, (byte)'-');
                break;
            default:
                // Instruction: ends at "?>".
                if (value == (byte)'>' && _closing == 1)
                {
                    _state = Text;
                }

                _closing = value == (byte)'?' ? 1 : 0;
                break;
        }
    }

    private void ObserveTag(byte value)
    {
        if (_state != Tag)
        {
            return;
        }

        if (_quote != 0)
        {
            if (value == _quote)
            {
                _quote = 0;
            }
        }
        else if (value == (byte)'"' || value == (byte)'\'')
        {
            _quote = value;
        }
        else if (value == (byte)'>')
        {
            // Element depth (SC-22 streaming bound): a start tag opens a level, an end tag closes one, a self-closing tag ("/>") neither.
            if (_endTag)
            {
                _depth--;
            }
            else if (_previous != (byte)'/')
            {
                _depth++;
                if (_depth > _maxDepth)
                {
                    throw _invalid($"the KML is nested deeper than {_maxDepth} elements");
                }
            }

            _state = Text;
        }
        else
        {
            _previous = value;
        }
    }

    // CDATA ends at "]]>", a comment at "-->": two marker bytes followed by '>'.
    private void ObserveTerminator(byte value, byte marker)
    {
        if (value == (byte)'>' && _closing >= 2)
        {
            _state = Text;
            _closing = 0;
        }
        else if (value == marker)
        {
            _closing = Math.Min(_closing + 1, 2);
        }
        else
        {
            _closing = 0;
        }
    }
}
