using UnityEngine;
using UnityEngine.UI;
using LWS.TruckSports;
namespace LWS.TruckPedestrianPlowing {
 [DefaultExecutionOrder(300)]public sealed class PlowMenuPresentation:MonoBehaviour {
  public PlowMatch match;
  void Start(){Apply();}
  public void Apply(){
   var menu=match.menus;
   foreach(var row in menu.rows){
    var image=row.GetComponent<Image>();if(image==null)image=row.AddComponent<Image>();image.color=SportsBroadcastCanvas.Raised;image.raycastTarget=false;
    var plate=row.GetComponent<SportsBroadcastPlate>();if(plate==null)plate=row.AddComponent<SportsBroadcastPlate>();plate.cut=14;
   }
   foreach(var image in menu.GetComponentsInChildren<Image>(true)){
    if(image.sprite!=null||image.GetComponent<TMPro.TMP_Text>()!=null)continue;
    if(image.GetComponent<Selectable>()==null&&image.name!="Card"&&!image.name.Contains("backing")&&!image.name.Contains("score card"))continue;
    var plate=image.GetComponent<SportsBroadcastPlate>();if(plate==null)plate=image.gameObject.AddComponent<SportsBroadcastPlate>();plate.cut=image.name=="Card"?24:10;
   }
   foreach(var field in menu.names){field.textComponent.enableAutoSizing=true;field.textComponent.fontSizeMax=27;field.textComponent.fontSizeMin=22;}
   var subtitle=menu.pages[0].transform.Find("Subtitle")?.GetComponent<TMPro.TMP_Text>();
   if(subtitle!=null){subtitle.text="PARADE PANDEMONIUM  /  UP TO 4 LOCAL DRIVERS + 8 TOTAL COMPETITORS";subtitle.rectTransform.anchoredPosition=new Vector2(0,310);subtitle.rectTransform.sizeDelta=new Vector2(1320,42);}
   var title=menu.pages[0].transform.Find("Title")?.GetComponent<TMPro.TMP_Text>();if(title!=null){title.text=match.cabExperience?"PLOWING · CAB CHAOS":"PEDESTRIAN PLOWING";title.fontSizeMax=42;}
  }
 }
}
