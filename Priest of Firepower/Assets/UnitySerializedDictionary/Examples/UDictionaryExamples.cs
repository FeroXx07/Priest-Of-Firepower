using System;
using UnityEngine;
using UnityEngine.Collections.Generic;

[Serializable]
public class UDictionaryStringInt : UDictionary<string, int> { }

[Serializable]
public class UDictionaryIntStruct : UDictionary<int, OtherStruct> { }

[Serializable]
public class UDictionaryStructStruct : UDictionary<Struct, OtherStruct> { }

[Serializable]
public class UDictionaryDoubleClass : UDictionary<double, Class> { }

[Serializable]
public class UDictionaryNoSerializebleKey : UDictionary<NoSerializebleClass, int> { }

[Serializable]
public class UDictionaryNoSerializebleValue : UDictionary<int, NoSerializebleClass> { }

[Serializable]
public struct Struct
{
    public string Text;
    public int Int;
}

[Serializable]
public struct OtherStruct
{
    public string Text;
    public float Float;
    public int Int;
    public Color Color;
}

[Serializable]
public class Class
{
    public string Text;
    public int Int;
}

public class NoSerializebleClass
{
    public int Int;
    public string Text;
}
