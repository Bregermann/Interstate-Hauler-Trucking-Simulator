using System.Collections;
using LWS.InterstateHauler;
using NWH.Common.Cameras;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckTaxi.Tests
{
    public class TruckTaxiPresentationPlayModeTests
    {
        [UnityTest]
        public IEnumerator CockpitPresentation()
        {
            yield return SceneManager.LoadSceneAsync("TruckTaxi_DemoCity");
            float deadline = Time.realtimeSinceStartup + 45;
            while ((TruckTaxiBootstrap.Instance == null || !TruckTaxiBootstrap.Instance.Ready) && Time.realtimeSinceStartup < deadline) yield return null;
            var host = TruckTaxiBootstrap.Instance;
            Assert.IsNotNull(host); Assert.IsTrue(host.Ready);
            var changer = host.Player.GetComponentInChildren<CameraChanger>(true);
            Assert.IsNotNull(changer);
            for (int i = 0; i < changer.cameras.Count && !changer.cameras[changer.currentCameraIndex].name.Contains("Driver"); i++) changer.NextCamera();
            host.StartShift();
            yield return new WaitForSeconds(2);
            host.SetPaused(true);
            host.hud.Root.gameObject.SetActive(false);
            var screen=host.GPS.DashboardScreen;
            Assert.IsNotNull(screen); Assert.AreEqual("interior",screen.parent.name);
            Assert.That(screen.rect.width*screen.lossyScale.x,Is.EqualTo(.1472f).Within(.001));
            Assert.That(screen.rect.height*screen.lossyScale.y,Is.EqualTo(.0782f).Within(.001));
            Assert.AreEqual(2,System.Array.FindAll(host.Player.GetComponentsInChildren<MonoBehaviour>(true),c=>c.GetType().FullName=="CompassNavigatorPro.CompassPro").Length);
            TruckTaxiDemoPlayModeTests.Capture("FactoryCockpit",1920,1080);
            host.hud.Root.gameObject.SetActive(true);
            host.SetPaused(false);
            Assert.IsTrue(host.Session.OfferRide());
            var offer=host.Session.Offer;
            Assert.IsTrue(offer.ToPickup.Navigable,offer.ToPickup.Source);
            Assert.IsTrue(offer.Trip.Navigable,offer.Trip.Source);
            yield return new WaitForSecondsRealtime(1);
            Assert.IsNotNull(host.GPS.PreviewCamera.targetTexture);
            var graphic=host.hud.Root.GetComponentInChildren<TruckTaxiOfferRouteGraphic>();
            Canvas.ForceUpdateCanvases();
            var pathMesh=graphic.canvasRenderer.GetMesh();
            Debug.Log("OFFER PATH MESH: vertices="+pathMesh.vertexCount+" rect="+graphic.rectTransform.rect+" cull="+graphic.canvasRenderer.cull+" color="+graphic.color);
            Assert.Greater(pathMesh.vertexCount,0);
            foreach(var img in screen.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                Debug.Log("CAB IMAGE: "+img.name+" material="+img.material.name+" sprite="+(img.sprite!=null ? img.sprite.name : "none")+" enabled="+img.enabled);
            foreach(var point in new[]{offer.PlayerPosition,offer.Pickup.StopPosition,offer.Destination.StopPosition})
            {
                var viewport=host.GPS.PreviewCamera.WorldToViewportPoint(point);
                Assert.That(viewport.x,Is.InRange(.05f,.95f)); Assert.That(viewport.y,Is.InRange(.05f,.95f));
            }
            TruckTaxiDemoPlayModeTests.Capture("FactoryOffer1080",1920,1080);
            TruckTaxiDemoPlayModeTests.Capture("FactoryOffer1440",2560,1440);
            TruckTaxiDemoPlayModeTests.Capture("FactoryOfferUltrawide",3440,1440);
            Assert.IsTrue(host.Session.AcceptRide());
            Assert.AreSame(offer.Passenger,host.Session.Passenger);
            Assert.AreSame(offer.Pickup,host.Session.Pickup);
            Assert.AreSame(offer.Destination,host.Session.Destination);
            Assert.AreEqual(offer.Pickup.locationId,host.GPS.TargetId);
            yield return new WaitForSecondsRealtime(.3f);
            TruckTaxiDemoPlayModeTests.Capture("FactoryDrivingHud",1920,1080);
            Debug.Log($"TAXI PRESENTATION: pickup {offer.ToPickup.Meters:0.0}m [{offer.ToPickup.Source}], trip {offer.Trip.Meters:0.0}m [{offer.Trip.Source}]; exact offer accepted.");
        }
    }
}
