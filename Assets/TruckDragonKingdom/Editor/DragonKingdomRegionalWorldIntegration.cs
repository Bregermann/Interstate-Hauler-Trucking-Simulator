#if UNITY_EDITOR && VISTA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    // A separate, reviewable composition of the measured vendor foundation.
    // Never replaces the accepted prefab, terrain, roads, scenes or identities.
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        const string Root="Assets/TruckDragonKingdom/";
        const string Preview=Root+"Scenes/DragonKingdomPreview.unity";
        const string Prefab=Root+"Prefabs/Shared Dragon Kingdom.prefab";
        const string Foundation=Root+"ReviewCandidates/VistaRegionalFoundation01";
        [Serializable]sealed class FoundationStatus {public string status="";public bool sourceBytesPreserved=false,requiresDigger=false;public string[] validationIssues=Array.Empty<string>();}
        [Serializable]public sealed class TileRecord
        {public string foundation,candidate;public int resolution,protectedSamples,transitionSamples,changedByVendor,foundationAbove200,integratedAbove200,foundationAbove400,integratedAbove400,undergroundFoundationSamples,roadGroundingSamples,bridgeCappedSamples;public float maximumVendorDelta,integratedMaximumY,maximumTransitionChange;public bool protectedSamplesExact,undergroundFoundationExact;}
        [Serializable]public sealed class Placement
        {public string region,name,operation,source;public Vector3 before,after;public Bounds bounds;public int trial;}
        [Serializable]public sealed class SweepFailure {public string extraction,collider;public Vector3 position;public Bounds bounds;}
        [Serializable]public sealed class Receipt
        {
            public string scope="MEASURED_VISTA_WORLD_INTEGRATION_REVIEW_CANDIDATE_NOT_VISUAL_OR_CONTINUOUS_DRIVING_ACCEPTANCE";
            public string tag,prefab,preview,definition,status="IN_PROGRESS",error;
            public TileRecord[] terrains;public Placement[] placements;public List<SweepFailure> extractionFailures=new List<SweepFailure>();
            public string terrainCoordinationMode;public NativeRoadReliefEvidence nativeRoadRelief;
            public NativeVistaPostGradeEvidence nativeVistaPostGrade;
            public CurrentFinalTerrainRecipeEvidence currentFinalTerrainRecipe;public RejectedWorldDiagnostic rejectedDiagnostic;
            public OwnedMountainRoadEvidence ownedMountainRoad;public bool candidateRoadAuthorityAndCriticalPosesExact;
            public DragonKingdomWorldAuthoring.NativeGradeSelectionEvidence nativeGradeSelection;
            public FinishedArchitectureComposition finishedArchitecture;public NativeSettlementFootingEvidence retainedNativeBuildings;public NativeFinishedIntegrationEvidence nativeArchitectureIntegration;
            public CavernJoinFinishEvidence cavernJoinFinish;
            public DragonKingdomRoadSurfaceProbe.CavernSavedRockMaterialsReceipt cavernMaterials;public DragonKingdomDiggerCavernAuthoring.Evidence cavern;public QuayImportEvidence nativeImportPreflight;public MooringBerth berth;public UrbanBankProof urbanBanks;public AuthoredTerrainPalette palette;public TerrainQuality terrainQuality;public GroundingReview groundingReview;public LandmarkFinish finish;public BearingEvidence bearings;public CoastalBand outerBand;public SettlementDryLand[] settlementDryLand;public DragonKingdomCandidateSurfaceReview.SurfaceBindings surfaceBindings;
            public int extractionSweepQueries;public bool sourceBytesPreserved,roadsAndCriticalPosesExact,originalPhysicalGatesPassed,broadVendorHighlandMassRetained;
            public string[] remainingPresentationWork={"Actual nine-region rendered review required", "Cavern underground excavation and scenery composition remain a separate vendor Digger review", "Mode ambient ground navigation must be explicitly re-grounded and re-baked before any adoption", "Continuous actual NWH driving and complete sport rounds remain unverified"};
        }
        public static Receipt LastReceipt{get;private set;}
        sealed class TileState {public Terrain terrain;public TerrainData original,candidate;public float[,] before,foundation;public byte[,] fieldTags;public string[,] fieldOwners;public int groundConflictCount;public List<GroundConflict> groundConflicts=new List<GroundConflict>();public TileRecord record;}

        [MenuItem("Truck Sports/Dragon Kingdom/Create integrated regional Vista review candidate")]
        public static void CreateReviewCandidate()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonWorldCandidate");
            CreateReviewCandidate(at>=0&&at+1<args.Length?args[at+1]:"VistaIntegrated01");
        }
        public static void CreateReviewCandidate(string tag)
        {
            var iterator=BuildReviewCandidate(tag,Foundation,false);
            while(iterator.MoveNext())throw new InvalidOperationException("Use the explicit rounded IEnumerator authoring fixture for asynchronous vendor excavation.");
        }
        public static IEnumerator CreateRoundedReviewCandidate()
        {
            string tag=Argument("-tsDragonWorldCandidate","RoundedIntegrated01"),foundationTag=Argument("-tsDragonVistaFoundation","RoundedVista01");
            if(string.IsNullOrEmpty(foundationTag)||foundationTag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new ArgumentException("Measured foundation tag required.");
            return BuildReviewCandidate(tag,Root+"ReviewCandidates/"+foundationTag,true);
        }
        static IEnumerator BuildReviewCandidate(string tag,string foundationFolder,bool allowExcavation)
        {
            if(string.IsNullOrEmpty(tag)||tag.Length>40||tag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new ArgumentException("Explicit short candidate tag required.");
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");
            string folder=Root+"VisualCandidates/"+tag;
            if(Directory.Exists(folder))throw new InvalidOperationException("Preserve existing candidate; choose a new tag: "+folder);
            var foundation=JsonUtility.FromJson<FoundationStatus>(File.ReadAllText(foundationFolder+"/Foundation Receipt.json"));
            if(foundation.status!="MEASURED_CANDIDATE_REQUIRES_WORLD_INTEGRATION_AND_VISUAL_REVIEW"||!foundation.sourceBytesPreserved||foundation.validationIssues==null||foundation.validationIssues.Length!=0)
                throw new InvalidOperationException("The actual measured V102 vendor foundation must pass its own gates before world integration.");
            if(foundation.requiresDigger&&!allowExcavation)throw new InvalidOperationException("This measured foundation requires actual Digger excavation. Use CreateRoundedReviewCandidate IEnumerator; do not flatten its roof with old surface grading.");
            for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(s.path==Preview||s.isDirty||string.IsNullOrEmpty(s.path))throw new InvalidOperationException("Save and close user scenes before isolated candidate composition.");}
            if(Terrain.activeTerrains.Length!=0)throw new InvalidOperationException("No unrelated active terrain may participate in EasyRoads grading.");
            string terrainMode=Argument("-tsDragonTerrainCoordination","LegacyContinuousEnvelope");
            if(terrainMode!="LegacyContinuousEnvelope"&&terrainMode!=NativeRoadReliefMode)throw new ArgumentException("Unknown explicit terrain coordination mode: "+terrainMode);
            if(NativeVistaPostGradeRequested()&&terrainMode!=NativeRoadReliefMode)throw new InvalidOperationException("Explicit Vista post-SDK grading requires native terrain coordination; legacy/default remains unchanged.");
            bool finishedArchitecture=NativeFinishedRequested();string architectureWitness=null;
            bool currentFinalRecipe=RequireCurrentFinalTerrainRecipeSelection(Environment.GetCommandLineArgs(),terrainMode,finishedArchitecture,Environment.GetCommandLineArgs().Contains("-tsDragonFinishArt"),NativeVistaPostGradeRequested(),Environment.GetCommandLineArgs().Contains(DragonKingdomVendorFalloffAuthoring.Flag));
            if((finishedArchitecture||NativeCrossingsRequested())&&terrainMode!=NativeRoadReliefMode)throw new InvalidOperationException("Finished architecture/physical crossing variants require the explicit native terrain mode; legacy behavior remains exact.");
            if(finishedArchitecture)
            {
                if(!Environment.GetCommandLineArgs().Contains("-tsDragonFinishArt"))throw new InvalidOperationException("Complete native architecture requires the established finished-art additions.");
                architectureWitness=Argument("-tsDragonArchitectureGeometry",null);
                if(string.IsNullOrEmpty(architectureWitness)||!File.Exists(Io(architectureWitness)))throw new InvalidOperationException("Explicit successful native architecture geometry witness required.");
                architectureWitness=Path.GetFullPath(architectureWitness);
                if(!CanReuseActualArchitectureMaterialPreflight())CaptureArchitectureMaterialPreflight();
            }
            var nativeImportPreflight=Environment.GetCommandLineArgs().Contains("-tsDragonFinishArt")?PrepareValidatedQuayNativeImports():null;
            var protectedFiles=SourceFiles(foundationFolder).Concat(OuterSourceFiles()).Concat(finishedArchitecture?NativeFinishedProtectedSources(architectureWitness):Array.Empty<string>()).Distinct().ToDictionary(p=>p,p=>Hash(File.ReadAllBytes(Io(p))));
            var prior=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;
            var receipt=new Receipt{tag=tag,terrainCoordinationMode=terrainMode,nativeImportPreflight=nativeImportPreflight,prefab=folder+"/Shared Dragon Kingdom.prefab",preview=folder+"/DragonKingdomPreview.unity",definition=folder+"/Dragon Kingdom.asset"};LastReceipt=receipt;
            var placements=new List<Placement>();bool ridgeAttempted=false,authoredGroupsAttempted=false,bankAttempted=false,urbanBankAttempted=false,berthAttempted=false;NativeFinishedContext nativeFinished=null;OwnedMountainContext mountain=null;
            DragonKingdomWorld world=null;Dictionary<Transform,string> critical=null;string graph=null;LastArtistMarketPlacement=null;
            try
            {
                mountain=SelectOwnedMountainFromArguments();receipt.ownedMountainRoad=mountain?.evidence;if(mountain!=null){if(terrainMode!=NativeRoadReliefMode||!NativeCrossingsRequested())throw new InvalidOperationException("Owned mountain integration requires existing native terrain/physical crossing coordination; no legacy exclusion fallback.");receipt.scope+="_INTENTIONAL_OWNED_TARGET_DELTA_NOT_ORIGINAL_REFERENCE_GRAPH_IDENTITY";}
                Directory.CreateDirectory(folder+"/Terrain");Directory.CreateDirectory(folder+"/Materials");AssetDatabase.Refresh();
                scene=EditorSceneManager.OpenScene(Preview,OpenSceneMode.Additive);EditorSceneManager.SetActiveScene(scene);
                world=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();
                critical=CriticalTransforms(world).Distinct().ToDictionary(t=>t,t=>Pose(t));
                mountain?.Apply(world);
                graph=JsonUtility.ToJson(world.definition.graph);
                var tiles=InstallFoundation(world,folder,foundationFolder);
                ReplaceUnsupportedWindmill(world,folder,placements);
                bool finishArt=Environment.GetCommandLineArgs().Contains("-tsDragonFinishArt");
                if(finishArt)receipt.finish=AddOwnedStreetfronts(world,folder,placements);
                SeatBuildingsOnMeasuredFoundation(world,tiles,placements);
                if(finishedArchitecture)nativeFinished=PrepareNativeFinishedIntegration(world,folder,architectureWitness,receipt,mountain?.DefinitionProvenance(world));
                // Vendor owns grading. Broad shoulders coordinate a real
                // regional landscape instead of a 3m cut in a flat -20m bed.
                Bounds? underground=foundation.requiresDigger?DragonKingdomWorldAuthoring.CavernUndergroundBounds():(Bounds?)null;
                bool observeGrade=Environment.GetCommandLineArgs().Contains("-tsDragonObserveVendorGrade"),nativeGrade=terrainMode==NativeRoadReliefMode;
                if(observeGrade||nativeGrade)
                {var gradingProbe=observeGrade?new DragonKingdomVendorGradingObservation(Path.Combine(Output(),"actual-vendor-grading-stages-"+tag+".json")):null;DragonKingdomWorldAuthoring.GradeTerrainWithEasyRoadsObserved(scene,28,110,underground,gradingProbe==null?null:(Action<DragonKingdomGradeObservation>)gradingProbe.Observe,nativeGrade,NativeCrossingsRequested());}
                else DragonKingdomWorldAuthoring.GradeTerrainWithEasyRoads(scene,28,110,underground);
                if(mountain!=null){mountain.evidence.gradeInput=DragonKingdomWorldAuthoring.LastOwnedMountainGradeInput;if(mountain.evidence.gradeInput==null||!mountain.evidence.gradeInput.exact)throw new InvalidOperationException("Selected owned mountain route did not reach the actual native grading input.");}
                if(DragonKingdomWorldAuthoring.NativeHairpinGradePolicyRequested())receipt.nativeGradeSelection=DragonKingdomWorldAuthoring.LastNativeGradeSelectionEvidence;
                if(terrainMode==NativeRoadReliefMode)
                {
                    if(NativeVistaPostGradeRequested())
                    {
                        var priorVista=LastNativeVistaPostGradeEvidence;
                        try{yield return ApplyNativeVistaPostGrade(world,tiles,underground,nativeFinished==null?Array.Empty<NativeArchitectureFooting>():nativeFinished.footings);}
                        finally{if(!ReferenceEquals(priorVista,LastNativeVistaPostGradeEvidence))receipt.nativeVistaPostGrade=LastNativeVistaPostGradeEvidence;}
                    }
                    var priorNativeProof=LastNativeRoadReliefEvidence;
                    try{receipt.nativeRoadRelief=CoordinateNativeRoadRelief(world,tiles,underground,null,nativeFinished==null?Array.Empty<NativeArchitectureFooting>():nativeFinished.footings);}
                    catch{if(!ReferenceEquals(priorNativeProof,LastNativeRoadReliefEvidence))receipt.nativeRoadRelief=LastNativeRoadReliefEvidence;throw;}
                }
                else ProtectAcceptedRoadAndStartSamples(world,tiles,underground);
                urbanBankAttempted=true;receipt.urbanBanks=CreateMeasuredUrbanRetainingBanks(world,tiles);
                if(finishArt){berthAttempted=true;receipt.berth=CreateMeasuredMerchantBerth(world,tiles,receipt.terrainCoordinationMode==NativeRoadReliefMode);}
                receipt.terrains=tiles.Select(t=>t.record).ToArray();
                if(currentFinalRecipe)yield return ApplyCurrentFinalTerrainRecipe(world,tiles,underground,nativeFinished.footings,receipt);
                receipt.terrainQuality=ValidateCoordinatedTerrain(world,tiles,underground);
                foreach(var tile in tiles)tile.terrain.Flush();Physics.SyncTransforms();
                receipt.groundingReview=ClassifyGroundConflicts(world,tiles);
                if(receipt.groundingReview.unsupportedPavement!=0)
                {receipt.bearings=CreateActualBearingFoundations(world,folder,receipt.groundingReview,receipt.terrainCoordinationMode==NativeRoadReliefMode);Physics.SyncTransforms();receipt.groundingReview=ClassifyGroundConflicts(world,tiles);
                    if(receipt.bearings.failures.Count!=0)throw new InvalidOperationException("Actual owned bearing support rejects "+receipt.bearings.failures.Count+" complete patch/footing checks. Full evidence: "+Path.Combine(Output(),"regional-world-"+tag+".json"));}
                if(!receipt.terrainQuality.passed)throw new InvalidOperationException("Actual integrated terrain rejects curtains/seams: steep65="+receipt.terrainQuality.integratedSteep65+"/"+receipt.terrainQuality.allowedSteep65+", steep80="+receipt.terrainQuality.integratedSteep80+"/"+receipt.terrainQuality.allowedSteep80+", conflictingGroundCoreSamples="+receipt.terrainQuality.conflictingGroundCoreSamples+", seam="+receipt.terrainQuality.maximumTileSeamDelta+". Full structured evidence: "+Path.Combine(Output(),"regional-world-"+tag+".json"));
                if(!receipt.groundingReview.passed)throw new InvalidOperationException("Actual ground-pavement classification rejects unsupported physical surfaces: "+receipt.groundingReview.unsupportedPavement+". Full evidence: "+Path.Combine(Output(),"regional-world-"+tag+".json"));
                receipt.broadVendorHighlandMassRetained=receipt.terrains.Sum(t=>t.integratedAbove200)>=receipt.terrains.Sum(t=>t.foundationAbove200)*.65f&&receipt.terrains.Max(t=>t.integratedMaximumY)>=450;
                if(!receipt.broadVendorHighlandMassRetained)throw new InvalidOperationException("Road coordination erased measured broad vendor highlands or all450m peaks.");
                foreach(var tile in tiles)
                {
                    DragonKingdomWorldVisualMigration.RepairTerrainAppearance(tile.candidate);
                    EditorUtility.SetDirty(tile.candidate);tile.terrain.Flush();
                }
                if(finishArt)receipt.palette=InstallAuthoredInnerPalette(world,folder);
                RegroundScenery(world,placements,nativeFinished?.frames);
                if(nativeFinished!=null){receipt.nativeArchitectureIntegration.measuredFramesExcludedFromLooseRegrounding=true;ValidateNativeFinishedFrameAndBuildingPoses(world,nativeFinished,receipt);}
                CorrectRefugeOpening(world,placements);
                foreach(var group in world.Region("forest").root.GetComponentsInChildren<LODGroup>(true))
                {var lods=group.GetLODs();if(lods.Length!=3)continue;lods[lods.Length-1].screenRelativeTransitionHeight=.025f;group.SetLODs(lods);}
                if(foundation.requiresDigger)
                {
                    var excavation=DragonKingdomDiggerCavernAuthoring.ExcavateCandidate(world,tag);
                    while(excavation.MoveNext())yield return excavation.Current;
                    receipt.cavern=DragonKingdomDiggerCavernAuthoring.LastEvidence;
                    if(receipt.cavern==null||receipt.cavern.status!="MEASURED_CAVERN_REQUIRES_FULL_WORLD_GATES_AND_RENDER_REVIEW")throw new InvalidOperationException("Actual vendor cavern did not pass its own measured geometry gates.");
                    receipt.remainingPresentationWork=new[]{"Actual nine-region and cavern interior rendered review required","Mode ambient ground navigation must be explicitly re-grounded and re-baked before adoption","Full actual animated dragon+original mounted truck course sweeps and NWH traversal remain required"};
                }
                if(finishArt)
                {
                    FinishOwnedLandmarks(world,folder,receipt.finish,placements);
                    if(Environment.GetCommandLineArgs().Contains("-tsDragonCavernRasterClearance"))receipt.cavernJoinFinish=InstallMeasuredCavernJoinFinish(world,folder,Argument("-tsDragonCavernOccupancyEvidence",""),Argument("-tsDragonCavernLiveEvidence",""));
                    bool originalFinishComplete=receipt.finish.complete;receipt.finish.complete=false;
                    ridgeAttempted=true;receipt.finish.ridgeComposition=RecomposeExistingRidgeArtWithDefinition(world,folder,placements,false,mountain?.DefinitionProvenance(world));
                    authoredGroupsAttempted=true;
                    receipt.finish.authoredGroups=InstallActualAuthoredGroupsWithDefinition(world,folder,receipt.finish,placements,mountain?.DefinitionProvenance(world));
                    bankAttempted=true;receipt.finish.retainingBank=InstallRetainingBankArt(world,folder,receipt.finish,placements);
                    CaptureExact51AssemblyMaterialProof();receipt.finish.assemblyMaterials=LastAssemblyMaterialEvidence;
                    receipt.finish.sourcePrefabs=receipt.finish.sources.OrderBy(p=>p).ToArray();
                    receipt.finish.complete=originalFinishComplete&&receipt.finish.authoredGroups.Length==5&&receipt.finish.authoredGroups.Sum(g=>g.instances)==335&&receipt.finish.authoredGroups.All(g=>g.passed)&&receipt.finish.ridgeComposition.passed&&receipt.finish.harborForecourt.passed&&receipt.finish.harborCargo.passed&&receipt.finish.retainingBank.passed&&receipt.finish.assemblyMaterials.passed;
                    if(!receipt.finish.complete)throw new InvalidOperationException("Schema3 must retain every required actual source group/support proof; no omitted composition branch.");
                    if(nativeFinished!=null)
                    {
                        CompleteFinishedWaterfrontPhysicalSupport(world,receipt.finishedArchitecture,folder);
                        ValidateNativeFinishedFrameAndBuildingPoses(world,nativeFinished,receipt);
                        receipt.nativeArchitectureIntegration.status="ACTUAL_NATIVE_ARCHITECTURE_WATERFRONT_AND_RETAINED_CONTACTS_REQUIRE_SAVED_WORLD_AND_RENDER_ACCEPTANCE";
                    }
                    foreach(var group in world.Region("forest").root.GetComponentsInChildren<LODGroup>(true).Concat(world.Region("nests").root.GetComponentsInChildren<LODGroup>(true)))
                    {var lods=group.GetLODs();if(lods.Length!=3)continue;lods[2].screenRelativeTransitionHeight=.025f;group.SetLODs(lods);}
                }
                var surfaces=DragonKingdomCandidateSurfaceReview.InstallVendorSurfaces(world,folder);
                receipt.surfaceBindings=surfaces.evidence;
                AssetDatabase.SaveAssetIfDirty(surfaces.road);AssetDatabase.SaveAssetIfDirty(surfaces.terrain);
                receipt.outerBand=AttachMeasuredCoast(world,folder,currentFinalRecipe);
                if(receipt.palette!=null){ApplyAuthoredPaletteWithOwnedGeometry(world,folder,receipt.palette,true,currentFinalRecipe&&receipt.outerBand!=null?new Action<Terrain[]>(_=>CompleteCurrentCoreCoastCopies(world,folder,receipt.outerBand)):null);FinalizeAuthoredPalette(world,receipt.palette);}
                if(nativeFinished!=null)
                {
                    ValidateNativeFinishedFrameAndBuildingPoses(world,nativeFinished,receipt);CompleteNativeFinishedTerrainContacts(world,folder,receipt);
                    ValidateFinishedArchitectureSavePrerequisites(receipt.finishedArchitecture,receipt.retainedNativeBuildings,receipt.nativeRoadRelief);
                    receipt.finish.compositionSchema=4;
                }
                receipt.settlementDryLand=MeasureCoherentSettlementDryLand(world,receipt.outerBand?.seaLevel??-2);
                foreach(var nest in world.regions.SelectMany(r=>r.nests))nest.position=DragonKingdomNestAnchorMigration.MeasuredNestSeat(nest.parent);
                foreach(var p in critical)if(p.Key==null||Pose(p.Key)!=p.Value)throw new InvalidOperationException("Candidate moved an accepted road/start/extraction/aerial pose: "+(p.Key==null?"missing":p.Key.name));
                if(JsonUtility.ToJson(world.definition.graph)!=graph)throw new InvalidOperationException("Candidate altered the shared road graph.");
                mountain?.Validate(world,false);receipt.roadsAndCriticalPosesExact=mountain==null;receipt.candidateRoadAuthorityAndCriticalPosesExact=true;receipt.placements=placements.ToArray();
                var definition=mountain?.Definition??UnityEngine.Object.Instantiate(world.definition);definition.name="Dragon Kingdom regional Vista review "+tag;
                foreach(var region in world.regions)
                {
                    var bounds=BoundsOf(region.root);bounds.Encapsulate(definition.FindRegion(region.id).center);
                    foreach(var start in region.truckStarts)bounds.Encapsulate(start.position);
                    definition.FindRegion(region.id).bounds=bounds;
                }
                if(nativeFinished!=null)ExpandNativeFinishedCastleDefinition(definition,receipt);
                AssetDatabase.CreateAsset(definition,receipt.definition);world.definition=definition;
                Physics.SyncTransforms();
                // Save diagnostic candidate even if a strict gate rejects it.
                // Status and receipt never call failed output accepted.
                if(Environment.GetCommandLineArgs().Contains("-tsDragonCavernOwnedRockMaterials"))receipt.cavernMaterials=DragonKingdomRoadSurfaceProbe.ApplyOwnedCavernRockMaterials(world,folder);
                PrefabUtility.SaveAsPrefabAsset(world.gameObject,receipt.prefab);
                if(receipt.cavernMaterials!=null)
                {
                    var savedWorld=AssetDatabase.LoadAssetAtPath<GameObject>(receipt.prefab)?.GetComponent<DragonKingdomWorld>();
                    DragonKingdomRoadSurfaceProbe.ValidateSavedOwnedCavernRockMaterials(savedWorld,receipt.cavernMaterials);
                }
                if(!EditorSceneManager.SaveScene(scene,receipt.preview))throw new InvalidOperationException("Candidate preview failed to save.");
                mountain?.Validate(world,true);
                foreach(var tile in tiles)AssetDatabase.SaveAssetIfDirty(tile.candidate);AssetDatabase.SaveAssetIfDirty(definition);
                DragonKingdomWorldAuthoring.ValidatePhysicalRoads(world.transform,world.definition.graph);
                DragonKingdomWorldAuthoring.ValidateTruckStarts(world,false);world.ValidateReferences();
                ValidateContinuousExtractionApproaches(world,receipt);
                receipt.originalPhysicalGatesPassed=true;receipt.status="PHYSICAL_GATES_PASSED_REQUIRES_RENDER_AND_ACTUAL_DRIVING_REVIEW";
            }
            finally
            {
                if(receipt.status=="IN_PROGRESS"){receipt.status="REJECTED_CANDIDATE";receipt.error="Authoring failed; inspect the native exception and bounded road/Digger receipts. No failed candidate is accepted.";}
                if(foundation.requiresDigger)receipt.cavern=DragonKingdomDiggerCavernAuthoring.LastEvidence;
                if(ridgeAttempted&&receipt.finish!=null&&receipt.finish.ridgeComposition==null&&LastRidgeComposition!=null)receipt.finish.ridgeComposition=LastRidgeComposition;
                if(authoredGroupsAttempted&&receipt.finish!=null&&receipt.finish.authoredGroups==null&&LastAuthoredGroups!=null)receipt.finish.authoredGroups=LastAuthoredGroups;
                if(bankAttempted&&receipt.finish!=null&&LastRetainingBankArt!=null)receipt.finish.retainingBank=LastRetainingBankArt;
                if(urbanBankAttempted&&receipt.urbanBanks==null&&LastMeasuredUrbanBank!=null)receipt.urbanBanks=LastMeasuredUrbanBank;
                if(berthAttempted&&receipt.berth==null&&LastMeasuredMerchantBerth!=null)receipt.berth=LastMeasuredMerchantBerth;
                if(LastBearingEvidence!=null&&LastBearingEvidence.tag==tag)receipt.bearings=LastBearingEvidence;
                receipt.placements=placements.ToArray();
                if(receipt.finish!=null&&receipt.finish.artistMarket==null&&LastArtistMarketPlacement!=null)receipt.finish.artistMarket=LastArtistMarketPlacement;
                if(receipt.status=="REJECTED_CANDIDATE"&&RejectedDiagnosticRequested(Environment.GetCommandLineArgs()))
                {
                    try{receipt.rejectedDiagnostic=CaptureRejectedCurrentWorld(world,folder,receipt,protectedFiles,critical,graph);}
                    catch(Exception diagnosticError){receipt.rejectedDiagnostic=new RejectedWorldDiagnostic{tag=tag,status="REJECTED_DIAGNOSTIC_CAPTURE_FAILED",error=diagnosticError.ToString(),authorizesAdoption=false};}
                }
                mountain?.Dispose();
                receipt.sourceBytesPreserved=protectedFiles.All(p=>File.Exists(Io(p.Key))&&Hash(File.ReadAllBytes(Io(p.Key)))==p.Value);
                CompleteRejectedDiagnosticSourceAudit(receipt);
                string output=Output();Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"regional-world-"+tag+".json"),JsonUtility.ToJson(receipt,true));
                if(Directory.Exists(folder))File.WriteAllText(folder+"/Integration Receipt.json",JsonUtility.ToJson(receipt,true));
                // A timed-out iterator must not destroy terrain while Digger's
                // real asynchronous mesh/voxel work still owns it. Preserve
                // that isolated scene for diagnosis until the jobs complete.
                if(DragonKingdomDiggerCavernAuthoring.IsBusy)Debug.LogError("Preserving rejected isolated candidate scene: actual Digger jobs are still pending. Do not close it or start another authoring run.");
                else{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);mountain?.DestroyUnsavedDefinition();EditorSceneManager.RestoreSceneManagerSetup(prior);}
                if(!receipt.sourceBytesPreserved)throw new InvalidOperationException("Candidate authoring modified a protected original/foundation source asset.");
            }
        }

        static TileState[] InstallFoundation(DragonKingdomWorld world,string folder,string foundationFolder)
        {
            var terrains=world.GetComponentsInChildren<Terrain>(true);if(terrains.Length!=4)throw new InvalidOperationException("Actual four-tile saved world required.");
            return terrains.Select(t=>
            {
                int x=Mathf.RoundToInt((t.transform.position.x+1600)/1600),z=Mathf.RoundToInt((t.transform.position.z+800)/1600);
                string source=foundationFolder+"/Terrain/Kingdom "+x+" "+z+".asset";var data=AssetDatabase.LoadAssetAtPath<TerrainData>(source);
                if(data==null||data.heightmapResolution!=513||data.size!=new Vector3(1600,600,1600)||t.transform.position!=new Vector3(x*1600-1600,-20,z*1600-800))throw new InvalidOperationException("Measured Vista tile contract changed: "+source);
                var state=new TileState{terrain=t,original=t.terrainData,foundation=data.GetHeights(0,0,513,513),before=t.terrainData.GetHeights(0,0,513,513),candidate=UnityEngine.Object.Instantiate(data),record=new TileRecord{foundation=source,candidate=folder+"/Terrain/Kingdom "+x+" "+z+".asset",resolution=513}};
                AssetDatabase.CreateAsset(state.candidate,state.record.candidate);t.terrainData=state.candidate;
                var collider=t.GetComponent<TerrainCollider>();if(collider==null)throw new InvalidOperationException("Actual terrain collision required.");collider.terrainData=state.candidate;
                return state;
            }).ToArray();
        }
        static void ProtectAcceptedRoadAndStartSamples(DragonKingdomWorld world,TileState[] tiles,Bounds? underground)
            =>CoordinateVendorFields(world,tiles,underground);
        static float OriginalHeight(TileState[] tiles,Vector3 p)
        {
            foreach(var tile in tiles){var d=p-tile.terrain.transform.position;if(d.x>=0&&d.z>=0&&d.x<=1600&&d.z<=1600)return tile.terrain.transform.position.y+tile.original.GetInterpolatedHeight(d.x/1600,d.z/1600);}
            throw new InvalidOperationException("Protected transition boundary lies outside actual original terrain: "+p);
        }
        static void ReplaceUnsupportedWindmill(DragonKingdomWorld world,string folder,List<Placement> records)
        {
            var root=world.Region("village").root;var sails=root.Cast<Transform>().Where(t=>t.name.StartsWith("P_BLD_windmill_sail",StringComparison.Ordinal)).ToArray();
            if(sails.Length!=1)throw new InvalidOperationException("Expected the single audited unsupported standalone sail.");
            Vector3 before=sails[0].position;UnityEngine.Object.DestroyImmediate(sails[0].gameObject);
            string source=DragonKingdomWorldAuthoring.Village+"buildings/P_BLD_house_14.prefab";var asset=AssetDatabase.LoadAssetAtPath<GameObject>(source);
            if(asset==null)throw new InvalidOperationException("Complete actual vendor windmill building is required.");
            var go=(GameObject)PrefabUtility.InstantiatePrefab(asset,root);go.name="Complete owned windmill house14";var t=go.transform;t.localPosition=Vector3.zero;t.localRotation=Quaternion.identity;
            t.localScale*=30/BoundsOf(t).size.y;AlignBase(t,root.position+new Vector3(-120,0,100));
            var sail=go.GetComponentsInChildren<Transform>(true).Single(s=>s.name=="P_BLD_windmill_sail");
            // The original clip has empty-path position curves near zero.
            // Bind the zero-local model child, retaining the prefab's actual
            // attachment offset and yaw on its unanimated parent transform.
            var rotatingModel=sail.Cast<Transform>().Single();
            var sailAnimator=rotatingModel.GetComponent<Animator>();if(sailAnimator==null)sailAnimator=rotatingModel.gameObject.AddComponent<Animator>();
            sailAnimator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(DragonKingdomWorldAuthoring.Art+"Tidal Flask Studios/Fantastic Village Pack/animation/CTRL_BLD_windmill_sail.controller");
            if(sailAnimator.runtimeAnimatorController==null)throw new InvalidOperationException("The actual owned windmill rotation controller is required.");
            var secondary=new HashSet<GameObject>();foreach(var group in go.GetComponentsInChildren<LODGroup>(true)){var lods=group.GetLODs();for(int i=1;i<lods.Length;i++)foreach(var r in lods[i].renderers)if(r!=null)secondary.Add(r.gameObject);}
            foreach(var mesh in go.GetComponentsInChildren<MeshFilter>(true))if(mesh.sharedMesh!=null&&!secondary.Contains(mesh.gameObject)&&!mesh.transform.IsChildOf(rotatingModel)&&mesh.GetComponent<Collider>()==null)mesh.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh.sharedMesh;
            foreach(var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var original=materials[i];if(original==null)throw new InvalidOperationException("Windmill source has missing material.");
                    // Actual vendor URP materials already own their authored
                    // texture/color/emission semantics, including deliberate
                    // untextured M_CLR_yellow_E window emission (native V117).
                    if(DragonKingdomWorldVisualMigration.HasSupportedUrpShader(original)){materials[i]=original;continue;}
                    string guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original)),path=folder+"/Materials/"+guid+" URP.mat";
                    materials[i]=DragonKingdomWorldVisualMigration.HasAuthoredAlbedo(original)
                        ?DragonKingdomWorldVisualMigration.ConfigureOwnedMaterial(original,path,false,out _)
                        :PreserveLegacySolidColor(original,path);
                }
                renderer.sharedMaterials=materials;
            }
            var animator=go.GetComponentsInChildren<Animator>(true).FirstOrDefault(a=>a.runtimeAnimatorController!=null);
            if(animator==null)throw new InvalidOperationException("Complete windmill lost its real nested animation controller.");
            records.Add(new Placement{region="village",name=go.name,operation="Replace unsupported sail with complete same-vendor animated building",before=before,after=t.position,bounds=BoundsOf(t)});
        }
        static Material PreserveLegacySolidColor(Material source,string path)
        {
            var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing!=null)return existing;
            if(File.Exists(path))throw new InvalidOperationException("Preserve differing existing candidate material: "+path);
            if(!source.HasProperty("_Color")&&!source.HasProperty("_BaseColor"))throw new InvalidOperationException("Unrecognized intentional solid-color source requires an actual property audit: "+AssetDatabase.GetAssetPath(source));
            var result=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=source.name+" authored solid color URP",renderQueue=source.renderQueue};
            result.SetColor("_BaseColor",source.HasProperty("_BaseColor")?source.GetColor("_BaseColor"):source.GetColor("_Color"));
            foreach(string property in new[]{"_Metallic","_Smoothness","_Cull"})if(source.HasProperty(property)&&result.HasProperty(property))result.SetFloat(property,source.GetFloat(property));
            if(source.HasProperty("_EmissionColor"))result.SetColor("_EmissionColor",source.GetColor("_EmissionColor"));
            if(source.IsKeywordEnabled("_EMISSION"))result.EnableKeyword("_EMISSION");else result.DisableKeyword("_EMISSION");
            result.globalIlluminationFlags=source.globalIlluminationFlags;result.doubleSidedGI=source.doubleSidedGI;
            AssetDatabase.CreateAsset(result,path);return result;
        }
        static void RegroundScenery(DragonKingdomWorld world,List<Placement> records,ISet<Transform> preservedMeasuredFrames=null)
        {
            var terrainSamples=world.GetComponentsInChildren<Terrain>(true);
            foreach(var region in world.regions)
            foreach(var t in region.root.Cast<Transform>().ToArray())
            {
                if(preservedMeasuredFrames!=null&&preservedMeasuredFrames.Contains(t))continue;
                if(t.GetComponentInChildren<Renderer>(true)==null)continue;
                // These are coordinated structural assemblies, not loose props.
                // Their fixed-height floors/aprons remain authoritative.
                if(t.name.Contains("sanctuary")||t.name.Contains("barge")||t.name.Contains("ramp")||t.name.Contains("ledge")||t.name.Contains("Rockwall_Archway")||t.name.Contains("Iron_Gate")||region.id=="caverns"||region.id=="harbor"&&t.name.Contains("Dock"))continue;
                Vector3 before=t.position;bool peak=region.id=="mountains"||region.id=="volcano";bool tree=t.name.StartsWith("P_ENV_tree",StringComparison.Ordinal);
                bool geology=peak||region.id=="refuge"&&t.name.StartsWith("RockFormation",StringComparison.Ordinal);
                if(geology)
                {
                    // Undo only the rejected anisotropic cosmetic stretching.
                    // The actual complete mesh and its accurate collider stay.
                    float scale=Mathf.Min(Mathf.Abs(t.localScale.x),Mathf.Abs(t.localScale.z));t.localScale=Vector3.one*scale;
                    var b=BoundsOf(t);t.localScale*=Mathf.Min(1,Mathf.Min(55/b.size.y,80/Mathf.Max(b.size.x,b.size.z)));
                }
                if(t.name.StartsWith("P_BLD_house",StringComparison.Ordinal)||t.name=="Complete owned windmill house14")continue;
                bool placed=false;var original=BoundsOf(t).center;int supportRejects=0,clearanceRejects=0;float lastRange=0;Vector3 lastCandidate=original;
                for(int trial=0;trial<(geology?192:96);trial++)
                {
                    float radius=trial==0?0:(geology?28:16)*Mathf.Sqrt(trial),angle=trial*2.399963f;
                    var p=original+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);var b=BoundsOf(t);
                    float half=tree?2:Mathf.Clamp(Mathf.Min(b.size.x,b.size.z)*(geology?.4f:.3f),2,geology?30:18);
                    lastCandidate=p;
                    if(!TryGround(world,p,half,out float y,out float range)||range>(geology?b.size.y*1.5f:tree?8:Mathf.Max(4,b.size.y*.35f))){supportRejects++;lastRange=range;continue;}
                    // Geology intersects and emerges from a real slope. Placing
                    // every rock above its highest footprint corner creates
                    // floating foundations; demanding prop-flat ground rejects
                    // the very cliffs these complete assets are meant to dress.
                    p.y=geology?y-range-b.size.y*.15f:y-.08f;AlignBase(t,p);Physics.SyncTransforms();
                    if(!ClearRoadEnvelope(world,t,tree)){clearanceRejects++;continue;}
                    if(region.id=="village"&&t.name.StartsWith("P_PROP_cart_01",StringComparison.Ordinal)&&!SettlementArtHasActualDryFooting(terrainSamples,region.id,t)){supportRejects++;continue;}
                    records.Add(new Placement{region=region.id,name=t.name,operation=geology?"Naturally proportioned complete geology embedded into measured Vista slope with actual collider clearance":"Ground existing complete asset on measured regional terrain",before=before,after=t.position,bounds=BoundsOf(t),trial=trial});placed=true;break;
                }
                if(!placed)throw new InvalidOperationException("No supported road-clear bounded placement for actual "+region.id+"/"+t.name+" originalCenter="+original+" finalCandidate="+lastCandidate+" actualBounds="+BoundsOf(t)+" supportRejects="+supportRejects+" colliderClearanceRejects="+clearanceRejects+" lastFootprintHeightRange="+lastRange);
            }
        }
        static void CorrectRefugeOpening(DragonKingdomWorld world,List<Placement> records)
        {
            var root=world.Region("refuge").root;var arch=root.Cast<Transform>().Single(t=>t.name.StartsWith("SM_Bld_Rockwall_Archway_01",StringComparison.Ordinal));
            Vector3 before=arch.position;arch.localRotation=Quaternion.Euler(0,90,0);arch.localScale=Vector3.one;
            var b=BoundsOf(arch);arch.localScale*=55/b.size.y;b=BoundsOf(arch);
            // yaw90 means source Z is world X and source X is world Z.
            arch.localScale=Vector3.Scale(arch.localScale,new Vector3(32/b.size.z,1,96/b.size.x));AlignBase(arch,root.position+new Vector3(0,0,65));
            records.Add(new Placement{region="refuge",name=arch.name,operation="Orient actual arch opening along the northbound extraction road before fitting96x32m footprint",before=before,after=arch.position,bounds=BoundsOf(arch)});
        }
        public static void ValidateContinuousExtractionApproaches(DragonKingdomWorld world,Receipt receipt)
        {
            var routes=new Dictionary<string,Vector3[]>{
                {"fortress",new[]{new Vector3(0,60,360),new Vector3(0,60,620)}},
                {"mountain",new[]{new Vector3(-360,330,1200),new Vector3(-360,330,1300)}},
                {"boat",new[]{new Vector3(720,0,-604),new Vector3(720,0,-685)}}};
            foreach(var route in routes)
            {
                Vector3 a=route.Value[0],b=route.Value[1],forward=(b-a).normalized;int count=Mathf.CeilToInt(Vector3.Distance(a,b));
                for(int i=0;i<=count;i++)
                {
                    Vector3 p=Vector3.Lerp(a,b,i/(float)count);receipt.extractionSweepQueries++;
                    foreach(var c in Physics.OverlapBox(p+Vector3.up*3.3f,new Vector3(2.2f,3,8),Quaternion.LookRotation(forward),~0,QueryTriggerInteraction.Ignore))
                    {
                        if(c.gameObject.scene!=world.gameObject.scene||!c.transform.IsChildOf(world.transform)||c.bounds.max.y<=p.y+.25f)continue;
                        string path=PathOf(c.transform,world.transform);
                        if(receipt.extractionFailures.Any(f=>f.extraction==route.Key&&f.collider==path))continue;
                        receipt.extractionFailures.Add(new SweepFailure{extraction=route.Key,collider=path,position=p,bounds=c.bounds});
                    }
                }
            }
            if(receipt.extractionFailures.Count!=0)throw new InvalidOperationException("Continuous full4.4x16m extraction body sweep failed; preserve actual collider and repair geometry: "+JsonUtility.ToJson(receipt));
        }
        static bool ClearRoadEnvelope(DragonKingdomWorld world,Transform t,bool tree,Action<AuthoredClearanceWitness> blocked=null)
        {
            var owned=new HashSet<Collider>(t.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&!c.isTrigger));
            if(owned.Count==0)throw new InvalidOperationException("Existing scenery lost its intentional collision representation: "+t.name);
            var bounds=owned.First().bounds;foreach(var c in owned.Skip(1))bounds.Encapsulate(c.bounds);bounds.Expand(8);
            foreach(var edge in world.definition.graph.edges)foreach(var sample in edge.samples)
            {
                if(sample.position.y+8<bounds.min.y||sample.position.y>bounds.max.y)continue;
                var p=sample.position;float margin=sample.roadWidthMeters*.5f+9;
                if(p.x<bounds.min.x-margin||p.x>bounds.max.x+margin||p.z<bounds.min.z-margin||p.z>bounds.max.z+margin)continue;
                var right=Vector3.Cross(Vector3.up,sample.forward).normalized;
                foreach(float lateral in new[]{-.35f,0,.35f})
                {
                    Vector3 contact=p+right*sample.roadWidthMeters*lateral;
                    var body=Physics.OverlapBox(contact+Vector3.up*3.3f,new Vector3(2.2f,3,8),Quaternion.LookRotation(sample.forward),~0,QueryTriggerInteraction.Ignore).FirstOrDefault(owned.Contains);if(body!=null){blocked?.Invoke(new AuthoredClearanceWitness{kind="NATIVE_ROAD_TRUCK_BODY_COLLIDER",path=PathOf(body.transform,world.transform),road=edge.edgeId,point=contact+Vector3.up*3.3f,bounds=body.bounds,queryExtents=new Vector3(2.2f,3,8),queryRotation=Quaternion.LookRotation(sample.forward)});return false;}
                    var surface=Physics.RaycastAll(contact+Vector3.up*8,Vector3.down,16,~0,QueryTriggerInteraction.Ignore).FirstOrDefault(h=>owned.Contains(h.collider)&&h.point.y>contact.y+.3f);if(surface.collider!=null){blocked?.Invoke(new AuthoredClearanceWitness{kind="NATIVE_ROAD_WHEEL_RAY_COLLIDER",path=PathOf(surface.collider.transform,world.transform),road=edge.edgeId,point=surface.point,bounds=surface.collider.bounds});return false;}
                }
            }
            foreach(var region in world.regions)foreach(var start in region.truckStarts)
                {var collider=Physics.OverlapBox(start.position+Vector3.up*2,new Vector3(2.2f,3,8),start.rotation,~0,QueryTriggerInteraction.Ignore).FirstOrDefault(owned.Contains);if(collider!=null){blocked?.Invoke(new AuthoredClearanceWitness{kind="NATIVE_ORIGINAL_START_BODY_COLLIDER",path=PathOf(collider.transform,world.transform),road=PathOf(start,world.transform),point=start.position+Vector3.up*2,bounds=collider.bounds,queryExtents=new Vector3(2.2f,3,8),queryRotation=start.rotation});return false;}}
            return true;
        }
        static bool TryGround(DragonKingdomWorld world,Vector3 p,float half,out float maximum,out float range)
        {
            maximum=float.NegativeInfinity;float minimum=float.PositiveInfinity;
            foreach(var offset in new[]{Vector3.zero,new Vector3(-half,0,-half),new Vector3(half,0,-half),new Vector3(-half,0,half),new Vector3(half,0,half)})
            {try{float y=DragonKingdomTerrainShoulderAuthoring.TerrainHeight(world,p+offset);maximum=Mathf.Max(maximum,y);minimum=Mathf.Min(minimum,y);}catch(InvalidOperationException){range=0;return false;}}
            range=maximum-minimum;return true;
        }
        static IEnumerable<Transform> CriticalTransforms(DragonKingdomWorld world)
        {
            foreach(var t in world.GetComponentsInChildren<Transform>(true))if(!world.regions.Any(r=>t.IsChildOf(r.root)))yield return t;
            foreach(var region in world.regions){yield return region.root;foreach(var start in region.truckStarts)yield return start;}
            foreach(var site in world.extractionSites){yield return site.secureZone.transform;yield return site.approach;if(site.vessel!=null)foreach(var t in site.vessel.GetComponentsInChildren<Transform>(true))yield return t;}
            foreach(var t in world.aerialCheckpoints.Concat(world.riderStarts))yield return t;
        }
        static IEnumerable<string> SourceFiles(string foundationFolder)
        {
            yield return Prefab;yield return Preview;yield return Root+"Configuration/Dragon Kingdom.asset";
            if(Environment.GetCommandLineArgs().Contains("-tsDragonFinishArt")){yield return ArtistMarketLayout;yield return ArtistMarketLayout+".meta";yield return ArtistImportProvenance;yield return ArtistImportProvenance+".meta";var imports=ReadMaterialProvenance();foreach(var binding in imports.bindings){yield return binding.snapshot;yield return binding.snapshot+".meta";}var artist=ReadArtistLayout();foreach(var dependency in artist.dependencies){yield return dependency.path;yield return dependency.path+".meta";}}
            if(Environment.GetCommandLineArgs().Contains("-tsDragonFinishArt"))
            {
                yield return AssemblyImportProvenance;yield return AssemblyImportProvenance+".meta";
                foreach(string layoutPath in new[]{GardenAssembly,NaturalAssembly}){yield return layoutPath;yield return layoutPath+".meta";var layout=ReadAssemblySource(layoutPath);foreach(var dependency in layout.dependencies){yield return dependency.path;yield return dependency.path+".meta";}foreach(var snapshot in layout.originalMaterials){yield return snapshot.path;yield return snapshot.path+".meta";}}
                foreach(var path in AuthoredPaletteSources().SelectMany(p=>AssetDatabase.GetDependencies(p,true)).Distinct()){yield return path;yield return path+".meta";}
                foreach(var path in QuayNativeImportSourcePaths().SelectMany(p=>AssetDatabase.GetDependencies(p,true).Concat(new[]{p})).Distinct().Where(p=>File.Exists(Io(p))&&File.Exists(Io(p+".meta")))){yield return path;yield return path+".meta";}
            }
            yield return DragonKingdomCandidateSurfaceReview.PackedRoadMap;yield return DragonKingdomCandidateSurfaceReview.PackedRoadMap+".meta";
            yield return "ProjectSettings/GraphicsSettings.asset";yield return "ProjectSettings/QualitySettings.asset";
            foreach(string folder in new[]{Root+"Terrain",Root+"FunctionalShoulders",Root+"Models",Root+"Materials",foundationFolder,Root+"PrivateLicensedEnvironment/DerivedVista/"+Path.GetFileName(foundationFolder),"Assets/Digger/Shaders"})
                if(Directory.Exists(folder))foreach(var p in Directory.GetFiles(folder,"*",SearchOption.AllDirectories))yield return p;
            foreach(string folder in new[]{"Assets/TruckHoardHeist/Scenes","Assets/TruckDragonEggDelivery/Scenes","Assets/TruckDragonEscape/Scenes","Assets/TruckDragonRiders/Scenes"})
                if(Directory.Exists(folder))foreach(var p in Directory.GetFiles(folder,"*.unity"))yield return p;
        }
        static bool InsideXZ(Bounds b,Vector3 p)=>p.x>=b.min.x&&p.x<=b.max.x&&p.z>=b.min.z&&p.z<=b.max.z;
        static float DistanceXZ(Vector3 p,Vector3 a,Vector3 b){p.y=a.y=b.y=0;var d=b-a;float t=d.sqrMagnitude>.0001f?Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude):0;return Vector3.Distance(p,a+d*t);}
        static long Key(int x,int z)=>((long)x<<32)^(uint)z;
        static Bounds BoundsOf(Transform root)=>DragonKingdomWorldAuthoring.MeasureOwnedModelBounds(root);
        static void AlignBase(Transform t,Vector3 p){var b=BoundsOf(t);t.position+=new Vector3(p.x-b.center.x,p.y-b.min.y,p.z-b.center.z);}
        static string Pose(Transform t)=>t.localPosition.ToString("R")+t.localRotation.ToString("R")+t.localScale.ToString("R");
        static string PathOf(Transform t,Transform root)=>t==root?t.name:PathOf(t.parent,root)+"/"+t.name;
        static string Hash(byte[] data){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(data));}
        static string Io(string path){string full=Path.GetFullPath(path);return Application.platform==RuntimePlatform.WindowsEditor&&full.Length>=248&&!full.StartsWith(@"\\?\",StringComparison.Ordinal)?@"\\?\"+full:full;}
        static string Argument(string flag,string fallback){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,flag);return at>=0&&at+1<args.Length?args[at+1]:fallback;}
        static string Output(){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");return at>=0&&at+1<args.Length?Path.GetFullPath(args[at+1]):"Logs/TruckSports/DragonKingdom/RegionalIntegration";}
    }
}
#endif
