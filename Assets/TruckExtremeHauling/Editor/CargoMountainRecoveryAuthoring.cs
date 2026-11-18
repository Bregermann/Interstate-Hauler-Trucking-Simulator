#if UNITY_EDITOR
using System;
using System.Linq;
using LWS.TruckHitchSports;
using LWS.TruckHitchSports.Editor;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckExtremeHauling.Editor
{
    public static class CargoMountainRecoveryAuthoring
    {
        const string ScenePath = "Assets/TruckExtremeHauling/Scenes/CargoCrisis.unity";
        const string Output = "Assets/TruckExtremeHauling/CargoMountainRecovery";
        const string Rock = "Assets/TruckFishing/Prefabs/River boulder 0.prefab";
        const string Tree = "Assets/JC_LP_MegaCity/Prefabs/Nature/SM_Nature_Tree_07.prefab";
        const string Crate = "Assets/LeartesStudios/Garage Warehouse Environment/Art/Prefabs/SM_PK04_WoodenCrate_01.prefab";
        const string CableReel = "Assets/ConstructionPropsBundle/URP/Prefabs/SM_CableReel01 Variant.prefab";
        const string FairleadName = "Mountain low front winch fairlead";

        public static void AuthorSavedMountainPassRecovery()
        {
            RequireStopped();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            UpgradeActiveMountainPassRecovery();
        }
        public static void RepairSavedMountainWinchApproach()
        {
            RequireStopped();EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);RepairActiveMountainWinchApproach();
        }
        [MenuItem("Truck Sports/Alpha 0.10/Repair active mountain front winch approach")]
        public static void RepairActiveMountainWinchApproach()
        {
            RequireStopped();var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!=ScenePath)throw new InvalidOperationException("Open the saved CargoCrisis venue explicitly.");
            var match=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<CargoCrisisMatch>(true)).Single();
            var site=match.emergencySites?.mountainRecovery;
            if(site?.environment==null||site.loads?.Length!=4||site.truckStarts?.Length!=4)throw new InvalidOperationException("Preserve the already authored mountain incident.");
            for(int slot=0;slot<4;slot++)
            {
                float x=match.trucks[slot].transform.position.x;var marker=site.truckStarts[slot];
                Vector3 corrected=new Vector3(x+20,31,RoofEye(site.loads[slot]).z);
                bool original=Vector3.Distance(marker.position,new Vector3(x,31,-95))<.01f&&Quaternion.Angle(marker.rotation,Quaternion.identity)<.01f;
                bool repaired=Vector3.Distance(marker.position,corrected)<.01f&&Quaternion.Angle(marker.rotation,Quaternion.LookRotation(Vector3.left))<.01f;
                if(!original&&!repaired)throw new InvalidOperationException("Customized mountain start "+slot+" preserved.");
            }
            var native=match.trucks.Select(value=>value.transform).Concat(site.loads.Select(value=>value.transform)).ToArray();
            var originalMatrices=native.Select(value=>value.localToWorldMatrix).ToArray();
            InstallFrontWinchApproach(match,site);
            if(!originalMatrices.SequenceEqual(native.Select(value=>value.localToWorldMatrix)))throw new InvalidOperationException("Native tractor/cargo poses must remain unchanged; edit only saved staging markers.");
            EditorUtility.SetDirty(match.emergencySites);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("HH10_CARGO_MOUNTAIN_FRONT_WINCH_REPAIRED low front fairleads, saved starts lane+20 facing west on actual terrace; original roads/loads/trucks unchanged; gameplay unverified.");
        }

        [MenuItem("Truck Sports/Alpha 0.10/Install active Cargo Crisis mountain recovery")]
        public static void UpgradeActiveMountainPassRecovery()
        {
            RequireStopped();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Open the saved CargoCrisis venue explicitly.");
            var match = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CargoCrisisMatch>(true)).Single();
            var sites = match.emergencySites;
            if (sites == null || sites.defaultSupport == null || sites.highwayPileup?.environment == null || match.trucks.Length != 4 || match.trailers.Length != 4 || match.objectives.Length != 4)
                throw new InvalidOperationException("Preserve the existing four-slot native Cargo Crisis and its emergency adapter.");
            if (sites.mountainRecovery?.environment != null)
            {
                var saved = sites.mountainRecovery;
                if (saved.loads?.Length != 4 || saved.loads.Any(load => load == null) || saved.truckStarts?.Length != 4 || saved.depots?.Length != 4)
                    throw new InvalidOperationException("Partial/customized mountain incident preserved; inspect before repairing.");
                Debug.Log("HH10_CARGO_MOUNTAIN_PRESERVED original authored mountain incident"); return;
            }
            foreach (var path in new[] { Rock, Tree, Crate, CableReel })
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) throw new InvalidOperationException("Existing owned mountain/depot art required: " + path);
            var road = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckExtremeHauling/Materials/Quarry and depot asphalt.mat");
            var contact = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/TruckExtremeHauling/Materials/Freight concrete.asset");
            if (road == null || contact == null) throw new InvalidOperationException("Reuse the established physical freight-road surface assets.");
            System.IO.Directory.CreateDirectory(Output + "/Materials"); AssetDatabase.Refresh();
            var protectedTransforms = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            var protectedMatrices = protectedTransforms.Select(value => value.localToWorldMatrix).ToArray();
            var site = new CargoEmergencySite
            {
                title = "MOUNTAIN PASS RECOVERY",
                environment = new GameObject("Selected incident · mountain pass native recovery"),
                loads = new TrailerBody[4], truckStarts = new Transform[4], depots = new Transform[4],
                heatSources = Array.Empty<Transform>(), flotation = Array.Empty<SportsFloatingBody>(),
                floodedVolumes = Array.Empty<Bounds>(), dryBankX = Array.Empty<float>(), cableReach = 22
            };
            try
            {
                var root = site.environment.transform;
                for (int slot = 0; slot < 4; slot++)
                {
                    float x = match.trucks[slot].transform.position.x;
                    Bake(root, "Mountain recovery terrace " + slot, new[] { new Vector3(x, 30, -120), new Vector3(x, 30, -80), new Vector3(x, 30, 0), new Vector3(x, 30, 45) }, 60, road, contact);
                    Bake(root, "Mountain descent road " + slot, new[] { new Vector3(x, 30, 45), new Vector3(x, 29, 65), new Vector3(x, 25, 95), new Vector3(x, 20, 125), new Vector3(x, 18, 145) }, 28, road, contact);
                    Bake(root, "Mountain receiving depot " + slot, new[] { new Vector3(x, 18, 145), new Vector3(x, 18, 165), new Vector3(x, 18, 210) }, 40, road, contact);
                    site.truckStarts[slot] = Marker(root, "Mountain native truck staging " + slot, new Vector3(x, 31, -95));
                    site.depots[slot] = Marker(root, "Mountain supported lower freight depot " + slot, new Vector3(x, 18.5f, 165));
                    var load = HitchAuthoring.CreateTrailer(new Vector3(x - 8, 33, -45), Quaternion.Euler(0, 0, 90), "Mountain pass stranded native cargo " + slot);
                    load.transform.SetParent(root, true); site.loads[slot] = load;
                    var stand = load.Module.module.trailerStand;
                    if (stand == null) throw new InvalidOperationException("Original NWH landing stand required.");
                    var standPosition = stand.transform.localPosition; standPosition.y = .544f - CargoLandingGearAuthoring.RecoveryStandExtensionMetres; stand.transform.localPosition = standPosition;
                    PlaceTippedLoadOnTerrace(load, 30);
                    // Owned rocks/pines describe an actual elevated mountain
                    // route. They add no hidden barriers or safety floor.
                    foreach (int side in new[] { -1, 1 })
                        for (int band = 0; band < 3; band++)
                        {
                            HaulingVenueLandscapeAuthoring.PlacePeripheralModel(root, Rock, new Vector3(x + side * 40, band * 10, -65 + band * 28), new Vector3(18, 21, 36), slot * 23 + band * 37, Output);
                            HaulingVenueLandscapeAuthoring.PlacePeripheralModel(root, Tree, new Vector3(x + side * 43, 27, 18 + band * 24), new Vector3(10, 18, 10), band * 57, Output);
                        }
                    HaulingVenueLandscapeAuthoring.PlacePeripheralModel(root, Crate, new Vector3(x + 16, 18, 183), new Vector3(4, 5, 4), 15, Output);
                    Sign(root, "Mountain recovery objective " + slot, new Vector3(x + 22, 35, -75), "MOUNTAIN RECOVERY\nWINCH · HITCH · CONTROL THE DESCENT");
                    Sign(root, "Mountain delivery objective " + slot, new Vector3(x, 23, 183), "CARGO SAFE\nSTOP YOUR ATTACHED LOAD HERE");
                }
                Physics.SyncTransforms();
                if (!protectedMatrices.SequenceEqual(protectedTransforms.Select(value => value.localToWorldMatrix))) throw new InvalidOperationException("Existing scene geometry/rigs/cameras changed unexpectedly.");
                site.environment.SetActive(false); sites.mountainRecovery = site;
                InstallFrontWinchApproach(match,site);
                EditorUtility.SetDirty(sites); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                Debug.Log("HH10_CARGO_MOUNTAIN_AUTHORED four elevated native tipped loads, real EasyRoads30m terraces/downhill18m depots, original winch/native hitch/AI/result authority; default and three earlier incidents preserved; option7 gameplay remains unverified.");
            }
            catch { UnityEngine.Object.DestroyImmediate(site.environment); throw; }
        }

        static Vector3 RoofEye(TrailerBody load)=>load.transform.TransformPoint(load.Body.centerOfMass+Vector3.up*2.1f+Vector3.right*.95f);
        static void InstallFrontWinchApproach(CargoCrisisMatch match,CargoEmergencySite site)
        {
            if(AssetDatabase.LoadAssetAtPath<GameObject>(CableReel)==null)throw new InvalidOperationException("Owned cable-reel model required for the visible front winch.");
            var sites=match.emergencySites;
            if(sites.mountainFairleads==null||sites.mountainFairleads.Length==0)sites.mountainFairleads=new Transform[4];
            if(sites.mountainFairleads.Length!=4)throw new InvalidOperationException("Preserve customized front-winch ownership.");
            for(int slot=0;slot<4;slot++)
            {
                var truck=match.trucks[slot];var fairlead=sites.mountainFairleads[slot];
                if(fairlead==null)
                {
                    if(truck.transform.Find(FairleadName)!=null)throw new InvalidOperationException("Unassigned existing front hardware preserved.");
                    float front=HitchAuthoring.Bounds(truck.gameObject).max.z;
                    fairlead=new GameObject(FairleadName).transform;fairlead.SetParent(truck.transform,false);fairlead.localPosition=new Vector3(0,.3f,front+.05f);
                    var model=new GameObject("Owned low front cable reel").transform;model.SetParent(fairlead,false);model.localPosition=new Vector3(0,-.22f,-.12f);
                    LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(model,CableReel,new Vector3(.65f,.55f,.45f),0,true,"TruckExtremeHauling");
                    sites.mountainFairleads[slot]=fairlead;
                }
                else if(fairlead.parent!=truck.transform||fairlead.name!=FairleadName||Mathf.Abs(fairlead.localPosition.y-.3f)>.01f||fairlead.GetComponentsInChildren<Collider>(true).Length!=0)
                    throw new InvalidOperationException("Customized native front hardware preserved.");
                fairlead.gameObject.SetActive(false);
                float laneX=truck.transform.position.x;
                site.truckStarts[slot].SetPositionAndRotation(new Vector3(laneX+20,31,RoofEye(site.loads[slot]).z),Quaternion.LookRotation(Vector3.left));
                EditorUtility.SetDirty(site.truckStarts[slot]);
            }
        }

        static void Bake(Transform parent, string name, Vector3[] points, float width, Material road, PhysicsMaterial contact)
            => SportsEasyRoadsAuthoring.Bake("TruckExtremeHauling/CargoMountainRecovery", name, points, width, road, contact, true).SetParent(parent, true);
        static Transform Marker(Transform parent, string name, Vector3 position)
        {
            var marker = new GameObject(name).transform; marker.SetParent(parent, false); marker.position = position; return marker;
        }
        static void Sign(Transform parent, string name, Vector3 position, string message)
        {
            var text = new GameObject(name).AddComponent<TextMeshPro>(); text.transform.SetParent(parent, false); text.transform.position = position; text.transform.rotation = Quaternion.Euler(0, 180, 0);
            text.font = SportsBroadcastAuthoring.Theme().headingFont; text.text = message; text.fontSize = 3.2f; text.alignment = TextAlignmentOptions.Center; text.color = new Color(1, .85f, .28f); text.rectTransform.sizeDelta = new Vector2(34, 7);
        }
        static void PlaceTippedLoadOnTerrace(TrailerBody load, float top)
        {
            float bottom = float.PositiveInfinity;
            foreach (var shape in load.GetComponentsInChildren<Collider>(true))
            {
                if (!shape.enabled || shape.isTrigger || shape.GetComponentInParent<Rigidbody>(true) != load.Body) continue;
                if (shape is MeshCollider mesh && mesh.sharedMesh != null)
                    foreach (var vertex in mesh.sharedMesh.vertices) bottom = Mathf.Min(bottom, mesh.transform.TransformPoint(vertex).y);
                else if (shape is BoxCollider box)
                {
                    Vector3 half = box.size * .5f;
                    float extent = Mathf.Abs(box.transform.TransformVector(Vector3.right * half.x).y) + Mathf.Abs(box.transform.TransformVector(Vector3.up * half.y).y) + Mathf.Abs(box.transform.TransformVector(Vector3.forward * half.z).y);
                    bottom = Mathf.Min(bottom, box.transform.TransformPoint(box.center).y - extent);
                }
            }
            if (float.IsInfinity(bottom)) throw new InvalidOperationException("The original solid native trailer body is required.");
            load.transform.position += Vector3.up * (top + .025f - bottom);
        }
        static void RequireStopped()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped compiled Editor required.");
        }
    }
}
#endif
