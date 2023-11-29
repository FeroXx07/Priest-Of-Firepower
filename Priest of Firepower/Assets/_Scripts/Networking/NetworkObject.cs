using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using _Scripts.Interfaces;
using _Scripts.Networking;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

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
        public float speed =0f;
        
        [SerializeField] private TransformAction lastAction = TransformAction.NONE;
        
        public List<TransformData> receivedTransformList = new List<TransformData>();
        TransformData newTransformData;
        private object _lockCurrentTransform = new object();
        //prediction variables
        private TransformData predictedPosition;
        private Stopwatch timerPacketFrequency = new Stopwatch();
        private float timeBetweenPackets;
        private Vector2 lastDirection;
        private bool PredictPosition = false;
        
        public long sequenceNum = 0;
        public long lastProcessedSequenceNum = -1;
        public float interpolationTime = 0.1f; 
        [SerializeField] private float interpolationTimer = 0f;
        #endregion
        
        private void Awake()
        {
            newTransformData = new TransformData(transform.position, transform.rotation, transform.localScale);
        }

        private void Start()
        {
            timerPacketFrequency.Start();
            if (TryGetComponent<Player.Player>( out Player.Player player))
            {
                speed = player.speed;
            }
        }

        #region Network Transforms

        public void ReadReplicationTransform(BinaryReader reader, UInt64 senderId, Int64 timeStamp, UInt64 sequenceState)
        {
            //Check if the transfrom belongs to the client if so avoid reading the data sent by the server
            //client->send data, server -> broadcast, skip if owner
            if (TryGetComponent<Player.Player>(out Player.Player player))
            {
                if (player.isOwner())
                {
                    Debug.Log("Player is owner, skiping T data revieced");
                    // Discard the packet and skip the remaining bytes
                    int remainingBytes = (sizeof(float) * 3 + sizeof(Int64) + sizeof(Int32) );
                    reader.BaseStream.Seek(remainingBytes, SeekOrigin.Current);
                    return;
                }
            }
            
            // init transform data
            TransformData newReceivedTransformData =
                new TransformData(transform.position, transform.rotation, transform.localScale);
            newReceivedTransformData.sequenceNumber = reader.ReadInt64();
            newReceivedTransformData.action = (TransformAction)reader.ReadInt32();

            lock (_lockCurrentTransform)
            {
                if(showDebugInfo)
                    Debug.Log("new: "+newReceivedTransformData.sequenceNumber+" current: " +newTransformData.sequenceNumber);
                
                // Check if the packet is outdated
                if (newReceivedTransformData.sequenceNumber <= newTransformData.sequenceNumber)
                {
                    // Discard the packet and skip the remaining bytes
                    int remainingBytes = (sizeof(float) * 3);
                    reader.BaseStream.Seek(remainingBytes, SeekOrigin.Current);
                    return;
                }
                
                // Serialize
                Vector3 newPos = new Vector3(reader.ReadSingle(), reader.ReadSingle());
                float rotZ = reader.ReadSingle();

                //get time between packets
                timeBetweenPackets = timerPacketFrequency.ElapsedMilliseconds;
                Debug.Log("time between packets: "+timeBetweenPackets);
                timerPacketFrequency.Restart();
                //calculate the direction of movement
                lastDirection = (Vector2)(newPos - transform.position).normalized;
                PredictPosition = true;
                predictedPosition.position = newPos + (Vector3)(speed * (timeBetweenPackets * 0.001f) * lastDirection) ;  


                lastAction = TransformAction.NETWORK_SET;

                isInterpolating = true;

                // Cache the new value
                newReceivedTransformData.position = newPos;
                newReceivedTransformData.rotation.z = rotZ;
                newTransformData.action = TransformAction.INTERPOLATE;
                //store new pos
                newTransformData = newReceivedTransformData;
            

                if(showDebugInfo)
                    Debug.Log($"ID: {globalObjectIdHash}, Receiving transform network: {newPos}");
            
            }
        }

        public void WriteReplicationTransform(TransformAction transformAction)
        {
            if (NetworkManager.Instance.IsClient())
            {
                if (clientSendReplicationData == false)
                    return;
            }
            
            // Serialize
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
            _writer.Write(sequenceNum);
            _writer.Write((int)transformAction);
            
            _writer.Write((float)transform.position.x);
            _writer.Write((float)transform.position.y);
            _writer.Write(transform.rotation.eulerAngles.z);
            sequenceNum++;
            
            if(showDebugInfo)
                Debug.Log($"ID: {globalObjectIdHash}, Sending transform network: {transformAction} {transform.position}, {transform.rotation}, size: {_stream.ToArray().Length}");
            
            // Enqueue to the output object sate stream buffer.
            NetworkManager.Instance.AddStateStreamQueue(_stream);
            lastAction = TransformAction.NETWORK_SEND;
        }

        #endregion

        public void HandleNetworkBehaviour(BinaryReader reader, UInt64 packetSender, Int64 timeStamp, UInt64 sequenceNumberState, Type type)
        {
            long currentPosition = reader.BaseStream.Position;
            NetworkBehaviour behaviour = GetComponent(type) as NetworkBehaviour;

            if (behaviour != null)
                behaviour.ReadReplicationPacket(reader, currentPosition);
            else
                Debug.LogError($"NetworkBehaviour cast failed {type}");
        }
        
        public void HandleNetworkInput(BinaryReader reader, UInt64 packetSender, Int64 timeStamp, UInt64 sequenceNumberInput, Type type)
        {
            long currentPosition = reader.BaseStream.Position;
            NetworkBehaviour behaviour = GetComponent(type) as NetworkBehaviour;

            if (behaviour != null)
            {
                if (NetworkManager.Instance.IsClient())
                {
                    behaviour.ReceiveInputFromServer(reader);
                }
                else if (NetworkManager.Instance.IsHost())
                {
                    behaviour.ReceiveInputFromClient(reader);
                }
            }
            else
                Debug.LogError($"NetworkBehaviour cast failed {type}");
        }

        private void Update()
        {
            if (Time.frameCount <= 300) return;

            // only send the position as a server

            if (NetworkManager.Instance.IsClient()) return;

            bool hasChanged = transform.hasChanged;

            if (hasChanged && isInterpolating)
            {
                hasChanged = false;
            }

            if (hasChanged && lastAction == TransformAction.NETWORK_SET)
            {
                hasChanged = false;
            }

            if (hasChanged && sendEveryChange)
            {
                WriteReplicationTransform(TransformAction.INTERPOLATE);
                lastAction = TransformAction.NETWORK_SEND;
                hasChanged = false;
            }

            // Send Write to state buffer
            float finalRate = 1.0f / tickRate;
            tickCounter += Time.deltaTime;
            if (tickCounter >= finalRate && hasChanged)
            {
                tickCounter = 0.0f;
                if (!isInterpolating && sendTickChange) // Only server should be able to these send sanity snapshots!
                    WriteReplicationTransform(TransformAction.INTERPOLATE);
            }

            tickCounter = tickCounter >= float.MaxValue - 100 ? 0.0f : tickCounter;

            transform.hasChanged = false;
        }

        private void FixedUpdate()
        {
            Interpolate();
        }

        void Interpolate()
        {
            lock (_lockCurrentTransform)
            {
                if (newTransformData.action == TransformAction.INTERPOLATE && isInterpolating )
                {
                    lastAction = TransformAction.INTERPOLATE;
                    
                    Vector3 pointA = transform.position; // Current position
                    Vector3 pointB = newTransformData.position; // New position
                    float speed = 7.0f; // Speed of the player
                    // Calculate the distance between pointA and pointB
                    float distance = Vector3.Distance(pointA, pointB);
                    // Calculate the time needed to travel the distance at the given speed
                    float travelTime = distance / speed;
                    // Calculate the interpolation factor based on the elapsed time
                    float t = Mathf.Clamp01(Time.deltaTime / travelTime);
                    // Perform interpolation towards the new target position
                    if (showDebugInfo) Debug.Log($"Interpolating from {pointA} to next position {pointB}");
                    transform.position = Vector3.LerpUnclamped(pointA, pointB, t);
                    
                    if (t >= 1.0f)
                    {
                        isInterpolating = false;
                        //if reached destination of the server, keep moving towards that direction
                        if (PredictPosition)
                        {
                            newTransformData.position = predictedPosition.position;
                            PredictPosition = false;
                            isInterpolating = true;
                        }
                        
                    }
                }
            }
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