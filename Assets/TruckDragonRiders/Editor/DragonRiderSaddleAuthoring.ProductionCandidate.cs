#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LWS.TruckDragonKingdom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        [Serializable] sealed class MountedProductionCandidateReceipt
        {
            public string scope="EXPLICIT_SAVED_MOUNT_CANDIDATE_NOT_CANONICAL_COURSE_FOUR_PLAYER_RUNTIME_WINDOWS_OR_HUMAN_ACCEPTANCE",sourceScene,candidateScene,assetFolder,failure;
            public string[] species,producerEvidence,runtimeEvidence,actorPrefabs,controllers;
            public bool savedReloaded,originalSourcesExact,sceneConfigurationExact,nwhFieldsExact,unitAncestors,profilesExact,noTestAssetDependencies;
            public int originalTrucks,replacedChoices;public long protectedSourceBytes;public float maximumNwhDimensionDrift;public List<ProductionStepTiming> timings;
            public FlightSourceHash[] protectedSources;
        }
        [Serializable]sealed class ProductionStepTiming{public string phase;public double elapsedSeconds,stepSeconds;}
        sealed class QualifiedProductionMount
        {
            public ColdFixtureReceipt fixture;public ColdRuntimeReceipt runtime;public FlightSourceHash[] producerManifest;public string species,actorPath,controllerPath;
            public Dictionary<UnityEngine.Object,string> controllerTokens=new Dictionary<UnityEngine.Object,string>();
            public string signature;
        }
        const string ProductionAssetPrefix="Assets/TruckDragonRiders/Animations/Authored/QualifiedMounted-";
        const string ProductionScenePrefix="Assets/TruckDragonRiders/ReviewCandidates/QualifiedMounted-";

        // Explicit authoring only. Builds and normal scene initialization never
        // promote a test lease, recalibrate a mount, or overwrite designer data.
        public static string CreateQualifiedMountedRidersSceneCandidate(string sourceScene,string[] producerEvidence,string[] runtimeEvidence,string candidateTag,string physicalCourseEvidence=null)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required for explicit mount candidate authoring.");
            var watch=System.Diagnostics.Stopwatch.StartNew();double previousStep=0;var timings=new List<ProductionStepTiming>();
            void Mark(string phase){double now=watch.Elapsed.TotalSeconds;timings.Add(new ProductionStepTiming{phase=phase,elapsedSeconds=now,stepSeconds=now-previousStep});previousStep=now;Debug.Log("RIDERS_PRODUCTION_AUTHORING_STAGE "+phase+" elapsed="+now.ToString("F3",System.Globalization.CultureInfo.InvariantCulture));}

            if(candidateTag==null||!Regex.IsMatch(candidateTag,"^[A-Za-z0-9][A-Za-z0-9-]{0,47}$"))throw new InvalidOperationException("Use a unique1–48character alphanumeric/hyphen candidate tag.");
            if(producerEvidence==null||runtimeEvidence==null||producerEvidence.Length<1||producerEvidence.Length>2||producerEvidence.Length!=runtimeEvidence.Length)throw new InvalidOperationException("One or two paired actual producer/runtime evidence folders required.");
            sourceScene=sourceScene.Replace('\\','/');
            for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(s.isDirty||string.IsNullOrEmpty(s.path)||s.path==sourceScene)throw new InvalidOperationException("Preserve incoming scenes: save them and close the explicit source scene before candidate authoring.");}
            string assets=ProductionAssetPrefix+candidateTag,sceneFolder=ProductionScenePrefix+candidateTag;
            if(Directory.Exists(assets)||File.Exists(assets+".meta")||Directory.Exists(sceneFolder)||File.Exists(sceneFolder+".meta"))throw new InvalidOperationException("Candidate outputs must be absent; never overwrite authored mounts, scenes or prior evidence.");
            var mounts=producerEvidence.Select((folder,i)=>RequireQualifiedProductionMount(folder,runtimeEvidence[i])).ToArray();Mark("paired-native-qualification");
            if(mounts.Select(m=>m.species).Distinct().Count()!=mounts.Length)throw new InvalidOperationException("A species may be promoted only once per candidate.");
            RequireProductionSourceScene(sourceScene,mounts[0].producerManifest,physicalCourseEvidence);
            var protectedPaths=mounts.SelectMany(m=>m.fixture.sourceBefore.Select(p=>p.path).Concat(m.fixture.fixtureAssets.Select(p=>p.path))).Concat(new[]{sourceScene,sourceScene+".meta"}).Distinct().ToArray();
            var proof=new MountedProductionCandidateReceipt{sourceScene=sourceScene,candidateScene=sceneFolder+"/Qualified mounted Riders.unity",assetFolder=assets,timings=timings,protectedSourceBytes=protectedPaths.Sum(p=>new FileInfo(CurrentRiderPhysicalDependency(p)).Length),species=mounts.Select(m=>m.species).ToArray(),producerEvidence=producerEvidence,runtimeEvidence=runtimeEvidence,protectedSources=protectedPaths.Select(p=>new FlightSourceHash{path=p,sha256=Hash(CurrentRiderPhysicalDependency(p))}).ToArray()};
            Mark("initial-protected-source-hashes");var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;Exception failure=null;
            try
            {
                Directory.CreateDirectory(assets);Directory.CreateDirectory(sceneFolder);AssetDatabase.Refresh();
                foreach(var mount in mounts){PromoteQualifiedProductionMount(mount,assets);Mark("copy-qualified-actor-"+mount.species);}
                proof.actorPrefabs=mounts.Select(m=>m.actorPath).ToArray();proof.controllers=mounts.Select(m=>m.controllerPath).ToArray();
                scene=EditorSceneManager.OpenScene(sourceScene,OpenSceneMode.Additive);var match=CourseMatch(scene);
                if(match.racers.Length!=4||match.trucks.Length!=4||match.trucks.Distinct().Count()!=4)throw new InvalidOperationException("Exactly four original, distinct NWH truck/racer bindings required.");
                proof.originalTrucks=match.trucks.Length;var fields=CaptureProductionNwhFields(match);var oldChoices=match.racers.SelectMany(r=>r.choices).ToArray();var oldRoots=new HashSet<GameObject>(oldChoices.Select(a=>a.gameObject));
                string configuration=ProductionSceneConfiguration(scene,oldRoots);Mark("initial-scene-and-nwh-signatures");var oldNames=match.racers.Select(r=>r.name).ToArray();
                foreach(var mount in mounts)for(int slot=0;slot<4;slot++)
                {
                    var racer=match.racers[slot];if(racer.truck!=match.trucks[slot]||racer.match!=match)throw new InvalidOperationException("Original racer/truck/host ownership mismatch.");
                    int selected=Array.IndexOf(racer.choiceIds,mount.species);if(selected<0||racer.choiceIds.Length!=racer.choices.Length)throw new InvalidOperationException("Preserve matching original species slots; no implicit roster redesign.");
                    var old=racer.choices[selected];RequireNoExternalMountReferences(scene,old,racer,selected);Mark("external-references-"+mount.species+"-slot"+slot);
                    var parent=old.transform.parent;for(var t=parent;t!=null;t=t.parent)if(!t.localScale.Equals(Vector3.one))throw new InvalidOperationException("Unit original mount ancestors required; never compensate by scaling NWH.");
                    int sibling=old.transform.GetSiblingIndex();var replacement=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(mount.actorPath),parent);
                    replacement.name=old.name;replacement.transform.SetLocalPositionAndRotation(old.transform.localPosition,old.transform.localRotation);replacement.transform.localScale=Vector3.one;replacement.transform.SetSiblingIndex(sibling);replacement.SetActive(false);
                    var actor=replacement.GetComponent<DragonCharacter>();actor.GetComponent<DragonRiderSaddle>().rigging.truck=racer.truck;racer.choices[selected]=actor;EditorUtility.SetDirty(racer);UnityEngine.Object.DestroyImmediate(old.gameObject);proof.replacedChoices++;
                }
                var newRoots=new HashSet<GameObject>(match.racers.SelectMany(r=>r.choices).Select(a=>a.gameObject));
                proof.sceneConfigurationExact=configuration==ProductionSceneConfiguration(scene,newRoots)&&match.racers.Select(r=>r.name).SequenceEqual(oldNames);
                proof.nwhFieldsExact=ExactProductionNwhFields(fields,CaptureProductionNwhFields(match))&&FiniteWorldBoxDimensionsAgree(fields,CaptureProductionNwhFields(match),out proof.maximumNwhDimensionDrift);
                Mark("changed-scene-and-nwh-signatures");if(!proof.sceneConfigurationExact||!proof.nwhFieldsExact)throw new InvalidOperationException("Candidate changed original NWH, setup, inputs, crew, world, course, cameras or presentation references.");
                if(!EditorSceneManager.SaveScene(scene,proof.candidateScene,true))throw new InvalidOperationException("Separate qualified mount candidate scene did not save.");
                EditorSceneManager.CloseScene(scene,true);scene=EditorSceneManager.OpenScene(proof.candidateScene,OpenSceneMode.Additive);match=CourseMatch(scene);proof.savedReloaded=true;Mark("save-reload-candidate-scene");
                newRoots=new HashSet<GameObject>(match.racers.SelectMany(r=>r.choices).Select(a=>a.gameObject));proof.sceneConfigurationExact&=configuration==ProductionSceneConfiguration(scene,newRoots);
                proof.nwhFieldsExact&=ExactProductionNwhFields(fields,CaptureProductionNwhFields(match))&&FiniteWorldBoxDimensionsAgree(fields,CaptureProductionNwhFields(match),out proof.maximumNwhDimensionDrift);
                Mark("reloaded-scene-and-nwh-signatures");proof.unitAncestors=true;proof.profilesExact=true;
                foreach(var mount in mounts)for(int slot=0;slot<4;slot++)
                {
                    var racer=match.racers[slot];int selected=Array.IndexOf(racer.choiceIds,mount.species);var actor=racer.choices[selected];var saddle=actor.GetComponent<DragonRiderSaddle>();
                    proof.profilesExact&=saddle.rigging.truck==match.trucks[slot]&&ProductionActorSignature(actor,mount.controllerTokens)==mount.signature&&!actor.gameObject.activeSelf;
                    for(var t=actor.transform;t!=null;t=t.parent)proof.unitAncestors&=t.localScale.Equals(Vector3.one);
                }
                Mark("reloaded-qualified-actor-profiles");var deps=AssetDatabase.GetDependencies(proof.candidateScene,true);proof.noTestAssetDependencies=!deps.Any(p=>p.Contains("/Tests/Generated/"));
                if(!proof.sceneConfigurationExact||!proof.nwhFieldsExact||!proof.unitAncestors||!proof.profilesExact||!proof.noTestAssetDependencies)throw new InvalidOperationException("Saved/reloaded candidate failed original configuration/profile/unit-parent/no-test-dependency checks.");
                if(scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0))throw new InvalidOperationException("Candidate contains a missing script.");
            }
            catch(Exception error){failure=error;proof.failure=error.ToString();}
            finally
            {
                if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);
                proof.originalSourcesExact=proof.protectedSources.All(p=>File.Exists(CurrentRiderPhysicalDependency(p.path))&&Hash(CurrentRiderPhysicalDependency(p.path))==p.sha256);
                Mark("final-protected-source-hashes");var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at>=0&&at+1<args.Length){string folder=Path.Combine(args[at+1],"RiderSaddleAuthoring","ProductionCandidate");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"qualified-mounted-scene.json"),JsonUtility.ToJson(proof,true));}
            }
            if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();if(!proof.originalSourcesExact)throw new InvalidOperationException("Candidate changed a protected original source/fixture; no adoption permitted.");return JsonUtility.ToJson(proof,true);
        }

        static QualifiedProductionMount RequireQualifiedProductionMount(string producerFolder,string runtimeFolder)
        {
            var runtimePaths=Directory.GetFiles(Path.Combine(runtimeFolder,"rendered-evidence"),"pristine-first-awake-runtime.json",SearchOption.AllDirectories);if(runtimePaths.Length!=1)throw new InvalidOperationException("One actual cold-runtime receipt required.");var runtime=JsonUtility.FromJson<ColdRuntimeReceipt>(File.ReadAllText(runtimePaths.Single()));
            var reports=Directory.GetFiles(Path.Combine(producerFolder,"rendered-evidence"),"precise-motor-root-*-course-envelope.json",SearchOption.AllDirectories).Select(p=>JsonUtility.FromJson<MountedSizeReport>(File.ReadAllText(p))).Where(p=>p.testOnlyColdFixture!=null&&p.testOnlyColdFixture.species==runtime.species).ToArray();
            if(reports.Length!=1)throw new InvalidOperationException("Use one species-specific actual producer evidence folder.");
            var report=reports.Single();var fixture=report.testOnlyColdFixture;if(fixture==null||fixture.species!="dragon-inferno"&&fixture.species!="dragon-fire")throw new InvalidOperationException("A qualified inactive supported-species fixture is required.");
            bool fire=fixture.species=="dragon-fire";int poses=fire?537:699;float scale=fire?7*1.67f:17;
            string producerTest=fire?"DragonRiderMotorRootRepresentationTests.UnitAncestorsAndInactiveFireMotorRootRetainFull537GeometryAndExportPristineTestFixture":"DragonRiderMotorRootRepresentationTests.UnitAncestorsAndInactiveConfiguredMotorRootRetainFull699GeometryAndMeasureActualTruckCenteredCourseEnvelope";
            var producer=RequireRiderPassedManifest(producerFolder,producerTest);RequireRiderPassedManifest(runtimeFolder,fire?"NativeDragonInputTests.RidersPristineFireMotor1169NaturallyAwakensAndPreservesActualCommandedCruiseCabBreathAndSoloRematches":"NativeDragonInputTests.RidersPristineMotor17NaturallyAwakensAndPreservesActualWorldSpeedCabBreathAndSoloRematches");
            var row=report.profiles.Single();if(!report.sourceBytesExact||!report.nwhExact||!report.sceneBytesExact||row.actualPoses!=poses||row.candidateSatPairs!=0||!row.exactOriginalBoxAcceptance||!row.originalFootprintFits||!fixture.sameCandidatePassedAllPoses||!fixture.configuredInactive||!fixture.pristineAuthoredStagingRestoredForSerialization||!fixture.sourceBytesExact||!fixture.nwhClosureExact||fixture.expectedPoses!=poses||!fixture.motorScale.Equals(Vector3.one*scale)||fixture.materialPoints!=5)throw new InvalidOperationException("Fresh complete geometry/pristine export/source/NWH gates required before promotion.");
            foreach(var asset in fixture.fixtureAssets)if(!File.Exists(asset.path)||Hash(asset.path)!=asset.sha256)throw new InvalidOperationException("Qualified fixture asset changed: "+asset.path);
            
            if(runtime.species!=fixture.species||runtime.expectedPoses!=poses||runtime.fixture!=fixture.prefab||Path.GetFullPath(runtime.evidence)!=Path.GetFullPath(producerFolder)||!string.IsNullOrEmpty(runtime.failure)||!string.IsNullOrEmpty(runtime.adapterFailure)||!runtime.sourceContextQualified||!runtime.inactiveBeforeFirstAwake||!runtime.nativeAwakeObserved||!runtime.nativeInitialized||!runtime.adapterStartObserved||!runtime.runtimeStateOwnership||!runtime.authoredStateSetsUntouched||!runtime.nwhExact||!runtime.finiteWorldBoxDimensionsPreserved||!runtime.nwhClosureExact||!runtime.restored)throw new InvalidOperationException("The SAME pristine species fixture must pass genuine native lifecycle/input/cab/Breath/rematches before production promotion.");
            var runtimeManifest=RequireRiderPassedManifest(runtimeFolder,fire?"NativeDragonInputTests.RidersPristineFireMotor1169NaturallyAwakensAndPreservesActualCommandedCruiseCabBreathAndSoloRematches":"NativeDragonInputTests.RidersPristineMotor17NaturallyAwakensAndPreservesActualWorldSpeedCabBreathAndSoloRematches");
            foreach(string path in new[]{ColdAdapterSourcePath}){string current=Hash(CurrentRiderPhysicalDependency(path));if(!string.Equals(producer.Single(p=>p.path==path).sha256,current,StringComparison.OrdinalIgnoreCase)||!string.Equals(runtimeManifest.Single(p=>p.path==path).sha256,current,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Production native-speed adapter differs from the actual paired producer/runtime source: "+path);}
            // Runtime code and actual source data must still match the qualified
            // observation; unrelated stopped-Editor/test fixes are not a new rig.
            foreach(var source in fixture.sourceBefore.Where(p=>!p.path.Replace('\\','/').Contains("/Editor/")&&!p.path.Replace('\\','/').Contains("/Tests/")))
            {
                string path=source.path.Replace('\\','/'),expected=source.sha256;
                if(path==ColdAdapterSourcePath)expected=runtimeManifest.Single(p=>p.path==path).sha256;
                if(path==ColdMountedCabSourcePath&&!string.Equals(expected,runtimeManifest.Single(p=>p.path==path).sha256,StringComparison.OrdinalIgnoreCase))
                {
                    var proof=runtime.consumerEvolution;if(proof==null||!proof.qualified||!proof.mountedCabQualified||string.IsNullOrEmpty(proof.reviewPath)||!string.Equals(Hash(proof.reviewPath),proof.reviewSha256,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Paired runtime mounted-cab qualification missing or changed.");
                    var review=JsonUtility.FromJson<ColdConsumerEvolutionReview>(File.ReadAllText(proof.reviewPath));if(Path.GetFullPath(review.nativeEvidence)!=Path.GetFullPath(runtimeFolder)||Path.GetFullPath(review.producerEvidence)!=Path.GetFullPath(producerFolder))throw new InvalidOperationException("Mounted cab review belongs to another native job.");
                    expected=runtimeManifest.Single(p=>p.path==path).sha256;ValidateReviewedMountedCab(review,proof,source.sha256,expected,producer,runtimeManifest);
                }
                if(!File.Exists(CurrentRiderPhysicalDependency(path))||!string.Equals(Hash(CurrentRiderPhysicalDependency(path)),expected,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Qualified runtime/source data changed before promotion: "+path);
            }
            foreach(var dependency in fixture.nwhAfter)if(!string.Equals(Hash(CurrentRiderPhysicalDependency(dependency.path)),dependency.nativeSha256,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Original NWH dependency changed after paired native acceptance: "+dependency.path);
            ValidateProductionRuntimeInventory(runtime,runtimeManifest);
            return new QualifiedProductionMount{fixture=fixture,runtime=runtime,producerManifest=producer,species=fixture.species};
        }
        static void ValidateProductionRuntimeInventory(ColdRuntimeReceipt runtime,FlightSourceHash[] manifest)
        {
            bool Same(string a,string b)=>string.Equals(a,b,StringComparison.OrdinalIgnoreCase);
            string[] snow={"Assets/TruckSports/Scripts/SportsSnowBodyContact.cs","Assets/TruckSports/Scripts/SportsSnowBodyContact.cs.meta"},appearance={"Assets/TruckSports/Scripts/SportsTruckAppearanceMountTag.cs","Assets/TruckSports/Scripts/SportsTruckAppearanceMountTag.cs.meta"},missing={"Assets/TruckFareFight/Prefabs/Fare Fight Taxi Tractor.prefab","Assets/TruckFareFight/Prefabs/Fare Fight Taxi Tractor.prefab.meta"};
            var reviewProof=runtime.consumerEvolution;if(reviewProof==null||!reviewProof.qualified||string.IsNullOrEmpty(reviewProof.reviewPath)||!Same(Hash(reviewProof.reviewPath),reviewProof.reviewSha256))throw new InvalidOperationException("Paired native inventory review changed.");var review=JsonUtility.FromJson<ColdConsumerEvolutionReview>(File.ReadAllText(reviewProof.reviewPath));
            var inventories=new[]{runtime.liveInventoryBefore,runtime.liveInventoryAfter};for(int i=0;i<inventories.Length;i++)
            {
                var inventory=inventories[i];var expected=i==0?snow:snow.Concat(appearance).ToArray();var transition=inventory?.observedTransition;
                if(inventory==null||!inventory.declaredBytesExact||!inventory.addedAssetsQualified||inventory.declaredCount!=1002||inventory.liveCount!=(i==0?1002:1004)||inventory.phase!=(i==0?"BEFORE_PRISTINE_TEST_PREFAB_ACTIVATION":"AFTER_SOLO_TEST_AND_ORIGINAL_SCENE_CHOICE_RESTORATION")||inventory.commonChanges.Length!=0||!new HashSet<string>(inventory.missingFromLive,StringComparer.Ordinal).SetEquals(missing)||inventory.added.Length!=expected.Length||!new HashSet<string>(inventory.added.Select(a=>a.path),StringComparer.Ordinal).SetEquals(expected)||transition==null||!transition.qualified||!transition.producerCallerExact||!transition.perOwnerSnowReferences||!transition.allFourPrefabLinksNonpersistent)throw new InvalidOperationException("Paired native inventory must retain the exact observed Snow/Appearance phase sets and original dependency closure.");
                if(!review.qualifyObservedSnowPrefabTransition||!Same(Hash(review.knownTransitionProvenance),review.knownTransitionSha256)||!Same(transition.provenanceSha256,review.knownTransitionSha256)||Path.GetFullPath(transition.provenancePath)!=Path.GetFullPath(review.knownTransitionProvenance))throw new InvalidOperationException("Retained Snow source provenance changed.");
                var sources=JsonUtility.FromJson<KnownSnowSourceProvenance>(File.ReadAllText(review.knownTransitionProvenance)).rows.AsEnumerable();
                if(i==1){if(!transition.appearanceSourcesQualified||!transition.appearanceRuntimeReferences||!review.qualifyObservedAppearanceTransition||!Same(Hash(review.appearanceTransitionProvenance),review.appearanceTransitionSha256)||!Same(transition.appearanceProvenanceSha256,review.appearanceTransitionSha256)||Path.GetFullPath(transition.appearanceProvenancePath)!=Path.GetFullPath(review.appearanceTransitionProvenance))throw new InvalidOperationException("Retained Appearance source provenance changed.");sources=sources.Concat(JsonUtility.FromJson<KnownSnowSourceProvenance>(File.ReadAllText(review.appearanceTransitionProvenance)).rows);}
                foreach(var added in inventory.added){var provenance=sources.Single(p=>p.path==added.path);var actual=manifest.Single(p=>p.path==added.path);if(!added.qualified||!Same(added.currentSha256,provenance.nativeSha256)||!Same(added.currentSha256,actual.sha256)||!Same(added.currentSha256,Hash(CurrentRiderPhysicalDependency(added.path))))throw new InvalidOperationException("Qualified runtime-added dependency changed before production: "+added.path);}
            }
        }
        static void RequireProductionSourceScene(string sourceScene,FlightSourceHash[] producerManifest,string courseEvidence)
        {
            if(sourceScene==DragonRidersAuthoring.ScenePath){RequireRecordedStartupSourceHash(producerManifest,sourceScene,"Explicit original Riders source");return;}
            if(string.IsNullOrEmpty(courseEvidence))throw new InvalidOperationException("A noncanonical source requires its actual passed physical-course evidence.");
            RequireRiderPassedManifest(courseEvidence,"DragonRiderPhysicalCourseTests.SavedMeasuredLargePhysicalRingsRetainSourceArtAndOrderedScoringWithNativeRimControls");var paths=Directory.GetFiles(Path.Combine(courseEvidence,"rendered-evidence"),"saved-physical-large-ring-course-candidate.json",SearchOption.AllDirectories);if(paths.Length!=1)throw new InvalidOperationException("Unique actual measured-course receipt required.");var proof=JsonUtility.FromJson<PhysicalCourseReceipt>(File.ReadAllText(paths.Single()));
            if(proof.candidateScene!=sourceScene||proof.candidateSceneSha256!=Hash(sourceScene)||!proof.sourceBytesExact||!proof.savedReloaded||!proof.orderedRouteExact||!proof.physicalControlsPassed||!proof.rulesControlsPassed)throw new InvalidOperationException("Source course does not match its actually qualified saved physical/scoring evidence.");
        }
        static void PromoteQualifiedProductionMount(QualifiedProductionMount mount,string folder)
        {
            mount.controllerPath=folder+"/"+mount.species+" safe mounted.overrideController";mount.actorPath=folder+"/"+mount.species+" qualified rider.prefab";
            if(!AssetDatabase.CopyAsset(mount.fixture.controller,mount.controllerPath))throw new InvalidOperationException("Qualified complete controller/subassets could not be copied.");
            var sourceObjects=AssetDatabase.LoadAllAssetsAtPath(mount.fixture.controller);var copies=AssetDatabase.LoadAllAssetsAtPath(mount.controllerPath);
            var byId=copies.ToDictionary(o=>{AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o,out string _,out long id);return id;});
            var remap=new Dictionary<UnityEngine.Object,UnityEngine.Object>();foreach(var source in sourceObjects){AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string _,out long id);if(!byId.TryGetValue(id,out var copy)||copy.GetType()!=source.GetType())throw new InvalidOperationException("Controller subasset identities changed during copy.");remap.Add(source,copy);mount.controllerTokens[source]="qualified-controller:"+id;mount.controllerTokens[copy]="qualified-controller:"+id;}
            foreach(var copy in copies)RemapProductionReferences(copy,remap);
            var sourceController=AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(mount.fixture.controller);var controller=AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(mount.controllerPath);var a=new List<KeyValuePair<AnimationClip,AnimationClip>>();var b=new List<KeyValuePair<AnimationClip,AnimationClip>>();sourceController.GetOverrides(a);controller.GetOverrides(b);
            if(sourceController.runtimeAnimatorController!=controller.runtimeAnimatorController||a.Count!=b.Count)throw new InvalidOperationException("Promoted controller changed its native graph or override membership.");
            for(int i=0;i<a.Count;i++){var expected=a[i].Value!=null&&remap.TryGetValue(a[i].Value,out var mapped)?(AnimationClip)mapped:a[i].Value;if(a[i].Key!=b[i].Key||expected!=b[i].Value||a[i].Value!=null&&CarrierClipMismatch(a[i].Value,b[i].Value)!=null)throw new InvalidOperationException("Promoted effective animation differs from actual qualified source.");}
            foreach(var asset in copies)AssetDatabase.SaveAssetIfDirty(asset);
            var root=PrefabUtility.LoadPrefabContents(mount.fixture.prefab);
            try
            {
                var actor=root.GetComponent<DragonCharacter>();mount.signature=ProductionActorSignature(actor,mount.controllerTokens);foreach(var component in root.GetComponentsInChildren<Component>(true).Where(c=>c!=null))RemapProductionReferences(component,remap);
                RequireProductionActorSignature(mount.signature,actor,mount.controllerTokens,"controller-remap-"+mount.species);
                if(PrefabUtility.IsPartOfPrefabInstance(root))PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                var saved=PrefabUtility.SaveAsPrefabAsset(root,mount.actorPath,out bool success);if(!success||saved==null||saved.activeSelf)throw new InvalidOperationException("Pristine inactive production-only actor prefab did not save.");
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            root=PrefabUtility.LoadPrefabContents(mount.actorPath);try{if(ProductionActorSignature(root.GetComponent<DragonCharacter>(),mount.controllerTokens)!=mount.signature||AssetDatabase.GetDependencies(mount.actorPath,true).Any(p=>p.Contains("/Tests/Generated/")))throw new InvalidOperationException("Saved production prefab lost qualified configuration or retains test assets.");}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static void RequireProductionActorSignature(string expected,DragonCharacter actor,Dictionary<UnityEngine.Object,string> aliases,string phase)
        {
            string actual=ProductionActorSignature(actor,aliases);if(actual==expected)return;int at=0;while(at<Math.Min(expected.Length,actual.Length)&&expected[at]==actual[at])at++;
            var args=Environment.GetCommandLineArgs();int output=Array.IndexOf(args,"-tsDragonOutput");if(output>=0&&output+1<args.Length)
            {
                string folder=Path.Combine(args[output+1],"RiderSaddleAuthoring","ProductionCandidate","ExactSignatureFailure");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,phase+"-before.txt"),expected);File.WriteAllText(Path.Combine(folder,phase+"-after.txt"),actual);
            }
            string Around(string text)=>text.Substring(Math.Max(0,at-160),Math.Min(text.Length-Math.Max(0,at-160),800));
            throw new InvalidOperationException("Controller promotion changed qualified actor signature at "+at+" expected="+Around(expected)+" actual="+Around(actual)+". Exact before/after retained; no failed actor adopted.");
        }
        static void RemapProductionReferences(UnityEngine.Object target,Dictionary<UnityEngine.Object,UnityEngine.Object> remap)
        {
            var serialized=new SerializedObject(target);var it=serialized.GetIterator();bool changed=false;while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference&&it.objectReferenceValue!=null&&remap.TryGetValue(it.objectReferenceValue,out var replacement)){it.objectReferenceValue=replacement;changed=true;}if(changed)serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        static string ProductionPath(Transform t,Transform root=null)=>t==root?"@":t.parent==null?t.GetSiblingIndex()+":"+t.name:ProductionPath(t.parent,root)+"/"+t.GetSiblingIndex()+":"+t.name;
        static string ProductionObjectToken(UnityEngine.Object value,Transform root,Dictionary<UnityEngine.Object,string> aliases=null)
        {
            if(value==null)return "null";if(aliases!=null&&aliases.TryGetValue(value,out string alias))return alias;
            if(AssetDatabase.Contains(value)&&AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value,out string guid,out long id))return guid+":"+id;
            if(value is GameObject go)return "GO:"+ProductionPath(go.transform,root);
            if(value is Component c)return c.GetType().FullName+":"+ProductionPath(c.transform,root)+":"+Array.IndexOf(c.GetComponents(c.GetType()),c);
            throw new InvalidOperationException("Unqualified transient object in saved candidate: "+value.GetType().FullName+" "+value.name);
        }
        static string ProductionComponentJson(Component component,Transform root,Dictionary<UnityEngine.Object,string> aliases=null,Dictionary<string,string> persistentAliases=null)
        {
            // Prefab provenance is intentionally replaced; actual runtime fields
            // and every object reference remain part of the signature.
            string json=Regex.Replace(EditorJsonUtility.ToJson(component),"\\\"m_(CorrespondingSourceObject|PrefabInstance|PrefabAsset)\\\"\\s*:\\s*\\{[^{}]*\\}","\"prefabProvenance\":null");
            if(aliases!=null)
            {
                var persistent=persistentAliases??ProductionPersistentAliases(aliases);
                // Unity emits persistent asset references as GUID/localID, not
                // instanceID. Normalize ONLY the already reviewed controller
                // and clip copy pairs; every other asset identity stays exact.
                json=Regex.Replace(json,@"\{\s*""fileID""\s*:\s*(-?[0-9]+)\s*,\s*""guid""\s*:\s*""([a-fA-F0-9]{32})""\s*,\s*""type""\s*:\s*([0-9]+)\s*\}",m=>persistent.TryGetValue(m.Groups[2].Value+":"+m.Groups[1].Value,out string token)?"{\"object\":\""+token+"\",\"type\":"+m.Groups[3].Value+"}":m.Value);
            }
            return Regex.Replace(json,"\\\"instanceID\\\"\\s*:\\s*(-?[0-9]+)",m=>"\"object\":\""+ProductionObjectToken(EditorUtility.InstanceIDToObject(int.Parse(m.Groups[1].Value)),root,aliases).Replace("\\","\\\\").Replace("\"","\\\"")+"\"");
        }
        static Dictionary<string,string> ProductionPersistentAliases(Dictionary<UnityEngine.Object,string> aliases)
        {
            var persistent=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);if(aliases!=null)foreach(var pair in aliases)if(AssetDatabase.Contains(pair.Key)&&AssetDatabase.TryGetGUIDAndLocalFileIdentifier(pair.Key,out string guid,out long id))persistent.Add(guid+":"+id,pair.Value);return persistent;
        }
        static string ProductionActorSignature(DragonCharacter actor,Dictionary<UnityEngine.Object,string> aliases)
        {
            var frame=actor.GetComponent<DragonRiderSaddle>();var rigging=frame.rigging;var truck=rigging.truck;var persistentAliases=ProductionPersistentAliases(aliases);
            try
            {
                rigging.truck=null;return string.Join("\n",actor.GetComponentsInChildren<Transform>(true).Select(t=>ProductionPath(t,actor.transform)+"|"+(t==actor.transform?"ROOT_PLACEMENT":PristineFloatBits(t.localPosition.x,t.localPosition.y,t.localPosition.z,t.localRotation.x,t.localRotation.y,t.localRotation.z,t.localRotation.w))+PristineFloatBits(t.localScale.x,t.localScale.y,t.localScale.z)+"|"+t.gameObject.activeSelf+"|"+t.gameObject.layer+"|"+t.gameObject.tag))+"\n"+string.Join("\n",actor.GetComponentsInChildren<Component>(true).Where(c=>c!=null&&c is not Transform).Select(c=>ProductionComponentJson(c,actor.transform,aliases,persistentAliases)));
            }
            finally{rigging.truck=truck;}
        }
        static bool UnderProductionRoots(Component c,HashSet<GameObject> roots)=>roots.Any(root=>c.gameObject==root||c.transform.IsChildOf(root.transform));
        static string ProductionSceneConfiguration(Scene scene,HashSet<GameObject> mounts)=>string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).Where(c=>c!=null&&!UnderProductionRoots(c,mounts)).Select(c=>(c is Transform?ProductionPath(c.transform)+"|"+c.gameObject.name+"|"+c.gameObject.activeSelf+"|"+c.gameObject.layer+"|"+c.gameObject.tag+"|"+GameObjectUtility.GetStaticEditorFlags(c.gameObject)+"|":"")+ProductionComponentJson(c,null)));
        static FixedNwhField[] CaptureProductionNwhFields(DragonRidersMatch match)
        {
            var ids=new Dictionary<string,string>();for(int slot=0;slot<match.trucks.Length;slot++){var truck=match.trucks[slot];ids[truck.GetEntityId().ToString()]="crew"+slot;var boxes=truck.GetComponentsInChildren<BoxCollider>(true);for(int box=0;box<boxes.Length;box++)ids[boxes[box].GetEntityId().ToString()]="box"+box;}
            return CaptureFixedNwhFields(match).Select(f=>new FixedNwhField{path=string.Join("/",f.path.Split('/').Select(p=>ids.TryGetValue(p,out var stable)?stable:p)),values=f.values,floatBits=f.floatBits}).ToArray();
        }
        static bool ExactProductionNwhFields(FixedNwhField[] before,FixedNwhField[] after)=>before.Length==after.Length&&before.Zip(after,(a,b)=>a.path==b.path&&a.floatBits==b.floatBits&&a.values.SequenceEqual(b.values)).All(v=>v);
        static void RequireNoExternalMountReferences(Scene scene,DragonCharacter old,DragonRiderRacer racer,int index)
        {
            foreach(var component in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).Where(c=>c!=null&&!c.transform.IsChildOf(old.transform)))
            {
                var serialized=new SerializedObject(component);var it=serialized.GetIterator();while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference&&it.objectReferenceValue!=null)
                {
                    var value=it.objectReferenceValue;Transform target=value is GameObject go?go.transform:value is Component c?c.transform:null;if(target==null||!target.IsChildOf(old.transform))continue;
                    if(component==racer&&it.propertyPath=="choices.Array.data["+index+"]")continue;
                    // The mount parent's child array is preserved in-place by
                    // sibling index; its reference is owned by the hierarchy.
                    if(component is Transform&&it.propertyPath.StartsWith("m_Children.Array.data[",StringComparison.Ordinal))continue;
                    throw new InvalidOperationException("Preserve unknown external mount reference: "+component.GetType().Name+"."+it.propertyPath);
                }
            }
        }
    }
}
#endif
