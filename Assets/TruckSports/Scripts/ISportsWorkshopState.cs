namespace LWS.TruckSports
{
    // Modes own validation and semantic snapshots; Pixel Crushers owns storage.
    public interface ISportsWorkshopState
    {
        string CaptureWorkshopJson();
        bool TryRestoreWorkshopJson(string json, out string reason);
    }
}
