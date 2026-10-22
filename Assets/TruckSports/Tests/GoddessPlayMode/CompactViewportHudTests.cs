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

public sealed class CompactViewportHudTests
{
    static object Field(object target,string name)=>target.GetType().GetField(name).GetValue(target);
    static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name).Invoke(target,args);
    static string Text(Transform target){var text=target.GetComponent(Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro",true));return (string)text.GetType().GetProperty("text").GetValue(text);}
    static Rect ScreenRect(RectTransform rect){var corners=new Vector3[4];rect.GetWorldCorners(corners);var min=RectTransformUtility.WorldToScreenPoint(null,corners[0]);var max=RectTransformUtility.WorldToScreenPoint(null,corners[2]);return Rect.MinMaxRect(min.x,min.y,max.x,max.y);}

    [UnityTest]
    public IEnumerator TerrierSharedCardsShowLocalHumansAndLeadersWithoutDuplicatingDisplayNameScores()
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"TerrierSharedCards"}))
        {
            Component match=null;
            try
            {
                EditorSceneManager.LoadSceneInPlayMode("Assets/TruckTerrierRacing/Scenes/TruckTerrierRacing.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckTerrierRacing.TerrierMatch, Assembly-CSharp",true),FindObjectsSortMode.None).Single();Call(match,"AllBots");match.GetType().GetField("option").SetValue(match,1);Assert.That((bool)Call(match,"StartMatch"),Is.True);Time.timeScale=0;
                var wins=(int[])Field(match,"wins");wins[9]=4;wins[10]=3;wins[11]=2;wins[8]=1;var players=(Array)Field(Field(match,"setup"),"players");players.GetValue(9).GetType().GetField("name").SetValue(players.GetValue(9),"SAME NAME");players.GetValue(10).GetType().GetField("name").SetValue(players.GetValue(10),"SAME NAME");yield return new WaitForSecondsRealtime(.3f);
                Assert.That((int)match.GetType().GetProperty("ScoreCardCount").GetValue(match),Is.EqualTo(4));int[] expected={9,10,11,8};
                var holder=((GameObject)Field(Field(match,"menus"),"hud")).transform.Find("Broadcast participant cards");Assert.That(holder,Is.Not.Null);Assert.That(holder.gameObject.activeInHierarchy,Is.True);
                for(int card=0;card<4;card++){Assert.That((int)Call(match,"ScoreCardSource",card),Is.EqualTo(expected[card]));Assert.That(Text(holder.Find("Participant card "+card+"/Score badge")),Is.EqualTo(wins[expected[card]]+"/5"));}
                Assert.That(holder.Cast<Transform>().Count(t=>t.gameObject.activeSelf),Is.EqualTo(4));Assert.That(((RectTransform)holder).rect.width,Is.LessThan(1200));Assert.That(((RectTransform)holder).rect.height,Is.LessThan(80));
                var first=(RectTransform)holder.GetChild(0);var second=(RectTransform)holder.GetChild(1);Assert.That(ScreenRect(first).Overlaps(ScreenRect(second)),Is.False);Assert.That(((Component)Field(Field(match,"menus"),"header")).transform.parent.gameObject.activeSelf,Is.False);
                var local=players.GetValue(2);var source=local.GetType().GetField("source");source.SetValue(local,Enum.Parse(source.FieldType,"Keyboard"));yield return null;
                Assert.That((int)Call(match,"ScoreCardSource",0),Is.EqualTo(2),"A local driver's own result stays visible ahead of distant AI leaders.");Assert.That((int)Call(match,"ScoreCardSource",1),Is.EqualTo(9));
            }
            finally{Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");}
        }
    }

    [UnityTest]
    public IEnumerator TerrierOneToFourHumanCardsStayInsideTheirOwnedViewsWithoutDuplicateScores()=>Cards("TruckTerrierRacing","LWS.TruckTerrierRacing.TerrierMatch","wins","0 / 5","3 / 5");
    [UnityTest]
    public IEnumerator BaseJumpingOneToFourHumanCardsStayInsideTheirOwnedViewsWithoutDuplicateScores()=>Cards("TruckBaseJumping","LWS.TruckBaseJumping.BaseJumpMatch","totals","0","3");
    [UnityTest]
    public IEnumerator MattressOneToFourHumanCardsStayInsideTheirOwnedViewsWithoutDuplicateScores()=>Cards("TruckMattressRacing","LWS.TruckMattressRacing.MattressMatch",null,"READY",null);
    [UnityTest]
    public IEnumerator TargetLaunchOneToFourHumanCardsReadTheirOwnAuthoritativeScores()=>Cards("TruckTargetLaunch","LWS.TruckTargetLaunch.TargetMatch","rules","0","10");
    [UnityTest]
    public IEnumerator WifeOneToFourHumanCardsReadTheirOwnPairPositionAndTime()=>Cards("TruckWifeCarrying","LWS.TruckWifeCarrying.WifeMatch","pairs","READY",null);
    static IEnumerator Cards(string scene,string typeName,string scoreField,string initialScore,string changedScore)
    {
        var prefs=Type.GetType("LWS.TruckSports.SportsPrefs, Assembly-CSharp",true);
        using((IDisposable)prefs.GetMethod("IsolateValidation").Invoke(null,new object[]{"CompactTerrierViews"}))
        {
            Component match=null;var pads=new Gamepad[4];GameObject isolation=null;var background=InputSystem.settings.backgroundBehavior;
            try
            {
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                isolation=new GameObject("Compact HUD physical-input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Type.GetType("LWS.TruckSports.SportsProbeInputIsolation, Assembly-CSharp",true));
                for(int i=0;i<4;i++){pads[i]=InputSystem.AddDevice<Gamepad>();InputSystem.EnableDevice(pads[i]);}
                EditorSceneManager.LoadSceneInPlayMode("Assets/"+scene+"/Scenes/"+scene+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                match=(Component)UnityEngine.Object.FindObjectsByType(Type.GetType(typeName+", Assembly-CSharp",true),FindObjectsSortMode.None).Single();
                foreach(var catalog in UnityEngine.Object.FindObjectsByType(Type.GetType("LWS.TruckSports.SportsWeatherPresets, Assembly-CSharp",true),FindObjectsSortMode.None))foreach(var entry in (IEnumerable)Field(catalog,"entries"))if((string)Field(entry,"displayName")=="Clear"){Call(catalog,"Select",Field(entry,"id"));break;}
                var cameraOptions=Type.GetType("LWS.TruckSports.SportsCameraOptions, Assembly-CSharp",true);
                for(int pass=0;pass<5;pass++)
                {
                    int humans=pass==4?1:pass+1;bool aiViews=pass==4;
                    Time.timeScale=1;Call(match,"SetupScreen");var setup=Field(match,"setup");setup.GetType().GetField("count").SetValue(setup,match.GetType().GetProperty("MaximumPlayers").GetValue(match));Call(match,"AllBots");match.GetType().GetField("option").SetValue(match,0);
                    // The last pass explicitly enables AI viewpoints; earlier
                    // human-only configurations must use all available space.
                    cameraOptions.GetMethod("Set").Invoke(null,new object[]{aiViews});
                    var players=(Array)Field(Field(match,"setup"),"players");for(int slot=0;slot<humans;slot++){var player=players.GetValue(slot);var source=player.GetType().GetField("source");source.SetValue(player,Enum.Parse(source.FieldType,"WholeController"));player.GetType().GetField("pad").SetValue(player,pads[slot]);}
                    Assert.That((bool)Call(match,"StartMatch"),Is.True);Time.timeScale=0;yield return new WaitForSecondsRealtime(.35f);Canvas.ForceUpdateCanvases();
                    var menus=Field(match,"menus");var panels=(RectTransform[])Field(menus,"viewPanels");var oldTexts=(Array)Field(menus,"viewTexts");var cameras=(Camera[])Field(match,"cameras");var owners=(int[])(scoreField==null||scoreField=="pairs"?Field(Field(match,"views"),"owners"):scoreField=="rules"?match.GetType().GetProperty("Owners").GetValue(match):Field(match,"viewSlots"));var visible=Enumerable.Range(0,cameras.Length).Where(i=>cameras[i]!=null&&cameras[i].enabled).ToArray();var rectangles=new Rect[cameras.Length];
                    Assert.That(visible.Length,Is.EqualTo(aiViews?4:humans));
                    foreach(int view in visible)
                    {
                        var card=panels[view].Find("Compact owned viewport score") as RectTransform;Assert.That(card,Is.Not.Null);Assert.That(card.gameObject.activeInHierarchy,Is.True);
                        Assert.That(((Component)oldTexts.GetValue(view)).gameObject.activeSelf,Is.False,"Old multiline score must not remain visible behind the compact group.");
                        Assert.That(panels[view].Find("View data backing").gameObject.activeSelf,Is.False);
                        Assert.That(Text(card.Find("Participant name")),Is.EqualTo((string)Call(match,"ViewportCardLabel",view,owners[view])));
                        Assert.That(Text(card.Find("Owned score")),Is.EqualTo(initialScore));
                        rectangles[view]=ScreenRect(card);var viewport=cameras[view].rect;var expected=new Rect(viewport.x*Screen.width,viewport.y*Screen.height,viewport.width*Screen.width,viewport.height*Screen.height);
                        Assert.That(rectangles[view].xMin,Is.GreaterThanOrEqualTo(expected.xMin-1));Assert.That(rectangles[view].xMax,Is.LessThanOrEqualTo(expected.xMax+1));Assert.That(rectangles[view].yMin,Is.GreaterThanOrEqualTo(expected.yMin-1));Assert.That(rectangles[view].yMax,Is.LessThanOrEqualTo(expected.yMax+1));
                        for(int other=0;other<view;other++)Assert.That(rectangles[view].Overlaps(rectangles[other]),Is.False,"Inner-corner groups must not overlap neighboring players.");
                    }
                    if(scoreField!=null&&scoreField!="pairs"){((int[])Field(scoreField=="rules"?Field(match,"rules"):match,scoreField=="rules"?"totals":scoreField))[owners[visible[0]]]=scoreField=="rules"?10:3;yield return new WaitForSecondsRealtime(.2f);Assert.That(Text(panels[visible[0]].Find("Compact owned viewport score/Owned score")),Is.EqualTo(changedScore));}
                    else if(scoreField==null){var racers=(Array)Field(match,"racers");var first=(Rigidbody)Field(Field(racers.GetValue(0),"truck"),"body");var second=(Rigidbody)Field(Field(racers.GetValue(1),"truck"),"body");second.position=first.position+Vector3.forward*15;Assert.That((int)Call(match,"RunningPlace",1),Is.LessThan((int)Call(match,"RunningPlace",0)),"Running position reads physical race progress, not series points or duplicate display names.");}
                    TestContext.WriteLine("Actual rendered "+scene+" compact views humans="+humans+" AI="+aiViews+" owners="+string.Join(",",visible.Select(i=>owners[i]))+" screen="+Screen.width+"x"+Screen.height);
                }
            }
            finally{Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");foreach(var pad in pads)if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);if(isolation!=null)UnityEngine.Object.Destroy(isolation);InputSystem.settings.backgroundBehavior=background;}
        }
    }
}
#endif
