using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using LWS.TruckSports;
using LWS.TruckSports.Editor;

namespace LWS.TruckKickball.Editor
{
    public static class KickballAuthoring
    {
        public const string Folder = "TruckKickball";
        public const string Scene = "Assets/TruckKickball/Scenes/TruckKickball.unity";
        [MenuItem("Truck Sports/Kickball/Create new stadium scene")]
        public static void Create()
        {
            if (File.Exists(Scene)) throw new InvalidOperationException("Kickball already exists. Preserve the hand-edited scene; use a targeted upgrade.");
            var m = SportsEventAuthoring.Begin<KickballMatch>(Folder);
            m.option = 7; m.NormalizeSetup();
            for (int i = 0; i < m.setup.players.Length; i++) m.setup.players[i].source = LWS.TruckBowling.BowlingSource.AI;
            m.setup.players[0].source = LWS.TruckBowling.BowlingSource.Keyboard;
            m.weatherPresets = SportsWeatherPresetsAuthoring.EnsureCatalog();
            var turf = SportsEventAuthoring.Mat(Folder, "Mown stadium turf", new Color(.12f, .28f, .09f), .12f);
            var dirt = SportsEventAuthoring.Mat(Folder, "Raked kickball infield clay", new Color(.38f, .18f, .075f), .08f);
            var ivory = SportsEventAuthoring.Mat(Folder, "Ivory base pads and chalk", new Color(.95f, .9f, .72f), .2f);
            var dark = SportsEventAuthoring.Mat(Folder, "Stadium navy steel", new Color(.025f, .055f, .08f), .4f);
            var gold = SportsEventAuthoring.Mat(Folder, "Event amber", new Color(1, .5f, .035f), .35f);
            var groundPhysics = SportsEventAuthoring.Contact(Folder, "Turf physical contact", .65f, .05f);
            Texture2D gravel = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/NWH/Common/Textures/Ground/RoadGravelNormal.png");
            foreach (var mat in new[] { turf, dirt })
            { mat.SetTexture("_BumpMap", gravel); mat.SetFloat("_BumpScale", .25f); mat.EnableKeyword("_NORMALMAP"); mat.mainTextureScale = Vector2.one * 24; }
            var ground = SportsEventAuthoring.Box("Connected stadium ground turf", new Vector3(0, -.4f, 27), new Vector3(230, .8f, 230), turf, groundPhysics);
            var infield = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            infield.name = "Continuous dirt infield"; infield.transform.position = new Vector3(0, .009f, 27);
            infield.transform.localScale = new Vector3(83, .009f, 83); infield.GetComponent<Renderer>().sharedMaterial = dirt;
            UnityEngine.Object.DestroyImmediate(infield.GetComponent<Collider>());
            for (int n = 0; n < 4; n++)
            {
                var pad = SportsEventAuthoring.Box(n == 0 ? "HOME PLATE" : "BASE " + n, KickballMatch.Bases[n] + Vector3.up * .025f, new Vector3(6, .05f, 6), ivory, groundPhysics);
                pad.transform.rotation = Quaternion.Euler(0, 45, 0);
                Chalk(KickballMatch.Bases[n], KickballMatch.Bases[(n + 1) % 4], ivory);
            }
            SportsEventAuthoring.Box("Pitching rubber", new Vector3(0, .026f, 24), new Vector3(4, .052f, 1.4f), ivory);
            Chalk(Vector3.zero, new Vector3(75, 0, 75), ivory);
            Chalk(Vector3.zero, new Vector3(-75, 0, 75), ivory);
            ReuseStadium(); Fence(dark);

            m.agents = new KickballAgent[12];
            var trucks = new SportsEventTruck[12];
            for (int i = 0; i < 12; i++)
            {
                var truck = SportsEventAuthoring.Truck(m, Folder, "Kickball competitor " + (i + 1), new Vector3((i < 6 ? -1 : 1) * 49, .7f, 4 + i % 6 * 5), i);
                SportsRallyAuthoring.FitTruck(truck); truck.maxSpeed = 21; truck.motorTorque = 9; truck.steerAngle = 38;
                var a = truck.gameObject.AddComponent<KickballAgent>(); a.match = m; a.truck = truck;
                a.catchingCup = new GameObject("Visible front ball cradle").transform;
                a.catchingCup.SetParent(truck.transform, false); a.catchingCup.localPosition = new Vector3(0, 1.05f, 1.9f);
                // A visible shallow basket marks the real contact pickup area.
                for (int side = -1; side <= 1; side += 2)
                    SportsEventAuthoring.Box("Cradle side", new Vector3(side * .72f, -.25f, 0), new Vector3(.09f, .45f, 1.1f), gold, null, a.catchingCup);
                SportsEventAuthoring.Box("Cradle lower lip", new Vector3(0, -.43f, .2f), new Vector3(1.5f, .09f, .8f), dark, null, a.catchingCup);
                m.agents[i] = a; trucks[i] = truck;
            }
            var rubber = SportsEventAuthoring.Mat(Folder, "Red pebbled rubber kickball", new Color(.8f, .055f, .025f), .24f);
            rubber.SetTexture("_BumpMap", gravel); rubber.EnableKeyword("_NORMALMAP"); rubber.SetFloat("_BumpScale", .08f);
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere); sphere.name = "ONE authoritative giant rubber kickball"; sphere.layer = 2;
            sphere.transform.position = new Vector3(0, 1.1f, 23); sphere.transform.localScale = Vector3.one * 1.3f;
            sphere.GetComponent<Renderer>().sharedMaterial = rubber;
            var body = sphere.AddComponent<Rigidbody>(); body.mass = .8f; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate; body.linearDamping = .02f; body.angularDamping = .03f; body.maxAngularVelocity = 30;
            var ball = sphere.AddComponent<KickballBall>(); ball.body = body; ball.shape = sphere.GetComponent<SphereCollider>(); ball.match = m;
            ball.shape.sharedMaterial = SportsEventAuthoring.Contact(Folder, "Rubber kickball bounce and grip", .38f, .48f); m.ball = ball;
            m.views = SportsRallyAuthoring.Views(m, trucks);
            var presentation = m.gameObject.AddComponent<KickballViews>(); presentation.match = m; presentation.rally = m.views;
            presentation.cabs = new Transform[4]; presentation.looks = new SportsCabLook[4]; m.presentation = presentation;
            int cabMask = (1 << 26) | (1 << 27) | (1 << 28) | (1 << 29);
            m.views.broadcast.view.cullingMask &= ~cabMask;
            for (int i = 0; i < 4; i++)
            {
                var camera = m.views.views[i].view; SportsChaosAuthoring.Cab(camera, Folder, i);
                presentation.cabs[i] = camera.transform.Find("Driver seat interior");
                presentation.cabs[i].gameObject.SetActive(false);
                presentation.looks[i] = camera.gameObject.AddComponent<SportsCabLook>();
                camera.cullingMask = (camera.cullingMask & ~cabMask) | (1 << (26 + i));
            }
            m.atmosphere = SportsChaosAuthoring.Weather(m, Folder); m.views.weather = m.atmosphere;
            SportsWeatherRenderingAuthoring.Install(m, Folder);
            m.replay = SportsRallyAuthoring.Replay(m, trucks.Select(t => t.visuals).Concat(new[] { sphere.transform }).ToArray());
            m.replay.capacity = 240; m.replay.interval = .05f; m.replay.rolling = true;
            CopyEventAudio(m);
            SportsEventAuthoring.UI(m); SportsRallyAuthoring.CompactUI(m);
            for (int i = 0; i < 12; i++)
            {
                var row = m.menus.rows[i].GetComponent<RectTransform>(); row.sizeDelta = new Vector2(650, 70);
                var name = m.menus.names[i].GetComponent<RectTransform>(); name.sizeDelta = new Vector2(450, 28); name.anchoredPosition = new Vector2(-75, 16);
                m.menus.names[i].textViewport.sizeDelta = new Vector2(425, 28);
                m.menus.names[i].textComponent.rectTransform.sizeDelta = new Vector2(415, 28);
                m.menus.names[i].textComponent.fontSizeMax = 21;
                foreach (var text in new[] { m.menus.sources[i], m.menus.pads[i], m.menus.levels[i], m.menus.colorLabels[i] })
                { text.fontSizeMax = 19; text.fontSizeMin = 14; }
                m.menus.sources[i].transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(350, 30);
                m.menus.sources[i].transform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(-125, -18);
                foreach (var text in new[] { m.menus.pads[i], m.menus.levels[i] })
                { var rect = text.transform.parent.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(170, 30); rect.anchoredPosition = new Vector2(150, -18); }
                var color = m.menus.colorLabels[i].transform.parent.GetComponent<RectTransform>(); color.sizeDelta = new Vector2(110, 28); color.anchoredPosition = new Vector2(255, 16);
            }
            m.viewChoice = SportsUIAuthoring.Button(m.menus.pages[0].transform, "CAMERA · BROADCAST", "view", new Vector2(0, -245), new Vector2(630, 45)).normalTextObj;
            m.menus.countText.transform.parent.gameObject.SetActive(false);
            SportsWeatherPickerAuthoring.Install(m, m.weatherPresets, new Vector2(-360, 255));
            var board = new GameObject("Live baseball scoreboard").AddComponent<TextMeshPro>();
            board.font = m.menus.header.font; board.fontSize = 4; board.alignment = TextAlignmentOptions.Center;
            board.color = new Color(1, .94f, .71f); board.text = "TRUCK KICKBALL\nBLUE  0   |   0  ORANGE";
            board.rectTransform.sizeDelta = new Vector2(40, 15); board.transform.position = new Vector3(0, 13, 113); m.scoreboard = board;
            SportsEventAuthoring.Box("Stadium scoreboard housing", new Vector3(0, 13, 113.3f), new Vector3(46, 17, .6f), dark);
            for (int s = -1; s <= 1; s += 2)
                SportsEventAuthoring.Box("Scoreboard support", new Vector3(s * 17, 5, 114), new Vector3(.6f, 10, .6f), dark);
            InstallStadiumDisplay(m);
            SportsEventAuthoring.Save(Folder); Validate();
        }
        static void Chalk(Vector3 from, Vector3 to, Material material)
        {
            var line = SportsEventAuthoring.Box("Inlaid chalk baseline", (from + to) * .5f + Vector3.up * .035f, new Vector3(.22f, .012f, Vector3.Distance(from, to)), material);
            line.transform.rotation = Quaternion.LookRotation(to - from);
        }
        static void ReuseStadium()
        {
            LWS.TruckFieldGoal.Editor.FieldGoalStadiumAuthoring.Reuse();
            var stadium = GameObject.Find("Reused Union Field stadium render geometry").transform;
            stadium.name = "Existing Union Field baseball stadium seating and press box";
            stadium.localScale = new Vector3(1.8f, 1, 1.3f); stadium.position = new Vector3(0, 0, -12);
        }
        static void Fence(Material material)
        {
            const string source = "Assets/JC_LP_MegaCity/Prefabs/FloorProps/SM_FloorProps_Fence_01.prefab";
            const int count = 56;
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2 / count, b = (i + 1) * Mathf.PI * 2 / count;
                var p = new Vector3(Mathf.Sin(a) * 78, 0, 27 + Mathf.Cos(a) * 78);
                var q = new Vector3(Mathf.Sin(b) * 78, 0, 27 + Mathf.Cos(b) * 78);
                var root = new GameObject("Installed stadium fence " + i).transform; root.position = (p + q) * .5f;
                root.rotation = Quaternion.LookRotation(q - p) * Quaternion.Euler(0, 90, 0);
                LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(root, source, new Vector3(Vector3.Distance(p, q), 3.6f, .25f), 0);
                var collider = root.gameObject.AddComponent<BoxCollider>(); collider.center = Vector3.up * 1.8f; collider.size = new Vector3(Vector3.Distance(p, q), 3.6f, .28f);
            }
        }
        static void CopyEventAudio(KickballMatch match)
        {
            var destination = SceneManager.GetActiveScene();
            var source = EditorSceneManager.OpenScene("Assets/TruckFieldGoal/Scenes/TruckFieldGoal.unity", OpenSceneMode.Additive);
            try
            {
                var old = source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LWS.TruckFieldGoal.FieldGoalMatch>(true)).First();
                match.feedback.clips = (AudioClip[])old.feedback.clips.Clone();
            }
            finally { SceneManager.SetActiveScene(destination); EditorSceneManager.CloseScene(source, true); }
        }
        public static void Validate()
        {
            var m = UnityEngine.Object.FindAnyObjectByType<KickballMatch>();
            if (m == null || m.agents.Length != 12 || m.views.views.Length != 4 || m.replay.sources.Length != 13 || m.weatherPresets == null || m.ball.body.collisionDetectionMode != CollisionDetectionMode.ContinuousDynamic)
                throw new InvalidOperationException("Kickball scene integration is incomplete.");
            if (m.agents.Select(a => a.truck.body).Distinct().Count() != 12) throw new InvalidOperationException("Kickball competitors must own distinct physical trucks.");
            Debug.Log("KICKBALL_SAVED_SCENE_VALIDATED twelve NWH trucks, one ball, shared weather/replay/UI and four owned views");
        }

        [MenuItem("Truck Sports/Kickball/Refresh help and team markings (preserve stadium)")]
        public static void UpgradePresentation()
        {
            EditorSceneManager.OpenScene(Scene);
            var match = UnityEngine.Object.FindAnyObjectByType<KickballMatch>();
            match.menus.pages[4].transform.Find("Rules and controls").GetComponent<TMP_Text>().text = match.Help;
            var blue = SportsEventAuthoring.Mat(Folder, "Blue team ground band", new Color(.08f, .55f, 1), .15f);
            var orange = SportsEventAuthoring.Mat(Folder, "Orange team ground band", new Color(1, .32f, .03f), .15f);
            string directory = "Assets/TruckKickball/Models"; Directory.CreateDirectory(directory); AssetDatabase.Refresh();
            string path = directory + "/Team identification ring.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh { name = "Thin noncolliding team ring" };
                var vertices = new Vector3[128]; var triangles = new int[384];
                for (int i = 0; i < 64; i++)
                {
                    float angle = i * Mathf.PI * 2 / 64;
                    vertices[i * 2] = new Vector3(Mathf.Sin(angle) * 1.55f, 0, Mathf.Cos(angle) * 1.55f);
                    vertices[i * 2 + 1] = new Vector3(Mathf.Sin(angle) * 1.68f, 0, Mathf.Cos(angle) * 1.68f);
                    int next = (i + 1) % 64 * 2, at = i * 6;
                    triangles[at] = i * 2; triangles[at + 1] = next; triangles[at + 2] = i * 2 + 1;
                    triangles[at + 3] = i * 2 + 1; triangles[at + 4] = next; triangles[at + 5] = next + 1;
                }
                mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, path);
            }
            for (int i = 0; i < match.agents.Length; i++)
            {
                var a = match.agents[i]; a.blueTeam = blue; a.orangeTeam = orange;
                if (a.teamMarker == null)
                {
                    var band = new GameObject("Team ground band"); band.transform.SetParent(a.truck.visuals, false);
                    band.transform.SetPositionAndRotation(a.truck.transform.TransformPoint(new Vector3(0, -.3f, 0)), a.truck.transform.rotation);
                    var scale = a.truck.visuals.lossyScale; band.transform.localScale = new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z);
                    band.AddComponent<MeshFilter>().sharedMesh = mesh; a.teamMarker = band.AddComponent<MeshRenderer>();
                    a.teamMarker.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    var ghostBand = UnityEngine.Object.Instantiate(band, match.replay.ghosts[i], false);
                    ghostBand.name = band.name; a.replayTeamMarker = ghostBand.GetComponent<Renderer>();
                }
                a.ApplyTeam(i < 6 ? 0 : 1); EditorUtility.SetDirty(a);
            }
            InstallStadiumDisplay(match);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); AssetDatabase.SaveAssets(); Validate();
        }
        [MenuItem("Truck Sports/Kickball/Repair stadium display and surface materials")]
        public static void UpgradeStadiumDisplay()
        {
            EditorSceneManager.OpenScene(Scene); var match = UnityEngine.Object.FindAnyObjectByType<KickballMatch>();
            InstallStadiumDisplay(match); EditorSceneManager.SaveScene(match.gameObject.scene); AssetDatabase.SaveAssets();
        }
        static void InstallCrowd(KickballMatch match)
        {
            var prior = match.GetComponent<LWS.TruckFieldGoal.FieldGoalCrowd>();
            if (prior != null) { prior.eventSession = match; EditorUtility.SetDirty(prior); return; }
            var preview = EditorSceneManager.OpenPreviewScene(LWS.TruckFieldGoal.Editor.FieldGoalAuthoring.Scene);
            try
            {
                var source = preview.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LWS.TruckFieldGoal.FieldGoalCrowd>(true)).Single();
                var stands = match.gameObject.scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true)).Where(r => r.name == "Union Field Grandstand tier").ToArray();
                if (stands.Length < 8 || source.batches.Length == 0) throw new InvalidOperationException("Actual Union Field stands and existing posed audience assets are required.");
                var crowd = match.gameObject.AddComponent<LWS.TruckFieldGoal.FieldGoalCrowd>(); crowd.eventSession = match;
                crowd.nearFans = Array.Empty<Animator>(); crowd.authoredVersion = 1;
                var root = new GameObject("Kickball audience · reused posed Taxi stadium fans").transform;
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root.gameObject, match.gameObject.scene);
                var seats = new System.Collections.Generic.List<Matrix4x4>[source.batches.Length];
                var fans = new System.Collections.Generic.List<MeshFilter>[source.batches.Length];
                for (int i = 0; i < seats.Length; i++) { seats[i] = new System.Collections.Generic.List<Matrix4x4>(); fans[i] = new System.Collections.Generic.List<MeshFilter>(); }
                int number = 0;
                foreach (var stand in stands) for (int row = 0; row < 4; row++) for (int seat = 0; seat < 72; seat++)
                {
                    int who = number++ % source.batches.Length;
                    var point = stand.transform.TransformPoint(new Vector3(Mathf.Lerp(-.47f, .47f, seat / 71f), .505f, Mathf.Lerp(-.36f, .36f, row / 3f)));
                    var rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(new Vector3(0, point.y, 27) - point, Vector3.up));
                    var fan = new GameObject("Instanced stadium spectator " + number).transform; fan.SetParent(root, false); fan.SetPositionAndRotation(point, rotation); fan.localScale = Vector3.one * 1.25f;
                    var filter = fan.gameObject.AddComponent<MeshFilter>(); filter.sharedMesh = source.batches[who].idle; fans[who].Add(filter);
                    var renderer = fan.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterials = source.batches[who].materials; renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
                    seats[who].Add(Matrix4x4.TRS(point, rotation, Vector3.one * 1.25f));
                }
                crowd.batches = new LWS.TruckFieldGoal.FieldGoalCrowd.Batch[source.batches.Length];
                for (int i = 0; i < crowd.batches.Length; i++) crowd.batches[i] = new LWS.TruckFieldGoal.FieldGoalCrowd.Batch { idle = source.batches[i].idle, cheer = source.batches[i].cheer, materials = source.batches[i].materials, seats = seats[i].ToArray(), fans = fans[i].ToArray() };
                var audio = new GameObject("Bounded event driven audience response").AddComponent<AudioSource>(); audio.transform.SetParent(root, false); audio.playOnAwake = false; audio.spatialBlend = 0;
                crowd.response = audio; crowd.cheer = source.cheer; crowd.boo = source.boo; EditorUtility.SetDirty(crowd);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }
        public static void InstallStadiumDisplay(KickballMatch match)
        {
            // The old four-point 3D font occupied a fraction of this forty-metre
            // board, and its quads were coplanar with the housing's front face.
            var board = match.scoreboard;
            board.transform.position = new Vector3(0, 13, 112.78f);
            board.fontSize = 58; board.enableAutoSizing = true; board.fontSizeMin = 30; board.fontSizeMax = 58; board.textWrappingMode = TextWrappingModes.NoWrap;
            board.rectTransform.sizeDelta = new Vector2(42, 15); board.extraPadding = true;
            board.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            EditorUtility.SetDirty(board);
            var grass = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckFieldGoal/Materials/Textured football turf.mat");
            var turf = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckKickball/Materials/Mown stadium turf.mat");
            var clay = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckKickball/Materials/Raked kickball infield clay.mat");
            var earth = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/3D Haven/Path Painter II/Editor/Demo/Terrains/DocsTerrain/TerrainLayers/stdMudRocky.terrainlayer");
            if (grass == null || turf == null || clay == null || earth == null || earth.diffuseTexture == null)
                throw new InvalidOperationException("Existing owned stadium turf and earth materials are required.");
            turf.mainTexture = grass.mainTexture; turf.SetColor("_BaseColor", grass.GetColor("_BaseColor"));
            turf.SetTexture("_BumpMap", grass.GetTexture("_BumpMap")); turf.SetFloat("_BumpScale", .3f);
            turf.EnableKeyword("_NORMALMAP"); turf.mainTextureScale = Vector2.one * 46;
            clay.mainTexture = earth.diffuseTexture; clay.SetColor("_BaseColor", new Color(.85f, .65f, .46f));
            clay.SetTexture("_BumpMap", earth.normalMapTexture); clay.SetFloat("_BumpScale", .18f);
            clay.EnableKeyword("_NORMALMAP"); clay.mainTextureScale = Vector2.one * 24;
            var infield = match.gameObject.scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshFilter>(true)).First(f => f.name == "Continuous dirt infield");
            string meshPath = AssetDatabase.GetAssetPath(infield.sharedMesh);
            if (infield.sharedMesh.name == "Cylinder" && (string.IsNullOrEmpty(meshPath) || meshPath.StartsWith("Library/", StringComparison.Ordinal) || meshPath.StartsWith("Resources/unity_builtin_extra", StringComparison.Ordinal)))
            {
                const string path = "Assets/TruckKickball/Models/Planar infield surface.asset";
                var planar = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (planar == null)
                {
                    planar = UnityEngine.Object.Instantiate(infield.sharedMesh); planar.name = "Continuous planar infield texture coordinates";
                    var vertices = planar.vertices; var uv = new Vector2[vertices.Length];
                    for (int i = 0; i < vertices.Length; i++) uv[i] = new Vector2(vertices[i].x + .5f, vertices[i].z + .5f);
                    planar.uv = uv; planar.RecalculateTangents(); AssetDatabase.CreateAsset(planar, path);
                }
                infield.sharedMesh = planar; EditorUtility.SetDirty(infield);
            }
            EditorUtility.SetDirty(turf); EditorUtility.SetDirty(clay);
            InstallCrowd(match);
        }
    }
}
