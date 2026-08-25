using UnityEngine;

namespace LWS.InterstateHauler
{
    public interface ILwsDevelopmentUiService : ILwsService
    {
        bool IsVisible { get; }
        bool IsBigMapVisible { get; }
        LwsDevelopmentUiTab ActiveTab { get; }
        LwsDevelopmentUiRoot RuntimeRoot { get; }
        void EnsureRuntime();
        void Show();
        void Hide();
        void Toggle();
        void OpenTab(LwsDevelopmentUiTab tab);
        void ShowBigMap();
        void HideBigMap();
        void ToggleBigMap();
    }

    public sealed class LwsDevelopmentUiService : ILwsDevelopmentUiService
    {
        private LwsServiceRegistry _registry;
        private LwsDevelopmentUiRoot _runtimeRoot;

        public string ServiceId => "lws.development.ui";
        public bool IsVisible => _runtimeRoot != null && _runtimeRoot.ControlCenterVisible;
        public bool IsBigMapVisible => _runtimeRoot != null && _runtimeRoot.BigMapVisible;
        public LwsDevelopmentUiTab ActiveTab => _runtimeRoot != null ? _runtimeRoot.ActiveTab : LwsDevelopmentUiTab.Overview;
        public LwsDevelopmentUiRoot RuntimeRoot => _runtimeRoot;

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            _registry = context.Registry;
            LwsDevelopmentUiDiagnostics.LogStage("Service registered");
            if (Application.isPlaying)
            {
                try
                {
                    EnsureRuntime();
                }
                catch (System.Exception ex)
                {
                    LwsDevelopmentUiDiagnostics.LogFailure("Runtime host created", ex);
                    return LwsServiceResult.Failure($"Development UI runtime creation failed: {ex.Message}");
                }
            }

            return LwsServiceResult.Success("LWS development UI service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            if (_runtimeRoot != null)
            {
                Object.Destroy(_runtimeRoot.gameObject);
                _runtimeRoot = null;
            }

            _registry = null;
            return LwsServiceResult.Success("LWS development UI service shut down.");
        }

        public void EnsureRuntime()
        {
            if (_runtimeRoot != null)
            {
                _runtimeRoot.gameObject.SetActive(true);
                return;
            }

            LwsDevelopmentUiRoot[] existing = Object.FindObjectsByType<LwsDevelopmentUiRoot>(FindObjectsSortMode.None);
            if (existing != null && existing.Length > 0)
            {
                _runtimeRoot = existing[0];
                _runtimeRoot.gameObject.SetActive(true);
                for (int i = 1; i < existing.Length; i++)
                {
                    Object.Destroy(existing[i].gameObject);
                }
            }
            else
            {
                GameObject root = new GameObject("IH Development UI Runtime");
                _runtimeRoot = root.AddComponent<LwsDevelopmentUiRoot>();
            }

            LwsDevelopmentUiDiagnostics.LogStage("Runtime host created");
            _runtimeRoot.BindService(this, _registry);
        }

        public void Show()
        {
            EnsureRuntime();
            _runtimeRoot?.ShowControlCenter();
        }

        public void Hide()
        {
            _runtimeRoot?.HideControlCenter();
        }

        public void Toggle()
        {
            EnsureRuntime();
            _runtimeRoot?.ToggleControlCenter();
        }

        public void OpenTab(LwsDevelopmentUiTab tab)
        {
            EnsureRuntime();
            _runtimeRoot?.OpenTab(tab);
        }

        public void ShowBigMap()
        {
            EnsureRuntime();
            _runtimeRoot?.ShowBigMap();
        }

        public void HideBigMap()
        {
            _runtimeRoot?.HideBigMap();
        }

        public void ToggleBigMap()
        {
            EnsureRuntime();
            _runtimeRoot?.ToggleBigMap();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureDevelopmentRuntimeAfterSceneLoad()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!Application.isPlaying || Object.FindFirstObjectByType<LwsDevelopmentUiRoot>() != null)
            {
                return;
            }

            if (LwsApplicationBootstrap.Instance != null &&
                LwsApplicationBootstrap.Instance.Registry != null &&
                LwsApplicationBootstrap.Instance.Registry.TryGet(out ILwsDevelopmentUiService service))
            {
                service.EnsureRuntime();
                LwsDevelopmentUiDiagnostics.LogHud("Persistent HUD self-healed through registered development UI service.");
                return;
            }

            var root = new GameObject("IH Development UI Runtime");
            root.AddComponent<LwsDevelopmentUiRoot>();
            LwsDevelopmentUiDiagnostics.LogHud("Persistent HUD self-healed without a registered development UI service.");
#endif
        }
    }
}
