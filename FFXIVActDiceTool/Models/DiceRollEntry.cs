namespace FFXIVActDiceTool.Models;

public class DiceRollEntry
{
    public DateTime Timestamp { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int RollValue { get; set; }
    public string RawLogLine { get; set; } = string.Empty;
}
