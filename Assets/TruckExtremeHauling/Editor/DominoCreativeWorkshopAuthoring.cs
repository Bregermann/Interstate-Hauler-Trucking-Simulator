using System;
using LWS.TruckSports.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckExtremeHauling.Editor
{
    public static class DominoCreativeWorkshopAuthoring
    {
        [MenuItem("Truck Sports/Alpha 0.10/Install active Domino creative workshop")]
        public static void InstallActiveWorkshop()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                scene.path != "Assets/TruckExtremeHauling/Scenes/DominoDerby.unity")
                throw new InvalidOperationException("Open the saved Domino Derby scene outside Play Mode.");

            var match = UnityEngine.Object.FindAnyObjectByType<DominoDerbyMatch>();
            if (match == null || match.gameObject.scene != scene)
                throw new InvalidOperationException("The original Domino Derby authority is required.");
            if (match.creativeWorkshop != null) return;

            const string materials = "Assets/TruckExtremeHauling/Materials/";
            var deck = AssetDatabase.LoadAssetAtPath<Material>(materials + "Freight tower gunmetal.mat");
            var stripes = AssetDatabase.LoadAssetAtPath<Material>(materials + "Freight safety orange.mat");
            var contact = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(materials + "Freight concrete.asset");
            if (deck == null || stripes == null || contact == null)
                throw new InvalidOperationException("The existing freight ramp materials are required.");

            var workshop = new GameObject("Creative chain ramp and branch lanes");
            Undo.RegisterCreatedObjectUndo(workshop, "Install Domino creative workshop");
            workshop.transform.SetParent(match.transform, false);
            for (int lane = 0; lane < match.trucks.Length; lane++)
            {
                Vector3 start = match.trailers[lane * match.piecesPerPlayer].transform.position;
                Vector3 first = new Vector3(start.x + 14, -.16f, start.z + 10);
                Vector3 top = first + new Vector3(0, 1.5f, 22);
                Vector3 last = top + new Vector3(0, -1.5f, 24);
                AddDeck(workshop.transform, deck, stripes, contact, first, top, lane, "ascent");
                AddDeck(workshop.transform, deck, stripes, contact, top, last, lane, "descent");
            }
            Undo.RecordObject(match, "Bind Domino creative workshop");
            match.creativeWorkshop = workshop;
            workshop.SetActive(false);
            EditorUtility.SetDirty(match);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void AddDeck(Transform parent, Material material, Material stripes, PhysicsMaterial contact,
            Vector3 a, Vector3 b, int lane, string name)
        {
            Vector3 span = b - a;
            float pitch = -Mathf.Atan2(span.y, span.z) * Mathf.Rad2Deg;
            Quaternion slope = Quaternion.Euler(pitch, 0, 0);
            var deck = SportsEventAuthoring.Box("Lane " + (lane + 1) + " creative " + name,
                (a + b) * .5f, new Vector3(9, .32f, span.magnitude), material, contact, parent);
            deck.transform.rotation = slope;

            // Painted guides sit outside the central placement corridor and
            // are visual only. The original concrete and ramp carry impacts.
            for (int side = -1; side <= 1; side += 2)
            {
                var strip = SportsEventAuthoring.Box("Edge guide", (a + b) * .5f +
                    slope * new Vector3(4.25f * side, .17f, 0),
                    new Vector3(.16f, .02f, span.magnitude - .4f), stripes, null, parent);
                strip.transform.rotation = slope;
                if (strip.TryGetComponent<Collider>(out var paintCollider))
                    UnityEngine.Object.DestroyImmediate(paintCollider);
            }
        }
    }
}
