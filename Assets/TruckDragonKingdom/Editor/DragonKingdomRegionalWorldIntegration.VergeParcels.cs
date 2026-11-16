#if UNITY_EDITOR && VISTA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using sc.splines.spawner.runtime;
namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        [Serializable]public sealed class VergeParcelAttempt{public string site,reason;public int trial,accepted;public Vector3 center;public bool originalRegionContained,passed;}
        [Serializable]public sealed class SupportedVergeReview{public string scope="BOUNDED_SAME_REGION_PARCEL_REVISION_WITH_UNCHANGED_MINIMUM_AND_ORIGINAL_GUARDS_NOT_ADOPTION",status="IN_PROGRESS",error,stage,cleanupError;public VergeDressingReview dressing;public VergeParcelAttempt[] parcelAttempts;}
        [Serializable]public sealed class DressingVendorFixture{public int originalDistributed,originalProjected,supportedDistributed,supportedProjected;public bool sameSeedExact,startupNone,noRetainedGenerator;public float maximumProjectionError;}
        public static SupportedVergeReview LastSupportedVergeReview{get;private set;}
        public static Vector3[] BoundedDressingParcelOffsets()=>new[]{Vector3.zero,new Vector3(-48,0,0),new Vector3(48,0,0),new Vector3(0,0,48),new Vector3(0,0,-48),new Vector3(-48,0,48),new Vector3(48,0,48),new Vector3(-48,0,-48),new Vector3(48,0,-48),new Vector3(-84,0,0),new Vector3(84,0,0),new Vector3(0,0,84),new Vector3(0,0,-84)};
        public static void ValidateDressingParcelOffsets(Vector3[] offsets)
        {if(offsets==null||offsets.Length!=13||offsets[0]!=Vector3.zero||offsets.Distinct().Count()!=offsets.Length||offsets.Any(p=>!RidgeFinite(p)||p.y!=0||p.magnitude>84.001f))throw new InvalidOperationException("Only13 bounded deterministic same-region parcel candidates within84m are allowed; original exclusions and useful population are unchanged.");}
        static bool DressingPartInsideRegion(Bounds region,Bounds art)=>art.min.x>=region.min.x&&art.max.x<=region.max.x&&art.min.z>=region.min.z&&art.max.z<=region.max.z;
        static VergeDressingGroup DressingBuildSupportedParcel(DragonKingdomWorld world,Scene scene,Transform parent,VergeDressingSite original,Collider[] originals,List<VergeDressingPart> parts,List<VergeParcelAttempt> attempts)
        {
            var offsets=BoundedDressingParcelOffsets();ValidateDressingParcelOffsets(offsets);Bounds region=world.definition.FindRegion(original.region).bounds;
            foreach(var indexed in offsets.Select((offset,index)=>(offset,index)))
            {
                int first=parts.Count;var oldChildren=new HashSet<Transform>(parent.Cast<Transform>());var site=new VergeDressingSite{id=original.id,kind=original.kind,region=original.region,node=original.node,center=original.center+indexed.offset,radius=original.radius,seed=original.seed,minimum=original.minimum,maximum=original.maximum};var attempt=new VergeParcelAttempt{site=site.id,trial=indexed.index,center=site.center};attempts.Add(attempt);
                Vector3 center=site.center;if(site.kind=="JUNCTION")center+=world.definition.graph.nodes.Single(n=>n.nodeId==site.node).position;float rx=site.radius.x,rz=site.radius.y;
                if(center.x-rx<region.min.x||center.x+rx>region.max.x||center.z-rz<region.min.z||center.z+rz>region.max.z){attempt.reason="WHOLE_PROPOSED_PARCEL_OUTSIDE_ACTUAL_ORIGINAL_REGION";continue;}bool accepted=false;
                try
                {
                    var group=DressingBuildSite(world,scene,parent,site,originals,parts);attempt.accepted=parts.Count-first;attempt.originalRegionContained=parts.Skip(first).All(p=>DressingPartInsideRegion(region,p.bounds)&&p.bounds.min.y> -1.75f);
                    if(!attempt.originalRegionContained){attempt.reason="ACTUAL_PRIMARY_ART_EXTENDS_OUTSIDE_REGION_OR_UNDER_WATER";continue;}
                    // Distinct islands must remain distinct compositions, not
                    // collapse all six named sites into the same easy parcel.
                    if(site.kind=="ISLAND"&&parts.Take(first).Where(p=>p.site!=site.id).Any(prior=>parts.Skip(first).Any(current=>RidgeInterior(current.bounds).Intersects(RidgeInterior(prior.bounds))))){attempt.reason="ACCEPTED_EARLIER_SITE_VISUAL_OVERLAP";continue;}
                    attempt.passed=accepted=true;return group;
                }
                catch(InvalidOperationException error)
                {
                    attempt.accepted=parts.Count-first;attempt.reason=error.Message;
                    if(!error.Message.StartsWith("Bounded dressing site does not meet",StringComparison.Ordinal)&&!error.Message.StartsWith("Bounded existing-junction ensemble cannot place",StringComparison.Ordinal))throw;
                }
                finally
                {
                    if(!accepted){foreach(var child in parent.Cast<Transform>().Where(c=>!oldChildren.Contains(c)).ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);parts.RemoveRange(first,parts.Count-first);}
                }
            }
            throw new InvalidOperationException("All13 unchanged-guard same-region parcels exhausted for "+original.id+"; never relax8-target useful population or road/physics/start/objective/aerial/cave buffers.");
        }
        public static DressingVendorFixture InspectActualDressingVendorProjectionFixture()
        {
            var result=new DressingVendorFixture();var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;
            try
            {
                scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);var ground=new GameObject("Temporary supported Spline Spawner fixture");SceneManager.MoveGameObjectToScene(ground,scene);ground.transform.position=new Vector3(9000,9.5f,9000);ground.AddComponent<BoxCollider>().size=new Vector3(100,1,100);Physics.SyncTransforms();var prefab=DressingSource(BoundedVergeDressingSources()[3]);var rows=new List<string[]>();
                for(int trial=0;trial<3;trial++)
                {
                    float half=trial==0?14:32;var knots=new[]{new Vector3(9000-half,20,9000-half),new Vector3(9000+half,20,9000-half),new Vector3(9000+half,20,9000+half),new Vector3(9000-half,20,9000+half)};var group=new VergeDressingGroup{id="supported-vendor-fixture-"+trial};int distributed=0;SplineSpawner.Action inspect=(spawner,index)=>{if(spawner.name=="Temporary actual Spline Spawner "+group.id)spawner.onAfterDistribution+=points=>distributed=points.Length;};List<sc.splines.spawner.runtime.SpawnPoint> sampled;
                    SplineSpawner.onPreRespawn+=inspect;try{sampled=DressingVendorPoints(scene,knots,true,new[]{prefab},new[]{2f},7,991,false,group);}finally{SplineSpawner.onPreRespawn-=inspect;}
                    if(trial==0){result.originalDistributed=distributed;result.originalProjected=sampled.Count;}else{result.supportedDistributed=distributed;result.supportedProjected=sampled.Count;rows.Add(sampled.Select(p=>JsonUtility.ToJson(p)).ToArray());foreach(var p in sampled)result.maximumProjectionError=Mathf.Max(result.maximumProjectionError,Mathf.Abs(p.position.y-10));}result.startupNone=group.startupNone;result.noRetainedGenerator=group.noRetainedGenerator;
                }
                result.sameSeedExact=rows[0].SequenceEqual(rows[1]);return result;
            }
            finally{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        static void SaveSupportedVergeReview(SupportedVergeReview review)
        {string output=Path.Combine(Output(),"SupportedVergeDressing");Directory.CreateDirectory(Io(output));File.WriteAllText(Io(Path.Combine(output,"supported-verge-dressing-review.json")),JsonUtility.ToJson(review,true));}
        public static IEnumerator CaptureSupportedVendorVergeDressingReview()
        {
            var review=LastSupportedVergeReview=new SupportedVergeReview();var stack=new Stack<IEnumerator>();stack.Push(SupportedVergeRoutine(review));
            try
            {
                while(stack.Count>0)
                {
                    bool moved;object current=null;try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;else(stack.Pop() as IDisposable)?.Dispose();}
                    catch(Exception error){while(stack.Count>0){try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception cleanup){review.cleanupError=cleanup.ToString();}}review.error=error.ToString();review.status="REJECTED_SUPPORTED_PARCEL_OR_NATIVE_DRESSING";SaveSupportedVergeReview(review);throw;}
                    if(moved){if(current is IEnumerator nested)stack.Push(nested);else yield return current;}
                }
            }
            finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();}
        }
        static IEnumerator SupportedVergeRoutine(SupportedVergeReview review)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");var sites=BoundedVergeDressingSites();ValidateBoundedVergeDressingPlan(sites);
            string tag=TowerAppearanceArg("-tsDragonWorldCandidate",""),owned=TowerAppearanceArg("-tsDragonDressingCandidate","");if(string.IsNullOrEmpty(tag)||string.IsNullOrEmpty(owned)||tag==owned||owned.Length>50||new[]{tag,owned}.Any(t=>t.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_')))throw new InvalidOperationException("Explicit passed parent and unique bounded dressing tag required.");
            string sourceFolder=Root+"VisualCandidates/"+tag,receipt=sourceFolder+"/Integration Receipt.json";var source=JsonUtility.FromJson<TowerAppearanceParent>(File.ReadAllText(Io(receipt)));if(source.tag!=tag||!source.originalPhysicalGatesPassed||!source.sourceBytesPreserved||source.status!="PHYSICAL_GATES_PASSED_REQUIRES_RENDER_AND_ACTUAL_DRIVING_REVIEW")throw new InvalidOperationException("Actual physically passed saved parent required.");
            string folder=Root+"VisualCandidates/"+owned,output=Path.Combine(Output(),"SupportedVergeDressing");if(Directory.Exists(Io(folder))||File.Exists(Io(folder+".meta"))||Directory.Exists(Io(output)))throw new InvalidOperationException("Preserve previous trials; fresh owned/evidence folders required.");Directory.CreateDirectory(Io(output));
            var proof=review.dressing=new VergeDressingReview{sourceCandidate=tag,folder=folder,preview=folder+"/Bounded dressing preview.unity",prefab=folder+"/Baked visual dressing.prefab",sites=sites,islands=6,bands=2,junctions=4};var watched=new Dictionary<string,TowerAppearanceSource>(StringComparer.Ordinal);
            foreach(string path in AssetDatabase.GetDependencies(BoundedVergeDressingSources().Concat(new[]{source.prefab,source.preview,receipt}).ToArray(),true).Concat(BoundedVergeDressingSources()).Concat(new[]{source.prefab,source.preview,receipt}).Distinct())TowerWatchSource(watched,path,true);
            foreach(string path in Directory.GetFiles(Io(source.cavern.dataFolder),"*",SearchOption.AllDirectories))TowerWatchSource(watched,path,false);TowerWatchPipelineShaderSources(watched);
            var vendor=UnityEditor.PackageManager.PackageInfo.FindForAssetPath(DressingPackage);if(vendor==null||vendor.assetPath!=DressingPackage)throw new InvalidOperationException("Actual registered Spline Spawner required.");foreach(string file in Directory.GetFiles(vendor.resolvedPath,"*",SearchOption.AllDirectories))TowerWatchSource(watched,DressingPackage+"/"+file.Substring(vendor.resolvedPath.TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar).Length+1).Replace('\\','/'),false);
            var setup=PrepareValidQuayProbeSceneSetup();foreach(string path in setup.Select(s=>s.path).Where(p=>!string.IsNullOrEmpty(p)))TowerWatchSource(watched,path,true);proof.watchedSources=watched.Values.ToArray();Scene scene=default;GameObject cameraObject=null,dressing=null;DragonKingdomWorld world=null;var frames=new List<TowerAppearanceFrame>();var parts=new List<VergeDressingPart>();var groups=new List<VergeDressingGroup>();var attempts=new List<VergeParcelAttempt>();bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            try
            {
                Directory.CreateDirectory(Io(folder+"/Materials/Finish"));AssetDatabase.Refresh();scene=EditorSceneManager.OpenScene(source.preview,OpenSceneMode.Additive);if(!EditorSceneManager.SaveScene(scene,proof.preview,true))throw new InvalidOperationException("Explicit owned scene copy failed.");EditorSceneManager.CloseScene(scene,true);scene=EditorSceneManager.OpenScene(proof.preview,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);world=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();Physics.SyncTransforms();proof.originalWorldBefore=DressingStableWorld(world);var originals=world.GetComponentsInChildren<Collider>(true);
                cameraObject=new GameObject("Temporary dressing comparison camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.nearClipPlane=.3f;camera.farClipPlane=6500;camera.fieldOfView=52;camera.aspect=16f/9;camera.clearFlags=CameraClearFlags.Skybox;
                foreach(var view in DressingViews()){yield return TowerSettle(camera,view.Item2,view.Item3);frames.Add(TowerCapture(camera,output,view.Item1+"-before",1920,1080,view.Item3,world.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Select(m=>m.shader).Distinct().ToArray()));}
                dressing=new GameObject("Bounded village harbor and terrace visual dressing");SceneManager.MoveGameObjectToScene(dressing,scene);foreach(var site in sites){review.stage="SUPPORTED_PARCEL_"+site.id;try{groups.Add(DressingBuildSupportedParcel(world,scene,dressing.transform,site,originals,parts,attempts));}finally{review.parcelAttempts=attempts.ToArray();}yield return null;}
                DressingConfigureMaterials(dressing.transform,folder);proof.parts=parts.ToArray();proof.groups=groups.ToArray();proof.instances=parts.Count;proof.noAddedPhysics=dressing.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled)&&dressing.GetComponentsInChildren<Rigidbody>(true).All(b=>b.isKinematic&&!b.detectCollisions);proof.noRuntimeGenerator=dressing.GetComponentsInChildren<SplineSpawner>(true).Length==0&&dressing.GetComponentsInChildren<SplineContainer>(true).Length==0;proof.originalWorldAfter=DressingStableWorld(world);proof.originalWorldExact=proof.originalWorldBefore==proof.originalWorldAfter;
                if(!proof.originalWorldExact||!proof.noAddedPhysics||!proof.noRuntimeGenerator||proof.instances>240)throw new InvalidOperationException("Dressing trial changed original world, added physical obstruction or retained a runtime generator.");
                if(PrefabUtility.SaveAsPrefabAssetAndConnect(dressing,proof.prefab,InteractionMode.AutomatedAction)==null)throw new InvalidOperationException("Baked owned dressing prefab did not save.");UnityEngine.Object.DestroyImmediate(cameraObject);cameraObject=null;if(!EditorSceneManager.SaveScene(scene,proof.preview))throw new InvalidOperationException("Owned dressed scene did not save.");EditorSceneManager.CloseScene(scene,true);scene=EditorSceneManager.OpenScene(proof.preview,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);world=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();dressing=scene.GetRootGameObjects().Single(r=>r.name=="Bounded village harbor and terrace visual dressing");proof.originalWorldReloaded=DressingStableWorld(world);proof.savedReloaded=proof.originalWorldReloaded==proof.originalWorldBefore;
                if(!proof.savedReloaded)throw new InvalidOperationException("Saved/reloaded dressing copy changed original world identity/physics/appearance.");cameraObject=new GameObject("Temporary saved dressing comparison camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.nearClipPlane=.3f;camera.farClipPlane=6500;camera.fieldOfView=52;camera.aspect=16f/9;camera.clearFlags=CameraClearFlags.Skybox;
                foreach(var view in DressingViews()){yield return TowerSettle(camera,view.Item2,view.Item3);frames.Add(TowerCapture(camera,output,view.Item1+"-saved-after",1920,1080,view.Item3,scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Renderer>(true)).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Select(m=>m.shader).Distinct().ToArray()));}
                proof.status="SAVED_VENDOR_DRESSING_CAPTURED_REQUIRES_LEAD_VISUAL_REVIEW_NOT_WORLD_ADOPTION";review.status="SUPPORTED_SAVED_VENDOR_DRESSING_CAPTURED_REQUIRES_LEAD_VISUAL_REVIEW_NOT_ADOPTION";
            }
            finally
            {
                if(cameraObject!=null)UnityEngine.Object.DestroyImmediate(cameraObject);if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);ShaderUtil.allowAsyncCompilation=async;
                proof.incomingSceneContextRestored=setup.Select(s=>s.path+"|"+s.isLoaded+"|"+s.isActive).SequenceEqual(EditorSceneManager.GetSceneManagerSetup().Select(s=>s.path+"|"+s.isLoaded+"|"+s.isActive));proof.changedSources=watched.Values.Where(p=>p.resourceKind!="UNITY_BUILTIN_VIRTUAL_RESOURCE_NO_DISK_FILE"&&(!File.Exists(p.resolvedPath)||TowerFileHash(p.resolvedPath)!=p.sha256)).Select(p=>p.path).ToArray();proof.originalSourcesExact=proof.changedSources.Length==0;proof.originalTerrainBytesExact=proof.originalSourcesExact;proof.parts=parts.ToArray();proof.groups=groups.ToArray();proof.frames=frames.ToArray();if(!proof.originalSourcesExact||!proof.incomingSceneContextRestored)proof.status="REJECTED_SOURCE_OR_CONTEXT_PRESERVATION";SaveSupportedVergeReview(review);if(!proof.originalSourcesExact||!proof.incomingSceneContextRestored)throw new InvalidOperationException("Original sources or incoming scene context changed during dressing trial.");
            }
        }
    }
}
#endif
