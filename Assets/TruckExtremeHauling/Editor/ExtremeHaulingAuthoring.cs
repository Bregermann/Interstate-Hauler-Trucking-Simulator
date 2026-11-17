using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using LWS.TruckHitchSports;
using LWS.TruckHitchSports.Editor;

namespace LWS.TruckExtremeHauling.Editor
{
    public static class ExtremeHaulingAuthoring
    {
        const string Folder = "TruckExtremeHauling";
        const string Root = "Assets/TruckExtremeHauling";
        static Material steel, ground, ice, accent;
        static PhysicsMaterial contact;
        [MenuItem("Truck Sports/Alpha 0.10/Create advanced hauling (new scenes only)")]
        public static void CreateAll()
        {
            Create<CargoCrisisMatch>("CargoCrisis"); Create<DominoDerbyMatch>("DominoDerby");
            Create<TrailerPlinkoMatch>("TrailerPlinko"); Create<JackknifeIceMatch>("JackknifeOnIce");
            Create<RoadTrainWreckMatch>("RoadTrainWreck"); Create<HitchRingTossMatch>("HitchRingToss");
            Create<ZeroGHitchMatch>("ZeroGHitchUp"); Create<SkyhookSprintMatch>("SkyhookSprint"); Create<ThinIceMatch>("IceRoadReckoning");
        }
        static void Create<T>(string scene) where T : ExtremeHaulingMatch
        {
            string path = Root + "/Scenes/" + scene + ".unity";
            if (File.Exists(path)) { Debug.Log("HH10_PRESERVE " + path); return; }
            var match = SportsEventAuthoring.Begin<T>(Folder); match.setup.count = 4; match.option = 0;
            match.NormalizeSetup(); for (int i = 1; i < 4; i++) match.setup.players[i].source = LWS.TruckBowling.BowlingSource.AI;
            steel = SportsEventAuthoring.Mat(Folder, "Freight tower gunmetal", new Color(.12f, .2f, .26f), .65f);
            ground = SportsEventAuthoring.Mat(Folder, "Quarry and depot asphalt", new Color(.38f, .4f, .37f), .1f);
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/3D Haven/Path Painter II/Editor/Demo/Terrains/DocsTerrain/TerrainLayers/stdMudRocky.terrainlayer");
            if (layer != null) { ground.mainTexture = layer.diffuseTexture; ground.mainTextureScale = Vector2.one * 45; }
            ice = SportsEventAuthoring.Mat(Folder, "Northern blue ice", new Color(.5f, .78f, .88f), .9f);
            accent = SportsEventAuthoring.Mat(Folder, "Freight safety orange", new Color(1, .42f, .025f), .4f);
            contact = SportsEventAuthoring.Contact(Folder, "Freight concrete", .65f, .06f);
            bool frozen = match is ThinIceMatch || match is JackknifeIceMatch, orbital = match is ZeroGHitchMatch;
            SportsEventAuthoring.Box("Continuous venue support", new Vector3(120, match is ThinIceMatch ? -18 : -1, 100), new Vector3(550, 2, 600), frozen ? ice : ground, contact);
            match.trucks = new SportsEventTruck[4]; match.objectives = new Transform[4];
            var trailers = new List<TrailerBody>(); var joints = new List<ConfigurableJoint>();
            for (int i = 0; i < 4; i++)
            {
                Vector3 position = new Vector3(i * 80, 1, -70);
                var truck = match.trucks[i] = HitchAuthoring.CreateTractor(match, i, position);
                var coupler = truck.GetComponent<HitchCoupler>();
                int pieces = match is DominoDerbyMatch ? 12 : match is RoadTrainWreckMatch ? 3 : 1;
                for (int j = 0; j < pieces; j++)
                {
                    var trailer = HitchAuthoring.CreateTrailer(position - Vector3.forward * (15 + j * 16), Quaternion.identity, scene + " physical trailer " + i + ":" + j);
                    trailers.Add(trailer);
                    if (match is DominoDerbyMatch || match is TrailerPlinkoMatch) trailer.gameObject.AddComponent<SportsSnowTrackExclusion>();
                    if (match is DominoDerbyMatch)
                    {
                        trailer.transform.SetPositionAndRotation(new Vector3(i * 80, 0, j * 4.5f), Quaternion.Euler(-90, 0, 0));
                        Physics.SyncTransforms(); float bottom = float.PositiveInfinity;
                        foreach (var shape in trailer.GetComponentsInChildren<Collider>(true)) if (!shape.isTrigger) bottom = Mathf.Min(bottom, shape.bounds.min.y);
                        if (!float.IsInfinity(bottom)) trailer.transform.position += Vector3.up * (.03f - bottom);
                    }
                    else if (j == 0 && !(match is CargoCrisisMatch) && !(match is TrailerPlinkoMatch) && !orbital && !(match is SkyhookSprintMatch))
                        HitchAuthoring.PlaceCoupledAtStart(coupler, trailer);
                    else if (match is RoadTrainWreckMatch)
                    {
                        var previous = trailers[trailers.Count - 2]; var bounds = HitchAuthoring.Bounds(previous.gameObject);
                        Vector3 rear = new Vector3(bounds.center.x, trailer.Kingpin.localPosition.y, bounds.min.z - .25f);
                        trailer.transform.position += previous.transform.TransformPoint(rear) - trailer.Kingpin.position;
                        var joint = previous.gameObject.AddComponent<ConfigurableJoint>(); joint.connectedBody = trailer.Body;
                        joint.autoConfigureConnectedAnchor = false; joint.anchor = rear; joint.connectedAnchor = trailer.transform.InverseTransformPoint(trailer.Kingpin.position);
                        joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Locked;
                        joint.angularXMotion = joint.angularZMotion = ConfigurableJointMotion.Limited; joint.angularYMotion = ConfigurableJointMotion.Free;
                        joint.lowAngularXLimit = new SoftJointLimit { limit = -15 }; joint.highAngularXLimit = new SoftJointLimit { limit = 15 };
                        joint.angularZLimit = new SoftJointLimit { limit = 12 }; joint.breakForce = 2500000; joint.breakTorque = 1800000;
                        joint.enableCollision = false; joint.projectionMode = JointProjectionMode.PositionAndRotation; joints.Add(joint);
                    }
                }
                match.objectives[i] = Depot(i, new Vector3(i * 80, 1, 210));
            }
            match.trailers = trailers.ToArray();
            if (match is CargoCrisisMatch rescue) Rescue(rescue);
            if (match is TrailerPlinkoMatch plinko) Plinko(plinko);
            if (match is RoadTrainWreckMatch road) road.additionalHitches = joints.ToArray();
            if (match is HitchRingTossMatch toss) Rings(toss);
            if (match is ZeroGHitchMatch zero) Orbit(zero);
            if (match is SkyhookSprintMatch sky) Cables(sky);
            if (match is ThinIceMatch thin) IceLake(thin);
            if (match is JackknifeIceMatch skating) { var slip = SportsEventAuthoring.Contact(Folder, "Polished ice contact", .015f, .05f); foreach (var c in UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None)) if (c.name == "Continuous venue support") c.sharedMaterial = slip; }
            Scenery(scene, frozen, orbital);
            match.views = SportsRallyAuthoring.Views(match, match.trucks); match.broadcast = match.views.broadcast;
            match.views.groundOffset = match is TrailerPlinkoMatch ? new Vector3(20, 5, -70) : match is DominoDerbyMatch ? new Vector3(25, 38, -25) : new Vector3(22, 15, -28);
            match.views.groundLookAhead = 2;
            foreach (var camera in match.cameras) camera.farClipPlane = 2200;
            match.broadcast.Pose(new Vector3(0, 5, 0), new Vector3(60, 55, -110), 65, true);
            match.feedback.clips = LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create();
            if (!orbital) { match.presentationWeather = SportsChaosAuthoring.Weather(match, Folder); match.views.weather = match.presentationWeather; SportsWeatherRenderingAuthoring.Install(match, Folder); }
            SportsEventAuthoring.UI(match); SportsRallyAuthoring.CompactUI(match); EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), path); SportsProductionAuthoring.InstallScene(); SportsFocusPolicy.InstallScene();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), path); AssetDatabase.SaveAssets(); Debug.Log("HH10_AUTHORED " + path);
        }
        static Transform Depot(int slot, Vector3 position)
        {
            var root = new GameObject("Player " + (slot + 1) + " cargo destination").transform; root.position = position;
            for (int side = -1; side <= 1; side += 2) SportsEventAuthoring.Box("Delivery lane edge", position + new Vector3(side * 13, -.8f, 0), new Vector3(.4f, .15f, 32), accent);
            SportsEventAuthoring.Box("Delivery stop line", position + new Vector3(0, -.8f, 16), new Vector3(26, .15f, .5f), accent);
            return root;
        }
        static void Rescue(CargoCrisisMatch match)
        {
            match.winches = new SpringJoint[4];
            for (int i = 0; i < 4; i++)
            {
                match.trailers[i].transform.SetPositionAndRotation(new Vector3(i * 80, 3, -45), Quaternion.Euler(0, 0, 65));
                var winch = match.trucks[i].gameObject.AddComponent<SpringJoint>(); winch.autoConfigureConnectedAnchor = false; winch.anchor = new Vector3(0, 1, -2); winch.spring = 0; winch.maxDistance = 5; match.winches[i] = winch;
                Prop("Assets/RoadPropsPack/URP/Prefabs/RoadBlock02_URP.prefab", new Vector3(i * 80 + 12, 0, -40), Quaternion.identity);
            }
        }
        static void Plinko(TrailerPlinkoMatch match)
        {
            match.boardScale=2.5f;match.boardOrigins=new Transform[4];match.landingTrays=new PlinkoLandingTray[4];
            var bounce=SportsEventAuthoring.Contact(Folder,"Plinko peg rebound",.07f,.55f);float scale=match.boardScale;
            for(int i=0;i<4;i++)
            {
                var origin=new GameObject("Physical Plinko board "+i).transform;origin.position=new Vector3(i*180,160,0);match.boardOrigins[i]=origin;
                match.trailers[i].transform.SetPositionAndRotation(origin.position,Quaternion.Euler(90,0,0));
                SportsEventAuthoring.Box("Board back",origin.position+new Vector3(0,-26,5)*scale,new Vector3(52,62,1)*scale,steel,contact,origin);
                match.landingTrays[i]=SportsEventAuthoring.Box("Catch tray",origin.position+new Vector3(0,-55,0)*scale,new Vector3(52,2,12)*scale,steel,contact,origin).AddComponent<PlinkoLandingTray>();
                for(int side=-1;side<=1;side+=2)SportsEventAuthoring.Box("Board sidewall",origin.position+new Vector3(side*26,-26,0)*scale,new Vector3(1,62,12)*scale,accent,bounce,origin);
                for(int row=0;row<6;row++)for(int peg=0;peg<5;peg++)
                {
                    var ball=GameObject.CreatePrimitive(PrimitiveType.Sphere);ball.name="Physical rebound peg";ball.transform.SetParent(origin,false);
                    ball.transform.localPosition=new Vector3(-20+peg*10+(row%2)*4,-8-row*7,0)*scale;ball.transform.localScale=new Vector3(2.5f,2.5f,9)*scale;
                    ball.GetComponent<Renderer>().sharedMaterial=accent;UnityEngine.Object.DestroyImmediate(ball.GetComponent<SphereCollider>());
                    var pin=ball.AddComponent<CapsuleCollider>();pin.direction=2;pin.radius=.5f;pin.height=1;pin.sharedMaterial=bounce;
                }
                for(int bin=0;bin<6;bin++)SportsEventAuthoring.Box("Bin separator",origin.position+new Vector3(-25+bin*10,-51,0)*scale,new Vector3(.6f,7,12)*scale,accent,contact,origin);
            }
            new GameObject("Full-size trailer Plinko spacing v2");
            var support=GameObject.Find("Continuous venue support");support.transform.position=new Vector3(270,-1,80);support.transform.localScale=new Vector3(850,2,500);
        }
        static void Rings(HitchRingTossMatch match)
        {
            match.rings = new HaulingTargetRing[4];
            for (int i = 0; i < 4; i++)
            {
                var ring = new GameObject("True passage target ring").AddComponent<HaulingTargetRing>(); ring.transform.position = new Vector3(i * 80, 9, 25); ring.radius = 12; match.rings[i] = ring;
                for (int j = 0; j < 24; j++) { float a = j * Mathf.PI / 12; var peg = SportsEventAuthoring.Box("Ring frame", ring.transform.position + new Vector3(Mathf.Sin(a) * 12, Mathf.Cos(a) * 12, 0), new Vector3(1, 3.2f, 1), accent, contact); peg.transform.rotation = Quaternion.Euler(0, 0, -a * Mathf.Rad2Deg); }
            }
        }
        static void Orbit(ZeroGHitchMatch match)
        {
            match.thrusters = new OrbitalFreightThrusters[4];
            RenderSettings.ambientLight = new Color(.12f, .18f, .25f);
            for (int i = 0; i < 4; i++)
            {
                match.trucks[i].transform.position = new Vector3(i * 80, 35, -30); match.trailers[i].transform.position = new Vector3(i * 80 + 5, 38, -50);
                var thruster = match.trucks[i].gameObject.AddComponent<OrbitalFreightThrusters>(); thruster.body = match.trucks[i].body; match.thrusters[i] = thruster;
                match.objectives[i].position = new Vector3(i * 80, 40, 100);
                SportsEventAuthoring.Box("Orbital freight landing dock", new Vector3(i * 80, 35, 100), new Vector3(30, 2, 45), steel, contact);
            }
        }
        static void Cables(SkyhookSprintMatch match)
        {
            match.cableCars = new PhysicalCableCar[4];
            for (int i = 0; i < 4; i++)
            {
                var load = match.trailers[i].Body; load.transform.position = new Vector3(i * 80, 45, -70);
                var trolley = new GameObject("Physical constrained cable trolley").AddComponent<Rigidbody>(); Vector3 liftEye=load.centerOfMass+Vector3.up*3;Vector3 trolleyStart=load.transform.TransformPoint(liftEye)+Vector3.up*18;trolley.transform.position=trolleyStart;trolley.position=trolleyStart;trolley.mass = 1000; trolley.useGravity = false;
                var rail = trolley.gameObject.AddComponent<ConfigurableJoint>(); rail.autoConfigureConnectedAnchor = false; rail.connectedAnchor = trolley.position; rail.axis = Vector3.forward; rail.secondaryAxis = Vector3.up;
                rail.xMotion = ConfigurableJointMotion.Limited; rail.yMotion = rail.zMotion = ConfigurableJointMotion.Locked; rail.linearLimit = new SoftJointLimit { limit = 300 }; rail.angularXMotion = rail.angularYMotion = rail.angularZMotion = ConfigurableJointMotion.Locked;
                var spring = load.gameObject.AddComponent<SpringJoint>(); spring.connectedBody = trolley; spring.autoConfigureConnectedAnchor = false; spring.connectedAnchor = Vector3.zero; spring.anchor = liftEye; spring.maxDistance = 18; spring.spring = 120000; spring.damper = 30000;
                var car = load.gameObject.AddComponent<PhysicalCableCar>(); car.load = load; car.trolley = trolley; car.cable = spring; car.routeEnd = new Vector3(i * 80, trolleyStart.y, 210);
                car.line = load.gameObject.AddComponent<LineRenderer>(); car.line.sharedMaterial = steel; car.line.widthMultiplier = .18f; match.cableCars[i] = car;
            }
        }
        static void IceLake(ThinIceMatch match)
        {
            var tiles = new List<LoadSensitiveIce>();
            for (int x = -1; x < 10; x++) for (int z = -3; z < 9; z++)
            {
                var sheet = SportsEventAuthoring.Box("Load-sensitive ice sheet", new Vector3(x * 35, 0, z * 35), new Vector3(35, .7f, 35), ice, contact);
                var tile = sheet.AddComponent<LoadSensitiveIce>(); tile.surface = sheet.GetComponent<Collider>(); tile.iceRenderer = sheet.GetComponent<Renderer>(); tile.thickness = z % 3 == 0 && x % 3 == 1 ? .22f : .65f; tiles.Add(tile);
            }
            match.ice = tiles.ToArray();
        }
        static void Scenery(string scene, bool frozen, bool orbital)
        {
            for (int i = 0; i < 18; i++)
            {
                float x = i % 2 == 0 ? -55 : 330; float z = -100 + i / 2 * 45;
                string asset = frozen ? "Assets/Toon Gas Station/Prefabs/Tree_6A.prefab" : orbital ? "Assets/ShipContainerPack/URP/Prefabs/ShipContainerGray_URP.prefab" : "Assets/ShipContainerPack/URP/Prefabs/ShipContainerBlue_URP.prefab";
                Prop(asset, new Vector3(x, 0, z), Quaternion.Euler(0, i * 37, 0));
            }
            if (!orbital && !frozen) for (int i = 0; i < 4; i++) Prop("Assets/JC_LP_MegaCity/Prefabs/Buildings/SM_Buildings_Warehouse_01.prefab", new Vector3(i * 80, 0, 280), Quaternion.identity);
        }
        static void Prop(string path, Vector3 position, Quaternion rotation)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (source == null) throw new InvalidOperationException("Required owned venue asset missing: " + path);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source); instance.transform.SetPositionAndRotation(position, rotation);
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var original = materials[i]; if (original == null || original.shader.name.StartsWith("Universal Render Pipeline/",StringComparison.Ordinal)) continue;
                    string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original));
                    string asset = Root + "/Materials/Venue " + original.name.Replace('/','_') + " " + guid + ".mat";
                    var converted = AssetDatabase.LoadAssetAtPath<Material>(asset);
                    if (converted == null) { converted = new Material(Shader.Find("Universal Render Pipeline/Lit")); converted.mainTexture = original.mainTexture; if (original.HasProperty("_Color")) converted.color = original.color; AssetDatabase.CreateAsset(converted,asset); }
                    materials[i] = converted;
                }
                renderer.sharedMaterials = materials;
            }
        }
    }
}
