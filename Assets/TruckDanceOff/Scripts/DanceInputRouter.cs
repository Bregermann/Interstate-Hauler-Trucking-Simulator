using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using LWS.TruckBowling;
namespace LWS.TruckDanceOff {
// InputAction timestamps share Time.realtimeSinceStartup's timeline (installed
// Input System CallbackContext.time contract). The match maps them to DSP time.
public sealed class DanceInputRouter:IDisposable {
 readonly InputActionMap map;readonly Action<double,int,bool> changed;readonly InputAction[] lanes=new InputAction[4];
 public int Held{get;private set;}public bool Armed{get;private set;}float neutralAge;
 public DanceInputRouter(BowlingPlayer player,Action<double,int,bool> sink){
  changed=sink;map=new InputActionMap("Owned Dance lanes");
  for(int i=0;i<4;i++){
   int bit=1<<i;var a=map.AddAction("Lane "+i,InputActionType.Button);lanes[i]=a;
   if(player.source==BowlingSource.Keyboard){var keys=new[]{player.left,player.back,player.forward,player.right};a.AddBinding(Keyboard.current[keys[i]].path);}
   else if(player.source==BowlingSource.WholeController){a.AddBinding("<Gamepad>/"+new[]{"buttonWest","buttonSouth","buttonNorth","buttonEast"}[i]);a.AddBinding("<Gamepad>/dpad/"+new[]{"left","down","up","right"}[i]);}
   else a.AddBinding("<Gamepad>/"+(player.source==BowlingSource.LeftStick?"leftStick/":"rightStick/")+new[]{"left","down","up","right"}[i]);
   a.performed+=c=>Change(c.time,bit,true);a.canceled+=c=>Change(c.time,bit,false);
  }
  map.devices=new ReadOnlyArray<InputDevice>(new InputDevice[]{player.source==BowlingSource.Keyboard?(InputDevice)Keyboard.current:player.pad});map.Enable();Gate();
 }
 void Change(double at,int bit,bool down){if(down)Held|=bit;else Held&=~bit;if(Armed)changed(at,bit,down);}
 public void Gate(){Armed=false;neutralAge=0;Held=0;for(int i=0;i<4;i++)if(lanes[i].IsPressed())Held|=1<<i;}
 public void Tick(float dt){if(Armed)return;neutralAge=Held==0?neutralAge+dt:0;if(neutralAge>=.1f)Armed=true;}
 public void Dispose(){map.Disable();map.Dispose();Held=0;Armed=false;}
}
}
