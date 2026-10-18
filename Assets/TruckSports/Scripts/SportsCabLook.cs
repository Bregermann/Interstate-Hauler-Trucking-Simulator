using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using LWS.TruckBowling;
namespace LWS.TruckSports {
// Owned source routing over Cinemachine's axis acceleration, limits and recentering.
// Call once after the sport computes its stable cab rotation. This never reads or writes drive inputs.
public sealed class SportsCabLook:MonoBehaviour {
 public float Yaw=>horizontal.Value;public float Pitch=>vertical.Value;public int Owner{get;private set;}=-1;
 AxisState horizontal=new AxisState(-170,170,false,false,125,.09f,.12f,"",false);
 AxisState vertical=new AxisState(-65,55,false,false,95,.09f,.12f,"",false);
 AxisState.Recentering recenterX=new AxisState.Recentering(true,0,.28f),recenterY=new AxisState.Recentering(true,0,.28f);
 BowlingPlayer prior;BowlingSource priorSource;int priorDevice=-2;bool neutral,captured,wasLive;CursorLockMode oldLock;bool oldVisible;int updatedFrame=-1;
 public Quaternion Apply(Quaternion basis,BowlingPlayer player,int owner,bool mouseOwner,bool live,float sensitivity,bool invertY){
  int device=player!=null&&player.pad!=null?player.pad.deviceId:-1;if(prior!=player||Owner!=owner||player!=null&&(priorSource!=player.source||priorDevice!=device)){prior=player;Owner=owner;priorSource=player!=null?player.source:BowlingSource.AI;priorDevice=device;ResetLook();}bool focused=Application.isFocused;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
 focused|=SportsProbeInputIsolation.Active;
#endif
 if(player!=null&&(player.source==BowlingSource.LeftStick||player.source==BowlingSource.RightStick||player.source==BowlingSource.Keyboard&&!mouseOwner)){ResetLook();return basis;}
  bool available=live&&focused&&player!=null&&player.source!=BowlingSource.AI;if(!available){wasLive=false;neutral=false;horizontal.Reset();vertical.Reset();ReleaseCursor();return basis*Quaternion.Euler(Pitch,Yaw,0);}
  if(!wasLive){horizontal.Reset();vertical.Reset();neutral=false;wasLive=true;}
  if(updatedFrame==Time.frameCount)return basis*Quaternion.Euler(Pitch,Yaw,0);updatedFrame=Time.frameCount;
  Vector2 input=Vector2.zero;bool mouse=player.source==BowlingSource.Keyboard&&mouseOwner&&Mouse.current!=null&&Mouse.current.added&&Mouse.current.enabled;bool pad=player.source==BowlingSource.WholeController&&player.pad!=null&&player.pad.added&&player.pad.enabled;bool recenter=mouse&&Mouse.current.middleButton.wasPressedThisFrame||pad&&player.pad.rightStickButton.wasPressedThisFrame;
  bool mouseHeld=mouse&&Mouse.current.rightButton.isPressed;var stick=pad?player.pad.rightStick.ReadValue():Vector2.zero;if(!neutral){if(!mouseHeld&&stick.sqrMagnitude<.015f)neutral=true;ReleaseCursor();return basis*Quaternion.Euler(Pitch,Yaw,0);}
  if(mouseHeld){CaptureCursor();input=Mouse.current.delta.ReadValue();}else{ReleaseCursor();if(pad)input=stick.magnitude>.14f?stick:Vector2.zero;}
  horizontal.m_SpeedMode=vertical.m_SpeedMode=mouseHeld?AxisState.SpeedMode.InputValueGain:AxisState.SpeedMode.MaxSpeed;horizontal.m_MaxSpeed=mouseHeld?.14f*sensitivity:125*sensitivity;vertical.m_MaxSpeed=mouseHeld?.11f*sensitivity:95*sensitivity;horizontal.m_AccelTime=vertical.m_AccelTime=mouseHeld?0:.09f;horizontal.m_DecelTime=vertical.m_DecelTime=mouseHeld?0:.12f;horizontal.m_InputAxisValue=input.x;vertical.m_InputAxisValue=input.y*(invertY?1:-1);horizontal.Update(Time.deltaTime);vertical.Update(Time.deltaTime);
  if(recenter){
   // Stop residual axis inertia, then prime Cinemachine's render-time clock without moving the view.
   // Otherwise the first recenter after a long idle snaps to zero and leftover speed drifts it away again.
   horizontal.Reset();vertical.Reset();
   recenterX.DoRecentering(ref horizontal,0,horizontal.Value);recenterY.DoRecentering(ref vertical,0,vertical.Value);
   recenterX.RecenterNow();recenterY.RecenterNow();
  }
  if(recenter||input.sqrMagnitude<.0001f&&recentering){recentering=true;recenterX.DoRecentering(ref horizontal,Time.deltaTime,0);recenterY.DoRecentering(ref vertical,Time.deltaTime,0);if(Mathf.Abs(Yaw)+Mathf.Abs(Pitch)<.05f)recentering=false;}else{recentering=false;recenterX.CancelRecentering();recenterY.CancelRecentering();}
  return basis*Quaternion.Euler(Pitch,Yaw,0);
 }
 bool recentering;
 public void ResetLook(){horizontal.Reset();vertical.Reset();horizontal.Value=vertical.Value=0;neutral=false;recentering=false;ReleaseCursor();}
 void CaptureCursor(){if(captured)return;captured=true;oldLock=Cursor.lockState;oldVisible=Cursor.visible;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
 public void ReleaseCursor(){if(!captured)return;captured=false;Cursor.lockState=oldLock;Cursor.visible=oldVisible;}
 void OnApplicationFocus(bool focus){if(!focus){neutral=false;ReleaseCursor();}}
 void OnDisable(){wasLive=false;neutral=false;ReleaseCursor();}void OnDestroy(){ReleaseCursor();}
}
}
