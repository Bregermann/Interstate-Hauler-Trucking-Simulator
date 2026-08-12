using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [Serializable]
    public struct LwsTruckMirrorRuntimeState
    {
        public LwsMirrorQuality quality;
        public bool leftActive;
        public bool rightActive;
        public int resolutionPixels;
        public int updateIntervalFrames;
        public float nearClip;
        public float farClip;
        public float fieldOfView;
        public int activeMirrorCount;
        public string notes;
    }

    [DisallowMultipleComponent]
    public sealed class LwsTruckMirrorController : MonoBehaviour
    {
        [SerializeField] private LwsTruckDashboardDefinition dashboardDefinition;
        [SerializeField] private Camera leftMirrorCamera;
        [SerializeField] private Camera rightMirrorCamera;
        [SerializeField] private Renderer leftMirrorRenderer;
        [SerializeField] private Renderer rightMirrorRenderer;
        [SerializeField] private LwsMirrorQuality currentQuality = LwsMirrorQuality.High;
        [SerializeField] private bool useRenderingServiceQuality = true;
        [SerializeField] private bool excludeUiLayer = true;
        [SerializeField] private Vector2 leftAdjustmentDegrees;
        [SerializeField] private Vector2 rightAdjustmentDegrees;

        private readonly Dictionary<Camera, RenderTexture> _runtimeTextures = new Dictionary<Camera, RenderTexture>();
        private readonly Dictionary<Camera, RenderTexture> _originalTextures = new Dictionary<Camera, RenderTexture>();
        private readonly Dictionary<Renderer, Material> _runtimeMaterials = new Dictionary<Renderer, Material>();
        private readonly Dictionary<Camera, Quaternion> _baseRotations = new Dictionary<Camera, Quaternion>();
        private ILwsRenderingService _renderingService;
        private LwsTruckMirrorRuntimeState _state;
        private int _lastManualRenderFrame = -1;
        private bool _initialized;

        public LwsTruckMirrorRuntimeState CurrentState => _state;
        public LwsMirrorQuality CurrentQuality => currentQuality;
        public Camera LeftMirrorCamera => leftMirrorCamera;
        public Camera RightMirrorCamera => rightMirrorCamera;

        private void Awake()
        {
            ResolveReferences();
            CaptureBaseRotations();
        }

        private void Start()
        {
            ResolveServices();
            ApplyResolvedQuality();
        }

        private void LateUpdate()
        {
            ResolveServices();
            if (useRenderingServiceQuality)
            {
                LwsMirrorQuality serviceQuality = ResolveQualityFromRenderingService();
                if (serviceQuality != currentQuality)
                {
                    SetQuality(serviceQuality);
                }
            }

            RenderManualMirrorsIfNeeded();
        }

        private void OnDestroy()
        {
            RestoreOriginalTextures();
            ReleaseRuntimeTextures();
        }

        public void Configure(LwsTruckDashboardDefinition definition)
        {
            dashboardDefinition = definition;
            if (definition != null)
            {
                currentQuality = definition.DefaultMirrorQuality;
            }

            ApplyResolvedQuality();
        }

        public void SetQuality(LwsMirrorQuality quality)
        {
            currentQuality = quality;
            ApplyResolvedQuality();
        }

        public void CycleQuality()
        {
            LwsMirrorQuality[] values = (LwsMirrorQuality[])Enum.GetValues(typeof(LwsMirrorQuality));
            int index = Array.IndexOf(values, currentQuality);
            SetQuality(values[(index + 1) % values.Length]);
        }

        public void SetMirrorAdjustment(bool left, Vector2 adjustmentDegrees)
        {
            if (left)
            {
                leftAdjustmentDegrees = adjustmentDegrees;
            }
            else
            {
                rightAdjustmentDegrees = adjustmentDegrees;
            }

            ApplyMirrorAdjustments();
        }

        public bool ValidateRequiredMirrors(out string message)
        {
            ResolveReferences();
            if (leftMirrorCamera == null || rightMirrorCamera == null)
            {
                message = "Required left/right mirror cameras are missing.";
                return false;
            }

            if (leftMirrorRenderer == null || rightMirrorRenderer == null)
            {
                message = "Required left/right mirror glass renderers are missing.";
                return false;
            }

            message = "Required mirror cameras and mirror glass renderers are assigned.";
            return true;
        }

        private void ResolveReferences()
        {
            if (dashboardDefinition == null)
            {
                LwsPlayerTruck truck = GetComponent<LwsPlayerTruck>();
                dashboardDefinition = truck != null && truck.Definition != null ? truck.Definition.DashboardDefinition : null;
            }

            if (leftMirrorCamera == null)
            {
                leftMirrorCamera = FindCameraByName("RenderTextureMirrorCameraL", "MirrorCameraL", "Mirror L");
            }

            if (rightMirrorCamera == null)
            {
                rightMirrorCamera = FindCameraByName("RenderTextureMirrorCameraR", "MirrorCameraR", "Mirror R");
            }

            if (leftMirrorRenderer == null)
            {
                leftMirrorRenderer = FindRendererByName("MirrorGlassL", "MirrorL", "Left Mirror");
            }

            if (rightMirrorRenderer == null)
            {
                rightMirrorRenderer = FindRendererByName("MirrorGlassR", "MirrorR", "Right Mirror");
            }
        }

        private void ResolveServices()
        {
            if (_renderingService != null || LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _renderingService);
        }

        private LwsMirrorQuality ResolveQualityFromRenderingService()
        {
            if (_renderingService != null && _renderingService.TryGetActiveProfile(out LwsRenderingQualityProfile profile) && profile != null)
            {
                return profile.mirrorQuality;
            }

            return dashboardDefinition != null ? dashboardDefinition.DefaultMirrorQuality : currentQuality;
        }

        private void ApplyResolvedQuality()
        {
            ResolveReferences();
            LwsMirrorQualityPreset preset = ResolvePreset(currentQuality);
            ApplyPreset(preset);
            _initialized = true;
        }

        private LwsMirrorQualityPreset ResolvePreset(LwsMirrorQuality quality)
        {
            if (dashboardDefinition != null && dashboardDefinition.TryGetMirrorPreset(quality, out LwsMirrorQualityPreset preset))
            {
                if (_renderingService != null && _renderingService.TryGetActiveProfile(out LwsRenderingQualityProfile profile) && profile != null && quality == profile.mirrorQuality)
                {
                    preset.resolutionPixels = Mathf.Max(128, profile.mirrorResolutionPixels);
                    preset.updateIntervalFrames = Mathf.Max(1, profile.mirrorUpdateIntervalFrames);
                }

                return preset;
            }

            return quality switch
            {
                LwsMirrorQuality.Off => new LwsMirrorQualityPreset { quality = quality, enabled = false },
                LwsMirrorQuality.Low => new LwsMirrorQualityPreset { quality = quality, enabled = true, resolutionPixels = 768, updateIntervalFrames = 3, nearClip = 0.05f, farClip = 120f, fieldOfView = 58f },
                LwsMirrorQuality.Medium => new LwsMirrorQualityPreset { quality = quality, enabled = true, resolutionPixels = 1024, updateIntervalFrames = 2, nearClip = 0.05f, farClip = 160f, fieldOfView = 56f },
                LwsMirrorQuality.Ultra => new LwsMirrorQualityPreset { quality = quality, enabled = true, resolutionPixels = 2048, updateIntervalFrames = 1, nearClip = 0.05f, farClip = 260f, fieldOfView = 55f, shadows = true },
                _ => new LwsMirrorQualityPreset { quality = quality, enabled = true, resolutionPixels = 1536, updateIntervalFrames = 1, nearClip = 0.05f, farClip = 220f, fieldOfView = 55f }
            };
        }

        private void ApplyPreset(LwsMirrorQualityPreset preset)
        {
            bool enabled = preset.enabled && preset.quality != LwsMirrorQuality.Off;
            ApplyCameraPreset(leftMirrorCamera, leftMirrorRenderer, enabled, preset);
            ApplyCameraPreset(rightMirrorCamera, rightMirrorRenderer, enabled, preset);
            ApplyMirrorAdjustments();

            _state = new LwsTruckMirrorRuntimeState
            {
                quality = preset.quality,
                leftActive = leftMirrorCamera != null && leftMirrorCamera.gameObject.activeSelf,
                rightActive = rightMirrorCamera != null && rightMirrorCamera.gameObject.activeSelf,
                resolutionPixels = enabled ? preset.resolutionPixels : 0,
                updateIntervalFrames = enabled ? Mathf.Max(1, preset.updateIntervalFrames) : 0,
                nearClip = preset.nearClip,
                farClip = preset.farClip,
                fieldOfView = preset.fieldOfView,
                activeMirrorCount = (leftMirrorCamera != null && leftMirrorCamera.gameObject.activeSelf ? 1 : 0) +
                                    (rightMirrorCamera != null && rightMirrorCamera.gameObject.activeSelf ? 1 : 0),
                notes = preset.notes
            };
        }

        private void ApplyCameraPreset(Camera mirrorCamera, Renderer mirrorRenderer, bool enabled, LwsMirrorQualityPreset preset)
        {
            if (mirrorCamera == null)
            {
                return;
            }

            mirrorCamera.gameObject.SetActive(enabled);
            mirrorCamera.enabled = enabled && preset.updateIntervalFrames <= 1;
            if (!enabled)
            {
                return;
            }

            mirrorCamera.nearClipPlane = Mathf.Max(0.01f, preset.nearClip);
            mirrorCamera.farClipPlane = Mathf.Max(mirrorCamera.nearClipPlane + 1f, preset.farClip);
            mirrorCamera.fieldOfView = Mathf.Clamp(preset.fieldOfView, 30f, 90f);
            mirrorCamera.allowHDR = false;
            mirrorCamera.allowMSAA = false;

            int cullingMask = mirrorCamera.cullingMask;
            if (excludeUiLayer)
            {
                int uiLayer = LayerMask.NameToLayer("UI");
                if (uiLayer >= 0)
                {
                    cullingMask &= ~(1 << uiLayer);
                }
            }

            mirrorCamera.cullingMask = cullingMask;
            RenderTexture texture = EnsureRuntimeTexture(mirrorCamera, preset.resolutionPixels);
            mirrorCamera.targetTexture = texture;
            ApplyTextureToRenderer(mirrorRenderer, texture);
        }

        private RenderTexture EnsureRuntimeTexture(Camera mirrorCamera, int resolution)
        {
            resolution = Mathf.Clamp(resolution, 128, 4096);
            if (_runtimeTextures.TryGetValue(mirrorCamera, out RenderTexture existing) &&
                existing != null &&
                existing.width == resolution &&
                existing.height == resolution)
            {
                return existing;
            }

            if (!_originalTextures.ContainsKey(mirrorCamera))
            {
                _originalTextures.Add(mirrorCamera, mirrorCamera.targetTexture);
            }

            if (existing != null)
            {
                existing.Release();
                DestroyTexture(existing);
            }

            var texture = new RenderTexture(resolution, resolution, 16, RenderTextureFormat.ARGB32)
            {
                name = $"{mirrorCamera.name}_{resolution}_LWS_RT",
                hideFlags = HideFlags.DontSave,
                antiAliasing = 1,
                useMipMap = false,
                autoGenerateMips = false
            };
            texture.Create();
            _runtimeTextures[mirrorCamera] = texture;
            return texture;
        }

        private void ApplyTextureToRenderer(Renderer mirrorRenderer, Texture texture)
        {
            if (mirrorRenderer == null || texture == null)
            {
                return;
            }

            Material material;
            if (!_runtimeMaterials.TryGetValue(mirrorRenderer, out material) || material == null)
            {
                material = mirrorRenderer.material;
                _runtimeMaterials[mirrorRenderer] = material;
            }

            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }
        }

        private void RenderManualMirrorsIfNeeded()
        {
            if (!_initialized || _state.updateIntervalFrames <= 1 || _state.quality == LwsMirrorQuality.Off)
            {
                return;
            }

            if (_lastManualRenderFrame >= 0 && Time.frameCount - _lastManualRenderFrame < _state.updateIntervalFrames)
            {
                return;
            }

            RenderMirror(leftMirrorCamera);
            RenderMirror(rightMirrorCamera);
            _lastManualRenderFrame = Time.frameCount;
        }

        private static void RenderMirror(Camera mirrorCamera)
        {
            if (mirrorCamera == null || !mirrorCamera.gameObject.activeInHierarchy)
            {
                return;
            }

            mirrorCamera.Render();
        }

        private void CaptureBaseRotations()
        {
            CaptureBaseRotation(leftMirrorCamera);
            CaptureBaseRotation(rightMirrorCamera);
        }

        private void CaptureBaseRotation(Camera mirrorCamera)
        {
            if (mirrorCamera != null && !_baseRotations.ContainsKey(mirrorCamera))
            {
                _baseRotations.Add(mirrorCamera, mirrorCamera.transform.localRotation);
            }
        }

        private void ApplyMirrorAdjustments()
        {
            ApplyMirrorAdjustment(leftMirrorCamera, leftAdjustmentDegrees);
            ApplyMirrorAdjustment(rightMirrorCamera, rightAdjustmentDegrees);
        }

        private void ApplyMirrorAdjustment(Camera mirrorCamera, Vector2 adjustment)
        {
            if (mirrorCamera == null)
            {
                return;
            }

            if (!_baseRotations.TryGetValue(mirrorCamera, out Quaternion baseRotation))
            {
                baseRotation = mirrorCamera.transform.localRotation;
                _baseRotations[mirrorCamera] = baseRotation;
            }

            mirrorCamera.transform.localRotation = baseRotation * Quaternion.Euler(adjustment.y, adjustment.x, 0f);
        }

        private Camera FindCameraByName(params string[] names)
        {
            Camera[] cameras = GetComponentsInChildren<Camera>(true);
            foreach (string candidate in names)
            {
                for (int i = 0; i < cameras.Length; i++)
                {
                    if (cameras[i].name.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return cameras[i];
                    }
                }
            }

            return null;
        }

        private Renderer FindRendererByName(params string[] names)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (string candidate in names)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i].name.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return renderers[i];
                    }
                }
            }

            return null;
        }

        private void RestoreOriginalTextures()
        {
            foreach (KeyValuePair<Camera, RenderTexture> pair in _originalTextures)
            {
                if (pair.Key != null)
                {
                    pair.Key.targetTexture = pair.Value;
                }
            }
        }

        private void ReleaseRuntimeTextures()
        {
            foreach (RenderTexture texture in _runtimeTextures.Values)
            {
                if (texture == null)
                {
                    continue;
                }

                texture.Release();
                DestroyTexture(texture);
            }

            _runtimeTextures.Clear();
        }

        private static void DestroyTexture(RenderTexture texture)
        {
            if (texture == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(texture);
            }
            else
            {
                DestroyImmediate(texture);
            }
        }
    }
}
