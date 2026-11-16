#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEngine;
public sealed class DragonRiderCurrentProofIdentityTests
{
#pragma warning disable CS0649
    [Serializable]sealed class Report{public string failure;public bool exactGeometryInputs,exactRuntimeInputs,sceneUnchanged,nwhUnchanged,productionAdoptionAllowed;public Dependency[] geometryDependencies,runtimeDependencies,originalNwhReferencedDependencies,actualNativeSourceBaselines;}
    [Serializable]sealed class Dependency{public string path,sha256;}
#pragma warning restore CS0649
    [Serializable]sealed class RoundTrip{public string failure;public bool sourceExact,sceneExact,nwhExact;public Restoration[] proofs;}
    [Serializable]sealed class Restoration{public string phase,expectedNativeConfiguration,actualNativeConfiguration;public bool exact,nativeConfigurationExact,transformsExact,bodyFlagsExact,animatorEnabledExact,lineDataExact;public int transforms,bodies,lines;public Difference[] differences;}
    [Serializable]sealed class Difference{public string path,expectedQuaternionBits,actualQuaternionBits;public float normalizedDoubleAngleDegrees;}
    [Test,Timeout(180000)]
    public void PristineMotorRootSerializationRoundTripRecordsExactNativeDifferencesWithoutSavingAssets()
    {
        var args=Environment.GetCommandLineArgs();int geometry=Array.IndexOf(args,"-tsRiderMotorRootGeometryEvidence"),material=Array.IndexOf(args,"-tsRiderPointwiseEvidence");Assert.That(geometry>=0&&geometry+1<args.Length&&material>=0&&material+1<args.Length,Is.True);var type=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);var report=JsonUtility.FromJson<RoundTrip>((string)type.GetMethod("InspectPristineMotorRootSerializationRoundTripWithoutSavingAssets").Invoke(null,new object[]{args[geometry+1],args[material+1]}));Assert.That(report.failure,Is.Null.Or.Empty);Assert.That(report.sourceExact&&report.sceneExact&&report.nwhExact,Is.True);Assert.That(report.proofs.Length,Is.EqualTo(4));foreach(var proof in report.proofs){Assert.That(proof.transforms,Is.GreaterThan(50));Assert.That(proof.bodies,Is.GreaterThan(0));Assert.That(proof.lines,Is.GreaterThan(0));Assert.That(proof.exact,Is.EqualTo(proof.nativeConfigurationExact&&proof.transformsExact&&proof.bodyFlagsExact&&proof.animatorEnabledExact&&proof.lineDataExact));Assert.That(proof.transformsExact,Is.EqualTo(proof.differences.Length==0));if(!proof.nativeConfigurationExact){Assert.That(proof.expectedNativeConfiguration,Is.Not.Empty);Assert.That(proof.actualNativeConfiguration,Is.Not.Empty);}if(proof.phase=="ORIGINAL_GEOMETRY_REST_SETTER_KNOWN_ONE_ULP_NEGATIVE_CONTROL"){Assert.That(proof.exact,Is.False);Assert.That(proof.nativeConfigurationExact&&proof.bodyFlagsExact&&proof.animatorEnabledExact&&proof.lineDataExact,Is.True);Assert.That(proof.differences.Length,Is.EqualTo(1));var change=proof.differences[0];Assert.That(change.path,Does.EndWith("/RigTail2Gizmo"));Assert.That(change.expectedQuaternionBits,Is.EqualTo("BE708D63 BF2ABCE2 3E708D6C 3F2ABCE4"));Assert.That(change.actualQuaternionBits,Is.EqualTo("BE708D63 BF2ABCE3 3E708D6C 3F2ABCE5"));}else{Assert.That(proof.exact,Is.True,"All authored TRS/native configuration/flags/lines must remain EXACT; no broad tolerance or excluded transform.");}TestContext.WriteLine("ACTUAL_PRISTINE_ROUNDTRIP phase="+proof.phase+" exact="+proof.exact+" native="+proof.nativeConfigurationExact+" transforms="+proof.transformsExact+" bodyflags="+proof.bodyFlagsExact+" animator="+proof.animatorEnabledExact+" lines="+proof.lineDataExact+" transformDifferences="+proof.differences.Length+" DIAGNOSTIC_NOT_FIXTURE_OR_GEOMETRY_ADOPTION");}
    }
    [Test]
    public void ExactProjectPackageConfigurationResolvesLocallyWhileActualUpmSourcesRemainStrict()
    {
        var type=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);Assert.That((bool)type.GetMethod("VerifyActualProjectConfigurationAndStrictPackageSourceResolution").Invoke(null,null),Is.True);
    }
    [Test,Timeout(180000)]
    public void CurrentFireGeometryAndRuntimeProofsRequireExactWholeSceneAndOriginalNwhReferencedSourceIdentity()
    {
        var args=Environment.GetCommandLineArgs();int geometry=Array.IndexOf(args,"-tsRiderFireGeometryIdentityEvidence"),runtime=Array.IndexOf(args,"-tsRiderFireRuntimeIdentityEvidence");Assert.That(geometry>=0&&geometry+1<args.Length&&runtime>=0&&runtime+1<args.Length,Is.True,"Exact actual passed native geometry and runtime receipt directories required.");var type=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);var report=JsonUtility.FromJson<Report>((string)type.GetMethod("InspectCurrentFireQualifiedProofIdentityWithoutSavingAssets").Invoke(null,new object[]{args[geometry+1],args[runtime+1]}));
        Assert.That(report.failure,Is.Null.Or.Empty);Assert.That(report.exactGeometryInputs&&report.exactRuntimeInputs&&report.sceneUnchanged&&report.nwhUnchanged,Is.True);Assert.That(report.productionAdoptionAllowed,Is.False,"Isolated geometry/cabs cannot authorize current course or cold-load production adoption.");Assert.That(report.geometryDependencies.Length,Is.GreaterThanOrEqualTo(1));Assert.That(report.actualNativeSourceBaselines.Length,Is.GreaterThan(5));Assert.That(report.runtimeDependencies.Length,Is.GreaterThan(5));Assert.That(report.originalNwhReferencedDependencies.Length,Is.GreaterThan(5));TestContext.WriteLine("CURRENT_SOURCE_WORLD_PROOF_IDENTITY geometry="+report.geometryDependencies.Length+" runtime="+report.runtimeDependencies.Length+" originalNwhReferencedAssets="+report.originalNwhReferencedDependencies.Length+" NO_CURRENT_COURSE_COLD_LOAD_PRODUCTION_ACCEPTANCE");
    }
}
#endif
