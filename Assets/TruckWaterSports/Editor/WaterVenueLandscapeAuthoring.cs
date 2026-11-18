using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LWS.TruckWaterSports.Editor
{
    /// <summary>
    /// Explicit additive landscape upgrade for the three authored Alpha 0.10 water venues.
    /// River Modeler's authored demo height field and Fishing's production terrain layers/scenery
    /// supply the foundation. Vista TerrainTile/LocalProceduralBiome were audited: their whole-tile
    /// generation is unnecessary here and could replace a fixed, already tested river course.
    /// This editor adapter only embeds that existing course in owned terrain; it is not another
    /// runtime terrain, water, weather or buoyancy authority. Original assets are never modified.
    /// </summary>
    public static class WaterVenueLandscapeAuthoring
    {
        const string RootName = "Continuous scenic water venue landscape";
        const string Folder = "Assets/TruckWaterSports/Landscapes";
        const string DemoTerrain = "Assets/RiverModeler/Demo/River Modeler Demo Terrain.asset";
        const string FishingTerrain = "Assets/TruckFishing/Models/Continuous Riverbank.asset";
        const string FishingMaterial = "Assets/TruckFishing/Materials/Continuous grassy bank terrain.mat";
        const string CliffLayer = "Assets/3D Haven/Path Painter II/Editor/Demo/Terrains/DocsTerrain/TerrainLayers/stdCliff.terrainlayer";
        const string TreeA = "Assets/Toon Gas Station/Prefabs/Tree_7A.prefab";
        const string TreeB = "Assets/Toon Gas Station/Prefabs/Tree_6A.prefab";
        const string Grass = "Assets/TruckFishing/Prefabs/River bank grass.prefab";
        const int Resolution = 513, PaintResolution = 256;
        const float CollisionExclusion = 55;

        struct Sample { public float waterY, distance, height; }
        sealed class Layout
        {
            public Vector3 origin, size;
            public Bounds lakeSurface;
            public Vector3[] stations;
            public float halfWidth;
            public bool lake;
        }

        [MenuItem("Truck Sports/Alpha 0.10/Upgrade active water venue surrounding landscape")]
        public static void UpgradeActiveVenue() { InstallActiveVenue(); }

        public static void RestoreSavedPaddlePalsRiverBed()
        {
            var scene = EditorSceneManager.OpenScene(WaterSportsAuthoring.PaddleScene, OpenSceneMode.Single);
            RestoreVisibleRiverBed(scene);
        }

        static void RestoreVisibleRiverBed(UnityEngine.SceneManagement.Scene scene)
        {
            string path = Folder + "/" + scene.name + "/Terrain/Scenic valley terrain.asset";
            TerrainData visual = Required<TerrainData>(path);
            Terrain valley = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var collider in root.GetComponentsInChildren<TerrainCollider>(true))
                    if (collider.terrainData == visual)
                        throw new InvalidOperationException("The render terrain cannot be shared with physical terrain support.");
                foreach (var terrain in root.GetComponentsInChildren<Terrain>(true))
                    if (terrain.terrainData == visual)
                        valley = terrain;
            }
            if (valley == null)
                throw new InvalidOperationException("The saved river valley does not use its owned render terrain.");

            // Keep the visible textured bed beneath the existing water. The
            // separate collision asset still excludes the full river corridor.
            int resolution = visual.holesResolution;
            var continuous = new bool[resolution, resolution];
            for (int z = 0; z < resolution; z++)
                for (int x = 0; x < resolution; x++)
                    continuous[z, x] = true;
            visual.SetHoles(0, 0, continuous);
            EditorUtility.SetDirty(visual);
            AssetDatabase.SaveAssetIfDirty(visual);
            string logTag = scene.path == WaterSportsAuthoring.PaddleScene
                ? "PADDLE_PALS_VISIBLE_RIVER_BED_SAVED" : "WHITEWATER_VISIBLE_RIVER_BED_SAVED";
            Debug.Log(logTag + " " + path);
        }

        public static void AuthorSavedWhitewaterLaunch()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before repairing the Whitewater launch.");
            var scene = EditorSceneManager.OpenScene(WaterSportsAuthoring.KayakScene, OpenSceneMode.Single);
            int banks = UpgradeActiveRiverBanks(scene);
            ExtendWhitewaterLaunchWater(scene);
            RestoreVisibleRiverBed(scene);
            if (scene.isDirty && !EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save the Whitewater launch presentation.");
            AssetDatabase.SaveAssets();
            Debug.Log("WHITEWATER_LAUNCH_LOOK_SAVED banks=" + banks + " existing river material and exact physical course preserved.");
        }

        static void ExtendWhitewaterLaunchWater(UnityEngine.SceneManagement.Scene scene)
        {
            const string originalPath = "Assets/TruckWaterSports/Art/WhitewaterWipeoutMatch watercourse.asset";
            const string launchPath = Folder + "/WhitewaterWipeout/Meshes/Whitewater river with visible launch pool.asset";
            var roots = scene.GetRootGameObjects();
            var race = roots.SelectMany(root => root.GetComponentsInChildren<WhitewaterWipeoutMatch>(true)).Single();
            var visible = roots.SelectMany(root => root.GetComponentsInChildren<MeshFilter>(true))
                .Single(filter => filter.name == "Owned River Modeler shaded flowing surface");
            if (visible.GetComponent<Collider>() != null)
                throw new InvalidOperationException("The visible water mesh cannot own physical support.");
            if (AssetDatabase.GetAssetPath(visible.sharedMesh) == launchPath) return;
            var source = Required<Mesh>(originalPath);
            if (visible.sharedMesh != source || source.vertexCount != race.course.stations.Length * 2)
                throw new InvalidOperationException("Preserve a customized authored river surface.");

            Mesh extended = AssetDatabase.LoadAssetAtPath<Mesh>(launchPath);
            if (extended == null)
            {
                var vertices = new List<Vector3>(source.vertices);
                var uv = new List<Vector2>(source.uv);
                var colors = new List<Color>(source.colors);
                var triangles = new List<int>(source.triangles);
                if (uv.Count != vertices.Count || colors.Count != vertices.Count)
                    throw new InvalidOperationException("Preserve the original water texture coordinates and flow colors.");
                const float extensionMetres = 9;
                Vector3 firstReach = Vector3.ProjectOnPlane(race.course.Point(1) - race.course.Point(0), Vector3.up);
                if (firstReach.sqrMagnitude < 0.01f)
                    throw new InvalidOperationException("The first two river stations must define a launch direction.");
                Vector3 upstream = visible.transform.InverseTransformVector(-firstReach.normalized * extensionMetres);
                Vector2 upstreamUv = (uv[0] - uv[2]) * (extensionMetres / firstReach.magnitude);
                int start = vertices.Count;
                vertices.Add(vertices[0] + upstream);
                vertices.Add(vertices[1] + upstream);
                uv.Add(uv[0] + upstreamUv);
                uv.Add(uv[1] + upstreamUv);
                colors.Add(colors[0]);
                colors.Add(colors[1]);
                triangles.AddRange(new[] { start, 0, start + 1, start + 1, 0, 1 });
                extended = new Mesh { name = "Whitewater river with visible launch pool" };
                extended.SetVertices(vertices);
                extended.SetUVs(0, uv);
                extended.SetColors(colors);
                extended.SetTriangles(triangles, 0);
                extended.RecalculateNormals();
                extended.RecalculateTangents();
                AssetDatabase.CreateAsset(extended, launchPath);
            }
            visible.sharedMesh = extended;
            EditorUtility.SetDirty(visible);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        [MenuItem("Truck Sports/Alpha 0.10/Repair saved Paddle Pals visible shoreline materials")]
        public static void AuthorSavedPaddlePalsBankLook()
        {
            if(Application.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before repairing the saved shoreline.");
            var scene=EditorSceneManager.OpenScene(WaterSportsAuthoring.PaddleScene,OpenSceneMode.Single);
            int count=UpgradeActivePaddlePalsBankLook();
            if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Could not save the Paddle Pals shoreline presentation.");
            AssetDatabase.SaveAssets();
            Debug.Log("PADDLE_PALS_SHORELINE_LOOK_SAVED "+scene.path+" banks="+count+"; exact original outlines and colliders; owned grass/pebble render materials and metre UVs only.");
        }

        public static int UpgradeActivePaddlePalsBankLook()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(Application.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode||scene.path!=WaterSportsAuthoring.PaddleScene)
                throw new InvalidOperationException("Open the saved Paddle Pals scene outside Play Mode.");
            return UpgradeActiveRiverBanks(scene);
        }

        static int UpgradeActiveRiverBanks(UnityEngine.SceneManagement.Scene scene)
        {
            // Audit every target and dependency before creating assets or changing
            // assignments. Never make a previously visible collision surface vanish.
            var grass=Required<TerrainLayer>("Assets/TruckFishing/Materials/Grass bank.terrainlayer");
            var pebble=Required<TerrainLayer>("Assets/TruckFishing/Materials/Shallow pebbled riverbed.terrainlayer");
            var template=Required<Material>("Assets/TruckWaterSports/Materials/Riverbed earth.mat");
            if(grass.diffuseTexture==null||grass.normalMapTexture==null||pebble.diffuseTexture==null||pebble.normalMapTexture==null||
                grass.tileSize.x<=0||grass.tileSize.y<=0||pebble.tileSize.x<=0||pebble.tileSize.y<=0||!template.HasProperty("_BaseMap"))
                throw new InvalidOperationException("The installed Fishing grass/pebble layers and current URP bank material must be intact.");
            var filters=new List<MeshFilter>();var renderers=new List<MeshRenderer>();WaterMatch match=null;
            Mesh source=null;Vector3 dimensions=Vector3.zero;Vector3[] originalVertices=null;
            foreach(var root in scene.GetRootGameObjects())
            {
                var candidate=root.GetComponentInChildren<WaterMatch>(true);if(candidate!=null)match=candidate;
                foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    if(filter.name!="Textured riverbank")continue;
                    var box=filter.GetComponent<BoxCollider>();var renderer=filter.GetComponent<MeshRenderer>();
                    Mesh mesh=filter.sharedMesh;Vector3 scale=filter.transform.lossyScale;
                    if(box==null||!box.enabled||box.isTrigger||renderer==null||!renderer.enabled||mesh==null||mesh.vertexCount!=24||
                        mesh.normals.Length!=mesh.vertexCount||Vector3.Distance(mesh.bounds.center,box.center)>.0001f||
                        Vector3.Distance(mesh.bounds.size,box.size)>.0001f||scale.x<=0||scale.y<=0||scale.z<=0)
                        throw new InvalidOperationException("Every original riverbank must retain its visible exact box surface and enabled solid collider: "+filter.name);
                    if(source==null){source=mesh;dimensions=scale;originalVertices=mesh.vertices;}
                    else if(Vector3.Distance(dimensions,scale)>.0001f||Vector3.Distance(mesh.bounds.size,source.bounds.size)>.0001f)
                        throw new InvalidOperationException("Preserve independently edited bank dimensions; this repair supports only the original matching bank surfaces.");
                    var candidateVertices=mesh.vertices;
                    for(int vertex=0;vertex<originalVertices.Length;vertex++)if(Vector3.Distance(candidateVertices[vertex],originalVertices[vertex])>.0001f)
                        throw new InvalidOperationException("A bank has independently edited visible geometry; preserve it for an explicit designer review.");
                    filters.Add(filter);renderers.Add(renderer);
                }
            }
            if(match==null||match.course==null||match.course.stations==null||filters.Count!=match.course.stations.Length*2)
                throw new InvalidOperationException("All original paired bank surfaces and river stations are required.");
            string folder=Folder+"/"+scene.name;EnsureFolder(folder+"/Materials");EnsureFolder(folder+"/Meshes");
            Material top=ShorelineMaterial(folder+"/Materials/Visible grass shoreline.mat",template,grass);
            Material side=ShorelineMaterial(folder+"/Materials/Visible pebbled shoreline.mat",template,pebble);
            string meshPath=folder+"/Meshes/Exact original shoreline render surface.asset";
            Mesh surface=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(surface==null)
            {
                surface=UnityEngine.Object.Instantiate(source);surface.name="Exact original shoreline with grass top and pebble sides";
                var vertices=surface.vertices;var normals=surface.normals;var uv=new Vector2[vertices.Length];
                for(int i=0;i<vertices.Length;i++)
                {
                    Vector3 metres=Vector3.Scale(vertices[i],dimensions);
                    if(normals[i].y>.5f)uv[i]=new Vector2(metres.x/grass.tileSize.x,metres.z/grass.tileSize.y);
                    else if(Mathf.Abs(normals[i].x)>.5f)uv[i]=new Vector2(metres.z/pebble.tileSize.x,metres.y/pebble.tileSize.y);
                    else uv[i]=new Vector2(metres.x/pebble.tileSize.x,metres.y/pebble.tileSize.y);
                }
                var triangles=surface.triangles;var tops=new List<int>();var sides=new List<int>();
                for(int i=0;i<triangles.Length;i+=3)
                {
                    var destination=normals[triangles[i]].y>.5f&&normals[triangles[i+1]].y>.5f&&normals[triangles[i+2]].y>.5f?tops:sides;
                    destination.Add(triangles[i]);destination.Add(triangles[i+1]);destination.Add(triangles[i+2]);
                }
                if(tops.Count!=6||sides.Count!=30)throw new InvalidOperationException("The original six visible box faces must be preserved.");
                surface.uv=uv;surface.subMeshCount=2;surface.SetTriangles(tops,0);surface.SetTriangles(sides,1);surface.RecalculateTangents();
                AssetDatabase.CreateAsset(surface,meshPath);
            }
            if(surface.vertexCount!=source.vertexCount||surface.subMeshCount!=2||Vector3.Distance(surface.bounds.size,source.bounds.size)>.0001f)
                throw new InvalidOperationException("The existing owned shoreline render asset must preserve the original box dimensions.");
            for(int i=0;i<filters.Count;i++)
            {
                filters[i].sharedMesh=surface;renderers[i].sharedMaterials=new[]{top,side};
                EditorUtility.SetDirty(filters[i]);EditorUtility.SetDirty(renderers[i]);
            }
            EditorSceneManager.MarkSceneDirty(scene);return filters.Count;
        }

        static Material ShorelineMaterial(string path,Material template,TerrainLayer layer)
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null)return material;
            material=new Material(template){name=System.IO.Path.GetFileNameWithoutExtension(path)};
            // The original dark albedo multiplier is the cause of the nearly
            // black blocks. Use the same actual textures as the surrounding land.
            material.SetColor("_BaseColor",Color.white);if(material.HasProperty("_Color"))material.SetColor("_Color",Color.white);
            material.SetTexture("_BaseMap",layer.diffuseTexture);material.SetTextureScale("_BaseMap",Vector2.one);material.SetTextureOffset("_BaseMap",Vector2.zero);
            material.SetTexture("_BumpMap",layer.normalMapTexture);material.SetFloat("_BumpScale",layer.normalScale);material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",layer.smoothness);material.enableInstancing=true;
            AssetDatabase.CreateAsset(material,path);return material;
        }

        /// <summary>Leaves saving to the caller. Repeated invocation preserves the authored upgrade.</summary>
        public static GameObject InstallActiveVenue()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before upgrading a saved water venue.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != WaterSportsAuthoring.PaddleScene && scene.path != WaterSportsAuthoring.KayakScene && scene.path != WaterSportsAuthoring.BlackwaterScene)
                throw new InvalidOperationException("Open the saved Paddle Pals, Whitewater Wipeout or Blackwater scene. No other scene is supported.");
            WaterMatch match = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == RootName) { Debug.Log("WATER_LANDSCAPE_PRESERVED " + scene.path); return root; }
                var candidate = root.GetComponentInChildren<WaterMatch>(true); if (candidate != null) match = candidate;
            }
            if (match == null || match.course == null || match.course.stations == null || match.course.stations.Length < 2)
                throw new InvalidOperationException("The active venue must retain its authored water course and stations.");

            // Validate dependencies before touching the scene or creating generated assets.
            var source = Required<TerrainData>(DemoTerrain);
            var fishing = Required<TerrainData>(FishingTerrain);
            if (fishing.terrainLayers == null || fishing.terrainLayers.Length < 2)
                throw new InvalidOperationException("Fishing's existing grass/pebble terrain layers are required.");
            var cliff = Required<TerrainLayer>(CliffLayer);
            foreach (string path in new[] { TreeA, TreeB, Grass, Boulder(0), Boulder(1), Boulder(2) }) Required<GameObject>(path);
            var layout = Describe(match);
            var originalStations = (Vector3[])match.course.stations.Clone();
            float width = match.course.width, current = match.course.currentSpeed, turbulence = match.course.turbulence;
            string venueFolder = Folder + "/" + scene.name;
            EnsureFolder(venueFolder + "/Materials"); EnsureFolder(venueFolder + "/Terrain");

            Sample[,] samples;
            var visualData = TerrainAsset(source, fishing.terrainLayers, cliff, layout, venueFolder, out samples);
            var collisionData = CollisionAsset(visualData, samples, layout, venueFolder);
            var material = TerrainMaterial(venueFolder);
            var landscape = new GameObject(RootName); landscape.SetActive(false);
            var terrainObject = Terrain.CreateTerrainGameObject(visualData); terrainObject.name = "Continuous textured valley and shore";
            terrainObject.transform.SetParent(landscape.transform, false); terrainObject.transform.position = layout.origin;
            // The rendered bank blends over existing bank meshes. Its collider is intentionally separate:
            // Unity cannot synchronize the visual data back over the wider physical exclusion holes.
            var automaticCollider = terrainObject.GetComponent<TerrainCollider>(); if (automaticCollider != null) UnityEngine.Object.DestroyImmediate(automaticCollider);
            var terrain = terrainObject.GetComponent<Terrain>(); terrain.materialTemplate = material; terrain.drawInstanced = true;
            terrain.heightmapPixelError = 7; terrain.basemapDistance = 850; terrain.shadowCastingMode = ShadowCastingMode.On;
            terrain.detailObjectDistance = 80; terrain.treeDistance = 550;
            var collisionObject = new GameObject("Peripheral land collision outside the existing course");
            collisionObject.transform.SetParent(landscape.transform, false); collisionObject.transform.position = layout.origin;
            collisionObject.AddComponent<TerrainCollider>().terrainData = collisionData;
            Scatter(landscape.transform, terrain, layout, venueFolder, scene.name == "Blackwater" ? 7308 : scene.name == "PaddlePals" ? 7301 : 7302);
            landscape.SetActive(true); Undo.RegisterCreatedObjectUndo(landscape, "Add water venue surrounding landscape");

            if (match.course.width != width || match.course.currentSpeed != current || match.course.turbulence != turbulence || match.course.stations.Length != originalStations.Length)
                throw new InvalidOperationException("Landscape authoring must not change water gameplay configuration.");
            for (int i = 0; i < originalStations.Length; i++) if (!match.course.stations[i].Equals(originalStations[i])) throw new InvalidOperationException("A water station changed during landscape authoring.");
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("WATER_LANDSCAPE_ADDED " + scene.name + " " + layout.size.x.ToString("F0") + "x" + layout.size.z.ToString("F0") + "m; owned textured terrain/forest; water, starts, gates and forces unchanged; caller must save scene/assets.");
            return landscape;
        }

        static Layout Describe(WaterMatch match)
        {
            var c = match.course; var bounds = new Bounds(c.stations[0], Vector3.zero);
            foreach (var p in c.stations) bounds.Encapsulate(p);
            var layout = new Layout { stations = (Vector3[])c.stations.Clone(), halfWidth = c.width * .5f, lake = c.lake };
            if (c.lake)
            {
                Renderer surface = null;
                foreach (var root in match.gameObject.scene.GetRootGameObjects())
                    foreach (var candidate in root.GetComponentsInChildren<MeshRenderer>(true))
                        if (candidate.name == "Owned River Modeler shaded flowing surface") surface = candidate;
                if (surface == null) throw new InvalidOperationException("Preserve the authored River Modeler lake surface before adding its shore.");
                layout.lakeSurface = surface.bounds;
                layout.origin = new Vector3(surface.bounds.min.x - 550, bounds.min.y - 40, surface.bounds.min.z - 550);
                layout.size = new Vector3(surface.bounds.size.x + 1100, 420, surface.bounds.size.z + 1100);
            }
            else
            {
                float length = Mathf.Max(1400, bounds.size.z + 600);
                layout.origin = new Vector3(bounds.center.x - 600, bounds.min.y - 40, bounds.center.z - length * .5f);
                layout.size = new Vector3(1200, 420, length);
            }
            return layout;
        }

        static Sample NearWater(Layout layout, float x, float z)
        {
            if (layout.lake)
            {
                var b = layout.lakeSurface; float dx = Mathf.Max(0, Mathf.Max(b.min.x - x, x - b.max.x)), dz = Mathf.Max(0, Mathf.Max(b.min.z - z, z - b.max.z));
                bool inside = dx == 0 && dz == 0;
                float distance = inside ? -Mathf.Min(Mathf.Min(x - b.min.x, b.max.x - x), Mathf.Min(z - b.min.z, b.max.z - z)) : Mathf.Sqrt(dx * dx + dz * dz);
                return new Sample { waterY = layout.stations[0].y, distance = distance };
            }
            float best = float.MaxValue, waterY = 0;
            for (int i = 0; i < layout.stations.Length - 1; i++)
            {
                Vector3 a = layout.stations[i], b = layout.stations[i + 1]; float dx = b.x - a.x, dz = b.z - a.z;
                float denominator = dx * dx + dz * dz;
                float t = denominator > .0001f ? Mathf.Clamp01(((x - a.x) * dx + (z - a.z) * dz) / denominator) : 0;
                float px = a.x + dx * t, pz = a.z + dz * t, d = (x - px) * (x - px) + (z - pz) * (z - pz);
                if (d < best) { best = d; waterY = Mathf.Lerp(a.y, b.y, t); }
            }
            return new Sample { waterY = waterY, distance = Mathf.Sqrt(best) - layout.halfWidth };
        }
        static float Smooth(float from, float to, float value) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(from, to, value));

        static TerrainData TerrainAsset(TerrainData source, TerrainLayer[] fishingLayers, TerrainLayer cliff, Layout layout, string folder, out Sample[,] samples)
        {
            string path = folder + "/Terrain/Scenic valley terrain.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            if (existing != null) throw new InvalidOperationException("Generated landscape data already exists without its scene root. Preserve/review those authored assets before an explicit retry: " + path);
            var data = UnityEngine.Object.Instantiate(source); data.name = "Owned course-fitted River Modeler valley";
            data.heightmapResolution = Resolution; data.size = layout.size;
            data.treeInstances = Array.Empty<TreeInstance>(); data.treePrototypes = Array.Empty<TreePrototype>(); data.detailPrototypes = Array.Empty<DetailPrototype>();
            data.terrainLayers = new[] { fishingLayers[0], fishingLayers[1], cliff };
            var heights = new float[Resolution, Resolution]; samples = new Sample[Resolution, Resolution];
            for (int z = 0; z < Resolution; z++) for (int x = 0; x < Resolution; x++)
            {
                float u = x / (float)(Resolution - 1), v = z / (float)(Resolution - 1);
                float wx = layout.origin.x + u * layout.size.x, wz = layout.origin.z + v * layout.size.z;
                var s = NearWater(layout, wx, wz);
                float authoredRelief = source.GetInterpolatedHeight(u, v) / Mathf.Max(1, source.size.y);
                float shore = Mathf.Lerp(s.waterY - 6, s.waterY + .7f, Smooth(-3, -.5f, s.distance));
                float foothills = Smooth(45, 245, s.distance) * (12 + authoredRelief * 45);
                float ridges = Smooth(170, 540, s.distance) * (65 + authoredRelief * 175);
                s.height = shore + foothills + ridges + Smooth(30, 100, s.distance) * Mathf.PerlinNoise(wx * .027f + 17, wz * .025f + 31) * 1.8f;
                heights[z, x] = Mathf.Clamp01((s.height - layout.origin.y) / layout.size.y); samples[z, x] = s;
            }
            data.SetHeights(0, 0, heights); data.alphamapResolution = PaintResolution; data.baseMapResolution = 1024;
            var paint = new float[PaintResolution, PaintResolution, 3];
            for (int z = 0; z < PaintResolution; z++) for (int x = 0; x < PaintResolution; x++)
            {
                int gx = Mathf.RoundToInt(x * (Resolution - 1f) / (PaintResolution - 1)), gz = Mathf.RoundToInt(z * (Resolution - 1f) / (PaintResolution - 1));
                var s = samples[gz, gx]; float u = x / (float)(PaintResolution - 1), v = z / (float)(PaintResolution - 1);
                float rock = Mathf.Clamp01(Mathf.Max(Smooth(28, 62, data.GetSteepness(u, v)), Smooth(260, 560, s.distance) * .65f));
                float pebble = (1 - Smooth(2, 24, s.distance)) * (1 - rock);
                paint[z, x, 0] = Mathf.Max(0, 1 - rock - pebble); paint[z, x, 1] = pebble; paint[z, x, 2] = rock;
            }
            data.SetAlphamaps(0, 0, paint); data.SetHoles(0, 0, Holes(samples, -.5f));
            AssetDatabase.CreateAsset(data, path); return data;
        }
        static bool[,] Holes(Sample[,] samples, float clearance)
        {
            var solid = new bool[Resolution - 1, Resolution - 1];
            for (int z = 0; z < Resolution - 1; z++) for (int x = 0; x < Resolution - 1; x++)
            { float nearest = Mathf.Min(Mathf.Min(samples[z, x].distance, samples[z, x + 1].distance), Mathf.Min(samples[z + 1, x].distance, samples[z + 1, x + 1].distance)); solid[z, x] = nearest >= clearance; }
            return solid;
        }
        static TerrainData CollisionAsset(TerrainData visual, Sample[,] samples, Layout layout, string folder)
        {
            var data = new TerrainData { name = "Peripheral valley collision with preserved course clearance", heightmapResolution = Resolution, size = layout.size };
            data.SetHeights(0, 0, visual.GetHeights(0, 0, Resolution, Resolution)); data.SetHoles(0, 0, Holes(samples, CollisionExclusion));
            AssetDatabase.CreateAsset(data, folder + "/Terrain/Peripheral land collision.asset"); return data;
        }
        static Material TerrainMaterial(string folder)
        {
            var source = AssetDatabase.LoadAssetAtPath<Material>(FishingMaterial);
            var material = source != null ? new Material(source) : new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));
            material.name = "Owned continuous water venue terrain"; material.enableInstancing = true; material.EnableKeyword("_ALPHATEST_ON");
            AssetDatabase.CreateAsset(material, folder + "/Materials/Continuous valley terrain.mat"); return material;
        }

        static void Scatter(Transform parent, Terrain terrain, Layout layout, string folder, int seed)
        {
            var trees = new GameObject("Owned forest belts and riverbank growth").transform; trees.SetParent(parent, false);
            var outcrops = new GameObject("Owned rocky valley outcrops").transform; outcrops.SetParent(parent, false);
            var growth = new GameObject("Owned shore grass tufts").transform; growth.SetParent(parent, false);
            var random = new System.Random(seed); string materialFolder = folder.Substring("Assets/".Length);
            for (int kind = 0; kind < 3; kind++)
            {
                int target = kind == 0 ? 230 : kind == 1 ? 95 : 135, placed = 0;
                for (int attempt = 0; attempt < target * 35 && placed < target; attempt++)
                {
                    float u = .04f + (float)random.NextDouble() * .92f, v = .04f + (float)random.NextDouble() * .92f;
                    float x = layout.origin.x + u * layout.size.x, z = layout.origin.z + v * layout.size.z;
                    var s = NearWater(layout, x, z); float minimum = kind == 2 ? 4 : 38, maximum = kind == 2 ? 65 : kind == 0 ? 430 : 590;
                    if (s.distance < minimum || s.distance > maximum) continue;
                    if (kind == 0 && (terrain.terrainData.GetSteepness(u, v) > 38 || Mathf.PerlinNoise(u * 15 + 7, v * 13 + 5) < .4f)) continue;
                    float y = layout.origin.y + terrain.terrainData.GetInterpolatedHeight(u, v);
                    if (kind == 2 && s.distance < 30) y = Mathf.Max(y, s.waterY + .55f);
                    float size = kind == 0 ? 13 + (float)random.NextDouble() * 16 : kind == 1 ? 3 + (float)random.NextDouble() * 10 : .8f + (float)random.NextDouble() * 1.6f;
                    var holder = new GameObject(kind == 0 ? "Riverside woodland " + placed : kind == 1 ? "Valley rock outcrop " + placed : "Natural bank grass " + placed).transform;
                    holder.SetParent(kind == 0 ? trees : kind == 1 ? outcrops : growth, false); holder.position = new Vector3(x, y, z); holder.rotation = Quaternion.Euler(0, (float)random.NextDouble() * 360, 0);
                    string source = kind == 0 ? placed % 2 == 0 ? TreeA : TreeB : kind == 1 ? Boulder(placed % 3) : Grass;
                    var dimensions = kind == 0 ? new Vector3(size * .7f, size, size * .7f) : kind == 1 ? new Vector3(size, size * .65f, size) : new Vector3(size, size * .6f, size);
                    LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(holder, source, dimensions, 0, false, materialFolder);
                    var renderers = holder.GetComponentsInChildren<Renderer>(true);
                    foreach (var renderer in renderers) { renderer.shadowCastingMode = kind == 2 ? ShadowCastingMode.Off : ShadowCastingMode.On; renderer.receiveShadows = true; }
                    var existingLods = holder.GetComponentsInChildren<LODGroup>(true);
                    if (existingLods.Length == 0) { var lod = holder.gameObject.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(kind == 2 ? .018f : .025f, renderers) }); lod.RecalculateBounds(); }
                    else foreach (var lod in existingLods) lod.RecalculateBounds();
                    foreach (var child in holder.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(child.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
                    placed++;
                }
            }
        }
        static string Boulder(int index) => "Assets/TruckFishing/Prefabs/River boulder " + index + ".prefab";
        static void EnsureFolder(string path)
        {
            string parent = "Assets"; var parts = path.Split('/');
            for (int i = 1; i < parts.Length; i++)
            { string next = parent + "/" + parts[i]; if (!AssetDatabase.IsValidFolder(next) && string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, parts[i]))) throw new InvalidOperationException("Cannot create owned landscape folder: " + next); parent = next; }
        }
        static T Required<T>(string path) where T : UnityEngine.Object
        { var asset = AssetDatabase.LoadAssetAtPath<T>(path); if (asset == null) throw new InvalidOperationException("Required installed landscape asset is missing: " + path); return asset; }
    }
}
