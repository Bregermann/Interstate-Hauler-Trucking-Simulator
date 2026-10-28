using LWS.TruckDanceOff;
using UnityEngine;

namespace LWS.TruckOrchestra
{
    public sealed class OrchestraInstrument : MonoBehaviour
    {
        public int part;
        public string instrumentName;
        public Transform[] movingParts = new Transform[4];
        public AudioSource[] voices = new AudioSource[4];
        public AudioSource errorVoice;
        public AudioClip tone, error;
        public Light accent;
        public LWS.TruckSports.SportsSettings settings;
        public int RealNoteHeads { get; private set; }
        public int VisibleStrikes { get; private set; }
        readonly Quaternion[] homes = new Quaternion[4];
        readonly Vector3[] positions = new Vector3[4];
        public Transform[] strikeTips=new Transform[4],strikeTargets=new Transform[4],resonators=new Transform[4];
        readonly Quaternion[] resonatorHomes=new Quaternion[4];
        readonly double[] struckAt = { -100, -100, -100, -100 };
        bool initialized;
        void EnsureHomes()
        {
            if (initialized) return;
            for (int i = 0; i < 4; i++) { homes[i] = movingParts[i].localRotation; positions[i]=movingParts[i].localPosition; if(resonators[i]!=null)resonatorHomes[i]=resonators[i].localRotation; }
            initialized = true;
        }
        public void Play(int mask, int bar, int key, DanceGrade grade, double hold, double songTime)
        {
            EnsureHomes(); RealNoteHeads++;
            for (int lane = 0; lane < 4; lane++)
            {
                if ((mask & (1 << lane)) == 0) continue;
                struckAt[lane] = songTime; VisibleStrikes++;
                var source = voices[lane]; source.Stop(); source.pitch = Mathf.Pow(2, (OrchestraScore.MidiFor(part, lane, bar, key) - 60) / 12f);
                source.volume = (settings == null ? .7f : settings.music) * .55f;
                source.PlayOneShot(tone);
            }
        }
        public void Mistake()
        {
            if (errorVoice == null || error == null || errorVoice.isPlaying) return;
            errorVoice.volume = (settings == null ? .65f : settings.sfx) * .2f; errorVoice.PlayOneShot(error);
        }
        public void Animate(double clock, double beat)
        {
            EnsureHomes(); float energy = 0;
            for (int i = 0; i < 4; i++)
            {
                float age = (float)(clock - struckAt[i]);
                float amplitude = age >= 0 && age < .45f ? Mathf.Exp(-age * 8) * Mathf.Sin(age * 30 + .5f) : 0;
                movingParts[i].localRotation = homes[i] * Quaternion.Euler(part == 0 ? amplitude * 7 : part == 2 ? 0 : amplitude * 24, 0, 0);
                movingParts[i].localPosition=positions[i]+(part==2?Vector3.down*(Mathf.Abs(amplitude)*.08f):Vector3.zero);
                if(resonators[i]!=null)resonators[i].localRotation=resonatorHomes[i]*Quaternion.Euler(amplitude*4,0,amplitude*2);
                energy = Mathf.Max(energy, Mathf.Abs(amplitude));
            }
            if (accent != null) accent.intensity = 2 + energy * 3;
        }
        public void Pause(bool paused)
        {
            foreach (var voice in voices) if (voice != null) { if (paused) voice.Pause(); else voice.UnPause(); }
            if (errorVoice != null) { if (paused) errorVoice.Pause(); else errorVoice.UnPause(); }
        }
        public void ResetPerformance(bool clearStatistics = true)
        {
            EnsureHomes(); if (clearStatistics) RealNoteHeads = VisibleStrikes = 0;
            for (int i = 0; i < 4; i++) { struckAt[i] = -100; movingParts[i].localRotation = homes[i]; movingParts[i].localPosition=positions[i]; if(resonators[i]!=null)resonators[i].localRotation=resonatorHomes[i]; if (voices[i] != null) voices[i].Stop(); }
            if (errorVoice != null) errorVoice.Stop(); if (accent != null) accent.intensity = 2;
        }
    }
}
