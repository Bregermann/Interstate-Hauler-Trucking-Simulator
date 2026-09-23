using System.Collections;
using System.IO;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckTaxi.Tests
{
    public class TruckTaxiDemoPlayModeTests
    {
        private Keyboard keyboard;
        private InputSettings originalInputSettings;
        [TearDown] public void CleanupInput()
        {
            if(keyboard!=null) InputSystem.RemoveDevice(keyboard);
            if(originalInputSettings!=null)
            {
                var temporary=InputSystem.settings;
                InputSystem.settings=originalInputSettings;
                Object.Destroy(temporary);
            }
            Time.timeScale=1;
        }
        [UnityTest]
        public IEnumerator DemoBootsAndCompletesThreeConsecutiveRides()
        {
            yield return SceneManager.LoadSceneAsync("TruckTaxi_DemoCity");
            float deadline=Time.realtimeSinceStartup+45;
            while((TruckTaxiBootstrap.Instance==null || !TruckTaxiBootstrap.Instance.Ready) && Time.realtimeSinceStartup<deadline) yield return null;
            var host=TruckTaxiBootstrap.Instance;
            Assert.IsNotNull(host); Assert.IsTrue(host.Ready);
            Assert.IsNotNull(host.Player); Assert.IsTrue(host.Player.IsReady);
            Assert.AreEqual(1,Object.FindObjectsByType<LwsPlayerTruck>(FindObjectsSortMode.None).Length);
            Assert.IsNull(host.spawner.SpawnedTrailer);
            Assert.IsFalse(LwsApplicationBootstrap.Instance.Registry.TryGet(out ILwsSaveService save));
            Assert.Greater(host.traffic.ActiveCount,0); Assert.Greater(host.pedestrians.ActiveCount,0);
            Assert.AreEqual(20,Object.FindObjectsByType<TruckTaxiRideLocation>(FindObjectsSortMode.None).Length);
            var compass=host.Player.GetComponent<LwsCompassNavigatorProAdapter>();
            yield return null;
            Assert.IsTrue(compass.VendorRuntimeReady,compass.LastError);
            Capture("Start",1920,1080);
            yield return null;
            host.StartShift();
            yield return new WaitForSeconds(3);
            var body=host.Player.GetComponent<Rigidbody>();
            Assert.Greater(body.position.y,-2,"Tractor fell through demo roads.");
            originalInputSettings=InputSystem.settings;
            InputSystem.settings=Object.Instantiate(originalInputSettings);
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            keyboard=InputSystem.AddDevice<Keyboard>("Taxi validation keyboard");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));
            yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return new WaitForSeconds(2);
            Vector3 before=body.position;
            var walkingPerson=Object.FindFirstObjectByType<TruckTaxiPedestrian>();
            Vector3 personBefore=walkingPerson.transform.position;
            var movingCar=System.Array.Find(Object.FindObjectsByType<TruckTaxiImpactTarget>(FindObjectsSortMode.None),t=>t.kind==TaxiImpactKind.Traffic);
            Vector3 carBefore=movingCar.transform.position;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));
            yield return new WaitForSeconds(5);
            float driven=Vector3.ProjectOnPlane(body.position-before,Vector3.up).magnitude;
            var source=host.Player.GetComponent<LwsKeyboardGamepadTruckInputSource>();
            var provider=host.Player.GetComponent<LwsNwhVehicleInputProvider>();
            LwsApplicationBootstrap.Instance.Registry.TryGet(out ILwsGameplayStateService gameplay);
            LwsApplicationBootstrap.Instance.Registry.TryGet(out ILwsVehicleInputService input);
            Debug.Log("TAXI INPUT DIAGNOSTIC: key="+keyboard.wKey.isPressed+" current="+(Keyboard.current==keyboard)+
                " sourceEnabled="+source.enabled+" suppressed="+source.DrivingInputSuppressed+
                " source="+input.ActiveSourceId+" gameplay="+gameplay.CurrentState+" kinematic="+body.isKinematic+
                " continuous="+JsonUtility.ToJson(source.ReadContinuousInput())+" provider="+JsonUtility.ToJson(provider.LastNwhContinuousInput)+
                " controls="+JsonUtility.ToJson(host.Player.TruckControlController.CurrentState)+
                " telemetry="+JsonUtility.ToJson(host.Player.LastTelemetry));
            Capture("Driving",2560,1440);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            Assert.Greater(driven,3,"Normal keyboard throttle did not move the NWH tractor.");
            Assert.Greater(Vector3.Distance(personBefore,walkingPerson.transform.position),.2f,"UTS pedestrian did not walk.");
            Assert.Greater(Vector3.Distance(carBefore,movingCar.transform.position),.2f,"UTS traffic did not move.");
            Debug.Log("TRUCK TAXI NWH INPUT: keyboard E/W drove "+driven.ToString("0.0")+" meters.");
            host.Session.DeclineRide();
            for(int i=0;i<3;i++)
            {
                Assert.IsTrue(host.Session.OfferRide());
                yield return new WaitForSecondsRealtime(.2f);
                Capture("Offer"+i,1920,1080);
                Assert.IsTrue(host.Session.AcceptRide());
                Assert.IsTrue(host.GPS.RouteReady,"Pickup route failed");
                host.TeleportNear(host.Session.Pickup);
                // Deterministic loop integration, not a claim of physical driving/coupling.
                body.isKinematic=true;
                float until=Time.realtimeSinceStartup+6;
                while(host.Session.State!=TruckTaxiState.DrivingToDestination && Time.realtimeSinceStartup<until) yield return null;
                Assert.AreEqual(TruckTaxiState.DrivingToDestination,host.Session.State);
                Assert.AreEqual(host.Session.Destination.locationId,host.GPS.TargetId);
                Assert.IsNotEmpty(host.Session.Passenger.passengerName);
                Assert.Greater(host.Session.Requests.Count,0,"No authored passenger request generated.");
                if(i==0)
                {
                    Debug.Log("TAXI COMPASS: "+compass.BuildDiagnosticsSummary()+" HUD visible="+compass.HudMinimapVisible);
                    foreach(var canvas in host.Player.GetComponentsInChildren<Canvas>(true))
                    {
                        if(!canvas.name.Contains("Compass")) continue;
                        Debug.Log("TAXI MAP CANVAS: "+canvas.name+" active="+canvas.gameObject.activeInHierarchy+" enabled="+canvas.enabled+
                            " rect="+((RectTransform)canvas.transform).rect+" scale="+canvas.transform.lossyScale);
                        foreach(var rect in canvas.GetComponentsInChildren<RectTransform>(true))
                            if(rect.name.Contains("MiniMap")) Debug.Log("TAXI MAP RECT: "+rect.name+" active="+rect.gameObject.activeInHierarchy+
                                " rect="+rect.rect+" pos="+rect.anchoredPosition+" scale="+rect.lossyScale);
                    }
                }
                yield return new WaitForSecondsRealtime(.2f);
                Capture("Passenger"+i,1920,1080);
                host.Session.RecordEvent(TaxiEventType.Shortcut,"test.shortcut");
                host.Session.RecordEvent(TaxiEventType.TrafficRam,"test.traffic",8);
                host.Session.RecordEvent(TaxiEventType.PedestrianHit,"test.npc",8);
                Assert.Greater(host.Session.ChaosScore,0);
                host.TeleportNear(host.Session.Destination);
                until=Time.realtimeSinceStartup+6;
                while(host.Session.State!=TruckTaxiState.RideComplete && Time.realtimeSinceStartup<until) yield return null;
                Assert.AreEqual(TruckTaxiState.RideComplete,host.Session.State);
                Assert.AreEqual(i+1,host.Session.CompletedRides);
                yield return new WaitForSecondsRealtime(.2f);
                Capture("Ride"+i,1920,1080);
                yield return null;
                host.Session.ContinueShift();
            }
            Assert.Greater(host.Session.ShiftEarnings,0);
            body.isKinematic=false;
            host.ResetCity();
            Assert.AreEqual(TruckTaxiState.Available,host.Session.State);
            Assert.AreEqual(1,Object.FindObjectsByType<LwsPlayerTruck>(FindObjectsSortMode.None).Length);
            host.Session.OfferRide(); host.Session.AcceptRide(); host.TeleportNear(host.Session.Pickup);
            body.isKinematic=true;
            float boardDeadline=Time.realtimeSinceStartup+6;
            while(!host.Session.HasPassenger && Time.realtimeSinceStartup<boardDeadline) yield return null;
            Assert.IsTrue(host.Session.HasPassenger);
            body.isKinematic=false;
            foreach(var kind in new[]{TaxiImpactKind.Traffic,TaxiImpactKind.Pedestrian,TaxiImpactKind.Property})
                yield return ValidatePhysicalContact(host,kind);
            host.ResetCity();
            foreach(var rect in host.hud.Root.GetComponentsInChildren<RectTransform>())
                Assert.Greater(rect.rect.height,1,"Collapsed taxi UI: "+rect.name);
            Debug.Log("TRUCK TAXI PLAYMODE: three rides completed without scene reload; routes, UTS population, no trailer, no career persistence, reset verified.");
            host.SetPaused(false);
        }
        private static IEnumerator ValidatePhysicalContact(TruckTaxiBootstrap host,TaxiImpactKind kind)
        {
            // Real PhysX contact against typed fixtures; not a claim of manual street driving.
            var point=new GameObject("Temporary collision test staging").AddComponent<TruckTaxiRideLocation>();
            point.transform.position=new Vector3(-160,.15f,-250);
            host.TeleportNear(point);
            yield return new WaitForSeconds(1);
            var body=host.Player.GetComponent<Rigidbody>();
            var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name="Temporary "+kind+" contact fixture";
            obstacle.transform.position=body.position+Vector3.forward*12+Vector3.up;
            obstacle.transform.localScale=new Vector3(5,3,1);
            obstacle.AddComponent<Rigidbody>().isKinematic=true;
            if(kind==TaxiImpactKind.Pedestrian) obstacle.AddComponent<TruckTaxiPedestrian>();
            var target=obstacle.AddComponent<TruckTaxiImpactTarget>(); target.kind=kind; target.targetId="test.contact."+kind;
            int previous=host.Session.ChaosScore;
            var observer=host.Player.GetComponent<TruckTaxiCollisionObserver>();
            body.linearVelocity=Vector3.forward*12;
            float deadline=Time.realtimeSinceStartup+4;
            while(observer.LastTarget!=target.targetId && Time.realtimeSinceStartup<deadline) yield return null;
            Assert.AreEqual(target.targetId,observer.LastTarget,"No physical "+kind+" contact detected.");
            Assert.Greater(host.Session.ChaosScore,previous,kind+" physical contact was not scored.");
            Debug.Log("TRUCK TAXI PHYSX CONTACT: "+kind+" scored via OnCollisionEnter.");
            Object.Destroy(obstacle); Object.Destroy(point.gameObject);
            yield return null;
        }
        private static void Capture(string name,int width,int height)
        {
            var camera=Camera.main;
            Assert.IsNotNull(camera,"No gameplay camera.");
            var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var overlays=new System.Collections.Generic.List<Canvas>();
            var texture=new RenderTexture(width,height,24);
            var prior=camera.targetTexture;
            var priorActive=RenderTexture.active;
            try
            {
                foreach(var canvas in canvases)
                    if(canvas.isRootCanvas && canvas.renderMode==RenderMode.ScreenSpaceOverlay)
                    { overlays.Add(canvas); canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1; }
                camera.targetTexture=texture;
                Canvas.ForceUpdateCanvases();
                camera.Render(); RenderTexture.active=texture;
                var capture=new Texture2D(width,height,TextureFormat.RGB24,false);
                capture.ReadPixels(new Rect(0,0,width,height),0,0); capture.Apply();
                string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../Builds/TruckTaxiDemo/Validation"));
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory,name+".png"),capture.EncodeToPNG());
                Object.Destroy(capture);
            }
            finally
            {
                camera.targetTexture=prior; RenderTexture.active=priorActive;
                foreach(var canvas in overlays) { canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.worldCamera=null; }
                texture.Release(); Object.Destroy(texture);
            }
        }
    }
}
