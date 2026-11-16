#if UNITY_EDITOR && VISTA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable]public sealed class RetainedThermalSourceFile{public string path,sha256;}
        const string RetainedThermalSourceRole="ACTUAL_FINAL_V417_AFTER_NATIVE_COORDINATION_URBAN_BANKS_AND_MERCHANT_BERTH_NOT_RAW_SDK";
        [Serializable]public sealed class RetainedThermalPhaseChain
        {
            public string terrain,foundation,sourceRole=RetainedThermalSourceRole,actualSdkRawHeightSha256,compositionReferenceHeightSha256,nativeCoordinatedHeightSha256,urbanBeforeHeightSha256,urbanAfterHeightSha256,berthBeforeHeightSha256,finalNativeHeightSha256;
            public string nativeAuthorizedMaskSha256,nativeDistrictRaisedMaskSha256,urbanBeforeOutsideSha256,urbanAfterOutsideSha256,berthBeforeOutsideSha256,berthAfterOutsideSha256;
            public Vector3 origin,size;public int resolution;
        }
        [Serializable]public sealed class RetainedThermalField{public string source,sourceSha256,sourceRole=RetainedThermalSourceRole,inputHeightSha256,candidate,candidateSha256,outputHeightSha256;}
        [Serializable]public sealed class RetainedThermalTrial
        {
            public string profile,status="IN_PROGRESS",error;public TerrainQuality before,after;
            public NativeVistaPostGradeEvidence vendor;public RetainedThermalField[] fields;
            public int foundationSamplesAbove200,actualSamplesAbove200;public float maximumHeight;
            public bool baselineExact,allProtectedSamplesExact,originalQualityLimitsPreserved,highlandMassAndPeakRetained;
        }
        [Serializable]public sealed class RetainedThermalReport
        {
            public string scope="ACTUAL_RETAINED_REJECTED_COORDINATED_FIELDS_PUBLIC_THERMAL3_PROBE_NOT_FULL_WORLD_COMPOSITION_DRIVING_OR_ADOPTION",status="IN_PROGRESS",error,sourceEvidence,sourceReceiptSha256,sourceTestsSha256,originalGraphSha256,candidateGraphSha256;
            public bool originalSourcesExact,retainedNativeHeightsQualified,recordedFootingSourcesExact,sourceSceneAndTerrainMemoryExact,ownedSelectionAndRandomRestored;
            public string sourceRole=RetainedThermalSourceRole,family;public string[] selectedProfiles;public RetainedThermalPhaseChain[] phaseChains;
            public int originalSteep65,originalSteep80,individualFootings,distinctPhysicalSourcePaths;public List<RetainedThermalTrial> trials=new List<RetainedThermalTrial>();public RetainedThermalSourceFile[] protectedSources;
        }
        public static RetainedThermalReport LastRetainedThermalReport{get;private set;}
        static void RequireRetainedThermalHeight(TerrainData data,string expected)
        {
            string observed=data!=null&&data.heightmapResolution==513&&data.size==new Vector3(1600,600,1600)?FinishFloatHash(data.GetHeights(0,0,513,513)):"INVALID_NATIVE_FIELD_SHAPE";
            if(string.IsNullOrEmpty(expected)||observed!=expected)throw new InvalidOperationException("Retained native terrain differs from the actual final native height SHA (after coordinator, urban banks and merchant berth); expected="+expected+" observed="+observed+". Do not regrade, substitute foundation data or infer a fixture.");
        }
        static Dictionary<string,string> ValidateRetainedThermalFootings(NativeArchitectureFooting[] footings)
        {
            if(footings==null||footings.Length!=550||footings.Select(f=>f?.id).Distinct().Count()!=550)throw new InvalidOperationException("Actual550 distinct recorded native architecture/retained-building footings required.");
            var observed=new Dictionary<string,string>(StringComparer.Ordinal);void Require(string path,string expected){if(string.IsNullOrEmpty(path)||string.IsNullOrEmpty(expected)||!File.Exists(Io(path)))throw new InvalidOperationException("Recorded native footing source missing: "+path);if(!observed.TryGetValue(path,out string actual)){actual=AssetSha(path);observed.Add(path,actual);}if(actual!=expected)throw new InvalidOperationException("Recorded native footing source changed: "+path);}
            foreach(var f in footings)
            {
                if(f==null||string.IsNullOrEmpty(f.id)||string.IsNullOrEmpty(f.instance)||string.IsNullOrEmpty(f.sourcePrefabGuid)||f.physicalFootprint.size.x<=0||f.physicalFootprint.size.z<=0||!float.IsFinite(f.surfaceY))throw new InvalidOperationException("Complete finite measured native footing provenance required.");
                Require(f.layout,f.layoutSha256);Require(f.sourcePrefab,f.sourcePrefabSha256);Require(f.sourcePrefab+".meta",f.sourcePrefabMetaSha256);Require(f.sourceGeometryWitness,f.sourceGeometryWitnessSha256);if(AssetDatabase.AssetPathToGUID(f.sourcePrefab)!=f.sourcePrefabGuid)throw new InvalidOperationException("Recorded native footing GUID changed.");
            }
            return observed;
        }
        // The saved native assets are the FINAL composition, not the coordinator's
        // earlier output. Retain all causal stage hashes rather than weakening the
        // loaded-field check or relabelling a post-SDK composition as raw SDK data.
        static RetainedThermalPhaseChain[] QualifyRetainedThermalPhaseChain(Receipt proof)
        {
            bool Digest(string value)=>value!=null&&value.Length==64&&value.All(c=>(c>='0'&&c<='9')||(c>='a'&&c<='f'));
            void Require(bool condition,string detail){if(!condition)throw new InvalidOperationException("Actual V417 retained terrain phase chain rejected: "+detail+". No thermal GPU work permitted.");}
            Require(proof!=null&&proof.sourceBytesPreserved&&proof.terrains?.Length==4,"four source-preserved final terrain records required");
            var native=proof.nativeRoadRelief;var urban=proof.urbanBanks;var berth=proof.berth;
            Require(native!=null&&native.passed&&native.actualVendorGradeCapturedBeforeCoordination&&native.sourceRoadPosesAndGraphUnchanged&&native.wholeTownAndGroveBoundingBoxesNotConstraints&&native.tiles?.Length==4,"completed native coordinator and its source/graph preservation gates required");
            Require(urban!=null&&urban.passed&&urban.neverLowered&&urban.outsideMaskExact&&urban.waterCoreExact&&urban.roadsAndPosesExact&&urban.seamsExact&&urban.maximumOutsideDelta==0&&urban.outsideChangedSamples==0&&urban.tiles?.Length==4,"completed urban-bank outside/core/road/pose/seam mask gates required");
            Require(berth!=null&&berth.passed&&berth.neverRaised&&berth.outsideMaskExact&&berth.seamsExact&&berth.shoreExact&&berth.ferryBedExact&&berth.maximumOutsideDelta==0&&berth.outsideChangedSamples==0&&berth.tiles?.Length==4,"completed berth outside/shore/ferry/seam mask gates required");
            var hull=berth.actualHull;Require(berth.policy=="NATIVE_ACTUAL_SOURCE_HULL_25_KEEL_CLEARANCES_AND_53_CONSERVATIVE_FERRY_SWEEPS"&&hull!=null&&hull.passed&&hull.sourceBytesExact&&hull.departureClear&&hull.depthSamples==25&&hull.departureSweepSamples==53&&hull.requiredKeelClearance==3&&hull.minimumKeelClearance>=3&&Digest(hull.sourceSha256)&&Digest(hull.sourceMetaSha256)&&Digest(hull.actualGeometrySha256)&&!string.IsNullOrEmpty(hull.source)&&!string.IsNullOrEmpty(hull.sourceGuid),"actual source-qualified merchant hull clearance evidence required");
            Require(proof.terrains.All(t=>t!=null)&&proof.terrains.Select(t=>t.candidate).Distinct().Count()==4&&native.tiles.All(t=>t!=null)&&native.tiles.Select(t=>t.terrain).Distinct().Count()==4&&urban.tiles.All(t=>t!=null)&&urban.tiles.Select(t=>t.terrain).Distinct().Count()==4&&berth.tiles.All(t=>t!=null)&&berth.tiles.Select(t=>t.terrain).Distinct().Count()==4,"no missing or duplicate tile at any stage");
            var rows=new List<RetainedThermalPhaseChain>();
            for(int z=0;z<2;z++)for(int x=0;x<2;x++)
            {
                string terrain=Root+"VisualCandidates/OwnedRoadRockV417/Terrain/Kingdom "+x+" "+z+".asset",foundation=Root+"ReviewCandidates/RoundedVista01/Terrain/Kingdom "+x+" "+z+".asset";
                var record=proof.terrains.SingleOrDefault(t=>t.candidate==terrain);var n=native.tiles.SingleOrDefault(t=>t.terrain==terrain);var u=urban.tiles.SingleOrDefault(t=>t.terrain==terrain);var b=berth.tiles.SingleOrDefault(t=>t.terrain==terrain);
                Require(record!=null&&n!=null&&u!=null&&b!=null,"missing actual tile "+terrain);
                Require(record.foundation==foundation&&record.resolution==513&&u.resolution==513&&u.origin.Equals(new Vector3(-1600+1600*x,-20,-800+1600*z))&&u.size.Equals(new Vector3(1600,600,1600)),"original foundation, 513 grid, origin and shape mismatch: "+terrain);
                Require(n.referenceAuthority=="ACTUAL_EASYROADS_OUTPUT"&&string.IsNullOrEmpty(n.actualVistaOutputHeightSha256)&&Digest(n.vendorGradedHeightSha256)&&n.compositionReferenceHeightSha256==n.vendorGradedHeightSha256&&Digest(n.coordinatedHeightSha256)&&Digest(n.authorizedMaskSha256)&&Digest(n.districtRaisedMaskSha256)&&n.outsideDeclaredCoordinationAreasExact&&n.validOriginalStartExtractionContactsExact&&n.nativeUndergroundSamplesExact,"raw SDK/coordinator provenance or protected mask mismatch: "+terrain);
                Require(Digest(u.beforeHeightSha256)&&Digest(u.afterHeightSha256)&&Digest(b.beforeHeightSha256)&&Digest(b.afterHeightSha256)&&n.coordinatedHeightSha256==u.beforeHeightSha256&&u.afterHeightSha256==b.beforeHeightSha256,"coordinator -> urban before/after -> berth before/after causal hash link mismatch: "+terrain);
                Require(Digest(u.beforeOutsideSha256)&&u.beforeOutsideSha256==u.afterOutsideSha256&&Digest(u.afterBankSha256)&&Digest(b.beforeOutsideSha256)&&b.beforeOutsideSha256==b.afterOutsideSha256&&u.changedSamples>=0&&u.outsideSamples>=0&&u.bankSamples>=0&&u.outsideSamples+u.bankSamples==513*513&&u.changedSamples<=u.bankSamples&&b.changedSamples>=0&&b.outsideSamples>=0&&b.outsideSamples<=513*513&&b.changedSamples<=513*513-b.outsideSamples,"outside-mask hashes or declared sample accounting mismatch: "+terrain);
                Require((u.changedSamples==0)==(u.beforeHeightSha256==u.afterHeightSha256)&&(b.changedSamples==0)==(b.beforeHeightSha256==b.afterHeightSha256),"changed sample counts contradict actual stage hashes: "+terrain);
                rows.Add(new RetainedThermalPhaseChain{terrain=terrain,foundation=foundation,origin=u.origin,size=u.size,resolution=u.resolution,actualSdkRawHeightSha256=n.vendorGradedHeightSha256,compositionReferenceHeightSha256=n.compositionReferenceHeightSha256,nativeCoordinatedHeightSha256=n.coordinatedHeightSha256,urbanBeforeHeightSha256=u.beforeHeightSha256,urbanAfterHeightSha256=u.afterHeightSha256,berthBeforeHeightSha256=b.beforeHeightSha256,finalNativeHeightSha256=b.afterHeightSha256,nativeAuthorizedMaskSha256=n.authorizedMaskSha256,nativeDistrictRaisedMaskSha256=n.districtRaisedMaskSha256,urbanBeforeOutsideSha256=u.beforeOutsideSha256,urbanAfterOutsideSha256=u.afterOutsideSha256,berthBeforeOutsideSha256=b.beforeOutsideSha256,berthAfterOutsideSha256=b.afterOutsideSha256});
            }
            Require(urban.changedSamples==urban.tiles.Sum(t=>t.changedSamples)&&urban.outsideSamples==urban.tiles.Sum(t=>t.outsideSamples)&&berth.changedSamples==berth.tiles.Sum(t=>t.changedSamples)&&berth.outsideSamples==berth.tiles.Sum(t=>t.outsideSamples),"stage total sample accounting mismatch");return rows.ToArray();
        }
        static Receipt RequireRetainedThermalReceipt(string evidence,out string receiptPath)
        {
            if(string.IsNullOrEmpty(evidence))throw new InvalidOperationException("Explicit -tsDragonRetainedThermalSource actual native run required.");
            string xmlPath=Path.Combine(evidence,"tests.xml");receiptPath=Path.Combine(evidence,"rendered-evidence","regional-world-OwnedRoadRockV417.json");var xml=new XmlDocument();xml.Load(Io(xmlPath));var cases=xml.SelectNodes("//test-case").Cast<XmlNode>().Where(n=>n.Attributes["fullname"]?.Value=="DragonKingdomOwnedMountainIntegrationTests.ExplicitQualifiedMountainProfileRetainsExistingFullWorldAcceptanceGates").ToArray();
            if(cases.Length!=1||cases[0].Attributes["result"]?.Value!="Failed"||!(cases[0].SelectSingleNode("failure/message")?.InnerText??"").Contains("steep65=20464/2000"))throw new InvalidOperationException("Use the actual V417 terrain-quality rejection, not an arbitrary partial or timed-out world.");
            var proof=JsonUtility.FromJson<Receipt>(File.ReadAllText(Io(receiptPath)));
            if(proof==null||proof.tag!="OwnedRoadRockV417"||proof.status!="REJECTED_CANDIDATE"||!proof.sourceBytesPreserved||proof.terrains==null||proof.terrains.Length!=4||proof.terrainQuality==null||proof.terrainQuality.integratedSteep65!=20464||proof.terrainQuality.integratedSteep80!=4492||proof.terrainQuality.conflictingGroundCoreSamples!=0||proof.terrainQuality.maximumTileSeamDelta!=0||proof.nativeRoadRelief==null||!proof.nativeRoadRelief.passed||proof.nativeRoadRelief.tiles?.Length!=4||proof.nativeArchitectureIntegration==null||!proof.nativeArchitectureIntegration.inventoryIdentityPreserved||proof.nativeArchitectureIntegration.totalIndividualFootings!=550||proof.ownedMountainRoad==null)throw new InvalidOperationException("Complete actual rejected V417 coordinated-field/550-parcel/owned-road evidence required; no world acceptance inferred.");QualifyRetainedThermalPhaseChain(proof);return proof;
        }
        public static IEnumerator CaptureRetainedCoordinatedThermalRepair()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required for the isolated retained-field probe.");
            string evidence=Argument("-tsDragonRetainedThermalSource",""),tag=Argument("-tsDragonRetainedThermalTag","");if(tag.Length<1||tag.Length>24||tag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new InvalidOperationException("Unique bounded retained-thermal output tag required.");
            var source=RequireRetainedThermalReceipt(evidence,out string receiptPath);string output=Path.Combine(Output(),"RetainedThermal"),folder=Root+"VisualCandidates/RetainedThermal-"+tag;if(Directory.Exists(Io(folder))||File.Exists(Io(folder+".meta"))||Directory.Exists(Io(output)))throw new InvalidOperationException("Preserve existing trial outputs; use a fresh retained-thermal tag/native output.");
            var proof=new RetainedThermalReport{sourceEvidence=evidence,sourceReceiptSha256=AssetSha(receiptPath),sourceTestsSha256=AssetSha(Path.Combine(evidence,"tests.xml")),originalSteep65=source.terrainQuality.integratedSteep65,originalSteep80=source.terrainQuality.integratedSteep80,individualFootings=550,phaseChains=QualifyRetainedThermalPhaseChain(source),family=Argument("-tsDragonRetainedThermalFamily","MultiResolution01"),selectedProfiles=SelectedRetainedThermalProfiles()};LastRetainedThermalReport=proof;
            var sources=ValidateRetainedThermalFootings(source.nativeRoadRelief.footings);proof.distinctPhysicalSourcePaths=sources.Count;proof.recordedFootingSourcesExact=true;
            var hull=source.berth.actualHull;if(!File.Exists(Io(hull.source))||!File.Exists(Io(hull.source+".meta"))||AssetSha(hull.source)!=hull.sourceSha256||AssetSha(hull.source+".meta")!=hull.sourceMetaSha256||AssetDatabase.AssetPathToGUID(hull.source)!=hull.sourceGuid)throw new InvalidOperationException("Actual retained final-stage merchant hull source/GUID changed; no thermal GPU work permitted.");sources[hull.source]=hull.sourceSha256;sources[hull.source+".meta"]=hull.sourceMetaSha256;
            foreach(string path in new[]{Prefab,Preview,Root+"Configuration/Dragon Kingdom.asset",receiptPath,Path.Combine(evidence,"tests.xml")}.Concat(source.terrains.SelectMany(t=>new[]{t.candidate,t.candidate+".meta",t.foundation,t.foundation+".meta"})))
            {if(!File.Exists(Io(path)))throw new InvalidOperationException("Retained terrain/source input missing: "+path);sources[path]=AssetSha(path);}
            var retained=source.terrains.ToDictionary(t=>t.candidate,t=>AssetDatabase.LoadAssetAtPath<TerrainData>(t.candidate),StringComparer.Ordinal);
            foreach(var row in source.terrains)RequireRetainedThermalHeight(retained[row.candidate],proof.phaseChains.Single(t=>t.terrain==row.candidate).finalNativeHeightSha256);proof.retainedNativeHeightsQualified=true;
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve incoming user scenes before retained-field diagnosis.");
            Scene scene=default;GameObject root=null;OwnedMountainContext mountain=null;var inputMemory=retained.ToDictionary(p=>p.Key,p=>FinishFloatHash(p.Value.GetHeights(0,0,513,513)));var bindings=new Dictionary<Terrain,TerrainData>();var colliderBindings=new Dictionary<TerrainCollider,TerrainData>();var created=new List<TerrainData>();var originalMemory=new Dictionary<TerrainData,string>();
            Directory.CreateDirectory(Io(output));Directory.CreateDirectory(Io(folder));AssetDatabase.Refresh();
            try
            {
                scene=EditorSceneManager.NewPreviewScene();root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),scene);var world=root.GetComponent<DragonKingdomWorld>();
                mountain=SelectOwnedMountainFromArguments();if(mountain==null)throw new InvalidOperationException("Same explicit actual qualified owned-road selection required; no old graph substitute.");mountain.Apply(world);proof.originalGraphSha256=mountain.evidence.originalGraphSha256;proof.candidateGraphSha256=FinishDigest(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(world.definition.graph)));if(proof.candidateGraphSha256!=source.ownedMountainRoad.candidateGraphSha256)throw new InvalidOperationException("Retained quality must use the exact V417 candidate graph.");
                foreach(var terrain in world.GetComponentsInChildren<Terrain>(true)){bindings.Add(terrain,terrain.terrainData);var collider=terrain.GetComponent<TerrainCollider>();if(collider==null||collider.terrainData!=terrain.terrainData)throw new InvalidOperationException("Original matching terrain/collider bindings required.");colliderBindings.Add(collider,collider.terrainData);}if(bindings.Count!=4)throw new InvalidOperationException("Exactly four original core terrains required.");foreach(var data in bindings.Values.Distinct())originalMemory.Add(data,FinishFloatHash(data.GetHeights(0,0,513,513)));
                foreach(string profile in proof.selectedProfiles)
                {
                    string label=NativeThermalOutputLabel(profile),trialFolder=folder+"/"+label;Directory.CreateDirectory(Io(trialFolder));AssetDatabase.Refresh();var trial=new RetainedThermalTrial{profile=profile};proof.trials.Add(trial);var fields=new List<RetainedThermalField>();
                    var states=bindings.Keys.Select(terrain=>
                    {
                        int x=Mathf.RoundToInt((terrain.transform.position.x+1600)/1600),z=Mathf.RoundToInt((terrain.transform.position.z+800)/1600);var record=source.terrains.Single(r=>r.candidate.EndsWith("/Kingdom "+x+" "+z+".asset",StringComparison.Ordinal));var chain=proof.phaseChains.Single(t=>t.terrain==record.candidate);if(!terrain.transform.position.Equals(chain.origin))throw new InvalidOperationException("Exact retained final native tile origin required.");var saved=retained[record.candidate];var foundation=AssetDatabase.LoadAssetAtPath<TerrainData>(record.foundation);if(foundation==null||foundation.heightmapResolution!=513||foundation.size!=saved.size||terrain.transform.position.y!=-20)throw new InvalidOperationException("Exact actual original foundation/core frame required.");var candidate=UnityEngine.Object.Instantiate(saved);candidate.name="Retained actual thermal "+x+" "+z+" "+label;string path=trialFolder+"/Kingdom "+x+" "+z+".asset";AssetDatabase.CreateAsset(candidate,path);created.Add(candidate);terrain.terrainData=candidate;terrain.GetComponent<TerrainCollider>().terrainData=candidate;terrain.Flush();var input=saved.GetHeights(0,0,513,513);fields.Add(new RetainedThermalField{source=record.candidate,sourceSha256=sources[record.candidate],inputHeightSha256=FinishFloatHash(input),candidate=path});return new TileState{terrain=terrain,original=saved,candidate=candidate,before=input,foundation=foundation.GetHeights(0,0,513,513),fieldTags=new byte[513,513],fieldOwners=new string[513,513],record=new TileRecord{candidate=path,foundation=record.foundation,resolution=513}};
                    }).ToArray();trial.fields=fields.ToArray();Physics.SyncTransforms();trial.before=ValidateCoordinatedTerrain(world,states,DragonKingdomWorldAuthoring.CavernUndergroundBounds());trial.baselineExact=RetainedThermalQualityEqual(trial.before,source.terrainQuality);if(!trial.baselineExact)throw new InvalidOperationException("Loaded actual retained fields do not reproduce V41720464/4492/core0/seam0 unchanged quality; no thermal operation permitted.");
                    var inputAuthorities=states.Select(state=>{var chain=proof.phaseChains.Single(t=>t.terrain==fields.Single(f=>f.candidate==state.record.candidate).source);return new NativeVistaInputAuthority{terrain=state.record.candidate,observedInputRole=RetainedThermalSourceRole,expectedObservedHeightSha256=chain.finalNativeHeightSha256,actualSdkRawHeightSha256=chain.actualSdkRawHeightSha256,sourceReceipt=receiptPath,sourceReceiptSha256=proof.sourceReceiptSha256};}).ToArray();
                    var vendor=ApplyNativeVistaPostGradeProfile(world,states,DragonKingdomWorldAuthoring.CavernUndergroundBounds(),source.nativeRoadRelief.footings,profile,Path.Combine(output,label),inputAuthorities);while(true){bool more;try{more=vendor.MoveNext();}catch(Exception error){trial.error=error.ToString();proof.error=error.ToString();throw;}if(!more)break;yield return vendor.Current;}trial.vendor=LastNativeVistaPostGradeEvidence;trial.allProtectedSamplesExact=trial.vendor.allProtectedInputSamplesExact&&trial.vendor.maximumProtectedDelta==0&&trial.vendor.sourceBytesExact&&trial.vendor.graphAndCriticalPosesExact;
                    trial.after=ValidateCoordinatedTerrain(world,states,DragonKingdomWorldAuthoring.CavernUndergroundBounds());trial.originalQualityLimitsPreserved=trial.before.allowedSteep65==trial.after.allowedSteep65&&trial.before.allowedSteep80==trial.after.allowedSteep80;trial.foundationSamplesAbove200=source.terrains.Sum(t=>t.foundationAbove200);
                    foreach(var state in states){var values=state.candidate.GetHeights(0,0,513,513);foreach(float value in values){float height=value*600-20;if(height>=200)trial.actualSamplesAbove200++;trial.maximumHeight=Mathf.Max(trial.maximumHeight,height);}AssetDatabase.SaveAssetIfDirty(state.candidate);var field=fields.Single(f=>f.candidate==state.record.candidate);field.candidateSha256=AssetSha(field.candidate);field.outputHeightSha256=FinishFloatHash(values);}
                    trial.highlandMassAndPeakRetained=trial.actualSamplesAbove200>=trial.foundationSamplesAbove200*.65f&&trial.maximumHeight>=450;trial.status=trial.after.passed&&trial.allProtectedSamplesExact&&trial.originalQualityLimitsPreserved&&trial.highlandMassAndPeakRetained?"BOUNDED_RETAINED_FIELDS_QUALITY_PASSED_REQUIRES_FULL_WORLD_AND_RENDER_ACCEPTANCE":"REJECTED_RETAINED_THERMAL_QUALITY_OR_PROTECTED_MASS_GUARD";
                    File.WriteAllText(Io(Path.Combine(output,"trial-"+label+".json")),JsonUtility.ToJson(trial,true));
                }
                proof.status=proof.trials.Any(t=>t.status=="BOUNDED_RETAINED_FIELDS_QUALITY_PASSED_REQUIRES_FULL_WORLD_AND_RENDER_ACCEPTANCE")?"BOUNDED_RETAINED_THERMAL_CANDIDATE_REQUIRES_SAME_FULL_WORLD_PIPELINE_AND_RENDER_ACCEPTANCE":"REJECTED_ALL_RETAINED_THERMAL_PROFILES";
            }
            finally
            {
                if(root!=null)UnityEngine.Object.DestroyImmediate(root);if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);mountain?.Dispose();mountain?.DestroyUnsavedDefinition();proof.ownedSelectionAndRandomRestored=mountain!=null&&mountain.evidence.legacySelectionRestored&&mountain.evidence.randomStateRestored;
                proof.originalSourcesExact=sources.All(p=>File.Exists(Io(p.Key))&&AssetSha(p.Key)==p.Value);proof.sourceSceneAndTerrainMemoryExact=inputMemory.All(p=>FinishFloatHash(retained[p.Key].GetHeights(0,0,513,513))==p.Value)&&originalMemory.All(p=>FinishFloatHash(p.Key.GetHeights(0,0,513,513))==p.Value)&&proof.originalSourcesExact;proof.protectedSources=sources.Select(p=>new RetainedThermalSourceFile{path=p.Key,sha256=p.Value}).ToArray();if(proof.status=="IN_PROGRESS")proof.status="REJECTED_RETAINED_THERMAL_INSPECT_NATIVE_EXCEPTION";File.WriteAllText(Io(Path.Combine(output,"retained-thermal-repair.json")),JsonUtility.ToJson(proof,true));if(!proof.originalSourcesExact||!proof.sourceSceneAndTerrainMemoryExact||!proof.ownedSelectionAndRandomRestored)throw new InvalidOperationException("Retained thermal probe changed original sources/input terrain memory or failed owned-selection restoration.");
            }
        }
        static bool RetainedThermalQualityEqual(TerrainQuality actual,TerrainQuality original)
            =>actual!=null&&original!=null&&actual.rawSteep65==original.rawSteep65&&actual.integratedSteep65==original.integratedSteep65&&actual.integratedSteep80==original.integratedSteep80&&actual.excludedHydrologyOrPortalCells==original.excludedHydrologyOrPortalCells&&actual.allowedSteep65==original.allowedSteep65&&actual.allowedSteep80==original.allowedSteep80&&actual.maximumTileSeamDelta==original.maximumTileSeamDelta&&actual.conflictingGroundCoreSamples==original.conflictingGroundCoreSamples;
    }
}
#endif
