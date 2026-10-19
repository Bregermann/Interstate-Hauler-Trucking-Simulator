using UnityEngine;
using DG.Tweening;
using LWS.TruckSports;
namespace LWS.TruckDanceOff {
// Deliberate stage choreography. NWH remains the truck/cab/wheel foundation;
// this stationary rhythm sport moves its authoritative kinematic pose, never
// the note clock or score. Separate platforms prevent inter-player impacts.
public sealed class DanceTruckRig:MonoBehaviour {
 public SportsEventTruck truck;public Transform deck;public Light accent;public ParticleSystem sparks;
 public string Routine{get;private set;}="READY";public int RoutineChanges{get;private set;}
 Vector3 home,entryOffset;Quaternion upright,entryRotation;Tween flourish;float blend;int style,mask;double began,beat=.5;bool performing,initialized;
 void Awake(){home=transform.position;upright=transform.rotation;initialized=true;}
 public void Begin(Color color){if(!initialized){home=transform.position;upright=transform.rotation;initialized=true;}truck.Prepare(home,upright,false);truck.Power(false);truck.body.isKinematic=true;truck.body.detectCollisions=true;Routine="READY";RoutineChanges=0;entryOffset=Vector3.zero;entryRotation=Quaternion.identity;blend=1;style=0;began=0;performing=true;if(accent!=null)accent.color=color;}
 public void Hit(DanceGrade grade,int combo,int lanes,double at,double beatSeconds,bool freestyle=false){
  if(grade==DanceGrade.Miss)style=0;else if(freestyle||combo>=24)style=9+(combo/4+lanes)%13;else style=1+combo%(combo<8?5:8);
  Routine=new[]{"SHY SWAY","BEAR UP","BOUNCY BOUNCE","HAPPY NOD","BUTT WIGGLE","TIPPY TAPS","PLAYFUL MINI DONUT","CELEBRATION BOUNCE","PEEKABOO LEAN","DIESEL DONUT","SIX-TON WINDMILL","HEADSPIN","JACKHAMMER","WINDSHIELD WIPER","FINAL DROP","WHEELIE","STOPPIE","SIDE SLIDE","HOP","TWO-WHEEL BALANCE","SPIN","ROOF SPIN"}[style];
  entryOffset=truck.body.position-home;entryRotation=Quaternion.Inverse(upright)*truck.body.rotation;
  RoutineChanges++;mask=lanes;began=at;beat=beatSeconds;flourish?.Kill();blend=0;flourish=DOTween.To(()=>blend,v=>blend=v,1,.12f).SetEase(Ease.OutBack).SetLink(gameObject);
  if(sparks!=null&&grade==DanceGrade.Perfect&&combo%8==0)sparks.Play();
 }
 public void Pose(double songTime){if(!performing)return;float t=(float)((songTime-began)/beat),a=Mathf.Sin(t*Mathf.PI*2),b=Mathf.Sin(t*Mathf.PI);float age=Mathf.Max(0,t);Vector3 offset=Vector3.zero,euler=Vector3.zero;float strength=Mathf.Lerp(.25f,1,blend);
  switch(style){case 0:euler.z=a*3;break;case 1:offset.y=Mathf.Max(0,b)*.24f;euler.x=-Mathf.Abs(b)*13;break;case 2:offset.y=Mathf.Abs(a)*.24f;euler.x=a*7;break;case 3:euler.x=a*11;break;case 4:euler.y=a*12;euler.z=b*5;break;case 5:offset.x=a*.22f;euler.z=a*9;offset.y=Mathf.Abs(a)*.12f;break;case 6:euler.y=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/2))*180;break;case 7:offset.y=Mathf.Abs(a)*.3f;euler=new Vector3(b*8,a*7,a*4);break;case 8:offset.x=b*.25f;euler.z=b*12;break;case 9:euler.y=age*180;offset=new Vector3(Mathf.Cos(t*2)*.5f,.1f,Mathf.Sin(t*2)*.5f);break;case 10:offset.y=2.4f;euler=new Vector3(12,age*180,150);break;case 11:offset.y=2.7f;euler=new Vector3(0,age*210,180);break;case 12:offset.y=1.8f+Mathf.Abs(a)*.4f;euler.x=75;break;case 13:offset.y=2;euler=new Vector3(0,a*50,150);break;case 15:euler.x=-30*Mathf.Abs(b);offset.y=.4f;break;case 16:euler.x=30*Mathf.Abs(b);offset.y=.4f;break;case 17:offset.x=a*.9f;euler.y=22;break;case 18:offset.y=Mathf.Abs(b)*.65f;break;case 19:euler.z=28*b;offset.y=.5f;break;case 20:euler.y=t*180;offset.y=.4f;break;case 21:offset.y=2.7f;euler=new Vector3(0,t*210,180);break;default:offset.y=Mathf.Max(0,1-Mathf.Clamp01(t/2))*2.5f;euler=new Vector3(a*8,0,a*6);break;}
  // No unbounded integrated rotation or collision teleport across platforms.
  float recovery=Mathf.SmoothStep(0,1,Mathf.InverseLerp(2,4,t));offset=Vector3.Lerp(offset,Vector3.zero,recovery);var pose=Quaternion.Slerp(Quaternion.Euler(euler),Quaternion.identity,recovery);truck.body.MovePosition(home+Vector3.Lerp(entryOffset,offset,blend));truck.body.MoveRotation(upright*Quaternion.Slerp(entryRotation,pose,blend));
  if(deck!=null)deck.localRotation=Quaternion.Euler(0,0,a*.6f);if(accent!=null)accent.intensity=2+Mathf.Max(0,a)*2;
 }
 public void Stop(){performing=false;flourish?.Kill();truck.Power(false);truck.StopSound();truck.body.isKinematic=true;if(initialized){truck.body.position=home;truck.body.rotation=upright;}if(deck!=null)deck.localRotation=Quaternion.identity;}
 void OnDestroy(){flourish?.Kill();}
}
}
