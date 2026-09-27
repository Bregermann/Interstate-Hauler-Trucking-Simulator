using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiReversibleTestRagdoll : MonoBehaviour, ITruckTaxiPedestrianRagdoll
    {
        public void ActivateRagdoll() { }
    }

    [Category("TaxiRegional")]
    public sealed class TruckTaxiPedestrianPoolPlayModeTests
    {
        [UnityTest]
        public IEnumerator RagdollCleanupReusesWholeUtsActorWithRestoredBodies()
        {
            var person = new GameObject("pooled pedestrian");
            var tractor = new GameObject("tractor");
            try
            {
                var tractorBody = tractor.AddComponent<Rigidbody>();
                tractor.AddComponent<BoxCollider>();
                var walkingBody = person.AddComponent<Rigidbody>();
                var capsule = person.AddComponent<CapsuleCollider>();
                var bone = new GameObject("ragdoll bone");
                bone.transform.SetParent(person.transform, false);
                bone.transform.localPosition = Vector3.up;
                var boneBody = bone.AddComponent<Rigidbody>();
                bone.AddComponent<SphereCollider>();
                bone.AddComponent<CharacterJoint>().connectedBody = walkingBody;
                var secondBone = new GameObject("second ragdoll bone");
                secondBone.transform.SetParent(person.transform, false);
                secondBone.transform.localPosition = Vector3.up * 1.5f;
                secondBone.AddComponent<Rigidbody>();
                secondBone.AddComponent<SphereCollider>();
                secondBone.AddComponent<CharacterJoint>().connectedBody = boneBody;
                person.AddComponent<TruckTaxiReversibleTestRagdoll>();
                var pedestrian = person.AddComponent<TruckTaxiPedestrian>();
                Assert.IsTrue(pedestrian.HasJointedRagdoll);
                pedestrian.ConfigureTractor(tractorBody);
                var trigger = person.GetComponentInChildren<TruckTaxiPedestrianTractorTrigger>();
                Assert.IsNotNull(trigger);
                var pool = new TruckTaxiPedestrianPool();
                Assert.IsTrue(pedestrian.TryStrike(Vector3.forward * 8, Vector3.up));
                Assert.IsTrue(pedestrian.IsRagdoll);
                Assert.IsFalse(boneBody.isKinematic);
                pool.Release(pedestrian);
                Assert.AreEqual(1, pool.Count);
                Assert.IsFalse(person.activeSelf);
                Assert.IsTrue(boneBody.isKinematic);
                Assert.IsFalse(capsule.enabled);

                var reused = pool.Acquire(new Vector3(10, 0, 0), Quaternion.identity, "taxi.pedestrian.17", true);
                Assert.AreSame(pedestrian, reused);
                Assert.AreEqual(0, pool.Count);
                Assert.IsTrue(person.activeSelf);
                Assert.IsFalse(reused.IsRagdoll);
                Assert.IsTrue(boneBody.isKinematic);
                Assert.AreEqual(Vector3.up, bone.transform.localPosition);
                Assert.IsTrue(capsule.enabled);
                Assert.IsTrue(trigger.GetComponent<Collider>().enabled);
                Assert.IsFalse(walkingBody.isKinematic);
                Assert.IsTrue(reused.TryStrike(Vector3.forward * 8, person.transform.position + Vector3.up));
                Assert.IsFalse(boneBody.isKinematic);
                yield return null;
            }
            finally { Object.Destroy(person); Object.Destroy(tractor); }
        }
    }
}
