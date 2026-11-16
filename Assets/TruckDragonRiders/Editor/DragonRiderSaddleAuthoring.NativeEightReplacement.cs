#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckDragonKingdom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        [Serializable] sealed class NativeEightChoice
        {
            public int slot,index; public string species,name,rootPlacement,originalSignature,qualifiedSignature;
            public string targetBefore,targetAfter,targetReloaded,actorBefore,actorAfter,actorReloaded;
            public bool rootReferenceRetained,actorReferenceRetained,otherSevenExact;
            public NativeReplacementOverride[] authorizedOverrides;
            public string[] authorizedAddedObjects,authorizedAddedComponents,authorizedRemovedObjects,authorizedRemovedComponents;
        }
        [Serializable] sealed class NativeEightReceipt
        {
            public string scope="DISPOSABLE_EXACT_ORIGINAL_SCENE_ALL_EIGHT_NATIVE_PREFAB_REPLACEMENTS_NOT_CANONICAL_COURSE_WORLD_FLIGHT_OR_WINDOWS_ADOPTION",status="IN_PROGRESS",failure,originalScene,copyScene,productionEvidence,singleEvidence;
            public string externalBefore,externalAfter,worldBefore,worldAfter;
            public int nativeCalls,replacedChoices,protectedExternalObjects,protectedRacers,protectedTrucks,protectedPhysicalRings;
            public bool allOriginalChoicesPreflighted,expectedLastChoiceEditRejected,otherSevenEveryStepExact,externalIdentitiesExact,externalConfigurationExact,worldExact,nwhExact,unitAncestors,qualifiedProfilesExact,noTestAssetDependencies,savedReloaded,originalSourcesExact;
            public float maximumNwhDimensionDrift;public NativeEightChoice[] choices;public FlightSourceHash[] originalSources;
        }
        static DragonCharacter[] NativeEightActors(DragonRidersMatch match)
        {
            if(match.racers.Length!=4||match.trucks.Length!=4||match.trucks.Distinct().Count()!=4)throw new InvalidOperationException("Exactly four distinct original racer/truck owners required.");
            var result=new List<DragonCharacter>();for(int slot=0;slot<4;slot++)
            {
                var racer=match.racers[slot];if(racer.slot!=slot||racer.match!=match||racer.truck!=match.trucks[slot]||racer.choiceIds.Length!=2||racer.choices.Length!=2||racer.choiceIds.Distinct().Count()!=2||!racer.choiceIds.OrderBy(s=>s).SequenceEqual(new[]{"dragon-fire","dragon-inferno"}))throw new InvalidOperationException("Preserve exact original two-species/four-owner roster.");
                result.AddRange(racer.choices);
            }
            if(result.Any(a=>a==null)||result.Distinct().Count()!=8)throw new InvalidOperationException("Eight distinct original choices required.");return result.ToArray();
        }
        static Dictionary<string,string> NativeEightProtectedIds(Scene scene,DragonCharacter[] actors)
        {
            var roots=new HashSet<Transform>(actors.Select(a=>a.transform));var result=new Dictionary<string,string>(StringComparer.Ordinal);
            foreach(var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>!roots.Any(root=>t==root||t.IsChildOf(root))))
            {
                result.Add(ProductionPath(t)+"|GameObject",NativeReplacementIdentity(t.gameObject));var components=t.GetComponents<Component>();for(int i=0;i<components.Length;i++){if(components[i]==null)throw new InvalidOperationException("Missing protected original component.");result.Add(ProductionPath(t)+"|"+i+":"+components[i].GetType().FullName,NativeReplacementIdentity(components[i]));}
            }
            return result;
        }
        static string NativeEightRootPlacement(Transform t)=>ProductionPath(t)+"|"+t.GetSiblingIndex()+"|"+PristineFloatBits(t.localPosition.x,t.localPosition.y,t.localPosition.z,t.localRotation.x,t.localRotation.y,t.localRotation.z,t.localRotation.w);
        static string NativeEightChoiceState(DragonCharacter actor)
        {
            var t=actor.transform;return ProductionActorSignature(actor,new Dictionary<UnityEngine.Object,string>())+"\nROOT|"+ProductionPath(t)+"|"+t.GetSiblingIndex()+"|"+PristineFloatBits(t.localPosition.x,t.localPosition.y,t.localPosition.z,t.localRotation.x,t.localRotation.y,t.localRotation.z,t.localRotation.w)+"|TRUCK|"+(actor.GetComponent<DragonRiderSaddle>().rigging.truck==null?"NULL":NativeReplacementIdentity(actor.GetComponent<DragonRiderSaddle>().rigging.truck))+"\n"+string.Join("\n",actor.GetComponentsInChildren<Transform>(true).SelectMany(x=>new[]{NativeReplacementIdentity(x.gameObject)}.Concat(x.GetComponents<Component>().Select(c=>c==null?"MISSING":NativeReplacementIdentity(c)))));
        }
        static void RequireNativeEightExternalReferences(Scene scene,DragonRidersMatch match,DragonCharacter[] actors)
        {
            var owners=actors.Select((a,i)=>new{actor=a,racer=match.racers[i/2],index=i%2}).ToArray();
            foreach(var component in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).Where(c=>c!=null))
            {
                var serialized=new SerializedObject(component);var it=serialized.GetIterator();while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference&&it.objectReferenceValue!=null)
                {
                    var value=it.objectReferenceValue;Transform target=value is GameObject go?go.transform:value is Component c?c.transform:null;if(target==null)continue;var owner=owners.SingleOrDefault(o=>target==o.actor.transform||target.IsChildOf(o.actor.transform));if(owner==null)continue;if(component.transform==owner.actor.transform||component.transform.IsChildOf(owner.actor.transform))continue;
                    if(component==owner.racer&&it.propertyPath=="choices.Array.data["+owner.index+"]")continue;if(component is Transform&&it.propertyPath.StartsWith("m_Children.Array.data[",StringComparison.Ordinal))continue;throw new InvalidOperationException("Unknown external actor reference: "+ProductionPath(component.transform)+"|"+it.propertyPath);
                }
            }
        }
        public static string ProbeAllEightKnownActorNativePrefabReplacement(string productionEvidence,string physicalCourseEvidence,string singleEvidence,string tag,bool injectLastChoiceEdit)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||string.IsNullOrEmpty(tag)||tag.Length>20||tag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new InvalidOperationException("Stopped compiled Editor and explicit short disposable tag required.");
            for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(s.isDirty||string.IsNullOrEmpty(s.path)||s.path==DragonRidersAuthoring.ScenePath)throw new InvalidOperationException("Save incoming scenes and close original Riders first.");}
            const string singleTest="DragonRiderNativePrefabReplacementTests.ActualSingleActorNativeApiRetainsQualifiedProfileAndRejectsUnknownEditsBeforeReplacement";RequireRiderPassedManifest(singleEvidence,singleTest+"(False)");RequireRiderPassedManifest(singleEvidence,singleTest+"(True)");
            var single=JsonUtility.FromJson<NativeReplacementReceipt>(File.ReadAllText(Path.Combine(singleEvidence,"rendered-evidence","RiderSaddleAuthoring","NativePrefabReplacement","actual-single-actor-replacement.json")));
            if(single.status!="DISPOSABLE_SINGLE_ACTOR_NATIVE_REPLACEMENT_VALIDATED_NOT_CANONICAL"||!string.IsNullOrEmpty(single.failure)||!single.originalSourcesExact||!single.externalIdentitiesExact||!single.externalConfigurationExact||!single.worldExact||!single.nwhExact||!single.qualifiedProfileExact||!single.savedReloaded||Path.GetFullPath(single.productionEvidence)!=Path.GetFullPath(productionEvidence))throw new InvalidOperationException("Actual passed single native replacement and exact same production authority required.");
            var production=ReadQualifiedCourseProduction(productionEvidence);if(production.species.Length!=2||!production.species.OrderBy(s=>s).SequenceEqual(new[]{"dragon-fire","dragon-inferno"}))throw new InvalidOperationException("Both qualified species required.");
            var mounts=production.species.Select((s,i)=>RequireQualifiedProductionMount(production.producerEvidence[i],production.runtimeEvidence[i])).ToArray();var aliases=mounts.Select((m,i)=>QualifiedCourseControllerAliases(m.fixture.controller,production.controllers[i])).ToArray();var expected=new string[2];
            for(int i=0;i<2;i++)
            {
                if(mounts[i].species!=production.species[i])throw new InvalidOperationException("Qualified species/evidence ordering changed.");
                RequireProductionSourceScene(production.sourceScene,mounts[i].producerManifest,physicalCourseEvidence);RequireRecordedStartupSourceHash(mounts[i].producerManifest,DragonRidersAuthoring.ScenePath,"All-eight original scene baseline");
                var contents=PrefabUtility.LoadPrefabContents(mounts[i].fixture.prefab);try{expected[i]=ProductionActorSignature(contents.GetComponent<DragonCharacter>(),aliases[i]);}finally{PrefabUtility.UnloadPrefabContents(contents);}contents=PrefabUtility.LoadPrefabContents(production.actorPrefabs[i]);try{if(ProductionActorSignature(contents.GetComponent<DragonCharacter>(),aliases[i])!=expected[i])throw new InvalidOperationException("Production actor differs from actual qualified fixture: "+production.species[i]);}finally{PrefabUtility.UnloadPrefabContents(contents);}
            }
            string folder="Assets/TruckDragonRiders/Tests/Generated/NativeEight-"+tag+(injectLastChoiceEdit?"-reject":"-baseline"),copy=folder+"/Original eight choices probe.unity";if(Directory.Exists(folder)||File.Exists(folder+".meta"))throw new InvalidOperationException("Never overwrite prior disposable evidence.");
            var watched=mounts.SelectMany(m=>m.fixture.sourceBefore.Select(row=>CurrentRiderPhysicalDependency(row.path))).Concat(AssetDatabase.GetDependencies(DragonRidersAuthoring.ScenePath,true)).Concat(AssetDatabase.GetDependencies(production.candidateScene,true)).Concat(new[]{DragonRidersAuthoring.ScenePath,production.candidateScene}).SelectMany(p=>new[]{p,p+".meta"}).Where(File.Exists).Distinct().ToArray();var proof=new NativeEightReceipt{originalScene=DragonRidersAuthoring.ScenePath,copyScene=copy,productionEvidence=productionEvidence,singleEvidence=singleEvidence,originalSources=watched.Select(p=>new FlightSourceHash{path=p,sha256=Hash(p)}).ToArray()};var setup=EditorSceneManager.GetSceneManagerSetup();Scene canonical=default,scene=default;Exception failure=null;
            try
            {
                canonical=EditorSceneManager.OpenScene(DragonRidersAuthoring.ScenePath,OpenSceneMode.Additive);var original=CourseMatch(canonical);var originals=NativeEightActors(original);var baseline=originals.Select(a=>ProductionActorSignature(a,new Dictionary<UnityEngine.Object,string>())).ToArray();var originalIds=NativeEightProtectedIds(canonical,originals);var nwh=CaptureProductionNwhFields(original);
                proof.choices=originals.Select((a,i)=>new NativeEightChoice{slot=i/2,index=i%2,species=original.racers[i/2].choiceIds[i%2],name=a.name,rootPlacement=NativeEightRootPlacement(a.transform),originalSignature=HashTextForNativeReplacement(baseline[i])}).ToArray();
                foreach(var row in proof.choices)if(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(originals[row.slot*2+row.index].gameObject)!="Assets/TruckDragonKingdom/Prefabs/Dragons/"+row.species+"/"+row.species+" Dragon Actor.prefab")throw new InvalidOperationException("Unknown original prefab/custom actor: "+row.slot+"/"+row.species);
                Directory.CreateDirectory(folder);AssetDatabase.Refresh();if(!AssetDatabase.CopyAsset(DragonRidersAuthoring.ScenePath,copy)||!File.ReadAllBytes(copy).SequenceEqual(File.ReadAllBytes(DragonRidersAuthoring.ScenePath)))throw new InvalidOperationException("Disposable copy must start byte-identical.");if(!EditorSceneManager.CloseScene(canonical,true))throw new InvalidOperationException("Original singleton scene must close before copy opens.");canonical=default;
                scene=EditorSceneManager.OpenScene(copy,OpenSceneMode.Additive);var match=CourseMatch(scene);var actors=NativeEightActors(match);var protectedIds=NativeEightProtectedIds(scene,actors);if(!NativeReplacementIdsAgree(originalIds,protectedIds))throw new InvalidOperationException("Original nontarget identities changed on copy.");proof.protectedExternalObjects=protectedIds.Count;proof.protectedRacers=match.racers.Length;proof.protectedTrucks=match.trucks.Length;proof.protectedPhysicalRings=CoursePhysicalGates(scene).Length;if(proof.protectedPhysicalRings!=12||match.gates.Length!=12)throw new InvalidOperationException("Preserve original twelve course checkpoints.");
                string external=ProductionSceneConfiguration(scene,new HashSet<GameObject>(actors.Select(a=>a.gameObject))),world=NativeReplacementWorldState(match.world);proof.externalBefore=HashTextForNativeReplacement(external);proof.worldBefore=HashTextForNativeReplacement(world);
                int last=Array.FindIndex(proof.choices,r=>r.slot==3&&r.species=="dragon-fire");if(last<0)throw new InvalidOperationException("Last-slot Fire negative control required.");if(injectLastChoiceEdit)actors[last].motor.transform.localScale*=1.0001f;
                bool baselineExact=true;for(int i=0;i<8;i++)baselineExact&=ProductionActorSignature(actors[i],new Dictionary<UnityEngine.Object,string>())==baseline[i];
                if(!baselineExact)
                {
                    if(!injectLastChoiceEdit)throw new InvalidOperationException("Unknown original actor edits: refuse all replacements before step0.");proof.expectedLastChoiceEditRejected=true;proof.status="LAST_CHOICE_CUSTOM_EDIT_REJECTED_BEFORE_ALL_REPLACEMENTS";
                }
                else
                {
                    if(injectLastChoiceEdit)throw new InvalidOperationException("Last-choice edit fault escaped baseline guard.");RequireNativeEightExternalReferences(scene,match,actors);proof.allOriginalChoicesPreflighted=true;proof.otherSevenEveryStepExact=true;
                    for(int i=0;i<8;i++)
                    {
                        actors=NativeEightActors(match);var row=proof.choices[i];var old=actors[i];var racer=match.racers[row.slot];int species=Array.IndexOf(production.species,row.species);var others=actors.Select((a,j)=>j==i?null:NativeEightChoiceState(a)).ToArray();var root=old.gameObject;var parent=old.transform.parent;int sibling=old.transform.GetSiblingIndex();var position=old.transform.localPosition;var rotation=old.transform.localRotation;for(var t=parent;t!=null;t=t.parent)if(!t.localScale.Equals(Vector3.one))throw new InvalidOperationException("Original actor ancestors must remain unit scale.");row.targetBefore=NativeReplacementIdentity(root);row.actorBefore=NativeReplacementIdentity(old);
                        row.authorizedOverrides=(PrefabUtility.GetPropertyModifications(root)??Array.Empty<PropertyModification>()).Select(p=>new NativeReplacementOverride{target=ProductionObjectToken(p.target,old.transform),property=p.propertyPath,value=p.value,reference=ProductionObjectToken(p.objectReference,old.transform)}).ToArray();row.authorizedAddedObjects=PrefabUtility.GetAddedGameObjects(root).Select(p=>ProductionPath(p.instanceGameObject.transform,old.transform)).ToArray();row.authorizedAddedComponents=PrefabUtility.GetAddedComponents(root).Select(p=>ProductionObjectToken(p.instanceComponent,old.transform)).ToArray();row.authorizedRemovedObjects=PrefabUtility.GetRemovedGameObjects(root).Select(p=>ProductionObjectToken(p.assetGameObject,old.transform)).ToArray();row.authorizedRemovedComponents=PrefabUtility.GetRemovedComponents(root).Select(p=>ProductionObjectToken(p.assetComponent,old.transform)).ToArray();
                        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(production.actorPrefabs[species]);if(prefab==null||prefab.activeSelf)throw new InvalidOperationException("Inactive qualified actor prefab required.");var settings=new PrefabReplacingSettings{objectMatchMode=ObjectMatchMode.ByHierarchy,changeRootNameToAssetName=false,logInfo=true,prefabOverridesOptions=PrefabOverridesOptions.ClearNonDefaultPropertyOverrides|PrefabOverridesOptions.ClearAddedComponents|PrefabOverridesOptions.ClearAddedGameObjects|PrefabOverridesOptions.ClearRemovedComponents|PrefabOverridesOptions.ClearRemovedGameObjects};proof.nativeCalls++;PrefabUtility.ReplacePrefabAssetOfPrefabInstance(root,prefab,settings,InteractionMode.AutomatedAction);
                        var replacement=parent.GetChild(sibling).gameObject;row.rootReferenceRetained=replacement==root;replacement.name=row.name;replacement.transform.SetSiblingIndex(sibling);if(!replacement.transform.localPosition.Equals(position))replacement.transform.localPosition=position;if(!replacement.transform.localRotation.Equals(rotation))throw new InvalidOperationException("Native API changed original root rotation.");replacement.transform.localScale=Vector3.one;replacement.SetActive(false);var current=replacement.GetComponent<DragonCharacter>();if(current==null)throw new InvalidOperationException("Native API lost actor component.");row.actorReferenceRetained=current==old;current.GetComponent<DragonRiderSaddle>().rigging.truck=racer.truck;racer.choices[row.index]=current;EditorUtility.SetDirty(racer);PrefabUtility.RecordPrefabInstancePropertyModifications(current.GetComponent<DragonRiderSaddle>().rigging);PrefabUtility.RecordPrefabInstancePropertyModifications(replacement);PrefabUtility.RecordPrefabInstancePropertyModifications(replacement.transform);row.targetAfter=NativeReplacementIdentity(replacement);row.actorAfter=NativeReplacementIdentity(current);row.qualifiedSignature=HashTextForNativeReplacement(expected[species]);if(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(current.gameObject)!=production.actorPrefabs[species]||ProductionActorSignature(current,aliases[species])!=expected[species])throw new InvalidOperationException("Actual qualified profile mismatch at step "+i);
                        actors=NativeEightActors(match);row.otherSevenExact=Enumerable.Range(0,8).Where(j=>j!=i).All(j=>others[j]==NativeEightChoiceState(actors[j]));proof.otherSevenEveryStepExact&=row.otherSevenExact;if(!row.otherSevenExact||!NativeReplacementIdsAgree(protectedIds,NativeEightProtectedIds(scene,actors)))throw new InvalidOperationException("Another actor or protected external identity changed at step "+i);proof.replacedChoices++;
                    }
                    if(!EditorSceneManager.SaveScene(scene,copy))throw new InvalidOperationException("Disposable all-eight scene failed to save.");if(!EditorSceneManager.CloseScene(scene,true))throw new InvalidOperationException("Disposable scene did not close for real reload.");scene=EditorSceneManager.OpenScene(copy,OpenSceneMode.Additive);match=CourseMatch(scene);actors=NativeEightActors(match);proof.savedReloaded=true;proof.unitAncestors=true;proof.qualifiedProfilesExact=true;
                    foreach(var row in proof.choices){var actor=actors[row.slot*2+row.index];int species=Array.IndexOf(production.species,row.species);row.targetReloaded=NativeReplacementIdentity(actor.gameObject);row.actorReloaded=NativeReplacementIdentity(actor);proof.qualifiedProfilesExact&=row.targetAfter==row.targetReloaded&&row.actorAfter==row.actorReloaded&&actor.name==row.name&&NativeEightRootPlacement(actor.transform)==row.rootPlacement&&!actor.gameObject.activeSelf&&actor.GetComponent<DragonRiderSaddle>().rigging.truck==match.trucks[row.slot]&&PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(actor.gameObject)==production.actorPrefabs[species]&&ProductionActorSignature(actor,aliases[species])==expected[species];for(var t=actor.transform;t!=null;t=t.parent)proof.unitAncestors&=t.localScale.Equals(Vector3.one);}
                    proof.noTestAssetDependencies=!AssetDatabase.GetDependencies(copy,true).Any(p=>p!=copy&&p.Contains("/Tests/Generated/"));proof.status="DISPOSABLE_ALL_EIGHT_NATIVE_REPLACEMENTS_VALIDATED_NOT_CANONICAL";
                }
                actors=NativeEightActors(match);proof.externalIdentitiesExact=NativeReplacementIdsAgree(protectedIds,NativeEightProtectedIds(scene,actors));string currentExternal=ProductionSceneConfiguration(scene,new HashSet<GameObject>(actors.Select(a=>a.gameObject))),currentWorld=NativeReplacementWorldState(match.world);proof.externalAfter=HashTextForNativeReplacement(currentExternal);proof.worldAfter=HashTextForNativeReplacement(currentWorld);proof.externalConfigurationExact=external==currentExternal;proof.worldExact=world==currentWorld;var currentNwh=CaptureProductionNwhFields(match);proof.nwhExact=ExactProductionNwhFields(nwh,currentNwh)&&FiniteWorldBoxDimensionsAgree(nwh,currentNwh,out proof.maximumNwhDimensionDrift);
                if(!proof.externalIdentitiesExact||!proof.externalConfigurationExact||!proof.worldExact||!proof.nwhExact||(!injectLastChoiceEdit&&(!proof.qualifiedProfilesExact||!proof.unitAncestors||!proof.noTestAssetDependencies||!proof.otherSevenEveryStepExact||proof.replacedChoices!=8)))throw new InvalidOperationException("All-eight saved scene failed protected identity/config/world/NWH or qualified profile gates.");
            }
            catch(Exception error){failure=error;proof.failure=error.ToString();proof.status="REJECTED_ALL_EIGHT_NATIVE_REPLACEMENT_PROBE";}
            finally
            {
                void Cleanup(string stage,Action action){try{action();}catch(Exception error){proof.failure+="\nCLEANUP "+stage+": "+error;proof.status="REJECTED_ALL_EIGHT_NATIVE_REPLACEMENT_PROBE";if(failure==null)failure=error;}}
                Cleanup("close-copy",()=>{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);});Cleanup("close-original",()=>{if(canonical.IsValid()&&canonical.isLoaded)EditorSceneManager.CloseScene(canonical,true);});Cleanup("restore-context",()=>{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);});Cleanup("source-bytes",()=>proof.originalSourcesExact=proof.originalSources.All(p=>File.Exists(p.path)&&Hash(p.path)==p.sha256));var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at>=0&&at+1<args.Length){string output=Path.Combine(args[at+1],"RiderSaddleAuthoring","NativeEightReplacement");Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,injectLastChoiceEdit?"last-choice-edit-rejection.json":"actual-all-eight-replacement.json"),JsonUtility.ToJson(proof,true));}
            }
            if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();if(!proof.originalSourcesExact)throw new InvalidOperationException("Original source dependencies changed; no canonical adoption permitted.");return JsonUtility.ToJson(proof,true);
        }
    }
}
#endif
