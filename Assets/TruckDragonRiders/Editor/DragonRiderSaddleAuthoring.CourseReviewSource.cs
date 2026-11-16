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
        [Serializable]public sealed class QualifiedCourseRigSource
        {
            public int crew,recordedSourcePoses,skinPoints,recordedMixerLabelledPoses;public string species,controller,controllerSha256,envelopeAuthority;
            public Bounds headingBounds;public float anyAttitudeRadius;public Vector3 neutralCrossingOffset;public string[] clips,clipSha256,poseLabels;
        }
        [Serializable]public sealed class QualifiedCourseRootState{public int index;public string name;public bool active;}
        [Serializable]public sealed class QualifiedCourseSourceFile{public string path,sha256;}
        [Serializable]public sealed class QualifiedCourseSource
        {
            public string scene,sceneSha256,productionEvidence,productionReceiptSha256,physicalCourseEvidence,sourceScene,status="IN_PROGRESS",failure,cleanupFailure;
            public bool candidateClosedForComparison,comparisonClosedBeforeCandidateReopen,candidateRestored,rootActivationExact,currentSourceBytesExact;public QualifiedCourseRootState[] rootStates;
            public bool currentConfigurationExact,currentProfilesExact,currentNwhExact,unitAncestors,noTestDependencies;
            public bool canonicalSourceReview;public string canonicalSourceCertificate;
            public float maximumNwhDimensionDrift,straightExitDistance;public List<QualifiedCourseRigSource> rigs=new List<QualifiedCourseRigSource>();public QualifiedCourseSourceFile[] currentSourceSnapshot;
            public string scope="CURRENT_SAVED_PRODUCTION_PROFILE_AND_ORIGINAL_NWH_EQUIVALENCE_REUSES_PAIRED_FULL_NATIVE_BOX_CENTERED_GEOMETRY_NOT_NEW_FLIGHT_OR_CANONICAL_ADOPTION";
        }
        static MountedProductionCandidateReceipt ReadQualifiedCourseProduction(string evidence)
        {
            RequireRiderPassedManifest(evidence,"DragonRiderProductionCandidateTests.SavedQualifiedRiderCandidateRetainsCrewNwhAndProductionOnlyMountReferences");
            string path=Path.Combine(evidence,"rendered-evidence","RiderSaddleAuthoring","ProductionCandidate","qualified-mounted-scene.json");var proof=JsonUtility.FromJson<MountedProductionCandidateReceipt>(File.ReadAllText(path));
            if(proof==null||!string.IsNullOrEmpty(proof.failure)||!proof.savedReloaded||!proof.originalSourcesExact||!proof.sceneConfigurationExact||!proof.nwhFieldsExact||!proof.unitAncestors||!proof.profilesExact||!proof.noTestAssetDependencies||proof.originalTrucks!=4||proof.replacedChoices!=8||proof.species==null||!new HashSet<string>(proof.species).SetEquals(new[]{"dragon-inferno","dragon-fire"})||proof.species.Length!=2||proof.producerEvidence.Length!=2||proof.runtimeEvidence.Length!=2||proof.actorPrefabs.Length!=2||proof.controllers.Length!=2||!proof.candidateScene.StartsWith(ProductionScenePrefix,StringComparison.Ordinal)||proof.candidateScene.Contains("..")||!File.Exists(proof.candidateScene))throw new InvalidOperationException("Actual passed two-species/eight-choice production candidate required; no arbitrary source scene.");return proof;
        }
        public static string QualifiedMountedCourseScenePath(string productionEvidence)=>PhysicalCourseReviewSourcePath(productionEvidence);
        static QualifiedCourseSource ValidateQualifiedMountedCourseSourceLegacy(ref Scene candidate,string productionEvidence,string physicalCourseEvidence)
        {
            var result=new QualifiedCourseSource{scene=candidate.path,productionEvidence=productionEvidence,physicalCourseEvidence=physicalCourseEvidence};MountedProductionCandidateReceipt proof=null;Scene source=default;Exception failure=null;bool closedCandidate=false;
            try
            {
                proof=ReadQualifiedCourseProduction(productionEvidence);if(!candidate.IsValid()||!candidate.isLoaded||candidate.path!=proof.candidateScene||candidate.isDirty)throw new InvalidOperationException("Only a clean, probe-owned exact saved production scene may be measured and reopened.");
                if(Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Any(scene=>scene.path==proof.sourceScene))throw new InvalidOperationException("Close the original physical-course scene before read-only qualification; never close a user-owned open source.");
                var mounts=proof.producerEvidence.Select((folder,i)=>RequireQualifiedProductionMount(folder,proof.runtimeEvidence[i])).ToArray();RequireProductionSourceScene(proof.sourceScene,mounts[0].producerManifest,physicalCourseEvidence);
                string receiptPath=Path.Combine(productionEvidence,"rendered-evidence","RiderSaddleAuthoring","ProductionCandidate","qualified-mounted-scene.json");result.sceneSha256=Hash(proof.candidateScene);result.productionReceiptSha256=Hash(receiptPath);result.sourceScene=proof.sourceScene;
                var watched=proof.protectedSources.Select(p=>CurrentRiderPhysicalDependency(p.path)).Concat(AssetDatabase.GetDependencies(proof.candidateScene,true)).Concat(new[]{proof.candidateScene,proof.sourceScene,receiptPath,Path.Combine(productionEvidence,"tests.xml")}).SelectMany(path=>new[]{path,path+".meta"}).Where(File.Exists).Distinct().ToArray();result.currentSourceSnapshot=watched.Select(path=>new QualifiedCourseSourceFile{path=path,sha256=Hash(path)}).ToArray();
                var match=CourseMatch(candidate);var candidateConfiguration=ProductionSceneConfiguration(candidate,new HashSet<GameObject>(match.racers.SelectMany(r=>r.choices).Select(a=>a.gameObject)));var candidateFields=CaptureProductionNwhFields(match);result.rootStates=candidate.GetRootGameObjects().Select((root,index)=>new QualifiedCourseRootState{index=index,name=root.name,active=root.activeSelf}).ToArray();result.straightExitDistance=match.aiStraightGateExitDistance;
                if(match.racers.Length!=4||match.trucks.Length!=4||match.trucks.Distinct().Count()!=4||match.gates.Length!=12||!float.IsFinite(result.straightExitDistance)||result.straightExitDistance!=55)throw new InvalidOperationException("Current production candidate changed original crew/ordered course or qualified55m exit.");
                result.currentProfilesExact=true;result.unitAncestors=true;var signatures=new Dictionary<string,string>();var aliasSets=new Dictionary<string,Dictionary<UnityEngine.Object,string>>();
                for(int speciesIndex=0;speciesIndex<mounts.Length;speciesIndex++)
                {
                    var mount=mounts[speciesIndex];if(mount.species!=proof.species[speciesIndex])throw new InvalidOperationException("Production species/evidence ordering changed.");var aliases=QualifiedCourseControllerAliases(mount.fixture.controller,proof.controllers[speciesIndex]);
                    var prefab=PrefabUtility.LoadPrefabContents(mount.fixture.prefab);string signature;Vector3 origin;Quaternion orientation;
                    try{var actor=prefab.GetComponent<DragonCharacter>();signature=ProductionActorSignature(actor,aliases);origin=actor.motor.transform.position;orientation=actor.motor.transform.rotation;}finally{PrefabUtility.UnloadPrefabContents(prefab);}
                    signatures.Add(mount.species,signature);aliasSets.Add(mount.species,aliases);
                    var nativeFiles=Directory.GetFiles(Path.Combine(proof.producerEvidence[speciesIndex],"rendered-evidence"),"precise-motor-root-*-course-envelope.json",SearchOption.AllDirectories);var native=nativeFiles.Select(p=>JsonUtility.FromJson<MountedSizeReport>(File.ReadAllText(p))).Single(r=>r.testOnlyColdFixture?.species==mount.species);var row=native.profiles.Single();var envelope=row.actualCourseEnvelope;
                    if(envelope==null||!envelope.hasBounds||envelope.poses!=row.actualPoses||envelope.poses!=(mount.species=="dragon-fire"?537:699)||envelope.crossingCenterAuthority!="CURRENT_AUTHORED_BOX_CENTER_TRANSFORMED_BY_CURRENT_LOCAL_TO_WORLD_NOT_PHYSX_CACHE"||envelope.crossingCenters==null||envelope.crossingCenters.Count!=envelope.poses||envelope.crossingCenters.Any(c=>string.IsNullOrEmpty(c.pose)||Vector3.Distance(c.currentLocalToWorld.MultiplyPoint3x4(c.authoredLocalCenter),c.currentMatrixCenter)>.0001f)||envelope.nwhMeshPoints==0||envelope.nwhBoxCorners==0||envelope.harnessLineSamples==0)throw new InvalidOperationException("Complete actually passed source geometry/current box-center/harness witnesses required; no stale PhysX bounds.");
                    var labels=envelope.crossingCenters.Select(c=>c.pose).ToArray();int mixers=labels.Count(label=>label.IndexOf("blend",StringComparison.OrdinalIgnoreCase)>=0||label.IndexOf("two-layer",StringComparison.OrdinalIgnoreCase)>=0);if(mixers==0)throw new InvalidOperationException("Full native geometry must retain actual graph/action mixer coverage.");
                    var controller=AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(proof.controllers[speciesIndex]);var clips=new List<KeyValuePair<AnimationClip,AnimationClip>>();controller.GetOverrides(clips);var paths=clips.Select(pair=>pair.Value==null?pair.Key:pair.Value).Distinct().Select(AssetDatabase.GetAssetPath).ToArray();
                    for(int slot=0;slot<4;slot++)
                    {
                        var racer=match.racers[slot];int index=Array.IndexOf(racer.choiceIds,mount.species);if(index<0||racer.choiceIds.Length!=racer.choices.Length||racer.match!=match||racer.truck!=match.trucks[slot])throw new InvalidOperationException("Original crew/truck/species ownership changed.");var actor=racer.choices[index];
                        result.currentProfilesExact&=!actor.gameObject.activeSelf&&actor.GetComponent<DragonRiderSaddle>().rigging.truck==racer.truck&&ProductionActorSignature(actor,aliases)==signature&&PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(actor.gameObject)==proof.actorPrefabs[speciesIndex];for(var t=actor.transform;t!=null;t=t.parent)result.unitAncestors&=t.localScale.Equals(Vector3.one);
                        result.rigs.Add(new QualifiedCourseRigSource{crew=slot,species=mount.species,recordedSourcePoses=envelope.poses,skinPoints=envelope.skinPoints,recordedMixerLabelledPoses=mixers,headingBounds=envelope.actorHeadingRelativeBounds,anyAttitudeRadius=envelope.maximumAnyAttitudeVertexRadius,neutralCrossingOffset=Quaternion.Inverse(orientation)*(envelope.crossingCenters[0].currentMatrixCenter-origin),controller=proof.controllers[speciesIndex],controllerSha256=Hash(proof.controllers[speciesIndex]),clips=paths,clipSha256=paths.Select(Hash).ToArray(),poseLabels=labels,envelopeAuthority=envelope.crossingCenterAuthority});
                    }
                }
                result.noTestDependencies=!AssetDatabase.GetDependencies(candidate.path,true).Any(path=>path.Contains("/Tests/Generated/"));if(!result.currentProfilesExact||!result.unitAncestors||!result.noTestDependencies||result.rigs.Count!=8)throw new InvalidOperationException("Current saved profiles/unit ancestors/no-test-dependency qualification failed.");
                // Installed Weather Maker releases its static instances in
                // OnDestroy, NOT OnDisable. Genuine sequential scene closure
                // is required; deactivation or private-static manipulation is
                // neither sufficient nor an acceptable lifecycle substitute.
                if(!EditorSceneManager.CloseScene(candidate,true))throw new InvalidOperationException("Probe-owned candidate did not close before comparison.");candidate=default;closedCandidate=true;result.candidateClosedForComparison=true;
                source=EditorSceneManager.OpenScene(proof.sourceScene,OpenSceneMode.Additive);var before=CourseMatch(source);result.currentConfigurationExact=ProductionSceneConfiguration(source,new HashSet<GameObject>(before.racers.SelectMany(r=>r.choices).Select(a=>a.gameObject)))==candidateConfiguration;var fields=CaptureProductionNwhFields(before);result.currentNwhExact=ExactProductionNwhFields(fields,candidateFields)&&FiniteWorldBoxDimensionsAgree(fields,candidateFields,out result.maximumNwhDimensionDrift);
                if(!result.currentConfigurationExact||!result.currentNwhExact)throw new InvalidOperationException("Current production candidate differs from the original saved physical course/configuration/NWH authority.");
                if(!EditorSceneManager.CloseScene(source,true))throw new InvalidOperationException("Comparison source did not close before candidate restoration.");source=default;result.comparisonClosedBeforeCandidateReopen=true;candidate=EditorSceneManager.OpenScene(proof.candidateScene,OpenSceneMode.Additive);RestoreQualifiedCourseRootStates(candidate,result);result.candidateRestored=true;match=CourseMatch(candidate);
                result.currentConfigurationExact&=ProductionSceneConfiguration(candidate,new HashSet<GameObject>(match.racers.SelectMany(r=>r.choices).Select(a=>a.gameObject)))==candidateConfiguration;float reopenedDrift=0;result.currentNwhExact&=ExactProductionNwhFields(candidateFields,CaptureProductionNwhFields(match))&&FiniteWorldBoxDimensionsAgree(candidateFields,CaptureProductionNwhFields(match),out reopenedDrift);result.maximumNwhDimensionDrift=Mathf.Max(result.maximumNwhDimensionDrift,reopenedDrift);
                foreach(var mount in mounts)for(int slot=0;slot<4;slot++){var racer=match.racers[slot];int index=Array.IndexOf(racer.choiceIds,mount.species);var actor=racer.choices[index];result.currentProfilesExact&=actor.GetComponent<DragonRiderSaddle>().rigging.truck==match.trucks[slot]&&ProductionActorSignature(actor,aliasSets[mount.species])==signatures[mount.species];}
                if(!result.currentConfigurationExact||!result.currentNwhExact||!result.currentProfilesExact||!result.rootActivationExact)throw new InvalidOperationException("Genuine scene reopen changed current profile/config/NWH/root activation state.");result.status="CURRENT_SOURCE_SEMANTIC_QUALIFICATION_COMPLETE_NATIVE_TEST_RESULT_REMAINS_SEPARATE";
            }
            catch(Exception error){failure=error;result.failure=error.ToString();result.status="REJECTED_CURRENT_SOURCE_QUALIFICATION";}
            finally
            {
                void Cleanup(string stage,Action action){try{action();}catch(Exception error){result.cleanupFailure+=(stage+": "+error+"\n");if(failure==null){failure=error;result.failure=error.ToString();}result.status="REJECTED_CURRENT_SOURCE_QUALIFICATION";}}
                // Always destroy the comparison singleton before any reopen.
                Cleanup("close-comparison",()=>{var owned=source.IsValid()&&source.isLoaded?source:proof==null?default:SceneManager.GetSceneByPath(proof.sourceScene);if(owned.IsValid()&&owned.isLoaded){if(!EditorSceneManager.CloseScene(owned,true))throw new InvalidOperationException("Comparison close failed.");}source=default;if(closedCandidate)result.comparisonClosedBeforeCandidateReopen=true;});
                if(closedCandidate){var ownedCandidate=candidate;Cleanup("restore-owned-candidate",()=>{if(!ownedCandidate.IsValid()||!ownedCandidate.isLoaded)ownedCandidate=EditorSceneManager.OpenScene(proof.candidateScene,OpenSceneMode.Additive);RestoreQualifiedCourseRootStates(ownedCandidate,result);result.candidateRestored=true;});candidate=ownedCandidate;}
                Cleanup("current-source-bytes",()=>{result.currentSourceBytesExact=result.currentSourceSnapshot!=null&&result.currentSourceSnapshot.All(row=>File.Exists(row.path)&&Hash(row.path)==row.sha256);if(result.currentSourceSnapshot!=null&&!result.currentSourceBytesExact)throw new InvalidOperationException("Read-only qualification changed actual source bytes.");});
                var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at>=0&&at+1<args.Length)Cleanup("retain-native-proof",()=>{string output=Path.Combine(args[at+1],"RiderSaddleAuthoring","QualifiedCourseSource");Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"actual-current-production-source-qualification.json"),JsonUtility.ToJson(result,true));});
            }
            if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();return result;
        }
        // Preserve the existing pure path-normalization contract. It grants
        // no scene/source certificate and does not alter the legacy caller.
        static string[] QualifiedCourseSnapshotPaths(IEnumerable<string> paths,bool canonical)
            =>canonical?paths.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray():paths.Distinct().ToArray();
        static void RestoreQualifiedCourseRootStates(Scene scene,QualifiedCourseSource result)
        {
            var roots=scene.GetRootGameObjects();if(result.rootStates==null||roots.Length!=result.rootStates.Length)throw new InvalidOperationException("Probe-owned candidate root membership changed during reopen.");foreach(var state in result.rootStates){if(state.index<0||state.index>=roots.Length||roots[state.index].name!=state.name)throw new InvalidOperationException("Probe-owned candidate root identity/order changed during reopen.");if(roots[state.index].activeSelf!=state.active)roots[state.index].SetActive(state.active);}result.rootActivationExact=result.rootStates.All(state=>roots[state.index].activeSelf==state.active);
        }
        static Dictionary<UnityEngine.Object,string> QualifiedCourseControllerAliases(string original,string promoted)
        {
            var source=AssetDatabase.LoadAllAssetsAtPath(original);var copies=AssetDatabase.LoadAllAssetsAtPath(promoted);if(source.Length!=copies.Length||source.Length<2)throw new InvalidOperationException("Complete native controller/subclip copy membership required.");var byId=copies.ToDictionary(o=>{AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o,out string _,out long id);return id;});var aliases=new Dictionary<UnityEngine.Object,string>();var pairs=new Dictionary<UnityEngine.Object,UnityEngine.Object>();
            foreach(var value in source){AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value,out string _,out long id);if(!byId.TryGetValue(id,out var copy)||copy.GetType()!=value.GetType())throw new InvalidOperationException("Promoted controller subasset ID/type differs from qualified source.");aliases.Add(value,"qualified-controller:"+id);aliases.Add(copy,"qualified-controller:"+id);pairs.Add(value,copy);if(value is AnimationClip a&&CarrierClipMismatch(a,(AnimationClip)copy)!=null)throw new InvalidOperationException("Promoted clip differs from full qualified motion.");}
            var aoc=AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(original);var b=AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(promoted);var aa=new List<KeyValuePair<AnimationClip,AnimationClip>>();var bb=new List<KeyValuePair<AnimationClip,AnimationClip>>();aoc.GetOverrides(aa);b.GetOverrides(bb);if(aoc.runtimeAnimatorController!=b.runtimeAnimatorController||aa.Count!=bb.Count)throw new InvalidOperationException("Promoted native controller graph/override membership changed.");for(int i=0;i<aa.Count;i++)if(aa[i].Key!=bb[i].Key||(aa[i].Value!=null&&pairs.TryGetValue(aa[i].Value,out var mapped)?mapped:aa[i].Value)!=bb[i].Value)throw new InvalidOperationException("Promoted effective clip binding changed.");return aliases;
        }
    }
}
#endif
