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

public sealed class MatchColorsLifecycleTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
    static object Registry(string name,params object[] args)=>Runtime("SportsParticipantRegistry").GetMethod(name).Invoke(null,args);
    static object Prefs(string name,params object[] args)=>Runtime("SportsPrefs").GetMethod(name).Invoke(null,args);
    static Color[] Colors(Array slots,int count)=>slots.Cast<object>().Take(count).Select(slot=>(Color)Get(slot,"color")).ToArray();
    static void EqualColor(Color actual,Color expected){Assert.That(Mathf.Abs(actual.r-expected.r)+Mathf.Abs(actual.g-expected.g)+Mathf.Abs(actual.b-expected.b)+Mathf.Abs(actual.a-expected.a),Is.LessThan(.0001f));}
    static Component[] Trucks(Component match,bool sumo)=>sumo?((Array)Get(match,"fighters")).Cast<object>().Select(fighter=>(Component)Get(fighter,"truck")).ToArray():((Array)Get(match,"trucks")).Cast<Component>().ToArray();
    static void ActualPaint(Component truck,Color color)
    {
        var block=new MaterialPropertyBlock();var host=Get(truck,"appearanceHost");
        if(host!=null&&(bool)Get(host,"Applied"))
        {
            var renderers=(Renderer[])Get(host,"paintRenderers");var slots=(Array)Get(host,"paintSlots");Assert.That(slots.Length,Is.GreaterThan(0));foreach(var slot in slots){var renderer=renderers[(int)Get(slot,"rendererIndex")];Assert.That(renderer.enabled&&renderer.gameObject.activeInHierarchy,Is.True);renderer.GetPropertyBlock(block,(int)Get(slot,"materialIndex"));EqualColor(block.GetColor("_BaseColor"),color);}
        }
        else{var renderer=(Renderer)Get(truck,"paint");Assert.That(renderer.enabled&&renderer.gameObject.activeInHierarchy,Is.True);renderer.GetPropertyBlock(block);EqualColor(block.GetColor("_BaseColor"),color);}
    }
    static void CaptureFleet(Component match,Component[] trucks,string label)
    {
        var original=((Camera[])Get(match,"cameras")).First(camera=>camera!=null&&camera.enabled);var go=new GameObject("Controlled actual match-color fleet camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(original);camera.enabled=true;var bounds=new Bounds(trucks[0].transform.position,Vector3.one);foreach(var truck in trucks)foreach(var renderer in truck.GetComponentsInChildren<Renderer>().Where(renderer=>renderer.enabled))bounds.Encapsulate(renderer.bounds);var offset=new Vector3(20,30,-35).normalized*Mathf.Max(35,bounds.size.magnitude*1.5f);camera.transform.SetPositionAndRotation(bounds.center+offset,Quaternion.LookRotation(-offset,Vector3.up));camera.orthographic=true;camera.orthographicSize=Mathf.Max(12,bounds.extents.magnitude*1.2f);camera.rect=new Rect(0,0,1,1);
        try{var save=Runtime("SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==6);foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})save.Invoke(null,new object[]{((Component)Get(match,"menus")).GetComponent<Canvas>(),new[]{camera},size.x,size.y,"Logs/TruckSports/PlaytestRepair/MatchColors/"+label+"-"+size.y+".png",null});}finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    [UnityTest,Timeout(180000)]public IEnumerator ActualSavedSumoFourKeepsDistinctTruckAndHudColorsWithoutSavingTemporaryPreferences()=>Inspect("TruckSumo",4);
    [UnityTest,Timeout(180000)]public IEnumerator ActualSavedAvalancheTwelveKeepsDistinctPhysicalFleetAndPreferenceBytes()=>Inspect("TruckAvalancheEscape",12);
    static IEnumerator Inspect(string scene,int count)
    {
        string production=PlayerPrefs.GetString("TruckSports.Participants.v1","");var prefs=(IDisposable)Prefs("IsolateValidation","MatchColorNative"+Guid.NewGuid().ToString("N"));Registry("ForgetCache");var isolation=new GameObject("Explicit original native FFA fleet and preference scope");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));Component match=null;string setupKey=null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/"+scene+"/Scenes/"+scene+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession"));Set(Get(match,"setup"),"count",count);Call(match,"AllBots");for(int seat=0;seat<12;seat++){var profile=Registry("Default",seat);Registry("Rename",Get(profile,"id"),"Same Driver",new Color(.04f+seat*.0005f,.28f,.92f),true);}Call(match,"RefreshParticipants",false);Call(match,"Save");var slots=(Array)Get(Get(match,"setup"),"players");var preferred=Colors(slots,count);setupKey=(string)Get(match,"PreferenceKey");string profileBytes=(string)Prefs("GetString","TruckSports.Participants.v1",""),backupBytes=(string)Prefs("GetString","TruckSports.Participants.v1.Backup",""),setupBytes=(string)Prefs("GetString",setupKey,"");Assert.That(Call(match,"StartMatch"),Is.True);var assignment=Get(match,"MatchColors");Assert.That(assignment,Is.Not.Null);var effective=Colors(slots,count);for(int i=0;i<count;i++)for(int j=i+1;j<count;j++)Assert.That((float)Runtime("SportsMatchColors").GetMethod("Distance").Invoke(null,new object[]{effective[i],effective[j]}),Is.GreaterThanOrEqualTo(22-.001f));
            var trucks=Trucks(match,scene=="TruckSumo").Take(count).ToArray();var starting=trucks.Select(truck=>((Rigidbody)Get(truck,"body")).position).ToArray();float stop=Time.time+2;while(Time.time<stop)yield return new WaitForFixedUpdate();Time.timeScale=0;
            Assert.That(trucks.Length,Is.EqualTo(count));for(int i=0;i<count;i++){Assert.That(trucks[i].gameObject.activeInHierarchy,Is.True);Assert.That(((Rigidbody)Get(trucks[i],"body")).isKinematic,Is.False);ActualPaint(trucks[i],effective[i]);EqualColor((Color)Get(slots.GetValue(i),"color"),effective[i]);}
            var scoreView=((Component)Get(match,"menus")).GetComponent(Runtime("SportsBroadcastScores"));Call(scoreView,"LateUpdate");var cards=(Array)Get(scoreView,"cards");for(int i=0;i<count;i++)EqualColor(((UnityEngine.UI.Image)Get(cards.GetValue(i),"stripe")).color,effective[i]);CaptureFleet(match,trucks,scene+"-actual-distinct-fleet");
            Call(match,"Pause",0,false);Call(match,"Save");Call(match,"RefreshParticipants",true);Assert.That(Colors(slots,count),Is.EqualTo(effective));Call(match,"Resume");Time.timeScale=0;
            Assert.Throws<InvalidOperationException>(()=>{using((IDisposable)Call(assignment,"Suspend")){Assert.That(Colors(slots,count),Is.EqualTo(preferred));Call(match,"Save");using((IDisposable)Call(assignment,"Suspend"))Call(match,"RefreshParticipants",false);throw new InvalidOperationException("explicit suspended-save exception");}});Assert.That(Colors(slots,count),Is.EqualTo(effective));
            Assert.That(Prefs("GetString","TruckSports.Participants.v1",""),Is.EqualTo(profileBytes));Assert.That(Prefs("GetString","TruckSports.Participants.v1.Backup",""),Is.EqualTo(backupBytes));Assert.That(Prefs("GetString",setupKey,""),Is.EqualTo(setupBytes));Registry("ForgetCache");Call(match,"RefreshParticipants",false);Assert.That(Colors(slots,count),Is.EqualTo(effective));Assert.That(Prefs("GetString","TruckSports.Participants.v1",""),Is.EqualTo(profileBytes));
            string id=(string)Get(match,"MatchId");Call(match,"SetupScreen");Assert.That(Colors(slots,count),Is.EqualTo(preferred));Assert.That(Get(match,"MatchColors"),Is.Null);Assert.That(Call(match,"StartMatch"),Is.True);Time.timeScale=0;Assert.That(Get(match,"MatchId"),Is.Not.EqualTo(id));Assert.That(Colors(slots,count),Is.EqualTo(effective));for(int i=0;i<count;i++)ActualPaint(trucks[i],effective[i]);
            // Real scene exit exercises the base OnDestroy boundary; managed
            // participant slot references let us inspect restoration afterwards.
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSports/Scenes/TruckSports.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;match=null;Assert.That(Colors(slots,count),Is.EqualTo(preferred));Assert.That(Prefs("GetString","TruckSports.Participants.v1",""),Is.EqualTo(profileBytes));
            TestContext.WriteLine("ACTUAL_NATIVE_MATCH_COLORS scene="+scene+" competitors="+count+"; original saved physical tractors/renderers/property-block paint and shared HUD stripes, near-duplicate preferred colors/duplicate names, stable effective match values, actual source Save/Refresh/pause/rematch/scene exit, nested suspend+exception, profile/backup/setup preference byte equivalence and registry reload. Sumo remains its original4-player mode;12fleet is Avalanche. Controlled fleet renders are not player-window/human/color-vision approval.");
        }
        finally{Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");if(setupKey!=null)Prefs("DeleteKey",setupKey);foreach(string key in new[]{"TruckSports.Participants.v1","TruckSports.Participants.v1.Backup"})Prefs("DeleteKey",key);Prefs("Save");Registry("ForgetCache");UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();Assert.That(PlayerPrefs.GetString("TruckSports.Participants.v1",""),Is.EqualTo(production));}
    }

    [UnityTest,Timeout(120000)]
    public IEnumerator TeamSportBypassesAssignmentAndKeepsIntentionalDuplicatePreferences()
    {
        var prefs=(IDisposable)Prefs("IsolateValidation","MatchColorTeam"+Guid.NewGuid().ToString("N"));Registry("ForgetCache");var isolation=new GameObject("Explicit native team-color bypass");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));Component match=null;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckVolleyball/Scenes/TruckVolleyball.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession"));Set(match,"teamA",2);Set(match,"teamB",2);Call(match,"AllBots");for(int i=0;i<12;i++){var profile=Registry("Default",i);Registry("Rename",Get(profile,"id"),"Same Team Driver",Color.blue,true);}Call(match,"RefreshParticipants",false);Call(match,"Save");string before=(string)Prefs("GetString","TruckSports.Participants.v1","");Assert.That(Call(match,"StartMatch"),Is.True);Time.timeScale=0;Assert.That(Get(match,"TeamSport"),Is.True);Assert.That(Get(match,"MatchColors"),Is.Null);var slots=(Array)Get(Get(match,"setup"),"players");Assert.That(Colors(slots,4),Is.All.EqualTo(Color.blue));Call(match,"Save");Call(match,"RefreshParticipants",true);Assert.That(Colors(slots,4),Is.All.EqualTo(Color.blue));Assert.That(Prefs("GetString","TruckSports.Participants.v1",""),Is.EqualTo(before));Call(match,"SetupScreen");Assert.That(Colors(slots,4),Is.All.EqualTo(Color.blue));TestContext.WriteLine("NATIVE_TEAM_BYPASS saved Volley team session never creates FFA assignment; intentional duplicate player preferences and source Save/Refresh stay unchanged. No team gameplay/human claim.");
        }
        finally{Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");Registry("ForgetCache");UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();}
    }
}
#endif
