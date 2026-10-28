using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
public sealed class CampingLandmarkRockTests
{
    static Type Author=>Type.GetType("LWS.TruckCamping.Editor.CampingLandmarkRockAuthoring, Assembly-CSharp-Editor",true);
    static object Call(string method,params object[] args)=>Author.GetMethod(method).Invoke(null,args);
    const string Shared="Assets/TruckCamping/Prefabs/Camp Haul shared wilderness.prefab";
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object value,string name)=>value.GetType().GetField(name,Any).GetValue(value);
    [Test] public void RealClosedBoulderProfilePreservesAuthoredBasisAndHasSeventeenUniformSolidSourceBouldersWithoutAssetWrites()
    {
        string before=(string)Call("Digest",Shared);var worldType=Type.GetType("LWS.TruckCamping.CampingWilderness, Assembly-CSharp",true);var root=PrefabUtility.LoadPrefabContents(Shared);ScriptableObject profile=null;
        try
        {
            var world=root.GetComponent(worldType);profile=(ScriptableObject)Call("CreatePreviewProfile",world);var targets=(Array)Get(profile,"targets");Assert.That(targets.Length,Is.EqualTo(5));
            var prefab=(GameObject)Get(profile,"boulderPrefab");Assert.That(prefab.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(prefab.GetComponentsInChildren<Rigidbody>(true),Is.Empty);Assert.That(prefab.GetComponentsInChildren<MonoBehaviour>(true),Is.Empty);
            var mesh=(Mesh)Get(profile,"originalRockAMesh");Assert.That(AssetDatabase.GetAssetPath(mesh),Is.EqualTo("Assets/Toby Fredson/Rocky Hills Environment - Whitebark Pine/RHEWP_Demo/Meshes/Meshes_Static/RockBigRHEWP.fbx"));
            var material=(Material)Get(profile,"ownedMaterial");Assert.That(material.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));Assert.That(material.GetTexture("_BaseMap"),Is.Not.Null);Assert.That(AssetDatabase.GetAssetPath(material.GetTexture("_BaseMap")),Does.StartWith("Assets/Toby Fredson/Rocky Hills Environment - Whitebark Pine/"));
            Assert.That((string)Call("VerifyActualSourceNormalization",profile),Does.Contain("weldedBoundaryEdges=0"));Assert.That((int)Call("ClosedBoundaryCount",mesh),Is.Zero);Assert.That(material.GetTexture("_BumpMap"),Is.Not.Null,"Genuine original rock normal map retained.");
            int rocks=0;foreach(var target in targets){var poses=(Array)Get(target,"boulders");Assert.That(poses.Length,Is.InRange(3,5));rocks+=poses.Length;foreach(var pose in poses)Assert.That((float)Get(pose,"uniformScale"),Is.GreaterThan(0));}Assert.That(rocks,Is.EqualTo(17));
            var adapterType=Type.GetType("LWS.TruckCamping.CampLandmarkRockPresentation, Assembly-CSharp",true);var adapter=root.AddComponent(adapterType);Assert.That((bool)adapterType.GetMethod("Apply").Invoke(adapter,new object[]{world,profile}),Is.True,adapterType.GetProperty("Diagnostic").GetValue(adapter) as string);
            string physicalGeometry=(string)Call("VerifyActualRenderedPlacement",world,profile,adapterType.GetProperty("VisualRoot").GetValue(adapter));Assert.That(physicalGeometry,Does.Contain("jointInsideWitness="));Assert.That(physicalGeometry,Does.Contain("realSolidRoofBridge=true"));adapterType.GetMethod("Release").Invoke(adapter,null);
            Assert.That((string)Call("Digest",Shared),Is.EqualTo(before));
        }
        finally {if(profile!=null)Call("DestroyPreview",profile);PrefabUtility.UnloadPrefabContents(root);}
    }
    [Test] public void CustomizedOriginalMeshIsRefusedWithoutRepairOrOriginalWorldAssetMutation()
    {
        string before=(string)Call("Digest",Shared);var worldType=Type.GetType("LWS.TruckCamping.CampingWilderness, Assembly-CSharp",true);var adapterType=Type.GetType("LWS.TruckCamping.CampLandmarkRockPresentation, Assembly-CSharp",true);
        var root=PrefabUtility.LoadPrefabContents(Shared);ScriptableObject profile=null;
        try
        {
            var world=root.GetComponent(worldType);profile=(ScriptableObject)Call("CreatePreviewProfile",world);var adapter=root.AddComponent(adapterType);var validate=adapterType.GetMethod("Validate");
            var args=new object[]{world,profile,null};Assert.That((bool)validate.Invoke(adapter,args),Is.True,args[2] as string);
            var first=((Array)Get(profile,"targets")).GetValue(0);Transform target=root.transform;foreach(int sibling in (int[])Get(first,"siblingPath"))target=target.GetChild(sibling);
            var filter=target.GetComponent<MeshFilter>();var original=filter.sharedMesh;filter.sharedMesh=null;
            args=new object[]{world,profile,null};Assert.That((bool)validate.Invoke(adapter,args),Is.False);Assert.That(args[2] as string,Does.Contain("Customized original"));Assert.That(filter.sharedMesh,Is.Null,"No repair or hidden substitution of designer changes.");
            Assert.That((bool)adapterType.GetProperty("Applied").GetValue(adapter),Is.False);filter.sharedMesh=original;Assert.That((string)Call("Digest",Shared),Is.EqualTo(before));
        }
        finally {if(profile!=null)Call("DestroyPreview",profile);PrefabUtility.UnloadPrefabContents(root);}
    }
    [Test] public void RoofSupportUsesActualTriangleInsideFootprintWhenItsCentreIsVoidAndRefusesUnreachableGeometry()
    {
        var roof=new[]{new Vector3(-2,8,1),new Vector3(0,8,1),new Vector3(-2,8,2)};var triangles=new[]{0,1,2};var footprint=new Bounds(new Vector3(-1,0,0),new Vector3(8,10,8));
        var missed=Assert.Throws<TargetInvocationException>(()=>Call("VerticalSurfaceInterval",roof,triangles,-1f,0f));Assert.That(missed.InnerException.Message,Does.Contain("XZ="));
        var point=(Vector3)Call("ChooseActualRoofSupport",roof,triangles,footprint,"test irregular cave roof");Assert.That(point.y,Is.EqualTo(8).Within(.0001f));Assert.That(point.z,Is.GreaterThan(1));Assert.That(point.x,Is.InRange(-2,0));var actual=(Vector2)Call("VerticalSurfaceInterval",roof,triangles,point.x,point.z);Assert.That(actual.x,Is.EqualTo(point.y).Within(.0001f));
        var unavailable=Assert.Throws<TargetInvocationException>(()=>Call("ChooseActualRoofSupport",roof,triangles,new Bounds(Vector3.one*100,new Vector3(2,2,2)),"unreachable-original-pillar"));Assert.That(unavailable.InnerException.Message,Does.Contain("unreachable-original-pillar"));Assert.That(unavailable.InnerException.Message,Does.Contain("no REAL roof triangle"));
    }

}
