using System;
using System.Collections;
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
        private int _pendingOperationCount;

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

            Type type = ResolveSceneStreamerType();
            MethodInfo method = type?.GetMethod("IsSceneLoaded", BindingFlags.Static | BindingFlags.Public);
            if (method == null)
            {
                return false;
            }

            try
            {
                return method.Invoke(null, new object[] { sceneName }) is bool loaded && loaded;
            }
            catch
            {
                return false;
            }
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
            _pendingOperationCount++;
            AsyncOperation operation = null;
            try
            {
                operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            }
            catch (Exception ex)
            {
                Fail(sceneName, $"LoadSceneAsync threw {ex.GetType().Name}: {ex.Message}");
            }

            if (operation == null)
            {
                _pendingOperationCount = Mathf.Max(0, _pendingOperationCount - 1);
                yield break;
            }

            while (!operation.isDone)
            {
                yield return null;
            }

            _pendingOperationCount = Mathf.Max(0, _pendingOperationCount - 1);
            Status = $"Loaded additive scene {sceneName}.";
        }

        private IEnumerator UnloadSceneAsync(string sceneName)
        {
            _pendingOperationCount++;
            AsyncOperation operation = null;
            try
            {
                operation = SceneManager.UnloadSceneAsync(sceneName);
            }
            catch (Exception ex)
            {
                Fail(sceneName, $"UnloadSceneAsync threw {ex.GetType().Name}: {ex.Message}");
            }

            if (operation == null)
            {
                _pendingOperationCount = Mathf.Max(0, _pendingOperationCount - 1);
                yield break;
            }

            while (!operation.isDone)
            {
                yield return null;
            }

            _pendingOperationCount = Mathf.Max(0, _pendingOperationCount - 1);
            Status = $"Unloaded additive scene {sceneName}.";
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

            _sceneStreamerType = Type.GetType(SceneStreamerTypeName) ??
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
