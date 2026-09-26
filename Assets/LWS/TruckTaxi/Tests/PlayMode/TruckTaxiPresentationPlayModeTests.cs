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
            var passenger=System.Array.Find(host.Configuration.passengerDatabase.passengers,p=>p.passengerName=="Bront Cheerstrike");
            Assert.IsNotNull(passenger,"Authored GPS validation fare missing.");
            Assert.IsTrue(host.Session.OfferRide(passenger));
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
            var priorSettings=TruckTaxiGPSDisplaySettings.Load();
            var settings=host.GPS.DisplaySettings;
            settings.showHud=true; settings.routeColorIndex=0; settings.routeWidth=12;
            settings.hudRangeMeters=settings.cabRangeMeters=700;
            host.GPS.ApplyDisplaySettings(false);
            yield return new WaitForSecondsRealtime(.3f);
            Canvas.ForceUpdateCanvases();
            foreach(var c in host.Player.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if(c.GetType().Name != "CompassProRouteGraphic") continue;
                var g=(UnityEngine.UI.Graphic)c;
                var mesh=g.canvasRenderer.GetMesh();
                Assert.Greater(mesh.vertexCount,0);
                Assert.Greater(g.rectTransform.rect.height,1);
                Assert.IsFalse(g.canvasRenderer.cull);
            }
            TruckTaxiDemoPlayModeTests.Capture("FactoryDrivingHud",1920,1080);
            Assert.IsNotNull(host.hud.GPSSettings);
            host.GPS.CabCompass.GetType().GetProperty("showRoute").SetValue(host.GPS.CabCompass,false);
            yield return null;
            TruckTaxiDemoPlayModeTests.Capture("GpsCabRouteOff",1920,1080);
            host.GPS.CabCompass.GetType().GetProperty("showRoute").SetValue(host.GPS.CabCompass,true);
            yield return null;
            TruckTaxiDemoPlayModeTests.Capture("GpsCabRouteOn",1920,1080);
            Assert.Greater(CabCyanPixels("GpsCabRouteOn",screen),CabCyanPixels("GpsCabRouteOff",screen)+5,"The vendor route must actually render on the physical screen, not just have geometry.");
            try
            {
                for(int i=0;i<changer.cameras.Count;i++)
                {
                    changer.NextCamera(); yield return null;
                    Assert.IsTrue(host.GPS.HudCompass.GetComponent<Canvas>().enabled);
                    Assert.IsTrue((bool)host.GPS.HudCompass.GetType().GetProperty("showMiniMap").GetValue(host.GPS.HudCompass));
                }
                host.GPS.ToggleHud(); yield return null;
                Assert.IsFalse((bool)host.GPS.HudCompass.GetType().GetProperty("showMiniMap").GetValue(host.GPS.HudCompass));
                Assert.IsTrue(host.GPS.CabCompass.GetComponent<Canvas>().enabled);
                Assert.IsTrue((bool)host.GPS.CabCompass.GetType().GetProperty("showMiniMap").GetValue(host.GPS.CabCompass));
                host.GPS.FrameOffer(offer); yield return null;
                Assert.IsNotNull(host.GPS.PreviewCamera.targetTexture);
                Assert.IsFalse(host.GPS.HudCompass.GetComponent<Canvas>().enabled);
                host.GPS.FrameOffer(null); yield return null;
                Assert.AreEqual(Vector3.zero,host.GPS.HudCompass.GetType().GetProperty("miniMapFollowOffset").GetValue(host.GPS.HudCompass));
                Assert.AreEqual(settings.hudRangeMeters,host.GPS.HudCompass.GetType().GetProperty("miniMapCaptureSize").GetValue(host.GPS.HudCompass));
                host.GPS.ToggleHud();
                host.hud.GPSSettings.Open(); yield return null;
                Assert.IsTrue(host.Paused);
                var slider=host.hud.Root.Find("GPS display settings/Cab map range").GetComponent<UnityEngine.UI.Slider>();
                slider.value=950;
                Assert.AreEqual(950,host.GPS.CabCompass.GetType().GetProperty("miniMapCaptureSize").GetValue(host.GPS.CabCompass));
                Assert.AreEqual(950,TruckTaxiGPSDisplaySettings.Load().cabRangeMeters);
                yield return new WaitForSecondsRealtime(.3f);
                TruckTaxiDemoPlayModeTests.Capture("GpsSettings1080",1920,1080);
                TruckTaxiDemoPlayModeTests.Capture("GpsSettings1440",2560,1440);
                host.hud.GPSSettings.Close();
                Assert.IsFalse(host.Paused);
            }
            finally { priorSettings.Save(); PlayerPrefs.Save(); }
            Debug.Log($"TAXI PRESENTATION: pickup {offer.ToPickup.Meters:0.0}m [{offer.ToPickup.Source}], trip {offer.Trip.Meters:0.0}m [{offer.Trip.Source}]; exact offer accepted.");
        }
        private static int CabCyanPixels(string name,RectTransform screen)
        {
            var image=new Texture2D(2,2);
            try
            {
                image.LoadImage(System.IO.File.ReadAllBytes(System.IO.Path.Combine(Application.dataPath,"../Builds/TruckTaxiDemo/Validation/"+name+".png")));
                var corners=new Vector3[4]; screen.GetWorldCorners(corners);
                float aspect=Camera.main.aspect;
                Camera.main.aspect=image.width/(float)image.height;
                Vector2 min=Vector2.one,max=Vector2.zero;
                foreach(var corner in corners)
                {
                    Vector2 p=Camera.main.WorldToViewportPoint(corner);
                    min=Vector2.Min(min,p); max=Vector2.Max(max,p);
                }
                Camera.main.aspect=aspect;
                int count=0;
                for(int y=Mathf.Max(0,(int)(min.y*image.height));y<Mathf.Min(image.height,max.y*image.height);y++)
                    for(int x=Mathf.Max(0,(int)(min.x*image.width));x<Mathf.Min(image.width,max.x*image.width);x++)
                    { var p=image.GetPixel(x,y); if(p.g>.5f && p.b>.65f && p.r<.25f) count++; }
                Debug.Log("CAB ROUTE PIXELS "+name+": "+count);
                return count;
            }
            finally { Object.Destroy(image); }
        }
    }
}
