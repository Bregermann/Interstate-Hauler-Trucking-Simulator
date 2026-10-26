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
public sealed class TruckCompatibilitySkeeLifecycleTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo f?f.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(owner,args);
    static object Registry(string method,params object[] args)=>Sports("SportsParticipantRegistry").GetMethod(method).Invoke(null,args);
    static string Stored()=>(string)Sports("SportsPrefs").GetMethod("GetString").Invoke(null,new object[]{"TruckSports.Participants.v1",""});
    IDisposable prefs;GameObject isolation;object match;
    [TearDown]public void Cleanup(){if(match is UnityEngine.Object live&&live!=null)Call(match,"SetupScreen");match=null;if(isolation!=null)UnityEngine.Object.DestroyImmediate(isolation);Time.timeScale=1;Registry("ForgetCache");prefs?.Dispose();prefs=null;}
    [UnityTest,Timeout(180000)]public IEnumerator EveryActiveSkeeSetupIdUsesTheEquivalentTemplateWithoutReassigningCurrentLaneOwner()
    {
        prefs=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"SkeeFit"+Guid.NewGuid().ToString("N")});Registry("ForgetCache");isolation=new GameObject("Skee setup fit isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Sports("SportsProbeInputIsolation"));var scene=EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSkeeBall/Scenes/TruckSkeeBall.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
        var source=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren(Sports("SportsTruckCompatibilitySource"),true)).Cast<Component>().Single();match=Get(source,"session");Set(match,"option",1);Set(Get(match,"setup"),"count",8);Call(match,"NormalizeSetup");Call(match,"AllBots");var ids=new string[8];for(int i=0;i<8;i++){ids[i]=(string)Get(Registry("Create","Same Driver"),"id");Registry("UseInSeat",i,ids[i]);}Call(match,"RefreshParticipants",false);var courses=((Array)Get(match,"courses")).Cast<object>().ToArray();var hosts=courses.Select(c=>Get(Get(c,"truck"),"appearanceHost")).ToArray();Assert.That(Get(source,"sharedTurnHost"),Is.SameAs(hosts[0]));Assert.That(((Array)Get(match,"appearanceHosts")).Length,Is.Zero);Assert.That(Call(match,"StartMatch"),Is.EqualTo(true));Call(courses[0],"ResetCourse",7);Assert.That(Get(hosts[0],"ProfileId"),Is.EqualTo(ids[7]),"The original actual turn boundary prepares participant7.");Call(match,"SetupScreen");yield return null;
        string bytes=Stored();var revisions=hosts.Select(h=>Get(h,"AppearanceRevision")).ToArray();var models=hosts.Select(h=>Get(h,"AppearanceRoot")).ToArray();var profiles=hosts.Select(h=>Get(h,"ProfileId")).ToArray();var owners=courses.Select(c=>Get(c,"owner")).ToArray();var slots=courses.Select(c=>Get(Get(c,"truck"),"slot")).ToArray();var bodies=courses.Select(c=>(Rigidbody)Get(Get(c,"truck"),"body")).Select(body=>(body,body.position,body.rotation,body.linearVelocity,body.angularVelocity)).ToArray();var choices=((Array)Get(Get(hosts[0],"catalog"),"bodies")).Cast<object>().ToArray();var author=Type.GetType("LWS.TruckSports.Editor.SportsTruckCompatibilitySkeeAuthoring, Assembly-CSharp-Editor",true);var prove=author.GetMethod("ProvenTemplate");
        for(int option=0;option<2;option++)for(int count=1;count<=(option==0?4:8);count++){Set(match,"option",option);Set(Get(match,"setup"),"count",count);Call(match,"NormalizeSetup");Assert.That(Get(source,"Count"),Is.EqualTo(count));Assert.That(prove.Invoke(null,new[]{match}),Is.SameAs(hosts[0]));for(int i=0;i<count;i++){Assert.That(Call(source,"Host",i),Is.SameAs(hosts[0]));Assert.That(Get(Call(source,"Read",i),"profileId"),Is.EqualTo(ids[i]));foreach(var body in choices){var draft=Registry("GetAppearance",ids[i]);Set(draft,"bodyId",Get(body,"id"));var view=Call(source,"Evaluate",i,ids[i],draft);Assert.That(Get(view,"canApply"),Is.EqualTo(true));Assert.That(Get(view,"bodyFallback"),Is.EqualTo(false));}}Assert.That(Call(source,"Host",count),Is.Null);}
        CollectionAssert.AreEqual(revisions,hosts.Select(h=>Get(h,"AppearanceRevision")));CollectionAssert.AreEqual(models,hosts.Select(h=>Get(h,"AppearanceRoot")));CollectionAssert.AreEqual(profiles,hosts.Select(h=>Get(h,"ProfileId")));CollectionAssert.AreEqual(owners,courses.Select(c=>Get(c,"owner")));CollectionAssert.AreEqual(slots,courses.Select(c=>Get(Get(c,"truck"),"slot")));Assert.That(Stored(),Is.EqualTo(bytes));foreach(var e in bodies){Assert.That(e.body.position,Is.EqualTo(e.position));Assert.That(e.body.rotation,Is.EqualTo(e.rotation));Assert.That(e.body.linearVelocity,Is.EqualTo(e.linearVelocity));Assert.That(e.body.angularVelocity,Is.EqualTo(e.angularVelocity));}Assert.That(Get(match,"Running"),Is.EqualTo(false));TestContext.WriteLine("SKEE_SETUP_TEMPLATE equivalent4, simultaneous1..4/hotseat1..8; actual ResetCourse7 then Setup; report leaves current owner7, profiles/models/revisions/original physics unchanged.");
    }
}
#endif
