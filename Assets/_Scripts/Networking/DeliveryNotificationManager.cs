using System;
using System.Collections.Generic;

namespace _Scripts.Networking
{
    /// <summary>
    /// A layer inside the network manager. It should implement with:
    /// But only for UDP (unreliable packets) and not TCP (reliable packets).
    /// void ProcessIncomingPacket(MemoryStream stream)
    /// and
    /// Packet PreparePacket(UInt64 senderId, PacketType type, ...)
    /// </summary>
    public class DeliveryNotificationManager
    {
        private List<Packet> pendingDeliveries;
        private List<Packet> pendingACKs;
        
        private UInt64 incomingSequenceNum = UInt64.MinValue;
        private UInt64 outgoingSequenceNum = UInt64.MinValue;
        private UInt64 expectedNextSequenceNum => incomingSequenceNum + 1;
        private void CleanPending(UInt64 ACK){}
        
        private float roundTripTime;
        private float timeOutRtt; // timeOutRtt = roundTripTime + someOffset; ex: timeOutRtt = roundTripTime(54ms) + someOffset (15ms)
        
        // Send
        public void MakeDelivery(Packet packet){}
        private void OnDeliverySuccess(UInt64 ACK){}
        private void OnDeliveryFailure(Packet packet){}
        
        // Receive
        public void ReceiveDelivery(Packet packet){}
        private void ReOrderPackets(){}
        
        // At the end of the frame, or after some time interval
        // Send a packet with all the acknowledged sequence numbers from all received packets
        private void SendAllACKs(){}
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
        public void Update(){}
    }
}