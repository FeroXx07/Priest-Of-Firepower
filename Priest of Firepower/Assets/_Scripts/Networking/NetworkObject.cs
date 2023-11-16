using System;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Scripts.Networking
{
    public class NetworkObject : MonoBehaviour
    {
        #region Fields

        [SerializeField] private UInt64 globalObjectIdHash = 10;
        public bool synchronizeTransform = true;
        [SerializeField] private bool isTransformDirty = false;
        [SerializeField] private bool isLastTransformFromNetwork = true;
        public float tickRate = 10.0f; // Network writes inside a second.
        [SerializeField] private float tickCounter = 0.0f;
        [SerializeField] private bool showDebugInfo = false;
        #endregion

        #region NetworkId

        public void SetNetworkId(UInt64 id)
        {
            globalObjectIdHash = id;
        }

        public UInt64 GetNetworkId()
        {
            return globalObjectIdHash;
        }

        #endregion

        #region Network Transforms

        public void HandleNetworkTransform(BinaryReader reader)
        {
            // Serialize
            Vector3 newPos = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            Quaternion newQuat = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                reader.ReadSingle());
            Vector3 newScale = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            
            // Enqueue an action to modify transform, which cannot be read/write outside main thread.
            UnityMainThreadDispatcher.Dispatcher.Enqueue(() =>
            {
                transform.position = newPos;
                transform.rotation = newQuat;
                transform.localScale = newScale;
            });
            
            if(showDebugInfo)
                Debug.Log($"ID: {globalObjectIdHash}, Receiving transform network: {newPos}, {newQuat.eulerAngles}, {newScale}");
            
            isLastTransformFromNetwork = true;
        }

        public MemoryStream SendNetworkTransform()
        {
            // DeSerialize
            MemoryStream _stream = new MemoryStream();
            BinaryWriter _writer = new BinaryWriter(_stream);
            if (NetworkManager.Instance == false)
            {
                Debug.LogWarning("No NetworkManager or NetworkObject");
            }

            Type objectType = this.GetType();
            _writer.Write(objectType.FullName);
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
            
            if(showDebugInfo)
                Debug.Log($"ID: {globalObjectIdHash}, Sending transform network: {transform.position}, {transform.rotation}, size: {_stream.ToArray().Length}");
            
            // Enqueue to the output object sate stream buffer.
            NetworkManager.Instance.AddStateStreamQueue(_stream);
            isTransformDirty = false;
            return _stream;
        }

        #endregion

        public void HandleNetworkBehaviour(Type type, BinaryReader reader)
        {
            // Redirect stream from the input object state stream buffer to the NetworkBehaviour DeSerializer
            NetworkBehaviour behaviour = GetComponent(type) as NetworkBehaviour;
            if (behaviour != null)
                behaviour.Read(reader);
            else
                Debug.LogError("Cast failed " + type);
        }

        private void Update()
        {
            if (Time.frameCount <= 300) return;
            
            if (transform.hasChanged)
            {
                if (isLastTransformFromNetwork)
                {
                    // Reset the flag as this change was due to network update
                    transform.hasChanged = false;
                    isLastTransformFromNetwork = false;
                }
                else
                {
                    // This transform was changed locally, mark it as dirty
                    isTransformDirty = true;
                    transform.hasChanged = false;
                }
            }

            // Send Write to state buffer
            float finalRate = 1.0f / tickRate;
            if (tickCounter >= finalRate)
            {
                if (isTransformDirty) 
                    SendNetworkTransform();
                tickCounter = 0.0f;
            }

            tickCounter += Time.deltaTime;
        }
    }
}