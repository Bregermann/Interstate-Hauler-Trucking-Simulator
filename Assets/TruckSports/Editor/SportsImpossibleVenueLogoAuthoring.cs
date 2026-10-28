using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckGuessThat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LWS.TruckSports.Editor
{
    // Deliberately only two suitable existing title displays. Other new sports'
    // objective signs, custom venue names and Orchestra title are untouched.
    public static class SportsImpossibleVenueLogoAuthoring
    {
        public const string OwnedName = "Owned Impossible original venue logo";
        public static readonly Vector3 GuessCenter = new Vector3(9.8f, 17.2f, .98f);
        public static readonly Vector2 GuessSize = new Vector2(2.4f, 1.2f);
        static readonly string[] Scenes = { "TruckBakeOff", "TruckGuessThat" };
        sealed class Target
        {
            public Transform parent;
            public TextMesh title;
            public Vector3 position;
            public Vector2 size;
            public GameObject existing;
        }
        static void Require(bool pass, string reason)
        { if (!pass) throw new InvalidOperationException("Preserve original/custom venue: " + reason); }
        static bool Near(Vector3 a, Vector3 b) => Vector3.Distance(a, b) < .0001f;
        static string MaterialPath(string name) => "Assets/TruckSports/Art/Logos/" + name + " Impossible venue.mat";
        static bool Allowed(Scene scene) => scene.path == "Assets/" + scene.name + "/Scenes/" + scene.name + ".unity" && Scenes.Contains(scene.name);
        static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        public static void InstallSavedBakeAndGuessVenueLogos()
        {
            Require(!EditorApplication.isPlaying && !EditorApplication.isCompiling, "stopped compiled Editor required");
            for (int i = 0; i < SceneManager.sceneCount; i++) Require(!SceneManager.GetSceneAt(i).isDirty, "save pending scene edits first");
            var previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                // All source signatures and logos are checked before either saved
                // scene is changed. No build/editor-init hook calls this method.
                foreach (string name in Scenes)
                {
                    string path = "Assets/" + name + "/Scenes/" + name + ".unity";
                    Require(File.Exists(path), "actual saved scene required: " + name);
                    Validate(EditorSceneManager.OpenScene(path, OpenSceneMode.Single));
                }
                foreach (string name in Scenes)
                {
                    var scene = EditorSceneManager.OpenScene("Assets/" + name + "/Scenes/" + name + ".unity", OpenSceneMode.Single);
                    InstallOpenScene(scene);
                    Require(EditorSceneManager.SaveScene(scene), "could not save explicit owned logo addition");
                }
            }
            finally
            {
                if (previous.Any(item => item.isLoaded && item.isActive && !string.IsNullOrEmpty(item.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
        }

        public static void InstallOpenScene(Scene scene)
        {
            Require(!EditorApplication.isPlaying && !EditorApplication.isCompiling, "stopped compiled Editor required");
            var targets = Validate(scene);
            var original = Critical(scene);
            var logo = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TruckSports/Art/Logos/" + scene.name + ".png");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(scene.name));
            if (material == null)
            {
                Require(!File.Exists(MaterialPath(scene.name)) && !File.Exists(MaterialPath(scene.name) + ".meta"), "unknown/partial material asset");
                var shader = Shader.Find("Universal Render Pipeline/Unlit"); Require(shader != null, "installed URP Unlit foundation required");
                material = new Material(shader) { name = scene.name + " Impossible venue", mainTexture = logo.texture, color = Color.white, renderQueue = 3000 };
                material.SetFloat("_Surface", 1); material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); material.SetFloat("_ZWrite", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); AssetDatabase.CreateAsset(material, MaterialPath(scene.name));
            }
            foreach (var target in targets)
            {
                if (target.existing != null) continue;
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); quad.name = OwnedName;
                // Only the NEW primitive's collider is removed. Original physics
                // is fingerprinted before/after and never configured here.
                UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
                quad.layer = target.parent.gameObject.layer;
                quad.transform.SetParent(target.parent, false);
                quad.transform.SetPositionAndRotation(target.position, Quaternion.identity);
                quad.transform.localScale = new Vector3(target.size.x / target.parent.lossyScale.x,
                    target.size.y / target.parent.lossyScale.y, 1 / target.parent.lossyScale.z);
                var renderer = quad.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
            Require(original.SequenceEqual(Critical(scene)), "an original title/camera/physics field changed unexpectedly");
            Validate(scene); EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("IMPOSSIBLE_VENUE_LOGOS " + scene.name + " count=" + targets.Length + "; render-only original PNG; native projection and visual acceptance remain required.");
        }

        static Target[] Validate(Scene scene)
        {
            Require(scene.IsValid() && Allowed(scene), "explicit Bake/Guess saved scene only");
            var sessions = All<SportsEventSession>(scene); Require(sessions.Length == 1, "one original sport authority required");
            var logo = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/TruckSports/Art/Logos/" + scene.name + ".png");
            Require(logo != null && Mathf.Abs(logo.rect.width / logo.rect.height - 2) < .001f, "existing original2:1 PNG logo required");
            var targets = new List<Target>();
            if (scene.name == "TruckBakeOff")
            {
                Require(sessions[0].GetType().FullName == "LWS.TruckBakeOff.BakeMatch", "original Bake authority required");
                var title = All<TextMesh>(scene).Single(item => item.name == "Original television show title");
                Title(title, "TRUCK BAKE-OFF", new Vector3(0,12,95), 1.3f, new Color(1,.84f,.42f));
                Require(title.transform.parent == null, "original root title retained");
                var backdrop = All<Transform>(scene).Single(item => item.name == "Giant television bake-off title backdrop");
                Require(Near(backdrop.position, new Vector3(0,8,97)) && Near(backdrop.localScale, new Vector3(180,16,1)) &&
                    Quaternion.Angle(backdrop.rotation, Quaternion.identity) < .001f && backdrop.GetComponent<Collider>() == null &&
                    backdrop.GetComponent<Rigidbody>() == null && backdrop.GetComponents<MonoBehaviour>().Length == 0, "original render-only studio backdrop signature");
                var renderer = backdrop.GetComponent<MeshRenderer>(); Require(renderer != null &&
                    AssetDatabase.GetAssetPath(renderer.sharedMaterial) == "Assets/TruckBakeOff/Materials/Warm studio backdrop burgundy.mat", "original owned backdrop material binding");
                targets.Add(new Target { parent = backdrop, title = title, position = new Vector3(-45.5f,11,94.98f), size = new Vector2(14,7) });
            }
            else
            {
                var match = sessions[0] as GuessMatch; Require(match != null && match.boardFocus.Length == 4, "four actual original owner boards required");
                var backdrop = All<Transform>(scene).Single(item => item.name == "Teal paneled room backdrop");
                Require(Near(backdrop.position,new Vector3(90,15,13)) && Near(backdrop.lossyScale,new Vector3(270,30,1)) &&
                    Quaternion.Angle(backdrop.rotation,Quaternion.identity)<.001f, "original30m room backdrop capacity required");
                Require(match.answerLabel != null && match.answerLabel.rectTransform.anchoredPosition == new Vector2(0,420) &&
                    match.answerLabel.rectTransform.sizeDelta == new Vector2(1300,45), "original live answer row signature required");
                Require(match.boardCamera != null && match.boardCamera.view != null && Mathf.Abs(match.boardCamera.view.fieldOfView-48) < .001f &&
                    match.boardCamera.follow != null && Near(match.boardCamera.follow.m_FollowOffset, new Vector3(0,6,-31)), "original fixed board lens/offset required; cameras are never changed");
                Require(GuessProposalFitsOriginalHeader(GuessCenter, GuessSize), "proposed emblem cannot intersect original board/answer/header envelope");
                for (int owner = 0; owner < 4; owner++)
                {
                    var board = match.boardFocus[owner]; Require(board != null && board.name == "Owner " + owner + " private mechanical board" &&
                        Near(board.localPosition, new Vector3(owner*60,0,0)) && Near(board.lossyScale, Vector3.one) && Quaternion.Angle(board.rotation,Quaternion.identity)<.001f, "exact original board root/owner/pose required");
                    var title = board.Find("Mechanical board title")?.GetComponent<TextMesh>(); Require(title != null, "original board header required");
                    Title(title, "GUESS THAT TRUCK", new Vector3(0,17.3f,1), .26f, new Color(1,.88f,.47f));
                    Require(title.transform.parent == board, "original header owner binding preserved");
                    targets.Add(new Target { parent = board, title = title, position = board.TransformPoint(GuessCenter), size = GuessSize });
                }
            }
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(scene.name));
            if (material == null) Require(!File.Exists(MaterialPath(scene.name)) && !File.Exists(MaterialPath(scene.name)+".meta"), "unknown/partial material asset");
            if (material != null) Require(material.shader != null && material.shader.name == "Universal Render Pipeline/Unlit" && material.mainTexture == logo.texture &&
                material.color == Color.white && material.renderQueue == 3000 && material.GetFloat("_Surface") == 1 && material.GetFloat("_SrcBlend") == (float)BlendMode.SrcAlpha &&
                material.GetFloat("_DstBlend") == (float)BlendMode.OneMinusSrcAlpha && material.GetFloat("_ZWrite") == 0 && material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"), "unknown/custom venue material preserved");
            foreach (var target in targets)
            {
                var titleBounds=target.title.GetComponent<MeshRenderer>().bounds;
                var titleRect=Rect.MinMaxRect(titleBounds.min.x,titleBounds.min.y,titleBounds.max.x,titleBounds.max.y);
                var proposed=new Rect(target.position.x-target.size.x/2,target.position.y-target.size.y/2,target.size.x,target.size.y);
                Require(!titleRect.Overlaps(proposed), scene.name+"/"+target.title.name+" actual original glyphRect="+titleRect.ToString("F6")+" overlaps proposedLogoRect="+proposed.ToString("F6")+"; reviewed placement required");
                var found = target.parent.Find(OwnedName); target.existing = found != null ? found.gameObject : null;
                if (found == null) continue;
                var renderer = found.GetComponent<MeshRenderer>();
                Require(Near(found.position,target.position) && Quaternion.Angle(found.rotation,Quaternion.identity)<.001f && Near(found.lossyScale,new Vector3(target.size.x,target.size.y,1)) &&
                    found.gameObject.layer == target.parent.gameObject.layer && found.gameObject.activeSelf && found.childCount == 0 && found.GetComponents<Component>().Length == 3 &&
                    found.GetComponent<MeshFilter>()?.sharedMesh == Resources.GetBuiltinResource<Mesh>("Quad.fbx") && renderer != null && renderer.enabled && material != null &&
                    renderer.sharedMaterial == material && renderer.shadowCastingMode == ShadowCastingMode.Off && !renderer.receiveShadows &&
                    renderer.lightProbeUsage == LightProbeUsage.Off && renderer.reflectionProbeUsage == ReflectionProbeUsage.Off, "unknown/custom existing logo object preserved");
            }
            Require(All<Transform>(scene).Count(item => item.name == OwnedName) == targets.Count(item => item.existing != null), "unknown owned-name object outside approved targets");
            return targets.ToArray();
        }

        static void Title(TextMesh title, string words, Vector3 position, float size, Color color)
        {
            Require(title != null && title.text == words && Near(title.transform.localPosition,position) && Near(title.transform.localScale,Vector3.one) &&
                Quaternion.Angle(title.transform.localRotation,Quaternion.identity)<.001f && title.transform.childCount == 0 && title.gameObject.activeSelf &&
                title.font == Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") && title.fontSize == 64 && Mathf.Abs(title.characterSize-size)<.0001f &&
                title.anchor == TextAnchor.MiddleCenter && title.alignment == TextAlignment.Center && title.fontStyle == FontStyle.Normal && title.richText &&
                ((Color32)title.color).Equals((Color32)color) && title.GetComponent<MeshRenderer>() != null && title.GetComponent<MeshRenderer>().enabled &&
                title.GetComponent<MeshRenderer>().sharedMaterial == title.font.material &&
                title.GetComponents<MonoBehaviour>().All(item => item is SportsDepthTestedWorldText), "original static title text/pose/font/render binding required");
        }
        static string[] Critical(Scene scene) => All<Component>(scene).Where(item => item is Collider || item is Rigidbody || item is Joint || item is Camera ||
            item is SportsEventCamera || item is TextMesh || item is SportsEventSession || item is SportsEventMenus).OrderBy(item => item.GetInstanceID())
            .Select(item => item.GetInstanceID()+":"+EditorJsonUtility.ToJson(item)).ToArray();

        public static bool GuessProposalFitsOriginalHeader(Vector3 center, Vector2 size)
        {
            if (size.x <= 0 || size.y <= 0 || center.x-size.x/2 < -12.5f || center.x+size.x/2 > 12.5f || center.y-size.y/2 < 16.5f) return false;
            var forward = new Vector3(0,-6,31).normalized; var up = Vector3.Cross(forward,Vector3.right);
            foreach (float x in new[] {-1f,1f}) foreach (float y in new[] {-1f,1f})
            {
                var delta = center + new Vector3(x*size.x/2,y*size.y/2,0)-new Vector3(0,14,-31);
                float z = Vector3.Dot(delta,forward), tangent = Mathf.Tan(24*Mathf.Deg2Rad);
                if (z <= 0) return false;
                float vx=.5f+delta.x/(2*z*tangent*(16f/9)), vy=.5f+Vector3.Dot(delta,up)/(2*z*tangent);
                // Existing answer row spans .8681.. .9097; rail starts .9463.
                // Keep a real gap below BOTH; never move/hide their UI.
                if (vx < .02f || vx > .98f || vy < .02f || vy > .865f) return false;
            }
            return true;
        }
    }
}
