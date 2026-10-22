#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class BowlingHudPresentationTests
{
    static object Field(object value, string name) => value.GetType().GetField(name).GetValue(value);
    [Test]
    public void SavedFourLaneHudRetainsOriginalReferencesAndUsesAssignedHeatGraphics()
    {
        EditorSceneManager.OpenScene("Assets/TruckBowling/Scenes/TruckBowling.unity"); var menus = UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckBowling.BowlingMenus, Assembly-CSharp", true), FindObjectsSortMode.None).Single();
        var ui = (Component)Field(menus, "presentation"); Assert.That(ui, Is.Not.Null);
        foreach (string name in new[] { "portraits", "accents", "totals", "frames", "states", "progress" })
        { var array = (Array)Field(ui, name); Assert.That(array.Length, Is.EqualTo(4), name); Assert.That(array.Cast<object>().All(o => o != null), Is.True, name); Assert.That(array.Cast<object>().Distinct().Count(), Is.EqualTo(4)); }
        foreach (string name in new[] { "headers", "guidance", "popups", "viewRoots" }) Assert.That(((Array)Field(menus, name)).Cast<object>().All(o => o != null), Is.True, name);
        foreach (var bar in (Array)Field(ui, "progress")) { Assert.That(bar.GetType().FullName, Is.EqualTo("Michsky.UI.Heat.ProgressBar")); var image = Field(bar, "barImage"); Assert.That(image.GetType().GetProperty("sprite").GetValue(image), Is.Not.Null); }
        foreach (Component portrait in (Array)Field(ui, "portraits")) Assert.That(portrait.GetType().GetProperty("sprite").GetValue(portrait), Is.Not.Null);
    }
}
#endif
