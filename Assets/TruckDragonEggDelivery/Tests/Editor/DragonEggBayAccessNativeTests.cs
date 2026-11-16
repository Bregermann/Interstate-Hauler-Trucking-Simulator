#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEditor;

public sealed class DragonEggBayAccessNativeTests
{
    [Test,Timeout(240000)]
    public void SavedFourNestBaysHaveContinuousActualFullNwhRoadConnectors()
    {
        Assert.That(EditorApplication.isPlayingOrWillChangePlaymode,Is.False,"Run this read-only physical query fixture in native EditMode.");
        var probe=Type.GetType("LWS.TruckDragonEggDelivery.Editor.EggLoadingBayAccessProbe, Assembly-CSharp-Editor",true);
        int failures=(int)probe.GetMethod("CaptureSavedBays").Invoke(null,null);
        Assert.That(failures,Is.Zero,"Inspect retained EggLoadingBayAccess/saved-bays.json. A clear parked bay does not establish a continuously supported full-NWH route; no domain/AI/physics/scoring gate was changed.");
    }
}
#endif
