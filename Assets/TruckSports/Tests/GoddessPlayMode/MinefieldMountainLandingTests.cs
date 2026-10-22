#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class MinefieldMountainLandingTests
{
    static Type Type(string name)=>System.Type.GetType(name+", Assembly-CSharp",true);
    static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    static object Property(object value,string name)=>value.GetType().GetProperty(name).GetValue(value);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name).Invoke(value,args);
    [UnityTest]
    public IEnumerator RealNwhControlledPostBlastLandingsRemainValidOnBothOuterRanges()
    {
        using((IDisposable)Type("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"MinefieldMountainNative"}))
        {
            Component match=null;GameObject isolation=null;Keyboard keyboard=null;var background=InputSystem.settings.backgroundBehavior;
            try
            {
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;isolation=new GameObject("Mountain landing software-input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Type("LWS.TruckSports.SportsProbeInputIsolation"));keyboard=InputSystem.AddDevice<Keyboard>();
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckMinefield/Scenes/TruckMinefield.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Type("LWS.TruckMinefield.MinefieldMatch"),FindObjectsSortMode.None).Single();var grounds=((Array)Field(match,"grounds")).Cast<Terrain>().ToArray();
                foreach(var terrain in grounds.Where(t=>t.name=="West meadow"||t.name=="East meadow"))
                {
                    Call(match,"SetupScreen");Call(match,"AllBots");var player=((Array)Field(Field(match,"setup"),"players")).GetValue(0);var source=player.GetType().GetField("source");source.SetValue(player,Enum.Parse(source.FieldType,"Keyboard"));match.GetType().GetField("weather").SetValue(match,1);Assert.That((bool)Call(match,"StartMatch"),Is.True);Time.timeScale=4;
                    float until=Time.realtimeSinceStartup+8;while(Property(match,"Phase").ToString()=="Countdown"&&Time.realtimeSinceStartup<until)yield return null;Assert.That(Property(match,"Phase").ToString(),Is.EqualTo("Driving"));
                    var racers=(Array)Field(match,"racers");for(int i=1;i<racers.Length;i++)Call(racers.GetValue(i),"Fail");foreach(Component hazard in (Array)Field(match,"hazards"))hazard.gameObject.SetActive(false);
                    Vector3 position=Vector3.zero,normal=Vector3.up;bool found=false;float best=5;
                    for(float z=2400;z<6500;z+=80)for(float magnitude=220;magnitude<=298;magnitude+=2)
                    {
                        float x=Mathf.Sign(terrain.transform.position.x)*magnitude,u=(x-terrain.transform.position.x)/terrain.terrainData.size.x,v=(z-terrain.transform.position.z)/terrain.terrainData.size.z;
                        float slope=terrain.terrainData.GetSteepness(u,v);if(slope>=best||terrain.terrainData.GetInterpolatedHeight(u,v)<60)continue;best=slope;position=new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y+8,z);normal=terrain.terrainData.GetInterpolatedNormal(u,v);found=true;
                    }
                    Assert.That(found,Is.True,"The natural range must contain valid landing shelves.");var racer=racers.GetValue(0);var truck=Field(racer,"truck");var body=(Rigidbody)Field(truck,"body");var rotation=Quaternion.FromToRotation(Vector3.up,normal);Call(truck,"Prepare",position,rotation,false);Assert.That((bool)Call(racer,"Launch",position),Is.True);yield return new WaitForFixedUpdate();
                    // Explicit post-blast initial pose/velocity, followed by real
                    // free PhysX motion. This proves a supported landing shelf,
                    // not that an arbitrary violent crest launch cannot escape.
                    body.linearVelocity=Vector3.down*4+Vector3.forward;body.angularVelocity=Vector3.zero;body.rotation=rotation;Debug.Log("MINEFIELD_MOUNTAIN_LANDING_START "+terrain.name+" p="+position+" slope="+best);
                    until=Time.realtimeSinceStartup+45;while(Property(racer,"Phase").ToString()=="Launched"&&Time.realtimeSinceStartup<until)yield return new WaitForFixedUpdate();
                    Assert.That(Property(racer,"Phase").ToString(),Is.EqualTo("Landed"));Assert.That((int)Property(racer,"LandingLocks"),Is.EqualTo(1));Assert.That((bool)Property(racer,"OutsideFinalized"),Is.False);Assert.That((bool)Property(racer,"RecoveryLocked"),Is.False);
                    var landing=(Vector3)Property(racer,"LandingOrigin");int expected=(int)Type("LWS.TruckMinefield.MinefieldRules").GetMethod("Precision").Invoke(null,new object[]{landing.z});Assert.That((int)Property(racer,"Distance"),Is.EqualTo(expected));Assert.That(Mathf.Abs(landing.x),Is.GreaterThan(145));Debug.Log("MINEFIELD_MOUNTAIN_LANDING "+terrain.name+" start="+position+" landed="+landing+" distance="+expected);
                }
                Call(match,"SetupScreen");Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return null;Assert.That(((Array)Field(match,"racers")).Cast<object>().All(r=>(int)Property(r,"LandingLocks")==0),Is.True);Call(match,"SetupScreen");
            }
            finally{Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(isolation!=null)UnityEngine.Object.Destroy(isolation);InputSystem.settings.backgroundBehavior=background;}
        }
    }
}
#endif
