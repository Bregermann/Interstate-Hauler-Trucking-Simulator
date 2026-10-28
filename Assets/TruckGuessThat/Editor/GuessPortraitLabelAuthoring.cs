using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckGuessThat.Editor
{
    public static class GuessPortraitLabelAuthoring
    {
        public static string Caption(int identity, string name)
        {
            int split = name.IndexOf(' ');
            if (split < 1) throw new InvalidOperationException("An original named hat/body identity is required.");
            return (identity + 1).ToString("00") + " · " + name.Substring(0, split) + "\n" + name.Substring(split + 1);
        }
        public static Bounds PortraitBounds(Transform portrait)
        {
            bool found = false; Bounds result = default;
            foreach (var renderer in portrait.GetComponentsInChildren<Renderer>(true).Where(renderer =>
                renderer.enabled && renderer.gameObject.activeInHierarchy && (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)))
            {
                var bounds = renderer.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var point = bounds.center + Vector3.Scale(bounds.extents, new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                    point = portrait.parent.InverseTransformPoint(point);
                    if (!found) { result = new Bounds(point, Vector3.zero); found = true; } else result.Encapsulate(point);
                }
            }
            if (!found || result.size.x <= 0 || result.size.y <= 0 || float.IsNaN(result.size.x) || float.IsInfinity(result.size.x))
                throw new InvalidOperationException("Actual finite portrait mesh bounds required: " + portrait.name);
            return result;
        }
        public static void FitOriginalPortrait(Transform portrait)
        {
            var bounds = PortraitBounds(portrait); FitPortrait(portrait, bounds);
        }
        public static void FitCaptionWidth(TextMesh label)
        {
            float width = label.GetComponent<Renderer>().bounds.size.x;
            if (width > 3.08f) label.characterSize *= 3.08f / width;
        }
        public static void FitSavedOwnedCaptionWidths()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped compiled Editor required.");
            for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Preserve unsaved scenes before the exact owned caption-width refinement.");
            var scene = EditorSceneManager.OpenScene(GuessAuthoring.Scene, OpenSceneMode.Single);
            var match = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<GuessMatch>(true)).Single();
            if (match.tiles.Length != 96) throw new InvalidOperationException("Original96 portrait captions required.");
            var labels = match.tiles.Select(tile => tile.hinge.Find("Truthful identity label")?.GetComponent<TextMesh>()).ToArray();
            for (int i = 0; i < labels.Length; i++)
                if (labels[i] == null || labels[i].text != Caption(match.tiles[i].identity, match.identities[match.tiles[i].identity].name) ||
                    Mathf.Abs(labels[i].characterSize - .048f) > .000001f ||
                    Vector3.Distance(labels[i].transform.localPosition, new Vector3(0, .37f, -.17f)) > .000001f ||
                    !((Color32)labels[i].color).Equals(new Color32(41, 20, 8, 255)))
                    throw new InvalidOperationException("Customized or already-fitted caption preserved.");
            foreach (var label in labels) { FitCaptionWidth(label); EditorUtility.SetDirty(label); }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save exact owned caption widths.");
        }
        static void FitPortrait(Transform portrait, Bounds original)
        {
            // Uniform fit of the existing render-only catalog composition. No
            // material, cosmetic, internal pose, identity, or collider changes.
            float scale = Mathf.Min(1, 3.1f / original.size.x, 2.25f / original.size.y);
            Vector3 pivot = portrait.localPosition;
            Vector3 lower = pivot + (original.min - pivot) * scale;
            Vector3 upper = pivot + (original.max - pivot) * scale;
            portrait.localScale *= scale;
            portrait.localPosition += new Vector3(-(lower.x + upper.x) * .5f, .85f - lower.y, 0);
        }
        public static void InstallSavedPortraitCaptions() { Install(false); }
        public static void RefineSavedPortraitFooterFit() { Install(true); }
        static void Install(bool refineOwnedCaption)
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped compiled Editor required.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Preserve unsaved scene changes before this explicit portrait-label correction.");
            var scene = EditorSceneManager.OpenScene(GuessAuthoring.Scene, OpenSceneMode.Single);
            var match = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<GuessMatch>(true)).Single();
            var tiles = match.tiles; if (tiles.Length != 96 || match.identities.Length != 24) throw new InvalidOperationException("Four original 24-candidate boards required.");
            var labels = tiles.Select(tile => tile.hinge.Find("Truthful identity label")?.GetComponent<TextMesh>()).ToArray();
            var portraits = tiles.Select(tile => Enumerable.Range(0, tile.hinge.childCount).Select(index => tile.hinge.GetChild(index))
                .Single(child => child.name.StartsWith("Real catalog portrait · ", StringComparison.Ordinal))).ToArray();
            var bounds = portraits.Select(PortraitBounds).ToArray();
            for (int i = 0; i < tiles.Length; i++)
            {
                var label = labels[i]; var tile = tiles[i]; var identity = match.identities[tile.identity];
                string expected = refineOwnedCaption ? Caption(tile.identity, identity.name) : (tile.identity + 1).ToString("00") + " · " + identity.name;
                if (label == null || label.text != expected ||
                    Mathf.Abs(label.characterSize - (refineOwnedCaption ? .05f : .055f)) > .000001f || label.fontSize != 64 ||
                    Vector3.Distance(label.transform.localPosition, new Vector3(0, refineOwnedCaption ? .37f : .23f, -.17f)) > .000001f ||
                    Quaternion.Angle(label.transform.localRotation, Quaternion.identity) > .001f ||
                    label.anchor != TextAnchor.MiddleCenter || label.alignment != TextAlignment.Center ||
                    !((Color32)label.color).Equals(new Color32(41, 20, 8, 255)))
                    throw new InvalidOperationException("Custom or already-corrected portrait label preserved: " + tile.name +
                        (label == null ? " missing text" : " text=" + label.text + " size=" + label.characterSize.ToString("R") +
                        " position=" + label.transform.localPosition.ToString("R") + " rgba=" + ((Color32)label.color)));
                if (Vector3.Distance(portraits[i].localPosition, new Vector3(0, .7f, -.65f)) > .00001f ||
                    Quaternion.Angle(portraits[i].localRotation, Quaternion.Euler(0, 155, 0)) > .001f ||
                    Vector3.Distance(portraits[i].localScale, Vector3.one * .42f) > .00001f ||
                    portraits[i].GetComponentsInChildren<Collider>(true).Length != 0 || portraits[i].GetComponentsInChildren<Rigidbody>(true).Length != 0)
                    throw new InvalidOperationException("Customized portrait composition preserved: " + portraits[i].name);
            }
            // Only caption layout and uniform render-only portrait fit change.
            // Identities, model composition, hinges, camera and physics remain.
            for (int i = 0; i < tiles.Length; i++)
            {
                labels[i].text = Caption(tiles[i].identity, match.identities[tiles[i].identity].name);
                labels[i].characterSize = .048f; labels[i].transform.localPosition = new Vector3(0, .37f, -.17f);
                FitCaptionWidth(labels[i]);
                FitPortrait(portraits[i], bounds[i]);
                EditorUtility.SetDirty(labels[i]); EditorUtility.SetDirty(portraits[i]);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save the explicit portrait-caption correction.");
            Debug.Log("GUESS_PORTRAIT_CAPTIONS 96 original identities wrapped into hat/body lines; native width/height and720/1080 visual verification remain required.");
        }
    }
}
