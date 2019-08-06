using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NpmPackageLoader.Loaders
{
#if PUBLISHER_ENV
    [CustomEditor(typeof(UnityPackageLoader), true)]
    public class UnityPackageLoaderEditor : Editor
    {
        private const string PackedObjectsPropertyName = "packedObjects";

        private readonly string[] _excludedProperties = {"m_Script", PackedObjectsPropertyName};

        private readonly List<int> _packedObjectDeleteIndexes = new List<int>();
        private SerializedProperty _packedObjects;

        // ReSharper disable once InconsistentNaming
        public new UnityPackageLoader target => (UnityPackageLoader) base.target;

        private void OnEnable()
        {
            _packedObjects = serializedObject.FindProperty(PackedObjectsPropertyName);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, _excludedProperties);

            GUILayout.Space(10);
            GUILayout.Label("Packed Assets", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            DrawPackedAssets();
            EditorGUI.indentLevel--;

            GUILayout.Space(5);

            GUILayout.Label("Add Asset", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            DrawAddAsset();
            EditorGUI.indentLevel--;

            GUILayout.Space(15);

            GUILayout.Space(15);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPackedAssets()
        {
            if (_packedObjects.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No Assets", MessageType.Info);
                return;
            }

            for (int i = 0; i < _packedObjects.arraySize; i++)
            {
                var packedObjectProperty = _packedObjects.GetArrayElementAtIndex(i);
                var packedObjectPath = packedObjectProperty.stringValue;
                var asset = AssetDatabase.LoadAssetAtPath<Object>(packedObjectPath);

                using (new GUILayout.HorizontalScope())
                {
                    if (asset != null)
                    {
                        EditorGUILayout.ObjectField(asset, typeof(Object), false);
                    }
                    else
                    {
                        using (new GUIColorScope(Color.yellow))
                        {
                            EditorGUILayout.TextField(GUIContent.none, "Invalid: " + packedObjectPath);
                        }
                    }

                    if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(25)))
                    {
                        _packedObjectDeleteIndexes.Add(i);
                    }
                }
            }

            for (int i = _packedObjectDeleteIndexes.Count - 1; i >= 0; i--)
            {
                var index = _packedObjectDeleteIndexes[i];
                _packedObjects.DeleteArrayElementAtIndex(index);
            }

            _packedObjectDeleteIndexes.Clear();
        }

        private void DrawAddAsset()
        {
            var newAsset = EditorGUILayout.ObjectField(null, typeof(Object), false);
            if (newAsset != null)
            {
                var newAssetPath = AssetDatabase.GetAssetPath(newAsset);
                var index = _packedObjects.arraySize;
                _packedObjects.InsertArrayElementAtIndex(index);
                _packedObjects.GetArrayElementAtIndex(index).stringValue = newAssetPath;
            }
        }
    }
#endif

    internal struct GUIColorScope : IDisposable
    {
        private readonly Color _color;

        public GUIColorScope(Color color)
        {
            _color = GUI.color;
            GUI.backgroundColor = color;
        }

        public void Dispose() => GUI.backgroundColor = _color;
    }
}