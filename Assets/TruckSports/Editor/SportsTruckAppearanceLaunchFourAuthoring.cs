using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LWS.TruckSkyTow;
using LWS.TruckGiantSwing;
using LWS.TruckFieldGoal;
using LWS.TruckBeerPong;

namespace LWS.TruckSports.Editor
{
    public static partial class SportsTruckAppearanceHostAuthoring
    {
        [MenuItem("Truck Sports/Customizer/Bind Open Sky Swing Field Goal Or Beer Pong")]
        public static void InstallOpenLaunchFourAppearance()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped compiled Editor required.");
            Scene scene = SceneManager.GetActiveScene();
            string[] allowed = { "TruckSkyTow", "TruckGiantSwing", "TruckFieldGoal", "TruckBeerPong" };
            if (!allowed.Contains(scene.name) || scene.path != "Assets/" + scene.name + "/Scenes/" + scene.name + ".unity") throw new InvalidOperationException("Open one of the four original launch venues explicitly.");
            var match = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SportsEventSession>(true)).Single();
            var catalog = AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");
            var original = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTruck.prefab");
            if (catalog == null || !catalog.Validate(out _) || original == null) throw new InvalidOperationException("Existing validated catalog and installed NWH visual source required.");
            SportsEventTruck[] trucks; Transform[][] equipment;
            if (match is SkyTowMatch sky)
            {
                if (sky.racers.Length != 4 || sky.review == null || sky.review.motion.sources.Length != 8) throw new InvalidOperationException("Four original Sky Tow owners and their eight-source review required.");
                trucks = sky.racers.Select(racer => racer.truck).ToArray(); equipment = new Transform[4][];
                for (int i = 0; i < 4; i++)
                {
                    var racer = sky.racers[i]; var sail = racer.transform.Find("Functional aerodynamic tow sail");
                    var straps = racer.transform.Cast<Transform>().Where(child => child.name == "Tow sail strap").ToArray();
                    if (racer.slot != i || racer.match != sky || racer.boat == null || racer.tether == null || racer.tether.connectedBody != racer.boat || racer.cable == null || racer.propeller == null || sail == null || straps.Length != 2 || sky.review.motion.sources[i * 2] != racer.transform || sky.review.motion.sources[i * 2 + 1] != racer.boat.transform)
                        throw new InvalidOperationException("Preserve original Sky owner, tow, sail, straps, boat and physical-root replay sources.");
                    equipment[i] = new[] { racer.boat.transform, racer.cable.transform, sail }.Concat(straps).ToArray();
                }
            }
            else if (match is SwingMatch swing)
            {
                trucks = new[] { swing.truck }; equipment = new[] { new[] { swing.rigVisual, swing.seat.transform, swing.pivot }.Concat(swing.cables).ToArray() };
                CheckLaunchRecord(swing.replay, swing.truck, 2);
            }
            else if (match is FieldGoalMatch field)
            {
                trucks = new[] { field.truck }; equipment = new[] { new[] { field.football.transform, field.boot.transform, field.tee, field.bootMechanism, field.posts } };
                CheckLaunchRecord(field.replay, field.truck, 3);
            }
            else if (match is PongMatch pong)
            {
                trucks = new[] { pong.truck }; equipment = new[] { pong.pads.Concat(pong.coils).Concat(pong.cups.Select(cup => cup.transform)).Concat(new[] { pong.table.transform, pong.floor.transform }).ToArray() };
                CheckLaunchRecord(pong.replay, pong.truck, 23);
            }
            else throw new InvalidOperationException("Unexpected launch owner.");
            var rolling = new Renderer[trucks.Length][];
            for (int i = 0; i < trucks.Length; i++)
            {
                var truck = trucks[i]; CheckVehicle(truck.transform, truck.visuals, truck.wheels);
                if (truck.session != match || equipment[i].Any(root => root == null || root == truck.visuals || root.IsChildOf(truck.visuals) || truck.visuals.IsChildOf(root))) throw new InvalidOperationException("Sport equipment must stay outside the exact body rendering subtree.");
                var actual = truck.visuals.GetComponentsInChildren<MeshFilter>(true);
                var expected = original.GetComponentsInChildren<MeshFilter>(true).Where(mesh => mesh.GetComponent<MeshRenderer>() != null).ToArray();
                if (actual.Length != 33 || expected.Length != 33 || actual.Any(mesh => mesh.GetComponent<MeshRenderer>() == null || mesh.sharedMesh == null || expected.Count(source => source.name == mesh.name && source.sharedMesh == mesh.sharedMesh) != 1))
                    throw new InvalidOperationException("Preserve custom geometry: exact original33 NWH mesh identities required.");
                var binding = truck.GetComponent<SportsWheelMeshBinding>(); string[] names = { "Whl HD FL", "Whl HD FR", "Whl HD RL", "Whl HD RR" };
                if (binding == null || binding.truck != truck || binding.meshes.Length != 4 || Enumerable.Range(0, 4).Any(wheel => binding.meshes[wheel] == null || binding.meshes[wheel].name != names[wheel] || !binding.meshes[wheel].IsChildOf(truck.visuals))) throw new InvalidOperationException("Preserve the existing explicit four-wheel visual binding; never invoke a physics fitter.");
                rolling[i] = WheelRenderers(truck.transform, truck.wheels, binding);
                if (rolling[i].Length != 4) throw new InvalidOperationException("Exactly four original rolling renderers required.");
            }
            // A partial/custom host setup is never completed by guessing. Existing
            // fully registered hosts are preserved, including their custom fits.
            if (trucks.Any(truck => truck.appearanceHost != null || truck.GetComponent<SportsTruckAppearanceHost>() != null))
            {
                if (trucks.All(truck => truck.appearanceHost != null && truck.GetComponents<SportsTruckAppearanceHost>().Length == 1 && truck.appearanceHost.presentationParent == truck.visuals && match.appearanceHosts.Contains(truck.appearanceHost))) return;
                throw new InvalidOperationException("Existing partial/custom appearance bindings retained for explicit review.");
            }
            var hosts = match.appearanceHosts.ToList();
            for (int i = 0; i < trucks.Length; i++)
            {
                var truck = trucks[i]; var host = Create(truck.transform, truck.visuals, truck.wheels, rolling[i], catalog);
                host.participantSlot = match is SkyTowMatch ? i : -1;
                host.protectedRoots = equipment[i]; host.modeOwnedSlots = new[] { SportsTruckAppearanceSlots.Dashboard };
                foreach (var fit in host.compatibleBodies) fit.notice += " Original launch equipment and physical envelope are retained. This venue has no dashboard view; that saved accessory remains available in other sports.";
                truck.appearanceHost = host; hosts.Add(host); EditorUtility.SetDirty(host); EditorUtility.SetDirty(truck);
            }
            match.appearanceHosts = hosts.ToArray();
            match.appearanceFraming = match.appearanceFraming.Concat(scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SportsBroadcastFraming>(true)).Where(frame => frame.session == match)).Distinct().ToArray();
            EditorUtility.SetDirty(match); EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("SPORTS_LAUNCH_FOUR_APPEARANCE_BOUND " + scene.name + " actors=" + trucks.Length + "; explicit original rendering only. Caller owns scene save; native proof pending.");
        }
        static void CheckLaunchRecord(SportsMotionReplay replay, SportsEventTruck truck, int expected)
        {
            if (replay == null || replay.sources.Length != expected || replay.ghosts.Length != expected || replay.sources[0] != truck.visuals || replay.ghosts.Any(root => root == null)) throw new InvalidOperationException("Exact original stopped-record source/root bindings required.");
        }
    }
}
