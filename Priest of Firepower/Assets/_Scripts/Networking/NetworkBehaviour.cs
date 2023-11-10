using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public abstract class NetworkBehaviour : MonoBehaviour
{
    public float tickRate = 10.0f; // Network writes inside a second.
    private float tickCounter = 0.0f;
    protected abstract MemoryStream Write(MemoryStream outputStream);
    protected abstract void Read(MemoryStream inputMemoryStream);
    public void SendData()
    {
        MemoryStream stream = new MemoryStream();
        // MemoryStream Write(MemoryStream outputStream);

        // Send MemoryStream to netowrk manager buffer
    }
    private void Update()
    {
        // Send Write to state buffer
        // SendData();
        tickCounter += Time.deltaTime;
    }
}
