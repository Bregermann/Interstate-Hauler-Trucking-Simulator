using System;
using System.Linq;
using System.Collections.Generic;
using LWS.TruckLogRolling;
using LWS.TruckWallball;
using NWH.WheelController3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckSports.Editor
{
    public static partial class SportsTruckAppearanceHostAuthoring
    {
        [MenuItem("Truck Sports/Customizer/Bind Missing Log Rolling And Wallball Hosts")]
        public static void BindMissingLogAndWallballHosts()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");
            var catalog=AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");var source=AssetDatabase.LoadAssetAtPath<GameObject>(FacadeSource);
            if(catalog==null||!catalog.Validate(out _)||source==null)throw new InvalidOperationException("Existing validated catalog and original GR3D renderer identities required.");
            foreach(string mode in new[]{"TruckLogRolling","TruckWallball"})BindArena(mode,catalog,source);
        }
        static void BindArena(string mode,SportsTruckAppearanceCatalog catalog,GameObject source)
        {
            string path="Assets/"+mode+"/Scenes/"+mode+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
            if(scene.isLoaded&&scene.isDirty)throw new InvalidOperationException("Save existing scene edits first.");if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var roots=scene.GetRootGameObjects();var log=mode=="TruckLogRolling"?roots.SelectMany(root=>root.GetComponentsInChildren<LogMatch>(true)).Single():null;var wall=mode=="TruckWallball"?roots.SelectMany(root=>root.GetComponentsInChildren<WallballMatch>(true)).Single():null;
                Component[] actors=log!=null?log.fighters.Select(fighter=>(Component)fighter.truck).ToArray():wall.trucks.Cast<Component>().ToArray();int count=log!=null?4:8;
                if(actors.Length!=count||actors.Any(actor=>actor==null)||actors.Distinct().Count()!=count)throw new InvalidOperationException("Exact original fixed-owner arena roster required.");
                if(actors.Any(actor=>actor.GetComponent<SportsTruckAppearanceHost>()!=null)||log!=null&&log.fighters.Any(fighter=>fighter.truck.appearanceHost!=null)||wall!=null&&(wall.appearanceHosts?.Length??0)>0){Debug.Log("SPORTS_ARENA_APPEARANCE_PRESERVED "+mode+" existing custom/partial bindings retained.");return;}
                var models=new Transform[count];var wheels=new WheelController[count][];var rolling=new Renderer[count][];var protectedRoots=new Transform[count][];
                if(log!=null&&(log.log==null||log.buoyancy==null||log.buoyancy.GetComponent<Rigidbody>()!=log.log||log.replay==null||log.replay.sources.Length!=5||log.replay.ghosts.Length!=5||log.replay.sources[4]!=log.log.transform))throw new InvalidOperationException("Preserve actual floating log/buoyancy and fifth replay source.");
                if(wall!=null&&(wall.ball==null||wall.ball.body==null||wall.ball.shape==null||wall.wall==null||wall.floor==null||wall.paint.Length!=8))throw new InvalidOperationException("Original Wallball solid contact/serve arena required.");
                for(int i=0;i<count;i++)
                {
                    var actor=actors[i];models[i]=actor.transform.Find("NWH GR3D visual geometry");wheels[i]=log!=null?log.fighters[i].truck.wheels:wall.trucks[i].wheels;
                    if(log!=null){var fighter=log.fighters[i];if(fighter.slot!=i||fighter.match!=log||fighter.truck.slot!=i||fighter.truck.session!=log||fighter.truck.visuals!=models[i]||fighter.splash==null||log.replay.sources[i]!=fighter.truck.transform)throw new InvalidOperationException("Original log fighter/slot/splash/scrape/replay binding required.");protectedRoots[i]=new[]{log.log.transform,log.buoyancy.transform,fighter.splash.transform}.Concat(fighter.scrape!=null?new[]{fighter.scrape.transform}:Array.Empty<Transform>()).Distinct().ToArray();}
                    else{if(wall.trucks[i].slot!=i||wall.trucks[i].match!=wall)throw new InvalidOperationException("Original Wallball actor slot required.");protectedRoots[i]=new[]{wall.ball.transform,wall.wall.transform,wall.floor.transform};}
                    CheckVehicle(actor.transform,models[i],wheels[i]);if(models[i].GetComponentsInChildren<Collider>(true).Length!=0||models[i].GetComponentsInChildren<Rigidbody>(true).Length!=0||actor.GetComponent<SportsFragmentRig>()!=null)throw new InvalidOperationException("Only clean nonphysical model subtrees; no fragment adapter in this batch.");
                    rolling[i]=ArenaOriginalRenderers(models[i],source.transform,FacadeWheelPaths);var body=ArenaOriginalRenderers(models[i],source.transform,FacadeBodyPaths);var all=models[i].GetComponentsInChildren<Renderer>(true);
                    if(all.Length!=33||!new HashSet<Renderer>(all).SetEquals(body.Concat(rolling[i]))||actor.GetComponentsInChildren<Renderer>(true).Any(renderer=>!all.Contains(renderer)))throw new InvalidOperationException("Unknown arena renderers retained for explicit inspection.");
                    if(protectedRoots[i].Any(part=>part==null||part==models[i]||part.IsChildOf(models[i])||models[i].IsChildOf(part)))throw new InvalidOperationException("Sport equipment overlaps the original clean model.");
                    if(log!=null){var binding=actor.GetComponent<SportsWheelMeshBinding>();if(binding==null||!new HashSet<Renderer>(WheelRenderers(actor.transform,wheels[i],binding)).SetEquals(rolling[i]))throw new InvalidOperationException("Original log wheel mesh bindings changed.");}
                }
                var hosts=new SportsTruckAppearanceHost[count];for(int i=0;i<count;i++){var host=Create(actors[i].transform,models[i],wheels[i],rolling[i],catalog);host.participantSlot=i;host.protectedRoots=protectedRoots[i];foreach(var fit in host.compatibleBodies)fit.notice+=log!=null?" Log Rolling retains its physical buoyant log, balanced chassis, water rules and brace controls.":" Wallball retains its physical contact envelope, real NWH wheels, jump and serve rules.";hosts[i]=host;if(log!=null){log.fighters[i].truck.appearanceHost=host;EditorUtility.SetDirty(log.fighters[i].truck);}EditorUtility.SetDirty(host);}
                if(log!=null){log.appearanceHosts=(log.appearanceHosts??Array.Empty<SportsTruckAppearanceHost>()).Concat(hosts).ToArray();log.appearanceFraming=(log.appearanceFraming??Array.Empty<SportsBroadcastFraming>()).Concat(roots.SelectMany(root=>root.GetComponentsInChildren<SportsBroadcastFraming>(true)).Where(framing=>framing.session==log)).Distinct().ToArray();EditorUtility.SetDirty(log);}else{wall.appearanceHosts=hosts;EditorUtility.SetDirty(wall);}
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("SPORTS_ARENA_APPEARANCE_BOUND "+mode+" fixedOwners="+count+" exact29+4 original mesh identities; physical ball/log/wheels/rules retained. Native proof pending.");
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        static Renderer[] ArenaOriginalRenderers(Transform model,Transform source,IEnumerable<string> paths)
        {
            return paths.Select(path=>{var part=model.Find(path.Substring(path.LastIndexOf('/')+1));var expected=source.Find(path)?.GetComponent<MeshFilter>();var filter=part!=null?part.GetComponent<MeshFilter>():null;var renderer=part!=null?part.GetComponent<MeshRenderer>():null;if(renderer==null||filter==null||expected==null||filter.sharedMesh==null||filter.sharedMesh!=expected.sharedMesh)throw new InvalidOperationException("Exact flat GR3D mesh identity required: "+path);return (Renderer)renderer;}).ToArray();
        }
    }
}
