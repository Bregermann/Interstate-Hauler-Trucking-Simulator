using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VendorPlane=Assets.CoreCollapse.EzySlice.Framework.Plane;
using VendorSide=Assets.CoreCollapse.EzySlice.Framework.SideOfPlane;
namespace LWS.TruckDockDiving.Editor
{
    // Missing pair semantics around the installed cutter: independently keeping
    // +normal/-normal otherwise includes a full vendor-ON face in BOTH uppers.
    public static class DockSplitCoplanarOwnership
    {
        public static int FilterOtherHalf(Mesh owned,Matrix4x4 toBody,Vector3 point,Vector3 normal,bool originalUpper)
        {
            if(owned==null||AssetDatabase.Contains(owned)||owned.subMeshCount!=1)throw new InvalidOperationException("Only an owned single-material Dock working mesh may be filtered.");
            // EXACT same canonical-plane -> mesh-local projection as the frozen
            // ClipOwnedWorkingMesh/Keep wrapper, including vendor normalization.
            var localPoint=toBody.inverse.MultiplyPoint3x4(point);var localNormal=toBody.transpose.MultiplyVector(normal).normalized;var plane=new VendorPlane(localPoint,localNormal);
            var vertices=owned.vertices;var triangles=owned.GetTriangles(0);var kept=new List<int>(triangles.Length);int removed=0;
            for(int face=0;face<triangles.Length;face+=3)
            {
                var a=vertices[triangles[face]];var b=vertices[triangles[face+1]];var c=vertices[triangles[face+2]];
                bool vendorOn=plane.SideOf(a)==VendorSide.ON&&plane.SideOf(b)==VendorSide.ON&&plane.SideOf(c)==VendorSide.ON;
                // Sign against this requested half's normal reverses exactly for
                // its opposite. True zero belongs only to ORIGINAL upper.
                double signed=(double)Vector3.Dot(localNormal,a)-plane.dist+(double)Vector3.Dot(localNormal,b)-plane.dist+(double)Vector3.Dot(localNormal,c)-plane.dist;
                if(vendorOn&&(signed<0||signed==0&&!originalUpper)){removed++;continue;}
                kept.Add(triangles[face]);kept.Add(triangles[face+1]);kept.Add(triangles[face+2]);
            }
            if(removed>0)owned.SetTriangles(kept,0,false);return removed;
        }
    }
}
