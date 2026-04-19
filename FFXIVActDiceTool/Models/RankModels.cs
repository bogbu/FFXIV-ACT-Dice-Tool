namespace FFXIVActDiceTool.Models;

public enum RankDirection
{
    Highest,
    Lowest
}

public enum RankMode
{
    DenseRank,
    SequentialOrder
}

public class RankedGroup
{
    public int Rank { get; set; }
    public int RollValue { get; set; }
    public List<DiceRollEntry> Entries { get; set; } = new();
}

public class RankQueryResult
{
    public int RequestedRank { get; set; }
    public RankDirection Direction { get; set; }
    public RankMode Mode { get; set; }
    public bool Exists { get; set; }
    public int? RollValue { get; set; }
    public List<DiceRollEntry> Entries { get; set; } = new();
}
