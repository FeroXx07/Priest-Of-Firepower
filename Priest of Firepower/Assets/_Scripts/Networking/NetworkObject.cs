using System;
using System.Collections.Generic;
using System.IO;
using _Scripts.Interfaces;
using _Scripts.Networking;
using Unity.Mathematics;
using UnityEngine;

namespace _Scripts.Networking
{
    public enum TransformAction
    {
        INTERPOLATE,
        NETWORK_SET,
        NETWORK_SEND,
        LOCAL_SET,
        NONE
    }
    public class NetworkObject : MonoBehaviour
    {
        #region NetworkId
        [SerializeField] private UInt64 globalObjectIdHash = 0;
        public void SetNetworkId(UInt64 id)
        {
            globalObjectIdHash = id;
        }

        public UInt64 GetNetworkId()
        {
            return globalObjectIdHash;
        }
        #endregion
        
        #region TickInfo
        public float tickRate = 10.0f; // Network writes inside a second.
        [SerializeField] private float tickCounter = 0.0f;
        [SerializeField] private bool showDebugInfo = false;
        #endregion
        
        #region TransformInfo
        public bool synchronizeTransform = true;
        public bool sendEveryChange = false;
        public bool sendTickChange = true;
        [SerializeField] private bool isInterpolating = false;
        [SerializeField] private TransformAction lastAction = TransformAction.NONE;
        public TransformData lastReceivedTransformData;
        public float lerpValue = 0.25f;
        #endregion

        private Queue<Action> _actionsToDo = new Queue<Action>();
        
        private void Awake()
        {
            // previousTransform = new TransformData(Vector3.zero, quaternion.identity, Vector3.one );
            lastReceivedTransformData = new TransformData(Vector3.zero, quaternion.identity, Vector3.one );
        }

        #region Network Transforms
        public void ReadReplicationTransform(BinaryReader reader)
        {
            lastReceivedTransformData.action = (TransformAction)reader.ReadInt32();
            lastReceivedTransformData.timeStamp = reader.ReadInt64();
            
            // Serialize
            Vector3 newPos = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            Quaternion newQuat = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                reader.ReadSingle());
            Vector3 newScale = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            
            // Before starting to interpolate to the new value,
            // If previous value hasn't been interpolated completely, finish it
           
            _actionsToDo.Clear();
            _actionsToDo.Enqueue(() => {Debug.Log("New Interpolation NETWORK_SET");
            
                transform.position = lastReceivedTransformData.position;
                transform.rotation = lastReceivedTransformData.rotation;
                transform.localScale = lastReceivedTransformData.scale;
                
                lastAction = TransformAction.NETWORK_SET;});
            
            
            // Cache the new value
            lastReceivedTransformData.position = newPos;
            lastReceivedTransformData.rotation = newQuat;
            lastReceivedTransformData.scale = newScale;
            
            // Start doing interpolation if cached action says so
            if (lastReceivedTransformData.action == TransformAction.INTERPOLATE)
            {
                isInterpolating = true;
            }
            else if (lastReceivedTransformData.action == TransformAction.NETWORK_SET)
            {
                _actionsToDo.Clear();                
                _actionsToDo.Enqueue(() =>
                {
                    Debug.Log("Direct NETWORK_SET");
                    transform.position = newPos;
                    transform.rotation = newQuat;
                    transform.localScale = newScale;

                    lastAction = TransformAction.NETWORK_SET;
                });
                
                // UnityMainThreadDispatcher.Dispatcher.RemoveLastEnqueuedAction();
                // UnityMainThreadDispatcher.Dispatcher.Enqueue(() =>
                // {
                //    
                // });
            }
            
            if(showDebugInfo)
                Debug.Log($"ID: {globalObjectIdHash}, Receiving transform network: {newPos}, {newQuat.eulerAngles}, {newScale}");
        }

        public MemoryStream WriteReplicationTransform(TransformAction transformAction)
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
            _writer.Write((int)ReplicationAction.TRANSFORM);
            _writer.Write((int)transformAction);
            long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            _writer.Write(milliseconds);
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
                Debug.Log($"ID: {globalObjectIdHash}, Sending transform network: {transformAction} {transform.position}, {transform.rotation}, size: {_stream.ToArray().Length}");
            
            // Enqueue to the output object sate stream buffer.
            NetworkManager.Instance.AddStateStreamQueue(_stream);
            lastAction = TransformAction.NETWORK_SEND;
            return _stream;
        }

        #endregion

        public void HandleNetworkBehaviour(Type type, BinaryReader reader)
        {
            long currentPosition = reader.BaseStream.Position;
            NetworkBehaviour behaviour = GetComponent(type) as NetworkBehaviour;

            if (behaviour != null)
                behaviour.ReadReplicationPacket(reader, currentPosition);
            else
                Debug.LogError("Cast failed " + type);
        }
        
        public void HandleNetworkInput(Type type, BinaryReader reader)
        {
            long currentPosition = reader.BaseStream.Position;
            NetworkBehaviour behaviour = GetComponent(type) as NetworkBehaviour;

            if (behaviour != null)
            {
                INetworkInput input = behaviour as INetworkInput;
                if (input != null) input.ReceiveInputFromClient(reader);
            }
            else
                Debug.LogError("Cast failed " + type);
        }

        private void FixedUpdate()
        {
            if (Time.frameCount <= 300) return;

            if (transform.hasChanged && isInterpolating)
            {
                transform.hasChanged = false;
            }

            if (transform.hasChanged && lastAction == TransformAction.NETWORK_SET)
            {
                transform.hasChanged = false;
            }
            
            if (transform.hasChanged && sendEveryChange)
            {
                WriteReplicationTransform(TransformAction.INTERPOLATE);
                lastAction = TransformAction.NETWORK_SEND;
                transform.hasChanged = false;
            }


            foreach (Action action in _actionsToDo)
            {
                action?.Invoke();
            }


            // Check if interpolation is needed and apply it
            if (lastReceivedTransformData.action == TransformAction.INTERPOLATE && isInterpolating)
            {
                lastAction = TransformAction.INTERPOLATE;
                Debug.Log($"Interpolating to position: {lastReceivedTransformData.position}, rotation: {lastReceivedTransformData.rotation}, scale_ {lastReceivedTransformData.scale}");
                long currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                float lerpFactor = Mathf.Clamp01((currentTime - lastReceivedTransformData.timeStamp) / lerpValue); // 0.5f is an example time to interpolate
                transform.position = Vector3.Lerp(transform.position, lastReceivedTransformData.position, lerpFactor);
                transform.rotation = Quaternion.Slerp(transform.rotation, lastReceivedTransformData.rotation, lerpFactor);
                transform.localScale = Vector3.Slerp(transform.localScale, lastReceivedTransformData.scale, lerpFactor);

                if (lerpFactor >= 1.0f)
                {
                    isInterpolating = false;
                }
            }
            
            // Send Write to state buffer
            float finalRate = 1.0f / tickRate;
            if (tickCounter >= finalRate)
            {
                tickCounter = 0.0f;
                
                if (!isInterpolating && sendTickChange) // Only server should be able to these send sanity snapshots!
                    WriteReplicationTransform(TransformAction.NETWORK_SET);
            }

            tickCounter = tickCounter >= float.MaxValue - 100 ? 0.0f : tickCounter;
            
            tickCounter += Time.deltaTime;
        }
    }
}

[Serializable]
public struct TransformData
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public long timeStamp;
    public TransformAction action;
    public TransformData(Vector3 pos, Quaternion rot, Vector3 s)
    {
        position = new Vector3(pos.x, pos.y, pos.z);
        rotation = new Quaternion(rot.x, rot.y, rot.z,rot.w);
        scale = new Vector3(s.x, s.y, s.z);
        action = TransformAction.INTERPOLATE;
        timeStamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    }
}