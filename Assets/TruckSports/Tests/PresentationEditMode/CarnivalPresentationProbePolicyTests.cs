#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class CarnivalPresentationProbePolicyTests
{
    static Type Probe => Type.GetType("LWS.TruckSports.SportsCarnivalPresentationWindowsProbe, Assembly-CSharp", true);
    static Type Replay => Type.GetType("LWS.TruckSports.SportsMotionReplay, Assembly-CSharp", true);
    static object Call(Type type, string name, object owner, params object[] args) => type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).Invoke(owner, args);
    static void Set(Component owner, string name, object value) => owner.GetType().GetField(name).SetValue(owner, value);

    [Test]
    public void AssertionFailuresRetainFailureExitButOnlyActualRuntimeFloodStopsRemainingVenues()
    {
        float priorScale = Time.timeScale; var root = new GameObject("Synthetic opt-in probe counters");
        try
        {
            var probe = root.AddComponent(Probe);
            for (int i = 0; i < 30; i++) Call(Probe, "Check", probe, false, "synthetic retained failure " + i);
            var fields = BindingFlags.Instance | BindingFlags.NonPublic;
            Assert.That(Probe.GetField("errors", fields).GetValue(probe), Is.EqualTo(30));
            Assert.That(Probe.GetField("runtimeErrors", fields).GetValue(probe), Is.EqualTo(0));
            Assert.That(Call(Probe, "AbortForRuntimeFlood", null, 0), Is.False);
            for (int i = 0; i < 12; i++) Call(Probe, "Error", probe, "synthetic runtime exception", "synthetic stack", LogType.Exception);
            Assert.That(Probe.GetField("errors", fields).GetValue(probe), Is.EqualTo(42));
            Assert.That(Call(Probe, "AbortForRuntimeFlood", null, (int)Probe.GetField("runtimeErrors", fields).GetValue(probe)), Is.False);
            Call(Probe, "Error", probe, "thirteenth actual runtime error", "", LogType.Error);
            Assert.That(Probe.GetField("errors", fields).GetValue(probe), Is.EqualTo(43));
            Assert.That(Call(Probe, "AbortForRuntimeFlood", null, (int)Probe.GetField("runtimeErrors", fields).GetValue(probe)), Is.True);
            Call(Probe, "Error", probe, "ordinary warning", "", LogType.Warning);
            Assert.That(Probe.GetField("runtimeErrors", fields).GetValue(probe), Is.EqualTo(13));
        }
        finally { UnityEngine.Object.DestroyImmediate(root); Time.timeScale = priorScale; }
    }

    [Test]
    public void ActualMeshPredicateRejectsEnabledZeroScalePoolWithoutDroppingLiveComposedGeometry()
    {
        var live = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            var renderer = live.GetComponent<Renderer>();
            Assert.That(Call(Probe, "ActualRenderedMesh", null, renderer), Is.True);
            live.transform.localScale = Vector3.zero;
            Assert.That(renderer.enabled && live.activeInHierarchy, Is.True, "The old predicate wrongly accepted this pooled fragment.");
            Assert.That(Call(Probe, "ActualRenderedMesh", null, renderer), Is.False);
            live.transform.localScale = Vector3.one; live.SetActive(false);
            Assert.That(Call(Probe, "ActualRenderedMesh", null, renderer), Is.False);
        }
        finally { UnityEngine.Object.DestroyImmediate(live); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void HiddenLiveSelectedBodyRequiresMatchingOriginalRecordedHierarchyMaterialsAndPerSlotPaint(bool wrongOwnerSource)
    {
        var source = new GameObject("Original replay truck source"); var owner = new GameObject("Synthetic original replay");
        GameObject ghost = null; Material changed = null;
        try
        {
            var model = new GameObject("Selected hotdog body"); model.transform.SetParent(source.transform, false);
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube); part.name = "Actual selected mesh"; part.transform.SetParent(model.transform, false);
            var original = part.GetComponent<Renderer>(); var paint = new MaterialPropertyBlock(); paint.SetColor("_BaseColor", Color.magenta); paint.SetColor("_Color", Color.magenta); original.SetPropertyBlock(paint, 0);
            var copy = Type.GetType("LWS.TruckSports.SportsReplayVisuals, Assembly-CSharp", true);
            ghost = (GameObject)Call(copy, "Copy", null, source.transform);
            var replay = owner.AddComponent(Replay); Set(replay, "sources", new[] { source.transform }); Set(replay, "ghosts", new[] { ghost.transform });
            Call(Replay, "ResetRecord", replay); Call(Replay, "Record", replay, 0f, 1); Call(Replay, "Record", replay, .1f, 1); Call(Replay, "Begin", replay, 0f, float.PositiveInfinity);
            source.SetActive(false); Assert.That(original.gameObject.activeInHierarchy, Is.False);
            var args = new object[] { replay, model.transform, new[] { original }, null, null };
            Assert.That(Call(Probe, "TryRecordedAppearance", null, args), Is.True, args[4]?.ToString());
            var rendered = (Renderer[])args[3]; Assert.That(rendered.Length, Is.EqualTo(1)); Assert.That(rendered[0].gameObject.activeInHierarchy, Is.True);
            Assert.That(rendered[0].GetComponent<MeshFilter>().sharedMesh, Is.SameAs(original.GetComponent<MeshFilter>().sharedMesh));
            if (wrongOwnerSource)
            {
                // A different replay source must not pass merely because the old
                // selected ghost is still visibly active elsewhere in the scene.
                var other = new GameObject("Other owner source"); other.transform.SetParent(owner.transform);
                Set(replay, "sources", new[] { other.transform });
                Assert.That(Call(Probe, "TryRecordedAppearance", null, args), Is.False);
            }
            else
            {
                paint.SetColor("_BaseColor", Color.cyan); rendered[0].SetPropertyBlock(paint, 0);
                Assert.That(Call(Probe, "TryRecordedAppearance", null, args), Is.False, "Another participant's indexed paint must fail.");
                original.GetPropertyBlock(paint, 0); rendered[0].SetPropertyBlock(paint, 0);
                changed = new Material(original.sharedMaterial); rendered[0].sharedMaterial = changed;
                Assert.That(Call(Probe, "TryRecordedAppearance", null, args), Is.False, "Different shared art must fail even with matching paint.");
                rendered[0].sharedMaterials = original.sharedMaterials;
                rendered[0].GetComponent<MeshFilter>().sharedMesh = null;
                Assert.That(Call(Probe, "TryRecordedAppearance", null, args), Is.False, "Missing selected geometry must fail.");
            }
        }
        finally
        {
            if (ghost != null) UnityEngine.Object.DestroyImmediate(ghost);
            UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(owner);
            if (changed != null) UnityEngine.Object.DestroyImmediate(changed);
        }
    }
}
#endif
