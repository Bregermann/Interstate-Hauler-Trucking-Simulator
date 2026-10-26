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

// Explicit presentation snapshots: these tests never claim physical distances,
// deliveries or race wins from their deliberately supplied model values.
public sealed class MineMetricAndSeriesCopyTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length&&m.GetParameters().Select((p,i)=>args[i]==null||p.ParameterType.IsInstanceOfType(args[i])).All(v=>v)).Invoke(owner,args);
    static Rect Bounds(RectTransform rect){var c=new Vector3[4];rect.GetWorldCorners(c);return Rect.MinMaxRect(c[0].x,c[0].y,c[2].x,c[2].y);}
    static void Capture(Component match,string name,Action<Camera> inspect)
    {
        var save=Runtime("SportsControlledCapture").GetMethods().Single(m=>m.Name=="Save"&&m.GetParameters().Length==5);
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})save.Invoke(null,new object[]{match,size.x,size.y,"Logs/TruckSports/PlaytestRepair/MineMetricCopy/"+name+"-"+size.y+".png",inspect});
    }
    [UnityTest,Timeout(180000)]public IEnumerator SyntheticMineDistancesKeepOriginalPrecisionAndVisibleUnitsInsideEightCards()
    {
        var prefs=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"ExplicitMineDistanceUI"});var isolation=new GameObject("Explicit synthetic distance UI snapshot, not physical landings");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));Component match=null;Array racers=null;int[] oldDistances=null,oldPoints=null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckMinefield/Scenes/TruckMinefield.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession"));Call(match,"AllBots");Runtime("SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{false});Assert.That(Call(match,"StartMatch"),Is.True);((Behaviour)match).enabled=false;Time.timeScale=0;
            racers=(Array)Get(match,"racers");oldDistances=racers.Cast<object>().Select(x=>(int)Get(x,"Distance")).ToArray();var points=(int[])Get(match,"points");oldPoints=(int[])points.Clone();int[] distances={0,1,25,4999,16093,27500,84000,110000};
            for(int i=0;i<8;i++){Set(racers.GetValue(i),"<Distance>k__BackingField",distances[i]);points[i]=i;}
            yield return new WaitForSecondsRealtime(.25f);var scoreCards=UnityEngine.Object.FindAnyObjectByType(Runtime("SportsBroadcastScores"));Assert.That(scoreCards,Is.Not.Null);var cards=(Array)Get(scoreCards,"cards");
            Action<Camera> inspect=camera=>
            {
                Canvas.ForceUpdateCanvases();for(int i=0;i<8;i++)
                {
                    object[] args={i,null,null};Assert.That(match.GetType().GetMethod("TryReadScoreCard").Invoke(match,args),Is.True);string original=(string)Runtime("SportsMeasure").GetMethod("Feet").Invoke(null,new object[]{distances[i]*.1f});Assert.That(args[1]+" ft",Is.EqualTo(original),"Original one-decimal foot measurement and rounding remain exact.");Assert.That(args[2],Is.EqualTo("FT · "+i+" PTS"));
                    var card=cards.GetValue(i);var value=(Component)Get(card,"value");var detail=(Component)Get(card,"detail");Assert.That(Get(value,"text"),Is.EqualTo(args[1]));Assert.That(Get(detail,"text"),Is.EqualTo(args[2]));Call(value,"ForceMeshUpdate",false,false);Call(detail,"ForceMeshUpdate",false,false);Assert.That(Get(value,"isTextOverflowing"),Is.False,"Distance must never become ellipses.");Assert.That(Get(detail,"isTextOverflowing"),Is.False,"Units and heat points must remain visible.");Assert.That(detail.gameObject.activeInHierarchy,Is.True);Assert.That(Bounds(value.GetComponent<RectTransform>()).Overlaps(Bounds(detail.GetComponent<RectTransform>())),Is.False);var outer=Bounds(((GameObject)Get(card,"root")).GetComponent<RectTransform>());Assert.That(outer.Contains(Bounds(value.GetComponent<RectTransform>()).center),Is.True);
                }
            };
            Capture(match,"synthetic-distance-boundaries",inspect);CollectionAssert.AreEqual(distances,racers.Cast<object>().Select(x=>(int)Get(x,"Distance")).ToArray());CollectionAssert.AreEqual(Enumerable.Range(0,8).ToArray(),points);Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.Zero,"Rendering synthetic UI data must not create score receipts.");
            TestContext.WriteLine("EXPLICIT_SYNTHETIC_MINE_UI: eight supplied distance/heat-point model values, original precision, visible FT, non-overlap and no TMP overflow at720p/1080p. No physical launch/distance or human acceptance.");
        }
        finally{if(match!=null){if(oldDistances!=null)for(int i=0;i<8;i++)Set(racers.GetValue(i),"<Distance>k__BackingField",oldDistances[i]);if(oldPoints!=null)Array.Copy(oldPoints,(int[])Get(match,"points"),8);((Behaviour)match).enabled=true;Call(match,"SetupScreen");}Time.timeScale=1;UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();}
    }
    [UnityTest,Timeout(180000)]public IEnumerator SyntheticMattressSeriesUsesSingularRaceWinWithoutChangingTotals()
    {
        var prefs=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"ExplicitMattressStatCopy"});var isolation=new GameObject("Explicit synthetic series-result copy fixture");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));Component match=null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckMattressRacing/Scenes/TruckMattressRacing.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession"));Call(match,"AllBots");Assert.That(Call(match,"StartMatch"),Is.True);((Behaviour)match).enabled=false;Time.timeScale=0;var series=Get(match,"series");Call(series,"Reset",8,true);
            for(int race=0;race<3;race++){Call(series,"BeginRound");var times=Enumerable.Range(0,8).Select(i=>(float)(i+3)).ToArray();times[0]=race==0?1:2;times[1]=race==0?2:1;Assert.That(Call(series,"Resolve",times),Is.True);}
            var totals=(int[])Get(series,"total");var before=(int[])totals.Clone();var finishes=(int[])Get(series,"finishes");var originalFinishes=(int[])finishes.Clone();Assert.That(finishes[0],Is.EqualTo(1));Assert.That(finishes[8],Is.EqualTo(2));Call(match,"EndMatch");yield return new WaitForSecondsRealtime(.2f);yield return null;
            var board=UnityEngine.Object.FindAnyObjectByType(Runtime("SportsResultsPresentation"));Call(board,"CompleteAnimations");var snapshot=Get(board,"Snapshot");var entries=((IEnumerable)Get(snapshot,"Rows")).Cast<object>().ToArray();Assert.That(entries.Length,Is.EqualTo(8));var views=(Array)Get(board,"rows");
            for(int i=0;i<8;i++){var entry=entries.Single(x=>(int)Get(x,"Slot")==i);int wins=finishes[i*8];string expected=wins+(wins==1?" RACE WIN · FINAL RACE ":" RACE WINS · FINAL RACE ")+"#"+((int[])Get(series,"ranks"))[i];Assert.That(Get(entry,"Detail"),Is.EqualTo(expected));Assert.That(Get(entry,"Measurement"),Is.EqualTo((double)totals[i]));Assert.That(Get(entry,"Place"),Is.EqualTo(Call(match,"ResultPlace",i)));}
            Action<Camera> inspect=camera=>{Canvas.ForceUpdateCanvases();foreach(var view in views.Cast<object>().Where(v=>((Component)Get(v,"rect")).gameObject.activeInHierarchy))foreach(string name in new[]{"value","detail"}){var text=Get(view,name);Call(text,"ForceMeshUpdate",false,false);Assert.That(Get(text,"isTextOverflowing"),Is.False);}};
            Capture(match,"synthetic-mattress-race-win",inspect);CollectionAssert.AreEqual(before,totals);CollectionAssert.AreEqual(originalFinishes,finishes);TestContext.WriteLine("EXPLICIT_SYNTHETIC_MATTRESS_COPY: original series model resolves supplied finish timestamps; saved result board displays0/1/2race wins with correct copy and unchanged totals, places and history. Not a physical race or human playtest.");
        }
        finally{Time.timeScale=1;if(match!=null){((Behaviour)match).enabled=true;Call(match,"SetupScreen");}UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();}
    }
}
#endif
