#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class LegacyFacadeAppearanceIntegrationTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
    static object Registry(string name,params object[] args)=>Sports("SportsParticipantRegistry").GetMethod(name).Invoke(null,args);
    Component match;IDisposable lease;GameObject isolation;
    [TearDown]public void Cleanup(){Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");if(isolation!=null)UnityEngine.Object.DestroyImmediate(isolation);Registry("ForgetCache");lease?.Dispose();lease=null;}
    IEnumerator Load(bool snake)
    {
        lease=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"LegacyFacadeAppearance"+Guid.NewGuid().ToString("N")});Registry("ForgetCache");isolation=new GameObject("Explicit legacy facade appearance fixture");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Sports("SportsProbeInputIsolation"));
        string mode=snake?"TruckSnake":"TruckFareFight";EditorSceneManager.LoadSceneInPlayMode("Assets/"+mode+"/Scenes/"+mode+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
        match=(Component)UnityEngine.Object.FindAnyObjectByType(Type.GetType(snake?"LWS.TruckSnake.SnakeMatch, Assembly-CSharp":"LWS.TruckFareFight.FareFightMatch, Assembly-CSharp",true));Assert.That(match,Is.Not.Null);Call(match,"AllBots");Set(Get(match,"setup"),"count",4);Call(match,"NormalizeSetup");
    }
    static void Save(string id,string body)
    {
        var draft=Registry("GetAppearance",id);bool same=(string)Get(draft,"bodyId")==body&&((IList)Get(draft,"selections")).Count==0;Set(draft,"bodyId",body);((IList)Get(draft,"selections")).Clear();Assert.That(Registry("TrySetAppearance",id,draft),Is.EqualTo(!same));Registry("ForgetCache");Assert.That(Get(Registry("GetAppearance",id),"bodyId"),Is.EqualTo(body));
    }
    static string Physics(Component truck)
    {
        var text=new StringBuilder(truck.transform.localScale.ToString("R"));foreach(var body in truck.GetComponentsInChildren<Rigidbody>(true))text.Append("/B").Append(body.GetInstanceID()).Append(':').Append(body.mass).Append(':').Append(body.centerOfMass.ToString("R"));foreach(var shape in truck.GetComponentsInChildren<Collider>(true)){text.Append("/C").Append(shape.GetInstanceID()).Append(':').Append(shape.GetType().Name).Append(':').Append(shape.isTrigger);if(shape is BoxCollider box)text.Append(':').Append(box.center.ToString("R")).Append(':').Append(box.size.ToString("R"));if(shape is MeshCollider mesh)text.Append(':').Append(mesh.sharedMesh!=null?mesh.sharedMesh.GetInstanceID():0);}
        foreach(var wheel in (IEnumerable)Get(truck,"wheels")){var data=Get(wheel,"wheel");var spring=Get(wheel,"spring");text.Append("/W").Append(((Component)wheel).GetInstanceID()).Append(':').Append(Get(data,"radius")).Append(':').Append(Get(data,"width")).Append(':').Append(Get(spring,"maxLength")).Append(':').Append(Get(spring,"maxForce"));}return text.ToString();
    }
    static string Gear(object[] hosts)
    {
        var text=new StringBuilder();foreach(var root in hosts.SelectMany(host=>(Transform[])Get(host,"protectedRoots")).Distinct()){text.Append(root.GetInstanceID()).Append(':').Append(root.parent!=null?root.parent.GetInstanceID():0).Append(':').Append(root.localPosition.ToString("R")).Append(':').Append(root.localRotation.ToString("R")).Append(':').Append(root.localScale.ToString("R"));foreach(var renderer in root.GetComponentsInChildren<Renderer>(true)){text.Append("/R").Append(renderer.GetInstanceID()).Append(':').Append(renderer.enabled);foreach(var material in renderer.sharedMaterials)text.Append(':').Append(material!=null?material.GetInstanceID():0);}}using(var hash=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())));
    }
    static string Relative(Transform part,Transform root){string path=part.name;while(part.parent!=root){part=part.parent;Assert.That(part,Is.Not.Null);path=part.name+"/"+path;}return path;}
    static void Paint(object host,Transform ghost)
    {
        var renderers=(Renderer[])Get(host,"paintRenderers");var slots=(Array)Get(host,"paintSlots");Assert.That(slots.Length,Is.GreaterThan(0));foreach(var slot in slots){var source=renderers[(int)Get(slot,"rendererIndex")];int index=(int)Get(slot,"materialIndex");var target=ghost.Find(Relative(source.transform,((Component)host).transform)).GetComponent<Renderer>();CollectionAssert.AreEqual(source.sharedMaterials,target.sharedMaterials);var a=new MaterialPropertyBlock();var b=new MaterialPropertyBlock();source.GetPropertyBlock(a,index);target.GetPropertyBlock(b,index);Assert.That(b.GetColor("_BaseColor"),Is.EqualTo(a.GetColor("_BaseColor")));}
    }
    IEnumerator Exercise(bool snake)
    {
        yield return Load(snake);var trucks=((Array)Get(match,"trucks")).Cast<Component>().ToArray();Assert.That(trucks.Length,Is.EqualTo(4));var hosts=trucks.Select(truck=>Get(truck,"appearanceHost")).ToArray();Assert.That(hosts.All(host=>host!=null),Is.True,"Invoke BindMissingFareAndSnakeHosts first.");var ids=new string[4];
        for(int i=0;i<4;i++){ids[i]=(string)Get(Registry("Create","Same Legacy Driver"),"id");Registry("UseInSeat",i,ids[i]);Save(ids[i],i==0?"truck.fire":i==1?"truck.monster":"novelty.icecream");Assert.That(Get(hosts[i],"participantSlot"),Is.EqualTo(i));Assert.That(Get(trucks[i],"slot"),Is.EqualTo(i));Assert.That(Get(hosts[i],"presentationParent"),Is.SameAs(trucks[i].transform));Assert.That(Get(hosts[i],"fragmentPresentation"),Is.Null);}
        Call(match,"RefreshParticipants",false);var before=trucks.Select(Physics).ToArray();var gear=Gear(hosts);Call(match,"PrepareAppearances");CollectionAssert.AreEqual(before,trucks.Select(Physics));Assert.That(Gear(hosts),Is.EqualTo(gear));Assert.That(Call(match,"StartMatch"),Is.True);
        double deadline=Time.realtimeSinceStartupAsDouble+10;while((float)Get(match,"Intro")>0&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;Assert.That((float)Get(match,"Intro"),Is.LessThanOrEqualTo(0),"Observe the actual original countdown/startup before controlled appearance samples.");((Behaviour)match).enabled=false;yield return null;yield return null;
        var physical=trucks.Select(Physics).ToArray();var views=match.GetComponent(Sports("SportsDrivingViews"));var cabs=((Transform[])Get(views,"cabs")).ToArray();var convoys=snake?((Array)Get(match,"convoys")).Cast<object>().ToArray():null;
        var attachment=convoys?.Select(convoy=>(Transform)Get(convoy,"firstTractorAttachment")).ToArray();var history=convoys?.Select(convoy=>Get(convoy,"history")).ToArray();var pools=convoys?.Select(convoy=>((IEnumerable)Get(convoy,"pool")).Cast<object>().ToArray()).ToArray();var actions=snake?null:Get(match,"interactions");var gestures=actions!=null?((Array)Get(actions,"gestures")).Cast<object>().ToArray():null;var cabJugs=actions!=null?((Transform[])Get(actions,"cabJugs")).ToArray():null;var gps=snake?null:((Array)Get(Get(match,"navigation"),"gps")).Cast<object>().ToArray();
        for(int i=0;i<4;i++){var binding=cabs[i].GetComponent(Sports("SportsTruckAppearanceCabBinding"));Assert.That(binding,Is.Not.Null);Assert.That(Get(binding,"drivingViews"),Is.SameAs(views));Assert.That(Get(binding,"viewIndex"),Is.EqualTo(i));}
        foreach(string body in new[]{"truck.fire","truck.monster","truck.lifted","semi.lowrider","novelty.hotdog","novelty.icecream","semi.standard"})
        {
            Save(ids[0],body);Call(match,"RefreshParticipants",false);gear=Gear(hosts);var hitch=convoys?.Select(convoy=>(Vector3)Get(convoy,"FirstTractorCouplingWorld")).ToArray();Call(match,"PrepareAppearances");Assert.That(Gear(hosts),Is.EqualTo(gear));CollectionAssert.AreEqual(physical,trucks.Select(Physics));
            for(int i=0;i<4;i++){Assert.That(Get(hosts[i],"ProfileId"),Is.EqualTo(ids[i]));Assert.That(Get(hosts[i],"EffectiveBodyId"),Is.EqualTo(i==0?body:i==1?"truck.monster":"novelty.icecream"));var model=(Transform)Get(hosts[i],"AppearanceRoot");Assert.That(model,Is.Not.Null);Assert.That(model.GetComponentsInChildren<Rigidbody>(true),Is.Empty);Assert.That(model.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(((Renderer[])Get(hosts[i],"originalBodyRenderers")).All(renderer=>!renderer.enabled),Is.True);Assert.That(((Transform[])Get(views,"cabs"))[i],Is.SameAs(cabs[i]));}
            if(snake)for(int i=0;i<4;i++)
            {
                Assert.That(Get(convoys[i],"firstTractorAttachment"),Is.SameAs(attachment[i]));Assert.That(Get(convoys[i],"history"),Is.SameAs(history[i]));Assert.That(Get(convoys[i],"FirstTractorCouplingWorld"),Is.EqualTo(hitch[i]));Assert.That(Get(convoys[i],"ActiveCount"),Is.EqualTo(1));CollectionAssert.AreEqual(pools[i],((IEnumerable)Get(convoys[i],"pool")).Cast<object>());Assert.That(Call(convoys[i],"UnsafeCompetitorPairs"),Is.EqualTo(0));var vendor=trucks[i].GetComponentsInChildren<Component>(true).Single(component=>component!=null&&component.GetType().Name=="TrailerHitchModuleWrapper");Assert.That(Get(Get(vendor,"module"),"attachmentPoint"),Is.SameAs(attachment[i]));
            }
            else{CollectionAssert.AreEqual(gestures,((Array)Get(actions,"gestures")).Cast<object>());CollectionAssert.AreEqual(cabJugs,(Transform[])Get(actions,"cabJugs"));CollectionAssert.AreEqual(gps,((Array)Get(Get(match,"navigation"),"gps")).Cast<object>());}
        }
        if(!snake)
        {
            Save(ids[0],"truck.fire");Call(match,"RefreshParticipants",false);Call(match,"PrepareAppearances");var review=Get(match,"review");Assert.That(review,Is.Not.Null);Call(review,"ResetRecord");Call(review,"Capture",1f);Call(review,"Capture",1.2f);var record=Get(review,"record");var ghosts=((Transform[])Get(record,"ghosts")).ToArray();var cabNodes=ghosts.Select(ghost=>ghost.Find("Cab")).ToArray();var lightNodes=ghosts.Select(ghost=>ghost.Find("Lights")).ToArray();var roots=Get(record,"roots");var parts=Get(record,"parts");
            Call(hosts[0],"ApplyEffectiveColor",Color.cyan);Call(review,"ResetRecord");Call(review,"Capture",1f);Call(review,"Capture",1.2f);Assert.That(Get(record,"roots"),Is.SameAs(roots));Assert.That(Get(record,"parts"),Is.SameAs(parts));Paint(hosts[0],ghosts[0]);
            Save(ids[0],"novelty.icecream");Call(match,"RefreshParticipants",false);Call(match,"PrepareAppearances");Call(review,"ResetRecord");Call(review,"Capture",1f);Call(review,"Capture",1.2f);Assert.That(Get(record,"roots"),Is.Not.SameAs(roots));for(int i=0;i<4;i++){Assert.That(((Transform[])Get(record,"ghosts"))[i],Is.SameAs(ghosts[i]));Assert.That(ghosts[i].Find("Cab"),Is.SameAs(cabNodes[i]));Assert.That(ghosts[i].Find("Lights"),Is.SameAs(lightNodes[i]));Paint(hosts[i],ghosts[i]);Assert.That(ghosts[i].GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(ghosts[i].GetComponentsInChildren<Rigidbody>(true),Is.Empty);}
            Call(record,"Begin",1f,float.PositiveInfinity);Assert.That(Get(record,"Playing"),Is.EqualTo(true));roots=Get(record,"roots");Assert.That(Call(record,"RefreshAppearanceSources",new int[]{0},true),Is.EqualTo(false));Assert.That(Get(record,"roots"),Is.SameAs(roots));Call(record,"End");
        }
        int revision=(int)Get(hosts[0],"AppearanceRevision");Call(match,"SetupScreen");Assert.That(Call(match,"StartMatch"),Is.True);Assert.That(Get(hosts[0],"AppearanceRevision"),Is.EqualTo(revision));CollectionAssert.AreEqual(physical,trucks.Select(Physics));
        TestContext.WriteLine("LEGACY_FIXED_FACADE_APPEARANCE "+(snake?"SNAKE":"FARE")+" fouractualfixedIDs/all7bodies, originalNWH/cabs/physics and "+(snake?"actualfifthwheel/history/pool/collisionfilters":"jug/gesture/GPS and source+ghost per-slotpaint/bufferreuse/topology/playingguard")+" retained throughcontrolledsamples andgenuinerematch. Originalcountdown observed; no complete-match/hardware/visual acceptance claim.");
    }
    [UnityTest,Timeout(180000)]public IEnumerator FareFourBodiesPreserveCabsJugGpsAndExistingRecordedReview(){yield return Exercise(false);}
    [UnityTest,Timeout(180000)]public IEnumerator SnakeFourBodiesPreserveActualFifthWheelConvoyAndPhysicalEnvelope(){yield return Exercise(true);}
}
#endif
