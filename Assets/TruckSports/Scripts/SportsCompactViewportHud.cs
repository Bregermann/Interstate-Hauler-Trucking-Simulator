using DG.Tweening;
using TMPro;
using UnityEngine;

namespace LWS.TruckSports
{
    // Presentation only: reuse the existing viewport roots, font assets,
    // branded plate and portraits. Each sport supplies its actual owner/data.
    [DefaultExecutionOrder(240)]
    public sealed class SportsCompactViewportHud : MonoBehaviour
    {
        public SportsEventMenus menus;
        sealed class Card
        {
            public RectTransform root;
            public TMP_Text name,score,detail;
            public UnityEngine.UI.Image portrait,stripe;
            public string previous;
            public RectTransform promptRoot;public TMP_Text prompt;public CanvasGroup promptAlpha;
        }
        readonly Card[] cards=new Card[4];

        public static Vector2 InnerAnchor(Rect viewport)
        {
            if(viewport.width<=.001f||viewport.height<=.001f)return Vector2.zero;
            if(viewport.width>=.99f&&viewport.height>=.99f)return Vector2.zero;
            return new Vector2(Mathf.Clamp01((.5f-viewport.x)/viewport.width),Mathf.Clamp01((.5f-viewport.y)/viewport.height));
        }
        static UnityEngine.UI.Image Image(Transform parent,string name,Vector2 at,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=at;rect.sizeDelta=size;
            var image=go.GetComponent<UnityEngine.UI.Image>();image.raycastTarget=false;return image;
        }
        static TMP_Text Text(Transform parent,string name,TMP_Text source,Vector2 at,Vector2 size,int fontSize)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
            var text=go.GetComponent<TMP_Text>();text.font=source.font;text.fontSharedMaterial=source.fontSharedMaterial;text.color=SportsBroadcastCanvas.Cream;text.fontSizeMax=fontSize;text.fontSizeMin=fontSize-2;text.enableAutoSizing=true;text.raycastTarget=false;text.overflowMode=TextOverflowModes.Ellipsis;text.alignment=TextAlignmentOptions.MidlineLeft;
            var rect=text.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=at;rect.sizeDelta=size;return text;
        }
        Card Build(int view)
        {
            var old=menus.viewTexts[view];var panel=menus.viewPanels[view];
            var image=Image(panel,"Compact owned viewport score",Vector2.zero,new Vector2(270,76));image.color=SportsBroadcastCanvas.Ink;image.gameObject.AddComponent<SportsBroadcastPlate>().cut=12;
            var card=new Card{root=image.rectTransform};
            card.stripe=Image(card.root,"Owner color",new Vector2(0,-8),new Vector2(4,60));
            card.portrait=Image(card.root,"Truck portrait",new Vector2(10,-31),new Vector2(36,36));card.portrait.sprite=Resources.Load<Sprite>("Broadcast truck portrait");card.portrait.preserveAspect=true;
            card.name=Text(card.root,"Participant name",old,new Vector2(12,-4),new Vector2(246,26),22);card.name.fontStyle=FontStyles.Bold;
            card.score=Text(card.root,"Owned score",old,new Vector2(53,-29),new Vector2(94,28),25);card.score.fontStyle=FontStyles.Bold;
            card.detail=Text(card.root,"Race status",old,new Vector2(151,-32),new Vector2(108,34),17);
            return cards[view]=card;
        }
        void Context(Card card,string value,Vector2 anchor,int view)
        {
            if(string.IsNullOrEmpty(value)){if(card.promptRoot!=null){card.promptAlpha.DOKill();card.promptRoot.gameObject.SetActive(false);}return;}
            if(card.promptRoot==null){var image=Image(card.root,"Contextual action",Vector2.zero,new Vector2(270,54));image.color=SportsBroadcastCanvas.Ink;image.gameObject.AddComponent<SportsBroadcastPlate>().cut=10;card.promptRoot=image.rectTransform;card.promptAlpha=image.gameObject.AddComponent<CanvasGroup>();card.prompt=Text(card.promptRoot,"Assigned action",menus.viewTexts[view],new Vector2(12,-5),new Vector2(246,44),20);card.prompt.alignment=TextAlignmentOptions.Center;card.promptRoot.gameObject.SetActive(false);}
            card.promptRoot.pivot=anchor.y<.5f?Vector2.zero:new Vector2(0,1);card.promptRoot.anchoredPosition=anchor.y<.5f?new Vector2(0,8):new Vector2(0,-84);card.prompt.text=value;
            if(!card.promptRoot.gameObject.activeSelf){card.promptRoot.gameObject.SetActive(true);card.promptAlpha.alpha=0;card.promptAlpha.DOFade(1,.12f).SetUpdate(true).SetLink(card.promptRoot.gameObject);}
        }
        void LateUpdate()
        {
            if(menus==null||menus.match==null)return;
            var match=menus.match;
            for(int view=0;view<cards.Length&&view<menus.viewPanels.Length;view++)
            {
                int owner=-1;string score=null,detail=null;
                bool active=menus.Screen==SportsEventScreen.Play&&match.MultiViewHUD&&view<match.cameras.Length&&match.cameras[view]!=null&&match.cameras[view].enabled&&match.TryReadViewportCard(view,out owner,out score,out detail)&&owner>=0&&owner<match.setup.count;
                if(!active){if(cards[view]!=null)cards[view].root.gameObject.SetActive(false);continue;}
                var card=cards[view]??Build(view);card.root.gameObject.SetActive(true);
                bool hasDetail=!string.IsNullOrEmpty(detail);card.root.sizeDelta=hasDetail?new Vector2(270,76):new Vector2(230,72);card.name.rectTransform.sizeDelta=new Vector2(hasDetail?246:206,26);card.score.rectTransform.sizeDelta=new Vector2(hasDetail?94:166,28);
                menus.viewTexts[view].gameObject.SetActive(false);var obsolete=menus.viewPanels[view].Find("View data backing");if(obsolete!=null)obsolete.gameObject.SetActive(false);
                var anchor=InnerAnchor(match.cameras[view].rect);card.root.anchorMin=card.root.anchorMax=card.root.pivot=anchor;
                card.root.anchoredPosition=new Vector2(anchor.x<.01f?10:anchor.x>.99f?-10:0,anchor.y<.01f?10:anchor.y>.99f?-10:0);
                var color=match.ViewportCardColor(view,owner);card.name.text=match.ViewportCardLabel(view,owner);card.name.color=card.stripe.color=color;card.portrait.color=Color.Lerp(color,Color.white,.3f);
                if(card.previous!=null&&card.previous!=score){card.score.transform.DOKill();card.score.transform.localScale=Vector3.one;card.score.transform.DOPunchScale(Vector3.one*.12f,.25f,2,.4f).SetUpdate(true).SetLink(card.root.gameObject);}
                card.previous=score;card.score.text=score;card.detail.text=detail;card.detail.gameObject.SetActive(hasDetail);
                Context(card,match.ViewportContextPrompt(view),anchor,view);
            }
        }
        void OnDisable(){for(int view=0;view<cards.Length;view++)if(cards[view]!=null){var card=cards[view];card.score.transform.DOKill();card.score.transform.localScale=Vector3.one;if(card.promptAlpha!=null)card.promptAlpha.DOKill();card.root.gameObject.SetActive(false);if(menus!=null){menus.viewTexts[view].gameObject.SetActive(true);var old=menus.viewPanels[view].Find("View data backing");if(old!=null)old.gameObject.SetActive(true);}}}
    }
}
