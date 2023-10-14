using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DataType
{
    Ping,
    Chat,
    Transform
}
public abstract class NetworkData <T>
{
    public abstract DataType DataType { get; set; }

    public abstract byte[] Serialize();
    public abstract T Deserialize(byte[] data);
}

public class PositionData
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}

public class RotationData
{
    public float Z { get; set; }
}

