#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    // Closed authoring input for installed Digger.CustomBrush. The arch ADDS
    // clearance above the complete old road box; it never narrows a portal.
    public static class DragonKingdomRoundedTunnelBrushProfile
    {
        public sealed class Geometry:IDisposable
        {
            public Mesh mesh;public float centerAboveRoad,width,length,wallHeight,archRise;
            public void Dispose(){if(mesh!=null){UnityEngine.Object.DestroyImmediate(mesh);mesh=null;}}
        }
        public static Geometry Create(float width,float length,float wallHeight=55.1f,float archRise=8,int arcSteps=16)
        {
            if(!float.IsFinite(width)||!float.IsFinite(length)||!float.IsFinite(wallHeight)||!float.IsFinite(archRise)||width<20||width>80||length<1||length>600||wallHeight!=55.1f||archRise!=8||arcSteps<8||arcSteps>32)throw new ArgumentException("Measured bounded road clearance and exact superset arch recipe required.");
            float half=width*.5f,total=wallHeight+archRise,center=total*.5f-.05f,bottom=-total*.5f,shoulder=bottom+wallHeight;
            var profile=new List<Vector2>{new Vector2(-half,bottom),new Vector2(half,bottom)};
            for(int i=0;i<=arcSteps;i++){float a=i*Mathf.PI/arcSteps;profile.Add(new Vector2(half*Mathf.Cos(a),shoulder+archRise*Mathf.Sin(a)));}
            int n=profile.Count;var vertices=new List<Vector3>();foreach(float z in new[]{-length*.5f,length*.5f})foreach(var p in profile)vertices.Add(new Vector3(p.x,p.y,z));
            int back=vertices.Count;vertices.Add(new Vector3(0,0,-length*.5f));int front=vertices.Count;vertices.Add(new Vector3(0,0,length*.5f));var indices=new List<int>();
            for(int i=0;i<n;i++)
            {
                int j=(i+1)%n;indices.AddRange(new[]{i,j,n+j,i,n+j,n+i,back,j,i,front,n+i,n+j});
            }
            var mesh=new Mesh{name="Temporary closed rounded road vault input",hideFlags=HideFlags.HideAndDontSave};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();mesh.RecalculateNormals();
            if(mesh.bounds.center.sqrMagnitude>.000001f){UnityEngine.Object.DestroyImmediate(mesh);throw new InvalidOperationException("Installed custom voxelizer requires centered actual input geometry.");}
            return new Geometry{mesh=mesh,centerAboveRoad=center,width=width,length=length,wallHeight=wallHeight,archRise=archRise};
        }
    }
}
#endif
