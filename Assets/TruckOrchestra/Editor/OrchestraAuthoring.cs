using System;
using System.IO;
using System.Linq;
using LWS.TruckDanceOff;
using LWS.TruckDanceOff.Editor;
using LWS.TruckMechanicalBull.Editor;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace LWS.TruckOrchestra.Editor
{
    public static class OrchestraAuthoring
    {
        public const string Folder = "TruckOrchestra", ScenePath = "Assets/TruckOrchestra/Scenes/TruckOrchestra.unity";
        static Material wood, ivory, black, gold, velvet;
        static Mesh bevel, round, bell;
        static T Required<T>(string path) where T : UnityEngine.Object
        {
            var item = AssetDatabase.LoadAssetAtPath<T>(path); if (item == null) throw new InvalidOperationException("Installed original asset required: " + path); return item;
        }
        [MenuItem("Truck Sports/Impossible Games/Create NEW Truck Orchestra")]
        public static void Create()
        {
            ImpossibleBalanceArt.GuardNew(ScenePath);
            Required<GameObject>(LWS.TruckFareFight.Editor.FareFightVehicleAuthoring.PrefabPath);
            Required<UniversalRenderPipelineAsset>("Assets/TruckDanceOff/Rendering/Concert pipeline.asset");
            Required<GameObject>("Assets/UTS_FullPack/Models/People/RagDoll Prefabs/Girl_44.prefab");
            var match = SportsEventAuthoring.Begin<OrchestraMatch>(Folder); match.setup.count = 4; match.option = 7; match.NormalizeSetup();
            match.library = OrchestraAudioAuthoring.CreateLibrary(out var score); match.score = score;
            wood = Required<Material>("Assets/TruckBowling/Materials/Maple plank.mat");
            ivory = ImpossibleBalanceArt.Material(Folder, "Warm aged ivory instrument keys", new Color(.91f, .86f, .68f), .4f);
            black = ImpossibleBalanceArt.Material(Folder, "Polished concert piano lacquer", new Color(.028f, .021f, .026f), .8f);
            gold = ImpossibleBalanceArt.Material(Folder, "Tarnished concert brass", new Color(.7f, .43f, .13f), .65f, .85f);
            velvet = ImpossibleBalanceArt.Material(Folder, "Burgundy opera velvet", new Color(.25f, .015f, .045f), .15f);
            bevel = ImpossibleBalanceArt.Mesh(Folder, "Bevelled acoustic instrument cabinet", ImpossibleBalanceArt.RoundedBlock);
            round = ImpossibleBalanceArt.Mesh(Folder, "Original cylindrical concert drum", () => Lathe(new[] { .48f, .5f, .5f, .48f }));
            bell = ImpossibleBalanceArt.Mesh(Folder, "Original flared brass bell", () => Lathe(new[] { .08f, .1f, .14f, .24f, .48f }));
            Hall(match);
            match.dancers = new DanceTruckRig[4]; var trucks = new SportsEventTruck[4];
            string[] names = { "GRAND PIANO", "DRUMS & CYMBALS", "BRASS HORNS", "XYLOPHONE & BELLS" };
            for (int slot = 0; slot < 4; slot++)
            {
                float x = (slot - 1.5f) * 11; var truck = ImpossibleBalanceArt.Tractor(match, slot, new Vector3(x, .8f, 0)); trucks[slot] = truck;
                var rig = truck.gameObject.AddComponent<DanceTruckRig>(); rig.truck = truck; match.dancers[slot] = rig;
                var stage = truck.gameObject.AddComponent<OrchestraAssemblyStage>(); stage.match = match; stage.dancer = rig;
                var root = new GameObject("Owned concert station " + slot + " " + names[slot]).transform; root.position = new Vector3(x, 0, 7);
                var instrument = root.gameObject.AddComponent<OrchestraInstrument>(); instrument.part = slot; instrument.instrumentName = names[slot]; instrument.settings = match.settings;
                instrument.tone = Required<AudioClip>("Assets/TruckOrchestra/Audio/Original instrument " + slot + " C4.wav"); instrument.error = Required<AudioClip>("Assets/TruckOrchestra/Audio/Playful wrong-note blip.wav");
                instrument.errorVoice = Voice(root, "Quiet owned wrong-note source", .65f);
                for (int lane = 0; lane < 4; lane++)
                {
                    var moving = new GameObject("Actual moving instrumental lane " + lane).transform; moving.SetParent(root, false); moving.localPosition = new Vector3((lane - 1.5f) * 1.6f, 2.7f, 0);
                    instrument.movingParts[lane] = moving; instrument.voices[lane] = Voice(moving, "Owned musical note source " + lane, .7f); Instrument(slot, lane, moving);
                }
                Stand(root, slot);
                var lamp = new GameObject("Warm owned concert spotlight").AddComponent<Light>(); lamp.type = LightType.Spot; lamp.range = 40; lamp.spotAngle = 50; lamp.intensity = 3; lamp.color = match.setup.players[slot].color;
                lamp.transform.position = new Vector3(x, 18, -6); lamp.transform.LookAt(new Vector3(x, 1, 4)); instrument.accent = lamp; rig.accent = lamp; match.instruments[slot] = instrument;
            }
            Conductor(match);
            OrchestraPresentationAuthoring.Install(match);
            match.broadcast = SportsEventAuthoring.Camera("Orchestra existing concert Cinemachine broadcast"); match.broadcast.Pose(new Vector3(0, 3, 2), new Vector3(0, 12, -30), 56, true);
            match.stageCamera = match.broadcast.view; match.stageCamera.farClipPlane = 350; match.stageCamera.clearFlags = CameraClearFlags.SolidColor; match.stageCamera.backgroundColor = new Color(.02f, .015f, .025f); match.cameras = new[] { match.stageCamera };
            var concert = match.broadcast.gameObject.AddComponent<DanceConcertCamera>(); concert.match = match; concert.cameraRig = match.broadcast; concert.minimumShotSeconds = 9; concert.transitionSeconds = 1.6f;
            var rendering = match.gameObject.AddComponent<LWS.TruckBaseJumping.BaseJumpRendering>(); rendering.pipeline = Required<UniversalRenderPipelineAsset>("Assets/TruckDanceOff/Rendering/Concert pipeline.asset");
            match.music = Voice(match.transform, "Original harmonic bed DSP source", 0); match.music.priority = 20;
            match.calibrationAudio = Voice(match.transform, "Original shared input calibration pulse", 0); match.calibrationAudio.clip = Required<AudioClip>("Assets/TruckDanceOff/Audio/Calibration pulse.wav");
            match.feedback.clips = (AudioClip[])LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create().Clone();
            SportsEventAuthoring.UI(match); SportsRallyAuthoring.CompactUI(match); Setup(match); DanceAuthoring.InstallRhythmHud(match); StyleRhythmHud(match);
            SportsEventAuthoring.Save(Folder); SportsTruckAppearanceHostAuthoring.BindNewImpossibleScene(match, trucks); EditorSceneManager.SaveScene(match.gameObject.scene);
            Debug.Log("ORCHESTRA_NEW_SCENE_SAVED original full NWH4, shared Dance DSP/rules/input/calibration/concert camera,7 original arrangements/112 parts, actual note-triggered instrument audio/motion, rigged conductor and animated concert audience. Native/visual/listening acceptance pending.");
        }
        static AudioSource Voice(Transform parent, string name, float spatial)
        {
            var source = new GameObject(name).AddComponent<AudioSource>(); source.transform.SetParent(parent, false); source.playOnAwake = false; source.loop = false; source.spatialBlend = spatial; source.rolloffMode = AudioRolloffMode.Linear; source.minDistance = 20; source.maxDistance = 150; return source;
        }
        static GameObject Part(Transform parent, string name, Mesh mesh, Material material, Vector3 position, Vector3 scale, Quaternion rotation = default) => ImpossibleBalanceArt.Part(parent, name, mesh, material, position, scale, rotation);
        static void Instrument(int part, int lane, Transform root)
        {
            if (part == 0)
            {
                for (int key = 0; key < 4; key++)
                {
                    Part(root, "Individually painted ivory piano key", bevel, ivory, new Vector3((key - 1.5f) * .3f, 0, 0), new Vector3(.27f, .2f, 2.1f));
                    if (key < 3) Part(root, "Raised ebony accidental key", bevel, black, new Vector3((key - 1) * .3f, .18f, .45f), new Vector3(.15f, .18f, 1));
                }
            }
            else if (part == 1)
            {
                Part(root, lane == 3 ? "Actually wobbling brass crash cymbal" : "Tuned drum shell", lane == 3 ? bell : round, lane == 3 ? gold : velvet, Vector3.zero, new Vector3(2.2f, lane == 3 ? .13f : 1.2f, 2.2f));
                if (lane != 3) Part(root, "Ivory taut drum skin", round, ivory, new Vector3(0, 1.22f, 0), new Vector3(2.1f, .05f, 2.1f));
                Part(root, "Animated wooden percussion mallet", bevel, wood, new Vector3(.5f, 1.7f, -.3f), new Vector3(.13f, .13f, 2.2f), Quaternion.Euler(-18, 12, 0));
            }
            else if (part == 2)
            {
                Part(root, "Flared sounding brass instrument", bell, gold, Vector3.zero, new Vector3(2.3f, 3.7f, 2.3f), Quaternion.Euler(-70, 0, 0));
                for (int valve = 0; valve < 3; valve++) Part(root, "Polished trumpet valve and cap", round, ivory, new Vector3((valve - 1) * .32f, .3f, .4f), new Vector3(.22f, .65f, .22f));
            }
            else
            {
                Part(root, "Resonant concert marimba bar", bevel, lane % 2 == 0 ? wood : gold, Vector3.zero, new Vector3(1.3f, .22f, 2.6f - lane * .25f));
                Part(root, "Hanging tuned orchestral bell", bell, gold, new Vector3(0, 2.8f, 1), new Vector3(.8f, 1.2f, .8f), Quaternion.Euler(180, 0, 0));
                Part(root, "Striking felt-tipped mallet", bevel, ivory, new Vector3(.2f, .6f, -.3f), new Vector3(.15f, .15f, 1.7f));
            }
        }
        static void Stand(Transform root, int part)
        {
            if (part == 0)
            {
                Part(root, "Giant grand piano curved lacquer cabinet", bevel, black, new Vector3(0, 2, 2.2f), new Vector3(8, 1.4f, 5));
                Part(root, "Open grand piano angled lid", bevel, black, new Vector3(0, 5, 3.5f), new Vector3(8, .2f, 5), Quaternion.Euler(-25, 0, 0));
                for (int stringId = 0; stringId < 22; stringId++) Part(root, "Visible brass soundboard string", bevel, gold, new Vector3((stringId - 10.5f) * .28f, 2.76f, 2.5f), new Vector3(.025f, .025f, 3.8f));
            }
            for (int side = -1; side <= 1; side += 2) Part(root, "Instrument carved leg", bevel, part == 0 ? black : gold, new Vector3(side * 3, 1, 1), new Vector3(.35f, 2.1f, .5f));
        }
        static void Hall(OrchestraMatch match)
        {
            var contact = SportsEventAuthoring.Contact(Folder, "Original concert stage contact", .8f, .02f);
            SportsEventAuthoring.Box("Actual sprung maple concert stage", new Vector3(0, -.45f, 7), new Vector3(65, .8f, 38), wood, contact);
            var root = new GameObject("Grand ornate concert hall and audience galleries").transform;
            Part(root, "Concert hall burgundy rear acoustic wall", bevel, velvet, new Vector3(0, 14, 29), new Vector3(75, 30, 1.2f));
            for (int side = -1; side <= 1; side += 2)
            {
                Part(root, "Concert hall acoustic side wall", bevel, velvet, new Vector3(side * 37, 13, 0), new Vector3(1.2f, 28, 75));
                for (int col = 0; col < 8; col++)
                {
                    Part(root, "Carved gilded gallery pilaster", round, gold, new Vector3(side * 35.8f, 0, -23 + col * 7), new Vector3(1.5f, 20, 1.5f));
                    Part(root, "Warm architectural wall lantern", bell, ivory, new Vector3(side * 34.6f, 10, -23 + col * 7), new Vector3(1.6f, 2.2f, 1.6f));
                }
            }
            for (int pleat = 0; pleat < 36; pleat++)
            {
                float x = (pleat - 17.5f) * 2;
                Part(root, "Gathered burgundy theater curtain fold", round, velvet, new Vector3(x, 3, 27), new Vector3(2.4f, 22, 2.4f));
                Part(root, "Gilded curtain top cornice", bevel, gold, new Vector3(x, 25, 26), new Vector3(2.2f, 1, 2));
            }
            Part(root, "Ornate conductor podium", bevel, wood, new Vector3(0, .5f, -10), new Vector3(5, 1, 4));
            var title = SportsEventAuthoring.Label("Concert venue original enamel title", "THE SIX-TON SYMPHONY", new Vector3(0, 16, 25), .42f, new Color(1, .84f, .45f)); title.transform.rotation = Quaternion.identity;
            var prefab = Required<GameObject>("Assets/UTS_FullPack/Models/People/RagDoll Prefabs/Girl_44.prefab");
            var controller = Required<RuntimeAnimatorController>("Assets/TruckFieldGoal/Crowd/Stadium reactions.controller");
            for (int row = 0; row < 4; row++) for (int seat = 0; seat < 16; seat++)
            {
                float x = (seat - 7.5f) * 3.8f, z = -24 - row * 3;
                Part(root, "Velvet concert audience seat", bevel, velvet, new Vector3(x, .6f + row * .8f, z), new Vector3(2.2f, 1.2f, 1.8f));
                var actor = SportsAudienceAuthoring.Spawn(prefab, root, new Vector3(x, .9f + row * .8f, z), new Vector3(x, 1, 6), controller, .9f); actor.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            }
            RenderSettings.skybox = null; RenderSettings.fog = false; RenderSettings.ambientLight = new Color(.35f, .28f, .26f);
            foreach (var sun in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.type == LightType.Directional)) { sun.intensity = 1.15f; sun.color = new Color(1, .87f, .71f); }
        }
        static void Conductor(OrchestraMatch match)
        {
            var root = new GameObject("Actual skinned concert conductor").transform;
            var actor = SportsAudienceAuthoring.Spawn(Required<GameObject>("Assets/UTS_FullPack/Models/People/RagDoll Prefabs/Girl_44.prefab"), root, new Vector3(0, 1.05f, -10), new Vector3(0, 1, 7), null, 2);
            actor.enabled = false; actor.runtimeAnimatorController = null;
            var bones = actor.GetComponentsInChildren<Transform>(true); var conductor = root.gameObject.AddComponent<OrchestraConductor>();
            conductor.actorSpace = actor.transform; conductor.leftArm = bones.Single(t => t.name == "Bip01 L UpperArm"); conductor.rightArm = bones.Single(t => t.name == "Bip01 R UpperArm"); conductor.torso = bones.Single(t => t.name == "Bip01 Spine2");
            var hand = bones.Single(t => t.name == "Bip01 R Hand"); conductor.baton = Part(hand, "Visible ivory conducting baton", bevel, ivory, new Vector3(0, .1f, .35f), new Vector3(.025f, .025f, .65f)).transform; match.conductor = conductor;
        }
        static void Setup(OrchestraMatch match)
        {
            var parent = match.menus.pages[0].transform;
            match.formatChoice = SportsUIAuthoring.Button(parent, "SINGLE SONG", "dance-format", new Vector2(-360, -245), new Vector2(480, 42)).normalTextObj;
            match.ensembleChoice = SportsUIAuthoring.Button(parent, "ENSEMBLE", "orchestra-ensemble", new Vector2(300, -245), new Vector2(650, 42)).normalTextObj;
            match.calibrationText = SportsUIAuthoring.Text("Saved instrumental timing offset", parent, "", new Vector2(540, 36), new Vector2(0, -300), 21);
            SportsUIAuthoring.Button(parent, "-", "dance-offset-", new Vector2(-350, -300), new Vector2(80, 38)); SportsUIAuthoring.Button(parent, "+", "dance-offset+", new Vector2(350, -300), new Vector2(80, 38));
            SportsUIAuthoring.Button(parent, "TAP CALIBRATION", "dance-calibrate", new Vector2(0, -350), new Vector2(430, 38));
        }
        static void StyleRhythmHud(OrchestraMatch match)
        {
            var hud = match.rhythmHud; hud.arrows = new Sprite[4];
            for (int lane = 0; lane < 4; lane++) hud.arrows[lane] = Required<Sprite>("Assets/Heat - Complete Modern UI/Textures/Icons/Navigation/Arrow " + new[] { "Left", "Bottom", "Top", "Right" }[lane] + " (64x).png");
            for (int i = 0; i < 4; i++)
            {
                var s = hud.strips[i]; s.root.sizeDelta = new Vector2(340, 370); s.root.anchoredPosition = new Vector2((i - 1.5f) * 370, -310); s.root.gameObject.AddComponent<SportsBroadcastPlate>().cut = 13;
                Place(s.title, 150, 24); Place(s.combo, 115, 25); Place(s.routine, -138, 20); Place(s.controls, -167, 15); Place(s.judgment, -108, 24);
                var field = (RectTransform)s.root.Find("Clipped note field"); field.sizeDelta = new Vector2(310, 190); field.anchoredPosition = new Vector2(0, -25);
                for (int lane = 0; lane < 4; lane++)
                {
                    var rect = s.receptors[lane].rectTransform; rect.localRotation = Quaternion.identity; rect.sizeDelta = new Vector2(34, 34); rect.anchoredPosition = new Vector2((lane - 1.5f) * 68, -80); s.receptors[lane].sprite = hud.arrows[lane]; s.receptors[lane].preserveAspect = true;
                    var rail = (RectTransform)field.Find("Lane " + lane); rail.sizeDelta = new Vector2(2, 180); rail.anchoredPosition = new Vector2((lane - 1.5f) * 68, 0);
                }
                foreach (var note in s.notes) note.localRotation = Quaternion.identity;
            }
        }
        static void Place(TMP_Text text, float y, int font) { text.rectTransform.anchoredPosition = new Vector2(0, y); text.rectTransform.sizeDelta = new Vector2(320, 32); text.fontSize = text.fontSizeMax = font; text.fontSizeMin = font * .85f; }
        static Mesh Lathe(float[] radii)
        {
            const int sides = 24; var vertices = new Vector3[radii.Length * (sides + 1)]; var uv = new Vector2[vertices.Length]; var indices = new int[(radii.Length - 1) * sides * 6]; int at = 0;
            for (int ring = 0; ring < radii.Length; ring++) for (int side = 0; side <= sides; side++)
            {
                float t = side / (float)sides, angle = t * Mathf.PI * 2; int i = ring * (sides + 1) + side;
                vertices[i] = new Vector3(Mathf.Cos(angle) * radii[ring], ring / (float)(radii.Length - 1), Mathf.Sin(angle) * radii[ring]); uv[i] = new Vector2(t, ring / (float)(radii.Length - 1));
                if (ring == 0 || side == sides) continue; int a = i - sides - 1; indices[at++] = a; indices[at++] = i; indices[at++] = a + 1; indices[at++] = a + 1; indices[at++] = i; indices[at++] = i + 1;
            }
            var mesh = new Mesh { vertices = vertices, uv = uv, triangles = indices }; mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
}
