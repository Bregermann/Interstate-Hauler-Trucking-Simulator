using System.Collections;
using System.Text;
using LWS.InterstateHauler;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadAir
{
    [DefaultExecutionOrder(520)]
    [DisallowMultipleComponent]
    public sealed class DeadAirGPSController : MonoBehaviour
    {
        // ============================================================
        // EDIT DEAD AIR GPS POSITION HERE
        // ============================================================
        // Local values are applied after the GPS is parented to IH_CabAnchor_GpsMount.
        // X moves right/left across the dash, Y moves up/down, Z moves forward/back from the cab anchor.
        private static readonly Vector3 GPS_LOCAL_POSITION = Vector3.zero;
        private static readonly Vector3 GPS_LOCAL_EULER_ANGLES = new Vector3(0f, 180f, 0f);
        private static readonly Vector3 GPS_LOCAL_SCALE = new Vector3(0.00042f, 0.00042f, 0.00042f);

        private const string CabCompassRootName = "IH Cab GPS Compass Navigator Pro";
        private const string NarrativeOverlayName = "DeadAirNarrativeOverlay";
        private static readonly Vector2 DefaultScreenSize = new Vector2(640f, 400f);
        private static readonly char[] CorruptionCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789#?/\\".ToCharArray();

        [SerializeField] private bool autoBindToCabGps = true;
        [SerializeField] private bool createMissingCabGpsInfrastructure = true;
        [SerializeField] private bool applyConfiguredPhysicalPlacement = true;
        [SerializeField] private bool logBindingDiagnostics = true;
        [SerializeField] private float bindRetryIntervalSeconds = 0.25f;
        [SerializeField] private Color narrativeTextColor = new Color(0.82f, 1f, 0.92f, 1f);
        [SerializeField] private Color narrativeWarningColor = new Color(1f, 0.88f, 0.52f, 1f);
        [SerializeField] private Color backplateColor = new Color(0f, 0.018f, 0.016f, 0.62f);
        [SerializeField] private Color flashColor = new Color(0.62f, 1f, 0.9f, 0.88f);
        [SerializeField] private Color glitchColor = new Color(0.1f, 1f, 0.8f, 0.42f);
        [SerializeField, Min(0.02f)] private float flashStepSeconds = 0.055f;
        [SerializeField, Min(1)] private int flashCount = 2;
        [SerializeField, Min(0.02f)] private float defaultGlitchDuration = 0.75f;
        [SerializeField, Range(0f, 1f)] private float defaultGlitchIntensity = 0.75f;
        [SerializeField, Min(0.01f)] private float glitchFlickerIntervalSeconds = 0.055f;
        [SerializeField, Min(0f)] private float glitchMaxPixelOffset = 10f;
        [SerializeField] private AudioSource gpsAudioSource;

        private DeadAirGPSDirector _gpsDirector;
        private DeadAirVehicleAdapter _vehicle;
        private LwsCabGpsController _cabGpsController;
        private LwsCompassNavigatorProAdapter _compassAdapter;
        private GameObject _compassRoot;
        private RectTransform _overlayRoot;
        private RectTransform _contentRoot;
        private TextMeshProUGUI _primaryText;
        private TextMeshProUGUI _secondaryText;
        private TextMeshProUGUI _statusText;
        private TextMeshProUGUI _arrowText;
        private Image _flashOverlay;
        private Image _glitchOverlay;
        private Coroutine _eventRoutine;
        private Coroutine _flashRoutine;
        private Coroutine _glitchRoutine;
        private float _nextBindAttemptTime;
        private bool _bindingDiagnosticLogged;
        private string _currentPrimary = "CONTINUE STRAIGHT";
        private string _currentSecondary = "8.0 MI";
        private string _currentStatus = string.Empty;
        private DeadAirGpsArrow _currentArrow = DeadAirGpsArrow.Straight;
        private bool _narrativeVisible = true;

        public static DeadAirGPSController Instance { get; private set; }
        public bool OverlayBound => _overlayRoot != null;
        public bool CompassRootBound => _compassRoot != null;
        public string LastBindingMessage { get; private set; } = string.Empty;
        public Transform CompassRootTransform => _compassRoot != null ? _compassRoot.transform : null;
        public RectTransform OverlayRoot => _overlayRoot;
        public TextMeshProUGUI PrimaryText => _primaryText;
        public TextMeshProUGUI SecondaryText => _secondaryText;
        public TextMeshProUGUI StatusText => _statusText;
        public TextMeshProUGUI ArrowText => _arrowText;
        public static Vector3 ConfiguredLocalPosition => GPS_LOCAL_POSITION;
        public static Vector3 ConfiguredLocalEulerAngles => GPS_LOCAL_EULER_ANGLES;
        public static Vector3 ConfiguredLocalScale => GPS_LOCAL_SCALE;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Duplicate Dead Air GPS controller disabled; the existing controller remains active.", this);
                enabled = false;
                return;
            }

            Instance = this;
            ResolveReferences();
        }

        private void Start()
        {
            AttachToCurrentTruck();
            SetNormal();
        }

        private void LateUpdate()
        {
            if (!autoBindToCabGps || OverlayBound || Time.unscaledTime < _nextBindAttemptTime)
            {
                return;
            }

            _nextBindAttemptTime = Time.unscaledTime + Mathf.Max(0.05f, bindRetryIntervalSeconds);
            AttachToCurrentTruck();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static DeadAirGPSController ResolveShared()
        {
            if (Instance != null && Instance.enabled)
            {
                return Instance;
            }

            Instance = FindAnyObjectByType<DeadAirGPSController>();
            return Instance != null && Instance.enabled ? Instance : null;
        }

        public bool AttachToCurrentTruck()
        {
            ResolveReferences();
            if (_vehicle == null)
            {
                LastBindingMessage = "Dead Air GPS waiting for a DeadAirVehicleAdapter.";
                return false;
            }

            GameObject truck = _vehicle.gameObject;
            if (createMissingCabGpsInfrastructure)
            {
                EnsureCabGpsInfrastructure(truck);
            }

            if (_cabGpsController == null)
            {
                LastBindingMessage = "Dead Air GPS waiting for LwsCabGpsController.";
                return false;
            }

            _cabGpsController.BindPhysicalScreen();
            _compassRoot = FindChildGameObjectRecursive(truck.transform, CabCompassRootName);
            if (_compassRoot == null)
            {
                LastBindingMessage = "Dead Air GPS waiting for IH Cab GPS Compass Navigator Pro.";
                return false;
            }

            if (applyConfiguredPhysicalPlacement)
            {
                ApplyConfiguredPhysicalPlacement(_compassRoot.transform);
            }

            Vector2 screenSize = ResolveScreenSize(_compassRoot, _cabGpsController);
            BuildOrRepairOverlay(_compassRoot.transform, screenSize);
            UpdateOverlayText();
            LogBindingDiagnostic(screenSize);
            LastBindingMessage = $"Dead Air GPS narrative overlay bound to {BuildHierarchyPath(_compassRoot.transform)}.";
            return true;
        }

        public void ExecuteEvent(DeadAirGpsNarrativeEvent gpsEvent)
        {
            if (gpsEvent == null || !gpsEvent.ShouldRun)
            {
                return;
            }

            if (_eventRoutine != null)
            {
                StopCoroutine(_eventRoutine);
                _eventRoutine = null;
            }

            _eventRoutine = StartCoroutine(RunEvent(gpsEvent.Clone()));
        }

        public void ShowDirection(string primaryText, string secondaryText, DeadAirGpsArrow arrow, bool flash = true, AudioClip gpsAudioClip = null, float gpsAudioVolume = 0.85f)
        {
            ApplyDirection(primaryText, secondaryText, arrow, 805f, string.Empty, true, false, false);
            PlayGPSSound(gpsAudioClip, gpsAudioVolume);
            if (flash)
            {
                Flash();
            }
        }

        public void ShowMessage(string primaryText, string secondaryText = "", DeadAirGpsArrow arrow = DeadAirGpsArrow.None, string statusText = "", bool flash = true)
        {
            _narrativeVisible = true;
            _currentPrimary = SanitizeText(primaryText);
            _currentSecondary = SanitizeText(secondaryText);
            _currentStatus = SanitizeText(statusText);
            _currentArrow = arrow;
            UpdateOverlayText();
            if (flash)
            {
                Flash();
            }
        }

        public void SetPrimaryText(string value)
        {
            _currentPrimary = SanitizeText(value);
            _narrativeVisible = true;
            UpdateOverlayText();
        }

        public void SetSecondaryText(string value)
        {
            _currentSecondary = SanitizeText(value);
            _narrativeVisible = true;
            UpdateOverlayText();
        }

        public void SetArrow(DeadAirGpsArrow arrow)
        {
            _currentArrow = arrow;
            _narrativeVisible = true;
            UpdateOverlayText();
        }

        public void Flash()
        {
            EnsureOverlayReady();
            StopFlashRoutine();
            _flashRoutine = StartCoroutine(RunFlashEffect());
        }

        public void Glitch(float duration = -1f, float intensity = -1f, AudioClip gpsAudioClip = null, float gpsAudioVolume = 0.85f)
        {
            EnsureOverlayReady();
            StopGlitchRoutine();
            PlayGPSSound(gpsAudioClip, gpsAudioVolume);
            _glitchRoutine = StartCoroutine(RunGlitchEffect(
                duration >= 0f ? duration : defaultGlitchDuration,
                intensity >= 0f ? intensity : defaultGlitchIntensity,
                _currentPrimary,
                _currentSecondary,
                _currentStatus,
                _currentArrow,
                true));
        }

        public void Recalculating(string recalculatingMessage, float duration, string finalPrimaryText, string finalSecondaryText, DeadAirGpsArrow finalArrow, bool flash = true, bool glitch = false, AudioClip gpsAudioClip = null, float gpsAudioVolume = 0.85f)
        {
            var gpsEvent = new DeadAirGpsNarrativeEvent
            {
                enableGpsEvent = true,
                eventType = DeadAirGpsNarrativeEventType.RecalculatingThenDirection,
                recalculating = true,
                recalculatingMessage = recalculatingMessage,
                recalculatingDuration = duration,
                finalPrimaryText = finalPrimaryText,
                finalSecondaryText = finalSecondaryText,
                finalArrow = finalArrow,
                flash = flash,
                glitch = glitch,
                gpsAudioClip = gpsAudioClip,
                gpsAudioVolume = gpsAudioVolume
            };
            ExecuteEvent(gpsEvent);
        }

        public void ClearNarrativeOverlay()
        {
            CancelTransientEffects(false);
            _narrativeVisible = false;
            _currentPrimary = string.Empty;
            _currentSecondary = string.Empty;
            _currentStatus = string.Empty;
            _currentArrow = DeadAirGpsArrow.None;
            UpdateOverlayText();
        }

        public void SetNormal()
        {
            CancelTransientEffects(false);
            DeadAirGpsState state = _gpsDirector != null ? _gpsDirector.CurrentState : DeadAirGpsState.Default();
            ApplyDirection(
                string.IsNullOrWhiteSpace(state.instructionText) ? "CONTINUE STRAIGHT" : state.instructionText,
                FormatDistance(state.distanceToInstructionMeters),
                state.arrow,
                state.distanceToInstructionMeters,
                state.destination,
                state.routeVisible,
                state.intentionallyWrong,
                false);
        }

        public void PlayGPSSound(AudioClip clip, float volume = 0.85f)
        {
            if (clip == null)
            {
                return;
            }

            EnsureAudioSource();
            if (gpsAudioSource != null)
            {
                gpsAudioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
            }
        }

        private IEnumerator RunEvent(DeadAirGpsNarrativeEvent gpsEvent)
        {
            if (gpsEvent.delayBeforeGpsEvent > 0f)
            {
                yield return new WaitForSeconds(gpsEvent.delayBeforeGpsEvent);
            }

            EnsureOverlayReady();
            CancelTransientEffects(false);
            PlayGPSSound(gpsEvent.gpsAudioClip, gpsEvent.gpsAudioVolume);

            switch (gpsEvent.eventType)
            {
                case DeadAirGpsNarrativeEventType.ChangeDirection:
                case DeadAirGpsNarrativeEventType.FlashAndChangeDirection:
                    ApplyDirectionFromEvent(gpsEvent, false);
                    if (gpsEvent.flash || gpsEvent.eventType == DeadAirGpsNarrativeEventType.FlashAndChangeDirection)
                    {
                        Flash();
                    }
                    break;
                case DeadAirGpsNarrativeEventType.Glitch:
                    yield return RunGlitchEffect(gpsEvent.glitchDuration, gpsEvent.glitchIntensity, _currentPrimary, _currentSecondary, _currentStatus, _currentArrow, true);
                    break;
                case DeadAirGpsNarrativeEventType.GlitchThenDirection:
                    yield return RunGlitchEffect(gpsEvent.glitchDuration, gpsEvent.glitchIntensity, _currentPrimary, _currentSecondary, _currentStatus, _currentArrow, false);
                    ApplyDirectionFromEvent(gpsEvent, false);
                    if (gpsEvent.flash)
                    {
                        Flash();
                    }
                    break;
                case DeadAirGpsNarrativeEventType.Recalculating:
                    yield return RunRecalculating(gpsEvent, false);
                    break;
                case DeadAirGpsNarrativeEventType.RecalculatingThenDirection:
                    yield return RunRecalculating(gpsEvent, true);
                    break;
                case DeadAirGpsNarrativeEventType.ClearOverlay:
                    ClearNarrativeOverlay();
                    break;
                case DeadAirGpsNarrativeEventType.CustomCombined:
                    yield return RunCustomCombined(gpsEvent);
                    break;
            }

            _eventRoutine = null;
        }

        private IEnumerator RunCustomCombined(DeadAirGpsNarrativeEvent gpsEvent)
        {
            if (gpsEvent.recalculating)
            {
                yield return RunRecalculating(gpsEvent, true);
                yield break;
            }

            if (gpsEvent.glitch)
            {
                yield return RunGlitchEffect(gpsEvent.glitchDuration, gpsEvent.glitchIntensity, _currentPrimary, _currentSecondary, _currentStatus, _currentArrow, false);
            }

            ApplyDirectionFromEvent(gpsEvent, false);
            if (gpsEvent.flash)
            {
                Flash();
            }
        }

        private IEnumerator RunRecalculating(DeadAirGpsNarrativeEvent gpsEvent, bool applyFinalDirection)
        {
            string message = string.IsNullOrWhiteSpace(gpsEvent.recalculatingMessage) ? "RECALCULATING..." : gpsEvent.recalculatingMessage;
            ShowMessage(message, string.Empty, DeadAirGpsArrow.None, "RECALCULATING", gpsEvent.flash);
            _gpsDirector?.SetRecalculating(true);
            if (gpsEvent.glitch)
            {
                StartGlitchRoutine(gpsEvent.glitchDuration, gpsEvent.glitchIntensity);
            }

            yield return new WaitForSeconds(Mathf.Max(0f, gpsEvent.recalculatingDuration));
            CancelTransientEffects(false);
            _gpsDirector?.SetRecalculating(false);

            if (applyFinalDirection)
            {
                ApplyDirection(
                    string.IsNullOrWhiteSpace(gpsEvent.finalPrimaryText) ? gpsEvent.primaryText : gpsEvent.finalPrimaryText,
                    string.IsNullOrWhiteSpace(gpsEvent.finalSecondaryText) ? gpsEvent.secondaryText : gpsEvent.finalSecondaryText,
                    gpsEvent.finalArrow,
                    gpsEvent.distanceMeters,
                    gpsEvent.destination,
                    gpsEvent.routeVisible,
                    gpsEvent.intentionallyWrong,
                    false);
                if (gpsEvent.flash)
                {
                    Flash();
                }
            }
        }

        private void ApplyDirectionFromEvent(DeadAirGpsNarrativeEvent gpsEvent, bool suppressDirector)
        {
            ApplyDirection(
                gpsEvent.primaryText,
                gpsEvent.secondaryText,
                gpsEvent.arrow,
                gpsEvent.distanceMeters,
                gpsEvent.destination,
                gpsEvent.routeVisible,
                gpsEvent.intentionallyWrong,
                suppressDirector);
        }

        private void ApplyDirection(string primary, string secondary, DeadAirGpsArrow arrow, float distanceMeters, string destination, bool routeVisible, bool intentionallyWrong, bool suppressDirector)
        {
            _narrativeVisible = true;
            _currentPrimary = SanitizeText(primary);
            _currentSecondary = SanitizeText(secondary);
            _currentStatus = intentionallyWrong ? "SIGNAL VARIANCE" : string.Empty;
            _currentArrow = arrow;
            UpdateOverlayText();

            if (!suppressDirector)
            {
                ResolveReferences();
                _gpsDirector?.SetEnabled(true);
                if (!string.IsNullOrWhiteSpace(destination))
                {
                    _gpsDirector?.SetDestination(destination);
                }

                _gpsDirector?.SetRouteVisible(routeVisible);
                _gpsDirector?.SetGpsInstruction(_currentPrimary, arrow, Mathf.Max(0f, distanceMeters), intentionallyWrong);
            }
        }

        private void EnsureOverlayReady()
        {
            if (!OverlayBound)
            {
                AttachToCurrentTruck();
            }
        }

        private void EnsureCabGpsInfrastructure(GameObject truck)
        {
            if (truck == null)
            {
                return;
            }

            LwsCabAccessoryAnchorRegistry anchors = truck.GetComponent<LwsCabAccessoryAnchorRegistry>();
            if (anchors == null)
            {
                anchors = truck.AddComponent<LwsCabAccessoryAnchorRegistry>();
            }

            anchors.EnsureInitialized();

            _cabGpsController = truck.GetComponent<LwsCabGpsController>();
            if (_cabGpsController == null)
            {
                _cabGpsController = truck.AddComponent<LwsCabGpsController>();
            }

            _compassAdapter = truck.GetComponent<LwsCompassNavigatorProAdapter>();
            if (_compassAdapter == null)
            {
                _compassAdapter = truck.AddComponent<LwsCompassNavigatorProAdapter>();
            }
        }

        private void BuildOrRepairOverlay(Transform compassRoot, Vector2 screenSize)
        {
            RectTransform compassRect = compassRoot as RectTransform;
            if (compassRect != null)
            {
                compassRect.sizeDelta = screenSize;
            }

            _overlayRoot = EnsureRect(compassRoot, NarrativeOverlayName);
            StretchToFill(_overlayRoot);
            _overlayRoot.SetAsLastSibling();

            _contentRoot = EnsureRect(_overlayRoot, "Narrative Content");
            StretchToFill(_contentRoot);

            Image backplate = EnsureImage(_contentRoot, "Narrative Text Backplate", backplateColor);
            ConfigureAnchoredRect(backplate.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 78f), new Vector2(screenSize.x - 38f, 128f));

            _arrowText = EnsureText(_contentRoot, "Direction Arrow", 50f, TextAlignmentOptions.Center, narrativeWarningColor);
            ConfigureAnchoredRect(_arrowText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(74f, 78f), new Vector2(112f, 92f));

            _primaryText = EnsureText(_contentRoot, "Primary Direction Text", 31f, TextAlignmentOptions.Left, narrativeTextColor);
            _primaryText.fontStyle = FontStyles.Bold;
            ConfigureAnchoredRect(_primaryText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(154f, 92f), new Vector2(screenSize.x - 198f, 44f));

            _secondaryText = EnsureText(_contentRoot, "Secondary Direction Text", 22f, TextAlignmentOptions.Left, narrativeTextColor);
            ConfigureAnchoredRect(_secondaryText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(154f, 48f), new Vector2(screenSize.x - 198f, 34f));

            _statusText = EnsureText(_contentRoot, "Status Text", 18f, TextAlignmentOptions.Right, narrativeWarningColor);
            ConfigureAnchoredRect(_statusText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(screenSize.x - 236f, -28f), new Vector2(212f, 28f));

            _glitchOverlay = EnsureImage(_overlayRoot, "Glitch Overlay", WithAlpha(glitchColor, 0f));
            StretchToFill(_glitchOverlay.rectTransform);
            _glitchOverlay.raycastTarget = false;

            _flashOverlay = EnsureImage(_overlayRoot, "Flash Overlay", WithAlpha(flashColor, 0f));
            StretchToFill(_flashOverlay.rectTransform);
            _flashOverlay.raycastTarget = false;
        }

        private IEnumerator RunFlashEffect()
        {
            for (int i = 0; i < Mathf.Max(1, flashCount); i++)
            {
                SetImageAlpha(_flashOverlay, flashColor.a);
                yield return new WaitForSeconds(flashStepSeconds);
                SetImageAlpha(_flashOverlay, 0f);
                yield return new WaitForSeconds(flashStepSeconds);
            }

            SetImageAlpha(_flashOverlay, 0f);
            _flashRoutine = null;
        }

        private void StartGlitchRoutine(float duration, float intensity)
        {
            StopGlitchRoutine();
            _glitchRoutine = StartCoroutine(RunGlitchEffect(duration, intensity, _currentPrimary, _currentSecondary, _currentStatus, _currentArrow, true));
        }

        private IEnumerator RunGlitchEffect(float duration, float intensity, string restorePrimary, string restoreSecondary, string restoreStatus, DeadAirGpsArrow restoreArrow, bool restoreFinalText)
        {
            EnsureOverlayReady();
            float end = Time.time + Mathf.Max(0.05f, duration);
            float clampedIntensity = Mathf.Clamp01(intensity <= 0f ? defaultGlitchIntensity : intensity);
            Vector2 originalPosition = _contentRoot != null ? _contentRoot.anchoredPosition : Vector2.zero;
            while (Time.time < end)
            {
                if (_contentRoot != null)
                {
                    _contentRoot.anchoredPosition = originalPosition + Random.insideUnitCircle * glitchMaxPixelOffset * clampedIntensity;
                }

                if (_primaryText != null)
                {
                    _primaryText.text = CorruptText(restorePrimary, clampedIntensity);
                }

                if (_secondaryText != null)
                {
                    _secondaryText.text = CorruptText(restoreSecondary, clampedIntensity);
                }

                if (_arrowText != null)
                {
                    _arrowText.text = Random.value > 0.45f ? GetArrowGlyph(restoreArrow) : GetArrowGlyph((DeadAirGpsArrow)Random.Range(0, 10));
                }

                SetImageAlpha(_glitchOverlay, Random.Range(0.08f, glitchColor.a) * clampedIntensity);
                yield return new WaitForSeconds(glitchFlickerIntervalSeconds);
            }

            if (_contentRoot != null)
            {
                _contentRoot.anchoredPosition = originalPosition;
            }

            SetImageAlpha(_glitchOverlay, 0f);
            if (restoreFinalText)
            {
                _currentPrimary = restorePrimary;
                _currentSecondary = restoreSecondary;
                _currentStatus = restoreStatus;
                _currentArrow = restoreArrow;
                UpdateOverlayText();
            }

            _glitchRoutine = null;
        }

        private void CancelTransientEffects(bool restoreText)
        {
            StopFlashRoutine();
            StopGlitchRoutine();
            SetImageAlpha(_flashOverlay, 0f);
            SetImageAlpha(_glitchOverlay, 0f);
            if (_contentRoot != null)
            {
                _contentRoot.anchoredPosition = Vector2.zero;
            }

            if (restoreText)
            {
                UpdateOverlayText();
            }
        }

        private void StopFlashRoutine()
        {
            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
                _flashRoutine = null;
            }
        }

        private void StopGlitchRoutine()
        {
            if (_glitchRoutine != null)
            {
                StopCoroutine(_glitchRoutine);
                _glitchRoutine = null;
            }
        }

        private void UpdateOverlayText()
        {
            if (_overlayRoot != null)
            {
                _overlayRoot.gameObject.SetActive(_narrativeVisible);
            }

            if (_primaryText != null)
            {
                _primaryText.text = _currentPrimary;
            }

            if (_secondaryText != null)
            {
                _secondaryText.text = _currentSecondary;
            }

            if (_statusText != null)
            {
                _statusText.text = _currentStatus;
            }

            if (_arrowText != null)
            {
                _arrowText.text = GetArrowGlyph(_currentArrow);
            }
        }

        private void ApplyConfiguredPhysicalPlacement(Transform gpsRoot)
        {
            gpsRoot.localPosition = GPS_LOCAL_POSITION;
            gpsRoot.localRotation = Quaternion.Euler(GPS_LOCAL_EULER_ANGLES);
            gpsRoot.localScale = GPS_LOCAL_SCALE;
        }

        private void ResolveReferences()
        {
            DeadAirGameManager manager = DeadAirGameManager.Instance;
            if (_gpsDirector == null)
            {
                _gpsDirector = manager != null ? manager.GpsDirector : FindAnyObjectByType<DeadAirGPSDirector>();
            }

            if (_vehicle == null)
            {
                _vehicle = manager != null ? manager.VehicleAdapter : FindAnyObjectByType<DeadAirVehicleAdapter>();
            }

            if (_vehicle != null)
            {
                if (_cabGpsController == null)
                {
                    _cabGpsController = _vehicle.GetComponent<LwsCabGpsController>();
                }

                if (_compassAdapter == null)
                {
                    _compassAdapter = _vehicle.GetComponent<LwsCompassNavigatorProAdapter>();
                }
            }
        }

        private void EnsureAudioSource()
        {
            if (gpsAudioSource == null)
            {
                gpsAudioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            }

            gpsAudioSource.playOnAwake = false;
            gpsAudioSource.spatialBlend = 0.35f;
        }

        private void LogBindingDiagnostic(Vector2 screenSize)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!logBindingDiagnostics || _bindingDiagnosticLogged || _compassRoot == null)
            {
                return;
            }

            _bindingDiagnosticLogged = true;
            Vector3 lossy = _compassRoot.transform.lossyScale;
            Vector2 physicalSize = new Vector2(Mathf.Abs(screenSize.x * lossy.x), Mathf.Abs(screenSize.y * lossy.y));
            Debug.Log(
                $"[Dead Air GPS] Narrative overlay bound to Compass Navigator Pro cab map at {BuildHierarchyPath(_compassRoot.transform)}; physical size approx {physicalSize.x:0.00}m x {physicalSize.y:0.00}m.",
                this);
#endif
        }

        private static Vector2 ResolveScreenSize(GameObject compassRoot, LwsCabGpsController cabGps)
        {
            RectTransform rect = compassRoot != null ? compassRoot.GetComponent<RectTransform>() : null;
            if (rect != null && rect.rect.width > 1f && rect.rect.height > 1f)
            {
                return rect.rect.size;
            }

            if (rect != null && rect.sizeDelta.x > 1f && rect.sizeDelta.y > 1f)
            {
                return rect.sizeDelta;
            }

            if (cabGps != null && cabGps.ScreenSize.x > 1f && cabGps.ScreenSize.y > 1f)
            {
                return cabGps.ScreenSize;
            }

            return DefaultScreenSize;
        }

        private static RectTransform EnsureRect(Transform parent, string name)
        {
            Transform existing = FindDirectChild(parent, name);
            RectTransform rect = existing as RectTransform;
            if (rect != null)
            {
                return rect;
            }

            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static Image EnsureImage(Transform parent, string name, Color color)
        {
            RectTransform rect = EnsureRect(parent, name);
            Image image = rect.GetComponent<Image>();
            if (image == null)
            {
                image = rect.gameObject.AddComponent<Image>();
            }

            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI EnsureText(Transform parent, string name, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            RectTransform rect = EnsureRect(parent, name);
            TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            }

            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static void ConfigureAnchoredRect(RectTransform rect, Vector2 anchorMinMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchorMinMax;
            rect.anchorMax = anchorMinMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        private static void StretchToFill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        private static GameObject FindChildGameObjectRecursive(Transform root, string objectName)
        {
            Transform found = FindChildRecursive(root, objectName);
            return found != null ? found.gameObject : null;
        }

        private static Transform FindChildRecursive(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == objectName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildRecursive(root.GetChild(i), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        public static string GetArrowGlyph(DeadAirGpsArrow arrow)
        {
            switch (arrow)
            {
                case DeadAirGpsArrow.Straight:
                    return "^";
                case DeadAirGpsArrow.SlightLeft:
                    return "<^";
                case DeadAirGpsArrow.SlightRight:
                    return "^>";
                case DeadAirGpsArrow.Left:
                    return "<";
                case DeadAirGpsArrow.Right:
                    return ">";
                case DeadAirGpsArrow.Exit:
                case DeadAirGpsArrow.ExitRight:
                    return "EXIT >";
                case DeadAirGpsArrow.ExitLeft:
                    return "< EXIT";
                case DeadAirGpsArrow.UTurn:
                    return "U";
                default:
                    return string.Empty;
            }
        }

        private static string SanitizeText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string FormatDistance(float meters)
        {
            if (meters >= 1609.344f)
            {
                return $"{meters / 1609.344f:0.0} MI";
            }

            return $"{Mathf.Max(0f, meters) * 3.28084f:0} FT";
        }

        private static string CorruptText(string source, float intensity)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(source.Length);
            float chance = Mathf.Lerp(0.15f, 0.65f, Mathf.Clamp01(intensity));
            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                if (!char.IsWhiteSpace(c) && Random.value < chance)
                {
                    c = CorruptionCharacters[Random.Range(0, CorruptionCharacters.Length)];
                }

                builder.Append(c);
            }

            return builder.ToString();
        }

        private static void SetImageAlpha(Image image, float alpha)
        {
            if (image == null)
            {
                return;
            }

            Color color = image.color;
            color.a = Mathf.Clamp01(alpha);
            image.color = color;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static string BuildHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = $"{transform.name}/{path}";
            }

            return path;
        }
    }
}
