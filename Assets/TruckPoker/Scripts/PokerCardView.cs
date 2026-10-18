using UnityEngine;using TMPro;
namespace LWS.TruckPoker {
public sealed class PokerCardView:MonoBehaviour {
 public GameObject face,back;public TMP_Text rank,corner;public PokerSuitGraphic suit,cornerSuit;public int Card{get;private set;}=-1;
 public void Show(int card){if(card<0||card>=52){Clear(false);return;}if(Card==card&&face.activeSelf)return;gameObject.SetActive(true);face.SetActive(true);back.SetActive(false);Card=card;int number=PokerHand.Rank(card);string value=number==14?"A":number==13?"K":number==12?"Q":number==11?"J":number.ToString();rank.text=corner.text=value;rank.color=corner.color=PokerHand.Suit(card)==1||PokerHand.Suit(card)==2?new Color(.8f,.05f,.12f):new Color(.025f,.04f,.08f);suit.SetCard(card);if(cornerSuit!=null)cornerSuit.SetCard(card);rank.havePropertiesChanged=corner.havePropertiesChanged=true;rank.ForceMeshUpdate(true,true);corner.ForceMeshUpdate(true,true);}
 // TMP owns its parse/layout caches. Clearing characterInfo by hand can leave cached glyph data inconsistent.
 static void ClearText(TMP_Text text){if(text==null)return;text.text="";text.ForceMeshUpdate(true,true);var renderer=text.GetComponent<CanvasRenderer>();if(renderer!=null)renderer.Clear();}
 public void Clear(bool hide=false){Card=-1;ClearText(rank);ClearText(corner);if(suit!=null)suit.Clear();if(cornerSuit!=null)cornerSuit.Clear();if(face!=null)face.SetActive(false);if(back!=null)back.SetActive(true);if(hide)gameObject.SetActive(false);}
 void OnDisable(){Clear(false);}
 public bool HasPrivateDisplayData=>Card>=0||rank!=null&&rank.text.Length>0||corner!=null&&corner.text.Length>0||suit!=null&&suit.Card>=0||cornerSuit!=null&&cornerSuit.Card>=0;
}
}
