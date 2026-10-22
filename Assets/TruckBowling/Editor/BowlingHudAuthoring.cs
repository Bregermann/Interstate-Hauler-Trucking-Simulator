using System;
using LWS.TruckSports;
using Michsky.UI.Heat;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckBowling.Editor
{
    public static class BowlingHudAuthoring
    {
        static TMP_FontAsset font;
        static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 point, Vector2 anchor)
        {
            var node = new GameObject(name, typeof(RectTransform)); node.transform.SetParent(parent, false); var r = node.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = anchor; r.sizeDelta = size; r.anchoredPosition = point; return r;
        }
        static UnityEngine.UI.Image Image(RectTransform rect, Color color, Sprite sprite = null)
        { var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.sprite = sprite; image.raycastTarget = false; return image; }
        static TMP_Text Label(string name, Transform parent, Vector2 size, Vector2 point, int points, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            var rect = Rect(name, parent, size, point, new Vector2(0, 1)); var text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = text.fontSizeMax = points; text.fontSizeMin = Mathf.Min(points, 18); text.enableAutoSizing = true;
            text.alignment = alignment; text.color = SportsBroadcastCanvas.Cream; text.raycastTarget = false; text.overflowMode = TextOverflowModes.Ellipsis; text.text = ""; return text;
        }
        static void Rounded(RectTransform rect)
        { rect.GetComponent<UnityEngine.UI.Image>().color = SportsBroadcastCanvas.Ink; var plate = rect.GetComponent<SportsBroadcastPlate>() ?? rect.gameObject.AddComponent<SportsBroadcastPlate>(); plate.cut = 12; }
        [MenuItem("Truck Sports/Bowling/Upgrade saved HUD only")]
        public static void Upgrade()
        {
            EditorSceneManager.OpenScene("Assets/TruckBowling/Scenes/TruckBowling.unity"); var menus = UnityEngine.Object.FindAnyObjectByType<BowlingMenus>();
            foreach (var label in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Debug.Log("BOWLING_WORLD_LABEL_RETAINED " + label.name + " text=" + label.text);
            if (menus == null || menus.viewRoots.Length != 4) throw new InvalidOperationException("Existing four-lane Bowling menus required.");
            if (menus.presentation != null) { Debug.Log("BOWLING_HUD_PRESERVED existing designer selections."); return; }
            var theme = AssetDatabase.LoadAssetAtPath<SportsBroadcastTheme>("Assets/TruckSports/Resources/SportsBroadcastTheme.asset"); if (theme == null || theme.headingFont == null) throw new InvalidOperationException("Existing Heat broadcast font required."); font = theme.headingFont;
            var portrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TruckSports/Resources/Broadcast truck portrait.png"); var fillSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); if (portrait == null || fillSprite == null) throw new InvalidOperationException("Existing HUD image assets required.");
            var ui = menus.gameObject.AddComponent<BowlingHudPresentation>(); menus.presentation = ui; ui.menus = menus;
            ui.portraits = new UnityEngine.UI.Image[4]; ui.accents = new UnityEngine.UI.Image[4]; ui.totals = new TMP_Text[4]; ui.frames = new TMP_Text[4]; ui.states = new TMP_Text[4]; ui.progress = new ProgressBar[4];
            for (int i = 0; i < 4; i++)
            {
                var header = menus.headers[i].rectTransform.parent as RectTransform;
                if (header == null || header.name != "Header" || header.GetComponent<UnityEngine.UI.Image>() == null) throw new InvalidOperationException("Preserve customized header hierarchy; expected original saved lane header.");
                header.anchorMin = header.anchorMax = header.pivot = new Vector2(0, 1); header.anchoredPosition = new Vector2(12, -12); header.sizeDelta = new Vector2(520, 112); Rounded(header);
                var name = menus.headers[i]; name.font = font; name.fontSize = name.fontSizeMax = 26; name.fontSizeMin = 20; name.enableAutoSizing = true; name.alignment = TextAlignmentOptions.MidlineLeft; name.overflowMode = TextOverflowModes.Ellipsis;
                var r = name.rectTransform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(72, -9); r.sizeDelta = new Vector2(326, 34);
                ui.portraits[i] = Image(Rect("Assigned truck portrait", header, new Vector2(42, 37), new Vector2(17, -8), new Vector2(0, 1)), Color.white, portrait); ui.portraits[i].preserveAspect = true;
                ui.accents[i] = Image(Rect("Participant color", header, new Vector2(4, 82), new Vector2(0, -15), new Vector2(0, 1)), Color.cyan);
                ui.totals[i] = Label("Resolved total", header, new Vector2(94, 42), new Vector2(410, -5), 36, TextAlignmentOptions.MidlineRight);
                Label("Score meaning", header, new Vector2(94, 22), new Vector2(410, -45), 16, TextAlignmentOptions.MidlineRight).text = "RESOLVED";
                ui.frames[i] = Label("Frame and delivery", header, new Vector2(362, 27), new Vector2(18, -48), 21);
                ui.states[i] = Label("Lane rack state", header, new Vector2(480, 25), new Vector2(18, -77), 19);
                var track = Rect("Ten-frame progress", header, new Vector2(484, 4), new Vector2(18, -105), new Vector2(0, 1)); Image(track, new Color(.18f, .22f, .27f));
                var fill = Rect("Frame fill", track, Vector2.zero, Vector2.zero, new Vector2(.5f, .5f)); fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one;
                var image = Image(fill, Color.cyan, fillSprite); image.type = UnityEngine.UI.Image.Type.Filled; image.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; image.fillOrigin = 0;
                var bar = ui.progress[i] = track.gameObject.AddComponent<ProgressBar>(); bar.barImage = image; bar.maxValue = 100; bar.onValueChanged = new ProgressBar.OnValueChanged();
                var footer = menus.guidance[i].rectTransform.parent as RectTransform; Rounded(footer); footer.anchorMin = Vector2.zero; footer.anchorMax = new Vector2(1, 0); footer.pivot = new Vector2(.5f, 0); footer.offsetMin = new Vector2(12, 12); footer.offsetMax = new Vector2(-12, 68);
                var guidance = menus.guidance[i]; guidance.font = font; guidance.fontSize = guidance.fontSizeMax = 21; guidance.fontSizeMin = 18; guidance.enableAutoSizing = true;
                var popup = menus.popups[i].rectTransform.parent as RectTransform; Rounded(popup);
            }
            EditorUtility.SetDirty(menus); EditorSceneManager.MarkSceneDirty(menus.gameObject.scene); EditorSceneManager.SaveScene(menus.gameObject.scene); AssetDatabase.SaveAssets();
            Debug.Log("BOWLING_HUD_AUTHORED existing lane HUDs only: portrait/color, resolved total, frame/delivery/rack, Heat progress and compact controls. All alley, truck, pinsetter, camera, physics and scoring objects preserved.");
        }
    }
}
