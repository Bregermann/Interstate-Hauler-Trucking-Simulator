#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

// Diagnostic sampling only. No sampled pose supplies an action, contact,
// pickup, movement, score, or gameplay-acceptance assertion.
public sealed class DragonContactGeometryNativeProbe
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    GameObject live,floor,samplingParent,sampled;
    [Serializable]sealed class PoseSample
    {
        public string action,clip;
        public float normalized,clipContactNormalized,palmHeightAboveSupport,mouthHeightAboveSupport;
        public Vector3 motorOrigin,palm,mouth,palmMotorLocal,mouthMotorLocal,actualModelWorldScale;
    }
    [Serializable]sealed class Receipt
    {
        public string scope="NO_GAMEPLAY_CREDIT: fresh native vendor-grounded root measurement, then Editor clip sampling on an inactive unawakened copy retaining original Generic binding paths. No saved asset writes.";
        public string species="dragon-inferno",prefab;
        public Vector3 actualGroundedRoot,actualSupport;
        public float actualVendorHipHeight;
        public bool actualGrounded,selectedSourceBytesUnchanged;
        public List<PoseSample> samples=new List<PoseSample>();
    }
    static object Read(object value,string name)=>value.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(value):value.GetType().GetProperty(name,Any).GetValue(value);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name,Any).Invoke(value,args);
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static string Hash(string path)
    {
        string absolute=Path.GetFullPath(path);
        if(Environment.OSVersion.Platform==PlatformID.Win32NT&&!absolute.StartsWith(@"\\?\",StringComparison.Ordinal))absolute=@"\\?\"+absolute;
        using(var hash=SHA256.Create())return string.Concat(hash.ComputeHash(File.ReadAllBytes(absolute)).Select(value=>value.ToString("x2")));
    }
    [UnityTest,Timeout(60000)]public IEnumerator ActualGroundedRootAndSampledSheepActionContactBonesPreserveSourceAssets()
    {
        Assert.That(Application.isPlaying,Is.True);
        const string prefabPath="Assets/TruckDragonKingdom/Prefabs/Dragons/dragon-inferno/dragon-inferno Dragon Actor.prefab";
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);Assert.That(prefab,Is.Not.Null);
        var catalog=AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset");Assert.That(catalog,Is.Not.Null);
        var lookup=new object[]{"dragon-inferno",null};Assert.That(Call(catalog,"TryGet",lookup),Is.EqualTo(true));var species=lookup[1];
        var mappings=((IEnumerable)Read(species,"animations")).Cast<object>().Where(value=>new[]{"GroundIdle","GroundGrab","Grab","Carry","Eat"}.Contains(Read(value,"action").ToString())).ToArray();Assert.That(mappings.Length,Is.InRange(4,5));
        var watched=AssetDatabase.GetDependencies(prefabPath,true).Concat(mappings.Select(value=>AssetDatabase.GetAssetPath((AnimationClip)Read(value,"clip")))).Where(File.Exists).Distinct().SelectMany(path=>File.Exists(path+".meta")?new[]{path,path+".meta"}:new[]{path}).ToDictionary(path=>path,Hash);
        floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Contact geometry probe actual support";floor.transform.position=new Vector3(0,-1,0);floor.transform.localScale=new Vector3(100,2,100);
        live=UnityEngine.Object.Instantiate(prefab,Vector3.up,Quaternion.identity);
        var actor=live.GetComponent(Runtime("LWS.TruckDragonKingdom.DragonCharacter"));var motor=(Component)Read(actor,"motor");var body=(Rigidbody)Read(motor,"body");
        Call(actor,"Prepare",species,Array.CreateInstance(Runtime("LWS.TruckSports.SportsEventTruck"),0),0,927);
        for(int step=0;step<30;step++){Call(motor,"StopMotion");yield return new WaitForFixedUpdate();}yield return null;
        Assert.That(Read(motor,"IsGrounded"),Is.EqualTo(true));
        var support=Physics.RaycastAll(body.position+Vector3.up*2,Vector3.down,10,~0,QueryTriggerInteraction.Ignore).Where(hit=>!hit.transform.IsChildOf(live.transform)&&hit.normal.y>.7f).OrderBy(hit=>hit.distance).First();
        Assert.That(support.collider.gameObject,Is.SameAs(floor));
        Vector3 actualRoot=body.position;Quaternion actualRotation=body.rotation;
        var receipt=new Receipt{prefab=prefabPath,actualGroundedRoot=actualRoot,actualSupport=support.point,actualGrounded=true,actualVendorHipHeight=(float)Read(Read(motor,"animal"),"Height")};
        Call(actor,"Pause",true);
        // MAnimal reparents Rig beneath Rotator in Awake. Sample an inactive
        // copy that has never awakened, so the actual authored Generic paths
        // are retained, exactly as in the existing editor calibration tools.
        samplingParent=new GameObject("Inactive source-path-preserving diagnostic copy");samplingParent.SetActive(false);
        sampled=UnityEngine.Object.Instantiate(prefab,samplingParent.transform);sampled.transform.SetPositionAndRotation(actualRoot,actualRotation);
        var sampledActor=sampled.GetComponent(Runtime("LWS.TruckDragonKingdom.DragonCharacter"));var sampledMotor=(Component)Read(sampledActor,"motor");
        var palm=(Transform)Read(sampledActor,"leftGrip");var mouth=(Transform)Read(sampledActor,"mouth");var sampledAnimator=(Animator)Read(sampledMotor,"animator");sampledAnimator.enabled=false;
        Assert.That(((Transform)Read(Read(sampledMotor,"animal"),"RootBone")).parent,Is.SameAs(sampledMotor.transform),"The unawakened copy must retain actual original Generic binding paths, before runtime Rotator reparenting.");
        string palmPath=AnimationUtility.CalculateTransformPath(palm,sampledMotor.transform);
        var bindings=((IEnumerable)Read(sampledMotor,"actionBindings")).Cast<object>().ToArray();
        foreach(var mapping in mappings)
        {
            string action=Read(mapping,"action").ToString();var clip=(AnimationClip)Read(mapping,"clip");Assert.That(clip,Is.Not.Null);
            Assert.That(AnimationUtility.GetCurveBindings(clip).Any(value=>value.path==palmPath),Is.True,"Actual selected clip must bind the actual palm path: "+action+"/"+palmPath);
            var binding=bindings.FirstOrDefault(value=>Read(value,"action").ToString()==action);float contact=binding==null?0:(float)Read(binding,"contactNormalizedTime");
            foreach(float normalized in new[]{0f,.25f,contact,.75f,.98f}.Distinct())
            {
                clip.SampleAnimation(sampledMotor.gameObject,clip.length*normalized);
                var value=new PoseSample{action=action,clip=AssetDatabase.GetAssetPath(clip),normalized=normalized,clipContactNormalized=contact,motorOrigin=sampledMotor.transform.position,palm=palm.position,mouth=mouth.position,palmMotorLocal=sampledMotor.transform.InverseTransformPoint(palm.position),mouthMotorLocal=sampledMotor.transform.InverseTransformPoint(mouth.position),actualModelWorldScale=sampledMotor.transform.lossyScale,palmHeightAboveSupport=palm.position.y-support.point.y,mouthHeightAboveSupport=mouth.position.y-support.point.y};
                Assert.That(value.actualModelWorldScale.y,Is.EqualTo(5).Within(.0001f));Assert.That(float.IsFinite(value.palmHeightAboveSupport)&&float.IsFinite(value.mouthHeightAboveSupport),Is.True);
                Assert.That(Vector3.Distance(value.motorOrigin,actualRoot),Is.LessThan(.0001f),"Sampled pose must retain the freshly measured genuine grounded motor origin, without inventing a lower root height.");
                receipt.samples.Add(value);TestContext.WriteLine("SAMPLED_BONE_GEOMETRY_NOT_PLAYBACK action="+action+" normalized="+normalized+" genuineRootH="+actualRoot.y+" palmHeight="+value.palmHeightAboveSupport+" mouthHeight="+value.mouthHeightAboveSupport+" palm="+value.palm.ToString("F5")+" localPalm="+value.palmMotorLocal.ToString("F5"));
            }
        }
        foreach(var pair in watched)Assert.That(Hash(pair.Key),Is.EqualTo(pair.Value),"Selected actual source/configuration bytes changed: "+pair.Key);
        receipt.selectedSourceBytesUnchanged=true;Assert.That(body.position,Is.EqualTo(actualRoot));Assert.That(body.rotation,Is.EqualTo(actualRotation));
        string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");Assert.That(at>=0&&at+1<args.Length,Is.True);
        string output=Path.Combine(args[at+1],"SheepContactGeometry");Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"report.json"),JsonUtility.ToJson(receipt,true));
    }
    [UnityTearDown]public IEnumerator Cleanup()
    {
        foreach(var value in new[]{samplingParent,live,floor})if(value!=null)UnityEngine.Object.Destroy(value);
        sampled=samplingParent=live=floor=null;yield return null;
    }
}
#endif
