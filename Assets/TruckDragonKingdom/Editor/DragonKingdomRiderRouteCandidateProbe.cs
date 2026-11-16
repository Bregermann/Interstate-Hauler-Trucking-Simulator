#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using LWS.TruckDragonRiders;
using LWS.TruckDragonRiders.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    // Disposable authoring diagnosis only. The existing reviewer remains the
    // native sphere/turn oracle; nothing here saves or certifies a source scene.
    public static class DragonKingdomRiderRouteCandidateProbe
    {
        const string Canonical="Assets/TruckDragonRiders/Scenes/TruckDragonRiders.unity";
        const float MaximumLift=300,MaximumAltitude=800,PlanningMargin=2;
        [Serializable]public sealed class Iteration{public int index,terrainVertices,meshTriangles;public float[] lifts;public string[] limitingObstacles;}
        [Serializable]public sealed class Evidence
        {
            public string scope="DISPOSABLE_LOCAL_CORRIDOR_POSE_PLAN_USING_EXISTING_NATIVE_ORACLE_NOT_SCENE_MIGRATION_FLIGHT_OR_ADOPTION",status="IN_PROGRESS",failure,candidate;
            public string actualRejectedRoute,actualRejectedRouteSha256,canonicalSceneSha256,worldPrefabSha256,worldPreviewSha256,certificate;
            public bool sourcesExact,allTwelvePhysicalRingsRetained,formationSeparated,equalFirstGateDistance;public int originalContactRecords,originalContactQueries,originalBlockerContactTotal,candidateBlockerContactTotal;
            public bool originalRecordsTruncated,candidateRecordsTruncated;public float enclosingStartRadius,firstGateDistance,maximumAllowedLift=MaximumLift,maximumAllowedAltitude=MaximumAltitude;
            public Pose[] proposedMotorStarts;public DragonRiderGateDefinition[] originalLayout,proposedLayout;public Iteration[] planning;public DragonKingdomMountedCourseReview.Receipt measured;
        }
        public static Evidence LastEvidence{get;private set;}
        static MethodInfo Existing(string name)=>typeof(DragonKingdomMountedCourseReview).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic)??throw new InvalidOperationException("Original course oracle missing: "+name);
        static T Invoke<T>(string name,params object[] args){try{return (T)Existing(name).Invoke(null,args);}catch(TargetInvocationException e){throw e.InnerException??e;}}
        static void Invoke(string name,params object[] args){try{Existing(name).Invoke(null,args);}catch(TargetInvocationException e){throw e.InnerException??e;}}
        static string Arg(string key){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,key);if(i<0||i+1>=a.Length||a[i+1].StartsWith("-",StringComparison.Ordinal)||a.Count(s=>s==key)!=1)throw new ArgumentException("One explicit value required: "+key);return a[i+1];}
        static string Full(string p)=>Path.GetFullPath(Path.IsPathRooted(p)?p:Path.Combine(Directory.GetParent(Application.dataPath).FullName,p));
        static string Io(string p){p=Full(p);return Application.platform==RuntimePlatform.WindowsEditor&&p.Length>=248&&!p.StartsWith(@"\\?\",StringComparison.Ordinal)?@"\\?\"+p:p;}
        static string Hash(string p){using(var h=SHA256.Create())return string.Concat(h.ComputeHash(File.ReadAllBytes(Io(p))).Select(b=>b.ToString("x2")));}
        static bool Same(string a,string b)=>string.Equals(a?.Replace("-",""),b?.Replace("-",""),StringComparison.OrdinalIgnoreCase);
        static bool Finite(float v)=>float.IsFinite(v);

        // Liang-Barsky in the horizontal plane. Padding is the unchanged native
        // sphere plus its <=.5m path-sampling coverage, not a smaller footprint.
        public static bool CorridorInterval(Vector3 a,Vector3 b,Bounds bounds,float radius,out float lo,out float hi)
        {
            if(!Finite(radius)||radius<0)throw new ArgumentException("Finite footprint required.");lo=0;hi=1;
            foreach(int axis in new[]{0,2}){float d=b[axis]-a[axis],min=bounds.min[axis]-radius,max=bounds.max[axis]+radius;if(Mathf.Abs(d)<1e-8f){if(a[axis]<min||a[axis]>max)return false;continue;}float u=(min-a[axis])/d,v=(max-a[axis])/d;if(u>v){float t=u;u=v;v=t;}lo=Mathf.Max(lo,u);hi=Mathf.Min(hi,v);if(lo>hi)return false;}return true;
        }
        public static float LocalTerrainCeiling(Terrain terrain,Vector3 a,Vector3 b,float radius,out int visited)
        {
            visited=0;if(terrain==null||terrain.terrainData==null||terrain.transform.rotation!=Quaternion.identity||terrain.transform.lossyScale!=Vector3.one)throw new InvalidOperationException("Native unrotated, unscaled terrain data required for exact local height-grid bounds.");
            var data=terrain.terrainData;int n=data.heightmapResolution;Vector3 p=terrain.transform.position,s=data.size;float dx=s.x/(n-1),dz=s.z/(n-1),pad=radius+Mathf.Sqrt(dx*dx+dz*dz);
            int x0=Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.x,b.x)-pad-p.x)/dx),0,n-1),x1=Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(a.x,b.x)+pad-p.x)/dx),0,n-1),z0=Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.z,b.z)-pad-p.z)/dz),0,n-1),z1=Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(a.z,b.z)+pad-p.z)/dz),0,n-1);
            var heights=data.GetHeights(x0,z0,x1-x0+1,z1-z0+1);float ceiling=float.NegativeInfinity;
            for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++)
            {
                var point=new Vector3(p.x+x*dx,0,p.z+z*dz);var flatA=new Vector3(a.x,0,a.z);var flatDelta=new Vector3(b.x-a.x,0,b.z-a.z);float t=flatDelta.sqrMagnitude==0?0:Mathf.Clamp01(Vector3.Dot(point-flatA,flatDelta)/flatDelta.sqrMagnitude);
                if((point-flatA-flatDelta*t).sqrMagnitude>pad*pad)continue;visited++;ceiling=Mathf.Max(ceiling,p.y+heights[z-z0,x-x0]*s.y);
            }
            // Terrain bilinear interpolation is bounded by the cell vertices.
            // The extra full grid diagonal includes all vertices of every cell
            // intersecting the native footprint, including terrain tile seams.
            return ceiling;
        }
        static float MeshCeiling(MeshCollider collider,Vector3 a,Vector3 b,float radius,ref int triangles)
        {
            var mesh=collider.sharedMesh;if(mesh==null)return collider.bounds.max.y;var vertices=mesh.vertices;var indices=mesh.triangles;var matrix=collider.transform.localToWorldMatrix;float top=float.NegativeInfinity;
            for(int i=0;i<indices.Length;i+=3){var v0=matrix.MultiplyPoint3x4(vertices[indices[i]]);var v1=matrix.MultiplyPoint3x4(vertices[indices[i+1]]);var v2=matrix.MultiplyPoint3x4(vertices[indices[i+2]]);var box=new Bounds(v0,Vector3.zero);box.Encapsulate(v1);box.Encapsulate(v2);if(!CorridorInterval(a,b,box,radius,out _,out _))continue;triangles++;top=Mathf.Max(top,box.max.y);}return top;
        }
        static Quaternion Heading(DragonRiderGateDefinition[] gates,int i)
        {var p=gates[i].center;var tangent=(p-gates[(i+11)%12].center).normalized+(gates[(i+1)%12].center-p).normalized;if(tangent.sqrMagnitude<.01f)throw new InvalidOperationException("Ordered route cannot reverse through a physical ring.");return Quaternion.LookRotation(tangent.normalized,Vector3.up);}
        static Pose[] Formation(DragonRiderGateDefinition first,float radius)
        {float arc=(2*radius+4)/(2*Mathf.Sin(40f/3*Mathf.Deg2Rad))+.1f;return DragonRiderStartFormationAuthoring.ProposeArc(first.center,first.rotation*Vector3.forward,arc,0);}
        static void Segments(DragonRiderGateDefinition[] gates,Pose[] starts,Vector3[] offsets,Action<int,int,Vector3,Vector3> visit)
        {
            var first=gates[0];var firstBack=first.center-first.rotation*Vector3.forward*90;for(int i=0;i<4;i++)visit(0,0,starts[i].position+starts[i].rotation*offsets[i],firstBack);
            for(int i=0;i<12;i++){var g=gates[i];var next=gates[(i+1)%12];var n=g.rotation*Vector3.forward;var back=g.center-n*90;var after=g.center+n*55;visit(i,i,back,g.center);visit(i,i,g.center,after);visit(i,(i+1)%12,after,next.center-next.rotation*Vector3.forward*90);}
        }
        static Iteration LiftLocalCorridors(DragonRiderGateDefinition[] gates,Pose[] starts,Vector3[] offsets,Collider[] world,float sphere,int index)
        {
            var result=new Iteration{index=index,lifts=new float[12],limitingObstacles=new string[12]};int terrainVertices=0,meshTriangles=0;
            Segments(gates,starts,offsets,(i,j,a,b)=>
            {
                foreach(var collider in world)
                {
                    float padded=sphere+.5f;if(!CorridorInterval(a,b,collider.bounds,padded,out float lo,out float hi))continue;float top;
                    if(collider is TerrainCollider tc){var terrain=tc.GetComponent<Terrain>();if(terrain==null||terrain.terrainData!=tc.terrainData)throw new InvalidOperationException("Actual terrain collider/data mismatch.");top=LocalTerrainCeiling(terrain,a,b,padded,out int count);terrainVertices+=count;}
                    else if(collider is MeshCollider mc&&Mathf.Max(collider.bounds.size.x,collider.bounds.size.z)>padded*4)top=MeshCeiling(mc,a,b,padded,ref meshTriangles);
                    else top=collider.bounds.max.y;
                    if(float.IsNegativeInfinity(top))continue;float bottom=Mathf.Min(Mathf.Lerp(a.y,b.y,lo),Mathf.Lerp(a.y,b.y,hi));float lift=Mathf.Max(0,top+padded+PlanningMargin-bottom);
                    foreach(int gate in new[]{i,j})if(lift>result.lifts[gate]){result.lifts[gate]=lift;result.limitingObstacles[gate]=collider.name;}
                }
            });result.terrainVertices=terrainVertices;result.meshTriangles=meshTriangles;return result;
        }
        static DragonKingdomMountedCourseReview.Receipt Retrace(DragonKingdomMountedCourseReview.Receipt original,DragonRiderGateDefinition[] gates,Pose[] starts,Vector3[] offsets,HashSet<Collider> blockers)
        {
            var r=new DragonKingdomMountedCourseReview.Receipt{candidate=original.candidate,fixedQualifiedCourse=true,qualifiedSweepRadius=original.qualifiedSweepRadius,straightExitDistance=55,combinedEnvelope=original.combinedEnvelope,courseLayout=gates,rigs=original.rigs,gateCount=12,physicalGateColliders=12};var contacts=new List<DragonKingdomMountedCourseReview.Contact>();
            var first=gates[0];for(int i=0;i<4;i++)Invoke("Probe","crew-"+i+"-start",starts[i].position+starts[i].rotation*offsets[i],first.center-first.rotation*Vector3.forward*90,r.combinedEnvelope,blockers,r,contacts);
            for(int i=0;i<12;i++)
            {
                var gate=gates[i];var next=gates[(i+1)%12];var previous=gates[(i+11)%12];var back=gate.center-gate.rotation*Vector3.forward*90;var after=gate.center+gate.rotation*Vector3.forward*55;var previousAfter=previous.center+previous.rotation*Vector3.forward*55;var nextBack=next.center-next.rotation*Vector3.forward*90;
                Invoke("ProbeTurn","gate-"+i+"-back-turn",back,back-previousAfter,gate.center-back,r.combinedEnvelope,blockers,r,contacts);Invoke("Probe","gate-"+i+"-back-to-center",back,gate.center,r.combinedEnvelope,blockers,r,contacts);Invoke("Probe","gate-"+i+"-center-to-forward",gate.center,after,r.combinedEnvelope,blockers,r,contacts);Invoke("Probe","gate-"+i+"-to-next-back",after,nextBack,r.combinedEnvelope,blockers,r,contacts);Invoke("ProbeTurn","gate-"+i+"-forward-turn",after,after-gate.center,nextBack-after,r.combinedEnvelope,blockers,r,contacts);
            }
            r.contacts=contacts.ToArray();r.blockers=r.blockerCounts.Select(p=>new DragonKingdomMountedCourseReview.Blocker{collider=p.Key,contactQueries=p.Value}).OrderByDescending(p=>p.contactQueries).ToArray();r.status=r.contactQueries==0?"DISPOSABLE_ROUTE_NATIVE_ORACLE_CLEAR_NOT_SOURCE_CERTIFICATE_OR_FLIGHT":"DISPOSABLE_ROUTE_NATIVE_CONTACTS_REMAIN";return r;
        }
        public static void MeasureActualQualifiedRouteCandidate()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled native Editor required.");
            string rejected=Arg("-tsRiderRejectedRouteEvidence"),tag=Arg("-tsDragonCourseSource"),output=Full(Arg("-tsDragonOutput")),production=Arg("-tsDragonMountedProductionEvidence"),folder="Assets/TruckDragonKingdom/VisualCandidates/"+tag,preview=folder+"/DragonKingdomPreview.unity";
            if(tag.Length>40||tag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new ArgumentException("Bounded existing world tag required.");
            var old=JsonUtility.FromJson<DragonKingdomMountedCourseReview.Receipt>(File.ReadAllText(Io(rejected)));if(old==null||old.status!="REQUIRES_EXACT_GEOMETRY_OR_COURSE_REVIEW_NOT_ACCEPTED"||!old.sourceBytesPreserved||!old.fixedQualifiedCourse||old.candidate!=tag||old.gateCount!=12||old.physicalGateColliders!=12||old.apertureFailures!=0||old.straightExitDistance!=55||old.contactQueries<=0||old.rigs?.Length!=8||old.courseLayout?.Length!=12||old.courseLayout.Any(g=>g.clearRadius!=60)||old.qualifiedSweepRadius!=old.rigs.Max(r=>r.maximumGateRadialExtent)+.25f||!Same(old.ridersSceneSha256,Hash(Canonical))||!Same(old.worldPreviewSha256,Hash(preview))||!Same(old.worldPrefabSha256,Hash(folder+"/Shared Dragon Kingdom.prefab")))throw new InvalidOperationException("Exact actual rejected route/current canonical/world and unchanged full native radius required.");
            string worldTests=Arg("-tsDragonPassedWorldTests");try{typeof(DragonKingdomPhysicalCourseWorldBridge).GetMethod("Passed",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{worldTests,"DragonKingdomCurrentFinalTerrainRecipeTests.ActualCurrentWorldRunsExplicitRecipeThenUnchangedOriginalRoofAndFullWorldGates"});}catch(TargetInvocationException e){throw e.InnerException??e;}
            string integration=folder+"/Integration Receipt.json",nativeWorld=Path.Combine(Path.GetDirectoryName(Full(worldTests)),"rendered-evidence","regional-world-"+tag+".json");if(!Same(Hash(integration),Hash(nativeWorld)))throw new InvalidOperationException("Saved world must equal its actual passed complete native world receipt.");
            if(old.qualifiedSource?.currentSourceSnapshot==null||old.qualifiedSource.currentSourceSnapshot.Length==0||old.qualifiedSource.currentSourceSnapshot.Any(row=>!File.Exists(Io(row.path))||!Same(Hash(row.path),row.sha256)))throw new InvalidOperationException("Every actual retained source-snapshot row must remain present and byte-exact; no skipped dependency.");
            for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(s.isDirty||string.IsNullOrEmpty(s.path)||s.path==Canonical||s.path==preview)throw new InvalidOperationException("Preserve edited/user source scenes; use the explicit owned clean test fixture.");}
            var files=AssetDatabase.GetDependencies(new[]{Canonical,preview},true).Concat(old.qualifiedSource.currentSourceSnapshot.Select(s=>s.path)).Concat(new[]{rejected,Canonical,preview,integration,worldTests,nativeWorld}).SelectMany(p=>new[]{p,p+".meta"}).Where(p=>File.Exists(Io(p))).Distinct(StringComparer.OrdinalIgnoreCase).ToDictionary(p=>p,p=>Hash(p),StringComparer.OrdinalIgnoreCase);var setup=EditorSceneManager.GetSceneManagerSetup();Scene source=default,candidate=default;
            var proof=new Evidence{candidate=tag,actualRejectedRoute=Full(rejected),actualRejectedRouteSha256=Hash(rejected),canonicalSceneSha256=Hash(Canonical),worldPrefabSha256=Hash(folder+"/Shared Dragon Kingdom.prefab"),worldPreviewSha256=Hash(preview),originalLayout=old.courseLayout,originalContactQueries=old.contactQueries,originalContactRecords=old.contacts.Length,originalBlockerContactTotal=old.blockers.Sum(b=>b.contactQueries)};proof.originalRecordsTruncated=proof.originalContactRecords<proof.originalBlockerContactTotal;LastEvidence=proof;
            try
            {
                using(DragonRiderSaddleAuthoring.OpenCertifiedPhysicalCourseReviewScope())
                {
                    if(DragonRiderSaddleAuthoring.QualifiedMountedCourseScenePath(production)!=Canonical)throw new InvalidOperationException("Actual explicitly certified canonical source selection required.");source=EditorSceneManager.OpenScene(Canonical,OpenSceneMode.Additive);var qualified=DragonRiderSaddleAuthoring.ValidateQualifiedMountedCourseSource(ref source,production,Arg("-tsRiderProductionCourseEvidence"));proof.certificate=qualified.canonicalSourceCertificate;
                    if(!qualified.currentSourceBytesExact||qualified.rigs.Count!=8||!qualified.currentProfilesExact||!qualified.currentNwhExact||!qualified.currentConfigurationExact||qualified.rigs.Any(r=>r.recordedSourcePoses!=(r.species=="dragon-fire"?537:699))||qualified.rigs.Any(r=>old.rigs.Single(o=>o.crew==r.crew&&o.species==r.species).maximumGateRadialExtent!=r.anyAttitudeRadius))throw new InvalidOperationException("Current actual eight source rigs/envelopes differ from retained failure.");
                    foreach(var rig in qualified.rigs){var prior=old.rigs.Single(r=>r.crew==rig.crew&&r.species==rig.species);if(!prior.envelope.Equals(rig.headingBounds)||!prior.neutralSeatOffset.Equals(rig.neutralCrossingOffset)||prior.skinnedVertices!=rig.skinPoints||prior.recordedSourcePoses!=rig.recordedSourcePoses||prior.controller!=rig.controller||!Same(prior.controllerSha256,rig.controllerSha256)||!prior.clips.SequenceEqual(rig.clips)||!prior.clipSha256.Select(h=>h.Replace("-","").ToLowerInvariant()).SequenceEqual(rig.clipSha256.Select(h=>h.Replace("-","").ToLowerInvariant())))throw new InvalidOperationException("Actual per-crew envelope/source/controller identity changed; do not reuse the old failure geometry.");}
                    var match=source.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DragonRidersMatch>(true)).Single();if(DragonKingdomMountedCourseReview.GateHash(match.gates)!=old.originalCourseHash)throw new InvalidOperationException("Saved ordered route changed after actual rejection.");foreach(var root in source.GetRootGameObjects())root.SetActive(false);var course=source.GetRootGameObjects().Single(r=>r.name=="Ordered kingdom aerial gate loop");course.SetActive(true);var rings=course.GetComponentsInChildren<MeshCollider>(true).Where(c=>c.enabled&&!c.isTrigger).OrderBy(c=>int.Parse(c.name.Substring("Aerial gate ".Length))).ToArray();if(rings.Length!=12)throw new InvalidOperationException("Retain every actual physical gate.");
                    candidate=EditorSceneManager.OpenScene(preview,OpenSceneMode.Additive);SceneManager.SetActiveScene(candidate);var world=candidate.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();world.ValidateReferences();Physics.SyncTransforms();var colliders=world.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger).ToArray();if(colliders.Length<100)throw new InvalidOperationException("Actual nonempty passed world collision geometry required.");
                    var proposed=Invoke<DragonRiderGateDefinition[]>("CoordinatedGeometry",(object)match.gates);var initial=proposed.Select(g=>g.center).ToArray();var offsets=Enumerable.Range(0,4).Select(i=>old.rigs.First(r=>r.crew==i).neutralSeatOffset).ToArray();proof.enclosingStartRadius=old.rigs.Max(r=>r.maximumGateRadialExtent+r.neutralSeatOffset.magnitude)+.5f;var history=new List<Iteration>();
                    for(int iteration=0;iteration<3;iteration++)
                    {
                        for(int i=0;i<12;i++)proposed[i].rotation=Heading(proposed,i);var starts=Formation(proposed[0],proof.enclosingStartRadius);var step=LiftLocalCorridors(proposed,starts,offsets,colliders,old.qualifiedSweepRadius,iteration);history.Add(step);proof.planning=history.ToArray();proof.proposedLayout=proposed;if(step.lifts.Max()<.001f)break;
                        for(int i=0;i<12;i++){var p=proposed[i].center;p.y+=step.lifts[i];if(p.y-initial[i].y>MaximumLift||p.y>MaximumAltitude)throw new InvalidOperationException("Local route exceeds bounded300m lift/800m absolute altitude; retain rejected diagnosis instead of lifting the entire kingdom.");proposed[i].center=p;}
                    }
                    for(int i=0;i<12;i++)proposed[i].rotation=Heading(proposed,i);proof.planning=history.ToArray();proof.proposedLayout=proposed;proof.proposedMotorStarts=Formation(proposed[0],proof.enclosingStartRadius);proof.formationSeparated=DragonRiderStartFormationAuthoring.Separate(proof.proposedMotorStarts,proof.enclosingStartRadius,4);proof.firstGateDistance=Vector3.Distance(proof.proposedMotorStarts[0].position,proposed[0].center);proof.equalFirstGateDistance=proof.proposedMotorStarts.All(p=>Mathf.Abs(Vector3.Distance(p.position,proposed[0].center)-proof.firstGateDistance)<.001f);if(!proof.formationSeparated||!proof.equalFirstGateDistance)throw new InvalidOperationException("Actual four-crew equal-distance separated authoring required.");
                    for(int i=0;i<12;i++)rings[i].transform.SetPositionAndRotation(proposed[i].center,proposed[i].rotation);Physics.SyncTransforms();proof.allTwelvePhysicalRingsRetained=rings.All(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.sharedMesh!=null);if(!proof.allTwelvePhysicalRingsRetained)throw new InvalidOperationException("No physical gate can be disabled or replaced.");proof.measured=Retrace(old,proposed,proof.proposedMotorStarts,offsets,new HashSet<Collider>(colliders.Concat(rings)));proof.candidateBlockerContactTotal=proof.measured.blockers.Sum(b=>b.contactQueries);proof.candidateRecordsTruncated=proof.measured.contacts.Length<proof.candidateBlockerContactTotal;proof.status=proof.measured.contactQueries==0?"DISPOSABLE_LOCAL_ROUTE_CLEAR_REQUIRES_EXPLICIT_POSE_TRANSITION_AND_REAL_FLIGHT":"DISPOSABLE_LOCAL_ROUTE_REJECTED_NATIVE_CONTACTS_REMAIN";
                }
            }
            catch(Exception e){proof.failure=e.ToString();proof.status="DISPOSABLE_LOCAL_ROUTE_REJECTED";throw;}
            finally
            {
                if(candidate.IsValid()&&candidate.isLoaded)EditorSceneManager.CloseScene(candidate,true);if(source.IsValid()&&source.isLoaded)EditorSceneManager.CloseScene(source,true);EditorSceneManager.RestoreSceneManagerSetup(setup);proof.sourcesExact=files.All(p=>Same(p.Value,Hash(p.Key)));Directory.CreateDirectory(output);File.WriteAllText(Io(Path.Combine(output,"rider-local-route-candidate.json")),JsonUtility.ToJson(proof,true));if(!proof.sourcesExact)throw new InvalidOperationException("Read-only route diagnosis changed source bytes.");
            }
        }
    }
}
#endif
