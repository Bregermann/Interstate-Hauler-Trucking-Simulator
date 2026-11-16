#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class DragonKingdomThermalFinishNativeTests
{
#pragma warning disable CS0649
    [Serializable]sealed class Proof
    {public string status;public bool actualProtectedSamplesExact,highlandMassAndPeakRetained,originalQualityAllowancesPreserved,beforeAndRestoredQualityExact,actualThermalQualityPassed,allRestorationGatesPassed;public float actualMaximumDelta,maximumNativeSeamDelta,maximumNativeHeldDelta;public Measurements measurements;}
    [Serializable]sealed class Measurements{public int nonFinite,protectedSamplesChanged,roadGatesExecuted,startGatesExecuted,extractionGatesExecuted,changedSamples;public Frame[] frames;public bool physicalGatesPassed;}
    [Serializable]sealed class Frame{public string png,sha256,view,stage;}
#pragma warning restore CS0649
    [UnityTest,Timeout(900000)]
    public IEnumerator ActualPublicThermal3RetainsOriginalRoadStartContactQualityAndSourceGatesWithSameCameraEvidence()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);yield return (IEnumerator)type.GetMethod("CaptureActualThermalLandformFinish").Invoke(null,null);
        string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");Assert.That(at>=0&&at+1<args.Length,Is.True);string output=Path.Combine(args[at+1],"ActualThermalLandformFinish");var proof=JsonUtility.FromJson<Proof>(File.ReadAllText(Path.Combine(output,"actual-thermal-landform-finish.json")));
        Assert.That(proof.status,Is.EqualTo("MEASURED_REQUIRES_ACTUAL_15_FRAME_VISUAL_AND_SCENERY_FOUNDATION_REVIEW_NOT_ADOPTED"));Assert.That(proof.allRestorationGatesPassed&&proof.originalQualityAllowancesPreserved&&proof.beforeAndRestoredQualityExact&&proof.actualThermalQualityPassed&&proof.highlandMassAndPeakRetained&&proof.actualProtectedSamplesExact,Is.True);
        Assert.That(proof.maximumNativeHeldDelta,Is.Zero);Assert.That(proof.maximumNativeSeamDelta,Is.LessThanOrEqualTo(.04f));Assert.That(proof.actualMaximumDelta,Is.LessThanOrEqualTo(60.02f),"The recorded native16-bit terrain storage quantum is below0.01m; this bounds its representation, not a larger authored displacement.");
        var m=proof.measurements;Assert.That(m.nonFinite,Is.Zero);Assert.That(m.protectedSamplesChanged,Is.Zero);Assert.That(m.changedSamples,Is.GreaterThan(0));Assert.That(m.physicalGatesPassed,Is.True);Assert.That(new[]{m.roadGatesExecuted,m.startGatesExecuted,m.extractionGatesExecuted},Is.All.EqualTo(3));
        Assert.That(m.frames,Has.Length.EqualTo(15));Assert.That(m.frames.Select(f=>f.view).Distinct().Count(),Is.EqualTo(5));foreach(var frame in m.frames){Assert.That(File.Exists(frame.png),Is.True);Assert.That(frame.sha256,Has.Length.EqualTo(64));}
        // Native terrain/source acceptance is distinct from lead image review.
        // Scenery fit and a finished visual composition remain explicitly open.
    }
}
#endif
