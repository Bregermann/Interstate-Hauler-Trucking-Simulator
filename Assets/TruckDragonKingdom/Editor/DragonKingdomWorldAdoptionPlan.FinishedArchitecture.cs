#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomWorldAdoptionPlan
    {
        [Serializable]public sealed class Schema4RetiredComponent
        {public string originalId,candidateId,originalPath,candidatePath,type,sourceGuid,sourceLocalId,sourceJsonSha256;public bool originalEnabled,candidateEnabled;}
        static bool IsSchema4(DragonKingdomRegionalWorldIntegration.Receipt receipt)=>receipt?.finish?.compositionSchema==4;
        static void ValidateSchema4SavedCandidate(DragonKingdomWorld candidate,DragonKingdomRegionalWorldIntegration.Receipt receipt)
        {
            if(!IsSchema4(receipt)||receipt.nativeArchitectureIntegration==null||!receipt.nativeArchitectureIntegration.inventoryIdentityPreserved||!receipt.nativeArchitectureIntegration.framePosesExact||!receipt.nativeArchitectureIntegration.measuredFramesExcludedFromLooseRegrounding||!receipt.nativeArchitectureIntegration.retainedFinalTerrainContactsVerified||receipt.nativeArchitectureIntegration.retainedBuildings!=25||receipt.nativeArchitectureIntegration.totalIndividualFootings!=550||receipt.nativeArchitectureIntegration.actualArchitectureTerrainSupport==null||!receipt.nativeArchitectureIntegration.actualArchitectureTerrainSupport.passed||!receipt.nativeArchitectureIntegration.actualArchitectureTerrainSupport.nativeColliderRaysPerformed||receipt.nativeArchitectureIntegration.actualArchitectureTerrainSupport.parcels!=525)
                throw new InvalidOperationException("Schema4 identity planning requires actual full native frame,25-building and525-source-footing support evidence.");
            DragonKingdomRegionalWorldIntegration.ValidateSavedFinishedArchitecture(candidate,receipt.finishedArchitecture,receipt.retainedNativeBuildings,receipt.nativeRoadRelief);
        }
        static Transform[] Schema4ArchitectureAdditions(Transform[] additions,DragonKingdomWorld world,DragonKingdomRegionalWorldIntegration.Receipt receipt)
        {
            var groups=receipt.finishedArchitecture.groups;var names=new HashSet<string>(groups.Select(g=>g.name)){"Finished source fishing supports and gangway"};var actual=additions.Where(t=>names.Contains(t.name)).ToArray();
            if(names.Count!=5||actual.Length!=5||actual.Select(t=>t.name).Distinct().Count()!=5||groups.Any(g=>actual.Count(t=>t.name==g.name&&t.parent==world.Region(g.region).root)!=1)||actual.Count(t=>t.name=="Finished source fishing supports and gangway"&&t.parent==world.Region("harbor").root)!=1)throw new InvalidOperationException("Schema4 may add only the exact four complete source frames and physically verified waterfront support root.");return actual;
        }
        static DragonKingdomRegionalWorldIntegration.LandmarkFinish Schema4LegacyFinishProjection(DragonKingdomRegionalWorldIntegration.LandmarkFinish finish)
        {
            // Existing schema3 validator remains the sole authority for its335
            // source instances. A narrow view changes no saved receipt or rules.
            return new DragonKingdomRegionalWorldIntegration.LandmarkFinish{compositionSchema=3,authoredGroups=finish.authoredGroups,assemblyMaterials=finish.assemblyMaterials};
        }
        static Transform Schema4RetainedCounterpart(Transform original,string region,Transform candidateRegion,DragonKingdomRegionalWorldIntegration.Receipt receipt)
        {
            var records=receipt.retainedNativeBuildings?.parts;if(records==null)throw new InvalidOperationException("Schema4 original house mapping requires actual native source correspondence records.");
            var identities=new HashSet<string>();UnityEngine.Object value=original;for(int i=0;value!=null&&i<32;i++){if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value,out string guid,out long id))identities.Add(guid+":"+id);var next=PrefabUtility.GetCorrespondingObjectFromSource(value);if(next==value)break;value=next;}
            var sourceRows=records.Where(p=>p.region==region&&p.name==original.name&&p.nativeRootCorrespondence!=null&&p.nativeRootCorrespondence.Any(id=>identities.Contains(id.guid+":"+id.localId))).ToArray();
            if(sourceRows.Length==0)return null;
            // Native footings capture AFTER the earlier dry-foundation pass.
            // That pass may move the pivot horizontally (actual house01 uses
            // golden-angle trial67). Bind RAW original -> exact recorded dry
            // placement -> native before, never equate these distinct stages.
            const string operation="Ground the retained actual building mesh on its dry measured foundation near the original streetscape hint; exclude but retain steam effects";
            var placements=(receipt.placements??Array.Empty<DragonKingdomRegionalWorldIntegration.Placement>()).Where(p=>p.region==region&&p.name==original.name&&p.operation==operation&&string.IsNullOrEmpty(p.source)&&p.before.Equals(original.position)).ToArray();
            if(placements.Length!=1)throw new InvalidOperationException("Ambiguous or changed exact original house placement chain: "+region+"/"+original.name+" raw placements="+placements.Length);
            var placement=placements[0];var rows=sourceRows.Where(p=>p.before.Equals(placement.after)&&p.rotation.Equals(original.rotation)&&p.scale.Equals(original.localScale)).ToArray();if(rows.Length!=1)throw new InvalidOperationException("Ambiguous or changed exact original house correspondence after measured placement: "+region+"/"+original.name+" native rows="+rows.Length);
            var row=rows[0];if(AssetDatabase.AssetPathToGUID(row.source)!=row.sourceGuid)throw new InvalidOperationException("Original native house source GUID drifted.");
            var actual=candidateRegion.Cast<Transform>().Where(t=>t.name==row.name&&Vector3.Distance(t.position,row.after)<.002f&&PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)==row.source).ToArray();if(actual.Length!=1)throw new InvalidOperationException("Original native house has no unique measured saved counterpart: "+row.name);return actual[0];
        }
        static Transform Schema4ReplacedWindmillCounterpart(Transform originalSail,Transform candidateRegion,DragonKingdomRegionalWorldIntegration.Receipt receipt)
        {
            // The eleven baseline buildings contain TEN retained original
            // house identities and ONE explicit sail-to-building replacement.
            // Never invent an eleventh retained original house identity.
            const string name="Complete owned windmill house14",replacementOperation="Replace unsupported sail with complete same-vendor animated building",dryOperation="Ground the retained actual building mesh on its dry measured foundation near the original streetscape hint; exclude but retain steam effects";
            string sailSource=DragonKingdomWorldAuthoring.Village+"buildings_modules/P_BLD_windmill_sail.prefab",buildingSource=DragonKingdomWorldAuthoring.Village+"buildings/P_BLD_house_14.prefab";
            if(originalSail==null||originalSail.name!="P_BLD_windmill_sail"||PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(originalSail.gameObject)!=sailSource||candidateRegion==null)throw new InvalidOperationException("Ambiguous or changed exact original standalone windmill source.");
            var placements=receipt.placements??Array.Empty<DragonKingdomRegionalWorldIntegration.Placement>();var replacements=placements.Where(p=>p.region=="village"&&p.name==name&&p.operation==replacementOperation&&string.IsNullOrEmpty(p.source)&&p.before.Equals(originalSail.position)).ToArray();
            if(replacements.Length!=1)throw new InvalidOperationException("Ambiguous or changed original sail-to-windmill replacement chain.");
            var dry=placements.Where(p=>p.region=="village"&&p.name==name&&p.operation==dryOperation&&string.IsNullOrEmpty(p.source)&&p.before.Equals(replacements[0].after)).ToArray();if(dry.Length!=1)throw new InvalidOperationException("Ambiguous or changed windmill replacement-to-dry-placement chain.");
            var rows=(receipt.retainedNativeBuildings?.parts??Array.Empty<DragonKingdomRegionalWorldIntegration.NativeSettlementBuildingFooting>()).Where(p=>p.region=="village"&&p.name==name&&p.source==buildingSource&&p.before.Equals(dry[0].after)).ToArray();if(rows.Length!=1)throw new InvalidOperationException("Ambiguous or changed windmill dry-placement-to-native chain.");var row=rows[0];
            string guid=AssetDatabase.AssetPathToGUID(buildingSource);if(row.sourceGuid!=guid||!string.Equals(Hash(buildingSource).Replace("-",""),row.sourceSha256,StringComparison.OrdinalIgnoreCase)||!string.Equals(Hash(buildingSource+".meta").Replace("-",""),row.sourceMetaSha256,StringComparison.OrdinalIgnoreCase)||!row.sourceGeometryAndScalePreserved||row.nativeRootCorrespondence==null||!row.nativeRootCorrespondence.Any(id=>id.guid==guid))throw new InvalidOperationException("Ambiguous or changed windmill native source/geometry identity.");
            var actual=candidateRegion.Cast<Transform>().Where(t=>t.name==name&&Vector3.Distance(t.position,row.after)<.002f&&t.rotation.Equals(row.rotation)&&t.localScale.Equals(row.scale)&&PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)==buildingSource).ToArray();if(actual.Length!=1)throw new InvalidOperationException("Ambiguous or changed actual saved windmill replacement pose/source.");return actual[0];
        }
        static void AuthorizeSchema4Retirement(Mapping map,DragonKingdomWorld original,DragonKingdomWorld candidate,DragonKingdomRegionalWorldIntegration.Receipt receipt,bool allowAlreadyRetired)
        {
            var bindings=DragonKingdomRegionalWorldIntegration.ValidateSavedCastlePrototypeRetirementBindings(original,candidate,receipt.finishedArchitecture,allowAlreadyRetired);var rows=new List<Schema4RetiredComponent>();
            foreach(var binding in bindings)
            {
                if(!map.candidateToOriginal.TryGetValue(binding.candidate,out var mapped)||mapped!=binding.original)throw new InvalidOperationException("Exact26 retired components must remain in the original identity-preserving object map.");map.retiredComponents.Add(binding.candidate,binding.original);
                rows.Add(new Schema4RetiredComponent{originalId=Identity(binding.original),candidateId=Identity(binding.candidate),originalPath=PathOf(binding.original.transform,original.transform),candidatePath=PathOf(binding.candidate.transform,candidate.transform),type=binding.proof.type,sourceGuid=binding.proof.guid,sourceLocalId=binding.proof.localId,sourceJsonSha256=binding.proof.sourceJsonSha256,originalEnabled=binding.originalEnabled,candidateEnabled=false});
            }
            map.retirementEvidence=rows.ToArray();if(map.retiredComponents.Count!=26)throw new InvalidOperationException("Only exact26 preserved component IDs may authorize disabled states.");
        }
        public static Schema4RetiredComponent[] ValidateSchema4ComponentIdentityMapProbe(DragonKingdomWorld original,DragonKingdomWorld candidate,DragonKingdomRegionalWorldIntegration.FinishedArchitectureComposition proof)
        {
            // Actual source clone only: this narrow probe establishes mapping,
            // never creates a passing full saved-world or visual receipt.
            var map=Map(original,candidate,Array.Empty<DragonKingdomRegionalWorldIntegration.Placement>());AuthorizeSchema4Retirement(map,original,candidate,new DragonKingdomRegionalWorldIntegration.Receipt{finishedArchitecture=proof},false);
            var obsolete=new HashSet<UnityEngine.Object>(map.obsolete.SelectMany(Objects));var retained=Objects(original.transform).Where(o=>!obsolete.Contains(o)).ToArray();var destinations=new HashSet<UnityEngine.Object>(map.candidateToOriginal.Values);if(retained.Any(o=>!destinations.Contains(o)))throw new InvalidOperationException("An unrelated original nonobsolete object disappeared from actual component identity mapping.");return map.retirementEvidence;
        }
    }
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        public sealed class Schema4CastleComponentBinding
        {public Component original,candidate;public FinishedCastleRetiredComponent proof;public bool originalEnabled;}
        public static Schema4CastleComponentBinding[] ValidateSavedCastlePrototypeRetirementBindings(DragonKingdomWorld original,DragonKingdomWorld candidate,FinishedArchitectureComposition proof,bool allowAlreadyRetired=false)
        {
            const string guid="7cc05905626c7234289a4d2e3eaa9869",sha="820c9dc867faac9243579acce615b7209e5fd57fdf5b1f5e45974819c14e1744",tower="5c2734c723764924ebd0458ab8293423";
            var retirement=proof?.castlePrototypeRetirement;if(original==null||candidate==null||retirement==null||!proof.prototypeRetirementComplete||retirement.source!=Prefab||retirement.sourceSha256!=sha||!retirement.exactNativeComponentBoundary||!retirement.sourceBytesPreserved||!retirement.allPerchReferencesAndWorldPosesExact||retirement.disabledComponents!=26||retirement.retainedTowerComponents!=8||retirement.perches!=4||retirement.retired==null||retirement.retired.Length!=26||retirement.retired.Select(p=>p.guid+":"+p.localId).Distinct().Count()!=26||(!allowAlreadyRetired&&AssetSha(Prefab)!=sha)||AssetSha(proof.geometryWitness)!=proof.geometryWitnessSha256)throw new InvalidOperationException("Exact original26 preserved renderer/collider component identities and native castle boundary required; no whole-object retirement.");
            var geometry=JsonUtility.FromJson<ArchitectureGeometryReceipt>(File.ReadAllText(Io(proof.geometryWitness)));var boundary=geometry.proposedPrototypeBoundaries.Single(b=>b.prefab==Prefab&&b.prefabGuid==guid&&b.prefabSha256==sha&&b.rootTransform.localId=="1276675040747331789"&&b.rootGameObject.localId=="7910969967957938101");if(boundary.rendererAndColliderComponents.Length!=34||boundary.incomingReferences.Length!=5)throw new InvalidOperationException("Exact original34-component and5-reference native castle boundary required.");
            var canonical=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);var sourceRoot=canonical.GetComponentsInChildren<Transform>(true).Single(t=>SourceIdentityMatches(t,guid,boundary.rootTransform.localId));string sourcePath=AnimationUtility.CalculateTransformPath(sourceRoot,canonical.transform);
            var oldRoot=original.GetComponentsInChildren<Transform>(true).Single(t=>AnimationUtility.CalculateTransformPath(t,original.transform)==sourcePath);var newRoot=candidate.GetComponentsInChildren<Transform>(true).Single(t=>AnimationUtility.CalculateTransformPath(t,candidate.transform)==sourcePath);
            // Validation reads saved assets and plain previews as well as native
            // instances. Existing authoring calls keep requireDisposable=true;
            // only this read-only exact boundary mapping permits saved targets.
            Component Paired(Transform target,UnityEngine.Object source)=>(Component)PairSourceAssemblyObject(sourceRoot.gameObject,target.gameObject,source,out _,false);
            var oldComponents=oldRoot.GetComponentsInChildren<Component>(true).Where(c=>c is Renderer||c is Collider).ToArray();var newComponents=newRoot.GetComponentsInChildren<Component>(true).Where(c=>c is Renderer||c is Collider).ToArray();if(oldComponents.Length!=34||newComponents.Length!=34)throw new InvalidOperationException("Schema4 must preserve every original castle component, never remove or substitute it.");
            bool Enabled(Component c)=>c is Renderer r?r.enabled:((Collider)c).enabled;var bindings=new List<Schema4CastleComponentBinding>();var retained=new List<Component>();
            foreach(var row in boundary.rendererAndColliderComponents)
            {
                var sourceComponent=sourceRoot.GetComponentsInChildren<Component>(true).Single(c=>c!=null&&c.GetType().FullName==row.type&&SourceIdentityMatches(c,guid,row.localId));var old=Paired(oldRoot,sourceComponent);var current=Paired(newRoot,sourceComponent);if(PathOf(old.transform,oldRoot)!=PathOf(current.transform,newRoot)||old.GetType()!=current.GetType())throw new InvalidOperationException("Original castle component hierarchy/type changed.");
                var mesh=old.GetComponent<MeshFilter>()?.sharedMesh;bool keep=mesh!=null&&AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh,out string meshGuid,out long _)&&meshGuid==tower;
                if(keep){if(!Enabled(old)||!Enabled(current))throw new InvalidOperationException("Original physical perch-support towers must remain enabled.");retained.Add(current);continue;}
                var explicitRow=retirement.retired.Single(p=>p.guid==guid&&p.localId==row.localId&&p.type==row.type);if(explicitRow.path!=row.path||explicitRow.sourceJsonSha256!=row.serializedJsonSha256||!explicitRow.beforeEnabled||explicitRow.afterEnabled||Enabled(current)||(!allowAlreadyRetired&&!Enabled(old)))throw new InvalidOperationException("Unreviewed component state or original identity outside exact26 castle retirement.");bindings.Add(new Schema4CastleComponentBinding{original=old,candidate=current,proof=explicitRow,originalEnabled=Enabled(old)});
            }
            if(bindings.Count!=26||retained.Count!=8||original.Region("castle").perches.Length!=4||candidate.Region("castle").perches.Length!=4)throw new InvalidOperationException("Exact26 obsolete states and8 retained physical tower components required.");
            for(int i=0;i<4;i++)
            {
                var a=original.Region("castle").perches[i];var b=candidate.Region("castle").perches[i];if(a==null||b==null||PathOf(a,oldRoot)!=PathOf(b,newRoot)||Pose(a)!=Pose(b)||!retained.OfType<Collider>().Any(c=>b.IsChildOf(c.transform)))throw new InvalidOperationException("Every original perch reference, pose and physical support must be preserved.");
            }
            var oldTransforms=oldRoot.GetComponentsInChildren<Transform>(true);var newTransforms=newRoot.GetComponentsInChildren<Transform>(true);if(oldTransforms.Length!=newTransforms.Length)throw new InvalidOperationException("Original castle hierarchy must remain complete.");
            for(int i=0;i<oldTransforms.Length;i++)if(PathOf(oldTransforms[i],oldRoot)!=PathOf(newTransforms[i],newRoot)||Pose(oldTransforms[i])!=Pose(newTransforms[i])||oldTransforms[i].gameObject.activeSelf!=newTransforms[i].gameObject.activeSelf)throw new InvalidOperationException("Unreviewed original castle transform/active-state change.");
            var oldGeometry=RidgeGeometryBindings(oldRoot,new Dictionary<string,string>());var newGeometry=RidgeGeometryBindings(newRoot,new Dictionary<string,string>());var geometryComponents=oldRoot.GetComponentsInChildren<Component>(true).Where(c=>c is MeshFilter||c is Collider).ToArray();if(oldGeometry.Length!=newGeometry.Length||oldGeometry.Length!=geometryComponents.Length)throw new InvalidOperationException("Original castle mesh/collider identity inventory changed.");
            for(int i=0;i<oldGeometry.Length;i++)
            {
                var a=oldGeometry[i];var b=newGeometry[i];var matrices=a.rootMatrix;var measured=b.rootMatrix;a.rootMatrix=b.rootMatrix=null;
                if(geometryComponents[i] is Collider&&bindings.Any(p=>p.original==geometryComponents[i]))a.enabled=false;
                if(geometryComponents[i] is MeshFilter filter&&bindings.Any(p=>p.original is Renderer&&p.original.transform==filter.transform))a.primaryLod=false;
                if(JsonUtility.ToJson(a)!=JsonUtility.ToJson(b)||matrices==null||measured==null||matrices.Length!=16||measured.Length!=16||Enumerable.Range(0,16).Any(index=>Mathf.Abs(matrices[index]-measured[index])>.0001f))throw new InvalidOperationException("Unreviewed original castle mesh, collider shape/material or relative geometry change.");
            }
            return bindings.ToArray();
        }
        public static string CaptureSchema4CastleComponentMapProbe()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonArchitectureGeometry");if(at<0||at+1>=args.Length)throw new InvalidOperationException("Actual native castle source geometry receipt required.");string path=args[at+1],before=AssetSha(Prefab);var source=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();GameObject clone=null;
            try
            {
                clone=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);var world=clone.GetComponent<DragonKingdomWorld>();var retired=RetireMeasuredCastlePrototypeShell(world,path);var proof=new FinishedArchitectureComposition{geometryWitness=path,geometryWitnessSha256=AssetSha(path),castlePrototypeRetirement=retired,prototypeRetirementComplete=true};var actual=ValidateSavedCastlePrototypeRetirementBindings(source.GetComponent<DragonKingdomWorld>(),world,proof);var identityMap=DragonKingdomWorldAdoptionPlan.ValidateSchema4ComponentIdentityMapProbe(source.GetComponent<DragonKingdomWorld>(),world,proof);if(actual.Length!=26||identityMap.Length!=26||identityMap.Select(p=>p.originalId).Distinct().Count()!=26)throw new InvalidOperationException("Actual component-map probe failed.");
                int rejected=0;var first=retired.retired[0];string id=first.localId;bool state=first.afterEnabled;
                try{first.localId=retired.retired[1].localId;try{ValidateSavedCastlePrototypeRetirementBindings(source.GetComponent<DragonKingdomWorld>(),world,proof);}catch(InvalidOperationException){rejected++;}}finally{first.localId=id;}
                try{first.afterEnabled=true;try{ValidateSavedCastlePrototypeRetirementBindings(source.GetComponent<DragonKingdomWorld>(),world,proof);}catch(InvalidOperationException){rejected++;}}finally{first.afterEnabled=state;}
                var collider=actual.Select(p=>p.candidate).OfType<Collider>().First();collider.enabled=true;try{try{ValidateSavedCastlePrototypeRetirementBindings(source.GetComponent<DragonKingdomWorld>(),world,proof);}catch(InvalidOperationException){rejected++;}}finally{collider.enabled=false;}
                if(rejected!=3||AssetSha(Prefab)!=before)throw new InvalidOperationException("Actual component-map fault rejection/source preservation failed.");return JsonUtility.ToJson(new Schema4ComponentProbe{retainedDisabledComponentIds=actual.Length,retainedEnabledTowerComponents=8,originalPerches=4,actualFaultsRejected=rejected,sourceBytesPreserved=true,noCanonicalAdoptionPerformed=true,sourceLocalIds=actual.Select(p=>p.proof.guid+":"+p.proof.localId).ToArray(),identityMap=identityMap},true);
            }
            finally{if(clone!=null)UnityEngine.Object.DestroyImmediate(clone);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);if(AssetSha(Prefab)!=before)throw new InvalidOperationException("Read-only component-map probe altered canonical source bytes.");}
        }
        [Serializable]sealed class Schema4ComponentProbe{public int retainedDisabledComponentIds,retainedEnabledTowerComponents,originalPerches,actualFaultsRejected;public bool sourceBytesPreserved,noCanonicalAdoptionPerformed;public string[] sourceLocalIds;public DragonKingdomWorldAdoptionPlan.Schema4RetiredComponent[] identityMap;}
    }
}
#endif
