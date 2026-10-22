#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class AirHockeyMenuPresentationTests
{
    static object Field(object target, string name) => target.GetType().GetField(name).GetValue(target);
    [Test]
    public void AirHockeySavedPresentationRetainsButtonsAndIndependentPlayerReferences()
    {
        EditorSceneManager.OpenScene("Assets/TruckAirHockey/Scenes/TruckAirHockey.unity");
        var menus = UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckAirHockey.AirHockeyMenus, Assembly-CSharp", true), FindObjectsSortMode.None).Single(); var ui = (Component)Field(menus, "presentation"); Assert.That(ui, Is.Not.Null);
        foreach (string name in new[] { "portraits", "names", "points", "setupPortraits", "setupAccents", "setupStatus", "resultPortraits", "resultAccents", "resultNames", "resultPoints", "resultRanks", "pauseScores" })
        { var array = (Array)Field(ui, name); Assert.That(array.Length, Is.EqualTo(2), name); Assert.That(array.Cast<object>().All(v => v != null), Is.True, name); Assert.That(array.Cast<object>().Distinct().Count(), Is.EqualTo(2)); }
        foreach (string name in new[] { "names", "sourceLabels", "levelLabels", "colorLabels" }) Assert.That(((Array)Field(menus, name)).Cast<object>().All(v => v != null), Is.True);
        var controls = (Component)Field(ui, "controls"); var rect = (RectTransform)controls.transform; Assert.That(rect.anchorMin, Is.EqualTo(new Vector2(.5f, 0))); Assert.That(rect.anchorMax, Is.EqualTo(rect.anchorMin));
        Assert.That(controls.gameObject.activeSelf, Is.True, "Actual-device controls must be visible while the HUD is active.");
        var hud = (GameObject)Field(menus, "hud"); var overlay = (Behaviour)hud.GetComponent(Type.GetType("LWS.TruckSports.SportsArenaOverlay, Assembly-CSharp", true)); Assert.That(overlay == null || !overlay.enabled, Is.True, "Legacy projected docks must not compete with the new HUD.");
        var setup = ((GameObject[])Field(menus, "pages"))[0]; Assert.That(setup.transform.Find("Original sport title artwork"), Is.Not.Null); Assert.That(setup.transform.Find("Existing original Air Hockey logo"), Is.Null, "Reuse the original branding instead of a duplicate.");
        Assert.That(((Component)Field(menus, "scoreText")).gameObject.activeSelf, Is.False); Assert.That(((Component)Field(menus, "resultText")).gameObject.activeSelf, Is.False);
    }
    [Test]
    public void ScrapHeapExcludesOnlyRequestedCardsFromMainAndPreservesThreeLaunchActions()
    {
        EditorSceneManager.OpenScene("Assets/TruckSports/Scenes/TruckSports.unity");
        var front = (Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckSports.SportsFrontEnd, Assembly-CSharp", true), FindObjectsSortMode.None).Single();
        var pages = (GameObject[])Field(front, "pages"); var cards = (GameObject[])Field(Field(front, "catalogue"), "cards"); var actionType = Type.GetType("LWS.TruckSports.SportsMenuAction, Assembly-CSharp", true);
        foreach (string key in new[] { "logrolling", "dodgeball", "wallball" })
        {
            Assert.That(cards.Any(card => card.GetComponentsInChildren(actionType, true).Any(a => (string)Field(a, "action") == key)), Is.False, key);
            var actions = front.GetComponentsInChildren(actionType, true).Where(a => (string)Field(a, "action") == key).ToArray(); Assert.That(actions.Length, Is.EqualTo(1), key); Assert.That(actions[0].transform.IsChildOf(pages[4].transform), Is.True, key);
            var card = actions[0].transform.parent; Assert.That(card.Find("Scrap sport logo"), Is.Not.Null); Assert.That(card.Find("Scrap gameplay preview"), Is.Not.Null);
        }
    }
}
#endif
