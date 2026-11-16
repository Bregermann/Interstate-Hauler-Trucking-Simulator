#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
public sealed partial class NativeDragonInputTests
{
    bool coldFireFixture;float ColdMotorScale=>coldFireFixture?7*1.67f:17;float ColdCatalogTarget=>coldFireFixture?36:32;
    bool coldInfernoFixture;IDisposable coldInfernoFixtureLease;int InputParticipantCount=>coldInfernoFixture?1:4;
#pragma warning disable CS0649
    [Serializable]sealed class ColdRuntime{public bool inactiveBeforeFirstAwake,nativeAwakeObserved,nativeInitialized,unitAncestorsRetained,runtimeStateOwnership,authoredStateSetsUntouched,nwhExact,finiteWorldBoxDimensionsPreserved;public Speed[] speeds;}
    [Serializable]sealed class Speed{public float worldTarget,ownedPosition,animationSpeed;public bool otherFieldsExact;}
#pragma warning restore CS0649
    void BeginPristineInfernoFixture(string[] args)
    {
        int at=Array.IndexOf(args,coldFireFixture?"-tsRiderFireColdFixtureEvidence":"-tsRiderColdFixtureEvidence");Assert.That(at>=0&&at+1<args.Length,Is.True,"Actual passed source-closed TEST fixture export evidence required.");var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);coldInfernoFixtureLease=(IDisposable)author.GetMethod(coldFireFixture?"BeginPristineFireMotor1169RuntimeFromActualTestFixture":"BeginPristineInfernoMotor17RuntimeFromActualTestFixture").Invoke(null,new object[]{match,args[at+1]});
    }
    void ObservePristineInfernoFixture()
    {
        if(coldFireFixture){reportName="TruckDragonRiders-PristineFireMotor1169Solo";if(riderCaptureFolder.StartsWith("RidersPristineMotor17Solo",StringComparison.Ordinal))riderCaptureFolder="RidersPristineFireMotor1169Solo"+riderCaptureFolder.Substring("RidersPristineMotor17Solo".Length);}
        var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);string json=(string)author.GetMethod("ObservePristineInfernoMotor17Runtime").Invoke(null,new object[]{coldInfernoFixtureLease});Log("ACTUAL_PRISTINE_FIRST_AWAKE "+json);var row=JsonUtility.FromJson<ColdRuntime>(json);Assert.That(row.inactiveBeforeFirstAwake&&row.nativeAwakeObserved&&row.nativeInitialized&&row.unitAncestorsRetained&&row.runtimeStateOwnership&&row.authoredStateSetsUntouched&&row.nwhExact&&row.finiteWorldBoxDimensionsPreserved,Is.True);Assert.That(row.speeds,Is.Not.Empty);Assert.That(row.speeds.All(speed=>speed.otherFieldsExact&&Mathf.Abs(speed.ownedPosition*ColdMotorScale-speed.worldTarget)<.0001f),Is.True);
    }
    [Serializable]sealed class CruiseSample
    {
        public string phase,source,activeState,speedSet,speedName,currentClips,nextClips,lastContact;
        public int frame,physicalContacts;
        public float fixedTime,realTime,stepSeconds,catalogTarget,commandedExpected,actualThrottle,stamina,scaleFactor,positionModifier,verticalModifier,verticalSmooth,upDownSmooth,angularDegreesPerSecond,actualSpeed;
        public bool flying,core,transition,sprint,actionPlaying,forwardKey,backKey,leftKey,rightKey,actionKey;
        public Vector2 rawDrive,sampledDrive;
        public Vector3 position,velocity,rawMotorInput,movementAxis,targetStep,inertiaStep,heading;
        public Quaternion rotation;
    }
    [Serializable]sealed class CruiseEvidence
    {
        public string scope="ACTUAL_ASSIGNED_KEYBOARD_NEUTRAL_STRAIGHT_75_PERCENT_CRUISE_NOT_FULL_CATALOG_CAPABILITY",status="OBSERVING",error;
        public float catalogTarget,commandedThrottle=.75f,commandedExpected,relativeTolerance=.2f,duration,travel,mean;
        public List<CruiseSample> samples=new List<CruiseSample>();
    }
    CruiseSample ReadCruiseSample(int slot,string phase,PristineCruiseContactObserver contacts,Quaternion previousRotation,float dt)
    {
        var motor=FlightMotor(slot);var animal=Read(motor,"animal");var body=(Rigidbody)Read(motor,"body");var animator=(Animator)Read(motor,"animator");var state=Read(animal,"ActiveState");var speed=Read(animal,"CurrentSpeedModifier");var racer=((Array)Read(match,"racers")).GetValue(slot);
        bool Key(string name)=>keyboard[(UnityEngine.InputSystem.Key)Read(Player(slot),name)].isPressed;
        return new CruiseSample {
            phase=phase,source=Source(slot),frame=Time.frameCount,fixedTime=Time.fixedTime,realTime=Time.realtimeSinceStartup,stepSeconds=dt,
            catalogTarget=(float)Read(Read(motor,"Species"),"flightSpeed"),commandedExpected=(float)Read(Read(motor,"Species"),"flightSpeed")*.75f,
            rawDrive=(Vector2)Call(match,"ReadDrive",slot),sampledDrive=((Vector2[])Read(match,"commands"))[slot],
            forwardKey=Key("forward"),backKey=Key("back"),leftKey=Key("left"),rightKey=Key("right"),actionKey=keyboard[((UnityEngine.InputSystem.Key[])Read(match,"actionKeys"))[slot]].isPressed,
            rawMotorInput=(Vector3)Read(animal,"RawInputAxis"),actualThrottle=((Vector3)Read(animal,"RawInputAxis")).magnitude,
            flying=(bool)Read(motor,"IsFlying"),core=(bool)Read(state,"InCoreAnimation"),transition=animator.IsInTransition(0),sprint=(bool)Read(animal,"Sprint"),actionPlaying=(bool)Read(motor,"ActionPlaying"),
            activeState=state.GetType().Name,speedSet=(string)Read(Read(animal,"CurrentSpeedSet"),"name"),speedName=(string)Read(speed,"name"),
            scaleFactor=(float)Read(animal,"ScaleFactor"),positionModifier=(float)Read(Read(speed,"position"),"Value"),verticalModifier=(float)Read(Read(speed,"Vertical"),"Value"),
            verticalSmooth=(float)Read(animal,"VerticalSmooth"),upDownSmooth=(float)Read(animal,"UpDownSmooth"),movementAxis=(Vector3)Read(animal,"MovementAxis"),
            targetStep=(Vector3)Read(animal,"TargetSpeed"),inertiaStep=(Vector3)Read(animal,"InertiaPositionSpeed"),stamina=(float)Read(Read(racer,"Budget"),"Stamina"),heading=(Vector3)Read(racer,"heading"),
            position=body.position,rotation=body.rotation,velocity=body.linearVelocity,actualSpeed=body.linearVelocity.magnitude,angularDegreesPerSecond=dt>0?Quaternion.Angle(previousRotation,body.rotation)/dt:float.MaxValue,
            currentClips=string.Join(";",animator.GetCurrentAnimatorClipInfo(0).Select(c=>c.clip.name+"@"+c.weight)),nextClips=string.Join(";",animator.GetNextAnimatorClipInfo(0).Select(c=>c.clip.name+"@"+c.weight)),
            physicalContacts=contacts.Count,lastContact=contacts.LastOther
        };
    }
    static bool IsStraightCruiseInput(CruiseSample row)
    {
        return row.source=="Keyboard"&&!row.forwardKey&&!row.backKey&&!row.leftKey&&!row.rightKey&&!row.actionKey
            &&row.rawDrive.sqrMagnitude<.000001f&&row.sampledDrive.sqrMagnitude<.000001f
            &&row.flying&&row.core&&!row.transition&&!row.sprint&&!row.actionPlaying
            &&Mathf.Abs(row.actualThrottle-.75f)<.001f&&Mathf.Abs(row.rawMotorInput.y)<.001f&&Mathf.Abs(row.movementAxis.y)<.001f&&Mathf.Abs(row.upDownSmooth)<.005f
            &&Vector3.Angle(row.rawMotorInput,row.heading)<.1f;
    }
    IEnumerator MeasurePristineInfernoWorldSpeed(int slot)
    {
        // The preceding real input test has already proved assigned drive+turn.
        // Neutral input preserves the same Keyboard owner and issues the game's
        // ordinary .75 cruise command; it does not inject a command/body state.
        Assert.That(Source(slot),Is.EqualTo("Keyboard"));Neutral();yield return null;yield return null;
        var motor=FlightMotor(slot);var body=(Rigidbody)Read(motor,"body");var racer=((Array)Read(match,"racers")).GetValue(slot);var dragon=(Component)Read(racer,"Dragon");
        var observer=body.gameObject.AddComponent<PristineCruiseContactObserver>();observer.OwnDragon=dragon.transform;observer.OwnTruck=Truck(slot).transform;
        Assert.That((string)Read(Read(motor,"Species"),"id"),Is.EqualTo(coldFireFixture?"dragon-fire":"dragon-inferno"));
        var evidence=new CruiseEvidence{catalogTarget=(float)Read(Read(motor,"Species"),"flightSpeed")};Assert.That(evidence.catalogTarget,Is.EqualTo(ColdCatalogTarget));evidence.commandedExpected=evidence.catalogTarget*.75f;
        string folder=Path.Combine(output,"RiderCommandedCruise",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        void Save()=>File.WriteAllText(Path.Combine(folder,"actual-keyboard-commanded-cruise.json"),JsonUtility.ToJson(evidence,true));
        try
        {
            float deadline=Time.realtimeSinceStartup+10,stableSeconds=0,previousTime=Time.fixedTime,previousSpeed=body.linearVelocity.magnitude;var previousRotation=body.rotation;int lastContacts=observer.Count;
            while(stableSeconds<.5f&&Time.realtimeSinceStartup<deadline)
            {
                yield return new WaitForFixedUpdate();float dt=Time.fixedTime-previousTime;
                var row=ReadCruiseSample(slot,"settling",observer,previousRotation,dt);evidence.samples.Add(row);
                bool stable=dt>0&&IsStraightCruiseInput(row)&&row.angularDegreesPerSecond<2&&Mathf.Abs(row.actualSpeed-previousSpeed)/dt<.5f&&observer.Count==lastContacts;
                stableSeconds=stable?stableSeconds+dt:0;previousTime=Time.fixedTime;previousRotation=body.rotation;previousSpeed=row.actualSpeed;lastContacts=observer.Count;
            }
            Save();Assert.That(stableSeconds,Is.GreaterThanOrEqualTo(.5f),"Real neutral keyboard .75 cruise must settle with core Fly, no turning/climb/sprint/avoidance/contact and stable native translation; inspect fixed-step evidence.");
            float start=Time.fixedTime,finish=start+1.5f;Vector3 previous=body.position;previousRotation=body.rotation;previousTime=start;int contactsAtStart=observer.Count;
            while(Time.fixedTime<finish)
            {
                yield return new WaitForFixedUpdate();float dt=Time.fixedTime-previousTime;var row=ReadCruiseSample(slot,"measured",observer,previousRotation,dt);evidence.samples.Add(row);if(evidence.samples.Count%10==0)Save();
                Assert.That(IsStraightCruiseInput(row),Is.True,"Actual observed input/native output must remain straight .75 cruise, without boost/brake/climb/avoidance.");
                Assert.That(row.angularDegreesPerSecond,Is.LessThan(2),"Measured native body must stay straight; do not average a held turning input against catalog speed.");
                Assert.That(observer.Count,Is.EqualTo(contactsAtStart),"No real collision may contaminate the commanded-cruise speed window: "+observer.LastOther);
                evidence.travel+=Vector3.Distance(previous,body.position);previous=body.position;previousRotation=body.rotation;previousTime=Time.fixedTime;
            }
            evidence.duration=Time.fixedTime-start;evidence.mean=evidence.travel/evidence.duration;evidence.status="ACTUAL_75_PERCENT_KEYBOARD_CRUISE_MEASURED_NOT_FULL_CATALOG_CAPABILITY";Save();
            Log("ACTUAL_PRISTINE_NATIVE_WORLD_TRANSLATION meanMps="+evidence.mean+" catalogTarget="+evidence.catalogTarget+" observedThrottle=.75 commandedExpected="+evidence.commandedExpected+" duration="+evidence.duration+" traveled="+evidence.travel+" straightNeutralKeyboard=true sprintFalse=true fullCatalogCapabilityAccepted=false evidence="+folder);
            Assert.That(evidence.mean,Is.InRange(evidence.commandedExpected*.8f,evidence.commandedExpected*1.2f),"Actual vendor translation must match the real .75 keyboard cruise with the original relative20% tolerance; full catalog capability remains separately unverified.");ObservePristineInfernoFixture();
        }
        finally{Save();if(observer!=null)UnityEngine.Object.DestroyImmediate(observer);}
    }
    void ValidatePristineInfernoCab(string file,int height)
    {
        ObservePristineInfernoFixture();var actual=JsonUtility.FromJson<CabTransientViews>(File.ReadAllText(Path.ChangeExtension(file,".actual-skin-sightlines.json")));Assert.That(actual.views.Length,Is.EqualTo(1),"SOLO scope; never claim multiplayer coverage.");foreach(var view in actual.views){Assert.That(view.owner,Is.Zero);Assert.That(view.exactOwnedStatePreserved,Is.True);Assert.That(view.actualBaseClips,Does.Contain("TRANSIENT_CAB_"));Assert.That(view.pureConnectedNeckCandidates.Length,Is.GreaterThan(0));var geometry=view.pureConnectedNeckCandidates[0];Assert.That(geometry.actualTrueBakeTriangleBoxIntersections,Is.Zero);Assert.That(geometry.triangleBoxIntersections,Is.Zero);var cab=geometry.sameFrameCabRays.Single(value=>value.name=="ACTUAL_CAMERA_ROUND_TRIP_CONTROL");var center=cab.centralWindscreenRays.Single(ray=>(ray.viewport-new Vector2(.5f,.5f)).sqrMagnitude<.000001f);Assert.That(center.ownDragon&&center.headNeckWeight>=.5f,Is.False);Assert.That(cab.ownHeadNeckDominantHits,Is.LessThanOrEqualTo(5));Log("ACTUAL_PRISTINE_CAB species="+(coldFireFixture?"dragon-fire":"dragon-inferno")+" owner=0 height="+height+" headHits="+cab.ownHeadNeckDominantHits+"/25 centerClear=true originalCameraPreserved=true NOT_HUMAN_COURSE_OR_MULTIPLAYER_ACCEPTANCE");}
    }
    [UnityTest,Timeout(600000)]public IEnumerator RidersPristineFireMotor1169NaturallyAwakensAndPreservesActualCommandedCruiseCabBreathAndSoloRematches()
    {
        coldFireFixture=true;
        try{yield return Exercise("TruckDragonRiders","LWS.TruckDragonRiders.DragonRidersMatch",focusedRiderPresentation:true,pristineInferno:true);}
        finally{coldFireFixture=false;}
    }
    [UnityTest,Timeout(600000)]public IEnumerator RidersPristineMotor17NaturallyAwakensAndPreservesActualWorldSpeedCabBreathAndSoloRematches()
    {yield return Exercise("TruckDragonRiders","LWS.TruckDragonRiders.DragonRidersMatch",focusedRiderPresentation:true,pristineInferno:true);}
}
public sealed class PristineCruiseContactObserver : MonoBehaviour
{
    public Transform OwnDragon,OwnTruck;public int Count {get;private set;}public string LastOther {get;private set;}
    void Observe(Collision collision)
    {
        var other=collision.collider;if(other==null)return;
        bool sameAssembly=OwnDragon!=null&&other.transform.IsChildOf(OwnDragon)||OwnTruck!=null&&other.transform.IsChildOf(OwnTruck);
        Count++;LastOther=other.name+" sameOriginalAssembly="+sameAssembly+" frame="+Time.frameCount+" impulse="+collision.impulse+" relativeVelocity="+collision.relativeVelocity;
    }
    void OnCollisionEnter(Collision collision)=>Observe(collision);
    void OnCollisionStay(Collision collision)=>Observe(collision);
}
#endif
