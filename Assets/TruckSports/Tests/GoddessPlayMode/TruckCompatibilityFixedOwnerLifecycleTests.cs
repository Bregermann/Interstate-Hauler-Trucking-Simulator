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

// Saved actors and real match/setup boundaries, with pure reports between them.
// This does not claim full-match or human cosmetic acceptance.
public sealed class TruckCompatibilityFixedOwnerLifecycleTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
    static object Registry(string method,params object[] args)=>Sports("SportsParticipantRegistry").GetMethod(method).Invoke(null,args);
    static string Stored()=>(string)Sports("SportsPrefs").GetMethod("GetString").Invoke(null,new object[]{"TruckSports.Participants.v1",""});
    IDisposable prefs;GameObject isolation;object match;
    [TearDown]public void Cleanup(){if(match is UnityEngine.Object live&&live!=null)Call(match,"SetupScreen");match=null;if(isolation!=null)UnityEngine.Object.DestroyImmediate(isolation);Time.timeScale=1;Registry("ForgetCache");prefs?.Dispose();prefs=null;}
    [UnityTest,Timeout(180000)]public IEnumerator KeepawayFourRealOwnersRemainPureAcrossRematch()=>Run("TruckKeepaway",4);
    [UnityTest,Timeout(180000)]public IEnumerator KeepawayCabFourRealOwnersRemainPureAcrossRematch()=>Run("TruckKeepawayFirstPerson",4);
    [UnityTest,Timeout(180000)]public IEnumerator MattressEightStaticWheelOwnersRemainPureAcrossRematch()=>Run("TruckMattressRacing",8);
    [UnityTest,Timeout(180000)]public IEnumerator MinefieldEightRealOwnersRemainPureAcrossRematch()=>Run("TruckMinefield",8);
    [UnityTest,Timeout(180000)]public IEnumerator DodgeballEightFixedTeamOwnersRemainPureAcrossRematch()=>Run("TruckTrampolineDodgeball",8);
    [UnityTest,Timeout(180000)]public IEnumerator LimboEightLocalResetOrderedHostsRemainPureAcrossRematch()=>Run("TruckLimbo",8);
    [UnityTest,Timeout(180000)]public IEnumerator KickballTwelveRoleIndependentOwnersRemainPureAcrossRematch()=>Run("TruckKickball",12);
    IEnumerator Run(string mode,int count)
    {
        prefs=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"FixedFit"+Guid.NewGuid().ToString("N")});Registry("ForgetCache");isolation=new GameObject("Fixed owner fit input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Sports("SportsProbeInputIsolation"));string folder=mode=="TruckKeepawayFirstPerson"?"TruckKeepaway":mode;var scene=EditorSceneManager.LoadSceneInPlayMode("Assets/"+folder+"/Scenes/"+mode+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
        var source=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren(Sports("SportsTruckCompatibilitySource"),true)).Cast<Component>().Single();match=Get(source,"session");Call(match,"AllBots");Set(match,"weather",0);if(mode=="TruckKickball")Set(match,"option",3);if(mode=="TruckTrampolineDodgeball"){Set(match,"teamA",4);Set(match,"teamB",4);}Set(Get(match,"setup"),"count",count);Call(match,"NormalizeSetup");Assert.That(Get(source,"Count"),Is.EqualTo(count));var ids=new string[count];for(int i=0;i<count;i++){ids[i]=(string)Get(Registry("Create","Same Driver"),"id");Registry("UseInSeat",i,ids[i]);}Call(match,"RefreshParticipants",false);
        var author=Type.GetType("LWS.TruckSports.Editor.SportsTruckCompatibilityFixedOwnerAuthoring, Assembly-CSharp-Editor",true);var prove=author.GetMethod("ProvenMap");var hosts=((Array)prove.Invoke(null,new[]{match})).Cast<object>().Where(host=>host!=null).ToArray();Assert.That(hosts.Length,Is.EqualTo(count));CollectionAssert.AreEqual((Array)Get(source,"participantHosts"),(Array)prove.Invoke(null,new[]{match}),"Run explicit fixed-owner host authors, then the compatibility installer.");if(mode=="TruckLimbo")Assert.That(((Array)Get(match,"appearanceHosts")).Length,Is.Zero);
        for(int cycle=0;cycle<3;cycle++)
        {
            Assert.That(Get(source,"IsSetup"),Is.EqualTo(true));string bytes=Stored();var revisions=hosts.Select(h=>Get(h,"AppearanceRevision")).ToArray();var models=hosts.Select(h=>Get(h,"AppearanceRoot")).ToArray();var profiles=hosts.Select(h=>Get(h,"ProfileId")).ToArray();var bodies=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Rigidbody>(true)).Select(body=>(body,body.position,body.rotation,body.linearVelocity,body.angularVelocity,body.isKinematic,body.detectCollisions)).ToArray();int transforms=scene.GetRootGameObjects().Sum(root=>root.GetComponentsInChildren<Transform>(true).Length);
            var choices=((Array)Get(Get(hosts[0],"catalog"),"bodies")).Cast<object>().ToArray();Assert.That(choices.Length,Is.EqualTo(7));for(int i=0;i<count;i++){Assert.That(Call(source,"Host",i),Is.SameAs(hosts[i]));Assert.That(Get(Call(source,"Read",i),"profileId"),Is.EqualTo(ids[i]));foreach(var body in choices){var draft=Registry("GetAppearance",ids[i]);Set(draft,"bodyId",Get(body,"id"));var view=Call(source,"Evaluate",i,ids[i],draft);Assert.That(Get(view,"hasHost"),Is.EqualTo(true));Assert.That(Get(view,"canApply"),Is.EqualTo(true));Assert.That(Get(view,"bodyFallback"),Is.EqualTo(false));StringAssert.Contains((string)Get(body,"id"),(string)Get(view,"effective"));}}
            CollectionAssert.AreEqual(revisions,hosts.Select(h=>Get(h,"AppearanceRevision")));CollectionAssert.AreEqual(models,hosts.Select(h=>Get(h,"AppearanceRoot")));CollectionAssert.AreEqual(profiles,hosts.Select(h=>Get(h,"ProfileId")));Assert.That(Stored(),Is.EqualTo(bytes));Assert.That(scene.GetRootGameObjects().Sum(root=>root.GetComponentsInChildren<Transform>(true).Length),Is.EqualTo(transforms));foreach(var entry in bodies){Assert.That(entry.body.position,Is.EqualTo(entry.position));Assert.That(entry.body.rotation,Is.EqualTo(entry.rotation));Assert.That(entry.body.linearVelocity,Is.EqualTo(entry.linearVelocity));Assert.That(entry.body.angularVelocity,Is.EqualTo(entry.angularVelocity));Assert.That(entry.body.isKinematic,Is.EqualTo(entry.isKinematic));Assert.That(entry.body.detectCollisions,Is.EqualTo(entry.detectCollisions));}CollectionAssert.AreEqual(hosts,((Array)prove.Invoke(null,new[]{match})).Cast<object>().Where(host=>host!=null));
            if(cycle<2){Assert.That(Call(match,"StartMatch"),Is.EqualTo(true));yield return null;yield return null;Call(match,"SetupScreen");yield return null;Assert.That(Get(match,"Running"),Is.EqualTo(false));Assert.That(((Behaviour)match).enabled,Is.True);}
        }
        TestContext.WriteLine("FIXED_SETUP_FIT "+mode+" owners="+count+" all7 bodies; actual StartMatch/Setup twice, stable duplicate-name IDs; pure reports leave models/revisions/profile bytes/transforms/physics unchanged.");
    }
}
#endif
