#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

// Real Heist override/curled clip, same existing Inferno surface lids and real
// original NWH sight target. Ordinary Prepare/Mode actions only, no phase,
// bone, visibility, contact, score or callback injection.
public sealed class HeistSleepingPeekNativeTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const string Vigil="Assets/TruckHoardHeist/PrivateLicensedPotteryFragments/HeistMotion/Curled sleeping vigilance.anim";
    const string Controller="Assets/TruckHoardHeist/Controllers/Heist guardian sleeping vigilance.overrideController";
    readonly List<GameObject> owned=new List<GameObject>();readonly StringBuilder log=new StringBuilder();
    Component actor,motor,lids,truck;GameObject animal;object species;Array roster;Camera camera;Light key,fill,oldSun;string output;
    AudioListener[] previousListeners;bool[] previousListenerEnabled;
    bool priorAsyncCompilation,changedAsyncCompilation;
    static Type Runtime(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name,false)).First(t=>t!=null);
    static object Read(object value,string name)=>value.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(value):value.GetType().GetProperty(name,Any).GetValue(value);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(value,args);
    object Action(string name)=>Enum.Parse(Runtime("LWS.TruckDragonKingdom.DragonAnimationAction"),name);
    void Log(string value){log.AppendLine(value);TestContext.WriteLine(value);File.WriteAllText(Path.Combine(output,"observations.txt"),log.ToString());}
    IEnumerator Until(Func<bool> condition,float seconds,string stage)
    {
        float until=Time.realtimeSinceStartup+seconds;while(!condition()&&Time.realtimeSinceStartup<until){Call(actor,"Tick",Time.fixedDeltaTime);yield return new WaitForFixedUpdate();yield return null;}
        Log(stage+" closure="+Read(lids,"Closure")+" eyesOpen="+Read(lids,"EyesOpenForVision")+" peek="+Read(lids,"ObservedPeekPlayback")+" phase="+Read(lids,"PeekNormalizedTime")+" action="+Read(motor,"CurrentAction")+" observed="+Read(motor,"ActionPlaying")+" seesActualTruck="+Call(actor,"CanObserveTarget",0));Assert.That(condition(),Is.True,stage);
    }
    int CaptureEye(string label)
    {
        var lid=((SkinnedMeshRenderer[])Read(lids,"lids"))[0];var head=(Transform)Read(lids,"actualHead");var mesh=lid.sharedMesh;var b=mesh.bounds;var points=new List<Vector3>();
        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)points.Add(head.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));
        var bounds=new Bounds(points[0],Vector3.zero);foreach(var point in points)bounds.Encapsulate(point);
        Vector3 normal=Vector3.zero;foreach(var n in mesh.normals)normal+=n;normal=head.TransformDirection(normal.normalized).normalized;float radius=Mathf.Max(.2f,bounds.extents.magnitude);
        camera.transform.SetPositionAndRotation(bounds.center+normal*radius*3.5f,Quaternion.LookRotation(-normal,animal.transform.up));key.transform.rotation=camera.transform.rotation;fill.transform.rotation=Quaternion.AngleAxis(32,camera.transform.up)*camera.transform.rotation;
        var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);var prior=RenderTexture.active;var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);int white=0;
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();foreach(var pixel in texture.GetPixels32())if(pixel.r>170&&pixel.g>150&&pixel.b>130&&Mathf.Max(pixel.r,pixel.g,pixel.b)-Mathf.Min(pixel.r,pixel.g,pixel.b)<90)white++;File.WriteAllBytes(Path.Combine(output,label+".png"),texture.EncodeToPNG());Log("ACTUAL_EYE_RENDER "+label+" white="+white+" phase="+Read(lids,"PeekNormalizedTime")+" closure="+Read(lids,"Closure"));}
        finally{camera.targetTexture=null;RenderTexture.active=prior;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}return white;
    }
    void Construct()
    {
        priorAsyncCompilation=ShaderUtil.allowAsyncCompilation;changedAsyncCompilation=true;ShaderUtil.allowAsyncCompilation=false;
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");output=Path.GetFullPath(Path.Combine(at>=0&&at+1<args.Length?args[at+1]:Application.persistentDataPath,"HeistSleepingPeek",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")));Directory.CreateDirectory(output);
        Assert.That(File.ReadAllText("Assets/TruckHoardHeist/Scenes/TruckHoardHeist.unity"),Does.Contain("sleepingPeekClips"),"Explicitly migrate the saved scene binding first.");
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);owned.Add(floor);floor.transform.position=new Vector3(0,-1,0);floor.transform.localScale=new Vector3(400,2,400);
        animal=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckDragonKingdom/Prefabs/Dragons/dragon-inferno/dragon-inferno Dragon Actor.prefab"),Vector3.up,Quaternion.identity);owned.Add(animal);
        actor=animal.GetComponent(Runtime("LWS.TruckDragonKingdom.DragonCharacter"));motor=(Component)Read(actor,"motor");lids=motor.GetComponent(Runtime("LWS.TruckDragonKingdom.DragonEyelidPresentation"));
        Assert.That(((AnimationClip[])Read(lids,"sleepingPeekClips")),Is.Empty,"Ordinary core Actor prefab remains unchanged and has no Heist-specific peek.");
        var animator=(Animator)Read(motor,"animator");animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(Controller);Assert.That(animator.runtimeAnimatorController,Is.Not.Null);
        var vigil=AssetDatabase.LoadAssetAtPath<AnimationClip>(Vigil);Assert.That(vigil,Is.Not.Null);Call(lids,"RegisterSleepingPeekClip",vigil);Call(lids,"ValidateBindings");
        // Apply the same saved Heist-only PlayOnce configuration before Prepare.
        // This cannot create/repair persistent assets or modify an active action.
        Runtime("LWS.TruckHoardHeist.Editor.HeistSleepingVigilExitAuthoring").GetMethod("ConfigureNativeFixtureBeforePrepare",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{motor});
        foreach(var t in animal.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
        var catalog=AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset");var lookup=new object[]{"dragon-inferno",null};Assert.That((bool)Call(catalog,"TryGet",lookup),Is.True);species=lookup[1];
        // Initial physical target arrangement, before the ordinary Prepare.
        // Same genuine NWH prefab used by the accepted real Breath fixture.
        var nwh=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckFareFight/Prefabs/Fare Fight Taxi Tractor.prefab"),new Vector3(0,6,27),Quaternion.identity);owned.Add(nwh);truck=nwh.GetComponent(Runtime("LWS.TruckSports.SportsEventTruck"));((Behaviour)truck).enabled=false;Call(truck,"Power",false);
        foreach(var body in nwh.GetComponentsInChildren<Rigidbody>(true))body.useGravity=false;foreach(var sound in nwh.GetComponentsInChildren<AudioSource>(true)){sound.Stop();sound.mute=true;}
        roster=Array.CreateInstance(truck.GetType(),1);roster.SetValue(truck,0);Physics.SyncTransforms();
        previousListeners=UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include,FindObjectsSortMode.None);previousListenerEnabled=previousListeners.Select(listener=>listener.enabled).ToArray();foreach(var listener in previousListeners)listener.enabled=false;
        var cameraOwner=new GameObject("Controlled actual vigilance eye camera");owned.Add(cameraOwner);cameraOwner.AddComponent<AudioListener>();camera=cameraOwner.AddComponent<Camera>();camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.035f,.055f);camera.cullingMask=1<<30;camera.aspect=16f/9;camera.fieldOfView=32;camera.nearClipPlane=.05f;camera.farClipPlane=500;
        var k=new GameObject("Actual eye-facing key");owned.Add(k);key=k.AddComponent<Light>();key.type=LightType.Directional;key.color=Color.white;key.intensity=2.4f;key.shadows=LightShadows.None;key.cullingMask=1<<30;
        var f=new GameObject("Actual eye-facing fill");owned.Add(f);fill=f.AddComponent<Light>();fill.type=LightType.Directional;fill.color=Color.white;fill.intensity=.6f;fill.shadows=LightShadows.None;fill.cullingMask=1<<30;oldSun=RenderSettings.sun;RenderSettings.sun=key;
    }
    [UnityTest,Timeout(120000)]
    public IEnumerator RealCurledHeistVigilanceClosesPeeksClosesBlocksFreshSightAndReusesRigOnPrepare()
    {
        Construct();int actorId=actor.GetInstanceID(),truckId=truck.GetInstanceID();var meshes=((SkinnedMeshRenderer[])Read(lids,"lids")).Select(renderer=>renderer.sharedMesh).ToArray();
        int openReference=0;
        for(int run=1;run<=2;run++)
        {
            Call(actor,"Prepare",species,roster,1,917+run);Call(actor,"SetIntent",Enum.Parse(Runtime("LWS.TruckDragonKingdom.DragonCharacterIntent"),"Passive"),motor.transform.position,-1);
            for(int i=0;i<35;i++){Call(actor,"Tick",Time.fixedDeltaTime);yield return new WaitForFixedUpdate();yield return null;}
            if(run==1){openReference=CaptureEye("run1-initial-open-reference");Assert.That(openReference,Is.GreaterThan(300));Assert.That(Call(actor,"CanObserveTarget",0),Is.EqualTo(true),"The genuine unobstructed NWH target must be visible before sleeping.");}
            Assert.That((bool)Call(motor,"TryAction",Action("Sleep"),2f),Is.True);yield return Until(()=>(bool)Read(motor,"ActionPlaying")&&Read(motor,"CurrentAction").ToString()=="Sleep"&&(bool)Read(lids,"ObservedSleepPlayback")&&(float)Read(lids,"Closure")>.99f,12,"run"+run+"-actual-weighted-sleep-closed");
            Assert.That(Call(actor,"CanObserveTarget",0),Is.EqualTo(false));int closed=CaptureEye("run"+run+"-actual-sleep-closed");Assert.That(closed,Is.LessThan(openReference*.3f));
            Call(motor,"InterruptAction");yield return Until(()=>!(bool)Read(motor,"ActionPlaying"),5,"run"+run+"-sleep-natural-interrupt");
            float until=Time.realtimeSinceStartup+5;bool accepted=false;while(!accepted&&Time.realtimeSinceStartup<until){accepted=(bool)Call(motor,"TryAction",Action("LookAround"),.5f);if(!accepted){Call(actor,"Tick",Time.fixedDeltaTime);yield return new WaitForFixedUpdate();yield return null;}}Assert.That(accepted,Is.True);
            yield return Until(()=>(bool)Read(lids,"ObservedPeekPlayback")&&(float)Read(lids,"Closure")<.01f,8,"run"+run+"-real-weighted-vigilance-open");
            Assert.That((AnimationClip)Read(lids,"ObservedPeekClip"),Is.SameAs(AssetDatabase.LoadAssetAtPath<AnimationClip>(Vigil)));Assert.That(Read(motor,"CurrentAction").ToString(),Is.EqualTo("LookAround"));Assert.That(Call(actor,"CanObserveTarget",0),Is.EqualTo(true));
            int peek=CaptureEye("run"+run+"-actual-peek-open");Assert.That(peek,Is.GreaterThan(closed+300));
            Call(actor,"Pause",true);float held=(float)Read(lids,"Closure"),phase=(float)Read(lids,"PeekNormalizedTime");yield return new WaitForSecondsRealtime(.3f);Assert.That(Read(lids,"Closure"),Is.EqualTo(held));Assert.That(Read(lids,"PeekNormalizedTime"),Is.EqualTo(phase));Assert.That(Call(actor,"CanObserveTarget",0),Is.EqualTo(false));Call(actor,"Pause",false);
            yield return Until(()=>(bool)Read(lids,"ObservedPeekPlayback")&&(float)Read(lids,"PeekNormalizedTime")>.75f&&(float)Read(lids,"Closure")>.99f,6,"run"+run+"-same-actual-vigilance-closed-again");Assert.That(Call(actor,"CanObserveTarget",0),Is.EqualTo(false));int again=CaptureEye("run"+run+"-actual-peek-closed-again");Assert.That(again,Is.LessThan(peek*.3f));
            yield return Until(()=>!(bool)Read(motor,"ActionPlaying"),6,"run"+run+"-real-vigilance-completed");Assert.That(Read(motor,"LastActionCompleted"),Is.EqualTo(true));Assert.That((float)Read(motor,"LastActionNormalizedTime"),Is.GreaterThanOrEqualTo(.9f));Assert.That((bool)Read(lids,"EyesOpenForVision"),Is.False,"Natural exit preserves sleeping closed-eye latch.");Assert.That(actor.GetInstanceID(),Is.EqualTo(actorId));Assert.That(truck.GetInstanceID(),Is.EqualTo(truckId));CollectionAssert.AreEqual(meshes,((SkinnedMeshRenderer[])Read(lids,"lids")).Select(renderer=>renderer.sharedMesh).ToArray());Call(actor,"ResetCharacter");
            // No yielded frame or manual presentation call: the ordinary actor
            // lifecycle itself must clear synchronous reset/Prepare history.
            Assert.That(Read(lids,"Closure"),Is.EqualTo(0f));Assert.That(Read(lids,"ObservedSleepPlayback"),Is.EqualTo(false));Assert.That(Read(lids,"ObservedPeekPlayback"),Is.EqualTo(false));Assert.That(Read(lids,"ObservedPeekClip"),Is.Null);Assert.That(Read(lids,"PeekNormalizedTime"),Is.EqualTo(0f));Assert.That(Read(lids,"ClosedFrames"),Is.EqualTo(0));
            foreach(var renderer in ((SkinnedMeshRenderer[])Read(lids,"lids")).Concat((SkinnedMeshRenderer[])Read(lids,"creases"))){Assert.That(renderer.enabled,Is.False);Assert.That(renderer.GetBlendShapeWeight(0),Is.EqualTo(0f));}
        }
        Log("PASS actual curled derived LookAround weighted phase drives same real lids closed→peek→closed, fresh actual NWH vision only when open, ordinary pause/Prepare rematch reuses rig/mesh/target. Hearing/noise/scoring remain separate authorities; no phase/state/contact injection or human visual acceptance.");
    }
    [TearDown]public void Cleanup(){if(actor!=null)Call(actor,"ResetCharacter");if(key!=null)RenderSettings.sun=oldSun!=null?oldSun:null;for(int i=owned.Count-1;i>=0;i--)if(owned[i]!=null)UnityEngine.Object.DestroyImmediate(owned[i]);owned.Clear();if(previousListeners!=null)for(int i=0;i<previousListeners.Length;i++)if(previousListeners[i]!=null)previousListeners[i].enabled=previousListenerEnabled[i];previousListeners=null;if(changedAsyncCompilation){ShaderUtil.allowAsyncCompilation=priorAsyncCompilation;changedAsyncCompilation=false;}}
}
#endif
