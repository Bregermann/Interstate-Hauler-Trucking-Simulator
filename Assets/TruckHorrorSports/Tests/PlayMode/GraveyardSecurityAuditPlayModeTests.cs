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
                var setup=Field(match,"setup");setup.GetType().GetField("count").SetValue(setup,1);match.GetType().GetField("option").SetValue(match,1);Call(match,"NormalizeSetup");Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);
                Time.timeScale=3;float deadline=Time.realtimeSinceStartup+105,nextSample=45;int rows=0;
                while(!(bool)Property(match,"Finished")&&!(bool)Call(audit,"Reported",0)&&Time.realtimeSinceStartup<deadline)
                {
                    if(rows<3&&(float)Property(match,"Age")>=nextSample){rows++;nextSample+=45;Debug.Log("HH10_GRAVEYARD_SECURITY_AUDIT_STATE clock="+Property(match,"Age")+" "+Call(audit,"Diagnostic",0));}
                    yield return new WaitForFixedUpdate();
                }
                string state="clock="+Property(match,"Age")+" "+Call(audit,"Diagnostic",0);Assert.That((bool)Call(audit,"Reported",0),Is.True,"The actual AI must reach the original console, select its live view and inspect a truly disappeared original rig. No report is injected. "+state);
                Assert.That((int)Property(audit,"ViewSelections"),Is.GreaterThan(0),state);Assert.That((int)Call(audit,"ReportedView",0),Is.EqualTo(1),"Observe the actual parked-bay view selection.");
                int bay=(int)Call(audit,"ReportedBay",0);var bays=(GameObject[])Field(audit,"parkedBays");Assert.That(bay,Is.InRange(0,bays.Length-1));Assert.That(bays[bay].activeInHierarchy,Is.True,"Do not substitute a destroyed or inactive asset for the supernatural disappearance.");
                Assert.That(bays[bay].GetComponentsInChildren<Renderer>(true).Any(renderer=>renderer.enabled&&renderer.gameObject.activeInHierarchy),Is.False,"The real scene rig must actually be absent from the rendered bay.");
                Assert.That((bool)Property(monitor,"HasFrame"),Is.True,state);Assert.That(camera.targetTexture,Is.Not.Null);Assert.That(camera.targetTexture.IsCreated(),Is.True);Assert.That(camera.GetInstanceID(),Is.EqualTo(cameraId),"Reuse the original live camera/RT; no pre-rendered substitute feed.");
                float recorded=(float)Call(audit,"ReportFrameTime",0);Assert.That(recorded,Is.GreaterThan(0));Assert.That((float)Property(monitor,"LastRenderedTime"),Is.GreaterThanOrEqualTo(recorded));
                var view=((Transform[])Field(audit,"views"))[1];Assert.That(Vector3.Distance((Vector3)Property(monitor,"LastRenderedPosition"),view.position),Is.LessThan(.01f));Assert.That(Quaternion.Angle((Quaternion)Property(monitor,"LastRenderedRotation"),view.rotation),Is.LessThan(.1f));
                var foot=(Component)((Array)Field(match,"footAgents")).GetValue(0);Assert.That((bool)Property(foot,"Outside"),Is.True);Assert.That(Vector3.Distance(foot.transform.position,((Transform)Field(audit,"console")).position),Is.LessThan(2.2f));
                Assert.That((bool)Call(Field(match,"rules"),"Done",0,3),Is.True,"Ordinary supply/pump/breaker/CB work must physically precede the security inspection.");
                var current=(Array)Field(match,"objectives");for(int job=0;job<6;job++)Assert.That(current.GetValue(job),Is.SameAs(originalJobs[job]),"Optional inspection must not replace the original tasks.");
                Call(match,"SetupScreen");yield return null;Assert.That(((AudioSource)Field(audit,"switchSound")).isPlaying,Is.False);Assert.That((bool)Call(match,"StartMatch"),Is.True);Assert.That((bool)Call(audit,"Reported",0),Is.False);Assert.That((int)Property(audit,"Reports"),Is.Zero);Assert.That((int)Property(audit,"ViewSelections"),Is.Zero);Assert.That((int)Property(audit,"SelectedView"),Is.Zero);Assert.That((int)Property(audit,"ConsoleOwner"),Is.EqualTo(-1));
                Assert.That(camera.GetInstanceID(),Is.EqualTo(cameraId));Assert.That(Vector3.Distance(camera.transform.position,((Transform[])Field(audit,"views"))[0].position),Is.LessThan(.01f));
                Debug.Log("HH10_GRAVEYARD_SECURITY_AUDIT_PASS actual original missing rig, selected existing live CCTV frame, real on-foot inspection after ordinary jobs and clean reset. Human tap/hold/controller/listening/Windows visual acceptance remain unverified. "+state);Call(match,"SetupScreen");
            }
            finally{try{if(match!=null&&(bool)Property(match,"Running"))Call(match,"SetupScreen");}finally{Time.timeScale=oldScale;}}
        }
    }
}
#endif
