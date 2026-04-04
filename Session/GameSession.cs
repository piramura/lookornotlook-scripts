using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Piramura.LookOrNotLook.Session
{
    public interface IGameSession
    {
        CancellationToken Token { get; }
        bool IsAlive { get; }
        int Version { get; }
        void BeginNewSession();
        void EndSession(); // Cancel + Dispose
    }

    public sealed class GameSession : IGameSession, IDisposable
    {
        private CancellationTokenSource _cts;
        public CancellationToken Token => _cts?.Token ?? CancellationToken.None;
        public bool IsAlive => _cts != null && !_cts.IsCancellationRequested;
        public int Version { get; private set; }
        public void BeginNewSession()
        {
            EndSession();
            _cts = new CancellationTokenSource();
            Version++;
        }

        public void EndSession()
        {
            if (_cts == null) return;
            try { _cts.Cancel(); } catch { /* ignore */ }
            _cts.Dispose();
            _cts = null;
        }

        public void Dispose() => EndSession();
    }
}
