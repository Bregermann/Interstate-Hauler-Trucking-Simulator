using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LWS.TruckTaxi
{
    // Taxi UI actions use Input System; legacy truck commands remain fixed direct reads.
    // The existing EventSystem owns UI navigation.
    // A single submit dispatch prevents an offer acceptance also submitting the next panel.
    public sealed class TruckTaxiUIInput : System.IDisposable
    {
        public InputActionMap Actions { get; } = new InputActionMap("Truck Taxi UI");
        public InputAction Submit { get; }
        public InputAction Cancel { get; }
        public InputAction Pause { get; }
        public InputAction Debug { get; }
        public InputAction Services { get; }
        public InputAction ShowControls { get; }
        public InputAction ToggleRideRequests { get; }
        public InputAction CenterView { get; }
        public InputAction FullMap { get; }
        public InputAction RadioToggle { get; }
        public InputAction RadioNext { get; }
        public InputAction RadioPrevious { get; }
        public InputAction RadioMute { get; }
        public InputAction RadioVolumeUp { get; }
        public InputAction RadioVolumeDown { get; }
        public InputAction CabMouseLook { get; }
        public InputAction CabMouseLookHold { get; }
        public InputAction CabGamepadLook { get; }
        public InputAction ExitTruck { get; }
        public InputAction WorldInteract { get; }
        public InputAction Inventory { get; }
        public InputAction InspectTruck { get; }
        public InputAction OnFootMove { get; }
        public InputAction OnFootLook { get; }
        public InputAction OnFootSprint { get; }
        public InputAction Navigate => navigate;
        public bool UsingGamepad => gamepad;
        public string ActiveDeviceLabel => !gamepad ? "KEYBOARD / MOUSE" : GamepadFamily(lastGamepadLayout);
        public float LastMeaningfulInputAt { get; private set; } = float.NegativeInfinity;
        private string lastGamepadLayout;
        public Transform FocusRoot => scope;
        private InputAction navigate;
        private Transform scope;
        private readonly List<Selectable> controls=new List<Selectable>();
        private readonly List<Selectable> candidates=new List<Selectable>();
        private readonly EventSystem eventSystem;
        private bool gamepad;
        private InputSystemUIInputModule module;
        private InputActionReference moveReference;
        private InputActionReference previousMove,previousSubmit,previousCancel;
        private InputActionAsset asset;
        public TruckTaxiUIInput() : this(EventSystem.current) { }
        public TruckTaxiUIInput(EventSystem uiEventSystem)
        {
            if(uiEventSystem==null) throw new System.ArgumentNullException(nameof(uiEventSystem));
            eventSystem=uiEventSystem;
            asset=ScriptableObject.CreateInstance<InputActionAsset>(); asset.AddActionMap(Actions);
            Submit=Button("SubmitAccept","<Keyboard>/enter","<Gamepad>/buttonSouth");
            Cancel=Button("CancelDecline","<Keyboard>/escape","<Gamepad>/buttonEast");
            Cancel.AddBinding("<Keyboard>/backspace");
            Pause=Button("Pause","<Keyboard>/escape","<Gamepad>/start");
            Debug=Button("Debug","<Keyboard>/f8",null);
            Services=Button("Services","<Keyboard>/end",null);
            ShowControls=Button("ShowControls","<Keyboard>/f1",null);
            ToggleRideRequests=Button("ToggleRideRequests","<Keyboard>/f2",null);
            CenterView=Button("CenterView","<Keyboard>/backquote",null);
            // M is the truck's engine brake; keep this action separate from driving.
            FullMap=Button("FullMap","<Keyboard>/f3",null);
            RadioToggle=Button("RadioToggle","<Keyboard>/f4",null);
            RadioNext=Button("RadioNext","<Keyboard>/f10",null);
            RadioPrevious=Button("RadioPrevious","<Keyboard>/f9",null);
            RadioMute=Button("RadioMute","<Keyboard>/f11",null);
            RadioVolumeUp=Button("RadioVolumeUp","<Keyboard>/numpadPlus",null);
            RadioVolumeDown=Button("RadioVolumeDown","<Keyboard>/numpadMinus",null);
            FullMap.AddCompositeBinding("ButtonWithOneModifier")
                .With("Modifier","<Gamepad>/leftTrigger").With("Button","<Gamepad>/rightStickPress");
            CabMouseLook=Actions.AddAction("CabMouseLook",InputActionType.PassThrough,"<Mouse>/delta");
            CabMouseLookHold=Actions.AddAction("CabMouseLookHold",InputActionType.Button,"<Mouse>/rightButton");
            CabGamepadLook=Actions.AddAction("CabGamepadLook",InputActionType.Value,"<Gamepad>/rightStick");
            ExitTruck=Button("ExitEnterTruck","<Keyboard>/g","<Gamepad>/rightStickPress");
            WorldInteract=Button("WorldInteract","<Keyboard>/e","<Gamepad>/buttonSouth");
            Inventory=Button("Inventory","<Keyboard>/tab","<Gamepad>/select");
            InspectTruck=Button("InspectTruck","<Keyboard>/i","<Gamepad>/buttonNorth");
            OnFootSprint=Button("OnFootSprint","<Keyboard>/leftShift","<Gamepad>/leftStickPress");
            OnFootMove=Actions.AddAction("OnFootMove",InputActionType.Value);
            OnFootMove.expectedControlType="Vector2";
            OnFootMove.AddCompositeBinding("2DVector").With("Up","<Keyboard>/w").With("Down","<Keyboard>/s")
                .With("Left","<Keyboard>/a").With("Right","<Keyboard>/d");
            OnFootMove.AddBinding("<Gamepad>/leftStick");
            OnFootLook=Actions.AddAction("OnFootLook",InputActionType.PassThrough);
            OnFootLook.AddBinding("<Mouse>/delta"); OnFootLook.AddBinding("<Gamepad>/rightStick");
            navigate=Actions.AddAction("Navigate",InputActionType.Value);
            navigate.expectedControlType="Vector2";
            navigate.AddCompositeBinding("2DVector").With("Up","<Keyboard>/upArrow").With("Down","<Keyboard>/downArrow").With("Left","<Keyboard>/leftArrow").With("Right","<Keyboard>/rightArrow");
            navigate.AddCompositeBinding("2DVector").With("Up","<Keyboard>/w").With("Down","<Keyboard>/s").With("Left","<Keyboard>/a").With("Right","<Keyboard>/d");
            navigate.AddBinding("<Gamepad>/dpad"); navigate.AddBinding("<Gamepad>/leftStick");
            foreach(var action in Actions) action.performed+=RememberDevice;
            module=eventSystem.GetComponent<InputSystemUIInputModule>();
            if(module==null) module=eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            if(module.actionsAsset==null) module.AssignDefaultActions();
            previousMove=module.move; previousSubmit=module.submit; previousCancel=module.cancel;
            moveReference=InputActionReference.Create(navigate);
            module.move=moveReference; module.submit=null; module.cancel=null;
            Actions.Enable();
        }
        private InputAction Button(string name,string keyboard,string pad)
        { var a=Actions.AddAction(name,InputActionType.Button,keyboard); if(pad!=null) a.AddBinding(pad); return a; }
        private void RememberDevice(InputAction.CallbackContext context)
        {
            if(context.valueType==typeof(Vector2) && !MeaningfulDeviceMotion(context.ReadValue<Vector2>(),context.control.device is Gamepad)) return;
            if(context.valueType==typeof(float) && Mathf.Abs(context.ReadValue<float>())<.2f) return;
            RememberMeaningfulDevice(context.control.device);
        }
        private void RememberMeaningfulDevice(InputDevice device)
        {
            gamepad=device is Gamepad;
            if (gamepad) lastGamepadLayout=device.layout;
            LastMeaningfulInputAt=Time.unscaledTime;
        }
        public static string GamepadFamily(string layout)
        {
            if (string.IsNullOrEmpty(layout)) return "GENERIC GAMEPAD";
            if (layout.IndexOf("DualShock",System.StringComparison.OrdinalIgnoreCase)>=0 ||
                layout.IndexOf("DualSense",System.StringComparison.OrdinalIgnoreCase)>=0) return "PLAYSTATION-STYLE CONTROLLER";
            if (layout.IndexOf("Switch",System.StringComparison.OrdinalIgnoreCase)>=0 ||
                layout.IndexOf("Nintendo",System.StringComparison.OrdinalIgnoreCase)>=0) return "NINTENDO-STYLE CONTROLLER";
            if (layout.IndexOf("XInput",System.StringComparison.OrdinalIgnoreCase)>=0 ||
                layout.IndexOf("Xbox",System.StringComparison.OrdinalIgnoreCase)>=0) return "XBOX-STYLE CONTROLLER";
            return "GENERIC GAMEPAD";
        }
        public static bool MeaningfulDeviceMotion(Vector2 motion,bool isGamepad) =>
            float.IsFinite(motion.x) && float.IsFinite(motion.y) && motion.sqrMagnitude >= (isGamepad ? .25f*.25f : 1f);
        public void RefreshDeviceFromHardware()
        {
            var pad=Gamepad.current;
            if(pad!=null && pad.wasUpdatedThisFrame &&
                (MeaningfulDeviceMotion(pad.leftStick.ReadValue(),true) || MeaningfulDeviceMotion(pad.rightStick.ReadValue(),true) ||
                pad.buttonSouth.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame ||
                pad.buttonWest.wasPressedThisFrame || pad.startButton.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame ||
                pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame ||
                pad.leftStickButton.wasPressedThisFrame || pad.rightStickButton.wasPressedThisFrame ||
                MeaningfulDeviceMotion(pad.dpad.ReadValue(),true) || pad.leftTrigger.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame)) RememberMeaningfulDevice(pad);
            var mouse=Mouse.current;
            if(Keyboard.current?.anyKey.wasPressedThisFrame==true || mouse!=null &&
                (MeaningfulDeviceMotion(mouse.delta.ReadValue(),false) || mouse.leftButton.wasPressedThisFrame ||
                mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame || mouse.scroll.ReadValue().sqrMagnitude>1))
                RememberMeaningfulDevice(Keyboard.current?.anyKey.wasPressedThisFrame==true ? (InputDevice)Keyboard.current : mouse);
        }
        public void ToggleRideRequestsFor(TruckTaxiSession session)
        {
            if(session!=null) session.SetRideRequestsEnabled(!session.RideRequestsEnabled);
        }
        public string Hint(InputAction action)
        {
            for(int i=0;i<action.bindings.Count;i++)
                if(!string.IsNullOrEmpty(action.bindings[i].effectivePath) && action.bindings[i].effectivePath.Contains(gamepad ? "Gamepad" : "Keyboard")) return action.GetBindingDisplayString(i);
            return action.GetBindingDisplayString();
        }
        public void Focus(Transform next)
        {
            bool scopeChanged=next!=scope;
            if(scopeChanged)
            {
                candidates.Clear();
                if(next!=null) next.GetComponentsInChildren(true,candidates);
            }
            var selected=eventSystem.currentSelectedGameObject;
            var selectedControl=selected!=null ? selected.GetComponent<Selectable>() : null;
            bool selectedValid=next!=null && Usable(selectedControl) && selected.transform.IsChildOf(next);
            if(!scopeChanged && NavigationIsCurrent() && (next==null ? selected==null : selectedValid)) return;
            scope=next; controls.Clear();
            foreach(var c in candidates) if(Usable(c)) controls.Add(c);
            for(int i=0;i<controls.Count;i++)
            {
                var c=controls[i];
                var nav=new Navigation { mode=Navigation.Mode.Explicit,
                    selectOnUp=controls[(i+controls.Count-1)%controls.Count], selectOnDown=controls[(i+1)%controls.Count] };
                if(!(c is Slider)) { nav.selectOnLeft=nav.selectOnUp; nav.selectOnRight=nav.selectOnDown; }
                c.navigation=nav;
            }
            eventSystem.SetSelectedGameObject(!scopeChanged && selectedValid ? selected : controls.Count>0 ? controls[0].gameObject : null);
        }
        private static bool Usable(Selectable control) => control!=null && control.isActiveAndEnabled && control.IsInteractable();
        private bool NavigationIsCurrent()
        {
            int index=0;
            foreach(var candidate in candidates) if(Usable(candidate))
            {
                if(index>=controls.Count || controls[index]!=candidate) return false;
                index++;
            }
            return index==controls.Count;
        }
        public void SubmitSelected()
        {
            var selected=eventSystem.currentSelectedGameObject;
            if(scope!=null && selected!=null && selected.activeInHierarchy && selected.transform.IsChildOf(scope))
                ExecuteEvents.Execute(selected,new BaseEventData(eventSystem),ExecuteEvents.submitHandler);
        }
        public void Dispose()
        {
            if(module!=null && module.move==moveReference)
            { module.move=previousMove; module.submit=previousSubmit; module.cancel=previousCancel; }
            Actions.Dispose();
            DestroyOwned(moveReference); DestroyOwned(asset);
        }
        private static void DestroyOwned(Object value)
        {
            if(value==null) return;
            if(Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value);
        }
    }
}
