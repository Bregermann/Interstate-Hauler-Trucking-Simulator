using System.Collections.Generic;
using LWS.InterstateHauler;
using UnityEngine.InputSystem;

namespace LWS.TruckTaxi
{
    // UI actions are queried live. Legacy vehicle commands are direct device reads in
    // LwsKeyboardGamepadTruckInputSource; these rows mirror that source until it exposes actions.
    public static class TruckTaxiControlsCatalog
    {
        public sealed class Entry
        {
            public readonly string Category,Name,Keyboard,Gamepad,Note,ScreenAccess;
            public readonly bool Quick,ExpectsAction;
            public readonly InputAction Action;
            public readonly InputAction GamepadAction,Modifier;
            public bool IsLegacyDirectRead => !ExpectsAction && ScreenAccess==null;
            public bool IsOnScreenOnly => ScreenAccess!=null;
            public Entry(string category,string name,string keyboard,string gamepad,bool quick=false,string note="",InputAction action=null,
                InputAction gamepadAction=null,InputAction modifier=null,string screenAccess=null,bool expectsAction=false)
            { Category=category; Name=name; Keyboard=keyboard; Gamepad=gamepad; Quick=quick; Note=note; Action=action; GamepadAction=gamepadAction; Modifier=modifier; ScreenAccess=screenAccess; ExpectsAction=expectsAction || action!=null; }
            public string Binding(bool gamepad)
            {
                if(ScreenAccess!=null) return ScreenAccess=="HUD" && gamepad?"UNBOUND (HUD POINTER)":ScreenAccess+" BUTTON";
                var action=gamepad && GamepadAction!=null?GamepadAction:Action;
                if(action!=null)
                {
                    string device=gamepad?"Gamepad":"Keyboard";
                    if(Name=="Look around" && !gamepad) device="Mouse";
                    var matches=new List<string>();
                    for(int i=0;i<action.bindings.Count;i++)
                    {
                        var binding=action.bindings[i];
                        if(binding.isComposite)
                        {
                            for(int part=i+1;part<action.bindings.Count && action.bindings[part].isPartOfComposite;part++)
                                if(action.bindings[part].effectivePath?.Contains(device)==true)
                                { matches.Add(action.GetBindingDisplayString(i)); break; }
                        }
                        else if(!binding.isPartOfComposite && !string.IsNullOrEmpty(binding.effectivePath) && binding.effectivePath.Contains(device))
                            matches.Add(action.GetBindingDisplayString(i));
                    }
                    if(matches.Count>0)
                        return Modifier!=null && !gamepad ? Modifier.GetBindingDisplayString(0)+" + "+string.Join(" / ",matches) : string.Join(" / ",matches);
                    return "UNBOUND";
                }
                if(ExpectsAction) return "UNBOUND";
                string value=gamepad?Gamepad:Keyboard;
                return string.IsNullOrEmpty(value)?"UNBOUND":value;
            }
            public string WheelBinding(LwsWheelCalibrationProfile profile)
            {
                if(ScreenAccess!=null) return ScreenAccess=="HUD"?"UNBOUND (HUD POINTER)":"MENU BUTTON";
                if(profile==null) return "UNBOUND";
                if(Name=="Shifter gates 1-6 / reverse")
                    return JoinWheel(profile,LwsWheelLogicalControl.ShifterGate1,LwsWheelLogicalControl.ShifterGate2,
                        LwsWheelLogicalControl.ShifterGate3,LwsWheelLogicalControl.ShifterGate4,
                        LwsWheelLogicalControl.ShifterGate5,LwsWheelLogicalControl.ShifterGate6,LwsWheelLogicalControl.ShifterReverse);
                if(Name=="Range / splitter") return JoinWheel(profile,LwsWheelLogicalControl.RangeToggle,LwsWheelLogicalControl.SplitterToggle);
                if(Name=="Low / high beams") return JoinWheel(profile,LwsWheelLogicalControl.Headlights,LwsWheelLogicalControl.HighBeams);
                if(Name=="Left / right indicator") return JoinWheel(profile,LwsWheelLogicalControl.LeftSignal,LwsWheelLogicalControl.RightSignal);
                if(Name=="Horn / air horn") return JoinWheel(profile,LwsWheelLogicalControl.Horn,LwsWheelLogicalControl.AirHorn);
                if(Name=="Engine brake +/-") return "UNBOUND";
                if(Name=="Retarder +/-") return JoinWheel(profile,LwsWheelLogicalControl.RetarderIncrease,LwsWheelLogicalControl.RetarderDecrease);
                LwsWheelLogicalControl control=WheelControl(Name);
                if(control==LwsWheelLogicalControl.None) return "UNBOUND";
                var binding=profile.GetBinding(control);
                return binding.IsBound ? (!string.IsNullOrWhiteSpace(binding.displayName)?binding.displayName:binding.controlPath) : "UNBOUND";
            }
            private static string JoinWheel(LwsWheelCalibrationProfile profile,params LwsWheelLogicalControl[] controls)
            {
                var values=new string[controls.Length];
                for(int i=0;i<controls.Length;i++)
                {
                    var binding=profile.GetBinding(controls[i]);
                    values[i]=binding.IsBound ? (!string.IsNullOrWhiteSpace(binding.displayName)?binding.displayName:binding.controlPath) : "UNBOUND";
                }
                return string.Join(" / ",values);
            }
        }
        public static List<Entry> Build(TruckTaxiUIInput ui,TruckTaxiDriverNeedsCoordinator needs=null,TruckTaxiPassengerRuntime passengers=null)
        {
            if(ui==null) throw new System.ArgumentNullException(nameof(ui));
            var rows=new List<Entry>();
            void Add(string category,string name,string keyboard,string pad,bool quick=false,string note="",InputAction action=null,
                InputAction gamepadAction=null,InputAction modifier=null)
                => rows.Add(new Entry(category,name,keyboard,pad,quick,note,action,gamepadAction,modifier));
            void Bound(string category,string name,bool quick,InputAction action,string note="")
                => rows.Add(new Entry(category,name,"","",quick,note,action,expectsAction:true));
            void Screen(string category,string name,string access)
                => rows.Add(new Entry(category,name,"","",screenAccess:access));
            Add("DRIVING","Steer","A / D or Left / Right","Left stick",true);
            Add("DRIVING","Accelerate","W or Up","Right trigger",true);
            Add("DRIVING","Brake / reverse","S or Down","Left trigger",true);
            Add("DRIVING","Clutch","Left / Right Ctrl","",false);
            Add("DRIVING","Parking brake","P","B / East",true,"CONFLICT: gamepad B also cancels menus");
            Add("DRIVING","Ignition","I","",false);
            Add("DRIVING","Start engine","E","",false);
            Add("DRIVING","Stop engine","Shift + E","",false);
            Add("DRIVING","Shift up / down","Period / Comma","",false);
            Add("DRIVING","Shifter gates 1-6 / reverse","","",false);
            Add("DRIVING","Range / splitter","","",false);
            Add("DRIVING","Trailer brake","Space","",false);
            Add("DRIVING","Attach / detach trailer","T","D-pad Down",false);
            Add("DRIVING","Reset upright","U","Select + Y / North",false);
            Add("DRIVING","Cruise on / off","C","D-pad Right",false);
            Add("DRIVING","Cruise set / resume","R / Shift + R","",false);
            Add("DRIVING","Cruise cancel","Backspace","D-pad Left",false,"CONFLICT: Backspace also cancels menus");
            Add("DRIVING","Cruise speed +/-","Equals / Minus","",false);
            Add("DRIVING","Engine brake","M","",false);
            Add("DRIVING","Engine brake +/-","Page Up / Page Down","",false);
            Add("DRIVING","Retarder +/-","Home / End","",false,"CONFLICT: Home throws a container; End opens services");
            Add("DRIVING","Differential lock","O","",false);
            Add("VEHICLE","Low / high beams","L / K","Y / North / X / West",false);
            Add("VEHICLE","Left / right indicator","Z / X","Left / Right shoulder",false,"CONFLICT: shoulders also serve jug controls");
            Add("VEHICLE","Hazard lights","J","Left stick press",false);
            Add("VEHICLE","Wipers","V","D-pad Up",false);
            Add("VEHICLE","Horn / air horn","H / B","A / South / Right stick press",false,"CONFLICT: gamepad A also submits menus");
            Add("VEHICLE","Driver gesture","F","Both stick presses",false,"CONFLICT: F also ejects a passenger");
            Add("CAMERA","Look around","Hold Right mouse + move","Right stick",true,"",ui?.CabMouseLook,ui?.CabGamepadLook,ui?.CabMouseLookHold);
            Add("CAMERA","Center view","Backquote","",true,"",ui?.CenterView);
            Add("CAMERA","Camera change","Tab","Select / View",true,"CONFLICT: Select also ejects a passenger");
            Add("PASSENGERS","Accept ride","Enter","A / South",true,"",ui?.Submit);
            Add("PASSENGERS","Decline ride","Escape / Backspace","B / East",false,"CONFLICT: Escape also pauses",ui?.Cancel);
            Add("PASSENGERS","Toggle ride requests","F2","",true,"",ui?.ToggleRideRequests);
            Bound("PASSENGERS","Hold to eject",false,passengers?.EjectAction,"CONFLICT: F is also driver gesture");
            Bound("DRIVER NEEDS","Hold jug",false,needs?.JugHoldAction,"CONFLICT: left shoulder is indicator");
            Bound("DRIVER NEEDS","Jug cue",false,needs?.JugCueAction,"CONFLICT: right shoulder is indicator");
            Bound("DRIVER NEEDS","Throw filled container",true,needs?.ThrowContainerAction,"CONFLICT: Home also increases retarder");
            Screen("GPS / NAVIGATION","GPS on / off","MENU");
            Screen("GPS / NAVIGATION","GPS display settings","MENU");
            Screen("GPS / NAVIGATION","On-screen GPS / north up / place markers","MENU");
            Screen("GPS / NAVIGATION","On-screen size / range","MENU");
            Screen("GPS / NAVIGATION","Cab map range / route width / color","MENU");
            Screen("GPS / NAVIGATION","Restore GPS defaults","MENU");
            Add("GPS / NAVIGATION","Services / tow","End","",true,"CONFLICT: End also decreases retarder",ui?.Services);
            Screen("DRIVER NEEDS / SERVICES","Open driver needs","MENU");
            Screen("DRIVER NEEDS / SERVICES","Open services page","MENU");
            Screen("DRIVER NEEDS / SERVICES","Open store page","MENU");
            Screen("DRIVER NEEDS / SERVICES","Open items page","MENU");
            Screen("DRIVER NEEDS / SERVICES","Return to driver page","MENU");
            Screen("DRIVER NEEDS / SERVICES","Route to restroom","MENU");
            Screen("DRIVER NEEDS / SERVICES","Use bathroom","MENU");
            Screen("DRIVER NEEDS / SERVICES","Use piss jug","MENU");
            Screen("DRIVER NEEDS / SERVICES","Dispose filled jug","MENU");
            Screen("DRIVER NEEDS / SERVICES","Clean cab","MENU");
            Screen("DRIVER NEEDS / SERVICES","Buy item","MENU");
            Screen("DRIVER NEEDS / SERVICES","Route to store","MENU");
            Screen("DRIVER NEEDS / SERVICES","Use item","MENU");
            Screen("DRIVER NEEDS / SERVICES","Mount cab decoration","MENU");
            Screen("DRIVER NEEDS / SERVICES","Throw filled from items page","MENU");
            Screen("DRIVER NEEDS / SERVICES","Find store","MENU");
            Screen("DRIVER NEEDS / SERVICES","Find food / drink","MENU");
            Screen("DRIVER NEEDS / SERVICES","Find gas","MENU");
            Screen("DRIVER NEEDS / SERVICES","Find repair","MENU");
            Screen("DRIVER NEEDS / SERVICES","Call tow truck","MENU");
            Screen("DRIVER NEEDS / SERVICES","Confirm tow","MENU");
            Screen("DRIVER NEEDS / SERVICES","Resume ride route","MENU");
            Screen("DRIVER NEEDS / SERVICES","Ride requests on / off","MENU");
            Add("UI","Show controls (hold)","F1","",true,"",ui?.ShowControls);
            Add("UI","Pause / resume","Escape","Start / Menu",true,"",ui?.Pause);
            Add("UI","Confirm / interact","Enter","A / South",false,"",ui?.Submit);
            Add("UI","Back","Escape / Backspace","B / East",false,"",ui?.Cancel);
            Add("UI","Navigate menus","Arrows or W A S D","D-pad / Left stick",false,"",ui?.Navigate);
            return rows;
        }
        private static LwsWheelLogicalControl WheelControl(string name)
        {
            switch(name)
            {
                case "Steer": return LwsWheelLogicalControl.Steering;
                case "Accelerate": return LwsWheelLogicalControl.Throttle;
                case "Brake / reverse": return LwsWheelLogicalControl.Brake;
                case "Clutch": return LwsWheelLogicalControl.Clutch;
                case "Parking brake": return LwsWheelLogicalControl.ParkingBrake;
                case "Ignition": return LwsWheelLogicalControl.Ignition;
                case "Start engine": return LwsWheelLogicalControl.EngineStart;
                case "Stop engine": return LwsWheelLogicalControl.EngineStop;
                case "Shifter gates 1-6 / reverse": return LwsWheelLogicalControl.ShifterGate1;
                case "Range / splitter": return LwsWheelLogicalControl.RangeToggle;
                case "Trailer brake": return LwsWheelLogicalControl.TrailerBrake;
                case "Attach / detach trailer": return LwsWheelLogicalControl.TrailerAttachDetach;
                case "Reset upright": return LwsWheelLogicalControl.ResetTruckUpright;
                case "Engine brake": return LwsWheelLogicalControl.EngineBrake;
                case "Differential lock": return LwsWheelLogicalControl.DifferentialLock;
                case "Hazard lights": return LwsWheelLogicalControl.Hazards;
                case "Wipers": return LwsWheelLogicalControl.Wipers;
                case "Camera change": return LwsWheelLogicalControl.CameraCycle;
                case "Center view": return LwsWheelLogicalControl.LookReset;
                case "Driver gesture": return LwsWheelLogicalControl.FlipOffDriver;
                case "Pause / resume": return LwsWheelLogicalControl.Pause;
                case "Confirm / interact": return LwsWheelLogicalControl.Interact;
                case "Back": return LwsWheelLogicalControl.MenuCancel;
                default: return LwsWheelLogicalControl.None;
            }
        }
    }
}
