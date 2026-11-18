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
    public sealed class ThinIceRescuePlayModeTests
    {
        static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
        static object Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(target);
        static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
        static object Call(object target, string name, params object[] arguments) => target.GetType().GetMethod(name).Invoke(target, arguments);
        static double ClosedVolume(Mesh mesh)
        {
            var vertices = mesh.vertices; var triangles = mesh.triangles; double volume = 0;
            for(int i=0;i<triangles.Length;i+=3)volume+=Vector3.Dot(vertices[triangles[i]],Vector3.Cross(vertices[triangles[i+1]],vertices[triangles[i+2]]))/6d;
            return Math.Abs(volume);
        }

        [UnityTest]
        public IEnumerator NaturalConvoyRescueUsesFractureCableBuoyancyAndShoreRecovery()
        {
            var lease = (IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null, new object[] { "ThinIceActualRescue" });
            float oldScale = Time.timeScale, fixedStep = Time.fixedDeltaTime;
            Component match = null;
            try
            {
                Time.timeScale = 1;
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckExtremeHauling/Scenes/IceRoadReckoning.unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null;
                var matches = UnityEngine.Object.FindObjectsByType(Runtime("LWS.TruckExtremeHauling.ThinIceMatch"), FindObjectsSortMode.None);
                Assert.That(matches.Length, Is.EqualTo(1)); match = (Component)matches[0];
                var rescue = Field(match, "rescue"); Assert.That(rescue, Is.Not.Null, "Use the actual saved rescue incident; no test-time authoring.");
                var frontLift=Field(rescue,"frontLift");Assert.That(frontLift,Is.Not.Null,"Install the saved physical recovery gantry before this natural acceptance run.");
                var rail=(ConfigurableJoint)Field(frontLift,"translationRail");
                Assert.That(rail,Is.Not.Null,"Run the explicit saved-scene recovery rail upgrade; no test-time scene authoring.");
                var carrier=Field(frontLift,"carrier");var trolley=(Rigidbody)Field(carrier,"trolley");var hook=Field(frontLift,"hook");
                Vector3 savedTrolley=trolley.position;
                match.GetType().GetField("option").SetValue(match, 3);
                var setup = Field(match, "setup"); setup.GetType().GetField("count").SetValue(setup, 1);
                Call(match, "NormalizeSetup"); Call(match, "AllBots");
                var weather = Field(match, "presentationWeather"); if (weather != null && Field(weather,"presets") != null) Call(Field(weather,"presets"),"Select","9aad5ff5dfa09ce468205f676714f891");
                var cargo = Field(rescue, "strandedCargo"); var body = (Rigidbody)Field(cargo,"Body");
                var floating = Field(rescue,"cargoFloat"); var incident = Field(rescue,"incidentIce");
                var proxy = (Mesh)Field(floating,"proxy");
                Assert.That(Mathf.Abs(proxy.bounds.center.y),Is.LessThan(10),"Displacement must be body-local, not a stale prefab-world AABB.");
                Assert.That(ClosedVolume(proxy)*1000,Is.GreaterThan(body.mass),"Actual sealed cargo volume must support its native mass.");
                var truckComponent = (Component)((Array)Field(match,"trucks")).GetValue(0);
                var mainFloat = ((Array)Field(rescue,"playerFloats")).GetValue(0);
                double tractorVolume = ClosedVolume((Mesh)Field(mainFloat,"proxy")), tractorMass = 0;
                foreach(var part in truckComponent.GetComponentsInChildren<Rigidbody>(true))tractorMass+=part.mass;
                var auxiliary = (Array)Field(rescue,"auxiliaryFloats");int cabBodies=0;
                foreach(var hydro in auxiliary)
                {
                    var hydroBody=(Rigidbody)Field(hydro,"body");
                    if(hydroBody.transform.IsChildOf(truckComponent.transform)){tractorVolume+=ClosedVolume((Mesh)Field(hydro,"proxy"));cabBodies++;}
                }
                Assert.That(cabBodies,Is.GreaterThan(0),"The native jointed cab has its own sealed collider and Rigidbody; do not omit its displacement.");
                Assert.That(tractorVolume*1000,Is.GreaterThan(tractorMass),"Actual cab plus chassis sealed volume must support the unchanged assembly mass.");
                Debug.Log("CONVOY_RESCUE_HULL_ACTUAL cargoLocal="+proxy.bounds+" cargoVolume="+ClosedVolume(proxy)+" cargoMass="+body.mass+" tractorVolume="+tractorVolume+" tractorMass="+tractorMass+" childHydroBodies="+cabBodies);
                Vector3 spawn = body.position;
                bool broke = false, immersed = false, wet = false, hooked = false, lifted=false,nativeCoupled=false,suspendedTranslated=false,seenSuspended=false;
                Vector3 firstSuspendedCargo=Vector3.zero;
                float peakCableForce = 0, nextObservation = 0;
                Assert.That((bool)Call(match,"StartMatch"),Is.True);
                string firstId = (string)Property(match,"MatchId");
                Time.timeScale = 3; float deadline = Time.realtimeSinceStartup + 90;
                while (!(bool)Property(match,"Finished") && Time.realtimeSinceStartup < deadline)
                {
                    broke |= (bool)Property(incident,"Broken"); immersed |= body.position.y < -.6f;
                    wet |= (uint)Property(floating,"WetTriangles") > 0;
                    lifted|=(bool)Property(frontLift,"Lifted");
                    if((bool)Property(hook,"Holding")&&Property(hook,"Captured")==body)
                    {
                        if(!seenSuspended){firstSuspendedCargo=body.worldCenterOfMass;seenSuspended=true;}
                        suspendedTranslated|=trolley.position.z-savedTrolley.z>8.5f&&body.worldCenterOfMass.z-firstSuspendedCargo.z>8.5f;
                    }
                    foreach(var truck in (Array)Field(match,"trucks")){var hitch=((Component)truck).GetComponent(Runtime("LWS.TruckHitchSports.HitchCoupler"));if(hitch!=null&&Property(hitch,"Attached")==cargo&&(bool)Property(hitch,"PhysicalConstraintPresent"))nativeCoupled=true;}
                    foreach (var joint in (SpringJoint[])Field(rescue,"cables"))
                        if (joint != null && joint.connectedBody == body) { hooked = true; peakCableForce = Mathf.Max(peakCableForce,joint.currentForce.magnitude); }
                    float clock = (float)Property(match,"RescueClock");
                    if (clock >= nextObservation)
                    {
                        nextObservation = clock + 50;
                        var truck = ((Array)Field(match,"trucks")).GetValue(0); var tractorBody = (Rigidbody)Field(truck,"body");
                        uint cabWet=0;foreach(var hydro in auxiliary){var hydroBody=(Rigidbody)Field(hydro,"body");if(hydroBody.transform.IsChildOf(truckComponent.transform))cabWet+=(uint)Property(hydro,"WetTriangles");}
                        Debug.Log("CONVOY_RESCUE_ACTUAL clock="+clock+" cargo="+body.position+" cargoVelocity="+body.linearVelocity+" tractor="+tractorBody.position+" tractorVelocity="+tractorBody.linearVelocity+" cargoHydroSteps="+Property(floating,"Steps")+" cargoWet="+Property(floating,"WetTriangles")+" cabWet="+cabWet+" iceBroken="+broke+" immersed="+immersed+" wet="+wet+" hooked="+hooked+" towed="+((float[])Field(rescue,"TowedMetres"))[0]+" recovered="+Property(rescue,"CargoRecovered")+" "+Call(rescue,"DiagnosticState",0));
                    }
                    yield return null;
                }
                Time.timeScale = 1;
                string observed = "clock="+Property(match,"RescueClock")+" cargo="+body.position+" broke="+broke+" immersed="+immersed+" wet="+wet+" hooked="+hooked+" peakCableForce="+peakCableForce+" towed="+((float[])Field(rescue,"TowedMetres"))[0]+" "+Call(rescue,"DiagnosticState",0);
                Assert.That((bool)Property(match,"Finished"),Is.True,observed);
                Assert.That(broke && immersed && wet && hooked,Is.True,"Actual stress failure, ingress, vendor buoyancy and native cable must occur. "+observed);
                Assert.That(peakCableForce,Is.GreaterThan(100),observed);
                Assert.That(lifted&&suspendedTranslated&&nativeCoupled,Is.True,"Actual hoist lift, latched suspended northbound transport and a real strict native fifth-wheel constraint must precede shore transport. "+observed);
                Assert.That(((float[])Field(rescue,"TowedMetres"))[0],Is.GreaterThan(20),observed);
                Assert.That((bool)Property(rescue,"CargoRecovered"),Is.True,observed);
                Assert.That((bool)Property(match,"CooperativeSuccess"),Is.True,observed);
                Assert.That((bool)Call(match,"ResultWinner",0),Is.False,"Cooperative rescue must not invent an individual winner.");
                Call(match,"SetupScreen"); yield return null;
                Assert.That((bool)Call(match,"StartMatch"),Is.True);
                Assert.That((string)Property(match,"MatchId"),Is.Not.EqualTo(firstId));
                Assert.That((bool)Property(rescue,"CargoRecovered"),Is.False);
                Assert.That((bool)Property(rescue,"CooperativeSuccess"),Is.False);
                Assert.That(((float[])Field(rescue,"TowedMetres"))[0],Is.Zero);
                Assert.That(((int[])Field(rescue,"Contributions"))[0],Is.Zero);
                Assert.That((bool)Property(frontLift,"RailPresented"),Is.False);Assert.That((float)Property(frontLift,"SuspendedTravel"),Is.Zero);
                Assert.That(Vector3.Distance(trolley.position,savedTrolley),Is.LessThan(.02f),"Only the normal round-boundary reset restores the rail trolley.");
                Assert.That((bool)Property(frontLift,"Lifted"),Is.False);Assert.That((bool)Property(frontLift,"NativeCoupled"),Is.False);Assert.That((int)Property(frontLift,"Owner"),Is.EqualTo(-1));
                Assert.That(Vector3.Distance(body.position,spawn),Is.LessThan(.02f),"Round-boundary reset must restore the original NPC load.");
                Time.timeScale = 3; yield return new WaitForSeconds(4);
                Assert.That((bool)Property(match,"Running"),Is.True);
                Assert.That(Time.fixedDeltaTime,Is.EqualTo(fixedStep));
                TestContext.WriteLine("CONVOY_RESCUE_NATURAL software_ai_only=true "+observed+" cooperative=true rematch=fresh");
            }
            finally
            {
                if(match!=null)Call(match,"SetupScreen");
                Time.timeScale=oldScale;lease.Dispose();
            }
        }
    }
}
#endif
