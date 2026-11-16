#if UNITY_EDITOR
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static partial class DragonKingdomNativeRoadTypeProbe
    {
        [Serializable]public sealed class NativePublicSetting
        {public string name,type,value;}
        [Serializable]public sealed class NativeTypeSettings
        {public string label,actualType,fullSerializedJson;public int publicInstanceFields;public NativePublicSetting[] settings;}
        static NativeTypeSettings CapturePublicTypeSettings(string label,object value)
        {
            if(value==null)return new NativeTypeSettings{label=label,actualType="NULL",settings=Array.Empty<NativePublicSetting>()};
            var fields=value.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance).OrderBy(f=>f.Name,StringComparer.Ordinal).ToArray();
            return new NativeTypeSettings{label=label,actualType=value.GetType().FullName,fullSerializedJson=JsonUtility.ToJson(value,true),publicInstanceFields=fields.Length,settings=fields.Select(field=>new NativePublicSetting{name=field.Name,type=field.FieldType.FullName,value=PublicSettingValue(field.GetValue(value),0)}).ToArray()};
        }
        static string PublicSettingValue(object value,int depth)
        {
            if(value==null)return "NULL";if(value is string text)return text;
            if(value is UnityEngine.Object asset)
            {
                string type=asset.GetType().FullName,instance;try{instance=asset.GetInstanceID().ToString(CultureInfo.InvariantCulture);}catch(Exception error){instance="UNAVAILABLE("+error.GetType().Name+")";}
                if(!asset)return type+" UNITY_NULL_OR_DESTROYED INSTANCE="+instance;
                if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset,out string guid,out long id)&&!string.IsNullOrEmpty(guid))return type+" GUID="+guid+" FILEID="+id;
                // A transient reference is already identified by its instance
                // ID. Never call native get_name on a null/destroyed SDK field.
                return type+" INSTANCE="+instance;
            }
            if(value is float f)return f.ToString("R",CultureInfo.InvariantCulture);if(value is double d)return d.ToString("R",CultureInfo.InvariantCulture);
            if(value.GetType().IsPrimitive||value.GetType().IsEnum)return Convert.ToString(value,CultureInfo.InvariantCulture);
            if(value is IEnumerable sequence&&depth<6)return "["+string.Join(",",sequence.Cast<object>().Select(item=>PublicSettingValue(item,depth+1)))+"]";
            return JsonUtility.ToJson(value,true);
        }
    }
}
#endif
