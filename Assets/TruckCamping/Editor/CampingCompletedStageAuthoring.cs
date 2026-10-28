using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace LWS.TruckCamping.Editor
{
    public static partial class CampingWildernessAuthoring
    {
        const string NormalizedStageManifest="Assets/TruckCamping/Editor/NavigationNormalizedStageV7.json";
        const string NormalizedWaterDigest="9a15ea778228f7a2d3c2878c674c8e215a76fdcabc062be76fff00be3586afa7";
        public static string ReviewedCompletedStageManifest => Digest(WaterMaterialPath)==NormalizedWaterDigest?NormalizedStageManifest:CompletedStageManifest;
        static bool IsCompletedStage(string path)=>path==CompletedStageManifest||path==NormalizedStageManifest;
        const string CompletedStageManifest="Assets/TruckCamping/Editor/NavigationCompletedStageV6.json";
        const string WaterMaterialPath="Assets/TruckCamping/Materials/Camp Haul River Modeler flowing water.mat";
        const string TerrainMaterialPath="Assets/TruckCamping/Materials/Camp Haul terrain surface.mat";
        static bool ResumingCompletedStage=>resumingKnownPartial&&IsCompletedStage(selectedStageManifest);
        static bool CompletedMaterialPath(string path)=>path==WaterMaterialPath||path==TerrainMaterialPath;
        static void VerifyExactCompletedMaterial(string path)
        {
            string expected=path==WaterMaterialPath?"b9c5fac55a360be0d107fcbe1641f1d7c9c090230af1b13cd96d539ceedf6b60":path==TerrainMaterialPath?"37b7d3193baa2df7cd5a83ff9290790d98da2167ea0b54a3e8ca196e648b30d5":null;
            if(expected==null||(Digest(path)!=expected&&(path!=WaterMaterialPath||Digest(path)!=NormalizedWaterDigest)))throw new InvalidOperationException("Unknown/custom material completion preserved: "+path);
        }
        [Serializable]sealed class WaterPreflightObservation
        {
            public string baseTexture,normalTexture;public Vector2 scale;public float bump,surface,src,dst,zwrite;public int queue;
            public bool normalKeyword,transparentKeyword,actualBaseMatchesLoadedVendor,actualNormalMatchesLoadedVendor;public string[] actualKeywords,failed;
        }
        public static void VerifyCompletedTerrainMaterialState()
        {
            VerifyExactCompletedMaterial(WaterMaterialPath);VerifyExactCompletedMaterial(TerrainMaterialPath);
            var water=AssetDatabase.LoadAssetAtPath<Material>(WaterMaterialPath);var surface=AssetDatabase.LoadAssetAtPath<Material>(TerrainMaterialPath);
            if(water==null||water.shader==null||water.shader.name!="Universal Render Pipeline/Lit"||surface==null||surface.shader==null||surface.shader.name!="Universal Render Pipeline/Terrain/Lit")throw new InvalidOperationException("Exact actual completed URP material foundations required.");
            var observation=new WaterPreflightObservation{
                baseTexture=AssetDatabase.GetAssetPath(water.GetTexture("_BaseMap")),normalTexture=AssetDatabase.GetAssetPath(water.GetTexture("_BumpMap")),scale=water.GetTextureScale("_BaseMap"),bump=water.GetFloat("_BumpScale"),surface=water.GetFloat("_Surface"),src=water.GetFloat("_SrcBlend"),dst=water.GetFloat("_DstBlend"),zwrite=water.GetFloat("_ZWrite"),queue=water.renderQueue,normalKeyword=water.IsKeywordEnabled("_NORMALMAP"),transparentKeyword=water.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"),actualKeywords=water.shaderKeywords,
                actualBaseMatchesLoadedVendor=water.GetTexture("_BaseMap")==AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RiverModeler/Textures/WaterFoam.png"),actualNormalMatchesLoadedVendor=water.GetTexture("_BumpMap")==AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RiverModeler/Textures/Splash_Normals.png")};
            var failures=new List<string>();
            if(observation.baseTexture!="Assets/RiverModeler/Textures/WaterFoam.png")failures.Add("BaseMap path="+observation.baseTexture);
            if(observation.normalTexture!="Assets/RiverModeler/Textures/Splash_Normals.png")failures.Add("BumpMap path="+observation.normalTexture);
            if(!observation.scale.Equals(new Vector2(9,9)))failures.Add("BaseMap scale="+observation.scale.ToString("R"));
            if(observation.bump!=.35f)failures.Add("BumpScale expected.35 actual="+observation.bump.ToString("R"));
            if(observation.surface!=1)failures.Add("Surface expected1 actual="+observation.surface.ToString("R"));
            if(observation.src!=1)failures.Add("URP preserve-specular SrcBlend expected1 actual="+observation.src.ToString("R"));
            if(water.GetFloat("_Blend")!=0||water.GetFloat("_BlendModePreserveSpecular")!=1||water.GetFloat("_SrcBlendAlpha")!=1||water.GetFloat("_DstBlendAlpha")!=10||!water.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON")||water.GetTag("RenderType",false)!="Transparent"||water.GetShaderPassEnabled("DepthOnly")||water.GetShaderPassEnabled("ShadowCaster")||water.GetTexture("_MainTex")!=water.GetTexture("_BaseMap")||!water.GetTextureScale("_MainTex").Equals(new Vector2(9,9)))failures.Add("Installed URP transparent preserve-specular surface state/legacy texture alias mismatch");
            if(observation.dst!=10)failures.Add("DstBlend expected10 actual="+observation.dst.ToString("R"));
            if(observation.zwrite!=0)failures.Add("ZWrite expected0 actual="+observation.zwrite.ToString("R"));
            if(observation.queue!=3000)failures.Add("Queue expected3000 actual="+observation.queue);
            if(!observation.normalKeyword)failures.Add("NORMALMAP keyword disabled");
            if(!observation.transparentKeyword)failures.Add("SURFACE_TYPE_TRANSPARENT keyword disabled");
            observation.failed=failures.ToArray();string directory="Logs/TruckSports/Impossible07/CampingCompletedStageWaterInputs";Directory.CreateDirectory(directory);string report=directory+"/runtime-water-inputs-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".json";File.WriteAllText(report,JsonUtility.ToJson(observation,true));
            if(failures.Count>0)throw new InvalidOperationException("Original completed flowing-water recipe mismatch: "+string.Join("; ",failures)+"; read-only inputs="+report);
            for(int z=0;z<2;z++)for(int x=0;x<2;x++)
            {
                string path="Assets/TruckCamping/Models/Camp Haul Vista terrain "+x+" "+z+".asset";var data=AssetDatabase.LoadAssetAtPath<TerrainData>(path);
                if(data==null||data.heightmapResolution!=257||data.alphamapResolution!=128||!data.size.Equals(new Vector3(1000,260,1000))||data.treeInstanceCount<=100||data.treePrototypes.Length!=1||AssetDatabase.GetAssetPath(data.treePrototypes[0].prefab)!=PineSource||data.treePrototypes[0].bendFactor!=.2f||data.terrainLayers.Length!=3)throw new InvalidOperationException("Exact completed four-tile forest required: "+path);
                for(int i=0;i<3;i++)if(AssetDatabase.GetAssetPath(data.terrainLayers[i])!=LayerSources[i])throw new InvalidOperationException("Original completed terrain layers changed.");
                foreach(var texture in data.alphamapTextures)if(texture==null||AssetDatabase.GetAssetPath(texture)!=path)throw new InvalidOperationException("Original same-GUID control texture required.");
            }
        }
        static Transform ReuseExactCompletedRoad(Material material)
        {
            if(!ResumingCompletedStage)throw new InvalidOperationException("Exact reviewed completed-stage reconstruction required.");
            VerifyRetainedInputs(false);var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(ReviewedRoadAsset);
            if(mesh==null||mesh.vertexCount!=15496||mesh.subMeshCount!=1)throw new InvalidOperationException("Original completed vendor road Mesh/GUID required.");
            var root=new GameObject("Camp Haul scenic woodland loop · EasyRoads generated course").transform;var piece=new GameObject("EasyRoads physical surface 0");piece.transform.SetParent(root,false);piece.AddComponent<MeshFilter>().sharedMesh=mesh;piece.AddComponent<MeshRenderer>().sharedMaterial=material;piece.AddComponent<MeshCollider>().sharedMesh=mesh;return root;
        }
    }
}
