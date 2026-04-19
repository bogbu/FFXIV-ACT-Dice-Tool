using FFXIVActDiceTool.Models;

namespace FFXIVActDiceTool.Services;

public class RankCalculator
{
    public RankQueryResult QueryRank(
        IReadOnlyCollection<DiceRollEntry> entries,
        int requestedRank,
        RankDirection direction,
        RankMode mode = RankMode.DenseRank)
    {
        var result = new RankQueryResult
        {
            RequestedRank = requestedRank,
            Direction = direction,
            Mode = mode,
            Exists = false
        };

        if (requestedRank <= 0 || entries.Count == 0)
        {
            return result;
        }

        var groups = mode == RankMode.DenseRank
            ? BuildDenseRank(entries, direction)
            : BuildSequentialRank(entries, direction);

        var matched = groups.FirstOrDefault(x => x.Rank == requestedRank);
        if (matched is null)
        {
            return result;
        }

        result.Exists = true;
        result.RollValue = matched.RollValue;
        result.Entries = matched.Entries;
        return result;
    }

    private static List<RankedGroup> BuildDenseRank(IReadOnlyCollection<DiceRollEntry> entries, RankDirection direction)
    {
        var orderedGroups = direction == RankDirection.Highest
            ? entries.GroupBy(x => x.RollValue).OrderByDescending(g => g.Key)
            : entries.GroupBy(x => x.RollValue).OrderBy(g => g.Key);

        var rank = 1;
        return orderedGroups
            .Select(g => new RankedGroup { Rank = rank++, RollValue = g.Key, Entries = g.ToList() })
            .ToList();
    }

    private static List<RankedGroup> BuildSequentialRank(IReadOnlyCollection<DiceRollEntry> entries, RankDirection direction)
    {
        var ordered = direction == RankDirection.Highest
            ? entries.OrderByDescending(x => x.RollValue).ToList()
            : entries.OrderBy(x => x.RollValue).ToList();

        return ordered
            .Select((entry, index) => new RankedGroup
            {
                Rank = index + 1,
                RollValue = entry.RollValue,
                Entries = new List<DiceRollEntry> { entry }
            })
            .ToList();
    }
}
