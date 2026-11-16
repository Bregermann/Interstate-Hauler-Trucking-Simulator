#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class DragonKingdomVistaFinishNativeTests
{
#pragma warning disable CS0649
    [Serializable]sealed class Evidence
    {
        public string status,inputSha256,outputSha256,maskSha256,nodeJsonSha256;
        public int heldSamples,changedSamples,nonFinite,protectedSamplesChanged,roadGatesExecuted,startGatesExecuted,extractionGatesExecuted;
        public float maximumDelta;
        public bool vendorNodeSettingsExact,sourceBytesPreserved,originalPhysicsExact,originalTerrainBindingsRestored,originalHeightmapsExact,originalSceneSetupRestored,originalLayerAlphaHoleBindingsExact,physicalGatesPassed;
        public Frame[] frames;public Tile[] tiles;
    }
    [Serializable]sealed class Frame{public string png,sha256,stage,view;}
    [Serializable]sealed class Tile{public string beforeHeightSha256,afterHeightSha256,restoredHeightSha256;}
#pragma warning restore CS0649
    [UnityTest,Timeout(600000)]
    public IEnumerator TechnicalWorld29UsesActualAuthoredVistaErosionWithProtectedPhysicalFootingsAndExactRestoration()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRegionalWorldIntegration, Assembly-CSharp-Editor",true);
        yield return (IEnumerator)type.GetMethod("CaptureActualBoundedVistaTerrainFinish").Invoke(null,null);
        string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");Assert.That(at>=0&&at+1<args.Length,Is.True);
        string output=Path.Combine(args[at+1],"ActualVistaTerrainFinish");var proof=JsonUtility.FromJson<Evidence>(File.ReadAllText(Path.Combine(output,"actual-vista-terrain-finish.json")));
        Assert.That(proof.status,Is.EqualTo("MEASURED_REQUIRES_ACTUAL_SAME_CAMERA_VISUAL_REVIEW_NOT_ADOPTED"));
        Assert.That(proof.vendorNodeSettingsExact&&proof.sourceBytesPreserved&&proof.originalPhysicsExact&&proof.originalTerrainBindingsRestored&&proof.originalHeightmapsExact&&proof.originalSceneSetupRestored&&proof.originalLayerAlphaHoleBindingsExact&&proof.physicalGatesPassed,Is.True,"No source, scene, collision, original height or data-binding waiver.");
        Assert.That(proof.nonFinite,Is.Zero);Assert.That(proof.protectedSamplesChanged,Is.Zero);Assert.That(proof.heldSamples,Is.GreaterThan(0));Assert.That(proof.changedSamples,Is.GreaterThan(0));Assert.That(proof.maximumDelta,Is.LessThanOrEqualTo(6));
        Assert.That(proof.roadGatesExecuted,Is.EqualTo(3));Assert.That(proof.startGatesExecuted,Is.EqualTo(3));Assert.That(proof.extractionGatesExecuted,Is.EqualTo(3));
        foreach(string hash in new[]{proof.inputSha256,proof.outputSha256,proof.maskSha256,proof.nodeJsonSha256})Assert.That(hash,Has.Length.EqualTo(64));
        Assert.That(proof.tiles,Has.Length.EqualTo(4));foreach(var tile in proof.tiles){Assert.That(tile.beforeHeightSha256,Is.EqualTo(tile.restoredHeightSha256));Assert.That(tile.afterHeightSha256,Has.Length.EqualTo(64));}
        Assert.That(proof.frames,Has.Length.EqualTo(15));Assert.That(proof.frames.Select(f=>f.view).Distinct().Count(),Is.EqualTo(5));
        foreach(string stage in new[]{"before","actual-vista-bounded","restored"})Assert.That(proof.frames.Count(f=>f.stage==stage),Is.EqualTo(5));
        foreach(var frame in proof.frames){Assert.That(File.Exists(frame.png),Is.True);Assert.That(frame.sha256,Has.Length.EqualTo(64));}
        // These are actual diagnostic images and unchanged physical gates. A pass
        // intentionally does not declare the rejected world artistically complete.
    }
}
#endif
