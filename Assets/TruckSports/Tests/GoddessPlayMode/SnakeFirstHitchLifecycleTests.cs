#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class SnakeFirstHitchLifecycleTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
    static readonly Dictionary<object,Transform> Models=new Dictionary<object,Transform>();
    internal static float Inspect(object convoy)
    {
        Assert.That((int)Get(convoy,"ActiveCount"),Is.GreaterThanOrEqualTo(1));
        var hitch=(Transform)Get(convoy,"firstTractorAttachment");Assert.That(hitch,Is.Not.Null,"Saved scene uses its existing NWH fifth wheel.");
        if(!Models.TryGetValue(convoy,out var model)||model==null){var contact=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude).Single(component=>component.GetType().Name=="SnakeTrailerContact"&&(int)Get(component,"index")==0&&ReferenceEquals(Get(component,"convoy"),convoy));model=contact.transform.GetChild(0);Models[convoy]=model;}
        var body=model.parent.GetComponent<Rigidbody>();Vector3 pin=(Vector3)Get(convoy,"firstTrailerAttachment")-Vector3.up*1.6f;
        pin=new Vector3(pin.x/model.localScale.x,pin.y/model.localScale.y,pin.z/model.localScale.z);
        float gap=Vector3.Distance(model.TransformPoint(pin),hitch.position);
        Assert.That(gap,Is.LessThan(.015f),"Actual interpolated lead render model stays on the fifth wheel.");
        Assert.That(body.isKinematic,Is.True,"Preserve the existing stable collider-backed convoy authority.");return gap;
    }
    [UnityTest,Timeout(240000)]
    public IEnumerator SavedFourConvoysStayConnectedDuringRealDrivingJumpPauseAndRematches()
    {
        var preferences=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"SnakeFirstHitchActual"});
        var isolation=new GameObject("Explicit software-keyboard hitch driving fixture");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        var originalInputSettings=InputSystem.settings;var isolatedInputSettings=UnityEngine.Object.Instantiate(originalInputSettings);
        isolatedInputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        isolatedInputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings=isolatedInputSettings;
        Keyboard keyboard=null;Component match=null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSnake/Scenes/TruckSnake.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession"));keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();Models.Clear();var observer=isolation.AddComponent<SnakeFirstHitchFrameObserver>();
            var world=Get(match,"world");var starts=(Vector3[])Get(world,"starts");var rotations=(Quaternion[])Get(world,"rotations");
            // Explicit physical fixture: same verified real outer road used by
            // the existing convoy acceptance, no injected outcome or velocity.
            Vector3 road=new Vector3(-1120,0,-1000);road.y=(float)Call(world,"Height",road)+.5f;starts[0]=road;rotations[0]=Quaternion.Euler(0,90,0);
            var convoys=((Array)Get(match,"convoys")).Cast<object>().ToArray();var trucks=((Array)Get(match,"trucks")).Cast<Component>().ToArray();
            var presets=Get(match,"presets");var clear=((Array)Get(presets,"entries")).Cast<object>().Single(entry=>(string)Get(entry,"displayName")=="Clear");Call(presets,"Select",Get(clear,"id"));
            Runtime("SportsTimeOfDay").GetMethod("Select").Invoke(null,new[]{Enum.Parse(Runtime("SportsTimeChoice"),"Day")});
            var capture=Runtime("SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==5);
            foreach(int count in new[]{1,2,3,4})
            {
                Call(match,"SetupScreen");var setup=Get(match,"setup");Set(setup,"count",count);Call(match,"AllBots");
                object player=((Array)Get(setup,"players")).GetValue(0);Set(player,"source",Enum.Parse(Get(player,"source").GetType(),"Keyboard"));Set(player,"forward",Key.W);Set(player,"back",Key.S);Set(player,"left",Key.A);Set(player,"right",Key.D);((Key[])Get(match,"actionKeys"))[0]=Key.Space;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();Assert.That(Call(match,"StartMatch"),Is.True);Call(Get(match,"drivingViews"),"Select",0);
                observer.convoys=convoys.Take(count).ToArray();observer.maximumGap=0;observer.error=null;observer.samples=0;observer.match=match;observer.heldFrames=observer.actionFrames=0;
                var body=(Rigidbody)Get(trucks[0],"body");float begin=Time.time,maxGap=0,maxClearance=0,nextPress=0,releasePress=0;int attempts=0;bool jumpRequested=false,jumpReleased=false,capturedAir=false,paused=false;
                float deadline=Time.realtimeSinceStartup+28;
                while(Time.time-begin<15&&Time.realtimeSinceStartup<deadline)
                {
                    yield return null;
                    Assert.That(observer.error,Is.Null);maxGap=Mathf.Max(maxGap,observer.maximumGap);
                    float age=Time.time-begin;
                    // Test-runner input is pumped explicitly. Fresh bounded
                    // presses also tolerate losing wheel contact between the
                    // observed render frame and the next physics step, just as
                    // a human can release and press again; no jump is injected.
                    if(!jumpReleased&&((float[])Get(match,"jumpedAt"))[0]>begin){jumpReleased=true;InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();}
                    else if(!jumpReleased&&releasePress>0&&Time.time>=releasePress){InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();releasePress=0;nextPress=Time.time+.25f;}
                    else if(age>4&&!jumpReleased&&releasePress==0&&Time.time>=nextPress&&(bool)Get(trucks[0],"Grounded")){jumpRequested=true;attempts++;keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));InputSystem.Update();releasePress=Time.time+.1f;TestContext.WriteLine("HITCH_ACTION attempt="+attempts+" held="+keyboard.spaceKey.isPressed+" current="+(Keyboard.current==keyboard)+" updateMode="+InputSystem.settings.updateMode+" actualActionHeld="+Call(match,"ActionHeld",0)+" ready="+Call(match,"Ready",0)+" usesAction="+Get(match,"UsesAction")+" currentSource="+Get(((Array)Get(Get(match,"setup"),"players")).GetValue(0),"source")+" sameSlot="+ReferenceEquals(player,((Array)Get(Get(match,"setup"),"players")).GetValue(0))+" action="+((bool[])Get(match,"actions"))[0]);}
                    else if(jumpReleased){InputSystem.QueueStateEvent(keyboard,age>9&&age<13?new KeyboardState(Key.D):new KeyboardState());InputSystem.Update();}
                    float clearance=float.PositiveInfinity;
                    foreach(var collider in trucks[0].GetComponentsInChildren<Collider>(true))if(collider.enabled&&!collider.isTrigger)clearance=Mathf.Min(clearance,collider.bounds.min.y-(float)Call(world,"Height",collider.bounds.center));
                    if(!float.IsInfinity(clearance))maxClearance=Mathf.Max(maxClearance,clearance);
                    if(!capturedAir&&clearance>=6.096f){capturedAir=true;if(count==1||count==4){observer.capture=()=>{foreach(int height in new[]{720,1080})capture.Invoke(null,new object[]{match,height==720?1280:1920,height,"Logs/TruckSports/PlaytestRepair/SnakeFirstHitchNative/count-"+count+"-air-"+height+".png",null});};while(observer.capture!=null)yield return null;Assert.That(observer.error,Is.Null);}}
                    if(!paused&&age>7){paused=true;Call(match,"Pause",0,false);var position=body.position;yield return new WaitForSecondsRealtime(.15f);Assert.That(body.position,Is.EqualTo(position));Assert.That(observer.error,Is.Null);Call(match,"Resume");}
                }
                // Keep actual driving images even when a later acceptance
                // assertion fails; never hide the failed run's visual evidence.
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
                if(count==1||count==4){observer.capture=()=>{foreach(int height in new[]{720,1080})capture.Invoke(null,new object[]{match,height==720?1280:1920,height,"Logs/TruckSports/PlaytestRepair/SnakeFirstHitchNative/count-"+count+"-driven-"+height+".png",null});};while(observer.capture!=null)yield return null;Assert.That(observer.error,Is.Null);}
                TestContext.WriteLine("HITCH_JUMP_DIAGNOSTIC count="+count+" requested="+jumpRequested+" released="+jumpReleased+" capturedAir="+capturedAir+" physicalJumps="+Get(match,"PhysicalJumps")+" maxLowestClearance="+maxClearance+" anchorSamples="+observer.samples+" maximumGap="+observer.maximumGap+" heldFrames="+observer.heldFrames+" actionFrames="+observer.actionFrames+" age="+(Time.time-begin)+" body="+body.position+" grounded="+Get(trucks[0],"Grounded")+" running="+Get(match,"Running")+" finished="+Get(match,"Finished")+" source="+Get(player,"source")+" releasedInput="+Get(player,"released")+" actionKey="+((Key[])Get(match,"actionKeys"))[0]+" keyboard="+keyboard.enabled+" standings="+Get(match,"Standings"));
                Assert.That(observer.samples,Is.GreaterThan(10),"The real late-frame observer must actually inspect the interpolated models.");Assert.That(observer.error,Is.Null);
                Assert.That(jumpRequested&&jumpReleased&&capturedAir,Is.True,"Real assigned keyboard action preserves the existing twenty-foot physical jump.");
                Assert.That((body.position-road).magnitude,Is.GreaterThan(10),"Actual NWH propulsion, not pose injection, moves the tractor.");
                observer.convoys=null;Call(match,"SetupScreen");Assert.That(Call(match,"StartMatch"),Is.True);
                for(int slot=0;slot<count;slot++)Assert.That(Get(convoys[slot],"ActiveCount"),Is.EqualTo(1));
                observer.convoys=convoys.Take(count).ToArray();int samples=observer.samples;while(observer.samples==samples)yield return null;Assert.That(observer.error,Is.Null);
                TestContext.WriteLine("ACTUAL_FIRST_HITCH count="+count+" maximumRenderedGapMetres="+maxGap+" lowestTruckClearanceMetres="+maxClearance+"; real NWH/software keyboard jump/turn/pause/rematch. Controlled native images, no Windows/human/hardware/listening/performance claim.");
            }
        }
        finally{Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);InputSystem.settings=originalInputSettings;UnityEngine.Object.DestroyImmediate(isolatedInputSettings);UnityEngine.Object.DestroyImmediate(isolation);Models.Clear();preferences.Dispose();}
    }
}
[DefaultExecutionOrder(30000)]
public sealed class SnakeFirstHitchFrameObserver:MonoBehaviour
{
    public object[] convoys;public Component match;public float maximumGap;public string error;public int samples,heldFrames,actionFrames;public Action capture;
    void LateUpdate(){if(convoys==null)return;try{foreach(var convoy in convoys)maximumGap=Mathf.Max(maximumGap,SnakeFirstHitchLifecycleTests.Inspect(convoy));samples++;if(match!=null){if((bool)match.GetType().GetMethod("ActionHeld").Invoke(match,new object[]{0}))heldFrames++;if(((bool[])match.GetType().GetField("actions").GetValue(match))[0])actionFrames++;}var pending=capture;capture=null;pending?.Invoke();}catch(Exception failure){error=failure.ToString();capture=null;}}
}
#endif
