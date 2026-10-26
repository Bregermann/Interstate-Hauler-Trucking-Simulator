#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class CarnivalLeaderboardPolicyTests
{
    static Type Source => Type.GetType("LWS.TruckSports.SportsCarnivalHudSource, Assembly-CSharp", true);
    static Type Row => Type.GetType("LWS.TruckSports.SportsCarnivalHudRow, Assembly-CSharp", true);
    static object Call(string name, params object[] values) => Source.GetMethod(name).Invoke(null, values);
    static object Make(string id, int place, int group)
    {
        var row = Activator.CreateInstance(Row); Row.GetField("id").SetValue(row, id); Row.GetField("name").SetValue(row, "Same Driver"); Row.GetField("place").SetValue(row, place); Row.GetField("group").SetValue(row, group); return row;
    }
    [Test] public void EveryAcceptedGateBoundsProgressAndShortcutCannotClaimLaterSegment()
    {
        float[] gates = { 60, 120, 180, 240, 310 };
        Assert.That(Call("GatedDistance", 0, 290f, gates, 310f), Is.EqualTo(60f));
        Assert.That(Call("GatedDistance", 1, 290f, gates, 310f), Is.EqualTo(120f));
        Assert.That(Call("GatedDistance", 2, 148f, gates, 310f), Is.EqualTo(148f));
        Assert.That(Call("GatedDistance", 3, 2f, gates, 310f), Is.EqualTo(180f));
        Assert.That(Call("GatedDistance", 5, 900f, gates, 310f), Is.EqualTo(310f));
    }
    [Test] public void ExplicitModeListDoesNotMistakeCountdownsOrSoloAttemptsForSimultaneousRaces()
    {
        string[] yes = { "TruckBaseJumping.BaseJumpMatch", "TruckMattressRacing.MattressMatch", "TruckWifeCarrying.WifeMatch", "TruckBogSnorkelling.BogMatch", "TruckTerrierRacing.TerrierMatch", "TruckAvalancheEscape.AvalancheMatch", "TruckRedLightGreenLight.RedLightMatch" };
        string[] no = { "TruckVolleyball.VolleyMatch", "TruckTetherball.TetherMatch", "TruckHotPotato.HotPotatoMatch", "TruckFrogger.FrogMatch", "TruckNinjaWarrior.NinjaMatch", "TruckSkyTow.SkyTowMatch", "TruckCheeseRolling.CheeseMatch", "TruckPedestrianPlowing.PlowMatch", "TruckHungryHaulers.HaulerMatch" };
        foreach (string name in yes.Concat(no))
        {
            var sport = Type.GetType("LWS." + name + ", Assembly-CSharp", true);
            Assert.That(Call("SupportsSport", sport), Is.EqualTo(yes.Contains(name)), name);
        }
    }
    [Test] public void EqualNamesAndPlacesUseStableIdsWithoutFabricatingDifferentPlaces()
    {
        var a = Make("profile-a", 2, 1); var b = Make("profile-b", 2, 1); var first = Make("profile-z", 1, 0); var outOfRace = Make("profile-0", 0, 2);
        Assert.That((int)Call("Compare", a, b), Is.LessThan(0)); Assert.That((int)Call("Compare", b, a), Is.GreaterThan(0));
        Assert.That((int)Call("Compare", first, a), Is.LessThan(0)); Assert.That((int)Call("Compare", outOfRace, a), Is.GreaterThan(0));
        Assert.That(Row.GetField("place").GetValue(a), Is.EqualTo(2)); Assert.That(Row.GetField("place").GetValue(b), Is.EqualTo(2));
    }
    [Test] public void SingleViewMustActuallyFillScreen()
    {
        Assert.That(Call("IsFullScreen", new Rect(0, 0, 1, 1)), Is.True);
        foreach (var rect in new[] { new Rect(0, 0, .5f, 1), new Rect(.5f, 0, .5f, .5f), new Rect(0, .5f, 1, .5f), new Rect(.01f, 0, .99f, 1) }) Assert.That(Call("IsFullScreen", rect), Is.False);
    }
}
#endif
