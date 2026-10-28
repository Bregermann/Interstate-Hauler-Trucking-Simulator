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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Saved-scene, ordinary AI starts. GPU witnesses are CONTROLLED NATIVE world-only
// renders, never substitutes for actual Windows backbuffer or human acceptance.
public sealed class ImpossibleOpeningPresentationTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object value,string name){var t=value.GetType();var f=t.GetField(name,Any);return f!=null?f.GetValue(value):t.GetProperty(name,Any).GetValue(value);}
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethods(Any).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length&&m.GetParameters().Select((p,i)=>args[i]==null?!p.ParameterType.IsValueType:p.ParameterType.IsInstanceOfType(args[i])).All(v=>v)).Invoke(value,args);
    Component match;GameObject isolation;IDisposable preferences;
    IEnumerator Load(string scene)
    {
        preferences=(IDisposable)Runtime("SportsPrefs").GetMethod("IsolateValidation").Invoke(null,new object[]{"OpeningPresentation"+Guid.NewGuid().ToString("N")});
        isolation=new GameObject("Original physical devices isolated for opening presentation regression");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));
        Runtime("SportsCameraOptions").GetMethod("Set").Invoke(null,new object[]{false});
        EditorSceneManager.LoadSceneInPlayMode("Assets/"+scene+"/Scenes/"+scene+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
        match=(Component)UnityEngine.Object.FindAnyObjectByType(Runtime("SportsEventSession"));Assert.That(match,Is.Not.Null);Assert.That(match.gameObject.scene.name,Is.EqualTo(scene));
        var setup=Get(match,"setup");setup.GetType().GetField("count").SetValue(setup,Mathf.Clamp(4,(int)Get(match,"MinimumPlayers"),(int)Get(match,"MaximumPlayers")));Call(match,"NormalizeSetup");Call(match,"AllBots");
    }
    void Cleanup(){Time.timeScale=1;if(match!=null)Call(match,"SetupScreen");if(isolation!=null)UnityEngine.Object.DestroyImmediate(isolation);preferences?.Dispose();Runtime("SportsParticipantRegistry").GetMethod("ForgetCache").Invoke(null,null);match=null;preferences=null;}
    static Rect Pixels(RectTransform rect,Camera ui){var points=new Vector3[4];rect.GetWorldCorners(points);var projected=points.Select(p=>(Vector2)ui.WorldToScreenPoint(p)).ToArray();return Rect.MinMaxRect(projected.Min(p=>p.x),projected.Min(p=>p.y),projected.Max(p=>p.x),projected.Max(p=>p.y));}
    static void Separate(Rect a,Rect b,string name){float x=Mathf.Min(a.xMax,b.xMax)-Mathf.Max(a.xMin,b.xMin),y=Mathf.Min(a.yMax,b.yMax)-Mathf.Max(a.yMin,b.yMin);Assert.That(x<=1||y<=1,Is.True,name+" actual pixel overlap "+a+" / "+b);}
    void CaptureHud(string label,Action<Camera> inspect)
    {
        var menus=(Component)Get(match,"menus");var save=Runtime("SportsControlledCapture").GetMethods().Single(m=>m.Name=="Save"&&m.GetParameters().Length==6);
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})save.Invoke(null,new object[]{menus.GetComponent<Canvas>(),Get(match,"cameras"),size.x,size.y,"Logs/TruckSports/PlaytestRepair/ImpossibleOpeningPresentationNative/"+label+"-controlled-"+size.y+".png",inspect});
    }
    void AssertHud(Camera ui)
    {
        Canvas.ForceUpdateCanvases();var menus=(Component)Get(match,"menus");var status=(Component)Get(menus,"status");if(!status.gameObject.activeInHierarchy)return;
        var bounds=Pixels(status.GetComponent<RectTransform>(),ui);var hud=(GameObject)Get(menus,"hud");
        foreach(var action in hud.GetComponentsInChildren(Runtime("SportsMenuAction"),false).Cast<Component>())Separate(bounds,Pixels(action.GetComponent<RectTransform>(),ui),match.name+" status/action "+Get(action,"action"));
        var cards=hud.transform.Find("Broadcast participant cards");if(cards!=null&&cards.gameObject.activeInHierarchy)foreach(Transform card in cards)if(card.gameObject.activeInHierarchy&&card.name.StartsWith("Participant card "))Separate(bounds,Pixels((RectTransform)card,ui),match.name+" status/participant");
        if(match.gameObject.scene.name=="TruckPetPals"){
            var care=UnityEngine.Object.FindAnyObjectByType(Type.GetType("LWS.TruckPetPals.PetCareHUD, Assembly-CSharp",true));foreach(var card in (IEnumerable)Get(care,"cards")){var r=(RectTransform)Get(card,"root");if(r.gameObject.activeInHierarchy)Separate(bounds,Pixels(r,ui),"actual pet care/status");}
        }
        if(match.gameObject.scene.name=="TruckPetPals")
        {
            var care=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Single(c=>c.GetType().Name=="PetCareHUD");int owner=0;
            foreach(var item in (Array)Get(care,"cards"))
            {
                var root=(RectTransform)Get(item,"root");if(!root.gameObject.activeInHierarchy)continue;var identity=(Component)Get(item,"identity");var player=((Array)Get(Get(match,"setup"),"players")).GetValue(owner++);
                Assert.That((string)Get(identity,"text"),Does.Contain((string)Get(player,"name")),"Actual crew name accompanies pet identity.");Assert.That((bool)Get(identity,"isTextOverflowing"),Is.False,"Owner/pet identity must fit its original care card.");
            }
        }
        var back=hud.transform.Find("Broadcast status backplate");Assert.That(back,Is.Not.Null);Assert.That(Pixels((RectTransform)back,ui),Is.EqualTo(bounds));
    }
    [UnityTest,Timeout(120000)] public IEnumerator FourEightPetAndSnowOriginalHudRowsKeepActualContextSeparateAt720And1080()
    {
        foreach(string scene in new[]{"TruckTruckGoose","TruckMoonMadness","TruckPetPals","TruckSnowmanBuilding","TruckTetherball"}){
            try{yield return Load(scene);Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return new WaitForSecondsRealtime(.35f);CaptureHud(scene+"-actual-original-hud",AssertHud);Call(match,"Pause",0,false);yield return null;Assert.That(Get(match,"Paused"),Is.True);Call(match,"Resume");Call(match,"SetupScreen");Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return new WaitForSecondsRealtime(.25f);CaptureHud(scene+"-actual-rematch-hud",AssertHud);}finally{Cleanup();}
        }
    }
    [UnityTest,Timeout(120000)] public IEnumerator ArchaeologyAndSnowmanWorkshopPauseControlsDoNotOverlap()
    {
        var failures=new List<string>();string witness=Guid.NewGuid().ToString("N");
        foreach(string scene in new[]{"TruckArchaeology","TruckSnowmanBuilding"}){
            try{yield return Load(scene);Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return new WaitForSecondsRealtime(.35f);Call(match,"Pause",0,false);yield return new WaitForSecondsRealtime(.2f);Assert.That(Get(match,"Paused"),Is.True);
                var menus=(Component)Get(match,"menus");var page=((GameObject[])Get(menus,"pages"))[3];
                CaptureHud(scene+"-workshop-pause-"+witness,ui=>{
                    Canvas.ForceUpdateCanvases();var buttons=page.GetComponentsInChildren(AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEngine.UI.Button",false)).Single(t=>t!=null),false).Cast<Component>().Select(c=>c.GetComponent<RectTransform>()).Distinct().ToArray();
                    Assert.That(buttons.Length,Is.GreaterThanOrEqualTo(8));
                    var status=(RectTransform)page.transform.Find("Workshop storage status");Assert.That(status,Is.Not.Null);
                    var controls=buttons.Concat(new[]{status}).ToArray();
                    for(int i=0;i<controls.Length;i++)for(int j=i+1;j<controls.Length;j++){
                        Rect a=Pixels(controls[i],ui),b=Pixels(controls[j],ui);float x=Mathf.Min(a.xMax,b.xMax)-Mathf.Max(a.xMin,b.xMin),y=Mathf.Min(a.yMax,b.yMax)-Mathf.Max(a.yMin,b.yMin);
                        if(x>1&&y>1)failures.Add(scene+" "+ui.pixelHeight+"px "+controls[i].name+"/"+controls[j].name+" overlap "+a+" / "+b);
                        Assert.That(a.xMin,Is.GreaterThanOrEqualTo(0));Assert.That(a.xMax,Is.LessThanOrEqualTo(ui.pixelWidth));Assert.That(a.yMin,Is.GreaterThanOrEqualTo(0));Assert.That(a.yMax,Is.LessThanOrEqualTo(ui.pixelHeight));
                    }
                });
            }finally{Cleanup();}
        }
        Assert.That(failures,Is.Empty,string.Join("\n",failures));
    }
    [UnityTest,Timeout(60000)] public IEnumerator OriginalNestPauseControlsAndOrchestraSetupInformationDoNotOverlap()
    {
        foreach(string scene in new[]{"TruckNestBuilding","TruckOrchestra"}){
            try{yield return Load(scene);var menus=(Component)Get(match,"menus");if(scene=="TruckNestBuilding"){Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return new WaitForSecondsRealtime(.2f);Call(match,"Pause",0,false);yield return null;}
                CaptureHud(scene+"-actual-menu",ui=>{
                    Canvas.ForceUpdateCanvases();var pages=(GameObject[])Get(menus,"pages");var page=pages[scene=="TruckNestBuilding"?3:0];var buttons=page.GetComponentsInChildren(AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEngine.UI.Button",false)).Single(t=>t!=null),false).Cast<Component>().Select(c=>c.GetComponent<RectTransform>()).Distinct().ToArray();
                    for(int i=0;i<buttons.Length;i++)for(int j=i+1;j<buttons.Length;j++)Separate(Pixels(buttons[i],ui),Pixels(buttons[j],ui),scene+" original controls "+buttons[i].name+"/"+buttons[j].name);
                    if(scene=="TruckOrchestra"){var info=(Component)Get(menus,"setupInfo");foreach(var button in buttons)Separate(Pixels(info.GetComponent<RectTransform>(),ui),Pixels(button,ui),"setup instructions/timing control");}
                });
            }finally{Cleanup();}
        }
    }
    [Serializable]sealed class WorldFact{public string scene,scope="CONTROLLED_NATIVE_WORLD_ONLY_NOT_WINDOWS",reason,requestedWeather,requestedPresetId,requestedTime,actualTime,matchId;public int primary,bodyRenderers,sentinelPixels;public float projectedHeight,aspect;public Rect projected;public Vector3 camera,rotation,body,objectiveViewport;public bool hasObjective;public float nearClip,farClip;public string[] interveningBounds,actualTriangleHits,insideOpaqueBounds;public string weatherId,configuration;public int cullingMask;public Bounds primaryBounds,objectiveBounds;public Vector3 actualLookAt;public bool objectiveWithinConfiguredRange;public Bounds funnelBounds;public Rect funnelViewport;public int funnelCorners,funnelInside;public float groundHeight,cameraGroundClearance;public Rect objectiveAabbViewport;}
    sealed class PaintState{public Renderer renderer;public Material[] materials;public MaterialPropertyBlock global;public MaterialPropertyBlock[] slots;}
    static Texture2D RenderWorld(Camera camera,RenderTexture target){camera.Render();var old=RenderTexture.active;try{RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();return image;}finally{RenderTexture.active=old;}}
    static string[] TriangleHits(Camera camera,Component match,Ray ray,float distance,Transform actor)
    {
        // Native editor mesh intersection measures original triangles, including
        // render-only scenery. Oversized skidmark AABBs are not occlusion proof.
        var intersect=typeof(UnityEditor.HandleUtility).GetMethods(BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public).Single(m=>m.Name=="IntersectRayMesh"&&m.GetParameters().Length==4);
        var hits=new List<(float distance,string text)>();
        foreach(var renderer in match.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)))
        {
            if(renderer.transform.IsChildOf(actor)||!renderer.enabled||!renderer.gameObject.activeInHierarchy||renderer.forceRenderingOff||(camera.cullingMask&(1<<renderer.gameObject.layer))==0||!renderer.sharedMaterials.Any(m=>m!=null&&m.renderQueue<2500)||!renderer.bounds.IntersectRay(ray,out float entry)||entry>distance)continue;
            var filter=renderer.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)continue;
            var args=new object[]{ray,filter.sharedMesh,renderer.localToWorldMatrix,default(RaycastHit)};
            if(!(bool)intersect.Invoke(null,args))continue;var hit=(RaycastHit)args[3];if(hit.distance>distance)continue;
            string path=renderer.name;for(var t=renderer.transform.parent;t!=null;t=t.parent)path=t.name+"/"+path;
            hits.Add((hit.distance,path+" triangleDistance="+hit.distance.ToString("R")+" hit="+hit.point.ToString("R")+" bounds="+renderer.bounds+" cameraInside="+renderer.bounds.Contains(camera.transform.position)+" materials="+string.Join("|",renderer.sharedMaterials.Where(m=>m!=null).Select(m=>m.name+"/"+m.shader.name))));
        }
        return hits.OrderBy(h=>h.distance).Take(16).Select(h=>h.text).ToArray();
    }
    IEnumerator SharedWorld(string scene,float openingSeconds=7,string requestedWeather=null,List<WorldFact> completed=null)
    {
        Camera camera=null;RenderTexture target=null,oldTarget=null;Canvas canvas=null;bool oldCanvas=false;Material sentinel=null;Texture2D before=null,after=null;var paints=new List<PaintState>();
        try{
            yield return Load(scene);string requestedPresetId=null;
            if(requestedWeather!=null)
            {
                var pickers=((Component)Get(match,"menus")).GetComponentsInChildren(Runtime("SportsWeatherPicker"),true).Cast<Component>().ToArray();var forecast=pickers.Single(picker=>!(bool)Get(picker,"timeOfDay"));var clock=pickers.Single(picker=>(bool)Get(picker,"timeOfDay"));var catalog=Get(forecast,"catalog");var entries=((Array)Get(catalog,"entries")).Cast<object>().ToArray();int index=Array.FindIndex(entries,entry=>(string)Get(entry,"displayName")==requestedWeather);Assert.That(index,Is.GreaterThanOrEqualTo(0),"Actual installed forecast must exist.");requestedPresetId=(string)Get(entries[index],"id");Call(forecast,"Choose",index+1);Call(clock,"Choose",(int)Enum.Parse(Runtime("SportsTimeChoice"),"Day"));Assert.That((string)Get(catalog,"Selection"),Is.EqualTo(requestedPresetId));Assert.That((int)Get(Get(match,"setup"),"count"),Is.EqualTo(4),"Original four AI start, not an injected primary/pose.");
            }
            Assert.That((bool)Call(match,"StartMatch"),Is.True);yield return new WaitForSecondsRealtime(openingSeconds);
            var broadcast=(Component)Get(match,"broadcast");camera=(Camera)Get(broadcast,"view");var framing=broadcast.GetComponent(Runtime("SportsBroadcastFraming"));Assert.That(framing,Is.Not.Null);Assert.That(Get(framing,"OwnsShot"),Is.True,"Existing shared Cinemachine authority must own this test; no cab/chase override.");
            int primary=(int)Get(framing,"Primary");Assert.That(primary,Is.GreaterThanOrEqualTo(0));var truck=((Array)Get(framing,"trucks")).GetValue(primary);var body=(Rigidbody)Get(truck,"body");var host=(Component)Get(truck,"appearanceHost");
            var renderers=((IEnumerable)Get(host,"ComposedBodyRenderers")).Cast<Renderer>().Where(r=>r!=null&&r.enabled&&r.gameObject.activeInHierarchy&&!r.forceRenderingOff).ToArray();Assert.That(renderers,Is.Not.Empty,"Actual active composed tractor body, not gear/HUD/objective primitives.");
            Call(match,"Pause",0,false);yield return null;canvas=((Component)Get(match,"menus")).GetComponent<Canvas>();oldCanvas=canvas.enabled;canvas.enabled=false;
            oldTarget=camera.targetTexture;target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;yield return null;yield return null;
            var worldBounds=(Bounds)Call(framing,"TruckBounds",primary);var projected=(Rect)Call(framing,"ProjectedTruckRect",camera,primary);var ray=new Ray(camera.transform.position,(worldBounds.center-camera.transform.position).normalized);float distance=Vector3.Distance(ray.origin,worldBounds.center);
            var actualTriangleHits=TriangleHits(camera,match,ray,distance,((Component)truck).transform);
            var blockers=match.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&!r.forceRenderingOff&&(camera.cullingMask&(1<<r.gameObject.layer))!=0&&r.sharedMaterials.Any(m=>m!=null&&m.renderQueue<2500)&&!r.transform.IsChildOf(((Component)truck).transform)&&r.bounds.IntersectRay(ray,out float d)&&d<distance).OrderBy(r=>Vector3.Distance(ray.origin,r.bounds.center)).Take(12).Select(r=>r.name+" center="+r.bounds.center.ToString("R")+" size="+r.bounds.size.ToString("R")).ToArray();
            var physical=match.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Rigidbody>(true)).Select(b=>(body:b,position:b.position,rotation:b.rotation,velocity:b.linearVelocity,angular:b.angularVelocity,mass:b.mass)).ToArray();int receipts=(int)Get(Get(match,"ScoreJournal"),"Count");
            var drawFacts=new List<string>();
            Action<UnityEngine.Rendering.ScriptableRenderContext,Camera> observe=(context,renderCamera)=>{if(renderCamera!=camera)return;drawFacts.Add("ACTUAL_BEGIN_CAMERA "+camera.name+" scene="+camera.scene.name+" rect="+camera.rect+" pixelRect="+camera.pixelRect+" matrix="+camera.cullingMatrix+" worldToCamera="+camera.worldToCameraMatrix+" projection="+camera.projectionMatrix+" layerCull="+string.Join(",",camera.layerCullDistances.Select(v=>v.ToString("R")))+" occlusion="+camera.useOcclusionCulling);foreach(var r in renderers)drawFacts.Add("BODY_DURING_DRAW "+r.name+" layer="+r.gameObject.layer+" maskIncluded="+((camera.cullingMask&(1<<r.gameObject.layer))!=0)+" enabled="+r.enabled+" active="+r.gameObject.activeInHierarchy+" forceOff="+r.forceRenderingOff+" shadows="+r.shadowCastingMode+" bounds="+r.bounds+" materials="+string.Join("|",r.sharedMaterials.Where(m=>m!=null).Select(m=>m.name+"/"+m.shader.name+"/queue="+m.renderQueue)));};
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering+=observe;try{before=RenderWorld(camera,target);}finally{UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering-=observe;}
            sentinel=new Material(Shader.Find("Universal Render Pipeline/Unlit"));sentinel.SetColor("_BaseColor",Color.magenta);
            foreach(var renderer in renderers){var state=new PaintState{renderer=renderer,materials=renderer.sharedMaterials,global=new MaterialPropertyBlock()};renderer.GetPropertyBlock(state.global);state.slots=new MaterialPropertyBlock[state.materials.Length];for(int slot=0;slot<state.slots.Length;slot++){state.slots[slot]=new MaterialPropertyBlock();renderer.GetPropertyBlock(state.slots[slot],slot);}paints.Add(state);renderer.sharedMaterials=Enumerable.Repeat(sentinel,state.materials.Length).ToArray();var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);block.SetColor("_BaseColor",Color.magenta);block.SetColor("_Color",Color.magenta);renderer.SetPropertyBlock(block);for(int slot=0;slot<state.slots.Length;slot++){var perSlot=new MaterialPropertyBlock();renderer.GetPropertyBlock(perSlot,slot);perSlot.SetColor("_BaseColor",Color.magenta);perSlot.SetColor("_Color",Color.magenta);renderer.SetPropertyBlock(perSlot,slot);}}
            after=RenderWorld(camera,target);var a=before.GetPixels32();var b=after.GetPixels32();int changed=0;for(int i=0;i<a.Length;i++)if(Mathf.Abs(a[i].r-b[i].r)+Mathf.Abs(a[i].g-b[i].g)+Mathf.Abs(a[i].b-b[i].b)>80&&b[i].r>b[i].g*1.35f&&b[i].b>b[i].g*1.35f&&b[i].r>60&&b[i].b>60)changed++;
            foreach(var p in physical){Assert.That(p.body.position,Is.EqualTo(p.position));Assert.That(p.body.rotation,Is.EqualTo(p.rotation));Assert.That(p.body.linearVelocity,Is.EqualTo(p.velocity));Assert.That(p.body.angularVelocity,Is.EqualTo(p.angular));Assert.That(p.body.mass,Is.EqualTo(p.mass));}Assert.That(Get(Get(match,"ScoreJournal"),"Count"),Is.EqualTo(receipts),"Rendering never awards points/outcomes.");
            string folder=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/ImpossibleOpeningWorldNativeV15/"+scene+"/"+(requestedWeather==null?"ordinary-start":requestedWeather.Replace(" ","-"))+"/age-"+openingSeconds.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture));Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,"original-world-no-HUD-controlled.png"),before.EncodeToPNG());File.WriteAllBytes(Path.Combine(folder,"actual-body-sentinel-controlled.png"),after.EncodeToPNG());
            var fact=new WorldFact{scene=scene,requestedWeather=requestedWeather,requestedPresetId=requestedPresetId,requestedTime=requestedWeather==null?null:"Day",actualTime=Get(match,"presentationWeather")==null?null:Get(Get(Get(match,"presentationWeather"),"matchTime"),"Applied").ToString(),matchId=(string)Get(match,"MatchId"),primary=primary,bodyRenderers=renderers.Length,sentinelPixels=changed,projectedHeight=projected.height,aspect=camera.aspect,projected=projected,camera=camera.transform.position,rotation=camera.transform.rotation.eulerAngles,body=body.position,reason=(string)Get(framing,"ShotReason"),interveningBounds=blockers,actualTriangleHits=actualTriangleHits,cullingMask=camera.cullingMask,configuration=JsonUtility.ToJson(Get(framing,"Configuration")),primaryBounds=worldBounds,objectiveBounds=(Bounds)Get(Get(framing,"action"),"objective"),actualLookAt=((Transform)Get(Get(broadcast,"rig"),"LookAt")).position,objectiveWithinConfiguredRange=Vector3.Distance(worldBounds.center,((Bounds)Get(Get(framing,"action"),"objective")).center)<Mathf.Max(2,Mathf.Max(worldBounds.size.x,worldBounds.size.z))*(float)Get(Get(framing,"Configuration"),"objectiveRangeInTruckLengths"),weatherId=Get(match,"presentationWeather")==null?"NO WEATHER":(string)Get(Get(match,"presentationWeather"),"PresetId"),insideOpaqueBounds=match.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&!r.name.Contains("Skidmark")&&r.bounds.Contains(camera.transform.position)).Select(r=>r.name+" bounds="+r.bounds+" materials="+string.Join("|",r.sharedMaterials.Where(m=>m!=null).Select(m=>m.name+"/"+m.shader.name))).Take(24).ToArray(),nearClip=camera.nearClipPlane,farClip=camera.farClipPlane,hasObjective=(bool)Get(Get(framing,"action"),"hasObjective"),objectiveViewport=camera.WorldToViewportPoint(((Bounds)Get(Get(framing,"action"),"objective")).center)};File.WriteAllText(Path.Combine(folder,"actual-shared-camera-world.json"),JsonUtility.ToJson(fact,true));TestContext.WriteLine(JsonUtility.ToJson(fact));
            if(scene=="TruckNestBuilding"&&changed<=64)
            {
                // A counterfactual diagnostic, not a passing replacement image:
                // preserve the original failure and restore every vendor effect.
                var vendor=(Component)Get(Get(match,"presentationWeather"),"vendor");var notes=new List<string>();
                File.WriteAllLines(Path.Combine(folder,"actual-render-draw-state.txt"),drawFacts);
                var worldRenderers=match.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&!r.forceRenderingOff).ToArray();
                File.WriteAllLines(Path.Combine(folder,"actual-world-layer-and-material-state.txt"),worldRenderers.GroupBy(r=>r.gameObject.layer).Select(g=>"LAYER "+g.Key+" count="+g.Count()+" cameraIncluded="+((camera.cullingMask&(1<<g.Key))!=0)).Concat(worldRenderers.Select(r=>r.name+" layer="+r.gameObject.layer+" bounds="+r.bounds+" materials="+string.Join("|",r.sharedMaterials.Where(m=>m!=null).Select(m=>m.name+"/"+m.shader.name+"/queue="+m.renderQueue)))));
                Func<string,Action,Action,int> counter=(label,change,restore)=>{Texture2D image=null;try{change();image=RenderWorld(camera,target);Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,"CF-NOT-ACCEPT-"+label+".png"),image.EncodeToPNG());var pixels=image.GetPixels32();int witness=0;for(int y=Mathf.Clamp((int)(projected.yMin*image.height),0,image.height-1);y<=Mathf.Clamp((int)(projected.yMax*image.height),0,image.height-1);y++)for(int x=Mathf.Clamp((int)(projected.xMin*image.width),0,image.width-1);x<=Mathf.Clamp((int)(projected.xMax*image.width),0,image.width-1);x++){var pixel=pixels[y*image.width+x];if(pixel.r>pixel.g*1.35f&&pixel.b>pixel.g*1.35f&&pixel.r>60&&pixel.b>60)witness++;}notes.Add("CAUSAL_CF "+label+" primaryRectMagenta="+witness);return witness;}finally{restore();if(image!=null)UnityEngine.Object.DestroyImmediate(image);}};
                var cameraData=camera.GetComponent(Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime",true));
                var pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;var pipelineState=new UnityEditor.SerializedObject(pipeline);var rendererData=pipelineState.FindProperty("m_RendererDataList");int defaultIndex=pipelineState.FindProperty("m_DefaultRendererIndex").intValue;int originalIndex=cameraData==null?-1:(int)Get(cameraData,"m_RendererIndex");
                notes.Add("ACTUAL_PIPELINE "+UnityEditor.AssetDatabase.GetAssetPath(pipeline)+" name="+pipeline.name+" defaultIndex="+defaultIndex+" actualCameraRendererIndex="+originalIndex+" additionalCameraDataPresent="+(cameraData!=null)+" activeRenderer="+(cameraData==null?Call(pipeline,"GetRenderer",defaultIndex):Get(cameraData,"scriptableRenderer")).GetType().FullName);
                for(int index=0;index<rendererData.arraySize;index++)
                {
                    var data=rendererData.GetArrayElementAtIndex(index).objectReferenceValue;var serialized=new UnityEditor.SerializedObject(data);var opaqueMask=serialized.FindProperty("m_OpaqueLayerMask");var transparent=serialized.FindProperty("m_TransparentLayerMask");
                    notes.Add("RENDERER_DATA "+index+" path="+UnityEditor.AssetDatabase.GetAssetPath(data)+" type="+data.GetType().FullName+" opaque="+(opaqueMask==null?"N/A":opaqueMask.intValue.ToString())+" transparent="+(transparent==null?"N/A":transparent.intValue.ToString()));
                    foreach(var feature in (IEnumerable)Get(data,"rendererFeatures"))if(feature!=null)notes.Add("FEATURE "+index+" "+feature.GetType().FullName+" active="+Get(feature,"isActive")+" data="+JsonUtility.ToJson(feature));
                }
                File.WriteAllLines(Path.Combine(folder,"actual-pipeline-and-renderers.txt"),notes);
                var actualData=rendererData.GetArrayElementAtIndex(originalIndex<0?defaultIndex:originalIndex).objectReferenceValue;
                foreach(var feature in (IEnumerable)Get(actualData,"rendererFeatures"))if(feature!=null){bool active=(bool)Get(feature,"isActive");counter("feature-off",()=>Call(feature,"SetActive",false),()=>Call(feature,"SetActive",active));}
                var terrains=UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None).Where(t=>t.gameObject.scene==match.gameObject.scene).ToArray();var terrainDraw=terrains.Select(t=>(terrain:t,height:t.drawHeightmap,foliage:t.drawTreesAndFoliage)).ToArray();
                counter("terrain-off",()=>{foreach(var state in terrainDraw){state.terrain.drawHeightmap=false;state.terrain.drawTreesAndFoliage=false;}},()=>{foreach(var state in terrainDraw){state.terrain.drawHeightmap=state.height;state.terrain.drawTreesAndFoliage=state.foliage;}});
                var opaque=worldRenderers.Where(r=>!renderers.Contains(r)&&r.sharedMaterials.Any(m=>m!=null&&m.renderQueue<2500)).ToArray();
                Func<string,Renderer[],int> hide=(label,group)=>counter(label,()=>{foreach(var renderer in group)renderer.enabled=false;},()=>{foreach(var renderer in group)renderer.enabled=true;});
                var originalClear=camera.clearFlags;var originalBackground=camera.backgroundColor;counter("skybox-off",()=>{camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;},()=>{camera.clearFlags=originalClear;camera.backgroundColor=originalBackground;});
                var featureStates=((IEnumerable)Get(actualData,"rendererFeatures")).Cast<object>().Where(f=>f!=null).Select(f=>(feature:f,active:(bool)Get(f,"isActive"))).ToArray();
                counter("combined-isolation",()=>{foreach(var state in featureStates)Call(state.feature,"SetActive",false);foreach(var state in terrainDraw){state.terrain.drawHeightmap=false;state.terrain.drawTreesAndFoliage=false;}foreach(var renderer in opaque)renderer.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;},()=>{foreach(var state in featureStates)Call(state.feature,"SetActive",state.active);foreach(var state in terrainDraw){state.terrain.drawHeightmap=state.height;state.terrain.drawTreesAndFoliage=state.foliage;}foreach(var renderer in opaque)renderer.enabled=true;camera.clearFlags=originalClear;camera.backgroundColor=originalBackground;});
                var allNonBody=worldRenderers.Where(r=>!renderers.Contains(r)).ToArray();
                counter("body-only-diagnostic",()=>{foreach(var state in featureStates)Call(state.feature,"SetActive",false);foreach(var state in terrainDraw){state.terrain.drawHeightmap=false;state.terrain.drawTreesAndFoliage=false;}foreach(var renderer in allNonBody)renderer.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;},()=>{foreach(var state in featureStates)Call(state.feature,"SetActive",state.active);foreach(var state in terrainDraw){state.terrain.drawHeightmap=state.height;state.terrain.drawTreesAndFoliage=state.foliage;}foreach(var renderer in allNonBody)renderer.enabled=true;camera.clearFlags=originalClear;camera.backgroundColor=originalBackground;});
                int revealed=hide("all-nonbody",opaque);notes.Add("OPAQUE_GROUP count="+opaque.Length+" revealed="+revealed);
                if(revealed>64)
                {
                    var suspects=opaque;
                    for(int step=0;step<10&&suspects.Length>1;step++)
                    {
                        var left=suspects.Take(suspects.Length/2).ToArray();var right=suspects.Skip(left.Length).ToArray();
                        if(hide("binary-"+step+"-a",left)>64)suspects=left;else if(hide("binary-"+step+"-b",right)>64)suspects=right;else{notes.Add("MULTIPLE_OCCLUDER_INTERACTION at="+step+" candidates="+suspects.Length);break;}
                    }
                    foreach(var suspect in suspects.Take(24)){string path=suspect.name;for(var parent=suspect.transform.parent;parent!=null;parent=parent.parent)path=parent.name+"/"+path;notes.Add("CAUSAL_RENDERER_CANDIDATE "+path+" bounds="+suspect.bounds+" materials="+string.Join("|",suspect.sharedMaterials.Where(m=>m!=null).Select(m=>m.name+"/"+m.shader.name)));}
                }
                File.WriteAllLines(Path.Combine(folder,"causal-feature-terrain-renderer-isolation.txt"),notes);
                foreach(var effect in vendor.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c!=null&&c.enabled&&(c.GetType().Name.Contains("FullScreenFog")||c.GetType().Name.Contains("FullScreenClouds")||c.GetType().Name.Contains("SphereFog")||c.GetType().Name.Contains("BoxFog"))))
                {
                    var profile=effect.GetType().GetField("FogProfile",Any);notes.Add(effect.GetType().FullName+" original="+JsonUtility.ToJson(effect)+(profile==null?"":" profile="+JsonUtility.ToJson(profile.GetValue(effect))));
                    try{effect.enabled=false;var diagnosis=RenderWorld(camera,target);try{Directory.CreateDirectory(folder);string suffix=effect.GetType().Name.Contains("FullScreenFog")?"Fog":effect.GetType().Name.Contains("FullScreenClouds")?"Clouds":effect.GetType().Name;File.WriteAllBytes(Path.Combine(folder,"CF-NOT-ACCEPT-"+suffix+".png"),diagnosis.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(diagnosis);}}finally{effect.enabled=true;}
                }
                File.WriteAllLines(Path.Combine(folder,"original-screen-effects-and-counterfactual-scope.txt"),notes);
            }
            if(scene=="TruckTornadoRodeo")
            {
                var storm=(Component)Get(match,"presentation");var funnel=(ParticleSystem)Get(storm,"funnel");var particles=new ParticleSystem.Particle[funnel.main.maxParticles];int count=funnel.GetParticles(particles);fact.funnelBounds=funnel.GetComponent<ParticleSystemRenderer>().bounds;var originalGround=(Collider)Get(match,"ground");Assert.That(originalGround.Raycast(new Ray(camera.transform.position+Vector3.up*300,Vector3.down),out var groundHit,600),Is.True,"Original saved Vista collision surface under actual camera is measured.");fact.groundHeight=groundHit.point.y;fact.cameraGroundClearance=camera.transform.position.y-groundHit.point.y;
                Vector2 min=Vector2.one*float.MaxValue,max=Vector2.one*float.MinValue;
                for(int index=0;index<count;index++)
                {
                    var center=funnel.main.simulationSpace==ParticleSystemSimulationSpace.World?particles[index].position:funnel.transform.TransformPoint(particles[index].position);float half=particles[index].GetCurrentSize(funnel)*.5f;
                    for(int corner=0;corner<4;corner++){var point=camera.WorldToViewportPoint(center+camera.transform.right*((corner&1)==0?-half:half)+camera.transform.up*((corner&2)==0?-half:half));min=Vector2.Min(min,point);max=Vector2.Max(max,point);fact.funnelCorners++;if(point.z>camera.nearClipPlane&&point.x>=.02f&&point.x<=.98f&&point.y>=.06f&&point.y<=.94f)fact.funnelInside++;}
                }
                fact.funnelViewport=Rect.MinMaxRect(min.x,min.y,max.x,max.y);Vector2 boxMin=Vector2.one*float.MaxValue,boxMax=Vector2.one*float.MinValue;for(int corner=0;corner<8;corner++){var bounds=fact.objectiveBounds;var point=camera.WorldToViewportPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1)));boxMin=Vector2.Min(boxMin,point);boxMax=Vector2.Max(boxMax,point);}fact.objectiveAabbViewport=Rect.MinMaxRect(boxMin.x,boxMin.y,boxMax.x,boxMax.y);File.WriteAllText(Path.Combine(folder,"actual-shared-camera-world.json"),JsonUtility.ToJson(fact,true));TestContext.WriteLine("FULL_FUNNEL_SILHOUETTE "+JsonUtility.ToJson(fact));
                // ParticleRenderer AABB encloses empty air around the tapered
                // storm. Keep its projected eight-corner rectangle in telemetry;
                // all actual live billboard corners above are the visible shape.
            }
            // All three explicit-weather world records are persisted before
            // acceptance checks, so one bad shot cannot hide the other starts.
            if(completed!=null)completed.Add(fact);else AssertWorld(fact);
        }finally{
            foreach(var p in paints){if(p.renderer==null)continue;p.renderer.sharedMaterials=p.materials;p.renderer.SetPropertyBlock(p.global);for(int i=0;i<p.slots.Length;i++)p.renderer.SetPropertyBlock(p.slots[i],i);Assert.That(p.renderer.sharedMaterials,Is.EqualTo(p.materials),"Original body material references restored.");}
            if(camera!=null)camera.targetTexture=oldTarget;if(canvas!=null)canvas.enabled=oldCanvas;if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}if(sentinel!=null)UnityEngine.Object.DestroyImmediate(sentinel);if(before!=null)UnityEngine.Object.DestroyImmediate(before);if(after!=null)UnityEngine.Object.DestroyImmediate(after);Cleanup();
        }
    }
    static void AssertWorld(WorldFact fact)
    {
        string scene=fact.scene;var projected=fact.projected;int changed=fact.sentinelPixels;
        if(fact.requestedWeather!=null){Assert.That(fact.weatherId,Is.EqualTo(fact.requestedPresetId),"Explicit original forecast retained: "+fact.requestedWeather);Assert.That(fact.actualTime,Is.EqualTo(fact.requestedTime),"Original Day selector applied.");}
            if(scene=="TruckPinataPanic")Assert.That(fact.actualTriangleHits.Any(hit=>hit.Contains("Laminated actual pinata suspension beam")),Is.False,"Original physical beam must not occlude the actual primary body center in the opening shot.");
            if(scene=="TruckBallet")Assert.That(fact.camera.y,Is.LessThan(29.6f),"Original opaque theater ceiling remains above the actual shared camera.");
            if(scene=="TruckTornadoRodeo")
            {
                Assert.That(fact.funnelCorners,Is.GreaterThan(0),"Original live storm particle system supplies actual silhouette points.");
                Assert.That(changed,Is.GreaterThan(64));Assert.That(projected.xMin,Is.GreaterThanOrEqualTo(.01f));Assert.That(projected.xMax,Is.LessThanOrEqualTo(.99f));Assert.That(projected.yMin,Is.GreaterThanOrEqualTo(.03f),"The whole actual primary must remain visible with the whole storm.");Assert.That(projected.yMax,Is.LessThanOrEqualTo(.92f));
                Assert.That(fact.cameraGroundClearance,Is.GreaterThanOrEqualTo(2),"Actual shared camera stays safely above the original Vista mesh, not just TerrainCollider terrain.");
                Assert.That(fact.funnelInside,Is.EqualTo(fact.funnelCorners),"Full actual live billboard silhouette must remain inside the world frame; center visibility alone cannot pass.");Assert.That(projected.height,Is.GreaterThanOrEqualTo(.08f),"Readable original tractor remains visible with the whole storm.");
            }
            if(scene=="TruckTornadoRodeo"){Assert.That(fact.hasObjective,Is.True);Assert.That(fact.objectiveViewport.z,Is.GreaterThan(fact.nearClip));Assert.That(fact.objectiveViewport.x,Is.InRange(.03f,.97f));Assert.That(fact.objectiveViewport.y,Is.InRange(.08f,.92f),"Actual tornado entry/flying objective must be framed, not merely a visible tractor on an empty snow field.");}
            Assert.That(changed,Is.GreaterThan(64),"Actual composed tractor body must contribute visible world pixels through original scene geometry. HUD variance cannot pass; do not hide roofs/trees/beams.");Assert.That(projected.height,Is.GreaterThanOrEqualTo(.06f),"Actual primary readability, not all unfocused AI.");Assert.That(projected.xMin,Is.GreaterThanOrEqualTo(.01f));Assert.That(projected.xMax,Is.LessThanOrEqualTo(.99f));Assert.That(projected.yMin,Is.GreaterThanOrEqualTo(scene=="TruckTornadoRodeo"?.03f:.12f));Assert.That(projected.yMax,Is.LessThanOrEqualTo(.92f));
    }
    static float ActualBirdSpan(Component bird,Mesh sample,bool useScale=true)
    {
        Bounds bounds=default;bool found=false;var inverse=Quaternion.Inverse(bird.transform.rotation);
        foreach(var skin in bird.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            skin.BakeMesh(sample,useScale);foreach(var vertex in sample.vertices){var point=inverse*(skin.transform.TransformPoint(vertex)-bird.transform.position);if(!found){bounds=new Bounds(point,Vector3.zero);found=true;}else bounds.Encapsulate(point);}
        }
        return found?Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z)):0;
    }
    [UnityTest,Timeout(60000)] public IEnumerator NestAllSixOriginalAnimatedBirdsFitKnownVisualSpanAndKeepRoutesOnRematch()
    {
        Mesh sample=null;GameObject witness=null;
        try
        {
            yield return Load("TruckNestBuilding");sample=new Mesh{name="Controlled native actual animated bird geometry witness"};
            var birds=match.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)).Where(c=>c.GetType().FullName=="LWS.TruckNestBuilding.NestBirdFlight").Cast<Component>().ToArray();Assert.That(birds.Length,Is.EqualTo(6));
            var routes=birds.Select(b=>(center:(Vector3)Get(b,"center"),radius:(float)Get(b,"radius"),phase:(float)Get(b,"phase"),animator:(Animator)Get(b,"animator"))).ToArray();var controllers=routes.Select(r=>r.animator.runtimeAnimatorController).ToArray();var avatars=routes.Select(r=>r.animator.avatar).ToArray();var skins=birds.Select(b=>b.GetComponentsInChildren<SkinnedMeshRenderer>(true)).ToArray();var meshes=skins.Select(a=>a.Select(r=>r.sharedMesh).ToArray()).ToArray();var bones=skins.Select(a=>a.SelectMany(r=>r.bones).Distinct().ToArray()).ToArray();var rotations=bones.Select(a=>a.Select(b=>b.localRotation).ToArray()).ToArray();var animated=new bool[6];var records=new List<string>();
            Assert.That((bool)Call(match,"StartMatch"),Is.True);
            string telemetryFolder=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/NestAnimatedBirdFitNativeV12");Directory.CreateDirectory(telemetryFolder);string telemetryPath=Path.Combine(telemetryFolder,"per-frame-scale-contract-before-assert.txt");var observedSpans=new float[6];var observedFacts=new string[6];int witnessFrame=-1;witness=new GameObject("Actual bird geometry after original Animator and mode LateUpdate");var late=witness.AddComponent<ImpossibleOpeningLateBirdWitness>();late.Sample=()=>{for(int index=0;index<birds.Length;index++){float falseSpan=ActualBirdSpan(birds[index],sample,false);var rawFalse=sample.bounds;observedSpans[index]=ActualBirdSpan(birds[index],sample,true);var rawTrue=sample.bounds;observedFacts[index]="frame="+Time.frameCount+" bird="+index+" actorScale="+birds[index].transform.localScale.ToString("R")+" maximumSourceSpan="+Get(birds[index],"MaximumSourceSpan")+" falseWorldSpan="+falseSpan.ToString("R")+" trueWorldSpan="+observedSpans[index].ToString("R")+" falseRawCenter="+rawFalse.center.ToString("R")+" falseRawExtent="+rawFalse.extents.ToString("R")+" trueRawCenter="+rawTrue.center.ToString("R")+" trueRawExtent="+rawTrue.extents.ToString("R")+" skins="+string.Join("|",skins[index].Select(skin=>skin.name+" lossyScale="+skin.transform.lossyScale.ToString("R")+" rendererBoundsCenter="+skin.bounds.center.ToString("R")+" rendererBoundsExtent="+skin.bounds.extents.ToString("R")+" actorRelativeBasis="+(birds[index].transform.worldToLocalMatrix*skin.localToWorldMatrix).ToString("G9")));}witnessFrame=Time.frameCount;};
            for(int run=0;run<2;run++)
            {
                float until=Time.realtimeSinceStartup+4;while(Time.realtimeSinceStartup<until)
                {
                    yield return null;if(witnessFrame<0)continue;File.AppendAllLines(telemetryPath,observedFacts);
                    for(int index=0;index<birds.Length;index++)
                    {
                        var bird=birds[index];Assert.That((bool)Get(bird,"VisualFitReady"),Is.True);float span=observedSpans[index],target=(float)Get(bird,"targetWingspan");Assert.That(span,Is.GreaterThan(.1f));Assert.That(span,Is.LessThanOrEqualTo(target*1.02f),"Actual baked animated geometry, not only a conservative imported AABB.");
                        for(int bone=0;bone<bones[index].Length;bone++)animated[index]|=Quaternion.Angle(rotations[index][bone],bones[index][bone].localRotation)>.25f;
                        Assert.That(Get(bird,"center"),Is.EqualTo(routes[index].center));Assert.That(Get(bird,"radius"),Is.EqualTo(routes[index].radius));Assert.That(Get(bird,"phase"),Is.EqualTo(routes[index].phase));Assert.That(routes[index].animator.runtimeAnimatorController,Is.SameAs(controllers[index]));Assert.That(routes[index].animator.avatar,Is.SameAs(avatars[index]));Assert.That(skins[index].Select(r=>r.sharedMesh).ToArray(),Is.EqualTo(meshes[index]));Assert.That(bird.GetComponentsInChildren<Rigidbody>(true),Is.Empty);Assert.That(bird.GetComponentsInChildren<Collider>(true),Is.Empty);
                        if(Time.frameCount%20==0)records.Add("run="+run+" bird="+index+" actualSpan="+span.ToString("R")+" target="+target+" sourceEnvelope="+Get(bird,"MaximumSourceSpan")+" parentScale="+bird.transform.localScale.ToString("R"));
                    }
                }
                Call(match,"Pause",0,false);yield return null;var poses=birds.Select(b=>(p:b.transform.position,q:b.transform.rotation,s:b.transform.localScale)).ToArray();yield return new WaitForSecondsRealtime(.2f);for(int index=0;index<birds.Length;index++){Assert.That(birds[index].transform.position,Is.EqualTo(poses[index].p));Assert.That(birds[index].transform.rotation,Is.EqualTo(poses[index].q));Assert.That(birds[index].transform.localScale,Is.EqualTo(poses[index].s));}Call(match,"Resume");
                if(run==0){Call(match,"SetupScreen");Assert.That((bool)Call(match,"StartMatch"),Is.True);}
            }
            Assert.That(animated,Is.All.True,"Each original Generic skeleton actually moves; parenting/rig/clip identity is retained.");string folder=Path.GetFullPath("Logs/TruckSports/PlaytestRepair/NestAnimatedBirdFitNativeV12");Directory.CreateDirectory(folder);File.WriteAllLines(Path.Combine(folder,"actual-six-bird-wingspans-and-original-routes.txt"),records);
        }finally{if(witness!=null)UnityEngine.Object.DestroyImmediate(witness);if(sample!=null)UnityEngine.Object.DestroyImmediate(sample);Cleanup();}
    }
    [UnityTest,Timeout(120000)]public IEnumerator PinataActualSharedOpeningBodyIsVisibleThroughOriginalVenue(){foreach(float age in new[]{3.2f,5f,7f,10f})yield return SharedWorld("TruckPinataPanic",age);}
    [UnityTest,Timeout(60000)]public IEnumerator BalletActualSharedOpeningBodyIsVisibleThroughOriginalTheater(){yield return SharedWorld("TruckBallet");}
    [UnityTest,Timeout(60000)]public IEnumerator NestActualSharedOpeningBodyIsVisibleThroughOriginalCanopy(){yield return SharedWorld("TruckNestBuilding");}
    [UnityTest,Timeout(120000)]public IEnumerator TornadoActualSharedOpeningBodyIsVisibleWithEntryContext()
    {
        var facts=new List<WorldFact>();
        foreach(string weather in new[]{"Clear","Heavy Snow","Heavy Rain"})yield return SharedWorld("TruckTornadoRodeo",7,weather,facts);
        Assert.That(facts.Count,Is.EqualTo(3));foreach(var fact in facts)AssertWorld(fact);
    }
}
[DefaultExecutionOrder(32000)]
public sealed class ImpossibleOpeningLateBirdWitness:MonoBehaviour
{
    public Action Sample;
    void LateUpdate()=>Sample?.Invoke();
}
#endif
