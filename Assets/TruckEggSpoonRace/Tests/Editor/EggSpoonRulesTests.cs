using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using LWS.TruckEggSpoonRace;

public sealed class EggSpoonRulesTests
{
    static void Gates(EggSpoonRules r, int slot) { for (int i = 0; i < 3; i++) Assert.That(r.Cross(slot, i), Is.True); }
    [Test] public void OnlyOrderedIntactEggsCanSecureAndFinishAndSpillsPenalizeOnce()
    {
        var r = new EggSpoonRules(); r.Begin(2, 0);
        Assert.That(r.Cross(0, 1), Is.False); Assert.That(r.Finish(0, 10), Is.False);
        Assert.That(r.Cross(0, 0), Is.True); Assert.That(r.Spill(0), Is.True); Assert.That(r.Spill(0), Is.False);
        Assert.That(r.Cross(0, 1), Is.False); Assert.That(r.spills[0], Is.EqualTo(1));
        Assert.That(r.Replace(0, false, true, 3), Is.False); Assert.That(r.Replace(0, true, false, 3), Is.False); Assert.That(r.Replace(0, true, true, 2.9f), Is.False); Assert.That(r.Replace(0, true, true, float.NaN), Is.False);
        Assert.That(r.Replace(0, true, true, 3), Is.True); Assert.That(r.replacements[0], Is.EqualTo(1));
        Assert.That(r.Cross(0, 1), Is.True); Assert.That(r.Cross(0, 2), Is.True); Assert.That(r.Finish(0, 50), Is.True); Assert.That(r.time[0], Is.EqualTo(58));
        Assert.That(r.Finish(0, 1), Is.False); Assert.That(r.Spill(0), Is.False);
        Gates(r, 1); Assert.That(r.Finish(1, 59), Is.True); Assert.That(r.Place(0), Is.EqualTo(1)); Assert.That(r.bestClean[0], Is.EqualTo(float.PositiveInfinity)); Assert.That(r.bestClean[1], Is.EqualTo(59));
    }
    [TestCase(1)] [TestCase(2)]
    public void CleanOrFewestSpillsFormatPreservesActualPhysicalOutcomes(int format)
    {
        var r = new EggSpoonRules(); r.Begin(2, format); r.Spill(0); r.Replace(0, true, true, 3); Gates(r, 0); Gates(r, 1);
        r.Finish(0, 10); r.Finish(1, 35);
        Assert.That(r.Place(1), Is.EqualTo(1), "A faster spilled run cannot override the selected clean/fewest-spills objective."); Assert.That(r.Place(0), Is.EqualTo(2));
    }
    [Test] public void FailedOrInvalidTimesCannotBeatActualFinishAndRematchResetsAllOwners()
    {
        var r = new EggSpoonRules(); r.Begin(4, 0); Gates(r, 0);
        Assert.That(r.Finish(0, float.NaN), Is.False); Assert.That(r.Finish(0, -1), Is.False); Assert.That(r.Finish(0, 181), Is.False);
        Assert.That(r.Finish(0, 80), Is.True); for (int i = 1; i < 4; i++) r.Fail(i);
        Assert.That(r.Complete, Is.True); Assert.That(r.Place(0), Is.EqualTo(1)); r.Begin(1, 2);
        Assert.That(r.Complete, Is.False); Assert.That(r.spills.All(x => x == 0), Is.True); Assert.That(r.checkpoint.All(x => x == 0), Is.True);
    }
    [Test] public void RealSpoonMeshIsConcaveAndCheckpointCrossingRejectsReverseAndWrongLane()
    {
        var author = Type.GetType("LWS.TruckEggSpoonRace.Editor.EggSpoonAuthoring, Assembly-CSharp-Editor", true);
        var mesh = (Mesh)author.GetMethod("BowlMesh").Invoke(null, null);
        try
        {
            mesh.RecalculateBounds(); mesh.RecalculateNormals(); Assert.That(mesh.vertexCount, Is.GreaterThan(800));
            Assert.That(mesh.vertices.Where(v => new Vector2(v.x, v.z - 5).magnitude < .05f).Max(v => v.y), Is.EqualTo(0).Within(.0001f));
            Assert.That(mesh.bounds.max.y, Is.GreaterThan(.7f)); Assert.That(mesh.bounds.size.z, Is.EqualTo(4.4f).Within(.001f));
            Assert.That(mesh.normals.All(v => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z)), Is.True);
        }
        finally { UnityEngine.Object.DestroyImmediate(mesh); }
        var course = Type.GetType("LWS.TruckEggSpoonRace.EggSpoonCourse, Assembly-CSharp", true); var crossing = course.GetMethod("Crossed", BindingFlags.Public | BindingFlags.Static);
        var point = course.GetMethod("Point"); var before = (Vector3)point.Invoke(null, new object[] { 0, 44f }); var after = (Vector3)point.Invoke(null, new object[] { 0, 46f });
        Assert.That((bool)crossing.Invoke(null, new object[] { before, after, 45f, 0 }), Is.True);
        Assert.That((bool)crossing.Invoke(null, new object[] { after, before, 45f, 0 }), Is.False);
        Assert.That((bool)crossing.Invoke(null, new object[] { before + Vector3.right * 20, after + Vector3.right * 20, 45f, 0 }), Is.False);
    }
}
