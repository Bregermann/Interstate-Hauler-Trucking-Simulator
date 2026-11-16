#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class DragonMayhemPresentationNativeTests
{
    Component presentation,match;Keyboard keyboard;Gamepad pad;GameObject isolation;IDisposable lease;
    InputSettings.BackgroundBehavior oldBackground;InputSettings.EditorInputBehaviorInPlayMode oldEditorInput;
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static object Field(object source,string name)=>source.GetType().GetField(name).GetValue(source);
    static object Property(object source,string name)=>source.GetType().GetProperty(name).GetValue(source);
    static object Call(object source,string name,params object[] args)=>source.GetType().GetMethod(name).Invoke(source,args);
    static GameObject ObjectOf(object component)=>component is GameObject instance?instance:((Component)component).gameObject;

    [UnityTest,Timeout(120000)]
    public IEnumerator FourSavedVariantsOwnModalFocusBlockSetupCancelAndStayWithinCanvas()
    {
        oldBackground=InputSystem.settings.backgroundBehavior;oldEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        lease=(IDisposable)Runtime("LWS.TruckSports.SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"MayhemUI"+Guid.NewGuid().ToString("N")});
        Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
        isolation=new GameObject("Mayhem native UI device isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("LWS.TruckSports.SportsProbeInputIsolation"));
        keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.EnableDevice(keyboard);keyboard.MakeCurrent();pad=InputSystem.AddDevice<Gamepad>();InputSystem.EnableDevice(pad);pad.MakeCurrent();
        foreach(string sport in new[]{"TruckKeepaway","TruckBalloonBattle","TruckCornhole","TruckCheeseRolling"})
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/"+sport+"/Scenes/"+sport+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            presentation=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("LWS.TruckDragonKingdom.Mayhem.DragonMayhemPresentation"));Assert.That(presentation,Is.Not.Null);
            var director=Field(presentation,"director");Assert.That((bool)Property(director,"EnabledForSport"),Is.False);
            Assert.That(Field(presentation,"helpWindow"),Is.Not.Null,"ROOT must explicitly UpgradePresentation and save this original scene first.");
            var menu=(Behaviour)Field(presentation,"menu");var setup=(CanvasGroup)Field(presentation,"setupInteraction");var help=ObjectOf(Field(presentation,"helpButton"));
            var adapter=Field(director,"adapter");match=new[]{"keepaway","balloons","cornhole","cheese"}.Select(n=>Field(adapter,n)).OfType<Component>().Single();
            Assert.That(menu.enabled&&setup.interactable,Is.True);
            var canvas=ObjectOf(Field(presentation,"overlay")).GetComponentInParent<Canvas>();Canvas.ForceUpdateCanvases();var canvasRect=(RectTransform)canvas.transform;var corners=new Vector3[4];
            foreach(var rect in ObjectOf(Field(presentation,"overlay")).GetComponentsInChildren<RectTransform>(true))
            {
                rect.GetWorldCorners(corners);foreach(var corner in corners){var point=canvasRect.InverseTransformPoint(corner);Assert.That(point.x,Is.InRange(canvasRect.rect.xMin-.5f,canvasRect.rect.xMax+.5f),sport+" "+rect.name);Assert.That(point.y,Is.InRange(canvasRect.rect.yMin-.5f,canvasRect.rect.yMax+.5f),sport+" "+rect.name);}
            }
            // Real keyboard submit through the original InputSystem UI module.
            EventSystem.current.SetSelectedGameObject(help);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));yield return null;yield return null;
            Assert.That((bool)Property(presentation,"IsHelpOpen"),Is.True,"Native keyboard submit must open the actual Heat modal.");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return ReadyFrames();
            Assert.That(menu.enabled,Is.False);Assert.That(setup.interactable||setup.blocksRaycasts,Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject,Is.EqualTo(ObjectOf(Field(presentation,"closeHelp"))));
            var pages=(GameObject[])Field(menu,"pages");var actionType=Runtime("LWS.TruckSports.SportsMenuAction");
            var start=pages[0].GetComponentsInChildren(actionType,true).Cast<Component>().First(a=>(string)Field(a,"action")=="start");
            ExecuteEvents.Execute(start.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
            Assert.That((bool)Property(adapter,"InMatch"),Is.False,"Even an incorrectly targeted behind-modal submit cannot start the original sport.");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;yield return null;
            Assert.That((bool)Property(presentation,"IsHelpOpen"),Is.True,"Closing input stays leased until release; it cannot leak into setup.");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return WaitClosed();
            Assert.That(menu.enabled&&setup.interactable&&setup.blocksRaycasts,Is.True);Assert.That(EventSystem.current.currentSelectedGameObject,Is.EqualTo(help));
            // Real gamepad submit/cancel, followed by a fresh confirm-close.
            yield return OpenWithPad(help);InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.East));yield return null;yield return null;
            InputSystem.QueueStateEvent(pad,new GamepadState());yield return WaitClosed();Assert.That(EventSystem.current.currentSelectedGameObject,Is.EqualTo(help));
            yield return OpenWithPad(help);InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));yield return null;yield return null;
            InputSystem.QueueStateEvent(pad,new GamepadState());yield return WaitClosed();
            // Programmatic legitimate match start must abort the leased modal
            // and retain the game's new play-page focus, never restore setup.
            yield return OpenWithPad(help);
            var all=match.GetType().GetMethod("AllBots")??match.GetType().GetMethod("AllAI");all.Invoke(match,null);Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return null;
            Assert.That((bool)Property(presentation,"IsHelpOpen"),Is.False);Assert.That(((GameObject)Field(presentation,"helpPage")).activeSelf,Is.False);Assert.That(menu.enabled,Is.True);Assert.That(EventSystem.current.currentSelectedGameObject,Is.Not.EqualTo(help));
            var stop=match.GetType().GetMethod("SetupScreen")??match.GetType().GetMethod("ChangeSetup");stop.Invoke(match,null);yield return null;
            Assert.That((int)Property(director,"RuntimeActorCount"),Is.Zero,"OFF modal review cannot spawn gameplay actors.");
        }
    }
    IEnumerator OpenWithPad(GameObject help)
    {
        EventSystem.current.SetSelectedGameObject(help);InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));yield return null;yield return null;
        Assert.That((bool)Property(presentation,"IsHelpOpen"),Is.True);InputSystem.QueueStateEvent(pad,new GamepadState());yield return ReadyFrames();
    }
    static IEnumerator ReadyFrames(){for(int i=0;i<8;i++)yield return null;}
    IEnumerator WaitClosed()
    {
        float end=Time.realtimeSinceStartup+2;while((bool)Property(presentation,"IsHelpOpen")&&Time.realtimeSinceStartup<end)yield return null;
        Assert.That((bool)Property(presentation,"IsHelpOpen"),Is.False,"Released cancel/confirm must finish the Heat close lifecycle.");
    }
    [UnityTearDown]public IEnumerator Cleanup()
    {
        Time.timeScale=1;if(match!=null){var stop=match.GetType().GetMethod("SetupScreen")??match.GetType().GetMethod("ChangeSetup");stop?.Invoke(match,null);}
        if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);
        if(isolation!=null)UnityEngine.Object.Destroy(isolation);yield return null;
        lease?.Dispose();lease=null;InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditorInput;
        Runtime("LWS.TruckSports.SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
        var current=SceneManager.GetActiveScene();var empty=SceneManager.CreateScene("Mayhem UI native cleanup");SceneManager.SetActiveScene(empty);if(current.IsValid()&&current.isLoaded)yield return SceneManager.UnloadSceneAsync(current);
    }
}
#endif
