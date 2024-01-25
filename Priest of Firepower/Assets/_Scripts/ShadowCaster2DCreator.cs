using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace _Scripts
{
#if UNITY_EDITOR

    [RequireComponent(typeof(CompositeCollider2D))]
    public class ShadowCaster2DCreator : MonoBehaviour
    {
        [SerializeField]
        private bool selfShadows = true;

        private CompositeCollider2D _tilemapCollider;

        static readonly FieldInfo MeshField = typeof(ShadowCaster2D).GetField("m_Mesh", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly FieldInfo ShapePathField = typeof(ShadowCaster2D).GetField("m_ShapePath", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly FieldInfo ShapePathHashField = typeof(ShadowCaster2D).GetField("m_ShapePathHash", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly MethodInfo GenerateShadowMeshMethod = typeof(ShadowCaster2D)
            .Assembly
            .GetType("UnityEngine.Rendering.Universal.ShadowUtility")
            .GetMethod("GenerateShadowMesh", BindingFlags.Public | BindingFlags.Static);

        public void Create()
        {
            DestroyOldShadowCasters();
            _tilemapCollider = GetComponent<CompositeCollider2D>();

            for (int i = 0; i < _tilemapCollider.pathCount; i++)
            {
                Vector2[] pathVertices = new Vector2[_tilemapCollider.GetPathPointCount(i)];
                _tilemapCollider.GetPath(i, pathVertices);
                GameObject shadowCaster = new GameObject("shadow_caster_" + i);
                shadowCaster.transform.parent = gameObject.transform;
                ShadowCaster2D shadowCasterComponent = shadowCaster.AddComponent<ShadowCaster2D>();
                shadowCasterComponent.selfShadows = this.selfShadows;

                Vector3[] testPath = new Vector3[pathVertices.Length];
                for (int j = 0; j < pathVertices.Length; j++)
                {
                    testPath[j] = pathVertices[j];
                }

                ShapePathField.SetValue(shadowCasterComponent, testPath);
                ShapePathHashField.SetValue(shadowCasterComponent, Random.Range(int.MinValue, int.MaxValue));
                MeshField.SetValue(shadowCasterComponent, new Mesh());
                GenerateShadowMeshMethod.Invoke(shadowCasterComponent,
                    new object[] { MeshField.GetValue(shadowCasterComponent), ShapePathField.GetValue(shadowCasterComponent) });
            }
        }
        public void DestroyOldShadowCasters()
        {

            var tempList = transform.Cast<Transform>().ToList();
            foreach (var child in tempList)
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    [CustomEditor(typeof(ShadowCaster2DCreator))]
    public class ShadowCaster2DTileMapEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create"))
            {
                var creator = (ShadowCaster2DCreator)target;
                creator.Create();
            }

            if (GUILayout.Button("Remove Shadows"))
            {
                var creator = (ShadowCaster2DCreator)target;
                creator.DestroyOldShadowCasters();
            }
            EditorGUILayout.EndHorizontal();
        }

    }

#endif
}