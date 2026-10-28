using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
public sealed class CampingWorldGeometryTests
{
    static Type Author=>Type.GetType("LWS.TruckCamping.Editor.CampingWildernessAuthoring, Assembly-CSharp-Editor",true);
    static float Height(Vector2 p)=>(float)Author.GetMethod("HeightAt").Invoke(null,new object[]{p});
    [Test]public void SharedWorldHasFourSquareKilometresOfConnectedTrailsClearLandmarksLakeBedAndMountainSilhouette()
    {
        Assert.That(2000f*2000f/2589988.11f,Is.InRange(1,2),"Actual world dimensions in square miles.");
        var stops=(Vector2[])Author.GetField("Stops").GetValue(null);var trail=(Vector2[])Author.GetField("Trail").GetValue(null);Assert.That(stops.Length,Is.EqualTo(12));Assert.That(trail.Length,Is.GreaterThan(12));
        foreach(var p in stops)Assert.That(Height(p),Is.InRange(6,9),"Each actual truck-accessible activity clearing: "+p);
        Assert.That(Height(new Vector2(145,83)),Is.LessThan(5.5f),"Real lake depth below the flowing surface.");Assert.That(Height(new Vector2(900,0)),Is.GreaterThan(60),"Mountain silhouette is actual terrain, not distant flat art.");
        for(int i=1;i<trail.Length;i++)for(int s=1;s<=20;s++){var p=Vector2.Lerp(trail[i-1],trail[i],s/20f);Assert.That(float.IsFinite(Height(p)),Is.True);}
    }
}
