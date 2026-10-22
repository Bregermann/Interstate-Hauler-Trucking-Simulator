#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class MinefieldSideRangeTests
{
    static Component Match()
    {
        EditorSceneManager.OpenScene("Assets/TruckMinefield/Scenes/TruckMinefield.unity");return (Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckMinefield.MinefieldMatch, Assembly-CSharp",true),FindObjectsSortMode.None).Single();
    }
    [Test]
    public void RunoutEdgesMeetBothSideTerrainsWithoutVerticalCollisionSteps()
    {
        var match=Match();var terrains=((Array)match.GetType().GetField("grounds").GetValue(match)).Cast<Terrain>().ToArray();var runout=terrains.Single(t=>t.name=="Mile landing runout");float largest=0;
        foreach(var side in terrains.Where(t=>t.name=="West meadow"||t.name=="East meadow"))for(int step=0;step<=64;step++)
        {
            var p=new Vector3(Mathf.Sign(side.transform.position.x)*57,0,Mathf.Lerp(1668,8388,step/64f));largest=Mathf.Max(largest,Mathf.Abs(side.SampleHeight(p)+side.transform.position.y-runout.SampleHeight(p)-runout.transform.position.y));
        }
        Debug.Log("MINEFIELD_RUNOUT_SEAM_MAX_METRES="+largest);Assert.That(largest,Is.LessThan(.03f));
    }
    [Test]
    public void VistaRangesPreserveTheCourseWidthLengthAndInnerMeadowAndAddRealMountainCollision()
    {
        var match=Match();var terrains=((Array)match.GetType().GetField("grounds").GetValue(match)).Cast<Terrain>().ToArray();var main=(Terrain)match.GetType().GetField("terrain").GetValue(match);
        Assert.That(AssetDatabase.GetAssetPath(main.terrainData),Is.EqualTo("Assets/TruckMinefield/Materials/Mile terrain.asset"));Assert.That(main.terrainData.size,Is.EqualTo(new Vector3(114,18,1680)));
        Assert.That(terrains.Max(t=>t.transform.position.x+t.terrainData.size.x)-terrains.Min(t=>t.transform.position.x),Is.EqualTo(614));Assert.That(terrains.Max(t=>t.transform.position.z+t.terrainData.size.z)-terrains.Min(t=>t.transform.position.z),Is.EqualTo(8400));Physics.SyncTransforms();
        foreach(var side in terrains.Where(t=>t.name=="West meadow"||t.name=="East meadow"))
        {
            var data=side.terrainData;var original=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/TruckMinefield/Materials/"+side.name+".asset");Assert.That(data,Is.Not.SameAs(original));Assert.That(side.GetComponent<TerrainCollider>().terrainData,Is.SameAs(data));Assert.That(side.GetComponent<TerrainCollider>().enabled,Is.True);
            float error=0;foreach(float magnitude in new[]{57f,75,100,125,144})for(int z=0;z<=512;z+=32)
            {
                float worldX=magnitude*Mathf.Sign(side.transform.position.x),u=(worldX-side.transform.position.x)/data.size.x,v=z/512f;error=Mathf.Max(error,Mathf.Abs(data.GetInterpolatedHeight(u,v)-original.GetInterpolatedHeight(u,v)));
            }
            Assert.That(error,Is.LessThan(.01f),"Inner meadow must retain its real height, allowing only native heightmap quantization.");
            float peak=data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution).Cast<float>().Max()*data.size.y;Assert.That(peak,Is.GreaterThan(110));
            float outer=(Mathf.Sign(side.transform.position.x)*305-side.transform.position.x)/data.size.x;
            for(int z=0;z<=32;z++)Assert.That(data.GetInterpolatedHeight(outer,z/32f),Is.GreaterThan(60),"Outer crest remains continuous instead of opening low gaps.");
            Assert.That(data.treeInstanceCount,Is.EqualTo(2500));Assert.That(data.treePrototypes.Length,Is.EqualTo(1));Assert.That(data.treePrototypes[0].prefab.GetComponentsInChildren<Collider>(true),Is.Empty,"New forest dressing must not add invisible driving obstacles.");
            foreach(var tree in data.treeInstances)
            {
                float x=side.transform.position.x+tree.position.x*data.size.x;Assert.That(Mathf.Abs(x),Is.InRange(170,300));Assert.That(Mathf.Abs(tree.position.y*data.size.y-data.GetInterpolatedHeight(tree.position.x,tree.position.z)),Is.LessThan(.02f));
            }
            foreach(float z in new[]{100f,3000,8000})
            {
                float x=Mathf.Sign(side.transform.position.x)*285;Assert.That(Physics.Raycast(new Vector3(x,320,z),Vector3.down,out var hit,400),Is.True);Assert.That(hit.collider,Is.SameAs(side.GetComponent<TerrainCollider>()));Assert.That(Mathf.Abs(hit.point.y-side.SampleHeight(hit.point)-side.transform.position.y),Is.LessThan(.04f));
            }
            Debug.Log("MINEFIELD_RANGE_VERIFIED "+side.name+" peak="+peak+" innerHeightError="+error+" trees="+data.treeInstanceCount);
        }
    }
    [Test]
    public void VistaAuditPreservesTheActiveRenderTargetAndOriginalGraphBytes()
    {
        var match=Match();var terrain=((Array)match.GetType().GetField("grounds").GetValue(match)).Cast<Terrain>().First(t=>t.name=="West meadow");
        const string graph="Assets/PinwheelStudio/Vista/Personal/Samples/Scenes/Demo_MultiLayerBiomeBlending Biomes/Mountain_TerrainGraph.asset";var original=File.ReadAllBytes(graph);var old=RenderTexture.active;var target=new RenderTexture(16,16,0){hideFlags=HideFlags.HideAndDontSave};target.Create();RenderTexture.active=target;Texture2D output=null;
        try
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("LWS.TruckMinefield.Editor.MinefieldSideRangeAuthoring")).First(t=>t!=null);output=(Texture2D)type.GetMethod("VistaMountains",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{terrain,72137});Assert.That(RenderTexture.active,Is.SameAs(target));CollectionAssert.AreEqual(original,File.ReadAllBytes(graph));
        }
        finally{RenderTexture.active=old;if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}if(output!=null)UnityEngine.Object.DestroyImmediate(output);}
    }
}
#endif
