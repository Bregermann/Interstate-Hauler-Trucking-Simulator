using System;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using Michsky.UI.Heat;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LWS.TruckSubmarine.Editor
{
    public static class SubmarineHudAuthoring
    {
        // Binds the existing Heat confirmation presenter. No new UI runtime or
        // text parser: participant cards and the arena board use typed mode data.
        public static void Install(SubmarineMatch match)
        {
            if (match == null || match.menus == null) throw new InvalidOperationException("Original sport menu must be authored first.");
            if (match.menus.GetComponentInChildren<SportsScorePresentation>(true) != null) return;
            var root = new GameObject("Submarine delivered points · existing Heat", typeof(RectTransform), typeof(CanvasGroup), typeof(SportsScorePresentation));
            root.layer = 5; root.transform.SetParent(match.menus.transform, false);
            var rect = root.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1); rect.anchoredPosition = new Vector2(0, -100); rect.sizeDelta = new Vector2(680, 106);
            var group = root.GetComponent<CanvasGroup>(); group.alpha = 0; group.interactable = group.blocksRaycasts = false;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Heat - Complete Modern UI/Prefabs/UI Elements/Notification/Notification.prefab");
            if (prefab == null) throw new InvalidOperationException("Existing Heat notification required.");
            var panel = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform); PrefabUtility.UnpackPrefabInstance(panel, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var panelRect = panel.GetComponent<RectTransform>(); panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one; panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
            foreach (var layout in panel.GetComponentsInChildren<LayoutGroup>(true)) layout.enabled = false;
            foreach (var fitter in panel.GetComponentsInChildren<ContentSizeFitter>(true)) fitter.enabled = false;
            foreach (var style in panel.GetComponentsInChildren<UIManagerImage>(true)) style.useCustomColor = true;
            foreach (var style in panel.GetComponentsInChildren<UIManagerText>(true)) { style.useCustomColor = true; style.useCustomFont = true; }
            foreach (var image in panel.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                image.raycastTarget = false;
                if (image.name == "Background") { var ink = SportsBroadcastCanvas.Ink; ink.a = 1; image.color = ink; image.sprite = null; image.gameObject.AddComponent<SportsBroadcastPlate>(); }
            }
            var notification = panel.GetComponent<NotificationManager>(); notification.useLocalization = false; notification.defaultState = NotificationManager.DefaultState.Minimized; notification.afterMinimize = NotificationManager.AfterMinimize.Disable; notification.icon = Resources.Load<Sprite>("Broadcast truck portrait"); notification.notificationText = "";
            var serialized = new SerializedObject(notification); var portrait = (UnityEngine.UI.Image)serialized.FindProperty("iconObj").objectReferenceValue; var message = (TextMeshProUGUI)serialized.FindProperty("textObj").objectReferenceValue;
            portrait.rectTransform.anchorMin = portrait.rectTransform.anchorMax = new Vector2(0, .5f); portrait.rectTransform.anchoredPosition = new Vector2(48, 0); portrait.rectTransform.sizeDelta = new Vector2(70, 70); portrait.preserveAspect = true;
            var text = message.rectTransform; text.anchorMin = Vector2.zero; text.anchorMax = Vector2.one; text.offsetMin = new Vector2(96, 10); text.offsetMax = new Vector2(-16, -10);
            message.font = Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme").headingFont; message.color = SportsBroadcastCanvas.Cream; message.enableAutoSizing = true; message.fontSizeMin = 19; message.fontSizeMax = 25; message.alignment = TextAlignmentOptions.MidlineLeft; message.raycastTarget = false;
            var filled = panel.transform.Find("Filled"); if (filled != null) filled.gameObject.SetActive(false);
            var stripe = new GameObject("Actual credited participant color", typeof(RectTransform), typeof(UnityEngine.UI.Image)); stripe.layer = 5; stripe.transform.SetParent(panel.transform.Find("Mask Content"), false);
            var line = stripe.GetComponent<RectTransform>(); line.anchorMin = Vector2.zero; line.anchorMax = new Vector2(1, 0); line.offsetMin = new Vector2(10, 2); line.offsetMax = new Vector2(-10, 6); var accent = stripe.GetComponent<UnityEngine.UI.Image>(); accent.raycastTarget = false;
            var presenter = root.GetComponent<SportsScorePresentation>(); presenter.session = match; presenter.visibility = group; presenter.notification = notification; presenter.portrait = portrait; presenter.accent = accent; presenter.notificationAnimator = (Animator)serialized.FindProperty("itemAnimator").objectReferenceValue;
        }
    }
}
