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
        [Serializable]public sealed class ReadableCanopyIsland
        {public string site;public int trees,understory;public string[] sourceSpecies;public Vector3[] measuredCanopyDimensions;public float minimumHeight,maximumHeight;}
        [Serializable]public sealed class ReadableCanopyPlacement
        {public string site,source,reason,conflict;public int trial;public Vector3 vendorPosition;public Bounds measuredBounds;public bool canopy,nativeBoundsMeasured,accepted;}
        [Serializable]public sealed class ReadableCanopyReview
        {
            public string scope="V524_BASELINE_SIX_REAL_CANOPY_COMPOSITIONS_NOT_TERRAIN_CHANGE_OR_FINISHED_WORLD_ADOPTION",status="IN_PROGRESS",stage,error,cleanupError,baselineReceipt,baselineReceiptSha256,baselinePreview,baselinePreviewSha256,baselinePrefabSha256;
            public VergeDressingReview dressing;public ReadableCanopyIsland[] islands;public VergeParcelAttempt[] parcelAttempts;public ReadableCanopyPlacement[] placementAttempts;public bool earlierTerraceAndJunctionPartsPreserved;public int totalCanopies;
        }
        [Serializable]public sealed class CanopyVendorFixture{public int actualProjectedPoints;public bool startupNone,noRetainedGenerator;public float minimumFittedHeight,maximumFittedHeight,maximumProjectionError;}
        public static ReadableCanopyReview LastReadableCanopyReview{get;private set;}
        static bool ReadableCanopySource(string source)=>BoundedVergeDressingSources().Take(3).Contains(source);
        public static ReadableCanopyIsland MeasureReadableCanopyIsland(string site,VergeDressingPart[] parts)
        {
            var local=parts.Where(p=>p.site==site).ToArray();var trees=local.Where(p=>ReadableCanopySource(p.source)).ToArray();var proof=new ReadableCanopyIsland{site=site,trees=trees.Length,understory=local.Length-trees.Length,sourceSpecies=trees.Select(p=>p.source+" GUID:"+p.guid).Distinct().ToArray(),measuredCanopyDimensions=trees.Select(p=>p.bounds.size).ToArray(),minimumHeight=trees.Length==0?0:trees.Min(p=>p.bounds.size.y),maximumHeight=trees.Length==0?0:trees.Max(p=>p.bounds.size.y)};
            if(proof.trees<3||proof.trees>5||proof.understory<12||proof.understory>20||proof.minimumHeight<22||proof.maximumHeight>30||trees.Any(p=>p.bounds.size.x<5||p.bounds.size.z<5))throw new InvalidOperationException("Readable canopy island requires3–5 actual22–30m tree crowns and12–20 understory plants; shrub-only total-count success is rejected: "+JsonUtility.ToJson(proof));return proof;
        }
        static List<sc.splines.spawner.runtime.SpawnPoint> ReadableCanopyVendorPoints(Scene scene,Vector3[] knots,int seed,VergeDressingGroup group)
        {
            // Reuse the existing sampler; select all authored grove knots
            // through the vendor's public pre-distribution hook, with no new
            // sampling/projection implementation or runtime generator.
            SplineSpawner.Action selectAll=(spawner,index)=>{if(spawner.name=="Temporary actual Spline Spawner "+group.id)spawner.distributionSettings.onKnots.selection=DistributionSettings.OnKnots.Selection.All;};SplineSpawner.onPreRespawn+=selectAll;
            try{return DressingVendorPoints(scene,knots,false,BoundedVergeDressingSources().Take(3).Select(DressingSource).ToArray(),new[]{26f,26f,26f},18,seed,true,group);}finally{SplineSpawner.onPreRespawn-=selectAll;}
        }
        static Vector3[] ReadableCanopyKnots(Vector3 center,Vector2 radius)
        {
            var layout=new[]{new Vector2(-.32f,-.24f),new Vector2(.32f,-.24f),new Vector2(0,.32f),new Vector2(-.42f,.25f),new Vector2(.42f,.25f),Vector2.zero,new Vector2(-.15f,-.5f),new Vector2(.15f,-.5f),new Vector2(-.58f,0),new Vector2(.58f,0),new Vector2(0,.58f),new Vector2(0,-.58f)};return layout.Select(p=>center+new Vector3(p.x*radius.x,2,p.y*radius.y)).ToArray();
        }
        public static string ReadableCanopyBoundsRejection(Bounds region,Bounds art,Bounds[] earlier)
        {
            if(!DressingPartInsideRegion(region,art))return "ACTUAL_PRIMARY_ART_OUTSIDE_ORIGINAL_REGION";
            if(art.min.y<=-1.75f)return "ACTUAL_PRIMARY_ART_UNDER_WATER";
            if(earlier.Any(p=>RidgeInterior(art).Intersects(RidgeInterior(p))))return "ACCEPTED_EARLIER_COMPOSITION_VISUAL_OVERLAP";
            return null;
        }
        static bool AcceptReadableCanopyBakedPoint(Transform parent,Bounds region,VergeDressingPart[] earlier,List<VergeDressingPart> parts,int first,List<string> rejects,ReadableCanopyPlacement trace)
        {
            var part=parts[first];trace.nativeBoundsMeasured=true;trace.measuredBounds=part.bounds;trace.reason=ReadableCanopyBoundsRejection(region,part.bounds,earlier.Select(p=>p.bounds).ToArray());
            if(trace.reason!=null){if(trace.reason=="ACCEPTED_EARLIER_COMPOSITION_VISUAL_OVERLAP")trace.conflict=earlier.First(p=>RidgeInterior(part.bounds).Intersects(RidgeInterior(p.bounds))).name;rejects.Add(trace.reason);var actual=parent.Cast<Transform>().Single(t=>t.name==part.name);UnityEngine.Object.DestroyImmediate(actual.gameObject);parts.RemoveAt(first);return false;}
            trace.accepted=true;return true;
        }
        [Serializable]public sealed class CanopyQuotaFixture{public int vendorPoints,attempted,accepted,rejected;public bool firstRejected,lateValidCompletedQuota,rejectedObjectsRemoved,earlierUntouched,startupNone,noRetainedGenerator;}
        public static CanopyQuotaFixture InspectActualCanopyEarlyInvalidLateValidQuotaFixture()
        {
            var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;var proof=new CanopyQuotaFixture();
            try
            {
                scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);var ground=new GameObject("Temporary actual canopy quota ground");SceneManager.MoveGameObjectToScene(ground,scene);ground.transform.position=new Vector3(9000,9.5f,9000);var collider=ground.AddComponent<BoxCollider>();collider.size=new Vector3(140,1,140);string originalGround=EditorJsonUtility.ToJson(collider);Physics.SyncTransforms();var parent=new GameObject("Temporary owned canopy quota instances");SceneManager.MoveGameObjectToScene(parent,scene);var group=new VergeDressingGroup{id="early-invalid-later-valid-quota"};var paths=BoundedVergeDressingSources();var points=ReadableCanopyVendorPoints(scene,ReadableCanopyKnots(new Vector3(9000,20,9000),new Vector2(42,48)),881,group);proof.vendorPoints=points.Count;proof.startupNone=group.startupNone;proof.noRetainedGenerator=group.noRetainedGenerator;var parts=new List<VergeDressingPart>();var rejects=new List<string>();VergeDressingPart[] earlier=null;var rejectedNames=new List<string>();
                foreach(var point in points)
                {
                    if(parts.Count>=3)break;int index=proof.attempted++,first=parts.Count;string path=paths[point.prefabIndex];var instance=(GameObject)PrefabUtility.InstantiatePrefab(DressingSource(path),parent.transform);instance.name="Actual vendor quota tree "+index;instance.transform.SetPositionAndRotation(point.position,point.rotation);instance.transform.localScale=point.scale;RidgeMeshSamples(instance.transform,out var bounds,out _,out _);instance.transform.position+=Vector3.up*(10-bounds.min.y-.035f);RidgeMeshSamples(instance.transform,out bounds,out _,out _);parts.Add(new VergeDressingPart{name=instance.name,source=path,bounds=bounds});if(earlier==null)earlier=new[]{new VergeDressingPart{name="Retained footprint exact first vendor canopy",bounds=bounds}};var trace=new ReadableCanopyPlacement();bool accepted=AcceptReadableCanopyBakedPoint(parent.transform,new Bounds(new Vector3(9000,30,9000),new Vector3(140,100,140)),earlier,parts,first,rejects,trace);if(!accepted){proof.rejected++;rejectedNames.Add("Actual vendor quota tree "+index);}if(index==0)proof.firstRejected=!accepted&&trace.reason=="ACCEPTED_EARLIER_COMPOSITION_VISUAL_OVERLAP";
                }
                proof.accepted=parts.Count;proof.lateValidCompletedQuota=proof.firstRejected&&proof.accepted==3&&proof.attempted>3;proof.rejectedObjectsRemoved=rejectedNames.All(name=>parent.transform.Cast<Transform>().All(t=>t.name!=name))&&parent.transform.childCount==parts.Count;proof.earlierUntouched=collider!=null&&EditorJsonUtility.ToJson(collider)==originalGround;return proof;
            }
            finally{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        static bool BakeReadableCanopyEligiblePoint(DragonKingdomWorld world,Transform parent,sc.splines.spawner.runtime.SpawnPoint point,string source,string site,int trial,Bounds region,VergeDressingPart[] earlier,Collider[] originals,List<VergeDressingPart> parts,List<string> rejects,List<ReadableCanopyPlacement> placement)
        {
            var trace=new ReadableCanopyPlacement{site=site,trial=trial,source=source,canopy=ReadableCanopySource(source),vendorPosition=point.position};placement.Add(trace);int first=parts.Count,rejected=rejects.Count;
            if(!DressingBakePoint(world,parent,point,source,site,originals,parts,rejects)){trace.reason=string.Join(";",rejects.Skip(rejected));return false;}
            return AcceptReadableCanopyBakedPoint(parent,region,earlier,parts,first,rejects,trace);
        }
        static VergeDressingGroup BuildReadableCanopySite(DragonKingdomWorld world,Scene scene,Transform parent,VergeDressingSite site,int trial,Bounds region,VergeDressingPart[] earlier,Collider[] originals,List<VergeDressingPart> parts,List<ReadableCanopyPlacement> placement)
        {
            var group=new VergeDressingGroup{id=site.id,kind="ISLAND"};var rejects=new List<string>();int first=parts.Count;var child=new GameObject(site.id);child.transform.SetParent(parent,false);Vector3 center=site.center;center.y=DressingTerrainY(world,center);var paths=BoundedVergeDressingSources();
            foreach(var point in ReadableCanopyVendorPoints(scene,ReadableCanopyKnots(center,site.radius),site.seed,group))
            {
                if(parts.Skip(first).Count(p=>ReadableCanopySource(p.source))>=5)break;BakeReadableCanopyEligiblePoint(world,child.transform,point,paths[point.prefabIndex],site.id,trial,region,earlier,originals,parts,rejects,placement);
            }
            var understory=paths.Skip(3).Take(6).ToArray();var heights=understory.Select(p=>p.Contains("grass")?1.8f:p.Contains("flower")?2.2f:4.5f).ToArray();var outline=Enumerable.Range(0,8).Select(i=>{float angle=i*Mathf.PI*.25f,r=i%3==0?.82f:1;return center+new Vector3(Mathf.Cos(angle)*site.radius.x*r,2,Mathf.Sin(angle)*site.radius.y*r);}).ToArray();int small=0;
            foreach(var point in DressingVendorPoints(scene,outline,true,understory.Select(DressingSource).ToArray(),heights,6,site.seed+100,false,group))
            {if(small>=20)break;if(BakeReadableCanopyEligiblePoint(world,child.transform,point,understory[point.prefabIndex],site.id,trial,region,earlier,originals,parts,rejects,placement))small++;}
            group.accepted=parts.Count-first;group.rejected=rejects.Count;group.rejectionReasons=rejects.Distinct().ToArray();MeasureReadableCanopyIsland(site.id,parts.Skip(first).ToArray());return group;
        }
        static VergeDressingGroup BuildReadableCanopyParcel(DragonKingdomWorld world,Scene scene,Transform parent,VergeDressingSite original,Collider[] originals,List<VergeDressingPart> parts,List<VergeParcelAttempt> attempts,List<ReadableCanopyPlacement> placement)
        {
            var offsets=BoundedDressingParcelOffsets();ValidateDressingParcelOffsets(offsets);Bounds region=world.definition.FindRegion(original.region).bounds;
            foreach(var indexed in offsets.Select((offset,index)=>(offset,index)))
            {
                int first=parts.Count;var oldChildren=new HashSet<Transform>(parent.Cast<Transform>());var site=new VergeDressingSite{id=original.id,kind="ISLAND",region=original.region,center=original.center+indexed.offset,radius=original.radius,seed=original.seed,minimum=15,maximum=25};var attempt=new VergeParcelAttempt{site=site.id,trial=indexed.index,center=site.center};attempts.Add(attempt);Vector3 center=site.center;
                if(center.x-site.radius.x<region.min.x||center.x+site.radius.x>region.max.x||center.z-site.radius.y<region.min.z||center.z+site.radius.y>region.max.z){attempt.reason="WHOLE_PROPOSED_PARCEL_OUTSIDE_ACTUAL_ORIGINAL_REGION";continue;}bool accepted=false;
                try
                {
                    var group=BuildReadableCanopySite(world,scene,parent,site,indexed.index,region,parts.Take(first).Where(p=>p.site!=site.id).ToArray(),originals,parts,placement);attempt.accepted=parts.Count-first;attempt.originalRegionContained=parts.Skip(first).All(p=>DressingPartInsideRegion(region,p.bounds)&&p.bounds.min.y> -1.75f);if(!attempt.originalRegionContained){attempt.reason="ACTUAL_PRIMARY_ART_EXTENDS_OUTSIDE_REGION_OR_UNDER_WATER";continue;}
                    if(parts.Take(first).Where(p=>p.site!=site.id).Any(prior=>parts.Skip(first).Any(current=>RidgeInterior(current.bounds).Intersects(RidgeInterior(prior.bounds))))){attempt.reason="ACCEPTED_EARLIER_COMPOSITION_VISUAL_OVERLAP";continue;}attempt.passed=accepted=true;return group;
                }
                catch(InvalidOperationException error){attempt.accepted=parts.Count-first;attempt.reason=error.Message;if(!error.Message.StartsWith("Readable canopy island requires",StringComparison.Ordinal))throw;}
                finally{if(!accepted){foreach(var child in parent.Cast<Transform>().Where(c=>!oldChildren.Contains(c)).ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);parts.RemoveRange(first,parts.Count-first);}}
            }
            throw new InvalidOperationException("All13 same-region canopy parcels exhausted for "+original.id+". Preserve original guards and require3real canopy trees plus12understory plants; never fall back to shrub-only success.");
        }
        public static CanopyVendorFixture InspectActualCanopyOnKnotsFixture()
        {
            var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;var result=new CanopyVendorFixture{minimumFittedHeight=float.PositiveInfinity};
            try
            {
                scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);var ground=new GameObject("Temporary real canopy OnKnots fixture");SceneManager.MoveGameObjectToScene(ground,scene);ground.transform.position=new Vector3(9000,9.5f,9000);ground.AddComponent<BoxCollider>().size=new Vector3(120,1,120);Physics.SyncTransforms();var group=new VergeDressingGroup{id="real-canopy-fixture"};var paths=BoundedVergeDressingSources();var points=ReadableCanopyVendorPoints(scene,ReadableCanopyKnots(new Vector3(9000,20,9000),new Vector2(42,42)),881,group);result.actualProjectedPoints=points.Count;result.startupNone=group.startupNone;result.noRetainedGenerator=group.noRetainedGenerator;
                foreach(var point in points){RidgeMeshSamples(DressingSource(paths[point.prefabIndex]).transform,out var bounds,out _,out _);float height=bounds.size.y*point.scale.y;result.minimumFittedHeight=Mathf.Min(result.minimumFittedHeight,height);result.maximumFittedHeight=Mathf.Max(result.maximumFittedHeight,height);result.maximumProjectionError=Mathf.Max(result.maximumProjectionError,Mathf.Abs(point.position.y-10));}return result;
            }
            finally{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        [Serializable]sealed class CanopyRetainedPartState{public Vector3 position,scale;public Quaternion rotation;public RidgeGeometryBinding[] geometry;}
        static Dictionary<string,string> ReadableRetainedSignatures(Transform root,IEnumerable<VergeDressingPart> parts)
        {
            var values=new Dictionary<string,string>();foreach(var part in parts){var actual=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==part.name);values.Add(part.name,JsonUtility.ToJson(new CanopyRetainedPartState{position=actual.position,rotation=actual.rotation,scale=actual.lossyScale,geometry=RidgeGeometryBindings(actual,new Dictionary<string,string>())}));}return values;
        }
        static VergeDressingPart[] ReadableSavedParts(Transform root,IEnumerable<VergeDressingPart> parts)
        {
            var actualParts=new List<VergeDressingPart>();foreach(var part in parts){var actual=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==part.name);RidgeMeshSamples(actual,out var bounds,out _,out _);if(Vector3.Distance(part.position,actual.position)>.0001f||Quaternion.Angle(part.rotation,actual.rotation)>.001f||Vector3.Distance(part.bounds.center,bounds.center)>.001f||Vector3.Distance(part.bounds.size,bounds.size)>.001f)throw new InvalidOperationException("Saved authored dressing instance geometry/pose differs: "+part.name);actualParts.Add(new VergeDressingPart{site=part.site,source=part.source,guid=part.guid,name=part.name,position=actual.position,rotation=actual.rotation,scale=actual.localScale,bounds=bounds,nativeGrounded=part.nativeGrounded,allOriginalExclusionsClear=part.allOriginalExclusionsClear,physicsDisabled=actual.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled)});}return actualParts.ToArray();
        }
        static void SaveReadableCanopyReview(ReadableCanopyReview review)
        {string output=Path.Combine(Output(),"ReadableCanopyDressing");Directory.CreateDirectory(Io(output));File.WriteAllText(Io(Path.Combine(output,"readable-canopy-dressing-review.json")),JsonUtility.ToJson(review,true));}
        public static IEnumerator CaptureReadableCanopyDressingReview()
        {
            var review=LastReadableCanopyReview=new ReadableCanopyReview();var stack=new Stack<IEnumerator>();stack.Push(ReadableCanopyRoutine(review));
            try{while(stack.Count>0){bool moved;object current=null;try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;else(stack.Pop() as IDisposable)?.Dispose();}catch(Exception error){while(stack.Count>0){try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception cleanup){review.cleanupError=cleanup.ToString();}}review.error=error.ToString();review.status="REJECTED_ACTUAL_CANOPY_OR_ORIGINAL_PRESERVATION";SaveReadableCanopyReview(review);throw;}if(moved){if(current is IEnumerator nested)stack.Push(nested);else yield return current;}}}finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();}
        }
        static IEnumerator ReadableCanopyRoutine(ReadableCanopyReview review)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");var sites=BoundedVergeDressingSites();foreach(var site in sites.Where(s=>s.kind=="ISLAND")){site.minimum=15;site.maximum=25;}ValidateBoundedVergeDressingPlan(sites);
            string tag=TowerAppearanceArg("-tsDragonWorldCandidate",""),owned=TowerAppearanceArg("-tsDragonDressingCandidate","");if(string.IsNullOrEmpty(tag)||string.IsNullOrEmpty(owned)||tag==owned||owned.Length>50||new[]{tag,owned}.Any(t=>t.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_')))throw new InvalidOperationException("Explicit passed parent and unique bounded dressing tag required.");
            string sourceFolder=Root+"VisualCandidates/"+tag,receipt=sourceFolder+"/Integration Receipt.json";var source=JsonUtility.FromJson<TowerAppearanceParent>(File.ReadAllText(Io(receipt)));if(source.tag!=tag||!source.originalPhysicalGatesPassed||!source.sourceBytesPreserved||source.status!="PHYSICAL_GATES_PASSED_REQUIRES_RENDER_AND_ACTUAL_DRIVING_REVIEW")throw new InvalidOperationException("Actual physically passed saved parent required.");
            review.baselineReceipt=TowerAppearanceArg("-tsDragonDressingBaseline","");if(string.IsNullOrEmpty(review.baselineReceipt)||!File.Exists(Io(review.baselineReceipt)))throw new InvalidOperationException("Explicit actual saved/reloaded V524 baseline receipt required.");var baseline=JsonUtility.FromJson<SupportedVergeReview>(File.ReadAllText(Io(review.baselineReceipt)));if(baseline.status!="SUPPORTED_SAVED_VENDOR_DRESSING_CAPTURED_REQUIRES_LEAD_VISUAL_REVIEW_NOT_ADOPTION"||!baseline.dressing.savedReloaded||!baseline.dressing.originalSourcesExact||!baseline.dressing.originalWorldExact||baseline.dressing.groups.Length!=12||baseline.dressing.sourceCandidate!=tag||baseline.dressing.instances!=baseline.dressing.parts.Length)throw new InvalidOperationException("Complete preserved experimental dressing baseline required; no style acceptance is inferred.");source.preview=baseline.dressing.preview;review.baselineReceiptSha256=TowerFileHash(review.baselineReceipt);review.baselinePreview=source.preview;review.baselinePreviewSha256=TowerFileHash(source.preview);review.baselinePrefabSha256=TowerFileHash(baseline.dressing.prefab);
            string folder=Root+"VisualCandidates/"+owned,output=Path.Combine(Output(),"ReadableCanopyDressing");if(Directory.Exists(Io(folder))||File.Exists(Io(folder+".meta"))||Directory.Exists(Io(output)))throw new InvalidOperationException("Preserve previous trials; fresh owned/evidence folders required.");Directory.CreateDirectory(Io(output));
            var proof=review.dressing=new VergeDressingReview{sourceCandidate=tag,folder=folder,preview=folder+"/Bounded dressing preview.unity",prefab=folder+"/Baked visual dressing.prefab",sites=sites,islands=6,bands=2,junctions=4};var watched=new Dictionary<string,TowerAppearanceSource>(StringComparer.Ordinal);
            foreach(string path in AssetDatabase.GetDependencies(BoundedVergeDressingSources().Concat(new[]{source.prefab,source.preview,receipt}).ToArray(),true).Concat(BoundedVergeDressingSources()).Concat(new[]{source.prefab,source.preview,receipt}).Distinct())TowerWatchSource(watched,path,true);
            foreach(string path in Directory.GetFiles(Io(source.cavern.dataFolder),"*",SearchOption.AllDirectories))TowerWatchSource(watched,path,false);TowerWatchPipelineShaderSources(watched);
            TowerWatchSource(watched,review.baselineReceipt,false);TowerWatchSource(watched,baseline.dressing.prefab,true);
            var vendor=UnityEditor.PackageManager.PackageInfo.FindForAssetPath(DressingPackage);if(vendor==null||vendor.assetPath!=DressingPackage)throw new InvalidOperationException("Actual registered Spline Spawner required.");foreach(string file in Directory.GetFiles(vendor.resolvedPath,"*",SearchOption.AllDirectories))TowerWatchSource(watched,DressingPackage+"/"+file.Substring(vendor.resolvedPath.TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar).Length+1).Replace('\\','/'),false);
            var setup=PrepareValidQuayProbeSceneSetup();foreach(string path in setup.Select(s=>s.path).Where(p=>!string.IsNullOrEmpty(p)))TowerWatchSource(watched,path,true);proof.watchedSources=watched.Values.ToArray();Scene scene=default;GameObject cameraObject=null,dressing=null;DragonKingdomWorld world=null;var frames=new List<TowerAppearanceFrame>();var parts=new List<VergeDressingPart>();var groups=new List<VergeDressingGroup>();var attempts=new List<VergeParcelAttempt>();var placement=new List<ReadableCanopyPlacement>();Dictionary<string,string> retainedSignatures=null;bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            try
            {
                Directory.CreateDirectory(Io(folder+"/Materials/Finish"));AssetDatabase.Refresh();scene=EditorSceneManager.OpenScene(source.preview,OpenSceneMode.Additive);if(!EditorSceneManager.SaveScene(scene,proof.preview,true))throw new InvalidOperationException("Explicit owned scene copy failed.");EditorSceneManager.CloseScene(scene,true);scene=EditorSceneManager.OpenScene(proof.preview,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);world=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();Physics.SyncTransforms();proof.originalWorldBefore=DressingStableWorld(world);if(proof.originalWorldBefore!=baseline.dressing.originalWorldBefore)throw new InvalidOperationException("Actual baseline world differs from the saved V524 physical/presentation identity.");var originals=world.GetComponentsInChildren<Collider>(true);
                cameraObject=new GameObject("Temporary dressing comparison camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.nearClipPlane=.3f;camera.farClipPlane=6500;camera.fieldOfView=52;camera.aspect=16f/9;camera.clearFlags=CameraClearFlags.Skybox;
                foreach(var view in DressingViews()){yield return TowerSettle(camera,view.Item2,view.Item3);frames.Add(TowerCapture(camera,output,view.Item1+"-before",1920,1080,view.Item3,world.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Select(m=>m.shader).Distinct().ToArray()));}
                var earlier=scene.GetRootGameObjects().Single(r=>r.name=="Bounded village harbor and terrace visual dressing");var retainedParts=baseline.dressing.parts.Where(p=>!sites.Where(t=>t.kind=="ISLAND").Any(t=>t.id==p.site)).ToArray();retainedSignatures=ReadableRetainedSignatures(earlier.transform,retainedParts);if(PrefabUtility.IsPartOfPrefabInstance(earlier))PrefabUtility.UnpackPrefabInstance(earlier,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);dressing=new GameObject("Bounded village harbor and terrace visual dressing");SceneManager.MoveGameObjectToScene(dressing,scene);var islandIds=new HashSet<string>(sites.Where(s=>s.kind=="ISLAND").Select(s=>s.id));foreach(var child in earlier.transform.Cast<Transform>().Where(c=>!islandIds.Contains(c.name)).ToArray())child.SetParent(dressing.transform,true);parts.AddRange(baseline.dressing.parts.Where(p=>!islandIds.Contains(p.site)));groups.AddRange(baseline.dressing.groups.Where(g=>g.kind!="ISLAND"));UnityEngine.Object.DestroyImmediate(earlier);
                foreach(var site in sites.Where(s=>s.kind=="ISLAND")){review.stage="REAL_CANOPY_"+site.id;try{groups.Add(BuildReadableCanopyParcel(world,scene,dressing.transform,site,originals,parts,attempts,placement));}finally{review.parcelAttempts=attempts.ToArray();review.placementAttempts=placement.ToArray();}yield return null;}review.islands=sites.Where(s=>s.kind=="ISLAND").Select(s=>MeasureReadableCanopyIsland(s.id,parts.ToArray())).ToArray();review.totalCanopies=review.islands.Sum(s=>s.trees);var retainedAfter=ReadableRetainedSignatures(dressing.transform,retainedParts);review.earlierTerraceAndJunctionPartsPreserved=retainedSignatures.Count==retainedAfter.Count&&retainedSignatures.All(p=>retainedAfter.TryGetValue(p.Key,out var value)&&value==p.Value);if(!review.earlierTerraceAndJunctionPartsPreserved)throw new InvalidOperationException("Earlier successful terrace/junction instance manifests changed.");

                DressingConfigureMaterials(dressing.transform,folder);proof.parts=parts.ToArray();proof.groups=groups.ToArray();proof.instances=parts.Count;proof.noAddedPhysics=dressing.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled)&&dressing.GetComponentsInChildren<Rigidbody>(true).All(b=>b.isKinematic&&!b.detectCollisions);proof.noRuntimeGenerator=dressing.GetComponentsInChildren<SplineSpawner>(true).Length==0&&dressing.GetComponentsInChildren<SplineContainer>(true).Length==0;proof.originalWorldAfter=DressingStableWorld(world);proof.originalWorldExact=proof.originalWorldBefore==proof.originalWorldAfter;
                if(!proof.originalWorldExact||!proof.noAddedPhysics||!proof.noRuntimeGenerator||proof.instances>240)throw new InvalidOperationException("Dressing trial changed original world, added physical obstruction or retained a runtime generator.");
                if(PrefabUtility.SaveAsPrefabAssetAndConnect(dressing,proof.prefab,InteractionMode.AutomatedAction)==null)throw new InvalidOperationException("Baked owned dressing prefab did not save.");UnityEngine.Object.DestroyImmediate(cameraObject);cameraObject=null;if(!EditorSceneManager.SaveScene(scene,proof.preview))throw new InvalidOperationException("Owned dressed scene did not save.");EditorSceneManager.CloseScene(scene,true);scene=EditorSceneManager.OpenScene(proof.preview,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);world=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();dressing=scene.GetRootGameObjects().Single(r=>r.name=="Bounded village harbor and terrace visual dressing");proof.originalWorldReloaded=DressingStableWorld(world);proof.savedReloaded=proof.originalWorldReloaded==proof.originalWorldBefore;var savedRetained=ReadableRetainedSignatures(dressing.transform,retainedParts);review.earlierTerraceAndJunctionPartsPreserved&=retainedSignatures.Count==savedRetained.Count&&retainedSignatures.All(p=>savedRetained.TryGetValue(p.Key,out var value)&&value==p.Value);var savedParts=ReadableSavedParts(dressing.transform,parts);review.islands=sites.Where(t=>t.kind=="ISLAND").Select(t=>MeasureReadableCanopyIsland(t.id,savedParts)).ToArray();review.totalCanopies=review.islands.Sum(t=>t.trees);if(!review.earlierTerraceAndJunctionPartsPreserved)throw new InvalidOperationException("Actual saved/reloaded original terrace/junction geometry or pose changed.");
                if(!proof.savedReloaded)throw new InvalidOperationException("Saved/reloaded dressing copy changed original world identity/physics/appearance.");cameraObject=new GameObject("Temporary saved dressing comparison camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.nearClipPlane=.3f;camera.farClipPlane=6500;camera.fieldOfView=52;camera.aspect=16f/9;camera.clearFlags=CameraClearFlags.Skybox;
                foreach(var view in DressingViews()){yield return TowerSettle(camera,view.Item2,view.Item3);frames.Add(TowerCapture(camera,output,view.Item1+"-saved-after",1920,1080,view.Item3,scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Renderer>(true)).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Select(m=>m.shader).Distinct().ToArray()));}
                proof.status="SAVED_VENDOR_DRESSING_CAPTURED_REQUIRES_LEAD_VISUAL_REVIEW_NOT_WORLD_ADOPTION";review.status="SAVED_REAL_CANOPIES_CAPTURED_REQUIRES_DIRECT_VISUAL_REVIEW_NOT_ADOPTION";
            }
            finally
            {
                if(cameraObject!=null)UnityEngine.Object.DestroyImmediate(cameraObject);if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);ShaderUtil.allowAsyncCompilation=async;
                proof.incomingSceneContextRestored=setup.Select(s=>s.path+"|"+s.isLoaded+"|"+s.isActive).SequenceEqual(EditorSceneManager.GetSceneManagerSetup().Select(s=>s.path+"|"+s.isLoaded+"|"+s.isActive));proof.changedSources=watched.Values.Where(p=>p.resourceKind!="UNITY_BUILTIN_VIRTUAL_RESOURCE_NO_DISK_FILE"&&(!File.Exists(p.resolvedPath)||TowerFileHash(p.resolvedPath)!=p.sha256)).Select(p=>p.path).ToArray();proof.originalSourcesExact=proof.changedSources.Length==0;proof.originalTerrainBytesExact=proof.originalSourcesExact;proof.parts=parts.ToArray();proof.groups=groups.ToArray();proof.frames=frames.ToArray();if(!proof.originalSourcesExact||!proof.incomingSceneContextRestored)proof.status="REJECTED_SOURCE_OR_CONTEXT_PRESERVATION";SaveReadableCanopyReview(review);if(!proof.originalSourcesExact||!proof.incomingSceneContextRestored)throw new InvalidOperationException("Original sources or incoming scene context changed during dressing trial.");
            }
        }
    }
}
#endif
