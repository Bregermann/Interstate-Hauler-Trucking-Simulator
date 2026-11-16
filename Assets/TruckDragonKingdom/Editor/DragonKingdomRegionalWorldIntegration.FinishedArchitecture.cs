#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable]public sealed class FinishedArchitecturePart
        {
            public string instance,source,guid,sourceSha256,sourceMetaSha256,parent,footingId,noTerrainFootingReason;public Vector3 composedRootPosition;public OwnedArchitectureMeshRepair ownedMeshRepair;
            public Vector3 localPosition,localScale;public Quaternion localRotation;public Bounds primaryBounds;
            public bool sourcePoseExact,sourceGeometryExact,active;public int primaryMeshes,primaryVertices,originalColliders,collidersAdded;
            public AssemblyOverrideEvidence[] geometryOverrides;public RidgeGeometryBinding[] sourceGeometry,composedGeometry;
        }
        [Serializable]public sealed class FinishedArchitectureGroup
        {
            public string key,region,name,layout,layoutSha256;public Vector3 position,scale;public Quaternion rotation;
            public Bounds bounds,declaredCompositionParcel,sourcePrimaryBounds;public string placementAuthority,retainedTowerSeparationAxis;public Vector3 originalFortressPlaza;public int instances,parents,primaryMeshes,primaryVertices,sourceColliders,addedColliders,footings;
            public float sourceGroundPlane;public bool exactCompleteSourceComposition,roadStartObjectiveAndWingClear,insideDeclaredCompositionParcel;
            public FinishedArchitecturePart[] parts;public AuthoredClearanceWitness blocker;public Bounds measuredRetainedTowerEnvelope,quarterBoundsBeforeTowerSeparation;public float retainedTowerSeparation,requiredRetainedTowerSeparation;
        }
        [Serializable]public sealed class FinishedArchitectureMaterial
        {public string source,target,targetSha256;public ArchitectureVariant publicUpgrade;}
        [Serializable]public sealed class FinishedArchitectureComposition
        {
            public string scope="ACTUAL_FOUR_SOURCE_ENSEMBLES_WITH_NATIVE_MESH_COLLISION_AND_INDIVIDUAL_GROUND_BEARING_PARCELS_NOT_TERRAIN_SUPPORT_PROTOTYPE_RETIREMENT_OR_VISUAL_ACCEPTANCE",geometryWitness,geometryWitnessSha256,status="IN_PROGRESS",error;
            public int instances,geometryFields,sourceFilesWatched,addedColliders,footings,ownedMeshRepairs;public FinishedWaterfront waterfront;
            public bool strictGeometryPrerequisite,sourceBytesPreserved,completeSourceFramesAndInstances,roadStartObjectiveAndWingClear,materialPreflightReused;public FinishedArchitectureGeometryMismatch geometryMismatch;
            public bool prototypeRetirementComplete,terrainSupportVerified,renderedAppearanceAccepted,crossEnsembleAndRetainedBuildingOverlapClear;public FinishedCastlePrototypeRetirement castlePrototypeRetirement;public FinishedArchitecturePeerReview overlapReview;
            public string[] changedSources;public FinishedArchitectureSourceEnvelope[] sourceEnvelopes;public FinishedArchitectureGroup[] groups;public NativeArchitectureFooting[] physicalFootings;public FinishedArchitectureMaterial[] materials;
        }
        [Serializable]public sealed class FinishedArchitectureSourceEnvelope
        {public string key,layout,layoutSha256;public int instances;public Bounds nativePrimaryBounds;}
        [Serializable]public sealed class FinishedArchitectureGeometryMismatch
        {public string instance,source,parentPath,expectedJsonSha256,actualJsonSha256;public string[] differingFields;public RidgeGeometryBinding[] expected,actual;public Vector3 rootLocalPosition,rootLocalScale,framePosition,frameScale;public Quaternion rootLocalRotation,frameRotation;}
        static FinishedArchitectureGeometryMismatch MeasureFinishedArchitectureGeometryMismatch(ArchitectureInstanceSource row,Transform root,Transform frame,RidgeGeometryBinding[] expected,RidgeGeometryBinding[] actual)
        {
            var differences=new List<string>();if(expected.Length!=actual.Length)differences.Add("bindingCount:"+expected.Length+"/"+actual.Length);
            for(int i=0;i<Mathf.Min(expected.Length,actual.Length);i++)foreach(var field in typeof(RidgeGeometryBinding).GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance))
            {var a=field.GetValue(expected[i]);var b=field.GetValue(actual[i]);bool equal;if(a is float[] fa&&b is float[] fb)equal=fa.Length==fb.Length&&fa.Zip(fb,(x,y)=>BitConverter.SingleToInt32Bits(x)==BitConverter.SingleToInt32Bits(y)).All(v=>v);else if(field.FieldType==typeof(string))equal=(a as string??string.Empty)==(b as string??string.Empty);else equal=Equals(a,b);if(!equal)differences.Add(i+":"+expected[i].key+":"+field.Name);}
            string first=JsonUtility.ToJson(new RidgeGeometryContainer{bindings=expected}),second=JsonUtility.ToJson(new RidgeGeometryContainer{bindings=actual});return new FinishedArchitectureGeometryMismatch{instance=row.sceneInstanceId,source=row.prefab,parentPath=frame.parent==null?"SCENE_ROOT":PathOf(frame.parent,frame.root),expected=expected,actual=actual,differingFields=differences.ToArray(),expectedJsonSha256=Hash(System.Text.Encoding.UTF8.GetBytes(first)).Replace("-","").ToLowerInvariant(),actualJsonSha256=Hash(System.Text.Encoding.UTF8.GetBytes(second)).Replace("-","").ToLowerInvariant(),rootLocalPosition=root.localPosition,rootLocalRotation=root.localRotation,rootLocalScale=root.localScale,framePosition=frame.position,frameRotation=frame.rotation,frameScale=frame.lossyScale};
        }
        sealed class FinishedArchitecturePlan
        {public string key,region;public Vector3 center;public float yaw,scale,sourcePlane;public Bounds parcel;}
        static FinishedArchitecturePlan[] FinishedArchitecturePlans()=>new[]{
            // The complete 4.3m-gate source quarter is an off-road neighborhood;
            // the existing 22m inner approach and extraction plaza stay clear.
            // The measured west/plaza NEW candidate parcel extends beyond the old
            // gray castle's renderer-derived region bounds. Adoption must explicitly
            // bind these measured bounds, never edit the persistent source SO.
            new FinishedArchitecturePlan{key="fortified-quarter",region="castle",center=new Vector3(0,60,620),yaw=90,scale=1,sourcePlane=0},
            new FinishedArchitecturePlan{key="lumber-warehouse",region="harbor",center=new Vector3(480,0,-454),yaw=0,scale=1.2f,sourcePlane=0,parcel=new Bounds(new Vector3(480,15,-454),new Vector3(90,80,90))},
            new FinishedArchitecturePlan{key="walled-town",region="village",center=new Vector3(-350,0,-310),yaw=0,scale=1,sourcePlane=15,parcel=new Bounds(new Vector3(-350,20,-310),new Vector3(150,90,230))},
            new FinishedArchitecturePlan{key="fishing-warehouse",region="harbor",center=new Vector3(570,-.6f,-646.6873f),yaw=0,scale=1.5f,sourcePlane=0,parcel=new Bounds(new Vector3(570,10,-646.6873f),new Vector3(50,80,65))}
        };
        public static FinishedArchitectureComposition LastFinishedArchitectureComposition{get;private set;}

        // Explicit opt-in authoring only. Not called by normal Play Mode,
        // rematch, source import, canonical initialization or normal builds.
        // The caller owns preview cleanup and, separately, candidate adoption.
        public static FinishedArchitectureComposition ComposeMeasuredFinishedArchitecture(DragonKingdomWorld world,string candidateFolder,string geometryWitness,bool persistMaterialVariants)
        {
            if(world==null||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!world.gameObject.scene.IsValid()||EditorUtility.IsPersistent(world))throw new InvalidOperationException("Actual stopped disposable candidate world scene required.");
            if(!candidateFolder.StartsWith(Root+"VisualCandidates/",StringComparison.Ordinal)||candidateFolder.Substring((Root+"VisualCandidates/").Length).Contains("/"))throw new InvalidOperationException("One explicit new world candidate folder required.");
            if(!File.Exists(Io(geometryWitness)))throw new InvalidOperationException("Actual complete strict native source geometry witness required before composition.");
            var native=JsonUtility.FromJson<ArchitectureGeometryReceipt>(File.ReadAllText(Io(geometryWitness)));
            if(native==null||!native.inspectionCompleted||!native.sourceBytesPreserved||!native.world29AndCanonicalBytesPreserved||!native.allGeometryOverridesReconciled||native.unresolvedTargets!=0||native.instances!=678||native.rawOverrideFields!=9637||native.ensembles==null||native.ensembles.Length!=4||native.ensembles.Any(e=>!e.geometryOverridesFullyReconciled||e.missingScripts!=0||e.unhandledStructuralEdits!=0))throw new InvalidOperationException("All9637 original source fields, zero unresolved targets/missing scripts and source-preserved complete native ensembles are mandatory. A provisional diagnostic cannot authorize placement.");
            var proof=new FinishedArchitectureComposition{strictGeometryPrerequisite=true,geometryWitness=geometryWitness,geometryWitnessSha256=AssetSha(geometryWitness),sourceEnvelopes=native.ensembles.Select(e=>new FinishedArchitectureSourceEnvelope{key=e.key,layout=e.layout,layoutSha256=e.layoutSha256,instances=e.instances,nativePrimaryBounds=e.actualPrimaryBounds}).ToArray()};LastFinishedArchitectureComposition=proof;
            var frozen=ArchitectureLayoutPaths.SelectMany(path=>JsonUtility.FromJson<ArchitectureGeometryLayout>(File.ReadAllText(Io(path))).dependencies.SelectMany(d=>new[]{d.path,d.path+".meta"})).Concat(ArchitectureLayoutPaths.SelectMany(p=>new[]{p,p+".meta"})).Distinct().ToDictionary(p=>p,AssetSha);proof.sourceFilesWatched=frozen.Count;
            var groups=new List<FinishedArchitectureGroup>();var footings=new List<NativeArchitectureFooting>();var variants=new Dictionary<Material,Material>();var materialRows=new List<FinishedArchitectureMaterial>();var created=new List<GameObject>();
            try
            {
                foreach(var plan in FinishedArchitecturePlans())
                {
                    var measured=native.ensembles.Single(e=>e.key==plan.key);int layoutIndex=Array.IndexOf(ArchitectureLayoutPaths,measured.layout);
                    if(layoutIndex<0||AssetSha(measured.layout)!=ArchitectureLayoutHashes[layoutIndex]||measured.layoutSha256!=ArchitectureLayoutHashes[layoutIndex])throw new InvalidOperationException("Pinned complete source layout differs.");
                    var layout=JsonUtility.FromJson<ArchitectureGeometryLayout>(File.ReadAllText(Io(measured.layout)));
                    var frame=new GameObject("Finished authored "+plan.key);created.Add(frame);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(frame,world.gameObject.scene);
                    // The accepted geometry probe reconstructs this exact frame
                    // at the scene root. Keep that same ancestry through every
                    // bit-exact source comparison; counter-translating a frame
                    // under the castle introduces inverse-matrix cancellation.
                    frame.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);frame.transform.localScale=Vector3.one;
                    var parents=layout.parentTransforms.ToDictionary(p=>p.id,p=>{var go=new GameObject(p.name);go.transform.SetParent(frame.transform,false);return go.transform;});
                    foreach(var p in layout.parentTransforms){var t=parents[p.id];t.SetParent(p.parent=="0"?frame.transform:parents[p.parent],false);t.localPosition=p.position;t.localRotation=p.rotation;t.localScale=p.scale;t.gameObject.SetActive(p.active);}
                    var group=new FinishedArchitectureGroup{key=plan.key,region=plan.region,name=frame.name,layout=measured.layout,layoutSha256=measured.layoutSha256,sourceGroundPlane=plan.sourcePlane,declaredCompositionParcel=plan.parcel,sourcePrimaryBounds=measured.actualPrimaryBounds,instances=layout.instances.Length,parents=layout.parentTransforms.Length};groups.Add(group);proof.groups=groups.ToArray();
                    var parts=new List<FinishedArchitecturePart>();var roots=new List<Transform>();
                    foreach(var row in layout.instances)
                    {
                        if(AssetDatabase.AssetPathToGUID(row.prefab)!=row.prefabGuid||AssetSha(row.prefab)!=row.sourceSha256||AssetSha(row.prefab+".meta")!=row.sourceMetaSha256)throw new InvalidOperationException("Exact original architecture source identity changed.");
                        var source=AssetDatabase.LoadAssetAtPath<GameObject>(row.prefab);if(source==null)throw new InvalidOperationException("Native original architecture source is missing.");
                        var instance=(GameObject)PrefabUtility.InstantiatePrefab(source,world.gameObject.scene);var t=instance.transform;t.SetParent(parents[row.sourceParent],false);t.localPosition=row.position;t.localRotation=row.rotation;t.localScale=row.scale;roots.Add(t);
                        var observations=native.overrides.Where(o=>o.layout==layout.name&&o.instance==row.sceneInstanceId).ToArray();
                        if(observations.Length!=row.modifications.Length)throw new InvalidOperationException("Complete native field observation/instance bijection required.");
                        var fields=row.modifications.Where((change,i)=>observations[i].geometryField&&observations[i].state!="EXACT_AUTHORED_WHOLE_ROOT_TRS_ALREADY_ASSIGNED_AND_NATIVE_READBACK_VERIFIED").ToArray();
                        var evidence=ApplyOriginalAssemblyOverrides(instance,ArchitectureAdapterRow(row,fields),ResolveArchitectureSourceTargets);
                        var geometry=RidgeGeometryBindings(t,new Dictionary<string,string>());var sourcePart=measured.parts.Single(p=>p.instance==row.sceneInstanceId);
                        // Compare before adding our collision or replacing four
                        // legacy materials with native public-upgrader variants.
                        if(JsonUtility.ToJson(new RidgeGeometryContainer{bindings=geometry})!=JsonUtility.ToJson(new RidgeGeometryContainer{bindings=sourcePart.nativeGeometry})){proof.geometryMismatch=MeasureFinishedArchitectureGeometryMismatch(row,t,frame.transform,sourcePart.nativeGeometry,geometry);throw new InvalidOperationException("Actual instantiated source geometry differs from complete native source witness: "+row.sceneInstanceId+" fields="+string.Join(",",proof.geometryMismatch.differingFields));}
                        var ownedRepair=RepairOwnedArchitectureMeshAfterRawBaseline(row,t,geometry);if(ownedRepair!=null)proof.ownedMeshRepairs++;
                        int old=instance.GetComponentsInChildren<Collider>(true).Length;ConfigureSourceAuthoredStaticCollision(t);int added=instance.GetComponentsInChildren<Collider>(true).Length-old;
                        foreach(var renderer in instance.GetComponentsInChildren<Renderer>(true))
                        {
                            var values=renderer.sharedMaterials;for(int slot=0;slot<values.Length;slot++)
                            {var original=values[slot];if(original==null)throw new InvalidOperationException("Actual authored architecture material is missing.");if(DragonKingdomWorldVisualMigration.HasSupportedUrpShader(original))continue;
                                if(!variants.TryGetValue(original,out var variant)){variant=CreateFinishedArchitectureMaterial(original,candidateFolder,persistMaterialVariants,out var rowMaterial);variants.Add(original,variant);materialRows.Add(rowMaterial);}values[slot]=variant;}
                            renderer.sharedMaterials=values;
                        }
                        var part=new FinishedArchitecturePart{instance=row.sceneInstanceId,source=row.prefab,guid=row.prefabGuid,sourceSha256=row.sourceSha256,sourceMetaSha256=row.sourceMetaSha256,parent=row.sourceParent,localPosition=row.position,localRotation=row.rotation,localScale=row.scale,sourcePoseExact=t.localPosition==row.position&&t.localScale==row.scale&&Quaternion.Angle(t.localRotation,row.rotation)<.005f,sourceGeometryExact=true,active=instance.activeInHierarchy,originalColliders=old,collidersAdded=added,geometryOverrides=evidence,sourceGeometry=geometry,ownedMeshRepair=ownedRepair};parts.Add(part);group.parts=parts.ToArray();group.sourceColliders+=old;group.addedColliders+=added;proof.addedColliders+=added;
                    }
                    // Attach the complete intact frame only after all original
                    // source-local geometry has passed the SAME exact equality.
                    frame.transform.SetParent(world.Region(plan.region).root,true);
                    // The source's actual vertex centroid determines horizontal
                    // placement. Original authored floor elevation, NOT whole
                    // minY (bridge piers, embedded bases), determines seating.
                    RidgeMeshSamples(frame.transform,out var sourceBounds,out _,out _);frame.transform.rotation=Quaternion.Euler(0,plan.yaw,0);frame.transform.localScale=Vector3.one*plan.scale;var rotatedCenter=frame.transform.TransformPoint(new Vector3(sourceBounds.center.x,plan.sourcePlane,sourceBounds.center.z));frame.transform.position=plan.center-rotatedCenter;
                    if(plan.key=="fortified-quarter")
                    {
                        // V318 proves the former north parcel crosses the real
                        // forest–caverns bypass. Place the ENTIRE actual source
                        // quarter beside the west towers, aligned with the
                        // original fortress plaza, rather than nudging modules
                        // around that road or enlarging the narrow source gates.
                        var towers=MeasuredRetainedCastleTowerBounds(world);var envelope=towers[0];foreach(var b in towers.Skip(1))envelope.Encapsulate(b);RidgeMeshSamples(frame.transform,out var quarter,out _,out _);group.quarterBoundsBeforeTowerSeparation=quarter;group.measuredRetainedTowerEnvelope=envelope;group.requiredRetainedTowerSeparation=6;
                        var plaza=world.extractionSites.Single(site=>site.id=="fortress").secureZone.bounds.center;group.originalFortressPlaza=plaza;group.placementAuthority="WHOLE_ORIGINAL_QUARTER_WEST_OF_MEASURED_RETAINED_TOWERS_ALIGNED_WITH_ORIGINAL_FORTRESS_EXTRACTION_PLAZA";group.retainedTowerSeparationAxis="NEGATIVE_X_WEST";
                        var center=new Vector3(envelope.min.x-group.requiredRetainedTowerSeparation-quarter.extents.x,quarter.center.y,plaza.z);frame.transform.position+=new Vector3(center.x-quarter.center.x,0,center.z-quarter.center.z);RidgeMeshSamples(frame.transform,out var separated,out _,out _);group.retainedTowerSeparation=envelope.min.x-separated.max.x;
                        // Explicit NEW candidate parcel, derived from the full
                        // unchanged source frame and6m authoring margin. It is
                        // placement/adoption provenance, never a terrain pad.
                        group.declaredCompositionParcel=new Bounds(new Vector3(separated.center.x,70,separated.center.z),new Vector3(separated.size.x+12,140,separated.size.z+12));
                        if(group.retainedTowerSeparation<group.requiredRetainedTowerSeparation-.001f)throw new InvalidOperationException("Actual intact west quarter does not clear the measured retained tower edge.");
                        if(!FinishBoundsClearOfRoads(world,separated,w=>group.blocker=w)||!RidgeWingClear(world,separated))throw new InvalidOperationException("Complete derived west/plaza quarter parcel blocks an original road/start/objective/wing corridor: "+JsonUtility.ToJson(group.blocker));
                    }
                    if(plan.key=="fishing-warehouse")PositionFinishedFishingFrame(frame.transform,layout,roots,group);
                    RidgeMeshSamples(frame.transform,out group.bounds,out group.primaryMeshes,out group.primaryVertices);group.position=frame.transform.position;group.rotation=frame.transform.rotation;group.scale=frame.transform.localScale;group.insideDeclaredCompositionParcel=RidgeInsideRegionXZ(group.declaredCompositionParcel,group.bounds);
                    if(!group.insideDeclaredCompositionParcel)throw new InvalidOperationException("Complete source composition exceeds its explicit new candidate parcel: "+plan.key);
                    for(int i=0;i<roots.Count;i++)
                    {
                        var t=roots[i];var part=parts[i];part.composedRootPosition=t.position;if(!part.active){part.noTerrainFootingReason="EXACT_AUTHORED_INACTIVE_INSTANCE_RETAINED";continue;}part.primaryBounds=ArchitecturePrimaryEnvelope(t,out part.primaryMeshes,out part.primaryVertices);
                        part.composedGeometry=RidgeGeometryBindings(t,new Dictionary<string,string>());
                        if(!FinishBoundsClearOfRoads(world,part.primaryBounds,w=>group.blocker=w)||!RidgeWingClear(world,part.primaryBounds))throw new InvalidOperationException("Actual individual authored module blocks an original road/start/objective/wing corridor: "+plan.key+"/"+part.instance+" "+JsonUtility.ToJson(group.blocker));
                        if(plan.key=="fishing-warehouse"){part.noTerrainFootingReason=part.source.EndsWith("/TB_Prop_Ship.prefab",StringComparison.Ordinal)?"SOURCE_WATERBORNE_BOAT_REQUIRES_ACTUAL_WATERLINE_AND_BED_NEVER_A_TERRAIN_PAD":"SOURCE_WATERFRONT_DOCK_STRUCTURE_OR_SUPPORTED_PROP_REQUIRES_ACTUAL_PILE_AND_GANGWAY_CONTACT_NEVER_A_TERRAIN_PAD";continue;}
                        // Only modules touching the original artist's ground
                        // plane are terrain constraints. Elevated windows,
                        // chimneys, roof details and stacked cargo are not pads.
                        float plane=plan.center.y;bool groundBearing=part.primaryBounds.min.y<=plane+plan.scale*.45f&&part.primaryBounds.max.y>=plane-plan.scale*.45f;
                        if(groundBearing)
                        {
                            var b=part.primaryBounds;if(b.size.x>65||b.size.z>65)throw new InvalidOperationException("An individual architecture footing must be a measured ground-bearing physical module, never a whole town envelope.");if(b.size.x<=.05f||b.size.z<=.05f){part.noTerrainFootingReason="REAL_THIN_SOURCE_COLLISION_RETAINED_WITHOUT_INVENTED_PLANAR_FOOTING";continue;}
                            b=new Bounds(new Vector3(b.center.x,plane,b.center.z),new Vector3(b.size.x,.16f,b.size.z));part.footingId=plan.key+":"+part.instance;
                            footings.Add(new NativeArchitectureFooting{id=part.footingId,layout=measured.layout,layoutSha256=measured.layoutSha256,instance=part.instance,sourcePrefab=part.source,sourcePrefabGuid=part.guid,sourcePrefabSha256=part.sourceSha256,sourcePrefabMetaSha256=part.sourceMetaSha256,sourceGeometryWitness=geometryWitness,sourceGeometryWitnessSha256=proof.geometryWitnessSha256,physicalFootprint=b,surfaceY=plane});group.footings++;
                        }
                    }
                    if(plan.key=="fishing-warehouse")proof.waterfront=MeasureFinishedWaterfrontSource(frame.transform,group);
                    group.exactCompleteSourceComposition=parts.Count==layout.instanceCount&&parts.All(p=>p.sourcePoseExact&&p.sourceGeometryExact);group.roadStartObjectiveAndWingClear=true;proof.instances+=parts.Count;
                }
                proof.overlapReview=ReviewFinishedArchitecturePeers(world,groups);proof.crossEnsembleAndRetainedBuildingOverlapClear=proof.overlapReview.passed;if(!proof.overlapReview.passed)throw new InvalidOperationException("Actual bounded inter-ensemble/retained-building overlap review rejected: "+JsonUtility.ToJson(proof.overlapReview));
                proof.physicalFootings=footings.ToArray();proof.footings=footings.Count;proof.materials=materialRows.ToArray();proof.geometryFields=native.appliedGeometryFields;
                proof.completeSourceFramesAndInstances=proof.instances==678&&groups.Count==4&&groups.All(g=>g.exactCompleteSourceComposition);proof.roadStartObjectiveAndWingClear=groups.All(g=>g.roadStartObjectiveAndWingClear);
                if(!proof.completeSourceFramesAndInstances||!proof.roadStartObjectiveAndWingClear||!proof.crossEnsembleAndRetainedBuildingOverlapClear||footings.Count==0||footings.Select(f=>f.id).Distinct().Count()!=footings.Count||materialRows.Count!=4||proof.ownedMeshRepairs!=1||proof.waterfront==null||!proof.waterfront.actualSourceHullGeometryPassed)throw new InvalidOperationException("Complete four-ensemble source/material/collision/individual-parcel stage required.");
                proof.status="COMPOSED_SOURCE_COLLISION_AND_PARCELS_REQUIRES_EXPLICIT_PROTOTYPE_REPLACEMENT_TERRAIN_SUPPORT_AND_RENDER_ACCEPTANCE";return proof;
            }
            catch(Exception e){proof.error=e.ToString();proof.status="REJECTED_COMPLETE_ARCHITECTURE_COMPOSITION";foreach(var go in created)if(go!=null)UnityEngine.Object.DestroyImmediate(go);if(!persistMaterialVariants)foreach(var material in variants.Values)if(material!=null)UnityEngine.Object.DestroyImmediate(material);throw;}
            finally{proof.changedSources=frozen.Where(p=>AssetSha(p.Key)!=p.Value).Select(p=>p.Key).ToArray();proof.sourceBytesPreserved=proof.changedSources.Length==0;if(!proof.sourceBytesPreserved)throw new InvalidOperationException("Architecture composition changed original source bytes.");}
        }
        [Serializable]public sealed class FinishedArchitecturePeerShape
        {public string group,instance,source,guid,path;public Bounds primaryBounds;}
        [Serializable]public sealed class FinishedArchitecturePeerReview
        {public string scope="INDIVIDUAL_ACTIVE_SOURCE_MODULES_VERSUS_OTHER_ENSEMBLES_AND_ACTUAL_RETAINED_BUILDING_TOWERS_ALLOWING_ORIGINAL_INTRA_ENSEMBLE_OVERLAP",error;public int crossEnsemblePairs,retainedBuildingPairs,retainedBuildings,retainedTowers;public bool passed;public FinishedArchitecturePeerShape first,second;}
        static Bounds[] MeasuredRetainedCastleTowerBounds(DragonKingdomWorld world)
        {
            const string meshGuid="5c2734c723764924ebd0458ab8293423";var sanctuary=world.Region("castle").root.Cast<Transform>().Single(t=>t.name=="Fortified castle sanctuary");var result=new List<Bounds>();
            foreach(Transform t in sanctuary)
            {bool sourceTower=RidgePrimaryMeshFilters(t).Any(f=>AssetDatabase.TryGetGUIDAndLocalFileIdentifier(f.sharedMesh,out string guid,out long _)&&guid==meshGuid);if(!sourceTower)continue;RidgeMeshSamples(t,out var b,out _,out _);result.Add(b);}
            if(result.Count!=4)throw new InvalidOperationException("Exactly four actual retained source tower primary meshes required before off-road quarter placement.");return result.ToArray();
        }
        static FinishedArchitecturePeerReview ReviewFinishedArchitecturePeers(DragonKingdomWorld world,List<FinishedArchitectureGroup> groups)
        {
            var proof=new FinishedArchitecturePeerReview();var newParts=groups.SelectMany(g=>g.parts.Where(p=>p.active&&p.primaryMeshes>0).Select(p=>new FinishedArchitecturePeerShape{group=g.key,instance=p.instance,source=p.source,guid=p.guid,path=g.name+"/"+p.instance,primaryBounds=p.primaryBounds})).ToArray();var retained=new List<FinishedArchitecturePeerShape>();
            foreach(var region in world.regions)foreach(Transform t in region.root)
            {
                if(!t.name.StartsWith("P_BLD_house",StringComparison.Ordinal)&&t.name!="Complete owned windmill house14")continue;RidgeMeshSamples(t,out var b,out _,out _);string source=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);retained.Add(new FinishedArchitecturePeerShape{group="RETAINED_BUILDING",instance=region.id+":"+t.GetSiblingIndex(),source=source,guid=AssetDatabase.AssetPathToGUID(source),path=PathOf(t,world.transform),primaryBounds=b});proof.retainedBuildings++;
            }
            var sanctuary=world.Region("castle").root.Cast<Transform>().Single(t=>t.name=="Fortified castle sanctuary");
            foreach(Transform t in sanctuary)
            {if(!RidgePrimaryMeshFilters(t).Any())continue;RidgeMeshSamples(t,out var b,out _,out _);string source=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);retained.Add(new FinishedArchitecturePeerShape{group="RETAINED_CASTLE_MODEL",instance="castle:"+t.GetSiblingIndex(),source=source,guid=AssetDatabase.AssetPathToGUID(source),path=PathOf(t,world.transform),primaryBounds=b});proof.retainedTowers++;}
            if(proof.retainedTowers!=4)throw new InvalidOperationException("Exact obsolete shell retirement must precede finished architecture peer review; four retained support towers only.");
            Bounds NumericalInterior(Bounds bounds){float minimum=Mathf.Max(0,Mathf.Min(bounds.extents.x,Mathf.Min(bounds.extents.y,bounds.extents.z)));float shrink=Mathf.Min(.01f,minimum*.1f);bounds.extents-=Vector3.one*shrink;return bounds;}
            bool Overlap(FinishedArchitecturePeerShape a,FinishedArchitecturePeerShape b)=>NumericalInterior(a.primaryBounds).Intersects(NumericalInterior(b.primaryBounds));
            foreach(var part in newParts)
            {
                foreach(var peer in newParts.Where(p=>p.group!=part.group&&string.CompareOrdinal(p.group,part.group)>0))
                {proof.crossEnsemblePairs++;if(Overlap(part,peer)){proof.first=part;proof.second=peer;proof.error="ACTUAL_CROSS_ENSEMBLE_PRIMARY_MESH_ENVELOPE_OVERLAP";return proof;}}
                foreach(var peer in retained)
                {proof.retainedBuildingPairs++;if(Overlap(part,peer)){proof.first=part;proof.second=peer;proof.error="ACTUAL_RETAINED_BUILDING_OR_SUPPORT_TOWER_PRIMARY_MESH_ENVELOPE_OVERLAP";return proof;}}
            }
            proof.passed=true;return proof;
        }
        [Serializable]public sealed class FinishedCastleRetiredComponent
        {public string type,guid,localId,sourceJsonSha256,path;public bool beforeEnabled,afterEnabled;}
        [Serializable]public sealed class FinishedCastlePrototypeRetirement
        {public string scope="EXPLICIT_13_OWNED_GRAY_WALL_GATE_AND_LARGE_TOWER_INSTANCE_RENDERER_COLLIDER_RETIREMENT_PRESERVING_4_ORIGINAL_PERCH_SUPPORT_TOWERS_ALL_TRANSFORMS_REFERENCES_AND_SOURCE_BYTES",source,sourceSha256;public int disabledComponents,retainedTowerComponents,perches;public bool exactNativeComponentBoundary,allPerchReferencesAndWorldPosesExact,sourceBytesPreserved;public FinishedCastleRetiredComponent[] retired;}
        public static FinishedCastlePrototypeRetirement RetireMeasuredCastlePrototypeShell(DragonKingdomWorld world,string geometryWitness)
        {
            if(world==null||EditorUtility.IsPersistent(world)||!world.gameObject.scene.IsValid()||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Explicit disposable world candidate only.");
            const string originalGuid="7cc05905626c7234289a4d2e3eaa9869",originalSha="820c9dc867faac9243579acce615b7209e5fd57fdf5b1f5e45974819c14e1744",retainedPerchTowerMeshGuid="5c2734c723764924ebd0458ab8293423";
            if(AssetSha(Prefab)!=originalSha||AssetDatabase.AssetPathToGUID(Prefab)!=originalGuid)throw new InvalidOperationException("Exact original owned castle source is mandatory for this replacement boundary.");
            var witness=JsonUtility.FromJson<ArchitectureGeometryReceipt>(File.ReadAllText(Io(geometryWitness)));var boundary=witness.proposedPrototypeBoundaries.Single(b=>b.prefab==Prefab&&b.prefabGuid==originalGuid&&b.prefabSha256==originalSha&&b.rootGameObject.localId=="7910969967957938101"&&b.rootTransform.localId=="1276675040747331789");
            if(boundary.rendererAndColliderComponents.Length!=34||boundary.incomingReferences.Length!=5)throw new InvalidOperationException("Exact actual measured original34-component castle boundary and5 scoped incoming references required.");
            var canonical=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);var originalSanctuary=canonical.GetComponentsInChildren<Transform>(true).Single(t=>SourceIdentityMatches(t,originalGuid,boundary.rootTransform.localId));
            string sourcePath=AnimationUtility.CalculateTransformPath(originalSanctuary,canonical.transform);
            var sanctuary=world.GetComponentsInChildren<Transform>(true).Single(t=>AnimationUtility.CalculateTransformPath(t,world.transform)==sourcePath);
            // The original preview stores plain scene objects. Full authored
            // subtree equivalence to the pinned canonical GameObject fileID
            // establishes provenance without inventing a missing prefab link.
            var sourceRoot=OriginalOwnedRoadSource(sanctuary,long.Parse(boundary.rootGameObject.localId));
            // Existing source-authoring correspondence uses sibling indices
            // and complete component slots, so repeated wall names remain safe.
            Component ResolveMeasuredComponent(UnityEngine.Object native)=>(Component)PairSourceAssemblyObject(sourceRoot,sanctuary.gameObject,native,out _);
            var originalPerches=world.Region("castle").perches.ToArray();var perchPoses=originalPerches.Select(Pose).ToArray();
            if(originalPerches.Length!=4||originalPerches.Any(t=>t==null||!t.IsChildOf(sanctuary)))throw new InvalidOperationException("All four original referenced perch transforms must survive on their actual source support towers.");
            var components=sanctuary.GetComponentsInChildren<Component>(true).Where(c=>c is Renderer||c is Collider).ToArray();var retired=new List<FinishedCastleRetiredComponent>();var keep=new List<Component>();
            foreach(var id in boundary.rendererAndColliderComponents)
            {
                var nativeSource=AssemblyObjects(canonical).Single(v=>v.GetType().FullName==id.type&&SourceIdentityMatches(v,originalGuid,id.localId));var c=ResolveMeasuredComponent(nativeSource);
                string serialized=Hash(System.Text.Encoding.UTF8.GetBytes(EditorJsonUtility.ToJson(nativeSource))).Replace("-","").ToLowerInvariant();if(serialized!=id.serializedJsonSha256)throw new InvalidOperationException("Original measured component JSON differs before explicit retirement.");
                var mesh=c.GetComponent<MeshFilter>()?.sharedMesh;bool retain=mesh!=null&&AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh,out string meshGuid,out long _)&&meshGuid==retainedPerchTowerMeshGuid;
                if(retain){keep.Add(c);continue;}bool enabled=c is Renderer renderer?renderer.enabled:((Collider)c).enabled;if(!enabled)throw new InvalidOperationException("Prototype boundary was already altered; no silent repeated authoring.");
                retired.Add(new FinishedCastleRetiredComponent{type=id.type,guid=id.guid,localId=id.localId,sourceJsonSha256=id.serializedJsonSha256,path=id.path,beforeEnabled=enabled,afterEnabled=false});
            }
            if(retired.Count!=26||keep.Count!=8||originalPerches.Any(t=>!keep.OfType<Collider>().Any(c=>t.IsChildOf(c.transform))))throw new InvalidOperationException("Exactly13 obsolete models and4 retained physical perch-support towers required; never disable referenced support geometry.");
            // Resolve and validate the ENTIRE boundary before changing a flag.
            foreach(var row in retired){var nativeSource=AssemblyObjects(canonical).Single(v=>v.GetType().FullName==row.type&&SourceIdentityMatches(v,row.guid,row.localId));var c=ResolveMeasuredComponent(nativeSource);if(c is Renderer renderer)renderer.enabled=false;else ((Collider)c).enabled=false;row.afterEnabled=c is Renderer actualRenderer?actualRenderer.enabled:((Collider)c).enabled;if(row.afterEnabled)throw new InvalidOperationException("Actual component retirement readback failed.");}
            var proof=new FinishedCastlePrototypeRetirement{source=Prefab,sourceSha256=originalSha,disabledComponents=retired.Count,retainedTowerComponents=keep.Count,perches=originalPerches.Length,exactNativeComponentBoundary=true,allPerchReferencesAndWorldPosesExact=originalPerches.SequenceEqual(world.Region("castle").perches)&&originalPerches.Select(Pose).SequenceEqual(perchPoses)&&keep.All(c=>c is Renderer r?r.enabled:((Collider)c).enabled),sourceBytesPreserved=AssetSha(Prefab)==originalSha,retired=retired.ToArray()};
            if(!proof.allPerchReferencesAndWorldPosesExact||!proof.sourceBytesPreserved)throw new InvalidOperationException("Explicit prototype retirement altered protected source/reference/support state.");return proof;
        }
        public static FinishedCastlePrototypeRetirement CaptureMeasuredCastlePrototypeRetirementFromCommandLine()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonArchitectureGeometry");if(at<0||at+1>=args.Length)throw new InvalidOperationException("Explicit actual native castle component witness path required.");string witness=Path.GetFullPath(args[at+1]);var source=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);string original=AssetSha(Prefab);var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();GameObject instance=null;FinishedCastlePrototypeRetirement proof=null;
            try{instance=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);proof=RetireMeasuredCastlePrototypeShell(instance.GetComponent<DragonKingdomWorld>(),witness);return proof;}
            finally{if(instance!=null)UnityEngine.Object.DestroyImmediate(instance);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"exact-castle-prototype-retirement-preview.json"),JsonUtility.ToJson(proof,true));if(AssetSha(Prefab)!=original)throw new InvalidOperationException("Exact temporary prototype retirement changed original world bytes.");}
        }
        static bool CanReuseActualArchitectureMaterialPreflight()
        {
            var proof=LastArchitectureMaterialPreflight;if(proof==null||!proof.passed||!proof.sourceBytesPreserved||!proof.allRawSnapshotsExact||!proof.allSourceIdentitiesExact||!proof.noUnknownDependencyChanges||!proof.temporaryVariantsDestroyed||proof.layouts!=4||proof.sourceInstances!=678||proof.sourceDependencies!=518||proof.rawMaterials!=26||proof.dependencies==null||proof.dependencies.Length!=518||proof.variants==null||proof.variants.Length!=4||proof.unityVersion!=Application.unityVersion)return false;
            for(int i=0;i<ArchitectureLayoutPaths.Length;i++)if(AssetSha(ArchitectureLayoutPaths[i])!=ArchitectureLayoutHashes[i])return false;
            var layouts=ArchitectureLayoutPaths.Select(path=>JsonUtility.FromJson<ArchitectureMaterialLayout>(File.ReadAllText(Io(path)))).ToArray();var dependencies=layouts.SelectMany(l=>l.dependencies).Select(d=>d.path).Distinct().OrderBy(p=>p,StringComparer.Ordinal).ToArray();if(!dependencies.SequenceEqual(proof.dependencies.Select(d=>d.path).OrderBy(p=>p,StringComparer.Ordinal)))return false;
            foreach(var row in proof.dependencies)if(AssetSha(row.path)!=row.nativeSha256||AssetSha(row.path+".meta")!=row.nativeMetaSha256||AssetDatabase.AssetPathToGUID(row.path)!=row.guid)return false;
            foreach(var snapshot in layouts.SelectMany(l=>l.originalMaterials).GroupBy(p=>p.source).Select(g=>g.First()))ValidateArchitectureSnapshot(snapshot,false);foreach(var layout in layouts)ValidateArchitectureSnapshot(layout.sourceSceneSnapshot,true);
            var package=UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset).Assembly);return package!=null&&package.name==proof.publicUpgraderPackage&&package.version==proof.urpVersion&&AssetSha(Path.Combine(package.resolvedPath,proof.publicUpgraderPath))==proof.publicUpgraderSha256;
        }
        public static FinishedArchitectureComposition CaptureMeasuredFinishedArchitectureStageFromCommandLine()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonArchitectureGeometry");if(at<0||at+1>=args.Length)throw new InvalidOperationException("Explicit actual successful strict architecture geometry receipt path required.");
            string witness=Path.GetFullPath(args[at+1]);bool reused=CanReuseActualArchitectureMaterialPreflight();if(!reused)CaptureArchitectureMaterialPreflight();var source=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);if(source==null)throw new InvalidOperationException("Actual original source world required.");string originalSha=AssetSha(Prefab);var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();GameObject root=null;Material[] temporary=Array.Empty<Material>();FinishedArchitectureComposition proof=null;
            try
            {
                root=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);var world=root.GetComponent<DragonKingdomWorld>();if(world==null)throw new InvalidOperationException("Actual source world component missing.");
                var retired=RetireMeasuredCastlePrototypeShell(world,witness);proof=ComposeMeasuredFinishedArchitecture(world,Root+"VisualCandidates/MeasuredArchitectureStage01",witness,false);proof.castlePrototypeRetirement=retired;proof.prototypeRetirementComplete=retired.exactNativeComponentBoundary&&retired.allPerchReferencesAndWorldPosesExact&&retired.sourceBytesPreserved;
                temporary=root.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null&&!EditorUtility.IsPersistent(m)).Distinct().ToArray();
                if(!proof.sourceBytesPreserved||!proof.strictGeometryPrerequisite||!proof.completeSourceFramesAndInstances||!proof.roadStartObjectiveAndWingClear)throw new InvalidOperationException("Actual complete source/collision/individual-parcel stage rejected.");return proof;
            }
            finally
            {
                if(root!=null){temporary=temporary.Concat(root.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null&&!EditorUtility.IsPersistent(m))).Distinct().ToArray();UnityEngine.Object.DestroyImmediate(root);}foreach(var material in temporary)if(material!=null)UnityEngine.Object.DestroyImmediate(material);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
                var result=proof??LastFinishedArchitectureComposition;if(result!=null)result.materialPreflightReused=reused;Directory.CreateDirectory(Output());File.WriteAllText(Path.Combine(Output(),"finished-architecture-source-collision-parcels-stage.json"),JsonUtility.ToJson(result,true));if(AssetSha(Prefab)!=originalSha)throw new InvalidOperationException("Disposable architecture stage changed original canonical source world.");
            }
        }
        static Material CreateFinishedArchitectureMaterial(Material source,string folder,bool persist,out FinishedArchitectureMaterial evidence)
        {
            string path=AssetDatabase.GetAssetPath(source);var prerequisite=LastArchitectureMaterialPreflight;
            if(prerequisite==null||!prerequisite.passed||prerequisite.variants==null||!prerequisite.variants.Any(v=>v.source==path&&v.sourceSha256==AssetSha(path)&&v.sourceMetaSha256==AssetSha(path+".meta")))throw new InvalidOperationException("Exact successful four-material native public StandardUpgrader preflight required, not a guessed shader replacement.");
            var copy=new Material(source){name=source.name+" authored kingdom URP",hideFlags=persist?HideFlags.None:HideFlags.HideAndDontSave};new StandardUpgrader("Standard").Upgrade(copy,MaterialUpgrader.UpgradeFlags.None);var measured=MeasureArchitectureVariant(source,copy,prerequisite);measured.temporaryOnly=!persist;
            if(!measured.supported||!measured.textureUvColorSmoothnessMetallicAndEmissionPreserved||measured.shaderErrors.Length!=0){UnityEngine.Object.DestroyImmediate(copy);throw new InvalidOperationException("Actual public URP material conversion failed.");}
            string target=folder+"/Materials/Architecture/"+measured.guid+" URP.mat";evidence=new FinishedArchitectureMaterial{source=path,target=persist?target:"DISPOSABLE_NATIVE_PUBLIC_UPGRADE_ONLY",publicUpgrade=measured};
            if(persist)
            {if(File.Exists(Io(target))){UnityEngine.Object.DestroyImmediate(copy);throw new InvalidOperationException("Fresh candidate architecture materials cannot silently overwrite existing authored assets.");}Directory.CreateDirectory(Io(folder+"/Materials/Architecture"));AssetDatabase.Refresh();AssetDatabase.CreateAsset(copy,target);AssetDatabase.SaveAssetIfDirty(copy);evidence.targetSha256=AssetSha(target);}
            return copy;
        }
    }
}
#endif
