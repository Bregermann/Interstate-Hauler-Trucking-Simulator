#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class DragonAnimalAuthoringNativeTests
{
    const string Folder="Assets/TruckDragonKingdom/Prefabs/Dragons/dragon-inferno";
    const string Raw="Assets/TruckDragonKingdom/PrivateLicensedAnimalController/Malbers Animations/Animal Controller/Empty Controller/Empty Fly.asset";
    static object Read(object value,string field)=>value.GetType().GetField(field).GetValue(value);
    static float FloatValue(object reference)=>(float)reference.GetType().GetProperty("Value").GetValue(reference);
    static string[] StateNames(object state,string field)=>((IEnumerable)Read(state,field)).Cast<UnityEngine.Object>().Select(value=>value==null?"null":value.name).ToArray();
    [Test] public void ConfiguredGroundTakeoffRetainsVendorTemplateAndRepeatedUpgradeIsByteIdempotent()
    {
        Assert.That(EditorApplication.isPlayingOrWillChangePlaymode,Is.False,"Run this asset contract in EditMode after explicit native upgrade.");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/dragon-inferno Dragon Animal.prefab");Assert.That(prefab,Is.Not.Null);
        var motor=prefab.GetComponentsInChildren<Component>(true).Single(value=>value!=null&&value.GetType().Name=="DragonAnimalMotor");
        var fly=((IEnumerable)Read(Read(motor,"animal"),"states")).Cast<ScriptableObject>().Single(value=>value.GetType().Name=="Fly");
        Assert.That(AssetDatabase.GetAssetPath(fly),Does.StartWith(Folder+"/"));Assert.That((bool)Read(fly,"IncludeSleepState"),Is.True);
        Assert.That(StateNames(fly,"SleepFromState"),Does.Not.Contain("Idle"));Assert.That(StateNames(fly,"SleepFromState"),Does.Not.Contain("Locomotion"));
        Assert.That(FloatValue(Read(fly,"Duration")),Is.EqualTo(0f),"The inherited3second demonstration duration cannot end sustained flight.");
        Assert.That(FloatValue(Read(fly,"LandMultiplier")),Is.EqualTo((float)Read(Read(motor,"animal"),"height")),"Normal Fly landing ray is limited to the authored real hip-height support.");
        foreach(var state in ((IEnumerable)Read(Read(motor,"animal"),"states")).Cast<ScriptableObject>().Where(value=>value.GetType().Name=="Idle"||value.GetType().Name=="Locomotion"))
        {
            var tag=((IEnumerable)Read(state,"TagModifiers")).Cast<object>().Single(value=>(string)Read(value,"AnimationTag")=="DragonLand");var settings=Read(tag,"modifier");Assert.That(Read(settings,"Persistent"),Is.EqualTo(true));Assert.That(Read(settings,"LockMovement"),Is.EqualTo(true));
        }
        var animator=(Animator)Read(motor,"animator");var controller=(AnimatorController)animator.runtimeAnimatorController;
        var landStates=controller.layers[0].stateMachine.states.Where(value=>value.state.name=="Land to Idle"||value.state.name=="Land to Movement").ToArray();Assert.That(landStates.Length,Is.EqualTo(2));foreach(var state in landStates)Assert.That(state.state.tag,Is.EqualTo("DragonLand"));
        Assert.That(StateNames(fly,"ResetFrom"),Does.Contain("Idle"));Assert.That(StateNames(fly,"ResetFrom"),Does.Contain("Locomotion"));
        var raw=AssetDatabase.LoadAssetAtPath<ScriptableObject>(Raw);Assert.That(StateNames(raw,"SleepFromState"),Does.Contain("Idle"));Assert.That(StateNames(raw,"SleepFromState"),Does.Contain("Locomotion"));
        Assert.That(FloatValue(Read(raw,"Duration")),Is.EqualTo(3f),"Raw vendor demonstration configuration is preserved.");
        Assert.That(FloatValue(Read(raw,"LandMultiplier")),Is.EqualTo(1f),"Raw vendor landing configuration is preserved.");
        var catalog=AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset");var lookup=new object[]{"dragon-inferno",null};Assert.That((bool)catalog.GetType().GetMethod("TryGet").Invoke(catalog,lookup),Is.True);
        var authoring=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonAnimalMotorAuthoring, Assembly-CSharp-Editor",true);var upgrade=authoring.GetMethod("UpgradeConfiguredPrefab");
        var before=Bytes();string rawBefore=Digest(Raw);
        upgrade.Invoke(null,new[]{lookup[1],prefab,(object)Folder});upgrade.Invoke(null,new[]{lookup[1],prefab,(object)Folder});
        var after=Bytes();Assert.That(after.Keys,Is.EquivalentTo(before.Keys));foreach(var pair in before)Assert.That(after[pair.Key],Is.EqualTo(pair.Value),"Already-valid configured asset changed on repeat: "+pair.Key);
        Assert.That(Digest(Raw),Is.EqualTo(rawBefore),"Raw vendor template must remain byte-identical.");
    }
    static Dictionary<string,string> Bytes()=>Directory.GetFiles(Folder,"*",SearchOption.AllDirectories).ToDictionary(path=>path,path=>Digest(path),StringComparer.Ordinal);
    [Test,Timeout(120000)]public void ExplicitGroundGrabAddsGroundOnlyActionPreservesAirborneGrabAndRepeatBytes()
    {
        Assert.That(EditorApplication.isPlayingOrWillChangePlaymode,Is.False);
        const string path="Assets/TruckDragonKingdom/Animations/Authored/dragon-inferno/GroundGrab.anim",catalogPath="Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset";
        var watched=Directory.GetFiles("Assets/TruckDragonKingdom/Animations/Authored","*.anim",SearchOption.AllDirectories).Where(value=>value.Replace('\\','/')!=path).Concat(new[]{"Assets/TruckDragonRiders/Scenes/TruckDragonRiders.unity","Assets/TruckKeepaway/Scenes/TruckKeepaway.unity","Assets/TruckBalloonBattle/Scenes/TruckBalloonBattle.unity","Assets/TruckCornhole/Scenes/TruckCornhole.unity","Assets/TruckCheeseRolling/Scenes/TruckCheeseRolling.unity"}).ToDictionary(value=>value,Digest);
        string configuredPath=Folder+"/dragon-inferno Dragon Animal.prefab",controllerPath=Folder+"/dragon-inferno Dragon.controller";string prefabGuid=AssetDatabase.AssetPathToGUID(configuredPath),controllerGuid=AssetDatabase.AssetPathToGUID(controllerPath),catalogGuid=AssetDatabase.AssetPathToGUID(catalogPath);
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);var states=controller.layers.SelectMany(layer=>layer.stateMachine.states).Select(value=>value.state).Where(value=>value.tag!="DragonAction_GroundGrab").ToDictionary(state=>state,state=>EditorJsonUtility.ToJson(state));
        var author=Type.GetType("LWS.TruckDragonKingdom.DragonSpeciesAuthoring, Assembly-CSharp-Editor",true);var upgrade=author.GetMethod("UpgradeInfernoGroundGrab");upgrade.Invoke(null,null);
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);Assert.That(clip,Is.Not.Null);Assert.That(clip.length,Is.EqualTo(2).Within(.001f));Assert.That(AnimationUtility.GetAnimationClipSettings(clip).loopTime,Is.False);
        foreach(var pair in watched)Assert.That(Digest(pair.Key),Is.EqualTo(pair.Value),"Existing airborne Grab/other actions/original mode scenes remain byte-identical: "+pair.Key);foreach(var pair in states)Assert.That(EditorJsonUtility.ToJson(pair.Key),Is.EqualTo(pair.Value),"Existing controller state must remain exact: "+pair.Key.name);
        Assert.That(AssetDatabase.AssetPathToGUID(configuredPath),Is.EqualTo(prefabGuid));Assert.That(AssetDatabase.AssetPathToGUID(controllerPath),Is.EqualTo(controllerGuid));Assert.That(AssetDatabase.AssetPathToGUID(catalogPath),Is.EqualTo(catalogGuid));
        Assert.That(controller.layers.SelectMany(layer=>layer.stateMachine.states).Count(value=>value.state.tag=="DragonAction_GroundGrab"&&value.state.motion==clip),Is.EqualTo(1));
        var before=Directory.GetFiles(Folder,"*",SearchOption.AllDirectories).Concat(new[]{path,path+".meta",catalogPath,catalogPath+".meta"}).ToDictionary(value=>value,Digest);upgrade.Invoke(null,null);foreach(var pair in before)Assert.That(Digest(pair.Key),Is.EqualTo(pair.Value),"Repeated explicit GroundGrab upgrade must not rewrite valid output: "+pair.Key);
    }
    [Test] public void ExplicitLandingCoreMaskRepairChangesOnlyOwnedGeneralResetBitsAndRepeatBytes()
    {
        Assert.That(EditorApplication.isPlayingOrWillChangePlaymode,Is.False);
        const string root="Assets/TruckDragonKingdom/Prefabs/Dragons";
        var paths=Directory.GetFiles(root,"*",SearchOption.AllDirectories);var before=paths.ToDictionary(path=>path,Digest,StringComparer.Ordinal);
        var expected=new Dictionary<string,string>(StringComparer.Ordinal);var guids=new Dictionary<string,string>(StringComparer.Ordinal);int required=0;
        foreach(var prefabPath in AssetDatabase.FindAssets("t:Prefab",new[]{root}).Select(AssetDatabase.GUIDToAssetPath).Where(path=>path.EndsWith(" Dragon Animal.prefab",StringComparison.Ordinal)))
        {
            var motor=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath).GetComponentsInChildren<Component>(true).Single(value=>value!=null&&value.GetType().Name=="DragonAnimalMotor");
            foreach(var state in ((IEnumerable)Read(Read(motor,"animal"),"states")).Cast<ScriptableObject>().Where(value=>value.GetType().Name=="Idle"||value.GetType().Name=="Locomotion"))
            {
                if(!((IEnumerable)Read(state,"TagModifiers")).Cast<object>().Any(value=>(string)Read(value,"AnimationTag")=="DragonLand"))continue;
                string path=AssetDatabase.GetAssetPath(state);string disk=File.ReadAllText(path);
                var mask=System.Text.RegularExpressions.Regex.Match(disk,@"(?m)^  General:\r?\n    modify: ([0-9]+)");
                Assert.That(mask.Success,Is.True,"Witness exact owned serialized General.modify field before explicit migration.");
                int old=int.Parse(mask.Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture);int repaired=old|128|256;
                Assert.That(Convert.ToInt32(Read(Read(state,"General"),"modify")),Is.EqualTo(old),"Loaded authoring must match the exact disk baseline.");
                expected[path]=disk.Remove(mask.Groups[1].Index,mask.Groups[1].Length).Insert(mask.Groups[1].Index,repaired.ToString(System.Globalization.CultureInfo.InvariantCulture));
                guids[path]=AssetDatabase.AssetPathToGUID(path);if(old!=repaired)required++;
            }
        }
        Assert.That(expected.Count,Is.GreaterThanOrEqualTo(2));string rawBefore=Digest(Raw);
        var authoring=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonAnimalMotorAuthoring, Assembly-CSharp-Editor",true);var migrate=authoring.GetMethod("UpgradeOwnedLandingCoreResetMasks");
        Assert.That(migrate.Invoke(null,null),Is.EqualTo(required));
        foreach(var pair in expected){Assert.That(File.ReadAllText(pair.Key),Is.EqualTo(pair.Value),"Migration may change only General.modify bits128/256: "+pair.Key);Assert.That(AssetDatabase.AssetPathToGUID(pair.Key),Is.EqualTo(guids[pair.Key]));}
        foreach(var pair in before)if(!expected.ContainsKey(pair.Key.Replace('\\','/')))Assert.That(Digest(pair.Key),Is.EqualTo(pair.Value),"All other configured files remain byte-identical: "+pair.Key);
        var repairedBytes=paths.ToDictionary(path=>path,Digest,StringComparer.Ordinal);Assert.That(migrate.Invoke(null,null),Is.EqualTo(0));foreach(var pair in repairedBytes)Assert.That(Digest(pair.Key),Is.EqualTo(pair.Value),"Repeated explicit migration is a byte no-op: "+pair.Key);
        Assert.That(Digest(Raw),Is.EqualTo(rawBefore));
    }
    [Test] public void DirectedBreathPreservesSourceBodyCurvesAndRepeatedSelectedUpgradeIsByteIdempotent()
    {
        Assert.That(EditorApplication.isPlayingOrWillChangePlaymode,Is.False);
        const string clipPath="Assets/TruckDragonKingdom/Animations/Authored/dragon-inferno/Breath.anim";
        const string sourcePath="Assets/TruckDragonKingdom/PrivateLicensedDragons/Monsters Ultimate Pack 02 Cute Series/Dragon Inferno Cute Series/FBX/Dragon Inferno@Cast Spell.FBX";
        const string catalogPath="Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset";
        const string receiptPath="Documentation/TruckSports/DragonKingdom/dragon-inferno-DirectedBreathReceipt.json";
        var output=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);Assert.That(output,Is.Not.Null,"Run explicit RegenerateInfernoBreath authoring before the contract fixture.");
        var source=AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<AnimationClip>().Single(value=>!value.name.StartsWith("__",StringComparison.Ordinal));
        var rawModel=AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath.Substring(0,sourcePath.IndexOf("FBX/",StringComparison.Ordinal))+"Prefabs/Dragon Inferno.prefab");
        var head=rawModel.GetComponentsInChildren<Transform>(true).Single(value=>value.name=="RigHead");var selectedPaths=new[]{AnimationUtility.CalculateTransformPath(head,rawModel.transform),AnimationUtility.CalculateTransformPath(head.parent,rawModel.transform)};int kept=0,changed=0;
        foreach(var binding in AnimationUtility.GetCurveBindings(source))
        {
            bool rotation=binding.propertyName.StartsWith("m_LocalRotation.",StringComparison.Ordinal)||binding.propertyName.IndexOf("Euler",StringComparison.OrdinalIgnoreCase)>=0;
            if(rotation&&selectedPaths.Contains(binding.path)){changed++;continue;}
            var a=AnimationUtility.GetEditorCurve(source,binding);var b=AnimationUtility.GetEditorCurve(output,binding);Assert.That(b,Is.Not.Null,binding.path+"/"+binding.propertyName);Assert.That(b.keys,Is.EqualTo(a.keys),"Original non-aim curve must remain exact: "+binding.path+"/"+binding.propertyName);Assert.That(b.preWrapMode,Is.EqualTo(a.preWrapMode));Assert.That(b.postWrapMode,Is.EqualTo(a.postWrapMode));kept++;
        }
        Assert.That(kept,Is.GreaterThan(100));Assert.That(changed,Is.GreaterThan(0));Assert.That(output.length,Is.EqualTo(source.length).Within(.0001f));
        Assert.That(File.Exists(receiptPath),Is.True);var receipt=File.ReadAllText(receiptPath);Assert.That(receipt,Does.Contain("\"originalSourcesUnchanged\": true"));Assert.That(receipt,Does.Contain("\"unrelatedAuthoredClipsUnchanged\": true"));
        var catalog=AssetDatabase.LoadAssetAtPath<ScriptableObject>(catalogPath);var args=new object[]{"dragon-inferno",null};Assert.That((bool)catalog.GetType().GetMethod("TryGet").Invoke(catalog,args),Is.True);
        var mapping=((IEnumerable)Read(args[1],"animations")).Cast<object>().Single(value=>Read(value,"action").ToString()=="Breath");Assert.That(Read(mapping,"clip"),Is.SameAs(output));
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder+"/dragon-inferno Dragon.controller");Assert.That(controller.layers.SelectMany(layer=>layer.stateMachine.states).Single(value=>value.state.tag=="DragonAction_Breath").state.motion,Is.SameAs(output));
        string[] paths=Directory.GetFiles("Assets/TruckDragonKingdom/Animations/Authored","*",SearchOption.AllDirectories).Concat(Directory.GetFiles(Folder,"*",SearchOption.AllDirectories)).Concat(new[]{catalogPath,catalogPath+".meta",receiptPath,sourcePath,sourcePath+".meta"}).ToArray();
        var before=paths.ToDictionary(path=>path,Digest,StringComparer.Ordinal);string guid=AssetDatabase.AssetPathToGUID(clipPath);
        var authoring=Type.GetType("LWS.TruckDragonKingdom.DragonSpeciesAuthoring, Assembly-CSharp-Editor",true);authoring.GetMethod("RegenerateInfernoBreath").Invoke(null,null);authoring.GetMethod("RegenerateInfernoBreath").Invoke(null,null);
        Assert.That(AssetDatabase.AssetPathToGUID(clipPath),Is.EqualTo(guid));foreach(var pair in before)Assert.That(Digest(pair.Key),Is.EqualTo(pair.Value),"Repeated selected Breath authoring changed an already-valid asset: "+pair.Key);
    }
    static string Digest(string path){using(var hash=SHA256.Create())return string.Concat(hash.ComputeHash(File.ReadAllBytes(path)).Select(value=>value.ToString("x2")));}
}
#endif
