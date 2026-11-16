using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using EasyRoads3Dv3;
using sc.modeling.river.runtime;
using UnityEngine.Splines;
using Unity.Mathematics;
using LWS.InterstateHauler;
using LWS.TruckSports.Editor;
using UnityEditor;
using UnityEngine;
#if VISTA
using Pinwheel.Vista;
using Pinwheel.Vista.Graph;
using Pinwheel.Vista.UnityTerrain;
#endif

namespace LWS.TruckDragonKingdom.Editor
{
    // Explicit editor preparation only. No InitializeOnLoad, scene replacement,
    // Play Mode generation or automatic prefab rebuild. The integration owner
    // binds runtime contracts and reviews terrain/road clearance before baking.
    public static partial class DragonKingdomWorldAuthoring
    {
        public const string Folder = "TruckDragonKingdom";
        public const string Private = "Assets/TruckDragonKingdom/PrivateLicensedEnvironment/";
        public const string Art = Private + "Plugins/";
        public const string Knights = Art + "Synty Studios/PolygonKnights/Prefabs/Buildings/";
        public const string Village = Art + "Tidal Flask Studios/Fantastic Village Pack/prefabs/";
        public const string Forest = Private + "Fantastic Ancient Forest/Prefabs/";
        public const string Cliff = Art + "Polyart Studio/HandpaintedEnvironmentVol1/Meshes/Prefab/Cliffs/";
        public const string Dock = Art + "Ventuar3D/MedievalCastlePack/Prefabs/Structures/Dock Pieces/";
        public const string PrefabPath = "Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab";
        public const string PreviewScenePath = "Assets/TruckDragonKingdom/Scenes/DragonKingdomPreview.unity";
        public const string DefinitionPath = "Assets/TruckDragonKingdom/Configuration/Dragon Kingdom.asset";
        public static bool IsBuilding { get; private set; }
        public static string LastBuildStatus { get; private set; } = "Not run";

        public static void Create()
        {
            RequireStoppedEditor();
            if (IsBuilding) throw new InvalidOperationException("Kingdom authoring is already running");
            EditorCoroutineUtility.StartCoroutineOwnerless(BuildSharedWorld());
        }

        public static IEnumerator BuildSharedWorld()
        {
            RequireStoppedEditor();
            if (File.Exists(PrefabPath) || File.Exists(PreviewScenePath) || File.Exists(DefinitionPath))
                throw new InvalidOperationException("Preserve existing kingdom output; review it before an explicit replacement pass");
            if (Terrain.activeTerrains.Length > 0)
                throw new InvalidOperationException("Run isolated kingdom authoring with no other loaded terrains; EasyRoads terrain mutation must not touch existing worlds");
            IsBuilding = true;
            LastBuildStatus = "Generating Vista terrain";
            var original = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            foreach (string sub in new[] { "Prefabs", "Scenes", "Configuration", "Models", "Materials", "Terrain" })
                Directory.CreateDirectory("Assets/" + Folder + "/" + sub);
            AssetDatabase.Refresh();
            var container = new GameObject("Dragon Kingdom preview and terrain").transform;
            Exception failure = null;
#if VISTA
            VistaManager manager = null; ProgressiveTask task = null;
            try { manager = PrepareVistaTerrain(container); task = manager.GenerateAll(); }
            catch (Exception e) { failure = e; }
            double started = EditorApplication.timeSinceStartup;
            while (failure == null && !task.isCompleted)
            {
                if (EditorApplication.timeSinceStartup - started > 240)
                { VistaManager.CancelActiveGenerateTask(); failure = new TimeoutException("Vista generation exceeded the bounded editor authoring window"); break; }
                yield return null;
            }
#else
            failure = new InvalidOperationException("The installed Vista foundation must be available for kingdom authoring");
#endif
            if (failure == null)
            {
                try
                {
                    LastBuildStatus = "Grading authored routes with EasyRoads";
                    GradeTerrainWithEasyRoads(scene);
                    // Vista sample graphs may output legacy/null layers and
                    // undernormalized splats. Paint only appearance after the
                    // final vendor height grading, preserving every height.
                    foreach(var terrain in container.GetComponentsInChildren<Terrain>())DragonKingdomWorldVisualMigration.RepairTerrainAppearance(terrain.terrainData);
                    LastBuildStatus = "Placing owned kingdom models";
                    var prepared = PrepareVendorContent(container);
                    // Terrain stays inside the single reusable runtime composition.
                    foreach (var terrain in container.GetComponentsInChildren<Terrain>().ToArray()) terrain.transform.SetParent(prepared.root, true);
                    var road = RoadMaterial();
                    var contact = new PhysicsMaterial("Kingdom gravel contact") { dynamicFriction = .8f, staticFriction = .9f, bounciness = 0 };
                    AssetDatabase.CreateAsset(contact, "Assets/" + Folder + "/Materials/Kingdom gravel contact.physicMaterial");
                    var graph = BakeRoads(prepared.root, road, contact);
                    Waterways(prepared);
                    var definition = ScriptableObject.CreateInstance<DragonKingdomDefinition>();
                    AssetDatabase.CreateAsset(definition, DefinitionPath);
                    var world = BindRuntime(prepared, definition, graph);
                    PreviewPresentation(container);
                    LastBuildStatus = "Validating physical support, clearance and extraction approaches";
                    Physics.SyncTransforms();
                    ValidateTruckStarts(world,true);
                    ValidatePhysicalRoads(prepared.root, graph);
                    world.ValidateReferences();
                    ValidateAuthoredBindings(world);
                    AssetDatabase.SaveAssets();
                    PrefabUtility.SaveAsPrefabAsset(prepared.root.gameObject, PrefabPath);
                    if (!EditorSceneManager.SaveScene(scene, PreviewScenePath)) throw new InvalidOperationException("Dedicated preview scene did not save");
                    LastBuildStatus = "Ready for rendered and NWH driving verification";
                    Debug.Log("DRAGON_KINGDOM_AUTHORING_READY prefab=" + PrefabPath + " scene=" + PreviewScenePath + " roads=" + graph.edges.Count);
                }
                catch (Exception e) { failure = e; }
            }
            if (failure != null)
            {
                LastBuildStatus = "FAILED: " + failure.Message;
                Debug.LogError("DRAGON_KINGDOM_AUTHORING_FAILED " + failure);
                // Keep the dedicated unsaved scene for diagnosis; never overwrite
                // prior output or save a failed composition as an accepted world.
            }
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            IsBuilding = false;
        }

        static Material RoadMaterial()
        {
            var result = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Owned Vista sediment kingdom roads" };
            result.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/Sediment/sediment_diff.jpg");
            if (result.mainTexture == null) throw new InvalidOperationException("Installed Vista sediment road texture required");
            result.mainTextureScale = new Vector2(4, 32); result.SetFloat("_Smoothness", .1f);
            AssetDatabase.CreateAsset(result, "Assets/" + Folder + "/Materials/Kingdom gravel road.mat");
            return result;
        }

        public static void RegisterOwnedNativeGradingType(ERRoadType type,ERModularBase owner,float indent,float surrounding)
        {
            if(type==null||owner==null||EditorUtility.IsPersistent(owner)||!owner.gameObject.scene.IsValid()||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||string.IsNullOrEmpty(type.roadTypeName)||!float.IsFinite(indent)||!float.IsFinite(surrounding)||indent<3||indent>50||surrounding<4||surrounding>200)throw new InvalidOperationException("Actual stopped disposable owned network/type and bounded grading settings required.");
            var active=UnityEngine.Object.FindObjectsByType<ERModularBase>(FindObjectsSortMode.None);
            if(active.Length!=1||active[0]!=owner)throw new InvalidOperationException("The public type.Update authority requires this exact sole active disposable road network; never choose an unrelated source network.");
            if(owner.roadTypes.Any(existing=>existing!=null&&existing.roadTypeName==type.roadTypeName))throw new InvalidOperationException("Owned native semantic road type already exists: "+type.roadTypeName);
            double stableId;using(var sha=System.Security.Cryptography.SHA256.Create())
            {var digest=sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes("LWS.NativeGrade."+type.roadTypeName));ulong stable=0;for(int i=0;i<6;i++)stable=(stable<<8)|digest[i];stableId=stable+1;}
            if(owner.roadTypes.Any(existing=>existing!=null&&existing.id==stableId))throw new InvalidOperationException("Stable owned native road type ID collision; preserve existing entry.");
            // Update allocates its new native entry with an SDK timestamp ID.
            // First force a new entry with an unused temporary request. Bind
            // only that actual appended entry to the exact stable semantic ID
            // through its public id BEFORE creating any dependent road.
            double request=stableId;
            if(owner.roadTypes.Any(existing=>existing!=null&&existing.id==request))throw new InvalidOperationException("SDK temporary registration request collides with existing type.");
            int before=owner.roadTypes.Count;var prefix=owner.roadTypes.ToArray();var prefixJson=prefix.Select(value=>JsonUtility.ToJson(value)).ToArray();type.id=request;type.Update();
            if(owner.roadTypes.Count!=before+1||!owner.roadTypes.Take(before).SequenceEqual(prefix))throw new InvalidOperationException("Public SDK registration did not append exactly one owned entry.");
            var actual=owner.roadTypes[before];if(actual==null||actual.roadTypeName!=type.roadTypeName)throw new InvalidOperationException("Actual new SDK entry does not identify this semantic road.");
            actual.id=stableId;type.id=stableId;type.Update();var bound=ERRoadType.GetRoadType(type,owner);
            if(owner.roadTypes.Count!=before+1||!ReferenceEquals(actual,bound)||bound.id!=stableId||bound.roadTypeName!=type.roadTypeName)throw new InvalidOperationException("Public stable owned native type binding was not retained exactly.");
            actual.defaultIndent=indent;actual.defaultSurrounding=surrounding;
            if(!owner.roadTypes.Take(before).Select(value=>JsonUtility.ToJson(value)).SequenceEqual(prefixJson))throw new InvalidOperationException("Owned native registration changed an existing type/default/ID configuration.");
        }
        public static ERModularRoad ConfigureOwnedNativeRoadGlobals(ERRoad road,ERRoadType type,ERModularBase owner,float indent,float surrounding)
        {
            if(road==null||type==null||owner==null||EditorUtility.IsPersistent(owner)||!owner.gameObject.scene.IsValid()||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!float.IsFinite(indent)||!float.IsFinite(surrounding)||indent<3||indent>50||surrounding<4||surrounding>200)throw new InvalidOperationException("Stopped disposable owned road and bounded global grading settings required.");
            var binding=ERRoadType.GetRoadType(type,owner);
            if(binding==null||binding.id!=type.id||binding.roadTypeName!=type.roadTypeName||binding.defaultIndent!=indent||binding.defaultSurrounding!=surrounding)throw new InvalidOperationException("Configure only this exact publicly registered owned type/default binding.");
            // The SDK can clone registered native type data during road creation.
            // The returned ERRoad's PUBLIC identity is authoritative, not an
            // assumed reference-equal registry object or a scene/name search.
            var actual=road.roadScript;
            if(actual==null||road.gameObject!=actual.gameObject||!actual.transform.IsChildOf(owner.transform)||EditorUtility.IsPersistent(actual)||actual.gameObject.scene!=owner.gameObject.scene||actual.rt==null||actual.rt.id!=binding.id||actual.rt.roadTypeName!=binding.roadTypeName||actual.rt.defaultIndent!=indent||actual.rt.defaultSurrounding!=surrounding)throw new InvalidOperationException("Returned actual road lacks this owned network/type semantic identity/defaults.");
            actual.indent=indent;actual.surrounding=surrounding;
            if(actual.indent!=indent||actual.surrounding!=surrounding||actual!=road.roadScript||actual.rt.id!=binding.id||actual.rt.roadTypeName!=binding.roadTypeName)throw new InvalidOperationException("Actual returned road global configuration/identity did not retain the requested values.");
            return actual;
        }
        public static void GradeTerrainWithEasyRoads(Scene target, float roadIndent=3, float roadSurrounding=4,Bounds? underground=null)
            =>GradeTerrainWithEasyRoadsObserved(target,roadIndent,roadSurrounding,underground,null);
        public static void GradeTerrainWithEasyRoadsObserved(Scene target,float roadIndent,float roadSurrounding,Bounds? underground,Action<DragonKingdomGradeObservation> observe,bool nativeTypeDefaults=false,bool nativePhysicalCrossings=false)
        {
            if(roadIndent<3||roadIndent>50||roadSurrounding<4||roadSurrounding>200)throw new ArgumentOutOfRangeException("Bounded installed EasyRoads shoulder settings required.");
            if(nativePhysicalCrossings&&!nativeTypeDefaults)throw new InvalidOperationException("Physical terrain crossing exclusions require the explicit owned native grading path; legacy grading is unchanged.");
            bool smoothNativeNe=Environment.GetCommandLineArgs().Contains(DragonKingdomVendorFalloffAuthoring.Flag);
            if(smoothNativeNe&&!nativeTypeDefaults)throw new InvalidOperationException("Vendor falloff trial requires explicit native grading; legacy authoring remains unchanged.");
            if(Environment.GetCommandLineArgs().Contains(DragonKingdomVendorFalloffAuthoring.ProfileFlag)&&!smoothNativeNe)throw new InvalidOperationException("Vendor falloff profile requires its explicit trial flag; never silently skip a selected authoring operation.");
            var terrains = Terrain.activeTerrains.Where(t => t.gameObject.scene == target).ToArray();
            if (terrains.Length != 4 || Terrain.activeTerrains.Any(t => t.gameObject.scene != target))
                throw new InvalidOperationException("Isolated four-tile Vista terrain required before vendor road grading");
            var template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EasyRoads3D/Resources/ERRoadNetwork.prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(template);
            SceneManager.MoveGameObjectToScene(instance, target);
            void Stage(string stage)=>observe?.Invoke(new DragonKingdomGradeObservation{stage=stage,terrains=terrains,network=instance});
            void Setting(ERRoad road,string setting,int marker,float requested,float returned)=>observe?.Invoke(new DragonKingdomGradeObservation{stage="setting",road=road,setting=setting,marker=marker,requested=requested,returned=returned});
            void Indent(ERRoad road,float value,int marker){float actual=road.SetIndent(value,marker);Setting(road,"SetIndent",marker,value,actual);}
            void Surrounding(ERRoad road,float value,int marker){float actual=road.SetSurrounding(value,marker);Setting(road,"SetSurrounding",marker,value,actual);}
            void Updated(ERRoad road)=>observe?.Invoke(new DragonKingdomGradeObservation{stage="roadUpdated",road=road,terrains=terrains,network=instance});
            try
            {
                Stage("beforeNetworkInit");
                var owner = instance.GetComponent<ERModularBase>();
                var settings = new SerializedObject(owner);
                var queryHeight = settings.FindProperty("terrainRaycastHeight");
                if (queryHeight == null) throw new InvalidOperationException("Installed EasyRoads terrain ray-query height configuration changed");
                if(queryHeight.propertyType==SerializedPropertyType.Float)queryHeight.floatValue=1000;
                else if(queryHeight.propertyType==SerializedPropertyType.Integer)queryHeight.intValue=1000;
                else throw new InvalidOperationException("Installed EasyRoads terrain ray-query height must be numeric");
                settings.ApplyModifiedPropertiesWithoutUndo();
                owner.RoadNetworkInit();
                var network = new ERRoadNetwork(owner);
                var actualReturnedRoads=new List<ERRoad>();var actualHairpinGrades=new List<ERRoad>();bool omitHairpinGrades=NativeHairpinGradePolicyRequested();if(omitHairpinGrades&&!nativeTypeDefaults)throw new InvalidOperationException("Explicit hairpin subset grading requires native source-road defaults; legacy/default authoring remains unchanged.");
                ERRoad CreateRoad(string name,ERRoadType type,Vector3[] points,ERMarkerControlType[] controls)
                {if(!TryCreateExplicitOwnedMountainGrade(network,owner,type,name,points,controls,out var road))road=network.CreateRoad(name,type,points,controls);actualReturnedRoads.Add(road);observe?.Invoke(new DragonKingdomGradeObservation{stage="roadCreated",road=road,assignedRoadName=name,assignedRoadTypeName=type.roadTypeName});return road;}
                var gradingMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/EasyRoads3D/Resources/Materials/roads/road material.mat");
                foreach(string settlement in new[]{"village","harbor"})
                foreach(var house in settlement=="village"?VillageHouseSites():HarborHouseSites())
                {
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(house.Item1);if(prefab==null)throw new InvalidOperationException("Owned village foundation model missing: "+house.Item1);
                    var measurement=(GameObject)PrefabUtility.InstantiatePrefab(prefab,instance.transform);
                    Bounds footprint;
                    try
                    {
                        measurement.transform.localPosition=Vector3.zero;measurement.transform.localRotation=Quaternion.Euler(0,house.Item3,0);
                        footprint=BoundsOf(measurement.transform);measurement.transform.localScale*=house.Item4/footprint.size.y;footprint=BoundsOf(measurement.transform);
                    }
                    finally{UnityEngine.Object.DestroyImmediate(measurement);}
                    Vector3 center=Regions.First(r=>r.id==settlement).center+house.Item2;
                    var type=new ERRoadType{roadTypeName="Owned village house foundation",roadWidth=footprint.size.x+4,hasMeshCollider=false,roadMaterial=gradingMaterial};
                    var road=CreateRoad("Owned house foundation "+System.IO.Path.GetFileNameWithoutExtension(house.Item1),type,new[]{center-Vector3.forward*(footprint.size.z*.5f+2),center+Vector3.forward*(footprint.size.z*.5f+2)},new[]{ERMarkerControlType.StraightXZY,ERMarkerControlType.StraightXZY});
                    road.SetTerrainDeformation(true,false);road.SnapToTerrain(false,false);for(int i=0;i<2;i++){Indent(road,2,i);Surrounding(road,4,i);}road.Refresh();Updated(road);
                }
                foreach (var region in Regions)
                {
                    if(underground.HasValue&&region.id=="caverns")continue;
                    // Vendor road grading also supplies the authored flat staging
                    // apron beneath architecture; no custom height generator.
                    // Flat junction plazas cannot overwrite a different-height
                    // road hundreds of metres away. Castle architecture has its
                    // own northward sanctuary footprint beyond the front hub.
                    float half = region.id == "castle" ? 150 : 36;
                    Vector3 center = region.center + (region.id == "castle" ? P(0,0,220) : Vector3.zero);
                    var type = new ERRoadType { roadTypeName = region.id + " staging apron", roadWidth = region.id == "castle" ? 300 : 72, hasMeshCollider = false, roadMaterial = gradingMaterial };
                    var road = CreateRoad(region.id + " grading", type, new[] { center+Vector3.left*half, center+Vector3.right*half },new[]{ERMarkerControlType.StraightXZY,ERMarkerControlType.StraightXZY});
                    road.SetTerrainDeformation(true, false); road.SnapToTerrain(false, false); road.Refresh();Updated(road);
                }
                Stage("afterHouseAndRegionalApronCreation");
                // Protected static extraction endpoints need the same real
                // foundation as the regional hubs. The harbor ramp/deck stay
                // owned by the actual ramp and moving hull, never a static pad.
                foreach(var junction in JunctionPositions().Where(n=>!Regions.Any(r=>r.id==n.Key)&&n.Key!="harbor-ramp"&&n.Key!="boat-deck"))
                {
                    if(underground.HasValue&&InsideXZ(underground.Value,junction.Value))continue;
                    var type=new ERRoadType{roadTypeName=junction.Key+" extraction staging",roadWidth=72,hasMeshCollider=false,roadMaterial=gradingMaterial};
                    var road=CreateRoad(junction.Key+" staging grading",type,new[]{junction.Value+Vector3.forward*36,junction.Value-Vector3.forward*36},new[]{ERMarkerControlType.StraightXZY,ERMarkerControlType.StraightXZY});
                    road.SetTerrainDeformation(true,false);road.SnapToTerrain(false,false);road.Refresh();Updated(road);
                }
                // Staging aprons are the broad architectural foundation. Roads
                // must deform AFTER them: otherwise a flat apron overwrites an
                // incoming graded road (V24: castle apron at z320 raised ground
                // to60m over the still55m drivable mesh). Preserve the actual
                // approach grade through the apron instead of raising road art
                // or weakening the physical wheel/body-clearance checks.
                var specifications = Roads();
                var bridges = nativePhysicalCrossings
                    ? DragonKingdomNativeRoadCrossings.Find(specifications,CanyonWaterPolyline(),3.125f).Select(row=>new BridgeSpan{upper=specifications.Single(spec=>spec.id==row.upper),lower=row.lower,start=row.start,end=row.end,crossing=row.crossing,point=row.point,forward=row.upperForward}).ToList()
                    : BridgeSpans(specifications);
                int loadingIndex=0;
                foreach(var bay in NestLoadingBays())
                {
                    var type=new ERRoadType{roadTypeName="Nesting ledge loading bay",roadWidth=24,hasMeshCollider=false,roadMaterial=gradingMaterial};
                    var road=CreateRoad("Nesting loading grading "+loadingIndex++,type,new[]{bay-Vector3.forward*12,bay+Vector3.forward*12},new[]{ERMarkerControlType.StraightXZY,ERMarkerControlType.StraightXZY});
                    road.SetTerrainDeformation(true,false);road.SnapToTerrain(false,false);for(int i=0;i<2;i++){Indent(road,2,i);Surrounding(road,4,i);}road.Refresh();Updated(road);
                }
                foreach(var turn in HairpinTurns(specifications))
                {
                    if(underground.HasValue&&InsideXZ(underground.Value,turn.Item3))continue;
                    var type=new ERRoadType{roadTypeName=turn.Item1+" hairpin turning area",roadWidth=42,hasMeshCollider=false,roadMaterial=gradingMaterial};
                    var road=CreateRoad(turn.Item1+" hairpin grading "+turn.Item2,type,new[]{turn.Item3-Vector3.forward*21,turn.Item3+Vector3.forward*21},new[]{ERMarkerControlType.StraightXZY,ERMarkerControlType.StraightXZY});
                    actualHairpinGrades.Add(road);
                    road.SetTerrainDeformation(true,false);road.SnapToTerrain(false,false);
                    for(int i=0;i<2;i++){Indent(road,3,i);Surrounding(road,6,i);}road.Refresh();Updated(road);
                }
                Stage("afterLoadingAndHairpinCreation");
                foreach (var spec in specifications.Where(r => r.id != "boarding-deck"))
                {
                    // StraightXZY is the installed vendor's XYZ-straight control:
                    // sparse authored points now match the piecewise-linear
                    // graph, avoiding both vertical spline overshoot and the
                    // 191-second dense-marker rebuild. Add only real bridge
                    // span boundary markers to the sparse alignment.
                    var spans = bridges.Where(b => b.upper.id == spec.id).ToList();
#if VISTA
                    // Only terrain-deformation exclusion is shared with the
                    // bridge marker API. This creates NO bridge/viaduct: the
                    // measured overhead field is later excavated by Digger.
                    if(underground.HasValue)
                    {
                        float along=0;
                        for(int i=1;i<spec.points.Length;i++)
                        {
                            var a=spec.points[i-1];var b=spec.points[i];
                            if(DragonKingdomDiggerCavernAuthoring.Clip(underground.Value,a,b,out var entry,out var exit))
                                spans.Add(new BridgeSpan{upper=spec,lower="Measured underground cavern exclusion",start=along+Vector3.Distance(a,entry),end=along+Vector3.Distance(a,exit)});
                            along+=Vector3.Distance(a,b);
                        }
                    }
#endif
                    foreach(var span in spans)observe?.Invoke(new DragonKingdomGradeObservation{stage="span",setting=spec.id+" / "+span.lower,requested=span.start,returned=span.end});
                    var points = GradingMarkers(spec,spans.ToArray());
                    var type = new ERRoadType { roadTypeName = spec.id, roadWidth = spec.width, hasMeshCollider = true, roadMaterial = gradingMaterial };
                    if(nativeTypeDefaults)RegisterOwnedNativeGradingType(type,owner,roadIndent,roadSurrounding);
                    var road = CreateRoad(spec.id, type, points, Enumerable.Repeat(ERMarkerControlType.StraightXZY, points.Length).ToArray());
                    road.SetTerrainDeformation(true, false); road.SnapToTerrain(false, false); road.SetResolution(.35f, false);
                    float distance = 0;
                    for (int i = 0; i < points.Length; i++)
                    {
                        float previous = distance;if(i>0)distance+=Vector3.Distance(points[i-1],points[i]);
                        Indent(road,roadIndent,i);Surrounding(road,roadSurrounding,i);
                        if(i>0 && spans.Any(b => (previous+distance)*.5f>b.start && (previous+distance)*.5f<b.end))
                        { road.SetTerrainDeformation(i-1,false);road.SetStartLevelDistance(i-1,16);road.SetEndLevelDistance(i-1,16);Setting(road,"actualTerrainDeformationOffInterval",i-1,previous,distance); }
                    }
                    if(nativeTypeDefaults)ConfigureOwnedNativeRoadGlobals(road,type,owner,roadIndent,roadSurrounding);
                    road.Refresh();Updated(road);
                }
                Stage("afterRoadCreationBeforeWaterCreation");
                foreach (var channel in new[]
                {
                    new RoadSpec("lakebed", "", "", 550, P(100,-8,-930), P(650,-8,-930), P(1300,-8,-930)),
                    new RoadSpec("canyon-stream-bed", "", "", 60, P(-1050,167,700), P(-1050,162,580), P(-1030,127,500), P(-1010,32,420), P(-950,5,250), P(-800,-5,-50), P(-600,-5,-450), P(100,-8,-930))
                })
                {
                    var type = new ERRoadType { roadTypeName = channel.id, roadWidth = channel.width, hasMeshCollider = false, roadMaterial = gradingMaterial };
                    var road = CreateRoad(channel.id, type, channel.points, Enumerable.Repeat(ERMarkerControlType.StraightXZY, channel.points.Length).ToArray());
                    road.SetTerrainDeformation(true, false); road.SnapToTerrain(false, false); road.Refresh();Updated(road);
                }
                Stage("beforeSingleVendorBuild");
                if(omitHairpinGrades)BuildNativeWithoutHairpinGradeOverwrites(network,owner,actualReturnedRoads,actualHairpinGrades);else network.BuildRoadNetwork();
                Stage("afterSingleVendorBuildBeforeDestroy");
                if(smoothNativeNe)
                {
                    Stage("beforeNativeSdkEdgeSmoothing");DragonKingdomVendorFalloffAuthoring.ApplyMeasuredNePair(network,owner,terrains,actualReturnedRoads,roadSurrounding,underground);Stage("afterNativeSdkEdgeSmoothing");
                }
                // Preserve the vendor-authored terrain output if its temporary
                // authoring network restores backups when destroyed. These are
                // snapshots of vendor output, not procedurally invented heights.
                var heights = terrains.Select(t => t.terrainData.GetHeights(0,0,t.terrainData.heightmapResolution,t.terrainData.heightmapResolution)).ToArray();
                UnityEngine.Object.DestroyImmediate(instance); instance = null;
                for (int i = 0; i < terrains.Length; i++) terrains[i].terrainData.SetHeights(0,0,heights[i]);
                Stage("afterNetworkDestroyAndExactVendorOutputRestore");
            }
            finally { if (instance != null) UnityEngine.Object.DestroyImmediate(instance); }
        }

        [Serializable] sealed class PhysicalRoadAudit
        {
            public string scope="NATIVE_PHYSX_ALL_ROAD_CONTACTS_BOUNDED_DETAILS_NOT_ACCEPTED_IF_ANY_FAILURE";
            public int testedContacts,totalFailures,detailedFailureLimit=512,blockingRootLimit=256;
            public List<PhysicalRoadFailure> failures=new List<PhysicalRoadFailure>();
            public List<PhysicalBlockingRoot> blockingRoots=new List<PhysicalBlockingRoot>();
            public List<PhysicalEdgeCategory> countsByEdgeCategory=new List<PhysicalEdgeCategory>();
        }
        [Serializable]sealed class PhysicalEdgeCategory{public string edge,category;public int count;}
        [Serializable] sealed class PhysicalRoadFailure {public string category,edge;public int sample;public float lateralOffset;public Vector3 expected,support;public string[] blockers;}
        [Serializable] sealed class PhysicalBlockingRoot {public string hierarchy,sourceAsset;public Bounds bounds;public int failureRecords;public List<string> colliderPaths=new List<string>();}

        public static void ValidatePhysicalRoads(Transform world, LwsRoadGraph graph)
        {
            var roadContacts = new HashSet<Collider>(world.GetComponentsInChildren<MeshCollider>().Where(c => c.transform.parent != null && c.transform.parent.name.StartsWith("Kingdom ", StringComparison.Ordinal)));
            var deck = world.Find("Lanternwater Harbor/Timber evacuation barge");
            var ramp = world.Find("Lanternwater Harbor/Barge loading ramp");
            foreach (var c in deck.GetComponentsInChildren<Collider>().Concat(ramp.GetComponentsInChildren<Collider>()).Where(c => !c.isTrigger)) roadContacts.Add(c);
            var audit=new PhysicalRoadAudit();var roots=new Dictionary<Transform,PhysicalBlockingRoot>();
            Action<string,string,int,float,Vector3,Vector3,IEnumerable<Collider>> record=(category,edge,index,lateral,expected,support,contacts)=>
            {
                audit.totalFailures++;
                var aggregate=audit.countsByEdgeCategory.FirstOrDefault(a=>a.edge==edge&&a.category==category);if(aggregate==null){aggregate=new PhysicalEdgeCategory{edge=edge,category=category};audit.countsByEdgeCategory.Add(aggregate);}aggregate.count++;
                var blocked=contacts.Where(c=>c!=null).Distinct().ToArray();
                if(audit.failures.Count<audit.detailedFailureLimit)audit.failures.Add(new PhysicalRoadFailure{category=category,edge=edge,sample=index,lateralOffset=lateral,expected=expected,support=support,blockers=blocked.Take(16).Select(c=>DescribeColliders(new[]{c})).ToArray()});
                foreach(var group in blocked.GroupBy(c=>BlockingRoot(c,world)))
                {
                    var root=group.Key;
                    if(!roots.TryGetValue(root,out var row))
                    {
                        if(roots.Count>=audit.blockingRootLimit)continue;
                        Bounds bounds=TryMeasureOwnedModelBounds(root,out var measured,true)?measured:group.First().bounds;
                        row=new PhysicalBlockingRoot{hierarchy=TransformPath(root),sourceAsset=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root.gameObject),bounds=bounds};roots.Add(root,row);audit.blockingRoots.Add(row);
                    }
                    row.failureRecords++;foreach(var c in group)if(row.colliderPaths.Count<16){string path=TransformPath(c.transform);if(!row.colliderPaths.Contains(path))row.colliderPaths.Add(path);}
                }
            };
            foreach (var edge in graph.edges)
            for(int sampleIndex=0;sampleIndex<edge.samples.Count;sampleIndex++)
            {
                var sample=edge.samples[sampleIndex];
                var right = Vector3.Cross(Vector3.up, sample.forward).normalized;
                foreach (float offset in new[] { -.35f, 0, .35f })
                {
                    Vector3 p = sample.position + right * sample.roadWidthMeters * offset;
                    var hits = Physics.RaycastAll(p+Vector3.up*8, Vector3.down, 16, ~0, QueryTriggerInteraction.Ignore);
                    var surface = hits.Where(h => roadContacts.Contains(h.collider)).OrderBy(h => Mathf.Abs(h.point.y-p.y)).FirstOrDefault();
                    if (surface.collider == null || Mathf.Abs(surface.point.y-p.y) > .6f)
                        record("wheel-support",edge.edgeId,sampleIndex,offset,p,surface.collider!=null?surface.point:p,hits.Select(h=>h.collider));
                    Vector3 support=surface.collider!=null?surface.point:p;
                    var obstructions = hits.Where(h => !roadContacts.Contains(h.collider) && h.point.y > support.y+.3f).ToArray();
                    if (obstructions.Length != 0)
                        record("support-obstruction",edge.edgeId,sampleIndex,offset,p,support,obstructions.Select(h=>h.collider));
                    var overlaps = Physics.OverlapBox(support+Vector3.up*3.3f, P(1.6f,3,1.8f), Quaternion.LookRotation(sample.forward), ~0, QueryTriggerInteraction.Ignore);
                    // Other road decks are real roof obstacles too; exempt only
                    // this sample's supporting surface, never every road mesh.
                    var blocked = overlaps.Where(c => c != surface.collider).ToArray();
                    if (blocked.Length != 0)
                        record("body-roof-clearance",edge.edgeId,sampleIndex,offset,p,support,blocked);
                    audit.testedContacts++;
                }
            }
            string[] args=Environment.GetCommandLineArgs();int outputAt=Array.IndexOf(args,"-tsDragonOutput");
            string folder=outputAt>=0&&outputAt+1<args.Length?args[outputAt+1]:"Logs/TruckSports/DragonKingdom/WorldPhysicalAudit";
            Directory.CreateDirectory(folder);string report=System.IO.Path.Combine(folder,"physical-roads-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".json");
            File.WriteAllText(report,JsonUtility.ToJson(audit,true));
            Debug.Log("DRAGON_KINGDOM_PHYSICAL_ROUTE_SAMPLES "+audit.testedContacts+" across="+graph.edges.Count+" failures="+audit.totalFailures+" uniqueBlockingRoots="+audit.blockingRoots.Count+" report="+report);
            if(audit.totalFailures!=0)throw new InvalidOperationException("Native road acceptance failed: "+audit.totalFailures+" failing support/clearance records across "+audit.testedContacts+" contacts; "+audit.blockingRoots.Count+" unique blocking roots; bounded structured report="+report+" first="+JsonUtility.ToJson(audit.failures[0]));
        }
        static string TransformPath(Transform t){string path=t.name;while(t.parent!=null){t=t.parent;path=t.name+"/"+path;}return path;}
        static Transform BlockingRoot(Collider collider,Transform world)
        {
            Transform chosen=null;
            for(var t=collider.transform;t!=null&&t!=world;t=t.parent)if(PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))chosen=t;
            return chosen!=null?chosen:collider.transform;
        }

        static string DescribeColliders(IEnumerable<Collider> colliders)
        {
            return string.Join("; ", colliders.Distinct().Select(c =>
            {
                string path = c.name;
                for (var parent = c.transform.parent; parent != null; parent = parent.parent) path = parent.name + "/" + path;
                string asset = c is TerrainCollider terrain ? AssetDatabase.GetAssetPath(terrain.terrainData) : c is MeshCollider mesh ? AssetDatabase.GetAssetPath(mesh.sharedMesh) : "";
                return path + " [" + c.GetType().Name + " boundsCenter=" + c.bounds.center + " boundsSize=" + c.bounds.size + " asset=" + asset + "]";
            }));
        }

        public static void ValidateAuthoredBindings(DragonKingdomWorld world)
        {
            world.ValidateReferences();
            ValidateTruckStarts(world,false);
            if (world.regions.Length != 9 || world.regions.Any(r => r.truckStarts.Length != 4))
                throw new InvalidOperationException("Every shared kingdom region requires four distinct authored truck starts");
            foreach (var renderer in world.GetComponentsInChildren<Renderer>(true))
                foreach (var material in renderer.sharedMaterials)
                    if (material == null || material.shader == null || !material.shader.isSupported || material.shader.name == "Hidden/InternalErrorShader")
                        throw new InvalidOperationException("Missing/unsupported native material on " + renderer.name);
            foreach (var terrain in world.GetComponentsInChildren<Terrain>(true))
                if (terrain.terrainData == null || terrain.terrainData.terrainLayers.Length == 0 || terrain.materialTemplate == null || !terrain.materialTemplate.shader.isSupported)
                    throw new InvalidOperationException("Vista terrain requires real textured layer/material output");
            var boat = world.Extraction("boat").vessel;
            if (boat == null || !boat.hull.isKinematic || Vector3.Distance(boat.hull.transform.lossyScale,Vector3.one) > .001f
                || boat.supportingDeck.Length == 0 || boat.supportingDeck.Any(c => c == null || c.isTrigger || c.attachedRigidbody != boat.hull)
                || boat.departureWaypoints.Length == 0 || boat.departureWaypoints.Any(t => t == null || t.IsChildOf(boat.hull.transform)))
                throw new InvalidOperationException("Moving barge physical ownership/departure contract failed");
            var spaces = boat.transform.Cast<Transform>().Where(t => t.name.StartsWith("Truck parking ",StringComparison.Ordinal)).ToArray();
            if (spaces.Length != 4) throw new InvalidOperationException("Barge requires four actual parking locations");
            for (int i = 0; i < spaces.Length; i++)
            {
                if (!boat.parkingZone.bounds.Contains(spaces[i].position)) throw new InvalidOperationException("Barge parking anchor falls outside its secure interior");
                for (int j = 0; j < i; j++) if (Vector3.Distance(spaces[i].position,spaces[j].position) < 10)
                    throw new InvalidOperationException("Barge parking locations overlap the intended full-truck separation");
            }
            Debug.Log("DRAGON_KINGDOM_NATIVE_BINDINGS regions=9 extractionTypes=3 parkingSpaces=4 materialBindings=present");
        }

        public static void ValidateTruckStarts(DragonKingdomWorld world,bool placeWithinAuthoredPad)
        {
            var roadSurfaces=new HashSet<Collider>(world.GetComponentsInChildren<MeshCollider>().Where(c=>c.transform.parent!=null&&c.transform.parent.name.StartsWith("Kingdom ",StringComparison.Ordinal)));
            int checkedStarts=0;
            foreach(var region in world.regions)
            {
                if(region.truckStarts.Length!=4)throw new InvalidOperationException("Four actual supported starts required: "+region.id);
                var assigned=new List<Vector3>();var floorHeights=new List<float>();
                for(int slot=0;slot<4;slot++)
                {
                    var anchor=region.truckStarts[slot];string reason="";bool found=false;Vector3 floor=default;
                    var candidates=new List<Vector3>();
                    if(placeWithinAuthoredPad)
                    {
                        candidates.Add(region.root.position+P(slot%2==0?-20:20,1,slot<2?-22:22));
                        foreach(float z in new[]{-24f,24f,-12f,12f,0f})foreach(float x in new[]{-24f,24f,-12f,12f,0f})candidates.Add(region.root.position+P(x,1,z));
                    }
                    else candidates.Add(anchor.position);
                    foreach(var candidate in candidates)
                    {
                        // Full tractor footprint plus room to step into the cab:
                        // 4.4m wide, 16m long, 6m high. Candidates stay inside
                        // the actual72m staging pad rather than snapping to a
                        // distant/lower surface or accepting a decorative roof.
                        if(assigned.Any(p=>Mathf.Abs(p.x-candidate.x)<6.4f&&Mathf.Abs(p.z-candidate.z)<18))continue;
                        if(!SupportedStart(candidate,region.root.position.y,roadSurfaces,out floor,out reason))continue;
                        found=true;
                        if(placeWithinAuthoredPad){anchor.SetPositionAndRotation(floor+Vector3.up,Quaternion.identity);Physics.SyncTransforms();}
                        break;
                    }
                    if(!found)throw new InvalidOperationException("No genuinely supported/clear tractor start in "+region.id+" slot="+slot+" reason="+reason);
                    assigned.Add(anchor.position);floorHeights.Add(floor.y);checkedStarts++;
                }
                if(floorHeights.Max()-floorHeights.Min()>.15f)throw new InvalidOperationException("Starting crews need comparable flat physical footing: "+region.id+" heightSpread="+(floorHeights.Max()-floorHeights.Min()));
            }
            Debug.Log("DRAGON_KINGDOM_SUPPORTED_TRUCK_STARTS "+checkedStarts+" regions="+world.regions.Length+" physicalFootprint=4.4x16m bodyHeight=6m comparableFloorSpread<=.15m");
        }
        static bool SupportedStart(Vector3 candidate,float expectedFloor,HashSet<Collider> roads,out Vector3 floor,out string reason)
        {
            floor=default;reason="";float low=float.PositiveInfinity,high=float.NegativeInfinity;
            foreach(var offset in new[]{Vector3.zero,P(-2,0,-7.5f),P(2,0,-7.5f),P(-2,0,7.5f),P(2,0,7.5f)})
            {
                var hits=Physics.RaycastAll(candidate+offset+Vector3.up*3,Vector3.down,8,~0,QueryTriggerInteraction.Ignore);
                var support=hits.Where(h=>(h.collider is TerrainCollider||roads.Contains(h.collider))&&Mathf.Abs(h.point.y-expectedFloor)<=.4f&&h.normal.y>.97f).OrderBy(h=>Mathf.Abs(h.point.y-expectedFloor)).FirstOrDefault();
                if(support.collider==null){reason="Missing actual pad/road footing near "+(candidate+offset)+" expectedFloor="+expectedFloor;return false;}
                if(offset==Vector3.zero)floor=support.point;
                low=Mathf.Min(low,support.point.y);high=Mathf.Max(high,support.point.y);
            }
            if(high-low>.15f){reason="Footprint crosses a grade/step of "+(high-low);return false;}
            var blockers=Physics.OverlapBox(floor+Vector3.up*3.3f,P(2.2f,3,8),Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
            if(blockers.Length!=0){reason="Start roof/body blocked: "+DescribeColliders(blockers);return false;}
            return true;
        }

        static void Waterways(PreparedWorld world)
        {
            River(world.root, "Lanternwater Lake", 550, new[] { P(100,-2,-930), P(650,-2,-930), P(1300,-2,-930) });
            River(world.root, "Whisperwood waterfall canyon", 45, new[] { P(-1050,170,700), P(-1050,165,580), P(-1030,130,500), P(-1010,35,420), P(-950,8,250), P(-800,-2,-50), P(-600,-2,-450), P(100,-2,-930) });
            var lava = River(world.root, "Ashen Crown molten river", 55, new[] { P(1370,330,1050), P(1450,260,900), P(1410,160,600), P(1300,90,430) });
            var molten = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckSports/Materials/CarnivalLava/TruckFloorIsLava molten currents.mat");
            if (molten == null) throw new InvalidOperationException("Existing accepted molten-current presentation required");
            var presentation = lava.gameObject.AddComponent<LWS.TruckSports.SportsCarnivalLavaSurface>();
            presentation.surface = lava; presentation.originalMaterials = new[] { molten }; presentation.moltenMaterial = molten;
            presentation.surfaceHeight = 240; presentation.worldEnvelope = lava.bounds; lava.sharedMaterial = molten;
            var ambient = new List<ParticleSystem>();
            for (int i = 0; i < 3; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NWH/Vehicle Physics 2/Effects/Prefabs/SurfaceDust.prefab");
                var effect = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                effect.name = "Owned volcanic steam vent " + i; effect.transform.SetParent(lava.transform, false); effect.transform.position = P(1370+i*25,330-i*30,1050-i*65);
                foreach (var behaviour in effect.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(behaviour);
                foreach (var particle in effect.GetComponentsInChildren<ParticleSystem>(true))
                {
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var main = particle.main; main.loop = true; main.playOnAwake = true; main.startLifetime = 8; main.startSpeed = 10; main.startSize = 12; main.maxParticles = 96;
                    main.startColor = new Color(.3f,.27f,.25f,.5f);
                    var emission = particle.emission; emission.rateOverTime = 5;
                    particle.GetComponent<ParticleSystemRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckSports/Materials/CarnivalLava/NWH soft volcanic steam.mat");
                    ambient.Add(particle);
                }
            }
            presentation.ambientEffects = ambient.ToArray();
        }

        static void PreviewPresentation(Transform container)
        {
            // Dedicated scene inspection objects stay outside the runtime prefab;
            // Sports/Weather Maker own production cameras and illumination.
            var sun = Child(container, "Preview sunlight").gameObject.AddComponent<Light>();
            sun.type = LightType.Directional; sun.color = new Color(1,.89f,.72f); sun.intensity = 1.4f;
            sun.shadows = LightShadows.Soft; sun.transform.rotation = Quaternion.Euler(42,-32,0);
            var camera = Child(container, "Kingdom preview camera").gameObject.AddComponent<Camera>();
            camera.transform.position = P(1300,950,-1600); camera.transform.LookAt(P(0,160,600));
            camera.nearClipPlane = .3f; camera.farClipPlane = 6500; camera.fieldOfView = 58;
        }

        static MeshRenderer River(Transform root, string name, float width, Vector3[] points)
        {
            var holder = Child(root, name);
            var spline = holder.gameObject.AddComponent<SplineContainer>();
            spline.Spline = new Spline();
            foreach (var p in points) spline.Spline.Add(new BezierKnot(new float3(p.x,p.y,p.z)), TangentMode.Linear);
            var mesh = holder.gameObject.AddComponent<MeshFilter>();
            var renderer = holder.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/RiverModeler/ShaderGraph/Default River.mat");
            if (renderer.sharedMaterial == null) throw new InvalidOperationException("Installed RiverModeler water material required");
            var river = holder.gameObject.AddComponent<RiverModeler>();
            river.meshFilter = mesh; river.splineContainer = spline; river.settings.shape.width = width;
            river.settings.triangulation.vertexDistance = width > 100 ? 16 : 4;
            river.settings.triangulation.vertexDistanceWidth = width > 100 ? 16 : 3;
            river.rebuildTriggers = RiverModeler.RebuildTriggers.None; river.Rebuild();
            if (mesh.sharedMesh == null || mesh.sharedMesh.vertexCount == 0) throw new InvalidOperationException("RiverModeler generated no actual water geometry");
            AssetDatabase.CreateAsset(mesh.sharedMesh, "Assets/" + Folder + "/Models/" + name + ".asset");
            return renderer;
        }

        static DragonRegionDefinition Region(string id, string label, Vector3 center, Vector3 size)
            => new DragonRegionDefinition { id = id, label = label, center = center, bounds = new Bounds(center, size) };

        public static readonly DragonRegionDefinition[] Regions =
        {
            Region("village", "Emberwick Village", new Vector3(-560, 0, -360), new Vector3(360, 100, 320)),
            Region("castle", "Crownwatch Castle", new Vector3(0, 60, 360), new Vector3(340, 180, 340)),
            Region("harbor", "Lanternwater Harbor", new Vector3(600, 0, -480), new Vector3(400, 120, 380)),
            Region("forest", "Whisperwood", new Vector3(-760, 24, 280), new Vector3(520, 180, 560)),
            Region("mountains", "Wyrmspine Pass", new Vector3(0, 300, 1040), new Vector3(600, 500, 560)),
            Region("refuge", "Stoneheart Refuge", new Vector3(-360, 330, 1200), new Vector3(300, 220, 340)),
            Region("caverns", "The Gilded Deep", new Vector3(520, 105, 780), new Vector3(420, 220, 380)),
            Region("volcano", "Ashen Crown", new Vector3(1040, 240, 840), new Vector3(480, 600, 500)),
            Region("nests", "Skycradle Cliffs", new Vector3(-840, 270, 920), new Vector3(420, 320, 380))
        };

        public sealed class PreparedRegion
        {
            public DragonRegionDefinition spec;
            public Transform root;
            public readonly List<Transform> spawns = new List<Transform>();
            public readonly List<Transform> perches = new List<Transform>();
            public readonly List<Transform> nests = new List<Transform>();
            public readonly List<Transform> treasure = new List<Transform>();
        }

        public sealed class PreparedWorld
        {
            public Transform root;
            public readonly Dictionary<string, PreparedRegion> regions = new Dictionary<string, PreparedRegion>();
            public readonly Dictionary<string, Transform> anchors = new Dictionary<string, Transform>();
            public readonly Dictionary<string, Collider> secureZones = new Dictionary<string, Collider>();
            public Transform vessel, ramp;
            public LwsRoadGraph graph;
        }

        public sealed class RoadSpec
        {
            public string id, from, to;
            public float width;
            public Vector3[] points;
            public RoadSpec(string id, string from, string to, float width, params Vector3[] points)
            { this.id = id; this.from = from; this.to = to; this.width = width; this.points = points; }
        }

        static Vector3 P(float x, float y, float z) => new Vector3(x, y, z);

        // Shared endpoint positions are exact. Switchbacks make elevation gains
        // explicit rather than drawing impossible direct cliff links.
        public static RoadSpec[] Roads()
        {
            var raw=new[]
            {
            new RoadSpec("village-castle", "village", "castle", 22, P(-560,0,-360), P(-380,15,-80), P(-180,35,160), P(0,60,360)),
            new RoadSpec("village-forest", "village", "forest", 18, P(-560,0,-360), P(-850,10,-160), P(-930,18,80), P(-760,24,280)),
            new RoadSpec("forest-castle", "forest", "castle", 18, P(-760,24,280), P(-550,35,430), P(-300,48,290), P(0,60,360)),
            new RoadSpec("village-harbor", "village", "harbor", 22, P(-560,0,-360), P(-180,0,-600), P(220,0,-650), P(600,0,-480)),
            new RoadSpec("castle-harbor", "castle", "harbor", 22, P(0,60,360), P(200,40,-120), P(480,18,-120), P(600,0,-480)),
            new RoadSpec("castle-pass", "castle", "mountains", 20, P(0,60,360), P(420,95,430), P(220,145,1020), P(760,195,1250), P(-240,265,1360), P(0,300,1040)),
            new RoadSpec("forest-nests", "forest", "nests", 16, P(-760,24,280), P(-1300,90,430), P(-650,170,540), P(-1300,220,650), P(-600,242,760), P(-840,270,920)),
            new RoadSpec("nests-refuge", "nests", "refuge", 18, P(-840,270,920), P(-900,290,1140), P(-650,315,1320), P(-360,330,1200)),
            new RoadSpec("pass-refuge", "mountains", "refuge", 20, P(0,300,1040), P(-360,330,1200)),
            new RoadSpec("castle-caverns", "castle", "caverns", 20, P(0,60,360), P(260,76,220), P(700,88,520), P(700,105,780), P(520,105,780)),
            new RoadSpec("harbor-caverns", "harbor", "caverns", 20, P(600,0,-480), P(960,25,-180), P(1040,50,130), P(790,78,420), P(700,105,780), P(520,105,780)),
            new RoadSpec("forest-caverns", "forest", "caverns", 18, P(-760,24,280), P(-300,55,900), P(80,84,780), P(520,105,780)),
            new RoadSpec("caverns-volcano", "caverns", "volcano", 18, P(520,105,780), P(700,105,780), P(780,140,1200), P(1180,195,1200), P(1280,215,1000), P(1040,240,840)),
            new RoadSpec("volcano-pass", "volcano", "mountains", 18, P(1040,240,840), P(800,270,1330), P(500,290,1530), P(0,300,1040)),
            new RoadSpec("castle-refuge-inner", "castle", "fortress-inner", 22, P(0,60,360), P(0,60,620)),
            new RoadSpec("mountain-refuge-inner", "refuge", "shelter-inner", 20, P(-360,330,1200), P(-360,330,1300)),
            new RoadSpec("harbor-loading", "harbor", "harbor-ramp", 22, P(600,0,-480), P(680,0,-600), P(720,0,-600), P(720,0,-640)),
            new RoadSpec("boarding-deck", "harbor-ramp", "boat-deck", 22, P(720,0,-640), P(720,0,-685))
            };
            var shared=new HashSet<Vector3>(raw.SelectMany(r=>r.points).GroupBy(p=>p).Where(g=>g.Count()>1).Select(g=>g.Key));
            return SelectExplicitOwnedMountainSpecifications(raw.Select(FlatJunctionApproaches).Select(r=>FlatSharedInteriorJunctions(r,shared)).Select(FlatInteriorHairpins).ToArray());
        }

        static RoadSpec FlatJunctionApproaches(RoadSpec source)
        {
            var points=source.points.ToList();
            if(Mathf.Abs(points[1].y-points[0].y)>.01f)
            {
                float distance=Vector3.ProjectOnPlane(points[1]-points[0],Vector3.up).magnitude;
                var portal=Vector3.Lerp(points[0],points[1],Mathf.Min(80,distance*.4f)/distance);portal.y=points[0].y;points.Insert(1,portal);
            }
            int last=points.Count-1;
            if(Mathf.Abs(points[last-1].y-points[last].y)>.01f)
            {
                float distance=Vector3.ProjectOnPlane(points[last-1]-points[last],Vector3.up).magnitude;
                var portal=Vector3.Lerp(points[last],points[last-1],Mathf.Min(80,distance*.4f)/distance);portal.y=points[last].y;points.Insert(last,portal);
            }
            return new RoadSpec(source.id,source.from,source.to,source.width,points.ToArray());
        }

        static RoadSpec FlatSharedInteriorJunctions(RoadSpec source,HashSet<Vector3> shared)
        {
            var points=new List<Vector3>{source.points[0]};
            for(int i=1;i<source.points.Length-1;i++)
            {
                var point=source.points[i];var previous=source.points[i-1];var next=source.points[i+1];
                if(shared.Contains(point))
                {
                    if(Mathf.Abs(point.y-previous.y)>.01f){var direction=Vector3.ProjectOnPlane(point-previous,Vector3.up);points.Add(point-direction.normalized*Mathf.Min(132,direction.magnitude*.49f));}
                    points.Add(point);
                    if(Mathf.Abs(point.y-next.y)>.01f){var direction=Vector3.ProjectOnPlane(next-point,Vector3.up);points.Add(point+direction.normalized*Mathf.Min(132,direction.magnitude*.49f));}
                }
                else points.Add(point);
            }
            points.Add(source.points[source.points.Length-1]);return new RoadSpec(source.id,source.from,source.to,source.width,points.ToArray());
        }
        static bool IsHairpin(Vector3 previous,Vector3 point,Vector3 next)
            => Vector3.Angle(Vector3.ProjectOnPlane(point-previous,Vector3.up),Vector3.ProjectOnPlane(next-point,Vector3.up))>=45;
        static RoadSpec FlatInteriorHairpins(RoadSpec source)
        {
            var points=new List<Vector3>{source.points[0]};
            for(int i=1;i<source.points.Length-1;i++)
            {
                Vector3 previous=source.points[i-1],point=source.points[i],next=source.points[i+1];
                if(IsHairpin(previous,point,next))
                {
                    Vector3 incoming=Vector3.ProjectOnPlane(point-previous,Vector3.up).normalized,outgoing=Vector3.ProjectOnPlane(next-point,Vector3.up).normalized;
                    // Cover the42m turning pavement plus its native terrain
                    // indent/surrounding influence before resuming the grade.
                    // Opposing graded legs of a sharp turn must separate by the
                    // complete road/indent/surrounding envelope before either
                    // starts climbing. A fixed34m throat overlaps on151degree turns.
                    float angle=Vector3.Angle(incoming,outgoing)*Mathf.Deg2Rad;
                    float throat=Mathf.Max(34,(source.width+16)/(2*Mathf.Cos(angle*.5f)));
                    float before=Mathf.Min(throat,Vector3.ProjectOnPlane(point-previous,Vector3.up).magnitude*.4f),after=Mathf.Min(throat,Vector3.ProjectOnPlane(next-point,Vector3.up).magnitude*.4f);
                    points.Add(point-incoming*before);points.Add(point);points.Add(point+outgoing*after);
                }
                else points.Add(point);
            }
            points.Add(source.points[source.points.Length-1]);return new RoadSpec(source.id,source.from,source.to,source.width,points.ToArray());
        }
        static IEnumerable<Tuple<string,int,Vector3>> HairpinTurns(RoadSpec[] roads)
        {
            foreach(var road in roads)for(int i=1;i<road.points.Length-1;i++)if(IsHairpin(road.points[i-1],road.points[i],road.points[i+1]))yield return Tuple.Create(road.id,i,road.points[i]);
        }

        static Dictionary<string,Vector3> JunctionPositions()
        {
            var result=new Dictionary<string,Vector3>();
            foreach(var road in Roads()){AddNode(result,road.from,road.points[0]);AddNode(result,road.to,road.points[road.points.Length-1]);}
            result.Add("cavern-east-fork",P(700,105,780));
            return result;
        }
        static Vector3[] NestPockets()=>new[]{P(56,0,26),P(26,0,56),P(-56,0,-26),P(-26,0,-56)};
        static IEnumerable<Vector3> NestLoadingBays()
        {
            var center=Regions.First(r=>r.id=="nests").center;
            foreach(var pocket in NestPockets())yield return center+pocket*(1-24/pocket.magnitude);
        }

        sealed class BridgeSpan {public RoadSpec upper;public string lower;public float start,end,crossing,halfClearanceSpan;public Vector3 point,forward;}
        public static Dictionary<string,Vector2[]> StructuralRoadTerrainSpans()
            =>BridgeSpans(Roads()).GroupBy(b=>b.upper.id).ToDictionary(g=>g.Key,g=>g.Select(b=>new Vector2(b.start,b.end)).ToArray());
        public static Vector3[] CanyonWaterPolyline()=>new[]{P(-1050,170,700),P(-1050,165,580),P(-1030,130,500),P(-1010,35,420),P(-950,8,250),P(-800,-2,-50),P(-600,-2,-450),P(100,-2,-930)};
        public static Bounds CavernUndergroundBounds()=>new Bounds(P(560,135,780),P(400,140,260));
        static bool InsideXZ(Bounds bounds,Vector3 p)=>p.x>=bounds.min.x&&p.x<=bounds.max.x&&p.z>=bounds.min.z&&p.z<=bounds.max.z;
        static List<BridgeSpan> BridgeSpans(RoadSpec[] roads)
        {
            var result=new List<BridgeSpan>();
            roads=roads.Concat(new[]{new RoadSpec("waterfall-canyon-stream","","",45,
                P(-1050,170,700),P(-1050,165,580),P(-1030,130,500),P(-1010,35,420),
                P(-950,8,250),P(-800,-2,-50),P(-600,-2,-450),P(100,-2,-930))}).ToArray();
            for(int a=0;a<roads.Length;a++)for(int b=a+1;b<roads.Length;b++)
            {
                if(roads[b].id=="waterfall-canyon-stream"&&roads[a].id!="forest-nests")continue;
                float alongA=0;
                for(int i=1;i<roads[a].points.Length;i++)
                {
                    Vector3 p=roads[a].points[i-1],u=roads[a].points[i]-p;float alongB=0;
                    for(int j=1;j<roads[b].points.Length;j++)
                    {
                        Vector3 q=roads[b].points[j-1],v=roads[b].points[j]-q,w=q-p;float det=u.x*v.z-u.z*v.x;
                        if(Mathf.Abs(det)>.001f)
                        {
                            float t=(w.x*v.z-w.z*v.x)/det,s=(w.x*u.z-w.z*u.x)/det;
                            if(t>.0001f&&t<.9999f&&s>.0001f&&s<.9999f)
                            {
                                Vector3 pa=p+u*t,pb=q+v*s;float gap=Mathf.Abs(pa.y-pb.y);
                                if(gap>.3f && (roads[b].id!="waterfall-canyon-stream" || pa.y>pb.y))
                                {
                                    if(gap<9)throw new InvalidOperationException("Authored road crossing needs a genuine >=9m overpass or junction: "+roads[a].id+"/"+roads[b].id+" gap="+gap);
                                    bool upper=pa.y>pb.y;float d=upper?alongA+u.magnitude*t:alongB+v.magnitude*s;
                                    var spec=upper?roads[a]:roads[b];float length=PolylineLength(spec.points);
                                    float sine=Mathf.Abs(det)/(new Vector2(u.x,u.z).magnitude*new Vector2(v.x,v.z).magnitude);
                                    // The owned stone bridge aperture is only25percent of its
                                    // overall length at half its total height (measured real FBX).
                                    // Fit its OPENING to the whole lower road, not its outer bounds.
                                    float clearHalf=Mathf.Max(30,((upper?roads[b].width:roads[a].width)+20)/(2*sine)+8)/.22f;
                                    float noGradeHalf=Mathf.Max(clearHalf+24,((upper?roads[b].width:roads[a].width)*.5f+spec.width*.5f+10)/sine+24);
                                    result.Add(new BridgeSpan{upper=spec,lower=upper?roads[b].id:roads[a].id,crossing=d,start=Mathf.Max(0,d-noGradeHalf),end=Mathf.Min(length,d+noGradeHalf),halfClearanceSpan=clearHalf,point=upper?pa:pb,forward=(upper?u:v).normalized});
                                }
                            }
                        }
                        alongB+=v.magnitude;
                    }
                    alongA+=u.magnitude;
                }
            }
            for(int i=0;i<result.Count;i++)for(int j=result.Count-1;j>i;j--)
            {
                var a=result[i];var b=result[j];
                if(a.upper.id!=b.upper.id||a.lower!=b.lower||Vector3.Distance(a.point,b.point)>50)continue;
                float crossing=(a.crossing+b.crossing)*.5f;
                a.halfClearanceSpan=Mathf.Max(a.halfClearanceSpan+Mathf.Abs(crossing-a.crossing),b.halfClearanceSpan+Mathf.Abs(crossing-b.crossing));
                a.crossing=crossing;a.point=PointAtDistance(a.upper.points,crossing);a.start=Mathf.Min(a.start,b.start);a.end=Mathf.Max(a.end,b.end);result.RemoveAt(j);
            }
            return result;
        }
        static float PolylineLength(Vector3[] points){float d=0;for(int i=1;i<points.Length;i++)d+=Vector3.Distance(points[i-1],points[i]);return d;}
        static Vector3 PointAtDistance(Vector3[] points,float distance)
        {for(int i=1;i<points.Length;i++){float length=Vector3.Distance(points[i-1],points[i]);if(distance<=length)return Vector3.Lerp(points[i-1],points[i],distance/length);distance-=length;}return points[points.Length-1];}
        static Vector3[] GradingMarkers(RoadSpec road,BridgeSpan[] bridges)
        {
            if(TryExactOwnedMountainGradeMarkers(road,bridges,out var owned))return owned;
            var distances=new SortedSet<float>{0};float d=0;for(int i=1;i<road.points.Length;i++){d+=Vector3.Distance(road.points[i-1],road.points[i]);distances.Add(d);}
            foreach(var bridge in bridges){distances.Add(bridge.start);distances.Add(bridge.end);}
            return distances.Select(distance=>PointAtDistance(road.points,distance)).ToArray();
        }

        public static LwsRoadGraph BakeRoads(Transform root, Material road, PhysicsMaterial contact)
        {
            RequireStoppedEditor();
            var graph = new LwsRoadGraph { graphId = "dragon-kingdom-authored-v1" };
            var nodes = new Dictionary<string, Vector3>();
            foreach (var spec in Roads())
            {
                if (spec.points.Length < 2) throw new InvalidOperationException("Road needs two points: " + spec.id);
                AddNode(nodes, spec.from, spec.points[0]);
                AddNode(nodes, spec.to, spec.points[spec.points.Length - 1]);
                var samples = SamplePolyline(spec.points, 8);
                for (int i = 1; i < samples.Count; i++)
                {
                    var delta = samples[i] - samples[i - 1];
                    if (Mathf.Abs(delta.y) > new Vector2(delta.x, delta.z).magnitude * .18f)
                        throw new InvalidOperationException("Review excessive truck grade on " + spec.id);
                }
                // The boarding link is physically supplied by the real ramp and
                // hull deck. A static road mesh over the boat would incorrectly
                // remain at the dock after departure.
                if (spec.id != "boarding-deck")
                {
                    var baked = SportsEasyRoadsAuthoring.Bake(Folder, "Kingdom " + spec.id, samples.ToArray(), spec.width, road, contact, true);
                    baked.SetParent(root, true);
                }
                var edge = new LwsRoadEdge { roadId = "DK_ROAD_" + spec.id, segmentId = "DK_SEG_" + spec.id,
                    edgeId = spec.id, fromNodeId = spec.from, toNodeId = spec.to,
                    roadClass = LwsRoadClass.LocalRoad, direction = LwsRoadDirection.Bidirectional,
                    surfaceType = LwsRoadSurfaceType.Gravel, oneWay = false, laneCount = 2,
                    laneWidthMeters = spec.width * .5f, speedLimitMph = 35, sceneChunkId = "dragon-kingdom", stateOrRegionId = spec.from };
                float distance = 0;
                for (int i = 0; i < samples.Count; i++)
                {
                    if (i > 0) distance += Vector3.Distance(samples[i - 1], samples[i]);
                    Vector3 forward = (samples[Mathf.Min(i + 1, samples.Count - 1)] - samples[Mathf.Max(0, i - 1)]).normalized;
                    edge.samples.Add(new LwsRoadSample { roadId = edge.roadId, segmentId = edge.segmentId,
                        position = samples[i], forward = forward, up = Vector3.up, distanceFromStartMeters = distance,
                        direction = edge.direction, roadWidthMeters = spec.width, laneWidthMeters = edge.laneWidthMeters,
                        laneCount = 2, speedLimitMph = 35 });
                }
                edge.distanceMeters = edge.travelCost = distance;
                graph.edges.Add(edge);
            }
            foreach(var node in JunctionPositions().Where(n=>n.Key!="harbor-ramp"&&n.Key!="boat-deck"))
            {
                // Individual road end caps leave an uncovered outside wedge at
                // a branching node. A genuine vendor-baked common pavement
                // spans the node and also supports the four full tractor bays.
                // Both incoming lanes overlap this shared physical mesh; graph
                // samples keep their actual road alignments and tolerances.
                var junction=SportsEasyRoadsAuthoring.Bake(Folder,"Kingdom junction "+node.Key,new[]{node.Value-Vector3.forward*34,node.Value+Vector3.forward*34},68,road,contact,true);
                junction.SetParent(root,true);
            }
            foreach(var turn in HairpinTurns(Roads()))
            {
                var turning=SportsEasyRoadsAuthoring.Bake(Folder,"Kingdom hairpin "+turn.Item1+" "+turn.Item2,new[]{turn.Item3-Vector3.forward*21,turn.Item3+Vector3.forward*21},42,road,contact,true);
                turning.SetParent(root,true);
            }
            int bayIndex=0;
            foreach(var bay in NestLoadingBays())
            {
                var loading=SportsEasyRoadsAuthoring.Bake(Folder,"Kingdom nest loading bay "+bayIndex++,new[]{bay-Vector3.forward*12,bay+Vector3.forward*12},24,road,contact,true);loading.SetParent(root,true);
            }
            BuildOwnedViaducts(root,BridgeSpans(Roads()));
            graph.nodes = nodes.Select(n => new LwsRoadNode { nodeId = n.Key, position = n.Value, regionId = NodeRegion(n.Key), sceneChunkId = "dragon-kingdom" }).ToList();
            var validation = graph.Validate();
            if (!validation.IsValid) throw new InvalidOperationException(validation.Summary);
            return graph;
        }

        static void BuildOwnedViaducts(Transform root,List<BridgeSpan> bridges)
        {
            foreach(var group in bridges.GroupBy(b=>b.upper.id))
            foreach(var run in BridgeRuns(group.ToList()))
            {
                var bridge=run[0];float start=run.Min(b=>b.start),end=run.Max(b=>b.end);string lower=string.Join("+",run.Select(b=>b.lower).Distinct());
                var openings=new List<Vector2>();
                foreach(var crossing in run.OrderBy(b=>b.crossing-b.halfClearanceSpan))
                {
                    var opening=new Vector2(Mathf.Max(start,crossing.crossing-crossing.halfClearanceSpan),Mathf.Min(end,crossing.crossing+crossing.halfClearanceSpan));
                    if(openings.Count>0&&opening.x<=openings[openings.Count-1].y)
                    {var previous=openings[openings.Count-1];previous.y=Mathf.Max(previous.y,opening.y);openings[openings.Count-1]=previous;}
                    else openings.Add(opening);
                }
                var distances=new SortedSet<float>{start,end};float d=0;
                foreach(var opening in openings){distances.Add(opening.x);distances.Add(opening.y);}
                for(int i=1;i<bridge.upper.points.Length;i++){d+=Vector3.Distance(bridge.upper.points[i-1],bridge.upper.points[i]);if(d>start&&d<end&&!openings.Any(o=>d>o.x&&d<o.y))distances.Add(d);}
                float[] boundaries=distances.ToArray();int part=0;
                for(int i=1;i<boundaries.Length;i++)
                {
                    // Keep one genuine arch centered over the underpassing
                    // road. Splitting at the crossing would place a stone pier
                    // directly in the lower road instead of an open arch.
                    bool crossingArch=openings.Any(o=>(boundaries[i-1]+boundaries[i])*.5f>o.x&&(boundaries[i-1]+boundaries[i])*.5f<o.y);
                    int count=crossingArch?1:Mathf.CeilToInt((boundaries[i]-boundaries[i-1])/60);
                    for(int unit=0;unit<count;unit++)
                    {
                        float a=Mathf.Lerp(boundaries[i-1],boundaries[i],(float)unit/count),b=Mathf.Lerp(boundaries[i-1],boundaries[i],(float)(unit+1)/count);
                        Vector3 from=PointAtDistance(bridge.upper.points,a),to=PointAtDistance(bridge.upper.points,b),mid=(from+to)*.5f;
                        float ground=TerrainHeightAt(mid),height=Mathf.Max(12,mid.y-ground-.5f);
                        var arch=Place(root,Village+"props/P_PROP_bridge_stone_01.prefab",mid,0,10);
                        arch.name="Owned stone viaduct "+bridge.upper.id+" over "+lower+" span "+part++;
                        var filter=arch.GetComponent<MeshFilter>();
                        if(filter==null||filter.sharedMesh==null)throw new InvalidOperationException("Owned stone bridge root mesh is required for measured viaduct fitting");
                        Bounds bounds=filter.sharedMesh.bounds;bool longX=bounds.size.x>bounds.size.z;
                        arch.rotation=Quaternion.LookRotation((to-from).normalized)*Quaternion.Euler(0,longX?90:0,0);
                        arch.localScale=P((longX?Vector3.Distance(from,to)+.4f:bridge.upper.width+12)/bounds.size.x,height/bounds.size.y,(longX?bridge.upper.width+12:Vector3.Distance(from,to)+.4f)/bounds.size.z);
                        arch.position=mid-arch.TransformVector(P(bounds.center.x,bounds.max.y,bounds.center.z))-arch.up*.5f;
                        // A whole visible arch may span a grade transition.
                        // Lower its intact geometry enough to keep its highest
                        // masonry below every real upper-deck sample, including
                        // the flat approach throat; never trim its collider.
                        float drop=0;
                        for(float sample=a;sample<=b;sample+=2)
                        {
                            Vector3 deck=PointAtDistance(bridge.upper.points,sample);
                            float fraction=Mathf.InverseLerp(a,b,sample);
                            float top=Mathf.Lerp(from.y,to.y,fraction)-.5f;
                            drop=Mathf.Max(drop,top-(deck.y-.5f));
                        }
                        arch.position-=Vector3.up*drop;
                    }
                }
                Debug.Log("DRAGON_KINGDOM_OWNED_VIADUCT upper="+bridge.upper.id+" lower="+lower+" crossings="+run.Count+" nativeNoDeformationSpan="+start+".."+end+" realStoneArchUnits="+part);
            }
        }
        static IEnumerable<List<BridgeSpan>> BridgeRuns(List<BridgeSpan> source)
        {
            var sorted=source.OrderBy(b=>b.start).ToArray();int index=0;
            while(index<sorted.Length)
            {
                var run=new List<BridgeSpan>{sorted[index++]};float end=run[0].end;
                while(index<sorted.Length&&sorted[index].start<=end){end=Mathf.Max(end,sorted[index].end);run.Add(sorted[index++]);}
                yield return run;
            }
        }
        static float TerrainHeightAt(Vector3 point)
        {
            foreach(var terrain in Terrain.activeTerrains)
            {
                var local=point-terrain.transform.position;var size=terrain.terrainData.size;
                if(local.x>=0&&local.z>=0&&local.x<=size.x&&local.z<=size.z)return terrain.SampleHeight(point)+terrain.transform.position.y;
            }
            throw new InvalidOperationException("Actual Vista terrain foundation missing under owned bridge: "+point);
        }

        static string NodeRegion(string id) => id == "fortress-inner" ? "castle" : id == "shelter-inner" ? "refuge" : id == "harbor-ramp" || id == "boat-deck" ? "harbor" : id;

        public static DragonKingdomWorld BindRuntime(PreparedWorld prepared, DragonKingdomDefinition definition, LwsRoadGraph bakedGraph)
        {
            RequireStoppedEditor();
            if (prepared == null || definition == null || bakedGraph == null) throw new ArgumentNullException("Prepared geometry, definition and baked graph are required");
            definition.regions = Regions.Select(r =>
            {
                var physical = BoundsOf(prepared.regions[r.id].root); physical.Encapsulate(r.center);
                var result = Region(r.id, r.label, r.center, physical.size); result.bounds = physical; return result;
            }).ToArray();
            definition.graph = prepared.graph = bakedGraph;
            definition.escapeStarts = new[]
            {
                new DragonEscapeStartDefinition { id = "village-start", regionId = "village", nodeId = "village" },
                new DragonEscapeStartDefinition { id = "castle-start", regionId = "castle", nodeId = "castle" },
                new DragonEscapeStartDefinition { id = "volcano-start", regionId = "volcano", nodeId = "volcano" }
            };
            definition.extractions = new[]
            {
                Extraction(prepared, "fortress", "fortress-inner", DragonExtractionKind.Fortress),
                Extraction(prepared, "mountain", "shelter-inner", DragonExtractionKind.Mountain),
                Extraction(prepared, "boat", "boat-deck", DragonExtractionKind.Boat)
            };
            definition.closurePlans = new[]
            {
                new DragonEscapeClosurePlan { id = "village-treefall", edgeIds = new[] { "village-castle" } },
                new DragonEscapeClosurePlan { id = "burning-forest-bridge", edgeIds = new[] { "forest-castle" } },
                new DragonEscapeClosurePlan { id = "volcanic-rockfall", edgeIds = new[] { "caverns-volcano" } },
                new DragonEscapeClosurePlan { id = "harbor-market-collapse", edgeIds = new[] { "castle-harbor" } }
            };
            var vessel = prepared.vessel.gameObject.AddComponent<DragonEvacuationVessel>();
            vessel.hull = prepared.vessel.GetComponent<Rigidbody>();
            vessel.parkingZone = (BoxCollider)prepared.secureZones["boat"];
            vessel.supportingDeck = new Collider[] { prepared.vessel.GetComponent<BoxCollider>() };
            vessel.departureWaypoints = new[] { prepared.anchors["boat-departure"] };
            var world = prepared.root.gameObject.AddComponent<DragonKingdomWorld>();
            world.definition = definition;
            world.regions = prepared.regions.Values.Select(r => new DragonWorldRegion { id = r.spec.id, root = r.root,
                truckStarts = r.spawns.ToArray(), perches = r.perches.ToArray(), nests = r.nests.ToArray(), treasureCaches = r.treasure.ToArray(),
                ambientRoutes = new[] { Anchor(r.root, "Ambient flight A", P(-100,160,0)), Anchor(r.root, "Ambient flight B", P(100,180,80)) } }).ToArray();
            world.extractionSites = definition.extractions.Select(e => new DragonExtractionSite { id = e.id,
                secureZone = prepared.secureZones[e.id], approach = prepared.anchors[e.id + "-approach"], vessel = e.kind == DragonExtractionKind.Boat ? vessel : null }).ToArray();
            var route = new[] { P(0,200,360), P(-550,160,400), P(-1050,180,580), P(-900,430,1000),
                P(-350,500,1250), P(150,480,1150), P(600,300,1000), P(1100,470,850),
                P(1050,260,250), P(600,170,-480), P(-200,170,-500), P(-700,180,-250) };
            world.aerialCheckpoints = route.Select((p,i) => Anchor(prepared.root, "Aerial checkpoint " + (i+1), p)).ToArray();
            world.riderStarts = Enumerable.Range(0,4).Select(i => Anchor(prepared.regions["castle"].root, "Rider start " + (i+1), P(-45+i*30,160,-30))).ToArray();
            world.resettableComponents = new MonoBehaviour[] { vessel };
            EditorUtility.SetDirty(definition);
            return world;
        }

        static DragonExtractionDefinition Extraction(PreparedWorld world, string id, string node, DragonExtractionKind kind)
            => new DragonExtractionDefinition { id = id, nodeId = node, kind = kind, position = world.secureZones[id].bounds.center,
                parkingBounds = world.secureZones[id].bounds, secureSeconds = 2, maximumRelativeSpeed = .7f, minimumBoatDepartureDistance = 12 };

        static void AddNode(Dictionary<string, Vector3> nodes, string id, Vector3 position)
        {
            if (nodes.TryGetValue(id, out var existing) && Vector3.Distance(existing, position) > .001f)
                throw new InvalidOperationException("Junction position disagreement: " + id);
            nodes[id] = position;
        }

        public static List<Vector3> SamplePolyline(Vector3[] points, float spacing)
        {
            var result = new List<Vector3> { points[0] };
            for (int i = 1; i < points.Length; i++)
            {
                int count = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(points[i - 1], points[i]) / spacing));
                for (int k = 1; k <= count; k++) result.Add(Vector3.Lerp(points[i - 1], points[i], (float)k / count));
            }
            return result;
        }

        public static PreparedWorld PrepareVendorContent(Transform parent)
        {
            RequireStoppedEditor();
            var result = new PreparedWorld { root = Child(parent, "Shared Dragon Kingdom") };
            foreach (var spec in Regions)
            {
                var region = new PreparedRegion { spec = spec, root = Child(result.root, spec.label) };
                region.root.position = spec.center;
                for (int i = 0; i < 4; i++) region.spawns.Add(Anchor(region.root, "Truck start " + (i+1), P(i%2==0?-20:20,1,i<2?-22:22)));
                result.regions.Add(spec.id, region);
            }
            Castle(result.regions["castle"]);
            VillageContent(result.regions["village"]);
            ForestContent(result.regions["forest"]);
            Cavern(result.regions["caverns"]);
            Refuge(result.regions["refuge"]);
            NestCliffs(result.regions["nests"]);
            MountainScenery(result.regions["mountains"], false);
            MountainScenery(result.regions["volcano"], true);
            Harbor(result);
            Secure(result, "fortress", result.regions["castle"].root, new Vector3(0, 2, 260), new Vector3(80, 10, 70));
            Secure(result, "mountain", result.regions["refuge"].root, new Vector3(0, 3, 100), new Vector3(90, 12, 70));
            result.anchors["waterfall-canyon"] = Anchor(result.root, "Waterfall canyon aerial passage", P(-1050, 180, 580));
            result.anchors["volcanic-lair"] = Anchor(result.regions["volcano"].root, "Volcanic lair", P(0, 40, 0));
            return result;
        }

        static void Castle(PreparedRegion region)
        {
            var sanctuary = Child(region.root, "Fortified castle sanctuary"); sanctuary.localPosition = P(0,0,200);
            for (int side = 0; side < 4; side++)
            {
                float angle = side * 90;
                var corner = Quaternion.Euler(0, angle+45, 0) * Vector3.forward * 155.5635f;
                var tower = Place(sanctuary, Knights + "SM_Bld_Castle_Tower_01.prefab", corner, angle, 75);
                region.perches.Add(Anchor(tower, "Dragon tower perch", Vector3.up * LocalHeight(tower)));
                for (int part = -1; part <= 1; part++)
                {
                    if (side == 2 && part == 0) continue;
                    var position = Quaternion.Euler(0, angle, 0) * new Vector3(part * 52, 0, 110);
                    Place(sanctuary, Knights + "SM_Bld_Castle_Wall_01.prefab", position, angle, 30);
                }
            }
            var gate=Place(sanctuary, Knights + "SM_Bld_Castle_Wall_Gate_01.prefab", P(0,0,-110), 0, 36);
            var gateBounds=BoundsOf(gate);FitFootprint(gate,new Vector2(gateBounds.size.x*3,gateBounds.size.z));
            Place(sanctuary, Knights + "SM_Bld_Castle_Tower_04.prefab", P(0,0,140), 0, 110);
        }

        static void VillageContent(PreparedRegion region)
        {
            foreach(var house in VillageHouseSites())Place(region.root,house.Item1,house.Item2,house.Item3,house.Item4);
            for (int i = 0; i < 8; i++)
                Place(region.root, Village + "props/P_PROP_cart_01.prefab", P(-100 + i * 28, 0, -130), i * 31, 4);
        }
        public static IEnumerable<Tuple<string,Vector3,float,float>> VillageHouseSites()
        {
            int[] styles = { 1, 2, 3, 5, 7, 10, 12, 14 };
            for (int row = 0; row < 3; row++) for (int side = -1; side <= 1; side += 2)
            {
                int index = row * 2 + (side > 0 ? 1 : 0);
                float x=side*(60+row*10);
                // V46 actual house07 windows/body occupied the forest road.
                // Retain the complete owned house on its own native foundation
                // farther west; no model or collider is removed or disabled.
                Vector3 position=P(x,0,-60+row*65);
                if(row==2&&side<0)position=P(-160,0,20);
                yield return Tuple.Create(Village+"buildings/P_BLD_house_"+styles[index].ToString("00")+".prefab",position,side>0?270f:90f,20f+row*3);
            }
            // The complete owned windmill includes the correctly attached and
            // animated sail. A sail alone is not a supported mill building.
            yield return Tuple.Create(Village+"buildings/P_BLD_house_14.prefab",P(-120,0,100),0f,30f);
        }
        public static IEnumerable<Tuple<string,Vector3,float,float>> HarborHouseSites()
        {
            var positions=new[]{P(-160,0,-140),P(-80,0,-140),P(-160,0,70),P(160,0,-70)};
            for(int i=0;i<positions.Length;i++)yield return Tuple.Create(Village+"buildings/P_BLD_house_"+(i%2==0?"12":"14")+".prefab",positions[i],180f,20f);
        }

        static void ForestContent(PreparedRegion region)
        {
            for (int i = 0; i < 80; i++)
            {
                float angle = i * 137.508f * Mathf.Deg2Rad;
                float radius = 45 + Mathf.Sqrt(i / 79f) * 220;
                var local = P(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                if (Mathf.Abs(local.x) < 35) continue;
                Place(region.root, Forest + "Trees/P_ENV_tree_ancient_03.prefab", local, i * 137.508f, 25 + i % 5 * 5);
            }
            for (int i = 0; i < 12; i++)
                Place(region.root, Forest + "Props/P_PROP_column_ancient_02.prefab", P(-120 + i % 4 * 65, 0, -110 + i / 4 * 90), i * 23, 12);
        }

        static void Cavern(PreparedRegion region)
        {
            for (int i = 0; i < 12; i++)
            {
                // Two opposite truck entrances remain open; collision is reviewed
                // separately from decorative rock surfaces before final bake.
                if (i == 0 || i == 6) continue;
                float angle = i * Mathf.PI / 6;
                var wall=Place(region.root, Cliff + "RockFormation_" + (i % 4 + 1).ToString("00") + ".prefab",
                    P(Mathf.Cos(angle) * 125, -10, Mathf.Sin(angle) * 105), 90 - i * 30, 90);
                FitHorizontalCap(wall,58,58);
            }
            for (int i = 0; i < 4; i++)
            {
                var ceiling = Place(region.root, Cliff + "RockFormation_01.prefab", P(-75 + i * 50, 85, 0), 0, 90);
                ceiling.localRotation = Quaternion.Euler(90, 0, 0);
            }
            region.treasure.Add(Anchor(region.root, "Hoard chamber", P(0,2,0)));
            region.perches.Add(Anchor(region.root, "Sleeping ancient dragon", P(0,2,35)));
            for (int i = 0; i < 8; i++)
                Place(region.root, Art + "Tarbo Studio/TARBO-TowerDefensePack/Prefabs/Crystals/TB_Str_Crystal_A.prefab",
                    P(-95 + i * 27, 0, 65), i * 47, 10 + i % 3 * 4);
        }

        static void Refuge(PreparedRegion region)
        {
            var entrance=Place(region.root, Knights + "SM_Bld_Rockwall_Archway_01.prefab", P(0,0,65), 90, 55);
            FitFootprint(entrance,new Vector2(96,32));
            for (int i = 0; i < 6; i++)
            {
                Place(region.root, Cliff + "RockFormation_02.prefab", P(-110, -10, 100 + i * 30), 90, 90);
                var right=Place(region.root, Cliff + "RockFormation_03.prefab", P(110, -10, 100 + i * 30), 270, 90);FitHorizontalCap(right,70,70);
            }
            Place(region.root, Knights + "SM_Bld_Castle_Iron_Gate_01.prefab", P(0,35,65), 0, 30);
        }

        static void MountainScenery(PreparedRegion region, bool volcanic)
        {
            for (int i = 0; i < 9; i++)
            {
                float angle = i * Mathf.PI * 2 / 9;
                var t = Place(region.root, Cliff + "RockFormation_" + (i%4+1).ToString("00") + ".prefab",
                    P(Mathf.Cos(angle)*240,-50,Mathf.Sin(angle)*240), i*40, volcanic ? 260 : 300);
                if (volcanic) t.name = "Owned basalt cliff composition " + i;
                FitHorizontalCap(t,180,180);
                GroundAndClearPeak(t,(volcanic?440:480)+(i%3)*45);
            }
        }

        static void NestCliffs(PreparedRegion region)
        {
            for (int i = 0; i < 4; i++)
            {
                var pockets=NestPockets();
                var group=Child(region.root,"Owned nesting ledge "+(i+1));group.localPosition=pockets[i];
                var rock=Place(group,Cliff+"RockFormation_04.prefab",Vector3.down*90,i*50,90);
                FitHorizontalCap(rock,28,28);
                var bounds=BoundsOf(rock);float terrain=TerrainHeightAt(group.position);float required=Mathf.Max(90,group.position.y-terrain);
                rock.localScale=Vector3.Scale(rock.localScale,P(1,required/bounds.size.y,1));bounds=BoundsOf(rock);rock.position+=Vector3.up*(group.position.y-bounds.max.y);
                var nest = Place(group, Forest + "Misc/P_ENV_birdnest_ancient_02.prefab", Vector3.zero, i * 60, 4);
                var seat=Anchor(nest,"Egg delivery nest",Vector3.zero);
                seat.position=DragonKingdomNestAnchorMigration.MeasuredNestSeat(nest);
                region.nests.Add(seat);
                region.perches.Add(Anchor(group,"Mother dragon perch "+i,P(0,5,0)));
            }
        }

        static void Harbor(PreparedWorld world)
        {
            var region = world.regions["harbor"];
            for (int i = 0; i < 5; i++)
                Place(region.root, Dock + "DockNoRailingConnector.prefab", P(20 + i * 24, -4, -125), 90, 4);
            foreach(var house in HarborHouseSites())Place(region.root,house.Item1,house.Item2,house.Item3,house.Item4);
            // A deliberately authored timber transport barge: actual dock modules
            // form a 24m x 52m deck with four truck spaces. Decorative small boat
            // is never enlarged into the vessel. Root owns moving-platform logic.
            world.vessel = Child(region.root, "Timber evacuation barge");
            world.vessel.localPosition = P(120, 0, -205);
            for (int x = 0; x < 4; x++) for (int z = 0; z < 8; z++)
            {
                var section = Place(world.vessel, Dock + "DockNoRailingConnector.prefab", P(-9 + x * 6, -3, -22.75f + z * 6.5f), 0, 3);
                FitFootprint(section, new Vector2(6, 6.5f));
            }
            for (int side = -1; side <= 1; side += 2) for (int z = 0; z < 8; z++)
            {
                var rail = Place(world.vessel, Dock + "DockSingleRailing.prefab", P(side * 12, -1, -22.75f + z * 6.5f), side > 0 ? 180 : 0, 3);
                FitFootprint(rail, new Vector2(2, 6.5f));
            }
            var body = world.vessel.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true; body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            foreach (var collider in world.vessel.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
            var deck = world.vessel.gameObject.AddComponent<BoxCollider>();
            deck.center = P(0, -.5f, 0); deck.size = P(24, 1, 52);
            foreach (int side in new[] { -1, 1 })
            {
                var rail = Child(world.vessel, "Solid timber safety rail " + side);
                var col = rail.gameObject.AddComponent<BoxCollider>(); col.center = P(side*11.8f,1.3f,0); col.size = P(.35f,2.6f,52);
            }
            world.ramp = Child(region.root, "Barge loading ramp");
            world.ramp.localPosition = P(120, 0, -160);
            for (int x = 0; x < 4; x++) for (int z = 0; z < 6; z++)
            {
                var rampArt = Place(world.ramp, Dock + "DockNoRailingConnector.prefab", P(-8.25f + x*5.5f,-3,-15.833333f+z*6.333333f), 0, 3);
                FitFootprint(rampArt, new Vector2(5.5f, 6.333333f));
            }
            foreach (var c in world.ramp.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
            var rampContact = world.ramp.gameObject.AddComponent<BoxCollider>(); rampContact.center = P(0,-.5f,0); rampContact.size = P(22,1,38);
            Secure(world, "boat", world.vessel, P(0,2,0), P(22,8,46));
            var boatApproach=world.anchors["boat-approach"];boatApproach.SetParent(region.root,false);boatApproach.localPosition=P(120,1,-124);boatApproach.rotation=Quaternion.LookRotation(Vector3.back);
            var lane=Anchor(region.root,"Barge boarding lane entry",P(120,1,-148));lane.rotation=Quaternion.LookRotation(Vector3.back);world.anchors["boat-boarding-entry"]=lane;
            var deckEntry=Anchor(region.root,"Barge deck entry",P(120,1,-188));deckEntry.rotation=Quaternion.LookRotation(Vector3.back);world.anchors["boat-deck-entry"]=deckEntry;
            for (int i = 0; i < 4; i++){var parking=Anchor(world.vessel,"Truck parking "+(i+1),P(i%2==0?-6:6,1,i<2?-12:12));parking.rotation=Quaternion.LookRotation(Vector3.back);world.anchors["boat-parking-"+i]=parking;}
            world.anchors["boat-departure"] = Anchor(region.root, "Barge departure destination", P(120,0,-600));
        }

        static void Secure(PreparedWorld world, string id, Transform parent, Vector3 local, Vector3 size)
        {
            var root = Anchor(parent, id + " secure zone", local);
            var box = root.gameObject.AddComponent<BoxCollider>(); box.isTrigger = true; box.size = size;
            world.secureZones.Add(id, box);
            world.anchors[id + "-approach"] = Anchor(parent, id + " approach", local - Vector3.forward * (size.z * .5f + 30));
        }

#if VISTA
        public static VistaManager PrepareVistaTerrain(Transform root)
        {
            RequireStoppedEditor();
            var manager = VistaManager.CreateInstanceInScene();
            // AddComponent-driven authoring does not guarantee the inspector's
            // Reset/serialized-event construction path. Initialize the vendor's
            // public defaults before assigning IDs to its tiles.
            manager.Reset();
            InitializeVistaCallback(manager, "m_beforeGeneratingUnityCallback");
            InitializeVistaCallback(manager, "m_afterGeneratingUnityCallback");
            if (string.IsNullOrEmpty(manager.id) || manager.beforeGeneratingUnityCallback == null || manager.afterGeneratingUnityCallback == null)
                throw new InvalidOperationException("Vista manager ID and both generation callbacks must be initialized before generation");
            manager.name = "Vista kingdom terrain authoring"; manager.transform.SetParent(root, false);
            manager.terrainMaxHeight = 600; manager.heightMapResolution = 1025;
            manager.missingTextureAction = MissingOutputAction.Keep;
            Directory.CreateDirectory("Assets/" + Folder + "/Terrain");
            var terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/PinwheelStudio/Vista/Personal/Samples/Materials/VistaSample_Terrain.mat");
            for (int z = 0; z < 2; z++) for (int x = 0; x < 2; x++)
            {
                string path = "Assets/" + Folder + "/Terrain/Kingdom " + x + " " + z + ".asset";
                if (AssetDatabase.LoadAssetAtPath<TerrainData>(path) != null)
                    throw new InvalidOperationException("Preserve existing authored terrain; explicit review required: " + path);
                var data = new TerrainData { heightmapResolution = 1025, size = P(1600,600,1600) };
                data.terrainLayers = new[] { AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/Grass Dark/Grass_Dark_VistaSample.terrainlayer") };
                AssetDatabase.CreateAsset(data, path);
                var tile = Terrain.CreateTerrainGameObject(data); tile.name = "Vista terrain tile " + x + " " + z;
                tile.transform.SetParent(root, false); tile.transform.position = P(-1600 + x * 1600, -20, -800 + z * 1600);
                tile.GetComponent<Terrain>().materialTemplate = terrainMaterial;
                tile.AddComponent<TerrainTile>().managerId = manager.id;
            }
            var biome = LocalProceduralBiome.CreateInstanceInScene(manager);
            biome.Reset();
            biome.name = "Vista mountain terrain foundation";
            biome.terrainGraph = AssetDatabase.LoadAssetAtPath<TerrainGraph>("Assets/PinwheelStudio/Vista/Personal/Samples/Graphs/MountainRange.asset");
            if (biome.terrainGraph == null) throw new InvalidOperationException("Installed Vista MountainRange graph required");
            biome.baseResolution = 1024; biome.seed = 28082026;
            biome.anchors = new[] { P(-1600,0,650), P(1600,0,650), P(1600,0,2400), P(-1600,0,2400) };
            // Integration owner explicitly yields manager.GenerateAll(), then
            // reviews terrain grade/road cuts before placing or baking content.
            return manager;
        }

        static void InitializeVistaCallback(VistaManager manager, string serializedField)
        {
            // Vista exposes read-only callback getters; the corresponding
            // serialized inspector fields have no public setters. Match the
            // existing project-owned Core-Collapse event-configuration pattern,
            // without editing vendor source or substituting its generation.
            var field = typeof(VistaManager).GetField(serializedField, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field == null || field.FieldType != typeof(UnityEngine.Events.UnityEvent))
                throw new InvalidOperationException("Installed Vista callback schema changed: " + serializedField);
            if (field.GetValue(manager) == null) field.SetValue(manager, new UnityEngine.Events.UnityEvent());
            EditorUtility.SetDirty(manager);
        }
#endif

        static Transform Child(Transform parent, string name)
        { var result = new GameObject(name).transform; result.SetParent(parent, false); return result; }
        static Transform Anchor(Transform parent, string name, Vector3 local)
        { var result = Child(parent, name); result.localPosition = local; return result; }

        public static Transform PlaceOwnedArtForReview(Transform parent,string path,Vector3 worldPoint,float yaw,float height,bool solid)
        {
            RequireStoppedEditor();
            if(parent==null||!path.StartsWith(Private,StringComparison.Ordinal)||height<=0)throw new ArgumentException("Actual private owned art and an explicit scene parent/positive size required.");
            return Place(parent,path,parent.InverseTransformPoint(worldPoint),yaw,height,solid,false);
        }
        static Transform Place(Transform parent, string path, Vector3 local, float yaw, float height,bool addStaticCollision=true,bool remapMaterials=true)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null && path.StartsWith(Knights, StringComparison.Ordinal))
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path.Replace("/Prefabs/Buildings/", "/Models/").Replace(".prefab", ".fbx"));
            if (prefab == null) throw new InvalidOperationException("Dependency-closed owned art required: " + path);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var t = instance.transform; t.SetParent(parent, false); t.localPosition = Vector3.zero; t.localRotation = Quaternion.Euler(0,yaw,0);
            var bounds = BoundsOf(t);
            if (bounds.size.y < .01f) throw new InvalidOperationException("Invalid authored model bounds: " + path);
            t.localScale *= height / bounds.size.y;
            bounds = BoundsOf(t);
            var secondaryLods=ConfigureAccurateLodCollision(t);
            t.position += parent.TransformPoint(local) - new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            bounds = BoundsOf(t);
            if (path.StartsWith(Knights, StringComparison.Ordinal))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(Art + "Synty Studios/PolygonKnights/Materials/PolyKnights_Mat_01.mat");
                foreach (var renderer in t.GetComponentsInChildren<MeshRenderer>())
                    renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
            }
            if (addStaticCollision&&path.StartsWith(Forest + "Trees/", StringComparison.Ordinal))
            {
                if (t.GetComponentInChildren<Collider>() == null)
                {
                    var trunk = t.gameObject.AddComponent<CapsuleCollider>();
                    trunk.center = t.InverseTransformPoint(bounds.center);
                    trunk.height = bounds.size.y / Mathf.Max(.001f,t.lossyScale.y);
                    trunk.radius = bounds.size.y * .035f / Mathf.Max(.001f,t.lossyScale.x);
                }
            }
            else if (addStaticCollision&&!path.EndsWith("P_BLD_windmill_sail.prefab", StringComparison.Ordinal) && !path.EndsWith("P_BLD_waterwheel.prefab", StringComparison.Ordinal))
                foreach (var mesh in t.GetComponentsInChildren<MeshFilter>())
                    if (!secondaryLods.Contains(mesh.gameObject)&&mesh.GetComponent<Collider>() == null && mesh.sharedMesh != null)
                        mesh.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh.sharedMesh;
            if(path.StartsWith(Forest+"Trees/",StringComparison.Ordinal))RelocateDecorativeTree(t);
            if(remapMaterials)RemapLegacyMaterials(t);
            if (path.EndsWith("P_BLD_windmill_sail.prefab", StringComparison.Ordinal) || path.EndsWith("P_BLD_waterwheel.prefab", StringComparison.Ordinal))
            {
                string controllerName = path.EndsWith("P_BLD_waterwheel.prefab", StringComparison.Ordinal) ? "CTRL_BLD_waterwheel" : "CTRL_BLD_windmill_sail";
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Art + "Tidal Flask Studios/Fantastic Village Pack/animation/" + controllerName + ".controller");
                if (controller == null) throw new InvalidOperationException("Owned environmental animation controller required: " + controllerName);
                var animator = t.GetComponent<Animator>();
                if (animator == null) animator = t.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
            }
            return t;
        }

        public static HashSet<GameObject> ConfigureAccurateLodCollision(Transform root)
        {
            var secondary=new HashSet<GameObject>();
            foreach(var group in root.GetComponentsInChildren<LODGroup>(true))
            {
                var lods=group.GetLODs();if(lods.Length==0)continue;
                var primary=new HashSet<GameObject>(lods[0].renderers.Where(r=>r!=null).Select(r=>r.gameObject));
                for(int i=1;i<lods.Length;i++)foreach(var renderer in lods[i].renderers.Where(r=>r!=null&&!primary.Contains(r.gameObject)))
                {
                    secondary.Add(renderer.gameObject);
                    // Remove only redundant scene-instance representations of
                    // this same LOD set. The primary visible LOD0 mesh remains
                    // the intentional accurate collision surface, independent
                    // of whichever rendering LOD is currently visible.
                    foreach(var duplicate in renderer.GetComponents<MeshCollider>())UnityEngine.Object.DestroyImmediate(duplicate);
                }
                foreach(var model in primary)
                {
                    var filter=model.GetComponent<MeshFilter>();var collider=model.GetComponent<MeshCollider>();
                    if(filter!=null&&filter.sharedMesh!=null&&collider!=null)collider.sharedMesh=filter.sharedMesh;
                }
            }
            return secondary;
        }

        static Bounds[] roadClearanceEnvelopes;
        static bool IntersectsRoadCorridor(Bounds bounds)
        {
            if(roadClearanceEnvelopes==null)
            {
                var envelopes=new List<Bounds>();
                foreach(var road in Roads())foreach(var p in SamplePolyline(road.points,12))envelopes.Add(new Bounds(p+Vector3.up*4,P(road.width+12,8,road.width+12)));
                foreach(var node in JunctionPositions().Where(n=>n.Key!="harbor-ramp"&&n.Key!="boat-deck"))envelopes.Add(new Bounds(node.Value+Vector3.up*4,P(76,8,76)));
                foreach(var turn in HairpinTurns(Roads()))envelopes.Add(new Bounds(turn.Item3+Vector3.up*4,P(50,8,50)));
                foreach(var bay in NestLoadingBays())envelopes.Add(new Bounds(bay+Vector3.up*4,P(32,8,32)));
                roadClearanceEnvelopes=envelopes.ToArray();
            }
            return roadClearanceEnvelopes.Any(clearance=>bounds.Intersects(clearance));
        }
        static void FitHorizontalCap(Transform root,float width,float depth)
        {
            var bounds=BoundsOf(root);float factor=Mathf.Min(width/bounds.size.x,depth/bounds.size.z);
            root.localScale=Vector3.Scale(root.localScale,P(factor,1,factor));
            var fitted=BoundsOf(root);root.position+=P(bounds.center.x-fitted.center.x,0,bounds.center.z-fitted.center.z);
        }
        static void GroundAndClearPeak(Transform root,float topHeight)
        {
            var original=BoundsOf(root).center;Vector3 fittedScale=root.localScale;
            for(int attempt=0;attempt<=120;attempt++)
            {
                Vector3 candidate=original;
                if(attempt>0){float radius=60*Mathf.Ceil(attempt/16f),angle=attempt*137.508f*Mathf.Deg2Rad;candidate+=P(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);}
                if(candidate.x< -1480||candidate.x>1480||candidate.z< -680||candidate.z>2280)continue;
                float ground=TerrainHeightAt(candidate);if(topHeight-ground<100)continue;
                root.localScale=fittedScale;var bounds=BoundsOf(root);root.localScale=Vector3.Scale(root.localScale,P(1,(topHeight-ground)/bounds.size.y,1));bounds=BoundsOf(root);
                root.position+=P(candidate.x-bounds.center.x,ground-bounds.min.y,candidate.z-bounds.center.z);bounds=BoundsOf(root);
                if(!IntersectsRoadCorridor(bounds)){Physics.SyncTransforms();return;}
            }
            throw new InvalidOperationException("Retain complete visible/collidable peak; no bounded road-clear grounded composition for "+root.name+" near "+original);
        }
        static void RelocateDecorativeTree(Transform root)
        {
            Vector3 original=root.position;
            var colliders=root.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&!c.isTrigger).ToArray();
            if(colliders.Length==0)throw new InvalidOperationException("Owned tree requires retained physical trunk: "+root.name);
            for(int attempt=0;attempt<=48;attempt++)
            {
                Vector3 candidate=original;
                if(attempt>0){float radius=24*Mathf.Ceil(attempt/12f),angle=attempt*137.508f*Mathf.Deg2Rad;candidate+=P(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);}
                root.position=candidate;var art=BoundsOf(root);root.position+=Vector3.up*(TerrainHeightAt(art.center)-art.min.y);Physics.SyncTransforms();
                var collision=colliders[0].bounds;foreach(var collider in colliders.Skip(1))collision.Encapsulate(collider.bounds);
                if(!IntersectsRoadCorridor(collision))return;
            }
            root.position=original;throw new InvalidOperationException("Keep owned tree/colliders; no bounded roadside relocation is clear: "+root.name+" at "+original);
        }

        static void RemapLegacyMaterials(Transform root)
        {
            const string folder = "Assets/TruckDragonKingdom/Materials/Environment";
            if (!Directory.Exists(folder)) { Directory.CreateDirectory(folder); AssetDatabase.Refresh(); }
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    if (source == null) throw new InvalidOperationException("Owned renderer has missing material: " + renderer.name);
                    if (DragonKingdomWorldVisualMigration.HasSupportedUrpShader(source)) continue;
                    string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
                    if (string.IsNullOrEmpty(guid)) throw new InvalidOperationException("Material has no persistent owned source: " + source.name);
                    string path = folder + "/" + guid + " URP.mat";
                    var variant = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (variant == null)
                    {
                        var shader = Shader.Find("Universal Render Pipeline/Lit");
                        if (shader == null) throw new InvalidOperationException("Installed URP Lit shader required");
                        variant = new Material(shader) { name = source.name + " · kingdom URP" };
                        Texture albedo = source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : null;
                        if (albedo == null && source.HasProperty("_Albedo")) albedo = source.GetTexture("_Albedo");
                        if (albedo != null) variant.SetTexture("_BaseMap", albedo);
                        if (source.HasProperty("_Color")) variant.SetColor("_BaseColor", source.GetColor("_Color"));
                        if (source.HasProperty("_BumpMap") && source.GetTexture("_BumpMap") != null)
                        { variant.SetTexture("_BumpMap", source.GetTexture("_BumpMap")); variant.EnableKeyword("_NORMALMAP"); }
                        variant.SetFloat("_Metallic", 0); variant.SetFloat("_Smoothness", .2f);
                        AssetDatabase.CreateAsset(variant, path);
                    }
                    materials[i] = variant;
                }
                renderer.sharedMaterials = materials;
            }
            DragonKingdomWorldVisualMigration.RepairRendererMaterials(root.gameObject,new DragonKingdomWorldVisualMigration.Receipt());
        }

        public static void FitOwnedArtFootprintForReview(Transform t,Vector2 size)=>FitFootprint(t,size);
        static void FitFootprint(Transform t, Vector2 size)
        {
            var b = BoundsOf(t);
            if (b.size.x < .01f || b.size.z < .01f) throw new InvalidOperationException("Invalid timber module footprint");
            var scale = t.localScale; scale.x *= size.x / b.size.x; scale.z *= size.y / b.size.z; t.localScale = scale;
            var fitted=BoundsOf(t);t.position+=P(b.center.x-fitted.center.x,0,b.center.z-fitted.center.z);
        }
        static float LocalHeight(Transform t) => BoundsOf(t).size.y / Mathf.Max(.001f,t.lossyScale.y);
        public static Bounds MeasureOwnedModelBounds(Transform root)
        {
            if(!TryMeasureOwnedModelBounds(root,out var bounds,false))throw new InvalidOperationException("Actual model mesh geometry required for static sizing/footing: "+(root==null?"NULL":root.name));return bounds;
        }
        public static bool TryMeasureOwnedModelBounds(Transform root,out Bounds result,bool colliderFallback=false)
        {
            var bounds=new Bounds();bool first=true;
            void Add(Vector3 p)
            {
                if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.x)||float.IsInfinity(p.y)||float.IsInfinity(p.z))throw new InvalidOperationException("Nonfinite actual model geometry: "+root.name);
                if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);
            }
            if(root!=null)
            {
                // Empty particle/line renderer bounds can stay at world0 until
                // evaluated. A house at(-775,-596) thereby became785m wide.
                // Static placement measures actual model geometry instead;
                // every original effect and its renderer remains untouched.
                foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    if(filter.sharedMesh==null||filter.GetComponent<MeshRenderer>()==null)continue;
                    var b=filter.sharedMesh.bounds;var matrix=filter.transform.localToWorldMatrix;
                    foreach(float x in new[]{b.min.x,b.max.x})foreach(float y in new[]{b.min.y,b.max.y})foreach(float z in new[]{b.min.z,b.max.z})Add(matrix.MultiplyPoint3x4(new Vector3(x,y,z)));
                }
                Mesh snapshot=null;
                try
                {
                    foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        if(skin.sharedMesh==null)continue;if(snapshot==null)snapshot=new Mesh();snapshot.Clear();skin.BakeMesh(snapshot,true);var matrix=skin.transform.localToWorldMatrix;
                        foreach(var p in snapshot.vertices)Add(matrix.MultiplyPoint3x4(p));
                    }
                }
                finally{if(snapshot!=null)UnityEngine.Object.DestroyImmediate(snapshot);}
                if(first&&colliderFallback)foreach(var collider in root.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&!c.isTrigger))
                {var b=collider.bounds;if(b.size.sqrMagnitude<.000001f)continue;Add(b.min);Add(b.max);}
            }
            result=bounds;return !first;
        }
        static Bounds BoundsOf(Transform t)=>MeasureOwnedModelBounds(t);
        static void RequireStoppedEditor()
        { if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Explicit stopped Editor authoring only"); }
    }
}


