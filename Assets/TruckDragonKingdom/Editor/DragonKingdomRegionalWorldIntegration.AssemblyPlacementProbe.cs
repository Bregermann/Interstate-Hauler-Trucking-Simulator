#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable] public sealed class AssemblyPlacementDiagnosis
        {
            public string scope="ACTUAL_ALL5_SOURCE_GROUPS_ON_RETAINED23_WITH_PASSED_RIDGE_AND_TEMPORARY_PROVEN_12M_BANK_NO_WORLD_REGEN_OR_ASSET_SAVE",status="IN_PROGRESS",error,cleanupError;
            public int groups,instances,acceptedGroups,physicsDescriptorRows;
            public string parityReceipt,parityReceiptSha256,parityInputManifest,parityInputManifestSha256;public int replayedSourceRoots,expectedReplaySourceRoots,verifiedReplaySourceFiles;public bool fullCompositionReplay,acceptedGroupPosesMatchFullReceipt,originalDefinitionAndTransformsRestored,ridgeReplayedFromRejectedReceipt;public BankParityTerrain[] parityTerrains;
            public bool allGroupsPlaced,sourceBaselinePrepared,sourceBytesPreserved,originalDataBindingsRestored,original172PosesRestored,originalHeightsAndPhysicsRestored,originalArtistMarketPosesRestored,remainingPlacementsPassed;
            public QuayImportEvidence importPreflight;public QuayDependencyChange[] preflightSourceChanges,unexpectedSourceChanges;
            public RidgePhysicsDifference[] physicsDifferences;public RidgeComposition ridge;public ArtistMarket recomposedMarket;public UrbanBankProof projectedBank;public RetainingBankArt retainingBank;public AuthoredGroup[] placements;public string[] failures;
        }
        [Serializable]public sealed class BankParityTerrain{public string path,guid,sha256,metaSha256,expectedHeightSha256,actualHeightSha256;public Vector3 origin;public bool heightExact;}
        [Serializable]sealed class ParitySourceRow{public string path,sha256;}
        [Serializable]sealed class ParitySourceManifest{public ParitySourceRow[] files;}
        public static AssemblyPlacementDiagnosis LastAssemblyPlacementDiagnosis{get;private set;}
        public static void CaptureActualAllFiveSourceGroupPlacements()
        {
            const string folder=Root+"VisualCandidates/RoundedIntegrated23";string preview=folder+"/DragonKingdomPreview.unity";
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists(Io(preview)))throw new InvalidOperationException("Stopped retained23 required for source-group measurement; no world regeneration.");
            string parityPath=Argument("-tsDragonBankParityReceipt",null);Receipt parity=null;
            if(!string.IsNullOrEmpty(parityPath)){parityPath=Path.GetFullPath(parityPath);parity=JsonUtility.FromJson<Receipt>(File.ReadAllText(Io(parityPath)));if(parity.tag!="RoundedIntegrated28"||parity.status!="REJECTED_CANDIDATE"||!parity.sourceBytesPreserved||parity.finish?.compositionSchema!=3||parity.finish.authoredGroups?.Length!=5||parity.finish.authoredGroups.Any(g=>!g.passed))throw new InvalidOperationException("Only exact source-preserved full28 partial native composition may provide diagnostic parity; never acceptance.");}
            string parityManifest=parity==null?null:Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(parityPath)),"input-manifest.json");
            var expectedParitySources=parity==null?new Dictionary<string,string>():JsonUtility.FromJson<ParitySourceManifest>("{\"files\":"+File.ReadAllText(Io(parityManifest))+"}").files.ToDictionary(p=>p.path,p=>p.sha256,StringComparer.Ordinal);
            string terrainWitness=parity==null?null:Path.Combine(Path.GetDirectoryName(parityManifest),"private-rejected28-core-terrain-diagnostic-only.json");
            var terrainSources=parity==null?new Dictionary<string,string>():JsonUtility.FromJson<ParitySourceManifest>(File.ReadAllText(Io(terrainWitness))).files.ToDictionary(p=>p.path,p=>p.sha256,StringComparer.Ordinal);
            var plans=ActualAuthoredGroupPlans();var layouts=plans.Select(p=>p.layout).Distinct().Select(ReadAssemblySource).ToArray();
            foreach(var row in layouts.SelectMany(p=>p.instances))AssetDatabase.LoadAssetAtPath<GameObject>(row.prefab);
            var paths=QuayProbeDependencyPaths(preview).Concat(layouts.SelectMany(p=>p.instances).SelectMany(p=>AssetDatabase.GetDependencies(p.prefab,true))).Concat(plans.Select(p=>p.layout)).SelectMany(p=>new[]{p,p+".meta"}).Distinct().Where(p=>File.Exists(Io(p))).ToArray();
            if(parity!=null)paths=paths.Concat(new[]{parityPath,parityManifest,terrainWitness,parity.finish.authoredGroups[0].inputDefinition,parity.finish.authoredGroups[0].inputDefinition+".meta"}).Concat(terrainSources.Keys).Distinct().ToArray();
            var originalHashes=paths.ToDictionary(p=>p,AssetSha);var originalPayloads=QuayImportPayloads(paths);var prior=PrepareValidQuayProbeSceneSetup();var identities=CaptureQuayProtectedImportIdentities();Dictionary<string,string> frozen=null;
            var proof=new AssemblyPlacementDiagnosis{parityReceipt=parityPath,parityReceiptSha256=parity==null?null:AssetSha(parityPath),parityInputManifest=parityManifest,parityInputManifestSha256=parity==null?null:AssetSha(parityManifest)};LastAssemblyPlacementDiagnosis=proof;LastAuthoredGroups=Array.Empty<AuthoredGroup>();var failures=new List<string>();Scene scene=default;DragonKingdomWorld world=null;string physics=null;Dictionary<string,RidgePhysicsDifference> physicsRows=null;
            DragonKingdomDefinition originalDefinition=null;var allTransformJson=new Dictionary<Transform,string>();
            var originals=new Dictionary<Terrain,TerrainData>();var colliderData=new Dictionary<TerrainCollider,TerrainData>();var heightHashes=new Dictionary<TerrainData,string>();var copies=new List<TerrainData>();var poses=new Dictionary<Transform,Vector3>();var marketPoses=new Dictionary<Transform,Tuple<Vector3,Quaternion,Vector3>>();
            try
            {
                scene=EditorSceneManager.OpenScene(preview,OpenSceneMode.Additive);world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();
                frozen=PrepareFrozenQuayGeometrySources(paths,originalHashes,originalPayloads,identities,prior,c=>proof.preflightSourceChanges=c,out var imports);proof.importPreflight=imports;proof.sourceBaselinePrepared=true;
                physics=DragonKingdomWorldVisualMigration.PhysicsAndAnchorHash(world.gameObject);physicsRows=CaptureRidgePhysicsDescriptors(world.gameObject);proof.physicsDescriptorRows=physicsRows.Count;
                originalDefinition=world.definition;foreach(var t in world.GetComponentsInChildren<Transform>(true))allTransformJson.Add(t,EditorJsonUtility.ToJson(t));
                if(parity!=null){string input=parity.finish.authoredGroups[0].inputDefinition;if(parity.finish.authoredGroups.Any(g=>g.inputDefinition!=input||AssetSha(input)!=g.inputDefinitionSha256||AssetSha(input+".meta")!=g.inputDefinitionMetaSha256||AssetDatabase.AssetPathToGUID(input)!=g.inputDefinitionGuid))throw new InvalidOperationException("Full28 input definition provenance changed.");world.definition=AssetDatabase.LoadAssetAtPath<DragonKingdomDefinition>(input);if(world.definition==null)throw new InvalidOperationException("Actual full28 input definition unavailable.");}

                var terrains=world.GetComponentsInChildren<Terrain>(true);foreach(var t in terrains){originals.Add(t,t.terrainData);if(!heightHashes.ContainsKey(t.terrainData))heightHashes.Add(t.terrainData,DragonKingdomWorldVisualMigration.HeightHash(t.terrainData));}foreach(var c in world.GetComponentsInChildren<TerrainCollider>(true))colliderData.Add(c,c.terrainData);
                var receipt=JsonUtility.FromJson<Receipt>(File.ReadAllText(Io(folder+"/Integration Receipt.json")));var records=receipt.placements.ToList();
                foreach(var row in records.Where(r=>r.name.StartsWith("Owned alpine shelter geology ",StringComparison.Ordinal)||r.name.StartsWith("Owned living cliff roost tree ",StringComparison.Ordinal)||r.name.StartsWith("Owned forest underbrush ",StringComparison.Ordinal)))
                {var t=world.Region(row.region).root.Cast<Transform>().Single(p=>p.name==row.name);poses.Add(t,t.localPosition);}if(poses.Count!=172)throw new InvalidOperationException("Exact172 source roots required.");
                // Native ridge contacts are measured against their persistent
                // source TerrainData before the disjoint village-bank projection.
                if(parity==null)proof.ridge=RecomposeExistingRidgeArt(world,Root+"VisualCandidates/ReadonlySourceGroupDiagnosis27",records,diagnoseAll:true);
                else{proof.ridge=parity.finish.ridgeComposition;proof.ridgeReplayedFromRejectedReceipt=true;proof.scope="ACTUAL_BANK_REPLAY_OF_PINNED_REJECTED_FULL28_SOURCE_POSES_INPUT_DEFINITION_AND_HASH_VERIFIED_NATIVE_HEIGHTS_NOT_WORLD_ACCEPTANCE_OR_FRESH_RIDGE_PROOF";}
                var core=terrains.Where(t=>AssetDatabase.GetAssetPath(t.terrainData).StartsWith(folder+"/Terrain/",StringComparison.Ordinal)).ToArray();if(core.Length!=4||terrains.Length!=16||colliderData.Count!=16)throw new InvalidOperationException("Exact4core/12coast retained foundation required.");var tiles=new List<TileState>();
                var terrainEvidence=new List<BankParityTerrain>();
                foreach(var t in core)
                {
                    var original=t.terrainData;var source=original;
                    if(parity!=null)
                    {
                        var expected=parity.urbanBanks.tiles.Single(v=>v.origin==t.transform.position);int x=Mathf.RoundToInt((t.transform.position.x+1600)/1600),z=Mathf.RoundToInt((t.transform.position.z+800)/1600);string path=Root+"VisualCandidates/RoundedIntegrated28/Terrain/Kingdom "+x+" "+z+".asset";
                        foreach(string protectedPath in new[]{path,path+".meta"})if(!terrainSources.TryGetValue(protectedPath,out string exact)||AssetSha(protectedPath)!=exact)throw new InvalidOperationException("Failed full28 diagnostic TerrainData serialized bytes differ from their archived native witness: "+protectedPath);
                        source=AssetDatabase.LoadAssetAtPath<TerrainData>(path);if(source==null||source.heightmapResolution!=expected.resolution||source.size!=expected.size)throw new InvalidOperationException("Actual failed full28 TerrainData unavailable/incompatible; cannot substitute old23 projection.");
                        var row=new BankParityTerrain{path=path,guid=AssetDatabase.AssetPathToGUID(path),sha256=AssetSha(path),metaSha256=AssetSha(path+".meta"),expectedHeightSha256=expected.afterHeightSha256,actualHeightSha256=DragonKingdomWorldVisualMigration.HeightHash(source),origin=t.transform.position};row.heightExact=row.actualHeightSha256==row.expectedHeightSha256;terrainEvidence.Add(row);proof.parityTerrains=terrainEvidence.ToArray();if(!row.heightExact)throw new InvalidOperationException("Failed generation disk TerrainData does NOT equal its measured in-memory full28 height witness: "+path);
                    }
                    var copy=UnityEngine.Object.Instantiate(source);copy.hideFlags=HideFlags.HideAndDontSave;copies.Add(copy);t.terrainData=copy;t.GetComponent<TerrainCollider>().terrainData=copy;int n=copy.heightmapResolution;tiles.Add(new TileState{terrain=t,original=original,candidate=copy,before=copy.GetHeights(0,0,n,n),record=new TileRecord{resolution=n},fieldTags=new byte[n,n],fieldOwners=new string[n,n]});
                }
                proof.projectedBank=parity==null?CreateMeasuredUrbanRetainingBanks(world,tiles.ToArray()):parity.urbanBanks;Physics.SyncTransforms();
                var market=world.Region("village").root.Cast<Transform>().Single(t=>t.name==ArtistMarketName);foreach(var t in market.GetComponentsInChildren<Transform>(true))marketPoses.Add(t,Tuple.Create(t.localPosition,t.localRotation,t.localScale));
                if(market.childCount!=61||market.localScale!=Vector3.one*1.75f||Quaternion.Angle(market.rotation,Quaternion.Euler(0,180,0))>.001f)throw new InvalidOperationException("Exact original61 market composition required before source-group parity measurement.");
                proof.recomposedMarket=receipt.finish.artistMarket;PlaceActualArtistMarket(world,market,proof.recomposedMarket);for(int i=0;i<61;i++){if(market.GetChild(i).name!=proof.recomposedMarket.children[i].name)throw new InvalidOperationException("Original market61 child identity changed.");proof.recomposedMarket.children[i].actualBounds=BoundsOf(market.GetChild(i));}
                if(parity!=null)
                {
                    var verifiedSources=new HashSet<string>();
                    var priorKeys=new HashSet<string>(receipt.placements.Select(p=>p.region+"\n"+p.name));
                    var currentRows=parity.placements.GroupBy(p=>p.region+"\n"+p.name).Select(g=>g.Last()).Where(p=>priorKeys.Contains(p.region+"\n"+p.name)&&p.name!=ArtistMarketName).ToArray();proof.expectedReplaySourceRoots=currentRows.Length;
                    foreach(var row in currentRows)
                    {
                        var region=world.regions.SingleOrDefault(r=>r.id==row.region);if(region==null)throw new InvalidOperationException("Full28 source region disappeared: "+row.region);var child=region.root.Cast<Transform>().SingleOrDefault(t=>t.name==row.name);if(child==null||child==market)throw new InvalidOperationException("Exact shared full28/retained23 source root disappeared: "+row.name);
                        if(!string.IsNullOrEmpty(row.source)&&row.source.StartsWith("Assets/",StringComparison.Ordinal)&&File.Exists(Io(row.source)))foreach(var path in new[]{row.source,row.source+".meta"}){if(!expectedParitySources.TryGetValue(path,out string expected)||AssetSha(path)!=expected)throw new InvalidOperationException("Actual full28 replay source bytes no longer match its frozen native input: "+path);verifiedSources.Add(path);}
                        child.position=row.after;var actual=BoundsOf(child);if(Vector3.Distance(actual.center,row.bounds.center)>.02f||Vector3.Distance(actual.size,row.bounds.size)>.02f)throw new InvalidOperationException("Full28 replay source geometry/rotation differs from exact recorded current composition: "+row.name);
                        proof.replayedSourceRoots++;
                    }
                    proof.verifiedReplaySourceFiles=verifiedSources.Count;proof.fullCompositionReplay=proof.expectedReplaySourceRoots>0&&proof.replayedSourceRoots==proof.expectedReplaySourceRoots&&verifiedSources.Count>0;Physics.SyncTransforms();if(!proof.fullCompositionReplay)throw new InvalidOperationException("Full28 replay omitted existing source roots.");
                }
                LastAuthoredGroups=Array.Empty<AuthoredGroup>();
                var finish=new LandmarkFinish();
                foreach(var plan in plans)
                {
                    try{InstallActualAuthoredGroup(world,null,plan,finish,records,configureMaterials:false);}
                    catch(Exception exception)
                    {string message=exception.Message;int data=message.IndexOf(" {\"",StringComparison.Ordinal);failures.Add(plan.name+": "+(data>=0?message.Substring(0,data):message));var failed=world.Region(plan.region).root.Cast<Transform>().SingleOrDefault(t=>t.name==plan.name);if(failed!=null)UnityEngine.Object.DestroyImmediate(failed.gameObject);Physics.SyncTransforms();}
                }
                proof.placements=LastAuthoredGroups;
                if(parity!=null){proof.acceptedGroupPosesMatchFullReceipt=proof.placements.Length==5&&proof.placements.All(p=>{var expected=parity.finish.authoredGroups.Single(g=>g.name==p.name);return p.passed&&p.trial==expected.trial&&Vector3.Distance(p.position,expected.position)<.02f&&Quaternion.Angle(p.rotation,expected.rotation)<.001f&&p.scale==expected.scale;});if(!proof.acceptedGroupPosesMatchFullReceipt)failures.Add("Actual reconstructed source groups differ from full28 accepted native poses; parity remains unverified.");}
                proof.groups=proof.placements.Length;proof.instances=proof.placements.Sum(p=>p.instances);proof.acceptedGroups=proof.placements.Count(p=>p.passed);proof.allGroupsPlaced=proof.groups==5&&proof.instances==335&&proof.acceptedGroups==5&&failures.Count==0;
                try{proof.retainingBank=InstallRetainingBankArt(world,null,finish,records,configureMaterials:false,diagnoseAll:true);}catch(Exception exception){proof.retainingBank=LastRetainingBankArt;failures.Add("Remaining24 bank placements: "+exception.Message);}proof.failures=failures.ToArray();proof.remainingPlacementsPassed=proof.allGroupsPlaced&&proof.retainingBank!=null&&proof.retainingBank.passed&&failures.Count==0;
                proof.status=proof.remainingPlacementsPassed?"ALL5_AND_REMAINING24_BANK_NATIVE_PLACEMENTS_PASSED_REQUIRES_FRESH_WORLD_AND_RENDER_REVIEW":"ALL5_AND_REMAINING24_BANK_MEASURED_WITH_UNRESOLVED_PLACEMENTS";
            }
            catch(Exception exception){proof.status="REJECTED_SOURCE_GROUP_DIAGNOSIS";proof.error=exception.ToString();proof.ridge=LastRidgeComposition;proof.placements=LastAuthoredGroups;throw;}
            finally
            {
                if(world!=null)foreach(var plan in plans){var root=world.Region(plan.region).root.Cast<Transform>().SingleOrDefault(t=>t.name==plan.name);if(root!=null)UnityEngine.Object.DestroyImmediate(root.gameObject);}
                if(world!=null){var bank=world.Region("village").root.Cast<Transform>().SingleOrDefault(t=>t.name==RetainingBankName);if(bank!=null)UnityEngine.Object.DestroyImmediate(bank.gameObject);}
                foreach(var p in poses)if(p.Key!=null&&!p.Key.localPosition.Equals(p.Value))p.Key.localPosition=p.Value;foreach(var p in marketPoses)if(p.Key!=null&&!p.Key.localPosition.Equals(p.Value.Item1))p.Key.localPosition=p.Value.Item1;
                if(world!=null&&originalDefinition!=null)world.definition=originalDefinition;
                if(parity!=null)foreach(var pair in allTransformJson)if(pair.Key!=null)EditorJsonUtility.FromJsonOverwrite(pair.Value,pair.Key);
                proof.originalDefinitionAndTransformsRestored=world!=null&&world.definition==originalDefinition&&allTransformJson.All(p=>p.Key!=null&&EditorJsonUtility.ToJson(p.Key)==p.Value);
                foreach(var p in originals)if(p.Key!=null){p.Key.terrainData=p.Value;p.Key.Flush();}foreach(var p in colliderData)if(p.Key!=null)p.Key.terrainData=p.Value;Physics.SyncTransforms();
                proof.originalArtistMarketPosesRestored=marketPoses.Count>61&&marketPoses.All(p=>p.Key!=null&&p.Key.localPosition.Equals(p.Value.Item1)&&p.Key.localRotation.Equals(p.Value.Item2)&&p.Key.localScale.Equals(p.Value.Item3));proof.originalDataBindingsRestored=originals.Count==16&&colliderData.Count==16&&originals.All(p=>p.Key!=null&&p.Key.terrainData==p.Value)&&colliderData.All(p=>p.Key!=null&&p.Key.terrainData==p.Value);proof.original172PosesRestored=poses.Count==172&&poses.All(p=>p.Key!=null&&p.Key.localPosition.Equals(p.Value));
                proof.physicsDifferences=world!=null&&physicsRows!=null?RidgePhysicsChanges(physicsRows,world.gameObject):Array.Empty<RidgePhysicsDifference>();proof.originalHeightsAndPhysicsRestored=world!=null&&physics==DragonKingdomWorldVisualMigration.PhysicsAndAnchorHash(world.gameObject)&&proof.physicsDifferences.Length==0&&heightHashes.All(p=>DragonKingdomWorldVisualMigration.HeightHash(p.Key)==p.Value);
                foreach(var copy in copies)UnityEngine.Object.DestroyImmediate(copy);
                try{if(scene.IsValid()&&scene.isLoaded&&!EditorSceneManager.CloseScene(scene,true))throw new InvalidOperationException("Owned group diagnostic scene did not close.");EditorSceneManager.RestoreSceneManagerSetup(prior);}catch(Exception exception){proof.cleanupError=exception.ToString();}
                proof.unexpectedSourceChanges=frozen==null?Array.Empty<QuayDependencyChange>():QuayDependencyChanges(frozen);proof.sourceBytesPreserved=proof.sourceBaselinePrepared&&proof.unexpectedSourceChanges.Length==0;
                Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"actual-all5-source-group-placement-diagnosis.json"),JsonUtility.ToJson(proof,true));
                if(!proof.originalDefinitionAndTransformsRestored||!proof.originalArtistMarketPosesRestored||!proof.originalDataBindingsRestored||!proof.original172PosesRestored||!proof.originalHeightsAndPhysicsRestored||!proof.sourceBytesPreserved||!string.IsNullOrEmpty(proof.cleanupError))throw new InvalidOperationException("Source-group diagnostic must restore all original data,heights,172poses,fullphysics and scene context exactly.");
            }
        }
    }
}
#endif
