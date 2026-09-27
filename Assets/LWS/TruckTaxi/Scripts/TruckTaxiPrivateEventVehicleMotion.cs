using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Temporary render proxies rock while NWH's entire physical hierarchy stays in place.
    public sealed class TruckTaxiPrivateEventVehicleMotion : MonoBehaviour
    {
        [Header("Render-only exaggerated hydraulics")]
        [Range(0,.5f)] public float bounceHeight=.24f;
        [Range(0,25)] public float rollDegrees=14;
        [Range(0,20)] public float pitchDegrees=9;
        [Range(0,.8f)] public float finalBounceHeight=.45f;
        private readonly Dictionary<Transform, Transform> proxyTransforms = new Dictionary<Transform, Transform>();
        private readonly List<Renderer> originals = new List<Renderer>();
        private readonly List<bool> originalEnabled = new List<bool>();
        private readonly List<Mesh> bakedMeshes = new List<Mesh>();
        private Transform truckRoot;
        private Rigidbody truckBody;
        private GameObject proxyRoot;
        private float elapsed;
        public bool IsActive { get; private set; }
        public bool Ready => truckRoot != null;

        public bool Initialize(Transform playerTruckRoot)
        {
            StopMotion();
            truckBody = playerTruckRoot != null ? playerTruckRoot.GetComponent<Rigidbody>() : null;
            truckRoot = truckBody != null ? playerTruckRoot : null;
            return Ready;
        }

        public bool Begin()
        {
            if (!Ready || IsActive) return false;
            var renderers = truckRoot.GetComponentsInChildren<Renderer>(true);
            proxyRoot = new GameObject("Truck Taxi temporary body presentation");
            proxyRoot.transform.SetParent(truckRoot, false);
            proxyTransforms.Add(truckRoot, proxyRoot.transform);
            IsActive = true;
            elapsed = 0;
            try
            {
                foreach (var source in renderers)
                {
                    if (source == null || !source.enabled || !source.gameObject.activeInHierarchy ||
                        source.GetComponentInParent<Rigidbody>() != truckBody) continue;
                    Mesh mesh = null;
                    if (source is MeshRenderer)
                        mesh = source.GetComponent<MeshFilter>()?.sharedMesh;
                    else if (source is SkinnedMeshRenderer skin && skin.sharedMesh != null)
                    {
                        mesh = new Mesh { name = "Truck Taxi temporary skinned pose" };
                        skin.BakeMesh(mesh);
                        bakedMeshes.Add(mesh);
                    }
                    if (mesh == null) continue;
                    Transform copyParent = ProxyTransform(source.transform);
                    var copy = new GameObject("Renderer proxy");
                    copy.transform.SetParent(copyParent, false);
                    copy.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var display = copy.AddComponent<MeshRenderer>();
                    display.sharedMaterials = source.sharedMaterials;
                    display.shadowCastingMode = source.shadowCastingMode;
                    display.receiveShadows = source.receiveShadows;
                    display.lightProbeUsage = source.lightProbeUsage;
                    display.reflectionProbeUsage = source.reflectionProbeUsage;
                    display.sortingLayerID = source.sortingLayerID;
                    display.sortingOrder = source.sortingOrder;
                    var block = new MaterialPropertyBlock();
                    source.GetPropertyBlock(block);
                    display.SetPropertyBlock(block);
                    originals.Add(source);
                    originalEnabled.Add(source.enabled);
                }
                if (originals.Count == 0) { StopMotion(); return false; }
                foreach (var source in originals) if (source != null) source.enabled = false;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                StopMotion();
                return false;
            }
        }

        private Transform ProxyTransform(Transform source)
        {
            if (proxyTransforms.TryGetValue(source, out var existing)) return existing;
            var parent = ProxyTransform(source.parent);
            var copy = new GameObject(source.name + " presentation").transform;
            copy.SetParent(parent, false);
            copy.localPosition = source.localPosition;
            copy.localRotation = source.localRotation;
            copy.localScale = source.localScale;
            proxyTransforms.Add(source, copy);
            return copy;
        }

        public void Step(float deltaTime)
        {
            if (!IsActive || proxyRoot == null || deltaTime <= 0 || !float.IsFinite(deltaTime)) return;
            elapsed += deltaTime;
            float pulse = Mathf.Sin(elapsed * 11f) * .7f + Mathf.Sin(elapsed * 16.7f) * .3f;
            float sway = Mathf.Sin(elapsed * 8.5f) + .28f * Mathf.Sin(elapsed * 19.3f);
            float finalBounce = Mathf.Exp(-Mathf.Pow((elapsed - 6.4f) * 3f, 2f)) * finalBounceHeight;
            proxyRoot.transform.localPosition = new Vector3(.075f * sway, bounceHeight * pulse + finalBounce, 0);
            proxyRoot.transform.localRotation = Quaternion.Euler(pitchDegrees * pulse, 0, rollDegrees * sway);
        }

        private void LateUpdate() => Step(Time.deltaTime);

        public void StopMotion()
        {
            for (int i = 0; i < originals.Count; i++)
                if (originals[i] != null) originals[i].enabled = originalEnabled[i];
            originals.Clear();
            originalEnabled.Clear();
            proxyTransforms.Clear();
            if (proxyRoot != null) { proxyRoot.SetActive(false); DestroyOwned(proxyRoot); }
            proxyRoot = null;
            foreach (var mesh in bakedMeshes) if (mesh != null) DestroyOwned(mesh);
            bakedMeshes.Clear();
            IsActive = false;
        }

        private static void DestroyOwned(UnityEngine.Object item)
        {
            if (Application.isPlaying) Destroy(item);
            else DestroyImmediate(item);
        }

        private void OnDisable() => StopMotion();
        private void OnDestroy() => StopMotion();
    }
}
