using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public class SerializationTest : NetworkBehaviour
{
    [SerializeField] public bool b = true;
    [SerializeField] public uint ui = 2;
    [SerializeField] public int i = -2;
    [SerializeField] float f = 3.3f;
    [SerializeField] double d = 4.4444;
    [SerializeField] string s = "hello_world";
    [SerializeField] ulong ul = 100000000000;

    [SerializeField] public NetworkVariable<int> myNetVariableInt = new NetworkVariable<int>(10101, 0);
    [SerializeField] public NetworkVariable<string> myNetVariableStr = new NetworkVariable<string>("My Name is Ali", 1);

    protected override MemoryStream Write(MemoryStream outputMemoryStream)
    {
        // [Object State][Object Class][Object ID][Fields total Size][Changed Fields][DATA I][Data J]... <- End of an object packet
        //[Object Class][Object ID][Fields total Size][Changed Fields][DATA I][Data J]...[Object Class][Object ID][Fields total Size][Changed Fields][DATA I][Data J]...

        MemoryStream tempStream = new MemoryStream();
        BinaryWriter tempWriter = new BinaryWriter(tempStream);

        BinaryWriter writer = new BinaryWriter(outputMemoryStream);
        Type objectType = this.GetType();
        writer.Write(objectType.AssemblyQualifiedName);
        writer.Write(networkObject.GetNetworkId());

        // Serialize the changed fields using the bitfield
        BitArray bitfield = bitTracker.GetBitfield();

        if (bitTracker.GetBitfield().Get(0))
            tempWriter.Write(b);
        if (bitTracker.GetBitfield().Get(1))
            tempWriter.Write(ui);
        if (bitTracker.GetBitfield().Get(2))
            tempWriter.Write(i);
        if (bitTracker.GetBitfield().Get(3))
            tempWriter.Write(f);
        if (bitTracker.GetBitfield().Get(4))
            tempWriter.Write(d);
        if (bitTracker.GetBitfield().Get(5))
            tempWriter.Write(s);
        if (bitTracker.GetBitfield().Get(6))
            tempWriter.Write(ul);

        int fieldCount = bitfield.Length;
        writer.Write(fieldCount);

        // Write the bitfield
        byte[] bitfieldBytes = new byte[(fieldCount + 7) / 8];
        bitfield.CopyTo(bitfieldBytes, 0);
        writer.Write(bitfieldBytes);

        tempStream.CopyTo(outputMemoryStream);

        // [Object State][Object Class][Object ID][Fields total Size][Changed Fields][DATA I][Data J]... <- End of an object packet
        //[Object Class][Object ID][Fields total Size][Changed Fields][DATA I][Data J]...[Object Class][Object ID][Fields total Size][Changed Fields][DATA I][Data J]...
        return outputMemoryStream;
    }

    public override void Read(BinaryReader reader)
    {
        //[Changed Fields][DATA I][Data J]... <- End of an object packet

        int fieldCount = bitTracker.GetBitfield().Length;
        int receivedFieldCount = reader.ReadInt32();
        if (receivedFieldCount != fieldCount)
        {
            UnityEngine.Debug.LogError("Mismatch in the count of fields");
            return;
        }

        // Read the bitfield from the input stream
        byte[] receivedBitfieldBytes = reader.ReadBytes((fieldCount + 7) / 8);
        BitArray receivedBitfield = new BitArray(receivedBitfieldBytes);

        if (receivedBitfield.Get(0))
            b = reader.ReadBoolean();
        if (receivedBitfield.Get(1))
            ui = reader.ReadUInt32();
        if (receivedBitfield.Get(2))
            i = reader.ReadInt32();
        if (receivedBitfield.Get(3))
            f = reader.ReadSingle();
        if (receivedBitfield.Get(4))
            d = reader.ReadDouble();
        if (receivedBitfield.Get(5))
            s = reader.ReadString();
        if (receivedBitfield.Get(6))
            ul = reader.ReadUInt64();

        // [Object State][Object Class][Object ID][Fields total Size][Changed Fields][DATA I][Data J]... <- End of an object packet
        //[Object Class][Object ID][Fields total Size][Changed Fields][DATA I][Data J]...[Object Class][Object ID][Fields total Size][Changed Fields][DATA I][Data J]...
    }

    public override void Awake()
    {
        base.Awake();
        bitTracker = new ChangeTracker(7);
    }
    private void Start()
    {
        bitTracker.SetAll(true);

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        MemoryStream testStream = new MemoryStream();
        Write(testStream);

        stopwatch.Stop();
        long wTime = stopwatch.ElapsedMilliseconds;
        UnityEngine.Debug.Log($"Write Elapsed Time: {wTime} milliseconds");

        int timer = 0;

        while(timer < 100000)
        {
            timer++;
        }

        b = false;
        ui = 1;
        i = -1;
        f = 99.3f;
        d = 200.4444;
        s = "goodbye";
        ul = 300040004000;

        stopwatch.Restart();
        //Read(testStream);
        stopwatch.Stop();
        long rTime = stopwatch.ElapsedMilliseconds;
        UnityEngine.Debug.Log($"Read Elapsed Time: {rTime} milliseconds");
    }

    private void Update()
    {
        UnityEngine.Debug.Log($"myNetVariableInt is: {myNetVariableInt.Value}");
        UnityEngine.Debug.Log($"myNetVariableStr is: {myNetVariableStr.Value}");
    }
}
