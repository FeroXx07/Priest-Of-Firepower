using System.Collections;
using _Scripts;
using UnityEngine;

public class DebugLogToScreen : GenericSingleton<DebugLogToScreen>
{
    uint qsize = 8;  // number of messages to keep
    Queue myLogQueue = new Queue();

    void Start() {
        if (!Debug.isDebugBuild)
            return;
        Debug.Log("Started up logging.");
    }

    void OnEnable() {
        if (!Debug.isDebugBuild)
            return;
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable() {
        if (!Debug.isDebugBuild)
            return;
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        myLogQueue.Enqueue($"[{type}] : {logString}");
        if (type == LogType.Exception || type == LogType.Error)
            myLogQueue.Enqueue(stackTrace);
        while (myLogQueue.Count > qsize)
            myLogQueue.Dequeue();
    }

    void OnGUI() {
        GUILayout.BeginArea(new Rect(200, 0, 900, Screen.height));
        GUILayout.Label("\n" + string.Join("\n", myLogQueue.ToArray()));
        GUILayout.EndArea();
    }
}
