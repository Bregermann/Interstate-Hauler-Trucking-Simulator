#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class LegacyMatchColorsLifecycleTests
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Runtime(string name)=>Type.GetType("LWS.TruckSports."+name+", Assembly-CSharp",true);
    static object Get(object owner,string name)=>owner.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(owner):owner.GetType().GetProperty(name,Any).GetValue(owner);
    static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Any).SetValue(owner,value);
    static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethods(Any).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);
    static object Registry(string name,params object[] args)=>Runtime("SportsParticipantRegistry").GetMethod(name).Invoke(null,args);
    static object Prefs(string name,params object[] args)=>Runtime("SportsPrefs").GetMethod(name).Invoke(null,args);
    static Color[] Colors(Array slots,int count)=>slots.Cast<object>().Take(count).Select(slot=>(Color)Get(slot,"color")).ToArray();
    static void PaintColor(Color actual,Color expected,string label)
    {
        // MPB SetColor/GetColor can round-trip through color-space conversion.
        // Verify the same native storage path and log every channel at float precision.
        var probe=new MaterialPropertyBlock();probe.SetColor("_BaseColor",expected);var roundtrip=probe.GetColor("_BaseColor");
        Func<Color,string> rgba=color=>"("+color.r.ToString("R")+","+color.g.ToString("R")+","+color.b.ToString("R")+","+color.a.ToString("R")+")";
        var delta=actual-expected;TestContext.WriteLine("LEGACY_MPB_RGBA "+label+" expected="+rgba(expected)+" actual="+rgba(actual)+" nativeRoundtrip="+rgba(roundtrip)+" delta="+rgba(delta));
        for(int channel=0;channel<4;channel++)
        {
            Assert.That(actual[channel],Is.EqualTo(expected[channel]).Within(.000002f),label+" paint channel "+channel);
            Assert.That(actual[channel],Is.EqualTo(roundtrip[channel]).Within(.000002f),label+" native MPB roundtrip channel "+channel);
        }
    }
    static void ColorStage(string scene,string stage) => Debug.Log("LEGACY_COLOR_STAGE "+scene+" "+stage+" frame="+Time.frameCount+" scale="+Time.timeScale+" fixed="+Time.fixedTime.ToString("R"));
    static void ClearWeatherForColorOnlyFixture()
    {
        var catalogs=Resources.FindObjectsOfTypeAll(Runtime("SportsWeatherPresets"));
        if(catalogs.Length==0)catalogs=UnityEditor.AssetDatabase.FindAssets("t:SportsWeatherPresets",new[]{"Assets/TruckSports"}).Select(UnityEditor.AssetDatabase.GUIDToAssetPath).Select(UnityEditor.AssetDatabase.LoadMainAssetAtPath).Where(asset=>asset!=null).ToArray();
        Assert.That(catalogs,Is.Not.Empty,"Use the actual installed catalog even when the indoor scene has not loaded it.");
        foreach(var catalog in catalogs)foreach(var entry in (Array)Get(catalog,"entries"))if((string)Get(entry,"displayName")=="Clear"){Call(catalog,"Select",Get(entry,"id"));return;}
        Assert.Fail("The installed explicit Clear preset is required for deterministic color-only lifecycle acceptance.");
    }
    static void Paint(Component truck,Color expected)
    {
        var block=new MaterialPropertyBlock();var host=truck.GetType().GetField("appearanceHost",Any)?.GetValue(truck);
        if(host!=null&&(bool)Get(host,"Applied")){var renderers=(Renderer[])Get(host,"paintRenderers");foreach(var slot in (Array)Get(host,"paintSlots")){var renderer=renderers[(int)Get(slot,"rendererIndex")];renderer.GetPropertyBlock(block,(int)Get(slot,"materialIndex"));PaintColor(block.GetColor("_BaseColor"),expected,truck.name+"/"+renderer.name);}}
        else{var renderer=(Renderer)Get(truck,"paint");Assert.That(renderer.enabled&&renderer.gameObject.activeInHierarchy,Is.True);renderer.GetPropertyBlock(block);PaintColor(block.GetColor("_BaseColor"),expected,truck.name+"/"+renderer.name);}
    }
    [UnityTest,Timeout(120000)]public IEnumerator SavedCornholeConcurrentFourKeepsColorScopeAcrossOriginalLaunchRoundBoundaries()=>Inspect("TruckCornhole",false);
    [UnityTest,Timeout(120000)]public IEnumerator SavedCheeseFourKeepsParticipantColorsAndUnscoredBackgroundPopulation()=>Inspect("TruckCheeseRolling",false);
    [UnityTest,Timeout(120000)]public IEnumerator SavedBowlingSimultaneousFourKeepsAppearanceOwnerAndPreferredBytes()=>Inspect("TruckBowling",false);
    [UnityTest,Timeout(120000)]public IEnumerator SavedBowlingHotSeatEightIntentionallyBypassesFfaUniqueness()=>Inspect("TruckBowling",true);
    static IEnumerator Inspect(string scene,bool hotSeat)
    {
        var prefs=(IDisposable)Prefs("IsolateValidation","LegacyMatchColors"+Guid.NewGuid().ToString("N"));Registry("ForgetCache");var isolation=new GameObject("Explicit legacy native temporary match-color boundaries");UnityEngine.Object.DontDestroyOnLoad(isolation);isolation.AddComponent(Runtime("SportsProbeInputIsolation"));Component match=null;string setupKey=null;bool bowling=scene=="TruckBowling",cornhole=scene=="TruckCornhole";int count=hotSeat?8:4;
        try
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/"+scene+"/Scenes/"+scene+".unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;var type=Type.GetType((bowling?"LWS.TruckBowling.BowlingMatch":cornhole?"LWS.TruckCornhole.CornholeMatch":"LWS.TruckCheeseRolling.CheeseMatch")+", Assembly-CSharp",true);match=(Component)UnityEngine.Object.FindAnyObjectByType(type);var setup=Get(match,"setup");Set(setup,"count",count);
            ColorStage(scene,"loaded");ClearWeatherForColorOnlyFixture();if(bowling)Call(match,"PresetAI",count,Enum.Parse(Get(setup,"mode").GetType(),hotSeat?"HotSeat":"Simultaneous"));else Call(match,"AllAI");
            for(int i=0;i<12;i++){var profile=Registry("Default",i);Registry("Rename",Get(profile,"id"),"Same Legacy Driver",new Color(.03f+i*.0004f,.3f,.9f),true);}Call(match,"RefreshParticipants",false);Call(match,"Save");var slots=(Array)Get(setup,"players");var preferred=Colors(slots,count);setupKey=(string)type.GetField("SetupKey").GetRawConstantValue();string profileBytes=(string)Prefs("GetString","TruckSports.Participants.v1",""),backup=(string)Prefs("GetString","TruckSports.Participants.v1.Backup",""),saved=(string)Prefs("GetString",setupKey,"");ColorStage(scene,"before-initial-start");Assert.That(Call(match,"StartMatch"),Is.True);ColorStage(scene,"initial-start-returned");var effective=Colors(slots,count);var assignment=Get(match,"MatchColors");
            if(hotSeat){Assert.That(assignment,Is.Null);Assert.That(effective,Is.EqualTo(preferred));}
            else{Assert.That(assignment,Is.Not.Null);for(int i=0;i<count;i++)for(int j=i+1;j<count;j++)Assert.That((float)Runtime("SportsMatchColors").GetMethod("Distance").Invoke(null,new object[]{effective[i],effective[j]}),Is.GreaterThanOrEqualTo(22-.001f));}
            float fixedStart=Time.fixedTime,wallDeadline=Time.realtimeSinceStartup+8;while(Time.fixedTime-fixedStart<.035f&&Time.realtimeSinceStartup<wallDeadline)yield return null;Assert.That(Time.fixedTime-fixedStart,Is.GreaterThanOrEqualTo(.035f),"Initial real physics must progress; do not hang a batch on a paused FixedUpdate yield.");Time.timeScale=0;ColorStage(scene,"initial-physics-complete");
            Component[] trucks=bowling?((Array)Get(match,"lanes")).Cast<object>().Take(hotSeat?1:4).Select(lane=>(Component)Get(lane,"truck")).ToArray():cornhole?((Array)Get(match,"courses")).Cast<object>().Take(4).Select(course=>(Component)Get(course,"Current")).ToArray():((Array)Get(match,"trucks")).Cast<Component>().Take(4).ToArray();for(int i=0;i<trucks.Length;i++){Assert.That(trucks[i].gameObject.activeInHierarchy,Is.True);Paint(trucks[i],effective[i]);}
            if(!bowling&&!cornhole){var all=(Array)Get(match,"trucks");Assert.That(all.Length,Is.EqualTo(16));for(int i=4;i<16;i++)Assert.That(((Component)all.GetValue(i)).gameObject.activeSelf,Is.True,"Background population remains unchanged and unscored.");}
            Call(match,"Pause",0,false);Call(match,"Save");Call(match,"RefreshParticipants",true);Assert.That(Colors(slots,count),Is.EqualTo(effective));Call(match,"Resume");Time.timeScale=0;
            if(assignment!=null){Assert.Throws<InvalidOperationException>(()=>{using((IDisposable)Call(assignment,"Suspend")){Call(match,"Save");Call(match,"RefreshParticipants",false);throw new InvalidOperationException("explicit legacy scoped-save failure");}});Assert.That(Colors(slots,count),Is.EqualTo(effective));}
            Assert.That(Prefs("GetString","TruckSports.Participants.v1",""),Is.EqualTo(profileBytes));Assert.That(Prefs("GetString","TruckSports.Participants.v1.Backup",""),Is.EqualTo(backup));Assert.That(Prefs("GetString",setupKey,""),Is.EqualTo(saved));Registry("ForgetCache");Call(match,"RefreshParticipants",false);Assert.That(Colors(slots,count),Is.EqualTo(effective));
            ColorStage(scene,"before-rematch-setup");Call(match,"ChangeSetup");ColorStage(scene,"rematch-setup-returned");Assert.That(Colors(slots,count),Is.EqualTo(preferred));Assert.That(Get(match,"MatchColors"),Is.Null);ColorStage(scene,"before-rematch-start");Assert.That(Call(match,"StartMatch"),Is.True);ColorStage(scene,"rematch-start-returned");Time.timeScale=0;Assert.That(Colors(slots,count),Is.EqualTo(effective));Assert.That(Get(match,"MatchColors")==null,Is.EqualTo(hotSeat));
            // Allow the newly prepared rematch to finish its first native frame
            // before testing scene teardown, matching a playable menu boundary.
            Time.timeScale=1;yield return null;yield return null;
            ColorStage(scene,"before-scene-exit");EditorSceneManager.LoadSceneInPlayMode("Assets/TruckSports/Scenes/TruckSports.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;ColorStage(scene,"scene-exit-returned");match=null;Assert.That(Colors(slots,count),Is.EqualTo(preferred));Assert.That(Prefs("GetString","TruckSports.Participants.v1",""),Is.EqualTo(profileBytes));
            TestContext.WriteLine("NATIVE_LEGACY_MATCH_COLORS scene="+scene+" count="+count+" hotSeatBypass="+hotSeat+"; real saved original truck paint/appearance owner, near-duplicate preferences and duplicate names, original lifecycle start/pause/save/nested exception/refresh/reload/setup/rematch/scene exit with exact registry/backup/setup bytes; no synthetic points or control/physics changes. Cheese's12background racers remain intact. Not full match, human or color-vision approval.");
        }
        finally{Time.timeScale=1;if(match!=null)Call(match,"ChangeSetup");if(setupKey!=null)Prefs("DeleteKey",setupKey);Prefs("DeleteKey","TruckSports.Participants.v1");Prefs("DeleteKey","TruckSports.Participants.v1.Backup");Prefs("Save");Registry("ForgetCache");UnityEngine.Object.DestroyImmediate(isolation);prefs.Dispose();}
    }
}
#endif
