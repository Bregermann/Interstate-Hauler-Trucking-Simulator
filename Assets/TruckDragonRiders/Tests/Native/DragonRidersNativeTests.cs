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
using UnityEngine.TestTools;

public sealed class DragonRidersNativeTests
{
    const string ScenePath="Assets/TruckDragonRiders/Scenes/TruckDragonRiders.unity";
    Component match;
    IDisposable lease;
    GameObject impact;
    string evidence;
    readonly List<RaceObservation> trace=new List<RaceObservation>();
    readonly List<FinishClearance> finishClearance=new List<FinishClearance>();
    [Serializable] sealed class CrewObservation {public int slot,passed,nextGate;public string phase,species,vendorState,action;public bool mounted,needsApproach,flying,grounded,paused,actionPlaying;public Vector3 truckPosition,dragonPosition,velocity,heading,requestedFlightHeading,gateCenter,gateNormal,passageCenter,passageBoundsExtents;public float stamina,brakeCharge,stability,signedGateDistance,radialGateDistance,gateRadius,usedPassageRadius;}
    [Serializable] sealed class RaceObservation {public string phase;public int matchSeed;public float realSeconds,clock,countdown,timeScale;public CrewObservation[] crews;}
    [Serializable]sealed class FinishClearance{public int slot;public float firstFinishedClock,othersActiveSeconds,requiredGeometricClearance,maximumDistanceFromFinalGate,maximumActualTravel;public Vector3 firstActualBody,latestActualBody,finalGate;public bool physicallyClearedWhileOthersActive;}
    [Serializable] sealed class RaceEvidence {public string scope="Read-only original saved-scene AI flight observations; no injected gates, movement, scoring, clocks or outcomes. Native controlled images, not Windows/hardware/human acceptance.";public List<RaceObservation> trace;public FinishClearance[] finishedCrewClearance;}
    [Serializable]sealed class ProjectileEvidence{public string scope="REAL_DRAGON_BODY_COLLISION_DETACHES_MOUNTED_ORIGINAL_NWH_NOT_DIRECT_TRUCK_ONLY_COLLISION_CERTIFICATION",target,actualCollider,phase;public bool initiallySeparated,actualTargetContact;public int actualTargetContacts;public Vector3 targetCenter,observedDragonVelocity,projectileStart,predictedContact,projectileVelocity;public Bounds originalTargetBounds;public float clock,flightLeadSeconds,initialSeparation,maximumActualNormalSpeed;public List<ProjectileFrame> trajectory=new List<ProjectileFrame>();public DragonRiderProjectileContactObservation.Contact[] observedContacts;}
    [Serializable]sealed class ProjectileFrame{public int frame,targetContacts;public float realtime,clock,closestTargetPointDistance;public string phase;public Vector3 projectilePosition,projectileVelocity,dragonBodyPosition,observedDragonVelocity,targetClosestPointToProjectile;public Bounds actualTargetBounds;public bool projectileStillExists,targetStillEnabled,actualTargetContact,actualOverlap;}
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static object Field(object source,string name)=>source.GetType().GetField(name).GetValue(source);
    static object Property(object source,string name)=>source.GetType().GetProperty(name).GetValue(source);
    static object Call(object source,string name,params object[] args)=>source.GetType().GetMethod(name).Invoke(source,args);
    IEnumerator Begin()
    {
        Assert.That(Application.isPlaying,Is.True,"Run this fixture in actual native PlayMode.");
        lease=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"DragonRiders"+Guid.NewGuid().ToString("N")});
        Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath,new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("LWS.TruckDragonRiders.DragonRidersMatch"));Assert.That(match,Is.Not.Null);
        var setup=Field(match,"setup");setup.GetType().GetField("count").SetValue(setup,4);match.GetType().GetField("option").SetValue(match,0);
        Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);
    }
    void Observe(string phase,float started)
    {
        if(evidence==null)
        {
            string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");
            Assert.That(at>=0&&at+1<args.Length,Is.True,"Explicit retained native evidence folder required.");
            evidence=Path.Combine(args[at+1],"RidersActualFlight",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"));Directory.CreateDirectory(evidence);
        }
        var rules=Property(match,"rules");var racers=(Array)Field(match,"racers");var passed=(int[])Field(rules,"Passed");
        var observation=new RaceObservation{phase=phase,matchSeed=(int)Property(match,"MatchSeed"),realSeconds=Time.realtimeSinceStartup-started,clock=(float)Property(match,"Clock"),countdown=(float)Property(match,"Countdown"),timeScale=Time.timeScale,
            crews=racers.Cast<object>().Select((racer,slot)=>ObserveCrew(rules,racer,slot,passed[slot])).ToArray()};
        trace.Add(observation);File.WriteAllText(Path.Combine(evidence,"observations.json"),JsonUtility.ToJson(new RaceEvidence{trace=trace,finishedCrewClearance=finishClearance.ToArray()},true));
        TestContext.WriteLine("ACTUAL_RIDERS "+phase+" clock="+observation.clock.ToString("F2")+" "+string.Join(" | ",observation.crews.Select(crew=>crew.slot+":"+crew.phase+" gate="+crew.passed+" position="+crew.truckPosition.ToString("F2"))));
    }
    static CrewObservation ObserveCrew(object rules,object racer,int slot,int passed)
    {
        var dragon=Property(racer,"Dragon");var motor=Field(dragon,"motor");var body=(Rigidbody)Field(motor,"body");var budget=Property(racer,"Budget");
        int next=(int)Call(rules,"NextGate",slot);var center=(Vector3)Call(rules,"GateCenter",next);var normal=(Quaternion)Call(rules,"GateRotation",next)*Vector3.forward;
        var shape=(BoxCollider)Field(Field(racer,"truck"),"shape");var delta=shape.bounds.center-center;
        var activeState=Property(Field(motor,"animal"),"ActiveState");
        return new CrewObservation{slot=slot,passed=passed,nextGate=next,phase=Property(racer,"Phase").ToString(),mounted=(bool)Property(racer,"PhysicallyMounted"),truckPosition=(Vector3)Property(racer,"Position"),dragonPosition=body.position,velocity=body.linearVelocity,heading=body.transform.forward,requestedFlightHeading=(Vector3)Property(racer,"FlightHeading"),
            species=(string)Field(Property(racer,"Species"),"id"),vendorState=activeState?.GetType().Name??"none",flying=(bool)Property(motor,"IsFlying"),grounded=(bool)Property(motor,"IsGrounded"),paused=(bool)Property(dragon,"Paused"),action=Property(motor,"CurrentAction").ToString(),actionPlaying=(bool)Property(motor,"ActionPlaying"),passageCenter=shape.bounds.center,passageBoundsExtents=shape.bounds.extents,gateRadius=(float)Call(rules,"GateRadius",next),usedPassageRadius=Mathf.Max(shape.bounds.extents.x,shape.bounds.extents.y),
            stamina=(float)Property(budget,"Stamina"),brakeCharge=(float)Property(budget,"BrakeCharge"),stability=(float)Property(budget,"Stability"),needsApproach=(bool)Call(rules,"NeedsApproach",slot),gateCenter=center,gateNormal=normal,signedGateDistance=Vector3.Dot(delta,normal),radialGateDistance=Vector3.ProjectOnPlane(delta,normal).magnitude};
    }
    void Capture(string phase)
    {
        try{using var visual=new DragonRiderMountedVisualObservation(match,evidence,phase);Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==5).Invoke(null,new object[]{match,1280,720,Path.Combine(evidence,phase+".png"),null});}
        catch(Exception exception){TestContext.WriteLine("ACTUAL_RIDERS_CAPTURE_FAILED "+exception);}
    }
    void InspectSettledResults(Camera ui,int width,int height)
    {
        var menus=(Component)Field(match,"menus");var presentation=menus.GetComponentsInChildren(Runtime("LWS.TruckSports.SportsResultsPresentation"),true).Cast<Component>().Single(value=>ReferenceEquals(Field(value,"menus"),menus));
        var snapshot=Property(presentation,"Snapshot");Assert.That(snapshot,Is.Not.Null);Assert.That(Property(presentation,"Displaying"),Is.EqualTo(true));
        var rows=((IEnumerable)Property(snapshot,"Rows")).Cast<object>().ToArray();var views=(Array)Field(presentation,"rows");var players=(Array)Field(Field(match,"setup"),"players");var rules=Property(match,"rules");var passed=(int[])Field(rules,"Passed");var times=(float[])Field(rules,"FinishTime");int required=(int)Property(rules,"GateCount")*(int)Property(rules,"Laps");
        Assert.That(rows.Length,Is.EqualTo(4));Assert.That(rows.Select(row=>(int)Field(row,"Place")),Is.Ordered.Ascending,"Actual authoritative snapshot must be sorted by finish placing.");Assert.That(rows.Select(row=>(string)Field(row,"ParticipantId")).Distinct().Count(),Is.EqualTo(4));Assert.That(rows.Select(row=>(int)Field(row,"Slot")).OrderBy(value=>value),Is.EqualTo(new[]{0,1,2,3}));Assert.That(passed.Sum(),Is.EqualTo(48));
        for(int index=0;index<rows.Length;index++)
        {
            var row=rows[index];int slot=(int)Field(row,"Slot");var view=views.GetValue(index);Assert.That(Field(row,"ParticipantId"),Is.EqualTo(Field(players.GetValue(slot),"participantId")));Assert.That(passed[slot],Is.EqualTo(required));Assert.That(Convert.ToDouble(Field(row,"Measurement")),Is.EqualTo(times[slot]).Within(.00001));Assert.That((string)Field(row,"Detail"),Does.StartWith(required+" / "+required+" GATES"));Assert.That((int)Field(row,"Place"),Is.EqualTo(Call(match,"ResultPlace",slot)));
            var rect=(RectTransform)Field(view,"rect");Assert.That(rect.gameObject.activeInHierarchy,Is.True);Assert.That(((CanvasGroup)Field(view,"visibility")).alpha,Is.EqualTo(1).Within(.00001));Assert.That(Property(Field(view,"value"),"text"),Is.EqualTo(Property(row,"Value")));Assert.That(Property(Field(view,"place"),"text"),Is.EqualTo("#"+Field(row,"Place")));Assert.That(Property(Field(view,"name"),"text"),Is.EqualTo(((string)Field(row,"DisplayName")).Replace("<","‹").Replace(">","›")));Assert.That(Property(Field(view,"detail"),"text"),Is.EqualTo(((string)Field(row,"Detail")).Replace("<","‹").Replace(">","›")));var corners=new Vector3[4];rect.GetWorldCorners(corners);foreach(var corner in corners){var pixel=RectTransformUtility.WorldToScreenPoint(ui,corner);Assert.That(pixel.x,Is.InRange(-1f,width+1f));Assert.That(pixel.y,Is.InRange(-1f,height+1f));}
        }
        TestContext.WriteLine("ACTUAL_SETTLED_RESULTS "+width+"x"+height+" ordinaryUnscaledAnimationWait=true ids=["+string.Join(",",rows.Select(row=>Field(row,"ParticipantId")))+"] actualGates="+passed.Sum()+" rowAlphas=1 authoritativeTimes=true");
    }
    void CaptureSettledResults()
    {
        foreach(int height in new[]{720,1080})
        {
            int width=height==720?1280:1920;Action<Camera> inspect=ui=>InspectSettledResults(ui,width,height);string path=Path.Combine(evidence,"settled-results-"+width+"x"+height+".png");Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==5).Invoke(null,new object[]{match,width,height,path,inspect});Assert.That(new FileInfo(path).Length,Is.GreaterThan(2000));
        }
    }
    [UnityTest,Timeout(1500000)]
    public IEnumerator FourActualAiMountsClearTheOrderedCourseThenResetForRematch()
    {
        yield return Begin();
        var racers=(Array)Field(match,"racers");var rules=Property(match,"rules");
        finishClearance.Clear();
        float start=Time.realtimeSinceStartup;int actualGateEvents=0;float nextObservation=start;int captures=0;
        var lastPass=new int[racers.Length];var progressedAt=Enumerable.Repeat(start,racers.Length).ToArray();
        while(!(bool)Property(match,"Finished")&&Time.realtimeSinceStartup-start<1200)
        {
            if(Time.realtimeSinceStartup>=nextObservation){Observe("RACING",start);nextObservation=Time.realtimeSinceStartup+10;if(captures<3){Capture("racing-"+captures);captures++;}}
            if(Time.realtimeSinceStartup-start>25)
                foreach(var racer in racers)
                {
                    var dragon=Property(racer,"Dragon");Assert.That(dragon,Is.Not.Null);
                    var motor=Field(dragon,"motor");var animator=(Animator)Field(motor,"animator");
                    string phase=Property(racer,"Phase").ToString();
                    Assert.That(animator.runtimeAnimatorController,Is.Not.Null,"The real configured controller must remain bound.");
                    if(phase!="Finished"&&phase!="Retired")Assert.That(animator.enabled,Is.True,"An active racing/rescuing dragon must retain real animation; finished crews legitimately pause while others continue.");
                }
            var passed=(int[])Field(rules,"Passed");actualGateEvents=0;foreach(int value in passed)actualGateEvents+=value;
            bool unfinishedStillActive=racers.Cast<object>().Any(racer=>Property(racer,"Phase").ToString()!="Finished"&&Property(racer,"Phase").ToString()!="Retired");
            if(unfinishedStillActive)for(int slot=0;slot<racers.Length;slot++)
            {
                var racer=racers.GetValue(slot);if(Property(racer,"Phase").ToString()!="Finished")continue;var dragon=Property(racer,"Dragon");var actualBody=(Rigidbody)Field(Field(dragon,"motor"),"body");float clock=(float)Property(match,"Clock");var record=finishClearance.FirstOrDefault(value=>value.slot==slot);
                if(record==null)
                {
                    var gates=(Array)Field(match,"gates");Vector3 finalGate=(Vector3)Call(rules,"GateCenter",gates.Length-1);float radius=(float)Call(rules,"GateRadius",gates.Length-1);var solids=((Collider[])Field(dragon,"physicalColliders")).Where(value=>value!=null&&value.enabled&&!value.isTrigger).ToArray();Assert.That(solids,Is.Not.Empty);record=new FinishClearance{slot=slot,firstFinishedClock=clock,firstActualBody=actualBody.position,finalGate=finalGate,requiredGeometricClearance=radius+solids.Max(value=>value.bounds.extents.magnitude)+10};finishClearance.Add(record);
                }
                record.latestActualBody=actualBody.position;record.othersActiveSeconds=clock-record.firstFinishedClock;record.maximumActualTravel=Mathf.Max(record.maximumActualTravel,Vector3.Distance(record.firstActualBody,actualBody.position));record.maximumDistanceFromFinalGate=Mathf.Max(record.maximumDistanceFromFinalGate,Vector3.Distance(record.finalGate,actualBody.position));record.physicallyClearedWhileOthersActive|=record.maximumActualTravel>10&&record.maximumDistanceFromFinalGate>record.requiredGeometricClearance;
            }
            for(int slot=0;slot<racers.Length;slot++)
            {
                if(passed[slot]!=lastPass[slot]){lastPass[slot]=passed[slot];progressedAt[slot]=Time.realtimeSinceStartup;}
                string phase=Property(racers.GetValue(slot),"Phase").ToString();
                if(phase!="Finished"&&phase!="Retired"&&Time.realtimeSinceStartup-progressedAt[slot]>120)
                {Observe("NO_PHYSICAL_GATE_PROGRESS",start);Capture("no-gate-progress-crew-"+slot);Assert.Fail("Actual crew "+slot+" made no physical ordered-gate progress for120real seconds; inspect retained original flight trace. No recovery, passage or result was injected.");}
            }
            yield return new WaitForSeconds(.25f);
        }
        Observe("TERMINAL_OR_DEADLINE",start);
        // Results use real unscaled DOTween entry/sort/value animations. Observe
        // their natural completion rather than capturing the intermediate rows.
        if((bool)Property(match,"Finished"))yield return new WaitForSecondsRealtime(1.1f);
        Capture("terminal-or-deadline");
        actualGateEvents=((int[])Field(rules,"Passed")).Sum();
        Assert.That((bool)Property(match,"Finished"),Is.True,"No injected result: the original AI must complete or legitimately retire under the real rules.");
        Assert.That(actualGateEvents,Is.EqualTo(((Array)Field(match,"gates")).Length*racers.Length),"All four crews must physically complete every original ordered gate in the configured single lap.");
        var times=(float[])Field(rules,"FinishTime");foreach(float value in times)Assert.That(value,Is.GreaterThan(0),"A retirement is not an actual racing finish.");
        foreach(var record in finishClearance.Where(value=>value.othersActiveSeconds>=5))Assert.That(record.physicallyClearedWhileOthersActive,Is.True,"An early finished original vendor body must physically clear the final gate while other crews still race; frozen bodies cannot obstruct it.");
        TestContext.WriteLine("ACTUAL_FINISH_CLEARANCE "+string.Join(" | ",finishClearance.Select(value=>value.slot+":othersActive="+value.othersActiveSeconds+",bodyTravel="+value.maximumActualTravel+",gateDistance="+value.maximumDistanceFromFinalGate+",requiredGeometry="+value.requiredGeometricClearance+",cleared="+value.physicallyClearedWhileOthersActive)));
        CaptureSettledResults();
        Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return new WaitForFixedUpdate();
        foreach(int value in (int[])Field(rules,"Passed"))Assert.That(value,Is.Zero);
        foreach(float value in (float[])Field(rules,"Penalty"))Assert.That(value,Is.Zero);
        Assert.That((float)Property(match,"Countdown"),Is.GreaterThan(0));
        foreach(var racer in racers)Assert.That((bool)Property(racer,"PhysicallyMounted"),Is.True,"The actual full NWH crew must be reattached on rematch.");
        Observe("ORDINARY_REMATCH",start);Capture("ordinary-rematch");
    }
    [UnityTest,Timeout(180000)]
    public IEnumerator ActualDynamicImpactDetachesTruckAndObservedGripAnimationRescuesIt()
    {
        yield return Begin();yield return new WaitForSeconds(12);
        var racer=((Array)Field(match,"racers")).GetValue(0);var dragon=Property(racer,"Dragon");var motor=Field(dragon,"motor");var dragonBody=(Rigidbody)Field(motor,"body");
        using var rescueTrace=new DragonRiderRescueObservation(match,racer);rescueTrace.Sample("BEFORE_ACTUAL_DYNAMIC_IMPACT",true,true);
        var truck=(Component)Field(racer,"truck");var body=(Rigidbody)Field(truck,"body");float mass=body.mass;int originalBodies=truck.GetComponentsInChildren<Rigidbody>(true).Length;
        var saddle=((Component)dragon).GetComponent(Runtime("LWS.TruckDragonRiders.DragonRiderSaddle"));Assert.That(saddle,Is.Not.Null);var rigging=Field(saddle,"rigging");if(rigging!=null)Assert.That((bool)Property(rigging,"UpperTiesVisible"),Is.True,"Authored cargo ties should visibly hold the actually mounted truck.");
        Assert.That((bool)Property(racer,"PhysicallyMounted"),Is.True);
        // This is a real physics fixture, not a call to Dislodge/ActionContact.
        impact=GameObject.CreatePrimitive(PrimitiveType.Cube);impact.name="Native actual airborne impact fixture";impact.transform.localScale=new Vector3(9,9,9);
        var target=((Collider[])Field(dragon,"physicalColliders")).Where(collider=>collider!=null&&collider.enabled&&!collider.isTrigger&&collider.gameObject.activeInHierarchy).OrderByDescending(collider=>collider.bounds.size.sqrMagnitude).First();var targetBounds=target.bounds;Vector3 right=dragonBody.rotation*Vector3.right;float targetExtent=Vector3.Dot(new Vector3(Mathf.Abs(right.x),Mathf.Abs(right.y),Mathf.Abs(right.z)),targetBounds.extents),projectileExtent=4.5f*(Mathf.Abs(right.x)+Mathf.Abs(right.y)+Mathf.Abs(right.z));float distance=targetExtent+projectileExtent+4,flight=distance/45;
        impact.transform.position=targetBounds.center+right*distance;Vector3 observedVelocity=(Vector3)Property(motor,"Velocity"),predictedContact=targetBounds.center+observedVelocity*flight;
        var projectile=impact.AddComponent<Rigidbody>();projectile.mass=2500;projectile.useGravity=false;projectile.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        projectile.linearVelocity=(predictedContact-impact.transform.position)/flight;var contact=impact.AddComponent<DragonRiderProjectileContactObservation>();contact.target=target;bool overlap=Physics.ComputePenetration(impact.GetComponent<Collider>(),impact.transform.position,impact.transform.rotation,target,target.transform.position,target.transform.rotation,out _,out _);
        var shot=new ProjectileEvidence{target=target.transform.root.name+"/"+target.name,clock=(float)Property(match,"Clock"),targetCenter=targetBounds.center,originalTargetBounds=targetBounds,observedDragonVelocity=observedVelocity,projectileStart=impact.transform.position,predictedContact=predictedContact,projectileVelocity=projectile.linearVelocity,flightLeadSeconds=flight,initialSeparation=distance-targetExtent-projectileExtent,initiallySeparated=!overlap};var arguments=Environment.GetCommandLineArgs();int outputAt=Array.IndexOf(arguments,"-tsDragonOutput");Assert.That(outputAt,Is.GreaterThanOrEqualTo(0));string shotFolder=Path.Combine(arguments[outputAt+1],"RidersActualRescueProjectile");Directory.CreateDirectory(shotFolder);string shotFile=Path.Combine(shotFolder,DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+".json");
        void SampleShot()
        {
            bool exists=projectile!=null;var position=exists?projectile.position:shot.projectileStart;bool targetExists=target!=null&&target.enabled&&target.gameObject.activeInHierarchy;var closest=targetExists?target.ClosestPoint(position):Vector3.zero;
            shot.trajectory.Add(new ProjectileFrame{frame=Time.frameCount,realtime=Time.realtimeSinceStartup,clock=(float)Property(match,"Clock"),phase=Property(racer,"Phase").ToString(),projectileStillExists=exists,projectilePosition=position,projectileVelocity=exists?projectile.linearVelocity:Vector3.zero,dragonBodyPosition=dragonBody.position,observedDragonVelocity=(Vector3)Property(motor,"Velocity"),actualTargetBounds=targetExists?target.bounds:default,targetClosestPointToProjectile=closest,closestTargetPointDistance=targetExists?Vector3.Distance(position,closest):-1,targetStillEnabled=targetExists,targetContacts=contact!=null?contact.TargetContactCount:shot.actualTargetContacts,actualTargetContact=contact!=null?contact.ActualTargetContact:shot.actualTargetContact,actualOverlap=exists&&targetExists&&Physics.ComputePenetration(impact.GetComponent<Collider>(),impact.transform.position,impact.transform.rotation,target,target.transform.position,target.transform.rotation,out _,out _)});
        }
        void RecordShot(){if(contact!=null){shot.actualTargetContact=contact.ActualTargetContact;shot.actualTargetContacts=contact.TargetContactCount;shot.maximumActualNormalSpeed=contact.MaximumTargetNormalSpeed;shot.actualCollider=contact.ActualCollider;shot.observedContacts=contact.ObservedContacts;}shot.phase=Property(racer,"Phase").ToString();File.WriteAllText(shotFile,JsonUtility.ToJson(shot,true));}
        // Preserve real launch/trajectory/contact facts even when an assertion
        // rejects this lab attempt. This never redirects the flying projectile.
        try
        {
        SampleShot();RecordShot();Assert.That(shot.initiallySeparated&&shot.initialSeparation>=3.9f,Is.True,"The real projectile starts outside the original collider, never as an overlap-induced synthetic impact.");
        float deadline=Time.realtimeSinceStartup+8;
        while(Property(racer,"Phase").ToString()=="Mounted"&&Time.realtimeSinceStartup<deadline){rescueTrace.Sample("AWAITING_ACTUAL_DETACH");yield return new WaitForFixedUpdate();SampleShot();}
        RecordShot();Assert.That(shot.actualTargetContact,Is.True,"A real observed original dragon-body contact must precede collision-driven detachment.");Assert.That(shot.maximumActualNormalSpeed,Is.GreaterThan(13));
        rescueTrace.Sample("AFTER_ACTUAL_DYNAMIC_IMPACT",true,true);
        Assert.That(Property(racer,"Phase").ToString(),Is.Not.EqualTo("Mounted"),"The actual dynamic collision must reach the original dislodging rule.");
        Assert.That(body.isKinematic,Is.False,"The real NWH root must physically detach.");
        if(rigging!=null)Assert.That((bool)Property(rigging,"UpperTiesVisible"),Is.False,"Actual dislodgement must release visual cargo ties immediately.");
        Assert.That(body.mass,Is.EqualTo(mass));Assert.That(truck.GetComponentsInChildren<Rigidbody>(true).Length,Is.EqualTo(originalBodies));
        UnityEngine.Object.Destroy(impact);impact=null;
        yield return new WaitForSeconds(.5f);Assert.That((bool)Property(racer,"ChuteOpen"),Is.True);
        deadline=Time.realtimeSinceStartup+55;
        while((int)Property(racer,"SuccessfulRescues")==0&&Time.realtimeSinceStartup<deadline){rescueTrace.Sample("ACTUAL_RESCUE_PROGRESS");yield return new WaitForFixedUpdate();}
        // Physics announces the genuine rescue in Step. Observe its rendered
        // result after the existing LateUpdate attachment/presentation phase.
        // No event, pose, ownership or score is injected to make this visible.
        yield return null;
        rescueTrace.Sample("ACTUAL_RESCUE_TERMINAL_OR_DEADLINE",true,true);
        Assert.That((int)Property(racer,"SuccessfulRescues"),Is.GreaterThan(0),"A timed checkpoint fallback cannot substitute for observed Grab/Carry contact rescue.");
        Assert.That((bool)Property(racer,"PhysicallyMounted"),Is.True);Assert.That(body.mass,Is.EqualTo(mass));
        if(rigging!=null)Assert.That((bool)Property(rigging,"UpperTiesVisible"),Is.True,"Only genuine successful saddle remount restores the cargo ties.");
        Assert.That(truck.GetComponentsInChildren<Rigidbody>(true).Length,Is.EqualTo(originalBodies));
        }
        finally
        {
            // A failed contact/detach/rescue assertion keeps its original NUnit
            // failure; diagnostic I/O must not replace it with a second error.
            try{if(projectile!=null)SampleShot();RecordShot();}catch(Exception error){TestContext.WriteLine("PROJECTILE_RECEIPT_WRITE_FAILED "+error);}
        }
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if(impact!=null)UnityEngine.Object.Destroy(impact);
        if(match!=null)Call(match,"SetupScreen");
        var current=SceneManager.GetActiveScene();var empty=SceneManager.CreateScene("Dragon Riders native cleanup");SceneManager.SetActiveScene(empty);
        if(current.IsValid()&&current.isLoaded)yield return SceneManager.UnloadSceneAsync(current);
        lease?.Dispose();lease=null;
        Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
        evidence=null;trace.Clear();
    }
}
#endif
