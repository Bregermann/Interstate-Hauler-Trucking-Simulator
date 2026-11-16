#if UNITY_EDITOR
using System;
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
        [Serializable]sealed class PristineRoundTripReport
        {
            public string scope="READ_ONLY_NATIVE_PRISTINE_CONFIGURATION_ROUND_TRIP_AND_ACTUAL_WALK45_RESTORE_DIAGNOSIS_NO_EXPORT_NO_699_OR_RUNTIME_ADOPTION",failure;public bool sourceExact,sceneExact,nwhExact;public PristineRestorationProof[] proofs;
        }
        public static string InspectPristineMotorRootSerializationRoundTripWithoutSavingAssets(string geometryEvidence,string materialEvidence)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled native Editor required.");for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve unsaved scenes.");RequireRiderPassedManifest(geometryEvidence,"DragonRiderMotorRootRepresentationTests.UnitAncestorsAndInactiveConfiguredMotorRootRetainFull699GeometryAndMeasureActualTruckCenteredCourseEnvelope");var original=JsonUtility.FromJson<MountedSizeReport>(File.ReadAllText(Directory.GetFiles(Path.Combine(geometryEvidence,"rendered-evidence"),"precise-motor-root-inferno-course-envelope.json",SearchOption.AllDirectories).Single()));var accepted=original.profiles.Single();if(!original.sourceBytesExact||!original.nwhExact||!original.sceneBytesExact||accepted.actualPoses!=699||accepted.invalidAnatomicalPoses!=0||accepted.candidateSatPairs!=0||!accepted.exactOriginalBoxAcceptance||!accepted.originalFootprintFits||accepted.roundedCandidateFactor!=3.4f)throw new InvalidOperationException("Actual full699motor17geometry control required before serialization-only diagnosis.");foreach(var source in accepted.sourceFiles)if(Hash(CurrentRiderPhysicalDependency(source.path))!=source.sha256)throw new InvalidOperationException("Original source identity differs before native configuration diagnosis: "+source.path);var material=RequireProvenActualPointwiseInfernoMaterial(materialEvidence);
            var report=new PristineRoundTripReport();var watched=Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories).ToDictionary(path=>path,Hash);string sceneHash=Hash(DragonRidersAuthoring.ScenePath),physics=null;var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;DragonRidersMatch match=null;var preview=new PreviewRenderUtility();DragonRiderSaddle frame=null;UnityEngine.Object[] generated=null;
            try
            {
                scene=EditorSceneManager.OpenScene(DragonRidersAuthoring.ScenePath,OpenSceneMode.Additive);match=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<DragonRidersMatch>(true)).Single();physics=PhysicsSignature(match);var saved=match.racers.SelectMany(racer=>racer.choices).First(actor=>actor.GetComponent<DragonRiderSaddle>().speciesId=="dragon-inferno");var copy=UnityEngine.Object.Instantiate(saved.gameObject);preview.AddSingleGO(copy);var actor=copy.GetComponent<DragonCharacter>();frame=copy.GetComponent<DragonRiderSaddle>();generated=CreateTransientSafeMountedController(actor,"dragon-inferno");var row=new MountedSizeProfile{existingAllActionSafeMounted=true};SavedJoint[] rest=null;PristineColdConfiguration pristine=null;PrepareInactiveMotorRootCandidate(preview,saved,match.trucks[0],generated.OfType<AnimatorOverrideController>().Single(),material,3.4f,row,ref copy,ref actor,ref frame,ref rest,value=>pristine=new PristineColdConfiguration(value));
                copy.SetActive(false);pristine.RestoreForSerialization();var immediate=pristine.Inspect("IMMEDIATE_NATIVE_CONFIGURATION_TRANSFORM_FLAG_LINE_ROUND_TRIP_NO_SOURCE_POSE_SAMPLE");copy.SetActive(true);actor.motor.animator.enabled=false;foreach(var body in copy.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.detectCollisions=false;}if(!match.catalog.TryGet("dragon-inferno",out var species)||!species.TryGetAnimation(DragonAnimationAction.Walk,out var walk))throw new InvalidOperationException("Original mapped Walk source required.");SampleMountedTrial(actor,new MountedPoseTrial{name="Actual original Walk phase0.45 before serialization roundtrip",clip=EffectiveRiderClip(actor,walk.clip),fraction=.45f});frame.CaptureEvaluatedAnimationPose();copy.SetActive(false);pristine.RestoreForSerialization();var afterWalk=pristine.Inspect("AFTER_GENUINE_OWNED_SOURCE_WALK45_FRAME_EVALUATION_AND_CONFIGURATION_RESTORE");
                // Reproduce the EXACT V376 setter operation on the isolated
                // authored clone, with no invented bone pose or oracle change.
                // Its observed negative control must then be repaired by the
                // same native serialized authority used by the exporter.
                foreach(var authored in pristine.joints)authored.Restore();var normalizedSetter=pristine.Inspect("ORIGINAL_GEOMETRY_REST_SETTER_KNOWN_ONE_ULP_NEGATIVE_CONTROL");pristine.RestoreForSerialization();report.proofs=new[]{immediate,afterWalk,normalizedSetter,pristine.Inspect("AFTER_NATIVE_SERIALIZED_RESTORE_OF_OBSERVED_ORIGINAL_SETTER_CHANGE")};
            }
            catch(Exception error){report.failure=error.ToString();throw;}
            finally
            {
                if(frame!=null)frame.ReleaseGeometrySnapshot();if(generated!=null)foreach(var value in generated)if(value!=null&&!AssetDatabase.Contains(value))UnityEngine.Object.DestroyImmediate(value);preview.Cleanup();if(match!=null&&physics!=null)report.nwhExact=PhysicsSignature(match)==physics;if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);report.sceneExact=Hash(DragonRidersAuthoring.ScenePath)==sceneHash;report.sourceExact=watched.Count==Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories).Length&&watched.All(pair=>File.Exists(pair.Key)&&Hash(pair.Key)==pair.Value);var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at>=0&&at+1<args.Length){string path=Path.Combine(args[at+1],"RiderSaddleAuthoring","PristineRoundTrip");Directory.CreateDirectory(path);File.WriteAllText(Path.Combine(path,"actual-pristine-native-configuration-roundtrip.json"),JsonUtility.ToJson(report,true));}
            }
            if(!report.sourceExact||!report.sceneExact||!report.nwhExact)throw new InvalidOperationException("Read-only configuration diagnosis changed original source/scene/NWH.");return JsonUtility.ToJson(report,true);
        }
    }
}
#endif
