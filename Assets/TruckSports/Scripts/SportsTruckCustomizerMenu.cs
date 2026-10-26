using System;
using System.Collections.Generic;
using System.Linq;
using Michsky.UI.Heat;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LWS.TruckSports
{
    // Detached profile editing over the existing Heat UI and party preferences.
    // The caller owns entry, driving/modal suppression and return-page focus.
    public sealed class SportsTruckCustomizerMenu : MonoBehaviour
    {
        public SportsTruckAppearanceCatalog catalog;
        public TMP_Text identity, categoryHeading, pageCaption, notice, previewCaption;
        public ButtonManager[] categories;
        public ShopButtonManager[] cards;
        public ButtonManager bodyFilter, previous, next, remove, apply, cancel, rotateLeft, rotateRight, autoRotate;
        public UnityEngine.UI.RawImage previewImage;
        // Optional authored sport context. The original global Open API remains
        // context free; evaluation never applies a host or saves a draft.
        public ButtonManager sportFit;
        public TMP_Text previewControls;
        public bool IsOpen { get; private set; }
        public bool SportReportOpen { get; private set; }
        public SportsTruckCompatibilityView SportCompatibility => sportContext!=null&&draft!=null?sportContext.Read(draft):null;
        static readonly string[] Slots = { "body", "hat", "face.eyes", "face.brows", "face.mouth", "face.mustache", "face.lashes", "mirrors", "wheels", "rear.ornament", "dashboard" };
        static readonly string[] Labels = { "TRUCK BODIES", "HAT SHOP", "EYES", "EYEBROWS", "MOUTHS", "MUSTACHES", "EYELASHES", "MIRRORS", "WHEELS", "TRUCK BALLS", "DASHBOARD" };
        sealed class Choice { public string id, label; public Sprite thumbnail; }
        readonly List<Choice> choices = new List<Choice>();
        readonly SportsDeviceInput input = new SportsDeviceInput();
        SportsTruckAppearanceData draft;
        string profileId, catalogError;
        Action closed;
        GameObject previousFocus;
        Preview preview;
        bool wired, catalogReady, spinning = true;
        int category, page, filter = -1, inputAfter;
        float nextRender;
        string[] bodyCategories = Array.Empty<string>();
        SportsTruckCompatibilityContext sportContext;
        GameObject reportFocus;
        bool waitForReportRelease;

        void Wire()
        {
            if (wired) return; wired = true;
            for (int i = 0; i < categories.Length; i++) { int index = i; categories[i].onClick.AddListener(() => Browse(index)); }
            for (int i = 0; i < cards.Length; i++)
            {
                int index = i; cards[i].onClick.AddListener(() => Equip(index));
                cards[i].onPurchaseClick.AddListener(() => Equip(index));
                cards[i].purchasedButton.onClick.AddListener(() => Equip(index));
            }
            previous.onClick.AddListener(() => Step(-1)); next.onClick.AddListener(() => Step(1));
            bodyFilter.onClick.AddListener(() => { filter++; if (filter >= bodyCategories.Length) filter = -1; page = 0; Refresh(); });
            remove.onClick.AddListener(Unequip); apply.onClick.AddListener(() => Close(true)); cancel.onClick.AddListener(() => Close());
            rotateLeft.onClick.AddListener(() => Rotate(-25)); rotateRight.onClick.AddListener(() => Rotate(25));
            autoRotate.onClick.AddListener(() => { spinning = !spinning; Caption(autoRotate, spinning ? "AUTO ROTATE: ON" : "AUTO ROTATE: OFF"); });
            if(sportFit!=null)sportFit.onClick.AddListener(()=>{if(IsOpen&&!SportReportOpen&&sportContext!=null)sportContext.showDetails?.Invoke(draft.Copy(),this);});
            var trigger = previewImage.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                var drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
                drag.callback.AddListener(data => { if (IsOpen && data is PointerEventData pointer) Rotate(-pointer.delta.x * .3f); });
                trigger.triggers.Add(drag);
            }
        }
        public void Open(string participantId, Action onClosed)
        {
            OpenCore(participantId,null,onClosed);
        }
        public void OpenForSport(string participantId,SportsTruckCompatibilityContext context,Action onClosed)
        {
            OpenCore(participantId,context,onClosed);
        }
        void OpenCore(string participantId,SportsTruckCompatibilityContext context,Action onClosed)
        {
            if (IsOpen) Close();
            var initial = SportsParticipantRegistry.GetAppearance(participantId);
            if (initial == null) { onClosed?.Invoke(); return; }
            Wire(); profileId = participantId; draft = initial; closed = onClosed;sportContext=context;SportReportOpen=false;waitForReportRelease=false;reportFocus=null;
            previousFocus = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (catalog == null) catalog = Resources.Load<SportsTruckAppearanceCatalog>("SportsTruckAppearanceCatalog");
            catalogError = "Truck appearance content is unavailable.";
            catalogReady = catalog != null && catalog.Validate(out catalogError);
            bodyCategories = catalogReady ? catalog.bodies.Select(body => body.category ?? "OTHER").Distinct().ToArray() : Array.Empty<string>();
            category = page = 0; filter = -1; spinning = true; inputAfter = Time.frameCount + 1;
            IsOpen = true; gameObject.SetActive(true);
            if (catalogReady&&sportContext==null) preview = new Preview(previewImage);
            if(sportFit!=null)sportFit.gameObject.SetActive(sportContext!=null);
            Caption(autoRotate, "AUTO ROTATE: ON"); Refresh(); RebuildPreview(); Focus(categories[0].gameObject);
        }
        public void Close(bool save = false)
        {
            if (!IsOpen) return;
            if (save)
            {
                if (!catalogReady) { notice.text = catalogError; return; }
                if (!SportsParticipantRegistry.TrySetAppearance(profileId, draft) && !Equivalent(draft, SportsParticipantRegistry.GetAppearance(profileId)))
                {
                    notice.text = string.IsNullOrEmpty(SportsParticipantRegistry.Notice) ? "This truck could not be saved. Your saved appearance is unchanged." : SportsParticipantRegistry.Notice;
                    return;
                }
            }
            FinishClose();
        }
        void FinishClose()
        {
            IsOpen = false; ReleasePreview(); draft = null;sportContext=null;SportReportOpen=false;waitForReportRelease=false;reportFocus=null;
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(transform))
                EventSystem.current.SetSelectedGameObject(null);
            var callback = closed; closed = null; gameObject.SetActive(false); callback?.Invoke();
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && previousFocus != null && previousFocus.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(previousFocus);
            previousFocus = null;
        }
        void OnDisable() { if (IsOpen) FinishClose(); else ReleasePreview(); }
        void OnDestroy() { if (IsOpen) FinishClose(); else ReleasePreview(); }
        void ReleasePreview() { preview?.Dispose(); preview = null; if (previewImage != null) previewImage.texture = null; }
        static bool Equivalent(SportsTruckAppearanceData a, SportsTruckAppearanceData b)
        {
            if (a == null || b == null) return false;
            var left = a.Copy(); var right = b.Copy();
            left.selections.Sort((x, y) => StringComparer.Ordinal.Compare(x.slotId, y.slotId));
            right.selections.Sort((x, y) => StringComparer.Ordinal.Compare(x.slotId, y.slotId));
            return JsonUtility.ToJson(left) == JsonUtility.ToJson(right);
        }
        static void Caption(ButtonManager button, string text) { button.buttonText = text; button.UpdateUI(); }
        void Browse(int index) { if (!IsOpen) return; category = index; page = 0; Refresh(); RebuildPreview(); }
        void Step(int direction) { if (!IsOpen || choices.Count == 0) return; int pages = Mathf.CeilToInt(choices.Count / (float)cards.Length); page = (page + direction + pages) % pages; Refresh(); Focus(cards[0].gameObject); }
        void Rotate(float degrees) { if (!IsOpen) return; spinning = false; Caption(autoRotate, "AUTO ROTATE: OFF"); preview?.Rotate(degrees); }
        SportsTruckAppearanceCatalog.Body Body => catalogReady ? catalog.ResolveBody(draft.bodyId) : null;
        static bool WheelBindings(SportsTruckAppearanceCatalog.Body body) => body != null && body.wheelMounts != null && body.wheelMounts.Length > 0 && body.wheelRendererNames != null && body.wheelRendererNames.Length > 0;
        SportsTruckCosmeticSelection Equipped(string slot) => draft.selections.Find(item => item.slotId == slot);
        void Equip(int index)
        {
            if (!IsOpen || !catalogReady) return;
            int at = page * cards.Length + index; if (at >= choices.Count) return;
            string id = choices[at].id, slot = Slots[category];
            if (category == 0) draft.bodyId = id;
            else
            {
                var equipped = Equipped(slot);
                if (equipped == null) draft.selections.Add(new SportsTruckCosmeticSelection { slotId = slot, itemId = id });
                else if (equipped.itemId != id) { equipped.itemId = id; equipped.variantId = ""; }
            }
            Refresh(); RebuildPreview(); Focus(cards[index].gameObject);
        }
        void Unequip()
        {
            if (!IsOpen || category == 0) return;
            draft.selections.RemoveAll(item => item.slotId == Slots[category]); Refresh(); RebuildPreview(); Focus(categories[category].gameObject);
        }
        void Refresh()
        {
            var participant = SportsParticipantRegistry.Find(profileId);
            identity.text = (participant?.displayName ?? "Driver") + "  /  ID " + profileId.Substring(0, Mathf.Min(8, profileId.Length));
            categoryHeading.text = Labels[category]; choices.Clear(); var body = Body;
            bodyFilter.gameObject.SetActive(category == 0 && catalogReady);
            Caption(bodyFilter, filter < 0 ? "ALL BODY STYLES" : bodyCategories[filter]);
            if (catalogReady && category == 0)
                foreach (var item in catalog.bodies)
                    if (item.visualPrefab != null && (filter < 0 || (item.category ?? "OTHER") == bodyCategories[filter])) choices.Add(new Choice { id = item.id, label = item.label, thumbnail = item.thumbnail });
            if (catalogReady && category != 0 && body != null && (category == 8 ? WheelBindings(body) : (body.mounts ?? Array.Empty<SportsTruckAppearanceCatalog.Mount>()).Any(mount => mount.slotId == Slots[category])))
                foreach (var item in catalog.cosmetics ?? Array.Empty<SportsTruckAppearanceCatalog.Cosmetic>())
                    if (item.slotId == Slots[category] && item.visualPrefab != null && item.CompatibleWith(body.id)) choices.Add(new Choice { id = item.id, label = item.label, thumbnail = item.thumbnail });
            int pages = Mathf.Max(1, Mathf.CeilToInt(choices.Count / (float)cards.Length)); page = Mathf.Clamp(page, 0, pages - 1);
            string selected = category == 0 ? string.IsNullOrEmpty(draft.bodyId) ? body?.id : draft.bodyId : Equipped(Slots[category])?.itemId;
            for (int i = 0; i < cards.Length; i++)
            {
                int at = page * cards.Length + i; var card = cards[i]; card.gameObject.SetActive(at < choices.Count);
                if (at >= choices.Count) continue;
                var choice = choices[at]; card.buttonTitle = choice.label; card.buttonDescription = "";
                card.buttonIcon = choice.thumbnail; card.enableIcon = choice.thumbnail != null;
                card.SetState(selected == choice.id ? ShopButtonManager.State.Purchased : ShopButtonManager.State.Default); card.UpdateUI();
            }
            pageCaption.text = choices.Count == 0 ? "NO COMPATIBLE ITEMS AVAILABLE" : "PAGE " + (page + 1) + " OF " + pages;
            previous.gameObject.SetActive(pages > 1); next.gameObject.SetActive(pages > 1);
            remove.gameObject.SetActive(category != 0 && Equipped(Slots[category]) != null);
            apply.isInteractable = catalogReady; var applySelectable = apply.GetComponent<UnityEngine.UI.Selectable>(); if (applySelectable != null) applySelectable.interactable = catalogReady;
            notice.text = !catalogReady ? catalogError : choices.Count == 0 ? "No compatible " + Labels[category].ToLowerInvariant() + " are available for this body. Saved choices are retained."
                : "Preview your truck, then SAVE TRUCK to confirm. CANCEL keeps your saved truck.";
            if (catalogReady && category == 8 && !WheelBindings(body)) notice.text = "This body has no authored wheel attachment points. Saved wheel choices are retained.";
            if (catalogReady && !string.IsNullOrEmpty(draft.bodyId) && catalog.FindBody(draft.bodyId) == null) notice.text += " Saved body unavailable; showing " + body.label + " temporarily.";
            for (int i = 0; i < categories.Length; i++) if (categories[i].normalTextObj != null) categories[i].normalTextObj.color = i == category ? SportsBroadcastCanvas.Orange : SportsBroadcastCanvas.Cream;
            Navigation();
        }
        void Navigation()
        {
            var items = GetComponentsInChildren<UnityEngine.UI.Selectable>(false).Where(item => item.IsInteractable()).ToArray();
            for (int i = 0; i < items.Length; i++)
            {
                var up = items[(i + items.Length - 1) % items.Length]; var down = items[(i + 1) % items.Length];
                var nav = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit, selectOnUp = up, selectOnDown = down, selectOnLeft = up, selectOnRight = down };
                items[i].navigation = nav;
                var button = items[i].GetComponent<ButtonManager>();
                if (button != null) { button.useUINavigation = true; button.navigationMode = nav.mode; button.selectOnUp = button.selectOnLeft = up.gameObject; button.selectOnDown = button.selectOnRight = down.gameObject; }
                var shop = items[i].GetComponent<ShopButtonManager>();
                if (shop != null) { shop.useUINavigation = true; shop.navigationMode = nav.mode; shop.selectOnUp = shop.selectOnLeft = up.gameObject; shop.selectOnDown = shop.selectOnRight = down.gameObject; }
            }
        }
        static void Focus(GameObject target) { if (EventSystem.current != null && target != null && target.activeInHierarchy) EventSystem.current.SetSelectedGameObject(target); }
        void RebuildPreview()
        {
            var fit=SportCompatibility;
            bool supported=fit==null||fit.canApply&&!fit.bodyFallback&&fit.unavailable==0;
            previewImage.gameObject.SetActive(supported);rotateLeft.gameObject.SetActive(supported);rotateRight.gameObject.SetActive(supported);autoRotate.gameObject.SetActive(supported);if(previewControls!=null)previewControls.gameObject.SetActive(supported);
            if(!supported)
            {
                ReleasePreview();previewCaption.text=!fit.canApply?"ORIGINAL SPORT APPEARANCE":fit.bodyFallback?"COMPATIBLE BODY USED HERE":"SAVED ITEMS NOT APPLIED HERE";Navigation();return;
            }
            if(preview==null&&catalogReady&&IsOpen)preview=new Preview(previewImage);
            if (preview == null || Body == null) return;
            int missing = preview.Compose(catalog, Body, draft, SportsParticipantRegistry.Find(profileId).color, category == 10);
            previewCaption.text = category == 10 && Equipped("dashboard") != null ? "DASHBOARD ITEM PREVIEW" : Body.label.ToUpperInvariant();
            if(fit!=null)previewCaption.text="SUPPORTED HERE · "+previewCaption.text;
            if (missing > 0) notice.text += " " + missing + " saved accessories are unavailable for this body; selections are retained.";
        }
        void Update()
        {
            if (!IsOpen||SportReportOpen) return;
            if(waitForReportRelease){if(ReportInputHeld())return;waitForReportRelease=false;inputAfter=Time.frameCount+1;}
            input.Sample(); if (Time.frameCount <= inputAfter) return;
            bool back = SportsDeviceInput.KeyDown(Key.Escape); foreach (var pad in Gamepad.all) back |= input.Down(pad, 8);
            if (back) { Close(); return; }
            float turn = (SportsDeviceInput.KeyHeld(Key.E) ? 1 : 0) - (SportsDeviceInput.KeyHeld(Key.Q) ? 1 : 0);
            foreach (var pad in Gamepad.all) if (Mathf.Abs(pad.rightStick.ReadValue().x) > .2f) { turn = pad.rightStick.ReadValue().x; break; }
            if (Mathf.Abs(turn) > .1f) Rotate(turn * Time.unscaledDeltaTime * 90);
            if (preview != null && Time.unscaledTime >= nextRender) { nextRender = Time.unscaledTime + 1f / 24; if (spinning) preview.Rotate(Time.unscaledDeltaTime * 22); else preview.Render(); }
        }
        public void SetSportReportOpen(bool value)
        {
            if(SportReportOpen==value)return;SportReportOpen=value;
            if(value)reportFocus=EventSystem.current!=null?EventSystem.current.currentSelectedGameObject:null;
            else{waitForReportRelease=true;inputAfter=Time.frameCount+1;Focus(reportFocus);reportFocus=null;}
        }
        static bool ReportInputHeld()
        {
            if(SportsDeviceInput.KeyHeld(Key.Escape)||SportsDeviceInput.KeyHeld(Key.Enter)||SportsDeviceInput.KeyHeld(Key.Space)||Mouse.current!=null&&Mouse.current.leftButton.isPressed)return true;
            foreach(var pad in Gamepad.all)if(pad.buttonSouth.isPressed||pad.buttonEast.isPressed||pad.startButton.isPressed||pad.dpad.ReadValue().sqrMagnitude>.1f)return true;return false;
        }

        sealed class Preview : IDisposable
        {
            readonly GameObject stage;
            readonly Camera camera;
            readonly Light key, fill;
            readonly RenderTexture texture;
            readonly UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest request;
            GameObject model;
            Renderer[] renderers = Array.Empty<Renderer>(), bodyRenderers = Array.Empty<Renderer>();
            bool[] bodyEnabled = Array.Empty<bool>();
            bool dashboard;
            float yaw = 35;
            public Preview(UnityEngine.UI.RawImage image)
            {
                texture = new RenderTexture(768, 576, 24) { name = "Sports customizer preview", hideFlags = HideFlags.DontSave }; texture.Create(); image.texture = texture;
                request = new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = texture };
                stage = new GameObject("Sports customizer render-only stage") { hideFlags = HideFlags.DontSave }; stage.transform.position = new Vector3(0, -20000, 0);
                var eye = new GameObject("Customizer preview camera"); eye.transform.SetParent(stage.transform, false); camera = eye.AddComponent<Camera>();
                camera.enabled = false; camera.cameraType = CameraType.Preview; camera.targetTexture = texture; camera.cullingMask = 1 << 5;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = SportsBroadcastCanvas.Ink; camera.fieldOfView = 36; camera.nearClipPlane = .01f;
                key = CreateLight("Preview key", new Vector3(35, -35, 0), 1.4f); fill = CreateLight("Preview fill", new Vector3(20, 145, 0), .7f);
            }
            Light CreateLight(string name, Vector3 angles, float intensity)
            {
                var root = new GameObject(name); root.transform.SetParent(stage.transform, false); root.transform.localRotation = Quaternion.Euler(angles);
                var light = root.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = intensity; light.shadows = LightShadows.None; light.cullingMask = 1 << 5; light.enabled = false; return light;
            }
            public int Compose(SportsTruckAppearanceCatalog catalog, SportsTruckAppearanceCatalog.Body body, SportsTruckAppearanceData draft, Color color, bool inspectDashboard)
            {
                if (model != null) { model.SetActive(false); Destroy(model); }
                model = new GameObject("Customized truck preview"); model.transform.SetParent(stage.transform, false); model.SetActive(false);
                var presentation = new GameObject("Canonical body presentation"); presentation.transform.SetParent(model.transform, false); presentation.transform.localRotation = Quaternion.Euler(body.presentationEuler);
                var bodyObject = UnityEngine.Object.Instantiate(body.visualPrefab, presentation.transform, false); bodyRenderers = bodyObject.GetComponentsInChildren<Renderer>(true);
                bodyEnabled = new bool[bodyRenderers.Length];
                var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor", color); block.SetColor("_Color", color);
                foreach (var renderer in bodyRenderers)
                    for (int index = 0; index < renderer.sharedMaterials.Length; index++)
                        if (SportsTruckAppearanceCompatibility.IsPaintMaterial(body.id, renderer.sharedMaterials[index])) renderer.SetPropertyBlock(block, index);
                int missing = 0; var dashboardParts = new List<Renderer>();
                foreach (var selected in draft.selections)
                {
                    var item = catalog.FindCosmetic(selected.itemId, selected.slotId, body.id);
                    if (selected.slotId == "wheels")
                    {
                        if (item == null || !WheelBindings(body)) { missing++; continue; }
                        foreach (var wheelMount in body.wheelMounts) Attach(item, wheelMount);
                        var originals = new HashSet<string>(body.wheelRendererNames, StringComparer.Ordinal);
                        foreach (var renderer in bodyRenderers) if (originals.Contains(renderer.name)) renderer.enabled = false;
                        continue;
                    }
                    var mount = (body.mounts ?? Array.Empty<SportsTruckAppearanceCatalog.Mount>()).FirstOrDefault(value => value.slotId == selected.slotId);
                    if (item == null || mount == null) { missing++; continue; }
                    var part = Attach(item, mount);
                    if (selected.slotId == "dashboard") dashboardParts.AddRange(part.GetComponentsInChildren<Renderer>(true));
                }
                GameObject Attach(SportsTruckAppearanceCatalog.Cosmetic item, SportsTruckAppearanceCatalog.Mount mount)
                {
                    var anchor = new GameObject("Cosmetic mount " + mount.slotId).transform; anchor.SetParent(presentation.transform, false);
                    anchor.localPosition = mount.position; anchor.localRotation = Quaternion.Euler(mount.euler); anchor.localScale = mount.scale;
                    var part = UnityEngine.Object.Instantiate(item.visualPrefab, anchor, false); part.transform.localPosition = item.position; part.transform.localRotation = Quaternion.Euler(item.euler); part.transform.localScale = item.scale;
                    return part;
                }
                foreach (var node in model.GetComponentsInChildren<Transform>(true)) node.gameObject.layer = 5;
                dashboard = inspectDashboard && dashboardParts.Count > 0;
                renderers = dashboard ? dashboardParts.ToArray() : model.GetComponentsInChildren<Renderer>(true); Render(); return missing;
            }
            public void Rotate(float amount) { yaw = Mathf.Repeat(yaw + amount, 360); Render(); }
            public void Render()
            {
                if (model == null) return;
                model.SetActive(true);
                try
                {
                    if (dashboard) for (int i = 0; i < bodyRenderers.Length; i++) { bodyEnabled[i] = bodyRenderers[i].enabled; bodyRenderers[i].enabled = false; }
                    Bounds bounds = default; bool found = false;
                    foreach (var renderer in renderers) if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy) { if (!found) { bounds = renderer.bounds; found = true; } else bounds.Encapsulate(renderer.bounds); }
                    if (!found) return;
                    float fov = camera.fieldOfView * Mathf.Deg2Rad * .5f;
                    float distance = Mathf.Max(.2f, bounds.extents.magnitude / Mathf.Sin(fov) * 1.12f);
                    camera.transform.position = bounds.center + Quaternion.Euler(0, yaw, 0) * new Vector3(0, .3f, 1).normalized * distance;
                    camera.transform.LookAt(bounds.center); camera.farClipPlane = Mathf.Max(10, distance + bounds.size.magnitude * 2);
                    key.enabled = fill.enabled = true;
                    if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null) camera.Render(); else UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera, request);
                }
                finally { key.enabled = fill.enabled = false; if (dashboard) for (int i = 0; i < bodyRenderers.Length; i++) bodyRenderers[i].enabled = bodyEnabled[i]; model.SetActive(false); }
            }
            public void Dispose() { camera.targetTexture = null; texture.Release(); Destroy(texture); if (model != null) model.SetActive(false); Destroy(stage); }
            static void Destroy(UnityEngine.Object value) { if (value == null) return; if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value); }
        }
    }
}
