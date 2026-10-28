#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using static WildernessNativeScope;

public sealed class CampingNavigationNativeTests
{
    [Serializable] sealed class FileRow { public string path,sha256; }
    [Serializable] sealed class OriginalStage { public FileRow[] files; }
    static string Digest(byte[] bytes){using(var hash=SHA256.Create())return string.Concat(hash.ComputeHash(bytes).Select(x=>x.ToString("x2")));}
    static readonly string[] HeightHashes={
        "6da94e9436083b80fa6ca8c6796d4488b14218466722137656f4be76a0c2150b",
        "59723d4f7d8123fa5c79e6fd7aafc0f5dc447fd051d12eca17a00f8918138582",
        "a201afa8f0f84433200933d893044fe0eaec1d9e4a4af9e4f4e7ded66ab97cb9",
        "9edf5754f7eacd927a65aa1d284f6174648d819e82f729d974995554557e904a"};
    static readonly string[] Guids={"ab66ebbed2d5de042a26d66937d9ef41","536d3ebb9c605dc4c8125f50b46b7dbe","41b2c3577830ed9498c578c32d6b4553","aa4f37bee36f7cb4fb33f3c3f9d8ec8e"};
    static readonly string[] Layers={"Grass Light/Grass_Light_VistaSample.terrainlayer","Grass Dark/Grass_Dark_VistaSample.terrainlayer","Rock Base/Rock_Base_VistaSample.terrainlayer"};

    static void OriginalEncodedHeights(byte[] asset,string expected)
    {
        // The observed serialized257² height-buffer signature is checked against
        // its original V1 SHA, independently of editor subasset ordering/IDs.
        bool exact=false;byte[] count=BitConverter.GetBytes(257*257);
        for(int at=0;at+4+257*257*2<=asset.Length;at++)
            if(asset[at]==count[0]&&asset[at+1]==count[1]&&asset[at+2]==count[2]&&asset[at+3]==count[3])
            {var values=new byte[257*257*2];Buffer.BlockCopy(asset,at+4,values,0,values.Length);exact|=Digest(values)==expected;}
        Assert.That(exact,Is.True,"All66049 original encoded height values must remain byte-exact.");
    }

    [UnityTest,Timeout(90000)]public IEnumerator SavedFourTileForestRetainsOriginalHeightBuffersMetasLayersAndAllTwelveRealNavQueriesThenRendersBothResolutions()
    {
        using(var scope=new WildernessNativeScope("navigation-v3-completed-world"))
        {
            yield return scope.Load("TruckCamping","LWS.TruckCamping.CampingMatch");var world=(Component)Get(scope.match,"world");var terrains=(Terrain[])Get(world,"terrains");Assert.That(terrains.Length,Is.EqualTo(4));
            var original=JsonUtility.FromJson<OriginalStage>(File.ReadAllText("Assets/TruckCamping/Editor/NavigationFailedStageV1.json"));int totalTrees=0;
            for(int i=0;i<4;i++)
            {
                var data=terrains[i].terrainData;string path=AssetDatabase.GetAssetPath(data);Assert.That(data.size,Is.EqualTo(new Vector3(1000,260,1000)));Assert.That(data.heightmapResolution,Is.EqualTo(257));
                Assert.That(AssetDatabase.AssetPathToGUID(path),Is.EqualTo(Guids[i]));
                Assert.That(Digest(File.ReadAllBytes(path+".meta")),Is.EqualTo(original.files.Single(f=>f.path==path+".meta").sha256));OriginalEncodedHeights(File.ReadAllBytes(path),HeightHashes[i]);
                Assert.That(data.terrainLayers.Length,Is.EqualTo(3));for(int layer=0;layer<3;layer++)Assert.That(AssetDatabase.GetAssetPath(data.terrainLayers[layer]),Is.EqualTo("Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/"+Layers[layer]));
                Assert.That(data.treeInstanceCount,Is.GreaterThan(100),"Complete the proven missing owned forest stage; never silently preserve an empty forest.");totalTrees+=data.treeInstanceCount;
                Assert.That(data.treePrototypes.Length,Is.EqualTo(1));Assert.That(AssetDatabase.GetAssetPath(data.treePrototypes[0].prefab),Is.EqualTo("Assets/TruckCamping/Prefabs/Camp Haul solid JC07 pine.prefab"));
                foreach(var renderer in data.treePrototypes[0].prefab.GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials)Assert.That(material!=null&&material.shader!=null&&material.shader.isSupported,Is.True,"Actual purchased URP tree materials must be supported.");
                var paint=data.GetAlphamaps(0,0,128,128);float dark=0;for(int z=0;z<128;z++)for(int x=0;x<128;x++)dark+=paint[z,x,1];
                Assert.That(dark/(128*128),Is.GreaterThan(.03f),"The originally null/unpersisted control map must be completed by the actual authored paint recipe.");
                Assert.That(data.alphamapTextures.All(t=>t!=null&&AssetDatabase.GetAssetPath(t)==path),Is.True,"Actual generated control textures persist under the SAME owned TerrainData asset/GUID.");
                TestContext.WriteLine($"CAMPING_TERRAIN_PROOF tile={i} trees={data.treeInstanceCount} originalEncodedHeightsAndMetaExact=true guid={Guids[i]} size={data.size} meanDarkControlWeight={dark/(128*128):R}");
            }
            var route=(Transform[])Get(world,"route");Assert.That(route.Length,Is.EqualTo(12));
            foreach(var point in route)
            {Assert.That(NavMesh.SamplePosition(point.position,out var hit,8,NavMesh.AllAreas),Is.True,"Original8m query for "+point.parent.name);TestContext.WriteLine($"CAMPING_NAV_POINT {point.parent.name} desired={point.position} actual={hit.position} distance={Vector3.Distance(point.position,hit.position):R}");}
            scope.Configure(4,true);Assert.That((bool)Call(scope.match,"StartMatch"),Is.True);yield return scope.Advance(1.2f,8);
            foreach(int width in new[]{1280,1920})
            {
                string name="camping-solid-jc07-real-forest-"+width;scope.Capture(name,width,width==1280?720:1080);
                string path=Path.GetFullPath("Logs/TruckSports/Impossible07/WildernessControlled/"+name+".png");var pixels=new Texture2D(2,2,TextureFormat.RGB24,false);
                try
                {Assert.That(pixels.LoadImage(File.ReadAllBytes(path)),Is.True);var data=pixels.GetPixels();Assert.That(data.Max(c=>c.grayscale)-data.Min(c=>c.grayscale),Is.GreaterThan(.08f));TestContext.WriteLine($"CAMPING_FOREST_CAPTURE {width}x{pixels.height} persistedTrees={totalTrees} actualOriginalCamera CONTROLLED_NATIVE_REVIEW_REQUIRED");}
                finally{UnityEngine.Object.DestroyImmediate(pixels);}
            }
        }
    }
}
#endif
