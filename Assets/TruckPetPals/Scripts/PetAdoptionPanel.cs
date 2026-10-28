using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Michsky.UI.Heat;
using LWS.TruckCamping;
using LWS.TruckSports;
namespace LWS.TruckPetPals
{
    // Production Heat/uGUI presentation, with a detached adoption draft. The
    // preview contains rendering/animation only, no owner vehicle or pet motor.
    [DefaultExecutionOrder(-12000)]
    public sealed class PetAdoptionPanel : MonoBehaviour
    {
        public PetPalsMatch match;
        public GameObject panel;
        public TMP_InputField petName;
        public TMP_Text title, description, bodyLabel, accessoryLabel, message;
        public RawImage preview;
        public ButtonManager apply, cancel, nextType, nextBody, nextAccessory;
        public GameObject[] renderTemplates;
        public bool IsOpen { get; private set; }
        public string DraftPetId => draft == null ? "" : draft.id;
        readonly SportsDeviceInput input = new SportsDeviceInput();
        readonly System.Collections.Generic.Dictionary<Camera, int> masks = new System.Collections.Generic.Dictionary<Camera, int>();
        static readonly string[] Accessories = { "collar.teal", "collar.sun", "bandana.rose" };
        PetRecord draft;
        int owner, reenableFrame = -1;
        bool originalMenuEnabled;
        GameObject originalFocus, stage, art;
        Camera camera;
        RenderTexture texture;
        SportsTruckAppearanceCatalog catalog;
        void OnEnable() { if (match != null) match.AdoptionRequested += Open; }
        void Start()
        {
            apply.onClick.AddListener(Apply); cancel.onClick.AddListener(Close); nextType.onClick.AddListener(CycleType); nextBody.onClick.AddListener(CycleBody); nextAccessory.onClick.AddListener(CycleAccessory);
            if (!IsOpen) panel.SetActive(false);
        }
        public void Open(int slot)
        {
            if (match.Live || slot < 0 || slot >= match.setup.count) throw new InvalidOperationException("Adoption opens only in Setup/Pause for an actual owner.");
            if (IsOpen) Close(); owner = slot;
            if (reenableFrame >= 0) { match.menus.enabled = originalMenuEnabled; reenableFrame = -1; }
            string profile = match.setup.players[slot].participantId;
            var records = match.petProfiles.ForProfile(profile, SportsParticipantRegistry.GetAppearance(profile).bodyId);
            draft = JsonUtility.FromJson<PetRecord>(JsonUtility.ToJson(records.pets.Single(p => p.id == records.selectedPetId)));
            catalog = Resources.Load<SportsTruckAppearanceCatalog>("SportsTruckAppearanceCatalog"); if (catalog == null) throw new InvalidOperationException("Existing appearance catalog required.");
            originalMenuEnabled = match.menus.enabled; match.menus.enabled = false; reenableFrame = -1;
            originalFocus = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
            IsOpen = true; panel.SetActive(true); petName.SetTextWithoutNotify(draft.name); petName.characterLimit = 32; message.text = match.petProfiles.ReadOnly ? match.petProfiles.Status : "Choose a friend. Existing companions and their progress remain in your profile.";
            match.Gate(); try { BuildPreview(); } catch { Close(); throw; } if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(nextType.gameObject);
        }
        public void CycleType() { if (!IsOpen) return; draft.kind = (PetKind)(((int)draft.kind + 1) % 3); BuildPreview(); }
        public void CycleBody()
        {
            if (!IsOpen || draft.kind != PetKind.MiniatureTruck) return;
            int index = Array.FindIndex(catalog.bodies, b => b.id == draft.bodyId); draft.bodyId = catalog.bodies[(index + 1) % catalog.bodies.Length].id; BuildPreview();
        }
        public void CycleAccessory() { if (!IsOpen) return; int index = Array.IndexOf(Accessories, draft.accessoryId); draft.accessoryId = Accessories[(index + 1) % Accessories.Length]; BuildPreview(); }
        public void Apply()
        {
            if (!IsOpen) return;
            if (string.IsNullOrWhiteSpace(petName.text) || petName.text.Trim().Length > 32) { message.text = "Give your companion a name up to32characters."; return; }
            match.Adopt(owner, draft.kind, petName.text.Trim(), draft.bodyId, draft.accessoryId); Close();
        }
        public void Close()
        {
            if (!IsOpen) return; IsOpen = false; panel.SetActive(false); ReleasePreview(); draft = null;
            if (match != null) { match.Gate(); reenableFrame = Time.frameCount + 1; }
            if (EventSystem.current != null && originalFocus != null) EventSystem.current.SetSelectedGameObject(originalFocus);
        }
        void Update()
        {
            if (reenableFrame >= 0 && Time.frameCount >= reenableFrame) { if (match != null) match.menus.enabled = originalMenuEnabled; reenableFrame = -1; }
            if (!IsOpen) return;
            input.Sample(); bool back = SportsDeviceInput.KeyDown(Key.Escape);
            foreach (var pad in Gamepad.all) back |= input.Down(pad, 8) || input.Down(pad, 4);
            if (back) { Close(); return; }
            if(SportsDeviceInput.KeyDown(Key.Tab)&&EventSystem.current!=null)
            {
                var selected=EventSystem.current.currentSelectedGameObject;var control=selected!=null?selected.GetComponent<Selectable>():null;
                var following=control==null?nextType.GetComponent<Selectable>():SportsDeviceInput.KeyHeld(Key.LeftShift)?control.navigation.selectOnUp:control.navigation.selectOnDown;
                if(following!=null)EventSystem.current.SetSelectedGameObject(following.gameObject);
            }
            if (art != null) art.transform.Rotate(Vector3.up, Time.unscaledDeltaTime * 18, Space.World);
        }
        void BuildPreview()
        {
            ReleasePreview(); title.text = match.setup.players[owner].name + "'S COMPANION";
            description.text = draft.kind == PetKind.MiniatureTruck ? "MINIATURE HAULER\nA bright-eyed little truck with rolling wheels, playful suspension and a tiny horn." : draft.kind == PetKind.ClothedHuman ? "CARTOON ADULT COMPANION\nA fully clothed, expressive grown-up character with original playful routines." : "FOREST DOG\nA genuine animated dog rig, with idle/walking clips and original contextual tricks.";
            bodyLabel.text = draft.kind == PetKind.MiniatureTruck ? "BODY: " + (catalog.FindBody(draft.bodyId)?.label ?? "SAVED BODY UNAVAILABLE · STANDARD PREVIEW") : "BODY: " + draft.kind;
            nextBody.gameObject.SetActive(draft.kind == PetKind.MiniatureTruck); accessoryLabel.text = "ACCESSORY: " + draft.accessoryId;
            Navigation();
            stage = new GameObject("Owned isolated pet adoption preview"); stage.transform.position = new Vector3(0, -8000, 0);
            var template = renderTemplates[(int)draft.kind]; if (template == null) throw new InvalidOperationException("Selected pet render template is missing.");
            art = Instantiate(template, stage.transform, false); art.SetActive(true);
            if (art.GetComponentsInChildren<Rigidbody>(true).Length > 0 || art.GetComponentsInChildren<Collider>(true).Length > 0 || art.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true).Length > 0 || art.GetComponentsInChildren<AudioListener>(true).Length > 0) throw new InvalidOperationException("Adoption preview must not copy gameplay/physics/input/listener authority.");
            foreach (var sound in art.GetComponentsInChildren<AudioSource>(true)) sound.enabled = false;
            var mini = art.GetComponentInChildren<PetTruckArt>(true); if (mini != null) mini.SelectBody(draft.bodyId, match.setup.players[owner].color);
            art.GetComponent<PetAccessory>()?.Select(draft.accessoryId);
            var living = art.GetComponentInChildren<CampLivingArt>(true); if (living != null) { living.previewUnscaled=true;living.Request(CampPose.Idle); if (living.animator != null) living.animator.updateMode = AnimatorUpdateMode.UnscaledTime; }
            foreach (var child in art.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 24;
            var rendering = art.GetComponentsInChildren<Renderer>(true); if (rendering.Length == 0) throw new InvalidOperationException("A real pet renderer is required.");
            Bounds bounds = rendering[0].bounds; foreach (var renderer in rendering.Skip(1)) bounds.Encapsulate(renderer.bounds);
            var cameraObject = new GameObject("Owned pet adoption RT camera"); cameraObject.transform.SetParent(stage.transform, false); camera = cameraObject.AddComponent<Camera>(); camera.cullingMask = 1 << 24; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.018f, .05f, .055f); camera.nearClipPlane = .05f; camera.farClipPlane = 40; camera.fieldOfView = 35;
            float radius = Mathf.Max(bounds.size.magnitude * 1.1f, 3); camera.transform.position = bounds.center + new Vector3(.65f, .3f, 1).normalized * radius; camera.transform.LookAt(bounds.center);
            texture = RenderTexture.GetTemporary(720, 620, 24, RenderTextureFormat.ARGB32); camera.targetTexture = texture; preview.texture = texture;
            foreach (Camera view in match.GetComponentsInChildren<Camera>(true).Concat(match.cameras).Where(c => c != null && c != camera).Distinct()) { masks[view] = view.cullingMask; view.cullingMask &= ~(1 << 24); }
        }
        void Navigation()
        {
            var active=panel.GetComponentsInChildren<Selectable>().Where(s=>s.IsInteractable()).ToArray();
            for(int i=0;i<active.Length;i++)
            {
                var up=active[(i+active.Length-1)%active.Length];var down=active[(i+1)%active.Length];var navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.Explicit,selectOnUp=up,selectOnDown=down,selectOnLeft=up,selectOnRight=down};active[i].navigation=navigation;
                var heat=active[i].GetComponent<ButtonManager>();if(heat!=null){heat.useUINavigation=true;heat.navigationMode=navigation.mode;heat.selectOnUp=heat.selectOnLeft=up.gameObject;heat.selectOnDown=heat.selectOnRight=down.gameObject;}
            }
            if(EventSystem.current!=null&&EventSystem.current.currentSelectedGameObject!=null&&!EventSystem.current.currentSelectedGameObject.activeInHierarchy)EventSystem.current.SetSelectedGameObject(nextType.gameObject);
        }
        void ReleasePreview()
        {
            foreach (var entry in masks) if (entry.Key != null) entry.Key.cullingMask = entry.Value; masks.Clear();
            if (preview != null) preview.texture = null; if (camera != null) camera.targetTexture = null;
            if (texture != null) { RenderTexture.ReleaseTemporary(texture); texture = null; }
            if (stage != null) { stage.SetActive(false); Destroy(stage); } stage = art = null; camera = null;
        }
        void OnDisable()
        {
            if (match != null) { match.AdoptionRequested -= Open; if (IsOpen || reenableFrame >= 0) { match.menus.enabled = originalMenuEnabled; match.Gate(); } }
            IsOpen = false; reenableFrame = -1; if (panel != null) panel.SetActive(false); ReleasePreview();
        }
    }
}
