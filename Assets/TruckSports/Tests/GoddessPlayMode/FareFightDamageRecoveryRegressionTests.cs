#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;

public sealed class FareFightDamageRecoveryRegressionTests
{
    static object Field(object target, string name) => target.GetType().GetField(name).GetValue(target);
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method).Invoke(target, args);
    static object Engine(Component match, int slot)
    {
        var truck = ((Array)Field(match, "trucks")).GetValue(slot);
        var vehicle = Field(Field(truck, "tractorMotor"), "vehicle");
        return Field(Field(vehicle, "powertrain"), "engine");
    }
    static void Damage(object engine, float value) => engine.GetType().GetProperty("Damage").SetValue(engine, value);
    static float Damage(object engine) => (float)engine.GetType().GetProperty("Damage").GetValue(engine);
    static IEnumerator Load()
    {
        EditorSceneManager.LoadSceneInPlayMode("Assets/TruckFareFight/Scenes/TruckFareFight.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null; yield return null;
    }
    static Component Match() => (Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckFareFight.FareFightMatch, Assembly-CSharp", true), FindObjectsSortMode.None).Single();
    static void ClearWeather(Component match)
    {
        var catalog = Field(match, "presets");
        foreach (var entry in (Array)Field(catalog, "entries")) if ((string)Field(entry, "displayName") == "Clear") { Call(catalog, "Select", Field(entry, "id")); break; }
    }

    [UnityTest]
    public IEnumerator RematchRestoresActualNwhEngineHealthRatherThanOnlyMovingTheBody()
    {
        var prefs = Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true);
        using ((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null, new object[] { "FareDamageRematch" }))
        {
            yield return Load(); var match = Match(); ClearWeather(match); Call(match, "AllBots");
            Assert.That((bool)Call(match, "StartMatch"), Is.True); yield return null;
            var engine = Engine(match, 2); Damage(engine, 1);
            Assert.That(Damage(engine), Is.EqualTo(1));
            Call(match, "SetupScreen"); Assert.That((bool)Call(match, "StartMatch"), Is.True); yield return null;
            Assert.That(Damage(engine), Is.Zero, "A moved truck with a destroyed NWH engine cannot compete in a rematch.");
            Call(match, "SetupScreen");
        }
    }

    [UnityTest]
    public IEnumerator ExistingWreckRecoveryRestoresTheNwhPowertrain()
    {
        var prefs = Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true);
        using ((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null, new object[] { "FareDamageRecovery" }))
        {
            yield return Load(); var match = Match(); ClearWeather(match); Call(match, "AllBots");
            Assert.That((bool)Call(match, "StartMatch"), Is.True); yield return null;
            var engine = Engine(match, 0); Damage(engine, 1);
            Call(match, "DestroyTruck", 0);
            yield return new WaitForSeconds(3.4f);
            Assert.That((int)match.GetType().GetProperty("Recoveries").GetValue(match), Is.GreaterThan(0));
            Assert.That(Damage(engine), Is.Zero, "Recovery must repair the actual vendor engine, not leave it permanently stalled after respawn.");
            Call(match, "SetupScreen");
        }
    }

    [UnityTest]
    public IEnumerator DestroyedAiPowertrainUsesAnnouncedRecoveryAndKeepsPassengerDestination() => DestroyedCarrierRecovery("AI");

    [UnityTest]
    public IEnumerator DestroyedHumanPowertrainUsesTheSameRecoveryRules() => DestroyedCarrierRecovery("Keyboard");

    static IEnumerator DestroyedCarrierRecovery(string source)
    {
        var prefs = Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true);
        using ((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null, new object[] { "FareAutomaticWreck" + source }))
        {
            Component match = null; Keyboard keyboard = null; GameObject isolation = null;
            var background = InputSystem.settings.backgroundBehavior;
            try
            {
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                isolation = new GameObject("Fare recovery fixture physical-input isolation"); UnityEngine.Object.DontDestroyOnLoad(isolation);
                isolation.AddComponent(Type.GetType("LWS.TruckSports.SportsProbeInputIsolation, Assembly-CSharp", true));
                yield return Load(); match = Match(); ClearWeather(match); Call(match, "AllBots");
                var player = ((Array)Field(Field(match, "setup"), "players")).GetValue(0);
                var input = player.GetType().GetField("source"); input.SetValue(player, Enum.Parse(input.FieldType, source));
                if (source == "Keyboard")
                {
                    keyboard = InputSystem.AddDevice<Keyboard>(); InputSystem.EnableDevice(keyboard); keyboard.MakeCurrent();
                    string[] fields = { "forward", "left", "back", "right" }; Key[] keys = { Key.W, Key.A, Key.S, Key.D };
                    for (int key = 0; key < fields.Length; key++) player.GetType().GetField(fields[key]).SetValue(player, keys[key]);
                }
                object[] validation = { null };
                Assert.That((bool)match.GetType().GetMethod("ValidateSources").Invoke(match, validation), Is.True, "Fixture source validation: " + validation[0]);
                Assert.That((bool)Call(match, "Ready", 0), Is.True, "Fixture keyboard must be the enabled current Input System device.");
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                Time.timeScale = 4; yield return new WaitForSeconds(3.2f);
                // Deliberate pickup-volume fixture, not a claim of human driving.
                var rules = Field(match, "rules");
                int pickup = (int)rules.GetType().GetProperty("PickupLocation").GetValue(rules);
                int destination = (int)rules.GetType().GetProperty("Destination").GetValue(rules);
                var stop = ((Array)Field(Field(match, "district"), "stops")).GetValue(pickup);
                var truck = ((Array)Field(match, "trucks")).GetValue(0);
                Call(truck, "Prepare", (Vector3)Field(stop, "position") + Vector3.up * .3f, Quaternion.identity, true);
                yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                Assert.That((int)rules.GetType().GetProperty("Carrier").GetValue(rules), Is.Zero);
                Damage(Engine(match, 0), 1);
                yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                Assert.That((int)rules.GetType().GetProperty("Carrier").GetValue(rules), Is.EqualTo(-1));
                Assert.That((int)rules.GetType().GetProperty("Resets").GetValue(rules), Is.EqualTo(1));
                Assert.That((int)rules.GetType().GetProperty("Destination").GetValue(rules), Is.EqualTo(destination));
                Assert.That((int)rules.GetType().GetProperty("Deliveries").GetValue(rules), Is.Zero);
                yield return new WaitForSeconds(2.2f);
                Assert.That((int)match.GetType().GetProperty("Recoveries").GetValue(match), Is.EqualTo(1));
                Assert.That(Damage(Engine(match, 0)), Is.Zero);
                Assert.That((bool)truck.GetType().GetProperty("Powered").GetValue(truck), Is.True);
            }
            finally { Time.timeScale = 1; if (match != null) Call(match, "SetupScreen"); if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard); if (isolation != null) UnityEngine.Object.Destroy(isolation); InputSystem.settings.backgroundBehavior = background; }
        }
    }

    [UnityTest]
    public IEnumerator ActualNwhBackingCreatesTurningClearanceBeforeForwardRetry()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"FareNwhReverseClearance"}))
        {
            Component match=null;GameObject wall=null;
            try
            {
                yield return Load();match=Match();ClearWeather(match);Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return null;
                // Isolate a real NWH truck at a static wall; no match scoring,
                // teleport recovery, engine healing or artificial drive forces.
                ((Behaviour)match).enabled=false;((Component)Field(match,"traffic")).gameObject.SetActive(false);((Component)Field(match,"pedestrians")).gameObject.SetActive(false);
                var trucks=(Array)Field(match,"trucks");for(int i=1;i<4;i++)Call(trucks.GetValue(i),"Park",new Vector3(100,-100-i*15,0));
                var truck=trucks.GetValue(0);Call(truck,"Prepare",new Vector3(0,.3f,-100),Quaternion.identity,true);var body=(Rigidbody)Field(truck,"body");Physics.SyncTransforms();
                float front=float.NegativeInfinity;foreach(var collider in ((Component)truck).GetComponentsInChildren<Collider>())if(collider.enabled&&!collider.isTrigger)front=Mathf.Max(front,collider.bounds.max.z);
                wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="NWH reverse clearance fixture wall";wall.transform.position=new Vector3(0,2,front+.3f);wall.transform.localScale=new Vector3(30,4,.3f);Physics.SyncTransforms();
                var route=((Array)Field(match,"routes")).GetValue(0);Call(route,"Reset");route.GetType().GetField("minimumReverseDistance")?.SetValue(route,3f);Call(route,"DirectGoal",body.position,body.position+Vector3.forward*60);
                Time.timeScale=3;float deadline=Time.realtimeSinceStartup+10;bool backing=false;Vector3 began=default;float clearance=0;int samples=0;
                while(Time.realtimeSinceStartup<deadline)
                {
                    var drive=(Vector2)Call(route,"Drive",body,((Component)truck).transform,1,Time.fixedDeltaTime,1f,null,null);Call(truck,"TickDrive",drive);
                    bool reverse=(bool)route.GetType().GetProperty("Reversing").GetValue(route);
                    if(reverse&&!backing){backing=true;began=body.position;}
                    if(backing){clearance=Mathf.Max(clearance,began.z-body.position.z);samples++;if(!reverse)break;}
                    yield return new WaitForFixedUpdate();
                }
                Assert.That(backing,Is.True);Assert.That(samples,Is.GreaterThan(5));
                TestContext.WriteLine("Actual native backward clearance before first forward retry="+clearance);
                Assert.That(clearance,Is.GreaterThan(2.8f),"A full-size NWH truck needs physical backing clearance, including its automatic DNR shift delay, before retrying a blocked turn.");
            }
            finally{Time.timeScale=1;if(wall!=null)UnityEngine.Object.Destroy(wall);if(match!=null){Call(match,"SetupScreen");((Behaviour)match).enabled=true;}}
        }
    }

    [UnityTest]
    public IEnumerator NaturalMatchRecordsActualPowertrainStateDuringProlongedRecovery()
    {
        var prefs = Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp", true);
        using ((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null, new object[] { "FareNaturalRecovery" }))
        {
            Component match = null; var log = new StringBuilder();
            string path = "Logs/TruckSports/PlaytestRepair/fare-natural-recovery-state.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            try
            {
                yield return Load(); match = Match(); ClearWeather(match); Call(match, "AllBots"); Field(match, "setup").GetType().GetField("count").SetValue(Field(match, "setup"), 4);
                foreach (var player in (Array)Field(Field(match, "setup"), "players"))
                { var difficulty = player.GetType().GetField("difficulty"); difficulty.SetValue(player, Enum.Parse(difficulty.FieldType, "Normal")); }
                match.GetType().GetField("option").SetValue(match, 2);
                UnityEngine.Random.InitState(20261021);
                match.GetType().GetField("validationSeed").SetValue(match,616736539);
                Assert.That((bool)Call(match, "StartMatch"), Is.True); yield return null;
                log.AppendLine("REAL MATCH seed=" + Field(match, "matchSeed") + "; normal AI, first-to-ten, four real NWH vehicles. Diagnostic, not complete-match acceptance.");
                var trucks = (Array)Field(match, "trucks"); var routes = (Array)Field(match, "routes");
                var prior = new Vector3[4]; var moved = new float[4];
                for (int i = 0; i < 4; i++) { prior[i] = ((Rigidbody)Field(trucks.GetValue(i), "body")).position; moved[i] = Time.time; }
                Time.timeScale = 3; float deadline = Time.realtimeSinceStartup + 120, next = 0; bool observed = false;
                while (Time.realtimeSinceStartup < deadline && !(bool)match.GetType().GetProperty("Finished").GetValue(match))
                {
                    for (int i = 0; i < 4; i++)
                    {
                        var body = (Rigidbody)Field(trucks.GetValue(i), "body");
                        if ((body.position - prior[i]).sqrMagnitude > 4) { prior[i] = body.position; moved[i] = Time.time; }
                        int reversals = (int)routes.GetValue(i).GetType().GetProperty("RecoveryStarts").GetValue(routes.GetValue(i));
                        if (reversals >= 5 && body.linearVelocity.magnitude < .5f && Time.time - moved[i] > 16) observed = true;
                    }
                    if (observed || Time.time >= next)
                    {
                        next = Time.time + 8; log.AppendLine("TIME " + Time.time.ToString("F2") + " observedProlonged=" + observed);
                        for (int i = 0; i < 4; i++)
                        {
                            var truck = trucks.GetValue(i); var body = (Rigidbody)Field(truck, "body"); var vehicle = Field(Field(truck, "tractorMotor"), "vehicle");
                            var powertrain = Field(vehicle, "powertrain"); var engine = Field(powertrain, "engine"); var transmission = Field(powertrain, "transmission");
                            log.AppendLine("slot=" + i + " position=" + body.position.ToString("F3") + " velocity=" + body.linearVelocity.ToString("F3") + " facing=" + body.rotation.eulerAngles + " command=" + Field(truck, "command") + " engineDamage=" + Damage(engine) + " running=" + engine.GetType().GetProperty("IsRunning").GetValue(engine) + " RPM=" + engine.GetType().GetProperty("OutputRPM").GetValue(engine) + " gear=" + transmission.GetType().GetProperty("Gear").GetValue(transmission) + " transmissionDamage=" + transmission.GetType().GetProperty("Damage").GetValue(transmission) + " torque=" + Field(engine, "outputTorque") + " reversals=" + routes.GetValue(i).GetType().GetProperty("RecoveryStarts").GetValue(routes.GetValue(i)));
                            var input=Field(vehicle,"input");log.AppendLine(" enabled="+((Behaviour)vehicle).enabled+" sleeping="+body.IsSleeping()+" constraints="+body.constraints+" powertrain="+powertrain.GetType().GetProperty("IsActive").GetValue(powertrain)+" actualThrottle="+input.GetType().GetProperty("Throttle").GetValue(input)+" actualBrakes="+input.GetType().GetProperty("Brakes").GetValue(input)+" handbrake="+input.GetType().GetProperty("Handbrake").GetValue(input));
                            foreach(var component in (IEnumerable)Field(powertrain,"wheels")){var wheel=(Component)Field(component,"wheelUAPI");log.AppendLine(" wheel="+wheel.name+" grounded="+wheel.GetType().GetProperty("IsGrounded").GetValue(wheel)+" motor="+wheel.GetType().GetProperty("MotorTorque").GetValue(wheel)+" brake="+wheel.GetType().GetProperty("BrakeTorque").GetValue(wheel)+" damage="+wheel.GetType().GetProperty("Damage").GetValue(wheel));}
                        }
                        File.WriteAllText(path, log.ToString());
                    }
                    if (observed) break;
                    yield return null;
                }
                log.AppendLine("DIAGNOSTIC FINISHED observedProlonged=" + observed + "; no scoring/position/health manipulation performed."); File.WriteAllText(path, log.ToString());
                Debug.Log("Fare natural recovery diagnostic recorded: " + observed);
            }
            finally { Time.timeScale = 1; if (match != null) Call(match, "SetupScreen"); }
        }
    }
}
#endif
