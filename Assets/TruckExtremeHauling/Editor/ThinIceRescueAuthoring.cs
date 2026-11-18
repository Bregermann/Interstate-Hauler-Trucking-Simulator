using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LWS.TruckHitchSports.Editor;
using LWS.TruckSports;
using LWS.TruckSports.Editor;

namespace LWS.TruckExtremeHauling.Editor
{
    public static class ThinIceRescueAuthoring
    {
        const string Folder = "TruckExtremeHauling";
        [MenuItem("Truck Sports/Alpha 0.10/Install active Thin Ice convoy rescue")]
        public static void InstallActiveConvoyRescue() => UpgradeActiveScene();

        public static ThinIceRescueObjectives UpgradeActiveScene()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before authoring the rescue incident.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/TruckExtremeHauling/Scenes/IceRoadReckoning.unity")
                throw new InvalidOperationException("Open the existing saved IceRoadReckoning scene.");
            var match = UnityEngine.Object.FindAnyObjectByType<ThinIceMatch>();
            if (match == null || match.gameObject.scene != scene || match.ice == null || match.ice.Length == 0)
                throw new InvalidOperationException("Preserve the original load-sensitive lake and match.");
            if (match.trucks == null || match.trucks.Length != 4 || match.trailers == null || match.trailers.Length != 4)throw new InvalidOperationException("The original four-slot recovery rig configuration is required.");
            if (match.rescue != null) { RepairFlotation(match.rescue); return match.rescue; }
            var contact = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/TruckExtremeHauling/Materials/Freight concrete.asset");
            var ground = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckExtremeHauling/Materials/Quarry and depot asphalt.mat");
            var steel = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckExtremeHauling/Materials/Freight tower gunmetal.mat");
            var safety = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckExtremeHauling/Materials/Freight safety orange.mat");
            var water = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckWaterSports/Materials/BlackwaterMatch river surface.mat");
            if (contact == null || ground == null || steel == null || safety == null || water == null)
                throw new InvalidOperationException("Existing freight contact/materials and owned River Modeler water material are required.");

            var rescue = match.gameObject.AddComponent<ThinIceRescueObjectives>(); rescue.match = match;
            var equipment = new GameObject("Convoy Rescue · original stranded cargo and northern recovery station").transform;
            equipment.SetParent(match.transform, false); rescue.equipment = equipment;
            rescue.incidentIce = match.ice.OrderBy(tile => Vector3.SqrMagnitude(tile.transform.position - new Vector3(105,0,70))).First();
            var cargo = HitchAuthoring.CreateTrailer(rescue.incidentIce.transform.position + Vector3.up * 1.2f, Quaternion.identity, "Original NPC freight trailer · real ice breakthrough rescue");
            cargo.transform.SetParent(equipment, true); rescue.strandedCargo = cargo;
            RepairFlotation(rescue);
            var first = match.ice[0].surface.bounds;
            foreach (var tile in match.ice) first.Encapsulate(tile.surface.bounds);
            rescue.waterBounds = new Bounds(first.center, new Vector3(first.size.x,100,first.size.z));
            rescue.cableMaterial = safety;

            var shore = new GameObject("Northern shore physical convoy rescue destination").transform;
            shore.SetParent(equipment, false); shore.position = new Vector3(105,.35f,340); rescue.shore = shore;
            SportsEventAuthoring.Box("Supported northern rescue station apron", new Vector3(105,-.65f,335), new Vector3(350,2,90), ground, contact, equipment);
            // An actual shallow ramp prevents a buoyant trailer meeting an
            // impossible vertical ice edge. Native wheels/contacts do the lift.
            var ramp = SportsEventAuthoring.Box("Physical submerged extraction ramp", equipment.position, Vector3.one, steel, contact, equipment);
            PositionExtractionRamp(rescue, ramp.transform);
            var waterPatch = GameObject.CreatePrimitive(PrimitiveType.Quad); waterPatch.name = "Actual lake water below fractured ice sheets";
            waterPatch.transform.SetParent(equipment, false); waterPatch.transform.position = new Vector3(rescue.waterBounds.center.x,-.55f,rescue.waterBounds.center.z);
            waterPatch.transform.rotation = Quaternion.Euler(90,0,0); waterPatch.transform.localScale = new Vector3(rescue.waterBounds.size.x,rescue.waterBounds.size.z,1);
            waterPatch.GetComponent<Renderer>().sharedMaterial = water; UnityEngine.Object.DestroyImmediate(waterPatch.GetComponent<Collider>());
            foreach (int side in new[] { -1,1 })
            {
                OwnedProp(equipment, "Assets/ShipContainerPack/URP/Prefabs/ShipContainerGray_URP.prefab", new Vector3(105+side*36,.35f,365), new Vector3(12,5,6));
                OwnedProp(equipment, "Assets/RoadPropsPack/URP/Prefabs/RoadBlock02_URP.prefab", new Vector3(105+side*12,.35f,323), new Vector3(3,1.3f,.7f));
                var beacon = new GameObject("Recovery shore amber beacon").AddComponent<Light>(); beacon.transform.SetParent(equipment,false);
                beacon.transform.position = new Vector3(105+side*12,3,323); beacon.type = LightType.Point; beacon.color = new Color(1,.45f,.08f); beacon.range = 22; beacon.intensity = 4;
            }
            equipment.gameObject.SetActive(false); match.rescue = rescue;
            EditorUtility.SetDirty(match); EditorUtility.SetDirty(rescue); EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Convoy Rescue authored: original NWH NPC trailer, actual load-stressed incident ice, Archimeds recovery buoyancy, extraction ramp and supported shore. Options0–2 geometry stays inactive. Save explicitly; runtime acceptance is pending.");
            return rescue;
        }

        [MenuItem("Truck Sports/Alpha 0.10/Align Thin Ice extraction ramp with ice edge")]
        public static void RepairActiveExtractionRamp()
        {
            var scene = SceneManager.GetActiveScene();
            const string scenePath = "Assets/TruckExtremeHauling/Scenes/IceRoadReckoning.unity";
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != scenePath)
                throw new InvalidOperationException("Open the saved Ice Road Reckoning scene outside Play Mode.");

            var match = UnityEngine.Object.FindAnyObjectByType<ThinIceMatch>();
            var rescue = match != null ? match.rescue : null;
            var ramp = rescue != null && rescue.equipment != null
                ? rescue.equipment.Find("Physical submerged extraction ramp") : null;
            if (ramp == null || !ramp.TryGetComponent<BoxCollider>(out _))
                throw new InvalidOperationException("The original rescue ramp is required.");

            Undo.RecordObject(ramp, "Align rescue ramp with ice edge");
            PositionExtractionRamp(rescue, ramp);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        [MenuItem("Truck Sports/Alpha 0.10/Install active Thin Ice physical recovery gantry")]
        public static void InstallActiveFrontLift() => UpgradeActiveRecoveryHoist();

        // Isolated batch entry: opens/saves exactly this existing venue. It does
        // not invoke expansion/global authoring or rebuild any original scene.
        public static void AuthorSavedRecoveryHoist()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before isolated recovery authoring.");
            const string path="Assets/TruckExtremeHauling/Scenes/IceRoadReckoning.unity";
            if(!File.Exists(path))throw new FileNotFoundException("Preserve the original saved Ice Road venue.",path);
            var priorMaterials=new HashSet<string>(AssetDatabase.FindAssets("t:Material",new[]{"Assets/TruckExtremeHauling/Materials"}).Select(id=>AssetDatabase.GUIDToAssetPath(id)));
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
            var hoist=UpgradeActiveRecoveryHoist();EditorUtility.SetDirty(hoist);EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Could not save the isolated Thin Ice recovery venue.");
            AssetDatabase.SaveAssets();
            Debug.Log("THIN_ICE_RECOVERY_AUTHOR_SAVED scene="+path+"; actual fixture references wired; gameplay acceptance remains unverified.");
            foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/TruckExtremeHauling/Materials"})){
                string material=AssetDatabase.GUIDToAssetPath(guid);if(!priorMaterials.Contains(material))Debug.Log("THIN_ICE_RECOVERY_NEW_OWNED_MATERIAL "+material);
            }
        }

        // Explicit additive upgrade; the lake/ramp/load/spawns/thresholds are
        // unchanged and no runtime code generates or relocates this equipment.
        public static ThinIceRecoveryHoist UpgradeActiveRecoveryHoist()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/TruckExtremeHauling/Scenes/IceRoadReckoning.unity")
                throw new InvalidOperationException("Open the saved Thin Ice rescue venue outside Play Mode.");
            var match=UnityEngine.Object.FindAnyObjectByType<ThinIceMatch>();var rescue=match!=null?match.rescue:null;
            if(rescue==null||rescue.strandedCargo==null||rescue.cargoFloat==null||rescue.equipment==null||rescue.incidentIce==null)
                throw new InvalidOperationException("Preserve the existing original NPC rescue load, exact displacement and ramp.");
            if(rescue.frontLift!=null)return rescue.frontLift;
            const string reelPath="Assets/ConstructionPropsBundle/URP/Prefabs/SM_CableReel01 Variant.prefab";
            const string floorPath="Assets/LeartesStudios/Garage Warehouse Environment/Art/Prefabs/SM_Floor_4x6m_01.prefab";
            var crane=Required<Mesh>("Assets/TruckBungeeDemolition/Models/Lattice Crane.asset");
            var hookMesh=Required<Mesh>("Assets/TruckExtremeHauling/Models/Physical freight hook.asset");
            var eyeMesh=Required<Mesh>("Assets/TruckExtremeHauling/Models/Physical freight lifting eye.asset");
            Required<GameObject>(reelPath);Required<GameObject>(floorPath);
            var steel=Required<Material>("Assets/TruckExtremeHauling/Materials/Freight tower gunmetal.mat");
            var paint=Required<Material>("Assets/TruckExtremeHauling/Materials/Freight safety orange.mat");
            var contact=Required<PhysicsMaterial>("Assets/TruckExtremeHauling/Materials/Freight concrete.asset");
            foreach(var truck in match.trucks)if(truck.GetComponent<LWS.TruckHitchSports.HitchCoupler>()?.Anchor==null)
                throw new InvalidOperationException("Every participating native tractor must retain its original fifth wheel.");
            var root=new GameObject("Convoy Rescue · real roof-sling front-support gantry").transform;root.SetParent(rescue.equipment,false);
            ThinIceRecoveryHoist owner=null;Transform eye=null;
            try{
                float x=rescue.incidentIce.transform.position.x,z=rescue.incidentIce.transform.position.z+5;
                foreach(int side in new[]{-1,1}){
                    var footing=HoistChild(root,"Owned fixed recovery gantry footing "+side,new Vector3(x+side*18,-.15f,z));
                    LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(footing,floorPath,new Vector3(8,1,8),-.5f,true,Folder);
                    var footCollider=footing.gameObject.AddComponent<BoxCollider>();footCollider.size=new Vector3(8,1,8);footCollider.sharedMaterial=contact;
                    var craneRoot=HoistChild(root,"Existing demolition lattice recovery support "+side,new Vector3(x+side*18,.35f,z));
                    var art=HoistChild(craneRoot,"Owned lattice crane mesh",craneRoot.position);
                    art.gameObject.AddComponent<MeshFilter>().sharedMesh=crane;art.gameObject.AddComponent<MeshRenderer>().sharedMaterial=steel;
                    float scale=8/Mathf.Max(.1f,crane.bounds.size.y);art.localScale=Vector3.one*scale;
                    art.localPosition=-new Vector3(crane.bounds.center.x,crane.bounds.min.y,crane.bounds.center.z)*scale;
                    foreach(var vertex in crane.vertices){Vector3 world=art.TransformPoint(vertex);if(world.y<6&&Mathf.Abs(world.x-x)<8&&world.z>50&&world.z<125)
                        throw new InvalidOperationException("The owned crane's low geometry intrudes into the real recovery driving corridor.");}
                    art.gameObject.AddComponent<MeshCollider>().sharedMesh=crane;
                }
                var beam=HoistChild(root,"Owned overhead recovery gantry crossmember",new Vector3(x,8.45f,z));
                LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(beam,floorPath,new Vector3(36,.6f,3),-.3f,true,Folder);
                var beamCollider=beam.gameObject.AddComponent<BoxCollider>();beamCollider.size=new Vector3(36,.6f,3);beamCollider.sharedMaterial=contact;
                var cart=HoistChild(root,"Fixed physical recovery winch anchor",new Vector3(x,8,z));
                var body=cart.gameObject.AddComponent<Rigidbody>();body.mass=1000;body.useGravity=false;body.isKinematic=true;body.solverIterations=20;body.solverVelocityIterations=8;
                var carrier=cart.gameObject.AddComponent<PhysicalCableCar>();carrier.trolley=body;carrier.load=rescue.strandedCargo.Body;carrier.routeEnd=cart.position+Vector3.forward*20;
                var hookRoot=HoistChild(root,"Physical original-load recovery hook",new Vector3(x,1.6f,z));hookRoot.gameObject.layer=2;
                var hookBody=hookRoot.gameObject.AddComponent<Rigidbody>();hookBody.mass=400;hookBody.isKinematic=true;hookBody.interpolation=RigidbodyInterpolation.Interpolate;
                hookBody.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;hookBody.solverIterations=20;hookBody.solverVelocityIterations=8;
                hookRoot.gameObject.AddComponent<SphereCollider>().radius=.25f;hookRoot.gameObject.AddComponent<MeshFilter>().sharedMesh=hookMesh;hookRoot.gameObject.AddComponent<MeshRenderer>().sharedMaterial=paint;
                var rope=hookRoot.gameObject.AddComponent<SpringJoint>();rope.autoConfigureConnectedAnchor=false;rope.connectedBody=body;rope.connectedAnchor=Vector3.zero;
                rope.spring=160000;rope.damper=35000;rope.maxDistance=6.4f;rope.enableCollision=false;
                var hook=hookRoot.gameObject.AddComponent<SuspendedFreightHook>();hook.body=hookBody;hook.rope=rope;hook.carrier=carrier;hook.initialLength=6.4f;hook.minimumLength=2.5f;hook.maximumLength=12;
                hook.resetPose=HoistChild(root,"Saved recovery hook pose",hookRoot.position);
                hook.line=hookRoot.gameObject.AddComponent<LineRenderer>();hook.line.sharedMaterial=steel;hook.line.widthMultiplier=.08f;carrier.cable=rope;
                var reel=HoistChild(root,"Owned physical recovery winch",cart.position);LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(reel,reelPath,new Vector3(2.6f,1.5f,2.6f),-1.5f,false,Folder);hook.winchVisual=reel;
                eye=HoistChild(rescue.strandedCargo.transform,"Actual recovery bridle lifting eye",rescue.strandedCargo.transform.position);
                eye.localPosition=new Vector3(rescue.cargoFloat.proxy.bounds.center.x,rescue.cargoFloat.proxy.bounds.max.y+.25f,rescue.cargoFloat.proxy.bounds.center.z);
                eye.gameObject.AddComponent<MeshFilter>().sharedMesh=eyeMesh;eye.gameObject.AddComponent<MeshRenderer>().sharedMaterial=paint;
                owner=root.gameObject.AddComponent<ThinIceRecoveryHoist>();owner.rescue=rescue;owner.carrier=carrier;owner.hook=hook;owner.liftingEye=eye;owner.roofHeight=rescue.cargoFloat.proxy.bounds.max.y+.25f;
                rescue.frontLift=owner;EditorUtility.SetDirty(rescue);EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("THIN_ICE_FRONT_SUPPORT_AUTHORED: same native cargo; physical roof-eye hook/hoist -> strict native fifth wheel -> unchanged shore haul. Existing public Skyhook constraints/art reused; Convoy Rescue only. Save explicitly; natural runtime acceptance pending.");return owner;
            }catch{if(eye!=null)UnityEngine.Object.DestroyImmediate(eye.gameObject);if(owner!=null)UnityEngine.Object.DestroyImmediate(owner);UnityEngine.Object.DestroyImmediate(root.gameObject);throw;}
        }
        // Explicit additive upgrade of the one saved incident. The original
        // fixed gantry remains usable when this optional rail is not installed.
        [MenuItem("Truck Sports/Alpha 0.10/Upgrade saved Thin Ice supported recovery rail")]
        public static void AuthorSavedRecoveryRail()
        {
            const string scenePath="Assets/TruckExtremeHauling/Scenes/IceRoadReckoning.unity";
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before upgrading the recovery rail.");
            var scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);
            var hoist=UpgradeActiveRecoveryRail();EditorUtility.SetDirty(hoist);EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Could not save the Thin Ice recovery rail.");
            AssetDatabase.SaveAssets();
            Debug.Log("THIN_ICE_RECOVERY_RAIL_SAVED: "+scene.path+"; supported 10m native trolley travel; original load/bridle/fifth-wheel and shore qualification unchanged.");
        }

        public static ThinIceRecoveryHoist UpgradeActiveRecoveryRail()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/TruckExtremeHauling/Scenes/IceRoadReckoning.unity")
                throw new InvalidOperationException("Open only the saved Thin Ice scene outside Play Mode.");
            var hoist=UpgradeActiveRecoveryHoist();
            if(hoist.translationRail!=null)return hoist;
            if(hoist.carrier==null||hoist.carrier.trolley==null||hoist.hook==null||hoist.carrier.trolley.GetComponent<ConfigurableJoint>()!=null)
                throw new InvalidOperationException("The original fixed recovery trolley and bridle must be intact and unconstrained.");
            const string floorPath="Assets/LeartesStudios/Garage Warehouse Environment/Art/Prefabs/SM_Floor_4x6m_01.prefab";
            var crane=Required<Mesh>("Assets/TruckBungeeDemolition/Models/Lattice Crane.asset");Required<GameObject>(floorPath);
            var steel=Required<Material>("Assets/TruckExtremeHauling/Materials/Freight tower gunmetal.mat");
            var contact=Required<PhysicsMaterial>("Assets/TruckExtremeHauling/Materials/Freight concrete.asset");
            Vector3 start=hoist.carrier.trolley.position;
            float x=hoist.rescue.incidentIce.transform.position.x,z=hoist.rescue.incidentIce.transform.position.z+5;
            if(Vector3.Distance(start,new Vector3(x,8,z))>.01f)
                throw new InvalidOperationException("Preserve the saved fixed gantry pose; this upgrade does not reposition live equipment.");
            var extension=new GameObject("Supported 10m northbound recovery trolley rail").transform;extension.SetParent(hoist.transform,false);
            ConfigurableJoint rail=null;
            try
            {
                foreach(int side in new[]{-1,1})
                {
                    var footing=HoistChild(extension,"Owned northern recovery portal footing "+side,new Vector3(x+side*18,-.15f,z+10));
                    LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(footing,floorPath,new Vector3(8,1,8),-.5f,true,Folder);
                    var footCollider=footing.gameObject.AddComponent<BoxCollider>();footCollider.size=new Vector3(8,1,8);footCollider.sharedMaterial=contact;
                    var mast=HoistChild(extension,"Owned northern lattice recovery support "+side,new Vector3(x+side*18,.35f,z+10));
                    var art=HoistChild(mast,"Owned lattice crane mesh",mast.position);
                    art.gameObject.AddComponent<MeshFilter>().sharedMesh=crane;art.gameObject.AddComponent<MeshRenderer>().sharedMaterial=steel;
                    float scale=8/Mathf.Max(.1f,crane.bounds.size.y);art.localScale=Vector3.one*scale;
                    art.localPosition=-new Vector3(crane.bounds.center.x,crane.bounds.min.y,crane.bounds.center.z)*scale;
                    foreach(var vertex in crane.vertices){Vector3 world=art.TransformPoint(vertex);if(world.y<6&&Mathf.Abs(world.x-x)<8&&world.z>50&&world.z<125)
                        throw new InvalidOperationException("The northern support intrudes into the real recovery driving corridor.");}
                    art.gameObject.AddComponent<MeshCollider>().sharedMesh=crane;
                }
                var crossmember=HoistChild(extension,"Owned northern overhead recovery crossmember",new Vector3(x,8.45f,z+10));
                LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(crossmember,floorPath,new Vector3(36,.6f,3),-.3f,true,Folder);
                var crossCollider=crossmember.gameObject.AddComponent<BoxCollider>();crossCollider.size=new Vector3(36,.6f,3);crossCollider.sharedMaterial=contact;
                var beam=HoistChild(extension,"Owned longitudinal trolley support rail",new Vector3(x,8.45f,z+5));
                LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(beam,floorPath,new Vector3(1.2f,.6f,10),-.3f,true,Folder);
                var beamCollider=beam.gameObject.AddComponent<BoxCollider>();beamCollider.size=new Vector3(1.2f,.6f,10);beamCollider.sharedMaterial=contact;
                // Same isolated rail constraint as Skyhook: actual trolley forces
                // act along Z; both transverse axes and all rotation stay locked.
                rail=hoist.carrier.trolley.gameObject.AddComponent<ConfigurableJoint>();rail.autoConfigureConnectedAnchor=false;
                rail.anchor=Vector3.zero;rail.connectedAnchor=start+Vector3.forward*5;rail.axis=Vector3.forward;rail.secondaryAxis=Vector3.up;
                rail.xMotion=ConfigurableJointMotion.Limited;rail.yMotion=rail.zMotion=ConfigurableJointMotion.Locked;
                rail.linearLimit=new SoftJointLimit{limit=5};rail.angularXMotion=rail.angularYMotion=rail.angularZMotion=ConfigurableJointMotion.Locked;
                rail.enableCollision=false;hoist.translationRail=rail;hoist.carrier.routeEnd=start+Vector3.forward*10;hoist.carrier.maxSpeed=1.25f;
                EditorUtility.SetDirty(hoist);EditorUtility.SetDirty(hoist.carrier);EditorSceneManager.MarkSceneDirty(scene);
                return hoist;
            }
            catch
            {
                if(rail!=null)UnityEngine.Object.DestroyImmediate(rail);UnityEngine.Object.DestroyImmediate(extension.gameObject);throw;
            }
        }

        static Transform HoistChild(Transform parent,string name,Vector3 position){var root=new GameObject(name).transform;root.SetParent(parent,false);root.position=position;return root;}
        static T Required<T>(string path)where T:UnityEngine.Object{var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(asset==null)throw new InvalidOperationException("Existing recovery asset required: "+path);return asset;}

        static void PositionExtractionRamp(ThinIceRescueObjectives rescue, Transform ramp)
        {
            // The next sheet starts at the northern edge of the incident tile.
            // Meet its upper surface there, before the trailer reaches the
            // slab face. Allow room for a buoyant load drifting off centre.
            Bounds tile = rescue.incidentIce.surface.bounds;
            const float deckThickness = .35f;
            Vector3 low = new Vector3(tile.center.x, tile.center.y - 3, tile.center.z - 8);
            Vector3 high = new Vector3(tile.center.x, tile.max.y - deckThickness * .5f, tile.max.z + .5f);
            Vector3 slope = high - low;
            ramp.SetPositionAndRotation((low + high) * .5f,
                Quaternion.Euler(-Mathf.Atan2(slope.y, slope.z) * Mathf.Rad2Deg, 0, 0));
            ramp.localScale = new Vector3(Mathf.Min(18, tile.size.x - 2), deckThickness, slope.magnitude);
        }

        static void OwnedProp(Transform parent, string source, Vector3 position, Vector3 size)
        {
            var holder = new GameObject("Owned rescue station equipment").transform; holder.SetParent(parent,false); holder.position = position;
            LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(holder,source,size,0,true,Folder);
        }

        static SportsFloatingBody AddFlotation(Rigidbody body, ThinIceMatch match, string label, bool cargo)
        {
            var existing = body.GetComponent<SportsFloatingBody>();
            if (existing != null && existing.session != match) throw new InvalidOperationException("This body already has a different flotation authority; preserve it rather than duplicating.");
            var boxes = body.GetComponentsInChildren<BoxCollider>(true).Where(c => !c.isTrigger && OwningBody(c) == body && (!cargo || c.name == "trailer")).ToArray();
            if (boxes.Length == 0) throw new InvalidOperationException("Original sealed hull colliders required for recovery displacement: " + body.name + "; found " + string.Join(", ", body.GetComponentsInChildren<BoxCollider>(true).Select(c=>c.name+" owner="+(OwningBody(c)!=null?OwningBody(c).name:"none")+" trigger="+c.isTrigger)));
            // collider.bounds can still describe the prefab's previous world pose in
            // stopped authoring immediately after CreateTrailer/moving a Transform.
            // Transform the exact collider-local vertices; no physics AABB or stale
            // world pose is involved, and every original closed box remains closed.
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var source = cube.GetComponent<MeshFilter>().sharedMesh;
            var unitVertices = source.vertices; var unitTriangles = source.triangles;
            UnityEngine.Object.DestroyImmediate(cube);
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            foreach (var collider in boxes)
            {
                int first = vertices.Count;
                var toBody = body.transform.worldToLocalMatrix * collider.transform.localToWorldMatrix;
                foreach (var vertex in unitVertices) vertices.Add(toBody.MultiplyPoint3x4(collider.center + Vector3.Scale(vertex, collider.size)));
                bool mirrored = toBody.determinant < 0;
                for (int index = 0; index < unitTriangles.Length; index += 3)
                {
                    triangles.Add(first + unitTriangles[index]);
                    triangles.Add(first + unitTriangles[index + (mirrored ? 2 : 1)]);
                    triangles.Add(first + unitTriangles[index + (mirrored ? 1 : 2)]);
                }
            }
            string path = "Assets/TruckExtremeHauling/Art/"+label+".asset";
            var rebuilt = new Mesh { name = label }; rebuilt.SetVertices(vertices); rebuilt.SetTriangles(triangles, 0);
            rebuilt.RecalculateNormals(); rebuilt.RecalculateBounds();
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                Directory.CreateDirectory("Assets/TruckExtremeHauling/Art"); AssetDatabase.CreateAsset(rebuilt, path); mesh = rebuilt;
            }
            else { EditorUtility.CopySerialized(rebuilt, mesh); UnityEngine.Object.DestroyImmediate(rebuilt); EditorUtility.SetDirty(mesh); }
            var floating = existing != null ? existing : body.gameObject.AddComponent<SportsFloatingBody>();
            floating.body=body;floating.proxy=mesh;floating.session=match;floating.waterHeight=-.55f;floating.waveAmplitude=.025f;floating.enabled=false;
            EditorUtility.SetDirty(floating);
            Debug.Log("CONVOY_RESCUE_HULL " + body.name + " mass=" + body.mass + " localCenter=" + mesh.bounds.center + " localSize=" + mesh.bounds.size + " sealedVolume=" + Volume(mesh).ToString("F3") + "m3");
            return floating;
        }

        static void RepairFlotation(ThinIceRescueObjectives rescue)
        {
            var match = rescue.match;
            rescue.cargoFloat = AddFlotation(rescue.strandedCargo.Body, match, "Stranded original cargo sealed displacement", true);
            rescue.playerFloats = new SportsFloatingBody[match.trucks.Length];
            var auxiliary = new List<SportsFloatingBody>();
            for (int i = 0; i < match.trucks.Length; i++)
            {
                var tractor = match.trucks[i];
                rescue.playerFloats[i] = AddFlotation(tractor.body, match, "Recovery tractor sealed displacement", false);
                foreach (var body in tractor.GetComponentsInChildren<Rigidbody>(true))
                {
                    if (body == tractor.body || !body.GetComponentsInChildren<BoxCollider>(true).Any(c => !c.isTrigger && OwningBody(c) == body)) continue;
                    // The original NWH cab is a separate 1500kg physical body with
                    // its own sealed 8.813m3 collider. Omitting it leaves only a
                    // 4.657m3 chassis supporting an 8900kg assembly: it must sink.
                    // Keep that jointed cab and apply the same vendor hydrostatic
                    // authority once on its actual body, without changing its mass.
                    auxiliary.Add(AddFlotation(body, match, "Recovery " + body.name + " sealed displacement", false));
                }
            }
            rescue.auxiliaryFloats = auxiliary.ToArray();
            EditorUtility.SetDirty(rescue); EditorSceneManager.MarkSceneDirty(rescue.gameObject.scene);
            Debug.Log("CONVOY_RESCUE_HULLS_REPAIRED: exact collider-local closed meshes, original asset GUIDs retained; " + auxiliary.Count + " native child bodies retain their own vendor hydrostatics. Rescue option only; no global water, gravity, mass or joint changes.");
        }
        static float Volume(Mesh mesh)
        {
            var vertices = mesh.vertices; var triangles = mesh.triangles; double volume = 0;
            for (int i = 0; i < triangles.Length; i += 3)
                volume += Vector3.Dot(vertices[triangles[i]], Vector3.Cross(vertices[triangles[i + 1]], vertices[triangles[i + 2]])) / 6d;
            return (float)Math.Abs(volume);
        }
        static Rigidbody OwningBody(Collider collider)
        {
            // Resolve the same nearest-parent Rigidbody ownership from the saved
            // hierarchy. PhysX attachedRigidbody can be null while an entire authored
            // incident is inactive; authoring must not activate it to query ownership.
            for(var cursor=collider.transform;cursor!=null;cursor=cursor.parent)
            { var body=cursor.GetComponent<Rigidbody>();if(body!=null)return body; }
            return null;
        }
    }
}
