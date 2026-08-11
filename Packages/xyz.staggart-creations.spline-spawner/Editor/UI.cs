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
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;

namespace sc.splines.spawner.editor
{
    public static class UI
    {
        public static readonly Color RedColor = new Color(1f, 0.31f, 0.34f);
        public static readonly Color OrangeColor= new Color(1f, 0.68f, 0f);
        public static readonly Color GreenColor = new Color(0.33f, 1f, 0f);
        
        public static string iconPrefix => EditorGUIUtility.isProSkin ? "d_" : string.Empty;
        
        private static Texture FoldoutIcon => _foldoutIcon ??= EditorGUIUtility.IconContent("IN_foldout").image;
        private static Texture FoldoutOnIcon => _foldoutOnIcon ??= EditorGUIUtility.IconContent("IN_foldout_on").image;

        private static Texture _foldoutIcon;
        private static Texture _foldoutOnIcon;
        
        public static Texture CreateIcon(string data)
        {
            byte[] bytes = System.Convert.FromBase64String(data);

            Texture2D icon = new Texture2D(32, 32, TextureFormat.RGBA32, false, false);
            icon.LoadImage(bytes, true);
            
            return icon;
        }
        
        public static void DrawH2(string text)
        {
            Rect backgroundRect = EditorGUILayout.GetControlRect();
            backgroundRect.height = 25f;
            
            var labelRect = backgroundRect;

            // Background rect should be full-width
            backgroundRect.xMin = 0f;

            // Background
            float backgroundTint = (EditorGUIUtility.isProSkin ? 0.1f : 1f);
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            // Title
            EditorGUI.LabelField(labelRect, new GUIContent(text), Styles.H2);
            
            EditorGUILayout.Space(backgroundRect.height * 0.5f);
        }

        
        private const float HeaderHeight = 27f;
        public static bool DrawFoldout(bool state, string title, Texture2D icon, Action drawAction, string subtitle = "")
        {
            DrawSplitter();

            Rect backgroundRect = GUILayoutUtility.GetRect(1f, HeaderHeight);
            
            var labelRect = backgroundRect;
            labelRect.xMin += 8f;
            labelRect.xMax -= 20f + 16 + 5;

            var iconRect = labelRect;
            iconRect.width = 16;
            iconRect.height = 16;
            iconRect.y += 4f;

            if (icon)
            {
                labelRect.x += iconRect.width + 6;
            }
            
            var foldoutRect = backgroundRect;
            foldoutRect.xMin -= 12f;
            foldoutRect.width = HeaderHeight;
            foldoutRect.height = HeaderHeight;

            // Background rect should be full-width
            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;

            // Background
            float backgroundTint = (EditorGUIUtility.isProSkin ? 0.1f : 1f);
            if (backgroundRect.Contains(Event.current.mousePosition)) backgroundTint *= EditorGUIUtility.isProSkin ? 1.5f : 0.9f;
            
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));
            
            // Icon
            if (icon)
            {
                GUI.DrawTexture(iconRect, icon);
            }
            
            // Title
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            if (subtitle != string.Empty)
            {
                Rect subtitleRect = labelRect;
                subtitleRect.xMin += EditorStyles.boldLabel.CalcSize(new GUIContent(title)).x + 3f;
                EditorGUI.LabelField(subtitleRect, subtitle, EditorStyles.miniLabel);

            }

            // Foldout
            GUI.Label(foldoutRect, state ? FoldoutOnIcon : FoldoutIcon, EditorStyles.miniLabel);
            
            // Handle events
            var e = Event.current;

            if (e.type == EventType.MouseDown)
            {
                if (backgroundRect.Contains(e.mousePosition))
                {
                    if (e.button == 0)
                    {
                        state = !state;
                        //if(clickAction != null) clickAction.Invoke();
                    }

                    e.Use();
                }
            }

            if(state)
            {
                EditorGUILayout.Space();
                
                drawAction.Invoke();
                
                EditorGUILayout.Space();
            }

            return state;
        }
        
        public static void DrawSplitter(bool isBoxed = false)
        {
            var rect = GUILayoutUtility.GetRect(1f, 1f);
            float xMin = rect.xMin;

            // Splitter rect should be full-width
            rect.xMin = 0f;
            rect.width += 4f;
            
            if (isBoxed)
            {
                rect.xMin = xMin == 7.0f ? 4.0f : EditorGUIUtility.singleLineHeight;
                rect.width -= 1;
            }

            if (Event.current.type != EventType.Repaint)
                return;

            EditorGUI.DrawRect(rect, !EditorGUIUtility.isProSkin
                ? new Color(0.6f, 0.6f, 0.6f, 1.333f)
                : new Color(0.12f, 0.12f, 0.12f, 1.333f));
        }

        public static void DrawVersion()
        {
            string versionText = $"Version {AssetInfo.VERSION} ";
            string statusText = AssetInfo.VersionChecking.UPDATE_AVAILABLE ? $"({AssetInfo.VersionChecking.LATEST_VERSION_AVAILABLE} available)" : "(latest)";
    
            if (AssetInfo.VersionChecking.UPDATE_AVAILABLE)
            {
                Rect rect = EditorGUILayout.GetControlRect();
        
                //Calculate the width of the version text to position the link correctly
                GUIStyle style = EditorStyles.centeredGreyMiniLabel;
                float versionTextWidth = style.CalcSize(new GUIContent(versionText)).x;
                float statusTextWidth = style.CalcSize(new GUIContent(statusText)).x;
                float totalWidth = versionTextWidth + statusTextWidth;
        
                //Center the content
                float startX = (rect.width - totalWidth) / 2;
        
                Rect versionRect = new Rect(rect.x + startX, rect.y, versionTextWidth, rect.height);
                Rect linkRect = new Rect(rect.x + startX + versionTextWidth, rect.y, statusTextWidth, rect.height);
        
                //Draw version text
                GUI.Label(versionRect, versionText, style);
        
                //Check if hovering over the link
                bool isHovering = linkRect.Contains(Event.current.mousePosition);

                //Draw clickable link with underline on hover
                GUIStyle linkStyle = isHovering ? new GUIStyle(style) { fontStyle = FontStyle.Bold } : style;
                
                //Draw clickable link
                if (GUI.Button(linkRect, statusText, style))
                {
                    AssetInfo.OpenInPackageManager();
                }
        
                // Change cursor to hand when hovering over the link
                EditorGUIUtility.AddCursorRect(linkRect, MouseCursor.Link);
            }
            else
            {
                EditorGUILayout.LabelField(versionText + statusText, EditorStyles.centeredGreyMiniLabel);
            }
        }

        public static void DrawAsset(string name, int id, string description)
        {
            EditorGUILayout.LabelField(name, EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                Texture2D icon = Icons.DownloadAssetIcon(id);
                            
                GUILayout.Box(new GUIContent(icon), GUILayout.Width(75), GUILayout.Height(75));

                GUILayout.Space(8f);

                using (new EditorGUILayout.VerticalScope())
                {
                    GUIContent descriptionContent = new GUIContent(description);
                    float descriptionHeight = UI.Styles.WordWrappedRich.CalcHeight(
                        descriptionContent,
                        EditorGUIUtility.currentViewWidth - 40f
                    );

                    EditorGUILayout.LabelField(
                        descriptionContent,
                        UI.Styles.WordWrappedRich,
                        GUILayout.Height(descriptionHeight)
                    );

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        
                        if (GUILayout.Button("View on asset store", UI.Styles.Button))
                        {
                            Application.OpenURL(
                                $"https://assetstore.unity.com/packages/slug/{id}?aid=1011l7Uk8&pubref=sp-editor");
                        }
                    }

                }
            }
        }
        
        public static class Icons
        {
            public static string prefix => EditorGUIUtility.isProSkin ? "d_" : string.Empty;
            
            private static Texture2D LoadFromResources(string path)
            {
                string absolutePath = $"{SplineSpawner.kPackageRoot}/Editor/Resources/{path}";
                Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(absolutePath);

                if (!icon)
                {
                    Debug.LogError($"Failed to load icon at {absolutePath}. Perhaps it was moved. If so, a clean install is required.");
                    return EditorGUIUtility.IconContent("_Help").image as Texture2D;
                }

                return icon;
            }

            private static Texture2D LoadNative(string path) => EditorGUIUtility.IconContent(path).image as Texture2D;

            public static Texture2D LoadFromData(string data, int size = 32)
            {
                byte[] bytes = Convert.FromBase64String(data);

                Texture2D icon = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
                icon.LoadImage(bytes, true);
        
                return icon;
            }
            
            private static Texture2D _Distribution;
            public static Texture2D Distribution => _Distribution ??= LoadFromResources("Icons/spline-spawner-distribution-icon-64px.psd");
            
            private static Texture2D _Masking;
            public static Texture2D Masking => _Masking ??= LoadFromResources("Icons/spline-spawner-mask-icon-64px.psd");
            
            private static Texture2D _Terrain;
            public static Texture2D Terrain => _Terrain ??= LoadFromResources("Icons/spline-spawner-terrain-icon-64px.psd");
            
            private static Texture2D _Modifiers;
            public static Texture2D Modifiers => _Modifiers ??= LoadFromResources("Icons/spline-spawner-modifiers-icon-64px.psd");

            private static Texture2D _Prefab;
            public static Texture2D Prefab => _Prefab ??= LoadNative("d_Prefab Icon");
            
            private static Texture2D _Event;
            public static Texture2D Event => _Event ??= LoadNative("EventSystem Icon");

            private static readonly Dictionary<int, Texture2D> DownloadedAssetIcons = new();
            private static readonly HashSet<int> AssetIconsBeingDownloaded = new();
            
            private static Texture2D _TemporaryAssetIcon;
            private static Texture2D TemporaryAssetIcon =>
                _TemporaryAssetIcon ??= EditorGUIUtility.IconContent("d_PreMatCube").image as Texture2D;
            
            public static Texture2D DownloadAssetIcon(int id)
            {
                if (DownloadedAssetIcons.TryGetValue(id, out Texture2D cachedIcon) && cachedIcon)
                {
                    return cachedIcon;
                }
                
                if(Application.internetReachability == NetworkReachability.NotReachable) return TemporaryAssetIcon;

                if (!AssetIconsBeingDownloaded.Contains(id))
                {
                    AssetIconsBeingDownloaded.Add(id);
                    DownloadAssetIconAsync(id);
                }

                return TemporaryAssetIcon;
            }
            
            private static async void DownloadAssetIconAsync(int id)
            {
                try
                {
                    string url = $"https://api.assetstore.unity3d.com/affiliate/embed/package/{id}/icon";

                    using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);

                    UnityWebRequestAsyncOperation operation = request.SendWebRequest();

                    while (!operation.isDone)
                    {
                        await System.Threading.Tasks.Task.Yield();
                    }

                    AssetIconsBeingDownloaded.Remove(id);

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogWarning($"Failed to download Asset Store icon for package {id}: {request.error}");
                        return;
                    }

                    Texture2D icon = DownloadHandlerTexture.GetContent(request);

                    if (icon)
                    {
                        DownloadedAssetIcons[id] = icon;
                        InternalEditorUtility.RepaintAllViews();
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to download Asset Store icon for package {id}: {e.Message}");
                }
            }
            
        }
        
        public static class Styles
        {
            private static GUIStyle _Section;
            public static GUIStyle Section
            {
                get
                {
                    if (_Section == null)
                    {
                        _Section = new GUIStyle()
                        {
                            margin = new RectOffset(0, 0, -5, 5),
                            padding = new RectOffset(10, 10, 5, 5),
                            clipping = TextClipping.Clip,
                        };
                    }

                    return _Section;
                }
            }
            
            private static GUIStyle _H2;
            public static GUIStyle H2
            {
                get
                {
                    if (_H2 == null)
                    {
                        _H2 = new GUIStyle(GUI.skin.label)
                        {
                            richText = true,
                            alignment = TextAnchor.MiddleLeft,
                            wordWrap = true,
                            fontSize = 14,
                            fontStyle = FontStyle.Bold,
                            padding = new RectOffset(10, 0, 0, 0)
                        };
                    }

                    return _H2;
                }
            }
            
            private static GUIStyle _Button;
            public static GUIStyle Button
            {
                get
                {
                    if (_Button == null)
                    {
                        _Button = new GUIStyle(GUI.skin.button)
                        {
                            alignment = TextAnchor.MiddleLeft,
                            stretchWidth = true,
                            richText = true,
                            wordWrap = true,
                            padding = new RectOffset()
                            {
                                left = 14,
                                right = 14,
                                top = 8,
                                bottom = 8
                            }
                        };
                    }

                    return _Button;
                }
            }
            
            private static GUIStyle _UpdateText;
            public static GUIStyle UpdateText
            {
                get
                {
                    if (_UpdateText == null)
                    {
                        _UpdateText = new GUIStyle("Button")
                        {
                            //fontSize = 10,
                            alignment = TextAnchor.MiddleLeft,
                            stretchWidth = false,
                        };
                    }

                    return _UpdateText;
                }
            }
            
            private static GUIStyle _WordWrappedRich;
            public static GUIStyle WordWrappedRich
            {
                get
                {
                    if (_WordWrappedRich == null)
                    {
                        _WordWrappedRich = new GUIStyle(GUI.skin.label)
                        {
                            richText = true,
                            wordWrap = true,
                        };
                    }

                    return _WordWrappedRich;
                }
            }

            private static GUIStyle _Header;
            public static GUIStyle Header
            {
                get
                {
                    if (_Header == null)
                    {
                        _Header = new GUIStyle(GUI.skin.label)
                        {
                            richText = true,
                            alignment = TextAnchor.MiddleCenter,
                            wordWrap = true,
                            fontSize = 18,
                            fontStyle = FontStyle.Normal
                        };
                    }

                    return _Header;
                }
            }
            
            private static GUIStyle _Footer;
            public static GUIStyle Footer
            {
                get
                {
                    if (_Footer == null)
                    {
                        _Footer = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                        {
                            richText = true,
                            alignment = TextAnchor.MiddleCenter,
                            wordWrap = true,
                            fontSize = 12
                        };
                    }

                    return _Footer;
                }
            }
        }
    }
}