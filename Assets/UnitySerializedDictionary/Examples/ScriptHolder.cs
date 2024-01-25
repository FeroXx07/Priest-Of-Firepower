using Unity.Collections;
using UnityEngine;
using UnityEngine.Collections.Generic;

public class ScriptHolder : MonoBehaviour
{
    [SerializeField] private UDictionaryIntColor intColor;

    [SerializeField]
    [ReadOnly]
    private UDictionaryStringInt stringIntReadOnly;
    [SerializeField] private UDictionaryIntStruct intStruct;
    [SerializeField] private UDictionaryStructStruct structStruct;
    [SerializeField] private UDictionaryDoubleClass doubleClass;

    public UDictionaryNoSerializebleKey noSerializebleKey;
    public UDictionaryNoSerializebleValue noSerializebleValue;

    private void Start()
    {
        stringIntReadOnly.Add("a", 1);
        stringIntReadOnly.Add("b", 1);
        stringIntReadOnly.Add("c", 1);
    }
}
