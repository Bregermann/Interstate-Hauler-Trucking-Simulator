#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        public const string NativeFinishedArchitectureFlag="-tsDragonFinishedArchitecture";
        public const string NativePhysicalCrossingsFlag="-tsDragonPhysicalCrossings";
        [Serializable]public sealed class NativeRetainedInventoryRow
        {public string region,name,source,guid;public int actualRootInstanceId;public Vector3 before,after;public Quaternion rotation;public Vector3 scale;}
        [Serializable]public sealed class NativeFinishedFramePose
        {public string region,name,key;public Vector3 position,scale;public Quaternion rotation;}
        [Serializable]public sealed class NativeFinishedIntegrationEvidence
        {
            public string scope="EXPLICIT_NATIVE_FOUR_FRAME_AND_25_RETAINED_BUILDING_INTEGRATION_REQUIRES_FULL_PHYSICAL_SAVED_AND_VISUAL_ACCEPTANCE",status="IN_PROGRESS";
            public NativeRetainedInventoryRow[] retainedInventory;public NativeFinishedFramePose[] frames;
            public int retainedBuildings,sourceArchitectureFootings,totalIndividualFootings;
            public bool inventoryIdentityPreserved,measuredFramesExcludedFromLooseRegrounding,framePosesExact,retainedFinalTerrainContactsVerified;
            public Bounds originalCastleDefinitionBounds,candidateCastleDefinitionBounds;
            public FinishedArchitectureTerrainSupport actualArchitectureTerrainSupport;
        }
        sealed class NativeFinishedContext
        {
            public HashSet<Transform> frames=new HashSet<Transform>();public Dictionary<Transform,string> framePoses=new Dictionary<Transform,string>();
            public Dictionary<Transform,string> retainedPoses=new Dictionary<Transform,string>();public NativeArchitectureFooting[] footings;
        }
        static bool NativeFinishedRequested()=>Environment.GetCommandLineArgs().Contains(NativeFinishedArchitectureFlag);
        static bool NativeCrossingsRequested()=>Environment.GetCommandLineArgs().Contains(NativePhysicalCrossingsFlag);
        static IEnumerable<string> NativeFinishedProtectedSources(string witness)
        {
            yield return witness;
            foreach(string path in ArchitectureLayoutPaths)
            {
                yield return path;yield return path+".meta";
                var source=JsonUtility.FromJson<ArchitectureGeometryLayout>(File.ReadAllText(Io(path)));
                foreach(var dependency in source.dependencies){yield return dependency.path;yield return dependency.path+".meta";}
                foreach(var snapshot in source.originalMaterials)foreach(var asset in ArchitectureSnapshotPaths(snapshot))yield return asset;
                foreach(var asset in ArchitectureSnapshotPaths(source.sourceSceneSnapshot))yield return asset;
            }
        }
        static KeyValuePair<Transform,NativeRetainedInventoryRow>[] NativeRetainedSourceInventory(DragonKingdomWorld world)
        {
            // Source identities, independent of the seater's display-name filter.
            var rows=new List<KeyValuePair<Transform,NativeRetainedInventoryRow>>();
            foreach(var region in world.regions.Where(r=>r.id=="village"||r.id=="harbor"||r.id=="refuge"))foreach(Transform root in region.root)
            {
                string source=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root.gameObject);
                if(string.IsNullOrEmpty(source)||!source.StartsWith(DragonKingdomWorldAuthoring.Village+"buildings/P_BLD_house_",StringComparison.Ordinal))continue;
                rows.Add(new KeyValuePair<Transform,NativeRetainedInventoryRow>(root,new NativeRetainedInventoryRow{region=region.id,name=root.name,source=source,guid=AssetDatabase.AssetPathToGUID(source),actualRootInstanceId=root.GetInstanceID(),before=root.position,after=root.position,rotation=root.rotation,scale=root.localScale}));
            }
            if(rows.Count!=25||rows.Select(p=>p.Key).Distinct().Count()!=25||rows.Count(p=>p.Value.region=="village")!=13||rows.Count(p=>p.Value.region=="harbor")!=8||rows.Count(p=>p.Value.region=="refuge")!=4||rows.Any(p=>string.IsNullOrEmpty(p.Value.guid)))throw new InvalidOperationException("Actual complete retained source inventory must be village13/harbor8/refuge4 with25 distinct native roots; no name-based omission or guessed count.");
            return rows.ToArray();
        }
        // Names and source-prefab GUIDs repeat legitimately in the harbor.
        // Bind each live placement to its independently inventoried native root,
        // while retaining source, authored pose, region and exact cardinality.
        static bool NativeRetainedFootingsMatch(NativeRetainedInventoryRow[] inventory,NativeSettlementFootingEvidence retained)
        {
            return inventory!=null&&inventory.Length==25&&inventory.All(p=>p!=null&&p.actualRootInstanceId!=0)&&inventory.Select(p=>p.actualRootInstanceId).Distinct().Count()==25&&retained!=null&&retained.buildings==25&&retained.parts!=null&&retained.parts.Length==25&&retained.parts.All(p=>p!=null&&p.actualRootInstanceId!=0)&&retained.parts.Select(p=>p.actualRootInstanceId).Distinct().Count()==25&&inventory.All(p=>retained.parts.Count(r=>r.actualRootInstanceId==p.actualRootInstanceId&&r.region==p.region&&r.name==p.name&&r.source==p.source&&r.sourceGuid==p.guid&&r.before==p.before&&r.rotation==p.rotation&&r.scale==p.scale)==1);
        }
        static NativeFinishedContext PrepareNativeFinishedIntegration(DragonKingdomWorld world,string folder,string witness,Receipt receipt,OwnedDefinitionProvenance definitionProvenance=null)
        {
            var proof=receipt.nativeArchitectureIntegration=new NativeFinishedIntegrationEvidence{originalCastleDefinitionBounds=world.definition.FindRegion("castle").bounds};
            var inventory=NativeRetainedSourceInventory(world);proof.retainedInventory=inventory.Select(p=>p.Value).ToArray();proof.retainedBuildings=inventory.Length;
            NativeSettlementFootingEvidence retained;var priorRetained=LastNativeSettlementFootings;
            try{retained=receipt.retainedNativeBuildings=SeatRetainedBuildingsForNativeRelief(world,Path.Combine(Output(),"actual-retained-building-base-"+receipt.tag+".json"),definitionProvenance);}
            catch{if(!ReferenceEquals(priorRetained,LastNativeSettlementFootings))receipt.retainedNativeBuildings=LastNativeSettlementFootings;throw;}
            var after=NativeRetainedSourceInventory(world);proof.inventoryIdentityPreserved=inventory.Select(p=>p.Key).ToHashSet().SetEquals(after.Select(p=>p.Key))&&inventory.All(p=>after.Any(a=>a.Key==p.Key&&a.Value.source==p.Value.source&&a.Value.guid==p.Value.guid))&&NativeRetainedFootingsMatch(inventory.Select(p=>p.Value).ToArray(),retained)&&inventory.All(p=>retained.parts.Single(r=>r.actualRootInstanceId==p.Value.actualRootInstanceId).after==p.Key.position);
            if(!proof.inventoryIdentityPreserved)throw new InvalidOperationException("Native seater did not account for every independently inventoried source building exactly once.");
            foreach(var item in inventory)item.Value.after=item.Key.position;
            var retired=RetireMeasuredCastlePrototypeShell(world,witness);
            FinishedArchitectureComposition architecture;var priorArchitecture=LastFinishedArchitectureComposition;
            try{architecture=receipt.finishedArchitecture=ComposeMeasuredFinishedArchitecture(world,folder,witness,true);}
            catch{if(!ReferenceEquals(priorArchitecture,LastFinishedArchitectureComposition))receipt.finishedArchitecture=LastFinishedArchitectureComposition;throw;}
            architecture.castlePrototypeRetirement=retired;architecture.prototypeRetirementComplete=retired.exactNativeComponentBoundary&&retired.allPerchReferencesAndWorldPosesExact&&retired.sourceBytesPreserved;
            if(architecture.groups.Length!=4||architecture.instances!=678||architecture.footings!=525||!architecture.prototypeRetirementComplete)throw new InvalidOperationException("Complete measured four-frame architecture/525 individual footings and exact prototype retirement required.");
            var context=new NativeFinishedContext{footings=retained.physicalFootings.Concat(architecture.physicalFootings).ToArray()};
            foreach(var group in architecture.groups)
            {
                var frame=world.Region(group.region).root.Cast<Transform>().Single(t=>t.name==group.name);
                context.frames.Add(frame);context.framePoses.Add(frame,Pose(frame));
            }
            if(context.frames.Count!=4)throw new InvalidOperationException("Four exact direct-child measured frame identities required before loose scenery re-grounding.");
            foreach(var item in inventory)context.retainedPoses.Add(item.Key,Pose(item.Key));
            proof.frames=architecture.groups.Select(g=>new NativeFinishedFramePose{region=g.region,name=g.name,key=g.key,position=g.position,rotation=g.rotation,scale=g.scale}).ToArray();
            proof.sourceArchitectureFootings=architecture.physicalFootings.Length;proof.totalIndividualFootings=context.footings.Length;
            if(proof.totalIndividualFootings!=550||context.footings.Select(f=>f.id).Distinct().Count()!=550)throw new InvalidOperationException("Actual525 architecture plus25 retained individual parcels must be distinct, with no frame/boat terrain pads.");
            proof.status="ACTUAL_COMPLETE_FRAMES_AND_RETAINED_SOURCE_INVENTORY_PREPARED_REQUIRES_TERRAIN_CONTACTS";return context;
        }
        static void ValidateNativeFinishedFrameAndBuildingPoses(DragonKingdomWorld world,NativeFinishedContext context,Receipt receipt)
        {
            var proof=receipt.nativeArchitectureIntegration;
            proof.framePosesExact=context.framePoses.All(p=>p.Key!=null&&Pose(p.Key)==p.Value);
            if(!proof.framePosesExact||context.retainedPoses.Any(p=>p.Key==null||Pose(p.Key)!=p.Value))throw new InvalidOperationException("Measured complete frame or retained building moved after native coordination/re-grounding.");
            ValidateRetainedNativeBuildingTerrainContacts(world,receipt.retainedNativeBuildings);proof.retainedFinalTerrainContactsVerified=receipt.retainedNativeBuildings.allNativeTerrainContactsVerified;
            if(!proof.retainedFinalTerrainContactsVerified)throw new InvalidOperationException("Actual complete retained-building contacts are not verified.");
        }
        static void CompleteNativeFinishedTerrainContacts(DragonKingdomWorld world,string folder,Receipt receipt)
        {
            var proof=receipt.nativeArchitectureIntegration;
            // Contact receipts record actual TerrainData file hashes. Persist
            // only the four owned candidate fields after final palette work,
            // before measuring; never use stale pre-coordination disk bytes.
            var fields=NativeFinishedOwnedCoreFields(world,folder);
            foreach(var terrain in fields){terrain.Flush();AssetDatabase.SaveAssetIfDirty(terrain.terrainData);}Physics.SyncTransforms();
            var support=ValidateFinishedArchitectureTerrainContacts(world,receipt.finishedArchitecture);proof.actualArchitectureTerrainSupport=support;
            receipt.finishedArchitecture.terrainSupportVerified=support.passed&&support.nativeColliderRaysPerformed&&support.parcels==525;
            if(!receipt.finishedArchitecture.terrainSupportVerified)throw new InvalidOperationException("Actual independent terrain/collider contact proof for525 architecture parcels required.");
        }
        internal static Terrain[] NativeFinishedOwnedCoreFields(DragonKingdomWorld world,string folder)
        {
            var outer=world.transform.Find("Owned Vista coastal outskirts");var fields=world.GetComponentsInChildren<Terrain>(true).Where(t=>t.terrainData!=null&&(outer==null||!t.transform.IsChildOf(outer))&&AssetDatabase.GetAssetPath(t.terrainData).StartsWith(folder+"/Terrain/",StringComparison.Ordinal)).ToArray();
            if(fields.Length!=4||fields.Select(t=>t.terrainData).Distinct().Count()!=4)throw new InvalidOperationException("Exactly four distinct owned coordinated CORE fields required; the twelve separately owned coastal fields cannot masquerade as core terrain.");return fields;
        }
        [Serializable]public sealed class NativeCoreContactProbe
        {public int totalTerrains,selectedCoreTerrains,excludedCoastTerrains,cavernMaterialCoreTerrains;public bool cavernMaterialCoreSelectionExact;public string[] coreData,cavernMaterialCoreData;public FinishedArchitectureTerrainSupport support;}
        static NativeCoreContactProbe MeasureActualRetainedFinalArchitectureContacts(DragonKingdomWorld world,string folder,Receipt receipt)
        {
            var all=world.GetComponentsInChildren<Terrain>(true);var selected=NativeFinishedOwnedCoreFields(world,folder);var materialCore=DragonKingdomRoadSurfaceProbe.OwnedCavernMaterialCoreFields(world,folder);var outer=world.transform.Find("Owned Vista coastal outskirts");var proof=new NativeCoreContactProbe{totalTerrains=all.Length,selectedCoreTerrains=selected.Length,excludedCoastTerrains=outer==null?0:outer.GetComponentsInChildren<Terrain>(true).Length,coreData=selected.Select(t=>AssetDatabase.GetAssetPath(t.terrainData)).ToArray(),cavernMaterialCoreTerrains=materialCore.Length,cavernMaterialCoreData=materialCore.Select(t=>AssetDatabase.GetAssetPath(t.terrainData)).ToArray(),cavernMaterialCoreSelectionExact=selected.SequenceEqual(materialCore)};if(!proof.cavernMaterialCoreSelectionExact)throw new InvalidOperationException("Cavern material setup must use the same four owned core fields, excluding the coastal palette copies.");proof.support=ValidateFinishedArchitectureTerrainContacts(world,receipt.finishedArchitecture);return proof;
        }
        static void ExpandNativeFinishedCastleDefinition(DragonKingdomDefinition definition,Receipt receipt)
        {
            var original=receipt.nativeArchitectureIntegration.originalCastleDefinitionBounds;
            original.Encapsulate(receipt.finishedArchitecture.groups.Single(g=>g.key=="fortified-quarter").declaredCompositionParcel);
            definition.FindRegion("castle").bounds=original;receipt.nativeArchitectureIntegration.candidateCastleDefinitionBounds=original;
        }
    }
}
#endif
