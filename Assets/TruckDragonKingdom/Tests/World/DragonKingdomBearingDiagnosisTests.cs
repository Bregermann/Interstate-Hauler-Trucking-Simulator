#if UNITY_EDITOR
using System;
using NUnit.Framework;

public sealed class DragonKingdomBearingDiagnosisTests
{
    [Test,Timeout(120000)]
    public void ActualFailed32ForestBearingHeightsRequireExactPersistedNativeReceiptHashBeforeDiagnosis()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
        type.GetMethod("DiagnoseFailed32ForestBearingFromCommandLine").Invoke(null,null);var proof=type.GetProperty("LastBearingHeightDiagnosis").GetValue(null);
        object Field(string name)=>proof.GetType().GetField(name).GetValue(proof);
        Assert.That((bool)Field("exactFailedNativeHeightfield"),Is.True);Assert.That((bool)Field("sourceBytesExact"),Is.True);Assert.That(((Array)Field("samples")).Length,Is.EqualTo(5));
        Assert.That((string)Field("status"),Is.EqualTo("EXACT_FAILED32_HEIGHTS_MEASURED_COLLIDER_BASE_RAYS_AND_PHYSICAL_REPAIR_PENDING"));
    }
}
#endif
