using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LWS.TruckCornhole;

namespace LWS.TruckSports.Editor
{
    public static partial class SportsTruckAppearanceHostAuthoring
    {
        [MenuItem("Truck Sports/Customizer/Bind Open Cornhole Retained Owners")]
        public static void InstallOpenCornholeAppearance()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped compiled Editor required.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/TruckCornhole/Scenes/TruckCornhole.unity") throw new InvalidOperationException("Explicit original Cornhole scene required; caller owns open/save.");
            var match = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CornholeMatch>(true)).Single();
            var catalog = AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");
            var original = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTruck.prefab");
            if (catalog == null || !catalog.Validate(out _) || original == null || match.courses.Length != 4) throw new InvalidOperationException("Existing catalog, NWH source and four original courses required.");
            var trucks = match.courses.SelectMany(course => course.trucks).ToArray();
            if (trucks.Length != 16 || trucks.Distinct().Count() != 16) throw new InvalidOperationException("Sixteen separate original retained deliveries required.");
            var models = new Transform[16]; var rolling = new Renderer[16][]; var cabs = new Transform[4];
            var expected = original.GetComponentsInChildren<MeshFilter>(true).Where(mesh => mesh.GetComponent<MeshRenderer>() != null).ToArray();
            for (int i = 0; i < 4; i++)
            {
                var course = match.courses[i]; var look = course.chase.view.GetComponent<SportsLegacyCabLook>();
                if (course.index != i || course.match != match || course.trucks.Length != 4 || course.sharedTarget == null || look == null || look.cornhole != course.chase || look.cockpit == null || course.chase.interior.Length == 0) throw new InvalidOperationException("Preserve existing shared board, fixed owner and actual private cab.");
                cabs[i] = look.cockpit;
                if (cabs[i].gameObject.layer != CornholeSharedTarget.PrivateLayerStart + i) throw new InvalidOperationException("Original private cab layer required.");
                for (int delivery = 0; delivery < 4; delivery++)
                {
                    int index = i * 4 + delivery; var truck = course.trucks[delivery];
                    if (truck.course != course || truck.paint == null || truck.body == null || truck.shape == null) throw new InvalidOperationException("Original Cornhole actor bindings required.");
                    var model = truck.paint.transform.parent; CheckVehicle(truck.transform, model, truck.wheels);
                    if (model.GetComponentsInChildren<Collider>(true).Length != 0 || model.GetComponentsInChildren<Rigidbody>(true).Length != 0) throw new InvalidOperationException("Only the separate nonphysical miniature model may be composed.");
                    var meshes = model.GetComponentsInChildren<MeshFilter>(true); var tires = meshes.Where(mesh => mesh.name == "Rubber tire").ToArray(); var sourceMeshes = meshes.Where(mesh => mesh.name != "Rubber tire").ToArray();
                    if (sourceMeshes.Length != 33 || tires.Length != 4 || sourceMeshes.Any(mesh => mesh.sharedMesh == null || mesh.GetComponent<MeshRenderer>() == null || expected.Count(source => source.name == mesh.name && source.sharedMesh == mesh.sharedMesh) != 1) || tires.Any(mesh => mesh.sharedMesh == null || mesh.GetComponent<MeshRenderer>() == null || mesh.sharedMesh != tires[0].sharedMesh)) throw new InvalidOperationException("Exact33 original NWH mesh identities and four existing Rubber tire meshes required.");
                    string[] names = { "Whl HD FL", "Whl HD FR", "Whl HD RL", "Whl HD RR" };
                    rolling[index] = names.Select(name => sourceMeshes.Single(mesh => mesh.name == name).GetComponent<Renderer>()).Concat(tires.Select(mesh => mesh.GetComponent<Renderer>())).ToArray(); models[index] = model;
                }
            }
            if (trucks.Any(truck => truck.appearanceHost != null || truck.GetComponent<SportsTruckAppearanceHost>() != null))
            {
                if (trucks.All(truck => truck.appearanceHost != null && truck.GetComponents<SportsTruckAppearanceHost>().Length == 1 && truck.appearanceHost.participantSlot == truck.course.index) && match.courses.All(course => course.chase.appearanceCab != null)) return;
                throw new InvalidOperationException("Existing partial/custom Cornhole bindings retained for explicit review.");
            }
            for (int i = 0; i < 16; i++)
            {
                var truck = trucks[i]; var course = truck.course;
                // Rendererless model root only: new render-only children inherit
                // the already-established shared truck layer; collider/root layers stay.
                models[i].gameObject.layer = CornholeSharedTarget.TruckLayer;
                var host = Create(truck.transform, models[i], truck.wheels, rolling[i], catalog); host.participantSlot = course.index;
                host.protectedRoots = new[] { course.board, course.trampoline, course.launchEffect.transform, course.holeEffect.transform, cabs[course.index] };
                // The legacy Rubber tire cylinders were authored underneath the
                // normalized/offset FBX root using physical local coordinates.
                // They are hidden rolling art, not a valid chassis-fit envelope.
                var sourceParts=models[i].GetComponentsInChildren<MeshFilter>(true).Where(mesh=>mesh.name!="Rubber tire").Select(mesh=>mesh.GetComponent<Renderer>());
                host.compatibleBodies=catalog.bodies.Select(body=>Fit(body,LocalBounds(truck.transform,sourceParts),truck.transform,truck.wheels)).ToArray();
                foreach (var fit in host.compatibleBodies) fit.notice += " Cornhole retains its miniature art scale, original physical delivery and shared board.";
                truck.appearanceHost = host; EditorUtility.SetDirty(host); EditorUtility.SetDirty(truck);
            }
            for (int i = 0; i < 4; i++) { var binding = Cab(cabs[i]); match.courses[i].chase.appearanceCab = binding; EditorUtility.SetDirty(binding); EditorUtility.SetDirty(match.courses[i].chase); }
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("CORNHOLE_APPEARANCE_BOUND original16 actors/four stable course owners/private cabs; physics, shared target and existing timeline untouched. Caller saves; native proof pending.");
        }

        [MenuItem("Truck Sports/Customizer/Repair Owned Open Cornhole Miniature Fits")]
        public static void RepairOwnedOpenCornholeFits()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");
            var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/TruckCornhole/Scenes/TruckCornhole.unity")throw new InvalidOperationException("Explicit original Cornhole scene required; caller owns open/save.");
            var match=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<CornholeMatch>(true)).Single();var trucks=match.courses.SelectMany(course=>course.trucks).ToArray();
            var catalog=AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTruck.prefab");
            if(trucks.Length!=16||trucks.Distinct().Count()!=16||catalog==null||!catalog.Validate(out _)||prefab==null)throw new InvalidOperationException("Existing16 actors and original catalog/vehicle required.");
            var expected=prefab.GetComponentsInChildren<MeshFilter>(true).Where(mesh=>mesh.GetComponent<MeshRenderer>()!=null).ToArray();var updates=new System.Collections.Generic.List<Action>();int changed=0;
            foreach(var truck in trucks)
            {
                var host=truck.appearanceHost;var model=truck.paint!=null?truck.paint.transform.parent:null;
                if(host==null||model==null||host.presentationParent!=model||host.participantSlot!=truck.course.index||host.catalog!=catalog||!host.wheels.SequenceEqual(truck.wheels)||host.compatibleBodies.Length!=catalog.bodies.Length)throw new InvalidOperationException("Custom/partial Cornhole host retained; explicit original bindings required.");
                var meshes=model.GetComponentsInChildren<MeshFilter>(true);var source=meshes.Where(mesh=>mesh.name!="Rubber tire").ToArray();var rubber=meshes.Where(mesh=>mesh.name=="Rubber tire").ToArray();
                if(source.Length!=33||rubber.Length!=4||source.Any(mesh=>mesh.sharedMesh==null||mesh.GetComponent<MeshRenderer>()==null||expected.Count(original=>original.name==mesh.name&&original.sharedMesh==mesh.sharedMesh)!=1))throw new InvalidOperationException("Exact33 original licensed source meshes and4 retained legacy Rubber tires required.");
                var rolling=source.Where(mesh=>new[]{"Whl HD FL","Whl HD FR","Whl HD RL","Whl HD RR"}.Contains(mesh.name)).Select(mesh=>mesh.GetComponent<Renderer>()).Concat(rubber.Select(mesh=>mesh.GetComponent<Renderer>())).ToArray();
                if(rolling.Length!=8||!host.originalWheelRenderers.OrderBy(r=>r.GetInstanceID()).SequenceEqual(rolling.OrderBy(r=>r.GetInstanceID()))||!host.originalBodyRenderers.OrderBy(r=>r.GetInstanceID()).SequenceEqual(source.Select(mesh=>mesh.GetComponent<Renderer>()).Except(rolling).OrderBy(r=>r.GetInstanceID())))throw new InvalidOperationException("Original exact body/wheel renderer references changed; preserved for review.");
                var originalBounds=LocalBounds(truck.transform,source.Select(mesh=>mesh.GetComponent<Renderer>()));var legacyBounds=LocalBounds(truck.transform,source.Concat(rubber).Select(mesh=>mesh.GetComponent<Renderer>()));
                foreach(var body in catalog.bodies)
                {
                    var stored=host.compatibleBodies.Single(fit=>fit.bodyId==body.id);var before=Fit(body,legacyBounds,truck.transform,truck.wheels);var corrected=Fit(body,originalBounds,truck.transform,truck.wheels);
                    string notice="Visual fit uses this sport's existing four wheel positions and balanced chassis. Cornhole retains its miniature art scale, original physical delivery and shared board.";
                    if(stored.notice!=notice||!SameCornFitGeometry(stored,before)&&!SameCornFitGeometry(stored,corrected))throw new InvalidOperationException("Customized Cornhole fit retained: "+body.id);
                    if(SameCornFitGeometry(stored,corrected))continue;
                    // Preserve paint-slot overrides, notices, wheel mappings and every
                    // host/equipment reference. Only proven bad envelope values change.
                    updates.Add(()=>{stored.position=corrected.position;stored.scale=corrected.scale;for(int i=0;i<stored.wheelGeometry.Length;i++)stored.wheelGeometry[i].visualDiameter=corrected.wheelGeometry[i].visualDiameter;EditorUtility.SetDirty(host);});changed++;
                }
                Debug.Log("CORN_OWNED_FIT_ENVELOPE actor="+truck.name+" source33="+originalBounds+" legacyWithRubber="+legacyBounds+" physicalShape="+truck.shape.center+"/"+truck.shape.size);
            }
            // Validate all16 before modifying any owned serialized fit.
            foreach(var update in updates)update();if(changed>0)EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("CORN_OWNED_MINIATURE_FITS_REPAIRED fits="+changed+" actors=16; original physics, wheel controllers/containers, all37 source renderers, private cabs/UI/score preserved. Caller saves.");
        }
        static bool SameCornFitGeometry(SportsTruckAppearanceCompatibility a,SportsTruckAppearanceCompatibility b)
        {
            const float tolerance=.00002f;if(a.bodyId!=b.bodyId||Vector3.Distance(a.position,b.position)>tolerance||Vector3.Distance(a.scale,b.scale)>tolerance||Vector3.Distance(a.euler,b.euler)>tolerance||a.wheelGeometry==null||b.wheelGeometry==null||a.wheelGeometry.Length!=b.wheelGeometry.Length)return false;
            for(int i=0;i<a.wheelGeometry.Length;i++){var x=a.wheelGeometry[i];var y=b.wheelGeometry[i];if(x==null||y==null||x.physicalWheel!=y.physicalWheel||!x.rendererIndices.SequenceEqual(y.rendererIndices)||Vector3.Distance(x.center,y.center)>tolerance||Vector3.Distance(x.euler,y.euler)>tolerance||Mathf.Abs(x.diameter-y.diameter)>tolerance||Mathf.Abs(x.visualDiameter-y.visualDiameter)>tolerance)return false;}return true;
        }
    }
}
