using System;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckCamping.Editor
{
    public static partial class CampingWildernessAuthoring
    {
        const string PineSource = "Assets/Toby Fredson/Rocky Hills Environment - Whitebark Pine/RHEWP_Demo/Prefabs/Prefabs_Vegetation/PV_Trees/WhitebarkPineRHEP_B.prefab";
        static readonly string[] LayerSources = {
            "Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/Grass Light/Grass_Light_VistaSample.terrainlayer",
            "Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/Grass Dark/Grass_Dark_VistaSample.terrainlayer",
            "Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/Rock Base/Rock_Base_VistaSample.terrainlayer" };

        // The failed V1 author saved data before its late Woodland stage/final
        // SaveAssets. Actual native diagnosis proves zero stored trees. Exact
        // hash preflight additionally accepts only these absent/default inputs;
        // existing authored trees or painted control maps are never replaced.
        static void VerifyMissingTerrainCompletionState()
        {
            for(int z=0;z<2;z++)for(int x=0;x<2;x++)
            {
                string path="Assets/TruckCamping/Models/Camp Haul Vista terrain "+x+" "+z+".asset";
                var data=AssetDatabase.LoadAssetAtPath<TerrainData>(path);
                if(data==null || data.treeInstanceCount!=0 || data.treePrototypes.Length>1)throw new InvalidOperationException("Nonempty/custom partial forest preserved: "+path);
                if(data.treePrototypes.Length==1 && (AssetDatabase.GetAssetPath(data.treePrototypes[0].prefab)!=PineSource || data.treePrototypes[0].bendFactor!=.2f))throw new InvalidOperationException("Custom tree prototype preserved: "+path);
                var layers=data.terrainLayers;if(layers.Length!=3)throw new InvalidOperationException("Custom terrain layers preserved: "+path);
                for(int i=0;i<3;i++)if(AssetDatabase.GetAssetPath(layers[i])!=LayerSources[i])throw new InvalidOperationException("Different original terrain layer preserved: "+path);
                var paint=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);
                for(int j=0;j<paint.GetLength(0);j++)for(int i=0;i<paint.GetLength(1);i++)
                    if((paint[j,i,0]!=0 && paint[j,i,0]!=1) || paint[j,i,1]!=0 || paint[j,i,2]!=0)throw new InvalidOperationException("Existing non-default/custom terrain painting preserved: "+path);
            }
        }

        static void PaintOwnedTerrain(TerrainData data,int x,int z)
        {
            var paint=new float[128,128,3];
            for(int j=0;j<128;j++)for(int i=0;i<128;i++)
            {
                float rock=Mathf.Clamp01((data.GetSteepness(i/127f,j/127f)-25)/22),dark=Mathf.PerlinNoise((i+x*127)*.1f,(j+z*127)*.1f)*.55f;
                paint[j,i,0]=(1-rock)*(1-dark);paint[j,i,1]=(1-rock)*dark;paint[j,i,2]=rock;
            }
            data.SetAlphamaps(0,0,paint);EditorUtility.SetDirty(data);
        }

        static void AttachOwnedControlMaps(TerrainData data,string path)
        {
            foreach(var texture in data.alphamapTextures)
            {
                if(texture==null)throw new InvalidOperationException("Owned terrain control map missing: "+path);
                if(AssetDatabase.Contains(texture))
                {
                    if(AssetDatabase.GetAssetPath(texture)!=path)throw new InvalidOperationException("External/custom control map preserved: "+path);
                }
                else AssetDatabase.AddObjectToAsset(texture,data);
                EditorUtility.SetDirty(texture);
            }
            EditorUtility.SetDirty(data);
        }
    }
}
