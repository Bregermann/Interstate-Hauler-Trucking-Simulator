#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckWaterSports.Tests
{
    public sealed class PaddleChaosHazardsPlayModeTests
    {
        static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
        static object Field(object value, string name) => value.GetType().GetField(name).GetValue(value);
        static object Property(object value, string name) => value.GetType().GetProperty(name).GetValue(value);
        static object Call(object value, string name, params object[] args) => value.GetType().GetMethod(name).Invoke(value, args);

        [UnityTest]
        public IEnumerator AuthoredChaosTimberUsesNativeBuoyancyTethersAndModeSafeReset()
        {
            var lease = (IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation")
                .Invoke(null, new object[] { "PaddleChaosTimber" });
            float scale = Time.timeScale, fixedStep = Time.fixedDeltaTime; Component match = null;
            try
            {
                Time.timeScale = 1;
                var weather = AssetDatabase.LoadAssetAtPath("Assets/TruckSports/Settings/SportsWeatherPresets.asset", Runtime("LWS.TruckSports.SportsWeatherPresets"));
                bool selected = false;
                foreach (var entry in (Array)Field(weather, "entries")) if ((string)Field(entry, "displayName") == "Clear")
                { Call(weather, "Select", (string)Field(entry, "id")); selected = true; break; }
                Assert.That(selected, Is.True);
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckWaterSports/Scenes/PaddlePals.unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null;
                match = (Component)UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckWaterSports.PaddlePalsMatch"), FindObjectsSortMode.None)[0];
                var hazard = match.GetComponent(Runtime("LWS.TruckWaterSports.PaddleChaosHazards"));
                Assert.That(hazard, Is.Not.Null, "Run the explicit saved-scene Chaos installer before this acceptance check.");
                var logs = (Rigidbody[])Field(hazard, "logs"); var saved = new Vector3[logs.Length];
                Assert.That(logs.Length, Is.EqualTo(4));
                for (int i = 0; i < logs.Length; i++) saved[i] = logs[i].position;
                var setup = Field(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 4); Call(match, "NormalizeSetup"); Call(match, "AllBots");
                match.GetType().GetField("option").SetValue(match, 2);
                Assert.That((bool)Call(match, "StartMatch"), Is.True);
                string firstMatch = (string)Property(match, "MatchId");
                bool wet = false, tension = false; float movement = 0;
                Time.timeScale = 4; float deadline = Time.realtimeSinceStartup + 20;
                while ((float)Property(match, "Clock") < 16 && Time.realtimeSinceStartup < deadline)
                {
                    foreach (var body in logs)
                    {
                        var joint = body.GetComponent<SpringJoint>(); Assert.That(joint, Is.Not.Null); Assert.That(joint.connectedBody, Is.Null);
                        tension |= joint.currentForce.sqrMagnitude > 1;
                        var floating = body.GetComponent(Runtime("LWS.TruckSports.SportsFloatingBody"));
                        wet |= (uint)Property(floating, "WetTriangles") > 0;
                    }
                    for (int i = 0; i < logs.Length; i++) movement = Mathf.Max(movement, Vector3.Distance(logs[i].position, saved[i]));
                    yield return null;
                }
                Assert.That((float)Property(match, "Clock"), Is.GreaterThanOrEqualTo(16));
                Assert.That(wet && tension, Is.True, "Real Archimeds wet hulls and native tether forces are required.");
                Assert.That(movement, Is.GreaterThan(.2f), "The authored current must move actual dynamic timber.");
                int contacts = (int)Property(hazard, "ActualCanoeContacts");

                Time.timeScale = 1; Call(match, "SetupScreen");
                match.GetType().GetField("option").SetValue(match, 0); Assert.That((bool)Call(match, "StartMatch"), Is.True);
                yield return new WaitForFixedUpdate(); yield return null;
                Assert.That(((Transform)Field(hazard, "equipment")).gameObject.activeSelf, Is.False, "Default Sprint must not gain Chaos equipment.");
                foreach (var body in logs) Assert.That(body.isKinematic, Is.True);

                Call(match, "SetupScreen"); match.GetType().GetField("option").SetValue(match, 2);
                Assert.That((bool)Call(match, "StartMatch"), Is.True); yield return new WaitForFixedUpdate(); yield return null;
                Assert.That((string)Property(match, "MatchId"), Is.Not.EqualTo(firstMatch));
                Assert.That((int)Property(hazard, "ActualCanoeContacts"), Is.Zero);
                for (int i = 0; i < logs.Length; i++) Assert.That(Vector3.Distance(logs[i].position, saved[i]), Is.LessThan(.5f), "Ordinary rematch restores saved physical equipment.");
                Assert.That(Time.fixedDeltaTime, Is.EqualTo(fixedStep));
                TestContext.WriteLine("PADDLE_CHAOS_TIMBER software_ai_only=true wet_hulls=true native_tether_force=true movement=" + movement + " actual_contact_events=" + contacts + "; no contacts, outcomes or scores were injected; full Chaos match/human acceptance is separate.");
            }
            finally { if (match != null) Call(match, "SetupScreen"); Time.timeScale = scale; lease.Dispose(); }
        }
        [UnityTest]
        public IEnumerator NaturalChaosCrewsMakePhysicalOpponentContactFinishAndRematch()
        {
            var lease=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"PaddleChaosNaturalCompetition"});
            float scale=Time.timeScale,fixedStep=Time.fixedDeltaTime;Component match=null;
            try
            {
                Time.timeScale=1;
                var weather=AssetDatabase.LoadAssetAtPath("Assets/TruckSports/Settings/SportsWeatherPresets.asset",Runtime("LWS.TruckSports.SportsWeatherPresets"));
                bool selected=false;
                foreach(var entry in (Array)Field(weather,"entries"))if((string)Field(entry,"displayName")=="Clear")
                {Call(weather,"Select",(string)Field(entry,"id"));selected=true;break;}
                Assert.That(selected,Is.True);
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckWaterSports/Scenes/PaddlePals.unity",new LoadSceneParameters(LoadSceneMode.Single));
                yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckWaterSports.PaddlePalsMatch"),FindObjectsSortMode.None)[0];
                Assert.That(match.GetComponent(Runtime("LWS.TruckWaterSports.PaddleChaosHazards")),Is.Not.Null,"Use the real saved Chaos venue.");
                var setup=Field(match,"setup");setup.GetType().GetField("count").SetValue(setup,4);Call(match,"NormalizeSetup");Call(match,"AllBots");
                var players=(Array)Field(setup,"players");
                for(int i=0;i<4;i++)
                {
                    var player=players.GetValue(i);var skill=player.GetType().GetField("difficulty");
                    skill.SetValue(player,Enum.ToObject(skill.FieldType,i<2?2:0));players.SetValue(player,i);
                }
                match.GetType().GetField("option").SetValue(match,2);
                float duration=(float)Field(match,"matchDuration");
                Assert.That((bool)Call(match,"StartMatch"),Is.True);string first=(string)Property(match,"MatchId");
                var vessels=(Array)Field(match,"vessels");bool wet=false,contact=false;int credited=0;
                Time.timeScale=4;float deadline=Time.realtimeSinceStartup+100;
                while(!(bool)Property(match,"Finished")&&Time.realtimeSinceStartup<deadline)
                {
                    foreach(var vessel in vessels)
                    {
                        wet|=(uint)Property(Field(vessel,"flotation"),"WetTriangles")>0;
                        contact|=(int)Property(vessel,"OpponentBumps")>0;
                    }
                    yield return null;
                }
                Time.timeScale=1;
                string observed="clock="+Property(match,"Clock")+" gates="+string.Join(",",(int[])Field(match,"Gates"))+" finishes="+string.Join(",",(float[])Field(match,"FinishTimes"));
                bool finish=false;foreach(float time in (float[])Field(match,"FinishTimes"))finish|=time>=0;
                Assert.That((bool)Property(match,"Finished"),Is.True,observed);
                Assert.That((float)Field(match,"matchDuration"),Is.EqualTo(duration),"The real configured competition length must remain unchanged.");
                Assert.That(wet&&contact,Is.True,"Real wet hulls and >2m/s native opponent collision events are required. "+observed);
                Assert.That(finish,Is.True,"At least one crew must complete the actual authored river, not merely expire the timer. "+observed);
                int attempts=0;
                foreach(var vessel in vessels)
                {
                    credited+=(int)Property(vessel,"CreditedBumps");
                    Assert.That((int)Call(vessel,"StrokesAtStation",0),Is.GreaterThan(0));
                    Assert.That((int)Call(vessel,"StrokesAtStation",1),Is.GreaterThan(0));
                }
                for(int slot=0;slot<4;slot+=2)attempts+=(int)Call(match,"ChaosInterceptionAttempts",slot);
                Assert.That(attempts,Is.GreaterThan(0),"Actual opponent-directed paddle decisions must occur.");
                var scores=(float[])Field(match,"Scores");
                Assert.That(scores[0],Is.EqualTo(scores[1]));Assert.That(scores[2],Is.EqualTo(scores[3]),"Each two-station crew shares its genuine canoe result.");
                bool winner=false;for(int i=0;i<4;i++)winner|=(bool)Call(match,"ResultWinner",i);
                Assert.That(winner,Is.True,"Competition must produce an eligible measured result.");
                Assert.That(Property(Field(match,"menus"),"Screen").ToString(),Is.EqualTo("Result"));
                TestContext.WriteLine("PADDLE_CHAOS_NATURAL software_ai_only=true native_opponent_contact=true credited_bumps="+credited+" interception_attempts="+attempts+" "+observed+"; no hull poses, contacts, scores, gates or outcomes injected.");
                Call(match,"SetupScreen");yield return null;Assert.That((bool)Call(match,"StartMatch"),Is.True);
                Assert.That((string)Property(match,"MatchId"),Is.Not.EqualTo(first));
                foreach(var vessel in vessels){Assert.That((int)Property(vessel,"OpponentBumps"),Is.Zero);Assert.That((int)Property(vessel,"CreditedBumps"),Is.Zero);}
                Assert.That((float[])Field(match,"Scores"),Is.All.EqualTo(0));
                Assert.That((int)Call(match,"ChaosInterceptionAttempts",0),Is.Zero);Assert.That((int)Call(match,"ChaosEvasionAttempts",2),Is.Zero);
                Time.timeScale=4;yield return new WaitForSeconds(4);Time.timeScale=1;
                Assert.That((bool)Property(match,"Running"),Is.True);Assert.That(Time.fixedDeltaTime,Is.EqualTo(fixedStep));
            }
            finally{if(match!=null)Call(match,"SetupScreen");Time.timeScale=scale;lease.Dispose();}
        }

    }
}
#endif
