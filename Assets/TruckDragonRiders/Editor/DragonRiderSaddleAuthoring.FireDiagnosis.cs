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
        [Serializable]sealed class FireHoverAimProfile
        {
            public string scope="READ_ONLY_ORIGINAL_HOVER_VS_SAVED_MOUNTED_HOVER_VS_ORIGINAL_FORWARD_CONNECTED_MUZZLE_PROFILE_NOT_CONTACT_OR_MATCH_ACCEPTANCE",sourceHover,ownedHover,sourceForward,actualNeckName;
            public bool sourceAndSavedAssetsExact;
            public float maximumOriginalToMountedAimError;
            public FireHoverAimRow[] rows;
            public string[] images;
        }
        [Serializable]sealed class FireHoverAimRow
        {
            public float phase,originalHoverAngle,mountedHoverAngle,sourceForwardAngle,originalToMountedAimError;
            public Vector3 originalHoverDirection,mountedHoverDirection,sourceForwardDirection;
            public Quaternion originalNeckLocalRotation,mountedNeckLocalRotation,sourceForwardNeckLocalRotation,originalHeadLocalRotation,mountedHeadLocalRotation,sourceForwardHeadLocalRotation;
        }
        public static string InspectSavedFireHoverMuzzleProfileWithoutSavingAssets()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required for read-only Fire source posture profile.");
            string actorPath=ActorPath("dragon-fire"),catalogPath="Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset";
            var catalog=AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>(catalogPath);if(catalog==null||!catalog.TryGet("dragon-fire",out var species)||!species.TryGetAnimation(DragonAnimationAction.FlyIdle,out var hover)||!species.TryGetAnimation(DragonAnimationAction.Fly,out var forward))throw new InvalidOperationException("Actual original Fire hover/forward sources required.");
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(FireHoverControllerPath);var mounted=controller!=null?controller[hover.clip]:null;if(mounted==null||mounted==hover.clip)throw new InvalidOperationException("Actual saved selected Riders hover required; diagnosis cannot generate it.");
            var watched=AssetDatabase.GetDependencies(actorPath,true).Concat(new[]{catalogPath,DragonRidersAuthoring.ScenePath,FireHoverControllerPath,AssetDatabase.GetAssetPath(mounted),AssetDatabase.GetAssetPath(hover.clip),AssetDatabase.GetAssetPath(forward.clip)}).Where(path=>path.StartsWith("Assets/",StringComparison.Ordinal)&&File.Exists(path)).SelectMany(path=>new[]{path,path+".meta"}).Distinct().ToDictionary(path=>path,Hash);
            var modeFiles=Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories).ToDictionary(path=>path,Hash);
            var profile=new FireHoverAimProfile{sourceHover=AssetDatabase.GetAssetPath(hover.clip),ownedHover=AssetDatabase.GetAssetPath(mounted),sourceForward=AssetDatabase.GetAssetPath(forward.clip)};GameObject root=null;
            try
            {
                root=PrefabUtility.LoadPrefabContents(actorPath);var actor=root.GetComponent<DragonCharacter>();actor.motor.animator.enabled=false;var model=actor.motor.transform;var head=model.GetComponentsInChildren<Transform>(true).Single(value=>value.name=="RigHead");var neck=head.parent;profile.actualNeckName=neck.name;
                if(actor.mouth==null||!actor.mouth.IsChildOf(head))throw new InvalidOperationException("Actual configured connected head/muzzle required; no replacement socket or root rotation.");
                var saved=model.GetComponentsInChildren<Transform>(true).Select(joint=>new SavedJoint{joint=joint,position=joint.localPosition,rotation=joint.localRotation,scale=joint.localScale}).ToArray();var rows=new List<FireHoverAimRow>();
                foreach(float phase in Enumerable.Range(0,121).Select(index=>index/120f).Concat(new[]{.604f}).Distinct().OrderBy(value=>value))
                {
                    var row=new FireHoverAimRow{phase=phase};
                    foreach(var pose in saved)pose.Restore();hover.clip.SampleAnimation(model.gameObject,hover.clip.length*phase);row.originalHoverDirection=model.InverseTransformDirection(actor.mouth.forward);row.originalHoverAngle=Vector3.Angle(actor.mouth.forward,model.forward);row.originalNeckLocalRotation=neck.localRotation;row.originalHeadLocalRotation=head.localRotation;
                    foreach(var pose in saved)pose.Restore();mounted.SampleAnimation(model.gameObject,mounted.length*phase);row.mountedHoverDirection=model.InverseTransformDirection(actor.mouth.forward);row.mountedHoverAngle=Vector3.Angle(actor.mouth.forward,model.forward);row.mountedNeckLocalRotation=neck.localRotation;row.mountedHeadLocalRotation=head.localRotation;
                    foreach(var pose in saved)pose.Restore();forward.clip.SampleAnimation(model.gameObject,forward.clip.length*phase);row.sourceForwardDirection=model.InverseTransformDirection(actor.mouth.forward);row.sourceForwardAngle=Vector3.Angle(actor.mouth.forward,model.forward);row.sourceForwardNeckLocalRotation=neck.localRotation;row.sourceForwardHeadLocalRotation=head.localRotation;
                    row.originalToMountedAimError=Vector3.Angle(row.originalHoverDirection,row.mountedHoverDirection);profile.maximumOriginalToMountedAimError=Mathf.Max(profile.maximumOriginalToMountedAimError,row.originalToMountedAimError);rows.Add(row);
                }
                profile.rows=rows.ToArray();foreach(var pose in saved)pose.Restore();
                var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1>=args.Length)throw new InvalidOperationException("Explicit retained native profile output required.");string output=Path.Combine(args[at+1],"RiderSaddleAuthoring","FireHoverAim");Directory.CreateDirectory(output);
                var preview=new PreviewRenderUtility();GameObject clone=null;
                try
                {
                    clone=UnityEngine.Object.Instantiate(root);preview.AddSingleGO(clone);clone.SetActive(true);var cloneActor=clone.GetComponent<DragonCharacter>();var cloneModel=cloneActor.motor.transform;cloneActor.motor.animator.enabled=false;
                    var cloneSaved=cloneModel.GetComponentsInChildren<Transform>(true).Select(joint=>new SavedJoint{joint=joint,position=joint.localPosition,rotation=joint.localRotation,scale=joint.localScale}).ToArray();
                    var clips=new[]{hover.clip,mounted,forward.clip};Bounds union=default;bool first=true;
                    foreach(var clip in clips){foreach(var pose in cloneSaved)pose.Restore();clip.SampleAnimation(cloneModel.gameObject,clip.length*.604f);var actual=ActualRenderedGeometryBounds(clone);if(first){union=actual;first=false;}else union.Encapsulate(actual);}
                    preview.ambientColor=new Color(.35f,.38f,.43f,1);preview.lights[0].intensity=1.3f;preview.lights[0].transform.rotation=Quaternion.Euler(40,35,0);preview.lights[1].intensity=.8f;preview.lights[1].transform.rotation=Quaternion.Euler(310,210,0);preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.09f,.12f,.16f,1);preview.camera.fieldOfView=38;preview.camera.nearClipPlane=.1f;preview.camera.farClipPlane=500;
                    profile.images=new string[3];
                    for(int index=0;index<clips.Length;index++){foreach(var pose in cloneSaved)pose.Restore();clips[index].SampleAnimation(cloneModel.gameObject,clips[index].length*.604f);string name=index==0?"original-hover":index==1?"saved-mounted-hover":"original-forward";profile.images[index]=Path.Combine(output,name+"-phase-0.604.png");CaptureInterference(preview,union.center,new Vector3(-1,.15f,1),Mathf.Max(8,union.extents.magnitude)*2.8f,profile.images[index]);}
                }
                finally{preview.Cleanup();}
            }
            finally
            {
                if(root!=null)PrefabUtility.UnloadPrefabContents(root);
                var after=Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories);profile.sourceAndSavedAssetsExact=modeFiles.Keys.OrderBy(path=>path,StringComparer.Ordinal).SequenceEqual(after.OrderBy(path=>path,StringComparer.Ordinal))&&modeFiles.All(pair=>Hash(pair.Key)==pair.Value)&&watched.All(pair=>Hash(pair.Key)==pair.Value);
                if(!profile.sourceAndSavedAssetsExact)throw new InvalidOperationException("Read-only Fire source profile modified original or saved assets.");
            }
            string json=JsonUtility.ToJson(profile,true);var arguments=Environment.GetCommandLineArgs();int outputIndex=Array.IndexOf(arguments,"-tsDragonOutput");File.WriteAllText(Path.Combine(arguments[outputIndex+1],"RiderSaddleAuthoring","fire-hover-source-aim-profile.json"),json);return json;
        }
    }
}
#endif
