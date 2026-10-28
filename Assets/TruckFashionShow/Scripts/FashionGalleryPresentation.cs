using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Michsky.UI.Heat;
using LWS.TruckSports;
using LWS.TruckBowling;

namespace LWS.TruckFashionShow
{
    // Mode-owned, read-only context/layout and shot description. The original
    // FashionMatch.Present remains the sole camera writer; original actions,
    // strut/wardrobe/competition/profile authorities remain unchanged.
    [DefaultExecutionOrder(360)]
    public sealed class FashionGalleryPresentation : MonoBehaviour
    {
        public const int Version = 2;
        public FashionMatch match;
        public ButtonManager[] readyButtons = Array.Empty<ButtonManager>();
        public GameObject[] supersededFloatingSigns = Array.Empty<GameObject>();
        public TextMesh[] fixedSigns = Array.Empty<TextMesh>();
        public Light[] practicalLights = Array.Empty<Light>();
        public Transform galleryArt;
        public int revision = Version;
        public int HubShotDescriptions { get; private set; }
        readonly string[] priorCaptions = new string[4];
        readonly string[] labels = { "HAT BOUTIQUE", "FACE STUDIO", "WHEEL ATELIER", "MIRROR SALON", "DRESSING ROOM" };
        readonly List<Renderer>[] subjects = { new List<Renderer>(), new List<Renderer>(), new List<Renderer>(), new List<Renderer>() };
        readonly int[] revisions = { -1, -1, -1, -1 };
        RectTransform statusBacking;
        bool initialized;

        public string NearestShopContext(int owner)
        {
            if (match == null || owner < 0 || owner >= match.walkers.Length || match.walkers[owner] == null) return "FREE COSMETICS";
            Vector3 p = match.walkers[owner].Position; float nearest = float.PositiveInfinity; int index = -1;
            for (int i = 0; i < 5; i++)
            {
                var point = i == 4 ? match.dressing : match.shops[i]; if (point == null) continue;
                float distance = Vector3.ProjectOnPlane(p - point.position, Vector3.up).magnitude;
                if (distance < nearest) { nearest = distance; index = i; }
            }
            return index < 0 ? "FREE COSMETICS" : labels[index] + (nearest < 7 ? " / ACTION TO DRESS" : " / " + Mathf.CeilToInt(nearest) + "m");
        }

        void LateUpdate()
        {
            if (match == null || match.menus == null || readyButtons.Length != 4) return;
            ApplyLayout();
            bool hub = match.Live && match.Phase == FashionPhase.Hub && !match.WardrobeOpen && match.menus.Screen == SportsEventScreen.Play;
            for (int i = 0; i < 4; i++)
            {
                var button = readyButtons[i]; if (button == null) continue;
                if (!hub && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == button.gameObject) EventSystem.current.SetSelectedGameObject(null);
                button.gameObject.SetActive(hub);
                if (!hub) continue;
                bool ai = match.setup.players[i].source == BowlingSource.AI;
                string caption = "P" + (i + 1) + (match.OutfitReady[i] ? "  OUTFIT READY" : ai ? "  AI STYLING" : "  READY OUTFIT") + "\n" + NearestShopContext(i);
                if (caption != priorCaptions[i]) { priorCaptions[i] = caption; button.SetText(caption); }
                bool interactable = !ai && !match.OutfitReady[i]; button.isInteractable = interactable;
                var selectable = button.GetComponent<UnityEngine.UI.Button>(); if (selectable != null) selectable.interactable = interactable;
            }
        }

        public void ApplyLayout()
        {
            for (int i = 0; i < readyButtons.Length; i++)
            {
                var button = readyButtons[i]; if (button == null) continue;
                var rect = button.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0); rect.pivot = new Vector2(.5f, 0);
                rect.anchoredPosition = new Vector2(-697.5f + i * 465, 8); rect.sizeDelta = new Vector2(445, 70);
                button.autoFitContent = false; button.textSize = 24;
                foreach (var text in new TMP_Text[] { button.normalTextObj, button.highlightTextObj, button.disabledTextObj })
                {
                    if (text == null) continue; text.enableAutoSizing = true; text.fontSizeMin = 22; text.fontSizeMax = 24; text.fontSize = 24;
                    text.alignment = TextAlignmentOptions.Center; text.lineSpacing = -3; text.margin = new Vector4(3, 0, 3, 0); text.rectTransform.sizeDelta = new Vector2(427, 64);
                }
            }
            if (match == null || match.menus == null || match.menus.status == null) return;
            var status = match.menus.status; status.rectTransform.anchorMin = status.rectTransform.anchorMax = new Vector2(.5f, 0); status.rectTransform.pivot = new Vector2(.5f, 0);
            status.rectTransform.anchoredPosition = new Vector2(0, 86); status.rectTransform.sizeDelta = new Vector2(1500, 58);
            status.fontSizeMin = 22; status.fontSizeMax = 26;
            if (statusBacking == null) statusBacking = status.transform.parent.Find("Broadcast status backplate") as RectTransform;
            if (statusBacking != null)
            {
                statusBacking.anchorMin = status.rectTransform.anchorMin; statusBacking.anchorMax = status.rectTransform.anchorMax; statusBacking.pivot = status.rectTransform.pivot;
                statusBacking.anchoredPosition = status.rectTransform.anchoredPosition; statusBacking.sizeDelta = status.rectTransform.sizeDelta;
            }
            initialized = true;
        }

        public static bool ActualVisibleMesh(Renderer renderer)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.bounds.size.sqrMagnitude < .000001f) return false;
            Vector3 scale = renderer.transform.lossyScale; if (Mathf.Abs(scale.x * scale.y * scale.z) < .000001f) return false;
            if (renderer is SkinnedMeshRenderer skin) return skin.sharedMesh != null && skin.sharedMesh.vertexCount > 0;
            var filter = renderer.GetComponent<MeshFilter>(); return filter != null && filter.sharedMesh != null && filter.sharedMesh.vertexCount > 0;
        }

        public bool TryHubShot(out Vector3 focus, out Vector3 offset, out float fov)
        {
            focus = offset = Vector3.zero; fov = 48;
            if (match == null || match.trucks == null || match.trucks.Length != 4 || match.broadcast == null) return false;
            bool found = false; var all = new Bounds();
            for (int i = 0; i < 4; i++)
            {
                var truck = match.trucks[i]; if (truck == null) return false; var host = truck.appearanceHost;
                int revisionNow = host == null ? 0 : host.AppearanceRevision;
                if (!initialized || revisions[i] != revisionNow || subjects[i].Count == 0)
                {
                    revisions[i] = revisionNow; subjects[i].Clear();
                    if (host != null && host.Applied) subjects[i].AddRange(host.ExteriorRenderers);
                    else if (truck.visuals != null) subjects[i].AddRange(truck.visuals.GetComponentsInChildren<Renderer>(true));
                }
                bool owner = false;
                foreach (var renderer in subjects[i]) if (ActualVisibleMesh(renderer)) { if (!found) { all = renderer.bounds; found = true; } else all.Encapsulate(renderer.bounds); owner = true; }
                if (!owner) return false;
            }
            // Include local browsing floor around the actual four trucks, rather
            // than fitting the entire empty124x112m shell into every hub shot.
            all.Expand(new Vector3(10, 2, 12));
            Vector3 outward = new Vector3(0, .72f, -1).normalized; Quaternion rotation = Quaternion.LookRotation(-outward, Vector3.up);
            float aspect = match.broadcast.view != null ? match.broadcast.view.aspect : 16f / 9;
            FitHubBounds(all, rotation, fov, aspect, out focus, out offset);
            HubShotDescriptions++; return true;
        }

        public static void FitHubBounds(Bounds bounds, Quaternion rotation, float verticalFov, float aspect, out Vector3 focus, out Vector3 offset)
        {
            // Keep actual actors above the mode's three bottom HUD rows and
            // below the header, without another camera or a moving world root.
            const float minY = .23f, maxY = .92f, side = .06f;
            float ty = Mathf.Tan(verticalFov * Mathf.Deg2Rad * .5f), tx = ty * Mathf.Max(.1f, aspect), distance = 12;
            float low = 2 * minY - 1, high = 2 * maxY - 1, bias = minY + maxY - 1;
            var inverse = Quaternion.Inverse(rotation);
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 local = inverse * (corner - bounds.center);
                distance = Mathf.Max(distance, Mathf.Abs(local.x) / (tx * (1 - 2 * side)) - local.z, (local.y / ty - high * local.z) / (high - bias), (-local.y / ty + low * local.z) / (bias - low));
            }
            distance += 1; focus = bounds.center - rotation * Vector3.up * (bias * ty * distance); offset = rotation * Vector3.back * distance;
        }
    }
}
