using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
public sealed class BalletTheaterGeometryTests
{
    [Test]public void OwnedPleatedTutuAndCurtainsHaveFiniteNondegenerateFrontFacesWithoutOpposingNormalCancellation()
    {
        var author=Type.GetType("LWS.TruckBallet.Editor.BalletAuthoring, Assembly-CSharp-Editor",true);
        foreach(var pair in new[]{("Curtain",new object[]{72,18}),("Tutu",new object[]{2f,3f,6f,6.5f})})
        {
            var mesh=(Mesh)author.GetMethod(pair.Item1).Invoke(null,pair.Item2);
            try
            {
                mesh.RecalculateNormals();mesh.RecalculateBounds();Assert.That(mesh.vertexCount,Is.GreaterThan(1000));Assert.That(mesh.normals.All(n=>float.IsFinite(n.x)&&float.IsFinite(n.y)&&float.IsFinite(n.z)&&n.sqrMagnitude>.9f),Is.True,"A reverse duplicate triangle may not cancel the cloth normals.");
                var vertices=mesh.vertices;var triangles=mesh.triangles;for(int i=0;i<triangles.Length;i+=3)Assert.That(Vector3.Cross(vertices[triangles[i+1]]-vertices[triangles[i]],vertices[triangles[i+2]]-vertices[triangles[i]]).sqrMagnitude,Is.GreaterThan(1e-12f));
                if(pair.Item1=="Tutu")Assert.That(mesh.normals.Count(n=>n.y>0),Is.GreaterThan(mesh.vertexCount*.9f));
            }
            finally{UnityEngine.Object.DestroyImmediate(mesh);}
        }
    }
}
