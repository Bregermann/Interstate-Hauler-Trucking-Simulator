#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class ParkingBayAppearanceTests
{
    static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    static object Property(object value,string name)=>value.GetType().GetProperty(name).GetValue(value);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name).Invoke(value,args);
    static string Text(object value)=>(string)Property(value,"text");
    [UnityTest]
    public IEnumerator NumberedBayGraphicsFollowAuthoritativeStateMotionAndRematchesWithoutColliders()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"ParkingBayAppearanceNative"}))
        {
            Component match=null;GameObject isolation=null;
            try
            {
                isolation=new GameObject("Parking graphics fixture device isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Type.GetType("LWS.TruckSports.SportsProbeInputIsolation, Assembly-CSharp",true));
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckMusicalParking/Scenes/TruckMusicalParking.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckMusicalParking.ParkingMatch, Assembly-CSharp",true),FindObjectsSortMode.None).Single();var type=Type.GetType("LWS.TruckMusicalParking.ParkingBayAppearance, Assembly-CSharp",true);var bays=(Array)Field(match,"bays");
                var canvas=match.transform.Find("Parking world bay graphics");Assert.That(canvas.GetComponentsInChildren<Canvas>(true).Length,Is.EqualTo(1),"Eight signs must share one world canvas.");Assert.That(canvas.GetComponentsInChildren<Collider>(true),Is.Empty,"Presentation cannot obstruct trucks.");
                Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);Time.timeScale=0;yield return new WaitForSecondsRealtime(.3f);
                for(int i=0;i<8;i++)
                {
                    var bay=(Component)bays.GetValue(i);var p=bay.GetComponent(type);Assert.That(p,Is.Not.Null);Assert.That((int)Field(p,"number"),Is.EqualTo(i+1));Assert.That((Vector2)Field(bay,"size"),Is.EqualTo(new Vector2(4.8f,7.5f)));Assert.That(bay.GetComponentsInChildren<Collider>(true),Is.Empty);
                    if(bay.gameObject.activeInHierarchy){var text=Field(p,"numberText");Call(text,"ForceMeshUpdate",false,false);Debug.Log("BAY_NUMBER_FITTING number="+(i+1)+" font="+Property(text,"fontSize")+" truncated="+Property(text,"isTextTruncated"));Assert.That(Text(text),Is.EqualTo((i+1).ToString("00")));Assert.That((bool)Property(text,"isTextTruncated"),Is.False,"Actual large bay digits must fit and render, not only exist as a string.");Assert.That(Text(Field(p,"stateText")),Is.EqualTo("CLOSED"));}
                    Assert.That(((RectTransform)Field(p,"plate")).gameObject.activeSelf,Is.EqualTo(bay.gameObject.activeInHierarchy));
                }
                var first=(Component)bays.GetValue(0);var display=first.GetComponent(type);var plate=(RectTransform)Field(display,"plate");var original=first.transform.position;first.transform.position+=new Vector3(3,0,5);yield return null;yield return null;
                Assert.That(Vector3.Distance(plate.position,first.transform.TransformPoint(new Vector3(0,.085f,-4.45f))),Is.LessThan(.01f));Assert.That(Vector3.Dot(plate.forward,Vector3.down),Is.GreaterThan(.99f));
                // Explicit visual state fixtures; no points or AI outcomes inferred.
                Call(first,"SetOpen",true);yield return new WaitForSecondsRealtime(.3f);Assert.That(Text(Field(display,"stateText")),Is.EqualTo("OPEN"));Assert.That(((GameObject)Field(display,"openMark")).activeSelf,Is.True);Assert.That(((GameObject)Field(display,"closedMark")).activeSelf,Is.False);
                var players=(Array)Field(Field(match,"setup"),"players");var player=players.GetValue(2);Call(first,"Secure",2,(Color)Field(player,"color"),(string)Field(player,"name"));yield return new WaitForSecondsRealtime(.3f);Assert.That(Text(Field(display,"stateText")),Is.EqualTo("SECURED"));Assert.That(Text(Field(display,"ownerText")),Is.EqualTo(Field(player,"name")));Assert.That(((Component)Field(display,"portrait")).gameObject.activeSelf,Is.True);
                first.transform.position=original;Call(bays.GetValue(1),"SetOpen",true);yield return null;
                var rig=(Component)Field(match,"arenaCamera");var frame=(Behaviour)Field(rig,"broadcastFraming");var fit=(Behaviour)Field(match,"framing");bool fitted=fit!=null&&fit.enabled;object fitTarget=fit!=null?Field(fit,"cameraTarget"):null;bool framed=frame!=null&&frame.enabled;bool enabled=((Behaviour)match).enabled;var camera=(Camera)Field(rig,"view");bool ortho=camera.orthographic;
                try
                {
                    if(frame!=null)frame.enabled=false;if(fit!=null){fit.enabled=false;fit.GetType().GetField("cameraTarget").SetValue(fit,null);}((Behaviour)match).enabled=false;
                    camera.orthographic=false;Call(rig,"PoseRaw",new Vector3(0,.12f,-11.45f),new Vector3(0,18,-18),50f,true);yield return null;yield return null;
                    string folder=(string)Type.GetType("LWS.TruckSports.SportsEvidencePaths, Assembly-CSharp",true).GetMethod("Folder").Invoke(null,new object[]{"ParkingBayPreview"});
                    var capture=Type.GetType("LWS.TruckSports.SportsControlledCapture, Assembly-CSharp",true).GetMethods().Single(m=>m.Name=="Save"&&m.GetParameters()[0].ParameterType.Name=="SportsEventSession");capture.Invoke(null,new object[]{match,1280,720,Path.Combine(folder,"native-bay-world-preview.png"),null});Debug.Log("PARKING_BAY_NATIVE_PREVIEW "+folder);
                }
                finally{camera.orthographic=ortho;if(fit!=null){fit.GetType().GetField("cameraTarget").SetValue(fit,fitTarget);fit.enabled=fitted;}if(frame!=null)frame.enabled=framed;((Behaviour)match).enabled=enabled;}
                first.transform.position=original;Call(match,"SetupScreen");Assert.That((bool)Call(match,"StartMatch"),Is.True);Time.timeScale=0;yield return new WaitForSecondsRealtime(.3f);Assert.That(Text(Field(display,"stateText")),Is.EqualTo("CLOSED"));Assert.That(((Component)Field(display,"portrait")).gameObject.activeSelf,Is.False);
                Call(match,"SetupScreen");
            }
            finally{Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");if(isolation!=null)UnityEngine.Object.Destroy(isolation);}
        }
    }
}
#endif
