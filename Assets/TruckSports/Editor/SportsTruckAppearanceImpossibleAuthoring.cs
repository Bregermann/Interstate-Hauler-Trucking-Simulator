using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckSports.Editor
{
    public static partial class SportsTruckAppearanceHostAuthoring
    {
        static readonly HashSet<string> ImpossibleScenes = new HashSet<string>(StringComparer.Ordinal)
        {
            "TruckTruckGoose", "TruckSubmarine", "TruckMoonMadness", "TruckPinataPanic",
            "TruckEggSpoonRace", "TruckTornadoRodeo", "TruckMechanicalBull", "TruckGuessThat",
            "TruckJenga", "TruckCamping", "TruckTeaParty", "TruckDentist", "TruckBallet",
            "TruckBakeOff", "TruckFashionShow", "TruckYoga", "TruckNestBuilding",
            "TruckSnowmanBuilding", "TruckArchaeology", "TruckOrchestra", "TruckPetPals"
        };

        // Explicit authoring for NEW expansion scenes only. The existing appearance
        // runtime, exact licensed facade bindings and fit calculation remain owners.
        // Never run this from Play, a normal build, or a legacy scene migration.
        public static void BindNewImpossibleScene(SportsEventSession match,
            SportsEventTruck[] trucks, Transform[] requiredEquipment = null)
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling || match == null)
                throw new InvalidOperationException("A stopped compiled Editor and explicit new match are required.");
            var scene = match.gameObject.scene;
            if (!scene.IsValid() || !ImpossibleScenes.Contains(scene.name))
                throw new InvalidOperationException("This author binds only explicitly named Impossible Games scenes.");
            if (trucks == null || trucks.Length == 0 || trucks.Length > 12 ||
                trucks.Any(t => t == null || t.gameObject.scene != scene || t.session != match) ||
                trucks.Distinct().Count() != trucks.Length || trucks.Select(t => t.slot).Distinct().Count() != trucks.Length)
                throw new InvalidOperationException("Provide the complete distinct fixed-owner tractor roster.");
            // Preserve every hand-authored/customized host. A mixed set is an error,
            // rather than a reason to silently replace its Inspector selections.
            if (trucks.Any(t => t.appearanceHost != null || t.GetComponent<SportsTruckAppearanceHost>() != null))
            {
                if (trucks.All(t => t.appearanceHost != null && t.appearanceHost.participantSlot == t.slot &&
                    t.GetComponents<SportsTruckAppearanceHost>().Length == 1 && (match.appearanceHosts ?? Array.Empty<SportsTruckAppearanceHost>()).Contains(t.appearanceHost)))
                    return;
                throw new InvalidOperationException("Existing partial/custom appearance bindings retained for review.");
            }
            var catalog = AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");
            var original = AssetDatabase.LoadAssetAtPath<GameObject>(FacadeSource);
            if (catalog == null || !catalog.Validate(out _) || original == null)
                throw new InvalidOperationException("Existing validated catalog and full NWH facade are required.");
            var views = match.GetComponent<SportsDrivingViews>();
            var equipment = (requiredEquipment ?? Array.Empty<Transform>()).ToList();
            if (views != null)
            {
                if (views.rally == null || !views.rally.trucks.SequenceEqual(trucks) ||
                    views.cabs == null || views.cabs.Length != 4 || views.cabs.Any(c => c == null))
                    throw new InvalidOperationException("Preserve the original explicit driving-view roster and four cabs.");
                equipment.AddRange(views.cabs);
            }
            if (equipment.Any(t => t == null || t.gameObject.scene != scene))
                throw new InvalidOperationException("Required equipment must be explicitly bound in this scene.");
            var bodies = new Renderer[trucks.Length][];
            var rolling = new Renderer[trucks.Length][];
            for (int i = 0; i < trucks.Length; i++)
            {
                var truck = trucks[i];
                if (truck.visuals != truck.transform || truck.tractorMotor == null || truck.body == null ||
                    truck.wheels == null || truck.wheels.Length != 4)
                    throw new InvalidOperationException("Use the existing full NWH Sports facade; physics is never synthesized here.");
                bodies[i] = FacadeRenderers(truck.transform, original.transform, FacadeBodyPaths);
                rolling[i] = FacadeRenderers(truck.transform, original.transform, FacadeWheelPaths);
                if (rolling[i].Any(renderer => !truck.wheels.Any(w => w != null && w.wheel.rotatingContainer != null &&
                    renderer.transform.IsChildOf(w.wheel.rotatingContainer))))
                    throw new InvalidOperationException("Four original NWH rolling containers are required.");
                if (equipment.Any(part => bodies[i].Concat(rolling[i]).Any(renderer =>
                    renderer.transform == part || renderer.transform.IsChildOf(part))))
                    throw new InvalidOperationException("Required equipment cannot contain the original tractor renderers.");
            }
            var hosts = new List<SportsTruckAppearanceHost>(match.appearanceHosts ?? Array.Empty<SportsTruckAppearanceHost>());
            for (int i = 0; i < trucks.Length; i++)
            {
                var truck = trucks[i];
                var host = truck.gameObject.AddComponent<SportsTruckAppearanceHost>();
                host.catalog = catalog; host.participantSlot = truck.slot; host.presentationParent = truck.transform;
                host.originalBodyRenderers = bodies[i]; host.originalWheelRenderers = rolling[i]; host.wheels = truck.wheels;
                host.protectedRoots = equipment.Distinct().ToArray();
                // Preserve exhaust and mode attachments, rather than treating them
                // as chassis geometry or trying to replace their physical owners.
                host.auxiliaryExteriorRenderers = truck.GetComponentsInChildren<Renderer>(true)
                    .Except(bodies[i]).Except(rolling[i]).ToArray();
                var bounds = LocalBounds(truck.transform, bodies[i].Concat(rolling[i]));
                host.compatibleBodies = catalog.bodies.Select(body => Fit(body, bounds, truck.transform, truck.wheels)).ToArray();
                truck.appearanceHost = host; hosts.Add(host);
                EditorUtility.SetDirty(host); EditorUtility.SetDirty(truck);
            }
            if (views != null)
                for (int i = 0; i < 4; i++) { var cab = Cab(views.cabs[i]); cab.drivingViews = views; cab.viewIndex = i; EditorUtility.SetDirty(cab); }
            match.appearanceHosts = hosts.ToArray();
            match.appearanceFraming = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<SportsBroadcastFraming>(true))
                .Where(framing => framing.session == match).ToArray();
            EditorUtility.SetDirty(match); EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("IMPOSSIBLE_APPEARANCE_BOUND " + scene.name + " actors=" + trucks.Length +
                "; existing facade meshes/NWH physics/profile/cab authorities retained; caller owns save and validation.");
        }
    }
}
