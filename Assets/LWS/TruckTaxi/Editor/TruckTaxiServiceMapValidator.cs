using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiServiceMapValidator
    {
        [MenuItem("Truck Taxi/Map/Align Selected Service Interactions")]
        public static void AlignSelectedServiceInteractions()
        {
            // Explicit capabilities, not names/POI tags, authorize adding the missing interaction component.
            foreach(var go in Selection.gameObjects)
            {
                var service=go.GetComponent<TruckTaxiServicePoint>();
                if(service==null || !service.gameObject.scene.IsValid()) continue;
                if(service.Supports(TruckTaxiServiceCapability.Store))
                {
                    var store=go.GetComponent<TruckTaxiStorePoint>() ?? Undo.AddComponent<TruckTaxiStorePoint>(go);
                    Undo.RecordObject(store,"Align store interaction bay");
                    store.stableId=service.stableId; store.displayName=service.displayName; store.location=service.location;
                    store.stopRadius=service.InteractionRadius; store.verticalTolerance=service.verticalTolerance;
                    EditorUtility.SetDirty(store); PrefabUtility.RecordPrefabInstancePropertyModifications(store);
                }
                if(service.Supports(TruckTaxiServiceCapability.Refuel))
                {
                    var gas=go.GetComponent<TruckTaxiGasStationPoint>() ?? Undo.AddComponent<TruckTaxiGasStationPoint>(go);
                    Undo.RecordObject(gas,"Align refuel interaction bay");
                    gas.stableId=service.stableId; gas.displayName=service.displayName;
                    gas.stoppingRadius=service.InteractionRadius; gas.verticalTolerance=service.verticalTolerance;
                    EditorUtility.SetDirty(gas); PrefabUtility.RecordPrefabInstancePropertyModifications(gas);
                }
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
            }
        }
        [MenuItem("Truck Taxi/Map/Validate Services And World Rings")]
        public static void ValidateLoaded()
        {
            var services=Object.FindObjectsByType<TruckTaxiServicePoint>(FindObjectsSortMode.None);
            var markers=Object.FindObjectsByType<TruckTaxiMapMarker>(FindObjectsSortMode.None);
            var presenter=Object.FindFirstObjectByType<TruckTaxiServiceRings>();
            var host=Object.FindFirstObjectByType<TruckTaxiBootstrap>();
            var findings=TruckTaxiServiceMapValidation.Validate(services,markers,
                Application.isPlaying ? presenter?.Rings ?? new Dictionary<string,TruckTaxiServiceRings.Ring>() : null,
                host?.Player!=null ? host.Player.transform.position : (Vector3?)null,presenter!=null ? presenter.visibleDistance : 350);
            foreach(var finding in findings) Debug.LogWarning("SERVICE MAP: "+finding);
            Debug.Log("SERVICE MAP: "+services.Length+" loaded services; "+findings.Count+" findings. "+
                (Application.isPlaying ? "Runtime ring geometry checked within presentation range." :
                    "Read-only authoring audit. Live ring geometry requires Play Mode; no scenes opened or modified."));
        }

        [MenuItem("Truck Taxi/Map/Ensure Service Marker Icons")]
        public static void EnsureServiceIcons()
        {
            // Keep all existing designer entries. Only the appended semantic types need new art.
            var registry=AssetDatabase.LoadAssetAtPath<TruckTaxiMapIconRegistry>(TruckTaxiMapIconSetup.AssetPath);
            if(registry==null) { Debug.LogError("Existing typed map registry is missing."); return; }
            Undo.RecordObject(registry,"Add service marker icons");
            var entries=new List<TruckTaxiMapIconRegistry.Entry>(registry.entries);
            foreach(var type in new[] { TruckTaxiMapMarkerType.Drink,TruckTaxiMapMarkerType.Sleep,TruckTaxiMapMarkerType.Place })
            {
                var entry=entries.Find(e=>e!=null && e.type==type);
                if(entry==null)
                {
                    entry=new TruckTaxiMapIconRegistry.Entry { type=type,label=type.ToString(),
                        color=(registry.stopRingPalette ?? new TruckTaxiStopRingPalette()).For(type),
                        explanation=type==TruckTaxiMapMarkerType.Place ? "A destination; no service capability implied." : "A usable "+type.ToString().ToLowerInvariant()+" service." };
                    entries.Add(entry);
                }
                if(entry.icon!=null) continue;
                const int size=64;
                var texture=new Texture2D(size,size,TextureFormat.RGBA32,false) { name="Taxi "+type+" glyph" };
                var pixels=new Color32[size*size];
                for(int y=0;y<size;y++) for(int x=0;x<size;x++)
                    pixels[y*size+x]=Glyph(type,new Vector2((x+.5f)/size,(y+.5f)/size)) ? new Color32(255,255,255,255) : new Color32(0,0,0,0);
                texture.SetPixels32(pixels); texture.Apply();
                // Embedded sub-assets keep the focused change in the existing registry; no world regeneration.
                AssetDatabase.AddObjectToAsset(texture,registry);
                entry.icon=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),size);
                entry.icon.name="Taxi "+type; AssetDatabase.AddObjectToAsset(entry.icon,registry);
            }
            registry.entries=entries.ToArray(); EditorUtility.SetDirty(registry); AssetDatabase.SaveAssetIfDirty(registry);
            Debug.Log("Service marker icons ready; existing mappings preserved.");
        }
        public static bool Glyph(TruckTaxiMapMarkerType type,Vector2 p)
        {
            if(type==TruckTaxiMapMarkerType.Sleep)
                return Box(p,.15f,.24f,.85f,.4f) || Box(p,.14f,.2f,.23f,.7f) ||
                    Box(p,.77f,.2f,.86f,.6f) || Box(p,.28f,.43f,.43f,.57f) || Box(p,.47f,.43f,.75f,.6f);
            if(type==TruckTaxiMapMarkerType.Drink)
                return Box(p,.3f,.2f,.7f,.3f) || Box(p,.25f,.3f,.34f,.7f) || Box(p,.66f,.3f,.75f,.7f) ||
                    Box(p,.25f,.66f,.75f,.75f) || Box(p,.5f,.7f,.58f,.88f) || Box(p,.33f,.3f,.67f,.48f);
            float distance=Vector2.Distance(p,new Vector2(.5f,.61f));
            return distance<.26f && distance>.15f || p.y<.5f && p.y>.15f && Mathf.Abs(p.x-.5f)<(p.y-.15f)*.5f;
        }
        private static bool Box(Vector2 p,float x0,float y0,float x1,float y1) => p.x>=x0 && p.x<=x1 && p.y>=y0 && p.y<=y1;
    }
}
