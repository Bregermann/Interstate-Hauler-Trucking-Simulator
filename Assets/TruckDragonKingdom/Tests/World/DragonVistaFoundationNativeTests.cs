#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// Deliberate native measurement, not a test that declares the world accepted
// merely because a vendor task completed. Nonzero/coverage results are retained
// even if they expose a broken production setup.
public sealed class DragonVistaFoundationNativeTests
{
    // JsonUtility fills the receipt DTO fields from actual native evidence.
#pragma warning disable CS0649
    [Serializable] sealed class Receipt
    {
        public bool savedSourceBytesPreserved,originalScenePreserved,temporaryObjectsRemoved;
        public Run[] runs;
    }
    [Serializable] sealed class Run
    {
        public string id,status;
        public int collectedTiles,collectedBiomes,heightPopulationCallbacks;
        public Dataset rawGraphHeight,rawBiomeMask;
        public Dataset[] blendedTileOutputs,ungradedTiles;
    }
    [Serializable] sealed class Dataset { public int samples,nonFinite;public string sha256,png; }
#pragma warning restore CS0649

    [UnityTest,Timeout(1500000)]
    public IEnumerator InstalledVistaUngradedCoverageProbeRecordsActualGraphAndFourTileOutputsWithoutSourceWrites()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonVistaFoundationProbe, Assembly-CSharp-Editor",true);
        var method=type.GetMethod("CompareUngradedVendorOutput",BindingFlags.Public|BindingFlags.Static);
        Assert.That(method,Is.Not.Null,"Installed VISTA-backed explicit diagnostic is required.");
        yield return (IEnumerator)method.Invoke(null,null);
        string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");Assert.That(at>=0&&at+1<args.Length,Is.True);
        string output=Path.Combine(args[at+1],"VistaFoundation");var receipt=JsonUtility.FromJson<Receipt>(File.ReadAllText(Path.Combine(output,"report.json")));
        Assert.That(receipt.savedSourceBytesPreserved&&receipt.originalScenePreserved&&receipt.temporaryObjectsRemoved,Is.True,"Measurement must leave original assets and scenes unchanged.");
        Assert.That(receipt.runs,Has.Length.EqualTo(2));
        Assert.That(receipt.runs[0].id,Is.EqualTo("exact_builder_coverage"));Assert.That(receipt.runs[1].id,Is.EqualTo("full_coverage_counterfactual"));
        foreach(var run in receipt.runs)
        {
            Assert.That(run.status,Is.EqualTo("measured_not_world_accepted"));Assert.That(run.collectedTiles,Is.EqualTo(4));Assert.That(run.collectedBiomes,Is.EqualTo(1));Assert.That(run.heightPopulationCallbacks,Is.EqualTo(4));
            Assert.That(run.blendedTileOutputs,Has.Length.EqualTo(4));Assert.That(run.ungradedTiles,Has.Length.EqualTo(4));
            Check(run.rawGraphHeight,output);Check(run.rawBiomeMask,output);
            foreach(var dataset in run.blendedTileOutputs)Check(dataset,output);
            foreach(var dataset in run.ungradedTiles)Check(dataset,output);
        }
    }
    static void Check(Dataset dataset,string output)
    {
        Assert.That(dataset,Is.Not.Null);Assert.That(dataset.samples,Is.GreaterThan(0));Assert.That(dataset.nonFinite,Is.Zero,"Nonfinite vendor output is not valid measurement data.");
        Assert.That(dataset.sha256,Has.Length.EqualTo(64));Assert.That(File.Exists(Path.Combine(output,dataset.png)),Is.True);
    }
}
#endif
