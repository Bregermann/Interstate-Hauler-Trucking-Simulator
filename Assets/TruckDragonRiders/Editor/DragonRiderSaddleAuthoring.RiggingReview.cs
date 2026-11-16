#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckDragonKingdom;
using LWS.TruckSports;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        [Serializable]sealed class RiggingReview
        {
            public string scope="READ_ONLY_SAVED_SCHEMA7_ACTUAL_VENDOR_TIMBER_AND_TEXTURED_HARNESS_RENDER_REVIEW_NOT_WINDOWS_OR_HUMAN_ACCEPTANCE";public bool assetsExact;public RiggingView[] poses;
        }
        [Serializable]sealed class RiggingView{public string species,pose;public int calibrationVersion,presentationVersion;public bool geometryPreserved,noNewColliders,tiesVisible;public DeckWheelSeating seating;public string[] images;}
        public static string InspectSavedRiggingWithoutSavingAssets()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required for saved visual review.");for(int index=0;index<SceneManager.sceneCount;index++)if(SceneManager.GetSceneAt(index).isDirty)throw new InvalidOperationException("Preserve unsaved scenes before saved visual review.");
            string scenePath=DragonRidersAuthoring.ScenePath,catalogPath="Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset";var catalog=AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>(catalogPath);if(catalog==null)throw new InvalidOperationException("Actual species catalog required.");
            var watched=AssetDatabase.GetDependencies(scenePath,true).Where(path=>path.StartsWith("Assets/",StringComparison.Ordinal)&&File.Exists(path)).SelectMany(path=>new[]{path,path+".meta"}).Distinct().ToDictionary(path=>path,Hash);var owned=Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories).ToDictionary(path=>path,Hash);
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1>=args.Length)throw new InvalidOperationException("Explicit native visual-review output required.");string output=Path.Combine(args[at+1],"RiderSaddleAuthoring","SavedRiggingReview");Directory.CreateDirectory(output);var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;var report=new RiggingReview();var rows=new List<RiggingView>();
            try
            {
                scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);var match=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<DragonRidersMatch>(true)).Single();
                foreach(string speciesId in new[]{"dragon-inferno","dragon-fire"})
                {
                    if(!catalog.TryGet(speciesId,out var species))throw new InvalidOperationException("Actual configured saved mount required.");var saved=match.racers[0].choices.Select(actor=>actor.GetComponent<DragonRiderSaddle>()).Single(candidateFrame=>candidateFrame.speciesId==speciesId);if(saved.calibrationVersion!=7||saved.rigging==null||saved.rigging.presentationVersion!=1)throw new InvalidOperationException("Retain accepted schema7 and explicit presentation1 before saved rendered review.");
                    var preview=new PreviewRenderUtility();DragonRiderSaddle frame=null;
                    try
                    {
                        var actorObject=UnityEngine.Object.Instantiate(saved.actor.gameObject);preview.AddSingleGO(actorObject);actorObject.SetActive(true);var actor=actorObject.GetComponent<DragonCharacter>();actor.motor.animator.enabled=false;frame=actorObject.GetComponent<DragonRiderSaddle>();
                        var truckObject=UnityEngine.Object.Instantiate(match.trucks[0].gameObject);preview.AddSingleGO(truckObject);truckObject.SetActive(true);var truck=truckObject.GetComponent<SportsEventTruck>();frame.rigging.truck=truck;foreach(var body in actorObject.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.detectCollisions=false;}foreach(var body in truckObject.GetComponentsInChildren<Rigidbody>(true))body.isKinematic=true;
                        var original=OriginalFootprints(new[]{match.trucks[0]}).Single();var originalPoses=actor.motor.GetComponentsInChildren<Transform>(true).Select(value=>new BonePose{bone=value,position=value.localPosition,rotation=value.localRotation}).ToArray();
                        if(!species.TryGetAnimation(DragonAnimationAction.FlyIdle,out var hover)||!species.TryGetAnimation(DragonAnimationAction.Breath,out var breath))throw new InvalidOperationException("Actual saved hover/Breath mappings required.");var trials=new[]{new MountedPoseTrial{name="Saved neutral quarter-cycle",clip=EffectiveRiderClip(actor,hover.clip),fraction=.25f},speciesId=="dragon-fire"?ActualV203FireFlightTrial():new MountedPoseTrial{name="Saved mapped Breath contact phase",clip=EffectiveRiderClip(actor,breath.clip),fraction=.45f}};
                        preview.ambientColor=new Color(.35f,.38f,.43f,1);preview.lights[0].intensity=1.3f;preview.lights[0].transform.rotation=Quaternion.Euler(40,35,0);preview.lights[1].intensity=.8f;preview.lights[1].transform.rotation=Quaternion.Euler(310,210,0);preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.09f,.12f,.16f,1);preview.camera.nearClipPlane=.1f;preview.camera.farClipPlane=800;
                        for(int poseIndex=0;poseIndex<trials.Length;poseIndex++)
                        {
                            foreach(var pose in originalPoses)pose.Restore();SampleMountedTrial(actor,trials[poseIndex]);frame.CaptureEvaluatedAnimationPose();var physical=frame.ReadPose();truck.transform.SetPositionAndRotation(physical.position+physical.rotation*Vector3.up*original.support,physical.rotation);frame.rigging.RefreshAuthoredPresentation(true);
                            var row=new RiggingView{species=speciesId,pose=trials[poseIndex].name,calibrationVersion=frame.calibrationVersion,presentationVersion=frame.rigging.presentationVersion,geometryPreserved=truck.shape.size==match.trucks[0].shape.size&&truck.shape.center==match.trucks[0].shape.center&&truck.transform.lossyScale==match.trucks[0].transform.lossyScale&&truck.GetComponentsInChildren<Rigidbody>(true).Sum(value=>value.mass)==match.trucks[0].GetComponentsInChildren<Rigidbody>(true).Sum(value=>value.mass),noNewColliders=frame.supportPresentation.GetComponentsInChildren<Collider>(true).Length==0&&frame.rigging.GetComponentsInChildren<Collider>(true).Length==0,tiesVisible=frame.rigging.UpperTiesVisible,seating=MeasureActualDeckSeating(truck,frame.supportPresentation,physical.rotation*Vector3.up,"READ_ONLY_SAVED_FITTING_ACTUAL_CONTACT_NOT_LIVE_SUSPENSION_ACCEPTANCE")};
                            rows.Add(row);
                            if(!row.geometryPreserved||!row.noNewColliders||!row.tiesVisible||row.seating.wheels.Any(wheel=>!wheel.actualVisualFound||!wheel.deckFaceFound||Mathf.Abs(wheel.actualGap)>.03f))throw new InvalidOperationException("Saved real fitting is missing, detached or fails actual wheel/wood contact; rendering cannot substitute for integration.");
                            var full=ActualRenderedGeometryBounds(actorObject);var truckBounds=ActualRenderedGeometryBounds(truckObject);full.Encapsulate(truckBounds);var images=new List<string>();
                            foreach(int height in new[]{720,1080})
                            {
                                int width=height==720?1280:1920;preview.camera.orthographic=false;preview.camera.fieldOfView=38;string all=Path.Combine(output,speciesId+"-pose"+poseIndex+"-full-"+height+".png");CaptureInterference(preview,full.center,new Vector3(-1,.55f,1),Mathf.Max(8,full.extents.magnitude)*2.8f,all,width,height);images.Add(all);preview.camera.orthographic=true;preview.camera.orthographicSize=Mathf.Max(3,truckBounds.extents.magnitude)*1.4f;string close=Path.Combine(output,speciesId+"-pose"+poseIndex+"-actual-truck-deck-girth-"+height+".png");CaptureInterference(preview,truckBounds.center-physical.rotation*Vector3.up*.7f,new Vector3(-1,.6f,1),Mathf.Max(12,full.extents.magnitude*4),close,width,height);images.Add(close);
                            }
                            row.images=images.ToArray();
                        }
                    }
                    finally{if(frame!=null)frame.ReleaseGeometrySnapshot();preview.Cleanup();}
                }
                report.poses=rows.ToArray();
            }
            finally
            {
                if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);var after=Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories);report.assetsExact=owned.Keys.OrderBy(path=>path,StringComparer.Ordinal).SequenceEqual(after.OrderBy(path=>path,StringComparer.Ordinal))&&owned.All(pair=>Hash(pair.Key)==pair.Value)&&watched.All(pair=>Hash(pair.Key)==pair.Value);report.poses=rows.ToArray();try{File.WriteAllText(Path.Combine(output,"actual-saved-rigging-review.json"),JsonUtility.ToJson(report,true));}catch(Exception error){Debug.LogWarning("Could not retain saved rigging observation: "+error.Message);}if(!report.assetsExact)throw new InvalidOperationException("Read-only saved visual review changed original source or saved assets.");
            }
            string json=JsonUtility.ToJson(report,true);File.WriteAllText(Path.Combine(output,"actual-saved-rigging-review.json"),json);return json;
        }
    }
}
#endif
