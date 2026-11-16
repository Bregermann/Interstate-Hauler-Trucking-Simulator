#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class DragonEscapeMatchNativeTests
{
    Component match;
    IDisposable preferences;
    readonly StringBuilder report = new StringBuilder();
    string output;
    int fullExtractionRun;
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    static object Read(object value, string name) => value.GetType().GetField(name, Any) is FieldInfo f ? f.GetValue(value) : value.GetType().GetProperty(name, Any).GetValue(value);
    static object Call(object value, string name, params object[] args) => value.GetType().GetMethods(Any).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(value, args);
    void Log(string value)
    {
        report.AppendLine(value); TestContext.WriteLine(value);
        if (output != null) File.WriteAllText(Path.Combine(output, "observations.txt"), report.ToString());
    }
    [UnityTest, Timeout(900000)] public IEnumerator FourActualAiCrewsReachTheFortressThenRematch() { yield return Run("Fortress"); }
    [UnityTest, Timeout(900000)] public IEnumerator FourActualAiCrewsReachTheMountainThenRematch() { yield return Run("Mountain"); }
    [UnityTest, Timeout(900000)] public IEnumerator FourActualAiCrewsBoardAndLeaveOnTheRealBoatThenRematch() { yield return Run("Boat"); }
    IEnumerator Run(string kind)
    {
        report.Clear();
        var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "-tsDragonOutput");
        output = Path.GetFullPath(Path.Combine(at >= 0 ? args[at + 1] : Application.persistentDataPath, "EscapeFullAI", kind)); Directory.CreateDirectory(output);
        preferences = (IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "Escape" + Guid.NewGuid().ToString("N") });
        Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null);
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/TruckDragonEscape/Scenes/TruckDragonEscape.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null;
        match = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("LWS.TruckDragonEscape.DragonEscapeMatch")); Assert.That(match, Is.Not.Null);
        var definition = Read(Read(match, "world"), "definition");
        AssertSharedRoadJoinGeometry(definition);
        var create = Runtime("LWS.TruckDragonKingdom.DragonEscapeScenario").GetMethod("TryCreate"); int selected = 0;
        for (int seed = 1; seed <= 256; seed++)
        {
            var values = new object[] { Read(definition, "graph"), seed, Read(definition, "escapeStarts"), Read(definition, "extractions"), Read(definition, "closurePlans"), null, null, null };
            Assert.That((bool)create.Invoke(null, values), Is.True, "Use actual authored scenario definitions: " + values[7]);
            if (Read(Read(values[6], "Extraction"), "Kind").ToString() == kind) { selected = seed; break; }
        }
        Assert.That(selected, Is.GreaterThan(0), "Real seeded scenario candidates must include " + kind);
        match.GetType().GetField("fixedSeed").SetValue(match, selected); match.GetType().GetField("option").SetValue(match, 0);
        Read(match, "setup").GetType().GetField("count").SetValue(Read(match, "setup"), 4);
        Call(match, "AllBots"); Assert.That(Call(match, "StartMatch"), Is.EqualTo(true));
        string previousMatch=null,previousDestination=null;
        for(fullExtractionRun=1;fullExtractionRun<=2;fullExtractionRun++)
        {
        string identity=(string)Read(match,"MatchId");Assert.That(identity,Is.Not.Empty.And.Not.EqualTo(previousMatch));previousMatch=identity;
        var actualExtraction=Read(Read(match,"Scenario"),"Extraction");string actualKind=Read(actualExtraction,"Kind").ToString(),actualDestination=(string)Read(actualExtraction,"Id");
        Assert.That(actualKind=="Fortress"||actualKind=="Mountain"||actualKind=="Boat",Is.True);Assert.That(actualDestination,Is.Not.Empty);
        if(fullExtractionRun==1)Assert.That(actualKind,Is.EqualTo(kind),"First real route must cover the requested extraction type.");
        else Assert.That(actualDestination,Is.Not.EqualTo(previousDestination),"The established scenario policy deliberately selects a different destination on rematch.");
        previousDestination=actualDestination;
        Time.timeScale = 4;
        Log("ACTUAL_SAVED_ESCAPE kind=" + actualKind + " firstRequested=" + kind + " seed=" + selected + " simulationScale=4. No human, physical-device or normal-speed performance acceptance. No truck poses, commands, scores, contacts, objectives or animation callbacks are injected.");
        Capture("actual-start");
        var trucks = ((Array)Read(match, "trucks")).Cast<Component>().ToArray();
        var starts = trucks.Select(t => ((Rigidbody)Read(t, "body")).position).ToArray();
        float deadline = Time.realtimeSinceStartup + 360, next = 0; bool moved = false, paused = false, gameplayCaptured=false;
        while (!(bool)Read(match, "Finished") && Time.realtimeSinceStartup < deadline)
        {
            float clock = (float)Read(match, "Clock");
            moved |= trucks.Select((t, i) => Vector3.Distance(((Rigidbody)Read(t, "body")).position, starts[i])).Any(d => d > 40);
            if (clock >= next)
            {
                next = clock + 20;
                Log("clock=" + clock.ToString("F1") + " extracted=" + Read(match, "ActualExtractions") + " closures=" + Read(match, "CommittedClosures") + " " + string.Join(" | ", trucks.Select((t, i) =>
                {
                    var body = (Rigidbody)Read(t, "body"); var route = Call(match, "NavigationRoute", i);
                    return "crew" + i + " position=" + body.position.ToString("F1") + " speed=" + body.linearVelocity.magnitude.ToString("F1") + " searches=" + Read(route, "Searches") + " cursor="+Read(route,"Cursor")+" desired="+Read(route,"DesiredSpeed")+" recovery="+Read(route,"RecoveryStarts")+" retraceJoins="+Read(match,"SharedRoadRetracesCancelled")+" repeatedBuilds="+Read(match,"RepeatedRouteBuilds")+ " obstruction=" + Read(route, "BlockingObstacle");
                })));
            }
            if(moved&&clock>20&&!gameplayCaptured)
            {
                Assert.That((float)Read(match,"Intro"),Is.Zero);
                Assert.That((float)Read(match,"RaceCountdownSeconds"),Is.LessThan(0),"A completed countdown must use the existing shared display sentinel, never permanent zero/beat1.");
                var countdown=UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckTerrierRacing.TerrierCountdown"),FindObjectsInactive.Include,FindObjectsSortMode.None).Cast<Component>().Single(value=>ReferenceEquals(Read(value,"Session"),match));
                Assert.That((int)Read(countdown,"Beat"),Is.EqualTo(-1),"The original shared Heat/DOTween countdown must be hidden during real driving.");
                Assert.That(((RectTransform[])Read(countdown,"panels")).All(panel=>!panel.gameObject.activeSelf),Is.True,"No viewport may retain the startup1badge during active extraction.");
                Capture("actual-route-progress");gameplayCaptured=true;
            }
            if (!paused && clock > 12)
            {
                Call(match, "Pause", 0, false); Assert.That(Read(match, "Paused"), Is.EqualTo(true));
                float stoppedClock = (float)Read(match, "Clock"); yield return new WaitForSecondsRealtime(.2f);
                Assert.That((float)Read(match, "Clock"), Is.EqualTo(stoppedClock)); Capture("ordinary-pause"); Call(match, "Resume");
                // Ordinary Resume correctly restores normal player speed. This
                // fixture explicitly declares4x; reapply only after proving
                // that real pause froze its clock. No physics step is injected.
                Time.timeScale = 4; paused = true;
            }
            yield return null;
        }
        Assert.That((int)Read(match,"RepeatedRouteBuilds"),Is.Zero,"The base duplicate planning call must remain cached; the driver still steps only once.");
        Log("TERMINAL finished=" + Read(match, "Finished") + " actualExtractions=" + Read(match, "ActualExtractions") + " results=" + Read(match, "Results"));
        Assert.That(moved, Is.True, "Actual NWH AI must drive its route."); Assert.That(Read(match, "Finished"), Is.EqualTo(true));
        Assert.That((int)Read(match, "ActualExtractions"), Is.GreaterThan(0), "A deadline or total elimination is not a successful extraction match.");
        var times = (float[])Read(match, "ExtractionTimes"); var scores = (int[])Read(match, "Scores");
        for (int i = 0; i < 4; i++) Assert.That(scores[i] > 0, Is.EqualTo(times[i] >= 0), "Only actual extraction receives place points.");
        if (actualKind == "Boat")
        {
            var world = Read(match, "world"); var site = Call(world, "Extraction", "boat"); var vessel = Read(site, "vessel");
            Assert.That(Read(vessel, "Sailing"), Is.EqualTo(true)); Assert.That((float)Read(vessel, "DistanceSailed"), Is.GreaterThanOrEqualTo(12));
        }
        yield return new WaitForSecondsRealtime(1.2f);Capture("ordinary-results");
        if(fullExtractionRun==1)
        {
        Assert.That(Call(match, "StartMatch"), Is.EqualTo(true)); yield return new WaitForFixedUpdate();
        Assert.That(Read(match, "ActualExtractions"), Is.EqualTo(0)); Assert.That(Read(match, "CommittedClosures"), Is.EqualTo(0));
        foreach (var value in (float[])Read(match, "ExtractionTimes")) Assert.That(value, Is.EqualTo(-1));
        foreach (var score in (int[])Read(match, "Scores")) Assert.That(score, Is.Zero);
        Assert.That((float)Read(match, "Intro"), Is.GreaterThan(0));
        Log("REMATCH real original StartMatch reset extraction, closure, score and countdown authorities.");
        Capture("ordinary-rematch");
        }
        }
    }
    static System.Collections.Generic.List<Vector3> SharedJoinPath(object graph, string incoming, string outgoing)
    {
        object Edge(string id) => ((IEnumerable)Read(graph,"edges")).Cast<object>().Single(e=>(string)Read(e,"edgeId")==id);
        Vector3[] Leg(string id,bool toward)
        {
            var edge=Edge(id);var points=((IEnumerable)Read(edge,"samples")).Cast<object>().Select(s=>(Vector3)Read(s,"position")).ToArray();
            bool from=(string)Read(edge,"fromNodeId")=="caverns";if(from==toward)Array.Reverse(points);return points;
        }
        return Leg(incoming,true).Concat(Leg(outgoing,false).Skip(1)).ToList();
    }
    static double RoadLength(System.Collections.Generic.IReadOnlyList<Vector3> points)
    {
        double sum=0;for(int i=1;i<points.Count;i++){double x=(double)points[i].x-points[i-1].x,y=(double)points[i].y-points[i-1].y,z=(double)points[i].z-points[i-1].z;sum+=Math.Sqrt(x*x+y*y+z*z);}return sum;
    }
    void AssertSharedRoadJoinGeometry(object definition)
    {
        var graph=Read(definition,"graph");string original=JsonUtility.ToJson(graph);var method=Runtime("LWS.TruckDragonEscape.DragonEscapeMatch").GetMethod("CancelSharedRoadRetraces",BindingFlags.Public|BindingFlags.Static);Assert.That(method,Is.Not.Null);int positives=0,negatives=0;
        int Normalize(System.Collections.Generic.List<Vector3> points,object source,string[] ids)=>(int)method.Invoke(null,new object[]{points,source,ids});
        foreach(string other in new[]{"harbor-caverns","castle-caverns"})foreach(bool reverse in new[]{false,true})
        {
            string a=reverse?other:"caverns-volcano",b=reverse?"caverns-volcano":other;var points=SharedJoinPath(graph,a,b);var before=points.ToArray();double length=RoadLength(points);
            Assert.That(Normalize(points,graph,new[]{a,b}),Is.EqualTo(1));Assert.That(points[0].Equals(before[0])&&points[points.Count-1].Equals(before[before.Length-1]),Is.True,"Physical start and goal must remain exact.");
            Assert.That(length-RoadLength(points),Is.EqualTo(360).Within(.000001),"Only the actual180m repeated cavern stem may disappear.");
            for(int i=1;i<points.Count;i++)Assert.That(Enumerable.Range(1,before.Length-1).Any(j=>before[j-1].Equals(points[i-1])&&before[j].Equals(points[i])),Is.True,"Every retained segment must already exist on the original planned physical road; no shortcut segment is invented.");
            var once=points.ToArray();Assert.That(Normalize(points,graph,new[]{a,b}),Is.Zero);Assert.That(points.SequenceEqual(once),Is.True);positives++;
        }
        var raw=SharedJoinPath(graph,"caverns-volcano","harbor-caverns");var ids=new[]{"caverns-volcano","harbor-caverns"};
        void Reject(string name,object source,System.Collections.Generic.List<Vector3> points,string[] edgeIds){var before=points.ToArray();Assert.That(Normalize(points,source,edgeIds),Is.Zero,name);Assert.That(points.SequenceEqual(before),Is.True,"Rejected geometry must remain byte-exact: "+name);negatives++;}
        object Copy()=>JsonUtility.FromJson(original,graph.GetType());
        object Edge(object source,string id)=>((IEnumerable)Read(source,"edges")).Cast<object>().Single(e=>(string)Read(e,"edgeId")==id);
        void ShiftSample(object source,Vector3 delta){var edge=Edge(source,"caverns-volcano");var samples=(IList)Read(edge,"samples");var sample=samples[1];sample.GetType().GetField("position").SetValue(sample,(Vector3)Read(sample,"position")+delta);samples[1]=sample;}
        Reject("unknown edge",graph,raw.ToList(),new[]{"unknown","harbor-caverns"});Reject("nonconsecutive edges",graph,raw.ToList(),new[]{"caverns-volcano","forest-caverns","harbor-caverns"});Reject("same edge",graph,raw.ToList(),new[]{"caverns-volcano","caverns-volcano"});
        foreach(string id in ids){var bad=Copy();Edge(bad,id).GetType().GetField("oneWay").SetValue(Edge(bad,id),true);Reject("one-way "+id,bad,raw.ToList(),ids);}
        foreach(string id in ids){var bad=Copy();var restrictions=(IList)Read(Edge(bad,id),"restrictions");var entry=Activator.CreateInstance(restrictions.GetType().GetGenericArguments()[0]);entry.GetType().GetField("truckAllowed").SetValue(entry,false);restrictions.Add(entry);Reject("truck restriction "+id,bad,raw.ToList(),ids);}
        foreach(var delta in new[]{new Vector3(0,.02f,0),new Vector3(0,0,.02f)}){var bad=Copy();ShiftSample(bad,delta);Reject("nonshared source geometry",bad,raw.ToList(),ids);}
        var crooked=raw.ToList();int joint=crooked.FindIndex(p=>p.Equals(new Vector3(520,105,780)));crooked[joint-1]+=new Vector3(0,0,.02f);Reject("unproved path point",graph,crooked,ids);
        var truncated=raw.Take(raw.FindLastIndex(p=>p.Equals(new Vector3(700,105,780)))+1).ToList();Reject("goal may not be discarded",graph,truncated,ids);
        Assert.That(JsonUtility.ToJson(graph),Is.EqualTo(original),"Actual authored graph and every source edge ID stay unchanged.");
        File.WriteAllText(Path.Combine(output,"shared-road-join-contract.json"),"{\"actualSourceGeometryCases\":"+positives+",\"negativeCases\":"+negatives+",\"cancelledRetraceMeters\":360,\"allRetainedSegmentsOriginal\":true,\"graphUnchanged\":true,\"noVehicleOrCameraPoseChanged\":true}");
        Log("SHARED_ROAD_JOIN_GEOMETRY actual="+positives+" negatives="+negatives+" removed=360m allRemainingSegmentsOriginal=true graphUnchanged=true; actual collision/traction and extraction still require the unchanged real saved-game assertions below.");
    }

    void Capture(string state)
    {
        state="extraction"+fullExtractionRun+"-"+state;
        var method=Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(value=>value.Name=="Save"&&value.GetParameters().Length==5);
        foreach(int width in new[]{1280,1920})
        {
            int height=width==1280?720:1080;string path=Path.Combine(output,state+"-"+width+".png");
            method.Invoke(null,new object[]{match,width,height,path,null});
            Assert.That(File.Exists(path),Is.True,"Actual saved gameplay/results cameras must render; no supplied image substitutes for this capture.");
        }
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        Time.timeScale = 1;
        if (match != null) Call(match, "SetupScreen");
        var original = SceneManager.GetActiveScene(); var empty = SceneManager.CreateScene("Escape acceptance cleanup"); SceneManager.SetActiveScene(empty);
        if (original.IsValid() && original.isLoaded) yield return SceneManager.UnloadSceneAsync(original);
        preferences?.Dispose(); preferences = null; match = null;
        Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null, null);
    }
}
#endif
