#if UNITY_EDITOR && VISTA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LWS.TruckDragonRiders;
using LWS.TruckDragonRiders.Editor;
using LWS.TruckSports;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    // Read-only conservative clearance over the actual saved, calibrated rigs.
    // Positive box contacts require narrower geometry review; they are not a
    // claim that every point in the box is occupied by an animal. A zero-contact
    // result is a bounded authoring check, never actual flight/race acceptance.
    public static class DragonKingdomMountedCourseReview
    {
        [Serializable]public sealed class Rig
        {
            public string poseAuthority;public int baseFlightPoses,recordedSourcePoses,recordedMixerLabelledPoses;public int crew,poses,skinnedVertices,originalTruckColliders,originalTruckRenderers,configuredGraphPoses,nativeMixerPoses;
            public string species,controller,controllerSha256;public Bounds envelope;public float maximumGateRadialExtent;public Vector3 neutralSeatOffset;public string[] clips,clipSha256;
        }
        [Serializable]public sealed class Contact {public string kind,path,collider;public Vector3 position;public Bounds blocker;public float requiredRadius,configuredRadius;}
        [Serializable]public sealed class Blocker{public string collider;public int contactQueries;}
        [Serializable]public sealed class Receipt
        {
            public string scope="READ_ONLY_SAMPLED_ACTUAL_ANIMATED_SKIN_AND_FULL_ORIGINAL_TRUCK_CONSERVATIVE_COURSE_ENVELOPE_NOT_ACTUAL_FLIGHT",candidate,status="IN_PROGRESS",error;
            public string collisionEnvelopeAuthority;public bool fixedQualifiedCourse;public float qualifiedSweepRadius,straightExitDistance;public string ridersScene,qualifiedProductionEvidence;public DragonRiderSaddleAuthoring.QualifiedCourseSource qualifiedSource;public bool coordinatedCourse;public string originalCourseHash,ridersSceneSha256,worldPrefabSha256,worldPreviewSha256;public DragonRiderGateDefinition[] courseLayout;
            public bool sourceBytesPreserved;public int gateCount,physicalGateColliders,queries,turnQueries,contactQueries,apertureFailures;public Bounds combinedEnvelope;
            public Rig[] rigs;public Contact[] contacts;public Blocker[] blockers;[NonSerialized]public Dictionary<string,int> blockerCounts=new Dictionary<string,int>();
            public string[] limitations={"Seventeen flight phases plus actual configured base/action/mixer trials and five bank/pitch poses are sampled, not every continuous animation state","Claw-carry and detached rescue require phase-aware carrier geometry; this is the seated-mount course","Bounds enclose empty space; a positive overlap needs exact geometry diagnosis","Connectors follow existing gate back/forward targets; actual steering and full race remain separate"};
        }
        public static Receipt LastReceipt{get;private set;}
        public static void ReviewCandidate()=>Review(false);
        public static void ReviewCoordinatedCourseCandidate()=>Review(true);
        public static string GateHash(DragonRiderGateDefinition[] values){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(new GateValues{values=values}))));}
        [Serializable]sealed class GateValues{public DragonRiderGateDefinition[] values;}
        static void Review(bool coordinateCourse)
        {
            string tag=Arg("-tsDragonCourseSource",Arg("-tsDragonWorldCandidate","RoundedIntegrated04"));
            if(string.IsNullOrEmpty(tag)||tag.Length>40||tag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_'))throw new ArgumentException("Explicit candidate tag required.");
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");
            string production=Arg("-tsDragonMountedProductionEvidence","");string ridersScene=string.IsNullOrEmpty(production)?DragonRidersAuthoring.ScenePath:DragonRiderSaddleAuthoring.QualifiedMountedCourseScenePath(production);
            string folder="Assets/TruckDragonKingdom/VisualCandidates/"+tag,preview=folder+"/DragonKingdomPreview.unity";
            foreach(string p in new[]{preview,folder+"/Integration Receipt.json",ridersScene})if(!File.Exists(Io(p)))throw new InvalidOperationException("Actual saved candidate and calibrated Riders scene required: "+p);
            var accepted=JsonUtility.FromJson<IntegrationStatus>(File.ReadAllText(folder+"/Integration Receipt.json"));
            if(accepted.status!="PHYSICAL_GATES_PASSED_REQUIRES_RENDER_AND_ACTUAL_DRIVING_REVIEW"||!accepted.sourceBytesPreserved||!string.IsNullOrEmpty(production)&&(!accepted.originalPhysicalGatesPassed||!accepted.candidateRoadAuthorityAndCriticalPosesExact))throw new InvalidOperationException("Run the complete actual rounded-world gates before reviewing its flight course.");
            for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(s.isDirty||string.IsNullOrEmpty(s.path)||s.path==preview||s.path==ridersScene)throw new InvalidOperationException("Save and close actual review source/user scenes first.");}
            var files=new[]{preview,folder+"/Shared Dragon Kingdom.prefab",folder+"/Dragon Kingdom.asset",ridersScene,"ProjectSettings/GraphicsSettings.asset","ProjectSettings/QualitySettings.asset"}
                .Concat(AssetDatabase.GetDependencies(new[]{preview,folder+"/Shared Dragon Kingdom.prefab"},true)).Concat(Directory.GetFiles(folder+"/Terrain","*",SearchOption.AllDirectories)).SelectMany(p=>new[]{p,p+".meta"}).Where(p=>File.Exists(Io(p))).Distinct().ToDictionary(p=>p,p=>Hash(p));
            var setup=EditorSceneManager.GetSceneManagerSetup();Scene source=default,candidate=default;GameObject ghost=null;
            var receipt=new Receipt{candidate=tag,ridersScene=ridersScene,qualifiedProductionEvidence=production,coordinatedCourse=coordinateCourse,ridersSceneSha256=Hash(ridersScene).Replace("-","").ToLowerInvariant(),worldPrefabSha256=Hash(folder+"/Shared Dragon Kingdom.prefab").Replace("-","").ToLowerInvariant(),worldPreviewSha256=Hash(preview).Replace("-","").ToLowerInvariant()};LastReceipt=receipt;var contacts=new List<Contact>();
            try
            {
                source=EditorSceneManager.OpenScene(ridersScene,OpenSceneMode.Additive);
                var match=source.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DragonRidersMatch>(true)).Single();
                if(match.racers.Length!=4||match.gates.Length<8)throw new InvalidOperationException("The actual saved four-crew ordered course is required.");
                receipt.originalCourseHash=GateHash(match.gates);
                if(!string.IsNullOrEmpty(production))
                {
                    receipt.qualifiedSource=DragonRiderSaddleAuthoring.ValidateQualifiedMountedCourseSource(ref source,production,Arg("-tsRiderProductionCourseEvidence",""));if(!source.IsValid()||!source.isLoaded||source.path!=ridersScene)throw new InvalidOperationException("Qualification did not restore its exact owned source scene.");match=source.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<DragonRidersMatch>(true)).Single();receipt.fixedQualifiedCourse=true;receipt.scope="READ_ONLY_NATIVE_WORLD_COLLISION_QUERIES_USING_CURRENT_PRODUCTION_EQUIVALENCE_AND_RECORDED_FULL_NATIVE_SOURCE_ENVELOPES_NOT_NEW_POSE_REPLAY_FLIGHT_OR_ADOPTION";receipt.limitations=new[]{"Reuses exact paired native699/537 whole-assembly box-centered geometry after current source/profile equivalence; not a new pose replay","A full-native any-attitude sphere encloses empty space; positive native overlaps remain honest rejection requiring exact geometry diagnosis","All12 saved physical gate poses and qualified55m exit retained; actual four-player flight/race remains separate"};
                    foreach(var item in receipt.qualifiedSource.currentSourceSnapshot)if(!files.ContainsKey(item.path))files.Add(item.path,Hash(item.path));
                }
                receipt.straightExitDistance=receipt.fixedQualifiedCourse?match.aiStraightGateExitDistance:30;
                if(coordinateCourse&&!receipt.fixedQualifiedCourse)match.gates=CoordinatedGeometry(match.gates);
                receipt.courseLayout=match.gates.Select(g=>new DragonRiderGateDefinition{center=g.center,rotation=g.rotation,clearRadius=g.clearRadius}).ToArray();
                Physics.SyncTransforms();var truckGeometry=match.racers.ToDictionary(r=>r,r=>TruckGeometry(r.truck));
                foreach(var root in source.GetRootGameObjects())root.SetActive(false);
                var physicalCourse=source.GetRootGameObjects().Single(r=>r.name=="Ordered kingdom aerial gate loop");physicalCourse.SetActive(true);
                var physicalGates=physicalCourse.GetComponentsInChildren<MeshCollider>(true).Where(c=>c.enabled&&c.sharedMesh!=null&&!c.isTrigger).ToArray();
                receipt.physicalGateColliders=physicalGates.Length;if(physicalGates.Length!=match.gates.Length)throw new InvalidOperationException("Every actual ordered physical ring mesh must remain in the mounted clearance query.");
                if(coordinateCourse&&!receipt.fixedQualifiedCourse)for(int i=0;i<match.gates.Length;i++)physicalGates.Single(c=>c.name=="Aerial gate "+(i+1)).transform.SetPositionAndRotation(match.gates[i].center,match.gates[i].rotation);
                candidate=EditorSceneManager.OpenScene(preview,OpenSceneMode.Additive);SceneManager.SetActiveScene(candidate);
                var world=candidate.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DragonKingdomWorld>(true)).Single();world.ValidateReferences();
                var rigs=new List<Rig>();
                if(receipt.qualifiedSource!=null)
                {
                    foreach(var item in receipt.qualifiedSource.rigs)
                    {
                        var truck=truckGeometry[match.racers.Single(r=>r.slot==item.crew)];rigs.Add(new Rig{crew=item.crew,species=item.species,poseAuthority="PAIRED_FULL_NATIVE_GEOMETRY_CURRENT_PROFILE_EQUIVALENCE_NOT_NEW_POSE_REPLAY",recordedSourcePoses=item.recordedSourcePoses,recordedMixerLabelledPoses=item.recordedMixerLabelledPoses,skinnedVertices=item.skinPoints,originalTruckColliders=truck.colliders,originalTruckRenderers=truck.renderers,envelope=item.headingBounds,maximumGateRadialExtent=item.anyAttitudeRadius,neutralSeatOffset=item.neutralCrossingOffset,controller=item.controller,controllerSha256=item.controllerSha256.Replace("-","").ToLowerInvariant(),clips=item.clips,clipSha256=item.clipSha256.Select(h=>h.Replace("-","").ToLowerInvariant()).ToArray()});
                    }
                }
                else foreach(var racer in match.racers)
                {
                    if(racer.choices.Length!=racer.choiceIds.Length||racer.choices.Length<2)throw new InvalidOperationException("Actual configured mount alternatives required for every original crew.");
                    for(int choice=0;choice<racer.choices.Length;choice++)
                    {
                        if(!match.catalog.TryGet(racer.choiceIds[choice],out var species))throw new InvalidOperationException("Saved mount species missing from actual catalog.");
                        ghost=UnityEngine.Object.Instantiate(racer.choices[choice].gameObject);ghost.name="Temporary actual animated mount measurement";SceneManager.MoveGameObjectToScene(ghost,candidate);ghost.SetActive(true);
                        var actor=ghost.GetComponent<DragonCharacter>();var saddle=ghost.GetComponent<DragonRiderSaddle>();if(saddle==null)throw new InvalidOperationException("Run native-proven saddle migration before course measurement.");saddle.Validate(actor);
                        foreach(var body in ghost.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.detectCollisions=false;}
                        actor.motor.animator.enabled=false;
                        var rig=Measure(racer,species,actor,saddle,truckGeometry[racer]);rigs.Add(rig);
                        UnityEngine.Object.DestroyImmediate(ghost);ghost=null;
                    }
                }
                receipt.rigs=rigs.ToArray();if(rigs.Count<8)throw new InvalidOperationException("All eight actual saved mount choices must be measured.");
                var envelope=rigs[0].envelope;foreach(var rig in rigs.Skip(1))envelope.Encapsulate(rig.envelope);envelope.Expand(.5f);receipt.combinedEnvelope=envelope;receipt.gateCount=match.gates.Length;
                float required=rigs.Max(r=>r.maximumGateRadialExtent)+.25f;receipt.qualifiedSweepRadius=receipt.fixedQualifiedCourse?required:0;receipt.collisionEnvelopeAuthority=receipt.fixedQualifiedCourse?"NATIVE_OVERLAP_SPHERE_FULL_NATIVE_ANY_ATTITUDE_POINT_RADIUS_CURRENT_TRUCK_BOX_CENTER_PLUS_MARGIN_AND_HALF_SPACING_ALL_COLLIDERS":"NATIVE_OVERLAP_BOX_SAMPLED_ANIMATED_BOUND";
                for(int i=0;i<match.gates.Length;i++)if(required>match.gates[i].clearRadius)
                {receipt.apertureFailures++;contacts.Add(new Contact{kind="Measured actual skin/truck radial extent exceeds saved opening",path="gate-"+i,position=match.gates[i].center,requiredRadius=required,configuredRadius=match.gates[i].clearRadius});}
                var blockers=new HashSet<Collider>(world.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger).Concat(physicalGates));Physics.SyncTransforms();
                foreach(var racer in match.racers)
                {
                    var first=match.gates[0];var start=world.riderStarts[racer.slot];var rig=rigs.First(r=>r.crew==racer.slot);
                    Probe("crew-"+racer.slot+"-start",start.position+start.rotation*rig.neutralSeatOffset,first.center-first.rotation*Vector3.forward*Mathf.Max(60,first.clearRadius*1.5f),envelope,blockers,receipt,contacts);
                }
                for(int i=0;i<match.gates.Length;i++)
                {
                    var gate=match.gates[i];var next=match.gates[(i+1)%match.gates.Length];var n=gate.rotation*Vector3.forward;
                    var previous=match.gates[(i+match.gates.Length-1)%match.gates.Length];var previousAfter=previous.center+previous.rotation*Vector3.forward*receipt.straightExitDistance;
                    Vector3 before=gate.center-n*Mathf.Max(60,gate.clearRadius*1.5f),after=gate.center+n*receipt.straightExitDistance;
                    ProbeTurn("gate-"+i+"-back-turn",before,before-previousAfter,gate.center-before,envelope,blockers,receipt,contacts);
                    Probe("gate-"+i+"-back-to-center",before,gate.center,envelope,blockers,receipt,contacts);
                    Probe("gate-"+i+"-center-to-forward",gate.center,after,envelope,blockers,receipt,contacts);
                    Probe("gate-"+i+"-to-next-back",after,next.center-next.rotation*Vector3.forward*Mathf.Max(60,next.clearRadius*1.5f),envelope,blockers,receipt,contacts);
                    ProbeTurn("gate-"+i+"-forward-turn",after,after-gate.center,next.center-next.rotation*Vector3.forward*Mathf.Max(60,next.clearRadius*1.5f)-after,envelope,blockers,receipt,contacts);
                }
                if(receipt.fixedQualifiedCourse&&GateHash(match.gates)!=receipt.originalCourseHash)throw new InvalidOperationException("Read-only qualified review changed actual saved ordered physical course poses.");
                receipt.status=receipt.contactQueries==0&&receipt.apertureFailures==0?"SAMPLED_FULL_MOUNT_ENVELOPE_CLEAR_REQUIRES_ACTUAL_FLIGHT":"REQUIRES_EXACT_GEOMETRY_OR_COURSE_REVIEW_NOT_ACCEPTED";
            }
            catch(Exception e){receipt.status="REJECTED_MOUNTED_COURSE_REVIEW";receipt.error=e.ToString();throw;}
            finally
            {
                if(ghost!=null)UnityEngine.Object.DestroyImmediate(ghost);
                if(candidate.IsValid()&&candidate.isLoaded)EditorSceneManager.CloseScene(candidate,true);if(source.IsValid()&&source.isLoaded)EditorSceneManager.CloseScene(source,true);EditorSceneManager.RestoreSceneManagerSetup(setup);
                receipt.contacts=contacts.ToArray();receipt.blockers=receipt.blockerCounts.Select(p=>new Blocker{collider=p.Key,contactQueries=p.Value}).OrderByDescending(p=>p.contactQueries).ToArray();receipt.sourceBytesPreserved=files.All(p=>File.Exists(Io(p.Key))&&Hash(p.Key)==p.Value);
                if(!receipt.sourceBytesPreserved)receipt.status="REJECTED_MOUNTED_COURSE_SOURCE_PRESERVATION";
                string output=Arg("-tsDragonOutput","Logs/TruckSports/DragonKingdom");Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"mounted-course-"+tag+(coordinateCourse?"-coordinated":"")+".json"),JsonUtility.ToJson(receipt,true));
                if(!receipt.sourceBytesPreserved)throw new InvalidOperationException("Read-only mounted course review changed actual saved source bytes.");
            }
        }
        [Serializable]sealed class IntegrationStatus{public string status="";public bool sourceBytesPreserved=false,originalPhysicalGatesPassed,candidateRoadAuthorityAndCriticalPosesExact;}
        static DragonRiderGateDefinition[] CoordinatedGeometry(DragonRiderGateDefinition[] original)
        {
            if(original.Length!=12)throw new InvalidOperationException("The measured twelve-gate Skyfire course is required; no lap/rule/count changes.");
            var centers=original.Select(g=>g.center).ToArray();
            // V154's original low forest links crossed real220m roads/viaducts
            // and tree crowns. Its first ring also cut through the four-crew
            // staging fan. Place genuine gates above those complete bounds.
            centers[0]=new Vector3(0,260,540);centers[1]=new Vector3(-550,270,430);centers[2]=new Vector3(-1050,300,580);
            var result=new DragonRiderGateDefinition[centers.Length];
            for(int i=0;i<result.Length;i++)
            {
                Vector3 incoming=(centers[i]-centers[(i+centers.Length-1)%centers.Length]).normalized,outgoing=(centers[(i+1)%centers.Length]-centers[i]).normalized;
                Vector3 tangent=incoming+outgoing;if(tangent.sqrMagnitude<.01f)throw new InvalidOperationException("A genuine course turn cannot reverse through its own ring.");
                // The original incoming-only normals sent subsequent targets
                // BEHIND passed rings, recrossing their actual rims. A tangent
                // through the real corner keeps approach and exit on their
                // correct physical sides; runtime passage/AI rules stay intact.
                result[i]=new DragonRiderGateDefinition{center=centers[i],rotation=Quaternion.LookRotation(tangent.normalized,Vector3.up),clearRadius=original[i].clearRadius};
            }
            return result;
        }
        sealed class Truck{public SportsEventTruck original;public Vector3[] points;public float support;public int colliders,renderers;}
        static Truck TruckGeometry(SportsEventTruck truck)
        {
            if(truck==null||truck.body==null||truck.shape==null||truck.wheels.Length==0)throw new InvalidOperationException("Full original configured NWH assembly required.");
            var inverse=truck.body.transform.worldToLocalMatrix;var points=new List<Vector3>();
            var colliders=truck.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&!c.isTrigger).ToArray();var renderers=truck.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();
            foreach(var bounds in colliders.Select(c=>c.bounds).Concat(renderers.Select(r=>r.bounds)))foreach(var p in Corners(bounds))points.Add(inverse.MultiplyPoint3x4(p));
            if(points.Count==0||colliders.Length==0||renderers.Length==0)throw new InvalidOperationException("No original complete rendered/colliding NWH truck to audit.");
            float lower=truck.shape.center.y-truck.shape.size.y*.5f;
            foreach(var wheel in truck.wheels)if(wheel!=null)lower=Mathf.Min(lower,truck.transform.InverseTransformPoint(wheel.transform.position).y-wheel.wheel.radius-wheel.spring.maxLength);
            return new Truck{original=truck,points=points.ToArray(),support=-lower+.05f,colliders=colliders.Length,renderers=renderers.Length};
        }
        static Rig Measure(DragonRiderRacer racer,DragonSpeciesDefinition species,DragonCharacter actor,DragonRiderSaddle saddle,Truck truck)
        {
            var model=actor.motor.transform;var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(s=>s.sharedMesh!=null).ToArray();if(skins.Length==0)throw new InvalidOperationException("Actual skinned dragon geometry required.");
            var basePoses=model.GetComponentsInChildren<Transform>(true).Select(t=>Tuple.Create(t,t.localPosition,t.localRotation,t.localScale)).ToArray();
            var beforeTruckPosition=truck.original.transform.localPosition;var beforeTruckRotation=truck.original.transform.localRotation;
            var rig=new Rig{poseAuthority="CURRENT_SOURCE_SAMPLED_BASE170_PLUS_CONFIGURED_GRAPH_PHASES_CURRENT_AUTHORED_BOX_CENTER",crew=racer.slot,species=species.id,originalTruckColliders=truck.colliders,originalTruckRenderers=truck.renderers};var clips=new List<string>();var baked=new Mesh();bool started=false;
            void Add(Vector3 p){if(!started){rig.envelope=new Bounds(p,Vector3.zero);started=true;}else{var bounds=rig.envelope;bounds.Encapsulate(p);rig.envelope=bounds;}rig.maximumGateRadialExtent=Mathf.Max(rig.maximumGateRadialExtent,new Vector2(p.x,p.y).magnitude);}
            Vector3[] attitudes={Vector3.zero,new Vector3(0,0,-saddle.maximumBank),new Vector3(0,0,saddle.maximumBank),new Vector3(-saddle.maximumPitch,0,0),new Vector3(saddle.maximumPitch,0,0)};
            Vector3 Capture()
            {
                saddle.CaptureEvaluatedAnimationPose();var pose=saddle.ReadPose();saddle.supportPresentation.SetPositionAndRotation(pose.position,pose.rotation);var seat=pose.position+pose.rotation*Vector3.up*truck.support;truck.original.transform.SetPositionAndRotation(seat,pose.rotation);if(saddle.rigging!=null)saddle.rigging.RefreshAuthoredPresentation(true);var crossing=DragonRiderSaddleAuthoring.MeasureCurrentRiderTruckCrossingCenter(truck.original.shape,"mounted-course-sampled-pose").currentMatrixCenter;
                foreach(var skin in skins){baked.Clear();skin.BakeMesh(baked,true);var matrix=skin.transform.localToWorldMatrix;var vertices=baked.vertices;rig.skinnedVertices+=vertices.Length;foreach(var p in vertices)Add(matrix.MultiplyPoint3x4(p)-crossing);}
                foreach(var r in actor.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled))foreach(var p in Corners(r.bounds))Add(p-crossing);
                foreach(var p in truck.points)Add(seat+pose.rotation*p-crossing);rig.poses++;return crossing;
            }
            try
            {
                foreach(var action in new[]{DragonAnimationAction.FlyIdle,DragonAnimationAction.Fly})
                {
                    if(!species.TryGetAnimation(action,out var animation))throw new InvalidOperationException("Actual configured flight animation missing: "+species.id+"/"+action);
                    var actualClip=DragonRiderSaddleAuthoring.EffectiveRiderClip(actor,animation.clip);if(actualClip==null)throw new InvalidOperationException("Actual mode-specific mounted animation missing.");clips.Add(AssetDatabase.GetAssetPath(actualClip));
                    foreach(var attitude in attitudes)
                    for(int frame=0;frame<=16;frame++)
                    {
                        foreach(var p in basePoses){p.Item1.SetLocalPositionAndRotation(p.Item2,p.Item3);p.Item1.localScale=p.Item4;}
                        actualClip.SampleAnimation(model.gameObject,actualClip.length*frame/16f);model.SetPositionAndRotation(Vector3.zero,Quaternion.Euler(attitude));
                        var seat=Capture();rig.baseFlightPoses++;
                        if(action==DragonAnimationAction.FlyIdle&&attitude==Vector3.zero&&frame==4)rig.neutralSeatOffset=seat;
                    }
                }
                // Reuse the carrier author's exact configured controller and
                // actual native blend evaluation, including launch and Breath.
                // A detached/claw-carried truck is deliberately not treated as
                // seated here; its actual rescue phase remains separate.
                foreach(var trial in DragonRiderSaddleAuthoring.MountedAnimationTrials(actor,species))
                {
                    foreach(var clip in new[]{trial.clip,trial.incoming})if(clip!=null)clips.Add(AssetDatabase.GetAssetPath(clip));
                    foreach(var attitude in attitudes)
                    {
                        foreach(var p in basePoses){p.Item1.SetLocalPositionAndRotation(p.Item2,p.Item3);p.Item1.localScale=p.Item4;}
                        DragonRiderSaddleAuthoring.SampleMountedTrial(actor,trial);model.SetPositionAndRotation(Vector3.zero,Quaternion.Euler(attitude));Capture();rig.configuredGraphPoses++;if(trial.incoming!=null)rig.nativeMixerPoses++;
                    }
                }
            }
            finally{truck.original.transform.SetLocalPositionAndRotation(beforeTruckPosition,beforeTruckRotation);UnityEngine.Object.DestroyImmediate(baked);}
            rig.clips=clips.Distinct().ToArray();rig.clipSha256=rig.clips.Select(path=>Hash(path).Replace("-","").ToLowerInvariant()).ToArray();rig.controller=AssetDatabase.GetAssetPath(actor.motor.animator.runtimeAnimatorController);
            if(string.IsNullOrEmpty(rig.controller))throw new InvalidOperationException("Saved actual mounted controller asset required for reproducible course geometry.");rig.controllerSha256=Hash(rig.controller).Replace("-","").ToLowerInvariant();return rig;
        }
        static void Probe(string label,Vector3 a,Vector3 b,Bounds envelope,HashSet<Collider> blockers,Receipt receipt,List<Contact> contacts)
        {
            Vector3 forward=b-a;if(forward.sqrMagnitude<.0001f)return;forward=Vector3.ProjectOnPlane(forward,Vector3.up);if(forward.sqrMagnitude<.0001f)forward=Vector3.forward;
            var rotation=Quaternion.LookRotation(forward,Vector3.up);int count=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b))); // <=1m along every connector
            for(int i=0;i<=count;i++)
            {
                ProbePose(label,Vector3.Lerp(a,b,(float)i/count),rotation,envelope,receipt.fixedQualifiedCourse?Vector3.Distance(a,b)/(2*count):0,blockers,receipt,contacts);
            }
        }
        static void ProbeTurn(string label,Vector3 p,Vector3 from,Vector3 to,Bounds envelope,HashSet<Collider> blockers,Receipt receipt,List<Contact> contacts)
        {
            from=Vector3.ProjectOnPlane(from,Vector3.up);to=Vector3.ProjectOnPlane(to,Vector3.up);if(from.sqrMagnitude<.001f||to.sqrMagnitude<.001f)return;
            Quaternion a=Quaternion.LookRotation(from),b=Quaternion.LookRotation(to);float angle=Quaternion.Angle(a,b);int steps=Mathf.Max(1,Mathf.CeilToInt(angle/5));
            // Bound the unsampled half-angle arc of every box point, including
            // its center offset. Zero contacts cover the full stationary turn
            // envelope; actual AI turning trajectories still need flight proof.
            float radius=envelope.center.magnitude+envelope.extents.magnitude,padding=receipt.fixedQualifiedCourse?0:radius*2*Mathf.Sin(angle*Mathf.Deg2Rad/(4*steps));
            for(int i=0;i<=steps;i++){ProbePose(label,p,Quaternion.Slerp(a,b,(float)i/steps),envelope,padding,blockers,receipt,contacts);receipt.turnQueries++;}
        }
        static void ProbePose(string label,Vector3 p,Quaternion rotation,Bounds envelope,float padding,HashSet<Collider> blockers,Receipt receipt,List<Contact> contacts)
        {
                Vector3 center=p+rotation*envelope.center;receipt.queries++;
                var overlaps=receipt.fixedQualifiedCourse?Physics.OverlapSphere(p,receipt.qualifiedSweepRadius+padding,~0,QueryTriggerInteraction.Ignore):Physics.OverlapBox(center,envelope.extents+Vector3.one*padding,rotation,~0,QueryTriggerInteraction.Ignore);var hits=overlaps.Where(blockers.Contains).ToArray();if(hits.Length==0)return;receipt.contactQueries++;
                foreach(var hit in hits)
                {
                    string path=PathOf(hit.transform);receipt.blockerCounts.TryGetValue(path,out int total);receipt.blockerCounts[path]=total+1;
                    if(contacts.Count<512)contacts.Add(new Contact{kind="Conservative sampled full-mount bounds contact requires exact geometry diagnosis",path=label,position=p,collider=path,blocker=hit.bounds});
                }
        }
        static IEnumerable<Vector3> Corners(Bounds b){foreach(float x in new[]{b.min.x,b.max.x})foreach(float y in new[]{b.min.y,b.max.y})foreach(float z in new[]{b.min.z,b.max.z})yield return new Vector3(x,y,z);}
        static string PathOf(Transform t){string result=t.name;while(t.parent!=null){t=t.parent;result=t.name+"/"+result;}return result;}
        static string Arg(string key,string fallback){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,key);return at>=0&&at+1<args.Length?args[at+1]:fallback;}
        static string Io(string path){string full=Path.GetFullPath(path);return Application.platform==RuntimePlatform.WindowsEditor&&full.Length>=248&&!full.StartsWith(@"\\?\",StringComparison.Ordinal)?@"\\?\"+full:full;}
        static string Hash(string path){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(Io(path))));}
    }
}
#endif
