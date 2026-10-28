#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

// Test-owned input/storage isolation, shared by the two tests of the SAME
// wilderness. No input, score, pose, inventory or care result is injected.
public sealed class WildernessNativeScope:IDisposable
{
    public const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    public static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    public static object Get(object o,string n)=>o.GetType().GetField(n,Any) is FieldInfo f?f.GetValue(o):o.GetType().GetProperty(n,Any).GetValue(o);
    public static void Set(object o,string n,object v){var f=o.GetType().GetField(n,Any);if(f!=null)f.SetValue(o,v);else o.GetType().GetProperty(n,Any).SetValue(o,v);}
    public static object Call(object o,string n,params object[] a)=>o.GetType().GetMethods(Any).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length&&m.GetParameters().Select((p,i)=>a[i]==null||p.ParameterType.IsInstanceOfType(a[i])).All(x=>x)).Invoke(o,a);
    public static object Sports(string t,string n,params object[] a)=>Runtime("LWS.TruckSports."+t).GetMethod(n,BindingFlags.Public|BindingFlags.Static).Invoke(null,a);
    public static Component[] Components(object list)=>((IEnumerable)list).Cast<Component>().ToArray();
    public Component match;
    public Keyboard keyboard;
    public readonly string mode,path,progress;
    public readonly List<Gamepad> pads=new List<Gamepad>();
    readonly IDisposable prefs;
    readonly InputSettings original,fixture;
    readonly GameObject isolation;
    public WildernessNativeScope(string name)
    {
        mode="WildernessValidation_"+Guid.NewGuid().ToString("N");path=Path.Combine(Application.persistentDataPath,"TruckSports","Workshops",mode);Assert.That(Directory.Exists(path),Is.False);
        original=InputSystem.settings;fixture=UnityEngine.Object.Instantiate(original);fixture.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;fixture.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;fixture.updateMode=InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;InputSystem.settings=fixture;
        prefs=(IDisposable)Sports("SportsPrefs","IsolateValidation",mode.Replace("_", ""));Sports("SportsParticipantRegistry","ForgetCache");
        isolation=new GameObject("Owned wilderness native input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));keyboard=InputSystem.AddDevice<Keyboard>();
        for(int i=0;i<3;i++)pads.Add(InputSystem.AddDevice<Gamepad>());
        SceneManager.sceneLoaded+=FreshStorageBeforeRepositoryStart;
        progress=Path.GetFullPath("Logs/TruckSports/Impossible07/WildernessNative/"+name+"-"+Guid.NewGuid().ToString("N")+".jsonl");Directory.CreateDirectory(Path.GetDirectoryName(progress));
    }
    void FreshStorageBeforeRepositoryStart(Scene scene,LoadSceneMode ignored)
    {
        foreach(var storage in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren(Runtime("LWS.TruckSports.SportsWorkshopPersistence"),true)).Cast<Component>())
        {
            // Awake resolves a path but performs no I/O. sceneLoaded runs before
            // repository Start: the first real disk read uses ONLY this fresh
            // validation directory, never a player's normal pet/workshop data.
            Set(storage,"modeId",mode);Set(Get(storage,"storage"),"customPath",path);
        }
    }
    public IEnumerator Load(string folder,string type)
    {
        EditorSceneManager.LoadSceneInPlayMode("Assets/"+folder+"/Scenes/"+folder+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
        InputSystem.EnableDevice(keyboard);keyboard.MakeCurrent();foreach(var pad in pads)InputSystem.EnableDevice(pad);
        match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime(type));Assert.That(match,Is.Not.Null);Trace("loaded");
        Assert.That(Path.GetFullPath((string)Get(Get(Get(match,"persistence"),"storage"),"customPath")),Is.EqualTo(Path.GetFullPath(path)),"Test storage was isolated BEFORE repository Start.");
    }
    public void Configure(int count,bool allAI=false)
    {
        Call(match,"SetupScreen");Set(Get(match,"setup"),"count",count);Call(match,"AllBots");Set(match,"option",0);
        if(allAI)return;var players=(Array)Get(Get(match,"setup"),"players");
        for(int i=0;i<count;i++){var player=players.GetValue(i);Set(player,"source",Enum.Parse(Get(player,"source").GetType(),i==0?"Keyboard":"WholeController"));if(i>0)Set(player,"pad",pads[i-1]);}
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();Assert.That((bool)Get(match,"DevicesReady"),Is.True,"Real test-owned devices remain enabled in an unfocused native runner.");
    }
    public void Keys(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.Update();}
    public Key Action=>(Key)((Array)Get(match,"actionKeys")).GetValue(0);
    public IEnumerator Advance(float seconds,float realLimit=8)
    {
        float start=Time.time,end=Time.realtimeSinceStartup+realLimit;while(Time.time-start<seconds&&Time.realtimeSinceStartup<end)yield return null;
        Trace("advance-"+seconds);Assert.That(Time.time-start,Is.GreaterThanOrEqualTo(seconds),"Actual simulation must advance through its normal fixed loop, not hang on a scaled wait.");
    }
    public IEnumerator Tap(){Keys(Action);yield return Advance(.14f);Keys();yield return Advance(.18f);}
    public IEnumerator DriveTo(Component explorer,Vector3 point,float realLimit=30)
    {
        var body=(Rigidbody)Get(Get(explorer,"truck"),"body");var player=((Array)Get(Get(match,"setup"),"players")).GetValue(0);float end=Time.realtimeSinceStartup+realLimit;
        while(Time.realtimeSinceStartup<end)
        {
            if(Vector3.ProjectOnPlane(body.position-point,Vector3.up).magnitude<7.5f&&body.linearVelocity.magnitude<1.2f)break;
            var drive=(Vector2)Call(explorer,"Navigate",point);var keys=new List<Key>();if(drive.x>.15f)keys.Add((Key)Get(player,"right"));else if(drive.x<-.15f)keys.Add((Key)Get(player,"left"));if(drive.y>.05f)keys.Add((Key)Get(player,"forward"));else if(drive.y<-.05f)keys.Add((Key)Get(player,"back"));Keys(keys.ToArray());yield return null;
        }
        Keys();Trace("ordinary-keyboard-drive-to");Assert.That(Vector3.ProjectOnPlane(body.position-point,Vector3.up).magnitude,Is.LessThan(9),"Actual NWH keyboard driving must reach the real activity; no teleport fallback.");yield return Advance(.8f);
    }
    public void Capture(string name,int width=1280,int height=720)=>Runtime("LWS.TruckSports.SportsControlledCapture").GetMethods().Single(m=>m.Name=="Save"&&m.GetParameters().Length==5).Invoke(null,new object[]{match,width,height,Path.GetFullPath("Logs/TruckSports/Impossible07/WildernessControlled/"+name+".png"),null});
    [Serializable]sealed class State{public string checkpoint,screen,activities,pets;public bool live,paused,ready;public float real,scaled,timeScale;}
    public void Trace(string name)
    {
        if(match==null)return;var explorers=Components(Get(match,"explorers"));string pets="";var field=match.GetType().GetField("companions",Any);if(field!=null)pets=string.Join("/",Components(field.GetValue(match)).Where(p=>Get(p,"Record")!=null).Select(p=>Get(p,"Mood")+":"+Get(p,"NaturalPees")));
        string row=JsonUtility.ToJson(new State{checkpoint=name,screen=Get(Get(match,"menus"),"Screen").ToString(),activities=string.Join("/",explorers.Select(e=>Get(e,"Activity")+":collections="+Get(e,"ActualCollections"))),pets=pets,live=(bool)Get(match,"Live"),paused=(bool)Get(match,"Paused"),ready=(bool)Get(match,"DevicesReady"),real=Time.realtimeSinceStartup,scaled=Time.time,timeScale=Time.timeScale});File.AppendAllText(progress,row+"\n");TestContext.WriteLine("WILDERNESS_NATIVE "+row);
    }
    public static string PhysicsIdentity(Component truck)
    {
        var body=(Rigidbody)Get(truck,"body");var shape=(BoxCollider)Get(truck,"shape");return truck.GetInstanceID()+":"+body.GetInstanceID()+":"+body.mass.ToString("R")+":"+body.useGravity+":"+shape.GetInstanceID()+":"+shape.center.ToString("R")+":"+shape.size.ToString("R")+string.Join("/",Components(Get(truck,"wheels")).Select(w=>w.GetInstanceID()+":"+Get(Get(w,"wheel"),"radius")+":"+Get(Get(w,"spring"),"maxLength")));
    }
    public void Dispose()
    {
        Time.timeScale=1;Keys();if(match!=null)Call(match,"SetupScreen");SceneManager.sceneLoaded-=FreshStorageBeforeRepositoryStart;
        if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);foreach(var pad in pads)if(pad.added)InputSystem.RemoveDevice(pad);UnityEngine.Object.DestroyImmediate(isolation);InputSystem.settings=original;UnityEngine.Object.DestroyImmediate(fixture);Sports("SportsParticipantRegistry","ForgetCache");prefs.Dispose();Sports("SportsParticipantRegistry","ForgetCache");
        string parent=Path.GetFullPath(Path.Combine(Application.persistentDataPath,"TruckSports","Workshops"))+Path.DirectorySeparatorChar;string target=Path.GetFullPath(path);if(!target.StartsWith(parent,StringComparison.OrdinalIgnoreCase)||Path.GetFileName(target)!=mode||!mode.StartsWith("WildernessValidation_",StringComparison.Ordinal))throw new InvalidOperationException("Refusing cleanup outside the fresh isolated validation directory.");if(Directory.Exists(target))Directory.Delete(target,true);
    }
}
#endif
