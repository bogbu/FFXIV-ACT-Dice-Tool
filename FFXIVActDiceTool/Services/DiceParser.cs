using System.Globalization;
using System.Text.RegularExpressions;
using FFXIVActDiceTool.Models;

namespace FFXIVActDiceTool.Services;

public class DiceParser
{
    private readonly List<Func<string, DiceRollEntry?>> _patterns;

    public DiceParser()
    {
        _patterns = new List<Func<string, DiceRollEntry?>>
        {
            ParsePatternActLocalizedPipe,
            ParsePatternWithBracketTimestamp,
            ParsePatternWithPipeTimestamp,
            ParsePatternSimple
        };
    }

    public DiceRollEntry? Parse(string line)
    {
        foreach (var parser in _patterns)
        {
            var result = parser(line);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    private static DiceRollEntry? ParsePatternWithBracketTimestamp(string line)
    {
        var match = Regex.Match(
            line,
            @"^\[(?<time>\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}:\d{2})\].*?(?<name>[\w'\- ]+) rolls? (?<value>\d{1,3})",
            RegexOptions.IgnoreCase);

        return CreateIfValid(match, line);
    }

    private static DiceRollEntry? ParsePatternWithPipeTimestamp(string line)
    {
        var match = Regex.Match(
            line,
            @"^(?<time>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2})\|.*?\|(?<name>[^|]+?)\s+(?:rolls?|casts.*?/dice).*?(?<value>\d{1,3})$",
            RegexOptions.IgnoreCase);

        return CreateIfValid(match, line);
    }

    private static DiceRollEntry? ParsePatternActLocalizedPipe(string line)
    {
        var entryMatch = Regex.Match(
            line,
            @"^\d{2}\|(?<time>[^|]+)\|[^|]*\|\|(?<message>[^|]+)\|",
            RegexOptions.IgnoreCase);

        if (!entryMatch.Success)
        {
            return null;
        }

        var messageMatch = Regex.Match(
            entryMatch.Groups["message"].Value,
            @"^(?<name>.+?)\s*님이\s*주사위를\s*굴려\s*(?<value>\d{1,3})가\s*나왔습니다!$",
            RegexOptions.IgnoreCase);

        if (!messageMatch.Success)
        {
            return null;
        }

        var combined = Regex.Match(
            $"{entryMatch.Groups["time"].Value}|{messageMatch.Groups["name"].Value}|{messageMatch.Groups["value"].Value}",
            @"^(?<time>[^|]+)\|(?<name>[^|]+)\|(?<value>\d{1,3})$");

        return CreateIfValid(combined, line);
    }

    private static DiceRollEntry? ParsePatternSimple(string line)
    {
        var match = Regex.Match(
            line,
            @"^(?<time>\d{2}:\d{2}:\d{2}).*?(?<name>[\w'\- ]+) (?:rolls?|obtains).*?(?<value>\d{1,3})",
            RegexOptions.IgnoreCase);

        return CreateIfValid(match, line);
    }

    private static DiceRollEntry? CreateIfValid(Match match, string raw)
    {
        if (!match.Success)
        {
            return null;
        }

        if (!int.TryParse(match.Groups["value"].Value, out var value) || value is < 0 or > 999)
        {
            return null;
        }

        var timeText = match.Groups["time"].Value;
        DateTime timestamp;

        if (DateTimeOffset.TryParse(timeText, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dto))
        {
            timestamp = dto.LocalDateTime;
        }
        else if (!DateTime.TryParse(timeText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out timestamp))
        {
            if (TimeOnly.TryParse(timeText, out var timeOnly))
            {
                timestamp = DateTime.Today.Add(timeOnly.ToTimeSpan());
            }
            else
            {
                timestamp = DateTime.Now;
            }
        }

        return new DiceRollEntry
        {
            Timestamp = timestamp,
            PlayerName = match.Groups["name"].Value.Trim(),
            RollValue = value,
            RawLogLine = raw
        };
    }
}
