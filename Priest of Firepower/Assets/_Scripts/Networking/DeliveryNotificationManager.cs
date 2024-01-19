#define MAX_CAPACITY_HISTORICAL_ACK 
using System;
using System.Collections.Generic;
using System.IO;
using _Scripts.Networking.Replication;
using UnityEngine;


namespace _Scripts.Networking
{
    /// <summary>
    /// A layer inside the network manager. It should implement with:
    /// But only for UDP (unreliable packets) and not TCP (reliable packets).
    /// void ProcessIncomingPacket(MemoryStream stream)
    /// and
    /// Packet PreparePacket(UInt64 senderId, PacketType type, ...)
    /// </summary>
    ///
    [Serializable]
    public struct AcknowledgmentWrapper
    {
        public AcknowledgmentWrapper(UInt64 ackSeqNum)
        {
            timeStamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            ACKSeqNum = ackSeqNum;
        }
        public Int64 timeStamp;
        public UInt64 ACKSeqNum;
    }
    
    [Serializable]
    public class DeliveryNotificationManager
    {
        private const int MAX_CAPACITY_HISTORICAL_ACK = 1000;

        private List<Packet> pendingDeliveries = new();
        [SerializeField] private List<UInt64> pendingACKs = new();
        [SerializeField] private List<AcknowledgmentWrapper> historicalACKs = new(MAX_CAPACITY_HISTORICAL_ACK);
        [SerializeField] private List<Int64> pendingDeliveriesTime = new();

        private int failThreshold = 50;
        private int failCounter = 0;
        
        private const int historicalRemoveTimeMs = 1000;
        private void CleanPending(int index)
        {
            pendingDeliveries.RemoveAt(index);
            pendingDeliveriesTime.RemoveAt(index);
        }
        
        private float roundTripTime;
        private const int rttOffset = 0;// timeOutRtt = roundTripTime + someOffset; ex: timeOutRtt = roundTripTime(54ms) + someOffset (15ms)

        private List<UInt64> lastACKsSent = new(); 
        private UInt64 tempIndex = UInt64.MinValue;
        // Send
        public void MakeDelivery(Packet packet)
        {
            lock (pendingDeliveries)
            {
                if (CheckDuplicate<Packet>(packet, pendingDeliveries))
                {
                    Debug.LogError($"DeliveryNotificationManager: Cannot make delivery as it is a duplicate packet. Seq num {packet.sequenceNum}");
                    return;
                }

                pendingDeliveries.Add(packet);
                pendingDeliveriesTime.Add(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
            }
        }   

        private bool OnDeliverySuccess(UInt64 ACK)
        {
            int index = pendingDeliveries.FindIndex(packet => packet.sequenceNum == ACK);
            if (index == -1)
            {
                Debug.LogWarning($"DeliveryNotificationManager: OnDeliverySuccess already processed ACK, Seq: {ACK}");
                return false;
            }
            Debug.Log($"DeliveryNotificationManager: OnDeliverySuccess, Seq: {ACK}");
            CleanPending(index);
            return true;
        }

        private void OnDeliveryFailure(Packet packet)
        {
            int index = pendingDeliveries.FindIndex(p => p.sequenceNum == packet.sequenceNum);
            if (index == -1)
            {
                Debug.LogError($"DeliveryNotificationManager: OnDeliveryFailure error, Seq: {packet.sequenceNum}");
                return;
            }
            
            Debug.Log($"DeliveryNotificationManager: OnDeliveryFailure resend {packet.sequenceNum}");

            // Set the time to current time.
            pendingDeliveriesTime[index] = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            
            NetworkManager.Instance.AddResendPacket(packet);
        }
        
        // Receive
        public bool ReceiveDelivery(Packet packet)
        {
            NetworkManager netManager = NetworkManager.Instance;
            if (historicalACKs.FindIndex(wrapper => wrapper.ACKSeqNum == packet.sequenceNum) != -1)            {
                if (packet.packetType == PacketType.OBJECT_STATE)
                    Debug.LogWarning($"DeliveryNotificationManager: Cannot receive delivery as it is a duplicate packet. " +
                                     $"Seq num {packet.sequenceNum}. Expected next state seq num {netManager.stateSequenceNum.expectedNextSequenceNum}");
                else
                    Debug.LogWarning($"DeliveryNotificationManager: Cannot receive delivery as it is a duplicate packet." +
                                     $" Seq num {packet.sequenceNum}. Expected next input seq num {netManager.inputSequenceNum.expectedNextSequenceNum}");
                // Acknowledgment is pending. No action.
                if (!pendingACKs.Contains(packet.sequenceNum))
                    pendingACKs.Add(packet.sequenceNum);
                return false;
            }
            
            // failCounter++;
            // if (failCounter >= failThreshold)
            // {
            //     Debug.Log($"DeliveryNotificationManager: Artificial packet lost {packet.sequenceNum}");
            //     failCounter = 0;
            //     return false;
            // }
            
            if (!pendingACKs.Contains(packet.sequenceNum))
                pendingACKs.Add(packet.sequenceNum);
            switch (packet.packetType)
            {
                case PacketType.OBJECT_STATE:
                {
                    if (packet.sequenceNum < netManager.stateSequenceNum.expectedNextSequenceNum)
                    {
                        Debug.LogWarning($"DeliveryNotificationManager: Old packet. Seq num {packet.sequenceNum}. Expected next state seq num {netManager.stateSequenceNum.expectedNextSequenceNum}");
                        // Resend the acknowledgment.
                        //return;
                    }

                    if (packet.sequenceNum > netManager.stateSequenceNum.expectedNextSequenceNum)
                    {
                        Debug.LogWarning($"DeliveryNotificationManager: Unordered, lost or duplicated packet. Seq num {packet.sequenceNum}. Expected next state seq num {netManager.stateSequenceNum.expectedNextSequenceNum}");
                        ReOrderPackets();
                    }
                    netManager.stateSequenceNum.incomingSequenceNum = packet.sequenceNum;
                }
                    break;
                case PacketType.INPUT:
                {
                    if (packet.sequenceNum < netManager.inputSequenceNum.expectedNextSequenceNum)
                    {
                        Debug.LogWarning($"DeliveryNotificationManager: Old packet. Seq num {packet.sequenceNum}. Expected next state seq num {netManager.inputSequenceNum.expectedNextSequenceNum}");
                        // Resend the acknowledgment.
                        //return;
                    }

                    if (packet.sequenceNum > netManager.inputSequenceNum.expectedNextSequenceNum)
                    {
                        Debug.LogWarning($"DeliveryNotificationManager: Unordered, lost or duplicated packet. Seq num {packet.sequenceNum}. Expected next state seq num {netManager.inputSequenceNum.expectedNextSequenceNum}");
                        ReOrderPackets();
                    }
                    netManager.inputSequenceNum.incomingSequenceNum = packet.sequenceNum;
                }
                    break;
            }
            return true;
        }

        private void ReOrderPackets()
        {
            
        }
        
        // At the end of the frame, or after some time interval
        // Send a packet with all the acknowledged sequence numbers from all received packets
        private void SendAllACKs()
        {
            if (historicalACKs.Count >= 300)
                historicalACKs.RemoveRange(0, 100);
            
            lastACKsSent.Clear();
            
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(pendingACKs.Count);
            string result = $"ACKs list count: {pendingACKs.Count} -- ";
            foreach (UInt64 sequenceNum in pendingACKs)
            {
                lastACKsSent.Add(sequenceNum);
                writer.Write(sequenceNum);
                result += sequenceNum.ToString() + ", ";
                AcknowledgmentWrapper ack = new AcknowledgmentWrapper(sequenceNum);
                
                if (!historicalACKs.Contains(ack))
                {
                    historicalACKs.Add(ack);
                }
            }
            Debug.Log($"DeliveryNotificationManager: Sending all ACKs --> {result}");
            pendingACKs.Clear();
            ReplicationHeader replicationHeader =
                new ReplicationHeader(tempIndex++, String.Empty, ReplicationAction.ACKNOWLEDGMENT, stream.ToArray().Length);
            NetworkManager.Instance.AddStateStreamQueue(replicationHeader, stream);
        }

        public void ProcessACKs(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            string result = $"ACKs list count: {count} -- ";
            for (int i = 0; i < count; i++)
            {
                UInt64 ack = reader.ReadUInt64();
                if (OnDeliverySuccess(ack))
                    result += ack.ToString() + ", ";
            }
            Debug.Log($"DeliveryNotificationManager: Processed ACKs --> {result}");
        }
        // When do we send notifications from server?
        /* A couple of options
            ● Each time we receive an input data packet
                ○ Faster response
                ○ If the client sends input data packets very frequently, we will use a lot of bandwidth
            ● With the next replication packet
                ○ Slower response
                ○ Optimized bandwidth usage
        */
        
        // Call to process timed-out packets
        // For each delivery that timed out, call onFailure() and remove the delivery
        public void Update(float rtt)
        {
            roundTripTime = rtt;
            if (roundTripTime == -1)
                roundTripTime = 1000;
            
            Int64 currentTimestamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            lock(pendingDeliveries)
            {
                for (int i = 0; i < pendingDeliveriesTime.Count; i++)
                {
                    Int64 storedTimeStamp = pendingDeliveriesTime[i];
                    // Compare the timestamps with the offset
                    if (currentTimestamp - storedTimeStamp > roundTripTime + rttOffset)
                    {
                        OnDeliveryFailure(pendingDeliveries[i]);
                    }
                }
            }

            if (pendingACKs.Count > 10)
            {
                SendAllACKs();
            }

            // for (int i = 0; i < historicalACKs.Count; i++)
            // {
            //     if (currentTimestamp - historicalACKs[i].timeStamp >= historicalRemoveTimeMs)
            //     {
            //         historicalACKs.RemoveAt(i);
            //         i--;
            //     }
            // }
        }

        private bool CheckDuplicate<T>(T packet, List<T> list)
        {
            return list.Contains(packet);
        }
    }
}