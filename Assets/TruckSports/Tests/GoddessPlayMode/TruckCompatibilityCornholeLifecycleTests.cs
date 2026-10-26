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
public sealed class TruckCompatibilityCornholeLifecycleTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo f?f.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(owner,args);
    static object Registry(string method,params object[] args)=>Sports("SportsParticipantRegistry").GetMethod(method).Invoke(null,args);
    static string Stored()=>(string)Sports("SportsPrefs").GetMethod("GetString").Invoke(null,new object[]{"TruckSports.Participants.v1",""});
    IDisposable prefs;GameObject isolation;Component presenter;
    [TearDown]public void Cleanup(){if(presenter!=null)((Behaviour)presenter).enabled=false;if(isolation!=null)UnityEngine.Object.DestroyImmediate(isolation);Time.timeScale=1;Registry("ForgetCache");prefs?.Dispose();prefs=null;}
    [UnityTest,Timeout(120000)]public IEnumerator AllFourSavedCornholeOwnersHaveHonestSupportedSetupReportsWithoutPreparingAnyOfSixteenActors()
    {
        prefs=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"CornFit"+Guid.NewGuid().ToString("N")});Registry("ForgetCache");isolation=new GameObject("Cornhole setup fit isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Sports("SportsProbeInputIsolation"));var scene=EditorSceneManager.LoadSceneInPlayMode("Assets/TruckCornhole/Scenes/TruckCornhole.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;var source=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren(Sports("SportsTruckCompatibilitySource"),true)).Cast<Component>().Single();presenter=source.GetComponent(Sports("SportsTruckCompatibilityPresenter"));var match=Get(source,"cornhole");Set(Get(match,"setup"),"count",4);Call(match,"AllAI");var ids=new string[4];for(int i=0;i<4;i++){ids[i]=(string)Get(Registry("Create","Same Driver"),"id");Registry("UseInSeat",i,ids[i]);}Call(match,"RefreshParticipants",false);var courses=((Array)Get(match,"courses")).Cast<object>().ToArray();var trucks=courses.SelectMany(c=>((Array)Get(c,"trucks")).Cast<object>()).ToArray();var hosts=trucks.Select(t=>Get(t,"appearanceHost")).ToArray();Assert.That(hosts.Length,Is.EqualTo(16));Assert.That(Get(source,"sharedTurnHost"),Is.Null);string bytes=Stored();var revisions=hosts.Select(h=>Get(h,"AppearanceRevision")).ToArray();var models=hosts.Select(h=>Get(h,"AppearanceRoot")).ToArray();var profiles=hosts.Select(h=>Get(h,"ProfileId")).ToArray();var states=trucks.Select(t=>(Rigidbody)Get(t,"body")).Select(body=>(body,body.position,body.rotation,body.linearVelocity,body.angularVelocity,body.isKinematic,body.detectCollisions)).ToArray();var choices=((Array)Get(Get(hosts[0],"catalog"),"bodies")).Cast<object>().ToArray();
        for(int count=1;count<=4;count++){Set(Get(match,"setup"),"count",count);for(int i=0;i<count;i++){Assert.That(Call(source,"Host",i),Is.SameAs(hosts[i*4]));var report=Call(source,"Read",i);Assert.That(Get(report,"profileId"),Is.EqualTo(ids[i]));Assert.That(Get(report,"hasHost"),Is.EqualTo(true));Assert.That(Get(report,"canApply"),Is.EqualTo(true));foreach(var body in choices){var draft=Registry("GetAppearance",ids[i]);Set(draft,"bodyId",Get(body,"id"));var view=Call(source,"Evaluate",i,ids[i],draft);Assert.That(Get(view,"canApply"),Is.EqualTo(true));Assert.That(Get(view,"bodyFallback"),Is.EqualTo(false));}}Assert.That(Call(source,"Host",count),Is.Null);}Set(Get(match,"setup"),"count",4);yield return null;Call(presenter,"Open");yield return new WaitForSecondsRealtime(.8f);var modal=Get(presenter,"Modal");Assert.That(Get(modal,"IsOpen"),Is.EqualTo(true));var rows=(Array)Get(modal,"rows");for(int i=0;i<4;i++){Assert.That(((GameObject)Get(rows.GetValue(i),"root")).activeSelf,Is.True);StringAssert.Contains(ids[i].Substring(0,8),(string)Get(Get(rows.GetValue(i),"name"),"text"));StringAssert.Contains("supported",((string)Get(Get(rows.GetValue(i),"status"),"text")).ToLowerInvariant());}((Behaviour)presenter).enabled=false;
        CollectionAssert.AreEqual(revisions,hosts.Select(h=>Get(h,"AppearanceRevision")));CollectionAssert.AreEqual(models,hosts.Select(h=>Get(h,"AppearanceRoot")));CollectionAssert.AreEqual(profiles,hosts.Select(h=>Get(h,"ProfileId")));Assert.That(Stored(),Is.EqualTo(bytes));foreach(var e in states){Assert.That(e.body.position,Is.EqualTo(e.position));Assert.That(e.body.rotation,Is.EqualTo(e.rotation));Assert.That(e.body.linearVelocity,Is.EqualTo(e.linearVelocity));Assert.That(e.body.angularVelocity,Is.EqualTo(e.angularVelocity));Assert.That(e.body.isKinematic,Is.EqualTo(e.isKinematic));Assert.That(e.body.detectCollisions,Is.EqualTo(e.detectCollisions));}Assert.That(Get(match,"InMatch"),Is.EqualTo(false));TestContext.WriteLine("CORN_SETUP_FIT four logical owners, four equivalent retained actors each/all7 bodyfits; actual modal shows all4 supported IDs; no Prepare/profile/model/physics changes.");
    }
}
#endif
