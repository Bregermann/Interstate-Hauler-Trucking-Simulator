#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DragonMotorOverrideNativeTests
{
    const string SavedOverride="Assets/TruckHoardHeist/Controllers/Heist guardian sleeping vigilance.overrideController";
    const string DerivedClip="Assets/TruckHoardHeist/PrivateLicensedPotteryFragments/HeistMotion/Curled sleeping vigilance.anim";
    GameObject owner;AnimatorOverrideController transient;AnimationClip replacement;
    AnimationClip CatalogLook()
    {
        var catalog=AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset");Assert.That(catalog,Is.Not.Null);var args=new object[]{"dragon-inferno",null};Assert.That((bool)catalog.GetType().GetMethod("TryGet").Invoke(catalog,args),Is.True);
        var mappings=(IEnumerable)args[1].GetType().GetField("animations").GetValue(args[1]);var look=mappings.Cast<object>().Single(value=>value.GetType().GetField("action").GetValue(value).ToString()=="LookAround");return (AnimationClip)look.GetType().GetField("clip").GetValue(look);
    }
    Component Motor(RuntimeAnimatorController controller)
    {
        owner=new GameObject("Actual Unity override binding contract");var animator=owner.AddComponent<Animator>();animator.runtimeAnimatorController=controller;
        var motor=owner.AddComponent(Type.GetType("LWS.TruckDragonKingdom.DragonAnimalMotor, Assembly-CSharp",true));motor.GetType().GetField("animator").SetValue(motor,animator);return motor;
    }
    static AnimationClip Resolve(Component motor,AnimationClip clip)=>(AnimationClip)motor.GetType().GetMethod("ResolveMappedClip",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(motor,new object[]{clip});
    [Test]
    public void SavedHeistOverrideResolvesCatalogLookAroundToItsExactAuthoredCurledClip()
    {
        Assert.That(EditorApplication.isPlayingOrWillChangePlaymode,Is.False,"Reference resolution is a native EditMode contract, not injected animation playback.");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(SavedOverride);var actual=AssetDatabase.LoadAssetAtPath<AnimationClip>(DerivedClip);var catalog=CatalogLook();Assert.That(controller,Is.Not.Null,"Explicitly author the saved Heist vigilance controller first.");Assert.That(actual,Is.Not.Null);Assert.That(actual,Is.Not.SameAs(catalog));
        var pairs=new List<KeyValuePair<AnimationClip,AnimationClip>>();controller.GetOverrides(pairs);Assert.That(pairs.Any(value=>value.Key==catalog&&value.Value==actual),Is.True,"The real scene controller must map the actual catalog reference.");
        Assert.That(Resolve(Motor(controller),catalog),Is.SameAs(actual));
    }
    [Test]
    public void InPlaceUnityOverrideChangesAreObservedWithoutClipNameAliasesOrMotorReprepare()
    {
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(SavedOverride);Assert.That(controller,Is.Not.Null);var catalog=CatalogLook();transient=new AnimatorOverrideController(controller.runtimeAnimatorController);var motor=Motor(transient);
        Assert.That(Resolve(motor,catalog),Is.SameAs(catalog));var actual=AssetDatabase.LoadAssetAtPath<AnimationClip>(DerivedClip);Assert.That(actual,Is.Not.Null);
        transient.ApplyOverrides(new List<KeyValuePair<AnimationClip,AnimationClip>>{new KeyValuePair<AnimationClip,AnimationClip>(catalog,actual)});Assert.That(Resolve(motor,catalog),Is.SameAs(actual));
        replacement=UnityEngine.Object.Instantiate(actual);replacement.name=actual.name;transient.ApplyOverrides(new List<KeyValuePair<AnimationClip,AnimationClip>>{new KeyValuePair<AnimationClip,AnimationClip>(catalog,replacement)});
        Assert.That(Resolve(motor,catalog),Is.SameAs(replacement),"Resolve the exact public override binding, even when distinct clips share a name.");
        transient.ApplyOverrides(new List<KeyValuePair<AnimationClip,AnimationClip>>{new KeyValuePair<AnimationClip,AnimationClip>(catalog,null)});Assert.That(Resolve(motor,catalog),Is.SameAs(catalog));
    }
    [TearDown]public void Cleanup(){if(owner!=null)UnityEngine.Object.DestroyImmediate(owner);if(transient!=null)UnityEngine.Object.DestroyImmediate(transient);if(replacement!=null)UnityEngine.Object.DestroyImmediate(replacement);}
}
#endif
