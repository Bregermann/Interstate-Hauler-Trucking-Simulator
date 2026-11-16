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
        [Serializable] sealed class CourseSourceCertificateProof
        {
            public string scope,status,receipt,receiptSha256,projectRoot,target,oldSceneSha256,currentSceneSha256,metaSha256;
            public bool productionEligible,currentProfilesExact,currentExternalConfigurationExact,currentWorldExact,currentNwhExact,unitAncestors,noTestDependencies,sourceClosureExact;
            public int choices,originalTrucks,recordedInfernoPoses=699,recordedFirePoses=537,freshPoseSamples,protectedFiles;
            public float maximumNwhDimensionDrift;
        }
        sealed class CourseMountSourceAuthority
        {
            public readonly CourseSourceCertificateProof proof;
            public CourseMountSourceAuthority(CourseSourceCertificateProof proof){this.proof=proof;}
        }
        static bool CourseCertificateSame(string a,string b)=>string.Equals(a,b,StringComparison.OrdinalIgnoreCase);
        static string CourseCertificatePhysical(string path)=>Path.GetFullPath(CurrentRiderPhysicalDependency(path));
        static void RequireProductionCourseMountAuthority(CourseMountSourceAuthority authority)
        {
            if(authority==null||!authority.proof.productionEligible||authority.proof.target!=DragonRidersAuthoring.ScenePath||!CourseCertificateSame(authority.proof.projectRoot,Directory.GetParent(Application.dataPath).FullName))throw new InvalidOperationException("A validated production canonical source authority is required; TEST_ONLY authority cannot enter production guards.");
        }
        static string CourseCertifiedSceneRowExpected(CourseMountSourceAuthority authority,string path,string recordedSha256,bool testOnly)
        {
            if(authority==null)return recordedSha256;
            // No metadata, prefab, controller or runtime path receives a
            // substitute hash. Their existing exact comparisons remain sole
            // authority. No TEST_ONLY token may authorize this production row.
            if(path.Replace('\\','/')!=authority.proof.target)return recordedSha256;
            if(!testOnly)RequireProductionCourseMountAuthority(authority);
            if(!CourseCertificateSame(recordedSha256,authority.proof.oldSceneSha256)||!CourseCertificateSame(Hash(CourseCertificatePhysical(path)),authority.proof.currentSceneSha256)||!CourseCertificateSame(Hash(CourseCertificatePhysical(path+".meta")),authority.proof.metaSha256))throw new InvalidOperationException("Certified canonical source row old/new/meta bytes no longer match the exact authorized chain.");
            return authority.proof.currentSceneSha256;
        }
        static void RequireCourseMountHeader(CanonicalMountApply apply,bool testOnly)
        {
            if(testOnly)throw new InvalidOperationException("Only an actual canonical mount Apply can supply upstream production authority.");
            string expected=testOnly?"DISPOSABLE_CANONICAL_PLAN_APPLY_VALIDATED_NOT_CANONICAL":"CANONICAL_MOUNTS_PROMOTED_REQUIRES_COURSE_WORLD_AND_GAMEPLAY_VALIDATION";
            if(apply==null||apply.plan==null||apply.canonicalTarget==testOnly||apply.status!=expected||!string.IsNullOrEmpty(apply.failure)||apply.nativeCalls!=8||!apply.allOriginalChoicesPreflighted||!apply.otherSevenEveryStepExact||!apply.externalIdentitiesExact||!apply.configurationExact||!apply.worldExact||!apply.nwhExact||!apply.profilesExact||!apply.unitAncestors||!apply.noTestDependencies||!apply.savedReloaded||!apply.sourceClosureExact||apply.protectedRacers!=4||apply.protectedTrucks!=4||apply.protectedRings!=12||apply.protectedExternalObjects<1000||apply.maximumNwhDimensionDrift>.00001f||apply.choices?.Length!=8)throw new InvalidOperationException("A successful exact canonical Apply is required; disposable receipts cannot authorize production canonical source rows.");
            if(apply.plan.scene!=DragonRidersAuthoring.ScenePath||!CourseCertificateSame(apply.plan.projectRoot,Directory.GetParent(Application.dataPath).FullName)||!CourseCertificateSame(apply.projectRoot,apply.plan.projectRoot)||!CourseCertificateSame(Path.GetFullPath(apply.targetPhysicalPath),CourseCertificatePhysical(apply.target))||(!testOnly&&apply.target!=DragonRidersAuthoring.ScenePath)||(testOnly&&(!apply.target.StartsWith("Assets/TruckDragonRiders/Tests/Generated/CanonicalPromotion-",StringComparison.Ordinal)||apply.target.Contains(".."))))throw new InvalidOperationException("Canonical certificate project/physical target ownership changed.");
            if(!CourseCertificateSame(apply.sourceSha256,apply.plan.sceneSha256)||CourseCertificateSame(apply.sourceSha256,apply.resultSha256)||!CourseCertificateSame(Hash(apply.target),apply.resultSha256)||!CourseCertificateSame(Hash(apply.target+".meta"),apply.metaSha256)||!CourseCertificateSame(Hash(DragonRidersAuthoring.ScenePath+".meta"),apply.plan.metaSha256)||AssetDatabase.AssetPathToGUID(DragonRidersAuthoring.ScenePath)!=apply.plan.sceneGuid||(!testOnly&&!CourseCertificateSame(apply.metaSha256,apply.plan.metaSha256)))throw new InvalidOperationException("Canonical certificate original/result scene or unchanged metadata/GUID chain changed.");
            if(!CourseCertificateSame(Hash(Path.Combine(apply.backupFolder,"before.unity")),apply.sourceSha256)||!CourseCertificateSame(Hash(Path.Combine(apply.backupFolder,"before.unity.meta")),apply.metaSha256))throw new InvalidOperationException("Actual source-closed pre-Apply backup changed.");
            if(!CourseCertificateSame(HashTextForNativeReplacement(JsonUtility.ToJson(apply.plan,true)),apply.planSha256)||apply.plan.protectedSources==null||apply.plan.protectedSources.Length<1000)throw new InvalidOperationException("Canonical Apply plan payload or complete protected source inventory changed; recordedPlan="+apply.planSha256+" roundTripPlan="+HashTextForNativeReplacement(JsonUtility.ToJson(apply.plan,true)));
        }
        static NativeEightReceipt RequireCourseMountEightAuthority(CanonicalMountApply apply)
        {
            const string test="DragonRiderNativeEightReplacementTests.ActualEightChoiceNativeReplacementPreservesOwnersAndRejectsLastChoiceEditBeforeStepZero";RequireRiderPassedManifest(apply.eightEvidence,test+"(False)");RequireRiderPassedManifest(apply.eightEvidence,test+"(True)");
            if(Path.GetFullPath(apply.eightEvidence)!=Path.GetFullPath(apply.plan.eightEvidence)||Path.GetFullPath(apply.productionEvidence)!=Path.GetFullPath(apply.plan.productionEvidence)||!CourseCertificateSame(Hash(Path.Combine(apply.eightEvidence,"tests.xml")),apply.plan.eightTestsSha256)||!CourseCertificateSame(Hash(CanonicalEightReceiptPath(apply.eightEvidence)),apply.plan.eightReceiptSha256))throw new InvalidOperationException("Canonical Apply no longer binds its actual passed eight-choice evidence.");
            var eight=JsonUtility.FromJson<NativeEightReceipt>(File.ReadAllText(CanonicalEightReceiptPath(apply.eightEvidence)));
            if(eight.status!="DISPOSABLE_ALL_EIGHT_NATIVE_REPLACEMENTS_VALIDATED_NOT_CANONICAL"||!string.IsNullOrEmpty(eight.failure)||!eight.originalSourcesExact||!eight.externalIdentitiesExact||!eight.externalConfigurationExact||!eight.worldExact||!eight.nwhExact||!eight.qualifiedProfilesExact||!eight.savedReloaded||!eight.unitAncestors||!eight.noTestAssetDependencies||!eight.allOriginalChoicesPreflighted||!eight.otherSevenEveryStepExact||eight.nativeCalls!=8||eight.replacedChoices!=8||eight.choices?.Length!=8||eight.protectedRacers!=4||eight.protectedTrucks!=4||eight.protectedPhysicalRings!=12||eight.protectedExternalObjects!=apply.protectedExternalObjects||Path.GetFullPath(eight.productionEvidence)!=Path.GetFullPath(apply.productionEvidence))throw new InvalidOperationException("Actual eight-profile native baseline authority changed.");
            var old=eight.originalSources.Where(row=>row.path.Replace('\\','/')==DragonRidersAuthoring.ScenePath).ToArray();if(old.Length!=1||!CourseCertificateSame(old[0].sha256,apply.sourceSha256)||eight.externalBefore!=apply.externalBefore||eight.worldBefore!=apply.worldBefore||apply.externalBefore!=apply.externalAfter||apply.worldBefore!=apply.worldAfter||apply.plan.externalConfiguration!=apply.externalBefore||apply.plan.worldState!=apply.worldBefore)throw new InvalidOperationException("Exact original scene/external configuration/world authority no longer chains to canonical Apply.");return eight;
        }
        static int CourseRequireProtectedInventoryShape(FlightSourceHash[] rows)
        {
            // The actual native receipt records5894 logical rows. Six URP
            // files are intentionally recorded both by logical Packages path
            // and by captured PackageCache path. Both rows remain mandatory;
            // one physical file is not two different source identities.
            if(rows==null||rows.Length!=5894||rows.Any(row=>row==null||string.IsNullOrEmpty(row.path)||string.IsNullOrEmpty(row.sha256))||rows.Select(row=>row.path.Replace('\\','/')).Distinct(StringComparer.Ordinal).Count()!=5894)throw new InvalidOperationException("Actual mount epoch requires5894 distinct recorded source rows, not5894 distinct physical files.");
            var physical=rows.GroupBy(row=>CourseCertificatePhysical(row.path),StringComparer.OrdinalIgnoreCase).ToArray();
            foreach(var group in physical)if(group.Select(row=>row.sha256).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=1)throw new InvalidOperationException("Recorded logical/physical source aliases disagree on their exact hash: "+group.Key);
            return physical.Length;
        }
        static void RequireCourseMountProtectedSources(CanonicalMountApply apply,CourseSourceCertificateProof output,bool testOnly,string testEvidence)
        {
            if(testOnly)throw new InvalidOperationException("Disposable or Editor-code epoch exceptions cannot create course mount authority.");
            CourseRequireProtectedInventoryShape(apply.plan.protectedSources);
            foreach(var row in apply.plan.protectedSources)if(!CourseCertificateSame(Hash(CourseCertificatePhysical(row.path)),row.sha256))throw new InvalidOperationException("Actual V511 protected source changed; no code/hash exceptions: "+row.path);
            output.protectedFiles=apply.plan.protectedSources.Length;
        }
        static CourseMountSourceAuthority ReadCourseMountSourceAuthority(string receiptPath,CanonicalMountApply apply,bool testOnly,string testEvidence)
        {
            RequirePromotionEditorContext();RequireCourseMountHeader(apply,testOnly);var eight=RequireCourseMountEightAuthority(apply);var production=ReadQualifiedCourseProduction(apply.productionEvidence);
            var proof=new CourseSourceCertificateProof{scope=testOnly?"TEST_ONLY_EXISTING_SAVED_DISPOSABLE_RESULT_NO_PRODUCTION_AUTHORITY":"ONE_EXACT_CANONICAL_SCENE_ROW_ORIGINAL_SHA_TO_SUCCESSFUL_APPLY_SHA_NOT_COURSE_WORLD_OR_FRESH_POSE_ACCEPTANCE",receipt=receiptPath,receiptSha256=Hash(receiptPath),projectRoot=apply.projectRoot,target=apply.target,oldSceneSha256=apply.sourceSha256,currentSceneSha256=apply.resultSha256,metaSha256=apply.metaSha256,productionEligible=!testOnly};var authority=new CourseMountSourceAuthority(proof);
            for(int i=0;i<2;i++)
            {
                string nativeCase=production.species[i]=="dragon-fire"?"DragonRiderMotorRootRepresentationTests.UnitAncestorsAndInactiveFireMotorRootRetainFull537GeometryAndExportPristineTestFixture":"DragonRiderMotorRootRepresentationTests.UnitAncestorsAndInactiveConfiguredMotorRootRetainFull699GeometryAndMeasureActualTruckCenteredCourseEnvelope";var manifest=RequireRiderPassedManifest(production.producerEvidence[i],nativeCase);var sceneRows=manifest.Where(row=>row.path.Replace('\\','/')==DragonRidersAuthoring.ScenePath).ToArray();var metaRows=manifest.Where(row=>row.path.Replace('\\','/')==DragonRidersAuthoring.ScenePath+".meta").ToArray();if(sceneRows.Length!=1||metaRows.Length!=1||!CourseCertificateSame(sceneRows[0].sha256,proof.oldSceneSha256)||!CourseCertificateSame(metaRows[0].sha256,apply.plan.metaSha256))throw new InvalidOperationException("Canonical source certificate must chain to BOTH original producer scene/meta rows.");
            }
            RequireCourseMountProtectedSources(apply,proof,testOnly,testEvidence);
            var mounts=production.species.Select((s,i)=>RequireCourseQualifiedProductionMount(production.producerEvidence[i],production.runtimeEvidence[i],authority)).ToArray();var aliases=mounts.Select((m,i)=>QualifiedCourseControllerAliases(m.fixture.controller,production.controllers[i])).ToArray();var signatures=new string[2];
            for(int i=0;i<2;i++)
            {
                if(mounts[i].species!=production.species[i])throw new InvalidOperationException("Canonical source species order changed.");if(!testOnly)RequireCourseOriginalSceneRow(DragonRidersAuthoring.ScenePath,mounts[i].producerManifest,apply.plan.courseEvidence,authority);var source=PrefabUtility.LoadPrefabContents(mounts[i].fixture.prefab);try{signatures[i]=ProductionActorSignature(source.GetComponent<DragonCharacter>(),aliases[i]);}finally{PrefabUtility.UnloadPrefabContents(source);}
                if(eight.choices.Where(row=>row.species==mounts[i].species).Count()!=4||eight.choices.Where(row=>row.species==mounts[i].species).Any(row=>row.qualifiedSignature!=HashTextForNativeReplacement(signatures[i])))throw new InvalidOperationException("Current canonical profiles lost original actual native geometry identity.");
            }
            var context=new CanonicalMountContext{production=production,baseline=eight,mounts=mounts,aliases=aliases,signatures=signatures};if(!CanonicalMountProtectedPaths(context,DragonRidersAuthoring.ScenePath,apply.eightEvidence).SequenceEqual(apply.plan.protectedSources.Select(row=>row.path)))throw new InvalidOperationException("Canonical certificate protected source inventory membership changed.");
            var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;var watched=apply.plan.protectedSources.Select(row=>CourseCertificatePhysical(row.path)).Concat(new[]{receiptPath,apply.target,apply.target+".meta",Path.Combine(apply.backupFolder,"before.unity"),Path.Combine(apply.backupFolder,"before.unity.meta")}).Distinct().ToDictionary(path=>path,path=>Hash(path));
            try
            {
                scene=EditorSceneManager.OpenScene(apply.target,OpenSceneMode.Single);var match=CourseMatch(scene);var actors=NativeEightActors(match);proof.currentProfilesExact=true;proof.unitAncestors=true;
                for(int i=0;i<8;i++)
                {
                    var row=apply.choices[i];var actor=actors[i];int species=Array.IndexOf(production.species,row.species);if(row.slot!=i/2||row.index!=i%2||match.racers[i/2].choiceIds[i%2]!=row.species||species<0)throw new InvalidOperationException("Canonical current choice roster changed.");proof.currentProfilesExact&=NativeReplacementIdentity(actor.gameObject)==row.targetAfter&&row.targetAfter==row.targetReloaded&&NativeReplacementIdentity(actor)==row.actorAfter&&row.actorAfter==row.actorReloaded&&NativeEightRootPlacement(actor.transform)==row.rootPlacement&&!actor.gameObject.activeSelf&&actor.GetComponent<DragonRiderSaddle>().rigging.truck==match.trucks[row.slot]&&PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(actor.gameObject)==production.actorPrefabs[species]&&ProductionActorSignature(actor,aliases[species])==signatures[species];for(var t=actor.transform;t!=null;t=t.parent)proof.unitAncestors&=t.localScale.Equals(Vector3.one);proof.choices++;
                }
                proof.currentExternalConfigurationExact=HashTextForNativeReplacement(ProductionSceneConfiguration(scene,new HashSet<GameObject>(actors.Select(a=>a.gameObject))))==apply.externalAfter;proof.currentWorldExact=HashTextForNativeReplacement(NativeReplacementWorldState(match.world))==apply.worldAfter;proof.noTestDependencies=!AssetDatabase.GetDependencies(apply.target,true).Any(path=>path!=apply.target&&path.Contains("/Tests/Generated/"));if(match.gates.Length!=12||CoursePhysicalGates(scene).Length!=12||NativeEightProtectedIds(scene,actors).Count!=apply.protectedExternalObjects)throw new InvalidOperationException("Canonical protected course/external identities changed.");var currentNwh=CaptureProductionNwhFields(match);proof.originalTrucks=match.trucks.Length;
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);scene=EditorSceneManager.OpenScene(production.candidateScene,OpenSceneMode.Single);var reference=CaptureProductionNwhFields(CourseMatch(scene));proof.currentNwhExact=ExactProductionNwhFields(reference,currentNwh)&&FiniteWorldBoxDimensionsAgree(reference,currentNwh,out proof.maximumNwhDimensionDrift);
                if(!proof.currentProfilesExact||!proof.unitAncestors||!proof.currentExternalConfigurationExact||!proof.currentWorldExact||!proof.noTestDependencies||!proof.currentNwhExact||proof.choices!=8||proof.originalTrucks!=4)throw new InvalidOperationException("Canonical current native profiles/external/world/original NWH semantic requalification failed.");
            }
            finally{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
            proof.sourceClosureExact=watched.All(row=>CourseCertificateSame(Hash(row.Key),row.Value));if(!proof.sourceClosureExact||!CourseCertificateSame(Hash(apply.target),proof.currentSceneSha256))throw new InvalidOperationException("Canonical certificate read-only source/target bytes changed after editor cleanup.");proof.status=testOnly?"TEST_ONLY_SAVED_COPY_CHAIN_VALIDATED_NOT_CANONICAL":"CURRENT_CANONICAL_SINGLE_SOURCE_ROW_CERTIFIED_RECORDED_NATIVE_POSES_NOT_NEW_REPLAY";return authority;
        }
        public static string ValidateUpstreamMountForPhysicalCourse(string receiptPath)
        {
            var apply=JsonUtility.FromJson<CanonicalMountApply>(File.ReadAllText(receiptPath));return JsonUtility.ToJson(ReadCourseMountSourceAuthority(receiptPath,apply,false,null).proof,true);
        }
        static QualifiedProductionMount RequireCourseQualifiedProductionMount(string producerFolder,string runtimeFolder,CourseMountSourceAuthority authority)
        {
            RequireProductionCourseMountAuthority(authority);
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
                if(path==DragonRidersAuthoring.ScenePath)expected=CourseCertifiedSceneRowExpected(authority,path,expected,false);
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
        static void RequireCourseOriginalSceneRow(string scene,FlightSourceHash[] manifest,string unusedCourseEvidence,CourseMountSourceAuthority authority)
        {
            RequireProductionCourseMountAuthority(authority);if(scene!=DragonRidersAuthoring.ScenePath)throw new InvalidOperationException("No alternate source scene is authorized by the exact course chain.");
            var rows=manifest.Where(row=>row.path.Replace('\\','/')==scene).ToArray();if(rows.Length!=1||!CourseCertificateSame(Hash(CourseCertificatePhysical(scene)),CourseCertifiedSceneRowExpected(authority,scene,rows[0].sha256,false)))throw new InvalidOperationException("Actual original producer scene row does not chain to this current canonical scene.");
        }
    }
}
#endif
