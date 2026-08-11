using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace CompassNavigatorPro {

    #region Save/Load public types

    /// <summary>
    /// Subsystems to include when saving/loading runtime state. Combine as a bitmask.
    /// </summary>
    [Flags]
    public enum CompassSaveScope {
        None = 0,
        Fog = 1 << 0,
        Route = 1 << 1,
        POIVisited = 1 << 2,
        RuntimePOIs = 1 << 3,
        MiniMapView = 1 << 4,
        All = Fog | Route | POIVisited | RuntimePOIs | MiniMapView
    }

    /// <summary>
    /// Options controlling a Save or Load operation.
    /// </summary>
    public sealed class CompassSaveOptions {

        /// <summary>Which subsystems to save or load.</summary>
        public CompassSaveScope scope = CompassSaveScope.All;

        /// <summary>
        /// When loading, replay gameplay events (OnPOIVisited, OnRouteUpdated, OnMiniMapChangeFullScreenState...).
        /// Default false: a load restores state silently so it doesn't re-trigger discovery audio/text/quests.
        /// </summary>
        public bool fireEventsOnLoad;

        public static CompassSaveOptions Default => new CompassSaveOptions();
    }

    /// <summary>
    /// Result returned by LoadState / LoadStateJson. Inspect 'ok' and the counters to know what happened.
    /// </summary>
    public struct CompassLoadResult {
        public bool ok;
        public int schemaVersion;
        public int poisRestored;
        public int poisRecreated;
        public int poisUnmatched;
        public string warning;
        public string error;

        public static CompassLoadResult Failed (string error) {
            return new CompassLoadResult { ok = false, error = error };
        }
    }

    /// <summary>
    /// Implement and register (SetPOIFactory) to control how runtime-created POIs are rebuilt on load.
    /// Return a GameObject (already positioned) that should carry the recreated POI, or null to let
    /// the compass create a bare GameObject. The compass then adds/configures the CompassProPOI from the descriptor.
    /// </summary>
    public interface ICompassPOIFactory {
        GameObject CreatePOIObject (CompassPoiDTO descriptor);
    }

    #endregion


    #region Save/Load DTO model

    [Serializable]
    public class CompassStateDTO {
        public int schemaVersion;
        public bool poisIncluded;        // true if the POI list was captured (POI scope active at save time); guards the authoritative prune on load
        public List<CompassInstanceDTO> compasses = new List<CompassInstanceDTO>();
        public List<CompassPoiDTO> pois = new List<CompassPoiDTO>();
    }

    [Serializable]
    public class CompassInstanceDTO {
        public int compassGroup;
        // mini-map view
        public float miniMapZoomLevel;
        public float miniMapFullScreenZoomLevel;
        public Vector3 miniMapFollowOffset;
        public bool miniMapFullScreenState;
        // route
        public CompassRouteDTO route;
        // fog
        public CompassFogDTO fog;
        // focused POI save-key for this compass (empty if none)
        public string focusedPoiKey;
    }

    [Serializable]
    public class CompassPoiDTO {
        // identity
        public string key;               // save key (stableId or id: fallback)
        public string stableId;          // explicit stableId (may be empty for legacy)
        public int id;                   // legacy int id
        // runtime state
        public int visitedMask;          // bit g-1 = visited by compass group g
        public bool enabled;             // authoritative enabled/hidden state at save time
        // transform
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
        // general
        public int priority;
        public int compassGroupMask;
        public int visibility;
        public bool ignoreAreaOfInterest;
        public bool clampPosition;
        public float visibleDistanceOverride;
        public float visibleMinDistanceOverride;
        public float titleMinPOIDistanceOverride;
        public string title;
        public int titleVisibility;
        public bool canBeVisited;
        public float visitedDistanceOverride;
        public bool hideWhenVisited;
        public string visitedText;
        public bool playAudioClipWhenVisited;
        public float radius;
        public float iconScale;
        public bool iconScaleIsFixed;
        public bool iconShowDistance;
        public Color tintColor;
        public Vector3 positionOffset;
        public bool dontDestroyOnLoad;
        // on-screen / off-screen indicator
        public bool showOnScreenIndicator;
        public float onScreenIndicatorScale;
        public bool onScreenIndicatorShowDistance;
        public bool onScreenIndicatorShowTitle;
        public bool showSceneGizmo;
        public bool showOffScreenIndicator;
        public bool offScreenIndicatorShowDistance;
        public float offScreenIndicatorMarginOverride;
        public float onScreenIndicatorNearFadeDistance;
        public float onScreenIndicatorNearFadeMin;
        public float onScreenIndicatorFarDistance;
        public float onScreenIndicatorFarFadeDistance;
        // heartbeat
        public bool heartbeatEnabled;
        public float heartbeatDistance;
        // mini-map
        public int miniMapType;
        public int miniMapVisibility;
        public float miniMapVisibleDistanceOverride;
        public bool miniMapClampPosition;
        public float miniMapClampedScaleMultiplier;
        public bool miniMapShowRotation;
        public float miniMapRotationAngleOffset;
        public float miniMapIconScale;
        public bool miniMapShowCircle;
        public float miniMapCircleRadius;
        public Color miniMapCircleColor;
        public Color miniMapCircleInnerColor;
        public float miniMapCircleStartRadius;
        public bool miniMapCircleAnimationWhenAppears;
        public int miniMapCircleAnimationRepetitions;
        // asset refs (stored by asset name; re-resolved on load against loaded assets, empty = leave current untouched)
        public string iconNonVisitedName;
        public string iconVisitedName;
        public string visitedAudioClipName;
        public string beaconAudioClipName;
        public string scanHitAudioClipName;
        public string heartbeatAudioClipName;
    }

    [Serializable]
    public class CompassRouteDTO {
        public int mode;                 // 0 none, 1 points, 2 toPOI, 3 toDestination, 4 waypoints
        public List<Vector3> points = new List<Vector3>();
        public int nextWaypoint;
        public float progress;
        public bool startFollowsPlayer;
        public string destinationPoiKey;
        public bool useWaypoints;
    }

    [Serializable]
    public class CompassFogDTO {
        public int textureSize;
        public Vector3 center;
        public Vector3 sizeWS;
        public Color color;
        public int encoding;             // 0 raw, 1 rle
        public byte[] payload;           // alpha bytes, encoded
    }

    #endregion


    public partial class CompassPro : MonoBehaviour {

        #region Save/Load constants

        const int SAVE_SCHEMA_VERSION = 2;          // v2: full POI snapshot (authoritative save/restore + prune)
        const int MIN_SUPPORTED_SCHEMA = 2;         // v1 saves only stored partial POI data; reject to avoid wiping props
        static readonly byte[] SAVE_MAGIC = { (byte)'C', (byte)'N', (byte)'P', (byte)'3' };

        const int ROUTE_MODE_NONE = 0;
        const int ROUTE_MODE_POINTS = 1;
        const int ROUTE_MODE_TO_POI = 2;
        const int ROUTE_MODE_TO_DESTINATION = 3;
        const int ROUTE_MODE_WAYPOINTS = 4;

        const int FOG_ENCODING_RAW = 0;
        const int FOG_ENCODING_RLE = 1;

        #endregion


        #region Save/Load registries (optional runtime POI factory)

        ICompassPOIFactory poiFactory;

        /// <summary>
        /// Optional. Registers a factory to control how recreated POIs get their host GameObject (e.g. spawn your own
        /// prefab). If unset, the compass creates a bare GameObject. Not required for save/load to work.
        /// </summary>
        public void SetPOIFactory (ICompassPOIFactory factory) {
            poiFactory = factory;
        }

        // Asset references (icons, audio) can't live in a portable blob, so the save stores each asset's name and the
        // load re-resolves it transparently against the assets already loaded in memory. No registration needed.
        Dictionary<Type, UnityEngine.Object[]> assetPoolCache;

        static string AssetName (UnityEngine.Object asset) {
            return asset != null ? asset.name : null;
        }

        T ResolveAssetByName<T> (string name) where T : UnityEngine.Object {
            if (string.IsNullOrEmpty(name)) return null;
            if (assetPoolCache == null) assetPoolCache = new Dictionary<Type, UnityEngine.Object[]>();
            if (!assetPoolCache.TryGetValue(typeof(T), out UnityEngine.Object[] pool)) {
                pool = Misc.FindObjectsOfTypeAll(typeof(T));
                assetPoolCache[typeof(T)] = pool;
            }
            for (int i = 0; i < pool.Length; i++) {
                if (pool[i] != null && pool[i].name == name) return (T)pool[i];
            }
            return null;
        }

        #endregion


        #region Save/Load public API (instance)

        /// <summary>
        /// Captures this compass' runtime state (fog, route, visited POIs, runtime POIs, minimap view) into a compact binary blob.
        /// Store it in your own save file; pass it back to LoadState to restore. Returns an empty array if nothing to save.
        /// </summary>
        public byte[] SaveState (CompassSaveOptions options = null) {
            CompassStateDTO dto = BuildStateDTO(options ?? CompassSaveOptions.Default);
            return EncodeBinary(dto);
        }

        /// <summary>
        /// Same as SaveState but returns a JSON string (debuggable, larger). Fog payload is base64'd inside the JSON.
        /// </summary>
        public string SaveStateJson (CompassSaveOptions options = null) {
            CompassStateDTO dto = BuildStateDTO(options ?? CompassSaveOptions.Default);
            return JsonUtility.ToJson(dto);
        }

        /// <summary>
        /// Restores runtime state previously produced by SaveState. The save is authoritative: matched POIs are
        /// overwritten with their saved state, POIs missing from the scene are recreated, and live POIs absent from
        /// the save are removed. Call once the scene has loaded.
        /// </summary>
        public CompassLoadResult LoadState (byte[] data, CompassSaveOptions options = null) {
            if (data == null || data.Length == 0) return CompassLoadResult.Failed("Empty data.");
            if (!DecodeBinary(data, out CompassStateDTO dto, out string error)) return CompassLoadResult.Failed(error);
            return ApplyStateDTO(dto, options ?? CompassSaveOptions.Default);
        }

        /// <summary>
        /// Restores runtime state previously produced by SaveStateJson.
        /// </summary>
        public CompassLoadResult LoadStateJson (string json, CompassSaveOptions options = null) {
            if (string.IsNullOrEmpty(json)) return CompassLoadResult.Failed("Empty json.");
            CompassStateDTO dto;
            try {
                dto = JsonUtility.FromJson<CompassStateDTO>(json);
            } catch (Exception ex) {
                return CompassLoadResult.Failed("Invalid json: " + ex.Message);
            }
            if (dto == null) return CompassLoadResult.Failed("Invalid json.");
            if (dto.schemaVersion > SAVE_SCHEMA_VERSION) return CompassLoadResult.Failed("Save schema version " + dto.schemaVersion + " is newer than supported (" + SAVE_SCHEMA_VERSION + ").");
            return ApplyStateDTO(dto, options ?? CompassSaveOptions.Default);
        }

        #endregion


        #region Save/Load public API (static, all compasses)

        /// <summary>
        /// Saves the runtime state of every CompassPro instance in the scene into a single blob (one block per compass group, a shared POI list).
        /// </summary>
        public static byte[] SaveStateAll (CompassSaveOptions options = null) {
            options = options ?? CompassSaveOptions.Default;
            CompassStateDTO container = new CompassStateDTO { schemaVersion = SAVE_SCHEMA_VERSION };
            container.poisIncluded = (options.scope & (CompassSaveScope.POIVisited | CompassSaveScope.RuntimePOIs)) != 0;
            Dictionary<string, CompassPoiDTO> poiIndex = new Dictionary<string, CompassPoiDTO>();
            int compassCount = compasses.Count;
            for (int i = 0; i < compassCount; i++) {
                CompassPro compass = compasses[i];
                if (compass == null) continue;
                CompassStateDTO sub = compass.BuildStateDTO(options);
                if (sub.compasses.Count > 0) container.compasses.Add(sub.compasses[0]);
                for (int p = 0; p < sub.pois.Count; p++) {
                    CompassPoiDTO poiDto = sub.pois[p];
                    if (poiDto == null || string.IsNullOrEmpty(poiDto.key)) continue;
                    if (poiIndex.TryGetValue(poiDto.key, out CompassPoiDTO existing)) {
                        existing.visitedMask |= poiDto.visitedMask;
                    } else {
                        poiIndex[poiDto.key] = poiDto;
                        container.pois.Add(poiDto);
                    }
                }
            }
            return EncodeBinary(container);
        }

        /// <summary>
        /// Restores the runtime state of every CompassPro instance from a blob produced by SaveStateAll. Compasses are matched by group.
        /// </summary>
        public static CompassLoadResult LoadStateAll (byte[] data, CompassSaveOptions options = null) {
            options = options ?? CompassSaveOptions.Default;
            if (data == null || data.Length == 0) return CompassLoadResult.Failed("Empty data.");
            if (!DecodeBinary(data, out CompassStateDTO dto, out string error)) return CompassLoadResult.Failed(error);

            CompassLoadResult total = new CompassLoadResult { ok = true, schemaVersion = dto.schemaVersion };
            int compassCount = compasses.Count;
            for (int i = 0; i < compassCount; i++) {
                CompassPro compass = compasses[i];
                if (compass == null) continue;
                CompassLoadResult r = compass.ApplyStateDTO(dto, options);
                total.poisRestored += r.poisRestored;
                total.poisRecreated += r.poisRecreated;
                total.poisUnmatched += r.poisUnmatched;
                if (!r.ok) { total.ok = false; total.error = r.error; }
                if (!string.IsNullOrEmpty(r.warning)) total.warning = r.warning;
            }
            return total;
        }

        #endregion


        #region Save/Load - build DTO

        CompassStateDTO BuildStateDTO (CompassSaveOptions options) {
            CompassStateDTO dto = new CompassStateDTO { schemaVersion = SAVE_SCHEMA_VERSION };

            CompassInstanceDTO inst = new CompassInstanceDTO { compassGroup = _compassGroup };

            if ((options.scope & CompassSaveScope.MiniMapView) != 0) {
                inst.miniMapZoomLevel = _miniMapFullScreenState ? miniMapRegularZoomLevel : _miniMapZoomLevel;
                inst.miniMapFullScreenZoomLevel = _miniMapFullScreenZoomLevel;
                inst.miniMapFollowOffset = _miniMapFollowOffset;
                inst.miniMapFullScreenState = _miniMapFullScreenState;
            }

            if ((options.scope & CompassSaveScope.Route) != 0) {
                inst.route = BuildRouteDTO();
            }

            if ((options.scope & CompassSaveScope.Fog) != 0 && _fogOfWarEnabled) {
                inst.fog = BuildFogDTO();
            }

            if ((options.scope & CompassSaveScope.POIVisited) != 0 && _focusedPOI != null) {
                inst.focusedPoiKey = _focusedPOI.GetSaveKey();
            }

            dto.compasses.Add(inst);

            bool savePOIs = (options.scope & (CompassSaveScope.POIVisited | CompassSaveScope.RuntimePOIs)) != 0;
            dto.poisIncluded = savePOIs;
            if (savePOIs) {
                BuildPoiDTOs(dto.pois, options);
            }

            return dto;
        }

        void BuildPoiDTOs (List<CompassPoiDTO> outList, CompassSaveOptions options) {
            // The save is authoritative for the scene: capture every POI, including those a visit unregistered
            // (hideWhenVisited disables the component, which drops it from 'pois'). Scan the scene to catch them all.
            CompassProPOI[] all = Misc.FindObjectsOfType<CompassProPOI>(true);
            HashSet<string> seen = new HashSet<string>();
            for (int i = 0; i < all.Length; i++) {
                CompassProPOI poi = all[i];
                if (poi == null || poi.isRouteWaypoint) continue;
                string key = poi.GetSaveKey();
                if (string.IsNullOrEmpty(key) || !seen.Add(key)) continue;
                outList.Add(WritePoiDTO(poi, key));
            }
        }

        CompassPoiDTO WritePoiDTO (CompassProPOI poi, string key) {
            CompassPoiDTO d = new CompassPoiDTO {
                key = key,
                stableId = poi.StableId,
                id = poi.id,
                enabled = poi.enabled
            };

            int mask = 0;
            if (poi.states != null) {
                for (int g = 1; g <= CompassProPOI.MAX_COMPASS_GROUPS; g++) {
                    CompassProPOIState st = poi.states[g - 1];
                    if (st != null && st.isVisited) mask |= 1 << (g - 1);
                }
            }
            d.visitedMask = mask;

            Transform t = poi.transform;
            d.position = t.position;
            d.rotation = t.rotation;
            d.localScale = t.localScale;

            d.priority = poi.priority;
            d.compassGroupMask = poi.compassGroupMask;
            d.visibility = (int)poi.visibility;
            d.ignoreAreaOfInterest = poi.ignoreAreaOfInterest;
            d.clampPosition = poi.clampPosition;
            d.visibleDistanceOverride = poi.visibleDistanceOverride;
            d.visibleMinDistanceOverride = poi.visibleMinDistanceOverride;
            d.titleMinPOIDistanceOverride = poi.titleMinPOIDistanceOverride;
            d.title = poi.title;
            d.titleVisibility = (int)poi.titleVisibility;
            d.canBeVisited = poi.canBeVisited;
            d.visitedDistanceOverride = poi.visitedDistanceOverride;
            d.hideWhenVisited = poi.hideWhenVisited;
            d.visitedText = poi.visitedText;
            d.playAudioClipWhenVisited = poi.playAudioClipWhenVisited;
            d.radius = poi.radius;
            d.iconScale = poi.iconScale;
            d.iconScaleIsFixed = poi.iconScaleIsFixed;
            d.iconShowDistance = poi.iconShowDistance;
            d.tintColor = poi.tintColor;
            d.positionOffset = poi.positionOffset;
            d.dontDestroyOnLoad = poi.dontDestroyOnLoad;

            d.showOnScreenIndicator = poi.showOnScreenIndicator;
            d.onScreenIndicatorScale = poi.onScreenIndicatorScale;
            d.onScreenIndicatorShowDistance = poi.onScreenIndicatorShowDistance;
            d.onScreenIndicatorShowTitle = poi.onScreenIndicatorShowTitle;
            d.showSceneGizmo = poi.showSceneGizmo;
            d.showOffScreenIndicator = poi.showOffScreenIndicator;
            d.offScreenIndicatorShowDistance = poi.offScreenIndicatorShowDistance;
            d.offScreenIndicatorMarginOverride = poi.offScreenIndicatorMarginOverride;
            d.onScreenIndicatorNearFadeDistance = poi.onScreenIndicatorNearFadeDistance;
            d.onScreenIndicatorNearFadeMin = poi.onScreenIndicatorNearFadeMin;
            d.onScreenIndicatorFarDistance = poi.onScreenIndicatorFarDistance;
            d.onScreenIndicatorFarFadeDistance = poi.onScreenIndicatorFarFadeDistance;

            d.heartbeatEnabled = poi.heartbeatEnabled;
            d.heartbeatDistance = poi.heartbeatDistance;

            d.miniMapType = (int)poi.miniMapType;
            d.miniMapVisibility = (int)poi.miniMapVisibility;
            d.miniMapVisibleDistanceOverride = poi.miniMapVisibleDistanceOverride;
            d.miniMapClampPosition = poi.miniMapClampPosition;
            d.miniMapClampedScaleMultiplier = poi.miniMapClampedScaleMultiplier;
            d.miniMapShowRotation = poi.miniMapShowRotation;
            d.miniMapRotationAngleOffset = poi.miniMapRotationAngleOffset;
            d.miniMapIconScale = poi.miniMapIconScale;
            d.miniMapShowCircle = poi.miniMapShowCircle;
            d.miniMapCircleRadius = poi.miniMapCircleRadius;
            d.miniMapCircleColor = poi.miniMapCircleColor;
            d.miniMapCircleInnerColor = poi.miniMapCircleInnerColor;
            d.miniMapCircleStartRadius = poi.miniMapCircleStartRadius;
            d.miniMapCircleAnimationWhenAppears = poi.miniMapCircleAnimationWhenAppears;
            d.miniMapCircleAnimationRepetitions = poi.miniMapCircleAnimationRepetitions;

            d.iconNonVisitedName = AssetName(poi.iconNonVisited);
            d.iconVisitedName = AssetName(poi.iconVisited);
            d.visitedAudioClipName = AssetName(poi.visitedAudioClipOverride);
            d.beaconAudioClipName = AssetName(poi.beaconAudioClip);
            d.scanHitAudioClipName = AssetName(poi.scanHitAudioClip);
            d.heartbeatAudioClipName = AssetName(poi.heartbeatAudioClip);

            return d;
        }

        CompassRouteDTO BuildRouteDTO () {
            CompassRouteDTO r = new CompassRouteDTO();
            r.nextWaypoint = routeNextWaypoint;
            r.progress = routeProgressValue;
            r.startFollowsPlayer = routeStartFollowsPlayer;
            r.useWaypoints = _routeUseWaypoints;

            if (_routeUseWaypoints) {
                r.mode = ROUTE_MODE_WAYPOINTS;
            } else if (routePoints.Count < 2) {
                r.mode = ROUTE_MODE_NONE;
            } else if (routeStartFollowsPlayer && routeDestinationPOI != null) {
                r.mode = ROUTE_MODE_TO_POI;
                r.destinationPoiKey = routeDestinationPOI.GetSaveKey();
                r.points.Add(routePoints[routePoints.Count - 1]);
            } else if (routeStartFollowsPlayer) {
                r.mode = ROUTE_MODE_TO_DESTINATION;
                r.points.Add(routePoints[routePoints.Count - 1]);
            } else {
                r.mode = ROUTE_MODE_POINTS;
                for (int i = 0; i < routePoints.Count; i++) r.points.Add(routePoints[i]);
            }
            return r;
        }

        CompassFogDTO BuildFogDTO () {
            if (fogOfWarColorBuffer == null) return null;
            int n = fogOfWarColorBuffer.Length;
            byte[] alpha = new byte[n];
            for (int i = 0; i < n; i++) alpha[i] = fogOfWarColorBuffer[i].a;

            byte[] rle = RleEncode(alpha);
            bool useRle = rle.Length < n;

            return new CompassFogDTO {
                textureSize = _fogOfWarTextureSize,
                center = _fogOfWarCenter,
                sizeWS = _fogOfWarSize,
                color = _fogOfWarColor,
                encoding = useRle ? FOG_ENCODING_RLE : FOG_ENCODING_RAW,
                payload = useRle ? rle : alpha
            };
        }

        #endregion


        #region Save/Load - apply DTO

        CompassLoadResult ApplyStateDTO (CompassStateDTO dto, CompassSaveOptions options) {
            if (dto == null) return CompassLoadResult.Failed("No state.");
            if (dto.schemaVersion > SAVE_SCHEMA_VERSION) {
                return CompassLoadResult.Failed("Save schema version " + dto.schemaVersion + " is newer than supported (" + SAVE_SCHEMA_VERSION + ").");
            }
            if (dto.schemaVersion < MIN_SUPPORTED_SCHEMA) {
                return CompassLoadResult.Failed("Save schema version " + dto.schemaVersion + " is from an older incompatible format; create a new save.");
            }

            CompassLoadResult result = new CompassLoadResult { ok = true, schemaVersion = dto.schemaVersion };
            assetPoolCache = null; // resolve asset names against whatever is loaded right now

            CompassInstanceDTO inst = FindInstanceForGroup(dto, _compassGroup);

            // Reconcile the scene's POIs against the save (authoritative): overwrite matches, recreate missing,
            // prune POIs that aren't in the save. Returns the live index used afterwards for route/focus lookups.
            bool poiScope = (options.scope & (CompassSaveScope.POIVisited | CompassSaveScope.RuntimePOIs)) != 0;
            Dictionary<string, CompassProPOI> liveIndex = poiScope
                ? ReconcilePOIs(dto, ref result)
                : BuildLiveIndex();

            // Focused POI
            if ((options.scope & CompassSaveScope.POIVisited) != 0 && inst != null) {
                if (!string.IsNullOrEmpty(inst.focusedPoiKey) && liveIndex.TryGetValue(inst.focusedPoiKey, out CompassProPOI focused)) {
                    _focusedPOI = focused;
                    focused.showOnScreenIndicator = true;
                    needUpdateCompassBarIcons = true;
                }
            }

            if (inst != null) {
                if ((options.scope & CompassSaveScope.Fog) != 0 && inst.fog != null) {
                    ApplyFogDTO(inst.fog, ref result);
                }
                if ((options.scope & CompassSaveScope.MiniMapView) != 0) {
                    ApplyMiniMapView(inst, options);
                }
                if ((options.scope & CompassSaveScope.Route) != 0 && inst.route != null) {
                    ApplyRouteDTO(inst.route, liveIndex, options);
                }
            }

            // Single reconcile pass (no gameplay events)
            Refresh();
            UpdateMiniMapContents();
            needUpdateRoute = true;

            return result;
        }

        CompassInstanceDTO FindInstanceForGroup (CompassStateDTO dto, int group) {
            if (dto.compasses == null) return null;
            for (int i = 0; i < dto.compasses.Count; i++) {
                if (dto.compasses[i] != null && dto.compasses[i].compassGroup == group) return dto.compasses[i];
            }
            // Single-compass saves: fall back to the only block present
            if (dto.compasses.Count == 1) return dto.compasses[0];
            return null;
        }

        // Live index of every POI in the scene by save key, including ones a visit unregistered (component disabled).
        Dictionary<string, CompassProPOI> BuildLiveIndex () {
            Dictionary<string, CompassProPOI> liveIndex = new Dictionary<string, CompassProPOI>();
            CompassProPOI[] all = Misc.FindObjectsOfType<CompassProPOI>(true);
            for (int i = 0; i < all.Length; i++) {
                CompassProPOI poi = all[i];
                if (poi == null || poi.isRouteWaypoint) continue;
                string k = poi.GetSaveKey();
                if (!string.IsNullOrEmpty(k) && !liveIndex.ContainsKey(k)) liveIndex[k] = poi;
            }
            return liveIndex;
        }

        // POI reconciliation is global (POIs are shared objects, not per-compass): when several compasses load the
        // same blob the first pass makes the scene match the save and later passes are idempotent.
        Dictionary<string, CompassProPOI> ReconcilePOIs (CompassStateDTO dto, ref CompassLoadResult result) {
            CompassProPOI[] all = Misc.FindObjectsOfType<CompassProPOI>(true);
            Dictionary<string, CompassProPOI> liveIndex = new Dictionary<string, CompassProPOI>();
            for (int i = 0; i < all.Length; i++) {
                CompassProPOI poi = all[i];
                if (poi == null || poi.isRouteWaypoint) continue;
                string k = poi.GetSaveKey();
                if (!string.IsNullOrEmpty(k) && !liveIndex.ContainsKey(k)) liveIndex[k] = poi;
            }

            HashSet<string> savedKeys = new HashSet<string>();
            if (dto.pois != null) {
                for (int i = 0; i < dto.pois.Count; i++) {
                    CompassPoiDTO d = dto.pois[i];
                    if (d == null || string.IsNullOrEmpty(d.key)) continue;
                    savedKeys.Add(d.key);

                    CompassProPOI poi;
                    if (!liveIndex.TryGetValue(d.key, out poi) || poi == null) {
                        poi = RecreatePOI(d);
                        if (poi == null) { result.poisUnmatched++; continue; }
                        liveIndex[d.key] = poi;
                        result.poisRecreated++;
                    }
                    ApplyFullState(poi, d);
                    result.poisRestored++;
                }
            }

            // Prune: the save is authoritative, so any live POI not present in it shouldn't exist (e.g. a re-spawn).
            // Guarded by poisIncluded so a save made without POI scope (empty pois list) can't wipe the scene on load.
            if (dto.poisIncluded) {
                for (int i = 0; i < all.Length; i++) {
                    CompassProPOI poi = all[i];
                    if (poi == null || poi.isRouteWaypoint) continue;
                    string k = poi.GetSaveKey();
                    if (string.IsNullOrEmpty(k) || savedKeys.Contains(k)) continue;
                    Misc.DestroySafe(poi.gameObject);
                }
            }

            return liveIndex;
        }

        CompassProPOI RecreatePOI (CompassPoiDTO d) {
            GameObject go = null;
            if (poiFactory != null) {
                go = poiFactory.CreatePOIObject(d);
            }
            if (go == null) {
                go = new GameObject(string.IsNullOrEmpty(d.title) ? "Compass POI" : d.title);
                go.transform.position = d.position;
                go.transform.rotation = d.rotation;
            }

            CompassProPOI poi = go.GetComponent<CompassProPOI>();
            if (poi == null) poi = go.AddComponent<CompassProPOI>();
            poi.StableId = string.IsNullOrEmpty(d.stableId) ? d.key : d.stableId;
            return poi;
        }

        // Overwrites the POI with the full saved state. enabled is applied last because toggling it
        // (un)registers the POI via OnEnable/OnDisable.
        void ApplyFullState (CompassProPOI poi, CompassPoiDTO d) {
            if (!string.IsNullOrEmpty(d.stableId)) poi.StableId = d.stableId;
            if (d.id != 0) poi.id = d.id;

            Transform t = poi.transform;
            t.position = d.position;
            t.rotation = d.rotation;
            if (d.localScale != Vector3.zero) t.localScale = d.localScale;

            poi.priority = d.priority;
            if (d.compassGroupMask != 0) poi.compassGroupMask = d.compassGroupMask;
            poi.visibility = (POIVisibility)d.visibility;
            poi.ignoreAreaOfInterest = d.ignoreAreaOfInterest;
            poi.clampPosition = d.clampPosition;
            poi.visibleDistanceOverride = d.visibleDistanceOverride;
            poi.visibleMinDistanceOverride = d.visibleMinDistanceOverride;
            poi.titleMinPOIDistanceOverride = d.titleMinPOIDistanceOverride;
            poi.title = d.title;
            poi.titleVisibility = (TitleVisibility)d.titleVisibility;
            poi.canBeVisited = d.canBeVisited;
            poi.visitedDistanceOverride = d.visitedDistanceOverride;
            poi.hideWhenVisited = d.hideWhenVisited;
            poi.visitedText = d.visitedText;
            poi.playAudioClipWhenVisited = d.playAudioClipWhenVisited;
            poi.radius = d.radius;
            poi.iconScale = d.iconScale;
            poi.iconScaleIsFixed = d.iconScaleIsFixed;
            poi.iconShowDistance = d.iconShowDistance;
            poi.tintColor = d.tintColor;
            poi.positionOffset = d.positionOffset;
            poi.dontDestroyOnLoad = d.dontDestroyOnLoad;

            poi.showOnScreenIndicator = d.showOnScreenIndicator;
            poi.onScreenIndicatorScale = d.onScreenIndicatorScale;
            poi.onScreenIndicatorShowDistance = d.onScreenIndicatorShowDistance;
            poi.onScreenIndicatorShowTitle = d.onScreenIndicatorShowTitle;
            poi.showSceneGizmo = d.showSceneGizmo;
            poi.showOffScreenIndicator = d.showOffScreenIndicator;
            poi.offScreenIndicatorShowDistance = d.offScreenIndicatorShowDistance;
            poi.offScreenIndicatorMarginOverride = d.offScreenIndicatorMarginOverride;
            poi.onScreenIndicatorNearFadeDistance = d.onScreenIndicatorNearFadeDistance;
            poi.onScreenIndicatorNearFadeMin = d.onScreenIndicatorNearFadeMin;
            poi.onScreenIndicatorFarDistance = d.onScreenIndicatorFarDistance;
            poi.onScreenIndicatorFarFadeDistance = d.onScreenIndicatorFarFadeDistance;

            poi.heartbeatEnabled = d.heartbeatEnabled;
            poi.heartbeatDistance = d.heartbeatDistance;

            poi.miniMapType = (POIMiniMapType)d.miniMapType;
            poi.miniMapVisibility = (POIVisibility)d.miniMapVisibility;
            poi.miniMapVisibleDistanceOverride = d.miniMapVisibleDistanceOverride;
            poi.miniMapClampPosition = d.miniMapClampPosition;
            poi.miniMapClampedScaleMultiplier = d.miniMapClampedScaleMultiplier;
            poi.miniMapShowRotation = d.miniMapShowRotation;
            poi.miniMapRotationAngleOffset = d.miniMapRotationAngleOffset;
            poi.miniMapIconScale = d.miniMapIconScale;
            poi.miniMapShowCircle = d.miniMapShowCircle;
            poi.miniMapCircleRadius = d.miniMapCircleRadius;
            poi.miniMapCircleColor = d.miniMapCircleColor;
            poi.miniMapCircleInnerColor = d.miniMapCircleInnerColor;
            poi.miniMapCircleStartRadius = d.miniMapCircleStartRadius;
            poi.miniMapCircleAnimationWhenAppears = d.miniMapCircleAnimationWhenAppears;
            poi.miniMapCircleAnimationRepetitions = d.miniMapCircleAnimationRepetitions;

            // Asset refs re-resolve by name against loaded assets; keep the current asset when unresolved so authored
            // scene references aren't wiped by a portable blob that can't carry object references.
            Sprite iconNV = ResolveAssetByName<Sprite>(d.iconNonVisitedName); if (iconNV != null) poi.iconNonVisited = iconNV;
            Sprite iconV = ResolveAssetByName<Sprite>(d.iconVisitedName); if (iconV != null) poi.iconVisited = iconV;
            AudioClip visitedClip = ResolveAssetByName<AudioClip>(d.visitedAudioClipName); if (visitedClip != null) poi.visitedAudioClipOverride = visitedClip;
            AudioClip beaconClip = ResolveAssetByName<AudioClip>(d.beaconAudioClipName); if (beaconClip != null) poi.beaconAudioClip = beaconClip;
            AudioClip scanClip = ResolveAssetByName<AudioClip>(d.scanHitAudioClipName); if (scanClip != null) poi.scanHitAudioClip = scanClip;
            AudioClip heartbeatClip = ResolveAssetByName<AudioClip>(d.heartbeatAudioClipName); if (heartbeatClip != null) poi.heartbeatAudioClip = heartbeatClip;

            ApplyVisitedMask(poi, d.visitedMask);

            if (poi.enabled != d.enabled) poi.enabled = d.enabled;
        }

        // Sets the visited flag per group without firing OnPOIVisited and without touching enabled (the caller
        // applies the authoritative enabled state afterwards).
        void ApplyVisitedMask (CompassProPOI poi, int mask) {
            for (int g = 1; g <= CompassProPOI.MAX_COMPASS_GROUPS; g++) {
                bool visited = (mask & (1 << (g - 1))) != 0;
                poi.GetState(g).isVisited = visited;
            }
        }

        void ApplyFogDTO (CompassFogDTO fog, ref CompassLoadResult result) {
            if (fog == null || fog.payload == null) return;

            byte[] alpha;
            if (fog.encoding == FOG_ENCODING_RLE) {
                alpha = RleDecode(fog.payload, fog.textureSize * fog.textureSize);
            } else {
                alpha = fog.payload;
            }
            if (alpha == null) { result.warning = "Fog payload could not be decoded."; return; }

            int savedN = fog.textureSize * fog.textureSize;
            if (alpha.Length != savedN) { result.warning = "Fog payload size mismatch; fog not restored."; return; }

            bool boundsMatch = fog.textureSize == _fogOfWarTextureSize
                && Approximately(fog.center, _fogOfWarCenter)
                && Approximately(fog.sizeWS, _fogOfWarSize);

            if (!boundsMatch) {
                result.warning = "Fog of war bounds/resolution differ from the current world; fog not restored.";
                return;
            }

            Color32[] buffer = new Color32[savedN];
            for (int i = 0; i < savedN; i++) {
                byte a = alpha[i];
                buffer[i] = new Color32(a, a, a, a);
            }
            fogOfWarTextureData = buffer;
        }

        void ApplyMiniMapView (CompassInstanceDTO inst, CompassSaveOptions options) {
            // Set backing fields directly to avoid firing OnMiniMapChangeFullScreenState.
            _miniMapFullScreenZoomLevel = inst.miniMapFullScreenZoomLevel;
            _miniMapFollowOffset = inst.miniMapFollowOffset;

            bool wantFullScreen = inst.miniMapFullScreenState;
            if (wantFullScreen != _miniMapFullScreenState) {
                if (options.fireEventsOnLoad) OnMiniMapChangeFullScreenState?.Invoke(wantFullScreen);
                // Set the regular zoom first so the toggle stashes/uses the right values
                _miniMapZoomLevel = inst.miniMapZoomLevel;
                miniMapRegularZoomLevel = inst.miniMapZoomLevel;
                MiniMapZoomToggle(wantFullScreen);
            } else {
                if (_miniMapFullScreenState) {
                    miniMapRegularZoomLevel = inst.miniMapZoomLevel;
                    _miniMapZoomLevel = inst.miniMapFullScreenZoomLevel;
                } else {
                    _miniMapZoomLevel = inst.miniMapZoomLevel;
                }
            }
        }

        void ApplyRouteDTO (CompassRouteDTO r, Dictionary<string, CompassProPOI> liveIndex, CompassSaveOptions options) {
            switch (r.mode) {
                case ROUTE_MODE_NONE:
                    routePoints.Clear();
                    routeStartFollowsPlayer = false;
                    routeDestinationPOI = null;
                    routeNextWaypoint = 0;
                    break;
                case ROUTE_MODE_WAYPOINTS:
                    _routeUseWaypoints = true;
                    // Keep the waypoint trackers in sync so RefreshRouteFromWaypoints doesn't reset the restored progress next frame
                    lastRouteUseWaypoints = true;
                    lastWaypointCount = _routeWaypoints.Count;
                    routeNextWaypoint = r.nextWaypoint;
                    break;
                case ROUTE_MODE_TO_POI:
                    RouteApiTakeover();
                    routePoints.Clear();
                    routePoints.Add(followPos);
                    CompassProPOI destPoi = null;
                    if (!string.IsNullOrEmpty(r.destinationPoiKey)) liveIndex.TryGetValue(r.destinationPoiKey, out destPoi);
                    if (destPoi != null) {
                        routePoints.Add(destPoi.transform.position);
                        routeDestinationPOI = destPoi;
                    } else if (r.points.Count > 0) {
                        routePoints.Add(r.points[r.points.Count - 1]);
                        routeDestinationPOI = null;
                    }
                    routeStartFollowsPlayer = true;
                    RecomputeRouteWorldArc();
                    break;
                case ROUTE_MODE_TO_DESTINATION:
                    RouteApiTakeover();
                    routePoints.Clear();
                    routePoints.Add(followPos);
                    if (r.points.Count > 0) routePoints.Add(r.points[r.points.Count - 1]);
                    routeStartFollowsPlayer = true;
                    routeDestinationPOI = null;
                    RecomputeRouteWorldArc();
                    break;
                case ROUTE_MODE_POINTS:
                default:
                    RouteApiTakeover();
                    routePoints.Clear();
                    for (int i = 0; i < r.points.Count; i++) routePoints.Add(r.points[i]);
                    routeStartFollowsPlayer = false;
                    routeDestinationPOI = null;
                    RecomputeRouteWorldArc();
                    break;
            }

            // Overwrite progress/index after the rebuild (SetRoute* would have reset them)
            routeNextWaypoint = r.nextWaypoint;
            routeProgressValue = Mathf.Clamp01(r.progress);
            needUpdateRoute = true;

            if (options.fireEventsOnLoad && r.mode != ROUTE_MODE_NONE) OnRouteUpdated?.Invoke();
        }

        #endregion


        #region Save/Load - binary codec

        static byte[] EncodeBinary (CompassStateDTO dto) {
            string json = JsonUtility.ToJson(dto);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms)) {
                bw.Write(SAVE_MAGIC);
                bw.Write((ushort)SAVE_SCHEMA_VERSION);
                bw.Write((ushort)0); // flags (reserved)
                bw.Write(jsonBytes.Length);
                bw.Write(jsonBytes);
                bw.Flush();
                return ms.ToArray();
            }
        }

        static bool DecodeBinary (byte[] data, out CompassStateDTO dto, out string error) {
            dto = null;
            error = null;
            try {
                using (MemoryStream ms = new MemoryStream(data))
                using (BinaryReader br = new BinaryReader(ms)) {
                    byte[] magic = br.ReadBytes(4);
                    if (magic.Length != 4 || magic[0] != SAVE_MAGIC[0] || magic[1] != SAVE_MAGIC[1] || magic[2] != SAVE_MAGIC[2] || magic[3] != SAVE_MAGIC[3]) {
                        error = "Unrecognized data (bad magic).";
                        return false;
                    }
                    ushort version = br.ReadUInt16();
                    br.ReadUInt16(); // flags
                    if (version > SAVE_SCHEMA_VERSION) {
                        error = "Save schema version " + version + " is newer than supported (" + SAVE_SCHEMA_VERSION + ").";
                        return false;
                    }
                    int len = br.ReadInt32();
                    if (len < 0 || len > ms.Length) {
                        error = "Corrupt data (bad length).";
                        return false;
                    }
                    byte[] jsonBytes = br.ReadBytes(len);
                    string json = Encoding.UTF8.GetString(jsonBytes);
                    dto = JsonUtility.FromJson<CompassStateDTO>(json);
                    if (dto == null) {
                        error = "Corrupt data (deserialization failed).";
                        return false;
                    }
                    return true;
                }
            } catch (Exception ex) {
                error = "Decode error: " + ex.Message;
                return false;
            }
        }

        #endregion


        #region Save/Load - RLE codec (fog alpha bytes)

        // Simple byte RLE: [count(1..255)][value] repeated. Long runs split into chunks of 255.
        static byte[] RleEncode (byte[] src) {
            if (src == null || src.Length == 0) return Array.Empty<byte>();
            using (MemoryStream ms = new MemoryStream(src.Length / 2 + 16)) {
                int i = 0;
                int n = src.Length;
                while (i < n) {
                    byte v = src[i];
                    int run = 1;
                    while (i + run < n && src[i + run] == v && run < 255) run++;
                    ms.WriteByte((byte)run);
                    ms.WriteByte(v);
                    i += run;
                }
                return ms.ToArray();
            }
        }

        static byte[] RleDecode (byte[] src, int expectedLength) {
            if (src == null || (src.Length & 1) != 0) return null;
            byte[] dst = new byte[expectedLength];
            int di = 0;
            int i = 0;
            int n = src.Length;
            while (i + 1 < n && di < expectedLength) {
                int run = src[i];
                byte v = src[i + 1];
                i += 2;
                for (int k = 0; k < run && di < expectedLength; k++) {
                    dst[di++] = v;
                }
            }
            if (di != expectedLength) return null;
            return dst;
        }

        #endregion


        #region Save/Load - helpers

        static bool Approximately (Vector3 a, Vector3 b) {
            return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y) && Mathf.Approximately(a.z, b.z);
        }

        #endregion

    }

}
