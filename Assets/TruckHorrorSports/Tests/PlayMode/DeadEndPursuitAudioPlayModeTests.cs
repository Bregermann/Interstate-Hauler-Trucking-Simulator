#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public sealed class DeadEndPursuitAudioPlayModeTests
{
    static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    static object Property(object value,string name)=>value.GetType().GetProperty(name).GetValue(value);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name).Invoke(value,args);
    [UnityTest,Timeout(100000)]
    public IEnumerator NaturalPhysicalPursuitWarnsTheActualTargetThenSilencesAndResets()
    {
        var type=Type.GetType("LWS.TruckHorrorSports.HorrorMatch, Assembly-CSharp",true);var helperType=Type.GetType("LWS.TruckHorrorSports.DeadEndPursuitAudio, Assembly-CSharp",true);var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);UnityEngine.Object match=null;float oldScale=Time.timeScale;
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"Alpha10PhysicalPursuitCues"}))
        {
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHorrorSports/DeadEnd/Scenes/DeadEnd.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();var actor=(Component)Field(match,"antagonist");var helper=actor.GetComponent(helperType);Assert.That(helper,Is.Not.Null,"Author the explicit existing-actor cue upgrade first.");
                var setup=Field(match,"setup");setup.GetType().GetField("count").SetValue(setup,1);match.GetType().GetField("option").SetValue(match,1);Call(match,"NormalizeSetup");Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);
                var body=(Rigidbody)Field(actor,"body");Vector3 start=body.position;Time.timeScale=3;float deadline=Time.realtimeSinceStartup+65,nextDiagnostic=30;int observations=0;
                int firstOwner=-1;string firstCaption=null;
                while(!(bool)Property(match,"Finished")&&Time.realtimeSinceStartup<deadline)
                {
                    if(firstOwner<0&&(int)Property(helper,"WarningCount")>0)
                    {
                        firstOwner=(int)Property(helper,"LastWarningOwner");
                        firstCaption=(string)Call(helper,"CaptionFor",0);
                    }
                    if(firstOwner>=0&&Vector3.Distance(start,body.position)>20)break;
                    if(observations<2&&(float)Property(match,"Age")>=nextDiagnostic){observations++;nextDiagnostic+=60;Debug.Log("HH10_DEAD_END_APPROACH_STATE clock="+Property(match,"Age")+" "+Call(helper,"Diagnostic"));}
                    yield return new WaitForFixedUpdate();
                }
                string state="clock="+Property(match,"Age")+" "+Call(helper,"Diagnostic");Assert.That((int)Property(helper,"WarningCount"),Is.GreaterThan(0),"A normal physical pursuit must produce a readable actual approach, not a timer-only scare. "+state);Assert.That(firstOwner,Is.EqualTo(0),state);Assert.That(Vector3.Distance(start,body.position),Is.GreaterThan(20),state);Assert.That(firstCaption,Is.Not.Empty,state);
                var source=(AudioSource)Field(helper,"emitter");Assert.That(source.transform.IsChildOf(actor.transform),Is.True);Assert.That(source.spatialBlend,Is.EqualTo(1));Assert.That(Field(helper,"horn"),Is.Not.Null);Assert.That(Field(helper,"airBrake"),Is.Not.Null);
                Call(match,"SetupScreen");yield return null;Assert.That(source.isPlaying,Is.False);Assert.That((bool)Property(helper,"Active"),Is.False);Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return new WaitForFixedUpdate();Assert.That((int)Property(helper,"WarningCount"),Is.Zero);Call(match,"SetupScreen");yield return null;
                foreach(int option in new[]{0,2}){match.GetType().GetField("option").SetValue(match,option);Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return new WaitForFixedUpdate();Assert.That((bool)Property(helper,"Active"),Is.False,"Existing default/puzzle paths must not acquire new pursuit cues.");Assert.That(source.isPlaying,Is.False);Call(match,"SetupScreen");yield return null;}
                Debug.Log("DEAD_END_PHYSICAL_CUE_PASS actual pursuer movement/target/approach, owned spatial clip binding, stopped audio/rematch and default/puzzle isolation. Listening and full-match acceptance are not claimed. "+state);
            }
            finally{try{if(match!=null&&(bool)Property(match,"Running"))Call(match,"SetupScreen");}finally{Time.timeScale=oldScale;}}
        }
    }
}
#endif
