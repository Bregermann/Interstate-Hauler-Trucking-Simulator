#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckDragonKingdom;
using LWS.TruckDragonKingdom.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        [Serializable]sealed class NativeReplacementOverride
        {public string target,property,value,reference;}
        [Serializable]sealed class NativeReplacementReceipt
        {
            public string scope="DISPOSABLE_COPIED_ORIGINAL_SCENE_SINGLE_CREW0_INFERNO_NATIVE_PREFAB_API_EXPERIMENT_NOT_CANONICAL_EIGHT_ACTOR_FLIGHT_OR_WORLD_ADOPTION",status="IN_PROGRESS",originalScene,copyScene,productionEvidence,failure;
            public string targetBefore,targetAfter,targetReloaded,actorBefore,actorAfter,actorReloaded,externalBefore,externalAfter,worldBefore,worldAfter,qualifiedSignatureSha256;
            public bool nativeReplacementCalled,expectedCustomEditRejected,targetRootReferenceRetained,actorReferenceRetained,externalIdentitiesExact,externalConfigurationExact,worldExact,nwhExact,qualifiedProfileExact,savedReloaded,originalSourcesExact;
            public int protectedExternalObjects,propertyOverrides,addedObjects,addedComponents,removedObjects,removedComponents;public float maximumNwhDimensionDrift;
            public NativeReplacementOverride[] authorizedBaselineOverrides;public string[] authorizedAddedObjects,authorizedAddedComponents,authorizedRemovedObjects,authorizedRemovedComponents;
            public string overridePolicy="ONLY_EXACT_NATIVE_BASELINE_OLD_TARGET_OVERRIDES_ARE_CLEARED_AFTER_SOURCE_GUARD_ROOT_NAME_POSE_AND_ORIGINAL_TRUCK_BINDING_ARE_RETAINED";
            public FlightSourceHash[] originalSources;
        }
        static string NativeReplacementIdentity(UnityEngine.Object value)
        {
            var id=GlobalObjectId.GetGlobalObjectIdSlow(value);return id.identifierType+":"+id.targetObjectId+":"+id.targetPrefabId;
        }
        static Dictionary<string,string> NativeReplacementExternalIds(Scene scene,Transform excluded)
        {
            var result=new Dictionary<string,string>(StringComparer.Ordinal);foreach(var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t!=excluded&&!t.IsChildOf(excluded)))
            {
                result.Add(ProductionPath(t)+"|GameObject",NativeReplacementIdentity(t.gameObject));var components=t.GetComponents<Component>();for(int i=0;i<components.Length;i++){if(components[i]==null)throw new InvalidOperationException("Missing original component outside the known actor.");result.Add(ProductionPath(t)+"|"+i+":"+components[i].GetType().FullName,NativeReplacementIdentity(components[i]));}
            }
            return result;
        }
        static bool NativeReplacementIdsAgree(Dictionary<string,string> before,Dictionary<string,string> after)=>before.Count==after.Count&&before.All(pair=>after.TryGetValue(pair.Key,out var value)&&value==pair.Value);
        static string NativeReplacementWorldState(DragonKingdomWorld world)=>string.Join("\n",world.GetComponentsInChildren<Transform>(true).Select(t=>ProductionPath(t,world.transform)+"|"+NativeReplacementIdentity(t.gameObject)+"|"+t.gameObject.activeSelf+"|"+PristineFloatBits(t.localPosition.x,t.localPosition.y,t.localPosition.z,t.localRotation.x,t.localRotation.y,t.localRotation.z,t.localRotation.w,t.localScale.x,t.localScale.y,t.localScale.z)))+"\n"+string.Join("\n",world.GetComponentsInChildren<Component>(true).Where(c=>c!=null&&c is not Transform).Select(c=>ProductionComponentJson(c,world.transform)));

        public static string ProbeSingleKnownActorNativePrefabReplacement(string productionEvidence,string physicalCourseEvidence,string tag,bool injectUnknownActorEdit)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||string.IsNullOrEmpty(tag)||tag.Length>20||tag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new InvalidOperationException("Stopped compiled Editor and explicit short test-only tag required.");
            for(int i=0;i<SceneManager.sceneCount;i++){var incoming=SceneManager.GetSceneAt(i);if(incoming.isDirty||string.IsNullOrEmpty(incoming.path)||incoming.path==DragonRidersAuthoring.ScenePath)throw new InvalidOperationException("Preserve user scenes; save and close original Riders before the disposable native experiment.");}
            var production=ReadQualifiedCourseProduction(productionEvidence);int species=Array.IndexOf(production.species,"dragon-inferno");if(species<0)throw new InvalidOperationException("Actual qualified Inferno required for the first bounded experiment.");var mount=RequireQualifiedProductionMount(production.producerEvidence[species],production.runtimeEvidence[species]);RequireProductionSourceScene(production.sourceScene,mount.producerManifest,physicalCourseEvidence);
            var aliases=QualifiedCourseControllerAliases(mount.fixture.controller,production.controllers[species]);string expected;var fixtureContents=PrefabUtility.LoadPrefabContents(mount.fixture.prefab);try{expected=ProductionActorSignature(fixtureContents.GetComponent<DragonCharacter>(),aliases);}finally{PrefabUtility.UnloadPrefabContents(fixtureContents);}
            var qualifiedContents=PrefabUtility.LoadPrefabContents(production.actorPrefabs[species]);try{if(ProductionActorSignature(qualifiedContents.GetComponent<DragonCharacter>(),aliases)!=expected)throw new InvalidOperationException("Current production prefab no longer equals the actual qualified native fixture.");}finally{PrefabUtility.UnloadPrefabContents(qualifiedContents);}
            // Before any copied-scene override is cleared, prove that the
            // original still is the exact known native baseline. A future
            // post-world-adoption path needs a separately qualified delta.
            RequireRecordedStartupSourceHash(mount.producerManifest,DragonRidersAuthoring.ScenePath,"Original scene for single native prefab replacement");
            string folder="Assets/TruckDragonRiders/Tests/Generated/NativeReplace-"+tag+(injectUnknownActorEdit?"-reject":"-baseline"),copy=folder+"/Original Riders probe.unity";
            if(Directory.Exists(folder)||File.Exists(folder+".meta"))throw new InvalidOperationException("Preserve prior test evidence; output must be absent.");
            var watched=AssetDatabase.GetDependencies(DragonRidersAuthoring.ScenePath,true).Concat(AssetDatabase.GetDependencies(production.candidateScene,true)).Concat(new[]{DragonRidersAuthoring.ScenePath,production.candidateScene}).SelectMany(p=>new[]{p,p+".meta"}).Where(File.Exists).Distinct().ToArray();
            var receipt=new NativeReplacementReceipt{originalScene=DragonRidersAuthoring.ScenePath,copyScene=copy,productionEvidence=productionEvidence,originalSources=watched.Select(p=>new FlightSourceHash{path=p,sha256=Hash(p)}).ToArray()};var setup=EditorSceneManager.GetSceneManagerSetup();Scene canonical=default,scene=default;Exception failure=null;
            try
            {
                canonical=EditorSceneManager.OpenScene(DragonRidersAuthoring.ScenePath,OpenSceneMode.Additive);var original=CourseMatch(canonical);var old=original.racers[0];int choice=Array.IndexOf(old.choiceIds,"dragon-inferno");if(choice<0||old.slot!=0||old.match!=original||old.truck!=original.trucks[0])throw new InvalidOperationException("Only the exact original crew0/species/truck binding is authorized.");
                var baselineActor=old.choices[choice];if(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(baselineActor.gameObject)!="Assets/TruckDragonKingdom/Prefabs/Dragons/dragon-inferno/dragon-inferno Dragon Actor.prefab")throw new InvalidOperationException("The first experiment requires the actual known prefab instance, never an unexpected plain/custom actor.");RequireNoExternalMountReferences(canonical,baselineActor,old,choice);
                string originalActorSignature=ProductionActorSignature(baselineActor,new Dictionary<UnityEngine.Object,string>());var canonicalIds=NativeReplacementExternalIds(canonical,baselineActor.transform);var fixedNwh=CaptureProductionNwhFields(original);
                Directory.CreateDirectory(folder);AssetDatabase.Refresh();if(!AssetDatabase.CopyAsset(DragonRidersAuthoring.ScenePath,copy)||!File.ReadAllBytes(copy).SequenceEqual(File.ReadAllBytes(DragonRidersAuthoring.ScenePath)))throw new InvalidOperationException("The disposable source must begin as an exact original scene copy.");
                // Native Weather Maker instances release in OnDestroy, not
                // OnDisable. Retain only captured values while closing the
                // original, then load the disposable copy sequentially.
                if(!EditorSceneManager.CloseScene(canonical,true))throw new InvalidOperationException("Original comparison scene did not close before loading its disposable copy.");canonical=default;
                scene=EditorSceneManager.OpenScene(copy,OpenSceneMode.Additive);var match=CourseMatch(scene);var racer=match.racers[0];var actor=racer.choices[choice];var root=actor.gameObject;var truck=racer.truck;
                if(!NativeReplacementIdsAgree(canonicalIds,NativeReplacementExternalIds(scene,actor.transform)))throw new InvalidOperationException("Copying already changed original nontarget local identities.");
                var externalIds=NativeReplacementExternalIds(scene,actor.transform);receipt.protectedExternalObjects=externalIds.Count;receipt.targetBefore=NativeReplacementIdentity(root);receipt.actorBefore=NativeReplacementIdentity(actor);string external=ProductionSceneConfiguration(scene,new HashSet<GameObject>{root}),world=NativeReplacementWorldState(match.world);receipt.externalBefore=HashTextForNativeReplacement(external);receipt.worldBefore=HashTextForNativeReplacement(world);
                if(injectUnknownActorEdit)actor.motor.transform.localScale*=1.0001f;
                if(ProductionActorSignature(actor,new Dictionary<UnityEngine.Object,string>())!=originalActorSignature)
                {
                    if(!injectUnknownActorEdit)throw new InvalidOperationException("Unknown original actor customization; do not clear or replace it.");receipt.expectedCustomEditRejected=true;receipt.status="KNOWN_CUSTOM_EDIT_REJECTED_BEFORE_NATIVE_REPLACEMENT";receipt.externalIdentitiesExact=NativeReplacementIdsAgree(externalIds,NativeReplacementExternalIds(scene,actor.transform));receipt.externalConfigurationExact=external==ProductionSceneConfiguration(scene,new HashSet<GameObject>{root});receipt.worldExact=world==NativeReplacementWorldState(match.world);receipt.nwhExact=ExactProductionNwhFields(fixedNwh,CaptureProductionNwhFields(match));
                }
                else
                {
                    if(injectUnknownActorEdit)throw new InvalidOperationException("Controlled unknown-edit fault was not detected.");RequireNoExternalMountReferences(scene,actor,racer,choice);var pose=root.transform;var parent=pose.parent;int sibling=pose.GetSiblingIndex();string name=root.name;var position=pose.localPosition;var rotation=pose.localRotation;
                    receipt.authorizedBaselineOverrides=(PrefabUtility.GetPropertyModifications(root)??Array.Empty<PropertyModification>()).Select(p=>new NativeReplacementOverride{target=ProductionObjectToken(p.target,actor.transform),property=p.propertyPath,value=p.value,reference=ProductionObjectToken(p.objectReference,actor.transform)}).ToArray();receipt.propertyOverrides=receipt.authorizedBaselineOverrides.Length;
                    receipt.authorizedAddedObjects=PrefabUtility.GetAddedGameObjects(root).Select(p=>ProductionPath(p.instanceGameObject.transform,actor.transform)).ToArray();receipt.authorizedAddedComponents=PrefabUtility.GetAddedComponents(root).Select(p=>ProductionObjectToken(p.instanceComponent,actor.transform)).ToArray();receipt.authorizedRemovedObjects=PrefabUtility.GetRemovedGameObjects(root).Select(p=>ProductionObjectToken(p.assetGameObject,actor.transform)).ToArray();receipt.authorizedRemovedComponents=PrefabUtility.GetRemovedComponents(root).Select(p=>ProductionObjectToken(p.assetComponent,actor.transform)).ToArray();receipt.addedObjects=receipt.authorizedAddedObjects.Length;receipt.addedComponents=receipt.authorizedAddedComponents.Length;receipt.removedObjects=receipt.authorizedRemovedObjects.Length;receipt.removedComponents=receipt.authorizedRemovedComponents.Length;
                    var settings=new PrefabReplacingSettings{objectMatchMode=ObjectMatchMode.ByHierarchy,changeRootNameToAssetName=false,logInfo=true,prefabOverridesOptions=PrefabOverridesOptions.ClearNonDefaultPropertyOverrides|PrefabOverridesOptions.ClearAddedComponents|PrefabOverridesOptions.ClearAddedGameObjects|PrefabOverridesOptions.ClearRemovedComponents|PrefabOverridesOptions.ClearRemovedGameObjects};var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(production.actorPrefabs[species]);if(prefab==null||prefab.activeSelf)throw new InvalidOperationException("Qualified inactive production prefab missing.");
                    receipt.nativeReplacementCalled=true;PrefabUtility.ReplacePrefabAssetOfPrefabInstance(root,prefab,settings,InteractionMode.AutomatedAction);
                    var replacement=parent.GetChild(sibling).gameObject;receipt.targetRootReferenceRetained=replacement==root;replacement.name=name;replacement.transform.SetSiblingIndex(sibling);if(!replacement.transform.localPosition.Equals(position))replacement.transform.localPosition=position;if(!replacement.transform.localRotation.Equals(rotation))throw new InvalidOperationException("Native replacement changed preserved root rotation; no silent setter normalization.");replacement.transform.localScale=Vector3.one;replacement.SetActive(false);var current=replacement.GetComponent<DragonCharacter>();receipt.actorReferenceRetained=current==actor;if(current==null)throw new InvalidOperationException("Native replacement lost the actual actor component.");current.GetComponent<DragonRiderSaddle>().rigging.truck=truck;racer.choices[choice]=current;EditorUtility.SetDirty(racer);PrefabUtility.RecordPrefabInstancePropertyModifications(current.GetComponent<DragonRiderSaddle>().rigging);PrefabUtility.RecordPrefabInstancePropertyModifications(replacement);PrefabUtility.RecordPrefabInstancePropertyModifications(replacement.transform);receipt.targetAfter=NativeReplacementIdentity(replacement);receipt.actorAfter=NativeReplacementIdentity(current);
                    receipt.qualifiedSignatureSha256=HashTextForNativeReplacement(expected);receipt.qualifiedProfileExact=ProductionActorSignature(current,aliases)==expected;
                    receipt.externalIdentitiesExact=NativeReplacementIdsAgree(externalIds,NativeReplacementExternalIds(scene,current.transform));receipt.externalConfigurationExact=external==ProductionSceneConfiguration(scene,new HashSet<GameObject>{replacement});receipt.worldExact=world==NativeReplacementWorldState(match.world);receipt.nwhExact=ExactProductionNwhFields(fixedNwh,CaptureProductionNwhFields(match))&&FiniteWorldBoxDimensionsAgree(fixedNwh,CaptureProductionNwhFields(match),out receipt.maximumNwhDimensionDrift);
                    if(!receipt.qualifiedProfileExact||!receipt.externalIdentitiesExact||!receipt.externalConfigurationExact||!receipt.worldExact||!receipt.nwhExact)throw new InvalidOperationException("Native replacement failed qualified profile or protected external identity/config/NWH/world checks.");
                    if(!EditorSceneManager.SaveScene(scene,copy))throw new InvalidOperationException("Disposable native replacement scene did not save.");EditorSceneManager.CloseScene(scene,true);scene=EditorSceneManager.OpenScene(copy,OpenSceneMode.Additive);match=CourseMatch(scene);racer=match.racers[0];current=racer.choices[choice];receipt.savedReloaded=true;receipt.targetReloaded=NativeReplacementIdentity(current.gameObject);receipt.actorReloaded=NativeReplacementIdentity(current);
                    receipt.qualifiedProfileExact&=current.GetComponent<DragonRiderSaddle>().rigging.truck==match.trucks[0]&&ProductionActorSignature(current,aliases)==expected;receipt.externalIdentitiesExact&=NativeReplacementIdsAgree(externalIds,NativeReplacementExternalIds(scene,current.transform));receipt.externalConfigurationExact&=external==ProductionSceneConfiguration(scene,new HashSet<GameObject>{current.gameObject});receipt.worldExact&=world==NativeReplacementWorldState(match.world);float reloadedDrift=0;receipt.nwhExact&=ExactProductionNwhFields(fixedNwh,CaptureProductionNwhFields(match))&&FiniteWorldBoxDimensionsAgree(fixedNwh,CaptureProductionNwhFields(match),out reloadedDrift);receipt.maximumNwhDimensionDrift=Mathf.Max(receipt.maximumNwhDimensionDrift,reloadedDrift);
                    if(!receipt.qualifiedProfileExact||!receipt.externalIdentitiesExact||!receipt.externalConfigurationExact||!receipt.worldExact||!receipt.nwhExact)throw new InvalidOperationException("Saved/reloaded native replacement lost actual profile or original external identities/config/NWH/world.");receipt.status="DISPOSABLE_SINGLE_ACTOR_NATIVE_REPLACEMENT_VALIDATED_NOT_CANONICAL";
                }
            }
            catch(Exception error){failure=error;receipt.status="REJECTED_NATIVE_PREFAB_REPLACEMENT_PROBE";receipt.failure=error.ToString();}
            finally
            {
                void Cleanup(string stage,Action action){try{action();}catch(Exception error){receipt.failure+=("\nCLEANUP "+stage+": "+error);receipt.status="REJECTED_NATIVE_PREFAB_REPLACEMENT_PROBE";if(failure==null)failure=error;}}
                Cleanup("close-copy",()=>{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);});Cleanup("close-original",()=>{if(canonical.IsValid()&&canonical.isLoaded)EditorSceneManager.CloseScene(canonical,true);});Cleanup("restore-context",()=>{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);});Cleanup("source-bytes",()=>receipt.originalSourcesExact=receipt.originalSources.All(p=>File.Exists(p.path)&&Hash(p.path)==p.sha256));var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at>=0&&at+1<args.Length){string output=Path.Combine(args[at+1],"RiderSaddleAuthoring","NativePrefabReplacement");Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,injectUnknownActorEdit?"unknown-edit-rejection.json":"actual-single-actor-replacement.json"),JsonUtility.ToJson(receipt,true));}
            }
            if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();if(!receipt.originalSourcesExact)throw new InvalidOperationException("Disposable prefab experiment changed protected original scene/meta/NWH/world dependencies.");return JsonUtility.ToJson(receipt,true);
        }
        static string HashTextForNativeReplacement(string value)=>DragonKingdomMountainRoadLayoutAuthoring.Digest(System.Text.Encoding.UTF8.GetBytes(value));
    }
}
#endif
