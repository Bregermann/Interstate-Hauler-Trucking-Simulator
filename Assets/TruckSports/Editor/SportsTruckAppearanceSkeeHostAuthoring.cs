using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckSkeeBall;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LWS.TruckSports.Editor
{
    public static partial class SportsTruckAppearanceHostAuthoring
    {
        [MenuItem("Truck Sports/Customizer/Bind Missing Four Turn Owned Skee Hosts")]
        public static void BindMissingSkeeTurnHosts()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");var catalog=AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");var source=AssetDatabase.LoadAssetAtPath<GameObject>(FacadeSource);if(catalog==null||!catalog.Validate(out _)||source==null)throw new InvalidOperationException("Existing validated catalog and installed GR3D source required.");const string path="Assets/TruckSkeeBall/Scenes/TruckSkeeBall.unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(scene.isLoaded&&scene.isDirty)throw new InvalidOperationException("Save original scene edits first.");if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var roots=scene.GetRootGameObjects();var match=roots.SelectMany(root=>root.GetComponentsInChildren<SkeeMatch>(true)).Single();if(match.courses.Length!=4||match.arenas.Length!=4||match.courses.Any(r=>r==null||r.truck==null)||match.courses.Select(r=>r.truck).Distinct().Count()!=4)throw new InvalidOperationException("Original four independent lanes required.");if((match.appearanceHosts?.Length??0)>0||match.courses.Any(r=>r.truck.appearanceHost!=null||r.truck.GetComponent<SportsTruckAppearanceHost>()!=null||r.appearanceCab!=null)){Debug.Log("SPORTS_SKEE_HOSTS_PRESERVED custom/partial turn binding.");return;}
                var tires=new Renderer[4][];var interiors=new Transform[4];
                for(int i=0;i<4;i++)
                {
                    var runner=match.courses[i];var truck=runner.truck;var model=truck.visuals;if(runner.match!=match||runner.lane!=i||runner.owner!=i||truck.session!=match||truck.slot!=i||model==null||model.name!="NWH GR3D visual geometry"||model.gameObject.layer!=0||truck.GetComponent<SportsFragmentRig>()!=null||runner.replay==null||runner.replay.sources.Length!=1||runner.replay.ghosts.Length!=1||runner.replay.sources[0]!=truck.transform||runner.target==null||runner.rings.Length!=7||runner.rings.Any(r=>r==null||r.GetComponent<MeshCollider>()==null)||runner.cabAnchor==null||runner.viewCamera==null||runner.look==null||runner.celebration==null)throw new InvalidOperationException("Original lane owner, root layer, whole-truck replay, seven real rings and cab/flight presentation required.");
                    CheckVehicle(truck.transform,model,truck.wheels);var body=ArenaOriginalRenderers(model,source.transform,FacadeBodyPaths);tires[i]=ArenaOriginalRenderers(model,source.transform,FacadeWheelPaths);var known=new HashSet<Renderer>(body.Concat(tires[i]));if(model.GetComponentsInChildren<Renderer>(true).Length!=33||truck.GetComponentsInChildren<Renderer>(true).Any(r=>!known.Contains(r))||known.Any(r=>r.gameObject.layer!=10+i)||model.GetComponentsInChildren<Collider>(true).Length!=0||model.GetComponentsInChildren<Rigidbody>(true).Length!=0||!new HashSet<Renderer>(WheelRenderers(truck.transform,truck.wheels,truck.GetComponent<SportsWheelMeshBinding>())).SetEquals(tires[i]))throw new InvalidOperationException("Original exact29+4 mesh identity, lane renderer layers and NWH wheel bindings required.");
                    if(model.Find("Sports Skee lane appearance")!=null)throw new InvalidOperationException("Preserve a custom/partial presentation node.");interiors[i]=runner.cabAnchor.Find("Driver seat interior");if(interiors[i]==null||interiors[i].gameObject.layer!=25+i||interiors[i].GetComponent<SportsTruckAppearanceCabBinding>()!=null||runner.viewCamera.cabSteering==null||!runner.viewCamera.cabSteering.transform.IsChildOf(interiors[i])||runner.replay.ghosts[0].GetComponentsInChildren<Renderer>(true).Any(r=>r.gameObject.layer!=10+i))throw new InvalidOperationException("Original per-lane cab and ghost renderer layers required.");
                    if(match.arenas[i]==null||model.IsChildOf(match.arenas[i])||runner.target==model||runner.target.IsChildOf(model)||runner.cabAnchor.IsChildOf(truck.transform))throw new InvalidOperationException("Physical target/ramp and independent cab must remain outside replaceable rendering.");
                }
                // All original refs/guards are proven before adding any nodes.
                for(int i=0;i<4;i++)
                {
                    var runner=match.courses[i];var truck=runner.truck;var host=Create(truck.transform,truck.visuals,truck.wheels,tires[i],catalog);var parent=new GameObject("Sports Skee lane appearance").transform;parent.SetParent(truck.visuals,false);parent.gameObject.layer=10+i;host.presentationParent=parent;host.participantSlot=-1;host.protectedRoots=new[]{match.arenas[i],runner.target,runner.cabAnchor,runner.celebration.transform};foreach(var fit in host.compatibleBodies)fit.notice+=" Skee retains this lane's original balanced truck, whole-body ring opening test, ramp, aftertouch and independent recorded landing. Appearance follows the current participant on each turn.";
                    truck.appearanceHost=host;runner.appearanceCab=Cab(interiors[i]);EditorUtility.SetDirty(host);EditorUtility.SetDirty(truck);EditorUtility.SetDirty(runner.appearanceCab);EditorUtility.SetDirty(runner);
                }
                // Deliberately do not fill Session.appearanceHosts: lane indices
                // are not participant IDs when up to eight hot-seat owners rotate.
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("SPORTS_SKEE_HOSTS_BOUND four original lanes, current-turn profiles; explicit10..13 exterior and25..28 cab layers, original physical/replay authorities retained. Native proof pending.");
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
