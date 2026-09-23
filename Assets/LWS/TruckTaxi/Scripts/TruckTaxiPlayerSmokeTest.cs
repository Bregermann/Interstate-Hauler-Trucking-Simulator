#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Opt-in CI validation only. Normal launches never create this component.
    public sealed class TruckTaxiPlayerSmokeTest : MonoBehaviour
    {
        private bool failed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void LaunchIfRequested()
        {
            if(Application.isEditor || !Array.Exists(Environment.GetCommandLineArgs(),a=>a=="-truck-taxi-smoke")) return;
            new GameObject("Truck Taxi command-line smoke test").AddComponent<TruckTaxiPlayerSmokeTest>();
        }
        private void OnEnable() { Application.logMessageReceived+=TrackError; }
        private void OnDisable() { Application.logMessageReceived-=TrackError; }
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
            host.Session.OfferRide();
            yield return new WaitForSecondsRealtime(1);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"Windows_Offer.png"));
            host.Session.AcceptRide();
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
            host.TeleportNear(host.Session.Destination);
            deadline=Time.realtimeSinceStartup+8;
            while(host.Session.State!=TruckTaxiState.RideComplete && Time.realtimeSinceStartup<deadline) yield return null;
            if(host.Session.CompletedRides!=1) Debug.LogError("TRUCK TAXI PLAYER SMOKE: ride did not complete.");
            yield return new WaitForSecondsRealtime(1);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"Windows_Fare.png"));
            yield return new WaitForSecondsRealtime(1);
            Debug.Log(failed ? "TRUCK TAXI PLAYER SMOKE FAIL" : "TRUCK TAXI PLAYER SMOKE PASS: one teleport-assisted ride in built Windows player.");
            Application.Quit(failed?2:0);
        }
    }
}
#endif
