using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckSports.Editor
{
    public static class SportsResultsPresentationAuthoring
    {
        public static void InstallMountainAndNinja() => InstallScenes(new[] { "TruckAvalancheEscape", "TruckNinjaWarrior" });
        public static void InstallScoredBatch() => InstallScenes(new[] { "TruckWhackAMole", "TruckMagnetMayhem", "TruckSheepHerding", "TruckSeesaw", "TruckKingOfTheHill", "TruckFloorIsLava", "TruckHideAndSeek", "TruckHungryHaulers" });
        public static void InstallContestBatch() => InstallScenes(new[] { "TruckHotPotato", "TruckBalloonBattle", "TruckTetherball", "TruckJousting", "TruckFrogger", "TruckRedLightGreenLight", "TruckVolleyball", "TruckFreezeTag" });
        public static void InstallAttemptBatch() => InstallScenes(new[] { "TruckDockDiving", "TruckFieldGoal", "TruckTargetLaunch", "TruckSkeeBall", "TruckGiantSwing", "TruckTrailerPlug" });
        public static void InstallAttemptReadability() => InstallReadability(new[] { "TruckDockDiving", "TruckFieldGoal", "TruckTargetLaunch", "TruckSkeeBall", "TruckGiantSwing", "TruckTrailerPlug" });
        static void InstallReadability(string[] names)
        {
            foreach (string name in names)
            {
                var scene = EditorSceneManager.OpenScene(SportsBuilder.SceneList().Single(path => System.IO.Path.GetFileNameWithoutExtension(path) == name), OpenSceneMode.Single);
                var session = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SportsEventSession>(true)).Single();
                var page = session.menus.pages[(int)SportsEventScreen.Result];
                if (page.transform.Find("Results readability backing") != null) continue;
                var backing = SportsUIAuthoring.Rect("Results readability backing", page.transform, Vector2.zero, Vector2.zero);
                var rect = (RectTransform)backing.transform; SportsBroadcastCanvas.Stretch(rect); rect.SetAsFirstSibling();
                var image = backing.AddComponent<UnityEngine.UI.Image>(); image.color = SportsBroadcastCanvas.Ink; image.raycastTarget = false;
                var plate = backing.AddComponent<SportsBroadcastPlate>(); plate.cut = 18;
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                Debug.Log("SPORTS_RESULTS_READABILITY " + name + " saved rounded noninteractive backing over busy venue scenery.");
            }
            AssetDatabase.SaveAssets();
        }
        public static void InstallSurvivalBatch()
        {
            var names = new[] { "TruckMinefield", "TruckFishing", "TruckSumo", "TruckLogRolling", "TruckLimbo" };
            InstallScenes(names); InstallReadability(names);
        }
        public static void InstallSeriesBatch()
        {
            var names = new[] { "TruckBaseJumping", "TruckBogSnorkelling", "TruckMattressRacing", "TruckTerrierRacing", "TruckWifeCarrying", "TruckSkyTow" };
            InstallScenes(names); InstallReadability(names);
        }
        public static void InstallPossessionTeamBatch()
        {
            var names = new[] { "TruckBuzkashi", "TruckKeepaway", "TruckKeepawayFirstPerson", "TruckKickball", "TruckTrampolineDodgeball" };
            InstallScenes(names); InstallReadability(names);
        }
        public static void InstallLegacyRoundBatch()
        {
            var names = new[] { "TruckBeerPong", "TruckBungeeDemolition", "TruckCurling", "TruckDanceOff", "TruckDemolitionDerby", "TruckFareFight", "TruckMusicalParking", "TruckSnake" };
            InstallScenes(names); InstallReadability(names);
        }
        static void InstallScenes(string[] names)
        {
            foreach (string name in names)
            {
                var scene = EditorSceneManager.OpenScene(SportsBuilder.SceneList().Single(path => System.IO.Path.GetFileNameWithoutExtension(path) == name), OpenSceneMode.Single);
                var session = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SportsEventSession>(true)).Single();
                var page = session.menus.pages[(int)SportsEventScreen.Result];
                if (page.GetComponentInChildren<SportsResultsPresentation>(true) != null) continue;
                var root = SportsUIAuthoring.Rect("Authoritative ranked results", page.transform, new Vector2(1320, 560), Vector2.zero);
                var presentation = root.AddComponent<SportsResultsPresentation>(); presentation.visibility = root.AddComponent<CanvasGroup>();
                presentation.visibility.alpha = 0; presentation.visibility.interactable = presentation.visibility.blocksRaycasts = false;
                presentation.menus = session.menus; presentation.source = root.AddComponent<SportsFinishResultsSource>(); presentation.source.session = session;
                var theme = Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme");
                presentation.heading = SportsUIAuthoring.Text("Outcome headline", root.transform, "", new Vector2(1280, 64), new Vector2(0, 205), 40); presentation.heading.font = theme.headingFont;
                presentation.summary = SportsUIAuthoring.Text("Authoritative summary", root.transform, "", new Vector2(1280, 32), new Vector2(0, 158), 21); presentation.summary.font = theme.bodyFont;
                presentation.rows = new SportsResultsPresentation.RowView[12];
                for (int i = 0; i < presentation.rows.Length; i++)
                {
                    var item = SportsUIAuthoring.Rect("Ranked participant " + i, root.transform, new Vector2(650, 58), Vector2.zero);
                    var rect = (RectTransform)item.transform; rect.pivot = new Vector2(0, 1);
                    var backing = item.AddComponent<UnityEngine.UI.Image>(); backing.color = SportsBroadcastCanvas.Raised; backing.raycastTarget = false;
                    var plate = item.AddComponent<SportsBroadcastPlate>(); plate.cut = 9; plate.accent = false;
                    var view = new SportsResultsPresentation.RowView { rect = rect, backing = backing, visibility = item.AddComponent<CanvasGroup>() };
                    view.visibility.interactable = view.visibility.blocksRaycasts = false;
                    view.accent = Block("Placing accent", rect, new Vector2(0, -4), new Vector2(4, 50));
                    view.portrait = Block("Participant truck", rect, new Vector2(72, -5), new Vector2(48, 48)); view.portrait.sprite = Resources.Load<Sprite>("Broadcast truck portrait"); view.portrait.preserveAspect = true;
                    view.place = Label("Actual place", rect, new Vector2(12, -6), new Vector2(54, 44), 27, theme.headingFont);
                    view.name = Label("Crew name", rect, new Vector2(130, -3), new Vector2(290, 28), 25, theme.headingFont);
                    view.control = Label("AI or human", rect, new Vector2(130, -32), new Vector2(64, 22), 16, theme.bodyFont);
                    view.detail = Label("Actual statistics", rect, new Vector2(200, -32), new Vector2(425, 24), 17, theme.bodyFont);
                    view.value = Label("Actual result value", rect, new Vector2(434, -7), new Vector2(204, 42), 28, theme.headingFont); view.value.fontSizeMin = 14; view.value.alignment = TextAlignmentOptions.MidlineRight;
                    presentation.rows[i] = view;
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                Debug.Log("SPORTS_RESULTS_SAVED " + name + " additive read-only ranks, actual metrics and existing Heat navigation preserved.");
            }
            AssetDatabase.SaveAssets();
        }
        static UnityEngine.UI.Image Block(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var item = SportsUIAuthoring.Rect(name, parent, size, position); var rect = (RectTransform)item.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position;
            var image = item.AddComponent<UnityEngine.UI.Image>(); image.raycastTarget = false; return image;
        }
        static TMP_Text Label(string name, Transform parent, Vector2 position, Vector2 size, int font, TMP_FontAsset face)
        {
            var label = SportsUIAuthoring.Text(name, parent, "", size, position, font); var rect = label.rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position;
            label.font = face; label.fontSizeMin = 14; label.alignment = TextAlignmentOptions.MidlineLeft; label.color = SportsBroadcastCanvas.Cream; return label;
        }
    }
}
