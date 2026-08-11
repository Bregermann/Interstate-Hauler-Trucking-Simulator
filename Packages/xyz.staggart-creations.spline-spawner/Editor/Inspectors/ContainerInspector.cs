// Spline Spawner © Staggart Creations (http://staggart.xyz)
// COPYRIGHT PROTECTED UNDER THE UNITY ASSET STORE EULA (https://unity.com/legal/as-terms)
//
// ⚠️ WARNING: UNAUTHORIZED USE OR DISTRIBUTION IS STRICTLY PROHIBITED
// • Copying, referencing, or reverse-engineering this source code for the creation of new Asset Store or derivative products,
//   or any other publicly distributed content is strictly forbidden and will result in legal action.
// • Studying this file for the purpose of reproducing its functionality in your own assets or tools is not permitted.
// • If you are viewing this file as a reference, please close it immediately to avoid unintentional design influence or potential EULA violations.
// • Uploading this file or any derivative of it to a public GitHub or similar repository will trigger an automated DMCA takedown request.
// • Studying to understand for personal, educational or integration purposes is allowed, studying to reproduce is not.

using System;
using System.Collections.Generic;
using sc.splines.spawner.runtime;
using UnityEditor;
using UnityEngine;

namespace sc.splines.spawner.editor
{
    [CustomEditor(typeof(SplineInstanceContainer))]
    public class ContainerInspector : Editor
    {
        private SplineInstanceContainer component;

        private SerializedProperty usePooling;
        private SerializedProperty linkedPrefabs;

        private void OnEnable()
        {
            component = (SplineInstanceContainer)target;

            usePooling = serializedObject.FindProperty("usePooling");
            linkedPrefabs = serializedObject.FindProperty("linkedPrefabs");
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            
            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.ObjectField("Belongs to:", component.owner, typeof(SplineSpawner), true);
            }
            if (component.owner == null)
            {
                EditorGUILayout.HelpBox("This container does not appear to belong to any Spline Spawner component, it has been orphaned", MessageType.Warning);
            }
            
            serializedObject.Update();
            
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.PropertyField(usePooling);

            if (usePooling.boolValue)
            {
                EditorGUILayout.HelpBox($"Pool size: {component.PoolSize}", MessageType.None);

                foreach (KeyValuePair<GameObject, Queue<GameObject>> pool in component.prefabPools)
                {
                    EditorGUILayout.LabelField($"Pool Key: {pool.Key.name}");
                    EditorGUILayout.LabelField($"Pool Size: {pool.Value.Count}");

                    foreach (GameObject queue in pool.Value)
                    {

                    }
                }
            }

            EditorGUILayout.PropertyField(linkedPrefabs);
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField($"Instances ({component.InstanceCount})", EditorStyles.miniBoldLabel);
        }
    }
}