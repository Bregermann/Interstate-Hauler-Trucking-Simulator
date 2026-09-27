using System.Linq;
using NUnit.Framework;
using LWS.InterstateHauler;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using Object=UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiIntegrated")]
    public sealed class TruckTaxiControlsAndCabLookTests
    {
        [Test]
        public void NewActionsHaveNonConflictingPrimaryBindings()
        {
            var go=new GameObject("Controls input test",typeof(EventSystem),typeof(InputSystemUIInputModule));
            TruckTaxiUIInput input=null;
            try
            {
                input=new TruckTaxiUIInput(go.GetComponent<EventSystem>());
                Assert.AreEqual("<Keyboard>/f1",input.ShowControls.bindings[0].path);
                Assert.AreEqual(1,input.ShowControls.bindings.Count,"A generic Gamepad has no touchpad button.");
                Assert.AreEqual("<Keyboard>/f2",input.ToggleRideRequests.bindings[0].path);
                Assert.AreEqual("<Keyboard>/backquote",input.CenterView.bindings[0].path);
                Assert.AreEqual("<Mouse>/delta",input.CabMouseLook.bindings[0].path);
                Assert.AreEqual("<Gamepad>/rightStick",input.CabGamepadLook.bindings[0].path);
                Assert.AreEqual("<Mouse>/rightButton",input.CabMouseLookHold.bindings[0].path);
            }
            finally { input?.Dispose(); Object.DestroyImmediate(go); }
        }

        [Test]
        public void FullAndQuickViewsShareLiveActionEntries()
        {
            var go=new GameObject("Controls catalog test",typeof(EventSystem),typeof(InputSystemUIInputModule));
            TruckTaxiUIInput input=null;
            try
            {
                input=new TruckTaxiUIInput(go.GetComponent<EventSystem>());
                var rows=TruckTaxiControlsCatalog.Build(input);
                foreach(var name in new[]{"Look around","Center view","Toggle ride requests","Throw filled container","Show controls (hold)"})
                    Assert.IsTrue(rows.Any(r=>r.Name==name && r.Quick),name);
                var toggle=rows.Single(r=>r.Name=="Toggle ride requests");
                Assert.AreSame(input.ToggleRideRequests,toggle.Action);
                Assert.IsFalse(toggle.IsLegacyDirectRead);
                Assert.IsTrue(toggle.Binding(false).Contains("F2"));
                Assert.AreEqual("UNBOUND",toggle.Binding(true));
                input.ToggleRideRequests.ApplyBindingOverride(0,"<Keyboard>/f4");
                Assert.IsTrue(toggle.Binding(false).Contains("F4"),"Action rows must use the effective binding, not catalog fallback text.");
                Assert.IsTrue(rows.Single(r=>r.Name=="Steer").IsLegacyDirectRead);
                Assert.AreSame(input.Navigate,rows.Single(r=>r.Name=="Navigate menus").Action);
                Assert.AreEqual("UNBOUND",rows.Single(r=>r.Name=="Show controls (hold)").Binding(true));
                Assert.AreEqual("UNBOUND",rows.Single(r=>r.Name=="Services / tow").Binding(true));
                Assert.AreEqual("UNBOUND",rows.Single(r=>r.Name=="Throw filled container").Binding(false),
                    "Missing runtime actions must not fall back to a copied key label.");
                Assert.IsFalse(rows.Single(r=>r.Name=="Throw filled container").IsLegacyDirectRead);
                var look=rows.Single(r=>r.Name=="Look around");
                Assert.AreEqual(input.CabMouseLookHold.GetBindingDisplayString(0)+" + "+input.CabMouseLook.GetBindingDisplayString(0),look.Binding(false));
                Assert.AreEqual(input.CabGamepadLook.GetBindingDisplayString(0),look.Binding(true));
                Assert.IsTrue(rows.Single(r=>r.Name=="GPS on / off").IsOnScreenOnly);
                Assert.AreEqual("MENU BUTTON",rows.Single(r=>r.Name=="Use bathroom").Binding(true));
                foreach(var name in new[]{"GPS display settings","On-screen GPS / north up / place markers",
                    "On-screen size / range","Cab map range / route width / color","Restore GPS defaults",
                    "Open driver needs","Open services page","Open store page",
                    "Open items page","Route to store","Throw filled from items page","Call tow truck","Ride requests on / off"})
                    Assert.IsTrue(rows.Single(r=>r.Name==name).IsOnScreenOnly,name);
                Assert.IsTrue(rows.Any(r=>r.Note.Contains("CONFLICT")));
            }
            finally { input?.Dispose(); Object.DestroyImmediate(go); }
        }

        [Test]
        public void CabAnglesAndSettingsStayWithinLimits()
        {
            Assert.AreEqual(new Vector2(120,-45),TruckTaxiCabLook.ClampAngles(new Vector2(180,-90)));
            var settings=new TruckTaxiCabLookSettings();
            settings.SetMouseSensitivity(float.NaN,false);
            settings.SetGamepadSensitivity(1000,false);
            settings.SetSmoothing(-1,false);
            Assert.AreEqual(.12f,settings.MouseSensitivity);
            Assert.AreEqual(240f,settings.GamepadSensitivity);
            Assert.AreEqual(0f,settings.Smoothing);
        }
        [Test]
        public void WheelRowsReadCalibrationRatherThanAssumingDefaultButtons()
        {
            var profile=LwsWheelCalibrationProfile.CreateDefaultLogitechG29();
            var row=new TruckTaxiControlsCatalog.Entry("DRIVING","Steer","A / D","Left stick");
            Assert.AreEqual("UNBOUND",row.WheelBinding(profile));
            profile.steeringBinding=LwsWheelControlBinding.Create(LwsWheelLogicalControl.Steering,LwsWheelControlKind.Axis,"<Joystick>/stick/x","Wheel axis");
            Assert.AreEqual("Wheel axis",row.WheelBinding(profile));
        }
    }
}
