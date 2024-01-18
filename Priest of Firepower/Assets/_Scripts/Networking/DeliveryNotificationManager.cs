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
    public struct SequenceNum
    {
        public UInt64 incomingSequenceNum;
        public UInt64 outgoingSequenceNum;
        public UInt64 expectedNextSequenceNum => incomingSequenceNum + 1;
    }
    public class DeliveryNotificationManager
    {
        private List<Packet> pendingDeliveries;
        private List<Packet> pendingACKs;
        private List<Int64> pendingDeliveriesTime;
        
        public SequenceNum inputSequenceNum;
        public SequenceNum stateSequenceNum;

        private int failThreshold = 3;
        private int failCounter = 0;
        private void CleanPending(int index)
        {
            pendingDeliveries.RemoveAt(index);
            pendingDeliveriesTime.RemoveAt(index);
        }
        
        private int roundTripTime;
        private const int rttOffset = 60;// timeOutRtt = roundTripTime + someOffset; ex: timeOutRtt = roundTripTime(54ms) + someOffset (15ms)
        
        // Send
        public void MakeDelivery(Packet packet)
        {
            if (CheckDuplicate(packet, pendingDeliveries))
            {
                Debug.LogError("DeliveryNotificationManager: Cannot make delivery as it is a duplicate packet");
                return;
            }

            switch (packet.packetType)
            {
                case PacketType.OBJECT_STATE:
                    if (stateSequenceNum.outgoingSequenceNum == ulong.MaxValue - 1) stateSequenceNum.outgoingSequenceNum = 0;
                    stateSequenceNum.outgoingSequenceNum += 1;
                    break;
                case PacketType.INPUT:
                    if (inputSequenceNum.outgoingSequenceNum == ulong.MaxValue - 1) inputSequenceNum.outgoingSequenceNum = 0;
                    inputSequenceNum.outgoingSequenceNum += 1;
                    break;
            }
            
            pendingDeliveries.Add(packet);
            pendingDeliveriesTime.Add(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
        }

        private void OnDeliverySuccess(UInt64 ACK)
        {
            int index = pendingDeliveries.FindIndex(packet => packet.sequenceNum == ACK);
            if (index == -1)
            {
                Debug.LogError($"DeliveryNotificationManager: OnDeliverySuccess error, Seq: {ACK}");
                return;
            }
            
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
            
            // Set the time to current time.
            pendingDeliveriesTime[index] = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            
            // Resend packet through network manager
            if (NetworkManager.Instance.IsHost())
            {
                NetworkManager.Instance.GetServer().SendUdpToAll(packet.allData);
            }
            else
            {
                NetworkManager.Instance.GetClient().SendUdpPacket(packet.allData);
            }
        }
        
        
        // Receive
        public void ReceiveDelivery(Packet packet, Action processAction)
        {
            if (CheckDuplicate(packet, pendingACKs))
            {
                Debug.LogError("DeliveryNotificationManager: Cannot receive delivery as it is a duplicate packet");
                
                // Maybe resend the acknowledgment.
                return;
            }
            
            switch (packet.packetType)
            {
                case PacketType.OBJECT_STATE:
                {
                    if (packet.sequenceNum < stateSequenceNum.expectedNextSequenceNum)
                    {
                        Debug.LogError("DeliveryNotificationManager: Old packet");
                        // Maybe resend the acknowledgment.
                    }
                    else if (packet.sequenceNum > stateSequenceNum.expectedNextSequenceNum)
                    {
                        Debug.LogError("DeliveryNotificationManager: Unordered packet");
                        ReOrderPackets();
                    }
                }
                    break;
                case PacketType.INPUT:
                {
                    if (packet.sequenceNum < inputSequenceNum.expectedNextSequenceNum)
                    {
                        Debug.LogError("DeliveryNotificationManager: Old packet");
                        // Maybe resend the acknowledgment.
                    }
                    else if (packet.sequenceNum > inputSequenceNum.expectedNextSequenceNum)
                    {
                        Debug.LogError("DeliveryNotificationManager: Unordered packet");
                        ReOrderPackets();
                    }
                }
                    break;
            }

            failCounter++;
            if (failCounter >= failThreshold)
            {
                failCounter = 0;
            }
            else
            {
                pendingACKs.Add(packet);
                processAction?.Invoke();
            }
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
            foreach (Packet packet in pendingACKs)
                writer.Write(packet.sequenceNum);
            pendingACKs.Clear(); // Maybe don't clear it so fast, ACK may get lost and we will need this.
            ReplicationHeader replicationHeader =
                new ReplicationHeader(UInt64.MaxValue, this.GetType().FullName, ReplicationAction.ACKNOWLEDGMENT, stream.ToArray().Length);
            NetworkManager.Instance.AddStateStreamQueue(replicationHeader, stream);
        }

        public void ProcessACKs(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                UInt64 ack = reader.ReadUInt64();
                OnDeliverySuccess(ack);
            }
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
        public void Update()
        {
            Int64 currentTimestamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            for (int i = 0; i < pendingDeliveriesTime.Count; i++)
            {
                Int64 storedTimeStamp = pendingDeliveriesTime[i];
                // Compare the timestamps with the offset
                if (storedTimeStamp - currentTimestamp > roundTripTime + rttOffset)
                {
                    OnDeliveryFailure(pendingDeliveries[i]);
                }
            }

            if (pendingACKs.Count > 3)
            {
                SendAllACKs();
            }
        }

        private bool CheckDuplicate(Packet packet, List<Packet> list)
        {
            return list.Contains(packet);
        }
    }
}