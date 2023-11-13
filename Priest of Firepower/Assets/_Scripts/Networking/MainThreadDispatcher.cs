using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Networking
{
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static readonly Queue<Action> ActionQueue = new Queue<Action>();
        private void Awake()
        {
            DontDestroyOnLoad(this);
        }
        private void Update()
        {
            while(ActionQueue.Count > 0)
            {
                ActionQueue.Dequeue()?.Invoke();
            }
        }
        public static void EnqueueAction(Action action)
        {
            ActionQueue.Enqueue(action);
        }
    }
}
