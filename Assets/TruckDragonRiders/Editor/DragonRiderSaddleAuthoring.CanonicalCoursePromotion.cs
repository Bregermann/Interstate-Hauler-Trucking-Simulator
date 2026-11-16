#if UNITY_EDITOR && VISTA
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
        const string CoursePromotionSource="Assets/TruckDragonRiders/Editor/DragonRiderSaddleAuthoring.CanonicalCoursePromotion.cs";
        const string CourseCertificateSource="Assets/TruckDragonRiders/Editor/DragonRiderSaddleAuthoring.CanonicalCourseCertificate.cs";
        const string CourseSourceAuthoritySource="Assets/TruckDragonRiders/Editor/DragonRiderSaddleAuthoring.CanonicalCourseSourceAuthority.cs";
        const string CourseRecordedMetadataSource="Assets/TruckDragonRiders/Editor/DragonRiderSaddleAuthoring.CanonicalCourseRecordedSource.cs";
        const string CourseCommandSource="Assets/TruckDragonRiders/Editor/DragonRiderSaddleAuthoring.CanonicalCourseCommand.cs";
        static string[] CourseBridgeSourcePaths=>new[]{CoursePromotionSource,CourseCertificateSource,CourseSourceAuthoritySource,CourseRecordedMetadataSource,CourseCommandSource,CoursePromotionTestSource};
        const string CoursePromotionTestSource="Assets/TruckDragonRiders/Tests/Authoring/DragonRiderCanonicalPhysicalCourseTests.cs";
        [Serializable]sealed class CourseIdentity{public string key,id;}
        [Serializable]sealed class CanonicalCoursePlan
        {
            public string scope="ONLY_EXISTING12_RING_SCALES_MATCH12_APERTURES_AND_STRAIGHT_EXIT_NOT_WORLD_ROUTE_OR_RIG_ADOPTION",projectRoot,target,sceneSha256,metaSha256,sceneGuid,mountReceipt,mountReceiptSha256,courseEvidence,courseTestsSha256,courseReceiptSha256,bridgeEvidence,bridgeTestsSha256,bridgePositive,bridgePositiveSha256,matchDocument,worldState,externalConfiguration;
            public bool testOnly,testCanonicalMount;public string[] transformDocuments,actorStates;public Vector3[] oldScales;public DragonRiderGateDefinition[] originalGates;public float originalExit;
            public CourseIdentity[] identities;public FlightSourceHash[] protectedSources;
        }
        [Serializable]sealed class CanonicalCourseApply
        {
            public string scope,status="IN_PROGRESS",failure,target,sourceSha256,resultSha256,metaSha256,sceneGuid,planSha256,backupFolder;
            public bool canonicalTarget,savedReloaded,allObjectIdsExact,only13DocumentsChanged,onlyAllowedFieldsChanged,worldExact,mountsExact,nwhExact,sourceClosureExact,physicalControlsPassed,rulesControlsPassed,rollbackExact,publicReaderRejectsTest;
            public int writes,changedDocuments,objects,gates;public float maximumNwhDimensionDrift;public PhysicalCourseReceipt geometry;public CanonicalCoursePlan plan;
        }
        static CanonicalCourseApply lastCanonicalCourseApply;
        static string CoursePromotionReceiptPath(string evidence)=>Path.Combine(evidence,"rendered-evidence","RiderSaddleAuthoring","PhysicalCourse","saved-physical-large-ring-course-candidate.json");
        static string CourseLocalId(UnityEngine.Object value)=>GlobalObjectId.GetGlobalObjectIdSlow(value).targetObjectId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        static Dictionary<string,string> CourseDocuments(string text)
        {
            var matches=Regex.Matches(text,@"(?m)^--- !u!\d+ &(-?\d+)(?: stripped)?\r?$\n");var result=new Dictionary<string,string>(StringComparer.Ordinal);
            for(int i=0;i<matches.Count;i++){int end=i+1<matches.Count?matches[i+1].Index:text.Length;result.Add(matches[i].Groups[1].Value,text.Substring(matches[i].Index,end-matches[i].Index));}
            if(result.Count<100)throw new InvalidOperationException("A complete text-serialized scene with original local object IDs is required.");return result;
        }
        static string CourseMaskDocument(string text,bool match,bool allowAbsentOldExit=false)
        {
            if(!match)
            {
                var scale=Regex.Matches(text,@"(?m)^  m_LocalScale: \{[^\r\n]+\}\r?$");if(scale.Count!=1)throw new InvalidOperationException("One native local-scale field required for each original ring Transform.");return text.Remove(scale[0].Index,scale[0].Length).Insert(scale[0].Index,"  m_LocalScale: <AUTHORIZED_COURSE_SCALE>");
            }
            var gates=Regex.Match(text,@"(?ms)^  gates:\r?\n.*?(?=^  [A-Za-z_][A-Za-z_0-9]*:|\z)");if(!gates.Success)throw new InvalidOperationException("Exact original serialized Match.gates block required.");
            var radii=Regex.Matches(gates.Value,@"(?m)^    clearRadius: [^\r\n]+\r?$");if(radii.Count!=12)throw new InvalidOperationException("Only twelve scoring apertures may be authorized.");
            string masked=Regex.Replace(gates.Value,@"(?m)^    clearRadius: [^\r\n]+\r?$","    clearRadius: <AUTHORIZED_COURSE_APERTURE>");text=text.Remove(gates.Index,gates.Length).Insert(gates.Index,masked);
            var exits=Regex.Matches(text,@"(?m)^  aiStraightGateExitDistance: [^\r\n]+\r?$\n?");if(exits.Count>1||exits.Count==0&&!allowAbsentOldExit)throw new InvalidOperationException("One explicit resulting straight-egress field required; only the original omitted default may be absent.");return exits.Count==0?text:text.Remove(exits[0].Index,exits[0].Length);
        }
        static bool CourseDocumentDeltaExact(string before,string after,CanonicalCoursePlan plan,out int changed)
        {
            var old=CourseDocuments(before);var current=CourseDocuments(after);changed=0;if(!old.Keys.ToHashSet().SetEquals(current.Keys)||plan.transformDocuments?.Length!=12||plan.transformDocuments.Distinct().Count()!=12||plan.transformDocuments.Contains(plan.matchDocument))return false;
            // Do not silently discard scene headers or line-ending changes.
            if(before.Substring(0,before.IndexOf("--- !u!",StringComparison.Ordinal))!=after.Substring(0,after.IndexOf("--- !u!",StringComparison.Ordinal)))return false;
            var transforms=plan.transformDocuments.ToHashSet();foreach(var row in old)
            {
                string value=current[row.Key];if(row.Value==value)continue;changed++;
                if(row.Key!=plan.matchDocument&&!transforms.Contains(row.Key))return false;
                if(CourseMaskDocument(row.Value,row.Key==plan.matchDocument,row.Key==plan.matchDocument)!=CourseMaskDocument(value,row.Key==plan.matchDocument))return false;
            }
            return changed==13;
        }
        static MeshCollider[] CourseOrderedOriginalRings(Scene scene)=>Enumerable.Range(1,12).Select(i=>CoursePhysicalGates(scene).Single(c=>c.name=="Aerial gate "+i)).ToArray();
        static void RequireCourseSourceRings(DragonRidersMatch match,MeshCollider[] rings)
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(CourseRingMesh);var material=AssetDatabase.LoadAssetAtPath<Material>(CourseRingMaterial);
            if(match.gates.Length!=12||CoursePhysicalGates(match.gameObject.scene).Length!=12||rings.Length!=12||mesh==null||material==null)throw new InvalidOperationException("Exactly twelve existing source-art rings and scoring gates required.");
            for(int i=0;i<12;i++)
            {
                var ring=rings[i];var filter=ring.GetComponent<MeshFilter>();var renderer=ring.GetComponent<MeshRenderer>();
                if(ring.sharedMesh!=mesh||filter==null||filter.sharedMesh!=mesh||renderer==null||renderer.sharedMaterial!=material||ring.convex||ring.isTrigger||!ring.enabled||ring.transform.parent==null||!ring.transform.parent.lossyScale.Equals(Vector3.one)||Vector3.Distance(ring.transform.position,match.gates[i].center)>.001f||Quaternion.Angle(ring.transform.rotation,match.gates[i].rotation)>.05f)throw new InvalidOperationException("Unknown original physical-ring source/configuration/pose; preserve designer edits.");
            }
        }
        static PhysicalCourseReceipt RequireCoursePromotionRecipe(string evidence,string producerOriginalSceneSha)
        {
            RequireRiderPassedManifest(evidence,"DragonRiderPhysicalCourseTests.SavedMeasuredLargePhysicalRingsRetainSourceArtAndOrderedScoringWithNativeRimControls");var proof=JsonUtility.FromJson<PhysicalCourseReceipt>(File.ReadAllText(CoursePromotionReceiptPath(evidence)));
            if(proof==null||!string.IsNullOrEmpty(proof.failure)||proof.sourceScene!=DragonRidersAuthoring.ScenePath||!proof.sourceBytesExact||!proof.savedReloaded||!proof.orderedRouteExact||!proof.sourceArtExact||!proof.physicalControlsPassed||!proof.rulesControlsPassed||proof.gates?.Count!=12||proof.clearRadius!=60||proof.straightExitDistance!=55||!CourseCertificateSame(Hash(proof.candidateScene),proof.candidateSceneSha256)||proof.clearRadius<proof.measuredAllAttitudeRadius+5||proof.straightExitDistance<proof.measuredRearReach+proof.physicalDepth*.5f+5)throw new InvalidOperationException("Actual saved twelve-ring60m/55m physical native authority required.");
            foreach(var row in proof.protectedSources)
            {
                string expected=row.path==DragonRidersAuthoring.ScenePath?producerOriginalSceneSha:Hash(CourseCertificatePhysical(row.path));if(!CourseCertificateSame(expected,row.sha256))throw new InvalidOperationException("Physical recipe source or original producer scene changed: "+row.path);
            }
            var measured=new ActualMountedCourseEnvelope();CompleteActualCourseEnvelopeDimensions(measured);
            if(Mathf.Abs(measured.sourceRingInnerAperture-proof.sourceInnerRadius)>.000001f||Mathf.Abs(measured.sourceRingOuterRadius-proof.sourceOuterRadius)>.000001f||Mathf.Abs(measured.sourceRingDepth-proof.sourceDepth)>.000001f||Mathf.Abs(proof.uniformScale*proof.sourceInnerRadius-60)>.001f)throw new InvalidOperationException("Actual current source mesh dimensions no longer match qualified aperture.");return proof;
        }
        static string[] CourseProtectedPaths(string target,string mountReceipt,CanonicalMountApply mount,string courseEvidence,string bridgeEvidence=null,string bridgePositive=null)=>mount.plan.protectedSources.Select(row=>CourseCertificatePhysical(row.path)).Concat(AssetDatabase.GetDependencies(target,true).Select(CurrentRiderPhysicalDependency)).Concat(new[]{mount.target,mountReceipt,Path.Combine(mount.backupFolder,"before.unity"),Path.Combine(mount.backupFolder,"before.unity.meta"),Path.Combine(courseEvidence,"tests.xml"),CoursePromotionReceiptPath(courseEvidence),CoursePromotionSource,CourseCertificateSource,CourseSourceAuthoritySource,CourseRecordedMetadataSource,CourseCommandSource,CoursePromotionTestSource}).Concat(string.IsNullOrEmpty(bridgeEvidence)?Array.Empty<string>():new[]{Path.Combine(bridgeEvidence,"tests.xml"),bridgePositive}).SelectMany(path=>new[]{path,path+".meta"}).Where(File.Exists).Where(path=>!CourseCertificateSame(Path.GetFullPath(path),Path.GetFullPath(target))).Distinct().OrderBy(path=>path,StringComparer.Ordinal).ToArray();
        static CanonicalCoursePlan CaptureCoursePlan(string target,string mountReceipt,CanonicalMountApply mount,string courseEvidence,bool testOnly,bool testCanonicalMount=false,string bridgeEvidence=null)
        {
            var plan=new CanonicalCoursePlan{testOnly=testOnly,testCanonicalMount=testCanonicalMount,projectRoot=Directory.GetParent(Application.dataPath).FullName,target=target,sceneSha256=Hash(target),metaSha256=Hash(target+".meta"),sceneGuid=AssetDatabase.AssetPathToGUID(target),mountReceipt=mountReceipt,mountReceiptSha256=Hash(mountReceipt),courseEvidence=courseEvidence,courseTestsSha256=Hash(Path.Combine(courseEvidence,"tests.xml")),courseReceiptSha256=Hash(CoursePromotionReceiptPath(courseEvidence))};
            if(!testOnly){plan.bridgeEvidence=bridgeEvidence;plan.bridgePositive=RequireNativePhysicalCourseBridge(bridgeEvidence,mountReceipt,courseEvidence,mount);plan.bridgeTestsSha256=Hash(Path.Combine(bridgeEvidence,"tests.xml"));plan.bridgePositiveSha256=Hash(plan.bridgePositive);}
            plan.protectedSources=CourseProtectedPaths(target,mountReceipt,mount,courseEvidence,plan.bridgeEvidence,plan.bridgePositive).Select(path=>new FlightSourceHash{path=path,sha256=Hash(path)}).ToArray();var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;
            try
            {
                scene=EditorSceneManager.OpenScene(target,OpenSceneMode.Single);var match=CourseMatch(scene);var rings=CourseOrderedOriginalRings(scene);RequireCourseSourceRings(match,rings);
                if(match.gates.Any(g=>g.clearRadius!=32))throw new InvalidOperationException("This explicit transition accepts only the original twelve32m apertures.");
                var actors=NativeEightActors(match);plan.actorStates=actors.Select(NativeEightChoiceState).ToArray();plan.identities=NativeEightProtectedIds(scene,Array.Empty<DragonCharacter>()).Select(p=>new CourseIdentity{key=p.Key,id=p.Value}).ToArray();plan.matchDocument=CourseLocalId(match);plan.transformDocuments=rings.Select(c=>CourseLocalId(c.transform)).ToArray();plan.oldScales=rings.Select(c=>c.transform.localScale).ToArray();plan.originalGates=match.gates.Select(CopyCourseGate).ToArray();plan.originalExit=match.aiStraightGateExitDistance;plan.worldState=HashTextForNativeReplacement(NativeReplacementWorldState(match.world));plan.externalConfiguration=HashTextForNativeReplacement(ProductionSceneConfiguration(scene,new HashSet<GameObject>(actors.Select(a=>a.gameObject))));
                if(plan.externalConfiguration!=mount.externalAfter||plan.worldState!=mount.worldAfter)throw new InvalidOperationException("Current scene is not the exact prior successful mount configuration/world.");
            }
            finally{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
            RequireCoursePlanCurrent(plan);return plan;
        }
        static void RequireCoursePlanCurrent(CanonicalCoursePlan plan)
        {
            if(plan==null||!CourseCertificateSame(plan.projectRoot,Directory.GetParent(Application.dataPath).FullName)||!CourseCertificateSame(Hash(plan.target),plan.sceneSha256)||!CourseCertificateSame(Hash(plan.target+".meta"),plan.metaSha256)||AssetDatabase.AssetPathToGUID(plan.target)!=plan.sceneGuid||!CourseCertificateSame(Hash(plan.mountReceipt),plan.mountReceiptSha256)||!CourseCertificateSame(Hash(Path.Combine(plan.courseEvidence,"tests.xml")),plan.courseTestsSha256)||!CourseCertificateSame(Hash(CoursePromotionReceiptPath(plan.courseEvidence)),plan.courseReceiptSha256)||plan.identities?.Length<1000||plan.actorStates?.Length!=8||plan.originalGates?.Length!=12||plan.oldScales?.Length!=12||plan.transformDocuments?.Length!=12||plan.transformDocuments.Distinct().Count()!=12||plan.transformDocuments.Contains(plan.matchDocument)||plan.protectedSources==null||plan.protectedSources.Length<1000||plan.protectedSources.Any(row=>!CourseCertificateSame(Hash(row.path),row.sha256)))throw new InvalidOperationException("Stale/truncated course plan or changed actual source closure; no write allowed.");
            var upstream=JsonUtility.FromJson<CanonicalMountApply>(File.ReadAllText(plan.mountReceipt));if(!CourseProtectedPaths(plan.target,plan.mountReceipt,upstream,plan.courseEvidence,plan.bridgeEvidence,plan.bridgePositive).SequenceEqual(plan.protectedSources.Select(row=>row.path)))throw new InvalidOperationException("Exact course source inventory membership changed or was truncated.");if(!plan.testOnly&&(plan.testCanonicalMount||RequireNativePhysicalCourseBridge(plan.bridgeEvidence,plan.mountReceipt,plan.courseEvidence,upstream)!=plan.bridgePositive||!CourseCertificateSame(Hash(Path.Combine(plan.bridgeEvidence,"tests.xml")),plan.bridgeTestsSha256)||!CourseCertificateSame(Hash(plan.bridgePositive),plan.bridgePositiveSha256)))throw new InvalidOperationException("Exact native bridge acceptance changed; no course write allowed.");
        }
        public static string BuildCanonicalPhysicalCoursePromotionPlan(string mountReceipt,string physicalCourseEvidence,string bridgeNativeEvidence)
        {
            RequirePromotionEditorContext();var mount=JsonUtility.FromJson<CanonicalMountApply>(File.ReadAllText(mountReceipt));var authority=ReadCourseMountSourceAuthority(mountReceipt,mount,false,null);RequireProductionCourseMountAuthority(authority);RequireCoursePromotionRecipe(physicalCourseEvidence,mount.sourceSha256);
            return JsonUtility.ToJson(CaptureCoursePlan(DragonRidersAuthoring.ScenePath,mountReceipt,mount,physicalCourseEvidence,false,false,bridgeNativeEvidence),true);
        }
        public static string ApplyCanonicalPhysicalCoursePromotion(string planJson,string outputDirectory)=>ApplyCoursePromotion(planJson,outputDirectory,null);
        static string CourseNeutralExternal(DragonRidersMatch match,CanonicalCoursePlan plan)
        {
            var rings=CourseOrderedOriginalRings(match.gameObject.scene);var scales=rings.Select(c=>c.transform.localScale).ToArray();var gates=match.gates;float exit=match.aiStraightGateExitDistance;
            try{for(int i=0;i<12;i++)rings[i].transform.localScale=plan.oldScales[i];match.gates=plan.originalGates.Select(CopyCourseGate).ToArray();match.aiStraightGateExitDistance=plan.originalExit;return HashTextForNativeReplacement(ProductionSceneConfiguration(match.gameObject.scene,new HashSet<GameObject>(NativeEightActors(match).Select(a=>a.gameObject))));}
            finally{for(int i=0;i<12;i++)rings[i].transform.localScale=scales[i];match.gates=gates;match.aiStraightGateExitDistance=exit;}
        }
        static PhysicalCourseReceipt InspectPromotedCourse(Scene scene,CanonicalCoursePlan plan,PhysicalCourseReceipt recipe)
        {
            var match=CourseMatch(scene);var rings=CourseOrderedOriginalRings(scene);RequireCourseSourceRings(match,rings);var result=JsonUtility.FromJson<PhysicalCourseReceipt>(JsonUtility.ToJson(recipe));result.scope="CURRENT_SAVED_COURSE_REMEASURE_USING_ORIGINAL_V405_SOURCE_RECIPE_NO_NEW_MOUNT_POSES_OR_FLIGHT_ACCEPTANCE";result.candidateScene=scene.path;result.candidateSceneSha256=Hash(scene.path);result.sourceBytesExact=false;result.savedReloaded=false;result.gates.Clear();Physics.SyncTransforms();
            for(int i=0;i<12;i++)result.gates.Add(InspectPhysicalCourseGate(rings[i],match.gates[i],plan.originalGates[i],AssetDatabase.LoadAssetAtPath<Mesh>(CourseRingMesh),AssetDatabase.LoadAssetAtPath<Material>(CourseRingMaterial),result,i));
            result.physicalControlsPassed=result.gates.All(g=>g.originalMeshAndMaterial&&g.renderColliderMatrixExact&&g.sourcePosePreserved&&g.maximumMatrixDifference<=.001f&&Mathf.Abs(g.actualInnerRadius-60)<=.001f&&g.centerClearBothDirections&&g.solidRimHits==16&&g.clearInnerRays==8&&g.clearOuterRays==8);result.rulesControlsPassed=InspectPhysicalCourseRules(match.gates,CourseOriginalShapeRadius(match));
            if(match.aiStraightGateExitDistance!=55||match.gates.Any(g=>g.clearRadius!=60)||!result.physicalControlsPassed||!result.rulesControlsPassed)throw new InvalidOperationException("Actual saved source-art collider/scoring/ordered-rule/egress gates failed.");return result;
        }
        static string ApplyCoursePromotion(string planJson,string outputDirectory,string fault)
        {
            RequirePromotionEditorContext();var plan=JsonUtility.FromJson<CanonicalCoursePlan>(planJson);RequireCoursePlanCurrent(plan);var mount=JsonUtility.FromJson<CanonicalMountApply>(File.ReadAllText(plan.mountReceipt));
            if(plan.testOnly){if(!plan.target.StartsWith("Assets/TruckDragonRiders/Tests/Generated/CoursePromotion-",StringComparison.Ordinal)||plan.target.Contains(".."))throw new InvalidOperationException("Only explicit owned disposable course copies allowed.");if(plan.testCanonicalMount)RequireProductionCourseMountAuthority(ReadCourseMountSourceAuthority(plan.mountReceipt,mount,false,null));else RequireDisposableCourseMountBaseline(plan.mountReceipt,mount);}
            else{if(plan.target!=DragonRidersAuthoring.ScenePath||fault!=null)throw new InvalidOperationException("Only explicit canonical production target allowed.");RequireProductionCourseMountAuthority(ReadCourseMountSourceAuthority(plan.mountReceipt,mount,false,null));}
            var recipe=RequireCoursePromotionRecipe(plan.courseEvidence,mount.sourceSha256);string permitted=Path.Combine(RiderEvidenceHostRoot(plan.courseEvidence),"Logs","TruckSports","DragonKingdom")+Path.DirectorySeparatorChar;outputDirectory=Path.GetFullPath(outputDirectory);if(!(outputDirectory+Path.DirectorySeparatorChar).StartsWith(permitted,StringComparison.OrdinalIgnoreCase)||Directory.Exists(outputDirectory)||File.Exists(outputDirectory))throw new InvalidOperationException("Fresh private Logs transaction directory required.");Directory.CreateDirectory(outputDirectory);
            var before=File.ReadAllBytes(plan.target);var meta=File.ReadAllBytes(plan.target+".meta");var receipt=new CanonicalCourseApply{scope=plan.testOnly?"TEST_ONLY_EXISTING_V426_SAVED_MOUNTS_COURSE_KERNEL_NOT_CANONICAL":"EXPLICIT_CANONICAL_PHYSICAL_COURSE_ONLY_NOT_WORLD_OR_FLIGHT_ACCEPTANCE",target=plan.target,canonicalTarget=!plan.testOnly,sourceSha256=plan.sceneSha256,metaSha256=plan.metaSha256,sceneGuid=plan.sceneGuid,plan=plan,planSha256=HashTextForNativeReplacement(planJson),backupFolder=Path.Combine(outputDirectory,"before-course")};lastCanonicalCourseApply=receipt;Directory.CreateDirectory(receipt.backupFolder);File.WriteAllBytes(Path.Combine(receipt.backupFolder,"before.unity"),before);File.WriteAllBytes(Path.Combine(receipt.backupFolder,"before.unity.meta"),meta);
            var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;Exception failure=null;bool complete=false;string report=Path.Combine(outputDirectory,"physical-course-promotion.json");
            try
            {
                scene=EditorSceneManager.OpenScene(plan.target,OpenSceneMode.Single);var match=CourseMatch(scene);var rings=CourseOrderedOriginalRings(scene);RequireCourseSourceRings(match,rings);if(fault=="unknown-unrelated")match.fixedSeed++;
                if(HashTextForNativeReplacement(ProductionSceneConfiguration(scene,new HashSet<GameObject>(NativeEightActors(match).Select(a=>a.gameObject))))!=plan.externalConfiguration||!NativeEightActors(match).Select(NativeEightChoiceState).SequenceEqual(plan.actorStates))throw new InvalidOperationException("Unknown current scene/profile edit; no course write allowed.");var nwh=CaptureProductionNwhFields(match);
                for(int i=0;i<12;i++){rings[i].transform.localScale=Vector3.one*recipe.uniformScale;match.gates[i].clearRadius=60;EditorUtility.SetDirty(rings[i].transform);}match.aiStraightGateExitDistance=55;EditorUtility.SetDirty(match);EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene,plan.target))throw new InvalidOperationException("Original course save failed.");receipt.writes++;if(fault=="rollback-after-save")throw new InvalidOperationException("INJECTED_AFTER_REAL_COURSE_SAVE");EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);scene=EditorSceneManager.OpenScene(plan.target,OpenSceneMode.Single);match=CourseMatch(scene);receipt.savedReloaded=true;
                receipt.geometry=InspectPromotedCourse(scene,plan,recipe);receipt.physicalControlsPassed=receipt.geometry.physicalControlsPassed;receipt.rulesControlsPassed=receipt.geometry.rulesControlsPassed;receipt.gates=12;receipt.allObjectIdsExact=NativeReplacementIdsAgree(plan.identities.ToDictionary(p=>p.key,p=>p.id),NativeEightProtectedIds(scene,Array.Empty<DragonCharacter>()));receipt.objects=plan.identities.Length;receipt.mountsExact=NativeEightActors(match).Select(NativeEightChoiceState).SequenceEqual(plan.actorStates);receipt.worldExact=HashTextForNativeReplacement(NativeReplacementWorldState(match.world))==plan.worldState;
                var afterNwh=CaptureProductionNwhFields(match);receipt.nwhExact=ExactProductionNwhFields(nwh,afterNwh)&&FiniteWorldBoxDimensionsAgree(nwh,afterNwh,out receipt.maximumNwhDimensionDrift);receipt.onlyAllowedFieldsChanged=CourseDocumentDeltaExact(System.Text.Encoding.UTF8.GetString(before),File.ReadAllText(plan.target),plan,out receipt.changedDocuments);receipt.only13DocumentsChanged=receipt.changedDocuments==13;
                if(!receipt.allObjectIdsExact||!receipt.mountsExact||!receipt.worldExact||!receipt.nwhExact||!receipt.onlyAllowedFieldsChanged||!receipt.only13DocumentsChanged||CourseNeutralExternal(match,plan)!=plan.externalConfiguration)throw new InvalidOperationException("Course promotion changed an unrelated identity/serialized field/world/mount/NWH configuration.");
                receipt.sourceClosureExact=plan.protectedSources.All(row=>CourseCertificateSame(Hash(row.path),row.sha256))&&File.ReadAllBytes(plan.target+".meta").SequenceEqual(meta)&&AssetDatabase.AssetPathToGUID(plan.target)==plan.sceneGuid;if(!receipt.sourceClosureExact)throw new InvalidOperationException("Protected source or original metadata changed.");receipt.resultSha256=Hash(plan.target);receipt.status=plan.testOnly?"TEST_ONLY_PHYSICAL_COURSE_PROMOTED_NO_CANONICAL_AUTHORITY":"CANONICAL_PHYSICAL_COURSE_PROMOTED_REQUIRES_WORLD_AND_FLIGHT_ACCEPTANCE";File.WriteAllText(report,JsonUtility.ToJson(receipt,true));complete=true;
            }
            catch(Exception error){failure=error;receipt.failure=error.ToString();}
            finally
            {
                void Cleanup(string stage,Action action){try{action();}catch(Exception error){receipt.failure+="\nCLEANUP "+stage+": "+error;complete=false;if(failure==null)failure=error;}}
                Cleanup("close-owned-scene",()=>{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);});Cleanup("restore-editor-context",()=>{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);});Cleanup("source-closure",()=>{receipt.sourceClosureExact=plan.protectedSources.All(row=>CourseCertificateSame(Hash(row.path),row.sha256))&&File.ReadAllBytes(plan.target+".meta").SequenceEqual(meta);if(!receipt.sourceClosureExact)throw new InvalidOperationException("Unrelated source/meta changed; preserve the external change.");});
                Action restore=()=>{File.WriteAllBytes(plan.target,before);File.WriteAllBytes(plan.target+".meta",meta);AssetDatabase.ImportAsset(plan.target,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);receipt.rollbackExact=File.ReadAllBytes(plan.target).SequenceEqual(before)&&File.ReadAllBytes(plan.target+".meta").SequenceEqual(meta);receipt.resultSha256=Hash(plan.target);if(!receipt.rollbackExact)throw new InvalidOperationException("Owned course scene rollback was not byte exact.");};
                if(!complete){Cleanup("restore-owned-target",restore);receipt.status="REJECTED_PHYSICAL_COURSE_PROMOTION";}Cleanup("write-receipt",()=>File.WriteAllText(report,JsonUtility.ToJson(receipt,true)));if(!complete&&!receipt.rollbackExact)Cleanup("restore-after-receipt-failure",restore);if(!complete)receipt.status="REJECTED_PHYSICAL_COURSE_PROMOTION";
            }
            if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();return JsonUtility.ToJson(receipt,true);
        }
    }
}
#endif
