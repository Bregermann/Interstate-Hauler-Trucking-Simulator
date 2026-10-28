using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckSports.Editor
{
    public static class SportsImpossibleArtworkAuthoring
    {
        public static void InstallFirstFiveHeaders()
        {
            InstallHeaders(new[] { "TruckTruckGoose", "TruckGuessThat", "TruckPinataPanic", "TruckMechanicalBull", "TruckYoga" });
        }

        public static void InstallAuthoredImpossibleHeaders()
        {
            InstallHeaders(SportsImpossibleGames.All.Where(entry => File.Exists(entry.ScenePath)).Select(entry => entry.scene).ToArray());
        }

        static void InstallHeaders(string[] names)
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled Editor required for explicit artwork binding.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save pending scene edits before explicit artwork binding.");
            // Require the actual original artwork and scenes before any changes.
            foreach (var name in names)
                if (!File.Exists(ScenePath(name)) || AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath(name)) == null)
                    throw new InvalidOperationException("Saved new scene and imported original sprite required: " + name);
            int bound = 0;
            foreach (var name in names)
            {
                var scene = EditorSceneManager.OpenScene(ScenePath(name), OpenSceneMode.Single);
                var session = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SportsEventSession>(true)).Single();
                foreach (int index in new[] { 0, 4 })
                {
                    var page = session.menus.pages[index];
                    if (page == null) throw new InvalidOperationException("Expected existing setup/help page: " + name);
                    var title = page.transform.Find("Title")?.GetComponent<TMP_Text>();
                    if (title == null) throw new InvalidOperationException("Expected authored title on " + name + "/" + page.name);
                    var prior = page.transform.Find("Original sport title artwork");
                    var art = prior != null ? prior.gameObject : SportsUIAuthoring.Rect("Original sport title artwork", page.transform, new Vector2(250, 125), new Vector2(-545, 372));
                    var image = art.GetComponent<UnityEngine.UI.Image>() ?? art.AddComponent<UnityEngine.UI.Image>();
                    image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath(name));
                    image.preserveAspect = true; image.raycastTarget = false; image.color = Color.white;
                    title.rectTransform.anchoredPosition = new Vector2(140, title.rectTransform.anchoredPosition.y);
                    title.rectTransform.sizeDelta = new Vector2(1020, title.rectTransform.sizeDelta.y);
                    EditorUtility.SetDirty(image); EditorUtility.SetDirty(title); bound++;
                }
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save original artwork binding: " + name);
            }
            Debug.Log("IMPOSSIBLE_ORIGINAL_ARTWORK_HEADERS=" + bound + "; menu, venue and rendered legibility acceptance remain separate.");
        }

        static string ScenePath(string name) => "Assets/" + name + "/Scenes/" + name + ".unity";
        static string ArtPath(string name) => "Assets/TruckSports/Art/Logos/" + name + ".png";
    }
}
