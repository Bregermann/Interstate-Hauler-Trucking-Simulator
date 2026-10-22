using System;
using System.Linq;
using LWS.TruckSports;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckAirHockey.Editor
{
    public static class AirHockeyPresentationAuthoring
    {
        static TMP_FontAsset font;
        static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 point)
        {
            var node = new GameObject(name, typeof(RectTransform)); node.transform.SetParent(parent, false); var r = node.GetComponent<RectTransform>();
            Place(r, size, anchor, point); return r;
        }
        static void Place(RectTransform r, Vector2 size, Vector2 anchor, Vector2 point)
        { r.anchorMin = r.anchorMax = r.pivot = anchor; r.sizeDelta = size; r.anchoredPosition = point; }
        static UnityEngine.UI.Image Image(RectTransform r, Color color, Sprite sprite = null)
        { var image = r.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.sprite = sprite; image.raycastTarget = false; return image; }
        static RectTransform Plate(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 point)
        { var r = Rect(name, parent, size, anchor, point); Image(r, SportsBroadcastCanvas.Ink); r.gameObject.AddComponent<SportsBroadcastPlate>().cut = 16; return r; }
        static TMP_Text Label(string name, Transform parent, string value, Vector2 size, Vector2 point, int points)
        {
            var r = Rect(name, parent, size, new Vector2(.5f, .5f), point); var text = r.gameObject.AddComponent<TextMeshProUGUI>(); text.text = value; Style(text, points); return text;
        }
        static void Style(TMP_Text text, int points)
        { text.font = font; text.fontSize = text.fontSizeMax = points; text.fontSizeMin = Mathf.Min(points, 20); text.enableAutoSizing = true; text.alignment = TextAlignmentOptions.Center; text.color = SportsBroadcastCanvas.Cream; text.raycastTarget = false; text.overflowMode = TextOverflowModes.Ellipsis; }
        static void Move(TMP_Text text, Transform parent, Vector2 size, Vector2 point, int points)
        { text.transform.SetParent(parent, false); Place(text.rectTransform, size, new Vector2(.5f, .5f), point); Style(text, points); }
        static void MoveControl(Transform control, Transform parent, Vector2 point)
        { control.SetParent(parent, false); Place((RectTransform)control, new Vector2(520, 56), new Vector2(.5f, .5f), point); }
        static bool RefineIntegration(AirHockeyMenus menus)
        {
            bool changed = false;
            var overlay = menus.hud.GetComponent<SportsArenaOverlay>();
            if (overlay != null && overlay.enabled) { overlay.enabled = false; EditorUtility.SetDirty(overlay); changed = true; }
            if (!menus.presentation.controls.gameObject.activeSelf) { menus.presentation.controls.gameObject.SetActive(true); changed = true; }
            var setup = menus.pages[(int)AirHockeyScreen.Setup].transform;
            var original = setup.Find("Original sport title artwork") as RectTransform; var duplicate = setup.Find("Existing original Air Hockey logo") as RectTransform;
            if (original != null && duplicate != null && duplicate.anchoredPosition == new Vector2(0, 355) && duplicate.sizeDelta == new Vector2(700, 140) && original.GetComponent<UnityEngine.UI.Image>().sprite == duplicate.GetComponent<UnityEngine.UI.Image>().sprite)
            {
                Place(original, new Vector2(700, 140), new Vector2(.5f, .5f), new Vector2(0, 355)); original.gameObject.SetActive(true);
                // This is solely our unshipped duplicate image, identified by
                // its exact initial name/layout/art. The original is retained.
                UnityEngine.Object.DestroyImmediate(duplicate.gameObject); changed = true;
            }
            if (changed) { menus.presentation.Refresh(); EditorUtility.SetDirty(menus.presentation); }
            return changed;
        }
        [MenuItem("Truck Sports/Air Hockey/Upgrade saved menus and HUD")]
        public static void Upgrade()
        {
            var scene = EditorSceneManager.OpenScene("Assets/TruckAirHockey/Scenes/TruckAirHockey.unity"); var menus = UnityEngine.Object.FindAnyObjectByType<AirHockeyMenus>();
            if (menus == null || menus.names.Length != 2) throw new InvalidOperationException("Existing two-player Air Hockey menus required.");
            if (menus.presentation != null)
            {
                bool changed = RefineIntegration(menus);
                var oldControls = menus.presentation.controls.rectTransform;
                if (oldControls.anchorMin == new Vector2(.5f, .5f) && oldControls.anchorMax == oldControls.anchorMin && oldControls.anchoredPosition == new Vector2(0, -512))
                {
                    Place(oldControls, new Vector2(1860, 32), new Vector2(.5f, 0), new Vector2(0, 14)); EditorUtility.SetDirty(oldControls);
                    changed = true;
                }
                if (changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                Debug.Log("AIR_HOCKEY_PRESENTATION_PRESERVED designer layout and references."); return;
            }
            var theme = AssetDatabase.LoadAssetAtPath<SportsBroadcastTheme>("Assets/TruckSports/Resources/SportsBroadcastTheme.asset"); if (theme == null || theme.headingFont == null) throw new InvalidOperationException("Existing Heat broadcast font required."); font = theme.headingFont;
            var portrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TruckSports/Resources/Broadcast truck portrait.png"); var logo = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TruckSports/Art/Logos/TruckAirHockey.png");
            if (portrait == null || logo == null) throw new InvalidOperationException("Existing original Air Hockey logo and shared truck portrait required.");
            var p = menus.gameObject.AddComponent<AirHockeyPresentation>(); p.menus = menus; menus.presentation = p;
            p.names = new TMP_Text[2]; p.points = new TMP_Text[2]; p.portraits = new UnityEngine.UI.Image[2]; p.setupPortraits = new UnityEngine.UI.Image[2]; p.setupAccents = new UnityEngine.UI.Image[2]; p.setupStatus = new TMP_Text[2];
            p.resultNames = new TMP_Text[2]; p.resultPoints = new TMP_Text[2]; p.resultRanks = new TMP_Text[2]; p.resultPortraits = new UnityEngine.UI.Image[2]; p.resultAccents = new UnityEngine.UI.Image[2]; p.pauseScores = new TMP_Text[2];
            var hud = (RectTransform)menus.hud.transform; SportsBroadcastCanvas.Stretch(hud);
            var controls = hud.GetComponentsInChildren<TMP_Text>(true).Single(t => t.name == "Controls");
            p.legacyDocks = new[] { menus.scoreText.transform.parent, menus.serveText.transform.parent, menus.noticeText.transform.parent, controls.transform.parent, menus.resultText.transform.parent }
                .Where(t => t != hud && t.name.EndsWith("Arena Dock", StringComparison.Ordinal)).Distinct().Select(t => t.gameObject).ToArray();
            var bug = p.scoreboard = Plate("Air Hockey score bug", hud, new Vector2(1040, 90), new Vector2(.5f, 1), new Vector2(0, -16));
            for (int i = 0; i < 2; i++)
            {
                float sign = i == 0 ? -1 : 1;
                p.portraits[i] = Image(Rect("Participant portrait " + i, bug, new Vector2(42, 40), new Vector2(.5f, .5f), new Vector2(sign * 481, 0)), Color.white, portrait); p.portraits[i].preserveAspect = true;
                p.names[i] = Label("Participant name " + i, bug, "", new Vector2(260, 50), new Vector2(sign * 326, 0), 30);
                p.points[i] = Label("Actual score " + i, bug, "0", new Vector2(90, 66), new Vector2(sign * 147, 0), 52);
            }
            p.clock = Label("Live play clock", bug, "PLAY 00:00", new Vector2(176, 32), new Vector2(0, 16), 24); Label("Target score", bug, "FIRST TO 7", new Vector2(160, 27), new Vector2(0, -18), 20);
            var serve = Plate("Serving broadcast card", hud, new Vector2(700, 40), new Vector2(.5f, 1), new Vector2(0, -112)); Move(menus.serveText, serve, new Vector2(670, 32), Vector2.zero, 24);
            var notice = Plate("Goal and match feedback", hud, new Vector2(1260, 50), new Vector2(.5f, 0), new Vector2(0, 58)); Move(menus.noticeText, notice, new Vector2(1220, 40), Vector2.zero, 25);
            p.controls = controls; Move(controls, hud, new Vector2(1860, 32), Vector2.zero, 21); Place(controls.rectTransform, new Vector2(1860, 32), new Vector2(.5f, 0), new Vector2(0, 14));
            foreach (var legacy in p.legacyDocks) legacy.SetActive(false); menus.scoreText.gameObject.SetActive(false); menus.resultText.gameObject.SetActive(false);

            var setup = menus.pages[(int)AirHockeyScreen.Setup].transform;
            var title = setup.Find("Title"); if (title != null) title.gameObject.SetActive(false);
            var originalLogo = setup.Find("Original sport title artwork") as RectTransform;
            if (originalLogo != null) { Place(originalLogo, new Vector2(700, 140), new Vector2(.5f, .5f), new Vector2(0, 355)); originalLogo.gameObject.SetActive(true); }
            else Image(Rect("Existing original Air Hockey logo", setup, new Vector2(700, 140), new Vector2(.5f, .5f), new Vector2(0, 355)), Color.white, logo).preserveAspect = true;
            var subtitle = setup.Find("Subtitle") as RectTransform; if (subtitle != null) Place(subtitle, new Vector2(1300, 28), new Vector2(.5f, .5f), new Vector2(0, 265));
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -330 : 330;
                var card = Plate("Driver setup card " + (i + 1), setup, new Vector2(600, 460), new Vector2(.5f, .5f), new Vector2(x, -60)); card.SetSiblingIndex(1);
                p.setupAccents[i] = Image(Rect("Driver identity color", card, new Vector2(4, 414), new Vector2(.5f, .5f), new Vector2(-297, 0)), Color.cyan);
                p.setupPortraits[i] = Image(Rect("Driver truck portrait", card, new Vector2(48, 44), new Vector2(.5f, .5f), new Vector2(-236, 184)), Color.white, portrait); p.setupPortraits[i].preserveAspect = true;
                Label("Driver heading", card, "DRIVER " + (i + 1), new Vector2(350, 42), new Vector2(0, 184), 25);
                var oldLabel = setup.Find("PARTICIPANT " + (i + 1)); if (oldLabel != null) oldLabel.gameObject.SetActive(false);
                MoveControl(menus.names[i].transform, card, new Vector2(0, 105));
                MoveControl(menus.sourceLabels[i].transform.parent, card, new Vector2(0, 27));
                MoveControl(menus.levelLabels[i].transform.parent, card, new Vector2(0, -51));
                MoveControl(menus.colorLabels[i].transform.parent, card, new Vector2(0, -129));
                p.setupStatus[i] = Label("Assigned input availability", card, "", new Vector2(552, 45), new Vector2(0, -199), 20);
                var outcome = Plate("Participant result card " + (i + 1), menus.pages[(int)AirHockeyScreen.Result].transform, new Vector2(580, 190), new Vector2(.5f, .5f), new Vector2(x, 120));
                p.resultAccents[i] = Image(Rect("Result identity color", outcome, new Vector2(4, 150), new Vector2(.5f, .5f), new Vector2(-287, 0)), Color.cyan);
                p.resultPortraits[i] = Image(Rect("Result truck portrait", outcome, new Vector2(46, 42), new Vector2(.5f, .5f), new Vector2(-238, 0)), Color.white, portrait); p.resultPortraits[i].preserveAspect = true;
                p.resultRanks[i] = Label("Result placing", outcome, "", new Vector2(500, 36), new Vector2(0, 56), 24);
                p.resultNames[i] = Label("Result participant", outcome, "", new Vector2(300, 45), new Vector2(-20, -2), 29);
                p.resultPoints[i] = Label("Result score", outcome, "0", new Vector2(90, 65), new Vector2(225, -2), 52);
                var paused = Plate("Paused score card " + (i + 1), menus.pages[(int)AirHockeyScreen.Pause].transform, new Vector2(560, 60), new Vector2(.5f, .5f), new Vector2(i == 0 ? -300 : 300, 250));
                p.pauseScores[i] = Label("Preserved paused score", paused, "", new Vector2(520, 45), Vector2.zero, 28);
            }
            var pause = menus.pages[(int)AirHockeyScreen.Pause].transform;
            Place((RectTransform)pause.Find("Title"), new Vector2(1300, 64), new Vector2(.5f, .5f), new Vector2(0, 375));
            Place((RectTransform)pause.Find("Subtitle"), new Vector2(1300, 36), new Vector2(.5f, .5f), new Vector2(0, 315));
            RefineIntegration(menus); p.Refresh(); EditorUtility.SetDirty(menus); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("AIR_HOCKEY_PRESENTATION_AUTHORED owned logo, two editable driver cards, live score/play clock, server/goal feedback, paused scores and graphical results; original controls, buttons, arena and physics preserved.");
        }
    }
}
