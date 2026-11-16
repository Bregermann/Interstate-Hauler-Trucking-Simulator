#if UNITY_EDITOR
using System;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomVendorFalloffAuthoring
    {
        [Serializable]public sealed class NativeSdkStencil
        {
            public string authority="READ_ONLY_INSTALLED_TERRAINSMOOTH_INDEX_SUPPORT_NOT_A_HEIGHT_KERNEL",centerRule="Convert.ToInt32(((world-origin)/size)*resolution)",maskSha256;
            public int resolution,radiusX,radiusZ,initialPhase,expectedFinalPhase,eligibleLeft,eligibleRight,skippedBridgeStations,allowedSamples;
            public float meterDistance;public Vector3 origin,size,heightmapScale;public Bounds actualGridBounds;
            [NonSerialized]public bool[] allowed;
        }
        // Reproduces only the installed public operation's finite index support,
        // never its height calculation. The SDK remains the sole height author.
        public static NativeSdkStencil MeasureInstalledSdkStencil(Vector3[] left,Vector3[] right,bool[] bridge,Vector3 origin,Vector3 size,Vector3 scale,int resolution,float distance,int phase)
        {
            if(left==null||right==null||bridge==null||left.Length!=right.Length||bridge.Length!=right.Length||resolution<2||distance<=0||!float.IsFinite(distance)||scale.x<=0||scale.z<=0||size.x<=0||size.z<=0||(phase!=0&&phase!=-1))throw new ArgumentException("Actual matched native contours/bridge flags/terrain dimensions and installed0/-1 phase required.");
            var proof=new NativeSdkStencil{resolution=resolution,origin=origin,size=size,heightmapScale=scale,meterDistance=distance,initialPhase=phase,radiusX=Convert.ToInt32(Mathf.Ceil(distance/scale.x)),radiusZ=Convert.ToInt32(Mathf.Ceil(distance/scale.z)),allowed=new bool[resolution*resolution]};
            int side=resolution+1;var differences=new int[side*side];float xmin=origin.x+1.5f*distance,zmin=origin.z+1.5f*distance,xmax=origin.x+size.x-1.5f*distance,zmax=origin.z+size.z-1.5f*distance;
            bool Eligible(Vector3 p){if(!float.IsFinite(p.x)||!float.IsFinite(p.z))throw new ArgumentException("Finite actual contour station required.");return p.x>=xmin&&p.x<xmax&&p.z>=zmin&&p.z<=zmax;}
            void Rectangle(int x,int z)
            {
                int x0=Math.Max(0,x),z0=Math.Max(0,z),x1=Math.Min(resolution-1,x+2*proof.radiusX-1),z1=Math.Min(resolution-1,z+2*proof.radiusZ-1);if(x0>x1||z0>z1)return;
                differences[z0*side+x0]++;differences[z0*side+x1+1]--;differences[(z1+1)*side+x0]--;differences[(z1+1)*side+x1+1]++;
            }
            for(int i=0;i<right.Length;i++)
            {
                if(bridge[i]){proof.skippedBridgeStations++;continue;}
                var p=left[i];if(Eligible(p)){int x=Convert.ToInt32((p.x-origin.x)/size.x*resolution)-proof.radiusX-phase,z=Convert.ToInt32((p.z-origin.z)/size.z*resolution)-proof.radiusZ-phase;Rectangle(Math.Max(0,x),Math.Max(0,z));phase=phase==0?-1:0;proof.eligibleLeft++;}
                p=right[i];if(Eligible(p)){Rectangle(Convert.ToInt32((p.x-origin.x)/size.x*resolution)-proof.radiusX,Convert.ToInt32((p.z-origin.z)/size.z*resolution)-proof.radiusZ);proof.eligibleRight++;}
            }
            proof.expectedFinalPhase=phase;var mask=new byte[proof.allowed.Length];bool first=true;
            for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++)
            {
                int at=z*side+x;differences[at]+=(x>0?differences[at-1]:0)+(z>0?differences[at-side]:0)-(x>0&&z>0?differences[at-side-1]:0);if(differences[at]<=0)continue;
                proof.allowed[z*resolution+x]=true;mask[z*resolution+x]=1;proof.allowedSamples++;var p=origin+new Vector3(x*scale.x,0,z*scale.z);if(first){proof.actualGridBounds=new Bounds(p,Vector3.zero);first=false;}else proof.actualGridBounds.Encapsulate(p);
            }
            proof.maskSha256=Sha(mask);return proof;
        }
    }
}
#endif
