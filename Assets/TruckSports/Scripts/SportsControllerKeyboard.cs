using System;
using System.Collections.Generic;
using DG.Tweening;
using Michsky.UI.Heat;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace LWS.TruckSports
{
    // One scoped uGUI modal. Input System remains the device authority, Heat owns keys,
    // and TMP validates detached edits. No profile/save or match ownership lives here.
    [DefaultExecutionOrder(-600)]
    public sealed class SportsControllerKeyboard : MonoBehaviour
    {
        static SportsControllerKeyboard current;
        static int blockedThrough = -1;
        public static bool InputBlocked => current != null || Time.frameCount <= blockedThrough;
        public static bool IsOpen => current != null && !current.closing;
        public static Gamepad OwnerPad => current != null ? current.owner : null;
        // Optional shared party/host policy. Explicit Open calls still name their owner.
        public static Predicate<Gamepad> MenuPadAllowed { get; set; }
        public static event Action<TMP_InputField, bool> Closed;
        TMP_InputField target;
        Gamepad owner;
        Keyboard physicalKeyboard;
        SportsTextEditBuffer draft;
        SportsKeyboardValidation validation;
        Action<string> committed;
        EventSystem events;
        GameObject previousFocus, panel;
        RectTransform animatedBody;
        bool previousNavigation, armed, closing, released, shifted, symbols;
        int openedFrame, closingFrame, selected;
        float nextMove;
        Vector2 lastMove;
        char pendingSurrogate;
        TMP_Text preview, help;
        readonly List<ButtonManager> keys = new List<ButtonManager>();
        readonly List<MonoBehaviour> suspendedMenus = new List<MonoBehaviour>();
        CanvasGroup underneath;
        bool ownedGroup, oldInteractable, oldRaycasts;
        LWS.TruckBilliards.BilliardsInputRouter billiardsInput;
        bool restoreBilliardsInput;
        const string Letters = "1234567890qwertyuiopasdfghjkl'zxcvbnm,.-";
        const string Symbols = "!@#$%^&*()[]{}<>?+-=_/\\:;\"'.,~`|";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { current = null; blockedThrough = -1; MenuPadAllowed = null; Closed = null; }
        public static Gamepad SubmittingPad()
        {
            foreach (var pad in Gamepad.all) if (pad.added && pad.enabled && (MenuPadAllowed == null || MenuPadAllowed(pad)) && pad.buttonSouth.wasPressedThisFrame) return pad;
            return null;
        }
        // Explicit owner is suitable for the shared lobby; no Gamepad.current guessing.
        // A null callback edits the field and emits its existing commit events once.
        public static bool Open(TMP_InputField field, Gamepad pad, string initial = null, Action<string> onCommit = null, string title = "EDIT NAME")
        {
            if (InputBlocked || field == null || !field.gameObject.activeInHierarchy || !field.IsInteractable() || field.readOnly || pad == null || !pad.added || !pad.enabled || EventSystem.current == null) return false;
            var root = new GameObject("Controller text keyboard", typeof(RectTransform));
            var view = root.AddComponent<SportsControllerKeyboard>();
            current = view;
            try { view.Begin(field, pad, initial, onCommit, title); return true; }
            catch { view.Restore(); Destroy(root); throw; }
        }
        public static void CancelFor(TMP_InputField field)
        { if (current != null && current.target == field && !current.closing) current.Finish(false); }
        public static void Cancel() { if (current != null && !current.closing) current.Finish(false); }
        void Begin(TMP_InputField field, Gamepad pad, string initial, Action<string> onCommit, string title)
        {
            target = field; owner = pad; committed = onCommit;
            draft = new SportsTextEditBuffer(initial ?? field.text, field.characterLimit);
            events = EventSystem.current; previousFocus = events.currentSelectedGameObject;
            previousNavigation = events.sendNavigationEvents; events.sendNavigationEvents = false;
            // The active menu's own Back/Tab shortcuts must not run behind the modal.
            foreach (var menu in field.GetComponentsInParent<MonoBehaviour>(true))
                if (menu.enabled && (menu is SportsMenuView || menu is LWS.TruckBilliards.BilliardsMenus)) { suspendedMenus.Add(menu); menu.enabled = false; }
            var legacy = field.GetComponentInParent<LWS.TruckBilliards.BilliardsMenus>(true);
            if (legacy != null && legacy.session != null) { billiardsInput = legacy.session.input; restoreBilliardsInput = billiardsInput != null && billiardsInput.enabled; if (restoreBilliardsInput) billiardsInput.enabled = false; }
            var canvas = field.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                underneath = canvas.GetComponent<CanvasGroup>(); ownedGroup = underneath == null;
                if (ownedGroup) underneath = canvas.gameObject.AddComponent<CanvasGroup>();
                oldInteractable = underneath.interactable; oldRaycasts = underneath.blocksRaycasts;
                underneath.interactable = false; underneath.blocksRaycasts = false;
            }
            var validator = new GameObject("Detached TMP validation", typeof(RectTransform)); validator.SetActive(false); validator.transform.SetParent(transform, false);
            validation = validator.AddComponent<SportsKeyboardValidation>(); validation.Configure(field);
            Build(title, field.textComponent != null ? field.textComponent.font : TMP_Settings.defaultFontAsset);
            physicalKeyboard = Keyboard.current;
            if (physicalKeyboard != null) physicalKeyboard.onTextInput += Typed;
            InputSystem.onDeviceChange += DeviceChanged;
            openedFrame = Time.frameCount; Render(); Select(0);
            animatedBody.localScale = new Vector3(.96f, .96f, 1);
            animatedBody.DOScale(Vector3.one, .12f).SetUpdate(true);
        }
        void DeviceChanged(InputDevice device, InputDeviceChange change)
        {
            if (device == owner && (change == InputDeviceChange.Disconnected || change == InputDeviceChange.Removed || change == InputDeviceChange.Disabled)) Finish(false);
        }
        void OnApplicationFocus(bool focus) { if (!focus && target != null && !closing) Finish(false); }
        void OnApplicationPause(bool paused) { if (paused && target != null && !closing) Finish(false); }
        static bool PadNeutral(Gamepad pad) => pad == null || !pad.added || !pad.enabled || (!pad.buttonSouth.isPressed && !pad.buttonEast.isPressed && !pad.buttonWest.isPressed && !pad.buttonNorth.isPressed && !pad.startButton.isPressed && !pad.rightShoulder.isPressed && pad.dpad.ReadValue().sqrMagnitude < .1f && pad.leftStick.ReadValue().sqrMagnitude < .1f);
        static bool AllReleased()
        {
            foreach (var p in Gamepad.all) if (!PadNeutral(p)) return false;
            var k = Keyboard.current; var mouse = Mouse.current;
            return (k == null || !k.enterKey.isPressed && !k.escapeKey.isPressed && !k.spaceKey.isPressed && !k.backspaceKey.isPressed) && (mouse == null || !mouse.leftButton.isPressed);
        }
        void Update()
        {
            if (closing) { if (Time.frameCount > closingFrame && AllReleased()) { Restore(); Destroy(gameObject); } return; }
            if (target == null || !target.gameObject.activeInHierarchy || !target.enabled || owner == null || !owner.added || !owner.enabled) { Finish(false); return; }
            if (events != null && events.currentSelectedGameObject != keys[selected].gameObject && (events.currentSelectedGameObject == null || !events.currentSelectedGameObject.transform.IsChildOf(transform))) Select(selected);
            if (!armed) { if (Time.frameCount > openedFrame && PadNeutral(owner) && AllReleased()) armed = true; return; }
            var k = Keyboard.current;
            if (owner.buttonEast.wasPressedThisFrame || k != null && k.escapeKey.wasPressedThisFrame) { Finish(false); return; }
            if (owner.startButton.wasPressedThisFrame || k != null && k.enterKey.wasPressedThisFrame) { Finish(true); return; }
            if (owner.buttonWest.wasPressedThisFrame || k != null && k.backspaceKey.wasPressedThisFrame) { draft.Backspace(); Render(); }
            if (owner.buttonNorth.wasPressedThisFrame) { shifted = !shifted; Render(); }
            if (owner.rightShoulder.wasPressedThisFrame) { symbols = !symbols; Render(); }
            Vector2 move = owner.dpad.ReadValue(); if (move.sqrMagnitude < .25f) move = owner.leftStick.ReadValue();
            if (k != null) move += new Vector2((k.rightArrowKey.isPressed ? 1 : 0) - (k.leftArrowKey.isPressed ? 1 : 0), (k.upArrowKey.isPressed ? 1 : 0) - (k.downArrowKey.isPressed ? 1 : 0));
            if (move.sqrMagnitude < .25f) { lastMove = Vector2.zero; nextMove = 0; }
            else if (lastMove == Vector2.zero || Time.unscaledTime >= nextMove)
            {
                int delta = Mathf.Abs(move.x) > Mathf.Abs(move.y) ? (move.x > 0 ? 1 : -1) : (move.y > 0 ? -10 : 10);
                int at = Mathf.Clamp(selected + delta, 0, keys.Count - 1);
                while (at < 40 && Character(at).Length == 0) { int next = at + (delta > 0 ? 1 : -1); if (next < 0 || next >= keys.Count) break; at = next; }
                Select(at); nextMove = Time.unscaledTime + (lastMove == Vector2.zero ? .3f : .1f); lastMove = move;
            }
            if (owner.buttonSouth.wasPressedThisFrame) Activate(selected);
        }
        void Typed(char c)
        {
            if (!armed || closing || char.IsControl(c)) return;
            if (char.IsHighSurrogate(c)) { pendingSurrogate = c; return; }
            string value = char.IsLowSurrogate(c) && pendingSurrogate != '\0' ? new string(new[] { pendingSurrogate, c }) : c.ToString();
            pendingSurrogate = '\0'; Insert(value);
        }
        void Insert(string value)
        {
            if (!armed || closing || string.IsNullOrEmpty(value)) return;
            string candidate = validation.AppendValidated(draft.Text, value);
            draft.ReplaceDraft(candidate); Render();
        }
        string Character(int at)
        {
            string values = symbols ? Symbols : Letters;
            return at < values.Length ? (shifted ? char.ToUpperInvariant(values[at]) : values[at]).ToString() : "";
        }
        void Activate(int at)
        {
            if (!armed || closing) return;
            if (at < 40) { Insert(Character(at)); return; }
            switch (at - 40)
            {
                case 0: Insert(" "); break;
                case 1: draft.Backspace(); Render(); break;
                case 2: draft.Clear(); Render(); break;
                case 3: shifted = !shifted; Render(); break;
                case 4: symbols = !symbols; Render(); break;
                case 5: Finish(false); break;
                case 6: Finish(true); break;
            }
        }
        void Finish(bool confirm)
        {
            if (closing || draft == null) return;
            closing = true; closingFrame = Time.frameCount + 1; armed = false;
            if (physicalKeyboard != null) physicalKeyboard.onTextInput -= Typed;
            InputSystem.onDeviceChange -= DeviceChanged;
            if (animatedBody != null) animatedBody.DOKill();
            // Remove all modal raycast targets immediately; keep the underlying lease
            // until buttons are released so the same A/B cannot start another action.
            foreach (var raycaster in GetComponents<UnityEngine.UI.GraphicRaycaster>()) raycaster.enabled = false;
            if (panel != null) panel.SetActive(false);
            bool applied = false;
            try
            {
                if (confirm && target != null && target.gameObject.activeInHierarchy)
                {
                    string value = draft.Commit();
                    if (committed != null) committed(value);
                    else { target.SetTextWithoutNotify(value); target.onValueChanged.Invoke(value); target.onEndEdit.Invoke(value); }
                    applied = true;
                }
                else draft.Cancel();
            }
            finally { committed = null; Closed?.Invoke(target, applied); }
        }
        void Restore()
        {
            if (released) return; released = true;
            InputSystem.onDeviceChange -= DeviceChanged;
            if (physicalKeyboard != null) physicalKeyboard.onTextInput -= Typed;
            if (underneath != null) { underneath.interactable = oldInteractable; underneath.blocksRaycasts = oldRaycasts; if (ownedGroup) Destroy(underneath); }
            if (events != null) events.sendNavigationEvents = previousNavigation;
            foreach (var menu in suspendedMenus) if (menu != null) menu.enabled = true;
            if (restoreBilliardsInput && billiardsInput != null) { billiardsInput.GateAll(); billiardsInput.enabled = true; }
            if (current == this) current = null;
            blockedThrough = Time.frameCount + 1;
            if (events != null && previousFocus != null && previousFocus.activeInHierarchy) events.SetSelectedGameObject(previousFocus);
            target = null;
        }
        void OnDestroy() { if (animatedBody != null) animatedBody.DOKill(); Restore(); }
        void OnDisable() { if (target != null && !closing) Finish(false); Restore(); }
        void Select(int at)
        {
            selected = at;
            if (events != null) events.SetSelectedGameObject(keys[at].gameObject);
        }
        void Render()
        {
            preview.text = draft.Text.Length == 0 ? "_" : target != null && target.inputType == TMP_InputField.InputType.Password ? new string('•', new System.Globalization.StringInfo(draft.Text).LengthInTextElements) : draft.Text;
            for (int i = 0; i < 40; i++) { string caption = Character(i); keys[i].SetText(caption); keys[i].GetComponent<UnityEngine.UI.Button>().interactable = keys[i].isInteractable = caption.Length > 0; }
            if (selected < 40 && Character(selected).Length == 0) Select(0);
            keys[43].SetText(shifted ? "SHIFT ON" : "SHIFT"); keys[44].SetText(symbols ? "ABC" : "SYMBOLS");
            help.text = "A SELECT  ·  B CANCEL  ·  X DELETE  ·  Y SHIFT  ·  RB SYMBOLS  ·  START DONE\n" + draft.Text.Length + (draft.Limit > 0 ? " / " + draft.Limit : "") + " CHARACTERS  ·  ONLY THE OPENING CONTROLLER EDITS";
        }
        static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f); r.sizeDelta = size; r.anchoredPosition = position; return r;
        }
        static TMP_Text Text(string name, Transform parent, string value, Vector2 size, Vector2 position, float fontSize, TMP_FontAsset font)
        {
            var r = Rect(name, parent, size, position); var t = r.gameObject.AddComponent<TextMeshProUGUI>(); t.font = font; t.text = value; t.fontSize = fontSize; t.alignment = TextAlignmentOptions.Center; t.color = new Color(.94f, .96f, .88f); t.richText = false; t.raycastTarget = false; return t;
        }
        void Build(string title, TMP_FontAsset font)
        {
            var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 30000;
            var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var shade = Rect("Backdrop", transform, new Vector2(1920, 1080), Vector2.zero); shade.anchorMin = Vector2.zero; shade.anchorMax = Vector2.one; shade.offsetMin = shade.offsetMax = Vector2.zero; shade.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0, .015f, .025f, .96f);
            // Backdrop belongs to this modal, independent of the disabled source canvas.
            var body = Rect("Keyboard", shade, new Vector2(1400, 900), Vector2.zero); panel = shade.gameObject; animatedBody = body;
            body.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.014f, .038f, .048f); body.gameObject.AddComponent<SportsBroadcastPlate>().cut = 22;
            Text("Title", body, title, new Vector2(1280, 70), new Vector2(0, 365), 42, font);
            var box = Rect("Draft", body, new Vector2(1260, 78), new Vector2(0, 260)); box.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.035f, .13f, .16f);
            preview = Text("Draft text", box, "", new Vector2(1220, 70), Vector2.zero, 32, font); preview.overflowMode = TextOverflowModes.Ellipsis;
            for (int i = 0; i < 40; i++) Key(body, i, "", new Vector2(-558 + i % 10 * 124, 140 - i / 10 * 88), new Vector2(112, 74), font);
            string[] commands = { "SPACE", "DELETE", "CLEAR", "SHIFT", "SYMBOLS", "CANCEL", "DONE" };
            for (int i = 0; i < commands.Length; i++) Key(body, 40 + i, commands[i], new Vector2(-552 + i * 184, -252), new Vector2(170, 64), font);
            help = Text("Controls", body, "", new Vector2(1290, 96), new Vector2(0, -365), 22, font);
        }
        void Key(Transform parent, int index, string caption, Vector2 position, Vector2 size, TMP_FontAsset font)
        {
            var rect = Rect("Key " + index, parent, size, position); rect.gameObject.SetActive(false);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = new Color(.035f, .19f, .19f);
            rect.gameObject.AddComponent<UnityEngine.UI.Button>().transition = UnityEngine.UI.Selectable.Transition.None;
            var label = (TextMeshProUGUI)Text("Caption", rect, caption, size - new Vector2(8, 8), Vector2.zero, index < 40 ? 29 : 21, font);
            var button = rect.gameObject.AddComponent<ButtonManager>(); button.autoFitContent = false; button.useLocalization = false; button.useSounds = false; button.checkForDoubleClick = false; button.useUINavigation = false; button.normalTextObj = label; button.textSize = index < 40 ? 29 : 21; button.buttonText = caption;
            button.onClick.AddListener(() => { Select(index); Activate(index); });
            rect.gameObject.AddComponent<SportsBroadcastPlate>().cut = 9;
            var visual = rect.gameObject.AddComponent<LWS.TruckBilliards.BilliardsButtonVisual>(); visual.surface = image; visual.normal = image.color; visual.highlight = new Color(.08f, .52f, .46f);
            keys.Add(button); rect.gameObject.SetActive(true);
        }
    }
}
