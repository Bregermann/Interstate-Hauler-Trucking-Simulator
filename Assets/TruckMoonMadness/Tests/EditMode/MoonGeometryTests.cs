using System;
using System.IO;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MoonGeometryTests
{
    static Type Author=>Type.GetType("LWS.TruckMoonMadness.Editor.MoonAuthoring, Assembly-CSharp-Editor",true);
    static float Height(float x,float z)=>(float)Author.GetMethod("Height",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{x,z});
    [Test]public void RealCraterDepressionsRemainDistinctFromBroadEightWayRampApproaches()
    {
        foreach(var p in new[]{new Vector2(-45,18),new Vector2(45,-18),new Vector2(-18,-45),new Vector2(18,45)})
            Assert.That(Height(p.x,p.y),Is.LessThan(-3),"Actual terrain crater must remain depressed, not erased by a radial road.");
        for(int owner=0;owner<8;owner++)for(int radius=20;radius<=90;radius+=2)
        {
            float a=owner*Mathf.PI/4;Assert.That(Height(Mathf.Sin(a)*radius,Mathf.Cos(a)*radius),Is.EqualTo(0).Within(.00001f),"Original AI/player radial ground approach must be an actual level navigable surface.");
        }
        for(int x=-120;x<=120;x+=4)for(int z=-120;z<=120;z+=4)Assert.That(float.IsFinite(Height(x,z)),Is.True);
    }
    [Test]public void NewSceneAuthorRefusesExistingSceneBeforeAnyAssetOrSceneMutation()
    {
        const string scene="Assets/TruckMoonMadness/Scenes/TruckMoonMadness.unity";
        Assert.That(File.Exists(scene),Is.True,"Run the explicit new-scene author first; this test must never author a scene as a side effect.");
        byte[] before=File.ReadAllBytes(scene);string active=SceneManager.GetActiveScene().path;
        var error=Assert.Throws<TargetInvocationException>(()=>Author.GetMethod("CreateNewScene").Invoke(null,null));
        Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>());CollectionAssert.AreEqual(before,File.ReadAllBytes(scene));Assert.That(SceneManager.GetActiveScene().path,Is.EqualTo(active));
    }
    [Test]public void ActualAuthoredPhysicalTerrainAndVisibleWarningFacesExist()
    {
        var terrain=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TruckMoonMadness/Materials/Lunar crater terrain.asset");
        Assert.That(terrain,Is.Not.Null,"Run explicit new-scene author first.");Assert.That(terrain.vertices.Min(v=>v.y),Is.LessThan(-3));
        for(int i=0;i<4;i++)
        {
            var ring=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TruckMoonMadness/Materials/Three second meteor warning "+i+".asset");
            Assert.That(ring,Is.Not.Null);Assert.That(ring.normals.All(n=>n.y>.99f),Is.True,"Actual warning geometry must face the sky and participant cameras, not cull from above.");
        }
    }
}
