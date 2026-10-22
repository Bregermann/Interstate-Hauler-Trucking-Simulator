#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class BilliardsHudPresentationTests
{
    static object Field(object target, string field) => target.GetType().GetField(field).GetValue(target);
    [Test]
    public void SavedHudHasIndependentBallCardsPortraitsAndActualHeatPowerFill()
    {
        EditorSceneManager.OpenScene("Assets/TruckBilliards/Scenes/TruckBilliards.unity");
        var hud = UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckBilliards.BilliardsHUD, Assembly-CSharp", true), FindObjectsSortMode.None).Single();
        var ui = (Component)Field(hud, "presentation"); Assert.That(ui, Is.Not.Null);
        foreach (string name in new[] { "cards", "names", "scores", "portraits", "accents", "turns", "groups", "remaining" })
        { var entries = (Array)Field(ui, name); Assert.That(entries.Length, Is.EqualTo(2), name); Assert.That(entries.Cast<object>().All(v => v != null), Is.True, name); }
        foreach (string name in new[] { "ballFaces", "ballNumbers" })
        { var entries = (Array)Field(ui, name); Assert.That(entries.Length, Is.EqualTo(30)); Assert.That(entries.Cast<object>().Distinct().Count(), Is.EqualTo(30)); }
        var bar = Field(ui, "power"); Assert.That(bar.GetType().FullName, Is.EqualTo("Michsky.UI.Heat.ProgressBar")); var image = Field(bar, "barImage"); Assert.That(image.GetType().GetProperty("sprite").GetValue(image), Is.Not.Null);
        foreach (string name in new[] { "banner", "status", "playerPanel", "aiPanel", "notice", "speed", "controls", "scoreboard" })
        { var old = (Component)Field(hud, name); Assert.That(old, Is.Not.Null, "Original serialized references remain: " + name); Assert.That(old.gameObject.activeSelf, Is.False, name); }
        Assert.That(ui.GetComponentsInChildren<Collider>(true), Is.Empty); Assert.That(ui.GetComponentsInChildren<Camera>(true), Is.Empty);
        Assert.That(ui.GetComponentsInChildren<Component>(true).All(c => c != null), Is.True, "No missing scripts in added HUD");
    }
}
#endif
