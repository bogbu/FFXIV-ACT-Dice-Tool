using System;
using System.Collections.Generic;
using FFXIVActDiceTool.Models;

namespace FFXIVActDiceTool.Services
{
    public sealed class DiceDuplicateFilter
    {
        private readonly object _sync = new object();
        private readonly Queue<string> _keys = new Queue<string>();
        private readonly HashSet<string> _set = new HashSet<string>(StringComparer.Ordinal);
        public DiceDuplicateFilter(int maximumSize = 500) { if (maximumSize < 1) throw new ArgumentOutOfRangeException("maximumSize"); MaximumSize = maximumSize; }
        public int MaximumSize { get; private set; }
        public bool IsDuplicate(DiceRollEntry entry)
        {
            if (entry == null) throw new ArgumentNullException("entry");
            var key = string.Format("{0:O}|{1}|{2}|{3}", entry.Timestamp, entry.PlayerName, entry.RollValue, entry.RawLogLine);
            lock (_sync)
            {
                if (_set.Contains(key)) return true;
                _set.Add(key); _keys.Enqueue(key);
                while (_keys.Count > MaximumSize) _set.Remove(_keys.Dequeue());
                return false;
            }
        }
        public void Clear() { lock (_sync) { _keys.Clear(); _set.Clear(); } }
    }
}
