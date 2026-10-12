using System;
using TMPro;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Career owns persistence. This view edits a draft and never invents another storage authority.
    public sealed class TruckTaxiDriverAppearancePanel : MonoBehaviour
    {
        public delegate bool CommitAppearance(TruckTaxiDriverAppearanceData value, out string reason);
        private TruckTaxiBootstrap host;
        private TruckTaxiHud hud;
        private RectTransform panel;
        private TextMeshProUGUI feedback;
        private Func<TruckTaxiDriverAppearanceData> capture;
        private CommitAppearance commit;
        private TruckTaxiDriverAppearanceData draft, applied;
        private TruckTaxiDriverPresentation appliedDriver;
        private string editingProfile;
        private bool wasPaused;
        private const float DriverReadyTimeoutSeconds = 180;
        private float nextRefresh, readyDeadline;
        private bool refreshPending;
        private TruckTaxiSession subscribedSession;
        private UnityEngine.UI.Toggle capToggle;
        private UnityEngine.UI.Slider chestSlider;
        private GameObject previewRoot, previewVisual;
        private Camera previewCamera;
        private RenderTexture previewTexture;
        private UnityEngine.UI.RawImage previewImage;
        public bool IsOpen => panel != null && panel.gameObject.activeSelf;
        public Transform FocusRoot => panel;
        public event Action VisibilityChanged;
        private static readonly Color[] Clothing = { new Color(.2f,.43f,.55f), new Color(.12f,.15f,.18f),
            new Color(.87f,.26f,.18f), new Color(.9f,.9f,.9f), new Color(.2f,.4f,.25f), new Color(.55f,.25f,.4f) };
        private static readonly Color[] Skin = { new Color(.95f,.79f,.66f), new Color(.86f,.65f,.48f),
            new Color(.76f,.54f,.38f), new Color(.59f,.38f,.24f), new Color(.4f,.25f,.16f), new Color(.25f,.15f,.1f) };

        public void Initialize(TruckTaxiBootstrap owner, TruckTaxiHud view)
        {
            if (panel != null) return;
            if (owner == null || view == null) throw new ArgumentNullException("Driver appearance requires the existing host and HUD.");
            host = owner; hud = view;
            panel = hud.Panel(hud.Root, "Driver appearance", new Vector2(.13f,.15f), new Vector2(.87f,.87f));
            panel.gameObject.SetActive(false);
            hud.Text(panel, "Title", new Vector2(.05f,.88f), new Vector2(.95f,.98f), 32).text = "DRIVER APPEARANCE";
            Swatches("SHIRT", .73f, Clothing, c => draft.shirt = c);
            Swatches("PANTS / CAP", .58f, Clothing, c => draft.pantsAndCap = c);
            Swatches("TRIM", .43f, Clothing, c => draft.trim = c);
            Swatches("SKIN", .28f, Skin, c => draft.skin = c);
            BuildChestControl();
            var previewFrame = TruckTaxiHud.Rect(panel, "Adult driver preview", new Vector2(.66f,.18f), new Vector2(.96f,.86f));
            previewImage = TruckTaxiHud.Rect(previewFrame, "Image", Vector2.zero, Vector2.one).gameObject.AddComponent<UnityEngine.UI.RawImage>();
            var aspect = previewImage.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            aspect.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent; aspect.aspectRatio = 2f / 3f;
            previewImage.raycastTarget = false;
            var toggleRoot = TruckTaxiHud.Rect(panel, "Cap", new Vector2(.05f,.11f), new Vector2(.3f,.16f));
            capToggle = toggleRoot.gameObject.AddComponent<UnityEngine.UI.Toggle>();
            var box = TruckTaxiHud.Rect(toggleRoot, "Box", Vector2.zero, new Vector2(.18f,1));
            var background = box.gameObject.AddComponent<UnityEngine.UI.Image>(); background.color = Color.gray;
            var mark = TruckTaxiHud.Rect(box, "Check", new Vector2(.2f,.2f), new Vector2(.8f,.8f)).gameObject.AddComponent<UnityEngine.UI.Image>();
            mark.color = Color.white; capToggle.targetGraphic = background; capToggle.graphic = mark;
            hud.Text(toggleRoot, "Cap label", new Vector2(.23f,0), Vector2.one, 22).text = "CAP";
            capToggle.onValueChanged.AddListener(value => { if (draft != null) { draft.capVisible = value; Preview(); } });
            feedback = hud.Text(panel, "Feedback", new Vector2(.05f,.015f), new Vector2(.44f,.1f), 18); feedback.richText = false;
            hud.Button(panel, "CANCEL", new Vector2(.47f,.025f), new Vector2(.69f,.12f), Close);
            hud.Button(panel, "APPLY", new Vector2(.72f,.025f), new Vector2(.95f,.12f), Apply);
            panel.gameObject.SetActive(false);
        }

        public void BindCareer(Func<TruckTaxiDriverAppearanceData> read, CommitAppearance write)
        {
            if (read == null || write == null) throw new ArgumentNullException("Career capture and commit are required.");
            if (IsOpen) Close(); capture = read; commit = write; applied = null;
            SubscribeCareer(); QueueRefresh();
        }
        private void SubscribeCareer()
        {
            var session = host?.Session;
            if (ReferenceEquals(session, subscribedSession)) return;
            UnsubscribeCareer(); subscribedSession = session;
            if (subscribedSession != null) subscribedSession.CareerEquipmentChanged += QueueRefresh;
        }
        private void UnsubscribeCareer()
        {
            if (subscribedSession != null) subscribedSession.CareerEquipmentChanged -= QueueRefresh;
            subscribedSession = null;
        }
        private void QueueRefresh()
        {
            if (!isActiveAndEnabled || capture == null) return;
            // Repeated events do not extend an already-bounded readiness wait indefinitely.
            if (!refreshPending) readyDeadline = Time.realtimeSinceStartup + DriverReadyTimeoutSeconds;
            refreshPending = true; nextRefresh = 0;
        }
        private bool OwnedRun => host?.Persistence?.HasActiveRun == true && !host.Persistence.ResumeBlocked &&
            !host.Persistence.IsBusy && host.Session != null && host.Session.CareerRunProfileId == host.Persistence.RunProfileId;
        private TruckTaxiDriverPresentation Driver => host?.Player != null ? host.Player.GetComponent<TruckTaxiDriverPresentation>() : null;
        public void Open()
        {
            if (panel == null || IsOpen || capture == null || commit == null || !OwnedRun || Driver?.Ready != true) return;
            var saved = capture();
            if (saved == null || !saved.Validate(out _)) return;
            draft = saved.Copy(); editingProfile = host.Persistence.RunProfileId;
            wasPaused = host.Paused; host.SetPaused(true); capToggle.SetIsOnWithoutNotify(draft.capVisible);
            if (chestSlider != null) { chestSlider.SetValueWithoutNotify(draft.chestShape); chestSlider.interactable = Driver.SupportsChestShape; }
            feedback.text = chestSlider == null ? "Chest control requires the configured Heat slider." : string.Empty;
            panel.gameObject.SetActive(true); panel.SetAsLastSibling();
            RenderPreview();
            hud.UIInput?.Focus(panel); VisibilityChanged?.Invoke();
        }
        private void Swatches(string label, float y, Color[] colors, Action<Color> select)
        {
            hud.Text(panel, label, new Vector2(.05f,y), new Vector2(.28f,y+.12f), 22).text = label;
            for (int i = 0; i < colors.Length; i++)
            {
                Color color = colors[i]; float x = .30f + i * .055f;
                var rect = TruckTaxiHud.Rect(panel, label + " swatch " + i, new Vector2(x,y+.02f), new Vector2(x+.043f,y+.11f));
                var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color;
                var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
                button.onClick.AddListener(() => { if (draft != null) { select(color); Preview(); } });
            }
        }
        private void Preview()
        {
            if (!OwnedRun || editingProfile != host.Persistence.RunProfileId) { Close(); return; }
            if (Driver == null) { feedback.text = "DRIVER UNAVAILABLE"; return; }
            if (!Driver.TryApplyAppearance(draft, out string reason)) feedback.text = reason;
            else RenderPreview();
        }
        private void BuildChestControl()
        {
            hud.Text(panel, "Chest label", new Vector2(.05f,.18f), new Vector2(.29f,.26f), 22).text = "CHEST SHAPE";
            if (hud.heatSliderPrefab == null) return;
            var control = Instantiate(hud.heatSliderPrefab, panel, false);
            control.name = "Adult chest shape";
            var rect = (RectTransform)control.transform;
            rect.anchorMin = new Vector2(.31f,.20f); rect.anchorMax = new Vector2(.62f,.24f); rect.offsetMin = rect.offsetMax = Vector2.zero;
            foreach (var component in control.GetComponents<Component>())
            {
                var type = component.GetType();
                if (type.FullName != "Michsky.UI.Heat.SliderManager") continue;
                type.GetField("saveValue").SetValue(component, false);
                type.GetField("invokeOnAwake").SetValue(component, false);
                type.GetField("useSounds").SetValue(component, false);
            }
            foreach (var text in control.GetComponentsInChildren<TMP_Text>(true)) text.enabled = false;
            foreach (var field in control.GetComponentsInChildren<TMP_InputField>(true)) field.gameObject.SetActive(false);
            chestSlider = control.GetComponent<UnityEngine.UI.Slider>();
            chestSlider.minValue = -1; chestSlider.maxValue = 1; chestSlider.wholeNumbers = false;
            chestSlider.onValueChanged.AddListener(value => { if (draft != null) { draft.chestShape = value; Preview(); } });
        }
        private void RenderPreview()
        {
            if (Driver?.Ready != true || draft == null) return;
            if (previewRoot == null)
            {
                previewRoot = new GameObject("Driver appearance preview stage");
                previewRoot.transform.position = new Vector3(0, -10000, 0);
                previewVisual = Driver.CreateAppearanceVisual(previewRoot.transform);
                previewVisual.transform.localRotation = Quaternion.Euler(0, 18, 0);
                foreach (var part in previewVisual.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = 29;
                foreach (var renderer in previewVisual.GetComponentsInChildren<Renderer>()) renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var eye = new GameObject("Preview camera"); eye.transform.SetParent(previewRoot.transform, false);
                eye.transform.localPosition = new Vector3(0, .85f, 3);
                eye.transform.LookAt(previewRoot.transform.position + Vector3.up * .85f);
                previewCamera = eye.AddComponent<Camera>(); previewCamera.enabled = false;
                previewCamera.cullingMask = 1 << 29; previewCamera.nearClipPlane = .05f; previewCamera.farClipPlane = 6;
                previewCamera.fieldOfView = 35; previewCamera.clearFlags = CameraClearFlags.SolidColor;
                previewCamera.backgroundColor = new Color(.12f,.14f,.13f);
                previewTexture = new RenderTexture(256, 384, 16) { name = "Adult driver appearance preview" };
                previewCamera.targetTexture = previewTexture; previewImage.texture = previewTexture;
                var lamp = new GameObject("Preview light"); lamp.transform.SetParent(eye.transform, false);
                var light = lamp.AddComponent<Light>(); light.type = LightType.Spot; light.range = 6; light.spotAngle = 65;
                light.intensity = 4; light.cullingMask = 1 << 29; light.shadows = LightShadows.None;
            }
            TruckTaxiDriverPresentation.ApplyVisualAppearance(previewVisual.transform, draft);
            previewRoot.SetActive(true);
            try { previewCamera.Render(); }
            finally { previewRoot.SetActive(false); }
        }
        private void Apply()
        {
            if (!IsOpen || !OwnedRun || editingProfile != host.Persistence.RunProfileId || commit == null) return;
            if (!commit(draft.Copy(), out string reason)) { feedback.text = reason; return; }
            host.Persistence.RequestAutosave("Driver appearance");
            Close();
        }
        public void Close()
        {
            if (!IsOpen) return;
            panel.gameObject.SetActive(false); draft = null; applied = null;
            if (RefreshSaved()) refreshPending = false;
            else QueueRefresh();
            host.SetPaused(wasPaused); hud.UIInput?.Focus(null); VisibilityChanged?.Invoke();
        }
        private bool RefreshSaved()
        {
            if (!OwnedRun || capture == null || Driver?.Ready != true) return false;
            var saved = capture();
            if (saved == null || !saved.Validate(out _)) return false;
            if (appliedDriver == Driver && saved.SameAs(applied)) return true;
            if (!Driver.TryApplyAppearance(saved, out _)) return false;
            appliedDriver = Driver; applied = saved.Copy(); return true;
        }
        private void Update()
        {
            if (IsOpen) { if (!OwnedRun || editingProfile != host.Persistence.RunProfileId) Close(); return; }
            if (!refreshPending) return;
            if (Time.realtimeSinceStartup >= readyDeadline)
            {
                refreshPending = false;
                Debug.LogWarning("Driver appearance readiness timed out; a later Career equipment event will retry.", this);
                return;
            }
            if (Time.realtimeSinceStartup < nextRefresh) return;
            nextRefresh = Time.realtimeSinceStartup + .25f;
            SubscribeCareer();
            if (RefreshSaved()) refreshPending = false;
        }
        public bool HandleInput(TruckTaxiUIInput input)
        {
            if (!IsOpen) return false;
            input.Focus(panel);
            if (input.Cancel.WasPressedThisFrame() || input.Pause.WasPressedThisFrame()) Close();
            else if (input.Submit.WasPressedThisFrame()) input.SubmitSelected();
            return true;
        }
        private void OnEnable() { if (capture != null) { SubscribeCareer(); QueueRefresh(); } }
        private void OnDisable()
        {
            if (IsOpen) Close();
            UnsubscribeCareer(); refreshPending = false;
        }
        private void OnDestroy()
        {
            UnsubscribeCareer();
            if (previewRoot != null) Destroy(previewRoot);
            if (previewTexture != null) { previewTexture.Release(); Destroy(previewTexture); }
        }
    }
}
