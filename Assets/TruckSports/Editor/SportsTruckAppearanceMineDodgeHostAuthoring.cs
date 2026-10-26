using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckMinefield;
using LWS.TruckDodgeball;
using NWH.WheelController3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LWS.TruckSports.Editor
{
    public static partial class SportsTruckAppearanceHostAuthoring
    {
        [MenuItem("Truck Sports/Customizer/Bind Missing Minefield And Dodgeball Hosts")]
        public static void BindMissingMinefieldAndDodgeballHosts()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");var catalog=AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");var source=AssetDatabase.LoadAssetAtPath<GameObject>(FacadeSource);if(catalog==null||!catalog.Validate(out _)||source==null)throw new InvalidOperationException("Existing catalog and installed GR3D meshes required.");
            foreach(string mode in new[]{"TruckMinefield","TruckTrampolineDodgeball"})BindMineDodge(mode,catalog,source);
        }
        static void BindMineDodge(string mode,SportsTruckAppearanceCatalog catalog,GameObject source)
        {
            string path="Assets/"+mode+"/Scenes/"+mode+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(scene.isLoaded&&scene.isDirty)throw new InvalidOperationException("Save scene edits first.");if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var roots=scene.GetRootGameObjects();var mine=mode=="TruckMinefield"?roots.SelectMany(r=>r.GetComponentsInChildren<MinefieldMatch>(true)).Single():null;var dodge=mine==null?roots.SelectMany(r=>r.GetComponentsInChildren<DodgeMatch>(true)).Single():null;SportsEventSession match=mine!=null?(SportsEventSession)mine:dodge;var replay=mine!=null?mine.replay:dodge.replay;var trucks=mine!=null?mine.racers.Select(r=>r.truck).ToArray():dodge.fighters.Select(f=>f.truck).ToArray();
                if(trucks.Length!=8||trucks.Any(t=>t==null)||trucks.Distinct().Count()!=8)throw new InvalidOperationException("Original eight distinct fixed owners required.");if((match.appearanceHosts?.Length??0)>0||trucks.Any(t=>t.appearanceHost!=null||t.GetComponent<SportsTruckAppearanceHost>()!=null)){Debug.Log("SPORTS_MINE_DODGE_PRESERVED "+mode+" custom/partial mapping.");return;}
                if(replay==null||replay.sources.Length!=(mine!=null?8:11)||replay.ghosts.Length!=replay.sources.Length)throw new InvalidOperationException("Preserve original exact replay source count.");
                if(dodge!=null&&(dodge.balls.Length!=3||dodge.replayColors==null||dodge.replayColors.replay!=replay||dodge.balls.Where((ball,i)=>ball==null||ball.body==null||ball.shape==null||replay.sources[8+i]!=ball.transform).Any()))throw new InvalidOperationException("Original three physical balls and recorded color authority required.");
                var bodies=new Renderer[8][];var tires=new Renderer[8][];var equipment=new Transform[8][];var aux=new Renderer[8][];
                for(int i=0;i<8;i++)
                {
                    var t=trucks[i];if(t.session!=match||t.slot!=i||t.visuals==null||t.visuals.name!="NWH GR3D visual geometry"||replay.sources[i]!=t.visuals||t.GetComponent<SportsFragmentRig>()!=null||t.visuals.GetComponentsInChildren<Collider>(true).Length!=0||t.visuals.GetComponentsInChildren<Rigidbody>(true).Length!=0)throw new InvalidOperationException("Original separate render-only truck replay source required.");CheckVehicle(t.transform,t.visuals,t.wheels);bodies[i]=ArenaOriginalRenderers(t.visuals,source.transform,FacadeBodyPaths);tires[i]=ArenaOriginalRenderers(t.visuals,source.transform,FacadeWheelPaths);if(!new HashSet<Renderer>(WheelRenderers(t.transform,t.wheels,t.GetComponent<SportsWheelMeshBinding>())).SetEquals(tires[i]))throw new InvalidOperationException("Original wheel visual bindings required.");
                    if(mine!=null){var r=mine.racers[i];if(r.slot!=i||r.match!=mine||mine.blastFlags.Length!=8||mine.landingFlags.Length!=8||mine.bestFlags.Length!=8||mine.flightTrails.Length!=8||mine.landPuffs.Length!=4)throw new InvalidOperationException("Original field racer, flags and bounded flight effects required.");equipment[i]=new[]{mine.blastFlags[i].transform,mine.landingFlags[i].transform,mine.bestFlags[i].transform,mine.flightTrails[i].transform,mine.landPuffs[i%4].transform};aux[i]=Array.Empty<Renderer>();}
                    else{var f=dodge.fighters[i];if(f.slot!=i||f.match!=dodge||f.teamTrim.Length!=2||f.teamTrim.Any(r=>r==null||r.transform.parent!=t.visuals)||f.strikeHalo==null||f.strikeHalo.parent!=t.visuals||f.strikeHalo.GetComponent<LineRenderer>()==null||f.hit==null)throw new InvalidOperationException("Original two team stripes, timed strike crescent and elimination effects required.");equipment[i]=new[]{f.strikeHalo,f.hit.transform}.Concat(f.teamTrim.Select(r=>r.transform)).Concat(dodge.balls.Select(b=>b.transform)).ToArray();aux[i]=f.teamTrim;}
                    var known=new HashSet<Renderer>(bodies[i].Concat(tires[i]).Concat(aux[i]));if(dodge!=null)known.Add(dodge.fighters[i].strikeHalo.GetComponent<Renderer>());if(t.visuals.GetComponentsInChildren<Renderer>(true).Length!=known.Count||t.GetComponentsInChildren<Renderer>(true).Any(r=>!known.Contains(r)))throw new InvalidOperationException("Unknown/custom truck rendering retained for inspection.");if(equipment[i].Any(part=>part==null)||bodies[i].Any(r=>equipment[i].Any(part=>r.transform==part||r.transform.IsChildOf(part))))throw new InvalidOperationException("Required sport equipment must never be included in replaceable body rendering.");
                }
                var hosts=new SportsTruckAppearanceHost[8];for(int i=0;i<8;i++){var t=trucks[i];var h=CreateFixedEquipmentHost(t,bodies[i],tires[i],catalog);h.participantSlot=i;h.protectedRoots=equipment[i];h.auxiliaryExteriorRenderers=aux[i];h.modeOwnedSlots=new[]{SportsTruckAppearanceSlots.Dashboard};foreach(var fit in h.compatibleBodies)fit.notice+=mine!=null?" Minefield retains its physical mines, original truck envelope and actual settled-distance rules.":" Dodgeball retains its team stripes, timed strike crescent, trampoline contact and three physical balls.";t.appearanceHost=h;hosts[i]=h;EditorUtility.SetDirty(h);EditorUtility.SetDirty(t);}
                match.appearanceHosts=hosts;match.appearanceFraming=(match.appearanceFraming??Array.Empty<SportsBroadcastFraming>()).Concat(roots.SelectMany(r=>r.GetComponentsInChildren<SportsBroadcastFraming>(true)).Where(f=>f.session==match)).Distinct().ToArray();EditorUtility.SetDirty(match);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("SPORTS_MINE_DODGE_BOUND "+mode+" owners8 exact29+4; original equipment/replay sources retained. Native proof pending.");
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        // Explicit body allowlist keeps equipment inside an otherwise shared
        // model parent out of both hiding and visual-fit bounds.
        static SportsTruckAppearanceHost CreateFixedEquipmentHost(SportsEventTruck truck,Renderer[] body,Renderer[] tires,SportsTruckAppearanceCatalog catalog)
        {
            var bounds=LocalBounds(truck.transform,body.Concat(tires));var host=truck.gameObject.AddComponent<SportsTruckAppearanceHost>();host.catalog=catalog;host.presentationParent=truck.visuals;host.originalBodyRenderers=body;host.originalWheelRenderers=tires;host.wheels=truck.wheels;host.compatibleBodies=catalog.bodies.Select(item=>Fit(item,bounds,truck.transform,truck.wheels)).ToArray();return host;
        }
    }
}
