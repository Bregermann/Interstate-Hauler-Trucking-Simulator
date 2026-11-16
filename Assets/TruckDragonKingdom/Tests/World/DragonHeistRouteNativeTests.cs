#if UNITY_EDITOR
using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

public sealed class DragonHeistRouteNativeTests
{
    [Serializable]sealed class Support {public bool supported;}
    [Serializable]sealed class Route {public int slot,unsupported;public float length,straightDistance,largestGap;}
    [Serializable]sealed class Receipt {public Support receptionSupport;public Route[] routes;public bool sceneBytesPreserved;}
    [Test]public void SavedReceptionUsesSupportedLocalJunctionRoutesInsteadOfAThreeKilometreNetworkLap()
    {
        var probe=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonHeistAccessProbe, Assembly-CSharp-Editor",true);
        probe.GetMethod("CaptureSavedReceptionAccess").Invoke(null,null);
        string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");Assert.That(at,Is.GreaterThanOrEqualTo(0));
        var evidence=JsonUtility.FromJson<Receipt>(File.ReadAllText(Path.Combine(args[at+1],"HeistRoadAccess","saved-reception.json")));
        Assert.That(evidence.sceneBytesPreserved&&evidence.receptionSupport.supported,Is.True);Assert.That(evidence.routes.Length,Is.EqualTo(4));
        foreach(var route in evidence.routes)
        {
            Assert.That(route.unsupported,Is.Zero,"Every actual authored route point must retain physical support.");
            Assert.That(route.length,Is.LessThan(route.straightDistance*1.5f),"Nearby reception on the same physical junction must use its actual local roads: slot "+route.slot);
            Assert.That(route.largestGap,Is.LessThan(30),"Retain sampled continuous road geometry, not a direct shortcut across the world.");
        }
    }
}
#endif
