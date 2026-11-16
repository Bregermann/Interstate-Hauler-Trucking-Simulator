#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Saved-scene broadcast acceptance. Ordinary AI start/rematch, real frames and
// actual Cinemachine framing only; no actor/camera pose, score or input injection.
public sealed class DragonBroadcastNativeTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    Component match,rig,framing;Camera camera;GameObject isolation;IDisposable preferences;string output,mode;bool riders;
    readonly List<Frame> frames=new List<Frame>();readonly StringBuilder draw=new StringBuilder();
    [Serializable] sealed class Frame
    {
        public string phase,matchId,reason,primaryName;public int run,matchSeed,primary,frame,evaluations,journalCount;public bool ownsShot,valid,legacyAllowed,paused;public float countdown,clock,minimumHeight;public Vector3 cameraPosition,cameraEuler,bodyPosition,primaryViewport;public Rect projected;public Bounds bounds;
        public Vector3 vendorPosition,readabilityPosition,vendorFocus,terrainSafetyAdjustment;public Rect vendorProjection;
        public string[] cameraViews,rayHits;
    }
    [Serializable] sealed class CaptureFact
    {public string scope="CONTROLLED_NATIVE_SOFTWARE_AI_CAMERA_RENDER_NOT_WINDOWS_HUMAN_HARDWARE_OR_PERFORMANCE_ACCEPTANCE",scene,png,phase,matchId;public int width,height,run,primary,visibleBodyPixels;public float configuredMinimum;public Rect projected;public string[] bodyDrawState;}
    sealed class Paint
    {public Renderer renderer;public Material[] materials;public MaterialPropertyBlock global;public MaterialPropertyBlock[] slots;}
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static object Read(object value,string name)
    {
        for(Type type=value.GetType();type!=null;type=type.BaseType){var field=type.GetField(name,Any|BindingFlags.DeclaredOnly);if(field!=null)return field.GetValue(value);var property=type.GetProperty(name,Any|BindingFlags.DeclaredOnly);if(property!=null)return property.GetValue(value);}throw new MissingMemberException(value.GetType().FullName,name);
    }
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length&&method.GetParameters().Select((parameter,index)=>args[index]==null||parameter.ParameterType.IsInstanceOfType(args[index])).All(ok=>ok)).Invoke(value,args);
    float Countdown=>(float)Read(match,riders?"Countdown":"Intro");
    int JournalCount=>(int)Read(Read(match,"ScoreJournal"),"Count");
    static string PathOf(Transform value){string path=value.name;while(value.parent!=null){value=value.parent;path=value.name+"/"+path;}return path;}
    Component Truck(int slot)=>(Component)((Array)Read(match,"trucks")).GetValue(slot);
    Frame Observe(string phase,int run,bool require)
    {
        var fact=new Frame{phase=phase,run=run,matchSeed=(int)Read(match,"MatchSeed"),matchId=(string)Read(match,"MatchId"),frame=Time.frameCount,countdown=Countdown,clock=(float)Read(match,"Clock"),paused=(bool)Read(match,"Paused"),journalCount=JournalCount,cameraPosition=camera.transform.position,cameraEuler=camera.transform.eulerAngles,cameraViews=((Camera[])Read(match,"cameras")).Select(view=>view.name+" enabled="+view.enabled+" active="+view.gameObject.activeInHierarchy+" rect="+view.rect).ToArray()};
        if(framing!=null)
        {
            fact.primary=(int)Read(framing,"Primary");fact.ownsShot=(bool)Read(framing,"OwnsShot");fact.reason=(string)Read(framing,"ShotReason");fact.evaluations=(int)Read(rig,"FrameEvaluations");fact.legacyAllowed=(bool)Read(rig,"LegacyPoseAllowed");fact.minimumHeight=(float)Read(Read(framing,"Configuration"),"minimumTruckHeight");
            fact.vendorPosition=(Vector3)Read(framing,"VendorPosition");fact.vendorProjection=(Rect)Read(framing,"VendorProjection");fact.readabilityPosition=(Vector3)Read(framing,"ReadabilityPosition");fact.vendorFocus=(Vector3)Read(framing,"VendorFocus");fact.terrainSafetyAdjustment=(Vector3)Read(framing,"TerrainSafetyAdjustment");
            if(fact.primary>=0&&fact.primary<4)
            {
                fact.valid=(bool)Call(framing,"Valid",fact.primary);fact.primaryName=Truck(fact.primary).name;fact.bodyPosition=((Rigidbody)Read(Truck(fact.primary),"body")).position;fact.bounds=(Bounds)Call(framing,"TruckBounds",fact.primary);fact.projected=(Rect)Call(framing,"ProjectedTruckRect",camera,fact.primary);fact.primaryViewport=camera.WorldToViewportPoint(fact.bounds.center);
                Vector3 delta=fact.bounds.center-camera.transform.position;fact.rayHits=Physics.RaycastAll(camera.transform.position,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore).Where(hit=>!hit.collider.transform.IsChildOf(Truck(fact.primary).transform)).OrderBy(hit=>hit.distance).Take(12).Select(hit=>PathOf(hit.collider.transform)+" distance="+hit.distance+" point="+hit.point).ToArray();
            }
        }
        frames.Add(fact);TestContext.WriteLine(JsonUtility.ToJson(fact));Save();
        if(require)
        {
            Assert.That(framing,Is.Not.Null,"Saved broadcast must bind existing SportsBroadcastFraming.");Assert.That(Read(rig,"broadcastFraming"),Is.SameAs(framing));Assert.That(Read(framing,"cameraRig"),Is.SameAs(rig));Assert.That(Read(framing,"session"),Is.SameAs(match));Assert.That(fact.ownsShot&&fact.valid&&!fact.legacyAllowed,Is.True,"Actual shared Cinemachine framing must own a valid participant shot.");
            Assert.That(fact.primary,Is.InRange(0,3));Assert.That(Call(framing,"Body",fact.primary),Is.SameAs(Read(Truck(fact.primary),"body")));Assert.That(Call(match,"BroadcastParticipantActive",fact.primary),Is.EqualTo(true));
            Assert.That(camera.enabled&&camera.gameObject.activeInHierarchy,Is.True);Assert.That(((Camera[])Read(match,"cameras")),Is.EqualTo(new[]{camera}),"AI cameras OFF must use the one actual broadcast.");
            Assert.That(fact.primaryViewport.z,Is.GreaterThan(camera.nearClipPlane));Assert.That(fact.projected.height,Is.GreaterThanOrEqualTo(fact.minimumHeight*.98f),"Use the existing framing component's configured readable-height gate.");
            Assert.That(fact.projected.xMin,Is.GreaterThanOrEqualTo(.01f));Assert.That(fact.projected.xMax,Is.LessThanOrEqualTo(.99f));Assert.That(fact.projected.yMin,Is.GreaterThanOrEqualTo(.12f));Assert.That(fact.projected.yMax,Is.LessThanOrEqualTo(.92f));
        }
        return fact;
    }
    void Save(){if(output!=null){File.WriteAllText(System.IO.Path.Combine(output,"frames.json"),JsonUtility.ToJson(new FrameList{scene=mode,scope="Normal timeScale1, original four-AI setup/start/countdown/GO/pause/rematch; no pose/score/camera injection.",frames=frames.ToArray()},true));File.WriteAllText(System.IO.Path.Combine(output,"actual-camera-draw.txt"),draw.ToString());}}
    [Serializable] sealed class FrameList{public string scene,scope;public Frame[] frames;}
    static Texture2D Render(Camera view,RenderTexture target)
    {var prior=RenderTexture.active;try{view.targetTexture=target;view.Render();RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();return image;}finally{RenderTexture.active=prior;}}
    void Capture(string phase,int run,int width,int height,bool witness)
    {
        var primary=(int)Read(framing,"Primary");var truck=Truck(primary);var host=Read(truck,"appearanceHost");Assert.That(host,Is.Not.Null);
        var renderers=((IEnumerable)Read(host,"ComposedBodyRenderers")).Cast<Renderer>().Where(value=>value!=null&&value.enabled&&value.gameObject.activeInHierarchy&&!value.forceRenderingOff).Distinct().ToArray();Assert.That(renderers,Is.Not.Empty,"Visible original composed tractor body required.");
        var physical=match.gameObject.scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Rigidbody>(true)).Select(body=>(body,position:body.position,rotation:body.rotation,velocity:body.linearVelocity,angular:body.angularVelocity,mass:body.mass)).ToArray();int journal=JournalCount;float clock=(float)Read(match,"Clock");
        string prefix=System.IO.Path.Combine(output,"run-"+run+"-"+phase+"-"+width+"x"+height);var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);var oldTarget=camera.targetTexture;float oldAspect=camera.aspect;var paints=new List<Paint>();Material sentinel=null;Texture2D original=null,marked=null;var bodyDraw=new List<string>();
        Action<ScriptableRenderContext,Camera> observe=(context,view)=>{if(view!=camera)return;string line="ACTUAL_BEGIN_CAMERA phase="+phase+" run="+run+" frame="+Time.frameCount+" camera="+PathOf(view.transform)+" position="+view.transform.position+" rect="+view.rect+" pixelRect="+view.pixelRect+" aspect="+view.aspect+" cullingMask="+view.cullingMask+" primary="+Read(framing,"Primary")+" evaluations="+Read(rig,"FrameEvaluations");draw.AppendLine(line);foreach(var renderer in renderers){string state=PathOf(renderer.transform)+" active="+renderer.gameObject.activeInHierarchy+" enabled="+renderer.enabled+" forceOff="+renderer.forceRenderingOff+" layer="+renderer.gameObject.layer+" maskIncluded="+((view.cullingMask&(1<<renderer.gameObject.layer))!=0)+" bounds="+renderer.bounds+" materials="+string.Join("|",renderer.sharedMaterials.Where(value=>value!=null).Select(value=>value.name+"/"+value.shader.name));bodyDraw.Add(state);draw.AppendLine(state);}};
        RenderPipelineManager.beginCameraRendering+=observe;
        try
        {
            camera.aspect=width/(float)height;original=Render(camera,target);File.WriteAllBytes(prefix+"-world.png",original.EncodeToPNG());var fact=Observe(phase+"-"+width+"x"+height,run,true);
            int pixels=0;
            if(witness)
            {
                var shader=Shader.Find("Universal Render Pipeline/Unlit");Assert.That(shader!=null&&shader.isSupported,Is.True);sentinel=new Material(shader);sentinel.SetColor("_BaseColor",Color.magenta);
                foreach(var renderer in renderers)
                {
                    var paint=new Paint{renderer=renderer,materials=renderer.sharedMaterials,global=new MaterialPropertyBlock()};renderer.GetPropertyBlock(paint.global);paint.slots=new MaterialPropertyBlock[paint.materials.Length];for(int slot=0;slot<paint.slots.Length;slot++){paint.slots[slot]=new MaterialPropertyBlock();renderer.GetPropertyBlock(paint.slots[slot],slot);}paints.Add(paint);
                    renderer.sharedMaterials=Enumerable.Repeat(sentinel,paint.materials.Length).ToArray();var magenta=new MaterialPropertyBlock();magenta.SetColor("_BaseColor",Color.magenta);magenta.SetColor("_Color",Color.magenta);renderer.SetPropertyBlock(magenta);for(int slot=0;slot<paint.slots.Length;slot++)renderer.SetPropertyBlock(magenta,slot);
                }
                marked=Render(camera,target);File.WriteAllBytes(prefix+"-actual-body-witness.png",marked.EncodeToPNG());var a=original.GetPixels32();var b=marked.GetPixels32();for(int i=0;i<a.Length;i++)if(Mathf.Abs(a[i].r-b[i].r)+Mathf.Abs(a[i].g-b[i].g)+Mathf.Abs(a[i].b-b[i].b)>80&&b[i].r>b[i].g*1.35f&&b[i].b>b[i].g*1.35f&&b[i].r>60&&b[i].b>60)pixels++;
            }
            File.WriteAllText(prefix+".json",JsonUtility.ToJson(new CaptureFact{scene=mode,png=prefix+"-world.png",phase=phase,run=run,width=width,height=height,matchId=(string)Read(match,"MatchId"),primary=primary,visibleBodyPixels=pixels,configuredMinimum=fact.minimumHeight,projected=fact.projected,bodyDrawState=bodyDraw.ToArray()},true));
            if(witness)Assert.That(pixels,Is.GreaterThan(64),"Actual composed tractor must contribute visible pixels through the original world. A projected box/HUD alone cannot prove visibility.");
        }
        finally
        {
            foreach(var paint in paints){paint.renderer.sharedMaterials=paint.materials;paint.renderer.SetPropertyBlock(paint.global);for(int slot=0;slot<paint.slots.Length;slot++)paint.renderer.SetPropertyBlock(paint.slots[slot],slot);}
            RenderPipelineManager.beginCameraRendering-=observe;camera.targetTexture=oldTarget;camera.aspect=oldAspect;target.Release();UnityEngine.Object.DestroyImmediate(target);if(sentinel!=null)UnityEngine.Object.DestroyImmediate(sentinel);if(original!=null)UnityEngine.Object.DestroyImmediate(original);if(marked!=null)UnityEngine.Object.DestroyImmediate(marked);
            foreach(var state in physical){Assert.That(state.body.position,Is.EqualTo(state.position));Assert.That(state.body.rotation,Is.EqualTo(state.rotation));Assert.That(state.body.linearVelocity,Is.EqualTo(state.velocity));Assert.That(state.body.angularVelocity,Is.EqualTo(state.angular));Assert.That(state.body.mass,Is.EqualTo(state.mass));}Assert.That(JournalCount,Is.EqualTo(journal));Assert.That((float)Read(match,"Clock"),Is.EqualTo(clock));Save();
        }
        // Reuse the existing isolated UI compositor, preserving the real world
        // camera and rendering the actual countdown/pause/HUD above it.
        var save=Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==5&&method.GetParameters()[0].ParameterType.IsInstanceOfType(match));save.Invoke(null,new object[]{match,width,height,prefix+"-HUD.png",null});
    }
    IEnumerator Exercise(string scene,string type)
    {
        mode=scene;output=null;riders=scene=="TruckDragonRiders";frames.Clear();draw.Clear();Assert.That(Application.isPlaying,Is.True);string path="Assets/"+scene+"/Scenes/"+scene+".unity";Assert.That(File.Exists(path),Is.True,"Explicitly author the actual saved scene; this fixture does not compose it.");
        string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");output=System.IO.Path.GetFullPath(System.IO.Path.Combine(at>=0&&at+1<args.Length?args[at+1]:Application.persistentDataPath,"BroadcastNative",scene));Directory.CreateDirectory(output);
        preferences=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"DragonBroadcast"+scene});Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);isolation=new GameObject("Dragon broadcast original-AI physical-input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime(type));Assert.That(match,Is.Not.Null);
        var setup=Read(match,"setup");setup.GetType().GetField("count").SetValue(setup,4);Call(match,"AllBots");Runtime("LWS.TruckSports.SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{false});
        var views=Read(match,"drivingViews");rig=(Component)Read(Read(views,"rally"),"broadcast");camera=(Camera)Read(rig,"view");framing=(Component)Read(rig,"broadcastFraming");Assert.That(framing,Is.Not.Null,"The saved scene must already contain the parent-owned shared broadcast binding.");
        var bodyIds=((Array)Read(match,"trucks")).Cast<object>().Select(truck=>((Rigidbody)Read(truck,"body")).GetEntityId()).ToArray();string previousMatch=null;
        for(int run=1;run<=4;run++)
        {
            match.GetType().GetField("fixedSeed").SetValue(match,run);
            Assert.That(Call(match,"StartMatch"),Is.EqualTo(true));Assert.That(Read(match,"MatchSeed"),Is.EqualTo(run));Assert.That(Read(match,"AllAI"),Is.EqualTo(true));Assert.That((string)Read(match,"MatchId"),Is.Not.EqualTo(previousMatch));previousMatch=(string)Read(match,"MatchId");yield return null;yield return null;
            var seen=new HashSet<int>();float deadline=Time.realtimeSinceStartup+35;
            while(Countdown>0&&Time.realtimeSinceStartup<deadline)
            {
                int number=Mathf.CeilToInt(Countdown);if(number>=1&&number<=3&&seen.Add(number)){Observe("actual-countdown-"+number,run,true);if(run==1)Capture("countdown-"+number,run,1280,720,false);}yield return null;
            }
            Observe("actual-GO-prerequisite",run,false);Assert.That(Countdown,Is.Zero);Assert.That(seen,Is.EquivalentTo(new[]{1,2,3}),"Observe the original complete3-2-1; never fast-forward or replace its countdown.");
            float until=Time.time+1;while(Time.time<until){yield return null;Observe("normal-live-after-GO",run,true);}Assert.That((float)Read(match,"Clock"),Is.GreaterThan(0));
            Capture("GO",run,1280,720,true);Capture("GO",run,1920,1080,true);
            if(run==1)
            {
                Call(match,"Pause",0,false);yield return null;var frozen=((Array)Read(match,"trucks")).Cast<object>().Select(truck=>{var body=(Rigidbody)Read(truck,"body");return (body,position:body.position,rotation:body.rotation);}).ToArray();int score=JournalCount;float clock=(float)Read(match,"Clock");
                for(int frame=0;frame<6;frame++){yield return null;Observe("ordinary-pause",run,true);}Capture("paused",run,1280,720,true);Capture("paused",run,1920,1080,true);
                foreach(var state in frozen){Assert.That(state.body.position,Is.EqualTo(state.position));Assert.That(state.body.rotation,Is.EqualTo(state.rotation));}Assert.That(JournalCount,Is.EqualTo(score));Assert.That((float)Read(match,"Clock"),Is.EqualTo(clock));Call(match,"Resume");yield return null;yield return null;
            }
            Assert.That(((Array)Read(match,"trucks")).Cast<object>().Select(truck=>((Rigidbody)Read(truck,"body")).GetEntityId()).ToArray(),Is.EqualTo(bodyIds));Call(match,"SetupScreen");yield return null;
        }
    }
    [UnityTest,Timeout(240000)] public IEnumerator HeistActualAiBroadcastFramesItsRealCrewsThroughCountdownPauseAndRematch(){yield return Exercise("TruckHoardHeist","LWS.TruckHoardHeist.HoardHeistMatch");}
    [UnityTest,Timeout(240000)] public IEnumerator EggActualAiBroadcastFramesItsRealCrewsThroughCountdownPauseAndRematch(){yield return Exercise("TruckDragonEggDelivery","LWS.TruckDragonEggDelivery.DragonEggDeliveryMatch");}
    [UnityTest,Timeout(240000)] public IEnumerator EscapeActualAiBroadcastFramesItsRealCrewsThroughCountdownPauseAndRematch(){yield return Exercise("TruckDragonEscape","LWS.TruckDragonEscape.DragonEscapeMatch");}
    [UnityTest,Timeout(240000)] public IEnumerator RidersActualAiBroadcastFramesItsRealCrewsThroughCountdownPauseAndRematch(){yield return Exercise("TruckDragonRiders","LWS.TruckDragonRiders.DragonRidersMatch");}
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if(match!=null)Call(match,"SetupScreen");Save();Time.timeScale=1;var current=SceneManager.GetActiveScene();var empty=SceneManager.CreateScene("Dragon broadcast cleanup "+Guid.NewGuid().ToString("N"));SceneManager.SetActiveScene(empty);if(current.IsValid()&&current.isLoaded)yield return SceneManager.UnloadSceneAsync(current);match=rig=framing=null;camera=null;
        if(isolation!=null)UnityEngine.Object.DestroyImmediate(isolation);preferences?.Dispose();preferences=null;Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
    }
}
#endif
