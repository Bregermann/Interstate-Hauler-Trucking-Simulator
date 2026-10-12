using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.InterstateHauler.Tests.PlayMode
{
    public sealed class LwsSaveLoadPhysicsPlayModeTests
    {
        private Scene scene;
        private PhysicsScene physicsScene;
        private LwsSaveLoadCoordinator coordinator;

        [SetUp]
        public void SetUp()
        {
            scene = SceneManager.CreateScene("save-load-physics-fixture",
                new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            physicsScene = scene.GetPhysicsScene();
            coordinator = new LwsSaveLoadCoordinator(null, () => null);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (scene.IsValid() && scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(scene);
        }

        [Test]
        public void FinalizationWakesRestoredTruckAndTrailerWithoutChangingPoseVelocityOrConstraints()
        {
            Rigidbody truck = CreateBody("restored-truck", new Vector3(0, 100, 0));
            Rigidbody trailer = CreateBody("restored-trailer", new Vector3(10, 100, 0));
            Rigidbody[] restored = { truck, trailer };
            Vector3[] positions = { truck.position, trailer.position };
            Quaternion[] rotations = { truck.rotation, trailer.rotation };
            const RigidbodyConstraints constraints = RigidbodyConstraints.FreezeRotation |
                RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;

            foreach (Rigidbody body in restored)
            {
                body.constraints = constraints;
                body.linearVelocity = Vector3.up * 2;
                Stage(body.gameObject);
                Assert.IsTrue(body.IsSleeping(), "Placement must leave a genuinely sleeping Rigidbody.");
                Assert.AreEqual(Vector3.zero, body.linearVelocity);
                Assert.AreEqual(Vector3.zero, body.angularVelocity);
            }

            Physics.SyncTransforms();
            physicsScene.Simulate(.02f);
            for (int i = 0; i < restored.Length; i++)
            {
                Assert.IsTrue(restored[i].IsSleeping());
                Assert.That(Vector3.Distance(positions[i], restored[i].position), Is.LessThan(.0001f),
                    "Gravity alone must not hide the missing finalization wake.");
            }

            FinalizePhysics();
            for (int i = 0; i < restored.Length; i++)
            {
                Rigidbody body = restored[i];
                Assert.IsFalse(body.IsSleeping(), "The load finalizer must release placement sleep.");
                Assert.IsFalse(body.isKinematic);
                Assert.AreEqual(constraints, body.constraints);
                Assert.That(Vector3.Distance(positions[i], body.position), Is.LessThan(.0001f));
                Assert.That(Quaternion.Angle(rotations[i], body.rotation), Is.LessThan(.001f));
                Assert.AreEqual(Vector3.zero, body.linearVelocity, "Wake must not inject a launch impulse.");
                Assert.AreEqual(Vector3.zero, body.angularVelocity);
            }

            Assert.Less(Physics.gravity.y, 0, "This fixture requires downward gravity.");
            physicsScene.Simulate(.02f);
            for (int i = 0; i < restored.Length; i++)
            {
                Assert.Less(restored[i].position.y, positions[i].y - .001f,
                    "A real physics step must move the released body without input or AddForce.");
                Assert.Less(restored[i].linearVelocity.y, 0);
            }

            // Completion is one-shot, including when the same coordinator is reused.
            foreach (Rigidbody body in restored) body.Sleep();
            FinalizePhysics();
            foreach (Rigidbody body in restored) Assert.IsTrue(body.IsSleeping());
            Stage(truck.gameObject);
            FinalizePhysics();
            Assert.IsFalse(truck.IsSleeping());
            Assert.IsTrue(trailer.IsSleeping());
        }

        [Test]
        public void FinalizationDoesNotActivateExcludedBodiesOrWakeUnrelatedPhysics()
        {
            Rigidbody restored = CreateBody("restored-root", new Vector3(0, 100, 0));
            Rigidbody kinematic = CreateBody("kinematic-child", new Vector3(5, 100, 0));
            Rigidbody inactive = CreateBody("inactive-child", new Vector3(10, 100, 0));
            Rigidbody removed = CreateBody("removed-child", new Vector3(15, 100, 0));
            Rigidbody unrelated = CreateBody("unrelated-body", new Vector3(20, 100, 0));
            kinematic.transform.SetParent(restored.transform, true);
            inactive.transform.SetParent(restored.transform, true);
            removed.transform.SetParent(restored.transform, true);
            Stage(restored.gameObject);

            // Ownership can change between placement and the final physics phase.
            kinematic.isKinematic = true;
            kinematic.constraints = RigidbodyConstraints.FreezeAll;
            inactive.gameObject.SetActive(false);
            Object.DestroyImmediate(removed.gameObject);
            unrelated.Sleep();

            FinalizePhysics();

            Assert.IsFalse(restored.IsSleeping());
            Assert.IsTrue(kinematic.isKinematic);
            Assert.AreEqual(RigidbodyConstraints.FreezeAll, kinematic.constraints);
            Assert.IsFalse(inactive.gameObject.activeSelf);
            Assert.IsTrue(unrelated.IsSleeping(), "Only bodies placed by this load may be woken.");
        }

        private Rigidbody CreateBody(string name, Vector3 position)
        {
            var root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0, 25, 0));
            root.AddComponent<BoxCollider>();
            return root.AddComponent<Rigidbody>();
        }

        private void Stage(GameObject root) => Invoke("ZeroRigidbodies", root);
        private void FinalizePhysics() => Invoke("FinalizeRestoredPhysics");

        private void Invoke(string methodName, params object[] arguments)
        {
            MethodInfo method = typeof(LwsSaveLoadCoordinator).GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, methodName);
            method.Invoke(coordinator, arguments);
        }
    }
}
