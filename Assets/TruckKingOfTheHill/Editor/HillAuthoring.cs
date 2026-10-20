using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using LWS.TruckFareFight.Editor;
using LWS.InterstateHauler;
namespace LWS.TruckKingOfTheHill.Editor
{
    public static class HillAuthoring
    {
        public const string Folder = "TruckKingOfTheHill", Scene = "Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity";
        public static void UpgradePresentation()
        {
            var scene = EditorSceneManager.OpenScene(Scene); var m = UnityEngine.Object.FindAnyObjectByType<HillMatch>();
            m.relocationSeconds = 40;
            var rules = m.menus.pages[4].transform.Find("Rules and controls"); if (rules != null) rules.GetComponent<TMP_Text>().text = m.Help;
            var shades = new[] { new Color(.52f, .38f, .22f), new Color(.31f, .39f, .32f), new Color(.30f, .38f, .52f), new Color(.53f, .29f, .20f), new Color(.42f, .27f, .46f) };
            for (int i = 0; i < m.sites.Length; i++)
            {
                var mountain = scene.GetRootGameObjects().First(g => g.name == "Physical stratified " + m.siteNames[i]); var renderer = mountain.GetComponent<MeshRenderer>(); var materials = renderer.sharedMaterials;
                var top = SportsEventAuthoring.Mat(Folder, "Distinctive " + m.siteNames[i] + " stone", shades[i], .08f); top.mainTexture = materials[0].mainTexture; top.mainTextureScale = Vector2.one * 10; materials[0] = top; renderer.sharedMaterials = materials; SportsDrivingSurfaceAuthoring.MarkHierarchy(mountain.transform);
            }
            if (!scene.GetRootGameObjects().Any(g => g.name == "Original king artwork painted on central plateau"))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckKingOfTheHill/Materials/Original mountain king venue artwork.mat"); var art = GameObject.CreatePrimitive(PrimitiveType.Quad); art.name = "Original king artwork painted on central plateau"; UnityEngine.Object.DestroyImmediate(art.GetComponent<Collider>()); art.transform.SetPositionAndRotation(m.sites[0] + Vector3.up * .07f, Quaternion.Euler(90, 0, 0)); art.transform.localScale = new Vector3(26, 17, 1); art.GetComponent<Renderer>().sharedMaterial = material;
            }
            var supportMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckKingOfTheHill/Materials/Royal purple bridge steel.mat");
            if (!scene.GetRootGameObjects().Any(g => g.name == "Logical spectator overlook support -1")) foreach (int side in new[] { -1, 1 }) { var support = GameObject.CreatePrimitive(PrimitiveType.Cylinder); support.name = "Logical spectator overlook support " + side; support.transform.position = new Vector3(side * 18, 49, -65); support.transform.localScale = new Vector3(4, 50, 4); support.GetComponent<Renderer>().sharedMaterial = supportMaterial; }
            var links = new[] { new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(0,3), new Vector2Int(0,4), new Vector2Int(1,2), new Vector2Int(2,4), new Vector2Int(4,3), new Vector2Int(3,1) };
            if (!scene.GetRootGameObjects().Any(g => g.name == "Continuous summit bridge safety rails"))
            {
                var rails = new GameObject("Continuous summit bridge safety rails").transform;
                for (int r = 0; r < links.Length; r++)
                {
                    var a = m.sites[links[r].x]; var b = m.sites[links[r].y]; var direction = Vector3.ProjectOnPlane(b-a, Vector3.up).normalized;
                    var p = a + direction * (links[r].x == 0 ? 45 : 32); p.y = a.y; var q = b-direction*32; q.y = b.y; var delta = q-p; var right = Vector3.Cross(Vector3.up, delta).normalized; float width = r == 4 ? 15 : 20;
                    foreach (int side in new[] {-1,1}) { var rail = SportsEventAuthoring.Box("Visible continuous bridge edge rail", (p+q)*.5f + right*(side*(width*.5f-.5f)) + Vector3.up*.7f, new Vector3(.6f,1.4f,delta.magnitude), supportMaterial, null, rails); rail.transform.rotation = Quaternion.LookRotation(delta); }
                }
            }
            var railContact = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/TruckKingOfTheHill/Materials/Mountain road grip.asset");
            foreach (var rail in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).Where(r=>r.name=="Visible continuous bridge edge rail"))
            { var collider=rail.GetComponent<BoxCollider>(); if(collider==null)collider=rail.gameObject.AddComponent<BoxCollider>(); collider.sharedMaterial=railContact; }
            EditorUtility.SetDirty(m); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
        public static void Create()
        {
            if (File.Exists(Scene)) throw new InvalidOperationException("Preserve the authored mountain scene. Apply additive repairs.");
            var m = SportsEventAuthoring.Begin<HillMatch>(Folder); m.setup.count = 8; m.NormalizeSetup(); m.feedback.clips = LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create();
            m.sites = new[] { new Vector3(0, 100, 0), new Vector3(-95, 108, 85), new Vector3(95, 112, 85), new Vector3(-95, 98, -85), new Vector3(95, 103, -85) };
            m.siteNames = new[] { "CROWN PLATEAU", "WESTERN WATCH", "SUNRISE LOOKOUT", "QUARRY RIDGE", "SOUTHERN THRONE" };
            var rock = SportsEventAuthoring.Mat(Folder, "Rough slate mountain strata", new Color(.27f, .29f, .37f), .09f); var original = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckKeepaway/Materials/Rocky mud terrain.mat"); rock.mainTexture = original.mainTexture; rock.mainTextureScale = Vector2.one * 6;
            var top = SportsEventAuthoring.Mat(Folder, "Granite competition plateaus", new Color(.42f, .39f, .44f), .08f); top.mainTexture = original.mainTexture; top.mainTextureScale = Vector2.one * 12;
            var gold = SportsEventAuthoring.Mat(Folder, "Crown ochre steel", new Color(1, .61f, .045f), .5f); var purple = SportsEventAuthoring.Mat(Folder, "Royal purple bridge steel", new Color(.19f, .055f, .31f), .3f);
            var grip = SportsEventAuthoring.Contact(Folder, "Mountain road grip", .85f, .02f);
            var grass = SportsEventAuthoring.Mat(Folder, "Vista valley grass surround", new Color(.38f, .52f, .28f), .05f); grass.mainTexture = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/Grass Light/Grass_Light_VistaSample.terrainlayer").diffuseTexture; grass.mainTextureScale = Vector2.one * 60;
            SportsEventAuthoring.Box("Continuous valley beneath the real cliffs", new Vector3(0, -2, 0), new Vector3(700, 3, 700), grass, grip);
            for (int i = 0; i < m.sites.Length; i++)
            {
                var mountain = new GameObject("Physical stratified " + m.siteNames[i]); mountain.transform.position = new Vector3(m.sites[i].x, 0, m.sites[i].z); var mesh = Plateau(i, i == 0 ? 48 : 35, m.sites[i].y);
                mountain.AddComponent<MeshFilter>().sharedMesh = mesh; mountain.AddComponent<MeshRenderer>().sharedMaterials = new[] { top, rock }; var collider = mountain.AddComponent<MeshCollider>(); collider.sharedMesh = mesh; collider.sharedMaterial = grip;
                SportsDrivingSurfaceAuthoring.MarkHierarchy(mountain.transform);
                var name = new GameObject("Readable plateau landmark name").AddComponent<TextMeshPro>(); name.transform.position = m.sites[i] + new Vector3(0, 5, -26); name.font = Resources.Load<SportsBroadcastTheme>("SportsBroadcastTheme").headingFont; name.rectTransform.sizeDelta = new Vector2(35, 5); name.fontSize = 18; name.alignment = TextAlignmentOptions.Center; name.color = new Color(1, .85f, .35f); name.outlineWidth = .2f; name.outlineColor = Color.black;
                name.text = m.siteNames[i];
            }
            Roads(m, top, purple, grip); Dress(m, gold);
            m.trucks = new SportsEventTruck[8]; m.fragments = new SportsFragmentRig[8];
            for (int i = 0; i < 8; i++) { var truck = FareFightVehicleAuthoring.Spawn(m, i, m.Grid(i), Quaternion.LookRotation(Vector3.ProjectOnPlane(m.sites[0] - m.Grid(i), Vector3.up))); m.trucks[i] = truck; m.fragments[i] = SportsFragmentAuthoring.InstallRigid(truck.body, truck.visuals.GetComponentsInChildren<MeshFilter>(true), Folder); }
            Ring(m, gold);
            m.broadcast = SportsEventAuthoring.Camera("Mountaintop all-plateau broadcast"); m.broadcast.view.orthographic = true;
            m.framing = m.broadcast.gameObject.AddComponent<SportsCameraFit>(); m.framing.cameraTarget = m.broadcast.view; m.framing.arenaFit = true; m.framing.topInset = .1f; m.framing.bottomInset = .25f; m.cameras = new[] { m.broadcast.view };
            SportsEventAuthoring.UI(m); SportsRallyAuthoring.CompactUI(m); m.drivingViews = SportsDrivingViewsAuthoring.Install(m, m.trucks, m.broadcast, Folder); m.drivingViews.independentViewModes = true;
            m.atmosphere = SportsChaosAuthoring.Weather(m, Folder); m.atmosphere.presets = SportsWeatherPresetsAuthoring.EnsureCatalog(); m.drivingViews.rally.weather = m.atmosphere; SportsWeatherPickerAuthoring.Install(m, m.atmosphere.presets, new Vector2(350, -180));
            SportsUIAuthoring.Button(m.menus.pages[6].transform, "WATCH THE CROWN", "watchhill", new Vector2(0, -245), new Vector2(520, 48)); var hud = m.gameObject.AddComponent<SportsExpansionViewHud>(); hud.match = m; hud.views = m.drivingViews;
            Artwork(m); SportsBroadcastAuthoring.ApplyScene(); SportsEventAuthoring.Save(Folder);
        }
        static Mesh Plateau(int index, float radius, float height)
        {
            string path = "Assets/TruckKingOfTheHill/Materials/Original stratified plateau " + index + ".asset"; var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (existing != null) return existing;
            const int slices = 48; var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var top = new List<int>(); var cliffs = new List<int>();
            vertices.Add(Vector3.up * height); uv.Add(Vector2.one * .5f);
            for (int k = 0; k < slices; k++) { float a = k * Mathf.PI * 2 / slices; float r = radius * (1 + Mathf.Sin(k * 1.7f + index) * .035f); vertices.Add(new Vector3(Mathf.Sin(a) * r, height, Mathf.Cos(a) * r)); uv.Add(new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * .5f + Vector2.one * .5f); top.Add(0); top.Add(k + 1); top.Add((k + 1) % slices + 1); }
            int start = vertices.Count;
            for (int ring = 0; ring < 4; ring++) for (int k = 0; k < slices; k++) { float a = k * Mathf.PI * 2 / slices; float[] radii = { radius, radius * 1.06f, radius * 1.25f, radius * 1.75f }; float[] y = { height, height * .72f, height * .32f, -1 }; float r = radii[ring] * (1 + Mathf.Sin(k * 1.7f + index) * .035f); vertices.Add(new Vector3(Mathf.Sin(a) * r, y[ring], Mathf.Cos(a) * r)); uv.Add(new Vector2(k / (float)slices, ring / 3f)); }
            for (int ring = 0; ring < 3; ring++) for (int k = 0; k < slices; k++) { int a = start + ring * slices + k, b = start + ring * slices + (k + 1) % slices, c = a + slices, d = b + slices; cliffs.AddRange(new[] { a, c, b, b, c, d }); }
            var mesh = new Mesh { name = "Original closed mountaintop strata " + index }; mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.subMeshCount = 2; mesh.SetTriangles(top, 0); mesh.SetTriangles(cliffs, 1); mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, path); return mesh;
        }
        static void Roads(HillMatch m, Material surface, Material steel, PhysicsMaterial contact)
        {
            m.roads = LwsRoadGraph.CreateEmpty("hill-authored-mountaintop-v1");
            for (int i = 0; i < m.sites.Length; i++) m.roads.nodes.Add(new LwsRoadNode { nodeId = "site." + i, position = m.sites[i] + Vector3.up * .04f, regionId = "hill.arena", sceneChunkId = Folder });
            var connections = new[] { new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3), new Vector2Int(0, 4), new Vector2Int(1, 2), new Vector2Int(2, 4), new Vector2Int(4, 3), new Vector2Int(3, 1) };
            for (int r = 0; r < connections.Length; r++)
            {
                int from = connections[r].x, to = connections[r].y; var a = m.sites[from]; var b = m.sites[to]; var direction = Vector3.ProjectOnPlane(b - a, Vector3.up).normalized;
                float margin = from == 0 ? 45 : 32; var p = a + direction * margin; p.y = a.y; var q = b - direction * 32; q.y = b.y;
                var markers = new[] { a + Vector3.up * .04f, p + Vector3.up * .04f, q + Vector3.up * .04f, b + Vector3.up * .04f };
                float width = r == 4 ? 15 : 20;
                var road = SportsEasyRoadsAuthoring.Bake(Folder, "Authored summit bridge " + r, markers, width, surface, contact); SportsDrivingSurfaceAuthoring.MarkHierarchy(road);
                // Decks stay level across each plateau, then change elevation
                // across the gap; the countdown grid never straddles a slope.
                var full = new List<Vector3>();
                for (int s = 0; s < markers.Length - 1; s++) { int pieces = Mathf.CeilToInt(Vector3.Distance(markers[s], markers[s + 1]) / 22); for (int k = 0; k < pieces; k++) full.Add(Vector3.Lerp(markers[s], markers[s + 1], k / (float)pieces)); }
                full.Add(markers[markers.Length - 1]);
                string previous = "site." + from;
                for (int k = 1; k < full.Count; k++)
                {
                    string node = k == full.Count - 1 ? "site." + to : "bridge." + r + ".node." + k;
                    if (k < full.Count - 1) m.roads.nodes.Add(new LwsRoadNode { nodeId = node, position = full[k], regionId = "hill.arena", sceneChunkId = Folder });
                    string id = "hill." + r + "." + k; float distance = Vector3.Distance(full[k - 1], full[k]); var forward = (full[k] - full[k - 1]).normalized;
                    var edge = new LwsRoadEdge { edgeId = id, roadId = id + ".road", segmentId = id + ".segment", fromNodeId = previous, toNodeId = node, roadClass = LwsRoadClass.LocalRoad, direction = LwsRoadDirection.Bidirectional, surfaceType = LwsRoadSurfaceType.Gravel, oneWay = false, distanceMeters = distance, travelCost = distance, speedLimitMph = 30, laneCount = 2, laneWidthMeters = width * .4f, leftShoulderWidthMeters = width * .1f, rightShoulderWidthMeters = width * .1f, laneCenterOffsetsMeters = new List<float> { -width * .2f, width * .2f }, stateOrRegionId = "hill.arena", sceneChunkId = Folder };
                    edge.samples.Add(new LwsRoadSample { roadId = edge.roadId, segmentId = edge.segmentId, position = full[k - 1], forward = forward, distanceFromStartMeters = 0, roadWidthMeters = width, laneWidthMeters = edge.laneWidthMeters, laneCount = 2, speedLimitMph = 30, direction = edge.direction });
                    edge.samples.Add(new LwsRoadSample { roadId = edge.roadId, segmentId = edge.segmentId, position = full[k], forward = forward, distanceFromStartMeters = distance, roadWidthMeters = width, laneWidthMeters = edge.laneWidthMeters, laneCount = 2, speedLimitMph = 30, direction = edge.direction }); m.roads.edges.Add(edge); previous = node;
                }
                for (int side = -1; side <= 1; side += 2) { var delta = q - p; var right = Vector3.Cross(Vector3.up, delta).normalized; var beam = SportsEventAuthoring.Box("Logical summit bridge steel girder", (p + q) * .5f + right * (side * (width * .5f - 1)) - Vector3.up * .8f, new Vector3(1.2f, 1.5f, delta.magnitude), steel); beam.transform.rotation = Quaternion.LookRotation(delta); }
                var pier = GameObject.CreatePrimitive(PrimitiveType.Cylinder); pier.name = "Single logical bridge support pier"; var mid = (p + q) * .5f; pier.transform.position = new Vector3(mid.x, mid.y * .5f - 1, mid.z); pier.transform.localScale = new Vector3(5, mid.y * .5f, 5); pier.GetComponent<Renderer>().sharedMaterial = steel;
            }
            if (!m.roads.Validate().IsValid) throw new InvalidOperationException("Authored hill graph metadata is invalid.");
            var planner = new LwsRoutePlanner(new LwsNavigationTuning { routeConnectorToleranceMeters = .5f });
            for (int a = 0; a < m.sites.Length; a++) for (int b = 0; b < m.sites.Length; b++) if (a != b) { var route = planner.PlanRoute(new LwsRouteRequest { originNodeId = "site." + a, destinationNodeId = "site." + b, truckRouteRequired = true }, m.roads); if (!route.succeeded) throw new InvalidOperationException("Disconnected physical hill route " + a + " to " + b); }
            Debug.Log("HILL_ALL_TWENTY_ROUTES_PLANNED nodes=" + m.roads.nodes.Count + " edges=" + m.roads.edges.Count);
        }
        static void Ring(HillMatch m, Material gold)
        {
            var material = SportsEventAuthoring.Mat(Folder, "Illuminated crown control ring", Color.yellow, .3f); material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", Color.yellow * 2);
            var next = SportsEventAuthoring.Mat(Folder, "Cyan announced next hill", Color.cyan, .3f); next.EnableKeyword("_EMISSION"); next.SetColor("_EmissionColor", Color.cyan * 2);
            var mesh = Annulus();
            for (int i = 0; i < 2; i++) { var root = new GameObject(i == 0 ? "Recorded authoritative control ring" : "Recorded three-second next-hill preview"); root.AddComponent<MeshFilter>().sharedMesh = mesh; var renderer = root.AddComponent<MeshRenderer>(); renderer.sharedMaterial = i == 0 ? material : next; if (i == 0) { m.ring = root.transform; m.ringRenderer = renderer; } else { m.nextRing = root.transform; m.nextRenderer = renderer; } }
            m.beacon = new GameObject("Recorded giant golden crown beacon").transform;
            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder); rim.name = "Crown beacon gold rim"; rim.transform.SetParent(m.beacon, false); rim.transform.localScale = new Vector3(5, .25f, 5); rim.GetComponent<Renderer>().sharedMaterial = gold; UnityEngine.Object.DestroyImmediate(rim.GetComponent<Collider>());
            for (int k = 0; k < 5; k++) { float a = k * Mathf.PI * 2 / 5; var tip = GameObject.CreatePrimitive(PrimitiveType.Sphere); tip.name = "Crown beacon rounded point"; tip.transform.SetParent(m.beacon, false); tip.transform.localPosition = new Vector3(Mathf.Sin(a) * 2.1f, 1, Mathf.Cos(a) * 2.1f); tip.transform.localScale = new Vector3(.65f, 2, .65f); tip.GetComponent<Renderer>().sharedMaterial = gold; UnityEngine.Object.DestroyImmediate(tip.GetComponent<Collider>()); }
        }
        static Mesh Annulus()
        {
            var path = "Assets/TruckKingOfTheHill/Materials/Original truck-scale control annulus.asset"; var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (mesh != null) return mesh;
            var vertices = new Vector3[128]; var triangles = new int[64 * 6];
            for (int i = 0; i < 64; i++) { float a = i * Mathf.PI * 2 / 64; vertices[i * 2] = new Vector3(Mathf.Sin(a) * 10.7f, 0, Mathf.Cos(a) * 10.7f); vertices[i * 2 + 1] = new Vector3(Mathf.Sin(a) * 11.6f, 0, Mathf.Cos(a) * 11.6f); int j = (i + 1) % 64; int at = i * 6; triangles[at] = i * 2; triangles[at + 1] = i * 2 + 1; triangles[at + 2] = j * 2; triangles[at + 3] = j * 2; triangles[at + 4] = i * 2 + 1; triangles[at + 5] = j * 2 + 1; }
            mesh = new Mesh { name = "Original clear ground-visible capture annulus", vertices = vertices, triangles = triangles }; mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, path); return mesh;
        }
        static void Dress(HillMatch m, Material gold)
        {
            var root = new GameObject("Owned mountain scenery and spectator overlook").transform;
            for (int i = 0; i < 48; i++) { float a = i * 2.39996f, r = 210 + i % 4 * 21; LWS.TruckBaseJumping.Editor.BaseSkyAuthoring.Scenery(root, new Vector3(Mathf.Sin(a) * r, -.5f, Mathf.Cos(a) * r), i % 3 == 0 ? 35 : 18, i % 3 != 0, Folder); }
            var contact = SportsEventAuthoring.Contact(Folder, "Protected overlook ground", .8f, .01f); SportsEventAuthoring.Box("Protected spectator balcony", new Vector3(0, 98.5f, -65), new Vector3(58, 3, 16), gold, contact, root);
            // Reuse complete original material arrays, including skin/eyes and
            // clothing. A single replacement material would flatten appearances.
            var targetScene = SceneManager.GetActiveScene(); var source = EditorSceneManager.OpenScene(LWS.TruckHotPotato.Editor.HotPotatoAuthoring.Scene, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(targetScene); var hot = source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LWS.TruckHotPotato.HotPotatoMatch>(true)).First(); var variants = hot.crowd.batches;
                for (int row = 0; row < 3; row++) for (int k = 0; k < 24; k++) { var original = variants[(row + k) % variants.Length]; var g = new GameObject("Reused posed overlook spectator"); g.transform.SetParent(root, false); g.transform.position = new Vector3(-26 + k * 2.25f, 100 + row * .8f, -60 - row * 3); g.AddComponent<MeshFilter>().sharedMesh = original.idle; g.AddComponent<MeshRenderer>().sharedMaterials = original.materials; }
            }
            finally { EditorSceneManager.CloseScene(source, true); SceneManager.SetActiveScene(targetScene); }
            SportsEventAuthoring.Box("Overlook spectator protection", new Vector3(0, 101, -56), new Vector3(58, 2, .5f), gold, contact, root);
        }
        static void Artwork(HillMatch m)
        {
            var logo = SportsRetroMenuAuthoring.LoadLogo(Folder);
            foreach (int page in new[] { 0, 4 }) { var art = SportsUIAuthoring.Rect("Original sport title artwork", m.menus.pages[page].transform, new Vector2(260, 160), new Vector2(-560, 365)).AddComponent<UnityEngine.UI.Image>(); art.sprite = logo; art.preserveAspect = true; art.raycastTarget = false; var title = m.menus.pages[page].transform.Find("Title").GetComponent<TMP_Text>(); title.rectTransform.anchoredPosition += Vector2.right * 140; title.rectTransform.sizeDelta = new Vector2(1000, title.rectTransform.sizeDelta.y); }
            var material = SportsEventAuthoring.Mat(Folder, "Original mountain king venue artwork", Color.white); material.shader = Shader.Find("Universal Render Pipeline/Unlit"); material.mainTexture = logo.texture; material.SetFloat("_Surface", 1); material.SetFloat("_SrcBlend", 5); material.SetFloat("_DstBlend", 10); material.SetFloat("_ZWrite", 0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000;
            var sign = GameObject.CreatePrimitive(PrimitiveType.Quad); sign.name = "Original king logo on spectator overlook"; UnityEngine.Object.DestroyImmediate(sign.GetComponent<Collider>()); sign.transform.position = new Vector3(0, 114, -70); sign.transform.rotation = Quaternion.Euler(0, 180, 0); sign.transform.localScale = new Vector3(26, 17, 1); sign.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
