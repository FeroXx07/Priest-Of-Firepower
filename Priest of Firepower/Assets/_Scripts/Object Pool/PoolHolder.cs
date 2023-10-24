using UnityEditor;
using UnityEngine;


[CreateAssetMenu(fileName = "PoolHolder", menuName = "ScriptableObjects/PoolHolder")]

public class PoolHolder : ScriptableObject
{
    public ObjectPool<PoolObject> pool = null;
    public GameObject prefab;
    public int numToInit = 0;

#if UNITY_EDITOR
    private void OnEnable() => EditorApplication.playModeStateChanged += HandleOnPlayModeChanged;

    private void OnDisable() => EditorApplication.playModeStateChanged -= HandleOnPlayModeChanged;

    void HandleOnPlayModeChanged(PlayModeStateChange mode)
    {
        switch (mode)
        {
            case PlayModeStateChange.EnteredPlayMode:
                InitPool();
                break;
            case PlayModeStateChange.ExitingPlayMode:
                pool = null;
                break;
            case PlayModeStateChange.EnteredEditMode:
                break;
            case PlayModeStateChange.ExitingEditMode:
                break;
            default:
                break;
        }
    }
#endif
    public void InitPool()
    {
        if (pool == null)
            pool = new ObjectPool<PoolObject>(prefab, numToInit);
    }
}
