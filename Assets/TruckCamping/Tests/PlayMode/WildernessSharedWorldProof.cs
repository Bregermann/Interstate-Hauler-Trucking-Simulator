#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using static WildernessNativeScope;
// Runtime prefab connection handles are not retained by the player. Prove the
// actual shared asset references, with saved Editor prefab identity tested apart.
public static class WildernessSharedWorldProof
{
    public const string Shared="Assets/TruckCamping/Prefabs/Camp Haul shared wilderness.prefab";
    public const string SharedGuid="2ec3264f79b522a49bcddfec51fc1cda";
    public static void AssertActualSharedAssetBindings(Component world)
    {
        Assert.That(AssetDatabase.AssetPathToGUID(Shared),Is.EqualTo(SharedGuid));var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Shared);Assert.That(prefab,Is.Not.Null);
        var source=prefab.GetComponent(Runtime("LWS.TruckCamping.CampingWilderness"));Assert.That(source,Is.Not.Null);
        var terrain=(Terrain[])Get(world,"terrains");var original=(Terrain[])Get(source,"terrains");Assert.That(terrain.Length,Is.EqualTo(4));Assert.That(original.Length,Is.EqualTo(4));
        for(int i=0;i<4;i++){Assert.That(terrain[i].terrainData,Is.SameAs(original[i].terrainData),"Actual mode uses the original shared saved TerrainData, not a second generated forest.");Assert.That(AssetDatabase.GetAssetPath(terrain[i].terrainData),Does.StartWith("Assets/TruckCamping/Models/"));}
        var surface=Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation",true);var actual=world.GetComponentsInChildren(surface,true);var expected=source.GetComponentsInChildren(surface,true);Assert.That(actual.Length,Is.EqualTo(1));Assert.That(expected.Length,Is.EqualTo(1));Assert.That(Get(actual[0],"navMeshData"),Is.SameAs(Get(expected[0],"navMeshData")));
        var places=Components(Get(world,"landmarks"));var old=Components(Get(source,"landmarks"));Assert.That(places.Length,Is.EqualTo(12));
        for(int i=0;i<12;i++){Assert.That(Get(places[i],"index"),Is.EqualTo(Get(old[i],"index")));Assert.That(Get(places[i],"kind"),Is.EqualTo(Get(old[i],"kind")));}
        TestContext.WriteLine("WILDERNESS_SHARED_RUNTIME actual4TerrainData+1NavMeshData+12landmarks match pinned shared prefab GUID"+SharedGuid+"; saved Editor prefab connection is verified separately.");
    }
}
#endif
