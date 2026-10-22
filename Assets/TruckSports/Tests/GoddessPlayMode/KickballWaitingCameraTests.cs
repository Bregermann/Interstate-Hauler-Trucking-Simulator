#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class KickballWaitingCameraTests
{
    [UnityTest, Timeout(180000)]
    public IEnumerator FourOwnedWaitingInputsPauseReconnectAndRealAITurnHandoff()
    {
        var root = new GameObject("Owned Kickball waiting PlayMode acceptance"); UnityEngine.Object.DontDestroyOnLoad(root);
        root.AddComponent(Type.GetType("LWS.TruckSports.SportsProbeInputIsolation, Assembly-CSharp", true));
        var type = Type.GetType("LWS.TruckKickball.KickballWaitingRepairProbe, Assembly-CSharp", true);
        var probe = root.AddComponent(type);
        try
        {
            yield return (IEnumerator)type.GetMethod("Run").Invoke(probe, null);
            Assert.That((bool)type.GetProperty("Complete").GetValue(probe), Is.True);
            Assert.That((int)type.GetProperty("Errors").GetValue(probe), Is.Zero);
            Assert.That((int)type.GetProperty("Checks").GetValue(probe), Is.GreaterThanOrEqualTo(20));
        }
        finally { UnityEngine.Object.DestroyImmediate(root); Time.timeScale = 1; }
    }
}
#endif
