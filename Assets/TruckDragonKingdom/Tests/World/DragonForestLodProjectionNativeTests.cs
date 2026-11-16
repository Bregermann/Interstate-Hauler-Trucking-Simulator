#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEditor;
public sealed class DragonForestLodProjectionNativeTests
{
    [Test,Timeout(180000)]public void ActualRetainedForestCameraPosesReportAllThreeLodsWithoutTuning()
    {
        Assert.That(EditorApplication.isPlayingOrWillChangePlaymode,Is.False);
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonForestMaterialAuthoring, Assembly-CSharp-Editor",true);
        Assert.That((int)type.GetMethod("CaptureForestLodProjection").Invoke(null,null),Is.GreaterThan(0),"Actual source trees required. Culled LOD predictions remain diagnostic, not automatic threshold changes.");
    }
}
#endif
