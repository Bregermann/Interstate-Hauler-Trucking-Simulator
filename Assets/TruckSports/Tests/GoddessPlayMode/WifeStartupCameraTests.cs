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

public sealed class WifeStartupCameraTests
{
    static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name).Invoke(value,args);
    static bool BannerBlocks(Transform banner,Mesh mesh,Vector3 camera,Vector3 target)
    {
        var a=banner.InverseTransformPoint(camera);var b=banner.InverseTransformPoint(target);
        if(Mathf.Abs(b.z-a.z)<.00001f)return false;
        float t=-a.z/(b.z-a.z);if(t<=0||t>=1)return false;
        var point=Vector3.Lerp(a,b,t);var bounds=mesh.bounds;
        return point.x>=bounds.min.x&&point.x<=bounds.max.x&&point.y>=bounds.min.y&&point.y<=bounds.max.y;
    }

    [UnityTest]
    public IEnumerator SavedChaseCameraKeepsCarriedTruckVisibleAtRepeatedRaceStarts()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"WifeStartupCamera"}))
        {
            Component match=null;var pads=new Gamepad[4];var background=InputSystem.settings.backgroundBehavior;GameObject isolation=null;
            try
            {
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                isolation=new GameObject("Wife startup software-device isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Type.GetType("LWS.TruckSports.SportsProbeInputIsolation, Assembly-CSharp",true));
                for(int i=0;i<4;i++){pads[i]=InputSystem.AddDevice<Gamepad>();InputSystem.EnableDevice(pads[i]);}
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckWifeCarrying/Scenes/TruckWifeCarrying.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckWifeCarrying.WifeMatch, Assembly-CSharp",true),FindObjectsSortMode.None).Single();
                var banner=GameObject.Find("Country finish banner and flags").transform;var mesh=banner.GetComponent<MeshFilter>().sharedMesh;
                var pairs=(Array)Field(match,"pairs");int blocks=0;
                for(int round=0;round<2;round++)
                {
                    Call(match,"SetupScreen");Call(match,"AllBots");var players=(Array)Field(Field(match,"setup"),"players");
                    for(int i=0;i<4;i++){var player=players.GetValue(i);var source=player.GetType().GetField("source");source.SetValue(player,Enum.Parse(source.FieldType,"WholeController"));player.GetType().GetField("pad").SetValue(player,pads[i]);}
                    Type.GetType("LWS.TruckSports.SportsCameraOptions, Assembly-CSharp",true).GetMethod("Set").Invoke(null,new object[]{false});
                    Assert.That((bool)Call(match,"StartMatch"),Is.True);
                    for(int frame=0;frame<5;frame++)
                    {
                        yield return new WaitForSecondsRealtime(frame==0?.2f:.8f);
                        var cameras=(Camera[])Field(match,"cameras");
                        for(int view=0;view<4;view++)
                        {
                            var passenger=(Component)Field(pairs.GetValue(view),"passenger");var body=(Rigidbody)Field(passenger,"body");
                            var target=body.worldCenterOfMass;bool blocked=BannerBlocks(banner,mesh,cameras[view].transform.position,target);if(blocked)blocks++;
                            Debug.Log("WIFE_START_CAMERA round="+round+" sample="+frame+" view="+view+" blocked="+blocked+" camera="+banner.InverseTransformPoint(cameras[view].transform.position)+" target="+banner.InverseTransformPoint(target));
                        }
                    }
                }
                Assert.That(blocks,Is.Zero,"The saved chase camera must not look through the render-only finish banner at countdown, GO, or rematch.");
            }
            finally{Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");foreach(var pad in pads)if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);if(isolation!=null)UnityEngine.Object.Destroy(isolation);InputSystem.settings.backgroundBehavior=background;}
        }
    }
}
#endif
