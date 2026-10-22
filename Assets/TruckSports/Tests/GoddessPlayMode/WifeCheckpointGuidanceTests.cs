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

public sealed class WifeCheckpointGuidanceTests
{
    static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    static object Property(object value,string name)=>value.GetType().GetProperty(name).GetValue(value);
    static object Call(object value,string name,params object[] arguments)=>value.GetType().GetMethod(name).Invoke(value,arguments);
    [UnityTest]
    public IEnumerator FourOwnedVendorMarkersFollowActualOrderedCheckpointsAndClearOnRematch()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"WifeCheckpointGuidance"}))
        {
            Component match=null;var pads=new Gamepad[4];var background=InputSystem.settings.backgroundBehavior;GameObject isolation=null;
            try
            {
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;isolation=new GameObject("Wife checkpoint physical-device isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Type.GetType("LWS.TruckSports.SportsProbeInputIsolation, Assembly-CSharp",true));
                for(int i=0;i<4;i++){pads[i]=InputSystem.AddDevice<Gamepad>();InputSystem.EnableDevice(pads[i]);}
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckWifeCarrying/Scenes/TruckWifeCarrying.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckWifeCarrying.WifeMatch, Assembly-CSharp",true),FindObjectsSortMode.None).Single();Call(match,"AllBots");var players=(Array)Field(Field(match,"setup"),"players");
                for(int i=0;i<4;i++){var player=players.GetValue(i);var source=player.GetType().GetField("source");source.SetValue(player,Enum.Parse(source.FieldType,"WholeController"));player.GetType().GetField("pad").SetValue(player,pads[i]);}
                Type.GetType("LWS.TruckSports.SportsCameraOptions, Assembly-CSharp",true).GetMethod("Set").Invoke(null,new object[]{false});
                var guide=match.GetComponent(Type.GetType("LWS.TruckWifeCarrying.WifeCheckpointGuidance, Assembly-CSharp",true));Assert.That(guide,Is.Not.Null,"The saved scene must bind guidance; a script alone is not integration.");
                var compasses=(Array)Field(guide,"compasses");var targets=(Array)Field(guide,"targets");Assert.That(compasses.Length,Is.EqualTo(4));Assert.That(targets.Length,Is.EqualTo(4));
                Assert.That((bool)Call(match,"StartMatch"),Is.True);Time.timeScale=3;float until=Time.realtimeSinceStartup+5;while(Property(match,"Phase").ToString()!="Race"&&Time.realtimeSinceStartup<until)yield return null;Assert.That(Property(match,"Phase").ToString(),Is.EqualTo("Race"));Time.timeScale=0;yield return new WaitForSecondsRealtime(.7f);
                var cameras=(Camera[])Field(match,"cameras");var course=Field(match,"course");var pairs=(Array)Field(match,"pairs");var gates=(float[])Field(course,"checkpoints");
                for(int view=0;view<4;view++)
                {
                    var compass=(Component)compasses.GetValue(view);var target=(Component)targets.GetValue(view);Assert.That(compass.gameObject.activeInHierarchy&&target.gameObject.activeInHierarchy,Is.True);
                    Assert.That(Property(compass,"cameraMain"),Is.SameAs(cameras[view]));Assert.That((Rect)Property(compass,"viewportRect"),Is.EqualTo(cameras[view].rect));Assert.That(((Array)Field(compass,"permittedTargets")).GetValue(0),Is.SameAs(target));
                    float gate=gates[(int)Property(pairs.GetValue(view),"Checkpoint")];var expected=((bool)Field(course,"loopCircuit")?(Vector3)Call(course,"Point",gate,0f):(Vector3)Call(course,"Surface",new Vector3((float)Call(course,"Center",gate),0,gate)))+Vector3.up*2;Assert.That(Vector3.Distance(target.transform.position,expected),Is.LessThan(.05f));
                    var state=Call(target,"GetState",4);var widget=(RectTransform)Field(state,"indicatorRT");Assert.That(widget,Is.Not.Null);Assert.That(widget.gameObject.activeInHierarchy,Is.True);var point=RectTransformUtility.WorldToScreenPoint(null,widget.position);Assert.That(point.x,Is.InRange(cameras[view].pixelRect.xMin,cameras[view].pixelRect.xMax));Assert.That(point.y,Is.InRange(cameras[view].pixelRect.yMin,cameras[view].pixelRect.yMax));
                }
                Call(match,"SetupScreen");yield return null;foreach(Component compass in compasses)Assert.That(compass.gameObject.activeSelf,Is.False);foreach(Component target in targets)Assert.That(target.gameObject.activeSelf,Is.False);
                Assert.That((bool)Call(match,"StartMatch"),Is.True);Assert.That(((Array)Field(guide,"compasses")).Cast<Component>().Select(c=>c.GetInstanceID()),Is.EqualTo(compasses.Cast<Component>().Select(c=>c.GetInstanceID())),"Rematch reuses four saved vendor references.");
            }
            finally{Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");foreach(var pad in pads)if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);if(isolation!=null)UnityEngine.Object.Destroy(isolation);InputSystem.settings.backgroundBehavior=background;}
        }
    }
}
#endif
