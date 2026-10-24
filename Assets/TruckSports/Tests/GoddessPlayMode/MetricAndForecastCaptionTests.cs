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
public sealed class MetricAndForecastCaptionTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length&&m.GetParameters().Select((p,i)=>args[i]==null||p.ParameterType.IsInstanceOfType(args[i])).All(v=>v)).Invoke(owner,args);
    static void Capture(Component match,string name)
    {
        var save=Runtime("SportsControlledCapture").GetMethods().Single(m=>m.Name=="Save"&&m.GetParameters().Length==5);
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})save.Invoke(null,new object[]{match,size.x,size.y,"Logs/TruckSports/PlaytestRepair/MetricForecast/"+name+"-"+size.y+".png",null});
    }
    [UnityTest,Timeout(180000)]public IEnumerator SyntheticMetricRowsKeepRealDataAndAnimatedLabelsConsistent()
    {
        var prefs=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"ExplicitResultMetricUIFixture"});Component match=null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckLogRolling/Scenes/TruckLogRolling.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession"));Call(match,"SetupScreen");var menus=(Component)Get(match,"menus");Call(menus,"Show",Enum.Parse(Runtime("SportsEventScreen"),"Result"));
            var board=menus.GetComponentsInChildren(Runtime("SportsResultsPresentation"),true).Single();
            string[] units={"WINS","TEAM WINS","TEAM DELIVERIES","CUPS HIT","TEAM CUPS HIT","FARES","PTS","WINS"};string[] expected={"1 WIN","1 TEAM WIN","1 TEAM DELIVERY","1 CUP HIT","1 TEAM CUP HIT","1 FARE","1 PTS","2 WINS"};
            var rows=Array.CreateInstance(Runtime("SportsResultRow"),units.Length);
            for(int i=0;i<units.Length;i++)
            {
                var player=Activator.CreateInstance(Type.GetType("LWS.TruckBowling.BowlingPlayer, Assembly-CSharp",true));Set(player,"participantId","metric-ui:"+i);Set(player,"name","Same Display Name");Set(player,"color",Color.HSVToRGB(i/8f,.7f,1));
                rows.SetValue(Activator.CreateInstance(Runtime("SportsResultRow"),player,i,0,false,(double?)(i==7?2:1),units[i],"SYNTHETIC UI DATA · NO GAMEPLAY","DNF","0"),i);
            }
            var snapshot=Activator.CreateInstance(Runtime("SportsResultsSnapshot"),"RESULT UI FIXTURE · NO GAMEPLAY","EXPLICIT SYNTHETIC COUNTS · ORIGINAL RULES UNCHANGED",rows);Call(board,"Render",snapshot);Call(board,"CompleteAnimations");Canvas.ForceUpdateCanvases();yield return null;
            var views=(Array)Get(board,"rows");for(int i=0;i<expected.Length;i++){var label=Get(views.GetValue(i),"value");Assert.That(Get(label,"text"),Is.EqualTo(expected[i]));Call(label,"ForceMeshUpdate",false,false);Assert.That(Get(label,"isTextOverflowing"),Is.False);Assert.That(Get(rows.GetValue(i),"Unit"),Is.EqualTo(units[i]));}
            Assert.That(Get(match,"Running"),Is.False);Assert.That(((int[])Get(Get(match,"rules"),"wins")).All(v=>v==0),Is.True);Capture(match,"synthetic-metric-rows");
            TestContext.WriteLine("Explicit synthetic result UI fixture only; original saved Heat/DOTween board, eight stable identities, original Unit/Measurement retained, actual rendered count labels. No physical match or human claim.");
        }
        finally{if(match!=null)Call(match,"SetupScreen");Time.timeScale=1;prefs.Dispose();}
    }
    [UnityTest,Timeout(180000)]public IEnumerator BogCaptionFollowsSelectedInstalledForecastRatherThanLegacyChoice()
    {
        var prefs=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"BogActualForecastCaption"});var isolation=new GameObject("Explicit forecast caption fixture");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));Component match=null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckBogSnorkelling/Scenes/TruckBogSnorkelling.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession"));
            var weather=Get(Get(match,"course"),"weather");var catalog=Get(weather,"presets");var entries=((IEnumerable)Get(catalog,"entries")).Cast<object>().ToArray();
            foreach(string kind in new[]{"Clear","Rain","Snow"})
            {
                var preset=entries.First(e=>Get(e,"legacyKind").ToString()==kind&&Get(e,"profile")!=null&&!(bool)Get(Get(e,"profile"),"Disabled"));
                Call(match,"SetupScreen");Set(match,"weather",kind=="Clear"?1:0);Call(match,"AllBots");Call(catalog,"Select",Get(preset,"id"));Assert.That(Call(match,"StartMatch"),Is.True);yield return null;yield return null;((Behaviour)match).enabled=false;Time.timeScale=0;
                string actual=((string)Get(preset,"displayName")).ToUpperInvariant();Assert.That(Get(weather,"Description"),Is.EqualTo(actual));Assert.That((string)Get(match,"Header"),Does.EndWith(" | "+actual));
                yield return new WaitForSecondsRealtime(.2f);Assert.That(Get(Get(Get(match,"menus"),"header"),"text"),Is.EqualTo(Get(match,"Header")));Capture(match,"forecast-"+kind);
                ((Behaviour)match).enabled=true;Time.timeScale=1;
            }
            TestContext.WriteLine("Actual installed Clear/Rain/Snow profile selection and saved header binding; legacy choice intentionally differs. Caption-only preview, no weather-art, complete race, physical-controller or human acceptance.");
        }
        finally{if(match!=null){((Behaviour)match).enabled=true;Call(match,"SetupScreen");}Time.timeScale=1;UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();}
    }
}
#endif
