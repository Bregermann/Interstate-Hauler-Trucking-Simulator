using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEngine;
public sealed class CampingCompletedStageTests
{
    [Serializable]sealed class Row{public string path,sha256;public long bytes;}
    [Serializable]sealed class Stage{public Row[] files;}
    static Stage Read(string file)=>JsonUtility.FromJson<Stage>(File.ReadAllText(file));
    static string Digest(string path){using(var sha=SHA256.Create())return string.Concat(sha.ComputeHash(File.ReadAllBytes(path)).Select(b=>b.ToString("x2")));}
    const string Completed="Assets/TruckCamping/Editor/NavigationCompletedStageV6.json";
    static string ActualReviewedManifest()=> (string)Type.GetType("LWS.TruckCamping.Editor.CampingWildernessAuthoring, Assembly-CSharp-Editor",true).GetProperty("ReviewedCompletedStageManifest",BindingFlags.Public|BindingFlags.Static).GetValue(null);
    [Test]public void ExactActualSavedStageContainsOnlyEightReviewedPayloadChangesAndEveryOriginalMeta()
    {
        var before=Read("Assets/TruckCamping/Editor/NavigationMaterializedStageV3.json");var after=Read(ActualReviewedManifest());Assert.That(after.files.Length,Is.EqualTo(127));
        var changed=after.files.Where(x=>before.files.Single(y=>y.path==x.path).sha256!=x.sha256).Select(x=>x.path).ToArray();
        CollectionAssert.AreEquivalent(new[]{"Assets/TruckCamping/Models/Camp Haul connected woodland navigation.asset","Assets/TruckCamping/Models/Camp Haul scenic woodland loop EasyRoads mesh 0.asset","Assets/TruckCamping/Models/Camp Haul Vista terrain 0 0.asset","Assets/TruckCamping/Models/Camp Haul Vista terrain 1 0.asset","Assets/TruckCamping/Models/Camp Haul Vista terrain 0 1.asset","Assets/TruckCamping/Models/Camp Haul Vista terrain 1 1.asset","Assets/TruckCamping/Materials/Camp Haul River Modeler flowing water.mat","Assets/TruckCamping/Materials/Camp Haul terrain surface.mat"},changed);
        foreach(var file in after.files){Assert.That(File.Exists(file.path),Is.True);Assert.That(new FileInfo(file.path).Length,Is.EqualTo(file.bytes));Assert.That(Digest(file.path),Is.EqualTo(file.sha256));if(file.path.EndsWith(".meta",StringComparison.Ordinal))Assert.That(file.sha256,Is.EqualTo(before.files.Single(f=>f.path==file.path).sha256),"Every original GUID/importer meta remains exact.");}
    }
    [Test]public void ActualCompletedForestAndOriginalMaterialRecipePreflightMutatesNoPersistentInput()
    {
        var stage=Read(ActualReviewedManifest());var before=stage.files.Select(f=>Digest(f.path)).ToArray();var type=Type.GetType("LWS.TruckCamping.Editor.CampingWildernessAuthoring, Assembly-CSharp-Editor",true);
        try{type.GetMethod("VerifyCompletedTerrainMaterialState",BindingFlags.Public|BindingFlags.Static).Invoke(null,null);}
        catch(TargetInvocationException error){throw error.InnerException;}
        for(int i=0;i<stage.files.Length;i++)Assert.That(Digest(stage.files[i].path),Is.EqualTo(before[i]),"Read-only actual material/forest verification cannot rewrite any of127 persisted inputs.");
        TestContext.WriteLine("CAMPING_COMPLETED_STATE_PROOF all127exact/allmetasretained/sourcewater+terrainshader+4persistedforests verified without mutations. Actual scene reconstruction/navigation/rendering remain separate native acceptance.");
    }
    [Test]public void InstalledUrpNormalizationReproducesReviewedTransparentWaterAndIsIdempotentWithoutAssetMutation()
    {
        const string path="Assets/TruckCamping/Materials/Camp Haul River Modeler flowing water.mat";
        string digest=Digest(path);var original=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);Assert.That(original,Is.Not.Null);
        var clone=new Material(original);
        try
        {
            // Reconstruct only the seven exact R18-to-V21 differences on a detached clone.
            clone.DisableKeyword("_ALPHAPREMULTIPLY_ON");clone.SetOverrideTag("RenderType","Opaque");
            clone.SetShaderPassEnabled("DepthOnly",true);clone.SetShaderPassEnabled("ShadowCaster",true);
            clone.SetTexture("_MainTex",null);clone.SetTextureScale("_MainTex",Vector2.one);clone.SetFloat("_SrcBlend",5);clone.SetFloat("_DstBlendAlpha",0);
            var gui=Type.GetType("UnityEditor.BaseShaderGUI, Unity.RenderPipelines.Universal.Editor",true);
            var lit=Type.GetType("UnityEditor.Rendering.Universal.ShaderGUI.LitGUI, Unity.RenderPipelines.Universal.Editor",true);
            var shading=(Action<Material>)Delegate.CreateDelegate(typeof(Action<Material>),lit.GetMethod("SetMaterialKeywords",BindingFlags.Public|BindingFlags.Static));
            var normalize=gui.GetMethod("SetMaterialKeywords",BindingFlags.Public|BindingFlags.Static);
            normalize.Invoke(null,new object[]{clone,shading,null});
            foreach(string property in new[]{"_Blend","_BlendModePreserveSpecular","_SrcBlend","_DstBlend","_SrcBlendAlpha","_DstBlendAlpha","_Surface","_ZWrite","_BumpScale"})Assert.That(clone.GetFloat(property),Is.EqualTo(original.GetFloat(property)),property);
            CollectionAssert.AreEquivalent(original.shaderKeywords,clone.shaderKeywords);
            Assert.That(clone.GetTag("RenderType",false),Is.EqualTo("Transparent"));
            foreach(string pass in new[]{"DepthOnly","ShadowCaster","MotionVectors"})Assert.That(clone.GetShaderPassEnabled(pass),Is.EqualTo(original.GetShaderPassEnabled(pass)),pass);
            foreach(string texture in new[]{"_MainTex","_BaseMap","_BumpMap"}){Assert.That(clone.GetTexture(texture),Is.SameAs(original.GetTexture(texture)));Assert.That(clone.GetTextureScale(texture),Is.EqualTo(original.GetTextureScale(texture)));}
            Assert.That(clone.renderQueue,Is.EqualTo(original.renderQueue));
            string once=UnityEditor.EditorJsonUtility.ToJson(clone);normalize.Invoke(null,new object[]{clone,shading,null});
            Assert.That(UnityEditor.EditorJsonUtility.ToJson(clone),Is.EqualTo(once),"Installed vendor normalization is idempotent on the detached material.");
        }
        finally{UnityEngine.Object.DestroyImmediate(clone);}
        Assert.That(Digest(path),Is.EqualTo(digest),"Native clone proof must not rewrite the reviewed original material.");
    }
}
