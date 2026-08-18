using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsStreamingDebugPanel : MonoBehaviour
    {
        [SerializeField] private bool visible = true;
        [SerializeField] private Rect windowRect = new Rect(12f, 520f, 430f, 360f);
        [SerializeField] private float refreshIntervalSeconds = 0.5f;

        private ILwsWorldStreamingService _streamingService;
        private float _nextRefreshTime;
        private string _summary = "Streaming service not resolved.";
        private List<LwsWorldChunkRuntimeState> _states = new List<LwsWorldChunkRuntimeState>();

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
            {
                visible = !visible;
            }

            if (Time.unscaledTime >= _nextRefreshTime)
            {
                RefreshSnapshot();
            }
        }

        private void OnGUI()
        {
            if (!visible || !Debug.isDebugBuild)
            {
                return;
            }

            windowRect = GUILayout.Window(GetInstanceID(), windowRect, DrawWindow, "LWS Streaming");
        }

        private void DrawWindow(int id)
        {
            GUILayout.Label(_summary);

            if (_streamingService != null)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(_streamingService.IsFrozen ? "Resume" : "Freeze"))
                {
                    _streamingService.SetFrozen(!_streamingService.IsFrozen);
                    RefreshSnapshot();
                }

                if (GUILayout.Button("Load All"))
                {
                    _streamingService.LoadAllChunks();
                    RefreshSnapshot();
                }

                if (GUILayout.Button("Unload Distant"))
                {
                    _streamingService.UnloadDistantChunks();
                    RefreshSnapshot();
                }

                if (GUILayout.Button("Reload Neighborhood"))
                {
                    _streamingService.ReloadCurrentNeighborhood();
                    RefreshSnapshot();
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.Space(4f);
            for (int i = 0; i < _states.Count; i++)
            {
                LwsWorldChunkRuntimeState state = _states[i];
                GUILayout.Label($"{state.chunkId}: {state.state} d={state.distanceMeters:0}m ahead={state.signedAheadDistanceMeters:0}m road={state.registeredRoadPresentation} traffic={state.registeredTrafficPresentation} wxade={state.registeredWeatheradePresentation}");
            }

            GUI.DragWindow();
        }

        private void RefreshSnapshot()
        {
            _nextRefreshTime = Time.unscaledTime + Mathf.Max(0.1f, refreshIntervalSeconds);
            ResolveService();
            if (_streamingService == null)
            {
                _summary = "Streaming service not resolved.";
                _states.Clear();
                return;
            }

            LwsWorldStreamingAnchorState anchor = _streamingService.AnchorState;
            _states = _streamingService.ChunkStates.OrderBy(s => s.signedAheadDistanceMeters).ToList();
            _summary =
                $"World: {_streamingService.WorldId}  Active: {_streamingService.ActiveWorldChunkId}\n" +
                $"Anchor Global: {anchor.TractorPosition.x:0},{anchor.TractorPosition.z:0}  Local: {anchor.TractorLocalPosition.x:0},{anchor.TractorLocalPosition.z:0}\n" +
                $"Speed: {anchor.SpeedMetersPerSecond:0.0}m/s  Trailer: {anchor.HasTrailer}\n" +
                $"Frozen: {_streamingService.IsFrozen}  Load: {_streamingService.LastLoadDurationSeconds:0.000}s  Unload: {_streamingService.LastUnloadDurationSeconds:0.000}s\n" +
                $"Last Error: {_streamingService.LastError}";
        }

        private void ResolveService()
        {
            if (_streamingService != null ||
                LwsApplicationBootstrap.Instance == null ||
                LwsApplicationBootstrap.Instance.Registry == null)
            {
                return;
            }

            LwsApplicationBootstrap.Instance.Registry.TryGet(out _streamingService);
        }
    }
}
