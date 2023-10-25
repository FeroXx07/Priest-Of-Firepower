using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PoolsSOHandler : MonoBehaviour
{
    private void Awake()
    {
        ScriptableObject[] scriptableObjects  = GetAllInstance<PoolHolder>();
        foreach (PoolHolder pool in scriptableObjects)
        {
            if (pool.pool != null)
                continue;
            pool.InitPool();
        }
    }

    //public static T[] GetAllInstance<T>() where T : ScriptableObject
    //{
    //    string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
    //    T[] a = new T[guids.Length];
    //    for (int i = 0; i < guids.Length; i++)
    //    {
    //        string path = AssetDatabase.GUIDToAssetPath(guids[i]);
    //        a[i] = AssetDatabase.LoadAssetAtPath<T>(path);
    //    }

    //    return a;
    //}

    public static T[] GetAllInstance<T>() where T : ScriptableObject
    {
        return Resources.FindObjectsOfTypeAll<T>();
    }
}
