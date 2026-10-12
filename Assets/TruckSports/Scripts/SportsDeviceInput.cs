using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
namespace LWS.TruckSports {
public sealed class SportsDeviceInput {
 struct Buttons{public uint previous,current;public int frame;}
 readonly Dictionary<Gamepad,Buttons> states=new Dictionary<Gamepad,Buttons>();
 static bool Held(ButtonControl b)=>b.ReadValue()>=b.pressPointOrDefault;
 uint Mask(Gamepad p)=>(Held(p.buttonSouth)?1u:0)|(Held(p.buttonNorth)?2u:0)|(Held(p.startButton)?4u:0)|(Held(p.buttonEast)?8u:0);
 Buttons Read(Gamepad pad){if(pad==null||!pad.added)return default;bool found=states.TryGetValue(pad,out var b);if(found&&b.frame==Time.frameCount)return b;uint mask=Mask(pad);b.previous=found?b.current:mask;b.current=mask;b.frame=Time.frameCount;states[pad]=b;return b;}
 public bool Down(Gamepad p,uint bit){var b=Read(p);return (b.current&bit)!=0&&(b.previous&bit)==0;}
 public bool Pressed(Gamepad p,uint bit)=>(Read(p).current&bit)!=0;
 public void Forget(Gamepad pad){states.Remove(pad);}
 public void Sample(){foreach(var p in Gamepad.all)Read(p);}
 public static bool KeyHeld(Key key)=>Keyboard.current!=null&&Keyboard.current[key].isPressed;
 public static bool KeyDown(Key key)=>Keyboard.current!=null&&Keyboard.current[key].wasPressedThisFrame;
 public bool Neutral(Gamepad p)=>p!=null&&p.added&&p.enabled&&p.leftStick.ReadValue().magnitude<.15f&&p.rightTrigger.ReadValue()<.1f&&p.leftTrigger.ReadValue()<.1f&&(Read(p).current&7)==0;
 public static bool NeutralKeys(bool second)=>second?!KeyHeld(Key.UpArrow)&&!KeyHeld(Key.DownArrow)&&!KeyHeld(Key.LeftArrow)&&!KeyHeld(Key.RightArrow)&&!KeyHeld(Key.RightShift)&&!KeyHeld(Key.Enter):!KeyHeld(Key.W)&&!KeyHeld(Key.S)&&!KeyHeld(Key.A)&&!KeyHeld(Key.D)&&!KeyHeld(Key.Space)&&!KeyHeld(Key.R)&&!KeyHeld(Key.Enter);
}
}
