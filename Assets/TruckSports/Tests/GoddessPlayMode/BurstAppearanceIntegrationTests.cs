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

public sealed class BurstAppearanceIntegrationTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
    static object Registry(string name,params object[] args)=>Sports("SportsParticipantRegistry").GetMethod(name).Invoke(null,args);
    Component match;IDisposable lease;GameObject isolation;
    [TearDown]public void Cleanup(){Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");if(isolation!=null)UnityEngine.Object.DestroyImmediate(isolation);Registry("ForgetCache");lease?.Dispose();lease=null;}
    IEnumerator Load(string mode,string type,int count)
    {
        lease=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"BurstAppearance"+Guid.NewGuid().ToString("N")});Registry("ForgetCache");isolation=new GameObject("Explicit real-rig fragment appearance fixture");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Sports("SportsProbeInputIsolation"));
        EditorSceneManager.LoadSceneInPlayMode("Assets/"+mode+"/Scenes/"+mode+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
        match=(Component)UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS."+mode+"."+type+", Assembly-CSharp",true));Assert.That(match,Is.Not.Null);Call(match,"AllBots");Set(Get(match,"setup"),"count",count);Call(match,"NormalizeSetup");Assert.That(Get(Get(match,"setup"),"count"),Is.EqualTo(count));
    }
    static string Physics(Component truck)
    {
        var text=new StringBuilder(truck.transform.localScale.ToString("R"));foreach(var body in truck.GetComponentsInChildren<Rigidbody>(true))text.Append("/B").Append(body.GetInstanceID()).Append(':').Append(body.mass).Append(':').Append(body.centerOfMass.ToString("R"));
        foreach(var shape in truck.GetComponentsInChildren<Collider>(true)){text.Append("/C").Append(shape.GetInstanceID()).Append(':').Append(shape.GetType().Name).Append(':').Append(shape.isTrigger);if(shape is BoxCollider box)text.Append(':').Append(box.center.ToString("R")).Append(':').Append(box.size.ToString("R"));if(shape is MeshCollider mesh)text.Append(':').Append(mesh.sharedMesh!=null?mesh.sharedMesh.GetInstanceID():0);}
        foreach(var wheel in (IEnumerable)Get(truck,"wheels")){var data=Get(wheel,"wheel");var spring=Get(wheel,"spring");text.Append("/W").Append(((Component)wheel).GetInstanceID()).Append(':').Append(Get(data,"radius")).Append(':').Append(Get(data,"width")).Append(':').Append(Get(data,"mass")).Append(':').Append(Get(spring,"maxLength")).Append(':').Append(Get(spring,"maxForce"));}
        using(var hash=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())));
    }
    static void Save(string id,string body,object catalog)
    {
        var draft=Registry("GetAppearance",id);Set(draft,"bodyId",body);var selections=(IList)Get(draft,"selections");selections.Clear();
        foreach(string slot in new[]{"hat","face.eyes","face.brows","face.mouth","face.mustache","face.lashes","mirrors","wheels","rear.ornament"})
        {
            if(body=="truck.monster"&&slot=="wheels")continue; // Exercise actual oversized body-default tires too.
            var item=((IEnumerable)Get(catalog,"cosmetics")).Cast<object>().FirstOrDefault(candidate=>(string)Get(candidate,"slotId")==slot&&(GameObject)Get(candidate,"visualPrefab")!=null&&(bool)Call(candidate,"CompatibleWith",body));if(item==null)continue;
            var selected=Activator.CreateInstance(Sports("SportsTruckCosmeticSelection"));Set(selected,"slotId",slot);Set(selected,"itemId",Get(item,"id"));selections.Add(selected);
        }
        Registry("TrySetAppearance",id,draft);Registry("ForgetCache");var actual=Registry("GetAppearance",id);Assert.That(Get(actual,"bodyId"),Is.EqualTo(body));Assert.That(((IList)Get(actual,"selections")).Count,Is.EqualTo(selections.Count));
    }
    static void GeometryAndMaterials(object presenter)
    {
        var graphics=((IEnumerable)Get(presenter,"graphics")).Cast<object>().ToArray();Assert.That(graphics.Length,Is.GreaterThan(12));
        foreach(var graphic in graphics)
        {
            var source=(Renderer)Get(graphic,"source");var target=(Renderer)Get(graphic,"target");var input=(Mesh)Get(graphic,"input");var owned=(Mesh)Get(graphic,"owned");int material=(int)Get(graphic,"sourceMaterial");Assert.That(owned,Is.Not.SameAs(input));Assert.That(target.enabled,Is.True);Assert.That(target.gameObject.activeInHierarchy,Is.True);
            Assert.That(((Renderer[])Get(Get(presenter,"host"),"ExteriorRenderers")).Contains(target),Is.True,"Owned custom fragment graphics must remain in the existing owner-cab exterior hiding authority.");
            var a=input.vertices;var b=owned.vertices;Assert.That(b.Length,Is.EqualTo(a.Length));int stride=Mathf.Max(1,a.Length/31);
            for(int i=0;i<a.Length;i+=stride)Assert.That(Vector3.Distance(source.transform.TransformPoint(a[i]),target.transform.TransformPoint(b[i])),Is.LessThan(.002f),"Actual pre-burst body/cosmetic vertices must remain at their exact prior art positions when attached to the unchanged physical piece.");
            if(input.normals.Length==input.vertexCount)for(int i=0;i<a.Length;i+=stride){var original=source.localToWorldMatrix.inverse.transpose.MultiplyVector(input.normals[i]).normalized;var actual=target.localToWorldMatrix.inverse.transpose.MultiplyVector(owned.normals[i]).normalized;if(original.sqrMagnitude>.5f)Assert.That(Vector3.Angle(original,actual),Is.LessThan(.1f),"Mirrored source normals must survive render-only baking.");}
            if(material<0)CollectionAssert.AreEqual(source.sharedMaterials,target.sharedMaterials);else Assert.That(target.sharedMaterials[0],Is.SameAs(source.sharedMaterials[material]));
            var x=new MaterialPropertyBlock();var y=new MaterialPropertyBlock();source.GetPropertyBlock(x);target.GetPropertyBlock(y);Assert.That(y.GetColor("_EmissionColor"),Is.EqualTo(x.GetColor("_EmissionColor")));
            for(int slot=0;slot<(material<0?source.sharedMaterials.Length:1);slot++){x.Clear();y.Clear();source.GetPropertyBlock(x,material<0?slot:material);target.GetPropertyBlock(y,slot);Assert.That(y.GetColor("_BaseColor"),Is.EqualTo(x.GetColor("_BaseColor")));Assert.That(y.GetColor("_Color"),Is.EqualTo(x.GetColor("_Color")));}
            Assert.That(target.GetComponents<Collider>(),Is.Empty);Assert.That(target.GetComponents<Rigidbody>(),Is.Empty);
        }
    }
    static int[] Path(Transform part,Transform root){var path=new System.Collections.Generic.List<int>();while(part!=root){path.Add(part.GetSiblingIndex());part=part.parent;Assert.That(part,Is.Not.Null);}path.Reverse();return path.ToArray();}
    static Transform At(Transform root,int[] path){foreach(int index in path)root=root.GetChild(index);return root;}
    static void EarlierBurstRemainsRecorded(object presenter,object rig,Component truck,object review,int count)
    {
        var graphics=((IEnumerable)Get(presenter,"graphics")).Cast<object>().ToArray();var pieces=((Array)Get(rig,"pieces")).Cast<object>().ToArray();var root=((Rigidbody)Get(pieces[0],"body")).transform.parent;
        var paths=graphics.Select(graphic=>Path(((Renderer)Get(graphic,"target")).transform,root)).ToArray();var local=graphics.Select(graphic=>((Mesh)Get(graphic,"owned")).vertices.ToArray()).ToArray();var world=graphics.Select((graphic,i)=>local[i].Select(point=>((Renderer)Get(graphic,"target")).transform.TransformPoint(point)).ToArray()).ToArray();
        var wheel=((IEnumerable)Get(truck,"wheels")).Cast<object>().First();var container=(Transform)Get(Get(wheel,"wheel"),"rotatingContainer");var rotation=container.rotation;
        try
        {
            Call(rig,"ResetBody");container.rotation=rotation*Quaternion.Euler(47,0,0);Call(Get(truck,"appearanceHost"),"LateUpdate");var body=(Rigidbody)Get(truck,"body");body.isKinematic=false;body.detectCollisions=true;Call(rig,"Burst");GeometryAndMaterials(presenter);
            for(int i=0;i<graphics.Length;i++)CollectionAssert.AreEqual(local[i],((Mesh)Get(graphics[i],"owned")).vertices,"A second burst must not rewrite a shared mesh used by an earlier recorded heat.");
            Call(review,"Capture",4.4f);var record=Get(review,"record");Call(record,"Begin",4f,float.PositiveInfinity);Call(record,"Seek",4.2f);var ghost=((Transform[])Get(record,"ghosts"))[count];Assert.That(ghost.gameObject.activeSelf,Is.True);
            for(int i=0;i<graphics.Length;i++){var target=At(ghost,paths[i]);var vertices=target.GetComponent<MeshFilter>().sharedMesh.vertices;int stride=Mathf.Max(1,vertices.Length/31);for(int v=0;v<vertices.Length;v+=stride)Assert.That(Vector3.Distance(target.TransformPoint(vertices[v]),world[i][v]),Is.LessThan(.002f),"The original recorder must reproduce the first burst's actual graphic pose after a later burst.");}Call(record,"End");
        }
        finally{container.rotation=rotation;}
    }
    IEnumerator Exercise(string mode,string type,int count)
    {
        yield return Load(mode,type,count);var trucks=((Array)Get(match,"trucks")).Cast<Component>().ToArray();var rigs=((Array)Get(match,"fragments")).Cast<Component>().ToArray();var host=Get(trucks[0],"appearanceHost");Assert.That(host,Is.Not.Null,"Run the explicit private recipe and Burst host authors first.");var presenter=Get(host,"fragmentPresentation");Assert.That(presenter,Is.Not.Null);var catalog=Get(host,"catalog");
        var ids=new string[count];for(int i=0;i<count;i++){ids[i]=(string)Get(Registry("Create","Same Burst Driver"),"id");Registry("UseInSeat",i,ids[i]);Save(ids[i],i%2==0?"truck.fire":"truck.monster",catalog);Assert.That(Get(trucks[i],"slot"),Is.EqualTo(i));Assert.That(Get(Get(trucks[i],"appearanceHost"),"participantSlot"),Is.EqualTo(i));}
        Call(match,"RefreshParticipants",false);Assert.That(Call(match,"StartMatch"),Is.True);((Behaviour)match).enabled=false;yield return null;yield return null;var physical=trucks.Select(Physics).ToArray();var pieceObjects=((Array)Get(rigs[0],"pieces")).Cast<object>().ToArray();var originalPieces=pieceObjects.Select(piece=>(Rigidbody)Get(piece,"body")).ToArray();var originalColliders=pieceObjects.Select(piece=>(Collider)Get(piece,"collider")).ToArray();
        var sampleReview=Get(match,"review");Assert.That(sampleReview,Is.Not.Null);
        foreach(string body in new[]{"truck.fire","truck.monster","truck.lifted","semi.lowrider","novelty.hotdog","novelty.icecream","semi.standard"})
        {
            Save(ids[0],body,catalog);Call(match,"RefreshParticipants",false);Call(match,"PrepareAppearances");Assert.That(Get(host,"EffectiveBodyId"),Is.EqualTo(body));Assert.That(Get(presenter,"PreparedRevision"),Is.EqualTo(Get(host,"AppearanceRevision")));int builds=(int)Get(presenter,"PoolBuilds");
            Call(rigs[0],"ResetBody");var owner=(Rigidbody)Get(trucks[0],"body");owner.isKinematic=false;owner.detectCollisions=true;owner.linearVelocity=new Vector3(2,1,3);owner.angularVelocity=new Vector3(.1f,.2f,-.1f);
            var velocity=new Vector3[12];var angular=new Vector3[12];for(int i=0;i<12;i++){var source=(Transform)Get(pieceObjects[i],"source");var mesh=source.GetComponent<MeshFilter>().sharedMesh;var direction=source.TransformPoint(mesh.bounds.center)-owner.worldCenterOfMass;if(direction.sqrMagnitude<.01f)direction=Vector3.up;direction.Normalize();velocity[i]=Vector3.ClampMagnitude(owner.GetPointVelocity(source.position),30)+direction*4+Vector3.up*4;angular[i]=Vector3.ClampMagnitude(owner.angularVelocity,10)+new Vector3(4,6,3);}
            Call(sampleReview,"ResetRecord");Call(sampleReview,"Capture",4f);int bursts=(int)Get(presenter,"ActualBursts");Call(rigs[0],"Burst");Assert.That(Get(presenter,"ActualBursts"),Is.EqualTo(bursts+1));Assert.That(Get(presenter,"LastBurstBodyId"),Is.EqualTo(body));Assert.That(Get(presenter,"PoolBuilds"),Is.EqualTo(builds));GeometryAndMaterials(presenter);Call(sampleReview,"Capture",4.2f);
            for(int i=0;i<12;i++){Assert.That(Get(pieceObjects[i],"body"),Is.SameAs(originalPieces[i]));Assert.That(Get(pieceObjects[i],"collider"),Is.SameAs(originalColliders[i]));Assert.That(originalColliders[i].enabled,Is.True);Assert.That(originalPieces[i].isKinematic,Is.False);Assert.That(((Renderer)Get(pieceObjects[i],"renderer")).enabled,Is.False,"No original semi wreck rendering may appear under the selected body.");Assert.That(Vector3.Distance(originalPieces[i].linearVelocity,velocity[i]),Is.LessThan(.0002f));Assert.That(Vector3.Distance(originalPieces[i].angularVelocity,angular[i]),Is.LessThan(.0002f));}
            CollectionAssert.AreEqual(physical,trucks.Select(Physics));Call(rigs[0],"Burst");Assert.That(Get(presenter,"ActualBursts"),Is.EqualTo(bursts+1));
            if(body=="truck.monster")EarlierBurstRemainsRecorded(presenter,rigs[0],trucks[0],sampleReview,count);
            Call(match,"Pause",0,false);Assert.That(Get(match,"Paused"),Is.EqualTo(true));Assert.That(Time.timeScale,Is.EqualTo(0f));yield return null;yield return null;var positions=originalPieces.Select(piece=>piece.position).ToArray();yield return null;yield return null;CollectionAssert.AreEqual(positions,originalPieces.Select(piece=>piece.position),"After the real pause boundary settles, every physical fragment must remain exactly stationary.");Call(match,"Resume");Assert.That(Get(match,"Paused"),Is.EqualTo(false));Assert.That(Time.timeScale,Is.EqualTo(1f));
            Call(rigs[0],"ResetBody");Assert.That(Get(presenter,"PoolBuilds"),Is.EqualTo(builds));Assert.That(originalPieces.All(piece=>piece.isKinematic&&piece.transform.localScale==Vector3.zero),Is.True);Assert.That(originalColliders.All(shape=>!shape.enabled),Is.True);Assert.That(((Transform)Get(host,"AppearanceRoot")).GetComponentsInChildren<Renderer>(true).Any(renderer=>renderer.enabled),Is.True);CollectionAssert.AreEqual(physical,trucks.Select(Physics));
            TestContext.WriteLine("CONTROLLED_ACTUAL_RIG_BURST "+mode+" selected="+body+" original12physics/forces retained; actualbody+cosmetic vertices/normals/materials/MPBs match before art; pool reused through duplicate/reset/pause.");
        }
        // Pure missing-recipe evaluation must not spawn a model or rewrite IDs.
        Save(ids[0],"truck.fire",catalog);Call(match,"RefreshParticipants",false);Call(match,"PrepareAppearances");var recipes=(Array)Get(presenter,"recipes");int fire=Array.FindIndex(recipes.Cast<object>().ToArray(),recipe=>(string)Get(recipe,"bodyId")=="truck.fire");var saved=recipes.GetValue(fire);var oldModel=Get(host,"AppearanceRoot");int revision=(int)Get(host,"AppearanceRevision");
        try{recipes.SetValue(null,fire);var report=Call(host,"EvaluateDraft",Registry("GetAppearance",ids[0]));Assert.That(Get(report,"effectiveBodyId"),Is.EqualTo("semi.standard"));Assert.That((string)Get(report,"notice"),Does.Contain("saved"));Assert.That(Get(host,"AppearanceRoot"),Is.SameAs(oldModel));Assert.That(Get(host,"AppearanceRevision"),Is.EqualTo(revision));Assert.That(Get(Registry("GetAppearance",ids[0]),"bodyId"),Is.EqualTo("truck.fire"));}finally{recipes.SetValue(saved,fire);}
        Call(match,"SetupScreen");Assert.That(Call(match,"StartMatch"),Is.True);((Behaviour)match).enabled=true;
        // Observe ordinary countdowns/timers. Only the labelled controlled
        // initial body pose/velocity changes below set up real mode boundaries.
        double readyDeadline=Time.realtimeSinceStartupAsDouble+10;
        while(Time.realtimeSinceStartupAsDouble<readyDeadline)
        {
            bool ready=mode=="TruckRedLightGreenLight"?Get(Get(match,"rules"),"Phase").ToString()!="Countdown":mode=="TruckAvalancheEscape"?Get(Get(match,"rules"),"Phase").ToString()=="Racing":(float)Get(match,"Countdown")<=0;
            if(ready)break;yield return null;
        }
        var truckBody=(Rigidbody)Get(trucks[0],"body");Vector3 position=truckBody.position;
        if(mode=="TruckRedLightGreenLight")position.x=(float)Get(match,"trackHalfWidth")+20;
        else if(mode=="TruckSeesaw"){position=(Vector3)Get(match,"platformOrigin")+new Vector3((float)Get(match,"halfWidth")+18,-8,0);truckBody.linearVelocity=Vector3.down*8;}
        else if(mode=="TruckFloorIsLava"){position=((Transform)Get(match,"lava")).position+Vector3.up*9;truckBody.linearVelocity=Vector3.down*5;}
        else if(mode=="TruckAvalancheEscape")position.x=(float)Get(Get(match,"world"),"halfWidth")+80;
        if(mode!="TruckHotPotato"){truckBody.position=position;trucks[0].transform.position=position;UnityEngine.Physics.SyncTransforms();}
        double deadline=Time.realtimeSinceStartupAsDouble+(mode=="TruckHotPotato"?30:15);Component actual=null;
        while(Time.realtimeSinceStartupAsDouble<deadline){actual=rigs.FirstOrDefault(rig=>(bool)Get(rig,"Bursting"));if(actual!=null)break;yield return null;}
        Assert.That(actual,Is.Not.Null,"The original mode countdown/timer/contact/boundary must actually trigger its existing Burst.");var actualHost=Get(actual,"appearanceHost");var actualPresenter=Get(actual,"fragmentPresentation");Assert.That(Get(actualPresenter,"LastBurstBodyId"),Is.EqualTo(Get(actualHost,"EffectiveBodyId")));Assert.That((int)Get(actualPresenter,"ActualBursts"),Is.GreaterThan(0));
        ((Behaviour)match).enabled=false;var review=Get(match,"review");float cursor=Mathf.Max(40,(float)Get(Get(review,"record"),"Duration")+.2f);Call(review,"Capture",cursor);Call(review,"Capture",cursor+.2f);var record=Get(review,"record");int slot=Array.IndexOf(rigs,actual);Call(record,"Begin",cursor,float.PositiveInfinity);Call(record,"Seek",cursor+.1f);
        var ghosts=(Transform[])Get(record,"ghosts");Assert.That(ghosts[slot].gameObject.activeSelf,Is.False);Assert.That(ghosts[count+slot].gameObject.activeSelf,Is.True,"The existing mask must select the actual custom debris hierarchy.");Assert.That(ghosts[count+slot].GetComponentsInChildren<Rigidbody>(true),Is.Empty);Assert.That(ghosts[count+slot].GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(ghosts[count+slot].GetComponentsInChildren<Transform>(true).Length,Is.EqualTo(((Component)actual).transform.GetComponentsInChildren<Transform>(true).First(part=>part.name=="Pooled actual NWH truck mesh fragments").GetComponentsInChildren<Transform>(true).Length));Call(record,"End");
        TestContext.WriteLine("ACTUAL_MODE_BURST "+mode+" slot="+slot+" selected="+Get(actualPresenter,"LastBurstBodyId")+" observed through original live mode boundary/timer/contact after labelled pose setup; existing debris mask/ghost physics isolation retained. No full-match/controller/art acceptance claim.");
    }
    [UnityTest,Timeout(180000)]public IEnumerator RedTwelveOriginalBoundaryAndSelectedBodyFragments(){yield return Exercise("TruckRedLightGreenLight","RedLightMatch",12);}
    [UnityTest,Timeout(180000)]public IEnumerator HotEightOriginalFuseAndSelectedBodyFragments(){yield return Exercise("TruckHotPotato","HotPotatoMatch",8);}
    [UnityTest,Timeout(180000)]public IEnumerator SeesawFourActualQuarryImpactAndSelectedBodyFragments(){yield return Exercise("TruckSeesaw","SeesawMatch",4);}
    [UnityTest,Timeout(180000)]public IEnumerator LavaEightActualSurfaceCrossingAndSelectedBodyFragments(){yield return Exercise("TruckFloorIsLava","LavaMatch",8);}
    [UnityTest,Timeout(180000)]public IEnumerator AvalancheTwelveOriginalRavineAndSelectedBodyFragments(){yield return Exercise("TruckAvalancheEscape","AvalancheMatch",12);}
}
#endif
