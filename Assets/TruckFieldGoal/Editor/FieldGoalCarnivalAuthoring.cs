using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LWS.TruckFieldGoal.Editor
{
    public static class FieldGoalCarnivalAuthoring
    {
        const string Folder = "Assets/TruckFieldGoal/CarnivalPresentation";
        const string RootName = "Carnival Field Goal curved bowl and recorded scoreboard";
        const float Rise = 1.15f, Step = 2.25f, Opening = 38;
        [MenuItem("Truck Sports/Field Goal/Install Missing Carnival Bowl And Replay")]
        public static void InstallMissingPresentation()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped, compiled Editor required.");
            var scene = SceneManager.GetSceneByPath(FieldGoalAuthoring.Scene); bool opened = !scene.isLoaded;
            if (scene.isLoaded && scene.isDirty) throw new InvalidOperationException("Save existing Field Goal scene edits first.");
            if (opened) scene = EditorSceneManager.OpenScene(FieldGoalAuthoring.Scene, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects(); var match = roots.SelectMany(root => root.GetComponentsInChildren<FieldGoalMatch>(true)).Single();
                if (match.carnivalPresentation != null || roots.SelectMany(root => root.GetComponentsInChildren<FieldGoalCarnivalPresentation>(true)).Any())
                { Debug.Log("FIELD_GOAL_CARNIVAL_PRESERVED existing custom presentation retained."); return; }
                if (roots.Any(root => root.name == RootName)) throw new InvalidOperationException("Inspect the preserved unbound Carnival root before reuse.");
                var board = match.stadiumBoard; var crowd = match.GetComponent<FieldGoalCrowd>();
                if (board == null || board.screen == null || board.fieldAssembly == null || crowd == null || crowd.batches == null || crowd.nearFans == null || match.replay == null || match.replay.sources.Length != 3 || match.replay.ghosts.Length != 3)
                    throw new InvalidOperationException("Existing accepted field, scoreboard, crowd and three-source real replay required.");
                var stands = roots.SelectMany(root => root.GetComponentsInChildren<MeshRenderer>(true)).Where(renderer => renderer.name == "Union Field Grandstand tier").ToArray();
                if (stands.Length != 8 || stands.Any(stand => !stand.enabled || stand.GetComponent<MeshFilter>()?.sharedMesh == null || stand.GetComponent<MeshFilter>().sharedMesh.name != "Cube" || stand.GetComponents<Collider>().Length != 0))
                    throw new InvalidOperationException("The eight original render-only straight stands must be intact; custom geometry is preserved.");
                var sorted = new[] { stands.Where(stand => stand.transform.position.x < 0).OrderBy(stand => stand.transform.position.y).ToArray(), stands.Where(stand => stand.transform.position.x > 0).OrderBy(stand => stand.transform.position.y).ToArray() };
                if (sorted.Any(side => side.Length != 4)) throw new InvalidOperationException("Expected the original two sets of four stand tiers.");
                for (int side = 0; side < 2; side++) for (int tier = 0; tier < 4; tier++)
                {
                    var point = sorted[side][tier].transform.position;
                    if (Mathf.Abs(Mathf.Abs(point.x) - (55.25f + 14.45f * tier)) > .75f || Mathf.Abs(point.z - 30) > .75f || Mathf.Abs(point.y - (2.6775f + 2.55f * tier)) > .75f)
                        throw new InvalidOperationException("Preserve designer stand placement; this explicit upgrade supports only the audited straight layout.");
                }
                if (crowd.batches.Any(batch => batch.fans == null || batch.seats == null || batch.fans.Length != batch.seats.Length || batch.fans.Any(fan => fan == null)) || crowd.nearFans.Any(fan => fan == null))
                    throw new InvalidOperationException("Preserve the complete existing crowd seat bindings.");
                // Historical decor can retain the same display name. Bind only
                // the audience explicitly referenced by this match's crowd.
                var audienceRoots = crowd.batches.SelectMany(batch => batch.fans).Select(fan => fan.transform.parent).Distinct().ToArray();
                if (audienceRoots.Length != 1 || audienceRoots[0] == null || crowd.nearFans.Any(fan => !fan.transform.IsChildOf(audienceRoots[0])))
                    throw new InvalidOperationException("The explicitly bound posed and animated crowd must share one owned audience ancestor; preserve custom or split bindings.");
                var audience = audienceRoots[0];
                var fans = crowd.batches.SelectMany(batch => batch.fans).Select(fan => fan.transform).Concat(crowd.nearFans.Select(fan => ActorRoot(fan.transform, audience))).Distinct().ToArray();
                Debug.Log("FIELD_GOAL_CARNIVAL_BOUND_AUDIENCE root=" + audience.name + " instance=" + audience.GetInstanceID() + " referencedPeople=" + fans.Length + "; unrelated same-name historical decor retained.");
                var superseded = FindSupersededAudience(roots, audience, stands);
                string physicsBefore = PhysicsFingerprint(roots);
                // Plan every original seat before writing any scene or asset.
                var positions = new Vector3[fans.Length]; var rotations = new Quaternion[fans.Length];
                Vector3 center = new Vector3(0, 0, 30 - FieldGoalBoardAuthoring.FieldLength * .5f);
                for (int i = 0; i < fans.Length; i++)
                {
                    var stand = stands.OrderBy(item => SeatDistance(item.transform.InverseTransformPoint(fans[i].position))).First();
                    var local = stand.transform.InverseTransformPoint(fans[i].position); int side = stand.transform.position.x < 0 ? 0 : 1; int tier = Array.IndexOf(sorted[side], stand);
                    int row = tier % 2 * 4 + Mathf.Clamp(Mathf.RoundToInt(Mathf.InverseLerp(-.36f, .36f, local.z) * 3), 0, 3); int deck = tier / 2;
                    float along = Mathf.InverseLerp(-.47f, .47f, local.x), angle = side == 1 ? Mathf.Lerp(Opening, 180, along) : Mathf.Lerp(360 - Opening, 180, along);
                    positions[i] = center + Point(angle, 45 + deck * 16 + (row + .5f) * Step, 77 + deck * 16 + (row + .5f) * Step, 3 + deck * 15 + row * Rise + .025f);
                    rotations[i] = Quaternion.LookRotation(Vector3.ProjectOnPlane(center - positions[i], Vector3.up), Vector3.up);
                }
                foreach (string name in new[] { "Owned lower curved seating deck.asset", "Owned upper curved seating deck.asset" })
                    if (AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/" + name) != null) throw new InvalidOperationException("Inspect preserved unbound owned stadium mesh before reuse: " + name);
                Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
                var owned = new GameObject(RootName); SceneManager.MoveGameObjectToScene(owned, scene);
                var presentation = owned.AddComponent<FieldGoalCarnivalPresentation>(); presentation.match = match; presentation.replacedStraightStands = stands; presentation.remappedCrowd = fans;
                var bowl = new GameObject("Owned stepped stadium bowl — two decks and scoreboard opening").transform; bowl.SetParent(owned.transform, false); bowl.position = center; presentation.bowl = bowl;
                presentation.curvedDecks = new MeshFilter[2];
                for (int deck = 0; deck < 2; deck++)
                {
                    string name = deck == 0 ? "Owned lower curved seating deck" : "Owned upper curved seating deck";
                    var mesh = Deck(deck); mesh.name = name; AssetDatabase.CreateAsset(mesh, Folder + "/" + name + ".asset");
                    var part = new GameObject(name); part.transform.SetParent(bowl, false); presentation.curvedDecks[deck] = part.AddComponent<MeshFilter>(); presentation.curvedDecks[deck].sharedMesh = mesh;
                    part.AddComponent<MeshRenderer>().sharedMaterial = stands[0].sharedMaterial;
                }
                for (int i = 0; i < fans.Length; i++) { fans[i].SetParent(bowl, true); fans[i].SetPositionAndRotation(positions[i], rotations[i]); EditorUtility.SetDirty(fans[i]); }
                foreach (var batch in crowd.batches) for (int i = 0; i < batch.fans.Length; i++) { var fan = batch.fans[i].transform; batch.seats[i] = Matrix4x4.TRS(fan.position, fan.rotation, fan.lossyScale); }
                foreach (var stand in stands) { stand.enabled = false; EditorUtility.SetDirty(stand); }
                InstallVideo(presentation, board);
                Retire(presentation, superseded, roots, physicsBefore);
                match.carnivalPresentation = presentation; EditorUtility.SetDirty(match); EditorUtility.SetDirty(crowd); EditorUtility.SetDirty(presentation);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                Debug.Log("FIELD_GOAL_CARNIVAL_SAVED actual curved two-deck owned geometry; original crowd remapped=" + fans.Length + "; reserved replay-only scoreboard video wing; existing field/endzones/runoff/posts/colliders/audio/lighting/primary camera and authoritative scoring retained. Native physical kick/render proof pending.");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        [MenuItem("Truck Sports/Field Goal/Retire Verified Superseded Crowd Rendering")]
        public static void RetireSupersededAudience()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped, compiled Editor required.");
            var scene = SceneManager.GetSceneByPath(FieldGoalAuthoring.Scene); bool opened = !scene.isLoaded;
            if (scene.isLoaded && scene.isDirty) throw new InvalidOperationException("Save existing Field Goal scene edits first.");
            if (opened) scene = EditorSceneManager.OpenScene(FieldGoalAuthoring.Scene, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects(); var match = roots.SelectMany(root => root.GetComponentsInChildren<FieldGoalMatch>(true)).Single(); var presentation = match.carnivalPresentation;
                if (presentation == null || presentation.authoredVersion != 1 || presentation.name != RootName) throw new InvalidOperationException("Explicit owned Carnival bowl bindings required; custom presentations retained.");
                if (presentation.supersededAudience != null) { Debug.Log("FIELD_GOAL_SUPERSEDED_AUDIENCE_PRESERVED recorded retirement/custom activation retained."); return; }
                var candidate = FindSupersededAudience(roots, null, presentation.replacedStraightStands);
                if (candidate == null) { Debug.Log("FIELD_GOAL_SUPERSEDED_AUDIENCE_NOT_FOUND no verified unowned duplicate was changed."); return; }
                Retire(presentation, candidate, roots, PhysicsFingerprint(roots)); EditorUtility.SetDirty(presentation);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        static GameObject FindSupersededAudience(GameObject[] roots, Transform boundAudience, MeshRenderer[] stands)
        {
            var eligible = new List<GameObject>();
            foreach (var root in roots.Where(root => root.name == "Union Field audience · shared Taxi characters" && root.activeSelf && root.transform != boundAudience))
            {
                var transforms = root.GetComponentsInChildren<Transform>(true);
                var posed = transforms.Where(part => part.name.StartsWith("Pooled instanced posed fan ", StringComparison.Ordinal)).ToArray();
                if (transforms.Length != 3862 || root.GetComponentsInChildren<MeshRenderer>(true).Length != 1715 || root.GetComponentsInChildren<Animator>(true).Length != 33 || posed.Length != 544 || root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length != 0 || root.GetComponentsInChildren<Collider>(true).Length != 0 || root.GetComponentsInChildren<Rigidbody>(true).Length != 0 || root.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                    continue;
                var audio = root.GetComponentsInChildren<AudioSource>(true);
                if (audio.Length != 1 || audio[0].playOnAwake || audio[0].loop || posed.Any(part => part.parent != root.transform)) continue;
                if (stands == null || stands.Length != 8 || stands.Any(stand => stand == null || stand.name != "Union Field Grandstand tier")) continue;
                if (posed.Any(part => !stands.Any(stand => OnTread(stand.transform.InverseTransformPoint(part.position))))) continue;
                var owned = new HashSet<UnityEngine.Object>();
                foreach (var part in transforms) { owned.Add(part.gameObject); foreach (var component in part.GetComponents<Component>()) if (component != null) owned.Add(component); }
                bool referenced = false;
                foreach (var component in roots.SelectMany(item => item.GetComponentsInChildren<Component>(true)).Where(component => component != null && !owned.Contains(component)))
                {
                    var serialized = new SerializedObject(component); var iterator = serialized.GetIterator();
                    while (iterator.Next(true)) if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.objectReferenceValue != null && owned.Contains(iterator.objectReferenceValue)) { referenced = true; break; }
                    if (referenced) break;
                }
                if (!referenced) eligible.Add(root);
            }
            if (eligible.Count > 1) throw new InvalidOperationException("Multiple historical crowds match the signature; preserve them for explicit inspection.");
            return eligible.Count == 1 ? eligible[0] : null;
        }
        static bool OnTread(Vector3 point) => Mathf.Abs(point.x) <= .51f && Mathf.Abs(point.z) <= .51f && point.y >= .45f && point.y <= .6f;
        static string PhysicsFingerprint(GameObject[] roots)
        {
            string bytes = string.Join("\n", roots.SelectMany(root => root.GetComponentsInChildren<Component>(true)).Where(component => component is Collider || component is Rigidbody).OrderBy(component => component.GetInstanceID()).Select(component => component.GetType().FullName + ":" + component.GetInstanceID() + ":" + EditorJsonUtility.ToJson(component)));
            using (var hash = System.Security.Cryptography.SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(bytes))).Replace("-", "");
        }
        static void Retire(FieldGoalCarnivalPresentation presentation, GameObject candidate, GameObject[] roots, string before)
        {
            if (candidate != null) { candidate.SetActive(false); presentation.supersededAudience = candidate.transform; EditorUtility.SetDirty(candidate); }
            string after = PhysicsFingerprint(roots);
            if (before != after)
            {
                if (candidate != null) { candidate.SetActive(true); presentation.supersededAudience = null; }
                throw new InvalidOperationException("Original Collider/Rigidbody serialized byte fingerprint changed; do not save this presentation upgrade.");
            }
            presentation.retirementPhysicsBefore = before; presentation.retirementPhysicsAfter = after;
            Debug.Log("FIELD_GOAL_CARNIVAL_PHYSICS_BYTES_PRESERVED sha256=" + before + " retiredRenderingOnly=" + (candidate != null) + "; all original components/data retained.");
        }
        static Transform ActorRoot(Transform actor, Transform parent) { while (actor.parent != null && actor.parent != parent) actor = actor.parent; return actor; }
        static float SeatDistance(Vector3 point) => Mathf.Abs(point.y - .505f) + Mathf.Max(0, Mathf.Abs(point.x) - .5f) * 5 + Mathf.Max(0, Mathf.Abs(point.z) - .5f) * 5;
        static Vector3 Point(float degrees, float x, float z, float y) { float angle = degrees * Mathf.Deg2Rad; return new Vector3(Mathf.Sin(angle) * x, y, Mathf.Cos(angle) * z); }
        static Mesh Deck(int deck)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) { int at = vertices.Count; vertices.AddRange(new[] { a, b, c, d }); triangles.AddRange(new[] { at, at + 1, at + 2, at, at + 2, at + 3 }); }
            for (int row = 0; row < 8; row++) for (int segment = 0; segment < 144; segment++)
            {
                float a = Mathf.Lerp(Opening, 360 - Opening, segment / 144f), b = Mathf.Lerp(Opening, 360 - Opening, (segment + 1) / 144f);
                float x = 45 + deck * 16 + row * Step, z = 77 + deck * 16 + row * Step, y = 3 + deck * 15 + row * Rise;
                Quad(Point(a, x, z, y), Point(a, x + Step, z + Step, y), Point(b, x + Step, z + Step, y), Point(b, x, z, y));
                Quad(Point(a, x, z, y - Rise), Point(a, x, z, y), Point(b, x, z, y), Point(b, x, z, y - Rise));
            }
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        static void InstallVideo(FieldGoalCarnivalPresentation presentation, FieldGoalStadiumBoard board)
        {
            var match = presentation.match;
            var camera = SportsEventAuthoring.Camera("Field Goal read-only recorded scoreboard camera"); camera.transform.SetParent(presentation.transform, true); camera.target.SetParent(presentation.transform, true); camera.aim.SetParent(presentation.transform, true); camera.rig.transform.SetParent(presentation.transform, true);
            camera.view.tag = "Untagged"; camera.view.enabled = false; camera.view.depth = -20; camera.view.cullingMask &= ~(1 << 5); camera.view.allowHDR = false;
            var data = camera.view.GetUniversalAdditionalCameraData(); data.renderPostProcessing = false; data.renderShadows = false; presentation.feedCamera = camera;
            var panel = new GameObject("Reserved scoreboard instant replay display", typeof(RectTransform), typeof(Canvas)); panel.layer = 5; panel.transform.SetParent(board.fieldAssembly, false);
            panel.transform.localPosition = new Vector3(0, 59.5f, 12.25f); panel.transform.localScale = Vector3.one * .04f;
            var rect = panel.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(960, 540);
            var canvas = panel.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = match.cameraRig.view; presentation.videoCanvas = canvas;
            var back = SportsUIAuthoring.Rect("Replay video backing", panel.transform, new Vector2(960, 540), Vector2.zero); back.AddComponent<Image>().color = new Color(.006f, .016f, .025f); back.GetComponent<Image>().raycastTarget = false;
            var graphic = SportsUIAuthoring.Rect("Actual recorded ghost RenderTexture", panel.transform, new Vector2(832, 468), new Vector2(0, -20)); presentation.video = graphic.AddComponent<RawImage>(); presentation.video.raycastTarget = false;
            presentation.caption = SportsUIAuthoring.Text("Actual attempt replay caption", panel.transform, "INSTANT REPLAY", new Vector2(930, 40), new Vector2(0, 245), 31); presentation.caption.overflowMode = TextOverflowModes.Ellipsis;
            foreach (var child in panel.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 5;
            var housing = board.fieldAssembly.Find("Fabricated scoreboard housing");
            var bezel = new GameObject("Owned scoreboard replay wing bezel"); bezel.transform.SetParent(board.fieldAssembly, false); bezel.transform.localPosition = new Vector3(0, 59.5f, 13.2f); bezel.transform.localScale = new Vector3(40, 23.2f, 1.2f);
            bezel.AddComponent<MeshFilter>().sharedMesh = housing.GetComponent<MeshFilter>().sharedMesh; bezel.AddComponent<MeshRenderer>().sharedMaterials = housing.GetComponent<MeshRenderer>().sharedMaterials;
            panel.SetActive(false);
        }
    }
}
