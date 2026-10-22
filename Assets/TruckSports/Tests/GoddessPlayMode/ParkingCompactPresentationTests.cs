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

public sealed class ParkingCompactPresentationTests
{
    static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
    static object Property(object value,string name)=>value.GetType().GetProperty(name).GetValue(value);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name).Invoke(value,args);
    static Component Text(Transform root)=>root.GetComponent(Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro",true));
    [UnityTest]
    public IEnumerator SharedAndOneToFourHumanSplitViewsKeepAllEightEliminationIconsAndReset()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"ParkingCompactNative"}))
        {
            Component match=null;var pads=new Gamepad[4];GameObject isolation=null;var background=InputSystem.settings.backgroundBehavior;
            try
            {
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                isolation=new GameObject("Parking compact software-device isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Type.GetType("LWS.TruckSports.SportsProbeInputIsolation, Assembly-CSharp",true));
                for(int i=0;i<4;i++){pads[i]=InputSystem.AddDevice<Gamepad>();InputSystem.EnableDevice(pads[i]);}
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckMusicalParking/Scenes/TruckMusicalParking.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckMusicalParking.ParkingMatch, Assembly-CSharp",true),FindObjectsSortMode.None).Single();var views=Field(match,"drivingViews");var menus=Field(match,"menus");var cameraOptions=Type.GetType("LWS.TruckSports.SportsCameraOptions, Assembly-CSharp",true);
                foreach(int humans in new[]{0,1,2,3,4})foreach(bool ai in new[]{false,true})
                {
                    Call(match,"SetupScreen");Call(match,"AllBots");cameraOptions.GetMethod("Set").Invoke(null,new object[]{ai});var players=(Array)Field(Field(match,"setup"),"players");
                    for(int i=0;i<humans;i++){var player=players.GetValue(i);var source=player.GetType().GetField("source");source.SetValue(player,Enum.Parse(source.FieldType,"WholeController"));player.GetType().GetField("pad").SetValue(player,pads[i]);}
                    Call(views,"Select",humans==0?0:1);Assert.That((bool)Call(match,"StartMatch"),Is.True);Time.timeScale=0;yield return new WaitForSecondsRealtime(.3f);Canvas.ForceUpdateCanvases();
                    var root=((GameObject)Field(menus,"hud")).transform.Find("Broadcast participant cards");Assert.That(root.gameObject.activeInHierarchy,Is.True,"Shared competition status must remain visible with split views.");
                    Assert.That(root.Find("Elimination phase backing"),Is.Not.Null,"Phase text needs contrast in bright weather.");
                    int viewCount=((Camera[])Field(match,"cameras")).Count(c=>c!=null&&c.enabled);Assert.That(((RectTransform)root).anchorMin,Is.EqualTo(new Vector2(.5f,viewCount>1?.5f:0)),"A single full-screen view keeps the icons at the bottom.");
                    Assert.That(((Component)Field(menus,"header")).transform.parent.gameObject.activeSelf,Is.False);Assert.That(((Component)Field(menus,"status")).gameObject.activeSelf,Is.False);
                    for(int i=0;i<8;i++){var card=root.Find("Participant card "+i);Assert.That(card.gameObject.activeInHierarchy,Is.True);Assert.That(Property(Text(card.Find("Name")),"text"),Is.EqualTo(Field(players.GetValue(i),"name")));}
                    // Explicit presentation-state fixture, not a claim of an AI elimination.
                    ((bool[])Field(match,"active"))[1]=false;yield return new WaitForSecondsRealtime(.2f);var retired=root.Find("Participant card 1");var image=retired.Find("Actual truck portrait").GetComponent(Type.GetType("UnityEngine.UI.Image, UnityEngine.UI",true));var gray=(Color)Property(image,"color");Assert.That(gray.r,Is.EqualTo(gray.g));Assert.That(gray.g,Is.EqualTo(gray.b));Assert.That(gray.a,Is.LessThan(.7f));Assert.That(Property(Text(retired.Find("Score badge")),"text"),Is.EqualTo("OUT"));
                    Call(match,"SetupScreen");Assert.That((bool)Call(match,"StartMatch"),Is.True);Time.timeScale=0;yield return new WaitForSecondsRealtime(.2f);Assert.That(Property(Text(root.Find("Participant card 1/Score badge")),"text"),Is.EqualTo(""),"Rematch restores active presentation.");
                }
            }
            finally{Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");foreach(var pad in pads)if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);InputSystem.settings.backgroundBehavior=background;if(isolation!=null)UnityEngine.Object.Destroy(isolation);}
        }
    }
}
#endif
