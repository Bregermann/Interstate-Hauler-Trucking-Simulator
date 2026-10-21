#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class FareFightCabLayerRegressionTests
{
    [UnityTest]public IEnumerator FourthViewportCannotRenderTheTaxiHiddenDriverInsideItsDashboard()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);var isolated=(IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"FareHiddenDriver"});
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/TruckFareFight/Scenes/TruckFareFight.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            var type=Type.GetType("LWS.TruckFareFight.FareFightMatch, Assembly-CSharp",true);var match=UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();type.GetMethod("AllBots").Invoke(match,null);
            Type.GetType("LWS.TruckSports.SportsCameraOptions, Assembly-CSharp",true).GetMethod("Set").Invoke(null,new object[]{true});
            var views=type.GetField("drivingViews").GetValue(match);views.GetType().GetMethod("Select").Invoke(views,new object[]{2});Assert.That((bool)type.GetMethod("StartMatch").Invoke(match,null),Is.True);yield return null;
            var cameras=(Camera[])type.GetField("cameras").GetValue(match);Assert.That(cameras.Length,Is.EqualTo(4));
            var driverType=Type.GetType("LWS.TruckFareFight.FareFightDriver, Assembly-CSharp",true);
            foreach(var driver in UnityEngine.Object.FindObjectsByType(driverType,FindObjectsSortMode.None))
            {
                var presentation=driverType.GetField("presentation").GetValue(driver);var occupant=(Transform)presentation.GetType().GetProperty("Occupant").GetValue(presentation);Assert.That(occupant,Is.Not.Null);
                foreach(var renderer in occupant.GetComponentsInChildren<Renderer>(true))foreach(var camera in cameras)Assert.That(camera.cullingMask&(1<<renderer.gameObject.layer),Is.Zero,"Hidden Taxi avatar layer must not alias any of the four cockpit layers: "+renderer.name+" in "+camera.name);
            }
            type.GetMethod("SetupScreen").Invoke(match,null);
        }
        finally{Time.timeScale=1;isolated.Dispose();}
    }
}
#endif
