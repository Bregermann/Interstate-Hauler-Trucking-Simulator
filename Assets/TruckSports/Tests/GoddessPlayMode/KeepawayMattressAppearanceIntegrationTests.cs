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

public sealed class KeepawayMattressAppearanceIntegrationTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value){if(owner.GetType().GetField(name,Any) is FieldInfo field)field.SetValue(owner,value);else owner.GetType().GetProperty(name,Any).SetValue(owner,value);}
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
    static object Registry(string name,params object[] args)=>Sports("SportsParticipantRegistry").GetMethod(name).Invoke(null,args);
    Component match;IDisposable lease;GameObject isolation;bool mattress;Component[] trucks;object[] hosts;string[] ids;
    [TearDown]public void Cleanup(){Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");if(isolation!=null)UnityEngine.Object.DestroyImmediate(isolation);Registry("ForgetCache");lease?.Dispose();lease=null;}
    static void Save(string id,string body){var draft=Registry("GetAppearance",id);Set(draft,"bodyId",body);((IList)Get(draft,"selections")).Clear();Registry("TrySetAppearance",id,draft);Registry("ForgetCache");Assert.That(Get(Registry("GetAppearance",id),"bodyId"),Is.EqualTo(body));}
    IEnumerator Load(string mode)
    {
        mattress=mode=="TruckMattressRacing";lease=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"KeepMattressAppearance"+Guid.NewGuid().ToString("N")});Registry("ForgetCache");isolation=new GameObject("Saved Keepaway Mattress appearance fixture");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Sports("SportsProbeInputIsolation"));
        EditorSceneManager.LoadSceneInPlayMode("Assets/"+(mattress?mode:"TruckKeepaway")+"/Scenes/"+mode+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;match=(Component)UnityEngine.Object.FindAnyObjectByType(Type.GetType(mattress?"LWS.TruckMattressRacing.MattressMatch, Assembly-CSharp":"LWS.TruckKeepaway.KeepawayMatch, Assembly-CSharp",true));Assert.That(match,Is.Not.Null);Call(match,"AllBots");Set(Get(match,"setup"),"count",mattress?8:4);Call(match,"NormalizeSetup");
        trucks=mattress?((Array)Get(match,"racers")).Cast<object>().Select(r=>(Component)Get(r,"truck")).ToArray():((Array)Get(match,"trucks")).Cast<Component>().ToArray();hosts=((Array)Get(match,"appearanceHosts")).Cast<object>().ToArray();Assert.That(hosts.Length,Is.EqualTo(trucks.Length),"Run BindMissingKeepawayAndMattressHosts first.");ids=new string[hosts.Length];for(int i=0;i<hosts.Length;i++){ids[i]=(string)Get(Registry("Create","Same Platform Driver"),"id");Registry("UseInSeat",i,ids[i]);Save(ids[i],i%2==0?"truck.fire":"novelty.icecream");Assert.That(((Component)hosts[i]).transform,Is.SameAs(trucks[i].transform));Assert.That(Get(hosts[i],"participantSlot"),Is.EqualTo(i));}Call(match,"RefreshParticipants",false);
        if(!mattress)foreach(var truck in trucks){Assert.That(truck.GetComponent(Sports("SportsWheelMeshBinding")),Is.Null,"Original Keepaway has no fitted mesh binder.");foreach(var wheel in (Array)Get(truck,"wheels"))Assert.That(((Transform)Get(Get(wheel,"wheel"),"rotatingContainer")).GetComponentsInChildren<Renderer>(true),Is.Empty,"Original NWH containers are present but contain no rendering.");var host=Get(truck,"appearanceHost");CollectionAssert.AreEquivalent(new[]{"Whl HD FL","Whl HD FR","Whl HD RL","Whl HD RR"},((Renderer[])Get(host,"originalWheelRenderers")).Select(r=>r.name));Assert.That(((Renderer[])Get(host,"originalWheelRenderers")).All(r=>r.transform.parent==(Transform)Get(truck,"visuals")),Is.True);}
    }
    IEnumerable<Component> Physical()=>trucks.SelectMany(t=>t.GetComponentsInChildren<Component>(true)).Where(c=>c is Rigidbody||c is Collider).Distinct();
    static string Config(Component c){if(c is Rigidbody b)return b.mass+"/"+b.centerOfMass.ToString("R")+"/"+b.inertiaTensor.ToString("R")+"/"+b.linearDamping+"/"+b.angularDamping+"/"+b.constraints;var shape=(Collider)c;string s=shape.isTrigger+"/"+shape.contactOffset+"/"+(shape.sharedMaterial!=null?shape.sharedMaterial.GetInstanceID():0);if(shape is BoxCollider box)s+="/"+box.center.ToString("R")+"/"+box.size.ToString("R");if(shape is MeshCollider mesh)s+="/"+(mesh.sharedMesh!=null?mesh.sharedMesh.GetInstanceID():0)+"/"+mesh.convex;return s;}
    Dictionary<Component,string> Snapshot()=>Physical().ToDictionary(c=>c,Config);
    void Same(Dictionary<Component,string> before){CollectionAssert.AreEquivalent(before.Keys,Physical());foreach(var pair in before)Assert.That(Config(pair.Key),Is.EqualTo(pair.Value),"Composition changed existing physical configuration: "+pair.Key.name);}
    string Wheels()=>string.Join(";",trucks.SelectMany(t=>((Array)Get(t,"wheels")).Cast<object>()).Select(w=>((Component)w).GetInstanceID()+"/"+Get(Get(w,"wheel"),"radius")+"/"+Get(Get(w,"wheel"),"width")+"/"+Get(Get(w,"spring"),"maxLength")+"/"+Get(Get(w,"spring"),"maxForce")));
    IEnumerator OriginalStartup()
    {
        var bindings=(Array)Get(match,"appearanceHosts");var truckBindings=trucks.Select(t=>Get(t,"appearanceHost")).ToArray();Set(match,"appearanceHosts",Array.CreateInstance(Sports("SportsTruckAppearanceHost"),0));foreach(var truck in trucks)Set(truck,"appearanceHost",null);
        try{Assert.That(Call(match,"StartMatch"),Is.EqualTo(true));yield return null;yield return null;}
        finally{Set(match,"appearanceHosts",bindings);for(int i=0;i<trucks.Length;i++)Set(trucks[i],"appearanceHost",truckBindings[i]);}
        ((Behaviour)match).enabled=false;foreach(var truck in trucks){Call(truck,"Power",false);((Rigidbody)Get(truck,"body")).isKinematic=true;}if(mattress)Call(Get(match,"world"),"Freeze");yield return null;
    }
    static string Relative(Transform part,Transform root){string path=part.name;while(part.parent!=root){part=part.parent;Assert.That(part,Is.Not.Null);path=part.name+"/"+path;}return path;}
    void AssertArt(string first)
    {
        for(int i=0;i<hosts.Length;i++)
        {
            Assert.That(Get(hosts[i],"ProfileId"),Is.EqualTo(ids[i]));Assert.That(Get(hosts[i],"EffectiveBodyId"),Is.EqualTo(i==0?first:i%2==0?"truck.fire":"novelty.icecream"));var model=(Transform)Get(hosts[i],"AppearanceRoot");Assert.That(model,Is.Not.Null);Assert.That(model.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(model.GetComponentsInChildren<Rigidbody>(true),Is.Empty);Assert.That(((Renderer[])Get(hosts[i],"originalBodyRenderers")).Length,Is.EqualTo(29));Assert.That(((Renderer[])Get(hosts[i],"originalBodyRenderers")).All(r=>!r.enabled),Is.True);Assert.That(((Renderer[])Get(hosts[i],"originalWheelRenderers")).Length,Is.EqualTo(4));
            var body=Call(Get(hosts[i],"catalog"),"FindBody",Get(hosts[i],"EffectiveBodyId"));var expected=((GameObject)Get(body,"visualPrefab")).GetComponentsInChildren<Renderer>(true);var actual=(Renderer[])Get(hosts[i],"paintRenderers");Assert.That(actual.Length,Is.EqualTo(expected.Length));for(int r=0;r<actual.Length;r++)CollectionAssert.AreEqual(expected[r].sharedMaterials,actual[r].sharedMaterials,"Legacy paint must preserve selected atlas, glass and chrome.");
            if(!mattress)Assert.That(model.GetComponentsInChildren<Renderer>(true).All(r=>r.gameObject.layer==22+i),Is.True,"Existing per-owner exterior layer must include selected wheels and body.");
        }
    }
    IEnumerator Exercise(string mode)
    {
        yield return Load(mode);var originalRefs=Physical().ToArray();var protectedRefs=hosts.SelectMany(h=>(Transform[])Get(h,"protectedRoots")).Distinct().ToArray();var scale=trucks.Select(t=>t.transform.localScale).ToArray();yield return OriginalStartup();foreach(var part in originalRefs)Assert.That(Physical().Contains(part),Is.True);string wheelConfig=Wheels();var replay=mattress?Get(match,"replay"):null;var sources=replay!=null?((Transform[])Get(replay,"sources")).ToArray():null;var ghosts=replay!=null?((Transform[])Get(replay,"ghosts")).ToArray():null;var worldParts=replay!=null?ghosts[8].GetComponentsInChildren<Transform>(true):null;var cushionRefs=mattress?((Array)Get(match,"racers")).Cast<object>().Select(r=>Get(r,"cushion")).ToArray():null;
        foreach(string body in new[]{"truck.fire","truck.monster","truck.lifted","semi.lowrider","novelty.hotdog","novelty.icecream","semi.standard"})
        {
            Save(ids[0],body);Call(match,"RefreshParticipants",false);var physical=Snapshot();Call(match,"PrepareAppearances");Same(physical);AssertArt(body);Assert.That(Wheels(),Is.EqualTo(wheelConfig));CollectionAssert.AreEqual(scale,trucks.Select(t=>t.transform.localScale));CollectionAssert.AreEqual(protectedRefs,hosts.SelectMany(h=>(Transform[])Get(h,"protectedRoots")).Distinct());
            if(mattress)
            {
                Assert.That(trucks.SelectMany(t=>((Array)Get(t,"wheels")).Cast<Behaviour>()).All(w=>!w.enabled),Is.True,"Original NWH controllers must remain disabled.");Call(match,"BeginRace");Call(match,"CaptureAppearanceReplay",0f,511);Call(match,"CaptureAppearanceReplay",.2f,511);CollectionAssert.AreEqual(sources,(Transform[])Get(replay,"sources"));CollectionAssert.AreEqual(ghosts,(Transform[])Get(replay,"ghosts"));CollectionAssert.AreEqual(worldParts,ghosts[8].GetComponentsInChildren<Transform>(true));CollectionAssert.AreEqual(cushionRefs,((Array)Get(match,"racers")).Cast<object>().Select(r=>Get(r,"cushion")));
                for(int i=0;i<8;i++)foreach(var slot in (Array)Get(hosts[i],"paintSlots")){var renderer=((Renderer[])Get(hosts[i],"paintRenderers"))[(int)Get(slot,"rendererIndex")];var copy=ghosts[i].Find(Relative(renderer.transform,sources[i])).GetComponent<Renderer>();int index=(int)Get(slot,"materialIndex");CollectionAssert.AreEqual(renderer.sharedMaterials,copy.sharedMaterials);var a=new MaterialPropertyBlock();var b=new MaterialPropertyBlock();renderer.GetPropertyBlock(a,index);copy.GetPropertyBlock(b,index);Assert.That(b.GetColor("_BaseColor"),Is.EqualTo(a.GetColor("_BaseColor")));}
                var racer=((Array)Get(match,"racers")).GetValue(0);Call(racer,"Fail");Assert.That(((Transform)Get(hosts[0],"AppearanceRoot")).gameObject.activeInHierarchy,Is.False);Call(racer,"Prepare",true);Assert.That(((Transform)Get(hosts[0],"AppearanceRoot")).gameObject.activeInHierarchy,Is.True);
            }
        }
        if(mattress){var buffers=Get(replay,"roots");Call(match,"BeginRace");Call(match,"CaptureAppearanceReplay",0f,511);Call(match,"CaptureAppearanceReplay",.2f,511);Assert.That(Get(replay,"roots"),Is.SameAs(buffers));Call(replay,"Begin",0f,float.PositiveInfinity);Assert.That(Get(replay,"Playing"),Is.EqualTo(true));Assert.That(Call(replay,"RefreshAppearanceSources",new[]{0,1,2,3,4,5,6,7},true),Is.EqualTo(false));Assert.That(ghosts[8].gameObject.activeSelf,Is.True);Call(replay,"End");}
        else
        {
            var players=(Array)Get(Get(match,"setup"),"players");var player=players.GetValue(0);Set(player,"source",Enum.Parse(Get(player,"source").GetType(),"Keyboard"));Call(match,"LayoutViews");var cabs=((Array)Get(match,"appearanceCabs")).Cast<object>().ToArray();Assert.That(cabs.Length,Is.EqualTo(4));var actualViews=((Array)Get(match,"cabViews")).Cast<object>().ToArray();for(int i=0;i<4;i++){var actualCamera=(Camera)Get(actualViews[i],"view");var legacy=actualCamera.GetComponent(Sports("SportsLegacyCabLook"));var cockpit=(Transform)Get(legacy,"cockpit");Assert.That(((Component)cabs[i]).transform,Is.SameAs(cockpit),"Bind the original detached cab reference, not a name lookup below the camera or truck ancestor.");Assert.That(cockpit.parent,Is.Null);Assert.That(cockpit.gameObject.layer,Is.EqualTo(26+i));Assert.That(((Component)Get(actualViews[i],"cabSteering")).transform,Is.SameAs(cockpit));}Assert.That(Get(cabs[0],"assignedOwner"),(bool)Get(match,"cabVariant")?Is.SameAs(hosts[0]):Is.Null);var views=((Array)Get(match,"cabViews")).Cast<object>().ToArray();var camera=(Camera)Get(views[0],"view");if((bool)Get(match,"cabVariant")){Call(match,"Present",0f);Assert.That(camera.cullingMask&(1<<22),Is.EqualTo(0));Assert.That(camera.cullingMask&(1<<26),Is.Not.EqualTo(0));}Set(player,"source",Enum.Parse(Get(player,"source").GetType(),"AI"));
        }
        int revision=(int)Get(hosts[0],"AppearanceRevision");Call(match,"SetupScreen");Assert.That(Call(match,"StartMatch"),Is.EqualTo(true));Assert.That(Get(hosts[0],"AppearanceRevision"),Is.EqualTo(revision));AssertArt("semi.standard");
        TestContext.WriteLine("KEEP_MATTRESS_APPEARANCE "+mode+" actual saved owners/all7 bodies; synchronous exact initialized physical configuration, wheel source authority and material identity; protected visibility, cab layers or original9 replay roots/world source retained, same-body buffers, playing guard and rematch. Controlled native fixture, not full match/art/hardware acceptance.");
    }
    [UnityTest,Timeout(180000)]public IEnumerator KeepawayArenaFourBodiesRetainBallRespawnAndLayerAuthority(){yield return Exercise("TruckKeepaway");}
    [UnityTest,Timeout(180000)]public IEnumerator KeepawayFirstPersonFourBodiesRetainActualCabOwnerAndMasks(){yield return Exercise("TruckKeepawayFirstPerson");}
    [UnityTest,Timeout(180000)]public IEnumerator MattressEightBodiesRetainDisabledWheelsClothPadAndNinthWorldReplay(){yield return Exercise("TruckMattressRacing");}
}
#endif
