using System;
using System.IO;
using UnityEngine;

namespace _Scripts.Networking
{
    public class NetworkObject : MonoBehaviour
    {
        public bool synchronizeTransform = true;
        [SerializeField] private UInt64 globalObjectIdHash = 10;
        public void SetNetworkId(UInt64 id){ globalObjectIdHash = id; }
        public UInt64 GetNetworkId() {  return globalObjectIdHash; }

        public void HandleNetworkBehaviour(Type type, BinaryReader reader)
        {
            NetworkBehaviour b = GetComponent(type) as NetworkBehaviour;
            if (b != null)
            {
                b.Read(reader);
            }
            else
            {
                Debug.LogError("Cast failed " + type);
            }
        }
    }
}