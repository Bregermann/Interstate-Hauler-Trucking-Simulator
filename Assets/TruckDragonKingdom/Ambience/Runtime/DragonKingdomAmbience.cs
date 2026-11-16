using System;
using System.Linq;
using DigitalRuby.WeatherMaker;
using LWS.TruckSports;
using UnityEngine;
using UnityEngine.AI;

namespace LWS.TruckDragonKingdom.Ambience
{
    [Serializable] public sealed class DragonRegionAmbience
    {
        public string regionId;
        public AudioSource source;
        [Range(0,1)] public float volume=.15f;
        public bool looping=true;
        public float minimumInterval=40;
        [NonSerialized] internal WeatherMakerLoopingAudioSource loop;
        [NonSerialized] internal bool playing;
        [NonSerialized] internal float nextCueAt;
        [NonSerialized] internal bool pausedByLifecycle;
    }
    // Match semantics and bounded visibility only. Unity Navigation, DOTween,
    // existing character rigs and Weather Maker retain their responsibilities.
    [DefaultExecutionOrder(-500)]
    public sealed class DragonKingdomAmbience : MonoBehaviour, IDragonWorldResettable
    {
        public DragonKingdomWorld world;
        public SportsEventSession session;
        public Transform[] observers=Array.Empty<Transform>();
        public DragonAmbientWalker[] walkers=Array.Empty<DragonAmbientWalker>();
        public DragonAmbientBird[] birds=Array.Empty<DragonAmbientBird>();
        public DragonRegionAmbience[] sounds=Array.Empty<DragonRegionAmbience>();
        public NavMeshData[] navigation=Array.Empty<NavMeshData>();
        public float visibilityDistance=280;
        public int maximumAudibleRegions=3;
        public int ResetCount { get; private set; }
        public bool Paused { get; private set; }=true;
        public bool Prepared { get; private set; }
        NavMeshDataInstance[] handles;
        bool explicitPause;
        AudioListener listener;
        float[] distances;
        void OnEnable(){if(Application.isPlaying)EnsureNavigation();}
        void EnsureNavigation()
        {
            if(handles!=null&&handles.Length==navigation.Length)return;
            if(handles!=null)foreach(var handle in handles)if(handle.valid)handle.Remove();
            handles=new NavMeshDataInstance[navigation.Length];
            for(int i=0;i<navigation.Length;i++)if(navigation[i]!=null)handles[i]=NavMesh.AddNavMeshData(navigation[i]);
        }
        // Serialized by explicit authoring or called once by the mode's normal
        // BeginGame. This uses the existing session's Live/Paused authority.
        public void Bind(SportsEventSession owner,Transform[] focus=null)
        {
            if(owner==null)throw new ArgumentNullException(nameof(owner));
            session=owner;
            if(focus!=null)observers=focus;
            listener=owner.GetComponent<SportsMatchAudioAnchor>()?.listener;
            if(listener==null)listener=UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include,FindObjectsSortMode.None).SingleOrDefault(value=>value.gameObject.scene==gameObject.scene);
            if(listener==null)throw new InvalidOperationException("Ambience reuses the existing single Sports AudioListener.");
            EnsureNavigation();InitializeSounds();
        }
        void InitializeSounds()
        {
            distances=new float[sounds.Length];
            foreach(var sound in sounds)
                if(sound.loop==null&&sound.source!=null)sound.loop=new WeatherMakerLoopingAudioSource(sound.source,1.5f,1.5f,sound.looping);
        }
        public void ResetDragonMatch(int seed)
        {
            EnsureNavigation();InitializeSounds();StopAudio();
            foreach(var walker in walkers)walker.ResetForMatch(seed);
            foreach(var bird in birds)bird.ResetForMatch(seed);
            explicitPause=false;Prepared=true;Paused=true;ResetCount++;
        }
        public void SetPaused(bool value){explicitPause=value;ApplyPause(value||session==null||!session.Live);}
        void ApplyPause(bool value)
        {
            if(Paused==value)return;Paused=value;
            if(value)
            {
                foreach(var walker in walkers)walker.SetActivity(false,walker.visuals.Any(renderer=>renderer!=null&&renderer.enabled));
                foreach(var bird in birds)bird.SetActivity(false,bird.visuals.Any(renderer=>renderer!=null&&renderer.enabled));
            }
            // A fading-out source has already lost its logical playing flag,
            // but its DSP playback must still stop at the pause boundary.
            foreach(var sound in sounds)
            {
                if(sound.source==null)continue;
                if(value){sound.pausedByLifecycle=sound.source.isPlaying;if(sound.pausedByLifecycle)sound.source.Pause();}
                else if(sound.pausedByLifecycle){sound.source.UnPause();sound.pausedByLifecycle=false;}
            }
        }
        public void StopAmbience()
        {
            Prepared=false;ApplyPause(true);foreach(var walker in walkers)walker.SetActivity(false,false);foreach(var bird in birds)bird.SetActivity(false,false);StopAudio();
        }
        void Update()
        {
            if(session==null||!Prepared)return;
            if(listener==null)Bind(session);
            bool running=session.Live&&!explicitPause;ApplyPause(!running);
            foreach(var walker in walkers)walker.SetActivity(running,Near(walker.transform.position));
            foreach(var bird in birds)bird.SetActivity(running,Near(bird.transform.position));
            if(running)UpdateAudio();
        }
        bool Near(Vector3 position)
        {
            float limit=visibilityDistance*visibilityDistance;
            foreach(var focus in observers)if(focus!=null&&focus.gameObject.activeInHierarchy&&(focus.position-position).sqrMagnitude<limit)return true;
            if(session.cameras!=null)foreach(var camera in session.cameras)if(camera!=null&&camera.enabled&&(camera.transform.position-position).sqrMagnitude<limit)return true;
            return false;
        }
        void UpdateAudio()
        {
            for(int i=0;i<sounds.Length;i++)distances[i]=sounds[i].source==null?float.PositiveInfinity:(sounds[i].source.transform.position-listener.transform.position).sqrMagnitude;
            for(int i=0;i<sounds.Length;i++)
            {
                var sound=sounds[i];if(sound.source==null||sound.loop==null)continue;
                int nearer=0;for(int k=0;k<sounds.Length;k++)if(distances[k]<distances[i]||distances[k]==distances[i]&&k<i)nearer++;
                bool audible=nearer<maximumAudibleRegions&&distances[i]<sound.source.maxDistance*sound.source.maxDistance;
                sound.loop.VolumeModifier=session.settings!=null?session.settings.sfx:1;
                if(audible&&!sound.playing&&(sound.looping||Time.time>=sound.nextCueAt)){sound.loop.Play(sound.volume);sound.playing=true;}
                else if(!audible&&sound.playing){sound.loop.Stop();sound.playing=false;}
                sound.loop.Update();
                if(!sound.looping&&sound.playing&&!sound.source.isPlaying){sound.playing=false;sound.nextCueAt=Time.time+sound.minimumInterval;}
            }
        }
        void StopAudio(){foreach(var sound in sounds){if(sound.source!=null)sound.source.Stop();sound.playing=sound.pausedByLifecycle=false;sound.nextCueAt=0;}}
        public void ValidateBindings()
        {
            if(world==null||walkers.Length<4||walkers.Length>12||birds.Length<3||birds.Length>12||sounds.Length!=world.regions.Length||navigation.Length>8||maximumAudibleRegions<1||maximumAudibleRegions>4)
                throw new InvalidOperationException("Bounded authored kingdom living ambience is incomplete.");
            foreach(var region in world.regions)if(sounds.Count(sound=>sound.regionId==region.id&&sound.source!=null&&sound.source.clip!=null&&sound.source.spatialBlend==1)!=1)throw new InvalidOperationException("One real spatial ambient source required in "+region.id);
            foreach(var walker in walkers)walker.ValidateBindings();foreach(var bird in birds)bird.ValidateBindings();
        }
        void OnDisable()
        {
            StopAmbience();if(handles!=null){foreach(var handle in handles)if(handle.valid)handle.Remove();handles=null;}
        }
    }
}
