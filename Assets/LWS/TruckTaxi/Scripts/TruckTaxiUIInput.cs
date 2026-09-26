using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LWS.TruckTaxi
{
    // Input System owns devices and rebinding; the existing EventSystem owns UI navigation.
    // A single submit dispatch prevents an offer acceptance also submitting the next panel.
    public sealed class TruckTaxiUIInput : System.IDisposable
    {
        public InputActionMap Actions { get; } = new InputActionMap("Truck Taxi UI");
        public InputAction Submit { get; }
        public InputAction Cancel { get; }
        public InputAction Pause { get; }
        public InputAction Debug { get; }
        private InputAction navigate;
        private Transform scope;
        private readonly List<Selectable> controls=new List<Selectable>();
        private bool gamepad;
        private InputSystemUIInputModule module;
        private InputActionReference moveReference;
        private InputActionReference previousMove,previousSubmit,previousCancel;
        private InputActionAsset asset;
        public TruckTaxiUIInput()
        {
            asset=ScriptableObject.CreateInstance<InputActionAsset>(); asset.AddActionMap(Actions);
            Submit=Button("SubmitAccept","<Keyboard>/enter","<Gamepad>/buttonSouth");
            Cancel=Button("CancelDecline","<Keyboard>/escape","<Gamepad>/buttonEast");
            Cancel.AddBinding("<Keyboard>/backspace");
            Pause=Button("Pause","<Keyboard>/escape","<Gamepad>/start");
            Debug=Button("Debug","<Keyboard>/f8",null);
            navigate=Actions.AddAction("Navigate",InputActionType.Value);
            navigate.expectedControlType="Vector2";
            navigate.AddCompositeBinding("2DVector").With("Up","<Keyboard>/upArrow").With("Down","<Keyboard>/downArrow").With("Left","<Keyboard>/leftArrow").With("Right","<Keyboard>/rightArrow");
            navigate.AddCompositeBinding("2DVector").With("Up","<Keyboard>/w").With("Down","<Keyboard>/s").With("Left","<Keyboard>/a").With("Right","<Keyboard>/d");
            navigate.AddBinding("<Gamepad>/dpad"); navigate.AddBinding("<Gamepad>/leftStick");
            foreach(var action in Actions) action.performed+=RememberDevice;
            module=EventSystem.current.GetComponent<InputSystemUIInputModule>();
            if(module==null) module=EventSystem.current.gameObject.AddComponent<InputSystemUIInputModule>();
            if(module.actionsAsset==null) module.AssignDefaultActions();
            previousMove=module.move; previousSubmit=module.submit; previousCancel=module.cancel;
            moveReference=InputActionReference.Create(navigate);
            module.move=moveReference; module.submit=null; module.cancel=null;
            Actions.Enable();
        }
        private InputAction Button(string name,string keyboard,string pad)
        { var a=Actions.AddAction(name,InputActionType.Button,keyboard); if(pad!=null) a.AddBinding(pad); return a; }
        private void RememberDevice(InputAction.CallbackContext context) => gamepad=context.control.device is Gamepad;
        public string Hint(InputAction action)
        {
            for(int i=0;i<action.bindings.Count;i++)
                if(action.bindings[i].effectivePath.Contains(gamepad ? "Gamepad" : "Keyboard")) return action.GetBindingDisplayString(i);
            return action.GetBindingDisplayString();
        }
        public void Focus(Transform next)
        {
            var selected=EventSystem.current.currentSelectedGameObject;
            if(next==scope && (next==null || (selected!=null && selected.activeInHierarchy && selected.transform.IsChildOf(next)))) return;
            scope=next; controls.Clear();
            if(next!=null) foreach(var c in next.GetComponentsInChildren<Selectable>()) if(c.IsInteractable()) controls.Add(c);
            for(int i=0;i<controls.Count;i++)
            {
                var c=controls[i];
                var nav=new Navigation { mode=Navigation.Mode.Explicit,
                    selectOnUp=controls[(i+controls.Count-1)%controls.Count], selectOnDown=controls[(i+1)%controls.Count] };
                if(!(c is Slider)) { nav.selectOnLeft=nav.selectOnUp; nav.selectOnRight=nav.selectOnDown; }
                c.navigation=nav;
            }
            EventSystem.current.SetSelectedGameObject(controls.Count>0 ? controls[0].gameObject : null);
        }
        public void SubmitSelected()
        {
            var selected=EventSystem.current.currentSelectedGameObject;
            if(scope!=null && selected!=null && selected.activeInHierarchy && selected.transform.IsChildOf(scope))
                ExecuteEvents.Execute(selected,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
        }
        public void Dispose()
        {
            Actions.Dispose();
            if(module!=null && module.move==moveReference)
            { module.move=previousMove; module.submit=previousSubmit; module.cancel=previousCancel; }
            if(moveReference!=null) Object.Destroy(moveReference);
            if(asset!=null) Object.Destroy(asset);
        }
    }
}
