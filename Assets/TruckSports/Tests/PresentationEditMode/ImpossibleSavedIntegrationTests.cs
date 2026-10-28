#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class ImpossibleSavedIntegrationTests
{
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Field(object owner, string name) => owner.GetType().GetField(name).GetValue(owner);
    static object[] Entries => ((IEnumerable)Runtime("SportsImpossibleGames").GetProperty("All").GetValue(null)).Cast<object>().ToArray();
    public static string[] Scenes => Entries.Select(entry => (string)Field(entry, "scene")).ToArray();

    [Test]
    public void TwentyOneDefinitionsHaveDistinctOriginalArtworkAndNavigationIdentities()
    {
        var entries = Entries; Assert.That(entries, Has.Length.EqualTo(21));
        Assert.That(entries.Select(entry => Field(entry, "key")).Distinct().Count(), Is.EqualTo(21));
        Assert.That(Scenes.Distinct().Count(), Is.EqualTo(21));
        foreach (var entry in entries)
        {
            string scene = (string)Field(entry, "scene"), key = (string)Field(entry, "key");
            object[] args = { key, null };
            Assert.That((bool)Runtime("SportsImpossibleGames").GetMethod("TryFindKey").Invoke(null, args), Is.True);
            Assert.That(args[1], Is.SameAs(entry));
            Assert.That((string)Field(entry, "description"), Is.Not.Empty);
            foreach (string folder in new[] { "Logos", "Icons/Impossible07" })
            {
                string path = "Assets/TruckSports/Art/" + folder + "/" + scene + ".png";
                var image = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                Assert.That(image, Is.Not.Null, path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), path);
                Assert.That(image.texture.width, Is.GreaterThan(32), path);
                Assert.That(image.rect.width / image.rect.height, Is.GreaterThan(.25f), path);
            }
        }
    }

    [TestCaseSource(nameof(Scenes))]
    public void SavedNewSceneHasNoMissingScriptsAndRetainsItsOwnSetupAndHelpArtwork(string name)
    {
        string path = "Assets/" + name + "/Scenes/" + name + ".unity";
        Assert.That(File.Exists(path), Is.True, "A definition is not an implemented scene.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var all = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            Assert.That(all.Sum(item => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject)), Is.Zero,
                "Saved references must resolve in the real imported scene.");
            var matches = roots.SelectMany(root => root.GetComponentsInChildren(Runtime("SportsEventSession"), true)).ToArray();
            Assert.That(matches, Has.Length.EqualTo(1));
            var menus = (Component)Field(matches[0], "menus");
            var pages = (GameObject[])Field(menus, "pages");
            foreach (int page in new[] { 0, 4 })
            {
                var art = pages[page].transform.Find("Original sport title artwork");
                Assert.That(art, Is.Not.Null, name + " actual setup/help art binding");
                var actualImage = art.GetComponents<Component>().Single(component => component != null && component.GetType().FullName == "UnityEngine.UI.Image");
                var serializedImage = new SerializedObject(actualImage);
                Assert.That(serializedImage.FindProperty("m_Sprite").objectReferenceValue,
                    Is.SameAs(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TruckSports/Art/Logos/" + name + ".png")));
            }
            Assert.That(roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Any(), Is.True);
        }
        finally
        {
            // A fresh batch test process may have no loaded, active saved scene.
            // Unity refuses to restore that empty setup. Restore real previous
            // scenes when present; this read-only fixture never saves either.
            if (setup.Any(item => item.isLoaded && item.isActive && !string.IsNullOrEmpty(item.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }
}
#endif
