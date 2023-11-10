using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class NetworkObject : MonoBehaviour
{
    public bool synchronizeTransform = true;
    [SerializeField] private UInt64 globalObjectIdHash;

    public void SetNetworkId(UInt64 id){ globalObjectIdHash = id; }
    UInt64 GetNetworkId() {  return globalObjectIdHash; }
}
