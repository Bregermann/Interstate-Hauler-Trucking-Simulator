#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using LWS.TruckHoardHeist;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Saved scene only. The normal setup/start/pause/resume/rematch API is the
// entire control surface. No pose, velocity, score, cargo, clock, dragon state,
// route, animation or gameplay callback is injected by this observer.
public sealed partial class HoardHeistFullAINativeTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    const string ScenePath = "Assets/TruckHoardHeist/Scenes/TruckHoardHeist.unity";
    Component match, guardian, motor;
    object[] treasure, trucks, hoists;
    Rigidbody[] cargoBodies;
    int[] originalIds;
    IDisposable preferences;
    GameObject inputIsolation;
    readonly List<KeyValuePair<EventInfo, Delegate>> subscriptions = new List<KeyValuePair<EventInfo, Delegate>>();
    readonly List<string> violations = new List<string>();
    readonly List<string> actions = new List<string>();
    readonly List<Delivery> deliveries = new List<Delivery>();
    readonly HashSet<string> requestedCaptures = new HashSet<string>();
    readonly StringBuilder progress = new StringBuilder();
    string output;
    string captureReason;
    int run, ordinaryOption;
    bool completed, capturePending;

    [Serializable] public sealed class Delivery
    {
        public int run, slot, originalBodyId, value;
        public string matchId, eventId, participantId;
        public float clock, truckSpeed, cargoSpeed, cargoSpin;
        public Vector3 cargoPosition, truckPosition;
        public bool originalIdentity, fullyInside, dynamicBody, securedToOriginalTruck, originalOwner, settled;
    }
    [Serializable] public sealed class Evidence
    {
        public string scope, status, scene;
        public float timeScale, fixedDeltaTime;
        public int ordinaryOption;
        public int[] originalCargoIds;
        public string[] actions, violations;
        public Delivery[] deliveries;public LiveCavernSummary liveCavern;
    }
    static Type Runtime(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).First(t => t != null);
    static object Read(object instance, string name) => instance.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(instance) : instance.GetType().GetProperty(name, Any).GetValue(instance);
    static object Call(object instance, string name, params object[] args) => instance.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(instance, args);
    static object[] ArrayOf(object instance, string name) => ((Array)Read(instance, name)).Cast<object>().ToArray();
    HoardHeistRules Rules => (HoardHeistRules)Read(match, "Rules");
    Rigidbody Body(object value) => (Rigidbody)Read(value, "body");
    void Log(string value)
    {
        progress.AppendLine(value); TestContext.WriteLine(value);
        File.WriteAllText(Path.Combine(output, "progress.txt"), progress.ToString());
    }
    void Persist()
    {
        if (output == null) return;
        liveStream?.Flush();
        File.WriteAllText(Path.Combine(output, "evidence.json"), JsonUtility.ToJson(new Evidence {
            scene = ScenePath, status = completed ? "COMPLETED" : "INCOMPLETE OR FAILED; inspect NUnit result and progress",
            scope = "Two original saved-scene full four-AI matches, genuine score-boundary physical witnesses, pause/rematch. 4x simulation; controlled Camera.Render snapshots. Not normal-speed performance, Windows capture, human/device/listening acceptance.",
            timeScale = 4, fixedDeltaTime = Time.fixedDeltaTime, ordinaryOption=ordinaryOption, originalCargoIds = originalIds,
            actions = actions.ToArray(), violations = violations.ToArray(), deliveries = deliveries.ToArray(),liveCavern=LiveCavernProgressSummary()
        }, true));
    }
    void Subscribe(object source, string name, Action<object> observe)
    {
        var signal = source.GetType().GetEvent(name);
        var parameter = Expression.Parameter(signal.EventHandlerType.GetMethod("Invoke").GetParameters().Single().ParameterType, "observed");
        var handler = Expression.Lambda(signal.EventHandlerType, Expression.Invoke(Expression.Constant(observe), Expression.Convert(parameter, typeof(object))), parameter).Compile();
        signal.AddEventHandler(source, handler); subscriptions.Add(new KeyValuePair<EventInfo, Delegate>(signal, handler));
    }
    void ObserveAction(string phase, object action)
    {
        string value = "run=" + run + " clock=" + Read(match,"Clock") + " phase=" + phase + " action=" + action + " actualMode=" + Read(Read(motor,"animal"),"ActiveMode") + " " + Animation();
        actions.Add(value); Log(value);
        if (phase == "START")
        {
            object mode=Read(Read(motor,"animal"),"ActiveMode");
            if(mode==null||!(bool)Read(mode,"PlayingMode")||!(bool)Read(motor,"ActionPlaying"))violations.Add("Mapped action lacks actual playing vendor mode: "+action);
            if (action.ToString() == "Sleep" || action.ToString() == "Wake" || action.ToString() == "Roar") QueueCapture("guardian-"+action);
        }
        Persist();
    }
    void QueueCapture(string reason)
    {
        // Each original delivery and semantic action gets at most one image
        // per match. Repeated sleep/vigilance must not generate an image leak.
        if(!requestedCaptures.Add(run+":"+reason))return;
        captureReason=reason;capturePending=true;
    }
    void ObserveScore(object receipt)
    {
        // Record synchronously at ConfirmScore, while the original cargo joint
        // and all real reception witnesses still exist. Observation never
        // throws back into the game's award path.
        try
        {
            string eventId = (string)Read(receipt,"EventId");
            int index = Array.FindIndex(treasure, item => eventId == "heist.delivery." + (string)Read(item,"stableId"));
            if (index < 0) { violations.Add("Unknown original delivery event " + eventId); return; }
            int slot = (int)Read(receipt,"Slot");
            if (slot < 0 || slot >= trucks.Length) { violations.Add("Invalid score slot " + slot); return; }
            var item = treasure[index]; var body = Body(item); var truck = Body(trucks[slot]);
            var joint = (FixedJoint)Read(item,"CargoJoint");
            bool inside = (bool)Runtime("LWS.TruckHoardHeist.HoardHeistMatch").GetMethod("FullyInside", BindingFlags.Public | BindingFlags.Static).Invoke(null, new[] { Read(item,"shape"), Read(match,"extractionZone") });
            var record = new Delivery { run = run, slot = slot, originalBodyId = body.GetInstanceID(), value = (int)Read(item,"value"),
                matchId = (string)Read(receipt,"MatchId"), eventId = eventId, participantId = (string)Read(receipt,"ParticipantId"),
                clock = (float)Read(match,"Clock"), cargoPosition = body.position, truckPosition = truck.position,
                truckSpeed = truck.linearVelocity.magnitude, cargoSpeed = body.linearVelocity.magnitude, cargoSpin = body.angularVelocity.magnitude,
                originalIdentity = body.GetInstanceID() == originalIds[index], fullyInside = inside, dynamicBody = !body.isKinematic,
                securedToOriginalTruck = joint != null && joint.connectedBody == truck, originalOwner = (int)Read(item,"Carrier") == slot,
                settled = truck.linearVelocity.magnitude < .7f && body.linearVelocity.magnitude < .8f && body.angularVelocity.magnitude < .4f };
            double prior = deliveries.Where(entry => entry.run == run && entry.slot == slot).Sum(entry => entry.value);
            var player = ArrayOf(Read(match,"setup"),"players")[slot];
            if (!record.originalIdentity || !record.fullyInside || !record.dynamicBody || !record.securedToOriginalTruck || !record.originalOwner || !record.settled)
                violations.Add("Incomplete synchronous physical award witness " + eventId);
            if (deliveries.Any(entry => entry.run == run && entry.eventId == eventId)) violations.Add("Duplicate award " + eventId);
            if ((double)Read(receipt,"Delta") != record.value || (double)Read(receipt,"Total") != prior + record.value || (string)Read(receipt,"Unit") != "VALUE" || Read(receipt,"Kind").ToString() != "Points") violations.Add("Receipt value/unit mismatch " + eventId);
            if (record.matchId != (string)Read(match,"MatchId") || record.participantId != (string)Read(player,"participantId") || !(bool)Read(receipt,"IsAI")) violations.Add("Receipt identity/source mismatch " + eventId);
            if (Rules.States[index] != HoardCargoState.Delivered) violations.Add("Original item not delivered at receipt " + eventId);
            deliveries.Add(record); QueueCapture("delivery-"+index); Log("PHYSICAL_AWARD " + JsonUtility.ToJson(record)); Persist();
        }
        catch (Exception exception) { violations.Add("Score observer failed: " + exception); Persist(); }
    }
    string Animation()
    {
        var animator = (Animator)Read(motor,"animator"); var state = animator.GetCurrentAnimatorStateInfo(0);
        return "action=" + Read(motor,"CurrentAction") + " playing=" + Read(motor,"ActionPlaying") + " state=" + state.fullPathHash + " phase=" + state.normalizedTime.ToString("F3") + " clips=" + string.Join(",",animator.GetCurrentAnimatorClipInfo(0).Select(clip => clip.clip.name));
    }
    void Snapshot(string phase)
    {
        Log("SNAPSHOT run=" + run + " " + phase + " match=" + Read(match,"MatchId") + " clock=" + Read(match,"Clock") + " intro=" + Read(match,"Intro") + " finished=" + Read(match,"Finished") + " score=" + string.Join(",",Rules.Scores) + " loads=" + Read(match,"PhysicalLoads") + " delivered=" + Rules.DeliveredCount + " noise=" + Read(match,"ActualNoiseEvents") + " alert=" + Read(match,"AlertStage") + "/" + Read(Read(match,"Alert"),"Value") + " intent=" + Read(guardian,"Intent") + " target=" + Read(guardian,"TargetSlot") + " guardian=" + Body(motor).position.ToString("F2") + " " + Animation());
        for (int slot = 0; slot < 4; slot++)
        {
            var route = Call(match,"NavigationRoute",slot); var body = Body(trucks[slot]);
            Log("CREW " + slot + " body=" + body.position.ToString("F2") + " speed=" + body.linearVelocity.magnitude.ToString("F2") + " goal=" + Call(match,"NavigationGoal",slot) + " path=" + Read(route,"Cursor") + "/" + ((ICollection)Read(route,"points")).Count + " stall=" + Read(route,"SecondsWithoutProgress") + " obstacle=" + Read(route,"BlockingObstacle") + " recovery=" + ((int[])Read(match,"Recoveries"))[slot] + " hoist=" + Read(hoists[slot],"Stage") + " load=" + Read(hoists[slot],"Load") + " pickups=" + Read(hoists[slot],"Pickups") + " releases=" + Read(hoists[slot],"Releases") + " cargo=" + Rules.LoadCount(slot) + " status=" + Call(match,"NavigationCaption",slot));
            Log("RIG_TRAFFIC crew="+slot+" phase="+((int[])Read(match,"trafficYieldPhase"))[slot]+" starts="+((int[])Read(match,"trafficYieldStarts"))[slot]+" blocker="+((int[])Read(match,"trafficBlocker"))[slot]+" yieldTo="+((int[])Read(match,"trafficYieldTo"))[slot]+" held="+((bool[])Read(match,"trafficHolding"))[slot]+" retryAt="+((float[])Read(match,"trafficRetryAt"))[slot]+" until="+((float[])Read(match,"trafficYieldUntil"))[slot]+" noCourseOther="+((int[])Read(match,"trafficNoCourseOther"))[slot]+" noCourseAt="+((float[])Read(match,"trafficNoCourseAt"))[slot]+" rejection="+((string[])Read(match,"trafficYieldRejection"))[slot]+" origin="+((Vector3[])Read(match,"trafficYieldOrigin"))[slot]+" first="+((Vector3[])Read(match,"trafficYieldRear"))[slot]+" goal="+((Vector3[])Read(match,"trafficYieldGoal"))[slot]);
            Log("ACTUAL_ROAD_ORIGIN crew="+slot+" supported="+((bool[])Read(match,"roadOriginValid"))[slot]+" held="+((bool[])Read(match,"roadOriginHolding"))[slot]+" sampledPosition="+((Vector3[])Read(match,"roadOriginPosition"))[slot]+" road="+((Vector3[])Read(match,"roadOriginRoadPoint"))[slot]+" reason="+((string[])Read(match,"roadOriginReason"))[slot]);
            var points=((IEnumerable)Read(route,"points")).Cast<Vector3>().ToArray();int cursor=(int)Read(route,"Cursor");
            Log("ACTUAL_ROUTE_LEG crew="+slot+" cursor="+cursor+" points="+string.Join(";",points.Skip(Mathf.Max(0,cursor-1)).Take(3).Select(point=>point.ToString("F2")))+" accessChecks="+Read(match,"treasureAccessChecks"));
        }
        for(int index=0;index<treasure.Length;index++) Log("CARGO " + Read(treasure[index],"stableId") + " original=" + cargoBodies[index].GetInstanceID() + " state=" + Rules.States[index] + " owner=" + Rules.Owners[index] + " body=" + cargoBodies[index].position.ToString("F2") + " speed=" + cargoBodies[index].linearVelocity.magnitude.ToString("F2")+" actualCarrier="+Read(treasure[index],"Carrier")+" referencedJoint="+Read(treasure[index],"CargoJoint")+" physicalJoints="+string.Join(";",cargoBodies[index].GetComponents<Joint>().Select(joint=>joint.GetType().Name+" to="+(joint.connectedBody!=null?joint.connectedBody.name:"world")+" break="+joint.breakForce+" force="+joint.currentForce.magnitude.ToString("F2"))));
        for(int index=0;index<treasure.Length;index++)if((float)Read(treasure[index],"LastJointBreakAt")>=0)
        {
            Log("ACTUAL_JOINT_BREAK cargo="+Read(treasure[index],"stableId")+" at="+Read(treasure[index],"LastJointBreakAt")+" reportedForce="+Read(treasure[index],"LastJointBreakForce")+" carrier="+Read(treasure[index],"LastBrokenCarrier")+" lastContact="+Read(treasure[index],"LastImpactCollider")+" contactAt="+Read(treasure[index],"LastImpactAt")+" impactSpeed="+Read(treasure[index],"LastImpactSpeed"));
            var atomic=Read(treasure[index],"LastJointBreakSnapshot");if(atomic!=null)Log("ACTUAL_ATOMIC_CARGO_JOINT_BREAK cargo="+Read(treasure[index],"stableId")+" "+JsonUtility.ToJson(atomic));
        }
        Persist();
    }
    void Capture(string phase,int width=1280)
    {
        var save = Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 5);
        string path = Path.Combine(output,"run-" + run + "-" + phase + (width==1280?"":"-"+width) + ".png");
        save.Invoke(null,new object[] { match,width,width==1280?720:1080,path,null });
        Assert.That(File.Exists(path)&&new FileInfo(path).Length>1000,Is.True,"Retain the actual rendered camera/UI evidence.");
        Log("CONTROLLED_RENDER " + path + "; inspect retained image, no visual acceptance inferred.");
    }
    Component ActualResultsBoard()
    {
        // The authored menu Canvas is a separate scene root, not a child of
        // the match. Resolve its actual bound Result page, as the production
        // menu and the preservation authoring do, rather than search the truck
        // session's unrelated hierarchy.
        var menus=Read(match,"menus");
        int index=Convert.ToInt32(Enum.Parse(Runtime("LWS.TruckSports.SportsEventScreen"),"Result"));
        var page=(GameObject)((Array)Read(menus,"pages")).GetValue(index);
        var boards=page.GetComponentsInChildren(Runtime("LWS.TruckSports.SportsResultsPresentation"),true);
        Assert.That(boards.Length,Is.EqualTo(1),"The original bound results page must contain its saved presentation.");
        Assert.That(Read(boards[0],"menus"),Is.SameAs(menus));
        return boards[0];
    }
    IEnumerator ObserveGenuineResultsBoard()
    {
        // Only the completed physical match above can reach this observer.
        // Never supplies a score, Finish flag or tween completion callback.
        var board=ActualResultsBoard();var source=Read(board,"source");
        Assert.That(source.GetType().FullName,Is.EqualTo("LWS.TruckDragonKingdom.DragonFinalResultsSource"));
        Assert.That(Read(source,"session"),Is.SameAs(match));Assert.That(Read(source,"ResultReady"),Is.EqualTo(true));
        var scores=Rules.Scores.ToArray();var states=Rules.States.ToArray();int journal=(int)Read(Read(match,"ScoreJournal"),"Count");
        float deadline=Time.realtimeSinceStartup+3;
        while(!(bool)Read(board,"Displaying")&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(Read(board,"Displaying"),Is.EqualTo(true));yield return new WaitForSecondsRealtime(1.2f);
        var snapshot=Read(board,"Snapshot");var rows=((IEnumerable)Read(snapshot,"Rows")).Cast<object>().ToArray();
        Assert.That(rows.Length,Is.EqualTo(4));Assert.That(rows.Select(row=>(string)Read(row,"ParticipantId")).Distinct().Count(),Is.EqualTo(4));
        var players=ArrayOf(Read(match,"setup"),"players");var views=(Array)Read(board,"rows");
        for(int index=0;index<rows.Length;index++)
        {
            var row=rows[index];int slot=(int)Read(row,"Slot");Assert.That(slot,Is.InRange(0,3));
            Assert.That(Read(row,"ParticipantId"),Is.EqualTo(Read(players[slot],"participantId")));
            Assert.That(Read(row,"Measurement"),Is.EqualTo((double)scores[slot]));Assert.That(Read(row,"Unit"),Is.EqualTo("VALUE"));
            Assert.That(Read(row,"Winner"),Is.EqualTo(scores[slot]==scores.Max()));
            Assert.That(Read(Read(views.GetValue(index),"value"),"text"),Is.EqualTo(Read(row,"Value")));
        }
        var visibility=(CanvasGroup)Read(board,"visibility");Assert.That(visibility.alpha,Is.GreaterThan(.99f));Assert.That(visibility.interactable||visibility.blocksRaycasts,Is.False);
        Assert.That(Read(Read(source,"OriginalResult"),"enabled"),Is.EqualTo(false));
        Capture("actual-final-results-board");Capture("actual-final-results-board",1920);
        CollectionAssert.AreEqual(scores,Rules.Scores);CollectionAssert.AreEqual(states,Rules.States);
        Assert.That(Read(Read(match,"ScoreJournal"),"Count"),Is.EqualTo(journal));
        Log("GENUINE_HEIST_RESULTS_BOARD run="+run+" actualFinalValues="+string.Join(",",scores)+" native720p1080p=true originalRulesAndJournalUnchanged=true");
    }
    IEnumerator PauseWithOriginalCargo()
    {
        Snapshot("before-pause"); Call(match,"Pause",0,false); yield return null; yield return null;
        Assert.That((bool)Read(match,"Paused"),Is.True); Assert.That(Time.timeScale,Is.Zero);
        var bodies = cargoBodies.Concat(trucks.Select(Body)).Concat(new[]{Body(motor)}).ToArray();
        var positions = bodies.Select(body=>body.position).ToArray(); var rotations = bodies.Select(body=>body.rotation).ToArray();
        float clock = (float)Read(match,"Clock"); int receipts = deliveries.Count; var states = (HoardCargoState[])Rules.States.Clone();
        var animator=(Animator)Read(motor,"animator"); float animation=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        Capture("paused-original-cargo"); yield return new WaitForSecondsRealtime(.4f);
        Assert.That((float)Read(match,"Clock"),Is.EqualTo(clock)); Assert.That(deliveries.Count,Is.EqualTo(receipts)); CollectionAssert.AreEqual(states,Rules.States);
        for(int i=0;i<bodies.Length;i++){Assert.That(Vector3.Distance(bodies[i].position,positions[i]),Is.LessThan(.001f));Assert.That(Quaternion.Angle(bodies[i].rotation,rotations[i]),Is.LessThan(.01f));}
        Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime,Is.EqualTo(animation).Within(.0001f));
        Call(match,"Resume"); Assert.That((bool)Read(match,"Paused"),Is.False); Time.timeScale=4; Log("PAUSE_PASSED run="+run+" original dynamic bodies, rules, clock, score receipts and actual guardian animation remained still.");
    }

    [UnityTest, Timeout(1800000)]
    public IEnumerator SavedHeistFourAICrewsPhysicallyDeliverWakeGuardianFinishAndRematchOriginalCargo()
    { yield return OriginalPhysicalMatches(0); }

    [UnityTest, Timeout(1800000)]
    public IEnumerator SavedDefaultFiveMinuteHeistPhysicallyDeliversAndRematchesWithActualResultsBoard()
    { yield return OriginalPhysicalMatches(1); }

    IEnumerator OriginalPhysicalMatches(int durationOption)
    {
        ordinaryOption=durationOption;
        string[] args=Environment.GetCommandLineArgs();int optionIndex=Array.IndexOf(args,"-tsDragonOutput");
        output=Path.GetFullPath(Path.Combine(optionIndex>=0&&optionIndex+1<args.Length?args[optionIndex+1]:Path.Combine(Application.persistentDataPath,"DragonNative"),"HeistFullAI",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+"-"+Guid.NewGuid().ToString("N").Substring(0,8)));Directory.CreateDirectory(output);
        try
        {
            Assert.That(File.Exists(ScenePath),Is.True,"Explicitly author and save the real Heist scene before this native fixture. This test never authors a venue.");
            preferences=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"HeistFullAINative"});Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
            inputIsolation=new GameObject("Heist full-AI physical input isolation");UnityEngine.Object.DontDestroyOnLoad(inputIsolation);inputIsolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));
            EditorSceneManager.LoadSceneInPlayMode(ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("LWS.TruckHoardHeist.HoardHeistMatch"));Assert.That(match,Is.Not.Null);
            // These are ordinary setup choices, before either original start.
            Read(match,"setup").GetType().GetField("count").SetValue(Read(match,"setup"),4);match.GetType().GetField("option").SetValue(match,ordinaryOption);
            Call(match,"AllBots");Runtime("LWS.TruckSports.SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{true});
            Assert.That((bool)Read(match,"AllAI"),Is.True);
            treasure=ArrayOf(match,"treasure");trucks=ArrayOf(match,"trucks");hoists=ArrayOf(match,"hoists");guardian=(Component)ArrayOf(match,"dragons")[0];motor=(Component)Read(guardian,"motor");
            ConfigureLiveCavernObservation();
            cargoBodies=treasure.Select(Body).ToArray();originalIds=cargoBodies.Select(body=>body.GetInstanceID()).ToArray();Assert.That(originalIds.Distinct().Count(),Is.EqualTo(treasure.Length));
            Subscribe(match,"ScoreConfirmed",ObserveScore);Subscribe(motor,"ActionStarted",action=>ObserveAction("START",action));Subscribe(motor,"ActionContact",action=>ObserveAction("CONTACT",action));Subscribe(motor,"ActionFinished",action=>ObserveAction("FINISH completed="+Read(motor,"LastActionCompleted"),action));
            string previousMatch=null;
            for(run=1;run<=2;run++)
            {
                int[] pickupsBefore=hoists.Select(hoist=>(int)Read(hoist,"Pickups")).ToArray();int[] releasesBefore=hoists.Select(hoist=>(int)Read(hoist,"Releases")).ToArray();int deliveryStart=deliveries.Count;
                Assert.That((bool)Call(match,"StartMatch"),Is.True);string matchId=(string)Read(match,"MatchId");Assert.That(matchId,Is.Not.EqualTo(previousMatch));previousMatch=matchId;
                CollectionAssert.AreEqual(originalIds,treasure.Select(item=>Body(item).GetInstanceID()).ToArray());Assert.That(Rules.Scores,Is.All.Zero);Assert.That(Rules.States,Is.All.EqualTo(HoardCargoState.Loose));Assert.That(Rules.Owners,Is.All.EqualTo(-1));Assert.That((int)Read(Read(match,"ScoreJournal"),"Count"),Is.Zero);
                for(int index=0;index<treasure.Length;index++)
                { Assert.That(Vector3.Distance(cargoBodies[index].position,(Vector3)Read(treasure[index],"seedPosition")),Is.LessThan(.001f));Assert.That((FixedJoint)Read(treasure[index],"CargoJoint"),Is.Null);Assert.That((int)Read(treasure[index],"Carrier"),Is.EqualTo(-1));Assert.That(cargoBodies[index].gameObject.activeSelf,Is.True);Assert.That(cargoBodies[index].isKinematic,Is.False); }
                Assert.That((int)Read(match,"PhysicalLoads"),Is.Zero);Assert.That((int)Read(match,"PhysicalDeliveries"),Is.Zero);Assert.That((bool)Read(Read(match,"Alert"),"Awakened"),Is.False);Assert.That(Read(guardian,"Intent").ToString(),Is.EqualTo("SleepingGuardian"));
                Snapshot("fresh-original-start");Time.timeScale=4;bool paused=false,loadCaptured=false,pursuitObserved=false;float deadline=Time.realtimeSinceStartup+720,nextSnapshot=Time.realtimeSinceStartup+10;
                while(!(bool)Read(match,"Finished")&&Time.realtimeSinceStartup<deadline)
                {
                    yield return null;ObserveLiveCavernFrame();
                    if(Time.realtimeSinceStartup>=nextSnapshot){Snapshot("progress");nextSnapshot=Time.realtimeSinceStartup+10;}
                    if((float)Read(match,"Intro")<=0&&capturePending){capturePending=false;Capture(captureReason);}
                    if(Read(guardian,"Intent").ToString()=="Pursuit"&&(int)Read(guardian,"TargetSlot")>=0)pursuitObserved=true;
                    if(!loadCaptured&&(int)Read(match,"PhysicalLoads")>0){loadCaptured=true;Snapshot("first-actual-rack-load");Capture("first-actual-load");}
                    if(!paused&&!(bool)Read(match,"Finished")&&Rules.States.Any(state=>state==HoardCargoState.Carried)){yield return PauseWithOriginalCargo();paused=true;}
                }
                Snapshot("terminal-or-timeout");Capture("terminal-or-timeout");
                Assert.That((bool)Read(match,"Finished"),Is.True,"Original full match did not reach its own result page; retain progress diagnostics instead of forcing a result.");
                Assert.That(Read(Read(match,"menus"),"Screen").ToString(),Is.EqualTo("Result"));Assert.That((string)Read(match,"Results"),Does.Contain("original treasures received"));
                Assert.That(paused,Is.True,"A genuine carried cargo must be observed before pause validation.");Assert.That((int)Read(match,"PhysicalLoads"),Is.GreaterThan(0));Assert.That(Rules.DeliveredCount,Is.GreaterThan(0),"Actual NWH AI driving and original crane deliveries must earn real points.");Assert.That(Rules.Scores.Sum(),Is.GreaterThan(0));
                Assert.That((int)Read(match,"PhysicalDeliveries"),Is.EqualTo(Rules.DeliveredCount));Assert.That(deliveries.Count-deliveryStart,Is.EqualTo(Rules.DeliveredCount));Assert.That((int)Read(Read(match,"ScoreJournal"),"Count"),Is.EqualTo(Rules.DeliveredCount));
                Assert.That(hoists.Select((hoist,index)=>(int)Read(hoist,"Pickups")-pickupsBefore[index]).Sum(),Is.GreaterThanOrEqualTo((int)Read(match,"PhysicalLoads")));
                Assert.That(hoists.Select((hoist,index)=>(int)Read(hoist,"Releases")-releasesBefore[index]).Sum(),Is.GreaterThanOrEqualTo((int)Read(match,"PhysicalLoads")));
                for(int slot=0;slot<4;slot++)Assert.That(deliveries.Where(entry=>entry.run==run&&entry.slot==slot).Sum(entry=>entry.value),Is.EqualTo(Rules.Scores[slot]));
                Assert.That(actions.Any(action=>action.StartsWith("run="+run+" ")&&action.Contains("phase=START action=Sleep ")),Is.True,"Actual mapped Sleep mode must play.");
                Assert.That(actions.Any(action=>action.StartsWith("run="+run+" ")&&action.Contains("phase=START action=Wake ")),Is.True,"Real AI noise/sensing must trigger the actual Wake mode; this fixture never requests it.");
                Assert.That(actions.Any(action=>action.StartsWith("run="+run+" ")&&action.Contains("phase=FINISH completed=True action=Wake ")),Is.True,"Actual non-interrupted Wake animation must complete.");Assert.That(pursuitObserved,Is.True,"Guardian must pursue a genuinely sensed AI target.");Assert.That((int)Read(match,"ActualNoiseEvents"),Is.GreaterThan(0));Assert.That(violations,Is.Empty);
                yield return ObserveGenuineResultsBoard();
                Log("FULL_MATCH_PASSED run="+run+" results="+Read(match,"Results"));Persist();Call(match,"SetupScreen");yield return null;
                var board=ActualResultsBoard();
                Assert.That(Read(board,"Snapshot"),Is.Null,"Ordinary Setup retires the previous original match snapshot.");
            }
            CompleteLiveCavernObservation();completed=true;Persist();
        }
        finally
        {
            Persist();Time.timeScale=1;
            foreach(var pair in subscriptions){object source=pair.Key.DeclaringType.IsInstanceOfType(match)?(object)match:motor;if(source!=null)pair.Key.RemoveEventHandler(source,pair.Value);}subscriptions.Clear();
            if(match!=null)Call(match,"SetupScreen");if(inputIsolation!=null)UnityEngine.Object.DestroyImmediate(inputIsolation);preferences?.Dispose();
            Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);ReleaseLiveCavernObservation();
        }
    }
}
#endif
