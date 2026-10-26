using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckSkyTow
{
    // Recorded transforms only; the completed match never runs physics again.
    public sealed class SkyTowReview : MonoBehaviour
    {
        public SkyTowMatch match;
        public SportsMotionReplay motion;
        public LineRenderer[] cables;
        public bool Active { get; private set; }
        public int WeatherMetadataRefreshes { get; private set; }
        SportsSnowSurface weatherSurface;
        bool weatherMetadataPending;
        static readonly int[] WeatherSources = { 0, 1, 2, 3, 4, 5, 6, 7 };
        int focus;Renderer[] live;bool[] visible;
        public void ResetRecord()
        {
            Stop(false);
            // Every saved source is sampled, including inactive competitors. A
            // new match may restore/dispose their models or replace equal-shaped
            // models, so neither host lookup nor matching child names proves the
            // old cached Transform instances are still the current hierarchy.
            if (motion.sources == null || motion.ghosts == null || motion.sources.Length != 8 || motion.ghosts.Length != 8 || match.racers == null || match.racers.Length != 4)
                throw new System.InvalidOperationException("Sky Tow fresh recording requires its original eight truck/boat sources.");
            for (int i = 0; i < 4; i++)
                if (match.racers[i] == null || match.racers[i].boat == null || motion.sources[i * 2] != match.racers[i].transform || motion.sources[i * 2 + 1] != match.racers[i].boat.transform)
                    throw new System.InvalidOperationException("Sky Tow fresh recording must retain its original truck/boat source roots.");
            if (!motion.InvalidateFreshRecord() || (!motion.RefreshAppearanceSources(WeatherSources, false) && !motion.RefreshAppearanceSources(WeatherSources, true)))
                throw new System.InvalidOperationException("Sky Tow fresh recording could not bind all eight current source hierarchies.");
            motion.ResetRecord();
            var colors = new Color[8];
            for (int i = 0; i < 4; i++) colors[i * 2] = colors[i * 2 + 1] = match.setup.players[i].color;
            motion.Colorize(colors);
            WeatherMetadataRefreshes = 0; weatherMetadataPending = true;
            if (weatherSurface == null)
                foreach (var candidate in FindObjectsByType<SportsSnowSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (candidate.gameObject.scene == gameObject.scene) { weatherSurface = candidate; break; }
        }
        public void Record(float age)
        {
            // Weatherade adapts live renderer materials in bounded later batches.
            // Copy that final metadata once; recorded poses/times keep advancing.
            if (weatherMetadataPending && (weatherSurface == null || weatherSurface.surface != null && weatherSurface.surface.EnvironmentBindingsReady))
            {
                if (!motion.RefreshAppearanceSources(WeatherSources, false)) throw new System.InvalidOperationException("Sky Tow weather refresh requires the existing unchanged eight-source hierarchy.");
                weatherMetadataPending = false; WeatherMetadataRefreshes++;
            }
            int mask = 0; for (int i = 0; i < match.setup.count; i++) mask |= 3 << (i * 2);
            motion.Record(age, mask);
        }
        public bool Play()
        {
            if (!match.Finished || motion.Duration < .1f) return false;
            focus = 0; for (int i = 0; i < match.setup.count; i++) if (match.rules.winners[i]) { focus = i; break; }
            motion.Begin(Mathf.Max(0, motion.Duration - 18));
            if (!motion.Playing) return false;
            Active = true;
            var renderers = new System.Collections.Generic.List<Renderer>();
            foreach (var racer in match.racers) { renderers.AddRange(racer.GetComponentsInChildren<Renderer>(true)); renderers.AddRange(racer.boat.GetComponentsInChildren<Renderer>(true)); }
            live = renderers.ToArray(); visible = new bool[live.Length];
            for(int i=0;i<live.Length;i++){visible[i]=live[i].enabled;live[i].enabled=false;}
            for (int i = 0; i < match.racers.Length; i++) { match.racers[i].cable.enabled = false; cables[i].enabled = i < match.setup.count; }
            match.menus.Show(SportsEventScreen.Play); return true;
        }
        public void Step(float dt)
        {
            if (!Active) return;
            if (motion.Step(dt)) { Stop(); return; }
            for (int i = 0; i < match.setup.count; i++)
            {
                var truck = motion.ghosts[i * 2]; var boat = motion.ghosts[i * 2 + 1];
                cables[i].SetPosition(0, boat.position + boat.rotation * new Vector3(0, 1, -1));
                cables[i].SetPosition(1, truck.position + truck.rotation * new Vector3(0, 1.25f, .2f));
            }
            match.views.ReplayView(motion.Position(focus * 2) + Vector3.up * 2, motion.Angle);
        }
        public void Action(string key)
        {
            if (key == "towreview") { Play(); return; }
            if (!Active) return;
            if (key == "replayangle") motion.Angle = (motion.Angle + 1) % 3;
            else if (key == "replayspeed") motion.Speed = motion.Speed == 1 ? .35f : motion.Speed < 1 ? 2 : 1;
            else if (key == "replayrewind") motion.Seek(Mathf.Max(0, motion.Duration - 18));
            else if (key == "replayskip") Stop();
        }
        public void Stop(bool showResult = true)
        {
            if (motion != null) motion.End();
            if (!Active) return;
            Active = false;
            if(live!=null)for(int i=0;i<live.Length;i++)if(live[i]!=null)live[i].enabled=visible[i];
            live=null;visible=null;
            for (int i = 0; i < match.racers.Length; i++)
            {
                if (match.racers[i]?.cable != null) match.racers[i].cable.enabled = true;
                if (cables[i] != null) cables[i].enabled = false;
            }
            match.views.Restore(); if (showResult) match.menus.Show(SportsEventScreen.Result);
        }
        void OnDisable() { Stop(false); }
    }
}
