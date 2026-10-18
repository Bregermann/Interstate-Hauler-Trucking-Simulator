using UnityEngine;
using LWS.TruckBowling;
using Michsky.UI.Heat;
namespace LWS.TruckSports {
// Reuses the saved Heat button and Canvas styling. No replacement UI framework.
public sealed class SportsCameraSetupControl:MonoBehaviour {
 SportsMenuView menu;ButtonManager button;BowlingMatch bowling;LWS.TruckCornhole.CornholeMatch cornhole;LWS.TruckCheeseRolling.CheeseMatch cheese;SportsEventSession sport;bool previousVisible,previousValue;bool initialized;
 static bool Supported(SportsEventSession s)=>s is LWS.TruckPedestrianPlowing.PlowMatch||s is LWS.TruckPoker.PokerMatch||s is LWS.TruckDerby.DerbyMatch||s is LWS.TruckKeepaway.KeepawayMatch||s is LWS.TruckTerrierRacing.TerrierMatch||s is LWS.TruckBaseJumping.BaseJumpMatch||s is LWS.TruckBungeeDemolition.BungeeMatch||s is LWS.TruckFishing.FishingMatch||s is LWS.TruckTargetLaunch.TargetMatch||s is LWS.TruckBuzkashi.BuzMatch||s is LWS.TruckSkeeBall.SkeeMatch||s is LWS.TruckMattressRacing.MattressMatch||s is LWS.TruckWifeCarrying.WifeMatch||s is LWS.TruckBogSnorkelling.BogMatch||s is LWS.TruckMinefield.MinefieldMatch||s is LWS.TruckDodgeball.DodgeMatch;
 public static SportsCameraSetupControl Install(SportsMenuView menu){
  var bowling=FindAnyObjectByType<BowlingMatch>();var corn=FindAnyObjectByType<LWS.TruckCornhole.CornholeMatch>();var cheese=FindAnyObjectByType<LWS.TruckCheeseRolling.CheeseMatch>();var sport=FindAnyObjectByType<SportsEventSession>();
  if(bowling==null&&corn==null&&cheese==null&&!Supported(sport))return null;
  if(menu.pages==null||menu.pages.Length==0||menu.pages[0]==null)return null;
  SportsMenuAction template=null;foreach(var action in menu.pages[0].GetComponentsInChildren<SportsMenuAction>(true))if(action.button!=null){template=action;break;}if(template==null)return null;
  var copy=Instantiate(template.gameObject,menu.pages[0].transform);copy.name="Show AI Cameras";var a=copy.GetComponent<SportsMenuAction>();a.action="aicameras";
  var rect=copy.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(500,365);rect.sizeDelta=new Vector2(350,46);
  var title=menu.pages[0].transform.Find("Title") as RectTransform;if(title!=null){title.anchoredPosition=new Vector2(-205,365);title.sizeDelta=new Vector2(900,title.sizeDelta.y);}
  var control=copy.AddComponent<SportsCameraSetupControl>();control.menu=menu;control.button=a.button;control.bowling=bowling;control.cornhole=corn;control.cheese=cheese;control.sport=sport;
  foreach(var label in copy.GetComponentsInChildren<TMPro.TMP_Text>(true)){label.enableAutoSizing=true;label.fontSizeMax=23;label.fontSizeMin=18;}
  control.Refresh();return control;
 }
 bool Applicable(){if(sport is LWS.TruckPedestrianPlowing.PlowMatch plow)return plow.option!=0;if(sport is LWS.TruckPoker.PokerMatch poker)return !poker.HotSeat;if(sport is LWS.TruckDerby.DerbyMatch derby)return derby.option==0;if(bowling!=null)return bowling.setup.mode==BowlingMode.Simultaneous;if(cornhole!=null||cheese!=null)return true;if(sport is LWS.TruckKeepaway.KeepawayMatch keep)return keep.cabVariant;if(sport is LWS.TruckTerrierRacing.TerrierMatch terrier)return terrier.option==0;if(sport is LWS.TruckBuzkashi.BuzMatch buz)return buz.option==1;if(sport is LWS.TruckDodgeball.DodgeMatch dodge)return dodge.option==1;if(sport is LWS.TruckSkeeBall.SkeeMatch skee)return !skee.HotSeat;return Supported(sport);}
 public void Refresh(){bool visible=Applicable(),value=SportsCameraOptions.ShowAI;if(initialized&&visible==previousVisible&&value==previousValue)return;bool layoutChanged=initialized&&visible!=previousVisible;initialized=true;previousVisible=visible;previousValue=value;gameObject.SetActive(visible);button.buttonText="Show AI Cameras: "+(value?"ON":"OFF");button.UpdateUI();if(layoutChanged&&menu.Page==0)menu.Focus();}
}
}
