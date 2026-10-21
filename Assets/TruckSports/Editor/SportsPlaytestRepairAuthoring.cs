namespace LWS.TruckSports.Editor
{
    public static class SportsPlaytestRepairAuthoring
    {
        public static void Apply()
        {
            LWS.TruckFishing.Editor.FishingRepairAuthoring.Upgrade();
            LWS.TruckKickball.Editor.KickballRepairAuthoring.Upgrade();
            SportsBroadcastCameraAuthoring.Bind();
        }
    }
}
