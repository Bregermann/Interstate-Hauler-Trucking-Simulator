#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using LWS.TruckCamping;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

namespace LWS.TruckDragonKingdom.Ambience
{
    public static partial class DragonKingdomAmbienceAuthoring
    {
        public const string Folder="Assets/TruckDragonKingdom/Ambience";
        const string BirdFolder="Assets/TruckBaseJumping/ThirdParty/QuaterniusBirds/";
        const string Weather="Assets/WeatherMaker/Prefab/Sound/";
        static readonly string[] Towns={"village","village","castle","harbor","refuge"};
        sealed class Habitat { public string id,region;public Vector3[] points;public NavMeshData data; }
        [Serializable] public sealed class Receipt {public string scope;public int walkers,birds,spatialSources;public string[] sourceAssets,groundHabitats,airHabitats;}
        public static DragonKingdomAmbience Install(DragonKingdomWorld world)
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling||world==null||EditorUtility.IsPersistent(world))throw new InvalidOperationException("Explicit stopped scene composition over the actual accepted kingdom required.");
            world.ValidateReferences();
            var existing=world.GetComponentInChildren<DragonKingdomAmbience>(true);
            if(existing!=null){existing.ValidateBindings();BindSavedSession(existing);return existing;}
            RequireSources();Physics.SyncTransforms();
            var originalRoutes=world.regions.ToDictionary(region=>region,region=>region.ambientRoutes);
            // Find every real habitat before creating any visible actor. No
            // terrain, road, collider, objective or source prefab is changed.
            var habitats=new List<Habitat>();
            var groundFailures=new List<string>();
            void FindHabitat(string id,string region,int phase)
            {
                try{habitats.Add(new Habitat{id=id,region=region,points=FindGroundLoop(world,world.Region(region),phase,habitats)});}
                catch(InvalidOperationException exception){groundFailures.Add(id+": "+exception.Message);}
            }
            foreach(var pair in Towns.Select((region,index)=>(region,index)))FindHabitat("villager-"+pair.index,pair.region,pair.index);
            FindHabitat("woodland-deer","forest",6);FindHabitat("woodland-rabbit","forest",10);FindHabitat("refuge-wolf","mountains",14);
            if(groundFailures.Count>0)throw new InvalidOperationException("All8 living ground habitat searches completed before scene mutation; no actors omitted. "+string.Join("\n",groundFailures));
            foreach(var habitat in habitats)habitat.data=BakeHabitat(world,habitat);
            var air=new Dictionary<string,Vector3[]>();
            foreach(var region in world.regions.Where(region=>region.id!="caverns"&&region.id!="volcano"))air.Add(region.id,FindAirLoop(world,region));
            var root=new GameObject("Bounded living kingdom ambience");root.transform.SetParent(world.transform,false);
            var owner=root.AddComponent<DragonKingdomAmbience>();owner.world=world;owner.navigation=habitats.Select(habitat=>habitat.data).ToArray();
            var walkers=new List<DragonAmbientWalker>();var birds=new List<DragonAmbientBird>();var sounds=new List<DragonRegionAmbience>();
            try
            {
                foreach(var habitat in habitats)
                {
                    var parent=new GameObject("Offroad habitat "+habitat.id).transform;parent.SetParent(root.transform,false);
                    var actor=new GameObject("Animated "+habitat.id);actor.transform.SetParent(parent,false);actor.transform.position=habitat.points[0];
                    var life=actor.AddComponent<DragonAmbientWalker>();life.regionId=habitat.region;life.authoredPosition=actor.transform.position;life.route=Route(parent,habitat.points,"Ground waypoint");life.villager=habitat.id.StartsWith("villager",StringComparison.Ordinal);
                    if(life.villager)
                    {
                        int index=int.Parse(habitat.id.Substring("villager-".Length));life.sourceAsset="Assets/LWS/TruckTaxi/Prefabs/Pedestrians/"+(index%2==0?"Taxi_Girl_11":"Taxi_Girl_22")+".prefab";
                        life.animator=DragonAmbientVillagerAuthoring.SpawnOriginal(life.sourceAsset,actor.transform,actor.transform.position,actor.transform.position+Vector3.forward);
                        life.animator.gameObject.name="Ordinary villager · original Taxi appearance";
                        life.idleState="idle1";life.walkState="walk";
                        life.pace=.95f;
                    }
                    else
                    {
                        life.sourceAsset=habitat.id.Contains("deer")?"Assets/TruckCamping/Prefabs/Original animated Deer.prefab":habitat.id.Contains("rabbit")?"Assets/TruckCamping/Prefabs/Original animated Rabbit.prefab":"Assets/TruckFishing/CarnivalPresentation/Genuine animated ambient wolf.prefab";
                        var art=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(life.sourceAsset),actor.transform);PrefabUtility.UnpackPrefabInstance(art,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);art.transform.localPosition=Vector3.zero;
                        foreach(var joint in art.GetComponentsInChildren<Joint>(true))UnityEngine.Object.DestroyImmediate(joint);
                        foreach(var collider in art.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
                        foreach(var body in art.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(body);
                        life.living=art.GetComponentInChildren<CampLivingArt>(true);life.animator=art.GetComponentInChildren<Animator>(true);life.pace=habitat.id.Contains("rabbit")?1.35f:1.05f;
                    }
                    ConfigureAppearance(actor);life.visuals=actor.GetComponentsInChildren<Renderer>(true).Where(renderer=>renderer.enabled&&renderer.gameObject.activeInHierarchy).ToArray();life.authoredRotation=actor.transform.rotation;
                    life.agent=actor.AddComponent<NavMeshAgent>();life.agent.enabled=false;life.agent.agentTypeID=0;life.agent.radius=.42f;life.agent.height=2;life.agent.baseOffset=0;life.agent.obstacleAvoidanceType=ObstacleAvoidanceType.MedQualityObstacleAvoidance;
                    if(life.animator!=null){life.animator.applyRootMotion=false;life.animator.fireEvents=false;life.animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;life.animator.enabled=false;}
                    if(life.living!=null)life.living.enabled=false;
                    life.ValidateBindings();walkers.Add(life);AppendRoutes(world.Region(habitat.region),life.route);
                }
                foreach(var region in world.regions.Where(region=>air.ContainsKey(region.id)))
                {
                    var parent=new GameObject("Clear ambient air "+region.id).transform;parent.SetParent(root.transform,false);var actor=new GameObject("Genuine winged ambient bird · "+region.id);actor.transform.SetParent(parent,false);actor.transform.position=air[region.id][0];
                    bool eagle=region.id=="mountains"||region.id=="nests"||region.id=="refuge";string source=BirdFolder+(eagle?"Eagle.fbx":"bird.fbx");
                    var art=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(source),actor.transform);art.transform.localPosition=Vector3.zero;
                    var flight=actor.AddComponent<DragonAmbientBird>();flight.regionId=region.id;flight.sourceAsset=source;flight.animator=art.GetComponentInChildren<Animator>(true);flight.animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BirdFolder+(eagle?"Eagle flight.controller":"Small bird flight.controller"));flight.animator.applyRootMotion=false;flight.animator.fireEvents=false;flight.animator.enabled=false;
                    FitSpan(art,eagle?2.8f:1);ConfigureAppearance(actor);flight.route=Route(parent,air[region.id],"Air waypoint");flight.authoredPosition=actor.transform.position;flight.authoredRotation=actor.transform.rotation;flight.visuals=actor.GetComponentsInChildren<Renderer>(true).Where(renderer=>renderer.enabled&&renderer.gameObject.activeInHierarchy).ToArray();flight.duration=eagle?55:42;flight.ValidateBindings();birds.Add(flight);AppendRoutes(region,flight.route);
                }
                foreach(var region in world.regions)
                {
                    var go=new GameObject("Spatial region ambience · "+region.id);go.transform.SetParent(root.transform,false);go.transform.position=SoundPosition(world,region);
                    var source=go.AddComponent<AudioSource>();source.clip=ClipFor(region.id);source.playOnAwake=false;source.loop=true;source.spatialBlend=1;source.rolloffMode=AudioRolloffMode.Logarithmic;source.minDistance=12;source.maxDistance=region.id=="forest"?200:260;source.dopplerLevel=0;source.priority=200;source.volume=0;
                    if(region.id=="caverns"){var reverb=go.AddComponent<AudioReverbZone>();reverb.reverbPreset=AudioReverbPreset.Cave;reverb.minDistance=35;reverb.maxDistance=90;}
                    source.loop=region.id!="castle";
                    sounds.Add(new DragonRegionAmbience{regionId=region.id,source=source,looping=source.loop,minimumInterval=45,volume=region.id=="volcano"?.22f:region.id=="castle"?.09f:.15f});
                }
                owner.walkers=walkers.ToArray();owner.birds=birds.ToArray();owner.sounds=sounds.ToArray();owner.ValidateBindings();BindSavedSession(owner);
                world.resettableComponents=world.resettableComponents.Concat(new MonoBehaviour[]{owner}).ToArray();EditorUtility.SetDirty(world);EditorUtility.SetDirty(owner);
                Directory.CreateDirectory(Folder+"/Documentation");File.WriteAllText(Folder+"/Documentation/LastAuthoring.json",JsonUtility.ToJson(new Receipt{scope="Explicit native authoring receipt only; route/navmesh checks do not imply rendered, auditory, full-match or performance acceptance.",walkers=walkers.Count,birds=birds.Count,spatialSources=sounds.Count,sourceAssets=walkers.Select(value=>value.sourceAsset).Concat(birds.Select(value=>value.sourceAsset)).Concat(sounds.Select(value=>AssetDatabase.GetAssetPath(value.source.clip))).Distinct().ToArray(),groundHabitats=habitats.Select(value=>value.id).ToArray(),airHabitats=air.Keys.ToArray()},true));
                return owner;
            }
            catch {foreach(var region in world.regions)region.ambientRoutes=originalRoutes[region];UnityEngine.Object.DestroyImmediate(root);throw; }
        }
        static void BindSavedSession(DragonKingdomAmbience owner)
        {
            var sessions=owner.gameObject.scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<SportsEventSession>(true)).ToArray();
            if(sessions.Length>1)throw new InvalidOperationException("One actual mode session must own ambience lifecycle.");
            if(sessions.Length==1){owner.session=sessions[0];var match=sessions[0] as DragonKingdomGroundMatch;if(match!=null)owner.observers=match.trucks.Select(truck=>truck.transform).ToArray();EditorUtility.SetDirty(owner);}
        }
        static void RequireSources()
        {
            foreach(string path in new[]{"Assets/LWS/TruckTaxi/Prefabs/Pedestrians/Taxi_Girl_11.prefab","Assets/LWS/TruckTaxi/Prefabs/Pedestrians/Taxi_Girl_22.prefab","Assets/TruckCamping/Prefabs/Original animated Deer.prefab","Assets/TruckCamping/Prefabs/Original animated Rabbit.prefab","Assets/TruckFishing/CarnivalPresentation/Genuine animated ambient wolf.prefab",BirdFolder+"Eagle.fbx",BirdFolder+"bird.fbx"})if(AssetDatabase.LoadAssetAtPath<GameObject>(path)==null)throw new InvalidOperationException("Existing owned living art required: "+path);
            foreach(string id in new[]{"village","castle","harbor","forest","mountains","refuge","caverns","volcano","nests"})if(ClipFor(id)==null)throw new InvalidOperationException("Actual owned spatial ambience clip required in "+id);
            DragonAmbientVillagerAuthoring.ValidateOriginalProfiles();
        }
        static AudioClip ClipFor(string id)
        {
            string path=id=="volcano"?Folder+"/PrivateLicensedAudio/Fire Loop.wav":id=="forest"?"Assets/RiverModeler/SFX/River Cascade large.wav":id=="harbor"?"Assets/RiverModeler/SFX/Shallow Creek Fast.wav":id=="castle"?"Assets/TruckDockDiving/Audio/Exceptional cheer.wav":id=="caverns"?Weather+"Ambient/WeatherMakerAmbientSound_EnergyHum.ogg":id=="village"?Folder+"/PrivateLicensedAudio/Birds Loop.wav":id=="refuge"?Weather+"Ambient/WeatherMakerAmbientSound_Crickets.ogg":id=="nests"?Weather+"Ambient/WeatherMakerAmbientSound_Crow.ogg":Weather+"Weather/WeatherMakerWind1.ogg";
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        static Transform[] Route(Transform parent,Vector3[] points,string label)
        {return points.Select((position,index)=>{var go=new GameObject(label+" "+index);go.transform.SetParent(parent,false);go.transform.position=position;return go.transform;}).ToArray();}
        static void AppendRoutes(DragonWorldRegion region,Transform[] points){region.ambientRoutes=region.ambientRoutes.Concat(points).ToArray();}
        public static bool ClearOfRoadsAndObjectives(DragonKingdomWorld world,Vector3 point,float margin=12)
        {
            if(!float.IsFinite(point.x)||!float.IsFinite(point.y)||!float.IsFinite(point.z)||!float.IsFinite(margin)||margin<0)return false;
            foreach(var edge in world.definition.graph.edges)
                for(int i=1;i<edge.samples.Count;i++)
                {
                    var a=edge.samples[i-1];var b=edge.samples[i];float width=Mathf.Max(a.roadWidthMeters,b.roadWidthMeters,edge.laneWidthMeters*edge.laneCount+edge.leftShoulderWidthMeters+edge.rightShoulderWidthMeters);
                    if(DistanceXZ(point,a.position,b.position)<width*.5f+margin)return false;
                }
            foreach(var region in world.regions)foreach(var anchor in region.truckStarts.Concat(region.perches).Concat(region.nests).Concat(region.treasureCaches))if(anchor!=null&&Vector3.Distance(point,anchor.position)<28)return false;
            foreach(var site in world.extractionSites)if(site.approach!=null&&Vector3.Distance(point,site.approach.position)<45||site.secureZone!=null&&site.secureZone.bounds.SqrDistance(point)<45*45)return false;
            return true;
        }
        public static float DistanceXZ(Vector3 point,Vector3 a,Vector3 b)
        {point.y=a.y=b.y=0;Vector3 delta=b-a;float length=delta.sqrMagnitude;return Vector3.Distance(point,a+delta*(length<.00001f?0:Mathf.Clamp01(Vector3.Dot(point-a,delta)/length)));}
        static Vector3[] FindGroundLoop(DragonKingdomWorld world,DragonWorldRegion region,int phase,List<Habitat> reserved)
            =>DragonAmbientHabitatSearch.Find(world,region,phase,reserved.Where(habitat=>habitat.region==region.id).Select(habitat=>habitat.points).ToArray());
        static NavMeshData BakeHabitat(DragonKingdomWorld world,Habitat habitat)
        {
            var bounds=new Bounds(habitat.points[0],Vector3.zero);foreach(var point in habitat.points)bounds.Encapsulate(point);bounds.Expand(new Vector3(10,12,10));
            var sources=new List<NavMeshBuildSource>();NavMeshBuilder.CollectSources(bounds,~0,NavMeshCollectGeometry.PhysicsColliders,0,new List<NavMeshBuildMarkup>(),sources);sources.RemoveAll(source=>source.component is Collider collider&&(collider.attachedRigidbody!=null||collider.GetComponentInParent<DragonAmbientWalker>()!=null||collider.GetComponentInParent<DragonAmbientBird>()!=null));
            var settings=NavMesh.GetSettingsByID(0);settings.agentRadius=.42f;settings.agentHeight=2;settings.agentClimb=.25f;settings.agentSlope=30;settings.overrideVoxelSize=true;settings.voxelSize=.15f;settings.overrideTileSize=true;settings.tileSize=128;
            var data=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);if(data==null)throw new InvalidOperationException("Actual bounded habitat navigation bake failed: "+habitat.id);
            var handle=NavMesh.AddNavMeshData(data);string digest;bool accepted=false;
            try
            {
                var path=new NavMeshPath();for(int i=0;i<habitat.points.Length;i++)
                {
                    if(!NavMesh.SamplePosition(habitat.points[i],out var sample,.5f,NavMesh.AllAreas)||Vector3.Distance(sample.position,habitat.points[i])>.5f||!NavMesh.CalculatePath(habitat.points[i],habitat.points[(i+1)%habitat.points.Length],NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)throw new InvalidOperationException("Incomplete real habitat path: "+habitat.id);
                    foreach(var point in path.corners)if(!ClearOfRoadsAndObjectives(world,point,10))throw new InvalidOperationException("Native habitat path enters a road/objective: "+habitat.id);
                }
                // Include the real baked geometry, not only the requested
                // route, so a world change cannot silently reuse stale data.
                var vertices=NavMesh.CalculateTriangulation().vertices.Where(point=>bounds.Contains(point)).OrderBy(point=>point.x).ThenBy(point=>point.y).ThenBy(point=>point.z).ToArray();
                using(var sha=SHA256.Create())digest=string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Points{values=habitat.points})+JsonUtility.ToJson(new Points{values=vertices}))).Take(8).Select(value=>value.ToString("x2")));
                accepted=true;
            }
            catch(Exception exception)
            {
                DragonAmbienceClearanceDiagnostics.Write(world,world.Region(habitat.region),"nav-"+habitat.id,habitat.points.Length,new[]{new DragonAmbienceClearanceDiagnostics.Rejection{reason=exception.Message,point=habitat.points[0]}});
                throw;
            }
            // Remove the native registration exactly once, before destroying
            // failed data. Exception diagnostics never leave a stale handle.
            finally{handle.Remove();if(!accepted)UnityEngine.Object.DestroyImmediate(data);}
            string folder=Folder+"/Generated/Navigation";Directory.CreateDirectory(folder);AssetDatabase.Refresh();string asset=folder+"/"+habitat.id+"-"+digest+".asset";
            var saved=AssetDatabase.LoadAssetAtPath<NavMeshData>(asset);if(saved==null){data.name=habitat.id+" bounded actual navigation";AssetDatabase.CreateAsset(data,asset);return data;}UnityEngine.Object.DestroyImmediate(data);return saved;
        }
        [Serializable] sealed class Points{public Vector3[] values;}
        static Vector3[] FindAirLoop(DragonKingdomWorld world,DragonWorldRegion region,Vector3? suppliedHint=null)
        {
            if(region.ambientRoutes.Length<2)throw new InvalidOperationException("Actual authored regional air hints required: "+region.id);
            var hint=suppliedHint??(region.ambientRoutes[0].position+region.ambientRoutes[1].position)*.5f;
            var rejected=new List<DragonAmbienceClearanceDiagnostics.Rejection>();
            for(int offset=0;offset<8;offset++)
            {
                var points=new Vector3[12];bool valid=true;Vector3 center=hint+new Vector3((offset%3-1)*40,(offset/3)*20,35);
                for(int i=0;i<12;i++){float a=i*Mathf.PI/6;points[i]=center+new Vector3(Mathf.Cos(a)*35,Mathf.Sin(a*2)*4,Mathf.Sin(a)*25);}
                for(int i=0;i<12&&valid;i++)
                {
                    var from=points[i];var delta=points[(i+1)%12]-from;
                    valid=!Physics.CheckSphere(from,.8f,~0,QueryTriggerInteraction.Ignore)&&!Physics.SphereCast(from,.8f,delta.normalized,out _,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
                    foreach(var objective in world.aerialCheckpoints.Concat(world.riderStarts))if(objective!=null&&Vector3.Distance(from,objective.position)<30)valid=false;
                    if(!valid)DragonAmbienceClearanceDiagnostics.Reject(rejected,"air-body-or-swept-corridor-or-rider-clearance",from);
                }
                if(valid)return points;
            }
            string report=DragonAmbienceClearanceDiagnostics.Write(world,region,"air",8,rejected);
            throw new InvalidOperationException("No physically clear bounded ambient bird corridor in "+region.id+"; native witnesses: "+report);
        }
        static Vector3 SoundPosition(DragonKingdomWorld world,DragonWorldRegion region)
        {
            if(region.id=="forest")return new Vector3(-1050,175,580);
            if(region.id=="volcano")return new Vector3(1370,333,1050);
            return region.root.position+Vector3.up*3;
        }
        static void FitSpan(GameObject actor,float width)
        {var bounds=LWS.TruckDragonKingdom.Editor.DragonKingdomWorldAuthoring.MeasureOwnedModelBounds(actor.transform);actor.transform.localScale*=width/Mathf.Max(.05f,Mathf.Max(bounds.size.x,bounds.size.z));}
        static void ConfigureAppearance(GameObject actor)
        {
            foreach(var renderer in actor.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;if(renderer is SkinnedMeshRenderer skin)skin.updateWhenOffscreen=false;
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var source=materials[i];if(source==null||source.shader==null)throw new InvalidOperationException("Original living appearance has a missing material.");
                    if(source.GetTag("RenderPipeline",false,"")=="UniversalPipeline"||source.shader.name.StartsWith("Universal Render Pipeline/",StringComparison.Ordinal))continue;
                    string guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));if(string.IsNullOrEmpty(guid))throw new InvalidOperationException("Actual persistent source material required.");
                    string folder=Folder+"/Generated/Materials";if(!Directory.Exists(folder)){Directory.CreateDirectory(folder);AssetDatabase.Refresh();}string path=folder+"/"+guid+" original appearance URP.mat";var owned=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(owned==null)
                    {
                        owned=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=source.name+" · original appearance URP"};
                        if(source.mainTexture!=null){owned.SetTexture("_BaseMap",source.mainTexture);owned.SetTextureScale("_BaseMap",source.mainTextureScale);owned.SetTextureOffset("_BaseMap",source.mainTextureOffset);}
                        owned.SetColor("_BaseColor",source.HasProperty("_BaseColor")?source.GetColor("_BaseColor"):source.HasProperty("_Color")?source.color:Color.white);
                        if(source.HasProperty("_BumpMap")&&source.GetTexture("_BumpMap")!=null){owned.SetTexture("_BumpMap",source.GetTexture("_BumpMap"));owned.EnableKeyword("_NORMALMAP");}
                        if(source.IsKeywordEnabled("_ALPHATEST_ON")){owned.SetFloat("_AlphaClip",1);owned.SetFloat("_Cutoff",source.HasProperty("_Cutoff")?source.GetFloat("_Cutoff"):.5f);owned.EnableKeyword("_ALPHATEST_ON");}
                        owned.SetFloat("_Smoothness",.2f);AssetDatabase.CreateAsset(owned,path);
                    }
                    materials[i]=owned;
                }
                renderer.sharedMaterials=materials;
            }
        }
    }
}
#endif
