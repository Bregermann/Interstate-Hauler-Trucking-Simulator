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
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class ImpossibleVenueLogoLifecycleTests
{
    const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static Type Sports(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
    static Rect Project(Renderer renderer,Camera view,Vector2Int size)
    {
        var matrix=Matrix4x4.Perspective(view.fieldOfView,size.x*view.rect.width/(size.y*view.rect.height),view.nearClipPlane,view.farClipPlane)*view.worldToCameraMatrix;
        var bounds=renderer.bounds;var pixels=new List<Vector2>();
        for(int i=0;i<8;i++){var p=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));var clip=matrix*new Vector4(p.x,p.y,p.z,1);Assert.That(clip.w,Is.GreaterThan(0),renderer.name+" behind actual camera");pixels.Add(new Vector2((clip.x/clip.w*.5f+.5f)*size.x,(clip.y/clip.w*.5f+.5f)*size.y));}
        return Rect.MinMaxRect(pixels.Min(p=>p.x),pixels.Min(p=>p.y),pixels.Max(p=>p.x),pixels.Max(p=>p.y));
    }
    static Rect UiBounds(Component component,Camera ui)
    {
        var rect=(RectTransform)component.transform;var corners=new Vector3[4];rect.GetWorldCorners(corners);var p=corners.Select(point=>RectTransformUtility.WorldToScreenPoint(ui,point)).ToArray();return Rect.MinMaxRect(p.Min(point=>point.x),p.Min(point=>point.y),p.Max(point=>point.x),p.Max(point=>point.y));
    }
    IDisposable prefs;InputSettings oldSettings,settings;GameObject isolation;Component match;
    [TearDown]public void Cleanup(){if(match!=null)Call(match,"SetupScreen");Time.timeScale=1;if(isolation!=null)UnityEngine.Object.DestroyImmediate(isolation);if(oldSettings!=null)InputSystem.settings=oldSettings;if(settings!=null)UnityEngine.Object.DestroyImmediate(settings);prefs?.Dispose();Sports("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);}
    IEnumerator Load(string name)
    {
        prefs=(IDisposable)Sports("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"OwnedVenue"+Guid.NewGuid().ToString("N")});Sports("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);
        oldSettings=InputSystem.settings;settings=UnityEngine.Object.Instantiate(oldSettings);settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=settings;
        isolation=new GameObject("Venue original-input isolation");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Sports("SportsProbeInputIsolation"));
        EditorSceneManager.LoadSceneInPlayMode("Assets/"+name+"/Scenes/"+name+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
        match=UnityEngine.Object.FindObjectsByType(Sports("SportsEventSession")).Cast<Component>().Single();Call(match,"SetupScreen");Get(match,"setup").GetType().GetField("count").SetValue(Get(match,"setup"),4);Call(match,"NormalizeSetup");Call(match,"AllBots");Assert.That((bool)Call(match,"StartMatch"),Is.True);
    }
    static Transform[] Logos(Component source)=>source.gameObject.scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Transform>(true)).Where(item=>item.name=="Owned Impossible original venue logo").ToArray();
    void Capture(string name,int owner,Transform logo,bool strictGuess)
    {
        var view=strictGuess?(Camera)Get(Get(match,"boardCamera"),"view"):((Camera[])Get(match,"cameras")).First(camera=>camera.enabled&&camera.gameObject.activeInHierarchy);var menus=(Component)Get(match,"menus");var save=Sports("SportsControlledCapture").GetMethods().Single(method=>method.Name=="Save"&&method.GetParameters().Length==5);
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})
        {
            string path="Logs/TruckSports/PlaytestRepair/ImpossibleVenueLogosControlledNative/"+name+"-owner"+owner+"-"+size.y+".png";
            Action<Camera> inspect=ui=>
            {
                if(!strictGuess&&Vector3.Dot(view.transform.forward,logo.position-view.transform.position)<=view.nearClipPlane){TestContext.WriteLine("CONTROLLED_NATIVE_VENUE Bake distant backdrop is outside current gameplay camera; no always-visible scenery claim.");return;}
                var projected=Project(logo.GetComponent<Renderer>(),view,size);TestContext.WriteLine("CONTROLLED_NATIVE_VENUE "+name+" owner="+owner+" size="+size+" logoPixels="+projected+" camera="+view.transform.position+"/"+view.transform.rotation.eulerAngles+" fov="+view.fieldOfView);
                if(!strictGuess)return;
                Assert.That(projected.xMin,Is.GreaterThan(2));Assert.That(projected.xMax,Is.LessThan(size.x-2));Assert.That(projected.yMin,Is.GreaterThan(2));Assert.That(projected.yMax,Is.LessThan(size.y-2));Assert.That(projected.width,Is.GreaterThanOrEqualTo(size.x*.0375f));Assert.That(projected.height,Is.GreaterThanOrEqualTo(size.y/30f));
                var header=(Component)Get(menus,"header");foreach(var element in new[]{header.transform.parent.GetComponent<RectTransform>(),((Component)Get(match,"answerLabel")).transform as RectTransform,((GameObject)Get(match,"questionPanel")).GetComponent<RectTransform>()})
                {var bounds=UiBounds(element,ui);Assert.That(projected.Overlaps(bounds),Is.False,"Original "+element.name+" must remain clear of the physical board emblem.");}
                var original=logo.parent.Find("Mechanical board title").GetComponent<Renderer>();Assert.That(projected.Overlaps(Project(original,view,size)),Is.False,"Original board header stays visible and separate.");
            };
            save.Invoke(null,new object[]{match,size.x,size.y,path,inspect});var bytes=File.ReadAllBytes(path);int width=bytes[16]<<24|bytes[17]<<16|bytes[18]<<8|bytes[19],height=bytes[20]<<24|bytes[21]<<16|bytes[22]<<8|bytes[23];Assert.That(width,Is.EqualTo(size.x));Assert.That(height,Is.EqualTo(size.y));
        }
    }
    [UnityTest,Timeout(70000)]public IEnumerator FourActualGuessTurnsKeepOwnedHeaderLogosInsideViewAndClearOfOriginalAnswerUi()
    {
        yield return Load("TruckGuessThat");var logos=Logos(match);Assert.That(logos,Has.Length.EqualTo(4));var ids=logos.Select(item=>item.GetInstanceID()).ToArray();var observed=new HashSet<int>();float deadline=Time.realtimeSinceStartup+40;
        while(observed.Count<4&&Time.realtimeSinceStartup<deadline&&!(bool)Get(match,"Finished"))
        {
            int owner=(int)Get(Get(match,"Rules"),"Turn");yield return null;
            if(observed.Contains(owner))continue;
            var boards=(Transform[])Get(match,"boardFocus");if(!boards[owner].gameObject.activeInHierarchy)continue;
            var logo=boards[owner].Find("Owned Impossible original venue logo");Assert.That(logo,Is.Not.Null);Assert.That(logo.gameObject.activeInHierarchy,Is.True);Assert.That(logo.gameObject.layer,Is.EqualTo(boards[owner].gameObject.layer));foreach(var other in logos.Where(item=>item!=logo))Assert.That(other.gameObject.activeInHierarchy,Is.False,"Unfocused/private boards cannot leak extra brand geometry.");Capture("TruckGuessThat",owner,logo,true);observed.Add(owner);
        }
        Assert.That(observed.Count,Is.EqualTo(4),"Observe actual AI question/answer turn transitions, never force owner/board state.");Call(match,"Pause",0,false);yield return new WaitForSecondsRealtime(.2f);Assert.That((bool)Get(match,"Paused"),Is.True);Call(match,"Resume");Call(match,"SetupScreen");Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return null;CollectionAssert.AreEqual(ids,Logos(match).Select(item=>item.GetInstanceID()).ToArray());
    }
    [UnityTest,Timeout(40000)]public IEnumerator BakeOriginalBackdropCarriesOnlyRenderOnlyLogoBesideUnchangedTitleDuringOpeningAndRematch()
    {
        yield return Load("TruckBakeOff");yield return new WaitForSecondsRealtime(2.5f);var logo=Logos(match).Single();var id=logo.GetInstanceID();var renderer=logo.GetComponent<MeshRenderer>();Assert.That(renderer.shadowCastingMode,Is.EqualTo(ShadowCastingMode.Off));Assert.That(renderer.receiveShadows,Is.False);Assert.That(logo.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(logo.GetComponentsInChildren<Rigidbody>(true),Is.Empty);
        Assert.That(logo.parent.name,Is.EqualTo("Giant television bake-off title backdrop"));Assert.That(logo.position.y-3.5f,Is.GreaterThan(0));Assert.That(logo.position.y+3.5f,Is.LessThan(16));var title=match.gameObject.scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<TextMesh>(true)).Single(item=>item.name=="Original television show title");Assert.That(title.text,Is.EqualTo("TRUCK BAKE-OFF"));Assert.That(title.GetComponent<MeshRenderer>().enabled,Is.True);var glyphs=title.GetComponent<Renderer>().bounds;Assert.That(new Rect(renderer.bounds.min.x,renderer.bounds.min.y,renderer.bounds.size.x,renderer.bounds.size.y).Overlaps(new Rect(glyphs.min.x,glyphs.min.y,glyphs.size.x,glyphs.size.y)),Is.False);Capture("TruckBakeOff",0,logo,false);
        Call(match,"Pause",0,false);yield return new WaitForSecondsRealtime(.2f);Assert.That((bool)Get(match,"Paused"),Is.True);Call(match,"Resume");Call(match,"SetupScreen");Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return null;Assert.That(Logos(match).Single().GetInstanceID(),Is.EqualTo(id));TestContext.WriteLine("Bake backdrop is world scenery; opening projections are recorded, not a claim that every gameplay camera always shows a distant venue sign.");
    }
}
#endif
