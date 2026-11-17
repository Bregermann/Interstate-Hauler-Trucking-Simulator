#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckExtremeHauling.Tests
{
    public sealed class HaulingNaturalRoundTests
    {
        static Type Runtime(string name) => Type.GetType("LWS.TruckExtremeHauling." + name + ", Assembly-CSharp", true);
        static object Field(object target, string name) => target.GetType().GetField(name).GetValue(target);
        static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
        static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name).Invoke(target, args);
        [UnityTest] public IEnumerator NaturalDominoAICompletesPhysicalChainRound() => Natural("DominoDerby", "DominoDerbyMatch");
        [UnityTest] public IEnumerator NaturalPlinkoAIReachesSupportedScoringBin() => Natural("TrailerPlinko", "TrailerPlinkoMatch");
        [UnityTest] public IEnumerator NaturalCableAIReleasesAndDeliversLoad() => Natural("SkyhookSprint", "SkyhookSprintMatch");
        [UnityTest] public IEnumerator NaturalOrbitalAICouplesAndDeliversCargo() => Natural("ZeroGHitchUp", "ZeroGHitchMatch");
        [UnityTest] public IEnumerator NaturalCargoAIRecoversAndDeliversNativeTrailer() => Natural("CargoCrisis", "CargoCrisisMatch");
        [UnityTest] public IEnumerator NaturalIceAIProducesSupportedArticulatedDrift() => Natural("JackknifeOnIce", "JackknifeIceMatch");
        [UnityTest] public IEnumerator NaturalRoadTrainAIHaulsConnectedTrailers() => Natural("RoadTrainWreck", "RoadTrainWreckMatch");
        [UnityTest] public IEnumerator NaturalRingAIReleasesThroughPhysicalAperture() => Natural("HitchRingToss", "HitchRingTossMatch");
        [UnityTest] public IEnumerator NaturalThinIceAICompletesLoadedCrossing() => Natural("IceRoadReckoning", "ThinIceMatch");
        static string Describe(object match,string type)
        {
            var trailer=(Component)((Array)Field(match,"trailers")).GetValue(0);var load=(Rigidbody)Field(trailer,"Body");
            string detail=type+" load="+load.position.ToString("F2")+" velocity="+load.linearVelocity.ToString("F2")+" up="+Vector3.Dot(load.transform.up,Vector3.up).ToString("F2")+" gravity="+load.useGravity+" score="+Call(match,"ResultScore",0)+" objectives="+Call(match,"ObjectivesCompleted",0);
            if(type=="SkyhookSprintMatch")
            {
                var car=((Array)Field(match,"cableCars")).GetValue(0);var trolley=(Rigidbody)Field(car,"trolley");var cable=(SpringJoint)Field(car,"cable");
                detail+=" trolley="+trolley.position.ToString("F2")+" trolleyV="+trolley.linearVelocity.ToString("F2")+" released="+Property(car,"Released")+" cable="+cable.maxDistance+" localAnchor="+cable.anchor.ToString("F2")+" loadCOM="+load.centerOfMass.ToString("F2");
            }
            if(type=="ZeroGHitchMatch")
            {
                var truck=(Component)((Array)Field(match,"trucks")).GetValue(0);var hitch=truck.GetComponent(Type.GetType("LWS.TruckHitchSports.HitchCoupler, Assembly-CSharp",true));
                var anchor=(Transform)Property(hitch,"Anchor");var pin=(Transform)Property(trailer,"Kingpin");var body=(Rigidbody)Field(truck,"body");
                detail+=" tractor="+body.position.ToString("F2")+" velocity="+body.linearVelocity.ToString("F2")+" gravity="+body.useGravity+" pinDistance="+Vector3.Distance(anchor.position,pin.position).ToString("F3")+" angle="+Quaternion.Angle(body.rotation,load.rotation).ToString("F2")+" reason="+Property(hitch,"LastReason")+" attached="+(Property(hitch,"Attached")!=null);
            }
            if(type=="CargoCrisisMatch")
            {
                var truck=(Component)((Array)Field(match,"trucks")).GetValue(0);var hitch=truck.GetComponent(Type.GetType("LWS.TruckHitchSports.HitchCoupler, Assembly-CSharp",true));
                detail+=" recovery="+Call(match,"RecoveryPhase",0)+" cable="+Call(match,"CableConnected",0)+" tractor="+truck.transform.position.ToString("F2")+" "+Call(hitch,"DiagnosticState",trailer);
            }
            if(type=="JackknifeIceMatch")detail+=" "+Call(match,"DriftDiagnostic",0);
            return detail;
        }
        IEnumerator Natural(string scene, string type)
        {
            var prefs = Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true);
            using ((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null, new object[] { "Alpha10NaturalHauling" }))
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckExtremeHauling/Scenes/" + scene + ".unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null;
                object match = UnityEngine.Object.FindObjectsByType(Runtime(type), FindObjectsSortMode.None).Single();
                object setup = Field(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 1);
                Call(match, "NormalizeSetup"); Call(match, "AllBots");
                var weather = Field(match, "presentationWeather");
                if(weather != null && Field(weather,"presets") != null) Call(Field(weather,"presets"),"Select","9aad5ff5dfa09ce468205f676714f891");
                Assert.That((bool)Call(match,"StartMatch"),Is.True);
                try
                {
                    Time.timeScale = 3;
                    float deadline = Time.realtimeSinceStartup + 70;
                    float nextObservation=Time.realtimeSinceStartup;
                    while (!(bool)Property(match,"Finished") && Time.realtimeSinceStartup < deadline)
                    {
                        if(Time.realtimeSinceStartup>=nextObservation){nextObservation=Time.realtimeSinceStartup+5;Debug.Log("HH10_NATURAL_STATE "+Describe(match,type));}
                        yield return null;
                    }
                    Debug.Log("HH10_NATURAL_FINAL "+Describe(match,type));
                    string measured = scene + " score=" + Call(match,"ResultScore",0) + " objectives=" + Call(match,"ObjectivesCompleted",0);
                    Assert.That((bool)Property(match,"Finished"),Is.True,"Normal match must resolve. " + measured);
                    Assert.That((int)Call(match,"ObjectivesCompleted",0),Is.GreaterThan(0),"AI must physically perform a qualifying objective, not merely exhaust the timer. " + measured);
                    Assert.That((bool)Call(match,"ResultWinner",0),Is.True,"A real qualifying competitive result is required. " + measured);
                    string previousId=(string)Property(match,"MatchId");
                    Call(match,"SetupScreen"); yield return null;
                    Assert.That((bool)Property(match,"Finished"),Is.False,"Returning to setup clears the completed presentation state.");
                    Assert.That((bool)Call(match,"StartMatch"),Is.True,"The normal rematch start must remain available.");
                    Assert.That((string)Property(match,"MatchId"),Is.Not.EqualTo(previousId));
                    Assert.That((int)Call(match,"ObjectivesCompleted",0),Is.Zero,"Completed objectives must not carry into a rematch.");
                    Time.timeScale=3;yield return new WaitForSeconds(3.5f);
                    Assert.That((bool)Property(match,"Running"),Is.True,"Rematch physics must initialize without an immediate false result.");
                    Call(match,"SetupScreen");
                }
                finally { Time.timeScale = 1; }
            }
        }
    }
}
#endif
