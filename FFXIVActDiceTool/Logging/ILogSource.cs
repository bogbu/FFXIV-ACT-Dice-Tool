using System;
namespace FFXIVActDiceTool.Logging
{
    public interface ILogSource : IDisposable
    {
        event EventHandler<string> LineReceived;
        void Start();
        void Stop();
    }
}
