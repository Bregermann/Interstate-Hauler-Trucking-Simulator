#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        sealed class ProvenInfernoMaterial{public string folder;public float rearward;public SurfaceProof[] points;}
        static ProvenInfernoMaterial RequireNativeProvenInfernoMaterial(string folder)
        {
            var manifest=RequireRiderPassedManifest(folder,"DragonRiderInfernoFitTests.QualifiedInfernoAll699FullAssemblyEnvelopeAndOriginalWingRootContactsAreMeasuredWithoutAdoptingOversize");
            var report=JsonUtility.FromJson<InfernoFitReport>(File.ReadAllText(Directory.GetFiles(Path.Combine(folder,"rendered-evidence"),"all699-envelope-and-original-anatomical-fit.json",SearchOption.AllDirectories).Single()));
            if(!report.sourceBytesExact||!report.sceneBytesExact||!report.nwhExact||!report.qualifiedNativeGeometry||report.poses!=699||!report.derivedStrictPatch||!string.IsNullOrEmpty(report.calibrationFailure)||report.measuredPatch==null||report.measuredPatch.Length!=5)throw new InvalidOperationException("The actual unique native source-derived full-baseline strict torso patch must have passed its existing60-surface calibration without any source or NWH change.");
            var interval=report.neutralStrictTorsoCorridors.Single(value=>value.center==report.derivedRearward);if(report.derivedRearward>=0||report.derivedRearward<interval.minimum||report.derivedRearward>interval.maximum)throw new InvalidOperationException("Native source-derived center must remain inside its measured bilateral corridor.");
            // Keep BOTH original authoring input and genuinely measured native
            // importer identities. No fresh metadata/source normalization.
            var originalBaselines=new System.Collections.Generic.List<RiderNativeSourceBaseline>();RequireActualInfernoSizeProof(report.nativeEvidence,originalBaselines);
            if(originalBaselines.Count!=report.actualNativeSourceBaselines.Count||originalBaselines.Any(original=>!report.actualNativeSourceBaselines.Any(baseline=>baseline.path==original.path&&baseline.prelaunchSha256==original.prelaunchSha256&&baseline.actualNativeSha256==original.actualNativeSha256&&baseline.hostInputSha256==original.hostInputSha256&&baseline.currentNativeSha256==original.currentNativeSha256&&baseline.agreeingPassedNativeProfiles==original.agreeingPassedNativeProfiles)))throw new InvalidOperationException("Native fit report must retain the exact independently captured two-profile source provenance; no synthetic or duplicated baseline.");
            RequireRecordedStartupSourceHash(manifest,DragonRidersAuthoring.ScenePath,"Native Inferno material whole-scene identity");
            foreach(string path in new[]{"Assets/TruckDragonRiders/Editor/DragonRiderSaddleAuthoring.InfernoFit.cs","Assets/TruckDragonRiders/Tests/Authoring/DragonRiderInfernoFitTests.cs"})RequireRecordedStartupSourceHash(manifest,path,"Native Inferno material exact producer identity");
            return new ProvenInfernoMaterial{folder=folder,rearward=report.derivedRearward,points=report.measuredPatch};
        }
        static void ApplyNativeProvenInfernoMaterial(DragonRiderSaddle frame,ProvenInfernoMaterial material)
        {
            var points=material.points;if(frame.speciesId!="dragon-inferno"||points.Any(point=>point.vertexInfluences==null||point.vertexInfluences.Length!=3||point.mesh!=AssetDatabase.GetAssetPath(frame.surfaceRenderer.sharedMesh)||point.torsoSkinWeight<=.5f||point.wingDominated||point.barycentric.x<0||point.barycentric.y<0||point.barycentric.z<0||Mathf.Abs(point.barycentric.x+point.barycentric.y+point.barycentric.z-1)>.0001f||point.upwardNormal<.1f||point.cpuSkinningRmse>=.0001f))throw new InvalidOperationException("Exact original mesh and five upward strict-majority anatomical material points required.");
            var anchors=points.Select(point=>new DragonRiderSurfaceAnchor{a=point.vertexInfluences[0].vertex,b=point.vertexInfluences[1].vertex,c=point.vertexInfluences[2].vertex,barycentric=point.barycentric}).ToArray();var calibration=CaptureFrameCalibration(frame);calibration.anchorA=anchors[0].a;calibration.anchorB=anchors[0].b;calibration.anchorC=anchors[0].c;calibration.anchorBarycentric=anchors[0].barycentric;calibration.patchAnchors=anchors;ApplyFrameCalibration(frame,calibration);
        }
        public static string InspectPreciseInfernoNativeMaterialMountedSizeWithoutSavingAssets(string materialEvidence,string startupEvidence)=>InspectMountedSizeProfiles(true,true,false,RequireProvenActualInfernoStartup(startupEvidence),null,RequireNativeProvenInfernoMaterial(materialEvidence));
    }
}
#endif
