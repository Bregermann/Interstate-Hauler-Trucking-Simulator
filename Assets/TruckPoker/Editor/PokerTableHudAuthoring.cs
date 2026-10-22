using System;
using System.Linq;
using LWS.TruckSports;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckPoker.Editor
{
    public static class PokerTableHudAuthoring
    {
        static TMP_FontAsset font; static Sprite portrait;
        static readonly Vector2 Center = new Vector2(.5f, .5f), Bottom = new Vector2(.5f, 0), Top = new Vector2(.5f, 1);
        static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 position)
        { var node = new GameObject(name, typeof(RectTransform)); node.transform.SetParent(parent, false); var r = (RectTransform)node.transform; Place(r, size, anchor, position); return r; }
        static void Place(RectTransform r, Vector2 size, Vector2 anchor, Vector2 position)
        { r.anchorMin = r.anchorMax = r.pivot = anchor; r.sizeDelta = size; r.anchoredPosition = position; }
        static UnityEngine.UI.Image Image(RectTransform r, Color color, Sprite sprite = null)
        { var image = r.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.sprite = sprite; image.raycastTarget = false; image.preserveAspect = sprite != null; return image; }
        static RectTransform Plate(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 position)
        { var r = Rect(name, parent, size, anchor, position); Image(r, SportsBroadcastCanvas.Ink); r.gameObject.AddComponent<SportsBroadcastPlate>().cut = 12; return r; }
        static TMP_Text Label(string name, Transform parent, Vector2 size, Vector2 position, int points)
        { var r = Rect(name, parent, size, Center, position); var t = r.gameObject.AddComponent<TextMeshProUGUI>(); Style(t, points); return t; }
        static void Style(TMP_Text t, int points)
        { t.font = font; t.fontSize = t.fontSizeMax = points; t.fontSizeMin = points - 3; t.enableAutoSizing = true; t.alignment = TextAlignmentOptions.Center; t.color = SportsBroadcastCanvas.Cream; t.raycastTarget = false; t.overflowMode = TextOverflowModes.Ellipsis; t.textWrappingMode = TextWrappingModes.NoWrap; }
        static void Move(TMP_Text t, Transform parent, Vector2 size, Vector2 position, int points)
        { t.transform.SetParent(parent, false); Place(t.rectTransform, size, Center, position); Style(t, points); t.gameObject.SetActive(true); }
        static PokerPublicSeat Seat(Transform parent, int index, Vector2 anchor, Vector2 pos, bool small)
        {
            float width = small ? 142 : 270; float height = small ? 72 : 96;
            var root = Plate("Public seat " + (index + 1), parent, new Vector2(width, height), anchor, pos);
            var seat = new PokerPublicSeat { root = root };
            seat.acting = Image(Rect("Acting player underline", root, new Vector2(width - 16, 3), Center, new Vector2(0, -height / 2 + 3)), SportsBroadcastCanvas.Orange);
            seat.accent = Image(Rect("Crew identity stripe", root, new Vector2(3, height - 12), Center, new Vector2(-width / 2 + 2, 0)), Color.white);
            seat.portrait = Image(Rect("Original truck portrait", root, new Vector2(small ? 22 : 34, small ? 22 : 34), Center, new Vector2(-width / 2 + (small ? 18 : 26), small ? 18 : 9)), Color.white, portrait);
            seat.name = Label("Public crew name", root, new Vector2(small ? 100 : 180, small ? 26 : 30), new Vector2(small ? 10 : 23, small ? 20 : 30), small ? 21 : 25);
            seat.stack = Label("Conserved chip stack", root, new Vector2(small ? 80 : 160, small ? 30 : 36), new Vector2(small ? -8 : 20, small ? -5 : 0), small ? 25 : 28);
            seat.action = Label("Public action only", root, new Vector2(width - 18, small ? 20 : 24), new Vector2(0, small ? -25 : -32), small ? 17 : 19);
            seat.button = Label("Dealer and blind marker", root, new Vector2(34, 24), new Vector2(width / 2 - 22, small ? -4 : -3), 20); seat.button.color = SportsBroadcastCanvas.Orange;
            return seat;
        }
        static void Card(PokerCardView card, Transform parent, Vector2 position, Vector2 anchor)
        { card.transform.SetParent(parent, false); Place(card.GetComponent<RectTransform>(), card.GetComponent<RectTransform>().sizeDelta, anchor, position); }
        public static void RulesRegression() { PokerFixtures.Run(true); }
        static bool RefineUnshippedBroadcast(PokerTableHud hud)
        {
            if (hud.authoredVersion >= 3) return false;
            if (hud.header.anchorMin == Top && (hud.header.anchoredPosition == new Vector2(0, -12) || hud.header.anchoredPosition == new Vector2(0, -104)))
            { hud.header.anchorMin = hud.header.anchorMax = new Vector2(.5f, .88f); hud.header.pivot = Top; hud.header.anchoredPosition = Vector2.zero; }
            hud.venueLogo = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(r => r.name == "Original sport venue artwork");
            for (int i = 0; i < hud.broadcastSeats.Length; i++)
            {
                var seat = hud.broadcastSeats[i]; if (seat.root.sizeDelta != new Vector2(270, 82) || seat.stack.rectTransform.sizeDelta != new Vector2(160, 30)) continue;
                seat.root.sizeDelta = new Vector2(270, 96); seat.root.anchoredPosition = new Vector2(i < 4 ? 14 : -14, 210 - i % 4 * 106);
                Place(seat.name.rectTransform, new Vector2(180, 30), Center, new Vector2(23, 30));
                Place(seat.stack.rectTransform, new Vector2(160, 36), Center, new Vector2(20, 0)); Style(seat.stack, 28);
                Place(seat.action.rectTransform, new Vector2(252, 24), Center, new Vector2(0, -32));
                seat.accent.rectTransform.sizeDelta = new Vector2(3, 84); seat.acting.rectTransform.anchoredPosition = new Vector2(0, -45);
            }
            var caption = hud.broadcastDesign.Find("Spectator privacy")?.GetComponent<TMP_Text>();
            if (caption != null && caption.text == "PUBLIC TABLE / PRIVATE HANDS HIDDEN") caption.text = "PUBLIC CARDS ONLY";
            hud.authoredVersion = 3; EditorUtility.SetDirty(hud); return true;
        }
        [MenuItem("Truck Sports/Poker/Upgrade saved table HUD and AI presence")]
        public static void Upgrade()
        {
            var scene = EditorSceneManager.OpenScene(PokerAuthoring.Scene); var m = UnityEngine.Object.FindAnyObjectByType<PokerMatch>();
            if (m == null || m.presentation == null || m.presentation.panes.Length != 4) throw new InvalidOperationException("Existing Poker scene and four private card panes required.");
            var theme = AssetDatabase.LoadAssetAtPath<SportsBroadcastTheme>("Assets/TruckSports/Resources/SportsBroadcastTheme.asset");
            if (theme == null || theme.headingFont == null) throw new InvalidOperationException("Existing Heat broadcast theme required."); font = theme.headingFont;
            if (m.presentation.tableHud != null)
            {
                if (RefineUnshippedBroadcast(m.presentation.tableHud)) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                Debug.Log("POKER_TABLE_HUD_PRESERVED saved designer layout and original cards."); return;
            }
            portrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TruckSports/Resources/Broadcast truck portrait.png"); if (portrait == null) throw new InvalidOperationException("Existing truck portrait required.");
            foreach (var e in m.presentation.expressions) if (e == null || e.GetComponentsInChildren<Collider>(true).Length > 0 || e.GetComponentsInChildren<Rigidbody>(true).Length > 0 || e.GetComponentsInChildren<Camera>(true).Length > 0) throw new InvalidOperationException("Poker gesture hierarchy must be visual-only.");
            var p = m.presentation; var hud = m.gameObject.AddComponent<PokerTableHud>(); hud.match = m; hud.authoredVersion = 3; p.tableHud = hud;
            hud.venueLogo = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(r => r.name == "Original sport venue artwork");
            var canvas = m.menus.hud.transform; SportsBroadcastCanvas.Stretch((RectTransform)canvas);
            var overlay = canvas.GetComponent<SportsArenaOverlay>(); if (overlay != null) overlay.enabled = false;
            var scores = m.menus.GetComponent<SportsBroadcastScores>(); if (scores != null) scores.enabled = false;
            hud.legacyDocks = new[] { m.menus.header.transform.parent.gameObject, m.menus.status.transform.parent.gameObject, m.menus.standings.transform.parent.gameObject }.Where(g => g.transform != canvas && g.name != "Event HUD").Distinct().ToArray();
            foreach (var old in hud.legacyDocks) old.SetActive(false);
            hud.header = Plate("Poker table title rail", canvas, new Vector2(1140, 44), new Vector2(.5f, .88f), Vector2.zero); hud.header.pivot = Top; hud.hand = Label("Hand and blinds", hud.header, new Vector2(1100, 36), Vector2.zero, 26);
            hud.broadcastRoot = Rect("Public broadcast table overlay", canvas, Vector2.zero, Center, Vector2.zero); SportsBroadcastCanvas.Stretch(hud.broadcastRoot);
            hud.broadcastSeats = new PokerPublicSeat[8];
            for (int i = 0; i < 8; i++) hud.broadcastSeats[i] = Seat(hud.broadcastRoot, i, new Vector2(i < 4 ? 0 : 1, .5f), new Vector2(i < 4 ? 14 : -14, 210 - i % 4 * 106), false);
            // The original spectator board, private renderers and betting routes
            // are moved, not replaced. Their original data/lifecycle is retained.
            p.spectatorRoot.transform.SetParent(hud.broadcastRoot, false);
            hud.broadcastDesign = (RectTransform)p.spectatorRoot.transform; Place(hud.broadcastDesign, new Vector2(1180, 180), Bottom, new Vector2(0, 18));
            Image(hud.broadcastDesign, SportsBroadcastCanvas.Ink); hud.broadcastDesign.gameObject.AddComponent<SportsBroadcastPlate>().cut = 14;
            for (int c = 0; c < 5; c++) Card(p.spectatorBoard[c], hud.broadcastDesign, new Vector2((c - 2) * 106, 80), Bottom);
            hud.broadcastActor = Label("Current public actor", hud.broadcastDesign, new Vector2(260, 36), new Vector2(-432, 34), 29);
            hud.broadcastStack = Label("Acting stack", hud.broadcastDesign, new Vector2(260, 34), new Vector2(-432, -6), 25);
            hud.broadcastPhase = Label("Public street", hud.broadcastDesign, new Vector2(260, 32), new Vector2(-432, -43), 22);
            hud.broadcastPot = Label("Authoritative pot total", hud.broadcastDesign, new Vector2(270, 48), new Vector2(430, 18), 34);
            var publicOnly = Label("Spectator privacy", hud.broadcastDesign, new Vector2(270, 28), new Vector2(430, -27), 20); publicOnly.text = "PUBLIC CARDS ONLY";
            hud.views = new PokerTableHudView[4];
            for (int v = 0; v < 4; v++)
            {
                var pane = p.panes[v]; var info = new PokerTableHudView(); hud.views[v] = info;
                info.design = Rect("Owned table lower third", pane.root, new Vector2(1180, 280), Bottom, new Vector2(0, 10));
                var handPlate = Plate("Owned hand and community board", info.design, new Vector2(1180, 140), Bottom, new Vector2(0, 140));
                info.owner = Label("Owned crew name", handPlate, new Vector2(250, 32), new Vector2(-432, 35), 27);
                info.stack = Label("Own public chip stack", handPlate, new Vector2(250, 36), new Vector2(-432, -2), 30);
                info.pot = Label("Public pot", handPlate, new Vector2(255, 32), new Vector2(-432, -41), 26);
                for (int c = 0; c < 5; c++) Card(pane.board[c], handPlate, new Vector2(-228 + c * 78, 9), Center);
                for (int c = 0; c < 2; c++) Card(pane.own[c], handPlate, new Vector2(420 + c * 78, 9), Center);
                Move(pane.hint, handPlate, new Vector2(755, 26), new Vector2(151, -53), 20);
                info.phase = Label("Public hand street and blinds", pane.root, new Vector2(1170, 28), Vector2.zero, 22); Place(info.phase.rectTransform, new Vector2(1170, 28), Top, new Vector2(0, -8));
                info.seats = Rect("Viewport public chip strip", pane.root, new Vector2(1180, 72), Top, new Vector2(0, -42));
                // Phase scales with the same local viewport-safe strip.
                info.phase.transform.SetParent(info.seats, false); Place(info.phase.rectTransform, new Vector2(1170, 28), Bottom, new Vector2(0, 78));
                info.publicSeats = new PokerPublicSeat[8]; for (int i = 0; i < 8; i++) info.publicSeats[i] = Seat(info.seats, i, Top, new Vector2((i - 3.5f) * 147, 0), true);
                var actions = (RectTransform)pane.actions.transform; actions.SetParent(info.design, false); Place(actions, new Vector2(1180, 130), Bottom, Vector2.zero);
                Move(pane.bet, actions, new Vector2(1130, 27), new Vector2(0, 48), 24); Move(pane.reason, actions, new Vector2(1130, 24), new Vector2(0, -51), 19);
                foreach (var button in actions.GetComponentsInChildren<PokerMouseButton>(true))
                {
                    Place((RectTransform)button.transform, new Vector2(152, 36), Center, new Vector2(-510 + button.action * 170, 10));
                    var heat = button.GetComponent<Michsky.UI.Heat.ButtonManager>(); if (heat != null) { heat.normalTextObj.fontSizeMax = 21; heat.normalTextObj.fontSizeMin = 18; }
                }
                Place(pane.amount.GetComponent<RectTransform>(), new Vector2(760, 18), Center, new Vector2(0, -24));
                var track = pane.amount.transform.Find("Track") as RectTransform; if (track != null) track.sizeDelta = new Vector2(760, 5);
                // Existing card ranks/suits, privacy and input route components
                // are preserved byte-for-byte apart from layout transforms.
            }
            var presence = m.gameObject.AddComponent<PokerTablePresence>(); presence.match = m; presence.pivots = new Transform[p.expressions.Length]; p.presence = presence;
            for (int i = 0; i < presence.pivots.Length; i++)
            {
                var old = p.expressions[i]; var wrapper = new GameObject("Public ambient truck presence " + i).transform; wrapper.SetParent(old.parent, false); wrapper.SetSiblingIndex(old.GetSiblingIndex()); old.SetParent(wrapper, false); presence.pivots[i] = wrapper;
            }
            EditorUtility.SetDirty(m); EditorUtility.SetDirty(p); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("POKER_TABLE_HUD_AUTHORED original cards/betting/privacy/dealer preserved; public stacks/button/pots/actor, owner-isolated lower thirds and safe AI visual pivots.");
        }
    }
}
