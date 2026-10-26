using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckCheeseRolling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LWS.TruckSports.Editor
{
    public static partial class SportsTruckAppearanceHostAuthoring
    {
        [MenuItem("Truck Sports/Customizer/Bind Missing Four Cheese Participant Hosts")]
        public static void BindMissingCheeseParticipantHosts()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");var catalog=AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");var source=AssetDatabase.LoadAssetAtPath<GameObject>(FacadeSource);if(catalog==null||!catalog.Validate(out _)||source==null)throw new InvalidOperationException("Existing validated catalog and installed GR3D mesh identities required.");
            const string path="Assets/TruckCheeseRolling/Scenes/TruckCheeseRolling.unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(scene.isLoaded&&scene.isDirty)throw new InvalidOperationException("Save original scene edits first.");if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var roots=scene.GetRootGameObjects();var match=roots.SelectMany(root=>root.GetComponentsInChildren<CheeseMatch>(true)).Single();if(match.trucks.Length!=16||match.trucks.Any(t=>t==null)||match.trucks.Distinct().Count()!=16||match.cheese==null||match.scenery==null||match.weather==null)throw new InvalidOperationException("Original four scoring racers plus twelve background trucks and cheese/world authority required.");
                if((match.appearanceHosts?.Length??0)>0||match.trucks.Any(t=>t.GetComponent<SportsTruckAppearanceHost>()!=null)){Debug.Log("SPORTS_CHEESE_HOSTS_PRESERVED existing custom/partial participant mapping.");return;}
                var models=new Transform[4];var tires=new Renderer[4][];var equipment=new Transform[4][];
                for(int i=0;i<16;i++)
                {
                    var truck=match.trucks[i];if(truck.match!=match||truck.index!=i||truck.body==null||truck.body.gameObject!=truck.gameObject||truck.wheels.Length!=4||truck.wheels.Any(w=>w==null||!w.transform.IsChildOf(truck.transform)))throw new InvalidOperationException("Original sixteen indexed physical Cheese rigs required.");if(i>=4)continue;
                    var model=truck.transform.Find("NWH GR3D visual geometry");CheckVehicle(truck.transform,model,truck.wheels);var body=ArenaOriginalRenderers(model,source.transform,FacadeBodyPaths);if(model.GetComponentsInChildren<Renderer>(true).Length!=29||model.GetComponentsInChildren<Collider>(true).Length!=0||model.GetComponentsInChildren<Rigidbody>(true).Length!=0||truck.paint==null||!body.Contains(truck.paint))throw new InvalidOperationException("Original exact29 body-only model subtree required.");
                    tires[i]=WheelRenderers(truck.transform,truck.wheels,null);if(tires[i].Length!=4)throw new InvalidOperationException("Original four real NWH rotating wheel meshes required.");for(int w=0;w<4;w++){var container=truck.wheels[w].wheel.rotatingContainer;var renderers=container.GetComponentsInChildren<MeshRenderer>(true);var expected=source.transform.Find(FacadeWheelPaths[w]).GetComponent<MeshFilter>();if(renderers.Length!=1||renderers[0].name!=FacadeWheelPaths[w].Substring(FacadeWheelPaths[w].LastIndexOf('/')+1)||renderers[0].GetComponent<MeshFilter>().sharedMesh!=expected.sharedMesh||renderers[0].transform.IsChildOf(model))throw new InvalidOperationException("Preserve exact existing repaired rotating GR3D wheel geometry; do not create new wheel controllers.");}
                    if(truck.marker==null||truck.splash==null||truck.tracks.Length!=2||truck.tracks.Any(track=>track==null)||truck.GetComponent<SportsFragmentRig>()!=null)throw new InvalidOperationException("Original marker, mud splash and both tracks required.");
                    equipment[i]=new[]{truck.marker,truck.splash.transform}.Concat(truck.tracks.Select(track=>track.transform)).Concat(match.trucks.Skip(4).Select(t=>t.transform)).ToArray();var known=new HashSet<Renderer>(body.Concat(tires[i]).Concat(truck.marker.GetComponentsInChildren<Renderer>(true)).Concat(truck.splash.GetComponentsInChildren<Renderer>(true)).Concat(truck.tracks.Cast<Renderer>()));if(truck.GetComponentsInChildren<Renderer>(true).Any(r=>!known.Contains(r))||equipment[i].Any(part=>part==model||part.IsChildOf(model)||model.IsChildOf(part)))throw new InvalidOperationException("Unknown rendering or overlapping participant/background equipment retained.");models[i]=model;
                }
                var hosts=new SportsTruckAppearanceHost[4];for(int i=0;i<4;i++){var truck=match.trucks[i];var host=Create(truck.transform,models[i],truck.wheels,tires[i],catalog);host.participantSlot=i;host.protectedRoots=equipment[i];host.modeOwnedSlots=new[]{SportsTruckAppearanceSlots.Dashboard};foreach(var fit in host.compatibleBodies)fit.notice+=" Cheese retains its original NWH grid settling, jump, catch, recovery and damage envelope. Only four marked participants are customized; twelve background racers retain their original art.";hosts[i]=host;EditorUtility.SetDirty(host);}match.appearanceHosts=hosts;EditorUtility.SetDirty(match);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("SPORTS_CHEESE_HOSTS_BOUND scoringOwners4/background12 unchanged; exact29+4 original meshes, physics and equipment retained. Native proof pending.");
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
