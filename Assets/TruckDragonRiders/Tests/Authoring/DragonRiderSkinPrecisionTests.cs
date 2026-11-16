#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
public sealed class DragonRiderSkinPrecisionTests
{
    [Test]public void KnownFarWorldRotatedAndNonUnitWeightControlsRetainExactSourceSemantics()
    {
        var helper=Type.GetType("DragonRiderSkinPrecisionObservation, LWS.TruckDragonRiders.NativeTests",true);
        var control=helper.GetMethod("RequireAnalyticControls",BindingFlags.Public|BindingFlags.Static);
        Assert.That(control,Is.Not.Null,"Use the explicitly frozen standalone native precision helper.");
        control.Invoke(null,null);
        var hierarchy=helper.GetMethod("RequireHierarchyControls",BindingFlags.Public|BindingFlags.Static);Assert.That(hierarchy,Is.Not.Null);hierarchy.Invoke(null,null);
    }
}
#endif
