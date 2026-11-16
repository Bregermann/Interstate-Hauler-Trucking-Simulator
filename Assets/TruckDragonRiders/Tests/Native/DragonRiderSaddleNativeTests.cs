#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

public sealed class DragonRiderSaddleNativeTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    Component match;IDisposable lease;string output;
    bool initialHumanSources;GameObject inputIsolation;InputSettings originalInput,temporaryInput;Keyboard startupKeyboard;readonly Gamepad[] startupPads=new Gamepad[2];
    string ProofScope=>initialHumanSources?"ACTUAL_FOUR_LEGALLY_ASSIGNED_SOFTWARE_HUMAN_INITIAL_FLIGHT_TWO_STARTS_10_SECONDS_NATIVE_TAKEOFF_CORE_FLY_BOUNDED_OWNED_BANK_INPUT_NO_POSE_STATE_SCORE_OR_CAMERA_INJECTION_NOT_AI_COURSE_OR_HARDWARE_ACCEPTANCE":"ACTUAL_SAVED_RIDERS_NWH_MOUNTED_POSES_AND_DEPTH_TESTED_BODY_PIXEL_WITNESS_NORMAL_AI_TWO_PREPARES_NO_INJECTED_FLIGHT_SCORE_OR_CAMERA_POSE";
    DragonRiderSaddleFrameObserver observer;
    readonly List<Frame> frames=new List<Frame>();
    readonly HashSet<string> observedSeating=new HashSet<string>();
    [Serializable]sealed class Frame
    {
        public int round,slot,carrierAnimationCallbacks,matchSeed;public Vector2 actualAssignedDrive;public bool realCoreFly;
        public float clock,bank,pitch,bodyAboveSpine,lowestShapeAboveSeat,dislodgedAt,collisionNormalSpeed,callbackFixedTime,observationFixedTime,rootIntegrationError,rootRotationIntegrationError;
        public string species,phase,notice,dislodgement,collision,actualBaseClips,actualActionClips;
        public bool animatorEnabled,motorPaused,actualBaseTransition;
        public float actualBaseNormalizedTime,actualBaseStateLength,actualNextBaseNormalizedTime;
        public string actualAnimatorFloatParameters;
        public Vector3 truckPosition,truckEuler,socketPosition,motorBodyPosition,motorTransformPosition,motorBodyEuler,motorTransformEuler,initialPosition,lastCarrierPosition,previousCarrierPosition,callbackRootBefore,callbackTransformBefore,callbackVelocity,callbackPendingTranslation,callbackEvaluatedSocket,callbackCommandedPosition,callbackCommandedEuler,callbackBodyEuler,callbackTransformEuler,callbackFrameRotationCorrection;
        public bool mounted;public BodyFrame[] assembly;public Vector3 physicalSaddlePosition;
        public float minimumActualSkinGap,skinPlatformGap,maximumMeasuredPlatformGap,cabCenterRayPlatformDelta;
        public bool actualCenterTorso,actualPlatformTorso;
        public Vector3 actualPlatformRaySkinPoint,actualAnatomicalAnchor,actualPatchNormal;
        public SkinWitness[] actualSkin;
        public string actualWheelDeckSeatingJson;
    }
    [Serializable]sealed class BodyFrame{public string name;public bool kinematic;public Vector3 position,velocity;public float lastCarrierError,previousCarrierError;}
    [Serializable]sealed class SkinWitness{public Vector3 physicalBottomPoint,actualDorsalPoint;public bool actualTorso;public float verticalGap;public string actualNegativeSkinProofJson;}
    [Serializable]sealed class Evidence {public string scope="ACTUAL_SAVED_RIDERS_NWH_MOUNTED_POSES_AND_DEPTH_TESTED_BODY_PIXEL_WITNESS_NORMAL_AI_TWO_PREPARES_NO_INJECTED_FLIGHT_SCORE_OR_CAMERA_POSE";public List<Frame> frames;public int[] bodyPixels;}
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static object Read(object value,string name){var field=value.GetType().GetField(name,Any);return field!=null?field.GetValue(value):value.GetType().GetProperty(name,Any).GetValue(value);}
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name,Any).Invoke(value,args);
    void ConfigureInitialHumanSources()
    {
        inputIsolation=new GameObject("Riders initial-flight existing software input isolation");UnityEngine.Object.DontDestroyOnLoad(inputIsolation);inputIsolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));
        originalInput=InputSystem.settings;temporaryInput=UnityEngine.Object.Instantiate(originalInput);temporaryInput.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;temporaryInput.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=temporaryInput;
        startupKeyboard=InputSystem.AddDevice<Keyboard>();InputSystem.EnableDevice(startupKeyboard);startupKeyboard.MakeCurrent();for(int i=0;i<startupPads.Length;i++){startupPads[i]=InputSystem.AddDevice<Gamepad>();InputSystem.EnableDevice(startupPads[i]);}
        var players=(Array)Read(Read(match,"setup"),"players");var names=new[]{"Keyboard","WholeController","LeftStick","RightStick"};
        for(int slot=0;slot<4;slot++){var player=players.GetValue(slot);player.GetType().GetField("source").SetValue(player,Enum.Parse(player.GetType().GetField("source").FieldType,names[slot]));player.GetType().GetField("pad").SetValue(player,slot==0?null:startupPads[slot==1?0:1]);if(slot>0)Assert.That(Call(match,"Assign",slot,Read(player,"pad")),Is.EqualTo(true));}
        Assert.That(Read(match,"DevicesReady"),Is.EqualTo(true));Call(Read(match,"drivingViews"),"Select",0);QueueInitialHumanInputs(0);
    }
    void QueueInitialHumanInputs(float clock)
    {
        if(!initialHumanSources)return;
        // Small deliberate steering inputs use only the original assigned
        // devices. Afterwards ordinary neutral human flight continues. The
        // public initial heading/altitude, gates and all colliders stay intact.
        bool keyboardPulse=clock>=.5f&&clock<2f;float stickPulse=clock>=.5f&&clock<3f?.6f:0;var player=((Array)Read(Read(match,"setup"),"players")).GetValue(0);
        InputSystem.QueueStateEvent(startupKeyboard,keyboardPulse?new KeyboardState((Key)Read(player,"right")):new KeyboardState());InputSystem.QueueStateEvent(startupPads[0],new GamepadState{leftStick=new Vector2(stickPulse,0)});InputSystem.QueueStateEvent(startupPads[1],new GamepadState{leftStick=new Vector2(stickPulse,0),rightStick=new Vector2(stickPulse,0)});
    }
    [UnityTest,Timeout(180000)]
    public IEnumerator ActualCalibratedSaddlesKeepOriginalNwhCrewsAboveBackWithBoundedLiveBankAndVisibleBodyAcrossRematch(){yield return ObserveTwoStarts(false);}
    [UnityTest,Timeout(180000)]
    public IEnumerator TwoActualSoftwareHumanStartsKeepOriginalNwhMountedAboveBackThroughNaturalTakeoffCoreFlightAndRematch(){yield return ObserveTwoStarts(true);}
    IEnumerator ObserveTwoStarts(bool humanInitialProof)
    {
        initialHumanSources=humanInitialProof;frames.Clear();observedSeating.Clear();
        Assert.That(Application.isPlaying,Is.True);lease=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"RidersSaddle"+Guid.NewGuid().ToString("N")});Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");Assert.That(at>=0&&at+1<args.Length,Is.True);output=Path.Combine(args[at+1],initialHumanSources?"RidersInitialHumanSaddle":"RidersSemanticSaddle");Directory.CreateDirectory(output);
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/TruckDragonRiders/Scenes/TruckDragonRiders.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;
        match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("LWS.TruckDragonRiders.DragonRidersMatch"));Assert.That(match,Is.Not.Null);var setup=Read(match,"setup");setup.GetType().GetField("count").SetValue(setup,4);match.GetType().GetField("option").SetValue(match,0);Call(match,"AllBots");if(initialHumanSources)ConfigureInitialHumanSources();
        var racers=((Array)Read(match,"racers")).Cast<Component>().ToArray();var trucks=racers.Select(value=>(Component)Read(value,"truck")).ToArray();var masses=trucks.Select(value=>((Rigidbody)Read(value,"body")).mass).ToArray();var ids=trucks.Select(value=>value.GetEntityId()).ToArray();var pixels=new List<int>();
        for(int round=0;round<2;round++)
        {
            Assert.That(Call(match,"StartMatch"),Is.EqualTo(true));float deadline=Time.realtimeSinceStartup+65;float next=0;bool captured=false;
            Exception observationFailure=null;int currentRound=round;
            if(observer==null)observer=new GameObject("Native read-only final saddle observation").AddComponent<DragonRiderSaddleFrameObserver>();
            observer.Observe=()=>
            {
                if(observationFailure!=null||(float)Read(match,"Clock")<next)return;
                try
                {
                    next=(float)Read(match,"Clock")+.2f;
                    foreach(var racer in racers)Observe(racer,currentRound);
                    if(!captured&&(float)Read(match,"Clock")>=3){pixels.Add(Capture(currentRound,trucks[0]));captured=true;}
                }
                catch(Exception error){observationFailure=error;CaptureFailedObservation(currentRound);}
            };
            while((float)Read(match,"Clock")<10&&Time.realtimeSinceStartup<deadline)
            {
                QueueInitialHumanInputs((float)Read(match,"Clock"));yield return new WaitForFixedUpdate();
                if(observationFailure!=null)throw observationFailure;
            }
            QueueInitialHumanInputs(0);observer.Observe=null;if(observationFailure!=null)throw observationFailure;
            Assert.That((float)Read(match,"Clock"),Is.GreaterThanOrEqualTo(10));Assert.That(captured,Is.True);
            for(int slot=0;slot<4;slot++){Assert.That(trucks[slot].GetEntityId(),Is.EqualTo(ids[slot]));Assert.That(((Rigidbody)Read(trucks[slot],"body")).mass,Is.EqualTo(masses[slot]));}
        }
        Assert.That(frames.Any(value=>Mathf.Abs(value.bank)>.25f&&value.clock>1),Is.True,"Retain genuine animated bank rather than freezing the truck to world up.");
        for(int round=0;round<2;round++)Assert.That(frames.Any(value=>value.round==round&&value.carrierAnimationCallbacks>0&&value.callbackVelocity.magnitude>5&&value.rootIntegrationError<.05f&&value.rootRotationIntegrationError<.1f),Is.True,"Actual post-vendor fixed-animation callback must precede the observed same-step dynamic root integration in position and quaternion; no double velocity or guessed angular prediction.");
        if(initialHumanSources)for(int round=0;round<2;round++)for(int slot=0;slot<4;slot++)
        {
            var actual=frames.Where(value=>value.round==round&&value.slot==slot).ToArray();Assert.That(actual.Any(value=>value.clock<2&&value.actualBaseClips.IndexOf("Takeoff",StringComparison.OrdinalIgnoreCase)>=0),Is.True,"Observe each original native Takeoff, never force Fly to begin a proof.");Assert.That(actual.Any(value=>value.clock>2&&value.realCoreFly),Is.True,"Each legally assigned human mount must naturally enter actual Malbers core Fly during initial flight.");Assert.That(actual.Any(value=>Mathf.Abs(value.actualAssignedDrive.x)>.2f),Is.True,"Retain actual sampled assigned bank/steering input for every crew.");Assert.That(actual.All(value=>value.mounted),Is.True);
        }
        File.WriteAllText(Path.Combine(output,"evidence.json"),JsonUtility.ToJson(new Evidence{scope=ProofScope,frames=frames,bodyPixels=pixels.ToArray()},true));
    }
    void CaptureFailedObservation(int round)
    {
        // Run only after a strict assertion has already failed. Capture real
        // saved-scene cameras/bodies without changing their pose or replacing
        // the original failure if the evidence camera is not yet available.
        try
        {
            using var visibility=new DragonRiderMountedVisualObservation(match,output,"round-"+round+"-first-failure");
            Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(value=>value.Name=="Save"&&value.GetParameters().Length==5).Invoke(null,new object[]{match,1280,720,Path.Combine(output,"round-"+round+"-first-failure.png"),null});
        }
        catch(Exception evidenceError){TestContext.WriteLine("Read-only failed-frame capture unavailable: "+evidenceError.GetBaseException().Message);}
    }
    void Observe(Component racer,int round)
    {
        var actor=(Component)Read(racer,"Dragon");var frame=actor.GetComponent(Runtime("LWS.TruckDragonRiders.DragonRiderSaddle"));Assert.That(frame,Is.Not.Null,"Explicit saved saddle upgrade required.");
        var motor=(Component)Read(actor,"motor");var body=(Rigidbody)Read(Read(racer,"truck"),"body");var bone=(Transform)Read(frame,"sourceBone");var socket=(Transform)Read(frame,"socket");var shape=(BoxCollider)Read(Read(racer,"truck"),"shape");var pose=(Pose)Call(frame,"ReadPose");
        var value=new Frame{round=round,slot=(int)Read(racer,"slot"),clock=(float)Read(match,"Clock"),mounted=(bool)Read(racer,"PhysicallyMounted"),bank=(float)Read(frame,"CurrentBank"),pitch=(float)Read(frame,"CurrentPitch"),species=(string)Read(frame,"speciesId"),truckPosition=body.position,truckEuler=body.rotation.eulerAngles,socketPosition=socket.position,bodyAboveSpine=Vector3.Dot(shape.bounds.center-bone.position,pose.rotation*Vector3.up),lowestShapeAboveSeat=float.MaxValue};
        var animator=(Animator)Read(motor,"animator");value.animatorEnabled=animator.enabled;value.motorPaused=(bool)Read(motor,"Paused");value.matchSeed=(int)Read(match,"MatchSeed");value.realCoreFly=(bool)Read(motor,"IsFlying");value.actualAssignedDrive=(Vector2)Call(match,"ReadDrive",value.slot);
        var baseState=animator.GetCurrentAnimatorStateInfo(0);value.actualBaseNormalizedTime=baseState.normalizedTime;value.actualBaseStateLength=baseState.length;value.actualBaseTransition=animator.IsInTransition(0);value.actualNextBaseNormalizedTime=animator.GetNextAnimatorStateInfo(0).normalizedTime;
        value.actualAnimatorFloatParameters=string.Join(" | ",animator.parameters.Where(parameter=>parameter.type==AnimatorControllerParameterType.Float).Select(parameter=>parameter.name+"="+animator.GetFloat(parameter.name).ToString("R",System.Globalization.CultureInfo.InvariantCulture)));
        string Clips(int layer)=>string.Join(" | ",animator.GetCurrentAnimatorClipInfo(layer).Select(clip=>UnityEditor.AssetDatabase.GetAssetPath(clip.clip)+"/"+clip.clip.name+"@"+clip.weight.ToString("F5")));
        value.actualBaseClips=Clips(0);value.actualActionClips=animator.layerCount>1?Clips(1):string.Empty;
        value.phase=Read(racer,"Phase").ToString();value.notice=(string)Read(racer,"Notice");value.dislodgement=(string)Read(racer,"LastDislodgementReason");value.dislodgedAt=(float)Read(racer,"LastDislodgementAt");value.collision=(string)Read(racer,"LastCollisionCollider");value.collisionNormalSpeed=(float)Read(racer,"LastCollisionNormalSpeed");value.motorBodyPosition=((Rigidbody)Read(motor,"body")).position;value.motorTransformPosition=motor.transform.position;value.initialPosition=((Pose)Read(racer,"initialPose")).position;
        value.carrierAnimationCallbacks=(int)Read(racer,"CarrierAnimationCallbacks");value.callbackFixedTime=(float)Read(racer,"CarrierAnimationFixedTime");value.observationFixedTime=Time.fixedTime;value.callbackRootBefore=(Vector3)Read(racer,"CarrierRootBeforeIntegration");value.callbackTransformBefore=(Vector3)Read(racer,"CarrierTransformBeforeIntegration");value.callbackVelocity=(Vector3)Read(racer,"CarrierVelocityBeforeIntegration");value.callbackPendingTranslation=(Vector3)Read(racer,"CarrierPendingTranslation");value.callbackEvaluatedSocket=((Pose)Read(racer,"CarrierEvaluatedSocket")).position;var commanded=(Pose)Read(racer,"CarrierCommandedPose");value.callbackCommandedPosition=commanded.position;value.callbackCommandedEuler=commanded.rotation.eulerAngles;value.rootIntegrationError=value.carrierAnimationCallbacks>0?Vector3.Distance(value.motorBodyPosition,value.callbackRootBefore+value.callbackVelocity*Time.fixedDeltaTime):float.MaxValue;
        var physicalRoot=(Rigidbody)Read(motor,"body");value.motorBodyEuler=physicalRoot.rotation.eulerAngles;value.motorTransformEuler=motor.transform.eulerAngles;var callbackBodyRotation=(Quaternion)Read(racer,"CarrierBodyRotationBeforeIntegration");value.callbackBodyEuler=callbackBodyRotation.eulerAngles;value.callbackTransformEuler=((Quaternion)Read(racer,"CarrierTransformRotationBeforeIntegration")).eulerAngles;value.callbackFrameRotationCorrection=((Quaternion)Read(racer,"CarrierFrameRotationCorrection")).eulerAngles;value.rootRotationIntegrationError=value.carrierAnimationCallbacks>0?Quaternion.Angle(physicalRoot.rotation,(Quaternion)Read(racer,"CarrierTransformRotationBeforeIntegration")):float.MaxValue;
        var last=(Pose)Read(racer,"lastCarrierPose");var previous=(Pose)Read(racer,"previousCarrierPose");value.lastCarrierPosition=last.position;value.previousCarrierPosition=previous.position;
        var binding=Read(racer,"carry");
        if(binding!=null)value.assembly=((Array)Read(binding,"bodies")).Cast<object>().Select(item=>{var actual=(Rigidbody)Read(item,"body");var offset=(Vector3)Read(item,"offset");return new BodyFrame{name=actual.name,kinematic=actual.isKinematic,position=actual.position,velocity=actual.linearVelocity,lastCarrierError=Vector3.Distance(actual.position,last.position+last.rotation*offset),previousCarrierError=Vector3.Distance(actual.position,previous.position+previous.rotation*offset)};}).ToArray();
        var relative=(Quaternion.Inverse(motor.transform.rotation)*body.rotation).eulerAngles;Assert.That(Mathf.Abs(Mathf.DeltaAngle(0,relative.z)),Is.LessThanOrEqualTo((float)Read(frame,"maximumBank")+2));Assert.That(Mathf.Abs(Mathf.DeltaAngle(0,relative.x)),Is.LessThanOrEqualTo((float)Read(frame,"maximumPitch")+2));
        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2){var corner=shape.transform.TransformPoint(shape.center+Vector3.Scale(shape.size*.5f,new Vector3(x,y,z)));value.lowestShapeAboveSeat=Mathf.Min(value.lowestShapeAboveSeat,Vector3.Dot(corner-pose.position,pose.rotation*Vector3.up));}
        value.physicalSaddlePosition=pose.position;value.minimumActualSkinGap=float.MaxValue;
        var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);var oracle=author.GetMethod("TryMeasureLiveDorsalSupport");
        var skinWitness=new List<SkinWitness>();var physicalShape=Matrix4x4.TRS(body.position,body.rotation,body.transform.lossyScale)*body.transform.worldToLocalMatrix*shape.transform.localToWorldMatrix;
        Vector3 rootBefore=physicalRoot.position,scaleBefore=motor.transform.lossyScale;Quaternion boneBefore=bone.localRotation;
        for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
        {
            Vector3 bottom=physicalShape.MultiplyPoint3x4(shape.center+Vector3.Scale(shape.size*.5f,new Vector3(x,-1,z)));
            var args=new object[]{motor.transform,bone,bottom,Vector3.zero};bool found=(bool)oracle.Invoke(null,args);var skinPoint=(Vector3)args[3];
            var witness=new SkinWitness{physicalBottomPoint=bottom,actualTorso=found,actualDorsalPoint=skinPoint,verticalGap=found?Vector3.Dot(bottom-skinPoint,motor.transform.up):float.MaxValue};skinWitness.Add(witness);
            if(found&&witness.verticalGap<-.15f)witness.actualNegativeSkinProofJson=(string)author.GetMethod("ExplainActualLiveDorsalSupport").Invoke(null,new object[]{motor.transform,bone,bottom});
            if(found)value.minimumActualSkinGap=Mathf.Min(value.minimumActualSkinGap,witness.verticalGap);
            if(x==0&&z==0){value.actualCenterTorso=found;value.cabCenterRayPlatformDelta=found?Vector3.Dot(pose.position-skinPoint,motor.transform.up):float.MaxValue;}
        }
        // The raised cab's underside and its bounded support-plane up are not
        // collinear with motor-up on a pitched rig. A skin hit under that cab
        // point therefore cannot measure the separate platform's direct gap.
        // Independently cast under the platform's own actual position instead.
        var platformArgs=new object[]{motor.transform,bone,pose.position,Vector3.zero};
        value.actualPlatformTorso=(bool)oracle.Invoke(null,platformArgs);value.actualPlatformRaySkinPoint=(Vector3)platformArgs[3];
        value.skinPlatformGap=value.actualPlatformTorso?Vector3.Dot(pose.position-value.actualPlatformRaySkinPoint,motor.transform.up):float.MaxValue;
        value.actualAnatomicalAnchor=(Vector3)Read(frame,"ActualSurfacePoint");value.actualPatchNormal=(Vector3)Read(frame,"ActualPatchNormal");
        string seatingStage=value.slot==0?(value.clock<.05f?"staged":value.clock>=3?"flight":null):null;
        if(seatingStage!=null&&observedSeating.Add(round+":"+seatingStage))value.actualWheelDeckSeatingJson=(string)author.GetMethod("ObserveActualTruckDeckSeating").Invoke(null,new object[]{frame,Read(racer,"truck")});
        value.actualSkin=skinWitness.ToArray();value.maximumMeasuredPlatformGap=(float)Read(frame,"dorsalClearance")+.45f;
        Assert.That(Read(racer,"FinalAttachmentFrame"),Is.EqualTo(Time.frameCount),"Read the actual post-physics pre-camera attachment phase of this frame, rather than an older physics/bone pose.");
        Assert.That(Vector3.Distance(pose.position,(Vector3)Read(racer,"FinalAttachmentSkinPoint")),Is.EqualTo((float)Read(frame,"dorsalClearance")).Within(.0001f),"Physical platform follows the actual deformed anatomical material point at the original .45m offset, not a whole-animation maximum envelope.");
        Assert.That(physicalRoot.position,Is.EqualTo(rootBefore));Assert.That(motor.transform.lossyScale,Is.EqualTo(scaleBefore));Assert.That(bone.localRotation,Is.EqualTo(boneBefore),"Independent live BakeMesh oracle must not move or rescale the actual rig.");
        frames.Add(value);File.WriteAllText(Path.Combine(output,"evidence.json"),JsonUtility.ToJson(new Evidence{scope=ProofScope,frames=frames},true));Assert.That(value.mounted,Is.True,"Full original assembly remains physically mounted.");Assert.That(value.bodyAboveSpine,Is.GreaterThan(.5f),"Physical truck center must sit above the actual dorsal bone.");Assert.That(value.lowestShapeAboveSeat,Is.GreaterThanOrEqualTo(-.15f),"The original physical cab cannot penetrate its semantic dorsal seat.");
        Assert.That(value.actualCenterTorso,Is.True,"Independent actual final-pose dorsal skin must support the original truck footprint; no cached platform/reference point supplies this witness.");Assert.That(value.minimumActualSkinGap,Is.GreaterThanOrEqualTo(-.15f),"Original physical truck underside cannot penetrate independent actual final-pose torso skin.");Assert.That(value.actualPlatformTorso,Is.True,"Independently find actual final-pose torso skin under the platform itself.");Assert.That(value.skinPlatformGap,Is.GreaterThanOrEqualTo((float)Read(frame,"dorsalClearance")-.15f));Assert.That(value.skinPlatformGap,Is.LessThanOrEqualTo(value.maximumMeasuredPlatformGap),"Bound the co-located platform-to-actual-skin gap; the separate raised-cab ray is retained only as a named diagnostic.");
        foreach(var renderer in ((Transform)Read(frame,"supportPresentation")).GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials)
        {
            Assert.That(material,Is.Not.Null);Assert.That(material.shader,Is.Not.Null);Assert.That(material.GetTag("RenderPipeline",false),Is.EqualTo("UniversalPipeline"),"Validate the actual active timber subshader pipeline, including the established project-owned snow adaptation; a shader-name prefix is not a pipeline contract.");Assert.That(material.shader.isSupported,Is.True,"Actual active timber shader must be supported on this native rendering device.");Assert.That(UnityEditor.ShaderUtil.ShaderHasError(material.shader),Is.False,"An unsupported/error shader cannot pass the timber presentation gate.");
        }
    }
    int Capture(int round,Component truck)
    {
        Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(value=>value.Name=="Save"&&value.GetParameters().Length==5).Invoke(null,new object[]{match,1280,720,Path.Combine(output,"round-"+round+"-actual.png"),null});
        var host=Read(truck,"appearanceHost");Assert.That(host,Is.Not.Null);var renderers=((IEnumerable)Read(host,"ComposedBodyRenderers")).Cast<Renderer>().Where(value=>value!=null&&value.enabled&&value.gameObject.activeInHierarchy&&!value.forceRenderingOff).Distinct().ToArray();Assert.That(renderers,Is.Not.Empty,"Original actual customized tractor renderers required.");
        var camera=((Camera[])Read(match,"cameras")).Single();var target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);var prior=RenderTexture.active;var priorTarget=camera.targetTexture;var materials=renderers.Select(value=>value.sharedMaterials).ToArray();var shader=Shader.Find("Universal Render Pipeline/Unlit");Assert.That(shader,Is.Not.Null);var sentinel=new Material(shader);sentinel.SetColor("_BaseColor",Color.green);Texture2D original=null,marked=null;
        var body=(Rigidbody)Read(truck,"body");var position=body.position;var rotation=body.rotation;float clock=(float)Read(match,"Clock");
        try
        {
            camera.targetTexture=target;original=Render(camera,target);foreach(var renderer in renderers)renderer.sharedMaterials=Enumerable.Repeat(sentinel,renderer.sharedMaterials.Length).ToArray();marked=Render(camera,target);File.WriteAllBytes(Path.Combine(output,"round-"+round+"-actual-body-pixel-witness.png"),marked.EncodeToPNG());
            var a=original.GetPixels32();var b=marked.GetPixels32();int count=0;for(int i=0;i<b.Length;i++)if(b[i].g>180&&b[i].g>b[i].r*1.4f&&b[i].g>b[i].b*1.4f&&Mathf.Abs(a[i].r-b[i].r)+Mathf.Abs(a[i].g-b[i].g)+Mathf.Abs(a[i].b-b[i].b)>30)count++;
            Assert.That(count,Is.GreaterThanOrEqualTo(256),"Actual depth-tested original cab/body pixels must be recognizable above the real dragon, not merely present in a renderer list.");Assert.That(body.position,Is.EqualTo(position));Assert.That(body.rotation,Is.EqualTo(rotation));Assert.That(Read(match,"Clock"),Is.EqualTo(clock));return count;
        }
        finally{for(int i=0;i<renderers.Length;i++)renderers[i].sharedMaterials=materials[i];camera.targetTexture=priorTarget;RenderTexture.active=prior;if(original!=null)UnityEngine.Object.Destroy(original);if(marked!=null)UnityEngine.Object.Destroy(marked);UnityEngine.Object.Destroy(sentinel);UnityEngine.Object.Destroy(target);}
    }
    static Texture2D Render(Camera camera,RenderTexture target){camera.Render();RenderTexture.active=target;var texture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();return texture;}
    [UnityTearDown]public IEnumerator Cleanup()
    {if(observer!=null){observer.Observe=null;UnityEngine.Object.Destroy(observer.gameObject);observer=null;}if(match!=null)Call(match,"SetupScreen");if(startupKeyboard!=null&&startupKeyboard.added)InputSystem.RemoveDevice(startupKeyboard);startupKeyboard=null;for(int i=0;i<startupPads.Length;i++){if(startupPads[i]!=null&&startupPads[i].added)InputSystem.RemoveDevice(startupPads[i]);startupPads[i]=null;}if(originalInput!=null)InputSystem.settings=originalInput;originalInput=null;if(temporaryInput!=null)UnityEngine.Object.DestroyImmediate(temporaryInput);temporaryInput=null;if(inputIsolation!=null)UnityEngine.Object.DestroyImmediate(inputIsolation);inputIsolation=null;var old=SceneManager.GetActiveScene();var empty=SceneManager.CreateScene("Riders saddle native cleanup");SceneManager.SetActiveScene(empty);if(old.IsValid()&&old.isLoaded)yield return SceneManager.UnloadSceneAsync(old);lease?.Dispose();Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);}
}
#endif
