#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class BroadcastPausedObjectiveTests
{
    static object Field(object target,string name)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(target);
    static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name).Invoke(target,args);
    [UnityTest]
    public IEnumerator NewlyEnabledOffscreenObjectiveIsActuallyRenderedWithTheSimulationFrozen()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"PausedBroadcastObjective"}))
        {
            Component match=null;Gamepad pad=null;GameObject isolation=null;var background=InputSystem.settings.backgroundBehavior;
            try
            {
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;isolation=new GameObject("Paused objective physical-input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Type.GetType("LWS.TruckSports.SportsProbeInputIsolation, Assembly-CSharp",true));
                pad=InputSystem.AddDevice<Gamepad>();InputSystem.EnableDevice(pad);
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckKeepaway/Scenes/TruckKeepaway.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckKeepaway.KeepawayMatch, Assembly-CSharp",true),FindObjectsSortMode.None).Single();
                Call(match,"AllBots");var player=((Array)Field(Field(match,"setup"),"players")).GetValue(0);var source=player.GetType().GetField("source");source.SetValue(player,Enum.Parse(source.FieldType,"WholeController"));player.GetType().GetField("pad").SetValue(player,pad);
                Assert.That((bool)Call(match,"StartMatch"),Is.True);Time.timeScale=0;
                var framing=(Behaviour)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckSports.SportsBroadcastFraming, Assembly-CSharp",true),FindObjectsSortMode.None).Single();
                yield return null;framing.enabled=false;yield return null;framing.enabled=true;yield return new WaitForSecondsRealtime(.6f);
                Assert.That((bool)framing.GetType().GetProperty("ObjectiveVisible").GetValue(framing),Is.False,"Expanded far-start fixture must exercise the offscreen indicator.");
                var guide=framing.GetComponent(Type.GetType("LWS.TruckSports.SportsBroadcastGuidance, Assembly-CSharp",true));var compass=(Behaviour)Field(guide,"compass");
                var marker=((Array)Field(guide,"targets")).GetValue(0);Assert.That(((Component)marker).gameObject.activeInHierarchy&&compass.isActiveAndEnabled,Is.True);
                var state=Call(marker,"GetState",4);var widget=(RectTransform)Field(state,"indicatorRT");var opacity=(CanvasGroup)Field(state,"indicatorCanvasGroup");
                Assert.That(widget,Is.Not.Null);Assert.That(widget.gameObject.activeInHierarchy,Is.True);Assert.That(opacity.alpha,Is.GreaterThan(.1f));
                Assert.That(widget.localScale.x,Is.GreaterThan(.1f),"Registered POI is insufficient: the actual vendor widget must animate visibly even at Time.timeScale=0.");
            }
            finally{Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);if(isolation!=null)UnityEngine.Object.Destroy(isolation);InputSystem.settings.backgroundBehavior=background;}
        }
    }
}
#endif
