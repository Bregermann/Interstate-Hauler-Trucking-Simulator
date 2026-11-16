#if UNITY_EDITOR && VISTA
using System;
using NUnit.Framework;

public sealed class DragonKingdomOwnedArchitectureMeshBindingsTests
{
    [Test,Timeout(120000)]
    public void ExactOwnedLadderMeshOverrideAndCollisionSurviveRealPrefabSaveReloadWithoutChangingVendorSource()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);string evidence=(string)type.GetMethod("CaptureOwnedLadderBindingPersistenceProbe").Invoke(null,null);Assert.That(evidence,Does.Contain("4300002"));TestContext.WriteLine(evidence);
    }
}
#endif
