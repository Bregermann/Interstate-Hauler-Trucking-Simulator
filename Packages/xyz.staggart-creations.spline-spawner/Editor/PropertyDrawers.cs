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

using sc.splines.spawner.runtime;
using UnityEditor;
using UnityEngine;

namespace sc.splines.spawner.editor
{
    internal sealed class PropertyDrawers
    {
        [CustomPropertyDrawer(typeof(Modifier.MinMaxSlider))]
        public sealed class MinMaxSliderDrawer : PropertyDrawer
        {
            private float min;
            private float max;

            private Rect rect;

            public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
            {
                this.rect = rect;
                Modifier.MinMaxSlider range = attribute as Modifier.MinMaxSlider;

                min = property.vector2Value.x;
                max = property.vector2Value.y;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(property.displayName, GUILayout.MaxWidth(EditorGUIUtility.labelWidth));
                    min = EditorGUILayout.FloatField(min, GUILayout.Width(50f));
                    EditorGUILayout.MinMaxSlider(ref min, ref max, range.min, range.max);
                    max = EditorGUILayout.FloatField(max, GUILayout.Width(50f));
                }

                property.vector2Value = new Vector2(min, max);
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return rect.height;
            }
        }
    }
}