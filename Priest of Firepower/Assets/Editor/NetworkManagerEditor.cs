using _Scripts.Networking;
using UnityEditor;
using UnityEngine.UIElements;

namespace Editor
{
    [CustomEditor(typeof(NetworkManager))] 
    public class NetworkManagerEditor : UnityEditor.Editor
    {
        VisualElement root;

        public override VisualElement CreateInspectorGUI()
        {
            root = new VisualElement();
            IMGUIContainer defaultInspector = new IMGUIContainer(() =>
            {
                EditorGUI.BeginChangeCheck();
                serializedObject.UpdateIfRequiredOrScript();
                DrawDefaultInspector();
                serializedObject.ApplyModifiedProperties();
                EditorGUI.EndChangeCheck();
            });
            root.Add(defaultInspector);
            NetworkManager obj = (NetworkManager)target;
            Button customButton = new Button(() => obj.InstantiatePlayer());
            customButton.text = "CreatePlayer";
            root.Add(customButton);
            return root;
        }
    }
}