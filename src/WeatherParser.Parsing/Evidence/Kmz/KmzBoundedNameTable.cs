using System.Xml;

namespace WeatherParser.Parsing.Evidence.Kmz;

// Bounds the total length of all distinct names the reader interns (element, attribute and prefix names and namespace URIs, also of skipped elements),
// so that many distinct names cannot accumulate beyond the streaming memory bound (SC-22).
internal sealed class KmzBoundedNameTable : XmlNameTable
{
    private const int MaxNameCharacters = 1048576;
    private readonly NameTable _inner = new();
    private readonly Func<string, Exception> _invalid;
    private long _characters;

    public KmzBoundedNameTable(Func<string, Exception> invalid)
    {
        _invalid = invalid;
    }

    public override string Add(char[] key, int start, int len)
    {
        if (Get(key, start, len) is { } existing)
        {
            return existing;
        }

        Count(len);
        return _inner.Add(key, start, len);
    }

    public override string Add(string key)
    {
        if (Get(key) is { } existing)
        {
            return existing;
        }

        Count(key.Length);
        return _inner.Add(key);
    }

    public override string? Get(char[] key, int start, int len) => _inner.Get(key, start, len);

    public override string? Get(string value) => _inner.Get(value);

    private void Count(int length)
    {
        _characters += length;
        if (_characters > MaxNameCharacters)
        {
            throw _invalid("the distinct XML names exceed 1048576 characters");
        }
    }
}
