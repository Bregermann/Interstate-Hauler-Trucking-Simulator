using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiObjectivePlayModeTests
    {
        private bool hadRidePreference;
        private int previousRidePreference;
        [SetUp] public void EnableTestRideRequests()
        {
            hadRidePreference = PlayerPrefs.HasKey(TruckTaxiSession.RideRequestsPreferenceKey);
            previousRidePreference = PlayerPrefs.GetInt(TruckTaxiSession.RideRequestsPreferenceKey, 1);
            PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey, 1);
        }
        [TearDown] public void RestoreRideRequests()
        {
            if (hadRidePreference) PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey, previousRidePreference);
            else PlayerPrefs.DeleteKey(TruckTaxiSession.RideRequestsPreferenceKey);
        }
        [UnityTest] public IEnumerator NoMouseStopsRatingsAndOptionalAdultChoice()
        {
            yield return SceneManager.LoadSceneAsync("TruckTaxi_DemoCity");
            float deadline=Time.realtimeSinceStartup+45;
            while((TruckTaxiBootstrap.Instance==null || !TruckTaxiBootstrap.Instance.Ready) && Time.realtimeSinceStartup<deadline) yield return null;
            Assert.IsTrue(TruckTaxiBootstrap.Instance?.Ready==true);
            yield return TruckTaxiObjectiveRuntimeProbe.Run(TruckTaxiBootstrap.Instance,(ok,message)=>Assert.IsTrue(ok,message),
                name=>TruckTaxiDemoPlayModeTests.Capture(name,1920,1080));
            Time.timeScale=1;
        }
    }
}
