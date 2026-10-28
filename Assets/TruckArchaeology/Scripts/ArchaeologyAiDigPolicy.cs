namespace LWS.TruckArchaeology
{
    // Selection of EXISTING tools only. Original cut sizes, opacity, cooldown,
    // physical soil clearance and preservation consequences remain authoritative.
    public static class ArchaeologyAiDigPolicy
    {
        public static ArchaeologyTool Choose(float actualKnownShapeClearance)
        {
            if(!float.IsFinite(actualKnownShapeClearance)||actualKnownShapeClearance<0)return ArchaeologyTool.Brush;
            // Original preservation damage begins inside radius+.05. An extra
            // .05m safety margin avoids selecting a coarse tool at that boundary.
            if(actualKnownShapeClearance>2.4f+.1f)return ArchaeologyTool.Excavator;
            if(actualKnownShapeClearance>1.25f+.1f)return ArchaeologyTool.Drill;
            if(actualKnownShapeClearance>1.05f+.1f)return ArchaeologyTool.Blower;
            return ArchaeologyTool.Brush;
        }
    }
}
