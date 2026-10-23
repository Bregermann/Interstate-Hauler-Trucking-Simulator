using System.Linq;
using Michsky.UI.Heat;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LWS.TruckSports.Editor
{
    public static class SportsScorePresentationAuthoring
    {
        static readonly string[] ArcadeScenes = { "TruckBalloonBattle", "TruckWhackAMole", "TruckTetherball", "TruckMagnetMayhem", "TruckSheepHerding" };
        // Deliberate additive installation. Normal Play Mode/builds never rebuild UI.
        public static void InstallSumo() => InstallScenes(new[] { "TruckSumo" });
        public static void InstallArcadeAwards() => InstallScenes(ArcadeScenes);
        public static void InstallSurvivalAwards() => InstallScenes(new[] { "TruckHotPotato", "TruckSeesaw", "TruckKingOfTheHill" });
        public static void InstallTeamAwards() => InstallScenes(new[] { "TruckVolleyball", "TruckFreezeTag" });
        public static void RefineArcadeAwardBackgrounds()
        {
            foreach (string name in ArcadeScenes)
            {
                var scene = EditorSceneManager.OpenScene("Assets/" + name + "/Scenes/" + name + ".unity", OpenSceneMode.Single);
                var presenter = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SportsScorePresentation>(true)).Single();
                foreach (var background in presenter.notification.GetComponentsInChildren<UnityEngine.UI.Image>(true).Where(image => image.name == "Background"))
                { var color = background.color; color.a = 1; background.color = color; EditorUtility.SetDirty(background); }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
        static void InstallScenes(string[] names)
        {
            foreach (string name in names)
            {
            var scene = EditorSceneManager.OpenScene("Assets/" + name + "/Scenes/" + name + ".unity", OpenSceneMode.Single);
            var session = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SportsEventSession>(true)).Single();
            if (session.menus.GetComponentInChildren<SportsScorePresentation>(true) != null) continue;
            var root = new GameObject("Confirmed scoring · shared Heat presentation", typeof(RectTransform), typeof(CanvasGroup), typeof(SportsScorePresentation));
            root.layer = 5; root.transform.SetParent(session.menus.transform, false);
            var rect = root.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1); rect.pivot = new Vector2(.5f, 1); rect.anchoredPosition = new Vector2(0, -112); rect.sizeDelta = new Vector2(760, 132);
            var group = root.GetComponent<CanvasGroup>(); group.alpha = 0; group.interactable = group.blocksRaycasts = false;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Heat - Complete Modern UI/Prefabs/UI Elements/Notification/Notification.prefab");
            var panel = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            PrefabUtility.UnpackPrefabInstance(panel, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            panel.name = "Sport-owned Heat score notification";
            var panelRect = panel.GetComponent<RectTransform>(); panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one; panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
            foreach (var layout in panel.GetComponentsInChildren<LayoutGroup>(true)) layout.enabled = false;
            foreach (var fitter in panel.GetComponentsInChildren<ContentSizeFitter>(true)) fitter.enabled = false;
            foreach (var image in panel.GetComponentsInChildren<UnityEngine.UI.Image>(true)) { image.raycastTarget = false; if (image.name == "Background") { var ink = SportsBroadcastCanvas.Ink; ink.a = 1; image.color = ink; image.sprite = null; image.gameObject.AddComponent<SportsBroadcastPlate>(); } }
            foreach (var style in panel.GetComponentsInChildren<UIManagerImage>(true)) style.useCustomColor = true;
            foreach (var style in panel.GetComponentsInChildren<UIManagerText>(true)) { style.useCustomColor = true; style.useCustomFont = true; }
            var notification = panel.GetComponent<NotificationManager>(); notification.useLocalization = false;
            notification.defaultState = NotificationManager.DefaultState.Minimized; notification.afterMinimize = NotificationManager.AfterMinimize.Disable;
            notification.icon = Resources.Load<Sprite>("Broadcast truck portrait"); notification.notificationText = "";
            var settings = new SerializedObject(notification);
            var portrait = (UnityEngine.UI.Image)settings.FindProperty("iconObj").objectReferenceValue;
            var message = (TextMeshProUGUI)settings.FindProperty("textObj").objectReferenceValue;
            var portraitRect = portrait.rectTransform; portraitRect.anchorMin = portraitRect.anchorMax = new Vector2(0, .5f); portraitRect.pivot = new Vector2(.5f, .5f); portraitRect.anchoredPosition = new Vector2(53, 0); portraitRect.sizeDelta = new Vector2(76, 76); portrait.preserveAspect = true;
            var textRect = message.rectTransform; textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.offsetMin = new Vector2(110, 12); textRect.offsetMax = new Vector2(-20, -12);
            var theme = Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme"); message.font = theme.headingFont; message.color = SportsBroadcastCanvas.Cream; message.enableAutoSizing = true; message.fontSizeMin = 20; message.fontSizeMax = 28; message.alignment = TextAlignmentOptions.MidlineLeft; message.raycastTarget = false;
            var filled = panel.transform.Find("Filled"); if (filled != null) filled.gameObject.SetActive(false);
            var stripe = new GameObject("Credited participant accent", typeof(RectTransform), typeof(UnityEngine.UI.Image)); stripe.layer = 5; stripe.transform.SetParent(panel.transform, false);
            var stripeRect = stripe.GetComponent<RectTransform>(); stripeRect.anchorMin = Vector2.zero; stripeRect.anchorMax = new Vector2(1, 0); stripeRect.pivot = new Vector2(.5f, 0); stripeRect.offsetMin = new Vector2(10, 2); stripeRect.offsetMax = new Vector2(-10, 6);
            var accent = stripe.GetComponent<UnityEngine.UI.Image>(); accent.raycastTarget = false;
            var presentation = root.GetComponent<SportsScorePresentation>(); presentation.session = session; presentation.notification = notification; presentation.visibility = group; presentation.portrait = portrait; presentation.accent = accent; presentation.notificationAnimator = (Animator)settings.FindProperty("itemAnimator").objectReferenceValue;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("SCORE_PRESENTATION: additive saved " + name + " Heat notification. Original gameplay, scorecards, replay and result flows retained.");
            }
        }
    }
}
