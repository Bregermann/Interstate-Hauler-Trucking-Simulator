#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;

public sealed class HockeyPresentationTests
{
    static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    static object Property(object value,string name)=>value.GetType().GetProperty(name).GetValue(value);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name).Invoke(value,args);
    static string Text(object value)=>(string)Property(value,"text");
    [UnityTest]
    public IEnumerator MixedHumanAndAIHUDsRetainSoftwareControllerOwnersFromOneThroughSixHumans()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"HockeyPresentationMixed"}))
        {
            Component match=null;var pads=new Gamepad[6];GameObject isolation=null;var background=InputSystem.settings.backgroundBehavior;
            try
            {
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                isolation=new GameObject("Hockey mixed software-device isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Type.GetType("LWS.TruckSports.SportsProbeInputIsolation, Assembly-CSharp",true));
                for(int i=0;i<6;i++){pads[i]=InputSystem.AddDevice<Gamepad>();InputSystem.EnableDevice(pads[i]);}
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHockey/Scenes/TruckHockey.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckHockey.HockeyMatch, Assembly-CSharp",true),FindObjectsSortMode.None).Single();
                var mode=match.GetType().GetField("mode");mode.SetValue(match,Enum.Parse(mode.FieldType,"Custom"));var players=(Array)Field(match,"players");
                foreach(int count in new[]{1,2,4,6})
                {
                    for(int i=0;i<6;i++){var player=players.GetValue(i);var source=player.GetType().GetField("source");source.SetValue(player,Enum.Parse(source.FieldType,i<count?"WholeController":"AI"));if(i<count)Assert.That((bool)Call(match,"Assign",i,pads[i]),Is.True);}
                    Call(match,"Configure");Assert.That((bool)Call(match,"StartMatch",false),Is.True);yield return null;var ui=Field(Field(match,"menus"),"presentation");Call(ui,"Refresh");var labels=(Array)Field(ui,"participantSources");
                    for(int i=0;i<6;i++){Assert.That(Text(labels.GetValue(i)),Does.Contain(i<count?"GAMEPAD":"AI"));if(i<count)Assert.That(Field(players.GetValue(i),"pad"),Is.SameAs(pads[i]));}
                    Call(match,"Pause",false);Call(match,"Resume");Call(match,"ChangeSetup");yield return null;
                }
            }
            finally{Time.timeScale=1;if(match!=null)Call(match,"ChangeSetup");foreach(var pad in pads)if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);InputSystem.settings.backgroundBehavior=background;if(isolation!=null)UnityEngine.Object.Destroy(isolation);}
        }
    }
    [UnityTest]
    public IEnumerator SavedHockeyPresentationPreservesSixOwnersAndAuthoritativeScoresAcrossRematch()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"HockeyPresentationNative"}))
        {
            Component match=null;GameObject isolation=null;
            try
            {
                isolation=new GameObject("Hockey UI device isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Type.GetType("LWS.TruckSports.SportsProbeInputIsolation, Assembly-CSharp",true));
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHockey/Scenes/TruckHockey.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckHockey.HockeyMatch, Assembly-CSharp",true),FindObjectsSortMode.None).Single();var menus=Field(match,"menus");var ui=(Component)Field(menus,"presentation");Assert.That(ui,Is.Not.Null,"Actual saved Hockey scene must contain the integrated UI.");
                var mode=match.GetType().GetField("mode");mode.SetValue(match,Enum.Parse(mode.FieldType,"AIVsAI"));Call(match,"Configure");var players=(Array)Field(match,"players");var colors=players.Cast<object>().Select(p=>(Color)Field(p,"color")).ToArray();var names=(Array)Field(ui,"participantNames");var sources=(Array)Field(ui,"participantSources");
                foreach(string label in new[]{"teamText","orangeTeamText","controlsText"})Assert.That(((Component)Field(menus,label)).transform.parent.GetComponent(Type.GetType("UnityEngine.UI.Image, UnityEngine.UI",true)),Is.Not.Null,"Small contrast plates must keep labels readable against the bright ice.");
                for(int run=0;run<2;run++)
                {
                    Assert.That((bool)Call(match,"StartMatch",run>0),Is.True);yield return null;Call(ui,"Refresh");
                    var overlay=((GameObject)Field(menus,"hud")).GetComponent(Type.GetType("LWS.TruckSports.SportsArenaOverlay, Assembly-CSharp",true));Assert.That(((Behaviour)overlay).enabled,Is.False,"Old overlay must not resurrect empty docks.");
                    foreach(var dock in (Array)Field(overlay,"docks"))Assert.That(((RectTransform)Field(dock,"plate")).gameObject.activeSelf,Is.False);
                    for(int i=0;i<6;i++){Assert.That(Text(names.GetValue(i)),Is.EqualTo(Field(players.GetValue(i),"name")));Assert.That((Color)Property(names.GetValue(i),"color"),Is.EqualTo(colors[i]));Assert.That(Text(sources.GetValue(i)),Does.StartWith((i<3?"B":"O")+(i%3+1)));}
                    Assert.That(Text(Field(menus,"scoreText")),Is.EqualTo("0   :   0"));Assert.That(Text(Field(menus,"teamText")),Does.Contain(run==0?"LEFT":"RIGHT"));
                    Call(match,"Pause",false);yield return null;Assert.That(Text(Field(ui,"pauseSummary")),Does.Contain("03:00"));Call(match,"Resume");Call(match,"ChangeSetup");yield return null;
                    Assert.That(players.Cast<object>().Select(p=>(Color)Field(p,"color")).ToArray(),Is.EqualTo(colors),"Team presentation must not overwrite Your Crew colors.");
                }
                Assert.That(((Array)Field(ui,"portraits")).Cast<Component>().All(i=>Property(i,"sprite")!=null),Is.True);
            }
            finally{Time.timeScale=1;if(match!=null)Call(match,"ChangeSetup");if(isolation!=null)UnityEngine.Object.Destroy(isolation);}
        }
    }
}
#endif
