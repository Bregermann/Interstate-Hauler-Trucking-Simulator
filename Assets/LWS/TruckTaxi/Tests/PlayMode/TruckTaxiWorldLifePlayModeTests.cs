using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiWorldLifePlayModeTests
    {
        [UnityTest]
        public IEnumerator GeneratorDestructionRetiresOnlyItsTrackedChildrenAndFiller()
        {
            var owner = new GameObject("Facility lifecycle test");
            var service = new GameObject("Authored service");
            var filler = new GameObject("Owned filler");
            var generated = new GameObject("Owned facility");
            generated.transform.SetParent(service.transform);
            var authored = GameObject.CreatePrimitive(PrimitiveType.Cube);
            authored.transform.SetParent(service.transform);
            try
            {
                var generator = owner.AddComponent<TruckTaxiPhysicalFacilities>();
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var ledger = (System.Collections.Generic.Dictionary<string, GameObject>)typeof(TruckTaxiPhysicalFacilities)
                    .GetField("roots", flags).GetValue(generator);
                ledger.Add("service", generated);
                typeof(TruckTaxiPhysicalFacilities).GetField("ownedFiller", flags).SetValue(generator, filler);
                yield return null;
                Object.Destroy(generator);
                yield return null; yield return null;
                Assert.IsTrue(generated == null, "Generator teardown must retire its externally parented geometry.");
                Assert.IsTrue(filler == null);
                Assert.IsTrue(authored != null && authored.GetComponent<Collider>().enabled);
                Assert.IsNotNull(service);
            }
            finally
            {
                Object.Destroy(owner); Object.Destroy(service);
                if (filler != null) Object.Destroy(filler);
            }
        }

        [UnityTest]
        public IEnumerator BreakawayPropYieldsButStructuralPropKeepsItsCollider()
        {
            var small = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var structure = GameObject.CreatePrimitive(PrimitiveType.Cube);
            structure.SetActive(false);
            try
            {
                var structural = structure.AddComponent<TruckTaxiBreakawayProp>();
                structural.category = TruckTaxiPropPhysicsCategory.Structural;
                structure.SetActive(true);
                var breakaway = small.AddComponent<TruckTaxiBreakawayProp>();
                yield return null;
                Assert.IsTrue(small.GetComponent<Collider>().isTrigger);
                Assert.IsTrue(small.GetComponent<Rigidbody>().isKinematic);
                breakaway.ResetProp();
                Assert.IsTrue(small.GetComponent<Collider>().isTrigger);
                Assert.IsFalse(structure.GetComponent<Collider>().isTrigger);
                Assert.IsNull(structure.GetComponent<Rigidbody>());
            }
            finally { Object.Destroy(small); Object.Destroy(structure); }
        }

        [UnityTest]
        public IEnumerator DemoCityBootstrapsOneWorldLifeAndPhysicalFacilities()
        {
            yield return SceneManager.LoadSceneAsync("TruckTaxi_DemoCity");
            float deadline = Time.realtimeSinceStartup + 60f;
            while ((TruckTaxiBootstrap.Instance == null || !TruckTaxiBootstrap.Instance.StartupComplete ||
                    TruckTaxiBootstrap.Instance.Persistence?.IsBusy == true) &&
                   Time.realtimeSinceStartup < deadline) yield return null;
            var host = TruckTaxiBootstrap.Instance;
            Assert.IsNotNull(host);
            Assert.IsTrue(host.Ready);
            Assert.IsTrue(host.CanResume, host.ResumeBlockReason);
            yield return TruckTaxiPlayModeSaveIsolation.CompleteNewRunIntroduction(host);
            Assert.IsNotNull(host.WorldLife);
            Assert.IsNotNull(host.ComponentDamage);
            Assert.IsNotNull(host.PhysicalFacilities);
            Assert.IsNotNull(host.FacilityAmbience);
            Assert.Greater(host.PhysicalFacilities.BuiltCount, 0);
            Assert.Greater(host.PhysicalFacilities.TotalToilets, 0);
            Assert.AreEqual(TruckTaxiPlayerControlMode.Driving, host.WorldLife.Mode);
            Assert.IsFalse(host.WorldLife.FootCamera.enabled);
            Assert.IsTrue(host.ComponentDamage.Parts.Count > 17);
            string tire = null;
            foreach (var part in host.ComponentDamage.Parts.Values)
                if (part.Category == TruckTaxiDamageCategory.Tire) { tire = part.Id; break; }
            Assert.IsNotNull(tire);
            Assert.IsTrue(host.ComponentDamage.Damage(tire, .9f));
            Assert.Greater(host.ComponentDamage.RepairCostFor(TruckTaxiServiceCapability.RepairTires), 0);
            host.ComponentDamage.RepairForCapabilities(TruckTaxiServiceCapability.RepairGeneralDamage);
            Assert.Less(host.ComponentDamage.Parts[tire].Condition, .2f);
            host.ComponentDamage.RepairForCapabilities(TruckTaxiServiceCapability.RepairTires);
            Assert.AreEqual(1f, host.ComponentDamage.Parts[tire].Condition);
            var body = host.Player.GetComponent<Rigidbody>();
            var driving = host.Player.GetComponentInChildren<LWS.InterstateHauler.LwsKeyboardGamepadTruckInputSource>(true);
            Assert.IsNotNull(driving);
            body.linearVelocity = Vector3.forward;
            Assert.IsFalse(host.WorldLife.TryExitTruck());
            body.linearVelocity = Vector3.zero;
            Assert.IsTrue(host.WorldLife.TryExitTruck(), host.WorldLife.Feedback);
            Assert.IsTrue(host.WorldLife.IsOnFoot);
            Assert.IsTrue(driving.DrivingInputSuppressed);
            Assert.IsTrue(host.WorldLife.TryEnterTruck());
            Assert.AreEqual(TruckTaxiPlayerControlMode.Driving, host.WorldLife.Mode);
            Assert.IsFalse(driving.DrivingInputSuppressed);
            var previousInput = InputSystem.settings;
            var testInput = Object.Instantiate(previousInput);
            InputSystem.settings = testInput;
            testInput.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            testInput.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            var keyboard = InputSystem.AddDevice<Keyboard>("World-life G-key regression");
            bool wasKinematic = body.isKinematic;
            try
            {
                // This is input/UI coverage, not a suspension test; hold the newly spawned truck still.
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.G));
                yield return null; yield return null;
                Assert.IsTrue(host.WorldLife.IsOnFoot, host.WorldLife.Feedback);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.G));
                yield return null; yield return null;
                Assert.AreEqual(TruckTaxiPlayerControlMode.Driving, host.WorldLife.Mode);
                Assert.IsFalse(driving.DrivingInputSuppressed);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings = previousInput;
                Object.Destroy(testInput);
                host.WorldLife.ReturnToTruck();
                body.isKinematic = wasKinematic;
            }
        }
    }
}
