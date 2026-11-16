#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class DragonVistaRegionalFoundationNativeTests
{
    // Actual native JSON supplies these fields.
#pragma warning disable CS0649
    [Serializable] sealed class Receipt
    {
        public string status,candidateFolder,privateGraphsFolder,prefab;
        public bool sourceBytesPreserved,originalScenePreserved,temporaryObjectsRemoved;
        public int effectiveGraphResolution,effectiveHeightResolution,heightPopulationCallbacks;
        public TerrainRecord[] terrains;public Biome[] biomes;public Region[] regions;public Water[] water;public string[] validationIssues;
    }
    [Serializable] sealed class TerrainRecord {public string path,png,floatMap;public Vector3 position,size;public int resolution,nonFinite,ceilingSamples,over200;public float maximumY;public Cell[] coverage;}
    [Serializable] sealed class Cell {public int count;}
    [Serializable] sealed class Biome {public string id,graphPath,kind;public float falloff;}
    [Serializable] sealed class Region {public string id;public float requestedY,actualY,neighborhoodMinimumY,neighborhoodMaximumY;public int neighborhoodSamples;}
    [Serializable] sealed class Water {public string id;public float waterY,actualBedY;}
#pragma warning restore CS0649

    [UnityTest,Timeout(1500000)]
    public IEnumerator ExplicitRegionalVistaCandidateMeasuresRealPregradingMassAndWaterBasins()
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonVistaRegionalFoundationAuthoring, Assembly-CSharp-Editor",true);
        yield return (IEnumerator)type.GetMethod("CreateReviewCandidate",BindingFlags.Static|BindingFlags.Public).Invoke(null,null);
        string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");Assert.That(at>=0&&at+1<args.Length,Is.True);int candidateAt=Array.IndexOf(args,"-tsDragonVistaCandidate");
        string candidate=candidateAt>=0&&candidateAt+1<args.Length?args[candidateAt+1]:(string)type.GetField("DefaultCandidateFolder").GetRawConstantValue();string leaf=candidate.Replace('\\','/').Split('/').Last();string evidence=Path.Combine(args[at+1],"VistaRegionalFoundation",leaf);
        var receipt=JsonUtility.FromJson<Receipt>(File.ReadAllText(Path.Combine(evidence,"report.json")));
        Assert.That(receipt.status,Is.EqualTo("MEASURED_CANDIDATE_REQUIRES_WORLD_INTEGRATION_AND_VISUAL_REVIEW"));Assert.That(receipt.validationIssues,Is.Empty);
        Assert.That(receipt.sourceBytesPreserved&&receipt.originalScenePreserved&&receipt.temporaryObjectsRemoved,Is.True,"Sources and current scene must survive actual vendor generation.");
        Assert.That(receipt.effectiveGraphResolution,Is.EqualTo(512));Assert.That(receipt.effectiveHeightResolution,Is.EqualTo(513));Assert.That(receipt.heightPopulationCallbacks,Is.EqualTo(4));Assert.That(receipt.terrains,Has.Length.EqualTo(4));Assert.That(receipt.regions,Has.Length.EqualTo(9));
        Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(receipt.prefab),Is.Not.Null,"Retain actual terrain and Vista composition references for separate integration.");
        Assert.That(receipt.biomes.Count(value=>value.kind=="region"),Is.EqualTo(9));Assert.That(receipt.biomes.Count(value=>value.kind=="crest"),Is.EqualTo(4));Assert.That(receipt.biomes.Count(value=>value.kind=="water-axial-gradient"),Is.EqualTo(7));Assert.That(receipt.biomes.Count(value=>value.kind=="lava-axial-gradient"),Is.EqualTo(3));
        foreach(var biome in receipt.biomes)
        {
            Assert.That(biome.graphPath,Does.StartWith("Assets/TruckDragonKingdom/PrivateLicensedEnvironment/DerivedVista/"),"No derived licensed graph payload in public candidate folder.");
            var graph=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(biome.graphPath);Assert.That(graph,Is.Not.Null);
            var getNodes=graph.GetType().GetMethods(BindingFlags.Public|BindingFlags.Instance).Single(value=>value.Name=="GetNodes"&&!value.IsGenericMethod&&value.GetParameters().Length==0);var nodes=((IEnumerable)getNodes.Invoke(graph,null)).Cast<object>().ToArray();
            Assert.That(nodes.Any(value=>value.GetType().Name=="HeightOutputNode"),Is.True);Assert.That(nodes.Any(value=>value.GetType().Name=="MathNode"),Is.True);
            Assert.That(nodes.All(value=>value.GetType().Namespace=="Pinwheel.Vista.Graph"),Is.True,"Only installed vendor graph nodes may generate terrain.");
            if(biome.kind=="region"||biome.kind=="crest")Assert.That(biome.falloff,Is.InRange(200,500));
        }
        foreach(var tile in receipt.terrains)
        {
            var data=AssetDatabase.LoadAssetAtPath<TerrainData>(tile.path);Assert.That(data,Is.Not.Null);Assert.That(data.heightmapResolution,Is.EqualTo(513));Assert.That(data.size,Is.EqualTo(new Vector3(1600,600,1600)));Assert.That(tile.position.y,Is.EqualTo(-20));Assert.That(tile.nonFinite,Is.Zero);Assert.That(tile.ceilingSamples,Is.Zero);
            Assert.That(tile.coverage,Has.Length.EqualTo(256));Assert.That(tile.coverage.Sum(value=>value.count),Is.EqualTo(513*513));Assert.That(File.Exists(Path.Combine(evidence,tile.png))&&File.Exists(Path.Combine(evidence,tile.floatMap)),Is.True);
        }
        Assert.That(receipt.terrains.Any(value=>value.maximumY>=400&&value.maximumY<580),Is.True,"Actual native broad mountain relief is required before road grading.");
        foreach(var region in receipt.regions){Assert.That(region.neighborhoodSamples,Is.EqualTo(169));Assert.That(region.neighborhoodMaximumY-region.neighborhoodMinimumY,Is.GreaterThan(1),"Retain genuine regional relief: "+region.id);Assert.That(Mathf.Abs(region.actualY-region.requestedY),Is.LessThanOrEqualTo(region.requestedY>=100?60:30),region.id);}
        foreach(var water in receipt.water){Assert.That(water.actualBedY,Is.LessThanOrEqualTo(water.waterY+1.5f),water.id);if(water.id=="lake-vessel")Assert.That(water.actualBedY,Is.EqualTo(-8).Within(1),"Actual moving hull must remain over a real basin, without a static raised pad.");}
    }
}
#endif
