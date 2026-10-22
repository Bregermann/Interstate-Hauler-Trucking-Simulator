#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class SnakePickupSupplyTests
{
    static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Invoke(value,args);
    static bool Live(Component gate)=>(bool)gate.GetType().GetProperty("Live").GetValue(gate);

    [UnityTest]
    public IEnumerator DistantUncollectedGatesCannotExhaustTheFinitePickupPool()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"SnakePickupSupply"}))
        {
            Component match=null;
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSnake/Scenes/TruckSnake.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckSnake.SnakeMatch, Assembly-CSharp",true),FindObjectsSortMode.None).Single();
                Field(match,"setup").GetType().GetField("count").SetValue(Field(match,"setup"),1);Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);Time.timeScale=0;
                var gates=((Array)Field(match,"pickups")).Cast<Component>().ToArray();var world=Field(match,"world");
                // Fill the existing pool with old city gates, then move the
                // tractor to its actual connected outer road. This explicitly
                // tests pool starvation, not autonomous navigation/collection.
                Vector3 old=gates.First(Live).transform.position-Vector3.up*.15f;
                foreach(var gate in gates){Call(gate,"Spawn",old);Call(gate,"Tick",60f);}
                var remote=new Vector3(-1120,0,-1000);remote.y=(float)Call(world,"Height",remote)+.5f;
                var truck=(Component)((Array)Field(match,"trucks")).GetValue(0);Call(truck,"Prepare",remote,Quaternion.Euler(0,90,0),true);Physics.SyncTransforms();
                yield return new WaitForSecondsRealtime(.5f);
                Assert.That(gates.All(g=>Live(g)&&Vector3.Distance(g.transform.position,remote)>900),Is.True,"All pooled slots are occupied by distant old gates in this lifecycle fixture.");
                Call(match,"SpawnPickups");
                Assert.That(gates.Any(g=>Live(g)&&Vector3.Distance(g.transform.position,remote)<650),Is.True,"Uncollected distant gates must eventually recycle so a new district still receives trailer pickups.");
                var near=gates.First(g=>Live(g)&&Vector3.Distance(g.transform.position,remote)<650);Vector3 before=near.transform.position;Call(near,"Tick",60f);Call(match,"SpawnPickups");Assert.That(near.transform.position,Is.EqualTo(before),"Keep nearby live gates stable; do not make goals pop away from approaching players.");
                foreach(var gate in gates)Call(gate,"Dormant");Call(match,"SpawnPickups");Assert.That(gates.Any(Live),Is.True,"Collected/inactive pool entries refill through the same existing spawn loop.");
                Call(match,"SetupScreen");Assert.That((bool)Call(match,"StartMatch"),Is.True);Assert.That(gates.Any(Live),Is.True,"Rematch restores the reusable gate pool.");
            }
            finally{Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");}
        }
    }
}
#endif
