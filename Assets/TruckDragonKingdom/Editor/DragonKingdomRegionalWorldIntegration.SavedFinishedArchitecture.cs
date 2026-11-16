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
        [Serializable]public sealed class SavedFinishedArchitectureValidation
        {
            public string scope="READ_ONLY_SAVED_ARCHITECTURE_BINDINGS_AND_PHYSICAL_PROVENANCE_NOT_ADOPTION_OR_VISUAL_APPROVAL";
            public int sourceInstances,ownedMeshRepairs,architectureFootings,retainedBuildings,persistentMaterials,waterfrontSupportParts;
            public bool savedBindingsVerified;
        }
        [Serializable]public sealed class FinishedArchitectureTerrainContact
        {public string footing,terrain,terrainData,terrainDataGuid,terrainDataSha256,terrainDataMetaSha256;public Vector3 point,actualColliderPoint;public float expectedSurface,heightfieldSurface,error;public bool matchingTerrainCollider,nativeColliderRayPerformed;}
        [Serializable]public sealed class FinishedArchitectureTerrainSupport
        {public string scope="FINAL_INDIVIDUAL_SOURCE_FOOTING_HEIGHTFIELD_AND_MATCHING_NATIVE_TERRAIN_COLLIDER_CONTACT_NOT_VISUAL_APPROVAL";public bool passed,nativeColliderRaysPerformed;public int parcels,samples;public float maximumError;public FinishedArchitectureTerrainContact[] contacts;}

        public static FinishedArchitectureTerrainSupport ValidateFinishedArchitectureTerrainContacts(DragonKingdomWorld world,FinishedArchitectureComposition proof)
        {
            if(world==null||EditorUtility.IsPersistent(world)||!world.gameObject.scene.IsValid()||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Actual stopped coordinated candidate scene required for native source architecture footing contacts.");
            return MeasureSavedArchitectureTerrainContacts(world,proof,true);
        }
        static FinishedArchitectureTerrainSupport MeasureSavedArchitectureTerrainContacts(DragonKingdomWorld world,FinishedArchitectureComposition proof,bool native)
        {
            if(proof?.physicalFootings==null||proof.physicalFootings.Length!=525||proof.physicalFootings.Select(f=>f.id).Distinct().Count()!=525)throw new InvalidOperationException("Exactly525 individual source geometry footings required for final support measurement.");
            var terrains=world.GetComponentsInChildren<Terrain>(true);if(terrains.Length<4)throw new InvalidOperationException("Actual four coordinated terrain fields required for source footing contacts.");
            var records=new List<FinishedArchitectureTerrainContact>();var hashes=new Dictionary<string,string>();string Digest(string path){if(!hashes.TryGetValue(path,out string value)){value=AssetSha(path);hashes.Add(path,value);}return value;}
            foreach(var footing in proof.physicalFootings)foreach(float x in new[]{footing.physicalFootprint.min.x,footing.physicalFootprint.center.x,footing.physicalFootprint.max.x})foreach(float z in new[]{footing.physicalFootprint.min.z,footing.physicalFootprint.center.z,footing.physicalFootprint.max.z})
            {
                var choices=terrains.Where(t=>t.terrainData!=null&&x>=t.transform.position.x-.001f&&z>=t.transform.position.z-.001f&&x<=t.transform.position.x+t.terrainData.size.x+.001f&&z<=t.transform.position.z+t.terrainData.size.z+.001f).ToArray();if(choices.Length==0)throw new InvalidOperationException("Source architecture footing falls outside actual coordinated fields: "+footing.id);
                foreach(var terrain in choices)
                {
                    var data=terrain.terrainData;var collider=terrain.GetComponent<TerrainCollider>();if(collider==null||!collider.enabled||collider.terrainData!=data)throw new InvalidOperationException("Source architecture footing requires matching actual TerrainData/TerrainCollider: "+footing.id);
                    string path=AssetDatabase.GetAssetPath(data);float height=terrain.transform.position.y+data.GetInterpolatedHeight(Mathf.Clamp01((x-terrain.transform.position.x)/data.size.x),Mathf.Clamp01((z-terrain.transform.position.z)/data.size.z));var point=new Vector3(x,height,z);var actual=point;
                    if(native){if(!collider.Raycast(new Ray(point+Vector3.up*2,Vector3.down),out var hit,4)||Mathf.Abs(hit.point.y-height)>.03f)throw new InvalidOperationException("Source architecture footing lacks matching native TerrainCollider contact: "+footing.id);actual=hit.point;}
                    float error=Mathf.Abs(height-(footing.surfaceY-.08f));if(error>.25f)throw new InvalidOperationException("Source architecture footing is floating/buried after final coordination: "+footing.id+" error="+error);
                    records.Add(new FinishedArchitectureTerrainContact{footing=footing.id,terrain=PathOf(terrain.transform,world.transform),terrainData=path,terrainDataGuid=AssetDatabase.AssetPathToGUID(path),terrainDataSha256=Digest(path),terrainDataMetaSha256=Digest(path+".meta"),point=point,actualColliderPoint=actual,expectedSurface=footing.surfaceY-.08f,heightfieldSurface=height,error=error,matchingTerrainCollider=true,nativeColliderRayPerformed=native});
                }
            }
            return new FinishedArchitectureTerrainSupport{passed=true,nativeColliderRaysPerformed=native,parcels=525,samples=records.Count,maximumError=records.Max(c=>c.error),contacts=records.ToArray()};
        }

        // Shared prerequisite for the existing adoption reader's future schema4
        // branch. Source-stage success is deliberately insufficient here.
        public static void ValidateFinishedArchitectureSavePrerequisites(FinishedArchitectureComposition proof,NativeSettlementFootingEvidence retained,NativeRoadReliefEvidence relief)
        {
            if(proof==null||proof.instances!=678||proof.ownedMeshRepairs!=1||proof.footings!=525||proof.groups==null||proof.groups.Length!=4||proof.physicalFootings==null||proof.physicalFootings.Length!=525)
                throw new InvalidOperationException("Saved architecture requires all678 original instances, one declared owned mesh repair and525 individual source footings.");
            var expected=new Dictionary<string,int>{{"fortified-quarter",110},{"lumber-warehouse",156},{"walled-town",382},{"fishing-warehouse",30}};
            if(proof.groups.Select(g=>g.key).Distinct().Count()!=4||proof.groups.Any(g=>!expected.TryGetValue(g.key,out int count)||g.instances!=count||g.parts==null||g.parts.Length!=count||g.parts.Select(p=>p.instance).Distinct().Count()!=count||!g.exactCompleteSourceComposition||!g.roadStartObjectiveAndWingClear||!g.insideDeclaredCompositionParcel))
                throw new InvalidOperationException("Saved architecture requires the complete four distinct original source groups and their exact instance inventories.");
            if(!proof.strictGeometryPrerequisite||!proof.sourceBytesPreserved||!proof.completeSourceFramesAndInstances||!proof.roadStartObjectiveAndWingClear||!proof.crossEnsembleAndRetainedBuildingOverlapClear||!proof.prototypeRetirementComplete||proof.changedSources==null||proof.changedSources.Length!=0||proof.castlePrototypeRetirement==null||!proof.castlePrototypeRetirement.exactNativeComponentBoundary||!proof.castlePrototypeRetirement.sourceBytesPreserved||!proof.castlePrototypeRetirement.allPerchReferencesAndWorldPosesExact||proof.castlePrototypeRetirement.disabledComponents!=26||proof.castlePrototypeRetirement.retainedTowerComponents!=8)
                throw new InvalidOperationException("Saved architecture requires source-preserved full geometry, clearance and exact26-component prototype retirement evidence.");
            if(proof.materials==null||proof.materials.Length!=4||proof.materials.Select(m=>m.target).Distinct().Count()!=4||proof.materials.Select(m=>m.source).Distinct().Count()!=4||proof.materials.Any(m=>m.publicUpgrade==null||m.publicUpgrade.temporaryOnly||!m.publicUpgrade.supported||!m.publicUpgrade.textureUvColorSmoothnessMetallicAndEmissionPreserved||m.publicUpgrade.shaderErrors==null||m.publicUpgrade.shaderErrors.Length!=0||string.IsNullOrEmpty(m.targetSha256)||!m.target.StartsWith(Root+"VisualCandidates/",StringComparison.Ordinal)||!m.target.EndsWith(".mat",StringComparison.Ordinal)))
                throw new InvalidOperationException("Saved architecture requires four persistent public-upgraded materials; disposable source-preview materials cannot authorize a saved world.");
            if(!proof.terrainSupportVerified)throw new InvalidOperationException("Saved architecture requires actual final native individual source footing support; assigning coordination targets is insufficient.");
            if(retained==null||retained.buildings!=25||retained.parts==null||retained.parts.Length!=25||retained.physicalFootings==null||retained.physicalFootings.Length!=25||!retained.sourceBytesPreserved||!retained.originalCriticalPosesAndGraphExact||!retained.allNativeTerrainContactsVerified||retained.parts.Any(p=>!p.sourceGeometryAndScalePreserved||!p.originalRoadStartObjectiveAndWingClear||!p.actualTerrainContactsVerified)||retained.parts.Count(p=>p.region=="village")!=13||retained.parts.Count(p=>p.region=="harbor")!=8||retained.parts.Count(p=>p.region=="refuge")!=4||retained.parts.Any(p=>string.IsNullOrEmpty(p.footingId))||retained.parts.Select(p=>p.footingId).Distinct().Count()!=25)
                throw new InvalidOperationException("Saved architecture requires all25 actual retained buildings:13 village,8 harbor,4 refuge, with final native contacts.");
            var waterfront=proof.waterfront;
            if(waterfront==null||!waterfront.actualSourceHullGeometryPassed||!waterfront.all30SourcePosesPreserved||!waterfront.noWaterfrontTerrainPads||!waterfront.actualBedAndPhysicalSupportsPassed||!waterfront.sourceGeometryPreserved||!waterfront.originalRoadsAndFerryPreserved||!waterfront.sourceAndTerrainBytesPreserved||!waterfront.ferryDepartureClear||waterfront.ferryDepartureQueries!=53||waterfront.supportedStructures!=3||waterfront.boats==null||waterfront.boats.Length!=2||waterfront.boats.Any(b=>!b.actualBedContactsPassed||b.depthSamples!=9||b.minimumKeelClearance<3)||waterfront.addedParts==null||waterfront.addedParts.Length==0||waterfront.pierContacts==null||waterfront.pierContacts.Any(p=>!p.baseContact||!p.deckContact)||waterfront.sourceBearings==null||waterfront.sourceBearings.Any(p=>!p.passed)||waterfront.gangwayContacts==null||waterfront.gangwayContacts.Length==0||waterfront.gangwayContacts.Any(p=>!p.passed))
                throw new InvalidOperationException("Saved architecture requires actual coordinated waterfront bed, piles, gangway and53 original ferry sweep proofs; source-only hull geometry is insufficient.");
            if(relief==null||!relief.passed||!relief.actualVendorGradeCapturedBeforeCoordination||!relief.wholeTownAndGroveBoundingBoxesNotConstraints||!relief.sourceRoadPosesAndGraphUnchanged||relief.footings==null||relief.explicitArchitectureFootings!=550||relief.footings.Length!=550)
                throw new InvalidOperationException("Saved architecture requires actual native coordination of all525 source and25 retained individual footings.");
        }

        public static SavedFinishedArchitectureValidation ValidateSavedFinishedArchitecture(DragonKingdomWorld world,FinishedArchitectureComposition proof,NativeSettlementFootingEvidence retained,NativeRoadReliefEvidence relief)
        {
            ValidateFinishedArchitectureSavePrerequisites(proof,retained,relief);
            if(world==null||!EditorUtility.IsPersistent(world)||!AssetDatabase.GetAssetPath(world).StartsWith(Root+"VisualCandidates/",StringComparison.Ordinal))throw new InvalidOperationException("Read-only validation requires the actual reloaded saved candidate, not an unsaved preview or canonical world.");
            if(AssetSha(proof.geometryWitness)!=proof.geometryWitnessSha256||AssetSha(retained.geometryWitness)!=retained.geometryWitnessSha256)throw new InvalidOperationException("Saved architecture geometry witnesses drifted.");
            var usedMaterials=new HashSet<string>();var result=new SavedFinishedArchitectureValidation();
            foreach(var material in proof.materials)ValidateSavedArchitectureMaterial(material);
            foreach(var group in proof.groups)
            {
                int layoutIndex=Array.IndexOf(ArchitectureLayoutPaths,group.layout);if(layoutIndex<0||group.layoutSha256!=ArchitectureLayoutHashes[layoutIndex]||AssetSha(group.layout)!=group.layoutSha256)throw new InvalidOperationException("Saved architecture complete source layout drifted: "+group.key);
                var layout=JsonUtility.FromJson<ArchitectureGeometryLayout>(File.ReadAllText(Io(group.layout)));
                var frame=world.Region(group.region).root.Cast<Transform>().Single(t=>t.name==group.name);
                SavedArchitecturePose(frame,group.position,group.rotation,group.scale,false,group.name);
                var parents=new Dictionary<string,Transform>{{"0",frame}};
                Transform Parent(string id)
                {
                    if(parents.TryGetValue(id,out var known))return known;var row=layout.parentTransforms.Single(p=>p.id==id);var t=Parent(row.parent).Cast<Transform>().Single(p=>p.name==row.name&&!PrefabUtility.IsAnyPrefabInstanceRoot(p.gameObject));SavedArchitecturePose(t,row.position,row.rotation,row.scale,true,row.id);parents.Add(id,t);return t;
                }
                foreach(var parent in layout.parentTransforms)Parent(parent.id);
                var matched=new HashSet<Transform>();
                foreach(var part in group.parts)
                {
                    var original=layout.instances.Single(p=>p.sceneInstanceId==part.instance);if(original.prefab!=part.source||original.prefabGuid!=part.guid||original.sourceSha256!=part.sourceSha256||original.sourceMetaSha256!=part.sourceMetaSha256||!part.sourcePoseExact||!part.sourceGeometryExact||AssetSha(part.source)!=part.sourceSha256||AssetSha(part.source+".meta")!=part.sourceMetaSha256||AssetDatabase.AssetPathToGUID(part.source)!=part.guid)throw new InvalidOperationException("Saved source instance provenance drifted: "+part.instance);
                    var t=Parent(part.parent).Cast<Transform>().Single(p=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(p.gameObject)==part.source&&Vector3.Distance(p.localPosition,part.localPosition)<.001f&&Quaternion.Angle(p.localRotation,part.localRotation)<.005f&&Vector3.Distance(p.localScale,part.localScale)<.0001f);
                    if(!matched.Add(t)||RidgeGeometryObjectActive(t)!=part.active||Vector3.Distance(t.position,part.composedRootPosition)>.002f)throw new InvalidOperationException("Saved source instance ownership, active state or composed pose differs: "+part.instance);
                    var actual=RidgeGeometryBindings(t,new Dictionary<string,string>());SavedArchitectureGeometry(actual,part.active?part.composedGeometry:part.sourceGeometry,!part.active,part.instance);
                    if(part.active)
                    {
                        var primary=ArchitecturePrimaryEnvelope(t,out int meshes,out int vertices);if(meshes!=part.primaryMeshes||vertices!=part.primaryVertices||Vector3.Distance(primary.center,part.primaryBounds.center)>.002f||Vector3.Distance(primary.size,part.primaryBounds.size)>.002f)throw new InvalidOperationException("Saved complete source primary mesh envelope differs: "+part.instance);
                        if(!string.IsNullOrEmpty(part.footingId))
                        {
                            var footing=proof.physicalFootings.Single(f=>f.id==part.footingId);var expectedFootprint=new Bounds(new Vector3(primary.center.x,footing.surfaceY,primary.center.z),new Vector3(primary.size.x,.16f,primary.size.z));
                            if(group.key=="fishing-warehouse"||footing.id!=group.key+":"+part.instance||footing.layout!=group.layout||footing.layoutSha256!=group.layoutSha256||footing.instance!=part.instance||footing.sourcePrefab!=part.source||footing.sourcePrefabGuid!=part.guid||footing.sourceGeometryWitness!=proof.geometryWitness||footing.sourceGeometryWitnessSha256!=proof.geometryWitnessSha256||Vector3.Distance(footing.physicalFootprint.center,expectedFootprint.center)>.002f||Vector3.Distance(footing.physicalFootprint.size,expectedFootprint.size)>.002f)throw new InvalidOperationException("Saved source footing no longer corresponds to its actual individual primary geometry: "+part.instance);
                        }
                    }
                    if(t.GetComponentsInChildren<Collider>(true).Length!=part.originalColliders+part.collidersAdded)throw new InvalidOperationException("Saved source collider inventory differs: "+part.instance);
                    if(part.ownedMeshRepair!=null){ValidateSavedOwnedArchitectureRepair(t,part);result.ownedMeshRepairs++;}
                    foreach(var renderer in t.GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials){if(material==null||!DragonKingdomWorldVisualMigration.HasSupportedUrpShader(material))throw new InvalidOperationException("Saved architecture renderer lost its actual supported material.");usedMaterials.Add(AssetDatabase.GetAssetPath(material));}
                    result.sourceInstances++;
                }
                if(parents.Values.SelectMany(p=>p.Cast<Transform>()).Count(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))!=matched.Count)throw new InvalidOperationException("Saved architecture has an unaccounted direct source instance.");
            }
            if(result.sourceInstances!=678||result.ownedMeshRepairs!=1||proof.materials.Any(m=>!usedMaterials.Contains(m.target)))throw new InvalidOperationException("Saved architecture instance/repair/material use is incomplete.");
            if(proof.groups.SelectMany(g=>g.parts).Count(p=>!string.IsNullOrEmpty(p.footingId))!=525)throw new InvalidOperationException("Every saved source footing must belong to one exact source module.");
            var allFootings=proof.physicalFootings.Concat(retained.physicalFootings).ToArray();if(allFootings.Select(f=>f.id).Distinct().Count()!=550)throw new InvalidOperationException("Saved architecture has duplicate individual footing IDs.");
            foreach(var footing in allFootings)
            {
                var applied=relief.footings.Single(f=>f.id==footing.id);if(JsonUtility.ToJson(footing)!=JsonUtility.ToJson(applied)||AssetSha(footing.layout)!=footing.layoutSha256||AssetSha(footing.sourcePrefab)!=footing.sourcePrefabSha256||AssetSha(footing.sourcePrefab+".meta")!=footing.sourcePrefabMetaSha256||AssetDatabase.AssetPathToGUID(footing.sourcePrefab)!=footing.sourcePrefabGuid||AssetSha(footing.sourceGeometryWitness)!=footing.sourceGeometryWitnessSha256)throw new InvalidOperationException("Saved individual footing/source/coordination provenance differs: "+footing.id);
                foreach(float x in new[]{footing.physicalFootprint.min.x,footing.physicalFootprint.center.x,footing.physicalFootprint.max.x})foreach(float z in new[]{footing.physicalFootprint.min.z,footing.physicalFootprint.center.z,footing.physicalFootprint.max.z})if(Mathf.Abs(DragonKingdomTerrainShoulderAuthoring.TerrainHeight(world,new Vector3(x,0,z))-(footing.surfaceY-.08f))>.25f)throw new InvalidOperationException("Saved individual footing has lost final coordinated support: "+footing.id);
            }
            MeasureSavedArchitectureTerrainContacts(world,proof,false);
            foreach(var part in retained.parts)
            {
                var root=world.Region(part.region).root.Cast<Transform>().Single(t=>t.name==part.name&&Vector3.Distance(t.position,part.after)<.002f&&PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)==part.source);SavedArchitecturePose(root,part.after,part.rotation,part.scale,false,part.name);
                if(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root.gameObject)!=part.source||AssetSha(part.source)!=part.sourceSha256||AssetSha(part.source+".meta")!=part.sourceMetaSha256||AssetDatabase.AssetPathToGUID(part.source)!=part.sourceGuid)throw new InvalidOperationException("Saved retained house source differs: "+part.name);
                SavedArchitectureGeometry(RidgeGeometryBindings(root,new Dictionary<string,string>()),part.geometry,false,part.name);
                var measured=MeasureNativeSettlementBuildingBase(root,out _,out _,out _);if(Vector3.Distance(measured.center,part.measuredBaseFootprint.center)>.002f||Vector3.Distance(measured.size,part.measuredBaseFootprint.size)>.002f)throw new InvalidOperationException("Saved retained actual structural base differs: "+part.name);result.retainedBuildings++;
            }
            foreach(var boat in proof.waterfront.boats)
            {
                var part=proof.groups.Single(g=>g.key=="fishing-warehouse").parts.Single(p=>p.instance==boat.instance);if(Vector3.Distance(part.primaryBounds.center,boat.bounds.center)>.002f||Vector3.Distance(part.primaryBounds.size,boat.bounds.size)>.002f||Mathf.Abs(boat.keel-boat.bounds.min.y)>.001f)throw new InvalidOperationException("Saved original source hull geometry/depth witness differs.");
                float minimum=float.PositiveInfinity;foreach(float x in new[]{boat.bounds.min.x,boat.bounds.center.x,boat.bounds.max.x})foreach(float z in new[]{boat.bounds.min.z,boat.bounds.center.z,boat.bounds.max.z})minimum=Mathf.Min(minimum,boat.keel-DragonKingdomTerrainShoulderAuthoring.TerrainHeight(world,new Vector3(x,0,z)));if(minimum<3||Mathf.Abs(minimum-boat.minimumKeelClearance)>.03f)throw new InvalidOperationException("Saved original source hull lost its actual sea-bed clearance.");
            }
            var support=world.Region("harbor").root.Cast<Transform>().Single(t=>t.name=="Finished source fishing supports and gangway");
            foreach(var part in proof.waterfront.addedParts)
            {
                var root=support.GetComponentsInChildren<Transform>(true).Single(t=>t.name==part.name);SavedArchitecturePose(root,part.position,part.rotation,part.scale,false,part.name);
                if(AssetSha(part.source)!=part.sourceSha256||AssetSha(part.source+".meta")!=part.sourceMetaSha256||AssetDatabase.AssetPathToGUID(part.source)!=part.guid)throw new InvalidOperationException("Saved waterfront timber source drifted.");SavedArchitectureGeometry(RidgeGeometryBindings(root,new Dictionary<string,string>()),part.geometry,false,part.name);result.waterfrontSupportParts++;
            }
            result.architectureFootings=525;result.persistentMaterials=4;result.savedBindingsVerified=true;return result;
        }
        static void SavedArchitecturePose(Transform t,Vector3 position,Quaternion rotation,Vector3 scale,bool local,string label)
        {if(Vector3.Distance(local?t.localPosition:t.position,position)>.002f||Quaternion.Angle(local?t.localRotation:t.rotation,rotation)>.005f||Vector3.Distance(t.localScale,scale)>.0001f)throw new InvalidOperationException("Saved architecture pose/scale differs: "+label);}
        static void SavedArchitectureGeometry(RidgeGeometryBinding[] actual,RidgeGeometryBinding[] expected,bool allowAdded,string label)
        {
            if(expected==null||(!allowAdded&&actual.Length!=expected.Length))throw new InvalidOperationException("Saved architecture geometry inventory differs: "+label);
            foreach(var row in expected)
            {
                var found=actual.Single(p=>p.key==row.key);var a=JsonUtility.FromJson<RidgeGeometryBinding>(JsonUtility.ToJson(found));var b=JsonUtility.FromJson<RidgeGeometryBinding>(JsonUtility.ToJson(row));var matrix=a.rootMatrix;var previous=b.rootMatrix;a.rootMatrix=b.rootMatrix=null;
                if(JsonUtility.ToJson(a)!=JsonUtility.ToJson(b)||matrix==null||previous==null||matrix.Length!=16||previous.Length!=16||Enumerable.Range(0,16).Any(i=>Mathf.Abs(matrix[i]-previous[i])>.0001f))throw new InvalidOperationException("Saved architecture original mesh/collider/material/state/relative geometry differs: "+label+"/"+row.key);
            }
        }
        static void ValidateSavedOwnedArchitectureRepair(Transform root,FinishedArchitecturePart part)
        {
            var p=part.ownedMeshRepair;if(part.instance!="1007988382"||part.guid!="231c22fbd5f9acf46901b04d3bda8604"||p.instance!=part.instance||p.sourcePrefab!=part.source||p.filterLocalId!="33846281388073860"||p.meshAssetGuid!="85c50bb430239ac44ac2cbc5b096ff4a"||p.originalRequestedId!="4300000"||p.repairedId!="4300002"||p.repairedVertices!=216||p.repairedIndices!=324||!p.rawNativeReferenceWasNull||!p.sourcePoseAndMaterialsPreserved||!p.sourceBytesPreserved||AssetSha(p.meshAsset)!=p.meshAssetSha256||AssetSha(p.meshAsset+".meta")!=p.meshAssetMetaSha256)throw new InvalidOperationException("Saved owned ladder correction lost its exact pinned authority.");
            var filter=root.GetComponentsInChildren<MeshFilter>(true).Single(f=>SourceIdentityMatches(f,part.guid,p.filterLocalId));if(filter.sharedMesh==null||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(filter.sharedMesh,out string guid,out long id)||guid!=p.meshAssetGuid||id!=4300002||filter.sharedMesh.vertexCount!=216||filter.sharedMesh.triangles.Length!=324||!root.GetComponentsInChildren<MeshCollider>(true).Any(c=>c.enabled&&!c.isTrigger&&c.sharedMesh==filter.sharedMesh))throw new InvalidOperationException("Saved owned ladder actual mesh/collision differs.");
        }
        static void ValidateSavedArchitectureMaterial(FinishedArchitectureMaterial part)
        {
            var p=part.publicUpgrade;if(p.source!=part.source||AssetSha(part.source)!=p.sourceSha256||AssetSha(part.source+".meta")!=p.sourceMetaSha256||AssetDatabase.AssetPathToGUID(part.source)!=p.guid||AssetSha(part.target)!=part.targetSha256)throw new InvalidOperationException("Saved architecture material source/target hashes differ.");
            var material=AssetDatabase.LoadAssetAtPath<Material>(part.target);if(material==null||material.shader==null||material.shader.name!=p.variantShader||!DragonKingdomWorldVisualMigration.HasSupportedUrpShader(material)||material.GetColor("_BaseColor")!=p.variantColor||material.GetColor("_EmissionColor")!=p.variantEmission||Mathf.Abs(material.GetFloat("_Smoothness")-p.variantSmoothness)>.00001f||Mathf.Abs(material.GetFloat("_Metallic")-p.variantMetallic)>.00001f)throw new InvalidOperationException("Saved architecture public material properties differ.");
            foreach(var map in p.maps)
            {
                var texture=material.GetTexture(map.targetProperty);string guid="",path="";long id=0;if(texture!=null){path=AssetDatabase.GetAssetPath(texture);AssetDatabase.TryGetGUIDAndLocalFileIdentifier(texture,out guid,out id);}if(!map.preserved||(guid??"")!=(map.textureGuid??"")||path!=(map.texturePath??"")||id!=map.textureFileId||material.GetTextureScale(map.targetProperty)!=map.scale||material.GetTextureOffset(map.targetProperty)!=map.offset)throw new InvalidOperationException("Saved architecture texture/localID/UV provenance differs.");
            }
        }
    }
}
#endif
