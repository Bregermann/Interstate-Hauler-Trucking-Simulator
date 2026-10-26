#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

[DefaultExecutionOrder(32000)]
public sealed class CornholeWheelLateDiagnosticObserver : MonoBehaviour
{
    public Action observe;
    void LateUpdate(){observe?.Invoke();}
}

public sealed class CornholeAirborneWheelAlignmentTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo f?f.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length&&m.GetParameters().Select((p,i)=>args[i]!=null?p.ParameterType.IsInstanceOfType(args[i]):!p.ParameterType.IsValueType).All(ok=>ok)).Invoke(owner,args);
    static object Registry(string name,params object[] args)=>Sports("SportsParticipantRegistry").GetMethod(name).Invoke(null,args);
    [Serializable] sealed class WheelSample
    {
        public string phase; public int frame,wheel; public bool enabled,sourceUnderPhysicalRoot; public Vector3 actorPosition,actorScale,modelScale,canonicalScale,pivotScale,sourceWorld,sourceLocalToActor,suspensionWorld,canonicalMountWorld,visualPivotWorld,meshBoundsCenter,meshLocalCenter,sourceScale;public float radius,springLength,visualDiameter,pivotToMesh,sourceToPivot,canonicalToPivot; public string bodyId;
    }
    [Serializable] sealed class Report {public string scope="Natural original AI flight diagnostic only; no pose/force/score injection. Measurements precede repair, not wheel acceptance.";public WheelSample[] samples;}
    static string Physical(Component truck)
    {
        var b=(Rigidbody)Get(truck,"body");var c=(BoxCollider)Get(truck,"shape");return b.GetInstanceID()+":"+b.mass+":"+b.centerOfMass.ToString("R")+":"+b.linearDamping+":"+b.angularDamping+":"+c.GetInstanceID()+":"+c.center.ToString("R")+":"+c.size.ToString("R")+":"+truck.transform.localScale.ToString("R")+string.Join("/",((IEnumerable)Get(truck,"wheels")).Cast<Component>().Select(w=>w.GetInstanceID()+":"+Get(Get(w,"wheel"),"radius")+":"+Get(Get(w,"spring"),"maxLength")));
    }
    static Vector3 Inertia(Component truck)=>((Rigidbody)Get(truck,"body")).inertiaTensor;
    static void AssertComputedInertia(Component truck,Vector3 expected,ref float observedMaxDelta,string phase)
    {
        var actual=Inertia(truck);var delta=actual-expected;float maximum=Mathf.Max(Mathf.Abs(delta.x),Mathf.Abs(delta.y),Mathf.Abs(delta.z));observedMaxDelta=Mathf.Max(observedMaxDelta,maximum);
        // PhysX recomputes implicit inertia from the same retained colliders;
        // Native47/48 measured 2e-6..1.28e-5 drift across identical retained
        // geometry at different world poses. Use 1e-4 kg*m^2 for this computed
        // value only (under 0.011% of the smallest original principal moment).
        // Authored fields/IDs and all saved physics documents remain exact;
        // wheel alignment tolerances are separate and unchanged.
        Assert.That(Mathf.Abs(delta.x),Is.LessThanOrEqualTo(.0001f),phase+" computed inertia.x expected="+expected.ToString("R")+" actual="+actual.ToString("R"));
        Assert.That(Mathf.Abs(delta.y),Is.LessThanOrEqualTo(.0001f),phase+" computed inertia.y expected="+expected.ToString("R")+" actual="+actual.ToString("R"));
        Assert.That(Mathf.Abs(delta.z),Is.LessThanOrEqualTo(.0001f),phase+" computed inertia.z expected="+expected.ToString("R")+" actual="+actual.ToString("R"));
    }
    static IEnumerable<WheelSample> Measure(Component truck,Component host,string phase)
    {
        var model=(Transform)Get(host,"AppearanceRoot");var canonical=model.Find("Body and semantic mounts/Canonical body");var fit=Call(host,"Fit",Get(host,"EffectiveBodyId"));var groups=((IEnumerable)Get(fit,"wheelGeometry")).Cast<object>().ToArray();var original=((IEnumerable)Get(truck,"wheels")).Cast<Component>().ToArray();
        foreach(var follow in ((IEnumerable)Get(host,"wheelVisuals")).Cast<object>())
        {
            var pivot=(Transform)Get(follow,"visual");var source=(Transform)Get(follow,"source");int index=Array.FindIndex(original,w=>(Transform)Get(Get(w,"wheel"),"rotatingContainer")==source);Assert.That(index,Is.GreaterThanOrEqualTo(0));var group=groups.Single(g=>(int)Get(g,"physicalWheel")==index);var meshes=pivot.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();Assert.That(meshes,Is.Not.Empty);var bounds=meshes[0].bounds;foreach(var r in meshes.Skip(1))bounds.Encapsulate(r.bounds);
            yield return new WheelSample{phase=phase,frame=Time.frameCount,wheel=index,enabled=((Behaviour)original[index]).enabled,sourceUnderPhysicalRoot=source.IsChildOf(truck.transform),actorPosition=truck.transform.position,actorScale=truck.transform.lossyScale,modelScale=model.lossyScale,canonicalScale=canonical.lossyScale,pivotScale=pivot.lossyScale,sourceWorld=source.position,sourceLocalToActor=truck.transform.InverseTransformPoint(source.position),sourceScale=source.lossyScale,suspensionWorld=original[index].transform.position,canonicalMountWorld=canonical.TransformPoint((Vector3)Get(group,"center")),visualPivotWorld=pivot.position,meshBoundsCenter=bounds.center,meshLocalCenter=pivot.InverseTransformPoint(bounds.center),radius=(float)Get(Get(original[index],"wheel"),"radius"),springLength=(float)Get(Get(original[index],"spring"),"length"),visualDiameter=(float)Get(group,"visualDiameter"),pivotToMesh=Vector3.Distance(pivot.position,bounds.center),sourceToPivot=Vector3.Distance(source.position,pivot.position),canonicalToPivot=Vector3.Distance(canonical.TransformPoint((Vector3)Get(group,"center")),pivot.position),bodyId=(string)Get(host,"EffectiveBodyId")};
        }
    }
    static Bounds LocalGeometry(Transform owner,IEnumerable<Renderer> parts)
    {
        bool first=true;Bounds result=default;foreach(var renderer in parts){var bounds=renderer.localBounds;var matrix=owner.worldToLocalMatrix*renderer.localToWorldMatrix;for(int i=0;i<8;i++){var point=matrix.MultiplyPoint3x4(bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));if(first){result=new Bounds(point,Vector3.zero);first=false;}else result.Encapsulate(point);}}Assert.That(first,Is.False);return result;
    }
    static void VerifyAllOwnedFits(Component truck,Component host)
    {
        var source=((Renderer[])Get(host,"originalBodyRenderers")).Concat((Renderer[])Get(host,"originalWheelRenderers")).Where(r=>r.name!="Rubber tire").Distinct().ToArray();var rubber=((Renderer[])Get(host,"originalWheelRenderers")).Where(r=>r.name=="Rubber tire").ToArray();Assert.That(source.Length,Is.EqualTo(33));Assert.That(rubber.Length,Is.EqualTo(4));
        var actual=LocalGeometry(truck.transform,source);var bad=LocalGeometry(truck.transform,source.Concat(rubber));Assert.That(bad.min.y,Is.LessThan(actual.min.y-1),"Original offset Rubber cylinder data is deliberately preserved, but must not define body fitting.");
        var catalog=Get(host,"catalog");foreach(var body in ((IEnumerable)Get(catalog,"bodies")).Cast<object>())
        {
            var fit=Call(host,"Fit",Get(body,"id"));var envelope=(Bounds)Get(body,"visualEnvelope");var rotation=Quaternion.Euler((Vector3)Get(body,"presentationEuler"));var rotated=new Bounds(rotation*envelope.center,Vector3.zero);for(int corner=0;corner<8;corner++)rotated.Encapsulate(rotation*(envelope.center+Vector3.Scale(envelope.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1))));float scale=Mathf.Min(actual.size.x/rotated.size.x,actual.size.y/rotated.size.y,actual.size.z/rotated.size.z);var position=new Vector3(actual.center.x-rotated.center.x*scale,actual.min.y-rotated.min.y*scale,actual.center.z-rotated.center.z*scale);
            Assert.That(Vector3.Distance((Vector3)Get(fit,"scale"),Vector3.one*scale),Is.LessThan(.00003f),"All7 saved fits must use exact33 source geometry, not4 offset legacy cylinders.");Assert.That(Vector3.Distance((Vector3)Get(fit,"position"),position),Is.LessThan(.00003f));
        }
        TestContext.WriteLine("CORN_EXACT_SOURCE_BOUNDS actor="+truck.name+" original33="+actual+" legacyWithRubber="+bad+" physicalBox="+((BoxCollider)Get(truck,"shape")).size);
    }
    static void Capture(Component match,string body,string phase)
    {
        string folder=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/CornCorrectedWheelFits");Directory.CreateDirectory(folder);var canvas=((Component)Get(match,"menus")).GetComponent<Canvas>();var cameras=((Array)Get(match,"courses")).Cast<object>().Select(course=>(Camera)Get(Get(course,"chase"),"view")).Where(c=>c.enabled).ToArray();Sports("SportsControlledCapture").GetMethods().Single(m=>m.Name=="Save"&&m.GetParameters().Length==6).Invoke(null,new object[]{canvas,cameras,1280,720,Path.Combine(folder,body+"-"+phase+"-1280-controlled.png"),null});
    }
    [UnityTest,Timeout(360000)]
    public IEnumerator AllSevenCorrectedMiniatureBodiesKeepFourVendorWheelsAttachedOnGroundAndNaturalFlightAcrossRematches()
    {
        var priorRandom=UnityEngine.Random.state;var prefs=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"CornCorrectedWheels"+Guid.NewGuid().ToString("N")});GameObject isolation=null;Component match=null;CornholeWheelLateDiagnosticObserver observer=null;var measurements=new List<WheelSample>();
        try
        {
            Registry("ForgetCache");isolation=new GameObject("Corrected Corn natural AI wheel input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Sports("SportsProbeInputIsolation"));observer=isolation.AddComponent<CornholeWheelLateDiagnosticObserver>();EditorSceneManager.LoadSceneInPlayMode("Assets/TruckCornhole/Scenes/TruckCornhole.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=(Component)UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS.TruckCornhole.CornholeMatch, Assembly-CSharp",true));var courses=((Array)Get(match,"courses")).Cast<object>().ToArray();var trucks=courses.SelectMany(c=>((Array)Get(c,"trucks")).Cast<Component>()).ToArray();Assert.That(trucks.Length,Is.EqualTo(16));var identities=trucks.Select(t=>t.GetInstanceID()).ToArray();foreach(var t in trucks)VerifyAllOwnedFits(t,(Component)Get(t,"appearanceHost"));
            string[] ids=new string[4];for(int i=0;i<4;i++){ids[i]=(string)Get(Registry("Create","Same wheel driver"),"id");Registry("UseInSeat",i,ids[i]);}Assert.That(ids.Distinct().Count(),Is.EqualTo(4));Call(match,"RefreshParticipants",false);
            // Original NWH Start lazily creates wheel mesh colliders; original
            // Launch disables them, and PhysX computes the chassis-only implicit
            // inertia. Warm ALL16 actual deliveries through that original boundary,
            // never compare serialized default inertia(1,1,1) to an initialized rig.
            var originalHosts=trucks.Select(t=>Get(t,"appearanceHost")).ToArray();string[] originalFirstAir=null;Vector3[] originalFirstAirInertia=null;
            try
            {
                foreach(var t in trucks)Set(t,"appearanceHost",null);Set(Get(match,"setup"),"count",4);Call(match,"AllAI");foreach(var player in ((Array)Get(Get(match,"setup"),"players")).Cast<object>())Set(player,"difficulty",Enum.Parse(Get(player,"difficulty").GetType(),"Hard"));var originalPresets=Get(match,"weatherPresets");var originalClear=((IEnumerable)Get(originalPresets,"entries")).Cast<object>().First(e=>(string)Get(e,"displayName")=="Clear");Call(originalPresets,"Select",Get(originalClear,"id"));UnityEngine.Random.InitState(174207);Assert.That((bool)Call(match,"StartMatch"),Is.True);
                float firstDeadline=Time.realtimeSinceStartup+40;while(!trucks.Where((t,i)=>i%4==0).All(t=>(bool)Get(t,"Launched"))&&Time.realtimeSinceStartup<firstDeadline)yield return null;Assert.That(trucks.Where((t,i)=>i%4==0).All(t=>(bool)Get(t,"Launched")),Is.True);float firstAirUntil=Time.realtimeSinceStartup+.65f;while(Time.realtimeSinceStartup<firstAirUntil)yield return null;originalFirstAir=trucks.Where((t,i)=>i%4==0).Select(Physical).ToArray();originalFirstAirInertia=trucks.Where((t,i)=>i%4==0).Select(Inertia).ToArray();
                float originalDeadline=Time.realtimeSinceStartup+150;while(!(bool)Get(match,"RoundBreak")&&Time.realtimeSinceStartup<originalDeadline)yield return null;Assert.That((bool)Get(match,"RoundBreak"),Is.True,"Original no-appearance AI baseline must complete its real first round.");
                Assert.That(trucks.All(t=>(int)Get(t,"LaunchCount")==1),Is.True,"All16 original retained actors must pass genuine Launch before inertia baseline.");foreach(var t in trucks)foreach(var wheel in ((IEnumerable)Get(t,"wheels")).Cast<Component>()){Assert.That((bool)Get(wheel,"_initialized"),Is.True);Assert.That(Get(Get(wheel,"wheel"),"meshCollider"),Is.Not.Null);Assert.That(((Behaviour)wheel).enabled,Is.False);}
                yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();yield return null;Call(match,"ChangeSetup");TestContext.WriteLine("CORN_ORIGINAL_PHYSICS_BASELINE originalHardClear174207 actual16LaunchCount1/64initializedNWHcontrollers+originalcolliders, completedRoundBreak; no appearance preparation/forced pose/force/result/inertia writes.");
            }
            finally{for(int i=0;i<trucks.Length;i++)Set(trucks[i],"appearanceHost",originalHosts[i]);}
            var physics=trucks.Select(Physical).ToArray();var expectedInertia=trucks.Select(Inertia).ToArray();float maxInertiaDelta=0;Assert.That(originalFirstAir,Is.Not.Null);Assert.That(originalFirstAirInertia,Is.Not.Null);for(int owner=0;owner<4;owner++){physics[owner*4]=originalFirstAir[owner];expectedInertia[owner*4]=originalFirstAirInertia[owner];}
            // Per original lifecycle phase: first deliveries are compared in
            // actual flight; the other12 are compared in their initialized parked
            // pool state. Holed/disabled original colliders may legitimately have
            // a different implicit inertia from an airborne chassis.
            foreach(string bodyId in new[]{"semi.standard","truck.monster","semi.lowrider","truck.lifted","truck.fire","novelty.hotdog","novelty.icecream"})
            {
                Call(match,"ChangeSetup");Set(Get(match,"setup"),"count",4);for(int i=0;i<4;i++){var draft=Registry("GetAppearance",ids[i]);Set(draft,"bodyId",bodyId);((IList)Get(draft,"selections")).Clear();Registry("TrySetAppearance",ids[i],draft);}Call(match,"RefreshParticipants",false);Call(match,"AllAI");var presets=Get(match,"weatherPresets");var clear=((IEnumerable)Get(presets,"entries")).Cast<object>().First(e=>(string)Get(e,"displayName")=="Clear");Call(presets,"Select",Get(clear,"id"));Sports("SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{false});UnityEngine.Random.InitState(174207);Assert.That((bool)Call(match,"StartMatch"),Is.True);string saved=string.Join("|",ids.Select(id=>JsonUtility.ToJson(Registry("GetAppearance",id))));
                foreach(var t in trucks){var host=(Component)Get(t,"appearanceHost");int owner=(int)Get(Get(t,"course"),"index");Assert.That(Get(host,"ProfileId"),Is.EqualTo(ids[owner]));Assert.That(Get(host,"participantSlot"),Is.EqualTo(owner));Assert.That(Get(host,"EffectiveBodyId"),Is.EqualTo(bodyId));Assert.That(((Renderer[])Get(host,"originalWheelRenderers")).Length,Is.EqualTo(8));}
                var truck=trucks[0];var activeHost=(Component)Get(truck,"appearanceHost");var perBody=new List<WheelSample>();observer.observe=()=>perBody.AddRange(Measure(truck,activeHost,"LateUpdate32000"));yield return new WaitForFixedUpdate();yield return null;yield return null;Assert.That((bool)Get(truck,"Launched"),Is.False);Assert.That(perBody.Any(r=>r.enabled),Is.True);Capture(match,bodyId,"ground");
                float deadline=Time.realtimeSinceStartup+35;while(!(bool)Get(truck,"Launched")&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That((bool)Get(truck,"Launched"),Is.True);float until=Time.realtimeSinceStartup+.65f;while(Time.realtimeSinceStartup<until)yield return null;observer.observe=null;Capture(match,bodyId,"natural-air");
                Assert.That(perBody.Count(r=>!r.enabled),Is.GreaterThanOrEqualTo(16));foreach(var sample in perBody){Assert.That(sample.pivotToMesh,Is.LessThan(.00004f),"Actual visible tire mesh must stay centered on following pivot in actual late-render phase.");float offset=Mathf.Abs(sample.visualDiameter-sample.radius*2)*.5f;Assert.That(sample.sourceToPivot,Is.EqualTo(offset).Within(.00004f),"Exact original NWH pose plus intentional tire-radius offset, enabled or disabled.");Assert.That(sample.canonicalToPivot,Is.LessThan(.6f),"All7 original-scale bodies must have their authored tire anchors within a miniature chassis length of the actual rolling centers; old4m displacement is invalid.");}
                measurements.AddRange(perBody);CollectionAssert.AreEqual(identities,trucks.Select(t=>t.GetInstanceID()).ToArray());CollectionAssert.AreEqual(physics,trucks.Select(Physical).ToArray());for(int i=0;i<trucks.Length;i++)AssertComputedInertia(trucks[i],expectedInertia[i],ref maxInertiaDelta,bodyId+" actor="+i);Assert.That(string.Join("|",ids.Select(id=>JsonUtility.ToJson(Registry("GetAppearance",id)))),Is.EqualTo(saved));TestContext.WriteLine("CORN_CORRECTED_WHEEL_BODY "+bodyId+" lateSamples="+perBody.Count+" maxBodyAnchorToVendorWheel="+perBody.Max(r=>r.canonicalToPivot)+" maxActualMeshToPivot="+perBody.Max(r=>r.pivotToMesh)+" maxComputedInertiaAbsoluteDelta="+maxInertiaDelta.ToString("R")+" inertiaAbsoluteTolerance=1e-4; all16fixed owners/fits/original authored config exact.");
            }
        }
        finally
        {
            if(observer!=null)observer.observe=null;string folder=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/CornCorrectedWheelFits");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"all-seven-natural-wheel-centers.json"),JsonUtility.ToJson(new Report{scope="Corrected body fit ground and natural flight; original AI/physics/configurations retained, actual late phase and controlled render captures.",samples=measurements.ToArray()},true));Time.timeScale=1;if(match!=null)Call(match,"ChangeSetup");if(isolation!=null)UnityEngine.Object.DestroyImmediate(isolation);UnityEngine.Random.state=priorRandom;Registry("ForgetCache");prefs.Dispose();
        }
    }
    [UnityTest,Timeout(90000)]
    public IEnumerator NaturalAiFlightRecordsActualWheelMeshCentersAndVendorAnchorsBeforeAnyVisualRepair()
    {
        var priorRandom=UnityEngine.Random.state;var prefs=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"CornNaturalWheelDiagnostic"+Guid.NewGuid().ToString("N")});GameObject isolation=null;CornholeWheelLateDiagnosticObserver observer=null;Component match=null;var samples=new List<WheelSample>();
        try
        {
            Registry("ForgetCache");isolation=new GameObject("Original Corn natural AI wheel diagnostic input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Sports("SportsProbeInputIsolation"));EditorSceneManager.LoadSceneInPlayMode("Assets/TruckCornhole/Scenes/TruckCornhole.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            match=(Component)UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS.TruckCornhole.CornholeMatch, Assembly-CSharp",true));var setup=Get(match,"setup");Set(setup,"count",1);Call(match,"AllAI");var id=(string)Get(Registry("Create","Natural wheel diagnostic driver"),"id");Registry("UseInSeat",0,id);var draft=Registry("GetAppearance",id);Set(draft,"bodyId","novelty.hotdog");((IList)Get(draft,"selections")).Clear();Registry("TrySetAppearance",id,draft);Call(match,"RefreshParticipants",false);Call(match,"AllAI");var presets=Get(match,"weatherPresets");var clear=((IEnumerable)Get(presets,"entries")).Cast<object>().First(e=>(string)Get(e,"displayName")=="Clear");Call(presets,"Select",Get(clear,"id"));Sports("SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{false});UnityEngine.Random.InitState(174207);Assert.That((bool)Call(match,"StartMatch"),Is.True);
            var course=((Array)Get(match,"courses")).GetValue(0);var truck=(Component)((Array)Get(course,"trucks")).GetValue(0);var host=(Component)Get(truck,"appearanceHost");Assert.That(host,Is.Not.Null);float deadline=Time.realtimeSinceStartup+40;
            while(!(bool)Get(truck,"Launched")&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That((bool)Get(truck,"Launched"),Is.True,"Actual ordinary AI must launch; no supplied trajectory.");
            float launchY=truck.transform.position.y;Vector3 originalInertia=Inertia(truck);float maxInertiaDelta=0;string physical=Physical(truck),saved=JsonUtility.ToJson(Registry("GetAppearance",id));observer=isolation.AddComponent<CornholeWheelLateDiagnosticObserver>();observer.observe=()=>samples.AddRange(Measure(truck,host,"LateUpdate32000"));float until=Time.realtimeSinceStartup+.8f;
            while(Time.realtimeSinceStartup<until){yield return null;samples.AddRange(Measure(truck,host,"CoroutineUpdateBeforeLateUpdate"));Assert.That(Physical(truck),Is.EqualTo(physical));AssertComputedInertia(truck,originalInertia,ref maxInertiaDelta,"natural diagnostic");}
            observer.observe=null;Assert.That(samples.Count(s=>s.phase=="LateUpdate32000"),Is.GreaterThanOrEqualTo(16),"Actual late observer must sample after Host execution250; no explicit FollowWheels invocation.");Assert.That(samples.All(s=>!s.enabled&&s.sourceUnderPhysicalRoot),Is.True);Assert.That(samples.Max(s=>s.actorPosition.y)-launchY,Is.GreaterThan(.5f));Assert.That(JsonUtility.ToJson(Registry("GetAppearance",id)),Is.EqualTo(saved));
        }
        finally
        {
            if(observer!=null)observer.observe=null;string folder=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/CornNaturalWheelDiagnostic");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"natural-flight-wheel-centers.json"),JsonUtility.ToJson(new Report{samples=samples.ToArray()},true));if(samples.Count>0)TestContext.WriteLine("CORN_NATURAL_WHEEL_MEASURE samples="+samples.Count+" lateSamples="+samples.Count(s=>s.phase=="LateUpdate32000")+" maxPivotToMesh="+samples.Max(s=>s.pivotToMesh)+" maxSourceToPivot="+samples.Max(s=>s.sourceToPivot)+" maxCanonicalToPivot="+samples.Max(s=>s.canonicalToPivot)+" actorScale="+samples[0].actorScale+" modelScale="+samples[0].modelScale+" canonicalScale="+samples[0].canonicalScale+" pivotScale="+samples[0].pivotScale);
            Time.timeScale=1;if(match!=null)Call(match,"ChangeSetup");if(isolation!=null)UnityEngine.Object.DestroyImmediate(isolation);UnityEngine.Random.state=priorRandom;Registry("ForgetCache");prefs.Dispose();
        }
    }
}
#endif
