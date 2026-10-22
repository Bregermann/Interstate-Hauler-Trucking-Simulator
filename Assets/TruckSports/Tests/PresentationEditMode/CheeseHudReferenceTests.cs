#if UNITY_EDITOR
using System;
using System.Linq;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class CheeseHudReferenceTests
{
    static object Get(object t, string n) => t.GetType().GetField(n).GetValue(t);
    [Test] public void OriginalTerrainScoringInputGridAndEarnedBoostFixturesStillPass()
    { Type.GetType("LWS.TruckCheeseRolling.Editor.CheeseFixtures, Assembly-CSharp-Editor", true).GetMethod("Run").Invoke(null, null); }
    [Test] public void RepeatedExplicitUpgradesPreserveSavedDesignerAssetsByteForByte()
    {
        var paths = new[] { "Assets/TruckCheeseRolling/Scenes/TruckCheeseRolling.unity", "Assets/TruckCheeseRolling/Prefabs/Animated owned cat.prefab", "Assets/TruckCheeseRolling/Art/Animals/Scenery cat gestures.controller", "Assets/TruckCheeseRolling/PrivateLicensedVfx/Resources/CheeseOwnedExplosion.prefab" };
        var bytes = paths.Select(File.ReadAllBytes).ToArray();
        foreach (string tool in new[] { "CheeseCatPresentationAuthoring", "CheeseExplosionPresentationAuthoring", "CheeseHudPresentationAuthoring" })
            Type.GetType("LWS.TruckCheeseRolling.Editor." + tool + ", Assembly-CSharp-Editor", true).GetMethod("Upgrade").Invoke(null, null);
        for (int i = 0; i < paths.Length; i++) CollectionAssert.AreEqual(bytes[i], File.ReadAllBytes(paths[i]), paths[i]);
    }
    [Test] public void SavedHudKeepsOriginalCountdownLocatorAndScoreAuthority()
    {
        EditorSceneManager.OpenScene("Assets/TruckCheeseRolling/Scenes/TruckCheeseRolling.unity");
        var m = UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckCheeseRolling.CheeseMatch, Assembly-CSharp", true), FindObjectsSortMode.None).Single();
        var menus = Get(m, "menus"); var p = Get(menus, "presentation"); Assert.That(p, Is.Not.Null); Assert.That(Get(p, "menus"), Is.SameAs(menus));
        Assert.That((int)Get(p, "authoredVersion"), Is.EqualTo(2));
        var views = (Array)Get(p, "views"); var originalViews = (RectTransform[])Get(menus, "views"); var indicators = (Array)Get(menus, "indicators");
        Assert.That(views.Length, Is.EqualTo(4)); Assert.That(((Array)Get(menus, "countdowns")).Length, Is.EqualTo(4)); Assert.That(((Array)Get(m, "trucks")).Length, Is.EqualTo(16));
        for (int i = 0; i < 4; i++)
        {
            var view = views.GetValue(i); var card = (RectTransform)Get(view, "card"); Assert.That(card.parent, Is.SameAs(originalViews[i]));
            Assert.That(((UnityEngine.UI.Image)Get(view, "portrait")).sprite, Is.Not.Null); Assert.That(((UnityEngine.UI.Image)Get(view, "progress")).type, Is.EqualTo(UnityEngine.UI.Image.Type.Filled));
            Assert.That(((Array)Get(view, "opponents")).Length, Is.EqualTo(3)); var marker = (Component)indicators.GetValue(i); var back = (RectTransform)Get(view, "markerBack");
            Assert.That(back.parent, Is.SameAs(marker.transform.parent)); Assert.That(back.GetSiblingIndex(), Is.LessThan(marker.transform.GetSiblingIndex()));
            foreach (var field in new[] { "name", "catches", "rank", "speed", "clock", "prompt" }) Assert.That(Get(view, field), Is.Not.Null);
            Assert.That(((Component)Get(view, "prompt")).GetComponent<RectTransform>().anchoredPosition.y, Is.EqualTo(36));
            Assert.That(originalViews[i].Find("Menu source contrast backing").GetComponent<UnityEngine.UI.Image>().color.a, Is.GreaterThan(.5f));
        }
        Assert.That(((Array)Get(p, "resultCards")).Length, Is.EqualTo(4)); Assert.That(((Component)Get(menus, "resultText")).gameObject.activeSelf, Is.False);
    }
}
#endif
