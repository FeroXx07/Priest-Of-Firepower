using System;
using System.Collections.Generic;
using System.IO;
using _Scripts.Networking.Client;
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
    public class RecvAck
    {
        public RecvAck(UInt64 ackSeqNum)
        {
            timeStamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            ACKSeqNum = ackSeqNum;
        }

        public Int64 timeStamp;
        public UInt64 ACKSeqNum;
    }

    public class SendAck
    {
        public SendAck(Packet packet)
        {
            this.packet = packet;
            timeStamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            count = 0;
        }

        public Packet packet;
        public Int64 timeStamp;
        public int count;
    }

    public class ACKContainer
    {
        private const int MAX_CAPACITY_HISTORICAL_ACK = 1000;

        // ACK that the destination has to respond the sender (origin)
        public List<UInt64> pendingACKs = new();

        // ACK that the sender (origin) has sent and check re-sends
        public List<SendAck> pendingDeliveries = new();

        // ACK that the destination has received historically
        public List<RecvAck> historicalRecvACKs = new(MAX_CAPACITY_HISTORICAL_ACK);

        // ACK that the origin has sent historically
        public List<SendAck> historicalSendAcks = new(MAX_CAPACITY_HISTORICAL_ACK);
        public List<UInt64> lastACKsSent = new();
    }

    [Serializable]
    public class DeliveryNotificationManager
    {
        private ClientData _client;
        private ACKContainer stateContainer = new ACKContainer();
        private ACKContainer inputContainer = new ACKContainer();
        private int failThreshold = 7;
        private int failCounter = 0;
        private float roundTripTime;

        private const int
            rttOffset = 0; // timeOutRtt = roundTripTime + someOffset; ex: timeOutRtt = roundTripTime(54ms) + someOffset (15ms)

        private UInt64 tempIndex = UInt64.MinValue;

        private void CleanPending(int index, PacketType type)
        {
            if (type == PacketType.OBJECT_STATE)
            {
                stateContainer.pendingDeliveries.RemoveAt(index);
            }
            else
            {
                inputContainer.pendingDeliveries.RemoveAt(index);
            }
        }

        // Send
        public void MakeDelivery(Packet packet)
        {
            SendAck sendAck = new SendAck(packet);
            NetworkManager netMan = NetworkManager.Instance;
            if (packet.packetType == PacketType.OBJECT_STATE)
            {
                int index = stateContainer.pendingDeliveries.FindIndex(sendAck =>
                    sendAck.packet.sequenceNum == packet.sequenceNum);
                if (index != -1)
                {
                    Debug.LogError(netMan.IsHost()
                        ? $"DeliveryNotificationManager: Client: {_client.userName}_{_client.id} - Cannot make delivery as it is a duplicate packet. State Seq num {packet.sequenceNum}"
                        : $"DeliveryNotificationManager: Cannot make delivery as it is a duplicate packet. State Seq num {packet.sequenceNum}");
                    return;
                }

                stateContainer.pendingDeliveries.Add(sendAck);
                if (stateContainer.historicalSendAcks.Count >= 300)
                    stateContainer.historicalSendAcks.RemoveRange(0, 100);
                stateContainer.historicalSendAcks.Add(sendAck);
            }
            else
            {
                int index = inputContainer.pendingDeliveries.FindIndex(sendAck =>
                    sendAck.packet.sequenceNum == packet.sequenceNum);
                if (index != -1)
                {
                    Debug.LogError(netMan.IsHost()
                        ? $"DeliveryNotificationManager: Client: {_client.userName}_{_client.id} - Cannot make delivery as it is a duplicate packet. Input Seq num {packet.sequenceNum}"
                        : $"DeliveryNotificationManager: Cannot make delivery as it is a duplicate packet. Input Seq num {packet.sequenceNum}");
                    return;
                }

                inputContainer.pendingDeliveries.Add(sendAck);
                if (inputContainer.historicalSendAcks.Count >= 300)
                    inputContainer.historicalSendAcks.RemoveRange(0, 100);
                inputContainer.historicalSendAcks.Add(sendAck);
            }
        }

        private bool OnDeliverySuccess(UInt64 ACK, PacketType type)
        {
            if (type == PacketType.OBJECT_STATE)
            {
                NetworkManager netMan = NetworkManager.Instance;
                int index = stateContainer.pendingDeliveries.FindIndex(sendAck => sendAck.packet.sequenceNum == ACK);
                if (index == -1)
                {
                    Debug.LogWarning(netMan.IsHost()
                        ? $"DeliveryNotificationManager: Client: {_client.userName}_{_client.id} - OnDeliverySuccess already processed ACK, State Seq: {ACK}"
                        : $"DeliveryNotificationManager: OnDeliverySuccess already processed ACK, State Seq: {ACK}");
                    return false;
                }

                // Debug.Log(netMan.IsHost()
                //     ? $"DeliveryNotificationManager: Client: {_client.userName}_{_client.id} - OnDeliverySuccess, State Seq: {ACK}"
                //     : $"DeliveryNotificationManager: OnDeliverySuccess, State Seq: {ACK}");
                CleanPending(index, PacketType.OBJECT_STATE);
            }
            else
            {
                NetworkManager netMan = NetworkManager.Instance;
                int index = inputContainer.pendingDeliveries.FindIndex(sendAck => sendAck.packet.sequenceNum == ACK);
                if (index == -1)
                {
                    Debug.LogWarning(netMan.IsHost()
                        ? $"DeliveryNotificationManager: Client: {_client.userName}_{_client.id} - OnDeliverySuccess already processed ACK, Input Seq: {ACK}"
                        : $"DeliveryNotificationManager: OnDeliverySuccess already processed ACK, Input Seq: {ACK}");
                    return false;
                }

                // Debug.Log(netMan.IsHost()
                //     ? $"DeliveryNotificationManager: Client: {_client.userName}_{_client.id} - OnDeliverySuccess, Input Seq: {ACK}"
                //     : $"DeliveryNotificationManager: OnDeliverySuccess, Input Seq: {ACK}");
                CleanPending(index, PacketType.INPUT);
            }

            return true;
        }

        private void OnDeliveryFailure(SendAck sendAck, float timeDiff, float timeToCompare)
        {
            NetworkManager netMan = NetworkManager.Instance;
            if (sendAck.packet.packetType == PacketType.OBJECT_STATE)
            {
                Debug.LogError(netMan.IsHost()
                    ? $"DeliveryNotificationManager: Client: {_client.userName}_{_client.id} - OnDeliveryFailure resend state seq: {sendAck.packet.sequenceNum}, timeDiff:{timeDiff}, timeToCompare: {timeToCompare}"
                    : $"DeliveryNotificationManager: OnDeliveryFailure resend state seq: {sendAck.packet.sequenceNum}, timeDiff:{timeDiff}, timeToCompare: {timeToCompare}");
            }
            else
            {
                Debug.LogError(netMan.IsHost()
                    ? $"DeliveryNotificationManager: Client: {_client.userName}_{_client.id} - OnDeliveryFailure resend input seq: {sendAck.packet.sequenceNum}, timeDiff:{timeDiff}, timeToCompare: {timeToCompare}"
                    : $"DeliveryNotificationManager: OnDeliveryFailure resend input seq: {sendAck.packet.sequenceNum}, timeDiff:{timeDiff}, timeToCompare: {timeToCompare}");
            }

            // Set the time to current time.
            sendAck.timeStamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            sendAck.count = 0;
            if (netMan.IsHost())
                netMan.AddResendPacket(new ResendPacket(sendAck.packet, _client.id));
            else
                netMan.AddResendPacket(new ResendPacket(sendAck.packet));
        }

        // Receive
        public bool ReceiveDelivery(Packet packet)
        {
            NetworkManager netMan = NetworkManager.Instance;
            if (packet.packetType == PacketType.OBJECT_STATE)
            {
                if (stateContainer.historicalRecvACKs.FindIndex(wrapper => wrapper.ACKSeqNum == packet.sequenceNum) !=
                    -1)
                {
                    // Acknowledgment is pending. No action.
                    if (!stateContainer.lastACKsSent.Contains(packet.sequenceNum) &&
                        !stateContainer.pendingACKs.Contains(packet.sequenceNum))
                        stateContainer.pendingACKs.Add(packet.sequenceNum);
                    Debug.LogWarning(netMan.IsHost()
                        ? $"DeliveryNotificationManager: Client: {_client.userName}_{_client.id} - Cannot receive delivery as it is a duplicate packet. " +
                          $"Seq num {packet.sequenceNum}. Expected next state seq num {netMan.stateSequenceNum.expectedNextSequenceNum}"
                        : $"DeliveryNotificationManager: Cannot receive delivery as it is a duplicate packet. " +
                          $"Seq num {packet.sequenceNum}. Expected next state seq num {netMan.stateSequenceNum.expectedNextSequenceNum}");
                    return false;
                }

                failCounter++;
                if (failCounter >= failThreshold)
                {
                    Debug.Log($"DeliveryNotificationManager: Artificial state packet lost {packet.sequenceNum}");
                    failCounter = 0;
                    return false;
                }
            }
            else
            {
                if (inputContainer.historicalRecvACKs.FindIndex(wrapper => wrapper.ACKSeqNum == packet.sequenceNum) !=
                    -1)
                {
                    // Acknowledgment is pending. No action.
                    if (!inputContainer.lastACKsSent.Contains(packet.sequenceNum) &&
                        !inputContainer.pendingACKs.Contains(packet.sequenceNum))
                        inputContainer.pendingACKs.Add(packet.sequenceNum);
                    Debug.LogWarning(netMan.IsHost()
                        ? $"DeliveryNotificationManager: Client: {_client.userName}_{_client.id} - Cannot receive delivery as it is a duplicate packet. " +
                          $"Seq num {packet.sequenceNum}. Expected next input seq num {netMan.inputSequenceNum.expectedNextSequenceNum}"
                        : $"DeliveryNotificationManager: Cannot receive delivery as it is a duplicate packet." +
                          $"Seq num {packet.sequenceNum}. Expected next input seq num {netMan.inputSequenceNum.expectedNextSequenceNum}");
                    return false;
                }

                failCounter++;
                if (failCounter >= failThreshold)
                {
                    Debug.Log($"DeliveryNotificationManager: Artificial input packet lost {packet.sequenceNum}");
                    failCounter = 0;
                    return false;
                }
            }

            if (packet.packetType == PacketType.OBJECT_STATE)
            {
                if (!stateContainer.pendingACKs.Contains(packet.sequenceNum))
                {
                    RecvAck ack = new RecvAck(packet.sequenceNum);
                    stateContainer.historicalRecvACKs.Add(ack);
                    stateContainer.pendingACKs.Add(packet.sequenceNum);
                }
            }
            else
            {
                if (!inputContainer.pendingACKs.Contains(packet.sequenceNum))
                {
                    RecvAck ack = new RecvAck(packet.sequenceNum);
                    inputContainer.historicalRecvACKs.Add(ack);
                    inputContainer.pendingACKs.Add(packet.sequenceNum);
                }
            }

            switch (packet.packetType)
            {
                case PacketType.OBJECT_STATE:
                {
                    if (packet.sequenceNum < netMan.stateSequenceNum.expectedNextSequenceNum)
                    {
                        Debug.LogError(netMan.IsHost()
                            ? $"DeliveryNotificationManager: Client: {_client.userName}_{_client.id} - Old packet. Seq num {packet.sequenceNum}. Expected next state seq num {netMan.stateSequenceNum.expectedNextSequenceNum}"
                            : $"DeliveryNotificationManager: Old packet. Seq num {packet.sequenceNum}. Expected next state seq num {netMan.stateSequenceNum.expectedNextSequenceNum}");
                        // Resend the acknowledgment.
                    }
                    else if (packet.sequenceNum > netMan.stateSequenceNum.expectedNextSequenceNum)
                    {
                        Debug.LogError(netMan.IsHost()
                            ? $"DeliveryNotificationManager: Client: {_client.userName}_{_client.id} - Unordered, lost or duplicated packet. Seq num {packet.sequenceNum}. Expected next state seq num {netMan.stateSequenceNum.expectedNextSequenceNum}"
                            : $"DeliveryNotificationManager: Unordered, lost or duplicated packet. Seq num {packet.sequenceNum}. Expected next state seq num {netMan.stateSequenceNum.expectedNextSequenceNum}");
                        ReOrderPackets();
                    }

                    netMan.stateSequenceNum.incomingSequenceNum = packet.sequenceNum;
                }
                    break;
                case PacketType.INPUT:
                {
                    if (packet.sequenceNum < netMan.inputSequenceNum.expectedNextSequenceNum)
                    {
                        Debug.LogError(netMan.IsHost()
                            ? $"DeliveryNotificationManager: Client: {_client.userName}_{_client.id} - Old packet. Seq num {packet.sequenceNum}. Expected next state seq num {netMan.inputSequenceNum.expectedNextSequenceNum}"
                            : $"DeliveryNotificationManager: Old packet. Seq num {packet.sequenceNum}. Expected next state seq num {netMan.inputSequenceNum.expectedNextSequenceNum}");
                        // Resend the acknowledgment.
                    }
                    else if (packet.sequenceNum > netMan.inputSequenceNum.expectedNextSequenceNum)
                    {
                        Debug.LogError(netMan.IsHost()
                            ? $"DeliveryNotificationManager: Client: {_client.userName}_{_client.id} - Unordered, lost or duplicated packet. Seq num {packet.sequenceNum}. Expected next state seq num {netMan.inputSequenceNum.expectedNextSequenceNum}"
                            : $"DeliveryNotificationManager: Unordered, lost or duplicated packet. Seq num {packet.sequenceNum}. Expected next state seq num {netMan.inputSequenceNum.expectedNextSequenceNum}");
                        ReOrderPackets();
                    }

                    netMan.inputSequenceNum.incomingSequenceNum = packet.sequenceNum;
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
            if (stateContainer.pendingACKs.Count >= 3) SendACKs(stateContainer, PacketType.OBJECT_STATE);
            if (inputContainer.pendingACKs.Count >= 3) SendACKs(inputContainer, PacketType.INPUT);
        }

        private void SendACKs(ACKContainer ackContainer, PacketType packetType)
        {
            if (ackContainer.historicalRecvACKs.Count >= 300) ackContainer.historicalRecvACKs.RemoveRange(0, 100);
            ackContainer.lastACKsSent.Clear();
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write((int)packetType);
            writer.Write(ackContainer.pendingACKs.Count);
            string result = $"ACKs list count: {ackContainer.pendingACKs.Count} -- ";
            foreach (UInt64 sequenceNum in ackContainer.pendingACKs)
            {
                ackContainer.lastACKsSent.Add(sequenceNum);
                writer.Write(sequenceNum);
                result += sequenceNum.ToString() + ", ";
            }

            NetworkManager netMan = NetworkManager.Instance;
            Debug.Log(netMan.IsHost()
                ? $"DeliveryNotificationManager: Client: {_client.userName}_{_client.id} - Sending all ACKs --> {result}"
                : $"DeliveryNotificationManager: Sending all ACKs --> {result}");
            ackContainer.pendingACKs.Clear();
            // ReplicationHeader replicationHeader =
            //     new ReplicationHeader(tempIndex++, "DNM", ReplicationAction.ACKNOWLEDGMENT, stream.ToArray().Length);
            // NetworkManager.Instance.AddStateStreamQueue(replicationHeader, stream);
            InputHeader inputHeader = new InputHeader(tempIndex++, "DNM", stream.ToArray().Length,
                NetworkManager.Instance.getId, DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
            NetworkManager.Instance.AddInputStreamQueue(inputHeader, stream);
        }

        public void ProcessACKs(BinaryReader reader)
        {
            PacketType type = (PacketType)reader.ReadInt32();
            int count = reader.ReadInt32();
            string result = $"ACKs {type} list count: {count} -- ";
            for (int i = 0; i < count; i++)
            {
                UInt64 ack = reader.ReadUInt64();
                if (OnDeliverySuccess(ack, type)) result += ack.ToString() + ", ";
            }

            NetworkManager netMan = NetworkManager.Instance;
            Debug.Log(netMan.IsHost()
                ? $"DeliveryNotificationManager: Client: {_client.userName}_{_client.id} - Processed ACKs {type} --> {result}"
                : $"DeliveryNotificationManager: Processed ACKs {type} --> {result}");
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
        public void CheckDeliveryFailures()
        {
            Int64 currentTimestamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            //Debug.Log("CheckDeliveryFailures() State");
            foreach (var sendAck in stateContainer.pendingDeliveries)
            {
                //Debug.Log($"SendAck: state seq:{sendAck.packet.sequenceNum}, count:{sendAck.count}");
                Int64 storedTimeStamp = sendAck.timeStamp;
                float timeDiff = currentTimestamp - storedTimeStamp;
                float totalToCompare = roundTripTime + rttOffset;
                if (sendAck.count >= 2)
                {
                    OnDeliveryFailure(sendAck, timeDiff, totalToCompare);
                }

                sendAck.count++;
            }

            //Debug.Log("CheckDeliveryFailures() Input");
            foreach (var sendAck in inputContainer.pendingDeliveries)
            {
                //Debug.Log($"SendAck: input seq:{sendAck.packet.sequenceNum}, count:{sendAck.count}");
                Int64 storedTimeStamp = sendAck.timeStamp;
                float timeDiff = currentTimestamp - storedTimeStamp;
                float totalToCompare = roundTripTime + rttOffset;
                if (sendAck.count >= 2)
                {
                    OnDeliveryFailure(sendAck, timeDiff, totalToCompare);
                }

                sendAck.count++;
            }
        }

        public void Update(float rtt, float timeBetweenStatePackets)
        {
            if (Math.Abs(timeBetweenStatePackets - (-1)) < 1) timeBetweenStatePackets = 1000;
            roundTripTime = rtt + timeBetweenStatePackets;
            SendAllACKs();
        }

        public void SetClient(ClientData clientData)
        {
            _client = clientData;
        }

        private bool CheckDuplicate<T>(T packet, List<T> list)
        {
            return list.Contains(packet);
        }
    }
}