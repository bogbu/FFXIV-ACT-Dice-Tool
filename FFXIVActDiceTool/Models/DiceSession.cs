namespace FFXIVActDiceTool.Models;

public class DiceSession
{
    public bool IsRunning { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public List<DiceRollEntry> Rolls { get; set; } = new();
}
