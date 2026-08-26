using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.InterstateHauler.Tests.PlayMode
{
    public sealed class LwsGameplayStatePlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            LwsApplicationBootstrap.ResetForTests();
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (LwsApplicationBootstrap.Instance != null)
            {
                Object.Destroy(LwsApplicationBootstrap.Instance.gameObject);
            }

            foreach (LwsPersistencePauseMenu menu in Object.FindObjectsByType<LwsPersistencePauseMenu>(FindObjectsSortMode.None))
            {
                Object.Destroy(menu.gameObject);
            }

            LwsApplicationBootstrap.ResetForTests();
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator BootstrapMovesGameplayStateToFreeDriveWhenServicesAreReady()
        {
            var go = new GameObject("gameplay-state-bootstrap");
            LwsApplicationBootstrap bootstrap = go.AddComponent<LwsApplicationBootstrap>();
            yield return null;

            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsGameplayStateService gameplayState));
            Assert.AreEqual(LwsGameplayState.FreeDrive, gameplayState.CurrentState);
            Assert.IsTrue(gameplayState.AllowsDrivingInput);
            StringAssert.Contains("bootstrap services ready", gameplayState.LastTransitionReason);
        }

        [UnityTest]
        public IEnumerator EscapePauseMenuPausesAndResumesPreviousGameplayState()
        {
            var go = new GameObject("gameplay-state-pause-bootstrap");
            LwsApplicationBootstrap bootstrap = go.AddComponent<LwsApplicationBootstrap>();
            yield return null;

            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsGameplayStateService gameplayState));
            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsPersistenceMenuService menuService));
            Assert.AreEqual(LwsGameplayState.FreeDrive, gameplayState.CurrentState);

            menuService.ShowPauseMenu();
            yield return null;

            Assert.AreEqual(LwsGameplayState.Paused, gameplayState.CurrentState);
            Assert.AreEqual(LwsGameplayState.FreeDrive, gameplayState.PauseReturnState);
            Assert.IsFalse(gameplayState.AllowsDrivingInput);

            menuService.Hide();
            yield return null;

            Assert.AreEqual(LwsGameplayState.FreeDrive, gameplayState.CurrentState);
            Assert.IsTrue(gameplayState.AllowsDrivingInput);
        }

        [UnityTest]
        public IEnumerator DirectF3SaveLoadMenuDoesNotEnterPausedMacroState()
        {
            var go = new GameObject("gameplay-state-f3-bootstrap");
            LwsApplicationBootstrap bootstrap = go.AddComponent<LwsApplicationBootstrap>();
            yield return null;

            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsGameplayStateService gameplayState));
            Assert.IsTrue(bootstrap.Registry.TryGet(out ILwsPersistenceMenuService menuService));

            menuService.ShowSaveLoad();
            yield return null;

            Assert.IsTrue(menuService.IsOpen);
            Assert.AreEqual(LwsGameplayState.FreeDrive, gameplayState.CurrentState);
            Assert.AreEqual(LwsPersistenceMenuOpenContext.DirectSaveLoad, menuService.RuntimeRoot.OpenContext);

            menuService.Hide();
            yield return null;

            Assert.AreEqual(LwsGameplayState.FreeDrive, gameplayState.CurrentState);
        }
    }
}