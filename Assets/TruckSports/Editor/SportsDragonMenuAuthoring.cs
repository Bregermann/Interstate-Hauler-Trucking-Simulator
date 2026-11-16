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
        // Explicit integration after saved gameplay and reviewed captures exist.
        // Registry/build dispatch alone never advertises an unfinished card.
        [MenuItem("Truck Sports/Menu/Install completed dragon sport entries")]
        public static void InstallDragonEntries()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled Editor required.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save pending scene edits before explicit menu integration.");

            var entries = SportsDragonGames.All.ToArray();
            foreach (var entry in entries)
            {
                if (!File.Exists(entry.ScenePath) || AssetDatabase.LoadAssetAtPath<SceneAsset>(entry.ScenePath) == null)
                    throw new InvalidOperationException("Complete every saved dragon sport before collection integration: " + entry.ScenePath);
                foreach (string folder in new[] { "Logos", "Previews" })
                {
                    string path = "Assets/TruckSports/Art/" + folder + "/" + entry.scene + ".png";
                    if (AssetDatabase.LoadAssetAtPath<Sprite>(path) == null)
                        throw new InvalidOperationException("Imported original logo and reviewed actual gameplay preview required: " + path);
                }
            }
            if (!File.Exists(SportsSportLogoAuthoring.InfoPath))
                throw new InvalidOperationException("Preserve the existing sport-information catalogue before adding entries.");
            string originalInformation = File.ReadAllText(SportsSportLogoAuthoring.InfoPath);
            var information = JsonUtility.FromJson<SportsSportLogoAuthoring.Catalogue>(originalInformation);
            if (information == null || information.entries == null || information.entries.Any(item => item == null || string.IsNullOrEmpty(item.scene)) ||
                information.entries.Select(item => item.scene).Distinct().Count() != information.entries.Length)
                throw new InvalidOperationException("Repair the existing unique sport-information catalogue before adding entries.");

            // Read actual saved participant authorities; do not infer capacities
            // from a shared four-truck presentation or rewrite prior wording.
            var additions = information.entries.ToList();
            foreach (var entry in entries)
            {
                var saved = EditorSceneManager.OpenScene(entry.ScenePath, OpenSceneMode.Single);
                var authority = saved.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SportsEventSession>(true)).Single();
                if (authority.MinimumPlayers < 1 || authority.MaximumPlayers < authority.MinimumPlayers)
                    throw new InvalidOperationException("Valid saved participant authority required: " + entry.scene);
                if (additions.Any(item => item.scene == entry.scene)) continue;
                string capacity = authority.MinimumPlayers == authority.MaximumPlayers ? authority.MaximumPlayers.ToString() : authority.MinimumPlayers + "–" + authority.MaximumPlayers;
                additions.Add(new SportsSportLogoAuthoring.Information { scene = entry.scene, title = authority.Title,
                    players = capacity + " PARTICIPANTS · UP TO " + Mathf.Min(4, authority.MaximumPlayers) + " LOCAL HUMANS · AI" });
            }

            var scene = EditorSceneManager.OpenScene(SportsBuilder.FrontScene, OpenSceneMode.Single);
            var front = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SportsFrontEnd>(true)).Single();
            if (front.catalogue == null || front.catalogue.cards == null || front.catalogue.cards.Any(card => card == null))
                throw new InvalidOperationException("Existing production catalogue cards required.");
            foreach (var entry in entries)
            {
                var existing = front.catalogue.cards.Where(card => card.GetComponentsInChildren<SportsMenuAction>(true).Any(action => action.action == entry.key)).ToArray();
                if (existing.Length > 1) throw new InvalidOperationException("Duplicate dragon card must be repaired: " + entry.key);
                if (existing.Length == 0) Add(front, entry.key, entry.title, entry.title, entry.description, entry.scene);
            }
            var collection = front.pages[0].transform.Find("Collection")?.GetComponent<TMPro.TMP_Text>();
            if (collection != null) collection.text = front.catalogue.cards.Length + " PLAYABLE TRUCK SPORTS. ONE COLLECTION.\nLocal humans, shared controllers and autonomous AI.";
            // Already present entries keep their exact designer wording. A
            // repeated installation leaves the complete JSON bytes untouched.
            if (additions.Count != information.entries.Length)
            {
                File.WriteAllText(SportsSportLogoAuthoring.InfoPath, JsonUtility.ToJson(new SportsSportLogoAuthoring.Catalogue { entries = additions.ToArray() }, true));
                AssetDatabase.ImportAsset(SportsSportLogoAuthoring.InfoPath, ImportAssetOptions.ForceSynchronousImport);
            }
            front.catalogue.Step(0);
            SportsMenuArtAuthoring.InstallInto(front);
            EditorUtility.SetDirty(front); EditorUtility.SetDirty(front.catalogue);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save explicit dragon sport cards.");
            Debug.Log("DRAGON_MENU_ENTRIES=" + entries.Length + "; actual Windows navigation and launch acceptance remain required.");
        }
    }
}
