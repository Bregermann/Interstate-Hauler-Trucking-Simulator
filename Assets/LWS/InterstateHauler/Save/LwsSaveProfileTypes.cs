using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public enum LwsSaveSlotType
    {
        Manual,
        Autosave,
        Backup
    }

    public static class LwsSaveSchema
    {
        public const int CurrentVersion = 1;
        public const int ProfileDirectoryVendorSlot = 16000;
        public const int FirstProfileSlotBase = 16100;
        public const int SlotsPerProfile = 20;
        public const int ManualSlotCount = 3;
        public const int AutosaveSlotOffset = 10;
        public const int BackupSlotOffset = 15;
        public const string ProfileDirectoryRecordKey = "lws.profile-directory";
        public const string SemanticSnapshotRecordKey = "lws.semantic-state";
        public const string DefaultGameVersion = "0.1-dev";

        public static int MapToVendorSlot(int profileIndex, LwsSaveSlotType slotType, int slotNumber)
        {
            profileIndex = Mathf.Max(0, profileIndex);
            int baseSlot = FirstProfileSlotBase + profileIndex * SlotsPerProfile;
            switch (slotType)
            {
                case LwsSaveSlotType.Manual:
                    return baseSlot + Mathf.Clamp(slotNumber, 1, ManualSlotCount);
                case LwsSaveSlotType.Autosave:
                    return baseSlot + AutosaveSlotOffset + Mathf.Max(1, slotNumber);
                case LwsSaveSlotType.Backup:
                    return baseSlot + BackupSlotOffset + Mathf.Max(1, slotNumber);
                default:
                    return baseSlot + Mathf.Clamp(slotNumber, 1, ManualSlotCount);
            }
        }

        public static IEnumerable<int> EnumerateReservedVendorSlots(int profileIndex)
        {
            for (int i = 1; i <= ManualSlotCount; i++)
            {
                yield return MapToVendorSlot(profileIndex, LwsSaveSlotType.Manual, i);
            }

            yield return MapToVendorSlot(profileIndex, LwsSaveSlotType.Autosave, 1);

            for (int i = 1; i <= 5; i++)
            {
                yield return MapToVendorSlot(profileIndex, LwsSaveSlotType.Backup, i);
            }
        }

        public static string ResolveGameVersion()
        {
            return string.IsNullOrWhiteSpace(Application.version) ? DefaultGameVersion : Application.version;
        }
    }

    [Serializable]
    public sealed class LwsSaveProfileMetadata
    {
        public string stableProfileId;
        public string displayName;
        public int profileIndex;
        public long createdUtcTicks;
        public long lastPlayedUtcTicks;
        public double totalPlaytimeSeconds;
        public int lastUsedManualSlot;
        public int schemaVersion = LwsSaveSchema.CurrentVersion;
        public string gameVersion = LwsSaveSchema.DefaultGameVersion;

        public bool IsValid => !string.IsNullOrWhiteSpace(stableProfileId) && profileIndex >= 0 && schemaVersion > 0;
        public string DisplayNameOrFallback => string.IsNullOrWhiteSpace(displayName) ? "Driver" : displayName;
    }

    [Serializable]
    public sealed class LwsManualSaveSlotMetadata
    {
        public string stableProfileId;
        public LwsSaveSlotType slotType;
        public int slotNumber;
        public int vendorSlotNumber;
        public bool occupied;
        public long savedUtcTicks;
        public double playtimeSeconds;
        public int schemaVersion = LwsSaveSchema.CurrentVersion;
        public string gameVersion = LwsSaveSchema.DefaultGameVersion;
        public string sceneName;
        public string worldLabel;
        public string truckDefinitionId;
        public string routeDestinationId;
        public string weatherPresetId;

        public string SlotLabel => slotType == LwsSaveSlotType.Manual ? $"SLOT {slotNumber}" : $"{slotType.ToString().ToUpperInvariant()} {slotNumber}";
        public string OccupancyLabel => occupied ? "OCCUPIED" : "EMPTY";
    }

    [Serializable]
    public sealed class LwsSaveProfileDirectory
    {
        public int schemaVersion = LwsSaveSchema.CurrentVersion;
        public string selectedProfileId;
        public int nextProfileIndex;
        public string gameVersion = LwsSaveSchema.DefaultGameVersion;
        public List<LwsSaveProfileMetadata> profiles = new List<LwsSaveProfileMetadata>();
        public List<LwsManualSaveSlotMetadata> manualSlots = new List<LwsManualSaveSlotMetadata>();

        public IReadOnlyList<LwsSaveProfileMetadata> Profiles => profiles;
        public IReadOnlyList<LwsManualSaveSlotMetadata> ManualSlots => manualSlots;

        public LwsSaveProfileMetadata FindProfile(string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return null;
            }

            return profiles.Find(p => string.Equals(p.stableProfileId, profileId, StringComparison.Ordinal));
        }

        public LwsManualSaveSlotMetadata FindManualSlot(string profileId, int slotNumber)
        {
            return manualSlots.Find(s =>
                s.slotType == LwsSaveSlotType.Manual &&
                s.slotNumber == slotNumber &&
                string.Equals(s.stableProfileId, profileId, StringComparison.Ordinal));
        }

        public LwsManualSaveSlotMetadata GetOrCreateManualSlot(LwsSaveProfileMetadata profile, int slotNumber)
        {
            if (profile == null)
            {
                return null;
            }

            LwsManualSaveSlotMetadata slot = FindManualSlot(profile.stableProfileId, slotNumber);
            if (slot != null)
            {
                slot.vendorSlotNumber = LwsSaveSchema.MapToVendorSlot(profile.profileIndex, LwsSaveSlotType.Manual, slotNumber);
                return slot;
            }

            slot = new LwsManualSaveSlotMetadata
            {
                stableProfileId = profile.stableProfileId,
                slotType = LwsSaveSlotType.Manual,
                slotNumber = slotNumber,
                vendorSlotNumber = LwsSaveSchema.MapToVendorSlot(profile.profileIndex, LwsSaveSlotType.Manual, slotNumber),
                occupied = false,
                schemaVersion = LwsSaveSchema.CurrentVersion,
                gameVersion = LwsSaveSchema.ResolveGameVersion(),
                sceneName = string.Empty,
                worldLabel = string.Empty,
                truckDefinitionId = string.Empty,
                routeDestinationId = string.Empty,
                weatherPresetId = string.Empty
            };
            manualSlots.Add(slot);
            return slot;
        }

        public List<LwsManualSaveSlotMetadata> GetManualSlotsForProfile(LwsSaveProfileMetadata profile)
        {
            var slots = new List<LwsManualSaveSlotMetadata>();
            if (profile == null)
            {
                return slots;
            }

            for (int i = 1; i <= LwsSaveSchema.ManualSlotCount; i++)
            {
                slots.Add(GetOrCreateManualSlot(profile, i));
            }

            return slots;
        }

        public int AllocateProfileIndex()
        {
            int index = Mathf.Max(0, nextProfileIndex);
            nextProfileIndex = index + 1;
            return index;
        }

        public bool RemoveProfile(string profileId)
        {
            int removed = profiles.RemoveAll(p => string.Equals(p.stableProfileId, profileId, StringComparison.Ordinal));
            manualSlots.RemoveAll(s => string.Equals(s.stableProfileId, profileId, StringComparison.Ordinal));
            if (string.Equals(selectedProfileId, profileId, StringComparison.Ordinal))
            {
                selectedProfileId = profiles.Count > 0 ? profiles[0].stableProfileId : string.Empty;
            }

            return removed > 0;
        }

        public void EnsureValid()
        {
            schemaVersion = schemaVersion <= 0 ? LwsSaveSchema.CurrentVersion : schemaVersion;
            gameVersion = string.IsNullOrWhiteSpace(gameVersion) ? LwsSaveSchema.ResolveGameVersion() : gameVersion;
            profiles ??= new List<LwsSaveProfileMetadata>();
            manualSlots ??= new List<LwsManualSaveSlotMetadata>();

            int next = 0;
            foreach (LwsSaveProfileMetadata profile in profiles)
            {
                if (profile == null)
                {
                    continue;
                }

                profile.displayName = string.IsNullOrWhiteSpace(profile.displayName) ? "Driver" : profile.displayName.Trim();
                profile.gameVersion = string.IsNullOrWhiteSpace(profile.gameVersion) ? gameVersion : profile.gameVersion;
                profile.schemaVersion = profile.schemaVersion <= 0 ? LwsSaveSchema.CurrentVersion : profile.schemaVersion;
                next = Mathf.Max(next, profile.profileIndex + 1);
            }

            nextProfileIndex = Mathf.Max(nextProfileIndex, next);
            if (FindProfile(selectedProfileId) == null)
            {
                selectedProfileId = profiles.Count > 0 ? profiles[0].stableProfileId : string.Empty;
            }
        }
    }

    [Serializable]
    public sealed class LwsFutureCabAccessorySavePayload
    {
        public int schemaVersion = LwsSaveSchema.CurrentVersion;
        public string stableAccessoryId;
        public string anchorId;
        public bool equipped;
    }

    [Serializable]
    public sealed class LwsFutureCompanionSavePayload
    {
        public int schemaVersion = LwsSaveSchema.CurrentVersion;
        public string stableCompanionId;
        public string type;
        public string name;
        public string basicState;
    }

    [Serializable]
    public sealed class LwsFutureLifeEventSavePayload
    {
        public int schemaVersion = LwsSaveSchema.CurrentVersion;
        public string stableEventId;
        public bool occurred;
    }
}
