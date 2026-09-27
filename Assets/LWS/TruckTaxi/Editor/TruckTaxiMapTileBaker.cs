using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiMapTileBaker
    {
        private const string Folder="Assets/LWS/TruckTaxi/Resources/TruckTaxi/MapTiles";
        private const string CatalogPath="Assets/LWS/TruckTaxi/Resources/TruckTaxi/RegionalMapTiles.asset";
        private const int Resolution=1024;

        [MenuItem("Truck Taxi/Regional/Rebuild Map Tiles")]
        public static void RebuildMapTiles()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before rebuilding regional map tiles.");
            Directory.CreateDirectory(Folder);
            var tiles=new List<TruckTaxiMapTileCatalog.Tile>();
            var scenes=AssetDatabase.FindAssets("t:Scene",new[] { TruckTaxiRegionalAuthoring.Root+"/Scenes" });
            if(scenes.Length==0) throw new InvalidOperationException("No authored Taxi regional scenes were found.");
            Array.Sort(scenes,(a,b)=>string.CompareOrdinal(AssetDatabase.GUIDToAssetPath(a),AssetDatabase.GUIDToAssetPath(b)));
            Scene metadataScene=default;
            bool openedMetadata=false;
            if(UnityEngine.Object.FindFirstObjectByType<TruckTaxiRegionalWorld>()==null)
            {
                if(AssetDatabase.LoadAssetAtPath<SceneAsset>(TruckTaxiDemoBuilder.ScenePath)==null)
                    throw new InvalidOperationException("The persistent Truck Taxi scene is missing.");
                metadataScene=EditorSceneManager.OpenScene(TruckTaxiDemoBuilder.ScenePath,OpenSceneMode.Additive);
                openedMetadata=true;
            }
            try
            {
                if(UnityEngine.Object.FindFirstObjectByType<TruckTaxiRegionalWorld>()==null)
                    throw new InvalidOperationException("Persistent regional metadata was not found; map tiles were not rebuilt.");
                foreach(var guid in scenes)
                {
                    string path=AssetDatabase.GUIDToAssetPath(guid);
                    string sceneName=Path.GetFileNameWithoutExtension(path);
                    if(!sceneName.StartsWith("Taxi_",StringComparison.Ordinal)) continue;
                    var scene=SceneManager.GetSceneByPath(path);
                    bool opened=!scene.IsValid() || !scene.isLoaded;
                    if(opened) scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                    try
                    {
                        if(TryBake(scene,sceneName,out var tile)) tiles.Add(tile);
                    }
                    finally
                    {
                        if(opened) EditorSceneManager.CloseScene(scene,true);
                    }
                }
            }
            finally
            {
                if(openedMetadata) EditorSceneManager.CloseScene(metadataScene,true);
            }
            if(tiles.Count==0) throw new InvalidOperationException("No static regional geometry was baked; the existing map catalog was preserved.");
            var catalog=AssetDatabase.LoadAssetAtPath<TruckTaxiMapTileCatalog>(CatalogPath);
            if(catalog==null)
            {
                catalog=ScriptableObject.CreateInstance<TruckTaxiMapTileCatalog>();
                AssetDatabase.CreateAsset(catalog,CatalogPath);
            }
            catalog.tiles=tiles.ToArray();
            EnsureRuntimeMaterial(catalog);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"Truck Taxi map tiles rebuilt: {tiles.Count} static regional scenes. No region scene was saved.");
        }

        public static void EnsureRuntimeMaterial(TruckTaxiMapTileCatalog catalog)
        {
            if(catalog==null) throw new ArgumentNullException(nameof(catalog));
            const string materialPath="Assets/LWS/TruckTaxi/Resources/TruckTaxi/RegionalMapTile.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null)
            {
                var shader=Shader.Find("Universal Render Pipeline/Unlit");
                if(shader==null) throw new InvalidOperationException("The installed URP Unlit shader is unavailable.");
                material=new Material(shader) { name="RegionalMapTile",color=Color.white };
                AssetDatabase.CreateAsset(material,materialPath);
            }
            catalog.tileMaterial=material;
            EditorUtility.SetDirty(catalog);
        }

        private static bool TryBake(Scene scene,string sceneName,out TruckTaxiMapTileCatalog.Tile tile)
        {
            tile=null;
            var sources=new List<MeshRenderer>();
            var bounds=new Bounds(); bool hasBounds=false;
            foreach(var root in scene.GetRootGameObjects())
                foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if(!renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                       renderer.GetComponent<MeshFilter>()?.sharedMesh==null ||
                       renderer.GetComponentInParent<Rigidbody>()!=null ||
                       renderer.GetComponentInParent<Animator>()!=null) continue;
                    sources.Add(renderer);
                    if(!hasBounds) { bounds=renderer.bounds; hasBounds=true; }
                    else bounds.Encapsulate(renderer.bounds);
                }
            if(!hasBounds) return false;
            var world=UnityEngine.Object.FindFirstObjectByType<TruckTaxiRegionalWorld>();
            if(world!=null)
                foreach(var region in world.regions)
                    if(region!=null && region.sceneName==sceneName)
                    {
                        bounds=new Bounds(new Vector3(region.bounds.center.x,bounds.center.y,region.bounds.center.z),
                            new Vector3(region.bounds.size.x,bounds.size.y,region.bounds.size.z));
                        break;
                    }
            var proxyRoot=new GameObject("Temporary static map bake geometry");
            proxyRoot.hideFlags=HideFlags.HideAndDontSave;
            var cameraObject=new GameObject("Temporary map tile camera",typeof(Camera));
            cameraObject.hideFlags=HideFlags.HideAndDontSave;
            var materials=new Dictionary<Color32,Material>();
            RenderTexture target=null;
            Texture2D capture=null;
            var previous=RenderTexture.active;
            try
            {
                foreach(var source in sources)
                {
                    var go=new GameObject(source.name,typeof(MeshFilter),typeof(MeshRenderer));
                    go.hideFlags=HideFlags.HideAndDontSave;
                    go.layer=TruckTaxiMapTileLayer.MapLayer;
                    go.transform.SetParent(proxyRoot.transform,false);
                    go.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
                    go.transform.localScale=source.transform.lossyScale;
                    var mesh=source.GetComponent<MeshFilter>().sharedMesh;
                    go.GetComponent<MeshFilter>().sharedMesh=mesh;
                    var sourceMaterials=source.sharedMaterials;
                    var flats=new Material[mesh.subMeshCount];
                    for(int submesh=0;submesh<flats.Length;submesh++)
                    {
                        var material=sourceMaterials.Length>0 ? sourceMaterials[Mathf.Min(submesh,sourceMaterials.Length-1)] : null;
                        Color color=material!=null && material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") :
                            material!=null && material.HasProperty("_Color") ? material.color : new Color(.45f,.5f,.5f);
                        var key=(Color32)color;
                        if(!materials.TryGetValue(key,out var flat))
                        {
                            flat=new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color=color };
                            materials.Add(key,flat);
                        }
                        flats[submesh]=flat;
                    }
                    go.GetComponent<MeshRenderer>().sharedMaterials=flats;
                }
                float width=Mathf.Max(50,bounds.size.x+20),height=Mathf.Max(50,bounds.size.z+20);
                int textureWidth=width>=height ? Resolution : Mathf.Max(64,Mathf.RoundToInt(Resolution*width/height));
                int textureHeight=height>=width ? Resolution : Mathf.Max(64,Mathf.RoundToInt(Resolution*height/width));
                var camera=cameraObject.GetComponent<Camera>();
                camera.enabled=false; camera.orthographic=true; camera.aspect=(float)textureWidth/textureHeight;
                camera.orthographicSize=height*.5f;
                camera.transform.SetPositionAndRotation(new Vector3(bounds.center.x,bounds.max.y+100,bounds.center.z),Quaternion.Euler(90,0,0));
                camera.nearClipPlane=.1f; camera.farClipPlane=Mathf.Max(250,bounds.size.y+250);
                camera.cullingMask=1<<TruckTaxiMapTileLayer.MapLayer;
                camera.clearFlags=CameraClearFlags.SolidColor;
                camera.backgroundColor=new Color(.14f,.22f,.2f,1);
                target=RenderTexture.GetTemporary(textureWidth,textureHeight,24,RenderTextureFormat.ARGB32);
                camera.targetTexture=target; camera.Render();
                RenderTexture.active=target;
                capture=new Texture2D(textureWidth,textureHeight,TextureFormat.RGBA32,false);
                capture.ReadPixels(new Rect(0,0,textureWidth,textureHeight),0,0);
                capture.Apply();
                string imagePath=Folder+"/"+sceneName+".png";
                File.WriteAllBytes(imagePath,capture.EncodeToPNG());
                AssetDatabase.ImportAsset(imagePath,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(imagePath);
                importer.textureType=TextureImporterType.Default; importer.mipmapEnabled=false;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                tile=new TruckTaxiMapTileCatalog.Tile { sceneName=sceneName,
                    texture=AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath),
                    center=new Vector2(bounds.center.x,bounds.center.z),size=new Vector2(width,height) };
                return tile.texture!=null;
            }
            finally
            {
                RenderTexture.active=previous;
                if(target!=null) RenderTexture.ReleaseTemporary(target);
                if(capture!=null) UnityEngine.Object.DestroyImmediate(capture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(proxyRoot);
                foreach(var material in materials.Values) UnityEngine.Object.DestroyImmediate(material);
            }
        }
    }
}
