using FFXIVActDiceTool.Models;

namespace FFXIVActDiceTool.Services;

public class DiceSessionManager
{
    public DiceSession CurrentSession { get; private set; } = new();

    public void StartSession()
    {
        CurrentSession = new DiceSession
        {
            IsRunning = true,
            StartTime = DateTime.Now,
            EndTime = null,
            Rolls = new List<DiceRollEntry>()
        };
    }

    public DiceSessionResult StopSession()
    {
        CurrentSession.IsRunning = false;
        CurrentSession.EndTime = DateTime.Now;
        return CalculateResult();
    }

    public void ResetSession()
    {
        CurrentSession = new DiceSession();
    }

    public void AddRoll(DiceRollEntry entry)
    {
        if (!CurrentSession.IsRunning)
        {
            return;
        }

        CurrentSession.Rolls.Add(entry);
    }

    public DiceSessionResult CalculateResult()
    {
        var rolls = CurrentSession.Rolls;
        if (rolls.Count == 0)
        {
            return new DiceSessionResult();
        }

        var max = rolls.Max(x => x.RollValue);
        var min = rolls.Min(x => x.RollValue);

        return new DiceSessionResult
        {
            HighestRolls = rolls.Where(x => x.RollValue == max).ToList(),
            LowestRolls = rolls.Where(x => x.RollValue == min).ToList(),
            TotalRollCount = rolls.Count,
            UniquePlayerCount = rolls.Select(x => x.PlayerName).Distinct(StringComparer.OrdinalIgnoreCase).Count()
        };
    }
}
