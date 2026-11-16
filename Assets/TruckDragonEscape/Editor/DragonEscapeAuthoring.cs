#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Assets.CoreCollapse.Scripts.Structural;
using LWS.InterstateHauler;
using LWS.TruckDragonKingdom;
using LWS.TruckDragonKingdom.Editor;
using LWS.TruckHoardHeist.Editor;
using LWS.TruckSports.Editor;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonEscape.Editor
{
    public static class DragonEscapeAuthoring
    {
        public const string Folder = "TruckDragonEscape";
        public const string Scene = "Assets/TruckDragonEscape/Scenes/TruckDragonEscape.unity";
        public const string PrivateFragments = "Assets/TruckDragonEscape/PrivateLicensedFragments";
        const string Environment = "Assets/TruckDragonKingdom/PrivateLicensedEnvironment/";
        const string Village = Environment + "Plugins/Tidal Flask Studios/Fantastic Village Pack/prefabs/props/";
        const string Cliff = Environment + "Plugins/Polyart Studio/HandpaintedEnvironmentVol1/Meshes/Prefab/Cliffs/";
        [Serializable] public sealed class SourceRecord { public string sourcePrefab, sourceSha256; public int sourceMeshes, fragments; }
        [Serializable] public sealed class HazardRecord { public string id, intactPrefab, fragmentsPrefab; public float roadWidth; public string[] edges; public SourceRecord[] sources; public string[] fragmentMeshes; }

        [MenuItem("Truck Sports/Dragon Kingdom/Create NEW Dragon Escape")]
        public static void Create()
        {
            var match = DragonKingdomModeAuthoring.Begin<DragonEscapeMatch>(Folder, 2, new[] { "dragon-inferno", "dragon-fire" });
            Install(match); DragonKingdomModeAuthoring.Finish(match, Folder);
        }
        public static void Install(DragonEscapeMatch match)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || match == null || match.world == null || match.routeHazards.Length != 0)
                throw new InvalidOperationException("Stopped compiled Editor and new original shared-world Escape composition required.");
            foreach (string sub in new[] { "Scenes", "Prefabs", "Materials" }) Directory.CreateDirectory("Assets/" + Folder + "/" + sub);
            Directory.CreateDirectory(PrivateFragments); AssetDatabase.Refresh();
            var plans = match.world.definition.closurePlans;
            if (plans.Length != 4 || plans.Select(plan => plan.id).Distinct(StringComparer.Ordinal).Count() != 4)
                throw new InvalidOperationException("Exactly four unique authored world closure plans are required.");
            var root = new GameObject("Actual CoreCollapse kingdom route hazards").transform; root.SetParent(match.transform, false);
            var hazards = new List<DragonRouteHazard>();
            foreach (var plan in plans)
            {
                if (plan.edgeIds == null || plan.edgeIds.Length != 1) throw new InvalidOperationException("Each initial physical hazard must have one exact authored edge site.");
                var edge = match.world.definition.graph.edges.Find(value => value.edgeId == plan.edgeIds[0]);
                if (edge == null || edge.samples.Count < 3) throw new InvalidOperationException("Physical hazard has no real sampled road: " + plan.id);
                int index = Mathf.Clamp(Mathf.RoundToInt((edge.samples.Count - 1) * (plan.id == "harbor-market-collapse" ? .8f : .5f)), 1, edge.samples.Count - 2);
                var sample = edge.samples[index];
                Vector3 forward = Vector3.ProjectOnPlane(sample.forward, Vector3.up).normalized;
                if (forward.sqrMagnitude < .5f) throw new InvalidOperationException("Actual road direction required: " + edge.edgeId);
                float width = edge.laneWidthMeters * edge.laneCount + edge.leftShoulderWidthMeters + edge.rightShoulderWidthMeters;
                var hazardRoot = new GameObject(plan.id).transform; hazardRoot.SetParent(root, false);
                hazardRoot.position = sample.position; hazardRoot.rotation = Quaternion.LookRotation(forward);
                var hazard = hazardRoot.gameObject.AddComponent<DragonRouteHazard>(); hazard.id = plan.id; hazard.blockedEdges = (string[])plan.edgeIds.Clone();
                if (plan.edgeIds[0] != EdgeFor(plan.id)) throw new InvalidOperationException("Closure identity and actual road site disagree: " + plan.id);
                hazard.intactPrefab = BuildStructure(plan.id, width, hazardRoot);
                hazard.warning = new GameObject("Actual incoming dragon warning glow").AddComponent<Light>();
                hazard.warning.transform.SetParent(hazardRoot, false); hazard.warning.transform.localPosition = new Vector3(0, 7, 0);
                hazard.warning.type = LightType.Point; hazard.warning.color = new Color(1, .25f, .06f); hazard.warning.range = 45; hazard.warning.intensity = 7; hazard.warning.enabled = false;
                hazard.collapseVelocityChange = plan.id == "burning-forest-bridge" ? Vector3.zero : new Vector3(-Mathf.Sign(Bounds(hazard.intactPrefab).center.x) * 10, 1, 0);
                ValidateInitialRoadClearance(hazard, sample.position, width, forward);
                hazards.Add(hazard);
            }
            match.routeHazards = hazards.ToArray(); EditorUtility.SetDirty(match); AssetDatabase.SaveAssets();
        }

        static GameObject BuildStructure(string id, float roadWidth, Transform site)
        {
            string folder = "Assets/" + Folder + "/Prefabs/" + id;
            Directory.CreateDirectory(folder); Directory.CreateDirectory(PrivateFragments + "/" + id); AssetDatabase.Refresh();
            string intactPath = folder + "/Actual owned structure.prefab", brokenPath = folder + "/Same-model physical fragments.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(intactPath);
            string manifestPath = PrivateFragments + "/" + id + "/actual-source-and-fragments.json";
            if (existing != null)
            {
                if (!File.Exists(manifestPath) || AssetDatabase.LoadAssetAtPath<GameObject>(brokenPath) == null)
                    throw new InvalidOperationException("Incomplete prior hazard authoring must be repaired explicitly: " + id);
                var receipt = JsonUtility.FromJson<HazardRecord>(File.ReadAllText(manifestPath));
                if (receipt == null || receipt.id != id || Mathf.Abs(receipt.roadWidth-roadWidth) > .01f || receipt.edges == null || !receipt.edges.SequenceEqual(new[]{EdgeFor(id)}) || receipt.fragmentMeshes == null || receipt.fragmentMeshes.Length < 2 || receipt.fragmentMeshes.Any(path=>AssetDatabase.LoadAssetAtPath<Mesh>(path)==null))
                    throw new InvalidOperationException("Prior authored hazard no longer matches this real route geometry: " + id);
                return existing;
            }
            var intact = new GameObject(id + " · actual owned intact structure");
            var broken = new GameObject(id + " · same-model preauthored collapse");
            var records = new List<SourceRecord>();
            try
            {
                if (id == "village-treefall")
                {
                    var tree = Owned(intact.transform, Environment + "Fantastic Ancient Forest/Prefabs/Trees/P_ENV_tree_ancient_01.prefab", records);
                    FitHeight(tree, 30); var b = Bounds(tree); tree.transform.localPosition += new Vector3(roadWidth * .5f + b.extents.x + 1.5f - b.center.x, -b.min.y, -b.center.z); PlaceOnActualSupport(tree,site);
                }
                else if (id == "burning-forest-bridge")
                {
                    var bridge = Owned(intact.transform, Village + "P_PROP_bridge_wood_04.prefab", records);
                    var b = Bounds(bridge); if (b.size.z > b.size.x) bridge.transform.rotation = Quaternion.Euler(0, 90, 0);
                    b = Bounds(bridge); bridge.transform.localScale *= (roadWidth + 10) / Mathf.Max(.01f, b.size.x); b = Bounds(bridge);
                    bridge.transform.position += new Vector3(-b.center.x, 10 - b.min.y, -b.center.z);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        var pier = Owned(intact.transform, Cliff + "RockFormation_04.prefab", records); FitDimensions(pier, new Vector3(4, 11, 5));
                        b = Bounds(pier); pier.transform.position += new Vector3(side * (roadWidth * .5f + 3.5f) - b.center.x, -b.min.y, -b.center.z); PlaceOnActualSupport(pier,site,false);
                        b=Bounds(pier); FitDimensions(pier,new Vector3(4,Mathf.Max(2,11-b.min.y),5));
                        b=Bounds(pier); pier.transform.position+=Vector3.up*(11-b.max.y);
                    }
                }
                else if (id == "volcanic-rockfall")
                {
                    var rock = Owned(intact.transform, Cliff + "RockFormation_04.prefab", records); FitHeight(rock, 24);
                    var b = Bounds(rock); rock.transform.position += new Vector3(roadWidth * .5f + b.extents.x + 1.5f - b.center.x, -b.min.y, -b.center.z); PlaceOnActualSupport(rock,site);
                }
                else if (id == "harbor-market-collapse")
                {
                    var cart = Owned(intact.transform, Village + "P_PROP_cart_01.prefab", records); FitHeight(cart, 7);
                    var b = Bounds(cart); cart.transform.position += new Vector3(roadWidth * .5f + b.extents.x + 1.5f - b.center.x, -b.min.y, -b.center.z); PlaceOnActualSupport(cart,site);
                }
                else throw new InvalidOperationException("No audited owned structure is assigned to closure " + id);

                var wood = SportsEventAuthoring.Mat(Folder, "Actual wood fracture interior", new Color(.24f, .105f, .035f));
                var stone = SportsEventAuthoring.Mat(Folder, "Actual stone fracture interior", new Color(.25f, .23f, .2f));
                var contact = SportsEventAuthoring.Contact(Folder, "Physical bounded dragon debris", .65f, .06f);
                var filters = intact.GetComponentsInChildren<MeshFilter>(true).Where(filter => filter.sharedMesh != null && filter.GetComponent<MeshRenderer>() != null).ToArray();
                if (filters.Length < 1 || filters.Length > 8) throw new InvalidOperationException("Audit a bounded whole-model visible mesh set before fragmenting " + id);
                var fragmentMeshes = new List<string>(); int total = 0;
                for (int index = 0; index < filters.Length; index++)
                {
                    var filter = filters[index];
                    int sourceIndex=Enumerable.Range(0,intact.transform.childCount).Single(child=>filter.transform.IsChildOf(intact.transform.GetChild(child)));
                    bool isStone=records[sourceIndex].sourcePrefab.StartsWith(Cliff,StringComparison.Ordinal);
                    var sourceCollider = filter.gameObject.AddComponent<MeshCollider>(); sourceCollider.sharedMesh = filter.sharedMesh; sourceCollider.sharedMaterial = contact;
                    var fragments = HoardHeistAuthoring.CreateOwnedFragments(filter.gameObject, broken.transform, PrivateFragments + "/" + id,
                        id + " mesh " + index, isStone ? stone : wood, contact, isStone ? 180 : 65, filters.Length <= 4 ? 2 : 1);
                    foreach (var fragment in fragments)
                    {
                        fragment.gameObject.SetActive(true); fragment.isKinematic = false; fragment.useGravity = true;
                        fragment.linearDamping = .12f; fragment.angularDamping = .1f; fragment.maxAngularVelocity = 10;
                        fragment.maxLinearVelocity = 45;
                        var meshes = fragment.GetComponentsInChildren<MeshFilter>(true).Select(value=>value.sharedMesh).Concat(fragment.GetComponentsInChildren<MeshCollider>(true).Select(value=>value.sharedMesh)).Distinct().ToArray();
                        if (meshes.Length == 0 || meshes.Any(mesh=>mesh==null || !AssetDatabase.GetAssetPath(mesh).StartsWith(PrivateFragments + "/", StringComparison.Ordinal))) throw new InvalidOperationException("Persist the actual licensed fragment geometry privately.");
                        fragmentMeshes.AddRange(meshes.Select(AssetDatabase.GetAssetPath)); total++;
                    }
                    records[sourceIndex].fragments+=fragments.Length;
                }
                if (total < 2 || total > 32) throw new InvalidOperationException("Each physical hazard requires2–32 bounded actual fragments: " + id);
                var brokenPrefab = PrefabUtility.SaveAsPrefabAsset(broken, brokenPath);
                var support = intact.AddComponent<Support>(); support.ClearSupports();
                typeof(Support).GetField("onCollapse", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(support, new Support.OnCollapseEvent());
                var core = intact.AddComponent<CollapserPrefrag>(); var config = new SerializedObject(core);
                config.FindProperty("startingHp").floatValue = 100; config.FindProperty("collapsedPrefab").objectReferenceValue = brokenPrefab; config.ApplyModifiedPropertiesWithoutUndo();
                var saved = PrefabUtility.SaveAsPrefabAsset(intact, intactPath);
                var manifest = new HazardRecord { id = id, intactPrefab = intactPath, fragmentsPrefab = brokenPath, roadWidth=roadWidth, edges = new[] { EdgeFor(id) }, sources = records.ToArray(), fragmentMeshes = fragmentMeshes.Distinct().ToArray() };
                File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true));
                AssetDatabase.SaveAssets(); return saved;
            }
            finally { UnityEngine.Object.DestroyImmediate(intact); UnityEngine.Object.DestroyImmediate(broken); }
        }
        static string EdgeFor(string id) => id == "village-treefall" ? "village-castle" : id == "burning-forest-bridge" ? "forest-castle" : id == "volcanic-rockfall" ? "caverns-volcano" : "castle-harbor";

        static GameObject Owned(Transform parent, string path, List<SourceRecord> records)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (prefab == null) throw new InvalidOperationException("Actual dependency-closed owned hazard source required: " + path);
            var result = (GameObject)PrefabUtility.InstantiatePrefab(prefab); PrefabUtility.UnpackPrefabInstance(result, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction); result.transform.SetParent(parent, false);
            var lods = result.GetComponentsInChildren<LODGroup>(true);
            foreach (var lod in lods)
            {
                var levels = lod.GetLODs(); var keep = new HashSet<Renderer>(levels.Length == 0 ? Array.Empty<Renderer>() : levels[0].renderers);
                foreach (var renderer in lod.GetComponentsInChildren<Renderer>(true))
                    if (!keep.Contains(renderer))
                    {
                        var filter = renderer.GetComponent<MeshFilter>(); if (filter != null) UnityEngine.Object.DestroyImmediate(filter);
                        UnityEngine.Object.DestroyImmediate(renderer);
                    }
                UnityEngine.Object.DestroyImmediate(lod);
            }
            foreach (var component in result.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(component);
            foreach (var body in result.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(body);
            foreach (var script in result.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(script);
            foreach (var renderer in result.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!renderer.enabled) { var filter = renderer.GetComponent<MeshFilter>(); if (filter != null) UnityEngine.Object.DestroyImmediate(filter); UnityEngine.Object.DestroyImmediate(renderer); continue; }
                renderer.sharedMaterials = renderer.sharedMaterials.Select(material=>HoardHeistAuthoring.OwnedUrpMaterial(material,Folder)).ToArray();
            }
            records.Add(new SourceRecord { sourcePrefab = path, sourceSha256 = Digest(File.ReadAllBytes(path)), sourceMeshes = result.GetComponentsInChildren<MeshFilter>(true).Length });
            return result;
        }
        [Serializable] sealed class SupportEvidence
        {public string model,site,status,support;public Vector3 prototypeFoot,worldFoot,physicalPoint,normal;public string[] candidates;}
        static void SupportReceipt(GameObject model,Transform site,string status,Vector3 prototypeFoot,Vector3 worldFoot,RaycastHit hit,List<string> candidates)
        {
            var evidence=new SupportEvidence{model=model.name,site=site.name,status=status,support=hit.collider!=null?hit.collider.name:null,prototypeFoot=prototypeFoot,worldFoot=worldFoot,physicalPoint=hit.point,normal=hit.normal,candidates=candidates.ToArray()};
            Debug.Log("ESCAPE_ACTUAL_SUPPORT "+JsonUtility.ToJson(evidence));string[] args=System.Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at<0||at+1>=args.Length)return;string folder=Path.Combine(Path.GetFullPath(args[at+1]),"EscapeHazardSupport");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,site.name+"-"+model.name.Replace('/','_')+".json"),JsonUtility.ToJson(evidence,true));
        }
        static void PlaceOnActualSupport(GameObject model,Transform site,bool allowRoadsideRelocation=true)
        {
            // BuildStructure uses an unparented identity prototype so generated
            // intact/fragments retain the same site-local authored coordinates.
            // Renderer.bounds are world coordinates at this identity origin;
            // exactly ONE site.TransformPoint maps the foot to the real road.
            var prototype=model.transform.root;
            if(prototype.position!=Vector3.zero||prototype.rotation!=Quaternion.identity||prototype.localScale!=Vector3.one||Vector3.Dot(site.up,Vector3.up)<.999f)throw new InvalidOperationException("Identity local hazard prototype and actual horizontal road frame required.");
            var b=Bounds(model);var originalFoot=new Vector3(b.center.x,b.min.y,b.center.z);var evidence=new List<string>();Physics.SyncTransforms();
            float[] along=allowRoadsideRelocation?new[]{0f,-6f,6f,-12f,12f,-18f,18f}:new[]{0f};
            foreach(float z in along)for(int side=0;side<(allowRoadsideRelocation?2:1);side++)
            {
                // Mirror only the placement, never the source geometry. The
                // entire fitted crown/rock/cart remains outside the same road
                // corridor on either side; bridge piers stay under the bridge.
                var foot=new Vector3(side==0?originalFoot.x:-originalFoot.x,originalFoot.y,originalFoot.z+z);Vector3 world=site.TransformPoint(foot);
                var all=Physics.RaycastAll(world+Vector3.up*60,Vector3.down,120,~0,QueryTriggerInteraction.Ignore).Where(hit=>!hit.transform.IsChildOf(model.transform)&&hit.collider.GetComponentInParent<LWS.TruckSports.SportsEventTruck>()==null).OrderBy(hit=>Mathf.Abs(hit.point.y-site.position.y)).ToArray();
                var supported=all.Where(hit=>hit.normal.y>.7f&&Mathf.Abs(hit.point.y-site.position.y)<=20).ToArray();
                evidence.Add("side="+side+" along="+z+" prototypeFoot="+foot+" worldFoot="+world+" hits=["+string.Join(";",all.Take(8).Select(hit=>hit.collider.name+" y="+hit.point.y+" normal="+hit.normal+" roadHeightDelta="+Mathf.Abs(hit.point.y-site.position.y)))+"]");
                if(supported.Length==0)continue;var contact=supported[0];
                model.transform.position+=(foot-originalFoot)+Vector3.up*(contact.point.y-world.y);
                SupportReceipt(model,site,"ACCEPTED",foot,world,contact,evidence);return;
            }
            SupportReceipt(model,site,"REJECTED_ALL",originalFoot,site.TransformPoint(originalFoot),default,evidence);
            throw new InvalidOperationException("Real authored roadside terrain support required for "+model.name+" at originalfoot="+site.TransformPoint(originalFoot)+"; no bounded same-road placement has upward support within20verticalm. Preserve physical gates and inspect EscapeHazardSupport receipt.");
        }
        static void FitHeight(GameObject model, float height)
        { var b = Bounds(model); model.transform.localScale *= height / Mathf.Max(.01f, b.size.y); }
        static void FitDimensions(GameObject model, Vector3 size)
        { var b = Bounds(model); model.transform.localScale = Vector3.Scale(model.transform.localScale, new Vector3(size.x / Mathf.Max(.01f, b.size.x), size.y / Mathf.Max(.01f, b.size.y), size.z / Mathf.Max(.01f, b.size.z))); }
        static Bounds Bounds(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<MeshRenderer>(true); if (renderers.Length == 0) throw new InvalidOperationException("Actual visible source meshes required.");
            var bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds); return bounds;
        }
        static void ValidateInitialRoadClearance(DragonRouteHazard hazard, Vector3 roadPoint, float width, Vector3 forward)
        {
            var preview = (GameObject)PrefabUtility.InstantiatePrefab(hazard.intactPrefab, hazard.transform); preview.transform.localPosition = Vector3.zero; preview.transform.localRotation = Quaternion.identity;
            try
            {
                Physics.SyncTransforms();
                var overlaps = Physics.OverlapBox(roadPoint + Vector3.up * 4.5f, new Vector3(width * .5f - .5f, 3.5f, 6), Quaternion.LookRotation(forward), ~0, QueryTriggerInteraction.Ignore);
                if (overlaps.Any(shape => shape.transform.IsChildOf(preview.transform))) throw new InvalidOperationException("The original road must be physically clear before the warned dragon collapse: " + hazard.id);
            }
            finally { UnityEngine.Object.DestroyImmediate(preview); }
        }
        static string Digest(byte[] bytes) { using (var hash = SHA256.Create()) return string.Concat(hash.ComputeHash(bytes).Select(value => value.ToString("x2"))); }
    }
}
#endif
