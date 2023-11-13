using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Networking
{
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static readonly Queue<Action> actionQueue = new Queue<Action>();
        private void Awake()
        {
            DontDestroyOnLoad(this);
        }
        private void Update()
        {
            while(actionQueue.Count > 0)
            {
                actionQueue.Dequeue()?.Invoke();
            }
        }
        public static void EnqueueAction(Action action)
        {
            actionQueue.Enqueue(action);
        }
    }
}
