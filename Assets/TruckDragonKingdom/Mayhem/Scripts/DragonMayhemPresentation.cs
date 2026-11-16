using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckSports;
using Michsky.UI.Heat;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LWS.TruckDragonKingdom.Mayhem
{
    [DefaultExecutionOrder(-90)]
    public sealed class DragonMayhemPresentation : MonoBehaviour
    {
        public DragonMayhemDirector director;
        public ButtonManager toggle;
        public TMP_Text caption, warning, bonus, result;
        public GameObject overlay, resultPanel;
        public ButtonManager helpButton, closeHelp;
        public GameObject helpPage;
        public SportsMenuView menu;
        public CanvasGroup setupInteraction;
        public ModalWindowManager helpWindow;
        public bool IsHelpOpen=>leased;
        readonly SportsDeviceInput input=new SportsDeviceInput();
        readonly Dictionary<ButtonManager,bool> setupButtons=new Dictionary<ButtonManager,bool>();
        bool leased,closing,ready,priorMenuEnabled,priorInteractable,priorRaycasts;
        int openedFrame;GameObject previousFocus;Coroutine closeRoutine;
        void Start()
        {
            if(menu==null||setupInteraction==null||helpWindow==null)throw new System.InvalidOperationException("Explicitly upgrade saved Mayhem presentation before using its help modal.");
            toggle.onClick.AddListener(Toggle);helpButton.onClick.AddListener(OpenHelp);closeHelp.onClick.AddListener(CloseHelp);helpWindow.onClose.AddListener(WindowClosed);helpPage.SetActive(false);Refresh();
        }
        public void OpenHelp()
        {
            if(leased||director.adapter.InMatch||menu.Page!=0||!setupInteraction.interactable)return;
            previousFocus=EventSystem.current!=null?EventSystem.current.currentSelectedGameObject:null;
            priorMenuEnabled=menu.enabled;priorInteractable=setupInteraction.interactable;priorRaycasts=setupInteraction.blocksRaycasts;
            setupButtons.Clear();foreach(var button in menu.pages[0].GetComponentsInChildren<ButtonManager>(true)){setupButtons[button]=button.isInteractable;button.isInteractable=false;}
            leased=true;closing=ready=false;openedFrame=Time.frameCount+2;input.Sample();
            setupInteraction.interactable=setupInteraction.blocksRaycasts=false;menu.enabled=false;
            helpPage.transform.SetAsLastSibling();helpPage.SetActive(true);helpWindow.OpenWindow();FocusClose();
        }
        public void CloseHelp()
        {
            if(!leased||closing)return;
            if(helpWindow.isOn)helpWindow.CloseWindow();else WindowClosed();
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);
        }
        void WindowClosed(){if(!leased||closing)return;closing=true;closeRoutine=StartCoroutine(AfterClose());}
        IEnumerator AfterClose()
        {
            var animator=helpWindow.GetComponent<Animator>();
            float seconds=animator!=null&&animator.runtimeAnimatorController!=null?animator.runtimeAnimatorController.animationClips.Where(c=>c.name=="ModalWindow_Out").Select(c=>c.length).DefaultIfEmpty(.3f).Max():.3f;
            yield return new WaitForSecondsRealtime(seconds+.03f);while(!Neutral())yield return null;yield return null;closeRoutine=null;Release(true);
        }
        static bool Neutral()
        {
            if(SportsDeviceInput.KeyHeld(Key.Escape)||SportsDeviceInput.KeyHeld(Key.Enter)||SportsDeviceInput.KeyHeld(Key.Space)||Mouse.current!=null&&Mouse.current.leftButton.isPressed)return false;
            foreach(var pad in Gamepad.all)if(pad.buttonSouth.isPressed||pad.buttonEast.isPressed||pad.startButton.isPressed||pad.dpad.ReadValue().sqrMagnitude>.1f)return false;
            return true;
        }
        void FocusClose()
        {
            var selectable=closeHelp.GetComponent<UnityEngine.UI.Selectable>();var nav=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.Explicit,selectOnUp=selectable,selectOnDown=selectable,selectOnLeft=selectable,selectOnRight=selectable};selectable.navigation=nav;
            closeHelp.useUINavigation=true;closeHelp.navigationMode=nav.mode;closeHelp.selectOnUp=closeHelp.selectOnDown=closeHelp.selectOnLeft=closeHelp.selectOnRight=closeHelp.gameObject;
            if(EventSystem.current!=null){EventSystem.current.SetSelectedGameObject(null);EventSystem.current.SetSelectedGameObject(closeHelp.gameObject);}
        }
        void Release(bool restoreFocus)
        {
            if(helpWindow!=null){helpWindow.isOn=false;helpWindow.StopAllCoroutines();}
            if(helpPage!=null)helpPage.SetActive(false);
            if(leased)
            {
                if(setupInteraction!=null){setupInteraction.interactable=priorInteractable;setupInteraction.blocksRaycasts=priorRaycasts;}
                foreach(var pair in setupButtons)if(pair.Key!=null)pair.Key.isInteractable=pair.Value;
                if(menu!=null)menu.enabled=priorMenuEnabled;
                if(director!=null&&director.adapter!=null)
                {var adapter=director.adapter;if(adapter.keepaway!=null)adapter.keepaway.Gate();else if(adapter.balloons!=null)adapter.balloons.Gate();else if(adapter.cornhole!=null)adapter.cornhole.Gate();else if(adapter.cheese!=null)adapter.cheese.Gate();}
            }
            leased=closing=ready=false;setupButtons.Clear();
            if(restoreFocus&&menu!=null&&menu.Page==0&&EventSystem.current!=null)
            {
                if(previousFocus!=null&&previousFocus.activeInHierarchy)EventSystem.current.SetSelectedGameObject(previousFocus);else menu.Focus();
            }
            previousFocus=null;
        }
        void AbortHelp(){if(closeRoutine!=null){StopCoroutine(closeRoutine);closeRoutine=null;}Release(false);}
        void Toggle() { director.SetEnabled(!director.EnabledForSport); Refresh(); }
        void Update()
        {
            if(leased)
            {
                if(director.adapter.InMatch||menu.Page!=0){AbortHelp();}
                else
                {
                    foreach(var button in setupButtons.Keys)if(button!=null)button.isInteractable=false;
                    input.Sample();if(!ready&&Time.frameCount>openedFrame&&Neutral())ready=true;
                    if(!closing)
                    {
                        if(EventSystem.current!=null&&(EventSystem.current.currentSelectedGameObject==null||!EventSystem.current.currentSelectedGameObject.transform.IsChildOf(helpPage.transform)))FocusClose();
                        bool close=SportsDeviceInput.KeyDown(Key.Escape)||SportsDeviceInput.KeyDown(Key.Enter)||SportsDeviceInput.KeyDown(Key.Space);foreach(var pad in Gamepad.all)close|=input.Down(pad,8)||input.Down(pad,1);
                        if(ready&&close)CloseHelp();
                    }
                }
            }
            Refresh();
        }
        void Refresh()
        {
            bool enabled=director.EnabledForSport;
            caption.text="DRAGON MAYHEM: "+(enabled?"ON":"OFF");
            toggle.isInteractable=!director.adapter.InMatch&&!leased;helpButton.isInteractable=!director.adapter.InMatch&&!leased;
            bool show=enabled && director.adapter.InMatch && !director.adapter.Finished;
            overlay.SetActive(show);
            resultPanel.SetActive(enabled && director.adapter.Finished);
            if(show) { warning.text=director.Warning; bonus.text=director.BonusText(); }
            if(enabled && director.adapter.Finished)result.text=director.ResultBonusText();
        }
        void OnDisable()=>AbortHelp();
        void OnDestroy() { AbortHelp();if(toggle!=null)toggle.onClick.RemoveListener(Toggle); if(helpButton!=null)helpButton.onClick.RemoveListener(OpenHelp); if(closeHelp!=null)closeHelp.onClick.RemoveListener(CloseHelp);if(helpWindow!=null)helpWindow.onClose.RemoveListener(WindowClosed); }
    }
}
