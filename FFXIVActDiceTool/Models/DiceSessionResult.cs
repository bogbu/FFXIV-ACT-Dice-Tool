using System.Collections.Generic;

namespace FFXIVActDiceTool.Models
{
    public class DiceSessionResult
    {
        public DiceSessionResult()
        {
            HighestRolls = new List<DiceRollEntry>();
            LowestRolls = new List<DiceRollEntry>();
        }
        public List<DiceRollEntry> HighestRolls { get; set; }
        public List<DiceRollEntry> LowestRolls { get; set; }
        public int TotalRollCount { get; set; }
        public int UniquePlayerCount { get; set; }
    }
}
