#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class SnakeCompetitiveFlowTests
{
    static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    static object Property(object value,string name)=>value.GetType().GetProperty(name).GetValue(value);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name).Invoke(value,args);
    [UnityTest]
    public IEnumerator FourPhysicalAiTrainsCollectAndCompleteWithoutScoreInjection()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"SnakeCompetitiveFlow"}))
        {
            Component match=null;GameObject isolation=null;
            try
            {
                isolation=new GameObject("Snake competitive-flow device isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Type.GetType("LWS.TruckSports.SportsProbeInputIsolation, Assembly-CSharp",true));
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSnake/Scenes/TruckSnake.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckSnake.SnakeMatch, Assembly-CSharp",true),FindObjectsSortMode.None).Single();Call(match,"AllBots");match.GetType().GetField("option").SetValue(match,1);var presets=Field(match,"presets");foreach(var preset in (Array)Field(presets,"entries"))if((string)Field(preset,"displayName")=="Clear"){Call(presets,"Select",Field(preset,"id"));break;}UnityEngine.Random.InitState(61993);Assert.That((bool)Call(match,"StartMatch"),Is.True);Time.timeScale=2;
                float until=Time.realtimeSinceStartup+135;int maximum=0;var convoys=(Array)Field(match,"convoys");float next=0;
                while(!(bool)Property(match,"Finished")&&Time.realtimeSinceStartup<until)
                {
                    foreach(var convoy in convoys)maximum=Mathf.Max(maximum,(int)Property(convoy,"ActiveCount"));
                    if(Time.realtimeSinceStartup>=next){next=Time.realtimeSinceStartup+5;Debug.Log("SNAKE_COMPETITIVE clock="+Property(Field(match,"rules"),"Clock")+" physicalPickups="+Property(match,"PhysicalPickups")+" maximum="+maximum+" standings="+Property(match,"Standings"));var drivers=(Array)Field(match,"drivers");var trucks=(Array)Field(match,"trucks");for(int slot=0;slot<4;slot++)Debug.Log("SNAKE_ROUTE slot="+slot+" position="+((Rigidbody)Field(trucks.GetValue(slot),"body")).position+" "+Property(drivers.GetValue(slot),"RouteDiagnostic"));}
                    yield return null;
                }
                Assert.That((bool)Property(match,"Finished"),Is.True,"Actual timed/crash-resolved match must reach results.");Assert.That((int)Property(match,"PhysicalPickups"),Is.GreaterThanOrEqualTo(8),"Keep the existing Windows acceptance threshold: AI must obtain actual contacts, not authored scores.");Assert.That(maximum,Is.GreaterThanOrEqualTo(6));
                Call(match,"SetupScreen");Assert.That((bool)Call(match,"StartMatch"),Is.True);foreach(var convoy in convoys)Assert.That((int)Property(convoy,"ActiveCount"),Is.EqualTo(1));
            }
            finally{Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");if(isolation!=null)UnityEngine.Object.Destroy(isolation);}
        }
    }
}
#endif
