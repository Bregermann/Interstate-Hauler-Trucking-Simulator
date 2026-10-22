#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class MattressManualResetTests
{
    static object Field(object target,string name)=>target.GetType().GetField(name).GetValue(target);
    static object Property(object target,string name)=>target.GetType().GetProperty(name).GetValue(target);
    static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name).Invoke(target,args);
    [UnityTest]public IEnumerator HumanInversionKeepsPhysicsAndWaitsForAFreshResetConfirmation()=>Inversion(true);
    [UnityTest]public IEnumerator AiInversionRetainsItsAutonomousRecovery()=>Inversion(false);
    static IEnumerator Inversion(bool human)
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"MattressManualReset"+human}))
        {
            Component match=null;Keyboard keyboard=null;GameObject isolation=null;var background=InputSystem.settings.backgroundBehavior;var editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            try
            {
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;isolation=new GameObject("Mattress reset physical-input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Type.GetType("LWS.TruckSports.SportsProbeInputIsolation, Assembly-CSharp",true));
                if(human){keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.EnableDevice(keyboard);keyboard.MakeCurrent();}
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckMattressRacing/Scenes/TruckMattressRacing.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckMattressRacing.MattressMatch, Assembly-CSharp",true),FindObjectsSortMode.None).Single();Call(match,"AllBots");
                if(human){var player=((Array)Field(Field(match,"setup"),"players")).GetValue(0);var source=player.GetType().GetField("source");source.SetValue(player,Enum.Parse(source.FieldType,"Keyboard"));string[] fields={"forward","left","back","right"};Key[] keys={Key.W,Key.A,Key.S,Key.D};for(int i=0;i<4;i++)player.GetType().GetField(fields[i]).SetValue(player,keys[i]);}
                var weather=Field(Field(match,"course"),"weather");var catalog=Field(weather,"presets");foreach(var entry in (IEnumerable)Field(catalog,"entries"))if((string)Field(entry,"displayName")=="Clear"){Call(catalog,"Select",Field(entry,"id"));break;}
                Assert.That((bool)Call(match,"StartMatch"),Is.True);Time.timeScale=3;float deadline=Time.realtimeSinceStartup+5;while(Property(match,"Phase").ToString()!="Race"&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That(Property(match,"Phase").ToString(),Is.EqualTo("Race"));
                var racers=(Array)Field(match,"racers");for(int i=1;i<racers.Length;i++)Call(racers.GetValue(i),"Fail");var racer=racers.GetValue(0);var truck=Field(racer,"truck");var body=(Rigidbody)Field(truck,"body");var at=(Vector3)Call(Field(match,"course"),"Spawn",0,0f)+Vector3.up*3;
                body.position=at;body.rotation=Quaternion.Euler(0,0,180);body.linearVelocity=body.angularVelocity=Vector3.zero;Physics.SyncTransforms();
                if(human)InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));
                float inverted=0,longest=0,began=Time.time;while(Time.time-began<5.2f&&(int)Property(racer,"Recoveries")==0){yield return new WaitForFixedUpdate();inverted=Vector3.Dot(body.rotation*Vector3.up,Vector3.up)<.2f?inverted+Time.fixedDeltaTime:0;longest=Mathf.Max(longest,inverted);}
                TestContext.WriteLine("Actual inverted duration="+longest+" human="+human+" recoveries="+Property(racer,"Recoveries")+" phase="+Property(racer,"Phase")+" body="+body.position);
                Assert.That(longest,Is.GreaterThan(2.8f),"Fixture must remain physically inverted long enough to exercise the old automatic flip-reset threshold.");
                if(!human){Assert.That((int)Property(racer,"Recoveries"),Is.EqualTo(1));yield break;}
                Assert.That(keyboard.spaceKey.isPressed,Is.True,"Fixture must deliver its held keyboard event to player input, even without Editor Game View focus.");Assert.That((int)Property(racer,"Recoveries"),Is.Zero,"A human's overturned truck must not teleport before a fresh confirmation, even if brace was already held.");Assert.That(body.isKinematic,Is.False);Assert.That(Property(racer,"Phase").ToString(),Is.EqualTo("Racing"));Assert.That((bool)Property(racer,"NeedsReset"),Is.True);
                Assert.That(((string)Call(match,"ViewText",0)).Contains("RESET TRUCK?"),Is.True);
                yield return null;Canvas.ForceUpdateCanvases();var panels=(RectTransform[])Field(Field(match,"menus"),"viewPanels");var prompt=panels[0].Find("Compact owned viewport score/Contextual action") as RectTransform;Assert.That(prompt,Is.Not.Null);Assert.That(prompt.gameObject.activeInHierarchy,Is.True);var corners=new Vector3[4];prompt.GetWorldCorners(corners);var camera=((Camera[])Field(match,"cameras"))[0];foreach(var corner in corners){var point=RectTransformUtility.WorldToScreenPoint(null,corner);Assert.That(point.x,Is.InRange(camera.pixelRect.xMin-1,camera.pixelRect.xMax+1));Assert.That(point.y,Is.InRange(camera.pixelRect.yMin-1,camera.pixelRect.yMax+1));}
                Assert.That((string)Call(match,"ViewportContextPrompt",0),Does.Contain((string)Call(match,"ResetControlCaption",0)));
                // Input edges are sampled in Update; several FixedUpdates can share a rendered frame.
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.12f);TestContext.WriteLine("RESET RELEASE held="+Call(match,"ActionHeld",0)+" needs="+Property(racer,"NeedsReset")+" released="+Field(((Array)Field(Field(match,"setup"),"players")).GetValue(0),"released")+" key="+((Key[])Field(match,"actionKeys"))[0]+" paused="+Property(match,"Paused")+" device="+keyboard.deviceId+" current="+(Keyboard.current!=null?Keyboard.current.deviceId:-1)+" ownSpace="+keyboard.spaceKey.isPressed+" enabled="+keyboard.enabled+" update="+InputSystem.settings.updateMode);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));yield return new WaitForSecondsRealtime(.12f);
                TestContext.WriteLine("RESET PRESS held="+Call(match,"ActionHeld",0)+" needs="+Property(racer,"NeedsReset")+" phase="+Property(racer,"Phase")+" paused="+Property(match,"Paused")+" device="+keyboard.deviceId+" current="+(Keyboard.current!=null?Keyboard.current.deviceId:-1)+" ownSpace="+keyboard.spaceKey.isPressed+" enabled="+keyboard.enabled+" update="+InputSystem.settings.updateMode);
                Assert.That((int)Property(racer,"Recoveries"),Is.EqualTo(1),"A newly pressed assigned action confirms exactly one existing recovery and penalty.");
            }
            finally{Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(isolation!=null)UnityEngine.Object.Destroy(isolation);InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;}
        }
    }
}
#endif
