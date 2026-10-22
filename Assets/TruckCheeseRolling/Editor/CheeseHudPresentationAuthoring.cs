using System;
using LWS.TruckSports;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckCheeseRolling.Editor
{
    public static class CheeseHudPresentationAuthoring
    {
        static TMP_FontAsset font; static readonly Vector2 Center = new Vector2(.5f, .5f), TopLeft = new Vector2(0, 1);
        static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 point)
        { var r = (RectTransform)new GameObject(name, typeof(RectTransform)).transform; r.SetParent(parent, false); Place(r, size, anchor, point); return r; }
        static void Place(RectTransform r, Vector2 size, Vector2 anchor, Vector2 point) { r.anchorMin = r.anchorMax = r.pivot = anchor; r.sizeDelta = size; r.anchoredPosition = point; }
        static UnityEngine.UI.Image Image(RectTransform r, Color color, Sprite sprite = null) { var image = r.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.sprite = sprite; image.preserveAspect = sprite != null; image.raycastTarget = false; return image; }
        static RectTransform Plate(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 point) { var r = Rect(name, parent, size, anchor, point); Image(r, SportsBroadcastCanvas.Ink); r.gameObject.AddComponent<SportsBroadcastPlate>().cut = 12; return r; }
        static TMP_Text Text(string name, Transform parent, Vector2 size, Vector2 point, int points)
        { var r = Rect(name, parent, size, Center, point); var text = r.gameObject.AddComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = text.fontSizeMax = points; text.fontSizeMin = points - 3; text.enableAutoSizing = true; text.alignment = TextAlignmentOptions.Center; text.color = SportsBroadcastCanvas.Cream; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis; return text; }
        static void PromptBackings(CheeseHudPresentation saved)
        {
            foreach (var view in saved.views)
            {
                var prompt = view.prompt.rectTransform; var backing = Plate("Menu source contrast backing", prompt.parent, prompt.sizeDelta + new Vector2(12, 6), new Vector2(1, 0), prompt.anchoredPosition + new Vector2(6, -3));
                backing.SetSiblingIndex(prompt.GetSiblingIndex());
            }
        }
        [MenuItem("Truck Sports/Cheese Rolling/Upgrade saved race HUD")]
        public static void Upgrade()
        {
            var scene = EditorSceneManager.OpenScene(CheeseAuthoring.Scene); var m = UnityEngine.Object.FindAnyObjectByType<CheeseMatch>(); var menus = m.menus;
            if (menus.presentation != null)
            {
                var saved = menus.presentation;
                if (saved.authoredVersion < 1)
                {
                    foreach (var view in saved.views) if ((view.prompt.rectTransform.anchoredPosition - new Vector2(-12, 12)).sqrMagnitude < .01f) view.prompt.rectTransform.anchoredPosition = new Vector2(-12, 36);
                    if ((saved.resultSummary.rectTransform.anchoredPosition - new Vector2(0, 266)).sqrMagnitude < .01f) saved.resultSummary.rectTransform.anchoredPosition = new Vector2(0, 246);
                }
                if (saved.authoredVersion < 2) { PromptBackings(saved); saved.authoredVersion = 2; EditorUtility.SetDirty(saved); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                Debug.Log("CHEESE_HUD_PRESERVED saved designer layout, original countdown and tracking."); return;
            }
            var theme = AssetDatabase.LoadAssetAtPath<SportsBroadcastTheme>("Assets/TruckSports/Resources/SportsBroadcastTheme.asset"); font = theme != null ? theme.headingFont : null; var portrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TruckSports/Resources/Broadcast truck portrait.png");
            if (font == null || portrait == null) throw new InvalidOperationException("Existing Heat broadcast font and original truck portrait required.");
            var p = menus.gameObject.AddComponent<CheeseHudPresentation>(); p.authoredVersion = 1; menus.presentation = p; p.menus = menus; p.views = new CheeseHudView[4];
            p.resultCards = new GameObject[4]; p.resultNames = new TMP_Text[4]; p.resultScores = new TMP_Text[4]; p.resultPlaces = new TMP_Text[4]; p.resultPortraits = new UnityEngine.UI.Image[4]; p.resultAccents = new UnityEngine.UI.Image[4];
            for (int i = 0; i < 4; i++)
            {
                var parent = menus.views[i]; var view = new CheeseHudView(); p.views[i] = view; menus.headers[i].transform.parent.gameObject.SetActive(false);
                view.card = Plate("Owned racer identity and progress", parent, new Vector2(430, 106), TopLeft, new Vector2(12, -12));
                view.accent = Image(Rect("Crew color", view.card, new Vector2(4, 82), Center, new Vector2(-213, 0)), Color.white);
                view.portrait = Image(Rect("Original truck portrait", view.card, new Vector2(44, 44), Center, new Vector2(-176, 10)), Color.white, portrait);
                view.name = Text("Racer name", view.card, new Vector2(260, 36), new Vector2(-14, 26), 29); view.catches = Text("Authoritative catches", view.card, new Vector2(82, 48), new Vector2(166, 12), 38);
                view.rank = Text("Distance chase position", view.card, new Vector2(130, 28), new Vector2(-114, -22), 22); view.speed = Text("Actual truck MPH", view.card, new Vector2(130, 28), new Vector2(10, -22), 22); view.clock = Text("Round and race timer", view.card, new Vector2(126, 28), new Vector2(140, -22), 22);
                Image(Rect("Catch progress track", view.card, new Vector2(392, 3), Center, new Vector2(0, -47)), SportsBroadcastCanvas.Raised);
                view.progress = Image(Rect("Five catch progress", view.card, new Vector2(392, 3), Center, new Vector2(0, -47)), Color.white); view.progress.type = UnityEngine.UI.Image.Type.Filled; view.progress.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; view.progress.fillOrigin = 0;
                var opponents = Rect("Other scored racers", parent, new Vector2(440, 62), new Vector2(1, 1), new Vector2(-12, -12)); view.opponents = new CheeseOpponentCard[3];
                for (int n = 0; n < 3; n++) { var card = Plate("Other participant " + n, opponents, new Vector2(136, 62), TopLeft, new Vector2(n * 144, 0)); view.opponents[n] = new CheeseOpponentCard { root = card.gameObject, name = Text("Other crew name", card, new Vector2(116, 27), new Vector2(0, 16), 20), catches = Text("Other catch total", card, new Vector2(112, 29), new Vector2(0, -15), 24), accent = Image(Rect("Other crew color", card, new Vector2(3, 46), Center, new Vector2(-66, 0)), Color.white) }; }
                var oldStatus = menus.footers[i].transform.parent.gameObject; var notice = Plate("Owned race feedback", parent, new Vector2(620, 42), Vector2.zero, new Vector2(12, 12)); menus.footers[i].transform.SetParent(notice, false); Place(menus.footers[i].rectTransform, new Vector2(590, 32), Center, Vector2.zero); menus.footers[i].font = font; menus.footers[i].fontSizeMax = 22; menus.footers[i].fontSizeMin = 19; oldStatus.SetActive(false);
                view.prompt = Text("Actual menu source prompt", parent, new Vector2(246, 30), Vector2.zero, 20); Place(view.prompt.rectTransform, new Vector2(246, 30), new Vector2(1, 0), new Vector2(-12, 36));
                // Existing projection/distance/outlined TMP material remain the
                // authority. A sibling backing follows that exact label pose.
                view.markerBack = Plate("High contrast cheese marker backing", parent, new Vector2(190, 40), Center, Vector2.zero); view.markerBack.SetSiblingIndex(menus.indicators[i].transform.GetSiblingIndex());
                menus.indicators[i].color = SportsBroadcastCanvas.Cream;
                float x = i % 2 == 0 ? -300 : 300, y = i < 2 ? 145 : -40;
                var result = Plate("Racer result " + (i + 1), menus.pages[6].transform, new Vector2(560, 160), Center, new Vector2(x, y)); p.resultCards[i] = result.gameObject;
                p.resultAccents[i] = Image(Rect("Result crew color", result, new Vector2(4, 124), Center, new Vector2(-278, 0)), Color.white); p.resultPortraits[i] = Image(Rect("Result truck portrait", result, new Vector2(48, 46), Center, new Vector2(-228, 0)), Color.white, portrait);
                p.resultPlaces[i] = Text("Result place", result, new Vector2(490, 32), new Vector2(0, 48), 25); p.resultNames[i] = Text("Result crew name", result, new Vector2(300, 40), new Vector2(-20, -5), 29); p.resultScores[i] = Text("Final catches", result, new Vector2(120, 50), new Vector2(205, -5), 38);
            }
            menus.resultText.gameObject.SetActive(false); p.resultSummary = Text("Actual match result and elapsed", menus.pages[6].transform, new Vector2(1300, 42), new Vector2(0, 246), 29); PromptBackings(p); p.authoredVersion = 2;
            EditorUtility.SetDirty(menus); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); Debug.Log("CHEESE_RACE_HUD_AUTHORED owned portraits/colors/catches, read-only chase order/MPH/timer, original feedback/countdown/boost/tracking, contrast backings and graphical results; no race or camera changes.");
        }
    }
}
