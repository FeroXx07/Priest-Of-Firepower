using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum NetworkAction
{
    CREATE_GAMEOBJECT,
    UPDATE_GAMEOBJECT,
    DESTROY_GAMEOBJECT
}
public class NetworkObject : MonoBehaviour
{
    public bool synchronizeTransform = true;
    [SerializeField] private UInt64 globalObjectIdHash = 10;
    public void SetNetworkId(UInt64 id){ globalObjectIdHash = id; }
    public UInt64 GetNetworkId() {  return globalObjectIdHash; }
}
