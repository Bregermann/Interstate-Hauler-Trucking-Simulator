using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiServiceMapValidator
    {
        [MenuItem("Truck Taxi/Map/Compose Selected Service Location")]
        public static void OpenComposition() => EditorWindow.GetWindow<TruckTaxiServiceCompositionWindow>("Service composition");

        // Compose existing art and anchors, not a second world generator. Never move anchors or save a scene.
        public static TruckTaxiServicePoint ComposeService(GameObject facility, TruckTaxiRideLocation location,
            TruckTaxiServiceCapability capabilities)
        {
            TruckTaxiWorldGeometryAuthoring.RequireEditableSceneObject(facility);
            TruckTaxiWorldGeometryAuthoring.RequireEditableSceneObject(location != null ? location.gameObject : null);
            if (facility.scene != location.gameObject.scene || location.truckStopPoint == null ||
                location.truckStopPoint.gameObject.scene != facility.scene || string.IsNullOrWhiteSpace(location.locationId) ||
                capabilities == TruckTaxiServiceCapability.None)
                throw new System.InvalidOperationException("Supply a same-scene authored location, stable ID, truck anchor and explicit capabilities.");
            bool hasArt = false;
            foreach (var renderer in facility.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is SkinnedMeshRenderer skin && skin.sharedMesh != null) hasArt = true;
                if (!(renderer is MeshRenderer)) continue;
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null) hasArt = true;
            }
            if (!hasArt)
                throw new System.InvalidOperationException("Supply existing facility art; this entry does not invent building geometry.");
            var current = facility.GetComponent<TruckTaxiServicePoint>();
            string id = !string.IsNullOrWhiteSpace(current?.stableId) ? current.stableId : "service." + location.locationId;
            if (current != null && current.location != null && current.location != location)
                throw new System.InvalidOperationException("Existing service belongs to another location; author a distinct bay instead.");
            foreach (var other in Resources.FindObjectsOfTypeAll<TruckTaxiServicePoint>())
                if (other != current && !EditorUtility.IsPersistent(other) && other.gameObject.scene.IsValid() &&
                    !UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(other.gameObject.scene) && other.stableId == id)
                    throw new System.InvalidOperationException("Duplicate service ID in loaded scenes: " + id);
            var marker = facility.GetComponent<TruckTaxiMapMarker>();
            foreach (var other in Resources.FindObjectsOfTypeAll<TruckTaxiMapMarker>())
                if (other != marker && !EditorUtility.IsPersistent(other) && other.gameObject.scene.IsValid() &&
                    !UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(other.gameObject.scene) && other.stableId == id)
                    throw new System.InvalidOperationException("Duplicate map ID in loaded scenes: " + id);
            if (marker != null && (!string.IsNullOrWhiteSpace(marker.stableId) && marker.stableId != id ||
                marker.service != null && marker.service != current))
                throw new System.InvalidOperationException("Existing map marker has another authority; keep it and choose a separate service owner.");
            // Refuse replacing existing interaction identities before making any changes.
            foreach (var component in new Component[] { facility.GetComponent<TruckTaxiStorePoint>(),
                facility.GetComponent<TruckTaxiGasStationPoint>(), facility.GetComponent<TruckTaxiBathroomPoint>() })
            {
                if (component == null) continue;
                var property = new SerializedObject(component).FindProperty("stableId");
                if (property != null && !string.IsNullOrWhiteSpace(property.stringValue) && property.stringValue != id)
                    throw new System.InvalidOperationException("Existing interaction ID differs; reconcile explicitly: " + property.stringValue);
                var linkedLocation = new SerializedObject(component).FindProperty("location");
                if (linkedLocation != null && linkedLocation.objectReferenceValue != null && linkedLocation.objectReferenceValue != location)
                    throw new System.InvalidOperationException("Existing interaction references another bay; reconcile explicitly.");
            }
            const string label = "Compose Truck Taxi service";
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName(label);
            try
            {
                T Ensure<T>() where T : Component => facility.GetComponent<T>() ?? Undo.AddComponent<T>(facility);
                void Edit(Component component, System.Action assign)
                {
                    string before = EditorJsonUtility.ToJson(component);
                    Undo.RecordObject(component, label); assign();
                    if (before == EditorJsonUtility.ToJson(component)) return;
                    EditorUtility.SetDirty(component); PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(facility.scene);
                }
                var service = Ensure<TruckTaxiServicePoint>();
                Edit(service, () => { service.stableId = id; service.location = location;
                    service.displayName = location.locationName; service.capabilities |= capabilities; service.generateValidationFacility = false; });
                if ((service.capabilities & TruckTaxiServiceCapability.Store) != 0)
                {
                    var store = Ensure<TruckTaxiStorePoint>();
                    Edit(store, () => { store.stableId = id; store.displayName = service.displayName; store.location = location;
                        store.stopRadius = service.InteractionRadius; store.verticalTolerance = service.verticalTolerance; });
                }
                if ((service.capabilities & TruckTaxiServiceCapability.Refuel) != 0)
                {
                    var gas = Ensure<TruckTaxiGasStationPoint>();
                    Edit(gas, () => { gas.stableId = id; gas.displayName = service.displayName;
                        gas.stoppingRadius = service.InteractionRadius; gas.verticalTolerance = service.verticalTolerance; });
                }
                if ((service.capabilities & TruckTaxiServiceCapability.Restroom) != 0)
                {
                    var bathroom = Ensure<TruckTaxiBathroomPoint>();
                    Edit(bathroom, () => { bathroom.stableId = id; bathroom.displayName = service.displayName; bathroom.location = location;
                        bathroom.stopRadius = service.InteractionRadius; bathroom.verticalTolerance = service.verticalTolerance;
                        bathroom.allowsJugDisposal = (service.capabilities & TruckTaxiServiceCapability.DisposeWaste) != 0;
                        bathroom.allowsCabCleanup = (service.capabilities & TruckTaxiServiceCapability.CleanCab) != 0; });
                }
                marker = Ensure<TruckTaxiMapMarker>();
                Edit(marker, () => { marker.stableId = id; marker.label = service.displayName; marker.service = service;
                    marker.serviceBound = true; marker.source = location.truckStopPoint;
                    marker.markerType = TruckTaxiServicePresentation.MarkerType(service.capabilities); });
                Undo.CollapseUndoOperations(group);
                return service;
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }

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
            var roots = new List<GameObject>();
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene.isLoaded && !UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(scene))
                    roots.AddRange(scene.GetRootGameObjects());
            }
            var services=Collect<TruckTaxiServicePoint>(roots);
            var markers=Collect<TruckTaxiMapMarker>(roots);
            var presenter=Object.FindFirstObjectByType<TruckTaxiServiceRings>();
            var host=Object.FindFirstObjectByType<TruckTaxiBootstrap>();
            var findings=TruckTaxiServiceMapValidation.Validate(services,markers,
                Application.isPlaying ? presenter?.Rings ?? new Dictionary<string,TruckTaxiServiceRings.Ring>() : null,
                host?.Player!=null ? host.Player.transform.position : (Vector3?)null,presenter!=null ? presenter.visibleDistance : 350,
                includeInactiveServices: true);
            findings.AddRange(TruckTaxiServiceMapValidation.ValidateAuthoringData(services, Collect<TruckTaxiWorldInteractable>(roots)));
            findings.AddRange(ValidateGeometry(services, Collect<Collider>(roots), Collect<MeshRenderer>(roots)));
            foreach(var finding in findings) Debug.LogWarning("SERVICE MAP: "+finding);
            Debug.Log("SERVICE MAP: "+services.Count+" loaded services; "+findings.Count+" findings. "+
                (Application.isPlaying ? "Runtime ring geometry checked within presentation range." :
                    "Read-only authoring audit. Live ring geometry requires Play Mode; no scenes opened or modified.") +
                " Geometry findings are candidates, not proof of the reported wall/flicker. No automatic repairs.");
        }

        [MenuItem("Truck Taxi/Map/Validate Selected Service Authoring")]
        public static void ValidateSelectedAuthoring()
        {
            var findings = ValidateAuthoring(Selection.gameObjects);
            foreach (var finding in findings) Debug.LogWarning("SERVICE AUTHORING: " + finding);
            Debug.Log($"SERVICE AUTHORING: {findings.Count} findings under selection. Include adjacent ground/buildings " +
                "in the selection to check their overlap. Candidates only; no objects changed or scenes saved.");
        }

        public static List<string> ValidateAuthoring(IEnumerable<GameObject> scope)
        {
            var roots = new List<GameObject>(scope);
            var services = Collect<TruckTaxiServicePoint>(roots);
            var findings = TruckTaxiServiceMapValidation.Validate(services, Collect<TruckTaxiMapMarker>(roots),
                includeInactiveServices: true);
            findings.AddRange(TruckTaxiServiceMapValidation.ValidateAuthoringData(services, Collect<TruckTaxiWorldInteractable>(roots)));
            findings.AddRange(ValidateGeometry(services, Collect<Collider>(roots), Collect<MeshRenderer>(roots)));
            findings.Sort(System.StringComparer.Ordinal);
            return findings;
        }

        private static List<T> Collect<T>(IEnumerable<GameObject> roots) where T : Component
        {
            var seen = new HashSet<T>();
            var result = new List<T>();
            foreach (var root in roots)
            {
                if (root == null || EditorUtility.IsPersistent(root) || !root.scene.IsValid() || !root.scene.isLoaded) continue;
                foreach (var item in root.GetComponentsInChildren<T>(true))
                    if (seen.Add(item)) result.Add(item);
            }
            return result;
        }

        private static List<string> ValidateGeometry(List<TruckTaxiServicePoint> services,
            List<Collider> colliders, List<MeshRenderer> renderers)
        {
            var findings = new List<string>();
            var solids = new List<Collider>();
            foreach (var collider in colliders)
            {
                if (!TruckTaxiWorldColliderReview.IsSolid(collider)) continue;
                solids.Add(collider);
                string suspicion = TruckTaxiWorldColliderReview.Suspicion(collider);
                if (suspicion != null)
                    findings.Add("SOLID REVIEW: " + suspicion + "\n" + TruckTaxiSignificantCollisionDiagnostic.DescribeCollider(collider));
                var renderer = collider.GetComponent<Renderer>();
                if (renderer != null && (!renderer.enabled || renderer.forceRenderingOff))
                    findings.Add("DISABLED RENDERER / LIVE COLLIDER: " + TruckTaxiWorldColliderReview.Identity(collider.transform));
            }

            solids.Sort((a, b) => a.bounds.min.x.CompareTo(b.bounds.min.x));
            for (int i = 0; i < solids.Count; i++)
                for (int j = i + 1; j < solids.Count && solids[j].bounds.min.x < solids[i].bounds.max.x; j++)
                {
                    var a = solids[i]; var b = solids[j];
                    if (a.GetType() != b.GetType() || (a.bounds.center - b.bounds.center).sqrMagnitude > .000001f ||
                        (a.bounds.size - b.bounds.size).sqrMagnitude > .000001f) continue;
                    findings.Add("COINCIDENT SOLID BOUNDS CANDIDATE (not shape equality): " +
                        TruckTaxiWorldColliderReview.Identity(a.transform) + " [collider=" + a.GetInstanceID() + "] <-> " +
                        TruckTaxiWorldColliderReview.Identity(b.transform) + " [collider=" + b.GetInstanceID() + "]");
                }

            var surfaces = new List<(MeshRenderer renderer, Bounds bounds)>();
            foreach (var renderer in renderers)
            {
                if (!TruckTaxiWorldColliderReview.IsVisible(renderer) || renderer.GetComponent<MeshFilter>()?.sharedMesh == null) continue;
                var bounds = TruckTaxiWorldGeometryAuthoring.WorldMeshBounds(renderer);
                if (bounds.size.x * bounds.size.z >= 1) surfaces.Add((renderer, bounds));
            }
            surfaces.Sort((a, b) => a.bounds.min.x.CompareTo(b.bounds.min.x));
            for (int i = 0; i < surfaces.Count; i++)
                for (int j = i + 1; j < surfaces.Count && surfaces[j].bounds.min.x < surfaces[i].bounds.max.x; j++)
                {
                    var a = surfaces[i]; var b = surfaces[j];
                    bool duplicate = TruckTaxiWorldGeometryAuthoring.ExactDuplicate(a.renderer, b.renderer);
                    bool coplanar = TruckTaxiWorldGeometryAuthoring.CoplanarTopOverlap(a.bounds, b.bounds, out float area) &&
                        a.bounds.size.y <= 2 && b.bounds.size.y <= 2;
                    if (!duplicate && !coplanar) continue;
                    findings.Add((duplicate ? "DUPLICATE MESH CANDIDATE" : "COPLANAR TOP-BOUNDS CANDIDATE") +
                        $" overlap={area:0.###}m2; topY={a.bounds.max.y:0.####}/{b.bounds.max.y:0.####}; " +
                        "confirm triangles and co-residency: " + TruckTaxiWorldColliderReview.Identity(a.renderer.transform) +
                        " <-> " + TruckTaxiWorldColliderReview.Identity(b.renderer.transform));
                }

            foreach (var service in services)
            {
                if (!service.isActiveAndEnabled) continue;
                var anchor = service.location != null ? service.location.truckStopPoint : service.transform;
                if (anchor == null) continue;
                // A conservative truck-sized bay sample. Ground below 0.3m is intentionally excluded.
                var bay = TruckTaxiWorldColliderReview.TransformBounds(new Bounds(Vector3.zero, new Vector3(3, 3, 8)),
                    Matrix4x4.TRS(service.Position + Vector3.up * 1.8f, anchor.rotation, Vector3.one));
                foreach (var solid in solids)
                {
                    if (solid.attachedRigidbody != null && !solid.attachedRigidbody.isKinematic || !bay.Intersects(solid.bounds)) continue;
                    findings.Add("PARKING BAY CLEARANCE CANDIDATE (3x3x8m oriented bay AABB, not a drive test): " +
                        TruckTaxiWorldColliderReview.Identity(service.transform) + " <-> " + TruckTaxiWorldColliderReview.Identity(solid.transform));
                }
            }
            return findings;
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

    public sealed class TruckTaxiServiceCompositionWindow : EditorWindow
    {
        [SerializeField] private GameObject facility;
        [SerializeField] private TruckTaxiRideLocation location;
        [SerializeField] private TruckTaxiServiceCapability capabilities;
        private string status;
        private void OnEnable() { if (facility == null) facility = Selection.activeGameObject; }
        private void OnGUI()
        {
            facility = (GameObject)EditorGUILayout.ObjectField("Existing facility art", facility, typeof(GameObject), true);
            location = (TruckTaxiRideLocation)EditorGUILayout.ObjectField("Authored bay/location", location, typeof(TruckTaxiRideLocation), true);
            capabilities = (TruckTaxiServiceCapability)EditorGUILayout.EnumFlagsField("Capabilities to add", capabilities);
            if (GUILayout.Button("Compose referenced service"))
            {
                try { var point = TruckTaxiServiceMapValidator.ComposeService(facility, location, capabilities);
                    status = "Composed " + point.stableId + ". Review/Undo; scene not saved."; }
                catch (System.Exception error) { status = error.Message; }
            }
            if (GUILayout.Button("Validate selected scope")) TruckTaxiServiceMapValidator.ValidateSelectedAuthoring();
            EditorGUILayout.HelpBox(status ?? "Uses existing art and explicit references. Existing unrelated components and content are retained.", MessageType.Info);
        }
    }
}
