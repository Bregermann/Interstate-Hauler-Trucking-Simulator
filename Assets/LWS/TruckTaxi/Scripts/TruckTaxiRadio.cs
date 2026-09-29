using System;
using System.Collections.Generic;
using NWH.Common.Cameras;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiRadio : MonoBehaviour
    {
        private const string Prefix = "LWS.TruckTaxi.Radio.";
        private TruckTaxiBootstrap host;
        private TruckTaxiRadioCatalog catalog;
        private AudioSource music, bulletinAudio;
        private AudioLowPassFilter filter;
        private readonly Queue<string> bulletins = new Queue<string>();
        private readonly Dictionary<string,int> stationTrack = new Dictionary<string,int>();
        private readonly Dictionary<string,int> stationSample = new Dictionary<string,int>();
        private int stationIndex, trackIndex = -1;
        private float bulletinUntil, cardUntil, cameraCheck;
        private bool inside, playingTrack, trackStarted;
        public bool IsOn { get; private set; }
        public bool Muted { get; private set; }
        public float Volume { get; private set; } = .65f;
        public bool MixerRoutingPending { get; private set; }
        public string Bulletin { get; private set; } = "";
        public TruckTaxiRadioStation Station => catalog != null && catalog.stations.Length > 0 ? catalog.stations[stationIndex] : null;
        public string NowPlaying => Station != null && trackIndex >= 0 && trackIndex < Station.playlist.Length ? Station.playlist[trackIndex].title : "NO AUTHORED PROGRAM";
        public string Display => !IsOn ? "RADIO OFF" : Station == null ? "RADIO CONTENT NOT ASSIGNED" :
            Station.frequency + " " + Station.displayName + "\n" + (Bulletin.Length > 0 ? Bulletin : NowPlaying);
        public bool CardVisible => Time.unscaledTime < cardUntil || Bulletin.Length > 0;
        public float PlaybackSeconds => music != null ? music.time : 0;
        public event Action Changed;

        public void Initialize(TruckTaxiBootstrap value)
        {
            if(host != null) return;
            host=value; catalog=Resources.Load<TruckTaxiRadioCatalog>("TruckTaxi/TruckTaxiRadioCatalog");
            music=Source("Radio program"); bulletinAudio=Source("Radio bulletin");
            filter=music.gameObject.AddComponent<AudioLowPassFilter>();
            var bus = host.Configuration.audio?.Find(TruckTaxiAudioCategory.Radio);
            MixerRoutingPending=bus?.group==null;
            host.Audio.Route(music,MixerRoutingPending ? TruckTaxiAudioCategory.Master : TruckTaxiAudioCategory.Radio);
            host.Audio.Route(bulletinAudio,MixerRoutingPending ? TruckTaxiAudioCategory.Master : TruckTaxiAudioCategory.Radio);
            if(MixerRoutingPending) Debug.LogWarning("Radio group / RadioVolume authoring pending. Radio uses Master with its own source volume until configured.",this);
            IsOn=PlayerPrefs.GetInt(Prefix+"On",1)!=0;
            Volume=Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix+"Volume",.65f));
            string saved=PlayerPrefs.GetString(Prefix+"Station","");
            if(catalog!=null) { int i=Array.FindIndex(catalog.stations,s=>s.stableId==saved); if(i>=0) stationIndex=i; }
            host.hud.Alerts.Published+=OnAlert;
            SelectTrack(0); ApplyVolume();
        }
        private AudioSource Source(string name)
        {
            var go=new GameObject(name); go.transform.SetParent(transform,false);
            var source=go.AddComponent<AudioSource>(); source.playOnAwake=false; source.loop=false;
            source.spatialBlend=0; source.ignoreListenerPause=true; return source;
        }
        public void Toggle()
        {
            IsOn=!IsOn;
            if(IsOn)
            {
                if(playingTrack && trackStarted) music.UnPause();
                else if(playingTrack) { int sample=music.timeSamples; music.Play(); music.timeSamples=sample; trackStarted=true; }
                else SelectTrack(Mathf.Max(0,trackIndex));
            }
            else { music.Pause(); bulletinAudio.Stop(); Bulletin=""; }
            Save(); Announce();
        }
        public void ToggleMute() { Muted=!Muted; ApplyVolume(); Announce(); }
        public void SetVolume(float value) { Volume=Mathf.Clamp01(value); ApplyVolume(); Save(); Changed?.Invoke(); }
        public void ChangeStation(int delta)
        {
            if(catalog==null || catalog.stations.Length==0) return;
            if(Station!=null) { stationTrack[Station.stableId]=trackIndex; stationSample[Station.stableId]=music.timeSamples; }
            stationIndex=(stationIndex+delta%catalog.stations.Length+catalog.stations.Length)%catalog.stations.Length;
            Bulletin=""; bulletinAudio.Stop(); bulletins.Clear();
            SelectTrack(stationTrack.TryGetValue(Station.stableId,out var track) ? track : 0);
            if(music.clip!=null && stationSample.TryGetValue(Station.stableId,out var sample)) music.timeSamples=Mathf.Clamp(sample,0,music.clip.samples-1);
            if(IsOn && Station.jingle!=null) bulletinAudio.PlayOneShot(Station.jingle);
            Save(); Announce();
            var passenger=host.Session.HasPassenger ? host.Session.Passenger : null;
            if(passenger!=null)
            {
                bool likes=Array.IndexOf(passenger.likedRadioStationIds ?? Array.Empty<string>(),Station.stableId)>=0;
                bool dislikes=Array.IndexOf(passenger.dislikedRadioStationIds ?? Array.Empty<string>(),Station.stableId)>=0;
                if(likes || dislikes) host.Passengers?.Dialogue?.Speak(passenger,TruckTaxiDialogueCategory.RadioReaction,host.Session,
                    likes ? passenger.likedRadioReaction : passenger.dislikedRadioReaction,5);
            }
        }
        public void NextTrack()
        {
            SelectTrack(trackIndex+1);
            if(Bulletin.Length>0) music.Pause();
            Announce();
        }
        private void SelectTrack(int desired)
        {
            music.Stop(); playingTrack=false; trackStarted=false;
            var station=Station;
            if(station==null || station.playlist.Length==0) { trackIndex=-1; return; }
            int hour=host.CalendarWeather?.CurrentDateTime.Hour ?? 12;
            for(int n=0;n<station.playlist.Length;n++)
            {
                int i=(Mathf.Max(0,desired)+n)%station.playlist.Length;
                var track=station.playlist[i];
                if(track==null || track.clip==null || !track.Available(hour)) continue;
                trackIndex=i; music.clip=track.clip; playingTrack=true;
                if(IsOn) { music.Play(); trackStarted=true; } return;
            }
            trackIndex=-1; music.clip=null;
        }
        public void EnqueueBulletin(string text)
        {
            if(string.IsNullOrWhiteSpace(text) || Bulletin==text || bulletins.Contains(text) || bulletins.Count>=8) return;
            bulletins.Enqueue(text);
        }
        public void WeatherBulletin()
        {
            if(host.CalendarWeather==null) return;
            var weather=host.CalendarWeather;
            string next="";
            foreach(var entry in weather.Forecast)
                if(entry.Start>weather.CurrentDateTime && entry.Condition!=weather.CurrentCondition)
                { next=" Next: "+entry.Condition+" at "+entry.Start.ToString("ddd HH:mm")+"."; break; }
            EnqueueBulletin("ROAD REPORT: "+weather.CurrentDateTime.ToString("ddd HH:mm")+". "+weather.CurrentCondition+"."+next);
        }
        private void OnAlert(string text,TruckTaxiAlertPriority priority)
        { if(priority>=TruckTaxiAlertPriority.High || text.StartsWith("Thunder Bowl:",StringComparison.Ordinal)) EnqueueBulletin(text); }
        private void Update()
        {
            if(host==null) return;
            var input=host.hud.UIInput;
            if(input.RadioToggle.WasPressedThisFrame()) Toggle();
            if(input.RadioNext.WasPressedThisFrame()) ChangeStation(1);
            if(input.RadioPrevious.WasPressedThisFrame()) ChangeStation(-1);
            if(input.RadioMute.WasPressedThisFrame()) ToggleMute();
            if(input.RadioVolumeUp.WasPressedThisFrame()) SetVolume(Volume+.05f);
            if(input.RadioVolumeDown.WasPressedThisFrame()) SetVolume(Volume-.05f);
            if(Time.unscaledTime>=cameraCheck)
            {
                cameraCheck=Time.unscaledTime+.25f;
                var camera=Camera.main;
                var context=camera!=null ? camera.GetComponent<CameraInsideVehicle>() : null;
                inside=context!=null && context.isInsideVehicle;
                filter.cutoffFrequency=inside ? 22000 : 1200; ApplyVolume();
            }
            if(!IsOn) return;
            if(Bulletin.Length>0 && Time.unscaledTime>=bulletinUntil)
            { Bulletin=""; music.UnPause(); Changed?.Invoke(); }
            if(Bulletin.Length==0 && bulletins.Count>0 && Station?.allowEmergencyInterrupt==true)
            {
                Bulletin=bulletins.Dequeue(); music.Pause();
                bulletinUntil=Time.unscaledTime+Mathf.Clamp(4+Bulletin.Length*.045f,6,18);
                if(catalog.alertTone!=null) bulletinAudio.PlayOneShot(catalog.alertTone);
                Announce();
            }
            if(Bulletin.Length==0 && playingTrack && !music.isPlaying) SelectTrack(Station.shuffle ? UnityEngine.Random.Range(0,Station.playlist.Length) : trackIndex+1);
        }
        private void ApplyVolume()
        {
            float volume=Muted ? 0 : Volume*(inside ? 1 : .18f);
            if(music!=null) music.volume=volume;
            if(bulletinAudio!=null) bulletinAudio.volume=volume;
        }
        private void Save()
        { PlayerPrefs.SetInt(Prefix+"On",IsOn ? 1 : 0); PlayerPrefs.SetFloat(Prefix+"Volume",Volume); PlayerPrefs.SetString(Prefix+"Station",Station?.stableId ?? ""); }
        private void Announce() { cardUntil=Time.unscaledTime+6; Changed?.Invoke(); }
        private void OnDestroy()
        { if(host?.hud?.Alerts!=null) host.hud.Alerts.Published-=OnAlert; Save(); }
    }
}
