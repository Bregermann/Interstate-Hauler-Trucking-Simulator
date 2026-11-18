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
public sealed class ScarecrowNightmarePlayModeTests
{
    static object Field(object value,string name)=>value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(value);
    static object Property(object value,string name)=>value.GetType().GetProperty(name).GetValue(value);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name).Invoke(value,args);
    [UnityTest,Timeout(150000)]
    public IEnumerator NaturalCooperativeNightmareCompletesSharedWardsEscapesTogetherAndRematches()
    {
        var type=Type.GetType("LWS.TruckHorrorSports.HorrorMatch, Assembly-CSharp",true);var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);UnityEngine.Object match=null;float oldScale=Time.timeScale;
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"Alpha10ScarecrowCooperativeNightmare"}))
        {
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHorrorSports/ScarecrowSeason/Scenes/ScarecrowSeason.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();match.GetType().GetField("option").SetValue(match,3);var setup=Field(match,"setup");setup.GetType().GetField("count").SetValue(setup,4);Call(match,"NormalizeSetup");Call(match,"AllBots");
                Assert.That((bool)Call(match,"StartMatch"),Is.True);Assert.That((bool)Property(match,"Cooperative"),Is.True);Assert.That((int)Field(setup,"count"),Is.EqualTo(4));
                var rules=Field(match,"rules");var trucks=(Array)Field(match,"trucks");var players=(Array)Field(setup,"players");var objectives=(Array)Field(match,"objectives");
                var bodies=Enumerable.Range(0,4).Select(slot=>(Rigidbody)Field(trucks.GetValue(slot),"body")).ToArray();var initial=bodies.Select(body=>body.position).ToArray();var identities=Enumerable.Range(0,4).Select(slot=>(string)Field(players.GetValue(slot),"participantId")).ToArray();
                var sawLost=new bool[4];var sawRecovered=new bool[4];float deadline=Time.realtimeSinceStartup+115,nextSample=45;int rows=0;Time.timeScale=3;
                while(!(bool)Property(match,"Finished")&&Time.realtimeSinceStartup<deadline)
                {
                    var outcomes=(Array)Field(rules,"outcome");
                    for(int slot=0;slot<4;slot++)
                    {
                        string outcome=outcomes.GetValue(slot).ToString();sawLost[slot]|=outcome=="Lost";sawRecovered[slot]|=sawLost[slot]&&outcome=="Active";
                    }
                    if(rows<4&&(float)Property(match,"Age")>=nextSample){rows++;nextSample+=45;Debug.Log("HH10_SCARECROW_COOP_STATE "+State(match));}
                    yield return new WaitForFixedUpdate();
                }
                string state=State(match);Assert.That((bool)Property(match,"Finished"),Is.True,state);Assert.That((bool)Property(match,"Successful"),Is.True,"A shared timeout or abandoned teammate is not a successful nightmare. "+state);
                Assert.That((int)Property(rules,"TotalTasks"),Is.EqualTo(objectives.Length),"Each shared ward must commit exactly once, not once per truck. "+state);
                var physicalExits=(bool[])Field(match,"physicallyExtracted");
                for(int slot=0;slot<4;slot++)
                {
                    Assert.That((bool)Call(match,"Escaped",slot)&&physicalExits[slot],Is.True,"All original participants must receive the physical group-extraction receipt. "+state);
                    for(int ward=0;ward<objectives.Length;ward++)Assert.That((bool)Call(rules,"Done",slot,ward),Is.True,"Shared receipts belong to every participant.");
                    Assert.That(Vector3.ProjectOnPlane(bodies[slot].position-initial[slot],Vector3.up).magnitude,Is.GreaterThan(100),"Every original truck must actually drive through the venue. "+state);
                    Assert.That((bool)Call(match,"ResultWinner",slot),Is.False,"Cooperative success must not produce an individual winner.");Assert.That((int)Call(match,"ResultPlace",slot),Is.Zero);Assert.That((bool)Call(match,"ResultEligible",slot),Is.False);
                }
                int rescueReceipts=((int[])Field(rules,"rescues")).Sum();
                if(sawRecovered.Any(value=>value))Assert.That(rescueReceipts,Is.GreaterThan(0),"Observed Lost→Active recovery requires a genuine cooperative rescue receipt. "+state);
                Assert.That((string)Property(match,"Results"),Is.Not.Empty);Assert.That((bool)Property(match,"FinalMetricsRecorded"),Is.True);
                string oldId=(string)Property(match,"MatchId");Call(match,"SetupScreen");yield return null;Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return new WaitForFixedUpdate();
                Assert.That((string)Property(match,"MatchId"),Is.Not.EqualTo(oldId));Assert.That((int)Property(rules,"TotalTasks"),Is.Zero);Assert.That(((int[])Field(rules,"rescues")).All(value=>value==0),Is.True);Assert.That(((float[])Field(rules,"health")).Take(4).All(value=>value==100),Is.True);Assert.That(((bool[])Field(match,"physicallyExtracted")).Any(value=>value),Is.False);
                for(int slot=0;slot<4;slot++){Assert.That((string)Field(players.GetValue(slot),"participantId"),Is.EqualTo(identities[slot]));Assert.That((bool)Call(match,"Escaped",slot),Is.False);}
                Debug.Log("HH10_SCARECROW_COOPERATIVE_ESCAPE_PASS shared physical wards, ALL original trucks extracted, no individual winners, results and rematch. ActualLostObserved="+sawLost.Count(value=>value)+" actualRecoveredObserved="+sawRecovered.Count(value=>value)+" realRescueReceipts="+rescueReceipts+". Rescue is NOT certified if no natural casualty/recovery occurred. "+state);Call(match,"SetupScreen");
            }
            finally{try{if(match!=null&&(bool)Property(match,"Running"))Call(match,"SetupScreen");}finally{Time.timeScale=oldScale;}}
        }
    }
    static string State(object match)
    {
        var rules=Field(match,"rules");var trucks=(Array)Field(match,"trucks");var outcomes=(Array)Field(rules,"outcome");var health=(float[])Field(rules,"health");var rescues=(int[])Field(rules,"rescues");
        return "clock="+Property(match,"Age")+" sharedTasks="+Property(rules,"TotalTasks")+" "+string.Join(" | ",Enumerable.Range(0,4).Select(slot=>"P"+(slot+1)+" outcome="+outcomes.GetValue(slot)+" health="+health[slot].ToString("F1")+" rescues="+rescues[slot]+" pos="+((Rigidbody)Field(trucks.GetValue(slot),"body")).position+" next="+Call(match,"NextObjective",slot)));
    }
}
#endif
