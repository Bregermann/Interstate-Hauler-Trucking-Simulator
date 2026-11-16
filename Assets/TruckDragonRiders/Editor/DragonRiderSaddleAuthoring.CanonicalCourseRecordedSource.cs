#if UNITY_EDITOR && VISTA
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
        // This is recorded source metadata after a separate full current
        // canonical certificate. It does not claim new pose replay or the
        // old candidate/source comparison lifecycle was repeated here.
        static QualifiedCourseSource CourseRecordedSourceMetadata(Scene scene,string productionEvidence,string physicalEvidence,CourseMountSourceAuthority authority)
        {
            RequireProductionCourseMountAuthority(authority);
            if(!scene.IsValid()||!scene.isLoaded||scene.isDirty||scene.path!=authority.proof.target||!CourseCertificateSame(Hash(scene.path),authority.proof.currentSceneSha256))throw new InvalidOperationException("Only the exact clean course-certified canonical source may expose recorded geometry metadata.");
            var production=ReadQualifiedCourseProduction(productionEvidence);var match=CourseMatch(scene);
            var result=new QualifiedCourseSource{scene=scene.path,sceneSha256=Hash(scene.path),sourceScene=scene.path,productionEvidence=productionEvidence,physicalCourseEvidence=physicalEvidence,productionReceiptSha256=Hash(Path.Combine(productionEvidence,"rendered-evidence","RiderSaddleAuthoring","ProductionCandidate","qualified-mounted-scene.json")),scope="EXACT_CANONICAL_MOUNT_AND_PHYSICAL_COURSE_CERTIFICATE_REUSES_RECORDED699_537_NATIVE_GEOMETRY_NO_NEW_POSE_REPLAY_OR_COMPARISON_SCENE",currentConfigurationExact=authority.proof.currentExternalConfigurationExact,currentProfilesExact=authority.proof.currentProfilesExact,currentNwhExact=authority.proof.currentNwhExact,unitAncestors=authority.proof.unitAncestors,noTestDependencies=authority.proof.noTestDependencies,maximumNwhDimensionDrift=authority.proof.maximumNwhDimensionDrift,straightExitDistance=match.aiStraightGateExitDistance};
            result.rootStates=scene.GetRootGameObjects().Select((root,index)=>new QualifiedCourseRootState{index=index,name=root.name,active=root.activeSelf}).ToArray();result.rootActivationExact=true;
            var dependencies=AssetDatabase.GetDependencies(scene.path,true).Select(CurrentRiderPhysicalDependency).Concat(new[]{scene.path,scene.path+".meta",authority.proof.receipt,Path.Combine(productionEvidence,"tests.xml")}).Distinct().ToArray();result.currentSourceSnapshot=dependencies.Select(path=>new QualifiedCourseSourceFile{path=path,sha256=Hash(path)}).ToArray();
            for(int species=0;species<2;species++)
            {
                var reports=Directory.GetFiles(Path.Combine(production.producerEvidence[species],"rendered-evidence"),"precise-motor-root-*-course-envelope.json",SearchOption.AllDirectories).Select(path=>JsonUtility.FromJson<MountedSizeReport>(File.ReadAllText(path)));var native=reports.Single(report=>report.testOnlyColdFixture?.species==production.species[species]);var row=native.profiles.Single();var envelope=row.actualCourseEnvelope;int expected=production.species[species]=="dragon-fire"?537:699;
                if(!native.sourceBytesExact||!native.sceneBytesExact||!native.nwhExact||row.actualPoses!=expected||row.candidateSatPairs!=0||!row.exactOriginalBoxAcceptance||envelope==null||!envelope.hasBounds||envelope.poses!=expected||envelope.crossingCenterAuthority!="CURRENT_AUTHORED_BOX_CENTER_TRANSFORMED_BY_CURRENT_LOCAL_TO_WORLD_NOT_PHYSX_CACHE"||envelope.crossingCenters==null||envelope.crossingCenters.Count!=expected||envelope.crossingCenters.Any(sample=>string.IsNullOrEmpty(sample.pose)||Vector3.Distance(sample.currentLocalToWorld.MultiplyPoint3x4(sample.authoredLocalCenter),sample.currentMatrixCenter)>.0001f)||envelope.nwhMeshPoints==0||envelope.nwhBoxCorners==0||envelope.harnessLineSamples==0)throw new InvalidOperationException("The complete original native box-centered geometry witness changed; no inferred or fresh pose count.");
                var labels=envelope.crossingCenters.Select(sample=>sample.pose).ToArray();int mixers=labels.Count(label=>label.IndexOf("blend",StringComparison.OrdinalIgnoreCase)>=0||label.IndexOf("two-layer",StringComparison.OrdinalIgnoreCase)>=0);if(mixers==0)throw new InvalidOperationException("Recorded native source mixer coverage must remain present.");
                Vector3 origin;Quaternion orientation;var prefab=PrefabUtility.LoadPrefabContents(native.testOnlyColdFixture.prefab);try{var actor=prefab.GetComponent<DragonCharacter>();origin=actor.motor.transform.position;orientation=actor.motor.transform.rotation;}finally{PrefabUtility.UnloadPrefabContents(prefab);}
                var controller=AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(production.controllers[species]);var overrides=new List<KeyValuePair<AnimationClip,AnimationClip>>();controller.GetOverrides(overrides);var clips=overrides.Select(pair=>pair.Value==null?pair.Key:pair.Value).Distinct().Select(AssetDatabase.GetAssetPath).ToArray();
                for(int slot=0;slot<4;slot++)result.rigs.Add(new QualifiedCourseRigSource{crew=slot,species=production.species[species],recordedSourcePoses=expected,skinPoints=envelope.skinPoints,recordedMixerLabelledPoses=mixers,headingBounds=envelope.actorHeadingRelativeBounds,anyAttitudeRadius=envelope.maximumAnyAttitudeVertexRadius,neutralCrossingOffset=Quaternion.Inverse(orientation)*(envelope.crossingCenters[0].currentMatrixCenter-origin),controller=production.controllers[species],controllerSha256=Hash(production.controllers[species]),clips=clips,clipSha256=clips.Select(Hash).ToArray(),poseLabels=labels,envelopeAuthority=envelope.crossingCenterAuthority});
            }
            result.currentSourceBytesExact=result.currentSourceSnapshot.All(row=>CourseCertificateSame(Hash(row.path),row.sha256));if(result.rigs.Count!=8||result.straightExitDistance!=55||!result.currentSourceBytesExact||!CourseCertificateSame(Hash(scene.path),authority.proof.currentSceneSha256))throw new InvalidOperationException("Current canonical recorded metadata/source bytes changed.");result.status="CURRENT_CANONICAL_COURSE_SOURCE_CERTIFIED_WITH_RECORDED_GEOMETRY_NOT_NEW_REPLAY";return result;
        }
    }
}
#endif
