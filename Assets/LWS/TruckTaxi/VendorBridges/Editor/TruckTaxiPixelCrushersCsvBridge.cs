using LWS.TruckTaxi.Editor;
using UnityEditor;

[InitializeOnLoad]
public static class TruckTaxiPixelCrushersCsvBridge
{
    static TruckTaxiPixelCrushersCsvBridge() => TruckTaxiDialogueImport.ReadVendorCsv=path=>PixelCrushers.CSVUtility.ReadCSVFile(path,PixelCrushers.EncodingType.UTF8);
}
