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

// Real saved rig/controller and native fixed-step motion. No Animator.Play,
// SampleAnimation, State_Force, velocity assignment or callback injection.
public sealed class DragonAnimalMotionNativeTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    GameObject model,floor,lamp,cameraOwner;
    Component motor;
    object species;
    Animator animator;
    Rigidbody body;
    Camera camera;
    Type actionType;
    Component character;
    GameObject audioSettingsOwner;
    bool listenerCaptured;
    float previousListener;
    readonly List<GameObject> physicalAimTargets=new List<GameObject>();
    readonly List<int> physicalAimHits=new List<int>();
    Component[] aimTrucks=Array.Empty<Component>();
    EventInfo aimHitEvent;
    Delegate aimHitHandler;
    bool aimActualClip;
    float aimContactNormalized,aimAngle,aimRange;
    string aimElement;
    readonly List<KeyValuePair<EventInfo,Delegate>> subscriptions=new List<KeyValuePair<EventInfo,Delegate>>();
    readonly List<UnityEngine.Object> transientRiderCabObjects=new List<UnityEngine.Object>();
    readonly StringBuilder report=new StringBuilder();
    int starts,contacts,finishes;
    float contactTime,startAt,contactAt;
    bool actualModeAtContact,actualBreathClipAtContact;
    string id,output;
    sealed class ActionObservation
    {
        public string name;public AnimationClip clip;public bool continuous,correct=true,completed;public int starts,contacts,finishes;public float maxNormalized,variation,startAt,contactAt;
    }
    ActionObservation observation;
    Quaternion[] restingGrip;
    static object Read(object value,string name)=>value.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(value):value.GetType().GetProperty(name,Any).GetValue(value);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(value,args);
    object Action(string name)=>Enum.Parse(actionType,name);
    void Log(string value){report.AppendLine(value);TestContext.WriteLine(value);}
    void Construct(string speciesId,Vector3 spawn,bool asActor=false,string ownedOverridePath=null)
    {
        Assert.That(Application.isPlaying,Is.True);id=speciesId;report.Clear();starts=contacts=finishes=0;observation=null;restingGrip=null;character=null;audioSettingsOwner=null;listenerCaptured=false;physicalAimHits.Clear();aimTrucks=Array.Empty<Component>();aimHitEvent=null;aimHitHandler=null;aimActualClip=false;
        string[] args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-tsDragonOutput");
        output=Path.GetFullPath(Path.Combine(index>=0&&index+1<args.Length?args[index+1]:Path.Combine(Application.persistentDataPath,"DragonNativeMotion"),"IsolatedMotion",id));Directory.CreateDirectory(output);
        if(ownedOverridePath!=null){output=Path.Combine(output,"RidersMountedOverride");Directory.CreateDirectory(output);}
        var catalog=AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset");Assert.That(catalog,Is.Not.Null);
        var lookup=new object[]{id,null};Assert.That((bool)Call(catalog,"TryGet",lookup),Is.True);species=lookup[1];
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckDragonKingdom/Prefabs/Dragons/"+id+"/"+id+" Dragon "+(asActor?"Actor":"Animal")+".prefab");Assert.That(prefab,Is.Not.Null,"Consume the actual configured saved animal.");
        floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Isolated real physical support";floor.transform.position=new Vector3(0,-1,0);floor.transform.localScale=new Vector3(1600,2,1600);
        model=UnityEngine.Object.Instantiate(prefab,spawn,Quaternion.identity);var type=Type.GetType("LWS.TruckDragonKingdom.DragonAnimalMotor, Assembly-CSharp",true);motor=model.GetComponentInChildren(type);
        Assert.That(motor,Is.Not.Null);animator=(Animator)Read(motor,"animator");body=(Rigidbody)Read(motor,"body");actionType=type.Assembly.GetType("LWS.TruckDragonKingdom.DragonAnimationAction",true);
        if(ownedOverridePath!=null)
        {
            var actualOverride=AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ownedOverridePath);Assert.That(actualOverride,Is.Not.Null,"Explicitly author and retain the selected actual Riders action assets first; this native fixture never generates them.");
            Assert.That(actualOverride.runtimeAnimatorController,Is.SameAs(animator.runtimeAnimatorController),"Select the saved compatible override on the actual original configured Actor instance.");animator.runtimeAnimatorController=actualOverride;
            var sourceBreath=(AnimationClip)Read(Mapping("Breath"),"clip");Assert.That(actualOverride[sourceBreath],Is.Not.SameAs(sourceBreath));Assert.That(AssetDatabase.GetAssetPath(actualOverride[sourceBreath]),Is.EqualTo("Assets/TruckDragonRiders/Animations/"+speciesId+"/Mounted Breath.anim"));
        }
        if(asActor)
        {
            character=model.GetComponent(type.Assembly.GetType("LWS.TruckDragonKingdom.DragonCharacter",true));Assert.That(character,Is.Not.Null);
            Assert.That(Read(character,"motor"),Is.SameAs(motor),"The saved Actor must reference its own actual configured child motor.");
            var truckType=type.Assembly.GetType("LWS.TruckSports.SportsEventTruck",true);Call(character,"Prepare",species,Array.CreateInstance(truckType,0),0,917);
        }
        else Call(motor,"Prepare",species);
        Assert.That(animator.avatar,Is.Not.Null);Assert.That(animator.avatar.isValid&&!animator.avatar.isHuman,Is.True);
        lamp=new GameObject("Isolated native dragon daylight");var sun=lamp.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=2.5f;sun.shadows=LightShadows.Soft;lamp.transform.rotation=Quaternion.Euler(40,-35,0);
        cameraOwner=new GameObject("Controlled native dragon evidence camera");camera=cameraOwner.AddComponent<Camera>();camera.enabled=false;camera.backgroundColor=new Color(.07f,.11f,.17f);camera.clearFlags=CameraClearFlags.SolidColor;camera.fieldOfView=48;camera.nearClipPlane=.1f;camera.farClipPlane=2000;camera.aspect=16f/9;
        if(!UnityEngine.Object.FindObjectsByType<AudioListener>().Any(listener=>listener.isActiveAndEnabled))cameraOwner.AddComponent<AudioListener>();
        Log("ISOLATED_NATIVE_DRAGON "+id+" prefab="+AssetDatabase.GetAssetPath(prefab)+" avatar="+AssetDatabase.GetAssetPath(animator.avatar)+" controller="+AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)+" fixedDelta="+Time.fixedDeltaTime+" timeScale="+Time.timeScale+". Controlled native Camera.Render captures; no Windows/human/performance acceptance.");
    }
    IEnumerator Warmup()
    {
        for(int i=0;i<30;i++){Call(motor,"StopMotion");yield return new WaitForFixedUpdate();}yield return null;
        Log(Pose("warmed"));
    }
    string Pose(string phase)
    {
        var animal=Read(motor,"animal");var state=Read(animal,"ActiveStateID") as UnityEngine.Object;var info=animator.GetCurrentAnimatorStateInfo(0);
        var fly=((IEnumerable)Read(animal,"states")).Cast<object>().FirstOrDefault(value=>value.GetType().Name=="Fly");
        string input=fly==null?"none":"input="+Read(fly,"InputValue")+",canActivate="+Read(fly,"CanBeActivated")+",sleep="+Read(fly,"IsSleep")+",stateSleep="+Read(fly,"IsSleepFromState")+",modeSleep="+Read(fly,"IsSleepFromMode")+",stanceSleep="+Read(fly,"IsSleepFromStance")+",includeSleep="+Read(fly,"IncludeSleepState")+",excludedStates=["+string.Join(",",((IEnumerable)Read(fly,"SleepFromState")).Cast<UnityEngine.Object>().Select(value=>value==null?"null":value.name))+"] active="+Read(fly,"IsActiveState");
        return phase+" body="+body.position.ToString("F4")+" velocity="+body.linearVelocity.ToString("F4")+" motor="+motor.transform.position.ToString("F4")+" state="+(state==null?"none":state.name)+" grounded="+Read(motor,"IsGrounded")+" flying="+Read(motor,"IsFlying")+" flyState={"+input+"} animation="+info.fullPathHash+" tag="+info.tagHash+" normalized="+info.normalizedTime.ToString("F4")+" clips="+string.Join(",",animator.GetCurrentAnimatorClipInfo(0).Select(clip=>clip.clip.name));
    }
    string Layers()
    {
        var values=new List<string>();
        for(int layer=0;layer<animator.layerCount;layer++)
        {
            var state=animator.GetCurrentAnimatorStateInfo(layer);var next=animator.GetNextAnimatorStateInfo(layer);
            Func<AnimatorClipInfo,string> clip=info=>AssetDatabase.GetAssetPath(info.clip)+"/"+info.clip.name+"@"+info.weight.ToString("F5");
            values.Add("layer="+layer+":"+animator.GetLayerName(layer)+" weight="+animator.GetLayerWeight(layer)+" transition="+animator.IsInTransition(layer)+" currentState="+state.fullPathHash+" currentTag="+state.tagHash+" currentTime="+state.normalizedTime.ToString("F5")+" currentClips=["+string.Join(";",animator.GetCurrentAnimatorClipInfo(layer).Select(clip))+"] nextState="+next.fullPathHash+" nextTag="+next.tagHash+" nextTime="+next.normalizedTime.ToString("F5")+" nextClips=["+string.Join(";",animator.GetNextAnimatorClipInfo(layer).Select(clip))+"]");
        }
        var mode=Read(Read(motor,"animal"),"ActiveMode");return "MODE_LAYER_EVIDENCE vendorLayer="+(mode==null?"none":Read(mode,"LayerIndex"))+" Mode="+animator.GetInteger("Mode")+" ModeStatus="+animator.GetInteger("ModeStatus")+" "+string.Join(" | ",values);
    }
    Transform[] Bones(string contains)
    {
        var values=model.GetComponentsInChildren<Transform>(true).Where(value=>value.name.IndexOf(contains,StringComparison.OrdinalIgnoreCase)>=0&&!value.name.EndsWith("Gizmo",StringComparison.Ordinal)).ToArray();Assert.That(values.Length,Is.GreaterThan(0),"Actual rig bones required: "+contains);return values;
    }
    static float Variation(Transform[] bones,Quaternion[] initial)=>bones.Select((bone,index)=>Quaternion.Angle(initial[index],bone.localRotation)).Max();
    struct SubjectPixels { public int count,minX,minY,maxX,maxY; public int Width=>maxX-minX+1; public int Height=>maxY-minY+1; public override string ToString()=>"pixels="+count+" rect="+minX+","+minY+","+Width+","+Height; }
    static SubjectPixels InspectSubject(Texture2D texture)
    {
        var pixels=texture.GetPixels32();var background=pixels[0];var result=new SubjectPixels{minX=texture.width,minY=texture.height,maxX=-1,maxY=-1};
        for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)
        {
            var pixel=pixels[y*texture.width+x];if(Mathf.Abs(pixel.r-background.r)+Mathf.Abs(pixel.g-background.g)+Mathf.Abs(pixel.b-background.b)<=24)continue;
            result.count++;result.minX=Mathf.Min(result.minX,x);result.maxX=Mathf.Max(result.maxX,x);result.minY=Mathf.Min(result.minY,y);result.maxY=Mathf.Max(result.maxY,y);
        }
        return result;
    }
    void RenderInto(RenderTexture rt,Texture2D texture)
    {camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();}
    void Capture(string label)
    {
        var renderers=model.GetComponentsInChildren<SkinnedMeshRenderer>().Where(value=>value.enabled&&value.sharedMesh!=null).ToArray();Assert.That(renderers.Length,Is.GreaterThan(0));
        // Bake without renderer scale, then apply the complete local-to-world
        // transform ONCE, including inherited parent scale. Calculate from
        // actual vertices, not the baked mesh's copied/stale culling bounds.
        var bounds=new Bounds();bool foundBounds=false;var baked=new Mesh();var vertices=new List<Vector3>();
        var envelope=renderers[0].bounds;foreach(var renderer in renderers)envelope.Encapsulate(renderer.bounds);
        try
        {
            foreach(var renderer in renderers)
            {
                baked.Clear();renderer.BakeMesh(baked,false);vertices.Clear();baked.GetVertices(vertices);
                foreach(var vertex in vertices)
                {
                    Vector3 point=renderer.transform.TransformPoint(vertex);
                    if(!foundBounds){bounds=new Bounds(point,Vector3.zero);foundBounds=true;}else bounds.Encapsulate(point);
                }
                Log("BAKED_POSE "+label+" renderer="+renderer.name+" useScale=false localScale="+renderer.transform.localScale+" parentLossyScale="+(renderer.transform.parent==null?Vector3.one:renderer.transform.parent.lossyScale)+" lossyScale="+renderer.transform.lossyScale+" bakedMetadata="+baked.bounds+" renderedEnvelope="+renderer.bounds+" vertexCount="+vertices.Count);
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(baked);}
        Assert.That(foundBounds,Is.True);
        // Always start with the genuine world renderer envelope. Being INSIDE
        // that envelope does not prove a baked imported basis has correct scale:
        // V38's understated baked bounds clipped the5xadult before any full fit.
        // The final proof comes from actual rendered dragon-only pixels.
        var looseEnvelope=envelope;looseEnvelope.Expand(2);
        bool sane=looseEnvelope.Contains(bounds.min)&&looseEnvelope.Contains(bounds.max);
        var framing=envelope;Log("POSE_BOUNDS "+label+" bakedWorld="+bounds+" envelope="+envelope+" bakedInsideEnvelope="+sane+" safeInitialLens=rendererEnvelope");
        float distance=Mathf.Max(3,framing.extents.magnitude)*2.9f;Vector3 target=framing.center,direction=new Vector3(1,.45f,1).normalized;
        var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);var prior=RenderTexture.active;var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
        var outside=UnityEngine.Object.FindObjectsByType<Renderer>().Where(renderer=>!renderer.transform.IsChildOf(model.transform)).ToArray();var priorHidden=outside.Select(renderer=>renderer.forceRenderingOff).ToArray();
        try
        {
            foreach(var renderer in outside)renderer.forceRenderingOff=true;
            SubjectPixels subject=default;
            for(int attempt=0;attempt<16;attempt++)
            {
                camera.transform.SetPositionAndRotation(target+direction*distance,Quaternion.LookRotation(-direction));RenderInto(rt,texture);subject=InspectSubject(texture);
                Log("SUBJECT_CAMERA_FIT "+label+" attempt="+attempt+" "+subject+" distance="+distance+" target="+target);
                Assert.That(subject.count,Is.GreaterThan(20),"No actual dragon-only pixels; floor and every other renderer are excluded.");
                // A clipped silhouette has an untrustworthy center and extent.
                // Expand first and measure the COMPLETE subject before fitting;
                // never aim/zoom using the visible fraction of a clipped wing.
                bool border=subject.minX<=12||subject.minY<=12||subject.maxX>=rt.width-13||subject.maxY>=rt.height-13;
                if(border){distance*=2;continue;}
                float centerX=(subject.minX+subject.maxX+1)*.5f/rt.width-.5f,centerY=(subject.minY+subject.maxY+1)*.5f/rt.height-.5f;
                float fit=Mathf.Max(subject.Width/(rt.width*.72f),subject.Height/(rt.height*.76f));
                if(Mathf.Abs(centerX)<.015f&&Mathf.Abs(centerY)<.015f&&fit>.95f&&fit<1.05f)break;
                if(attempt==15)break;
                float halfHeight=distance*Mathf.Tan(camera.fieldOfView*.5f*Mathf.Deg2Rad);
                target+=(camera.transform.right*(centerX*halfHeight*2*camera.aspect)+camera.transform.up*(centerY*halfHeight*2))*.75f;
                distance=Mathf.Max(2,distance*Mathf.Clamp(fit,.8f,1.25f));
            }
            string subjectPath=Path.Combine(output,label+"-subject-only.png");File.WriteAllBytes(subjectPath,texture.EncodeToPNG());
            Assert.That(subject.count,Is.GreaterThan(12000),"Actual dragon pixel coverage must exclude the floor/background.");
            Assert.That(subject.Width,Is.GreaterThanOrEqualTo(240));Assert.That(subject.Height,Is.GreaterThanOrEqualTo(240));
            Assert.That(Mathf.Max(subject.Width/(float)rt.width,subject.Height/(float)rt.height),Is.GreaterThanOrEqualTo(.5f),"The actual subject must be large enough to inspect.");
            Assert.That(subject.minX,Is.GreaterThan(12));Assert.That(subject.minY,Is.GreaterThan(12));Assert.That(subject.maxX,Is.LessThan(rt.width-13));Assert.That(subject.maxY,Is.LessThan(rt.height-13));
            for(int i=0;i<outside.Length;i++)if(outside[i]!=null)outside[i].forceRenderingOff=priorHidden[i];
            RenderInto(rt,texture);
            string path=Path.Combine(output,label+".png");File.WriteAllBytes(path,texture.EncodeToPNG());
            Log("CAPTURE "+path+" subjectOnly="+subjectPath+" "+subject+" "+Pose(label));
        }
        finally{for(int i=0;i<outside.Length;i++)if(outside[i]!=null)outside[i].forceRenderingOff=priorHidden[i];camera.targetTexture=null;RenderTexture.active=prior;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
    }
    [UnityTest,Timeout(60000)] public IEnumerator InfernoGroundWalkAndPauseUseActualVendorMovementAndRigMotion()
    {
        Construct("dragon-inferno",Vector3.up);yield return Warmup();Capture("ground-idle");
        Vector3 start=body.position;var legs=Bones("Leg");var rotations=legs.Select(bone=>bone.localRotation).ToArray();float variation=0;bool groundCore=false;
        for(int step=0;step<180;step++)
        {
            Call(motor,"MoveWorld",Vector3.forward,false,false);yield return new WaitForFixedUpdate();
            variation=Mathf.Max(variation,Variation(legs,rotations));groundCore|=(bool)Read(motor,"IsGrounded");
            if(step==60){yield return null;Capture("ground-walk");}
        }
        yield return null;Capture("ground-travelled");float travel=Vector3.ProjectOnPlane(body.position-start,Vector3.up).magnitude;
        Log("GROUND_RESULT travel="+travel+" legRotationDegrees="+variation+" "+Pose("walk-end"));Assert.That(groundCore,Is.True);Assert.That(travel,Is.GreaterThan(3));Assert.That(variation,Is.GreaterThan(1),"Actual walk bones must animate, beyond root translation.");
        Call(motor,"Pause",true);Vector3 paused=body.position;float animation=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        for(int step=0;step<15;step++){Call(motor,"MoveWorld",Vector3.forward,true,true);yield return new WaitForFixedUpdate();}
        Assert.That(Vector3.Distance(body.position,paused),Is.LessThan(.001f));Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime,Is.EqualTo(animation).Within(.0001f));Call(motor,"Pause",false);
    }
    [UnityTest,Timeout(60000)] public IEnumerator InfernoActualTakeoffThreeDimensionalFlightAndWingMotion(){yield return Flight("dragon-inferno",Vector3.up);}
    [UnityTest,Timeout(60000)] public IEnumerator FireActualThreeDimensionalFlightAndWingMotion(){yield return Flight("dragon-fire",Vector3.up*35);}
    IEnumerator Flight(string speciesId,Vector3 spawn)
    {
        Construct(speciesId,spawn);yield return Warmup();Capture("flight-before-command");Vector3 start=body.position;
        var wings=Bones("Wing");var rotations=wings.Select(bone=>bone.localRotation).ToArray();float variation=0;bool actualFly=false,maintainedFlight=true;float maxHeight=body.position.y;
        for(int step=0;step<240;step++)
        {
            Call(motor,"MoveWorld",new Vector3(.15f,.4f,1).normalized,false,true);yield return new WaitForFixedUpdate();
            bool flyingNow=(bool)Read(motor,"IsFlying");if(actualFly&&!flyingNow)maintainedFlight=false;actualFly|=flyingNow;maxHeight=Mathf.Max(maxHeight,body.position.y);variation=Mathf.Max(variation,Variation(wings,rotations));
            if(step==0||step==5||step==15||step==30||step==60)Log(Pose("real-flight-command-step-"+step));
            if(step==90||step==130){yield return null;Capture("flight-flap-"+step);}
        }
        yield return null;Capture("flight-climbed");float horizontal=Vector3.ProjectOnPlane(body.position-start,Vector3.up).magnitude;
        Log("FLIGHT_RESULT start="+start+" maxHeight="+maxHeight+" horizontalTravel="+horizontal+" wingRotationDegrees="+variation+" "+Pose("flight-end"));
        Assert.That(actualFly,Is.True,"Only real vendor Fly core animation establishes flight.");Assert.That(maintainedFlight,Is.True,"A commanded sustained flight cannot silently become Fall after briefly entering Fly.");Assert.That(Read(motor,"IsFlying"),Is.EqualTo(true));Assert.That(horizontal,Is.GreaterThan(5));Assert.That(maxHeight-start.y,Is.GreaterThan(1));Assert.That(variation,Is.GreaterThan(1),"Actual wing bones must flap/bank.");
        Vector3 apex=body.position;for(int step=0;step<150;step++){Call(motor,"MoveWorld",new Vector3(.25f,-.5f,1).normalized,false,true);yield return new WaitForFixedUpdate();maintainedFlight&=(bool)Read(motor,"IsFlying");}
        yield return null;Capture("flight-descending");Log("DESCENT_RESULT displacement="+(body.position-apex).ToString("F4")+" maintainedActualFly="+maintainedFlight);Assert.That(maintainedFlight,Is.True,"Gravity-only Fall does not prove commanded three-dimensional flight descent.");Assert.That(body.position.y,Is.LessThan(apex.y-.5f),"A real three-dimensional descent must follow the vendor flight command.");
    }
    void Subscribe(string name,string method)
    {
        var info=motor.GetType().GetEvent(name);var callback=GetType().GetMethod(method,Any).MakeGenericMethod(actionType);var handler=Delegate.CreateDelegate(info.EventHandlerType,this,callback);info.AddEventHandler(motor,handler);subscriptions.Add(new KeyValuePair<EventInfo,Delegate>(info,handler));
    }
    void Started<T>(T action){if(action.ToString()!="Breath")return;starts++;startAt=Time.time;Log("ACTUAL_ACTION_STARTED "+Pose("breath-start")+" "+Layers());}
    void Contact<T>(T action)
    {
        if(action.ToString()!="Breath")return;contacts++;contactTime=(float)Read(motor,"ActionNormalizedTime");contactAt=Time.time;
        var mode=Read(Read(motor,"animal"),"ActiveMode");actualModeAtContact=mode!=null&&(bool)Read(mode,"PlayingMode");
        var actual=EffectiveMappedClip("Breath");
        actualBreathClipAtContact=animator.GetCurrentAnimatorClipInfo(1).Any(info=>info.clip==actual&&info.weight>0);
        Log("ACTUAL_ACTION_CONTACT normalized="+contactTime+" realMode="+actualModeAtContact+" actualClip="+actualBreathClipAtContact+" expectedClip="+AssetDatabase.GetAssetPath(actual)+"/"+actual.name+" deltaSinceStart="+(contactAt-startAt)+" state="+animator.GetCurrentAnimatorStateInfo(1).fullPathHash+" "+Layers());
    }
    void Finished<T>(T action){if(action.ToString()!="Breath")return;finishes++;Log("ACTUAL_ACTION_FINISHED lastNormalized="+Read(motor,"LastActionNormalizedTime")+" completed="+Read(motor,"LastActionCompleted"));}
    [UnityTest,Timeout(60000)] public IEnumerator InfernoBreathContactComesFromActualObservedClipPlayback(){yield return Breath("dragon-inferno",Vector3.up);}
    [UnityTest,Timeout(60000)] public IEnumerator FireBreathContactComesFromActualObservedClipPlayback(){yield return Breath("dragon-fire",Vector3.up*35);}
    IEnumerator Breath(string speciesId,Vector3 spawn)
    {
        Construct(speciesId,spawn);yield return Warmup();Subscribe("ActionStarted",nameof(Started));Subscribe("ActionContact",nameof(Contact));Subscribe("ActionFinished",nameof(Finished));Capture("breath-before");
        bool accepted=false,captured=false;float deadline=Time.time+12;
        while(Time.time<deadline&&finishes==0)
        {
            if(!accepted)accepted=(bool)Call(motor,"TryAction",Action("Breath"),1f);
            Call(motor,"StopMotion");yield return new WaitForFixedUpdate();yield return null;
            if(contacts>0&&!captured){Capture("breath-real-contact");captured=true;}
        }
        Capture("breath-after");Assert.That(accepted,Is.True,"The real vendor mode must accept this supported action.");Assert.That(starts,Is.EqualTo(1));Assert.That(contacts,Is.EqualTo(1));Assert.That(finishes,Is.EqualTo(1));
        Assert.That(actualModeAtContact&&actualBreathClipAtContact,Is.True);Assert.That(contactTime,Is.InRange(.45f,1.15f));Assert.That(contactAt,Is.GreaterThan(startAt));Assert.That(Read(motor,"LastActionCompleted"),Is.EqualTo(true));
        if(id=="dragon-fire")Assert.That(Call(motor,"TryAction",Action("Sleep"),1f),Is.EqualTo(false),"Unsupported Fire Sleep must decline rather than play idle.");
    }
    object Mapping(string name)=>((IEnumerable)Read(species,"animations")).Cast<object>().Single(value=>Read(value,"action").ToString()==name);
    AnimationClip EffectiveMappedClip(string name)
    {
        var original=(AnimationClip)Read(Mapping(name),"clip");
        if(animator.runtimeAnimatorController is AnimatorOverrideController controller)
        {var entries=new List<KeyValuePair<AnimationClip,AnimationClip>>();controller.GetOverrides(entries);foreach(var pair in entries)if(pair.Key==original)return pair.Value!=null?pair.Value:original;}
        return original;
    }
    bool ExactPlayback(string name,int layer,bool actionTag,out AnimatorStateInfo info)
    {
        var clip=EffectiveMappedClip(name);info=animator.GetCurrentAnimatorStateInfo(layer);
        if((!actionTag||info.IsTag("DragonAction_"+name))&&animator.GetCurrentAnimatorClipInfo(layer).Any(value=>value.clip==clip&&value.weight>.0001f))return true;
        if(animator.IsInTransition(layer))
        {
            info=animator.GetNextAnimatorStateInfo(layer);
            if((!actionTag||info.IsTag("DragonAction_"+name))&&animator.GetNextAnimatorClipInfo(layer).Any(value=>value.clip==clip&&value.weight>.0001f))return true;
        }
        info=default;return false;
    }
    void ObserveActions(){Subscribe("ActionStarted",nameof(CoverageStarted));Subscribe("ActionContact",nameof(CoverageContact));Subscribe("ActionFinished",nameof(CoverageFinished));}
    void StagedRigObservation(string stage,Transform[] bones)
    {
        Log("ACTUAL_STAGED_ANIMATION_OBSERVATION "+stage+" animatorEnabled="+animator.enabled+" animatorSpeed="+animator.speed+" motorPaused="+Read(motor,"Paused")+" "+Pose(stage)+" "+Layers());
        foreach(var bone in bones)Log("ACTUAL_STAGED_BONE "+stage+" "+bone.name+" localPosition="+bone.localPosition.ToString("F6")+" localEuler="+bone.localEulerAngles.ToString("F5")+" world="+bone.position.ToString("F5"));
    }
    [UnityTest,Timeout(60000)]public IEnumerator FireActualPreparedPausedAndFirstNormalAnimatorEvaluationRetainsRigPoseEvidence()
    {
        Construct("dragon-fire",Vector3.up*35,true);var bones=RelevantBones("Spine","Ribcage","Neck");var before=bones.Select(value=>value.localRotation).ToArray();
        Log("DIAGNOSTIC_ONLY_NORMAL_PREPARE_PAUSE_AND_ENGINE_EVALUATION: no saved Rider scene/version override, mount/score/contact injection, Animator.Play/Sample/Update or StateForce. Not mounted acceptance.");
        StagedRigObservation("prepared-before-first-normal-evaluation",bones);Capture("staged-prepared-before-evaluation");
        Call(character,"Pause",true);StagedRigObservation("ordinary-paused-staging",bones);
        for(int i=0;i<3;i++)yield return new WaitForFixedUpdate();yield return null;
        StagedRigObservation("after-real-paused-frames",bones);Capture("staged-paused-frames");Assert.That(animator.enabled,Is.False);
        Call(character,"Pause",false);Call(motor,"MoveWorld",Vector3.zero,false,true);
        yield return new WaitForFixedUpdate();StagedRigObservation("first-normal-fixed-boundary",bones);yield return null;
        StagedRigObservation("first-normal-render-boundary",bones);Capture("staged-first-normal-evaluated");
        for(int i=0;i<6;i++){Call(motor,"MoveWorld",Vector3.zero,false,true);yield return new WaitForFixedUpdate();}yield return null;
        StagedRigObservation("six-normal-vendor-flight-input-frames",bones);Capture("staged-normal-vendor-flight");
        float variation=0;for(int i=0;i<bones.Length;i++)variation=Mathf.Max(variation,Quaternion.Angle(before[i],bones[i].localRotation));
        Log("ACTUAL_STAGED_RIG_INITIAL_TO_NORMAL_BONE_DELTA="+variation+". A nonzero result identifies actual startup pose change; no mounted result is inferred.");
        var mapped=((Array)Read(species,"animations")).Cast<object>().Select(value=>(AnimationClip)Read(value,"clip")).Where(value=>value!=null).ToArray();
        Assert.That(animator.enabled,Is.True);Assert.That(animator.GetCurrentAnimatorClipInfo(0).Any(value=>mapped.Contains(value.clip)&&value.weight>.0001f)||animator.GetNextAnimatorClipInfo(0).Any(value=>mapped.Contains(value.clip)&&value.weight>.0001f),Is.True,"Observe an actual catalog clip through the normal engine graph.");
    }
    int groundGrabContacts;float groundGrabPalmHeight;bool groundGrabActualClip;
    void GroundGrabContact<T>(T action)
    {
        if(action.ToString()!="GroundGrab")return;
        groundGrabContacts++;groundGrabActualClip=ExactPlayback("GroundGrab",1,true,out var info)&&info.normalizedTime>=.45f;
        groundGrabPalmHeight=((Transform)Read(character,"leftGrip")).position.y-floor.GetComponent<Collider>().bounds.max.y;
        Log("ACTUAL_GROUNDED_GRAB_CONTACT normalized="+info.normalizedTime+" actualWeightedClip="+groundGrabActualClip+" palmHeight="+groundGrabPalmHeight+" grounded="+Read(motor,"IsGrounded")+" body="+body.position+" "+Layers());
    }
    [UnityTest,Timeout(60000)]public IEnumerator InfernoGroundGrabReachesGroundWithActualMappedContactAndStationaryMotorRoot()
    {
        Construct("dragon-inferno",Vector3.up,true);yield return Warmup();Vector3 start=body.position;groundGrabContacts=0;groundGrabActualClip=false;groundGrabPalmHeight=float.MaxValue;
        ObserveActions();Subscribe("ActionContact",nameof(GroundGrabContact));yield return PlayObservedAction("GroundGrab","Arm","Spine","Leg","Neck");
        Assert.That(groundGrabContacts,Is.EqualTo(1));Assert.That(groundGrabActualClip,Is.True);Assert.That(groundGrabPalmHeight,Is.InRange(.2f,1.5f),"The real mapped grounded reach must physically approach an actual ground sheep's height, rather than request the airborne carry pose.");
        Assert.That(Vector3.Distance(body.position,start),Is.LessThan(.05f),"Articulate actual bones without moving the vendor motor/root to the prey.");Assert.That(Read(motor,"IsGrounded"),Is.EqualTo(true));
    }
    void CoverageStarted<T>(T action)
    {
        if(observation==null||action.ToString()!=observation.name)return;
        observation.starts++;observation.startAt=Time.time;observation.correct&=ExactPlayback(observation.name,1,true,out _);Log("COVERAGE_STARTED "+observation.name+" "+Layers());
    }
    void CoverageContact<T>(T action)
    {
        if(observation==null||action.ToString()!=observation.name)return;
        observation.contacts++;observation.contactAt=Time.time;bool actual=ExactPlayback(observation.name,1,true,out var info);
        observation.correct&=actual&&info.normalizedTime>=.45f&&Mathf.Abs(info.normalizedTime-(float)Read(motor,"ActionNormalizedTime"))<.025f;
        Log("COVERAGE_CONTACT "+observation.name+" actualWeightedMappedClip="+actual+" actualNormalized="+info.normalizedTime+" motorNormalized="+Read(motor,"ActionNormalizedTime")+" "+Layers());
    }
    void CoverageFinished<T>(T action)
    {
        if(observation==null||action.ToString()!=observation.name)return;
        observation.finishes++;observation.completed=(bool)Read(motor,"LastActionCompleted");observation.maxNormalized=Mathf.Max(observation.maxNormalized,(float)Read(motor,"LastActionNormalizedTime"));
        Log("COVERAGE_FINISHED "+observation.name+" observedNormalized="+Read(motor,"LastActionNormalizedTime")+" completed="+observation.completed);
    }
    Transform[] RelevantBones(params string[] pieces)
    {
        var bones=model.GetComponentsInChildren<Transform>(true).Where(value=>!value.name.EndsWith("Gizmo",StringComparison.Ordinal)&&pieces.Any(piece=>value.name.IndexOf(piece,StringComparison.OrdinalIgnoreCase)>=0)).ToArray();
        Assert.That(bones.Length,Is.GreaterThan(0));return bones;
    }
    IEnumerator PlayObservedAction(string name,params string[] relevant)
    {
        var mapping=Mapping(name);var clip=(AnimationClip)Read(mapping,"clip");bool continuous=(bool)Read(mapping,"loop");
        Assert.That(clip,Is.Not.Null);Assert.That(clip,Is.Not.SameAs(Read(Mapping("GroundIdle"),"clip")),"An action cannot alias the idle clip.");
        var bones=RelevantBones(relevant);var initial=bones.Select(bone=>bone.localRotation).ToArray();
        observation=new ActionObservation{name=name,clip=clip,continuous=continuous};
        Log("ACTION_SOURCE "+id+" action="+name+" clip="+AssetDatabase.GetAssetPath(clip)+"/"+clip.name+" seconds="+clip.length+" playback="+Read(mapping,"playbackSpeed")+" loop="+continuous+" source="+Read(mapping,"sourceNote")+" bones="+string.Join(",",bones.Select(bone=>bone.name)));
        var curves=AnimationUtility.GetCurveBindings(clip);Log("ACTION_CURVES "+name+" count="+curves.Length+" relevant="+string.Join(";",curves.Where(curve=>bones.Any(bone=>curve.path.EndsWith("/"+bone.name,StringComparison.Ordinal)||curve.path==bone.name)).Select(curve=>curve.path+":"+curve.propertyName).Distinct().Take(24)));
        Capture("action-"+name+"-before");bool accepted=false,captured=false;float deadline=Time.time+Mathf.Max(12,clip.length*3);
        while(Time.time<deadline&&observation.finishes==0)
        {
            if(!accepted)accepted=(bool)Call(motor,"TryAction",Action(name),.5f);
            Call(motor,"StopMotion");yield return new WaitForFixedUpdate();yield return null;
            if(ExactPlayback(name,1,true,out var info))
            {
                observation.maxNormalized=Mathf.Max(observation.maxNormalized,info.normalizedTime);observation.variation=Mathf.Max(observation.variation,Variation(bones,initial));
                if(!captured&&info.normalizedTime>=.45f){Capture("action-"+name+"-actual-playback");captured=true;}
            }
            if(continuous&&observation.maxNormalized>=1.15f)
            {
                Assert.That(observation.finishes,Is.Zero,"Continuous playback cannot silently become a successful finish.");Assert.That(Read(motor,"ActionPlaying"),Is.EqualTo(true));
                if(name=="Carry"&&restingGrip!=null){float grip=Variation(Bones("Palm"),restingGrip);Log("ACTUAL_CARRY_PALM_OFFSET degrees="+grip);Assert.That(grip,Is.GreaterThan(4),"Real carry keeps the hands in their authored grip pose while wings animate.");}
                Capture("action-"+name+"-continuous-after-loop");Call(motor,"InterruptAction");break;
            }
        }
        if(continuous)
        {
            deadline=Time.time+4;while(Time.time<deadline&&observation.finishes==0){yield return new WaitForFixedUpdate();yield return null;}
        }
        Capture("action-"+name+"-after");Log("ACTION_RESULT "+name+" accepted="+accepted+" starts="+observation.starts+" contacts="+observation.contacts+" finishes="+observation.finishes+" actualMaxNormalized="+observation.maxNormalized+" relevantBoneRotationDegrees="+observation.variation+" completed="+observation.completed);
        Assert.That(accepted,Is.True);Assert.That(observation.starts,Is.EqualTo(1));Assert.That(observation.contacts,Is.EqualTo(1));Assert.That(observation.finishes,Is.EqualTo(1));Assert.That(observation.correct,Is.True,"Actual observed state, mapped weighted clip and its own normalized contact time must agree.");
        Assert.That(observation.contactAt,Is.GreaterThan(observation.startAt));Assert.That(observation.maxNormalized,Is.GreaterThanOrEqualTo(continuous?1.15f:.9f));Assert.That(observation.variation,Is.GreaterThan(.5f),"The relevant real bones must visibly change, beyond root motion.");
        Assert.That(observation.completed,Is.EqualTo(!continuous),"Interrupted continuous clips do not establish completed one-shot actions.");
    }
    [UnityTest,Timeout(120000)] public IEnumerator InfernoSleepInterruptAndWakeUseActualContinuousAndCompletedPlayback()
    {
        Construct("dragon-inferno",Vector3.up);yield return Warmup();ObserveActions();yield return PlayObservedAction("Sleep","Neck","Spine","Leg");yield return PlayObservedAction("Wake","Neck","Spine","Jaw");
    }
    [UnityTest,Timeout(120000)] public IEnumerator InfernoGrabCarryDropUseActualGripAndWingAnimations()
    {
        Construct("dragon-inferno",Vector3.up);yield return Warmup();
        for(int step=0;step<150;step++){Call(motor,"MoveWorld",new Vector3(0,.4f,1).normalized,false,true);yield return new WaitForFixedUpdate();}
        yield return null;Assert.That(Read(motor,"IsFlying"),Is.EqualTo(true));restingGrip=Bones("Palm").Select(bone=>bone.localRotation).ToArray();ObserveActions();
        yield return PlayObservedAction("Grab","Arm","Palm");yield return PlayObservedAction("Carry","Wing");yield return PlayObservedAction("Drop","Arm","Palm");
    }
    [UnityTest,Timeout(120000)] public IEnumerator InfernoEatAndInspectEggUseActualJawAndNeckAnimations()
    {
        Construct("dragon-inferno",Vector3.up);yield return Warmup();ObserveActions();yield return PlayObservedAction("Eat","Jaw","Neck","Arm");yield return PlayObservedAction("InspectEgg","Neck","Head");
    }
    [UnityTest,Timeout(90000)] public IEnumerator SparkHatchUsesActualMappedClipAndChangingGenericRigBones()
    {
        Construct("dragon-spark",Vector3.up);yield return Warmup();ObserveActions();yield return PlayObservedAction("Hatch","Spine","Neck","Leg");
    }
    [UnityTest,Timeout(90000)] public IEnumerator InfernoLandingRequiresRealSupportAndActualLandClip()
    {
        Construct("dragon-inferno",Vector3.up,true);yield return Warmup();
        int completedBefore=(int)Read(motor,"CompletedLandings");Assert.That(completedBefore,Is.Zero,"Prepare resets the actual base-landing proof.");
        for(int step=0;step<170;step++)
        {
            Call(motor,"MoveWorld",new Vector3(0,.4f,1).normalized,false,true);yield return new WaitForFixedUpdate();
            if(step==0||step==5||step==15||step==30||step==60||step==120)Log(Pose("actor-prelanding-takeoff-step-"+step)+" prepared="+Read(motor,"Prepared")+" paused="+Read(motor,"Paused")+" kinematic="+body.isKinematic+" animatorEnabled="+animator.enabled+" actorPrepared="+Read(character,"Prepared")+" actorPaused="+Read(character,"Paused"));
        }
        yield return null;Log(Pose("actor-prelanding-after-takeoff")+" "+Layers());Assert.That(Read(motor,"IsFlying"),Is.EqualTo(true),"Actual saved Actor must first establish real flight before landing is tested; inspect actor-prelanding diagnostics.");Assert.That(body.position.y,Is.GreaterThan(10));Capture("landing-before-descent");
        Vector3 goal=new Vector3(body.position.x,0,body.position.z+70);Assert.That(Call(character,"RequestLanding",goal),Is.EqualTo(true));float deadline=Time.time+30,maxLandTime=0,variation=0,nextTrace=0;bool captured=false,sawPublicLanding=false;string lastState="";
        var bones=RelevantBones("Leg","Wing");var initial=bones.Select(bone=>bone.localRotation).ToArray();
        while(Time.time<deadline)
        {
            Call(character,"Tick",Time.fixedDeltaTime);yield return new WaitForFixedUpdate();yield return null;
            var animal=Read(motor,"animal");string state=((UnityEngine.Object)Read(animal,"ActiveStateID")).name;
            if(state!=lastState||Time.time>=nextTrace)
            {
                var fly=((IEnumerable)Read(animal,"states")).Cast<object>().Single(value=>value.GetType().Name=="Fly");Vector3 pivot=(Vector3)Read(animal,"Main_Pivot_Point");
                float rayLength=(float)Read(Read(fly,"LandMultiplier"),"Value")*(float)Read(animal,"ScaleFactor");
                var hits=Physics.RaycastAll(pivot,Vector3.down,200,~0,QueryTriggerInteraction.Ignore).Where(hit=>!hit.transform.IsChildOf(model.transform)).OrderBy(hit=>hit.distance).ToArray();
                Log("LANDING_NATIVE_RAY pivot="+pivot+" vendorRayLength="+rayLength+" actualHipHeight="+Read(animal,"Height")+" actualHit="+(hits.Length==0?"none":hits[0].collider.name+" distance="+hits[0].distance)+" "+Pose("landing-state"));lastState=state;nextTrace=Time.time+5;
            }
            if(ExactPlayback("Land",0,false,out var info))
            {
                maxLandTime=Mathf.Max(maxLandTime,info.normalizedTime);variation=Mathf.Max(variation,Variation(bones,initial));sawPublicLanding|=(bool)Read(motor,"LandingPlaying");
                if(animator.IsInTransition(0)&&info.normalizedTime>=.9f)Log("LANDING_EXIT_EVIDENCE actualNormalized="+info.normalizedTime+" transitionAnyState="+animator.GetAnimatorTransitionInfo(0).anyState+" landingPlaying="+Read(motor,"LandingPlaying")+" completed="+Read(motor,"CompletedLandings")+" "+Layers());
                if(!captured&&info.normalizedTime>=.35f){Capture("landing-actual-Land-clip");captured=true;}
            }
            if((bool)Read(motor,"IsGrounded")&&!(bool)Read(motor,"IsFlying")&&maxLandTime>=.9f&&body.linearVelocity.magnitude<1&&(int)Read(motor,"CompletedLandings")==completedBefore+1)break;
        }
        Capture("landing-supported-end");var support=Physics.RaycastAll(body.position+Vector3.up*2,Vector3.down,Mathf.Max(4,(float)Read(Read(motor,"animal"),"Height")+4),~0,QueryTriggerInteraction.Ignore).Where(hit=>!hit.transform.IsChildOf(model.transform)&&hit.normal.y>.7f).OrderBy(hit=>hit.distance).ToArray();
        Log("LANDING_RESULT goal="+goal+" actualLandNormalized="+maxLandTime+" relevantBoneDegrees="+variation+" publicLandingObserved="+sawPublicLanding+" completedLandings="+Read(motor,"CompletedLandings")+" actualSupport="+(support.Length==0?"none":support[0].collider.name+" distance="+support[0].distance)+" "+Pose("landing-end")+" "+Layers());
        Log("LANDING_RECEIPT_STATE observed="+Read(motor,"landingObserved")+" genuineNaturalExit="+Read(motor,"landingNaturalExit")+" interrupted="+Read(motor,"landingInterrupted")+" cancelledOperation="+Read(motor,"landingReceiptCancelled")+" lastWeightedLandTime="+Read(motor,"landingNormalized"));
        Assert.That(Read(motor,"IsGrounded"),Is.EqualTo(true));Assert.That(Read(motor,"IsFlying"),Is.EqualTo(false));Assert.That(maxLandTime,Is.GreaterThanOrEqualTo(.9f),"Grounded state without actual Land playback is not animation acceptance.");Assert.That(variation,Is.GreaterThan(1));Assert.That(support.Length,Is.GreaterThan(0));Assert.That(Vector3.ProjectOnPlane(body.position-goal,Vector3.up).magnitude,Is.LessThan(12));
        Assert.That(sawPublicLanding,Is.True);Assert.That(Read(motor,"LandingPlaying"),Is.EqualTo(false));Assert.That(Read(motor,"CompletedLandings"),Is.EqualTo(completedBefore+1),"Only the genuinely completed natural base animation exit supplies landing proof.");
        for(int step=0;step<8;step++){Call(character,"Tick",Time.fixedDeltaTime);yield return new WaitForFixedUpdate();}Assert.That(Read(motor,"CompletedLandings"),Is.EqualTo(completedBefore+1),"A settled continuing ground state cannot duplicate the landing receipt.");
        var landedAnimal=Read(motor,"animal");Log("LANDING_CORE_RESET lockMovement="+Read(landedAnimal,"LockMovement")+" persistent="+Read(Read(landedAnimal,"ActiveState"),"IsPersistent"));
        Assert.That(Read(landedAnimal,"LockMovement"),Is.EqualTo(false),"The real completed Land must release its temporary movement lock through the core modifier.");
        Assert.That(Read(Read(landedAnimal,"ActiveState"),"IsPersistent"),Is.EqualTo(false),"The core state must permit normal subsequent state arbitration.");
        Vector3 groundStart=body.position;
        for(int step=0;step<100;step++){Call(motor,"MoveWorld",Vector3.forward,false,false);yield return new WaitForFixedUpdate();}
        float groundTravel=Vector3.ProjectOnPlane(body.position-groundStart,Vector3.up).magnitude;
        Log("ACTUAL_POST_LAND_WALK travel="+groundTravel+" "+Pose("post-land-ground-command"));Assert.That(groundTravel,Is.GreaterThan(2),"The actual landed animal must walk again without forcibly unlocking or resetting its state.");
        Vector3 secondTakeoff=body.position;
        for(int step=0;step<170;step++){Call(motor,"MoveWorld",new Vector3(0,.4f,1).normalized,false,true);yield return new WaitForFixedUpdate();}
        Log("ACTUAL_POST_LAND_TAKEOFF heightGain="+(body.position.y-secondTakeoff.y)+" "+Pose("post-land-second-takeoff"));
        Assert.That(Read(motor,"IsFlying"),Is.EqualTo(true));Assert.That(body.position.y-secondTakeoff.y,Is.GreaterThan(10),"A genuine second vendor takeoff must work after landing and walking.");
        Assert.That(Read(motor,"CompletedLandings"),Is.EqualTo(completedBefore+1));
    }
    [UnityTest,Timeout(90000)] public IEnumerator ActualActorMouthAudioFollowsFlightHonorsSharedMuteAndReusesEmitters()
    {
        Construct("dragon-inferno",Vector3.up,true);yield return Warmup();
        previousListener=AudioListener.volume;listenerCaptured=true;
        // Inactive real settings component supplies the same public settings
        // values without invoking its display-loading Awake in this audio test.
        audioSettingsOwner=new GameObject("Isolated actual sports audio settings data");audioSettingsOwner.SetActive(false);
        var settings=audioSettingsOwner.AddComponent(motor.GetType().Assembly.GetType("LWS.TruckSports.SportsSettings",true));settings.GetType().GetField("master").SetValue(settings,.6f);settings.GetType().GetField("sfx").SetValue(settings,.4f);Call(settings,"ApplyAudio");Call(character,"BindAudio",settings);
        var voice=(AudioSource)Read(character,"voice");var mouth=(Transform)Read(character,"mouth");var fire=mouth.Find("FireBreathAudio")?.GetComponent<AudioSource>();
        Assert.That(voice.transform.parent,Is.SameAs(mouth));Assert.That(fire,Is.Not.Null,"Actual owned Fire Throw supplies this intake's dedicated breath voice.");Assert.That(voice.volume,Is.EqualTo(.26f).Within(.0001f));Assert.That(fire.volume,Is.EqualTo(.2f).Within(.0001f));Assert.That(AudioListener.volume,Is.EqualTo(.6f).Within(.0001f),"Master remains a single global listener gain, not double multiplication.");
        Vector3 root=model.transform.position,emitter=voice.transform.position;int sourceCount=model.GetComponentsInChildren<AudioSource>(true).Length;
        var intent=motor.GetType().Assembly.GetType("LWS.TruckDragonKingdom.DragonCharacterIntent",true);Call(character,"SetIntent",Enum.Parse(intent,"PlayerMounted"),body.position,-1);
        for(int step=0;step<110;step++){Call(character,"Tick",Time.fixedDeltaTime);yield return new WaitForFixedUpdate();}
        Assert.That(Call(character,"RequestAttack",Action("Breath"),-1),Is.EqualTo(true));var warning=voice.clip;Assert.That(warning,Is.Not.Null);Assert.That(warning.name.ToLowerInvariant(),Does.Match(".*(growl|roar).*"));Assert.That(Call(character,"RequestAttack",Action("Breath"),-1),Is.EqualTo(false));Assert.That(voice.clip,Is.SameAs(warning));
        bool actualBreath=false;for(int step=0;step<210;step++)
        {
            Call(character,"Tick",Time.fixedDeltaTime);Call(motor,"MoveWorld",new Vector3(0,.4f,1).normalized,false,true);yield return new WaitForFixedUpdate();yield return null;
            actualBreath|=(bool)Read(motor,"ActionPlaying")&&Read(motor,"CurrentAction").ToString()=="Breath";
        }
        Assert.That(actualBreath,Is.True);Assert.That(Read(motor,"IsFlying"),Is.EqualTo(true));Assert.That(Vector3.Distance(voice.transform.position,emitter),Is.GreaterThan(12));Assert.That(Vector3.Distance(voice.transform.position,mouth.position),Is.LessThan(.001f));Assert.That(Vector3.Distance(model.transform.position,root),Is.LessThan(.001f),"Audio must follow the moving mouth without moving the semantic Actor root.");
        Assert.That(fire.clip.name,Is.EqualTo("Fire Throw"));Log("ACTUAL_SPATIAL_AUDIO mouth="+mouth.position+" emitter="+voice.transform.position+" root="+model.transform.position+" warningClip="+warning.name+" breathClip="+fire.clip.name+" listener="+AudioListener.volume+" voiceGain="+voice.volume+" breathGain="+fire.volume+" sources="+sourceCount+". Native position/gain/clip evidence only; no listening acceptance.");
        settings.GetType().GetField("sfx").SetValue(settings,0f);Call(character,"Tick",Time.fixedDeltaTime);Assert.That(voice.volume,Is.Zero);Assert.That(fire.volume,Is.Zero);Assert.That(voice.mute&&fire.mute,Is.True);
        settings.GetType().GetField("sfx").SetValue(settings,1f);settings.GetType().GetField("master").SetValue(settings,0f);Call(settings,"ApplyAudio");Call(character,"Tick",Time.fixedDeltaTime);Assert.That(AudioListener.volume,Is.Zero);Assert.That(voice.volume,Is.Zero);Assert.That(fire.volume,Is.Zero);
        settings.GetType().GetField("master").SetValue(settings,.6f);settings.GetType().GetField("sfx").SetValue(settings,.4f);Call(settings,"ApplyAudio");for(int i=0;i<10;i++)Call(character,"Tick",Time.fixedDeltaTime);Assert.That(voice.volume,Is.EqualTo(.26f).Within(.0001f));Assert.That(fire.volume,Is.EqualTo(.2f).Within(.0001f));
        float deadline=Time.time+10;bool requested=false;
        while(Time.time<deadline&&!((bool)Read(motor,"ActionPlaying")&&Read(motor,"CurrentAction").ToString()=="Breath"))
        {
            if(!requested)requested=(bool)Call(character,"RequestAttack",Action("Breath"),-1);Call(character,"Tick",Time.fixedDeltaTime);Call(motor,"MoveWorld",Vector3.zero,false,true);yield return new WaitForFixedUpdate();yield return null;
        }
        Assert.That(requested,Is.True);Assert.That(Read(motor,"ActionPlaying"),Is.EqualTo(true));Call(character,"Pause",true);yield return null;int sample=fire.timeSamples;for(int i=0;i<5;i++)yield return new WaitForFixedUpdate();Assert.That(fire.timeSamples,Is.EqualTo(sample));Assert.That(voice.isPlaying||fire.isPlaying,Is.False);Call(character,"Pause",false);
        Call(character,"ResetCharacter");Assert.That(voice.isPlaying||fire.isPlaying,Is.False);var truckType=motor.GetType().Assembly.GetType("LWS.TruckSports.SportsEventTruck",true);Call(character,"Prepare",species,Array.CreateInstance(truckType,0),0,917);yield return null;
        Assert.That(Read(character,"voice"),Is.SameAs(voice));Assert.That(mouth.Find("FireBreathAudio").GetComponent<AudioSource>(),Is.SameAs(fire));Assert.That(model.GetComponentsInChildren<AudioSource>(true).Length,Is.EqualTo(sourceCount),"Rematches reuse both mouth voices rather than allocating more emitters.");
    }
    Component PhysicalTruckTarget(Vector3 center)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckFareFight/Prefabs/Fare Fight Taxi Tractor.prefab");Assert.That(prefab,Is.Not.Null,"Use the actual original NWH tractor target, not a substitute hit sphere.");
        var target=UnityEngine.Object.Instantiate(prefab,center,Quaternion.LookRotation(-motor.transform.forward));physicalAimTargets.Add(target);
        var truck=target.GetComponent(motor.GetType().Assembly.GetType("LWS.TruckSports.SportsEventTruck",true));Assert.That(truck,Is.Not.Null);
        // Isolated passive collision receiver: retain the complete original NWH
        // assembly/colliders while disabling its unrelated driving/session loop.
        ((Behaviour)truck).enabled=false;Call(truck,"Power",false);foreach(var sound in target.GetComponentsInChildren<AudioSource>(true)){sound.Stop();sound.mute=true;}
        var truckBody=(Rigidbody)Read(truck,"body");truckBody.isKinematic=false;truckBody.detectCollisions=true;foreach(var part in target.GetComponentsInChildren<Rigidbody>(true))part.useGravity=false;
        var shape=(BoxCollider)Read(truck,"shape");Assert.That(shape.enabled,Is.True);Physics.SyncTransforms();Vector3 position=truckBody.position+center-shape.bounds.center;truckBody.position=position;target.transform.position=position;Call(Read(truck,"tractorMotor"),"ResetAttachedBodies");Physics.SyncTransforms();
        return truck;
    }
    void AimHit<T>(int slot,T element,float damage,Vector3 point)
    {
        physicalAimHits.Add(slot);var mouth=(Transform)Read(character,"mouth");aimActualClip=ExactPlayback("Breath",1,true,out var info);aimContactNormalized=info.normalizedTime;aimAngle=Vector3.Angle(mouth.forward,point-mouth.position);aimRange=Vector3.Distance(mouth.position,point);aimElement=element.ToString();
        Log("ACTUAL_CALIBRATED_BREATH_HIT slot="+slot+" element="+element+" damage="+damage+" actualClip="+aimActualClip+" normalized="+info.normalizedTime+" mouth="+mouth.position+" mouthForward="+mouth.forward+" rawBoneForward="+mouth.parent.forward+" bodyForward="+motor.transform.forward+" physicalPoint="+point+" angle="+aimAngle+" range="+aimRange+" "+Layers());
    }
    static string HierarchyPath(Transform value)
    {
        if(value==null)return "none";string path=value.name;while(value.parent!=null){value=value.parent;path=value.name+"/"+path;}return path;
    }
    void AimGateEvidence(string phase)
    {
        var mouth=(Transform)Read(character,"mouth");float range=(float)Read(character,"breathRange");var overlap=new Collider[96];int overlaps=Physics.OverlapSphereNonAlloc(mouth.position,range,overlap,~0,QueryTriggerInteraction.Ignore);
        Log("ACTUAL_AIM_GATES phase="+phase+" prepared="+Read(character,"Prepared")+" paused="+Read(character,"Paused")+" actionPlaying="+Read(motor,"ActionPlaying")+" currentAction="+Read(motor,"CurrentAction")+" normalized="+Read(motor,"ActionNormalizedTime")+" mouth="+mouth.position.ToString("F4")+" calibratedForward="+mouth.forward.ToString("F4")+" rawBoneForward="+mouth.parent.forward.ToString("F4")+" bodyForward="+motor.transform.forward.ToString("F4")+" range="+range+" halfAngle="+Read(character,"breathHalfAngle")+" overlapCount96="+overlaps+" protectedSlot="+Read(character,"protectedSlot")+" hitMask="+Read(character,"hitMask")+" exactClip="+ExactPlayback("Breath",1,true,out var state)+" clipNormalized="+state.normalizedTime+" "+Layers());
        for(int slot=0;slot<aimTrucks.Length;slot++)
        {
            var truck=aimTrucks[slot];var receiver=(Rigidbody)Read(truck,"body");var delta=receiver.worldCenterOfMass-mouth.position;var shape=(BoxCollider)Read(truck,"shape");
            var contacts=overlap.Take(overlaps).Where(value=>value!=null&&value.transform.IsChildOf(truck.transform)).ToArray();var ray=new RaycastHit[32];int rays=Physics.RaycastNonAlloc(mouth.position,delta.normalized,ray,delta.magnitude,(LayerMask)Read(character,"obstructionMask"),QueryTriggerInteraction.Ignore);
            var sorted=ray.Take(rays).Where(value=>value.collider!=null&&!value.collider.transform.IsChildOf(character.transform)).OrderBy(value=>value.distance).ToArray();var nearest=sorted.Length>0?sorted[0]:default;
            bool clear=rays<ray.Length&&(nearest.collider==null||nearest.collider.transform.IsChildOf(truck.transform));
            Log("ACTUAL_AIM_TARGET phase="+phase+" slot="+slot+" active="+truck.gameObject.activeInHierarchy+" truckEnabled="+((Behaviour)truck).enabled+" validTarget="+Call(character,"ValidTarget",slot)+" body="+receiver.position.ToString("F4")+" centerOfMass="+receiver.worldCenterOfMass.ToString("F4")+" detectCollisions="+receiver.detectCollisions+" kinematic="+receiver.isKinematic+" angle="+Vector3.Angle(mouth.forward,delta)+" distance="+delta.magnitude+" shapeEnabled="+shape.enabled+" shapeTrigger="+shape.isTrigger+" shapeActive="+shape.gameObject.activeInHierarchy+" shapeBounds="+shape.bounds+" overlapMembership="+contacts.Length+" contacts=["+string.Join(";",contacts.Select(value=>HierarchyPath(value.transform)))+"] rays32="+rays+" clearSight="+clear+" nearest="+HierarchyPath(nearest.collider!=null?nearest.collider.transform:null)+" nearestDistance="+nearest.distance+" allNonSelfRayHits=["+string.Join(";",sorted.Select(value=>HierarchyPath(value.collider.transform)+"@"+value.distance))+ "] colliderFlags=["+string.Join(";",truck.GetComponentsInChildren<Collider>(true).Select(value=>HierarchyPath(value.transform)+":enabled="+value.enabled+",active="+value.gameObject.activeInHierarchy+",trigger="+value.isTrigger))+ "]");
        }
    }
    void AimContact<T>(T action){if(action.ToString()=="Breath"){AimGateEvidence("actual-ActionContact");Capture("calibrated-mouth-actual-Breath-contact");}}
    [UnityTest,Timeout(90000)] public IEnumerator CalibratedMouthBreathActuallyHitsFrontNwhTruckAndRejectsSideTruck()
    {yield return CalibratedAim("dragon-inferno",Vector3.up);}
    [UnityTest,Timeout(90000)] public IEnumerator FireCalibratedMouthBreathActuallyHitsFrontNwhTruckAndRejectsSideTruck()
    {yield return CalibratedAim("dragon-fire",Vector3.up*35);}
    [UnityTest,Timeout(90000)] public IEnumerator RidersInfernoLoadBearingBreathActuallyHitsFrontNwhTruckAndRejectsSideTruck()
    {yield return CalibratedAim("dragon-inferno",Vector3.up,"Assets/TruckDragonRiders/Animations/dragon-inferno/Mounted Actions.overrideController");}
    [UnityTest,Timeout(90000)] public IEnumerator RidersFireLoadBearingBreathActuallyHitsFrontNwhTruckAndRejectsSideTruck()
    {yield return CalibratedAim("dragon-fire",Vector3.up*35,"Assets/TruckDragonRiders/Animations/dragon-fire/Mounted Hover.overrideController");}
    [UnityTest,Timeout(120000)] public IEnumerator RidersTransientInfernoNormalFlightRestoresOriginalBreathAtRealFrontHitAndRejectsSideTruck()
    {yield return CalibratedAim("dragon-inferno",Vector3.up*35,"Assets/TruckDragonRiders/Animations/dragon-inferno/Mounted Actions.overrideController",true);}
    [UnityTest,Timeout(120000)] public IEnumerator RidersQuarterEnvelopeInfernoRestoresOriginalBreathFrontHitAndRejectsSideTruck(){yield return CalibratedAim("dragon-inferno",Vector3.up*35,"Assets/TruckDragonRiders/Animations/dragon-inferno/Mounted Actions.overrideController",true,.25f);}
    [UnityTest,Timeout(120000)] public IEnumerator RidersQuarterEnvelopeFireRestoresOriginalBreathFrontHitAndRejectsSideTruck(){yield return CalibratedAim("dragon-fire",Vector3.up*35,"Assets/TruckDragonRiders/Animations/dragon-fire/Mounted Hover.overrideController",true,.25f);}
    [UnityTest,Timeout(120000)] public IEnumerator RidersHalfEnvelopeInfernoRestoresOriginalBreathFrontHitAndRejectsSideTruck(){yield return CalibratedAim("dragon-inferno",Vector3.up*35,"Assets/TruckDragonRiders/Animations/dragon-inferno/Mounted Actions.overrideController",true,.5f);}
    [UnityTest,Timeout(120000)] public IEnumerator RidersHalfEnvelopeFireRestoresOriginalBreathFrontHitAndRejectsSideTruck(){yield return CalibratedAim("dragon-fire",Vector3.up*35,"Assets/TruckDragonRiders/Animations/dragon-fire/Mounted Hover.overrideController",true,.5f);}
    [UnityTest,Timeout(120000)] public IEnumerator RidersSafeMountedInfernoBreathRetainsAimAndActuallyHitsFrontAndRejectsSide(){yield return CalibratedAim("dragon-inferno",Vector3.up*35,"Assets/TruckDragonRiders/Animations/dragon-inferno/Mounted Actions.overrideController",true,null,true);}
    [UnityTest,Timeout(120000)] public IEnumerator RidersSafeMountedFireBreathRetainsAimAndActuallyHitsFrontAndRejectsSide(){yield return CalibratedAim("dragon-fire",Vector3.up*35,"Assets/TruckDragonRiders/Animations/dragon-fire/Mounted Hover.overrideController",true,null,true);}
    IEnumerator EstablishAimFlight()
    {
        float deadline=Time.realtimeSinceStartup+8;int steps=0;
        while(!(bool)Read(motor,"IsFlying")&&Time.realtimeSinceStartup<deadline)
        {
            Call(motor,"MoveWorld",Vector3.zero,false,true);yield return new WaitForFixedUpdate();yield return null;
            if(steps++%25==0)Log(Pose("aim-real-zero-flight-command"));
        }
        Assert.That(Read(motor,"IsFlying"),Is.EqualTo(true),"Real zero-direction wantsFlight input must reach the vendor Fly core, without state force or a body pose patch.");
        for(int i=0;i<30;i++){Call(motor,"MoveWorld",Vector3.zero,false,true);yield return new WaitForFixedUpdate();yield return null;Assert.That(Read(motor,"IsFlying"),Is.EqualTo(true));}
        Log(Pose("aim-real-flight-established"));
    }
    IEnumerator CalibratedAim(string speciesId,Vector3 spawn,string ownedOverridePath=null,bool transientNormalFlight=false,float? sourceEnvelopeFlap=null,bool safeMountedProfile=false)
    {
        Construct(speciesId,spawn,true,ownedOverridePath);
        if(transientNormalFlight)
        {
            output=Path.Combine(output,"TransientNormalFlight");Directory.CreateDirectory(output);var originalBreath=EffectiveMappedClip("Breath");var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);var objects=(UnityEngine.Object[])(safeMountedProfile?author.GetMethod("CreateTransientSafeMountedController").Invoke(null,new object[]{character,speciesId}):sourceEnvelopeFlap.HasValue?author.GetMethod("CreateTransientCabFlightEnvelope").Invoke(null,new object[]{character,speciesId,sourceEnvelopeFlap.Value}):author.GetMethod("CreateTransientCabFlightController").Invoke(null,new object[]{character,speciesId}));transientRiderCabObjects.AddRange(objects);animator.runtimeAnimatorController=objects.OfType<AnimatorOverrideController>().Single();if(safeMountedProfile){Assert.That(EffectiveMappedClip("Breath"),Is.Not.SameAs(originalBreath),"Actual connected safe mounted derivative must be resolved by the production mapping; source byte/aim preservation is independently enforced by its native factory.");Log("EXPLICIT_SAFE_MOUNTED_BREATH_DERIVATIVE actual="+EffectiveMappedClip("Breath").name+" original="+originalBreath.name+" sourceHeadAndMuzzleWorldAimFactoryGate=.5degrees originalJawTimingsEvents=true");}else Assert.That(EffectiveMappedClip("Breath"),Is.SameAs(originalBreath));
        }
        yield return Warmup();if(speciesId=="dragon-fire"||transientNormalFlight)yield return EstablishAimFlight();var mouth=(Transform)Read(character,"mouth");
        Assert.That(mouth.name,Is.EqualTo("LWS Dragon Mouth Socket"));Assert.That(mouth.parent.name,Is.EqualTo((string)Read(species,"mouthBone")));Assert.That(Vector3.Distance(mouth.position,mouth.parent.position),Is.LessThan(.0001f));
        Log("ACTUAL_NEUTRAL_MOUTH_AIM species="+speciesId+" ownedOverride="+ownedOverridePath+" angle="+Vector3.Angle(mouth.forward,motor.transform.forward)+" mouthForward="+mouth.forward.ToString("F6")+" motorForward="+motor.transform.forward.ToString("F6")+" mouthPosition="+mouth.position.ToString("F6")+" motorEuler="+motor.transform.eulerAngles.ToString("F6")+" layers="+Layers());
        Capture("calibrated-mouth-actual-neutral-head-before-angle-gate");
        if(!transientNormalFlight)Assert.That(Vector3.Angle(mouth.forward,motor.transform.forward),Is.LessThan(20),"Neutral real head socket must aim along the actual model's front, retaining its animated head pose.");
        else{Assert.That(animator.GetCurrentAnimatorClipInfo(0).Any(value=>value.clip.name.StartsWith("TRANSIENT_CAB_",StringComparison.Ordinal)&&value.weight>.0001f),Is.True,"Observe actual authored normal-flight playback before testing original attack restoration.");Log("EXPLICIT_TRANSIENT_NORMAL_FLIGHT_MOUTH_AIM_CHANGE angle="+Vector3.Angle(mouth.forward,motor.transform.forward)+"; neutral20degree original-profile assertion remains unchanged in its original tests. Actual original Breath front-hit/cone/side-rejection/contact/root guards below are all retained.");}
        var front=PhysicalTruckTarget(mouth.position+motor.transform.forward*18);var side=PhysicalTruckTarget(mouth.position-motor.transform.right*18+motor.transform.forward*4);aimTrucks=new[]{front,side};
        var truckType=motor.GetType().Assembly.GetType("LWS.TruckSports.SportsEventTruck",true);var roster=Array.CreateInstance(truckType,2);roster.SetValue(front,0);roster.SetValue(side,1);Call(character,"Prepare",species,roster,2,917);yield return Warmup();if(speciesId=="dragon-fire"||transientNormalFlight)yield return EstablishAimFlight();mouth=(Transform)Read(character,"mouth");
        var frontBody=(Rigidbody)Read(front,"body");var sideBody=(Rigidbody)Read(side,"body");float half=(float)Read(character,"breathHalfAngle"),range=(float)Read(character,"breathRange"),originalActorMass=body.mass,originalFrontMass=frontBody.mass,originalSideMass=sideBody.mass;
        if(!transientNormalFlight)Assert.That(Vector3.Angle(mouth.forward,frontBody.worldCenterOfMass-mouth.position),Is.LessThan(half));Assert.That(Vector3.Distance(mouth.position,sideBody.worldCenterOfMass),Is.LessThan(range));Assert.That(Vector3.Angle(mouth.forward,sideBody.worldCenterOfMass-mouth.position),Is.GreaterThan(half+15),"Side rejection must prove the true cone, not merely an out-of-range target.");
        aimHitEvent=character.GetType().GetEvent("TruckHit");var callback=GetType().GetMethod(nameof(AimHit),Any).MakeGenericMethod(motor.GetType().Assembly.GetType("LWS.TruckDragonKingdom.DragonElement",true));aimHitHandler=Delegate.CreateDelegate(aimHitEvent.EventHandlerType,this,callback);aimHitEvent.AddEventHandler(character,aimHitHandler);
        Subscribe("ActionStarted",nameof(Started));Subscribe("ActionContact",nameof(Contact));Subscribe("ActionContact",nameof(AimContact));Subscribe("ActionFinished",nameof(Finished));
        var intent=motor.GetType().Assembly.GetType("LWS.TruckDragonKingdom.DragonCharacterIntent",true);Call(character,"SetIntent",Enum.Parse(intent,"PlayerMounted"),body.position,-1);
        for(int step=0;step<110;step++){Call(character,"Tick",Time.fixedDeltaTime);if(speciesId=="dragon-fire"||transientNormalFlight)Call(motor,"MoveWorld",Vector3.zero,false,true);else Call(motor,"StopMotion");yield return new WaitForFixedUpdate();}
        if(speciesId=="dragon-fire"||transientNormalFlight)Assert.That(Read(motor,"IsFlying"),Is.EqualTo(true),"The real airborne fixture must reach its vendor Fly core before the strike.");
        Vector3 root=model.transform.position,bodyStart=body.position;Quaternion rotation=body.rotation;AimGateEvidence("before-request");Capture("calibrated-mouth-before-front-target-hit");Assert.That(Call(character,"RequestAttack",Action("Breath"),-1),Is.EqualTo(true));bool played=false;float deadline=Time.time+12;
        while(Time.time<deadline)
        {
            Call(character,"Tick",Time.fixedDeltaTime);if(speciesId=="dragon-fire"||transientNormalFlight)Call(motor,"MoveWorld",Vector3.zero,false,true);else Call(motor,"StopMotion");yield return new WaitForFixedUpdate();yield return null;played|=(bool)Read(motor,"ActionPlaying")&&Read(motor,"CurrentAction").ToString()=="Breath";
            if(played&&!(bool)Read(motor,"ActionPlaying"))break;
        }
        Capture("calibrated-mouth-after-real-front-target-hit");
        AimGateEvidence("after-playback");Log("CALIBRATED_AIM_RESULT hits=["+string.Join(",",physicalAimHits)+"] actionStarts="+starts+" actionContacts="+contacts+" actionFinishes="+finishes+" rootDisplacement="+Vector3.Distance(model.transform.position,root)+" bodyDisplacement="+Vector3.Distance(body.position,bodyStart)+" bodyRotationDegrees="+Quaternion.Angle(body.rotation,rotation)+" frontVelocity="+frontBody.linearVelocity+" sideVelocity="+sideBody.linearVelocity+". Real original NWH passive collision targets; no driving or listening claim, no injected contact/pose or animation callbacks.");
        Assert.That(played,Is.True);Assert.That(physicalAimHits,Is.EqualTo(new[]{0}),"Actual observed Breath contact must hit the frontal physical truck once and reject the in-range side truck.");Assert.That(aimActualClip,Is.True);Assert.That(aimContactNormalized,Is.GreaterThanOrEqualTo(.45f));Assert.That(aimElement,Is.EqualTo("Fire"));Assert.That(aimAngle,Is.LessThanOrEqualTo(half));Assert.That(aimRange,Is.LessThanOrEqualTo(range));
        Assert.That(Vector3.Distance(model.transform.position,root),Is.LessThan(.001f));Assert.That(Vector3.Distance(body.position,bodyStart),Is.LessThan(.1f));Assert.That(Quaternion.Angle(body.rotation,rotation),Is.LessThan(.1f),"Socket calibration must not rotate the original motor/body to manufacture a hit.");
        if(transientNormalFlight){Assert.That(body.mass,Is.EqualTo(originalActorMass));Assert.That(frontBody.mass,Is.EqualTo(originalFrontMass));Assert.That(sideBody.mass,Is.EqualTo(originalSideMass));}
    }
    [TearDown] public void Cleanup()
    {
        Exception cleanupFailure=null;void Release(System.Action action){try{action();}catch(Exception error){if(cleanupFailure==null)cleanupFailure=error;TestContext.WriteLine("NATIVE_MOTION_CLEANUP_FAILURE "+error);}}
        try
        {
            if(!string.IsNullOrEmpty(output)){string leaf=TestContext.CurrentContext.Test.MethodName;if(leaf.Length>48)leaf=leaf.Substring(0,24)+"-"+Hash128.Compute(leaf).ToString().Substring(0,8);Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,leaf+".txt"),report.ToString());}
        }
        finally
        {
            Release(()=>{if(aimHitHandler!=null&&character!=null)aimHitEvent.RemoveEventHandler(character,aimHitHandler);});
            foreach(var pair in subscriptions)Release(()=>{if(motor!=null)pair.Key.RemoveEventHandler(motor,pair.Value);});subscriptions.Clear();
            Release(()=>{if(motor!=null){Call(motor,"StopMotion");if((bool)Read(motor,"Paused"))Call(motor,"Pause",false);}});Release(()=>{if(character!=null)Call(character,"ResetCharacter");});
            foreach(var item in new[]{model,floor,lamp,cameraOwner})Release(()=>{if(item!=null)UnityEngine.Object.DestroyImmediate(item);});
            foreach(var item in transientRiderCabObjects)Release(()=>{if(item!=null)UnityEngine.Object.DestroyImmediate(item);});transientRiderCabObjects.Clear();
            foreach(var target in physicalAimTargets)Release(()=>{if(target!=null)UnityEngine.Object.DestroyImmediate(target);});physicalAimTargets.Clear();
            Release(()=>{if(audioSettingsOwner!=null)UnityEngine.Object.DestroyImmediate(audioSettingsOwner);});Release(()=>{if(listenerCaptured)AudioListener.volume=previousListener;});Time.timeScale=1;
        }
        if(cleanupFailure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(cleanupFailure).Throw();
    }
}
#endif
