using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckFishing.Editor
{
    // Explicit, versioned repair of the saved sport. Never runs during Play,
    // ordinary builds or scene initialization; subsequent designer edits survive.
    public static class FishingRepairAuthoring
    {
        const string Models="Assets/TruckFishing/Models/";
        public static void Upgrade()
        {
            var scene=EditorSceneManager.OpenScene(FishingAuthoring.Scene);
            var match=UnityEngine.Object.FindAnyObjectByType<FishingMatch>();
            if(match.humanPlaytestVersion>=1){FishingAuthoring.Validate();return;}
            foreach(var prefab in match.streams.SelectMany(s=>s.fishPrefabs).Distinct())
            {
                string path=AssetDatabase.GetAssetPath(prefab);
                if(!path.StartsWith("Assets/TruckFishing/Prefabs/",StringComparison.Ordinal))throw new InvalidOperationException("Only sport-owned fish variants may be resized: "+path);
                var content=PrefabUtility.LoadPrefabContents(path);
                try{content.transform.localScale=Vector3.one*1.3f;PrefabUtility.SaveAsPrefabAsset(content,path);}
                finally{PrefabUtility.UnloadPrefabContents(content);}
            }
            var original=match.streams[0].GetComponentInChildren<Terrain>();
            var ground=Ground(original.terrainData);var water=Water();
            foreach(var stream in match.streams)
            {
                stream.netHalfTravel=2.6f;
                var terrain=stream.GetComponentInChildren<Terrain>();terrain.terrainData=ground;
                terrain.GetComponent<TerrainCollider>().terrainData=ground;
                terrain.transform.localPosition=new Vector3(-18,-2,-8);
                terrain.SetNeighbors(null,null,null,null);
                var continuation=stream.transform.Find("Offscreen upstream river continuation");if(continuation!=null)continuation.gameObject.SetActive(false);
                var old=stream.transform.Find("Riverbank scenery");if(old!=null)old.gameObject.SetActive(false);
                stream.transform.Find("River Modeler clear flowing surface").GetComponent<MeshFilter>().sharedMesh=water;
                Dress(stream.transform,terrain);
                var camera=match.streamCameras[stream.slot].view;camera.farClipPlane=Mathf.Max(camera.farClipPlane,300);
                match.framing[stream.slot].arenaBounds=new Bounds(stream.transform.position+new Vector3(2,.3f,10),new Vector3(15,.6f,22));
                EditorUtility.SetDirty(stream);EditorUtility.SetDirty(camera);EditorUtility.SetDirty(match.framing[stream.slot]);
            }
            match.originalInitialSpeed=2.5f;match.initialSpeedMultiplier=2;match.multiplierPerThirtySeconds=.5f;match.maximumSpeedMultiplier=6;match.fishHalfWidth=2.5f;match.humanPlaytestVersion=1;
            EditorUtility.SetDirty(match);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();FishingAuthoring.Validate();
            Debug.Log("FISHING_PLAYTEST_REPAIR_AUTHORED: preserved scene, half-width targets, winding240m channel, installed bank assets,130% sport-owned fish, configurable2x baseline progression");
        }
        static float Height(float x,float z)
        {
            float distance=Mathf.Abs(x-FishingStream.ChannelCenter(z));
            float bank=Mathf.SmoothStep(0,1,Mathf.InverseLerp(4.05f,5.8f,distance));
            // The original parked truck and wooden pier remain in their clear
            // dock apron; narrower banks must not grow through either model.
            if(Mathf.Abs(x)<8.5f)bank*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-1,1,z));
            return Mathf.Lerp(-.8f,.65f+1.4f*Mathf.PerlinNoise(x*.14f+20,z*.08f),bank);
        }
        static TerrainData Ground(TerrainData source)
        {
            string path=Models+"Winding playtest riverbanks.asset";var data=AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            if(data==null){data=new TerrainData();AssetDatabase.CreateAsset(data,path);}
            data.heightmapResolution=257;data.size=new Vector3(36,5,248);data.terrainLayers=source.terrainLayers;
            var heights=new float[257,257];for(int z=0;z<257;z++)for(int x=0;x<257;x++)heights[z,x]=(Height(-18+x*36f/256,-8+z*248f/256)+2)/5;
            data.SetHeights(0,0,heights);data.alphamapResolution=256;int layers=data.terrainLayers.Length;
            if(layers<2)throw new InvalidOperationException("Existing grassy/pebbled river terrain layers are required.");
            var paint=new float[256,256,layers];for(int z=0;z<256;z++)for(int x=0;x<256;x++){float xx=-18+x*36f/255,zz=-8+z*248f/255;float bank=Mathf.SmoothStep(0,1,Mathf.InverseLerp(4.2f,5.5f,Mathf.Abs(xx-FishingStream.ChannelCenter(zz))));paint[z,x,0]=bank;paint[z,x,1]=1-bank;}
            data.SetAlphamaps(0,0,paint);EditorUtility.SetDirty(data);return data;
        }
        static Mesh Water()
        {
            string path=Models+"Winding playtest river surface.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh{name="Owned winding River Modeler surface"};AssetDatabase.CreateAsset(mesh,path);}
            const int rows=118;var vertices=new Vector3[rows*2];var normals=new Vector3[rows*2];var tangents=new Vector4[rows*2];var uv=new Vector2[rows*2];var flow=new Vector2[rows*2];var colors=new Color[rows*2];var indices=new int[(rows-1)*6];
            for(int row=0;row<rows;row++){float z=-2+row*2;for(int side=0;side<2;side++){int i=row*2+side;vertices[i]=new Vector3(FishingStream.ChannelCenter(z)+(side==0?-4.2f:4.2f),.02f,z);normals[i]=Vector3.up;tangents[i]=new Vector4(1,0,0,1);uv[i]=new Vector2(side*8.4f,z);flow[i]=new Vector2(0,-.1f);colors[i]=Color.white;}if(row<rows-1){int i=row*6,v=row*2;indices[i]=v;indices[i+1]=v+3;indices[i+2]=v+1;indices[i+3]=v;indices[i+4]=v+2;indices[i+5]=v+3;}}
            mesh.Clear();mesh.vertices=vertices;mesh.normals=normals;mesh.tangents=tangents;mesh.uv=uv;mesh.colors=colors;mesh.SetUVs(2,flow);mesh.triangles=indices;mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);return mesh;
        }
        static void Dress(Transform parent,Terrain terrain)
        {
            if(parent.Find("Winding river owned-asset scenery")!=null)return;
            var root=new GameObject("Winding river owned-asset scenery").transform;root.SetParent(parent,false);
            string[] paths={"Assets/TruckFishing/Prefabs/River bank grass.prefab","Assets/TruckFishing/Prefabs/River boulder 0.prefab","Assets/TruckFishing/Prefabs/River boulder 1.prefab","Assets/JC_LP_MegaCity/Prefabs/Nature/SM_Nature_Tree_03.prefab","Assets/JC_LP_MegaCity/Prefabs/Nature/SM_Nature_Tree_07.prefab"};
            var assets=paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();if(assets.Any(p=>p==null))throw new InvalidOperationException("Installed owned riverbank/JC MegaCity scenery is missing.");
            var random=new System.Random(20261021);
            for(int i=0;i<100;i++)
            {
                int kind=i%7==0?3+i%2:i%3;float z=1+(float)random.NextDouble()*100;
                float x=FishingStream.ChannelCenter(z)+(i%2==0?-1:1)*(5.3f+(float)random.NextDouble()*2.4f);
                var model=(GameObject)PrefabUtility.InstantiatePrefab(assets[kind]);model.transform.SetParent(root,false);model.transform.localRotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0);model.transform.localScale*=.8f+(float)random.NextDouble()*.5f;
                model.transform.localPosition=new Vector3(x,0,z);var renderers=model.GetComponentsInChildren<Renderer>(true);
                if(renderers.Length>0){var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);float ground=terrain.SampleHeight(parent.TransformPoint(new Vector3(x,0,z)))+terrain.transform.position.y;model.transform.position+=Vector3.up*(ground-bounds.min.y-.04f);}
            }
        }
    }
}
