#if UNITY_EDITOR && VISTA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using Unity.Mathematics;
using sc.splines.spawner.runtime;
using VendorDressingSpawnPoint=sc.splines.spawner.runtime.SpawnPoint;
namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        const string DressingPackage="Packages/xyz.staggart-creations.spline-spawner";
        const string DressingPolyart=DragonKingdomWorldAuthoring.Art+"Polyart Studio/HandpaintedEnvironmentVol1/Meshes/Prefab/";
        const string DressingVillage=DragonKingdomWorldAuthoring.Village;
        [Serializable]public sealed class VergeDressingSite
        {public string id,kind,region,node;public Vector3 center;public Vector2 radius;public int seed,minimum,maximum;}
        [Serializable]public sealed class VergeDressingPart
        {public string site,source,guid,name;public Vector3 position,scale;public Quaternion rotation;public Bounds bounds;public bool nativeGrounded,allOriginalExclusionsClear,physicsDisabled;}
        [Serializable]public sealed class VergeDressingGroup
        {public string id,kind;public int vendorPoints,accepted,rejected,spawnerCalls;public bool startupNone,noRetainedGenerator;public string[] rejectionReasons;}
        [Serializable]public sealed class VergeDressingReview
        {
            public string scope="BOUNDED_SIX_ISLAND_TWO_TERRACE_FOUR_JUNCTION_VENDOR_DRESSING_TRIAL_NOT_TERRAIN_REPAIR_OR_WORLD_ADOPTION",status="IN_PROGRESS",error,sourceCandidate,folder,preview,prefab,originalWorldBefore,originalWorldAfter,originalWorldReloaded;
            public bool originalWorldExact,originalTerrainBytesExact,originalSourcesExact,savedReloaded,noAddedPhysics,noRuntimeGenerator,incomingSceneContextRestored;public int islands,bands,junctions,instances;
            public VergeDressingSite[] sites;public VergeDressingGroup[] groups;public VergeDressingPart[] parts;public TowerAppearanceSource[] watchedSources;public TowerAppearanceFrame[] frames;public string[] changedSources;
        }
        public static VergeDressingReview LastVergeDressingReview{get;private set;}
        public static VergeDressingSite[] BoundedVergeDressingSites()=>new[]{
            new VergeDressingSite{id="village-west-grove",kind="ISLAND",region="village",center=new Vector3(-620,0,-380),radius=new Vector2(42,48),seed=8101,minimum=8,maximum=32},
            new VergeDressingSite{id="village-south-orchard",kind="ISLAND",region="village",center=new Vector3(-470,0,-455),radius=new Vector2(42,38),seed=8102,minimum=8,maximum=32},
            new VergeDressingSite{id="village-north-verge",kind="ISLAND",region="village",center=new Vector3(-690,0,-240),radius=new Vector2(38,46),seed=8103,minimum=8,maximum=32},
            new VergeDressingSite{id="harbor-market-grove",kind="ISLAND",region="harbor",center=new Vector3(570,0,-430),radius=new Vector2(42,40),seed=8104,minimum=8,maximum=32},
            new VergeDressingSite{id="harbor-east-grove",kind="ISLAND",region="harbor",center=new Vector3(725,0,-455),radius=new Vector2(42,44),seed=8105,minimum=8,maximum=32},
            new VergeDressingSite{id="harbor-south-verge",kind="ISLAND",region="harbor",center=new Vector3(715,0,-595),radius=new Vector2(42,38),seed=8106,minimum=8,maximum=32},
            new VergeDressingSite{id="mountain-south-foot",kind="TERRACE",region="mountains",center=new Vector3(-150,0,800),radius=new Vector2(50,18),seed=8111,minimum=3,maximum=12},
            new VergeDressingSite{id="mountain-upper-foot",kind="TERRACE",region="mountains",center=new Vector3(180,0,1170),radius=new Vector2(48,18),seed=8112,minimum=3,maximum=12},
            new VergeDressingSite{id="village-west-approach",kind="JUNCTION",region="village",node="village",center=new Vector3(-60,0,-45),radius=new Vector2(12,12),seed=8121,minimum=3,maximum=3},
            new VergeDressingSite{id="village-east-approach",kind="JUNCTION",region="village",node="village",center=new Vector3(60,0,45),radius=new Vector2(12,12),seed=8122,minimum=3,maximum=3},
            new VergeDressingSite{id="harbor-west-approach",kind="JUNCTION",region="harbor",node="harbor",center=new Vector3(-60,0,45),radius=new Vector2(12,12),seed=8123,minimum=3,maximum=3},
            new VergeDressingSite{id="harbor-east-approach",kind="JUNCTION",region="harbor",node="harbor",center=new Vector3(60,0,-45),radius=new Vector2(12,12),seed=8124,minimum=3,maximum=3}
        };
        public static string[] BoundedVergeDressingSources()=>new[]{
            DressingPolyart+"Tree Medium/TreeMedium_01.prefab",DressingPolyart+"Tree Medium/TreeMedium_02.prefab",DressingPolyart+"Tree Medium/TreeMedium_03.prefab",
            DressingPolyart+"Foliage/Bushes/Bush_01.prefab",DressingPolyart+"Foliage/Bushes/Bush_02.prefab",DressingPolyart+"Foliage/Bushes/Bush_04.prefab",DressingPolyart+"Foliage/Bushes/Bush_05.prefab",
            DragonKingdomWorldAuthoring.Forest+"Plants/P_ENV_grass_ancient_01.prefab",DragonKingdomWorldAuthoring.Forest+"Flowers/P_ENV_flower_ancient_03.prefab",
            DressingPolyart+"Cliffs/RockFormation_02.prefab",DressingPolyart+"Cliffs/RockFormation_03.prefab",
            DressingVillage+"props/P_PROP_streetlamp_v01_01.prefab",DressingVillage+"props/P_PROP_cart_01.prefab",DressingVillage+"props/P_PROP_market_v01_01.prefab"};
        public static void ValidateBoundedVergeDressingPlan(VergeDressingSite[] sites)
        {
            if(sites==null||sites.Length!=12||sites.Select(s=>s.id).Distinct().Count()!=12||sites.Count(s=>s.kind=="ISLAND")!=6||sites.Count(s=>s.kind=="TERRACE")!=2||sites.Count(s=>s.kind=="JUNCTION")!=4||sites.Any(s=>s.minimum<1||s.maximum<s.minimum||s.maximum>32||!RidgeFinite(s.center)||!float.IsFinite(s.radius.x)||!float.IsFinite(s.radius.y)||s.radius.x<10||s.radius.x>55||s.radius.y<10||s.radius.y>55)||sites.Sum(s=>s.maximum)>240)throw new InvalidOperationException("Only the explicit bounded six islands, two terrace bands and four junction ensembles are authorized; no global scatter.");
        }
        static string DressingPath(Transform value,Transform root)=>value==root?"@":DressingPath(value.parent,root)+"/"+value.GetSiblingIndex()+":"+value.name;
        static string DressingStableWorld(DragonKingdomWorld world)
        {
            string Token(UnityEngine.Object obj){if(obj==null)return "null";if(AssetDatabase.Contains(obj)&&AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj,out string guid,out long id))return guid+":"+id;if(obj is GameObject go)return "GO:"+DressingPath(go.transform,world.transform);if(obj is Component c)return c.GetType().FullName+":"+DressingPath(c.transform,world.transform)+":"+Array.IndexOf(c.GetComponents(c.GetType()),c);throw new InvalidOperationException("Unknown world reference prevents an exact appearance comparison.");}
            var lines=new List<string>();foreach(var component in world.GetComponentsInChildren<Component>(true).Where(c=>c is Transform||c is Collider||c is Rigidbody||c is Renderer||c is MeshFilter||c is LODGroup||c is Terrain||c is DragonKingdomWorld||c is Digger.Modules.Core.Sources.DiggerSystem))
            {
                string json=Regex.Replace(EditorJsonUtility.ToJson(component),"\\\"m_(CorrespondingSourceObject|PrefabInstance|PrefabAsset)\\\"\\s*:\\s*\\{[^{}]*\\}","\"prefabProvenance\":null");json=Regex.Replace(json,"\\\"instanceID\\\"\\s*:\\s*(-?[0-9]+)",match=>"\"stableObject\":\""+Token(EditorUtility.InstanceIDToObject(int.Parse(match.Groups[1].Value))).Replace("\\","\\\\").Replace("\"","\\\"")+"\"");lines.Add(component.GetType().FullName+"|"+DressingPath(component.transform,world.transform)+"|"+json);
            }
            using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n",lines)))).Replace("-","").ToLowerInvariant();
        }
        static float DressingTerrainY(DragonKingdomWorld world,Vector3 p)
        {
            var terrains=world.GetComponentsInChildren<Terrain>(true).Where(t=>p.x>=t.transform.position.x&&p.z>=t.transform.position.z&&p.x<=t.transform.position.x+t.terrainData.size.x&&p.z<=t.transform.position.z+t.terrainData.size.z).ToArray();if(terrains.Length==0)throw new InvalidOperationException("Dressing site lacks actual original terrain: "+p);return terrains.Max(t=>t.SampleHeight(p)+t.transform.position.y);
        }
        static bool DressingExclusionsClear(DragonKingdomWorld world,Bounds bounds,Collider[] originals,out string reason)
        {
            reason=null;if(!FinishBoundsClearOfRoads(world,bounds)){reason="ORIGINAL_ROAD_START_OBJECTIVE";return false;}if(!RidgeWingClear(world,bounds)){reason="ORIGINAL_AERIAL_ENVELOPE";return false;}
            if(bounds.Intersects(DragonKingdomWorldAuthoring.CavernUndergroundBounds())){reason="ORIGINAL_CAVERN_ENVELOPE";return false;}
            foreach(var collider in Physics.OverlapBox(bounds.center,bounds.extents,Quaternion.identity,~0,QueryTriggerInteraction.Ignore))if(collider!=null&&collider.enabled&&!(collider is TerrainCollider)&&originals.Contains(collider)){reason="ORIGINAL_PHYSICAL_GEOMETRY:"+collider.name;return false;}return true;
        }
        static GameObject DressingSource(string path)=>AssetDatabase.LoadAssetAtPath<GameObject>(path)??throw new InvalidOperationException("Actual original owned-art prefab required: "+path);
        static List<VendorDressingSpawnPoint> DressingVendorPoints(Scene scene,Vector3[] knots,bool closed,GameObject[] prefabs,float[] heights,float spacing,int seed,bool onKnots,VergeDressingGroup group)
        {
            var temporary=new GameObject("Temporary actual Spline Spawner "+group.id);SceneManager.MoveGameObjectToScene(temporary,scene);var points=new List<VendorDressingSpawnPoint>();
            try
            {
                var container=temporary.AddComponent<SplineContainer>();var spline=new Spline();foreach(var knot in knots)spline.Add(new BezierKnot((float3)knot),TangentMode.Linear);spline.Closed=closed;container.Spline=spline;
                var spawner=temporary.AddComponent<SplineSpawner>();spawner.respawnTriggers=0;spawner.startupBehaviour=SplineSpawner.StartupBehaviour.None;spawner.splineChangeTrigger=SplineSpawner.SplineChangeTrigger.None;spawner.spawnObjects=false;spawner.hideInstances=false;spawner.root=temporary.transform;spawner.SetSplineContainer(container);spawner.distributionSettings.SetSeed(seed);spawner.distributionSettings.mode=onKnots?DistributionSettings.DistributionMode.OnKnots:closed?DistributionSettings.DistributionMode.InsideArea:DistributionSettings.DistributionMode.OnCurve;
                spawner.distributionSettings.insideArea.spacing=spacing;spawner.distributionSettings.insideArea.padding=3;spawner.distributionSettings.insideArea.allowOverflow=false;spawner.distributionSettings.onCurve.spacing=spacing;spawner.distributionSettings.onCurve.startEndTrimming=new Vector2(3,3);spawner.distributionSettings.onKnots.selection=DistributionSettings.OnKnots.Selection.FirstOnly;
                for(int i=0;i<prefabs.Length;i++){RidgeMeshSamples(prefabs[i].transform,out var bounds,out _,out _);if(bounds.size.y<=.01f)throw new InvalidOperationException("Actual source primary height required.");var entry=spawner.AddObject(prefabs[i]);entry.baseScale=Vector3.one*(heights[i]/bounds.size.y);}
                spawner.AddModifier(sc.splines.spawner.runtime.Rotate.CreateRandomY());spawner.AddModifier(new sc.splines.spawner.runtime.Scale{randomUniformMin=.85f,randomUniformMax=1.15f});spawner.AddModifier(new SnapToColliders{direction=SnapToColliders.Direction.StraightDown,rayHeightOffset=35,positioning=1,slopeLimit=new Vector2(0,closed?32:60),heightOffset=0,alignRotationX=0,alignRotationZ=0});
                spawner.onAfterModifiers+=list=>{foreach(var point in list)if(point.isValid)points.Add(point);};Physics.SyncTransforms();spawner.Respawn();group.spawnerCalls++;group.startupNone=spawner.startupBehaviour==SplineSpawner.StartupBehaviour.None;group.vendorPoints+=points.Count;return points;
            }
            finally{UnityEngine.Object.DestroyImmediate(temporary);group.noRetainedGenerator=temporary==null;}
        }
        static bool DressingBakePoint(DragonKingdomWorld world,Transform parent,VendorDressingSpawnPoint point,string source,string site,Collider[] originals,List<VergeDressingPart> parts,List<string> rejects)
        {
            var prefab=DressingSource(source);var root=((GameObject)PrefabUtility.InstantiatePrefab(prefab,parent)).transform;root.SetPositionAndRotation((Vector3)point.position,(Quaternion)point.rotation);root.localScale=(Vector3)point.scale;root.name=site+" "+prefab.name+" "+parts.Count;
            try
            {
                // Only the new visual instances lose physics. Original terrain,
                // traffic routes, buildings, supports and colliders are untouched.
                foreach(var c in root.GetComponentsInChildren<Collider>(true))c.enabled=false;foreach(var body in root.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.detectCollisions=false;}
                RidgeMeshSamples(root,out var bounds,out _,out _);float ground=DressingTerrainY(world,bounds.center);root.position+=Vector3.up*(ground-bounds.min.y-.035f);RidgeMeshSamples(root,out bounds,out _,out _);
                if(!DressingExclusionsClear(world,bounds,originals,out var reason)){rejects.Add(reason);return false;}
                if(source.StartsWith(DressingVillage+"props/",StringComparison.Ordinal)&&parts.Where(p=>p.site==site).Any(p=>RidgeInterior(p.bounds).Intersects(RidgeInterior(bounds)))){rejects.Add("NEW_JUNCTION_PROP_OVERLAP");return false;}
                var terrainHits=Physics.RaycastAll(new Vector3(bounds.center.x,ground+5,bounds.center.z),Vector3.down,10,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider is TerrainCollider&&h.collider.GetComponent<Terrain>()?.transform.IsChildOf(world.transform)==true).ToArray();bool grounded=terrainHits.Any(h=>Mathf.Abs(h.point.y-ground)<.04f)&&Mathf.Abs(bounds.min.y-ground+.035f)<.02f;
                if(!grounded){rejects.Add("ACTUAL_TERRAIN_CONTACT");return false;}
                parts.Add(new VergeDressingPart{site=site,source=source,guid=AssetDatabase.AssetPathToGUID(source),name=root.name,position=root.position,rotation=root.rotation,scale=root.localScale,bounds=bounds,nativeGrounded=true,allOriginalExclusionsClear=true,physicsDisabled=true});root=null;return true;
            }
            finally{if(root!=null)UnityEngine.Object.DestroyImmediate(root.gameObject);}
        }
        static void DressingConfigureMaterials(Transform root,string folder)
        {
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;for(int i=0;i<materials.Length;i++)
                {
                    var source=materials[i];if(source==null)throw new InvalidOperationException("New original-art dressing has a missing source material.");if(DragonKingdomWorldVisualMigration.HasSupportedUrpShader(source))continue;
                    string guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));if(guid.Length!=32)throw new InvalidOperationException("Persistent source art material required.");string name=source.name.ToLowerInvariant();bool foliage=source.GetTag("RenderType",false,"")=="TransparentCutout"||(source.HasProperty("_Mode")&&source.GetFloat("_Mode")==1)||new[]{"leav","bush","grass","flower","plant"}.Any(name.Contains);
                    materials[i]=DragonKingdomWorldVisualMigration.ConfigureOwnedMaterial(source,folder+"/Materials/Finish/"+guid+" URP.mat",foliage,out _);
                }renderer.sharedMaterials=materials;
            }
        }
        static VergeDressingGroup DressingBuildSite(DragonKingdomWorld world,Scene scene,Transform parent,VergeDressingSite site,Collider[] originals,List<VergeDressingPart> parts)
        {
            var group=new VergeDressingGroup{id=site.id,kind=site.kind};var rejects=new List<string>();var sources=BoundedVergeDressingSources();Vector3 center=site.center;if(site.kind=="JUNCTION")center+=world.definition.graph.nodes.Single(n=>n.nodeId==site.node).position;center.y=DressingTerrainY(world,center);int before=parts.Count;
            var child=new GameObject(site.id);child.transform.SetParent(parent,false);
            if(site.kind=="ISLAND")
            {
                var knots=Enumerable.Range(0,8).Select(i=>{float a=i*Mathf.PI*.25f,r=i%3==0?.82f:1;return center+new Vector3(Mathf.Cos(a)*site.radius.x*r,2,Mathf.Sin(a)*site.radius.y*r);}).ToArray();
                foreach(bool tree in new[]{true,false})
                {
                    string[] paths=tree?sources.Take(3).ToArray():sources.Skip(3).Take(6).ToArray();float[] heights=paths.Select(p=>tree?22:p.Contains("grass")?1.3f:p.Contains("flower")?1.8f:3.5f).ToArray();var points=DressingVendorPoints(scene,knots,true,paths.Select(DressingSource).ToArray(),heights,tree?22:7,site.seed+(tree?0:100),false,group);int cap=tree?7:25,accepted=0;
                    foreach(var point in points){if(accepted>=cap)break;if(DressingBakePoint(world,child.transform,point,paths[point.prefabIndex],site.id,originals,parts,rejects))accepted++;}
                }
            }
            else if(site.kind=="TERRACE")
            {
                string[] paths={sources[9],sources[10],sources[3],sources[5]};var knots=new[]{center-new Vector3(site.radius.x,0,site.radius.y),center,center+new Vector3(site.radius.x,0,site.radius.y)};var points=DressingVendorPoints(scene,knots,false,paths.Select(DressingSource).ToArray(),new[]{9f,10f,4f,4f},9,site.seed,false,group);
                foreach(var point in points){if(parts.Count-before>=site.maximum)break;DressingBakePoint(world,child.transform,point,paths[point.prefabIndex],site.id,originals,parts,rejects);}
            }
            else
            {
                // Three authored roles at each approach, all selected through
                // actual vendor OnKnots distribution/projection, not a new placer.
                for(int i=0;i<3;i++)
                {
                    string path=sources[11+i];bool accepted=false;foreach(var offset in new[]{Vector3.zero,new Vector3(-8,0,0),new Vector3(8,0,0),new Vector3(0,0,8),new Vector3(0,0,-8)})
                    {
                        var position=center+new Vector3((i-1)*7,1,i==1?5:0)+offset;var points=DressingVendorPoints(scene,new[]{position,position+Vector3.forward*4},false,new[]{DressingSource(path)},new[]{i==0?9f:i==1?4f:6f},4,site.seed+i,true,group);
                        foreach(var point in points)if(DressingBakePoint(world,child.transform,point,path,site.id,originals,parts,rejects)){accepted=true;break;}if(accepted)break;
                    }
                    if(!accepted)throw new InvalidOperationException("Bounded existing-junction ensemble cannot place its actual "+path+" without violating original buffers.");
                }
            }
            group.accepted=parts.Count-before;group.rejected=rejects.Count;group.rejectionReasons=rejects.Distinct().ToArray();if(group.accepted<site.minimum||group.accepted>site.maximum)throw new InvalidOperationException("Bounded dressing site does not meet its declared useful population without original clearance changes: "+JsonUtility.ToJson(group));return group;
        }
        static Tuple<string,Vector3,Vector3>[] DressingViews()=>new[]{
            Tuple.Create("village-overview",new Vector3(-820,185,-610),new Vector3(-550,10,-340)),Tuple.Create("village-eye",new Vector3(-560,8,-520),new Vector3(-560,8,-300)),
            Tuple.Create("harbor-overview",new Vector3(920,210,-840),new Vector3(610,12,-490)),Tuple.Create("harbor-eye",new Vector3(600,8,-620),new Vector3(600,8,-420)),
            Tuple.Create("terrace-south",new Vector3(-420,360,550),new Vector3(-150,230,800)),Tuple.Create("terrace-upper",new Vector3(450,460,940),new Vector3(180,300,1170))};
        public static IEnumerator CaptureBoundedVendorVergeDressingReview()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");var sites=BoundedVergeDressingSites();ValidateBoundedVergeDressingPlan(sites);
            string tag=TowerAppearanceArg("-tsDragonWorldCandidate",""),owned=TowerAppearanceArg("-tsDragonDressingCandidate","");if(string.IsNullOrEmpty(tag)||string.IsNullOrEmpty(owned)||tag==owned||owned.Length>50||new[]{tag,owned}.Any(t=>t.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_')))throw new InvalidOperationException("Explicit passed parent and unique bounded dressing tag required.");
            string sourceFolder=Root+"VisualCandidates/"+tag,receipt=sourceFolder+"/Integration Receipt.json";var source=JsonUtility.FromJson<TowerAppearanceParent>(File.ReadAllText(Io(receipt)));if(source.tag!=tag||!source.originalPhysicalGatesPassed||!source.sourceBytesPreserved||source.status!="PHYSICAL_GATES_PASSED_REQUIRES_RENDER_AND_ACTUAL_DRIVING_REVIEW")throw new InvalidOperationException("Actual physically passed saved parent required.");
            string folder=Root+"VisualCandidates/"+owned,output=Path.Combine(Output(),"BoundedVergeDressing");if(Directory.Exists(Io(folder))||File.Exists(Io(folder+".meta"))||Directory.Exists(Io(output)))throw new InvalidOperationException("Preserve previous trials; fresh owned/evidence folders required.");Directory.CreateDirectory(Io(output));
            var proof=LastVergeDressingReview=new VergeDressingReview{sourceCandidate=tag,folder=folder,preview=folder+"/Bounded dressing preview.unity",prefab=folder+"/Baked visual dressing.prefab",sites=sites,islands=6,bands=2,junctions=4};var watched=new Dictionary<string,TowerAppearanceSource>(StringComparer.Ordinal);
            foreach(string path in AssetDatabase.GetDependencies(BoundedVergeDressingSources().Concat(new[]{source.prefab,source.preview,receipt}).ToArray(),true).Concat(BoundedVergeDressingSources()).Concat(new[]{source.prefab,source.preview,receipt}).Distinct())TowerWatchSource(watched,path,true);
            foreach(string path in Directory.GetFiles(Io(source.cavern.dataFolder),"*",SearchOption.AllDirectories))TowerWatchSource(watched,path,false);TowerWatchPipelineShaderSources(watched);
            var vendor=UnityEditor.PackageManager.PackageInfo.FindForAssetPath(DressingPackage);if(vendor==null||vendor.assetPath!=DressingPackage)throw new InvalidOperationException("Actual registered Spline Spawner required.");foreach(string file in Directory.GetFiles(vendor.resolvedPath,"*",SearchOption.AllDirectories))TowerWatchSource(watched,DressingPackage+"/"+file.Substring(vendor.resolvedPath.TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar).Length+1).Replace('\\','/'),false);
            var setup=PrepareValidQuayProbeSceneSetup();foreach(string path in setup.Select(s=>s.path).Where(p=>!string.IsNullOrEmpty(p)))TowerWatchSource(watched,path,true);proof.watchedSources=watched.Values.ToArray();Scene scene=default;GameObject cameraObject=null,dressing=null;DragonKingdomWorld world=null;var frames=new List<TowerAppearanceFrame>();var parts=new List<VergeDressingPart>();var groups=new List<VergeDressingGroup>();bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            try
            {
                Directory.CreateDirectory(Io(folder+"/Materials/Finish"));AssetDatabase.Refresh();scene=EditorSceneManager.OpenScene(source.preview,OpenSceneMode.Additive);if(!EditorSceneManager.SaveScene(scene,proof.preview,true))throw new InvalidOperationException("Explicit owned scene copy failed.");EditorSceneManager.CloseScene(scene,true);scene=EditorSceneManager.OpenScene(proof.preview,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);world=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();Physics.SyncTransforms();proof.originalWorldBefore=DressingStableWorld(world);var originals=world.GetComponentsInChildren<Collider>(true);
                cameraObject=new GameObject("Temporary dressing comparison camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.nearClipPlane=.3f;camera.farClipPlane=6500;camera.fieldOfView=52;camera.aspect=16f/9;camera.clearFlags=CameraClearFlags.Skybox;
                foreach(var view in DressingViews()){yield return TowerSettle(camera,view.Item2,view.Item3);frames.Add(TowerCapture(camera,output,view.Item1+"-before",1920,1080,view.Item3,world.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Select(m=>m.shader).Distinct().ToArray()));}
                dressing=new GameObject("Bounded village harbor and terrace visual dressing");SceneManager.MoveGameObjectToScene(dressing,scene);foreach(var site in sites){groups.Add(DressingBuildSite(world,scene,dressing.transform,site,originals,parts));yield return null;}
                DressingConfigureMaterials(dressing.transform,folder);proof.parts=parts.ToArray();proof.groups=groups.ToArray();proof.instances=parts.Count;proof.noAddedPhysics=dressing.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled)&&dressing.GetComponentsInChildren<Rigidbody>(true).All(b=>b.isKinematic&&!b.detectCollisions);proof.noRuntimeGenerator=dressing.GetComponentsInChildren<SplineSpawner>(true).Length==0&&dressing.GetComponentsInChildren<SplineContainer>(true).Length==0;proof.originalWorldAfter=DressingStableWorld(world);proof.originalWorldExact=proof.originalWorldBefore==proof.originalWorldAfter;
                if(!proof.originalWorldExact||!proof.noAddedPhysics||!proof.noRuntimeGenerator||proof.instances>240)throw new InvalidOperationException("Dressing trial changed original world, added physical obstruction or retained a runtime generator.");
                if(PrefabUtility.SaveAsPrefabAssetAndConnect(dressing,proof.prefab,InteractionMode.AutomatedAction)==null)throw new InvalidOperationException("Baked owned dressing prefab did not save.");UnityEngine.Object.DestroyImmediate(cameraObject);cameraObject=null;if(!EditorSceneManager.SaveScene(scene,proof.preview))throw new InvalidOperationException("Owned dressed scene did not save.");EditorSceneManager.CloseScene(scene,true);scene=EditorSceneManager.OpenScene(proof.preview,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);world=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();dressing=scene.GetRootGameObjects().Single(r=>r.name=="Bounded village harbor and terrace visual dressing");proof.originalWorldReloaded=DressingStableWorld(world);proof.savedReloaded=proof.originalWorldReloaded==proof.originalWorldBefore;
                if(!proof.savedReloaded)throw new InvalidOperationException("Saved/reloaded dressing copy changed original world identity/physics/appearance.");cameraObject=new GameObject("Temporary saved dressing comparison camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.nearClipPlane=.3f;camera.farClipPlane=6500;camera.fieldOfView=52;camera.aspect=16f/9;camera.clearFlags=CameraClearFlags.Skybox;
                foreach(var view in DressingViews()){yield return TowerSettle(camera,view.Item2,view.Item3);frames.Add(TowerCapture(camera,output,view.Item1+"-saved-after",1920,1080,view.Item3,scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Renderer>(true)).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Select(m=>m.shader).Distinct().ToArray()));}
                proof.status="SAVED_VENDOR_DRESSING_CAPTURED_REQUIRES_LEAD_VISUAL_REVIEW_NOT_WORLD_ADOPTION";
            }
            finally
            {
                if(cameraObject!=null)UnityEngine.Object.DestroyImmediate(cameraObject);if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);ShaderUtil.allowAsyncCompilation=async;
                proof.incomingSceneContextRestored=setup.Select(s=>s.path+"|"+s.isLoaded+"|"+s.isActive).SequenceEqual(EditorSceneManager.GetSceneManagerSetup().Select(s=>s.path+"|"+s.isLoaded+"|"+s.isActive));proof.changedSources=watched.Values.Where(p=>p.resourceKind!="UNITY_BUILTIN_VIRTUAL_RESOURCE_NO_DISK_FILE"&&(!File.Exists(p.resolvedPath)||TowerFileHash(p.resolvedPath)!=p.sha256)).Select(p=>p.path).ToArray();proof.originalSourcesExact=proof.changedSources.Length==0;proof.originalTerrainBytesExact=proof.originalSourcesExact;proof.parts=parts.ToArray();proof.groups=groups.ToArray();proof.frames=frames.ToArray();if(!proof.originalSourcesExact||!proof.incomingSceneContextRestored)proof.status="REJECTED_SOURCE_OR_CONTEXT_PRESERVATION";File.WriteAllText(Io(Path.Combine(output,"bounded-vendor-verge-dressing-review.json")),JsonUtility.ToJson(proof,true));if(!proof.originalSourcesExact||!proof.incomingSceneContextRestored)throw new InvalidOperationException("Original sources or incoming scene context changed during dressing trial.");
            }
        }
    }
}
#endif
