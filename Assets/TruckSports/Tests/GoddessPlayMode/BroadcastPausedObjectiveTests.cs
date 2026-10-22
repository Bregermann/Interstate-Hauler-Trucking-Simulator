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
    static object Property(object target,string name)=>target.GetType().GetProperty(name).GetValue(target);
    [UnityTest]
    public IEnumerator AllDistantLocalHumansHaveActualVisibleVendorWidgets()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"DistantBroadcastHumans"}))
        {
            Component match=null;var pads=new Gamepad[4];GameObject isolation=null;var background=InputSystem.settings.backgroundBehavior;
            try
            {
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;isolation=new GameObject("Distant human cue input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Type.GetType("LWS.TruckSports.SportsProbeInputIsolation, Assembly-CSharp",true));
                for(int i=0;i<4;i++){pads[i]=InputSystem.AddDevice<Gamepad>();InputSystem.EnableDevice(pads[i]);}
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckMagnetMayhem/Scenes/TruckMagnetMayhem.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckMagnetMayhem.MagnetMatch, Assembly-CSharp",true),FindObjectsSortMode.None).Single();
                var setup=Field(match,"setup");setup.GetType().GetField("count").SetValue(setup,8);Call(match,"AllBots");var players=(Array)Field(setup,"players");
                for(int i=0;i<4;i++){var player=players.GetValue(i);var source=player.GetType().GetField("source");source.SetValue(player,Enum.Parse(source.FieldType,"WholeController"));player.GetType().GetField("pad").SetValue(player,pads[i]);}
                var driving=Field(match,"drivingViews");var options=Type.GetType("LWS.TruckSports.SportsCameraOptions, Assembly-CSharp",true);
                // Reproduce the preceding player-build acceptance sequence too:
                // legacy comparison, distant AI teleport and target retirement.
                for(int i=1;i<4;i++){var player=players.GetValue(i);var source=player.GetType().GetField("source");source.SetValue(player,Enum.Parse(source.FieldType,"AI"));player.GetType().GetField("pad").SetValue(player,null);}
                options.GetMethod("Set").Invoke(null,new object[]{false});Call(driving,"Select",0);Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return new WaitForSecondsRealtime(4);Time.timeScale=0;
                var beforeFraming=(Behaviour)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckSports.SportsBroadcastFraming, Assembly-CSharp",true),FindObjectsSortMode.None).Single();var beforeCamera=(Camera)Field(Field(beforeFraming,"cameraRig"),"view");beforeCamera.aspect=16/9f;
                beforeFraming.enabled=false;yield return new WaitForSecondsRealtime(.7f);beforeFraming.enabled=true;yield return new WaitForSecondsRealtime(2);
                int beforePrimary=(int)Property(beforeFraming,"Primary");int far=beforePrimary==0?1:0;var farBody=(Rigidbody)Call(beforeFraming,"Body",far);var original=farBody.position;farBody.position=original+new Vector3(1500,0,1500);farBody.transform.position=farBody.position;Physics.SyncTransforms();yield return new WaitForSecondsRealtime(1.5f);farBody.position=original;farBody.transform.position=original;Physics.SyncTransforms();
                var retired=(Rigidbody)Call(beforeFraming,"Body",beforePrimary);retired.gameObject.SetActive(false);yield return new WaitForSecondsRealtime(.25f);retired.gameObject.SetActive(true);Time.timeScale=1;
                for(int humans=1;humans<=4;humans++){
                    Call(match,"SetupScreen");for(int i=0;i<4;i++){var player=players.GetValue(i);var source=player.GetType().GetField("source");source.SetValue(player,Enum.Parse(source.FieldType,i<humans?"WholeController":"AI"));player.GetType().GetField("pad").SetValue(player,i<humans?pads[i]:null);}
                    options.GetMethod("Set").Invoke(null,new object[]{false});Call(driving,"Select",1);Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return new WaitForSecondsRealtime(.3f);options.GetMethod("Set").Invoke(null,new object[]{true});Call(driving,"Layout");yield return new WaitForSecondsRealtime(.15f);options.GetMethod("Set").Invoke(null,new object[]{false});Call(driving,"Layout");
                }
                Call(match,"SetupScreen");Call(driving,"Select",0);Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return new WaitForSecondsRealtime(.3f);Time.timeScale=0;
                var framing=(Behaviour)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckSports.SportsBroadcastFraming, Assembly-CSharp",true),FindObjectsSortMode.None).Single();
                int lead=(int)framing.GetType().GetProperty("Primary").GetValue(framing);Assert.That(lead,Is.InRange(0,3));
                var camera=(Camera)Field(Field(framing,"cameraRig"),"view");var origin=((Rigidbody)Call(framing,"Body",lead)).position;var behind=Vector3.ProjectOnPlane(-camera.transform.forward,Vector3.up).normalized;var right=Vector3.ProjectOnPlane(camera.transform.right,Vector3.up).normalized;
                int remote=0;for(int i=0;i<4;i++)if(i!=lead){var body=(Rigidbody)Call(framing,"Body",i);var at=origin+behind*200+right*((remote++-1)*150);body.position=at;body.transform.position=at;}Physics.SyncTransforms();yield return new WaitForSecondsRealtime(1.8f);
                int primary=(int)framing.GetType().GetProperty("Primary").GetValue(framing);var guide=framing.GetComponent(Type.GetType("LWS.TruckSports.SportsBroadcastGuidance, Assembly-CSharp",true));var compass=Field(guide,"compass");
                TestContext.WriteLine("Repeated-view-switch frame owns="+framing.GetType().GetProperty("OwnsShot").GetValue(framing)+" primary="+primary+" bounds="+Call(framing,"TruckBounds",primary)+" projected="+Call(framing,"ProjectedTruckRect",camera,primary)+" camera="+camera.transform.position);
                var truck=((Array)Field(match,"trucks")).GetValue(primary);foreach(var renderer in ((Transform)Field(truck,"visuals")).GetComponentsInChildren<Renderer>().OrderByDescending(r=>r.bounds.size.magnitude).Take(4))TestContext.WriteLine("RENDERER "+renderer.name+" bounds="+renderer.bounds);
                var actualBounds=(Bounds)Call(framing,"TruckBounds",primary);Assert.That(actualBounds.size.magnitude,Is.LessThan(20),"Invisible zero-scale wreck meshes at old positions must not turn the living truck into a giant framing subject.");Assert.That(Vector3.Distance(actualBounds.center,((Component)truck).transform.position),Is.LessThan(10));
                foreach(var renderer in ((Transform)Field(truck,"visuals")).GetComponentsInChildren<Renderer>().Where(r=>(r is MeshRenderer||r is SkinnedMeshRenderer)&&r.enabled&&r.gameObject.activeInHierarchy).OrderByDescending(r=>Vector3.Distance(r.bounds.center,((Component)truck).transform.position)).Take(6))TestContext.WriteLine("DISTANT MESH "+renderer.name+" scale="+renderer.transform.lossyScale+" bounds="+renderer.bounds);
                Assert.That(Field(compass,"permittedTargets"),Is.SameAs(Field(guide,"targets")),"The saved seven-target guide must not be filtered by a stale three-target prefab override.");
                var scoreboard=((GameObject)Field(Field(match,"menus"),"hud")).transform.Find("Broadcast participant cards") as RectTransform;Assert.That(scoreboard,Is.Not.Null);var boardCorners=new Vector3[4];scoreboard.GetWorldCorners(boardCorners);float boardTop=RectTransformUtility.WorldToScreenPoint(null,boardCorners[2]).y;
                bool all=true;
                for(int i=0;i<4;i++)if(i!=primary)
                {
                    var position=((Rigidbody)Call(framing,"Body",i)).position;var marker=((Array)Field(guide,"targets")).Cast<Component>().Single(p=>p.gameObject.activeInHierarchy&&Vector3.Distance(p.transform.position,position)<.1f);
                    var viewport=camera.WorldToViewportPoint(position);Assert.That(viewport.z<=0||viewport.x<0||viewport.x>1||viewport.y<0||viewport.y>1,Is.True,"Distant fixture must actually be outside the rendered frustum.");
                    var state=Call(marker,"GetState",4);var widget=(RectTransform)Field(state,"indicatorRT");var alpha=(CanvasGroup)Field(state,"indicatorCanvasGroup");var arrow=(RectTransform)Field(state,"indicatorArrowRT");
                    var screen=widget!=null?RectTransformUtility.WorldToScreenPoint(null,widget.position):Vector2.zero;
                    bool visible=widget!=null&&widget.gameObject.activeInHierarchy&&widget.lossyScale.x>.05f&&alpha!=null&&alpha.alpha>.1f&&arrow!=null&&arrow.gameObject.activeInHierarchy&&screen.x>2&&screen.y>2&&screen.x<Screen.width-2&&screen.y<Screen.height-2;all&=visible;
                    if(visible){var glyph=arrow.GetComponentInChildren(Type.GetType("UnityEngine.UI.Image, UnityEngine.UI",true),true);Assert.That(glyph,Is.Not.Null);Assert.That(((RectTransform)glyph.transform).rect.width,Is.LessThanOrEqualTo(32),"Direction cues must be compact rather than the stock100px chevron.");Assert.That((Color)Property(glyph,"color"),Is.EqualTo((Color)Field(marker,"tintColor")),"The real arrow retains its participant color.");var arrowCorners=new Vector3[4];arrow.GetWorldCorners(arrowCorners);float arrowBottom=arrowCorners.Min(p=>RectTransformUtility.WorldToScreenPoint(null,p).y);Assert.That(arrowBottom,Is.GreaterThan(boardTop+2),"Actual CNP arrow must clear the compact score row, not merely remain inside the screen.");}
                    TestContext.WriteLine("CUE slot="+i+" primary="+primary+" marker="+marker.name+" visible="+visible+" widgetActive="+(widget!=null&&widget.gameObject.activeInHierarchy)+" scale="+(widget!=null?widget.lossyScale.ToString():"none")+" alpha="+(alpha!=null?alpha.alpha:-1)+" arrow="+(arrow!=null&&arrow.gameObject.activeInHierarchy)+" screen="+screen+" viewport="+Field(state,"viewportPos"));
                }
                Assert.That(all,Is.True,"Every distant human must have a visible screen-safe vendor widget, including a diagonal bearing shared with another cue.");
            }
            finally{Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");foreach(var pad in pads)if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);if(isolation!=null)UnityEngine.Object.Destroy(isolation);InputSystem.settings.backgroundBehavior=background;}
        }
    }
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
