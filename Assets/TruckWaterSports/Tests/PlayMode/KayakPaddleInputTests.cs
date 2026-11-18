using System;
using System.Collections;
using UnityEngine.TestTools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace LWS.TruckWaterSports.Tests
{
    // Software devices only: ownership and bindings, not physical-controller acceptance.
    public sealed class KayakPaddleInputTests
    {
        static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
        static object Player(Gamepad pad,string source)
        {
            var player=Activator.CreateInstance(Runtime("LWS.TruckBowling.BowlingPlayer"));
            var type=player.GetType();type.GetField("pad").SetValue(player,pad);type.GetField("released").SetValue(player,true);
            var field=type.GetField("source");field.SetValue(player,Enum.Parse(field.FieldType,source));return player;
        }
        static object Read(object player)=>Runtime("LWS.TruckWaterSports.KayakPaddleControls").GetMethod("Read").Invoke(null,new[]{player});
        static float Stroke(object value,string name)=>(float)value.GetType().GetField(name).GetValue(value);
        [UnityTest] public IEnumerator IndependentWholePadsDoNotSharePaddleOrDisconnectedInput()
        {
            var background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            var first=InputSystem.AddDevice<Gamepad>("Kayak paddle owner1");var second=InputSystem.AddDevice<Gamepad>("Kayak paddle owner2");
            try
            {
                yield return null;yield return null;yield return null;
                Assert.That(first.enabled&&second.enabled,Is.True,"Owned software devices must be enabled before checking mappings.");
                var a=Player(first,"WholeController");var b=Player(second,"WholeController");
                InputSystem.QueueStateEvent(first,new GamepadState().WithButton(GamepadButton.LeftShoulder));yield return null;yield return null;
                Assert.That(Stroke(Read(a),"left"),Is.EqualTo(1));Assert.That(Stroke(Read(a),"right"),Is.Zero);
                Assert.That(Stroke(Read(b),"left"),Is.Zero);Assert.That(Stroke(Read(b),"right"),Is.Zero);
                InputSystem.DisableDevice(first);
                Assert.That(Stroke(Read(a),"left"),Is.Zero,"Disconnected paddles must stop rather than retaining the held stroke.");
            }
            finally{InputSystem.RemoveDevice(first);InputSystem.RemoveDevice(second);InputSystem.settings.backgroundBehavior=background;}
        }
        [UnityTest] public IEnumerator SplitPadStationsUseOnlyTheirAssignedStickAndPreserveHorn()
        {
            var background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            var pad=InputSystem.AddDevice<Gamepad>("Kayak split paddle owners");
            try
            {
                yield return null;yield return null;yield return null;
                Assert.That(pad.enabled,Is.True,"Owned software device must be enabled before checking mappings.");
                var left=Player(pad,"LeftStick");var right=Player(pad,"RightStick");
                InputSystem.QueueStateEvent(pad,new GamepadState{leftStick=new Vector2(-1,0),rightStick=new Vector2(1,0)});yield return null;yield return null;
                Assert.That(Stroke(Read(left),"left"),Is.EqualTo(1));Assert.That(Stroke(Read(left),"right"),Is.Zero);
                Assert.That(Stroke(Read(right),"left"),Is.Zero);Assert.That(Stroke(Read(right),"right"),Is.EqualTo(1));
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.LeftShoulder).WithButton(GamepadButton.RightShoulder));yield return null;yield return null;
                Assert.That(Stroke(Read(left),"left"),Is.Zero);Assert.That(Stroke(Read(right),"right"),Is.Zero,"Split-pad horn shoulders must not stroke either paddle.");
            }
            finally{InputSystem.RemoveDevice(pad);InputSystem.settings.backgroundBehavior=background;}
        }
    }
}
