#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckBowling;

namespace LWS.TruckFieldGoal
{
    // Explicit development diagnostic: real AI bumper kicks, source timelines
    // and this game's own listener mix only. No microphone/system-audio access.
    public sealed class FieldGoalAudioPlaybackProbe : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-tsFieldAudio") < 0) return;
            var root = new GameObject("Explicit Field Goal audio playback diagnostic"); DontDestroyOnLoad(root);
            root.AddComponent<SportsProbeInputIsolation>(); root.AddComponent<FieldGoalAudioPlaybackProbe>();
        }
        readonly StringBuilder report = new StringBuilder();
        readonly Dictionary<int, bool> playing = new Dictionary<int, bool>();
        FieldGoalMatch match; FieldGoalAudioCapture capture; AudioSource[] sources;
        IDisposable preferences; string folder; int checks, errors, launches, attempts;
        bool observedLaunch, capturedGoal; float next; FieldGoalPhase phase;
        void Check(bool value, string label) { checks++; if (!value) errors++; report.AppendLine((value ? "PASS " : "FAIL ") + label); Flush(); }
        void Flush() { if (folder != null) File.WriteAllText(Path.Combine(folder, "progress.txt"), report.ToString()); }
        void Error(string value, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception) { errors++; report.AppendLine("ERROR " + value + "\n" + stack); Flush(); } }
        IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "-tsFieldAudioOutput");
            bool fullMatch = Array.IndexOf(args, "-tsFieldAudioFull") >= 0;
            folder = at >= 0 && at + 1 < args.Length ? args[at + 1] : SportsEvidencePaths.Folder("FieldAudioPlayback");
            Directory.CreateDirectory(folder); preferences = SportsPrefs.IsolateValidation("FieldAudioPlayback");
            Application.logMessageReceived += Error;
            yield return SportsProbeScene.Ensure<FieldGoalMatch>("TruckFieldGoal"); yield return null;
            match = FindAnyObjectByType<FieldGoalMatch>(); Check(match != null, "real saved Field Goal scene loaded");
            foreach (var preset in match.weatherPresets.entries) if (preset.displayName == "Clear") { match.weatherPresets.Select(preset.id); break; }
            match.AllBots(); match.setup.count = 1; match.option = 0; match.NormalizeSetup();
            match.setup.players[0].difficulty = Array.IndexOf(args, "-tsFieldAudioEasy") >= 0 ? BowlingDifficulty.Easy : BowlingDifficulty.Hard;
            Check(match.StartMatch(), "one AI starts normal five-distance competition through actual NWH driving");
            Time.timeScale = 1;
            capture = FieldGoalAudioCapture.Install(FindAnyObjectByType<AudioListener>());
            sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            float deadline = Time.realtimeSinceStartup + (fullMatch ? 360 : 180); phase = match.Phase;
            while ((fullMatch ? !match.Finished : attempts < 3) && Time.realtimeSinceStartup < deadline && errors == 0)
            {
                if (match.Phase != phase)
                {
                    phase = match.Phase;
                    report.AppendLine("PHASE " + phase + " round=" + match.rules.Round + " clock=" + match.Clock.ToString("F3") + " feedbackPlayed=" + match.feedback.Played + " good=" + match.rules.LastGood);
                    if (phase == FieldGoalPhase.Countdown) observedLaunch = false;
                }
                if (match.football.Launched && !observedLaunch)
                {
                    observedLaunch = true; launches++; if (launches == 1) capture.recording = true;
                    report.AppendLine("KICK " + launches + " bodyPowered=" + match.truck.Powered + " enginePlaying=" + match.truck.engine.isPlaying + " enginePitch=" + match.truck.engine.pitch + " focus=" + Application.isFocused);
                }
                if (Time.realtimeSinceStartup >= next)
                {
                    next = Time.realtimeSinceStartup + .05f;
                    foreach (var source in sources)
                    {
                        if (source == null) continue; int id = source.GetInstanceID(); bool active = source.isPlaying;
                        if (!playing.TryGetValue(id, out bool prior) || active != prior)
                        {
                            playing[id] = active;
                            if (active) report.AppendLine("AUDIO_START phase=" + phase + " role=" + Role(source) + " clip=" + (source.clip == null ? "pooled one-shot" : source.clip.name) + " loop=" + source.loop + " gain=" + source.volume + " pitch=" + source.pitch);
                        }
                    }
                    Flush();
                }
                attempts = 0; foreach (int count in match.rules.attempts) attempts += count;
                if (!capturedGoal && match.rules.LastGood && match.Phase == FieldGoalPhase.Aftermath && match.PhaseAge > .65f)
                {
                    capturedGoal = true;
                    foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
                        SportsControlledCapture.Save(match, size.x, size.y, Path.Combine(folder, "holder-goal-" + size.x + ".png"));
                }
                // Let every aftermath play in real time; skip only recorded review.
                if (match.Phase == FieldGoalPhase.Replay) match.ExtraAction("replayskip");
                if (!fullMatch && attempts >= 3) { yield return new WaitForSecondsRealtime(2); break; }
                yield return null;
            }
            if (fullMatch && match.Finished) yield return new WaitForSecondsRealtime(1.2f);
            capture.recording = false;
            Check(launches >= 3 && attempts >= 3, "three actual bumper kicks and scored/missed attempts observed");
            int frames = capture.Write(Path.Combine(folder, "first-kick-mix.wav"));
            Check(frames > 0, "game listener DSP captured actual non-headless output frames=" + frames);
            report.AppendLine("AUDIO peak=" + capture.Peak + " clippedSamples=" + capture.Clipped + " channels=" + capture.Channels + " sampleRate=" + AudioSettings.outputSampleRate);
            Check(capture.Peak > .00001f, "audio diagnostic has actual non-silent output; no subjective listening claim");
            Check(capture.Clipped == 0 && capture.Peak < .98f, "actual recorded listener mix contains no digital clipping");
            report.AppendLine("RESULT goals=" + match.rules.goals[0] + " attempts=" + attempts + " kicks=" + launches);
            if (fullMatch) Check(match.Finished && match.rules.Complete && attempts == 5, "complete actual five-distance solo competition");
            match.SetupScreen(); capture.RestoreListener(); yield return null;
            Check(!match.flightWind.isPlaying && !match.truck.engine.isPlaying, "return to setup stops flight and engine audio");
            if (fullMatch)
            {
                Check(match.StartMatch() && match.Phase == FieldGoalPhase.Countdown && match.rules.attempts[0] == 0, "rematch restores countdown, holder and attempt state");
                yield return null; match.SetupScreen(); SportsNavigation.FrontEnd(); yield return null; yield return null;
                Check(FindAnyObjectByType<FieldGoalMatch>() == null && FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length == 1, "menu exit cleans Field Goal presentation and listener tap");
            }
            report.AppendLine("CHECKS=" + checks + " ERRORS=" + errors + (fullMatch ? "; actual solo AI competition and audio diagnostic; no human listening or physical-controller acceptance." : "; diagnostic coverage only, not human listening or complete match acceptance."));
            File.WriteAllText(Path.Combine(folder, "result.txt"), report.ToString());
            Application.logMessageReceived -= Error; Application.Quit(errors == 0 ? 0 : 2);
        }
        string Role(AudioSource source)
        {
            if (source == match.flightWind) return "flight-wind";
            if (source == match.atmosphere.ambience) return "weather-ambience";
            if (source == match.truck.engine) return "engine";
            var crowd = match.GetComponent<FieldGoalCrowd>(); if (crowd != null && source == crowd.response) return "crowd";
            return source.gameObject == match.gameObject ? "feedback" : source.name;
        }
        void OnDestroy() { Application.logMessageReceived -= Error; if (capture != null) capture.recording = false; preferences?.Dispose(); Time.timeScale = 1; }
    }

    public sealed class FieldGoalAudioCapture : MonoBehaviour
    {
        public volatile bool recording;
        readonly float[] samples = new float[48000 * 90 * 2];
        volatile int used; int channels;
        AudioListener original;
        public int Channels => channels;
        public float Peak { get; private set; }
        public int Clipped { get; private set; }
        public static FieldGoalAudioCapture Install(AudioListener listener)
        {
            if (listener == null || !listener.isActiveAndEnabled) throw new InvalidOperationException("An active game listener is required for audio capture.");
            var tap = new GameObject("Isolated development game-listener capture"); tap.transform.SetParent(listener.transform, false);
            listener.enabled = false; tap.AddComponent<AudioListener>();
            var capture = tap.AddComponent<FieldGoalAudioCapture>(); capture.original = listener; return capture;
        }
        public void RestoreListener()
        {
            recording = false;
            var tap = GetComponent<AudioListener>(); if (tap != null) tap.enabled = false;
            if (original != null) original.enabled = true;
            original = null;
        }
        void OnDestroy() { RestoreListener(); }
        void OnAudioFilterRead(float[] data, int channelCount)
        {
            if (!recording) return;
            channels = channelCount; int count = Math.Min(data.Length, samples.Length - used);
            if (count <= 0) return;
            Array.Copy(data, 0, samples, used, count); used += count;
        }
        public int Write(string path)
        {
            int count = used; if (channels <= 0) return 0;
            count -= count % channels;
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + count * 2); writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16); writer.Write((short)1); writer.Write((short)channels); writer.Write(AudioSettings.outputSampleRate);
                writer.Write(AudioSettings.outputSampleRate * channels * 2); writer.Write((short)(channels * 2)); writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(count * 2);
                for (int i = 0; i < count; i++) { float value = samples[i]; Peak = Mathf.Max(Peak, Mathf.Abs(value)); if (Mathf.Abs(value) >= .999f) Clipped++; writer.Write((short)(Mathf.Clamp(value, -1, 1) * short.MaxValue)); }
            }
            return count / channels;
        }
    }
}
#endif
