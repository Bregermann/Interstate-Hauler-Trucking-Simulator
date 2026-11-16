#if UNITY_EDITOR && VISTA
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml;
using LWS.TruckDragonRiders.Editor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    // A certificate around the existing native clearance review, not another
    // course, geometry sampler, world builder or adoption implementation.
    public static class DragonKingdomPhysicalCourseWorldBridge
    {
        public const string NativeCase="DragonKingdomPhysicalCourseConsumerTests.ActualCertifiedPhysicalCanonicalCourseClearsPassedWorldWithoutAdoption";
        const string WorldCase="DragonKingdomCurrentFinalTerrainRecipeTests.ActualCurrentWorldRunsExplicitRecipeThenUnchangedOriginalRoofAndFullWorldGates";
        const string Canonical="Assets/TruckDragonRiders/Scenes/TruckDragonRiders.unity";
        const string CertificateScope="ONE_CANONICAL_SCENE_ROW_EXACT_PRODUCER_TO_NATIVE_MOUNTS_TO_NATIVE_PHYSICAL_COURSE_CHAIN_NOT_NEW_POSE_OR_WORLD_FLIGHT_ACCEPTANCE";
        const string CertificateStatus="CURRENT_CANONICAL_PHYSICAL_COURSE_SOURCE_CERTIFIED_RECORDED_NATIVE_POSES_NOT_NEW_REPLAY";
        [Serializable]public sealed class Certificate
        {
            public string scope,status,receipt,receiptSha256,target,oldSceneSha256,currentSceneSha256,metaSha256;
            public bool productionEligible,currentProfilesExact,currentExternalConfigurationExact,currentWorldExact,currentNwhExact,unitAncestors,noTestDependencies,sourceClosureExact;
            public int choices,originalTrucks,recordedInfernoPoses,recordedFirePoses,freshPoseSamples,protectedFiles;public float maximumNwhDimensionDrift;
        }
        [Serializable]public sealed class Receipt
        {
            public string scope="EXACT_CANONICAL_PHYSICAL_COURSE_AND_PASSED_WORLD_EXISTING_NATIVE_CLEARANCE_NOT_FLIGHT_OR_ADOPTION",status="IN_PROGRESS",failure,candidate;
            public string coursePromotionReceipt,coursePromotionReceiptSha256,certificate,courseReviewReceipt,courseReviewReceiptSha256,worldTests,worldTestsSha256,worldNativeReceipt,worldNativeReceiptSha256;
            public string canonicalSceneSha256,worldPrefabSha256,worldPreviewSha256,worldIntegrationSha256;
            public bool sourceBytesPreserved;public int freshPoseSamples,recordedInfernoPoses=699,recordedFirePoses=537;
        }
        public static Receipt LastReceipt{get;private set;}
        [Serializable]sealed class SourceRow{public string path,sha256;}
        [Serializable]sealed class SourceRows{public SourceRow[] rows;}
        static void RequirePassedConsumerSources(string tests)
        {
            string manifest=Path.Combine(Path.GetDirectoryName(Physical(tests)),"input-manifest.json");var rows=JsonUtility.FromJson<SourceRows>("{\"rows\":"+File.ReadAllText(Io(manifest))+"}")?.rows;if(rows==null)throw new InvalidOperationException("Actual passed native input source inventory required.");
            foreach(string source in new[]{"Assets/TruckDragonRiders/Editor/DragonRiderSaddleAuthoring.CourseReviewSource.cs","Assets/TruckDragonRiders/Editor/DragonRiderSaddleAuthoring.PhysicalCourseReviewRouting.cs","Assets/TruckDragonKingdom/Editor/DragonKingdomPhysicalCourseWorldBridge.cs","Assets/TruckDragonKingdom/Tests/World/DragonKingdomPhysicalCourseConsumerTests.cs"})foreach(string path in new[]{source,source+".meta"})
            {var actual=rows.Where(row=>row.path==path).ToArray();if(actual.Length!=1||!Hash(actual[0].sha256,Digest(path)))throw new InvalidOperationException("Current exact consumer source differs from its specific passed native case: "+path);}
        }
        static string Arg(string flag){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,flag);if(args.Count(value=>value==flag)!=1||at<0||at+1>=args.Length||string.IsNullOrWhiteSpace(args[at+1])||args[at+1].StartsWith("-",StringComparison.Ordinal))throw new ArgumentException("One explicit actual evidence value required: "+flag);return args[at+1];}
        static string Physical(string path)=>Path.GetFullPath(Path.IsPathRooted(path)?path:Path.Combine(Directory.GetParent(Application.dataPath).FullName,path));
        static string Io(string path){string full=Physical(path);return Application.platform==RuntimePlatform.WindowsEditor&&full.Length>=248&&!full.StartsWith(@"\\?\",StringComparison.Ordinal)?@"\\?\"+full:full;}
        static string Digest(string path){using(var sha=SHA256.Create())return string.Concat(sha.ComputeHash(File.ReadAllBytes(Io(path))).Select(value=>value.ToString("x2")));}
        static bool Hash(string a,string b)=>a!=null&&a.Length==64&&b!=null&&b.Length==64&&a.All(Uri.IsHexDigit)&&b.All(Uri.IsHexDigit)&&string.Equals(a,b,StringComparison.OrdinalIgnoreCase);
        static bool SamePath(string a,string b)=>!string.IsNullOrEmpty(a)&&!string.IsNullOrEmpty(b)&&string.Equals(Physical(a),Physical(b),StringComparison.OrdinalIgnoreCase);
        static void Passed(string tests,string name){var xml=new XmlDocument();xml.Load(Io(tests));var rows=xml.SelectNodes("//test-case").Cast<XmlNode>().Where(row=>row.Attributes["fullname"]?.Value==name).ToArray();if(rows.Length!=1||rows[0].Attributes["result"]?.Value!="Passed")throw new InvalidOperationException("Specific actual native case has not passed: "+name);}
        static Certificate RequireCertificate(string json,string receipt,string sceneSha)
        {
            var c=string.IsNullOrEmpty(json)?null:JsonUtility.FromJson<Certificate>(json);
            if(c==null||c.scope!=CertificateScope||c.status!=CertificateStatus||!c.productionEligible||c.target!=Canonical||!SamePath(c.receipt,receipt)||!Hash(c.currentSceneSha256,sceneSha)||!c.currentProfilesExact||!c.currentExternalConfigurationExact||!c.currentWorldExact||!c.currentNwhExact||!c.unitAncestors||!c.noTestDependencies||!c.sourceClosureExact||c.choices!=8||c.originalTrucks!=4||c.recordedInfernoPoses!=699||c.recordedFirePoses!=537||c.freshPoseSamples!=0||c.protectedFiles<5894||!float.IsFinite(c.maximumNwhDimensionDrift)||c.maximumNwhDimensionDrift>.00001f)throw new InvalidOperationException("Actual complete canonical producer->mount->physical-course authority required; metadata alone never grants adoption.");return c;
        }
        static void RequireMetadata(Receipt side,DragonKingdomMountedCourseReview.Receipt review,string liveCertificate,string tag,string courseReceipt,string sceneSha,string prefabSha,string previewSha,string integrationSha)
        {
            var c=RequireCertificate(liveCertificate,courseReceipt,sceneSha);var captured=RequireCertificate(side?.certificate,courseReceipt,sceneSha);var q=review?.qualifiedSource;
            if(side==null||side.candidate!=tag||side.freshPoseSamples!=0||side.recordedInfernoPoses!=699||side.recordedFirePoses!=537||!SamePath(side.coursePromotionReceipt,courseReceipt)||!Hash(side.coursePromotionReceiptSha256,c.receiptSha256)||!Hash(c.receiptSha256,captured.receiptSha256)||!Hash(c.oldSceneSha256,captured.oldSceneSha256)||!Hash(c.metaSha256,captured.metaSha256)||!Hash(side.canonicalSceneSha256,sceneSha)||!Hash(side.worldPrefabSha256,prefabSha)||!Hash(side.worldPreviewSha256,previewSha)||!Hash(side.worldIntegrationSha256,integrationSha))throw new InvalidOperationException("Course/world sidecar has a stale or unrelated authority chain.");
            if(review==null||review.candidate!=tag||review.status!="SAMPLED_FULL_MOUNT_ENVELOPE_CLEAR_REQUIRES_ACTUAL_FLIGHT"||!string.IsNullOrEmpty(review.error)||!review.fixedQualifiedCourse||!review.coordinatedCourse||!review.sourceBytesPreserved||review.ridersScene!=Canonical||!Hash(review.ridersSceneSha256,sceneSha)||!Hash(review.worldPrefabSha256,prefabSha)||!Hash(review.worldPreviewSha256,previewSha)||review.contactQueries!=0||review.apertureFailures!=0||review.queries<=0||review.turnQueries<=0||review.gateCount!=12||review.physicalGateColliders!=12||review.straightExitDistance!=55||review.courseLayout?.Length!=12||review.courseLayout.Any(g=>g.clearRadius!=60)||review.originalCourseHash!=DragonKingdomMountedCourseReview.GateHash(review.courseLayout)||review.rigs?.Length!=8)throw new InvalidOperationException("Existing native fixed-course/world clearance has not passed with exact12physical60m apertures and55m exit.");
            if(q==null||q.scene!=Canonical||q.sourceScene!=Canonical||!q.canonicalSourceReview||!Hash(q.sceneSha256,sceneSha)||q.status!="CURRENT_CANONICAL_COURSE_SOURCE_CERTIFIED_WITH_RECORDED_GEOMETRY_NOT_NEW_REPLAY"||!string.IsNullOrEmpty(q.failure)||!string.IsNullOrEmpty(q.cleanupFailure)||!q.currentConfigurationExact||!q.currentProfilesExact||!q.currentNwhExact||!q.unitAncestors||!q.noTestDependencies||!q.rootActivationExact||!q.currentSourceBytesExact||q.straightExitDistance!=55||!float.IsFinite(q.maximumNwhDimensionDrift)||q.maximumNwhDimensionDrift>.00001f||q.currentSourceSnapshot==null||q.currentSourceSnapshot.Length==0||q.currentSourceSnapshot.Any(row=>row==null||string.IsNullOrWhiteSpace(row.path)||row.sha256==null||row.sha256.Length!=64||!row.sha256.All(Uri.IsHexDigit))||q.currentSourceSnapshot.Select(row=>Physical(row.path)).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=q.currentSourceSnapshot.Length||q.rigs?.Count!=8)throw new InvalidOperationException("Truthful current canonical source/recorded geometry metadata required; old comparison lifecycle flags are not substituted.");
            var original=RequireCertificate(q.canonicalSourceCertificate,courseReceipt,sceneSha);if(!Hash(original.receiptSha256,c.receiptSha256)||!Hash(original.oldSceneSha256,c.oldSceneSha256)||!Hash(original.metaSha256,c.metaSha256))throw new InvalidOperationException("The actual source reader qualified a different course transaction.");
            foreach(int crew in Enumerable.Range(0,4))foreach(string species in new[]{"dragon-inferno","dragon-fire"})
            {
                var rr=review.rigs.Where(r=>r!=null&&r.crew==crew&&r.species==species).ToArray();var ss=q.rigs.Where(r=>r!=null&&r.crew==crew&&r.species==species).ToArray();if(rr.Length!=1||ss.Length!=1)throw new InvalidOperationException("Exactly one actual source rig per crew/species required.");var r=rr[0];var s=ss[0];int expected=species=="dragon-fire"?537:699;
                if(r.poseAuthority!="PAIRED_FULL_NATIVE_GEOMETRY_CURRENT_PROFILE_EQUIVALENCE_NOT_NEW_POSE_REPLAY"||r.poses!=0||r.baseFlightPoses!=0||r.configuredGraphPoses!=0||r.nativeMixerPoses!=0||r.recordedSourcePoses!=expected||s.recordedSourcePoses!=expected||s.poseLabels?.Length!=expected||s.poseLabels.Any(string.IsNullOrEmpty)||s.recordedMixerLabelledPoses<=0||s.recordedMixerLabelledPoses!=s.poseLabels.Count(label=>label.IndexOf("blend",StringComparison.OrdinalIgnoreCase)>=0||label.IndexOf("two-layer",StringComparison.OrdinalIgnoreCase)>=0)||r.recordedMixerLabelledPoses!=s.recordedMixerLabelledPoses||s.envelopeAuthority!="CURRENT_AUTHORED_BOX_CENTER_TRANSFORMED_BY_CURRENT_LOCAL_TO_WORLD_NOT_PHYSX_CACHE"||r.skinnedVertices<=0||r.skinnedVertices!=s.skinPoints||r.originalTruckColliders<1||r.originalTruckRenderers<1||r.controller!=s.controller||!Hash(r.controllerSha256,s.controllerSha256)||!r.envelope.Equals(s.headingBounds)||!r.neutralSeatOffset.Equals(s.neutralCrossingOffset)||r.maximumGateRadialExtent!=s.anyAttitudeRadius||!float.IsFinite(r.maximumGateRadialExtent)||r.maximumGateRadialExtent<=0||r.clips==null||r.clips.Length==0||r.clipSha256?.Length!=r.clips.Length||s.clips==null||s.clipSha256?.Length!=r.clips.Length||!r.clips.SequenceEqual(s.clips)||r.clipSha256.Where((hash,index)=>!Hash(hash,s.clipSha256[index])).Any())throw new InvalidOperationException("Recorded699/537 full source/controller/mixer provenance changed or fresh counters were fabricated.");
            }
        }
        public static void ReviewActualCertifiedCourseAgainstPassedWorld()
        {
            string tag=Arg("-tsDragonCourseSource"),course=Arg("-tsDragonCanonicalPhysicalCourseReceipt"),tests=Arg("-tsDragonPassedWorldTests"),output=Arg("-tsDragonOutput"),folder="Assets/TruckDragonKingdom/VisualCandidates/"+tag;
            if(tag.Length>40||tag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new ArgumentException("Exact bounded saved world tag required.");Passed(tests,WorldCase);string native=Path.Combine(Path.GetDirectoryName(Physical(tests)),"rendered-evidence","regional-world-"+tag+".json"),integration=folder+"/Integration Receipt.json";
            if(!Hash(Digest(native),Digest(integration)))throw new InvalidOperationException("Saved world integration differs from its specific actual passed native receipt.");var world=JsonUtility.FromJson<DragonKingdomRegionalWorldIntegration.Receipt>(File.ReadAllText(Io(integration)));if(world.tag!=tag||world.status!="PHYSICAL_GATES_PASSED_REQUIRES_RENDER_AND_ACTUAL_DRIVING_REVIEW"||!world.sourceBytesPreserved||!world.originalPhysicalGatesPassed||!world.candidateRoadAuthorityAndCriticalPosesExact)throw new InvalidOperationException("Actual full physical world gates are required before course review.");
            var proof=new Receipt{candidate=tag,coursePromotionReceipt=course,coursePromotionReceiptSha256=Digest(course),worldTests=Physical(tests),worldTestsSha256=Digest(tests),worldNativeReceipt=native,worldNativeReceiptSha256=Digest(native),canonicalSceneSha256=Digest(Canonical),worldPrefabSha256=Digest(folder+"/Shared Dragon Kingdom.prefab"),worldPreviewSha256=Digest(folder+"/DragonKingdomPreview.unity"),worldIntegrationSha256=Digest(integration)};LastReceipt=proof;string sidecar=Path.Combine(Physical(output),"canonical-physical-course-world-"+tag+".json");
            try
            {
                using(DragonRiderSaddleAuthoring.OpenCertifiedPhysicalCourseReviewScope())DragonKingdomMountedCourseReview.ReviewCoordinatedCourseCandidate();var review=DragonKingdomMountedCourseReview.LastReceipt;proof.courseReviewReceipt=Path.Combine(Physical(output),"mounted-course-"+tag+"-coordinated.json");proof.courseReviewReceiptSha256=Digest(proof.courseReviewReceipt);proof.certificate=DragonRiderSaddleAuthoring.ValidateCanonicalPhysicalCoursePromotionCertificate(course);RequireMetadata(proof,review,proof.certificate,tag,course,Digest(Canonical),Digest(folder+"/Shared Dragon Kingdom.prefab"),Digest(folder+"/DragonKingdomPreview.unity"),Digest(integration));
                proof.sourceBytesPreserved=review.sourceBytesPreserved&&Hash(Digest(course),proof.coursePromotionReceiptSha256)&&Hash(Digest(tests),proof.worldTestsSha256)&&Hash(Digest(native),proof.worldNativeReceiptSha256);if(!proof.sourceBytesPreserved)throw new InvalidOperationException("Read-only world/course source evidence changed.");proof.status="CANONICAL_PHYSICAL_COURSE_WORLD_CLEARANCE_CERTIFIED_RECORDED_SOURCE_NOT_FLIGHT_OR_ADOPTION";
            }
            catch(Exception error){proof.failure=error.ToString();proof.status="REJECTED_CANONICAL_PHYSICAL_COURSE_WORLD_REVIEW";throw;}
            finally{Directory.CreateDirectory(Physical(output));File.WriteAllText(Io(sidecar),JsonUtility.ToJson(proof,true));}
        }
        public static void ValidateForWorldAdoption(string tag,string reviewPath,string sidecarPath,string tests,string courseReceipt)
        {
            Passed(tests,NativeCase);RequirePassedConsumerSources(tests);string nativeSidecar=Path.Combine(Path.GetDirectoryName(Physical(tests)),"rendered-evidence","canonical-physical-course-world-"+tag+".json");if(!Hash(Digest(nativeSidecar),Digest(sidecarPath)))throw new InvalidOperationException("Only the exact sidecar from its actual specific passed native case may enter adoption.");
            var side=JsonUtility.FromJson<Receipt>(File.ReadAllText(Io(sidecarPath)));if(side.status!="CANONICAL_PHYSICAL_COURSE_WORLD_CLEARANCE_CERTIFIED_RECORDED_SOURCE_NOT_FLIGHT_OR_ADOPTION"||!string.IsNullOrEmpty(side.failure)||!side.sourceBytesPreserved||!SamePath(side.courseReviewReceipt,reviewPath)||!Hash(side.courseReviewReceiptSha256,Digest(reviewPath))||!Hash(side.worldTestsSha256,Digest(side.worldTests))||!Hash(side.worldNativeReceiptSha256,Digest(side.worldNativeReceipt)))throw new InvalidOperationException("Actual native course sidecar or paired world evidence changed.");
            var review=JsonUtility.FromJson<DragonKingdomMountedCourseReview.Receipt>(File.ReadAllText(Io(reviewPath)));string folder="Assets/TruckDragonKingdom/VisualCandidates/"+tag;string live=DragonRiderSaddleAuthoring.ValidateCanonicalPhysicalCoursePromotionCertificate(courseReceipt);RequireMetadata(side,review,live,tag,courseReceipt,Digest(Canonical),Digest(folder+"/Shared Dragon Kingdom.prefab"),Digest(folder+"/DragonKingdomPreview.unity"),Digest(folder+"/Integration Receipt.json"));
            foreach(var row in review.qualifiedSource.currentSourceSnapshot)if(!Hash(Digest(row.path),row.sha256))throw new InvalidOperationException("Actual source dependency changed after native review: "+row.path);foreach(var rig in review.rigs)if(!Hash(Digest(rig.controller),rig.controllerSha256)||rig.clips.Where((path,index)=>!Hash(Digest(path),rig.clipSha256[index])).Any())throw new InvalidOperationException("Actual source controller/effective clips changed after native review.");
        }
        public static void ValidateForCourseMigration(string tag,string reviewPath,string adoptionPath,string sidecarPath,string tests,string courseReceipt,string appearancePath)
        {
            // The old pre-world scene hash is not rewritten. Its actual
            // certificate/clearance is chained through the real world Apply
            // backup and saved-file result, before existing pose migration.
            Passed(tests,NativeCase);RequirePassedConsumerSources(tests);string nativeSidecar=Path.Combine(Path.GetDirectoryName(Physical(tests)),"rendered-evidence","canonical-physical-course-world-"+tag+".json");if(!Hash(Digest(nativeSidecar),Digest(sidecarPath)))throw new InvalidOperationException("Specific actual passed course-world sidecar required before recorded-source migration.");
            var side=JsonUtility.FromJson<Receipt>(File.ReadAllText(Io(sidecarPath)));var review=JsonUtility.FromJson<DragonKingdomMountedCourseReview.Receipt>(File.ReadAllText(Io(reviewPath)));var adoption=JsonUtility.FromJson<DragonKingdomWorldAdoptionPlan.WorldApplyReceipt>(File.ReadAllText(Io(adoptionPath)));string folder="Assets/TruckDragonKingdom/VisualCandidates/"+tag;
            if(side.status!="CANONICAL_PHYSICAL_COURSE_WORLD_CLEARANCE_CERTIFIED_RECORDED_SOURCE_NOT_FLIGHT_OR_ADOPTION"||!string.IsNullOrEmpty(side.failure)||!side.sourceBytesPreserved||!SamePath(side.courseReviewReceipt,reviewPath)||!Hash(side.courseReviewReceiptSha256,Digest(reviewPath))||!Hash(side.coursePromotionReceiptSha256,Digest(courseReceipt))||!Hash(side.worldTestsSha256,Digest(side.worldTests))||!Hash(side.worldNativeReceiptSha256,Digest(side.worldNativeReceipt)))throw new InvalidOperationException("The original native course/world evidence changed after adoption.");
            RequireMetadata(side,review,side.certificate,tag,courseReceipt,side.canonicalSceneSha256,Digest(folder+"/Shared Dragon Kingdom.prefab"),Digest(folder+"/DragonKingdomPreview.unity"),Digest(folder+"/Integration Receipt.json"));
            if(adoption==null||adoption.candidate!=tag||adoption.status!="ADOPTED_ORIGINAL_IDENTITIES_AND_MODES_PRESERVED_REQUIRES_ACTUAL_PLAY_ACCEPTANCE"||!string.IsNullOrEmpty(adoption.error)||!adoption.physicalGatesPassed||!adoption.allModeExternalStateAndSceneOwnedIdsPreserved||!adoption.originalGuidsPreserved||!adoption.originalSourceBytesPreserved||!Hash(adoption.mountedCourseReceiptSha256,Digest(reviewPath))||!SamePath(adoption.mountedCourseReceipt,reviewPath)||adoption.savedFiles==null||adoption.savedFiles.Select(row=>Physical(row.path)).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=adoption.savedFiles.Length)throw new InvalidOperationException("Actual successful identity-preserving world adoption must bind this exact native course.");
            var saved=adoption.savedFiles.Single(row=>row.path==Canonical);string before=Path.Combine(adoption.backupFolder,Canonical);if(!Hash(Digest(before),side.canonicalSceneSha256))throw new InvalidOperationException("World adoption did not start from the actual course-certified canonical scene.");
            if(string.IsNullOrEmpty(appearancePath)){if(!Hash(Digest(Canonical),saved.sha256))throw new InvalidOperationException("Current canonical scene differs from the actual world-adoption result.");}else DragonKingdomRoadSnowAuthoring.ValidateAdoptedSceneAfterAppearanceRefresh(adoptionPath,appearancePath,Canonical);
            var certificate=RequireCertificate(side.certificate,courseReceipt,side.canonicalSceneSha256);if(!Hash(Digest(Canonical+".meta"),certificate.metaSha256))throw new InvalidOperationException("Original canonical scene metadata changed after course/world transactions.");
            var mode=adoption.modes?.Single(row=>row.scene==Canonical);if(mode==null||!mode.sceneOwnedIdentitiesPreserved||!mode.originalWorldOverridesPreserved||mode.trucks!=4||mode.externalStateBefore!=mode.externalStateAfter)throw new InvalidOperationException("Actual world adoption did not preserve the four original NWH owners and nonworld state.");
            foreach(var row in review.qualifiedSource.currentSourceSnapshot)
            {
                string current=Digest(row.path);if(Hash(current,row.sha256))continue;var changed=adoption.savedFiles.Where(file=>SamePath(file.path,row.path)).ToArray();if(changed.Length!=1||!Hash(Digest(Path.Combine(adoption.backupFolder,changed[0].path)),row.sha256))throw new InvalidOperationException("Unaccounted source change after course/world adoption: "+row.path);
                if(!Hash(current,changed[0].sha256)&&!(row.path==Canonical&&!string.IsNullOrEmpty(appearancePath)))throw new InvalidOperationException("Source does not equal the actual saved world result: "+row.path);
            }
            foreach(var rig in review.rigs)if(!Hash(Digest(rig.controller),rig.controllerSha256)||rig.clips.Where((path,index)=>!Hash(Digest(path),rig.clipSha256[index])).Any())throw new InvalidOperationException("Recorded original source controllers/animations changed during world adoption.");
        }
    }
}
#endif
