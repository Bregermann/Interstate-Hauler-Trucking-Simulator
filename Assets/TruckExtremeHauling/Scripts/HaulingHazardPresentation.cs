using System;
using UnityEngine;
using LWS.TruckSports;

namespace LWS.TruckExtremeHauling
{
    /// <summary>Read-only presentation of actual ice/load/winch states. Owns no physics or scores.</summary>
    public sealed class HaulingHazardPresentation : MonoBehaviour
    {
        public ExtremeHaulingMatch match;
        public ThinIceMatch thinIce;
        public CargoCrisisMatch cargo;
        public LineRenderer[] crackLines = Array.Empty<LineRenderer>();
        public ParticleSystem[] iceChips = Array.Empty<ParticleSystem>();
        public SportsWaterInteraction waterEffects;
        public LWS.TruckBogSnorkelling.BogCameraEffects[] selectedWaterCameraEffects = Array.Empty<LWS.TruckBogSnorkelling.BogCameraEffects>();
        public int stressClip = -1, releaseClip = -1, winchClip = -1, heatAlarmClip = -1;
        public float warningCooldown = 1.4f, hotZoneCooldown = 7;
        public int ActualBreakBursts { get; private set; }
        public int StressSounds { get; private set; }
        public int MachinerySounds { get; private set; }
        public int HotZoneSounds { get; private set; }
        public int ActualWaterEntries { get; private set; }
        string activeMatch;
        byte[] warningStage = Array.Empty<byte>();
        bool[] broke = Array.Empty<bool>();
        readonly SpringJoint[] cachedWinches = new SpringJoint[4];
        readonly float[] priorCableLength = new float[4], nextWinch = new float[4], nextHot = new float[4];
        float nextStress, nextPoll;
        int chipPool;
        bool subscribed;

        void Start()
        {
            if (thinIce != null && thinIce.ice != null)
            {
                warningStage = new byte[thinIce.ice.Length]; broke = new bool[thinIce.ice.Length];
                foreach (var tile in thinIce.ice) if (tile != null) tile.Breakthrough += OnActualBreakthrough;
            }
            if (waterEffects != null) waterEffects.SurfaceEntry += OnActualWaterEntry;
            subscribed = true; HidePresentation();
        }
        void Update()
        {
            if (match == null || !match.Live)
            {
                WaterPresentation(false);
                HidePresentation(); return;
            }
            if (activeMatch != match.MatchId) ResetMatch();
            WaterPresentation(thinIce != null ? thinIce.option == 3 : cargo != null && cargo.option == 5);
            if (Time.time < nextPoll) return;
            nextPoll = Time.time + .1f;
            if (thinIce != null) PresentIce();
            PresentMachinery();
        }
        void ResetMatch()
        {
            activeMatch = match.MatchId; ActualBreakBursts = StressSounds = MachinerySounds = HotZoneSounds = ActualWaterEntries = 0;
            nextStress = nextPoll = 0; chipPool = 0; Array.Clear(warningStage, 0, warningStage.Length); Array.Clear(broke, 0, broke.Length);
            Array.Clear(nextWinch, 0, 4); Array.Clear(nextHot, 0, 4);
            for (int i = 0; i < 4; i++)
            {
                cachedWinches[i] = cargo != null && cargo.winches != null && i < cargo.winches.Length ? cargo.winches[i] : match.trucks[i].GetComponent<SpringJoint>();
                priorCableLength[i] = cachedWinches[i] != null ? cachedWinches[i].maxDistance : 0;
            }
            HidePresentation();
        }
        void PresentIce()
        {
            for (int i = 0; i < thinIce.ice.Length; i++)
            {
                var tile = thinIce.ice[i]; if (tile == null) continue;
                float stress = tile.Stress;
                for (int line = i * 2; line < Mathf.Min(crackLines.Length, i * 2 + 2); line++)
                {
                    var crack = crackLines[line]; if (crack == null) continue;
                    crack.enabled = !tile.Broken && stress >= .18f;
                    crack.widthMultiplier = Mathf.Lerp(.015f, .17f, stress);
                }
                byte stage = stress >= .72f ? (byte)2 : stress >= .4f ? (byte)1 : (byte)0;
                if (!tile.Broken && stage > warningStage[i] && Time.time >= nextStress && NearParticipant(tile.transform.position, 65))
                {
                    // An owned suspension-load thump warns of actual increasing stress;
                    // it is not represented as a dedicated authentic ice-crack recording.
                    if (Play(stressClip, tile.transform.position, 7)) StressSounds++;
                    nextStress = Time.time + warningCooldown;
                }
                warningStage[i] = Math.Max(warningStage[i], stage);
            }
        }
        void OnActualBreakthrough(LoadSensitiveIce tile)
        {
            if (match == null || !match.Live) return;
            if (activeMatch != match.MatchId) ResetMatch();
            int index = Array.IndexOf(thinIce.ice, tile);
            if (index < 0 || broke[index]) return;
            broke[index] = true;
            for (int line = index * 2; line < Mathf.Min(crackLines.Length, index * 2 + 2); line++) if (crackLines[line] != null) crackLines[line].enabled = false;
            if (iceChips.Length > 0)
            {
                var particles = iceChips[chipPool++ % iceChips.Length];
                particles.transform.position = tile.transform.position + Vector3.up * .4f;
                particles.Emit(65); ActualBreakBursts++;
            }
            if (NearParticipant(tile.transform.position, 90)) Play(releaseClip, tile.transform.position, 12);
        }
        void PresentMachinery()
        {
            for (int i = 0; i < match.setup.count; i++)
            {
                if (cachedWinches[i] == null) cachedWinches[i] = match.trucks[i].GetComponent<SpringJoint>();
                var cable = cachedWinches[i];
                if (cable != null)
                {
                    bool realReeling = cable.connectedBody != null && cable.currentForce.sqrMagnitude > 10000 && cable.maxDistance < priorCableLength[i] - .001f;
                    if (realReeling && Time.time >= nextWinch[i])
                    {
                        if (Play(winchClip, match.trucks[i].transform.TransformPoint(cable.anchor), 8)) MachinerySounds++;
                        nextWinch[i] = Time.time + Mathf.Max(3.4f, ClipSeconds(winchClip) + .15f);
                    }
                    priorCableLength[i] = cable.maxDistance;
                }
                if (cargo != null && cargo.emergencySites != null && cargo.emergencySites.Active != null && !cargo.emergencySites.FloodActive && i < cargo.trailers.Length
                    && cargo.emergencySites.HeatAt(cargo.trailers[i].Body.worldCenterOfMass) > .05f && Time.time >= nextHot[i])
                {
                    if (Play(heatAlarmClip, cargo.trailers[i].Body.worldCenterOfMass, 0, true)) HotZoneSounds++;
                    nextHot[i] = Time.time + hotZoneCooldown;
                }
            }
        }
        void OnActualWaterEntry(Vector3 point, Vector3 velocity, float mass, bool playback)
        {
            if (playback || match == null || !match.Live) return;
            if (activeMatch != match.MatchId) ResetMatch();
            ActualWaterEntries++;
        }
        bool NearParticipant(Vector3 point, float distance)
        {
            foreach (var truck in match.trucks) if (truck != null && truck.gameObject.activeInHierarchy && Vector3.SqrMagnitude(truck.body.position - point) <= distance * distance) return true;
            return false;
        }
        bool Play(int clip, Vector3 point, float speed, bool cue = false)
        {
            if (match.feedback == null || clip < 0) return false;
            int before = match.feedback.SpatialPlayed; match.feedback.PlayAt(clip, point, speed, cue); return match.feedback.SpatialPlayed > before;
        }
        float ClipSeconds(int index) => match.feedback != null && match.feedback.clips != null && index >= 0 && index < match.feedback.clips.Length && match.feedback.clips[index] != null ? match.feedback.clips[index].length : 0;
        void WaterPresentation(bool enabled)
        {
            if(waterEffects!=null)waterEffects.enabled=enabled;
            foreach(var cameraEffect in selectedWaterCameraEffects)if(cameraEffect!=null)cameraEffect.enabled=enabled;
        }
        void HidePresentation()
        {
            foreach (var line in crackLines) if (line != null && line.enabled) line.enabled = false;
            foreach (var particles in iceChips) if (particles != null && particles.particleCount > 0) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        void OnDestroy()
        {
            if (!subscribed) return;
            if (thinIce != null && thinIce.ice != null) foreach (var tile in thinIce.ice) if (tile != null) tile.Breakthrough -= OnActualBreakthrough;
            if (waterEffects != null) waterEffects.SurfaceEntry -= OnActualWaterEntry;
        }
    }
}
