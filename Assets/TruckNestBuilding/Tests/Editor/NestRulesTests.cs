using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using LWS.TruckNestBuilding;

public sealed class NestRulesTests
{
    [Test] public void AStructureRequiresAnUnbrokenPlatformPathAndRejectsCycles()
    {
        var parents = new[] { -1, 0, 1, -2 }; var connected = new[] { true, true, true, true };
        Assert.That(NestSupportGraph.Rooted(2, parents, connected), Is.True);
        Assert.That(NestSupportGraph.Rooted(3, parents, connected), Is.False);
        connected[0] = false; Assert.That(NestSupportGraph.Rooted(2, parents, connected), Is.False);
        connected[0] = true; parents[0] = 2; Assert.That(NestSupportGraph.Rooted(2, parents, connected), Is.False);
        Assert.That(NestSupportGraph.Rooted(0, parents, new bool[1]), Is.False);
    }
    [Test] public void PickupsAndLoosePilesCannotReceiveAnEggOrEarnConstructionQuality()
    {
        var rules = new NestRules(); rules.Begin(4, true);
        for (int i = 0; i < 1000; i++) Assert.That(rules.Observe(0, false, false, 0, 0, 0, .02f), Is.EqualTo(NestMilestone.None));
        Assert.That(rules.quality[0], Is.Zero); Assert.That(rules.received[0], Is.False); Assert.That(rules.Complete, Is.False);
        foreach (float bad in new[] { float.NaN, float.PositiveInfinity, 0, -1, 1 }) rules.Observe(0, true, true, 12, 5, 2, bad);
        Assert.That(rules.quality[0], Is.Zero); Assert.That(rules.settled[0], Is.Zero);
    }
    [Test] public void PhysicalEggContactAndContinuousSupportAreRequiredForHatching()
    {
        var rules = new NestRules(); rules.Begin(1, true); int eggs = 0, hatches = 0;
        for (int i = 0; i < 180; i++) if (rules.Observe(0, true, false, 10, 4, 0, .02f) == NestMilestone.ReceiveEgg) eggs++;
        Assert.That(eggs, Is.EqualTo(1)); Assert.That(rules.received[0], Is.True);
        for (int i = 0; i < 500; i++) rules.Observe(0, true, false, 10, 4, 0, .02f);
        Assert.That(rules.incubation[0], Is.Zero); Assert.That(rules.hatched[0], Is.False);
        for (int i = 0; i < 180; i++) rules.Observe(0, true, true, 10, 4, 0, .02f);
        rules.Observe(0, false, true, 9, 4, 0, .02f); Assert.That(rules.incubation[0], Is.Zero);
        for (int i = 0; i < 260; i++) if (rules.Observe(0, true, true, 12, 5, 2, .02f) == NestMilestone.Hatch) hatches++;
        Assert.That(hatches, Is.EqualTo(1)); Assert.That(rules.Complete, Is.True); Assert.That(rules.quality[0], Is.EqualTo(460));
        rules.Observe(0, true, true, 12, 5, 2, .02f); Assert.That(rules.quality[0], Is.EqualTo(460));
    }
    [Test] public void LostEggAndBrokenStructureLoseProgressAndCompetitionRanksActualQuality()
    {
        var rules = new NestRules(); rules.Begin(4, false);
        for (int i = 0; i < 180; i++) rules.Observe(0, true, false, 10, 4, 0, .02f);
        rules.EggLost(0); Assert.That(rules.received[0], Is.False); Assert.That(rules.settled[0], Is.Zero);
        rules.Observe(0, false, false, 3, 1, 0, .02f); rules.Observe(1, true, false, 12, 5, 2, .02f);
        Assert.That(rules.quality[0], Is.EqualTo(60)); Assert.That(rules.quality[1], Is.EqualTo(310));
        Assert.That(rules.Place(1), Is.EqualTo(1)); Assert.That(rules.Place(0), Is.EqualTo(2));
        rules.Begin(1, true); Assert.That(rules.Sites, Is.EqualTo(1)); Assert.That(rules.quality.All(v => v == 0), Is.True);
        Assert.That(rules.hatched.All(v => !v), Is.True); Assert.That(rules.Complete, Is.False);
        Assert.Throws<ArgumentOutOfRangeException>(() => rules.Begin(0, true));
    }
    [Test] public void AuthoredFoliageIsClosedFiniteAndHasAnUpwardVisibleSurface()
    {
        var author = Type.GetType("LWS.TruckNestBuilding.Editor.NestAuthoring, Assembly-CSharp-Editor", true);
        var mesh = (Mesh)author.GetMethod("Foliage", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
        try
        {
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            Assert.That(mesh.vertexCount, Is.EqualTo(66)); Assert.That(mesh.triangles.Length, Is.EqualTo(384));
            Assert.That(mesh.vertices.All(v => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z)), Is.True);
            Assert.That(mesh.normals[65].y, Is.GreaterThan(.99f)); Assert.That(mesh.normals[64].y, Is.LessThan(-.99f));
            Assert.That(mesh.bounds.size.y, Is.EqualTo(1).Within(.00001f));
        }
        finally { UnityEngine.Object.DestroyImmediate(mesh); }
    }
}
