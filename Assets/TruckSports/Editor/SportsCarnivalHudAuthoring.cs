using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

namespace LWS.TruckSports.Editor
{
    public static class SportsCarnivalHudAuthoring
    {
        // This is an explicit saved-scene migration. No sceneLoaded or runtime fallback installs it.
        public static readonly string[] Scenes =
        {
            "Assets/TruckBaseJumping/Scenes/TruckBaseJumping.unity",
            "Assets/TruckMattressRacing/Scenes/TruckMattressRacing.unity",
            "Assets/TruckWifeCarrying/Scenes/TruckWifeCarrying.unity",
            "Assets/TruckBogSnorkelling/Scenes/TruckBogSnorkelling.unity",
            "Assets/TruckTerrierRacing/Scenes/TruckTerrierRacing.unity",
            "Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity",
            "Assets/TruckRedLightGreenLight/Scenes/TruckRedLightGreenLight.unity"
        };
        [MenuItem("Truck Sports/Carnival/Install seven explicit race and Base HUDs")]
        public static void Install()
        {
            foreach (string path in Scenes) InstallScene(path);
            Debug.Log("CARNIVAL_HUD_AUTHORED seven explicit saved presenters; Heat/TMP/portraits/DOTween; six real simultaneous races plus Base score table; protected multiscreen HUDs untouched");
        }
        public static void InstallScene(string path)
        {
            if (!Scenes.Contains(path)) throw new ArgumentException("This author explicitly supports seven scenes only; Plowing and specialized HUDs are excluded.", nameof(path));
            var scene = EditorSceneManager.OpenScene(path);
            var match = UnityEngine.Object.FindAnyObjectByType<SportsEventSession>();
            if (match == null || match.menus == null) throw new InvalidOperationException("Saved sport/menu references are required: " + path);
            if (match.menus.GetComponent<SportsCarnivalLeaderboard>() != null) { Validate(match); return; }
            var theme = Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme");
            var portrait = Resources.Load<Sprite>("Broadcast truck portrait");
            if (theme == null || theme.headingFont == null || theme.bodyFont == null || portrait == null) throw new InvalidOperationException("Use the existing authored Heat broadcast fonts and truck portrait.");
            // Preserve the original authored text/image/layout data, particularly approved Base/Mattress split HUDs.
            var graphics = match.menus.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
            var preserved = graphics.ToDictionary(graphic => graphic, graphic => EditorJsonUtility.ToJson(graphic));
            var source = match.menus.gameObject.AddComponent<SportsCarnivalHudSource>(); source.match = match;
            if (!source.Supported) throw new InvalidOperationException("No typed Carnival adapter for " + match.GetType().Name);
            var policy = match.menus.gameObject.AddComponent<SportsCarnivalHudPolicy>(); policy.source = source;
            var legacy = new List<UnityEngine.UI.Graphic>();
            legacy.AddRange(match.menus.header.transform.parent.GetComponentsInChildren<UnityEngine.UI.Graphic>(true));
            legacy.Add(match.menus.status); legacy.Add(match.menus.standings); legacy.AddRange(match.menus.viewTexts);
            policy.legacyGraphics = legacy.Distinct().ToArray();
            var hud = match.menus.gameObject.AddComponent<SportsCarnivalLeaderboard>(); hud.source = source; hud.policy = policy;
            hud.root = Rect(match.menus.hud.transform, "Carnival saved single-view standings", Vector2.zero);
            SportsBroadcastCanvas.Stretch(hud.root);
            var group = hud.root.gameObject.AddComponent<CanvasGroup>(); group.interactable = group.blocksRaycasts = false;
            hud.table = Rect(hud.root, "All scored competitors in actual order", Vector2.zero);
            hud.contextRoot = Plate(hud.root, "Essential race state", new Vector2(760, 40));
            hud.contextRoot.anchorMin = hud.contextRoot.anchorMax = new Vector2(.5f, 1); hud.contextRoot.pivot = new Vector2(.5f, 1); hud.contextRoot.anchoredPosition = new Vector2(0, -12);
            hud.context = Text(hud.contextRoot, "Actual clock phase and warning", theme.headingFont, 22);
            SportsBroadcastCanvas.Stretch(hud.context.rectTransform, 10, 3); hud.context.alignment = TextAlignmentOptions.Center;
            int count = match is LWS.TruckWifeCarrying.WifeMatch ? 4 : match.MaximumPlayers;
            hud.cards = new SportsCarnivalLeaderboard.Card[count];
            for (int slot = 0; slot < count; slot++)
            {
                var card = new SportsCarnivalLeaderboard.Card { root = Plate(hud.table, "Stable competitor " + slot, new Vector2(180, 104)) };
                card.name = Text(card.root, "Actual competitor name", theme.headingFont, 20);
                card.place = Text(card.root, "Authoritative place", theme.headingFont, 22);
                card.value = Text(card.root, "Actual points or race time", theme.headingFont, 23);
                card.detail = Text(card.root, "Actual score progress and state", theme.bodyFont, 17);
                card.portrait = Image(card.root, "Existing broadcast truck portrait", Vector2.one * 32); card.portrait.sprite = portrait; card.portrait.preserveAspect = true;
                card.stripe = Image(card.root, "Actual match color", new Vector2(4, 92));
                card.progress = Image(card.root, "Accepted course progress", new Vector2(140, 3));
                hud.cards[slot] = card;
            }
            hud.root.gameObject.SetActive(false);
            foreach (var pair in preserved) if (EditorJsonUtility.ToJson(pair.Key) != pair.Value) throw new InvalidOperationException("Existing protected graphic changed: " + pair.Key.name);
            EditorUtility.SetDirty(source); EditorUtility.SetDirty(policy); EditorUtility.SetDirty(hud);
            Validate(match); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("CARNIVAL_SAVED_HUD " + path + " competitors=" + count + "; original graphics preserved; runtime cleanup single full-screen only");
        }
        public static void Validate(SportsEventSession match)
        {
            var hud = match.menus.GetComponent<SportsCarnivalLeaderboard>();
            int count = match is LWS.TruckWifeCarrying.WifeMatch ? 4 : match.MaximumPlayers;
            if (hud == null || hud.source == null || !hud.source.Supported || hud.source.match != match || hud.policy == null || hud.root == null || hud.table == null || hud.context == null || hud.cards == null || hud.cards.Length != count) throw new InvalidOperationException("Incomplete explicit Carnival HUD: " + match.Title);
            foreach (var card in hud.cards) if (card.root == null || card.name == null || card.place == null || card.value == null || card.detail == null || card.stripe == null || card.portrait == null || card.portrait.sprite == null || card.progress == null) throw new InvalidOperationException("Incomplete saved competitor card: " + match.Title);
        }
        static RectTransform Rect(Transform parent, string name, Vector2 size) => SportsUIAuthoring.Rect(name, parent, size, Vector2.zero).GetComponent<RectTransform>();
        static RectTransform Plate(Transform parent, string name, Vector2 size)
        {
            var rect = Rect(parent, name, size); var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = SportsBroadcastCanvas.Ink; image.raycastTarget = false; rect.gameObject.AddComponent<SportsBroadcastPlate>().cut = 7; return rect;
        }
        static UnityEngine.UI.Image Image(Transform parent, string name, Vector2 size)
        {
            var image = Rect(parent, name, size).gameObject.AddComponent<UnityEngine.UI.Image>(); image.raycastTarget = false; image.color = SportsBroadcastCanvas.Cream; return image;
        }
        static TMP_Text Text(Transform parent, string name, TMP_FontAsset font, int size)
        {
            var text = SportsUIAuthoring.Text(name, parent, "", new Vector2(150, 30), Vector2.zero, size);
            text.font = font; text.richText = false; text.fontSizeMax = size; text.fontSizeMin = size - 2;
            text.alignment = TextAlignmentOptions.MidlineLeft; text.overflowMode = TextOverflowModes.Ellipsis; text.raycastTarget = false; return text;
        }
    }
}
