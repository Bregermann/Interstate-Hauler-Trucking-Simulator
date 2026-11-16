#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestTools;

public sealed partial class NativeDragonInputTests
{
    bool qualifiedEightSavedScene,qualifiedEightSourceCaptureComplete,qualifiedEightBodyComplete;
    int qualifiedEightOriginalSourceRows;string qualifiedEightSavedPath;
    [Serializable]sealed class QualifiedEightPostCleanupProof
    {
        public string scope="ACTUAL_CAPTURED_SOURCE_BYTES_AFTER_INHERITED_UNITY_TEARDOWN_NATIVE_GAMEPLAY_TEST_RESULT_REMAINS_SEPARATE",status;
        public bool captureComplete,cleanupBoundaryObserved,bodyCompleted;public int originalSourceRows,capturedFiles,checkedFiles,failures;public string[] differences;
    }
    // Installed UTF TestCommandBuilder wraps EnumerableSetUpTearDownCommand
    // inside OuterUnityTestActionCommand. This method-only action therefore
    // runs after the existing UnityTearDown without competing teardown order.
    [AttributeUsage(AttributeTargets.Method)]
    sealed class QualifiedEightSourceAfterCleanupAttribute:NUnitAttribute,IOuterUnityTestAction
    {
        public IEnumerator BeforeTest(ITest test){yield break;}
        public IEnumerator AfterTest(ITest test)
        {
            UnityEngine.Debug.Log("QUALIFIED_EIGHT_OUTER_AFTER_BEGIN id="+test.Id+" name="+test.FullName);NativeDragonInputTests fixture=null;var owner=test;int depth=0;for(;owner!=null&&fixture==null&&depth<16;owner=owner.Parent,depth++)fixture=owner.Fixture as NativeDragonInputTests;UnityEngine.Debug.Log("QUALIFIED_EIGHT_OUTER_AFTER_LOOKUP depth="+depth+" fixtureFound="+(fixture!=null)+" parentChainBounded=true");
            Assert.That(fixture,Is.Not.Null,"The public NUnit ITest.Fixture/Parent chain must expose this actual test instance.");yield return fixture.CheckQualifiedEightSourcesAfterCleanup();
        }
    }
    IEnumerator CheckQualifiedEightSourcesAfterCleanup()
    {
        var proof=new QualifiedEightPostCleanupProof{captureComplete=qualifiedEightSourceCaptureComplete,bodyCompleted=qualifiedEightBodyComplete,originalSourceRows=qualifiedEightOriginalSourceRows,capturedFiles=qualifiedEightSourceBytes.Count};var failures=new List<string>();
        try
        {
            proof.cleanupBoundaryObserved=match==null&&(keyboard==null||!keyboard.added)&&pads.All(pad=>pad==null)&&riderMouse==null&&preferences==null&&temporary==null&&isolation==null&&(original==null||ReferenceEquals(UnityEngine.InputSystem.InputSystem.settings,original))&&(string.IsNullOrEmpty(qualifiedEightSavedPath)||!UnityEngine.SceneManagement.SceneManager.GetSceneByPath(qualifiedEightSavedPath).isLoaded);
            foreach(var row in qualifiedEightSourceBytes)
            {
                try{if(!File.Exists(QualifiedEightIoPath(row.Key))||!string.Equals(QualifiedEightHash(row.Key),row.Value,StringComparison.Ordinal)) {proof.failures++;if(failures.Count<16)failures.Add("Missing or changed captured source after UnityTearDown: "+row.Key);}}
                catch(Exception error){proof.failures++;if(failures.Count<16)failures.Add(row.Key+": "+error.GetType().Name+": "+error.Message);}
                proof.checkedFiles++;if(proof.checkedFiles%256==0&&!string.IsNullOrEmpty(output)){proof.differences=failures.ToArray();proof.status="POST_CLEANUP_SOURCE_AUDIT_IN_PROGRESS_NOT_ACCEPTANCE";File.WriteAllText(Path.Combine(output,"TruckDragonRiders-QualifiedEightSavedScene.source-after-cleanup.json"),JsonUtility.ToJson(proof,true));}if(proof.checkedFiles%64==0)yield return null;
            }
            proof.differences=failures.ToArray();proof.status=proof.captureComplete&&proof.cleanupBoundaryObserved&&proof.failures==0?"POST_CLEANUP_CAPTURED_SOURCE_BYTES_EXACT_NATIVE_GAMEPLAY_TEST_RESULT_SEPARATE":"POST_CLEANUP_SOURCE_CAPTURE_INCOMPLETE_OR_REJECTED_NOT_ACCEPTANCE";
            if(!string.IsNullOrEmpty(output)){Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"TruckDragonRiders-QualifiedEightSavedScene.source-after-cleanup.json"),JsonUtility.ToJson(proof,true));}
            TestContext.WriteLine("QUALIFIED_EIGHT_POST_UNITY_TEARDOWN_SOURCE_AUDIT captureComplete="+proof.captureComplete+" cleanupBoundary="+proof.cleanupBoundaryObserved+" checked="+proof.checkedFiles+" failures="+proof.failures+" nativeGamePlayResultSeparate=true");
            Assert.That(proof.cleanupBoundaryObserved,Is.True,"The source audit must observe completed inherited cleanup, including scene unload and original InputSystem restoration.");Assert.That(proof.failures,Is.Zero,string.Join("\n",failures));if(proof.bodyCompleted)Assert.That(proof.captureComplete&&proof.checkedFiles>0,Is.True,"A completed gameplay body requires the complete source guard, never an empty post-cleanup success.");
        }
        finally{DetachQualifiedEightSceneDiagnostics();qualifiedEightSourceBytes.Clear();qualifiedEightSavedScene=false;}
    }
    bool qualifiedEightSceneDiagnostics;
    void QualifiedEightSceneChanged(UnityEngine.SceneManagement.Scene before,UnityEngine.SceneManagement.Scene after)
    {Log("QUALIFIED_EIGHT_ACTIVE_SCENE_CHANGED before="+before.path+"/"+before.name+" after="+after.path+"/"+after.name+" beforeLoaded="+before.isLoaded+" afterLoaded="+after.isLoaded+" bodyComplete="+qualifiedEightBodyComplete);}
    void QualifiedEightSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
    {Log("QUALIFIED_EIGHT_SCENE_UNLOADED path="+scene.path+" name="+scene.name+" bodyComplete="+qualifiedEightBodyComplete);}
    void AttachQualifiedEightSceneDiagnostics()
    {
        Assert.That(qualifiedEightSceneDiagnostics,Is.False);UnityEngine.SceneManagement.SceneManager.activeSceneChanged+=QualifiedEightSceneChanged;UnityEngine.SceneManagement.SceneManager.sceneUnloaded+=QualifiedEightSceneUnloaded;qualifiedEightSceneDiagnostics=true;
    }
    void DetachQualifiedEightSceneDiagnostics()
    {
        if(!qualifiedEightSceneDiagnostics)return;UnityEngine.SceneManagement.SceneManager.activeSceneChanged-=QualifiedEightSceneChanged;UnityEngine.SceneManagement.SceneManager.sceneUnloaded-=QualifiedEightSceneUnloaded;qualifiedEightSceneDiagnostics=false;
    }
    void ObserveQualifiedEightOutgoingDiggerBeforeCleanup()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();Log("QUALIFIED_EIGHT_BODY_FINALLY_BEFORE_INHERITED_CLEANUP active="+scene.path+"/"+scene.name+" loaded="+scene.isLoaded+" sourceCaptureComplete="+qualifiedEightSourceCaptureComplete+" capturedFiles="+qualifiedEightSourceBytes.Count+" bodyComplete="+qualifiedEightBodyComplete);
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Digger.Modules.Core.Sources.DiggerSystem",false)).FirstOrDefault(t=>t!=null);if(type==null){Log("QUALIFIED_EIGHT_OUTGOING_DIGGER typeAbsent=true");return;}
        var systems=UnityEngine.Resources.FindObjectsOfTypeAll(type).OfType<Component>().Where(c=>c.gameObject.scene.IsValid()&&c.gameObject.scene.isLoaded).ToArray();Log("QUALIFIED_EIGHT_OUTGOING_DIGGER loadedSceneSystems="+systems.Length);
        foreach(var system in systems)
        {
            try
            {
                var cutter=type.GetProperty("Cutter",BindingFlags.Public|BindingFlags.Instance)?.GetValue(system);var undo=cutter?.GetType().GetField("undoPlayMode",BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(cutter);string holes="none",details="none",trees="none";
                if(undo!=null){var ut=undo.GetType();var h=ut.GetField("UndoHoles")?.GetValue(undo) as Array;holes=h==null?"none":string.Join("x",Enumerable.Range(0,h.Rank).Select(h.GetLength));details=(ut.GetField("UndoDetails")?.GetValue(undo) as System.Collections.ICollection)?.Count.ToString()??"none";trees=(ut.GetField("UndoTrees")?.GetValue(undo) as Array)?.Length.ToString()??"none";}
                Log("QUALIFIED_EIGHT_OUTGOING_DIGGER_STATE name="+system.name+" scene="+system.gameObject.scene.path+"/"+system.gameObject.scene.name+" initialized="+type.GetProperty("IsInitialized")?.GetValue(system)+" cutterUndoPresent="+(undo!=null)+" undoHoles="+holes+" undoDetailLayers="+details+" undoTrees="+trees+" beforeReadOnlyVersion=true");
                var version=type.GetMethod("GetDiggerVersion",BindingFlags.Public|BindingFlags.Instance)?.Invoke(system,null);Log("QUALIFIED_EIGHT_OUTGOING_DIGGER_VERSION name="+system.name+" actual="+version+" expected="+type.GetField("DiggerVersion",BindingFlags.Public|BindingFlags.Static)?.GetRawConstantValue()+" afterReadOnlyVersion=true");
            }
            catch(Exception error){Log("QUALIFIED_EIGHT_OUTGOING_DIGGER_DIAGNOSTIC_ERROR "+error);}
        }
    }
    readonly Dictionary<string,string> qualifiedEightSourceBytes=new Dictionary<string,string>(StringComparer.Ordinal);
    [Serializable]sealed class QualifiedEightFile{public string path,sha256;}
    [Serializable]sealed class QualifiedEightArchive
    {public string scope,archive,zipSha256,actualTestsSha256;public bool privateOnly,canonicalWritten,requiresFreshSemanticAndSourceQualificationBeforeReuse;public QualifiedEightFile[] files,proofs;}
    [Serializable]sealed class QualifiedEightNativeReceipt
    {
        public string status,failure,copyScene;public int nativeCalls,replacedChoices,protectedExternalObjects,protectedRacers,protectedTrucks,protectedPhysicalRings;
        public bool allOriginalChoicesPreflighted,otherSevenEveryStepExact,externalIdentitiesExact,externalConfigurationExact,worldExact,nwhExact,unitAncestors,qualifiedProfilesExact,noTestAssetDependencies,savedReloaded,originalSourcesExact;
        public float maximumNwhDimensionDrift;public QualifiedEightFile[] originalSources;
    }
    static string QualifiedEightPhysicalPath(string path)
    {return Path.GetFullPath(Path.IsPathRooted(path)?path:Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath,"..")),path));}
    static MethodInfo qualifiedEightHashAuthority,qualifiedEightIoAuthority;
    static string QualifiedEightIoPath(string path)
    {
        if(qualifiedEightIoAuthority==null)qualifiedEightIoAuthority=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomMountedCourseReview, Assembly-CSharp-Editor",true).GetMethod("Io",BindingFlags.Static|BindingFlags.NonPublic);
        Assert.That(qualifiedEightIoAuthority,Is.Not.Null,"Reuse the existing project Windows long-path I/O adapter.");return (string)qualifiedEightIoAuthority.Invoke(null,new object[]{QualifiedEightPhysicalPath(path)});
    }
    static string QualifiedEightHash(string path)
    {
        path=QualifiedEightPhysicalPath(path);
        try
        {
            if(qualifiedEightHashAuthority==null)qualifiedEightHashAuthority=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true).GetMethod("Hash",BindingFlags.Static|BindingFlags.NonPublic);
            Assert.That(qualifiedEightHashAuthority,Is.Not.Null,"Reuse the original exact project source-byte Hash authority, including its Windows long-path handling.");return ((string)qualifiedEightHashAuthority.Invoke(null,new object[]{path})).ToLowerInvariant();
        }
        catch(Exception error){var originalError=error is TargetInvocationException&&error.InnerException!=null?error.InnerException:error;UnityEngine.Debug.Log("QUALIFIED_EIGHT_SOURCE_HASH_IO_FAILURE physical="+path+" io="+QualifiedEightIoPath(path)+" error="+originalError);throw new IOException("Exact qualified source-byte Hash failed: "+path,originalError);}
    }
    static string QualifiedEightArgument(string name)
    {var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,name);Assert.That(index>=0&&index+1<args.Length,Is.True,"Explicit passed native source evidence required: "+name);return args[index+1];}
    void RequireQualifiedEightFile(string path,string sha)
    {
        path=QualifiedEightPhysicalPath(path);Assert.That(File.Exists(QualifiedEightIoPath(path)),Is.True,"Actual qualified source missing: "+path+" io="+QualifiedEightIoPath(path));Assert.That(sha,Is.Not.Null);Assert.That(sha.Length,Is.EqualTo(64));Assert.That(QualifiedEightHash(path),Is.EqualTo(sha.ToLowerInvariant()),"Actual qualified source changed: "+path);
        if(qualifiedEightSourceBytes.TryGetValue(path,out var previous))Assert.That(previous,Is.EqualTo(sha.ToLowerInvariant()),"Contradictory source hashes: "+path);else qualifiedEightSourceBytes.Add(path,sha.ToLowerInvariant());
    }
    string RequireQualifiedEightSavedScene()
    {
        Assert.That(riders&&qualifiedEightSavedScene,Is.True);string evidence=QualifiedEightArgument("-tsRiderQualifiedEightEvidence"),archivePath=QualifiedEightArgument("-tsRiderQualifiedEightArchiveReceipt");Assert.That(Path.IsPathRooted(evidence)&&Directory.Exists(QualifiedEightIoPath(evidence))&&Path.IsPathRooted(archivePath)&&File.Exists(QualifiedEightIoPath(archivePath)),Is.True);
        var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);var passed=author.GetMethod("RequireRiderPassedManifest",BindingFlags.Static|BindingFlags.NonPublic);Assert.That(passed,Is.Not.Null);
        const string test="DragonRiderNativeEightReplacementTests.ActualEightChoiceNativeReplacementPreservesOwnersAndRejectsLastChoiceEditBeforeStepZero";passed.Invoke(null,new object[]{evidence,test+"(False)"});passed.Invoke(null,new object[]{evidence,test+"(True)"});
        var archive=JsonUtility.FromJson<QualifiedEightArchive>(File.ReadAllText(QualifiedEightIoPath(archivePath)));Assert.That(archive.privateOnly&&!archive.canonicalWritten&&archive.requiresFreshSemanticAndSourceQualificationBeforeReuse,Is.True);Assert.That(archive.scope,Does.Contain("POST_NATIVE_CAPTURE").And.Contain("NOT_CANONICAL"));Assert.That(archive.files.Length,Is.EqualTo(4));Assert.That(archive.proofs.Length,Is.EqualTo(2));
        RequireQualifiedEightFile(Path.Combine(evidence,"tests.xml"),archive.actualTestsSha256);string zip=Path.Combine(Path.GetDirectoryName(archivePath),Path.GetFileName(archive.archive));RequireQualifiedEightFile(zip,archive.zipSha256);qualifiedEightSourceBytes.Add(archivePath,QualifiedEightHash(archivePath));
        string receiptPath=Path.Combine(evidence,"rendered-evidence","RiderSaddleAuthoring","NativeEightReplacement","actual-all-eight-replacement.json");var recorded=archive.proofs.Single(p=>p.path.EndsWith("/actual-all-eight-replacement.json",StringComparison.Ordinal));RequireQualifiedEightFile(receiptPath,recorded.sha256);
        var proof=JsonUtility.FromJson<QualifiedEightNativeReceipt>(File.ReadAllText(QualifiedEightIoPath(receiptPath)));Assert.That(proof.status,Is.EqualTo("DISPOSABLE_ALL_EIGHT_NATIVE_REPLACEMENTS_VALIDATED_NOT_CANONICAL"));Assert.That(proof.failure,Is.Null.Or.Empty);Assert.That(proof.nativeCalls==8&&proof.replacedChoices==8&&proof.protectedRacers==4&&proof.protectedTrucks==4&&proof.protectedPhysicalRings==12&&proof.protectedExternalObjects>20000,Is.True);
        Assert.That(proof.allOriginalChoicesPreflighted&&proof.otherSevenEveryStepExact&&proof.externalIdentitiesExact&&proof.externalConfigurationExact&&proof.worldExact&&proof.nwhExact&&proof.unitAncestors&&proof.qualifiedProfilesExact&&proof.noTestAssetDependencies&&proof.savedReloaded&&proof.originalSourcesExact,Is.True);Assert.That(proof.maximumNwhDimensionDrift,Is.Zero);
        Assert.That(proof.copyScene.StartsWith("Assets/TruckDragonRiders/Tests/Generated/NativeEight-",StringComparison.Ordinal)&&!Path.IsPathRooted(proof.copyScene)&&!proof.copyScene.Split('/').Contains(".."),Is.True,"Only the explicit actual disposable native scene is allowed.");
        foreach(string path in new[]{proof.copyScene,proof.copyScene+".meta"}){var row=archive.files.Single(p=>p.path==path);RequireQualifiedEightFile(path,row.sha256);}
        string project=Path.GetFullPath(Path.Combine(Application.dataPath,"..")).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar)+Path.DirectorySeparatorChar;
        var resolve=author.GetMethod("CurrentRiderPhysicalDependency",BindingFlags.Static|BindingFlags.NonPublic);Assert.That(resolve,Is.Not.Null,"Use the existing registered-package/source resolver, never skip logical package dependencies.");
        Assert.That(proof.originalSources,Is.Not.Empty);int sourceRowsChecked=0;foreach(var row in proof.originalSources){bool package=row.path.StartsWith("Packages/",StringComparison.Ordinal);string path=QualifiedEightPhysicalPath((string)resolve.Invoke(null,new object[]{row.path}));Assert.That((package||path.StartsWith(project,StringComparison.OrdinalIgnoreCase))&&!row.path.Replace('\\','/').Split('/').Contains(".."),Is.True,"Only this project or its registered, exactly hashed package dependencies are accepted.");RequireQualifiedEightFile(path,row.sha256);if(++sourceRowsChecked%128==0)Log("QUALIFIED_EIGHT_SOURCE_CAPTURE_PROGRESS checked="+sourceRowsChecked+"/"+proof.originalSources.Length+" capturedFiles="+qualifiedEightSourceBytes.Count+" gameplayNotStarted=true");}
        qualifiedEightOriginalSourceRows=proof.originalSources.Length;qualifiedEightSourceCaptureComplete=true;qualifiedEightSavedPath=proof.copyScene;return proof.copyScene;
    }
    readonly Dictionary<int,string> qualifiedEightObservedPhases=new Dictionary<int,string>();
    float qualifiedEightFirstDriveClock;
    string QualifiedEightRiderPhase(int slot)=>Read(((Array)Read(match,"racers")).GetValue(slot),"Phase").ToString();
    void ObserveQualifiedEightRiderPhases(string stage)
    {
        foreach(int slot in Enumerable.Range(0,4))
        {
            var racer=((Array)Read(match,"racers")).GetValue(slot);string phase=QualifiedEightRiderPhase(slot),signature=phase+"/"+Read(racer,"LastDislodgementAt")+"/"+Read(racer,"LastCollisionCollider");
            if(qualifiedEightObservedPhases.TryGetValue(slot,out var previous)&&previous==signature)continue;qualifiedEightObservedPhases[slot]=signature;
            Log("QUALIFIED_EIGHT_ACTUAL_PHASE stage="+stage+" slot="+slot+" clock="+Read(match,"Clock")+" phase="+phase+" dislodgedAt="+Read(racer,"LastDislodgementAt")+" collision="+Read(racer,"LastCollisionCollider")+" normalSpeed="+Read(racer,"LastCollisionNormalSpeed")+" reason="+Read(racer,"LastDislodgementReason")+" chute="+Read(racer,"ChuteOpen")+" actualSuccessfulRescues="+Read(racer,"SuccessfulRescues")+" actualFailedRescues="+Read(racer,"FailedRescues")+" publicInputsOnly=true");
        }
    }
    IEnumerator AwaitQualifiedEightMountedInputWindow(int slot)
    {
        if(QualifiedEightRiderPhase(slot)=="Mounted")yield break;
        var racer=((Array)Read(match,"racers")).GetValue(slot);string reason=(string)Read(racer,"LastDislodgementReason");float detached=(float)Read(racer,"LastDislodgementAt");
        Assert.That(reason=="HEAVY AERIAL COLLISION"||reason=="SADDLE DISLODGED BY ATTACK",Is.True,"A broken attachment or unexplained non-mounted state is not a valid input-readiness delay.");
        Assert.That(detached,Is.GreaterThanOrEqualTo(qualifiedEightFirstDriveClock),"Do not hide a pre-input/GO dislodgement behind recovery. Startup must first be observed Mounted before the assigned steering probes.");
        Assert.That(new[]{"Falling","RescueApproach","ClawCarry","ReturningToSaddle","FailedRescueNotice"},Does.Contain(QualifiedEightRiderPhase(slot)),"Only the observed public recovery lifecycle may defer Mounted steering.");
        ObserveQualifiedEightRiderPhases("before-public-recovery-input");Neutral();yield return null;RiderActionOnly(slot);yield return null;yield return null;
        Assert.That(Call(match,"ActionHeld",slot),Is.EqualTo(true));foreach(int other in Enumerable.Range(0,4).Where(other=>other!=slot&&Source(other)!="AI"))Assert.That(Call(match,"ActionHeld",other),Is.EqualTo(false),"Recovery action must remain assigned exclusively to the actual owner.");
        foreach(int human in Enumerable.Range(0,4).Where(human=>Source(human)!="AI"))Assert.That(((Vector2)Call(match,"ReadDrive",human)).sqrMagnitude,Is.LessThan(.0001f),"The assigned parachute action must not fabricate steering for any human.");
        yield return new WaitForFixedUpdate();if(QualifiedEightRiderPhase(slot)=="Falling"||QualifiedEightRiderPhase(slot)=="RescueApproach")Assert.That(Read(racer,"ChuteOpen"),Is.EqualTo(true),"The real assigned action must deploy the actual emergency parachute.");
        Neutral();yield return null;float deadline=Time.realtimeSinceStartup+35,clockDeadline=(float)Read(match,"Clock")+25,next=Time.realtimeSinceStartup;
        while(QualifiedEightRiderPhase(slot)!="Mounted"&&Time.realtimeSinceStartup<deadline&&(float)Read(match,"Clock")<clockDeadline)
        {
            Assert.That(Read(match,"Paused"),Is.EqualTo(false));Assert.That(new[]{"Falling","RescueApproach","ClawCarry","ReturningToSaddle","FailedRescueNotice"},Does.Contain(QualifiedEightRiderPhase(slot)),"Recovery cannot be treated as steering success if the participant finishes or retires.");
            ObserveQualifiedEightRiderPhases("natural-public-recovery");if(Time.realtimeSinceStartup>=next){ObserveDrive(slot,"qualified-eight-natural-recovery-wait");next=Time.realtimeSinceStartup+1;}
            yield return new WaitForFixedUpdate();
        }
        ObserveQualifiedEightRiderPhases("after-public-recovery-wait");Assert.That(QualifiedEightRiderPhase(slot),Is.EqualTo("Mounted"),"Actual grab/carry/return or public failed-rescue checkpoint recovery must restore the mount within the existing18+2second recovery lifecycle plus a bounded margin; never set phase, pose, velocity, score or timers in the test.");
        Log("QUALIFIED_EIGHT_PUBLIC_RECOVERY_INPUT_READY slot="+slot+" clock="+Read(match,"Clock")+" successful="+Read(racer,"SuccessfulRescues")+" failed="+Read(racer,"FailedRescues")+" originalDislodgedAt="+detached+" originalReason="+reason+" noInjectedRecoveryOrPhysics=true");
    }
    float QualifiedEightOutwardSteering(int slot)
    {
        var racer=((Array)Read(match,"racers")).GetValue(slot);Vector3 heading=(Vector3)Read(racer,"FlightHeading"),right=Vector3.Cross(Vector3.up,heading).normalized,centre=Vector3.zero;
        foreach(int other in Enumerable.Range(0,4).Where(other=>other!=slot))centre+=Position(other);centre/=3;
        float side=Vector3.Dot(Position(slot)-centre,right),sign=Mathf.Abs(side)>.001f?Mathf.Sign(side):slot<2?-1:1;
        Log("QUALIFIED_EIGHT_ACTUAL_OUTWARD_STEERING slot="+slot+" position="+Position(slot)+" otherCentroid="+centre+" heading="+heading+" actualLateralSeparation="+side+" assignedYawSign="+sign+" realSoftwareInputOnly=true");return sign;
    }
    void PulseQualifiedEightDrive(int slot,float sign)
    {
        if(Source(slot)=="Keyboard")
        {UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState((UnityEngine.InputSystem.Key)Read(Player(slot),"forward"),(UnityEngine.InputSystem.Key)Read(Player(slot),sign<0?"left":"right")));return;}
        var state=new UnityEngine.InputSystem.LowLevel.GamepadState();var direction=new Vector2(.75f*sign,.8f);
        if(Source(slot)=="WholeController"){state.leftStick=direction;state.rightTrigger=.7f;}else if(Source(slot)=="LeftStick")state.leftStick=direction;else state.rightStick=direction;
        UnityEngine.InputSystem.InputSystem.QueueStateEvent((UnityEngine.InputSystem.Gamepad)Read(Player(slot),"pad"),state);
    }
    void AssertQualifiedEightExclusiveDrive(int owner,float sign)
    {
        foreach(int slot in Enumerable.Range(0,4).Where(slot=>Source(slot)!="AI"))
        {
            var raw=(Vector2)Call(match,"ReadDrive",slot);var sampled=((Vector2[])Read(match,"commands"))[slot];
            if(slot==owner){Assert.That(raw.x*sign,Is.GreaterThan(.2f));Assert.That(raw.y,Is.GreaterThan(.2f));Assert.That(sampled.x*sign,Is.GreaterThan(.2f));Assert.That(sampled.y,Is.GreaterThan(.2f));}
            else{Assert.That(raw.sqrMagnitude,Is.LessThan(.0001f),"Assigned outward steering cannot leak into another human's source.");Assert.That(sampled.sqrMagnitude,Is.LessThan(.0001f),"Sampled drive must preserve the same exclusive owner.");}
        }
    }
    IEnumerator AwaitQualifiedEightCoreFlight(int slot,float sign)
    {
        var motor=FlightMotor(slot);if((bool)Read(motor,"IsFlying"))yield break;
        var animator=(Animator)Read(motor,"animator");bool actualTakeoff=animator.GetCurrentAnimatorClipInfo(0).Concat(animator.GetNextAnimatorClipInfo(0)).Any(value=>value.weight>0&&value.clip.name.IndexOf("Takeoff",StringComparison.OrdinalIgnoreCase)>=0);
        ObserveDrive(slot,"qualified-eight-before-natural-core-flight-wait");Assert.That(Read(motor,"IsGrounded"),Is.EqualTo(false),"Only observed airborne native Takeoff can extend the assigned input response window.");Assert.That(actualTakeoff,Is.True,"Do not replace the real Takeoff gate with an arbitrary readiness delay.");float deadline=Time.realtimeSinceStartup+6;
        while(!(bool)Read(motor,"IsFlying")&&Time.realtimeSinceStartup<deadline){ObserveQualifiedEightRiderPhases("natural-outward-takeoff");yield return new WaitForFixedUpdate();}
        ObserveDrive(slot,"qualified-eight-after-natural-core-flight-wait");AssertQualifiedEightExclusiveDrive(slot,sign);Assert.That(Read(motor,"IsFlying"),Is.EqualTo(true),"The original native Takeoff must naturally enter core Fly within the existing bounded window.");
    }
    IEnumerator FocusedQualifiedEightPresentation(int humans,string identity)
    {
        int[] humanSlots=Enumerable.Range(0,4).Where(slot=>Source(slot)!="AI").ToArray();
        for(int attempt=0;attempt<2;attempt++)
        {
            foreach(int slot in humanSlots)yield return AwaitQualifiedEightMountedInputWindow(slot);
            bool interrupted=false;var presentation=FocusedRiderPresentation(humans,identity);
            try
            {
                while(true)
                {
                    // Preserve the inherited positive Breath/cab/HUD/rematch
                    // assertions. Discard only a real collision-interrupted
                    // attempt before advancing its Mounted-only action state.
                    bool playing=(bool)Read(match,"Live")&&!(bool)Read(match,"Paused")&&(float)Read(match,"Countdown")<=0;
                    if(playing&&humanSlots.Any(slot=>QualifiedEightRiderPhase(slot)!="Mounted"))
                    {ObserveQualifiedEightRiderPhases("presentation-interrupted-before-mounted-action");Log("QUALIFIED_EIGHT_FULL_PRESENTATION_INTERRUPTED attempt="+attempt+" inheritedPositiveAssertionsRetained=true publicRecoveryRequiredBeforeRestart=true");interrupted=true;break;}
                    if(!presentation.MoveNext())yield break;yield return presentation.Current;
                }
            }
            finally{(presentation as IDisposable)?.Dispose();}
            Assert.That(interrupted&&attempt==0,Is.True,"Repeated interruptions cannot be accepted as completed presentation; the full original positive action/camera/HUD/rematch sequence must finish.");Neutral();yield return null;
        }
        Assert.Fail("The complete inherited presentation must finish after at most one genuine recovery interruption.");
    }
    // Keep the historically qualified input driver byte-exact. This pre-canonical
    // entry reuses its input/camera/HUD primitives against the actual saved scene.
    IEnumerator ExerciseQualifiedEightSavedScene()
    {
        mode="TruckDragonRiders";riders=true;reportName="TruckDragonRiders-QualifiedEightSavedScene";report.Clear();riderCaptureFolder="RidersQualifiedEight";
        coldInfernoFixture=false;transientCabCandidate=false;transientFireSize=false;transientFireNormalFamily=false;transientFireHeadLever=false;transientSourceEnvelopeFlap=0;mixedRiderEnvelope=false;safeMountedEnvelope=false;
        Assert.That(Application.isPlaying,Is.True);var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");output=Path.GetFullPath(Path.Combine(at>=0&&at+1<args.Length?args[at+1]:Application.persistentDataPath,"InputOwnership"));Directory.CreateDirectory(output);AttachQualifiedEightSceneDiagnostics();Log("QUALIFIED_EIGHT_SOURCE_CAPTURE_BEGIN gameplayNotStarted=true cwd="+Directory.GetCurrentDirectory()+" actualProject="+Path.GetFullPath(Path.Combine(Application.dataPath,"..")));string path;try{path=RequireQualifiedEightSavedScene();}catch(Exception error){Log("QUALIFIED_EIGHT_SOURCE_CAPTURE_REJECTED_BEFORE_GAMEPLAY "+error);throw;}
        preferences=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"DragonQualifiedEightInput"});Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
        isolation=new GameObject("Qualified eight saved-scene software input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));
        original=UnityEngine.InputSystem.InputSystem.settings;temporary=UnityEngine.Object.Instantiate(original);temporary.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;temporary.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;UnityEngine.InputSystem.InputSystem.settings=temporary;
        keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();UnityEngine.InputSystem.InputSystem.EnableDevice(keyboard);keyboard.MakeCurrent();
        for(int i=0;i<4;i++){pads[i]=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();UnityEngine.InputSystem.InputSystem.EnableDevice(pads[i]);}
        riderMouse=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>();UnityEngine.InputSystem.InputSystem.EnableDevice(riderMouse);riderMouse.MakeCurrent();
        yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path,new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));yield return null;yield return null;
        match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("LWS.TruckDragonRiders.DragonRidersMatch"));Assert.That(match,Is.Not.Null);players=(Array)Read(Read(match,"setup"),"players");trucks=(Array)Read(match,"trucks");views=Read(match,"drivingViews");
        Log("ACTUAL_V424_SAVED_ALL_EIGHT_SCENE softwareDevicesOnly=true path="+path+" protectedSourceFiles="+qualifiedEightSourceBytes.Count+"; no fixture actor replacement, pose/velocity/state/score injection, accepted-world, full-race, Windows, physical hardware or human acceptance");
        observedStaleNativeModeResets=0;observedPrepareModeReceipts.Clear();
        foreach(int humans in new[]{1,2,3,4})
        {
            Neutral();yield return null;Configure(humans);Runtime("LWS.TruckSports.SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{false});Call(views,"Select",1);Assert.That(Call(match,"StartMatch"),Is.EqualTo(true));
            float deadline=Time.realtimeSinceStartup+35;while((float)Read(match,"Countdown")>0&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That((float)Read(match,"Countdown"),Is.Zero);yield return null;yield return null;
            ObserveActualPrepareModeResets("qualified-eight-layout-start");foreach(int slot in Enumerable.Range(0,4).Where(slot=>Source(slot)!="AI"))AssertActualRiderReadiness(slot,"qualified-eight-after-countdown");
            string identity=Identity();var ids=Enumerable.Range(0,4).Select(slot=>(string)Read(Player(slot),"participantId")).ToArray();Assert.That(ids.All(value=>!string.IsNullOrWhiteSpace(value))&&ids.Distinct().Count()==4,Is.True);var initial=Enumerable.Range(0,4).Select(Position).ToArray();AssertLayout(humans,false,identity);
            qualifiedEightObservedPhases.Clear();ObserveQualifiedEightRiderPhases("before-first-assigned-drive");foreach(int slot in Enumerable.Range(0,4))Assert.That(QualifiedEightRiderPhase(slot),Is.EqualTo("Mounted"),"Every actual crew must begin mounted; recovery must not hide an initial spawn/GO failure.");qualifiedEightFirstDriveClock=(float)Read(match,"Clock");
            foreach(int slot in Enumerable.Range(0,4).Where(slot=>Source(slot)!="AI"))
            {
                bool steered=false;
                for(int attempt=0;attempt<2&&!steered;attempt++)
                {
                    yield return AwaitQualifiedEightMountedInputWindow(slot);Neutral();yield return null;yield return null;Vector3 start=Position(slot),heading=(Vector3)Read(((Array)Read(match,"racers")).GetValue(slot),"heading");float steeringSign=QualifiedEightOutwardSteering(slot);PulseQualifiedEightDrive(slot,steeringSign);yield return null;yield return null;AssertQualifiedEightExclusiveDrive(slot,steeringSign);ObserveDrive(slot,"qualified-eight-held-start");
                    float end=Time.time+1.1f;while(Time.time<end){Assert.That(Read(match,"Paused"),Is.EqualTo(false));ObserveQualifiedEightRiderPhases("assigned-steering-window");yield return new WaitForFixedUpdate();}ObserveDrive(slot,"qualified-eight-held-end");AssertQualifiedEightExclusiveDrive(slot,steeringSign);float yaw=Vector3.Angle(heading,(Vector3)Read(((Array)Read(match,"racers")).GetValue(slot),"heading"));
                    if(yaw<=.5f&&QualifiedEightRiderPhase(slot)!="Mounted"&&attempt==0){Log("QUALIFIED_EIGHT_STEERING_INTERRUPTED_BY_ACTUAL_RECOVERY slot="+slot+" yaw="+yaw+" phase="+QualifiedEightRiderPhase(slot)+" retryLimit=1 originalSteeringGuardRetained=true");continue;}
                    Assert.That(Vector3.Distance(Position(slot),start),Is.GreaterThan(.15f),"Assigned input must move the real qualified vendor participant.");Assert.That(yaw,Is.GreaterThan(.5f),"The observed Mounted input window must actually change the owned flight heading; raw input alone is insufficient.");yield return AwaitQualifiedEightCoreFlight(slot,steeringSign);Assert.That(Read(FlightMotor(slot),"IsFlying"),Is.EqualTo(true));steered=true;
                }
                Assert.That(steered,Is.True,"An actual recovered participant still needs the original exclusive movement and heading proof.");
            }
            Neutral();yield return null;foreach(int slot in Enumerable.Range(0,4).Where(slot=>Source(slot)=="AI"))Assert.That(Vector3.Distance(Position(slot),initial[slot]),Is.GreaterThan(.05f),"AI must remain active without occupying a human viewport.");
            yield return FocusedQualifiedEightPresentation(humans,identity);yield return CameraOwnership(humans,identity);Assert.That(Identity(),Is.EqualTo(identity));Call(match,"SetupScreen");yield return null;
        }
    }
    [UnityTest,Timeout(900000),QualifiedEightSourceAfterCleanup]
    public IEnumerator RidersPassedEightSavedProfilesKeepOneToFourSoftwareOwnersCabsAndViewportHudsThroughPublicStarts()
    {
        qualifiedEightSavedScene=true;qualifiedEightSourceCaptureComplete=false;qualifiedEightBodyComplete=false;qualifiedEightOriginalSourceRows=0;qualifiedEightSavedPath=null;qualifiedEightSourceBytes.Clear();
        try
        {
            yield return ExerciseQualifiedEightSavedScene();
            Log("ACTUAL_PASSED_EIGHT_SAVED_SCENE_1_TO4_SOFTWARE_OWNERS_CAB_HUD_PUBLIC_STARTS_COMPLETE_NOT_CANONICAL_CURRENT_WORLD_FULL_RACE_WINDOWS_HARDWARE_OR_HUMAN_ACCEPTANCE");qualifiedEightBodyComplete=true;
        }
        finally
        {
            if(!string.IsNullOrEmpty(output))ObserveQualifiedEightOutgoingDiggerBeforeCleanup();
            qualifiedEightSavedScene=false; // Retain the exact source snapshot for the guaranteed post-UnityTearDown outer action.
        }
    }
}
#endif
