using System;
using System.IO;
using System.Linq;
using PixelCrushers;
using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckPetPals
{
    // Semantic records, using the SAME configured vendor storer as the strict
    // world snapshot. Slot0 belongs to SportsWorkshopPersistence; slot1 is pets.
    // No second SaveSystem, alternate folder, PlayerPrefs or display-name keys.
    public sealed class PetProfileRepository : MonoBehaviour
    {
        public const int ReservedSlot = 1;
        public const string RecordKey = "LWS.TruckSports.PetPals.Profiles.v1";
        const string DocumentId = "TruckPetPalsPetProfiles";
        public SportsWorkshopPersistence workshop;
        public PetArchive Archive { get; private set; } = new PetArchive { version = 1, profiles = Array.Empty<PetProfileRecord>() };
        public bool ReadOnly { get; private set; }
        public bool Loaded { get; private set; }
        public bool Dirty { get; private set; }
        public string Status { get; private set; } = "";
        float quietUntil, nextWrite;
        [Serializable] sealed class VersionHeader { public int version; }
        bool Ready(out string reason)
        {
            reason = "";
            if (workshop == null || workshop.storage == null || string.IsNullOrEmpty(workshop.modeId) || workshop.modeId.Length > 64 || !workshop.modeId.All(c => char.IsLetterOrDigit(c) || c == '_')) { reason = "The existing configured workshop storer is required."; return false; }
            string expected = Path.GetFullPath(Path.Combine(Application.persistentDataPath, "TruckSports", "Workshops", workshop.modeId));
            if (workshop.storage.storeSaveFilesIn != DiskSavedGameDataStorer.BasePath.Custom || string.IsNullOrEmpty(workshop.storage.customPath) || !string.Equals(expected, Path.GetFullPath(workshop.storage.customPath), StringComparison.OrdinalIgnoreCase)) { reason = "Pet records must share the workshop's isolated mode directory."; return false; }
            return true;
        }
        void Start() { if (!Loaded) LoadProfiles(); }
        public bool LoadProfiles()
        {
            if (!Ready(out string reason)) { Status = reason; return false; }
            try
            {
                var data = workshop.storage.RetrieveSavedGameData(ReservedSlot);
                bool existing = File.Exists(workshop.storage.GetSaveGameFilename(ReservedSlot));
                if (data == null && !existing) { Loaded = true; Status = "NO SAVED COMPANIONS YET"; return true; }
                if (data == null || data.version != 1 || data.sceneName != DocumentId) return Preserve("Unreadable or incompatible pet save retained.");
                return TryApplyArchiveJson(data.GetData(RecordKey), out reason) || Preserve(reason);
            }
            catch (Exception e) { return Preserve("Pet save could not be read; original retained: " + e.Message); }
        }
        bool Preserve(string reason) { ReadOnly = true; Loaded = true; Dirty = false; Status = reason + " Saving disabled to preserve the original."; return false; }
        public bool TryApplyArchiveJson(string json, out string reason)
        {
            reason = "";
            if (string.IsNullOrWhiteSpace(json) || json.Length > 4 * 1024 * 1024) { reason = "Missing/oversized pet record."; return false; }
            try
            {
                var header = JsonUtility.FromJson<VersionHeader>(json);
                if (header == null || header.version != PetArchive.CurrentVersion) { reason = "Pet record version is unsupported; data remains unchanged."; return false; }
                var data = JsonUtility.FromJson<PetArchive>(json);
                if (!PetArchive.Valid(data, out reason)) return false;
                Archive = data; Loaded = true; ReadOnly = false; Dirty = false; Status = "COMPANION PROFILES RESTORED"; return true;
            }
            catch (Exception) { reason = "Corrupt pet profile JSON; data remains unchanged."; return false; }
        }
        public PetProfileRecord ForProfile(string profileId, string ownerBodyId)
        {
            if (!Loaded) LoadProfiles();
            if (!PetArchive.StableId(profileId)) throw new ArgumentException("An actual stable participant ID is required.", nameof(profileId));
            var found = Archive.profiles.FirstOrDefault(p => p.profileId == profileId); if (found != null) return found;
            var pet = new PetRecord { id = "pet." + Guid.NewGuid().ToString("N"), profileId = profileId, bodyId = string.IsNullOrWhiteSpace(ownerBodyId) ? "semi.standard" : ownerBodyId };
            var profile = new PetProfileRecord { profileId = profileId, selectedPetId = pet.id, pets = new[] { pet } };
            Archive.profiles = Archive.profiles.Concat(new[] { profile }).ToArray(); Changed(); return profile;
        }
        public PetRecord SelectOrAdopt(string profileId, PetKind kind, string ownerBodyId)
        {
            if (kind < PetKind.MiniatureTruck || kind > PetKind.Dog) throw new ArgumentOutOfRangeException(nameof(kind));
            var profile = ForProfile(profileId, ownerBodyId); var pet = profile.pets.FirstOrDefault(p => p.kind == kind);
            if (pet == null) { pet = new PetRecord { id = "pet." + Guid.NewGuid().ToString("N"), profileId = profileId, kind = kind, bodyId = string.IsNullOrWhiteSpace(ownerBodyId) ? "semi.standard" : ownerBodyId }; profile.pets = profile.pets.Concat(new[] { pet }).ToArray(); }
            profile.selectedPetId = pet.id; Changed(); return pet;
        }
        public bool Rename(string profileId, string petId, string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 32) return false;
            var pet = Archive.profiles.FirstOrDefault(p => p.profileId == profileId)?.pets.FirstOrDefault(p => p.id == petId); if (pet == null) return false;
            pet.name = name.Trim(); Changed(); return true;
        }
        public bool CanMergeProfiles(PetProfileRecord[] updates, out string reason)
        {
            reason = ""; if (ReadOnly) { reason = "The unsupported original pet archive is preserved read-only."; return false; }
            if (updates == null || updates.Any(p => p == null)) { reason = "Missing pet profile updates."; return false; }
            var ids = updates.Select(p => p.profileId).ToArray();
            var candidate = new PetArchive { version = 1, profiles = Archive.profiles.Where(p => Array.IndexOf(ids, p.profileId) < 0).Concat(updates).ToArray() };
            return PetArchive.Valid(candidate, out reason);
        }
        public bool MergeProfiles(PetProfileRecord[] updates, out string reason)
        {
            if (!CanMergeProfiles(updates, out reason)) return false;
            var ids = updates.Select(p => p.profileId).ToArray();
            Archive = new PetArchive { version = 1, profiles = Archive.profiles.Where(p => Array.IndexOf(ids, p.profileId) < 0).Concat(updates).ToArray() }; Changed(); return true;
        }
        public void Changed()
        {
            if (ReadOnly) return; Dirty = true; quietUntil = Time.unscaledTime + 2;
        }
        void Update() { if (Dirty && Time.unscaledTime >= quietUntil && Time.unscaledTime >= nextWrite) SaveProfiles(); }
        public bool SaveProfiles()
        {
            if (ReadOnly) return false;
            if (!Ready(out string reason) || !PetArchive.Valid(Archive, out reason)) { Status = reason; nextWrite = Time.unscaledTime + 10; return false; }
            try
            {
                string json = JsonUtility.ToJson(Archive); var data = new SavedGameData { version = 1, sceneName = DocumentId };
                data.SetData(RecordKey, SaveSystem.NoSceneIndex, json); Directory.CreateDirectory(workshop.storage.customPath);
                workshop.storage.StoreSavedGameData(ReservedSlot, data);
                var returned = workshop.storage.RetrieveSavedGameData(ReservedSlot);
                if (returned == null || returned.GetData(RecordKey) != json) { Status = "Vendor pet-record verification failed."; return false; }
                Dirty = false; nextWrite = Time.unscaledTime + 5; Status = "COMPANION PROFILES SAVED"; return true;
            }
            catch (Exception e) { Status = "Companion save failed: " + e.Message; nextWrite = Time.unscaledTime + 10; return false; }
        }
        void OnApplicationPause(bool paused) { if (paused && Dirty) SaveProfiles(); }
        void OnApplicationQuit() { if (Dirty) SaveProfiles(); }
        void OnDisable() { if (Dirty && workshop != null && workshop.storage != null) SaveProfiles(); }
    }
}
