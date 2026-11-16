#if UNITY_EDITOR && VISTA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        public const string CurrentFinalTerrainRecipeFlag="-tsDragonFinalTerrainRecipe",CurrentFinalTerrainRecipeProfile="CurrentFinalThermalAndFeedback1000v1";
        const string CurrentFinalTerrainInputRole="ACTUAL_CURRENT_WORLD_FINAL_NATIVE_COORDINATOR_URBAN_BANKS_MERCHANT_BERTH_NOT_RAW_SDK";
        const string CurrentFinalRoofProfileFlag="-tsDragonFinalOriginalRoof",CurrentFinalRoofReceiptFlag="-tsDragonFinalOriginalRoofReceipt",CurrentFinalRoofTestsFlag="-tsDragonFinalOriginalRoofTests";
        const string CurrentFinalRoofProtectedInputRole="ACTUAL_CURRENT_WORLD_FINAL_THEN_QUALIFIED_ORIGINAL_ROOF_SOURCE_PROTECTION_NOT_RAW_SDK";
        static QualifiedCavernRoadAuthority currentFinalOriginalRoofAuthority;
        [Serializable]public sealed class CurrentFinalRoofSelection{public string profile,receipt,tests;}
        [Serializable]public sealed class CurrentFinalRoofChangedCell
        {public string terrain;public Vector3 point;public float beforeNormalized,foundationNormalized;public string[] rawOwners,undergroundColliderOwners,terrainConflictOwners;}
        [Serializable]public sealed class CurrentFinalRoofField
        {public string terrain,beforeRoofSha256,afterRoofSha256,foundationSha256;public OriginalRoofCopyEvidence copy;public int authorizedChangedSamples,otherProtectedCoreChanges;public bool actualNativeReadbackExact,holesLayersAlphaExact;}
        [Serializable]public sealed class CurrentFinalRoofCheckpoint
        {
            public string role=CurrentFinalRoofProtectedInputRole,profile,graphSha256,beforeRoofCheckpoint,beforeRoofCheckpointSha256,qualificationReceipt,qualificationReceiptSha256,qualificationTests,qualificationTestsSha256;
            public CurrentFinalRoofField[] fields;public CurrentFinalRoofChangedCell[] everyChangedCell;public RetainedThermalPhaseChain[] unchangedActualSdkCoordinatorUrbanBerthPhases;
        }
        static CurrentFinalRoofSelection RequireCurrentFinalOriginalRoofSelection(string[] args)
        {
            string[] flags={CurrentFinalRoofProfileFlag,CurrentFinalRoofReceiptFlag,CurrentFinalRoofTestsFlag};if(!flags.Any(args.Contains))return null;
            string Value(string flag){int at=Array.IndexOf(args,flag);if(args.Count(a=>a==flag)!=1||at<0||at+1>=args.Length||string.IsNullOrEmpty(args[at+1])||args[at+1].StartsWith("-",StringComparison.Ordinal))throw new InvalidOperationException("Explicit complete unique current-final original-roof profile/receipt/tests required.");return args[at+1];}
            var selection=new CurrentFinalRoofSelection{profile=Value(CurrentFinalRoofProfileFlag),receipt=Value(CurrentFinalRoofReceiptFlag),tests=Value(CurrentFinalRoofTestsFlag)};
            if(selection.profile!=CavernRoadAuthorityProfile||!Path.IsPathRooted(selection.receipt)||!Path.IsPathRooted(selection.tests))throw new InvalidOperationException("Explicit CavernRoadColliderRoof01 and absolute actually-passed native receipt/tests paths required.");return selection;
        }
        [Serializable]public sealed class CurrentFinalTerrainCheckpoint
        {
            public string role=CurrentFinalTerrainInputRole,tag,graphSha256;
            public RetainedThermalPhaseChain[] phases;public RetainedThermalSourceFile[] protectedSources;
        }
        [Serializable]public sealed class CurrentFinalTerrainField
        {
            public string terrain,finalCompositionSha256,authorizedPostRoofBaselineSha256,thermalSha256,actualFeedbackSha256;public int protectedChangesRelativeToBeforeRoof;
            public int protectedSamples,protectedChanged,originalRoofMismatchesBefore,originalRoofMismatchesAfter;
            public bool layersAlphaHolesExact;
        }
        [Serializable]public sealed class CurrentFinalTerrainRecipeEvidence
        {
            public string scope="CURRENT_WORLD_VENDOR_RECIPE_ONLY_ORIGINAL_ROOF_DIGGER_AND_FULL_WORLD_GATES_REMAIN_REQUIRED",profile=CurrentFinalTerrainRecipeProfile,status="IN_PROGRESS",checkpoint,checkpointSha256,maskPath,maskSha256;
            public string observedInputRole=CurrentFinalTerrainInputRole,originalRoofProfile,roofCheckpoint,roofCheckpointSha256,roofAuthorityError;public CurrentFinalRoofField[] roofFields;public CurrentFinalRoofChangedCell[] everyAuthorizedRoofChange;public bool qualifiedOriginalRoofApplied,authorizedRoofProtectedChangeAccountingExact,roofAuthoritySourcesExact,roofAuthorityContextRestored;public int authorizedRoofChangedSamples,otherProtectedCoreChanges;
            public TerrainQuality before,after;public NativeVistaPostGradeEvidence thermal;public PublicMaskedFeedbackEvidence feedback;
            public bool exactNativePhysicalCores,layersAlphaHolesExact,originalSourcesExact,originalMemoryExact,graphAndCriticalPosesExact,highlandMassAndPeakRetained,qualityLimitsUnchanged;
            public int physicalFootings,actualAbove200,originalAbove200,originalRoofMismatchesBefore,originalRoofMismatchesAfter;public float actualPeak;
            public CurrentFinalTerrainField[] fields;public List<PublicMaskedFeedbackStage> stages=new List<PublicMaskedFeedbackStage>();
        }
        static bool RequireCurrentFinalTerrainRecipeSelection(string[] args,string terrainMode,bool finishedArchitecture,bool finishArt,bool priorVista,bool vendorFalloff)
        {
            int at=Array.IndexOf(args,CurrentFinalTerrainRecipeFlag);if(at<0){if(RequireCurrentFinalOriginalRoofSelection(args)!=null)throw new InvalidOperationException("Original roof profile requires explicit current-final terrain recipe.");return false;}
            if(args.Count(a=>a==CurrentFinalTerrainRecipeFlag)!=1||at+1>=args.Length||args[at+1]!=CurrentFinalTerrainRecipeProfile)throw new InvalidOperationException("Explicit known current-final terrain recipe required; omitted flag preserves the original path.");
            RequireCurrentFinalOriginalRoofSelection(args);
            if(terrainMode!=NativeRoadReliefMode||!finishedArchitecture||!finishArt||priorVista||vendorFalloff)throw new InvalidOperationException("Current-final recipe requires native coordination, finished550-footing architecture and art, with no earlier Vista or SDK falloff recipe.");return true;
        }
        static RetainedThermalPhaseChain[] QualifyCurrentFinalTerrainPhases(Receipt proof)
        {
            bool Digest(string value)=>value!=null&&value.Length==64&&value.All(c=>(c>='0'&&c<='9')||(c>='a'&&c<='f'));
            void Require(bool pass,string detail){if(!pass)throw new InvalidOperationException("Current final terrain phase chain rejected before GPU: "+detail);}
            Require(proof!=null&&proof.status=="IN_PROGRESS"&&proof.terrainCoordinationMode==NativeRoadReliefMode&&proof.terrains?.Length==4,"in-progress current native candidate required");
            var native=proof.nativeRoadRelief;var urban=proof.urbanBanks;var berth=proof.berth;
            Require(native!=null&&native.passed&&native.actualVendorGradeCapturedBeforeCoordination&&native.sourceRoadPosesAndGraphUnchanged&&native.wholeTownAndGroveBoundingBoxesNotConstraints&&native.tiles?.Length==4,"completed actual raw SDK/coordinator evidence required");
            Require(urban!=null&&urban.passed&&urban.neverLowered&&urban.outsideMaskExact&&urban.waterCoreExact&&urban.roadsAndPosesExact&&urban.seamsExact&&urban.maximumOutsideDelta==0&&urban.outsideChangedSamples==0&&urban.tiles?.Length==4,"urban-bank mask/core/source/seam gates required");
            Require(berth!=null&&berth.passed&&berth.neverRaised&&berth.outsideMaskExact&&berth.seamsExact&&berth.shoreExact&&berth.ferryBedExact&&berth.maximumOutsideDelta==0&&berth.outsideChangedSamples==0&&berth.tiles?.Length==4,"berth mask/shore/ferry/seam gates required");
            var hull=berth.actualHull;Require(berth.policy=="NATIVE_ACTUAL_SOURCE_HULL_25_KEEL_CLEARANCES_AND_53_CONSERVATIVE_FERRY_SWEEPS"&&hull!=null&&hull.passed&&hull.sourceBytesExact&&hull.departureClear&&hull.depthSamples==25&&hull.departureSweepSamples==53&&hull.requiredKeelClearance==3&&hull.minimumKeelClearance>=3&&Digest(hull.sourceSha256)&&Digest(hull.sourceMetaSha256)&&Digest(hull.actualGeometrySha256)&&!string.IsNullOrEmpty(hull.source)&&!string.IsNullOrEmpty(hull.sourceGuid),"source-qualified physical merchant hull required");
            Require(proof.nativeArchitectureIntegration!=null&&proof.nativeArchitectureIntegration.inventoryIdentityPreserved&&proof.nativeArchitectureIntegration.totalIndividualFootings==550,"actual550-footing architecture required");
            Require(proof.terrains.All(t=>t!=null)&&proof.terrains.Select(t=>t.candidate).Distinct().Count()==4&&native.tiles.All(t=>t!=null)&&native.tiles.Select(t=>t.terrain).Distinct().Count()==4&&urban.tiles.All(t=>t!=null)&&urban.tiles.Select(t=>t.terrain).Distinct().Count()==4&&berth.tiles.All(t=>t!=null)&&berth.tiles.Select(t=>t.terrain).Distinct().Count()==4,"distinct four fields at every phase required");
            var rows=new List<RetainedThermalPhaseChain>();
            foreach(var record in proof.terrains)
            {
                string terrain=record.candidate;Require(terrain!=null&&terrain.StartsWith(Root+"VisualCandidates/"+proof.tag+"/Terrain/",StringComparison.Ordinal)&&record.resolution==513&&!string.IsNullOrEmpty(record.foundation),"current candidate field identity required");
                var n=native.tiles.SingleOrDefault(t=>t.terrain==terrain);var u=urban.tiles.SingleOrDefault(t=>t.terrain==terrain);var b=berth.tiles.SingleOrDefault(t=>t.terrain==terrain);Require(n!=null&&u!=null&&b!=null,"same field must occur in every phase");
                Require(u.resolution==513&&u.size.Equals(new Vector3(1600,600,1600))&&(u.origin.x==-1600||u.origin.x==0)&&u.origin.y==-20&&(u.origin.z==-800||u.origin.z==800),"original actual513 field frame required");
                Require(n.referenceAuthority=="ACTUAL_EASYROADS_OUTPUT"&&string.IsNullOrEmpty(n.actualVistaOutputHeightSha256)&&Digest(n.vendorGradedHeightSha256)&&n.compositionReferenceHeightSha256==n.vendorGradedHeightSha256&&Digest(n.coordinatedHeightSha256)&&Digest(n.authorizedMaskSha256)&&Digest(n.districtRaisedMaskSha256)&&n.outsideDeclaredCoordinationAreasExact&&n.validOriginalStartExtractionContactsExact&&n.nativeUndergroundSamplesExact,"raw SDK reference and exact native protected authorities required");
                Require(Digest(u.beforeHeightSha256)&&Digest(u.afterHeightSha256)&&Digest(b.beforeHeightSha256)&&Digest(b.afterHeightSha256)&&n.coordinatedHeightSha256==u.beforeHeightSha256&&u.afterHeightSha256==b.beforeHeightSha256,"coordinator -> urban -> berth causal hashes differ");
                Require(Digest(u.beforeOutsideSha256)&&u.beforeOutsideSha256==u.afterOutsideSha256&&Digest(u.afterBankSha256)&&Digest(b.beforeOutsideSha256)&&b.beforeOutsideSha256==b.afterOutsideSha256&&u.changedSamples>=0&&u.outsideSamples>=0&&u.bankSamples>=0&&u.outsideSamples+u.bankSamples==513*513&&u.changedSamples<=u.bankSamples&&b.changedSamples>=0&&b.outsideSamples>=0&&b.outsideSamples<=513*513&&b.changedSamples<=513*513-b.outsideSamples,"outside-mask digests and sample accounting required");
                Require((u.changedSamples==0)==(u.beforeHeightSha256==u.afterHeightSha256)&&(b.changedSamples==0)==(b.beforeHeightSha256==b.afterHeightSha256),"changed counts contradict hashes");
                rows.Add(new RetainedThermalPhaseChain{sourceRole=CurrentFinalTerrainInputRole,terrain=terrain,foundation=record.foundation,origin=u.origin,size=u.size,resolution=513,actualSdkRawHeightSha256=n.vendorGradedHeightSha256,compositionReferenceHeightSha256=n.compositionReferenceHeightSha256,nativeCoordinatedHeightSha256=n.coordinatedHeightSha256,urbanBeforeHeightSha256=u.beforeHeightSha256,urbanAfterHeightSha256=u.afterHeightSha256,berthBeforeHeightSha256=b.beforeHeightSha256,finalNativeHeightSha256=b.afterHeightSha256,nativeAuthorizedMaskSha256=n.authorizedMaskSha256,nativeDistrictRaisedMaskSha256=n.districtRaisedMaskSha256,urbanBeforeOutsideSha256=u.beforeOutsideSha256,urbanAfterOutsideSha256=u.afterOutsideSha256,berthBeforeOutsideSha256=b.beforeOutsideSha256,berthAfterOutsideSha256=b.afterOutsideSha256});
            }
            Require(rows.Select(r=>r.origin).Distinct().Count()==4,"four distinct actual original tile origins required");
            Require(urban.changedSamples==urban.tiles.Sum(t=>t.changedSamples)&&urban.outsideSamples==urban.tiles.Sum(t=>t.outsideSamples)&&berth.changedSamples==berth.tiles.Sum(t=>t.changedSamples)&&berth.outsideSamples==berth.tiles.Sum(t=>t.outsideSamples),"phase totals differ from fields");return rows.ToArray();
        }
        static Vector2Int CurrentFinalTileOffset(TileState tile)=>new Vector2Int(Mathf.RoundToInt((tile.terrain.transform.position.x+1600)/1600)*512,Mathf.RoundToInt((tile.terrain.transform.position.z+800)/1600)*512);
        static float[,] CaptureCurrentFinalMosaic(TileState[] tiles)
        {
            var result=new float[1025,1025];var seen=new bool[1025,1025];
            foreach(var tile in tiles){var offset=CurrentFinalTileOffset(tile);var values=tile.candidate.GetHeights(0,0,513,513);for(int z=0;z<513;z++)for(int x=0;x<513;x++){int iz=offset.y+z,ix=offset.x+x;if(seen[iz,ix]&&Mathf.Abs(result[iz,ix]-values[z,x])*600>.04f)throw new InvalidOperationException("Current native mosaic seam exceeds unchanged .04m authority.");result[iz,ix]=values[z,x];seen[iz,ix]=true;}}
            if(seen.Cast<bool>().Any(v=>!v))throw new InvalidOperationException("Complete current native mosaic required.");return result;
        }
        static IEnumerator ApplyCurrentFinalTerrainRecipe(DragonKingdomWorld world,TileState[] tiles,Bounds? roof,NativeArchitectureFooting[] footings,Receipt receipt)
        {
            var roofSelection=RequireCurrentFinalOriginalRoofSelection(Environment.GetCommandLineArgs());var phases=QualifyCurrentFinalTerrainPhases(receipt);var sources=ValidateRetainedThermalFootings(footings);
            if(receipt.nativeRoadRelief.footings==null||!footings.Select(f=>JsonUtility.ToJson(f)).SequenceEqual(receipt.nativeRoadRelief.footings.Select(f=>JsonUtility.ToJson(f))))throw new InvalidOperationException("Current actual architecture footprint authority differs from completed coordinator.");
            string output=Path.Combine(Output(),"CurrentFinalTerrainRecipe-"+receipt.tag);if(Directory.Exists(Io(output)))throw new InvalidOperationException("Fresh current-world vendor recipe evidence required.");Directory.CreateDirectory(Io(output));
            var proof=new CurrentFinalTerrainRecipeEvidence{physicalFootings=footings.Length,originalAbove200=tiles.Sum(t=>t.record.foundationAbove200)};receipt.currentFinalTerrainRecipe=proof;double started=EditorApplication.timeSinceStartup;
            void Stage(string name){proof.stages.Add(new PublicMaskedFeedbackStage{stage=name,utc=DateTime.UtcNow.ToString("O"),elapsedSeconds=EditorApplication.timeSinceStartup-started});File.WriteAllText(Io(Path.Combine(output,"current-final-terrain-recipe.json")),JsonUtility.ToJson(proof,true));}
            var hull=receipt.berth.actualHull;if(AssetSha(hull.source)!=hull.sourceSha256||AssetSha(hull.source+".meta")!=hull.sourceMetaSha256||AssetDatabase.AssetPathToGUID(hull.source)!=hull.sourceGuid)throw new InvalidOperationException("Current merchant hull source changed.");sources[hull.source]=hull.sourceSha256;sources[hull.source+".meta"]=hull.sourceMetaSha256;
            foreach(string path in new[]{Prefab,Preview,Root+"Configuration/Dragon Kingdom.asset",Root+"Editor/DragonKingdomRegionalWorldIntegration.CurrentFinalTerrainRecipe.cs"}.Concat(tiles.SelectMany(t=>new[]{t.record.foundation,t.record.foundation+".meta"})))sources[path]=AssetSha(path);
            var sourceMemory=tiles.Select(t=>t.original).Concat(tiles.Select(t=>AssetDatabase.LoadAssetAtPath<TerrainData>(t.record.foundation))).Distinct().ToDictionary(d=>d,d=>FinishFloatHash(d.GetHeights(0,0,513,513)));
            string graph=JsonUtility.ToJson(world.definition.graph);var poses=CriticalTransforms(world).Distinct().ToDictionary(t=>t,t=>Pose(t));var originalValues=new Dictionary<TileState,float[,]>();var authorizedValues=new Dictionary<TileState,float[,]>();var holes=tiles.ToDictionary(t=>t,t=>FinishHoleHash(t.candidate));var alpha=tiles.ToDictionary(t=>t,t=>FinishAlphaHash(t.candidate));var layers=tiles.ToDictionary(t=>t,t=>t.candidate.terrainLayers.ToArray());
            foreach(var tile in tiles){var phase=phases.Single(p=>p.terrain==tile.record.candidate);if(tile.terrain.transform.position!=phase.origin||tile.terrain.terrainData!=tile.candidate||tile.terrain.GetComponent<TerrainCollider>()?.terrainData!=tile.candidate)throw new InvalidOperationException("Current actual terrain/collider/frame binding differs.");RequireRetainedThermalHeight(tile.candidate,phase.finalNativeHeightSha256);originalValues.Add(tile,tile.candidate.GetHeights(0,0,513,513));authorizedValues.Add(tile,originalValues[tile]);}
            proof.fields=tiles.Select(t=>new CurrentFinalTerrainField{terrain=t.record.candidate,finalCompositionSha256=FinishFloatHash(originalValues[t]),authorizedPostRoofBaselineSha256=FinishFloatHash(originalValues[t])}).ToArray();
            var checkpoint=new CurrentFinalTerrainCheckpoint{tag=receipt.tag,graphSha256=FinishDigest(System.Text.Encoding.UTF8.GetBytes(graph)),phases=phases,protectedSources=sources.Select(p=>new RetainedThermalSourceFile{path=p.Key,sha256=p.Value}).ToArray()};proof.checkpoint=Path.Combine(output,"actual-current-final-input.json");File.WriteAllText(Io(proof.checkpoint),JsonUtility.ToJson(checkpoint,true));proof.checkpointSha256=AssetSha(proof.checkpoint);sources[proof.checkpoint]=proof.checkpointSha256;
            var previousRoofAuthority=currentFinalOriginalRoofAuthority;QualifiedCavernRoadAuthority roofAuthority=null;
            try
            {
                proof.before=ValidateCoordinatedTerrain(world,tiles,roof);Stage("ACTUAL_CURRENT_FINAL_PHASES_FIELDS_550_FOOTINGS_AND_BASELINE_QUALIFIED");
                if(roofSelection!=null)
                {
                    if(!roof.HasValue||!roof.Value.Equals(DragonKingdomWorldAuthoring.CavernUndergroundBounds())||previousRoofAuthority!=null)throw new InvalidOperationException("Exact original roof and isolated current-final authority context required.");
                    roofAuthority=LoadQualifiedCavernRoadAuthority(roofSelection.profile,roofSelection.receipt,roofSelection.tests);roofAuthority.ValidateWorld(world,roof.Value);currentFinalOriginalRoofAuthority=roofAuthority;proof.originalRoofProfile=roofSelection.profile;
                    foreach(string path in new[]{roofSelection.receipt,roofSelection.tests,Root+"Editor/DragonKingdomRegionalWorldIntegration.CavernAuthoredRoadAuthority.cs",Root+"Editor/DragonKingdomRegionalWorldIntegration.OriginalRoofProtection.cs"}.SelectMany(p=>new[]{p,p+".meta"}).Where(p=>File.Exists(Io(p))))sources[path]=AssetSha(path);
                    Stage("PASSED_NATIVE_AUTHORED_ROAD_AND_SIX_CAP_TOKEN_BOUND_TO_CURRENT_GRAPH_AND_MESHES");var maskProof=new NativeVistaPostGradeEvidence();var physicalMask=BuildQualifiedCavernRoofPhysicalMask(world,roof.Value,footings,maskProof,roofAuthority);var fields=new List<CurrentFinalRoofField>();var changed=new List<CurrentFinalRoofChangedCell>();
                    foreach(var tile in tiles)
                    {
                        var phase=phases.Single(p=>p.terrain==tile.record.candidate);var copy=CopyQualifiedOriginalUndergroundRoof(world,roofAuthority,roofSelection.profile,originalValues[tile],tile.foundation,tile.terrain.transform.position,tile.candidate.size,roof.Value,phase.actualSdkRawHeightSha256,phase.finalNativeHeightSha256,FinishFloatHash(tile.foundation),CurrentFinalTerrainInputRole,out var planned);var f=new CurrentFinalRoofField{terrain=tile.record.candidate,beforeRoofSha256=phase.finalNativeHeightSha256,afterRoofSha256=copy.protectedCompositionHeightSha256,foundationSha256=copy.foundationHeightSha256,copy=copy};fields.Add(f);authorizedValues[tile]=planned;var offset=CurrentFinalTileOffset(tile);
                        for(int z=0;z<513;z++)for(int x=0;x<513;x++)if(BitConverter.SingleToInt32Bits(originalValues[tile][z,x])!=BitConverter.SingleToInt32Bits(planned[z,x]))
                        {
                            var point=tile.terrain.transform.position+new Vector3(x*3.125f,0,z*3.125f);var owners=new List<string>();NativeVistaProtectionDistance(physicalMask,point,offset.x+x,offset.y+z,false,owners);var roles=ClassifyNativeVistaTerrainConflictOwners(physicalMask,point,owners);var cell=new CurrentFinalRoofChangedCell{terrain=f.terrain,point=point,beforeNormalized=originalValues[tile][z,x],foundationNormalized=planned[z,x],rawOwners=roles.rawOwners,undergroundColliderOwners=roles.undergroundColliderOwners,terrainConflictOwners=roles.terrainConflictOwners};changed.Add(cell);f.authorizedChangedSamples++;if(!InsideXZ(roof.Value,point)||BitConverter.SingleToInt32Bits(planned[z,x])!=BitConverter.SingleToInt32Bits(tile.foundation[z,x])||roles.terrainConflictOwners.Length>0)f.otherProtectedCoreChanges++;
                        }
                    }
                    proof.roofFields=fields.ToArray();proof.everyAuthorizedRoofChange=changed.ToArray();proof.authorizedRoofChangedSamples=changed.Count;proof.otherProtectedCoreChanges=fields.Sum(f=>f.otherProtectedCoreChanges);Stage("ORIGINAL_ROOF_EXACT_SOURCE_COPY_PLAN_AND_EVERY_CHANGED_CELL_ROLE_RECORDED_BEFORE_WRITES");if(proof.otherProtectedCoreChanges!=0)throw new InvalidOperationException("Original-roof plan conflicts with unrelated surface/start/extraction/footing authority; inspect every changed-cell roles before any write.");
                    foreach(var tile in tiles)
                    {
                        var f=fields.Single(v=>v.terrain==tile.record.candidate);var rect=f.copy.changedRectangle;if(rect.width>0&&rect.height>0){var block=new float[rect.height,rect.width];for(int z=0;z<rect.height;z++)for(int x=0;x<rect.width;x++)block[z,x]=authorizedValues[tile][rect.y+z,rect.x+x];tile.candidate.SetHeights(rect.x,rect.y,block);}tile.terrain.Flush();var actual=tile.candidate.GetHeights(0,0,513,513);f.actualNativeReadbackExact=FinishFloatHash(actual)==f.afterRoofSha256;f.holesLayersAlphaExact=FinishHoleHash(tile.candidate)==holes[tile]&&FinishAlphaHash(tile.candidate)==alpha[tile]&&tile.candidate.terrainLayers.SequenceEqual(layers[tile]);if(!f.actualNativeReadbackExact||!f.holesLayersAlphaExact)throw new InvalidOperationException("Exact original roof source-copy native readback/holes/layers/alpha failed.");authorizedValues[tile]=actual;proof.fields.Single(v=>v.terrain==f.terrain).authorizedPostRoofBaselineSha256=f.afterRoofSha256;
                    }
                    CaptureCurrentFinalMosaic(tiles);Physics.SyncTransforms();var roofCheckpoint=new CurrentFinalRoofCheckpoint{profile=roofSelection.profile,graphSha256=checkpoint.graphSha256,beforeRoofCheckpoint=proof.checkpoint,beforeRoofCheckpointSha256=proof.checkpointSha256,qualificationReceipt=roofSelection.receipt,qualificationReceiptSha256=AssetSha(roofSelection.receipt),qualificationTests=roofSelection.tests,qualificationTestsSha256=AssetSha(roofSelection.tests),fields=proof.roofFields,everyChangedCell=proof.everyAuthorizedRoofChange,unchangedActualSdkCoordinatorUrbanBerthPhases=phases};proof.roofCheckpoint=Path.Combine(output,"actual-current-post-original-roof-input.json");File.WriteAllText(Io(proof.roofCheckpoint),JsonUtility.ToJson(roofCheckpoint,true));proof.roofCheckpointSha256=AssetSha(proof.roofCheckpoint);sources[proof.roofCheckpoint]=proof.roofCheckpointSha256;proof.observedInputRole=CurrentFinalRoofProtectedInputRole;proof.qualifiedOriginalRoofApplied=true;Stage("ORIGINAL_ROOF_SOURCE_COPY_NATIVE_READBACK_AND_NEW_POST_ROOF_INPUT_CHECKPOINT_EXACT");
                }
                var authorities=phases.Select(p=>roofSelection==null?new NativeVistaInputAuthority{terrain=p.terrain,observedInputRole=CurrentFinalTerrainInputRole,expectedObservedHeightSha256=p.finalNativeHeightSha256,actualSdkRawHeightSha256=p.actualSdkRawHeightSha256,sourceReceipt=proof.checkpoint,sourceReceiptSha256=proof.checkpointSha256}:new NativeVistaInputAuthority{terrain=p.terrain,observedInputRole=CurrentFinalRoofProtectedInputRole,expectedObservedHeightSha256=proof.roofFields.Single(f=>f.terrain==p.terrain).afterRoofSha256,actualSdkRawHeightSha256=p.actualSdkRawHeightSha256,sourceReceipt=proof.roofCheckpoint,sourceReceiptSha256=proof.roofCheckpointSha256,originalFinalHeightSha256=p.finalNativeHeightSha256,originalRoofFoundationHeightSha256=proof.roofFields.Single(f=>f.terrain==p.terrain).foundationSha256,roofProtectionReceipt=proof.roofCheckpoint,roofProtectionReceiptSha256=proof.roofCheckpointSha256}).ToArray();var prior=LastNativeVistaPostGradeEvidence;
                Stage("CURRENT_BUFFER_PUBLIC_SINGLE_THERMAL1000_START");try{yield return ApplyNativeVistaPostGradeProfile(world,tiles,roof,footings,NativeThermal1000SingleProfile,Path.Combine(output,"Thermal1000"),authorities);}finally{if(!ReferenceEquals(prior,LastNativeVistaPostGradeEvidence))proof.thermal=LastNativeVistaPostGradeEvidence;}
                var thermal=proof.thermal;if(thermal==null||!thermal.publicThermalNodeConfiguredExact||!thermal.actualUnityReadbackVerified||!thermal.allProtectedInputSamplesExact||!thermal.sourceBytesExact||!thermal.graphAndCriticalPosesExact||thermal.individualArchitectureFootings!=550||thermal.observedInputRole!=proof.observedInputRole)throw new InvalidOperationException("Current actual single Thermal1000 native/source/core proof required.");
                Stage("CURRENT_BUFFER_PUBLIC_SINGLE_THERMAL1000_NATIVE_END");proof.maskPath=Path.Combine(output,"Thermal1000","physical-core-editable-weight.float32");proof.maskSha256=AssetSha(proof.maskPath);if(proof.maskSha256!=thermal.hardnessEditableWeightSha256)throw new InvalidOperationException("Use exactly the mask emitted by the current Thermal authority, never reconstruct a second mask.");var maskBytes=File.ReadAllBytes(Io(proof.maskPath));if(maskBytes.Length!=1025*1025*4)throw new InvalidOperationException("Current actual1025 mask required.");var weight=new float[1025,1025];Buffer.BlockCopy(maskBytes,0,weight,0,maskBytes.Length);sources[proof.maskPath]=proof.maskSha256;
                foreach(var tile in tiles){var row=proof.fields.Single(f=>f.terrain==tile.record.candidate);row.thermalSha256=FinishFloatHash(tile.candidate.GetHeights(0,0,513,513));if(row.thermalSha256!=thermal.tiles.Single(t=>t.terrain==row.terrain).actualVistaOutputSha256)throw new InvalidOperationException("Current thermal native output changed before feedback.");}
                var input=CaptureCurrentFinalMosaic(tiles);proof.feedback=new PublicMaskedFeedbackEvidence();yield return ExecutePublicMaskedCurrentFeedback(input,weight,1000,10,50,Path.Combine(output,"MaskedFeedback1000"),proof.feedback,Stage);
                var feedback=proof.feedback;if(!feedback.resourcesRestored||!feedback.everyGpuReadbackExact||!feedback.sourceBytesExact||!feedback.inputArraysExact||feedback.actualOutput==null||feedback.maskSha256!=proof.maskSha256)throw new InvalidOperationException("Shared actual current vendor feedback core/input/source/resource proof required.");
                proof.exactNativePhysicalCores=true;proof.layersAlphaHolesExact=true;proof.actualPeak=float.NegativeInfinity;
                foreach(var tile in tiles)
                {
                    var offset=CurrentFinalTileOffset(tile);var row=proof.fields.Single(f=>f.terrain==tile.record.candidate);var values=new float[513,513];for(int z=0;z<513;z++)for(int x=0;x<513;x++)values[z,x]=feedback.actualOutput[offset.y+z,offset.x+x];tile.candidate.SetHeights(0,0,values);tile.terrain.Flush();var actual=tile.candidate.GetHeights(0,0,513,513);
                    tile.record.integratedAbove200=0;tile.record.integratedAbove400=0;tile.record.integratedMaximumY=float.NegativeInfinity;tile.record.undergroundFoundationExact=true;
                    for(int z=0;z<513;z++)for(int x=0;x<513;x++)
                    {
                        if(!float.IsFinite(actual[z,x])||actual[z,x]<0||actual[z,x]>1)throw new InvalidOperationException("Actual native final height is outside the unchanged finite domain.");float y=actual[z,x]*600-20;if(y>=200)tile.record.integratedAbove200++;if(y>=400)tile.record.integratedAbove400++;tile.record.integratedMaximumY=Mathf.Max(tile.record.integratedMaximumY,y);
                        if(weight[offset.y+z,offset.x+x]==0){row.protectedSamples++;if(BitConverter.SingleToInt32Bits(actual[z,x])!=BitConverter.SingleToInt32Bits(authorizedValues[tile][z,x]))row.protectedChanged++;if(BitConverter.SingleToInt32Bits(actual[z,x])!=BitConverter.SingleToInt32Bits(originalValues[tile][z,x]))row.protectedChangesRelativeToBeforeRoof++;}
                        var point=tile.terrain.transform.position+new Vector3(x*3.125f,0,z*3.125f);if(roof.HasValue&&InsideXZ(roof.Value,point)){if(originalValues[tile][z,x]!=tile.foundation[z,x])row.originalRoofMismatchesBefore++;if(actual[z,x]!=tile.foundation[z,x]){row.originalRoofMismatchesAfter++;tile.record.undergroundFoundationExact=false;}}
                    }
                    row.actualFeedbackSha256=FinishFloatHash(actual);row.layersAlphaHolesExact=FinishHoleHash(tile.candidate)==holes[tile]&&FinishAlphaHash(tile.candidate)==alpha[tile]&&tile.candidate.terrainLayers.SequenceEqual(layers[tile]);proof.layersAlphaHolesExact&=row.layersAlphaHolesExact;proof.exactNativePhysicalCores&=row.protectedChanged==0;proof.originalRoofMismatchesBefore+=row.originalRoofMismatchesBefore;proof.originalRoofMismatchesAfter+=row.originalRoofMismatchesAfter;proof.actualAbove200+=tile.record.integratedAbove200;proof.actualPeak=Mathf.Max(proof.actualPeak,tile.record.integratedMaximumY);EditorUtility.SetDirty(tile.candidate);File.WriteAllBytes(Io(Path.Combine(output,Path.GetFileNameWithoutExtension(row.terrain)+".actual-native.float32")),FinishFloatBytes(actual));
                }
                proof.authorizedRoofProtectedChangeAccountingExact=proof.fields.All(f=>f.protectedChangesRelativeToBeforeRoof==(roofSelection==null?0:proof.roofFields.Single(r=>r.terrain==f.terrain).authorizedChangedSamples));if(!proof.authorizedRoofProtectedChangeAccountingExact)throw new InvalidOperationException("Every protected change relative to the pre-roof checkpoint must match an explicitly recorded authorized source-roof change; no hidden baseline reset.");
                Physics.SyncTransforms();proof.after=ValidateCoordinatedTerrain(world,tiles,roof);proof.qualityLimitsUnchanged=proof.before.allowedSteep65==proof.after.allowedSteep65&&proof.before.allowedSteep80==proof.after.allowedSteep80;proof.highlandMassAndPeakRetained=proof.actualAbove200>=proof.originalAbove200*.65f&&proof.actualPeak>=450;
                Stage("CURRENT_BUFFER_ACTUAL_NATIVE_READBACK_QUALITY_MASS_AND_ORIGINAL_ROOF_RECORDED");
                if(!proof.exactNativePhysicalCores||!proof.layersAlphaHolesExact||!proof.after.passed||!proof.qualityLimitsUnchanged||!proof.highlandMassAndPeakRetained)throw new InvalidOperationException("Current final vendor recipe rejected unchanged native quality/core/layer/mass gates: steep65="+proof.after.integratedSteep65+"/"+proof.after.allowedSteep65+", steep80="+proof.after.integratedSteep80+"/"+proof.after.allowedSteep80+"; original roof remains separately required.");
                if(roofSelection!=null&&(!proof.qualifiedOriginalRoofApplied||proof.originalRoofMismatchesAfter!=0||proof.otherProtectedCoreChanges!=0))throw new InvalidOperationException("Qualified current original-roof profile must retain every exact source roof sample and every unrelated protected core after both vendor stages.");
                proof.status="CURRENT_WORLD_VENDOR_RECIPE_PASSED_REQUIRES_ORIGINAL_ROOF_DIGGER_AND_FULL_WORLD_ACCEPTANCE";
            }
            finally
            {
                currentFinalOriginalRoofAuthority=previousRoofAuthority;proof.roofAuthorityContextRestored=ReferenceEquals(currentFinalOriginalRoofAuthority,previousRoofAuthority);proof.roofAuthoritySourcesExact=true;if(roofAuthority!=null)try{roofAuthority.ValidateWorld(world,roof.Value);roofAuthority.VerifySourcesUnchanged();}catch(Exception error){proof.roofAuthoritySourcesExact=false;proof.roofAuthorityError=error.ToString();}
                proof.originalSourcesExact=sources.All(p=>File.Exists(Io(p.Key))&&AssetSha(p.Key)==p.Value);proof.originalMemoryExact=sourceMemory.All(p=>FinishFloatHash(p.Key.GetHeights(0,0,513,513))==p.Value);proof.graphAndCriticalPosesExact=JsonUtility.ToJson(world.definition.graph)==graph&&poses.All(p=>Pose(p.Key)==p.Value);if(proof.status=="IN_PROGRESS"||!proof.roofAuthoritySourcesExact||!proof.roofAuthorityContextRestored||!proof.originalSourcesExact||!proof.originalMemoryExact||!proof.graphAndCriticalPosesExact)proof.status="REJECTED_CURRENT_FINAL_VENDOR_RECIPE_INSPECT_NATIVE_EXCEPTION";Stage("CURRENT_WORLD_ORIGINAL_SOURCES_MEMORY_GRAPH_AND_POSES_CHECKED");if(!proof.roofAuthoritySourcesExact||!proof.roofAuthorityContextRestored||!proof.originalSourcesExact||!proof.originalMemoryExact||!proof.graphAndCriticalPosesExact)throw new InvalidOperationException("Current final terrain recipe changed original source bytes, memory, graph or critical poses.");
            }
        }
    }
}
#endif
