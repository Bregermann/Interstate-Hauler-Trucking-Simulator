#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LWS.InterstateHauler;
using LWS.TruckSports;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRoadSnowAuthoring
    {
        static readonly string[] AppearanceTextures={"_BaseMap","_BumpMap","_MetallicGlossMap","_OcclusionMap"};
        static readonly string[] AppearanceFloats={"_BumpScale","_Metallic","_Smoothness","_OcclusionStrength","_OffsetFactor","_OffsetUnit"};
        static readonly string[] AppearanceColors={"_BaseColor","_Color"};
        static readonly string[] AppearanceKeywords={"_NORMALMAP","_METALLICSPECGLOSSMAP","_OCCLUSIONMAP"};
        [Serializable]public sealed class AppearanceChange
        {
            public string variant,variantGuid,legacySource,legacySourceGuid,canonicalSource,canonicalSourceGuid;
            public string originalAppearanceHash,legacyExpectedAppearanceHash,canonicalAppearanceHash,afterAppearanceHash;
            public string legacySourceSha256,canonicalSourceSha256,beforeSha256,afterSha256,metaSha256,protectedPropertiesHash,status,reason;
            public bool depthBiasSupported;
        }
        [Serializable]public sealed class AppearanceReceipt
        {
            public string scope="EXPLICIT_POST_ADOPTION_EXISTING_SNOW_VARIANT_PBR_REFRESH_ONLY",status="IN_PROGRESS",adoptionReceipt,adoptionReceiptSha256,candidate;
            public bool sceneIdsPhysicsAndOverridesExact,sceneBindingsOnlyClearMaterialChanged,canonicalSourcesExact,variantGuidsExact,protectedSnowAndTracePropertiesExact;
            public AppearanceChange[] variants;
            public SceneAppearanceBinding[] sceneBindings;
        }
        [Serializable]public sealed class SceneAppearanceBinding
        {
            public string scene,path,sceneGuid,componentId,adoptionSha256,beforeSha256,afterSha256,metaSha256,material,materialGuid,status;
            public bool otherSerializedFieldsExact;
        }
        [Serializable]sealed class AppliedFile{public string path="",sha256="";}
        [Serializable]sealed class AppliedWorld
        {
            public string candidate="",status="",prefabGuid="";
            public bool prefabIdentitiesPreserved=false,previewIdentitiesPreserved=false,originalGuidsPreserved=false,originalSourceBytesPreserved=false,definitionRulesPreserved=false,physicalGatesPassed=false,allModeExternalStateAndSceneOwnedIdsPreserved=false;
            public AppliedFile[] savedFiles=Array.Empty<AppliedFile>();
        }
        sealed class VariantPlan{public Material variant,canonical;public AppearanceChange record;public string protectedHash,metaHash;}
        sealed class BindingPlan{public Material material;public SceneAppearanceBinding record;}
        public static AppearanceReceipt LastAppearanceReceipt{get;private set;}

        public static void RefreshSavedVariantsForAdoptedWorld()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonWorldAdoptionReceipt");
            if(at<0||at+1>=args.Length)throw new InvalidOperationException("Supply the actual successful -tsDragonWorldAdoptionReceipt; never refresh before accepted canonical adoption.");
            RefreshSavedVariantsForAdoptedWorld(args[at+1]);
        }
        public static void RefreshSavedVariantsForAdoptedWorld(string adoptionReceiptPath)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve user scene edits before explicit material refresh.");
            var approval=ValidateAdoptedWorld(adoptionReceiptPath);
            var receipt=new AppearanceReceipt{adoptionReceipt=Path.GetFullPath(adoptionReceiptPath),adoptionReceiptSha256=Hash(adoptionReceiptPath),candidate=approval.candidate};LastAppearanceReceipt=receipt;
            string canonicalPath=DragonKingdomWorldAuthoring.PrefabPath;var canonical=AssetDatabase.LoadAssetAtPath<GameObject>(canonicalPath);
            if(canonical==null)throw new InvalidOperationException("Actual adopted canonical world prefab required.");
            var canonicalRoads=canonical.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.GetComponent<MeshFilter>()?.sharedMesh!=null&&r.GetComponent<MeshCollider>()?.sharedMesh==r.GetComponent<MeshFilter>().sharedMesh)
                .Where(r=>AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>().sharedMesh).StartsWith("Assets/TruckDragonKingdom/Models/Kingdom ",StringComparison.Ordinal)&&Path.GetFileName(AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>().sharedMesh)).Contains(" EasyRoads mesh ")).ToArray();
            var sources=canonicalRoads.GroupBy(MeshIdentity).ToDictionary(g=>g.Key,g=>g.Select(r=>r.sharedMaterials).ToArray());
            var plans=new Dictionary<string,VariantPlan>(StringComparer.Ordinal);var watched=approval.savedFiles.ToDictionary(f=>f.path,f=>Hash(f.path),StringComparer.Ordinal);
            var bindings=new List<BindingPlan>();
            foreach(var file in approval.savedFiles)watched.Add(file.path+".meta",Hash(file.path+".meta"));
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(var entry in SportsDragonGames.All)
                {
                    var scene=EditorSceneManager.OpenScene(entry.ScenePath,OpenSceneMode.Single);var authority=Authority(scene);var intendedSources=new HashSet<Material>();
                    foreach(var renderer in Roads(scene))
                    {
                        string mesh=MeshIdentity(renderer);
                        if(!sources.TryGetValue(mesh,out var choices))throw new InvalidOperationException("Actual scene road mesh has no canonical source: "+mesh);
                        var slots=renderer.sharedMaterials;
                        for(int slot=0;slot<slots.Length;slot++)
                        {
                            var variant=slots[slot];string path=AssetDatabase.GetAssetPath(variant),folder="Assets/"+entry.scene+"/Materials/Kingdom Road Snow/";
                            if(variant==null||!path.StartsWith(folder,StringComparison.Ordinal)||!LwsWeatheradeMaterialFactory.IsWeatheradeMaterialForMode(variant,LwsWeatheradeSurfaceMaterialMode.Snow))throw new InvalidOperationException("Preserve and review non-owned/custom road override: "+path);
                            if(EditorUtility.IsDirty(variant))throw new InvalidOperationException("Preserve unsaved designer material edits before explicit appearance refresh: "+path);
                            var mapped=choices.Select(values=>slot<values.Length?values[slot]:null).Distinct().ToArray();
                            if(mapped.Length!=1||mapped[0]==null)throw new InvalidOperationException("Ambiguous canonical source for actual mesh/slot: "+mesh+"/"+slot);
                            var intended=mapped[0];string oldGuid=Path.GetFileNameWithoutExtension(path),oldPath=AssetDatabase.GUIDToAssetPath(oldGuid);var legacy=AssetDatabase.LoadAssetAtPath<Material>(oldPath);
                            intendedSources.Add(intended);
                            if(legacy==null)throw new InvalidOperationException("Original factory source GUID no longer resolves; preserve variant: "+path);
                            var decision=PreviewVariantAppearance(variant,legacy,intended);
                            if(plans.TryGetValue(path,out var prior))
                            {
                                if(prior.record.canonicalAppearanceHash!=decision.canonicalAppearanceHash||prior.record.canonicalSourceGuid!=decision.canonicalSourceGuid)throw new InvalidOperationException("One existing variant maps to conflicting canonical road sources; preserve it: "+path);
                                continue;
                            }
                            foreach(string source in new[]{oldPath,AssetDatabase.GetAssetPath(intended)})foreach(string suffix in new[]{"",".meta"})if(!watched.ContainsKey(source+suffix))watched.Add(source+suffix,Hash(source+suffix));
                            decision.beforeSha256=Hash(path);decision.legacySourceSha256=Hash(oldPath);decision.canonicalSourceSha256=Hash(AssetDatabase.GetAssetPath(intended));
                            decision.protectedPropertiesHash=ProtectedAppearanceHash(variant);decision.metaSha256=Hash(path+".meta");
                            plans.Add(path,new VariantPlan{variant=variant,canonical=intended,record=decision,protectedHash=decision.protectedPropertiesHash,metaHash=decision.metaSha256});
                        }
                    }
                    var after=Authority(scene);if(authority.Count!=after.Count||authority.Any(pair=>!after.TryGetValue(pair.Key,out string value)||value!=pair.Value))throw new InvalidOperationException("Read-only material plan changed original scene component/marker/physics authority.");
                    if(intendedSources.Count!=1)throw new InvalidOperationException("The existing scene clear-road adapter requires one unambiguous actual canonical road material; preserve heterogeneous sources for review.");
                    var snow=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<SportsSnowSurface>(true)).Single();var serialized=new SerializedObject(snow);var property=serialized.FindProperty("clearRoadMaterial");
                    if(property==null)throw new InvalidOperationException("Updated existing SportsSnowSurface.clearRoadMaterial API required.");
                    var intendedClear=intendedSources.Single();var current=property.objectReferenceValue as Material;
                    var binding=new SceneAppearanceBinding{scene=entry.scene,path=entry.ScenePath,sceneGuid=AssetDatabase.AssetPathToGUID(entry.ScenePath),componentId=GlobalObjectId.GetGlobalObjectIdSlow(snow).targetObjectId.ToString(System.Globalization.CultureInfo.InvariantCulture),adoptionSha256=approval.savedFiles.Single(f=>f.path==entry.ScenePath).sha256,beforeSha256=Hash(entry.ScenePath),afterSha256=Hash(entry.ScenePath),metaSha256=Hash(entry.ScenePath+".meta"),material=AssetDatabase.GetAssetPath(intendedClear),materialGuid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(intendedClear)),status=current==intendedClear?"ALREADY_BOUND_CANONICAL_CLEAR_MATERIAL":current==null?"ELIGIBLE_LEGACY_NULL_CLEAR_MATERIAL":"DECLINED_DESIGNER_CLEAR_MATERIAL"};
                    bindings.Add(new BindingPlan{material=intendedClear,record=binding});
                }
                receipt.variants=plans.Values.Select(p=>p.record).OrderBy(p=>p.variant,StringComparer.Ordinal).ToArray();
                receipt.sceneBindings=bindings.Select(p=>p.record).ToArray();
                if(plans.Count==0)throw new InvalidOperationException("Actual owned road variants required.");
                if(plans.Values.Any(p=>p.record.status=="DECLINED_DESIGNER_APPEARANCE"))
                {receipt.status="DECLINED_DESIGNER_APPEARANCE_NO_VARIANT_WRITES";throw new InvalidOperationException("Existing road variant appearance differs from both original factory baseline and adopted canonical source; preserve designer values. Inspect appearance receipt.");}
                if(bindings.Any(p=>p.record.status=="DECLINED_DESIGNER_CLEAR_MATERIAL"))
                {receipt.status="DECLINED_DESIGNER_CLEAR_MATERIAL_NO_WRITES";throw new InvalidOperationException("Preserve the existing non-null designer clear-road material binding; no refresh writes permitted.");}
                foreach(var plan in plans.Values)
                    if(Hash(plan.record.variant)!=plan.record.beforeSha256||Hash(plan.record.variant+".meta")!=plan.metaHash)throw new InvalidOperationException("Variant changed during preflight; no writes permitted: "+plan.record.variant);
                foreach(var pair in watched)if(Hash(pair.Key)!=pair.Value)throw new InvalidOperationException("Source/scene changed during material preflight: "+pair.Key);
                var before=plans.Keys.Concat(bindings.Select(p=>p.record.path)).SelectMany(p=>new[]{p,p+".meta"}).ToDictionary(p=>p,p=>File.ReadAllBytes(FullIo(p)));bool complete=false;
                try
                {
                    foreach(var plan in plans.Values)
                    {
                        if(plan.record.status=="ELIGIBLE_ORIGINAL_FACTORY_APPEARANCE")
                        {
                            var prototype=BuildMappedAppearance(plan.canonical,false);
                            try{CopyAppearance(prototype,plan.variant);}finally{UnityEngine.Object.DestroyImmediate(prototype);}
                            if(ProtectedAppearanceHash(plan.variant)!=plan.protectedHash)throw new InvalidOperationException("Snow/trace/coverage/shader/non-appearance material data changed: "+plan.record.variant);
                            EditorUtility.SetDirty(plan.variant);AssetDatabase.SaveAssetIfDirty(plan.variant);plan.record.status="REFRESHED_EXISTING_VARIANT_PBR";
                        }
                        plan.record.afterAppearanceHash=AppearanceHash(plan.variant);plan.record.afterSha256=Hash(plan.record.variant);
                        if(plan.record.afterAppearanceHash!=plan.record.canonicalAppearanceHash||Hash(plan.record.variant+".meta")!=plan.metaHash||AssetDatabase.AssetPathToGUID(plan.record.variant)!=plan.record.variantGuid)throw new InvalidOperationException("Exact intended appearance or variant GUID preservation failed.");
                    }
                    foreach(var binding in bindings)InstallClearMaterialBinding(binding);
                    var scenePaths=new HashSet<string>(bindings.Select(p=>p.record.path),StringComparer.Ordinal);
                    foreach(var pair in watched)if(!scenePaths.Contains(pair.Key)&&Hash(pair.Key)!=pair.Value)throw new InvalidOperationException("Original source/metadata changed during selected appearance refresh: "+pair.Key);
                    complete=true;
                }
                finally
                {
                    if(!complete){foreach(var pair in before)File.WriteAllBytes(FullIo(pair.Key),pair.Value);foreach(string path in plans.Keys.Concat(bindings.Select(p=>p.record.path)))AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);}
                }
                receipt.sceneIdsPhysicsAndOverridesExact=receipt.sceneBindingsOnlyClearMaterialChanged=receipt.canonicalSourcesExact=receipt.variantGuidsExact=receipt.protectedSnowAndTracePropertiesExact=true;
                receipt.status=plans.Values.All(p=>p.record.status=="ALREADY_MATCHES_ADOPTED_CANONICAL_APPEARANCE")&&bindings.All(p=>p.record.status=="ALREADY_BOUND_CANONICAL_CLEAR_MATERIAL")?"NOOP_EXISTING_VARIANTS_BYTE_PRESERVED":"REFRESHED_EXISTING_VARIANT_PBR_AND_CLEAR_SOURCE_REQUIRES_ACTUAL_WEATHER_RENDER_REVIEW";
            }
            finally
            {
                if(receipt.variants==null)receipt.variants=plans.Values.Select(p=>p.record).ToArray();
                if(receipt.sceneBindings==null)receipt.sceneBindings=bindings.Select(p=>p.record).ToArray();
                if(receipt.status=="IN_PROGRESS")receipt.status="FAILED_OR_ROLLED_BACK_APPEARANCE_REFRESH";
                WriteAppearanceReceipt(receipt);
                if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        public static AppearanceChange PreviewVariantAppearance(Material variant,Material legacySource,Material canonicalSource)
        {
            if(variant==null||legacySource==null||canonicalSource==null||!LwsWeatheradeMaterialFactory.IsWeatheradeMaterialForMode(variant,LwsWeatheradeSurfaceMaterialMode.Snow))throw new InvalidOperationException("Actual compatible existing snow variant and both real source materials required.");
            Material old=null,next=null;
            try
            {
                old=BuildMappedAppearance(legacySource,true);next=BuildMappedAppearance(canonicalSource,false);
                var result=new AppearanceChange{variant=AssetDatabase.GetAssetPath(variant),variantGuid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(variant)),legacySource=AssetDatabase.GetAssetPath(legacySource),legacySourceGuid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(legacySource)),canonicalSource=AssetDatabase.GetAssetPath(canonicalSource),canonicalSourceGuid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(canonicalSource)),originalAppearanceHash=AppearanceHash(variant),legacyExpectedAppearanceHash=AppearanceHash(old),canonicalAppearanceHash=AppearanceHash(next),depthBiasSupported=variant.HasProperty("_OffsetFactor")&&variant.HasProperty("_OffsetUnit")};
                result.status=result.originalAppearanceHash==result.canonicalAppearanceHash?"ALREADY_MATCHES_ADOPTED_CANONICAL_APPEARANCE":result.originalAppearanceHash==result.legacyExpectedAppearanceHash?"ELIGIBLE_ORIGINAL_FACTORY_APPEARANCE":"DECLINED_DESIGNER_APPEARANCE";
                result.reason=result.status=="DECLINED_DESIGNER_APPEARANCE"?"Managed PBR/normal keyword values differ from the reproducible original factory baseline and the adopted source; no overwrite authorized.":!result.depthBiasSupported?"Installed Weatherade Snow has no compatible ER depth-bias properties; no shader or geometry substitution performed.":"Compatible source appearance only.";
                return result;
            }
            finally{if(old!=null)UnityEngine.Object.DestroyImmediate(old);if(next!=null)UnityEngine.Object.DestroyImmediate(next);}
        }
        static Material BuildMappedAppearance(Material source,bool legacy)
        {
            if(!LwsWeatheradeMaterialFactory.TryCreateWeatheradeRoadSurfaceMaterial("Temporary actual factory appearance",source,Color.gray,LwsWeatheradeSurfaceMaterialMode.Snow,out var material))throw new InvalidOperationException("Installed Weatherade Snow factory unavailable.");
            string map=source.HasProperty("_BaseMap")?"_BaseMap":source.HasProperty("_MainTex")?"_MainTex":null;
            if(map!=null){material.SetTextureScale("_BaseMap",source.GetTextureScale(map));material.SetTextureOffset("_BaseMap",source.GetTextureOffset(map));}
            if(legacy){material.SetFloat("_Smoothness",.18f);return material;} // Exact original BindSceneRoads factory contract.
            foreach(string property in AppearanceTextures.Where(p=>p!="_BaseMap"))if(source.HasProperty(property)&&material.HasProperty(property)){material.SetTexture(property,source.GetTexture(property));material.SetTextureScale(property,source.GetTextureScale(property));material.SetTextureOffset(property,source.GetTextureOffset(property));}
            MapFloat(source,material,"_BumpScale","_BumpScale");MapFloat(source,material,"_Metallic","_MainMetallicPower","_Metallic");MapFloat(source,material,"_Smoothness","_MainSmoothnessPower","_Smoothness","_Glossiness");MapFloat(source,material,"_OcclusionStrength","_OcclusionStrength");
            MapFloat(source,material,"_OffsetFactor","_OffsetFactor");MapFloat(source,material,"_OffsetUnit","_OffsetUnit");
            SetKeyword(material,"_NORMALMAP",material.GetTexture("_BumpMap")!=null);SetKeyword(material,"_METALLICSPECGLOSSMAP",material.GetTexture("_MetallicGlossMap")!=null);SetKeyword(material,"_OCCLUSIONMAP",material.GetTexture("_OcclusionMap")!=null);
            return material;
        }
        static void MapFloat(Material source,Material destination,string target,params string[] aliases){if(!destination.HasProperty(target))return;foreach(string alias in aliases)if(source.HasProperty(alias)){destination.SetFloat(target,source.GetFloat(alias));return;}}
        static void CopyAppearance(Material source,Material target)
        {
            foreach(string property in AppearanceTextures)if(source.HasProperty(property)&&target.HasProperty(property)){target.SetTexture(property,source.GetTexture(property));target.SetTextureScale(property,source.GetTextureScale(property));target.SetTextureOffset(property,source.GetTextureOffset(property));}
            foreach(string property in AppearanceFloats)if(source.HasProperty(property)&&target.HasProperty(property))target.SetFloat(property,source.GetFloat(property));
            foreach(string property in AppearanceColors)if(source.HasProperty(property)&&target.HasProperty(property))target.SetColor(property,source.GetColor(property));
            foreach(string keyword in AppearanceKeywords)SetKeyword(target,keyword,source.IsKeywordEnabled(keyword));
        }
        static void SetKeyword(Material material,string keyword,bool enabled){if(enabled)material.EnableKeyword(keyword);else material.DisableKeyword(keyword);}
        public static string AppearanceHash(Material material)
        {
            var text=new StringBuilder();
            foreach(string property in AppearanceTextures)if(material.HasProperty(property)){var texture=material.GetTexture(property);string identity="null";if(texture!=null){AssetDatabase.TryGetGUIDAndLocalFileIdentifier(texture,out string guid,out long id);identity=guid+":"+id+":"+AssetDatabase.GetAssetPath(texture)+":"+texture.name;}text.Append(property).Append('=').Append(identity).Append('|').Append(JsonUtility.ToJson(new TextureUv{scale=material.GetTextureScale(property),offset=material.GetTextureOffset(property)})).Append(';');}
            foreach(string property in AppearanceFloats)if(material.HasProperty(property))text.Append(property).Append('=').Append(material.GetFloat(property).ToString("R",System.Globalization.CultureInfo.InvariantCulture)).Append(';');
            foreach(string property in AppearanceColors)if(material.HasProperty(property)){Color color=material.GetColor(property);text.Append(property).Append('=').Append(JsonUtility.ToJson(new Tint{value=color})).Append(';');}
            foreach(string keyword in AppearanceKeywords)text.Append(keyword).Append('=').Append(material.IsKeywordEnabled(keyword)).Append(';');return TextHash(text.ToString());
        }
        [Serializable]sealed class TextureUv{public Vector2 scale,offset;}
        [Serializable]sealed class Tint{public Color value;}
        public static string ProtectedAppearanceHash(Material source)
        {
            var clone=new Material(source){name=source.name};
            try
            {
                foreach(string property in AppearanceTextures)if(clone.HasProperty(property)){clone.SetTexture(property,null);clone.SetTextureScale(property,Vector2.one);clone.SetTextureOffset(property,Vector2.zero);}
                foreach(string property in AppearanceFloats)if(clone.HasProperty(property))clone.SetFloat(property,0);
                foreach(string property in AppearanceColors)if(clone.HasProperty(property))clone.SetColor(property,Color.clear);
                clone.shaderKeywords=clone.shaderKeywords.Where(k=>!AppearanceKeywords.Contains(k)).OrderBy(k=>k,StringComparer.Ordinal).ToArray();return TextHash(EditorJsonUtility.ToJson(clone));
            }
            finally{UnityEngine.Object.DestroyImmediate(clone);}
        }
        static string TextHash(string text){using(var sha=SHA256.Create())return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(text)).Select(b=>b.ToString("x2")));}
        static string MeshIdentity(MeshRenderer renderer){var mesh=renderer.GetComponent<MeshFilter>()?.sharedMesh;if(mesh==null||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh,out string guid,out long id))throw new InvalidOperationException("Actual asset-backed road mesh required.");return guid+":"+id;}
        static string SceneText(string path)=>Encoding.UTF8.GetString(File.ReadAllBytes(FullIo(path)));
        static string ComponentDocument(string text,string id)
        {
            var matches=Regex.Matches(text,@"(?ms)^--- !u!114 &"+Regex.Escape(id)+@"(?: stripped)?\r?\n.*?(?=^--- !u!|\z)");
            if(matches.Count!=1)throw new InvalidOperationException("Exactly one actual scene-owned SportsSnowSurface document required: "+id);
            string document=matches[0].Value,script=AssetDatabase.AssetPathToGUID("Assets/TruckSports/Scripts/SportsSnowSurface.cs");
            if(string.IsNullOrEmpty(script)||!Regex.IsMatch(document,@"(?m)^  m_Script: \{fileID: 11500000, guid: "+Regex.Escape(script)+@", type: 3\}\r?$"))throw new InvalidOperationException("Selected scene document is not the actual SportsSnowSurface script.");
            return document;
        }
        static string ClearField(string document)
        {
            var fields=Regex.Matches(document,@"(?m)^  clearRoadMaterial: [^\r\n]*\r?\n");
            if(fields.Count>1)throw new InvalidOperationException("Duplicate clear-road field; preserve serialized scene.");
            return fields.Count==0?null:fields[0].Value;
        }
        static string WithoutClearField(string document){string field=ClearField(document);return field==null?document:document.Replace(field,"");}
        public static bool OnlyClearRoadBindingChanged(string before,string after,string componentId)
        {
            string a=ComponentDocument(before,componentId),b=ComponentDocument(after,componentId);
            // Native SaveScene can normalize line endings. We never retain that
            // rewrite: the actual installed field is patched into original bytes.
            return before.Replace(a,WithoutClearField(a)).Replace("\r\n","\n")==after.Replace(b,WithoutClearField(b)).Replace("\r\n","\n");
        }
        static string InstallFieldInOriginal(string original,string id,string serializedField)
        {
            string document=ComponentDocument(original,id),old=ClearField(document),newline=document.Contains("\r\n")?"\r\n":"\n";
            string field=serializedField.TrimEnd('\r','\n')+newline;
            string updated;
            if(old!=null)updated=document.Replace(old,field);
            else
            {
                var anchors=Regex.Matches(document,@"(?m)^  traceExposedRoads: [^\r\n]*\r?\n");
                if(anchors.Count!=1)throw new InvalidOperationException("Existing real-contact road field required as the serialized insertion anchor.");
                updated=document.Insert(anchors[0].Index,field);
            }
            return original.Replace(document,updated);
        }
        static void InstallClearMaterialBinding(BindingPlan plan)
        {
            var row=plan.record;string original=SceneText(row.path);byte[] bytes=File.ReadAllBytes(FullIo(row.path));
            if(Hash(row.path)!=row.beforeSha256||Hash(row.path+".meta")!=row.metaSha256)throw new InvalidOperationException("Scene or metadata changed during binding preflight; preserve it.");
            if(row.status=="ALREADY_BOUND_CANONICAL_CLEAR_MATERIAL"){row.otherSerializedFieldsExact=true;return;}
            if(row.status!="ELIGIBLE_LEGACY_NULL_CLEAR_MATERIAL")throw new InvalidOperationException("Unreviewed clear-road binding cannot be overwritten.");
            bool complete=false;
            try
            {
                var scene=EditorSceneManager.OpenScene(row.path,OpenSceneMode.Single);var snow=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<SportsSnowSurface>(true)).Single();
                string id=GlobalObjectId.GetGlobalObjectIdSlow(snow).targetObjectId.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if(id!=row.componentId||snow.clearRoadMaterial!=null)throw new InvalidOperationException("Original owned component identity or designer binding changed.");
                var data=new SerializedObject(snow);data.FindProperty("clearRoadMaterial").objectReferenceValue=plan.material;data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(snow);
                if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Native selected scene save failed.");
                string saved=SceneText(row.path),field=ClearField(ComponentDocument(saved,id));
                if(field==null||!field.Contains("guid: "+row.materialGuid+"," )||!OnlyClearRoadBindingChanged(original,saved,id))throw new InvalidOperationException("Native save changed fields outside the sole authorized clear-road material binding; rolled back exact original scene bytes.");
                string exact=InstallFieldInOriginal(original,id,field);File.WriteAllBytes(FullIo(row.path),Encoding.UTF8.GetBytes(exact));
                if(Hash(row.path+".meta")!=row.metaSha256||AssetDatabase.AssetPathToGUID(row.path)!=row.sceneGuid)throw new InvalidOperationException("Scene GUID metadata changed during selected binding.");
                AssetDatabase.ImportAsset(row.path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
                scene=EditorSceneManager.OpenScene(row.path,OpenSceneMode.Single);snow=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<SportsSnowSurface>(true)).Single();
                if(snow.clearRoadMaterial!=plan.material||!OnlyClearRoadBindingChanged(original,SceneText(row.path),id))throw new InvalidOperationException("Actual reloaded binding or exact serialized preservation failed.");
                row.afterSha256=Hash(row.path);row.otherSerializedFieldsExact=true;row.status="BOUND_ACTUAL_CANONICAL_CLEAR_MATERIAL_ONLY";complete=true;
            }
            finally
            {
                if(!complete){File.WriteAllBytes(FullIo(row.path),bytes);AssetDatabase.ImportAsset(row.path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);EditorSceneManager.OpenScene(row.path,OpenSceneMode.Single);}
            }
        }
        static bool MatchesAdoptionWithOnlyClearBinding(string path,string expected)
        {
            string text=SceneText(path),script=AssetDatabase.AssetPathToGUID("Assets/TruckSports/Scripts/SportsSnowSurface.cs");
            var docs=Regex.Matches(text,@"(?ms)^--- !u!114 &(?<id>\d+)(?: stripped)?\r?\n.*?(?=^--- !u!|\z)").Cast<Match>().Where(m=>m.Value.Contains("guid: "+script+",")).ToArray();
            if(docs.Length!=1)return false;string document=docs[0].Value,field=ClearField(document);if(field==null)return false;
            string removed=text.Replace(document,document.Replace(field,""));
            if(TextHash(removed)==expected)return true;
            string newline=field.EndsWith("\r\n",StringComparison.Ordinal)?"\r\n":"\n";
            return TextHash(text.Replace(document,document.Replace(field,"  clearRoadMaterial: {fileID: 0}"+newline)))==expected;
        }
        static AppliedWorld ValidateAdoptedWorld(string path)
        {
            if(string.IsNullOrEmpty(path)||!File.Exists(FullIo(path)))throw new InvalidOperationException("Actual successful canonical WorldApply receipt required before road variant refresh.");
            var result=JsonUtility.FromJson<AppliedWorld>(File.ReadAllText(FullIo(path)));
            if(result==null||result.status!="ADOPTED_ORIGINAL_IDENTITIES_AND_MODES_PRESERVED_REQUIRES_ACTUAL_PLAY_ACCEPTANCE"||!result.prefabIdentitiesPreserved||!result.previewIdentitiesPreserved||!result.originalGuidsPreserved||!result.originalSourceBytesPreserved||!result.definitionRulesPreserved||!result.physicalGatesPassed||!result.allModeExternalStateAndSceneOwnedIdsPreserved||result.savedFiles==null||result.savedFiles.Length==0)throw new InvalidOperationException("Only successful identity-preserving accepted world adoption permits this selected refresh.");
            var files=result.savedFiles.ToDictionary(f=>f.path,StringComparer.Ordinal);
            foreach(string required in SportsDragonGames.All.Select(e=>e.ScenePath).Concat(new[]{DragonKingdomWorldAuthoring.PrefabPath}))if(!files.ContainsKey(required)||!File.Exists(FullIo(required+".meta")))throw new InvalidOperationException("Adoption proof omits actual canonical/mode asset or GUID metadata: "+required);
            if(AssetDatabase.AssetPathToGUID(DragonKingdomWorldAuthoring.PrefabPath)!=result.prefabGuid)throw new InvalidOperationException("Canonical prefab GUID differs from the actual successful adoption.");
            var scenes=new HashSet<string>(SportsDragonGames.All.Select(e=>e.ScenePath),StringComparer.Ordinal);
            foreach(var file in result.savedFiles)
                if(!File.Exists(FullIo(file.path))||Hash(file.path)!=file.sha256&&(!scenes.Contains(file.path)||!MatchesAdoptionWithOnlyClearBinding(file.path,file.sha256)))throw new InvalidOperationException("Canonical world or saved scene fields other than the sole clear-road binding changed since actual successful adoption; preserve and review them first.");
            return result;
        }
        public static void ValidateAdoptedSceneAfterAppearanceRefresh(string adoptionPath,string appearanceReceiptPath,string scenePath)
        {
            var adoption=ValidateAdoptedWorld(adoptionPath);
            if(string.IsNullOrEmpty(appearanceReceiptPath)||!File.Exists(FullIo(appearanceReceiptPath)))throw new InvalidOperationException("Actual successful road appearance receipt required for the explicit course chain.");
            var receipt=JsonUtility.FromJson<AppearanceReceipt>(File.ReadAllText(FullIo(appearanceReceiptPath)));
            bool success=receipt!=null&&(receipt.status=="NOOP_EXISTING_VARIANTS_BYTE_PRESERVED"||receipt.status=="REFRESHED_EXISTING_VARIANT_PBR_AND_CLEAR_SOURCE_REQUIRES_ACTUAL_WEATHER_RENDER_REVIEW");
            if(!success||receipt.candidate!=adoption.candidate||receipt.adoptionReceiptSha256!=Hash(adoptionPath)||!receipt.sceneIdsPhysicsAndOverridesExact||!receipt.sceneBindingsOnlyClearMaterialChanged||!receipt.canonicalSourcesExact||!receipt.variantGuidsExact||!receipt.protectedSnowAndTracePropertiesExact)throw new InvalidOperationException("Road appearance receipt must identify the same successful WorldApply and every preservation gate.");
            var expected=new HashSet<string>(SportsDragonGames.All.Select(entry=>entry.ScenePath),StringComparer.Ordinal);
            if(!expected.Contains(scenePath)||receipt.sceneBindings==null||receipt.sceneBindings.Length!=4||!expected.SetEquals(receipt.sceneBindings.Select(row=>row.path)))throw new InvalidOperationException("Exactly the four current actual Dragon scenes must be covered by the selected appearance chain.");
            var canonical=AssetDatabase.LoadAssetAtPath<GameObject>(DragonKingdomWorldAuthoring.PrefabPath);
            var roadSources=canonical.GetComponentsInChildren<MeshRenderer>(true).Where(renderer=>renderer.GetComponent<MeshFilter>()?.sharedMesh!=null&&renderer.GetComponent<MeshCollider>()?.sharedMesh==renderer.GetComponent<MeshFilter>().sharedMesh).Where(renderer=>AssetDatabase.GetAssetPath(renderer.GetComponent<MeshFilter>().sharedMesh).StartsWith("Assets/TruckDragonKingdom/Models/Kingdom ",StringComparison.Ordinal)&&Path.GetFileName(AssetDatabase.GetAssetPath(renderer.GetComponent<MeshFilter>().sharedMesh)).Contains(" EasyRoads mesh ")).SelectMany(renderer=>renderer.sharedMaterials).Distinct().ToArray();
            if(roadSources.Length!=1||roadSources[0]==null)throw new InvalidOperationException("Current canonical road material must remain unambiguous for this explicit scene adapter.");
            string canonicalPath=AssetDatabase.GetAssetPath(roadSources[0]),canonicalGuid=AssetDatabase.AssetPathToGUID(canonicalPath);
            foreach(var row in receipt.sceneBindings)
            {
                string original=adoption.savedFiles.Single(file=>file.path==row.path).sha256;
                if(row.adoptionSha256!=original||row.afterSha256!=Hash(row.path)||row.metaSha256!=Hash(row.path+".meta")||row.sceneGuid!=AssetDatabase.AssetPathToGUID(row.path)||!row.otherSerializedFieldsExact||row.material!=canonicalPath||row.materialGuid!=canonicalGuid)throw new InvalidOperationException("Current scene/GUID/binding differs from its exact accepted appearance record: "+row.path);
                if(Hash(row.path)!=original&&!MatchesAdoptionWithOnlyClearBinding(row.path,original))throw new InvalidOperationException("Original Apply scene bytes differ beyond the sole selected clear material field: "+row.path);
                string field=ClearField(ComponentDocument(SceneText(row.path),row.componentId));
                if(field==null||!field.Contains("guid: "+canonicalGuid+","))throw new InvalidOperationException("Actual original SportsSnowSurface document no longer binds its recorded canonical clear source.");
            }
            if(receipt.variants==null||receipt.variants.Length<4||receipt.variants.Select(row=>row.variant).Distinct(StringComparer.Ordinal).Count()!=receipt.variants.Length)throw new InvalidOperationException("Distinct actual used weather variants required for all four modes.");
            foreach(var entry in SportsDragonGames.All)if(!receipt.variants.Any(row=>row.variant.StartsWith("Assets/"+entry.scene+"/Materials/Kingdom Road Snow/",StringComparison.Ordinal)))throw new InvalidOperationException("Appearance proof omits one actual mode's road variants.");
            foreach(var row in receipt.variants)
            {
                string full=Path.GetFullPath(row.variant);bool owned=SportsDragonGames.All.Any(entry=>full.StartsWith(Path.GetFullPath("Assets/"+entry.scene+"/Materials/Kingdom Road Snow")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase));
                var material=owned?AssetDatabase.LoadAssetAtPath<Material>(row.variant):null;
                if(material==null||row.afterSha256!=Hash(row.variant)||row.metaSha256!=Hash(row.variant+".meta")||row.variantGuid!=AssetDatabase.AssetPathToGUID(row.variant)||row.protectedPropertiesHash!=ProtectedAppearanceHash(material)||row.afterAppearanceHash!=AppearanceHash(material)||row.afterAppearanceHash!=row.canonicalAppearanceHash||row.canonicalSource!=canonicalPath||row.canonicalSourceGuid!=canonicalGuid||row.canonicalSourceSha256!=Hash(canonicalPath)||row.legacySourceSha256!=Hash(row.legacySource)||AssetDatabase.AssetPathToGUID(row.legacySource)!=row.legacySourceGuid)throw new InvalidOperationException("Actual used road appearance/source/protected snow/GUID bytes changed: "+row.variant);
            }
        }
        static string FullIo(string path){string full=Path.GetFullPath(path);return Application.platform==RuntimePlatform.WindowsEditor&&full.Length>=248&&!full.StartsWith(@"\\?\",StringComparison.Ordinal)?@"\\?\"+full:full;}
        static void WriteAppearanceReceipt(AppearanceReceipt receipt){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");string folder=Path.Combine(at>=0&&at+1<args.Length?args[at+1]:"Logs/TruckSports/DragonKingdom","RoadSnow");Directory.CreateDirectory(FullIo(folder));string json=JsonUtility.ToJson(receipt,true);File.WriteAllText(FullIo(Path.Combine(folder,"adopted-appearance-refresh.json")),json);if(receipt.status=="NOOP_EXISTING_VARIANTS_BYTE_PRESERVED"||receipt.status=="REFRESHED_EXISTING_VARIANT_PBR_AND_CLEAR_SOURCE_REQUIRES_ACTUAL_WEATHER_RENDER_REVIEW")File.WriteAllText(FullIo(Path.Combine(folder,"adopted-appearance-refresh.accepted.json")),json);}
    }
}
#endif
