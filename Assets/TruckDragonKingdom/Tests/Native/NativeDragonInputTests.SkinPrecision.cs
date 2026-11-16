#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed partial class NativeDragonInputTests
{
    [Serializable]sealed class PrecisionResolvedSources{public QualifiedEightFile[] rows;}
    [Serializable]sealed class PrecisionHudCapture{public string scope="EXISTING_CONTROLLED_CAPTURE_PUBLIC_PAUSE_SAME_NATURAL_POSE_NOT_WINDOWS_OR_HUMAN_ACCEPTANCE",image,beforePose,afterPose;public bool physicalPoseExact;public int width,height,viewMode;public float clock;public string[] actualRiderPhases;}
    string PrecisionHudPose(object observer)
    {
        string state=(string)observer.GetType().GetMethod("SnapshotOwnedState",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(observer,null);
        return string.Join("\n",state.Split('\n').Select(line=>
        {
            if(line.StartsWith("frame=",StringComparison.Ordinal))return line.Substring(line.IndexOf(" time=",StringComparison.Ordinal));
            if(!line.StartsWith("T:",StringComparison.Ordinal))return line;
            int end=line.IndexOf(':',2);var fields=line.Substring(2,end-2).Split('/');Assert.That(fields.Length,Is.EqualTo(3));
            // Notification only; every actual local/world pose and all native
            // body/collider/Animator/global-physics fields remain authoritative.
            return "T:"+fields[0]+"/"+fields[1]+":"+line.Substring(end+1);
        }));
    }
    IEnumerator CapturePrecisionHudFromActualNaturalPose(object observer)
    {
        Neutral();yield return null;Call(match,"Pause",0,false);Call(Read(match,"menus"),"Show",Enum.Parse(Runtime("LWS.TruckSports.SportsEventScreen"),"Play"));yield return null;yield return null;
        string initial=PrecisionHudPose(observer),folder=Path.Combine(output,"SkinPrecision","Hud");Directory.CreateDirectory(folder);
        try
        {
            foreach(int cameraMode in new[]{1,2})
            {
                Call(views,"Select",cameraMode);yield return null;yield return null;
                foreach(int height in new[]{720,1080})
                {
                    int width=height==720?1280:1920;string file=Path.Combine(folder,"four-human-view"+cameraMode+"-"+width+"x"+height+".png");
                    Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==5).Invoke(null,new object[]{match,width,height,file,new Action<Camera>(ui=>InspectRiderViewportHud(ui,width,height))});
                    string after=PrecisionHudPose(observer);var proof=new PrecisionHudCapture{image=file,beforePose=initial,afterPose=after,physicalPoseExact=initial==after,width=width,height=height,viewMode=cameraMode,clock=(float)Read(match,"Clock"),actualRiderPhases=Enumerable.Range(0,4).Select(QualifiedEightRiderPhase).ToArray()};File.WriteAllText(Path.ChangeExtension(file,".capture.json"),JsonUtility.ToJson(proof,true));
                    Assert.That(proof.physicalPoseExact,Is.True,"Only camera/UI mode may change during the public-paused natural-pose screenshots.");
                }
            }
        }
        finally{Call(match,"Resume");}
    }
    IEnumerator PrepareReadOnlyRiderCabAndHudScene()
    {
        qualifiedEightSavedScene=true;qualifiedEightBodyComplete=false;mode="TruckDragonRiders";riders=true;reportName="TruckDragonRiders-SkinPrecision";report.Clear();
        Assert.That(Application.isPlaying,Is.True);output=Path.Combine(QualifiedEightPhysicalPath(QualifiedEightArgument("-tsDragonOutput")),"InputOwnership");Directory.CreateDirectory(output);
        string path=RequireQualifiedEightSavedScene();var captured=new System.Collections.Generic.List<QualifiedEightFile>();foreach(var source in qualifiedEightSourceBytes)captured.Add(new QualifiedEightFile{path=source.Key,sha256=source.Value});File.WriteAllText(Path.Combine(output,"SkinPrecision.resolved-sources.json"),JsonUtility.ToJson(new PrecisionResolvedSources{rows=captured.ToArray()},true));
        preferences=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"RiderNativeSkinPrecision"});Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
        isolation=new GameObject("Read-only skin precision software input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));
        original=UnityEngine.InputSystem.InputSystem.settings;temporary=UnityEngine.Object.Instantiate(original);temporary.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;temporary.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;UnityEngine.InputSystem.InputSystem.settings=temporary;
        keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();UnityEngine.InputSystem.InputSystem.EnableDevice(keyboard);keyboard.MakeCurrent();for(int i=0;i<4;i++){pads[i]=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();UnityEngine.InputSystem.InputSystem.EnableDevice(pads[i]);}
        yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path,new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));yield return null;yield return null;
        match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("LWS.TruckDragonRiders.DragonRidersMatch"));Assert.That(match,Is.Not.Null);players=(Array)Read(Read(match,"setup"),"players");trucks=(Array)Read(match,"trucks");views=Read(match,"drivingViews");
        Neutral();yield return null;Configure(4);Runtime("LWS.TruckSports.SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{false});Call(views,"Select",2);Assert.That(Call(match,"StartMatch"),Is.EqualTo(true));
        for(int slot=0;slot<4;slot++)Assert.That(Call(match,"ConfigureAuthoredSourceCabMountCandidate",slot,slot,true),Is.EqualTo(true),"Each actual human cab uses the existing authored source cockpit eye.");
        float deadline=Time.realtimeSinceStartup+35;while((float)Read(match,"Countdown")>0&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That((float)Read(match,"Countdown"),Is.Zero);
    }
    [UnityTest,Timeout(600000),QualifiedEightSourceAfterCleanup]
    public IEnumerator RidersAuthoredCabAndCornerHudRenderFourOwnersWithoutChangingPhysicalState()
    {
        // This independent visual/ownership witness does not claim that the
        // separately failing submillimeter skin predictor has been repaired.
        yield return PrepareReadOnlyRiderCabAndHudScene();
        reportName="TruckDragonRiders-AuthoredCabHud";
        for(int slot=0;slot<4;slot++)
        {
            yield return AwaitQualifiedEightMountedInputWindow(slot);Neutral();yield return null;
            float sign=QualifiedEightOutwardSteering(slot);PulseQualifiedEightDrive(slot,sign);yield return null;yield return null;AssertQualifiedEightExclusiveDrive(slot,sign);
            for(int tick=0;tick<6;tick++)yield return new WaitForFixedUpdate();Neutral();yield return null;
        }
        Directory.CreateDirectory(Path.Combine(output,"SkinPrecision"));
        var type=Type.GetType("DragonRiderCabSightlineObservation, LWS.TruckDragonRiders.NativeTests",true);
        var observer=Activator.CreateInstance(type,match,Path.Combine(output,"SkinPrecision","independent-hud-observer.json"));
        ((IDisposable)observer).Dispose();
        yield return CapturePrecisionHudFromActualNaturalPose(observer);
        Assert.That(Enumerable.Range(0,4).All(slot=>QualifiedEightRiderPhase(slot)=="Mounted"),Is.True,"Capture actual mounted participants, not an empty or detached cab.");
        qualifiedEightBodyComplete=true;Log("ACTUAL_AUTHORED_CAB_HUD fourSoftwareOwners=true nativeSourceClosure=true chaseCab7201080=true pausedPhysicalStateExact=true NOT_NUMERIC_SKIN_OR_WINDOWS_OR_HUMAN_ACCEPTANCE");
    }
    [UnityTest,Timeout(600000),QualifiedEightSourceAfterCleanup]
    public IEnumerator RidersRealFarWorldFramesCompareNativeBakeWithUnchangedFloatAndDoubleSkinning()
    {
        yield return PrepareReadOnlyRiderCabAndHudScene();
        var type=Type.GetType("DragonRiderSkinPrecisionObservation, LWS.TruckDragonRiders.NativeTests",true);var observation=Activator.CreateInstance(type,match,Path.Combine(output,"SkinPrecision"));
        try
        {
            float deadline=Time.realtimeSinceStartup+90;
            while(!(bool)Read(observation,"Ready")&&Time.realtimeSinceStartup<deadline)
            {
                foreach(int slot in new[]{0,1,2,3})
                {
                    yield return AwaitQualifiedEightMountedInputWindow(slot);Neutral();yield return null;float sign=QualifiedEightOutwardSteering(slot);PulseQualifiedEightDrive(slot,sign);yield return null;yield return null;AssertQualifiedEightExclusiveDrive(slot,sign);
                    float end=Time.time+.8f;while(Time.time<end){Assert.That(Read(match,"Paused"),Is.EqualTo(false));yield return new WaitForFixedUpdate();}Neutral();yield return null;
                    // Batch PlayMode does not necessarily render a Game view.
                    // Use the existing actual-camera capture path while the
                    // public match remains live at its naturally reached pose.
                    if((float)Read(match,"Clock")>=10)
                    {
                        string samples=Path.Combine(output,"SkinPrecision","NaturalFrames");Directory.CreateDirectory(samples);
                        Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==5).Invoke(null,new object[]{match,1280,720,Path.Combine(samples,"frame-"+Time.frameCount+".png"),null});
                    }
                    Assert.That(Read(observation,"Failure"),Is.Null.Or.Empty,"Retain a real numeric failure; never suppress its render callback.");if((bool)Read(observation,"Ready"))break;
                }
            }
            Call(observation,"RequirePositiveEvidence");((IDisposable)observation).Dispose();yield return CapturePrecisionHudFromActualNaturalPose(Read(observation,"original"));qualifiedEightBodyComplete=true;Log("STANDALONE_NATIVE_PRECISION captures="+Read(observation,"Count")+" bothSpecies=true originalObserverUnchanged=true actualNativeSatAnd25Rays=true oldFloatNegativeRetained=true newDoubleRmseBelowPoint1mm=true controlledHud7201080Saved=true NOT_PRODUCTION_OR_HUMAN_ACCEPTANCE");
        }
        finally{((IDisposable)observation).Dispose();}
    }
}
#endif
