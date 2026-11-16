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
        [Serializable] sealed class CanonicalMountPlan
        {
            public string scope="EXPLICIT_EIGHT_MOUNT_SUBTREES_ONLY_NOT_COURSE_WORLD_OR_GAMEPLAY_ADOPTION",projectRoot,scene,sceneSha256,metaSha256,sceneGuid,productionEvidence,courseEvidence,eightEvidence,eightTestsSha256,eightReceiptSha256;
            public string externalConfiguration,worldState;public NativeEightChoice[] choices;public FlightSourceHash[] protectedSources;
        }
        [Serializable] sealed class CanonicalMountApply
        {
            public string scope,status="IN_PROGRESS",failure,planSha256,projectRoot,target,targetPhysicalPath,sourceSha256,resultSha256,metaSha256,backupFolder,productionEvidence,eightEvidence;
            public string externalBefore,externalAfter,worldBefore,worldAfter;
            public bool canonicalTarget,allOriginalChoicesPreflighted,otherSevenEveryStepExact,externalIdentitiesExact,configurationExact,worldExact,nwhExact,profilesExact,unitAncestors,noTestDependencies,savedReloaded,sourceClosureExact,rollbackExact;
            public int nativeCalls,protectedExternalObjects,protectedRacers,protectedTrucks,protectedRings;public float maximumNwhDimensionDrift;public NativeEightChoice[] choices;public CanonicalMountPlan plan;
        }
        sealed class CanonicalMountContext
        {
            public MountedProductionCandidateReceipt production;public NativeEightReceipt baseline;public QualifiedProductionMount[] mounts;public Dictionary<UnityEngine.Object,string>[] aliases;public string[] signatures;
        }
        static CanonicalMountApply lastCanonicalMountApply;
        static void RequirePromotionEditorContext()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required for explicit mount promotion.");
            for(int i=0;i<SceneManager.sceneCount;i++){var scene=SceneManager.GetSceneAt(i);if(scene.isDirty||string.IsNullOrEmpty(scene.path)||scene.path==DragonRidersAuthoring.ScenePath)throw new InvalidOperationException("Preserve incoming edits: save and close canonical Riders before explicit promotion.");}
        }
        static CanonicalMountContext RequireCanonicalMountContext(string productionEvidence,string courseEvidence,string eightEvidence)
        {
            const string test="DragonRiderNativeEightReplacementTests.ActualEightChoiceNativeReplacementPreservesOwnersAndRejectsLastChoiceEditBeforeStepZero";RequireRiderPassedManifest(eightEvidence,test+"(False)");RequireRiderPassedManifest(eightEvidence,test+"(True)");
            string folder=Path.Combine(eightEvidence,"rendered-evidence","RiderSaddleAuthoring","NativeEightReplacement");var proof=JsonUtility.FromJson<NativeEightReceipt>(File.ReadAllText(Path.Combine(folder,"actual-all-eight-replacement.json")));var rejected=JsonUtility.FromJson<NativeEightReceipt>(File.ReadAllText(Path.Combine(folder,"last-choice-edit-rejection.json")));
            if(proof.status!="DISPOSABLE_ALL_EIGHT_NATIVE_REPLACEMENTS_VALIDATED_NOT_CANONICAL"||!string.IsNullOrEmpty(proof.failure)||!proof.originalSourcesExact||!proof.externalIdentitiesExact||!proof.externalConfigurationExact||!proof.worldExact||!proof.nwhExact||!proof.qualifiedProfilesExact||!proof.savedReloaded||!proof.unitAncestors||!proof.noTestAssetDependencies||!proof.allOriginalChoicesPreflighted||!proof.otherSevenEveryStepExact||proof.nativeCalls!=8||proof.replacedChoices!=8||proof.choices?.Length!=8||proof.protectedRacers!=4||proof.protectedTrucks!=4||proof.protectedPhysicalRings!=12||Path.GetFullPath(proof.productionEvidence)!=Path.GetFullPath(productionEvidence))throw new InvalidOperationException("Exact actual passed all-eight authority required.");
            if(rejected.status!="LAST_CHOICE_CUSTOM_EDIT_REJECTED_BEFORE_ALL_REPLACEMENTS"||!string.IsNullOrEmpty(rejected.failure)||!rejected.expectedLastChoiceEditRejected||rejected.nativeCalls!=0||rejected.replacedChoices!=0||!rejected.originalSourcesExact||!rejected.externalIdentitiesExact||!rejected.externalConfigurationExact||!rejected.worldExact||!rejected.nwhExact)throw new InvalidOperationException("Actual passed final-choice rejection before step0 required.");
            var production=ReadQualifiedCourseProduction(productionEvidence);var mounts=production.species.Select((s,i)=>RequireQualifiedProductionMount(production.producerEvidence[i],production.runtimeEvidence[i])).ToArray();var aliases=mounts.Select((m,i)=>QualifiedCourseControllerAliases(m.fixture.controller,production.controllers[i])).ToArray();var expected=new string[2];
            for(int i=0;i<2;i++)
            {
                if(mounts[i].species!=production.species[i])throw new InvalidOperationException("Qualified production evidence species ordering changed.");RequireProductionSourceScene(production.sourceScene,mounts[i].producerManifest,courseEvidence);RequireRecordedStartupSourceHash(mounts[i].producerManifest,DragonRidersAuthoring.ScenePath,"Original canonical mount-promotion baseline");
                var contents=PrefabUtility.LoadPrefabContents(mounts[i].fixture.prefab);try{expected[i]=ProductionActorSignature(contents.GetComponent<DragonCharacter>(),aliases[i]);}finally{PrefabUtility.UnloadPrefabContents(contents);}contents=PrefabUtility.LoadPrefabContents(production.actorPrefabs[i]);try{if(ProductionActorSignature(contents.GetComponent<DragonCharacter>(),aliases[i])!=expected[i])throw new InvalidOperationException("Production prefab differs from actual native fixture.");}finally{PrefabUtility.UnloadPrefabContents(contents);}
                if(proof.choices.Where(row=>row.species==mounts[i].species).Count()!=4||proof.choices.Where(row=>row.species==mounts[i].species).Any(row=>row.qualifiedSignature!=HashTextForNativeReplacement(expected[i])))throw new InvalidOperationException("Current qualified profile no longer equals actual all-eight native profile.");
            }
            return new CanonicalMountContext{production=production,baseline=proof,mounts=mounts,aliases=aliases,signatures=expected};
        }
        static string CanonicalEightReceiptPath(string folder)=>Path.Combine(folder,"rendered-evidence","RiderSaddleAuthoring","NativeEightReplacement","actual-all-eight-replacement.json");
        static void RequireOriginalPromotionChoices(DragonRidersMatch match,NativeEightChoice[] expected)
        {
            var actors=NativeEightActors(match);for(int i=0;i<8;i++)
            {
                var row=expected[i];var actor=actors[i];if(row.slot!=i/2||row.index!=i%2||match.racers[i/2].choiceIds[i%2]!=row.species||actor.name!=row.name||NativeEightRootPlacement(actor.transform)!=row.rootPlacement||NativeReplacementIdentity(actor.gameObject)!=row.targetBefore||NativeReplacementIdentity(actor)!=row.actorBefore||HashTextForNativeReplacement(ProductionActorSignature(actor,new Dictionary<UnityEngine.Object,string>()))!=row.originalSignature||PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(actor.gameObject)!="Assets/TruckDragonKingdom/Prefabs/Dragons/"+row.species+"/"+row.species+" Dragon Actor.prefab")throw new InvalidOperationException("Unknown original choice edit or source identity: crew"+row.slot+"/"+row.species+"; no native replacement permitted.");
            }
        }
        static string[] CanonicalMountProtectedPaths(CanonicalMountContext context,string scene,string eightEvidence)=>context.mounts.SelectMany(m=>m.fixture.sourceBefore.Select(row=>CurrentRiderPhysicalDependency(row.path))).Concat(AssetDatabase.GetDependencies(scene,true)).Concat(AssetDatabase.GetDependencies(context.production.candidateScene,true)).Concat(new[]{scene,context.production.candidateScene,Path.Combine(eightEvidence,"tests.xml"),CanonicalEightReceiptPath(eightEvidence),Path.Combine(eightEvidence,"rendered-evidence","RiderSaddleAuthoring","NativeEightReplacement","last-choice-edit-rejection.json")}).SelectMany(p=>new[]{p,p+".meta"}).Where(File.Exists).Where(p=>!string.Equals(Path.GetFullPath(p),Path.GetFullPath(scene),StringComparison.OrdinalIgnoreCase)).Distinct().OrderBy(p=>p,StringComparer.Ordinal).ToArray();
        public static string BuildCanonicalKnownMountPromotionPlan(string productionEvidence,string courseEvidence,string eightEvidence)
        {
            RequirePromotionEditorContext();var context=RequireCanonicalMountContext(productionEvidence,courseEvidence,eightEvidence);string scenePath=DragonRidersAuthoring.ScenePath;
            var plan=new CanonicalMountPlan{projectRoot=Directory.GetParent(Application.dataPath).FullName,scene=scenePath,sceneSha256=Hash(scenePath),metaSha256=Hash(scenePath+".meta"),sceneGuid=AssetDatabase.AssetPathToGUID(scenePath),productionEvidence=productionEvidence,courseEvidence=courseEvidence,eightEvidence=eightEvidence,eightTestsSha256=Hash(Path.Combine(eightEvidence,"tests.xml")),eightReceiptSha256=Hash(CanonicalEightReceiptPath(eightEvidence)),choices=JsonUtility.FromJson<NativeEightReceipt>(JsonUtility.ToJson(context.baseline)).choices};
            plan.protectedSources=CanonicalMountProtectedPaths(context,scenePath,eightEvidence).Select(p=>new FlightSourceHash{path=p,sha256=Hash(p)}).ToArray();var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;
            try
            {
                scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);var match=CourseMatch(scene);RequireOriginalPromotionChoices(match,plan.choices);var actors=NativeEightActors(match);RequireNativeEightExternalReferences(scene,match,actors);if(match.gates.Length!=12||CoursePhysicalGates(scene).Length!=12)throw new InvalidOperationException("Preserve twelve original physical/scoring rings.");
                plan.externalConfiguration=HashTextForNativeReplacement(ProductionSceneConfiguration(scene,new HashSet<GameObject>(actors.Select(a=>a.gameObject))));plan.worldState=HashTextForNativeReplacement(NativeReplacementWorldState(match.world));if(plan.externalConfiguration!=context.baseline.externalBefore||plan.worldState!=context.baseline.worldBefore)throw new InvalidOperationException("Current original external configuration/world no longer equals the actual passed all-eight baseline.");
            }
            finally{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
            if(Hash(scenePath)!=plan.sceneSha256||Hash(scenePath+".meta")!=plan.metaSha256||!plan.protectedSources.All(row=>File.Exists(row.path)&&Hash(row.path)==row.sha256))throw new InvalidOperationException("Plan construction changed a protected source.");return JsonUtility.ToJson(plan,true);
        }
        static void RequireCanonicalPlanCurrent(CanonicalMountPlan plan)
        {
            if(plan==null||!string.Equals(Path.GetFullPath(plan.projectRoot),Directory.GetParent(Application.dataPath).FullName,StringComparison.OrdinalIgnoreCase)||plan.scene!=DragonRidersAuthoring.ScenePath||Hash(plan.scene)!=plan.sceneSha256||Hash(plan.scene+".meta")!=plan.metaSha256||AssetDatabase.AssetPathToGUID(plan.scene)!=plan.sceneGuid||Hash(Path.Combine(plan.eightEvidence,"tests.xml"))!=plan.eightTestsSha256||Hash(CanonicalEightReceiptPath(plan.eightEvidence))!=plan.eightReceiptSha256||plan.choices?.Length!=8||plan.protectedSources==null||!plan.protectedSources.All(row=>File.Exists(row.path)&&Hash(row.path)==row.sha256))throw new InvalidOperationException("Stale or changed exact canonical promotion plan/source closure; no write permitted.");
        }
        public static string ApplyCanonicalKnownMountPromotion(string planJson,string outputDirectory)
        {
            // Explicit entry point only; never called by normal builds,
            // initialization or the disposable native acceptance controls.
            return ApplyKnownMountPromotion(planJson,DragonRidersAuthoring.ScenePath,outputDirectory,null,false);
        }
        static string[] CanonicalPromotionCommandArguments(string[] args)
        {
            const string apply="-tsRiderApplyCanonicalMounts";
            var flags=new[]{"-tsDragonMountedProductionEvidence","-tsRiderProductionCourseEvidence","-tsRiderNativeEightEvidence","-tsRiderCanonicalPromotionOutput"};
            if(args==null||args.Count(a=>a==apply)!=1)throw new ArgumentException("One explicit canonical-mount Apply flag is required; normal tests/builds must never promote mounts.");
            return flags.Select(flag=>
            {
                int at=Array.IndexOf(args,flag);
                if(args.Count(a=>a==flag)!=1||at<0||at+1>=args.Length||string.IsNullOrWhiteSpace(args[at+1])||args[at+1].StartsWith("-",StringComparison.Ordinal))throw new ArgumentException("One explicit evidence/output value is required: "+flag);
                return args[at+1];
            }).ToArray();
        }
        // An explicit batch command, never an initialization/build callback.
        // It invokes the same guarded Plan/Apply already exercised by the
        // disposable controls; it grants no course/world/gameplay acceptance.
        public static void ApplyCanonicalKnownMountPromotionFromCommandLine()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Explicit batch invocation required; use the guarded Plan/Apply APIs for an interactive saved Editor.");
            var values=CanonicalPromotionCommandArguments(Environment.GetCommandLineArgs());
            foreach(string evidence in values.Take(3))if(!File.Exists(Path.Combine(evidence,"tests.xml")))throw new InvalidOperationException("Actual passed native evidence is required before preparing an isolated command scene: "+evidence);
            string output=Path.GetFullPath(values[3]),permitted=Path.Combine(RiderEvidenceHostRoot(values[2]),"Logs","TruckSports","DragonKingdom")+Path.DirectorySeparatorChar;
            if(!(output+Path.DirectorySeparatorChar).StartsWith(permitted,StringComparison.OrdinalIgnoreCase)||output.TrimEnd(Path.DirectorySeparatorChar)==permitted.TrimEnd(Path.DirectorySeparatorChar)||Directory.Exists(output)||File.Exists(output))throw new InvalidOperationException("A fresh explicit private DragonKingdom Logs output directory is required; preserve prior plans/receipts.");
            if(SceneManager.sceneCount==1)
            {
                var incoming=SceneManager.GetSceneAt(0);
                if(string.IsNullOrEmpty(incoming.path)&&!incoming.isDirty&&incoming.rootCount==0)
                {
                    const string folder="Assets/TruckDragonRiders/ReviewCandidates/CanonicalPromotionRunner/";
                    Directory.CreateDirectory(folder);AssetDatabase.Refresh();
                    if(!EditorSceneManager.SaveScene(incoming,folder+"Isolated-"+Guid.NewGuid().ToString("N")+".unity"))throw new InvalidOperationException("Could not preserve the empty batch runner scene.");
                }
            }
            RequirePromotionEditorContext();
            string plan=BuildCanonicalKnownMountPromotionPlan(values[0],values[1],values[2]);
            Directory.CreateDirectory(output);
            using(var stream=new FileStream(Path.Combine(output,"canonical-mount-plan.json"),FileMode.CreateNew,FileAccess.Write))using(var writer=new StreamWriter(stream))writer.Write(plan);
            var receipt=JsonUtility.FromJson<CanonicalMountApply>(ApplyCanonicalKnownMountPromotion(plan,output));
            Debug.Log("Explicit canonical mount promotion: "+receipt.status+"; world/course/gameplay acceptance remains separate. Receipt: "+Path.Combine(output,"mount-promotion-canonical.json"));
        }
        static string ApplyKnownMountPromotion(string planJson,string target,string outputDirectory,string testFault,bool disposable)
        {
            RequirePromotionEditorContext();var plan=JsonUtility.FromJson<CanonicalMountPlan>(planJson);RequireCanonicalPlanCurrent(plan);var context=RequireCanonicalMountContext(plan.productionEvidence,plan.courseEvidence,plan.eightEvidence);
            if(disposable?(!target.StartsWith("Assets/TruckDragonRiders/Tests/Generated/CanonicalPromotion-",StringComparison.Ordinal)||target.Contains("..")||Hash(target)!=plan.sceneSha256):target!=plan.scene)throw new InvalidOperationException("Only exact canonical target or explicitly owned byte-identical native test copy permitted.");
            string permittedOutput=Path.Combine(RiderEvidenceHostRoot(plan.eightEvidence),"Logs","TruckSports","DragonKingdom")+Path.DirectorySeparatorChar,actualOutput=Path.GetFullPath(outputDirectory)+Path.DirectorySeparatorChar;if(!actualOutput.StartsWith(permittedOutput,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Promotion backups/evidence must remain within project-private TruckSports DragonKingdom Logs.");
            if(!CanonicalMountProtectedPaths(context,plan.scene,plan.eightEvidence).SequenceEqual(plan.protectedSources.Select(row=>row.path)))throw new InvalidOperationException("Promotion plan source closure membership changed or was truncated.");
            string receiptPath=Path.Combine(outputDirectory,"mount-promotion-"+(disposable?testFault??"baseline":"canonical")+".json");if(File.Exists(receiptPath))throw new InvalidOperationException("Preserve prior mount-promotion evidence; choose a fresh explicit output directory.");
            string backup=Path.Combine(outputDirectory,"mount-promotion-backup-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(backup);var before=File.ReadAllBytes(target);var meta=File.ReadAllBytes(target+".meta");File.WriteAllBytes(Path.Combine(backup,"before.unity"),before);File.WriteAllBytes(Path.Combine(backup,"before.unity.meta"),meta);
            var proof=new CanonicalMountApply{scope=disposable?"DISPOSABLE_PLAN_APPLY_CONTROL_NEVER_CANONICAL":"EXPLICIT_CANONICAL_EIGHT_MOUNTS_ONLY_COURSE_WORLD_AND_GAMEPLAY_REMAIN_UNVERIFIED",projectRoot=Directory.GetParent(Application.dataPath).FullName,target=target,targetPhysicalPath=Path.GetFullPath(target),canonicalTarget=!disposable,sourceSha256=Hash(target),metaSha256=Hash(target+".meta"),backupFolder=backup,planSha256=HashTextForNativeReplacement(planJson),productionEvidence=plan.productionEvidence,eightEvidence=plan.eightEvidence,plan=plan,choices=JsonUtility.FromJson<NativeEightReceipt>(JsonUtility.ToJson(context.baseline)).choices};lastCanonicalMountApply=proof;foreach(var row in proof.choices){row.targetAfter=row.actorAfter=row.targetReloaded=row.actorReloaded=null;row.rootReferenceRetained=row.actorReferenceRetained=row.otherSevenExact=false;}
            var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;Exception failure=null;bool complete=false;
            try
            {
                scene=EditorSceneManager.OpenScene(target,OpenSceneMode.Single);var match=CourseMatch(scene);var actors=NativeEightActors(match);var ids=NativeEightProtectedIds(scene,actors);var nwh=CaptureProductionNwhFields(match);string external=ProductionSceneConfiguration(scene,new HashSet<GameObject>(actors.Select(a=>a.gameObject))),world=NativeReplacementWorldState(match.world);proof.protectedExternalObjects=ids.Count;proof.protectedRacers=match.racers.Length;proof.protectedTrucks=match.trucks.Length;proof.protectedRings=CoursePhysicalGates(scene).Length;proof.externalBefore=HashTextForNativeReplacement(external);proof.worldBefore=HashTextForNativeReplacement(world);
                if(proof.externalBefore!=plan.externalConfiguration||proof.worldBefore!=plan.worldState||proof.protectedRings!=12||match.gates.Length!=12)throw new InvalidOperationException("Protected world/course/configuration changed before promotion.");if(testFault=="unknown-last")actors[Array.FindIndex(plan.choices,row=>row.slot==3&&row.species=="dragon-fire")].motor.transform.localScale*=1.0001f;
                RequireOriginalPromotionChoices(match,plan.choices);RequireNativeEightExternalReferences(scene,match,actors);proof.allOriginalChoicesPreflighted=true;proof.otherSevenEveryStepExact=true;
                for(int i=0;i<8;i++)
                {
                    actors=NativeEightActors(match);var row=proof.choices[i];var old=actors[i];var others=actors.Select((a,j)=>j==i?null:NativeEightChoiceState(a)).ToArray();var root=old.gameObject;var parent=old.transform.parent;int sibling=old.transform.GetSiblingIndex();var position=old.transform.localPosition;var rotation=old.transform.localRotation;int species=Array.IndexOf(context.production.species,row.species);var racer=match.racers[row.slot];for(var t=parent;t!=null;t=t.parent)if(!t.localScale.Equals(Vector3.one))throw new InvalidOperationException("Original ancestors must remain unit scale.");
                    // Same installed native API/options as the passed V424
                    // eight-choice experiment; only scene-local overrides
                    // and the known choice reference may change.
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(context.production.actorPrefabs[species]);if(prefab==null||prefab.activeSelf)throw new InvalidOperationException("Inactive exact qualified prefab required.");var settings=new PrefabReplacingSettings{objectMatchMode=ObjectMatchMode.ByHierarchy,changeRootNameToAssetName=false,logInfo=true,prefabOverridesOptions=PrefabOverridesOptions.ClearNonDefaultPropertyOverrides|PrefabOverridesOptions.ClearAddedComponents|PrefabOverridesOptions.ClearAddedGameObjects|PrefabOverridesOptions.ClearRemovedComponents|PrefabOverridesOptions.ClearRemovedGameObjects};proof.nativeCalls++;PrefabUtility.ReplacePrefabAssetOfPrefabInstance(root,prefab,settings,InteractionMode.AutomatedAction);
                    var replacement=parent.GetChild(sibling).gameObject;row.rootReferenceRetained=replacement==root;replacement.name=row.name;replacement.transform.SetSiblingIndex(sibling);if(!replacement.transform.localPosition.Equals(position))replacement.transform.localPosition=position;if(!replacement.transform.localRotation.Equals(rotation))throw new InvalidOperationException("Native replacement changed original root rotation.");replacement.transform.localScale=Vector3.one;replacement.SetActive(false);var current=replacement.GetComponent<DragonCharacter>();if(current==null)throw new InvalidOperationException("Native replacement lost actual actor.");row.actorReferenceRetained=current==old;current.GetComponent<DragonRiderSaddle>().rigging.truck=racer.truck;racer.choices[row.index]=current;EditorUtility.SetDirty(racer);PrefabUtility.RecordPrefabInstancePropertyModifications(current.GetComponent<DragonRiderSaddle>().rigging);PrefabUtility.RecordPrefabInstancePropertyModifications(replacement);PrefabUtility.RecordPrefabInstancePropertyModifications(replacement.transform);row.targetAfter=NativeReplacementIdentity(replacement);row.actorAfter=NativeReplacementIdentity(current);
                    if(ProductionActorSignature(current,context.aliases[species])!=context.signatures[species])throw new InvalidOperationException("Qualified profile changed at native step "+i);actors=NativeEightActors(match);row.otherSevenExact=Enumerable.Range(0,8).Where(j=>j!=i).All(j=>others[j]==NativeEightChoiceState(actors[j]));proof.otherSevenEveryStepExact&=row.otherSevenExact;if(!row.otherSevenExact||!NativeReplacementIdsAgree(ids,NativeEightProtectedIds(scene,actors)))throw new InvalidOperationException("Protected other choice or external identity changed at step "+i);
                    if(testFault=="rollback-after-first-save"&&i==0){if(!EditorSceneManager.SaveScene(scene,target))throw new InvalidOperationException("Controlled first save failed.");throw new InvalidOperationException("INJECTED_AFTER_FIRST_NATIVE_SAVE");}
                }
                if(!EditorSceneManager.SaveScene(scene,target))throw new InvalidOperationException("Promoted original scene did not save.");EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);scene=EditorSceneManager.OpenScene(target,OpenSceneMode.Single);match=CourseMatch(scene);actors=NativeEightActors(match);proof.savedReloaded=true;proof.profilesExact=true;proof.unitAncestors=true;
                foreach(var row in proof.choices){var actor=actors[row.slot*2+row.index];int species=Array.IndexOf(context.production.species,row.species);row.targetReloaded=NativeReplacementIdentity(actor.gameObject);row.actorReloaded=NativeReplacementIdentity(actor);proof.profilesExact&=row.targetAfter==row.targetReloaded&&row.actorAfter==row.actorReloaded&&NativeEightRootPlacement(actor.transform)==row.rootPlacement&&!actor.gameObject.activeSelf&&actor.GetComponent<DragonRiderSaddle>().rigging.truck==match.trucks[row.slot]&&PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(actor.gameObject)==context.production.actorPrefabs[species]&&ProductionActorSignature(actor,context.aliases[species])==context.signatures[species];for(var t=actor.transform;t!=null;t=t.parent)proof.unitAncestors&=t.localScale.Equals(Vector3.one);}
                proof.externalIdentitiesExact=NativeReplacementIdsAgree(ids,NativeEightProtectedIds(scene,actors));proof.externalAfter=HashTextForNativeReplacement(ProductionSceneConfiguration(scene,new HashSet<GameObject>(actors.Select(a=>a.gameObject))));proof.worldAfter=HashTextForNativeReplacement(NativeReplacementWorldState(match.world));proof.configurationExact=proof.externalBefore==proof.externalAfter;proof.worldExact=proof.worldBefore==proof.worldAfter;var afterNwh=CaptureProductionNwhFields(match);proof.nwhExact=ExactProductionNwhFields(nwh,afterNwh)&&FiniteWorldBoxDimensionsAgree(nwh,afterNwh,out proof.maximumNwhDimensionDrift);proof.noTestDependencies=!AssetDatabase.GetDependencies(target,true).Any(path=>path!=target&&path.Contains("/Tests/Generated/"));proof.sourceClosureExact=plan.protectedSources.All(row=>File.Exists(row.path)&&Hash(row.path)==row.sha256)&&File.ReadAllBytes(target+".meta").SequenceEqual(meta);
                if(!proof.otherSevenEveryStepExact||!proof.externalIdentitiesExact||!proof.configurationExact||!proof.worldExact||!proof.nwhExact||!proof.profilesExact||!proof.unitAncestors||!proof.noTestDependencies||!proof.sourceClosureExact)throw new InvalidOperationException("Saved mount promotion violated qualified profile or protected original identity/config/world/NWH/source closure.");proof.resultSha256=Hash(target);proof.status=disposable?"DISPOSABLE_CANONICAL_PLAN_APPLY_VALIDATED_NOT_CANONICAL":"CANONICAL_MOUNTS_PROMOTED_REQUIRES_COURSE_WORLD_AND_GAMEPLAY_VALIDATION";complete=true;
            }
            catch(Exception error){failure=error;proof.failure=error.ToString();proof.status="REJECTED_CANONICAL_MOUNT_PROMOTION";}
            finally
            {
                void Cleanup(string stage,Action action){try{action();}catch(Exception error){proof.failure+="\nCLEANUP "+stage+": "+error;complete=false;if(failure==null)failure=error;}}
                Cleanup("close-target",()=>{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);});
                Cleanup("restore-editor-context",()=>{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);});Cleanup("source-closure",()=>{proof.sourceClosureExact=plan.protectedSources.All(row=>File.Exists(row.path)&&Hash(row.path)==row.sha256)&&Hash(plan.scene+".meta")==plan.metaSha256&&File.ReadAllBytes(target+".meta").SequenceEqual(meta);if(!proof.sourceClosureExact)throw new InvalidOperationException("Protected source closure changed; do not overwrite unrelated external changes.");});
                Action restoreTarget=()=>{File.WriteAllBytes(target,before);File.WriteAllBytes(target+".meta",meta);AssetDatabase.ImportAsset(target,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);proof.rollbackExact=File.ReadAllBytes(target).SequenceEqual(before)&&File.ReadAllBytes(target+".meta").SequenceEqual(meta);if(!proof.rollbackExact)throw new InvalidOperationException("Owned scene rollback was not byte exact.");proof.resultSha256=Hash(target);};
                if(!complete)Cleanup("restore-owned-target",restoreTarget);if(!complete)proof.status="REJECTED_CANONICAL_MOUNT_PROMOTION";
                Cleanup("write-transaction-receipt",()=>{Directory.CreateDirectory(outputDirectory);File.WriteAllText(receiptPath,JsonUtility.ToJson(proof,true));});
                if(!complete&&!proof.rollbackExact)Cleanup("rollback-after-reporting-failure",restoreTarget);if(!complete)proof.status="REJECTED_CANONICAL_MOUNT_PROMOTION";
            }
            if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();return JsonUtility.ToJson(proof,true);
        }
        public static string ProbeCanonicalMountPromotionPlanApply(string productionEvidence,string courseEvidence,string eightEvidence,string tag,string fault)
        {
            if(string.IsNullOrEmpty(tag)||tag.Length>20||tag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-')||!new[]{"baseline","unknown-last","rollback-after-first-save","stale-plan"}.Contains(fault))throw new InvalidOperationException("Explicit bounded disposable control/tag required.");string planJson=BuildCanonicalKnownMountPromotionPlan(productionEvidence,courseEvidence,eightEvidence);string folder="Assets/TruckDragonRiders/Tests/Generated/CanonicalPromotion-"+tag+"-"+fault,copy=folder+"/Original Riders promotion probe.unity";if(Directory.Exists(folder)||File.Exists(folder+".meta"))throw new InvalidOperationException("Never overwrite prior promotion evidence.");Directory.CreateDirectory(folder);AssetDatabase.Refresh();if(!AssetDatabase.CopyAsset(DragonRidersAuthoring.ScenePath,copy)||Hash(copy)!=Hash(DragonRidersAuthoring.ScenePath))throw new InvalidOperationException("Disposable promotion source must be byte-identical.");
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1>=args.Length)throw new InvalidOperationException("Explicit actual native evidence output required.");string output=Path.Combine(args[at+1],"RiderSaddleAuthoring","CanonicalMountPromotion");if(fault=="stale-plan"){var plan=JsonUtility.FromJson<CanonicalMountPlan>(planJson);plan.sceneSha256="STALE";planJson=JsonUtility.ToJson(plan);}
            lastCanonicalMountApply=null;try{return ApplyKnownMountPromotion(planJson,copy,output,fault=="baseline"?null:fault,true);}catch(InvalidOperationException error)
            {
                if(fault=="stale-plan"&&lastCanonicalMountApply==null&&error.Message.Contains("Stale or changed"))return "{\"status\":\"STALE_PLAN_REJECTED_BEFORE_WRITE\",\"canonicalTarget\":false,\"nativeCalls\":0}";
                if(lastCanonicalMountApply==null||!lastCanonicalMountApply.rollbackExact||!lastCanonicalMountApply.sourceClosureExact||fault=="baseline")throw;if(fault=="unknown-last"&&(lastCanonicalMountApply.nativeCalls!=0||!error.Message.Contains("Unknown original choice edit")))throw;if(fault=="rollback-after-first-save"&&(lastCanonicalMountApply.nativeCalls!=1||!error.Message.Contains("INJECTED_AFTER_FIRST_NATIVE_SAVE")))throw;return JsonUtility.ToJson(lastCanonicalMountApply,true);
            }
        }
    }
}
#endif
