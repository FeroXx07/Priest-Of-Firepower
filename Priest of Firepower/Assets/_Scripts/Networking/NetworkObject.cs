using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Scripts.Networking
{
    public class NetworkObject : MonoBehaviour
    {
        [SerializeField] private UInt64 globalObjectIdHash = 10;
        public bool synchronizeTransform = true;
        [SerializeField] private bool isTransformDirty = false;
        [SerializeField] private bool isLastTransformFromNetwork = false;
        private Transform _newTransform;
        private Queue<System.Action> TODO = new Queue<System.Action>();

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
            Vector3 newPos = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            Quaternion newQuat = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),reader.ReadSingle());
            Vector3 newScale = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            
            UnityMainThreadDispatcher.Dispatcher.Enqueue(() => {  
                transform.position = newPos;
                transform.rotation = newQuat;
                transform.localScale = newScale;
            });
            
            Debug.Log($"ID: {globalObjectIdHash}, Receiving transform network: {newPos}, {newQuat.eulerAngles}, {newScale}");

            isTransformDirty = false;
            isLastTransformFromNetwork = true;
        }

        private void SetTransformThreaded(Vector3 newPos, Quaternion newQuat, Vector3 newScale)
        {
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
            
            Type objectType = this.GetType();
            _writer.Write(objectType.AssemblyQualifiedName);
            _writer.Write(globalObjectIdHash);
            _writer.Write((int)NetworkAction.TRANSFORM);
            
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
            
            Debug.Log($"ID: {globalObjectIdHash}, Sending transform network: {transform.position}, {transform.rotation}, size: {_stream.ToArray().Length}");
            NetworkManager.Instance.AddStateStreamQueue(_stream);
            isTransformDirty = false;
            
            return _stream;
        }
        
        private void Update()
        {
            if (Time.frameCount <= 5)
                return;
            
            if (transform.hasChanged)
            {
                isTransformDirty = true;
                transform.hasChanged = false;
            }
            
            // Send Write to state buffer
            float finalRate = 1.0f / tickRate;
            if (_tickCounter >= finalRate)
            {
                if (!isLastTransformFromNetwork && isTransformDirty) SendNetworkTransform();
                _tickCounter = 0.0f;
            }
            _tickCounter += Time.deltaTime;
        }
    }
}