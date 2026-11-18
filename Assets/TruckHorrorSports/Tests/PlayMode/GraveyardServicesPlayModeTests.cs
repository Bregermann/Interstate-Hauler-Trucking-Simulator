#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public sealed class GraveyardServicesPlayModeTests
{
    static object Field(object target,string name)=>target.GetType().GetField(name).GetValue(target);
    static object Property(object target,string name)=>target.GetType().GetProperty(name).GetValue(target);
    static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name).Invoke(target,args);
    static string NightState(object match)
    {
        var supply=Field(match,"supplyJob");var rules=Field(match,"rules");var truck=((Array)Field(match,"trucks")).GetValue(0);var body=(Rigidbody)Field(truck,"body");
        var foot=(Component)((Array)Field(match,"footAgents")).GetValue(0);var actor=Field(match,"antagonist");var enemy=(Rigidbody)Field(actor,"body");
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        float damage=((float[])match.GetType().GetField("lastDamage",flags).GetValue(match))[0];var corners=(Vector3[][])match.GetType().GetField("pathCorners",flags).GetValue(match);
        return "clock="+Property(match,"Age")+" ending="+Property(match,"Ending")+" success="+Property(match,"Successful")+" health="+((float[])Field(rules,"health"))[0]+" outcome="+((Array)Field(rules,"outcome")).GetValue(0)+" lastDamage="+damage+" nextObjective="+Call(match,"NextObjective",0)+" player="+body.position.ToString("F2")+" playerV="+body.linearVelocity.ToString("F2")+" command="+Field(truck,"command")+" outside="+Property(foot,"Outside")+" foot="+foot.transform.position.ToString("F2")+" enemy="+enemy.position.ToString("F2")+" enemyV="+enemy.linearVelocity.ToString("F2")+" enemyState="+Property(actor,"State")+" enemyTarget="+Property(actor,"Target")+" footEnemyDistance="+Vector3.Distance(foot.transform.position,enemy.position).ToString("F2")+" cachedCorners="+(corners[0]==null?"none":string.Join(" / ",corners[0].Take(5).Select(c=>c.ToString("F2"))))+" "+Call(supply,"Diagnostic",0);
    }
    [UnityTest,Timeout(160000)]
    public IEnumerator NaturalShiftJobsPowerTheEquipmentAndTheSameNativeVisitorReturnsThenResets()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);var type=Type.GetType("LWS.TruckHorrorSports.HorrorMatch, Assembly-CSharp",true);
        UnityEngine.Object match=null;float previousScale=Time.timeScale;
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"Alpha10GraveyardServices"}))
        {
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHorrorSports/GraveyardShift/Scenes/GraveyardShift.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();var services=Field(match,"nightServices");Assert.That(services,Is.Not.Null,"Apply the explicit contextual-service scene upgrade.");
                var setup=Field(match,"setup");setup.GetType().GetField("count").SetValue(setup,1);match.GetType().GetField("option").SetValue(match,1);Call(match,"NormalizeSetup");Call(match,"AllBots");
                var visitor=Field(services,"visitor");var truck=Field(visitor,"truck");var body=(Rigidbody)Field(truck,"body");bool sawNativeWheelDrive=false;int captions=0;string lastCaption=null;
                Assert.That((bool)Call(match,"StartMatch"),Is.True);Time.timeScale=3;float deadline=Time.realtimeSinceStartup+115;float nextObservation=15;int observations=0;
                while(!(bool)Property(match,"Finished")&&Time.realtimeSinceStartup<deadline)
                {
                    if(observations<3&&(float)Property(match,"Age")>=nextObservation){observations++;nextObservation+=15;Debug.Log("GRAVEYARD_NIGHT_STATE "+NightState(match));}
                    sawNativeWheelDrive|=(bool)Property(truck,"Powered")&&(bool)Property(truck,"Grounded")&&body.linearVelocity.magnitude>1;
                    string caption=(string)Call(services,"CaptionFor",0);if(!string.IsNullOrEmpty(caption)&&caption!=lastCaption){captions++;lastCaption=caption;Assert.That(caption,Does.StartWith((string)Field(((Array)Field(setup,"players")).GetValue(0),"name")));}
                    if((int)Call(match,"Objectives",0)==0){Assert.That((bool)Property(services,"GeneratorRunning"),Is.False);Assert.That((bool)Property(services,"PowerRestored"),Is.False);}
                    yield return new WaitForFixedUpdate();
                }
                string state="clock="+Property(match,"Age")+" jobs="+Call(match,"Objectives",0)+" arrivals="+Property(visitor,"Arrivals")+" distance="+Property(visitor,"DistanceDriven")+" native="+body.position+" responses="+Property(services,"Responses")+" · "+NightState(match);
                Assert.That((bool)Property(match,"Finished"),Is.True,state);Assert.That((int)Call(match,"Objectives",0),Is.GreaterThanOrEqualTo(6),"The original jobs must genuinely complete. "+state);
                Assert.That((bool)Property(services,"PumpChecked"),Is.True,state);Assert.That((bool)Property(services,"PowerRestored"),Is.True,state);Assert.That((bool)Property(services,"GeneratorRunning"),Is.True,state);
                for(int task=0;task<6;task++)Assert.That((bool)Call(services,"Observed",0,task),Is.True,state);
                Assert.That(sawNativeWheelDrive,Is.True,"The visitor must physically drive with native grounded wheel propulsion. "+state);Assert.That((int)Property(visitor,"Arrivals"),Is.GreaterThanOrEqualTo(2),state);Assert.That((float)Property(visitor,"DistanceDriven"),Is.GreaterThan(100),state);
                Assert.That((int)Property(services,"Responses"),Is.InRange(6,8),"First task receipts and two arrivals are finite; repeated task cycles must not spam responses. "+state);Assert.That(captions,Is.GreaterThanOrEqualTo(3),state);Assert.That((bool)Call(services,"AnsweredSecondCall",0),Is.False,"AI's ordinary task path ignores the optional call; no hidden answer is fabricated.");
                foreach(var source in (AudioSource[])Field(services,"stations"))Assert.That(source.isPlaying,Is.False,"Ending/reset must stop owned spatial cues.");
                Call(match,"SetupScreen");yield return null;Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return new WaitForFixedUpdate();
                Assert.That((int)Property(services,"Responses"),Is.Zero);Assert.That((int)Property(visitor,"Arrivals"),Is.Zero);Assert.That((float)Property(visitor,"DistanceDriven"),Is.Zero);Assert.That((bool)Property(services,"GeneratorRunning"),Is.False);Assert.That((bool)Call(services,"HasSecurityEvidence",0),Is.False);Assert.That((bool)Call(services,"AnsweredSecondCall",0),Is.False);
                Debug.Log("GRAVEYARD_SERVICES_PASS actual jobs, native repeated arrival, finite owned captions/equipment/audio/reset. "+state);Call(match,"SetupScreen");
            }
            finally{try{if(match!=null&&(bool)Property(match,"Running"))Call(match,"SetupScreen");}finally{Time.timeScale=previousScale;}}
        }
    }
}
#endif
