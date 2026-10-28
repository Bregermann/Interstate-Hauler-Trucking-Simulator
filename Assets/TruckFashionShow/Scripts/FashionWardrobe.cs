using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;
namespace LWS.TruckFashionShow
{
    public sealed partial class FashionMatch
    {
        public SportsTruckAppearanceCatalog catalog;public Transform[] shops;public Transform dressing;
        public bool WardrobeOpen=>wardrobe!=null&&wardrobe.IsOpen;public int WardrobeOwner {get;private set;}=-1;public int SavedOutfits {get;private set;}
        SportsTruckCustomizerMenu wardrobe;bool releasing,leasedMatch,leasedMenu,leaseActive;string beforeOutfit;CanvasGroup leaseGroup;bool leaseInteract,leaseRaycast;
        public bool OpenDressing(int owner)
        {
            if(!Live||Phase!=FashionPhase.Hub||owner<0||owner>=4||setup.players[owner].source==BowlingSource.AI||WardrobeOpen||!NearShop(owner))return false;
            if(wardrobe==null){var prefab=Resources.Load<GameObject>("TruckSports/SportsTruckCustomizerMenu");if(prefab==null)throw new InvalidOperationException("Existing Heat free customizer prefab required.");wardrobe=Instantiate(prefab).GetComponent<SportsTruckCustomizerMenu>();}
            WardrobeOwner=owner;beforeOutfit=JsonUtility.ToJson(SportsParticipantRegistry.GetAppearance(setup.players[owner].participantId));leaseActive=true;leasedMatch=enabled;leasedMenu=menus.enabled;leaseGroup=menus.GetComponent<CanvasGroup>();if(leaseGroup!=null){leaseInteract=leaseGroup.interactable;leaseRaycast=leaseGroup.blocksRaycasts;leaseGroup.interactable=leaseGroup.blocksRaycasts=false;}
            menus.enabled=false;enabled=false;DisposeRouters();wardrobe.Open(setup.players[owner].participantId,ClosedWardrobe);return true;
        }
        bool NearShop(int owner)=>shops.Any(t=>Vector3.ProjectOnPlane(walkers[owner].Position-t.position,Vector3.up).magnitude<7)||Vector3.ProjectOnPlane(walkers[owner].Position-dressing.position,Vector3.up).magnitude<7;
        void ClosedWardrobe()
        {
            if(releasing||this==null)return;int owner=WardrobeOwner;if(owner>=0&&beforeOutfit!=JsonUtility.ToJson(SportsParticipantRegistry.GetAppearance(setup.players[owner].participantId)))SavedOutfits++;leaseActive=false;WardrobeOwner=-1;enabled=leasedMatch;menus.enabled=leasedMenu;if(leaseGroup!=null){leaseGroup.interactable=leaseInteract;leaseGroup.blocksRaycasts=leaseRaycast;}PrepareAppearances();CreateRouters();Gate();
            if(Running&&!DevicesReady)Pause();
        }
        bool EquipAi(int owner)
        {
            var host=trucks[owner].appearanceHost;if(host==null||!host.Applied)throw new InvalidOperationException("Lead must bind actual four appearance hosts before Fashion runtime.");
            var draft=SportsParticipantRegistry.GetAppearance(setup.players[owner].participantId).Copy();string body=host.EffectiveBodyId;var slots=new[]{"hat","face.eyes","face.mouth","mirrors","wheels","rear.ornament"};
            foreach(string slot in slots)
            {
                var available=catalog.cosmetics.Where(c=>c!=null&&c.slotId==slot&&c.visualPrefab!=null&&c.CompatibleWith(body)).OrderByDescending(c=>(FashionRules.Tags(c.id)&FashionRules.ThemeTag(Theme))!=0).ThenBy(c=>c.id,StringComparer.Ordinal).ToArray();
                if(available.Length==0)continue;var item=available[(owner==0?0:owner%2)%available.Length];var selected=draft.selections.Find(s=>s.slotId==slot);if(selected==null)draft.selections.Add(new SportsTruckCosmeticSelection{slotId=slot,itemId=item.id});else{selected.itemId=item.id;selected.variantId="";}
            }
            if(!host.EvaluateDraft(draft).canApply)return false;SportsParticipantRegistry.TrySetAppearance(setup.players[owner].participantId,draft);host.Prepare(setup.players[owner].participantId,setup.players[owner].color);SavedOutfits++;return true;
        }
        public FashionItem[] ActualOutfit(int owner)
        {
            var host=trucks[owner].appearanceHost;if(host==null||!host.Applied)throw new InvalidOperationException("Only actually composed appearance items can be judged.");
            var draft=SportsParticipantRegistry.GetAppearance(setup.players[owner].participantId);var report=host.EvaluateDraft(draft);var result=new List<FashionItem>{new FashionItem(host.EffectiveBodyId,"body",FashionRules.Tags(host.EffectiveBodyId))};
            foreach(var selected in draft.selections)
            {
                if(selected.slotId==SportsTruckAppearanceSlots.Dashboard||report.unavailableSlots.Contains(selected.slotId)||!string.IsNullOrEmpty(selected.variantId))continue;var item=catalog.FindCosmetic(selected.itemId,selected.slotId,host.EffectiveBodyId);if(item!=null)result.Add(new FashionItem(item.id,item.slotId,FashionRules.Tags(item.id)));
            }
            return result.ToArray();
        }
        void ReleaseWardrobe(){releasing=true;if(wardrobe!=null){wardrobe.Close(false);Destroy(wardrobe.gameObject);wardrobe=null;}WardrobeOwner=-1;if(leaseActive){enabled=leasedMatch;if(menus!=null)menus.enabled=leasedMenu;leaseActive=false;}if(leaseGroup!=null){leaseGroup.interactable=leaseInteract;leaseGroup.blocksRaycasts=leaseRaycast;}releasing=false;}
    }
}
