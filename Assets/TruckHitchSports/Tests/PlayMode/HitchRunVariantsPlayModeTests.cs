#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public sealed class HitchRunVariantsPlayModeTests
{
    static object Field(object value,string name)=>value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(value);
    static object Property(object value,string name)=>value.GetType().GetProperty(name).GetValue(value);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name).Invoke(value,args);
    [UnityTest,Timeout(130000)]
    public IEnumerator NaturalTeamSmugglingUsesSharedDepotsCreditsBothPartnersAndRematches()
    {
        var type=Type.GetType("LWS.TruckHitchSports.HitchRunMatch, Assembly-CSharp",true);var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);UnityEngine.Object match=null;float oldScale=Time.timeScale;
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"Alpha10TeamSmuggling"}))
        {
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHitchSports/Scenes/HitchAndRun.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();var setup=Field(match,"setup");setup.GetType().GetField("count").SetValue(setup,4);match.GetType().GetField("option").SetValue(match,2);Call(match,"NormalizeSetup");Call(match,"AllBots");
                var weather=Field(match,"presentationWeather");if(weather!=null&&Field(weather,"presets")!=null)Call(Field(weather,"presets"),"Select","9aad5ff5dfa09ce468205f676714f891");
                Assert.That((bool)Call(match,"StartMatch"),Is.True);Assert.That((int)Field(setup,"count"),Is.EqualTo(4));
                var trucks=(Array)Field(match,"trucks");var loads=(Array)Field(match,"trailers");var couplers=(Array)Field(match,"couplers");var depots=(Transform[])Field(match,"depots");var players=(Array)Field(setup,"players");
                var identities=Enumerable.Range(0,4).Select(slot=>(string)Field(players.GetValue(slot),"participantId")).ToArray();var loadIds=Enumerable.Range(0,loads.Length).Select(index=>((Rigidbody)Field(loads.GetValue(index),"Body")).GetInstanceID()).ToArray();
                var originalPositions=Enumerable.Range(0,loads.Length).Select(index=>((Rigidbody)Field(loads.GetValue(index),"Body")).position).ToArray();
                var nativeParticipation=new bool[4];var pending=new object[4];var receivingEvidence=new string[4];var observedDeliveries=new int[4];var previousDeliveries=new int[4];
                float nextSample=45,deadline=Time.realtimeSinceStartup+95;int rows=0;Time.timeScale=3;
                while(Time.realtimeSinceStartup<deadline)
                {
                    var deliveries=(int[])Field(match,"Deliveries");var scores=(double[])Field(match,"Scores");
                    for(int slot=0;slot<4;slot++)
                    {
                        var coupler=couplers.GetValue(slot);var attached=Property(coupler,"Attached");
                        if(deliveries[slot]==previousDeliveries[slot]&&pending[slot]!=null&&!ReferenceEquals(pending[slot],attached))pending[slot]=null;
                        if(attached!=null&&(bool)Property(coupler,"PhysicalConstraintPresent"))
                        {
                            nativeParticipation[slot]=true;var body=(Rigidbody)Field(attached,"Body");var truckBody=(Rigidbody)Field(trucks.GetValue(slot),"body");int index=Enumerable.Range(0,loads.Length).Where(i=>ReferenceEquals(loads.GetValue(i),attached)).DefaultIfEmpty(-1).First();
                            Assert.That(index,Is.GreaterThanOrEqualTo(0),"The attached load must belong to the original saved roster.");
                            Assert.That(body.GetInstanceID(),Is.EqualTo(loadIds[index]),"The native hitch must carry an original saved load, not a replacement.");
                            float gap=Vector3.Distance(body.worldCenterOfMass,depots[slot%2].position);
                            if(gap<(float)Field(match,"deliveryRadius")&&body.linearVelocity.magnitude<.8f&&truckBody.linearVelocity.magnitude<.8f)
                            {
                                pending[slot]=attached;
                                receivingEvidence[slot]="P"+(slot+1)+" originalLoad="+index+" sharedDepot="+(slot%2)+" gap="+gap.ToString("F2")+" cargoSpeed="+body.linearVelocity.magnitude.ToString("F2")+" tractorSpeed="+truckBody.linearVelocity.magnitude.ToString("F2")+" travelled="+Vector3.ProjectOnPlane(body.position-originalPositions[index],Vector3.up).magnitude.ToString("F1");
                            }
                        }
                        if(deliveries[slot]>previousDeliveries[slot])
                        {
                            Assert.That(pending[slot],Is.Not.Null,"Delivery requires an observed actual original-load native hitch stopped at its shared receiving depot. "+State(match));
                            Assert.That((int)Property(pending[slot],"Deliveries"),Is.GreaterThan(0),"The exact physically received load must be dispatched. "+receivingEvidence[slot]);
                            observedDeliveries[slot]+=deliveries[slot]-previousDeliveries[slot];pending[slot]=null;
                        }
                        previousDeliveries[slot]=deliveries[slot];
                    }
                    for(int team=0;team<2;team++)
                    {
                        Assert.That(scores[team+2],Is.EqualTo(scores[team]).Within(.001),"Each banked delivery credits its two actual teammates equally.");
                        if(deliveries[team]+deliveries[team+2]==0)Assert.That(scores[team],Is.Zero,"A team without any real delivery cannot earn cargo points.");
                    }
                    if(rows<4&&(float)Property(match,"Elapsed")>=nextSample){rows++;nextSample+=45;Debug.Log("HH10_TEAM_SMUGGLING_STATE "+State(match));}
                    if((bool)Property(match,"Finished"))break;
                    yield return new WaitForFixedUpdate();
                }
                string state=State(match);Assert.That((bool)Property(match,"Finished"),Is.True,state);var finalScores=(double[])Field(match,"Scores");var finalDeliveries=(int[])Field(match,"Deliveries");
                for(int team=0;team<2;team++)Assert.That(observedDeliveries[team]+observedDeliveries[team+2],Is.GreaterThan(0),"Both teams must physically bank cargo, not finish through a zero-delivery timeout. "+state);
                Assert.That(nativeParticipation.All(value=>value),Is.True,"All four assigned AI teammates/opponents must actually operate a native attached load. "+state);
                double best=Math.Max(finalScores[0],finalScores[1]);Assert.That(best,Is.GreaterThan(0),state);
                for(int slot=0;slot<4;slot++)
                {
                    Assert.That((bool)Call(match,"ResultEligible",slot),Is.True,"A teammate shares only a genuine banked team result. "+state);
                    Assert.That((bool)Call(match,"ResultWinner",slot),Is.EqualTo(Math.Abs(finalScores[slot]-best)<.001),"Winning partners share the authoritative competitive outcome.");
                    Assert.That(finalDeliveries[slot],Is.EqualTo(observedDeliveries[slot]),"Every delivery must have observed physical receiving evidence.");
                }
                Assert.That((string)Property(match,"Results"),Is.Not.Empty);string oldId=(string)Property(match,"MatchId");Call(match,"SetupScreen");yield return null;Assert.That((bool)Call(match,"StartMatch"),Is.True);
                Assert.That((string)Property(match,"MatchId"),Is.Not.EqualTo(oldId));Assert.That(((double[])Field(match,"Scores")).All(value=>value==0),Is.True);Assert.That(((int[])Field(match,"Deliveries")).All(value=>value==0),Is.True);
                for(int slot=0;slot<4;slot++)Assert.That((string)Field(players.GetValue(slot),"participantId"),Is.EqualTo(identities[slot]));
                for(int index=0;index<loads.Length;index++)Assert.That(((Rigidbody)Field(loads.GetValue(index),"Body")).GetInstanceID(),Is.EqualTo(loadIds[index]));
                Debug.Log("HH10_TEAM_SMUGGLING_PASS four native AI roles, original loads, actual stopped shared-depot deliveries, equal partner credits, competitive winners and fresh rematch. "+state+" receiving=["+string.Join(" | ",receivingEvidence)+"]");Call(match,"SetupScreen");
            }
            finally{try{if(match!=null&&(bool)Property(match,"Running"))Call(match,"SetupScreen");}finally{Time.timeScale=oldScale;}}
        }
    }
    static string State(object match)
    {return "clock="+Property(match,"Elapsed")+" deliveries="+string.Join(",",((int[])Field(match,"Deliveries")).Take(4))+" scores="+string.Join(",",((double[])Field(match,"Scores")).Take(4))+" "+string.Join(" | ",Enumerable.Range(0,4).Select(slot=>"P"+(slot+1)+" "+Call(match,"ContestDiagnostic",slot)+" "+Call(match,"HitchRunApproachDiagnostic",slot)));}
}
#endif
