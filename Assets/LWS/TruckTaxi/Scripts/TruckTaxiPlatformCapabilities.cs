using System;

namespace LWS.TruckTaxi
{
    [Flags]
    public enum TruckTaxiPlatformCapability
    {
        None = 0, Achievements = 1, CloudSave = 2, Overlay = 4,
        DisplayName = 8, RichPresence = 16, HandheldController = 32
    }

    // Optional distribution features only. Never owns Career identity, save slots or storage.
    public interface ITruckTaxiPlatformCapabilities
    {
        TruckTaxiPlatformCapability Available { get; }
        string SuggestedDisplayName { get; }
        bool TryUnlockAchievement(string semanticId);
        bool TryOpenOverlay();
        bool TrySetRichPresence(string semanticActivityId);
    }

    public sealed class TruckTaxiOfflinePlatformCapabilities : ITruckTaxiPlatformCapabilities
    {
        public TruckTaxiPlatformCapability Available => TruckTaxiPlatformCapability.None;
        public string SuggestedDisplayName => null;
        public bool TryUnlockAchievement(string semanticId) => false;
        public bool TryOpenOverlay() => false;
        public bool TrySetRichPresence(string semanticActivityId) => false;
    }
}
