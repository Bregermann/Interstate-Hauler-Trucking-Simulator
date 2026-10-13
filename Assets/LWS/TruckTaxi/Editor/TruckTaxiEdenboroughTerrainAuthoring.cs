using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Editor
{
    /// <summary>
    /// Explicit editor bake of the fictional, non-GIS Layout heightfield. No scene opening/saving,
    /// runtime manager, foliage generator, or vendor mutation. Call Build once from main authoring.
    /// Vista's UnityTerrainSystem grid/SetNeighbors and TerrainGridFactory asset patterns are reused,
    /// but its wizard saves scenes and requires a VistaManager/graph. Native TerrainData is needed
    /// here to keep the semantic Height and sampled road grade as the single geometry authority.
    /// </summary>
    public static class TruckTaxiEdenboroughTerrainAuthoring
    {
        public const int Columns = 3, Rows = 2, HeightmapResolution = 1025, AlphamapResolution = 512;
        public const float VerticalRange = 600, RoadInset = .12f, EmbankmentBlend = 48, RoadGridCellSize = 64;
        public const float PadShoulderBlend = 24;
        private const string OwnerFile = "EdenboroughTerrainOwner.asset";
        private const string OwnerSignature = "LWS.EdenboroughTerrain/v1/" + TruckTaxiEdenboroughLayout.WorldId;
        private const string TerrainShader = "Universal Render Pipeline/Terrain/Lit";
        private const string MaterialFile = "EdenboroughTerrain.mat";
        private static readonly string[] LayerNames = { "GrassDark", "GrassLight", "Dirt", "Rock" };
        // Inspected owned sources: each TerrainLayer has existing diffuse and normal textures.
        // Copies below keep the source texture references; only output-folder copies are configured.
        private static readonly string[] LayerSources =
        {
            "Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/Grass Dark/Grass_Dark_VistaSample.terrainlayer",
            "Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/Grass Light/Grass_Light_VistaSample.terrainlayer",
            "Assets/NWH/Common/TerrainLayers/DirtLayer.asset",
            "Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/Rock Base/Rock_Base_VistaSample.terrainlayer"
        };

        public static Vector3 TileSize => new Vector3(TruckTaxiEdenboroughLayout.WorldBounds.size.x / Columns,
            VerticalRange, TruckTaxiEdenboroughLayout.WorldBounds.size.z / Rows);
        // A padded core covers vertices whose bilinear terrain cells overlap even a narrow street.
        public static float RoadSamplePadding => new Vector2(TileSize.x, TileSize.z).magnitude / (HeightmapResolution - 1);

        public static Bounds TileBounds(int column, int row)
        {
            if (column < 0 || column >= Columns || row < 0 || row >= Rows) throw new ArgumentOutOfRangeException();
            var min = TruckTaxiEdenboroughLayout.WorldBounds.min + new Vector3(column * TileSize.x, 0, row * TileSize.z);
            return new Bounds(min + TileSize * .5f, TileSize);
        }

        /// <summary>
        /// Returns six terrains, row-major (row * Columns + column). assetFolder must be a new/empty
        /// dedicated child of Assets/LWS/TruckTaxi, or this builder's previously marked folder.
        /// Parent must be in an unsaved authoring scene or the exact Edenborough ScenePath, with
        /// identity world rotation/scale. World positions come from Layout, not the parent pivot.
        /// Stable assets/objects are reused on rerun; only generated assets are saved, never scenes.
        /// Optional pads are world-axis-aligned rectangles: center.y is the saved finished terrain
        /// surface height (no RoadInset), size.x/z are the flat footprint, size.y is ignored.
        /// Their padded cores blend over 24m; overlapping road cores retain collider clearance.
        /// Pass service/garage bounds explicitly every bake. No pads or stop positions are inferred.
        /// </summary>
        public static Terrain[] Build(Transform parent, string assetFolder, IReadOnlyList<Bounds> pads = null)
        {
            ValidateParent(parent);
            assetFolder = NormalizeAssetFolder(assetFolder);
            ValidateOwnership(assetFolder);
            ValidateObjects(parent, assetFolder);
            var shader = Shader.Find(TerrainShader);
            if (shader == null) throw new InvalidOperationException("Installed URP Terrain/Lit shader is unavailable.");
            var sources = LoadSources();
            var heightfield = new RoadHeightfield(TruckTaxiEdenboroughLayout.Roads, pads);
            EnsureFolder(assetFolder);
            if (AssetDatabase.LoadMainAssetAtPath(assetFolder + "/" + OwnerFile) == null)
                AssetDatabase.CreateAsset(new TextAsset(OwnerSignature), assetFolder + "/" + OwnerFile);

            Undo.IncrementCurrentGroup();
            var layers = CopyLayers(assetFolder, sources);
            var material = LoadOrCreate(assetFolder + "/" + MaterialFile, () => new Material(shader));
            Undo.RecordObject(material, "Configure Edenborough terrain material");
            material.shader = shader; material.enableInstancing = true;
            SaveGenerated(material);
            var tiles = new Terrain[Columns * Rows];
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Bake Edenborough terrain");
            try
            {
                for (int row = 0; row < Rows; row++)
                    for (int column = 0; column < Columns; column++)
                    {
                        int index = row * Columns + column;
                        EditorUtility.DisplayProgressBar("Edenborough terrain", "Baking tile " + (index + 1) + "/" + tiles.Length, index / (float)tiles.Length);
                        string path = TilePath(assetFolder, column, row);
                        var data = LoadOrCreate(path, () => new TerrainData());
                        Undo.RegisterCompleteObjectUndo(data, "Bake Edenborough height and texture maps");
                        data.heightmapResolution = HeightmapResolution; data.size = TileSize;
                        data.alphamapResolution = AlphamapResolution; data.baseMapResolution = 1024; data.terrainLayers = layers;
                        var heights = Heights(heightfield, column, row);
                        data.SetHeights(0, 0, heights);
                        data.SetAlphamaps(0, 0, Alphamaps(heightfield, heights, column, row));
                        SaveGenerated(data);

                        var tile = FindTile(parent, TileName(column, row));
                        if (tile == null)
                        {
                            var go = CreateTerrainObject(data, parent.gameObject.scene);
                            go.name = TileName(column, row); go.transform.SetParent(parent, false);
                            Undo.RegisterCreatedObjectUndo(go, "Create Edenborough terrain tile");
                            tile = go.GetComponent<Terrain>();
                        }
                        Undo.RecordObject(tile, "Configure Edenborough terrain");
                        Undo.RecordObject(tile.transform, "Position Edenborough terrain");
                        var collider = tile.GetComponent<TerrainCollider>();
                        Undo.RecordObject(collider, "Configure Edenborough terrain collider");
                        tile.terrainData = data; collider.terrainData = data;
                        tile.transform.position = TileBounds(column, row).min;
                        tile.transform.rotation = Quaternion.identity; tile.transform.localScale = Vector3.one;
                        tile.materialTemplate = material; tile.drawInstanced = true;
                        tile.heightmapPixelError = 5; tile.basemapDistance = 2000;
                        // Native auto-connect restores this group after scene reload without a runtime manager.
                        tile.allowAutoConnect = true; tile.groupingID = 91417;
                        // No tree/detail prototypes or instances are authored; main owns foliage/dressing.
                        tiles[index] = tile;
                    }
                for (int row = 0; row < Rows; row++)
                    for (int column = 0; column < Columns; column++)
                    {
                        var tile = tiles[row * Columns + column];
                        tile.SetNeighbors(Neighbor(tiles, column - 1, row), Neighbor(tiles, column, row + 1),
                            Neighbor(tiles, column + 1, row), Neighbor(tiles, column, row - 1));
                        tile.Flush(); EditorUtility.SetDirty(tile);
                    }
                return tiles;
            }
            finally { EditorUtility.ClearProgressBar(); Undo.CollapseUndoOperations(undoGroup); }
        }

        public readonly struct SurfaceSample
        {
            public readonly float Height, RoadBlend, PadBlend;
            public SurfaceSample(float height, float blend, float padBlend = 0) { Height = height; RoadBlend = blend; PadBlend = padBlend; }
        }

        /// <summary>Build once per editor bake. Sampling allocates nothing and inspects only the local grid bucket.</summary>
        public sealed class RoadHeightfield
        {
            private readonly struct Segment
            {
                public readonly Vector3 A, B;
                public readonly float Dx, Dz, LengthSquared, Core, Radius;
                public Segment(Vector3 a, Vector3 b, float width)
                {
                    A = a; B = b; Dx = b.x - a.x; Dz = b.z - a.z; LengthSquared = Dx * Dx + Dz * Dz;
                    Core = width * .5f + RoadSamplePadding; Radius = Core + EmbankmentBlend;
                }
            }
            private readonly List<Segment> segments = new List<Segment>();
            private readonly Dictionary<Vector2Int, List<int>> cells = new Dictionary<Vector2Int, List<int>>();
            private readonly List<Bounds> padBounds = new List<Bounds>();
            private readonly Dictionary<Vector2Int, List<int>> padCells = new Dictionary<Vector2Int, List<int>>();
            public int SegmentCount => segments.Count;

            public RoadHeightfield(IEnumerable<TruckTaxiEdenboroughLayout.Road> roads, IReadOnlyList<Bounds> pads = null)
            {
                if (roads == null) throw new ArgumentNullException(nameof(roads));
                if (pads != null)
                    foreach (var pad in pads)
                    {
                        var world = TruckTaxiEdenboroughLayout.WorldBounds;
                        if (!Finite(pad.center) || !Finite(pad.size) || pad.size.x <= 0 || pad.size.z <= 0 || pad.size.y < 0 ||
                            pad.center.y < world.min.y || pad.center.y > world.max.y || pad.min.x < world.min.x ||
                            pad.max.x > world.max.x || pad.min.z < world.min.z || pad.max.z > world.max.z)
                            throw new ArgumentException("Pad must have a finite positive XZ footprint inside WorldBounds and a saved surface height within its vertical range.");
                        int index = padBounds.Count; padBounds.Add(pad);
                        float margin = RoadSamplePadding + PadShoulderBlend;
                        IndexCells(padCells, Cell(pad.min.x - margin, pad.min.z - margin),
                            Cell(pad.max.x + margin, pad.max.z + margin), index);
                    }
                foreach (var road in roads)
                {
                    if (road.Points == null || !float.IsFinite(road.Width) || road.Width <= 0)
                        throw new ArgumentException("Road has invalid points/width: " + road.Id);
                    foreach (var point in road.Points)
                        if (!Finite(point)) throw new ArgumentException("Road has a non-finite sample: " + road.Id);
                    for (int i = 1; i < road.Points.Length; i++)
                    {
                        var segment = new Segment(road.Points[i - 1], road.Points[i], road.Width);
                        if (segment.LengthSquared < .0001f) continue;
                        int index = segments.Count; segments.Add(segment);
                        var min = Cell(Mathf.Min(segment.A.x, segment.B.x) - segment.Radius, Mathf.Min(segment.A.z, segment.B.z) - segment.Radius);
                        var max = Cell(Mathf.Max(segment.A.x, segment.B.x) + segment.Radius, Mathf.Max(segment.A.z, segment.B.z) + segment.Radius);
                        IndexCells(cells, min, max, index);
                    }
                }
            }
            public SurfaceSample Sample(float x, float z)
            {
                float natural = TruckTaxiEdenboroughLayout.Height(x, z);
                var cell = Cell(x, z);
                cells.TryGetValue(cell, out var candidates);
                float bestBlend = 0, height = natural, coreHeight = float.PositiveInfinity;
                if (candidates != null)
                foreach (int index in candidates)
                {
                    var segment = segments[index];
                    float gradeT = ((x - segment.A.x) * segment.Dx + (z - segment.A.z) * segment.Dz) / segment.LengthSquared;
                    float t = Mathf.Clamp01(gradeT);
                    float dx = x - (segment.A.x + t * segment.Dx), dz = z - (segment.A.z + t * segment.Dz);
                    float distanceSquared = dx * dx + dz * dz;
                    if (distanceSquared > segment.Radius * segment.Radius) continue;
                    // Extend the local grade into padding: flat endpoint caps can poke above the
                    // road when Terrain bilinearly interpolates vertices on either side of an endpoint.
                    float target = Mathf.LerpUnclamped(segment.A.y, segment.B.y, gradeT) - RoadInset;
                    if (distanceSquared <= segment.Core * segment.Core)
                    {
                        // At junctions keep the terrain below every overlapping road collider, not an average above one.
                        coreHeight = Mathf.Min(coreHeight, target); bestBlend = 1; continue;
                    }
                    float blend = 1 - Mathf.SmoothStep(0, 1, (Mathf.Sqrt(distanceSquared) - segment.Core) / EmbankmentBlend);
                    float candidateHeight = Mathf.Lerp(natural, target, blend);
                    if (blend > bestBlend || Mathf.Approximately(blend, bestBlend) && candidateHeight < height)
                    { bestBlend = blend; height = candidateHeight; }
                }
                bool roadCore = !float.IsPositiveInfinity(coreHeight);
                if (roadCore) height = coreHeight;
                height = ApplyPads(cell, x, z, height, out float padBlend);
                if (roadCore) height = Mathf.Min(height, coreHeight);
                return new SurfaceSample(height, bestBlend, padBlend);
            }
            private float ApplyPads(Vector2Int cell, float x, float z, float underlying, out float bestBlend)
            {
                bestBlend = 0;
                if (!padCells.TryGetValue(cell, out var candidates)) return underlying;
                float height = underlying, coreHeight = float.PositiveInfinity;
                float padding = RoadSamplePadding;
                foreach (int index in candidates)
                {
                    var pad = padBounds[index];
                    // Full sample padding keeps all terrain vertices supporting the rectangle flat.
                    float dx = Mathf.Max(0, Mathf.Abs(x - pad.center.x) - pad.extents.x - padding);
                    float dz = Mathf.Max(0, Mathf.Abs(z - pad.center.z) - pad.extents.z - padding);
                    float distanceSquared = dx * dx + dz * dz;
                    if (distanceSquared > PadShoulderBlend * PadShoulderBlend) continue;
                    if (distanceSquared == 0)
                    { coreHeight = Mathf.Min(coreHeight, pad.center.y); bestBlend = 1; continue; }
                    float blend = 1 - Mathf.SmoothStep(0, 1, Mathf.Sqrt(distanceSquared) / PadShoulderBlend);
                    float candidateHeight = Mathf.Lerp(underlying, pad.center.y, blend);
                    if (blend > bestBlend || Mathf.Approximately(blend, bestBlend) && candidateHeight < height)
                    { bestBlend = blend; height = candidateHeight; }
                }
                // Incompatible overlapping pad heights resolve to the lower surface deterministically.
                return float.IsPositiveInfinity(coreHeight) ? height : coreHeight;
            }
            private static void IndexCells(Dictionary<Vector2Int, List<int>> grid, Vector2Int min, Vector2Int max, int index)
            {
                for (int z = min.y; z <= max.y; z++) for (int x = min.x; x <= max.x; x++)
                {
                    var key = new Vector2Int(x, z);
                    if (!grid.TryGetValue(key, out var list)) { list = new List<int>(); grid.Add(key, list); }
                    list.Add(index);
                }
            }
            private static Vector2Int Cell(float x, float z) => new Vector2Int(Mathf.FloorToInt(x / RoadGridCellSize), Mathf.FloorToInt(z / RoadGridCellSize));
            private static bool Finite(Vector3 p) => float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z);
        }

        private static float[,] Heights(RoadHeightfield field, int column, int row)
        {
            int intervals = HeightmapResolution - 1;
            var heights = new float[HeightmapResolution, HeightmapResolution];
            float baseY = TruckTaxiEdenboroughLayout.WorldBounds.min.y;
            var step = new Vector2(TileSize.x / intervals, TileSize.z / intervals);
            // Global integer sample indices guarantee bit-identical shared tile-edge heights.
            for (int z = 0; z <= intervals; z++) for (int x = 0; x <= intervals; x++)
            {
                float wx = TruckTaxiEdenboroughLayout.WorldBounds.min.x + (column * intervals + x) * step.x;
                float wz = TruckTaxiEdenboroughLayout.WorldBounds.min.z + (row * intervals + z) * step.y;
                heights[z, x] = Mathf.Clamp01((field.Sample(wx, wz).Height - baseY) / VerticalRange);
            }
            return heights;
        }
        private static float[,,] Alphamaps(RoadHeightfield field, float[,] heights, int column, int row)
        {
            int end = AlphamapResolution - 1;
            var weights = new float[AlphamapResolution, AlphamapResolution, LayerNames.Length];
            var origin = TileBounds(column, row).min;
            float dx = TileSize.x / (HeightmapResolution - 1), dz = TileSize.z / (HeightmapResolution - 1);
            for (int z = 0; z <= end; z++) for (int x = 0; x <= end; x++)
            {
                float wx = TruckTaxiEdenboroughLayout.WorldBounds.min.x + (column * end + x) * (TileSize.x / end);
                float wz = TruckTaxiEdenboroughLayout.WorldBounds.min.z + (row * end + z) * (TileSize.z / end);
                float hx = x * (HeightmapResolution - 1f) / end, hz = z * (HeightmapResolution - 1f) / end;
                float height = Bilinear(heights, hx, hz) * VerticalRange + origin.y;
                // Cache-backed slopes; only tile-edge neighbors need another spatial-grid query.
                float sx = (SurfaceHeight(field, heights, hx + 1, hz, wx + dx, wz, origin.y) -
                    SurfaceHeight(field, heights, hx - 1, hz, wx - dx, wz, origin.y)) / (2 * dx);
                float sz = (SurfaceHeight(field, heights, hx, hz + 1, wx, wz + dz, origin.y) -
                    SurfaceHeight(field, heights, hx, hz - 1, wx, wz - dz, origin.y)) / (2 * dz);
                float slope = Mathf.Sqrt(sx * sx + sz * sz);
                var surface = field.Sample(wx, wz);
                float road = Mathf.Max(surface.RoadBlend, surface.PadBlend);
                float rock = Mathf.Clamp01(Mathf.InverseLerp(.09f, .3f, slope) + Mathf.InverseLerp(110, 225, height) * .45f);
                float dirt = road * .75f * (1 - rock);
                float meadow = .2f + .5f * Mathf.PerlinNoise((wx + 7000) * .0011f, (wz + 7000) * .0011f);
                float grass = 1 - rock - dirt;
                weights[z, x, 0] = grass * (1 - meadow); weights[z, x, 1] = grass * meadow;
                weights[z, x, 2] = dirt; weights[z, x, 3] = rock;
            }
            return weights;
        }
        private static float SurfaceHeight(RoadHeightfield field, float[,] heights, float hx, float hz, float wx, float wz, float baseY) =>
            hx < 0 || hz < 0 || hx > HeightmapResolution - 1 || hz > HeightmapResolution - 1 ? field.Sample(wx, wz).Height :
                Bilinear(heights, hx, hz) * VerticalRange + baseY;
        private static float Bilinear(float[,] heights, float x, float z)
        {
            int ix = Mathf.FloorToInt(x), iz = Mathf.FloorToInt(z);
            int nextX = Mathf.Min(ix + 1, HeightmapResolution - 1), nextZ = Mathf.Min(iz + 1, HeightmapResolution - 1);
            return Mathf.Lerp(Mathf.Lerp(heights[iz, ix], heights[iz, nextX], x - ix),
                Mathf.Lerp(heights[nextZ, ix], heights[nextZ, nextX], x - ix), z - iz);
        }

        public static string NormalizeAssetFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A dedicated terrain asset folder is required.");
            path = path.Replace('\\', '/').TrimEnd('/');
            if (!path.StartsWith("Assets/LWS/TruckTaxi/", StringComparison.Ordinal) || path.Contains("//"))
                throw new ArgumentException("Terrain assets must be in a dedicated child of Assets/LWS/TruckTaxi.");
            foreach (string part in path.Split('/'))
                if (part == "." || part == ".." || part != part.Trim() || part.IndexOfAny(new[] { ':', '*', '?', '"', '<', '>', '|' }) >= 0)
                    throw new ArgumentException("Invalid terrain asset folder segment.");
            return path;
        }
        private static void ValidateParent(Transform parent)
        {
            if (parent == null || EditorUtility.IsPersistent(parent) || !parent.gameObject.scene.IsValid())
                throw new ArgumentException("Terrain parent must be an Edenborough authoring scene object.");
            string scenePath = parent.gameObject.scene.path;
            if (!string.IsNullOrEmpty(scenePath) && scenePath != TruckTaxiEdenboroughLayout.ScenePath)
                throw new InvalidOperationException("Refusing to author terrain into a saved source/demo/other scene: " + scenePath);
            if (Quaternion.Angle(parent.rotation, Quaternion.identity) > .001f || (parent.lossyScale - Vector3.one).sqrMagnitude > .000001f)
                throw new ArgumentException("Native terrain parent must have identity world rotation and unit world scale.");
            if (!Mathf.Approximately(TruckTaxiEdenboroughLayout.WorldBounds.size.y, VerticalRange))
                throw new InvalidOperationException("Layout vertical bounds do not match the 600m terrain range.");
        }
        private static void ValidateOwnership(string folder)
        {
            var marker = AssetDatabase.LoadMainAssetAtPath(folder + "/" + OwnerFile);
            if (marker != null && (!(marker is TextAsset text) || text.text != OwnerSignature))
                throw new InvalidOperationException("Terrain output folder has another owner.");
            var expected = new HashSet<string>(StringComparer.Ordinal) { folder + "/" + OwnerFile, folder + "/" + MaterialFile };
            foreach (string name in LayerNames) expected.Add(folder + "/" + name + ".terrainlayer");
            for (int row = 0; row < Rows; row++) for (int column = 0; column < Columns; column++) expected.Add(TilePath(folder, column, row));
            foreach (string path in AssetDatabase.GetAllAssetPaths())
                if (path.StartsWith(folder + "/", StringComparison.Ordinal) && !AssetDatabase.IsValidFolder(path) &&
                    (marker == null || !expected.Contains(path)))
                    throw new InvalidOperationException("Refusing to reuse a non-dedicated terrain folder: " + path);
            foreach (string path in expected)
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (asset == null) continue;
                bool valid = path.EndsWith(".terrainlayer", StringComparison.Ordinal) ? asset is TerrainLayer :
                    path.EndsWith(".mat", StringComparison.Ordinal) ? asset is Material : path.EndsWith(OwnerFile, StringComparison.Ordinal) ? asset is TextAsset : asset is TerrainData;
                if (!valid) throw new InvalidOperationException("Unexpected existing terrain asset type: " + path);
            }
        }
        private static void ValidateObjects(Transform parent, string folder)
        {
            for (int row = 0; row < Rows; row++) for (int column = 0; column < Columns; column++)
            {
                var tile = FindTile(parent, TileName(column, row));
                if (tile != null && (tile.GetComponent<TerrainCollider>() == null || tile.terrainData == null ||
                    AssetDatabase.GetAssetPath(tile.terrainData) != TilePath(folder, column, row)))
                    throw new InvalidOperationException("Existing tile is not owned by this output folder: " + tile.name);
            }
        }
        private static Terrain FindTile(Transform parent, string name)
        {
            Terrain result = null;
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i); if (child.name != name) continue;
                var tile = child.GetComponent<Terrain>();
                if (tile == null || result != null) throw new InvalidOperationException("Duplicate or non-terrain named tile: " + name);
                result = tile;
            }
            return result;
        }
        private static TerrainLayer[] LoadSources()
        {
            var sources = new TerrainLayer[LayerSources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                sources[i] = AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerSources[i]);
                if (sources[i] == null || sources[i].diffuseTexture == null || sources[i].normalMapTexture == null)
                    throw new InvalidOperationException("Owned terrain source or its textures are missing: " + LayerSources[i]);
            }
            return sources;
        }
        private static TerrainLayer[] CopyLayers(string folder, TerrainLayer[] sources)
        {
            var layers = new TerrainLayer[sources.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                layers[i] = LoadOrCreate(folder + "/" + LayerNames[i] + ".terrainlayer", () => new TerrainLayer());
                Undo.RecordObject(layers[i], "Configure Edenborough terrain layer");
                EditorUtility.CopySerialized(sources[i], layers[i]); layers[i].name = LayerNames[i];
                // 10m divides both tile sides, so shared layer UVs also meet exactly at tile borders.
                layers[i].tileSize = new Vector2(10, 10); layers[i].tileOffset = Vector2.zero;
                layers[i].metallic = 0; layers[i].smoothness = .08f; layers[i].normalScale = 1;
                SaveGenerated(layers[i]);
            }
            return layers;
        }
        private static T LoadOrCreate<T>(string path, Func<T> create) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) { asset = create(); AssetDatabase.CreateAsset(asset, path); }
            return asset;
        }
        private static GameObject CreateTerrainObject(TerrainData data, Scene scene)
        {
            // Terrain.CreateTerrainGameObject uses the active scene: do not temporarily dirty a demo
            // if main is building an additive Edenborough scene while another scene remains active.
            var previous = SceneManager.GetActiveScene();
            if (previous != scene && !SceneManager.SetActiveScene(scene)) throw new InvalidOperationException("Terrain parent scene is not loaded.");
            try { return Terrain.CreateTerrainGameObject(data); }
            finally { if (previous.IsValid() && previous.isLoaded && previous != scene) SceneManager.SetActiveScene(previous); }
        }
        private static void SaveGenerated(Object asset) { EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset); }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/'); EnsureFolder(path.Substring(0, slash));
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1))))
                throw new InvalidOperationException("Cannot create dedicated terrain output folder: " + path);
        }
        private static Terrain Neighbor(Terrain[] tiles, int column, int row) =>
            column < 0 || row < 0 || column >= Columns || row >= Rows ? null : tiles[row * Columns + column];
        private static string TileName(int column, int row) => "Edenborough Terrain " + column + " " + row;
        private static string TilePath(string folder, int column, int row) => folder + "/Edenborough_Terrain_" + column + "_" + row + ".asset";
    }
}
