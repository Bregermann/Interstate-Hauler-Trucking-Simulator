using UnityEngine;
using UnityEngine.Rendering;

namespace LWS.TruckTaxi
{
    // Persistent static image planes under the existing Compass camera; never loads scenery.
    public sealed class TruckTaxiMapTileLayer : MonoBehaviour
    {
        public const int MapLayer=30;
        private Material material;
        private Camera mapCamera;
        private MeshRenderer[] renderers;
        public bool Initialize(TruckTaxiMapTileCatalog catalog)
        {
            if(catalog==null || catalog.tiles==null || catalog.tiles.Length==0 || catalog.tileMaterial==null)
            {
                Debug.LogError("Truck Taxi regional map requires baked tiles and their referenced material. Run Truck Taxi/Regional/Rebuild Map Tiles.",this);
                return false;
            }
            material=new Material(catalog.tileMaterial) { name="Truck Taxi regional map tile" };
            for(int i=0;i<catalog.tiles.Length;i++)
            {
                var tile=catalog.tiles[i];
                if(tile==null || tile.texture==null || tile.size.x<=0 || tile.size.y<=0) continue;
                var go=new GameObject("Map tile: "+tile.sceneName,typeof(MeshFilter),typeof(MeshRenderer));
                go.layer=MapLayer; go.transform.SetParent(transform,false);
                go.transform.position=new Vector3(tile.center.x,-35+i*.01f,tile.center.y);
                var mesh=new Mesh { name="Map tile quad" };
                float x=tile.size.x*.5f,z=tile.size.y*.5f;
                mesh.vertices=new[] { new Vector3(-x,0,-z),new Vector3(-x,0,z),new Vector3(x,0,z),new Vector3(x,0,-z) };
                mesh.uv=new[] { Vector2.zero,Vector2.up,Vector2.one,new Vector2(1,0) };
                mesh.triangles=new[] { 0,1,2,0,2,3 };
                mesh.RecalculateNormals(); go.GetComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.GetComponent<MeshRenderer>();
                var tileMaterial=new Material(material) { name="Map tile: "+tile.sceneName };
                tileMaterial.mainTexture=tile.texture;
                if(tileMaterial.HasProperty("_BaseMap")) tileMaterial.SetTexture("_BaseMap",tile.texture);
                renderer.sharedMaterial=tileMaterial;
                renderer.enabled=false;
            }
            renderers=GetComponentsInChildren<MeshRenderer>();
            RenderPipelineManager.beginCameraRendering+=BeginCamera;
            RenderPipelineManager.endCameraRendering+=EndCamera;
            return true;
        }
        public void ShowOnlyFor(Camera camera)
        {
            if(mapCamera==camera) return;
            mapCamera=camera;
            if(renderers!=null) foreach(var renderer in renderers) if(renderer!=null) renderer.enabled=false;
        }
        private void BeginCamera(ScriptableRenderContext context,Camera camera)
        {
            if(renderers==null) return;
            bool visible=mapCamera!=null && camera==mapCamera;
            foreach(var renderer in renderers) if(renderer!=null) renderer.enabled=visible;
        }
        private void EndCamera(ScriptableRenderContext context,Camera camera)
        {
            if(renderers!=null) foreach(var renderer in renderers) if(renderer!=null) renderer.enabled=false;
        }
        private void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering-=BeginCamera;
            RenderPipelineManager.endCameraRendering-=EndCamera;
            foreach(var filter in GetComponentsInChildren<MeshFilter>())
                if(filter.sharedMesh!=null) Destroy(filter.sharedMesh);
            foreach(var renderer in GetComponentsInChildren<MeshRenderer>())
                if(renderer.sharedMaterial!=null) Destroy(renderer.sharedMaterial);
            if(material!=null) Destroy(material);
        }
    }
}
