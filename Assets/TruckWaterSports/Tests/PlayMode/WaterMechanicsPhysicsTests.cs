using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckWaterSports.Tests
{
    public sealed class WaterMechanicsPhysicsTests
    {
        Scene localScene;
        PhysicsScene physics;
        GameObject boat;
        Rigidbody body;
        Component vessel, course;
        bool contactObserved;
        float closingSpeed, impulsePerMass;

        static Type Runtime(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (assembly.GetType(name) is Type type) return type;
            throw new InvalidOperationException("Missing runtime type " + name);
        }
        static void Set(object target, string field, object value) => target.GetType().GetField(field).SetValue(target, value);
        static object Call(object target, string method, params object[] values) => target.GetType().GetMethod(method).Invoke(target, values);

        [SetUp]
        public void CreateIsolatedPhysics()
        {
            localScene = SceneManager.CreateScene("Water mechanics native physics " + Guid.NewGuid().ToString("N"), new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            physics = localScene.GetPhysicsScene();
            boat = new GameObject("Actual test hull"); boat.SetActive(false); SceneManager.MoveGameObjectToScene(boat, localScene);
            body = boat.AddComponent<Rigidbody>(); body.mass = 900; body.useGravity = false; body.linearDamping = 0;
            boat.AddComponent<BoxCollider>();
            vessel = boat.AddComponent(Runtime("LWS.TruckWaterSports.WaterVessel")); Set(vessel, "body", body); Set(vessel, "paddles", Array.Empty<Transform>());
            course = boat.AddComponent(Runtime("LWS.TruckWaterSports.WaterCourse"));
            Set(course, "stations", new[] { Vector3.zero, Vector3.forward * 100, Vector3.forward * 200 });
            Set(course, "currentSpeed", 8f); Set(course, "turbulence", 0f); Set(vessel, "course", course);
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Mesh proxy = primitive.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(primitive);
            var floating = boat.AddComponent(Runtime("LWS.TruckSports.SportsFloatingBody"));
            Set(floating, "body", body); Set(floating, "proxy", proxy); Set(vessel, "flotation", floating);
            ((Behaviour)floating).enabled = false;
            contactObserved = false; closingSpeed = impulsePerMass = 0;
            var impact = vessel.GetType().GetEvent("HullImpact");
            var capture = GetType().GetMethod(nameof(ObserveImpact), BindingFlags.Instance | BindingFlags.NonPublic);
            impact.AddEventHandler(vessel, Delegate.CreateDelegate(impact.EventHandlerType, this, capture));
            boat.SetActive(true);
        }

        [UnityTearDown]
        public IEnumerator RemoveIsolatedPhysics()
        {
            if (localScene.IsValid()) yield return SceneManager.UnloadSceneAsync(localScene);
        }

        void ObserveImpact(object observedVessel, Collision collision)
        {
            contactObserved = true;
            for (int i = 0; i < collision.contactCount; i++) closingSpeed = Mathf.Max(closingSpeed, Mathf.Abs(Vector3.Dot(collision.relativeVelocity, collision.GetContact(i).normal)));
            impulsePerMass = Mathf.Max(impulsePerMass, collision.impulse.magnitude / body.mass);
        }

        void StrikeWall(float speed)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = "Actual static impact wall";
            SceneManager.MoveGameObjectToScene(wall, localScene); wall.transform.position = new Vector3(0, 0, 3); wall.transform.localScale = new Vector3(8, 8, .5f);
            Physics.SyncTransforms();
            body.AddForce(Vector3.forward * (speed * body.mass), ForceMode.Impulse);
            for (int i = 0; i < 100 && !contactObserved; i++) physics.Simulate(.02f);
        }

        float Damage() => (float)Runtime("LWS.TruckWaterSports.BlackwaterMatch").GetMethod("ContactDamageFor")
            .Invoke(null, new object[] { closingSpeed, impulsePerMass, 3.5f, .75f, 20f });

        [Test]
        public void ActualHardHullContactProducesBoundedDamage()
        {
            StrikeWall(6);
            Assert.That(contactObserved, Is.True, "The real WaterVessel collision event must fire.");
            Assert.That(closingSpeed, Is.GreaterThan(3.5f)); Assert.That(impulsePerMass, Is.GreaterThan(.75f));
            Assert.That(Damage(), Is.GreaterThan(0).And.LessThanOrEqualTo(20));
        }

        [Test]
        public void ActualGentleDockingContactDoesNotDamageHull()
        {
            StrikeWall(2);
            Assert.That(contactObserved, Is.True); Assert.That(closingSpeed, Is.LessThan(3.5f));
            Assert.That(Damage(), Is.Zero);
        }

        [Test]
        public void CalmSectionBlendsAndMovesActualHullWithReducedCurrent()
        {
            var sectionType = Runtime("LWS.TruckWaterSports.WaterCurrentSection");
            var section = Activator.CreateInstance(sectionType);
            Set(section, "firstSegment", 0); Set(section, "lastSegment", 0); Set(section, "speedMultiplier", .25f); Set(section, "turbulenceMultiplier", .2f); Set(section, "edgeBlendMetres", 10f);
            var sections = Array.CreateInstance(sectionType, 1); sections.SetValue(section, 0); Set(course, "currentSections", sections);
            Vector3 center = new Vector3(0, 0, 50);
            var calm = (Vector3)Call(course, "Current", center, 0f);
            var entry = (Vector3)Call(course, "Current", Vector3.zero, 0f);
            var outside = (Vector3)Call(course, "Current", new Vector3(0, 0, 150), 0f);
            Assert.That(calm.z, Is.EqualTo(2).Within(.001f)); Assert.That(entry.z, Is.EqualTo(8).Within(.001f)); Assert.That(outside.z, Is.EqualTo(8).Within(.001f));
            body.position = center; Physics.SyncTransforms();
            Call(vessel, "Tick", .02f, 0f); physics.Simulate(.02f);
            Assert.That(body.linearVelocity.z, Is.GreaterThan(.01f).And.LessThan(.05f), "The existing current adapter must apply the reduced current as a real force.");
        }
    }
}
