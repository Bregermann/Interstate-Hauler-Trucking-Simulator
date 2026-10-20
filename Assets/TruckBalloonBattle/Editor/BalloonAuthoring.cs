using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using LWS.TruckFareFight.Editor;
using LWS.TruckFieldGoal;
namespace LWS.TruckBalloonBattle.Editor
{
    public static class BalloonAuthoring
    {
        public const string Folder = "TruckBalloonBattle", Scene = "Assets/TruckBalloonBattle/Scenes/TruckBalloonBattle.unity";
        public static void Upgrade()
        {
            var scene = EditorSceneManager.OpenScene(Scene); var match = UnityEngine.Object.FindAnyObjectByType<BalloonMatch>();
            foreach (int page in new[] { 0, 4 }) { var artwork = match.menus.pages[page].transform.Find("Original Balloon Battle title artwork"); if (artwork != null) artwork.name = "Original sport title artwork"; }
            foreach (var target in match.targets)
            {
                if (target.contact is SphereCollider) { UnityEngine.Object.DestroyImmediate(target.contact); var capsule = target.gameObject.AddComponent<CapsuleCollider>(); capsule.radius = 1.4f; capsule.height = 3.7f; capsule.isTrigger = true; target.contact = capsule; }
            }
            foreach (var effect in match.confetti) Paper(effect);
            foreach (var spike in match.spikes) Tip(spike);
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
        public static void Create()
        {
            if (File.Exists(Scene)) throw new InvalidOperationException("Preserve the authored Balloon Battle scene. Use an additive repair.");
            var m = SportsEventAuthoring.Begin<BalloonMatch>(Folder); m.setup.count = 8; m.NormalizeSetup();
            m.feedback.clips = LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create();
            var purple = SportsEventAuthoring.Mat(Folder, "Party plum rubberized track", new Color(.24f, .075f, .35f), .15f);
            var teal = SportsEventAuthoring.Mat(Folder, "Electric lagoon lanes", new Color(.035f, .42f, .49f), .25f);
            var gold = SportsEventAuthoring.Mat(Folder, "Marigold party trim", new Color(1, .58f, .06f), .3f);
            var ground = SportsEventAuthoring.Contact(Folder, "Party track contact", .8f, .02f);
            SportsEventAuthoring.Box("Continuous party stadium collision base", new Vector3(0, -.5f, 0), new Vector3(180, 1, 180), teal, ground);
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder); pad.name = "Broad plum battle pad"; pad.transform.localScale = new Vector3(108, .025f, 108); pad.GetComponent<Renderer>().sharedMaterial = purple; UnityEngine.Object.DestroyImmediate(pad.GetComponent<Collider>());
            var containment = SportsEventAuthoring.Contact(Folder, "Rounded bumper contact", .2f, .05f);
            for (int i = 0; i < 64; i++)
            {
                float a = i * Mathf.PI * 2 / 64, b = (i + 1) * Mathf.PI * 2 / 64;
                var p = new Vector3(Mathf.Sin(a) * 55, 1, Mathf.Cos(a) * 55); var q = new Vector3(Mathf.Sin(b) * 55, 1, Mathf.Cos(b) * 55);
                var wall = SportsEventAuthoring.Box("Continuous rounded party bumper", (p + q) * .5f, new Vector3(.7f, 2, Vector3.Distance(p, q) + .05f), i % 2 == 0 ? teal : gold, containment); wall.transform.rotation = Quaternion.LookRotation(q - p);
                if (i % 4 == 0) SportsRallyAuthoring.Prop(null, "Assets/RoadPropsPack/URP/Prefabs/RoadBlock02_URP.prefab", p - Vector3.up, new Vector3(3, 1.6f, 1));
            }
            foreach (int side in new[] { -1, 1 })
                SportsEasyRoadsAuthoring.Bake(Folder, "Broad party launch bank " + side, new[] { new Vector3(side * 24, .035f, -20), new Vector3(side * 24, .15f, -15), new Vector3(side * 24, 1.4f, -7), new Vector3(side * 24, 2.2f, 0), new Vector3(side * 24, 1.4f, 7), new Vector3(side * 24, .15f, 15), new Vector3(side * 24, .035f, 20) }, 12, side < 0 ? teal : gold, ground);
            Stadium(m); Dress(teal, gold, purple);
            m.trucks = new SportsEventTruck[8]; m.fragments = new SportsFragmentRig[8]; m.targets = new BalloonTarget[24]; m.spikes = new BalloonSpike[8]; m.balloonGroups = new Transform[8]; m.confetti = new ParticleSystem[8];
            var shell = SportsEventAuthoring.Mat(Folder, "Glossy colored balloon rubber", Color.white, .7f);
            var stringMat = SportsEventAuthoring.Mat(Folder, "Braided cream balloon cord", new Color(1, .91f, .65f), .1f);
            var steel = SportsEventAuthoring.Mat(Folder, "Retractable polished spike", new Color(.6f, .72f, .78f), .8f);
            var glow = SportsEventAuthoring.Mat(Folder, "Readable spike signal lamp", Color.cyan, .5f); glow.EnableKeyword("_EMISSION"); glow.SetColor("_EmissionColor", Color.cyan);
            for (int i = 0; i < 8; i++)
            {
                var truck = FareFightVehicleAuthoring.Spawn(m, i, m.Grid(i), Quaternion.LookRotation(-Vector3.ProjectOnPlane(m.Grid(i), Vector3.up))); m.trucks[i] = truck;
                m.fragments[i] = SportsFragmentAuthoring.InstallRigid(truck.body, truck.visuals.GetComponentsInChildren<MeshFilter>(true), Folder);
                m.balloonGroups[i] = new GameObject("Recorded tethered balloon group " + i).transform;
                for (int b = 0; b < 3; b++)
                {
                    var actor = new GameObject("Actual balloon target " + i + ":" + b); actor.transform.SetParent(m.balloonGroups[i], false);
                    var target = actor.AddComponent<BalloonTarget>(); target.match = m; target.slot = i; target.index = b; target.home = new Vector3((b - 1) * 2.8f, b == 1 ? 8.2f : 7, -1.7f); m.targets[i * 3 + b] = target;
                    var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere); visual.name = "Glossy pear-shaped balloon"; visual.transform.SetParent(actor.transform, false); visual.transform.localScale = new Vector3(2.8f, 3.7f, 2.8f); UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>()); visual.GetComponent<Renderer>().sharedMaterial = shell;
                    var color = new MaterialPropertyBlock(); color.SetColor("_BaseColor", Color.HSVToRGB((i * .137f + b * .12f) % 1, .75f, 1)); visual.GetComponent<Renderer>().SetPropertyBlock(color);
                    target.body = actor.AddComponent<Rigidbody>(); target.body.isKinematic = true; target.body.useGravity = false; target.body.interpolation = RigidbodyInterpolation.Interpolate; target.body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                    var balloonCollider = actor.AddComponent<CapsuleCollider>(); balloonCollider.radius = 1.4f; balloonCollider.height = 3.7f; balloonCollider.isTrigger = true; target.contact = balloonCollider;
                    target.cord = new Transform[2];
                    for (int c = 0; c < 2; c++) { var cord = GameObject.CreatePrimitive(PrimitiveType.Cylinder); cord.name = "Recorded flexible tether segment"; cord.transform.SetParent(m.balloonGroups[i], false); UnityEngine.Object.DestroyImmediate(cord.GetComponent<Collider>()); cord.GetComponent<Renderer>().sharedMaterial = stringMat; target.cord[c] = cord.transform; }
                }
                Spike(m, i, steel, glow); Confetti(m, i);
            }
            m.broadcast = SportsEventAuthoring.Camera("Balloon party broadcast"); m.broadcast.view.orthographic = true;
            m.framing = m.broadcast.gameObject.AddComponent<SportsCameraFit>(); m.framing.cameraTarget = m.broadcast.view; m.framing.arenaFit = true; m.framing.topInset = .1f; m.framing.bottomInset = .25f; m.cameras = new[] { m.broadcast.view };
            SportsEventAuthoring.UI(m); SportsRallyAuthoring.CompactUI(m);
            m.drivingViews = SportsDrivingViewsAuthoring.Install(m, m.trucks, m.broadcast, Folder); m.drivingViews.independentViewModes = true;
            m.atmosphere = SportsChaosAuthoring.Weather(m, Folder); m.atmosphere.presets = SportsWeatherPresetsAuthoring.EnsureCatalog(); m.drivingViews.rally.weather = m.atmosphere;
            SportsWeatherPickerAuthoring.Install(m, m.atmosphere.presets, new Vector2(350, -180));
            SportsUIAuthoring.Button(m.menus.pages[6].transform, "WATCH THE POPS", "watchballoon", new Vector2(0, -245), new Vector2(520, 48));
            var hud = m.gameObject.AddComponent<SportsExpansionViewHud>(); hud.match = m; hud.views = m.drivingViews;
            var spectator = m.gameObject.AddComponent<SportsExpansionSpectators>(); spectator.match = m; spectator.views = m.drivingViews;
            Artwork(m);
            SportsBroadcastAuthoring.ApplyScene(); SportsEventAuthoring.Save(Folder);
        }
        static void Spike(BalloonMatch m, int slot, Material steel, Material lamp)
        {
            var root = new GameObject("Telescoping roof attack mast").transform; root.SetParent(m.trucks[slot].transform, false);
            var mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder); mast.name = "Visible spike mast"; mast.transform.SetParent(root, false); mast.transform.localPosition = new Vector3(0, 4.2f, 1.2f); mast.transform.localScale = new Vector3(.3f, 2.1f, .3f); mast.GetComponent<Renderer>().sharedMaterial = steel; UnityEngine.Object.DestroyImmediate(mast.GetComponent<Collider>());
            var attack = new GameObject("Retractable contact tip"); attack.transform.SetParent(root, false); attack.transform.localPosition = new Vector3(0, 7, 5.5f);
            var component = attack.AddComponent<BalloonSpike>(); component.match = m; component.slot = slot; m.spikes[slot] = component;
            component.contact = attack.AddComponent<BoxCollider>(); component.contact.isTrigger = true; component.contact.size = new Vector3(1.6f, 2.7f, 7.5f); component.contact.enabled = false;
            var spear = GameObject.CreatePrimitive(PrimitiveType.Cylinder); spear.name = "Animated telescoping attack spar"; spear.transform.SetParent(root, false); spear.transform.localPosition = new Vector3(0, 7, 1.2f); spear.transform.localRotation = Quaternion.Euler(90, 0, 0); spear.transform.localScale = new Vector3(.22f, 4, .22f); spear.GetComponent<Renderer>().sharedMaterial = steel; UnityEngine.Object.DestroyImmediate(spear.GetComponent<Collider>());
            // Scale a neutral parent along Z, retaining the cylinder's local axis.
            component.spear = new GameObject("Spike extension presentation").transform; component.spear.SetParent(root, false); component.spear.localPosition = new Vector3(0, 7, 1.2f); spear.transform.SetParent(component.spear, true); spear.transform.localPosition = Vector3.forward * 3.5f;
            var light = GameObject.CreatePrimitive(PrimitiveType.Sphere); light.name = "Yellow arm / red attack / cyan ready lamp"; light.transform.SetParent(root, false); light.transform.localPosition = new Vector3(0, 7, 1.2f); light.transform.localScale = Vector3.one * .9f; UnityEngine.Object.DestroyImmediate(light.GetComponent<Collider>()); component.lamp = light.GetComponent<Renderer>(); component.lamp.sharedMaterial = lamp;
            Tip(component);
        }
        static void Tip(BalloonSpike spike)
        {
            if (spike.spear.Find("Visible pointed contact tip") != null) return;
            string path = "Assets/TruckBalloonBattle/Materials/Original pointed attack tip.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh { name = "Original low-poly pointed spike tip" }; var vertices = new Vector3[18]; var triangles = new int[96];
                vertices[0] = new Vector3(0, 0, 1.25f); vertices[17] = Vector3.zero;
                for (int i = 0; i < 16; i++) { float a = i * Mathf.PI * 2 / 16; vertices[i + 1] = new Vector3(Mathf.Sin(a) * .65f, Mathf.Cos(a) * .65f, 0); int at = i * 6; triangles[at] = 0; triangles[at + 1] = i + 1; triangles[at + 2] = (i + 1) % 16 + 1; triangles[at + 3] = 17; triangles[at + 4] = triangles[at + 2]; triangles[at + 5] = i + 1; }
                mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, path);
            }
            var tip = new GameObject("Visible pointed contact tip"); tip.transform.SetParent(spike.spear, false); tip.transform.localPosition = Vector3.forward * 6.8f; tip.AddComponent<MeshFilter>().sharedMesh = mesh;
            tip.AddComponent<MeshRenderer>().sharedMaterial = spike.lamp.sharedMaterial;
        }
        static void Confetti(BalloonMatch m, int slot)
        {
            var effect = new GameObject("Pooled paper confetti " + slot).AddComponent<ParticleSystem>(); SportsChaosAuthoring.FixBlast(effect, SportsChaosAuthoring.Flare(Folder));
            var main = effect.main; main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 3.2f); main.startSpeed = new ParticleSystem.MinMaxCurve(5, 13); main.startSize = new ParticleSystem.MinMaxCurve(.2f, .55f); main.gravityModifier = .6f; main.maxParticles = 220; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.cyan, 0), new GradientColorKey(Color.magenta, .3f), new GradientColorKey(Color.yellow, .65f), new GradientColorKey(Color.green, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) }); main.startColor = new ParticleSystem.MinMaxGradient(gradient) { mode = ParticleSystemGradientMode.RandomColor };
            var over = effect.colorOverLifetime; over.enabled = false; var emission = effect.emission; emission.SetBursts(Array.Empty<ParticleSystem.Burst>()); emission.rateOverTime = 0;
            var rotation = effect.rotationOverLifetime; rotation.enabled = true; rotation.z = new ParticleSystem.MinMaxCurve(-5, 5); m.confetti[slot] = effect;
            Paper(effect);
        }
        static void Paper(ParticleSystem effect)
        {
            var material = SportsEventAuthoring.Mat(Folder, "Original cut-paper confetti", Color.white);
            material.shader = Shader.Find("Universal Render Pipeline/Particles/Unlit"); material.SetFloat("_Cull", 0);
            var template = GameObject.CreatePrimitive(PrimitiveType.Quad); var renderer = effect.GetComponent<ParticleSystemRenderer>(); renderer.renderMode = ParticleSystemRenderMode.Mesh; renderer.mesh = template.GetComponent<MeshFilter>().sharedMesh; renderer.sharedMaterial = material; UnityEngine.Object.DestroyImmediate(template);
        }
        static void Stadium(BalloonMatch m)
        {
            var scene = SceneManager.GetActiveScene(); var source = EditorSceneManager.OpenScene(LWS.TruckHotPotato.Editor.HotPotatoAuthoring.Scene, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene); var hot = source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LWS.TruckHotPotato.HotPotatoMatch>(true)).First();
                var stand = source.GetRootGameObjects().First(g => g.name == "Reused Hot Potato Existing Union Field grandstands and press boxes");
                var venue = UnityEngine.Object.Instantiate(stand); venue.name = "Reused owned Union Field party stands"; SceneManager.MoveGameObjectToScene(venue, scene); venue.transform.position *= .8f; venue.transform.localScale *= .8f;
                var fans = UnityEngine.Object.Instantiate(hot.crowd.batches[0].fans[0].transform.parent.gameObject); fans.name = "Reused shared-mesh party spectators"; SceneManager.MoveGameObjectToScene(fans, scene); fans.transform.localScale *= .8f;
                var all = fans.GetComponentsInChildren<MeshFilter>(); m.crowd = m.gameObject.AddComponent<FieldGoalCrowd>(); m.crowd.eventSession = m; m.crowd.nearFans = Array.Empty<Animator>(); m.crowd.batches = new FieldGoalCrowd.Batch[hot.crowd.batches.Length];
                for (int i = 0; i < m.crowd.batches.Length; i++) { var original = hot.crowd.batches[i]; var posed = all.Where(f => f.sharedMesh == original.idle).ToArray(); m.crowd.batches[i] = new FieldGoalCrowd.Batch { idle = original.idle, cheer = original.cheer, materials = original.materials, fans = posed, seats = posed.Select(f => f.transform.localToWorldMatrix).ToArray() }; }
                m.crowd.response = m.gameObject.AddComponent<AudioSource>(); m.crowd.response.playOnAwake = false; m.crowd.cheer = hot.crowd.cheer; m.crowd.boo = hot.crowd.boo;
            }
            finally { EditorSceneManager.CloseScene(source, true); SceneManager.SetActiveScene(scene); }
        }
        static void Dress(Material teal, Material gold, Material purple)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TruckLimbo/Materials/Original cloth pennant.asset");
            for (int side = -1; side <= 1; side += 2) for (int i = 0; i < 34; i++)
            {
                var flag = new GameObject("Reused authored festival cloth pennant"); flag.transform.position = new Vector3(-51 + i * 3.1f, 9 + Mathf.Sin(i * .2f), side * 59); flag.transform.localScale = Vector3.one * 1.4f;
                flag.AddComponent<MeshFilter>().sharedMesh = mesh; flag.AddComponent<MeshRenderer>().sharedMaterial = i % 3 == 0 ? teal : i % 3 == 1 ? gold : purple;
            }
        }
        static void Artwork(BalloonMatch m)
        {
            var logo = SportsRetroMenuAuthoring.LoadLogo(Folder);
            foreach (int page in new[] { 0, 4 }) { var art = SportsUIAuthoring.Rect("Original sport title artwork", m.menus.pages[page].transform, new Vector2(260, 160), new Vector2(-560, 365)).AddComponent<UnityEngine.UI.Image>(); art.sprite = logo; art.preserveAspect = true; art.raycastTarget = false; var title = m.menus.pages[page].transform.Find("Title").GetComponent<TMP_Text>(); title.rectTransform.anchoredPosition += Vector2.right * 140; title.rectTransform.sizeDelta = new Vector2(1000, title.rectTransform.sizeDelta.y); }
            var material = SportsEventAuthoring.Mat(Folder, "Original Balloon Battle arena artwork", Color.white); material.shader = Shader.Find("Universal Render Pipeline/Unlit"); material.mainTexture = logo.texture; material.SetFloat("_Surface", 1); material.SetFloat("_SrcBlend", 5); material.SetFloat("_DstBlend", 10); material.SetFloat("_ZWrite", 0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000;
            var artFloor = GameObject.CreatePrimitive(PrimitiveType.Quad); artFloor.name = "Original Balloon Battle party floor logo"; UnityEngine.Object.DestroyImmediate(artFloor.GetComponent<Collider>()); artFloor.transform.SetPositionAndRotation(new Vector3(0, .06f, 0), Quaternion.Euler(90, 0, 0)); artFloor.transform.localScale = new Vector3(28, 18, 1); artFloor.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
