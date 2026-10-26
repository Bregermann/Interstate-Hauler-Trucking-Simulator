#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Actual saved full-NWH actors/equipment; controlled appearance/recorded-pose
// samples. These tests do not claim complete matches or visual acceptance.
public sealed class FacadeAppearanceIntegrationTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
    static object Registry(string name,params object[] args)=>Sports("SportsParticipantRegistry").GetMethod(name).Invoke(null,args);
    Component match;IDisposable lease;GameObject isolation;
    IEnumerator Load(string mode,string matchName)
    {
        lease=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"FacadeAppearance"+Guid.NewGuid().ToString("N")});Registry("ForgetCache");
        isolation=new GameObject("Explicit fixed-facade appearance fixture");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Sports("SportsProbeInputIsolation"));
        EditorSceneManager.LoadSceneInPlayMode("Assets/"+mode+"/Scenes/"+mode+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
        match=(Component)UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS."+mode+"."+matchName+", Assembly-CSharp",true));Assert.That(match,Is.Not.Null);Call(match,"AllBots");
    }
    [TearDown]public void Cleanup(){if(match!=null)Call(match,"SetupScreen");Time.timeScale=1;if(isolation!=null)UnityEngine.Object.DestroyImmediate(isolation);Registry("ForgetCache");lease?.Dispose();lease=null;}
    static void Save(string id,string body)
    {
        var draft=Registry("GetAppearance",id);bool identical=(string)Get(draft,"bodyId")==body&&((IList)Get(draft,"selections")).Count==0;Set(draft,"bodyId",body);((IList)Get(draft,"selections")).Clear();
        Assert.That(Registry("TrySetAppearance",id,draft),Is.EqualTo(!identical));Assert.That(Get(Registry("GetAppearance",id),"bodyId"),Is.EqualTo(body));
    }
    static string Digest(StringBuilder value){using(var hash=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value.ToString())));}
    static string Physics(Component truck)
    {
        var value=new StringBuilder();value.Append(truck.transform.localScale.ToString("R"));
        foreach(var body in truck.GetComponentsInChildren<Rigidbody>(true))value.Append("/B").Append(body.GetInstanceID()).Append(':').Append(body.mass).Append(':').Append(body.centerOfMass.ToString("R"));
        foreach(var shape in truck.GetComponentsInChildren<Collider>(true))
        {
            value.Append("/C").Append(shape.GetInstanceID()).Append(':').Append(shape.GetType().Name).Append(':').Append(shape.isTrigger).Append(':').Append(shape.sharedMaterial!=null?shape.sharedMaterial.GetInstanceID():0);
            if(shape is BoxCollider box)value.Append(':').Append(box.center.ToString("R")).Append(':').Append(box.size.ToString("R"));
            if(shape is MeshCollider mesh)value.Append(':').Append(mesh.convex).Append(':').Append(mesh.sharedMesh!=null?mesh.sharedMesh.GetInstanceID():0);
        }
        foreach(var wheel in (IEnumerable)Get(truck,"wheels")){var tire=Get(wheel,"wheel");var spring=Get(wheel,"spring");value.Append("/W").Append(((Component)wheel).GetInstanceID()).Append(':').Append(Get(tire,"radius")).Append(':').Append(Get(tire,"width")).Append(':').Append(Get(tire,"mass")).Append(':').Append(Get(spring,"maxLength")).Append(':').Append(Get(spring,"maxForce"));}
        return Digest(value);
    }
    static string Gear(object[] hosts)
    {
        var value=new StringBuilder();
        foreach(var root in hosts.SelectMany(host=>(Transform[])Get(host,"protectedRoots")).Distinct())
        {
            Assert.That(root!=null,Is.True);value.Append("/T").Append(root.GetInstanceID()).Append(':').Append(root.parent!=null?root.parent.GetInstanceID():0).Append(':').Append(root.localPosition.ToString("R")).Append(':').Append(root.localRotation.ToString("R")).Append(':').Append(root.localScale.ToString("R"));
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true)){value.Append("/R").Append(renderer.GetInstanceID()).Append(':').Append(renderer.enabled);foreach(var material in renderer.sharedMaterials)value.Append(':').Append(material!=null?material.GetInstanceID():0);}
            foreach(var shape in root.GetComponentsInChildren<Collider>(true))value.Append("/C").Append(shape.GetInstanceID()).Append(':').Append(shape.enabled).Append(':').Append(shape.isTrigger);
        }
        return Digest(value);
    }
    static string Relative(Transform part,Transform root){string path=part.name;while(part.parent!=root){part=part.parent;Assert.That(part,Is.Not.Null);path=part.name+"/"+path;}return path;}
    static void PaintMatches(object host,Transform ghost,Color? expected=null)
    {
        var renderers=(Renderer[])Get(host,"paintRenderers");var slots=(Array)Get(host,"paintSlots");Assert.That(slots.Length,Is.GreaterThan(0));
        foreach(var slot in slots)
        {
            var source=renderers[(int)Get(slot,"rendererIndex")];int index=(int)Get(slot,"materialIndex");var node=ghost.Find(Relative(source.transform,((Component)host).transform));Assert.That(node,Is.Not.Null);
            var target=node.GetComponent<Renderer>();Assert.That(target,Is.Not.Null);CollectionAssert.AreEqual(source.sharedMaterials,target.sharedMaterials);
            var a=new MaterialPropertyBlock();var b=new MaterialPropertyBlock();source.GetPropertyBlock(a,index);target.GetPropertyBlock(b,index);Assert.That(b.GetColor("_BaseColor"),Is.EqualTo(a.GetColor("_BaseColor")));if(expected.HasValue)Assert.That(a.GetColor("_BaseColor"),Is.EqualTo(expected.Value));
        }
    }
    static void Model(object host)
    {
        var model=(GameObject)Get(host,"model");Assert.That(model,Is.Not.Null);Assert.That(model.GetComponentsInChildren<Rigidbody>(true),Is.Empty);Assert.That(model.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(model.GetComponentsInChildren<Joint>(true),Is.Empty);Assert.That(model.GetComponentsInChildren<Camera>(true),Is.Empty);Assert.That(model.GetComponentsInChildren<AudioListener>(true),Is.Empty);
        Assert.That(((Renderer[])Get(host,"originalBodyRenderers")).Length,Is.EqualTo(29));Assert.That(((Renderer[])Get(host,"originalWheelRenderers")).Length,Is.EqualTo(4));Assert.That(((Renderer[])Get(host,"originalBodyRenderers")).All(renderer=>!renderer.enabled),Is.True);
        Assert.That(((Renderer[])Get(host,"originalWheelRenderers")).All(renderer=>!renderer.enabled),Is.True);
    }
    IEnumerator Exercise(string mode,string matchName,int count)
    {
        yield return Load(mode,matchName);var trucks=((Array)Get(match,"trucks")).Cast<Component>().ToArray();var rigs=((Array)Get(match,"fragments")).Cast<Component>().ToArray();Assert.That(trucks.Length,Is.EqualTo(count));Assert.That(rigs.Length,Is.EqualTo(count));
        var hosts=trucks.Select(truck=>Get(truck,"appearanceHost")).ToArray();Assert.That(hosts.All(host=>host!=null),Is.True,"Invoke the explicit fixed-owner facade author first.");
        if((bool)Get(match,"TeamSport")){Set(match,"teamA",count/2);Set(match,"teamB",count-count/2);}
        Set(Get(match,"setup"),"count",count);Call(match,"NormalizeSetup");Assert.That(Get(Get(match,"setup"),"count"),Is.EqualTo(count),"The real team roster must include every physical actor under test.");var ids=new string[count];
        for(int i=0;i<count;i++)
        {
            ids[i]=(string)Get(Registry("Create","Same Facade Driver"),"id");Registry("UseInSeat",i,ids[i]);Save(ids[i],i==0?"truck.fire":i%2==0?"truck.monster":"novelty.icecream");
            Assert.That(Get(trucks[i],"slot"),Is.EqualTo(i));Assert.That(Get(hosts[i],"participantSlot"),Is.EqualTo(i));Assert.That(Get(trucks[i],"visuals"),Is.SameAs(trucks[i].transform));Assert.That(Get(hosts[i],"presentationParent"),Is.SameAs(trucks[i].transform));Assert.That(Get(rigs[i],"truck"),Is.Null);Assert.That(Get(rigs[i],"appearanceHost"),Is.SameAs(hosts[i]));Assert.That(Get(rigs[i],"sourceBody"),Is.SameAs(Get(trucks[i],"body")));
            if(mode=="TruckFreezeTag")
            {
                var effect=(ParticleSystem)((Array)Get(match,"shatter")).GetValue(i);Assert.That(effect,Is.Not.Null);Assert.That(effect.name,Is.EqualTo("Pooled existing Weatherade ice shatter"));Assert.That(effect.transform.parent,Is.SameAs(trucks[i].transform));Assert.That(((Transform[])Get(hosts[i],"protectedRoots")).Contains(effect.transform),Is.True,"Bind the exact original typed Weatherade shatter pool, never infer arbitrary extra rendering.");
                Assert.That(effect.GetComponentsInChildren<Renderer>(true).All(renderer=>renderer is ParticleSystemRenderer&&renderer.GetComponent<ParticleSystem>()!=null),Is.True);Assert.That(effect.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(effect.GetComponentsInChildren<Rigidbody>(true),Is.Empty);
            }
        }
        Assert.That(ids.Distinct().Count(),Is.EqualTo(count));Call(match,"RefreshParticipants",false);
        var before=trucks.Select(Physics).ToArray();var gear=Gear(hosts);Call(match,"PrepareAppearances");CollectionAssert.AreEqual(before,trucks.Select(Physics),"Pure appearance preparation must preserve every actual original Rigidbody/Collider/NWH parameter.");Assert.That(Gear(hosts),Is.EqualTo(gear));
        Assert.That(Call(match,"StartMatch"),Is.True);((Behaviour)match).enabled=false;yield return null;yield return null;
        var physical=trucks.Select(Physics).ToArray();var originalRoots=trucks.Select(truck=>truck.transform).ToArray();var originalWheels=trucks.Select(truck=>((IEnumerable)Get(truck,"wheels")).Cast<Component>().ToArray()).ToArray();
        string[] bodies={"truck.fire","truck.monster","truck.lifted","semi.lowrider","novelty.hotdog","novelty.icecream","semi.standard"};
        foreach(string body in bodies)
        {
            Save(ids[0],body);Call(match,"RefreshParticipants",false);gear=Gear(hosts);Call(match,"PrepareAppearances");Assert.That(Gear(hosts),Is.EqualTo(gear));
            for(int i=0;i<count;i++)
            {
                Call(rigs[i],"ResetBody");Model(hosts[i]);Assert.That(Get(hosts[i],"ProfileId"),Is.EqualTo(ids[i]));Assert.That(Get(hosts[i],"EffectiveBodyId"),Is.EqualTo(i==0?body:i%2==0?"truck.monster":"novelty.icecream"));
                Assert.That(trucks[i].transform,Is.SameAs(originalRoots[i]));CollectionAssert.AreEqual(originalWheels[i],((IEnumerable)Get(trucks[i],"wheels")).Cast<Component>());Assert.That(Physics(trucks[i]),Is.EqualTo(physical[i]));
            }
            int revision=(int)Get(hosts[0],"AppearanceRevision");var old=((Renderer[])Get(hosts[0],"originalBodyRenderers"))[0];old.enabled=true;Assert.That(Call(hosts[0],"Prepare",ids[0],((Color)Get(((Array)Get(Get(match,"setup"),"players")).GetValue(0),"color"))),Is.EqualTo(true));Assert.That(old.enabled,Is.False,"Same-fingerprint preparation must reassert the selected body after an original renderer reset.");Assert.That(Get(hosts[0],"AppearanceRevision"),Is.EqualTo(revision));
        }
        Save(ids[0],"truck.fire");Call(match,"RefreshParticipants",false);Call(match,"PrepareAppearances");
        var review=Get(match,"review");Assert.That(review,Is.Not.Null);Call(review,"Capture",4f);Call(review,"Capture",4.2f);var record=Get(review,"record");Assert.That(record,Is.Not.Null);
        var ghosts=((Transform[])Get(record,"ghosts")).ToArray();var cabNodes=ghosts.Take(count).Select(ghost=>ghost.Find("Cab")).ToArray();var lightNodes=ghosts.Take(count).Select(ghost=>ghost.Find("Lights")).ToArray();
        for(int i=0;i<count;i++){Assert.That(cabNodes[i],Is.Not.Null);Assert.That(lightNodes[i],Is.Not.Null);PaintMatches(hosts[i],ghosts[i]);Assert.That(ghosts[i].GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(ghosts[i].GetComponentsInChildren<Rigidbody>(true),Is.Empty);}
        var poseBuffer=Get(record,"roots");var partBuffer=Get(record,"parts");Call(hosts[0],"ApplyEffectiveColor",Color.magenta);Call(review,"ResetRecord");Assert.That(Get(record,"Duration"),Is.EqualTo(0f));Call(review,"Capture",4f);Call(review,"Capture",4.2f);
        Assert.That(Get(record,"roots"),Is.SameAs(poseBuffer),"Same-body metadata refresh must retain existing pose buffers.");Assert.That(Get(record,"parts"),Is.SameAs(partBuffer));PaintMatches(hosts[0],ghosts[0],Color.magenta);
        Save(ids[0],"novelty.icecream");Call(match,"RefreshParticipants",false);Call(match,"PrepareAppearances");Call(review,"ResetRecord");Call(review,"Capture",4f);Call(review,"Capture",4.2f);Assert.That(Get(record,"roots"),Is.Not.SameAs(poseBuffer),"Changed source topology must replace the old pose buffer before recording.");
        for(int i=0;i<count;i++){Assert.That(((Transform[])Get(record,"ghosts"))[i],Is.SameAs(ghosts[i]));Assert.That(ghosts[i].Find("Cab"),Is.SameAs(cabNodes[i]));Assert.That(ghosts[i].Find("Lights"),Is.SameAs(lightNodes[i]));PaintMatches(hosts[i],ghosts[i]);Assert.That(ghosts[i].GetComponentsInChildren<Transform>(true).Length,Is.EqualTo(trucks[i].GetComponentsInChildren<Transform>(true).Length));}
        Call(record,"Begin",4f,float.PositiveInfinity);Assert.That(Get(record,"Playing"),Is.EqualTo(true));poseBuffer=Get(record,"roots");
        Assert.That(Call(record,"RefreshAppearanceSources",new int[]{0,count},false),Is.EqualTo(false));Assert.That(Call(record,"RefreshAppearanceSources",new int[]{0,count},true),Is.EqualTo(false));Assert.That(Call(record,"ResetFreshAppearanceRecord"),Is.EqualTo(false));Assert.That(Get(record,"roots"),Is.SameAs(poseBuffer));Call(record,"End");
        var views=match.GetComponent(Sports("SportsDrivingViews"));var players=(Array)Get(Get(match,"setup"),"players");var first=players.GetValue(0);var originalSource=Get(first,"source");var originalReleased=Get(first,"released");int originalMode=(int)Get(views,"Mode");
        try
        {
            // Synthetic view-owner routing only. The real input/device policy
            // remains in force and must see the original AI source at rematch.
            Set(first,"source",Enum.Parse(originalSource.GetType(),"Keyboard"));Set(first,"released",true);Call(views,"Select",2);
            var rally=Get(views,"rally");Assert.That((int)Get(rally,"Humans"),Is.GreaterThan(0));var camera=(Camera)Get(((Array)Get(rally,"views")).GetValue(0),"view");var exterior=(Renderer[])Get(hosts[0],"ExteriorRenderers");var cab=((Transform[])Get(views,"cabs"))[0];Assert.That(cab.gameObject.activeSelf,Is.True);Assert.That(cab.GetComponentsInChildren<Renderer>(true).Any(renderer=>exterior.Contains(renderer)),Is.False);
            var prior=exterior.Select(renderer=>renderer.forceRenderingOff).ToArray();Call(views,"BeginCamera",default(ScriptableRenderContext),camera);Assert.That(exterior.All(renderer=>renderer.forceRenderingOff),Is.True);Call(views,"RefreshAppearanceExteriors");CollectionAssert.AreEqual(prior,exterior.Select(renderer=>renderer.forceRenderingOff),"Refreshing cab exteriors must first restore every previous camera-local hiding state.");
        }
        finally{Set(first,"source",originalSource);Set(first,"released",originalReleased);Call(views,"Select",originalMode);}
        TestContext.WriteLine("FACADE_REAL_REMATCH_READINESS mode="+mode+" count="+Get(Get(match,"setup"),"count")+" teamA="+Get(match,"teamA")+" teamB="+Get(match,"teamB")+" restoredSource="+Get(first,"source")+" ready0="+Call(match,"Ready",0)+" DevicesReady="+Get(match,"DevicesReady"));
        int savedRevision=(int)Get(hosts[0],"AppearanceRevision");Call(match,"SetupScreen");Assert.That(Call(match,"StartMatch"),Is.True);Assert.That(Get(hosts[0],"AppearanceRevision"),Is.EqualTo(savedRevision));Call(review,"Capture",4f);Call(review,"Capture",4.2f);PaintMatches(hosts[0],ghosts[0]);
        CollectionAssert.AreEqual(physical,trucks.Select(Physics));Assert.That((int)Get(Get(match,"ScoreJournal"),"Count"),Is.Zero,"Controlled appearance/recorded-pose sampling must not award gameplay points.");
        TestContext.WriteLine("SAVED_FIXED_FACADE_APPEARANCE "+mode+" originalActors="+count+" all7bodies fixedIDs, exact original physics/gear, ResetBody/samefingerprint visibility, source+ghost per-slot color, unchanged-buffer metadata refresh, changed-body topology, retained Cab/Lights/root refs, playing guard, owner-cab restoration and rematch. Controlled recording samples; no complete-match or visual acceptance claim.");
    }
    [UnityTest,Timeout(180000)]public IEnumerator BalloonEight(){yield return Exercise("TruckBalloonBattle","BalloonMatch",8);}
    [UnityTest,Timeout(180000)]public IEnumerator WhackEight(){yield return Exercise("TruckWhackAMole","WhackMatch",8);}
    [UnityTest,Timeout(180000)]public IEnumerator HillEight(){yield return Exercise("TruckKingOfTheHill","HillMatch",8);}
    [UnityTest,Timeout(180000)]public IEnumerator TetherTwo(){yield return Exercise("TruckTetherball","TetherMatch",2);}
    [UnityTest,Timeout(180000)]public IEnumerator JoustTwo(){yield return Exercise("TruckJousting","JoustMatch",2);}
    [UnityTest,Timeout(180000)]public IEnumerator MagnetEight(){yield return Exercise("TruckMagnetMayhem","MagnetMatch",8);}
    [UnityTest,Timeout(180000)]public IEnumerator VolleyEight(){yield return Exercise("TruckVolleyball","VolleyMatch",8);}
    [UnityTest,Timeout(180000)]public IEnumerator FreezeEight(){yield return Exercise("TruckFreezeTag","FreezeMatch",8);}
    [UnityTest,Timeout(180000)]public IEnumerator HaulerFour(){yield return Exercise("TruckHungryHaulers","HaulerMatch",4);}
    [UnityTest,Timeout(180000)]public IEnumerator HerdFour(){yield return Exercise("TruckSheepHerding","HerdMatch",4);}
    [UnityTest,Timeout(180000)]public IEnumerator HideEight(){yield return Exercise("TruckHideAndSeek","HideMatch",8);}
    [UnityTest,Timeout(180000)]public IEnumerator FrogEight(){yield return Exercise("TruckFrogger","FrogMatch",8);}
    [UnityTest,Timeout(180000)]public IEnumerator NinjaEight(){yield return Exercise("TruckNinjaWarrior","NinjaMatch",8);}
}
#endif
