namespace WeatherParser.Parsing.Mqtt;

/// <summary>
/// Parses and checks MQTT topic templates of the consumer configuration (contract MQ-04, MQ-07).
/// </summary>
public sealed class MqttTopicTemplateParser : IMqttTopicTemplateParser
{
    /// <inheritdoc/>
    public MqttTopicTemplate Parse(string template, MqttMessageType messageType)
    {
        ArgumentNullException.ThrowIfNull(template);
        
        if (!Enum.IsDefined(messageType))
        {
            throw new ArgumentException("Unknown message type.", nameof(messageType));
        }

        if (template.Length == 0)
        {
            throw Fail(messageType, "The topic template must not be empty.");
        }

        var segments = new List<MqttTopicTemplateSegment>();
        int i = 0;
        int literalStart = 0;
        bool inPlaceholder = false;
        int placeholderStart = 0;

        while (i < template.Length)
        {
            char c = template[i];

            if (c == '{' && !inPlaceholder)
            {
                if (literalStart < i)
                {
                    AddLiteral(segments, template, literalStart, i, messageType);
                }

                inPlaceholder = true;
                placeholderStart = i + 1;
            }
            else if (c == '}' && inPlaceholder)
            {
                int placeholderLength = i - placeholderStart;
                if (placeholderLength == 0)
                {
                    throw Fail(messageType, $"The topic template contains an unbalanced or empty placeholder brace at position {placeholderStart - 1}.");
                }

                string name = template.Substring(placeholderStart, placeholderLength);
                if (!IsAvailable(name, messageType))
                {
                    bool isValidName = IsPlainName(name);
                    string message = isValidName 
                        ? $"The placeholder '{name}' is not available for message type {messageType}." 
                        : "The topic template contains an unknown placeholder.";
                    
                    throw Fail(messageType, message);
                }

                segments.Add(new MqttTopicTemplateSegment(true, name));
                literalStart = i + 1;
                inPlaceholder = false;
            }
            else if (c == '{' && inPlaceholder)
            {
                throw Fail(messageType, $"The topic template contains an unbalanced or empty placeholder brace at position {i}.");
            }
            else if (c == '}' && !inPlaceholder)
            {
                throw Fail(messageType, $"The topic template contains an unbalanced or empty placeholder brace at position {i}.");
            }

            i++;
        }

        if (inPlaceholder)
        {
            throw Fail(messageType, $"The topic template contains an unbalanced or empty placeholder brace at position {placeholderStart - 1}.");
        }

        if (literalStart < template.Length)
        {
            AddLiteral(segments, template, literalStart, template.Length, messageType);
        }

        if (!segments[0].IsPlaceholder && segments[0].Text.StartsWith('/'))
        {
            throw Fail(messageType, "The topic template contains an empty topic level.");
        }

        if (!segments[^1].IsPlaceholder && segments[^1].Text.EndsWith('/'))
        {
            throw Fail(messageType, "The topic template contains an empty topic level.");
        }

        if (!segments[0].IsPlaceholder && segments[0].Text.StartsWith('$'))
        {
            throw Fail(messageType, "The topic template must not start with '$'.");
        }

        int totalLength = 0;
        foreach (var segment in segments)
        {
            if (!segment.IsPlaceholder)
            {
                totalLength += System.Text.Encoding.UTF8.GetByteCount(segment.Text);
            }
            else
            {
                totalLength += 1;
            }
        }

        if (totalLength > 65535)
        {
            throw Fail(messageType, "The topic template is longer than the 65 535-byte topic limit.");
        }

        return new MqttTopicTemplate(messageType, template, segments);
    }

    private static void AddLiteral(List<MqttTopicTemplateSegment> segments, string template, int start, int end, MqttMessageType messageType)
    {
        int disallowedIndex = MqttTopicCharacters.FindDisallowed(template, start, end - start);
        if (disallowedIndex >= 0)
        {
            throw Fail(messageType, $"The topic template contains a disallowed character at position {disallowedIndex}.");
        }

        int wildcardIndex = template.IndexOfAny(['+', '#'], start, end - start);
        if (wildcardIndex >= 0)
        {
            throw Fail(messageType, $"The topic template contains a wildcard character at position {wildcardIndex}.");
        }

        if (template.IndexOf("//", start, end - start, StringComparison.Ordinal) >= 0)
        {
            throw Fail(messageType, "The topic template contains an empty topic level.");
        }

        segments.Add(new MqttTopicTemplateSegment(false, template.Substring(start, end - start)));
    }

    private static bool IsAvailable(string name, MqttMessageType messageType)
    {
        return messageType switch
        {
            MqttMessageType.Observation => name == "source" || name == "provider" || name == "station_id",
            MqttMessageType.Forecast => name == "source" || name == "provider" || name == "mosmix_id" || name == "product",
            _ => name == "provider"
        };
    }

    private static bool IsPlainName(string name)
    {
        foreach (char ch in name)
        {
            if (!(ch is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_'))
            {
                return false;
            }
        }
        return true;
    }

    private static MqttMessageException Fail(MqttMessageType messageType, string message)
    {
        return new MqttMessageException(MqttMessageErrorIds.OptionsInvalid, messageType, message);
    }
}
