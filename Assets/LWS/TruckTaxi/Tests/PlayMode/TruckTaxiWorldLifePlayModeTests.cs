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
        [TestCase(.15f), TestCase(1f / 3f), TestCase(5f), Category("TaxiOnFootFuelCharacter")]
        public void GroundedJumpRisesOnLongFrameAndMatchesBoundedShortSteps(float elapsed)
        {
            using (var fixture = new FootMovementFixture())
            {
                float startY = fixture.Foot.position.y;
                Assert.IsTrue(fixture.World.TryJump());
                Assert.IsFalse(fixture.World.TryJump(), "No repeated air jumps.");
                fixture.Step(elapsed);
                float longRise = fixture.Foot.position.y - startY;
                Assert.Greater(longRise, .1f, "A real CharacterController must rise, including at the maximum frame delta.");
                float longVelocity = fixture.VerticalSpeed;

                fixture.ResetGrounded(); startY = fixture.Foot.position.y;
                Assert.IsTrue(fixture.World.TryJump());
                float boundedDuration = Mathf.Min(elapsed, 20f / 60f);
                for (int i = 0; i < 20; i++) fixture.Step(boundedDuration / 20);
                Assert.Greater(fixture.Foot.position.y, startY + .1f);
                Assert.AreEqual(fixture.Foot.position.y - startY, longRise, .025f,
                    "A long frame must follow the short-frame arc, not lose its launch to gravity.");
                Assert.AreEqual(fixture.VerticalSpeed, longVelocity, .01f);
            }
        }

        [Test, Category("TaxiOnFootFuelCharacter")]
        public void JumpSubstepsRespectCeilingAndRecoverGroundedControl()
        {
            using (var fixture = new FootMovementFixture())
            {
                var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceiling.transform.SetParent(fixture.Root.transform);
                ceiling.transform.position = fixture.Foot.position + Vector3.up * 2.3f;
                ceiling.transform.localScale = new Vector3(6, .5f, 6);
                Physics.SyncTransforms();
                float startY = fixture.Foot.position.y;
                Assert.IsTrue(fixture.World.TryJump()); fixture.Step(.15f);
                Assert.Less(fixture.Foot.position.y, startY + .35f, "The capsule must not pass through the low ceiling.");
                Assert.LessOrEqual(fixture.VerticalSpeed, 0, "A ceiling hit must cancel upward velocity.");
                for (int i = 0; i < 40 && !fixture.World.IsGrounded; i++) fixture.Step(1f / 60f);
                Assert.IsTrue(fixture.World.IsGrounded);
                Assert.IsTrue(fixture.World.TryJump(), "Landing must re-arm grounded jump.");
            }
        }

        private sealed class FootMovementFixture : System.IDisposable
        {
            private const System.Reflection.BindingFlags Private = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            private readonly CursorLockMode previousLock = Cursor.lockState;
            private readonly bool previousCursor = Cursor.visible;
            public readonly GameObject Root = new GameObject("Isolated foot movement test");
            public readonly TruckTaxiWorldLifeController World;
            public readonly Transform Foot;
            private readonly CharacterController controller;
            private readonly System.Reflection.MethodInfo step;
            public float VerticalSpeed => (float)typeof(TruckTaxiWorldLifeController).GetField("verticalSpeed", Private).GetValue(World);
            public FootMovementFixture()
            {
                World = Root.AddComponent<TruckTaxiWorldLifeController>();
                var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.transform.SetParent(Root.transform);
                ground.transform.position = new Vector3(1000, 999.5f, 1000);
                ground.transform.localScale = new Vector3(6, 1, 6);
                Foot = new GameObject("Actual CharacterController").transform;
                Foot.SetParent(Root.transform);
                controller = Foot.gameObject.AddComponent<CharacterController>();
                controller.height = 1.8f; controller.radius = .32f; controller.center = new Vector3(0, .9f, 0);
                controller.stepOffset = .32f; controller.slopeLimit = 48f;
                Set("footBody", Foot.gameObject); Set("controller", controller);
                Set("<Mode>k__BackingField", TruckTaxiPlayerControlMode.OnFoot);
                step = typeof(TruckTaxiWorldLifeController).GetMethod("StepFootMovement", Private);
                Assert.IsNotNull(step);
                ResetGrounded();
            }
            private void Set(string name, object value) => typeof(TruckTaxiWorldLifeController).GetField(name, Private).SetValue(World, value);
            public void ResetGrounded()
            {
                controller.enabled = false; Foot.position = new Vector3(1000, 1000.06f, 1000);
                controller.enabled = true; Set("verticalSpeed", 0f);
                Physics.SyncTransforms(); controller.Move(Vector3.down * .2f);
                Assert.IsTrue(World.IsGrounded);
            }
            public void Step(float elapsed) => step.Invoke(World, new object[] { Vector3.zero, elapsed });
            public void Dispose()
            {
                Set("<Mode>k__BackingField", TruckTaxiPlayerControlMode.Driving);
                Object.DestroyImmediate(Root);
                Cursor.lockState = previousLock; Cursor.visible = previousCursor;
            }
        }

        [UnityTest, Category("TaxiOnFootFuelCharacter")]
        public IEnumerator ActualFillerHighlightRestoresMaterialsOnDisableAndDestruction()
        {
            var fillerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = Color.red };
            try
            {
                var renderer = fillerObject.GetComponent<Renderer>();
                renderer.sharedMaterial = material;
                var filler = fillerObject.AddComponent<TruckTaxiFuelFiller>();
                yield return null;
                for (int cycle = 0; cycle < 2; cycle++)
                {
                    Assert.IsTrue(filler.isActiveAndEnabled);
                    CollectionAssert.Contains(TruckTaxiWorldInteractable.Active, filler);
                    filler.SetHighlighted(true);
                    var highlight = renderer.sharedMaterial;
                    Assert.IsTrue(filler.Highlighted);
                    Assert.AreNotSame(material, highlight);
                    filler.SetHighlighted(true);
                    Assert.AreSame(highlight, renderer.sharedMaterial, "A held nozzle must reuse its highlight material.");

                    if (cycle == 0) filler.enabled = false;
                    else fillerObject.SetActive(false);

                    Assert.IsFalse(filler.Highlighted, "Unity OnDisable must clear the highlight immediately.");
                    Assert.AreSame(material, renderer.sharedMaterial);
                    CollectionAssert.DoesNotContain(TruckTaxiWorldInteractable.Active, filler);
                    Assert.AreEqual(Color.red, material.color);
                    yield return null;
                    Assert.IsTrue(highlight == null, "Runtime disable must dispose the owned clone after the frame.");

                    if (cycle == 0) filler.enabled = true;
                    else fillerObject.SetActive(true);
                    Assert.IsFalse(filler.Highlighted);
                    Assert.AreSame(material, renderer.sharedMaterial);
                }

                filler.SetHighlighted(true);
                var finalHighlight = renderer.sharedMaterial;
                Object.Destroy(filler);
                yield return null;
                yield return null;
                Assert.IsTrue(filler == null);
                Assert.AreSame(material, renderer.sharedMaterial, "Destroying the component must restore the surviving renderer.");
                Assert.IsTrue(finalHighlight == null);
                Assert.AreEqual(Color.red, material.color);
            }
            finally
            {
                if (fillerObject != null)
                {
                    var filler=fillerObject.GetComponent<TruckTaxiFuelFiller>();
                    if(filler!=null) filler.SetHighlighted(false);
                    Object.Destroy(fillerObject);
                }
                Object.Destroy(material);
            }
        }

        [UnityTest, Category("TaxiOnFootFuelCharacter")]
        public IEnumerator OnFootJumpPauseUtilitiesFuelLeaseAndTrafficRecovery()
        {
            yield return SceneManager.LoadSceneAsync("TruckTaxi_DemoCity");
            float deadline = Time.realtimeSinceStartup + 60;
            while ((TruckTaxiBootstrap.Instance?.StartupComplete != true || TruckTaxiBootstrap.Instance.Persistence?.IsBusy == true) &&
                Time.realtimeSinceStartup < deadline) yield return null;
            var host = TruckTaxiBootstrap.Instance;
            Assert.IsNotNull(host); Assert.IsTrue(host.Ready);
            yield return TruckTaxiPlayModeSaveIsolation.CompleteNewRunIntroduction(host);
            var truck = host.Player.GetComponent<Rigidbody>();
            bool wasKinematic = truck.isKinematic;
            var stationObject = new GameObject("Focused test pump");
            var trafficObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                truck.linearVelocity = truck.angularVelocity = Vector3.zero; truck.isKinematic = true;
                var world = host.WorldLife;
                Assert.IsTrue(world.TryExitTruck(), world.Feedback);
                var driver = host.Player.GetComponent<TruckTaxiDriverPresentation>();
                Assert.IsNotNull(driver.OnFootVisual);
                var savedAppearance = host.Session.CaptureDriverAppearance();
                var previewAppearance = savedAppearance.Copy(); previewAppearance.chestShape = .8f; previewAppearance.capVisible = false;
                try
                {
                    Assert.IsTrue(driver.TryApplyAppearance(previewAppearance, out var appearanceReason), appearanceReason);
                    var seated = driver.Occupant.Find(TruckTaxiWobbleVisual.VisualName);
                    Assert.AreEqual(seated.Find("Left Clothed chest").localScale, driver.OnFootVisual.Find("Left Clothed chest").localScale);
                    Assert.IsFalse(seated.Find("Cap").gameObject.activeSelf);
                    Assert.IsFalse(driver.OnFootVisual.Find("Cap").gameObject.activeSelf);
                }
                finally { driver.TryApplyAppearance(savedAppearance, out _); }
                var controller = world.FootTransform.GetComponent<CharacterController>();
                controller.Move(Vector3.down * .2f);
                Assert.IsTrue(world.IsGrounded);
                float startY = world.FootTransform.position.y;
                float launchDelta = Time.deltaTime;
                string headroom = string.Empty;
                foreach (var hit in Physics.SphereCastAll(world.FootTransform.position + Vector3.up * (controller.height - controller.radius),
                    controller.radius * .95f, Vector3.up, 1f, ~0, QueryTriggerInteraction.Ignore))
                    if (!hit.collider.transform.IsChildOf(world.FootTransform)) headroom += hit.collider.name + "@" + hit.distance.ToString("F3") + " ";
                Assert.IsTrue(world.TryJump()); Assert.IsFalse(world.TryJump(), "No repeated air jumps.");
                yield return new WaitForSeconds(.15f);
                Assert.Greater(world.FootTransform.position.y, startY + .1f,
                    $"Jump must rise. Launch delta={launchDelta:F3}, sample delta={Time.deltaTime:F3}, flags={controller.collisionFlags}, headroom=[{headroom}]");
                yield return new WaitForSeconds(.9f);

                host.SetPaused(true);
                world.OpenInventory(); Assert.IsTrue(world.InventoryOpen); Assert.IsNotNull(world.FocusRoot);
                Assert.IsFalse(world.TryJump()); world.CloseInventory(); Assert.IsTrue(host.Paused);
                world.OpenDamage(); Assert.IsTrue(world.DamageOpen); world.CloseDamage(); Assert.IsTrue(host.Paused);
                host.SetPaused(false); yield return null; yield return null;

                var station = stationObject.AddComponent<TruckTaxiGasStationPoint>();
                station.stableId = "test.onfoot.gas"; stationObject.transform.position = truck.position;
                world.ToggleNozzle(station);
                Assert.AreSame(station, world.ActiveNozzleStation);
                var filler = host.Player.GetComponentInChildren<TruckTaxiFuelFiller>();
                Assert.IsNotNull(filler); Assert.IsTrue(filler.Highlighted);
                stationObject.transform.position += Vector3.right * 100;
                yield return null; yield return null;
                Assert.IsNull(world.ActiveNozzleStation); Assert.IsFalse(filler.Highlighted);
                Assert.IsFalse(host.Fuel.IsManualFueling);

                Assert.IsTrue(world.RagdollAvailable, world.RagdollStatus);
                world.ragdollRecoverySeconds = 1;
                trafficObject.transform.position = world.FootTransform.position + Vector3.right * 2 + Vector3.up;
                trafficObject.AddComponent<TruckTaxiImpactTarget>().kind = TaxiImpactKind.Traffic;
                var traffic = trafficObject.AddComponent<Rigidbody>(); traffic.useGravity = false;
                traffic.linearVelocity = Vector3.left * 8;
                Assert.IsTrue(world.TryTrafficImpact(trafficObject.GetComponent<Collider>()));
                Assert.IsTrue(world.IsRagdoll); Assert.IsFalse(controller.enabled);
                Assert.LessOrEqual(world.GetComponent<TruckTaxiPlayerRagdoll>().LastImpulse.magnitude, 180.001f);
                Assert.IsFalse(world.TryJump()); Assert.IsFalse(world.TryEnterTruck());
                Object.Destroy(trafficObject);
                yield return new WaitForSeconds(1.3f);
                Assert.IsFalse(world.IsRagdoll); Assert.IsTrue(world.IsOnFoot); Assert.IsTrue(controller.enabled);
                Assert.Less(Vector3.Distance(world.FootCamera.transform.localPosition, new Vector3(0, 1.62f, 0)), .001f);
                Assert.IsTrue(host.Player.GetComponentInChildren<LWS.InterstateHauler.LwsKeyboardGamepadTruckInputSource>(true).DrivingInputSuppressed);
                world.ReturnToTruck();
                Assert.AreEqual(TruckTaxiPlayerControlMode.Driving, world.Mode);
                Assert.IsFalse(host.Player.GetComponentInChildren<LWS.InterstateHauler.LwsKeyboardGamepadTruckInputSource>(true).DrivingInputSuppressed);
            }
            finally
            {
                host.WorldLife.ReturnToTruck(); host.SetPaused(false); truck.isKinematic = wasKinematic;
                Object.Destroy(stationObject); if (trafficObject != null) Object.Destroy(trafficObject);
            }
        }

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
