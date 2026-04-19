namespace FFXIVActDiceTool.Models;

public class DiceSessionResult
{
    public List<DiceRollEntry> HighestRolls { get; set; } = new();
    public List<DiceRollEntry> LowestRolls { get; set; } = new();
    public int TotalRollCount { get; set; }
    public int UniquePlayerCount { get; set; }
}
