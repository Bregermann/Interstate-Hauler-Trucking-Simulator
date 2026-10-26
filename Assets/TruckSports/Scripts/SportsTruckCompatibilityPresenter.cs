using System;
using System.Collections;
using System.Linq;
using Michsky.UI.Heat;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LWS.TruckSports
{
    public sealed class SportsTruckCompatibilityPresenter:MonoBehaviour
    {
        public const string ModalResource="TruckSports/SportsTruckCompatibilityModal";
        public SportsTruckCompatibilitySource source;public TMP_Text subtitle;public string originalSubtitle="";public ButtonManager reportButton;
        public bool IsOpen=>lease!=null;public SportsTruckCompatibilityModal Modal=>modal;
        SportsTruckCompatibilityModal modal;SportsTruckCustomizerMenu shop;MenuLease lease,shopLease;int pendingSlot=-1;bool closing,draftReport,ownedSubtitle;float nextRefresh;GameObject previousFocus;
        sealed class MenuLease:IDisposable
        {
            readonly MonoBehaviour menu;readonly CanvasGroup group;readonly bool enabled,interactable,raycasts;readonly bool disable;
            public MenuLease(MonoBehaviour target,CanvasGroup canvas,bool disablePresenter=true){menu=target;group=canvas;disable=disablePresenter;enabled=menu!=null&&menu.enabled;interactable=group!=null&&group.interactable;raycasts=group!=null&&group.blocksRaycasts;if(group!=null){group.interactable=false;group.blocksRaycasts=false;}if(menu!=null&&disable)menu.enabled=false;}
            public void Dispose(){if(group!=null){group.interactable=interactable;group.blocksRaycasts=raycasts;}if(menu!=null&&disable)menu.enabled=enabled;}
        }
        void Start(){if(reportButton!=null)reportButton.onClick.AddListener(Open);RefreshSummary();}
        void Update(){if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.2f;if(lease==null)RefreshSummary();}
        public void RefreshSummary()
        {
            if(source==null||subtitle==null)return;bool setup=source.IsSetup;if(reportButton!=null)reportButton.gameObject.SetActive(setup);if(!setup)return;int original=0,temporary=0,items=0;for(int i=0;i<source.Count;i++){var view=source.Read(i);if(!view.hasHost||!view.canApply)original++;else if(view.bodyFallback)temporary++;items+=view.unavailable;}
            string fit=original==source.Count?"TRUCK FIT · ORIGINAL TRUCK · SAVED DESIGNS KEPT":original>0?"TRUCK FIT · "+original+" ORIGINAL APPEARANCES · DETAILS AVAILABLE":temporary>0||items>0?"TRUCK FIT · TEMPORARY FITS / "+items+" SAVED ITEMS NOT APPLIED · DETAILS":"TRUCK FIT · SAVED TRUCKS SUPPORTED HERE";
            subtitle.text=string.IsNullOrEmpty(originalSubtitle)?fit:originalSubtitle+"\n"+fit;ownedSubtitle=true;
        }
        SportsTruckCompatibilityModal EnsureModal()
        {
            if(modal!=null)return modal;var prefab=Resources.Load<GameObject>(ModalResource);if(prefab==null)throw new InvalidOperationException("Author the reusable Heat truck compatibility modal first.");modal=Instantiate(prefab).GetComponent<SportsTruckCompatibilityModal>();return modal;
        }
        public void Open()
        {
            if(source==null||!source.IsSetup||lease!=null)return;var ready=EnsureModal();previousFocus=EventSystem.current!=null?EventSystem.current.currentSelectedGameObject:null;lease=new MenuLease(source.menu,source.setupInteraction);pendingSlot=-1;draftReport=false;ready.Open(Enumerable.Range(0,source.Count).Select(source.Read).ToArray(),source.sportName+" · TRUCK FIT",Closed,Customize);
        }
        void Customize(int slot){pendingSlot=slot;modal.Close();}
        void Closed(){if(closing)return;closing=true;StartCoroutine(AfterClose());}
        IEnumerator AfterClose()
        {
            yield return new WaitForSecondsRealtime(modal.CloseSeconds+.03f);while(!Neutral())yield return null;yield return null;closing=false;
            if(draftReport){draftReport=false;shopLease?.Dispose();shopLease=null;if(shop!=null)shop.SetSportReportOpen(false);yield break;}
            if(pendingSlot>=0&&source!=null&&source.IsSetup){int slot=pendingSlot;pendingSlot=-1;OpenShop(slot);yield break;}
            Release();
        }
        void OpenShop(int slot)
        {
            if(slot<0||slot>=source.Count){Release();return;}if(shop==null){var prefab=Resources.Load<GameObject>("TruckSports/SportsTruckCustomizerMenu");if(prefab==null){Release();return;}shop=Instantiate(prefab).GetComponent<SportsTruckCustomizerMenu>();}
            string id=source.Participants[slot].ParticipantId;var context=new SportsTruckCompatibilityContext{source=source,slot=slot,profileId=id};context.showDetails=(draft,owner)=>OpenDraft(context,draft,owner);shop.OpenForSport(id,context,ShopClosed);
        }
        void ShopClosed(){if(!isActiveAndEnabled){Release();return;}if(closing)return;closing=true;StartCoroutine(AfterShopClose());}
        IEnumerator AfterShopClose(){while(!Neutral())yield return null;yield return null;closing=false;Release();}
        void OpenDraft(SportsTruckCompatibilityContext context,SportsTruckAppearanceData draft,SportsTruckCustomizerMenu owner)
        {
            if(closing||draftReport||owner==null||!owner.IsOpen)return;draftReport=true;owner.SetSportReportOpen(true);shopLease=new MenuLease(owner,owner.GetComponent<CanvasGroup>(),false);EnsureModal().Open(new[]{context.Read(draft)},source.sportName+" · CURRENT DRAFT FIT",Closed,null,true);
        }
        static bool Neutral()
        {
            if(SportsDeviceInput.KeyHeld(Key.Escape)||SportsDeviceInput.KeyHeld(Key.Enter)||SportsDeviceInput.KeyHeld(Key.Space)||Mouse.current!=null&&Mouse.current.leftButton.isPressed)return false;foreach(var pad in Gamepad.all)if(pad.buttonSouth.isPressed||pad.buttonEast.isPressed||pad.startButton.isPressed||pad.dpad.ReadValue().sqrMagnitude>.1f)return false;return true;
        }
        void Release()
        {
            bool hadLease=lease!=null;shopLease?.Dispose();shopLease=null;if(shop!=null)shop.SetSportReportOpen(false);lease?.Dispose();lease=null;if(source!=null){if(hadLease&&source.IsSetup)source.GateFresh();RefreshSummary();}if(EventSystem.current!=null&&previousFocus!=null&&previousFocus.activeInHierarchy)EventSystem.current.SetSelectedGameObject(previousFocus);previousFocus=null;
        }
        void OnDisable(){StopAllCoroutines();closing=draftReport=false;pendingSlot=-1;if(modal!=null)modal.Abort();if(shop!=null&&shop.IsOpen)shop.Close();Release();if(ownedSubtitle&&subtitle!=null)subtitle.text=originalSubtitle;}
        void OnDestroy(){if(modal!=null)Destroy(modal.gameObject);if(shop!=null)Destroy(shop.gameObject);}
    }
}
