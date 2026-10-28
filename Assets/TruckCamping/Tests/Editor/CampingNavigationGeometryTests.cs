using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class CampingNavigationGeometryTests
{
    static Type Author => Type.GetType("LWS.TruckCamping.Editor.CampingWildernessAuthoring, Assembly-CSharp-Editor", true);
    static void Call(string name, TerrainData data)
    {
        try { Author.GetMethod(name).Invoke(null, new object[] { data }); }
        catch (TargetInvocationException error) { throw error.InnerException; }
    }
    static TerrainData KnownFailedTile()
    {
        var data = new TerrainData(); data.size = new Vector3(1000, 260, 1000);
        data.heightmapResolution = 257; data.alphamapResolution = 128;
        return data;
    }

    [Test] public void OriginalResolutionOrderReproducesMeasuredEightfoldTilePitchAndNewOrderRetainsOneKilometre()
    {
        var data = KnownFailedTile();
        try
        {
            Assert.That(data.heightmapResolution, Is.EqualTo(257));
            Assert.That(data.size, Is.EqualTo(new Vector3(8000, 260, 8000)), "The native failed asset stores 31.25m horizontal sample pitch, 256 intervals.");
            Call("ConfigureNewTerrainDimensions", data);
            Assert.That(data.size, Is.EqualTo(new Vector3(1000, 260, 1000)));
            Assert.That(data.heightmapScale.x, Is.EqualTo(1000f / 256)); Assert.That(data.heightmapScale.z, Is.EqualTo(1000f / 256));
            Assert.That(data.alphamapResolution, Is.EqualTo(128));
        }
        finally { UnityEngine.Object.DestroyImmediate(data); }
    }

    [Test] public void ExplicitKnownTileRepairChangesOnlyHorizontalPitchAndRetainsAllSerializedNativeContent()
    {
        var data = KnownFailedTile(); var layer = new TerrainLayer();
        try
        {
            var heights = new float[257, 257];
            for (int z = 0; z < 257; z++) for (int x = 0; x < 257; x++) heights[z, x] = (8 + x * .001f + z * .002f) / 260;
            data.SetHeights(0, 0, heights); data.terrainLayers = new[] { layer };
            var storedHeights = data.GetHeights(0, 0, 257, 257); string before = EditorJsonUtility.ToJson(data);
            Call("RepairKnownTerrainDimensions", data);
            Assert.That(data.size, Is.EqualTo(new Vector3(1000, 260, 1000))); Assert.That(data.terrainLayers[0], Is.SameAs(layer));
            CollectionAssert.AreEqual(storedHeights, data.GetHeights(0, 0, 257, 257), "Actual quantized stored height values stay exact through the size-only repair.");
            string after = EditorJsonUtility.ToJson(data);
            string canonicalBefore = System.Text.RegularExpressions.Regex.Replace(before, "\"m_Scale\":\\{[^}]*\\}", "SAME_PITCH_FIELD");
            string canonicalAfter = System.Text.RegularExpressions.Regex.Replace(after, "\"m_Scale\":\\{[^}]*\\}", "SAME_PITCH_FIELD");
            Assert.That(canonicalAfter, Is.EqualTo(canonicalBefore), "Every serialized field outside the proved scale remains exact.");
        }
        finally { UnityEngine.Object.DestroyImmediate(data); UnityEngine.Object.DestroyImmediate(layer); }
    }

    [Test] public void CustomPartialTileDimensionsAreRejectedAndPreserved()
    {
        var data = KnownFailedTile();
        try
        {
            data.size = new Vector3(7999, 260, 8000); string before = EditorJsonUtility.ToJson(data);
            Assert.Throws<InvalidOperationException>(() => Call("RepairKnownTerrainDimensions", data));
            Assert.That(EditorJsonUtility.ToJson(data), Is.EqualTo(before));
        }
        finally { UnityEngine.Object.DestroyImmediate(data); }
    }
}
