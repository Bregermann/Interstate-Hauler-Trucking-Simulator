using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckTaxi.Tests
{
    public class TruckTaxiPedestrianPlayModeTests
    {
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
