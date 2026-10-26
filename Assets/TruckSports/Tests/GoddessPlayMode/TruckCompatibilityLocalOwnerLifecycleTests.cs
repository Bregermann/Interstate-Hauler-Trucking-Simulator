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
public sealed class TruckCompatibilityLocalOwnerLifecycleTests
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
    [UnityTest,Timeout(180000)]public IEnumerator TerrierReportsAllTwelveActualBridgeOwnersWithoutBuildingHeatOrWreckPools()=>Run("TruckTerrierRacing");
    [UnityTest,Timeout(180000)]public IEnumerator DockReportsEverySetupDriverAgainstTheActualValidatedSplitTurnHost()=>Run("TruckDockDiving");
    IEnumerator Run(string mode)
    {
        prefs=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"LocalFit"+Guid.NewGuid().ToString("N")});Registry("ForgetCache");isolation=new GameObject("Local owner setup fit isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Sports("SportsProbeInputIsolation"));var scene=EditorSceneManager.LoadSceneInPlayMode("Assets/"+mode+"/Scenes/"+mode+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
        bool terrier=mode=="TruckTerrierRacing";int count=terrier?12:8;var source=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren(Sports("SportsTruckCompatibilitySource"),true)).Cast<Component>().Single();var match=Get(source,"session");Set(Get(match,"setup"),"count",count);Call(match,"NormalizeSetup");Call(match,"AllBots");var ids=new string[count];for(int i=0;i<count;i++){ids[i]=(string)Get(Registry("Create","Same Driver"),"id");Registry("UseInSeat",i,ids[i]);}Call(match,"RefreshParticipants",false);var author=Type.GetType("LWS.TruckSports.Editor.SportsTruckCompatibilityLocalOwnerAuthoring, Assembly-CSharp-Editor",true);var value=author.GetMethod(terrier?"ProvenTerrierMap":"ProvenDockTemplate").Invoke(null,new[]{match});var hosts=terrier?((Array)value).Cast<object>().ToArray():new[]{Get(Get(match,"truck"),"appearanceHost")}.Concat(((Array)Get(match,"staged")).Cast<object>().Select(t=>Get(t,"appearanceHost"))).ToArray();if(terrier){CollectionAssert.AreEqual((Array)Get(source,"participantHosts"),(Array)value);Assert.That(((Array)Get(match,"appearanceHosts")).Length,Is.Zero);}else Assert.That(Get(source,"sharedTurnHost"),Is.SameAs(value));Assert.That(Get(source,"IsSetup"),Is.EqualTo(true));string bytes=Stored();var revisions=hosts.Select(h=>Get(h,"AppearanceRevision")).ToArray();var models=hosts.Select(h=>Get(h,"AppearanceRoot")).ToArray();var profiles=hosts.Select(h=>Get(h,"ProfileId")).ToArray();var presenter=Get(match,terrier?"appearanceBridge":"appearanceSplit");int builds=terrier?(int)Get(presenter,"PoolBuilds"):0;int heats=terrier?(int)Get(presenter,"PreparedHeats"):0;int transforms=scene.GetRootGameObjects().Sum(root=>root.GetComponentsInChildren<Transform>(true).Length);var bodies=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Rigidbody>(true)).Select(body=>(body,body.position,body.rotation,body.linearVelocity,body.angularVelocity,body.isKinematic,body.detectCollisions)).ToArray();var choices=((Array)Get(Get(hosts[0],"catalog"),"bodies")).Cast<object>().ToArray();
        for(int repeat=0;repeat<3;repeat++)for(int i=0;i<count;i++){Assert.That(Call(source,"Host",i),Is.SameAs(terrier?hosts[i]:hosts[0]));Assert.That(Get(Call(source,"Read",i),"profileId"),Is.EqualTo(ids[i]));foreach(var body in choices){var draft=Registry("GetAppearance",ids[i]);Set(draft,"bodyId",Get(body,"id"));var view=Call(source,"Evaluate",i,ids[i],draft);Assert.That(Get(view,"hasHost"),Is.EqualTo(true));Assert.That(Get(view,"canApply"),Is.EqualTo(true));Assert.That(Get(view,"bodyFallback"),Is.EqualTo(false));StringAssert.Contains((string)Get(body,"id"),(string)Get(view,"effective"));}}
        if(!terrier)for(int n=1;n<=8;n++){Set(Get(match,"setup"),"count",n);Call(match,"NormalizeSetup");Assert.That(Get(source,"Count"),Is.EqualTo(n));Assert.That(Call(source,"Host",n-1),Is.SameAs(hosts[0]));Assert.That(Call(source,"Host",n),Is.Null);}CollectionAssert.AreEqual(revisions,hosts.Select(h=>Get(h,"AppearanceRevision")));CollectionAssert.AreEqual(models,hosts.Select(h=>Get(h,"AppearanceRoot")));CollectionAssert.AreEqual(profiles,hosts.Select(h=>Get(h,"ProfileId")));Assert.That(Stored(),Is.EqualTo(bytes));Assert.That(scene.GetRootGameObjects().Sum(root=>root.GetComponentsInChildren<Transform>(true).Length),Is.EqualTo(transforms));if(terrier){Assert.That(Get(presenter,"PoolBuilds"),Is.EqualTo(builds));Assert.That(Get(presenter,"PreparedHeats"),Is.EqualTo(heats));}foreach(var e in bodies){Assert.That(e.body.position,Is.EqualTo(e.position));Assert.That(e.body.rotation,Is.EqualTo(e.rotation));Assert.That(e.body.linearVelocity,Is.EqualTo(e.linearVelocity));Assert.That(e.body.angularVelocity,Is.EqualTo(e.angularVelocity));Assert.That(e.body.isKinematic,Is.EqualTo(e.isKinematic));Assert.That(e.body.detectCollisions,Is.EqualTo(e.detectCollisions));}Assert.That(Get(match,"Running"),Is.EqualTo(false));TestContext.WriteLine("LOCAL_SETUP_FIT "+mode+" actualowners="+count+" all7 bodies; pure reports preserve preferences/models/revisions/physics and create no pools/heat/split rendering.");
    }
}
#endif
