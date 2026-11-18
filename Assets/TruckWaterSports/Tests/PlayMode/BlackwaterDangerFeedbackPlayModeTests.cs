#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckWaterSports.Tests
{
    public sealed class BlackwaterDangerFeedbackPlayModeTests
    {
        static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
        static object Field(object value,string name)=>value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(value);
        static object Property(object value,string name)=>value.GetType().GetProperty(name).GetValue(value);
        static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name).Invoke(value,args);

        [UnityTest]
        public IEnumerator TwoSoftwareHumansReceiveTheirOwnActualCreatureCuesAndReset()
        {
            var lease=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"BlackwaterOwnedDanger"});
            float scale=Time.timeScale,step=Time.fixedDeltaTime;
            var background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            Gamepad first=null,second=null;Component match=null;
            try
            {
                Time.timeScale=1;
                var weather=AssetDatabase.LoadAssetAtPath("Assets/TruckSports/Settings/SportsWeatherPresets.asset",Runtime("LWS.TruckSports.SportsWeatherPresets"));
                bool clear=false;foreach(var entry in (Array)Field(weather,"entries"))if((string)Field(entry,"displayName")=="Clear")
                {Call(weather,"Select",(string)Field(entry,"id"));clear=true;break;}
                Assert.That(clear,Is.True);
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckWaterSports/Scenes/Blackwater.unity",new LoadSceneParameters(LoadSceneMode.Single));
                yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckWaterSports.BlackwaterMatch"),FindObjectsSortMode.None)[0];
                first=InputSystem.AddDevice<Gamepad>("Blackwater first software controller");second=InputSystem.AddDevice<Gamepad>("Blackwater second software controller");
                yield return null;yield return new WaitForFixedUpdate();
                var setup=Field(match,"setup");setup.GetType().GetField("count").SetValue(setup,2);
                var players=(Array)Field(setup,"players");
                for(int i=0;i<2;i++){var player=players.GetValue(i);var source=player.GetType().GetField("source");source.SetValue(player,Enum.Parse(source.FieldType,"WholeController"));players.SetValue(player,i);}
                match.GetType().GetField("option").SetValue(match,2);Call(match,"NormalizeSetup");
                Assert.That((bool)Call(match,"Assign",0,first),Is.True);Assert.That((bool)Call(match,"Assign",1,second),Is.True);
                Assert.That((bool)Call(match,"StartMatch"),Is.True);string firstId=(string)Property(match,"MatchId");
                InputSystem.QueueStateEvent(first,new GamepadState());InputSystem.QueueStateEvent(second,new GamepadState());
                yield return null;yield return new WaitForFixedUpdate();yield return null;
                var cameras=(Array)Field(match,"participantCameras");
                var view0=(Camera)Field(cameras.GetValue(0),"view");var view1=(Camera)Field(cameras.GetValue(1),"view");
                Assert.That(view0.enabled&&view1.enabled,Is.True,"The existing two-human split views must remain active.");
                Assert.That(view0.rect.width,Is.EqualTo(.5f));Assert.That(view1.rect.x,Is.EqualTo(.5f));
                bool nearbyCue=false,pursuitCue=false;string observed="";
                Time.timeScale=4;float deadline=Time.realtimeSinceStartup+45;
                // Ordinary neutral player inputs allow the real authored stalker
                // to approach after its existing35s pacing. No actor/state is moved.
                while((float)Property(match,"Clock")<60&&(bool)Property(match,"Running")&&Time.realtimeSinceStartup<deadline)
                {
                    for(int slot=0;slot<2;slot++)
                    {
                        string cue=(string)Call(match,"ParticipantDanger",slot);if(cue.Length==0)continue;
                        object[] args={slot,null,null};Assert.That((bool)Call(match,"TryReadScoreCard",args),Is.True);
                        Assert.That((string)args[2],Does.Contain(cue),"The actual participant's scorecard must carry its own cue.");
                        if(cue.StartsWith("WAKE")||cue.StartsWith("HUNTED"))nearbyCue=true;
                        if(cue.StartsWith("HUNTED"))
                        {Assert.That(slot,Is.EqualTo((int)Field(match,"hunted")),"Another participant must not inherit the current target's pursuit warning.");pursuitCue=true;}
                        observed="clock="+Property(match,"Clock")+" slot="+slot+" cue="+cue;
                    }
                    yield return null;
                }
                Time.timeScale=1;
                Assert.That(nearbyCue&&pursuitCue,Is.True,"A real creature approach must produce owner-specific feedback. "+observed);
                Call(match,"SetupScreen");yield return null;
                for(int slot=0;slot<2;slot++)Assert.That((string)Call(match,"ParticipantDanger",slot),Is.Empty,"No danger cue persists after leaving the round.");
                Assert.That((bool)Call(match,"StartMatch"),Is.True);Assert.That((string)Property(match,"MatchId"),Is.Not.EqualTo(firstId));
                for(int slot=0;slot<2;slot++)Assert.That((string)Call(match,"ParticipantDanger",slot),Is.Empty,"Normal rematch must clear strike/quiet history and reset the actual entity.");
                Assert.That(Time.fixedDeltaTime,Is.EqualTo(step));
                TestContext.WriteLine("BLACKWATER_OWNED_DANGER software_gamepads_only=true split_views=true "+observed+"; actual entity approach, owner-specific existing scorecards and ordinary reset; no poses, clocks, target, damage or outcomes injected.");
            }
            finally
            {
                if(match!=null)Call(match,"SetupScreen");
                if(first!=null&&first.added)InputSystem.RemoveDevice(first);if(second!=null&&second.added)InputSystem.RemoveDevice(second);
                InputSystem.settings.backgroundBehavior=background;Time.timeScale=scale;lease.Dispose();
            }
        }
    }
}
#endif
