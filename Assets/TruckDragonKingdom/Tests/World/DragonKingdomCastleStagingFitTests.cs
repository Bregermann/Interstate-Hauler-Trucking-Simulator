#if UNITY_EDITOR && VISTA
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class DragonKingdomCastleStagingFitTests
{
    static Type Author=>Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);
    static object Call(string method,params object[] args)=>Author.GetMethod(method,BindingFlags.Public|BindingFlags.Static).Invoke(null,args);
    [TestCase("RigLLegAnkle",0)][TestCase("RigRLegToes2",1)][TestCase("RigLLeg1",-1)][TestCase("RigLWing01",-1)]
    public void OnlyDisclosedActualSourceAnkleAndToeNamesEnterFootScreen(string name,int side)=>Assert.That(Call("CastleFootSide",name),Is.EqualTo(side));
    [TestCase(100f)][TestCase(140f)]
    public void DefaultFeetMotorSolutionUsesActualRuntimeFirstGateHeadingAndMetreOffsets(float goalHeight)
    {
        var feet=new[]{new Vector3(-1,-4,-2),new Vector3(1,-4,-2),new Vector3(-1,-4,2),new Vector3(1,-4,2)};var roof=new Vector3(100,100,300);var goal=new Vector3(100,goalHeight,500);object[] args={roof,goal,feet,default(Pose)};Assert.That(Call("SolveCastleMotorPose",args),Is.True);var pose=(Pose)args[3];var points=feet.Select(p=>pose.position+pose.rotation*p).ToArray();Assert.That(points.Min(p=>p.y),Is.EqualTo(100.015f).Within(.0003f));Assert.That((points.Min(p=>p.x)+points.Max(p=>p.x))*.5f,Is.EqualTo(roof.x).Within(.0003f));Assert.That((points.Min(p=>p.z)+points.Max(p=>p.z))*.5f,Is.EqualTo(roof.z).Within(.0003f));Assert.That(Vector3.Dot(pose.rotation*Vector3.forward,(goal-pose.position).normalized),Is.GreaterThan(.999999f));
    }
    [Test]public void MissingNamedFeetCannotInventSupportedMotorHeight()=>Assert.That(Call("SolveCastleMotorPose",Vector3.zero,Vector3.forward*100,Array.Empty<Vector3>(),default(Pose)),Is.False);
    [TestCase(100f)][TestCase(140f)]
    public void YawOnlyIsAnExplicitLevelDiagnosticAndDoesNotChangeTheRuntimeFullPitchSolver(float goalHeight)
    {
        var feet=new[]{new Vector3(-1,-4,-2),new Vector3(1,-4,-2),new Vector3(-1,-4,2),new Vector3(1,-4,2)};var roof=new Vector3(100,100,300);var goal=new Vector3(100,goalHeight,500);object[] yaw={roof,goal,feet,default(Pose)};Assert.That(Call("SolveCastleMotorPoseYaw",yaw),Is.True);var pose=(Pose)yaw[3];Assert.That(Vector3.Dot(pose.rotation*Vector3.up,Vector3.up),Is.GreaterThan(.999999f));Assert.That(feet.Min(p=>(pose.position+pose.rotation*p).y),Is.EqualTo(100.015f).Within(.0003f));object[] full={roof,goal,feet,default(Pose)};Assert.That(Call("SolveCastleMotorPose",full),Is.True);if(goalHeight==140)Assert.That(Mathf.Abs((((Pose)full[3]).rotation*Vector3.forward).y),Is.GreaterThan(.1f),"Existing actual full-pitch policy remains independently measured, not silently replaced.");
    }
    [UnityTest,Timeout(1800000)]
    public IEnumerator ActualEightCurrentDefaultMountsAndFourNativeCastleRoofsAreMeasuredWithoutSavingSources()
    {
        var bootstrap=typeof(DragonKingdomPhysicalCourseConsumerTests).GetMethod("OwnedBatchBootstrap",BindingFlags.NonPublic|BindingFlags.Static);const string generated="Assets/TruckDragonKingdom/Tests/World/Generated/";string root=Directory.GetParent(Application.dataPath).FullName;Directory.CreateDirectory(Path.Combine(root,generated));AssetDatabase.Refresh();
        for(int i=0;i<SceneManager.sceneCount;i++){var scene=SceneManager.GetSceneAt(i);if(string.IsNullOrEmpty(scene.path)){Assert.That((bool)bootstrap.Invoke(null,new object[]{scene}),Is.True,"Preserve unknown/edited user scenes.");Assert.That(EditorSceneManager.SaveScene(scene,generated+"CastleFitBootstrap-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);}Assert.That(scene.isDirty||string.IsNullOrEmpty(scene.path),Is.False);}
        var setup=EditorSceneManager.GetSceneManagerSetup();var bytes=setup.ToDictionary(s=>s.path,s=>File.ReadAllBytes(Path.Combine(root,s.path)));var isolation=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Assert.That(EditorSceneManager.SaveScene(isolation,generated+"CastleFitIsolation-"+Guid.NewGuid().ToString("N")+".unity"),Is.True);
        try
        {
            Call("InspectActualCastleStagingFit");yield return null;var proof=Author.GetProperty("LastCastleStagingFit").GetValue(null);var type=proof.GetType();foreach(string field in new[]{"sourceBytesExact","originalNwhExact","perchAndTowerStatesExact","allSourceChoicesMeasured"})Assert.That(type.GetField(field).GetValue(proof),Is.True,field);Assert.That(type.GetField("physicalRings").GetValue(proof),Is.EqualTo(12));Assert.That(type.GetField("fullCourseRadius").GetValue(proof),Is.EqualTo(51.860015869140625f));var rigs=(IList)type.GetField("rigs").GetValue(proof);Assert.That(rigs.Count,Is.EqualTo(8));var paths=new System.Collections.Generic.List<string>();foreach(var rig in rigs){var rt=rig.GetType();var yaw=rt.GetField("yawOnlyDiagnostic").GetValue(rig);Assert.That(yaw,Is.Not.Null);foreach(var sample in new[]{rig,yaw}){Assert.That(rt.GetField("failure").GetValue(sample),Is.Null.Or.Empty);Assert.That(rt.GetField("capturePoseExact").GetValue(sample),Is.True);string png=(string)rt.GetField("capturePng").GetValue(sample);Assert.That(File.Exists(png),Is.True,png);paths.Add(png);var leaves=(Array)rt.GetField("defaultLeaves").GetValue(sample);Assert.That(leaves.Length,Is.GreaterThan(0));}Assert.That(rt.GetField("defaultBonePoseSha256").GetValue(rig),Is.EqualTo(rt.GetField("defaultBonePoseSha256").GetValue(yaw)),"Heading comparisons must preserve the same genuine source default skeleton.");if((string)rt.GetField("species").GetValue(rig)=="dragon-fire"){var layers=(Array)rt.GetField("defaultLayers").GetValue(rig);var layer=layers.GetValue(0);Assert.That(((Array)layer.GetType().GetField("configuredEffectiveLeaves").GetValue(layer)).Length,Is.GreaterThan(1),"The actual Fire multi-leaf default must be evaluated completely, not one arbitrarily chosen child.");}}Assert.That(paths.Distinct().Count(),Is.EqualTo(16));var supports=(Array)type.GetField("supports").GetValue(proof);Assert.That(supports.Length,Is.EqualTo(4));foreach(var support in supports){var t=support.GetType();Assert.That(t.GetField("sourceMeshColliderExact").GetValue(support),Is.True);Assert.That(((Array)t.GetField("roofGrid").GetValue(support)).Length,Is.EqualTo(81));}Assert.That(type.GetField("status").GetValue(proof),Is.EqualTo("ACTUAL_EIGHT_DEFAULT_SOURCE_AND_FOUR_NATIVE_CASTLE_SUPPORTS_MEASURED_NOT_GROUNDED_OR_MIGRATED"));
            // Unsupported feet, sloped/spiked roofs, species-height mismatch,
            // or positive native contacts are truthful measurement outcomes.
            // This case never labels them supported, grounded or production-ready.
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(setup);foreach(var p in bytes)CollectionAssert.AreEqual(p.Value,File.ReadAllBytes(Path.Combine(root,p.Key)));}
    }
}
#endif
