using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
namespace LWS.TruckSports {
// Presentation over the existing catalogue and Heat actions. Does not launch
// scenes, route devices or own participant configuration.
public sealed class SportsCatalogueHero:MonoBehaviour {
 public Image logo,preview;public TMP_Text description,title;public CanvasGroup content;
 public string SelectedScene{get;private set;}
 Sequence transition;SportsCatalogueChoice selected;
 public void Select(SportsCatalogueChoice choice,bool immediate=false){
  if(choice==null||selected==choice)return;
  transition?.Kill();selected=choice;SelectedScene=choice.scene;
  System.Action apply=()=>{logo.sprite=choice.logo;preview.sprite=choice.preview;title.text=choice.title;description.text=choice.description;};
  if(immediate||!isActiveAndEnabled){apply();content.alpha=1;return;}
  transition=DOTween.Sequence().SetUpdate(true).Append(content.DOFade(0,.08f)).AppendCallback(()=>apply()).Append(content.DOFade(1,.18f));
 }
 void OnDisable(){transition?.Kill();selected=null;if(content!=null)content.alpha=1;}
}
}
