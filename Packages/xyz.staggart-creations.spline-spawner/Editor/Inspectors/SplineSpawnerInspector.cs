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
using System.Reflection;
using sc.splines.spawner.runtime;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Splines;
using UnityEditorInternal;
using UnityEngine;

namespace sc.splines.spawner.editor
{
    [CustomEditor(typeof(SplineSpawner))]
    [CanEditMultipleObjects]
    public class SplineSpawnerInspector : Editor
    {
        SplineSpawner spawner;
        
        private SerializedProperty splineContainer;
        private SerializedProperty respawnTriggers;
        private SerializedProperty splineChangeTrigger;
        private SerializedProperty root;
        private SerializedProperty hideInstances;
        private SerializedProperty startupBehaviour;
        
        private SerializedProperty inputObjects;
        private SerializedProperty distributionSettings;
        private SerializedProperty maskRules;
        private SerializedProperty terrainLayerMasks;
        private SerializedProperty respawnOnTerrainTextureChange;
        
        private SerializedProperty modifiers;
        //private SerializedProperty modifierStack;

        private Vector2 modifierScrollPos;

        private Texture2D defaultThumb;
        private const float ThumbSize = 75f;
        private Texture2D maskIcon;
        private int newPrefabPickerWindowID;
        
        DistributionSettingsEditor distributionSettingsEditor;

        private static bool ShowStats
        {
            get => SessionState.GetBool("SPLINE_SPAWNER_DISPLAY_DATA", false);
            set => SessionState.SetBool("SPLINE_SPAWNER_DISPLAY_DATA", value);
        }
        
        private static bool ExpandPrefabs
        {
            get => SessionState.GetBool("SPLINE_SPAWNER_EXPAND_PREFABS", false);
            set => SessionState.SetBool("SPLINE_SPAWNER_EXPAND_PREFABS", value);
        }
        private static bool ExpandDistribution
        {
            get => SessionState.GetBool("SPLINE_SPAWNER_EXPAND_DISTRIBUTION", false);
            set => SessionState.SetBool("SPLINE_SPAWNER_EXPAND_DISTRIBUTION", value);
        }
        private static bool ExpandMasking
        {
            get => SessionState.GetBool("SPLINE_SPAWNER_EXPAND_MASKING", false);
            set => SessionState.SetBool("SPLINE_SPAWNER_EXPAND_MASKING", value);
        }
        private static bool ExpandTerrain
        {
            get => SessionState.GetBool("SPLINE_SPAWNER_EXPAND_TERRAIN", false);
            set => SessionState.SetBool("SPLINE_SPAWNER_EXPAND_TERRAIN", value);
        }
        private static bool ExpandModifiers
        {
            get => SessionState.GetBool("SPLINE_SPAWNER_EXPAND_MODIFIERS", false);
            set => SessionState.SetBool("SPLINE_SPAWNER_EXPAND_MODIFIERS", value);
        }
        
        private static int SelectedModifier
        {
            get => SessionState.GetInt("SPLINE_SPAWNER_SELECTED_MODIFIER", 0);
            set => SessionState.SetInt("SPLINE_SPAWNER_SELECTED_MODIFIER", value);
        }

        private bool inspectingPrefab;
        private bool isAbleToSpawn;
        private int openSplineCount;
        
        private ReorderableList modifierList;
        private void OnEnable()
        {
            spawner = (SplineSpawner)target;
            isAbleToSpawn = spawner.IsAllowedToSpawn();
            
            inspectingPrefab = PrefabUtility.IsPartOfPrefabInstance(spawner.gameObject) && spawner.gameObject.scene == null;
            
            splineContainer = serializedObject.FindProperty("splineContainer");
            respawnTriggers = serializedObject.FindProperty("respawnTriggers");
            splineChangeTrigger = serializedObject.FindProperty("splineChangeTrigger");
            root = serializedObject.FindProperty("root");
            hideInstances = serializedObject.FindProperty("hideInstances");
            startupBehaviour = serializedObject.FindProperty("startupBehaviour");
            
            inputObjects = serializedObject.FindProperty("inputObjects");
            distributionSettings = serializedObject.FindProperty("distributionSettings");
            maskRules = serializedObject.FindProperty("maskRules");
            terrainLayerMasks = serializedObject.FindProperty("terrainLayerMasks");
            respawnOnTerrainTextureChange = serializedObject.FindProperty("respawnOnTerrainTextureChange");
            modifiers = serializedObject.FindProperty("modifiers");

            distributionSettingsEditor = DistributionSettingsEditor.CreateEditor(distributionSettings);
            
            defaultThumb = EditorGUIUtility.IconContent("GameObject Icon").image as Texture2D;

            IconAttribute maskIconAttribute = (IconAttribute)(typeof(SplineSpawnerMask)).GetCustomAttribute(typeof(IconAttribute));
            if (maskIconAttribute != null)
            {
                maskIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(maskIconAttribute.path);
            }
            
            ValidateTargets();

            if (spawner.HasMissingPrefabs())
            {
                ExpandPrefabs = true;
            }

            InitModifierStack();
        }
        
        // layer list view
        const int kElementHeight = 40;
        const int kElementObjectFieldHeight = 16;
        const int kElementPadding = 2;
        const int kElementObjectFieldWidth = 140;
        const int kElementToggleWidth = 20;
        const int kElementThumbSize = 40;

        private void InitModifierStack()
        {
            modifierList = new ReorderableList(serializedObject, modifiers, true, true, true, true);
            
            modifierList.drawHeaderCallback = (Rect rect) => {
                EditorGUI.LabelField(rect, "Stack");
            };

            modifierList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                SerializedProperty element = modifiers.GetArrayElementAtIndex(index);
                
                Modifier modifier = element.managedReferenceValue as Modifier;

                if (modifier == null) return;
                
                rect.y = rect.y;
                var rectButton = new Rect((rect.x + kElementPadding), rect.y + kElementPadding, kElementToggleWidth,
                    kElementToggleWidth);
                var labelRect = new Rect(rect.x + rectButton.x - 19, rect.y+kElementPadding, 120, 17);
                
                ModifierAttribute attribute = ModifierAttribute.GetFor(modifier.GetType());

                if (attribute != null)
                {
                    EditorGUI.LabelField(labelRect, attribute.displayName, EditorStyles.boldLabel);
                }
                SerializedProperty enabled = element.FindPropertyRelative("enabled");
                
                enabled.boolValue = EditorGUI.Toggle(rectButton, GUIContent.none, enabled.boolValue);
            };

            modifierList.onSelectCallback += OnModifierSelect;

            modifierList.elementHeightCallback = (int index) =>
            {
                return EditorGUIUtility.singleLineHeight + kElementPadding;
                //return EditorGUI.GetPropertyHeight(modifiers.GetArrayElementAtIndex(index), true) + 4f;
            };

            modifierList.onAddDropdownCallback = (Rect rect, ReorderableList list) =>
            {
                GenericMenu menu = new GenericMenu();

                void OnModifierAdded()
                {
                    spawner.Respawn();

                    modifierList.index = spawner.modifiers.Count-1;
                    
                    EditorUtility.SetDirty(spawner);
                }
                for (int i = 0; i < Modifier.names.Length; i++)
                {
                    int index = i;
                    menu.AddItem(new GUIContent(Modifier.names[index]), false, () =>
                    {
                        spawner.AddModifier(Modifier.types[index]);
                        OnModifierAdded();
                    });
                }
                
                menu.AddSeparator("———————");
                
                menu.AddItem(new GUIContent("Presets/Random Y Rotation"), false, () =>
                {
                    spawner.AddModifier(Rotate.CreateRandomY());
                    OnModifierAdded();
                });
                menu.AddItem(new GUIContent("Presets/Random XYZ Rotation"), false, () =>
                {
                    spawner.AddModifier(Rotate.CreateRandomXYZ());
                    OnModifierAdded();
                });
                menu.AddItem(new GUIContent("Presets/Scale 20% variation"), false, () =>
                {
                    spawner.AddModifier(Scale.CreateRandomScale());
                    OnModifierAdded();
                });
                            
                menu.ShowAsContext();
            };

            modifierList.onRemoveCallback = (ReorderableList list) => 
            {
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
            };
            
            modifierList.drawElementBackgroundCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var prevColor = GUI.color;
                var prevBgColor = GUI.backgroundColor;

                GUI.color = index % 2 == 0
                    ? Color.grey * (EditorGUIUtility.isProSkin ? 1f : 1.7f)
                    : Color.grey * (EditorGUIUtility.isProSkin ? 1.05f : 1.66f);
                
                //Selection outline (note: can't rely on isfocused. Focus and selection aren't the same thing)
                if (index == modifierList.index)
                {
                    GUI.color = EditorGUIUtility.isProSkin ? Color.grey * 1.1f : Color.grey * 1.5f;
                    //GUI.color = GUI.skin.settings.selectionColor * 1.6f;
                    
                    Rect outline = rect;
                    EditorGUI.DrawRect(outline, EditorGUIUtility.isProSkin ? Color.gray * 1.5f : Color.gray);

                    rect.x += 1;
                    rect.y += 1;
                    rect.width -= 2;
                    rect.height -= 2;
                }
                
                EditorGUI.DrawRect(rect, GUI.color);

                GUI.color = prevColor;
                GUI.backgroundColor = prevBgColor;
            };

            modifierList.drawNoneElementCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Empty, add a modifier using the + button");
            };
            
            SelectedModifier = Mathf.Clamp(SelectedModifier, 0, modifiers.arraySize);
            modifierList.index = SelectedModifier;
        }

        private void OnModifierSelect(ReorderableList list)
        {
            SelectedModifier = Mathf.Clamp(list.index, 0, modifiers.arraySize);
        }
        
        public override void OnInspectorGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(60f);
                UI.DrawVersion();
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent(UI.iconPrefix + "Help").image, "Help window"), GUILayout.Width(30f), GUILayout.Height(21f)))
                {
                    HelpWindow.ShowWindow();
                }
                
                ShowStats = GUILayout.Toggle(ShowStats, new GUIContent(string.Empty, EditorGUIUtility.IconContent("d_UnityEditor.ProfilerWindow").image), "Button", GUILayout.MaxWidth(37f), GUILayout.MaxHeight(21f));
            }
            EditorGUILayout.Space();

            if (Unity.Burst.BurstCompiler.IsEnabled == false)
            {
                EditorGUILayout.HelpBox("Burst compilation is disabled, expect performance degradation", MessageType.Warning);
                EditorGUILayout.Separator();
            }
            
            if (isAbleToSpawn == false)
            {
                EditorGUILayout.HelpBox("\nSpawning disabled. This instance is not a scene object." +
                                        "\n\nEdit the source prefab\n", MessageType.Info);
                EditorGUILayout.Separator();
            }
            
            #if !UNITY_2022_3_OR_NEWER
            EditorGUILayout.HelpBox("Minimum required and compatible Unity version is 2022.3.23f1", MessageType.Error);
            #endif
            
            #if SPLINES
            EditorGUI.BeginChangeCheck();

            //base.OnInspectorGUI();
            
            serializedObject.Update();
            
            SplineSpawnerEditor.HasOpenSplines(spawner.SplineContainer, out openSplineCount);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(splineContainer);

                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var m_target in targets)
                    {
                        ((SplineSpawner)m_target).RebuildSplineCache();
                    }
                    ValidateTargets();
                }
                
                if (splineContainer.objectReferenceValue)
                {
                    if (GUILayout.Button("Edit", EditorStyles.miniButton, GUILayout.Width(50f)))
                    {
                        Selection.activeGameObject = spawner.SplineContainer.gameObject;
                        EditorApplication.delayCall += ToolManager.SetActiveContext<SplineToolContext>;
                    }
                }
                else
                {
                    if (GUILayout.Button("Add", EditorStyles.miniButton, GUILayout.Width(50f)))
                    {
                        splineContainer.objectReferenceValue = SplineSpawnerEditor.AddSplineContainer(spawner.gameObject);
                    }
                }
            }

            if (splineContainer.objectReferenceValue || inspectingPrefab)
            {
                EditorGUI.indentLevel++;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(splineChangeTrigger, new GUIContent("Change Trigger", splineChangeTrigger.tooltip),GUILayout.MaxWidth(EditorGUIUtility.labelWidth + 200f));

                    if (splineChangeTrigger.intValue == (int)SplineSpawner.SplineChangeTrigger.None)
                    {
                        if (GUILayout.Button("Respawn", EditorStyles.miniButton, GUILayout.Width(100f)))
                        {
                            RespawnTargets();
                        }
                    }
                }

                EditorGUI.indentLevel--;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(root);
                    if (GUILayout.Button("This", EditorStyles.miniButton, GUILayout.Width(50f)))
                    {
                        root.objectReferenceValue = spawner.gameObject;
                    }
                }

                EditorGUI.indentLevel++;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(hideInstances);
                }
                EditorGUI.indentLevel--;
                
                EditorGUILayout.Separator();

                int m_rebuildTrigger = respawnTriggers.intValue;
                bool IsTriggerEnabled(SplineSpawner.RespawnTriggers trigger) => (m_rebuildTrigger & (int)trigger) == (int)trigger;
                        
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(respawnTriggers, new GUIContent("Respawn triggers", respawnTriggers.tooltip), GUILayout.Width(EditorGUIUtility.labelWidth + 140f));
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                    }
                }
                
                m_rebuildTrigger = respawnTriggers.intValue;
            
                if (IsTriggerEnabled(SplineSpawner.RespawnTriggers.OnTransformChange))
                {
                    //Check if Gizmos are disabled in the scene-view
                    if (SceneView.lastActiveSceneView && SceneView.lastActiveSceneView.drawGizmos == false)
                    {
                        EditorApplication.delayCall += () =>
                        {
                            foreach (var m_target in targets)
                            {
                                ((SplineSpawner)m_target).ListenForTransformChanges();
                            }
                        };
                    } 
                }
                
                if (!IsTriggerEnabled(SplineSpawner.RespawnTriggers.OnSplineAdded) ||
                    !IsTriggerEnabled(SplineSpawner.RespawnTriggers.OnSplineChanged) ||
                    !IsTriggerEnabled(SplineSpawner.RespawnTriggers.OnSplineRemoved))
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField(
                            "One or more spline rebuild triggers are disabled.\n\n" +
                            "Use the <b>AddCachedSpline</b>, <b>UpdateCachedSpline</b> and <b>RemoveCacheSpline</b> functions to manage spline changes manually through scripting.",
                            UI.Styles.WordWrappedRich);
                    }

                    if (GUILayout.Button("Force Spline Cache Rebuild", EditorStyles.miniButton, GUILayout.Width(220f),
                            GUILayout.Height(22f)))
                    {
                        spawner.RebuildSplineCache();
                    }
                }
                
                EditorGUILayout.PropertyField(startupBehaviour, GUILayout.MaxWidth(EditorGUIUtility.labelWidth + 160f));
                
                EditorGUILayout.Space();

                DrawPrefabs();

                DistributionSettings.DistributionMode distributionMode = (DistributionSettings.DistributionMode)distributionSettings.FindPropertyRelative("mode").enumValueIndex;
                ExpandDistribution = UI.DrawFoldout(ExpandDistribution, "Distribution", UI.Icons.Distribution, () =>
                {
                    if (openSplineCount > 0 && (distributionMode != DistributionSettings.DistributionMode.OnCurve && distributionMode != DistributionSettings.DistributionMode.OnKnots))
                    {
                        EditorGUILayout.HelpBox($"The Spline Container has {openSplineCount} {(openSplineCount > 1 ? "splines" : "spline")} that {(openSplineCount > 1 ? "aren't" : "isn't")} closed, this is required for the {distributionMode} distribution mode", MessageType.Warning);
                        EditorGUILayout.Separator();
                    }
                    
                    distributionSettingsEditor.OnInspectorGUI();

                    if (spawner.Warnings.Count > 0)
                    {
                        EditorGUILayout.Separator();
                        foreach (var msg in spawner.Warnings)
                        {
                            EditorGUILayout.HelpBox(msg, MessageType.Warning);
                        }
                    }
                }, $"({System.Text.RegularExpressions.Regex.Replace(distributionMode.ToString(), "(\\B[A-Z])", " $1")})");
                
                ExpandMasking = UI.DrawFoldout(ExpandMasking, "Masking", UI.Icons.Masking, () =>
                {
                    DrawMasking();
                }, GetMaskLayerNames());

                ExpandTerrain = UI.DrawFoldout(ExpandTerrain, "Terrain Layers", UI.Icons.Terrain, () =>
                {
                    DrawTerrainLayers();
                }, GetTerrainLayerNames());
                
                int modifierCount = spawner.modifiers.Count;
                
                ExpandModifiers = UI.DrawFoldout(ExpandModifiers, "Modifiers", UI.Icons.Modifiers, () =>
                {
                    if (modifierCount == 0)
                    {
                        EditorGUILayout.HelpBox("\n" +
                                                "Modifiers perform specific actions on each spawned object." +
                                                "\n\n" +
                                                "They can be used to add randomization, rotations or to further control positioning" +
                                                "\n", MessageType.Info);
                        
                        EditorGUILayout.Space();
                    }
                    
                    modifierList.DoLayoutList();

                    if (modifierList.count > 0 && modifierList.index >= 0 && modifierList.index < modifierList.count)
                    {
                        EditorGUILayout.Space(-10f);
                        SerializedProperty modifier = modifiers.GetArrayElementAtIndex(modifierList.index);
                        
                        ModifierAttribute attributes = ModifierAttribute.GetFor(modifier);
                        if (attributes != null)
                        {
                            if (attributes.IsIncompatibleDistributionMode(spawner.distributionSettings.mode))
                            {
                                EditorGUILayout.HelpBox("This modifier is incompatible with the current distribution mode", MessageType.Warning);
                            }
                        }
                        EditorGUILayout.PropertyField(modifier, GUIContent.none);
                    }
                }, $"({modifierCount})");
            }
            else
            {
                EditorGUILayout.HelpBox("Assign a Spline container to spawn with...", MessageType.Info);
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                isAbleToSpawn = spawner.IsAllowedToSpawn();

                //EditorApplication.delayCall += RespawnTargets;
                if (!inspectingPrefab && spawner.RespawnTriggerEnabled(SplineSpawner.RespawnTriggers.OnUIChange))
                {
                    RespawnTargets();
                }
            }

            if (splineContainer.objectReferenceValue && !inspectingPrefab)
            {
                EditorGUILayout.Space();
                
                EditorGUILayout.LabelField($"Instance count: {spawner.InstanceCount}", EditorStyles.boldLabel);
                if (ShowStats)
                {
                    
                    //EditorGUIUtility.labelWidth *= 0.5f;
                    void DrawStat(string label, string value, bool bold = false)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.PrefixLabel(label);
                            EditorGUILayout.LabelField(value, bold ? EditorStyles.boldLabel : EditorStyles.miniLabel);
                        }           
                    }
                    
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.Separator();

                        EditorGUILayout.LabelField("Processing time:", EditorStyles.boldLabel);
                        DrawStat("Distribution:", $"{spawner.lastDistributionTime}ms");
                        DrawStat("Masking:", $"{spawner.lastMaskingTime}ms");
                        DrawStat("Terrain layers:", $"{spawner.lastTerrainLayerTime}ms");
                        DrawStat("Modifiers:", $"{spawner.lastModifierStackTime}ms");
                        DrawStat("Instantiating:", $"{spawner.lastInstantiateTime}ms");
                        EditorGUILayout.Separator();
                        DrawStat("Total:", $"{spawner.LastRespawnTime}ms", true);

                        EditorGUILayout.Separator();
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                if (spawner.spawnObjects == false)
                {
                    EditorGUILayout.HelpBox("Spawning of objects has been disabled", MessageType.Warning);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(" - Respawn - ", EditorStyles.miniButtonMid))
                    {
                        RespawnTargets();
                    }
                    GUILayout.FlexibleSpace();
                }
            }
            #else
            EditorGUILayout.HelpBox("The Splines package isn't installed, component unavailable", MessageType.Error);
            #endif

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("- Staggart Creations -", EditorStyles.centeredGreyMiniLabel);
        }

        #if SPLINES
        private void DrawPrefabs()
        {
            int prefabCount = inputObjects.arraySize;
            
            ExpandPrefabs = UI.DrawFoldout(ExpandPrefabs, $"Prefabs", UI.Icons.Prefab, () =>
            {
                Event curEvent = Event.current;

                PrefabPickingActions();
                
                inputObjects.isExpanded = ExpandPrefabs;

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        GUILayout.Space(5f);

                        for (int i = 0; i < inputObjects.arraySize; i++)
                        {
                            SerializedProperty prefab = inputObjects.GetArrayElementAtIndex(i);
                            SerializedProperty prefabObject = prefab.FindPropertyRelative("prefab");
                            
                            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                            {
                                EditorGUI.BeginChangeCheck();
                                EditorGUILayout.PropertyField(prefabObject);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    var index = i;
                                    EditorApplication.delayCall += () =>
                                    {
                                        RefreshThumbnail(spawner.inputObjects[index]);
                                    };
                                }
                                
                                if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.MaxWidth(30f)))
                                {
                                    inputObjects.DeleteArrayElementAtIndex(i);
                                    //spawner.RemoveObject(i);
                                    EditorUtility.SetDirty(spawner);
                                    return; 
                                }
                            }
                            
                            if (prefabObject.objectReferenceValue)
                            {
                                PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(prefabObject.objectReferenceValue);

                                if (prefabType == PrefabAssetType.Model)
                                {
                                    EditorGUILayout.HelpBox("Model asset used: All child objects will be instantiated." +
                                                            "\n\n" +
                                                            "For singular child objects, create a prefab instead.", MessageType.Warning);
                                }
                            }

                            if (prefabObject.objectReferenceValue == null)
                            {
                                EditorGUILayout.HelpBox("An object or prefab must be assigned.", MessageType.Warning);
                            }
                            else
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    SplineSpawner.SpawnableObject spawnableObject = spawner.inputObjects[i];
                                    
                                    if(spawnableObject == null) continue;
                                    
                                    if (!spawnableObject.thumbnail) RefreshThumbnail(spawnableObject);
                                    GUILayout.Box(new GUIContent(null, spawnableObject.thumbnail), GUILayout.Width(ThumbSize), GUILayout.Height(ThumbSize));
                                    
                                    Rect thumbRect = GUILayoutUtility.GetLastRect();
            
                                    //Right click
                                    if (curEvent.isMouse && curEvent.type == EventType.MouseUp && curEvent.button == 1)
                                    {
                                        if (thumbRect.Contains(curEvent.mousePosition))
                                        {
                                            GenericMenu menu = new GenericMenu();
                                            
                                            menu.AddItem(new GUIContent("Refresh thumbnail"), false, () =>
                                            {
                                                RefreshThumbnail(spawnableObject);
                                            });

                                            menu.AddItem(new GUIContent("Debug prefab data"), false, () =>
                                            {
                                                SplineSpawnerEditor.DrawPrefabDataPopup(thumbRect, spawnableObject);
                                            });
                                            
                                            menu.ShowAsContext();
                                        }
                                    }
                                    
                                    GUILayout.Space(8f);
                                    
                                    using (new EditorGUILayout.VerticalScope())
                                    {
                                        EditorGUILayout.Separator();
                                        
                                        //Experimental
                                        //EditorGUILayout.PropertyField(prefab.FindPropertyRelative("forwardDirection"));
                                        //EditorGUILayout.PropertyField(prefab.FindPropertyRelative("pivot"));

                                        //SerializedProperty selectBySplineDistance = prefab.FindPropertyRelative("selectBySplineDistance");
                                        //EditorGUILayout.PropertyField(selectBySplineDistance);
                                        
                                        SerializedProperty baseScale = prefab.FindPropertyRelative("baseScale");
                                        EditorGUILayout.PropertyField(baseScale);
                                        
                                        SerializedProperty probability = prefab.FindPropertyRelative("probability");
                                        using (new EditorGUILayout.HorizontalScope())
                                        {
                                            EditorGUILayout.PropertyField(probability);
                                            EditorGUILayout.LabelField("%", EditorStyles.boldLabel, GUILayout.MaxWidth(13f));
                                        }
                                    }

                                    
                                    GUILayout.Space(10f);
                                }
                            }
                            
                            EditorGUILayout.Separator();
                        }
                    }
                }

                void RefreshThumbnail(SplineSpawner.SpawnableObject obj)
                {
                    if (obj.prefab == null)
                    {
                        obj.thumbnail = defaultThumb;
                        return;
                    }
                    obj.thumbnail = AssetPreview.GetAssetPreview(obj.prefab);
                }
                
                void DropAreaGUI()
                {
                    Event currentEvent = Event.current;

                    Rect activeArea = GUILayoutUtility.GetRect(0, 50f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                    string text = "+ Drag & drop prefabs to add here";
                    switch (currentEvent.type)
                    {
                        case EventType.DragUpdated:
                        case EventType.DragPerform:
                            if (!activeArea.Contains(currentEvent.mousePosition))
                            {
                                return;
                            }

                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                            if (currentEvent.type == EventType.DragPerform)
                            {
                                int objectCount = DragAndDrop.objectReferences.Length;

                                if (objectCount == 1)
                                {
                                    if (DragAndDrop.objectReferences[0] is not GameObject)
                                    {
                                        Event.current.Use();
                                        break;
                                    }
                                }
                                DragAndDrop.AcceptDrag();

                                if(DragAndDrop.objectReferences.Length > 0) Undo.RecordObjects(targets, "Drop & Drop prefabs into spawner");
                                
                                foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                                {
                                    int addedPrefabCount = 0;
                                    if (draggedObject is GameObject gameObject)
                                    {
                                        foreach (var m_target in targets)
                                        {
                                            ((SplineSpawner)m_target).AddObject(gameObject);
                                        }
                                        addedPrefabCount++;
                                    }
                                    
                                    if(addedPrefabCount > 0) RespawnTargets();
                                }
                                
                                DragAndDrop.activeControlID = 0;
                                Event.current.Use();
                            }

                            break;
                    }
                    
                    GUI.Box(activeArea, GUIContent.none, EditorStyles.textArea);
                    GUI.Label(activeArea, text, EditorStyles.centeredGreyMiniLabel);
                }

                //Drag & drop new prefabs
                DropAreaGUI();
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Add from project"))
                    {
                        newPrefabPickerWindowID = EditorGUIUtility.GetControlID(FocusType.Passive) + 200; 
                        EditorGUIUtility.ShowObjectPicker<GameObject>(null, false, "t:prefab", newPrefabPickerWindowID);
                    }
                }
            },
                $"({prefabCount})");
        }

        private void PrefabPickingActions()
        {
            //New specifics (initial prefab)
            if (Event.current.commandName == "ObjectSelectorClosed" &&
                EditorGUIUtility.GetObjectPickerControlID() == newPrefabPickerWindowID)
            {
                GameObject pickedPrefab = (GameObject)EditorGUIUtility.GetObjectPickerObject();
                newPrefabPickerWindowID = -1;

                if (pickedPrefab == null) return;

                SplineSpawner.SpawnableObject spawnableObject = new SplineSpawner.SpawnableObject(pickedPrefab);

                spawner.AddObject(pickedPrefab);
                
                EditorUtility.SetDirty(spawner);

                spawner.Respawn();
            }
        }
        
        private void DrawMasking()
        {
            int ruleCount = maskRules.arraySize;

            if (ruleCount == 0)
            {
                EditorGUILayout.HelpBox("Masking prevents this spawner from instantiating objects near or within other splines ", MessageType.Info);
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Edit layer names", EditorStyles.miniButton, GUILayout.MaxWidth(110f)))
                    {
                        MaskLayerEditor.MaskLayerSettingsEditor.OpenSettings();
                    }
                }
            }
            
            for (int i = 0; i < ruleCount; i++)
            {
                SerializedProperty property = maskRules.GetArrayElementAtIndex(i);
                
                SerializedProperty layer = property.FindPropertyRelative("layer");
                SerializedProperty invert = property.FindPropertyRelative("invert");
                SerializedProperty minDistance = property.FindPropertyRelative("minDistance");
                SerializedProperty falloff = property.FindPropertyRelative("falloff");

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                    {
                        EditorGUILayout.PropertyField(layer);
                        if (GUILayout.Button(EditorGUIUtility.TrIconContent("Toolbar Minus", "Choose layer add to the list"), EditorStyles.toolbarButton, GUILayout.Width(30)))
                        {
                            maskRules.DeleteArrayElementAtIndex(i);
                            return;
                        }
                    }
                    EditorGUILayout.PropertyField(invert);

                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(minDistance);
                    EditorGUILayout.PropertyField(falloff);
                }
                
                EditorGUILayout.Separator();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button(EditorGUIUtility.TrIconContent("Toolbar Plus More", "Choose layer add to the list"), GUILayout.MaxWidth(40)))
                {
                    GenericMenu menu = new GenericMenu();

                    for (int j = 0; j < MaskLayerEditor.LayerCount; j++)
                    {
                        int index = j;
                        menu.AddItem(new GUIContent(MaskLayerEditor.IndexToName(index)), false, () =>
                        {
                            List<SplineSpawner.MaskRule> maskList = new List<SplineSpawner.MaskRule>(spawner.maskRules);
                            
                            maskList.Add(new SplineSpawner.MaskRule(index));

                            spawner.maskRules = maskList.ToArray();
                            spawner.Respawn();
                            
                            EditorUtility.SetDirty(spawner);
                        });
                    }
                    
                    menu.ShowAsContext();
                }
            }
            
            EditorGUILayout.LabelField($"Spline masks in scene: {SplineSpawnerMask.Instances.Count}", EditorStyles.boldLabel);
            
            if (SplineSpawnerMask.Instances.Count == 0)
            {
                EditorGUILayout.HelpBox("Spline masks can be created through GameObject->Spline->Spawner Mask", MessageType.Info);
            }
            else
            {
                //this.Repaint();
                using (new EditorGUILayout.VerticalScope(EditorStyles.textArea))
                {
                    foreach (SplineSpawnerMask splineMask in SplineSpawnerMask.Instances)
                    {
                        var rect = EditorGUILayout.BeginHorizontal(EditorStyles.miniLabel);

                        if (rect.Contains(Event.current.mousePosition))
                        {
                            EditorGUIUtility.AddCursorRect(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 27, 27), MouseCursor.Link);
                            EditorGUI.DrawRect(rect, Color.gray * (EditorGUIUtility.isProSkin ? 0.66f : 0.20f));
                        }

                        string maskLabel = $" {splineMask.name} {MaskLayerEditor.LayersToReadableList(splineMask.layers)}";
                        if (GUILayout.Button(new GUIContent(maskLabel, maskIcon), EditorStyles.miniLabel, GUILayout.Height(20f)))
                        {
                            EditorGUIUtility.PingObject(splineMask);
                            Selection.activeGameObject = splineMask.gameObject;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
        }

        private void DrawTerrainLayers()
        {
            #if TERRAIN
            int ruleCount = terrainLayerMasks.arraySize;
            
            if (ruleCount == 0)
            {
                EditorGUILayout.HelpBox("Terrain layer masks allow restricting or removing spawned objects based on painted terrain layers.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < ruleCount; i++)
                {
                    SerializedProperty property = terrainLayerMasks.GetArrayElementAtIndex(i);
                    
                    SerializedProperty enabled = property.FindPropertyRelative("enabled");
                    SerializedProperty filterMode = property.FindPropertyRelative("filterMode");
                    SerializedProperty terrainLayer = property.FindPropertyRelative("terrainLayer");
                    SerializedProperty threshold = property.FindPropertyRelative("threshold");
                    SerializedProperty falloff = property.FindPropertyRelative("falloff");
                    
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                        {
                            EditorGUILayout.PropertyField(enabled, new GUIContent(string.Empty, "Toggle the enabled state of this mask"), GUILayout.Width(20f));
                            
                            EditorGUILayout.PropertyField(terrainLayer, GUIContent.none);
                            if (GUILayout.Button(new GUIContent("▼", "Change layer"), EditorStyles.toolbarButton, GUILayout.Width(30)))
                            {
                                GenericMenu menu = new GenericMenu();
                    
                                if (Terrain.activeTerrain)
                                {
                                    TerrainLayer[] terrainLayers = Terrain.activeTerrain.terrainData.terrainLayers;
                                    for (int t = 0; t < terrainLayers.Length; t++)
                                    {
                                        if(terrainLayers[t] == null) continue;
                            
                                        int index = t;
                                        menu.AddItem(new GUIContent(terrainLayers[t].name), false, () =>
                                        {
                                            spawner.terrainLayerMasks[i].terrainLayer = terrainLayers[index];

                                            spawner.Respawn();
                            
                                            EditorUtility.SetDirty(spawner);
                                        });
                                    }
                                }
                
                                menu.ShowAsContext();
                                return;
                            }
                            if (GUILayout.Button(EditorGUIUtility.TrIconContent("Toolbar Minus", "Remove layer mask"), EditorStyles.toolbarButton, GUILayout.Width(30)))
                            {
                                terrainLayerMasks.DeleteArrayElementAtIndex(i);
                                return;
                            }
                        }

                        EditorGUI.BeginDisabledGroup(enabled.boolValue == false);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            Texture2D terrainLayerIcon = GetTerrainLayerIcon(terrainLayer.objectReferenceValue as TerrainLayer);
                            
                            GUILayout.Box(
                                new GUIContent(terrainLayerIcon),
                                GUILayout.Width(75),
                                GUILayout.Height(75)
                            );

                            GUILayout.Space(8f);

                            using (new EditorGUILayout.VerticalScope())
                            {
                                EditorGUILayout.Separator();

                                float labelWidth = EditorGUIUtility.labelWidth;
                                EditorGUIUtility.labelWidth = 100f;

                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    EditorGUILayout.LabelField(filterMode.displayName,
                                        GUILayout.MaxWidth(EditorGUIUtility.labelWidth));

                                    filterMode.enumValueIndex = GUILayout.Toolbar(
                                        filterMode.enumValueIndex,
                                        filterMode.enumDisplayNames
                                    );
                                }

                                EditorGUILayout.Separator();

                                EditorGUILayout.PropertyField(threshold);
                                EditorGUILayout.PropertyField(falloff);
                                EditorGUIUtility.labelWidth = labelWidth;
                                
                            }
                        }
                        EditorGUI.EndDisabledGroup();
                        
                        EditorGUILayout.Separator();
                    }
                
                    EditorGUILayout.Separator();
                }

            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
            
                if (GUILayout.Button(EditorGUIUtility.TrIconContent("Toolbar Plus More", "Choose layer add to the list"), GUILayout.MaxWidth(40)))
                {
                    GenericMenu menu = new GenericMenu();
                    
                    if (Terrain.activeTerrain)
                    {
                        TerrainLayer[] terrainLayers = Terrain.activeTerrain.terrainData.terrainLayers;
                        for (int i = 0; i < terrainLayers.Length; i++)
                        {
                            if(terrainLayers[i] == null) continue;
                            
                            int index = i;
                            menu.AddItem(new GUIContent(terrainLayers[i].name), false, () =>
                            {
                                spawner.terrainLayerMasks.Add(new SplineSpawner.TerrainLayerMask(terrainLayers[index]));

                                spawner.Respawn();
                            
                                EditorUtility.SetDirty(spawner);
                            });
                        }
                    }
                
                    menu.ShowAsContext();
                }
            }
            
            EditorGUILayout.PropertyField(respawnOnTerrainTextureChange, new GUIContent("Auto respawn on terrain change", respawnOnTerrainTextureChange.tooltip));
            #else
            EditorGUILayout.HelpBox("Terrain module is disabled in this project, functionality is unavailable.", MessageType.Info);
            #endif
        }
        #endif

        private void OnDisable()
        {
            //Keep editor memory performance healthy. Dispose of allocated resources when user interaction is done
            foreach (var m_target in targets)
            {
                ((SplineSpawner)m_target).Dispose();
            }
        }

        string GetMaskLayerNames()
        {
            List<string> names = new List<string>();
            for (int i = 0; i < maskRules.arraySize; i++)
            {
                SerializedProperty property = maskRules.GetArrayElementAtIndex(i);
                SerializedProperty layer = property.FindPropertyRelative("layer");

                names.Add(MaskLayerEditor.IndexToName(layer.intValue));
            }

            return names.Count > 0 ? $"({string.Join(", ", names)})" : string.Empty;
        }
        
        string GetTerrainLayerNames()
        {
            List<string> names = new List<string>();
            for (int i = 0; i < terrainLayerMasks.arraySize; i++)
            {
                SerializedProperty property = terrainLayerMasks.GetArrayElementAtIndex(i);
                SerializedProperty enabled = property.FindPropertyRelative("enabled");
                SerializedProperty layer = property.FindPropertyRelative("terrainLayer");

                if (layer.objectReferenceValue && enabled.boolValue)
                {
                    SerializedProperty filterMode = property.FindPropertyRelative("filterMode");

                    names.Add($"{((SplineSpawner.TerrainLayerMask.FilterMode)filterMode.intValue == SplineSpawner.TerrainLayerMask.FilterMode.Include ? "+" : "-")}{layer.objectReferenceValue.name}");
                }
            }

            return names.Count > 0 ? $"({string.Join(", ", names)})" : string.Empty;
        }

        Texture2D GetTerrainLayerIcon(TerrainLayer layer)
        {
            if (!layer) return EditorGUIUtility.IconContent("DefaultAsset Icon").image as Texture2D;

            return layer.diffuseTexture ? layer.diffuseTexture : Texture2D.grayTexture;
        }
        
        private void RespawnTargets()
        {
            if (!isAbleToSpawn) return;
            
            foreach (var m_target in targets)
            {
                SplineSpawner targetSpawner = (SplineSpawner)m_target;
                targetSpawner.Respawn();
                targetSpawner.CountInstances();
            }
        }

        private void ValidateTargets()
        {
            foreach (var m_target in targets)
            {
                SplineSpawner targetSpawner = (SplineSpawner)m_target;
                targetSpawner.Validate();
                targetSpawner.CountInstances();
            }
        }
    }

    
}