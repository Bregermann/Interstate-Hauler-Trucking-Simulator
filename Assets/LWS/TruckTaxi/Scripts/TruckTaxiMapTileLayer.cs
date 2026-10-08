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
        private bool capturing;
        private int captureMask;
        private float captureFarClip;
        private bool captureOcclusion;
        private CameraClearFlags captureClearFlags;
        private Color captureBackground;
        public bool IsAssignedTo(Camera camera) => camera!=null && camera==mapCamera && renderers?.Length>0;
        public bool Initialize(TruckTaxiMapTileCatalog catalog)
        {
            if(renderers!=null) return renderers.Length>0;
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
                renderer.shadowCastingMode=ShadowCastingMode.Off;
                renderer.receiveShadows=false;
                renderer.enabled=false;
            }
            renderers=GetComponentsInChildren<MeshRenderer>();
            return renderers.Length>0;
        }
        public void ShowOnlyFor(Camera camera)
        {
            if(mapCamera==camera) return;
            EndCapture();
            mapCamera=camera;
        }
        // Compass brackets its explicit Camera.Render with these public capture events.
        // SRP callbacks alone are not a reliable ownership boundary for manual captures.
        public void BeginCapture()
        {
            if(capturing || mapCamera==null || renderers==null || renderers.Length==0) return;
            capturing=true;
            captureMask=mapCamera.cullingMask; captureFarClip=mapCamera.farClipPlane;
            captureOcclusion=mapCamera.useOcclusionCulling;
            captureClearFlags=mapCamera.clearFlags; captureBackground=mapCamera.backgroundColor;
            // Do not mix streamed world geometry with the complete baked regional atlas.
            // Save AFTER vendor setup, which can reset these camera properties on each open.
            mapCamera.cullingMask=1<<MapLayer;
            mapCamera.useOcclusionCulling=false;
            mapCamera.clearFlags=CameraClearFlags.SolidColor;
            mapCamera.backgroundColor=new Color(.14f,.22f,.2f,1);
            foreach(var renderer in renderers) if(renderer!=null)
            {
                renderer.enabled=true;
                mapCamera.farClipPlane=Mathf.Max(mapCamera.farClipPlane,mapCamera.transform.position.y-renderer.bounds.min.y+1);
            }
        }
        public void EndCapture()
        {
            if(renderers!=null) foreach(var renderer in renderers) if(renderer!=null) renderer.enabled=false;
            if(capturing && mapCamera!=null)
            {
                mapCamera.cullingMask=captureMask; mapCamera.farClipPlane=captureFarClip;
                mapCamera.useOcclusionCulling=captureOcclusion;
                mapCamera.clearFlags=captureClearFlags; mapCamera.backgroundColor=captureBackground;
            }
            capturing=false;
        }
        private void OnDisable() => EndCapture();
        private void OnDestroy()
        {
            EndCapture();
            foreach(var filter in GetComponentsInChildren<MeshFilter>())
                if(filter.sharedMesh!=null) Release(filter.sharedMesh);
            foreach(var renderer in GetComponentsInChildren<MeshRenderer>())
                if(renderer.sharedMaterial!=null) Release(renderer.sharedMaterial);
            if(material!=null) Release(material);
        }
        private static void Release(Object value)
        { if(Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
    }
}
