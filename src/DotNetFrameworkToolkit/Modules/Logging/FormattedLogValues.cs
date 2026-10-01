using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DotNetFrameworkToolkit.Modules.Logging;

// Supports named templates and standard composite-format alignment/format suffixes.
internal sealed class FormattedLogValues
{
    internal readonly string OriginalMessage;
    internal readonly object[] MessageArguments;
    internal readonly Dictionary<string, object> Properties = new();

    private readonly string _format;
    
    internal FormattedLogValues(string message, params object[] args)
    {
        OriginalMessage = message ?? string.Empty;
        MessageArguments = args == null 
            ? new object[0] 
            : (object[])args.Clone();

        Properties["{OriginalFormat}"] = OriginalMessage;

        if (MessageArguments.Length == 0) 
        { 
            _format = OriginalMessage; return; 
        }

        StringBuilder output = new(); 
        Dictionary<string, int> names = new(); 
        int next = 0;

        for (int i = 0; i < OriginalMessage.Length; i++)
        {
            char c = OriginalMessage[i];
            if ((c == '{' || c == '}') && i + 1 < OriginalMessage.Length && OriginalMessage[i + 1] == c)
            { 
                output.Append(c).Append(c); 
                i++; 
                continue; 
            }

            if (c != '{') 
            { 
                output.Append(c); 
                continue; 
            }

            int end = OriginalMessage.IndexOf('}', i + 1);

            if (end < 0)
            { 
                throw new FormatException("Unclosed log placeholder."); 
            }

            string token = OriginalMessage.Substring(i + 1, end - i - 1);
            int suffix = token.IndexOfAny(new[] { ',', ':' });
            string name = (suffix < 0 ? token : token.Substring(0, suffix)).Trim();

            if (name.Length == 0 || name.IndexOf('{') >= 0)
            { 
                throw new FormatException("Invalid log placeholder."); 
            }

            int index;
            if (!int.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out index))
            {
                if (!names.TryGetValue(name, out index))
                {
                    index = next++;
                    names.Add(name, index);
                }
            }
            else if (index >= next)
            {
                next = index + 1;
            }

            if (index < 0 || index >= MessageArguments.Length)
            {
                throw new FormatException("Missing log argument for " + name);
            }

            Properties[name] = MessageArguments[index];
            output.Append('{').Append(index.ToString(CultureInfo.InvariantCulture));

            if (suffix >= 0)
            {
                output.Append(token.Substring(suffix));
            }

            output.Append('}'); i = end;
        }

        _format = output.ToString();
    }

    public override string ToString()
    {
        return MessageArguments.Length == 0 
            ? _format 
            : string.Format(CultureInfo.InvariantCulture, _format, MessageArguments);
    }
}
