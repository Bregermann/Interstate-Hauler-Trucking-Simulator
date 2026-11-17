using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckExtremeHauling.Editor
{
    // Scene configuration only. NWH still owns the stand, suspension and hitch.
    public static class CargoLandingGearAuthoring
    {
        const string CargoScene = "Assets/TruckExtremeHauling/Scenes/CargoCrisis.unity";
        const float NativeStandLocalY = .544f;
        public const float RecoveryStandExtensionMetres = .045f;

        [MenuItem("Truck Sports/Alpha 0.10/Repair active Cargo Crisis landing gear")]
        public static void RepairActiveCargoLandingGear()
            => UpgradeActiveCargoLandingGear();

        public static int UpgradeActiveCargoLandingGear()
            => ConfigureActiveCargoLandingGear(RecoveryStandExtensionMetres);

        // The extension is editable at authoring time; there is no runtime
        // transform correction or automatic replacement of Inspector settings.
        public static int ConfigureActiveCargoLandingGear(float extensionMetres)
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before configuring Cargo Crisis landing gear.");
            if (float.IsNaN(extensionMetres) || float.IsInfinity(extensionMetres) || extensionMetres < 0 || extensionMetres > .1f)
                throw new ArgumentOutOfRangeException(nameof(extensionMetres), "Use a physical stand extension between zero and ten centimetres.");

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != CargoScene)
                throw new InvalidOperationException("Open the saved CargoCrisis scene before applying this sport-only configuration.");

            CargoCrisisMatch match = null;
            foreach (var candidate in UnityEngine.Object.FindObjectsByType<CargoCrisisMatch>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (candidate.gameObject.scene == scene) { match = candidate; break; }
            if (match == null || match.trailers == null || match.trailers.Length == 0)
                throw new InvalidOperationException("The active Cargo Crisis scene has no authored rescue trailers.");

            // Validate the entire edit before changing any instantiated geometry.
            foreach (var trailer in match.trailers)
            {
                var stand = trailer != null && trailer.Module != null ? trailer.Module.module.trailerStand : null;
                if (stand == null || stand.scene != scene || stand.transform.parent != trailer.transform || EditorUtility.IsPersistent(stand))
                    throw new InvalidOperationException("Expected each rescue trailer's existing direct-child native landing stand in this scene.");
            }

            int changed = 0;
            foreach (var trailer in match.trailers)
            {
                Transform stand = trailer.Module.module.trailerStand.transform;
                Vector3 position = stand.localPosition;
                float targetY = NativeStandLocalY - extensionMetres;
                if (Mathf.Abs(position.y - targetY) < .00001f) continue;

                // The recovered native trailer settles on its jacks and rear
                // suspension at root Y approximately -.030m. Its pin is then
                // approximately .932m high, versus the tractor's .994m hitch.
                // Extending these jacks 45mm changes the natural pitch about
                // the rear axles and raises the pin about 62mm under gravity.
                // Move the existing leg visuals and collider together; never
                // reposition the trailer body, kingpin or tractor attachment.
                Undo.RecordObject(stand, "Configure Cargo Crisis landing gear");
                position.y = targetY;
                stand.localPosition = position;
                EditorUtility.SetDirty(stand);
                changed++;
            }
            if (changed > 0) EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Cargo Crisis landing gear: " + changed + " instantiated stands configured; extension " + extensionMetres.ToString("F3") + "m. Scene saving and runtime acceptance remain explicit.");
            return changed;
        }
    }
}
