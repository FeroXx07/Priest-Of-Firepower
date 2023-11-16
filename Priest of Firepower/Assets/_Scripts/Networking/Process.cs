using System;
using System.Threading;
using UnityEngine;

namespace _Scripts.Networking
{
    public struct Process
    {
        public Thread thread;
        public CancellationTokenSource cancellationToken;
        public string Name;
        
        public void Shutdown()
        {
            cancellationToken.Cancel();
            if (thread != null && thread.IsAlive)
            {
                thread.Join();
            }
            if(thread != null && thread.IsAlive)
            {
                thread.Abort();
            }
            
            Debug.Log("thread shutting down: " + Name);
        }
    }
}