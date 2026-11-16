#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using LWS.TruckHoardHeist;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Controlled saved-scene planning contract, NOT a full gameplay result. Original
// loose cargo is temporarily raised while paused to create unsupported targets.
// No delivery, pickup, action, route, score or result callback is injected.
public sealed class HoardHeistAccessNativeTests
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    const string ScenePath = "Assets/TruckHoardHeist/Scenes/TruckHoardHeist.unity";
    Component match;
    GameObject isolation;
    IDisposable preferences;
    object keyboard;
    ScriptableObject originalInputSettings,temporaryInputSettings;
    object[] cargo;
    Rigidbody[] bodies;
    Vector3[] savedPositions;
    Quaternion[] savedRotations;
    bool moved;
    string output;
    readonly List<Row> rows = new List<Row>();
    readonly Dictionary<string,string> hashes = new Dictionary<string,string>();

    [Serializable] public sealed class Row
    {
        public string stage, reason, source, matchId;
        public int slot, index, checks, selected;
        public float clock;
        public bool accepted;
        public bool authoredRoadFound;
        public float authoredRoadDistance;
        public Vector3 position, cachedPosition, parking, nearestAuthoredRoad, truckOrigin;
    }
    [Serializable] public sealed class Report
    {
        public string scope, status;
        public int[] originalBodies;
        public string[] stableIds;
        public Row[] observations;
    }
    static Type Runtime(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name,false)).First(t=>t!=null);
    static object Read(object instance,string name)
    {
        for(Type type=instance.GetType();type!=null;type=type.BaseType)
        {
            var field=type.GetField(name,Any|BindingFlags.DeclaredOnly);if(field!=null)return field.GetValue(instance);
            var property=type.GetProperty(name,Any|BindingFlags.DeclaredOnly);if(property!=null)return property.GetValue(instance);
        }
        throw new MissingMemberException(instance.GetType().Name,name);
    }
    static object Call(object instance,string name,params object[] args) => instance.GetType().GetMethods(Any).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(instance,args);
    static object[] Values(object instance,string name) => ((Array)Read(instance,name)).Cast<object>().ToArray();
    HoardHeistRules Rules => (HoardHeistRules)Read(match,"Rules");
    int Checks => (int)Read(match,"treasureAccessChecks");
    string Source(int slot) => Read(Values(Read(match,"setup"),"players")[slot],"source").ToString();
    static string Hash(string path) { using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))); }
    bool NearbyAuthoredRoad(Vector3 position,out Vector3 nearest)
    {
        var query=Runtime("LWS.InterstateHauler.LwsRoadGraphQuery").GetMethods(BindingFlags.Public|BindingFlags.Static).Single(method=>method.Name=="TryFindNearestRoad"&&method.GetParameters().Length==4);
        object[] arguments={Read(match,"liveGraph"),position,150f,null};bool found=(bool)query.Invoke(null,arguments);
        nearest=found?(Vector3)Read(arguments[3],"NearestPosition"):Vector3.zero;return found;
    }
    void Query(string stage,int slot,int index,bool accepted,int selected=-1)
    {
        var row=new Row {stage=stage,slot=slot,index=index,accepted=accepted,selected=selected,checks=Checks,
            clock=(float)Read(match,"Clock"),source=Source(slot),matchId=(string)Read(match,"MatchId"),position=bodies[index].position,
            truckOrigin=((Rigidbody)Read(Values(match,"trucks")[slot],"body")).position,
            cachedPosition=((Vector3[,])Read(match,"treasureAccessPosition"))[slot,index],
            parking=((Vector3[,])Read(match,"treasureAccessParking"))[slot,index],reason=((string[,])Read(match,"treasureAccessReason"))[slot,index]};
        row.authoredRoadFound=NearbyAuthoredRoad(row.position,out row.nearestAuthoredRoad);row.authoredRoadDistance=row.authoredRoadFound?Vector3.Distance(row.position,row.nearestAuthoredRoad):-1;
        rows.Add(row);TestContext.WriteLine(JsonUtility.ToJson(row));Persist("IN PROGRESS");
    }
    void Persist(string status)
    {
        if(output==null)return;
        File.WriteAllText(Path.Combine(output,"evidence.json"),JsonUtility.ToJson(new Report {
            scope="Controlled saved-scene access/cache fixture with original NWH crews and original cargo. Paused airborne cargo and the recorded V606 stranded pose are deliberate test geometry. Controlled crane/road-recovery checks include one explicit restraint/rule setup but claim no full match, delivery, hardware or human acceptance.",
            status=status,originalBodies=bodies?.Select(b=>b.GetInstanceID()).ToArray(),
            stableIds=cargo?.Select(c=>(string)Read(c,"stableId")).ToArray(),observations=rows.ToArray()},true));
    }
    void Unawarded(int[] identities,string[] ids,float[] masses)
    {
        CollectionAssert.AreEqual(identities,bodies.Select(b=>b.GetInstanceID()).ToArray());
        CollectionAssert.AreEqual(ids,cargo.Select(c=>(string)Read(c,"stableId")).ToArray());
        CollectionAssert.AreEqual(masses,bodies.Select(b=>b.mass).ToArray());
        Assert.That(Rules.States,Is.All.EqualTo(HoardCargoState.Loose));Assert.That(Rules.Owners,Is.All.EqualTo(-1));
        Assert.That(Rules.Scores,Is.All.Zero);Assert.That(Rules.DeliveredCount,Is.Zero);Assert.That(Rules.LostCount,Is.Zero);
        Assert.That((int)Read(match,"PhysicalLoads"),Is.Zero);Assert.That((int)Read(match,"PhysicalDeliveries"),Is.Zero);
        Assert.That((int)Read(Read(match,"ScoreJournal"),"Count"),Is.Zero);
        foreach(var item in cargo){Assert.That(((Component)item).gameObject.activeInHierarchy,Is.True);Assert.That(Read(item,"CargoJoint"),Is.Null);}
        foreach(var body in bodies)Assert.That(body.isKinematic,Is.False,"Fixture never freezes or substitutes original physical cargo.");
    }
    void SavePoses() { savedPositions=bodies.Select(b=>b.position).ToArray();savedRotations=bodies.Select(b=>b.rotation).ToArray();moved=true; }
    void RestorePoses()
    {
        if(!moved)return;
        for(int i=0;i<bodies.Length;i++){bodies[i].position=savedPositions[i];bodies[i].rotation=savedRotations[i];}
        Physics.SyncTransforms();moved=false;
    }
    void StartPaused()
    {
        Assert.That((bool)Call(match,"StartMatch"),Is.True);Call(match,"Pause",0,false);
        Assert.That((bool)Read(match,"Paused"),Is.True);Assert.That(Time.timeScale,Is.Zero);
        Assert.That(Checks,Is.Zero,"Ordinary synchronous new-match preparation must clear the search count.");
        Assert.That(((bool[,])Read(match,"treasureAccessValid")).Cast<bool>(),Is.All.False);
        Assert.That(((string[,])Read(match,"treasureAccessReason")).Cast<string>(),Is.All.Null);
        Assert.That(((float[,])Read(match,"treasureAccessUntil")).Cast<float>(),Is.All.Zero);
        Physics.SyncTransforms();
    }

    // Explicit controlled adapter fixture after the read-only access checks;
    // this does not inject or claim a whole-match delivery.
    IEnumerator HeldHoistAndStrandedCrewUseTheExistingRecovery()
    {
        Call(match,"SetupScreen");Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);
        Assert.That((bool)Read(match,"Live"),Is.True);Assert.That((bool)Read(match,"Paused"),Is.False);Assert.That(Time.timeScale,Is.EqualTo(1));
        float deadline=Time.realtimeSinceStartup+15;
        while((float)Read(match,"Intro")>0&&Time.realtimeSinceStartup<deadline){Assert.That((bool)Read(match,"Paused"),Is.False);yield return null;}
        Assert.That((float)Read(match,"Intro"),Is.Zero,"Actual NWH wheels must settle before controlled driving checks.");
        Call(match,"Pause",0,false);
        var truck=Values(match,"trucks")[0];var truckBody=(Rigidbody)Read(truck,"body");
        var crane=Values(match,"hoists")[0];var route=Call(match,"NavigationRoute",0);
        Assert.That((bool)Call(crane,"Begin",bodies[0],bodies[0].position+Vector3.up*3,Quaternion.identity,(float)Read(cargo[0],"Height")),Is.True);
        Call(route,"DirectGoal",truckBody.position,truckBody.position+Vector3.forward*30);
        float stall=(float)Read(route,"SecondsWithoutProgress");int reverse=(int)Read(route,"RecoveryStarts");
        Assert.That((bool)Call(match,"CanFollowRoute",0,.02f),Is.False,"AI crane hold must stop before the road driver's stall/reverse update.");
        float clock=(float)Read(match,"Clock"),fixedAt=Time.fixedTime;Call(match,"Resume");
        Assert.That((bool)Read(match,"Live"),Is.True);Assert.That((bool)Read(match,"Paused"),Is.False);Assert.That(Time.timeScale,Is.EqualTo(1));deadline=Time.realtimeSinceStartup+10;
        while((float)Read(match,"Clock")<clock+.5f&&Time.realtimeSinceStartup<deadline){Assert.That((bool)Read(match,"Live"),Is.True);Assert.That((bool)Read(match,"Paused"),Is.False);yield return null;}
        Assert.That((float)Read(match,"Clock"),Is.GreaterThanOrEqualTo(clock+.5f),"Resumed hoist hold made no real fixed-step progress within10 realtime seconds.");Assert.That(Time.fixedTime,Is.GreaterThan(fixedAt));
        Call(match,"Pause",0,false);
        Assert.That((float)Read(route,"SecondsWithoutProgress"),Is.EqualTo(stall));Assert.That((int)Read(route,"RecoveryStarts"),Is.EqualTo(reverse));
        Assert.That((int[])Read(match,"Recoveries"),Is.All.Zero); // Keep the controlled hoist busy while real wheels settle: no automatic recovery race.
        // Exact observed V606 hold location. Placement is test geometry, then
        // genuine NWH wheel contact is allowed to settle before querying it.
        Call(truck,"Prepare",new Vector3(439.75f,105.10f,760.72f),truckBody.rotation,true);Physics.SyncTransforms();
        clock=(float)Read(match,"Clock");fixedAt=Time.fixedTime;Call(match,"Resume");
        Assert.That((bool)Read(match,"Live"),Is.True);Assert.That((bool)Read(match,"Paused"),Is.False);Assert.That(Time.timeScale,Is.EqualTo(1));deadline=Time.realtimeSinceStartup+10;
        float settledSince=-1;
        while(Time.realtimeSinceStartup<deadline)
        {
            Assert.That((bool)Read(match,"Live"),Is.True);Assert.That((bool)Read(match,"Paused"),Is.False);
            if((bool)Read(Read(truck,"tractorMotor"),"Grounded")&&truckBody.linearVelocity.magnitude<.5f)
            {if(settledSince<0)settledSince=Time.fixedTime;if(Time.fixedTime-settledSince>=.3f)break;}
            else settledSince=-1;
            yield return null;
        }
        Assert.That(settledSince,Is.GreaterThanOrEqualTo(0),"Actual NWH truck never reached the unchanged grounded/stationary predicate.");Assert.That(Time.fixedTime-settledSince,Is.GreaterThanOrEqualTo(.3f),"Actual NWH truck did not remain grounded below0.5m/s for0.3fixed seconds within10realtime seconds.");Assert.That(Time.fixedTime,Is.GreaterThan(fixedAt));
        Call(match,"Pause",0,false);
        Assert.That((bool)Call(match,"SupportedRoadOrigin",0),Is.False);
        Assert.That((bool)Read(Read(truck,"tractorMotor"),"Grounded"),Is.True,"The observed failure is grounded, not an airborne shortcut.");
        Assert.That(truckBody.linearVelocity.magnitude,Is.LessThan(.5f));Assert.That((bool)Call(match,"TruckBelowRoad",0),Is.False);
        Assert.That(((int[])Read(match,"Recoveries"))[0],Is.Zero);Call(match,"StopHoist",0);
        // Other real crews may collect during settling; choose an actually
        // loose original body instead of stealing another crew's cargo.
        int contractCargo=Enumerable.Range(0,cargo.Length).First(index=>Rules.States[index]==HoardCargoState.Loose&&Rules.Owners[index]<0&&Read(cargo[index],"CargoJoint")==null&&!bodies[index].isKinematic);
        float height=(float)Read(cargo[contractCargo],"Height");
        bodies[contractCargo].position=(Vector3)Call(match,"RackPoint",0,0,height);bodies[contractCargo].rotation=truckBody.rotation;bodies[contractCargo].linearVelocity=bodies[contractCargo].angularVelocity=Vector3.zero;
        Assert.That(Rules.ReserveLift(contractCargo,0,true),Is.True);Assert.That((bool)Call(cargo[contractCargo],"Secure",0,truckBody),Is.True);Assert.That(Rules.CompleteLoad(contractCargo,0,true),Is.True);
        Vector3 position=truckBody.position;int identity=bodies[contractCargo].GetInstanceID();
        Assert.That((bool)Call(match,"CanFollowRoute",0,2.1f),Is.False);
        Assert.That(((int[])Read(match,"Recoveries"))[0],Is.EqualTo(1));Assert.That(((float[])Read(match,"recoverAt"))[0],Is.GreaterThan(Time.time));
        Assert.That(truckBody.position,Is.EqualTo(position),"Recovery must remain delayed; the gate cannot teleport the truck.");
        Assert.That(Read(cargo[contractCargo],"CargoJoint"),Is.Null);Assert.That((int)Read(cargo[contractCargo],"Carrier"),Is.EqualTo(-1));Assert.That(Rules.States[contractCargo],Is.EqualTo(HoardCargoState.Loose));
        Assert.That(bodies[contractCargo].GetInstanceID(),Is.EqualTo(identity));Assert.That(bodies[contractCargo].isKinematic,Is.False);Assert.That(Rules.Scores,Is.All.Zero);Assert.That(Rules.DeliveredCount,Is.Zero);
        Call(match,"CycleSource",0);Assert.That(Source(0),Is.EqualTo("Keyboard"));
        Assert.That((bool)Call(match,"CanFollowRoute",0,10f),Is.True,"Human control must bypass this AI-only road hold/recovery.");
        Assert.That(((int[])Read(match,"Recoveries"))[0],Is.EqualTo(1));
        TestContext.WriteLine("Controlled hoist hold kept stall/reversals unchanged; actual grounded V606 unsupported origin scheduled delayed recovery and released original cargo; human bypass retained.");
    }

    [UnityTest, Timeout(240000)]
    public IEnumerator SavedOriginalCargoUsesSupportedAiAccessCacheMovementRematchAndHumanBypass()
    {
        string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");
        output=Path.GetFullPath(Path.Combine(at>=0&&at+1<args.Length?args[at+1]:Application.persistentDataPath,"HeistControlledAccess",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")));Directory.CreateDirectory(output);
        foreach(string path in new[]{ScenePath,ScenePath+".meta","Assets/TruckHoardHeist/Scripts/HoardHeistMatch.Access.cs","Assets/TruckHoardHeist/Rules/HoardHeistRules.cs"})
        {Assert.That(File.Exists(path),Is.True,path);hashes[path]=Hash(path);}
        bool passed=false;
        try
        {
            preferences=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"HeistControlledAccessNative"});
            Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
            var inputSystem=Runtime("UnityEngine.InputSystem.InputSystem");
            var settingsProperty=inputSystem.GetProperty("settings",BindingFlags.Public|BindingFlags.Static);
            originalInputSettings=(ScriptableObject)settingsProperty.GetValue(null);temporaryInputSettings=UnityEngine.Object.Instantiate(originalInputSettings);
            foreach(var setting in new[]{new[]{"backgroundBehavior","IgnoreFocus"},new[]{"editorInputBehaviorInPlayMode","AllDeviceInputAlwaysGoesToGameView"}})
            {var property=temporaryInputSettings.GetType().GetProperty(setting[0]);property.SetValue(temporaryInputSettings,Enum.Parse(property.PropertyType,setting[1]));}
            settingsProperty.SetValue(null,temporaryInputSettings);
            isolation=new GameObject("Heist controlled access input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));
            // Isolation deliberately disables native keyboards. The ordinary
            // human setup below needs a real enabled InputSystem device, even
            // though this contract sends no driving or action input.
            keyboard=Runtime("UnityEngine.InputSystem.InputSystem").GetMethod("AddDevice",new[]{typeof(string),typeof(string),typeof(string)}).Invoke(null,new object[]{"Keyboard","Heist controlled planning virtual keyboard",null});
            Assert.That((bool)Read(keyboard,"native"),Is.False,"The fixture must create a software keyboard, never enable physical input.");
            inputSystem.GetMethod("EnableDevice",new[]{Runtime("UnityEngine.InputSystem.InputDevice")}).Invoke(null,new[]{keyboard});
            keyboard.GetType().GetMethod("MakeCurrent").Invoke(keyboard,null);
            Assert.That((bool)Read(keyboard,"enabled"),Is.True,"The explicit software keyboard must stay enabled under batch-mode focus policy.");
            EditorSceneManager.LoadSceneInPlayMode(ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("LWS.TruckHoardHeist.HoardHeistMatch"));Assert.That(match,Is.Not.Null);
            var setup=Read(match,"setup");setup.GetType().GetField("count").SetValue(setup,4);Call(match,"AllBots");
            cargo=Values(match,"treasure");bodies=cargo.Select(c=>(Rigidbody)Read(c,"body")).ToArray();
            int[] identities=bodies.Select(b=>b.GetInstanceID()).ToArray();string[] ids=cargo.Select(c=>(string)Read(c,"stableId")).ToArray();float[] masses=bodies.Select(b=>b.mass).ToArray();
            Assert.That(identities.Distinct().Count(),Is.EqualTo(cargo.Length));Assert.That(cargo.Length,Is.InRange(1,64));
            foreach(var truck in Values(match,"trucks"))
            {var motor=Read(truck,"tractorMotor");Assert.That(motor,Is.Not.Null);Assert.That(Read(motor,"vehicle").GetType().FullName,Does.StartWith("NWH.VehiclePhysics2"));}
            StartPaused();string firstMatch=(string)Read(match,"MatchId");Unawarded(identities,ids,masses);
            int safe=-1;
            for(int index=0;index<cargo.Length;index++)
            {
                bool accepted=(bool)Call(match,"CanReachTreasure",0,index);Query("original-authored-cargo",0,index,accepted);
                if(accepted){safe=index;break;}
            }
            Assert.That(safe,Is.GreaterThanOrEqualTo(0),"At least one original authored treasure must have a genuine full-truck road connector; inspect exact rejection reasons.");
            Assert.That(((string[,])Read(match,"treasureAccessReason"))[0,safe],Is.EqualTo("ACTUAL FULL-TRUCK SUPPORTED ROAD CONNECTOR"));
            int count=Checks;Assert.That((bool)Call(match,"CanReachTreasure",0,safe),Is.True);Assert.That(Checks,Is.EqualTo(count),"Identical paused query must reuse the real cache.");Query("same-position-cache-hit",0,safe,true);
            // Reproduce V294's impossible source-to-road connector while the
            // original cargo and its successful final-access cache stay exact.
            // This controlled paused pose is restored before any simulation.
            var originalTruck=(Rigidbody)Read(Values(match,"trucks")[0],"body");
            Vector3 truckPosition=originalTruck.position;Quaternion truckRotation=originalTruck.rotation;
            try
            {
                originalTruck.position=truckPosition-Vector3.up*40;Physics.SyncTransforms();
                Assert.That(NearbyAuthoredRoad(originalTruck.position,out var originRoad),Is.True,"An actual road must still be found; the defect is unsupported access, not graph lookup failure.");
                Assert.That(originRoad.y-originalTruck.position.y,Is.GreaterThan(30));
                bool accepted=(bool)Call(match,"CanReachTreasure",0,safe);Query("controlled-unsupported-origin-with-cached-valid-cargo",0,safe,accepted);
                Assert.That(accepted,Is.False,"A valid cargo-to-road connector cannot authorize a forty-metre vertical truck-to-road shortcut, including within the existing cache interval.");
                Unawarded(identities,ids,masses);
            }
            finally{originalTruck.position=truckPosition;originalTruck.rotation=truckRotation;Physics.SyncTransforms();}
            Assert.That((bool)Call(match,"CanReachTreasure",0,safe),Is.True,"Restoring the original supported truck must invalidate the negative origin witness immediately.");
            count=Checks;
            SavePoses();bodies[safe].position=savedPositions[safe]+Vector3.up*40;Physics.SyncTransforms();
            Assert.That(NearbyAuthoredRoad(bodies[safe].position,out var nearby),Is.True,"This controlled rejection must retain real authored-road lookup within150m, so it exercises physical final access rather than missing-road rejection.");Assert.That(Vector3.Distance(nearby,bodies[safe].position),Is.LessThanOrEqualTo(150));
            Assert.That((bool)Call(match,"CanReachTreasure",0,safe),Is.False,"Original cargo40m above its supported seed must fail genuine physical final access.");Assert.That(Checks,Is.EqualTo(count+1));Assert.That(((string[,])Read(match,"treasureAccessReason"))[0,safe],Is.EqualTo("NO SUPPORTED FINAL ACCESS"));Query("controlled-forty-metre-air-with-real-road",0,safe,false);
            count=Checks;Assert.That((bool)Call(match,"CanReachTreasure",0,safe),Is.False);Assert.That(Checks,Is.EqualTo(count));
            bodies[safe].position+=Vector3.right*2;Physics.SyncTransforms();
            Assert.That((bool)Call(match,"CanReachTreasure",0,safe),Is.False);Assert.That(Checks,Is.EqualTo(count+1));
            Assert.That(((Vector3[,])Read(match,"treasureAccessPosition"))[0,safe],Is.EqualTo(bodies[safe].position));Query("two-metre-movement-invalidates",0,safe,false);
            RestorePoses();Assert.That((bool)Call(match,"CanReachTreasure",0,safe),Is.True,"Returning the same original cargo to its unchanged supported seed must be revalidated.");Unawarded(identities,ids,masses);
            Call(match,"SetupScreen");StartPaused();Assert.That((string)Read(match,"MatchId"),Is.Not.EqualTo(firstMatch));Unawarded(identities,ids,masses);
            SavePoses();for(int i=0;i<bodies.Length;i++)bodies[i].position=savedPositions[i]+Vector3.up*500;Physics.SyncTransforms();
            Assert.That((int)Call(match,"ChooseTreasure",0),Is.EqualTo(-1),"AI must reject the original inventory when every final access is unsupported.");Assert.That(Checks,Is.GreaterThan(0));Query("ai-unsupported-inventory",0,safe,false,-1);Unawarded(identities,ids,masses);
            RestorePoses();Call(match,"SetupScreen");Call(match,"CycleSource",0);Assert.That(Source(0),Is.EqualTo("Keyboard"),"Ordinary public source cycle selects the existing keyboard owner.");
            StartPaused();SavePoses();for(int i=0;i<bodies.Length;i++)bodies[i].position=savedPositions[i]+Vector3.up*500;Physics.SyncTransforms();
            int humanTarget=(int)Call(match,"ChooseTreasure",0);Assert.That(humanTarget,Is.GreaterThanOrEqualTo(0));Assert.That(Checks,Is.Zero,"Human treasure selection must not run the AI-only static access filter.");Query("human-source-planning-bypass-not-delivery",0,humanTarget,true,humanTarget);Unawarded(identities,ids,masses);
            RestorePoses();yield return HeldHoistAndStrandedCrewUseTheExistingRecovery();passed=true;
        }
        finally
        {
            RestorePoses();if(match!=null)Call(match,"SetupScreen");Time.timeScale=1;
            if(keyboard!=null)Runtime("UnityEngine.InputSystem.InputSystem").GetMethod("RemoveDevice",new[]{Runtime("UnityEngine.InputSystem.InputDevice")}).Invoke(null,new[]{keyboard});keyboard=null;
            if(isolation!=null)UnityEngine.Object.DestroyImmediate(isolation);preferences?.Dispose();Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
            if(originalInputSettings!=null)Runtime("UnityEngine.InputSystem.InputSystem").GetProperty("settings",BindingFlags.Public|BindingFlags.Static).SetValue(null,originalInputSettings);
            if(temporaryInputSettings!=null)UnityEngine.Object.DestroyImmediate(temporaryInputSettings);
            Persist(passed?"PASSED CONTROLLED ACCESS CONTRACT":"INCOMPLETE OR FAILED; inspect NUnit result");
            foreach(var pair in hashes)Assert.That(Hash(pair.Key),Is.EqualTo(pair.Value),"Read-only fixture changed source/scene: "+pair.Key);
        }
    }

    [UnityTearDown] public IEnumerator UnloadOwnedTestScene()
    {
        if(match==null)yield break;
        var scene=match.gameObject.scene;if(!scene.IsValid()||!scene.isLoaded)yield break;
        var empty=SceneManager.CreateScene("Heist access fixture cleanup "+Guid.NewGuid());SceneManager.SetActiveScene(empty);
        yield return SceneManager.UnloadSceneAsync(scene);match=null;
    }
}
#endif
