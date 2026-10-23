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

public sealed class ContestResultsLifecycleTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Runtime(string name) => Type.GetType("LWS.TruckSports." + name + ", Assembly-CSharp", true);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(owner) : owner.GetType().GetProperty(name, Any).GetValue(owner);
    static void Set(object owner, string name, object value) { if (owner.GetType().GetField(name, Any) is FieldInfo field) field.SetValue(owner, value); else owner.GetType().GetProperty(name, Any).SetValue(owner, value); }
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(owner, args);
    static Rect Bounds(RectTransform rect) { var c = new Vector3[4]; rect.GetWorldCorners(c); return Rect.MinMaxRect(c.Min(v => v.x), c.Min(v => v.y), c.Max(v => v.x), c.Max(v => v.y)); }
    static string[] RuleArrays(object rules) => rules.GetType().GetFields(Any).Where(field => field.FieldType.IsArray).Select(field => field.Name + ":" + string.Join(",", ((Array)field.GetValue(rules)).Cast<object>())).ToArray();
    [UnityTest, Timeout(240000)]
    public IEnumerator SyntheticContestsPreserveSurvivorsTeamResultsDrawsAndDNFs()
    {
        foreach (string scene in new[] { "TruckHotPotato", "TruckBalloonBattle", "TruckTetherball", "TruckJousting", "TruckFrogger", "TruckRedLightGreenLight", "TruckVolleyball", "TruckFreezeTag" }) yield return Inspect(scene);
        yield return Inspect("TruckBalloonBattle", true);
        yield return Inspect("TruckRedLightGreenLight", false, true);
    }
    static IEnumerator Inspect(string scene, bool mutualPop = false, bool noLegal = false)
    {
        var scope = (IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "SyntheticContestResultBindings" });
        var isolation = new GameObject("Explicit synthetic rule values and actual saved results fixture"); UnityEngine.Object.DontDestroyOnLoad(isolation); isolation.AddComponent(Runtime("SportsProbeInputIsolation")); Component match = null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/" + scene + "/Scenes/" + scene + ".unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null; yield return null;
            match = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession")); var setup = Get(match, "setup"); Set(setup, "count", Get(match, "MaximumPlayers")); if(scene=="TruckVolleyball"||scene=="TruckFreezeTag"){Set(match,"teamA",4);Set(match,"teamB",4);} Call(match, "AllBots");
            foreach (object player in ((Array)Get(setup, "players")).Cast<object>().Take((int)Get(setup, "count"))) Set(player, "name", "Shared Name");
            Assert.That(Call(match, "StartMatch"), Is.True); ((Behaviour)match).enabled = false; Time.timeScale = 0; int count = (int)Get(setup, "count"); Assert.That(count,Is.EqualTo(Get(match,"MaximumPlayers")),"This fixture must exercise the full supported roster, including actual4v4 configuration."); var rules = Get(match, "rules");
            // These deliberately assigned values exercise display contracts only.
            // Actual physical tournament/survival/ball checks run separately.
            switch (scene)
            {
                case "TruckHotPotato":
                    ((int[])Get(rules,"scores"))[0]=900; ((int[])Get(rules,"scores"))[count-1]=140; Set(rules,"Winner",count-1); break;
                case "TruckBalloonBattle":
                    ((int[])Get(rules,"scores"))[0]=900; ((int[])Get(rules,"scores"))[count-1]=mutualPop?0:5;
                    var balloons=(bool[,])Get(rules,"balloons");Array.Clear(balloons,0,balloons.Length);if(!mutualPop)for(int i=0;i<3;i++)balloons[count-1,i]=true;
                    Set(rules,"Winner",mutualPop?-1:count-1); break;
                case "TruckTetherball":
                    ((int[])Get(rules,"wins"))[0]=2; ((int[])Get(rules,"wins"))[1]=1; Set(rules,"Winner",0); break;
                case "TruckJousting":
                    ((int[])Get(rules,"points"))[0]=10; ((int[])Get(rules,"points"))[1]=10; Set(rules,"Winner",-1); break;
                case "TruckFrogger":
                    ((int[])Get(rules,"points"))[0]=1; ((int[])Get(rules,"points"))[1]=1; ((float[,])Get(rules,"times"))[0,0]=20; ((float[,])Get(rules,"times"))[0,1]=10; Set(rules,"Winner",1); break;
                case "TruckRedLightGreenLight":
                    var outcomes=(Array)Get(rules,"outcomes"); var outcomeType=outcomes.GetType().GetElementType();
                    for(int i=0;i<count;i++) outcomes.SetValue(Enum.Parse(outcomeType,"Eliminated"),i);
                    if(!noLegal){outcomes.SetValue(Enum.Parse(outcomeType,"Finished"),0);outcomes.SetValue(Enum.Parse(outcomeType,"Finished"),1);((float[])Get(rules,"finishTimes"))[0]=20;((float[])Get(rules,"finishTimes"))[1]=10;}
                    Set(rules,"Winner",noLegal?-1:1);break;
                case "TruckVolleyball":
                    ((int[])Get(rules,"points"))[0]=15;((int[])Get(rules,"points"))[1]=13;((int[])Get(rules,"spikes"))[0]=12;((int[])Get(rules,"spikes"))[1]=8;((int[])Get(rules,"saves"))[0]=23;((int[])Get(rules,"saves"))[1]=17;Set(rules,"Winner",0);break;
                case "TruckFreezeTag":
                    ((int[])Get(rules,"wins"))[0]=2;((int[])Get(rules,"wins"))[1]=2;for(int i=0;i<count;i++){((int[])Get(rules,"tags"))[i]=i;((int[])Get(rules,"rescues"))[i]=count-i;}Set(rules,"Winner",-1);break;
            }
            Call(match, "EndMatch"); Time.timeScale = 0; var before = RuleArrays(rules); yield return null; yield return null;
            var board = (Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsResultsPresentation")); Assert.That(board, Is.Not.Null); Call(board, "CompleteAnimations"); Canvas.ForceUpdateCanvases();
            Assert.That(Get(board, "Displaying"), Is.True); var snapshot = Get(board, "Snapshot"); var entries = ((IEnumerable)Get(snapshot, "Rows")).Cast<object>().ToArray(); Assert.That(entries.Length, Is.EqualTo(count)); Assert.That(entries.Select(entry => Get(entry, "ParticipantId")).Distinct().Count(), Is.EqualTo(count));
            var views = ((Array)Get(board, "rows")).Cast<object>().Where(view => ((Component)Get(view, "rect")).gameObject.activeSelf).ToArray();
            for (int i = 0; i < entries.Length; i++)
            {
                int slot = (int)Get(entries[i], "Slot");
                int winner=(int)Get(rules,"Winner"), place; bool wins; double? measurement; string unit;
                if(scene=="TruckVolleyball" || scene=="TruckFreezeTag")
                {
                    int team=(int)Call(scene=="TruckVolleyball"?(object)match:rules,"Team",slot);
                    place=winner<0||team==winner?1:2;wins=winner<0||team==winner;
                    measurement=((int[])Get(rules,scene=="TruckVolleyball"?"points":"wins"))[team];unit=scene=="TruckVolleyball"?"TEAM PTS":"TEAM WINS";
                    StringAssert.StartsWith(team==0?"BLUE":"ORANGE",(string)Get(entries[i],"Detail"));
                    StringAssert.Contains(scene=="TruckVolleyball"?((int[])Get(rules,"spikes"))[team]+" AERIAL":((int[])Get(rules,"tags"))[slot]+" TAGS",(string)Get(entries[i],"Detail"));
                }
                else if(scene=="TruckRedLightGreenLight")
                {
                    bool finished=((Array)Get(rules,"outcomes")).GetValue(slot).ToString()=="Finished";
                    place=(int)Call(match,"ResultPlace",slot);wins=slot==winner;measurement=finished?(double?)((float[])Get(rules,"finishTimes"))[slot]:null;unit="SEC";
                }
                else
                {
                    place=scene=="TruckFrogger"?(int)Call(rules,"Place",slot):scene=="TruckTetherball"||scene=="TruckJousting"?(int)Call(match,"ResultPlace",slot):slot==winner?1:0;
                    wins=slot==winner || scene=="TruckJousting"&&winner<0;
                    string field=scene=="TruckTetherball"?"wins":scene=="TruckFrogger"||scene=="TruckJousting"?"points":"scores";
                    measurement=((int[])Get(rules,field))[slot];unit=scene=="TruckTetherball"||scene=="TruckFrogger"?"WINS":"PTS";
                }
                Assert.That(Get(entries[i],"Place"),Is.EqualTo(place));Assert.That(Get(entries[i],"Winner"),Is.EqualTo(wins));Assert.That(Get(entries[i],"Measurement"),Is.EqualTo(measurement));Assert.That(Get(entries[i],"Unit"),Is.EqualTo(unit));
                Assert.That(Get(Get(views[i],"value"),"text"),Is.EqualTo(Get(entries[i],"Value")));
            }
            if(!mutualPop && (scene=="TruckHotPotato" || scene=="TruckBalloonBattle")) Assert.That(Get(entries[0],"Slot"),Is.EqualTo(count-1),"Declared survivor wins even when an eliminated truck has more points.");
            if(scene=="TruckJousting" || scene=="TruckFreezeTag") Assert.That(entries.All(entry=>(bool)Get(entry,"Winner")),Is.True,"Original shared result preserves every winner.");
            if(mutualPop || noLegal) Assert.That(entries.Any(entry=>(bool)Get(entry,"Winner")),Is.False,"A draw or all-eliminated finish has no invented winner.");
            var menus = (Component)Get(match, "menus"); var page = ((GameObject[])Get(menus, "pages"))[6];
            Action<Camera> inspect = camera =>
            {
                Canvas.ForceUpdateCanvases(); var canvas = Bounds(menus.GetComponent<RectTransform>());
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
            foreach (var size in new[] { new Vector2Int(1280,720), new Vector2Int(1920,1080) }) save.Invoke(null, new object[] { match, size.x, size.y, "Logs/TruckSports/PlaytestRepair/ContestResults/" + scene + (mutualPop?"-draw":noLegal?"-no-finish":"") + "-" + size.y + ".png", inspect });
            CollectionAssert.AreEqual(before, RuleArrays(rules), "Rendering and counting values cannot change actual rule data.");
            ((Behaviour)match).enabled = true; Time.timeScale = 1; Call(match, "SetupScreen"); Assert.That(Get(board, "Snapshot"), Is.Null); Assert.That(Call(match, "StartMatch"), Is.True); yield return null; Assert.That(Get(board, "Snapshot"), Is.Null);
            TestContext.WriteLine("SYNTHETIC_CONTEST_RESULTS " + scene + " count=" + count + ": explicit synthetic rule values, actual saved Heat/DOTween result board; original ranks/ties/metrics/IDs, controlled720/1080 bounds/TMP and rematch. Not physical-match acceptance.");
        }
        finally { Time.timeScale = 1; if (match != null) { ((Behaviour)match).enabled = true; Call(match, "SetupScreen"); } UnityEngine.Object.DestroyImmediate(isolation); scope.Dispose(); }
    }
}
#endif
