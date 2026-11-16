#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

// Real saved Generic rigs and ordinary public vendor locomotion only. No
// injected clips, Animator.Play, sampled poses, velocities or sound callbacks.
public sealed class DragonWingAudioNativeTests
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    readonly List<GameObject> owned = new List<GameObject>();
    readonly StringBuilder log = new StringBuilder();
    string output;
    float previousMaster;
    bool masterCaptured;
    AudioListener fixtureListener;
    AudioListener[] originalListeners=Array.Empty<AudioListener>();
    bool[] originalListenerEnabled=Array.Empty<bool>();
    bool originalListenerPause;
    readonly List<Component> observedMotors=new List<Component>();
    readonly List<PlaybackTrace> traces = new List<PlaybackTrace>();
    sealed class PlaybackTrace
    {
        public Component wing,motor;
        public AudioSource source;
        public Animator animator;
        public Transform leftHinge,leftTip,rightHinge,rightTip;
        public string phase="ordinary-flight",file;
        public int emissions,playingFrames,advancingSampleFrames,advancingDspFrames;
        public float maximumSourceSignal,maximumMixedSignal;
        public readonly List<string> violations=new List<string>();
        public readonly List<float> cadence=new List<float>();
        struct PoseSample{public float time,fixedTime;public Vector2 height;}
        readonly List<PoseSample> wingHistory=new List<PoseSample>(512);
        readonly float[] sourceSignal=new float[512],mixedSignal=new float[512];
        readonly StringBuilder rows=new StringBuilder("time,realTime,phase,emissions,observedStrokes,leftHeight,rightHeight,playing,samples,volume,master,animation,weight,audio,historySeconds,historyEvaluations,sourcePeak,mixedPeak,dspTime\n");
        int previousEmits,previousSamples,rowsSinceFlush;
        float lastEmission=-1;
        AudioClip previousAudio;
        bool previousPlaying;
        double previousDspTime;
        public void Observe()
        {
            if(wing==null||motor==null||source==null)return;
            Vector3 up=animator.transform.up;
            var height=new Vector2(Vector3.Dot((leftTip.position-leftHinge.position).normalized,up),Vector3.Dot((rightTip.position-rightHinge.position).normalized,up));
            // Actual Animator evaluates in FixedUpdate. At native batch render
            // rates256render frames can span only0.15s and lose the upstroke.
            // Retain4game-seconds of evaluated poses, not rendered frame count.
            var pose=new PoseSample{time=Time.time,fixedTime=Time.fixedTime,height=height};
            bool newEvaluation=wingHistory.Count==0||wingHistory[wingHistory.Count-1].fixedTime!=Time.fixedTime;
            if(newEvaluation)wingHistory.Add(pose);else wingHistory[wingHistory.Count-1]=pose;
            while(wingHistory.Count>1&&wingHistory[0].time<Time.time-4)wingHistory.RemoveAt(0);
            int emitted=(int)Read(wing,"EmittedStrokes"),observed=(int)Read(wing,"ObservedStrokes");
            // A fresh/reset source legitimately has no AudioClip resource.
            // Avoid querying timeSamples until actual playback binds a clip.
            int currentSamples=source.clip!=null?source.timeSamples:0;
            if(source.isPlaying)
            {
                playingFrames++;
                if(previousPlaying&&source.clip==previousAudio&&currentSamples>previousSamples)advancingSampleFrames++;
                if(AudioSettings.dspTime>previousDspTime)advancingDspFrames++;
                if(newEvaluation&&source.volume>0&&AudioListener.volume>0&&!AudioListener.pause)
                {
                    source.GetOutputData(sourceSignal,0);AudioListener.GetOutputData(mixedSignal,0);
                    foreach(float value in sourceSignal)maximumSourceSignal=Mathf.Max(maximumSourceSignal,Mathf.Abs(value));
                    foreach(float value in mixedSignal)maximumMixedSignal=Mathf.Max(maximumMixedSignal,Mathf.Abs(value));
                }
            }
            if(emitted<previousEmits){lastEmission=-1;wingHistory.Clear();wingHistory.Add(pose);}
            if(emitted>previousEmits)
            {
                emissions+=emitted-previousEmits;
                if(emitted-previousEmits!=1)violations.Add("More than one emission between post-LateUpdate observations.");
                if(!source.isPlaying||source.clip==null||source.loop)violations.Add("Emission did not start a real nonlooping AudioSource.");
                var actual=(AnimationClip)Read(wing,"LastEmissionClip");float weight=(float)Read(wing,"LastEmissionWeight"),minimum=(float)Read(wing,"minimumClipWeight");
                if(actual==null||!animator.GetCurrentAnimatorClipInfo(0).Concat(animator.GetNextAnimatorClipInfo(0)).Any(item=>item.clip==actual&&item.weight>=minimum))violations.Add("Emission lacks independently observed actual weighted current/incoming mapped clip.");
                if(weight<minimum)violations.Add("Emission weight is below the unchanged runtime gate.");
                float maxLeft=wingHistory.Max(point=>point.height.x),maxRight=wingHistory.Max(point=>point.height.y),minLeft=wingHistory.Min(point=>point.height.x),minRight=wingHistory.Min(point=>point.height.y);
                float rise=(float)Read(wing,"minimumRise"),departure=(float)Read(wing,"crestDeparture");
                if(maxLeft-minLeft<rise||maxRight-minRight<rise||maxLeft-height.x<departure||maxRight-height.y<departure)violations.Add("Actual post-animation bilateral bone history does not support a real crest/downstroke at emission: time="+Time.time+" actualRange="+(maxLeft-minLeft)+"/"+(maxRight-minRight)+" requiredRise="+rise+" actualDeparture="+(maxLeft-height.x)+"/"+(maxRight-height.y)+" requiredDeparture="+departure+" historySeconds="+(Time.time-wingHistory[0].time)+" evaluatedPoses="+wingHistory.Count);
                if(lastEmission>=0)
                {
                    float interval=Time.time-lastEmission;cadence.Add(interval);
                    if(interval<(float)Read(wing,"cooldown"))violations.Add("Real emission cadence violates unchanged cooldown: "+interval);
                }
                lastEmission=Time.time;
            }
            var clip=(AnimationClip)Read(wing,"LastEmissionClip");
            rows.Append(Time.time.ToString("R",System.Globalization.CultureInfo.InvariantCulture)).Append(',').Append(Time.realtimeSinceStartup.ToString("R",System.Globalization.CultureInfo.InvariantCulture)).Append(',').Append(phase).Append(',').Append(emitted).Append(',').Append(observed).Append(',').Append(height.x.ToString("R",System.Globalization.CultureInfo.InvariantCulture)).Append(',').Append(height.y.ToString("R",System.Globalization.CultureInfo.InvariantCulture)).Append(',').Append(source.isPlaying).Append(',').Append(currentSamples).Append(',').Append(source.volume.ToString("R",System.Globalization.CultureInfo.InvariantCulture)).Append(',').Append(AudioListener.volume.ToString("R",System.Globalization.CultureInfo.InvariantCulture)).Append(',').Append(clip!=null?clip.name:"").Append(',').Append(Read(wing,"LastEmissionWeight")).Append(',').Append(source.clip!=null?source.clip.name:"").Append(',').Append(Time.time-wingHistory[0].time).Append(',').Append(wingHistory.Count).Append(',').Append(maximumSourceSignal).Append(',').Append(maximumMixedSignal).Append(',').Append(AudioSettings.dspTime).AppendLine();
            previousEmits=emitted;previousSamples=currentSamples;previousAudio=source.clip;previousPlaying=source.isPlaying;
            previousDspTime=AudioSettings.dspTime;
            if(++rowsSinceFlush>=60)Flush();
        }
        public void Flush(){File.WriteAllText(file,rows.ToString());rowsSinceFlush=0;}
    }
    static Type Runtime(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(name, false)).First(type => type != null);
    static object Read(object value, string name) => value.GetType().GetField(name, Any) is FieldInfo field ? field.GetValue(value) : value.GetType().GetProperty(name, Any).GetValue(value);
    static object Call(object value, string name, params object[] args) => value.GetType().GetMethods(Any).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(value, args);
    void Log(string message) { log.AppendLine(message); TestContext.WriteLine(message); File.WriteAllText(Path.Combine(output, "observations.txt"), log.ToString()); }
    void Begin(string name)
    {
        previousMaster = AudioListener.volume; masterCaptured = true;
        originalListenerPause=AudioListener.pause;AudioListener.pause=false;
        originalListeners=UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        originalListenerEnabled=originalListeners.Select(listener=>listener.enabled).ToArray();foreach(var listener in originalListeners)listener.enabled=false;
        var listenerOwner=new GameObject("Isolated actual wing mix AudioListener");owned.Add(listenerOwner);fixtureListener=listenerOwner.AddComponent<AudioListener>();
        listenerOwner.AddComponent<DragonWingListenerWitness>().follow=FollowListener;
        Assert.That(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(listener=>listener.isActiveAndEnabled),Is.EqualTo(1));
        string[] args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "-tsDragonOutput");
        output = Path.GetFullPath(Path.Combine(at >= 0 && at + 1 < args.Length ? args[at + 1] : Application.persistentDataPath, "WingAudio", name, DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"))); Directory.CreateDirectory(output); log.Clear();
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "Actual wing-flight physical support"; floor.transform.position = new Vector3(0, -1, 0); floor.transform.localScale = new Vector3(1000, 2, 1000); owned.Add(floor);
    }
    Component Settings()
    {
        var owner = new GameObject("Original shared Sports audio settings"); owned.Add(owner); var settings = owner.AddComponent(Runtime("LWS.TruckSports.SportsSettings"));
        settings.GetType().GetField("sfx").SetValue(settings, .4f); settings.GetType().GetField("master").SetValue(settings, .6f); Call(settings, "ApplyAudio"); return settings;
    }
    void FollowListener()
    {
        if(fixtureListener==null)return;Vector3 center=Vector3.zero;int count=0;
        foreach(var motor in observedMotors)if(motor!=null&&motor.gameObject.activeInHierarchy){center+=motor.transform.position;count++;}
        if(count>0)fixtureListener.transform.position=center/count+Vector3.up*3;
    }
    Component Construct(string id, Vector3 position, Component settings, out Component motor, out object species)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckDragonKingdom/Prefabs/Dragons/" + id + "/" + id + " Dragon Animal.prefab"); Assert.That(prefab, Is.Not.Null);
        var instance = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity); owned.Add(instance);
        motor = instance.GetComponentInChildren(Runtime("LWS.TruckDragonKingdom.DragonAnimalMotor")); var wings = instance.GetComponentInChildren(Runtime("LWS.TruckDragonKingdom.DragonWingAudio"));
        Assert.That(wings, Is.Not.Null, "Run explicit InstallConfiguredWingAudio first."); Call(wings, "ValidateBindings"); Call(wings, "BindAudio", settings);
        var catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/TruckDragonKingdom/Settings/DragonSpeciesCatalog.asset"); var lookup = new object[] { id, null }; Assert.That((bool)Call(catalog, "TryGet", lookup), Is.True); species = lookup[1]; Call(motor, "Prepare", species);
        Assert.That(((Animator)Read(motor,"animator")).updateMode,Is.EqualTo(AnimatorUpdateMode.Fixed),"Real configured controller uses fixed animation evaluations; witness follows those actual evaluations.");observedMotors.Add(motor);FollowListener();
        return wings;
    }
    PlaybackTrace Trace(Component wing,Component motor,string label)
    {
        var trace=new PlaybackTrace{wing=wing,motor=motor,source=(AudioSource)Read(wing,"source"),animator=(Animator)Read(motor,"animator"),leftHinge=(Transform)Read(wing,"leftHinge"),leftTip=(Transform)Read(wing,"leftTip"),rightHinge=(Transform)Read(wing,"rightHinge"),rightTip=(Transform)Read(wing,"rightTip"),file=Path.Combine(output,label+"-actual-playback.csv")};
        var witness=motor.gameObject.AddComponent<DragonWingPlaybackWitness>();witness.observe=trace.Observe;traces.Add(trace);return trace;
    }
    IEnumerator VerifyOwnedWaveforms(Component wings)
    {
        var vendor=(Component)Read(wings,"vendorSounds");var source=(AudioSource)Read(wings,"source");
        Assert.That(Read(vendor,"_audioSource"),Is.SameAs(source));
        var events=((IEnumerable)Read(vendor,"m_EventSound")).Cast<object>().ToArray();
        var named=events.Single(value=>(string)Read(value,"name")=="Owned dragon wing downstroke");
        Assert.That(Read(named,"source"),Is.SameAs(source));
        foreach(var clip in (AudioClip[])Read(named,"Clips"))
        {
            string path=AssetDatabase.GetAssetPath(clip);Assert.That(path,Does.StartWith("Assets/TruckDragonKingdom/PrivateLicensedAudioVfx/MalbersWings/"));
            Assert.That(clip.length,Is.GreaterThan(.4f));Assert.That(clip.samples,Is.GreaterThan(10000));
            // Original Standalone import settings deliberately do not preload
            // and load in background. Decode the actual imported asset, never
            // replace its samples or manufacture a playback notification.
            Assert.That(clip.LoadAudioData(),Is.True);float deadline=Time.realtimeSinceStartup+10;
            while((clip.loadState==AudioDataLoadState.Unloaded||clip.loadState==AudioDataLoadState.Loading)&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(clip.loadState,Is.EqualTo(AudioDataLoadState.Loaded));
            float maximum=0;var samples=new float[Mathf.Min(4096,clip.samples*clip.channels)];
            for(int portion=0;portion<4;portion++)
            {
                Assert.That(clip.GetData(samples,Mathf.Min(clip.samples-1,portion*clip.samples/4)),Is.True,"Read actual imported source waveform, never synthesize samples.");
                foreach(float sample in samples)maximum=Mathf.Max(maximum,Mathf.Abs(sample));
            }
            Assert.That(maximum,Is.GreaterThan(.0001f),"The genuine wing take must contain actual nonzero audio samples.");
            Log("ACTUAL_IMPORTED_WING_AUDIO path="+path+" samples="+clip.samples+" channels="+clip.channels+" frequency="+clip.frequency+" length="+clip.length+" inspectedAbsolutePeak="+maximum+"; decoded imported audio proof only, no listening claim.");
        }
    }
    IEnumerator Fly(Component motor, int frames)
    {
        for (int step = 0; step < frames; step++) { Call(motor, "MoveWorld", new Vector3(.08f, .3f, 1).normalized, false, true); yield return new WaitForFixedUpdate(); yield return null; }
    }
    [UnityTest, Timeout(90000)] public IEnumerator InfernoRealWingDownstrokeAudioHonorsPauseMuteResetAndEmitterReuse() { yield return StrokeLifecycle("dragon-inferno", Vector3.up); }
    [UnityTest, Timeout(90000)] public IEnumerator FireRealWingDownstrokeAudioHonorsPauseMuteResetAndEmitterReuse() { yield return StrokeLifecycle("dragon-fire", Vector3.up * 35); }
    IEnumerator StrokeLifecycle(string id, Vector3 spawn)
    {
        Begin(id); var settings = Settings(); var wings = Construct(id, spawn, settings, out var motor, out var species);
        var source = (AudioSource)Read(wings, "source"); var originalSources = motor.GetComponentsInChildren<AudioSource>(true); int originalObjects = motor.GetComponentsInChildren<Transform>(true).Length;
        yield return VerifyOwnedWaveforms(wings);var trace=Trace(wings,motor,"flight-lifecycle");
        yield return Fly(motor, 430);
        Log("ACTUAL_FLIGHT species=" + id + " flying=" + Read(motor, "IsFlying") + " observedStrokes=" + Read(wings, "ObservedStrokes") + " emitted=" + Read(wings, "EmittedStrokes") + " clip=" + Read(wings, "LastEmissionClip") + " weight=" + Read(wings, "LastEmissionWeight") + " bilateralTravel=" + Read(wings, "LastStrokeExcursion") + " position=" + motor.transform.position);
        Assert.That(Read(motor, "IsFlying"), Is.True); Assert.That((int)Read(wings, "EmittedStrokes"), Is.GreaterThanOrEqualTo(2)); Assert.That((int)Read(wings, "ObservedStrokes"), Is.GreaterThanOrEqualTo((int)Read(wings, "EmittedStrokes")));
        Assert.That((float)Read(wings, "LastEmissionWeight"), Is.GreaterThanOrEqualTo((float)Read(wings, "minimumClipWeight"))); Assert.That((float)Read(wings, "LastStrokeExcursion"), Is.GreaterThanOrEqualTo((float)Read(wings, "minimumRise")));
        CollectionAssert.Contains((AnimationClip[])Read(wings, "flappingClips"), Read(wings, "LastEmissionClip"));
        Assert.That(trace.emissions,Is.GreaterThanOrEqualTo(2));Assert.That(trace.playingFrames,Is.GreaterThan(3));Assert.That(trace.advancingSampleFrames,Is.GreaterThan(3),"Actual source DSP sample positions must advance during genuine wing playback.");Assert.That(trace.cadence.Count,Is.GreaterThan(0));Assert.That(trace.violations,Is.Empty);trace.Flush();
        Assert.That(trace.advancingDspFrames,Is.GreaterThan(3));Log("ACTUAL_SOFTWARE_AUDIO_MIX sourcePeak="+trace.maximumSourceSignal+" mixedPeak="+trace.maximumMixedSignal+" dspAdvances="+trace.advancingDspFrames+" listener="+fixtureListener.transform.position+". Software buffers/DSP, not hardware/listening acceptance.");
        Assert.That(trace.maximumSourceSignal,Is.GreaterThan(.000001f),"Real source DSP output must contain actual owned wing signal.");Assert.That(trace.maximumMixedSignal,Is.GreaterThan(.000001f),"The actual isolated listener mix must contain nonzero owned wing audio.");
        float deadline = Time.realtimeSinceStartup + 6;
        while (!source.isPlaying && Time.realtimeSinceStartup < deadline) yield return Fly(motor, 1);
        Assert.That(source.isPlaying, Is.True); string clipPath = AssetDatabase.GetAssetPath(source.clip);
        Assert.That(clipPath, Does.StartWith("Assets/TruckDragonKingdom/PrivateLicensedAudioVfx/MalbersWings/")); Assert.That(source.spatialBlend, Is.EqualTo(1)); Assert.That(source.loop || source.playOnAwake, Is.False);
        trace.phase="ordinary-pause";Call(motor, "Pause", true); yield return null; int emits = (int)Read(wings, "EmittedStrokes"), sample = source.timeSamples;
        yield return new WaitForSecondsRealtime(.35f); Assert.That(source.isPlaying, Is.False); Assert.That(source.timeSamples, Is.EqualTo(sample)); Assert.That(Read(wings, "EmittedStrokes"), Is.EqualTo(emits));
        trace.phase="ordinary-resume";Call(motor, "Pause", false); yield return Fly(motor, 90);
        trace.phase="shared-sfx-mute";
        settings.GetType().GetField("sfx").SetValue(settings, 0f); yield return null; yield return null; emits = (int)Read(wings, "EmittedStrokes"); yield return Fly(motor, 100);
        Assert.That(source.volume, Is.Zero); Assert.That(Read(wings, "EmittedStrokes"), Is.EqualTo(emits));
        trace.phase="original-listener-master-mute";settings.GetType().GetField("sfx").SetValue(settings, .4f); settings.GetType().GetField("master").SetValue(settings, 0f); Call(settings, "ApplyAudio"); yield return Fly(motor, 80);
        Assert.That(AudioListener.volume, Is.Zero); Assert.That(source.volume, Is.EqualTo((float)Read(wings, "gain") * .4f * (float)Read(wings, "LastEmissionWeight")).Within(.0001f), "SFX applied once at source; original AudioListener owns master.");
        settings.GetType().GetField("master").SetValue(settings, .6f); Call(settings, "ApplyAudio");
        // Same ordinary bind/prepare sequence used for a fresh match/rematch.
        trace.phase="ordinary-bind-prepare-rematch";Call(wings, "BindAudio", settings); Call(motor, "Prepare", species); Assert.That(Read(wings, "EmittedStrokes"), Is.EqualTo(0)); Assert.That(source.isPlaying, Is.False);
        yield return Fly(motor, 220); Assert.That((int)Read(wings, "EmittedStrokes"), Is.GreaterThan(0));
        CollectionAssert.AreEquivalent(originalSources, motor.GetComponentsInChildren<AudioSource>(true)); Assert.That(motor.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(originalObjects));
        motor.gameObject.SetActive(false); Assert.That(source.isPlaying, Is.False); Assert.That(Read(wings, "EmittedStrokes"), Is.EqualTo(0));
        trace.Flush();Assert.That(trace.violations,Is.Empty);
        Log("ACTUAL_CADENCE emissions="+trace.emissions+" sourcePlayingFrames="+trace.playingFrames+" advancingDspSampleFrames="+trace.advancingSampleFrames+" softwareSourcePeak="+trace.maximumSourceSignal+" softwareMixedPeak="+trace.maximumMixedSignal+" intervals="+string.Join(";",trace.cadence.Select(value=>value.ToString("F5")))+" evidence="+trace.file+"; actual post-LateUpdate bones/current-incoming clips/source samples, no fake contact.");
        Log("PASS actual owned clip=" + clipPath + "; actual stroke + weighted mapped clip, paused source, SFX/master authority, ordinary rematch reset and stable emitter count. Native simulation is not listening/Windows/human acceptance.");
    }
    [UnityTest, Timeout(90000)] public IEnumerator FourActualFlyingDragonsNeverExceedThreeWingVoices()
    {
        Begin("bounded-concurrency"); var settings = Settings(); var motors = new List<Component>(); var sources = new List<AudioSource>(); var wings = new List<Component>(); int max = 0, total = 0;
        for (int index = 0; index < 4; index++) { var sound = Construct("dragon-fire", new Vector3(index * 20, 35, 0), settings, out var motor, out _); wings.Add(sound); motors.Add(motor); sources.Add((AudioSource)Read(sound, "source"));Trace(sound,motor,"fire-"+index); }
        for (int step = 0; step < 500; step++)
        {
            foreach (var motor in motors) Call(motor, "MoveWorld", new Vector3(0, .2f, 1).normalized, false, true);
            yield return new WaitForFixedUpdate(); yield return null;
            int playing = sources.Count(source => source.isPlaying); max = Mathf.Max(max, playing); Assert.That(playing, Is.LessThanOrEqualTo(3));
        }
        foreach (var sound in wings) total += (int)Read(sound, "EmittedStrokes");
        Assert.That(total, Is.GreaterThanOrEqualTo(4)); Assert.That(max, Is.GreaterThan(0));
        foreach(var trace in traces){trace.Flush();Assert.That(trace.violations,Is.Empty);}
        Log("PASS four actual flying rigs; maximum concurrent real wing sources=" + max + " total emitted actual strokes=" + total + "; no playback/callback injection or listening acceptance.");
    }
    [TearDown] public void Cleanup()
    {
        foreach(var trace in traces)trace.Flush();traces.Clear();
        for (int index = owned.Count - 1; index >= 0; index--) if (owned[index] != null) UnityEngine.Object.DestroyImmediate(owned[index]); owned.Clear();
        if (masterCaptured) { AudioListener.volume = previousMaster; masterCaptured = false; }
        for(int index=0;index<originalListeners.Length;index++)if(originalListeners[index]!=null)originalListeners[index].enabled=originalListenerEnabled[index];
        AudioListener.pause=originalListenerPause;originalListeners=Array.Empty<AudioListener>();originalListenerEnabled=Array.Empty<bool>();fixtureListener=null;observedMotors.Clear();
    }
}

// Read-only test probe after the production animation/audio LateUpdates. It
// never drives motion, animation, contacts or audio and is destroyed with rig.
[DefaultExecutionOrder(1000)]
public sealed class DragonWingPlaybackWitness : MonoBehaviour
{
    public Action observe;
    void LateUpdate()=>observe?.Invoke();
}
[DefaultExecutionOrder(25)]
public sealed class DragonWingListenerWitness : MonoBehaviour
{
    public Action follow;
    void LateUpdate()=>follow?.Invoke();
}
#endif
