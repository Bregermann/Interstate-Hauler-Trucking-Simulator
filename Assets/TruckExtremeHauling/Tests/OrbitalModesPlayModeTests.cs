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
    // Normal setup, controller-independent AI inputs and real dynamic cargo.
    // The test only observes poses/contacts/results; it never writes them.
    public sealed class OrbitalModesPlayModeTests
    {
        static Type Runtime(string name)=>Type.GetType("LWS.TruckExtremeHauling."+name+", Assembly-CSharp",true);
        static object Field(object target,string name)=>target.GetType().GetField(name).GetValue(target);
        static object Property(object target,string name)=>target.GetType().GetProperty(name).GetValue(target);
        static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name).Invoke(target,args);
        static string Diagnostic(object match,object original,Array trucks,OrbitalContactProbe contacts)
        {
            var load=(Rigidbody)Field(original,"Body");var thrusters=(Array)Field(match,"thrusters");
            string state="cargoPos="+load.position.ToString("F2")+" COM="+load.worldCenterOfMass.ToString("F2")+" Euler="+load.rotation.eulerAngles.ToString("F1")+" velocity="+load.linearVelocity.ToString("F2")+" spin="+load.angularVelocity.ToString("F2")+" condition="+Property(original,"CargoCondition")+" contact="+contacts.Description;
            for(int i=0;i<((int)Field(Field(match,"setup"),"count"));i++)
            {
                var truck=(Component)trucks.GetValue(i);var body=(Rigidbody)Field(truck,"body");var hitch=truck.GetComponent(Type.GetType("LWS.TruckHitchSports.HitchCoupler, Assembly-CSharp",true));
                state+=" | P"+(i+1)+" pos="+body.position.ToString("F2")+" v="+body.linearVelocity.ToString("F2")+" fuel="+Field(thrusters.GetValue(i),"fuel")+" native="+Property(hitch,"PhysicalConstraintPresent")+" "+Call(hitch,"DiagnosticState",original);
                if((int)Field(match,"option")==1)state+=i<2?" forwardClear="+Call(match,"RelayLeadForwardClear",i):" receiverApproach="+Call(match,"RelayApproachStage",i);
            }
            return state;
        }
        [UnityTest] public IEnumerator RelayPhysicallyTransfersOriginalLoadsBetweenFourPilots()=>NaturalMode(1);
        [UnityTest] public IEnumerator PrecisionRewardsMeasuredStoppedCargoAlignment()=>NaturalMode(2);
        [UnityTest] public IEnumerator RecoveryCapturesDriftingCargoAmongPhysicalDebris()=>NaturalMode(3);

        IEnumerator NaturalMode(int mode)
        {
            var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
            using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"Alpha10OrbitalModes"}))
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckExtremeHauling/Scenes/ZeroGHitchUp.unity",new LoadSceneParameters(LoadSceneMode.Single));
                yield return null;yield return null;
                object match=UnityEngine.Object.FindObjectsByType(Runtime("ZeroGHitchMatch"),FindObjectsSortMode.None).Single();
                match.GetType().GetField("option").SetValue(match,mode);
                var setup=Field(match,"setup");setup.GetType().GetField("count").SetValue(setup,mode==1?4:1);
                Call(match,"NormalizeSetup");Call(match,"AllBots");
                Assert.That((int)Field(setup,"count"),Is.EqualTo(mode==1?4:1));
                var trailers=(Array)Field(match,"trailers");var trucks=(Array)Field(match,"trucks");
                object original=trailers.GetValue(0);var load=(Rigidbody)Field(original,"Body");
                bool receiverCapturedOriginal=false,loadRotated=false;var contacts=load.gameObject.AddComponent<OrbitalContactProbe>();
                object field=Field(match,"recoveryField");Assert.That(field,Is.Not.Null,"The saved orbital recovery field must be authored.");
                var debris=(Rigidbody[])Field(field,"asteroids");var origins=debris.Select(b=>b.position).ToArray();
                float debrisTravel=0;
                Assert.That((bool)Call(match,"StartMatch"),Is.True);
                try
                {
                    Time.timeScale=3;
                    float deadline=Time.realtimeSinceStartup+90,next=Time.realtimeSinceStartup;
                    while(!(bool)Property(match,"Finished")&&Time.realtimeSinceStartup<deadline)
                    {
                        if(mode==1)
                        {
                            var receiver=(Component)trucks.GetValue(2);var coupler=receiver.GetComponent(Type.GetType("LWS.TruckHitchSports.HitchCoupler, Assembly-CSharp",true));
                            receiverCapturedOriginal|=ReferenceEquals(Property(coupler,"Attached"),original)&&((bool)Property(coupler,"PhysicalConstraintPresent"));
                        }
                        if(mode==3)
                        {
                            loadRotated|=load.angularVelocity.magnitude>.03f;
                            for(int n=0;n<debris.Length;n++)debrisTravel=Mathf.Max(debrisTravel,Vector3.Distance(origins[n],debris[n].position));
                            Assert.That(load.useGravity,Is.False);
                        }
                        if(Time.realtimeSinceStartup>=next)
                        {
                            next=Time.realtimeSinceStartup+8;
                            Debug.Log("HH10_ORBITAL_MODE "+mode+" load="+load.position.ToString("F2")+" velocity="+load.linearVelocity.ToString("F2")+" objectives="+Call(match,"ObjectivesCompleted",0)+(mode==1?" relay="+Call(match,"RelayLeg",0):"")+" "+Diagnostic(match,original,trucks,contacts));
                        }
                        yield return null;
                    }
                    Assert.That((bool)Property(match,"Finished"),Is.True,"The ordinary mode clock or actual deliveries must resolve.");
                    Assert.That((int)Call(match,"ObjectivesCompleted",0),Is.GreaterThan(0),"The original cargo must be physically delivered; timeout is not acceptance. "+Diagnostic(match,original,trucks,contacts));
                    if(mode==1)
                    {
                        Assert.That(receiverCapturedOriginal,Is.True,"Pilot3 must physically recouple the SAME original trailer released by pilot1.");
                        Assert.That((int)Call(match,"RelayLeg",0),Is.EqualTo(2));
                        Assert.That((int)Call(match,"ObjectivesCompleted",2),Is.GreaterThan(0));
                        Assert.That((double)Call(match,"ResultScore",0),Is.EqualTo((double)Call(match,"ResultScore",2)));
                    }
                    if(mode==2){Assert.That((float)Call(match,"PrecisionBonus",0),Is.GreaterThan(0),"A controlled actual docking must bank a measured precision bonus.");Assert.That((float)Call(match,"DockErrorMetres",0),Is.LessThan(6));}
                    if(mode==3){Assert.That(loadRotated,Is.True);Assert.That(debrisTravel,Is.GreaterThan(2),"Real asteroid bodies must drift, not remain decorative.");}
                    string id=(string)Property(match,"MatchId");Call(match,"SetupScreen");yield return null;
                    Assert.That((bool)Call(match,"StartMatch"),Is.True);
                    Assert.That((string)Property(match,"MatchId"),Is.Not.EqualTo(id));
                    Assert.That((int)Call(match,"ObjectivesCompleted",0),Is.Zero);
                    if(mode==1)Assert.That((int)Call(match,"RelayLeg",0),Is.Zero);
                    yield return new WaitForSeconds(4);
                    Assert.That((bool)Property(match,"Running"),Is.True);
                }
                finally{Time.timeScale=1;Call(match,"SetupScreen");}
            }
        }
    }
    public sealed class OrbitalContactProbe:MonoBehaviour
    {
        string last="none";float observedAt;
        public string Description=>last+" age="+(Time.time-observedAt).ToString("F1");
        void OnCollisionEnter(Collision collision)=>Observe(collision);
        void OnCollisionStay(Collision collision)=>Observe(collision);
        void Observe(Collision collision)
        {if(collision.contactCount==0)return;var hit=collision.GetContact(0);last=collision.collider.name+" point="+hit.point.ToString("F2")+" normal="+hit.normal.ToString("F2")+" impulse="+collision.impulse.magnitude.ToString("F0");observedAt=Time.time;}
    }
}
#endif
