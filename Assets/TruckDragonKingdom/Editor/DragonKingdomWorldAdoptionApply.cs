#if UNITY_EDITOR && VISTA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LWS.TruckDragonKingdom.Ambience;
using LWS.TruckSports;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomWorldAdoptionPlan
    {
        const string CanonicalPreview=Root+"Scenes/DragonKingdomPreview.unity",CanonicalDefinition=Root+"Configuration/Dragon Kingdom.asset";
        static readonly string[] ModeNames={"TruckHoardHeist","TruckDragonEggDelivery","TruckDragonRiders","TruckDragonEscape"};
        [Serializable]public sealed class VisualAcceptance
        {
            public string scope,candidate,prefabSha256,previewSha256,renderReceipt,renderReceiptSha256;
            public bool acceptedForAdoption,humanVerified;public AcceptedImage[] reviewedImages;
        }
        [Serializable]public sealed class AcceptedImage{public string path,sha256;}
        [Serializable]sealed class RenderAcceptanceHeader{public string prefab,preview;public int width,height;public RenderRegion[] regions;}
        [Serializable]sealed class RenderRegion{public string id;public RenderCapture eye,overview;}
        [Serializable]sealed class RenderCapture{public string png;public int nonBackgroundPixels;}
        [Serializable]public sealed class WorldApplyReceipt
        {
            public string scope="EXPLICIT_IDENTITY_PRESERVING_WORLD_ADOPTION_NOT_WINDOWS_OR_HUMAN_GAMEPLAY_ACCEPTANCE",candidate,status="IN_PROGRESS",error,backupFolder,visualAcceptance;
            public string prefabGuid,previewGuid,definitionGuid;public int retainedPrefabIdentities,retainedPreviewIdentities;
            public bool prefabIdentitiesPreserved,previewIdentitiesPreserved,originalGuidsPreserved,originalSourceBytesPreserved,definitionRulesPreserved,physicalGatesPassed,allModeExternalStateAndSceneOwnedIdsPreserved;
            public string qualifiedOwnedMountainWorldEvidence,qualifiedOwnedMountainWorldTestsSha256;public bool qualifiedOwnedMountainDeltaApplied;public AppliedMode[] modes;public SavedFile[] savedFiles,requiredNavigationDependencies,requiredOuterDependencies;
            public string retainedWorldManifest,retainedWorldManifestSha256,mountedCourseReceipt,mountedCourseReceiptSha256;
            public string diggerDataFolder;public SavedFile[] requiredDiggerDependencies;public bool exactRetainedDiggerDependenciesPresent;
            public string retainedOuterManifest,retainedOuterManifestSha256;public bool exactRetainedOuterDependenciesPresent;
        }
        [Serializable]public sealed class AppliedMode
        {public string scene,externalStateBefore,externalStateAfter;public int originalSceneOwnedIdentities,trucks;public bool sceneOwnedIdentitiesPreserved,originalWorldOverridesPreserved;public DragonKingdomAmbienceAuthoring.RegroundEvidence ambience;}
        [Serializable]public sealed class SavedFile{public string path,sha256;}
        [Serializable]sealed class RetainedFiles{public SavedFile[] files;}
        sealed class ModeBefore{public string external,overrides;public Dictionary<string,string> ownedIds;public int trucks;}
        public static WorldApplyReceipt LastApplyReceipt{get;private set;}
        public static IEnumerator ApplyAcceptedCandidate()
        {
            string tag=Arg("-tsDragonWorldCandidate","");string visual=Arg("-tsDragonVisualAcceptance","");
            if(string.IsNullOrEmpty(tag)||string.IsNullOrEmpty(visual))throw new InvalidOperationException("Supply the exact physically passed candidate and actual lead-reviewed18-image acceptance receipt. No extra user approval or generation is performed.");
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");
            string output=Arg("-tsDragonOutput","Logs/TruckSports/DragonKingdom/WorldAdoption"),report=Path.Combine(output,"canonical-world-adoption-"+tag+".json");Directory.CreateDirectory(ApplyIo(output));
            ValidateVisualAcceptance(tag,visual);
            string retainedManifest=Arg("-tsDragonRetainedWorldManifest","");var diggerDependencies=ValidateRetainedDependencies(tag,retainedManifest);
            string folder=Root+"VisualCandidates/"+tag;var physical=JsonUtility.FromJson<DragonKingdomRegionalWorldIntegration.Receipt>(File.ReadAllText(ApplyIo(folder+"/Integration Receipt.json")));
            string outerManifest=Arg("-tsDragonRetainedOuterManifest","");var outerDependencies=ValidateRetainedOuterDependencies(folder,physical.outerBand,outerManifest,physical.palette);
            string courseReceipt=Arg("-tsDragonMountedCourseReceipt","");
            if(File.Exists(ApplyIo(report)))
            {
                var prior=JsonUtility.FromJson<WorldApplyReceipt>(File.ReadAllText(ApplyIo(report)));
                if(prior.status=="ADOPTED_ORIGINAL_IDENTITIES_AND_MODES_PRESERVED_REQUIRES_ACTUAL_PLAY_ACCEPTANCE"&&prior.mountedCourseReceiptSha256==Digest(courseReceipt)&&prior.savedFiles.All(f=>Digest(f.path)==f.sha256)&&prior.requiredNavigationDependencies!=null&&prior.requiredNavigationDependencies.All(f=>Digest(f.path)==f.sha256)){LastApplyReceipt=prior;yield break;}
                throw new InvalidOperationException("Preserve previous adoption evidence; current assets differ. Do not overwrite a reviewed result or later scene edits.");
            }
            ValidateMountedCourseEvidence(tag,courseReceipt);
            var plan=Build(tag);var candidate=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/Shared Dragon Kingdom.prefab").GetComponent<DragonKingdomWorld>();
            var paths=new[]{CanonicalPrefab,CanonicalPreview,CanonicalDefinition}.Concat(ModeNames.Select(n=>"Assets/"+n+"/Scenes/"+n+".unity")).ToArray();
            foreach(string path in paths)if(!File.Exists(ApplyIo(path)))throw new InvalidOperationException("All original saved world and four actual scenes are required: "+path);
            for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(s.isDirty||string.IsNullOrEmpty(s.path))throw new InvalidOperationException("Save existing user scenes before explicit adoption.");}
            var originalFiles=paths.ToDictionary(p=>p,p=>File.ReadAllBytes(ApplyIo(p)));var setup=EditorSceneManager.GetSceneManagerSetup();
            var protectedFiles=new Dictionary<string,string>();
            foreach(string directory in new[]{Root+"Terrain",Root+"FunctionalShoulders",Root+"Models",Root+"Materials",folder,"Assets/DiggerData/Scenes/DK-"+tag})
                foreach(string path in ExistingFolderFiles(directory))protectedFiles[path]=Digest(path);
            foreach(string path in paths.Select(p=>p+".meta"))protectedFiles[path]=Digest(path);
            foreach(var dependency in outerDependencies)protectedFiles[dependency.path]=dependency.sha256;
            var receipt=new WorldApplyReceipt{candidate=tag,visualAcceptance=visual,prefabGuid=AssetDatabase.AssetPathToGUID(CanonicalPrefab),previewGuid=AssetDatabase.AssetPathToGUID(CanonicalPreview),definitionGuid=AssetDatabase.AssetPathToGUID(CanonicalDefinition),retainedWorldManifest=retainedManifest,retainedWorldManifestSha256=Digest(retainedManifest),mountedCourseReceipt=courseReceipt,mountedCourseReceiptSha256=Digest(courseReceipt),diggerDataFolder="Assets/DiggerData/Scenes/DK-"+tag,requiredDiggerDependencies=diggerDependencies,exactRetainedDiggerDependenciesPresent=true,retainedOuterManifest=outerManifest,retainedOuterManifestSha256=outerDependencies.Length==0?null:Digest(outerManifest),requiredOuterDependencies=outerDependencies,exactRetainedOuterDependenciesPresent=true};LastApplyReceipt=receipt;
            receipt.backupFolder=Path.Combine(output,"before-canonical-"+tag+"-"+Guid.NewGuid().ToString("N"));
            foreach(var pair in originalFiles){string destination=Path.Combine(receipt.backupFolder,pair.Key);Directory.CreateDirectory(ApplyIo(Path.GetDirectoryName(destination)));File.WriteAllBytes(ApplyIo(destination),pair.Value);}
            // Preserve the exact retained native chunks and source bytes too.
            // A vendor load callback must never turn detection into data loss.
            foreach(string source in protectedFiles.Keys){string destination=Path.Combine(receipt.backupFolder,"protected",source);Directory.CreateDirectory(ApplyIo(Path.GetDirectoryName(destination)));File.Copy(ApplyIo(source),ApplyIo(destination),false);}
            File.WriteAllText(ApplyIo(Path.Combine(receipt.backupFolder,"required-native-dependencies.json")),JsonUtility.ToJson(new RetainedFiles{files=diggerDependencies},true));
            GameObject contents=null;bool mutated=false,complete=false;var modes=new List<AppliedMode>();
            try
            {
                var beforeModes=new Dictionary<string,ModeBefore>();foreach(string path in paths.Skip(3))
                {var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);beforeModes[path]=CaptureModeState(scene);yield return null;}
                var original=AssetDatabase.LoadAssetAtPath<GameObject>(CanonicalPrefab);var ids=ObjectIdentities(original.transform);var originalMap=Map(original.GetComponent<DragonKingdomWorld>(),candidate,physical.placements,physical);var obsolete=originalMap.obsolete.SelectMany(Objects).Select(o=>ObjectKey(o,original.transform)).ToArray();
                contents=PrefabUtility.LoadPrefabContents(CanonicalPrefab);mutated=true;ApplyWorldContents(contents.GetComponent<DragonKingdomWorld>(),candidate,physical);PrefabUtility.SaveAsPrefabAsset(contents,CanonicalPrefab);PrefabUtility.UnloadPrefabContents(contents);contents=null;AssetDatabase.ImportAsset(CanonicalPrefab,ImportAssetOptions.ForceUpdate);
                receipt.prefabIdentitiesPreserved=RetainedIdentities(ids,ObjectIdentities(AssetDatabase.LoadAssetAtPath<GameObject>(CanonicalPrefab).transform),obsolete);receipt.retainedPrefabIdentities=ids.Count-obsolete.Length;
                if(!receipt.prefabIdentitiesPreserved)throw new InvalidOperationException("Original nonobsolete prefab local identities changed.");
                var definition=AssetDatabase.LoadAssetAtPath<DragonKingdomDefinition>(CanonicalDefinition);string rules=DefinitionRules(definition);
                if(physical.ownedMountainRoad!=null){ApplyQualifiedOwnedMountainDefinition(definition,candidate.definition,physical);receipt.qualifiedOwnedMountainDeltaApplied=true;receipt.qualifiedOwnedMountainWorldEvidence=Arg("-tsDragonOwnedMountainWorldEvidence","");receipt.qualifiedOwnedMountainWorldTestsSha256=Digest(Path.Combine(receipt.qualifiedOwnedMountainWorldEvidence,"tests.xml"));rules=DefinitionRules(definition);}
                foreach(var region in definition.regions)region.bounds=candidate.definition.FindRegion(region.id).bounds;
                EditorUtility.SetDirty(definition);AssetDatabase.SaveAssetIfDirty(definition);receipt.definitionRulesPreserved=rules==DefinitionRules(definition);
                if(!receipt.definitionRulesPreserved)throw new InvalidOperationException("World adoption changed original graph/extractions/rules rather than region presentation bounds.");
                var preview=EditorSceneManager.OpenScene(CanonicalPreview,OpenSceneMode.Single);var world=FindWorld(preview);var previewIds=ObjectIdentities(world.transform);var sceneMap=Map(world,candidate,physical.placements,physical,true);var sceneObsolete=sceneMap.obsolete.SelectMany(Objects).Select(o=>ObjectKey(o,world.transform)).ToArray();
                ApplyWorldContents(world,candidate,physical);ValidateAdoptedWorld(world);EditorSceneManager.MarkSceneDirty(preview);if(!EditorSceneManager.SaveScene(preview))throw new InvalidOperationException("Original preview could not be saved.");
                receipt.previewIdentitiesPreserved=RetainedIdentities(previewIds,ObjectIdentities(world.transform),sceneObsolete);receipt.retainedPreviewIdentities=previewIds.Count-sceneObsolete.Length;if(!receipt.previewIdentitiesPreserved)throw new InvalidOperationException("Original nonobsolete preview identities changed.");receipt.physicalGatesPassed=true;yield return null;
                foreach(string path in paths.Skip(3))
                {
                    var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);world=FindWorld(scene);world.ValidateReferences();
                    var before=beforeModes[path];var row=new AppliedMode{scene=path,externalStateBefore=before.external,originalSceneOwnedIdentities=before.ownedIds.Count,trucks=before.trucks};modes.Add(row);
                    foreach(var terrain in world.GetComponentsInChildren<Terrain>(true))
                    {var expected=candidate.GetComponentsInChildren<Terrain>(true).Single(t=>t.name==terrain.name);if(terrain.terrainData!=expected.terrainData||terrain.GetComponent<TerrainCollider>().terrainData!=expected.terrainData)throw new InvalidOperationException("Existing mode contains a stale terrain override: "+path+"/"+terrain.name);}
                    row.ambience=DragonKingdomAmbienceAuthoring.RegroundExistingForChangedWorld(world);ValidateAdoptedWorld(world);
                    EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Existing mode habitat changes could not save.");
                    var after=CaptureModeState(scene);row.externalStateAfter=after.external;row.sceneOwnedIdentitiesPreserved=RetainedIdentities(before.ownedIds,after.ownedIds,Array.Empty<string>());row.originalWorldOverridesPreserved=before.overrides==after.overrides;
                    if(row.externalStateAfter!=row.externalStateBefore||!row.sceneOwnedIdentitiesPreserved||!row.originalWorldOverridesPreserved||!row.ambience.allExistingObjectsAndRouteReferencesPreserved)throw new InvalidOperationException("Original scene controls/session/cameras/NWH/overrides or ambient identities changed: "+path);
                    yield return null;
                }
                receipt.originalGuidsPreserved=AssetDatabase.AssetPathToGUID(CanonicalPrefab)==receipt.prefabGuid&&AssetDatabase.AssetPathToGUID(CanonicalPreview)==receipt.previewGuid&&AssetDatabase.AssetPathToGUID(CanonicalDefinition)==receipt.definitionGuid;
                receipt.originalSourceBytesPreserved=protectedFiles.All(p=>Digest(p.Key)==p.Value);receipt.allModeExternalStateAndSceneOwnedIdsPreserved=modes.Count==4&&modes.All(m=>m.externalStateBefore==m.externalStateAfter&&m.sceneOwnedIdentitiesPreserved&&m.originalWorldOverridesPreserved);
                if(!receipt.originalGuidsPreserved||!receipt.originalSourceBytesPreserved||!receipt.allModeExternalStateAndSceneOwnedIdsPreserved)throw new InvalidOperationException("Canonical adoption violated original GUID/source/mode preservation.");
                receipt.modes=modes.ToArray();receipt.savedFiles=paths.Select(p=>new SavedFile{path=p,sha256=Digest(p)}).ToArray();receipt.requiredNavigationDependencies=modes.SelectMany(m=>m.ambience.navigation).Distinct().SelectMany(p=>new[]{p,p+".meta"}).Select(p=>new SavedFile{path=p,sha256=Digest(p)}).ToArray();receipt.status="ADOPTED_ORIGINAL_IDENTITIES_AND_MODES_PRESERVED_REQUIRES_ACTUAL_PLAY_ACCEPTANCE";complete=true;
            }
            finally
            {
                if(contents!=null)PrefabUtility.UnloadPrefabContents(contents);
                if(!complete&&mutated)
                {EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);foreach(var pair in originalFiles){File.WriteAllBytes(ApplyIo(pair.Key),pair.Value);AssetDatabase.ImportAsset(pair.Key,ImportAssetOptions.ForceUpdate);}foreach(var pair in protectedFiles)if(!File.Exists(ApplyIo(pair.Key))||Digest(pair.Key)!=pair.Value){File.Copy(ApplyIo(Path.Combine(receipt.backupFolder,"protected",pair.Key)),ApplyIo(pair.Key),true);if(!pair.Key.EndsWith(".meta",StringComparison.Ordinal)&&pair.Key.StartsWith("Assets/",StringComparison.Ordinal))AssetDatabase.ImportAsset(pair.Key,ImportAssetOptions.ForceUpdate);}receipt.status="ROLLED_BACK_EXACT_ORIGINAL_SEVEN_ASSETS_AND_PROTECTED_DEPENDENCIES";}
                EditorSceneManager.RestoreSceneManagerSetup(setup);
                receipt.modes=modes.ToArray();File.WriteAllText(ApplyIo(report),JsonUtility.ToJson(receipt,true));
            }
        }
        static void ApplyWorldContents(DragonKingdomWorld original,DragonKingdomWorld candidate,DragonKingdomRegionalWorldIntegration.Receipt proof)
        {
            var mapping=Map(original,candidate,proof.placements,proof,true);var originalDefinition=AssetDatabase.LoadAssetAtPath<DragonKingdomDefinition>(CanonicalDefinition);mapping.candidateToOriginal.Add(candidate.definition,originalDefinition);
            var added=candidate.GetComponentsInChildren<Transform>(true).Where(t=>!mapping.candidateToOriginal.ContainsKey(t)&&t.parent!=null&&mapping.candidateToOriginal.ContainsKey(t.parent)).ToArray();bool active=original.gameObject.activeSelf;original.gameObject.SetActive(false);
            var newComponents=new HashSet<Component>();
            try
            {
                foreach(var root in added)
                {var clone=UnityEngine.Object.Instantiate(root.gameObject,(Transform)mapping.candidateToOriginal[root.parent]);clone.name=root.name;PairTree(clone.transform,root,mapping);foreach(var component in clone.GetComponentsInChildren<Component>(true))newComponents.Add(component);}
                foreach(var pair in mapping.candidateToOriginal.ToArray())
                {
                    if(pair.Key is Transform source&&pair.Value is Transform target){target.SetLocalPositionAndRotation(source.localPosition,source.localRotation);target.localScale=source.localScale;continue;}
                    if(!(pair.Key is Component component)||!(pair.Value is Component destination))continue;
                    // Existing native/vendor components keep their private
                    // state. Copy only the explicitly reviewed visual/data
                    // fields; full new subtrees already came from Instantiate.
                    if(!newComponents.Contains(destination))CopyReviewedExistingFields(component,destination,mapping,proof);
                    var references=new SerializedObject(destination);var property=references.GetIterator();bool enter=true;
                    while(property.Next(enter))
                    {enter=true;if(property.propertyType!=SerializedPropertyType.ObjectReference||property.objectReferenceValue==null||Bookkeeping(property.name))continue;if(mapping.candidateToOriginal.TryGetValue(property.objectReferenceValue,out var mapped))property.objectReferenceValue=mapped;else if(AssetDatabase.GetAssetPath(property.objectReferenceValue)==AssetDatabase.GetAssetPath(candidate.gameObject))throw new InvalidOperationException("Unmapped candidate internal reference: "+destination.name+"/"+property.propertyPath);}
                    references.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(destination);
                }
                foreach(var retirement in mapping.mountainRetirements)retirement.Value.gameObject.SetActive(false);
                foreach(var obsolete in mapping.obsolete)UnityEngine.Object.DestroyImmediate(obsolete.gameObject);
                original.definition=originalDefinition;EditorUtility.SetDirty(original);
            }
            finally{original.gameObject.SetActive(active);}
        }
        static void CopyReviewedExistingFields(Component source,Component target,Mapping mapping,DragonKingdomRegionalWorldIntegration.Receipt proof)
        {
            if(source is Terrain terrain&&target is Terrain toTerrain){toTerrain.terrainData=terrain.terrainData;toTerrain.materialTemplate=terrain.materialTemplate;toTerrain.allowAutoConnect=terrain.allowAutoConnect;toTerrain.groupingID=terrain.groupingID;return;}
            if(source is TerrainCollider collider&&target is TerrainCollider toCollider){toCollider.terrainData=collider.terrainData;return;}
            if(IsSchema4(proof)&&source is Collider physical&&target is Collider toPhysical)
            {if(physical.enabled!=toPhysical.enabled){if(!mapping.retiredComponents.TryGetValue(physical,out var authorized)||authorized!=toPhysical||physical.enabled)throw new InvalidOperationException("Unreviewed existing collider enabled-state change: "+physical.name);toPhysical.enabled=false;}return;}
            if(source is MeshFilter filter&&target is MeshFilter toFilter){toFilter.sharedMesh=filter.sharedMesh;return;}
            if(source is Renderer renderer&&target is Renderer toRenderer)
            {toRenderer.sharedMaterials=renderer.sharedMaterials;if(renderer.enabled!=toRenderer.enabled){if((!mapping.retiredComponents.TryGetValue(renderer,out var authorized)||authorized!=toRenderer||renderer.enabled)&&(renderer.name!="Lanternwater Lake"||proof.outerBand==null||!proof.outerBand.duplicateLakeRenderingReplaced||renderer.enabled))throw new InvalidOperationException("Unreviewed existing renderer visibility change: "+renderer.name);toRenderer.enabled=false;}return;}
            if(source is LODGroup group&&target is LODGroup toGroup)
            {
                var lods=group.GetLODs();for(int i=0;i<lods.Length;i++)lods[i].renderers=lods[i].renderers.Select(r=>(Renderer)mapping.candidateToOriginal[r]).ToArray();
                toGroup.SetLODs(lods);toGroup.localReferencePoint=group.localReferencePoint;toGroup.size=group.size;return;
            }
            if(source is DragonKingdomWorld)
            {
                var from=new SerializedObject(source);var to=new SerializedObject(target);var property=from.GetIterator();bool enter=true;
                while(property.Next(enter)){enter=false;if(property.depth!=0||Bookkeeping(property.name))continue;to.CopyFromSerializedProperty(property);}to.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        static bool Bookkeeping(string property)=>new[]{"m_ObjectHideFlags","m_CorrespondingSourceObject","m_PrefabInstance","m_PrefabAsset","m_GameObject","m_Script","m_EditorClassIdentifier"}.Contains(property);
        static void ValidateAdoptedWorld(DragonKingdomWorld world)
        {
            DragonKingdomRegionalWorldIntegration.LinkActualTerrainNeighbors(world.GetComponentsInChildren<Terrain>(true));
            Physics.SyncTransforms();world.ValidateReferences();DragonKingdomWorldAuthoring.ValidatePhysicalRoads(world.transform,world.definition.graph);DragonKingdomWorldAuthoring.ValidateTruckStarts(world,false);
            var sweep=new DragonKingdomRegionalWorldIntegration.Receipt();DragonKingdomRegionalWorldIntegration.ValidateContinuousExtractionApproaches(world,sweep);ValidateCavern(world);
        }
        static void ValidateCavern(DragonKingdomWorld world)
        {
            var chunks=new HashSet<Collider>(world.GetComponentsInChildren<MeshCollider>(true).Where(c=>c.enabled&&!c.isTrigger&&Ancestor(c.transform,"Actual vendor cavern Digger")));
            if(chunks.Count==0)throw new InvalidOperationException("Original world lacks its actual retained Digger mesh collision.");
            for(int z=-50;z<=50;z+=10)for(int x=-80;x<=80;x+=10)
            {var p=new Vector3(520+x,106.5f,780+z);var down=Physics.RaycastAll(p,Vector3.down,5,~0,QueryTriggerInteraction.Ignore).Where(h=>chunks.Contains(h.collider)).OrderBy(h=>h.distance).ToArray();var up=Physics.RaycastAll(p,Vector3.up,110,~0,QueryTriggerInteraction.Ignore).Where(h=>chunks.Contains(h.collider)).OrderBy(h=>h.distance).ToArray();if(down.Length==0||up.Length==0||Mathf.Abs(down[0].point.y-105)>.3f||up[0].point.y-down[0].point.y<42)throw new InvalidOperationException("Saved adopted actual cavern floor/roof failed at "+p);}
        }
        static bool Ancestor(Transform t,string name){while(t!=null){if(t.name==name)return true;t=t.parent;}return false;}
        static DragonKingdomWorld FindWorld(Scene scene)=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();
        static ModeBefore CaptureModeState(Scene scene)
        {
            var world=FindWorld(scene);var objects=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();var sessions=objects.SelectMany(t=>t.GetComponents<SportsEventSession>()).ToArray();if(sessions.Length!=1)throw new InvalidOperationException("One original actual event session required.");
            var trucks=sessions[0].GetType().GetField("trucks").GetValue(sessions[0]) as SportsEventTruck[];if(trucks==null||trucks.Length!=4||trucks.Any(t=>t==null||t.tractorMotor==null||t.tractorMotor.vehicle==null))throw new InvalidOperationException("Four original NWH trucks must remain.");
            var protectedObjects=objects.Where(t=>!t.IsChildOf(world.transform)&&t!=world.transform).SelectMany(ObjectsShallow).Concat(trucks.SelectMany(t=>Objects(t.transform))).Distinct().ToArray();
            string external=DigestText(string.Join("\n",protectedObjects.Select(o=>Identity(o)+"|"+StableJson(o)).OrderBy(s=>s,StringComparer.Ordinal)));
            var instance=PrefabUtility.GetOutermostPrefabInstanceRoot(world.gameObject);if(instance==null)throw new InvalidOperationException("Every mode must retain its original shared-world prefab instance.");
            string overrides=DigestText(string.Join("\n",(PrefabUtility.GetPropertyModifications(instance)??Array.Empty<PropertyModification>()).Select(m=>Identity(m.target)+"|"+m.propertyPath+"|"+m.value+"|"+(m.objectReference==null?"NULL":Identity(m.objectReference))).OrderBy(s=>s,StringComparer.Ordinal)));
            var owned=objects.SelectMany(ObjectsShallow).Where(o=>PrefabUtility.GetCorrespondingObjectFromSource(o)==null).ToDictionary(o=>Identity(o),o=>o.GetType().FullName);
            return new ModeBefore{external=external,overrides=overrides,ownedIds=owned,trucks=trucks.Length};
        }
        static IEnumerable<UnityEngine.Object> ObjectsShallow(Transform t){yield return t.gameObject;foreach(var c in t.GetComponents<Component>())yield return c;}
        static string StableJson(UnityEngine.Object obj)=>Regex.Replace(EditorJsonUtility.ToJson(obj),@"""instanceID""\s*:\s*(-?\d+)",m=>{var referenced=EditorUtility.InstanceIDToObject(int.Parse(m.Groups[1].Value));return "\"stableObject\":\""+(referenced==null?"NULL":Identity(referenced))+"\"";});
        static Dictionary<string,string> ObjectIdentities(Transform root)=>Objects(root).ToDictionary(o=>ObjectKey(o,root),Identity);
        static string ObjectKey(UnityEngine.Object obj,Transform root)
        {if(obj is GameObject go)return PathOf(go.transform,root)+"|GO";var component=(Component)obj;return PathOf(component.transform,root)+"|"+Array.IndexOf(component.GetComponents<Component>(),component);}
        static bool RetainedIdentities(Dictionary<string,string> before,Dictionary<string,string> after,IEnumerable<string> obsolete)
        {var removed=new HashSet<string>(obsolete);return before.Where(p=>!removed.Contains(p.Key)).All(p=>after.TryGetValue(p.Key,out var value)&&value==p.Value);}
        static string DefinitionRules(DragonKingdomDefinition definition)
        {var clone=UnityEngine.Object.Instantiate(definition);try{foreach(var r in clone.regions)r.bounds=new Bounds();return EditorJsonUtility.ToJson(clone);}finally{UnityEngine.Object.DestroyImmediate(clone);}}
        static void ValidateVisualAcceptance(string tag,string path)
        {
            if(!File.Exists(ApplyIo(path)))throw new InvalidOperationException("Actual18-image lead review acceptance is required before changing canonical assets.");
            var review=JsonUtility.FromJson<VisualAcceptance>(File.ReadAllText(ApplyIo(path)));string folder=Root+"VisualCandidates/"+tag;
            if(review==null||!review.acceptedForAdoption||review.candidate!=tag||review.reviewedImages==null||review.reviewedImages.Length!=18||review.prefabSha256!=Digest(folder+"/Shared Dragon Kingdom.prefab")||review.previewSha256!=Digest(folder+"/DragonKingdomPreview.unity")||review.renderReceiptSha256!=Digest(review.renderReceipt)||review.reviewedImages.Any(i=>i.sha256!=Digest(i.path)))throw new InvalidOperationException("Visual review must match the exact actual candidate/render/images and accept all18real views. Human verification remains a separate recorded field.");
            var rendered=JsonUtility.FromJson<RenderAcceptanceHeader>(File.ReadAllText(ApplyIo(review.renderReceipt)));
            if(rendered.prefab!=folder+"/Shared Dragon Kingdom.prefab"||rendered.preview!=folder+"/DragonKingdomPreview.unity"||rendered.regions.Length!=9||rendered.regions.Select(r=>r.id).Distinct().Count()!=9||rendered.width!=1280||rendered.height!=720||rendered.regions.Any(r=>r.eye.nonBackgroundPixels<=0||r.overview.nonBackgroundPixels<=0))throw new InvalidOperationException("Actual complete nine-region native rendered evidence required.");
            var actualImages=new HashSet<string>(rendered.regions.SelectMany(r=>new[]{Path.GetFullPath(r.eye.png),Path.GetFullPath(r.overview.png)}),StringComparer.OrdinalIgnoreCase);
            if(actualImages.Count!=18||!actualImages.SetEquals(review.reviewedImages.Select(i=>Path.GetFullPath(i.path))))throw new InvalidOperationException("Reviewed image set differs from the actual complete native render receipt.");
        }
        static SavedFile[] ValidateRetainedDependencies(string tag,string manifest)
        {
            if(string.IsNullOrEmpty(manifest)||!File.Exists(ApplyIo(manifest)))throw new InvalidOperationException("Supply -tsDragonRetainedWorldManifest with the actual guarded candidate/Digger retention manifest; persistent CLEAN cache is insufficient provenance.");
            var rows=JsonUtility.FromJson<RetainedFiles>("{\"files\":"+File.ReadAllText(ApplyIo(manifest))+"}").files;
            string digger="Assets/DiggerData/Scenes/DK-"+tag,folder=Root+"VisualCandidates/"+tag;
            var expected=new HashSet<string>(ExistingFolderFiles(digger).Concat(ExistingFolderFiles(folder)),StringComparer.OrdinalIgnoreCase);
            var retained=rows.Where(r=>r.path.StartsWith(digger+"/",StringComparison.Ordinal)||r.path==digger+".meta"||r.path.StartsWith(folder+"/",StringComparison.Ordinal)||r.path==folder+".meta").ToArray();
            // The JSON receipt is written after the last native asset import.
            // A later Editor creates its unreferenced DefaultImporter meta.
            // It is evidence metadata, never a runtime/chunk dependency. Every
            // actual saved asset and ALL Digger persistence remain exact.
            string receiptMeta=folder+"/Integration Receipt.json.meta";
            if(!retained.Any(r=>r.path==receiptMeta))expected.Remove(receiptMeta);
            if(!Directory.Exists(digger)||retained.Length==0||retained.Select(r=>r.path).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=retained.Length||!expected.SetEquals(retained.Select(r=>r.path))||retained.Any(r=>!File.Exists(ApplyIo(r.path))||Digest(r.path)!=r.sha256))throw new InvalidOperationException("Exact candidate/native Digger dependency closure is missing, stale, duplicated, or differs from its retained passed output.");
            var serializedDigger=AssetDatabase.GetDependencies(folder+"/Shared Dragon Kingdom.prefab",true).Where(p=>p.StartsWith("Assets/DiggerData/",StringComparison.Ordinal)).ToArray();
            if(serializedDigger.Length==0||serializedDigger.Any(p=>!p.StartsWith(digger+"/",StringComparison.Ordinal)||!expected.Contains(p)))throw new InvalidOperationException("Candidate references missing native chunks or a different Digger scene folder.");
            return retained.Where(r=>r.path.StartsWith(digger+"/",StringComparison.Ordinal)||r.path==digger+".meta").OrderBy(r=>r.path,StringComparer.Ordinal).ToArray();
        }
        static IEnumerable<string> ExistingFolderFiles(string directory)
        {if(Directory.Exists(directory))foreach(string path in Directory.GetFiles(directory,"*",SearchOption.AllDirectories))yield return path.Replace('\\','/');if(File.Exists(ApplyIo(directory+".meta")))yield return directory+".meta";}
        static SavedFile[] ValidateRetainedOuterDependencies(string candidateFolder,DragonKingdomRegionalWorldIntegration.CoastalBand coast,string manifest,DragonKingdomRegionalWorldIntegration.AuthoredTerrainPalette palette=null)
        {
            if(coast==null||coast.outerTerrains==0)return Array.Empty<SavedFile>();
            if(string.IsNullOrEmpty(manifest)||!File.Exists(ApplyIo(manifest)))throw new InvalidOperationException("Supply -tsDragonRetainedOuterManifest with the actual guarded passed outer-Vista retention array; cached tiles alone are insufficient provenance.");
            string folder=Root+"ReviewCandidates/"+coast.tag;
            var rows=JsonUtility.FromJson<RetainedFiles>("{\"files\":"+File.ReadAllText(ApplyIo(manifest))+"}").files.Where(r=>r.path.StartsWith(folder+"/",StringComparison.Ordinal)||r.path==folder+".meta").ToArray();
            var expected=new HashSet<string>(ExistingFolderFiles(folder),StringComparer.OrdinalIgnoreCase);string receiptMeta=folder+"/Outer Vista Receipt.json.meta";if(!rows.Any(r=>r.path==receiptMeta))expected.Remove(receiptMeta);
            if(rows.Length==0||rows.Select(r=>r.path).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=rows.Length||!expected.SetEquals(rows.Select(r=>r.path))||rows.Any(r=>!File.Exists(ApplyIo(r.path))||Digest(r.path)!=r.sha256))throw new InvalidOperationException("Actual outer-Vista runtime folder differs from its exact passed retained files.");
            var assets=new HashSet<string>(AssetDatabase.GetDependencies(candidateFolder+"/Shared Dragon Kingdom.prefab",true).Where(p=>p.StartsWith(folder+"/Terrain/",StringComparison.Ordinal)));
            if(palette==null)
            {
                // Legacy candidates still bind the exact retained original12.
                if(assets.Count!=12||assets.Any(p=>!expected.Contains(p)||!expected.Contains(p+".meta")))throw new InvalidOperationException("Saved candidate must resolve exactly twelve retained actual outer TerrainData assets and their original GUID metadata.");
            }
            else
            {
                // Keep the original outer closure exact. The current palette
                // owns twelve copies already covered by the candidate manifest;
                // its existing native saved validator owns all geometry checks.
                var candidate=AssetDatabase.LoadAssetAtPath<GameObject>(candidateFolder+"/Shared Dragon Kingdom.prefab");
                if(candidate==null||palette.tiles==null)throw new InvalidOperationException("Actual saved candidate/palette required for cloned outer provenance.");
                DragonKingdomRegionalWorldIntegration.ValidateSavedAuthoredPalette(candidate.GetComponent<DragonKingdomWorld>(),palette);
                var clones=palette.tiles.Where(t=>t.externalDataCloned).ToArray();
                var dependencies=new HashSet<string>(AssetDatabase.GetDependencies(candidateFolder+"/Shared Dragon Kingdom.prefab",true).Where(p=>p.StartsWith(candidateFolder+"/Terrain/Palette/",StringComparison.Ordinal)&&p.EndsWith(".asset",StringComparison.Ordinal)),StringComparer.Ordinal);
                if(clones.Length!=12||clones.Select(t=>t.sourceData).Distinct(StringComparer.Ordinal).Count()!=12||clones.Select(t=>t.candidate).Distinct(StringComparer.Ordinal).Count()!=12||assets.Count!=0||!dependencies.SetEquals(clones.Select(t=>t.candidate)))throw new InvalidOperationException("Exactly twelve distinct original-source to candidate-owned outer TerrainData bindings required; no stale direct source tiles.");
                foreach(var tile in clones)
                    if(!tile.sourceData.StartsWith(folder+"/Terrain/",StringComparison.Ordinal)||!expected.Contains(tile.sourceData)||!expected.Contains(tile.sourceData+".meta")||Digest(tile.sourceData)!=tile.sourceDataSha256||Digest(tile.sourceData+".meta")!=tile.sourceDataMetaSha256||AssetDatabase.AssetPathToGUID(tile.sourceData)!=tile.sourceDataGuid||!tile.candidate.StartsWith(candidateFolder+"/Terrain/Palette/",StringComparison.Ordinal)||Digest(tile.candidate)!=tile.candidateSha256||Digest(tile.candidate+".meta")!=tile.candidateMetaSha256||AssetDatabase.AssetPathToGUID(tile.candidate)!=tile.candidateGuid||tile.candidateGuid==tile.sourceDataGuid)throw new InvalidOperationException("Actual retained outer source and exact owned palette clone identity differ.");
            }
            return rows.OrderBy(r=>r.path,StringComparer.Ordinal).ToArray();
        }
        static void ValidateMountedCourseEvidence(string tag,string path)
        {
            if(string.IsNullOrEmpty(path)||!File.Exists(ApplyIo(path)))throw new InvalidOperationException("Actual mounted-course review is required before adopting terrain around the flight course.");
            var proof=JsonUtility.FromJson<DragonKingdomMountedCourseReview.Receipt>(File.ReadAllText(ApplyIo(path)));
            if(proof.candidate!=tag||proof.status!="SAMPLED_FULL_MOUNT_ENVELOPE_CLEAR_REQUIRES_ACTUAL_FLIGHT"||!proof.sourceBytesPreserved||!proof.coordinatedCourse||proof.contactQueries!=0||proof.apertureFailures!=0||proof.queries==0||proof.gateCount!=12||proof.rigs==null||proof.rigs.Length!=8||proof.rigs.Any(r=>r.poses<170||r.configuredGraphPoses==0||r.nativeMixerPoses==0)||proof.courseLayout==null||proof.courseLayout.Length!=12)throw new InvalidOperationException("Actual eight-rig coordinated mounted-course geometry including configured controller/mixer poses has not passed. Actual flight remains a separate acceptance gate.");
            string folder=Root+"VisualCandidates/"+tag;
            if(proof.worldPrefabSha256!=Digest(folder+"/Shared Dragon Kingdom.prefab")||proof.worldPreviewSha256!=Digest(folder+"/DragonKingdomPreview.unity")||proof.ridersSceneSha256!=Digest("Assets/TruckDragonRiders/Scenes/TruckDragonRiders.unity"))throw new InvalidOperationException("Mounted course evidence predates the current candidate or actual saved calibrated Riders scene. Repeat its read-only geometry measurement.");
            foreach(var rig in proof.rigs)if(rig.controllerSha256!=Digest(rig.controller)||rig.clips==null||rig.clipSha256==null||rig.clips.Length!=rig.clipSha256.Length||rig.clips.Where((clip,index)=>Digest(clip)!=rig.clipSha256[index]).Any())throw new InvalidOperationException("The actual mounted controller/effective animation changed after eight-rig course geometry measurement.");
        }
        static string ApplyIo(string path){string full=Path.GetFullPath(path);return Application.platform==RuntimePlatform.WindowsEditor&&full.Length>=248&&!full.StartsWith(@"\\?\",StringComparison.Ordinal)?@"\\?\"+full:full;}
        static string Digest(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(ApplyIo(path)))).Replace("-","").ToLowerInvariant();}
        static string DigestText(string text){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant();}
    }
}
#endif
