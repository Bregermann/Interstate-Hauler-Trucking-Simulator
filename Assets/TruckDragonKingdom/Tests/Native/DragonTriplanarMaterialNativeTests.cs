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

public sealed class DragonTriplanarMaterialNativeTests
{
    const string Vendor="Assets/TruckDragonKingdom/PrivateLicensedEnvironment/Fantastic Ancient Forest/";
    const string Proof="Assets/TruckDragonKingdom/VisualCandidates/MaterialFamilyProof/";
    readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
    Camera camera;Light key;string output;AmbientMode priorMode;Color priorAmbient;float priorIntensity;bool priorAsyncCompilation,changedCompilation;
    Light[] oldLights;bool[] enabledLights;
    static Material MaterialAt(string path)=>AssetDatabase.LoadAssetAtPath<Material>(path)??throw new InvalidOperationException("Explicitly prepare actual proof material first: "+path);
    static string Hash(string path){using(var sha=SHA256.Create())using(var file=File.OpenRead(path))return string.Concat(sha.ComputeHash(file).Select(value=>value.ToString("x2")));}
    static Type Migration()=>AppDomain.CurrentDomain.GetAssemblies().Select(assembly=>assembly.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomWorldVisualMigration",false)).First(type=>type!=null);
    static void MatchProperties(Material source,Material target)
    {
        Assert.That(target.shader,Is.SameAs(source.shader));Assert.That(target.shader.isSupported,Is.True);Assert.That((bool)Migration().GetMethod("HasSupportedUrpShader").Invoke(null,new object[]{target}),Is.True);
        CollectionAssert.AreEquivalent(source.shaderKeywords,target.shaderKeywords);Assert.That(target.renderQueue,Is.EqualTo(source.renderQueue));
        for(int index=0;index<source.shader.GetPropertyCount();index++)
        {
            string property=source.shader.GetPropertyName(index);
            switch(source.shader.GetPropertyType(index))
            {
                case ShaderPropertyType.Texture:
                    Assert.That(target.GetTexture(property),Is.SameAs(source.GetTexture(property)),property);
                    Assert.That(target.GetTextureScale(property),Is.EqualTo(source.GetTextureScale(property)),property+" tiling");Assert.That(target.GetTextureOffset(property),Is.EqualTo(source.GetTextureOffset(property)),property+" offset");break;
                case ShaderPropertyType.Color:Assert.That(target.GetColor(property),Is.EqualTo(source.GetColor(property)),property);break;
                case ShaderPropertyType.Vector:Assert.That(target.GetVector(property),Is.EqualTo(source.GetVector(property)),property);break;
                default:Assert.That(target.GetFloat(property),Is.EqualTo(source.GetFloat(property)),property);break;
            }
        }
    }
    GameObject Instance(string path)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);Assert.That(prefab,Is.Not.Null);var subject=UnityEngine.Object.Instantiate(prefab);owned.Add(subject);
        foreach(var transform in subject.GetComponentsInChildren<Transform>(true))transform.gameObject.layer=30;
        foreach(var lod in subject.GetComponentsInChildren<LODGroup>(true))lod.ForceLOD(0);
        foreach(var renderer in subject.GetComponentsInChildren<Renderer>(true))renderer.shadowCastingMode=ShadowCastingMode.Off;
        return subject;
    }
    void Frame(GameObject subject)
    {
        var renderers=subject.GetComponentsInChildren<Renderer>(true);var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
        float radius=Mathf.Max(.3f,bounds.extents.magnitude);Vector3 direction=new Vector3(1,.35f,-1).normalized;
        camera.transform.SetPositionAndRotation(bounds.center+direction*radius*3.2f,Quaternion.LookRotation(-direction));camera.nearClipPlane=.02f;camera.farClipPlane=radius*15+100;
        key.transform.rotation=Quaternion.LookRotation(new Vector3(-1,-.8f,.6f));
    }
    Texture2D Render(string name)
    {
        var target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);var texture=new Texture2D(1280,720,TextureFormat.RGBA32,false);owned.Add(texture);var previous=RenderTexture.active;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());return texture;}
        finally{camera.targetTexture=null;RenderTexture.active=previous;target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
    static int Different(Texture2D a,Texture2D b,int tolerance)
    {
        var first=a.GetPixels32();var second=b.GetPixels32();int changed=0;
        for(int index=0;index<first.Length;index++)if(Mathf.Abs(first[index].r-second[index].r)>tolerance||Mathf.Abs(first[index].g-second[index].g)>tolerance||Mathf.Abs(first[index].b-second[index].b)>tolerance)changed++;
        return changed;
    }
    void Assign(GameObject subject,Material material)
    {foreach(var renderer in subject.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterials=renderer.sharedMaterials.Select(old=>material).ToArray();}
    [UnityTest,Timeout(90000)]
    public IEnumerator ActualColumnRetainsVendorTriplanarDetailAndNormalsWithSameRigRenderAndOriginalBytes()
    {
        // A first-frame blue asynchronous-compilation placeholder must never
        // masquerade as time-driven foliage motion in the pixel comparison.
        priorAsyncCompilation=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;changedCompilation=true;
        string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");output=Path.GetFullPath(Path.Combine(at>=0&&at+1<args.Length?args[at+1]:Application.persistentDataPath,"MaterialFamilyProof",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")));Directory.CreateDirectory(output);
        var source=MaterialAt(Vendor+"Materials/M_PROP_column_ancient_02.mat");var repaired=MaterialAt(Proof+"Configured original triplanar.mat");var broken=MaterialAt(Proof+"Observed pre-repair column.mat");
        var paths=AssetDatabase.GetDependencies(Vendor+"Prefabs/Props/P_PROP_column_ancient_02.prefab",true).Concat(AssetDatabase.GetDependencies(Vendor+"Materials/M_ENV_tree_leaves_ancient.mat",true)).Where(File.Exists).SelectMany(path=>new[]{path,path+".meta"}).Where(File.Exists).Distinct().ToDictionary(path=>path,Hash);
        priorMode=RenderSettings.ambientMode;priorAmbient=RenderSettings.ambientLight;priorIntensity=RenderSettings.ambientIntensity;
        oldLights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None);enabledLights=oldLights.Select(light=>light.enabled).ToArray();foreach(var light in oldLights)light.enabled=false;
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.24f,.24f,.24f);RenderSettings.ambientIntensity=1;
        var cameraOwner=new GameObject("Controlled real column camera");owned.Add(cameraOwner);camera=cameraOwner.AddComponent<Camera>();camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.11f,.16f);camera.cullingMask=1<<30;camera.fieldOfView=32;camera.aspect=16f/9;
        var lightOwner=new GameObject("Controlled actual normal-map key");owned.Add(lightOwner);key=lightOwner.AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.35f;key.shadows=LightShadows.None;key.cullingMask=1<<30;
        Assert.That((bool)Migration().GetMethod("HasAuthoredAlbedo").Invoke(null,new object[]{source}),Is.True,"Triplanar albedo channels must not be misclassified as intentional flat white.");MatchProperties(source,repaired);
        foreach(string property in new[]{"_DetailTexture","_TopTexture","_AssetSpecificNormal","_DetailNormal","_DetailPBR","_TopPBR"})Assert.That(repaired.GetTexture(property),Is.Not.Null,property);
        Assert.That(broken.shader,Is.Not.SameAs(source.shader));Assert.That(broken.GetTexture("_BaseMap"),Is.Null);
        var column=Instance(Vendor+"Prefabs/Props/P_PROP_column_ancient_02.prefab");Frame(column);
        foreach(var filter in column.GetComponentsInChildren<MeshFilter>(true))Assert.That(filter.sharedMesh.HasVertexAttribute(VertexAttribute.Color),Is.False,"Actual source columns do not use a lost vertex-color stream.");
        Assign(column,source);var originalImage=Render("column-original-vendor");Assign(column,broken);var flatImage=Render("column-observed-flat-variant");Assign(column,repaired);var fixedImage=Render("column-configured-vendor");
        int matchDifference=Different(originalImage,fixedImage,2),brokenDifference=Different(originalImage,flatImage,3);
        Assert.That(matchDifference,Is.LessThan(100),"Same real mesh/camera/light must reproduce the original vendor surface.");Assert.That(brokenDifference,Is.GreaterThan(300),"Actual original detailed surface must visibly differ from the observed lost-texture material.");
        // Separately retain existing installed foliage's actual alpha texture,
        // cutoff and live wind properties; do not replace its vendor shader.
        column.SetActive(false);var leafSource=MaterialAt(Vendor+"Materials/M_ENV_tree_leaves_ancient.mat");var leaf=MaterialAt(Proof+"Configured existing foliage.mat");Assert.That(leaf.shader,Is.SameAs(leafSource.shader));
        foreach(string property in new[]{"_WindStrength","_WindScale"})Assert.That(leaf.GetFloat(property),Is.EqualTo(leafSource.GetFloat(property)),property);
        Assert.That(leaf.GetVector("_WindSpeed"),Is.EqualTo(leafSource.GetVector("_WindSpeed")));Assert.That(leaf.GetFloat("_WindStrength"),Is.GreaterThan(0));Assert.That(leaf.GetTexture("_BaseTexture"),Is.SameAs(leafSource.GetTexture("_BaseTexture")));Assert.That(leaf.GetFloat("_AlphaClipThreshold"),Is.EqualTo(leafSource.GetFloat("_AlphaClipThreshold")));Assert.That(leaf.IsKeywordEnabled("_ALPHATEST_ON"),Is.EqualTo(leafSource.IsKeywordEnabled("_ALPHATEST_ON")));
        var tree=Instance(Vendor+"Prefabs/Trees/P_ENV_tree_ancient_03.prefab");
        int leafSlots=0;foreach(var renderer in tree.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterials=renderer.sharedMaterials.Select(material=>{if(material!=leafSource)return material;leafSlots++;return leaf;}).ToArray();Assert.That(leafSlots,Is.GreaterThan(0));Frame(tree);
        var windStart=Render("actual-tree-cutout-wind-start");
        // Read-only diagnostic counterfactual on an unsaved copy. Same actual
        // alpha texture/mesh/camera/frame; source/product cutout stays intact.
        var noCutout=new Material(leaf);owned.Add(noCutout);noCutout.SetFloat("_AlphaClipThreshold",0);if(noCutout.HasProperty("_AlphaClip"))noCutout.SetFloat("_AlphaClip",0);noCutout.DisableKeyword("_ALPHATEST_ON");
        foreach(var renderer in tree.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterials=renderer.sharedMaterials.Select(material=>material==leaf?noCutout:material).ToArray();
        var opaqueCards=Render("diagnostic-same-tree-cutout-disabled");int cutoutDifference=Different(windStart,opaqueCards,2);Assert.That(cutoutDifference,Is.GreaterThan(300),"Real vendor alpha clipping must alter rendered leaf-card coverage; a keyword alone is insufficient.");
        foreach(var renderer in tree.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterials=renderer.sharedMaterials.Select(material=>material==noCutout?leaf:material).ToArray();
        yield return new WaitForSeconds(.75f);var windEnd=Render("actual-tree-cutout-wind-later");int windDifference=Different(windStart,windEnd,2);Assert.That(windDifference,Is.GreaterThan(20),"Actual time-driven vendor wind must affect the real rendered tree; a property name alone is insufficient.");
        foreach(var pair in paths)Assert.That(Hash(pair.Key),Is.EqualTo(pair.Value),"Preserve original source bytes: "+pair.Key);
        string receipt="Original-vs-configured changedPixels>2="+matchDifference+"; original-vs-observed-flat changedPixels>3="+brokenDifference+"; same-frame actual leaf cutout changedPixels>2="+cutoutDifference+"; actual tree wind changedPixels>2="+windDifference+"; column shader="+source.shader.name+"; detailNormal="+AssetDatabase.GetAssetPath(source.GetTexture("_AssetSpecificNormal"))+"; windStrength="+leaf.GetFloat("_WindStrength")+" scale="+leaf.GetFloat("_WindScale")+" speed="+leaf.GetVector("_WindSpeed")+"; alphaCutoff="+leaf.GetFloat("_AlphaClipThreshold")+"; originalFilesPreserved="+paths.Count+". Actual controlled native rendering; no production scene/Windows/human acceptance.";
        File.WriteAllText(Path.Combine(output,"observations.txt"),receipt+"\n");TestContext.WriteLine(receipt);
    }
    [TearDown]public void Cleanup()
    {
        for(int index=owned.Count-1;index>=0;index--)if(owned[index]!=null)UnityEngine.Object.DestroyImmediate(owned[index]);owned.Clear();
        if(oldLights!=null){for(int index=0;index<oldLights.Length;index++)if(oldLights[index]!=null)oldLights[index].enabled=enabledLights[index];RenderSettings.ambientMode=priorMode;RenderSettings.ambientLight=priorAmbient;RenderSettings.ambientIntensity=priorIntensity;oldLights=null;}
        if(changedCompilation){ShaderUtil.allowAsyncCompilation=priorAsyncCompilation;changedCompilation=false;}
    }
}
#endif
