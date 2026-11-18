using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckHitchSports;
using LWS.TruckHitchSports.Editor;
using LWS.TruckKeepaway.Editor;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckExtremeHauling.Editor
{
    public static class CargoEmergencyAuthoring
    {
        const string ScenePath = "Assets/TruckExtremeHauling/Scenes/CargoCrisis.unity";
        const string Folder = "Assets/TruckExtremeHauling/CargoEmergencySites";
        const string Fire = "Assets/TruckDragonKingdom/Prefabs/Dragons/dragon-fire/dragon-fire Owned Breath URP.prefab";
        const string Smoke = "Assets/NWH/Vehicle Physics 2/Effects/Prefabs/ExhaustSmoke.prefab";
        const string SmokeMaterial = "Assets/TruckTornadoRodeo/Materials/Owned Weather Maker smoke vortex particles.mat";
        const string Warehouse = "Assets/JC_LP_MegaCity/Prefabs/Buildings/SM_Buildings_Warehouse_01.prefab";
        const string Container = "Assets/ShipContainerPack/URP/Prefabs/ShipContainerGray_URP.prefab";
        const string Pallet = "Assets/ConstructionPropsBundle/URP/Prefabs/SM_Pallet Variant.prefab";
        const string Planks = "Assets/ConstructionPropsBundle/URP/Prefabs/SM_Planks01 Variant.prefab";
        const string Crates = "Assets/ConstructionPropsBundle/URP/Prefabs/SM_BoxesPack01 Variant.prefab";
        const string Barrier = "Assets/RoadPropsPack/URP/Prefabs/RoadBlock02_URP.prefab";
        const string WreckVehicle = "Assets/NWH/Vehicle Physics 2/Vehicles/Niva/Niva.prefab";
        const string RoadSign = "Assets/RoadPropsPack/URP/Prefabs/RoadSign_URP.prefab";

        // Isolated batch entry. Existing fire/flood/default venues are retained.
        public static void AuthorSavedHighwayPileup()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before isolated pileup authoring.");
            if(!System.IO.File.Exists(ScenePath))throw new System.IO.FileNotFoundException("Preserve the existing Cargo Crisis scene.",ScenePath);
            var materials=new HashSet<string>(AssetDatabase.FindAssets("t:Material",new[]{"Assets/TruckExtremeHauling/Materials"}).Select(id=>AssetDatabase.GUIDToAssetPath(id)));
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);UpgradeActiveHighwayPileup();EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Could not save the existing Cargo Crisis scene.");
            AssetDatabase.SaveAssets();Debug.Log("CARGO_PILEUP_SAVED scene="+ScenePath+"; original native recovery/scoring retained; natural acceptance remains unverified.");
            foreach(string id in AssetDatabase.FindAssets("t:Material",new[]{"Assets/TruckExtremeHauling/Materials"})){
                string path=AssetDatabase.GUIDToAssetPath(id);if(!materials.Contains(path))Debug.Log("CARGO_PILEUP_NEW_OWNED_MATERIAL "+path);
            }
        }
        [MenuItem("Truck Sports/Alpha 0.10/Install active Cargo Crisis highway pileup")]
        public static void InstallActivePileup()=>UpgradeActiveHighwayPileup();
        public static CargoEmergencySite UpgradeActiveHighwayPileup()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().path!=ScenePath)
                throw new InvalidOperationException("Open the existing Cargo Crisis scene outside Play Mode.");
            var match=UnityEngine.Object.FindAnyObjectByType<CargoCrisisMatch>();var sites=match!=null?match.emergencySites:null;
            if(sites==null||sites.defaultSupport==null||match.trucks.Length!=4||match.objectives.Length!=4)
                throw new InvalidOperationException("Preserve the existing four-slot Cargo Crisis authority and authored emergency adapter.");
            if (sites.highwayPileup?.environment != null)
            {
                var existing = sites.highwayPileup;
                for (int lane = 0; lane < existing.loads.Length; lane++)
                {
                    var load = existing.loads[lane];
                    RestPileupLoadOnItsSide(load);
                    var parkedWreck = existing.wrecks[lane * 2];
                    float spacing = load.transform.position.x - 4.6f - parkedWreck.body.transform.position.x;
                    parkedWreck.body.transform.position += Vector3.right * spacing;
                    foreach (var pose in parkedWreck.resetPoses)
                        pose.position += Vector3.right * spacing;
                    RequireNoInitialCargoOverlap(load, parkedWreck);
                }
                return existing;
            }
            Need<GameObject>(WreckVehicle);Need<GameObject>(Barrier);Need<GameObject>(RoadSign);
            var contact=Need<PhysicsMaterial>("Assets/TruckExtremeHauling/Materials/Freight concrete.asset");
            var paint=Need<Material>("Assets/TruckExtremeHauling/Materials/Freight safety orange.mat");
            var site=new CargoEmergencySite{title="HIGHWAY PILEUP RECOVERY",environment=new GameObject("Selected incident · physical highway pileup"),
                loads=new TrailerBody[4],truckStarts=new Transform[4],depots=new Transform[4],heatSources=Array.Empty<Transform>(),
                flotation=Array.Empty<SportsFloatingBody>(),floodedVolumes=Array.Empty<Bounds>(),dryBankX=Array.Empty<float>(),cableReach=22};
            try{
                var root=site.environment.transform;var wrecks=new List<CargoPileupWreck>();
                for(int lane=0;lane<4;lane++){
                    float x=lane*80;
                    var start=new GameObject("Pileup recovery staging "+lane).transform;start.SetParent(root,false);start.position=new Vector3(x,1,-85);site.truckStarts[lane]=start;
                    var depot=new GameObject("Pileup cargo supported delivery "+lane).transform;depot.SetParent(root,false);depot.position=new Vector3(x,1,match.objectives[lane].position.z);site.depots[lane]=depot;
                    var load=HitchAuthoring.CreateTrailer(new Vector3(x,1,-45),Quaternion.Euler(0,0,90),"Pileup original native cargo "+lane);load.transform.SetParent(root,true);site.loads[lane]=load;
                    var stand=load.Module.module.trailerStand;if(stand==null)throw new InvalidOperationException("Original native parking stand required.");
                    Vector3 leg=stand.transform.localPosition;leg.y=.544f-CargoLandingGearAuthoring.RecoveryStandExtensionMetres;stand.transform.localPosition=leg;
                    RestPileupLoadOnItsSide(load);
                    // The first real wreck catches/obstructs the tipped load's
                    // left-rear footprint. The second interrupts the haul lane;
                    // shoulders remain open for careful real steering/recovery.
                    var recoveryWreck=PileupVehicle(root,lane,new Vector3(x-4.6f,0,-51),Quaternion.Euler(0,30,155),"Recovery-side overturned vehicle");
                    RequireNoInitialCargoOverlap(load,recoveryWreck);wrecks.Add(recoveryWreck);
                    wrecks.Add(PileupVehicle(root,lane,new Vector3(x+3.8f,0,65),Quaternion.Euler(0,70,105),"Cross-lane rolled vehicle"));
                    foreach(int side in new[]{-1,1}){
                        Owned(root,"Visible highway recovery warning",RoadSign,new Vector3(x+side*22,0,-95),new Vector3(2,4,.6f),side<0?0:180,contact);
                        foreach(float z in new[]{-15f,85f,180f})Owned(root,"Visible highway shoulder barrier",Barrier,new Vector3(x+side*26,0,z),new Vector3(4,1.4f,.8f),0,contact);
                        var line=new GameObject("Highway shoulder paint "+lane+" "+side).AddComponent<LineRenderer>();line.transform.SetParent(root,false);
                        line.useWorldSpace=true;line.sharedMaterial=paint;line.widthMultiplier=.14f;line.positionCount=2;
                        line.SetPosition(0,new Vector3(x+side*19,.025f,-110));line.SetPosition(1,new Vector3(x+side*19,.025f,depot.position.z+20));
                    }
                    Owned(root,"Pileup depot marker left",Barrier,depot.position+new Vector3(-12,-1,0),new Vector3(3,1.4f,.8f),0,contact);
                    Owned(root,"Pileup depot marker right",Barrier,depot.position+new Vector3(12,-1,0),new Vector3(3,1.4f,.8f),0,contact);
                }
                site.wrecks=wrecks.ToArray();site.environment.SetActive(false);sites.highwayPileup=site;EditorUtility.SetDirty(sites);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                Debug.Log("CARGO_PILEUP_AUTHORED: four tipped native cargo rigs, eight1210kg passive NWH vehicle wrecks with original bodies/colliders/joints, actual open shoulders; option6 only. No wreck contact awards points; original native delivery determines results.");return site;
            }catch{UnityEngine.Object.DestroyImmediate(site.environment);throw;}
        }

        static CargoPileupWreck PileupVehicle(Transform parent,int lane,Vector3 position,Quaternion rotation,string label)
        {
            var root=(GameObject)PrefabUtility.InstantiatePrefab(Need<GameObject>(WreckVehicle));PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            root.name=label+" · native passive wreck";root.transform.SetParent(parent,true);root.transform.SetPositionAndRotation(position,rotation);
            var body=root.GetComponent<Rigidbody>();if(body==null||body.mass<500)throw new InvalidOperationException("The existing full-mass NWH vehicle Rigidbody is required.");
            // These are disabled, overturned nonparticipants. Preserve original
            // rigidbodies/joints/compound collision; do not run a second traffic,
            // wheel, powertrain or input authority for a passive crash obstacle.
            foreach(var script in root.GetComponentsInChildren<MonoBehaviour>(true))script.enabled=false;
            foreach(var camera in root.GetComponentsInChildren<Camera>(true))camera.enabled=false;
            foreach(var listener in root.GetComponentsInChildren<AudioListener>(true))UnityEngine.Object.DestroyImmediate(listener);
            foreach(var audio in root.GetComponentsInChildren<AudioSource>(true))audio.enabled=false;
            foreach(var particle in root.GetComponentsInChildren<ParticleSystem>(true)){particle.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=particle.main;main.playOnAwake=false;var emission=particle.emission;emission.enabled=false;}
            foreach(var collider in root.GetComponentsInChildren<Collider>(true))collider.gameObject.layer=2;
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true)){
                var materials=renderer.sharedMaterials;for(int i=0;i<materials.Length;i++)materials[i]=PileupMaterial(materials[i]);renderer.sharedMaterials=materials;
            }
            float bottom=float.PositiveInfinity;
            foreach(var box in root.GetComponentsInChildren<BoxCollider>(true))if(!box.isTrigger&&box.GetComponentInParent<Rigidbody>(true)==body){
                for(int i=0;i<8;i++){Vector3 corner=box.center+Vector3.Scale(box.size*.5f,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));bottom=Mathf.Min(bottom,box.transform.TransformPoint(corner).y);}
            }
            if(float.IsInfinity(bottom))throw new InvalidOperationException("Original vehicle body boxes required for a grounded authored wreck.");
            root.transform.position+=Vector3.up*(.06f-bottom);
            var wreck=new CargoPileupWreck{lane=lane,body=body,parts=root.GetComponentsInChildren<Rigidbody>(true).OrderBy(part=>part==body?0:1).ToArray()};wreck.resetPoses=new Transform[wreck.parts.Length];
            for(int i=0;i<wreck.parts.Length;i++){
                var part=wreck.parts[i];part.isKinematic=true;part.interpolation=RigidbodyInterpolation.Interpolate;part.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
                var pose=new GameObject("Saved pileup vehicle part "+lane+" "+label+" "+i).transform;pose.SetParent(parent,false);pose.SetPositionAndRotation(part.transform.position,part.transform.rotation);wreck.resetPoses[i]=pose;
            }
            return wreck;
        }
        static Material PileupMaterial(Material source)
        {
            if(source==null||source.shader.name.StartsWith("Universal Render Pipeline/",StringComparison.Ordinal))return source;
            string path="Assets/TruckExtremeHauling/Materials/Pileup native "+source.name.Replace('/','_')+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null)return material;
            material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.mainTexture=source.mainTexture;
            material.color=source.HasProperty("_Color")?source.color:Color.white;material.SetFloat("_Smoothness",.3f);material.enableInstancing=true;
            if(source.renderQueue>=3000){material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",5);material.SetFloat("_DstBlend",10);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;}
            AssetDatabase.CreateAsset(material,path);return material;
        }
        static void RestPileupLoadOnItsSide(TrailerBody load)
        {
            // Lay the original solid trailer on level asphalt without drop energy.
            load.transform.rotation = Quaternion.Euler(0, 0, 90);
            float bottom = float.PositiveInfinity;
            foreach (var box in load.GetComponentsInChildren<BoxCollider>(true))
            {
                if (!box.enabled || box.isTrigger || box.attachedRigidbody != load.Body) continue;
                Vector3 half = box.size * .5f;
                float verticalExtent = Mathf.Abs(box.transform.TransformVector(Vector3.right * half.x).y)
                    + Mathf.Abs(box.transform.TransformVector(Vector3.up * half.y).y)
                    + Mathf.Abs(box.transform.TransformVector(Vector3.forward * half.z).y);
                bottom = Mathf.Min(bottom, box.transform.TransformPoint(box.center).y - verticalExtent);
            }
            if (float.IsInfinity(bottom))
                throw new InvalidOperationException("The pileup requires the original native solid trailer body.");
            load.transform.position += Vector3.up * (.025f - bottom);
        }

        static void RequireNoInitialCargoOverlap(TrailerBody load,CargoPileupWreck wreck)
        {
            foreach(var cargo in load.GetComponentsInChildren<Collider>(true))if(cargo.enabled&&!cargo.isTrigger)
                foreach(var shape in wreck.body.GetComponentsInChildren<Collider>(true))if(shape.enabled&&!shape.isTrigger&&
                    Physics.ComputePenetration(cargo,cargo.transform.position,cargo.transform.rotation,shape,shape.transform.position,shape.transform.rotation,out _,out float depth)&&depth>.005f)
                    throw new InvalidOperationException("The authored pileup starts with solid cargo/vehicle overlap: "+cargo.name+" / "+shape.name+" depth="+depth+". Preserve collision instead of forcing the spawn.");
        }

        [MenuItem("Truck Sports/Alpha 0.10/Install active Cargo Crisis fire and flooded loading sites")]
        public static void InstallActiveSites() => UpgradeActiveScene();

        public static CargoEmergencyScenarios UpgradeActiveScene()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before authoring emergency incidents.");
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Open the existing saved CargoCrisis scene.");
            var match = UnityEngine.Object.FindObjectsByType<CargoCrisisMatch>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(m => m.gameObject.scene == scene);
            if (match == null || match.trucks == null || match.trucks.Length != 4 || match.trailers == null || match.trailers.Length != 4 || match.objectives == null || match.objectives.Length != 4)
                throw new InvalidOperationException("Preserve Cargo Crisis's original four-slot native recovery rigs and depots.");
            if (match.emergencySites != null) return match.emergencySites;
            var support = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Continuous venue support");
            if (support == null || support.GetComponent<Collider>() == null)
                throw new InvalidOperationException("The original support floor must remain available for baseline modes.");
            foreach (string path in new[] { Fire, Smoke, Warehouse, Container, Pallet, Planks, Crates, Barrier }) Need<GameObject>(path);
            var smoke = Need<Material>(SmokeMaterial);
            var concrete = Need<PhysicsMaterial>("Assets/TruckExtremeHauling/Materials/Freight concrete.asset");
            var ground = Need<Material>("Assets/TruckExtremeHauling/Materials/Quarry and depot asphalt.mat");
            var steel = Need<Material>("Assets/TruckExtremeHauling/Materials/Freight tower gunmetal.mat");
            var water = Need<Material>("Assets/TruckWaterSports/Materials/BlackwaterMatch river surface.mat");
            EnsureFolder(Folder + "/Meshes"); EnsureFolder("Assets/TruckExtremeHauling/Materials");

            var sites = match.gameObject.AddComponent<CargoEmergencyScenarios>(); sites.match = match; sites.defaultSupport = support;
            // Original flat-site barriers would hang above the selected depressed bay.
            // Record only those four exact scene-owned props; restore their active states
            // whenever an ordinary or fire scenario is selected.
            var replaced = new List<GameObject>();
            foreach(var prop in scene.GetRootGameObjects())
                if(prop.name.IndexOf("RoadBlock02",StringComparison.OrdinalIgnoreCase)>=0)
                    for(int i=0;i<4;i++)if(Vector3.Distance(prop.transform.position,new Vector3(i*80+12,0,-40))<.25f){replaced.Add(prop);break;}
            sites.floodedSiteReplacedRoadblocks = replaced.ToArray();
            try
            {
                sites.industrialFire = Build(match, false, ground, steel, water, smoke, concrete);
                sites.floodedLoading = Build(match, true, ground, steel, water, smoke, concrete);
            }
            catch
            {
                if(sites.industrialFire?.environment!=null)UnityEngine.Object.DestroyImmediate(sites.industrialFire.environment);
                if(sites.floodedLoading?.environment!=null)UnityEngine.Object.DestroyImmediate(sites.floodedLoading.environment);
                UnityEngine.Object.DestroyImmediate(sites);throw;
            }
            match.emergencySites = sites;
            EditorUtility.SetDirty(match); EditorUtility.SetDirty(sites); EditorSceneManager.MarkSceneDirty(scene);
            Undo.RegisterCreatedObjectUndo(sites.industrialFire.environment, "Add authored industrial fire incident");
            Undo.RegisterCreatedObjectUndo(sites.floodedLoading.environment, "Add authored flooded loading incident");
            Debug.Log("CARGO_EMERGENCIES_AUTHORED: explicit options4/5, eight original NWH loads with existing landing-gear extension, owned production industrial props/fire/smoke, actual flooded bays/Archimeds displacement and visible extraction slopes. Baseline options0–3 support/course/rigs/scoring untouched. Caller saves; natural gameplay acceptance pending.");
            return sites;
        }

        static CargoEmergencySite Build(CargoCrisisMatch match, bool flooded, Material ground, Material steel, Material water, Material smoke, PhysicsMaterial contact)
        {
            var site = new CargoEmergencySite
            {
                title = flooded ? "FLOODED LOADING RESCUE" : "INDUSTRIAL FIRE RECOVERY",
                environment = new GameObject(flooded ? "Selected incident · flooded collapsed loading bays" : "Selected incident · industrial freight fire"),
                loads = new TrailerBody[4], truckStarts = new Transform[4], depots = new Transform[4],
                heatSources = flooded ? Array.Empty<Transform>() : new Transform[4],
                flotation = flooded ? new SportsFloatingBody[4] : Array.Empty<SportsFloatingBody>(),
                floodedVolumes = flooded ? new Bounds[4] : Array.Empty<Bounds>(),
                dryBankX = flooded ? new float[4] : Array.Empty<float>(),
                cableReach = flooded ? 37 : 22, bankApproach = flooded ? 28 : 12
            };
            var root = site.environment.transform;
            try
            {
            if (flooded)
            {
                // Explicit selected-site supports: only this scenario disables the old
                // continuous floor. Every new collision surface is visible concrete/steel.
                SportsEventAuthoring.Box("Submerged loading-bay floor", new Vector3(120, -4.5f, 100), new Vector3(550, 2, 600), ground, contact, root);
                SportsEventAuthoring.Box("Dry southern recovery staging apron", new Vector3(120, -1, -130), new Vector3(550, 2, 140), ground, contact, root);
                SportsEventAuthoring.Box("Dry northern freight haul road", new Vector3(120, -1, 140), new Vector3(550, 2, 320), ground, contact, root);
                SportsEventAuthoring.Box("Dry western freight apron", new Vector3(-85, -1, -40), new Vector3(140, 2, 40), ground, contact, root);
            }
            for (int i = 0; i < 4; i++)
            {
                float x = i * 80;
                var start = new GameObject("Incident " + (i + 1) + " recovery truck staging").transform; start.SetParent(root, false);
                start.position = new Vector3(x + (flooded ? 28 : 0), 1, -75); site.truckStarts[i] = start;
                var depot = new GameObject("Incident " + (i + 1) + " supported cargo delivery stop").transform; depot.SetParent(root, false);
                depot.position = new Vector3(x + (flooded ? 28 : 0), 1, match.objectives[i].position.z); site.depots[i] = depot;
                var load = HitchAuthoring.CreateTrailer(new Vector3(x, 3, -45), Quaternion.Euler(0, 0, 65), site.title + " original native cargo " + (i + 1));
                load.transform.SetParent(root, true); site.loads[i] = load;
                // Same existing physical landing-gear correction used by the passing
                // baseline, applied solely to these newly instantiated sport-owned rigs.
                var stand = load.Module.module.trailerStand;
                if (stand == null) throw new InvalidOperationException("The original native trailer landing stand is required.");
                var leg = stand.transform.localPosition; leg.y = .544f - CargoLandingGearAuthoring.RecoveryStandExtensionMetres; stand.transform.localPosition = leg;
                Owned(root, "Freight incident warehouse " + (i + 1), Warehouse, new Vector3(x - 25, 0, -120), new Vector3(26, 15, 28), 0, contact);
                Owned(root, "Recovered freight depot marker left", Barrier, depot.position + new Vector3(-12, -1, 0), new Vector3(3, 1.4f, .8f), 0, contact);
                Owned(root, "Recovered freight depot marker right", Barrier, depot.position + new Vector3(12, -1, 0), new Vector3(3, 1.4f, .8f), 0, contact);
                if (flooded)
                {
                    // Dry bank starts at x+25. A real 17m cross-slope rises from
                    // the submerged floor to it; native cable/wheels perform extraction.
                    SportsEventAuthoring.Box("Dry right-bank recovery service lane " + i, new Vector3(x + 47.5f, -1, -40), new Vector3(45, 2, 40), ground, contact, root);
                    var low = new Vector3(x + 8, -3.5f, -40); var high = new Vector3(x + 25, 0, -40);
                    var ramp = SportsEventAuthoring.Box("Physical flooded loading extraction slope " + i, (low + high) * .5f - Vector3.up * .2f,
                        new Vector3(Vector3.Distance(low, high), .4f, 40), steel, contact, root);
                    ramp.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(high.y - low.y, high.x - low.x) * Mathf.Rad2Deg);
                    var surface = GameObject.CreatePrimitive(PrimitiveType.Quad); surface.name = "River Modeler flooded loading-bay water " + i;
                    surface.transform.SetParent(root, false); surface.transform.SetPositionAndRotation(new Vector3(x + 7.5f, -.55f, -40), Quaternion.Euler(90, 0, 0));
                    surface.transform.localScale = new Vector3(35, 40, 1); surface.GetComponent<Renderer>().sharedMaterial = water; UnityEngine.Object.DestroyImmediate(surface.GetComponent<Collider>());
                    site.floodedVolumes[i] = new Bounds(new Vector3(x + 7.5f, 0, -40), new Vector3(35, 40, 40)); site.dryBankX[i] = x + 25;
                    site.flotation[i] = AddFloat(load, match, i);
                    // Real scattered loading timbers at the far edge imply a collapsed
                    // loading apron without invisible geometry across the recovery lane.
                    Owned(root, "Collapsed loading timber pile " + i, Planks, new Vector3(x - 6, -2.1f, -23), new Vector3(5, 2.5f, 5), 24, contact);
                    Owned(root, "Stranded pallet stacks " + i, Pallet, new Vector3(x - 4, -3.45f, -29), new Vector3(3, 1, 3), -18, contact);
                    Owned(root, "Cargo beside flooded loading bay " + i, Crates, new Vector3(x + 40, 0, -22), new Vector3(4, 3, 4), 5, contact);
                }
                else
                {
                    var burning = new Vector3(x - 17, 0, -45);
                    Owned(root, "Burning freight container " + i, Container, burning, new Vector3(11, 4.5f, 5), 0, contact);
                    var source = new GameObject("Visible industrial fire heat source " + i).transform; source.SetParent(root, false); source.position = burning + Vector3.up * 2; site.heatSources[i] = source;
                    FireEffect(root, Fire, burning + new Vector3(0, 3.5f, 0), null, false);
                    FireEffect(root, Smoke, burning + new Vector3(0, 4, 0), smoke, true);
                    var lamp = new GameObject("Industrial blaze warm light " + i).AddComponent<Light>(); lamp.transform.SetParent(root, false); lamp.transform.position = source.position;
                    lamp.type = LightType.Point; lamp.color = new Color(1, .22f, .025f); lamp.range = 30; lamp.intensity = 6;
                    Owned(root, "Heat-exposed cargo crates " + i, Crates, burning + new Vector3(1, 0, 6), new Vector3(4, 3, 4), -9, contact);
                }
            }
            site.environment.SetActive(false); return site;
            }
            catch{UnityEngine.Object.DestroyImmediate(site.environment);throw;}
        }

        static void Owned(Transform parent, string name, string source, Vector3 position, Vector3 size, float yaw, PhysicsMaterial contact)
        {
            var holder = new GameObject(name).transform; holder.SetParent(parent, false);
            KeepawayPresentationAuthoring.Fit(holder, source, size, 0, false, "TruckExtremeHauling"); holder.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            foreach (var filter in holder.GetComponentsInChildren<MeshFilter>(true)) if (filter.sharedMesh != null && filter.GetComponent<Renderer>() != null)
            { var shape = filter.gameObject.AddComponent<MeshCollider>(); shape.sharedMesh = filter.sharedMesh; shape.sharedMaterial = contact; }
        }
        static void FireEffect(Transform parent, string source, Vector3 position, Material material, bool smoke)
        {
            var effect = UnityEngine.Object.Instantiate(Need<GameObject>(source)); effect.name = smoke ? "Owned NWH industrial smoke" : "Owned dragon fire industrial blaze";
            effect.transform.SetParent(parent, false); effect.transform.SetPositionAndRotation(position, Quaternion.Euler(-90, 0, 0));
            foreach (var behaviour in effect.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(behaviour);
            foreach (var shape in effect.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(shape);
            foreach (var particle in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particle.main; main.loop = true; main.playOnAwake = false; main.duration = 3; main.maxParticles = 100;
                main.startLifetime = smoke ? 5 : 1.8f; main.startSpeed = smoke ? 2.8f : 2.2f; main.startSize = smoke ? 3 : 2.4f;
                var emission = particle.emission; emission.enabled = true; emission.rateOverTime = smoke ? 10 : 35;
                var shape = particle.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 15; shape.radius = 2.2f;
                if (material != null) particle.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
        static SportsFloatingBody AddFloat(TrailerBody load, CargoCrisisMatch match, int slot)
        {
            var body = load.Body;
            if (body.GetComponent<SportsFloatingBody>() != null) throw new InvalidOperationException("Preserve the single existing flotation authority.");
            var shapes = load.GetComponentsInChildren<BoxCollider>(true).Where(c => !c.isTrigger && c.attachedRigidbody == body && c.name == "trailer").ToArray();
            if (shapes.Length == 0) throw new InvalidOperationException("The original trailer's sealed cargo-box collision geometry is required.");
            var bounds = new Bounds(); bool first = true;
            foreach (var shape in shapes) for (int corner = 0; corner < 8; corner++)
            {
                // Use actual oriented collider corners rather than the tipped world's
                // expanded AABB, which would invent excess displaced volume.
                var local = shape.center + Vector3.Scale(shape.size * .5f, new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                var point = body.transform.InverseTransformPoint(shape.transform.TransformPoint(local));
                if (first) { bounds = new Bounds(point, Vector3.zero); first = false; } else bounds.Encapsulate(point);
            }
            string path = Folder + "/Meshes/Original cargo sealed displacement " + slot + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); mesh = UnityEngine.Object.Instantiate(cube.GetComponent<MeshFilter>().sharedMesh); UnityEngine.Object.DestroyImmediate(cube);
                var vertices = mesh.vertices; for (int v = 0; v < vertices.Length; v++) vertices[v] = Vector3.Scale(vertices[v], bounds.size) + bounds.center;
                mesh.vertices = vertices; mesh.RecalculateBounds(); mesh.name = "Original native sealed cargo displacement " + slot; AssetDatabase.CreateAsset(mesh, path);
            }
            var floating = body.gameObject.AddComponent<SportsFloatingBody>(); floating.body = body; floating.proxy = mesh; floating.session = match; floating.waterHeight = -.55f; floating.waveAmplitude = .025f; floating.enabled = false; return floating;
        }
        static T Need<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Installed owned dependency missing: " + path);
        static void EnsureFolder(string path)
        { if (AssetDatabase.IsValidFolder(path)) return; int i = path.LastIndexOf('/'); string parent = path.Substring(0, i); EnsureFolder(parent); AssetDatabase.CreateFolder(parent, path.Substring(i + 1)); }
    }
}
