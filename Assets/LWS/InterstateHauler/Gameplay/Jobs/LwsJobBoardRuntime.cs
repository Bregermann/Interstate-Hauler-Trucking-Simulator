using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LWS.InterstateHauler
{
    [DefaultExecutionOrder(250)]
    [DisallowMultipleComponent]
    public sealed class LwsJobBoardPresenter : MonoBehaviour
    {
        private static readonly Color BackdropColor = new Color(0.015f, 0.018f, 0.02f, 0.88f);
        private static readonly Color PanelColor = new Color(0.08f, 0.09f, 0.10f, 0.96f);
        private static readonly Color HeaderColor = new Color(0.12f, 0.14f, 0.15f, 1f);
        private static readonly Color ButtonColor = new Color(0.18f, 0.22f, 0.24f, 1f);
        private static readonly Color ButtonSelectedColor = new Color(0.22f, 0.36f, 0.42f, 1f);
        private static readonly Color TextColor = new Color(0.93f, 0.95f, 0.92f, 1f);
        private static readonly Color MutedTextColor = new Color(0.70f, 0.74f, 0.72f, 1f);

        private readonly List<GameObject> _offerRows = new List<GameObject>();
        private ILwsJobBoardService _jobBoardService;
        private ILwsDepotService _depotService;
        private ILwsVehicleInputService _inputService;
        private Canvas _canvas;
        private RectTransform _root;
        private RectTransform _offerList;
        private RectTransform _detailsPanel;
        private Text _titleText;
        private Text _detailsText;
        private Text _statusText;
        private Button _acceptButton;
        private Action<IReadOnlyList<LwsJobOffer>> _offersChangedHandler;
        private Action<LwsActiveJob> _jobAcceptedHandler;
        private bool _subscribed;

        public static LwsJobBoardPresenter EnsureScenePresenter()
        {
            LwsJobBoardPresenter existing = FindFirstObjectByType<LwsJobBoardPresenter>();
            if (existing != null)
            {
                return existing;
            }

            GameObject go = new GameObject("IH Job Board Presenter");
            return go.AddComponent<LwsJobBoardPresenter>();
        }

        private void Awake()
        {
            BuildUi();
        }

        private void Start()
        {
            ResolveServices();
            RefreshUi();
        }

        private void Update()
        {
            ResolveServices();
            if (_jobBoardService == null)
            {
                return;
            }

            if (_root != null && _root.gameObject.activeSelf != _jobBoardService.IsOpen)
            {
                _root.gameObject.SetActive(_jobBoardService.IsOpen);
                RefreshUi();
            }

            if (_jobBoardService.IsOpen && _inputService != null)
            {
                LwsVehicleCommandFrame commands = _inputService.ReadCommandFrame();
                if (LwsVehicleCommandFrameUtility.IsPressed(commands.menuCancel))
                {
                    _jobBoardService.CloseJobBoard("Job board canceled.", false);
                }
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void ResolveServices()
        {
            if (LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _depotService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _inputService);
            if (_jobBoardService == null && LwsApplicationBootstrap.Instance.Registry.TryGet(out _jobBoardService))
            {
                Subscribe();
            }
        }

        private void Subscribe()
        {
            if (_subscribed || _jobBoardService == null)
            {
                return;
            }

            _offersChangedHandler = _ => RefreshUi();
            _jobAcceptedHandler = _ => RefreshUi();
            _jobBoardService.JobBoardOpened += RefreshUi;
            _jobBoardService.JobBoardClosed += RefreshUi;
            _jobBoardService.OffersChanged += _offersChangedHandler;
            _jobBoardService.JobAccepted += _jobAcceptedHandler;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _jobBoardService == null)
            {
                return;
            }

            _jobBoardService.JobBoardOpened -= RefreshUi;
            _jobBoardService.JobBoardClosed -= RefreshUi;
            if (_offersChangedHandler != null)
            {
                _jobBoardService.OffersChanged -= _offersChangedHandler;
            }

            if (_jobAcceptedHandler != null)
            {
                _jobBoardService.JobAccepted -= _jobAcceptedHandler;
            }

            _offersChangedHandler = null;
            _jobAcceptedHandler = null;
            _subscribed = false;
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject("IH Job Board Canvas");
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 820;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            _root = CreateRect(canvasObject.transform, "Job Board Root", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image backdrop = _root.gameObject.AddComponent<Image>();
            backdrop.color = BackdropColor;

            RectTransform window = CreateRect(_root, "Window", new Vector2(0.12f, 0.10f), new Vector2(0.88f, 0.92f), Vector2.zero, Vector2.zero);
            Image windowImage = window.gameObject.AddComponent<Image>();
            windowImage.color = PanelColor;

            RectTransform header = CreateRect(window, "Header", new Vector2(0f, 0.86f), Vector2.one, Vector2.zero, Vector2.zero);
            Image headerImage = header.gameObject.AddComponent<Image>();
            headerImage.color = HeaderColor;
            _titleText = CreateText(header, "Title", "JOB BOARD", Vector2.zero, Vector2.one, new Vector2(24f, 6f), new Vector2(-24f, -6f), 34, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor);

            CreateText(window, "Offer Heading", "AVAILABLE JOBS", new Vector2(0.04f, 0.78f), new Vector2(0.43f, 0.84f), Vector2.zero, Vector2.zero, 22, FontStyle.Bold, TextAnchor.MiddleLeft, MutedTextColor);
            CreateText(window, "Details Heading", "JOB DETAILS", new Vector2(0.48f, 0.78f), new Vector2(0.96f, 0.84f), Vector2.zero, Vector2.zero, 22, FontStyle.Bold, TextAnchor.MiddleLeft, MutedTextColor);

            _offerList = CreateRect(window, "Offer List", new Vector2(0.04f, 0.16f), new Vector2(0.43f, 0.78f), Vector2.zero, Vector2.zero);
            Image listImage = _offerList.gameObject.AddComponent<Image>();
            listImage.color = new Color(0.045f, 0.052f, 0.055f, 0.88f);

            _detailsPanel = CreateRect(window, "Details Panel", new Vector2(0.48f, 0.16f), new Vector2(0.96f, 0.78f), Vector2.zero, Vector2.zero);
            Image detailsImage = _detailsPanel.gameObject.AddComponent<Image>();
            detailsImage.color = new Color(0.045f, 0.052f, 0.055f, 0.88f);
            _detailsText = CreateText(_detailsPanel, "Details", "No job selected.", Vector2.zero, Vector2.one, new Vector2(22f, 18f), new Vector2(-22f, -18f), 21, FontStyle.Normal, TextAnchor.UpperLeft, TextColor);
            _detailsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _detailsText.verticalOverflow = VerticalWrapMode.Truncate;

            _statusText = CreateText(window, "Status", "", new Vector2(0.04f, 0.08f), new Vector2(0.60f, 0.14f), Vector2.zero, Vector2.zero, 18, FontStyle.Normal, TextAnchor.MiddleLeft, MutedTextColor);
            _acceptButton = CreateButton(window, "Accept Job", "ACCEPT JOB", new Vector2(0.65f, 0.06f), new Vector2(0.80f, 0.14f), () => _jobBoardService?.AcceptSelectedOffer());
            CreateButton(window, "Close", "CLOSE", new Vector2(0.82f, 0.06f), new Vector2(0.96f, 0.14f), () => _jobBoardService?.CloseJobBoard("Job board closed.", false));
            _root.gameObject.SetActive(false);
        }

        private void RefreshUi()
        {
            if (_root == null)
            {
                return;
            }

            bool open = _jobBoardService != null && _jobBoardService.IsOpen;
            _root.gameObject.SetActive(open);
            if (!open)
            {
                return;
            }

            string depot = _depotService != null && _depotService.CurrentDepotDefinition != null
                ? _depotService.CurrentDepotDefinition.DisplayName
                : "CURRENT DEPOT";
            _titleText.text = $"{depot}\nJOB BOARD";
            BuildOfferRows();
            RenderDetails();
            _statusText.text = _jobBoardService.LastMessage;
            if (_acceptButton != null)
            {
                _acceptButton.interactable = _jobBoardService.SelectedOffer != null;
            }
        }

        private void BuildOfferRows()
        {
            foreach (GameObject row in _offerRows)
            {
                Destroy(row);
            }

            _offerRows.Clear();
            IReadOnlyList<LwsJobOffer> offers = _jobBoardService.CurrentOffers;
            if (offers.Count == 0)
            {
                RectTransform empty = CreateRect(_offerList, "No Offers", Vector2.zero, Vector2.one, new Vector2(18f, 18f), new Vector2(-18f, -18f));
                CreateText(empty, "Text", "NO AUTHORED JOBS AVAILABLE", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 22, FontStyle.Bold, TextAnchor.MiddleCenter, MutedTextColor);
                _offerRows.Add(empty.gameObject);
                return;
            }

            float rowHeight = Mathf.Min(0.26f, 0.96f / Mathf.Max(1, offers.Count));
            for (int i = 0; i < offers.Count; i++)
            {
                LwsJobOffer offer = offers[i];
                float top = 0.98f - i * rowHeight;
                float bottom = Mathf.Max(0.02f, top - rowHeight + 0.02f);
                string label = $"{offer.cargoDisplayName}\n{offer.destinationDisplayName}\n{offer.DistanceDisplay}    {offer.QuotedGrossDisplay}";
                string selectedId = _jobBoardService.SelectedOffer != null ? _jobBoardService.SelectedOffer.stableOfferId : string.Empty;
                bool selected = string.Equals(selectedId, offer.stableOfferId, StringComparison.Ordinal);
                string id = offer.stableOfferId;
                Button button = CreateButton(_offerList, offer.jobDefinitionId, label, new Vector2(0.03f, bottom), new Vector2(0.97f, top), () =>
                {
                    _jobBoardService.SelectOffer(id);
                    RefreshUi();
                });
                SetButtonColor(button, selected ? ButtonSelectedColor : ButtonColor);
                _offerRows.Add(button.gameObject);
            }
        }

        private void RenderDetails()
        {
            LwsJobOffer offer = _jobBoardService.SelectedOffer;
            if (offer == null)
            {
                _detailsText.text = "Select an authored job offer.";
                return;
            }

            _detailsText.text =
                $"{offer.cargoDisplayName}\n\n" +
                $"Origin:\n{offer.originDepotDisplayName}\n\n" +
                $"Destination:\n{offer.destinationDisplayName}\n\n" +
                $"Distance:\n{offer.DistanceDisplay}\n\n" +
                $"Trailer:\n{offer.requiredTrailerTypeId}\n\n" +
                $"Weight:\n{offer.WeightDisplay}\n\n" +
                $"Quoted Gross:\n{offer.QuotedGrossDisplay}\n\n" +
                $"Flavor:\n{offer.flavorText}";
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        private static Text CreateText(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, int fontSize, FontStyle style, TextAnchor alignment, Color color)
        {
            RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            Text label = rect.gameObject.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Action onClick)
        {
            RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = ButtonColor;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick?.Invoke());
            CreateText(rect, "Label", label, Vector2.zero, Vector2.one, new Vector2(12f, 6f), new Vector2(-12f, -6f), 19, FontStyle.Bold, TextAnchor.MiddleLeft, TextColor);
            return button;
        }

        private static void SetButtonColor(Button button, Color color)
        {
            if (button == null || button.targetGraphic == null)
            {
                return;
            }

            button.targetGraphic.color = color;
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = color * 1.12f;
            colors.pressedColor = color * 0.82f;
            colors.selectedColor = color;
            button.colors = colors;
        }

        private static void EnsureEventSystem()
        {
            EventSystem[] systems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            if (systems.Length > 0)
            {
                return;
            }

            GameObject eventSystem = new GameObject("IH Job Board EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }

    [DefaultExecutionOrder(180)]
    [DisallowMultipleComponent]
    public sealed class LwsJobBoardTerminal : MonoBehaviour
    {
        private const float MetersPerMph = 0.44704f;

        [SerializeField] private LwsDepotRuntime depotRuntime;
        [SerializeField] private Transform interactionPoint;
        [SerializeField] private float interactionRadiusMeters = 8f;
        [SerializeField] private float stationaryThresholdMph = 1f;

        private ILwsDepotService _depotService;
        private ILwsJobBoardService _jobBoardService;
        private ILwsActiveJobService _activeJobService;
        private ILwsVehicleInputService _inputService;
        private ILwsVehicleRuntimeService _vehicleRuntimeService;
        private ILwsPlayerVehicleService _playerVehicleService;
        private Canvas _promptCanvas;
        private Text _promptText;

        public float StationaryThresholdMph => stationaryThresholdMph;

        public void Configure(LwsDepotRuntime runtime, Transform point, float radiusMeters, float stopThresholdMph)
        {
            depotRuntime = runtime;
            interactionPoint = point;
            interactionRadiusMeters = Mathf.Max(0.5f, radiusMeters);
            stationaryThresholdMph = Mathf.Max(0f, stopThresholdMph);
        }

        private void Awake()
        {
            if (interactionPoint == null)
            {
                interactionPoint = transform;
            }

            BuildPrompt();
        }

        private void Update()
        {
            ResolveServices();
            bool eligible = ResolveEligibility(out string prompt);
            SetPromptVisible(eligible || !string.IsNullOrWhiteSpace(prompt), prompt);

            if (!eligible || _inputService == null)
            {
                return;
            }

            LwsVehicleCommandFrame commands = _inputService.ReadCommandFrame();
            if (LwsVehicleCommandFrameUtility.IsPressed(commands.interact) || LwsVehicleCommandFrameUtility.IsPressed(commands.menuSubmit))
            {
                _jobBoardService?.OpenJobBoard("Player interacted with depot job board terminal.");
            }
        }

        private bool ResolveEligibility(out string prompt)
        {
            prompt = string.Empty;
            if (_jobBoardService != null && _jobBoardService.IsOpen)
            {
                return false;
            }

            if (_activeJobService != null && _activeJobService.HasActiveJob)
            {
                return false;
            }

            if (_depotService == null || !_depotService.IsPlayerAtDepot || depotRuntime == null || _depotService.CurrentDepotRuntime != depotRuntime)
            {
                return false;
            }

            LwsServiceResult canOpen = _jobBoardService != null ? _jobBoardService.CanOpenCurrentDepot() : LwsServiceResult.Failure("Job board service missing.");
            if (!canOpen.Succeeded)
            {
                return false;
            }

            LwsPlayerTruck truck = _playerVehicleService != null ? _playerVehicleService.ActiveTruck : null;
            if (truck == null)
            {
                return false;
            }

            float distance = Vector3.Distance(truck.transform.position, interactionPoint != null ? interactionPoint.position : transform.position);
            if (distance > interactionRadiusMeters)
            {
                return false;
            }

            float speedMps = _vehicleRuntimeService != null ? Mathf.Abs(_vehicleRuntimeService.LastTelemetry.speedMetersPerSecond) : 0f;
            if (speedMps > stationaryThresholdMph * MetersPerMph)
            {
                prompt = "JOB BOARD AVAILABLE\nSTOP TO OPEN";
                return false;
            }

            prompt = "JOB BOARD AVAILABLE\n[ENTER] OPEN JOB BOARD";
            return true;
        }

        private void BuildPrompt()
        {
            GameObject canvasObject = new GameObject("IH Job Board Prompt Canvas");
            canvasObject.transform.SetParent(transform, false);
            _promptCanvas = canvasObject.AddComponent<Canvas>();
            _promptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _promptCanvas.sortingOrder = 810;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();

            RectTransform panel = CreateRect(canvasObject.transform, "Prompt", new Vector2(0.36f, 0.13f), new Vector2(0.64f, 0.25f), Vector2.zero, Vector2.zero);
            Image image = panel.gameObject.AddComponent<Image>();
            image.color = new Color(0.04f, 0.045f, 0.045f, 0.88f);
            _promptText = CreateText(panel, "Text", string.Empty, Vector2.zero, Vector2.one, new Vector2(12f, 8f), new Vector2(-12f, -8f), 22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            canvasObject.SetActive(false);
        }

        private void SetPromptVisible(bool visible, string text)
        {
            if (_promptCanvas == null)
            {
                return;
            }

            _promptCanvas.gameObject.SetActive(visible && !string.IsNullOrWhiteSpace(text));
            if (_promptText != null)
            {
                _promptText.text = text ?? string.Empty;
            }
        }

        private void ResolveServices()
        {
            if (LwsApplicationBootstrap.Instance == null || LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _depotService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _jobBoardService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _activeJobService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _inputService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _vehicleRuntimeService);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out _playerVehicleService);
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        private static Text CreateText(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, int fontSize, FontStyle style, TextAnchor alignment, Color color)
        {
            RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            Text label = rect.gameObject.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }
    }
}
