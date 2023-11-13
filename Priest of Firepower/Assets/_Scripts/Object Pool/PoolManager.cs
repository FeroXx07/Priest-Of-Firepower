using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Object_Pool
{
    public class PoolManager : GenericSingleton<PoolManager>
    {
        // The pool holders
        private Dictionary<int, ObjectPool<PoolObject>> pools = new Dictionary<int, ObjectPool<PoolObject>>();

        // Optional pool list to init from awake
        [SerializeField] int defaultSize = 5;
        [SerializeField] List<GameObject> prefabsToAdd = new List<GameObject>();
        [SerializeField] List<int> prefabsToAddSize = new List<int>();

        public override void Awake()
        {
            for (int i = 0; i < prefabsToAdd.Count; i++)
            {
                if (i > prefabsToAddSize.Count - 1)
                    CreatePool(prefabsToAdd[i], defaultSize);
                else
                    CreatePool(prefabsToAdd[i], prefabsToAddSize[i]);
            }
        }

        // Get the pool from the dictionary
        public ObjectPool<PoolObject> GetPool(GameObject prefab, int size = -1)
        {
            int hash = prefab.GetHashCode();

            if (pools.TryGetValue(hash, out var pool))
                return pool;

            ObjectPool<PoolObject> newPool;

            if (size >= 0)
                newPool = CreatePool(prefab, size);
            else
                newPool = CreatePool(prefab, defaultSize);

            return newPool;
        }

        // Pull an object from a pool from the dictionary
        public GameObject Pull(GameObject prefab, int size = -1)
        {
            return Pull(prefab, Vector3.zero, Quaternion.identity, size);
        }

        public GameObject Pull(GameObject prefab, Vector3 position, int size = -1)
        {
            return Pull(prefab, position, Quaternion.identity, size);
        }

        public GameObject Pull(GameObject prefab, Vector3 position, Quaternion quaternion, int size = -1)
        {
            int hash = prefab.GetHashCode();

            if (pools.TryGetValue(hash, out var pool))
                return pool.PullGameObject(position, quaternion);

            if (size >= 0)
                CreatePool(prefab, size);
            else
                CreatePool(prefab, defaultSize);

            return Pull(prefab, position, quaternion, size);
        }

        // Handle pool creation
        ObjectPool<PoolObject> CreatePool(GameObject prefab, int size)
        {
            int hash = prefab.GetHashCode();

            if (pools.ContainsKey(hash))
                return pools[hash];

            ObjectPool<PoolObject> newPool = new ObjectPool<PoolObject>(prefab, size);
            pools.TryAdd(hash, newPool);
            return newPool;
        }
    }
}
