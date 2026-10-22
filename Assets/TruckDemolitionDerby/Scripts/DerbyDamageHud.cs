using LWS.TruckSports;
using TMPro;
using UnityEngine;

namespace LWS.TruckDerby
{
    // Adds the sport's two health readings to the existing shared cards.
    // DerbyDamageState remains the only damage authority.
    [DefaultExecutionOrder(300)]
    public sealed class DerbyDamageHud : MonoBehaviour
    {
        public DerbyMatch match;
        public Sprite engineIcon,bodyIcon;
        public TMP_FontAsset font;
        public int authoredVersion;
        sealed class Reading
        {
            public RectTransform root;
            public TMP_Text engine,body,warning;
            public UnityEngine.UI.Image engineImage,bodyImage;
            public Transform oldDetail;
            public int priorEngine=-1,priorBody=-1;
        }
        readonly Reading[] shared=new Reading[8],views=new Reading[4];
        Transform sharedRoot;
        int layoutCount=-1;
        static RectTransform Rect(string name,Transform parent,Vector2 at,Vector2 size)
        {
            var node=new GameObject(name,typeof(RectTransform));node.transform.SetParent(parent,false);
            var r=(RectTransform)node.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=at;r.sizeDelta=size;return r;
        }
        TMP_Text Text(Transform parent,string name,Vector2 at,Vector2 size)
        {
            var text=Rect(name,parent,at,size).gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.color=SportsBroadcastCanvas.Cream;text.fontSize=24;text.enableAutoSizing=false;text.alignment=TextAlignmentOptions.MidlineLeft;text.raycastTarget=false;text.overflowMode=TextOverflowModes.Overflow;return text;
        }
        UnityEngine.UI.Image Icon(Transform parent,string name,Sprite sprite,float x)
        {
            var image=Rect(name,parent,new Vector2(x,-1),new Vector2(24,24)).gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;return image;
        }
        Reading Build(Transform parent,bool split)
        {
            var r=new Reading();r.root=Rect("Derby actual engine and body health",parent,new Vector2(split?12:55,split?-76:-30),new Vector2(split?246:380,28));
            r.engineImage=Icon(r.root,"NWH engine health icon",engineIcon,0);r.engine=Text(r.root,"Engine remaining percent",new Vector2(30,0),new Vector2(66,28));
            r.bodyImage=Icon(r.root,"Heat body health icon",bodyIcon,104);r.body=Text(r.root,"Body remaining percent",new Vector2(134,0),new Vector2(66,28));
            r.warning=Text(r.root,"Stopped fire countdown",new Vector2(218,0),new Vector2(156,28));r.warning.fontSize=20;r.warning.gameObject.SetActive(!split);return r;
        }
        public static int RemainingPercent(float damage)=>Mathf.RoundToInt((1-Mathf.Clamp01(damage))*100);
        void Draw(Reading r,int slot)
        {
            var fighter=match.fighters[slot];int engine=RemainingPercent(fighter.damage.Engine),body=RemainingPercent(fighter.damage.Structure);
            if(engine!=r.priorEngine){r.engine.text=engine+"%";r.priorEngine=engine;}
            if(body!=r.priorBody){r.body.text=body+"%";r.priorBody=body;}
            r.engineImage.color=r.engine.color=engine<30?new Color(1,.42f,.22f):SportsBroadcastCanvas.Cream;
            r.bodyImage.color=r.body.color=body<30?new Color(1,.42f,.22f):SportsBroadcastCanvas.Cream;
            r.warning.text=!match.rules.alive[slot]?"OUT":fighter.motion.Stopped>1?"MOVE! "+Mathf.Max(0,5-fighter.motion.Stopped).ToString("F1")+"s":"";
        }
        void LateUpdate()
        {
            if(match==null||font==null)return;
            bool playing=match.menus.Screen==SportsEventScreen.Play;
            if(sharedRoot==null)sharedRoot=match.menus.hud.transform.Find("Broadcast participant cards");
            if(sharedRoot!=null&&layoutCount!=match.setup.count)
            {
                layoutCount=match.setup.count;var rect=(RectTransform)sharedRoot;int rows=Mathf.CeilToInt(layoutCount/4f);rect.anchoredPosition=new Vector2(0,76);rect.sizeDelta=new Vector2(1860,rows*65);
                for(int i=0;i<8;i++){var card=sharedRoot.Find("Participant card "+i) as RectTransform;if(card!=null)card.anchoredPosition=new Vector2(i%4*465,-i/4*65);}
            }
            if(sharedRoot!=null&&sharedRoot.gameObject.activeInHierarchy)
                for(int slot=0;slot<match.setup.count;slot++)
                {
                    // Shared component owns its card geometry, name, portrait,
                    // wins and stop-progress. Only replace its long detail line.
                    if(shared[slot]==null){var card=sharedRoot.Find("Participant card "+slot);if(card==null)continue;shared[slot]=Build(card,false);shared[slot].oldDetail=card.Find("Status");}
                    if(shared[slot].oldDetail!=null)shared[slot].oldDetail.gameObject.SetActive(false);shared[slot].root.gameObject.SetActive(playing);Draw(shared[slot],slot);
                }
            if(!match.MultiViewHUD){foreach(var reading in views)if(reading!=null)reading.root.gameObject.SetActive(false);return;}
            for(int view=0;view<views.Length&&view<match.menus.viewPanels.Length;view++)
            {
                var card=views[view]!=null?views[view].root.parent as RectTransform:match.menus.viewPanels[view].Find("Compact owned viewport score") as RectTransform;
                if(card==null||!card.gameObject.activeInHierarchy){if(views[view]!=null)views[view].root.gameObject.SetActive(false);continue;}
                int slot=match.ViewParticipant(view);if(slot<0)continue;
                if(views[view]==null)views[view]=Build(card,true);card.sizeDelta=new Vector2(270,106);views[view].root.gameObject.SetActive(playing);Draw(views[view],slot);
            }
        }
    }
}
