#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.InterstateHauler;
using LWS.TruckDragonKingdom;
using LWS.TruckDragonKingdom.Editor;
using LWS.TruckJenga;
using LWS.TruckMechanicalBull.Editor;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckDragonEggDelivery.Editor
{
    public static class DragonEggDeliveryAuthoring
    {
        public const string Folder = "TruckDragonEggDelivery";
        public const string Scene = "Assets/TruckDragonEggDelivery/Scenes/TruckDragonEggDelivery.unity";
        [MenuItem("Truck Sports/Dragon Kingdom/Create NEW Egg Delivery")]
        public static void Create()
        {
            var match = DragonKingdomModeAuthoring.Begin<DragonEggDeliveryMatch>(Folder, 2, new[] { "dragon-inferno", "dragon-inferno" });
            Install(match); DragonKingdomModeAuthoring.Finish(match, Folder);
            for (int i = 0; i < 4; i++)
                if (match.trucks[i].appearanceHost != null)
                {
                    var host = match.trucks[i].appearanceHost;
                    host.protectedRoots = (host.protectedRoots ?? Array.Empty<Transform>()).Append(match.carriers[i].equipment).Distinct().ToArray();
                    EditorUtility.SetDirty(host);
                }
            EditorSceneManager.SaveScene(match.gameObject.scene); AssetDatabase.SaveAssets();
        }

        public static void Install(DragonEggDeliveryMatch match)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || match == null || match.world == null || match.trucks.Length != 4)
                throw new InvalidOperationException("Stopped compiled Editor and four complete shared-world NWH trucks required.");
            if (match.carriers.Length != 0) throw new InvalidOperationException("Preserve existing Egg Delivery equipment; use an explicit scoped repair.");
            foreach (string sub in new[] { "Models", "Materials", "Animations", "Scenes" }) Directory.CreateDirectory("Assets/" + Folder + "/" + sub);
            AssetDatabase.Refresh();
            var eggMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TruckEggSpoonRace/Models/Original tapered giant egg.asset");
            var bowlMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TruckEggSpoonRace/Models/Physical concave cutlery bowl.asset");
            var chipMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TruckEggSpoonRace/Models/Original curved shell chip.asset");
            if (eggMesh == null || bowlMesh == null || chipMesh == null) throw new InvalidOperationException("Reuse the delivered original Egg & Spoon egg/cradle/chip geometry.");
            if (!match.species.TryGet("dragon-spark", out var baby) || !baby.HasAnimation(DragonAnimationAction.Hatch)) throw new InvalidOperationException("Actual Spark baby rig/Hatch mapping required.");
            var babyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckDragonKingdom/Prefabs/Dragons/dragon-spark/dragon-spark Dragon Animal.prefab");
            if (babyPrefab == null) throw new InvalidOperationException("Configured actual Generic Spark/URP prefab is required.");
            var controller = BabyController(baby);
            var shell = ImpossibleBalanceArt.Material(Folder, "Fire brood shell ivory and ember", new Color(.97f, .66f, .28f), .42f);
            var wood = ImpossibleBalanceArt.Material(Folder, "Royal protective carrier timber", new Color(.3f, .14f, .055f), .25f);
            var padded = ImpossibleBalanceArt.Material(Folder, "Cushioned royal brood lining", new Color(.34f, .1f, .13f), .35f);
            var metal = ImpossibleBalanceArt.Material(Folder, "Bronze brood delivery equipment", new Color(.65f, .38f, .085f), .55f, .6f);
            const string crackMaterialPath = "Assets/" + Folder + "/Materials/Dark visible shell cracks.mat";
            var dark = AssetDatabase.LoadAssetAtPath<Material>(crackMaterialPath);
            if (dark == null)
            { dark = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Dark visible shell cracks" }; dark.SetColor("_BaseColor", new Color(.065f, .025f, .015f)); AssetDatabase.CreateAsset(dark, crackMaterialPath); }
            var physical = SportsEventAuthoring.Contact(Folder, "Physical fragile cargo and padded cradle", .55f, .08f);
            var root = new GameObject("Dragon Egg Delivery physical cargo and nest equipment").transform;
            match.carriers = new DragonEggCarrier[4]; match.nests = new DragonEggNest[4];
            var nestRegion = match.world.Region("nests");
            if (nestRegion.nests.Length < 4 || nestRegion.truckStarts.Length < 4 || nestRegion.perches.Length < match.dragons.Length) throw new InvalidOperationException("Four actual shared kingdom nests and safe loading aprons required.");
            for (int slot = 0; slot < 4; slot++)
            {
                var nest = BuildNest(root, nestRegion.nests[slot], match.trucks[slot], match.world.definition.graph, slot, bowlMesh, padded, physical);
                match.nests[slot] = nest;
                var carrier = BuildCarrier(match, root, slot, bowlMesh, eggMesh, chipMesh, wood, padded, metal, shell, dark, physical, babyPrefab, controller);
                match.carriers[slot] = carrier;
            }
            match.motherPerches = nestRegion.perches.Take(match.dragons.Length).ToArray();
            for (int i = 0; i < match.dragons.Length; i++)
                match.dragons[i].transform.position = match.world.Region(i == 0 ? "forest" : "nests").root.position + new Vector3(20, 75, -20);
            EditorUtility.SetDirty(match); AssetDatabase.SaveAssets();
        }

        static DragonEggNest BuildNest(Transform parent, Transform actualNest, SportsEventTruck truck, LwsRoadGraph graph, int slot, Mesh bowl, Material padded, PhysicsMaterial physical)
        {
            var root = new GameObject("Correct physical nest " + (slot + 1)).transform; root.SetParent(parent, false);
            // The shared marker is a child of height-fitted licensed art. Its
            // local +Y is scaled (15.5m on the actual bird nest), so it cannot
            // define a cargo support floor in world metres. Seat the retained
            // physical lining on the real nest/ledge surface instead.
            root.position = ActualNestFloor(actualNest,slot);
            var liner = new GameObject("Protective concave nest lining"); liner.transform.SetParent(root, false);
            liner.AddComponent<MeshFilter>().sharedMesh = bowl; liner.AddComponent<MeshRenderer>().sharedMaterial = padded;
            liner.transform.localScale = Vector3.one * .75f; liner.transform.localPosition = -new Vector3(0, 0, 5) * .75f;
            var support = liner.AddComponent<MeshCollider>(); support.sharedMesh = bowl; support.sharedMaterial = physical;
            Physics.SyncTransforms();
            if (!support.Raycast(new Ray(root.position + Vector3.up * 15, Vector3.down), out var hit, 30) || hit.normal.y < .5f)
                throw new InvalidOperationException("The actual concave nest lining must provide real upward physical support.");
            var center = new GameObject("Actual egg or baby resting center").transform; center.SetParent(root, true); center.position = hit.point + Vector3.up * .9f;
            var apron=LoadingApproach(root,center.position,truck,graph,slot);
            var zoneRoot = new GameObject("Correct nest secured volume"); zoneRoot.transform.SetParent(root, true); zoneRoot.transform.position = center.position + Vector3.up * .8f;
            var zone = zoneRoot.AddComponent<BoxCollider>(); zone.isTrigger = true; zone.size = new Vector3(4.8f, 4.8f, 4.8f);
            var marker = SportsEventAuthoring.Label("Nest identity " + (slot + 1), "NEST " + (slot + 1), center.position + Vector3.up * 5, .35f, new Color(1, .85f, .35f));
            marker.transform.SetParent(root, true); marker.gameObject.AddComponent<SportsPerCameraBillboard>();
            var supports = actualNest.parent.GetComponentsInChildren<Collider>(true).Where(c => !c.isTrigger).Append(support).ToArray();
            return new DragonEggNest { id = "royal-fire-nest-" + slot, center = center, approach = apron, zone = zone, supports = supports };
        }

        static Vector3 ActualNestFloor(Transform marker,int slot)
        {
            if(marker==null||marker.parent==null)throw new InvalidOperationException("Actual shared nest art/marker required.");
            var art=marker.parent.GetComponentsInChildren<Renderer>(true).Where(renderer=>renderer.enabled&&renderer.gameObject.activeInHierarchy&&!(renderer is ParticleSystemRenderer)).ToArray();
            if(art.Length==0)throw new InvalidOperationException("Real nest renderer bounds required; the marker alone is no physical floor.");
            var bounds=art[0].bounds;foreach(var renderer in art.Skip(1))bounds.Encapsulate(renderer.bounds);
            Vector3 origin=new Vector3(bounds.center.x,bounds.max.y+.5f,bounds.center.z);Physics.SyncTransforms();
            var source=marker.parent.GetComponentsInChildren<Collider>(true).Where(collider=>collider.enabled&&!collider.isTrigger&&collider.gameObject.activeInHierarchy).ToArray();
            var direct=new List<RaycastHit>();foreach(var collider in source)if(collider.Raycast(new Ray(origin,Vector3.down),out var hit,bounds.size.y+2)&&hit.normal.y>.65f)direct.Add(hit);
            // Prefer actual nest mesh contact. A hollow authored nest may have
            // no central twig; its existing supporting cliff/terrain is then
            // the valid foundation for the real concave protective lining.
            if(direct.Count==0)direct.AddRange(Physics.RaycastAll(origin,Vector3.down,bounds.size.y+2,~0,QueryTriggerInteraction.Ignore)
                .Where(hit=>hit.normal.y>.7f&&hit.point.y<=bounds.min.y+.25f&&hit.point.y>=bounds.min.y-1.5f&&hit.collider.GetComponentInParent<SportsEventTruck>()==null));
            if(direct.Count==0)throw new InvalidOperationException("Actual nest "+(slot+1)+" has no real upward central nest/ledge support inside its authored geometry bounds "+bounds+"; preserve the world and repair physical art.");
            var support=direct.OrderByDescending(hit=>hit.point.y).First();
            Debug.Log("EGG_ACTUAL_NEST_FLOOR slot="+slot+" marker="+marker.position+" markerLossyScale="+marker.lossyScale+" artBounds="+bounds+" physicalSupport="+support.collider.name+" point="+support.point+" normal="+support.normal+". Shared marker/art/terrain unchanged; mode lining follows actual geometry.");
            return support.point+Vector3.up*.03f;
        }

        [Serializable] sealed class BayEvidence
        {public string scope="Actual existing support/full NWH clearance and <=25m three-dimensional reach; no long crane or new support surface.",support,status;public int slot;public Vector3 nestCenter,position;public Quaternion rotation;public float bodyOriginReach;public string[] rejected;}
        static void WriteBayEvidence(BayEvidence evidence)
        {
            string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1>=args.Length)return;
            string folder=Path.Combine(Path.GetFullPath(args[at+1]),"EggLoadingBays");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"nest-"+evidence.slot+".json"),JsonUtility.ToJson(evidence,true));
        }

        static Transform LoadingApproach(Transform parent,Vector3 nestCenter,SportsEventTruck truck,LwsRoadGraph graph,int slot)
        {
            // Region starting grids are unrelated to nest loading bays. Derive a
            // reachable real support pose near this specific existing nest;
            // never extend the hoist across the region to its generic start.
            Physics.SyncTransforms();float lift=.12f;var rejected=new List<string>();
            foreach(var wheel in truck.wheels)lift=Mathf.Max(lift,.12f-(truck.transform.InverseTransformPoint(wheel.transform.position).y-wheel.wheel.radius-wheel.spring.maxLength));
            foreach(float radius in new[]{18f,21f,23f})for(int candidate=0;candidate<16;candidate++)
            {
                float angle=(candidate+slot*4)*Mathf.PI/8;
                Vector3 proposed=nestCenter+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                var hits=Physics.RaycastAll(proposed+Vector3.up*50,Vector3.down,110,~0,QueryTriggerInteraction.Ignore)
                    .Where(value=>value.normal.y>.8f&&Mathf.Abs(value.point.y-nestCenter.y)<8&&!value.transform.IsChildOf(truck.transform))
                    .OrderBy(value=>Mathf.Abs(value.point.y-nestCenter.y)).ToArray();
                if(hits.Length==0){rejected.Add("radius="+radius+" candidate="+candidate+" proposed="+proposed+" no actual upward support within8verticalm of real nest center");continue;}var support=hits[0];Vector3 point=support.point;
                if(!LwsRoadGraphQuery.TryFindNearestRoad(graph,point,180,out _)){rejected.Add("radius="+radius+" candidate="+candidate+" support="+support.collider.name+" point="+point+" no actual nearby graph road");continue;}
                Vector3 heading=Vector3.ProjectOnPlane(nestCenter-point,support.normal).normalized;
                var rotation=Quaternion.LookRotation(heading,support.normal);Vector3 origin=point+support.normal*lift;
                if(Vector3.Distance(origin,nestCenter)>DragonEggCarrier.MaximumHoistReach-1){rejected.Add("radius="+radius+" candidate="+candidate+" actual 3D reach="+Vector3.Distance(origin,nestCenter)+" exceeds bounded25m body-origin margin");continue;}
                Vector3 size=Vector3.Scale(truck.shape.size*.5f,truck.transform.lossyScale)+new Vector3(.45f,.1f,.6f);
                Vector3 bodyCenter=origin+rotation*Vector3.Scale(truck.shape.center,truck.transform.lossyScale);
                var overlaps=Physics.OverlapBox(bodyCenter,size,rotation,~0,QueryTriggerInteraction.Ignore);
                var blocking=overlaps.Where(value=>value!=support.collider&&!value.transform.IsChildOf(truck.transform)).ToArray();if(blocking.Length>0){rejected.Add("radius="+radius+" candidate="+candidate+" body clearance blockers=["+string.Join(";",blocking.Select(value=>value.name+" "+value.bounds))+"]");continue;}
                bool wheelsSupported=true;
                foreach(var wheel in truck.wheels)
                {
                    Vector3 local=truck.transform.InverseTransformPoint(wheel.transform.position);
                    Vector3 wheelPoint=origin+rotation*Vector3.Scale(local,truck.transform.lossyScale);
                    bool found=Physics.RaycastAll(wheelPoint+Vector3.up*3,Vector3.down,8,~0,QueryTriggerInteraction.Ignore)
                        .Any(value=>value.normal.y>.7f&&Mathf.Abs(value.point.y-point.y)<1.5f&&!value.transform.IsChildOf(truck.transform));
                    if(!found){wheelsSupported=false;break;}
                }
                if(!wheelsSupported){rejected.Add("radius="+radius+" candidate="+candidate+" incomplete actual wheel support at="+point);continue;}
                var result=new GameObject("Nest "+(slot+1)+" actual bounded hoist loading approach").transform;result.SetParent(parent,true);result.SetPositionAndRotation(point,rotation);
                WriteBayEvidence(new BayEvidence{slot=slot,status="ACCEPTED",nestCenter=nestCenter,position=point,rotation=rotation,support=support.collider.name,bodyOriginReach=Vector3.Distance(origin,nestCenter),rejected=rejected.ToArray()});return result;
            }
            WriteBayEvidence(new BayEvidence{slot=slot,status="REJECTED_ALL",nestCenter=nestCenter,rejected=rejected.ToArray()});
            throw new InvalidOperationException("No actual supported NWH loading bay within bounded26m hoist reach of nest "+(slot+1)+" at "+nestCenter+". Repair the physical authored apron; a long-distance crane substitute is not accepted.");
        }

        static DragonEggCarrier BuildCarrier(DragonEggDeliveryMatch match, Transform root, int slot, Mesh bowl, Mesh egg, Mesh chip,
            Material wood, Material padded, Material metal, Material shell, Material dark, PhysicsMaterial physical, GameObject babyPrefab, RuntimeAnimatorController controller)
        {
            var truck = match.trucks[slot];
            var equipment = new GameObject("Temporary protected brood equipment · owner " + slot).transform;
            equipment.SetParent(truck.transform, false);
            equipment.localPosition = new Vector3(truck.shape.center.x, truck.shape.center.y + truck.shape.size.y * .5f + .22f, truck.shape.center.z - truck.shape.size.z * .2f);
            var bowlVisual = new GameObject("Original concave protective cargo cradle"); bowlVisual.transform.SetParent(equipment, false);
            bowlVisual.AddComponent<MeshFilter>().sharedMesh = bowl; bowlVisual.AddComponent<MeshRenderer>().sharedMaterial = padded;
            bowlVisual.transform.localScale = Vector3.one * .48f; bowlVisual.transform.localPosition = new Vector3(0, 0, -5 * .48f);
            Box(equipment, "Physical cushion floor", Vector3.down * .08f, new Vector3(1.9f, .16f, 1.9f), padded, physical);
            for (int side = -1; side <= 1; side += 2)
            {
                Box(equipment, "Protective cradle side", new Vector3(side * 1.02f, .35f, 0), new Vector3(.16f, .72f, 2.1f), wood, physical);
                Box(equipment, "Protective cradle end", new Vector3(0, .35f, side * 1.02f), new Vector3(2.1f, .72f, .16f), wood, physical);
            }
            var socket = new GameObject("Brood current physical carry center").transform; socket.SetParent(equipment, false); socket.localPosition = Vector3.up * .9f;
            var cargoRoot = new GameObject("Physical fire egg identity owner " + slot); cargoRoot.transform.SetParent(root, true); cargoRoot.transform.position = socket.position;
            var body = cargoRoot.AddComponent<Rigidbody>(); body.mass = 25; body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; body.solverIterations = 16; body.maxDepenetrationVelocity = 3; body.isKinematic = true; body.detectCollisions = false;
            var eggShape = cargoRoot.AddComponent<CapsuleCollider>(); eggShape.radius = .65f; eggShape.height = 1.7f; eggShape.sharedMaterial = physical;
            var babyShape = cargoRoot.AddComponent<CapsuleCollider>(); babyShape.radius = .55f; babyShape.height = 1.5f; babyShape.sharedMaterial = physical; babyShape.enabled = false;
            var cargo = cargoRoot.AddComponent<DragonEggCargo>(); cargo.match = match; cargo.slot = slot; cargo.body = body; cargo.eggShape = eggShape; cargo.babyShape = babyShape;
            var shellRoot = new GameObject("Actual retained egg mesh"); shellRoot.transform.SetParent(cargoRoot.transform, false);
            shellRoot.AddComponent<MeshFilter>().sharedMesh = egg; var shellRenderer = shellRoot.AddComponent<MeshRenderer>(); shellRenderer.sharedMaterial = shell;
            shellRoot.transform.localScale = new Vector3(1.3f / egg.bounds.size.x, 1.7f / egg.bounds.size.y, 1.3f / egg.bounds.size.z);
            shellRoot.transform.localPosition = -Vector3.Scale(egg.bounds.center, shellRoot.transform.localScale); cargo.shellRenderers = new Renderer[] { shellRenderer };
            cargo.cracks = new LineRenderer[3];
            for (int seam = 0; seam < 3; seam++)
            {
                var line = new GameObject("Physical shell crack " + seam).AddComponent<LineRenderer>(); line.transform.SetParent(cargoRoot.transform, false);
                line.useWorldSpace = false; line.sharedMaterial = dark; line.startWidth = line.endWidth = .018f; line.positionCount = 9;
                for (int p = 0; p < 9; p++)
                {
                    float y = -.65f + p * .16f, radius = Mathf.Sqrt(Mathf.Max(.05f, 1 - y * y / (.85f * .85f))) * .662f;
                    float angle = seam * Mathf.PI * 2 / 3 + (p % 2 == 0 ? -.06f : .06f);
                    line.SetPosition(p, new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius));
                }
                line.enabled = false; cargo.cracks[seam] = line;
            }
            var baby = (GameObject)PrefabUtility.InstantiatePrefab(babyPrefab, cargoRoot.transform);
            PrefabUtility.UnpackPrefabInstance(baby, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var script in baby.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(script);
            foreach (var collider in baby.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
            foreach (var rigidbody in baby.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(rigidbody);
            var animator = baby.GetComponentInChildren<Animator>(true); animator.runtimeAnimatorController = controller; animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; animator.keepAnimatorStateOnDisable = true;
            var bounds = Bounds(baby); baby.transform.localScale *= 1.45f / Mathf.Max(.01f, bounds.size.y); bounds = Bounds(baby); baby.transform.position += cargoRoot.transform.position - bounds.center;
            cargo.baby = baby; cargo.babyAnimator = animator; baby.SetActive(false);
            cargo.hatchLight = new GameObject("Warm non-graphic hatch glow").AddComponent<Light>(); cargo.hatchLight.transform.SetParent(cargoRoot.transform, false); cargo.hatchLight.color = new Color(1, .6f, .12f); cargo.hatchLight.range = 5; cargo.hatchLight.enabled = false;
            cargo.shellChips = new Rigidbody[8];
            for (int i = 0; i < 8; i++)
            {
                var shard = new GameObject("Pooled curved shell fragment " + slot + ":" + i); shard.transform.SetParent(root, false); shard.transform.localScale = Vector3.one * .4f;
                shard.AddComponent<MeshFilter>().sharedMesh = chip; shard.AddComponent<MeshRenderer>().sharedMaterial = shell; var shape = shard.AddComponent<SphereCollider>(); shape.radius = .2f;
                var rb = shard.AddComponent<Rigidbody>(); rb.mass = .12f; rb.isKinematic = true; shard.SetActive(false); cargo.shellChips[i] = rb;
            }
            var craneRoot = new GameObject("Existing Jenga physical brood hoist " + slot).transform; craneRoot.SetParent(equipment, false);
            var crane = craneRoot.gameObject.AddComponent<JengaCrane>(); crane.travelSpeed = 10; crane.liftSpeed = 4; crane.ceiling = 450; crane.parkHeight = socket.position.y + 4;
            var head = new GameObject("Actual physical hoist spreader"); head.transform.SetParent(craneRoot, false); head.transform.position = socket.position + Vector3.up * 4;
            crane.spreader = head.AddComponent<Rigidbody>(); crane.spreader.isKinematic = true;
            Box(head.transform, "Bronze hoist pickup plate", Vector3.zero, new Vector3(.35f, .12f, .35f), metal, null);
            var cable = new GameObject("Brood hoist cable").AddComponent<LineRenderer>(); cable.transform.SetParent(craneRoot, false); cable.sharedMaterial = metal; cable.startWidth = cable.endWidth = .06f; cable.positionCount = 2;
            var boom = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform; boom.name = "Brood loading boom"; boom.SetParent(craneRoot, false); UnityEngine.Object.DestroyImmediate(boom.GetComponent<Collider>()); boom.GetComponent<Renderer>().sharedMaterial = metal;
            return new DragonEggCarrier { truck = truck, equipment = equipment, socket = socket, cargo = cargo, crane = crane, cable = cable, boom = boom };
        }
        static AnimatorController BabyController(DragonSpeciesDefinition species)
        {
            const string path = "Assets/TruckDragonEggDelivery/Animations/Actual Spark hatch and care.controller";
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path); if (existing != null) { ValidateBabyController(existing,species); return existing; }
            var controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            PopulateBabyController(controller,species);SaveBabyController(controller);ValidateBabyController(controller,species);return controller;
        }

        // V82 failed composition retained the freshly created but unsaved empty
        // controller. V85 reused it. This explicit migration repairs only that
        // exact empty owned artifact, preserving its GUID and all scene refs.
        public static void RepairEmptyBabyController()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required for selected baby controller repair.");
            const string path="Assets/TruckDragonEggDelivery/Animations/Actual Spark hatch and care.controller";
            var catalog=AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>(DragonSpeciesAuthoring.CatalogPath);if(catalog==null||!catalog.TryGet("dragon-spark",out var species))throw new InvalidOperationException("Actual retained Spark catalog required.");
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);if(controller==null)throw new InvalidOperationException("Retain the exact existing empty controller before explicit repair.");
            string guid=AssetDatabase.AssetPathToGUID(path);byte[] sceneBytes=File.Exists(Scene)?File.ReadAllBytes(Scene):null;
            if(controller.layers.Length==0&&controller.parameters.Length==0)
            {
                if(AssetDatabase.LoadAllAssetsAtPath(path).Any(value=>value!=controller))throw new InvalidOperationException("Partial/custom controller subassets require reviewed repair; only the retained completely empty artifact is authorized here.");
                PopulateBabyController(controller,species);SaveBabyController(controller);ValidateBabyController(controller,species);
                Debug.Log("EGG_EMPTY_BABY_CONTROLLER_REPAIRED guid="+AssetDatabase.AssetPathToGUID(path)+" actualSparkHatchAndCare=true sceneReferencesPreserved=true");
            }
            else ValidateBabyController(controller,species); // valid designers retained byte-for-byte
            if(AssetDatabase.AssetPathToGUID(path)!=guid||sceneBytes!=null&&!sceneBytes.SequenceEqual(File.ReadAllBytes(Scene)))throw new InvalidOperationException("Selected controller repair must preserve its existing GUID and saved scene bytes.");
        }
        static void PopulateBabyController(AnimatorController controller,DragonSpeciesDefinition species)
        {
            if (!species.TryGetAnimation(DragonAnimationAction.Hatch, out var hatch) || !species.TryGetAnimation(DragonAnimationAction.GroundIdle, out var idle) || !species.TryGetAnimation(DragonAnimationAction.Walk, out var walk))
                throw new InvalidOperationException("Actual baby hatch, idle and walk clips required.");
            if(controller.layers.Length==0){var baseMachine=new AnimatorStateMachine{name="Base Layer"};AssetDatabase.AddObjectToAsset(baseMachine,controller);controller.layers=new[]{new AnimatorControllerLayer{name="Base Layer",defaultWeight=1,stateMachine=baseMachine}};}
            if(controller.layers.Length!=1||controller.layers[0].stateMachine.states.Length!=0||controller.parameters.Length!=0)throw new InvalidOperationException("Populate only a new/retained empty baby controller.");
            controller.AddParameter("Activity", AnimatorControllerParameterType.Float);
            var machine = controller.layers[0].stateMachine; var born = machine.AddState("Hatch"); born.motion = hatch.clip; born.speed = hatch.playbackSpeed;
            var tree = new BlendTree { name = "Actual baby comfort and walking", blendType = BlendTreeType.Simple1D, blendParameter = "Activity", useAutomaticThresholds = false };
            tree.AddChild(idle.clip, 0); tree.AddChild(walk.clip, 1); AssetDatabase.AddObjectToAsset(tree, controller);
            var care = machine.AddState("Care"); care.motion = tree;
            var transition = born.AddTransition(care); transition.hasExitTime = true; transition.exitTime = .97f; transition.duration = .08f;
            machine.defaultState = born;EditorUtility.SetDirty(machine);EditorUtility.SetDirty(born);EditorUtility.SetDirty(care);EditorUtility.SetDirty(tree);EditorUtility.SetDirty(transition);EditorUtility.SetDirty(controller);
        }
        static void SaveBabyController(AnimatorController controller)
        {foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(controller)))if(asset!=null)EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(controller);}
        static void ValidateBabyController(AnimatorController controller,DragonSpeciesDefinition species)
        {
            if(controller.layers.Length==0||controller.layers[0].stateMachine==null)throw new InvalidOperationException("Owned baby AnimatorController is empty and cannot animate; explicitly run RepairEmptyBabyController before scene acceptance.");
            var states=controller.layers[0].stateMachine.states.Select(value=>value.state).ToArray();var hatch=states.SingleOrDefault(value=>value.name=="Hatch");var care=states.SingleOrDefault(value=>value.name=="Care");
            if(!species.TryGetAnimation(DragonAnimationAction.Hatch,out var source)||hatch==null||care==null||hatch.motion!=source.clip||!hatch.transitions.Any(value=>value.destinationState==care&&value.hasExitTime&&value.exitTime>=.9f)||!controller.parameters.Any(value=>value.name=="Activity"&&value.type==AnimatorControllerParameterType.Float)||!(care.motion is BlendTree))throw new InvalidOperationException("Retain actual Spark Hatch/Care clips, transition and Activity blend controller; custom/partial controller requires explicit reviewed repair.");
        }
        static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material, PhysicsMaterial physical)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (physical == null) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); else go.GetComponent<Collider>().sharedMaterial = physical;
            return go;
        }
        static Bounds Bounds(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true); if (renderers.Length == 0) throw new InvalidOperationException("Actual baby renderers required.");
            var result = renderers[0].bounds; foreach (var renderer in renderers) result.Encapsulate(renderer.bounds); return result;
        }
    }
}
#endif
