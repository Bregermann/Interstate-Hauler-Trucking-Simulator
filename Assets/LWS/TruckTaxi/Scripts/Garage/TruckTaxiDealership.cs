using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using TMPro;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiDealership : MonoBehaviour
    {
        private TruckTaxiBootstrap host;
        private TruckTaxiHud hud;
        private TruckTaxiHudModalBridge bridge;
        private TruckTaxiTruckFactory factory;
        private TruckTaxiSession session;
        private RectTransform overlay, panel;
        private TextMeshProUGUI wallet, heading, details, feedback;
        private TextMeshProUGUI[] statuses;
        private GameObject purchase, equip;
        private TruckTaxiCareerTruckDefinition[] entries;
        private TruckTaxiModelPreview preview;
        private readonly List<LwsKeyboardGamepadTruckInputSource> inputs = new List<LwsKeyboardGamepadTruckInputSource>();
        private IDisposable lease;
        private string runId;
        private int selectedIndex;
        private bool transacting;
        public bool IsOpen => lease != null;
        public Transform FocusRoot => panel;

        public void Initialize(TruckTaxiBootstrap owner, TruckTaxiHud view)
        {
            if (owner == null || !owner.Ready || owner.Player == null || owner.Session == null ||
                owner.TruckFactory == null || view == null || view.Root == null || view.UIInput == null || view.heatButtonPrefab == null)
                throw new ArgumentException("Dealership requires a ready host, truck factory and shared HUD.");
            Close(); Unsubscribe(); DestroyUi();
            host = owner; hud = view; factory = host.TruckFactory; session = host.Session;
            bridge = hud.GetComponent<TruckTaxiHudModalBridge>() ?? hud.gameObject.AddComponent<TruckTaxiHudModalBridge>();
            bridge.Initialize(host, hud);
            BuildUi(); Subscribe();
        }

        private bool Eligible(bool editing)
        {
            if (!isActiveAndEnabled || host == null || !host.Ready || host.Player == null || host.Session != session ||
                host.TruckFactory != factory || factory == null || factory.Catalog == null ||
                host.Persistence == null || !host.Persistence.HasActiveRun || host.Persistence.ResumeBlocked ||
                host.Persistence.IsBusy || session.CareerRunProfileId != host.Persistence.RunProfileId ||
                session.State != TruckTaxiState.Available || bridge == null) return false;
            if (editing ? !bridge.IsOwnedBy(this) || runId != host.Persistence.RunProfileId : !bridge.CanAcquire) return false;
            return factory.CanSwitch(out _, editing);
        }

        // Regional facilities may share the same catalog and modal entry point.
        public bool TryOpen()
        {
            if (IsOpen || panel == null || !Eligible(false) || !factory.Catalog.Validate(out _) || entries.Length == 0) return false;
            lease = bridge.TryAcquire(this, panel, Close, Interrupt);
            if (lease == null) return false;
            runId = host.Persistence.RunProfileId;
            session.AcquireOfferSuppression(this); SuppressCurrentTruck();
            selectedIndex = Array.FindIndex(entries, t => t.StableId == session.EquippedTruckId);
            if (selectedIndex < 0) selectedIndex = 0;
            overlay.gameObject.SetActive(true); overlay.SetAsLastSibling();
            feedback.text = string.Empty;
            try { ShowSelection(); hud.UIInput.Focus(panel); }
            catch { Close(); throw; }
            return true;
        }

        private void Update()
        {
            if (IsOpen && (!overlay.gameObject.activeInHierarchy || !bridge.IsOpen || !Eligible(true))) Close();
        }

        private void Select(int index)
        {
            if (!IsOpen || transacting || index < 0 || index >= entries.Length) return;
            selectedIndex = index; feedback.text = string.Empty; ShowSelection();
        }

        private void ShowSelection()
        {
            var entry = entries[selectedIndex];
            heading.text = entry.DisplayName;
            var model = entry.truck != null && entry.truck.PlayerPrefab != null
                ? entry.truck.PlayerPrefab.GetComponent<TruckTaxiTruckModel>() : null;
            details.text = (entry.description ?? string.Empty) + "\n" +
                (model != null ? (model.hasSleeper ? "SLEEPER CAB" : "DAY CAB") + "\nENGINE  " +
                model.engineDescription + "\nHANDLING  " + model.handlingDescription : "Model configuration unavailable.");
            if (!preview.Show(factory, entry.StableId, model?.primaryPaintMaterials))
                feedback.text = "Real-model preview unavailable for this truck.";
            preview.Tint(null);
            var owned = session.CaptureCareerProfile()?.trucks.Find(t => t.truckId == entry.StableId);
            if (owned?.hasCustomPaint == true) preview.Tint(owned.paint);
            else if (owned?.cosmetics != null)
                foreach (var selection in owned.cosmetics)
                {
                    var cosmetic = factory.Catalog.FindCosmetic(selection.itemId);
                    if (selection.slotId == "paint" && cosmetic?.category == TruckTaxiCosmeticCategory.Paint &&
                        cosmetic.HasContent && TruckTaxiCareerCatalog.Compatible(cosmetic.compatibleTruckIds, entry.StableId))
                        preview.Tint(new TruckTaxiGaragePaintData { color = cosmetic.paintColor });
                }
            preview.Render(); RefreshState();
        }

        private void RefreshState()
        {
            if (!IsOpen) return;
            var profile = session.CaptureCareerProfile();
            wallet.text = "CASH  " + TruckTaxiHud.Money(session.WalletBalanceCents);
            for (int i = 0; i < entries.Length; i++)
                statuses[i].text = (i == selectedIndex ? "> " : string.Empty) +
                    Status(entries[i], profile, session.WalletBalanceCents) + "  |  " + TruckTaxiHud.Money(entries[i].priceCents);
            var entry = entries[selectedIndex];
            bool owned = profile?.trucks.Exists(t => t.truckId == entry.StableId) == true;
            bool current = profile?.equippedTruckId == entry.StableId;
            SetEnabled(purchase, !transacting && !owned && entry.availableForPurchase && entry.HasContent &&
                session.WalletBalanceCents >= entry.priceCents);
            SetEnabled(equip, !transacting && owned && !current && entry.HasContent);
            hud.UIInput.Focus(panel);
        }

        public static string Status(TruckTaxiCareerTruckDefinition entry, TruckTaxiCareerProfileData profile, long cash)
        {
            if (entry == null) return "UNAVAILABLE";
            if (profile?.equippedTruckId == entry.StableId) return "EQUIPPED";
            if (profile?.trucks.Exists(t => t.truckId == entry.StableId) == true) return "OWNED";
            if (!entry.HasContent || !entry.availableForPurchase) return "UNAVAILABLE";
            return cash < entry.priceCents ? "NEED CASH" : "FOR SALE";
        }

        private void Purchase()
        {
            if (!IsOpen || transacting || !Eligible(true)) return;
            transacting = true;
            try
            {
                if (!session.TryPurchaseTruck(factory.Catalog, entries[selectedIndex].StableId, out string reason))
                { feedback.text = reason; return; }
                feedback.text = "PURCHASED  " + entries[selectedIndex].DisplayName;
                Save("Truck Taxi dealership purchase");
            }
            finally { transacting = false; RefreshState(); }
        }

        private void Equip()
        {
            if (!IsOpen || transacting || !Eligible(true)) return;
            transacting = true;
            try
            {
                if (!factory.TryEquip(entries[selectedIndex].StableId, out string reason, true))
                { feedback.text = reason; return; }
                SuppressCurrentTruck();
                feedback.text = "EQUIPPED  " + entries[selectedIndex].DisplayName;
                Save("Truck Taxi dealership equip");
            }
            finally { transacting = false; RefreshState(); }
        }

        private void Save(string reason)
        {
            var result = host.Persistence.RequestAutosave(reason);
            if (!result.Succeeded) feedback.text += "\nCareer updated; autosave failed: " + result.Message;
        }

        private void SuppressCurrentTruck()
        {
            if (host?.Player == null) return;
            foreach (var input in host.Player.GetComponentsInChildren<LwsKeyboardGamepadTruckInputSource>(true))
                if (!inputs.Contains(input)) { input.AcquireDrivingInputSuppression(this); inputs.Add(input); }
        }
        private void ModelChanged()
        {
            if (!IsOpen) return;
            SuppressCurrentTruck();
            if (!transacting) ShowSelection();
        }
        private void CareerChanged() { if (IsOpen && !transacting) RefreshState(); }
        private bool Interrupt() { Close(); return true; }
        public void Close()
        {
            if (overlay != null) overlay.gameObject.SetActive(false);
            preview?.Clear();
            foreach (var input in inputs) if (input != null) input.ReleaseDrivingInputSuppression(this);
            inputs.Clear(); session?.ReleaseOfferSuppression(this);
            var held = lease; lease = null; held?.Dispose(); runId = null;
        }
        private void Subscribe()
        {
            Unsubscribe();
            if (session != null) session.CareerEquipmentChanged += CareerChanged;
            if (factory != null) factory.ModelChanged += ModelChanged;
        }
        private void Unsubscribe()
        {
            if (session != null) session.CareerEquipmentChanged -= CareerChanged;
            if (factory != null) factory.ModelChanged -= ModelChanged;
        }
        private void OnEnable() { Subscribe(); }
        private void OnDisable() { Close(); Unsubscribe(); }
        private void OnDestroy() { Close(); Unsubscribe(); DestroyUi(); }

        private void BuildUi()
        {
            entries = Array.FindAll(factory.Catalog?.trucks ?? Array.Empty<TruckTaxiCareerTruckDefinition>(), entry => entry != null);
            overlay = TruckTaxiHud.Rect(hud.Root, "Truck Taxi dealership", Vector2.zero, Vector2.one);
            overlay.gameObject.SetActive(false);
            var scrim = overlay.gameObject.AddComponent<UnityEngine.UI.Image>();
            scrim.color = new Color(0, 0, 0, .7f); scrim.raycastTarget = true;
            panel = hud.Panel(overlay, "Dealership controls", new Vector2(.04f, .06f), new Vector2(.96f, .94f));
            panel.GetComponent<UnityEngine.UI.Image>().color=new Color(.045f,.055f,.06f,1);
            Label("Title", new Vector2(.025f, .9f), new Vector2(.4f, .98f), 30).text = "TRUCK DEALERSHIP";
            wallet = Label("Cash", new Vector2(.55f, .91f), new Vector2(.97f, .98f), 24);
            wallet.alignment = TextAlignmentOptions.Right;
            statuses = new TextMeshProUGUI[entries.Length];
            float rowHeight = .66f / Mathf.Max(6, entries.Length);
            for (int i = 0; i < entries.Length; i++)
            {
                int index = i; float top = .87f - i * rowHeight;
                hud.Button(panel, entries[i].DisplayName, new Vector2(.025f, top - rowHeight * .59f),
                    new Vector2(.35f, top), () => Select(index));
                statuses[i] = Label("Truck status " + i, new Vector2(.03f, top - rowHeight + .008f),
                    new Vector2(.35f, top - rowHeight * .68f), 19);
            }
            heading = Label("Selected truck", new Vector2(.4f, .82f), new Vector2(.97f, .9f), 28);
            var frame = TruckTaxiHud.Rect(panel, "Real model", new Vector2(.4f, .47f), new Vector2(.97f, .81f));
            preview = new TruckTaxiModelPreview(frame, transform);
            hud.Button(panel, "<", new Vector2(.4f, .405f), new Vector2(.49f, .465f), () => preview.Rotate(-45));
            hud.Button(panel, "FRONT", new Vector2(.50f, .405f), new Vector2(.64f, .465f), () => preview.SetAngle(0));
            hud.Button(panel, "SIDE", new Vector2(.65f, .405f), new Vector2(.8f, .465f), () => preview.SetAngle(90));
            hud.Button(panel, "REAR", new Vector2(.81f, .405f), new Vector2(.95f, .465f), () => preview.SetAngle(180));
            hud.Button(panel, ">", new Vector2(.955f, .405f), new Vector2(.995f, .465f), () => preview.Rotate(45));
            details = Label("Model configuration", new Vector2(.4f, .19f), new Vector2(.97f, .395f), 23);
            details.overflowMode = TextOverflowModes.Truncate;
            feedback = Label("Feedback", new Vector2(.025f, .125f), new Vector2(.97f, .19f), 20);
            purchase = hud.Button(panel, "PURCHASE", new Vector2(.4f, .025f), new Vector2(.59f, .115f), Purchase);
            equip = hud.Button(panel, "EQUIP", new Vector2(.61f, .025f), new Vector2(.78f, .115f), Equip);
            hud.Button(panel, "BACK", new Vector2(.80f, .025f), new Vector2(.97f, .115f), Close);
        }
        private TextMeshProUGUI Label(string name, Vector2 min, Vector2 max, float size)
        {
            var text = hud.Text(panel, name, min, max, size);
            text.richText = false; text.fontSizeMin = 16; return text;
        }
        private static void SetEnabled(GameObject button, bool enabled)
        {
            // Match both Heat's click authority and Unity's navigation authority.
            foreach (var component in button.GetComponents<Component>())
                if (component.GetType().FullName == "Michsky.UI.Heat.ButtonManager")
                    component.GetType().GetMethod("Interactable").Invoke(component, new object[] { enabled });
            var selectable = button.GetComponent<UnityEngine.UI.Selectable>();
            if (selectable != null) selectable.interactable = enabled;
        }
        private void DestroyUi()
        {
            preview?.Dispose(); preview = null;
            if (overlay != null) TruckTaxiModelPreview.DestroyOwned(overlay.gameObject);
            overlay = panel = null;
        }
    }

    // One presentation-only tree per open view. Never instantiate a player prefab here.
    internal sealed class TruckTaxiModelPreview : IDisposable
    {
        private sealed class PaintSlot
        {
            public Renderer renderer;
            public int index, property;
            public MaterialPropertyBlock original;
        }
        private readonly GameObject stage;
        private readonly Camera camera;
        private readonly Light key, fill;
        private readonly RenderTexture texture;
        private readonly UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest renderRequest;
        private readonly List<PaintSlot> paintSlots = new List<PaintSlot>();
        private readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
        private GameObject model;
        private Renderer[] renderers = Array.Empty<Renderer>();
        private string stableId;
        private float yaw = 35;
        public Color DefaultColor { get; private set; } = Color.white;

        public TruckTaxiModelPreview(RectTransform frame, Transform owner)
        {
            var imageRoot = TruckTaxiHud.Rect(frame, "Model image", Vector2.zero, Vector2.one);
            var image = imageRoot.gameObject.AddComponent<UnityEngine.UI.RawImage>(); image.raycastTarget = false;
            var fit = imageRoot.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            fit.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio = 16f / 9f;
            texture = new RenderTexture(768, 432, 24) { name = "Truck Taxi model preview", hideFlags = HideFlags.DontSave };
            texture.Create(); image.texture = texture;
            renderRequest = new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = texture };
            stage = new GameObject("Truck Taxi visual-only preview") { hideFlags = HideFlags.DontSave };
            stage.transform.SetParent(owner, false); stage.transform.position = new Vector3(0, -20000, 0);
            var eye = new GameObject("Preview camera", typeof(Camera)); eye.transform.SetParent(stage.transform, false);
            camera = eye.GetComponent<Camera>(); camera.enabled = false; camera.targetTexture = texture;
            camera.cameraType = CameraType.Preview; camera.cullingMask = 1 << LayerMask.NameToLayer("UI");
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.10f, .12f, .11f);
            camera.fieldOfView = 36; camera.nearClipPlane = .1f; camera.farClipPlane = 500;
            key = CreateLight("Preview key", new Vector3(35, -35, 0), 1.4f);
            fill = CreateLight("Preview fill", new Vector3(20, 145, 0), .65f);
        }

        private Light CreateLight(string name, Vector3 angles, float intensity)
        {
            var go = new GameObject(name, typeof(Light)); go.transform.SetParent(stage.transform, false);
            go.transform.rotation = Quaternion.Euler(angles);
            var light = go.GetComponent<Light>(); light.type = LightType.Directional;
            light.intensity = intensity; light.shadows = LightShadows.None;
            light.cullingMask = camera.cullingMask; light.enabled = false;
            return light;
        }

        public bool Show(TruckTaxiTruckFactory factory, string id, Material[] paintMaterials)
        {
            if (model != null && stableId == id) return true;
            Clear();
            if (factory == null || string.IsNullOrEmpty(id)) return false;
            model = factory.CreatePreview(id, stage.transform);
            if (model == null) { Render(); return false; }
            stableId = id; model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            renderers = model.GetComponentsInChildren<Renderer>(true);
            foreach (var node in model.GetComponentsInChildren<Transform>(true)) node.gameObject.layer = LayerMask.NameToLayer("UI");
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null || paintMaterials == null || Array.IndexOf(paintMaterials, material) < 0) continue;
                    string property = material.HasProperty("_BaseColor") ? "_BaseColor" : material.HasProperty("_Color") ? "_Color" : null;
                    if (property == null) continue;
                    var original = new MaterialPropertyBlock(); renderer.GetPropertyBlock(original, i);
                    if (original.isEmpty) renderer.GetPropertyBlock(original);
                    int propertyId = Shader.PropertyToID(property);
                    if (paintSlots.Count == 0)
                        DefaultColor = original.HasColor(propertyId) ? original.GetColor(propertyId) : material.GetColor(propertyId);
                    paintSlots.Add(new PaintSlot { renderer = renderer, index = i, property = propertyId, original = original });
                }
            }
            // Keep it out of all gameplay cameras between explicit preview draws.
            model.SetActive(false);
            return renderers.Length > 0;
        }

        public void Tint(TruckTaxiGaragePaintData paint)
        {
            if (paint != null && !paint.Validate()) return;
            foreach (var slot in paintSlots)
            {
                if (slot.renderer == null) continue;
                if (paint == null) slot.renderer.SetPropertyBlock(slot.original, slot.index);
                else
                {
                    slot.renderer.GetPropertyBlock(block, slot.index);
                    block.SetColor(slot.property, paint.color);
                    slot.renderer.SetPropertyBlock(block, slot.index); block.Clear();
                }
            }
        }

        public void Rotate(float degrees) { SetAngle(yaw + degrees); }
        public void SetAngle(float degrees) { yaw = Mathf.Repeat(degrees, 360); Render(); }
        public void Render()
        {
            if (camera == null) return;
            if (model == null) { RenderCamera(); return; }
            model.SetActive(true);
            try
            {
                Bounds bounds = default; bool found = false;
                foreach (var renderer in renderers)
                {
                    if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                    if (!found) { bounds = renderer.bounds; found = true; } else bounds.Encapsulate(renderer.bounds);
                }
                if (!found) { model.SetActive(false); RenderCamera(); return; }
                float halfFov = camera.fieldOfView * Mathf.Deg2Rad * .5f;
                float limitingFov = Mathf.Min(halfFov, Mathf.Atan(Mathf.Tan(halfFov) * texture.width / texture.height));
                float distance = Mathf.Max(2, bounds.extents.magnitude / Mathf.Sin(limitingFov) * 1.12f);
                Vector3 direction = Quaternion.Euler(0, yaw, 0) * new Vector3(0, .3f, 1).normalized;
                camera.transform.position = bounds.center + direction * distance;
                camera.transform.LookAt(bounds.center);
                camera.farClipPlane = Mathf.Max(100, distance + bounds.size.magnitude * 2);
                key.enabled = fill.enabled = true;
                RenderCamera();
            }
            finally { key.enabled = fill.enabled = false; if (model != null) model.SetActive(false); }
        }

        private void RenderCamera()
        {
            if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null) camera.Render();
            else UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera, renderRequest);
        }

        public void Clear()
        {
            if (model != null) { model.SetActive(false); DestroyOwned(model); }
            model = null; stableId = null; paintSlots.Clear(); renderers = Array.Empty<Renderer>(); DefaultColor = Color.white;
        }
        public void Dispose()
        {
            Clear();
            if (camera != null) camera.targetTexture = null;
            if (texture != null) { texture.Release(); DestroyOwned(texture); }
            DestroyOwned(stage);
        }
        internal static void DestroyOwned(UnityEngine.Object value)
        { if (value != null) { if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value); } }
    }
}
