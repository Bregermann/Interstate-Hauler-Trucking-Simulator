#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LWS.TruckDragonKingdom;
using LWS.TruckDragonKingdom.Editor;
using LWS.TruckSports;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        const string PresentationName="LWS Riders semantic saddle presentation";
        const string DeckName="Actual timber truck saddle support";
        sealed class Calibration
        {public Vector3 position,supportMotorLocalPosition,anchorBarycentric;public Quaternion rotation;public AnimationClip clip;public int surfaces,anchorA,anchorB,anchorC;public string anchorMesh;public float modelRearwardOffset,dorsalMinimum,dorsalMaximum;public SurfaceProof[] proof;public DragonRiderSurfaceAnchor[] patchAnchors;public FootprintReceipt footprint;}
        struct BonePose {public Transform bone;public Vector3 position;public Quaternion rotation;public void Restore()=>bone.SetLocalPositionAndRotation(position,rotation);}
        [Serializable]sealed class Receipt {public string scope="EXPLICIT_SAVED_RIDERS_ONLY_NEUTRAL_ANIMATION_AND_ACTUAL_BAKED_DORSAL_SKIN_CALIBRATION_NOT_RUNTIME_VISUAL_ACCEPTANCE",sceneGuid;public int mountedChoices,changed;public bool sourceBytesPreserved,nwhPhysicsPreserved;public Record[] rigs;}
        [Serializable]sealed class Record {public string species,clip,anchorMesh;public Vector3 socketLocalPosition,supportMotorLocalPosition,anchorBarycentric;public Quaternion socketLocalRotation;public int sampledSkinSurfaces,anchorA,anchorB,anchorC;public float modelRearwardOffset,measuredDorsalMinimum,measuredDorsalMaximum;public SurfaceProof[] actualSurfaces;public DragonRiderSurfaceAnchor[] patchAnchors;public FootprintReceipt footprint;}
        sealed class DorsalPatchUnavailable : InvalidOperationException {public DorsalPatchUnavailable(string message):base(message){}}
        [Serializable]sealed class SurfaceProof {public string context,mesh;public Vector3 actualModelPoint,barycentric;public float upwardNormal,torsoSkinWeight,wingSkinWeight,cpuSkinningRmse;public bool wingDominated;public Influence[] vertexInfluences;}
        [Serializable]sealed class DorsalProbe
        {
            public string scope="READ_ONLY_ACTUAL_SKIN_BAKE_AND_DORSAL_RAY_DIAGNOSIS_NO_SAVED_ASSETS",context,model,bone;
            public Vector3 modelPosition,modelEuler,modelScale,bonePosition,boneModelPosition,boneRight,boneUp,boneForward;public Vector2 ray;
            public SkinProbe[] skins;
        }
        [Serializable]sealed class SkinProbe
        {
            public string path,mesh,vertexFile;public Vector3 position,euler,scale;public Bounds sourceBounds,rendererBounds,falseBakeBounds,falseModelBounds,trueModelBounds,selectedModelBounds;
            public string selectedContract="BakeMesh(true) with complete renderer local-to-world; validated against actual weighted bindpose/bone reference";
            public int vertices,triangles,torsoTriangles,allProjectionHits,torsoProjectionHits,heightAccepted,upwardAccepted;public float determinant;public RayProbe[] intersections;
            public SkinningContract skinning;
        }
        [Serializable]sealed class RayProbe {public float height,crossNormalY,interpolatedNormalY,torsoSkinWeight,wingSkinWeight;public bool torso,heightWindow,wingDominated;public Vector3 a,b,c;public Influence[] actualVertexInfluences;}
        [Serializable]sealed class SkinningContract
        {
            public string error;public int sourceVertices,weights,bindposes,bones;public Bounds cpuExpectedModelBounds,unitScaleModelBounds;public Vector3 unitScaleBoneModel;
            public float falseFullMatrixRmse,falseScaleFreeRmse,trueFullMatrixRmse,trueScaleFreeRmse,unitScaleVsCpuRmse;
            public Influence[] nearestBackVertices;
        }
        [Serializable]sealed class Influence {public int vertex;public Vector3 cpuExpectedModelPosition;public string[] bones;public float[] weights;}

        public static void UpgradeSavedSaddles()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Explicit stopped Editor migration required.");
            string path=DragonRidersAuthoring.ScenePath;if(!File.Exists(path))throw new InvalidOperationException("Existing saved Riders scene required; this upgrade never builds a replacement world or truck.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).path==path)throw new InvalidOperationException("Close the actual Riders scene before this isolated saved-scene upgrade.");
            var before=new Dictionary<string,string>();string sceneGuid=AssetDatabase.AssetPathToGUID(path);Watch(path+".meta",before);
            var catalog=AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>("Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset");if(catalog==null)throw new InvalidOperationException("Actual configured catalog required.");
            foreach(var species in catalog.species.Where(value=>value!=null&&value.Supports(DragonSpeciesRole.RiderMount)))
            {string actor=ActorPath(species.id);foreach(string dependency in AssetDatabase.GetDependencies(actor,true))Watch(dependency,before);}
            UpgradeKnownFireHoverForExplicitSaddleMigration(catalog);
            var original=SceneManager.GetActiveScene();Scene scene=default;var receipt=new Receipt{sceneGuid=sceneGuid};
            try
            {
                scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);var match=scene.GetRootGameObjects().SelectMany(value=>value.GetComponentsInChildren<DragonRidersMatch>(true)).Single();
                string physics=PhysicsSignature(match);var records=new List<Record>();var calibrations=new Dictionary<string,Calibration>();
                foreach(var racer in match.racers)for(int index=0;index<racer.choices.Length;index++)
                {
                    var actor=racer.choices[index];if(!catalog.TryGet(racer.choiceIds[index],out var species))throw new InvalidOperationException("Unknown actual mount species.");
                    var existing=actor.GetComponent<DragonRiderSaddle>();receipt.mountedChoices++;
                    if(existing!=null&&existing.calibrationVersion==7){existing.Validate(actor);continue;}
                    if(!calibrations.TryGetValue(species.id,out var calibration)){calibration=Calibrate(species,match.trucks);calibrations.Add(species.id,calibration);records.Add(new Record{species=species.id,clip=AssetDatabase.GetAssetPath(calibration.clip),socketLocalPosition=calibration.position,socketLocalRotation=calibration.rotation,supportMotorLocalPosition=calibration.supportMotorLocalPosition,anchorMesh=calibration.anchorMesh,anchorA=calibration.anchorA,anchorB=calibration.anchorB,anchorC=calibration.anchorC,anchorBarycentric=calibration.anchorBarycentric,patchAnchors=calibration.patchAnchors,sampledSkinSurfaces=calibration.surfaces,modelRearwardOffset=calibration.modelRearwardOffset,measuredDorsalMinimum=calibration.dorsalMinimum,measuredDorsalMaximum=calibration.dorsalMaximum,actualSurfaces=calibration.proof,footprint=calibration.footprint});}
                    Install(actor,racer.truck,species,calibration);receipt.changed++;
                }
                receipt.rigs=records.ToArray();receipt.nwhPhysicsPreserved=physics==PhysicsSignature(match);if(!receipt.nwhPhysicsPreserved)throw new InvalidOperationException("Saddle migration changed original NWH physics.");
                if(receipt.changed>0){EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Could not retain the selected owned Riders upgrade.");}
                if(AssetDatabase.AssetPathToGUID(path)!=sceneGuid)throw new InvalidOperationException("Saved Riders scene GUID changed.");
                receipt.sourceBytesPreserved=before.All(pair=>File.Exists(pair.Key)&&Hash(pair.Key)==pair.Value);if(!receipt.sourceBytesPreserved)throw new InvalidOperationException("Original configured/source rig bytes changed.");
                var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at>=0&&at+1<args.Length){string output=Path.Combine(args[at+1],"RiderSaddleAuthoring");Directory.CreateDirectory(output);string receiptPath=Path.Combine(output,"receipt.json");if(receipt.changed>0||!File.Exists(receiptPath))File.WriteAllText(receiptPath,JsonUtility.ToJson(receipt,true));}
                Debug.Log("RIDERS_ACTUAL_SADDLE_UPGRADE choices="+receipt.mountedChoices+" changed="+receipt.changed+" sourceBytesExact=true nwhPhysicsExact=true sceneGuid="+sceneGuid);
            }
            finally{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);}
        }
        public static void InstallForNewChoice(DragonCharacter actor,SportsEventTruck truck,DragonSpeciesDefinition species){Install(actor,truck,species,Calibrate(species,new[]{truck}));ApplySavedNormalFlightIfPresent(actor,species);}
        static string ActorPath(string id)=>"Assets/TruckDragonKingdom/Prefabs/Dragons/"+id+"/"+id+" Dragon Actor.prefab";
        static Calibration Calibrate(DragonSpeciesDefinition species,SportsEventTruck[] trucks)
        {
            var root=PrefabUtility.LoadPrefabContents(ActorPath(species.id));
            try
            {
                var actor=root.GetComponent<DragonCharacter>();ConfigureRiderHover(actor,species);var model=actor.motor.transform;var bone=model.GetComponentsInChildren<Transform>(true).Single(value=>value.name==species.saddleBone);actor.motor.animator.enabled=false;
                if(!species.TryGetAnimation(DragonAnimationAction.FlyIdle,out var neutral)&&!species.TryGetAnimation(DragonAnimationAction.Fly,out neutral))throw new InvalidOperationException("Actual neutral flight clip required for "+species.id);
                var neutralClip=EffectiveRiderClip(actor,neutral.clip);
                var poses=model.GetComponentsInChildren<Transform>(true).Select(value=>new BonePose{bone=value,position=value.localPosition,rotation=value.localRotation}).ToArray();
                neutralClip.SampleAnimation(model.gameObject,neutralClip.length*.25f);Quaternion rotation=Quaternion.Inverse(bone.rotation)*model.rotation;Vector3 localUp=bone.InverseTransformDirection(model.up).normalized;
                DorsalPatchUnavailable lastMiss=null;
                // V141 Fire's generic forward ray reaches100%RigHead skin,
                // not its back. Select a bounded rearward actual torso patch;
                // every candidate must pass all unchanged pose/ray/weight gates.
                foreach(float rearward in new[]{0f,-.075f,-.15f,-.225f,-.3f,-.375f,-.45f})
                {
                    Calibration candidate;
                    try{candidate=MeasureActualDorsalCandidate(actor,species,neutral,neutralClip,poses,rotation,localUp,rearward);}
                    catch(DorsalPatchUnavailable miss){lastMiss=miss;continue;}
                    bool fits=FitsOriginalFootprints(actor,species,candidate,poses,trucks,out var footprint);WriteFootprintReceipt(footprint);
                    if(!fits){lastMiss=new DorsalPatchUnavailable("Original NWH footprint rejected this measured torso patch: "+footprint.failure);continue;}
                    candidate.footprint=footprint;return candidate;
                }
                throw new InvalidOperationException("No bounded genuine torso patch supports all actual mount poses for "+species.id+". Last measured rejection: "+lastMiss?.Message,lastMiss);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static Calibration MeasureActualDorsalCandidate(DragonCharacter actor,DragonSpeciesDefinition species,DragonAnimationMapping neutral,AnimationClip neutralClip,BonePose[] poses,Quaternion rotation,Vector3 localUp,float rearward,float longitudinalRadius=.15f,bool strictMaterialInside=false)
        {
            if(longitudinalRadius!=.15f&&(species.id!="dragon-fire"||longitudinalRadius!=.075f&&longitudinalRadius!=.065f))throw new InvalidOperationException("Only the original or explicitly measured Fire longitudinal baselines are authorized.");
            var model=actor.motor.transform;var bone=model.GetComponentsInChildren<Transform>(true).Single(value=>value.name==species.saddleBone);
            var points=new List<Vector3>();var proof=new List<SurfaceProof>();int surfaces=0;
            foreach(var action in new[]{DragonAnimationAction.GroundIdle,DragonAnimationAction.FlyIdle,DragonAnimationAction.Fly})
            {
                if(!species.TryGetAnimation(action,out var mapping))continue;
                foreach(float fraction in new[]{0f,.25f,.5f,.75f})
                {
                    foreach(var pose in poses)pose.Restore();var actualClip=EffectiveRiderClip(actor,mapping.clip);actualClip.SampleAnimation(model.gameObject,actualClip.length*fraction);
                    foreach(var offset in new[]{Vector2.zero,new Vector2(.15f,0),new Vector2(-.15f,0),new Vector2(0,longitudinalRadius),new Vector2(0,-longitudinalRadius)})
                    {Vector3 point=DorsalSurface(model,bone,offset+new Vector2(0,rearward),species.id+"-"+action+"-"+fraction.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+"-patch"+rearward.ToString("F3",System.Globalization.CultureInfo.InvariantCulture),out var actualSurface,strictMaterialInside:strictMaterialInside);points.Add(bone.InverseTransformPoint(point));proof.Add(actualSurface);surfaces++;}
                }
            }
            if(points.Count<3)throw new InvalidOperationException("Actual dorsal skin measurements missing.");
            Vector3 highest=points.OrderByDescending(value=>Vector3.Dot(value,localUp)).First();
            foreach(var pose in poses)pose.Restore();neutralClip.SampleAnimation(model.gameObject,neutralClip.length*.25f);
            Vector3 centerLocal=bone.InverseTransformVector(model.TransformVector(new Vector3(0,0,rearward)));
            Vector3 localPosition=centerLocal-localUp*Vector3.Dot(centerLocal,localUp)+localUp*Vector3.Dot(highest,localUp)+localUp*(.45f/Mathf.Max(.001f,bone.lossyScale.y));
            float minimum=proof.Min(value=>value.actualModelPoint.y),maximum=proof.Max(value=>value.actualModelPoint.y);
            Vector3 neutralRoot=model.InverseTransformPoint(bone.position);
            Vector3 physicalSupport=new Vector3(neutralRoot.x,maximum+.45f/Mathf.Max(.001f,model.lossyScale.y),neutralRoot.z+rearward);
            string neutralContext=species.id+"-"+neutral.action+"-0.25-patch"+rearward.ToString("F3",System.Globalization.CultureInfo.InvariantCulture);
            var anchor=proof.First(value=>value.context==neutralContext);var vertices=anchor.vertexInfluences;
            Vector3 barycentric=anchor.barycentric;
            if(barycentric.x<-.0001f||barycentric.y<-.0001f||barycentric.z<-.0001f)throw new InvalidOperationException("Actual dorsal attachment point lies outside its verified torso triangle.");
            var patch=proof.Where(value=>value.context==neutralContext).ToArray();
            if(patch.Length!=5||patch.Any(value=>value.mesh!=anchor.mesh))throw new InvalidOperationException("Five verified torso material points on the same actual skin are required.");
            var patchAnchors=patch.Select(value=>new DragonRiderSurfaceAnchor{a=value.vertexInfluences[0].vertex,b=value.vertexInfluences[1].vertex,c=value.vertexInfluences[2].vertex,barycentric=value.barycentric}).ToArray();
            return new Calibration{position=localPosition,rotation=rotation,supportMotorLocalPosition=physicalSupport,dorsalMinimum=minimum,dorsalMaximum=maximum,clip=neutralClip,surfaces=surfaces,modelRearwardOffset=rearward,proof=proof.ToArray(),anchorMesh=anchor.mesh,anchorA=vertices[0].vertex,anchorB=vertices[1].vertex,anchorC=vertices[2].vertex,anchorBarycentric=barycentric,patchAnchors=patchAnchors};
        }
        public static bool TryMeasureLiveDorsalSupport(Transform model,Transform bone,Vector3 worldRay,out Vector3 point)
        {
            var requested=model.InverseTransformPoint(worldRay);var anchor=model.InverseTransformPoint(bone.position);
            try{point=DorsalSurface(model,bone,new Vector2(requested.x-anchor.x,requested.z-anchor.z),"actual-live-original-truck-footprint",out _,true);return true;}
            catch(DorsalPatchUnavailable){point=default;return false;}
        }
        public static string ExplainActualLiveDorsalSupport(Transform model,Transform bone,Vector3 worldRay)
        {
            var query=model.InverseTransformPoint(worldRay);var origin=model.InverseTransformPoint(bone.position);
            DorsalSurface(model,bone,new Vector2(query.x-origin.x,query.z-origin.z),"actual-live-independent-negative-underside-ray",out var proof,true);return JsonUtility.ToJson(proof,true);
        }
        static Vector3 DorsalSurface(Transform model,Transform bone,Vector2 offset,string context,out SurfaceProof proof,bool live=false,bool includeWingPanelsForDiagnosis=false,bool strictMaterialInside=false)
        {
            proof=null;Vector3 anchor=model.InverseTransformPoint(bone.position);Vector2 query=new Vector2(anchor.x+offset.x,anchor.z+offset.y);float best=float.NegativeInfinity;int hits=0;
            var diagnostics=new List<SkinProbe>();var worldPoints=new List<Vector3[]>();var probe=new DorsalProbe{context=context,model=model.name,bone=bone.name,modelPosition=model.position,modelEuler=model.eulerAngles,modelScale=model.lossyScale,bonePosition=bone.position,boneModelPosition=anchor,boneRight=model.InverseTransformDirection(bone.right),boneUp=model.InverseTransformDirection(bone.up),boneForward=model.InverseTransformDirection(bone.forward),ray=query};
            var baked=new Mesh();var scaled=new Mesh();
            try
            {
                foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(value=>value.sharedMesh!=null&&!value.name.StartsWith("LWS ",StringComparison.Ordinal)))
                {
                    baked.Clear();skin.BakeMesh(baked,false);var matrix=model.worldToLocalMatrix*skin.transform.localToWorldMatrix;
                    var falsePoints=baked.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
                    scaled.Clear();skin.BakeMesh(scaled,true);var scaleFree=Matrix4x4.TRS(skin.transform.position,skin.transform.rotation,Vector3.one);var truePoints=scaled.vertices.Select(point=>model.InverseTransformPoint(scaleFree.MultiplyPoint3x4(point))).ToArray();
                    // V130 actual2552weightedvertices proved this contract:
                    // true+fullmatrixRMSE5.28e-7 vs false+fullmatrix8.36modelm.
                    // Select the native bake against that real skinning proof,
                    // never divide by a guessed scale or use renderer bounds.
                    var points=scaled.vertices;var normals=scaled.normals;var normalMatrix=matrix.inverse.transpose;
                    for(int i=0;i<points.Length;i++){points[i]=matrix.MultiplyPoint3x4(points[i]);if(normals.Length==points.Length)normals[i]=normalMatrix.MultiplyVector(normals[i]).normalized;}var triangles=scaled.triangles;
                    var info=new SkinProbe{path=AnimationUtility.CalculateTransformPath(skin.transform,model),mesh=AssetDatabase.GetAssetPath(skin.sharedMesh),position=skin.transform.position,euler=skin.transform.eulerAngles,scale=skin.transform.lossyScale,sourceBounds=skin.sharedMesh.bounds,rendererBounds=skin.bounds,falseBakeBounds=baked.bounds,falseModelBounds=BoundsOf(falsePoints),trueModelBounds=BoundsOf(truePoints),selectedModelBounds=BoundsOf(points),determinant=matrix.determinant,vertices=points.Length,triangles=triangles.Length/3};
                    info.skinning=ProbeSkinningContract(model,bone,skin,baked.vertices,scaled.vertices,!live);
                    bool verified=string.IsNullOrEmpty(info.skinning.error)&&info.skinning.trueFullMatrixRmse<.0001f;
                    var skinWeights=skin.sharedMesh.boneWeights;var skinBones=skin.bones;
                    var intersections=new List<RayProbe>();diagnostics.Add(info);worldPoints.Add(points);
                    for(int index=0;index<triangles.Length;index+=3)
                    {
                        var a=points[triangles[index]];var b=points[triangles[index+1]];var c=points[triangles[index+2]];
                        // Local torso triangles only: large wing triangles and
                        // remote neck/head geometry cannot set dorsal clearance.
                        bool torso=!new[]{a,b,c}.Any(value=>Mathf.Abs(value.x-anchor.x)>.8f||Mathf.Abs(value.z-anchor.z)>1.1f);if(torso)info.torsoTriangles++;
                        var u=new Vector2(b.x-a.x,b.z-a.z);var v=new Vector2(c.x-a.x,c.z-a.z);var p=query-new Vector2(a.x,a.z);float determinant=u.x*v.y-u.y*v.x;if(Mathf.Abs(determinant)<.0000001f)continue;
                        float wb=(p.x*v.y-p.y*v.x)/determinant,wc=(u.x*p.y-u.y*p.x)/determinant,wa=1-wb-wc;float minimumBarycentric=strictMaterialInside?0:-.0001f;if(wa<minimumBarycentric||wb<minimumBarycentric||wc<minimumBarycentric)continue;
                        float height=a.y*wa+b.y*wb+c.y*wc,cross=Vector3.Cross(b-a,c-a).normalized.y;bool heightWindow=height>=anchor.y-.1f&&height<=anchor.y+1.2f;info.allProjectionHits++;float torsoWeight=TorsoWeight(skinWeights[triangles[index]],skinBones)*wa+TorsoWeight(skinWeights[triangles[index+1]],skinBones)*wb+TorsoWeight(skinWeights[triangles[index+2]],skinBones)*wc;
                        // Actual V194 negative-ray triangles are84-94% wing
                        // driven, despite a small Ribcage blend. They are wing
                        // panels, not the dorsal support surface. Legitimate
                        // V184 low torso weights (.142/.298) are neck blends,
                        // so never require majority torso influence. Keep the
                        // unchanged local/upward/actual-skin gates and retain
                        // raw wing hits separately for physical/render review.
                        float wingWeight=WingWeight(skinWeights[triangles[index]],skinBones)*wa+WingWeight(skinWeights[triangles[index+1]],skinBones)*wb+WingWeight(skinWeights[triangles[index+2]],skinBones)*wc;bool wingDominated=wingWeight>.5f;
                        torso&=torsoWeight>0&&(includeWingPanelsForDiagnosis||!wingDominated);if(torso)info.torsoProjectionHits++;float interpolated=normals.Length==points.Length?(normals[triangles[index]]*wa+normals[triangles[index+1]]*wb+normals[triangles[index+2]]*wc).normalized.y:0;
                        intersections.Add(new RayProbe{height=height,crossNormalY=cross,interpolatedNormalY=interpolated,torsoSkinWeight=torsoWeight,wingSkinWeight=wingWeight,wingDominated=wingDominated,torso=torso,heightWindow=heightWindow,a=a,b=b,c=c,
                            actualVertexInfluences=new[]{triangles[index],triangles[index+1],triangles[index+2]}.Select(vertex=>SkinInfluence(vertex,points[vertex],skinWeights[vertex],skinBones)).ToArray()});
                        if(!verified||!torso||!heightWindow)continue;info.heightAccepted++;if(cross<.1f)continue;info.upwardAccepted++;hits++;
                        if(height>best){best=height;proof=new SurfaceProof{context=context,mesh=info.mesh,actualModelPoint=new Vector3(query.x,height,query.y),barycentric=new Vector3(wa,wb,wc),upwardNormal=cross,torsoSkinWeight=torsoWeight,wingSkinWeight=wingWeight,wingDominated=wingDominated,cpuSkinningRmse=info.skinning.trueFullMatrixRmse,vertexInfluences=intersections[intersections.Count-1].actualVertexInfluences};}
                    }
                    info.intersections=intersections.OrderByDescending(value=>value.height).ToArray();
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(baked);UnityEngine.Object.DestroyImmediate(scaled);}
            if(hits==0)
            {
                probe.skins=diagnostics.ToArray();string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");string receipt="no explicit evidence directory";
                if(!live&&at>=0&&at+1<args.Length)
                {
                    string folder=Path.Combine(args[at+1],"RiderSaddleAuthoring","DorsalProbe-"+context);Directory.CreateDirectory(folder);
                    for(int skin=0;skin<worldPoints.Count;skin++){var points=worldPoints[skin];var floats=new float[points.Length*3];for(int vertex=0;vertex<points.Length;vertex++){floats[vertex*3]=points[vertex].x;floats[vertex*3+1]=points[vertex].y;floats[vertex*3+2]=points[vertex].z;}var bytes=new byte[floats.Length*sizeof(float)];Buffer.BlockCopy(floats,0,bytes,0,bytes.Length);diagnostics[skin].vertexFile="skin-"+skin+"-model-vertices.float32";File.WriteAllBytes(Path.Combine(folder,diagnostics[skin].vertexFile),bytes);}
                    receipt=Path.Combine(folder,"probe.json");File.WriteAllText(receipt,JsonUtility.ToJson(probe,true));
                }
                string message="No actual upward torso skin above "+bone.name+" for dorsal ray "+offset+" on "+model.name+". Measured bake/ray diagnostics: "+receipt+". No whole-wing/renderer-bound substitute allowed.";
                if(diagnostics.Any(value=>!string.IsNullOrEmpty(value.skinning.error)||value.skinning.trueFullMatrixRmse>=.0001f))throw new InvalidOperationException("Actual skinning contract failed; never try a different patch to hide it. "+message);
                throw new DorsalPatchUnavailable(message);
            }
            return model.TransformPoint(new Vector3(query.x,best,query.y));
        }
        static Bounds BoundsOf(Vector3[] points){if(points.Length==0)return default;var bounds=new Bounds(points[0],Vector3.zero);foreach(var point in points)bounds.Encapsulate(point);return bounds;}
        static float TorsoWeight(BoneWeight value,Transform[] bones)
        {float total=0;var indices=new[]{value.boneIndex0,value.boneIndex1,value.boneIndex2,value.boneIndex3};var weights=new[]{value.weight0,value.weight1,value.weight2,value.weight3};for(int i=0;i<4;i++)if(weights[i]>0&&indices[i]<bones.Length&&bones[indices[i]]!=null){string name=bones[indices[i]].name;if(name.StartsWith("RigSpine",StringComparison.Ordinal)||name=="RigRibcage"||name=="RigPelvis")total+=weights[i];}return total;}
        static float WingWeight(BoneWeight value,Transform[] bones)
        {float total=0;var indices=new[]{value.boneIndex0,value.boneIndex1,value.boneIndex2,value.boneIndex3};var weights=new[]{value.weight0,value.weight1,value.weight2,value.weight3};for(int index=0;index<4;index++)if(weights[index]>0&&indices[index]<bones.Length&&bones[indices[index]]!=null){string name=bones[indices[index]].name;if(name.StartsWith("RigLWing",StringComparison.Ordinal)||name.StartsWith("RigRWing",StringComparison.Ordinal))total+=weights[index];}return total;}
        static Influence SkinInfluence(int vertex,Vector3 position,BoneWeight weight,Transform[] bones)=>new Influence{vertex=vertex,cpuExpectedModelPosition=position,bones=new[]{bones[weight.boneIndex0]?.name,bones[weight.boneIndex1]?.name,bones[weight.boneIndex2]?.name,bones[weight.boneIndex3]?.name},weights=new[]{weight.weight0,weight.weight1,weight.weight2,weight.weight3}};
        static SkinningContract ProbeSkinningContract(Transform model,Transform bone,SkinnedMeshRenderer skin,Vector3[] falseBake,Vector3[] trueBake,bool includeUnitScale=true)
        {
            var result=new SkinningContract();
            try
            {
                // Diagnostic reference skinning uses the actual imported vertex,
                // bindpose, bone matrices and weights. It produces no saved mesh
                // and is never a runtime skinning or terrain implementation.
                Vector3[] source;using(var snapshot=MeshUtility.AcquireReadOnlyMeshData(skin.sharedMesh))
                {using(var values=new Unity.Collections.NativeArray<Vector3>(snapshot[0].vertexCount,Unity.Collections.Allocator.Temp)){snapshot[0].GetVertices(values);source=values.ToArray();}}
                var weights=skin.sharedMesh.boneWeights;var bindposes=skin.sharedMesh.bindposes;var bones=skin.bones;result.sourceVertices=source.Length;result.weights=weights.Length;result.bindposes=bindposes.Length;result.bones=bones.Length;
                if(weights.Length!=source.Length)throw new InvalidOperationException("Actual legacy4 imported skin weight count differs from vertex count; record rather than guess.");
                var expected=new Vector3[source.Length];var inverse=model.worldToLocalMatrix;
                for(int index=0;index<source.Length;index++)
                {
                    var weight=weights[index];var indices=new[]{weight.boneIndex0,weight.boneIndex1,weight.boneIndex2,weight.boneIndex3};var amounts=new[]{weight.weight0,weight.weight1,weight.weight2,weight.weight3};Vector3 world=Vector3.zero;
                    for(int slot=0;slot<4;slot++)if(amounts[slot]>0){if(indices[slot]>=bones.Length||indices[slot]>=bindposes.Length||bones[indices[slot]]==null)throw new InvalidOperationException("Actual weighted bone reference missing.");world+=bones[indices[slot]].localToWorldMatrix.MultiplyPoint3x4(bindposes[indices[slot]].MultiplyPoint3x4(source[index]))*amounts[slot];}
                    expected[index]=inverse.MultiplyPoint3x4(world);
                }
                result.cpuExpectedModelBounds=BoundsOf(expected);var full=inverse*skin.transform.localToWorldMatrix;var scaleFree=inverse*Matrix4x4.TRS(skin.transform.position,skin.transform.rotation,Vector3.one);
                result.falseFullMatrixRmse=Rmse(expected,falseBake.Select(full.MultiplyPoint3x4).ToArray());result.falseScaleFreeRmse=Rmse(expected,falseBake.Select(scaleFree.MultiplyPoint3x4).ToArray());result.trueFullMatrixRmse=Rmse(expected,trueBake.Select(full.MultiplyPoint3x4).ToArray());result.trueScaleFreeRmse=Rmse(expected,trueBake.Select(scaleFree.MultiplyPoint3x4).ToArray());
                if(includeUnitScale)
                {
                    Vector3 oldScale=model.localScale;var unitMesh=new Mesh();
                    try{model.localScale=Vector3.one;skin.BakeMesh(unitMesh,false);var unitMatrix=model.worldToLocalMatrix*skin.transform.localToWorldMatrix;var unit=unitMesh.vertices.Select(unitMatrix.MultiplyPoint3x4).ToArray();result.unitScaleModelBounds=BoundsOf(unit);result.unitScaleBoneModel=model.InverseTransformPoint(bone.position);result.unitScaleVsCpuRmse=Rmse(expected,unit);}
                    finally{model.localScale=oldScale;UnityEngine.Object.DestroyImmediate(unitMesh);}
                }
                Vector3 anchor=model.InverseTransformPoint(bone.position);result.nearestBackVertices=Enumerable.Range(0,expected.Length).OrderBy(index=>(expected[index]-anchor).sqrMagnitude).Take(16).Select(index=>
                {var weight=weights[index];return new Influence{vertex=index,cpuExpectedModelPosition=expected[index],bones=new[]{bones[weight.boneIndex0]?.name,bones[weight.boneIndex1]?.name,bones[weight.boneIndex2]?.name,bones[weight.boneIndex3]?.name},weights=new[]{weight.weight0,weight.weight1,weight.weight2,weight.weight3}};}).ToArray();
            }
            catch(Exception error){result.error=error.ToString();}
            return result;
        }
        static float Rmse(Vector3[] expected,Vector3[] actual){if(expected.Length!=actual.Length)return -1;double squares=0;for(int i=0;i<expected.Length;i++)squares+=(expected[i]-actual[i]).sqrMagnitude;return (float)Math.Sqrt(squares/expected.Length);}
        static void Install(DragonCharacter actor,SportsEventTruck truck,DragonSpeciesDefinition species,Calibration calibration)
        {
            ConfigureRiderHover(actor,species);
            var bone=actor.motor.GetComponentsInChildren<Transform>(true).Single(value=>value.name==species.saddleBone);var frame=actor.GetComponent<DragonRiderSaddle>()??actor.gameObject.AddComponent<DragonRiderSaddle>();
            var socket=bone.Find(DragonRiderSaddle.SocketName);if(socket==null){socket=new GameObject(DragonRiderSaddle.SocketName).transform;socket.SetParent(bone,false);}socket.SetLocalPositionAndRotation(calibration.position,calibration.rotation);socket.localScale=Vector3.one;
            var support=actor.transform.Find(PresentationName);if(support==null){support=new GameObject(PresentationName).transform;support.SetParent(actor.transform,false);}support.SetPositionAndRotation(actor.motor.transform.TransformPoint(calibration.supportMotorLocalPosition),actor.motor.transform.rotation);
            var deck=actor.GetComponentsInChildren<Transform>(true).Single(value=>value.name==DeckName);deck.SetParent(support,true);deck.rotation=support.rotation;
            var renderers=deck.GetComponentsInChildren<Renderer>(true);var bounds=DragonKingdomWorldAuthoring.MeasureOwnedModelBounds(deck);deck.position+=support.position-new Vector3(bounds.center.x,bounds.max.y,bounds.center.z);
            foreach(var renderer in renderers)
            {
                var materials=renderer.sharedMaterials;for(int i=0;i<materials.Length;i++)
                {var source=materials[i];if(source==null)throw new InvalidOperationException("Actual timber saddle material missing.");if(source.shader!=null&&source.shader.name.StartsWith("Universal Render Pipeline/",StringComparison.Ordinal))continue;
                    string folder="Assets/TruckDragonRiders/Materials";Directory.CreateDirectory(folder);AssetDatabase.Refresh();string path=folder+"/Saddle "+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+" URP.mat";materials[i]=DragonKingdomWorldVisualMigration.ConfigureOwnedMaterial(source,path,false,out _);AssetDatabase.SaveAssetIfDirty(materials[i]);}
                renderer.sharedMaterials=materials;
            }
            frame.actor=actor;frame.sourceBone=bone;frame.socket=socket;frame.supportPresentation=support;frame.neutralClip=calibration.clip;frame.speciesId=species.id;frame.calibrationVersion=7;frame.surfaceRenderer=actor.motor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(value=>value.sharedMesh!=null&&AssetDatabase.GetAssetPath(value.sharedMesh)==calibration.anchorMesh);frame.surfaceVertexA=calibration.anchorA;frame.surfaceVertexB=calibration.anchorB;frame.surfaceVertexC=calibration.anchorC;frame.surfaceBarycentric=calibration.anchorBarycentric;frame.surfacePatchAnchors=calibration.patchAnchors;frame.supportMotorLocalPosition=calibration.supportMotorLocalPosition;frame.measuredDorsalMinimum=calibration.dorsalMinimum;frame.measuredDorsalMaximum=calibration.dorsalMaximum;frame.sampledSkinSurfaces=calibration.surfaces;frame.dorsalClearance=.45f;frame.maximumBank=25;frame.maximumPitch=20;frame.maximumYaw=15;frame.Validate(actor);
            try{frame.CaptureEvaluatedAnimationPose();var actual=frame.ReadPose();support.SetPositionAndRotation(actual.position,actual.rotation);}
            finally{frame.ReleaseGeometrySnapshot();}
            EditorUtility.SetDirty(frame);
        }
        static string PhysicsSignature(DragonRidersMatch match)=>string.Join("|",match.trucks.SelectMany(truck=>truck.GetComponentsInChildren<Rigidbody>(true).Select(body=>body.GetEntityId()+":"+body.mass+":"+body.isKinematic+":"+body.useGravity+":"+body.constraints+":"+body.collisionDetectionMode+":"+body.interpolation)).Concat(match.trucks.SelectMany(truck=>truck.GetComponentsInChildren<Collider>(true).Select(shape=>shape.GetEntityId()+":"+shape.enabled+":"+shape.isTrigger+":"+shape.sharedMaterial))));
        static void Watch(string path,Dictionary<string,string> values){if(string.IsNullOrEmpty(path)||!File.Exists(path))return;values[path]=Hash(path);if(File.Exists(path+".meta"))values[path+".meta"]=Hash(path+".meta");}
        static string Hash(string path){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(@"\\?\"+Path.GetFullPath(path)))).Replace("-","");}
    }
}
#endif
