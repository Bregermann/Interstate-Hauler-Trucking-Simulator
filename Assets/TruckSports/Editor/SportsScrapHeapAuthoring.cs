using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckSports.Editor
{
    public static class SportsScrapHeapAuthoring
    {
        static readonly string[] Keys = { "wallball", "curling", "swing", "logrolling", "dodgeball" };
        static readonly string[] Scenes = { SportsNavigation.Wallball, SportsNavigation.Curling, SportsNavigation.Swing, SportsNavigation.LogRolling, SportsNavigation.Dodgeball };
        static readonly string[] Descriptions = { "Wall volleys and truck returns.", "Aim, slide and settle on the ice.", "Build swing momentum and release.", "Balance, roll and battle on the log.", "Trampoline jumps and physical dodgeball." };
        static bool Archived(GameObject card) => card.GetComponentsInChildren<SportsMenuAction>(true).Any(a => Keys.Contains(a.action));
        static RectTransform Child(Transform parent, string name, Vector2 size, Vector2 position)
        {
            var child = parent.Find(name) as RectTransform;
            if (child == null) { var node = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)); node.transform.SetParent(parent, false); child = node.GetComponent<RectTransform>(); }
            child.anchorMin = child.anchorMax = child.pivot = new Vector2(.5f, .5f); child.sizeDelta = size; child.anchoredPosition = position; child.gameObject.SetActive(true); return child;
        }
        [MenuItem("Truck Sports/Move preserved games into Scrap Heap")]
        public static void Upgrade()
        {
            var scene = EditorSceneManager.OpenScene(SportsBuilder.FrontScene); var front = UnityEngine.Object.FindAnyObjectByType<SportsFrontEnd>();
            if (Apply(front)) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); }
        }
        public static bool Apply(SportsFrontEnd front)
        {
            if (front == null || front.catalogue == null || front.pages.Length < 5 || front.pages[4] == null) throw new InvalidOperationException("Existing Sports catalogue and Scrap Heap page required.");
            var scrap = front.pages[4].transform; var cards = new Transform[Keys.Length];
            for (int i = 0; i < Keys.Length; i++)
            {
                var actions = front.GetComponentsInChildren<SportsMenuAction>(true).Where(a => a.action == Keys[i]).ToArray();
                if (actions.Length != 1) throw new InvalidOperationException("Expected one preserved menu action for " + Keys[i] + "; found " + actions.Length);
                var card = actions[0].transform;
                while (card.parent != null && card.parent != front.pages[1].transform && card.parent != scrap) card = card.parent;
                if (card.parent == null) throw new InvalidOperationException("Existing sport card root is not inside the menu."); cards[i] = card;
            }
            if (!front.catalogue.cards.Any(Archived) && cards.All(c => c.parent == scrap && c.Find("Scrap sport logo") != null && c.Find("Scrap gameplay preview") != null))
            { Debug.Log("SPORTS_SCRAP_HEAP_PRESERVED curated cards, artwork and designer layout."); return false; }
            var logos = Scenes.Select(s => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TruckSports/Art/Logos/" + s + ".png")).ToArray();
            var previews = Scenes.Select(s => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TruckSports/Art/Previews/" + s + ".png")).ToArray();
            if (logos.Any(s => s == null) || previews.Any(s => s == null)) throw new InvalidOperationException("Every preserved Scrap sport requires its existing original logo and actual gameplay preview before changing the menu.");
            front.catalogue.cards = front.catalogue.cards.Where(c => !Archived(c)).ToArray();
            for (int i = 0; i < cards.Length; i++)
            {
                var card = cards[i]; card.SetParent(scrap, false); card.gameObject.SetActive(true); var rect = (RectTransform)card;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.localScale = Vector3.one; rect.sizeDelta = new Vector2(430, 290);
                rect.anchoredPosition = i < 3 ? new Vector2((i - 1) * 470, 160) : new Vector2((i - 3.5f) * 470, -160);
                var plate = card.GetComponent<SportsBroadcastPlate>() ?? card.gameObject.AddComponent<SportsBroadcastPlate>(); plate.cut = 16;
                var surface = card.GetComponent<UnityEngine.UI.Image>(); if (surface != null) { surface.color = SportsBroadcastCanvas.Ink; surface.raycastTarget = false; }
                foreach (Transform child in card)
                    if (child.name != "Description" && child.GetComponent<SportsMenuAction>() == null) child.gameObject.SetActive(false);
                var logo = Child(card, "Scrap sport logo", new Vector2(400, 78), new Vector2(0, 94)).GetComponent<UnityEngine.UI.Image>();
                logo.sprite = logos[i]; logo.color = Color.white; logo.preserveAspect = true; logo.raycastTarget = false;
                var preview = Child(card, "Scrap gameplay preview", new Vector2(400, 110), new Vector2(0, -12)).GetComponent<UnityEngine.UI.Image>();
                preview.sprite = previews[i]; preview.color = Color.white; preview.preserveAspect = true; preview.raycastTarget = false;
                var description = card.Find("Description").GetComponent<TMP_Text>(); description.gameObject.SetActive(true); description.text = Descriptions[i]; description.rectTransform.sizeDelta = new Vector2(400, 32); description.rectTransform.anchoredPosition = new Vector2(0, -90); description.fontSizeMax = 22; description.fontSizeMin = 18; description.enableAutoSizing = true;
                var action = card.GetComponentsInChildren<SportsMenuAction>(true).Single(a => a.action == Keys[i]); var button = action.GetComponent<RectTransform>();
                button.anchorMin = button.anchorMax = button.pivot = new Vector2(.5f, .5f); button.sizeDelta = new Vector2(390, 34); button.anchoredPosition = new Vector2(0, -125); action.gameObject.SetActive(true);
                action.button.buttonText = "PLAY"; action.button.normalTextObj.text = "PLAY";
                foreach (var choice in card.GetComponentsInChildren<SportsCatalogueChoice>(true)) { choice.hero = null; EditorUtility.SetDirty(choice); }
            }
            var subtitle = scrap.Find("Subtitle")?.GetComponent<TMP_Text>(); if (subtitle != null) subtitle.text = "Preserved collection games · Select a sport to play";
            var mainSubtitle = front.pages[1].transform.Find("Subtitle")?.GetComponent<TMP_Text>(); if (mainSubtitle != null) mainSubtitle.text = front.catalogue.cards.Length + " active sports · Additional games in Scrap Heap · Offline local play";
            var collection = front.pages[0].transform.Find("Collection")?.GetComponent<TMP_Text>(); if (collection != null) collection.text = System.Text.RegularExpressions.Regex.Replace(collection.text, @"^\d+ ", front.catalogue.cards.Length + " ");
            EditorUtility.SetDirty(front); EditorUtility.SetDirty(front.catalogue);
            Debug.Log("SPORTS_SCRAP_HEAP_AUTHORED Curling, Giant Swing, Log Rolling and Trampoline Dodgeball curated with existing Wallball; all five retain original actions, scenes, logos and gameplay previews. No game asset deleted."); return true;
        }
    }
}
