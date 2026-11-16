#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class DragonCharacterPerceptionNativeTests
{
    const string ActorPath = "Assets/TruckDragonKingdom/Prefabs/Dragons/dragon-inferno/dragon-inferno Dragon Actor.prefab";
    const string CatalogPath = "Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset";
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    GameObject actor, truck, cover, floor;
    Component character, eventTruck;
    Rigidbody truckBody;
    object Get(object value, string name) => value.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(value) : value.GetType().GetProperty(name, Any).GetValue(value);
    void Set(object value, string field, object supplied) => value.GetType().GetField(field, Any).SetValue(value, supplied);
    object Call(object value, string method, params object[] args) => value.GetType().GetMethod(method).Invoke(value, args);
    void Tick(float dt) => Call(character, "Tick", dt);
    bool Remembered(out Vector3 position)
    {
        var args = new object[] { 0, null, null };
        bool result = (bool)character.GetType().GetMethod("TryReadRememberedTarget").Invoke(character, args);
        position = (Vector3)args[1]; return result;
    }
    void Construct()
    {
        Assert.That(Application.isPlaying, Is.True, "Run this fixture in the native PlayMode test runner.");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ActorPath);
        Assert.That(prefab, Is.Not.Null, "Explicitly author the configured real Inferno actor before running its native acceptance fixture.");
        floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "Native perception ground";
        floor.transform.position = new Vector3(0, -2, 0); floor.transform.localScale = new Vector3(400, 2, 400);
        actor = UnityEngine.Object.Instantiate(prefab);
        character = actor.GetComponent(Type.GetType("LWS.TruckDragonKingdom.DragonCharacter, Assembly-CSharp", true));
        Assert.That(character, Is.Not.Null);
        var catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(CatalogPath); Assert.That(catalog, Is.Not.Null);
        var lookup = new object[] { "dragon-inferno", null };
        Assert.That((bool)catalog.GetType().GetMethod("TryGet").Invoke(catalog, lookup), Is.True);
        truck = new GameObject("Synthetic collider target for real dragon perception"); truck.SetActive(false);
        truckBody = truck.AddComponent<Rigidbody>(); truckBody.useGravity = false;
        var shape = truck.AddComponent<BoxCollider>(); shape.size = new Vector3(2, 2, 4);
        var visuals = new GameObject("Visuals"); visuals.transform.SetParent(truck.transform, false);
        var cab = new GameObject("Cab"); cab.transform.SetParent(visuals.transform, false); var paint = cab.AddComponent<MeshRenderer>();
        var truckType = Type.GetType("LWS.TruckSports.SportsEventTruck, Assembly-CSharp", true);
        eventTruck = truck.AddComponent(truckType); Set(eventTruck, "body", truckBody); Set(eventTruck, "shape", shape);
        Set(eventTruck, "visuals", visuals.transform); Set(eventTruck, "paint", paint);
        var wheelType = truckType.GetField("wheels").FieldType.GetElementType(); Set(eventTruck, "wheels", Array.CreateInstance(wheelType, 0));
        truck.SetActive(true);
        var roster = Array.CreateInstance(truckType, 1); roster.SetValue(eventTruck, 0);
        Call(character, "Prepare", lookup[1], roster, 1, 917);
        // Explicit queries inspect original colliders rather than a mocked sensor.
        var mouth = (Transform)Get(character, "mouth"); var motor = (Component)Get(character, "motor");
        truckBody.position = mouth.position + motor.transform.forward * 30;
        truck.transform.position = truckBody.position; Physics.SyncTransforms();
        Evidence("constructed-before-first-physics-animation-step");
    }
    IEnumerator SettleAndPlaceGenuineTarget()
    {
        // MAnimal reparents its original bound rig on Awake and the configured
        // Animator evaluates in Fixed mode. One yield-null need not include any
        // physics/animation evaluation. Establish the target from the real pose
        // after those native steps; do not write sensor memory or sensor gates.
        var motor=(Component)Get(character,"motor");
        for(int step=0;step<24;step++){Call(motor,"StopMotion");yield return new WaitForFixedUpdate();}
        yield return null;Evidence("settled-before-target-reposition");
        var mouth=(Transform)Get(character,"mouth");truckBody.position=mouth.position+motor.transform.forward*30;
        truckBody.linearVelocity=truckBody.angularVelocity=Vector3.zero;truck.transform.position=truckBody.position;Physics.SyncTransforms();
        Evidence("settled-real-target-thirty-metres-ahead");
    }
    string Evidence(string stage)
    {
        var mouth=(Transform)Get(character,"mouth");var motor=(Component)Get(character,"motor");var body=(Rigidbody)Get(motor,"body");var animator=(Animator)Get(motor,"animator");
        Vector3 target=truckBody.worldCenterOfMass,delta=target-mouth.position;var mask=(LayerMask)Get(character,"obstructionMask");
        var hits=Physics.RaycastAll(mouth.position,delta.normalized,delta.magnitude,mask,QueryTriggerInteraction.Ignore).OrderBy(hit=>hit.distance).ToArray();
        var real=hits.Where(hit=>!hit.transform.IsChildOf(actor.transform)).ToArray();
        string first=real.Length==0?"none":Hierarchy(real[0].transform)+" distance="+real[0].distance.ToString("F4");
        string value="DRAGON_PERCEPTION "+stage+" mouth="+mouth.position.ToString("F4")+" mouthForward="+mouth.forward.ToString("F4")+" motor="+motor.transform.position.ToString("F4")+" body="+body.position.ToString("F4")+" velocity="+body.linearVelocity.ToString("F4")+" facing="+motor.transform.forward.ToString("F4")+" targetBody="+truckBody.position.ToString("F4")+" targetCOM="+target.ToString("F4")+" angle="+Vector3.Angle(motor.transform.forward,delta).ToString("F3")+" range="+delta.magnitude.ToString("F3")+" firstRealHit="+first+" hits="+hits.Length+" prepared="+Get(character,"Prepared")+" paused="+Get(character,"Paused")+" observable="+Call(character,"CanObserveTarget",0)+" stateHash="+animator.GetCurrentAnimatorStateInfo(0).fullPathHash+" normalized="+animator.GetCurrentAnimatorStateInfo(0).normalizedTime.ToString("F4");
        TestContext.WriteLine(value);return value;
    }
    static string Hierarchy(Transform item)=>item.parent==null?item.name:Hierarchy(item.parent)+"/"+item.name;
    void PlaceCover()
    {
        var mouth = (Transform)Get(character, "mouth"); Vector3 delta = truckBody.position - mouth.position;
        cover = GameObject.CreatePrimitive(PrimitiveType.Cube); cover.name = "Actual opaque physical perception occluder";
        cover.transform.position = mouth.position + delta * .5f; cover.transform.rotation = Quaternion.LookRotation(delta);
        cover.transform.localScale = new Vector3(25, 25, 3); Physics.SyncTransforms();
    }

    [UnityTest]
    public IEnumerator HiddenTruckRetainsOnlyItsLastSensedPositionThenMemoryExpires()
    {
        Construct(); yield return SettleAndPlaceGenuineTarget();
        string evidence=Evidence("before-first-real-sight-sample");
        Tick(.2f); Assert.That(Remembered(out var lastSeen), Is.True,evidence);
        Assert.That(Call(character, "CanObserveTarget", 0), Is.EqualTo(true),Evidence("after-first-real-sight-sample"));
        PlaceCover();
        var motor = (Component)Get(character, "motor");
        truckBody.position += motor.transform.forward * 20; truck.transform.position = truckBody.position; Physics.SyncTransforms();
        Assert.That(Call(character, "CanObserveTarget", 0), Is.EqualTo(false),Evidence("real-cover-placed-target-moved"));
        Call(character, "SetIntent", Enum.Parse(character.GetType().Assembly.GetType("LWS.TruckDragonKingdom.DragonCharacterIntent"), "Pursuit"), truckBody.position, 0);
        Tick(.3f); Assert.That(Remembered(out var hiddenMemory), Is.True);
        Assert.That(Vector3.Distance(lastSeen, hiddenMemory), Is.LessThan(.001f), "A hidden truck's new pose cannot refresh perception.");
        Tick((float)Get(character, "memorySeconds") + 1);
        Assert.That(Remembered(out _), Is.False); Assert.That(Get(character, "TargetSlot"), Is.EqualTo(-1));
        yield return null;
    }

    [UnityTest]
    public IEnumerator FiniteRangeNoiseAndEligibilityCannotRevealFarOrExtractedCrews()
    {
        Construct(); yield return SettleAndPlaceGenuineTarget();
        var mouth = (Transform)Get(character, "mouth"); var motor = (Component)Get(character, "motor");
        truckBody.position = mouth.position + motor.transform.forward * 500; truck.transform.position = truckBody.position; Physics.SyncTransforms();
        Call(character, "ReportNoise", 0, truckBody.position, 1f); Assert.That(Remembered(out _), Is.False);
        Call(character, "ReportNoise", 0, new Vector3(float.NaN, 0, 0), 1f); Assert.That(Remembered(out _), Is.False);
        Call(character, "ReportNoise", 0, mouth.position, float.PositiveInfinity); Assert.That(Remembered(out _), Is.False);
        truckBody.position = mouth.position + motor.transform.forward * 30; truck.transform.position = truckBody.position; Physics.SyncTransforms();
        PlaceCover();
        Call(character, "ReportNoise", 0, truckBody.position, 1f); Assert.That(Remembered(out var heard), Is.True);
        Assert.That(Vector3.Distance(heard, truckBody.position), Is.LessThan(.001f));
        character.GetType().GetProperty("TargetEligible").SetValue(character, new Func<int, bool>(_ => false));
        Call(character, "ReportNoise", 0, truckBody.position, 1f); Tick(.3f);
        Assert.That(Remembered(out _), Is.False, "Secured/eliminated crews stay excluded without hiding their trucks.");
        Assert.That(truck.activeSelf, Is.True); yield return null;
    }

    [TearDown]
    public void Cleanup()
    {
        if (character != null) Call(character, "ResetCharacter");
        foreach (var item in new[] { actor, truck, cover, floor }) if (item != null) UnityEngine.Object.DestroyImmediate(item);
        Time.timeScale = 1;
    }

    [UnityTest]
    public IEnumerator SleepingWakeGateDoesNotGrantOmniscienceOrDisableAwakePursuit()
    {
        Construct(); yield return SettleAndPlaceGenuineTarget(); PlaceCover();
        var intentType = character.GetType().Assembly.GetType("LWS.TruckDragonKingdom.DragonCharacterIntent");
        character.GetType().GetProperty("SleepingWakeEligible").SetValue(character, new Func<int, bool>(_ => false));
        Call(character, "SetIntent", Enum.Parse(intentType, "SleepingGuardian"), actor.transform.position, -1);
        Call(character, "ReportNoise", 0, truckBody.position, 1f); Tick(.3f);
        Assert.That(Get(character, "Intent").ToString(), Is.EqualTo("SleepingGuardian"));
        Assert.That(Remembered(out _), Is.True, "Hearing memory remains truthful while the Heist alertness authority withholds waking.");
        Call(character, "SetIntent", Enum.Parse(intentType, "Pursuit"), truckBody.position, 0); Tick(.3f);
        Assert.That(Get(character, "TargetSlot"), Is.EqualTo(0), "The sleeping-only gate cannot prevent a legitimately awake dragon pursuing heard memory.");
        yield return null;
    }

    [UnityTest,Timeout(45000)]
    public IEnumerator ActualClosedEyelidsBlockFreshSightButRetainHearingAndWakeRestoresVision()
    {
        Construct();yield return SettleAndPlaceGenuineTarget();var motor=(Component)Get(character,"motor");
        var lids=actor.GetComponentsInChildren<Component>(true).Single(value=>value!=null&&value.GetType().Name=="DragonEyelidPresentation");Assert.That(lids,Is.Not.Null,"Use the real configured actor with actual surface eyelids.");
        Assert.That(Call(character,"CanObserveTarget",0),Is.EqualTo(true),Evidence("eyes-open-before-real-sleep"));Tick(.2f);Assert.That(Remembered(out var lastSeen),Is.True);
        var actionType=motor.GetType().Assembly.GetType("LWS.TruckDragonKingdom.DragonAnimationAction");object sleep=Enum.Parse(actionType,"Sleep"),wake=Enum.Parse(actionType,"Wake");
        Assert.That(Call(motor,"TryAction",sleep,0f),Is.EqualTo(true),"Only the actual public vendor-mode request may start sleep.");
        float deadline=Time.realtimeSinceStartup+10;
        while((float)Get(lids,"Closure")<.99f&&Time.realtimeSinceStartup<deadline){Call(motor,"StopMotion");yield return new WaitForFixedUpdate();yield return null;}
        Assert.That((bool)Get(lids,"ObservedSleepPlayback"),Is.True);Assert.That((float)Get(lids,"Closure"),Is.GreaterThanOrEqualTo(.99f));Assert.That((bool)Get(lids,"EyesOpenForVision"),Is.False);
        var mouth=(Transform)Get(character,"mouth");truckBody.position=mouth.position+motor.transform.forward*22;truckBody.linearVelocity=truckBody.angularVelocity=Vector3.zero;truck.transform.position=truckBody.position;Physics.SyncTransforms();
        var delta=truckBody.worldCenterOfMass-mouth.position;Assert.That(Vector3.Angle(mouth.forward,delta),Is.LessThan((float)Get(character,"sightAngle")*.5f),"The target must remain inside the real unchanged sight cone while lids close.");
        var realHits=Physics.RaycastAll(mouth.position,delta.normalized,delta.magnitude,(LayerMask)Get(character,"obstructionMask"),QueryTriggerInteraction.Ignore).Where(hit=>!hit.transform.IsChildOf(actor.transform)).OrderBy(hit=>hit.distance).ToArray();
        Assert.That(realHits.Length==0||realHits[0].transform.IsChildOf(truck.transform),Is.True,"Actual clear LOS independently excludes geometry as the reason for lost sight.");
        Assert.That(Call(character,"CanObserveTarget",0),Is.EqualTo(false),Evidence("actual-lids-closed-clear-cone-and-ray"));Tick(.2f);Assert.That(Remembered(out var closedMemory),Is.True);Assert.That(closedMemory,Is.EqualTo(lastSeen),"Closed eyes cannot refresh the last seen position from a moved truck.");
        Call(character,"ReportNoise",0,truckBody.position,1f);Assert.That(Remembered(out var heard),Is.True);Assert.That(Vector3.Distance(heard,truckBody.position),Is.LessThan(.001f),"Actual finite hearing remains valid through closed eyes.");
        float frozenClosure=(float)Get(lids,"Closure");Call(character,"Pause",true);Tick(3f);yield return null;Assert.That((float)Get(lids,"Closure"),Is.EqualTo(frozenClosure));Assert.That(Call(character,"CanObserveTarget",0),Is.EqualTo(false));Call(character,"Pause",false);
        Call(motor,"InterruptAction");deadline=Time.realtimeSinceStartup+8;bool accepted=false;
        while(Time.realtimeSinceStartup<deadline){if(!(bool)Get(motor,"ActionPlaying")&&(bool)Call(motor,"TryAction",wake,0f)){accepted=true;break;}yield return new WaitForFixedUpdate();yield return null;}
        Assert.That(accepted,Is.True,"Actual vendor sleep must release before a real Wake request.");deadline=Time.realtimeSinceStartup+8;bool observedWake=false;
        while(Time.realtimeSinceStartup<deadline)
        {
            observedWake|=(bool)Get(motor,"ActionPlaying")&&Get(motor,"CurrentAction").ToString()=="Wake";
            if(observedWake&&(bool)Get(lids,"EyesOpenForVision"))break;yield return new WaitForFixedUpdate();yield return null;
        }
        Assert.That(observedWake,Is.True);Assert.That((bool)Get(lids,"EyesOpenForVision"),Is.True,"Actual Wake playback must visibly reopen the lids.");
        mouth=(Transform)Get(character,"mouth");truckBody.position=mouth.position+motor.transform.forward*22;truck.transform.position=truckBody.position;Physics.SyncTransforms();
        Assert.That(Call(character,"CanObserveTarget",0),Is.EqualTo(true),Evidence("real-wake-open-eyes-reacquire-clear-target"));Tick(.2f);Assert.That(Remembered(out var reacquired),Is.True);Assert.That(Vector3.Distance(reacquired,truckBody.worldCenterOfMass),Is.LessThan(.001f));
        Call(character,"ResetCharacter");Assert.That(Get(character,"eyelids"),Is.Null,"Reset discards the cached presentation binding before the next Prepare.");
    }
}
#endif
