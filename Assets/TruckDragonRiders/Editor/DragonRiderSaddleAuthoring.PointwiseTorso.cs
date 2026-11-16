#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckDragonKingdom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        [Serializable]sealed class PointwiseTorsoReport
        {
            public string scope="READ_ONLY_EXACT_POINTWISE_ORIGINAL_FIVE_RAY_MAJORITY_TORSO_INTERVALS_ALL12_SOURCE_CALIBRATION_PHASES_NOT_ALL_VERTEX_SCREEN_OR_OFFSET_GRID",failure;
            public bool sourceExact,sceneExact,nwhExact,derived;public float center;public int sourcePoses;public List<PointwiseTorsoPose> poses=new List<PointwiseTorsoPose>();public Vector2[] completeIntervals;public SurfaceProof[] points;
        }
        [Serializable]sealed class PointwiseTorsoPose{public string clip,action;public float phase;public Vector2[] allowedCenters;}
        struct TorsoCut{public float z,y,torso,wing;}
        static List<Vector2> UnionTorsoRanges(IEnumerable<Vector2> input)
        {
            var result=new List<Vector2>();foreach(var range in input.OrderBy(value=>value.x)){if(result.Count==0||range.x>result[result.Count-1].y+1e-6f)result.Add(range);else{var previous=result[result.Count-1];previous.y=Mathf.Max(previous.y,range.y);result[result.Count-1]=previous;}}return result;
        }
        static List<Vector2> IntersectTorsoRanges(IEnumerable<Vector2> a,IEnumerable<Vector2> b)=>UnionTorsoRanges(a.SelectMany(left=>b.Select(right=>new Vector2(Mathf.Max(left.x,right.x),Mathf.Min(left.y,right.y)))).Where(value=>value.y-value.x>1e-6f));
        static List<Vector2> PointwiseTorsoLane(Vector3[] vertices,int[] triangles,BoneWeight[] weights,Transform[] bones,Vector3 origin,float lateral)
        {
            var ranges=new List<Vector2>();float x=origin.x+lateral;
            for(int i=0;i<triangles.Length;i+=3)
            {
                var ids=new[]{triangles[i],triangles[i+1],triangles[i+2]};var p=ids.Select(id=>vertices[id]).ToArray();if(Vector3.Cross(p[1]-p[0],p[2]-p[0]).normalized.y<.1f||p.Any(point=>Mathf.Abs(point.x-origin.x)>.8f||Mathf.Abs(point.z-origin.z)>1.1f))continue;
                var torso=ids.Select(id=>TorsoWeight(weights[id],bones)).ToArray();var wing=ids.Select(id=>WingWeight(weights[id],bones)).ToArray();var cuts=new List<TorsoCut>();
                for(int edge=0;edge<3;edge++)
                {
                    int next=(edge+1)%3;var a=p[edge];var b=p[next];if(Mathf.Abs(a.x-b.x)<1e-7f){if(Mathf.Abs(x-a.x)<1e-6f){cuts.Add(new TorsoCut{z=a.z-origin.z,y=a.y,torso=torso[edge],wing=wing[edge]});cuts.Add(new TorsoCut{z=b.z-origin.z,y=b.y,torso=torso[next],wing=wing[next]});}continue;}
                    float t=(x-a.x)/(b.x-a.x);if(t>=0&&t<=1)cuts.Add(new TorsoCut{z=Mathf.Lerp(a.z,b.z,t)-origin.z,y=Mathf.Lerp(a.y,b.y,t),torso=Mathf.Lerp(torso[edge],torso[next],t),wing=Mathf.Lerp(wing[edge],wing[next],t)});
                }
                if(cuts.Count<2)continue;var low=cuts.OrderBy(value=>value.z).First();var high=cuts.OrderBy(value=>value.z).Last();if(high.z-low.z<1e-7f)continue;float from=0,to=1;
                bool AtLeast(float a,float b,float minimum){float delta=b-a;if(Mathf.Abs(delta)<1e-8f)return a>=minimum;float crossing=(minimum-a)/delta;if(delta>0)from=Mathf.Max(from,crossing);else to=Mathf.Min(to,crossing);return to>=from;}
                // Weights and height are affine along this actual triangle
                // segment. Require the real POINT to be majority torso; an
                // unused corner need not itself be majority torso.
                if(AtLeast(low.torso,high.torso,.500001f)&&AtLeast(-low.wing,-high.wing,-.5f)&&AtLeast(low.y,high.y,origin.y-.1f)&&AtLeast(-low.y,-high.y,-origin.y-1.2f)&&to>=from)ranges.Add(new Vector2(Mathf.Lerp(low.z,high.z,from),Mathf.Lerp(low.z,high.z,to)));
            }
            return UnionTorsoRanges(ranges);
        }
        static ProvenInfernoMaterial DeriveActualPointwiseInfernoMaterial()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled native Editor required.");for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve unsaved scenes.");
            var report=new PointwiseTorsoReport();var watched=Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories).ToDictionary(path=>path,Hash);string sceneHash=Hash(DragonRidersAuthoring.ScenePath),physics=null;var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;DragonRidersMatch match=null;var preview=new PreviewRenderUtility();DragonRiderSaddle frame=null;var mesh=new Mesh();ProvenInfernoMaterial result=null;
            try
            {
                scene=EditorSceneManager.OpenScene(DragonRidersAuthoring.ScenePath,OpenSceneMode.Additive);match=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<DragonRidersMatch>(true)).Single();physics=PhysicsSignature(match);var saved=match.racers.SelectMany(racer=>racer.choices).First(actor=>actor.GetComponent<DragonRiderSaddle>().speciesId=="dragon-inferno");var copy=UnityEngine.Object.Instantiate(saved.gameObject);preview.AddSingleGO(copy);copy.SetActive(true);var actor=copy.GetComponent<DragonCharacter>();frame=copy.GetComponent<DragonRiderSaddle>();actor.motor.animator.enabled=false;foreach(var body in copy.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.detectCollisions=false;}
                var catalog=AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>("Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset");if(!catalog.TryGet("dragon-inferno",out var species)||!species.TryGetAnimation(DragonAnimationAction.FlyIdle,out var neutral))throw new InvalidOperationException("Actual original source mappings required.");var model=actor.motor.transform;var bone=frame.sourceBone;var skin=frame.surfaceRenderer;var rest=model.GetComponentsInChildren<Transform>(true).Select(joint=>new SavedJoint{joint=joint,position=joint.localPosition,rotation=joint.localRotation,scale=joint.localScale}).ToArray();List<Vector2> common=null;
                foreach(var action in new[]{DragonAnimationAction.GroundIdle,DragonAnimationAction.FlyIdle,DragonAnimationAction.Fly})
                {
                    if(!species.TryGetAnimation(action,out var mapping))throw new InvalidOperationException("All original calibration actions required.");var clip=EffectiveRiderClip(actor,mapping.clip);
                    foreach(float phase in new[]{0f,.25f,.5f,.75f})
                    {
                        foreach(var joint in rest)joint.Restore();clip.SampleAnimation(model.gameObject,clip.length*phase);skin.BakeMesh(mesh,true);var vertices=mesh.vertices.Select(point=>model.InverseTransformPoint(skin.transform.TransformPoint(point))).ToArray();var origin=model.InverseTransformPoint(bone.position);var center=PointwiseTorsoLane(vertices,skin.sharedMesh.triangles,skin.sharedMesh.boneWeights,skin.bones,origin,0);var right=PointwiseTorsoLane(vertices,skin.sharedMesh.triangles,skin.sharedMesh.boneWeights,skin.bones,origin,.15f);var left=PointwiseTorsoLane(vertices,skin.sharedMesh.triangles,skin.sharedMesh.boneWeights,skin.bones,origin,-.15f);
                        // EXACT existing five rays: fore/rear only use center
                        // lane. Do not erode side lanes or invent extra rays.
                        var allowed=IntersectTorsoRanges(IntersectTorsoRanges(center,right),left);allowed=IntersectTorsoRanges(allowed,center.Select(range=>range-Vector2.one*.15f));allowed=IntersectTorsoRanges(allowed,center.Select(range=>range+Vector2.one*.15f));report.poses.Add(new PointwiseTorsoPose{clip=AssetDatabase.GetAssetPath(clip),action=action.ToString(),phase=phase,allowedCenters=allowed.ToArray()});report.sourcePoses++;common=common==null?allowed:IntersectTorsoRanges(common,allowed);
                    }
                }
                report.completeIntervals=common.ToArray();var candidates=common.Where(range=>(range.x+range.y)*.5f<0).OrderBy(range=>Mathf.Abs((range.x+range.y)*.5f)).ToArray();if(candidates.Length==0)throw new InvalidOperationException("No exact pointwise majority-torso five-ray interval survives all12source phases with original.15radius; no offset grid or smaller baseline substituted.");report.center=(candidates[0].x+candidates[0].y)*.5f;
                foreach(var joint in rest)joint.Restore();var poses=rest.Select(joint=>new BonePose{bone=joint.joint,position=joint.position,rotation=joint.rotation}).ToArray();var measured=MeasureActualDorsalCandidate(actor,species,neutral,EffectiveRiderClip(actor,neutral.clip),poses,Quaternion.Inverse(bone.rotation)*model.rotation,bone.InverseTransformDirection(model.up).normalized,report.center,.15f,true);var points=measured.proof.Where(point=>point.context==species.id+"-"+neutral.action+"-0.25-patch"+report.center.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)).ToArray();VerifyCenteredMaterialPatch(model,bone,points,report.center,.15f,true,out _,out _,out _);report.points=points;report.derived=true;result=new ProvenInfernoMaterial{folder="ACTUAL_NATIVE_SAME_INVOCATION_POINTWISE_ALL12_SOURCE_PHASES_AND_EXISTING60_SURFACE_VERIFIER",rearward=report.center,points=points};
            }
            catch(Exception error){report.failure=error.ToString();}
            finally
            {
                if(frame!=null)frame.ReleaseGeometrySnapshot();UnityEngine.Object.DestroyImmediate(mesh);preview.Cleanup();report.nwhExact=match!=null&&PhysicsSignature(match)==physics;if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);report.sceneExact=Hash(DragonRidersAuthoring.ScenePath)==sceneHash;report.sourceExact=watched.Count==Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories).Length&&watched.All(pair=>File.Exists(pair.Key)&&Hash(pair.Key)==pair.Value);var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at>=0&&at+1<args.Length){string folder=Path.Combine(args[at+1],"RiderSaddleAuthoring","PointwiseTorso");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"actual-all12-pointwise-five-ray-material-intervals.json"),JsonUtility.ToJson(report,true));}
            }
            if(!report.sourceExact||!report.sceneExact||!report.nwhExact||!report.derived||!string.IsNullOrEmpty(report.failure))throw new InvalidOperationException("Actual source-derived pointwise torso calibration rejected: "+report.failure);return result;
        }
        public static string InspectPrecisePointwiseInfernoMaterialMountedSizeWithoutSavingAssets(string startupEvidence)
        {
            var startup=RequireProvenActualInfernoStartup(startupEvidence);var material=DeriveActualPointwiseInfernoMaterial();return InspectMountedSizeProfiles(true,true,false,startup,null,material);
        }
    }
}
#endif
