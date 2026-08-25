using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.InterstateHauler.Tests.PlayMode
{
    public sealed class LwsDashboardMirrorsCabPlayModeTests
    {
        [UnityTest]
        public IEnumerator DashboardControllerInitializesWithoutNwhAndPublishesSnapshot()
        {
            var service = new LwsTruckDashboardService();
            service.Initialize(new LwsServiceContext(new LwsServiceRegistry()));
            GameObject go = new GameObject("dashboard-playmode");
            LwsTruckDashboardController controller = go.AddComponent<LwsTruckDashboardController>();

            LwsServiceResult result = service.RegisterActiveController(controller);
            service.PublishSnapshot(controller, controller.CurrentSnapshot);

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.AreEqual(controller, service.ActiveController);
            yield return null;
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator MirrorQualityOffDisablesRequiredMirrorCameras()
        {
            GameObject root = new GameObject("mirror-playmode");
            Camera left = new GameObject("RenderTextureMirrorCameraL").AddComponent<Camera>();
            Camera right = new GameObject("RenderTextureMirrorCameraR").AddComponent<Camera>();
            left.transform.SetParent(root.transform, false);
            right.transform.SetParent(root.transform, false);
            new GameObject("MirrorGlassL").AddComponent<MeshRenderer>().transform.SetParent(root.transform, false);
            new GameObject("MirrorGlassR").AddComponent<MeshRenderer>().transform.SetParent(root.transform, false);
            LwsTruckMirrorController controller = root.AddComponent<LwsTruckMirrorController>();

            controller.SetQuality(LwsMirrorQuality.Off);

            Assert.IsFalse(left.gameObject.activeSelf);
            Assert.IsFalse(right.gameObject.activeSelf);
            Assert.AreEqual(LwsMirrorQuality.Off, controller.CurrentState.quality);
            yield return null;
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator CabAnchorRegistryKeepsHulaPlaceholderDisabledByDefault()
        {
            GameObject root = new GameObject("cab-life-playmode");
            new GameObject("Cab").transform.SetParent(root.transform, false);
            LwsCabAccessoryAnchorRegistry registry = root.AddComponent<LwsCabAccessoryAnchorRegistry>();

            registry.EnsureInitialized();

            Assert.IsFalse(registry.HulaPlaceholderAttached);
            Assert.IsTrue(registry.TryGetAnchor("IH_CabAnchor_Dashboard01", out LwsCabAccessoryAnchor anchor));
            Assert.IsFalse(anchor.Occupied);
            yield return null;
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator DashboardSnapshotConsumesTruckControlState()
        {
            GameObject root = new GameObject("dashboard-state-playmode");
            LwsTruckControlController controls = root.AddComponent<LwsTruckControlController>();
            LwsTruckDashboardController dashboard = root.AddComponent<LwsTruckDashboardController>();

            controls.ApplyCommandFrame(new LwsVehicleCommandFrame
            {
                parkingBrakeToggle = LwsMomentaryIntent.Pressed,
                highBeamLights = LwsMomentaryIntent.Pressed,
                hazardLights = LwsMomentaryIntent.Pressed
            }, default);

            LwsTruckDashboardSnapshot snapshot = dashboard.BuildSnapshot();

            Assert.IsTrue(snapshot.parkingBrakeOn);
            Assert.IsTrue(snapshot.highBeamsOn);
            Assert.IsTrue(snapshot.hazardsOn);
            yield return null;
            Object.Destroy(root);
        }
    }
}
