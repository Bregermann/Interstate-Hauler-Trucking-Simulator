using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using TMPro;
using LWS.TruckSports;
namespace LWS.TruckBowling {
 public sealed class BowlingHighlights:MonoBehaviour {
  public BowlingLane lane;public SportsMotionReplay replay;public RectTransform celebration;public CanvasGroup group;public TMP_Text title,caption;public BowlingPinMascot[] mascots;
  public bool Playing=>replay!=null&&replay.Playing;public int Replays{get;private set;}public int Celebrations{get;private set;}
  public const string Preference="TruckBowling.Highlights.v1";public static int Policy=>Mathf.Clamp(SportsPrefs.GetInt(Preference,1),0,2);public static string PolicyName=>new[]{"OFF","NOTABLE SHOTS","EVERY SHOT"}[Policy];
  Sequence animation;bool pending,consumed,neutral;float clock,lastInput;Renderer[] pinRenderers;int previousChoice;
  void Awake(){var list=new System.Collections.Generic.List<Renderer>();foreach(var pin in lane.pins)list.AddRange(pin.GetComponentsInChildren<Renderer>(true));pinRenderers=list.ToArray();}
  public void BeginDelivery(){animation?.Kill();End();clock=0;pending=consumed=false;replay.ResetRecord();if(group!=null)group.alpha=0;}
  public void Record(float dt){clock+=dt;int mask=1;for(int i=0;i<10;i++)if(lane.pins[i].gameObject.activeInHierarchy)mask|=1<<(i+1);replay.Record(clock,mask);}
  public void Complete(string kind){Record(.051f);bool split=IsSplit();pending=Policy==2||Policy==1&&(kind=="STRIKE"||kind=="SPARE"||kind=="GUTTER"||split||lane.LastPins>=9);Celebrate(kind,split);}
  bool IsSplit(){if(lane.LastPins<=0||lane.LastPins>=9)return false;bool left=false,right=false,head=false;for(int i=0;i<10;i++)if(!lane.pins[i].credited){float x=lane.pins[i].body.position.x-lane.center;left|=x<-.5f;right|=x>.5f;if(i==0)head=true;}return !head&&left&&right;}
  public bool TryBegin(){if(!pending||consumed||lane.Owner.source==BowlingSource.AI&&!lane.cab.view.enabled)return false;consumed=true;replay.Begin(Mathf.Max(0,replay.Duration-10));if(!replay.Playing)return false;replay.Speed=lane.LastPins>=9?.5f:1;neutral=false;lastInput=0;previousChoice=0;Replays++;return true;}
  public bool Step(float dt){var p=lane.Owner;Vector2 drive=lane.match.Drive(lane.ownerSlot);bool skip=p.source==BowlingSource.Keyboard?SportsDeviceInput.KeyHeld(p.forward)&&SportsDeviceInput.KeyHeld(p.back):p.pad!=null&&(p.source==BowlingSource.WholeController?p.pad.buttonSouth.isPressed:p.source==BowlingSource.LeftStick?p.pad.leftTrigger.isPressed:p.pad.rightTrigger.isPressed);
   if(!neutral){if(drive.sqrMagnitude<.02f&&!skip)neutral=true;}else if(skip){End();return true;}else{int choice=Mathf.Abs(drive.x)>.6f?(drive.x>0?1:-1):Mathf.Abs(drive.y)>.6f?(drive.y>0?2:-2):0;if(choice!=0&&previousChoice==0&&Time.time-lastInput>.15f){lastInput=Time.time;if(Mathf.Abs(choice)==1)replay.Angle=(replay.Angle+(choice>0?1:2))%3;else replay.Speed=replay.Speed<.75f?1:replay.Speed<1.5f?2:.35f;}previousChoice=choice;}
   if(replay.Step(dt)){End();return true;}return false;
  }
  public void ReplayCamera(Transform camera,float dt){Vector3 truck=replay.Position(0),sum=Vector3.zero;int count=0;for(int i=1;i<=10;i++){if(!replay.ghosts[i].gameObject.activeSelf)continue;sum+=replay.Position(i);count++;}Vector3 pins=count>0?sum/count:new Vector3(lane.center,.5f,lane.pinLine);float k=Mathf.InverseLerp(lane.pinLine-10,lane.pinLine,truck.z);Vector3 focus=Vector3.Lerp(truck+Vector3.up*.2f,pins,k);focus.y=Mathf.Max(.5f,focus.y);Vector3 offset=replay.Angle==0?new Vector3(2.7f,3.7f,-6):replay.Angle==1?new Vector3(-3,2.7f,3.5f):new Vector3(0,7.5f,-1.5f);Vector3 goal=focus+offset;goal.x=Mathf.Clamp(goal.x,lane.center-3.2f,lane.center+3.2f);goal.y=Mathf.Clamp(goal.y,1.1f,7.6f);goal.z=Mathf.Clamp(goal.z,-21,lane.pinLine+7);float blend=1-Mathf.Exp(-dt*7);camera.position=Vector3.Lerp(camera.position,goal,blend);camera.rotation=Quaternion.Slerp(camera.rotation,Quaternion.LookRotation(focus-camera.position,Vector3.up),blend);}
  public void HideLivePins(bool hide){if(pinRenderers!=null)foreach(var renderer in pinRenderers)if(renderer!=null)renderer.forceRenderingOff=hide;}
  void Celebrate(string kind,bool split){if(celebration==null)return;animation?.Kill();Celebrations++;title.text=kind=="STRIKE"?"STRIKE!":kind=="SPARE"?"SPARE!":kind=="GUTTER"?"GUTTER CLUB":split?"SPLIT HAPPENS":lane.LastPins==9?"ONE PIN. REALLY?":lane.LastPins==0?"THE PINS SURVIVED":kind;
   caption.text=kind=="STRIKE"?"TEN PINS. ZERO MERCY.":kind=="SPARE"?"CLEANED UP NICELY.":kind=="GUTTER"?"SCENIC ROUTE TO THE PIT.":split?"YOU GO LEFT. I'LL PANIC RIGHT.":lane.LastPins==9?"THAT ONE HAS PLANS.":"NEXT FRAME, BIG RIG.";
   group.alpha=0;celebration.localScale=Vector3.one*.8f;animation=DOTween.Sequence().Append(group.DOFade(1,.16f)).Join(celebration.DOScale(1,.32f).SetEase(Ease.OutBack));
   for(int i=0;i<mascots.Length;i++){var t=mascots[i].rectTransform;t.anchoredPosition=new Vector2(i==0?-190:-125,-4);t.localRotation=Quaternion.identity;t.localScale=Vector3.one;mascots[i].mood=kind=="GUTTER"?2:split?3:0;mascots[i].SetVerticesDirty();animation.Insert(.16f,t.DOAnchorPosY(14,.24f).SetLoops(6,LoopType.Yoyo).SetEase(Ease.OutQuad));animation.Insert(.1f,t.DOLocalRotate(new Vector3(0,0,(i==0?1:-1)*(split?25:kind=="STRIKE"?360:18)),kind=="STRIKE"?.8f:.25f,RotateMode.FastBeyond360).SetLoops(kind=="STRIKE"?2:6,LoopType.Yoyo));}
   animation.Insert(2.8f,group.DOFade(0,.25f));
  }
  public void End(){if(replay!=null)replay.End();HideLivePins(false);}
  void OnDisable(){animation?.Kill();End();}void OnDestroy(){animation?.Kill();End();}
 }
}
