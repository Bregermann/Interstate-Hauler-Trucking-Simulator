#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class ArenaAppearanceIntegrationTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value){if(owner.GetType().GetField(name,Any) is FieldInfo field)field.SetValue(owner,value);else owner.GetType().GetProperty(name,Any).SetValue(owner,value);}
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
    static object Registry(string name,params object[] args)=>Sports("SportsParticipantRegistry").GetMethod(name).Invoke(null,args);
    Component match;IDisposable lease;GameObject isolation;bool log;Component[] trucks;object[] hosts;string[] ids;
    [TearDown]public void Cleanup(){Time.timeScale=1;if(match!=null)Call(match,log?"SetupScreen":"ChangeSetup");if(isolation!=null)UnityEngine.Object.DestroyImmediate(isolation);Registry("ForgetCache");lease?.Dispose();lease=null;}
    IEnumerator Load(bool rollingLog)
    {
        log=rollingLog;lease=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"ArenaAppearance"+Guid.NewGuid().ToString("N")});Registry("ForgetCache");isolation=new GameObject("Explicit saved arena appearance fixture");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Sports("SportsProbeInputIsolation"));string mode=log?"TruckLogRolling":"TruckWallball";
        EditorSceneManager.LoadSceneInPlayMode("Assets/"+mode+"/Scenes/"+mode+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=(Component)UnityEngine.Object.FindAnyObjectByType(Type.GetType(log?"LWS.TruckLogRolling.LogMatch, Assembly-CSharp":"LWS.TruckWallball.WallballMatch, Assembly-CSharp",true));Assert.That(match,Is.Not.Null);
        if(log){Call(match,"AllBots");Set(Get(match,"setup"),"count",4);Call(match,"NormalizeSetup");trucks=((Array)Get(match,"fighters")).Cast<object>().Select(fighter=>(Component)Get(fighter,"truck")).ToArray();}
        else{Set(match,"blueSize",4);Set(match,"orangeSize",4);trucks=((Array)Get(match,"trucks")).Cast<Component>().ToArray();foreach(var player in (Array)Get(match,"players"))Set(player,"source",Enum.Parse(Get(player,"source").GetType(),"AI"));}
        Assert.That(trucks.Length,Is.EqualTo(log?4:8));hosts=((Array)Get(match,"appearanceHosts")).Cast<object>().ToArray();Assert.That(hosts.Length,Is.EqualTo(trucks.Length),"Invoke BindMissingLogAndWallballHosts first.");ids=new string[trucks.Length];for(int i=0;i<ids.Length;i++){ids[i]=(string)Get(Registry("Create","Same Arena Driver"),"id");Registry("UseInSeat",i,ids[i]);Save(ids[i],i%3==0?"truck.fire":i%3==1?"truck.monster":"novelty.icecream");Assert.That(((Component)hosts[i]).transform,Is.SameAs(trucks[i].transform));Assert.That(Get(hosts[i],"participantSlot"),Is.EqualTo(i));}Call(match,"RefreshParticipants",false);
    }
    static void Save(string id,string body)
    {
        var draft=Registry("GetAppearance",id);bool same=(string)Get(draft,"bodyId")==body&&((IList)Get(draft,"selections")).Count==0;Set(draft,"bodyId",body);((IList)Get(draft,"selections")).Clear();Assert.That(Registry("TrySetAppearance",id,draft),Is.EqualTo(!same));Registry("ForgetCache");Assert.That(Get(Registry("GetAppearance",id),"bodyId"),Is.EqualTo(body));
    }
    static string Configuration(Component component)
    {
        if(component is Rigidbody body)return body.mass+":"+body.centerOfMass.ToString("R")+":"+body.linearDamping+":"+body.angularDamping;
        if(component is Collider shape){string text=shape.isTrigger+":"+shape.contactOffset+":"+(shape.sharedMaterial!=null?shape.sharedMaterial.GetInstanceID():0);if(shape is BoxCollider box)text+=":"+box.center.ToString("R")+":"+box.size.ToString("R");if(shape is MeshCollider mesh)text+=":"+(mesh.sharedMesh!=null?mesh.sharedMesh.GetInstanceID():0)+":"+mesh.convex;return text;}return null;
    }
    Dictionary<Component,string> Physical()=>trucks.SelectMany(truck=>truck.GetComponentsInChildren<Component>(true)).Where(component=>component is Rigidbody||component is Collider).ToDictionary(component=>component,Configuration);
    void SamePhysical(Dictionary<Component,string> snapshot,bool allowWheelInitialization=false)
    {
        var actual=Physical();foreach(var pair in snapshot){Assert.That(pair.Key!=null,Is.True);Assert.That(actual.ContainsKey(pair.Key),Is.True);if(!allowWheelInitialization)Assert.That(actual[pair.Key],Is.EqualTo(pair.Value),"Original physical configuration changed: "+pair.Key.name);}
        if(!allowWheelInitialization){CollectionAssert.AreEquivalent(snapshot.Keys,actual.Keys);return;}
        var permitted=new HashSet<Collider>();foreach(var truck in trucks)foreach(var wheel in (Array)Get(truck,"wheels")){var collider=(Collider)Get(Get(wheel,"wheel"),"meshCollider");if(collider!=null)permitted.Add(collider);}foreach(var added in actual.Keys.Where(component=>!snapshot.ContainsKey(component)))Assert.That(added is Collider collider&&permitted.Contains(collider),Is.True,"Added physical component must be the exact original NWH wheel.meshCollider.");
    }
    string Wheels(){var text=new StringBuilder();foreach(var truck in trucks)foreach(var wheel in (Array)Get(truck,"wheels")){var data=Get(wheel,"wheel");var spring=Get(wheel,"spring");text.Append(((Component)wheel).GetInstanceID()).Append(':').Append(Get(data,"radius")).Append(':').Append(Get(data,"width")).Append(':').Append(Get(spring,"maxLength")).Append(':').Append(Get(spring,"maxForce"));}return text.ToString();}
    IEnumerator StartOriginal()
    {
        Assert.That(log?Call(match,"StartMatch"):Call(match,"StartMatch",false),Is.EqualTo(true));double deadline=Time.realtimeSinceStartupAsDouble+8;
        while(Time.realtimeSinceStartupAsDouble<deadline&&(log?Get(match,"Stage").ToString()=="Countdown":!(bool)Get(match,"ServeReleased")))yield return null;
        Assert.That(log?Get(match,"Stage").ToString()=="Balance":(bool)Get(match,"ServeReleased"),Is.True,"Actual original countdown must activate the original NWH wheels before controlled composition trials.");((Behaviour)match).enabled=false;if(log){foreach(var fighter in (Array)Get(match,"fighters"))Call(fighter,"Freeze");((Rigidbody)Get(match,"log")).isKinematic=true;}else Call(match,"Freeze",true);yield return null;
    }
    IEnumerator StartUncustomizedOriginal()
    {
        var fixedBindings=(Array)Get(match,"appearanceHosts");var originalTruckBindings=log?trucks.Select(truck=>Get(truck,"appearanceHost")).ToArray():null;
        Set(match,"appearanceHosts",Array.CreateInstance(Sports("SportsTruckAppearanceHost"),0));if(log)foreach(var truck in trucks)Set(truck,"appearanceHost",null);
        try{yield return StartOriginal();}
        finally{Set(match,"appearanceHosts",fixedBindings);if(log)for(int i=0;i<trucks.Length;i++)Set(trucks[i],"appearanceHost",originalTruckBindings[i]);}
    }
    void Prepare(){if(log){Call(match,"PrepareAppearances");Call(match,"BeginHeat");((Rigidbody)Get(match,"log")).isKinematic=true;}else Call(match,"ResetServe");}
    void AssertBodies(string first)
    {
        for(int i=0;i<trucks.Length;i++){Assert.That(Get(hosts[i],"ProfileId"),Is.EqualTo(ids[i]));Assert.That(Get(hosts[i],"EffectiveBodyId"),Is.EqualTo(i==0?first:i%3==0?"truck.fire":i%3==1?"truck.monster":"novelty.icecream"));var model=(Transform)Get(hosts[i],"AppearanceRoot");Assert.That(model,Is.Not.Null);Assert.That(model.GetComponentsInChildren<Rigidbody>(true),Is.Empty);Assert.That(model.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(trucks[i].transform.localScale,Is.EqualTo(Vector3.one));Assert.That(((Renderer[])Get(hosts[i],"originalBodyRenderers")).Length,Is.EqualTo(29));Assert.That(((Renderer[])Get(hosts[i],"originalWheelRenderers")).Length,Is.EqualTo(4));Assert.That(((Renderer[])Get(hosts[i],"originalBodyRenderers")).All(renderer=>!renderer.enabled),Is.True);}
    }
    static string Relative(Transform part,Transform root){string path=part.name;while(part.parent!=root){part=part.parent;Assert.That(part,Is.Not.Null);path=part.name+"/"+path;}return path;}
    void AssertGhostPaint(object replay)
    {
        var ghosts=(Transform[])Get(replay,"ghosts");for(int i=0;i<4;i++){var renderers=(Renderer[])Get(hosts[i],"paintRenderers");foreach(var slot in (Array)Get(hosts[i],"paintSlots")){var source=renderers[(int)Get(slot,"rendererIndex")];int index=(int)Get(slot,"materialIndex");var copy=ghosts[i].Find(Relative(source.transform,trucks[i].transform)).GetComponent<Renderer>();CollectionAssert.AreEqual(source.sharedMaterials,copy.sharedMaterials);var a=new MaterialPropertyBlock();var b=new MaterialPropertyBlock();source.GetPropertyBlock(a,index);copy.GetPropertyBlock(b,index);Assert.That(b.GetColor("_BaseColor"),Is.EqualTo(a.GetColor("_BaseColor")));}}
    }
    [UnityTest,Timeout(180000)]public IEnumerator WallballEightActualBodiesPreserveNwhContactBallJumpAndServeAcrossRematch()
    {
        yield return Load(false);var original=Physical();string wheels=Wheels();var ball=Get(match,"ball");var floor=Get(match,"floor");var wall=Get(match,"wall");var jumps=trucks.Select(truck=>Get(truck,"jumpSpeed")).ToArray();var contactCenter=Get(match,"contactCenter");var contactHalf=Get(match,"contactHalfExtents");yield return StartUncustomizedOriginal();SamePhysical(original,true);var physical=Physical();var collisionShapes=trucks.Select(truck=>((Collider[])Get(truck,"CollisionShapes")).ToArray()).ToArray();int points=(int)Get(Get(match,"rules"),"Points");var scores=((int[])Get(Get(match,"rules"),"Score")).ToArray();
        foreach(string body in new[]{"truck.fire","truck.monster","truck.lifted","semi.lowrider","novelty.hotdog","novelty.icecream","semi.standard"}){Save(ids[0],body);Call(match,"RefreshParticipants",false);Prepare();AssertBodies(body);SamePhysical(physical);Assert.That(Wheels(),Is.EqualTo(wheels));Assert.That(Get(match,"ball"),Is.SameAs(ball));Assert.That(Get(match,"floor"),Is.SameAs(floor));Assert.That(Get(match,"wall"),Is.SameAs(wall));Assert.That(Get(match,"contactCenter"),Is.EqualTo(contactCenter));Assert.That(Get(match,"contactHalfExtents"),Is.EqualTo(contactHalf));for(int i=0;i<8;i++){CollectionAssert.AreEquivalent(collisionShapes[i],(Collider[])Get(trucks[i],"CollisionShapes"));Assert.That(Get(trucks[i],"jumpSpeed"),Is.EqualTo(jumps[i]));}Assert.That(Get(Get(match,"rules"),"Points"),Is.EqualTo(points));CollectionAssert.AreEqual(scores,(int[])Get(Get(match,"rules"),"Score"));}
        int revision=(int)Get(hosts[0],"AppearanceRevision");Call(match,"ChangeSetup");Assert.That(Call(match,"StartMatch",true),Is.EqualTo(true));Assert.That(Get(hosts[0],"AppearanceRevision"),Is.EqualTo(revision));SamePhysical(physical);TestContext.WriteLine("WALLBALL_APPEARANCE actual4v4/eightIDs, all7 bodies, real original countdown and NWH activation, stable collider/wheel/contact/jump/ball/serve refs and rematch. Focused controlled composition, not full physical match or visual acceptance.");
    }
    [UnityTest,Timeout(180000)]public IEnumerator LogFourBodiesRefreshOnlyTruckReplayTreesAndPreserveActualBuoyancyLogAndBraceAuthority()
    {
        yield return Load(true);var original=Physical();string wheels=Wheels();var actualLog=(Rigidbody)Get(match,"log");var buoyancy=Get(match,"buoyancy");var proxy=Get(buoyancy,"proxy");var fighters=((Array)Get(match,"fighters")).Cast<object>().ToArray();var splashes=fighters.Select(fighter=>Get(fighter,"splash")).ToArray();var scrapes=fighters.Select(fighter=>Get(fighter,"scrape")).ToArray();var replay=Get(match,"replay");var sources=((Transform[])Get(replay,"sources")).ToArray();var ghosts=((Transform[])Get(replay,"ghosts")).ToArray();var logParts=ghosts[4].GetComponentsInChildren<Transform>(true);var logMeshes=ghosts[4].GetComponentsInChildren<MeshFilter>(true).Select(filter=>filter.sharedMesh).ToArray();yield return StartUncustomizedOriginal();SamePhysical(original,true);var physical=Physical();var logConfig=Configuration(actualLog);
        foreach(string body in new[]{"truck.fire","truck.monster","truck.lifted","semi.lowrider","novelty.hotdog","novelty.icecream","semi.standard"}){Save(ids[0],body);Call(match,"RefreshParticipants",false);Prepare();AssertBodies(body);SamePhysical(physical);Assert.That(Wheels(),Is.EqualTo(wheels));Assert.That(Get(match,"log"),Is.SameAs(actualLog));Assert.That(Configuration(actualLog),Is.EqualTo(logConfig));Assert.That(Get(match,"buoyancy"),Is.SameAs(buoyancy));Assert.That(Get(buoyancy,"proxy"),Is.SameAs(proxy));CollectionAssert.AreEqual(splashes,fighters.Select(fighter=>Get(fighter,"splash")));CollectionAssert.AreEqual(scrapes,fighters.Select(fighter=>Get(fighter,"scrape")));Set(match,"Clock",1f);Call(match,"CaptureAppearanceReplay");Set(match,"Clock",1.2f);Call(match,"CaptureAppearanceReplay");AssertGhostPaint(replay);CollectionAssert.AreEqual(sources,(Transform[])Get(replay,"sources"));CollectionAssert.AreEqual(ghosts,(Transform[])Get(replay,"ghosts"));CollectionAssert.AreEqual(logParts,ghosts[4].GetComponentsInChildren<Transform>(true));CollectionAssert.AreEqual(logMeshes,ghosts[4].GetComponentsInChildren<MeshFilter>(true).Select(filter=>filter.sharedMesh));}
        var buffers=Get(replay,"roots");Prepare();Set(match,"Clock",1f);Call(match,"CaptureAppearanceReplay");Set(match,"Clock",1.2f);Call(match,"CaptureAppearanceReplay");Assert.That(Get(replay,"roots"),Is.SameAs(buffers));Call(replay,"Begin",0f,float.PositiveInfinity);Assert.That(Get(replay,"Playing"),Is.EqualTo(true));Assert.That(ghosts[4].gameObject.activeSelf,Is.True,"Original recorded mask includes actual log source4.");Assert.That(Call(replay,"RefreshAppearanceSources",new[]{0,1,2,3},true),Is.EqualTo(false));Assert.That(Get(replay,"roots"),Is.SameAs(buffers));Call(replay,"End");
        int revision=(int)Get(hosts[0],"AppearanceRevision");Call(match,"SetupScreen");Assert.That(Call(match,"StartMatch"),Is.EqualTo(true));Assert.That(Get(hosts[0],"AppearanceRevision"),Is.EqualTo(revision));SamePhysical(physical);TestContext.WriteLine("LOG_APPEARANCE actual4IDs/all7 bodies, original real countdown, collider/wheel/brace/physical buoyant log+proxy/splash/scrape refs retained. Explicit replay sources0..3 refreshed, actual log source4/ghost topology/mesh/mask unchanged, native paint and buffer reuse/playing guard/rematch exercised. Controlled samples, no full-match/art/hardware claim.");
    }
}
#endif
