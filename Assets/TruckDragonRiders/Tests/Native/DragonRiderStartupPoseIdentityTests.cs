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
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class DragonRiderStartupPoseIdentityTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    const string ScenePath="Assets/TruckDragonRiders/Scenes/TruckDragonRiders.unity";
    Component match;IDisposable preferences;string output,requested;bool observed;Exception observationFailure;byte[] originalScene;
    int allPipelineCallbacks,ownedPipelineCallbacks,controlledRenders;
    int currentRound,boundaryCallbacks;EventInfo graphEvent;Delegate graphHandler;readonly Dictionary<int,Pose> beforeGraph=new Dictionary<int,Pose>();readonly Dictionary<string,Choice> firstDefaults=new Dictionary<string,Choice>();
#pragma warning disable CS0649
    [Serializable]sealed class Pose{public Choice[] choices;public float countdown;public Body[] actualOriginalNwhBodies;}
    [Serializable]sealed class Choice{public int slot;public string species,phase,controller,controllerName;public bool active,prepared,physicallyMounted,inTimeline;public int nativeInputSourceId;public string nativeInputSourceType;public Vector3 motorPosition,motorScale;public Quaternion motorRotation;public Bone[] actualBoneLocals;public Leaf[] actualLeaves;}
    [Serializable]sealed class Bone{public string path;public bool sourceSkinBone;public Vector3 localPosition,localScale;public Quaternion localRotation;}
    [Serializable]sealed class Body{public int owner,entityId;public string path;public Vector3 position,localScale;public Quaternion rotation;public float mass;}
    [Serializable]sealed class Leaf{public string path,guid;public long fileId;}
#pragma warning restore CS0649
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static object Read(object value,string name)=>value.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(value):value.GetType().GetProperty(name,Any).GetValue(value);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(value,args);
    Pose Save(string stage,Camera camera)
    {
        var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);string json=(string)author.GetMethod("ObserveActualRiderStartupPoseIdentity").Invoke(null,new object[]{match,stage,camera});File.WriteAllText(Path.Combine(output,stage+".json"),json);return JsonUtility.FromJson<Pose>(json);
    }
    void GraphBoundary<T>(T racer,string phase)
    {
        if(observationFailure!=null)return;
        try
        {
            int slot=(int)Read(racer,"slot");var snapshot=Save("public-start-"+currentRound+"-slot-"+slot+"-"+phase,null);if(phase!="before-configured-default-evaluation"&&phase!="after-configured-default-evaluation")return;boundaryCallbacks++;
            if(phase.StartsWith("before-",StringComparison.Ordinal)){beforeGraph[slot]=snapshot;return;}
            var prior=beforeGraph[slot];Assert.That(snapshot.countdown,Is.EqualTo(prior.countdown));var before=prior.choices.Single(choice=>choice.slot==slot&&choice.active);var after=snapshot.choices.Single(choice=>choice.slot==slot&&choice.active);
            Assert.That(after.inTimeline,Is.EqualTo(before.inTimeline));Assert.That(after.nativeInputSourceId,Is.EqualTo(before.nativeInputSourceId));Assert.That(after.nativeInputSourceType,Is.EqualTo(before.nativeInputSourceType));Assert.That(after.controller,Is.EqualTo(before.controller));Assert.That(after.controllerName,Is.EqualTo(before.controllerName));Assert.That(Vector3.Distance(after.motorPosition,before.motorPosition),Is.LessThan(.001f));Assert.That(Quaternion.Angle(after.motorRotation,before.motorRotation),Is.LessThan(.01f));Assert.That(Vector3.Distance(after.motorScale,before.motorScale),Is.LessThan(.000001f));
            Assert.That(snapshot.actualOriginalNwhBodies.Length,Is.EqualTo(prior.actualOriginalNwhBodies.Length));foreach(var body in snapshot.actualOriginalNwhBodies){var original=prior.actualOriginalNwhBodies.Single(value=>value.entityId==body.entityId);Assert.That(body.mass,Is.EqualTo(original.mass));Assert.That(body.localScale,Is.EqualTo(original.localScale));Assert.That(Vector3.Distance(body.position,original.position),Is.LessThan(.00001f));Assert.That(Quaternion.Angle(body.rotation,original.rotation),Is.LessThan(.001f));}
            string key=slot+"/"+after.species;
            if(currentRound==0)firstDefaults[key]=after;
            else
            {
                var first=firstDefaults[key];var firstBones=first.actualBoneLocals.Where(bone=>bone.sourceSkinBone).ToDictionary(bone=>bone.path.Split('/').Last());var secondBones=after.actualBoneLocals.Where(bone=>bone.sourceSkinBone).ToArray();Assert.That(secondBones.Length,Is.EqualTo(firstBones.Count));
                foreach(var bone in secondBones){var original=firstBones[bone.path.Split('/').Last()];Assert.That(Vector3.Distance(bone.localPosition,original.localPosition),Is.LessThan(.00001f));Assert.That(Vector3.Distance(bone.localScale,original.localScale),Is.LessThan(.000001f));Assert.That(Quaternion.Angle(bone.localRotation,original.localRotation),Is.LessThan(.01f),bone.path+" genuine configured default must be repeatable across public prepares.");}
            }
        }
        catch(Exception error){observationFailure=error;}
    }
    void Rendering(ScriptableRenderContext context,Camera camera)
    {
        allPipelineCallbacks++;if(match==null||!((Camera[])Read(match,"cameras")).Contains(camera))return;ownedPipelineCallbacks++;if(observed||requested==null)return;
        try
        {
            var pose=Save(requested,camera);Assert.That(pose.choices.Length,Is.EqualTo(8));
            if(!requested.StartsWith("menu",StringComparison.Ordinal))
            {
                var active=pose.choices.Where(choice=>choice.active).ToArray();Assert.That(active.Length,Is.EqualTo(4));Assert.That(active.All(choice=>choice.prepared&&choice.physicallyMounted&&choice.actualBoneLocals.Length>20),Is.True,"The actual visible public-start participant must be prepared and physically mounted; no substituted pose is sampled by this observer.");
            }
            observed=true;
        }
        catch(Exception error){observationFailure=error;}
    }
    IEnumerator AwaitOwnedRender(string stage)
    {
        requested=stage;observed=false;observationFailure=null;int callbacksBefore=allPipelineCallbacks,ownedBefore=ownedPipelineCallbacks;
        // Batch Editor need not render a natural GameView. First let ordinary
        // Update/Animator/final attachment run; if no owned render occurs,
        // retain complete camera/URP context and request the existing actual
        // controlled render. No camera or participant pose is supplied.
        yield return null;yield return null;
        if(!observed&&observationFailure==null)
        {
            Save(stage+"-camera-context-before-controlled-render",null);controlledRenders++;TestContext.WriteLine("STARTUP_NATIVE_RENDER_CONTEXT stage="+stage+" naturalPipelineCallbacks="+(allPipelineCallbacks-callbacksBefore)+" naturalOwnedCallbacks="+(ownedPipelineCallbacks-ownedBefore)+" batch="+Application.isBatchMode+" graphics="+SystemInfo.graphicsDeviceType+" controlledOwnedRenderRequested=true NOT_NATURAL_GAMEVIEW_FIRST_FRAME_ACCEPTANCE");
            Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==5).Invoke(null,new object[]{match,1280,720,Path.Combine(output,stage+"-controlled-native-render.png"),null});
        }
        requested=null;if(observationFailure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(observationFailure).Throw();Assert.That(observed,Is.True,"The original owned game camera must actually render; no synthetic camera pose is supplied.");
    }
    [UnityTest,Timeout(120000)]
    public IEnumerator ActualPublicStartsRecordSerializedMenuFirstVisibleCountdownGoAndRematchSourcePoseIdentity()
    {
        Assert.That(Application.isPlaying,Is.True);originalScene=File.ReadAllBytes(ScenePath);preferences=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"RiderStartupIdentity"+Guid.NewGuid().ToString("N")});Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");Assert.That(at>=0&&at+1<args.Length,Is.True);output=Path.Combine(args[at+1],"RiderSaddleAuthoring","StartupIdentity");Directory.CreateDirectory(output);RenderPipelineManager.beginCameraRendering+=Rendering;
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath,new LoadSceneParameters(LoadSceneMode.Single));match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("LWS.TruckDragonRiders.DragonRidersMatch"));Assert.That(match,Is.Not.Null);Save("menu-after-scene-load-before-public-start",null);yield return null;yield return null;Save("menu-after-two-natural-frames",null);
        Read(match,"setup").GetType().GetField("count").SetValue(Read(match,"setup"),4);Call(match,"AllBots");
        var racerType=Runtime("LWS.TruckDragonRiders.DragonRiderRacer");graphEvent=racerType.GetEvent("PrepareGraphObservation",BindingFlags.Public|BindingFlags.Static);Assert.That(graphEvent,Is.Not.Null);graphHandler=Delegate.CreateDelegate(graphEvent.EventHandlerType,this,GetType().GetMethod(nameof(GraphBoundary),BindingFlags.Instance|BindingFlags.NonPublic).MakeGenericMethod(racerType));graphEvent.AddEventHandler(null,graphHandler);
        for(int round=0;round<2;round++)
        {
            currentRound=round;observationFailure=null;string prefix="public-start-"+round+"-";Assert.That(Call(match,"StartMatch"),Is.EqualTo(true));if(observationFailure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(observationFailure).Throw();Save(prefix+"synchronous-after-Prepare-before-yield",null);
            yield return AwaitOwnedRender(prefix+"first-visible-countdown-render");yield return AwaitOwnedRender(prefix+"next-natural-countdown-render");
            float deadline=Time.realtimeSinceStartup+35;while((float)Read(match,"Countdown")>0&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That((float)Read(match,"Countdown"),Is.Zero);yield return AwaitOwnedRender(prefix+"first-post-GO-render");
            Call(match,"SetupScreen");yield return null;
        }
        Assert.That(boundaryCallbacks,Is.EqualTo(16));Assert.That(File.ReadAllBytes(ScenePath),Is.EqualTo(originalScene));TestContext.WriteLine("ACTUAL_TWO_PUBLIC_START_SOURCE_POSE_IDENTITIES_RETAINED beforeAfterRealDefaultGraphEvaluation=16 incomingControllerRootAndOriginalNwhUnchanged=true sameActualSourceBoneDefaultAcrossRematch=true menu ownedCountdownRender firstGO actualClipGuidFileIds controlledRenders="+controlledRenders+" TEST_OBSERVER_NEVER_SAMPLES_OR_WRITES_POSE_NO_NATURAL_GAMEVIEW_OR_HUMAN_ACCEPTANCE");
    }
    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        RenderPipelineManager.beginCameraRendering-=Rendering;if(graphEvent!=null&&graphHandler!=null)graphEvent.RemoveEventHandler(null,graphHandler);graphEvent=null;graphHandler=null;requested=null;if(match!=null)Call(match,"SetupScreen");var scene=SceneManager.GetActiveScene();var empty=SceneManager.CreateScene("Rider startup identity cleanup");SceneManager.SetActiveScene(empty);if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);match=null;preferences?.Dispose();preferences=null;Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);if(originalScene!=null)Assert.That(File.ReadAllBytes(ScenePath),Is.EqualTo(originalScene));
    }
}
#endif
