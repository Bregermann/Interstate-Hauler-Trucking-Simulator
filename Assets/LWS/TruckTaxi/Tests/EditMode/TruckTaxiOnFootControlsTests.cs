using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiOnFootControlsTests
    {

        [Test]
        public void PickupCancelUsesUnclaimedKeyAndExistingMapLifetime()
        {
            var root = new GameObject("Pickup cancel input test");
            try
            {
                using (var input = new TruckTaxiUIInput(root.AddComponent<EventSystem>()))
                {
                    Assert.AreEqual("<Keyboard>/f6", input.CancelPickup.bindings[0].path);
                    Assert.AreEqual(1, input.CancelPickup.bindings.Count, "Gamepad requires coordinated raw-driving chord consumption first.");
                    Assert.AreSame(input.Actions, input.CancelPickup.actionMap);
                    Assert.IsNotEmpty(input.Hint(input.CancelPickup));
                    Assert.IsTrue(input.CancelPickup.enabled);
                    input.Actions.Disable(); Assert.IsFalse(input.CancelPickup.enabled);
                    input.Actions.Enable(); Assert.IsTrue(input.CancelPickup.enabled);
                    Assert.AreEqual("<Gamepad>/buttonEast", input.Cancel.bindings[1].path);
                    Assert.AreEqual("<Keyboard>/backspace", input.Cancel.bindings[2].path);
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void JumpBindingsDoNotReuseSouthInteraction()
        {
            var root = new GameObject("On-foot input test");
            try
            {
                var events = root.AddComponent<EventSystem>();
                using (var input = new TruckTaxiUIInput(events))
                {
                    Assert.AreEqual("<Keyboard>/space", input.OnFootJump.bindings[0].path);
                    Assert.AreEqual("<Gamepad>/buttonWest", input.OnFootJump.bindings[1].path);
                    Assert.AreEqual("<Keyboard>/e", input.WorldInteract.bindings[0].path);
                    Assert.AreEqual("<Gamepad>/buttonSouth", input.WorldInteract.bindings[1].path);
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void TrafficImpactRequiresMovingVehicleApproachingPlayer()
        {
            Assert.IsTrue(TruckTaxiWorldLifeController.QualifiesTrafficImpact(Vector3.forward * 8, Vector3.zero, Vector3.forward, 2.5f));
            Assert.IsFalse(TruckTaxiWorldLifeController.QualifiesTrafficImpact(Vector3.forward, Vector3.zero, Vector3.forward, 2.5f));
            Assert.IsFalse(TruckTaxiWorldLifeController.QualifiesTrafficImpact(Vector3.zero, Vector3.back * 8, Vector3.forward, 2.5f));
            Assert.IsFalse(TruckTaxiWorldLifeController.QualifiesTrafficImpact(Vector3.back * 8, Vector3.zero, Vector3.forward, 2.5f));
            Assert.IsFalse(TruckTaxiWorldLifeController.QualifiesTrafficImpact(Vector3.forward * 8, Vector3.forward * 8, Vector3.forward, 2.5f));
            Assert.IsFalse(TruckTaxiWorldLifeController.QualifiesTrafficImpact(new Vector3(float.NaN, 0, 0), Vector3.zero, Vector3.forward, 2.5f));
        }

        [Test]
        public void MissingRagdollProviderDoesNotPretendToRagdoll()
        {
            var root = new GameObject("Missing provider test");
            try
            {
                var ragdoll = root.AddComponent<TruckTaxiPlayerRagdoll>();
                ragdoll.Initialize(null, null);
                Assert.IsFalse(ragdoll.Ready);
                Assert.IsFalse(ragdoll.Begin(Vector3.zero, Quaternion.identity, Vector3.forward * 8, Vector3.zero));
                Assert.IsFalse(ragdoll.IsActive);
                StringAssert.Contains("No compatible", ragdoll.Status);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
