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
        private List<Packet> pendingDeliveries = new();
        [SerializeField] private List<UInt64> pendingACKs = new();
        [SerializeField] private List<AcknowledgmentWrapper> historicalACKs = new();
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
        private const int rttOffset = 20;// timeOutRtt = roundTripTime + someOffset; ex: timeOutRtt = roundTripTime(54ms) + someOffset (15ms)

        // 
        private UInt64 idIndex = UInt64.MinValue;
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

        private void OnDeliverySuccess(UInt64 ACK)
        {
            int index = pendingDeliveries.FindIndex(packet => packet.sequenceNum == ACK);
            if (index == -1)
            {
                Debug.LogError($"DeliveryNotificationManager: OnDeliverySuccess error, Seq: {ACK}");
                return;
            }
            Debug.Log($"DeliveryNotificationManager: OnDeliverySuccess, Seq: {ACK}");
            CleanPending(index);
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
            
            // Resend packet through network manager
            if (NetworkManager.Instance.IsHost() && NetworkManager.Instance.getId != packet.senderId)
            {
                NetworkManager.Instance.GetServer().SendUdp(packet.senderId, packet.allData);
            }
            else
            {
                NetworkManager.Instance.GetClient().SendUdpPacket(packet.allData);
            }
        }
        
        // Receive
        public bool ReceiveDelivery(Packet packet)
        {
            if (CheckDuplicate<UInt64>(packet.sequenceNum, pendingACKs) || historicalACKs.FindIndex(wrapper => wrapper.ACKSeqNum == packet.sequenceNum) != -1)
            {
                Debug.LogError($"DeliveryNotificationManager: Cannot receive delivery as it is a duplicate packet. Seq num {packet.sequenceNum}");
                // Acknowledgment is pending. No action.
                return false;
            }
            
            // failCounter++;
            // if (failCounter >= failThreshold)
            // {
            //     Debug.Log($"DeliveryNotificationManager: Artificial packet lost {packet.sequenceNum}");
            //     failCounter = 0;
            //     return false;
            // }
            
            pendingACKs.Add(packet.sequenceNum);
            NetworkManager netManager = NetworkManager.Instance;
            switch (packet.packetType)
            {
                case PacketType.OBJECT_STATE:
                {
                    if (packet.sequenceNum < netManager.stateSequenceNum.expectedNextSequenceNum && packet.sequenceNum != 0)
                    {
                        Debug.LogWarning($"DeliveryNotificationManager: Old packet. Seq num {packet.sequenceNum}");
                        // Resend the acknowledgment.
                        //return;
                    }

                    if (packet.sequenceNum > netManager.stateSequenceNum.expectedNextSequenceNum)
                    {
                        Debug.LogError($"DeliveryNotificationManager: Unordered, lost or duplicated packet. Seq num {packet.sequenceNum}");
                        ReOrderPackets();
                    }
                    netManager.stateSequenceNum.incomingSequenceNum = packet.sequenceNum;
                }
                    break;
                case PacketType.INPUT:
                {
                    if (packet.sequenceNum < netManager.inputSequenceNum.expectedNextSequenceNum && packet.sequenceNum != 0)
                    {
                        Debug.LogWarning($"DeliveryNotificationManager: Old packet. Seq num {packet.sequenceNum}");
                        // Resend the acknowledgment.
                        //return;
                    }

                    if (packet.sequenceNum > netManager.inputSequenceNum.expectedNextSequenceNum)
                    {
                        Debug.LogError($"DeliveryNotificationManager: Unordered, lost or duplicated packet. Seq num {packet.sequenceNum}");
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
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(pendingACKs.Count);
            string result = $"ACKs list count: {pendingACKs.Count} -- ";
            foreach (UInt64 sequenceNum in pendingACKs)
            {
                writer.Write(sequenceNum);
                result += sequenceNum.ToString() + ", ";
                AcknowledgmentWrapper ack = new AcknowledgmentWrapper(sequenceNum);
                if (!historicalACKs.Contains(ack))
                    historicalACKs.Add(ack);
            }
            Debug.Log($"DeliveryNotificationManager: Sending all ACKs --> {result}");
            pendingACKs.Clear();
            ReplicationHeader replicationHeader =
                new ReplicationHeader(UInt64.MaxValue, this.GetType().FullName, ReplicationAction.ACKNOWLEDGMENT, stream.ToArray().Length);
            NetworkManager.Instance.AddStateStreamQueue(replicationHeader, stream);
        }

        public void ProcessACKs(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            string result = $"ACKs list count: {count} -- ";
            for (int i = 0; i < count; i++)
            {
                UInt64 ack = reader.ReadUInt64();
                result += ack.ToString() + ", ";
                OnDeliverySuccess(ack);
            }
            Debug.Log($"DeliveryNotificationManager: Processing ACKs --> {result}");
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

            if (pendingACKs.Count > 3)
            {
                SendAllACKs();
            }

            for (int i = 0; i < historicalACKs.Count; i++)
            {
                if (currentTimestamp - historicalACKs[i].timeStamp >= historicalRemoveTimeMs)
                {
                    historicalACKs.RemoveAt(i);
                    i--;
                }
            }
        }

        private bool CheckDuplicate<T>(T packet, List<T> list)
        {
            return list.Contains(packet);
        }
    }
}