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
using UnityEngine.Rendering;
using UnityEngine.TestTools;

public sealed class DragonForestMaterialNativeTests
{
    const string Vendor="Assets/TruckDragonKingdom/PrivateLicensedEnvironment/Fantastic Ancient Forest/";
    const string Tree=Vendor+"Prefabs/Trees/P_ENV_tree_ancient_03.prefab";
    const string Proof="Assets/TruckDragonKingdom/VisualCandidates/ForestMaterialProof/";
    readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
    Camera camera;Light key;GameObject subject;string output;Light[] priorLights;bool[] lightStates;bool priorAsync,hasPrior;
    AmbientMode priorAmbientMode;Color priorAmbient;float priorIntensity;
    [Serializable] sealed class LodReceipt {public int lod,vertices,colorVertices,leafSlots,woodSlots,configuredDifference,litDifference,cutoutDifference,windDifference,vertexColorDifference,normalDifference;public string[] meshPaths;public float transitionHeight;}
    [Serializable] sealed class Receipt {public string scope="Controlled original real tree LOD0/1/2 Camera.Render with synchronous shader compilation; test-only ForceLOD and counterfactual mesh/material copies, no production LOD/material/mesh edits or human/world acceptance.";public LodReceipt[] lods;public string[] sourcePaths,sourceHashes;public bool originalsExact,asyncCompilationDisabled;}
    static Material Mat(string path)=>AssetDatabase.LoadAssetAtPath<Material>(path)??throw new InvalidOperationException("Prepare actual forest proof materials first: "+path);
    static string Hash(string path){using(var sha=SHA256.Create())using(var input=File.OpenRead(path))return string.Concat(sha.ComputeHash(input).Select(value=>value.ToString("x2")));}
    static void Match(Material source,Material target)
    {
        Assert.That(target.shader,Is.SameAs(source.shader));Assert.That(target.shader.isSupported,Is.True);Assert.That(target.renderQueue,Is.EqualTo(source.renderQueue));CollectionAssert.AreEquivalent(source.shaderKeywords,target.shaderKeywords);
        using(var a=new SerializedObject(source))using(var b=new SerializedObject(target))
        {
            var x=a.GetIterator();var y=b.GetIterator();bool enter=true;while(true){bool l=x.Next(enter),r=y.Next(enter);enter=true;Assert.That(r,Is.EqualTo(l));if(!l)break;Assert.That(y.propertyPath,Is.EqualTo(x.propertyPath));Assert.That(y.propertyType,Is.EqualTo(x.propertyType));if(x.propertyPath=="m_Name"){enter=false;continue;}if(x.propertyType!=SerializedPropertyType.Generic)Assert.That(SerializedProperty.DataEquals(x,y),Is.True,x.propertyPath);}
        }
    }
    Texture2D Render(string name)
    {
        var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);var prior=RenderTexture.active;var texture=new Texture2D(1280,720,TextureFormat.RGBA32,false);owned.Add(texture);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();if(name!=null)File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());return texture;}
        finally{camera.targetTexture=null;RenderTexture.active=prior;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
    }
    static int Difference(Texture2D a,Texture2D b,int tolerance=2)
    {var x=a.GetPixels32();var y=b.GetPixels32();int count=0;for(int i=0;i<x.Length;i++)if(Mathf.Abs(x[i].r-y[i].r)>tolerance||Mathf.Abs(x[i].g-y[i].g)>tolerance||Mathf.Abs(x[i].b-y[i].b)>tolerance)count++;return count;}
    [UnityTest,Timeout(180000)]
    public IEnumerator ActualThreeTreeLodsRetainOriginalWindCutoutVertexColorsAndPbrSurface()
    {
        priorAsync=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;hasPrior=true;
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");Assert.That(at>=0&&at+1<args.Length,Is.True);output=Path.Combine(args[at+1],"ForestMaterialProof",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"));Directory.CreateDirectory(output);
        var originals=AssetDatabase.GetDependencies(Tree,true).Where(path=>path.StartsWith(Vendor,StringComparison.Ordinal)&&File.Exists(path)).SelectMany(path=>new[]{path,path+".meta"}).Where(File.Exists).Distinct().ToDictionary(path=>path,Hash);
        var sourceLeaf=Mat(Vendor+"Materials/M_ENV_tree_leaves_ancient.mat");var sourceWood=Mat(Vendor+"Materials/M_ENV_wood_ancient_01.mat");var leaf=Mat(Proof+"Configured vendor leaf.mat");var wood=Mat(Proof+"Configured vendor wood.mat");var oldLeaf=Mat(Proof+"Observed original Lit leaf.mat");var oldWood=Mat(Proof+"Observed original Lit wood.mat");Match(sourceLeaf,leaf);Match(sourceWood,wood);
        Assert.That(leaf.GetFloat("_WindStrength"),Is.GreaterThan(0));Assert.That(leaf.GetFloat("_AlphaClipThreshold"),Is.GreaterThan(0));foreach(string property in new[]{"_BaseTexture","_NormalTexture","_PBRTexture"})Assert.That(wood.GetTexture(property),Is.Not.Null,property);
        priorLights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None);lightStates=priorLights.Select(light=>light.enabled).ToArray();foreach(var light in priorLights)light.enabled=false;
        priorAmbientMode=RenderSettings.ambientMode;priorAmbient=RenderSettings.ambientLight;priorIntensity=RenderSettings.ambientIntensity;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.24f,.24f,.24f);RenderSettings.ambientIntensity=1;
        var cameraObject=new GameObject("Controlled actual three LOD forest camera");owned.Add(cameraObject);camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.11f,.16f);camera.fieldOfView=32;camera.aspect=16f/9;camera.nearClipPlane=.02f;camera.farClipPlane=500;
        var lightObject=new GameObject("Controlled forest normal key");owned.Add(lightObject);key=lightObject.AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.5f;key.shadows=LightShadows.None;key.cullingMask=1<<30;key.transform.rotation=Quaternion.LookRotation(new Vector3(-1,-.8f,.6f));
        subject=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Tree));owned.Add(subject);foreach(var t in subject.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;foreach(var renderer in subject.GetComponentsInChildren<Renderer>(true))renderer.shadowCastingMode=ShadowCastingMode.Off;
        var group=subject.GetComponent<LODGroup>();Assert.That(group,Is.Not.Null);var lods=group.GetLODs();Assert.That(lods.Length,Is.EqualTo(3));group.animateCrossFading=false;group.fadeMode=LODFadeMode.None;var receipts=new List<LodReceipt>();
        var originalSlots=subject.GetComponentsInChildren<Renderer>(true).ToDictionary(renderer=>renderer,renderer=>renderer.sharedMaterials);
        Action<Material,Material> assign=(l,w)=>{foreach(var pair in originalSlots)pair.Key.sharedMaterials=pair.Value.Select(material=>material==sourceLeaf?l:material==sourceWood?w:throw new InvalidOperationException("Unexpected source material on actual tree: "+material)).ToArray();};
        for(int level=0;level<3;level++)
        {
            group.ForceLOD(level);var active=lods[level].renderers;Assert.That(active,Is.Not.Empty);var bounds=active[0].bounds;foreach(var renderer in active.Skip(1))bounds.Encapsulate(renderer.bounds);float radius=bounds.extents.magnitude;Vector3 direction=new Vector3(1,.3f,-1).normalized;camera.transform.SetPositionAndRotation(bounds.center+direction*radius*3.2f,Quaternion.LookRotation(-direction));
            var meshes=active.Select(renderer=>renderer.GetComponent<MeshFilter>()).Where(filter=>filter!=null&&filter.sharedMesh!=null).ToArray();var fact=new LodReceipt{lod=level,transitionHeight=lods[level].screenRelativeTransitionHeight,vertices=meshes.Sum(filter=>filter.sharedMesh.vertexCount),colorVertices=meshes.Where(filter=>filter.sharedMesh.HasVertexAttribute(VertexAttribute.Color)).Sum(filter=>filter.sharedMesh.vertexCount),leafSlots=active.Sum(renderer=>originalSlots[renderer].Count(material=>material==sourceLeaf)),woodSlots=active.Sum(renderer=>originalSlots[renderer].Count(material=>material==sourceWood)),meshPaths=meshes.Select(filter=>AssetDatabase.GetAssetPath(filter.sharedMesh)+"#"+filter.sharedMesh.name).ToArray()};
            Assert.That(fact.leafSlots,Is.GreaterThan(0));Assert.That(fact.woodSlots,Is.GreaterThan(0));Assert.That(fact.colorVertices,Is.GreaterThan(0));
            // Warm all involved shader variants synchronously before retaining
            // the same-frame A/B, so async/debug-blue is never wind evidence.
            assign(oldLeaf,oldWood);Render(null);assign(leaf,wood);Render(null);assign(sourceLeaf,sourceWood);Render(null);
            var original=Render("lod"+level+"-original-vendor");assign(leaf,wood);var configured=Render("lod"+level+"-configured-vendor");assign(oldLeaf,oldWood);var flat=Render("lod"+level+"-observed-Lit");fact.configuredDifference=Difference(original,configured);fact.litDifference=Difference(original,flat,3);Assert.That(fact.configuredDifference,Is.LessThan(100));Assert.That(fact.litDifference,Is.GreaterThan(300));
            var opaque=new Material(leaf);owned.Add(opaque);opaque.SetFloat("_AlphaClipThreshold",0);assign(opaque,wood);Render(null);var noCutout=Render("lod"+level+"-diagnostic-cutout-disabled");fact.cutoutDifference=Difference(configured,noCutout);Assert.That(fact.cutoutDifference,Is.GreaterThan(300));
            var smooth=new Material(wood);owned.Add(smooth);smooth.SetFloat("_NormalStrength",0);assign(leaf,smooth);Render(null);var noNormal=Render("lod"+level+"-diagnostic-normal-disabled");fact.normalDifference=Difference(configured,noNormal);Assert.That(fact.normalDifference,Is.GreaterThan(20));
            assign(leaf,wood);var changedMeshes=new Dictionary<MeshFilter,Mesh>();foreach(var filter in meshes){changedMeshes.Add(filter,filter.sharedMesh);var copy=UnityEngine.Object.Instantiate(filter.sharedMesh);owned.Add(copy);copy.colors32=new Color32[copy.vertexCount];filter.sharedMesh=copy;}
            var noColors=Render("lod"+level+"-diagnostic-vertex-color-zero");fact.vertexColorDifference=Difference(configured,noColors);foreach(var pair in changedMeshes)pair.Key.sharedMesh=pair.Value;Assert.That(fact.vertexColorDifference,Is.GreaterThan(20));
            assign(leaf,wood);var windStart=Render("lod"+level+"-real-wind-start");yield return new WaitForSeconds(.75f);var windEnd=Render("lod"+level+"-real-wind-later");fact.windDifference=Difference(windStart,windEnd);Assert.That(fact.windDifference,Is.GreaterThan(20));receipts.Add(fact);
            File.WriteAllText(Path.Combine(output,"observations.json"),JsonUtility.ToJson(new Receipt{lods=receipts.ToArray(),sourcePaths=originals.Keys.ToArray(),sourceHashes=originals.Values.ToArray(),originalsExact=originals.All(pair=>Hash(pair.Key)==pair.Value),asyncCompilationDisabled=!ShaderUtil.allowAsyncCompilation},true));
        }
        foreach(var pair in originals)Assert.That(Hash(pair.Key),Is.EqualTo(pair.Value));group.ForceLOD(-1);TestContext.WriteLine("ACTUAL_FOREST_THREE_LOD_PROOF "+output);
    }
    [TearDown]public void Cleanup()
    {
        for(int i=owned.Count-1;i>=0;i--)if(owned[i]!=null)UnityEngine.Object.DestroyImmediate(owned[i]);owned.Clear();if(priorLights!=null){for(int i=0;i<priorLights.Length;i++)if(priorLights[i]!=null)priorLights[i].enabled=lightStates[i];RenderSettings.ambientMode=priorAmbientMode;RenderSettings.ambientLight=priorAmbient;RenderSettings.ambientIntensity=priorIntensity;priorLights=null;}if(hasPrior){ShaderUtil.allowAsyncCompilation=priorAsync;hasPrior=false;}
    }
}
#endif
