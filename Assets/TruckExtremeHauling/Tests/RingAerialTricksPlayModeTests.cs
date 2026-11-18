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

namespace LWS.TruckExtremeHauling.Tests
{
    public sealed class RingAerialTricksPlayModeTests
    {
        static Type Runtime => Type.GetType("LWS.TruckExtremeHauling.HitchRingTossMatch, Assembly-CSharp", true);
        static object Field(object target, string name) => target.GetType().GetField(name).GetValue(target);
        static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
        static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name).Invoke(target, args);
        [UnityTest]
        public IEnumerator NaturalTrickRoundScoresActualRingPassagesAndRejectsSupportedRotationAcrossRematch()
        {
            var prefs = Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true);
            using ((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null, new object[] { "Alpha10RingAerialTricks" }))
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckExtremeHauling/Scenes/HitchRingToss.unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null;
                var match = UnityEngine.Object.FindObjectsByType(Runtime, FindObjectsSortMode.None).Single();
                match.GetType().GetField("option").SetValue(match, 3);
                Field(match, "setup").GetType().GetField("count").SetValue(Field(match, "setup"), 1);
                Call(match, "NormalizeSetup"); Call(match, "AllBots");
                var weather = Field(match, "presentationWeather");
                if (weather != null && Field(weather, "presets") != null) Call(Field(weather, "presets"), "Select", "9aad5ff5dfa09ce468205f676714f891");
                var trailer = ((Array)Field(match, "trailers")).GetValue(0); var body = (Rigidbody)Field(trailer, "Body"); int bodyId = body.GetInstanceID();
                var ring=((Array)Field(match,"rings")).GetValue(0) as Component;Vector3 authoredRing=ring.transform.position;
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                Assert.That((bool)Call(match,"TrickLaneReady",0),Is.True,(string)Call(match,"TrickDiagnostic",0));
                Assert.That(Vector3.Distance(ring.transform.position,authoredRing),Is.EqualTo((float)Field(match,"trickRingDownrangeOffset")).Within(.01f));
                int observedAttempts=0;
                int supportedSamples = 0; bool priorGrounded = false; int priorTurns = 0, priorAttempt = -1; float priorAir = 0;
                try
                {
                    Time.timeScale = 3; float deadline = Time.realtimeSinceStartup + 75; float nextObservation = Time.realtimeSinceStartup;
                    while (!(bool)Property(match, "Finished") && Time.realtimeSinceStartup < deadline)
                    {
                        var meter = ((Array)FieldNonPublic(match, "trickMeters")).GetValue(0);
                        bool released = (bool)Call(match, "TrailerReleased", 0);
                        int attempt = (int)Call(match, "AttemptsResolved", 0);
                        if(attempt!=observedAttempts){observedAttempts=attempt;Debug.Log("HH10_RING_TRICK_REAL_OUTCOME "+Call(match,"LastTrickAttemptDiagnostic",0));}
                        bool grounded = released && !(bool)Property(meter, "FreeFlight");
                        int turns = (int)Property(meter, "CompletedTurns"); float air = (float)Property(meter, "FreeFlightSeconds");
                        if (grounded && priorGrounded && attempt == priorAttempt)
                        {
                            supportedSamples++;
                            Assert.That(turns, Is.EqualTo(priorTurns), "Supported roof/chassis/wheel rotation must never accumulate a new aerial trick. " + Call(match, "TrickDiagnostic", 0));
                            Assert.That(air, Is.EqualTo(priorAir).Within(.0001f), "Ground sliding cannot be timed as free flight. " + Call(match, "TrickDiagnostic", 0));
                        }
                        if (Time.realtimeSinceStartup >= nextObservation) { nextObservation = Time.realtimeSinceStartup + 6; Debug.Log("HH10_RING_TRICK_PHYSICS " + Call(match, "TrickDiagnostic", 0) + " cargo=" + body.position.ToString("F2")); }
                        priorGrounded = grounded; priorTurns = turns; priorAir = air; priorAttempt = attempt;
                        yield return null;
                    }
                    string diagnostic = (string)Call(match, "TrickDiagnostic", 0);
                    int passed = (int)Call(match, "ObjectivesCompleted", 0), misses = (int)Call(match, "Misses", 0), qualifiedTurns = (int)Call(match, "PassedAerialTrickTurns", 0);
                    Assert.That((bool)Property(match, "Finished"), Is.True, diagnostic);
                    Assert.That((int)Call(match, "AttemptsResolved", 0), Is.EqualTo((int)Field(match, "throwsPerPlayer")), diagnostic);
                    Assert.That(passed, Is.GreaterThan(0), "A natural full aperture passage is required; timer-only misses are insufficient. " + diagnostic);
                    Assert.That(passed + misses, Is.EqualTo((int)Field(match, "throwsPerPlayer")), "One outcome per real physical throw.");
                    Assert.That(body.GetInstanceID(), Is.EqualTo(bodyId), "All attempts retain the original native physical trailer.");
                    Assert.That(supportedSamples, Is.GreaterThan(0), "This negative check requires actual observed supported post-release physics, not an unsampled claim.");
                    Assert.That(qualifiedTurns,Is.GreaterThan(0),"At least one genuine full airborne revolution must precede an actual aperture pass. "+Call(match,"LastTrickAttemptDiagnostic",0));
                    Assert.That((double)Call(match,"ResultScore",0),Is.GreaterThan(passed*1000-misses*100),"An actual signed airborne turn must produce its real scoring bonus.");
                    Assert.That((bool)Call(match, "ResultWinner", 0), Is.True);
                    Call(match,"SetupScreen");match.GetType().GetField("option").SetValue(match,0);Assert.That((bool)Call(match,"StartMatch"),Is.True);
                    Assert.That(Vector3.Distance(ring.transform.position,authoredRing),Is.LessThan(.001f),"Other variants restore the exact authored ring origin.");
                    Call(match,"SetupScreen");match.GetType().GetField("option").SetValue(match,3);Assert.That((bool)Call(match,"StartMatch"),Is.True);
                    Assert.That((int)Call(match, "AerialTrickTurns", 0), Is.Zero); Assert.That((int)Call(match, "PassedAerialTrickTurns", 0), Is.Zero);
                    Assert.That((int)Call(match, "AttemptsResolved", 0), Is.Zero); Assert.That((double)Call(match, "ResultScore", 0), Is.Zero);
                    yield return new WaitForSeconds(3.6f);
                    Assert.That((bool)Property(match, "Running"), Is.True); Call(match, "SetupScreen");
                }
                finally { Time.timeScale = 1; }
            }
        }
        static void Queue(Gamepad pad,float steering,float effort,bool action)
        {
            var state=new GamepadState{leftStick=new Vector2(steering,0),rightTrigger=Mathf.Max(0,effort),leftTrigger=Mathf.Max(0,-effort)};
            if(action)state=state.WithButton(GamepadButton.South);
            InputSystem.QueueStateEvent(pad,state);
        }
        [UnityTest]
        public IEnumerator AssignedSoftwarePadReleasesRollsARealFullTurnAndPassesThePhysicalRing()
        {
            var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
            using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"Alpha10RingHumanRoll"}))
            {
                var oldBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                var pad=InputSystem.AddDevice<Gamepad>();object match=null;
                try
                {
                    EditorSceneManager.LoadSceneInPlayMode("Assets/TruckExtremeHauling/Scenes/HitchRingToss.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                    match=UnityEngine.Object.FindObjectsByType(Runtime,FindObjectsSortMode.None).Single();
                    var setup=Field(match,"setup");setup.GetType().GetField("count").SetValue(setup,1);match.GetType().GetField("option").SetValue(match,3);match.GetType().GetField("throwsPerPlayer").SetValue(match,1);
                    var player=((Array)Field(setup,"players")).GetValue(0);var source=player.GetType().GetField("source");source.SetValue(player,Enum.Parse(source.FieldType,"WholeController"));player.GetType().GetField("pad").SetValue(player,pad);
                    Call(match,"NormalizeSetup");Queue(pad,0,0,false);yield return null;yield return null;yield return null;
                    var weather=Field(match,"presentationWeather");if(weather!=null&&Field(weather,"presets")!=null)Call(Field(weather,"presets"),"Select","9aad5ff5dfa09ce468205f676714f891");
                    Assert.That((bool)Property(match,"DevicesReady"),Is.True);Assert.That((bool)Call(match,"StartMatch"),Is.True);Assert.That((bool)Call(match,"TrickLaneReady",0),Is.True,(string)Call(match,"TrickDiagnostic",0));
                    var trailer=((Array)Field(match,"trailers")).GetValue(0);var body=(Rigidbody)Field(trailer,"Body");int bodyId=body.GetInstanceID();var ring=(Component)((Array)Field(match,"rings")).GetValue(0);
                    float deadline=Time.realtimeSinceStartup+75,pressUntil=0,nextObservation=Time.realtimeSinceStartup;bool pressed=false,rollCommandSeen=false;
                    Time.timeScale=2;
                    float neutralUntil=Time.time+3.6f;while(Time.time<neutralUntil&&Time.realtimeSinceStartup<deadline){Queue(pad,0,0,false);yield return null;}
                    while(!(bool)Property(match,"Finished")&&Time.realtimeSinceStartup<deadline)
                    {
                        bool released=(bool)Call(match,"TrailerReleased",0);
                        float remaining=-ring.transform.InverseTransformPoint(body.worldCenterOfMass).z;
                        if(!pressed&&remaining<14&&body.linearVelocity.magnitude>6){pressed=true;pressUntil=Time.realtimeSinceStartup+.18f;}
                        var meter=((Array)FieldNonPublic(match,"trickMeters")).GetValue(0);var net=(Vector3)Property(meter,"CurrentNetDegrees");
                        float steering=released&&net.z<365?1:0;
                        Queue(pad,steering,released ? .8f : 1,Time.realtimeSinceStartup<pressUntil);yield return null;
                        rollCommandSeen|=released&&pad.leftStick.x.ReadValue()>.8f&&(bool)Property(meter,"FreeFlight");
                        if(Time.realtimeSinceStartup>=nextObservation){nextObservation=Time.realtimeSinceStartup+4;Debug.Log("HH10_RING_ASSIGNED_ROLL "+Call(match,"TrickDiagnostic",0));}
                    }
                    Queue(pad,0,0,false);string state=(string)Call(match,"LastTrickAttemptDiagnostic",0);
                    Assert.That((bool)Property(match,"Finished"),Is.True,state);Assert.That(rollCommandSeen,Is.True,"The actual assigned pad must command airborne roll through ordinary inputs. "+state);
                    Assert.That(body.GetInstanceID(),Is.EqualTo(bodyId));Assert.That((int)Call(match,"AttemptsResolved",0),Is.EqualTo(1),state);
                    Assert.That((int)Call(match,"PassedAerialTrickTurns",0),Is.GreaterThan(0),"A complete signed360-degree free-flight turn and real whole-body aperture pass are required. "+state);
                    Assert.That((int)Call(match,"ObjectivesCompleted",0),Is.EqualTo(1),state);Assert.That((double)Call(match,"ResultScore",0),Is.GreaterThan(1350),state);
                    Call(match,"SetupScreen");Queue(pad,0,0,false);yield return null;yield return null;Assert.That((bool)Call(match,"StartMatch"),Is.True);Assert.That((int)Call(match,"PassedAerialTrickTurns",0),Is.Zero);
                }
                finally{Time.timeScale=1;if(match is Component component&&component!=null)Call(match,"SetupScreen");InputSystem.RemoveDevice(pad);InputSystem.settings.backgroundBehavior=oldBackground;}
            }
        }
        static object FieldNonPublic(object target, string name) => target.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(target);
    }
}
#endif
