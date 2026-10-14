using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using TMPro;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Scene-owned interaction only. The canonical truck, Session, HUD and vendor save system remain authorities.
    [DefaultExecutionOrder(-50), DisallowMultipleComponent]
    public sealed class TruckTaxiGarage : MonoBehaviour
    {
        private TruckTaxiBootstrap host;
        private TruckTaxiHud hud;
        private TruckTaxiHudModalBridge bridge;
        private BoxCollider volume;
        private TruckTaxiGaragePaint paint;
        private TruckTaxiSession session;
        private TruckTaxiCareerCatalog catalog;
        private RectTransform overlay, panel;
        private TextMeshProUGUI prompt, truckName, feedback, colorLabel;
        private UnityEngine.UI.Image swatch;
        private GameObject previousTruck, nextTruck;
        private readonly UnityEngine.UI.Slider[] sliders = new UnityEngine.UI.Slider[3];
        private readonly Texture2D[] gradients = new Texture2D[3];
        private readonly List<LwsKeyboardGamepadTruckInputSource> inputs = new List<LwsKeyboardGamepadTruckInputSource>();
        private readonly TruckTaxiGarageDraft draft = new TruckTaxiGarageDraft();
        private List<TruckTaxiCareerTruckDefinition> owned;
        private IDisposable lease;
        private string runId;
        private int selectedIndex;
        private bool applying;
        private Camera previewCamera;
        private RenderTexture previewTexture;
        public bool IsOpen => lease != null;
        public Transform FocusRoot => panel;

        // Call after host.Ready / HUD initialization. Volume placement is entirely owned by scene authoring.
        public void Configure(TruckTaxiBootstrap owner, TruckTaxiHud view, BoxCollider interactionVolume, Material[] bodyMaterials)
        {
            if (owner == null || !owner.Ready || owner.Player == null || owner.Session == null ||
                view == null || view.Root == null || view.UIInput == null || view.heatSliderPrefab == null ||
                interactionVolume == null || !interactionVolume.isTrigger || bodyMaterials == null)
                throw new ArgumentException("Garage requires a ready host/HUD, Heat slider, trigger volume and explicit body materials.");
            Cancel(); Unsubscribe(); DestroyUi();
            host = owner; hud = view; volume = interactionVolume; session = host.Session;
            catalog = host.CareerEquipment?.Catalog;
            bridge = hud.GetComponent<TruckTaxiHudModalBridge>() ?? hud.gameObject.AddComponent<TruckTaxiHudModalBridge>();
            bridge.Initialize(host, hud);
            paint = host.Player.GetComponent<TruckTaxiGaragePaint>() ?? host.Player.gameObject.AddComponent<TruckTaxiGaragePaint>();
            paint.Configure(host.Player, bodyMaterials, session, catalog);
            if (!paint.Ready) Debug.LogWarning("Garage has no matching canonical body-material slots; customization stays unavailable.", this);
            BuildUi(); Subscribe();
        }

        private bool CanonicalTruck
        {
            get
            {
                var registry = LwsApplicationBootstrap.Instance?.Registry;
                return registry != null && registry.TryGet(out ILwsPlayerVehicleService vehicles) &&
                    vehicles.ActiveTruck == host.Player && host.spawner?.SpawnedTruck == host.Player;
            }
        }
        private bool Eligible(bool editing)
        {
            if (!isActiveAndEnabled || host == null || !host.Ready || host.Player == null || host.Session != session ||
                host.Persistence == null || !host.Persistence.HasActiveRun || host.Persistence.ResumeBlocked || host.Persistence.IsBusy ||
                session.CareerRunProfileId != host.Persistence.RunProfileId || session.State != TruckTaxiState.Available ||
                session.EquippedTruckId != host.Player.DefinitionId ||
                host.WorldLife?.IsOnFoot == true || host.Races?.IsActive == true || host.Companions?.IsBusy == true ||
                host.CustomContent?.OwnsInputHold == true || !CanonicalTruck || paint == null || !paint.Ready || volume == null ||
                !volume.enabled || !volume.gameObject.activeInHierarchy || !volume.isTrigger) return false;
            if (editing ? !bridge.IsOwnedBy(this) || host.Persistence.RunProfileId != runId : host.Paused || !bridge.CanAcquire)
                return false;
            var body = host.Player.GetComponent<Rigidbody>();
            if (body == null || body.linearVelocity.sqrMagnitude > .01f || body.angularVelocity.sqrMagnitude > .01f) return false;
            Vector3 local = volume.transform.InverseTransformPoint(body.position) - volume.center;
            Vector3 half = volume.size * .5f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }

        private void Update()
        {
            if (overlay == null) return;
            if (IsOpen)
            {
                if (!overlay.gameObject.activeInHierarchy || !Eligible(true) || !bridge.IsOpen) Cancel();
                return;
            }
            bool available = Eligible(false) && hud.EnvironmentNeeds?.ServiceInteractionEligible != true && host.Companions?.CanInteract != true;
            prompt.gameObject.SetActive(available);
            if (!available) return;
            prompt.text = "[" + hud.UIInput.Hint(hud.UIInput.WorldInteract) + "] CUSTOMIZE TRUCK";
            if (hud.UIInput.WorldInteract.WasPressedThisFrame()) TryOpen();
        }
        public bool TryOpen()
        {
            if (IsOpen || !Eligible(false) || !draft.Begin(session.CaptureCareerProfile(), catalog)) return false;
            lease = bridge.TryAcquire(this, panel, Cancel, Interrupt);
            if (lease == null) { draft.Cancel(); return false; }
            runId = host.Persistence.RunProfileId;
            session.AcquireOfferSuppression(this);
            inputs.Clear(); host.Player.GetComponentsInChildren(true, inputs);
            foreach (var input in inputs) input.AcquireDrivingInputSuppression(this);
            owned = TruckTaxiGarageDraft.OwnedTrucks(session.CaptureCareerProfile(), catalog);
            previousTruck.SetActive(owned.Count > 1); nextTruck.SetActive(owned.Count > 1);
            selectedIndex = owned.FindIndex(t => t.StableId == draft.TruckId);
            overlay.gameObject.SetActive(true); overlay.SetAsLastSibling(); prompt.gameObject.SetActive(false);
            feedback.text = string.Empty; ShowSelection(); hud.UIInput.Focus(panel); return true;
        }

        private void SelectTruck(int direction)
        {
            if (!IsOpen || owned.Count < 2) return;
            selectedIndex = (selectedIndex + direction + owned.Count) % owned.Count;
            if (draft.Select(catalog, owned[selectedIndex].StableId)) ShowSelection();
        }
        private void ShowSelection()
        {
            restoringDefault = false;
            var entry = owned[selectedIndex];
            truckName.text = entry.DisplayName + "  (" + (selectedIndex + 1) + "/" + owned.Count + ")";
            feedback.text = entry.StableId == host.Player.DefinitionId ? string.Empty : "Canonical factory swap is unavailable for this truck.";
            var selected = session.CaptureCareerProfile().trucks.Find(t => t.truckId == draft.TruckId);
            SetPicker(draft.Paint?.color ?? paint.CareerColor(selected)); Preview();
        }
        private void SetPicker(Color value)
        {
            Color.RGBToHSV(value, out float h, out float s, out float v);
            sliders[0].SetValueWithoutNotify(h); sliders[1].SetValueWithoutNotify(s); sliders[2].SetValueWithoutNotify(v);
            RefreshPicker();
        }
        private Color PickerColor => Color.HSVToRGB(sliders[0].value, sliders[1].value, sliders[2].value);
        private void PickerChanged(float ignored)
        {
            if (!IsOpen) return;
            restoringDefault = false;
            if (draft.SetPaint(new TruckTaxiGaragePaintData { color = PickerColor })) { RefreshPicker(); Preview(); }
        }
        private void RefreshPicker()
        {
            Color value = PickerColor; swatch.color = value;
            colorLabel.text = "#" + ColorUtility.ToHtmlStringRGB(value);
            for (int channel = 0; channel < 3; channel++)
            {
                var pixels = new Color[256];
                for (int i = 0; i < pixels.Length; i++)
                {
                    float t = i / 255f;
                    pixels[i] = Color.HSVToRGB(channel == 0 ? t : sliders[0].value,
                        channel == 1 ? t : channel == 0 ? 1 : sliders[1].value,
                        channel == 2 ? t : channel == 0 ? 1 : sliders[2].value);
                }
                gradients[channel].SetPixels(pixels); gradients[channel].Apply(false);
            }
        }
        private void Preview()
        {
            if (!Eligible(true)) { Cancel(); return; }
            if (draft.TruckId == host.Player.DefinitionId)
            {
                if (draft.Paint != null) paint.Preview(draft.Paint);
                else
                {
                    // No custom paint means the authored/default color, except on initial open with a catalog cosmetic.
                    paint.Preview(null);
                    var saved = session.CaptureCareerProfile().trucks.Find(t => t.truckId == draft.TruckId);
                    if (!restoringDefault && saved.cosmetics.Exists(s => s.slotId == "paint")) paint.Refresh();
                }
            }
            else paint.Refresh();
            RenderPreview();
        }
        private bool restoringDefault;
        public void RestoreDefault()
        {
            if (!IsOpen) return;
            restoringDefault = true; draft.SetPaint(null); SetPicker(paint.DefaultColor); Preview();
        }
        public void Apply()
        {
            if (!IsOpen || !Eligible(true)) { Cancel(); return; }
            applying = true;
            try
            {
                if (!draft.Apply(session, catalog, host.Player.DefinitionId, out string reason)) { feedback.text = reason; return; }
                var result = host.Persistence.RequestAutosave("Truck Taxi garage customization");
                Close();
                if (!result.Succeeded) Debug.LogWarning("Garage applied to Career; autosave request failed: " + result.Message, this);
            }
            finally { applying = false; }
        }
        public void Cancel() { draft.Cancel(); Close(); }
        private bool Interrupt() { Cancel(); return true; }
        private void Close()
        {
            if (overlay != null) overlay.gameObject.SetActive(false);
            if (paint != null) paint.Refresh(); restoringDefault = false;
            foreach (var input in inputs) if (input != null) input.ReleaseDrivingInputSuppression(this);
            inputs.Clear(); session?.ReleaseOfferSuppression(this);
            var held = lease; lease = null; held?.Dispose(); runId = null;
        }
        private void CareerChanged() { if (!applying && IsOpen) Cancel(); }
        private void Subscribe() { if (session != null) { session.CareerEquipmentChanged -= CareerChanged; session.CareerEquipmentChanged += CareerChanged; } }
        private void Unsubscribe() { if (session != null) session.CareerEquipmentChanged -= CareerChanged; }
        private void OnEnable() { Subscribe(); }
        private void OnDisable() { Cancel(); Unsubscribe(); if (prompt != null) prompt.gameObject.SetActive(false); }
        private void OnDestroy() { Cancel(); Unsubscribe(); DestroyUi(); }

        private void BuildUi()
        {
            prompt = hud.Text(hud.Root, "Garage interaction", new Vector2(.25f,.23f), new Vector2(.75f,.29f), 23);
            prompt.alignment = TextAlignmentOptions.Center; prompt.gameObject.SetActive(false);
            overlay = TruckTaxiHud.Rect(hud.Root, "Truck customization garage", Vector2.zero, Vector2.one);
            overlay.gameObject.SetActive(false);
            var scrim = overlay.gameObject.AddComponent<UnityEngine.UI.Image>(); scrim.color = new Color(0,0,0,.65f);
            scrim.raycastTarget = true;
            panel = hud.Panel(overlay, "Garage controls", new Vector2(.12f,.14f), new Vector2(.88f,.87f));
            hud.Text(panel, "Title", new Vector2(.06f,.87f), new Vector2(.94f,.97f), 30).text = "TRUCK CUSTOMIZATION";
            truckName = hud.Text(panel, "Owned truck", new Vector2(.17f,.72f), new Vector2(.83f,.84f), 24);
            truckName.richText = false; truckName.alignment = TextAlignmentOptions.Center;
            previousTruck = hud.Button(panel, "<", new Vector2(.06f,.73f), new Vector2(.14f,.83f), () => SelectTruck(-1));
            nextTruck = hud.Button(panel, ">", new Vector2(.86f,.73f), new Vector2(.94f,.83f), () => SelectTruck(1));
            string[] names = { "HUE", "SATURATION", "VALUE" };
            for (int i = 0; i < 3; i++) BuildSlider(i, names[i], .56f - i * .14f);
            BuildPreview();
            swatch = TruckTaxiHud.Rect(panel, "Paint swatch", new Vector2(.77f,.26f), new Vector2(.81f,.30f)).gameObject.AddComponent<UnityEngine.UI.Image>();
            swatch.raycastTarget = false;
            colorLabel = hud.Text(panel, "RGB color", new Vector2(.83f,.25f), new Vector2(.94f,.31f), 20);
            feedback = hud.Text(panel, "Feedback", new Vector2(.06f,.15f), new Vector2(.94f,.22f), 18); feedback.richText = false;
            hud.Button(panel, "RESTORE DEFAULT", new Vector2(.06f,.04f), new Vector2(.43f,.13f), RestoreDefault);
            hud.Button(panel, "CANCEL", new Vector2(.47f,.04f), new Vector2(.68f,.13f), Cancel);
            hud.Button(panel, "APPLY", new Vector2(.72f,.04f), new Vector2(.94f,.13f), Apply);
        }
        private void BuildSlider(int channel, string label, float y)
        {
            hud.Text(panel, label, new Vector2(.06f,y), new Vector2(.28f,y+.07f), 20).text = label;
            var control = Instantiate(hud.heatSliderPrefab, panel, false);
            control.name = label;
            // Parent is inactive: turn off Heat's PlayerPrefs path before its Awake can run.
            foreach (var component in control.GetComponents<Component>())
            {
                var type = component.GetType();
                if (type.FullName != "Michsky.UI.Heat.SliderManager") continue;
                type.GetField("saveValue").SetValue(component, false);
                type.GetField("invokeOnAwake").SetValue(component, false);
                type.GetField("useSounds").SetValue(component, false);
            }
            var rect = (RectTransform)control.transform;
            rect.anchorMin = new Vector2(.30f,y+.015f); rect.anchorMax = new Vector2(.71f,y+.055f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            foreach (var text in control.GetComponentsInChildren<TMP_Text>(true)) text.enabled = false;
            foreach (var field in control.GetComponentsInChildren<TMP_InputField>(true)) field.gameObject.SetActive(false);
            var slider = control.GetComponent<UnityEngine.UI.Slider>();
            if (slider == null) throw new ArgumentException("Heat slider prefab has no root Slider.");
            sliders[channel] = slider; slider.minValue = 0; slider.maxValue = 1; slider.wholeNumbers = false;
            slider.onValueChanged.AddListener(PickerChanged);
            var rail = TruckTaxiHud.Rect(panel, label + " color spectrum", new Vector2(.30f,y-.015f), new Vector2(.71f,y));
            gradients[channel] = new Texture2D(256, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var spectrum = rail.gameObject.AddComponent<UnityEngine.UI.RawImage>(); spectrum.texture = gradients[channel]; spectrum.raycastTarget = false;
        }
        private void DestroyUi()
        {
            if (previewCamera != null) { previewCamera.targetTexture = null; DestroyOwned(previewCamera.gameObject); }
            if (previewTexture != null) { previewTexture.Release(); DestroyOwned(previewTexture); }
            previewCamera = null; previewTexture = null;
            if (overlay != null) DestroyOwned(overlay.gameObject);
            if (prompt != null) DestroyOwned(prompt.gameObject);
            foreach (var texture in gradients) DestroyOwned(texture);
            overlay = panel = null; prompt = null;
        }
        private static void DestroyOwned(UnityEngine.Object value)
        { if (value != null) { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); } }

        private void BuildPreview()
        {
            var frame = TruckTaxiHud.Rect(panel, "Canonical truck preview", new Vector2(.73f,.34f), new Vector2(.94f,.68f));
            var imageRoot = TruckTaxiHud.Rect(frame, "Image", Vector2.zero, Vector2.one);
            var image = imageRoot.gameObject.AddComponent<UnityEngine.UI.RawImage>(); image.raycastTarget = false;
            var fit = imageRoot.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            fit.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio = 5f / 3f;
            previewTexture = new RenderTexture(640, 384, 16) { name = "Garage canonical truck preview", hideFlags = HideFlags.DontSave };
            previewTexture.Create(); image.texture = previewTexture;
            var eye = new GameObject("Garage preview camera", typeof(Camera)); eye.transform.SetParent(transform, false);
            previewCamera = eye.GetComponent<Camera>(); previewCamera.enabled = false; previewCamera.targetTexture = previewTexture;
            previewCamera.cameraType = CameraType.Preview;
            previewCamera.cullingMask = 1 << LayerMask.NameToLayer("UI");
            previewCamera.clearFlags = CameraClearFlags.SolidColor; previewCamera.backgroundColor = new Color(.10f,.12f,.11f);
            previewCamera.fieldOfView = 36; previewCamera.nearClipPlane = .1f; previewCamera.farClipPlane = 150;
        }
        private void RenderPreview()
        {
            if (previewCamera == null || host.Player == null) return;
            if (paint == null || !paint.TryGetBodyBounds(out var bounds)) return;
            float distance = Mathf.Max(4, bounds.extents.magnitude / Mathf.Tan(previewCamera.fieldOfView * Mathf.Deg2Rad * .5f) * 1.1f);
            Vector3 direction = (host.Player.transform.forward + host.Player.transform.right + Vector3.up * .45f).normalized;
            previewCamera.transform.position = bounds.center + direction * distance;
            previewCamera.transform.LookAt(bounds.center);
            var originalLayers = new Dictionary<GameObject,int>();
            try
            {
                // Scope the preview draw only. No physics tick or lasting truck-layer change.
                foreach (var renderer in host.Player.GetComponentsInChildren<MeshRenderer>())
                {
                    if (!originalLayers.ContainsKey(renderer.gameObject)) originalLayers.Add(renderer.gameObject, renderer.gameObject.layer);
                    renderer.gameObject.layer = LayerMask.NameToLayer("UI");
                }
                previewCamera.Render();
            }
            finally { foreach (var item in originalLayers) if (item.Key != null) item.Key.layer = item.Value; }
        }
    }
}
