#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    // A closed, centered authoring INPUT for installed Digger.CustomBrush.
    // It is never rendered as scenery and does not generate voxel terrain,
    // terrain holes, collision, LODs or persistence. Digger owns those outputs.
    // No existing excavation path uses this profile until native acceptance.
    public static class DragonKingdomRoundedCavernBrushProfile
    {
        public sealed class Geometry:IDisposable
        {
            public Mesh mesh;public float radius,floorY,shoulderY,topY,centerY;
            public int sides,domeRings;
            public void Dispose(){if(mesh!=null){UnityEngine.Object.DestroyImmediate(mesh);mesh=null;}}
        }
        public static Geometry Create(float radius,float floorY,float shoulderY,float topY,int sides=64,int domeRings=12)
        {
            if(!float.IsFinite(radius)||!float.IsFinite(floorY)||!float.IsFinite(shoulderY)||!float.IsFinite(topY)||radius<10||radius>100||shoulderY-floorY<10||topY-shoulderY<10||topY-floorY>100||sides<16||sides>128||sides%4!=0||domeRings<4||domeRings>24)throw new ArgumentException("Bounded rounded authoring lobe with genuine flat floor, vertical lower wall and arched vault required.");
            float centerY=(floorY+topY)*.5f;var vertices=new List<Vector3>();var triangles=new List<int>();
            void Ring(float r,float y){for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;vertices.Add(new Vector3(r*Mathf.Cos(a),y-centerY,r*Mathf.Sin(a)));}}
            Ring(radius,floorY);Ring(radius,shoulderY);
            for(int k=1;k<domeRings;k++){float angle=k*Mathf.PI*.5f/domeRings;Ring(radius*Mathf.Cos(angle),shoulderY+(topY-shoulderY)*Mathf.Sin(angle));}
            int rings=domeRings+1,bottom=vertices.Count;vertices.Add(new Vector3(0,floorY-centerY,0));int apex=vertices.Count;vertices.Add(new Vector3(0,topY-centerY,0));
            for(int i=0;i<sides;i++)
            {
                int next=(i+1)%sides;triangles.Add(bottom);triangles.Add(i);triangles.Add(next);
                for(int k=0;k<rings-1;k++)
                {
                    int lo=k*sides+i,hi=(k+1)*sides+i,hiNext=(k+1)*sides+next,loNext=k*sides+next;
                    triangles.Add(lo);triangles.Add(hi);triangles.Add(hiNext);triangles.Add(lo);triangles.Add(hiNext);triangles.Add(loNext);
                }
                triangles.Add((rings-1)*sides+i);triangles.Add(apex);triangles.Add((rings-1)*sides+next);
            }
            if(vertices.Count>=ushort.MaxValue)throw new InvalidOperationException("Installed Digger custom input uses ushort triangle indices.");
            var mesh=new Mesh{name="Temporary original rounded cavern brush input",hideFlags=HideFlags.HideAndDontSave};
            mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();mesh.RecalculateNormals();
            if(mesh.bounds.center.sqrMagnitude>.000001f){UnityEngine.Object.DestroyImmediate(mesh);throw new InvalidOperationException("Installed custom-brush voxel origin requires a genuinely centered input mesh.");}
            return new Geometry{mesh=mesh,radius=radius,floorY=floorY,shoulderY=shoulderY,topY=topY,centerY=centerY,sides=sides,domeRings=domeRings};
        }
        public static float AnalyticCeiling(float radius,float shoulderY,float topY,float radialDistance)
        {
            if(!float.IsFinite(radialDistance)||radialDistance<0||radialDistance>radius)return float.NegativeInfinity;
            return shoulderY+(topY-shoulderY)*Mathf.Sqrt(Mathf.Max(0,1-radialDistance*radialDistance/(radius*radius)));
        }
    }
}
#endif
