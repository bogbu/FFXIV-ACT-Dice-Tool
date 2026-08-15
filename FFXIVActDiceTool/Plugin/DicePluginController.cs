using System;
using System.Linq;
using FFXIVActDiceTool.Logging;
using FFXIVActDiceTool.Models;
using FFXIVActDiceTool.Services;

namespace FFXIVActDiceTool.Plugin
{
    public sealed class DicePluginController : IDisposable
    {
        private readonly object _sync = new object();
        private readonly ILogSource _logSource;
        private readonly DiceParser _parser;
        private readonly DiceSessionManager _sessions;
        private readonly RankCalculator _ranks;
        private readonly DiceDuplicateFilter _duplicates;
        private bool _disposed;

        public DicePluginController(ILogSource logSource, DiceParser parser, DiceSessionManager sessions, RankCalculator ranks, DiceDuplicateFilter duplicates)
        {
            _logSource = logSource; _parser = parser; _sessions = sessions; _ranks = ranks; _duplicates = duplicates;
            _logSource.LineReceived += OnLineReceived;
        }

        public event EventHandler<DiceRollEventArgs> RollAdded;
        public event EventHandler<SessionChangedEventArgs> SessionChanged;
        public void StartListening() { _logSource.Start(); }
        public void StartSession()
        {
            SessionSnapshot snapshot;
            lock (_sync) { _sessions.StartSession(); _duplicates.Clear(); snapshot = CreateSnapshot(new DiceSessionResult()); }
            RaiseSessionChanged(snapshot);
        }
        public void StopSession()
        {
            SessionSnapshot snapshot;
            lock (_sync) { var result = _sessions.StopSession(); snapshot = CreateSnapshot(result); }
            RaiseSessionChanged(snapshot);
        }
        public void ResetSession()
        {
            SessionSnapshot snapshot;
            lock (_sync) { _sessions.ResetSession(); _duplicates.Clear(); snapshot = CreateSnapshot(new DiceSessionResult()); }
            RaiseSessionChanged(snapshot);
        }
        public RankQueryResult QueryRank(int rank, RankDirection direction)
        {
            lock (_sync) return _ranks.QueryRank(_sessions.CurrentSession.Rolls.ToList(), rank, direction, RankMode.DenseRank);
        }
        private void OnLineReceived(object sender, string line)
        {
            var entry = _parser.Parse(line);
            if (entry == null) return;
            SessionSnapshot snapshot;
            lock (_sync)
            {
                if (!_sessions.CurrentSession.IsRunning || _duplicates.IsDuplicate(entry)) return;
                _sessions.AddRoll(entry);
                snapshot = CreateSnapshot(_sessions.CalculateResult());
            }
            var handler = RollAdded;
            if (handler != null) handler(this, new DiceRollEventArgs(entry, snapshot));
        }
        private SessionSnapshot CreateSnapshot(DiceSessionResult result)
        {
            var session = _sessions.CurrentSession;
            return new SessionSnapshot(session.IsRunning, session.StartTime, session.EndTime, result);
        }
        private void RaiseSessionChanged(SessionSnapshot snapshot)
        {
            var handler = SessionChanged;
            if (handler != null) handler(this, new SessionChangedEventArgs(snapshot));
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true; _logSource.LineReceived -= OnLineReceived; _logSource.Dispose();
        }
    }

    public sealed class SessionSnapshot
    {
        public SessionSnapshot(bool running, DateTime? start, DateTime? end, DiceSessionResult result) { IsRunning = running; StartTime = start; EndTime = end; Result = result; }
        public bool IsRunning { get; private set; }
        public DateTime? StartTime { get; private set; }
        public DateTime? EndTime { get; private set; }
        public DiceSessionResult Result { get; private set; }
    }
    public sealed class DiceRollEventArgs : EventArgs
    {
        public DiceRollEventArgs(DiceRollEntry entry, SessionSnapshot snapshot) { Entry = entry; Snapshot = snapshot; }
        public DiceRollEntry Entry { get; private set; }
        public SessionSnapshot Snapshot { get; private set; }
    }
    public sealed class SessionChangedEventArgs : EventArgs
    {
        public SessionChangedEventArgs(SessionSnapshot snapshot) { Snapshot = snapshot; }
        public SessionSnapshot Snapshot { get; private set; }
    }
}
