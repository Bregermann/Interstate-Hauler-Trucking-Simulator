using System;
using System.Linq;
using NWH.VehiclePhysics2.Modules.Trailer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckSnake.Editor
{
    public static class SnakeFirstHitchAuthoring
    {
        const string VendorTrailer="Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTrailer Variant.prefab";
        // Same owned render-model normalization used by Trailer Plug and Snake,
        // applied to the vendor's real kingpin instead of an invented offset.
        public static Vector3 MeasureNormalizedTrailerKingpin()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(VendorTrailer);
            if(prefab==null)throw new InvalidOperationException("Preserve the installed NWH cargo trailer asset.");
            var reference=UnityEngine.Object.Instantiate(prefab);
            try
            {
                reference.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                var filters=reference.GetComponentsInChildren<MeshFilter>(true);
                bool first=true;Bounds bounds=default;
                foreach(var filter in filters){var renderer=filter.GetComponent<Renderer>();if(renderer==null)continue;if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);}
                var trailer=reference.GetComponentsInChildren<TrailerModuleWrapper>(true).Single();
                if(first||bounds.size.x<.1f||bounds.size.y<.1f||bounds.size.z<.1f||trailer.module.attachmentPoint==null)throw new InvalidOperationException("NWH trailer needs its authored mesh bounds and kingpin.");
                return Vector3.up*1.6f+Vector3.Scale(trailer.module.attachmentPoint.position-bounds.center,new Vector3(2.6f/bounds.size.x,3.2f/bounds.size.y,13.5f/bounds.size.z));
            }
            finally{UnityEngine.Object.DestroyImmediate(reference);}
        }
        [MenuItem("Truck Sports/Truck Snake/Attach first trailer to authored fifth wheel")]
        public static void BindSavedFirstHitches()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TruckSnake/Scenes/TruckSnake.unity",OpenSceneMode.Single);
            var match=UnityEngine.Object.FindAnyObjectByType<SnakeMatch>();
            if(match==null||match.convoys.Length!=4)throw new InvalidOperationException("Use the existing four-participant Snake scene.");
            Vector3 kingpin=MeasureNormalizedTrailerKingpin();
            foreach(var convoy in match.convoys)
            {
                var hitch=convoy.tractor.GetComponentsInChildren<TrailerHitchModuleWrapper>(true).Single();
                if(hitch.module.attachmentPoint==null)throw new InvalidOperationException("Existing NWH tractor has no authored fifth wheel.");
                convoy.firstTractorAttachment=hitch.module.attachmentPoint;convoy.firstTrailerAttachment=kingpin;EditorUtility.SetDirty(convoy);
                Debug.Log("SNAKE_FIRST_HITCH slot="+convoy.slot+" tractorLocal="+convoy.tractor.transform.InverseTransformPoint(convoy.firstTractorAttachment.position)+" normalizedTrailerKingpin="+kingpin+"; later path segments and all vehicle/vendor settings preserved.");
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
    }
}
