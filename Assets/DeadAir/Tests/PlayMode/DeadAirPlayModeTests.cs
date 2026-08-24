using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DeadAir.Tests.PlayMode
{
    public sealed class DeadAirPlayModeTests
    {
        [UnityTest]
        public IEnumerator SceneBootstrapperCreatesRequiredRuntimeSystems()
        {
            var root = new GameObject("Dead Air Test Bootstrapper");
            try
            {
                root.AddComponent<DeadAirSceneBootstrapper>();
                yield return null;

                Assert.IsNotNull(Object.FindFirstObjectByType<DeadAirGameManager>());
                Assert.IsNotNull(Object.FindFirstObjectByType<DeadAirStoryDirector>());
                Assert.IsNotNull(Object.FindFirstObjectByType<DeadAirGPSDirector>());
                Assert.IsNotNull(GameObject.Find("DEAD_AIR_UNPLACED_GAMEPLAY"));
            }
            finally
            {
                string[] createdRootNames =
                {
                    "Dead Air Test Bootstrapper",
                    "DEAD_AIR",
                    "START",
                    "SYSTEMS",
                    "STORY_TRIGGERS",
                    "CHOICE_TRIGGERS",
                    "ENVIRONMENT",
                    "UI",
                    "DEBUG",
                    "DEAD_AIR_UNPLACED_GAMEPLAY"
                };

                foreach (string rootName in createdRootNames)
                {
                    GameObject go = GameObject.Find(rootName);
                    if (go != null)
                    {
                        Object.Destroy(go);
                    }
                }
            }
        }
    }
}
