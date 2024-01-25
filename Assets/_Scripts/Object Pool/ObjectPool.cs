using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Object_Pool
{
    public class ObjectPool<T> : IPool<T> where T : MonoBehaviour, IPoolable<T>
    {
        private GameObject _parent;
        public ObjectPool(GameObject pooledObject, int numToSpawn = 0)
        {
            _parent = new GameObject("Pool Of " + pooledObject.name);
            this._prefab = pooledObject;
            Spawn(numToSpawn);
        }

        public ObjectPool(GameObject pooledObject, Action<T> onCreateObject, Action<T> pullObject, Action<T> pushObject, int numToSpawn = 0)
        {
            _parent = new GameObject("Pool Of " + pooledObject.name);
            this._prefab = pooledObject;
            this._pullObject = pullObject;
            this._pushObject = pushObject;
            this._onCreateObject = onCreateObject;
            Spawn(numToSpawn);
        }

        private System.Action<T> _pullObject;
        private System.Action<T> _pushObject;
        private System.Action<T> _onCreateObject;
        private Stack<T> _pooledObjects = new Stack<T>();
        private GameObject _prefab;
        public int PooledCount
        {
            get
            {
                return _pooledObjects.Count;
            }
        }

        public T Pull()
        {
            T t;
            if (PooledCount > 0)
                t = _pooledObjects.Pop();
            else
                t = GameObject.Instantiate(_prefab, _parent.transform).GetComponent<T>();

            t.gameObject.SetActive(true); //ensure the object is on
            t.Initialize(Push);

            //allow default behavior and turning object back on
            _pullObject?.Invoke(t);

            return t;
        }

        public T Pull(Vector3 position)
        {
            T t = Pull();
            t.transform.position = position;
            return t;
        }

        public T Pull(Vector3 position, Quaternion rotation)
        {
            T t = Pull();
            t.transform.position = position;
            t.transform.rotation = rotation;
            return t;
        }

        public GameObject PullGameObject()
        {
            return Pull().gameObject;
        }

        public GameObject PullGameObject(Vector3 position)
        {
            GameObject go = Pull(position).gameObject;
            go.transform.position = position;
            return go;
        }

        public GameObject PullGameObject(Vector3 position, Quaternion rotation)
        {
            GameObject go = Pull(position, rotation).gameObject;
            go.transform.position = position;
            go.transform.rotation = rotation;
            return go;
        }

        public void Push(T t)
        {
            _pooledObjects.Push(t);

            //create default behavior to turn off objects
            _pushObject?.Invoke(t);

            t.gameObject.SetActive(false);
        }

        private void Spawn(int number)
        {
            T t;

            for (int i = 0; i < number; i++)
            {
                t = GameObject.Instantiate(_prefab, _parent.transform).GetComponent<T>();
                _pooledObjects.Push(t);
                _onCreateObject?.Invoke(t);
                t.gameObject.SetActive(false);
            }
        }

        public GameObject GetParent()
        {
            return _parent;
        }
    }

    public interface IPool<T>
    {
        T Pull();
        void Push(T t);
    }

    public interface IPoolable<T>
    {
        void Initialize(System.Action<T> returnAction);
        void ReturnToPool();
    }
}