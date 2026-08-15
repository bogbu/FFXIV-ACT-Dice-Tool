using System;
using System.Collections.Generic;

namespace FFXIVActDiceTool.Models
{
    public class DiceSession
    {
        public DiceSession() { Rolls = new List<DiceRollEntry>(); }
        public bool IsRunning { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public List<DiceRollEntry> Rolls { get; set; }
    }
}
