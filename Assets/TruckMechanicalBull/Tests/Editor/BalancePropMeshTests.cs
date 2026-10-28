using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class BalancePropMeshTests
{
    [TestCase("RoundedBlock")]
    [TestCase("Ovoid")]
    public void OriginalPropSurfacesHaveOutwardWindingAndFiniteUvNormals(string recipe)
    {
        var type = Type.GetType("LWS.TruckMechanicalBull.Editor.ImpossibleBalanceArt, Assembly-CSharp-Editor", true);
        var mesh = (Mesh)type.GetMethod(recipe).Invoke(null, null);
        try
        {
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            Assert.That(mesh.vertexCount, Is.GreaterThan(30)); Assert.That(mesh.uv.Length, Is.EqualTo(mesh.vertexCount));
            var v = mesh.vertices; var triangles = mesh.triangles; int valid = 0;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var a = v[triangles[i]]; var b = v[triangles[i + 1]]; var c = v[triangles[i + 2]];
                var normal = Vector3.Cross(b - a, c - a); if (normal.sqrMagnitude < 1e-12f) continue;
                Assert.That(Vector3.Dot(normal.normalized, (a + b + c) / 3), Is.GreaterThan(0), recipe + " must not present inward-facing caps or sides."); valid++;
            }
            Assert.That(valid, Is.GreaterThan(25)); Assert.That(mesh.normals.All(n => float.IsFinite(n.x) && float.IsFinite(n.y) && float.IsFinite(n.z)), Is.True);
        }
        finally { UnityEngine.Object.DestroyImmediate(mesh); }
    }
}
