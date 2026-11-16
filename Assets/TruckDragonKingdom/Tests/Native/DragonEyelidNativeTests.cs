#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

// Actual saved configured animal + original public mode API. No Animator.Play,
// animation sampling, bone/mesh edits, forced states or synthetic callbacks.
public sealed class DragonEyelidNativeTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    GameObject animal,floor,lamp,fillLamp,cameraOwner;
    Light previousSun,keyLight,fillLight;
    bool changedSun;
    Component motor,lids;
    Camera camera;
    string output;
    readonly StringBuilder log=new StringBuilder();
    static Type Runtime(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(assembly=>assembly.GetType(name,false)).First(type=>type!=null);
    static object Read(object instance,string name)=>instance.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(instance):instance.GetType().GetProperty(name,Any).GetValue(instance);
    static object Call(object instance,string name,params object[] args)=>instance.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(instance,args);
    object Action(string name)=>Enum.Parse(Runtime("LWS.TruckDragonKingdom.DragonAnimationAction"),name);
    bool Playing(string action)=>(bool)Read(motor,"ActionPlaying")&&Read(motor,"CurrentAction").ToString()==action;
    void Log(string value){log.AppendLine(value);TestContext.WriteLine(value);File.WriteAllText(Path.Combine(output,"observations.txt"),log.ToString());}
    IEnumerator Until(Func<bool> condition,float seconds,string stage)
    {
        float deadline=Time.realtimeSinceStartup+seconds;
        while(!condition()&&Time.realtimeSinceStartup<deadline)yield return null;
        Log(stage+" action="+Read(motor,"CurrentAction")+" playing="+Read(motor,"ActionPlaying")+" phase="+Read(motor,"ActionNormalizedTime")+" closure="+Read(lids,"Closure")+" observedSleep="+Read(lids,"ObservedSleepPlayback"));
        Assert.That(condition(),Is.True,"Actual native eyelid stage timed out: "+stage);
    }
    int CaptureEye(string name)
    {
        var renderer=((SkinnedMeshRenderer[])Read(lids,"lids"))[0];var head=(Transform)Read(lids,"actualHead");var mesh=renderer.sharedMesh;
        var local=mesh.bounds;var corners=new Vector3[8];int index=0;
        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)corners[index++]=head.TransformPoint(local.center+Vector3.Scale(local.extents,new Vector3(x,y,z)));
        var bounds=new Bounds(corners[0],Vector3.zero);foreach(var point in corners)bounds.Encapsulate(point);
        Vector3 normal=Vector3.zero;foreach(var value in mesh.normals)normal+=value;normal=head.TransformDirection(normal.normalized).normalized;
        float radius=Mathf.Max(.2f,bounds.extents.magnitude);camera.transform.SetPositionAndRotation(bounds.center+normal*radius*3.5f,Quaternion.LookRotation(-normal,animal.transform.up));camera.aspect=16f/9;camera.fieldOfView=32;camera.cullingMask=1<<head.gameObject.layer;
        // The original arbitrary world sun lit the brow while leaving the
        // actual painted white eye in shadow. Illuminate this same real
        // head-attached view directly, without changing art or pixel gates.
        keyLight.transform.rotation=camera.transform.rotation;
        fillLight.transform.rotation=Quaternion.AngleAxis(32,camera.transform.up)*camera.transform.rotation;
        keyLight.cullingMask=fillLight.cullingMask=camera.cullingMask;
        var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);var prior=RenderTexture.active;var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);int white=0;
        try
        {
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
            foreach(var pixel in texture.GetPixels32())if(pixel.r>170&&pixel.g>150&&pixel.b>130&&Mathf.Max(pixel.r,pixel.g,pixel.b)-Mathf.Min(pixel.r,pixel.g,pixel.b)<90)white++;
            string path=Path.Combine(output,name+".png");File.WriteAllBytes(path,texture.EncodeToPNG());Log("CONTROLLED_ACTUAL_EYE_RENDER "+path+" whitePixels="+white+" closure="+Read(lids,"Closure")+" bounds="+bounds+" head="+head.position+" camera="+camera.transform.position+" keyForward="+keyLight.transform.forward+" keyIntensity="+keyLight.intensity+" fillForward="+fillLight.transform.forward+" fillIntensity="+fillLight.intensity+"; subject-only actual camera view, unchanged white/occlusion gates; no Windows/human visual acceptance.");
        }
        finally{camera.targetTexture=null;RenderTexture.active=prior;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
        return white;
    }
    [UnityTest,Timeout(90000)]
    public IEnumerator ActualSurfaceLidsCloseOverPaintedEyesDuringSleepFreezeOnPauseAndReopenWithWake()
    {
        var args=Environment.GetCommandLineArgs();int option=Array.IndexOf(args,"-tsDragonOutput");output=Path.GetFullPath(Path.Combine(option>=0&&option+1<args.Length?args[option+1]:Path.Combine(Application.persistentDataPath,"DragonNative"),"Eyelids",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")));Directory.CreateDirectory(output);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckDragonKingdom/Prefabs/Dragons/dragon-inferno/dragon-inferno Dragon Animal.prefab");Assert.That(prefab,Is.Not.Null);
        floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Actual physical eyelid-fixture support";floor.transform.position=new Vector3(0,-1,0);floor.transform.localScale=new Vector3(400,2,400);
        animal=UnityEngine.Object.Instantiate(prefab,Vector3.up,Quaternion.identity);motor=animal.GetComponentInChildren(Runtime("LWS.TruckDragonKingdom.DragonAnimalMotor"));lids=animal.GetComponentInChildren(Runtime("LWS.TruckDragonKingdom.DragonEyelidPresentation"));Assert.That(lids,Is.Not.Null,"Run the explicit selected eyelid prefab installer first.");Call(lids,"ValidateBindings");
        var catalog=AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset");var lookup=new object[]{"dragon-inferno",null};Assert.That((bool)Call(catalog,"TryGet",lookup),Is.True);Call(motor,"Prepare",lookup[1]);
        lamp=new GameObject("Controlled eye-facing key");keyLight=lamp.AddComponent<Light>();keyLight.type=LightType.Directional;keyLight.color=Color.white;keyLight.intensity=2.4f;keyLight.shadows=LightShadows.None;
        fillLamp=new GameObject("Controlled eye-facing fill");fillLight=fillLamp.AddComponent<Light>();fillLight.type=LightType.Directional;fillLight.color=Color.white;fillLight.intensity=.6f;fillLight.shadows=LightShadows.None;
        previousSun=RenderSettings.sun;changedSun=true;RenderSettings.sun=keyLight;
        cameraOwner=new GameObject("Controlled actual eye camera");camera=cameraOwner.AddComponent<Camera>();camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.035f,.055f);camera.nearClipPlane=.05f;camera.farClipPlane=500;
        for(int step=0;step<35;step++){Call(motor,"StopMotion");yield return new WaitForFixedUpdate();}
        var actualLids=(SkinnedMeshRenderer[])Read(lids,"lids");var creases=(SkinnedMeshRenderer[])Read(lids,"creases");Assert.That(actualLids.Length,Is.EqualTo(2));Assert.That(creases.Length,Is.EqualTo(2));
        foreach(var renderer in actualLids.Concat(creases)){Assert.That(renderer.sharedMesh.blendShapeCount,Is.EqualTo(1));Assert.That(renderer.sharedMesh.GetBlendShapeName(0),Is.EqualTo("LWS Visible Eyelid Closure"));Assert.That(renderer.bones.Length,Is.EqualTo(1));Assert.That(renderer.bones[0],Is.SameAs(Read(lids,"actualHead")));}
        int open=CaptureEye("awake-open-eye");Assert.That(open,Is.GreaterThan(300),"The actual awake painted eye must be visible in the retained calibrated view.");
        Assert.That((bool)Call(motor,"TryAction",Action("Sleep"),2f),Is.True);
        yield return Until(()=>Playing("Sleep")&&(float)Read(lids,"Closure")>.99f&&(int)Read(lids,"ClosedFrames")>3,15,"ACTUAL_SLEEP_CLOSED");
        Assert.That(actualLids.All(renderer=>renderer.enabled&&renderer.GetBlendShapeWeight(0)>99),Is.True);Assert.That(creases.All(renderer=>renderer.enabled&&renderer.GetBlendShapeWeight(0)>99),Is.True);
        int closed=CaptureEye("sleep-actual-closed-eyelids");Assert.That(closed,Is.LessThan(open*.3f),"The actual generated geometry must occlude most original eye-white pixels; curve/morph existence alone is insufficient.");
        Call(motor,"Pause",true);float held=(float)Read(lids,"Closure");var weights=actualLids.Select(renderer=>renderer.GetBlendShapeWeight(0)).ToArray();yield return new WaitForSecondsRealtime(.3f);Assert.That(Read(lids,"Closure"),Is.EqualTo(held));CollectionAssert.AreEqual(weights,actualLids.Select(renderer=>renderer.GetBlendShapeWeight(0)).ToArray());Call(motor,"Pause",false);
        Call(motor,"InterruptAction");yield return Until(()=>!(bool)Read(motor,"ActionPlaying"),5,"SLEEP_INTERRUPTED_THROUGH_ORIGINAL_API");Assert.That((bool)Call(motor,"TryAction",Action("Wake"),2f),Is.True);
        yield return Until(()=>Playing("Wake")&&(float)Read(motor,"ActionNormalizedTime")>.3f&&(float)Read(lids,"Closure")<.01f,10,"ACTUAL_WAKE_OPEN");int awake=CaptureEye("wake-actual-reopened-eye");Assert.That(awake,Is.GreaterThan(closed+300));Assert.That(actualLids.All(renderer=>!renderer.enabled),Is.True);
        Log("PASSED actual painted eye visible before/after, real surface lids occlude during actual Sleep playback, pause freezes mesh weights, Wake reopens; retained renders still require human/lead inspection. No source art/rig/clip edits or fake Animator channels.");
    }
    [TearDown] public void Cleanup()
    {
        if(motor!=null&&(bool)Read(motor,"Paused"))Call(motor,"Pause",false);
        if(changedSun){RenderSettings.sun=previousSun!=null?previousSun:null;changedSun=false;}
        foreach(var owner in new[]{animal,floor,lamp,fillLamp,cameraOwner})if(owner!=null)UnityEngine.Object.DestroyImmediate(owner);motor=lids=null;
    }
}
#endif
