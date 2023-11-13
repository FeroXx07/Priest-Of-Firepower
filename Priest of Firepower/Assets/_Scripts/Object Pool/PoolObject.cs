using System;
using UnityEngine;

namespace _Scripts.Object_Pool
{
    public class PoolObject : MonoBehaviour, IPoolable<PoolObject>
    {
        private Action<PoolObject> _returnToPool;

        private void OnDisable()
        {
            ReturnToPool();
        }

        public void Initialize(Action<PoolObject> returnAction)
        {
            //cache reference to return action
            this._returnToPool = returnAction;
        }

        public void ReturnToPool()
        {
            //invoke and return this object to pool
            _returnToPool?.Invoke(this);
        }
    }
}


