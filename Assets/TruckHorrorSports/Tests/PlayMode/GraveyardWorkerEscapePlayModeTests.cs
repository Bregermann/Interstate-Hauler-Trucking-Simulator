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
public sealed class GraveyardWorkerEscapePlayModeTests
{
    static object Field(object value,string name)=>value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(value);
    static object Property(object value,string name)=>value.GetType().GetProperty(name).GetValue(value);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name).Invoke(value,args);
    [UnityTest,Timeout(150000)]
    public IEnumerator NaturalTargetedWorkerPhysicallyReturnsToReachableCabAndResets()
    {
        var type=Type.GetType("LWS.TruckHorrorSports.HorrorMatch, Assembly-CSharp",true);var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);UnityEngine.Object match=null;float oldScale=Time.timeScale;
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"Alpha10GraveyardWorkerEscape"}))
        {
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHorrorSports/GraveyardShift/Scenes/GraveyardShift.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();var setup=Field(match,"setup");setup.GetType().GetField("count").SetValue(setup,4);match.GetType().GetField("option").SetValue(match,1);Call(match,"NormalizeSetup");Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);
                var feet=(Array)Field(match,"footAgents");var trucks=(Array)Field(match,"trucks");var rules=Field(match,"rules");
                var footIds=Enumerable.Range(0,4).Select(slot=>((Component)feet.GetValue(slot)).GetInstanceID()).ToArray();var seen=new bool[4];var jobsAtEscape=new int[4];var physicalWalk=new float[4];var lastFeet=Enumerable.Range(0,4).Select(slot=>((Component)feet.GetValue(slot)).transform.position).ToArray();
                float deadline=Time.realtimeSinceStartup+115,nextSample=45;int rows=0,returned=-1;Time.timeScale=3;
                while(!(bool)Property(match,"Finished")&&Time.realtimeSinceStartup<deadline&&returned<0)
                {
                    for(int slot=0;slot<4;slot++)
                    {
                        var foot=feet.GetValue(slot);var component=(Component)foot;Vector3 position=component.transform.position;
                        if((bool)Call(match,"GraveyardWorkerRetreating",slot))
                        {
                            if(!seen[slot]){seen[slot]=true;jobsAtEscape[slot]=((int[])Field(rules,"tasks"))[slot];lastFeet[slot]=position;}
                            physicalWalk[slot]+=Vector3.ProjectOnPlane(position-lastFeet[slot],Vector3.up).magnitude;
                        }
                        lastFeet[slot]=position;
                        if((int)Call(match,"GraveyardWorkerCabReturns",slot)>0)
                        {
                            returned=slot;Assert.That((bool)Property(foot,"Outside"),Is.False,"The original worker must physically enter its own cab, not receive a remote safety state.");
                            var truck=(Component)trucks.GetValue(slot);var body=(Rigidbody)Field(truck,"body");
                            Assert.That(Vector3.Distance(position,body.position),Is.LessThan(4.25f),"Observe the existing4m entry after one physical step, not a worker disappearance across the map.");
                            Assert.That(component.GetInstanceID(),Is.EqualTo(footIds[slot]));Assert.That((int)Call(match,"GraveyardWorkerRetreats",slot),Is.GreaterThan(0));
                            if(seen[slot])Assert.That(((int[])Field(rules,"tasks"))[slot],Is.EqualTo(jobsAtEscape[slot]),"An interrupted foot job must not be awarded by the escape response.");
                            break;
                        }
                    }
                    if(rows<3&&(float)Property(match,"Age")>=nextSample){rows++;nextSample+=45;Debug.Log("HH10_GRAVEYARD_WORKER_ESCAPE_STATE "+State(match));}
                    if(returned<0)yield return new WaitForFixedUpdate();
                }
                string state=State(match);
                if(returned<0)Assert.Inconclusive("No successful natural worker-to-cab escape occurred in this bounded round. Preserve actual captures/no encounter as unverified; no damage, threat, pose, score or outcome was injected. "+state);
                string previous=(string)Property(match,"MatchId");Call(match,"SetupScreen");yield return null;Assert.That((bool)Call(match,"StartMatch"),Is.True);Assert.That((string)Property(match,"MatchId"),Is.Not.EqualTo(previous));
                for(int slot=0;slot<4;slot++){Assert.That((int)Call(match,"GraveyardWorkerRetreats",slot),Is.Zero);Assert.That((int)Call(match,"GraveyardWorkerCabReturns",slot),Is.Zero);Assert.That((bool)Call(match,"GraveyardWorkerRetreating",slot),Is.False);Assert.That(((Component)feet.GetValue(slot)).GetInstanceID(),Is.EqualTo(footIds[slot]));}
                Debug.Log("HH10_GRAVEYARD_WORKER_ESCAPE_PASS actual targeted worker, reachable original cab door, CharacterController walk="+physicalWalk[returned].ToString("F2")+"m, no interrupted-job award and fresh rematch. This does not certify a complete night or guaranteed survival. "+state);Call(match,"SetupScreen");
            }
            finally{try{if(match!=null&&(bool)Property(match,"Running"))Call(match,"SetupScreen");}finally{Time.timeScale=oldScale;}}
        }
    }
    static string State(object match)
    {
        var rules=Field(match,"rules");var feet=(Array)Field(match,"footAgents");var trucks=(Array)Field(match,"trucks");var outcomes=(Array)Field(rules,"outcome");var health=(float[])Field(rules,"health");
        return "clock="+Property(match,"Age")+" actualTasks="+Property(rules,"TotalTasks")+" "+string.Join(" | ",Enumerable.Range(0,4).Select(slot=>"P"+(slot+1)+" "+Call(match,"GraveyardWorkerEscapeDiagnostic",slot)+" outcome="+outcomes.GetValue(slot)+" health="+health[slot].ToString("F1")+" foot="+((Component)feet.GetValue(slot)).transform.position+" cab="+((Rigidbody)Field(trucks.GetValue(slot),"body")).position));
    }
}
#endif
