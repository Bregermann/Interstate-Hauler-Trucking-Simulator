#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public sealed class DragonKingdomCavernPhysicalAuthorityTests
{
    static Type Probe=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    [TestCase(454,6,true)][TestCase(453,6,false)][TestCase(455,6,false)][TestCase(454,5,false)][TestCase(454,7,false)]
    public void ExplicitContactInventoryNeverDropsUnsupportedCapsOrAddsUnqualifiedArea(int contacts,int portals,bool accepted)
    {
        Action call=()=>Probe.GetMethod("ValidateCavernPhysicalAuthorityInputCount").Invoke(null,new object[]{contacts,portals});
        if(accepted)Assert.DoesNotThrow(()=>call());else Assert.That(Assert.Throws<TargetInvocationException>(()=>call()).InnerException,Is.TypeOf<InvalidOperationException>());
    }
    [Test]
    public void StagingIdentityUsesExactWorldAnchorAndSameMeshRatherThanSceneWrapperSuffixOrUnrelatedWorld()
    {
        var incoming=SceneManager.GetActiveScene();bool dirty=incoming.isDirty;var preview=EditorSceneManager.NewPreviewScene();var wrapper=new GameObject("saved preview wrapper"){hideFlags=HideFlags.HideAndDontSave};SceneManager.MoveGameObjectToScene(wrapper,preview);var world=new GameObject("Shared Dragon Kingdom");world.transform.SetParent(wrapper.transform);var stage=new GameObject("Kingdom junction caverns · EasyRoads generated course");stage.transform.SetParent(world.transform);var pad=new GameObject("EasyRoads physical surface 0");pad.transform.SetParent(stage.transform);var other=new GameObject("Shared Dragon Kingdom"){hideFlags=HideFlags.HideAndDontSave};SceneManager.MoveGameObjectToScene(other,preview);var mesh=new Mesh{hideFlags=HideFlags.HideAndDontSave};mesh.vertices=new[]{Vector3.zero,Vector3.right,Vector3.forward};mesh.triangles=new[]{0,2,1};var different=UnityEngine.Object.Instantiate(mesh);var filter=pad.AddComponent<MeshFilter>();filter.sharedMesh=mesh;var collider=pad.AddComponent<MeshCollider>();collider.sharedMesh=mesh;string owner="Shared Dragon Kingdom/Kingdom junction caverns · EasyRoads generated course/EasyRoads physical surface 0";
        bool Match(Transform root,string path)=>(bool)Probe.GetMethod("ExactCavernStagingAuthority").Invoke(null,new object[]{collider,root,path});
        try{Assert.That(Match(world.transform,owner),Is.True);Assert.That(Match(other.transform,owner),Is.False,"Matching world name cannot substitute identity/ancestry");Assert.That(Match(world.transform,"saved preview wrapper/"+owner),Is.False);Assert.That(Match(world.transform,"EasyRoads physical surface 0"),Is.False);Assert.That(Match(world.transform,owner+" "),Is.False);filter.sharedMesh=different;Assert.That(Match(world.transform,owner),Is.False,"Correct hierarchy with wrong renderer mesh is rejected");filter.sharedMesh=mesh;collider.sharedMesh=null;Assert.That(Match(world.transform,owner),Is.False);}
        finally{UnityEngine.Object.DestroyImmediate(wrapper);UnityEngine.Object.DestroyImmediate(other);UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(different);EditorSceneManager.ClosePreviewScene(preview);Assert.That(SceneManager.GetActiveScene(),Is.EqualTo(incoming));Assert.That(incoming.isDirty,Is.EqualTo(dirty));}
    }
    [Test,Timeout(1800000)]
    public void ActualRetainedCave17AndQualifiedTargetRoadProveEveryOriginalOverlapWithNativeRaysAndNwhClearanceWithoutChangingSources()
    {
        Probe.GetMethod("CaptureRetainedCavernPhysicalAuthority").Invoke(null,null);var proof=Probe.GetProperty("LastCavernPhysicalAuthority").GetValue(null);object Field(string name)=>proof.GetType().GetField(name).GetValue(proof);
        foreach(string name in new[]{"passed11946SourceColumns","archive1258Exact","sourceBytesExact","voxelMembershipExact","geometryTransformsExact","physicsHeightsExact","sceneContextRestored","knownTargetCertificateExact","currentGraphMatchesFrozenMask","originalDiggerContextExact"})Assert.That((bool)Field(name),Is.True,name);
        Assert.That(((System.Collections.ICollection)Field("timingWriteErrors")).Count,Is.Zero);Assert.That(((System.Collections.ICollection)Field("stages")).Count,Is.GreaterThan(0));
        Assert.That((int)Field("overlapContacts"),Is.EqualTo(454));Assert.That((int)Field("portalContacts"),Is.EqualTo(6));Assert.That((int)Field("nwhSourceBoxes"),Is.EqualTo(2));Assert.That(((Array)Field("nwhBoxes")).Length,Is.EqualTo(2));Assert.That((int)Field("originalDiggerColliders"),Is.GreaterThan(0));Assert.That(((Array)Field("contacts")).Length,Is.EqualTo(454));
        Assert.That((bool)Field("allSixPortalContactsQualified"),Is.True,"Every previously unqualified cap must produce real saved-Digger floor/current-road contact and unchanged actual NWH/headroom clearance; inspect retained row failures.");Assert.That((bool)Field("all454ContactsQualified"),Is.True);Assert.That((int)Field("failedContacts"),Is.Zero);
        foreach(var row in (Array)Field("contacts"))
        {
            object Row(string name)=>row.GetType().GetField(name).GetValue(row);foreach(string name in new[]{"actualDiggerFloor","sourceRoadContact","existingBodyClear","actualNwhClear","qualified"})Assert.That((bool)Row(name),Is.True,name);Assert.That(((Array)Row("failures")).Length,Is.Zero);Assert.That(((Array)Row("actualNwhQueries")).Length,Is.EqualTo(2));Assert.That(((Array)Row("diggerDown")).Length,Is.GreaterThan(0));
        }
        Assert.That((string)Field("status"),Is.EqualTo("RETAINED_SOURCE_NATIVE_CONTACTS_PASSED_REQUIRES_NEW_ROOF_COMPOSITION_FULL_WORLD_DIGGER_AND_DRIVING"));
    }
}
#endif
