using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

//[CustomPropertyDrawer(typeof(NetworkVariable<int>))]
//public class NetworkVariableTypeIntDrawer : NetworkVariableTypeDrawerBase<int>
//{
//    public override VisualElement CreatePropertyGUI(SerializedProperty property)
//    {
//        Debug.Log("Draw int");
//        //return base.CreatePropertyGUI(property);
//        // Create a VisualElement to hold the property GUI
//        VisualElement root = new VisualElement();

//        // Get the target object of the serialized property
//        Object targetObject = property.serializedObject.targetObject;
//        var networkVariableField = targetObject.GetType().GetField(property.name);

//        if (networkVariableField != null)
//        {
//            var valueField = networkVariableField.FieldType.GetField("_value", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
//            object networkVariableInstance = networkVariableField.GetValue(targetObject);
//            object value = valueField.GetValue(networkVariableInstance);

//            int intValue = (int)value;
//            Label intLabel = new Label($"{intValue} This is an int-specific drawer");
//            root.Add(intLabel);
//        }
//        else
//        {
//            // Handle the case where the NetworkVariable field is not found
//            Debug.LogWarning("NetworkVariable field not found in the MonoBehaviour.");
//        }
//        return root;
//    }
//}

//[CustomPropertyDrawer(typeof(NetworkVariable<bool>))]
//public class NetworkVariableTypeBoolDrawer : NetworkVariableTypeDrawerBase<bool> { }

//[CustomPropertyDrawer(typeof(NetworkVariable<string>))]
//public class NetworkVariableTypeStringDrawer : NetworkVariableTypeDrawerBase<string>
//{
//    public override VisualElement CreatePropertyGUI(SerializedProperty property)
//    {
//        Debug.Log("Draw string");
//        //return base.CreatePropertyGUI(property);
//        VisualElement root = new VisualElement();
//        TextField stringField = new TextField(property.displayName);
//        stringField.bindingPath = property.propertyPath;
//        stringField.BindProperty(property);
//        root.Add(stringField);
//        return root;
//    }
//}

[CustomPropertyDrawer(typeof(NetworkVariable<>))]
public class NetworkVariableTypeDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        VisualElement root = new VisualElement();
        var targetObject = property.serializedObject.targetObject;
        var networkVariableField = targetObject.GetType().GetField(property.name);

        if (networkVariableField != null)
        {
            var valueField = networkVariableField.FieldType.GetField("_value", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isDirtyField = networkVariableField.FieldType.GetField("_isDirty", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (valueField != null)
            {
                object networkVariableInstance = networkVariableField.GetValue(targetObject);
                object value = valueField.GetValue(networkVariableInstance);

                if (value != null)
                {
                    CreateField(root, networkVariableField.Name, value, isDirtyField, networkVariableInstance);
                }
            }
        }
        else
        {
            Debug.LogWarning("NetworkVariable field not found in the MonoBehaviour.");
        }

        return root;
    }

    private void CreateField(VisualElement root, string fieldName, object value, System.Reflection.FieldInfo isDirtyField, object networkVariableInstance)
    {
        if (value is int intValue)
        {
            IntegerField intField = new IntegerField(fieldName);
            intField.value = intValue;
            intField.RegisterValueChangedCallback(evt => SetProperty(networkVariableInstance, isDirtyField, fieldName, evt.newValue));
            root.Add(intField);

            AddDirtyLabel(root, networkVariableInstance, isDirtyField);
        }
        else if (value is string stringValue)
        {
            TextField stringField = new TextField(fieldName);
            stringField.value = stringValue;
            stringField.RegisterValueChangedCallback(evt => SetProperty(networkVariableInstance, isDirtyField, fieldName, evt.newValue));
            root.Add(stringField);

            AddDirtyLabel(root, networkVariableInstance, isDirtyField);
        }
        else if (value is bool boolValue)
        {
            Toggle boolField = new Toggle(fieldName);
            boolField.value = boolValue;
            boolField.RegisterValueChangedCallback(evt => SetProperty(networkVariableInstance, isDirtyField, fieldName, evt.newValue));
            root.Add(boolField);

            AddDirtyLabel(root, networkVariableInstance, isDirtyField);
        }
        else if (value is float floatValue)
        {
            FloatField floatField = new FloatField(fieldName);
            floatField.value = floatValue;
            floatField.RegisterValueChangedCallback(evt => SetProperty(networkVariableInstance, isDirtyField, fieldName, evt.newValue));
            root.Add(floatField);

            AddDirtyLabel(root, networkVariableInstance, isDirtyField);
        }
        else if (value is double doubleValue)
        {
            DoubleField doubleField = new DoubleField(fieldName);
            doubleField.value = doubleValue;
            doubleField.RegisterValueChangedCallback(evt => SetProperty(networkVariableInstance, isDirtyField, fieldName, evt.newValue));
            root.Add(doubleField);

            AddDirtyLabel(root, networkVariableInstance, isDirtyField);
        }
        else if (value is uint uintValue)
        {
            IntegerField uintField = new IntegerField(fieldName);
            uintField.value = (int)uintValue;
            uintField.RegisterValueChangedCallback(evt => SetProperty(networkVariableInstance, isDirtyField, fieldName, evt.newValue));
            root.Add(uintField);

            AddDirtyLabel(root, networkVariableInstance, isDirtyField);
        }
        else if (value is ulong ulongValue)
        {
            LongField ulongField = new LongField(fieldName);
            ulongField.value = (long)ulongValue;
            ulongField.RegisterValueChangedCallback(evt => SetProperty(networkVariableInstance, isDirtyField, fieldName, evt.newValue));
            root.Add(ulongField);

            AddDirtyLabel(root, networkVariableInstance, isDirtyField);
        }

        // Add more types as needed
    }

    private void AddDirtyLabel(VisualElement root, object networkVariableInstance, System.Reflection.FieldInfo isDirtyField)
    {
        object dirtyValue = isDirtyField.GetValue(networkVariableInstance);
        Label dirtyTextField = new Label($"Dirty?: {dirtyValue.ToString()}");
        root.Add(dirtyTextField);
    }

    private void SetProperty(object networkVariableInstance, System.Reflection.FieldInfo isDirtyField, string fieldName, object value)
    {
        var field = networkVariableInstance.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            var currentValue = field.GetValue(networkVariableInstance);
            if (currentValue == null || !currentValue.Equals(value))
            {
                isDirtyField.SetValue(networkVariableInstance, true);
            }
            else
            {
                isDirtyField.SetValue(networkVariableInstance, false);
            }

            field.SetValue(networkVariableInstance, value);
        }
    }
}

//public abstract class NetworkVariableTypeDrawerBase<T> : PropertyDrawer
//{
//    // Whatever.
//}


