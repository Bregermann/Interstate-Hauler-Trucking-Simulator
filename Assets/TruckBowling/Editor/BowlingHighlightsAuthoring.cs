using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
namespace LWS.TruckBowling.Editor {
 public static class BowlingHighlightsAuthoring {
  public static void Upgrade(){var scene=EditorSceneManager.OpenScene(BowlingAuthoring.Scene);Install(Object.FindAnyObjectByType<BowlingMatch>());EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
  public static void Install(BowlingMatch match){
   foreach(var lane in match.lanes){if(lane.highlights!=null){lane.highlights.celebration.anchoredPosition=new Vector2(0,205);foreach(var actor in lane.highlights.mascots){if(actor.GetComponent<CanvasRenderer>()==null)actor.gameObject.AddComponent<CanvasRenderer>();actor.SetAllDirty();}EditorUtility.SetDirty(lane.highlights.celebration);continue;}var highlight=lane.gameObject.AddComponent<BowlingHighlights>();lane.highlights=highlight;highlight.lane=lane;var replay=lane.gameObject.AddComponent<SportsMotionReplay>();highlight.replay=replay;replay.capacity=1000;replay.interval=.05f;replay.rolling=true;replay.maximumReplayAge=15;replay.sources=new Transform[11];replay.ghosts=new Transform[11];replay.sources[0]=lane.truck.transform;for(int i=0;i<10;i++)replay.sources[i+1]=lane.pins[i].transform;
    for(int i=0;i<11;i++){var copy=SportsReplayVisuals.Copy(replay.sources[i]);copy.name="Bowling recorded visual lane "+lane.index+" object "+i;foreach(var part in copy.GetComponentsInChildren<Transform>(true))part.gameObject.layer=26+lane.index;copy.SetActive(false);replay.ghosts[i]=copy.transform;}
    int layers=(1<<26)|(1<<27)|(1<<28)|(1<<29);lane.cab.view.cullingMask=(lane.cab.view.cullingMask&~layers)|(1<<(26+lane.index));
    var card=SportsUIAuthoring.Rect("Original alley pin-character celebration",match.menus.viewRoots[lane.index],new Vector2(500,170),new Vector2(0,205));var rect=card.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);rect.pivot=new Vector2(.5f,.5f);highlight.celebration=rect;var image=card.AddComponent<Image>();image.color=SportsBroadcastCanvas.Ink;image.raycastTarget=false;var plate=card.AddComponent<SportsBroadcastPlate>();plate.cut=22;plate.accent=true;highlight.group=card.AddComponent<CanvasGroup>();highlight.group.alpha=0;highlight.group.blocksRaycasts=false;
    highlight.mascots=new BowlingPinMascot[2];for(int i=0;i<2;i++){var actor=SportsUIAuthoring.Rect("Original expressive pin "+i,rect,new Vector2(60,130),new Vector2(i==0?-175:-115,-57));highlight.mascots[i]=actor.AddComponent<BowlingPinMascot>();highlight.mascots[i].raycastTarget=false;}
    highlight.title=SportsUIAuthoring.Text("Pin-character headline",rect,"STRIKE!",new Vector2(285,64),new Vector2(62,25),38);highlight.title.fontSizeMin=22;highlight.title.color=SportsBroadcastCanvas.Orange;highlight.caption=SportsUIAuthoring.Text("Pin-character punchline",rect,"TEN PINS. ZERO MERCY.",new Vector2(290,58),new Vector2(65,-34),21);highlight.caption.fontSizeMin=16;EditorUtility.SetDirty(lane);EditorUtility.SetDirty(highlight);
   }
   if(match.menus.pages[0].GetComponentsInChildren<SportsMenuAction>(true).All(a=>a.action!="highlights"))SportsUIAuthoring.Button(match.menus.pages[0].transform,"REPLAYS: NOTABLE SHOTS","highlights",new Vector2(0,310),new Vector2(650,44));
   EditorUtility.SetDirty(match.menus);
  }
 }
}
