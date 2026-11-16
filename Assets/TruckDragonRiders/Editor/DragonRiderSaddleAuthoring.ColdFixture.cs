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
        [Serializable]sealed class ColdFixtureReceipt
        {
            public string scope="EXPLICIT_TEST_ONLY_PRISTINE_AUTHORED_INACTIVE_MOTOR17_PREFAB_AFTER_SAME_FULL699_GEOMETRY_NOT_RUNTIME_AWAKE_OR_PRODUCTION_ADOPTION",folder,prefab,controller,manifest,hostRoot,geometryEvidence,failure;
            public bool runtimeAdapterConfiguredUnawakened,sameCandidatePassed699,configuredInactive,pristineAuthoredStagingRestoredForSerialization,sourceBytesExact,nwhClosureExact,hostContextQualified,nativeRuntimeAwakeObserved;
            public string species="dragon-inferno";public int expectedPoses=699;public bool sameCandidatePassedAllPoses;
            public string hostPackageLock,nativePackageLock;public CurrentOriginalNwhDependency[] nwhBefore,nwhAfter;public FlightSourceHash[] sourceBefore,sourceAfter,fixtureAssets;
            public PristineRestorationProof pristineRestoration;public int materialPoints,restoredAuthoredLocals;public Vector3 motorScale,actorScale;
        }
        static ColdFixtureExport coldFixtureExport;
        sealed class ColdFixtureExport
        {
            public readonly ColdFixtureReceipt receipt;
            PristineColdConfiguration pristine;readonly HashSet<string> originalModePaths;public string[] createdPaths=Array.Empty<string>();bool began,exported;
            public ColdFixtureExport(string folder,string evidence,string species="dragon-inferno")
            {
                folder=folder.Replace('\\','/').TrimEnd('/');const string prefix="Assets/TruckDragonRiders/Tests/Generated/";
                if(!folder.StartsWith(prefix,StringComparison.Ordinal)||folder.Substring(prefix.Length).Contains("/")||folder.Contains("..")||Directory.Exists(folder)||File.Exists(folder+".meta"))throw new InvalidOperationException("Unique absent direct TEST Generated child required; never overwrite fixture or production assets.");
                if(species!="dragon-inferno"&&species!="dragon-fire")throw new InvalidOperationException("Only explicitly qualified mounted species have a cold fixture contract.");receipt=new ColdFixtureReceipt{folder=folder,manifest=folder+"/actual-qualified-fixture.json",geometryEvidence=evidence,hostRoot=RiderEvidenceHostRoot(evidence),species=species,expectedPoses=species=="dragon-fire"?537:699};if(species=="dragon-fire")receipt.scope="EXPLICIT_TEST_ONLY_PRISTINE_AUTHORED_INACTIVE_FIRE_MOTOR11_POINT69_PREFAB_AFTER_SAME_FULL537_GEOMETRY_NOT_RUNTIME_AWAKE_OR_PRODUCTION_ADOPTION";originalModePaths=Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories).ToHashSet(StringComparer.Ordinal);
            }
            public void Begin(DragonRidersMatch match)
            {
                if(began)throw new InvalidOperationException("One explicit geometry/export pass required.");began=true;receipt.hostPackageLock=Hash(Path.Combine(receipt.hostRoot,"Packages/packages-lock.json"));receipt.nativePackageLock=Hash("Packages/packages-lock.json");receipt.nwhBefore=CaptureCurrentOriginalNwhClosure(match,receipt.hostRoot);receipt.sourceBefore=CaptureColdFixtureSources(match,receipt.species);
            }
            public void CapturePristineConfiguration(DragonCharacter actor)
            {if(pristine!=null)throw new InvalidOperationException("One fresh inactive actor configuration capture required.");pristine=new PristineColdConfiguration(actor);}
            public void Export(GameObject copy,DragonCharacter actor,DragonRiderSaddle frame,SavedJoint[] rest,UnityEngine.Object[] generated,MountedSizeProfile row)
            {
                if(!began||exported||row.species!=receipt.species||row.actualPoses!=receipt.expectedPoses||row.invalidAnatomicalPoses!=0||row.candidateSatPairs!=0||!row.exactOriginalBoxAcceptance||!row.originalFootprintFits||!row.sourceMotionUnchanged||row.actualCandidateSkinScaleError>=.001f||row.candidateWheelContactPoses!=receipt.expectedPoses||row.maximumActualWheelGap>.03f||!row.nativeUnitAncestors||!row.nativeMotorParentRetained||row.nativeRuntimeAwakeObserved)throw new InvalidOperationException("Same actual complete"+receipt.expectedPoses+" motor-root candidate must qualify BEFORE any test-only asset export.");
                // Acceptance is finished. Restore the pristine authored clone's
                // captured locals only for prefab serialization, never as an
                // animation/contact oracle or runtime pose correction.
                frame.ReleaseGeometrySnapshot();copy.SetActive(false);if(pristine==null)throw new InvalidOperationException("Fresh authored configuration was not captured before geometry overrides.");pristine.RestoreForSerialization();frame.rigging.truck=null;receipt.configuredInactive=!copy.activeInHierarchy;receipt.pristineRestoration=pristine.Inspect("AFTER_SAME_ACTUAL"+receipt.expectedPoses+"_AND_SERIALIZATION_RESTORE");receipt.pristineAuthoredStagingRestoredForSerialization=receipt.pristineRestoration.exact;receipt.restoredAuthoredLocals=pristine.joints.Length;
                bool fire=receipt.species=="dragon-fire";float originalScale=fire?7:5,motorScale=fire?7*1.67f:17;receipt.motorScale=actor.motor.transform.localScale;receipt.actorScale=actor.transform.localScale;receipt.materialPoints=frame.surfacePatchAnchors.Length;var adapter=actor.GetComponent<DragonRiderOwnedMotorScaleSpeed>();receipt.runtimeAdapterConfiguredUnawakened=adapter!=null&&!adapter.AwakeObserved&&adapter.motor==actor.motor&&adapter.speciesId==receipt.species&&adapter.authoredMotorScale==originalScale&&adapter.configuredMotorScale==motorScale&&adapter.authoredStateSources.SequenceEqual(actor.motor.animal.states)&&actor.motor.animal.states.All(AssetDatabase.Contains);if(!receipt.runtimeAdapterConfiguredUnawakened)throw new InvalidOperationException("Runtime-assembly adapter must retain exact authored State sources/species/scale and remain genuinely unawakened in stopped Editor.");if(!receipt.configuredInactive||!receipt.pristineAuthoredStagingRestoredForSerialization||!receipt.motorScale.Equals(Vector3.one*motorScale)||!receipt.actorScale.Equals(Vector3.one)||receipt.materialPoints!=5)throw new InvalidOperationException("Pristine inactive unit-owner/exact qualified motor/material5 configuration was not preserved for serialization.");
                Directory.CreateDirectory(receipt.folder);AssetDatabase.Refresh();var controller=actor.motor.animator.runtimeAnimatorController as AnimatorOverrideController;if(controller==null||AssetDatabase.Contains(controller)||generated==null||!generated.Contains(controller))throw new InvalidOperationException("Actual already-qualified owned generated controller required.");
                receipt.controller=receipt.folder+"/"+controller.name+".overrideController";foreach(var item in generated)if(item!=null)item.hideFlags=HideFlags.None;AssetDatabase.CreateAsset(controller,receipt.controller);foreach(var item in generated.Where(item=>item!=null&&item!=controller)){if(item is not AnimationClip||AssetDatabase.Contains(item))throw new InvalidOperationException("Only actual owned generated animation clips may become TEST controller subassets.");AssetDatabase.AddObjectToAsset(item,controller);}AssetDatabase.SaveAssetIfDirty(controller);
                receipt.prefab=receipt.folder+(fire?"/Pristine Fire motor11.69 TEST ONLY.prefab":"/Pristine Inferno motor17 TEST ONLY.prefab");var saved=PrefabUtility.SaveAsPrefabAsset(copy,receipt.prefab,out bool success);if(!success||saved==null||saved.activeSelf)throw new InvalidOperationException("Inactive pristine TEST prefab did not save correctly.");AssetDatabase.SaveAssetIfDirty(saved);AssetDatabase.Refresh();receipt.sameCandidatePassedAllPoses=true;receipt.sameCandidatePassed699=!fire;exported=true;
            }
            public void Complete(DragonRidersMatch match)
            {
                if(!exported)throw new InvalidOperationException("No qualified fixture was exported; inspect retained actual geometry failures.");receipt.nwhAfter=CaptureCurrentOriginalNwhClosure(match,receipt.hostRoot);receipt.sourceAfter=CaptureColdFixtureSources(match,receipt.species);receipt.nwhClosureExact=receipt.nwhBefore.Length==receipt.nwhAfter.Length&&receipt.nwhBefore.Zip(receipt.nwhAfter,(a,b)=>a.path==b.path&&a.nativeSha256==b.nativeSha256&&a.hostBytesAvailable==b.hostBytesAvailable&&a.hostSha256==b.hostSha256).All(equal=>equal);receipt.sourceBytesExact=receipt.sourceBefore.Length==receipt.sourceAfter.Length&&receipt.sourceBefore.Zip(receipt.sourceAfter,(a,b)=>a.path==b.path&&a.sha256==b.sha256).All(equal=>equal);receipt.hostContextQualified=false;receipt.nativeRuntimeAwakeObserved=false;
                if(!receipt.nwhClosureExact||!receipt.sourceBytesExact||Hash(Path.Combine(receipt.hostRoot,"Packages/packages-lock.json"))!=receipt.hostPackageLock||Hash("Packages/packages-lock.json")!=receipt.nativePackageLock)throw new InvalidOperationException("Fresh geometry/export changed chosen native NWH/source closure or separate HOST identity; no host-equivalence assumption permitted.");
                receipt.fixtureAssets=Directory.GetFiles(receipt.folder,"*",SearchOption.AllDirectories).OrderBy(path=>path,StringComparer.Ordinal).Select(path=>new FlightSourceHash{path=path.Replace('\\','/'),sha256=Hash(path)}).ToArray();File.WriteAllText(receipt.manifest,JsonUtility.ToJson(receipt,true));AssetDatabase.Refresh();createdPaths=Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories).Where(path=>!originalModePaths.Contains(path)).ToArray();
                foreach(var path in createdPaths){string normalized=path.Replace('\\','/');if(!normalized.StartsWith(receipt.folder+"/",StringComparison.Ordinal)&&normalized!=receipt.folder+".meta"&&normalized!="Assets/TruckDragonRiders/Tests/Generated.meta")throw new InvalidOperationException("Unexpected new asset outside explicit TEST fixture: "+path);}
            }
        }
        [Serializable]sealed class PristineRestorationProof
        {
            public string phase,expectedNativeConfiguration,actualNativeConfiguration;public int transforms,bodies,lines;public bool exact,nativeConfigurationExact,transformsExact,bodyFlagsExact,animatorEnabledExact,lineDataExact;public List<PristineJointDifference> differences=new List<PristineJointDifference>();
        }
        [Serializable]sealed class PristineJointDifference
        {public string path,expectedQuaternionBits,actualQuaternionBits;public Vector3 expectedPosition,actualPosition,expectedScale,actualScale;public Quaternion expectedRotation,actualRotation;public float normalizedDoubleAngleDegrees;}
        static void RestorePristineJointThroughNativeSerialization(SavedJoint authored)
        {
            bool positionChanged=!authored.joint.localPosition.Equals(authored.position),rotationChanged=!authored.joint.localRotation.Equals(authored.rotation),scaleChanged=!authored.joint.localScale.Equals(authored.scale);
            if(!positionChanged&&!rotationChanged&&!scaleChanged)return;
            // Transform.SetLocalPositionAndRotation normalizes even an
            // unchanged imported quaternion (V376 proves its one-ULP change).
            // Restore the authored SERIALIZED float fields without invoking
            // that normalized gameplay setter. Only this owned preview clone
            // is touched, after geometry acceptance, for prefab serialization.
            var serialized=new SerializedObject(authored.joint);serialized.Update();
            if(positionChanged)serialized.FindProperty("m_LocalPosition").vector3Value=authored.position;
            if(rotationChanged){var rotation=serialized.FindProperty("m_LocalRotation");rotation.FindPropertyRelative("x").floatValue=authored.rotation.x;rotation.FindPropertyRelative("y").floatValue=authored.rotation.y;rotation.FindPropertyRelative("z").floatValue=authored.rotation.z;rotation.FindPropertyRelative("w").floatValue=authored.rotation.w;}
            if(scaleChanged)serialized.FindProperty("m_LocalScale").vector3Value=authored.scale;serialized.ApplyModifiedPropertiesWithoutUndo();
            if(!authored.joint.localPosition.Equals(authored.position)||!authored.joint.localRotation.Equals(authored.rotation)||!authored.joint.localScale.Equals(authored.scale))throw new InvalidOperationException("Native serialized pristine TRS did not restore EXACT authored fields: "+authored.joint.name+" expectedQuaternion="+PristineFloatBits(authored.rotation.x,authored.rotation.y,authored.rotation.z,authored.rotation.w)+" actualQuaternion="+PristineFloatBits(authored.joint.localRotation.x,authored.joint.localRotation.y,authored.joint.localRotation.z,authored.joint.localRotation.w));
        }
        static string PristineFloatBits(params float[] values)=>string.Join(" ",values.Select(value=>BitConverter.ToInt32(BitConverter.GetBytes(value),0).ToString("X8")));
        sealed class PristineColdConfiguration
        {
            public readonly SavedJoint[] joints;readonly Rigidbody[] bodies;readonly bool[] kinematic,collisions;readonly MalbersAnimations.Controller.MAnimal animal;readonly string authoredNativeConfiguration;readonly Animator animator;readonly bool animatorEnabled;readonly LineRenderer[] lines;readonly Vector3[][] points;readonly bool[] lineEnabled;
            public PristineColdConfiguration(DragonCharacter actor)
            {
                joints=actor.GetComponentsInChildren<Transform>(true).Select(joint=>new SavedJoint{joint=joint,position=joint.localPosition,rotation=joint.localRotation,scale=joint.localScale}).ToArray();bodies=actor.GetComponentsInChildren<Rigidbody>(true);kinematic=bodies.Select(body=>body.isKinematic).ToArray();collisions=bodies.Select(body=>body.detectCollisions).ToArray();animal=actor.motor.animal;authoredNativeConfiguration=EditorJsonUtility.ToJson(animal);animator=actor.motor.animator;animatorEnabled=animator.enabled;lines=actor.GetComponentsInChildren<LineRenderer>(true);points=lines.Select(line=>{var value=new Vector3[line.positionCount];line.GetPositions(value);return value;}).ToArray();lineEnabled=lines.Select(line=>line.enabled).ToArray();
            }
            public void RestoreForSerialization()
            {
                EditorJsonUtility.FromJsonOverwrite(authoredNativeConfiguration,animal);foreach(var joint in joints)RestorePristineJointThroughNativeSerialization(joint);for(int i=0;i<bodies.Length;i++){bodies[i].isKinematic=kinematic[i];bodies[i].detectCollisions=collisions[i];}animator.enabled=animatorEnabled;for(int i=0;i<lines.Length;i++){lines[i].positionCount=points[i].Length;lines[i].SetPositions(points[i]);lines[i].enabled=lineEnabled[i];}
            }
            public PristineRestorationProof Inspect(string phase)
            {
                var proof=new PristineRestorationProof{phase=phase,transforms=joints.Length,bodies=bodies.Length,lines=lines.Length,nativeConfigurationExact=EditorJsonUtility.ToJson(animal)==authoredNativeConfiguration,bodyFlagsExact=bodies.Select((body,index)=>body.isKinematic==kinematic[index]&&body.detectCollisions==collisions[index]).All(value=>value),animatorEnabledExact=animator.enabled==animatorEnabled,lineDataExact=lines.Select((line,index)=>line.enabled==lineEnabled[index]&&line.positionCount==points[index].Length&&Enumerable.Range(0,line.positionCount).All(point=>line.GetPosition(point).Equals(points[index][point]))).All(value=>value)};
                foreach(var value in joints)if(!value.joint.localPosition.Equals(value.position)||!value.joint.localRotation.Equals(value.rotation)||!value.joint.localScale.Equals(value.scale))
                {
                    var actual=value.joint.localRotation;double dot=(double)actual.x*value.rotation.x+(double)actual.y*value.rotation.y+(double)actual.z*value.rotation.z+(double)actual.w*value.rotation.w,norm=Math.Sqrt(((double)actual.x*actual.x+(double)actual.y*actual.y+(double)actual.z*actual.z+(double)actual.w*actual.w)*((double)value.rotation.x*value.rotation.x+(double)value.rotation.y*value.rotation.y+(double)value.rotation.z*value.rotation.z+(double)value.rotation.w*value.rotation.w));proof.differences.Add(new PristineJointDifference{path=AnimationUtility.CalculateTransformPath(value.joint,joints[0].joint),expectedPosition=value.position,actualPosition=value.joint.localPosition,expectedScale=value.scale,actualScale=value.joint.localScale,expectedRotation=value.rotation,actualRotation=actual,expectedQuaternionBits=PristineFloatBits(value.rotation.x,value.rotation.y,value.rotation.z,value.rotation.w),actualQuaternionBits=PristineFloatBits(actual.x,actual.y,actual.z,actual.w),normalizedDoubleAngleDegrees=(float)(Math.Acos(Math.Min(1,Math.Abs(dot)/norm))*360/Math.PI)});
                }
                proof.transformsExact=proof.differences.Count==0;proof.exact=proof.nativeConfigurationExact&&proof.transformsExact&&proof.bodyFlagsExact&&proof.animatorEnabledExact&&proof.lineDataExact;if(!proof.nativeConfigurationExact){proof.expectedNativeConfiguration=authoredNativeConfiguration;proof.actualNativeConfiguration=EditorJsonUtility.ToJson(animal);}return proof;
            }
        }
        static FlightSourceHash[] CaptureColdFixtureSources(DragonRidersMatch match,string species="dragon-inferno")
        {
            var saved=match.racers.SelectMany(racer=>racer.choices).First(actor=>actor.GetComponent<DragonRiderSaddle>().speciesId==species);var paths=AssetDatabase.GetDependencies(ActorPath(species),true).Concat(AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(saved.motor.animator.runtimeAnimatorController),true)).Concat(new[]{DragonRidersAuthoring.ScenePath,"Packages/packages-lock.json","Packages/manifest.json","ProjectSettings/ProjectVersion.txt"}).Concat(Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories).Where(path=>!path.Replace('\\','/').StartsWith("Assets/TruckDragonRiders/Tests/Generated",StringComparison.Ordinal))).Concat(Directory.GetFiles("Assets/TruckDragonKingdom/Scripts","*",SearchOption.AllDirectories)).Concat(Directory.GetFiles("Assets/TruckDragonKingdom/Editor","*.cs",SearchOption.AllDirectories)).Concat(Directory.GetFiles("Assets/TruckDragonKingdom/Tests","*",SearchOption.AllDirectories)).Concat(Directory.GetFiles("Assets/TruckDragonKingdom/PrivateLicensedAnimalController","*.cs",SearchOption.AllDirectories));var logical=new HashSet<string>(StringComparer.Ordinal);
            foreach(var item in paths){string path=item.Replace('\\','/');if(File.Exists(CurrentRiderPhysicalDependency(path)))logical.Add(path);if(File.Exists(CurrentRiderPhysicalDependency(path)+".meta"))logical.Add(path+".meta");}return logical.OrderBy(path=>path,StringComparer.Ordinal).Select(path=>new FlightSourceHash{path=path,sha256=Hash(CurrentRiderPhysicalDependency(path))}).ToArray();
        }
        public static string InspectAndExportTestOnlyMotorRootInfernoAfterFresh699Geometry(string ownedEvidence,string materialEvidence,string startupEvidence,string folder)
        {
            if(coldFixtureExport!=null)throw new InvalidOperationException("No nested test-only fixture export.");try{coldFixtureExport=new ColdFixtureExport(folder,ownedEvidence);return InspectOwnedMotorRootInfernoGeometryAndActualCourseEnvelopeWithoutSavingAssets(ownedEvidence,materialEvidence,startupEvidence);}finally{coldFixtureExport=null;}
        }
        public static string InspectAndExportTestOnlyMotorRootFireAfterFresh537Geometry(string geometryEvidence,string headExitEvidence,string folder)
        {
            if(coldFixtureExport!=null)throw new InvalidOperationException("No nested test-only fixture export.");try{coldFixtureExport=new ColdFixtureExport(folder,geometryEvidence,"dragon-fire");return InspectOwnedMotorRootFireGeometryAndActualCourseEnvelopeWithoutSavingAssets(geometryEvidence,headExitEvidence);}finally{coldFixtureExport=null;}
        }
    }
}
#endif
