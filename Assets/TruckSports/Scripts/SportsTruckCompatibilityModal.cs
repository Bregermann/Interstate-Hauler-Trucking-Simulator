using System;
using System.Linq;
using Michsky.UI.Heat;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LWS.TruckSports
{
    // The installed Heat window owns animation/lifecycle. This component only
    // pages detached report data and routes its own focused menu input.
    public sealed class SportsTruckCompatibilityModal:MonoBehaviour
    {
        [Serializable]public sealed class Row{public GameObject root;public TMP_Text name,fit,status;public ButtonManager details,customize;}
        public ModalWindowManager window;public TMP_Text heading,pageCaption,detailText;public GameObject roster,details;
        public Row[] rows;public ButtonManager previous,next,back,close;
        public bool IsOpen=>window!=null&&window.isOn;
        public float CloseSeconds{get{var animator=window.GetComponent<Animator>();return animator.runtimeAnimatorController!=null?animator.runtimeAnimatorController.animationClips.Where(clip=>clip.name=="ModalWindow_Out").Select(clip=>clip.length).DefaultIfEmpty(.3f).Max():.3f;}}
        SportsTruckCompatibilityView[] entries=Array.Empty<SportsTruckCompatibilityView>();readonly SportsDeviceInput input=new SportsDeviceInput();
        Action closed;Action<int> customize;int page,driver,detailPage,openedFrame;bool detailed,draftOnly,wired;
        void Wire()
        {
            if(wired)return;wired=true;previous.onClick.AddListener(()=>Step(-1));next.onClick.AddListener(()=>Step(1));back.onClick.AddListener(()=>{detailed=false;Render();});close.onClick.AddListener(Close);window.onClose.AddListener(()=>closed?.Invoke());
            for(int row=0;row<rows.Length;row++){int index=row;rows[row].details.onClick.AddListener(()=>{driver=page*4+index;detailPage=0;detailed=true;Render();});rows[row].customize.onClick.AddListener(()=>{int at=page*4+index;if(at<entries.Length)customize?.Invoke(at);});}
        }
        public void Open(SportsTruckCompatibilityView[] values,string title,Action onClosed,Action<int> onCustomize=null,bool onlyDetails=false)
        {
            gameObject.SetActive(true);Wire();entries=values??Array.Empty<SportsTruckCompatibilityView>();customize=onCustomize;closed=onClosed;heading.text=title;page=driver=detailPage=0;detailed=draftOnly=onlyDetails;openedFrame=Time.frameCount+2;
            // Use the authored1240px body rectangle and existing font at its
            // configured maximum size; never shrink text to manufacture space.
            detailText.ForceMeshUpdate(true,true);float width=detailText.rectTransform.rect.width-detailText.margin.x-detailText.margin.z-12;
            foreach(var entry in entries)entry.FitDetails(value=>detailText.GetPreferredValues(value).x,width);
            window.OpenWindow();Render();
        }
        public void Close(){if(IsOpen)window.CloseWindow();}
        public void Abort(){closed=null;customize=null;if(window!=null){window.isOn=false;window.StopAllCoroutines();}gameObject.SetActive(false);}
        void Step(int delta)
        {
            if(detailed){int count=entries.Length==0?1:entries[driver].DetailPages;detailPage=(detailPage+delta+count)%count;}else{int count=Mathf.Max(1,Mathf.CeilToInt(entries.Length/4f));page=(page+delta+count)%count;}Render();
        }
        static string Brief(string text,int limit){if(string.IsNullOrEmpty(text))return "";var starts=System.Globalization.StringInfo.ParseCombiningCharacters(text);return starts.Length<=limit?text:text.Substring(0,starts[limit-1])+"…";}
        void Render()
        {
            roster.SetActive(!detailed);details.SetActive(detailed);int pages;
            if(detailed&&entries.Length>0){var value=entries[Mathf.Clamp(driver,0,entries.Length-1)];pages=value.DetailPages;detailText.text=value.Details(detailPage);pageCaption.text="DRIVER "+(driver+1)+" / "+entries.Length+" · DETAILS "+(detailPage+1)+" / "+pages;}
            else
            {
                pages=Mathf.Max(1,Mathf.CeilToInt(entries.Length/4f));pageCaption.text="DRIVERS "+(page*4+1)+"–"+Mathf.Min(entries.Length,page*4+4)+" / "+entries.Length;
                for(int i=0;i<rows.Length;i++){int at=page*4+i;var row=rows[i];row.root.SetActive(at<entries.Length);if(at>=entries.Length)continue;var value=entries[at];row.name.text=Brief(value.name,32)+" · ID "+value.ShortId;row.fit.text="REQUESTED: "+Brief(value.requested,40)+"\nHERE: "+Brief(value.effective,40);row.status.text=!value.canApply?"Original sport appearance · saved choices retained":value.unavailable>0?value.unavailable+" saved selections not applied here":value.bodyFallback?"Compatible body used here · saved choice retained":"Saved selections supported here";row.customize.gameObject.SetActive(customize!=null);}
            }
            previous.gameObject.SetActive(pages>1);next.gameObject.SetActive(pages>1);back.gameObject.SetActive(detailed&&!draftOnly);Navigation();
        }
        void Navigation()
        {
            var items=GetComponentsInChildren<UnityEngine.UI.Selectable>(false).Where(item=>item.IsInteractable()).ToArray();for(int i=0;i<items.Length;i++){var nav=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.Explicit,selectOnUp=items[(i+items.Length-1)%items.Length],selectOnDown=items[(i+1)%items.Length]};nav.selectOnLeft=nav.selectOnUp;nav.selectOnRight=nav.selectOnDown;items[i].navigation=nav;var heat=items[i].GetComponent<ButtonManager>();if(heat!=null){heat.useUINavigation=true;heat.navigationMode=nav.mode;heat.selectOnUp=heat.selectOnLeft=nav.selectOnUp.gameObject;heat.selectOnDown=heat.selectOnRight=nav.selectOnDown.gameObject;}}
            if(EventSystem.current!=null&&items.Length>0)EventSystem.current.SetSelectedGameObject(items[0].gameObject);
        }
        void Update()
        {
            if(!IsOpen||Time.frameCount<=openedFrame)return;input.Sample();bool cancel=SportsDeviceInput.KeyDown(Key.Escape);foreach(var pad in Gamepad.all)cancel|=input.Down(pad,8);if(cancel)Close();
        }
    }
}
