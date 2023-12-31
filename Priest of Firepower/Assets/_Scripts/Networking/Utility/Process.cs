﻿using System.Threading;
using UnityEngine;

namespace _Scripts.Networking.Utility
{
    public struct Process
    {
        public Thread thread;
        public CancellationTokenSource cancellationToken;
        public string Name;
        
        public void Shutdown()
        {
            if (cancellationToken != null)
            {
                cancellationToken.Cancel();
            }
                
            if (thread != null && thread.IsAlive)
            {
                thread.Join();
            }
            if(thread != null && thread.IsAlive)
            {
                thread.Abort();
            }

            cancellationToken = null;
            thread = null;
            
            Debug.Log("thread shutting down: " + Name);
        }
    }
}