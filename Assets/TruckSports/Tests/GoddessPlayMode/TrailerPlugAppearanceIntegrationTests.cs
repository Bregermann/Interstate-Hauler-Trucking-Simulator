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

public sealed class TrailerPlugAppearanceIntegrationTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
    static object Registry(string name,params object[] args)=>Sports("SportsParticipantRegistry").GetMethod(name).Invoke(null,args);
    Component match,truck;object rig,host;IDisposable lease;GameObject isolation;string[] ids;Rigidbody trailer;
    [TearDown]public void Cleanup(){Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");if(isolation!=null)UnityEngine.Object.DestroyImmediate(isolation);Registry("ForgetCache");lease?.Dispose();lease=null;}
    IEnumerator Load()
    {
        lease=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"TrailerAppearance"+Guid.NewGuid().ToString("N")});Registry("ForgetCache");isolation=new GameObject("Explicit Trailer Plug appearance fixture");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Sports("SportsProbeInputIsolation"));
        EditorSceneManager.LoadSceneInPlayMode("Assets/TruckTrailerPlug/Scenes/TruckTrailerPlug.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
        match=(Component)UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS.TruckTrailerPlug.TrailerPlugMatch, Assembly-CSharp",true));Assert.That(match,Is.Not.Null);rig=Get(match,"rig");truck=(Component)Get(rig,"truck");trailer=(Rigidbody)Get(rig,"trailer");host=Get(truck,"appearanceHost");Assert.That(host,Is.Not.Null,"Invoke BindMissingTrailerPlugHost first.");
        Assert.That(Get(host,"participantSlot"),Is.EqualTo(-1));CollectionAssert.DoesNotContain(((Array)Get(match,"appearanceHosts")).Cast<object>().ToArray(),host);Call(match,"AllBots");Set(Get(match,"setup"),"count",4);Call(match,"NormalizeSetup");ids=new string[4];
        for(int i=0;i<4;i++){ids[i]=(string)Get(Registry("Create","Same Trailer Driver"),"id");Registry("UseInSeat",i,ids[i]);Save(ids[i],i%2==0?"truck.fire":"truck.monster");}Call(match,"RefreshParticipants",false);
    }
    static void Save(string id,string body)
    {
        var draft=Registry("GetAppearance",id);bool same=(string)Get(draft,"bodyId")==body&&((IList)Get(draft,"selections")).Count==0;Set(draft,"bodyId",body);((IList)Get(draft,"selections")).Clear();Assert.That(Registry("TrySetAppearance",id,draft),Is.EqualTo(!same));Registry("ForgetCache");Assert.That(Get(Registry("GetAppearance",id),"bodyId"),Is.EqualTo(body));
    }
    static IEnumerable<Component> Parts(Component tractor,Rigidbody trailer)=>tractor.GetComponentsInChildren<Component>(true).Concat(trailer.GetComponentsInChildren<Component>(true));
    static string Configuration(Component component,bool includeInertia=true)
    {
        if(component is Rigidbody body)return body.mass+":"+body.centerOfMass.ToString("R")+(includeInertia?":"+body.inertiaTensor.ToString("R"):"")+":"+body.linearDamping+":"+body.angularDamping;
        if(component is Collider shape){string text=shape.isTrigger+":"+shape.contactOffset+":"+(shape.sharedMaterial!=null?shape.sharedMaterial.GetInstanceID():0);if(shape is BoxCollider box)text+=":"+box.center.ToString("R")+":"+box.size.ToString("R");if(shape is MeshCollider mesh)text+=":"+(mesh.sharedMesh!=null?mesh.sharedMesh.GetInstanceID():0)+":"+mesh.convex;return text;}
        return null;
    }
    static Dictionary<Component,string> Snapshot(Component truck,Rigidbody trailer,bool includeInertia=true)=>Parts(truck,trailer).Where(component=>component is Rigidbody||component is Collider).ToDictionary(component=>component,component=>Configuration(component,includeInertia));
    static void Unchanged(Dictionary<Component,string> snapshot,bool includeInertia=true){foreach(var pair in snapshot){Assert.That(pair.Key!=null,Is.True,"Original physical component destroyed.");Assert.That(Configuration(pair.Key,includeInertia),Is.EqualTo(pair.Value),"Original physical configuration changed: "+pair.Key.name);}}
    static void OnlyVendorWheelInitialization(Dictionary<Component,string> originals,Component truck,Rigidbody trailer)
    {
        var colliders=new HashSet<Collider>();foreach(var component in Parts(truck,trailer).Where(component=>component!=null&&component.GetType().FullName=="NWH.WheelController3D.WheelController")){var wheel=Get(component,"wheel");var collider=(Collider)Get(wheel,"meshCollider");if(collider!=null)colliders.Add(collider);}
        foreach(var added in Snapshot(truck,trailer).Keys.Where(component=>!originals.ContainsKey(component)))Assert.That(added is Collider collider&&colliders.Contains(collider),Is.True,"Only exact original NWH wheel.meshCollider initialization may add physical components: "+added);
    }
    string Wheels()
    {
        var text=new StringBuilder();foreach(var wheel in Parts(truck,trailer).Where(component=>component!=null&&component.GetType().FullName=="NWH.WheelController3D.WheelController")){var data=Get(wheel,"wheel");var spring=Get(wheel,"spring");text.Append(wheel.GetInstanceID()).Append(':').Append(Get(data,"radius")).Append(':').Append(Get(data,"width")).Append(':').Append(Get(spring,"maxLength")).Append(':').Append(Get(spring,"maxForce"));}return text.ToString();
    }
    IEnumerator StartActualSwing()
    {
        Assert.That(Call(match,"StartMatch"),Is.EqualTo(true));double until=Time.realtimeSinceStartupAsDouble+8;while(Get(match,"Phase").ToString()=="CountIn"&&Time.realtimeSinceStartupAsDouble<until)yield return null;Assert.That(Get(match,"Phase").ToString(),Is.EqualTo("Swing"),"Observe original real countdown and NWH activation, not a synthetic release.");((Behaviour)match).enabled=false;Call(rig,"Hold");yield return null;
    }
    IEnumerator PrepareSample()
    {
        Call(match,"BeginAttempt");yield return null;yield return null;Assert.That(Get(rig,"Attached"),Is.EqualTo(true));Assert.That((float)Get(rig,"HitchSeparation"),Is.LessThan(.03f));Call(rig,"BeginSwing");yield return new WaitForFixedUpdate();Call(rig,"Hold");Set(match,"totalAge",3.1f);Call(match,"CaptureReplay");Set(match,"totalAge",3.3f);Call(match,"CaptureReplay");
    }
    static string Relative(Transform part,Transform root){string path=part.name;while(part.parent!=root){part=part.parent;Assert.That(part,Is.Not.Null);path=part.name+"/"+path;}return path;}
    void AssertSourceAndGhost(object replay)
    {
        var ghost=((Transform[])Get(replay,"ghosts"))[0];var model=(Transform)Get(host,"AppearanceRoot");Assert.That(model,Is.Not.Null);var copy=ghost.Find(Relative(model,truck.transform));Assert.That(copy,Is.Not.Null);Assert.That(copy.GetComponentsInChildren<Rigidbody>(true),Is.Empty);Assert.That(copy.GetComponentsInChildren<Collider>(true),Is.Empty);
        var renderers=(Renderer[])Get(host,"paintRenderers");foreach(var slot in (Array)Get(host,"paintSlots")){var source=renderers[(int)Get(slot,"rendererIndex")];int index=(int)Get(slot,"materialIndex");var target=ghost.Find(Relative(source.transform,truck.transform)).GetComponent<Renderer>();CollectionAssert.AreEqual(source.sharedMaterials,target.sharedMaterials);var a=new MaterialPropertyBlock();var b=new MaterialPropertyBlock();source.GetPropertyBlock(a,index);target.GetPropertyBlock(b,index);Assert.That(b.GetColor("_BaseColor"),Is.EqualTo(a.GetColor("_BaseColor")));}
    }
    [UnityTest,Timeout(180000)]public IEnumerator AllSevenSharedTurnBodiesKeepMatchedPhysicalHitchTrailerBungeeAndSavedIdentity()
    {
        yield return Load();var originals=Snapshot(truck,trailer,false);string wheels=Wheels();var spring=(SpringJoint)Get(rig,"spring");var anchor=(Transform)Get(rig,"anchor");var cable=(LineRenderer)Get(rig,"cable");var hitch=Get(rig,"tractorHitch");var module=Get(rig,"trailerModule");var a=(Transform)Get(Get(hitch,"module"),"attachmentPoint");var b=(Transform)Get(Get(module,"module"),"attachmentPoint");Vector3 localA=a.localPosition,localB=b.localPosition,springAnchor=spring.anchor,connected=spring.connectedAnchor;float springForce=spring.spring,damper=spring.damper,rest=(float)Get(rig,"restLength");var sourceRoots=((Transform[])Get(Get(match,"replay"),"sources")).ToArray();var ghostRoots=((Transform[])Get(Get(match,"replay"),"ghosts")).ToArray();
        // Observe original PhysX/NWH startup without appearance composition.
        // The held tractor initially exposes a placeholder (1,1,1) inertia;
        // its real tensor is generated on first dynamic activation.
        Set(truck,"appearanceHost",null);try{yield return StartActualSwing();}finally{Set(truck,"appearanceHost",host);}
        Unchanged(originals,false);OnlyVendorWheelInitialization(originals,truck,trailer);var initialized=Snapshot(truck,trailer);foreach(var body in initialized.Keys.OfType<Rigidbody>())Assert.That(body.inertiaTensor.x>0&&body.inertiaTensor.y>0&&body.inertiaTensor.z>0,Is.True,"Real initialized inertia must be positive before first custom composition.");
        foreach(string body in new[]{"truck.fire","truck.monster","truck.lifted","semi.lowrider","novelty.hotdog","novelty.icecream","semi.standard"})
        {
            Save(ids[0],body);Call(match,"RefreshParticipants",false);yield return PrepareSample();Assert.That(Get(host,"ProfileId"),Is.EqualTo(ids[0]));Assert.That(Get(host,"EffectiveBodyId"),Is.EqualTo(body));Assert.That(Get(truck,"slot"),Is.EqualTo(0));Assert.That(((Renderer[])Get(host,"originalBodyRenderers")).All(renderer=>!renderer.enabled),Is.True);Assert.That(((Renderer[])Get(host,"originalBodyRenderers")).Length,Is.EqualTo(29));Assert.That(((Renderer[])Get(host,"originalWheelRenderers")).Length,Is.EqualTo(4));Assert.That(truck.transform.localScale,Is.EqualTo(Vector3.one));var model=(Transform)Get(host,"AppearanceRoot");Assert.That(model.GetComponentsInChildren<Rigidbody>(true),Is.Empty);Assert.That(model.GetComponentsInChildren<Collider>(true),Is.Empty);
            Unchanged(initialized);Assert.That(Wheels(),Is.EqualTo(wheels));Assert.That(Get(rig,"spring"),Is.SameAs(spring));Assert.That(Get(rig,"anchor"),Is.SameAs(anchor));Assert.That(Get(rig,"cable"),Is.SameAs(cable));Assert.That(Get(rig,"tractorHitch"),Is.SameAs(hitch));Assert.That(Get(rig,"trailerModule"),Is.SameAs(module));Assert.That(a.localPosition,Is.EqualTo(localA));Assert.That(b.localPosition,Is.EqualTo(localB));Assert.That(spring.anchor,Is.EqualTo(springAnchor));Assert.That(spring.connectedAnchor,Is.EqualTo(connected));Assert.That(spring.spring,Is.EqualTo(springForce));Assert.That(spring.damper,Is.EqualTo(damper));Assert.That(spring.maxDistance,Is.EqualTo(rest));CollectionAssert.AreEqual(sourceRoots,(Transform[])Get(Get(match,"replay"),"sources"));CollectionAssert.AreEqual(ghostRoots,(Transform[])Get(Get(match,"replay"),"ghosts"));AssertSourceAndGhost(Get(match,"replay"));
        }
        int revision=(int)Get(host,"AppearanceRevision");Call(match,"SetupScreen");Assert.That(Call(match,"StartMatch"),Is.EqualTo(true));Assert.That(Get(host,"AppearanceRevision"),Is.EqualTo(revision));Unchanged(initialized);
        TestContext.WriteLine("TRAILER_APPEARANCE actual original countdown, all7 actual bodies on shared physical tractor; matched NWH hitch/trailer/spring/anchors/wheel geometry/source+ghost roots retained; reload/rematch identity proved. Controlled composition samples, no complete-match/hardware/art acceptance claim.");
    }
    [UnityTest,Timeout(180000)]public IEnumerator AlternatingOwnersRefreshBothRecordedTreesAndRestoreExactLiveVisibilityOnSkipAndSetup()
    {
        yield return Load();yield return StartActualSwing();var replay=Get(match,"replay");var ghostRoots=((Transform[])Get(replay,"ghosts")).ToArray();var oldCab=ghostRoots[0].Find("Cab");var oldLights=ghostRoots[0].Find("Lights");yield return PrepareSample();var buffers=Get(replay,"roots");yield return PrepareSample();Assert.That(Get(replay,"roots"),Is.SameAs(buffers),"Same owner/body/topology reuses original pose buffers.");
        var trailerRenderer=trailer.GetComponentsInChildren<Renderer>(true).First();trailerRenderer.enabled=false;var smoke=((Renderer[])Get(host,"auxiliaryExteriorRenderers")).Single();smoke.enabled=false;((Transform)Get(host,"AppearanceRoot")).GetComponentsInChildren<Renderer>(true).First(renderer=>renderer.enabled).enabled=false;var live=truck.GetComponentsInChildren<Renderer>(true).Concat(trailer.GetComponentsInChildren<Renderer>(true)).ToArray();var states=live.Select(renderer=>renderer.enabled).ToArray();
        Call(match,"BeginReplay");Assert.That(Get(replay,"Playing"),Is.EqualTo(true));Assert.That(live.All(renderer=>!renderer.enabled),Is.True);Assert.That(Call(replay,"RefreshAppearanceSources",new[]{0,1},true),Is.EqualTo(false));Call(match,"StopReplay");CollectionAssert.AreEqual(states,live.Select(renderer=>renderer.enabled));Assert.That(((Renderer[])Get(host,"originalBodyRenderers")).All(renderer=>!renderer.enabled),Is.True);
        // Advance through the existing rule authority, without waiting for a
        // complete crowd/flight simulation in this focused appearance fixture.
        var rules=Get(match,"rules");Call(rules,"Release");Call(rules,"Resolve",0f,false,false,0f,1f);Call(match,"NextAttempt");yield return null;yield return null;Assert.That(Get(rules,"Participant"),Is.EqualTo(1));Assert.That(Get(truck,"slot"),Is.EqualTo(1));Assert.That(Get(host,"ProfileId"),Is.EqualTo(ids[1]));Assert.That(Get(host,"EffectiveBodyId"),Is.EqualTo("truck.monster"));Call(rig,"BeginSwing");yield return new WaitForFixedUpdate();Call(rig,"Hold");Set(match,"totalAge",3.1f);Call(match,"CaptureReplay");Set(match,"totalAge",3.3f);Call(match,"CaptureReplay");Assert.That(Get(replay,"roots"),Is.Not.SameAs(buffers));CollectionAssert.AreEqual(ghostRoots,(Transform[])Get(replay,"ghosts"));Assert.That(ghostRoots[0].Find("Cab"),Is.SameAs(oldCab));Assert.That(ghostRoots[0].Find("Lights"),Is.SameAs(oldLights));AssertSourceAndGhost(replay);
        var velocity=trailer.linearVelocity;var angular=trailer.angularVelocity;Assert.That(Call(rig,"Release"),Is.EqualTo(true));Assert.That(trailer.linearVelocity,Is.EqualTo(velocity));Assert.That(trailer.angularVelocity,Is.EqualTo(angular));Assert.That(Call(rig,"Release"),Is.EqualTo(false));
        live=truck.GetComponentsInChildren<Renderer>(true).Concat(trailer.GetComponentsInChildren<Renderer>(true)).ToArray();states=live.Select(renderer=>renderer.enabled).ToArray();Call(match,"BeginReplay");Call(match,"SetupScreen");CollectionAssert.AreEqual(states,live.Select(renderer=>renderer.enabled));Assert.That(Get(replay,"Playing"),Is.EqualTo(false));Assert.That(ghostRoots.All(ghost=>!ghost.gameObject.activeSelf),Is.True);Registry("ForgetCache");Assert.That(Get(Registry("GetAppearance",ids[0]),"bodyId"),Is.EqualTo("truck.fire"));Assert.That(Get(Registry("GetAppearance",ids[1]),"bodyId"),Is.EqualTo("truck.monster"));
        TestContext.WriteLine("TRAILER_TURN_REPLAY two stable duplicate-name owners, same-body buffer reuse/body-change batch refresh, exact hidden live states on replay exit/setup, original ghost Cab/Lights/root identity, no physical momentum mutation on real hitch release. Rule advancement is explicitly controlled.");
    }
}
#endif
