#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEngine;
public sealed class DragonRiderMaterialFrameTests
{
#pragma warning disable CS0649
    [Serializable]sealed class Report{public string failure;public bool sourceExact,sceneExact,nwhExact,baselineReproduced;public float maximumTriangleReproductionError,actualMaterialYaw,physicalYaw,proposedYaw,rotationDifference;public Variant[] variants;}
    [Serializable]sealed class Variant{public string frame;public int skins,triangles,boxes,nativeUnitSat;public bool allFourActualWheels,unbounded;public float pitch,bank,yaw,largestFiniteForbiddenUpper,onePoseDerivedFactor,maximumWheelGap;}
#pragma warning restore CS0649
    [Test,Timeout(300000)]
    public void ActualWalk45MaterialLongitudinalAxisAndUnchangedClampFrameAreComparedAgainstExactSourceWitness()
    {
        var args=Environment.GetCommandLineArgs();int owned=Array.IndexOf(args,"-tsRiderAllActionEvidence"),material=Array.IndexOf(args,"-tsRiderPointwiseEvidence");Assert.That(owned>=0&&owned+1<args.Length&&material>=0&&material+1<args.Length,Is.True);var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);var report=JsonUtility.FromJson<Report>((string)author.GetMethod("InspectActualInfernoWalkMaterialFrameWithoutSavingAssets").Invoke(null,new object[]{args[owned+1],args[material+1]}));Assert.That(report.failure,Is.Null.Or.Empty);Assert.That(report.sourceExact&&report.sceneExact&&report.nwhExact&&report.baselineReproduced,Is.True);Assert.That(report.maximumTriangleReproductionError,Is.LessThan(.001f));Assert.That(report.variants.Length,Is.EqualTo(2));foreach(var row in report.variants){Assert.That(row.skins,Is.GreaterThan(0));Assert.That(row.triangles,Is.GreaterThan(2500));Assert.That(row.boxes,Is.EqualTo(2));Assert.That(row.allFourActualWheels,Is.True);Assert.That(row.maximumWheelGap,Is.LessThanOrEqualTo(.03f));Assert.That(Mathf.Abs(row.pitch),Is.LessThanOrEqualTo(20.001f));Assert.That(Mathf.Abs(row.bank),Is.LessThanOrEqualTo(25.001f));Assert.That(Mathf.Abs(row.yaw),Is.LessThanOrEqualTo(15.001f));TestContext.WriteLine("ACTUAL_SINGLE_POSE_MATERIAL_FRAME "+row.frame+" actualUnitSAT="+row.nativeUnitSat+" largestFiniteInterval="+row.largestFiniteForbiddenUpper+" unbounded="+row.unbounded+" onePoseFactor="+row.onePoseDerivedFactor);}
        TestContext.WriteLine("ACTUAL_MATERIAL_FORWARD_YAW="+report.actualMaterialYaw+" currentYaw="+report.physicalYaw+" proposedYaw="+report.proposedYaw+" deltaDegrees="+report.rotationDifference+" NO_RUNTIME_OR_FULL699_FRAME_ADOPTION_ACCEPTANCE");
    }
}
#endif
