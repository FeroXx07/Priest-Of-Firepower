using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;

public class Authenticator 
{
    protected enum AuthenticationState
    {
        REQUESTED,
        CONFIRMATION
    }
    protected void SerializeIPEndPoint(IPEndPoint endpoint, BinaryWriter writer)
    {
        // Serialize IP Address
        byte[] ipAddressBytes = endpoint.Address.GetAddressBytes();
        writer.Write(ipAddressBytes.Length);
        writer.Write(ipAddressBytes);

        // Serialize Port
        writer.Write(endpoint.Port);
    }

    protected IPEndPoint DeserializeIPEndPoint(BinaryReader reader)
    {
        try
        {
            // Deserialize IP Address
            int ipAddressLength = reader.ReadInt32();
            byte[] ipAddressBytes = reader.ReadBytes(ipAddressLength);
            IPAddress ipAddress = new IPAddress(ipAddressBytes);

            // Deserialize Port
            int port = reader.ReadInt32();

            // Create IPEndPoint
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, port);

            return ipEndPoint;
        }
        catch (Exception ex)
        {
            Debug.LogError("Error while deserializing IPEndPoint: " + ex.Message);
            return null;
        }
    }

}
