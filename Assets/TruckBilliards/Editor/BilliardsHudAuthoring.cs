using System;
using System.Linq;
using LWS.TruckSports;
using Michsky.UI.Heat;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckBilliards.Editor
{
    public static class BilliardsHudAuthoring
    {
        static TMP_FontAsset font;
        static Sprite circle;
        static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position, Vector2 anchor)
        {
            var node = new GameObject(name, typeof(RectTransform)); node.transform.SetParent(parent, false);
            var rect = node.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
        }
        static UnityEngine.UI.Image Image(RectTransform rect, Color color, Sprite sprite = null)
        {
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.sprite = sprite; image.raycastTarget = false; return image;
        }
        static RectTransform Plate(string name, Transform parent, Vector2 size, Vector2 position, Vector2 anchor)
        {
            var rect = Rect(name, parent, size, position, anchor); Image(rect, SportsBroadcastCanvas.Ink);
            rect.gameObject.AddComponent<SportsBroadcastPlate>().cut = 14; return rect;
        }
        static TMP_Text Text(string name, Transform parent, Vector2 size, Vector2 position, int points, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            var rect = Rect(name, parent, size, position, new Vector2(0, 1));
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>(); label.font = font; label.fontSize = label.fontSizeMax = points; label.fontSizeMin = Mathf.Min(points, 18); label.enableAutoSizing = true;
            label.color = SportsBroadcastCanvas.Cream; label.alignment = alignment; label.overflowMode = TextOverflowModes.Ellipsis; label.raycastTarget = false; label.text = ""; return label;
        }
        [MenuItem("Truck Sports/Billiards/Upgrade saved gameplay HUD")]
        public static void Upgrade()
        {
            EditorSceneManager.OpenScene(BilliardsBuilder.ScenePath);
            var hud = UnityEngine.Object.FindAnyObjectByType<BilliardsHUD>();
            if (hud == null) throw new InvalidOperationException("Existing Billiards HUD is missing.");
            if (hud.presentation != null)
            {
                // Upgrade only the known first-authoring null sprite. All saved
                // layout, text and designer sprite assignments remain intact.
                var savedFill = hud.presentation.power.barImage;
                if (savedFill.sprite == null)
                {
                    savedFill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); EditorUtility.SetDirty(savedFill);
                    EditorSceneManager.MarkSceneDirty(hud.gameObject.scene); EditorSceneManager.SaveScene(hud.gameObject.scene);
                }
                Debug.Log("BILLIARDS_HUD_PRESERVED existing designer presentation."); return;
            }
            if (hud.transform.Find("Billiards broadcast HUD") != null) throw new InvalidOperationException("Unassigned owned HUD exists; preserve and review its references.");
            var theme = AssetDatabase.LoadAssetAtPath<SportsBroadcastTheme>("Assets/TruckSports/Resources/SportsBroadcastTheme.asset");
            if (theme == null || theme.headingFont == null) throw new InvalidOperationException("Existing Heat broadcast theme is required.");
            font = theme.headingFont; circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            if (circle == null) throw new InvalidOperationException("Unity round UI sprite is unavailable.");
            var root = Rect("Billiards broadcast HUD", hud.transform, Vector2.zero, Vector2.zero, new Vector2(.5f, .5f));
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
            var ui = root.gameObject.AddComponent<BilliardsHudPresentation>(); ui.hud = hud; ui.content = root.gameObject; hud.presentation = ui;
            ui.cards = new RectTransform[2]; ui.accents = new UnityEngine.UI.Image[2]; ui.portraits = new UnityEngine.UI.Image[2]; ui.turns = new UnityEngine.UI.Image[2];
            ui.names = new TMP_Text[2]; ui.scores = new TMP_Text[2]; ui.groups = new TMP_Text[2]; ui.remaining = new TMP_Text[2];
            ui.ballFaces = new UnityEngine.UI.Image[30]; ui.ballNumbers = new TMP_Text[30];
            for (int side = 0; side < 2; side++)
            {
                var card = ui.cards[side] = Plate("Participant " + (side + 1), root, new Vector2(590, 130), new Vector2(side == 0 ? 20 : -20, -16), new Vector2(side, 1));
                ui.accents[side] = Image(Rect("Participant color", card, new Vector2(4, 96), new Vector2(0, -17), new Vector2(0, 1)), Color.cyan);
                ui.portraits[side] = Image(Rect("Truck portrait", card, new Vector2(44, 38), new Vector2(16, -13), new Vector2(0, 1)), Color.white, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TruckSports/Resources/Broadcast truck portrait.png"));
                ui.portraits[side].preserveAspect = true;
                ui.names[side] = Text("Participant name", card, new Vector2(412, 35), new Vector2(72, -10), 29);
                ui.scores[side] = Text("Rack wins", card, new Vector2(84, 35), new Vector2(492, -10), 32, TextAlignmentOptions.MidlineRight);
                ui.groups[side] = Text("Ball objective", card, new Vector2(258, 28), new Vector2(18, -48), 20);
                ui.remaining[side] = Text("Remaining balls", card, new Vector2(285, 28), new Vector2(288, -48), 20, TextAlignmentOptions.MidlineRight);
                ui.turns[side] = Image(Rect("Turn marker", card, new Vector2(556, 3), new Vector2(17, -122), new Vector2(0, 1)), Color.cyan);
                for (int slot = 0; slot < 15; slot++)
                {
                    int index = side * 15 + slot;
                    var ball = Rect("Objective ball " + (slot + 1), card, new Vector2(30, 30), new Vector2(18 + slot * 36, -82), new Vector2(0, 1));
                    Image(ball, Color.white, circle); ball.gameObject.AddComponent<CanvasGroup>();
                    ui.ballFaces[index] = Image(Rect("Ball color", ball, new Vector2(30, 30), Vector2.zero, new Vector2(.5f, .5f)), Color.yellow, circle);
                    Image(Rect("Number disc", ball, new Vector2(21, 21), Vector2.zero, new Vector2(.5f, .5f)), Color.white, circle);
                    var number = ui.ballNumbers[index] = Text("Ball number", ball, new Vector2(26, 26), new Vector2(2, -2), 18, TextAlignmentOptions.Center); number.color = Color.black;
                }
            }
            var rack = Plate("Rack broadcast card", root, new Vector2(600, 50), new Vector2(0, -16), new Vector2(.5f, 1));
            ui.rack = Text("Series progress", rack, new Vector2(568, 40), new Vector2(16, -5), 26, TextAlignmentOptions.Center);
            var callout = Plate("Current shot card", root, new Vector2(1215, 74), new Vector2(20, 54), Vector2.zero);
            ui.turn = Text("Current turn", callout, new Vector2(1180, 30), new Vector2(18, -5), 25);
            ui.message = Text("Shot outcome", callout, new Vector2(1180, 31), new Vector2(18, -36), 22);
            var power = Plate("Shot power card", root, new Vector2(645, 74), new Vector2(-20, 54), new Vector2(1, 0));
            ui.powerText = Text("Power state", power, new Vector2(430, 28), new Vector2(18, -7), 22);
            ui.speedText = Text("Truck speed", power, new Vector2(180, 28), new Vector2(446, -7), 24, TextAlignmentOptions.MidlineRight);
            var track = Rect("Power track", power, new Vector2(609, 13), new Vector2(18, -45), new Vector2(0, 1)); Image(track, new Color(.18f, .22f, .27f));
            var fill = Rect("Power fill", track, Vector2.zero, Vector2.zero, new Vector2(.5f, .5f)); fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one;
            var fillImage = Image(fill, Color.cyan, AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd")); fillImage.type = UnityEngine.UI.Image.Type.Filled; fillImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; fillImage.fillOrigin = 0;
            ui.power = track.gameObject.AddComponent<ProgressBar>(); ui.power.barImage = fillImage; ui.power.onValueChanged = new ProgressBar.OnValueChanged(); ui.power.maxValue = 100;
            ui.bindings = Text("Assigned driving controls", root, new Vector2(1880, 32), new Vector2(20, 42), 20, TextAlignmentOptions.Center);
            ui.bindings.rectTransform.anchorMin = ui.bindings.rectTransform.anchorMax = ui.bindings.rectTransform.pivot = Vector2.zero;
            ui.bindings.rectTransform.anchoredPosition = new Vector2(20, 14);
            foreach (var legacy in new[] { hud.banner, hud.playerPanel, hud.aiPanel, hud.notice, hud.speed, hud.scoreboard, hud.status, hud.controls })
            {
                if (legacy == null) continue;
                if (legacy.transform.parent != hud.transform && legacy.transform.parent.name.EndsWith("Arena Dock", StringComparison.Ordinal)) legacy.transform.parent.gameObject.SetActive(false);
                legacy.gameObject.SetActive(false);
            }
            root.SetAsFirstSibling(); // Existing pause/results/setup pages retain overlay priority.
            EditorUtility.SetDirty(hud); EditorSceneManager.MarkSceneDirty(hud.gameObject.scene); EditorSceneManager.SaveScene(hud.gameObject.scene); AssetDatabase.SaveAssets();
            Debug.Log("BILLIARDS_HUD_AUTHORED additive saved Heat/TMP cards, objective balls, series/turn/power and assigned controls. No table, truck, camera, rules, inputs or menu objects removed.");
        }
    }
}
