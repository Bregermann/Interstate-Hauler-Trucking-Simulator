#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LWS.TruckDragonKingdom;
using UnityEditor;
using UnityEngine;
namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        static string explicitFireSupportedPostEvidence;
        public static string InspectAndExportFireSupportedPostsAfterFresh537Geometry(string geometryEvidence,string headExitEvidence,string folder,string planningEvidence)
        {
            if(explicitFireSupportedPostEvidence!=null||coldFixtureExport!=null||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("One explicit stopped Fire supported-post candidate only.");
            RequireRiderPassedManifest(planningEvidence,"DragonRiderFireCabPlanningTests.ActualFireFrameComparesWholeSupportedPostHeightsAndHeadFlexionWithoutChangingSourceOrFiveSkinAnchors");
            var proof=JsonUtility.FromJson<FireCabPlanningReport>(File.ReadAllText(Directory.GetFiles(Path.Combine(planningEvidence,"rendered-evidence"),"actual-frame-supported-post-and-head-planning.json",SearchOption.AllDirectories).Single()));
            var control=proof.rows.Single(r=>r.cameraBasis=="BODY_FRAME_EYE_AND_ORIENTATION_SCALED_LOCAL_OFFSET"&&r.extraHeadDegrees==0&&r.extraPostWorldMeters==0);
            var proposed=proof.rows.Single(r=>r.cameraBasis=="BODY_FRAME_EYE_AND_ORIENTATION_SCALED_LOCAL_OFFSET"&&r.extraHeadDegrees==0&&r.extraPostWorldMeters==5);
            if(!string.IsNullOrEmpty(proof.failure)||!proof.sourcesExact||!proof.fixtureAssetsExact||!proof.originalSceneExact||!proof.baselineRaysExact||!proof.baselineAnchorsExact||!proof.originalNwhDependencyBytesExact||proof.originalDorsalClearance!=.45f||proof.fullSkinVertices!=2064||proof.fullSkinTriangles!=2698||Hash(proof.input)!=proof.inputSha256||!control.centerHead||control.headHits!=13||!proposed.meetsRayPlanningThreshold||proposed.centerHead||proposed.headHits>5||proposed.fullSkinBoxPairs!=0||!proposed.sourceSkinUnchanged||!proposed.fiveAnchorsInvariant||proposed.maximumAnchorDisplacement>.0001f)throw new InvalidOperationException("Exact actual Fire frame and supported5-world-metre-post planning authority required; this is not full537 or native cab acceptance.");
            try{explicitFireSupportedPostEvidence=planningEvidence;return InspectAndExportTestOnlyMotorRootFireAfterFresh537Geometry(geometryEvidence,headExitEvidence,folder);}finally{explicitFireSupportedPostEvidence=null;}
        }
        static void ApplyExplicitFireSupportedPosts(DragonCharacter actor,MountedSizeProfile row)
        {
            if(explicitFireSupportedPostEvidence==null)return;if(row.species!="dragon-fire"||coldFixtureExport==null)throw new InvalidOperationException("Supported-post candidate cannot affect another species or normal authoring.");
            var frame=actor.GetComponent<DragonRiderSaddle>();if(frame.dorsalClearance!=.45f||frame.surfacePatchAnchors.Length!=5||frame.rigging==null||frame.supportPresentation==null)throw new InvalidOperationException("Exact original five skin anchors and actual timber/deck rig required.");
            row.supportedPostEvidence=explicitFireSupportedPostEvidence;row.supportedPostOriginalClearance=frame.dorsalClearance;row.supportedPostExtraWorldMeters=5;frame.dorsalClearance+=5;row.supportedPostCandidateClearance=frame.dorsalClearance;
            // Move the complete mounted truck/deck through the existing material
            // frame. Existing timber legs connect the same five skin anchors.
            // Full actual537 skin/SAT/wheel-contact proof follows this change.
        }
    }
}
#endif
