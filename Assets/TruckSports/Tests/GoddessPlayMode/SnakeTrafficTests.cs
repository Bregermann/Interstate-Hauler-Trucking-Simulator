#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class SnakeTrafficTests
{
    static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    static object Property(object value,string name)=>value.GetType().GetProperty(name).GetValue(value);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name).Invoke(value,args);
    [UnityTest]
    public IEnumerator SavedTownSpawnsActualMovingUtsTrafficAndResetsWithoutDuplication()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"SnakeTraffic"}))
        {
            Component match=null;
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSnake/Scenes/TruckSnake.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckSnake.SnakeMatch, Assembly-CSharp",true),FindObjectsSortMode.None).Single();Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);var traffic=(Component)Field(match,"traffic");
                Assert.That((int)Property(traffic,"LogicalCount"),Is.GreaterThan(0),"A nonzero density target does not prove traffic exists; the saved lane list must produce logical cars.");
                Time.timeScale=2;yield return new WaitForSecondsRealtime(3);int actors=(int)Property(traffic,"ActiveCount");Assert.That(actors,Is.GreaterThan(10));Assert.That((int)Property(traffic,"FullPhysicsCount"),Is.LessThanOrEqualTo((int)Field(traffic,"maxFullTraffic")));
                var type=Type.GetType("LWS.TruckTaxi.TruckTaxiTrafficBehaviour, LWS.TruckTaxi.Runtime",true);var cars=traffic.GetComponentsInChildren(type).Cast<Component>().Where(c=>c.gameObject.activeInHierarchy).ToArray();Assert.That(cars.Length,Is.GreaterThan(10));var previous=cars.Select(c=>c.transform.position).ToArray();yield return new WaitForSecondsRealtime(1);int moved=cars.Select((c,i)=>c!=null&&Vector3.Distance(c.transform.position,previous[i])>1).Count(v=>v);Assert.That(moved,Is.GreaterThan(3),"Actual native traffic actors must move on the preserved city loops.");
                Debug.Log("SNAKE_TRAFFIC logical="+Property(traffic,"LogicalCount")+" actors="+actors+" moving="+moved+" full="+Property(traffic,"FullPhysicsCount"));
                Call(match,"SetupScreen");Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return new WaitForSecondsRealtime(.5f);Assert.That((int)Property(traffic,"LogicalCount"),Is.GreaterThan(0));Assert.That((int)Property(traffic,"ActiveCount"),Is.LessThanOrEqualTo((int)Field(traffic,"maxFullTraffic")+(int)Field(traffic,"maxVisibleTraffic")),"Rematch must reuse the adapter's bounded population.");
            }
            finally{Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");}
        }
    }
}
#endif
