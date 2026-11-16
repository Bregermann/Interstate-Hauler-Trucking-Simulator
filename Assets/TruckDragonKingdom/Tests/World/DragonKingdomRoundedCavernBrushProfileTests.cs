#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class DragonKingdomRoundedCavernBrushProfileTests
{
    [TestCase(16,4)]
    [TestCase(64,12)]
    [TestCase(128,24)]
    public void RoundedInputIsClosedOutwardManifoldCenteredAndHasGenuineFlat105MetreFloor(int sides,int rings)
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRoundedCavernBrushProfile, Assembly-CSharp-Editor",true);
        var geometry=type.GetMethod("Create").Invoke(null,new object[]{65f,105f,130f,160f,sides,rings});
        try
        {
            var mesh=(Mesh)geometry.GetType().GetField("mesh").GetValue(geometry);float center=(float)geometry.GetType().GetField("centerY").GetValue(geometry);var vertices=mesh.vertices;var triangles=mesh.triangles;var edges=new Dictionary<ulong,List<int>>();double signedVolume=0;int floorFaces=0;
            Assert.That(mesh.bounds.center.magnitude,Is.LessThan(.0001f));Assert.That(mesh.bounds.size,Is.EqualTo(new Vector3(130,55,130)));Assert.That(vertices.Length,Is.LessThan(ushort.MaxValue));
            for(int i=0;i<triangles.Length;i+=3)
            {
                int a=triangles[i],b=triangles[i+1],c=triangles[i+2];var p=vertices[a];var q=vertices[b];var r=vertices[c];Vector3 normal=Vector3.Cross(q-p,r-p);Assert.That(normal.sqrMagnitude,Is.GreaterThan(.00001f),"No collapsed seam/apex triangle.");
                signedVolume+=Vector3.Dot(p,Vector3.Cross(q,r))/6d;
                bool floor=Mathf.Abs(p.y+center-105)<.0001f&&Mathf.Abs(q.y+center-105)<.0001f&&Mathf.Abs(r.y+center-105)<.0001f;
                if(floor){Assert.That(normal.y,Is.LessThan(0));floorFaces++;}
                else{Vector3 centroid=(p+q+r)/3;Assert.That(Vector3.Dot(normal,new Vector3(centroid.x,Mathf.Max(0,centroid.y+center-130),centroid.z)),Is.GreaterThan(0),"Wall/vault faces must point outward.");}
                void Edge(int u,int v){ulong key=((ulong)Math.Min(u,v)<<32)|(uint)Math.Max(u,v);if(!edges.TryGetValue(key,out var directions))edges.Add(key,directions=new List<int>());directions.Add(u<v?1:-1);}
                Edge(a,b);Edge(b,c);Edge(c,a);
            }
            Assert.That(floorFaces,Is.EqualTo(sides));Assert.That(edges.Values.All(e=>e.Count==2&&e.Sum()==0),Is.True,"Every actual indexed edge has exactly two opposite face uses.");Assert.That(vertices.Length-edges.Count+triangles.Length/3,Is.EqualTo(2));Assert.That(signedVolume,Is.GreaterThan(550000));
            Assert.That(vertices.Min(v=>v.y)+center,Is.EqualTo(105));Assert.That(vertices.Max(v=>v.y)+center,Is.EqualTo(160));
            Assert.That(vertices.Where(v=>Mathf.Abs(v.y+center-105)<.0001f).Count(),Is.EqualTo(sides+1));
        }
        finally{((IDisposable)geometry).Dispose();}
    }
}
#endif
