#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckExtremeHauling.Tests
{
    public sealed class CargoPileupPlayModeTests
    {
        static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
        static object Field(object value,string name)=>value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(value);
        static object Property(object value,string name)=>value.GetType().GetProperty(name).GetValue(value);
        static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name).Invoke(value,args);

        [UnityTest]
        public IEnumerator NaturalHighwayPileupUsesPhysicalWrecksNativeRecoveryDeliveryAndRematch()
        {
            var lease=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"CargoPhysicalPileup"});
            float scale=Time.timeScale,fixedStep=Time.fixedDeltaTime;Component match=null;
            try{
                Time.timeScale=1;EditorSceneManager.LoadSceneInPlayMode("Assets/TruckExtremeHauling/Scenes/CargoCrisis.unity",new LoadSceneParameters(LoadSceneMode.Single));
                yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckExtremeHauling.CargoCrisisMatch"),FindObjectsSortMode.None)[0];
                var sites=Field(match,"emergencySites");var pileup=Field(sites,"highwayPileup");
                Assert.That(pileup,Is.Not.Null,"Install/save the explicit physical pileup first.");Assert.That((int)Property(match,"OptionCount"),Is.GreaterThanOrEqualTo(7));
                var setup=Field(match,"setup");setup.GetType().GetField("count").SetValue(setup,1);match.GetType().GetField("option").SetValue(match,6);
                Call(match,"NormalizeSetup");Call(match,"AllBots");
                var weather=Field(match,"presentationWeather");if(weather!=null&&Field(weather,"presets")!=null)Call(Field(weather,"presets"),"Select","9aad5ff5dfa09ce468205f676714f891");
                float duration=(float)Field(match,"matchSeconds");Assert.That((bool)Call(match,"StartMatch"),Is.True);string first=(string)Property(match,"MatchId");
                Assert.That((bool)Property(sites,"PileupActive"),Is.True);Assert.That((bool)Property(sites,"FloodActive"),Is.False);
                var load=((Array)Field(pileup,"loads")).GetValue(0);var cargo=(Rigidbody)Field(load,"Body");var spawn=cargo.position;
                var wrecks=(Array)Field(pileup,"wrecks");Assert.That(wrecks.Length,Is.EqualTo(8));
                var saved=new Vector3[wrecks.Length];int active=0;
                for(int i=0;i<wrecks.Length;i++){
                    var wreck=wrecks.GetValue(i);var body=(Rigidbody)Field(wreck,"body");saved[i]=body.transform.position;
                    if((int)Field(wreck,"lane")!=0)continue;active++;Assert.That(body.isKinematic,Is.False);Assert.That(body.mass,Is.GreaterThan(500));
                    Assert.That(body.GetComponentsInChildren<Collider>(true).Length,Is.GreaterThan(0));
                    foreach(var component in body.GetComponentsInChildren<MonoBehaviour>(true))Assert.That(component.enabled,Is.False,"A passive wreck must not run hidden vehicle/traffic/input simulation.");
                }
                Assert.That(active,Is.EqualTo(2));
                bool cable=false,native=false;float furthest=0;
                var truck=(Component)((Array)Field(match,"trucks")).GetValue(0);var hitch=truck.GetComponent(Runtime("LWS.TruckHitchSports.HitchCoupler"));
                Time.timeScale=3;float deadline=Time.realtimeSinceStartup+90;
                while(!(bool)Property(match,"Finished")&&Time.realtimeSinceStartup<deadline){
                    foreach(var joint in (SpringJoint[])Field(match,"winches"))if(joint!=null&&joint.connectedBody==cargo&&joint.currentForce.magnitude>100)cable=true;
                    native|=Property(hitch,"Attached")==load&&(bool)Property(hitch,"PhysicalConstraintPresent");
                    furthest=Mathf.Max(furthest,Vector3.Distance(cargo.position,spawn));yield return null;
                }
                Time.timeScale=1;
                string observed="phase="+Call(match,"RecoveryPhase",0)+" cargo="+cargo.position+" native="+native+" cable="+cable+" wreck_planar_metres="+((float[])Field(sites,"PileupWreckMetres"))[0];
                Assert.That((bool)Property(match,"Finished"),Is.True,observed);Assert.That((float)Field(match,"matchSeconds"),Is.EqualTo(duration));
                Assert.That(cable&&native,Is.True,"The actual load must be recovered and physically coupled. "+observed);
                Assert.That(furthest,Is.GreaterThan(30),observed);Assert.That((int)Call(match,"ObjectivesCompleted",0),Is.GreaterThan(0),observed);
                Assert.That((bool)Call(match,"ResultWinner",0),Is.True,observed);
                Assert.That(Property(Field(match,"menus"),"Screen").ToString(),Is.EqualTo("Result"));
                TestContext.WriteLine("CARGO_HIGHWAY_PILEUP software_ai_only=true "+observed+"; alternate shoulders allowed, no particular/random collision required or rewarded; no poses/scores/outcomes injected.");
                Call(match,"SetupScreen");yield return null;Assert.That((bool)Call(match,"StartMatch"),Is.True);
                Assert.That((string)Property(match,"MatchId"),Is.Not.EqualTo(first));Assert.That((float[])Field(sites,"PileupWreckMetres"),Is.All.EqualTo(0));
                for(int i=0;i<wrecks.Length;i++)if((int)Field(wrecks.GetValue(i),"lane")==0)Assert.That(Vector3.Distance(((Rigidbody)Field(wrecks.GetValue(i),"body")).position,saved[i]),Is.LessThan(.02f));
                Time.timeScale=3;yield return new WaitForSeconds(4);Time.timeScale=1;Assert.That((bool)Property(match,"Live"),Is.True);
                Call(match,"SetupScreen");match.GetType().GetField("option").SetValue(match,0);Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return null;
                Assert.That((bool)Property(sites,"PileupActive"),Is.False);Assert.That(((GameObject)Field(pileup,"environment")).activeSelf,Is.False);
                Assert.That(Time.fixedDeltaTime,Is.EqualTo(fixedStep));
            }finally{if(match!=null)Call(match,"SetupScreen");Time.timeScale=scale;lease.Dispose();}
        }
    }
}
#endif
