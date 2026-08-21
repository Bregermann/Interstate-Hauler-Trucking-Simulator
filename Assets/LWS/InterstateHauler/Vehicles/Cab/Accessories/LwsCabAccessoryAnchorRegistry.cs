using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsCabAccessoryAnchorRegistry : MonoBehaviour
    {
        [SerializeField] private bool createDefaultAnchors = true;
        [SerializeField] private bool attachDevelopmentHulaPlaceholder = true;
        [SerializeField] private string anchorRootName = "IH_CabAccessoryAnchors";
        [SerializeField] private List<LwsCabAccessoryAnchorDefinition> defaultAnchors = new List<LwsCabAccessoryAnchorDefinition>
        {
            new LwsCabAccessoryAnchorDefinition { anchorId = "IH_CabAnchor_Dashboard01", anchorType = LwsCabAccessoryAnchorType.DashboardAccessory, localPosition = new Vector3(-0.38f, 1.35f, 1.15f), localEulerAngles = new Vector3(0f, 0f, 0f), localScale = Vector3.one, expectedContent = "hula girl / bobblehead", notes = "Primary left dashboard accessory location." },
            new LwsCabAccessoryAnchorDefinition { anchorId = "IH_CabAnchor_Dashboard02", anchorType = LwsCabAccessoryAnchorType.DashboardAccessory, localPosition = new Vector3(0.34f, 1.35f, 1.15f), localEulerAngles = new Vector3(0f, 0f, 0f), localScale = Vector3.one, expectedContent = "souvenir / mini flag / coffee cup", notes = "Secondary dashboard accessory location." },
            new LwsCabAccessoryAnchorDefinition { anchorId = "IH_CabAnchor_GpsMount", anchorType = LwsCabAccessoryAnchorType.DashboardAccessory, localPosition = new Vector3(0.28f, 1.22f, 1.20f), localEulerAngles = new Vector3(62f, -8f, 0f), localScale = Vector3.one, expectedContent = "world-space cab GPS", notes = "Center-right dashboard GPS mount for cockpit navigation." },
            new LwsCabAccessoryAnchorDefinition { anchorId = "IH_CabAnchor_Hanging01", anchorType = LwsCabAccessoryAnchorType.HangingAccessory, localPosition = new Vector3(0f, 1.82f, 1.02f), localEulerAngles = new Vector3(0f, 0f, 0f), localScale = Vector3.one, expectedContent = "hanging dice / air freshener", notes = "Windshield hanging accessory location." },
            new LwsCabAccessoryAnchorDefinition { anchorId = "IH_CabAnchor_PassengerSeat", anchorType = LwsCabAccessoryAnchorType.PassengerSeat, localPosition = new Vector3(0.82f, 0.82f, -0.2f), localEulerAngles = new Vector3(0f, -12f, 0f), localScale = Vector3.one, expectedContent = "future dog companion / bag", notes = "Passenger seat placement." },
            new LwsCabAccessoryAnchorDefinition { anchorId = "IH_CabAnchor_Sleeper", anchorType = LwsCabAccessoryAnchorType.Sleeper, localPosition = new Vector3(0f, 0.72f, -1.35f), localEulerAngles = new Vector3(0f, 180f, 0f), localScale = Vector3.one, expectedContent = "future dog or cat companion / bedding", notes = "Sleeper placement." },
            new LwsCabAccessoryAnchorDefinition { anchorId = "IH_CabAnchor_Memento01", anchorType = LwsCabAccessoryAnchorType.PersonalMemento, localPosition = new Vector3(-0.74f, 1.48f, 0.86f), localEulerAngles = new Vector3(0f, 18f, 0f), localScale = Vector3.one, expectedContent = "photo / postcard / family drawing", notes = "Flat memento surface." }
        };

        private readonly List<LwsCabAccessoryAnchor> _anchors = new List<LwsCabAccessoryAnchor>();
        private Transform _anchorRoot;
        private GameObject _hulaPlaceholder;
        private bool _initialized;
        private bool _initializing;

        public IReadOnlyList<LwsCabAccessoryAnchor> Anchors => _anchors;
        public bool HulaPlaceholderAttached => _hulaPlaceholder != null && _hulaPlaceholder.transform.parent != null;
        public string HulaPlaceholderAnchorId => HulaPlaceholderAttached ? _hulaPlaceholder.transform.parent.GetComponent<LwsCabAccessoryAnchor>()?.AnchorId : string.Empty;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnValidate()
        {
            for (int i = 0; i < defaultAnchors.Count; i++)
            {
                LwsCabAccessoryAnchorDefinition anchor = defaultAnchors[i];
                if (anchor.localScale == Vector3.zero)
                {
                    anchor.localScale = Vector3.one;
                    defaultAnchors[i] = anchor;
                }
            }
        }

        public void EnsureInitialized()
        {
            if (_initialized || _initializing)
            {
                return;
            }

            _initializing = true;
            RefreshAnchors();
            if (createDefaultAnchors)
            {
                EnsureDefaultAnchors();
            }

            if (attachDevelopmentHulaPlaceholder)
            {
                EnsureDevelopmentHulaPlaceholder();
            }

            _initialized = true;
            _initializing = false;
        }

        public bool TryGetAnchor(string anchorId, out LwsCabAccessoryAnchor anchor)
        {
            if (!_initialized && !_initializing)
            {
                EnsureInitialized();
            }

            anchor = _anchors.FirstOrDefault(a => a != null && a.AnchorId == anchorId);
            return anchor != null;
        }

        public int CountByType(LwsCabAccessoryAnchorType type)
        {
            if (!_initialized && !_initializing)
            {
                EnsureInitialized();
            }

            return _anchors.Count(a => a != null && a.AnchorType == type);
        }

        public bool Validate(out string message)
        {
            EnsureInitialized();
            var ids = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (LwsCabAccessoryAnchor anchor in _anchors)
            {
                if (anchor == null)
                {
                    message = "Cab accessory registry contains a null anchor.";
                    return false;
                }

                if (!ids.Add(anchor.AnchorId))
                {
                    message = $"Duplicate cab accessory anchor ID: {anchor.AnchorId}";
                    return false;
                }

                if (!anchor.Validate(out message))
                {
                    return false;
                }
            }

            LwsCabAccessoryAnchorType[] required =
            {
                LwsCabAccessoryAnchorType.DashboardAccessory,
                LwsCabAccessoryAnchorType.HangingAccessory,
                LwsCabAccessoryAnchorType.PassengerSeat,
                LwsCabAccessoryAnchorType.Sleeper,
                LwsCabAccessoryAnchorType.PersonalMemento
            };

            foreach (LwsCabAccessoryAnchorType type in required)
            {
                if (_anchors.All(a => a.AnchorType != type))
                {
                    message = $"Missing required cab accessory anchor category: {type}";
                    return false;
                }
            }

            message = "Cab accessory anchor registry is valid.";
            return true;
        }

        private void EnsureDefaultAnchors()
        {
            Transform cabRoot = FindCabRoot();
            _anchorRoot = FindChild(cabRoot, anchorRootName);
            if (_anchorRoot == null)
            {
                var rootGo = new GameObject(anchorRootName);
                _anchorRoot = rootGo.transform;
                _anchorRoot.SetParent(cabRoot, false);
                _anchorRoot.localPosition = Vector3.zero;
                _anchorRoot.localRotation = Quaternion.identity;
                _anchorRoot.localScale = Vector3.one;
            }

            foreach (LwsCabAccessoryAnchorDefinition definition in defaultAnchors)
            {
                if (!definition.Validate(out _))
                {
                    continue;
                }

                Transform anchorTransform = FindChild(_anchorRoot, definition.anchorId);
                if (anchorTransform == null)
                {
                    var anchorGo = new GameObject(definition.anchorId);
                    anchorTransform = anchorGo.transform;
                    anchorTransform.SetParent(_anchorRoot, false);
                }

                anchorTransform.localPosition = definition.localPosition;
                anchorTransform.localRotation = Quaternion.Euler(definition.localEulerAngles);
                anchorTransform.localScale = definition.localScale;

                LwsCabAccessoryAnchor anchor = anchorTransform.GetComponent<LwsCabAccessoryAnchor>();
                if (anchor == null)
                {
                    anchor = anchorTransform.gameObject.AddComponent<LwsCabAccessoryAnchor>();
                }

                anchor.Configure(definition.anchorId, definition.anchorType, definition.expectedContent);
            }

            RefreshAnchors();
        }

        private void EnsureDevelopmentHulaPlaceholder()
        {
            if (!TryGetAnchor("IH_CabAnchor_Dashboard01", out LwsCabAccessoryAnchor anchor))
            {
                return;
            }

            if (anchor.Occupied)
            {
                _hulaPlaceholder = anchor.AttachedAccessory;
                return;
            }

            _hulaPlaceholder = new GameObject("IH_DevHulaGirl_Placeholder");
            TextMesh label = _hulaPlaceholder.AddComponent<TextMesh>();
            label.text = "HULA";
            label.characterSize = 0.06f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(0.25f, 0.95f, 0.7f);
            _hulaPlaceholder.AddComponent<LwsCabAccessoryBobble>();
            _hulaPlaceholder.transform.localScale = Vector3.one;

            LwsCabAccessoryAttachmentResult result = anchor.Attach(_hulaPlaceholder);
            if (!result.Succeeded)
            {
                Debug.LogWarning(result.Message, this);
            }
        }

        private void RefreshAnchors()
        {
            _anchors.Clear();
            GetComponentsInChildren(true, _anchors);
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

        private static Transform FindChild(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
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
                if (child.name == childName)
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
    }
}
