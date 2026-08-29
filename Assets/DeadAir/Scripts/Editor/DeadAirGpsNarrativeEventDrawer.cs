using UnityEditor;
using UnityEngine;

namespace DeadAir.Editor
{
    [CustomPropertyDrawer(typeof(DeadAirGpsNarrativeEvent))]
    internal sealed class DeadAirGpsNarrativeEventDrawer : PropertyDrawer
    {
        private static readonly GUIContent Header = new GUIContent("DEAD AIR GPS EVENT");
        private const float IndentPixels = 12f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            float y = position.y;
            DrawHeader(position, ref y);

            SerializedProperty enabled = property.FindPropertyRelative("enableGpsEvent");
            DrawField(position, ref y, enabled, new GUIContent("Enable GPS Event"));
            if (!enabled.boolValue)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;
            SerializedProperty eventType = property.FindPropertyRelative("eventType");
            DrawField(position, ref y, eventType, new GUIContent("GPS Event Type"));
            DrawField(position, ref y, property.FindPropertyRelative("delayBeforeGpsEvent"), new GUIContent("Delay Before GPS Event"));

            DeadAirGpsNarrativeEventType selected = (DeadAirGpsNarrativeEventType)eventType.enumValueIndex;
            switch (selected)
            {
                case DeadAirGpsNarrativeEventType.ChangeDirection:
                    DrawDirectionFields(position, ref y, property, false);
                    break;
                case DeadAirGpsNarrativeEventType.FlashAndChangeDirection:
                    DrawDirectionFields(position, ref y, property, false);
                    DrawField(position, ref y, property.FindPropertyRelative("flash"), new GUIContent("Flash"));
                    DrawAudioFields(position, ref y, property);
                    break;
                case DeadAirGpsNarrativeEventType.Glitch:
                    DrawGlitchFields(position, ref y, property);
                    DrawAudioFields(position, ref y, property);
                    break;
                case DeadAirGpsNarrativeEventType.GlitchThenDirection:
                    DrawGlitchFields(position, ref y, property);
                    DrawDirectionFields(position, ref y, property, false);
                    DrawAudioFields(position, ref y, property);
                    break;
                case DeadAirGpsNarrativeEventType.Recalculating:
                    DrawRecalculatingFields(position, ref y, property, false);
                    DrawAudioFields(position, ref y, property);
                    break;
                case DeadAirGpsNarrativeEventType.RecalculatingThenDirection:
                    DrawRecalculatingFields(position, ref y, property, true);
                    DrawField(position, ref y, property.FindPropertyRelative("flash"), new GUIContent("Flash"));
                    DrawField(position, ref y, property.FindPropertyRelative("glitch"), new GUIContent("Glitch"));
                    if (property.FindPropertyRelative("glitch").boolValue)
                    {
                        DrawGlitchFields(position, ref y, property);
                    }

                    DrawAudioFields(position, ref y, property);
                    break;
                case DeadAirGpsNarrativeEventType.ClearOverlay:
                    break;
                case DeadAirGpsNarrativeEventType.CustomCombined:
                    DrawDirectionFields(position, ref y, property, false);
                    DrawField(position, ref y, property.FindPropertyRelative("flash"), new GUIContent("Flash"));
                    DrawField(position, ref y, property.FindPropertyRelative("glitch"), new GUIContent("Glitch"));
                    if (property.FindPropertyRelative("glitch").boolValue)
                    {
                        DrawGlitchFields(position, ref y, property);
                    }

                    DrawField(position, ref y, property.FindPropertyRelative("recalculating"), new GUIContent("Recalculating"));
                    if (property.FindPropertyRelative("recalculating").boolValue)
                    {
                        DrawRecalculatingFields(position, ref y, property, true);
                    }

                    DrawAudioFields(position, ref y, property);
                    break;
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int lines = 2;
            SerializedProperty enabled = property.FindPropertyRelative("enableGpsEvent");
            if (enabled == null || !enabled.boolValue)
            {
                return HeightForLines(lines);
            }

            lines += 2;
            SerializedProperty eventType = property.FindPropertyRelative("eventType");
            DeadAirGpsNarrativeEventType selected = eventType != null ? (DeadAirGpsNarrativeEventType)eventType.enumValueIndex : DeadAirGpsNarrativeEventType.None;
            switch (selected)
            {
                case DeadAirGpsNarrativeEventType.ChangeDirection:
                    lines += 6;
                    break;
                case DeadAirGpsNarrativeEventType.FlashAndChangeDirection:
                    lines += 11;
                    break;
                case DeadAirGpsNarrativeEventType.Glitch:
                    lines += 4;
                    break;
                case DeadAirGpsNarrativeEventType.GlitchThenDirection:
                    lines += 10;
                    break;
                case DeadAirGpsNarrativeEventType.Recalculating:
                    lines += 4;
                    break;
                case DeadAirGpsNarrativeEventType.RecalculatingThenDirection:
                    lines += property.FindPropertyRelative("glitch").boolValue ? 12 : 10;
                    break;
                case DeadAirGpsNarrativeEventType.CustomCombined:
                    lines += 11;
                    if (property.FindPropertyRelative("glitch").boolValue)
                    {
                        lines += 2;
                    }

                    if (property.FindPropertyRelative("recalculating").boolValue)
                    {
                        lines += 5;
                    }

                    break;
            }

            return HeightForLines(lines);
        }

        private static void DrawHeader(Rect position, ref float y)
        {
            Rect rect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(rect, Header, EditorStyles.boldLabel);
            y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        private static void DrawDirectionFields(Rect position, ref float y, SerializedProperty property, bool finalFields)
        {
            DrawField(position, ref y, property.FindPropertyRelative(finalFields ? "finalPrimaryText" : "primaryText"), new GUIContent(finalFields ? "Final Primary Text" : "Primary Text"));
            DrawField(position, ref y, property.FindPropertyRelative(finalFields ? "finalSecondaryText" : "secondaryText"), new GUIContent(finalFields ? "Final Secondary Text" : "Secondary Text"));
            DrawField(position, ref y, property.FindPropertyRelative(finalFields ? "finalArrow" : "arrow"), new GUIContent(finalFields ? "Final Arrow" : "Arrow"));
            if (!finalFields)
            {
                DrawField(position, ref y, property.FindPropertyRelative("distanceMeters"), new GUIContent("Distance Meters"));
                DrawField(position, ref y, property.FindPropertyRelative("destination"), new GUIContent("Destination"));
                DrawField(position, ref y, property.FindPropertyRelative("intentionallyWrong"), new GUIContent("Intentionally Wrong"));
            }
        }

        private static void DrawGlitchFields(Rect position, ref float y, SerializedProperty property)
        {
            DrawField(position, ref y, property.FindPropertyRelative("glitchDuration"), new GUIContent("Glitch Duration"));
            DrawField(position, ref y, property.FindPropertyRelative("glitchIntensity"), new GUIContent("Glitch Intensity"));
        }

        private static void DrawRecalculatingFields(Rect position, ref float y, SerializedProperty property, bool includeFinal)
        {
            DrawField(position, ref y, property.FindPropertyRelative("recalculatingMessage"), new GUIContent("Recalculating Message"));
            DrawField(position, ref y, property.FindPropertyRelative("recalculatingDuration"), new GUIContent("Recalculating Duration"));
            if (includeFinal)
            {
                DrawDirectionFields(position, ref y, property, true);
            }
        }

        private static void DrawAudioFields(Rect position, ref float y, SerializedProperty property)
        {
            DrawField(position, ref y, property.FindPropertyRelative("gpsAudioClip"), new GUIContent("GPS Audio Clip"));
            DrawField(position, ref y, property.FindPropertyRelative("gpsAudioVolume"), new GUIContent("GPS Audio Volume"));
        }

        private static void DrawField(Rect position, ref float y, SerializedProperty property, GUIContent label)
        {
            if (property == null)
            {
                return;
            }

            Rect rect = new Rect(position.x + IndentPixels, y, Mathf.Max(0f, position.width - IndentPixels), EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(rect, property, label);
            y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        private static float HeightForLines(int lines)
        {
            return lines * EditorGUIUtility.singleLineHeight + Mathf.Max(0, lines - 1) * EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
