#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TruckStaticWheelPoseTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
    static object Registry(string name,params object[] args)=>Sports("SportsParticipantRegistry").GetMethod(name).Invoke(null,args);
    static float MaxAxis(MeshFilter mesh,Transform root=null){var size=mesh.sharedMesh.bounds.size;return new[]{Vector3.right*size.x,Vector3.up*size.y,Vector3.forward*size.z}.Max(axis=>root!=null?root.InverseTransformVector(mesh.transform.TransformVector(axis)).magnitude:mesh.transform.TransformVector(axis).magnitude);}
    [TestCase(1f,.6f,.1f)][TestCase(.55f,.33f,.055f)][TestCase(.1f,.06f,.01f)]
    public void LocalDiameterFitRetainsProportionsAcrossUnitPassengerAndMiniatureScale(float scale,float worldDiameter,float offset)
    {
        var args=new object[]{.6f,.4f,2f,.3f*scale,scale,0f,0f};Assert.That(Sports("SportsTruckAppearanceWheelMath").GetMethod("TryFit").Invoke(null,args),Is.EqualTo(true));Assert.That((float)args[5]*2f*.3f*scale,Is.EqualTo(worldDiameter).Within(2e-7));Assert.That((float)args[6],Is.EqualTo(offset).Within(2e-7));
        if(scale==1){Assert.That((float)args[5],Is.EqualTo(.6f/(2f*.3f)),"Unit NWH reference ratio remains unchanged.");Assert.That((float)args[6],Is.EqualTo((.6f-.4f)*.5f),"Unit NWH reference offset remains unchanged.");}
        args[3]=-.3f*scale;Assert.That(Sports("SportsTruckAppearanceWheelMath").GetMethod("TryFit").Invoke(null,args),Is.EqualTo(true));Assert.That((float)args[5]*2f*.3f*scale,Is.EqualTo(worldDiameter).Within(2e-7),"Mirrored canonical art keeps a positive fit without mirroring the actor.");
    }
    [Test]public void InvalidNonuniformAndNonfiniteGeometryFailsClosed()
    {
        var math=Sports("SportsTruckAppearanceWheelMath");foreach(var scale in new[]{new Vector3(1,.55f,1),new Vector3(-1,-1,-1),Vector3.zero,new Vector3(float.NaN,1,1)}){var args=new object[]{scale,0f};Assert.That(math.GetMethod("TryUniformScale").Invoke(null,args),Is.EqualTo(false));}
        foreach(float diameter in new[]{0f,-1f,float.NaN,float.PositiveInfinity}){var args=new object[]{diameter,.4f,2f,.3f,1f,0f,0f};Assert.That(math.GetMethod("TryFit").Invoke(null,args),Is.EqualTo(false));}
    }
    static object Fit(object body,object[] poses)
    {
        var type=Sports("SportsTruckAppearanceCompatibility");var geometryType=type.GetNestedType("WheelGeometry");var fit=Activator.CreateInstance(type);Set(fit,"bodyId",Get(body,"id"));var prefab=(GameObject)Get(body,"visualPrefab");var renderers=prefab.GetComponentsInChildren<Renderer>(true);var names=(string[])Get(body,"wheelRendererNames");var mounts=((Array)Get(body,"wheelMounts")).Cast<object>().ToArray();Assert.That(mounts.Length,Is.EqualTo(4));var geometry=Array.CreateInstance(geometryType,4);
        for(int i=0;i<4;i++)
        {
            int group=i;var item=Activator.CreateInstance(geometryType);Set(item,"physicalWheel",i);Set(item,"center",Get(mounts[i],"position"));Set(item,"euler",Get(mounts[i],"euler"));Set(item,"diameter",((Vector3)Get(mounts[i],"scale")).x);Set(item,"visualDiameter",(float)Get(poses[i],"diameter")*((string)Get(body,"id")=="truck.monster"?1.5f:1f));var indices=Enumerable.Range(0,renderers.Length).Where(index=>names.Contains(renderers[index].name)&&Enumerable.Range(0,4).OrderBy(mount=>(prefab.transform.InverseTransformPoint(renderers[index].bounds.center)-(Vector3)Get(mounts[mount],"position")).sqrMagnitude).First()==group).ToArray();Assert.That(indices,Is.Not.Empty);Set(item,"rendererIndices",indices);geometry.SetValue(item,i);
        }
        Set(fit,"wheelGeometry",geometry);return fit;
    }
    static void Save(string id,string body,object item=null)
    {
        var draft=Registry("GetAppearance",id);Set(draft,"bodyId",body);var selections=(IList)Get(draft,"selections");selections.Clear();if(item!=null){var selection=Activator.CreateInstance(Sports("SportsTruckCosmeticSelection"));Set(selection,"slotId","wheels");Set(selection,"itemId",Get(item,"id"));selections.Add(selection);}Registry("TrySetAppearance",id,draft);Registry("ForgetCache");
    }
    [Test]public void ExistingSavedUnitNwhHostKeepsItsOriginalControllerPosePathAndPhysicalConfiguration()
    {
        const string path="Assets/TruckMusicalParking/Scenes/TruckMusicalParking.unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;IDisposable scope=null;Component host=null;
        try
        {
            scope=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"StaticWheelNwhBackward"+Guid.NewGuid().ToString("N")});Registry("ForgetCache");if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);host=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren(Sports("SportsTruckAppearanceHost"),true)).Cast<Component>().First();Assert.That(((Array)Get(host,"staticWheelPoses")),Is.Empty);Assert.That(host.transform.lossyScale,Is.EqualTo(Vector3.one));var wheels=((Array)Get(host,"wheels")).Cast<object>().ToArray();Assert.That(wheels.Length,Is.EqualTo(4));var physical=host.GetComponentsInChildren<Component>(true).Where(component=>component is Rigidbody||component is Collider).ToDictionary(component=>component,component=>EditorJsonUtility.ToJson(component));string id=(string)Get(Registry("Create","Existing NWH Driver"),"id");
            foreach(string body in new[]{"semi.standard","truck.monster","semi.lowrider","truck.lifted","truck.fire","novelty.hotdog","novelty.icecream"})
            {
                Save(id,body);var draft=Registry("GetAppearance",id);Assert.That(Get(Call(host,"EvaluateDraft",draft),"effectiveBodyId"),Is.EqualTo(body));Assert.That(Call(host,"Prepare",id,Color.cyan),Is.EqualTo(true));CollectionAssert.AreEquivalent(physical.Keys,host.GetComponentsInChildren<Component>(true).Where(component=>component is Rigidbody||component is Collider));foreach(var pair in physical)Assert.That(EditorJsonUtility.ToJson(pair.Key),Is.EqualTo(pair.Value));foreach(var following in ((IEnumerable)Get(host,"wheelVisuals")).Cast<object>()){Assert.That(Get(following,"useLocalCenter"),Is.EqualTo(false));var source=(Transform)Get(following,"source");Assert.That(wheels.Any(wheel=>(Transform)Get(Get(wheel,"wheel"),"rotatingContainer")==source),Is.True);Assert.That(Vector3.Distance(((Transform)Get(following,"visual")).position,source.position+host.transform.up*(float)Get(following,"verticalOffset")),Is.LessThan(2e-5));}
            }
        }
        finally{if(host!=null)Call(host,"RestoreOriginal");if(opened&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);Registry("ForgetCache");scope?.Dispose();}
    }
    [TestCase(1f)][TestCase(.55f)][TestCase(.1f)]
    public void ActualCatalogStaticDefaultsAndCosmeticsFollowOnlyOriginalMeshCentersWithPureReportParity(float scale)
    {
        IDisposable scope=null;GameObject root=null;
        try
        {
            scope=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"StaticWheelPose"+Guid.NewGuid().ToString("N")});Registry("ForgetCache");string id=(string)Get(Registry("Create","Static Pose Driver"),"id");var catalog=AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");Assert.That(catalog,Is.Not.Null);var standard=catalog.GetType().GetMethod("FindBody").Invoke(catalog,new object[]{"semi.standard"});var monster=catalog.GetType().GetMethod("FindBody").Invoke(catalog,new object[]{"truck.monster"});root=new GameObject("Synthetic scaled static-pose integration; actual owned catalog art");root.transform.SetPositionAndRotation(new Vector3(4,7,-3),Quaternion.Euler(0,37,0));root.transform.localScale=Vector3.one*scale;var original=UnityEngine.Object.Instantiate((GameObject)Get(standard,"visualPrefab"),root.transform,false);var renderers=original.GetComponentsInChildren<Renderer>(true);var wheelNames=(string[])Get(standard,"wheelRendererNames");var wheels=renderers.Where(renderer=>wheelNames.Contains(renderer.name)).ToArray();Assert.That(wheels.Length,Is.EqualTo(4));var poses=Array.CreateInstance(Sports("SportsTruckStaticWheelPose"),4);
            for(int i=0;i<4;i++){var pose=Activator.CreateInstance(Sports("SportsTruckStaticWheelPose"));var filter=wheels[i].GetComponent<MeshFilter>();Set(pose,"source",wheels[i].transform);Set(pose,"center",filter.sharedMesh.bounds.center);Set(pose,"diameter",MaxAxis(filter,root.transform));poses.SetValue(pose,i);}
            var host=root.AddComponent(Sports("SportsTruckAppearanceHost"));Set(host,"catalog",catalog);Set(host,"presentationParent",original.transform);Set(host,"originalBodyRenderers",renderers.Except(wheels).ToArray());Set(host,"originalWheelRenderers",wheels);Set(host,"staticWheelPoses",poses);var entries=Array.CreateInstance(Sports("SportsTruckAppearanceCompatibility"),2);entries.SetValue(Fit(standard,poses.Cast<object>().ToArray()),0);entries.SetValue(Fit(monster,poses.Cast<object>().ToArray()),1);Set(host,"compatibleBodies",entries);
            foreach(var body in new[]{standard,monster})
            {
                string bodyId=(string)Get(body,"id");Save(id,bodyId);var draft=Registry("GetAppearance",id);int revision=(int)Get(host,"AppearanceRevision"),children=root.GetComponentsInChildren<Transform>(true).Length;var report=Call(host,"EvaluateDraft",draft);Assert.That(Get(report,"canApply"),Is.EqualTo(true));Assert.That(Get(report,"effectiveBodyId"),Is.EqualTo(bodyId));Assert.That(Get(host,"AppearanceRevision"),Is.EqualTo(revision));Assert.That(root.GetComponentsInChildren<Transform>(true).Length,Is.EqualTo(children),"Pure evaluation must not compose or mutate source geometry.");Assert.That(Call(host,"Prepare",id,Color.cyan),Is.EqualTo(true));Assert.That(Get(host,"EffectiveBodyId"),Is.EqualTo(Get(report,"effectiveBodyId")));Assert.That(wheels.All(renderer=>!renderer.enabled),Is.True);var model=(Transform)Get(host,"AppearanceRoot");Assert.That(model.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(model.GetComponentsInChildren<Rigidbody>(true),Is.Empty);
                var following=((IEnumerable)Get(host,"wheelVisuals")).Cast<object>().ToArray();Assert.That(following.Length,Is.EqualTo(4));foreach(var wheel in following){var source=(Transform)Get(wheel,"source");var pose=poses.Cast<object>().Single(candidate=>Get(candidate,"source")==source);float local=(float)Get(pose,"diameter");float expectedOffset=bodyId=="truck.monster"?local*.5f*scale*.5f:0;var visual=(Transform)Get(wheel,"visual");Assert.That(Vector3.Distance(visual.position,source.TransformPoint((Vector3)Get(pose,"center"))+root.transform.up*expectedOffset),Is.LessThan(2e-5));}
                var first=following[0];var originalSource=(Transform)Get(first,"source");originalSource.position+=new Vector3(.1f,.2f,-.3f);originalSource.rotation=Quaternion.AngleAxis(41,root.transform.right)*originalSource.rotation;Call(host,"LateUpdate");var actual=(Transform)Get(first,"visual");Assert.That(Vector3.Distance(actual.position,originalSource.TransformPoint((Vector3)Get(first,"localCenter"))+root.transform.up*(float)Get(first,"verticalOffset")),Is.LessThan(2e-5));Assert.That(Quaternion.Angle(actual.rotation,originalSource.rotation*(Quaternion)Get(first,"alignment")),Is.LessThan(.001f));
            }
            var cosmetic=((Array)Get(catalog,"cosmetics")).Cast<object>().First(item=>(string)Get(item,"slotId")=="wheels"&&(bool)Get(item,"rotatesWithWheel")&&(bool)Call(item,"CompatibleWith","truck.monster"));Save(id,"truck.monster",cosmetic);var selected=Registry("GetAppearance",id);var supported=Call(host,"EvaluateDraft",selected);Assert.That(((string[])Get(supported,"unavailableSlots")),Is.Empty);Assert.That(Call(host,"Prepare",id,Color.magenta),Is.EqualTo(true));foreach(var wheel in ((IEnumerable)Get(host,"wheelVisuals")).Cast<object>()){Assert.That(Get(wheel,"verticalOffset"),Is.EqualTo(0f));Assert.That(Vector3.Distance(((Transform)Get(wheel,"visual")).position,((Transform)Get(wheel,"source")).TransformPoint((Vector3)Get(wheel,"localCenter"))),Is.LessThan(2e-5));}
            string saved=JsonUtility.ToJson(Registry("GetAppearance",id));Set(poses.GetValue(1),"source",Get(poses.GetValue(0),"source"));Assert.That(Get(Call(host,"EvaluateDraft",Registry("GetAppearance",id)),"canApply"),Is.EqualTo(false),"Duplicate actual pose authority must fail closed.");Assert.That(JsonUtility.ToJson(Registry("GetAppearance",id)),Is.EqualTo(saved));Assert.That(root.GetComponentsInChildren<Rigidbody>(true),Is.Empty);Assert.That(root.GetComponentsInChildren<Collider>(true),Is.Empty);
            TestContext.WriteLine("STATIC_WHEEL_POSE synthetic ownerScale="+scale+" actual installed standard/Monster meshes+cosmetic, source center/rotation authority, scale math/pure parity and no physics. Saved sport authoring/native vehicle acceptance is separate.");
        }
        finally{if(root!=null)UnityEngine.Object.DestroyImmediate(root);Registry("ForgetCache");scope?.Dispose();}
    }
}
#endif
