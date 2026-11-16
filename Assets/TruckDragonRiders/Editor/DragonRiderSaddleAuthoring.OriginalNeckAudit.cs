#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckDragonKingdom;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        [Serializable]sealed class OriginalNeckAudit
        {public string scope="READ_ONLY_TRANSIENT_NATIVE_ORIGINAL_AND_EXISTING_CONFIGURED_FLIGHT_NECK_POSES_NO_ASSET_SAVES_NOT_MOUNT_OR_CAB_ACCEPTANCE";public bool assetsExact;public int originalImportedClips,configuredFlightClips;public List<OriginalNeckPose> poses=new List<OriginalNeckPose>();}
        [Serializable]sealed class OriginalNeckPose
        {public string species,clip,clipGuid,clipLocalId,provenance,configuredActions;public float phase,clipLength;public Vector3 headRelativeToSaddleBoneModel,mouthRelativeToSaddleBoneModel,mouthForwardModel,headForwardModel;public Bounds headDominantModelBounds,headDominantSaddleRelativeBounds;public int headDominantVertices;public OriginalNeckJoint[] connectedJoints;}
        [Serializable]sealed class OriginalNeckJoint
        {public string name,parent;public Vector3 modelPosition,localPosition;public Quaternion localRotation;}

        public static string InspectOwnedOriginalFlightNecksWithoutSavingAssets()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required for original flight-motion audit.");
            var catalog=AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>("Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset");if(catalog==null)throw new InvalidOperationException("Actual configured species catalog required.");
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1>=args.Length)throw new InvalidOperationException("Explicit retained native evidence folder required.");string folder=Path.Combine(args[at+1],"RiderSaddleAuthoring","OriginalFlightNeckAudit");Directory.CreateDirectory(folder);
            var report=new OriginalNeckAudit();var watched=new Dictionary<string,string>();var owned=Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories).ToDictionary(path=>path,Hash);
            try
            {
                foreach(string id in new[]{"dragon-inferno","dragon-fire"})
                {
                    if(!catalog.TryGet(id,out var species))throw new InvalidOperationException("Actual rideable source species missing.");
                    string sourceFolder="Assets/TruckDragonKingdom/PrivateLicensedDragons/Monsters Ultimate Pack 02 Cute Series/"+(id=="dragon-inferno"?"Dragon Inferno Cute Series":"Dragon Fire Cute Series")+"/FBX";
                    var imported=Directory.GetFiles(sourceFolder,"*",SearchOption.AllDirectories).Where(path=>path.EndsWith(".fbx",StringComparison.OrdinalIgnoreCase)&&Path.GetFileName(path).IndexOf("Fly",StringComparison.OrdinalIgnoreCase)>=0).Select(path=>path.Replace('\\','/')).SelectMany(path=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(clip=>!clip.name.StartsWith("__preview__",StringComparison.Ordinal))).Distinct().ToArray();
                    if(imported.Length==0)throw new InvalidOperationException("No actual original imported flight clips found; do not silently substitute authored motions.");
                    var configured=species.animations.Where(mapping=>mapping!=null&&mapping.clip!=null&&new[]{"Fly","FlyIdle","Glide","BankLeft","BankRight","Dive","TurnLeft","TurnRight","Takeoff","Land"}.Contains(mapping.action.ToString())).Select(mapping=>mapping.clip).Distinct().ToArray();report.originalImportedClips+=imported.Length;report.configuredFlightClips+=configured.Length;
                    foreach(string file in AssetDatabase.GetDependencies(ActorPath(id),true).Concat(imported.Concat(configured).Select(AssetDatabase.GetAssetPath)).Distinct().Where(path=>path.StartsWith("Assets/",StringComparison.Ordinal)&&File.Exists(path)))foreach(string path in new[]{file,file+".meta"})if(File.Exists(path)&&!watched.ContainsKey(path))watched.Add(path,Hash(path));
                    var loaded=PrefabUtility.LoadPrefabContents(ActorPath(id));
                    try
                    {
                        var actor=loaded.GetComponent<DragonCharacter>();var model=actor.motor.transform;actor.motor.animator.enabled=false;var joints=model.GetComponentsInChildren<Transform>(true);var saved=joints.Select(joint=>new SavedJoint{joint=joint,position=joint.localPosition,rotation=joint.localRotation,scale=joint.localScale}).ToArray();var saddleBone=joints.Single(value=>value.name==species.saddleBone);var head=joints.Single(value=>value.name=="RigHead");
                        foreach(var clip in imported.Concat(configured).Distinct())foreach(float phase in new[]{0f,.125f,.25f,.375f,.5f,.625f,.75f,.875f,.98f})
                        {
                            foreach(var pose in saved)pose.Restore();clip.SampleAnimation(model.gameObject,clip.length*phase);var origin=model.InverseTransformPoint(saddleBone.position);var neck=joints.Where(value=>value.name.StartsWith("RigNeck",StringComparison.Ordinal)||value==head).ToArray();
                            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(clip,out string guid,out long localId);var row=new OriginalNeckPose{species=id,clip=AssetDatabase.GetAssetPath(clip)+"/"+clip.name,clipGuid=guid,clipLocalId=localId.ToString(),provenance=imported.Contains(clip)?"ORIGINAL_LICENSED_IMPORTED_FLIGHT":"EXISTING_CONFIGURED_AUTHORED_FLIGHT",configuredActions=string.Join("/",species.animations.Where(mapping=>mapping!=null&&mapping.clip==clip).Select(mapping=>mapping.action.ToString())),phase=phase,clipLength=clip.length,headRelativeToSaddleBoneModel=model.InverseTransformPoint(head.position)-origin,mouthRelativeToSaddleBoneModel=model.InverseTransformPoint(actor.mouth.position)-origin,mouthForwardModel=model.InverseTransformDirection(actor.mouth.forward),headForwardModel=model.InverseTransformDirection(head.forward),connectedJoints=neck.Select(value=>new OriginalNeckJoint{name=value.name,parent=value.parent.name,modelPosition=model.InverseTransformPoint(value.position),localPosition=value.localPosition,localRotation=value.localRotation}).ToArray()};
                            bool first=true;foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                            {
                                var weights=skin.sharedMesh.boneWeights;var bones=skin.bones;var mesh=new Mesh();try{skin.BakeMesh(mesh,true);var points=mesh.vertices;for(int index=0;index<points.Length;index++){var w=weights[index];float headWeight=0;Add(w.boneIndex0,w.weight0);Add(w.boneIndex1,w.weight1);Add(w.boneIndex2,w.weight2);Add(w.boneIndex3,w.weight3);if(headWeight<.5f)continue;var point=model.InverseTransformPoint(skin.transform.TransformPoint(points[index]));if(first){row.headDominantModelBounds=new Bounds(point,Vector3.zero);row.headDominantSaddleRelativeBounds=new Bounds(point-origin,Vector3.zero);first=false;}else{row.headDominantModelBounds.Encapsulate(point);row.headDominantSaddleRelativeBounds.Encapsulate(point-origin);}row.headDominantVertices++;void Add(int bone,float amount){if(amount>0&&bones[bone]!=null&&(bones[bone]==head||bones[bone].IsChildOf(head)))headWeight+=amount;}}}finally{UnityEngine.Object.DestroyImmediate(mesh);}
                            }
                            if(row.headDominantVertices==0)throw new InvalidOperationException("Original native flight audit has no actual head-weighted skin.");report.poses.Add(row);
                        }
                    }
                    finally{PrefabUtility.UnloadPrefabContents(loaded);}
                }
            }
            finally
            {
                var after=Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories);report.assetsExact=owned.Keys.OrderBy(path=>path,StringComparer.Ordinal).SequenceEqual(after.OrderBy(path=>path,StringComparer.Ordinal))&&owned.All(pair=>Hash(pair.Key)==pair.Value)&&watched.All(pair=>File.Exists(pair.Key)&&Hash(pair.Key)==pair.Value);File.WriteAllText(Path.Combine(folder,"original-flight-neck-poses.json"),JsonUtility.ToJson(report,true));if(!report.assetsExact)throw new InvalidOperationException("Read-only original flight audit changed source or saved assets.");
            }
            return JsonUtility.ToJson(report,true);
        }
    }
}
#endif
