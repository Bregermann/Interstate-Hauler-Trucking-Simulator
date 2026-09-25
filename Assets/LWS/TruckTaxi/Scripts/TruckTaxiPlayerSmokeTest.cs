#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using NWH.Common.Cameras;

namespace LWS.TruckTaxi
{
    // Opt-in CI validation only. Normal launches never create this component.
    public sealed class TruckTaxiPlayerSmokeTest : MonoBehaviour
    {
        private bool failed;
        private Keyboard testKeyboard;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void LaunchIfRequested()
        {
            if(Application.isEditor || !Array.Exists(Environment.GetCommandLineArgs(),a=>a=="-truck-taxi-smoke")) return;
            new GameObject("Truck Taxi command-line smoke test").AddComponent<TruckTaxiPlayerSmokeTest>();
        }
        private void OnEnable() { Application.logMessageReceived+=TrackError; }
        private void OnDisable()
        {
            Application.logMessageReceived-=TrackError;
            if(testKeyboard!=null) InputSystem.RemoveDevice(testKeyboard);
        }
        private void TrackError(string condition,string stack,LogType type)
        { if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) failed=true; }
        private IEnumerator Start()
        {
            Application.runInBackground=true;
            Screen.SetResolution(1920,1080,FullScreenMode.Windowed);
            float deadline=Time.realtimeSinceStartup+60;
            while((TruckTaxiBootstrap.Instance==null || !TruckTaxiBootstrap.Instance.Ready) && Time.realtimeSinceStartup<deadline) yield return null;
            var host=TruckTaxiBootstrap.Instance;
            if(host==null || !host.Ready) { Debug.LogError("TRUCK TAXI PLAYER SMOKE: startup timed out."); Application.Quit(2); yield break; }
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../Validation"));
            Directory.CreateDirectory(output);
            yield return new WaitForSecondsRealtime(2);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"Windows_Start.png"));
            host.StartShift();
            yield return new WaitForSeconds(2);
            testKeyboard=InputSystem.AddDevice<Keyboard>("Taxi standalone validation keyboard");
            InputSystem.QueueStateEvent(testKeyboard,new KeyboardState(Key.E));
            yield return null;
            InputSystem.QueueStateEvent(testKeyboard,new KeyboardState());
            yield return new WaitForSeconds(2);
            Vector3 driveStart=host.Player.transform.position;
            InputSystem.QueueStateEvent(testKeyboard,new KeyboardState(Key.W));
            yield return new WaitForSeconds(5);
            InputSystem.QueueStateEvent(testKeyboard,new KeyboardState());
            float distance=Vector3.ProjectOnPlane(host.Player.transform.position-driveStart,Vector3.up).magnitude;
            if(distance<3) Debug.LogError("TRUCK TAXI PLAYER SMOKE: normal keyboard input did not move tractor.");
            else Debug.Log("TRUCK TAXI PLAYER INPUT PASS: keyboard E/W drove "+distance.ToString("0.0")+" metres through existing input/NWH.");
            if(host.traffic.ActiveCount==0 || host.pedestrians.ActiveCount==0)
                Debug.LogError("TRUCK TAXI PLAYER SMOKE: ambient population missing.");
            host.Session.DeclineRide();
            var audition=Array.Find(host.Configuration.passengerDatabase.passengers,p=>p.passengerId=="passenger-d9f5d0d1a6a6");
            if(audition==null) { Debug.LogError("TRUCK TAXI PLAYER SMOKE: demo audition missing."); Application.Quit(2); yield break; }
            host.Session.OfferRide(audition);
            yield return new WaitForSecondsRealtime(1);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"Windows_Offer.png"));
            host.Session.AcceptRide();
            yield return null;
            if(!host.Passengers.Dialogue.AudioPlaying || string.IsNullOrWhiteSpace(host.Passengers.Dialogue.Subtitle))
                Debug.LogError("TRUCK TAXI PLAYER SMOKE: generated greeting audio/subtitle missing.");
            else Debug.Log("TRUCK TAXI PLAYER AUDIO PASS: generated greeting playing through Pixel Crushers bark output with subtitle.");
            yield return new WaitForSecondsRealtime(1);
            if(!host.PickupZone.Visible) Debug.LogError("TRUCK TAXI PLAYER SMOKE: pickup VFX missing.");
            host.TeleportNear(host.Session.Pickup);
            var body=host.Player.GetComponent<Rigidbody>();
            body.isKinematic=true;
            deadline=Time.realtimeSinceStartup+8;
            while(!host.Session.HasPassenger && Time.realtimeSinceStartup<deadline) yield return null;
            if(!host.Session.HasPassenger || host.Session.Requests.Count==0 || !host.GPS.RouteReady)
                Debug.LogError("TRUCK TAXI PLAYER SMOKE: pickup, authored request or route failed.");
            yield return new WaitForSecondsRealtime(2);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"Windows_Passenger.png"));
            var changer=host.Player.GetComponentInChildren<CameraChanger>(true);
            if(changer==null || host.GPS.DashboardScreen==null)
                Debug.LogError("TRUCK TAXI PLAYER SMOKE: cockpit camera/dashboard GPS missing.");
            else
            {
                int priorCamera=changer.currentCameraIndex;
                for(int n=0;n<changer.cameras.Count && !changer.cameras[changer.currentCameraIndex].name.Contains("Driver");n++) changer.NextCamera();
                yield return new WaitForSecondsRealtime(.5f);
                var screen=host.GPS.DashboardScreen;
                if(screen.rect.width<=1 || screen.rect.height<=1 || !screen.gameObject.activeInHierarchy)
                    Debug.LogError("TRUCK TAXI PLAYER SMOKE: physical GPS collapsed or inactive.");
                Debug.Log("TRUCK TAXI PLAYER GPS: rect="+screen.rect+" lossyScale="+screen.lossyScale+" target="+host.GPS.TargetId+" route="+host.GPS.RouteReady);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"Windows_Cockpit.png"));
                for(int n=0;n<changer.cameras.Count && changer.currentCameraIndex!=priorCamera;n++) changer.NextCamera();
            }
            host.TeleportNear(host.Session.Destination);
            deadline=Time.realtimeSinceStartup+8;
            while(host.Session.State!=TruckTaxiState.RideComplete && Time.realtimeSinceStartup<deadline) yield return null;
            if(host.Session.CompletedRides!=1) Debug.LogError("TRUCK TAXI PLAYER SMOKE: ride did not complete.");
            yield return new WaitForSecondsRealtime(1);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"Windows_Fare.png"));
            yield return new WaitForSecondsRealtime(1);
            host.Session.ContinueShift();
            if(!host.Session.OfferRide(audition)) Debug.LogError("TRUCK TAXI PLAYER SMOKE: second ride unavailable.");
            host.Session.AcceptRide(); host.TeleportNear(host.Session.Pickup);
            deadline=Time.realtimeSinceStartup+8;
            while(!host.Session.HasPassenger && Time.realtimeSinceStartup<deadline) yield return null;
            int prior=host.Passengers.EjectedBodies;
            host.Passengers.uiEjectHeld=true;
            deadline=Time.realtimeSinceStartup+4;
            while(host.Session.HasPassenger && Time.realtimeSinceStartup<deadline) yield return null;
            if(host.Session.State!=TruckTaxiState.PassengerEjected || host.Passengers.EjectedBodies<=prior || host.Passengers.PrimaryActor!=null)
                Debug.LogError("TRUCK TAXI PLAYER SMOKE: hold-to-eject failed.");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"Windows_Ejection.png"));
            yield return new WaitForSecondsRealtime(3.5f);
            if(host.Session.State!=TruckTaxiState.Available) Debug.LogError("TRUCK TAXI PLAYER SMOKE: post-ejection cleanup/next ride failed.");
            else Debug.Log("TRUCK TAXI PLAYER EJECTION PASS: hold input, passenger physics body, fare consequence, cleanup and next-ride availability.");
            Debug.Log(failed ? "TRUCK TAXI PLAYER SMOKE FAIL" : "TRUCK TAXI PLAYER SMOKE PASS: one teleport-assisted ride in built Windows player.");
            Application.Quit(failed?2:0);
        }
    }
}
#endif
