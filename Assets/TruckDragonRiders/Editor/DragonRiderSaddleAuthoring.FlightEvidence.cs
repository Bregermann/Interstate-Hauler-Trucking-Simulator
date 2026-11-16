#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        [Serializable]sealed class FlightEvidenceRequest{public string infernoGeometry,fireGeometry,cameras,breath;}
        [Serializable]sealed class FlightSourceHash{public string path,sha256;}
        [Serializable]sealed class FlightSourceRows{public FlightSourceHash[] rows;}
        [Serializable]sealed class FlightEvidenceReceipt{public string scope="EXACT_POSITIVE_NATIVE_PROFILE_GEOMETRY_REAL_PUBLIC_REMATCH_CAB_SEATING_AND_ORIGINAL_BREATH_PREREQUISITES_NOT_SAVED_BUILD_OR_HUMAN_ACCEPTANCE";public FlightSourceHash[] artifacts;}
        [Serializable]sealed class FlightFitReceipt{public bool assetsExact,originalNwhExact,satControlsPassed,cachedBodyOracleControlsPassed;public FlightFitProfile[] profiles;}
        [Serializable]sealed class FlightFitProfile{public string species;public float sourceRootFlapFraction;public bool fits;public FlightFitPose[] poses;}
        [Serializable]sealed class FlightFitPose{public string failure;public int fullSkinBoxIntersections;}
        [Serializable]sealed class FlightCameraReceipt{public FlightCameraView[] views;}
        [Serializable]sealed class FlightCameraView{public bool exactOwnedStatePreserved;public string actualBaseClips,actualWheelDeckSeating;public FlightCameraGeometry[] pureConnectedNeckCandidates;}
        [Serializable]sealed class FlightCameraGeometry{public int actualTrueBakeTriangleBoxIntersections,triangleBoxIntersections;public FlightCameraRays[] sameFrameCabRays;}
        [Serializable]sealed class FlightCameraRays{public string name;public int ownHeadNeckDominantHits;public FlightCameraRay[] centralWindscreenRays;}
        [Serializable]sealed class FlightCameraRay{public Vector2 viewport;public bool ownDragon;public float headNeckWeight;}
        [Serializable]sealed class FlightSeatingReceipt{public string context;public FlightSeatingWheel[] wheels;}
        [Serializable]sealed class FlightSeatingWheel{public bool actualVisualFound,deckFaceFound;public float actualGap;}
        static readonly string[] FlightRecipeInputs={"Assets/TruckDragonRiders/Editor/DragonRiderSaddleAuthoring.TransientCab.cs","Assets/TruckDragonRiders/Editor/DragonRiderSaddleAuthoring.AnimationCoverage.cs","Assets/TruckDragonRiders/Scripts/DragonRiderSaddle.cs","Assets/TruckDragonRiders/Scripts/DragonRiderSaddleRigging.cs"};
        public static string InspectSavedNormalFlightAcceptanceOnly(string descriptor)=>JsonUtility.ToJson(RequirePositiveFlightEvidence(descriptor),true);
        static FlightEvidenceReceipt RequirePositiveFlightEvidence(string descriptor=null)
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsRiderFlightEvidence");if(descriptor==null&&(at<0||at+1>=args.Length))throw new InvalidOperationException("Explicit -tsRiderFlightEvidence JSON with genuine positive geometry/camera/Breath cohorts is required; source existence is not acceptance.");
            string requestPath=descriptor??args[at+1];var request=JsonUtility.FromJson<FlightEvidenceRequest>(File.ReadAllText(requestPath));var facts=new Dictionary<string,string>(StringComparer.Ordinal);Track(requestPath);
            RequireCase(request.infernoGeometry,"DragonRiderSaddleAuthoringTests.ReadOnlyInfernoConnectedOriginalNeckFamilyEnvelopeAndActualBodyPatchMeasuresAllSourceFrames");RequireProfile(request.infernoGeometry,"dragon-inferno",.25f,615);
            RequireCase(request.fireGeometry,"DragonRiderSaddleAuthoringTests.ReadOnlyOriginalMotionFlightEnvelopeAndActualPureTorsoSupportsMeasureAllSourceFramesAndLoopMixers");RequireProfile(request.fireGeometry,"dragon-fire",.5f,607);
            RequireCase(request.cameras,"NativeDragonInputTests.RidersTransientMixedEnvelopeActuallyAnimatesBothCabsAfterPublicRematch");
            RequireFrozen(request.cameras,new[]{"Assets/TruckDragonKingdom/Tests/Native/NativeDragonInputTests.cs","Assets/TruckDragonRiders/Tests/Native/DragonRiderCabSightlineObservation.cs"}.Concat(FlightRecipeInputs));
            RequireCase(request.breath,"DragonAnimalMotionNativeTests.RidersQuarterEnvelopeInfernoRestoresOriginalBreathFrontHitAndRejectsSideTruck");RequireCase(request.breath,"DragonAnimalMotionNativeTests.RidersHalfEnvelopeFireRestoresOriginalBreathFrontHitAndRejectsSideTruck");RequireFrozen(request.breath,new[]{"Assets/TruckDragonKingdom/Tests/Native/DragonAnimalMotionNativeTests.cs"}.Concat(FlightRecipeInputs));
            foreach(int height in new[]{720,1080})
            {
                string leaf="humans2-view2-aiFalse-"+(height==720?1280:1920)+"x"+height;string folder=Path.Combine(request.cameras,"rendered-evidence","InputOwnership","RidersMixedEnvelope","Candidate");string path=Path.Combine(folder,leaf+".actual-skin-sightlines.json");var camera=JsonUtility.FromJson<FlightCameraReceipt>(File.ReadAllText(path));Track(path);if(camera.views==null||camera.views.Length!=2)throw new InvalidOperationException("Both genuine human camera receipts required.");
                foreach(var view in camera.views)
                {
                    if(!view.exactOwnedStatePreserved||view.actualBaseClips==null||!view.actualBaseClips.Contains("TRANSIENT_CAB_"))throw new InvalidOperationException("Actual native playback and immutable observation are mandatory.");var skin=view.pureConnectedNeckCandidates[0];var rays=skin.sameFrameCabRays.Single(value=>value.name=="ACTUAL_CAMERA_ROUND_TRIP_CONTROL");var center=rays.centralWindscreenRays.Single(value=>(value.viewport-new Vector2(.5f,.5f)).sqrMagnitude<.000001f);if(skin.actualTrueBakeTriangleBoxIntersections!=0||skin.triangleBoxIntersections!=0||rays.ownHeadNeckDominantHits>5||center.ownDragon&&center.headNeckWeight>=.5f)throw new InvalidOperationException("Actual clear cab/full skin acceptance failed.");var seating=JsonUtility.FromJson<FlightSeatingReceipt>(view.actualWheelDeckSeating);if(seating.context!="ACTUAL_NATIVE_PLAYMODE_ORIGINAL_PARTICIPANT"||seating.wheels.Length!=4||seating.wheels.Any(wheel=>!wheel.actualVisualFound||!wheel.deckFaceFound||Mathf.Abs(wheel.actualGap)>.03f))throw new InvalidOperationException("Actual original four-wheel/vendor-timber contact evidence failed.");
                }
                foreach(int view in new[]{1,2}){string png=Path.Combine(folder,"humans2-view"+view+"-aiFalse-"+(height==720?1280:1920)+"x"+height+".png");if(new FileInfo(png).Length<2000)throw new InvalidOperationException("Actual candidate image missing.");Track(png);}
            }
            return new FlightEvidenceReceipt{artifacts=facts.Select(pair=>new FlightSourceHash{path=Path.GetFullPath(pair.Key),sha256=pair.Value}).ToArray()};
            void Track(string path){if(string.IsNullOrEmpty(path)||!File.Exists(path))throw new InvalidOperationException("Missing exact acceptance artifact: "+path);facts[path]=Hash(path);}
            void RequireCase(string folder,string test){string xml=Path.Combine(folder,"tests.xml");var rows=XDocument.Load(xml).Descendants("test-case").Where(value=>(string)value.Attribute("fullname")==test).ToArray();if(rows.Length!=1||(string)rows[0].Attribute("result")!="Passed")throw new InvalidOperationException("Positive actual native case required: "+test+" / "+folder);Track(xml);}
            void RequireProfile(string folder,string species,float flap,int count){string path=Path.Combine(folder,"rendered-evidence","RiderSaddleAuthoring","SourceMotionEnvelopeSweep","actual-flight-profile-sweep.json");var report=JsonUtility.FromJson<FlightFitReceipt>(File.ReadAllText(path));var profile=report.profiles.Single(value=>value.species==species&&value.sourceRootFlapFraction==flap);if(!report.assetsExact||!report.originalNwhExact||!report.satControlsPassed||!report.cachedBodyOracleControlsPassed||!profile.fits||profile.poses.Length!=count||profile.poses.Any(value=>!string.IsNullOrEmpty(value.failure)||value.fullSkinBoxIntersections!=0))throw new InvalidOperationException("Complete measured source-profile geometry required for "+species);Track(path);RequireFrozen(folder,species=="dragon-fire"?FlightRecipeInputs.Where(value=>!value.EndsWith(".TransientCab.cs",StringComparison.Ordinal)):FlightRecipeInputs);}
            void RequireFrozen(string folder,IEnumerable<string> paths){string manifest=Path.Combine(folder,"input-manifest.json");var rows=JsonUtility.FromJson<FlightSourceRows>("{\"rows\":"+File.ReadAllText(manifest)+"}").rows.ToDictionary(value=>value.path,value=>value.sha256,StringComparer.Ordinal);foreach(string path in paths){if(!rows.TryGetValue(path,out string expected)||!File.Exists(path)||!string.Equals(Hash(path),expected,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Native acceptance no longer matches current source: "+path+" / "+folder);}Track(manifest);}
        }
    }
}
#endif
