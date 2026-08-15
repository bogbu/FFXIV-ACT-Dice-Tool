using System;
using System.Linq;
using FFXIVActDiceTool.Models;

namespace FFXIVActDiceTool.Services
{
    public class DiceSessionManager
    {
        public DiceSessionManager() { CurrentSession = new DiceSession(); }
        public DiceSession CurrentSession { get; private set; }
        public void StartSession() { CurrentSession = new DiceSession { IsRunning = true, StartTime = DateTime.Now }; }
        public DiceSessionResult StopSession() { CurrentSession.IsRunning = false; CurrentSession.EndTime = DateTime.Now; return CalculateResult(); }
        public void ResetSession() { CurrentSession = new DiceSession(); }
        public void AddRoll(DiceRollEntry entry) { if (CurrentSession.IsRunning) CurrentSession.Rolls.Add(entry); }
        public DiceSessionResult CalculateResult()
        {
            var rolls = CurrentSession.Rolls;
            if (rolls.Count == 0) return new DiceSessionResult();
            var max = rolls.Max(x => x.RollValue); var min = rolls.Min(x => x.RollValue);
            return new DiceSessionResult
            {
                HighestRolls = rolls.Where(x => x.RollValue == max).ToList(),
                LowestRolls = rolls.Where(x => x.RollValue == min).ToList(),
                TotalRollCount = rolls.Count,
                UniquePlayerCount = rolls.Select(x => x.PlayerName).Distinct(StringComparer.OrdinalIgnoreCase).Count()
            };
        }
    }
}
