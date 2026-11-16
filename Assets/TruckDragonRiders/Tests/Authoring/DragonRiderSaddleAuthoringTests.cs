#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DragonRiderSaddleAuthoringTests
{
    const string Path="Assets/TruckDragonRiders/Scenes/TruckDragonRiders.unity";
    static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    [Serializable]sealed class CabSweepReadOnly{public string scope="";public bool assetsExact=false,originalNwhExact=false,satControlsPassed=false,cachedBodyOracleControlsPassed=false;public int cachedBodyOracleComparisons=0;public CabSweepProfileReadOnly[] profiles=Array.Empty<CabSweepProfileReadOnly>();}
    [Serializable]sealed class CabSweepProfileReadOnly{public string species="";public float wingDihedral=0,sourceRootFlapFraction=0;public bool stableTorsoAnchorPrototype=false,fits=false;public int sourceFrames=0,mixedPoses=0,nativeThreeLeafPoses=0;public CabSweepPoseReadOnly[] poses=Array.Empty<CabSweepPoseReadOnly>();}
    [Serializable]sealed class CabSweepPoseReadOnly{public string failure="";public int actualCompleteTriangles=0,fullSkinBoxIntersections=0;public float maximumSupportDisplacement=0;}
    [Test,Timeout(360000)]
    public void ReadOnlyActualAuthoredFlightSourceFramesMixersAndWingDihedralMeasureCompleteOriginalTruckClearance()
    {
        var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);string json=(string)author.GetMethod("InspectTransientCabFlightSourceFramesWithoutSavingAssets").Invoke(null,null);var report=JsonUtility.FromJson<CabSweepReadOnly>(json);Assert.That(report.assetsExact&&report.originalNwhExact&&report.satControlsPassed&&report.cachedBodyOracleControlsPassed,Is.True);Assert.That(report.cachedBodyOracleComparisons,Is.GreaterThan(100));Assert.That(report.profiles.Length,Is.EqualTo(6));foreach(var profile in report.profiles){Assert.That(profile.sourceFrames,Is.GreaterThan(100));Assert.That(profile.mixedPoses,Is.EqualTo(200));Assert.That(profile.nativeThreeLeafPoses,Is.EqualTo(profile.species=="dragon-fire"?6:0));Assert.That(profile.poses.Length,Is.EqualTo(profile.sourceFrames+profile.mixedPoses+profile.nativeThreeLeafPoses));Assert.That(profile.poses.All(pose=>string.IsNullOrEmpty(pose.failure)&&pose.actualCompleteTriangles>2500),Is.True,"Every counted source/mixer pose must actually reach native skin measurement; candidate geometric rejection is recorded separately.");TestContext.WriteLine("TRANSIENT_FULL_FLIGHT_PROFILE species="+profile.species+" wing="+profile.wingDihedral+" poses="+profile.poses.Length+" geometryFits="+profile.fits+" intersectionPoses="+profile.poses.Count(pose=>pose.fullSkinBoxIntersections>0)+" movedSupportPoses="+profile.poses.Count(pose=>pose.maximumSupportDisplacement>.0002f));}Assert.That(report.scope,Does.Contain("NOT_LIVE_OR_PRODUCTION_ACCEPTANCE"));
    }
    [Test,Timeout(360000)]
    public void ReadOnlyOriginalMotionFlightEnvelopeAndActualPureTorsoSupportsMeasureAllSourceFramesAndLoopMixers()
    {
        var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);string json=(string)author.GetMethod("InspectTransientSourceMotionEnvelopeWithoutSavingAssets").Invoke(null,null);var report=JsonUtility.FromJson<CabSweepReadOnly>(json);Assert.That(report.assetsExact&&report.originalNwhExact&&report.satControlsPassed&&report.cachedBodyOracleControlsPassed,Is.True);Assert.That(report.cachedBodyOracleComparisons,Is.GreaterThan(100));Assert.That(report.profiles.Length,Is.EqualTo(4));foreach(var profile in report.profiles){Assert.That(profile.sourceRootFlapFraction,Is.EqualTo(.25f).Or.EqualTo(.5f));Assert.That(profile.stableTorsoAnchorPrototype,Is.False,"Actual centered original skin bindings remain the sole saddle authority.");Assert.That(profile.sourceFrames,Is.GreaterThan(100));Assert.That(profile.mixedPoses,Is.EqualTo(330));Assert.That(profile.nativeThreeLeafPoses,Is.EqualTo(profile.species=="dragon-fire"?6:0));Assert.That(profile.poses.Length,Is.EqualTo(profile.sourceFrames+profile.mixedPoses+profile.nativeThreeLeafPoses));Assert.That(profile.poses.All(pose=>string.IsNullOrEmpty(pose.failure)&&pose.actualCompleteTriangles>2500),Is.True,"Every original-motion envelope pose must actually reach complete native geometry; unsupported candidates remain rejected.");TestContext.WriteLine("ACTUAL_SOURCE_MOTION_ENVELOPE species="+profile.species+" retainedRootFlap="+profile.sourceRootFlapFraction+" poses="+profile.poses.Length+" fullGeometryFits="+profile.fits+" intersectionPoses="+profile.poses.Count(pose=>pose.fullSkinBoxIntersections>0)+" movedActualBodySupportPoses="+profile.poses.Count(pose=>pose.maximumSupportDisplacement>.0002f));}Assert.That(report.scope,Does.Contain("NOT_LIVE_OR_PRODUCTION_ACCEPTANCE"));
    }
    [Test,Timeout(240000)]
    public void ReadOnlyInfernoConnectedOriginalNeckFamilyEnvelopeAndActualBodyPatchMeasuresAllSourceFrames()
    {
        var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);string json=(string)author.GetMethod("InspectTransientInfernoConnectedFamilyEnvelopeWithoutSavingAssets").Invoke(null,null);var report=JsonUtility.FromJson<CabSweepReadOnly>(json);Assert.That(report.assetsExact&&report.originalNwhExact&&report.satControlsPassed&&report.cachedBodyOracleControlsPassed,Is.True);Assert.That(report.profiles.Length,Is.EqualTo(2));foreach(var profile in report.profiles){Assert.That(profile.species,Is.EqualTo("dragon-inferno"));Assert.That(profile.sourceFrames,Is.EqualTo(285));Assert.That(profile.mixedPoses,Is.EqualTo(330));Assert.That(profile.nativeThreeLeafPoses,Is.Zero);Assert.That(profile.poses.Length,Is.EqualTo(615));Assert.That(profile.poses.All(pose=>string.IsNullOrEmpty(pose.failure)&&pose.actualCompleteTriangles>2500),Is.True);TestContext.WriteLine("ACTUAL_INFERNO_CONNECTED_NECK_FAMILY_ENVELOPE sourceRootFlap="+profile.sourceRootFlapFraction+" geometryFits="+profile.fits+" actualSATPoses="+profile.poses.Count(pose=>pose.fullSkinBoxIntersections>0)+" movedPureTorsoSupportPoses="+profile.poses.Count(pose=>pose.maximumSupportDisplacement>.0002f));}Assert.That(report.scope,Does.Contain("NOT_LIVE_OR_PRODUCTION_ACCEPTANCE"));
    }
    [Serializable]sealed class OriginalFlightNeckReadOnly{public string scope="";public bool assetsExact=false;public int originalImportedClips=0,configuredFlightClips=0;public OriginalFlightNeckPoseReadOnly[] poses=Array.Empty<OriginalFlightNeckPoseReadOnly>();}
    [Serializable]sealed class OriginalFlightNeckPoseReadOnly{public string species="",clip="",clipGuid="",clipLocalId="",provenance="";public float phase=0;public int headDominantVertices=0;public Vector3 headRelativeToSaddleBoneModel=Vector3.zero;public Bounds headDominantSaddleRelativeBounds=new Bounds();}
    [Test,Timeout(180000)]
    public void ReadOnlyAllOwnedOriginalAndConfiguredFlightClipsExposeActualConnectedNeckSkinWithoutSavingAssets()
    {
        var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);string json=(string)author.GetMethod("InspectOwnedOriginalFlightNecksWithoutSavingAssets").Invoke(null,null);var report=JsonUtility.FromJson<OriginalFlightNeckReadOnly>(json);Assert.That(report.assetsExact,Is.True);Assert.That(report.scope,Does.Contain("NOT_MOUNT_OR_CAB_ACCEPTANCE"));Assert.That(report.originalImportedClips,Is.GreaterThan(10));Assert.That(report.configuredFlightClips,Is.GreaterThan(0));Assert.That(report.poses.Select(row=>row.species).Distinct().Count(),Is.EqualTo(2));foreach(var group in report.poses.GroupBy(row=>row.species+"/"+row.clipGuid+"/"+row.clipLocalId)){Assert.That(group.Count(),Is.EqualTo(9));Assert.That(group.All(row=>row.headDominantVertices>0),Is.True);Assert.That(group.Any(row=>row.phase==0)&&group.Any(row=>row.phase==.98f),Is.True);}TestContext.WriteLine("ORIGINAL_FLIGHT_NECK_AUDIT originalImported="+report.originalImportedClips+" configured="+report.configuredFlightClips+" actualNativeHeadSkinPoses="+report.poses.Length+" sourceAndSavedAssetsExact=true NO_MOUNT_OR_CAB_ACCEPTANCE");
    }
    [Serializable]sealed class CenteredReadOnly
    {public string scope="";public bool actualNeutralPatchCentered=false,sourceAndSavedAssetsExact=false;public float measuredNeutralRearward=0,measuredNeutralLateral=0;public ReadOnlyFootprint footprint=new ReadOnlyFootprint();public ReadOnlyTriangle[] retainedNeutralAnchorProofs=Array.Empty<ReadOnlyTriangle>(),retainedLowTorsoNeckProofs=Array.Empty<ReadOnlyTriangle>();public InterferenceReadOnly[] actualInterference=Array.Empty<InterferenceReadOnly>();}
    [Serializable]sealed class ReadOnlyFootprint
    {public string scope="";public bool fits=false;public int originalCrews=0,uniqueFootprints=0,violatingRayProofs=0;public ReadOnlyPose[] poses=Array.Empty<ReadOnlyPose>();}
    [Serializable]sealed class ReadOnlyPose{public ReadOnlyPoint[] points=Array.Empty<ReadOnlyPoint>();public int actualTailTrianglesTested=0,actualTailBoxIntersections=0;public string[] actualConfiguredLeafPaths=Array.Empty<string>();public float[] actualConfiguredLeafWeights=Array.Empty<float>();}
    [Serializable]sealed class ReadOnlyPoint{public bool torso=false;public float gap=0;public ReadOnlyTriangle actualPenetratingTriangle=new ReadOnlyTriangle();}
    [Serializable]sealed class ReadOnlyTriangle
    {public string mesh="";public Vector3 barycentric=Vector3.zero;public float torsoSkinWeight=0,wingSkinWeight=0,upwardNormal=0,cpuSkinningRmse=0;public bool wingDominated=false;public ReadOnlyInfluence[] vertexInfluences=Array.Empty<ReadOnlyInfluence>();}
    [Serializable]sealed class ReadOnlyInfluence{public int vertex=0;public string[] bones=Array.Empty<string>();public float[] weights=Array.Empty<float>();}
    [Serializable]sealed class InterferenceReadOnly
    {public string pose="";public bool originalGeometryPreserved=false,actualColliderQueryControlsPassed=false;public int originalBodies=0,clonedBodies=0,originalSolidColliders=0,clonedSolidColliders=0,renderers=0,rawWingRays=0,rawWingPointsInsideOriginalColliders=0;public float originalMass=0,clonedMass=0,insideControlDistance=0,outsideControlDistance=0;public InterferenceRayReadOnly[] rays=Array.Empty<InterferenceRayReadOnly>();public string[] images=Array.Empty<string>();public DeckSeatingReadOnly actualWheelDeckSeating=new DeckSeatingReadOnly();}
    [Serializable]sealed class InterferenceRayReadOnly
    {public bool rawFound=false,rawIsMajorityWing=false,insideAnyOriginalSolidCollider=false;public float rawGap=0;public ReadOnlyTriangle rawProof=new ReadOnlyTriangle();public ClosestReadOnly[] actualColliderClosestPoints=Array.Empty<ClosestReadOnly>();}
    [Serializable]sealed class ClosestReadOnly{public string type="";public bool enabled=false,active=false,pointInsideOrOnBoundary=false;public float distance=0;}
    [Serializable]sealed class DeckSeatingReadOnly{public int visibleDeckMeshes=0,inactiveDeckMeshes=0;public string[] actualDeckSources=Array.Empty<string>();public DeckWheelReadOnly[] wheels=Array.Empty<DeckWheelReadOnly>();public float visibleDeckTopOffset=0,allIncludingInactiveDeckTopOffset=0;}
    [Serializable]sealed class DeckWheelReadOnly{public bool actualVisualFound=false,deckFaceFound=false;public int sampledVisibleVertices=0;public float actualGap=0,theoreticalDroopToActualWheelBottom=0;}
    [Serializable]sealed class HoverProfileReadOnly{public string scope="",actualNeckName="";public bool sourceAndSavedAssetsExact=false;public HoverProfileRowReadOnly[] rows=Array.Empty<HoverProfileRowReadOnly>();public string[] images=Array.Empty<string>();public float maximumOriginalToMountedAimError=0;}
    [Serializable]sealed class HoverProfileRowReadOnly{public float phase=0,originalHoverAngle=0,mountedHoverAngle=0,sourceForwardAngle=0,originalToMountedAimError=0;}
    [Serializable]sealed class FlightReplayReadOnly{public string scope="";public bool assetsExact=false,weightsMatchObserved=false,colliderControlsPassed=false,tailSatControlsPassed=false,candidateHoverV3=false;public int actualTailTrianglesTested=0,actualTailBoxIntersections=0,diagnosticWingTrianglesHidden=0;public float normalizedTime=0,minimumSkinGap=0;public FlightReplayRayReadOnly[] rays=Array.Empty<FlightReplayRayReadOnly>();public string[] images=Array.Empty<string>();public HoverRecipeReadOnly candidateRecipe=new HoverRecipeReadOnly();}
    [Serializable]sealed class HoverRecipeReadOnly{public string recipe="";public bool neckTranslationsUnchanged=false,tailRootTranslationsUnchanged=false,worldAimVerified=false;public float maximumWorldAimError=0,maximumReferenceTailRootRotationError=0;}
    [Serializable]sealed class RiggingReviewReadOnly{public string scope="";public bool assetsExact=false;public RiggingViewReadOnly[] poses=Array.Empty<RiggingViewReadOnly>();}
    [Serializable]sealed class RiggingViewReadOnly{public string species="";public int calibrationVersion=0,presentationVersion=0;public bool geometryPreserved=false,noNewColliders=false,tiesVisible=false;public DeckSeatingReadOnly seating=new DeckSeatingReadOnly();public string[] images=Array.Empty<string>();}
    [Serializable]sealed class FiveTailReadOnly{public string scope="";public bool assetsExact=false;public ReadOnlyFootprint footprint=new ReadOnlyFootprint();public HoverRecipeReadOnly[] recipes=Array.Empty<HoverRecipeReadOnly>();}
    [Test,Timeout(180000)]
    public void ReadOnlyFiveProvenFireTailRootRepairsClearAllActualSourceAndNativeMixedPosesWithoutSavingAssets()
    {
        var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);string json=(string)author.GetMethod("InspectFiveFireTailRepairsWithoutSavingAssets").Invoke(null,null);var report=JsonUtility.FromJson<FiveTailReadOnly>(json);Assert.That(report.assetsExact,Is.True);Assert.That(report.scope,Does.Contain("NOT_SAVED_AUTHORING_OR_MATCH_ACCEPTANCE"));Assert.That(report.footprint.fits,Is.True);Assert.That(report.footprint.poses.Length,Is.GreaterThanOrEqualTo(326));Assert.That(report.recipes.Length,Is.EqualTo(6));
        foreach(var pose in report.footprint.poses){Assert.That(pose.actualTailTrianglesTested,Is.GreaterThan(0));Assert.That(pose.actualTailBoxIntersections,Is.Zero);}Assert.That(report.footprint.poses.Count(pose=>pose.actualConfiguredLeafWeights!=null&&pose.actualConfiguredLeafWeights.Length==3),Is.EqualTo(6));
        foreach(var recipe in report.recipes){Assert.That(recipe.tailRootTranslationsUnchanged,Is.True);Assert.That(recipe.maximumReferenceTailRootRotationError,Is.LessThanOrEqualTo(.5f));if(recipe.worldAimVerified)Assert.That(recipe.maximumWorldAimError,Is.LessThanOrEqualTo(.5f));}
        TestContext.WriteLine("READ_ONLY_FIVE_FIRE_TAIL_ROOT_REPAIRS actualAllPoses="+report.footprint.poses.Length+" nativeThreeLeafPhases=6 allTailTriangleBoxIntersections=0 sourceAndSavedBytesExact=true NOT_SAVED_AUTHORING_OR_MATCH_ACCEPTANCE");
    }
    [Test,Timeout(180000)]
    public void ReadOnlySavedCompleteRiggingRendersBothSpeciesAndMeasuresActualWheelSeatsWithoutSavingAssets()
    {
        var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);string json=(string)author.GetMethod("InspectSavedRiggingWithoutSavingAssets").Invoke(null,null);var report=JsonUtility.FromJson<RiggingReviewReadOnly>(json);Assert.That(report.assetsExact,Is.True);Assert.That(report.scope,Does.Contain("NOT_WINDOWS_OR_HUMAN_ACCEPTANCE"));Assert.That(report.poses.Length,Is.EqualTo(4));Assert.That(report.poses.Select(row=>row.species).Distinct().Count(),Is.EqualTo(2));
        foreach(var row in report.poses){Assert.That(row.calibrationVersion,Is.EqualTo(7));Assert.That(row.presentationVersion,Is.EqualTo(1));Assert.That(row.geometryPreserved&&row.noNewColliders&&row.tiesVisible,Is.True);Assert.That(row.seating.wheels.Length,Is.EqualTo(4));foreach(var wheel in row.seating.wheels){Assert.That(wheel.actualVisualFound&&wheel.deckFaceFound,Is.True);Assert.That(Mathf.Abs(wheel.actualGap),Is.LessThanOrEqualTo(.03f));}Assert.That(row.images.Length,Is.EqualTo(4));foreach(string image in row.images){Assert.That(File.Exists(image),Is.True);Assert.That(new FileInfo(image).Length,Is.GreaterThan(2000));}}
        TestContext.WriteLine("READ_ONLY_SAVED_RIGGING_RENDER_REVIEW species=2 poses=4 frames=16 at720p_and1080p actualWheelTimberFaces=true assetsExact=true NO_WINDOWS_OR_HUMAN_ACCEPTANCE");
    }
    [Serializable]sealed class FlightReplayRayReadOnly{public int index=0;public bool found=false;public float gap=0;public ReadOnlyTriangle actualTriangle=new ReadOnlyTriangle();public ClosestReadOnly[] originalColliderQueries=Array.Empty<ClosestReadOnly>();}
    [Test,Timeout(120000)]
    public void CandidateFireHoverV3PreservesConnectedAimAndClearsActualNativeThreeLeafTailWithoutSavingAssets()
    {
        var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);string json=(string)author.GetMethod("InspectCandidateFireHoverV3WithoutSavingAssets").Invoke(null,null);var report=JsonUtility.FromJson<FlightReplayReadOnly>(json);
        Assert.That(report.assetsExact&&report.weightsMatchObserved&&report.colliderControlsPassed&&report.tailSatControlsPassed&&report.candidateHoverV3,Is.True);Assert.That(report.scope,Does.Contain("NOT_SAVED_AUTHORING_OR_MATCH_ACCEPTANCE"));Assert.That(report.actualTailTrianglesTested,Is.GreaterThan(0));Assert.That(report.actualTailBoxIntersections,Is.Zero,"Every original tail triangle is checked against both original solid NWH boxes, independently of the vertical skin ray.");Assert.That(report.minimumSkinGap,Is.GreaterThanOrEqualTo(-.15f));Assert.That(report.candidateRecipe.neckTranslationsUnchanged&&report.candidateRecipe.tailRootTranslationsUnchanged,Is.True);Assert.That(report.candidateRecipe.maximumWorldAimError,Is.LessThanOrEqualTo(.5f));Assert.That(report.candidateRecipe.maximumReferenceTailRootRotationError,Is.LessThanOrEqualTo(.5f));Assert.That(report.candidateRecipe.recipe,Does.Contain("V3"));
        Assert.That(report.images.Length,Is.EqualTo(4));foreach(string image in report.images){Assert.That(File.Exists(image),Is.True);Assert.That(new FileInfo(image).Length,Is.GreaterThan(2000));}TestContext.WriteLine("READ_ONLY_CANDIDATE_FIRE_HOVER_V3 originalAssetsExact=true actualNativeWeightsExact=true allTailTriangles="+report.actualTailTrianglesTested+" actualTailBoxIntersections="+report.actualTailBoxIntersections+" minSkin="+report.minimumSkinGap+" NOT_SAVED_AUTHORING_OR_MATCH_ACCEPTANCE");
    }
    [Test,Timeout(120000)]
    public void ExplicitSavedTimberCradlesAndAnimatedHarnessPreserveOriginalPhysicsAndRepeatSceneBytes()
    {
        var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);var upgrade=author.GetMethod("UpgradeSavedSaddlePresentation");string guid=AssetDatabase.AssetPathToGUID(Path);upgrade.Invoke(null,null);var after=File.ReadAllBytes(Path);upgrade.Invoke(null,null);Assert.That(File.ReadAllBytes(Path),Is.EqualTo(after));Assert.That(AssetDatabase.AssetPathToGUID(Path),Is.EqualTo(guid));
        var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;
        try
        {
            scene=EditorSceneManager.OpenScene(Path,OpenSceneMode.Additive);var frames=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Component>(true)).Where(value=>value!=null&&value.GetType().Name=="DragonRiderSaddle").ToArray();Assert.That(frames.Length,Is.GreaterThanOrEqualTo(8));
            foreach(var frame in frames)
            {
                Assert.That(Field(frame,"calibrationVersion"),Is.EqualTo(7));var rigging=(Component)Field(frame,"rigging");Assert.That(rigging,Is.Not.Null);Assert.That(Field(rigging,"frame"),Is.SameAs(frame));Assert.That(Field(rigging,"truck"),Is.Not.Null);Assert.That(Field(rigging,"presentationVersion"),Is.EqualTo(1));Assert.That(((Array)Field(rigging,"wheelCradles")).Length,Is.EqualTo(4));Assert.That(((Array)Field(rigging,"legs")).Length,Is.EqualTo(4));Assert.That(((Array)Field(rigging,"girths")).Length,Is.EqualTo(2));Assert.That(((Array)Field(rigging,"upperTies")).Length,Is.EqualTo(2));
                var support=(Transform)Field(frame,"supportPresentation");Assert.That(support.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(rigging.GetComponentsInChildren<Collider>(true),Is.Empty);
                foreach(var renderer in support.GetComponentsInChildren<Renderer>(true).Concat(rigging.GetComponentsInChildren<Renderer>(true)))foreach(var material in renderer.sharedMaterials){Assert.That(material,Is.Not.Null);Assert.That(material.shader.name,Does.StartWith("Universal Render Pipeline/"));if(renderer is LineRenderer)Assert.That(material.GetTexture("_BaseMap"),Is.Not.Null,"Actual owned leather/rope textures must survive conversion, not a flat-color placeholder.");}
            }
        }
        finally{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
    }
    [Test,Timeout(120000)]
    public void ReadOnlyObservedFireThreeLeafStateReplaysActualTailAndOriginalNwhFailureWithoutSavingAssets()
    {
        var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);var json=(string)author.GetMethod("InspectActualV203FireStateWithoutSavingAssets").Invoke(null,null);var report=JsonUtility.FromJson<FlightReplayReadOnly>(json);
        Assert.That(report.assetsExact&&report.weightsMatchObserved&&report.colliderControlsPassed,Is.True);Assert.That(report.scope,Does.Contain("NOT_MOUNT_ACCEPTANCE"));Assert.That(report.normalizedTime,Is.EqualTo(.535184741f).Within(.0001f));Assert.That(report.minimumSkinGap,Is.LessThan(-.15f),"This read-only historical V203 replay must reproduce the actual rejected pose, not substitute new HoverV2 assets.");
        var ray=report.rays.Single(value=>value.index==3);Assert.That(ray.found,Is.True);Assert.That(ray.gap,Is.LessThan(-.15f));Assert.That(ray.actualTriangle.wingSkinWeight,Is.EqualTo(0));Assert.That(ray.actualTriangle.vertexInfluences.Any(value=>value.bones.Any(name=>name.StartsWith("RigTail",StringComparison.Ordinal))),Is.True);Assert.That(ray.actualTriangle.cpuSkinningRmse,Is.LessThan(.0001f));Assert.That(ray.originalColliderQueries,Is.Not.Empty);
        Assert.That(report.tailSatControlsPassed,Is.True);Assert.That(report.actualTailTrianglesTested,Is.GreaterThan(0));Assert.That(report.diagnosticWingTrianglesHidden,Is.GreaterThan(0));Assert.That(report.images.Length,Is.EqualTo(4));foreach(string image in report.images){Assert.That(File.Exists(image),Is.True);Assert.That(new FileInfo(image).Length,Is.GreaterThan(2000));}TestContext.WriteLine("ACTUAL_V203_THREE_LEAF_NATIVE_REPLAY minSkin="+report.minimumSkinGap+" allOriginalTailTriangles="+report.actualTailTrianglesTested+" actualTailBoxIntersections="+report.actualTailBoxIntersections+" originalAssetsExact=true observedWeightsExact=true NO_MOUNT_ACCEPTANCE_CLAIM");
    }
    [Test,Timeout(120000)]
    public void ReadOnlyOriginalAndSavedFireHoverMuzzleProfilesMeasureActualConnectedHeadAtNativeFailurePhase()
    {
        Assert.That(EditorApplication.isPlayingOrWillChangePlaymode,Is.False);var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);string json=(string)author.GetMethod("InspectSavedFireHoverMuzzleProfileWithoutSavingAssets").Invoke(null,null);var profile=JsonUtility.FromJson<HoverProfileReadOnly>(json);
        Assert.That(profile.sourceAndSavedAssetsExact,Is.True);Assert.That(profile.scope,Does.Contain("NOT_CONTACT_OR_MATCH_ACCEPTANCE"));Assert.That(profile.rows.Length,Is.GreaterThanOrEqualTo(121));Assert.That(profile.actualNeckName,Is.Not.Empty);var actual=profile.rows.Single(row=>Mathf.Abs(row.phase-.604f)<.000001f);
        foreach(var row in profile.rows)Assert.That(float.IsFinite(row.originalHoverAngle)&&float.IsFinite(row.mountedHoverAngle)&&float.IsFinite(row.sourceForwardAngle)&&float.IsFinite(row.originalToMountedAimError),Is.True);
        Assert.That(profile.images.Length,Is.EqualTo(3));foreach(string image in profile.images){Assert.That(File.Exists(image),Is.True);Assert.That(new FileInfo(image).Length,Is.GreaterThan(2000));}
        TestContext.WriteLine("ACTUAL_FIRE_HOVER_AIM_PROFILE phase=.604 original="+actual.originalHoverAngle+" owned="+actual.mountedHoverAngle+" forward="+actual.sourceForwardAngle+" originalVsOwned="+actual.originalToMountedAimError+" maximumOriginalVsOwned="+profile.maximumOriginalToMountedAimError+" neck="+profile.actualNeckName+" sourcesBytesExact=true NO_CONTACT_OR_MATCH_ACCEPTANCE_CLAIM");
    }
    static void VerifyRetainedAnatomicalProofs(CenteredReadOnly report)
    {
        Assert.That(report.retainedNeutralAnchorProofs.Length,Is.EqualTo(5),"All five actual saved neutral anchors must remain eligible at their identical material points.");
        Assert.That(report.retainedLowTorsoNeckProofs,Is.Not.Empty,"Real neck-blended torso with low named-torso weight must remain supported.");
        foreach(var proof in report.retainedNeutralAnchorProofs.Concat(report.retainedLowTorsoNeckProofs)){Assert.That(proof.torsoSkinWeight,Is.GreaterThan(0));Assert.That(proof.wingDominated,Is.False);Assert.That(proof.wingSkinWeight,Is.LessThanOrEqualTo(.5f));Assert.That(proof.cpuSkinningRmse,Is.LessThan(.0001f));}
        Assert.That(report.retainedLowTorsoNeckProofs.Any(value=>value.torsoSkinWeight<.2f&&value.vertexInfluences.Any(vertex=>vertex.bones.Any(name=>name!=null&&name.StartsWith("RigNeck",StringComparison.Ordinal)))),Is.True);
    }
    [Test,Timeout(180000)]
    public void ReadOnlyCenteredInfernoFootprintExplainsActualNegativeGapSkinWeightsWithoutSavingAssets()
    {
        Assert.That(EditorApplication.isPlayingOrWillChangePlaymode,Is.False);
        var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);
        string json=(string)author.GetMethod("InspectSavedCenteredInfernoFootprintWithoutSavingAssets").Invoke(null,null);
        var report=JsonUtility.FromJson<CenteredReadOnly>(json);
        Assert.That(report.scope,Does.Contain("NOT_SAVED_CALIBRATION_OR_MOUNT_ACCEPTANCE"));Assert.That(report.sourceAndSavedAssetsExact,Is.True);Assert.That(report.actualNeutralPatchCentered,Is.True);
        VerifyRetainedAnatomicalProofs(report);
        Assert.That(Mathf.Abs(report.measuredNeutralLateral),Is.LessThan(.0001f));Assert.That(Mathf.Abs(report.measuredNeutralRearward),Is.LessThan(.0001f));
        var footprint=report.footprint;Assert.That(footprint.poses.Length,Is.GreaterThan(400),"The same real configured controller/native mixer coverage must execute, not just one selected bad frame.");Assert.That(footprint.originalCrews,Is.EqualTo(4));Assert.That(footprint.uniqueFootprints,Is.GreaterThan(0));
        int negative=0;
        foreach(var pose in footprint.poses)foreach(var point in pose.points??Array.Empty<ReadOnlyPoint>())
            if(point.torso&&point.gap<-.15f)
            {
                negative++;var triangle=point.actualPenetratingTriangle;
                Assert.That(triangle,Is.Not.Null);Assert.That(triangle.mesh,Is.Not.Empty);Assert.That(triangle.upwardNormal,Is.GreaterThanOrEqualTo(.1f));Assert.That(triangle.torsoSkinWeight,Is.GreaterThan(0));Assert.That(triangle.cpuSkinningRmse,Is.LessThan(.0001f));Assert.That(triangle.barycentric.x+triangle.barycentric.y+triangle.barycentric.z,Is.EqualTo(1).Within(.0001f));Assert.That(triangle.vertexInfluences.Length,Is.EqualTo(3));
                foreach(var vertex in triangle.vertexInfluences){Assert.That(vertex.vertex,Is.GreaterThanOrEqualTo(0));Assert.That(vertex.weights.Length,Is.EqualTo(4));Assert.That(vertex.bones.Length,Is.EqualTo(4));Assert.That(vertex.weights.Sum(),Is.EqualTo(1).Within(.002f));}
            }
        Assert.That(footprint.violatingRayProofs,Is.EqualTo(negative));
        TestContext.WriteLine("ACTUAL_CENTERED_INFERNO_DIAGNOSIS poses="+footprint.poses.Length+" fits="+footprint.fits+" actualNegativeRayBoneProofs="+negative+" centered=true assetsBytesExact=true NOT_CALIBRATION_OR_FULL_MOUNT_ACCEPTANCE");
    }
    [Test,Timeout(180000)]
    public void FourActualWingPanelPosesRenderFullOriginalNwhAndMeasureRealColliderInterferenceWithoutSavingAssets()
    {
        Assert.That(EditorApplication.isPlayingOrWillChangePlaymode,Is.False);var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);
        string json=(string)author.GetMethod("InspectSavedCenteredInfernoInterferenceWithoutSavingAssets").Invoke(null,null);var report=JsonUtility.FromJson<CenteredReadOnly>(json);
        Assert.That(report.sourceAndSavedAssetsExact,Is.True);Assert.That(report.actualNeutralPatchCentered,Is.True);VerifyRetainedAnatomicalProofs(report);Assert.That(report.actualInterference.Length,Is.EqualTo(4));
        foreach(var pose in report.actualInterference)
        {
            Assert.That(pose.originalGeometryPreserved,Is.True);Assert.That(pose.actualColliderQueryControlsPassed,Is.True);Assert.That(pose.insideControlDistance,Is.LessThan(.0001f));Assert.That(pose.outsideControlDistance,Is.GreaterThan(900));Assert.That(pose.originalBodies,Is.EqualTo(pose.clonedBodies));Assert.That(pose.originalMass,Is.EqualTo(pose.clonedMass));Assert.That(pose.originalSolidColliders,Is.EqualTo(pose.clonedSolidColliders));Assert.That(pose.renderers,Is.GreaterThan(0));Assert.That(pose.rays.Length,Is.EqualTo(9));Assert.That(pose.rawWingRays,Is.GreaterThan(0));Assert.That(pose.images.Length,Is.EqualTo(2));
            foreach(string image in pose.images){Assert.That(File.Exists(image),Is.True);Assert.That(new FileInfo(image).Length,Is.GreaterThan(2000),"Actual native source-material rendering required; final visual review remains separate.");}
            var seating=pose.actualWheelDeckSeating;Assert.That(seating.visibleDeckMeshes,Is.GreaterThan(0));Assert.That(seating.actualDeckSources,Is.Not.Empty);Assert.That(seating.wheels,Is.Not.Empty);
            foreach(var wheel in seating.wheels){Assert.That(wheel.actualVisualFound,Is.True,"Measure the actual NWH public wheel visual rather than a radius/spring guess.");Assert.That(wheel.sampledVisibleVertices,Is.GreaterThan(0));if(wheel.deckFaceFound)Assert.That(float.IsFinite(wheel.actualGap),Is.True);}
            TestContext.WriteLine("ACTUAL_DECK_WHEEL_DIAGNOSIS pose="+pose.pose+" visibleTop="+seating.visibleDeckTopOffset+" allIncludingInactiveTop="+seating.allIncludingInactiveDeckTopOffset+" actualWheelGaps="+string.Join(",",seating.wheels.Select(value=>value.deckFaceFound?value.actualGap.ToString("F4"):"NO_ACTUAL_TIMBER_FACE"))+" NO_SEATING_ACCEPTANCE_CLAIM");
            var raw=pose.rays.Where(value=>value.rawFound&&value.rawIsMajorityWing).ToArray();Assert.That(raw.Any(value=>value.rawGap<-.15f),Is.True,"The original observed negative-wing-ray witness must remain recorded independently of corrected anatomical classification.");
            foreach(var ray in raw){Assert.That(ray.rawProof.wingSkinWeight,Is.GreaterThan(.5f));Assert.That(ray.actualColliderClosestPoints,Is.Not.Empty);Assert.That(ray.actualColliderClosestPoints.Any(value=>value.type=="BoxCollider"&&value.enabled&&value.active),Is.True);Assert.That(ray.insideAnyOriginalSolidCollider,Is.EqualTo(ray.actualColliderClosestPoints.Any(value=>value.enabled&&value.active&&value.pointInsideOrOnBoundary)));}
            TestContext.WriteLine("ACTUAL_WING_CAB_INTERFERENCE pose="+pose.pose+" rawWingRays="+pose.rawWingRays+" actualPointsInsideOriginalSolidColliders="+pose.rawWingPointsInsideOriginalColliders+" images="+string.Join(";",pose.images)+" NO_NO_CLIPPING_OR_MOUNT_ACCEPTANCE_CLAIM");
        }
    }
    [Test,Timeout(300000)]
    public void ExplicitSavedSaddleCalibrationPreservesGuidNwhAndRepeatSceneBytes()
    {
        Assert.That(EditorApplication.isPlayingOrWillChangePlaymode,Is.False);Assert.That(File.Exists(Path),Is.True);
        string guid=AssetDatabase.AssetPathToGUID(Path);byte[] meta=File.ReadAllBytes(Path+".meta");
        const string catalogPath="Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset";var catalogBytes=File.ReadAllBytes(catalogPath);
        var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);var upgrade=author.GetMethod("UpgradeSavedSaddles");upgrade.Invoke(null,null);
        var calibrated=File.ReadAllBytes(Path);var ownedMotion=Directory.GetFiles("Assets/TruckDragonRiders/Animations","*",SearchOption.AllDirectories).ToDictionary(value=>value,File.ReadAllBytes);upgrade.Invoke(null,null);upgrade.Invoke(null,null);
        Assert.That(File.ReadAllBytes(Path),Is.EqualTo(calibrated),"Already-calibrated mode scene must remain byte-identical on repeat.");Assert.That(AssetDatabase.AssetPathToGUID(Path),Is.EqualTo(guid));Assert.That(File.ReadAllBytes(Path+".meta"),Is.EqualTo(meta));
        Assert.That(File.ReadAllBytes(catalogPath),Is.EqualTo(catalogBytes),"Riders carrier posture cannot alter shared Fire or other-mode animation mappings.");foreach(var pair in ownedMotion)Assert.That(File.ReadAllBytes(pair.Key),Is.EqualTo(pair.Value),"Repeat explicit upgrade must preserve owned derivative/controller GUIDs and bytes: "+pair.Key);
        var original=SceneManager.GetActiveScene();Scene scene=default;
        try
        {
            scene=EditorSceneManager.OpenScene(Path,OpenSceneMode.Additive);var type=Type.GetType("LWS.TruckDragonRiders.DragonRidersMatch, Assembly-CSharp",true);var match=scene.GetRootGameObjects().SelectMany(value=>value.GetComponentsInChildren(type,true)).Single();int count=0;
            foreach(var racer in (IEnumerable)Field(match,"racers"))foreach(Component actor in (IEnumerable)Field(racer,"choices"))
            {
                var saddle=actor.GetComponents<Component>().Single(value=>value!=null&&value.GetType().Name=="DragonRiderSaddle");Assert.That(Field(saddle,"calibrationVersion"),Is.EqualTo(7));Assert.That((int)Field(saddle,"sampledSkinSurfaces"),Is.GreaterThanOrEqualTo(20));
                Assert.That(Field(saddle,"surfaceRenderer"),Is.Not.Null);var bary=(Vector3)Field(saddle,"surfaceBarycentric");Assert.That(bary.x+bary.y+bary.z,Is.EqualTo(1).Within(.0001f));
                var patch=((IEnumerable)Field(saddle,"surfacePatchAnchors")).Cast<object>().ToArray();Assert.That(patch.Length,Is.EqualTo(5),"Center/bilateral/longitudinal actual torso patch, rather than one curved face normal.");
                foreach(var point in patch){var weights=(Vector3)Field(point,"barycentric");Assert.That(weights.x+weights.y+weights.z,Is.EqualTo(1).Within(.0001f));Assert.That((int)Field(point,"a"),Is.GreaterThanOrEqualTo(0));Assert.That((int)Field(point,"b"),Is.GreaterThanOrEqualTo(0));Assert.That((int)Field(point,"c"),Is.GreaterThanOrEqualTo(0));}
                float maximum=(float)Field(saddle,"measuredDorsalMaximum"),minimum=(float)Field(saddle,"measuredDorsalMinimum");var rootSupport=(Vector3)Field(saddle,"supportMotorLocalPosition");var motor=(Component)Field(actor,"motor");Assert.That(maximum,Is.GreaterThanOrEqualTo(minimum));Assert.That((rootSupport.y-maximum)*motor.transform.lossyScale.y,Is.EqualTo(.45f).Within(.0001f),"Retain exact historical measurement provenance without using its maximum as the runtime platform height.");
                var socket=(Transform)Field(saddle,"socket");var bone=(Transform)Field(saddle,"sourceBone");Assert.That(socket.parent,Is.SameAs(bone));Assert.That(bone.name,Is.EqualTo("RigSpine2"));Assert.That(Field(saddle,"neutralClip"),Is.Not.Null);Assert.That((float)Field(saddle,"dorsalClearance"),Is.GreaterThanOrEqualTo(.2f));
                var support=(Transform)Field(saddle,"supportPresentation");Assert.That(support.GetComponentsInChildren<Collider>(true),Is.Empty,"Display support does not substitute new truck/dragon physics.");
                if((string)Field(saddle,"speciesId")=="dragon-fire")
                {
                    var animator=(Animator)Field(motor,"animator");var controller=animator.runtimeAnimatorController as AnimatorOverrideController;Assert.That(controller,Is.Not.Null);Assert.That(AssetDatabase.GetAssetPath(controller),Does.StartWith("Assets/TruckDragonRiders/Animations/dragon-fire/"));
                    var derivative=(AnimationClip)Field(saddle,"neutralClip");Assert.That(AssetDatabase.GetAssetPath(derivative),Is.EqualTo("Assets/TruckDragonRiders/Animations/dragon-fire/Mounted Hover.anim"));
                    var catalog=AssetDatabase.LoadAssetAtPath<ScriptableObject>(catalogPath);var fire=((IEnumerable)Field(catalog,"species")).Cast<object>().Single(value=>(string)Field(value,"id")=="dragon-fire");var hoverMapping=((IEnumerable)Field(fire,"animations")).Cast<object>().Single(value=>Field(value,"action").ToString()=="FlyIdle");var source=(AnimationClip)Field(hoverMapping,"clip");Assert.That(controller[source],Is.SameAs(derivative));Assert.That(motor.transform.localScale.y,Is.EqualTo(7),"Keep actual Fire size; the carrier solution is a real pose derivative.");
                    var wings=AnimationUtility.GetCurveBindings(source).Where(binding=>binding.path.Contains("Wing")).ToArray();Assert.That(wings,Is.Not.Empty,"Actual original hover wing motion required.");
                    foreach(var binding in wings){var a=AnimationUtility.GetEditorCurve(source,binding);var b=AnimationUtility.GetEditorCurve(derivative,binding);Assert.That(b,Is.Not.Null);Assert.That(b.keys,Is.EqualTo(a.keys),"Owned hover must retain the actual original wing channels: "+binding.path+"/"+binding.propertyName);}
                    var hoverHead=motor.GetComponentsInChildren<Transform>(true).Single(value=>value.name=="RigHead");var hoverTail=motor.GetComponentsInChildren<Transform>(true).Single(value=>value.name=="RigTail1");string hoverHeadPath=AnimationUtility.CalculateTransformPath(hoverHead,motor.transform),hoverNeckPath=AnimationUtility.CalculateTransformPath(hoverHead.parent,motor.transform),hoverTailPath=AnimationUtility.CalculateTransformPath(hoverTail,motor.transform);int hoverPreserved=0,distalTailPreserved=0;
                    foreach(var binding in AnimationUtility.GetCurveBindings(source))
                    {
                        string joint=binding.path.Split('/').Last();bool supportJoint=new[]{"RigPelvis","RigSpine1","RigSpine2","RigRibcage"}.Contains(joint),rotation=binding.type==typeof(Transform)&&(binding.propertyName.StartsWith("m_LocalRotation.",StringComparison.Ordinal)||binding.propertyName.IndexOf("Euler",StringComparison.OrdinalIgnoreCase)>=0);
                        if(supportJoint&&binding.type==typeof(Transform)&&(rotation||binding.propertyName.StartsWith("m_LocalPosition.",StringComparison.Ordinal)))continue;if(rotation&&(binding.path==hoverHeadPath||binding.path==hoverNeckPath||binding.path==hoverTailPath))continue;
                        var originalCurve=AnimationUtility.GetEditorCurve(source,binding);var mounted=AnimationUtility.GetEditorCurve(derivative,binding);Assert.That(mounted,Is.Not.Null);Assert.That(mounted.keys,Is.EqualTo(originalCurve.keys),"HoverV3 retains every unselected original root/limb/wing/jaw/distal-tail/neck-translation channel: "+binding.path+"/"+binding.propertyName);hoverPreserved++;if(joint.StartsWith("RigTail",StringComparison.Ordinal)&&joint!="RigTail1")distalTailPreserved++;
                    }
                    Assert.That(hoverPreserved,Is.GreaterThan(100));Assert.That(distalTailPreserved,Is.GreaterThan(0));
                    var launchMapping=((IEnumerable)Field(fire,"animations")).Cast<object>().Single(value=>Field(value,"action").ToString()=="Takeoff");var launch=(AnimationClip)Field(launchMapping,"clip");var mountedLaunch=controller[launch];
                    Assert.That(mountedLaunch,Is.Not.Null);Assert.That(AssetDatabase.GetAssetPath(mountedLaunch),Is.EqualTo("Assets/TruckDragonRiders/Animations/dragon-fire/Mounted Takeoff.anim"));Assert.That(mountedLaunch.length,Is.EqualTo(launch.length));
                    string[] supportBones={"RigPelvis","RigSpine1","RigSpine2","RigRibcage"};int preserved=0;
                    foreach(var binding in AnimationUtility.GetCurveBindings(launch).Where(value=>!supportBones.Contains(value.path.Split('/').Last())&&!(value.path.Split('/').Last()=="RigTail1"&&value.type==typeof(Transform)&&(value.propertyName.StartsWith("m_LocalRotation.",StringComparison.Ordinal)||value.propertyName.IndexOf("Euler",StringComparison.OrdinalIgnoreCase)>=0))))
                    {var a=AnimationUtility.GetEditorCurve(launch,binding);var b=AnimationUtility.GetEditorCurve(mountedLaunch,binding);Assert.That(b,Is.Not.Null);Assert.That(b.keys,Is.EqualTo(a.keys),"Mounted Takeoff preserves actual root/head/mouth/wing/limb/unselected-tail channel: "+binding.path+"/"+binding.propertyName);preserved++;}
                    Assert.That(preserved,Is.GreaterThan(100));
                    var fireMappings=((IEnumerable)Field(fire,"animations")).Cast<object>().ToArray();var head=motor.GetComponentsInChildren<Transform>(true).Single(value=>value.name=="RigHead");string headPath=AnimationUtility.CalculateTransformPath(head,motor.transform),neckPath=AnimationUtility.CalculateTransformPath(head.parent,motor.transform);
                    foreach(string action in new[]{"TurnLeft","TurnRight","Land","Breath"})
                    {
                        var mapping=fireMappings.Single(value=>Field(value,"action").ToString()==action);var originalAction=(AnimationClip)Field(mapping,"clip");var mounted=controller[originalAction];
                        string title=action=="TurnLeft"?"Mounted Turn Left":action=="TurnRight"?"Mounted Turn Right":"Mounted "+action;
                        Assert.That(mounted,Is.Not.Null);Assert.That(AssetDatabase.GetAssetPath(mounted),Is.EqualTo("Assets/TruckDragonRiders/Animations/dragon-fire/"+title+".anim"));Assert.That(mounted.length,Is.EqualTo(originalAction.length));int unchanged=0;
                        foreach(var binding in AnimationUtility.GetCurveBindings(originalAction))
                        {
                            bool supportJoint=supportBones.Contains(binding.path.Split('/').Last());bool rotation=binding.type==typeof(Transform)&&(binding.propertyName.StartsWith("m_LocalRotation.",StringComparison.Ordinal)||binding.propertyName.IndexOf("Euler",StringComparison.OrdinalIgnoreCase)>=0);if(binding.path.Split('/').Last()=="RigTail1"&&rotation)continue;
                            if(supportJoint&&binding.type==typeof(Transform)&&(rotation||binding.propertyName.StartsWith("m_LocalPosition.",StringComparison.Ordinal)))continue;
                            if(action=="Breath"&&(binding.path==headPath||binding.path==neckPath)&&rotation)continue;
                            var a=AnimationUtility.GetEditorCurve(originalAction,binding);var b=AnimationUtility.GetEditorCurve(mounted,binding);Assert.That(b,Is.Not.Null);Assert.That(b.keys,Is.EqualTo(a.keys),"Mounted Fire action preserves original root/wing/limb/tail/jaw and neck/head translation channels: "+action+"/"+binding.path+"/"+binding.propertyName);unchanged++;
                        }
                        Assert.That(unchanged,Is.GreaterThan(100));
                    }
                }
                else if((string)Field(saddle,"speciesId")=="dragon-inferno")
                {
                    var animator=(Animator)Field(motor,"animator");var controller=animator.runtimeAnimatorController as AnimatorOverrideController;Assert.That(controller,Is.Not.Null);Assert.That(AssetDatabase.GetAssetPath(controller),Is.EqualTo("Assets/TruckDragonRiders/Animations/dragon-inferno/Mounted Actions.overrideController"));
                    Assert.That(motor.transform.localScale.y,Is.EqualTo(5),"Keep original Inferno size and original NWH geometry.");
                    var catalog=AssetDatabase.LoadAssetAtPath<ScriptableObject>(catalogPath);var inferno=((IEnumerable)Field(catalog,"species")).Cast<object>().Single(value=>(string)Field(value,"id")=="dragon-inferno");var mappings=((IEnumerable)Field(inferno,"animations")).Cast<object>().ToArray();
                    var head=motor.GetComponentsInChildren<Transform>(true).Single(value=>value.name=="RigHead");string headPath=AnimationUtility.CalculateTransformPath(head,motor.transform),neckPath=AnimationUtility.CalculateTransformPath(head.parent,motor.transform);
                    foreach(string action in new[]{"TurnRight","Breath"})
                    {
                        var mapping=mappings.Single(value=>Field(value,"action").ToString()==action);var source=(AnimationClip)Field(mapping,"clip");var derivative=controller[source];Assert.That(derivative,Is.Not.Null);Assert.That(AssetDatabase.GetAssetPath(derivative),Is.EqualTo("Assets/TruckDragonRiders/Animations/dragon-inferno/"+(action=="Breath"?"Mounted Breath":"Mounted Turn Right")+".anim"));Assert.That(derivative.length,Is.EqualTo(source.length));int preserved=0;
                        foreach(var binding in AnimationUtility.GetCurveBindings(source))
                        {
                            bool supportJoint=new[]{"RigPelvis","RigSpine1","RigSpine2","RigRibcage"}.Contains(binding.path.Split('/').Last());
                            bool rotation=binding.type==typeof(Transform)&&(binding.propertyName.StartsWith("m_LocalRotation.",StringComparison.Ordinal)||binding.propertyName.IndexOf("Euler",StringComparison.OrdinalIgnoreCase)>=0);
                            if(supportJoint&&binding.type==typeof(Transform)&&(rotation||binding.propertyName.StartsWith("m_LocalPosition.",StringComparison.Ordinal)))continue;
                            if(action=="Breath"&&(binding.path==headPath||binding.path==neckPath)&&rotation)continue;
                            var a=AnimationUtility.GetEditorCurve(source,binding);var b=AnimationUtility.GetEditorCurve(derivative,binding);Assert.That(b,Is.Not.Null);Assert.That(b.keys,Is.EqualTo(a.keys),"Preserve original root/head translations/wing/limb/tail/jaw channel: "+binding.path+"/"+binding.propertyName);preserved++;
                        }
                        Assert.That(preserved,Is.GreaterThan(100));
                    }
                }
                foreach(var renderer in support.GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials){Assert.That(material,Is.Not.Null);Assert.That(material.shader.name,Does.StartWith("Universal Render Pipeline/"));Assert.That(AssetDatabase.GetAssetPath(material),Does.StartWith("Assets/TruckDragonRiders/Materials/"));}
                count++;
            }
            Assert.That(count,Is.GreaterThanOrEqualTo(8),"Both actual species remain available for all four original NWH crews.");
        }
        finally{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);}
    }
}
#endif
