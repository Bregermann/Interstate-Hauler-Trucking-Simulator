using PixelCrushers;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsPixelCrushersSemanticSaver : Saver
    {
        private ILwsSaveService _saveService;

        public void Bind(ILwsSaveService saveService)
        {
            _saveService = saveService;
            ConfigureDefaults();
        }

        public override void Reset()
        {
            base.Reset();
            ConfigureDefaults();
        }

        public override void Awake()
        {
            ConfigureDefaults();
            base.Awake();
        }

        public override void Start()
        {
            ConfigureDefaults();
            ResolveService();
            base.Start();
        }

        public override string RecordData()
        {
            ResolveService();
            if (_saveService == null)
            {
                return string.Empty;
            }

            LwsSaveSnapshot snapshot = _saveService.CaptureSnapshot(_saveService.ActiveProfileId);
            return SaveSystem.Serialize(snapshot);
        }

        public override void ApplyData(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return;
            }

            ResolveService();
            if (_saveService == null)
            {
                Debug.LogWarning("LWS save service is not available for Pixel Crushers payload restore.", this);
                return;
            }

            LwsSaveSnapshot snapshot = SaveSystem.Deserialize<LwsSaveSnapshot>(s);
            if (snapshot == null)
            {
                Debug.LogWarning("Pixel Crushers LWS semantic save payload could not be deserialized.", this);
                return;
            }

            LwsSaveOperationResult result = _saveService.RestoreSnapshot(snapshot);
            if (!result.Succeeded)
            {
                Debug.LogWarning(result.Message, this);
            }
        }

        public override void OnRestartGame()
        {
            ResolveService();
            _saveService?.ClearAllParticipants();
        }

        private void ConfigureDefaults()
        {
            key = LwsSaveSchema.SemanticSnapshotRecordKey;
            appendSaverTypeToKey = false;
            saveAcrossSceneChanges = true;
            skipSaveWhenChangingScenes = true;
            restoreStateOnStart = false;
            order = -250;
        }

        private void ResolveService()
        {
            if (_saveService != null)
            {
                return;
            }

            LwsApplicationBootstrap bootstrap = LwsApplicationBootstrap.Instance;
            if (bootstrap != null &&
                bootstrap.Registry != null &&
                bootstrap.Registry.TryGet(out ILwsSaveService saveService))
            {
                _saveService = saveService;
            }
        }
    }
}
