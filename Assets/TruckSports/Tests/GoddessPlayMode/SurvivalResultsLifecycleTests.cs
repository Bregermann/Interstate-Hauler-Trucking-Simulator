#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class SurvivalResultsLifecycleTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) { if (owner.GetType().GetField(name, Any) is FieldInfo field) field.SetValue(owner, value); else owner.GetType().GetProperty(name, Any).SetValue(owner, value); }
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    static Rect Bounds(RectTransform rect) { var c = new Vector3[4]; rect.GetWorldCorners(c); return Rect.MinMaxRect(c.Min(v => v.x), c.Min(v => v.y), c.Max(v => v.x), c.Max(v => v.y)); }
    [UnityTest, Timeout(240000)]
    public IEnumerator SyntheticFiveSurvivalBoardsPreserveOriginalOutcomeAuthority()
    {
        foreach (string scene in new[] { "TruckMinefield", "TruckFishing", "TruckSumo", "TruckLogRolling", "TruckLimbo" }) yield return Inspect(scene);
        yield return Inspect("TruckFishing", true); yield return Inspect("TruckFishing", solo:true); yield return Inspect("TruckLimbo", true); yield return Inspect("TruckLimbo", false, true); yield return Inspect("TruckMinefield", false, false, true);
    }
    static IEnumerator Inspect(string scene, bool shared = false, bool bestHeight = false, bool wideName = false, bool solo = false)
    {
        var scope = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "SyntheticSurvivalResultBindings" });
        var isolation = new GameObject("Explicit synthetic rule values and actual saved results fixture"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation")); Component match = null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/" + scene + "/Scenes/" + scene + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession")); if(bestHeight)Set(match,"option",4);var setup = Get(match, "setup"); Set(setup, "count", solo?1:Get(match, "MaximumPlayers")); Call(match, "AllBots");
            foreach (object player in ((Array)Get(setup, "players")).Cast<object>().Take((int)Get(setup, "count"))) Set(player, "name", wideName ? new string('W', 40) : "Shared Name");
            Assert.That(Call(match, "StartMatch"), Is.True); ((Behaviour)match).enabled = false; Time.timeScale = 0; int count = (int)Get(setup, "count"); Assert.That(count,Is.EqualTo(solo?1:Get(match,"MaximumPlayers")),"Exercise maximum supported roster or explicit solo case.");
            var rules = scene=="TruckMinefield"?(object)match:Get(match,"rules");
            string metricField = scene=="TruckMinefield"||scene=="TruckLimbo"?"points":scene=="TruckFishing"?"caught":"wins";
            var metrics=(int[])Get(rules,metricField);
            for(int i=0;i<count;i++)metrics[i]=0;
            if(scene=="TruckMinefield") { metrics[0]=7;metrics[1]=1;Set(match,"Winner",1);((int[])Get(match,"personalBest"))[0]=1000;((int[])Get(match,"personalBest"))[1]=16030;var racers=(Array)Get(match,"racers");var finisher=racers.GetValue(1);Set(finisher,"Phase",Enum.Parse(Get(finisher,"Phase").GetType(),"DrivenFinish"));Set(finisher,"Distance",16030); }
            if(scene=="TruckFishing")
            {
                metrics[0]=100;metrics[1]=30;Set(rules,"Winner",shared||solo?-1:1);Set(rules,"Complete",true);Set(match,"Clock",72d);
                var alive=(bool[])Get(rules,"alive");var times=(float[])Get(match,"eliminatedAt");var reasons=(string[])Get(rules,"reason");var missed=(int[])Get(rules,"missed");
                for(int i=0;i<count;i++){alive[i]=!shared&&!solo&&i==1;times[i]=shared||solo?72:60-i;reasons[i]=alive[i]?"":"20 LB MISSED";missed[i]=alive[i]?5:20;}
                Runtime("SportsPrefs").GetMethod("SetFloat").Invoke(null,new object[]{"TruckFishing.SurvivalPB",123.4f});Runtime("SportsPrefs").GetMethod("SetInt").Invoke(null,new object[]{"TruckFishing.CaughtPB",567});
            }
            if(scene=="TruckSumo"||scene=="TruckLogRolling")
            { metrics[0]=4;metrics[1]=5;Set(rules,"Winner",1);for(int i=0;i<count;i++)((bool[])Get(rules,"alive"))[i]=i==1;if(scene=="TruckSumo"){((int[])Get(rules,"points"))[0]=42;((int[])Get(rules,"points"))[1]=25;((int[])Get(rules,"ringouts"))[0]=30;((int[])Get(rules,"ringouts"))[1]=10;} }
            if(scene=="TruckLimbo")
            {
                var winners=(bool[])Get(rules,"winners");var best=(float[])Get(rules,"best");Set(rules,"Complete",true);
                for(int i=0;i<count;i++){best[i]=float.PositiveInfinity;winners[i]=shared;}
                if(!shared){metrics[0]=500;metrics[1]=metrics[2]=400;best[0]=.9f;best[1]=best[2]=.8f;winners[1]=winners[2]=true;((int[])Get(rules,"clears"))[1]=2;}
            }
            Call(match, "EndMatch"); Time.timeScale = 0; var before = metrics.Cast<object>().ToArray(); yield return new WaitForSecondsRealtime(.22f); yield return null;
            var board = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsResultsPresentation")); Assert.That(board, Is.Not.Null); Call(board, "CompleteAnimations"); Canvas.ForceUpdateCanvases();
            Assert.That(Get(board, "Displaying"), Is.True); var snapshot = Get(board, "Snapshot"); var entries = ((IEnumerable)Get(snapshot, "Rows")).Cast<object>().ToArray(); Assert.That(entries.Length, Is.EqualTo(count)); Assert.That(entries.Select(entry => Get(entry, "ParticipantId")).Distinct().Count(), Is.EqualTo(count));
            var views = ((Array)Get(board, "rows")).Cast<object>().Where(view => ((Component)Get(view, "rect")).gameObject.activeSelf).ToArray();
            for (int i = 0; i < entries.Length; i++)
            {
                int slot=(int)Get(entries[i],"Slot"),place=(int)Call(match,"ResultPlace",slot);
                bool winner=scene=="TruckLimbo"?((bool[])Get(rules,"winners"))[slot]:(int)Get(rules,"Winner")==slot;
                double value=metrics[slot];
                Assert.That(Get(entries[i],"Place"),Is.EqualTo(place));Assert.That(Get(entries[i],"Winner"),Is.EqualTo(winner));Assert.That(Get(entries[i],"Measurement"),Is.EqualTo(value));
                Assert.That(Get(entries[i],"Unit"),Is.EqualTo(scene=="TruckFishing"?"LB":scene=="TruckSumo"||scene=="TruckLogRolling"?"WINS":"PTS"));
                Assert.That(Get(Get(views[i],"value"),"text"),Is.EqualTo(Get(entries[i],"Value")));
                if(scene=="TruckMinefield"&&slot==1){string detail=(string)Get(entries[i],"Detail");StringAssert.Contains("CROSSED THE FINISH",detail);StringAssert.Contains((string)Runtime("SportsMeasure").GetMethod("Feet",new[]{typeof(float)}).Invoke(null,new object[]{1603f}),detail,"A direct-finisher still retains the original measured distance.");}
            }
            if(scene=="TruckMinefield"||scene=="TruckFishing"&&!shared&&!solo) Assert.That(Get(entries[0],"Slot"),Is.EqualTo(1),"Existing winner takes precedence over another participant's larger displayed metric.");
            if(scene=="TruckFishing"&&(shared||solo))Assert.That(entries.Any(entry=>(bool)Get(entry,"Winner")),Is.False,"Actual simultaneous elimination has no winner.");
            if(scene=="TruckLimbo")Assert.That(entries.Count(entry=>(bool)Get(entry,"Winner")),Is.EqualTo(shared?count:2),"Original shared/NO CLEAR winner flags remain intact.");
            if(scene=="TruckFishing"){StringAssert.Contains("SOLO BEST 123.4 SEC / 567 LB",(string)Get(snapshot,"Summary"),"Retain the two original stored record values even in multiplayer results.");if(solo)StringAssert.DoesNotContain("WINS",(string)Get(snapshot,"Summary"),"Solo survival elimination has no invented match winner.");}
            var menus = (Component)Get(match, "menus"); var page = ((GameObject[])Get(menus, "pages"))[6];
            var readability = page.transform.Find("Results readability backing"); Assert.That(readability, Is.Not.Null, "Busy stadium/environment text must remain behind the results.");
            var image = readability.GetComponent(Type.GetType("UnityEngine.UI.Image, UnityEngine.UI", true)); Assert.That(((Color)Get(image,"color")).a, Is.GreaterThanOrEqualTo(.9f)); Assert.That(Get(image,"raycastTarget"), Is.False); Assert.That(readability.GetSiblingIndex(),Is.Zero);
            Action<Camera> inspect = camera =>
            {
                Canvas.ForceUpdateCanvases(); var canvas = Bounds(menus.GetComponent<RectTransform>());
                foreach(string field in new[]{"heading","summary"}){var label=Get(board,field);Call(label,"ForceMeshUpdate",false,false);Assert.That(Get(label,"isTextOverflowing"),Is.False,"Original record and match captions must fit: "+scene);}
                for (int i = 0; i < views.Length; i++)
                {
                    var view = views[i]; var row = Bounds((RectTransform)Get(view, "rect")); Assert.That(row.xMin, Is.GreaterThanOrEqualTo(canvas.xMin)); Assert.That(row.xMax, Is.LessThanOrEqualTo(canvas.xMax)); Assert.That(row.yMin, Is.GreaterThanOrEqualTo(canvas.yMin)); Assert.That(row.yMax, Is.LessThanOrEqualTo(canvas.yMax));
                    foreach (var action in page.GetComponentsInChildren(Runtime("SportsMenuAction"), true).Cast<Component>()) Assert.That(row.Overlaps(Bounds(action.GetComponent<RectTransform>())), Is.False, "Results must not cover existing replay/rematch controls.");
                    foreach (string heading in new[] { "Title", "Subtitle" }) Assert.That(row.Overlaps(Bounds(page.transform.Find(heading).GetComponent<RectTransform>())), Is.False);
                    foreach (string field in new[] { "name", "value", "detail" }) { var label = Get(view, field); Call(label, "ForceMeshUpdate", false, false); Assert.That(Get(label, "isTextOverflowing"), Is.False, "Actual result text must fit: " + scene + " / " + field); }
                    for (int j = i + 1; j < views.Length; j++) Assert.That(row.Overlaps(Bounds((RectTransform)Get(views[j], "rect"))), Is.False);
                }
            };
            var save = Runtime("SportsControlledCapture").GetMethods().Single(method => method.Name == "Save" && method.GetParameters().Length == 5);
            foreach (var size in new[] { new Vector2Int(1280,720), new Vector2Int(1920,1080) }) save.Invoke(null, new object[] { match, size.x, size.y, "Logs/TruckSports/PlaytestRepair/SurvivalResults/" + scene + (shared?"-shared":bestHeight?"-best-height":wideName?"-wide-name":solo?"-solo":"") + "-" + size.y + ".png", inspect });
            if(scene=="TruckFishing"){Assert.That(Get(match,"ResultSurvivalBest"),Is.EqualTo(123.4f));Assert.That(Get(match,"ResultCaughtBest"),Is.EqualTo(567));}
            CollectionAssert.AreEqual(before, metrics.Cast<object>().ToArray(), "Rendering and counting values cannot change actual rule data.");
            ((Behaviour)match).enabled = true; Time.timeScale = 1; Call(match, "SetupScreen"); Assert.That(Get(board, "Snapshot"), Is.Null); Assert.That(Call(match, "StartMatch"), Is.True); yield return null; Assert.That(Get(board, "Snapshot"), Is.Null);if(scene=="TruckFishing"){Assert.That(Get(match,"ResultSurvivalBest"),Is.EqualTo(123.4f));Assert.That(Get(match,"ResultCaughtBest"),Is.EqualTo(567));}
            TestContext.WriteLine("SYNTHETIC_SURVIVAL_RESULTS " + scene + " count=" + count + ": explicit synthetic rule values, actual saved Heat/DOTween result board; original ranks/ties/metrics/IDs, controlled720/1080 bounds/TMP and rematch. Not physical-match acceptance.");
        }
        finally { Time.timeScale = 1; if (match != null) { ((Behaviour)match).enabled = true; Call(match, "SetupScreen"); } UnityEngine.Object.DestroyImmediate(isolation); scope.Dispose(); }
    }
}
#endif
