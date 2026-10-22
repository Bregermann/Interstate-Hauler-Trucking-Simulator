#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class DanceConcertCameraTests
{
    static object Field(object o,string key)=>o.GetType().GetField(key).GetValue(o);
    static object Call(object o,string key,params object[] args)=>o.GetType().GetMethod(key).Invoke(o,args);
    [UnityTest]public IEnumerator MusicalShotFixturesUseOneRigAndCannotChangeScoresOrTimingOwnership()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"DanceConcertCamera"}))
        {
            Component match=null;
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckDanceOff/Scenes/TruckDanceOff.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckDanceOff.DanceMatch, Assembly-CSharp",true),FindObjectsSortMode.None).Single();Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return null;yield return null;
                ((Behaviour)match).enabled=false;Time.timeScale=0;var type=Type.GetType("LWS.TruckDanceOff.DanceConcertCamera, Assembly-CSharp",true);var director=match.GetComponent(type);var camera=(Camera)Field(match,"stageCamera");var rule=(Array)match.GetType().GetProperty("Rules").GetValue(match);
                foreach(var r in rule){Assert.That((double)r.GetType().GetProperty("AcceptedGreatWindow").GetValue(r),Is.EqualTo(.12).Within(.00001));Assert.That((double)r.GetType().GetProperty("AcceptedGoodWindow").GetValue(r),Is.EqualTo(.21).Within(.00001));}
                var song=match.GetType().GetProperty("Song").GetValue(match);double beat=(double)song.GetType().GetProperty("Beat").GetValue(song);double period=Math.Max((float)Field(director,"minimumShotSeconds"),beat*16);var clock=match.GetType().GetProperty("SongTime").GetSetMethod(true);Vector3 first=camera.transform.position;
                // Explicit camera-clock fixtures, not simulated rhythm outcomes.
                // Disable the match update so no DSP/input judgment is advanced.
                for(int shot=0;shot<4;shot++)
                {
                    clock.Invoke(match,new object[]{period*shot+.5});for(int f=0;f<12;f++)yield return null;
                    Assert.That((int)type.GetProperty("CurrentShot").GetValue(director),Is.EqualTo(shot));Assert.That(camera.transform.position.y,Is.GreaterThan(3));Assert.That(camera.fieldOfView,Is.InRange(49,55));
                    var rig=Field(director,"cameraRig");Assert.That((int)rig.GetType().GetProperty("FrameEvaluations").GetValue(rig),Is.EqualTo(1),"Only the existing Cinemachine adapter may compose this camera once per frame.");
                }
                var totals=(int[])Field(match,"total");int focused=(int)type.GetProperty("FocusedParticipant").GetValue(director);totals[(focused+1)%4]=100;for(int f=0;f<4;f++)yield return null;Assert.That((int)type.GetProperty("FocusedParticipant").GetValue(director),Is.EqualTo(focused),"A close-up must not chase every temporary leader change inside one musical shot.");Array.Clear(totals,0,totals.Length);
                Assert.That(Vector3.Distance(first,camera.transform.position),Is.GreaterThan(.05f));Assert.That(((int[])Field(match,"total")).All(v=>v==0),Is.True);Assert.That(rule.Cast<object>().All(r=>(int)r.GetType().GetProperty("Score").GetValue(r)==0),Is.True);
                ((Behaviour)match).enabled=true;Time.timeScale=1;Call(match,"SetupScreen");yield return null;Assert.That((int)type.GetProperty("CurrentShot").GetValue(director),Is.EqualTo(-1));
            }
            finally{Time.timeScale=1;if(match!=null){((Behaviour)match).enabled=true;Call(match,"SetupScreen");}}
        }
    }
}
#endif
