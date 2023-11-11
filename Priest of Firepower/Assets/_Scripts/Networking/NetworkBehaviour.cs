using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public abstract class NetworkBehaviour : MonoBehaviour
{
    public float tickRate = 10.0f; // Network writes inside a second.
    private float tickCounter = 0.0f;
    protected ChangeTracker bitTracker;
    protected NetworkObject networkObject;

    #region Serialization
    protected abstract MemoryStream Write(MemoryStream outputMemoryStream);
    //{
    //        // Serialize the type of the object and the global object id
    //        BinaryWriter writer = new BinaryWriter(outputMemoryStream);
    //        Type objectType = this.GetType();
    //        writer.Write(objectType.AssemblyQualifiedName);
    //        writer.Write(networkObject.GetNetworkId());

    //        // Serialize the changed fields using the bitfield
    //        BitArray bitfield = bitTracker.GetBitfield();
    //        int fieldCount = bitfield.Length;

    //        // Write the count of fields
    //        writer.Write(fieldCount);

    //        // Write the bitfield
    //        byte[] bitfieldBytes = new byte[(fieldCount + 7) / 8];
    //        bitfield.CopyTo(bitfieldBytes, 0);
    //        writer.Write(bitfieldBytes);

    //        //foreach (var field in this.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
    //        //{
    //        //    Debug.Log(field.Name);
    //        //}
    //        List<FieldInfo> fields = this.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).ToList();

    //        // Serialize the changed fields using the bitfield
    //        for (int i = 0; i<bitTracker.GetBitfield().Length; i++)
    //        {
    //            if (bitTracker.HasChanged(i))
    //            {
    //                FieldInfo field = fields[i];

    //                if (field != null && IsPrimitiveType(field.FieldType))
    //                {
    //                    object value = field.GetValue(this);
    //                    if (value != null)
    //                    {
    //                        WriteValue(value, writer);
    //    }
    //                }
    //                else
    //    {
    //        fields.Remove(field);
    //        i--;
    //        continue;
    //    }
    //            }
    //        }
    //}
    //bitfield.SetAll(false);
    //return outputMemoryStream;

    protected abstract void Read(MemoryStream inputMemoryStream);
    //{
    //    inputMemoryStream.Position = 0;  // Reset the stream position for reading

    //    BinaryReader reader = new BinaryReader(inputMemoryStream);
    //    string typeName = reader.ReadString();
    //    Type objectType = Type.GetType(typeName);
    //    UInt64 objectId = reader.ReadUInt64();

    //    int fieldCount = bitTracker.GetBitfield().Length;
    //    int receivedFieldCount = reader.ReadInt32();
    //    if (receivedFieldCount != fieldCount)
    //    {
    //        Debug.LogError("Mismatch in the count of fields");
    //    }

    //    if (objectType == this.GetType() && objectId == networkObject.GetNetworkId())
    //    {
    //        // Read the bitfield from the input stream
    //        byte[] receivedBitfieldBytes = reader.ReadBytes((fieldCount + 7) / 8);
    //        BitArray receivedBitfield = new BitArray(receivedBitfieldBytes);

    //        List<FieldInfo> fields = this.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).ToList();

    //        for (int i = 0; i < receivedFieldCount; i++)
    //        {
    //            if (receivedBitfield[i])
    //            {
    //                FieldInfo field = fields[i];

    //                if (field != null && IsPrimitiveType(field.FieldType))
    //                {
    //                    // Deserialize the field value from the input stream
    //                    object deserializedValue = ReadValue(field.FieldType, reader);

    //                    // Set the deserialized value to the corresponding field in this object
    //                    field.SetValue(this, deserializedValue);
    //                }
    //                else
    //                {
    //                    fields.Remove(field);
    //                    i--;
    //                    continue;
    //                }
    //            }
    //        }
    //    }
    //    else
    //    {
    //        Debug.LogWarning("Received a an incorrect MemoryStream!");
    //    }
    //}
    #endregion

    //#region Serialization support
    //private static bool IsPrimitiveType(Type type)
    //{
    //    return type.IsPrimitive || type == typeof(string) || type == typeof(float);
    //}

    //private void WriteValue(object value, BinaryWriter writer)
    //{
    //    if (value != null)
    //    {
    //        if (value is string stringValue)
    //            writer.Write(stringValue);
    //        else if (value is int intValue)
    //            writer.Write(intValue);
    //        else if (value is bool boolValue)
    //            writer.Write(boolValue);
    //        else if (value is float floatValue)
    //            writer.Write(floatValue);
    //        else if (value is double doubleValue)
    //            writer.Write(doubleValue);
    //        else if (value is uint uintValue)
    //            writer.Write(uintValue);
    //        else if (value is ulong uLongValue)
    //            writer.Write(uLongValue);
    //    }
    //}
    //private object ReadValue(Type dataType, BinaryReader reader)
    //{
    //    if (dataType == typeof(int))
    //        return reader.ReadInt32();
    //    else if (dataType == typeof(bool))
    //        return reader.ReadBoolean();
    //    else if (dataType == typeof(string))
    //        return reader.ReadString();
    //    else if (dataType == typeof(float))
    //        return reader.ReadSingle();
    //    else if (dataType == typeof(double))
    //        return reader.ReadDouble();
    //    else if (dataType == typeof(uint))
    //        return reader.ReadUInt32();
    //    else if (dataType == typeof(ulong))
    //        return reader.ReadUInt64();
    //    throw new NotSupportedException($"Deserialization of type {dataType} is not supported.");
    //}
    //#endregion

    public virtual void Awake()
    {
        if (TryGetComponent<NetworkObject>(out networkObject) == false)
        {
            Debug.LogWarning("A NetworkBehaviour needs a NetworkObject");
        }
    }
    public void SendData()
    {
        if (NetworkManager.Instance == false && networkObject == false)
        {
            Debug.LogWarning("No NetworkManager or NetworkObject");
        }

        MemoryStream stream = new MemoryStream();
        Write(stream);
        
        // MemoryStream Write(MemoryStream outputStream);

        // Send MemoryStream to netowrk manager buffer

       NetworkManager.Instance.AddStateStreamQueue(stream);
    }
    private void Update()
    {
        // Send Write to state buffer
        // SendData();
        tickCounter += Time.deltaTime;
    }
}

public class ChangeTracker
{
    private BitArray bitfield;

    public ChangeTracker(int numberOfFields)
    {
        // Initialize the bitfield with the specified number of fields
        bitfield = new BitArray(numberOfFields);
    }

    public void TrackChange(int fieldIndex)
    {
        // Set the corresponding bit to 1 to indicate the field has changed
        bitfield.Set(fieldIndex, true);
    }
    public void SetAll(bool value)
    {
        bitfield.SetAll(value);
    }

    public bool HasChanged(int fieldIndex)
    {
        // Check if the corresponding bit is set
        return bitfield.Get(fieldIndex);
    }

    public BitArray GetBitfield()
    {
        return bitfield;
    }
}

