using System;
using _Scripts.Networking;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;


[CustomPropertyDrawer(typeof(NetworkVariable<>),true)]
public class NetworkVariableTypeDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        // Create property container element.
        var container = new VisualElement();
        // Create property fields.
        Label displayName = new Label(property.displayName);
        container.Add(displayName);
        // Add an indentation space after the label
        container.Add(new PropertyField(property.FindPropertyRelative("value"), "Value"));
        container.Add(new PropertyField(property.FindPropertyRelative("isDirty"), "IsDirty"));
        container.style.marginBottom = 10;
        return container;
    }
}

