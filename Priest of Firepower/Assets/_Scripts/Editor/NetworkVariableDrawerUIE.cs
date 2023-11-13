using System.Collections;
using System.Collections.Generic;
using _Scripts.Networking;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(NetworkVariable<>))]
public class NetworkVariableDrawerUIE : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        // Create property container element.
        var container = new VisualElement();

        // Get the serialized property of the Value property
        SerializedProperty valueProp = property.FindPropertyRelative("_value");

        // Create property fields.
        var field = new PropertyField(valueProp, "Value");

        container.Add(field);

        return container;
    }
}
