using System;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DefaultExecutionOrder(188)]
    [DisallowMultipleComponent]
    public sealed class LwsTruckCabInteriorRecovery : MonoBehaviour
    {
        public const string SleeperPlaceholderRootName = "IH_SleeperInterior_Placeholder";
        public const string DashboardLightName = "IH_CabInterior_DashboardLight";
        public const string SleeperLightName = "IH_CabInterior_SleeperDomeLight";

        [SerializeField] private bool applyOnStart = true;
        [SerializeField] private bool hideLegacyTextPlaceholder = true;
        [SerializeField] private bool createDashDecorationPlaceholder = true;
        [SerializeField] private bool createSleeperPlaceholders = true;
        [SerializeField] private bool createInteriorLightsIfMissing = true;
        [SerializeField] private Vector3 dashDecorationScale = new Vector3(0.14f, 0.045f, 0.08f);
        [SerializeField] private Color dashDecorationColor = new Color(0.11f, 0.55f, 0.62f, 1f);
        [SerializeField] private Color sleeperMattressColor = new Color(0.11f, 0.16f, 0.21f, 1f);
        [SerializeField] private Color sleeperCabinetColor = new Color(0.13f, 0.11f, 0.09f, 1f);
        [SerializeField] private Color beddingAccentColor = new Color(0.56f, 0.72f, 0.78f, 1f);
        [SerializeField] private float dashboardLightIntensity = 0.45f;
        [SerializeField] private float sleeperLightIntensity = 0.28f;

        private LwsCabAccessoryAnchorRegistry _anchorRegistry;
        private bool _applied;

        public bool GpsMountReady { get; private set; }
        public bool DashDecorationReady { get; private set; }
        public bool SleeperInteriorReady { get; private set; }
        public bool InteriorLightsReady { get; private set; }
        public int LegacyTextPlaceholdersHidden { get; private set; }

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            if (applyOnStart)
            {
                ApplyRecovery();
            }
        }

        public void ApplyRecovery()
        {
            ResolveReferences();
            _anchorRegistry?.EnsureInitialized();

            if (hideLegacyTextPlaceholder)
            {
                LegacyTextPlaceholdersHidden = HideLegacyTextPlaceholders();
            }

            GpsMountReady = _anchorRegistry != null && _anchorRegistry.TryGetAnchor(LwsCabAccessoryAnchorRegistry.GpsMountAnchorId, out _);

            if (createDashDecorationPlaceholder)
            {
                DashDecorationReady = EnsureDashDecorationPlaceholder();
            }

            if (createSleeperPlaceholders)
            {
                SleeperInteriorReady = EnsureSleeperInteriorPlaceholder();
            }

            if (createInteriorLightsIfMissing)
            {
                InteriorLightsReady = EnsureInteriorLights();
            }

            _applied = true;
        }

        public bool Validate(out string message)
        {
            if (!_applied)
            {
                ApplyRecovery();
            }

            if (!GpsMountReady)
            {
                message = "Cab GPS mount anchor is missing.";
                return false;
            }

            if (createDashDecorationPlaceholder && !DashDecorationReady)
            {
                message = "Dashboard decoration placeholder is missing.";
                return false;
            }

            if (createSleeperPlaceholders && !SleeperInteriorReady)
            {
                message = "Sleeper interior placeholder is missing.";
                return false;
            }

            message = "Cab interior recovery is valid.";
            return true;
        }

        private void ResolveReferences()
        {
            if (_anchorRegistry == null)
            {
                _anchorRegistry = GetComponent<LwsCabAccessoryAnchorRegistry>();
            }
        }

        private bool EnsureDashDecorationPlaceholder()
        {
            if (_anchorRegistry == null || !_anchorRegistry.TryGetAnchor(LwsCabAccessoryAnchorRegistry.DashDecorationAnchorId, out LwsCabAccessoryAnchor anchor))
            {
                return false;
            }

            if (anchor.Occupied)
            {
                if (IsLegacyTextPlaceholder(anchor.AttachedAccessory))
                {
                    GameObject detached = anchor.Detach();
                    if (detached != null)
                    {
                        detached.SetActive(false);
                    }
                }
                else
                {
                    return anchor.AttachedAccessory != null;
                }
            }

            GameObject decoration = GameObject.CreatePrimitive(PrimitiveType.Cube);
            decoration.name = LwsCabAccessoryAnchorRegistry.DashDecorationPlaceholderName;
            decoration.transform.localScale = dashDecorationScale;
            DisableOrDestroyCollider(decoration.GetComponent<Collider>());
            ApplyMaterial(decoration.GetComponent<Renderer>(), dashDecorationColor);
            decoration.AddComponent<LwsCabAccessoryBobble>();

            LwsCabAccessoryAttachmentResult result = anchor.Attach(decoration);
            if (!result.Succeeded)
            {
                Debug.LogWarning(result.Message, this);
            }

            return result.Succeeded;
        }

        private bool EnsureSleeperInteriorPlaceholder()
        {
            if (_anchorRegistry == null || !_anchorRegistry.TryGetAnchor("IH_CabAnchor_Sleeper", out LwsCabAccessoryAnchor anchor))
            {
                return false;
            }

            if (anchor.Occupied && anchor.AttachedAccessory != null && !IsLegacyTextPlaceholder(anchor.AttachedAccessory))
            {
                return true;
            }

            if (anchor.Occupied)
            {
                GameObject detached = anchor.Detach();
                if (detached != null)
                {
                    detached.SetActive(false);
                }
            }

            GameObject sleeperRoot = new GameObject(SleeperPlaceholderRootName);
            sleeperRoot.transform.localScale = Vector3.one;

            CreateCube("IH_Sleeper_BunkBase", sleeperRoot.transform, new Vector3(0f, 0f, 0f), new Vector3(1.45f, 0.12f, 0.56f), sleeperCabinetColor);
            CreateCube("IH_Sleeper_Mattress", sleeperRoot.transform, new Vector3(0f, 0.09f, 0f), new Vector3(1.36f, 0.08f, 0.5f), sleeperMattressColor);
            CreateCube("IH_Sleeper_Pillow", sleeperRoot.transform, new Vector3(-0.48f, 0.17f, 0f), new Vector3(0.28f, 0.06f, 0.42f), beddingAccentColor);
            CreateCube("IH_Sleeper_BackCabinet", sleeperRoot.transform, new Vector3(0f, 0.46f, 0.34f), new Vector3(1.3f, 0.36f, 0.12f), sleeperCabinetColor);
            CreateCube("IH_Sleeper_OverheadStorage", sleeperRoot.transform, new Vector3(0f, 0.78f, 0.12f), new Vector3(1.34f, 0.18f, 0.22f), sleeperCabinetColor);
            CreateCube("IH_Sleeper_SideStorage", sleeperRoot.transform, new Vector3(0.78f, 0.28f, -0.04f), new Vector3(0.18f, 0.42f, 0.46f), sleeperCabinetColor);

            LwsCabAccessoryAttachmentResult result = anchor.Attach(sleeperRoot);
            if (!result.Succeeded)
            {
                Debug.LogWarning(result.Message, this);
            }

            return result.Succeeded;
        }

        private bool EnsureInteriorLights()
        {
            Transform cabRoot = FindCabRoot();
            if (cabRoot == null)
            {
                return false;
            }

            Light[] lights = cabRoot.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] == null)
                {
                    continue;
                }

                string lightName = lights[i].name;
                if (lightName.IndexOf("Cab", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    lightName.IndexOf("Interior", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    lightName.IndexOf("Dome", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    lightName.IndexOf("Dashboard", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            CreatePointLight(cabRoot, DashboardLightName, new Vector3(0f, 1.35f, 0.92f), new Color(0.48f, 0.78f, 0.9f, 1f), dashboardLightIntensity, 1.2f);
            CreatePointLight(cabRoot, SleeperLightName, new Vector3(0f, 1.25f, -1.1f), new Color(1f, 0.78f, 0.48f, 1f), sleeperLightIntensity, 1.5f);
            return true;
        }

        private int HideLegacyTextPlaceholders()
        {
            int hidden = 0;
            TextMesh[] labels = GetComponentsInChildren<TextMesh>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                TextMesh label = labels[i];
                if (label != null && IsLegacyTextPlaceholder(label.gameObject))
                {
                    label.gameObject.SetActive(false);
                    hidden++;
                }
            }

            Transform[] transforms = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate != null && candidate.gameObject.activeSelf && IsLegacyTextPlaceholder(candidate.gameObject))
                {
                    candidate.gameObject.SetActive(false);
                    hidden++;
                }
            }

            return hidden;
        }

        private bool IsLegacyTextPlaceholder(GameObject candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            if (candidate.name.IndexOf("Hula", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            TextMesh text = candidate.GetComponent<TextMesh>();
            return text != null && string.Equals(text.text, "HULA", StringComparison.OrdinalIgnoreCase);
        }

        private Transform FindCabRoot()
        {
            Transform cab = FindChildRecursive(transform, "Cab");
            if (cab != null)
            {
                return cab;
            }

            Transform interior = FindChildRecursive(transform, "interior");
            return interior != null ? interior : transform;
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localRotation = Quaternion.identity;
            cube.transform.localScale = localScale;
            DisableOrDestroyCollider(cube.GetComponent<Collider>());
            ApplyMaterial(cube.GetComponent<Renderer>(), color);
            return cube;
        }

        private static void CreatePointLight(Transform parent, string name, Vector3 localPosition, Color color, float intensity, float range)
        {
            Transform existing = FindChildRecursive(parent, name);
            if (existing != null)
            {
                return;
            }

            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = localPosition;
            lightObject.transform.localRotation = Quaternion.identity;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = Mathf.Max(0f, intensity);
            light.range = Mathf.Max(0.1f, range);
            light.shadows = LightShadows.None;
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name.Equals(childName, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }

                Transform nested = FindChildRecursive(child, childName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static void ApplyMaterial(Renderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");
            if (shader == null)
            {
                return;
            }

            Material material = new Material(shader)
            {
                color = color,
                hideFlags = HideFlags.DontSave
            };
            renderer.sharedMaterial = material;
        }

        private static void DisableOrDestroyCollider(Collider collider)
        {
            if (collider == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                collider.enabled = false;
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }
    }
}
