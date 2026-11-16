#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DragonRiderGateEmissionImportTests
{
    const string Gold="Assets/TruckDragonRiders/Materials/Royal skyfire gate gold.mat";
    const string Author="Assets/TruckDragonRiders/Editor/DragonRidersAuthoring.cs";
    [Serializable]sealed class FileProof{public string path,sha256;}
    [Serializable]sealed class Variant
    {
        public string path,rawBeforeImportSha256,initialImportedSha256,propertiesBefore,propertiesAfterFirst,propertiesAfterSecond,firstImportedSha256,secondImportedSha256;
        public string[] keywordsBefore,keywordsAfterFirst,keywordsAfterSecond;public bool otherKeywordsExact;public int requestedGi,giBefore,giAfterFirst,giAfterSecond;public bool keywordBefore,keywordAfterFirst,keywordAfterSecond,firstRepeatStateExact,propertiesPreserved,twoNativeImportHashesExact;
    }
    [Serializable]sealed class Proof
    {
        public string scope="DISPOSABLE_NATIVE_URP_EMISSION_FLAGS_IMPORT_CONTROL_NOT_CANONICAL_MATERIAL_CORRECTION",status="IN_PROGRESS",failure,projectRoot,folder,setterAssembly,setterSignature;
        public bool originalSourceBytesExact,sceneContextExact;public Variant[] variants;public FileProof[] originalSources,postEditorExpectedFiles;
    }
    static string Physical(string path)=>Path.GetFullPath(Path.IsPathRooted(path)?path:Path.Combine(Application.dataPath,"..",path));
    static string Io(string path)
    {var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomMountedCourseReview, Assembly-CSharp-Editor",true);return(string)type.GetMethod("Io",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{Physical(path)});}
    static string Hash(string path){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(Io(path)))).Replace("-","").ToLowerInvariant();}
    static string Arg(string flag){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,flag);if(i<0||i+1>=a.Length)throw new InvalidOperationException("Explicit native diagnostic argument required: "+flag);return a[i+1];}
    static string Context()=>string.Join("|",Enumerable.Range(0,SceneManager.sceneCount).Select(i=>{var s=SceneManager.GetSceneAt(i);return s.handle+":"+s.path+":"+s.isLoaded+":"+s.isDirty;}))+"/active="+SceneManager.GetActiveScene().handle;
    static string Properties(Material m)
    {
        var values=new List<string>{"shader="+AssetDatabase.GetAssetPath(m.shader),"queue="+m.renderQueue,"instancing="+m.enableInstancing,"doubleGI="+m.doubleSidedGI};for(int i=0;i<m.passCount;i++){string pass=m.GetPassName(i);values.Add("pass/"+pass+"="+m.GetShaderPassEnabled(pass));}
        for(int i=0;i<ShaderUtil.GetPropertyCount(m.shader);i++){string p=ShaderUtil.GetPropertyName(m.shader,i);string type=ShaderUtil.GetPropertyType(m.shader,i).ToString();string v=type=="Color"?m.GetColor(p).ToString("R"):type=="Vector"?m.GetVector(p).ToString("R"):type=="TexEnv"?AssetDatabase.GetAssetPath(m.GetTexture(p))+"/"+m.GetTextureScale(p).ToString("R")+"/"+m.GetTextureOffset(p).ToString("R"):m.GetFloat(p).ToString("R",System.Globalization.CultureInfo.InvariantCulture);values.Add(p+"/"+type+"="+v);}return string.Join("\n",values);
    }
    [Test,Timeout(300000)]
    public void ActualDisposableUrpSetterAndTwoImportsSeparateInconsistentEmissionFromBakedFlagAndPreserveOriginalSources()
    {
        Assert.That(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling,Is.False);string tag=Arg("-tsRiderEmissionImportProbe");Assert.That(tag.Length>0&&tag.Length<=16&&tag.All(char.IsLetterOrDigit),Is.True);string folder="Assets/TruckDragonRiders/ReviewCandidates/EmissionImport-"+tag;Assert.That(Directory.Exists(Io(folder))||File.Exists(Io(folder+".meta")),Is.False,"Fresh owned diagnostic folder required; preserve earlier evidence");
        string output=Arg("-tsDragonOutput"),report=Path.Combine(output,"rider-gate-emission-import-control.json"),context=Context();Directory.CreateDirectory(output);var active=SceneManager.GetActiveScene();var proof=new Proof{projectRoot=Path.GetFullPath(Path.Combine(Application.dataPath,"..")),folder=folder};var originalPaths=new[]{Gold,Gold+".meta",Author,Author+".meta"};var watched=originalPaths.ToDictionary(p=>p,Hash);proof.originalSources=watched.Select(p=>new FileProof{path=Physical(p.Key),sha256=p.Value}).ToArray();var variants=new List<Variant>();var post=new List<FileProof>();
        try
        {
            Assert.That(watched[Gold],Is.EqualTo("40e932020fb7d5b31991d4831e96224b969f355ddb72550195f17a012822b331"),"This control pins the original inconsistent source; never normalize its old producer guard");string raw=File.ReadAllText(Io(Gold));Assert.That(raw,Does.Contain("  m_LightmapFlags: 4\n").Or.Contain("  m_LightmapFlags: 4\r\n"));Assert.That(raw,Does.Contain("  - _EMISSION"));
            var setterType=Type.GetType("UnityEditor.BaseShaderGUI, Unity.RenderPipelines.Universal.Editor",true);var setter=setterType.GetMethod("SetMaterialKeywords",BindingFlags.Public|BindingFlags.Static);Assert.That(setter,Is.Not.Null);proof.setterAssembly=setterType.Assembly.FullName;proof.setterSignature=setter.ToString();Assert.That(setter.GetParameters().Length,Is.EqualTo(3));
            Directory.CreateDirectory(Io(folder));foreach(int flags in new[]{4,2}){string path=folder+"/"+(flags==4?"Flags4":"Baked2")+".mat";string payload=flags==4?raw:raw.Replace("  m_LightmapFlags: 4","  m_LightmapFlags: 2");File.WriteAllText(Io(path),payload);variants.Add(new Variant{path=path,requestedGi=flags,rawBeforeImportSha256=Hash(path)});}AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var row in variants)
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>(row.path);Assert.That(material,Is.Not.Null);Assert.That(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(material.shader)),Is.EqualTo("933532a4fcc9baf4fa0491de14d08ed7"));row.initialImportedSha256=Hash(row.path);material.globalIlluminationFlags=(MaterialGlobalIlluminationFlags)row.requestedGi;material.EnableKeyword("_EMISSION");row.giBefore=(int)material.globalIlluminationFlags;row.keywordBefore=material.IsKeywordEnabled("_EMISSION");row.keywordsBefore=material.shaderKeywords.OrderBy(k=>k,StringComparer.Ordinal).ToArray();row.propertiesBefore=Properties(material);
                setter.Invoke(null,new object[]{material,null,null});row.giAfterFirst=(int)material.globalIlluminationFlags;row.keywordAfterFirst=material.IsKeywordEnabled("_EMISSION");row.keywordsAfterFirst=material.shaderKeywords.OrderBy(k=>k,StringComparer.Ordinal).ToArray();row.propertiesAfterFirst=Properties(material);string first=EditorJsonUtility.ToJson(material);
                setter.Invoke(null,new object[]{material,null,null});row.giAfterSecond=(int)material.globalIlluminationFlags;row.keywordAfterSecond=material.IsKeywordEnabled("_EMISSION");row.keywordsAfterSecond=material.shaderKeywords.OrderBy(k=>k,StringComparer.Ordinal).ToArray();row.propertiesAfterSecond=Properties(material);row.firstRepeatStateExact=first==EditorJsonUtility.ToJson(material);var expectedKeywords=row.keywordsBefore.Where(k=>k!="_EMISSION"||row.requestedGi==2).ToArray();row.otherKeywordsExact=expectedKeywords.SequenceEqual(row.keywordsAfterFirst)&&row.keywordsAfterFirst.SequenceEqual(row.keywordsAfterSecond);row.propertiesPreserved=row.propertiesBefore==row.propertiesAfterFirst&&row.propertiesAfterFirst==row.propertiesAfterSecond;
                Assert.That(row.keywordBefore,Is.True);Assert.That(row.otherKeywordsExact,Is.True,"Only the intentionally inconsistent emission keyword may change");if(row.requestedGi==2){Assert.That(row.giAfterFirst,Is.EqualTo(2));Assert.That(row.giAfterSecond,Is.EqualTo(2));}Assert.That(row.keywordAfterFirst&&row.keywordAfterSecond,Is.EqualTo(row.requestedGi==2),"Installed public URP setter must demonstrate the intended AnyEmissive-bit decision");Assert.That(row.firstRepeatStateExact,Is.True,"Public setter must become idempotent; retain all actual payload differences on failure");Assert.That(row.propertiesPreserved,Is.True,"No diffuse/normal/map/UV/scalar/color/render setting drift is allowed in the proposed GI-only correction");
                EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);AssetDatabase.ImportAsset(row.path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);material=AssetDatabase.LoadAssetAtPath<Material>(row.path);row.firstImportedSha256=Hash(row.path);Assert.That(material.IsKeywordEnabled("_EMISSION"),Is.EqualTo(row.requestedGi==2));if(row.requestedGi==2)Assert.That((int)material.globalIlluminationFlags,Is.EqualTo(2));Assert.That(Properties(material),Is.EqualTo(row.propertiesAfterSecond));
                AssetDatabase.ImportAsset(row.path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);material=AssetDatabase.LoadAssetAtPath<Material>(row.path);row.secondImportedSha256=Hash(row.path);row.twoNativeImportHashesExact=row.firstImportedSha256==row.secondImportedSha256;Assert.That(row.twoNativeImportHashesExact,Is.True);Assert.That(material.IsKeywordEnabled("_EMISSION"),Is.EqualTo(row.requestedGi==2));if(row.requestedGi==2)Assert.That((int)material.globalIlluminationFlags,Is.EqualTo(2));Assert.That(Properties(material),Is.EqualTo(row.propertiesAfterSecond));post.Add(new FileProof{path=Physical(row.path),sha256=row.secondImportedSha256});post.Add(new FileProof{path=Physical(row.path+".meta"),sha256=Hash(row.path+".meta")});
            }
            proof.status="NATIVE_DISPOSABLE_SETTER_AND_TWO_IMPORTS_PASSED_REQUIRES_INDEPENDENT_POST_EDITOR_HASH_GUARD";
        }
        catch(Exception e){proof.failure=e.ToString();proof.status="REJECTED_NATIVE_EMISSION_IMPORT_CONTROL";throw;}
        finally
        {
            proof.variants=variants.ToArray();proof.originalSourceBytesExact=watched.All(p=>Hash(p.Key)==p.Value);proof.sceneContextExact=context==Context()&&SceneManager.GetActiveScene()==active;if(!proof.originalSourceBytesExact||!proof.sceneContextExact)proof.status="REJECTED_ORIGINAL_SOURCE_BYTES_OR_SCENE_CONTEXT";proof.postEditorExpectedFiles=proof.originalSources.Concat(post).ToArray();File.WriteAllText(Io(report),JsonUtility.ToJson(proof,true));Assert.That(proof.originalSourceBytesExact,Is.True,"Original gold/meta and authoring source/meta must remain EXACT; no restoration by this diagnostic");Assert.That(proof.sceneContextExact,Is.True);
        }
    }
}
#endif
