namespace WeatherParser.Parsing.Mqtt;

/// <summary>
/// Character rules shared by the topic template parser and the topic validator (protocol facts F2 to F4 of the MQTT contract).
/// </summary>
internal static class MqttTopicCharacters
{
    /// <summary>
    /// Finds the first disallowed code point in the specified text.
    /// </summary>
    /// <param name="text">The text to scan.</param>
    /// <param name="start">The start index (inclusive).</param>
    /// <param name="length">The length of the range to scan.</param>
    /// <returns>The index of the first disallowed code point, or -1 if none found.</returns>
    public static int FindDisallowed(string text, int start, int length)
    {
        ArgumentNullException.ThrowIfNull(text);

        int end = start + length;
        for (int i = start; i < end;)
        {
            char c = text[i];
            int codePoint;

            if (char.IsHighSurrogate(c) && i + 1 < end && char.IsLowSurrogate(text[i + 1]))
            {
                codePoint = char.ConvertToUtf32(c, text[i + 1]);
                i += 2;
            }
            else
            {
                if (char.IsSurrogate(c))
                {
                    return i;
                }
                
                codePoint = c;
                i += 1;
            }

            if (codePoint < 0x20 || (codePoint >= 0x7F && codePoint <= 0x9F) || (codePoint >= 0xFDD0 && codePoint <= 0xFDEF) ||
                (codePoint & 0xFFFE) == 0xFFFE)
            {
                return i - (codePoint > 0xFFFF ? 2 : 1);
            }
        }

        return -1;
    }
}
