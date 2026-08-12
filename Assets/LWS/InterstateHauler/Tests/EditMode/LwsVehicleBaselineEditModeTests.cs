using System;
using NUnit.Framework;
using UnityEngine;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class LwsVehicleBaselineEditModeTests
    {
        [Test]
        public void TruckDefinitionIdsValidate()
        {
            var prefab = new GameObject("definition-prefab");
            LwsTruckDefinition definition = ScriptableObject.CreateInstance<LwsTruckDefinition>();
            definition.ConfigureForTests("ih.test.truck", prefab);

            bool valid = definition.Validate(out string message);
            bool unique = LwsTruckDefinition.ValidateUniqueIds(new[] { definition }, out string uniqueMessage);

            Assert.IsTrue(valid, message);
            Assert.IsTrue(unique, uniqueMessage);
            UnityEngine.Object.DestroyImmediate(prefab);
            UnityEngine.Object.DestroyImmediate(definition);
        }

        [Test]
        public void DuplicateTruckDefinitionIdsAreRejected()
        {
            var prefab = new GameObject("definition-prefab");
            LwsTruckDefinition first = ScriptableObject.CreateInstance<LwsTruckDefinition>();
            LwsTruckDefinition second = ScriptableObject.CreateInstance<LwsTruckDefinition>();
            first.ConfigureForTests("ih.test.duplicate", prefab);
            second.ConfigureForTests("ih.test.duplicate", prefab);

            bool unique = LwsTruckDefinition.ValidateUniqueIds(new[] { first, second }, out string message);

            Assert.IsFalse(unique);
            StringAssert.Contains("Duplicate", message);
            UnityEngine.Object.DestroyImmediate(prefab);
            UnityEngine.Object.DestroyImmediate(first);
            UnityEngine.Object.DestroyImmediate(second);
        }

        [Test]
        public void NwhAdapterTelemetryDoesNotThrowWhenUninitialized()
        {
            var go = new GameObject("adapter-only");
            LwsNwhVehicleAdapter adapter = go.AddComponent<LwsNwhVehicleAdapter>();

            Assert.DoesNotThrow(() => adapter.ReadTelemetry());
            Assert.AreEqual(default(float), adapter.ReadTelemetry().speedMetersPerSecond);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void TrailerAttachmentStateSerializationShapeValidates()
        {
            var wrapper = new TrailerAttachmentStateWrapper
            {
                state = new LwsTrailerAttachmentState
                {
                    attached = true,
                    towingVehicleId = "truck-a",
                    trailerId = "trailer-a",
                    trailerPose = new LwsSerializablePose
                    {
                        position = new Vector3(1f, 2f, 3f),
                        rotation = Quaternion.identity
                    }
                }
            };

            string json = JsonUtility.ToJson(wrapper);
            TrailerAttachmentStateWrapper copy = JsonUtility.FromJson<TrailerAttachmentStateWrapper>(json);

            Assert.IsTrue(copy.state.attached);
            Assert.AreEqual("trailer-a", copy.state.trailerId);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), copy.state.trailerPose.position);
        }

        [Test]
        public void PlayerVehicleServiceRejectsDuplicateActiveTrucks()
        {
            var service = new LwsPlayerVehicleService();
            service.Initialize(new LwsServiceContext(new LwsServiceRegistry()));

            LwsPlayerTruck first = CreateTruck("truck-one");
            LwsPlayerTruck second = CreateTruck("truck-two");

            LwsServiceResult firstResult = service.RegisterActiveTruck(first);
            LwsServiceResult secondResult = service.RegisterActiveTruck(second);

            Assert.IsTrue(firstResult.Succeeded, firstResult.Message);
            Assert.IsFalse(secondResult.Succeeded);

            UnityEngine.Object.DestroyImmediate(first.gameObject);
            UnityEngine.Object.DestroyImmediate(second.gameObject);
        }

        private static LwsPlayerTruck CreateTruck(string id)
        {
            var go = new GameObject(id);
            LwsVehicleIdentity identity = go.AddComponent<LwsVehicleIdentity>();
            identity.Configure(id, "ih.test.truck", LwsVehicleRole.PlayerTractor, id, true);
            return go.AddComponent<LwsPlayerTruck>();
        }

        [Serializable]
        private sealed class TrailerAttachmentStateWrapper
        {
            public LwsTrailerAttachmentState state;
        }
    }
}
