using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using FFXIVActDiceTool.Models;

namespace FFXIVActDiceTool.Services
{
    public class DiceParser
    {
        private readonly IList<Func<string, DiceRollEntry>> _patterns;

        public DiceParser()
        {
            _patterns = new List<Func<string, DiceRollEntry>>
            {
                ParsePatternActLocalizedPipe, ParsePatternKoreanBracketTimestamp,
                ParsePatternWithBracketTimestamp, ParsePatternWithPipeTimestamp, ParsePatternSimple
            };
        }

        public DiceRollEntry Parse(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;
            foreach (var parser in _patterns)
            {
                var result = parser(line);
                if (result != null) return result;
            }
            return null;
        }

        private static DiceRollEntry ParsePatternWithBracketTimestamp(string line)
        {
            return CreateIfValid(Regex.Match(line, @"^\[(?<time>\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}:\d{2})\].*?(?<name>[\w'\- ]+) rolls? (?<value>\d{1,3})", RegexOptions.IgnoreCase), line);
        }

        private static DiceRollEntry ParsePatternKoreanBracketTimestamp(string line)
        {
            return CreateIfValid(Regex.Match(line, @"^\[(?<time>\d{1,2}:\d{2}(?::\d{2})?)\]\s*(?<name>.+?)\s*님이\s*주사위를\s*굴려\s*(?<value>\d{1,3})(?:이|가)\s*나왔습니다!?$", RegexOptions.IgnoreCase), line);
        }

        private static DiceRollEntry ParsePatternWithPipeTimestamp(string line)
        {
            return CreateIfValid(Regex.Match(line, @"^(?<time>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2})\|.*?\|(?<name>[^|]+?)\s+(?:rolls?|casts.*?/dice).*?(?<value>\d{1,3})$", RegexOptions.IgnoreCase), line);
        }

        private static DiceRollEntry ParsePatternActLocalizedPipe(string line)
        {
            var entry = Regex.Match(line, @"^\d{2}\|(?<time>[^|]+)\|[^|]*\|\|(?<message>[^|]+)\|", RegexOptions.IgnoreCase);
            if (!entry.Success) return null;
            var message = Regex.Match(entry.Groups["message"].Value, @"^(?<name>.+?)\s*님이\s*주사위를\s*굴려\s*(?<value>\d{1,3})(?:이|가)\s*나왔습니다!?$", RegexOptions.IgnoreCase);
            return CreateIfValid(message, line, entry.Groups["time"].Value);
        }

        private static DiceRollEntry ParsePatternSimple(string line)
        {
            return CreateIfValid(Regex.Match(line, @"^(?<time>\d{2}:\d{2}:\d{2}).*?(?<name>[\w'\- ]+) (?:rolls?|obtains).*?(?<value>\d{1,3})", RegexOptions.IgnoreCase), line);
        }

        private static DiceRollEntry CreateIfValid(Match match, string raw, string timestampText = null)
        {
            int value;
            if (!match.Success || !int.TryParse(match.Groups["value"].Value, out value) || value < 0 || value > 999) return null;
            return new DiceRollEntry
            {
                Timestamp = ParseTimestamp(timestampText ?? match.Groups["time"].Value),
                PlayerName = match.Groups["name"].Value.Trim(), RollValue = value, RawLogLine = raw
            };
        }

        private static DateTime ParseTimestamp(string text)
        {
            DateTimeOffset offset;
            DateTime date;
            TimeSpan time;
            if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out offset)) return offset.LocalDateTime;
            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date)) return date;
            if (TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out time) && time >= TimeSpan.Zero && time < TimeSpan.FromDays(1)) return DateTime.Today.Add(time);
            return DateTime.Now;
        }
    }
}
