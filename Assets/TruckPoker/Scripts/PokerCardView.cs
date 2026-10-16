using UnityEngine;using TMPro;
namespace LWS.TruckPoker {
public sealed class PokerCardView:MonoBehaviour {
 public GameObject face,back;public TMP_Text rank,corner;public PokerSuitGraphic suit;public int Card{get;private set;}=-1;
 public void Show(int card){if(card<0){Clear(false);return;}if(Card==card&&face.activeSelf)return;gameObject.SetActive(true);face.SetActive(true);back.SetActive(false);Card=card;string value="23456789TJQKA"[card%13].ToString();rank.text=corner.text=value;rank.color=corner.color=PokerHand.Suit(card)==1||PokerHand.Suit(card)==2?new Color(.8f,.05f,.12f):new Color(.025f,.04f,.08f);suit.SetCard(card);rank.havePropertiesChanged=corner.havePropertiesChanged=true;rank.ForceMeshUpdate(true);corner.ForceMeshUpdate(true);}
 static void ClearText(TMP_Text text){if(text==null)return;text.text="";if(text.textInfo!=null){if(text.textInfo.characterInfo!=null)System.Array.Clear(text.textInfo.characterInfo,0,text.textInfo.characterInfo.Length);text.textInfo.characterCount=0;}if(text.mesh!=null)text.mesh.Clear();var renderer=text.GetComponent<CanvasRenderer>();if(renderer!=null)renderer.Clear();}
 public void Clear(bool hide=false){Card=-1;ClearText(rank);ClearText(corner);if(suit!=null)suit.Clear();if(face!=null)face.SetActive(false);if(back!=null)back.SetActive(true);if(hide)gameObject.SetActive(false);}
 void OnDisable(){Clear(false);}
 public bool HasPrivateDisplayData=>Card>=0||rank!=null&&rank.text.Length>0||corner!=null&&corner.text.Length>0||suit!=null&&suit.Card>=0;
}
}
