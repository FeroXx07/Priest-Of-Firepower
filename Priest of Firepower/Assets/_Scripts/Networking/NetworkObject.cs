using System;
using System.IO;
using UnityEngine;

namespace _Scripts.Networking
{
    public class NetworkObject : MonoBehaviour
    {
        [SerializeField] private UInt64 globalObjectIdHash = 10;
        public bool synchronizeTransform = true;
        public float tickRate = 10.0f; // Network writes inside a second.
        private float _tickCounter = 0.0f;
        
        public void SetNetworkId(UInt64 id){ globalObjectIdHash = id; }
        public UInt64 GetNetworkId() {  return globalObjectIdHash; }

        public void HandleNetworkBehaviour(Type type, BinaryReader reader)
        {
            NetworkBehaviour behaviour = GetComponent(type) as NetworkBehaviour;
            if (behaviour != null){
                behaviour.Read(reader);
            }
            else
                Debug.LogError("Cast failed " + type);
        }

        public void HandleNetworkTransform(BinaryReader reader)
        {
            reader.BaseStream.Position = 0;
            // Read Transform
            Vector3 newPos = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            Quaternion newQuat = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),reader.ReadSingle());
            Vector3 newScale = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

            transform.position = newPos;
            transform.rotation = newQuat;
            transform.localScale = newScale;
        }

        public MemoryStream SendNetworkTransform()
        {
            MemoryStream _stream = new MemoryStream();
            BinaryWriter _writer = new BinaryWriter(_stream);
            
            if (NetworkManager.Instance == false)
            {
                Debug.LogWarning("No NetworkManager or NetworkObject");
            }
            
            // Type objectType = this.GetType();
            // _writer.Write(objectType.AssemblyQualifiedName);
            // _writer.Write(globalObjectIdHash);
            // _writer.Write((int)NetworkAction.TRANSFORM);
            
            _writer.Write((float)transform.position.x);
            _writer.Write((float)transform.position.y);
            _writer.Write((float)transform.position.z);
            
            _writer.Write((float)transform.rotation.x);
            _writer.Write((float)transform.rotation.y);
            _writer.Write((float)transform.rotation.z);
            _writer.Write((float)transform.rotation.w);
            
            _writer.Write((float)transform.localScale.x);
            _writer.Write((float)transform.localScale.y);
            _writer.Write((float)transform.localScale.z);
            
            //NetworkManager.Instance.AddStateStreamQueue(_stream);
            return _stream;
        }
        
        private void Update()
        {
            // Send Write to state buffer
            float finalRate = 1.0f / tickRate;
            if (_tickCounter >= finalRate)
            {
                SendNetworkTransform();
                _tickCounter = 0.0f;
            }
            _tickCounter += Time.deltaTime;
        }
    }
}