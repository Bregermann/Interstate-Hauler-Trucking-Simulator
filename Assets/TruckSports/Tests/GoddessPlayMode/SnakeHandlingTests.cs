#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class SnakeHandlingTests
{
    static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    static object Property(object value,string name)=>value.GetType().GetProperty(name).GetValue(value);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name).Invoke(value,args);
    static void Set(object value,string name,object data)=>value.GetType().GetField(name).SetValue(value,data);

    [UnityTest]
    public IEnumerator ModeOwnedNwhTuningImprovesMeasuredLaunchAndTurningWithoutExtraPropulsion()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"SnakeHandling"}))
        {
            Component match=null;GameObject isolation=null;Gamepad pad=null;var background=InputSystem.settings.backgroundBehavior;var editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            try
            {
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                isolation=new GameObject("Snake handling software-device isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Type.GetType("LWS.TruckSports.SportsProbeInputIsolation, Assembly-CSharp",true));pad=InputSystem.AddDevice<Gamepad>();InputSystem.EnableDevice(pad);
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSnake/Scenes/TruckSnake.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckSnake.SnakeMatch, Assembly-CSharp",true),FindObjectsSortMode.None).Single();var world=Field(match,"world");var p=new Vector3(-1120,0,-1000);p.y=(float)Call(world,"Height",p)+.5f;((Vector3[])Field(world,"starts"))[0]=p;((Quaternion[])Field(world,"rotations"))[0]=Quaternion.Euler(0,90,0);
                var tractor=(Component)((Array)Field(match,"trucks")).GetValue(0);var body=(Rigidbody)Field(tractor,"body");var handling=Field(Field(tractor,"tractorMotor"),"handling");var elapsed=new float[2];var turn=new float[2];
                for(int pass=0;pass<2;pass++)
                {
                    if(pass>0)
                    {
                        // A rematch keeps a warm engine/transmission. Reload
                        // the saved scene for a like-for-like cold powertrain
                        // comparison rather than measuring different gears.
                        Call(match,"SetupScreen");
                        EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSnake/Scenes/TruckSnake.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                        match=(Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckSnake.SnakeMatch, Assembly-CSharp",true),FindObjectsSortMode.None).Single();world=Field(match,"world");p=new Vector3(-1120,0,-1000);p.y=(float)Call(world,"Height",p)+.5f;((Vector3[])Field(world,"starts"))[0]=p;((Quaternion[])Field(world,"rotations"))[0]=Quaternion.Euler(0,90,0);
                        tractor=(Component)((Array)Field(match,"trucks")).GetValue(0);body=(Rigidbody)Field(tractor,"body");handling=Field(Field(tractor,"tractorMotor"),"handling");
                    }
                    UnityEngine.Random.InitState(62119);
                    Call(match,"SetupScreen");Set(match,"cruiseSpeed",18f);Set(match,"humanTurnSpeedScale",pass==0?1f:.65f);Set(Field(match,"setup"),"count",1);Call(match,"AllBots");var player=((Array)Field(Field(match,"setup"),"players")).GetValue(0);var source=player.GetType().GetField("source");source.SetValue(player,Enum.Parse(source.FieldType,"WholeController"));Set(player,"pad",pad);InputSystem.QueueStateEvent(pad,new GamepadState());
                    var presets=Field(match,"presets");foreach(var preset in (Array)Field(presets,"entries"))if((string)Field(preset,"displayName")=="Clear"){Call(presets,"Select",Field(preset,"id"));break;}
                    Assert.That((bool)Call(match,"StartMatch"),Is.True);Call(handling,"Restore");Set(handling,"launchPowerAtRest",pass==0?3.4f:4.2f);Set(handling,"launchPowerAt15Mph",pass==0?2.2f:2.8f);Set(handling,"lowSpeedSteeringLock",pass==0?70f:50f);Set(handling,"citySteeringLock",pass==0?40f:36f);Set(handling,"steeringResponse",pass==0?400f:500f);Set(handling,"lowSpeedSmoothing",pass==0?.025f:.015f);Call(handling,"Apply");var transmission=Field(Field(Field(Field(tractor,"tractorMotor"),"vehicle"),"powertrain"),"transmission");Set(transmission,"shiftDuration",pass==0?.35f:.18f);
                    Time.timeScale=2;float deadline=Time.realtimeSinceStartup+10;while((float)Property(match,"Intro")>0&&Time.realtimeSinceStartup<deadline)yield return null;float began=Time.fixedTime;
                    while(body.linearVelocity.magnitude<8.9408f&&Time.fixedTime-began<12&&Time.realtimeSinceStartup<deadline)yield return null;elapsed[pass]=Time.fixedTime-began;Assert.That(body.linearVelocity.magnitude,Is.GreaterThanOrEqualTo(8.9408f),"The real NWH powertrain must reach twenty MPH.");
                    Set(match,"cruiseSpeed",9f);yield return new WaitForSeconds(1.5f);Debug.Log("SNAKE_HANDLING matched turn entry speed="+body.linearVelocity.magnitude);var start=body.position;var forward=tractor.transform.forward;InputSystem.QueueStateEvent(pad,new GamepadState{leftStick=new Vector2(1,0)});began=Time.fixedTime;while(Mathf.Abs(Vector3.SignedAngle(forward,tractor.transform.forward,Vector3.up))<60&&Time.fixedTime-began<5&&Time.realtimeSinceStartup<deadline)yield return null;turn[pass]=Vector3.Distance(start,body.position);
                    Assert.That(Mathf.Abs(Vector3.SignedAngle(forward,tractor.transform.forward,Vector3.up)),Is.GreaterThanOrEqualTo(60));Assert.That(Vector3.Dot(tractor.transform.up,Vector3.up),Is.GreaterThan(.7f),"Arcade turning must retain upright real truck physics.");Debug.Log("SNAKE_HANDLING pass="+pass+" secondsTo20mph="+elapsed[pass]+" distanceTo60degreeTurn="+turn[pass]+" wheelAngle="+Property(handling,"SteeringAngle")+" exitSpeed="+body.linearVelocity.magnitude);
                    InputSystem.QueueStateEvent(pad,new GamepadState());Time.timeScale=1;
                }
                Assert.That(elapsed[1],Is.LessThan(elapsed[0]*.98f),"Mode tuning must measurably improve launch responsiveness.");Assert.That(turn[1],Is.LessThan(turn[0]),"The tuned NWH steering must produce a tighter physical turn.");
            }
            finally{Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);if(isolation!=null)UnityEngine.Object.Destroy(isolation);InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;}
        }
    }
}
#endif
