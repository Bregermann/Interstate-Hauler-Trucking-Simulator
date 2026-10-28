using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class CampingRoadPreservationTests
{
    static Type Author => Type.GetType("LWS.TruckSports.Editor.SportsEasyRoadsAuthoring, Assembly-CSharp-Editor", true);
    static Mesh Persist(Mesh mesh, string path, bool preserve)
    {
        try { return (Mesh)Author.GetMethod("Persist", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { mesh, path, preserve }); }
        catch (TargetInvocationException error) { throw error.InnerException; }
    }
    static Mesh Triangle(float x = 0)
    {
        var mesh = new Mesh { name = "Temporary real EasyRoads persistence regression" };
        mesh.vertices = new[] { new Vector3(x, 0, 0), new Vector3(x + 1, 0, 0), new Vector3(x, 0, 1) };
        mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up };
        mesh.tangents = new[] { new Vector4(1, 0, 0, 1), new Vector4(1, 0, 0, 1), new Vector4(1, 0, 0, 1) };
        mesh.colors = new[] { Color.red, Color.green, Color.blue };
        mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up };
        mesh.SetUVs(4, new[] { new Vector4(1, 2, 3, 4), new Vector4(2, 3, 4, 5), new Vector4(3, 4, 5, 6) });
        mesh.triangles = new[] { 0, 2, 1 }; mesh.RecalculateBounds(); return mesh;
    }
    static string Folder()
    {
        string folder = "Assets/TemporaryCampingRoadProof_" + Guid.NewGuid().ToString("N");
        AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder)); return folder;
    }

    [Test] public void ExplicitPreservationReturnsSameAssetWithExactBytesGuidAndDirtyState()
    {
        string folder = Folder(), path = folder + "/road.asset"; var source = Triangle();
        try
        {
            var existing = Persist(source, path, false); AssetDatabase.SaveAssets();
            var bytes = File.ReadAllBytes(path); string guid = AssetDatabase.AssetPathToGUID(path); bool dirty = EditorUtility.IsDirty(existing);
            Assert.That(Persist(source, path, true), Is.SameAs(existing));
            Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guid));
            Assert.That(EditorUtility.IsDirty(existing), Is.EqualTo(dirty));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(path));
        }
        finally { UnityEngine.Object.DestroyImmediate(source); AssetDatabase.DeleteAsset(folder); }
    }

    [Test] public void ChangedHigherUvOrTopologyRejectsWithoutMutatingExistingAsset()
    {
        string folder = Folder(), path = folder + "/road.asset"; var source = Triangle();
        try
        {
            var existing = Persist(source, path, false); AssetDatabase.SaveAssets();
            var bytes = File.ReadAllBytes(path); string guid = AssetDatabase.AssetPathToGUID(path); var original = source.GetIndices(0);
            source.SetUVs(4, new[] { Vector4.zero, Vector4.one, Vector4.zero });
            StringAssert.Contains("UV channel 4", Assert.Throws<InvalidOperationException>(() => Persist(source, path, true)).Message);
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(path));
            source.SetUVs(4, new[] { new Vector4(1, 2, 3, 4), new Vector4(2, 3, 4, 5), new Vector4(3, 4, 5, 6) });
            source.SetIndices(new[] { 0, 1, 2 }, MeshTopology.Triangles, 0);
            StringAssert.Contains("topology/indices", Assert.Throws<InvalidOperationException>(() => Persist(source, path, true)).Message);
            CollectionAssert.AreEqual(original, existing.GetIndices(0));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(path)); Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guid));
        }
        finally { UnityEngine.Object.DestroyImmediate(source); AssetDatabase.DeleteAsset(folder); }
    }

    [Test] public void OrdinaryDefaultPersistenceStillUpdatesExistingGeometryAndRetainsGuid()
    {
        string folder = Folder(), path = folder + "/road.asset"; var source = Triangle(); var changed = Triangle(3);
        try
        {
            var existing = Persist(source, path, false); AssetDatabase.SaveAssets(); string guid = AssetDatabase.AssetPathToGUID(path);
            Assert.That(Persist(changed, path, false), Is.SameAs(existing));
            CollectionAssert.AreEqual(changed.vertices, existing.vertices); CollectionAssert.AreEqual(changed.GetIndices(0), existing.GetIndices(0));
            Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guid));
        }
        finally { UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(changed); AssetDatabase.DeleteAsset(folder); }
    }
}
