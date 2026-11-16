#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEngine;
public sealed class DragonRiderNwhSizeControlTests
{
#pragma warning disable CS0649
    [Serializable]sealed class Proof
    {
        public bool ordinaryRootMotionAccepted,ordinaryFiveMillimeterCabArticulationAccepted,rootScaleChangeRejected,colliderSizeChangeRejected,colliderCenterChangeRejected,colliderScaleChangeRejected,bodyMassChangeRejected,sourceBytesExact,originalPhysicsExact;public float maximumPositiveWorldEdgeDrift;
    }
#pragma warning restore CS0649
    [Test]
    public void ActualOriginalNwhDimensionsRemainFixedWhileDrivingAndCabArticulationAreAllowed()
    {
        var author=Type.GetType("LWS.TruckDragonRiders.Editor.DragonRiderSaddleAuthoring, Assembly-CSharp-Editor",true);string json=(string)author.GetMethod("InspectOriginalNwhMotionAndSizeControlsWithoutSavingAssets").Invoke(null,null);TestContext.WriteLine(json);var proof=JsonUtility.FromJson<Proof>(json);
        Assert.That(proof.sourceBytesExact&&proof.originalPhysicsExact,Is.True);Assert.That(proof.ordinaryRootMotionAccepted&&proof.ordinaryFiveMillimeterCabArticulationAccepted,Is.True);Assert.That(proof.maximumPositiveWorldEdgeDrift,Is.LessThanOrEqualTo(.00001f));Assert.That(proof.rootScaleChangeRejected&&proof.colliderSizeChangeRejected&&proof.colliderCenterChangeRejected&&proof.colliderScaleChangeRejected&&proof.bodyMassChangeRejected,Is.True);
    }
}
#endif
