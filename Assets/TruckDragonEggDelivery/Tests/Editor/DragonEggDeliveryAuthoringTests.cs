#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DragonEggDeliveryAuthoringTests
{
    Scene priorActive,inspected;
    bool opened;
    byte[] sceneBytes;
    const string ScenePath="Assets/TruckDragonEggDelivery/Scenes/TruckDragonEggDelivery.unity";
    static object Read(object value,string name)=>value.GetType().GetField(name) is FieldInfo field?field.GetValue(value):value.GetType().GetProperty(name).GetValue(value);
    [Test,Timeout(180000)] public void ExplicitEmptyBabyControllerRepairPreservesGuidSceneAndValidCustomControllerByteExactly()
    {
        Assert.That(EditorApplication.isPlayingOrWillChangePlaymode,Is.False,"This explicitly authorized selected migration runs only in stopped native EditMode.");
        const string path="Assets/TruckDragonEggDelivery/Animations/Actual Spark hatch and care.controller";
        Assert.That(File.Exists(path)&&File.Exists(ScenePath),Is.True,"Retain the original owned controller and saved Egg scene before explicit repair.");
        string guid=AssetDatabase.AssetPathToGUID(path);var meta=File.ReadAllBytes(path+".meta");var originalScene=File.ReadAllBytes(ScenePath);Assert.That(guid,Is.Not.Empty);
        var authoring=Type.GetType("LWS.TruckDragonEggDelivery.Editor.DragonEggDeliveryAuthoring, Assembly-CSharp-Editor",true);var repair=authoring.GetMethod("RepairEmptyBabyController");Assert.That(repair,Is.Not.Null);
        repair.Invoke(null,null);
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);Assert.That(controller,Is.Not.Null);Assert.That(controller.layers,Is.Not.Empty);var machine=controller.layers[0].stateMachine;Assert.That(machine,Is.Not.Null);
        var states=machine.states.Select(value=>value.state).ToArray();var hatch=states.Single(value=>value.name=="Hatch");var care=states.Single(value=>value.name=="Care");
        Assert.That(machine.defaultState,Is.SameAs(hatch));Assert.That(hatch.motion,Is.TypeOf<AnimationClip>());Assert.That(AssetDatabase.GetAssetPath(hatch.motion),Is.EqualTo("Assets/TruckDragonKingdom/Animations/Authored/dragon-spark/Hatch.anim"));Assert.That(((AnimationClip)hatch.motion).length,Is.GreaterThan(0));
        Assert.That(hatch.transitions.Any(value=>value.destinationState==care&&value.hasExitTime&&value.exitTime>=.9f),Is.True);Assert.That(care.motion,Is.TypeOf<BlendTree>());var tree=(BlendTree)care.motion;Assert.That(tree.blendParameter,Is.EqualTo("Activity"));Assert.That(tree.children.Length,Is.GreaterThanOrEqualTo(2));Assert.That(tree.children.All(value=>value.motion is AnimationClip&&((AnimationClip)value.motion).length>0),Is.True);Assert.That(controller.parameters.Any(value=>value.name=="Activity"&&value.type==AnimatorControllerParameterType.Float),Is.True);
        Assert.That(AssetDatabase.AssetPathToGUID(path),Is.EqualTo(guid));Assert.That(File.ReadAllBytes(path+".meta"),Is.EqualTo(meta));Assert.That(File.ReadAllBytes(ScenePath),Is.EqualTo(originalScene));
        byte[] repaired=File.ReadAllBytes(path);repair.Invoke(null,null);repair.Invoke(null,null);Assert.That(File.ReadAllBytes(path),Is.EqualTo(repaired),"Repeated selected repair must not rewrite a valid controller.");
        // Prove a valid designer setting survives, then remove only this test's
        // temporary edit. The retained product remains the repaired controller.
        float authoredSpeed=hatch.speed;
        try
        {
            hatch.speed=authoredSpeed*.83f;EditorUtility.SetDirty(hatch);AssetDatabase.SaveAssetIfDirty(hatch);byte[] customized=File.ReadAllBytes(path);
            repair.Invoke(null,null);Assert.That(hatch.speed,Is.EqualTo(authoredSpeed*.83f));Assert.That(File.ReadAllBytes(path),Is.EqualTo(customized),"Valid custom Hatch speed must remain byte-identical under repair.");
        }
        finally{if(hatch.speed!=authoredSpeed){hatch.speed=authoredSpeed;EditorUtility.SetDirty(hatch);AssetDatabase.SaveAssetIfDirty(hatch);}}
        Assert.That(File.ReadAllBytes(path),Is.EqualTo(repaired),"Test-only designer tuning must be fully removed before retaining the repaired asset.");Assert.That(AssetDatabase.AssetPathToGUID(path),Is.EqualTo(guid));Assert.That(File.ReadAllBytes(ScenePath),Is.EqualTo(originalScene));
        SavedEggSceneHasBoundedSupportedLoadingBaysAndTheActualBabyHatchController();
        Debug.Log("EGG_EXPLICIT_EMPTY_CONTROLLER_REPAIR_ACCEPTED guid="+guid+" realHatchCare=true repeatBytesExact=true validCustomSpeedPreserved=true sceneBytesExact=true testTuningRestored=true");
    }
    [Test] public void SavedEggSceneHasBoundedSupportedLoadingBaysAndTheActualBabyHatchController()
    {
        const string path=ScenePath;
        Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(path),Is.Not.Null,"Explicitly compose the saved real world first.");
        priorActive=SceneManager.GetActiveScene();sceneBytes=File.ReadAllBytes(path);opened=false;
        inspected=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).FirstOrDefault(scene=>scene.isLoaded&&scene.path==path);
        if(!inspected.IsValid()){inspected=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);opened=true;}
        Assert.That(inspected.isDirty,Is.False,"Read the saved original scene without discarding user changes.");
        var type=Type.GetType("LWS.TruckDragonEggDelivery.DragonEggDeliveryMatch, Assembly-CSharp",true);var match=inspected.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren(type,true)).Single();Assert.That(match,Is.Not.Null);Physics.SyncTransforms();
        var nests=((IEnumerable)Read(match,"nests")).Cast<object>().ToArray();var carriers=((IEnumerable)Read(match,"carriers")).Cast<object>().ToArray();Assert.That(nests.Length,Is.EqualTo(4));Assert.That(carriers.Length,Is.EqualTo(4));
        var world=Read(match,"world");var region=world.GetType().GetMethod("Region").Invoke(world,new object[]{"nests"});var actualNests=(Transform[])Read(region,"nests");
        for(int slot=0;slot<nests.Length;slot++)
        {
            var nest=nests[slot];
            var center=(Transform)Read(nest,"center");var approach=(Transform)Read(nest,"approach");Assert.That(Vector3.Distance(center.position,approach.position),Is.InRange(12,25));
            var art=actualNests[slot].parent.GetComponentsInChildren<Renderer>(true).Where(renderer=>renderer.enabled&&renderer.gameObject.activeInHierarchy&&!(renderer is ParticleSystemRenderer)).ToArray();Assert.That(art.Length,Is.GreaterThan(0));var bounds=art[0].bounds;foreach(var renderer in art.Skip(1))bounds.Encapsulate(renderer.bounds);
            Assert.That(Vector3.ProjectOnPlane(center.position-bounds.center,Vector3.up).magnitude,Is.LessThan(.1f),"The lining belongs at the actual visible nest centre.");
            Assert.That(center.position.y,Is.InRange(bounds.min.y-.75f,bounds.max.y+1.5f),"A local marker beneath15x art cannot put cargo15world metres above the actual nest.");
            var support=Physics.RaycastAll(approach.position+Vector3.up*3,Vector3.down,6,~0,QueryTriggerInteraction.Ignore).Where(hit=>hit.collider.gameObject.scene==inspected&&hit.normal.y>.7f&&Mathf.Abs(hit.point.y-approach.position.y)<.2f).ToArray();
            Assert.That(support.Length,Is.GreaterThan(0),"The bounded approach marker requires genuine existing wheel support.");
        }
        foreach(var carrier in carriers)
        {
            var truck=(Component)Read(carrier,"truck");Assert.That(Read(truck,"tractorMotor"),Is.Not.Null);
            var cargo=(Component)Read(carrier,"cargo");var baby=(GameObject)Read(cargo,"baby");var animator=(Animator)Read(cargo,"babyAnimator");var body=(Rigidbody)Read(cargo,"body");
            Assert.That(baby.GetComponentsInChildren<Rigidbody>(true).Length,Is.Zero,"Baby presentation must share the conserved cargo Rigidbody.");
            Assert.That(baby.GetComponentsInChildren<MonoBehaviour>(true).Length,Is.Zero,"A second animal motor must not fight cargo physics.");
            Assert.That(body.transform,Is.SameAs(cargo.transform));Assert.That(animator.avatar!=null&&animator.avatar.isValid&&!animator.avatar.isHuman,Is.True);
            var controller=animator.runtimeAnimatorController as AnimatorController;Assert.That(controller,Is.Not.Null,"Saved baby must reference its actual owned AnimatorController.");Assert.That(controller.layers,Is.Not.Empty,"Saved baby controller is empty; actual Hatch/Care animation cannot play. Explicitly repair the retained empty owned controller, not this gate.");var states=controller.layers[0].stateMachine.states.Select(value=>value.state).ToArray();
            var hatch=states.Single(state=>state.name=="Hatch");Assert.That(hatch.motion,Is.TypeOf<AnimationClip>());Assert.That(AssetDatabase.GetAssetPath(hatch.motion),Does.StartWith("Assets/TruckDragonKingdom/Animations/Authored/dragon-spark/"));
            Assert.That(AssetDatabase.GetAssetPath(hatch.motion),Does.EndWith("/Hatch.anim"));Assert.That(hatch.transitions.Any(transition=>transition.destinationState.name=="Care"&&transition.hasExitTime&&transition.exitTime>=.9f),Is.True);
        }
    }
    [TearDown] public void Restore()
    {
        if(opened&&inspected.IsValid()&&inspected.isLoaded)
        {
            if(!Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Any(scene=>scene.isLoaded&&scene!=inspected))EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            EditorSceneManager.CloseScene(inspected,true);
        }
        if(priorActive.IsValid()&&priorActive.isLoaded)SceneManager.SetActiveScene(priorActive);
        else{var loaded=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).FirstOrDefault(scene=>scene.isLoaded);if(loaded.IsValid())SceneManager.SetActiveScene(loaded);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
        if(sceneBytes!=null)Assert.That(File.ReadAllBytes(ScenePath),Is.EqualTo(sceneBytes),"Read-only fixture changed saved scene bytes.");opened=false;sceneBytes=null;
    }
}
#endif
