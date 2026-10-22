#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class SnakePresentationTests
{
    static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    static object Property(object value,string name)=>value.GetType().GetProperty(name).GetValue(value);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name).Invoke(value,args);
    static string Text(Transform root)=> (string)Property(root.GetComponent(Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro",true)),"text");
    static Rect Bounds(RectTransform rect){var c=new Vector3[4];rect.GetWorldCorners(c);var a=RectTransformUtility.WorldToScreenPoint(null,c[0]);var b=RectTransformUtility.WorldToScreenPoint(null,c[2]);return Rect.MinMaxRect(a.x,a.y,b.x,b.y);}

    [UnityTest]
    public IEnumerator SavedSnakeHudAndCnpObjectivesKeepActualViewportOwnersAndReset()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"SnakePresentation"}))
        {
            Component match=null;var pads=new Gamepad[4];GameObject isolation=null;var background=InputSystem.settings.backgroundBehavior;
            try
            {
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                isolation=new GameObject("Snake presentation software-device isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Type.GetType("LWS.TruckSports.SportsProbeInputIsolation, Assembly-CSharp",true));
                for(int i=0;i<4;i++){pads[i]=InputSystem.AddDevice<Gamepad>();InputSystem.EnableDevice(pads[i]);}
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSnake/Scenes/TruckSnake.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckSnake.SnakeMatch, Assembly-CSharp",true),FindObjectsSortMode.None).Single();
                var guide=match.GetComponent(Type.GetType("LWS.TruckSnake.SnakePickupGuidance, Assembly-CSharp",true));Assert.That(guide,Is.Not.Null,"The actual saved scene must bind CNP references.");var driving=Field(match,"drivingViews");var menus=Field(match,"menus");
                var cameraOptions=Type.GetType("LWS.TruckSports.SportsCameraOptions, Assembly-CSharp",true);
                for(int humans=1;humans<=4;humans++)
                {
                    Call(match,"SetupScreen");Call(match,"AllBots");cameraOptions.GetMethod("Set").Invoke(null,new object[]{false});var players=(Array)Field(Field(match,"setup"),"players");
                    for(int i=0;i<humans;i++){var player=players.GetValue(i);var source=player.GetType().GetField("source");source.SetValue(player,Enum.Parse(source.FieldType,"WholeController"));player.GetType().GetField("pad").SetValue(player,pads[i]);}
                    Call(driving,"Select",1);Assert.That((bool)Call(match,"StartMatch"),Is.True);Time.timeScale=3;yield return new WaitForSecondsRealtime(1.2f);Time.timeScale=0;yield return new WaitForSecondsRealtime(.3f);Canvas.ForceUpdateCanvases();
                    var cameras=(Camera[])Field(match,"cameras");var panels=(Array)Field(menus,"viewPanels");var compasses=(Array)Field(guide,"compasses");var targets=(Array)Field(guide,"targets");Assert.That(cameras.Length,Is.EqualTo(humans));
                    for(int view=0;view<humans;view++)
                    {
                        var panel=(Transform)panels.GetValue(view);var card=(RectTransform)panel.Find("Compact owned viewport score");Assert.That(card.gameObject.activeInHierarchy,Is.True);
                        var bounds=Bounds(card);Assert.That(bounds.xMin,Is.GreaterThanOrEqualTo(cameras[view].pixelRect.xMin-1));Assert.That(bounds.xMax,Is.LessThanOrEqualTo(cameras[view].pixelRect.xMax+1));Assert.That(bounds.yMin,Is.GreaterThanOrEqualTo(cameras[view].pixelRect.yMin-1));Assert.That(bounds.yMax,Is.LessThanOrEqualTo(cameras[view].pixelRect.yMax+1));
                        Assert.That(Text(card.Find("Participant name")),Is.EqualTo(Field(players.GetValue(view),"name")));Assert.That(Text(card.Find("Race status")),Does.Contain("TRAILER"));Assert.That(Text(card.Find("Race status")),Does.Contain("MPH"));Assert.That(Text(card.Find("Driving telemetry")),Does.Contain("BEST"));Assert.That(((Component)((Array)Field(menus,"viewTexts")).GetValue(view)).gameObject.activeSelf,Is.False);
                        foreach(string label in new[]{"Race status","Driving telemetry"})Assert.That((bool)Property(card.Find(label).GetComponent(Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro",true)),"isTextTruncated"),Is.False,"Complete Snake telemetry must fit its real TMP rectangle: "+label);
                        var objective=(Component)Call(guide,"Objective",view);Assert.That(objective,Is.Not.Null);var compass=(Component)compasses.GetValue(view);var target=(Component)targets.GetValue(view);Assert.That(Property(compass,"cameraMain"),Is.SameAs(cameras[view]));Assert.That(Property(compass,"viewportRect"),Is.EqualTo(cameras[view].rect));Assert.That(Vector3.Distance(target.transform.position,objective.transform.position+Vector3.up*2),Is.LessThan(.01f));
                    }
                    Call(match,"SetupScreen");yield return null;foreach(Component compass in compasses)Assert.That(compass.gameObject.activeSelf,Is.False);
                }
                Call(match,"AllBots");Call(driving,"Select",0);Assert.That((bool)Call(match,"StartMatch"),Is.True);Time.timeScale=3;yield return new WaitForSecondsRealtime(1.2f);Time.timeScale=0;yield return new WaitForSecondsRealtime(.3f);
                Assert.That(((Camera[])Field(match,"cameras")).Single().enabled,Is.True);Assert.That(Call(guide,"Objective",0),Is.Not.Null,"AI-only shared spectator also receives a reachable objective.");
                Assert.That((int)Property(guide,"RouteQueries"),Is.LessThan(45),"Road searches must be throttled, never performed every rendered frame.");Debug.Log("SNAKE_OBJECTIVE maximum query group milliseconds="+Property(guide,"MaxQueryMilliseconds"));
            }
            finally{Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");foreach(var pad in pads)if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);if(isolation!=null)UnityEngine.Object.Destroy(isolation);InputSystem.settings.backgroundBehavior=background;}
        }
    }
}
#endif
