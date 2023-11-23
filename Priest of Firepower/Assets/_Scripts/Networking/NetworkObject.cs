using System;
using System.Collections.Generic;
using System.IO;
using _Scripts.Interfaces;
using _Scripts.Networking;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

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
        public bool clientSendReplicationData = false;
        [SerializeField] private bool isInterpolating = false;
        [SerializeField] private TransformAction lastAction = TransformAction.NONE;
        public List<TransformData> receivedTransforms = new List<TransformData>();
        public float lerpValue = 0.25f;
        public long sequenceNum = 0;
        public float interpolationDuration = 0.1f;
        #endregion

        //private Queue<Action> _actionsToDo = new Queue<Action>();
        
        private void Awake()
        {
            // previousTransform = new TransformData(Vector3.zero, quaternion.identity, Vector3.one );
            // newReceivedTransformData = new TransformData(transform.position, lastPosData.rotation, lastPosData.scale);
            // lastPosData = new TransformData(transform.position, lastPosData.rotation, lastPosData.scale );
        }

        #region Network Transforms
        public void ReadReplicationTransform(BinaryReader reader)
        {
            TransformData newReceivedTransformData = new TransformData(Vector3.zero, quaternion.identity, Vector3.one);
            newReceivedTransformData.action = (TransformAction)reader.ReadInt32();
            newReceivedTransformData.timeStamp = reader.ReadInt64();
            newReceivedTransformData.sequenceNumber = reader.ReadInt64();
            
            // Serialize
            Vector3 newPos = new Vector3(reader.ReadSingle(), reader.ReadSingle());
            Quaternion newQuat = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                reader.ReadSingle());
            //Vector3 newScale = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            
            // Before starting to interpolate to the new value,
            // If previous value hasn't been interpolated completely, finish it
            // if(showDebugInfo) Debug.Log($"Finish interpolation NETWORK_SET: position {newReceivedTransformData.position} {newReceivedTransformData.rotation.eulerAngles}");
            // transform.position = newReceivedTransformData.position;
            // transform.rotation = newReceivedTransformData.rotation;
            // lastAction = TransformAction.NETWORK_SET;
            // UnityMainThreadDispatcher.Dispatcher.Enqueue(() => {
            //     if(showDebugInfo) Debug.Log($"Finish interpolation NETWORK_SET: position {lastReceivedTransformData.position} {lastReceivedTransformData.rotation.eulerAngles}");
            //     transform.position = lastReceivedTransformData.position;
            //     transform.rotation = lastReceivedTransformData.rotation;
            //     //transform.localScale = lastReceivedTransformData.scale;
            //     
            //     lastAction = TransformAction.NETWORK_SET;});
            
            // // Cache the new value
            newReceivedTransformData.position = newPos;
            newReceivedTransformData.rotation = newQuat;
            // //lastReceivedTransformData.scale = newScale;
            //
            // // Start doing interpolation if cached action says so
            // if (newReceivedTransformData.action == TransformAction.INTERPOLATE)
            // {
            //     isInterpolating = true;
            // }
            // else if (newReceivedTransformData.action == TransformAction.NETWORK_SET)
            // {
            //     if(showDebugInfo) Debug.Log($"New NETWORK_SET: position {newPos} {newQuat.eulerAngles}");
            //     transform.position = newPos;
            //     transform.rotation = newQuat;
            //     lastAction = TransformAction.NETWORK_SET;
            //     // UnityMainThreadDispatcher.Dispatcher.Enqueue(() =>
            //     // {
            //     //     if(showDebugInfo) Debug.Log($"New NETWORK_SET: position {newPos} {newQuat.eulerAngles}");
            //     //     transform.position = newPos;
            //     //     transform.rotation = newQuat;
            //     //     //transform.localScale = newScale;
            //     //
            //     //     lastAction = TransformAction.NETWORK_SET;
            //     // });
            // }
            
            if(showDebugInfo)
                Debug.Log($"ID: {globalObjectIdHash}, Receiving transform network: {newPos}, {newQuat.eulerAngles}");
        }

        public void WriteReplicationTransform(TransformAction transformAction)
        {
            if (NetworkManager.Instance.IsClient())
            {
                if (clientSendReplicationData == false)
                    return;
            }
            
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
            _writer.Write(sequenceNum);
            _writer.Write((float)transform.position.x);
            _writer.Write((float)transform.position.y);
            //_writer.Write((float)transform.position.z);
            _writer.Write((float)transform.rotation.x);
            _writer.Write((float)transform.rotation.y);
            _writer.Write((float)transform.rotation.z);
            _writer.Write((float)transform.rotation.w);
            //_writer.Write((float)transform.localScale.x);
            //_writer.Write((float)transform.localScale.y);
            //_writer.Write((float)transform.localScale.z);
            sequenceNum++;
            
            if(showDebugInfo)
                Debug.Log($"ID: {globalObjectIdHash}, Sending transform network: {transformAction} {transform.position}, {transform.rotation}, size: {_stream.ToArray().Length}");
            
            // Enqueue to the output object sate stream buffer.
            NetworkManager.Instance.AddStateStreamQueue(_stream);
            lastAction = TransformAction.NETWORK_SEND;
        }

        #endregion

        public void HandleNetworkBehaviour(Type type, BinaryReader reader)
        {
            long currentPosition = reader.BaseStream.Position;
            NetworkBehaviour behaviour = GetComponent(type) as NetworkBehaviour;

            if (behaviour != null)
                behaviour.ReadReplicationPacket(reader, currentPosition);
            else
                Debug.LogError($"NetworkBehaviour cast failed {type}");
        }
        
        public void HandleNetworkInput(Type type, BinaryReader reader)
        {
            long currentPosition = reader.BaseStream.Position;
            NetworkBehaviour behaviour = GetComponent(type) as NetworkBehaviour;

            if (behaviour != null)
                behaviour.ReceiveInputFromClient(reader);
            else
                Debug.LogError($"NetworkBehaviour cast failed {type}");
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
            
            // Ensure at least two transforms are received
            if (receivedTransforms.Count < 2)
                return;

            // Sort received transforms based on sequence numbers
            receivedTransforms.Sort((a, b) => a.sequenceNumber.CompareTo(b.sequenceNumber));

            TransformData currentTransform = receivedTransforms[(int)sequenceNum];
            TransformData nextTransform = receivedTransforms[(int)sequenceNum + 1];

            float timeSinceLastTransform = Time.time - currentTransform.timeStamp;
            float interpolationFactor = Mathf.Clamp01(timeSinceLastTransform / interpolationDuration);

            // Interpolate position and rotation
            transform.position = Vector3.Lerp(currentTransform.position, nextTransform.position, interpolationFactor);
            transform.rotation = Quaternion.Slerp(currentTransform.rotation, nextTransform.rotation, interpolationFactor);

            // Check if interpolation is complete
            if (interpolationFactor >= 1.0f)
            {
                sequenceNum++;
                if (sequenceNum >= receivedTransforms.Count - 1)
                {
                    sequenceNum = 0;
                }
            }

            // Remove older packets based on sequence number
            long currentSequenceNumber = currentTransform.sequenceNumber;
            receivedTransforms.RemoveAll(t => t.sequenceNumber <= currentSequenceNumber);
            
            // // Check if interpolation is needed and apply it
            // if (newReceivedTransformData.action == TransformAction.INTERPOLATE && isInterpolating)
            // {
            //     lastAction = TransformAction.INTERPOLATE;
            //     if(showDebugInfo) Debug.Log($"Interpolating from {transform.position} to position: {newReceivedTransformData.position}");
            //     long currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            //     float lerpFactor = Mathf.Clamp01((currentTime - newReceivedTransformData.timeStamp) / lerpValue); // 0.5f is an example time to interpolate
            //     transform.position = Vector3.Lerp(transform.position, newReceivedTransformData.position, lerpFactor);
            //     transform.rotation = Quaternion.Slerp(transform.rotation, newReceivedTransformData.rotation, lerpFactor);
            //     //transform.localScale = Vector3.Slerp(transform.localScale, lastReceivedTransformData.scale, lerpFactor);
            //
            //     if (lerpFactor >= 1.0f)
            //     {
            //         isInterpolating = false;
            //     }
            // }
            
            // Send Write to state buffer
            float finalRate = 1.0f / tickRate;
            if (tickCounter >= finalRate)
            {
                tickCounter = 0.0f;
                
                if (!isInterpolating && sendTickChange) // Only server should be able to these send sanity snapshots!
                    WriteReplicationTransform(TransformAction.INTERPOLATE);
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
    public long sequenceNumber;
    public TransformData(Vector3 pos, Quaternion rot, Vector3 s)
    {
        position = new Vector3(pos.x, pos.y, pos.z);
        rotation = new Quaternion(rot.x, rot.y, rot.z,rot.w);
        scale = new Vector3(s.x, s.y, s.z);
        action = TransformAction.INTERPOLATE;
        timeStamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        sequenceNumber = 0;
    }
}