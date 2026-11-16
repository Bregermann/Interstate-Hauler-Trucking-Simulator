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

// Actual saved ground modes and normal setup/start/rematch only. No body pose,
// velocity, contact, score, countdown, route or gameplay callback injection.
public sealed class DragonGroundStartNativeTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    Component match;
    object[] trucks;
    GameObject isolation;
    IDisposable preferences;
    string output,sceneName;
    readonly List<Attempt> attempts=new List<Attempt>();
    [Serializable] public sealed class Wheel
    {
        public string name,support;
        public bool enabled,grounded,supportIsTruck;
        public Vector3 position,hit,normal;
        public float springLength,springMaximum,springCompression;
    }
    [Serializable] public sealed class Crew
    {
        public int slot,bodyId;
        public string root;
        public bool active,powered,vehicleEnabled,motorGrounded;
        public Vector3 position,velocity,angularVelocity,up;
        public Quaternion rotation;
        public float handbrake;
        public Wheel[] wheels;
        public string[] physicalOverlaps;
    }
    [Serializable] public sealed class Sample
    {
        public string phase;
        public float intro,clock,realSeconds;
        public Crew[] crews;
    }
    [Serializable] public sealed class Candidate
    {
        public int slot;
        public string anchor,root;
        public Vector3 anchorPosition,candidatePosition,boxCenter,boxHalf;
        public Quaternion candidateRotation;
        public string[] supportHits,overlaps;
    }
    [Serializable] public sealed class Attempt
    {
        public int seed,rotation,samples,fullySupportedSamples;
        public string matchId,exception,region;
        public bool started,reachedGo,passed;
        public int[] buckets,truckIds,cargoIds;
        public float[] maximumVerticalSpeed,maximumAngularSpeed,maximumHeightExcursion,minimumUp;
        public Candidate[] candidates;
        public List<Sample> trace=new List<Sample>();
        public List<string> failures=new List<string>();
    }
    [Serializable] sealed class Evidence
    {
        public string scope,scene,status;
        public Attempt[] attempts;
    }
    static Type Runtime(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(assembly=>assembly.GetType(name,false)).First(type=>type!=null);
    static object Read(object instance,string name)
    {
        for(Type type=instance.GetType();type!=null;type=type.BaseType)
        {var field=type.GetField(name,Any|BindingFlags.DeclaredOnly);if(field!=null)return field.GetValue(instance);var property=type.GetProperty(name,Any|BindingFlags.DeclaredOnly);if(property!=null)return property.GetValue(instance);}
        throw new MissingMemberException(instance.GetType().FullName,name);
    }
    static object Call(object instance,string name,params object[] args)=>instance.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(instance,args);
    static object[] Items(object instance,string name)=>((Array)Read(instance,name)).Cast<object>().ToArray();
    static Rigidbody Body(object truck)=>(Rigidbody)Read(truck,"body");
    static string RootPath(Transform value){string path=value.name;while(value.parent!=null){value=value.parent;path=value.name+"/"+path;}return path;}
    static string ColliderDescription(Collider collider)=>RootPath(collider.transform)+" active="+collider.gameObject.activeInHierarchy+" attachedBody="+(collider.attachedRigidbody==null?"none":RootPath(collider.attachedRigidbody.transform))+" bounds="+collider.bounds;
    string StartRegion()=>(string)Read(match,"startingRegionId");
    void Save(string status)
    {
        if(output==null)return;
        File.WriteAllText(Path.Combine(output,"evidence.json"),JsonUtility.ToJson(new Evidence{scene=sceneName,status=status,scope="Actual saved-scene NWH four-crew starts across ordinary fixedSeed1..4 configurations and rematches. Normal timeScale1; original SetupScreen/AllBots/StartMatch, actual wheel/support/countdown observations. Controlled renders only; no Windows/performance/human/device acceptance.",attempts=attempts.ToArray()},true));
    }
    Crew ObserveCrew(object truck,int slot,bool detailed=true)
    {
        var root=(Component)truck;var body=Body(truck);var motor=Read(truck,"tractorMotor");var vehicle=(Behaviour)Read(motor,"vehicle");
        var shape=(BoxCollider)Read(truck,"shape");var half=Vector3.Scale(shape.size*.5f,root.transform.lossyScale);var center=shape.transform.TransformPoint(shape.center);
        return new Crew{slot=slot,bodyId=body.GetInstanceID(),root=detailed?RootPath(root.transform):root.name,active=root.gameObject.activeInHierarchy,powered=(bool)Read(truck,"Powered"),vehicleEnabled=vehicle.enabled,motorGrounded=(bool)Read(motor,"Grounded"),position=body.position,rotation=body.rotation,velocity=body.linearVelocity,angularVelocity=body.angularVelocity,up=root.transform.up,
            handbrake=(float)Read(Read(Read(vehicle,"input"),"states"),"handbrake"),
            physicalOverlaps=detailed?Physics.OverlapBox(center,half,shape.transform.rotation,~0,QueryTriggerInteraction.Ignore).Where(collider=>!collider.transform.IsChildOf(root.transform)).Take(24).Select(ColliderDescription).ToArray():Array.Empty<string>(),
            wheels=Items(truck,"wheels").Select(value=>{var wheel=(Behaviour)value;var collider=(Collider)Read(value,"HitCollider");return new Wheel{name=detailed?RootPath(wheel.transform):wheel.name,enabled=wheel.enabled,grounded=(bool)Read(value,"IsGrounded"),position=(Vector3)Read(value,"WheelPosition"),hit=(Vector3)Read(value,"HitPoint"),normal=(Vector3)Read(value,"HitNormal"),support=collider==null?"none":detailed?ColliderDescription(collider):collider.name,supportIsTruck=collider!=null&&collider.GetComponentInParent(Runtime("LWS.TruckSports.SportsEventTruck"))!=null,springLength=(float)Read(value,"SpringLength"),springMaximum=(float)Read(value,"SpringMaxLength"),springCompression=(float)Read(value,"SpringCompression")};}).ToArray()};
    }
    Sample Snapshot(Attempt attempt,string phase,float since)
    {
        var sample=new Sample{phase=phase,intro=(float)Read(match,"Intro"),clock=(float)Read(match,"Clock"),realSeconds=Time.realtimeSinceStartup-since,crews=trucks.Select((truck,index)=>ObserveCrew(truck,index)).ToArray()};attempt.trace.Add(sample);
        TestContext.WriteLine("GROUND_START "+sceneName+" seed="+attempt.seed+" phase="+phase+" intro="+sample.intro+" clock="+sample.clock+" "+string.Join(" | ",sample.crews.Select(crew=>"crew"+crew.slot+" "+crew.position.ToString("F3")+" wheels="+crew.wheels.Count(wheel=>wheel.grounded)+"/"+crew.wheels.Length+" up="+crew.up.y.ToString("F3")+" vy="+crew.velocity.y.ToString("F3")+" spin="+crew.angularVelocity.magnitude.ToString("F3"))));Save("RUNNING");return sample;
    }
    Candidate[] PlacementWitnesses(int seed)
    {
        var world=Read(match,"world");string region;
        try{region=StartRegion();}catch{return Array.Empty<Candidate>();}
        if(string.IsNullOrEmpty(region))return Array.Empty<Candidate>();
        var starts=Items(Call(world,"Region",region),"truckStarts").Cast<Transform>().ToArray();if(starts.Length<4)return Array.Empty<Candidate>();int rotation=(int)((uint)seed%4);var results=new List<Candidate>();
        for(int slot=0;slot<4;slot++)
        {
            var truck=(Component)trucks[slot];var shape=(BoxCollider)Read(truck,"shape");var anchor=starts[(slot+rotation)%4];float lift=.12f;
            foreach(var wheel in Items(truck,"wheels"))lift=Mathf.Max(lift,.12f-(truck.transform.InverseTransformPoint(((Component)wheel).transform.position).y-(float)Read(wheel,"Radius")-(float)Read(wheel,"SpringMaxLength")));
            var hits=Physics.RaycastAll(anchor.position+Vector3.up*6,Vector3.down,30,~0,QueryTriggerInteraction.Ignore);
            var support=hits.Where(hit=>hit.normal.y>=.65f&&hit.collider.GetComponentInParent(Runtime("LWS.TruckSports.SportsEventTruck"))==null&&Mathf.Abs(hit.point.y-anchor.position.y)<4).OrderBy(hit=>Mathf.Abs(hit.point.y-anchor.position.y)).ToArray();
            Vector3 p=support.Length>0?support[0].point+support[0].normal*lift:anchor.position;Quaternion rotationAt=anchor.rotation;
            if(support.Length>0)rotationAt=Quaternion.LookRotation(Vector3.ProjectOnPlane(anchor.forward,support[0].normal).normalized,support[0].normal);
            var center=p+rotationAt*Vector3.Scale(shape.center,truck.transform.lossyScale);var half=Vector3.Scale(shape.size*.5f,truck.transform.lossyScale)+new Vector3(.2f,.05f,.3f);
            results.Add(new Candidate{slot=slot,root=RootPath(truck.transform),anchor=RootPath(anchor),anchorPosition=anchor.position,candidatePosition=p,candidateRotation=rotationAt,boxCenter=center,boxHalf=half,supportHits=hits.OrderBy(hit=>hit.distance).Take(16).Select(hit=>ColliderDescription(hit.collider)+" hit="+hit.point.ToString("F4")+" normal="+hit.normal.ToString("F4")).ToArray(),overlaps=Physics.OverlapBox(center,half,rotationAt,~0,QueryTriggerInteraction.Ignore).Take(32).Select(ColliderDescription).ToArray()});
        }
        return results.ToArray();
    }
    int[] CargoIds()
    {
        var treasure=match.GetType().GetField("treasure",Any);
        if(treasure!=null)return ((Array)treasure.GetValue(match)).Cast<object>().Select(value=>Body(value).GetInstanceID()).ToArray();
        var carriers=match.GetType().GetField("carriers",Any);
        if(carriers!=null)return ((Array)carriers.GetValue(match)).Cast<object>().Select(value=>Body(Read(value,"cargo")).GetInstanceID()).ToArray();
        return Array.Empty<int>();
    }
    void Capture(int seed,string phase)
    {
        try
        {
            var method=Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(value=>value.Name=="Save"&&value.GetParameters().Length==5);
            method.Invoke(null,new object[]{match,1280,720,Path.Combine(output,"seed-"+seed+"-"+phase+".png"),null});
        }
        catch(Exception exception){TestContext.WriteLine("CONTROLLED_CAPTURE_FAILED "+exception);}
    }
    IEnumerator ObserveAttempt(Attempt attempt)
    {
        float since=Time.realtimeSinceStartup;
        attempt.maximumVerticalSpeed=new float[4];attempt.maximumAngularSpeed=new float[4];attempt.maximumHeightExcursion=new float[4];attempt.minimumUp=Enumerable.Repeat(1f,4).ToArray();
        bool started=false;
        try{started=(bool)Call(match,"StartMatch");}
        catch(Exception exception){attempt.exception=(exception is TargetInvocationException target?target.InnerException:exception).ToString();}
        attempt.started=started;attempt.candidates=PlacementWitnesses(attempt.seed);attempt.region=StartRegion();
        if(!started)
        {
            attempt.failures.Add("Ordinary StartMatch failed: "+(attempt.exception??"returnedfalse"));Snapshot(attempt,"START_FAILED",since);Capture(attempt.seed,"start-failed");yield break;
        }
        attempt.matchId=(string)Read(match,"MatchId");var initial=Snapshot(attempt,"START_RETURNED",since);Capture(attempt.seed,"start");
        var buckets=new HashSet<int>{Mathf.CeilToInt(initial.intro)};int previousBucket=Mathf.CeilToInt(initial.intro);float deadline=since+25,nextProgress=since+5;bool lastSupported=false;
        while(Time.realtimeSinceStartup<deadline&&!(bool)Read(match,"Finished"))
        {
            yield return new WaitForFixedUpdate();attempt.samples++;
            float intro=(float)Read(match,"Intro"),clock=(float)Read(match,"Clock");int bucket=Mathf.CeilToInt(intro);buckets.Add(bucket);
            var sample=trucks.Select((truck,index)=>ObserveCrew(truck,index,false)).ToArray();lastSupported=true;
            for(int slot=0;slot<4;slot++)
            {
                var crew=sample[slot];bool supported=crew.wheels.Length>=4&&crew.wheels.All(wheel=>wheel.enabled&&wheel.grounded&&wheel.support!="none"&&!wheel.supportIsTruck&&wheel.normal.y>.7f);lastSupported&=supported;
                if(Time.realtimeSinceStartup-since>.35f&&intro>0)
                {
                    attempt.maximumVerticalSpeed[slot]=Mathf.Max(attempt.maximumVerticalSpeed[slot],Mathf.Abs(crew.velocity.y));attempt.maximumAngularSpeed[slot]=Mathf.Max(attempt.maximumAngularSpeed[slot],crew.angularVelocity.magnitude);attempt.maximumHeightExcursion[slot]=Mathf.Max(attempt.maximumHeightExcursion[slot],Mathf.Abs(crew.position.y-initial.crews[slot].position.y));attempt.minimumUp[slot]=Mathf.Min(attempt.minimumUp[slot],crew.up.y);
                    if(crew.handbrake<.99f&&!attempt.failures.Contains("Countdown handbrake missing slot "+slot))attempt.failures.Add("Countdown handbrake missing slot "+slot);
                }
                if(!crew.active||!crew.powered||!crew.vehicleEnabled){string reason="Original active NWH assembly disabled slot "+slot;if(!attempt.failures.Contains(reason))attempt.failures.Add(reason);}
            }
            if(lastSupported)attempt.fullySupportedSamples++;
            if(bucket!=previousBucket){Snapshot(attempt,"COUNTDOWN_"+bucket,since);previousBucket=bucket;}
            if(Time.realtimeSinceStartup>=nextProgress){Snapshot(attempt,"PROGRESS",since);nextProgress=Time.realtimeSinceStartup+5;}
            if(intro<=0&&!attempt.reachedGo)
            {
                attempt.reachedGo=true;Snapshot(attempt,"GO_BOUNDARY",since);Capture(attempt.seed,"go");
                if(!lastSupported)attempt.failures.Add("GO without every original NWH wheel on actual non-truck upright support");
            }
            if(attempt.reachedGo&&clock>=1)break;
        }
        attempt.buckets=buckets.OrderByDescending(value=>value).ToArray();
        if(!attempt.reachedGo)attempt.failures.Add("Original countdown did not reach GO in25real seconds");
        if(!new[]{3,2,1,0}.All(value=>buckets.Contains(value)))attempt.failures.Add("Original3-2-1GO buckets missing: "+string.Join(",",attempt.buckets));
        if((float)Read(match,"Clock")<1)attempt.failures.Add("Original clock did not advance afterGO");
        if(attempt.fullySupportedSamples<20)attempt.failures.Add("Fewer than20native fixed samples with every configured wheel supported");
        for(int slot=0;slot<4;slot++)
        {
            if(attempt.maximumVerticalSpeed[slot]>3)attempt.failures.Add("Countdown vertical speed >3m/s slot "+slot);
            if(attempt.maximumAngularSpeed[slot]>1.5f)attempt.failures.Add("Countdown angular speed >1.5rad/s slot "+slot);
            if(attempt.maximumHeightExcursion[slot]>1)attempt.failures.Add("Countdown root jumped/dropped >1m slot "+slot);
            if(attempt.minimumUp[slot]<.88f)attempt.failures.Add("Countdown up normal <.88 slot "+slot);
        }
        Snapshot(attempt,"TERMINAL_OBSERVATION",since);if(attempt.failures.Count>0){attempt.candidates=PlacementWitnesses(attempt.seed);Capture(attempt.seed,"failed-observation");}
    }
    IEnumerator ExerciseScene(string name)
    {
        sceneName=name;attempts.Clear();var args=Environment.GetCommandLineArgs();int option=Array.IndexOf(args,"-tsDragonOutput");
        output=Path.GetFullPath(Path.Combine(option>=0&&option+1<args.Length?args[option+1]:Path.Combine(Application.persistentDataPath,"DragonNative"),"GroundStarts",sceneName,DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")));Directory.CreateDirectory(output);
        string path="Assets/"+name+"/Scenes/"+name+".unity";
        try
        {
            Assert.That(File.Exists(path),Is.True,"Explicitly author the accepted actual ground scene before this fixture. It never creates a venue.");
            preferences=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"DragonGroundStarts"});Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
            isolation=new GameObject("Ground starts original-input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));
            EditorSceneManager.LoadSceneInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("LWS.TruckDragonKingdom.DragonKingdomGroundMatch"));Assert.That(match,Is.Not.Null);trucks=Items(match,"trucks");Assert.That(trucks.Length,Is.EqualTo(4));
            int[] truckIds=trucks.Select(value=>Body(value).GetInstanceID()).ToArray(),cargoIds=CargoIds();
            for(int seed=1;seed<=4;seed++)
            {
                Call(match,"SetupScreen");Read(match,"setup").GetType().GetField("count").SetValue(Read(match,"setup"),4);match.GetType().GetField("fixedSeed").SetValue(match,seed);Call(match,"AllBots");
                Assert.That((bool)Read(match,"AllAI"),Is.True);Time.timeScale=1;
                var attempt=new Attempt{seed=seed,rotation=seed%4,truckIds=trucks.Select(value=>Body(value).GetInstanceID()).ToArray(),cargoIds=CargoIds()};attempts.Add(attempt);
                if(!truckIds.SequenceEqual(attempt.truckIds))attempt.failures.Add("Original truck body identities changed acrossrematch");if(!cargoIds.SequenceEqual(attempt.cargoIds))attempt.failures.Add("Original cargo body identities changed acrossrematch");
                yield return ObserveAttempt(attempt);attempt.passed=attempt.failures.Count==0;Save("PERMUTATION_OBSERVED");Call(match,"SetupScreen");yield return null;
            }
            Save(attempts.All(attempt=>attempt.passed)?"PASSED":"FAILED; exact original observations retained");
            Assert.That(attempts.All(attempt=>attempt.passed),Is.True,string.Join("\n",attempts.Where(attempt=>!attempt.passed).Select(attempt=>"seed"+attempt.seed+": "+string.Join("; ",attempt.failures))));
        }
        finally
        {
            Save(attempts.Count==4&&attempts.All(attempt=>attempt.passed)?"PASSED":"INCOMPLETE OR FAILED; inspect NUnit result and evidence");Time.timeScale=1;
            if(match!=null)Call(match,"SetupScreen");if(isolation!=null)UnityEngine.Object.DestroyImmediate(isolation);preferences?.Dispose();preferences=null;match=null;isolation=null;Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
        }
    }
    [UnityTest,Timeout(240000)] public IEnumerator HeistActualNwhGroundStartsAndRematchesAcrossAllFourGridRotations(){yield return ExerciseScene("TruckHoardHeist");}
    [UnityTest,Timeout(240000)] public IEnumerator EggActualNwhGroundStartsAndRematchesAcrossAllFourGridRotations(){yield return ExerciseScene("TruckDragonEggDelivery");}
    [UnityTest,Timeout(240000)] public IEnumerator EscapeActualNwhGroundStartsAndRematchesAcrossAllFourGridRotations(){yield return ExerciseScene("TruckDragonEscape");}
}
#endif
