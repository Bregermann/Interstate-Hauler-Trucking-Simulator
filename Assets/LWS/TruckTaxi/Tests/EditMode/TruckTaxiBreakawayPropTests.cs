using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiBreakawayPropTests
    {
        private readonly List<GameObject> owned = new List<GameObject>();
        private int baselineBodies, baselineDebris;
        [SetUp] public void Setup()
        { baselineBodies = TruckTaxiPropPhysicsBudget.Shared.ActiveBodies; baselineDebris = TruckTaxiPropPhysicsBudget.Shared.ActiveDebris; }
        private GameObject Box(string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.position = new Vector3(10000 + owned.Count * 5, 10000, 10000);
            owned.Add(go); return go;
        }
        private TruckTaxiBreakawayProp Prop(TruckTaxiPropPhysicsCategory category = TruckTaxiPropPhysicsCategory.Destructible)
        {
            var go = Box("Breakaway fixture"); go.SetActive(false);
            var target = go.AddComponent<TruckTaxiImpactTarget>(); target.kind = TaxiImpactKind.Property; target.targetId = "test.prop." + owned.Count;
            var prop = go.AddComponent<TruckTaxiBreakawayProp>(); prop.category = category;
            prop.durability = 100; go.SetActive(true);
            prop.InitializePhysics(); return prop;
        }
        [TearDown] public void Cleanup()
        {
            foreach (var go in owned)
            {
                if (go == null) continue;
                var prop = go.GetComponent<TruckTaxiBreakawayProp>();
                // Match production lifecycle explicitly for runtime-only MonoBehaviours in EditMode.
                if (prop != null) typeof(TruckTaxiBreakawayProp).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(prop, null);
                TruckTaxiPropPhysicsBudget.Shared.Release(go.GetComponent<Rigidbody>());
                Object.DestroyImmediate(go);
            }
            owned.Clear();
            Assert.AreEqual(baselineBodies, TruckTaxiPropPhysicsBudget.Shared.ActiveBodies);
            Assert.AreEqual(baselineDebris, TruckTaxiPropPhysicsBudget.Shared.ActiveDebris);
        }

        [Test]
        public void SerializedStructuralValueAndLegacyCategoriesRemainStable()
        {
            Assert.AreEqual(0, (int)TruckTaxiPropPhysicsCategory.Lightweight);
            Assert.AreEqual(1, (int)TruckTaxiPropPhysicsCategory.Breakaway);
            Assert.AreEqual(2, (int)TruckTaxiPropPhysicsCategory.Destructible);
            Assert.AreEqual(3, (int)TruckTaxiPropPhysicsCategory.Structural);
        }

        [TestCase(TruckTaxiPropPartRole.PortableRestroomShell, TruckTaxiPropPhysicsCategory.Destructible)]
        [TestCase(TruckTaxiPropPartRole.PortableRestroomDoor, TruckTaxiPropPhysicsCategory.Breakaway)]
        [TestCase(TruckTaxiPropPartRole.PortableRestroomFixture, TruckTaxiPropPhysicsCategory.Lightweight)]
        [TestCase(TruckTaxiPropPartRole.LoadBearing, TruckTaxiPropPhysicsCategory.Structural)]
        public void PortablePiecesHaveStableAuthoredRoles(TruckTaxiPropPartRole role, TruckTaxiPropPhysicsCategory expected)
        {
            var go = Box("Portable piece"); go.SetActive(false);
            var prop = go.AddComponent<TruckTaxiBreakawayProp>(); prop.partRole = role;
            prop.stableId = "test.facility.porta." + role;
            Assert.AreEqual(expected, prop.EffectiveCategory);
            Assert.IsTrue(prop.ValidateAuthoring(out string error), error);
            prop.category = TruckTaxiPropPhysicsCategory.Structural;
            Assert.IsTrue(prop.PreservesAuthoredPhysics, "An explicit structural override must win over cosmetic role defaults.");
        }

        [TestCase(TruckTaxiPropPhysicsCategory.Structural)]
        [TestCase(TruckTaxiPropPhysicsCategory.HeavyPushable)]
        [TestCase(TruckTaxiPropPhysicsCategory.LargeVegetation)]
        public void ProtectedCategoriesKeepAuthoredBodyCollisionAndPose(TruckTaxiPropPhysicsCategory category)
        {
            var go = Box("Protected fixture"); go.SetActive(false);
            var body = go.AddComponent<Rigidbody>(); body.isKinematic = true; body.mass = 850;
            var target = go.AddComponent<TruckTaxiImpactTarget>(); target.kind = TaxiImpactKind.Property;
            var prop = go.AddComponent<TruckTaxiBreakawayProp>(); prop.category = category; go.SetActive(true);
            Vector3 position = go.transform.position;
            Assert.IsFalse(prop.InitializePhysics()); Assert.IsFalse(target.Hit());
            prop.RetirePhysics(); prop.ResetProp(); target.ResetTarget();
            Assert.IsFalse(go.GetComponent<Collider>().isTrigger); Assert.IsTrue(go.GetComponent<Collider>().enabled);
            Assert.IsTrue(body.isKinematic); Assert.AreEqual(850, body.mass);
            Assert.AreEqual(position, go.transform.position); Assert.IsFalse(prop.Activated);
        }

        [Test]
        public void LegacyTargetHitsApplyRepeatedDurabilityButAwardPropertyDamageOnlyOnce()
        {
            var prop = Prop(); prop.durability = 10; prop.ResetProp(); prop.debrisPieces = 0;
            var target = prop.GetComponent<TruckTaxiImpactTarget>();
            Assert.IsTrue(target.Hit()); Assert.AreEqual(6, prop.RemainingDurability);
            Assert.IsFalse(target.Hit()); Assert.AreEqual(6, prop.RemainingDurability, "Same-contact duplicates are debounced.");
            prop.MaintainPhysics(.6f);
            Assert.IsFalse(target.Hit(), "Repeated damage must not create another first-hit score.");
            Assert.AreEqual(2, prop.RemainingDurability);
            prop.MaintainPhysics(.6f); Assert.IsFalse(target.Hit());
            Assert.IsTrue(prop.Destroyed); Assert.AreEqual(3, prop.AcceptedHitCount);
            Assert.AreEqual(0, prop.RemainingDurability);
            Assert.IsTrue(prop.GetComponent<Rigidbody>().isKinematic);
            Assert.AreEqual(baselineBodies, TruckTaxiPropPhysicsBudget.Shared.ActiveBodies);
            target.ResetTarget(); Assert.IsFalse(target.Damaged); Assert.IsFalse(prop.Destroyed);
            Assert.IsTrue(target.Hit());
        }

        [TestCase(false)] [TestCase(true)]
        public void MeasuredProjectileHookDebouncesContactsAndRestoresPriorIgnoreState(bool previouslyIgnored)
        {
            var prop = Prop(); var target = prop.GetComponent<TruckTaxiImpactTarget>();
            var source = Box("Impact source").AddComponent<Rigidbody>(); source.mass = 2000;
            var a = prop.GetComponent<Collider>(); var b = source.GetComponent<Collider>();
            Physics.SyncTransforms(); Physics.IgnoreCollision(a, b, previouslyIgnored);
            Assert.IsTrue(target.Hit(source, prop.transform.position, 4));
            Assert.AreEqual(96, prop.RemainingDurability); Assert.IsTrue(Physics.GetIgnoreCollision(a, b));
            Assert.IsFalse(target.Hit(source, prop.transform.position, 4)); Assert.AreEqual(96, prop.RemainingDurability);
            prop.MaintainPhysics(.6f);
            Assert.AreEqual(previouslyIgnored, Physics.GetIgnoreCollision(a, b));
            Assert.IsFalse(target.Hit(source, prop.transform.position, 4)); Assert.AreEqual(92, prop.RemainingDurability);
            prop.RetirePhysics(); Assert.AreEqual(previouslyIgnored, Physics.GetIgnoreCollision(a, b));
        }

        [Test]
        public void BudgetCapsBothKindsPrunesDestroyedBodiesAndReleaseIsIdempotent()
        {
            var budget = new TruckTaxiPropPhysicsBudget(2, 1);
            var a = Box("Body").AddComponent<Rigidbody>(); var b = Box("Debris").AddComponent<Rigidbody>();
            var c = Box("Overflow").AddComponent<Rigidbody>();
            Assert.IsTrue(budget.TryAcquire(a, false)); Assert.IsTrue(budget.TryAcquire(a, false));
            Assert.IsFalse(budget.TryAcquire(a, true));
            Assert.IsTrue(budget.TryAcquire(b, true)); Assert.IsFalse(budget.TryAcquire(c, true));
            Assert.IsFalse(budget.TryAcquire(c, false));
            budget.Release(a); budget.Release(a); Assert.AreEqual(1, budget.ActiveBodies);
            Object.DestroyImmediate(b.gameObject);
            Assert.AreEqual(0, budget.ActiveBodies); Assert.AreEqual(0, budget.ActiveDebris);
            Assert.IsTrue(budget.TryAcquire(c, true));
        }

        [Test]
        public void FullGlobalBudgetCannotBeBypassedByImpactTargetHit()
        {
            var prop = Prop();
            var budget = TruckTaxiPropPhysicsBudget.Shared;
            while (budget.ActiveBodies < budget.MaximumBodies)
                Assert.IsTrue(budget.TryAcquire(Box("Reserved body").AddComponent<Rigidbody>(), false));
            Assert.IsFalse(prop.GetComponent<TruckTaxiImpactTarget>().Hit());
            Assert.IsFalse(prop.Activated); Assert.IsTrue(prop.Retired);
            Assert.IsTrue(prop.GetComponent<Rigidbody>().isKinematic);
            Assert.IsFalse(prop.GetComponent<Collider>().enabled, "Denied small props must not become static truck blockers.");
            Assert.AreEqual(budget.MaximumBodies, budget.ActiveBodies);
        }

        [Test]
        public void RetirementKeepsCurrentPoseNonblockingAndResetPreservesHiddenAuthoredParts()
        {
            var go = Box("Reset fixture"); go.SetActive(false);
            var hidden = Box("Authored hidden detail"); hidden.transform.SetParent(go.transform, true);
            hidden.GetComponent<Collider>().enabled = false; hidden.GetComponent<Renderer>().enabled = false;
            var prop = go.AddComponent<TruckTaxiBreakawayProp>(); prop.activeLifetimeSeconds = 1; go.SetActive(true);
            Assert.IsTrue(prop.InitializePhysics()); Vector3 home = go.transform.position;
            Assert.IsTrue(prop.TryReceiveHit()); go.transform.position += Vector3.right * 3;
            prop.MaintainPhysics(1);
            Assert.IsTrue(prop.Retired); Assert.AreEqual(home + Vector3.right * 3, go.transform.position);
            Assert.IsFalse(go.GetComponent<Collider>().enabled); Assert.IsTrue(go.GetComponent<Renderer>().enabled);
            prop.ResetProp(); Assert.AreEqual(home, go.transform.position);
            Assert.IsTrue(go.GetComponent<Collider>().enabled); Assert.IsTrue(go.GetComponent<Collider>().isTrigger);
            Assert.IsFalse(hidden.GetComponent<Collider>().enabled); Assert.IsFalse(hidden.GetComponent<Renderer>().enabled);
        }

        [Test]
        public void ShatterRetiresRootAndBoundsOwnedDebrisUntilDisable()
        {
            var prop = Prop(); prop.durability = 1; prop.ResetProp(); prop.debrisPieces = 500;
            Assert.IsTrue(prop.TryReceiveHit()); Assert.IsTrue(prop.Destroyed);
            Assert.That(prop.OwnedDebrisCount, Is.InRange(0, 4));
            Assert.AreEqual(baselineBodies + prop.OwnedDebrisCount, TruckTaxiPropPhysicsBudget.Shared.ActiveBodies);
            Assert.LessOrEqual(TruckTaxiPropPhysicsBudget.Shared.ActiveDebris, TruckTaxiPropPhysicsBudget.Shared.MaximumDebris);
            foreach (var collider in prop.GetComponentsInChildren<Collider>(true)) Assert.IsFalse(collider.enabled);
            for (int i = 0; i < 9; i++) prop.MaintainPhysics(1);
            Assert.AreEqual(0, prop.OwnedDebrisCount); Assert.AreEqual(baselineBodies, TruckTaxiPropPhysicsBudget.Shared.ActiveBodies);
            prop.ResetProp(); Assert.IsTrue(prop.TryReceiveHit());
            typeof(TruckTaxiBreakawayProp).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(prop, null);
            Assert.AreEqual(0, prop.OwnedDebrisCount); Assert.AreEqual(baselineBodies, TruckTaxiPropPhysicsBudget.Shared.ActiveBodies);
        }

        [Test]
        public void MixedStructuralHierarchyAndUnidentifiedPortablePiecesAreRejectedWithoutMutation()
        {
            var root = Box("Unsafe compound"); root.SetActive(false);
            var child = Box("Load-bearing piece"); child.transform.SetParent(root.transform, true);
            child.AddComponent<TruckTaxiBreakawayProp>().partRole = TruckTaxiPropPartRole.LoadBearing;
            var prop = root.AddComponent<TruckTaxiBreakawayProp>();
            Assert.IsFalse(prop.InitializePhysics()); Assert.IsNotEmpty(prop.InitializationFailureReason);
            Assert.IsNull(root.GetComponent<Rigidbody>()); Assert.IsFalse(child.GetComponent<Collider>().isTrigger);
            var unidentified = Box("Unidentified portable door"); unidentified.SetActive(false);
            var door = unidentified.AddComponent<TruckTaxiBreakawayProp>(); door.partRole = TruckTaxiPropPartRole.PortableRestroomDoor;
            Assert.IsFalse(door.InitializePhysics()); Assert.IsNull(unidentified.GetComponent<Rigidbody>());
            Assert.IsFalse(unidentified.GetComponent<Collider>().isTrigger);
        }

        [Test]
        public void InvalidMeasuredHitsNeverActivateOrReservePhysics()
        {
            var prop = Prop(); var source = Box("Invalid impact source").AddComponent<Rigidbody>();
            Assert.IsFalse(prop.TryReceiveImpact(source, prop.transform.position, float.NaN));
            Assert.IsFalse(prop.TryReceiveImpact(source, Vector3.one * float.PositiveInfinity, 4));
            Assert.IsFalse(prop.TryReceiveImpact(source, prop.transform.position, .1f));
            Assert.IsFalse(prop.TryReceiveImpact(prop.GetComponent<Rigidbody>(), prop.transform.position, 4));
            Assert.IsFalse(prop.Activated); Assert.AreEqual(0, prop.AcceptedHitCount);
            Assert.AreEqual(baselineBodies, TruckTaxiPropPhysicsBudget.Shared.ActiveBodies);
        }

        [Test]
        public void MultipleShatteredPropsShareDebrisCapAndReleaseItOnRetirement()
        {
            var budget = TruckTaxiPropPhysicsBudget.Shared;
            int totalChunks = 0;
            for (int i = 0; i < 8; i++)
            {
                var prop = Prop(); prop.durability = 1; prop.debrisPieces = 4; prop.ResetProp();
                Assert.IsTrue(prop.TryReceiveHit()); Assert.IsTrue(prop.Destroyed);
                totalChunks += prop.OwnedDebrisCount;
                Assert.LessOrEqual(budget.ActiveBodies, budget.MaximumBodies);
                Assert.LessOrEqual(budget.ActiveDebris, budget.MaximumDebris);
            }
            Assert.AreEqual(budget.MaximumDebris - baselineDebris, totalChunks);
            foreach (var go in owned)
                go.GetComponent<TruckTaxiBreakawayProp>()?.MaintainPhysics(1);
            foreach (var go in owned)
                go.GetComponent<TruckTaxiBreakawayProp>()?.ResetProp();
            Assert.AreEqual(baselineBodies, budget.ActiveBodies);
            Assert.AreEqual(baselineDebris, budget.ActiveDebris);
        }

        [Test]
        public void LateStructuralConfigurationRestoresOriginalColliderAndRemovesOwnedBody()
        {
            var prop = Prop(TruckTaxiPropPhysicsCategory.Breakaway);
            Assert.IsTrue(prop.GetComponent<Collider>().isTrigger);
            prop.Configure(TruckTaxiPropPhysicsCategory.Structural, 100);
            Assert.IsFalse(prop.GetComponent<Collider>().isTrigger); Assert.IsTrue(prop.GetComponent<Collider>().enabled);
            Assert.IsNull(prop.GetComponent<Rigidbody>());
        }
    }
}
