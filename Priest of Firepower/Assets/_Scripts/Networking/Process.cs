using System.Threading;

namespace _Scripts.Networking
{
    public struct Process
    {
        public Thread thread;
        public CancellationTokenSource cancellationToken;
        public void Shutdown()
        {
            cancellationToken?.Cancel();

            if (thread != null && thread.IsAlive)
            {
                thread.Join();
            }
            if(thread != null && thread.IsAlive)
            {
                thread.Abort();
            }
        }
    }
}