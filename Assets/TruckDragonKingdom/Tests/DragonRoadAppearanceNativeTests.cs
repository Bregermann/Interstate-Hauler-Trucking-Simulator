#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DragonRoadAppearanceNativeTests
{
    static readonly string[] Modes={"TruckHoardHeist","TruckDragonEggDelivery","TruckDragonEscape","TruckDragonRiders"};
    const string Root="Assets/TruckDragonKingdom/";
    static Type Author=>Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRoadSnowAuthoring, Assembly-CSharp-Editor",true);
    static string Approval()
    {
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonWorldAdoptionReceipt");
        Assert.That(at>=0&&at+1<args.Length,Is.True,"Post-adoption tests require the actual successful WorldApply receipt; no pre-adoption execution.");
        string path=args[at+1];Author.GetMethod("ValidateAdoptedWorld",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{path});return path;
    }
    static string[] Variants()=>Modes.SelectMany(mode=>Directory.GetFiles("Assets/"+mode+"/Materials/Kingdom Road Snow","*.mat",SearchOption.TopDirectoryOnly)).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
    static string[] SceneFiles()=>Modes.SelectMany(mode=>new[]{"Assets/"+mode+"/Scenes/"+mode+".unity","Assets/"+mode+"/Scenes/"+mode+".unity.meta"}).Concat(new[]{Root+"Prefabs/Shared Dragon Kingdom.prefab",Root+"Prefabs/Shared Dragon Kingdom.prefab.meta"}).ToArray();
    static string Io(string path){string full=Path.GetFullPath(path);return Application.platform==RuntimePlatform.WindowsEditor&&full.Length>=248&&!full.StartsWith(@"\\?\",StringComparison.Ordinal)?@"\\?\"+full:full;}
    static string Hash(string path){using(var sha=SHA256.Create())return string.Concat(sha.ComputeHash(File.ReadAllBytes(Io(path))).Select(b=>b.ToString("x2")));}
    static Dictionary<string,string> Snapshot(IEnumerable<string> paths)=>paths.Distinct().ToDictionary(path=>path,Hash,StringComparer.Ordinal);
    static void Exact(Dictionary<string,string> paths){foreach(var pair in paths)Assert.That(Hash(pair.Key),Is.EqualTo(pair.Value),"Original bytes preserved: "+pair.Key);}
    static string Appearance(Material m)=>(string)Author.GetMethod("AppearanceHash").Invoke(null,new object[]{m});
    static string Protected(Material m)=>(string)Author.GetMethod("ProtectedAppearanceHash").Invoke(null,new object[]{m});
    static Material Prototype(Material m,bool legacy)=>(Material)Author.GetMethod("BuildMappedAppearance",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{m,legacy});
    static object Preview(Material m,Material old,Material next)=>Author.GetMethod("PreviewVariantAppearance").Invoke(null,new object[]{m,old,next});
    static object Field(object o,string field)=>o.GetType().GetField(field).GetValue(o);
    static void Refresh(string approval)=>Author.GetMethod("RefreshSavedVariantsForAdoptedWorld",new[]{typeof(string)}).Invoke(null,new object[]{approval});
    static string SceneText(string path)=>Encoding.UTF8.GetString(File.ReadAllBytes(Io(path)));
    static bool OnlyBinding(string before,string after,string id)=>(bool)Author.GetMethod("OnlyClearRoadBindingChanged").Invoke(null,new object[]{before,after,id});
    static Match SceneSnowDocument(string text)
    {
        string script=AssetDatabase.AssetPathToGUID("Assets/TruckSports/Scripts/SportsSnowSurface.cs");
        return Regex.Matches(text,@"(?ms)^--- !u!114 &(?<id>\d+)(?: stripped)?\r?\n.*?(?=^--- !u!|\z)").Cast<Match>().Single(m=>m.Value.Contains("guid: "+script+","));
    }

    [Test,Timeout(30000)]
    public void ActualSavedScenesRejectEveryChangeExceptTheirOwnClearRoadBinding()
    {
        var before=Snapshot(SceneFiles());string material="Assets/TruckDragonKingdom/Materials/Kingdom gravel road.mat",guid=AssetDatabase.AssetPathToGUID(material);
        Assert.That(AssetDatabase.LoadAssetAtPath<Material>(material),Is.Not.Null);Assert.That(guid.Length,Is.EqualTo(32));
        foreach(string mode in Modes)
        {
            string path="Assets/"+mode+"/Scenes/"+mode+".unity",original=SceneText(path);var document=SceneSnowDocument(original);string id=document.Groups["id"].Value;
            string changed=(string)Author.GetMethod("InstallFieldInOriginal",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{original,id,"  clearRoadMaterial: {fileID: 2100000, guid: "+guid+", type: 2}\n"});
            Assert.That(OnlyBinding(original,changed,id),Is.True,"The actual original component may receive its sole new material field.");
            var transform=Regex.Match(changed,@"(?m)^  m_LocalPosition: [^\r\n]+");Assert.That(transform.Success,Is.True);
            string wrongPose=changed.Remove(transform.Index,transform.Length).Insert(transform.Index,"  m_LocalPosition: {x: 913, y: 712, z: -516}");
            Assert.That(OnlyBinding(original,wrongPose,id),Is.False,"A real unrelated Transform change must reject the whole native scene save.");
            string wrongContact=changed.Replace("  traceExposedRoads: 1","  traceExposedRoads: 0");Assert.That(wrongContact,Is.Not.EqualTo(changed));
            Assert.That(OnlyBinding(original,wrongContact,id),Is.False,"Changing actual road/contact behavior is outside the appearance-only upgrade.");
            var duplicate=Author.GetMethod("OnlyClearRoadBindingChanged");var invalid=Assert.Throws<TargetInvocationException>(()=>duplicate.Invoke(null,new object[]{original,changed,id+"9"}));Assert.That(invalid.InnerException,Is.TypeOf<InvalidOperationException>());
        }
        Exact(before);TestContext.WriteLine("ACTUAL_SAVED_SCENE_BINDING_GUARD scenes=4 ownFieldAccepted=true unrelatedTransformAndContactChangesRejected=true allSourceBytesExact=true noAdoptionOrSceneWritePerformed=true");
    }

    [Test,Timeout(180000)]
    public void ActualFactoryPbrMappingPreservesSnowTraceFieldsAndDeclinesDesignerAppearance()
    {
        Approval();string path=Variants().First();var installed=AssetDatabase.LoadAssetAtPath<Material>(path);var legacy=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(Path.GetFileNameWithoutExtension(path)));
        var canonicalWorld=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/Shared Dragon Kingdom.prefab");
        var canonical=canonicalWorld.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.GetComponent<MeshFilter>()?.sharedMesh!=null&&r.GetComponent<MeshCollider>()?.sharedMesh==r.GetComponent<MeshFilter>().sharedMesh).Where(r=>AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>().sharedMesh).StartsWith(Root+"Models/Kingdom ",StringComparison.Ordinal)&&Path.GetFileName(AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>().sharedMesh)).Contains(" EasyRoads mesh ")).SelectMany(r=>r.sharedMaterials).Distinct().Single();
        Assert.That(canonical,Is.Not.Null,"Requires actual adopted mesh-authoritative vendor road source, including retained candidate material references.");
        Assert.That(canonical.GetTexture("_MainTex"),Is.Not.Null);Assert.That(canonical.GetTexture("_BumpMap"),Is.Not.Null,"Genuine owned normal texture required.");
        var before=Snapshot(SceneFiles().Concat(new[]{path,path+".meta",AssetDatabase.GetAssetPath(legacy),AssetDatabase.GetAssetPath(legacy)+".meta",AssetDatabase.GetAssetPath(canonical),AssetDatabase.GetAssetPath(canonical)+".meta"}));
        Material sample=null,next=null;
        try
        {
            sample=Prototype(legacy,true);sample.name="Temporary exact original factory contract";next=Prototype(canonical,false);
            Assert.That((string)Field(Preview(sample,legacy,canonical),"status"),Is.EqualTo("ELIGIBLE_ORIGINAL_FACTORY_APPEARANCE"));
            foreach(string property in new[]{"_CoverageAmount","_TracesNormalScale","_CoverageAreaMaskRange"})Assert.That(sample.HasProperty(property),Is.True,"Use the actual installed vendor shader property: "+property);
            sample.SetFloat("_CoverageAmount",.43f);sample.SetFloat("_TracesNormalScale",1.7f);sample.SetFloat("_CoverageAreaMaskRange",.66f);string protectedBefore=Protected(sample);
            Author.GetMethod("CopyAppearance",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{next,sample});
            Assert.That(Protected(sample),Is.EqualTo(protectedBefore));Assert.That(sample.GetFloat("_CoverageAmount"),Is.EqualTo(.43f));Assert.That(sample.GetFloat("_TracesNormalScale"),Is.EqualTo(1.7f));Assert.That(sample.GetFloat("_CoverageAreaMaskRange"),Is.EqualTo(.66f));
            Assert.That(sample.GetTexture("_BaseMap"),Is.SameAs(canonical.GetTexture("_MainTex")));Assert.That(sample.GetTextureScale("_BaseMap"),Is.EqualTo(canonical.GetTextureScale("_MainTex")));Assert.That(sample.GetTextureOffset("_BaseMap"),Is.EqualTo(canonical.GetTextureOffset("_MainTex")));
            Assert.That(sample.GetTexture("_BumpMap"),Is.SameAs(canonical.GetTexture("_BumpMap")));Assert.That(sample.GetFloat("_BumpScale"),Is.EqualTo(canonical.GetFloat("_BumpScale")));Assert.That(sample.IsKeywordEnabled("_NORMALMAP"),Is.True);Assert.That(sample.GetFloat("_Metallic"),Is.EqualTo(canonical.GetFloat("_MainMetallicPower")));Assert.That(sample.GetFloat("_Smoothness"),Is.EqualTo(canonical.GetFloat("_MainSmoothnessPower")));
            Assert.That((string)Field(Preview(sample,legacy,canonical),"status"),Is.EqualTo("ALREADY_MATCHES_ADOPTED_CANONICAL_APPEARANCE"));
            sample.SetFloat("_BumpScale",sample.GetFloat("_BumpScale")+.137f);string custom=Appearance(sample);var declined=Preview(sample,legacy,canonical);
            Assert.That((string)Field(declined,"status"),Is.EqualTo("DECLINED_DESIGNER_APPEARANCE"));Assert.That(Appearance(sample),Is.EqualTo(custom),"Preview must preserve actual customized values.");Assert.That((bool)Field(declined,"depthBiasSupported"),Is.False,"Installed Weatherade Snow has no ER depth-bias fields; no shader substitution permitted.");
            Exact(before);
        }
        finally{if(sample!=null)UnityEngine.Object.DestroyImmediate(sample);if(next!=null)UnityEngine.Object.DestroyImmediate(next);}
    }

    [Test,Timeout(300000)]
    public void AcceptedAdoptionRefreshesOnlyExistingVariantAppearanceAndRepeatsExactBytes()
    {
        string approval=Approval();var variants=Variants();Assert.That(variants.Length,Is.GreaterThanOrEqualTo(4));
        var sceneBefore=Snapshot(SceneFiles());var sceneTexts=Modes.ToDictionary(mode=>"Assets/"+mode+"/Scenes/"+mode+".unity",mode=>SceneText("Assets/"+mode+"/Scenes/"+mode+".unity"));var metas=Snapshot(variants.Select(p=>p+".meta"));var protectedBefore=variants.ToDictionary(p=>p,p=>Protected(AssetDatabase.LoadAssetAtPath<Material>(p)));
        Refresh(approval);var receipt=Author.GetProperty("LastAppearanceReceipt").GetValue(null);
        foreach(string field in new[]{"sceneIdsPhysicsAndOverridesExact","sceneBindingsOnlyClearMaterialChanged","canonicalSourcesExact","variantGuidsExact","protectedSnowAndTracePropertiesExact"})Assert.That((bool)Field(receipt,field),Is.True,field);
        foreach(var row in (Array)Field(receipt,"variants"))
        {
            string path=(string)Field(row,"variant");Assert.That(Protected(AssetDatabase.LoadAssetAtPath<Material>(path)),Is.EqualTo(protectedBefore[path]));Assert.That((string)Field(row,"afterAppearanceHash"),Is.EqualTo((string)Field(row,"canonicalAppearanceHash")));Assert.That(AssetDatabase.AssetPathToGUID(path),Is.EqualTo((string)Field(row,"variantGuid")));
        }
        var bindings=(Array)Field(receipt,"sceneBindings");Assert.That(bindings.Length,Is.EqualTo(4));
        foreach(var row in bindings)
        {
            string path=(string)Field(row,"path"),id=(string)Field(row,"componentId");Assert.That((bool)Field(row,"otherSerializedFieldsExact"),Is.True);
            Assert.That(OnlyBinding(sceneTexts[path],SceneText(path),id),Is.True,"Independently verify the complete actual saved scene, allowing only the selected material binding.");
            Assert.That(SceneSnowDocument(SceneText(path)).Value,Does.Contain("guid: "+(string)Field(row,"materialGuid")+","));
        }
        foreach(var pair in sceneBefore.Where(pair=>!sceneTexts.ContainsKey(pair.Key)))Assert.That(Hash(pair.Key),Is.EqualTo(pair.Value),"Canonical world, original scene GUID metadata and every other protected file stay exact.");
        Exact(metas);var first=Snapshot(variants.SelectMany(p=>new[]{p,p+".meta"}).Concat(SceneFiles()));Refresh(approval);Refresh(approval);Exact(first);Exact(metas);
        receipt=Author.GetProperty("LastAppearanceReceipt").GetValue(null);Assert.That((string)Field(receipt,"status"),Is.EqualTo("NOOP_EXISTING_VARIANTS_BYTE_PRESERVED"));
        TestContext.WriteLine("ACTUAL_POST_ADOPTION_ROAD_PBR variants="+variants.Length+" GUIDs=true scenesPhysicsMarkersBytesExact=true snowTracePropertiesExact=true repeatBytesExact=true visibleRoadAndSnowReviewStillRequired=true");
    }

    [Test,Timeout(300000)]
    public void DesignerClearRoadBindingDeclinesWholeBatchBeforeAnyAppearanceWrite()
    {
        string approval=Approval();Refresh(approval);string path="Assets/TruckHoardHeist/Scenes/TruckHoardHeist.unity";
        var outputs=Variants().SelectMany(p=>new[]{p,p+".meta"}).Concat(SceneFiles()).Distinct().ToArray();var bytes=outputs.ToDictionary(p=>p,p=>File.ReadAllBytes(Io(p)));string original=SceneText(path);var document=SceneSnowDocument(original);string id=document.Groups["id"].Value;
        try
        {
            string custom=Variants().First(),guid=AssetDatabase.AssetPathToGUID(custom);
            string changed=(string)Author.GetMethod("InstallFieldInOriginal",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{original,id,"  clearRoadMaterial: {fileID: 2100000, guid: "+guid+", type: 2}\n"});
            File.WriteAllBytes(Io(path),Encoding.UTF8.GetBytes(changed));AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);var modified=Snapshot(outputs);
            var failure=Assert.Throws<TargetInvocationException>(()=>Refresh(approval));Assert.That(failure.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That((string)Field(Author.GetProperty("LastAppearanceReceipt").GetValue(null),"status"),Is.EqualTo("DECLINED_DESIGNER_CLEAR_MATERIAL_NO_WRITES"));Exact(modified);
        }
        finally
        {
            foreach(var pair in bytes)if(!File.ReadAllBytes(Io(pair.Key)).SequenceEqual(pair.Value)){File.WriteAllBytes(Io(pair.Key),pair.Value);if(!pair.Key.EndsWith(".meta",StringComparison.Ordinal))AssetDatabase.ImportAsset(pair.Key,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);}
            foreach(var pair in bytes)CollectionAssert.AreEqual(pair.Value,File.ReadAllBytes(Io(pair.Key)),"Restore actual selected native scene after temporary designer binding.");
        }
    }

    [Test,Timeout(300000)]
    public void DesignerModifiedSavedVariantDeclinesWholeBatchBeforeAnyRefreshWrite()
    {
        string approval=Approval();var variants=Variants();var originals=variants.SelectMany(p=>new[]{p,p+".meta"}).ToDictionary(p=>p,p=>File.ReadAllBytes(Io(p)));var sceneBefore=Snapshot(SceneFiles());string path=variants[0];
        try
        {
            var variant=AssetDatabase.LoadAssetAtPath<Material>(path);variant.SetTextureOffset("_BaseMap",variant.GetTextureOffset("_BaseMap")+new Vector2(.271f,.183f));EditorUtility.SetDirty(variant);AssetDatabase.SaveAssetIfDirty(variant);var custom=Snapshot(originals.Keys);
            var failure=Assert.Throws<TargetInvocationException>(()=>Refresh(approval));Assert.That(failure.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That((string)Field(Author.GetProperty("LastAppearanceReceipt").GetValue(null),"status"),Is.EqualTo("DECLINED_DESIGNER_APPEARANCE_NO_VARIANT_WRITES"));Exact(custom);Exact(sceneBefore);
        }
        finally
        {
            foreach(var pair in originals)if(!File.ReadAllBytes(Io(pair.Key)).SequenceEqual(pair.Value)){File.WriteAllBytes(Io(pair.Key),pair.Value);if(!pair.Key.EndsWith(".meta",StringComparison.Ordinal))AssetDatabase.ImportAsset(pair.Key,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);}
            foreach(var pair in originals)CollectionAssert.AreEqual(pair.Value,File.ReadAllBytes(Io(pair.Key)),"Restore original native-clone material bytes after temporary designer edit.");Exact(sceneBefore);
        }
    }
}
#endif
