#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEngine;
public sealed class DragonRiderInfernoFitTests
{
#pragma warning disable CS0649
    [Serializable]sealed class Report{public string failure;public bool sourceBytesExact,nwhExact,sceneBytesExact,qualifiedNativeGeometry,conservativeEnvelopeAndStartupClear,derivedStrictPatch;public int poses;public float factor,derivedRearward;public Bounds fullEnvelope,skinEnvelope,nwhEnvelope,harnessEnvelope;public Triangle[] limitingTriangles;public Gate[] gates;public Contact[] startupContacts;public string calibrationFailure;}
    [Serializable]sealed class Triangle{public int triangle;public string sourceGuid,incomingGuid;public float incomingWeight;public Vector3 actualNormal,physicalUp;}
    [Serializable]sealed class Gate{public bool fullEnvelopeFits;public float radius,maximumConservativeProjectedRadius;}
    [Serializable]sealed class Contact{public int start,otherStart;public float depth;}
#pragma warning restore CS0649
    [Test,Timeout(1800000)]
    public void QualifiedInfernoAll699FullAssemblyEnvelopeAndOriginalWingRootContactsAreMeasuredWithoutAdoptingOversize()
    {
        var args=Environment.GetCommandLineArgs();int evidence=Array.IndexOf(args,"-tsRiderInfernoSizeEvidence"),startup=Array.IndexOf(args,"-tsRiderStartupEvidence");Assert.That(evidence>=0&&evidence+1<args.Length&&startup>=0&&startup+1<args.Length,Is.True,"Actual passed source/startup/native geometry receipts required.");
        var type=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);var report=JsonUtility.FromJson<Report>((string)type.GetMethod("InspectQualifiedInfernoFullEnvelopeAndAnatomicalFitWithoutSavingAssets").Invoke(null,new object[]{args[evidence+1],args[startup+1]}));
        Assert.That(report.failure,Is.Null.Or.Empty);Assert.That(report.qualifiedNativeGeometry&&report.sourceBytesExact&&report.nwhExact&&report.sceneBytesExact,Is.True);Assert.That(report.poses,Is.EqualTo(699));Assert.That(report.limitingTriangles.Length,Is.EqualTo(2));Assert.That(report.limitingTriangles[0].triangle,Is.EqualTo(2493));Assert.That(report.limitingTriangles[1].triangle,Is.EqualTo(2495));foreach(var triangle in report.limitingTriangles){Assert.That(triangle.sourceGuid,Is.Not.Null.And.Not.Empty);Assert.That(triangle.incomingGuid,Is.Not.Null.And.Not.Empty);Assert.That(triangle.incomingWeight,Is.EqualTo(.25f));}Assert.That(report.gates.Length,Is.GreaterThanOrEqualTo(8));Assert.That(report.fullEnvelope.size.x,Is.GreaterThanOrEqualTo(report.skinEnvelope.size.x));Assert.That(report.nwhEnvelope.size.magnitude,Is.GreaterThan(1));Assert.That(report.harnessEnvelope.size.magnitude,Is.GreaterThan(1));
        TestContext.WriteLine("ACTUAL_ALL699_INFERNO_FIT_DIAGNOSTIC factor="+report.factor+" assemblyDimensions="+report.fullEnvelope.size+" currentStartContacts="+report.startupContacts.Length+" conservativeRingFit="+report.conservativeEnvelopeAndStartupClear+" derivedStrictPatch="+report.derivedStrictPatch+" sourceDerivedRear="+report.derivedRearward+" calibrationFailure="+report.calibrationFailure+" NOT_RUNTIME_OR_COURSE_ACCEPTANCE");
    }
}
#endif
