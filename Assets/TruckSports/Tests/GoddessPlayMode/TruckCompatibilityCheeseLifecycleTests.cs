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
public sealed class TruckCompatibilityCheeseLifecycleTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo f?f.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(owner,args);
    static object Registry(string method,params object[] args)=>Sports("SportsParticipantRegistry").GetMethod(method).Invoke(null,args);
    static string Stored()=>(string)Sports("SportsPrefs").GetMethod("GetString").Invoke(null,new object[]{"TruckSports.Participants.v1",""});
    IDisposable prefs;GameObject isolation;
    [TearDown]public void Cleanup(){if(isolation!=null)UnityEngine.Object.DestroyImmediate(isolation);Time.timeScale=1;Registry("ForgetCache");prefs?.Dispose();prefs=null;}
    [UnityTest,Timeout(120000)]public IEnumerator ActualFourCheeseParticipantReportsNeverBorrowTheTwelveBackgroundTrucks()
    {
        prefs=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"CheeseFit"+Guid.NewGuid().ToString("N")});Registry("ForgetCache");isolation=new GameObject("Cheese setup fit isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Sports("SportsProbeInputIsolation"));var scene=EditorSceneManager.LoadSceneInPlayMode("Assets/TruckCheeseRolling/Scenes/TruckCheeseRolling.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
        var source=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren(Sports("SportsTruckCompatibilitySource"),true)).Cast<Component>().Single();var match=Get(source,"cheese");var setup=Get(match,"setup");Set(setup,"count",4);Call(match,"AllAI");var ids=new string[4];for(int i=0;i<4;i++){ids[i]=(string)Get(Registry("Create","Same Driver"),"id");Registry("UseInSeat",i,ids[i]);}Call(match,"RefreshParticipants",false);var hosts=((Array)Get(match,"appearanceHosts")).Cast<object>().ToArray();var trucks=((Array)Get(match,"trucks")).Cast<Component>().ToArray();Assert.That(hosts.Length,Is.EqualTo(4));Assert.That(trucks.Length,Is.EqualTo(16));Assert.That(Get(source,"IsSetup"),Is.EqualTo(true));string bytes=Stored();var revisions=hosts.Select(h=>Get(h,"AppearanceRevision")).ToArray();var models=hosts.Select(h=>Get(h,"AppearanceRoot")).ToArray();var profiles=hosts.Select(h=>Get(h,"ProfileId")).ToArray();var states=trucks.Select(t=>(t,t.gameObject.activeSelf,scale:t.transform.localScale,body:(Rigidbody)Get(t,"body"))).Select(e=>(e.t,e.activeSelf,e.scale,e.body,position:e.body.position,rotation:e.body.rotation,velocity:e.body.linearVelocity,angular:e.body.angularVelocity)).ToArray();var choices=((Array)Get(Get(hosts[0],"catalog"),"bodies")).Cast<object>().ToArray();
        for(int repeat=0;repeat<3;repeat++)for(int i=0;i<4;i++){Assert.That(Call(source,"Host",i),Is.SameAs(hosts[i]));Assert.That(Get(Call(source,"Read",i),"profileId"),Is.EqualTo(ids[i]));foreach(var body in choices){var draft=Registry("GetAppearance",ids[i]);Set(draft,"bodyId",Get(body,"id"));var view=Call(source,"Evaluate",i,ids[i],draft);Assert.That(Get(view,"canApply"),Is.EqualTo(true));Assert.That(Get(view,"bodyFallback"),Is.EqualTo(false));StringAssert.Contains((string)Get(body,"id"),(string)Get(view,"effective"));}}
        Set(setup,"count",2);Assert.That(Call(source,"Host",2),Is.Null);Assert.That(Call(source,"Host",3),Is.Null);Set(setup,"count",4);for(int i=4;i<8;i++){Assert.That(Call(source,"Host",i),Is.Null);Assert.That(Get(Call(source,"Read",i),"profileId"),Is.EqualTo(""));}foreach(var truck in trucks.Skip(4))Assert.That(truck.GetComponent(Sports("SportsTruckAppearanceHost")),Is.Null);CollectionAssert.AreEqual(revisions,hosts.Select(h=>Get(h,"AppearanceRevision")));CollectionAssert.AreEqual(models,hosts.Select(h=>Get(h,"AppearanceRoot")));CollectionAssert.AreEqual(profiles,hosts.Select(h=>Get(h,"ProfileId")));Assert.That(Stored(),Is.EqualTo(bytes));foreach(var e in states){Assert.That(e.t.gameObject.activeSelf,Is.EqualTo(e.activeSelf));Assert.That(e.t.transform.localScale,Is.EqualTo(e.scale));Assert.That(e.body.position,Is.EqualTo(e.position));Assert.That(e.body.rotation,Is.EqualTo(e.rotation));Assert.That(e.body.linearVelocity,Is.EqualTo(e.velocity));Assert.That(e.body.angularVelocity,Is.EqualTo(e.angular));}Assert.That(Get(match,"InMatch"),Is.EqualTo(false));TestContext.WriteLine("CHEESE_SETUP_FIT four real participant IDs/all7 bodies; active-count masking; twelve background actors, profile bytes, model revisions and physics unchanged. No race/weather start needed for pure setup evaluation.");
    }
}
#endif
