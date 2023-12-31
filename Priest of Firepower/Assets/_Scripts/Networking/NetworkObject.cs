using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using _Scripts.Networking.Replication;
using _Scripts.Networking.Utility;
using _Scripts.Player;
using Cinemachine.Utility;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace _Scripts.Networking
{
    public enum TransformAction
    {
        INTERPOLATE,
        NETWORK_SET,
        NETWORK_SEND,
        NONE
    }
    
    [Serializable]
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
        
        #region Transform Info
        [Header("Transform sync")]
        public float tickRate = 10.0f; // Network writes inside a second.
        public bool synchronizeTransform = true;
        public bool sendEveryChange = false;
        public bool sendTickChange = true;
        public bool clientSendReplicationData = false;
        public float accelerationFactor = 5f;
        public float lagThreshold = 1.5f; // Tweak this threshold as needed
        public float tpThreshold = 10f;
        TransformData newTransformData;
        TransformData currentTransfromData;
        TransformData lastTransformSentData;
        private object _lockCurrentTransform = new object();
        //prediction variables
        private TransformData predictedPosition;
        private Stopwatch timerPacketFrequency = new Stopwatch();
        private float timeBetweenPackets;
        private Vector2 lastDirection;
        private bool PredictPosition = false;
        private Interpolator interpolator;
        #endregion
        
        #region Internal Info
        [Header("Internal info")]
        [SerializeField] private float tickCounter = 0.0f;
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool isInterpolating = false;
        [SerializeField] private TransformAction lastAction = TransformAction.NONE;
        public long sequenceNum = 0;
        public long lastProcessedSequenceNum = -1;
        public float interpolationTime = 0.1f; 
        public float speed =0f;//velocity of the obj to calculate interpolation
        #endregion
        [SerializeField] public bool isDeSpawned = false;

        private void Awake()
        {
            newTransformData = new TransformData(transform.position, transform.rotation, transform.localScale);
            lastTransformSentData = new TransformData(transform.position, transform.rotation, transform.localScale);
        }

        private void Start()
        {
            timerPacketFrequency.Start();
        }

        #region Network Transforms

        private void UpdatePosition(TransformAction action,Vector3 newPos,float rotZ, UInt64 sequenceState )
        {

            if (TryGetComponent<Player.Player>(out Player.Player player) && player.isOwner())
            {
                // Discard update position as the client owner moves by himself
                return;
            }

            lock (_lockCurrentTransform)
            {
                TransformData newReceivedTransformData =
                    new TransformData(transform.position, transform.rotation, transform.localScale);

                newReceivedTransformData.action = action;
                newReceivedTransformData.sequenceNumber = sequenceState;

                // Calculate time between packets
                timeBetweenPackets = timerPacketFrequency.ElapsedMilliseconds;
                timerPacketFrequency.Restart();

                Player.Player p = GetComponent<Player.Player>();
                if (p != null)
                {
                    if (p.state == PlayerState.IDLE)
                    {
                        PredictPosition = false;
                        lastDirection = Vector3.zero;
                        predictedPosition.position = transform.position;
                    }
                    else if (p.state == PlayerState.MOVING)
                    {
                        PredictPosition = true;
                        lastDirection = (Vector2)(newPos - transform.position).normalized;
                        predictedPosition.position = newPos;
                        predictedPosition.position = newPos + (Vector3)(speed * (timeBetweenPackets * 0.001f) * lastDirection);
                    }
                }

                lastAction = TransformAction.NETWORK_SET;
                isInterpolating = true;

                newReceivedTransformData.position = newPos;
                newReceivedTransformData.rotation.z = rotZ;
                newTransformData = newReceivedTransformData;
                
                if(showDebugInfo) Debug.Log($"ID: {globalObjectIdHash}, Receiving transform network: {newPos}");
            }
        }
        public void ReadReplicationTransform(BinaryReader reader, UInt64 senderId, Int64 timeStamp, UInt64 sequenceState)
        {
            //read data on second thread
            int actionValue = reader.ReadInt32();
            ushort tick = reader.ReadUInt16();
            Vector2 newPos = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            float rotZ = reader.ReadSingle();

            UpdatePosition((TransformAction)actionValue,newPos,rotZ, sequenceState);
        }

        public void WriteReplicationTransform(TransformAction transformAction)
        {
            // if (NetworkManager.Instance.IsClient())
            // {
            //     if (clientSendReplicationData == false)
            //         return;
            // }

            // Serialize
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            if (NetworkManager.Instance == false)
            {
                Debug.LogWarning("No NetworkManager or NetworkObject");
            }
            
            writer.Write((int)transformAction);
            if (NetworkManager.Instance.IsHost())
            {
                writer.Write(NetworkManager.Instance.GetServer()._currentTick);
            }
            else
            {
                writer.Write(NetworkManager.Instance.GetClient().ServerTick);
            }
            writer.Write((float)transform.position.x);
            writer.Write((float)transform.position.y);
            writer.Write(transform.rotation.eulerAngles.z);
            if (showDebugInfo) Debug.Log($"Sending transform isHost: {NetworkManager.Instance.IsHost()}");
            int size = stream.ToArray().Length;
            if(showDebugInfo)
                Debug.Log($"ID: {globalObjectIdHash}, Sending transform network: {transformAction} {transform.position}, {transform.rotation}, size: {size}");
            
            // Enqueue to the output object sate stream buffer.
            lastTransformSentData.position = transform.position;
            ReplicationHeader replicationHeader = new ReplicationHeader(globalObjectIdHash, this.GetType().FullName, ReplicationAction.TRANSFORM, size);
            NetworkManager.Instance.AddStateStreamQueue(replicationHeader, stream);
            lastAction = TransformAction.NETWORK_SEND;
        }

        #endregion
        private readonly Dictionary<Type, NetworkBehaviour> behaviourCache = new Dictionary<Type, NetworkBehaviour>();
        public void HandleNetworkBehaviour(BinaryReader reader, ReplicationHeader header, Int64 timeStamp, UInt64 sequenceNumberState)
        {
            long currentPosition = reader.BaseStream.Position;
            Type type = Type.GetType(header.objectFullName);
            if (typeof(NetworkBehaviour).IsAssignableFrom(type))
            {
                NetworkBehaviour behaviour;

                // Try to get the cached behaviour
                if (behaviourCache.TryGetValue(type, out behaviour))
                {
                    // Use the cached behaviour
                    behaviour.ReadReplicationPacket(reader, currentPosition);
                }
                else
                {
                    behaviour = GetComponent(type) as NetworkBehaviour;

                    // Cache the result
                    if (behaviour != null)
                    {
                        behaviourCache[type] = behaviour;
                        behaviour.ReadReplicationPacket(reader, currentPosition);
                    }
                    else
                    {
                        Debug.LogError($"NetworkBehaviour component not found on the GameObject id: {header.id} for type {type}");
                    }
                    
                    // // If not cached, try to get the component on the main thread
                    // UnityMainThreadDispatcher.Dispatcher.Enqueue(() => {
                    //     behaviour = GetComponent(type) as NetworkBehaviour;
                    //
                    //     // Cache the result
                    //     if (behaviour != null)
                    //     {
                    //         behaviourCache[type] = behaviour;
                    //         behaviour.ReadReplicationPacket(reader, currentPosition);
                    //     }
                    //     else
                    //     {
                    //         Debug.LogError($"NetworkBehaviour component not found on the GameObject for type {type}");
                    //     }
                    // });
                }
            }
            else
            {
                Debug.LogError($"{type} is not a valid NetworkBehaviour type");
            }
        }
        
        public void HandleNetworkInput(BinaryReader reader, UInt64 packetSender, Int64 timeStamp, UInt64 sequenceNumberInput, InputHeader header)
        {
            long currentPosition = reader.BaseStream.Position;
            NetworkBehaviour behaviour = GetComponent(Type.GetType(header.objectFullName)) as NetworkBehaviour;

            if (behaviour != null)
            {
                if (NetworkManager.Instance.IsClient())
                {
                    behaviour.ReceiveInputFromServer(header, reader);
                }
                else if (NetworkManager.Instance.IsHost())
                {
                    behaviour.ReceiveInputFromClient(header, reader);
                }
            }
            else
                Debug.LogError($"NetworkBehaviour cast failed {Type.GetType(header.objectFullName)}");
        }

        private void Update()
        {
            if (Time.frameCount <= 300) return;

            // only send the position as a server

            if (NetworkManager.Instance.IsClient() || isDeSpawned) return;

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
                currentTransfromData = newTransformData;
            }
            if (currentTransfromData.action == TransformAction.INTERPOLATE && isInterpolating )
            {
                lastAction = TransformAction.INTERPOLATE;
                
                Vector3 pointA = transform.position; // Current position
                Vector3 pointB = currentTransfromData.position; // New position
                // Calculate the distance between pointA and pointB
                float distance = Vector3.Distance(pointA, pointB);


                // Speed up the interpolation if it's lagging behind
                float adjustedSpeed = speed;
                if (distance > lagThreshold && distance < tpThreshold)
                {
                    adjustedSpeed *= accelerationFactor;
                }
                else if(distance < tpThreshold)
                {
                    //tp
                    transform.position = currentTransfromData.position;
                }
                // Calculate the time needed to travel the distance at the given speed
                float travelTime = distance / adjustedSpeed;
                // Calculate the interpolation factor based on the elapsed time
                float t = Mathf.Clamp01(Time.deltaTime / travelTime);

                
                // Perform interpolation towards the new target position
                //if (showDebugInfo) Debug.Log($"Interpolating from {pointA} to next position {pointB}");
                Vector3 nextPos = Vector3.LerpUnclamped(pointA, pointB, t);
                
                if (!nextPos.IsNaN())
                    transform.position = nextPos;
                
                if (t >= 1.0f)
                {
                    isInterpolating = false;
                    //if reached destination of the server, keep moving towards that direction
                    if (PredictPosition)
                    {
                        currentTransfromData.position = predictedPosition.position;
                        PredictPosition = false;
                        isInterpolating = true;
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
        public ulong sequenceNumber;
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

    public class TransformUpdate 
    {
        public ushort Tick { get; private set; }
        public bool IsTeleport { get; private set; }
        public Vector2 Position { get; private set;}
        public float Rotation { get;private set; }
        public TransformUpdate(ushort tick,bool teleport, Vector2 position,float rotation)
        {
            Tick = tick;
            IsTeleport = teleport;
            Position = position;    
            Rotation = rotation;    
        }
    }
}