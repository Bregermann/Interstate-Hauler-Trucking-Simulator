using UnityEngine;
using UnityEngine.Rendering;
namespace LWS.TruckHorrorSports
{
    public sealed class HorrorSecurityMonitor : MonoBehaviour
    {
        public HorrorMatch match;
        public Camera securityCamera;
        public Renderer display;
        public bool HasFrame { get; private set; }
        public float LastRenderedTime { get; private set; }=-1;
        public Vector3 LastRenderedPosition { get; private set; }
        public Quaternion LastRenderedRotation { get; private set; }
        RenderTexture image;
        Material material;
        float nextFrame;
        void OnEnable() { RenderPipelineManager.endCameraRendering += Rendered; }
        void Rendered(ScriptableRenderContext context, Camera camera) { if (camera == securityCamera && image != null) {HasFrame = true;LastRenderedTime=Time.unscaledTime;LastRenderedPosition=camera.transform.position;LastRenderedRotation=camera.transform.rotation;} }
        public void ResetFeed() { HasFrame = false; nextFrame = 0; LastRenderedTime=-1; }
        public void SelectView(Transform station,float fieldOfView)
        {
            securityCamera.transform.SetPositionAndRotation(station.position,station.rotation);securityCamera.fieldOfView=fieldOfView;ResetFeed();
        }
        void Start()
        {
            image = new RenderTexture(320, 180, 16) { name = "Night stop security feed", hideFlags = HideFlags.DontSave };
            image.Create(); securityCamera.targetTexture = image; securityCamera.enabled = false;
            material = new Material(display.sharedMaterial) { hideFlags = HideFlags.DontSave }; material.mainTexture = image; display.sharedMaterial = material;
        }
        void Update()
        {
            securityCamera.enabled = match != null && match.Live && Time.unscaledTime >= nextFrame;
            if (securityCamera.enabled) nextFrame = Time.unscaledTime + .5f;
        }
        void OnDisable() { RenderPipelineManager.endCameraRendering -= Rendered; if (securityCamera != null) securityCamera.enabled = false; }
        void OnDestroy()
        {
            if (securityCamera != null) securityCamera.targetTexture = null;
            if (image != null) { image.Release(); Destroy(image); }
            if (material != null) Destroy(material);
        }
    }
}
