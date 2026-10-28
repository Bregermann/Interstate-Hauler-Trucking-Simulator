#if UNITY_EDITOR
using System;using System.Collections;using System.IO;using System.Linq;using System.Reflection;using NUnit.Framework;using UnityEditor;using UnityEngine;using UnityEngine.AI;using UnityEngine.InputSystem;using UnityEngine.TestTools;using static WildernessNativeScope;
public sealed class CampingSolidPinePhysicalNativeTests
{
    const string Owned="Assets/TruckCamping/Prefabs/Camp Haul solid JC07 pine.prefab",Old="Assets/TruckCamping/Prefabs/Camp Haul living Vista pine.prefab",Source="Assets/JC_LP_MegaCity/Prefabs/Nature/SM_Nature_Tree_07.prefab";
    static Type Author=>Type.GetType("LWS.TruckCamping.Editor.CampingSolidPineAuthoring, Assembly-CSharp-Editor",true);
    static string Digest(string p)=>(string)Author.GetMethod("Digest").Invoke(null,new object[]{p});
    static string Semantic(TerrainData data)=>(string)Author.GetMethod("Semantic").Invoke(null,new object[]{data});
    static bool[] HorizontalQueries(Vector3 centre)
    {
        var hits=new System.Collections.Generic.List<bool>();foreach(float height in new[]{1f,4f,8f,12f,15f})foreach(var axis in new[]{Vector3.forward,Vector3.back,Vector3.right,Vector3.left})hits.Add(Physics.Raycast(centre+Vector3.up*height-axis*12,axis,24,~0,QueryTriggerInteraction.Ignore));return hits.ToArray();
    }
    [UnityTest,Timeout(45000)]public IEnumerator SavedSolidPinePrototypeAddsNoTreeCollisionComparedWithRetainedZeroColliderVista()
    {
        using(var scope=new WildernessNativeScope("solid-pine-tree-physics"))
        {
            yield return scope.Load("TruckCamping","LWS.TruckCamping.CampingMatch");var owned=AssetDatabase.LoadAssetAtPath<GameObject>(Owned);var old=AssetDatabase.LoadAssetAtPath<GameObject>(Old);var source=AssetDatabase.LoadAssetAtPath<GameObject>(Source);Assert.That(owned,Is.Not.Null);Assert.That(old.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(owned.GetComponentsInChildren<Collider>(true).Length,Is.EqualTo(2));Assert.That(owned.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),Is.True);
            var root=new GameObject("TEST ONLY actual tree collider comparison");var transientData=new TerrainData[2];var centres=new[]{new Vector3(10016,0,10016),new Vector3(10096,0,10016)};
            try
            {
                for(int i=0;i<2;i++){var data=new TerrainData{heightmapResolution=33,size=new Vector3(32,1,32)};transientData[i]=data;data.treePrototypes=new[]{new TreePrototype{prefab=i==0?old:owned,bendFactor=.2f}};data.SetTreeInstances(new[]{new TreeInstance{position=new Vector3(.5f,0,.5f),heightScale=1,widthScale=1,prototypeIndex=0,color=Color.white,lightmapColor=Color.white,rotation=0}},false);var terrain=Terrain.CreateTerrainGameObject(data);terrain.transform.SetParent(root.transform,false);terrain.transform.position=centres[i]-new Vector3(16,0,16);Assert.That(terrain.GetComponent<TerrainCollider>().enabled,Is.True);}
                var positive=UnityEngine.Object.Instantiate(source,root.transform);positive.transform.position=new Vector3(10180,0,10016);positive.transform.localScale*=16.6f/((Bounds)Author.GetMethod("RenderBounds").Invoke(null,new object[]{positive})).size.y;var bounds=(Bounds)Author.GetMethod("RenderBounds").Invoke(null,new object[]{positive});positive.transform.position+=new Vector3(10180-bounds.center.x,-bounds.min.y,10016-bounds.center.z);
                yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
                Assert.That(Physics.Raycast(new Vector3(10168,8,10016),Vector3.right,24,~0,QueryTriggerInteraction.Ignore),Is.True,"The real enabled original JC07 collider is the native positive query control, never a fabricated collider.");
                for(int i=0;i<2;i++)Assert.That(Physics.Raycast(centres[i]+Vector3.up*20,Vector3.down,out var ground,25,~0,QueryTriggerInteraction.Ignore)&&ground.collider is TerrainCollider,Is.True,"Actual transient native TerrainCollider is active beneath each tree.");
                var before=HorizontalQueries(centres[0]);var after=HorizontalQueries(centres[1]);Assert.That(before,Is.All.False,"Retained Vista prototype is the actual zero-physics baseline.");CollectionAssert.AreEqual(before,after,"Two disabled colliders must not create ANY added actual terrain-tree support across20native trunk/canopy queries.");
                TestContext.WriteLine("SOLID_PINE_NATIVE_COLLISION actualTerrainCollider+treePrototype comparison20heights/directions each; oldVista0colliders/newJC07twoDISABLED; positive originalenabledJC07 collider hit; no productionterrain/prototype/body/query-filter change.");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);foreach(var data in transientData)if(data!=null)UnityEngine.Object.DestroyImmediate(data);}
        }
    }
    [UnityTest,Timeout(60000)]public IEnumerator CampingAndPetPalsReuseSameFourTerrainAssetsNavAndActualOriginalNwhDrivingWithNewPine()
    {
        string[] expectedGuids={"ab66ebbed2d5de042a26d66937d9ef41","536d3ebb9c605dc4c8125f50b46b7dbe","41b2c3577830ed9498c578c32d6b4553","aa4f37bee36f7cb4fb33f3c3f9d8ec8e"};
        const string shared="Assets/TruckCamping/Prefabs/Camp Haul shared wilderness.prefab",nav="Assets/TruckCamping/Models/Camp Haul connected woodland navigation.asset";string worldHash=Digest(shared),navHash=Digest(nav);
        foreach(string mode in new[]{"TruckCamping","TruckPetPals"})using(var scope=new WildernessNativeScope("solid-pine-shared-"+mode))
        {
            yield return scope.Load(mode,mode=="TruckCamping"?"LWS.TruckCamping.CampingMatch":"LWS.TruckPetPals.PetPalsMatch");var world=(Component)Get(scope.match,"world");var terrains=(Terrain[])Get(world,"terrains");var data=terrains.Select(t=>t.terrainData).ToArray();Assert.That(data.Length,Is.EqualTo(4));CollectionAssert.AreEqual(expectedGuids,data.Select(d=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(d))));Assert.That(data.Sum(d=>d.treeInstanceCount),Is.EqualTo(2297));foreach(var tile in data)Assert.That(AssetDatabase.GetAssetPath(tile.treePrototypes[0].prefab),Is.EqualTo(Owned));
            string yaml=File.ReadAllText("Assets/"+mode+"/Scenes/"+mode+".unity");Assert.That(System.Text.RegularExpressions.Regex.Matches(yaml,@"m_SourcePrefab: \{fileID: 100100000, guid: 2ec3264f79b522a49bcddfec51fc1cda, type: 3\}").Count,Is.EqualTo(1),"Actual saved scene binds exactly the original SAME sharedworld prefab; PlayMode PrefabUtility instance-path API is not reliable.");
            var semantics=data.Select(Semantic).ToArray();var colliders=world.GetComponentsInChildren<Collider>(true).Select(c=>c.GetInstanceID()).OrderBy(i=>i).ToArray();foreach(var point in (Transform[])Get(world,"route"))Assert.That(NavMesh.SamplePosition(point.position,out var hit,8,NavMesh.AllAreas),Is.True,"Original8m nav query preserved at "+point.parent.name);
            scope.Configure(1);Assert.That((bool)Call(scope.match,"StartMatch"),Is.True);yield return scope.Advance(1.2f);var actors=Components(Get(scope.match,"explorers"));var trucks=actors.Select(e=>(Component)Get(e,"truck")).ToArray();var physical=trucks.Select(PhysicsIdentity).ToArray();var body=(Rigidbody)Get(trucks[0],"body");var start=body.position;var player=((Array)Get(Get(scope.match,"setup"),"players")).GetValue(0);scope.Keys((Key)Get(player,"forward"));yield return scope.Advance(2.5f);scope.Keys();Assert.That(Vector3.Distance(start,body.position),Is.GreaterThan(.5f),"Real test-owned keyboard moves original NWH truck through unchanged sharedworld physics, not a pose write.");
            CollectionAssert.AreEqual(physical,trucks.Select(PhysicsIdentity));CollectionAssert.AreEqual(colliders,world.GetComponentsInChildren<Collider>(true).Select(c=>c.GetInstanceID()).OrderBy(i=>i));CollectionAssert.AreEqual(semantics,data.Select(Semantic));Assert.That(Digest(shared),Is.EqualTo(worldHash));Assert.That(Digest(nav),Is.EqualTo(navHash));Assert.That(world.GetComponentsInChildren<Transform>(true).Count(t=>t.name.StartsWith("Actual fallen Whitebark pine ",StringComparison.Ordinal)),Is.EqualTo(9));
            TestContext.WriteLine("SOLID_PINE_SHARED_PHYSICS "+mode+" same4terrainGUIDs/2297trees/nav12/serializedsharedprefab+originalcolliderIDs+NWHauthoredconfig; actualmachine-keyboarddisplacement="+Vector3.Distance(start,body.position).ToString("R")+". No human/hardware/performance claim.");
        }
    }
}
#endif
