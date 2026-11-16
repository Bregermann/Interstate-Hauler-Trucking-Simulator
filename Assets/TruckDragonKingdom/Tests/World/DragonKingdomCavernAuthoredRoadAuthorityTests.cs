#if UNITY_EDITOR
using System;
using System.Reflection;
using System.IO;
using System.Linq;
using UnityEditor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DragonKingdomCavernAuthoredRoadAuthorityTests
{
    static Type Probe=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
    [TestCase("NONE",true)][TestCase("NO_PAVEMENT",false)][TestCase("PAVEMENT_ONE_METER_FROM_AUTHORING",false)][TestCase("GENUINE_LOW_ROOF",false)][TestCase("OTHER_ROAD_DECK",false)][TestCase("SOLID_DIGGER_ABOVE_ROAD",false)]
    public void NativeRoadSupportAllowsLowerCaveFloorButRejectsAbsentPavementRaisedDeckAndRealObstructions(string mutation,bool expectedPass)
    {
        // Editor preview scenes do not participate in the global Physics queries
        // used by the production helper. Save ONLY the clean, batch-created
        // untitled harness, then use a real additive/default-physics scene.
        // Never save dirty or interactive untitled user work.
        const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";
        var incoming=SceneManager.GetActiveScene();
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var existing=SceneManager.GetSceneAt(i);if(!string.IsNullOrEmpty(existing.path))continue;
            if(!Application.isBatchMode||existing.isDirty)Assert.Ignore("Native additive physics control requires a saved scene; preserving interactive or dirty untitled user scene.");
            Directory.CreateDirectory(generated);AssetDatabase.Refresh();string harness=generated+"CavernRoadPhysicsRunner-"+Guid.NewGuid().ToString("N")+".unity";Assert.That(EditorSceneManager.SaveScene(existing,harness),Is.True,"Save only the clean isolated batch runner, never user work");TestContext.WriteLine("BATCH_ONLY_NATIVE_PHYSICS_RUNNER "+harness);
        }
        string Context()=>string.Join("|",Enumerable.Range(0,SceneManager.sceneCount).Select(i=>{var x=SceneManager.GetSceneAt(i);return x.handle+":"+x.path+":"+x.isLoaded+":"+x.isDirty;}));
        string context=Context();bool dirty=incoming.isDirty;var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);GameObject root=null;
        try
        {
            root=new GameObject("Temporary authored pavement versus lower cave floor control"){hideFlags=HideFlags.HideAndDontSave};SceneManager.MoveGameObjectToScene(root,scene);root.transform.position=new Vector3(10000,0,10000);
            Assert.That(scene.GetPhysicsScene(),Is.EqualTo(Physics.defaultPhysicsScene),"The registered scene must be queried by the helper's actual global PhysX API");
            BoxCollider Box(string name,Vector3 center,Vector3 size){var g=new GameObject(name){hideFlags=HideFlags.HideAndDontSave};g.transform.SetParent(root.transform,false);g.transform.localPosition=center;var b=g.AddComponent<BoxCollider>();b.size=size;return b;}
            var road=Box("actual supporting pavement",new Vector3(0,104.95f,0),new Vector3(60,.1f,60));var floor=Box("separate actual lower Digger surface",new Vector3(0,101.95f,0),new Vector3(60,.1f,60));
            var type=Probe.GetNestedType("CavernNwhBoxSource",BindingFlags.Public);var boxes=Array.CreateInstance(type,2);for(int i=0;i<2;i++){var b=Activator.CreateInstance(type);type.GetField("path").SetValue(b,"Representative native control box "+i+"; actual original source NWH is required by the separate full fixture");type.GetField("rootRelative").SetValue(b,Matrix4x4.identity);type.GetField("center").SetValue(b,new Vector3(0,i==0?.8f:2,0));type.GetField("size").SetValue(b,i==0?new Vector3(2,.6f,5):new Vector3(2,2,2));type.GetField("floorLift").SetValue(b,1f);boxes.SetValue(b,i);}
            object Measure(Vector3 expected){Physics.SyncTransforms();return Probe.GetMethod("MeasureCavernAuthoredRoadContact").Invoke(null,new object[]{root.transform,expected,Vector3.forward,new Collider[]{road},new Collider[]{floor},boxes,null});}
            object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
            var normalPoint=new Vector3(10000,105,10000);var baseline=Measure(normalPoint);TestContext.WriteLine("NATIVE_REGISTERED_PHYSICS_POSITIVE_BASELINE "+JsonUtility.ToJson(baseline));
            foreach(string gate in new[]{"qualified","roadSupportPresent","diggerFloorPresent","sixMeterClear","actualNwhClear"})Assert.That((bool)Field(baseline,gate),Is.True,"Positive baseline must really query support before negative mutation: "+gate);
            Assert.That((string[])Field(baseline,"failures"),Is.Empty);Assert.That(((Array)Field(baseline,"nwhQueries")).Length,Is.EqualTo(2));
            var floorHits=(Array)Field(baseline,"diggerDown");Assert.That(floorHits.Length,Is.GreaterThan(0));Assert.That(((Vector3)Field(floorHits.GetValue(0),"point")).y,Is.EqualTo(102).Within(.01f),"The native positive has a genuinely lower cave floor");
            switch(mutation)
            {
                case "NO_PAVEMENT":road.enabled=false;break;
                case "GENUINE_LOW_ROOF":Box("genuine low ceiling obstacle",new Vector3(0,107.5f,0),new Vector3(10,.2f,10));break;
                case "OTHER_ROAD_DECK":Box("other road deck is NOT exempt",new Vector3(0,105.5f,0),new Vector3(10,.1f,10));break;
                case "SOLID_DIGGER_ABOVE_ROAD":floor.transform.localPosition=new Vector3(0,105.95f,0);break;
            }
            var point=mutation=="PAVEMENT_ONE_METER_FROM_AUTHORING"?new Vector3(10000,104,10000):normalPoint;var proof=Measure(point);var failures=(string[])Field(proof,"failures");TestContext.WriteLine("NATIVE_REGISTERED_PHYSICS_MUTATION "+mutation+" "+JsonUtility.ToJson(proof));Assert.That((bool)Field(proof,"qualified"),Is.EqualTo(expectedPass),string.Join(",",failures));
            if(expectedPass)Assert.That(failures,Is.Empty);
            else if(mutation=="NO_PAVEMENT"||mutation=="PAVEMENT_ONE_METER_FROM_AUTHORING")
            {
                Assert.That(failures,Does.Contain("NO_NATIVE_PAVEMENT_SUPPORT_WITHIN_EXISTING_POINT6_METERS"));Assert.That((bool)Field(proof,"roadSupportPresent"),Is.False);if(mutation=="PAVEMENT_ONE_METER_FROM_AUTHORING")Assert.That((float)Field(proof,"roadSupportError"),Is.EqualTo(1).Within(.01f),"This is real offset pavement, not a missed fixture query");
            }
            else
            {
                Assert.That((bool)Field(proof,"roadSupportPresent"),Is.True,"A negative obstruction must still have real road support");Assert.That(failures,Does.Not.Contain("NO_NATIVE_PAVEMENT_SUPPORT_WITHIN_EXISTING_POINT6_METERS"));
                if(mutation=="SOLID_DIGGER_ABOVE_ROAD")Assert.That(failures,Does.Contain("NO_ACTUAL_SOURCE_DIGGER_FLOOR_BENEATH_AUTHORED_PAVEMENT"));
                if(mutation=="GENUINE_LOW_ROOF"||mutation=="OTHER_ROAD_DECK")Assert.That(failures,Does.Contain("AUTHORED_ROAD_SIX_METER_BODY_BLOCKED_EXACT_SUPPORT_COLLIDER_ONLY_EXEMPTED"));
                if(mutation=="GENUINE_LOW_ROOF")Assert.That(failures,Does.Contain("ACTUAL_TWO_ORIGINAL_NWH_SOLID_BOXES_BLOCKED_ON_PAVEMENT"));
                if(mutation=="OTHER_ROAD_DECK"){var q=Field(proof,"sixMeterQuery");Assert.That((string[])Field(q,"blockers"),Has.Some.Contains("other road deck is NOT exempt"));}
            }
        }
        finally
        {
            if(root!=null)UnityEngine.Object.DestroyImmediate(root);if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(incoming.IsValid()&&incoming.isLoaded)EditorSceneManager.SetActiveScene(incoming);Assert.That(SceneManager.GetActiveScene(),Is.EqualTo(incoming));Assert.That(incoming.isDirty,Is.EqualTo(dirty));Assert.That(Context(),Is.EqualTo(context),"Every incoming scene/path/dirty state must remain intact after the isolated control");Physics.SyncTransforms();
        }
    }

    [TestCase("ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789",true)]
    [TestCase("BBCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789",false)]
    [TestCase("GBCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789",false)]
    [TestCase("abcdef",false)]
    public void Sha256HexCasingIsNotAByteChangeButAnyDigestMutationIsRejected(string expected,bool equal)
    {
        const string actual="abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789";Assert.That((bool)Probe.GetMethod("CavernRoadSha256Equal").Invoke(null,new object[]{actual,expected}),Is.EqualTo(equal));
    }
    [Test]
    public void MissingSourceWitnessRetainsExactPathAndExpectedHashRatherThanGenericAllFailure()
    {
        const string path="Assets/TruckDragonKingdom/Tests/World/THIS_SOURCE_IS_INTENTIONALLY_ABSENT_CAVERN_HASH_CONTROL.never",sha="abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789";var row=Probe.GetMethod("CheckCavernRoadSourceHash").Invoke(null,new object[]{path,sha,"CONTROL"});object F(string n)=>row.GetType().GetField(n).GetValue(row);Assert.That(F("path"),Is.EqualTo(path));Assert.That(F("expectedSha256"),Is.EqualTo(sha));Assert.That(F("exists"),Is.EqualTo(false));Assert.That(F("hexExact"),Is.EqualTo(false));Assert.That((string)F("resolvedPath"),Does.EndWith("THIS_SOURCE_IS_INTENTIONALLY_ABSENT_CAVERN_HASH_CONTROL.never"));Assert.That((string)F("error"),Does.Contain("MISSING"));
    }
    [TestCase("")][TestCase("LegacyRoof")]
    public void AnUnknownProfileCannotIssueAnUndergroundAuthorityToken(string profile)
    {var e=Assert.Throws<TargetInvocationException>(()=>Probe.GetMethod("LoadQualifiedCavernRoadAuthority").Invoke(null,new object[]{profile,"missing.json","missing.xml"}));Assert.That(e.InnerException,Is.TypeOf<InvalidOperationException>());}
    [Test]
    public void MissingQualifiedTokenPreservesEveryOriginalSurfaceStartExtractionFootingAndRoadConflict()
    {
        var owners=new[]{"ORIGINAL_PHYSICAL_ROAD_CORE:castle-caverns","ACTUAL_PHYSICAL_STAGING_MESH:Shared Dragon Kingdom/pad","ORIGINAL_NWH_START:caverns:1","ORIGINAL_PHYSICAL_EXTRACTION:boat","EXPLICIT_SOURCE_PHYSICAL_ARCHITECTURE_FOOTING:one","ORIGINAL_TILE_SEAM_FINAL_BASELINE"};var result=Probe.GetMethod("ClassifyCavernRoofAuthorityRoles").Invoke(null,new object[]{null,new Vector3(520,105,780),owners});Assert.That((string[])result.GetType().GetField("rawOwners").GetValue(result),Is.EqualTo(owners));Assert.That((string[])result.GetType().GetField("terrainConflictOwners").GetValue(result),Is.EqualTo(owners));Assert.That((string[])result.GetType().GetField("undergroundColliderOwners").GetValue(result),Is.Empty);
    }
    [Test,Timeout(1800000)]
    public void ActualAuthoredRoadLanesAndAllSixExactCapsUseNativePavementFramesWithoutChangingRetainedSources()
    {
        Probe.GetMethod("CaptureQualifiedCavernAuthoredRoadAuthority").Invoke(null,null);var proof=Probe.GetProperty("LastCavernAuthoredRoadAuthority").GetValue(null);object Field(string n)=>proof.GetType().GetField(n).GetValue(proof);
        foreach(string n in new[]{"sourceClosureExact","rawDiagnosticSourceQualified","graphAndPosesExact","geometryPhysicsHeightsExact","sourceTerrainMemoryExact","sceneContextRestored","allAuthoredRoadContactsQualified","allSixExactCapsQualified"})Assert.That((bool)Field(n),Is.True,n);
        Assert.That((int)Field("rawGridPoints"),Is.EqualTo(454));Assert.That((int)Field("requiredCaps"),Is.EqualTo(6));Assert.That((int)Field("ownedTargetPhysicalPieces"),Is.GreaterThan(0));Assert.That((Array)Field("contacts"),Is.Not.Empty);Assert.That((Array)Field("roads"),Is.Not.Empty);Assert.That((string)Field("status"),Is.EqualTo("AUTHORED_ROAD_AND_SIX_CAP_NATIVE_CONTACTS_PASSED_NOT_FULL_WORLD_OR_DRIVING_ACCEPTANCE"));
        foreach(var row in (Array)Field("contacts")){object R(string n)=>row.GetType().GetField(n).GetValue(row);Assert.That((bool)R("qualified"),Is.True);Assert.That(((Array)R("failures")).Length,Is.Zero);Assert.That(((Array)R("nwhQueries")).Length,Is.EqualTo(2));}
    }
}
#endif
