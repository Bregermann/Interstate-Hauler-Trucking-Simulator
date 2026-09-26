using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiTestRagdoll : MonoBehaviour, ITruckTaxiPedestrianRagdoll
    {
        public void ActivateRagdoll() { }
    }

    public class TruckTaxiPedestrianPlayModeTests
    {
        [UnityTest] public IEnumerator CanonicalTractorIgnoresWalkingCapsuleAndRagdollBones()
        {
            var tractor = new GameObject("test tractor");
            var person = new GameObject("test pedestrian");
            var world = new GameObject("test world");
            try
            {
                var tractorBody = tractor.AddComponent<Rigidbody>();
                var tractorCollider = tractor.AddComponent<BoxCollider>();
                tractorCollider.size = new Vector3(2, 2, 3);
                var worldCollider = world.AddComponent<BoxCollider>();
                person.AddComponent<Rigidbody>();
                var capsule = person.AddComponent<CapsuleCollider>();
                capsule.center = Vector3.up; capsule.height = 2;
                var bones = new Collider[2];
                for (int i = 0; i < bones.Length; i++)
                {
                    var bone = new GameObject("bone " + i);
                    bone.transform.SetParent(person.transform);
                    bone.transform.localPosition = Vector3.up * (i + 1);
                    bone.AddComponent<Rigidbody>();
                    bones[i] = bone.AddComponent<SphereCollider>();
                }
                person.AddComponent<TruckTaxiTestRagdoll>();
                var pedestrian = person.AddComponent<TruckTaxiPedestrian>();
                pedestrian.ConfigureTractor(tractorBody);
                Assert.IsTrue(Physics.GetIgnoreCollision(capsule, tractorCollider));
                Assert.IsFalse(Physics.GetIgnoreCollision(capsule, worldCollider));
                Assert.IsFalse(Physics.GetIgnoreCollision(bones[0], tractorCollider));
                Assert.IsTrue(pedestrian.TryStrike(Vector3.forward * 8, person.transform.position + Vector3.up));
                foreach (var bone in bones)
                {
                    Assert.IsTrue(Physics.GetIgnoreCollision(bone, tractorCollider));
                    Assert.IsFalse(Physics.GetIgnoreCollision(bone, worldCollider));
                }
                Assert.IsFalse(pedestrian.TryStrike(Vector3.forward * 8, person.transform.position));
                yield return null;
            }
            finally
            {
                Object.Destroy(person);
                Object.Destroy(tractor);
                Object.Destroy(world);
            }
        }

        [UnityTest] public IEnumerator ActualTractorImpactsActivateUtsRagdollAndReplenishPopulation()
        {
            yield return SceneManager.LoadSceneAsync("TruckTaxi_DemoCity");
            float deadline=Time.realtimeSinceStartup+45;
            while((TruckTaxiBootstrap.Instance==null || !TruckTaxiBootstrap.Instance.Ready) && Time.realtimeSinceStartup<deadline) yield return null;
            var host=TruckTaxiBootstrap.Instance; Assert.IsNotNull(host); Assert.IsTrue(host.Ready);
            yield return TruckTaxiPedestrianRuntimeProbe.Run(host,(ok,message)=>Assert.IsTrue(ok,message),
                name=>TruckTaxiDemoPlayModeTests.Capture(name,1920,1080));
            Time.timeScale=1;
        }
    }
}
