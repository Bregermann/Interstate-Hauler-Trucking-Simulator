#if UNITY_EDITOR && VISTA
using System;
using System.Linq;
using Pinwheel.Vista.Graph;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomRegionalWorldIntegration
    {
        public static int InstalledErosionRecipeResolution(int configuredGraphResolution,int actualInputWidth)
        {
            var graph=AssetDatabase.LoadAssetAtPath<TerrainGraph>(FinishErosionGraph);if(graph==null)throw new InvalidOperationException("Actual installed authored Vista graph required.");return graph.GetNodesOfType<ErosionNode>().Single().CalculateResolution(configuredGraphResolution,actualInputWidth);
        }
        // Erosion OutputHeight01 writes unpadded grid vertices id/(resolution-1).
        // Simulation-only+4pixel margins are removed by the vendor output kernel.
        // This transfers that output grid to TerrainData vertices; no height kernel.
        public static float[,] ResampleVistaVertexGrid(float[] actualPixels,int width,int height,int destination)
        {
            if(actualPixels==null||width<2||height<2||destination<2||actualPixels.Length!=checked(width*height))throw new ArgumentException("Exact complete actual vendor float pixel grid and destination vertices required.");
            var result=new float[destination,destination];for(int z=0;z<destination;z++)for(int x=0;x<destination;x++)
            {
                float px=(float)x/(destination-1)*(width-1),pz=(float)z/(destination-1)*(height-1);int x0=Mathf.FloorToInt(px),z0=Mathf.FloorToInt(pz),x1=Math.Min(x0+1,width-1),z1=Math.Min(z0+1,height-1);float a=actualPixels[z0*width+x0],b=actualPixels[z0*width+x1],c=actualPixels[z1*width+x0],d=actualPixels[z1*width+x1];if(!float.IsFinite(a)||!float.IsFinite(b)||!float.IsFinite(c)||!float.IsFinite(d))throw new InvalidOperationException("Actual vendor output contains nonfinite grid pixels.");result[z,x]=Mathf.Lerp(Mathf.Lerp(a,b,px-x0),Mathf.Lerp(c,d,px-x0),pz-z0);
            }
            return result;
        }
        static float[] ReadActualVistaGpuPixels(RenderTexture source)
        {
            if(source.format!=RenderTextureFormat.RFloat)throw new InvalidOperationException("Actual Vista height output must be RFloat; never label another buffer as raw height pixels.");
            var prior=RenderTexture.active;var texture=new Texture2D(source.width,source.height,TextureFormat.RFloat,false,true);
            try{RenderTexture.active=source;texture.ReadPixels(new Rect(0,0,source.width,source.height),0,0,false);texture.Apply(false,false);var native=texture.GetRawTextureData<float>();var result=new float[native.Length];for(int i=0;i<result.Length;i++)result[i]=native[i];return result;}
            finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(texture);}
        }
    }
}
#endif
