using System;
using Advanced_Combat_Tracker;
using FFXIVActDiceTool.Logging;

namespace FFXIVActDiceTool.ACT
{
    public sealed class ActLogSource : ILogSource
    {
        private bool _started;
        public event EventHandler<string> LineReceived;
        public void Start()
        {
            if (_started) return;
            ActGlobals.oFormActMain.OnLogLineRead += OnLogLineRead;
            _started = true;
        }
        public void Stop()
        {
            if (!_started) return;
            ActGlobals.oFormActMain.OnLogLineRead -= OnLogLineRead;
            _started = false;
        }
        private void OnLogLineRead(bool isImport, LogLineEventArgs logInfo)
        {
            if (isImport || logInfo == null || string.IsNullOrWhiteSpace(logInfo.logLine)) return;
            var handler = LineReceived;
            if (handler != null) handler(this, logInfo.logLine);
        }
        public void Dispose() { Stop(); }
    }
}
