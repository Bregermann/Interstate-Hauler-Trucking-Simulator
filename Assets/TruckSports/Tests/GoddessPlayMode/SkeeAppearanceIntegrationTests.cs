#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class SkeeAppearanceIntegrationTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value){if(owner.GetType().GetField(name,Any) is FieldInfo field)field.SetValue(owner,value);else owner.GetType().GetProperty(name,Any).SetValue(owner,value);}
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
    static object Registry(string name,params object[] args)=>Sports("SportsParticipantRegistry").GetMethod(name).Invoke(null,args);
    Component match;IDisposable lease;GameObject isolation;bool hotSeat;object[] courses;Component[] trucks;object[] hosts;string[] ids;
    [TearDown]public void Cleanup(){Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");if(isolation!=null)UnityEngine.Object.DestroyImmediate(isolation);Registry("ForgetCache");lease?.Dispose();lease=null;}
    static void Save(string id,string body){var draft=Registry("GetAppearance",id);Set(draft,"bodyId",body);((IList)Get(draft,"selections")).Clear();Registry("TrySetAppearance",id,draft);Registry("ForgetCache");Assert.That(Get(Registry("GetAppearance",id),"bodyId"),Is.EqualTo(body));}
    IEnumerator Load(bool shared)
    {
        hotSeat=shared;lease=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"SkeeAppearance"+Guid.NewGuid().ToString("N")});Registry("ForgetCache");isolation=new GameObject("Actual turn-owned Skee appearance fixture");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Sports("SportsProbeInputIsolation"));EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSkeeBall/Scenes/TruckSkeeBall.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=(Component)UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS.TruckSkeeBall.SkeeMatch, Assembly-CSharp",true));Assert.That(match,Is.Not.Null);Set(match,"option",shared?1:0);Set(Get(match,"setup"),"count",shared?8:4);Call(match,"AllBots");Call(match,"NormalizeSetup");Assert.That(((Array)Get(match,"appearanceHosts")),Is.Empty,"Physical lanes must not be bound as fixed profile slots.");courses=((Array)Get(match,"courses")).Cast<object>().ToArray();trucks=courses.Select(r=>(Component)Get(r,"truck")).ToArray();hosts=trucks.Select(t=>Get(t,"appearanceHost")).ToArray();Assert.That(hosts.All(h=>h!=null),Is.True,"Run BindMissingSkeeTurnHosts first.");ids=new string[shared?8:4];for(int i=0;i<ids.Length;i++){ids[i]=(string)Get(Registry("Create","Same Skee Driver"),"id");Registry("UseInSeat",i,ids[i]);Save(ids[i],i%2==0?"truck.fire":"novelty.icecream");}for(int i=0;i<4;i++){Assert.That(((Component)hosts[i]).transform,Is.SameAs(trucks[i].transform));Assert.That(Get(hosts[i],"participantSlot"),Is.EqualTo(-1));Assert.That(((Transform)Get(hosts[i],"presentationParent")).gameObject.layer,Is.EqualTo(10+i));Assert.That(((Transform)Get(hosts[i],"presentationParent")).parent,Is.SameAs(Get(trucks[i],"visuals")));}Call(match,"RefreshParticipants",false);
    }
    IEnumerable<Component> Physical()=>trucks.SelectMany(t=>t.GetComponentsInChildren<Component>(true)).Where(c=>c is Rigidbody||c is Collider).Distinct();
    static string Config(Component c){if(c is Rigidbody b)return b.mass+"/"+b.centerOfMass.ToString("R")+"/"+b.inertiaTensor.ToString("R")+"/"+b.linearDamping+"/"+b.angularDamping+"/"+b.constraints;var shape=(Collider)c;string s=shape.isTrigger+"/"+shape.contactOffset+"/"+(shape.sharedMaterial!=null?shape.sharedMaterial.GetInstanceID():0);if(shape is BoxCollider box)s+="/"+box.center.ToString("R")+"/"+box.size.ToString("R");if(shape is MeshCollider mesh)s+="/"+(mesh.sharedMesh!=null?mesh.sharedMesh.GetInstanceID():0)+"/"+mesh.convex;return s;}
    Dictionary<Component,string> Snapshot()=>Physical().ToDictionary(c=>c,Config);
    void Same(Dictionary<Component,string> before){CollectionAssert.AreEquivalent(before.Keys,Physical());foreach(var pair in before)Assert.That(Config(pair.Key),Is.EqualTo(pair.Value),"Composition changed existing physical configuration: "+pair.Key.name);}
    string Wheels()=>string.Join(";",trucks.SelectMany(t=>((Array)Get(t,"wheels")).Cast<object>()).Select(w=>((Component)w).GetInstanceID()+"/"+Get(Get(w,"wheel"),"radius")+"/"+Get(Get(w,"wheel"),"width")+"/"+Get(Get(w,"spring"),"maxLength")+"/"+Get(Get(w,"spring"),"maxForce")));
    IEnumerator OriginalStartup()
    {
        for(int i=0;i<4;i++)Set(trucks[i],"appearanceHost",null);
        try{Assert.That(Call(match,"StartMatch"),Is.EqualTo(true));double deadline=Time.realtimeSinceStartupAsDouble+5;while(Time.realtimeSinceStartupAsDouble<deadline&&courses.Take(hotSeat?1:4).Any(r=>Get(r,"Phase").ToString()!="Approach"))yield return null;Assert.That(courses.Take(hotSeat?1:4).All(r=>Get(r,"Phase").ToString()=="Approach"),Is.True,"Original ready countdown must activate actual NWH before controlled appearance trials.");yield return null;}
        finally{for(int i=0;i<4;i++)Set(trucks[i],"appearanceHost",hosts[i]);}
        for(int lane=0;lane<(hotSeat?1:4);lane++){Assert.That(Get(courses[lane],"appearanceReplayInitialized"),Is.EqualTo(true),"Initialize original owned lane topology before composing a profile body.");Assert.That(Get(hosts[lane],"AppearanceRoot"),Is.Null,"Original no-appearance physical startup must not compose a selected body.");}Call(match,"Pause",0,false);((Behaviour)match).enabled=false;foreach(var course in courses)Call(course,"Freeze");yield return null;yield return null;
    }
    static string Relative(Transform part,Transform root){string path=part.name;while(part.parent!=root){part=part.parent;Assert.That(part,Is.Not.Null);path=part.name+"/"+path;}return path;}
    void Capture(object course){Call(course,"CaptureAppearanceReplay",0f);Call(course,"CaptureAppearanceReplay",.2f);}
    void AssertArt(int lane,int owner,string expected)
    {
        var host=hosts[lane];Assert.That(Get(host,"ProfileId"),Is.EqualTo(ids[owner]));Assert.That(Get(host,"EffectiveBodyId"),Is.EqualTo(expected));Assert.That(Get(trucks[lane],"slot"),Is.EqualTo(owner));Assert.That(Get(courses[lane],"owner"),Is.EqualTo(owner));var model=(Transform)Get(host,"AppearanceRoot");Assert.That(model,Is.Not.Null);Assert.That(model.GetComponentsInChildren<Rigidbody>(true),Is.Empty);Assert.That(model.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(model.GetComponentsInChildren<Renderer>(true).All(r=>r.gameObject.layer==10+lane),Is.True,"Selected body/wheels may never leak onto layer0 or another lane's cab layer.");Assert.That(((Renderer[])Get(host,"originalBodyRenderers")).Length,Is.EqualTo(29));Assert.That(((Renderer[])Get(host,"originalBodyRenderers")).All(r=>!r.enabled),Is.True);var entry=Call(Get(host,"catalog"),"FindBody",expected);var originals=((GameObject)Get(entry,"visualPrefab")).GetComponentsInChildren<Renderer>(true);var actual=(Renderer[])Get(host,"paintRenderers");Assert.That(actual.Length,Is.EqualTo(originals.Length));for(int r=0;r<actual.Length;r++)CollectionAssert.AreEqual(originals[r].sharedMaterials,actual[r].sharedMaterials);Assert.That(Get(Get(courses[lane],"appearanceCab"),"assignedOwner"),Is.SameAs(host));
    }
    void AssertReplay(int lane,Transform source,Transform ghost)
    {
        var replay=Get(courses[lane],"replay");CollectionAssert.AreEqual(new[]{source},(Transform[])Get(replay,"sources"));CollectionAssert.AreEqual(new[]{ghost},(Transform[])Get(replay,"ghosts"));Assert.That(ghost.GetComponentsInChildren<Renderer>(true).All(r=>r.gameObject.layer==10+lane),Is.True);Assert.That(ghost.GetComponentsInChildren<Rigidbody>(true),Is.Empty);Assert.That(ghost.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(source.GetComponentsInChildren<Transform>(true).Length,Is.EqualTo(ghost.GetComponentsInChildren<Transform>(true).Length));foreach(var slot in (Array)Get(hosts[lane],"paintSlots")){var renderer=((Renderer[])Get(hosts[lane],"paintRenderers"))[(int)Get(slot,"rendererIndex")];var copy=ghost.Find(Relative(renderer.transform,source)).GetComponent<Renderer>();int index=(int)Get(slot,"materialIndex");CollectionAssert.AreEqual(renderer.sharedMaterials,copy.sharedMaterials);var a=new MaterialPropertyBlock();var b=new MaterialPropertyBlock();renderer.GetPropertyBlock(a,index);copy.GetPropertyBlock(b,index);Assert.That(b.GetColor("_BaseColor"),Is.EqualTo(a.GetColor("_BaseColor")));}
    }
    IEnumerator Exercise(bool shared)
    {
        yield return Load(shared);var originalRefs=Physical().ToArray();var replays=courses.Select(r=>Get(r,"replay")).ToArray();var source= replays.Select(r=>((Transform[])Get(r,"sources")).Single()).ToArray();var ghost=replays.Select(r=>((Transform[])Get(r,"ghosts")).Single()).ToArray();var originalGhostParts=ghost.Select(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();var rings=courses.Select(r=>((Array)Get(r,"rings")).Cast<Component>().ToArray()).ToArray();var targets=courses.Select(r=>Get(r,"target")).ToArray();var cabAnchors=courses.Select(r=>Get(r,"cabAnchor")).ToArray();var bodyLayers=trucks.Select(t=>t.gameObject.layer).ToArray();var targetPhysical=rings.SelectMany(a=>a).SelectMany(r=>r.GetComponentsInChildren<Collider>(true)).Distinct().ToDictionary(c=>(Component)c,c=>Config(c));yield return OriginalStartup();foreach(var part in originalRefs)Assert.That(Physical().Contains(part),Is.True);int active=shared?1:4;
        foreach(string body in new[]{"truck.fire","truck.monster","truck.lifted","semi.lowrider","novelty.hotdog","novelty.icecream","semi.standard"})
        {
            Save(ids[0],body);Call(match,"RefreshParticipants",false);var physical=Snapshot();string wheelConfig=Wheels();for(int lane=0;lane<active;lane++){Call(courses[lane],"ResetCourse",lane);AssertArt(lane,lane,lane==0?body:lane%2==0?"truck.fire":"novelty.icecream");Capture(courses[lane]);AssertReplay(lane,source[lane],ghost[lane]);foreach(var original in originalGhostParts[lane])Assert.That(original!=null&&(original==ghost[lane]||original.IsChildOf(ghost[lane])),Is.True,"Original ghost root/children must survive topology refresh.");}Same(physical);Assert.That(Wheels(),Is.EqualTo(wheelConfig));CollectionAssert.AreEqual(bodyLayers,trucks.Select(t=>t.gameObject.layer));CollectionAssert.AreEqual(targets,courses.Select(r=>Get(r,"target")));CollectionAssert.AreEqual(cabAnchors,courses.Select(r=>Get(r,"cabAnchor")));foreach(var pair in targetPhysical)Assert.That(Config(pair.Key),Is.EqualTo(pair.Value));for(int lane=0;lane<4;lane++)CollectionAssert.AreEqual(rings[lane],((Array)Get(courses[lane],"rings")).Cast<Component>());
        }
        var buffers=Get(replays[0],"roots");Call(courses[0],"StartAttempt");Capture(courses[0]);Assert.That(Get(replays[0],"roots"),Is.SameAs(buffers));Call(replays[0],"Begin",0f,float.PositiveInfinity);Assert.That(Get(replays[0],"Playing"),Is.EqualTo(true));Assert.That(Call(replays[0],"RefreshAppearanceSources",new[]{0},true),Is.EqualTo(false));Assert.That(Get(replays[0],"roots"),Is.SameAs(buffers));Call(replays[0],"End");
        if(shared)
        {
            // Real delivery/review handoff methods; zero is a controlled miss,
            // not a claim of a physically earned target entry.
            for(int owner=0;owner<8;owner++)
            {
                Assert.That(Get(courses[0],"owner"),Is.EqualTo(owner));string expected=owner==0?"semi.standard":owner%2==0?"truck.fire":"novelty.icecream";AssertArt(0,owner,expected);Capture(courses[0]);Call(courses[0],"Tick",Vector2.zero,false,1.3f);Call(courses[0],"Resolve",0,"CONTROLLED APPEARANCE TURN MISS");Call(courses[0],"Tick",Vector2.zero,false,1.0f);Assert.That(Get(courses[0],"Phase").ToString(),Is.EqualTo("Replay"));Call(courses[0],"Tick",Vector2.zero,false,1.3f);Assert.That(Get(courses[0],"owner"),Is.EqualTo((owner+1)%8));Assert.That(trucks[0].gameObject.activeSelf,Is.True);
            }
            CollectionAssert.AreEqual(Enumerable.Repeat(1,8).ToArray(),(int[])Get(Get(match,"rules"),"attempts"));Assert.That(((int[])Get(Get(match,"rules"),"totals")).All(score=>score==0),Is.True);Assert.That(ids.Distinct().Count(),Is.EqualTo(8));
        }
        else
        {
            var item=((Array)Get(Get(hosts[0],"catalog"),"cosmetics")).Cast<object>().First(c=>(string)Get(c,"slotId")=="dashboard"&&(bool)Call(c,"CompatibleWith","semi.standard"));var draft=Registry("GetAppearance",ids[0]);var selection=Activator.CreateInstance(Sports("SportsTruckCosmeticSelection"));Set(selection,"slotId","dashboard");Set(selection,"itemId",Get(item,"id"));((IList)Get(draft,"selections")).Add(selection);Registry("TrySetAppearance",ids[0],draft);Registry("ForgetCache");Call(courses[0],"ResetCourse",0);Capture(courses[0]);AssertReplay(0,source[0],ghost[0]);var figure=(GameObject)Get(Get(courses[0],"appearanceCab"),"graphic");Assert.That(figure,Is.Not.Null,"Actual selected dashboard item must use the existing owner cab adapter.");Assert.That(figure.GetComponentsInChildren<Renderer>(true).All(r=>r.gameObject.layer==25),Is.True);Assert.That(figure.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(figure.GetComponentsInChildren<Rigidbody>(true),Is.Empty);
            var player=((Array)Get(Get(match,"setup"),"players")).GetValue(0);Set(player,"source",Enum.Parse(Get(player,"source").GetType(),"Keyboard"));((bool[])Get(match,"cab"))[0]=true;Call(match,"Present",0f);var camera=(Camera)Get(Get(courses[0],"viewCamera"),"view");Assert.That(camera.cullingMask&(1<<10),Is.EqualTo(0));Assert.That(camera.cullingMask&(1<<25),Is.Not.EqualTo(0));for(int lane=1;lane<4;lane++)Assert.That(camera.cullingMask&(1<<(25+lane)),Is.EqualTo(0));Assert.That(((Transform)Get(courses[0],"cabAnchor")).gameObject.activeSelf,Is.True);Set(player,"source",Enum.Parse(Get(player,"source").GetType(),"AI"));
        }
        int revision=(int)Get(hosts[0],"AppearanceRevision");Call(match,"SetupScreen");Assert.That(Call(match,"StartMatch"),Is.EqualTo(true));Assert.That(Get(hosts[0],"AppearanceRevision"),Is.EqualTo(revision));AssertArt(0,0,"semi.standard");TestContext.WriteLine("SKEE_APPEARANCE "+(shared?"hotseat8":"simultaneous4")+" actual stable profile IDs/all7 bodies, original ready/NWH initialization and physical/ring/wheel/source roots, lane/cab renderer isolation, original ghost children, indexed paint, buffer reuse/playing guard/rematch retained. Hotseat uses original miss/review/handoff methods; no physical entry/art/hardware acceptance claim.");
    }
    [UnityTest,Timeout(180000)]public IEnumerator FourIndependentLanesUseSelectedBodiesWithoutChangingPhysicalRingRulesCabLayersOrReplayRoots(){yield return Exercise(false);}
    [UnityTest,Timeout(180000)]public IEnumerator EightHotseatProfilesFollowActualDeliveryReviewHandoffsOnTheSamePhysicalLane(){yield return Exercise(true);}
}
#endif
