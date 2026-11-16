#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using LWS.TruckDragonKingdom.Editor;
using UnityEngine;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        const string EditorEpochScope="EXACT_TWO_EDITOR_CONSUMERS_AND_TWO_BOUNDED_COURSE_GUARDS_NOT_SCENE_WORLD_OR_HUMAN_AUTHORITY";
        const string EditorEpochNativeCase="DragonRiderCanonicalEditorEpochTests.ActualPriorCourseAndPassedWorldSidecarQualifyOnlyExactReviewedEditorChanges";
        const string EditorEpochSource="Assets/TruckDragonRiders/Editor/DragonRiderSaddleAuthoring.CanonicalCourseEditorEpoch.cs";
        const string EditorEpochTests="Assets/TruckDragonRiders/Tests/Authoring/DragonRiderCanonicalEditorEpochTests.cs";
        static readonly string[] EditorEpochPaths={"Assets/TruckDragonKingdom/Editor/DragonKingdomWorldAdoptionApply.cs","Assets/TruckDragonKingdom/Editor/DragonKingdomRidersCourseMigration.cs",CourseSourceAuthoritySource,CourseCertificateSource};
        [Serializable]sealed class EditorEpochTransition{public string path,oldSha256,newSha256;}
        [Serializable]sealed class EditorEpochReview
        {
            public string scope,projectRoot,courseReceipt,courseReceiptSha256,sidecarTests,sidecarTestsSha256,sidecar,sidecarSha256;public EditorEpochTransition[] transitions;
        }
        [Serializable]sealed class EditorEpochProof
        {
            public string scope=EditorEpochScope,status="IN_PROGRESS",failure,review,reviewSha256,nativeEvidence,nativeInputSha256,courseReceiptSha256,sidecarSha256;public bool testOnly=true,sourceRowsExact,sceneDataExact,priorNativeAuthorityExact;public int mountRows,courseRows,changedProtectedRows;public EditorEpochTransition[] transitions;
        }
        sealed class EditorEpochPolicy{public EditorEpochReview review;public EditorEpochProof proof;}
        static EditorEpochPolicy activeCourseEditorEpoch;
        sealed class EditorEpochLease:IDisposable
        {bool disposed;public void Dispose(){if(disposed)return;disposed=true;activeCourseEditorEpoch=null;}}
        static string EpochArgument(string[] args,string key)
        {
            int count=args?.Count(value=>value==key)??0;if(count==0)return null;int at=Array.IndexOf(args,key);if(count!=1||at+1>=args.Length||string.IsNullOrWhiteSpace(args[at+1])||args[at+1].StartsWith("-",StringComparison.Ordinal))throw new ArgumentException("One explicit Editor epoch argument required: "+key);return Path.GetFullPath(args[at+1]);
        }
        static string EpochLogical(string path)
        {
            string value=path.Replace('\\','/'),root=Directory.GetParent(Application.dataPath).FullName.Replace('\\','/').TrimEnd('/')+"/";return value.StartsWith(root,StringComparison.OrdinalIgnoreCase)?value.Substring(root.Length):value;
        }
        static string EpochExpected(EditorEpochReview review,string path,string recorded)
        {
            if(review==null)return recorded;var found=review.transitions.Where(row=>row.path==EpochLogical(path)).ToArray();if(found.Length==0)return recorded;if(found.Length!=1||!CourseCertificateSame(found[0].oldSha256,recorded))throw new InvalidOperationException("Conflicting original source hash in the explicit Editor epoch: "+path);return found[0].newSha256;
        }
        static void EpochRequireComparison(EditorEpochReview review,string path,string recorded,string current)
        {if(!CourseCertificateSame(EpochExpected(review,path,recorded),current))throw new InvalidOperationException("Source changed outside its exact reviewed Editor epoch: "+path);}
        static void EpochRequireShape(EditorEpochReview review)
        {
            bool Hash(string value)=>value!=null&&value.Length==64&&value.All(Uri.IsHexDigit);
            if(review==null||review.scope!=EditorEpochScope||!CourseCertificateSame(Path.GetFullPath(review.projectRoot),Directory.GetParent(Application.dataPath).FullName)||review.transitions==null||review.transitions.Length!=4||!review.transitions.Select(row=>row.path).ToHashSet(StringComparer.Ordinal).SetEquals(EditorEpochPaths)||review.transitions.Select(row=>row.path).Distinct().Count()!=4||review.transitions.Any(row=>!Hash(row.oldSha256)||!Hash(row.newSha256)||CourseCertificateSame(row.oldSha256,row.newSha256)))throw new InvalidOperationException("Only the exact four reviewed Editor CSharp transitions may enter this epoch; no metadata/runtime/art/config exemptions.");
        }
        static FlightSourceHash[] EpochInput(string folder)
        {return JsonUtility.FromJson<FlightSourceRows>("{\"rows\":"+File.ReadAllText(Path.Combine(folder,"input-manifest.json"))+"}")?.rows??throw new InvalidOperationException("Actual native input source manifest required.");}
        static void EpochPassed(string tests,string name)
        {var xml=new XmlDocument();xml.Load(tests);var cases=xml.SelectNodes("//test-case").Cast<XmlNode>().Where(row=>row.Attributes["fullname"]?.Value==name).ToArray();if(cases.Length!=1||cases[0].Attributes["result"]?.Value!="Passed")throw new InvalidOperationException("Actual focused native epoch case has not passed: "+name);}
        static EditorEpochPolicy ValidateEditorEpoch(string reviewPath,string nativeEvidence,bool requirePassed)
        {
            var review=JsonUtility.FromJson<EditorEpochReview>(File.ReadAllText(reviewPath));EpochRequireShape(review);var input=EpochInput(nativeEvidence);var course=JsonUtility.FromJson<CanonicalCourseApply>(File.ReadAllText(review.courseReceipt));
            if(!CourseCertificateSame(Hash(review.courseReceipt),review.courseReceiptSha256)||course?.plan==null||!course.canonicalTarget||course.plan.testOnly||course.plan.testCanonicalMount||course.target!=DragonRidersAuthoring.ScenePath||course.status!="CANONICAL_PHYSICAL_COURSE_PROMOTED_REQUIRES_WORLD_AND_FLIGHT_ACCEPTANCE"||!string.IsNullOrEmpty(course.failure)||course.changedDocuments!=13||course.writes!=1||course.gates!=12||!course.savedReloaded||!course.allObjectIdsExact||!course.only13DocumentsChanged||!course.onlyAllowedFieldsChanged||!course.worldExact||!course.mountsExact||!course.nwhExact||!course.sourceClosureExact||!course.physicalControlsPassed||!course.rulesControlsPassed||!CourseCertificateSame(Hash(course.target),course.resultSha256)||!CourseCertificateSame(Hash(course.target+".meta"),course.metaSha256)||!CourseCertificateSame(HashTextForNativeReplacement(JsonUtility.ToJson(course.plan,true)),course.planSha256)||!CourseCertificateSame(Hash(Path.Combine(course.backupFolder,"before.unity")),course.sourceSha256)||!CourseCertificateSame(Hash(Path.Combine(course.backupFolder,"before.unity.meta")),course.metaSha256)||!CourseDocumentDeltaExact(File.ReadAllText(Path.Combine(course.backupFolder,"before.unity")),File.ReadAllText(course.target),course.plan,out int documents)||documents!=13)throw new InvalidOperationException("A real unchanged successful canonical physical-course Apply is required before any Editor source epoch.");
            var mount=JsonUtility.FromJson<CanonicalMountApply>(File.ReadAllText(course.plan.mountReceipt));if(mount?.plan==null||!mount.canonicalTarget||mount.status!="CANONICAL_MOUNTS_PROMOTED_REQUIRES_COURSE_WORLD_AND_GAMEPLAY_VALIDATION"||!string.IsNullOrEmpty(mount.failure)||mount.nativeCalls!=8||!mount.sourceClosureExact||!mount.profilesExact||!mount.nwhExact||!mount.worldExact||!mount.configurationExact||!CourseCertificateSame(Hash(course.plan.mountReceipt),course.plan.mountReceiptSha256)||!CourseCertificateSame(mount.resultSha256,course.sourceSha256)||!CourseCertificateSame(Hash(Path.Combine(mount.backupFolder,"before.unity")),mount.sourceSha256)||!CourseCertificateSame(HashTextForNativeReplacement(JsonUtility.ToJson(mount.plan,true)),mount.planSha256))throw new InvalidOperationException("The exact actual producer->mount->course data chain changed.");
            var originalBridge=EpochInput(course.plan.bridgeEvidence);
            foreach(string fault in new[]{"baseline","unknown-unrelated","rollback-after-save","stale-plan"})RequireRiderPassedManifest(course.plan.bridgeEvidence,"DragonRiderCanonicalPhysicalCourseTests.SavedMountResultKeepsAllIdentitiesThroughPhysicalCourseOrExactRollback(\""+fault+"\")");
            if(!CourseCertificateSame(Hash(Path.Combine(course.plan.bridgeEvidence,"tests.xml")),course.plan.bridgeTestsSha256)||!CourseCertificateSame(Hash(course.plan.bridgePositive),course.plan.bridgePositiveSha256))throw new InvalidOperationException("The actual prior native physical-course implementation proof changed.");
            foreach(var transition in review.transitions)
            {
                string old=transition.path==CourseSourceAuthoritySource||transition.path==CourseCertificateSource?originalBridge.Single(row=>row.path==transition.path).sha256:mount.plan.protectedSources.Single(row=>EpochLogical(row.path)==transition.path).sha256;
                var native=input.Where(row=>row.path==transition.path).ToArray();if(!CourseCertificateSame(old,transition.oldSha256)||native.Length!=1||!CourseCertificateSame(native[0].sha256,transition.newSha256)||!CourseCertificateSame(Hash(CourseCertificatePhysical(transition.path)),transition.newSha256))throw new InvalidOperationException("Reviewed old/new code does not match original proof and actual focused native input: "+transition.path);
            }
            foreach(string source in new[]{EditorEpochSource,EditorEpochTests})foreach(string path in new[]{source,source+".meta"}){var row=input.Where(item=>item.path==path).ToArray();if(row.Length!=1||!CourseCertificateSame(Hash(CourseCertificatePhysical(path)),row[0].sha256))throw new InvalidOperationException("Actual native epoch policy/test source changed: "+path);}
            EpochPassed(review.sidecarTests,DragonKingdomPhysicalCourseWorldBridge.NativeCase);string nativeSidecar=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(review.sidecarTests)),"rendered-evidence",Path.GetFileName(review.sidecar));
            var priorConsumerInput=EpochInput(Path.GetDirectoryName(Path.GetFullPath(review.sidecarTests)));foreach(string source in new[]{"Assets/TruckDragonRiders/Editor/DragonRiderSaddleAuthoring.CourseReviewSource.cs","Assets/TruckDragonRiders/Editor/DragonRiderSaddleAuthoring.PhysicalCourseReviewRouting.cs","Assets/TruckDragonKingdom/Editor/DragonKingdomPhysicalCourseWorldBridge.cs","Assets/TruckDragonKingdom/Tests/World/DragonKingdomPhysicalCourseConsumerTests.cs"})foreach(string path in new[]{source,source+".meta"})
            {var old=priorConsumerInput.Where(row=>row.path==path).ToArray();var current=input.Where(row=>row.path==path).ToArray();if(old.Length!=1||current.Length!=1||!CourseCertificateSame(old[0].sha256,current[0].sha256)||!CourseCertificateSame(Hash(CourseCertificatePhysical(path)),old[0].sha256))throw new InvalidOperationException("The actual passed331source consumer must remain exact outside the separate four-path Editor epoch: "+path);}
            var side=JsonUtility.FromJson<DragonKingdomPhysicalCourseWorldBridge.Receipt>(File.ReadAllText(review.sidecar));var certificate=JsonUtility.FromJson<DragonKingdomPhysicalCourseWorldBridge.Certificate>(side.certificate);
            if(!CourseCertificateSame(Hash(review.sidecarTests),review.sidecarTestsSha256)||!CourseCertificateSame(Hash(review.sidecar),review.sidecarSha256)||!CourseCertificateSame(Hash(nativeSidecar),review.sidecarSha256)||side.status!="CANONICAL_PHYSICAL_COURSE_WORLD_CLEARANCE_CERTIFIED_RECORDED_SOURCE_NOT_FLIGHT_OR_ADOPTION"||!string.IsNullOrEmpty(side.failure)||!side.sourceBytesPreserved||side.freshPoseSamples!=0||side.recordedInfernoPoses!=699||side.recordedFirePoses!=537||!CourseCertificateSame(Path.GetFullPath(side.coursePromotionReceipt),Path.GetFullPath(review.courseReceipt))||!CourseCertificateSame(side.coursePromotionReceiptSha256,review.courseReceiptSha256)||!CourseCertificateSame(side.canonicalSceneSha256,course.resultSha256)||certificate==null||!certificate.productionEligible||!certificate.sourceClosureExact||!certificate.currentProfilesExact||!certificate.currentNwhExact||!certificate.currentWorldExact||!certificate.currentExternalConfigurationExact||certificate.choices!=8||certificate.originalTrucks!=4||certificate.recordedInfernoPoses!=699||certificate.recordedFirePoses!=537||certificate.freshPoseSamples!=0||!CourseCertificateSame(certificate.currentSceneSha256,course.resultSha256))throw new InvalidOperationException("The actual specific passed331source canonical course/world sidecar is required; no metadata-only or TEST_ONLY authority.");
            string world="Assets/TruckDragonKingdom/VisualCandidates/"+side.candidate;foreach(var pair in new[]{new[]{world+"/Shared Dragon Kingdom.prefab",side.worldPrefabSha256},new[]{world+"/DragonKingdomPreview.unity",side.worldPreviewSha256},new[]{world+"/Integration Receipt.json",side.worldIntegrationSha256},new[]{side.courseReviewReceipt,side.courseReviewReceiptSha256},new[]{side.worldTests,side.worldTestsSha256},new[]{side.worldNativeReceipt,side.worldNativeReceiptSha256}})if(!CourseCertificateSame(Hash(pair[0]),pair[1]))throw new InvalidOperationException("Actual prior course/world evidence or data changed: "+pair[0]);
            CourseRequireProtectedInventoryShape(mount.plan.protectedSources);var hashes=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);var seen=new HashSet<string>(StringComparer.Ordinal);int changed=0;
            foreach(var row in mount.plan.protectedSources.Concat(course.plan.protectedSources))
            {
                string path=CourseCertificatePhysical(row.path);if(!hashes.TryGetValue(path,out string current)){current=Hash(path);hashes.Add(path,current);}EpochRequireComparison(review,row.path,row.sha256,current);string logical=EpochLogical(row.path);if(EditorEpochPaths.Contains(logical)){seen.Add(logical);if(!CourseCertificateSame(current,row.sha256))changed++;}
            }
            if(!seen.SetEquals(EditorEpochPaths))throw new InvalidOperationException("All four source transitions must bind actual original closure rows, not invented paths.");
            var proof=new EditorEpochProof{review=Path.GetFullPath(reviewPath),reviewSha256=Hash(reviewPath),nativeEvidence=Path.GetFullPath(nativeEvidence),nativeInputSha256=Hash(Path.Combine(nativeEvidence,"input-manifest.json")),courseReceiptSha256=review.courseReceiptSha256,sidecarSha256=review.sidecarSha256,sourceRowsExact=true,sceneDataExact=true,priorNativeAuthorityExact=true,mountRows=mount.plan.protectedSources.Length,courseRows=course.plan.protectedSources.Length,changedProtectedRows=changed,transitions=review.transitions};
            if(requirePassed)
            {
                EpochPassed(Path.Combine(nativeEvidence,"tests.xml"),EditorEpochNativeCase);foreach(string fault in new[]{"missing","duplicate","unlisted","wrong-old","wrong-new"})EpochPassed(Path.Combine(nativeEvidence,"tests.xml"),"DragonRiderCanonicalEditorEpochTests.ReviewedEpochRejectsMissingConflictingAndUnlistedTransitions(\""+fault+"\")");EpochPassed(Path.Combine(nativeEvidence,"tests.xml"),"DragonRiderCanonicalEditorEpochTests.DefaultAndEveryUnlistedRuntimeArtConfigOrMetadataRowRemainStrict");
                string actual=Path.Combine(nativeEvidence,"rendered-evidence","RiderSaddleAuthoring","EditorConsumerEpoch","actual-editor-epoch-kernel.json");var native=JsonUtility.FromJson<EditorEpochProof>(File.ReadAllText(actual));if(native==null||!native.testOnly||native.status!="TEST_ONLY_EXACT_EDITOR_EPOCH_KERNEL_PASSED_NO_SCENE_OR_ADOPTION_AUTHORITY"||!string.IsNullOrEmpty(native.failure)||!native.sourceRowsExact||!native.sceneDataExact||!native.priorNativeAuthorityExact||!CourseCertificateSame(native.reviewSha256,proof.reviewSha256)||!CourseCertificateSame(native.nativeInputSha256,proof.nativeInputSha256)||!CourseCertificateSame(native.courseReceiptSha256,proof.courseReceiptSha256)||!CourseCertificateSame(native.sidecarSha256,proof.sidecarSha256)||native.mountRows!=proof.mountRows||native.courseRows!=proof.courseRows)throw new InvalidOperationException("Specific genuine passed native epoch kernel must bind this exact reviewed code and prior real data authority.");proof.testOnly=false;proof.status="PASSED_FOCUSED_NATIVE_EDITOR_SOURCE_EPOCH_NOT_SCENE_OR_ADOPTION_AUTHORITY";
            }
            else proof.status="TEST_ONLY_EXACT_EDITOR_EPOCH_KERNEL_PASSED_NO_SCENE_OR_ADOPTION_AUTHORITY";return new EditorEpochPolicy{review=review,proof=proof};
        }
        static IDisposable OpenCourseEditorEpochScope()
        {
            if(activeCourseEditorEpoch!=null)throw new InvalidOperationException("Nested Editor source epochs are not allowed.");var args=Environment.GetCommandLineArgs();string review=EpochArgument(args,"-tsRiderEditorConsumerEpochReview"),evidence=EpochArgument(args,"-tsRiderEditorConsumerEpochEvidence");if(review==null&&evidence==null)return new EditorEpochLease();if(review==null||evidence==null)throw new InvalidOperationException("Both explicit reviewed epoch and its actual passed native evidence are required.");activeCourseEditorEpoch=ValidateEditorEpoch(review,evidence,true);return new EditorEpochLease();
        }
        static string CourseEpochExpectedHash(string path,string original)
        {
            bool requested=Environment.GetCommandLineArgs().Contains("-tsRiderEditorConsumerEpochReview");if(!requested)return original;if(activeCourseEditorEpoch==null||activeCourseEditorEpoch.proof.testOnly)throw new InvalidOperationException("No passed production Editor source epoch is active.");return EpochExpected(activeCourseEditorEpoch.review,path,original);
        }
        static void CourseRequireRecordedEpochSource(FlightSourceHash[] manifest,string source,string context)
        {
            var rows=manifest.Where(row=>row.path==source).ToArray();if(rows.Length!=1)throw new InvalidOperationException("Exactly one actual prior native source row required: "+source);string expected=CourseEpochExpectedHash(source,rows[0].sha256);
            if(CourseCertificateSame(expected,rows[0].sha256)){RequireRecordedStartupSourceHash(manifest,source,context);return;}if(!CourseCertificateSame(Hash(CourseCertificatePhysical(source)),expected))throw new InvalidOperationException("Exact reviewed/native-validated course guard source differs: "+source);
        }
        static string CourseEpochDescription()=>activeCourseEditorEpoch==null?null:JsonUtility.ToJson(activeCourseEditorEpoch.proof,true);
        public static string ProbeActualEditorConsumerEpoch(string reviewPath)
        {
            string output=EpochArgument(Environment.GetCommandLineArgs(),"-tsDragonOutput");if(string.IsNullOrEmpty(output))throw new InvalidOperationException("Actual native output required for the TEST_ONLY epoch kernel.");var policy=ValidateEditorEpoch(reviewPath,Directory.GetParent(output).FullName,false);string folder=Path.Combine(output,"RiderSaddleAuthoring","EditorConsumerEpoch");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"actual-editor-epoch-kernel.json"),JsonUtility.ToJson(policy.proof,true));return JsonUtility.ToJson(policy.proof,true);
        }
    }
}
#endif
