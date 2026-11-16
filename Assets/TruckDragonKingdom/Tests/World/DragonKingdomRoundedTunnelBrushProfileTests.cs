#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class DragonKingdomRoundedTunnelBrushProfileTests
{
    [TestCase(32f,100f)]
    [TestCase(34f,424f)]
    public void ClosedBarrelContainsEveryOriginalBoxCornerAndRaisesOnlyTheRoof(float width,float length)
    {
        var type=Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomRoundedTunnelBrushProfile, Assembly-CSharp-Editor",true);
        var geometry=(IDisposable)type.GetMethod("Create").Invoke(null,new object[]{width,length,55.1f,8f,16});GameObject go=null;
        try
        {
            var mesh=(Mesh)geometry.GetType().GetField("mesh").GetValue(geometry);float center=(float)geometry.GetType().GetField("centerAboveRoad").GetValue(geometry);var vertices=mesh.vertices;var triangles=mesh.triangles;var edges=new Dictionary<ulong,List<int>>();double volume=0;
            for(int i=0;i<triangles.Length;i+=3)
            {
                int a=triangles[i],b=triangles[i+1],c=triangles[i+2];volume+=Vector3.Dot(vertices[a],Vector3.Cross(vertices[b],vertices[c]))/6d;
                void Edge(int x,int y){ulong key=((ulong)Math.Min(x,y)<<32)|(uint)Math.Max(x,y);if(!edges.TryGetValue(key,out var signs))edges.Add(key,signs=new List<int>());signs.Add(x<y?1:-1);}
                Edge(a,b);Edge(b,c);Edge(c,a);
            }
            foreach(var edge in edges.Values){Assert.That(edge.Count,Is.EqualTo(2));Assert.That(edge[0]+edge[1],Is.Zero);}
            Assert.That(vertices.Length-edges.Count+triangles.Length/3,Is.EqualTo(2));Assert.That(volume,Is.GreaterThan(width*length*55.1));
            Assert.That(mesh.bounds.center.magnitude,Is.LessThan(.0001f));Assert.That(mesh.bounds.min.y+center,Is.EqualTo(-.05f).Within(.0001f));Assert.That(mesh.bounds.max.y+center,Is.EqualTo(63.05f).Within(.0001f));
            go=new GameObject("Temporary native convex road-vault containment control"){hideFlags=HideFlags.HideAndDontSave};var collider=go.AddComponent<MeshCollider>();collider.sharedMesh=mesh;collider.convex=true;Physics.SyncTransforms();
            foreach(float x in new[]{-width*.5f,width*.5f})foreach(float z in new[]{-length*.5f,length*.5f})foreach(float y in new[]{-.05f,55.05f})
            {var p=new Vector3(x,y-center,z);Assert.That(Vector3.Distance(collider.ClosestPoint(p),p),Is.LessThan(.0001f),"Actual native convex input must contain all eight corners of the complete original road-clearance box.");}
        }
        finally{if(go!=null)UnityEngine.Object.DestroyImmediate(go);geometry.Dispose();}
    }
}
#endif
