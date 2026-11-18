#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public sealed class GraveyardSecurityAuditPlayModeTests
{
    static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    static object Property(object value,string name)=>value.GetType().GetProperty(name).GetValue(value);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name).Invoke(value,args);
    [UnityTest,Timeout(140000)]
    public IEnumerator NaturalSecurityJobSelectsRealFeedReportsMissingOriginalRigAndResets()
    {
        var type=Type.GetType("LWS.TruckHorrorSports.HorrorMatch, Assembly-CSharp",true);var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);UnityEngine.Object match=null;float oldScale=Time.timeScale;
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"Alpha10MissingRigSecurityAudit"}))
        {
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckHorrorSports/GraveyardShift/Scenes/GraveyardShift.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();var services=Field(match,"nightServices");var audit=Field(services,"securityAudit");Assert.That(audit,Is.Not.Null,"Apply the explicit saved-scene security-console upgrade.");
                var objectives=(Array)Field(match,"objectives");Assert.That(objectives.Length,Is.EqualTo(6),"Keep the original six-job route intact.");var originalJobs=objectives.Cast<object>().ToArray();var monitor=Field(audit,"monitor");var camera=(Camera)Field(monitor,"securityCamera");int cameraId=camera.GetInstanceID();
                var setup=Field(match,"setup");setup.GetType().GetField("count").SetValue(setup,4);match.GetType().GetField("option").SetValue(match,1);Call(match,"NormalizeSetup");Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);Assert.That((int)Field(setup,"count"),Is.EqualTo(4));
                Time.timeScale=3;float deadline=Time.realtimeSinceStartup+105,nextSample=45;int rows=0;
                var feet=(Array)Field(match,"footAgents");var supply=Field(match,"supplyJob");var observedPosition=new Vector3[4];var stationarySince=new float[4];var obstructionLogged=new bool[4];
                while(!(bool)Property(match,"Finished")&&!Enumerable.Range(0,4).Any(slot=>(bool)Call(audit,"Reported",slot))&&Time.realtimeSinceStartup<deadline)
                {
                    if(rows<3&&(float)Property(match,"Age")>=nextSample){rows++;nextSample+=45;Debug.Log("HH10_GRAVEYARD_SECURITY_AUDIT_STATE "+State(match,audit));}
                    float age=(float)Property(match,"Age");
                    for(int slot=0;slot<4;slot++)
                    {
                        var worker=(Component)feet.GetValue(slot);int next=(int)Call(match,"NextObjective",slot);
                        bool walking=((float[])Field(Field(match,"rules"),"health"))[slot]>0&&(bool)Property(worker,"Outside")&&next>0&&next<objectives.Length
                            &&Vector3.Distance(worker.transform.position,((Component)objectives.GetValue(next)).transform.position)>2.2f;
                        if(!walking||Vector3.ProjectOnPlane(worker.transform.position-observedPosition[slot],Vector3.up).sqrMagnitude>.04f)
                        {observedPosition[slot]=worker.transform.position;stationarySince[slot]=age;}
                        if(!obstructionLogged[slot]&&walking&&age-stationarySince[slot]>=5)
                        {
                            obstructionLogged[slot]=true;
                            Debug.Log("HH10_GRAVEYARD_FOOT_OBSTRUCTION clock="+age+" P"+(slot+1)+" job="+next+" foot="+worker.transform.position+" "+Call(supply,"FootObstacleDiagnostic",slot));
                        }
                    }
                    yield return new WaitForFixedUpdate();
                }
                string state=State(match,audit);int reporter=Enumerable.Range(0,4).Where(slot=>(bool)Call(audit,"Reported",slot)).DefaultIfEmpty(-1).First();Assert.That(reporter,Is.GreaterThanOrEqualTo(0),"An actual participant must reach the original console, select its live view and inspect a truly disappeared original rig. No report is injected. "+state);
                Assert.That((int)Property(audit,"ViewSelections"),Is.GreaterThan(0),state);Assert.That((int)Call(audit,"ReportedView",reporter),Is.EqualTo(1),"Observe the actual reporter's parked-bay view selection.");
                int bay=(int)Call(audit,"ReportedBay",reporter);var bays=(GameObject[])Field(audit,"parkedBays");Assert.That(bay,Is.InRange(0,bays.Length-1));Assert.That(bays[bay].activeInHierarchy,Is.True,"Do not substitute a destroyed or inactive asset for the supernatural disappearance.");
                Assert.That(bays[bay].GetComponentsInChildren<Renderer>(true).Any(renderer=>renderer.enabled&&renderer.gameObject.activeInHierarchy),Is.False,"The real scene rig must actually be absent from the rendered bay.");
                var foot=(Component)((Array)Field(match,"footAgents")).GetValue(reporter);Vector3 reportPosition=foot.transform.position;Assert.That((bool)Property(foot,"Outside"),Is.True);Assert.That(Vector3.Distance(reportPosition,((Transform)Field(audit,"console")).position),Is.LessThan(2.2f),"Inspect the exact reporter's physical console position. "+state);
                Assert.That((bool)Call(Field(match,"rules"),"Done",reporter,3),Is.True,"That participant's ordinary supply/pump/breaker/CB work must physically precede its security inspection.");
                float recorded=(float)Call(audit,"ReportFrameTime",reporter);Assert.That(recorded,Is.GreaterThan(0));
                // Another real nearby teammate can select a view immediately
                // after the report. Preserve the reporter's physical snapshot;
                // wait only for the existing camera's next real render receipt.
                float frameDeadline=Time.realtimeSinceStartup+2;
                while((!(bool)Property(monitor,"HasFrame")||(float)Property(monitor,"LastRenderedTime")<recorded)&&!(bool)Property(match,"Finished")&&Time.realtimeSinceStartup<frameDeadline)yield return null;
                Assert.That((bool)Property(monitor,"HasFrame"),Is.True,state);Assert.That(camera.targetTexture,Is.Not.Null);Assert.That(camera.targetTexture.IsCreated(),Is.True);Assert.That(camera.GetInstanceID(),Is.EqualTo(cameraId),"Reuse the original live camera/RT; no pre-rendered substitute feed.");
                Assert.That((float)Property(monitor,"LastRenderedTime"),Is.GreaterThanOrEqualTo(recorded));
                var view=((Transform[])Field(audit,"views"))[1];Assert.That(Vector3.Distance((Vector3)Property(monitor,"LastRenderedPosition"),view.position),Is.LessThan(.01f));Assert.That(Quaternion.Angle((Quaternion)Property(monitor,"LastRenderedRotation"),view.rotation),Is.LessThan(.1f));
                var current=(Array)Field(match,"objectives");for(int job=0;job<6;job++)Assert.That(current.GetValue(job),Is.SameAs(originalJobs[job]),"Optional inspection must not replace the original tasks.");
                Call(match,"SetupScreen");yield return null;Assert.That(((AudioSource)Field(audit,"switchSound")).isPlaying,Is.False);Assert.That((bool)Call(match,"StartMatch"),Is.True);for(int slot=0;slot<4;slot++)Assert.That((bool)Call(audit,"Reported",slot),Is.False);Assert.That((int)Property(audit,"Reports"),Is.Zero);Assert.That((int)Property(audit,"ViewSelections"),Is.Zero);Assert.That((int)Property(audit,"SelectedView"),Is.Zero);Assert.That((int)Property(audit,"ConsoleOwner"),Is.EqualTo(-1));
                Assert.That(camera.GetInstanceID(),Is.EqualTo(cameraId));Assert.That(Vector3.Distance(camera.transform.position,((Transform[])Field(audit,"views"))[0].position),Is.LessThan(.01f));
                Debug.Log("HH10_GRAVEYARD_SECURITY_AUDIT_PASS reporter=P"+(reporter+1)+" reportFoot="+reportPosition+" actual original missing rig, selected existing live CCTV frame, real on-foot inspection after ordinary jobs and clean reset. Human tap/hold/controller/listening/Windows visual acceptance remain unverified. "+state);Call(match,"SetupScreen");
            }
            finally{try{if(match!=null&&(bool)Property(match,"Running"))Call(match,"SetupScreen");}finally{Time.timeScale=oldScale;}}
        }
    }
    static string State(object match,object audit)
    {
        var rules=Field(match,"rules");var feet=(Array)Field(match,"footAgents");var trucks=(Array)Field(match,"trucks");var outcomes=(Array)Field(rules,"outcome");var health=(float[])Field(rules,"health");var tasks=(int[])Field(rules,"tasks");var console=(Transform)Field(audit,"console");
        return "clock="+Property(match,"Age")+" "+string.Join(" | ",Enumerable.Range(0,4).Select(slot=>
        {var foot=(Component)feet.GetValue(slot);var controller=(CharacterController)Field(foot,"controller");return "P"+(slot+1)+" jobs="+tasks[slot]+" next="+Call(match,"NextObjective",slot)+" outcome="+outcomes.GetValue(slot)+" health="+health[slot].ToString("F1")+" cab="+((Rigidbody)Field(trucks.GetValue(slot),"body")).position+" outside="+Property(foot,"Outside")+" foot="+foot.transform.position+" footVelocity="+controller.velocity+" consoleGap="+Vector3.Distance(foot.transform.position,console.position).ToString("F2")+" "+Call(audit,"Diagnostic",slot)+" "+Call(Field(match,"supplyJob"),"FootObstacleDiagnostic",slot);}));
    }
}
#endif
