using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.InterstateHauler
{
    public interface ILwsSceneStreamerAdapter
    {
        bool IsAvailable { get; }
        string Status { get; }
        int PendingOperationCount { get; }
        event Action<string> SceneLoadRequested;
        event Action<string> SceneLoaded;
        event Action<string> SceneUnloadRequested;
        event Action<string> SceneUnloaded;
        event Action<string, string> SceneOperationFailed;
        bool SetCurrentScene(string sceneName);
        bool RequestLoadScene(string sceneName);
        bool RequestUnloadScene(string sceneName);
        bool IsSceneLoaded(string sceneName);
        bool ClearAll();
    }

    [DisallowMultipleComponent]
    public sealed class LwsSceneStreamerAdapter : MonoBehaviour, ILwsSceneStreamerAdapter
    {
        private const string SceneStreamerTypeName = "PixelCrushers.SceneStreamer.SceneStreamer";

        [SerializeField] private bool logDiagnostics;

        private Type _sceneStreamerType;
        private MethodInfo _explicitVendorLoad;
        private PropertyInfo _vendorInstance;
        private int _pendingOperationCount;
        private readonly HashSet<string> _pendingScenes = new HashSet<string>();

        public bool IsAvailable => ResolveSceneStreamerType() != null;
        public string Status { get; private set; } = "Scene Streamer adapter not initialized.";
        public int PendingOperationCount => _pendingOperationCount;

        public event Action<string> SceneLoadRequested;
        public event Action<string> SceneLoaded;
        public event Action<string> SceneUnloadRequested;
        public event Action<string> SceneUnloaded;
        public event Action<string, string> SceneOperationFailed;

        private void OnEnable()
        {
            ResolveSceneStreamerType();
            SceneManager.sceneLoaded += HandleSceneLoaded;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
            Status = IsAvailable
                ? "Pixel Crushers Scene Streamer is available."
                : "Pixel Crushers Scene Streamer runtime type was not found.";
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
        }

        public bool SetCurrentScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            Type type = ResolveSceneStreamerType();
            MethodInfo method = type?.GetMethod("SetCurrentScene", BindingFlags.Static | BindingFlags.Public);
            if (method == null)
            {
                Fail(sceneName, "SceneStreamer.SetCurrentScene was not found.");
                return false;
            }

            try
            {
                method.Invoke(null, new object[] { sceneName });
                Status = $"Scene Streamer current scene set to {sceneName}.";
                return true;
            }
            catch (Exception ex)
            {
                Fail(sceneName, $"SceneStreamer.SetCurrentScene failed: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        public bool RequestLoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            if (IsSceneLoaded(sceneName))
            {
                SceneLoaded?.Invoke(sceneName);
                return true;
            }

            if (_pendingScenes.Contains(sceneName)) return true;
            if (!IsAvailable || !Application.CanStreamedLevelBeLoaded(sceneName))
            { Fail(sceneName, "Scene Streamer scene is not available in build settings."); return false; }
            SceneLoadRequested?.Invoke(sceneName);
            StartCoroutine(LoadSceneAsync(sceneName));
            return true;
        }

        public bool RequestUnloadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            if (!IsSceneLoaded(sceneName))
            {
                SceneUnloaded?.Invoke(sceneName);
                return true;
            }

            if (_pendingScenes.Contains(sceneName)) return false;
            SceneUnloadRequested?.Invoke(sceneName);
            StartCoroutine(UnloadSceneAsync(sceneName));
            return true;
        }

        public bool IsSceneLoaded(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (scene.IsValid() && scene.isLoaded)
            {
                return true;
            }

            // A Single scene load can unload streamed scenes without clearing the vendor's
            // persistent registry. Only Unity can confirm that their roads/colliders exist.
            return false;
        }

        public bool ClearAll()
        {
            Type type = ResolveSceneStreamerType();
            MethodInfo method = type?.GetMethod("UnloadAll", BindingFlags.Static | BindingFlags.Public);
            if (method == null)
            {
                return false;
            }

            method.Invoke(null, null);
            return true;
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            yield return ObserveVendorOperation(sceneName, true);
        }

        private IEnumerator UnloadSceneAsync(string sceneName)
        {
            yield return ObserveVendorOperation(sceneName, false);
        }

        // The vendor owns the asynchronous scene operation. LWS observes completion only.
        private IEnumerator ObserveVendorOperation(string sceneName, bool loading)
        {
            _pendingScenes.Add(sceneName);
            _pendingOperationCount++;
            bool started = false;
            try
            {
                if (loading) InvokeExplicitVendorLoad(sceneName);
                else
                {
                    var method = ResolveSceneStreamerType()?.GetMethod("UnloadScene", BindingFlags.Public | BindingFlags.Static);
                    if (method == null) throw new MissingMethodException("Scene Streamer unload API unavailable.");
                    method.Invoke(null, new object[] { sceneName });
                }
                started = true;
            }
            catch (Exception ex) { Fail(sceneName, ex.GetBaseException().Message); }
            float deadline = Time.realtimeSinceStartup + 60;
            if (started)
            {
                while (SceneManager.GetSceneByName(sceneName).isLoaded != loading && Time.realtimeSinceStartup < deadline)
                    yield return null;
                // Let Scene Streamer's finish handler update its own registry before another request.
                yield return null;
                if (SceneManager.GetSceneByName(sceneName).isLoaded != loading)
                    Fail(sceneName, "Scene Streamer operation timed out; world preparation remains blocked.");
                else Status = $"Scene Streamer {(loading ? "loaded" : "unloaded")} {sceneName}.";
            }
            _pendingScenes.Remove(sceneName);
            _pendingOperationCount = Mathf.Max(0, _pendingOperationCount - 1);
        }

        private void InvokeExplicitVendorLoad(string sceneName)
        {
            // Scene Streamer 1.26.3's public Load(string) ignores its argument. SetCurrentScene
            // also runs a competing neighbour-unload policy. Use the vendor's exact explicit-load
            // overload until that bug is fixed upstream; do not copy its loader or edit vendor code.
            var type = ResolveSceneStreamerType();
            ReconcileUnloadedWorld(type);
            if (_explicitVendorLoad == null)
            {
                var handler = type?.GetNestedType("InternalLoadedHandler", BindingFlags.NonPublic);
                if (handler != null)
                    _explicitVendorLoad = type.GetMethod("Load", BindingFlags.Instance | BindingFlags.NonPublic,
                        null, new[] { typeof(string), handler, typeof(int) }, null);
                _vendorInstance = type?.GetProperty("instance", BindingFlags.Static | BindingFlags.NonPublic);
            }
            if (_explicitVendorLoad == null || _vendorInstance == null)
                throw new MissingMethodException("Installed Scene Streamer explicit-load compatibility signature changed; review adapter before loading.");
            _explicitVendorLoad.Invoke(_vendorInstance.GetValue(null), new object[] { sceneName, null, 0 });
        }

        private void ReconcileUnloadedWorld(Type type)
        {
            // Reset only a completely unloaded world, never a live or in-flight region set.
            // Empty ApplyState/UnloadAll use vendor APIs without loading or unloading a scene.
            if (_pendingOperationCount > 1) return;
            var record = type?.GetMethod("RecordState", BindingFlags.Public | BindingFlags.Static);
            var apply = type?.GetMethod("ApplyState", BindingFlags.Public | BindingFlags.Static);
            var clear = type?.GetMethod("UnloadAll", BindingFlags.Public | BindingFlags.Static);
            if (record == null || apply == null || clear == null) return;
            object state = record.Invoke(null, null);
            var loadedField = state?.GetType().GetField("loaded");
            var loaded = loadedField?.GetValue(state) as List<string>;
            if (loaded == null || loaded.Count == 0) return;
            foreach (string name in loaded)
                if (IsSceneLoaded(name)) return;

            loaded.Clear();
            state.GetType().GetField("near")?.SetValue(state, new List<string>());
            state.GetType().GetField("current")?.SetValue(state, string.Empty);
            apply.Invoke(null, new[] { state });
            clear.Invoke(null, null);
            if (logDiagnostics)
                Debug.Log("LWS Scene Streamer cleared stale registry after a full world unload.", this);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.IsValid())
            {
                return;
            }

            if (logDiagnostics)
            {
                Debug.Log($"LWS Scene Streamer adapter observed scene load: {scene.name}", this);
            }

            SceneLoaded?.Invoke(scene.name);
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            if (!scene.IsValid())
            {
                return;
            }

            if (logDiagnostics)
            {
                Debug.Log($"LWS Scene Streamer adapter observed scene unload: {scene.name}", this);
            }

            SceneUnloaded?.Invoke(scene.name);
        }

        private void Fail(string sceneName, string message)
        {
            Status = message;
            SceneOperationFailed?.Invoke(sceneName ?? string.Empty, message);
            if (Debug.isDebugBuild)
            {
                Debug.LogWarning(message, this);
            }
        }

        private Type ResolveSceneStreamerType()
        {
            if (_sceneStreamerType != null)
            {
                return _sceneStreamerType;
            }

            // Prefer the installed package when a legacy Assets copy is also imported.
            _sceneStreamerType = Type.GetType($"{SceneStreamerTypeName}, PixelCrushers.SceneStreamer") ?? Type.GetType(SceneStreamerTypeName) ??
                                 Type.GetType($"{SceneStreamerTypeName}, Assembly-CSharp");
            if (_sceneStreamerType != null)
            {
                return _sceneStreamerType;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                _sceneStreamerType = assemblies[i].GetType(SceneStreamerTypeName);
                if (_sceneStreamerType != null)
                {
                    return _sceneStreamerType;
                }
            }

            return null;
        }
    }
}
