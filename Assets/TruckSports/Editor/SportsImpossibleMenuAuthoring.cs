using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckSports.Editor
{
    public static partial class SportsNewEntriesAuthoring
    {
        // Explicit integration only, after real scenes and reviewed gameplay
        // captures exist. Normal builds do not generate art or rewrite the menu.
        public static void InstallImpossibleEntries()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled Editor required.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save pending scene edits before explicit menu integration.");
            var entries = SportsImpossibleGames.All.ToArray();
            foreach (var entry in entries)
                if (!File.Exists(entry.ScenePath))
                    throw new InvalidOperationException("Complete every requested saved sport before collection integration: " + entry.ScenePath);
            foreach (var entry in entries)
            {
                if (AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TruckSports/Art/Icons/Impossible07/" + entry.scene + ".png") == null)
                    throw new InvalidOperationException("Original imported sport icon required before adding the card: " + entry.scene);
                foreach (string folder in new[] { "Logos", "Previews" })
                    if (AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TruckSports/Art/" + folder + "/" + entry.scene + ".png") == null)
                        throw new InvalidOperationException("Original logo and actual reviewed gameplay capture required before adding the card: " + entry.scene + "/" + folder);
            }
            // Read the saved rule authority, including inherited capacities;
            // avoid advertising four trucks in the fixed eight-truck Moon mode.
            // Existing catalogue wording remains designer-owned.
            var information = JsonUtility.FromJson<SportsSportLogoAuthoring.Catalogue>(File.ReadAllText(SportsSportLogoAuthoring.InfoPath));
            if (information == null || information.entries == null ||
                information.entries.Select(item => item.scene).Distinct().Count() != information.entries.Length)
                throw new InvalidOperationException("Preserve and repair the existing unique sport-information catalogue before adding entries.");
            var additions = information.entries.ToList();
            foreach (var entry in entries)
            {
                var saved = EditorSceneManager.OpenScene(entry.ScenePath, OpenSceneMode.Single);
                var authority = saved.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SportsEventSession>(true)).Single();
                if (authority.MinimumPlayers < 1 || authority.MaximumPlayers < authority.MinimumPlayers)
                    throw new InvalidOperationException("Valid original participant capacity required: " + entry.scene);
                if (additions.Any(item => item.scene == entry.scene)) continue;
                string capacity = authority.MinimumPlayers == authority.MaximumPlayers ? authority.MaximumPlayers.ToString() : authority.MinimumPlayers + "–" + authority.MaximumPlayers;
                additions.Add(new SportsSportLogoAuthoring.Information { scene = entry.scene, title = authority.Title,
                    players = capacity + " PARTICIPANTS · UP TO " + Mathf.Min(4, authority.MaximumPlayers) + " LOCAL HUMANS · AI" });
            }
            var scene = EditorSceneManager.OpenScene(SportsBuilder.FrontScene, OpenSceneMode.Single);
            var front = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SportsFrontEnd>(true)).Single();
            foreach (var entry in entries) Add(front, entry.key, entry.title, entry.title, entry.description, entry.scene);
            int count = front.catalogue.cards.Length;
            var collection = front.pages[0].transform.Find("Collection")?.GetComponent<TMPro.TMP_Text>();
            if (collection != null) collection.text = count + " PLAYABLE TRUCK SPORTS. ONE COLLECTION.\nLocal humans, shared controllers and autonomous AI.";
            File.WriteAllText(SportsSportLogoAuthoring.InfoPath, JsonUtility.ToJson(new SportsSportLogoAuthoring.Catalogue { entries = additions.ToArray() }, true));
            AssetDatabase.ImportAsset(SportsSportLogoAuthoring.InfoPath, ImportAssetOptions.ForceSynchronousImport);
            front.catalogue.Step(0);
            SportsMenuArtAuthoring.InstallInto(front);
            EditorUtility.SetDirty(front); EditorUtility.SetDirty(front.catalogue);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save explicit new sport cards.");
            Debug.Log("IMPOSSIBLE_MENU_CARDS_ADDED=" + entries.Length + "; actual Windows navigation and launch acceptance remain required.");
        }
    }
}
