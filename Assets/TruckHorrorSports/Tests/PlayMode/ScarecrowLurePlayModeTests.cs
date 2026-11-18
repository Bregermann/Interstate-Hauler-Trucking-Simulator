#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public sealed class ScarecrowLurePlayModeTests
{
    static object Field(object target,string name)=>target.GetType().GetField(name).GetValue(target);
    static object Property(object target,string name)=>target.GetType().GetProperty(name).GetValue(target);
    static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name).Invoke(target,args);
    [UnityTest,Timeout(90000)]
    public IEnumerator NaturalStalkerUsesPhysicalFalseHeadlightsOrActualRevelationAndResets()
    {
        var type=Type.GetType("LWS.TruckHorrorSports.HorrorMatch, Assembly-CSharp",true);var helperType=Type.GetType("LWS.TruckHorrorSports.ScarecrowHeadlightLure, Assembly-CSharp",true);var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        UnityEngine.Object match=null;float previousScale=Time.timeScale;
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"Alpha10ScarecrowLure"}))
        {
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHorrorSports/ScarecrowSeason/Scenes/ScarecrowSeason.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();var actor=(Component)Field(match,"antagonist");var helper=actor.GetComponent(helperType);Assert.That(helper,Is.Not.Null,"Apply the explicit owned-pair lure upgrade and existing-pursuer hooks.");
                var setup=Field(match,"setup");setup.GetType().GetField("count").SetValue(setup,1);match.GetType().GetField("option").SetValue(match,0);Call(match,"NormalizeSetup");Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);
                Time.timeScale=3;float deadline=Time.realtimeSinceStartup+45;bool lightsObserved=false;
                while(!(bool)Property(match,"Finished")&&Time.realtimeSinceStartup<deadline)
                {
                    if((bool)Property(helper,"Luring"))
                    {
                        lightsObserved|=((Light[])Field(helper,"decoyLights")).Any(l=>l.enabled);
                        int owner=(int)Property(helper,"LureOwner");int station=(int)Property(helper,"Station");var markers=(Transform[])Field(helper,"lureStations");
                        Assert.That((Vector3)Call(helper,"DestinationFor",owner,Vector3.zero),Is.EqualTo(markers[station].position),"Only the existing force/navigation controller receives an authored destination.");
                    }
                    if((int)Property(helper,"LuresStarted")>0&&((float)Property(helper,"LureDistance")>2||(int)Property(helper,"Reveals")>0))break;
                    yield return new WaitForFixedUpdate();
                }
                string state="clock="+Property(match,"Age")+" started="+Property(helper,"LuresStarted")+" physicalLureDistance="+Property(helper,"LureDistance")+" actualReveals="+Property(helper,"Reveals")+" actor="+actor.transform.position;
                Assert.That((int)Property(helper,"LuresStarted"),Is.GreaterThan(0),state);
                Assert.That((float)Property(helper,"LureDistance")>2||(int)Property(helper,"Reveals")>0,Is.True,"The real AI must physically approach or actual player headlights must reveal the lure. "+state);
                Assert.That(lightsObserved||(int)Property(helper,"Reveals")>0,Is.True,state);
                Call(match,"SetupScreen");yield return null;Assert.That((bool)Property(helper,"Luring"),Is.False);Assert.That(((Light[])Field(helper,"decoyLights")).All(l=>!l.enabled),Is.True);Assert.That(((AudioSource)Field(helper,"lureSound")).isPlaying,Is.False);
                Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return new WaitForFixedUpdate();Assert.That((int)Property(helper,"LuresStarted"),Is.Zero);Assert.That((int)Property(helper,"Reveals"),Is.Zero);Assert.That((float)Property(helper,"LureDistance"),Is.Zero);
                Debug.Log("SCARECROW_LURE_LOOP_PASS actual existing-stalker strategy/headlights or illumination counterplay, finite cues and normal reset. This is a focused mechanics check, not complete-night/hardware acceptance. "+state);Call(match,"SetupScreen");
            }
            finally{try{if(match!=null&&(bool)Property(match,"Running"))Call(match,"SetupScreen");}finally{Time.timeScale=previousScale;}}
        }
    }
}
#endif
