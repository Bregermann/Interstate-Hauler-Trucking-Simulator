#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    // Read-only first stage of identity-preserving adoption. It deliberately
    // maps the ORIGINAL objects rather than replacing their prefab/GUID. A
    // mapped plan is not permission to apply a visually rejected candidate.
    public static partial class DragonKingdomWorldAdoptionPlan
    {
        const string Root="Assets/TruckDragonKingdom/";
        public const string CanonicalPrefab=Root+"Prefabs/Shared Dragon Kingdom.prefab";
        [Serializable]public sealed class Entry{public string originalPath,candidatePath,originalId,candidateId,kind;public Vector3 oldPosition,newPosition,oldScale,newScale;public Quaternion oldRotation,newRotation;}
        [Serializable]public sealed class RootChange{public string path,identity,reason,parentPath;public int objects;}
        [Serializable]public sealed class Plan
        {
            public string scope="READ_ONLY_ORIGINAL_OBJECT_ID_MAPPING_NOT_ADOPTION_OR_VISUAL_APPROVAL",tag,status="IN_PROGRESS",canonicalPrefabHash,candidatePrefabHash,canonicalGuid,candidateGuid;
            public int originalObjects,mappedOriginalObjects,ownedFinishRoots,ownedCoastalRoots,ownedArtistMarketRoots;public bool allRetainedOriginalObjectsMapped;
            public string retainedLakeRendererTreatment="Original enabled state retained";
            public Entry[] transforms;public RootChange[] obsolete,additions;public string[] retainedOriginalIds;
            public int compositionSchema,ownedArchitectureRoots,ownedMountainRoots,retainedInactiveMountainRoots;public Schema4RetiredComponent[] retiredComponents;
            public string[] requiredBeforeApplication={"Actual native image review accepted","Actual full mounted course geometry accepted","Candidate/plan hashes still exact","All original nonobsolete local IDs retained after save","Mode external controls/rules/cameras/NWH data unchanged","Existing ambience regrounded and NavMesh rebaked preserving actor/route identities"};
        }
        sealed class Mapping
        {public readonly Dictionary<UnityEngine.Object,UnityEngine.Object> candidateToOriginal=new Dictionary<UnityEngine.Object,UnityEngine.Object>();public readonly HashSet<Transform> obsolete=new HashSet<Transform>();public readonly Dictionary<Component,Component> retiredComponents=new Dictionary<Component,Component>();public Schema4RetiredComponent[] retirementEvidence;public readonly Dictionary<Transform,Transform> mountainRetirements=new Dictionary<Transform,Transform>();}
        public static Plan LastPlan{get;private set;}
        public static void CapturePlan()
        {
            string tag=Arg("-tsDragonWorldCandidate","RoundedIntegrated04");var plan=Build(tag);LastPlan=plan;
            string output=Arg("-tsDragonOutput","Logs/TruckSports/DragonKingdom");Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"identity-adoption-plan-"+tag+".json"),JsonUtility.ToJson(plan,true));
        }
        public static Plan Build(string tag)
        {
            if(string.IsNullOrEmpty(tag)||tag.Length>40||tag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new ArgumentException("Explicit actual candidate tag required.");
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");
            string folder=Root+"VisualCandidates/"+tag,path=folder+"/Shared Dragon Kingdom.prefab";
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(CanonicalPrefab);var candidate=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(original==null||candidate==null)throw new InvalidOperationException("Existing original and actual physically passed candidate required; no generation.");
            var receipt=JsonUtility.FromJson<DragonKingdomRegionalWorldIntegration.Receipt>(File.ReadAllText(folder+"/Integration Receipt.json"));
            if(receipt.status!="PHYSICAL_GATES_PASSED_REQUIRES_RENDER_AND_ACTUAL_DRIVING_REVIEW"||!receipt.sourceBytesPreserved||!receipt.originalPhysicalGatesPassed)throw new InvalidOperationException("A rejected/partial candidate cannot be mapped for adoption.");
            var beforeOriginal=Hash(CanonicalPrefab);var beforeCandidate=Hash(path);
            var ow=original.GetComponent<DragonKingdomWorld>();var cw=candidate.GetComponent<DragonKingdomWorld>();if(ow==null||cw==null||ow.regions.Length!=9||cw.regions.Length!=9)throw new InvalidOperationException("Both actual nine-region worlds required.");
            var schema4=IsSchema4(receipt);if(schema4)ValidateSchema4SavedCandidate(cw,receipt);
            var mountainRoot=ValidateOwnedMountainAdoption(ow,cw,receipt,false);
            var map=Map(ow,cw,receipt.placements,receipt);
            var all=Objects(original.transform).ToArray();var removed=new HashSet<UnityEngine.Object>(map.obsolete.SelectMany(Objects));
            var retained=all.Where(o=>!removed.Contains(o)).ToArray();var destinations=new HashSet<UnityEngine.Object>(map.candidateToOriginal.Values);
            if(retained.Any(o=>!destinations.Contains(o)))throw new InvalidOperationException("An original nonobsolete world object has no unambiguous candidate mapping.");
            var additions=candidate.GetComponentsInChildren<Transform>(true).Where(t=>!map.candidateToOriginal.ContainsKey(t)&&t.parent!=null&&map.candidateToOriginal.ContainsKey(t.parent)).ToArray();
            bool bearings=receipt.bearings!=null&&receipt.bearings.patches>0;
            var coastAdditions=additions.Where(t=>t.name=="Owned Vista coastal outskirts").ToArray();
            if(coastAdditions.Length!=0)ValidateCoastalAddition(coastAdditions,cw,receipt);
            var artistAdditions=additions.Where(t=>t.name==DragonKingdomRegionalWorldIntegration.ArtistMarketName).ToArray();
            if(artistAdditions.Length!=0){if(artistAdditions.Length!=1)throw new InvalidOperationException("Only one exact original artist marketplace group is permitted.");DragonKingdomRegionalWorldIntegration.ValidateArtistMarket(artistAdditions[0],cw,receipt);}
            var architectureAdditions=schema4?Schema4ArchitectureAdditions(additions,cw,receipt):Array.Empty<Transform>();
            var finishAdditions=additions.Where(t=>t!=mountainRoot&&!architectureAdditions.Contains(t)&&t.name!=DragonKingdomRegionalWorldIntegration.ArtistMarketName&&t.name!="Complete owned windmill house14"&&t.name!="Actual vendor Digger cavern persistence"&&t.name!="Actual vendor cavern Digger"&&t.name!="Owned measured bridge bearing foundations"&&t.name!="Owned Vista coastal outskirts").ToArray();
            if(additions.Length!=4+(mountainRoot!=null?1:0)+(bearings?1:0)+finishAdditions.Length+coastAdditions.Length+artistAdditions.Length+architectureAdditions.Length||additions.Count(t=>t.name=="Complete owned windmill house14")!=1||additions.Count(t=>t.name=="Actual vendor Digger cavern persistence")!=1||additions.Count(t=>t.name=="Actual vendor cavern Digger")!=2||additions.Count(t=>t.name=="Owned measured bridge bearing foundations")!=(bearings?1:0))throw new InvalidOperationException("Unreviewed added root outside the exact windmill/master/two-Digger-system and measured bearing contract.");
            if(finishAdditions.Length!=0)
            {
                var finish=receipt.finish;
                if(finish==null||!finish.complete||finish.failures==null||finish.failures.Count!=0||finish.waterways==null||!finish.waterways.originalHeightmapsExact||!finish.waterways.originalColliderGeometryExact)throw new InvalidOperationException("Added landmark roots require their complete actual placement/material/water preservation evidence.");
                int expected=finish.newStreetfronts+finish.refugeHouses+finish.marketStalls+finish.farmPieces+finish.harborProps+finish.smallMooredBoats+finish.underbrush+finish.mushrooms+finish.cavernCrystals+finish.basalt+finish.alpineRocks+finish.nestRoostTrees+finish.lanterns+finish.traderShips;
                bool legacy=finish.compositionSchema==0&&expected==334&&finish.traderShips==0&&artistAdditions.Length==0&&finish.artistMarket==null&&finish.mooredShip==null;
                bool current=(finish.compositionSchema==2||finish.compositionSchema==3||schema4)&&expected==335&&finish.traderShips==1&&artistAdditions.Length==1&&finish.mooredShip!=null&&finish.mooredShip.passed;
                var assemblyNames=new HashSet<string>(finish.authoredGroups?.Select(g=>g.name)??Array.Empty<string>());if(finish.compositionSchema==3||schema4){assemblyNames.Add(DragonKingdomRegionalWorldIntegration.HarborQuaysideName);assemblyNames.Add(DragonKingdomRegionalWorldIntegration.HarborCargoName);assemblyNames.Add(DragonKingdomRegionalWorldIntegration.RetainingBankName);}int additionsExpected=finish.compositionSchema==3||schema4?8:0;
                if((!legacy&&!current)||finishAdditions.Length!=expected+additionsExpected||assemblyNames.Count!=additionsExpected||assemblyNames.Any(n=>finishAdditions.Count(t=>t.name==n)!=1))throw new InvalidOperationException("Only exact legacy334/current335 ordinary roots and the separately validated artist court, plus schema3 five authored groups/quay/cargo/bank, may extend this identity-preserving plan.");
                foreach(var root in finishAdditions.Where(t=>!assemblyNames.Contains(t.name)))ValidateFinishAddition(root,cw,receipt);
                if(finish.compositionSchema==3||schema4){DragonKingdomRegionalWorldIntegration.ValidateSavedActualAuthoredGroups(cw,schema4?Schema4LegacyFinishProjection(finish):finish);DragonKingdomRegionalWorldIntegration.ValidateSavedHarborForecourt(cw,finish.harborForecourt);DragonKingdomRegionalWorldIntegration.ValidateSavedTraderCargo(cw,finish.harborCargo);DragonKingdomRegionalWorldIntegration.ValidateSavedRetainingBankArt(cw,finish.retainingBank);DragonKingdomRegionalWorldIntegration.ValidateSavedAuthoredPalette(cw,receipt.palette);}
                if(current)DragonKingdomRegionalWorldIntegration.ValidateSavedMerchantBerth(cw,receipt.berth);if(receipt.urbanBanks!=null)DragonKingdomRegionalWorldIntegration.ValidateSavedUrbanRetainingBanks(cw,receipt.urbanBanks);if(receipt.finish?.ridgeComposition!=null)DragonKingdomRegionalWorldIntegration.ValidateSavedRidgeComposition(cw,receipt.finish.ridgeComposition);
                if(current)DragonKingdomRegionalWorldIntegration.ValidateMooredShip(finishAdditions.Single(t=>t.name=="Actual owned side-moored trader ship"),cw,receipt);
            }
            if(bearings)
            {
                var proof=receipt.bearings;var root=additions.Single(t=>t.name=="Owned measured bridge bearing foundations");
                if(proof.status!="ACTUAL_STATIC_SUPPORT_CHAINS_REQUIRE_FULL_WORLD_GATES_AND_RENDER_REVIEW"||!proof.allLowerRoadBuffersClear||proof.failures.Count!=0||proof.geometry.Length!=proof.patches||root.childCount!=proof.patches||receipt.groundingReview==null||!receipt.groundingReview.passed)throw new InvalidOperationException("Every added bearing must retain its passed actual support/road/grounding evidence.");
                var models=new HashSet<string>{proof.beamModel,proof.pillarModel};var filters=root.GetComponentsInChildren<MeshFilter>(true);var colliders=root.GetComponentsInChildren<Collider>(true);
                if(filters.Length==0||filters.Any(f=>f.sharedMesh==null||!models.Contains(AssetDatabase.GetAssetPath(f.sharedMesh)))||colliders.Any(c=>!(c is MeshCollider m)||m.sharedMesh==null||!models.Contains(AssetDatabase.GetAssetPath(m.sharedMesh))))throw new InvalidOperationException("Only the exact measured visible owned beam/pillar meshes may form this new static support root.");
                int pieces=root.GetComponentsInChildren<Transform>(true).Count(t=>t.name.StartsWith("Owned bridge bearing ",StringComparison.Ordinal)||t.name.StartsWith("Owned bridge structural crossbeam ",StringComparison.Ordinal)||t.name.StartsWith("Owned grounded viaduct pier ",StringComparison.Ordinal));
                if(pieces!=proof.girders+proof.deckPlanks+proof.crossbeams+proof.piers)throw new InvalidOperationException("Saved support model count differs from measured native evidence.");
            }
            var plan=new Plan{tag=tag,canonicalPrefabHash=beforeOriginal,candidatePrefabHash=beforeCandidate,canonicalGuid=AssetDatabase.AssetPathToGUID(CanonicalPrefab),candidateGuid=AssetDatabase.AssetPathToGUID(path),originalObjects=all.Length,mappedOriginalObjects=retained.Length,ownedFinishRoots=finishAdditions.Length,ownedCoastalRoots=coastAdditions.Length,ownedArtistMarketRoots=artistAdditions.Length,allRetainedOriginalObjectsMapped=true,retainedLakeRendererTreatment=coastAdditions.Length==0?"Original enabled state retained":"Original renderer/component/mesh IDs retained; old coplanar lake strip is replaced visually by the actual connected vendor coastal sea"};
            plan.transforms=map.candidateToOriginal.Where(p=>p.Key is Transform).Select(p=>
            {var src=(Transform)p.Key;var dst=(Transform)p.Value;return new Entry{originalPath=PathOf(dst,original.transform),candidatePath=PathOf(src,candidate.transform),originalId=Identity(dst),candidateId=Identity(src),kind=Pose(dst)==Pose(src)?"Retain existing object and pose":"Retain existing object; apply reviewed pose only",oldPosition=dst.localPosition,newPosition=src.localPosition,oldScale=dst.localScale,newScale=src.localScale,oldRotation=dst.localRotation,newRotation=src.localRotation};}).ToArray();
            plan.obsolete=map.obsolete.Select(t=>new RootChange{path=PathOf(t,original.transform),identity=Identity(t),reason=t.name.StartsWith("P_BLD_windmill_sail",StringComparison.Ordinal)?"Audited unsupported standalone sail replaced by complete vendor windmill":"Audited sideways roof cluster replaced by real Digger roof",objects=Objects(t).Count()}).ToArray();
            plan.additions=additions.Select(t=>new RootChange{path=PathOf(t,candidate.transform),identity=Identity(t),parentPath=PathOf((Transform)map.candidateToOriginal[t.parent],original.transform),reason="New reviewed asset-backed subtree; remap all internal original-world references before saving",objects=Objects(t).Count()}).ToArray();
            plan.retainedOriginalIds=retained.Select(Identity).ToArray();plan.compositionSchema=receipt.finish?.compositionSchema??0;plan.ownedArchitectureRoots=architectureAdditions.Length;plan.ownedMountainRoots=mountainRoot==null?0:1;plan.retainedInactiveMountainRoots=map.mountainRetirements.Count;plan.retiredComponents=map.retirementEvidence??Array.Empty<Schema4RetiredComponent>();
            if(beforeOriginal!=Hash(CanonicalPrefab)||beforeCandidate!=Hash(path))throw new InvalidOperationException("Read-only identity planning changed an actual prefab.");
            plan.status="MAPPED_ORIGINAL_IDENTITIES_REQUIRES_VISUAL_COURSE_APPROVAL_AND_EXPLICIT_APPLICATION";return plan;
        }
        static void ValidateFinishAddition(Transform root,DragonKingdomWorld world,DragonKingdomRegionalWorldIntegration.Receipt proof)
        {
            var region=world.regions.SingleOrDefault(r=>r.root==root.parent);if(region==null)throw new InvalidOperationException("New finish root is outside its exact region: "+root.name);
            var records=proof.placements.Where(p=>p.region==region.id&&p.name==root.name).ToArray();var provenance=records.Where(p=>p.operation.StartsWith("Add complete owned art",StringComparison.Ordinal)).ToArray();
            if(provenance.Length!=1||string.IsNullOrEmpty(provenance[0].source)||!provenance[0].source.StartsWith(DragonKingdomWorldAuthoring.Private,StringComparison.Ordinal)||!proof.finish.sourcePrefabs.Contains(provenance[0].source))throw new InvalidOperationException("New finish root lacks its unique measured actual owned prefab provenance: "+root.name);
            var final=records.Last();var bounds=DragonKingdomWorldAuthoring.MeasureOwnedModelBounds(root);
            var native=IsSchema4(proof)?proof.retainedNativeBuildings.parts.SingleOrDefault(p=>p.region==region.id&&p.name==root.name&&p.source==provenance[0].source):null;
            if(native!=null?Vector3.Distance(root.position,native.after)>.002f:Vector3.Distance(root.position,final.after)>.12f||Vector3.Distance(bounds.center,final.bounds.center)>.2f||Vector3.Distance(bounds.size,final.bounds.size)>.2f)throw new InvalidOperationException("New finish art differs from its actual final measured pose/bounds: "+root.name);
            var dependencies=new HashSet<string>(AssetDatabase.GetDependencies(provenance[0].source,true),StringComparer.Ordinal);
            var meshes=root.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).Concat(root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r=>r.sharedMesh)).ToArray();
            if(meshes.Length==0||meshes.Any(m=>m==null||!dependencies.Contains(AssetDatabase.GetAssetPath(m))))throw new InvalidOperationException("New finish subtree contains unaudited/placeholder geometry rather than its actual owned source model: "+root.name);
            if(root.GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterials.Any(m=>m==null)))throw new InvalidOperationException("New finish subtree has a missing actual material: "+root.name);
        }
        static void ValidateCoastalAddition(Transform[] roots,DragonKingdomWorld world,DragonKingdomRegionalWorldIntegration.Receipt proof)
        {
            var coast=proof.outerBand;if(roots.Length!=1||coast==null||coast.status!="ATTACHED_REAL_VISTA_COAST_REQUIRES_FULL_WORLD_RENDER_AND_FLIGHT_REVIEW"||coast.outerTerrains!=12||coast.totalTerrains!=16||!coast.coreHeightmapsExact||!coast.coreColliderGeometryExact||!coast.sourceBytesPreserved||!coast.duplicateLakeRenderingReplaced||coast.seams==null||coast.seams.issues.Length!=0)throw new InvalidOperationException("Only the actual measured twelve-tile Vista band and vendor sea may extend the coastal identity plan.");
            string folder=Root+"ReviewCandidates/"+coast.tag;
            if(coast.sourcePrefab!=folder+"/Outer Vista Band.prefab"||coast.sourceReceipt!=folder+"/Outer Vista Receipt.json"||Hash(coast.sourceReceipt).Replace("-","").ToLowerInvariant()!=coast.sourceReceiptSha256.Replace("-","").ToLowerInvariant())throw new InvalidOperationException("Coast source receipt/prefab provenance changed.");
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(coast.sourcePrefab);if(source==null)throw new InvalidOperationException("Actual measured outer source prefab missing.");var sourceTerrains=source.GetComponentsInChildren<Terrain>(true);var root=roots[0];var actual=root.GetComponentsInChildren<Terrain>(true);
            if(root.parent!=world.transform||root.childCount!=13||sourceTerrains.Length!=12||actual.Length!=12||actual.Any(t=>t.GetComponent<TerrainCollider>()==null||t.GetComponent<TerrainCollider>().terrainData!=t.terrainData))throw new InvalidOperationException("Coast contains unreviewed layout/data/collision instead of exactly the actual twelve source tiles.");
            if(proof.palette==null)
            {
                if(actual.Any(t=>!sourceTerrains.Any(s=>s.terrainData==t.terrainData&&Vector3.Distance(s.transform.position,t.transform.position)<.001f)))throw new InvalidOperationException("Legacy coast must retain all twelve exact direct original TerrainData bindings.");
            }
            else
            {
                DragonKingdomRegionalWorldIntegration.ValidateSavedAuthoredPalette(world,proof.palette);
                var pairs=proof.palette.tiles.Where(t=>t.externalDataCloned).ToArray();string owned=Root+"VisualCandidates/"+proof.tag+"/Terrain/Palette/";
                if(pairs.Length!=12||pairs.Select(t=>t.sourceData).Distinct(StringComparer.Ordinal).Count()!=12||pairs.Select(t=>t.candidate).Distinct(StringComparer.Ordinal).Count()!=12)throw new InvalidOperationException("Exactly twelve distinct source-to-owned outer palette pairs required.");
                foreach(var terrain in actual)
                {
                    string path=AssetDatabase.GetAssetPath(terrain.terrainData);var pair=pairs.SingleOrDefault(p=>p.candidate==path);
                    var original=pair==null?null:sourceTerrains.SingleOrDefault(t=>AssetDatabase.GetAssetPath(t.terrainData)==pair.sourceData);
                    if(pair==null||original==null||!path.StartsWith(owned,StringComparison.Ordinal)||Vector3.Distance(original.transform.position,terrain.transform.position)>=.001f||pair.sourceDataGuid==pair.candidateGuid||AssetDatabase.AssetPathToGUID(pair.sourceData)!=pair.sourceDataGuid||AssetDatabase.AssetPathToGUID(path)!=pair.candidateGuid||!string.Equals(Hash(pair.sourceData).Replace("-",""),pair.sourceDataSha256,StringComparison.OrdinalIgnoreCase)||!string.Equals(Hash(pair.sourceData+".meta").Replace("-",""),pair.sourceDataMetaSha256,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Coastal palette clone changed its exact original source/GUID/meta or tile position.");
                }
            }
            var meshes=root.GetComponentsInChildren<MeshFilter>(true);if(meshes.Length!=1||AssetDatabase.GetAssetPath(meshes[0].sharedMesh)!=coast.seaMesh||meshes[0].sharedMesh.vertexCount!=coast.seaVertices||meshes[0].GetComponent<Renderer>().sharedMaterial!=AssetDatabase.LoadAssetAtPath<Material>(coast.seaMaterial))throw new InvalidOperationException("Coast lacks the exact real vendor-generated sea mesh/material.");
            if(proof.finish?.compositionSchema==2||proof.finish?.compositionSchema==3)
            {
                if(!coast.exactStraightSeaGeometry||coast.invalidSeaVertices!=0||coast.invertedSeaTriangles!=0)throw new InvalidOperationException("Current composition requires actual straight native sea geometry evidence.");
                var measured=new DragonKingdomRegionalWorldIntegration.CoastalBand{seaLevel=coast.seaLevel};var sea=meshes[0].transform;DragonKingdomRegionalWorldIntegration.ValidateStraightSeaGeometry(sea,meshes[0].sharedMesh,sea.GetComponent<UnityEngine.Splines.SplineContainer>().Spline.Count,measured);
                if(measured.seaRows!=coast.seaRows||measured.seaColumns!=coast.seaColumns||Mathf.Abs(measured.maximumSeaRowDrift-coast.maximumSeaRowDrift)>.001f||Mathf.Abs(measured.minimumSeaTriangleArea-coast.minimumSeaTriangleArea)>.001f)throw new InvalidOperationException("Saved sea geometry differs from actual measured native row proof.");
            }
            var neighborhood=root.GetComponents<DragonTerrainNeighborhood>();var all=world.GetComponentsInChildren<Terrain>(true);if(neighborhood.Length!=1||all.Length!=16||!new HashSet<Terrain>(neighborhood[0].terrains).SetEquals(all)||all.Any(t=>t.allowAutoConnect))throw new InvalidOperationException("Actual mixed-size neighborhood references or manual native adjacency configuration changed.");
            var core=all.Where(t=>!t.transform.IsChildOf(root)).ToArray();var seams=DragonKingdomOuterVistaAuthoring.ValidateCoreAgainstOuter(core,root.gameObject);if(seams.issues.Length!=0)throw new InvalidOperationException("Actual mapped coastal geometry no longer passes its native measured seam contract: "+string.Join(" | ",seams.issues));
            if(root.GetComponentsInChildren<MonoBehaviour>(true).Any(b=>!(b is DragonTerrainNeighborhood)&&!(b is UnityEngine.Splines.SplineContainer)&&!(b is sc.modeling.river.runtime.RiverModeler)))throw new InvalidOperationException("Unexpected generation/gameplay authority in the coastal visual addition.");
            if(world.transform.Find("Lanternwater Lake").GetComponent<Renderer>().enabled)throw new InvalidOperationException("Old coplanar transparency must be explicitly replaced while preserving its original object identity.");
        }
        static Mapping Map(DragonKingdomWorld original,DragonKingdomWorld candidate,DragonKingdomRegionalWorldIntegration.Placement[] placements,DragonKingdomRegionalWorldIntegration.Receipt schema4=null,bool allowAlreadyRetired=false)
        {
            var map=new Mapping();PairShallow(original.transform,candidate.transform,map);
            var regionRoots=new HashSet<Transform>(original.regions.Select(r=>r.root));
            foreach(Transform t in original.transform)if(!regionRoots.Contains(t))PairTree(t,FindExact(t,candidate.transform),map);
            foreach(var region in original.regions)
            {
                var target=candidate.Region(region.id).root;PairShallow(region.root,target,map);
                foreach(Transform t in region.root)
                {
                    if(region.id=="village"&&t.name.StartsWith("P_BLD_windmill_sail",StringComparison.Ordinal)||region.id=="caverns"&&t.name.StartsWith("RockFormation_01",StringComparison.Ordinal)&&Mathf.Abs(Mathf.DeltaAngle(t.localEulerAngles.x,90))<1){if(IsSchema4(schema4)&&region.id=="village")Schema4ReplacedWindmillCounterpart(t,target,schema4);map.obsolete.Add(t);continue;}
                    var records=placements.Where(p=>p.region==region.id&&p.name==t.name&&Vector3.Distance(p.before,t.position)<.12f).ToArray();
                    if(records.Length>1)throw new InvalidOperationException("Ambiguous actual placement history: "+region.id+"/"+t.name);
                    Transform match;
                    var nativeMatch=!IsSchema4(schema4)?null:Schema4RetainedCounterpart(t,region.id,target,schema4);
                    if(nativeMatch!=null)match=nativeMatch;
                    else if(records.Length==1)match=target.Cast<Transform>().SingleOrDefault(s=>s.name==t.name&&Vector3.Distance(s.position,records[0].after)<.12f);
                    else match=FindExact(t,target);
                    if(match==null)throw new InvalidOperationException("Original asset root lacks its exact reviewed counterpart: "+region.id+"/"+t.name);
                    PairTree(t,match,map);
                }
            }
            if(map.obsolete.Count!=5)throw new InvalidOperationException("Only one unsupported sail and four rejected roof clusters may become obsolete.");if(IsSchema4(schema4))AuthorizeSchema4Retirement(map,original,candidate,schema4,allowAlreadyRetired);if(schema4?.ownedMountainRoad!=null)AuthorizeOwnedMountainRetirements(map,original,candidate,schema4,allowAlreadyRetired);return map;
        }
        static Transform FindExact(Transform original,Transform candidateParent)
        {
            var matches=candidateParent.Cast<Transform>().Where(t=>t.name==original.name&&Vector3.Distance(t.localPosition,original.localPosition)<.12f&&Quaternion.Angle(t.localRotation,original.localRotation)<.05f&&Vector3.Distance(t.localScale,original.localScale)<.01f).ToArray();
            if(matches.Length!=1)throw new InvalidOperationException("Unambiguous existing root counterpart required: "+original.name+" matches="+matches.Length);return matches[0];
        }
        static void PairTree(Transform original,Transform candidate,Mapping map)
        {
            PairShallow(original,candidate,map);var children=original.Cast<Transform>().ToArray();
            foreach(var child in children)
            {
                int ordinal=children.TakeWhile(t=>t!=child).Count(t=>t.name==child.name);var matches=candidate.Cast<Transform>().Where(t=>t.name==child.name).ToArray();
                if(ordinal>=matches.Length)throw new InvalidOperationException("Existing subtree changed structurally; explicit review required: "+original.name+"/"+child.name);PairTree(child,matches[ordinal],map);
            }
        }
        static void PairShallow(Transform original,Transform candidate,Mapping map)
        {
            map.candidateToOriginal.Add(candidate.gameObject,original.gameObject);
            foreach(var group in original.GetComponents<Component>().GroupBy(c=>c.GetType()))
            {
                var a=group.ToArray();var b=candidate.GetComponents(group.Key);if(a.Length!=b.Length)throw new InvalidOperationException("Original component identity/count changed: "+original.name+"/"+group.Key.Name);
                for(int i=0;i<a.Length;i++)map.candidateToOriginal.Add(b[i],a[i]);
            }
        }
        static IEnumerable<UnityEngine.Object> Objects(Transform root){foreach(var t in root.GetComponentsInChildren<Transform>(true)){yield return t.gameObject;foreach(var c in t.GetComponents<Component>())yield return c;}}
        static string Pose(Transform t)=>t.localPosition.ToString("R")+t.localRotation.ToString("R")+t.localScale.ToString("R");
        static string Identity(UnityEngine.Object o){if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o,out string guid,out long id))return guid+":"+id;return GlobalObjectId.GetGlobalObjectIdSlow(o).ToString();}
        static string PathOf(Transform t,Transform root){if(t==root)return ".";int ordinal=t.parent==null?0:t.parent.Cast<Transform>().TakeWhile(s=>s!=t).Count(s=>s.name==t.name);string name=t.name+"["+ordinal+"]";return t.parent==root?name:PathOf(t.parent,root)+"/"+name;}
        static string Arg(string key,string fallback){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,key);return at>=0&&at+1<args.Length?args[at+1]:fallback;}
        static string Hash(string file){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(file)));}
    }
}
#endif
