using System;
using System.IO;
using PixelCrushers;
using UnityEngine;

namespace LWS.TruckSports
{
    // A mode-local adapter to the installed vendor storer. It does not load a
    // host save, change scenes, register a second SaveSystem, or write profiles.
    [DisallowMultipleComponent]
    public sealed class SportsWorkshopPersistence : MonoBehaviour
    {
        public MonoBehaviour stateProvider;
        public DiskSavedGameDataStorer storage;
        public string modeId;
        public TMPro.TMP_Text statusLabel;
        public string Status { get; private set; } = "";
        public bool LastOperationSucceeded { get; private set; }
        string RecordKey => "LWS.TruckSports.Workshop.v1." + modeId;

        void Awake() { ConfigureStoragePath(); }

        void ConfigureStoragePath()
        {
            if (storage == null || string.IsNullOrEmpty(modeId) || modeId.Length > 64 ||
                !System.Linq.Enumerable.All(modeId, c => char.IsLetterOrDigit(c) || c == '_')) return;
            storage.storeSaveFilesIn = DiskSavedGameDataStorer.BasePath.Custom;
            // Resolve on this machine at runtime; never serialize an author's
            // absolute LocalLow path into a distributable Unity scene.
            storage.customPath = Path.Combine(Application.persistentDataPath, "TruckSports", "Workshops", modeId);
        }

        public void RequestSave()
        {
            var session = stateProvider as SportsEventSession;
            if (session != null && session.Running && !session.MenuConfirm()) return;
            if (session != null) session.feedback.Play(0, 0, true);
            SaveWorkshop(); if (statusLabel != null) statusLabel.text = Status;
        }

        public void RequestLoad()
        {
            var session = stateProvider as SportsEventSession;
            if (session != null && session.Running && !session.MenuConfirm()) return;
            if (session != null) session.feedback.Play(0, 0, true);
            LoadWorkshop(); if (statusLabel != null) statusLabel.text = Status;
        }

        [Serializable] sealed class Envelope
        {
            public int version = 1;
            public string mode, payload;
        }

        bool Ready(out ISportsWorkshopState state)
        {
            state = stateProvider as ISportsWorkshopState;
            if (state == null || storage == null)
                return Fail("Workshop state and the installed Pixel Crushers disk storer must be assigned.");
            if (string.IsNullOrEmpty(modeId) || modeId.Length > 64 ||
                !System.Linq.Enumerable.All(modeId, c => char.IsLetterOrDigit(c) || c == '_'))
                return Fail("Workshop mode ID must be a stable alphanumeric ID, independent of player names.");
            if (string.IsNullOrEmpty(storage.customPath)) ConfigureStoragePath();
            // A production workshop can never use the host's normal save folder.
            string expected = Path.GetFullPath(Path.Combine(Application.persistentDataPath, "TruckSports", "Workshops", modeId));
            if (storage.storeSaveFilesIn != DiskSavedGameDataStorer.BasePath.Custom ||
                string.IsNullOrEmpty(storage.customPath) ||
                !string.Equals(Path.GetFullPath(storage.customPath), expected, StringComparison.OrdinalIgnoreCase))
                return Fail("Workshop storage must use its own TruckSports/Workshops/" + modeId + " directory.");
            return true;
        }

        bool Fail(string reason) { LastOperationSucceeded = false; Status = reason; return false; }

        public bool SaveWorkshop()
        {
            if (!Ready(out var state)) return false;
            try
            {
                // Providers reject live/unstable construction before any write.
                string payload = state.CaptureWorkshopJson();
                if (string.IsNullOrWhiteSpace(payload)) return Fail("Workshop snapshot is empty.");
                var envelope = JsonUtility.ToJson(new Envelope { mode = modeId, payload = payload });
                var data = new SavedGameData { version = 1, sceneName = modeId };
                data.SetData(RecordKey, SaveSystem.NoSceneIndex, envelope);
                Directory.CreateDirectory(storage.customPath);
                storage.StoreSavedGameData(0, data);
                var saved = storage.RetrieveSavedGameData(0);
                if (saved == null || saved.GetData(RecordKey) != envelope)
                    return Fail("The vendor storer could not verify the saved workshop.");
                LastOperationSucceeded = true; Status = "WORKSHOP SAVED"; return true;
            }
            catch (Exception e) { return Fail("Workshop save failed: " + e.Message); }
        }

        public bool LoadWorkshop()
        {
            if (!Ready(out var state)) return false;
            try
            {
                var data = storage.RetrieveSavedGameData(0);
                if (data == null || data.version != 1 || data.sceneName != modeId)
                    return Fail("No compatible saved workshop exists for this sport.");
                string encoded = data.GetData(RecordKey);
                if (string.IsNullOrEmpty(encoded)) return Fail("The saved workshop record is missing.");
                var envelope = JsonUtility.FromJson<Envelope>(encoded);
                if (envelope == null || envelope.version != 1 || envelope.mode != modeId ||
                    string.IsNullOrWhiteSpace(envelope.payload))
                    return Fail("The workshop record has an incompatible mode or version.");
                // Each mode validates every identity and physical state before
                // applying the snapshot. This adapter never changes the crew.
                if (!state.TryRestoreWorkshopJson(envelope.payload, out string reason)) return Fail(reason);
                LastOperationSucceeded = true; Status = "WORKSHOP RESTORED · RESUME WHEN READY"; return true;
            }
            catch (Exception e) { return Fail("Workshop restore failed: " + e.Message); }
        }
    }
}
