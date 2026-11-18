using System;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckHitchSports.Editor
{
    public static class HitchRunOwnershipAuthoring
    {
        [MenuItem("Truck Sports/Alpha 0.10/Upgrade active Hitch Run ownership badges")]
        public static void UpgradeActiveHitchRunOwnershipBadges()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled Editor required.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/TruckHitchSports/Scenes/HitchAndRun.unity")
                throw new InvalidOperationException("Open the saved HitchAndRun scene explicitly.");
            HitchRunMatch match = null;
            foreach (var root in scene.GetRootGameObjects())
                if ((match = root.GetComponentInChildren<HitchRunMatch>(true)) != null) break;
            if (match == null) throw new InvalidOperationException("Hitch Run match missing.");
            var font = SportsBroadcastAuthoring.Theme().headingFont;
            if (font == null) throw new InvalidOperationException("Existing broadcast heading font missing.");
            foreach (var trailer in match.trailers)
            {
                var badge = trailer.GetComponentInChildren<HitchRunOwnershipBadge>(true);
                if (badge == null)
                {
                    var go = new GameObject("Actual cargo owner · per-camera badge");
                    go.transform.SetParent(trailer.transform, false);
                    go.transform.localPosition = new Vector3(0, 5.8f, 0);
                    badge = go.AddComponent<HitchRunOwnershipBadge>();
                    var text = go.AddComponent<TextMeshPro>();
                    text.font = font;
                    text.fontSize = 6;
                    text.alignment = TextAlignmentOptions.Center;
                    text.enableAutoSizing = true;
                    text.fontSizeMin = 3;
                    text.fontSizeMax = 6;
                    text.richText = false;
                    text.rectTransform.sizeDelta = new Vector2(13, 2.8f);
                    text.text = "UNCLAIMED CARGO\nALIGN / HOLD ACTION";
                    text.color = new Color(.9f, .88f, .7f);
                    badge.Label = text;
                    // Reuse the collection's per-camera billboard behavior;
                    // each split viewport resolves orientation and pixel caps.
                    var billboard = go.AddComponent<SportsPerCameraBillboard>();
                    billboard.distanceSizing = true;
                    billboard.referenceDistance = 60;
                    billboard.maximumPixelHeight = 36;
                    billboard.maximumPixelWidth = 190;
                    billboard.maximumLabelDistance = 280;
                    var renderer = text.GetComponent<Renderer>();
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
                badge.Match = match;
                badge.Trailer = trailer;
                EditorUtility.SetDirty(badge);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
