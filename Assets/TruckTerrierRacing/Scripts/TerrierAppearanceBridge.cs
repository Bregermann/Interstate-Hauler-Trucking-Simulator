using System;
using System.Collections.Generic;
using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckTerrierRacing
{
    // Terrier-only rendering on the existing twelve racers and144 wreck bodies.
    // Its hosts are intentionally prepared after the original NewHeat reset,
    // rather than by SportsEventSession before BeginGame.
    public sealed partial class TerrierAppearanceBridge : MonoBehaviour
    {
        [Serializable] public sealed class Binding
        {
            public SportsTruckAppearanceHost host;
            public TerrierWheelPresentation wheels;
            public Transform[] originalParts = Array.Empty<Transform>(), originalGhostParts = Array.Empty<Transform>();
        }
        public TerrierMatch match;
        public Binding[] owners = Array.Empty<Binding>();
        public SportsTruckFragmentRecipe[] recipes = Array.Empty<SportsTruckFragmentRecipe>();
        public SportsTruckAppearanceCabBinding[] cabs = Array.Empty<SportsTruckAppearanceCabBinding>();
        public int PreparedHeats { get; private set; }
        public int ActualEmissions { get; private set; }
        public int PoolBuilds { get; private set; }
        public int WeatherMetadataRefreshes { get; private set; }
        public string LastFallback { get; private set; } = "";
        readonly State[] states = new State[12];
        bool pendingWeatherMetadata;
        sealed class State
        {
            public int revision = -1, snapshotRevision = int.MinValue;
            public Transform snapshot;
            public readonly List<Graphic> graphics = new List<Graphic>();
            public readonly List<GameObject> roots = new List<GameObject>();
            public readonly List<Renderer> sources = new List<Renderer>(), targets = new List<Renderer>();
            public Transform[] wheelSources = new Transform[4], wheelTargets = new Transform[4];
        }
        State Owner(int slot) { if (states[slot] == null) states[slot] = new State(); return states[slot]; }
        public void PrepareHeat()
        {
            if (match.replay.Playing || owners.Length != 12) throw new InvalidOperationException("Prepare Terrier rendering only after its stopped original heat reset.");
            ActualEmissions = WeatherMetadataRefreshes = 0; LastFallback = ""; pendingWeatherMetadata = true;
            for (int slot = 0; slot < 12; slot++)
            {
                var binding = owners[slot]; var host = binding.host; var player = match.setup.players[slot];
                host.Prepare(player.participantId, player.color);
                try
                {
                    if (host.Applied)
                    {
                        if (Owner(slot).revision != host.AppearanceRevision) BuildWrecks(slot);
                        foreach (var graphic in Owner(slot).graphics) { graphic.target.gameObject.SetActive(true); Pose(graphic); CopyGraphicMetadata(graphic); }
                        foreach (var piece in match.wrecks.pieces) if (piece.owner == slot) { piece.renderer.enabled = false; piece.ghostRenderer.enabled = false; }
                    }
                    else ClearWrecks(slot);
                    if (Owner(slot).snapshotRevision != host.AppearanceRevision) BuildIntactSnapshot(slot);
                    RefreshIntactMetadata(slot);
                }
                catch (Exception exception)
                {
                    ClearWrecks(slot); host.UseOriginalForUnavailableFragments(); match.racers[slot].truck.ApplyColor(player.color); ClearIntactSnapshot(slot); BuildIntactSnapshot(slot); RefreshIntactMetadata(slot);
                    LastFallback = exception.Message; Debug.LogWarning("Terrier selected rendering refused; original compatible truck retained: " + exception.Message, this);
                }
            }
            PreparedHeats++;
            foreach (var framing in match.appearanceFraming) if (framing != null) framing.RefreshAppearanceSubjects();
        }
        public void TryRefreshWeatherMetadata()
        {
            if (!pendingWeatherMetadata || match.snow == null || match.snow.surface == null || !match.snow.surface.EnvironmentBindingsReady) return;
            RefreshAllMetadata(); pendingWeatherMetadata = false; WeatherMetadataRefreshes++;
        }
        public void RefreshAllMetadata()
        {
            for (int slot = 0; slot < 12; slot++)
            {
                RefreshIntactMetadata(slot); foreach (var graphic in Owner(slot).graphics) CopyGraphicMetadata(graphic);
            }
        }
        void LateUpdate()
        {
            for (int view = 0; view < cabs.Length; view++)
            {
                SportsTruckAppearanceHost owner = null;
                if (match.Running && match.Phase != TerrierPhase.Replay && view < match.HumanViews)
                { int slot = match.viewSlots[view]; if (slot >= 0 && slot < 12 && match.racers[slot].Alive) owner = owners[slot].host; }
                if (cabs[view] != null) cabs[view].SetOwner(owner);
            }
        }
        static void CopyMetadata(Renderer source, Renderer target)
        {
            target.sharedMaterials = source.sharedMaterials; target.enabled = source.enabled;
            var block = new MaterialPropertyBlock(); source.GetPropertyBlock(block); target.SetPropertyBlock(block);
            for (int material = 0; material < target.sharedMaterials.Length; material++) { block.Clear(); source.GetPropertyBlock(block, material); target.SetPropertyBlock(block.isEmpty ? null : block, material); }
        }
        static void Dispose(UnityEngine.Object value) { if (value == null) return; if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
        void OnDestroy() { for (int slot = 0; slot < 12; slot++) { ClearWrecks(slot); ClearIntactSnapshot(slot); } }
    }
}
