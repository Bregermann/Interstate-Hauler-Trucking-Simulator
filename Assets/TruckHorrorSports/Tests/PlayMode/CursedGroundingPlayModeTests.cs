#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public sealed class CursedGroundingPlayModeTests
{
    static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    static object Property(object value,string name)=>value.GetType().GetProperty(name).GetValue(value);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name).Invoke(value,args);
    [UnityTest,Timeout(170000)]
    public IEnumerator NaturalExorcismPhysicallyGroundsOriginalLoadAtEachWardAndResets()
    {
        var type=Type.GetType("LWS.TruckHorrorSports.HorrorMatch, Assembly-CSharp",true);var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);UnityEngine.Object match=null;float oldScale=Time.timeScale;
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"Alpha10CursedGrounding"}))
        {
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHorrorSports/CursedCargo/Scenes/CursedCargo.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();var ritual=Field(match,"groundingRitual");Assert.That(ritual,Is.Not.Null,"Author the explicit exorcism grounding upgrade first.");
                var setup=Field(match,"setup");setup.GetType().GetField("count").SetValue(setup,1);match.GetType().GetField("option").SetValue(match,2);Call(match,"NormalizeSetup");Call(match,"AllBots");
                var cargo=((Array)Field(match,"cargo")).GetValue(0);var original=Field(cargo,"trailer");Assert.That((bool)Call(match,"StartMatch"),Is.True);Assert.That((bool)Property(ritual,"Active"),Is.True);
                bool observedLiveCable=false;Time.timeScale=3;float deadline=Time.realtimeSinceStartup+115,next=45;int rows=0;
                while(!(bool)Property(match,"Finished")&&Time.realtimeSinceStartup<deadline)
                {
                    if((bool)Call(ritual,"Attached",0))
                    {
                        var body=(Rigidbody)Property(cargo,"Body");Assert.That(body.GetComponents<SpringJoint>().Length,Is.GreaterThan(0),"The visible lead must have a real physical constraint on the ORIGINAL load.");observedLiveCable=true;
                        Assert.That(Field(cargo,"trailer"),Is.SameAs(original));
                    }
                    if(rows<4&&(float)Property(match,"Age")>=next){rows++;next+=45;Debug.Log("HH10_EXORCISM_GROUNDING clock="+Property(match,"Age")+" "+Call(ritual,"Diagnostic",0));}
                    yield return new WaitForFixedUpdate();
                }
                string state="clock="+Property(match,"Age")+" "+Call(ritual,"Diagnostic",0);Assert.That((bool)Property(match,"Finished"),Is.True,state);Assert.That(observedLiveCable,Is.True,state);Assert.That((bool)Property(cargo,"Exorcised"),Is.True,"Timer-only partial work cannot banish the load. "+state);Assert.That((bool)Call(match,"Escaped",0),Is.True,state);
                Assert.That((int)Call(match,"Objectives",0),Is.EqualTo(3),state);Assert.That((int)Call(ritual,"GroundedWards",0),Is.EqualTo(3),"Every ordinary receipt must follow a real settled ground lead. "+state);Assert.That(Field(cargo,"trailer"),Is.SameAs(original));Assert.That((bool)Property(cargo,"Ready"),Is.True,state);
                Assert.That(float.IsFinite((float)Call(ritual,"PeakTension",0)),Is.True,state);Assert.That((float)Property(cargo,"Condition"),Is.GreaterThan(0),state);
                Call(match,"SetupScreen");yield return null;Assert.That((bool)Call(ritual,"Attached",0),Is.False);Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return new WaitForFixedUpdate();Assert.That((int)Call(ritual,"Connections",0),Is.Zero);Assert.That((int)Call(ritual,"GroundedWards",0),Is.Zero);Assert.That((bool)Property(cargo,"Exorcised"),Is.False);Call(match,"SetupScreen");yield return null;
                match.GetType().GetField("option").SetValue(match,0);Assert.That((bool)Call(match,"StartMatch"),Is.True);Assert.That((bool)Property(ritual,"Active"),Is.False);Assert.That(((GameObject)Field(ritual,"geometry")).activeSelf,Is.False);Assert.That((bool)Call(ritual,"AllowsChannel",0,0),Is.True,"Default delivery must retain its existing channel authority.");Call(match,"SetupScreen");
                Debug.Log("CURSED_GROUNDING_PASS actual native original load, three physical tethers and ward receipts, permanent exorcism, rematch and default isolation. "+state);
            }
            finally{try{if(match!=null&&(bool)Property(match,"Running"))Call(match,"SetupScreen");}finally{Time.timeScale=oldScale;}}
        }
    }
}
#endif
