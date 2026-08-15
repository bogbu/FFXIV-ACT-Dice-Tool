using System.Collections.Generic;
using System.Linq;
using FFXIVActDiceTool.Models;

namespace FFXIVActDiceTool.Services
{
    public class RankCalculator
    {
        public RankQueryResult QueryRank(IReadOnlyCollection<DiceRollEntry> entries, int requestedRank, RankDirection direction, RankMode mode = RankMode.DenseRank)
        {
            var result = new RankQueryResult { RequestedRank = requestedRank, Direction = direction, Mode = mode };
            if (requestedRank <= 0 || entries.Count == 0) return result;
            var groups = mode == RankMode.DenseRank ? BuildDenseRank(entries, direction) : BuildSequentialRank(entries, direction);
            var matched = groups.FirstOrDefault(x => x.Rank == requestedRank);
            if (matched == null) return result;
            result.Exists = true; result.RollValue = matched.RollValue; result.Entries = matched.Entries;
            return result;
        }
        private static List<RankedGroup> BuildDenseRank(IEnumerable<DiceRollEntry> entries, RankDirection direction)
        {
            var groups = entries.GroupBy(x => x.RollValue);
            var ordered = direction == RankDirection.Highest ? groups.OrderByDescending(x => x.Key) : groups.OrderBy(x => x.Key);
            var rank = 1;
            return ordered.Select(x => new RankedGroup { Rank = rank++, RollValue = x.Key, Entries = x.ToList() }).ToList();
        }
        private static List<RankedGroup> BuildSequentialRank(IEnumerable<DiceRollEntry> entries, RankDirection direction)
        {
            var ordered = direction == RankDirection.Highest ? entries.OrderByDescending(x => x.RollValue) : entries.OrderBy(x => x.RollValue);
            return ordered.Select((x, i) => new RankedGroup { Rank = i + 1, RollValue = x.RollValue, Entries = new List<DiceRollEntry> { x } }).ToList();
        }
    }
}
