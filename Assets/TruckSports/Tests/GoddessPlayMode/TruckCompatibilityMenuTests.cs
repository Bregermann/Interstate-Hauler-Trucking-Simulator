#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Controlled saved-scene menu/input tests. They do not claim physical-device,
// full-match, subjective artwork or human compatibility acceptance.
public sealed class TruckCompatibilityMenuTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
    static object Registry(string name,params object[] args)=>Sports("SportsParticipantRegistry").GetMethod(name).Invoke(null,args);
    static string Stored()=>(string)Sports("SportsPrefs").GetMethod("GetString").Invoke(null,new object[]{"TruckSports.Participants.v1",""});
    static void Click(object button)=>((UnityEvent)Get(button,"onClick")).Invoke();
    IDisposable prefs;GameObject isolation;Keyboard keyboard;Gamepad pad;Component source,presenter;Scene scene;InputSettings originalSettings,testSettings;Action afterInputUpdate;int escapeDynamicFrame=-1;
    [TearDown]public void Cleanup()
    {
        if(presenter!=null)((Behaviour)presenter).enabled=false;if(afterInputUpdate!=null)InputSystem.onAfterUpdate-=afterInputUpdate;afterInputUpdate=null;if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);if(isolation!=null)UnityEngine.Object.DestroyImmediate(isolation);Time.timeScale=1;if(originalSettings!=null)InputSystem.settings=originalSettings;if(testSettings!=null)UnityEngine.Object.DestroyImmediate(testSettings);originalSettings=testSettings=null;Registry("ForgetCache");prefs?.Dispose();prefs=null;
    }
    IEnumerator Load(string mode)
    {
        prefs=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"TruckFit"+Guid.NewGuid().ToString("N")});Registry("ForgetCache");isolation=new GameObject("Explicit synthetic truck fit input");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Sports("SportsProbeInputIsolation"));originalSettings=InputSystem.settings;testSettings=UnityEngine.Object.Instantiate(originalSettings);testSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;testSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;testSettings.updateMode=InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;InputSystem.settings=testSettings;keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.EnableDevice(keyboard);keyboard.MakeCurrent();escapeDynamicFrame=-1;
        afterInputUpdate=()=>{if(InputState.currentUpdateType==InputUpdateType.Dynamic&&keyboard!=null&&keyboard.added&&keyboard.escapeKey.wasPressedThisFrame){escapeDynamicFrame=Time.frameCount;TestContext.WriteLine("FIT_ESCAPE_DYNAMIC frame="+Time.frameCount+" current="+(Keyboard.current==keyboard)+" enabled="+keyboard.enabled+" held="+keyboard.escapeKey.isPressed);}};InputSystem.onAfterUpdate+=afterInputUpdate;
        scene=EditorSceneManager.LoadSceneInPlayMode("Assets/"+mode+"/Scenes/"+mode+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
        source=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren(Sports("SportsTruckCompatibilitySource"),true)).Cast<Component>().SingleOrDefault();Assert.That(source,Is.Not.Null,"Run explicit compatibility registered-scene authoring first.");presenter=source.GetComponent(Sports("SportsTruckCompatibilityPresenter"));Assert.That(presenter,Is.Not.Null);
        if(Get(source,"billiards") is object billiards){Call(Get(source,"menu"),"Action","play");Call(billiards,"SelectMode",Enum.Parse(Get(billiards,"mode").GetType(),"AIVsAI"));Assert.That(Get(billiards,"Screen").ToString(),Is.EqualTo("Setup"),"Use the original Play action; selecting controls alone does not open Billiards Setup.");}
        yield return null;Assert.That(Get(source,"IsSetup"),Is.EqualTo(true));
    }
    static string Page(object source)
    {
        var menu=Get(source,"menu");return Get(source,"billiards") is object billiards?Get(billiards,"Screen").ToString():Get(menu,"Screen").ToString();
    }
    IEnumerator Open()
    {
        Call(presenter,"Open");yield return HeatReady();Assert.That(Get(presenter,"IsOpen"),Is.EqualTo(true));Assert.That(Get(Get(presenter,"Modal"),"IsOpen"),Is.EqualTo(true));Assert.That(((Behaviour)Get(source,"menu")).enabled,Is.False);
    }
    IEnumerator HeatReady()
    {
        var modal=(Component)Get(presenter,"Modal");Assert.That(Get(modal,"IsOpen"),Is.EqualTo(true));var window=(Component)Get(modal,"window");var animator=window.GetComponent<Animator>();var clips=animator.runtimeAnimatorController.animationClips;float duration=clips.Where(clip=>clip.name=="ModalWindow_In").Select(clip=>clip.length).DefaultIfEmpty(0).Max();Assert.That(duration,Is.GreaterThan(0),"Use the actual installed Heat In clip.");float deadline=Time.realtimeSinceStartup+duration+3;yield return null;
        while(Time.realtimeSinceStartup<deadline&&(Time.frameCount<=(int)Get(modal,"openedFrame")||animator.enabled&&animator.GetCurrentAnimatorStateInfo(0).normalizedTime<1))yield return null;
        var group=window.GetComponent<CanvasGroup>();Assert.That(Time.frameCount,Is.GreaterThan((int)Get(modal,"openedFrame")));Assert.That(!animator.enabled||animator.GetCurrentAnimatorStateInfo(0).normalizedTime>=1,Is.True,"The authored Heat opening must finish before a real cancel edge.");Assert.That(group,Is.Not.Null);Assert.That(group.alpha,Is.GreaterThan(.99f));TestContext.WriteLine("FIT_HEAT_READY frame="+Time.frameCount+" gate="+Get(modal,"openedFrame")+" authoredIn="+duration+" alpha="+group.alpha);
    }
    void Key(bool held)
    {
        Assert.That(keyboard.added,Is.True);if(!keyboard.enabled)InputSystem.EnableDevice(keyboard);keyboard.MakeCurrent();if(held)escapeDynamicFrame=-1;InputSystem.QueueStateEvent(keyboard,held?new KeyboardState(UnityEngine.InputSystem.Key.Escape):new KeyboardState());TestContext.WriteLine("FIT_ESCAPE_QUEUE held="+held+" frame="+Time.frameCount+" gate="+Get(Get(presenter,"Modal"),"openedFrame")+" current="+(Keyboard.current==keyboard)+" enabled="+keyboard.enabled+" native="+keyboard.native+" focus="+Application.isFocused+" editorRoute="+InputSystem.settings.editorInputBehaviorInPlayMode+" updateMode="+InputSystem.settings.updateMode);
    }
    IEnumerator CloseWithOneEscape(object expectedOpen=null)
    {
        yield return HeatReady();string before=Page(source);int gate=(int)Get(Get(presenter,"Modal"),"openedFrame");Key(true);yield return null;yield return null;Assert.That(escapeDynamicFrame,Is.GreaterThan(gate),"The synthetic Escape must reach an actual Dynamic update after the open-frame gate.");Assert.That(Keyboard.current,Is.SameAs(keyboard));Assert.That(keyboard.escapeKey.isPressed,Is.True);Assert.That(Page(source),Is.EqualTo(before),"The actual Match/router Update must not invoke underlying Back while menu-leased.");Assert.That(scene.isLoaded,Is.True);Assert.That(Get(Get(presenter,"Modal"),"IsOpen"),Is.EqualTo(false));Assert.That(((Behaviour)Get(source,"menu")).enabled,Is.False,"Hold the menu lease while Escape remains held.");
        Key(false);yield return new WaitForSecondsRealtime(.65f);yield return null;Assert.That(Page(source),Is.EqualTo(before));if(expectedOpen==null){Assert.That(Get(presenter,"IsOpen"),Is.EqualTo(false));Assert.That(((Behaviour)Get(source,"menu")).enabled,Is.True);}else{Assert.That(Get(expectedOpen,"IsOpen"),Is.EqualTo(true));Assert.That(Get(expectedOpen,"SportReportOpen"),Is.EqualTo(false));Assert.That(((Behaviour)Get(source,"menu")).enabled,Is.False);}
    }
    IEnumerator LegacyBack(string mode)
    {
        yield return Load(mode);var menu=(Behaviour)Get(source,"menu");string before=Page(source),bytes=Stored();var focus=EventSystem.current.currentSelectedGameObject;var group=(CanvasGroup)Get(source,"setupInteraction");bool interactable=group.interactable,raycasts=group.blocksRaycasts;
        if(mode=="TruckHockey"){yield return null;yield return null;CaptureSetup("hockey-title");}
        yield return Open();Assert.That(group.interactable,Is.False);Assert.That(group.blocksRaycasts,Is.False);Call(menu,"Back");Assert.That(Page(source),Is.EqualTo(before),"Disabled menu rejects Match/router direct Back calls.");
        foreach(string field in new[]{"session","bowling","cornhole","cheese","hockey","airHockey","wallball","billiards"})if(Get(source,field) is Behaviour authority)Assert.That(authority.enabled,Is.True,"Never disable the gameplay or input owner.");
        yield return CloseWithOneEscape();Assert.That(group.interactable,Is.EqualTo(interactable));Assert.That(group.blocksRaycasts,Is.EqualTo(raycasts));Assert.That(EventSystem.current.currentSelectedGameObject,Is.SameAs(focus));Assert.That(Stored(),Is.EqualTo(bytes));
    }
    [UnityTest,Timeout(90000)]public IEnumerator HockeyMatchEscapeClosesOnlyHeatModal()=>LegacyBack("TruckHockey");
    [UnityTest,Timeout(90000)]public IEnumerator AirHockeyMatchEscapeClosesOnlyHeatModal()=>LegacyBack("TruckAirHockey");
    [UnityTest,Timeout(90000)]public IEnumerator WallballMatchEscapeClosesOnlyHeatModal()=>LegacyBack("TruckWallball");
    [UnityTest,Timeout(90000)]public IEnumerator BilliardsRouterEscapeClosesOnlyHeatModal()=>LegacyBack("TruckBilliards");
    [UnityTest,Timeout(120000)]public IEnumerator HockeyFixedSixSetupReportsUseActualStaticHostArray()=>FixedStatic("TruckHockey");
    [UnityTest,Timeout(120000)]public IEnumerator AirHockeyFixedTwoSetupReportsUseActualStaticHostArray()=>FixedStatic("TruckAirHockey");
    [UnityTest,Timeout(120000)]public IEnumerator BilliardsHotSeatKeepsFixedSideHostsAndPracticeUsesOnlySideZero()=>FixedStatic("TruckBilliards");
    [UnityTest,Timeout(120000)]public IEnumerator PokerSetupReportsUseActualEightSeatedHostArray()=>FixedStatic("TruckPoker");
    IEnumerator FixedStatic(string mode)
    {
        yield return Load(mode);var owner=mode=="TruckHockey"?Get(source,"hockey"):mode=="TruckAirHockey"?Get(source,"airHockey"):mode=="TruckBilliards"?Get(source,"billiards"):Get(source,"session");if(mode=="TruckPoker"){Call(owner,"AllBots");Set(Get(owner,"setup"),"count",8);Call(owner,"NormalizeSetup");}if(mode=="TruckBilliards")Call(owner,"SelectMode",Enum.Parse(Get(owner,"mode").GetType(),"HotSeat"));
        var hosts=((Array)Get(owner,"appearanceHosts")).Cast<object>().ToArray();int expected=mode=="TruckHockey"?6:mode=="TruckPoker"?8:2;Assert.That(hosts.Length,Is.EqualTo(expected));Assert.That(Get(source,"sharedTurnHost"),Is.Null,"Static HotSeat keeps each participant's physical side/avatar, not one borrowed truck.");var revisions=hosts.Select(host=>Get(host,"AppearanceRevision")).ToArray();var models=hosts.Select(host=>Get(host,"AppearanceRoot")).ToArray();var profiles=hosts.Select(host=>Get(host,"ProfileId")).ToArray();string bytes=Stored();
        for(int i=0;i<expected;i++){Assert.That(Call(source,"Host",i),Is.SameAs(hosts[i]));Assert.That(Get(hosts[i],"participantSlot"),Is.EqualTo(i));Assert.That(((Array)Get(hosts[i],"wheels")).Length,Is.Zero);Assert.That(((Array)Get(hosts[i],"staticWheelPoses")).Length,Is.EqualTo(4));var view=Call(source,"Read",i);Assert.That(Get(view,"hasHost"),Is.EqualTo(true));Assert.That(Get(view,"canApply"),Is.EqualTo(true));var player=((Array)Get(source,"Participants")).GetValue(i);Assert.That(Get(view,"profileId"),Is.EqualTo(Get(player,"participantId")));}
        yield return Open();yield return CloseWithOneEscape();
        if(mode=="TruckBilliards"){Call(owner,"SelectMode",Enum.Parse(Get(owner,"mode").GetType(),"Practice"));Assert.That(Get(source,"Count"),Is.EqualTo(1));Assert.That(Call(source,"Host",0),Is.SameAs(hosts[0]));Assert.That(Call(source,"Host",1),Is.Null,"Hidden practice opponent is not applicable.");yield return Open();Assert.That(((GameObject)Get(((Array)Get(Get(presenter,"Modal"),"rows")).GetValue(1),"root")).activeSelf,Is.False);yield return CloseWithOneEscape();}
        CollectionAssert.AreEqual(revisions,hosts.Select(host=>Get(host,"AppearanceRevision")));CollectionAssert.AreEqual(models,hosts.Select(host=>Get(host,"AppearanceRoot")));CollectionAssert.AreEqual(profiles,hosts.Select(host=>Get(host,"ProfileId")));Assert.That(Stored(),Is.EqualTo(bytes));Assert.That(((Behaviour)owner).enabled,Is.True);
    }

    [UnityTest,Timeout(120000)]public IEnumerator SessionGamepadBackAndSetupDisconnectKeepTheUnderlyingPageAndIdentity()
    {
        yield return Load("TruckSumo");var match=Get(source,"session");var setup=Get(match,"setup");var player=((Array)Get(setup,"players")).GetValue(0);pad=InputSystem.AddDevice<Gamepad>();Set(player,"source",Enum.Parse(Get(player,"source").GetType(),"WholeController"));Set(player,"pad",pad);string id=(string)Get(player,"participantId"),bytes=Stored();yield return Open();yield return null;
        InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.East));yield return null;yield return null;Assert.That(Get(Get(presenter,"Modal"),"IsOpen"),Is.EqualTo(false));Assert.That(((Behaviour)Get(source,"menu")).enabled,Is.False);Assert.That(Page(source),Is.EqualTo("Setup"));
        InputSystem.QueueStateEvent(pad,new GamepadState());yield return new WaitForSecondsRealtime(.65f);yield return null;Assert.That(((Behaviour)Get(source,"menu")).enabled,Is.True);Assert.That(Get(player,"participantId"),Is.EqualTo(id));
        yield return Open();InputSystem.RemoveDevice(pad);yield return null;yield return null;Assert.That(Page(source),Is.EqualTo("Setup"));Assert.That(Get(player,"participantId"),Is.EqualTo(id));Assert.That(((Behaviour)match).enabled,Is.True);yield return CloseWithOneEscape();Assert.That(Stored(),Is.EqualTo(bytes));
    }

    [UnityTest,Timeout(120000)]public IEnumerator EightDriversPageByStableIdAndAllReadoutsLeaveProfilesAndModelsUntouched()
    {
        yield return Load("TruckWifeCarrying");var match=Get(source,"session");Assert.That((int)Get(match,"MaximumPlayers"),Is.GreaterThanOrEqualTo(8));Call(match,"AllBots");Set(Get(match,"setup"),"count",8);Call(match,"NormalizeSetup");Assert.That(Get(source,"Count"),Is.EqualTo(8),"This saved mode genuinely supports eight fixed participant owners.");var ids=new string[8];
        for(int i=0;i<8;i++){ids[i]=(string)Get(Registry("Create","Same Driver"),"id");Registry("UseInSeat",i,ids[i]);}Call(match,"RefreshParticipants",false);
        string bytes=Stored();var hosts=((Array)Get(source,"participantHosts")).Cast<object>().Where(host=>host!=null).ToArray();var revisions=hosts.Select(host=>(int)Get(host,"AppearanceRevision")).ToArray();var models=hosts.Select(host=>Get(host,"AppearanceRoot")).ToArray();
        for(int repeat=0;repeat<20;repeat++)for(int i=0;i<8;i++){var report=Call(source,"Read",i);Assert.That(Get(report,"profileId"),Is.EqualTo(ids[i]));Assert.That(Get(report,"name"),Is.EqualTo("Same Driver"));}
        CollectionAssert.AreEqual(revisions,hosts.Select(host=>(int)Get(host,"AppearanceRevision")));CollectionAssert.AreEqual(models,hosts.Select(host=>Get(host,"AppearanceRoot")));Assert.That(Stored(),Is.EqualTo(bytes));
        yield return null;yield return null;CaptureSetup("wife-eight");yield return Open();var modal=Get(presenter,"Modal");for(int page=0;page<2;page++){var rows=(Array)Get(modal,"rows");for(int i=0;i<4;i++){string name=(string)Get(Get(rows.GetValue(i),"name"),"text");StringAssert.Contains(ids[page*4+i].Substring(0,8),name);StringAssert.Contains("REQUESTED:",(string)Get(Get(rows.GetValue(i),"fit"),"text"));StringAssert.Contains("HERE:",(string)Get(Get(rows.GetValue(i),"fit"),"text"));}Capture(modal,"eight-drivers-page-"+page);if(page==0)Click(Get(modal,"next"));}
        // A removed synthetic setup device cannot turn this pure report into a
        // join/disconnect page transition or rewrite participant identity.
        pad=InputSystem.AddDevice<Gamepad>();InputSystem.RemoveDevice(pad);yield return null;Assert.That(Page(source),Is.EqualTo("Setup"));yield return CloseWithOneEscape();Assert.That(Stored(),Is.EqualTo(bytes));
    }
    void CaptureSetup(string name)
    {
        Assert.That(Get(source,"IsSetup"),Is.EqualTo(true));Assert.That(Get(presenter,"IsOpen"),Is.EqualTo(false));var page=(GameObject)Get(source,"setupPage");var canvas=page.GetComponentInParent<Canvas>();Assert.That(canvas,Is.Not.Null);var cameras=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Camera>(true)).Where(camera=>camera.enabled&&camera.gameObject.activeInHierarchy&&camera.targetTexture==null).ToArray();Assert.That(cameras,Is.Not.Empty);var summary=(Component)Get(presenter,"subtitle");var button=(Component)Get(presenter,"reportButton");var title=page.transform.Find("Title") as RectTransform;Assert.That(title,Is.Not.Null);var logo=page.transform.Find("Original sport title artwork") as RectTransform;var summaryAt=((RectTransform)summary.transform).anchoredPosition;var buttonAt=((RectTransform)button.transform).anchoredPosition;var titleAt=title.anchoredPosition;var titleSize=title.sizeDelta;var backing=page.transform.Find("Truck fit summary readability backing") as RectTransform;Assert.That(backing,Is.Not.Null,"Run the explicit owned summary backing upgrade.");var backingImage=backing.GetComponent(Type.GetType("UnityEngine.UI.Image, UnityEngine.UI",true));Assert.That(Get(backingImage,"sprite"),Is.Not.Null);Assert.That(Get(backingImage,"type").ToString(),Is.EqualTo("Sliced"));Assert.That(Get(backingImage,"raycastTarget"),Is.EqualTo(false));var actualInk=(Color)Get(backingImage,"color");var sharedInk=(Color)Sports("SportsBroadcastCanvas").GetField("Ink").GetValue(null);Assert.That(actualInk,Is.EqualTo(sharedInk),"Use the actual shared palette exactly.");Assert.That(actualInk.a,Is.GreaterThanOrEqualTo(.9f));Assert.That(backing.sizeDelta,Is.EqualTo(new Vector2(1064,50)));Assert.That(backing.GetSiblingIndex(),Is.EqualTo(summary.transform.GetSiblingIndex()-1));var save=Sports("SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==6);
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})
        {
            Action<Camera> inspect=camera=>
            {
                Canvas.ForceUpdateCanvases();Rect Bounds(RectTransform rect){var corners=new Vector3[4];rect.GetWorldCorners(corners);var pixels=corners.Select(corner=>RectTransformUtility.WorldToScreenPoint(camera,corner)).ToArray();foreach(var pixel in pixels){Assert.That(pixel.x,Is.InRange(-2,size.x+2),rect.name);Assert.That(pixel.y,Is.InRange(-2,size.y+2),rect.name);}return Rect.MinMaxRect(pixels.Min(pixel=>pixel.x),pixels.Min(pixel=>pixel.y),pixels.Max(pixel=>pixel.x),pixels.Max(pixel=>pixel.y));}
                var summaryBounds=Bounds((RectTransform)summary.transform);var buttonBounds=Bounds((RectTransform)button.transform);var backingBounds=Bounds(backing);Assert.That(backingBounds.Contains(summaryBounds.min)&&backingBounds.Contains(summaryBounds.max),Is.True);Assert.That(backingBounds.Overlaps(buttonBounds),Is.False);Assert.That(summaryBounds.Overlaps(buttonBounds),Is.False);foreach(var original in new[]{title,logo}.Where(rect=>rect!=null)){var bounds=Bounds(original);Assert.That(bounds.Overlaps(summaryBounds),Is.False,original.name+" versus fit summary");Assert.That(bounds.Overlaps(backingBounds),Is.False,original.name+" versus owned summary backing");Assert.That(bounds.Overlaps(buttonBounds),Is.False,original.name+" versus TRUCK FIT button");}
                foreach(var text in new[]{summary,Get(button,"normalTextObj") as Component,title.GetComponent(Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro",true))}.Where(text=>text!=null)){Call(text,"ForceMeshUpdate",true,false);Assert.That(Get(text,"isTextOverflowing"),Is.EqualTo(false),text.name+": "+Get(text,"text"));}TestContext.WriteLine("FIT_SETUP_CAPTURE "+name+" "+size+" summary="+summaryBounds+" button="+buttonBounds+" title="+Bounds(title));
            };
            save.Invoke(null,new object[]{canvas,cameras,size.x,size.y,"Logs/TruckSports/PlaytestRepair/TruckCompatibility/setup-"+name+"-"+size.y+".png",inspect});
        }
        Assert.That(((RectTransform)summary.transform).anchoredPosition,Is.EqualTo(summaryAt));Assert.That(((RectTransform)button.transform).anchoredPosition,Is.EqualTo(buttonAt));Assert.That(title.anchoredPosition,Is.EqualTo(titleAt));Assert.That(title.sizeDelta,Is.EqualTo(titleSize));
    }
    static void Capture(object modal,string name)
    {
        var component=(Component)modal;var canvas=component.GetComponent<Canvas>();var cameras=component.gameObject.scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Camera>(true)).Where(camera=>camera.enabled&&camera.gameObject.activeInHierarchy&&camera.targetTexture==null).ToArray();
        if(cameras.Length==0)cameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude).Where(camera=>camera.enabled&&camera.targetTexture==null).ToArray();Assert.That(cameras,Is.Not.Empty);
        var save=Sports("SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==6);
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})
        {
            Action<Camera> inspect=camera=>{Canvas.ForceUpdateCanvases();foreach(var text in component.GetComponentsInChildren(Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro",true),false).Cast<Component>()){Call(text,"ForceMeshUpdate",true,false);Assert.That(Get(text,"isTextOverflowing"),Is.EqualTo(false),text.name+": "+Get(text,"text"));var corners=new Vector3[4];((RectTransform)text.transform).GetWorldCorners(corners);foreach(var corner in corners){var pixel=RectTransformUtility.WorldToScreenPoint(camera,corner);Assert.That(pixel.x,Is.InRange(-2,size.x+2));Assert.That(pixel.y,Is.InRange(-2,size.y+2));}}};
            save.Invoke(null,new object[]{canvas,cameras,size.x,size.y,"Logs/TruckSports/PlaytestRepair/TruckCompatibility/"+name+"-"+size.y+".png",inspect});
        }
    }
    [UnityTest,Timeout(120000)]public IEnumerator UnmappedSportDraftReportsOriginalAndNestedBackNeverCancelsShopOrSetup()
    {
        yield return Load("TruckDemolitionDerby");Assert.That(Get(source,"sharedTurnHost"),Is.Null);Assert.That(Call(source,"Host",0),Is.Null,"This fixture requires the explicitly original-appearance Derby venue, not a newly bound sport.");var participants=(Array)Get(source,"Participants");string id=(string)participants.GetValue(0).GetType().GetField("participantId").GetValue(participants.GetValue(0));string bytes=Stored();yield return null;yield return null;CaptureSetup("derby");yield return Open();var modal=Get(presenter,"Modal");var row=((Array)Get(modal,"rows")).GetValue(0);Click(Get(row,"customize"));yield return new WaitForSecondsRealtime(.65f);yield return null;
        var shop=Get(presenter,"shop");Assert.That(Get(shop,"IsOpen"),Is.EqualTo(true));Assert.That(Get(source,"IsSetup"),Is.EqualTo(true));Assert.That(Get(shop,"profileId"),Is.EqualTo(id));Assert.That(Get(shop,"preview"),Is.Null,"An unbound sport must not preview arbitrary supported body models.");StringAssert.Contains("ORIGINAL SPORT APPEARANCE",(string)Get(Get(shop,"previewCaption"),"text"));
        var cards=((Array)Get(shop,"cards")).Cast<object>().ToArray();var monster=cards.Single(card=>(string)Get(card,"buttonTitle")=="Monster Hauler");Click(monster);Assert.That(Get(Get(shop,"draft"),"bodyId"),Is.EqualTo("truck.monster"));Assert.That(Stored(),Is.EqualTo(bytes));
        var fit=Get(shop,"sportFit");EventSystem.current.SetSelectedGameObject(((Component)fit).gameObject);Click(fit);yield return HeatReady();Assert.That(Get(shop,"IsOpen"),Is.EqualTo(true));Assert.That(Get(shop,"SportReportOpen"),Is.EqualTo(true));var report=Get(shop,"SportCompatibility");StringAssert.Contains("truck.monster",(string)Get(report,"requested"));StringAssert.Contains("ORIGINAL SPORT TRUCK",(string)Get(report,"effective"));Capture(modal,"unmapped-draft-reasons");
        yield return CloseWithOneEscape(shop);Assert.That(EventSystem.current.currentSelectedGameObject,Is.SameAs(((Component)fit).gameObject));Assert.That(Stored(),Is.EqualTo(bytes));
        Key(true);yield return null;yield return null;Assert.That(Get(shop,"IsOpen"),Is.EqualTo(false));Assert.That(((Behaviour)Get(source,"menu")).enabled,Is.False);Assert.That(Page(source),Is.EqualTo("Setup"));Key(false);yield return null;yield return null;yield return null;Assert.That(((Behaviour)Get(source,"menu")).enabled,Is.True);Assert.That(Stored(),Is.EqualTo(bytes));
    }
    [UnityTest,Timeout(120000)]public IEnumerator TrailerSharedTurnReportUsesActualHostAndAbortRestoresExactMenuState()
    {
        yield return Load("TruckTrailerPlug");var match=Get(source,"session");Call(match,"AllBots");Set(Get(match,"setup"),"count",4);Call(match,"NormalizeSetup");var host=Get(Get(Get(match,"rig"),"truck"),"appearanceHost");Assert.That(Get(source,"sharedTurnHost"),Is.SameAs(host));Assert.That(host,Is.Not.Null);string bytes=Stored();int revision=(int)Get(host,"AppearanceRevision");var model=Get(host,"AppearanceRoot");for(int i=0;i<4;i++)Assert.That(Call(source,"Host",i),Is.SameAs(host));
        var menu=(Behaviour)Get(source,"menu");var group=(CanvasGroup)Get(source,"setupInteraction");group.interactable=false;group.blocksRaycasts=true;menu.enabled=false;
        yield return Open();((Behaviour)presenter).enabled=false;Assert.That(menu.enabled,Is.False,"Restore exact original disabled state.");Assert.That(group.interactable,Is.False);Assert.That(group.blocksRaycasts,Is.True);Assert.That(Get(Get(presenter,"Modal"),"IsOpen"),Is.EqualTo(false));Assert.That(Get(host,"AppearanceRevision"),Is.EqualTo(revision));Assert.That(Get(host,"AppearanceRoot"),Is.SameAs(model));Assert.That(Stored(),Is.EqualTo(bytes));
        menu.enabled=true;((Behaviour)presenter).enabled=true;Call(presenter,"Open");yield return new WaitForSecondsRealtime(.5f);Assert.That(Get(Get(presenter,"Modal"),"IsOpen"),Is.EqualTo(true),"Aborted reusable Heat modal can reopen.");yield return CloseWithOneEscape();
    }
    [UnityTest,Timeout(120000)]public IEnumerator SwingSetupEvaluatesEveryDriverAgainstTheActualSharedTurnHost()=>SharedLaunch("TruckGiantSwing");
    [UnityTest,Timeout(120000)]public IEnumerator FieldGoalSetupEvaluatesEveryDriverAgainstTheActualSharedTurnHost()=>SharedLaunch("TruckFieldGoal");
    [UnityTest,Timeout(120000)]public IEnumerator BeerPongSetupEvaluatesEveryDriverAgainstTheActualSharedTurnHost()=>SharedLaunch("TruckBeerPong");
    [UnityTest,Timeout(120000)]public IEnumerator CurlingSetupUsesOnlyAProvenEquivalentSixteenStoneTemplate()=>SharedLaunch("TruckCurling");
    [UnityTest,Timeout(180000)]public IEnumerator TugLocalEightSetupReportsStayPureAfterOriginalBurstZeroesTheVisuals()
    {
        yield return Load("TruckTugOfWar");var match=Get(source,"session");Set(match,"teamA",4);Set(match,"teamB",4);Call(match,"AllBots");Call(match,"NormalizeSetup");var hosts=((Array)Get(match,"tugAppearanceHosts")).Cast<object>().ToArray();Assert.That(hosts.Length,Is.EqualTo(8));Assert.That(((Array)Get(match,"appearanceHosts")).Length,Is.Zero);for(int i=0;i<8;i++)Assert.That(Call(source,"Host",i),Is.SameAs(hosts[i]));Set(match,"teamA",1);Set(match,"teamB",1);Call(match,"NormalizeSetup");Assert.That(Get(source,"Count"),Is.EqualTo(2));Assert.That(Call(source,"Host",2),Is.Null);Set(match,"teamA",4);Set(match,"teamB",4);Call(match,"NormalizeSetup");Assert.That(Call(match,"StartMatch"),Is.EqualTo(true));
        var rig=((Array)Get(match,"fragments")).GetValue(0);var truck=((Array)Get(match,"trucks")).GetValue(0);Call(rig,"Burst");Assert.That(((Transform)Get(truck,"visuals")).localScale,Is.EqualTo(Vector3.zero));Call(match,"SetupScreen");yield return null;Assert.That(Get(source,"IsSetup"),Is.EqualTo(true));Assert.That(((Transform)Get(truck,"visuals")).localScale,Is.EqualTo(Vector3.zero),"Original setup boundary does not pretend ResetBody/next heat has occurred.");
        string bytes=Stored();var revisions=hosts.Select(host=>Get(host,"AppearanceRevision")).ToArray();var models=hosts.Select(host=>Get(host,"AppearanceRoot")).ToArray();var profiles=hosts.Select(host=>Get(host,"ProfileId")).ToArray();var fragments=hosts.Select(host=>Get(host,"fragmentPresentation")).ToArray();var pools=fragments.Select(fragment=>Get(fragment,"PoolBuilds")).ToArray();var bodies=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Rigidbody>(true)).Select(body=>(body,body.position,body.rotation,body.linearVelocity,body.angularVelocity,body.isKinematic,body.detectCollisions)).ToArray();var catalog=Get(hosts[0],"catalog");var choices=((Array)Get(catalog,"bodies")).Cast<object>().ToArray();Assert.That(choices.Length,Is.EqualTo(7));var author=Type.GetType("LWS.TruckSports.Editor.SportsTruckCompatibilityAuthoring, Assembly-CSharp-Editor",true);var target=(Array)author.GetMethod("ProvenTugSetupMap").Invoke(null,new[]{match});
        for(int i=0;i<8;i++)foreach(var body in choices){var player=((Array)Get(source,"Participants")).GetValue(i);string id=(string)Get(player,"participantId");var draft=Registry("GetAppearance",id);Set(draft,"bodyId",Get(body,"id"));var view=Call(source,"Evaluate",i,id,draft);Assert.That(Get(view,"hasHost"),Is.EqualTo(true));Assert.That(Get(view,"canApply"),Is.EqualTo(true));Assert.That(Get(view,"bodyFallback"),Is.EqualTo(false));StringAssert.Contains((string)Get(body,"id"),(string)Get(view,"effective"));Assert.That(target.GetValue(i),Is.SameAs(hosts[i]));}
        Assert.That(((Transform)Get(truck,"visuals")).localScale,Is.EqualTo(Vector3.zero));CollectionAssert.AreEqual(revisions,hosts.Select(host=>Get(host,"AppearanceRevision")));CollectionAssert.AreEqual(models,hosts.Select(host=>Get(host,"AppearanceRoot")));CollectionAssert.AreEqual(profiles,hosts.Select(host=>Get(host,"ProfileId")));CollectionAssert.AreEqual(pools,fragments.Select(fragment=>Get(fragment,"PoolBuilds")));Assert.That(Stored(),Is.EqualTo(bytes));foreach(var entry in bodies){Assert.That(entry.body.position,Is.EqualTo(entry.position));Assert.That(entry.body.rotation,Is.EqualTo(entry.rotation));Assert.That(entry.body.linearVelocity,Is.EqualTo(entry.linearVelocity));Assert.That(entry.body.angularVelocity,Is.EqualTo(entry.angularVelocity));Assert.That(entry.body.isKinematic,Is.EqualTo(entry.isKinematic));Assert.That(entry.body.detectCollisions,Is.EqualTo(entry.detectCollisions));}
        yield return Open();yield return CloseWithOneEscape();Assert.That(((Transform)Get(truck,"visuals")).localScale,Is.EqualTo(Vector3.zero));CollectionAssert.AreEqual(revisions,hosts.Select(host=>Get(host,"AppearanceRevision")));CollectionAssert.AreEqual(pools,fragments.Select(fragment=>Get(fragment,"PoolBuilds")));Assert.That(Stored(),Is.EqualTo(bytes));
        TestContext.WriteLine("TUG_SETUP_FIT local8, actual4 wheel-piece sources/all7 recipes, original controlled Burst then Setup keeps zero visuals, pure report/author validation creates no models/pools/revisions/profile writes or physics changes. Actual next-heat reset remains sole gameplay authority.");
    }
    IEnumerator SharedLaunch(string mode)
    {
        yield return Load(mode);var match=Get(source,"session");Set(match,"option",0);Call(match,"AllBots");Set(Get(match,"setup"),"count",Mathf.Min(4,(int)Get(match,"MaximumPlayers")));if((bool)Get(match,"TeamSport")){Set(match,"teamA",2);Set(match,"teamB",2);}Call(match,"NormalizeSetup");var truck=(Component)(mode=="TruckCurling"?Get(((Array)Get(match,"stones")).GetValue(0),"truck"):Get(match,"truck"));var host=Get(truck,"appearanceHost");Assert.That(host,Is.Not.Null,"Run explicit launch/Curling host author before compatibility author.");Assert.That(Get(source,"sharedTurnHost"),Is.SameAs(host));Assert.That(Get(host,"participantSlot"),Is.EqualTo(-1),"Setup evaluation does not pretend a turn has started.");Assert.That(((Array)Get(match,"appearanceHosts")).Cast<object>(),Does.Contain(host));
        string bytes=Stored();int revision=(int)Get(host,"AppearanceRevision");var model=Get(host,"AppearanceRoot");string profile=(string)Get(host,"ProfileId");int count=(int)Get(source,"Count");Assert.That(count,Is.GreaterThanOrEqualTo(2));
        for(int i=0;i<count;i++){Assert.That(Call(source,"Host",i),Is.SameAs(host));var view=Call(source,"Read",i);Assert.That(Get(view,"hasHost"),Is.EqualTo(true));Assert.That(Get(view,"canApply"),Is.EqualTo(true));}
        if(mode=="TruckCurling")
        {
            var author=Type.GetType("LWS.TruckSports.Editor.SportsTruckCompatibilityAuthoring, Assembly-CSharp-Editor",true);var template=author.GetMethod("CurlingSetupTemplate");var hosts=((Array)Get(match,"stones")).Cast<object>().Select(stone=>Get(Get(stone,"truck"),"appearanceHost")).ToArray();Assert.That(hosts.Length,Is.EqualTo(16));var revisions=hosts.Select(item=>Get(item,"AppearanceRevision")).ToArray();var profiles=hosts.Select(item=>Get(item,"ProfileId")).ToArray();Assert.That(template.Invoke(null,new[]{match}),Is.SameAs(host));
            var fit=((Array)Get(hosts[15],"compatibleBodies")).GetValue(0);var position=(Vector3)Get(fit,"position");try{Set(fit,"position",position+Vector3.right*.01f);var error=Assert.Throws<TargetInvocationException>(()=>template.Invoke(null,new[]{match}));Assert.That(error.InnerException,Is.TypeOf<InvalidOperationException>(),"A genuinely different retained-stone fit cannot borrow the first actor's setup preview.");}finally{Set(fit,"position",position);}Assert.That(template.Invoke(null,new[]{match}),Is.SameAs(host));CollectionAssert.AreEqual(revisions,hosts.Select(item=>Get(item,"AppearanceRevision")));CollectionAssert.AreEqual(profiles,hosts.Select(item=>Get(item,"ProfileId")));
        }
        yield return Open();yield return CloseWithOneEscape();Assert.That(Get(host,"participantSlot"),Is.EqualTo(-1));Assert.That(Get(host,"AppearanceRevision"),Is.EqualTo(revision));Assert.That(Get(host,"AppearanceRoot"),Is.SameAs(model));Assert.That(Get(host,"ProfileId"),Is.EqualTo(profile));Assert.That(Stored(),Is.EqualTo(bytes));Assert.That(((Behaviour)match).enabled,Is.True);Assert.That(Get(match,"Running"),Is.EqualTo(false));
    }
}
#endif
