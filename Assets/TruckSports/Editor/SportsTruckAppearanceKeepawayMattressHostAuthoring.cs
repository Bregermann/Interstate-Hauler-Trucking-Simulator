using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckKeepaway;
using LWS.TruckMattressRacing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckSports.Editor
{
    public static partial class SportsTruckAppearanceHostAuthoring
    {
        [MenuItem("Truck Sports/Customizer/Bind Missing Keepaway And Mattress Hosts")]
        public static void BindMissingKeepawayAndMattressHosts()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");
            var catalog=AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");var source=AssetDatabase.LoadAssetAtPath<GameObject>(FacadeSource);
            if(catalog==null||!catalog.Validate(out _)||source==null)throw new InvalidOperationException("Existing validated catalog and installed GR3D meshes required.");
            foreach(string mode in new[]{"TruckKeepaway","TruckKeepawayFirstPerson","TruckMattressRacing"})BindKeepawayMattress(mode,catalog,source);
        }
        static void BindKeepawayMattress(string mode,SportsTruckAppearanceCatalog catalog,GameObject source)
        {
            string folder=mode=="TruckKeepawayFirstPerson"?"TruckKeepaway":mode;string path="Assets/"+folder+"/Scenes/"+mode+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
            if(scene.isLoaded&&scene.isDirty)throw new InvalidOperationException("Save existing scene edits first.");if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var roots=scene.GetRootGameObjects();var keep=mode!="TruckMattressRacing"?roots.SelectMany(root=>root.GetComponentsInChildren<KeepawayMatch>(true)).Single():null;var mattress=keep==null?roots.SelectMany(root=>root.GetComponentsInChildren<MattressMatch>(true)).Single():null;
                SportsEventSession match=keep!=null?(SportsEventSession)keep:mattress;var trucks=keep!=null?keep.trucks:mattress.racers.Select(r=>r.truck).ToArray();int count=keep!=null?4:8;
                if(trucks.Length!=count||trucks.Any(t=>t==null)||trucks.Distinct().Count()!=count)throw new InvalidOperationException("Original fixed-owner roster required.");
                if((match.appearanceHosts?.Length??0)>0||trucks.Any(t=>t.appearanceHost!=null||t.GetComponent<SportsTruckAppearanceHost>()!=null)||keep!=null&&(keep.appearanceCabs?.Length??0)>0){Debug.Log("SPORTS_KEEP_MATTRESS_HOSTS_PRESERVED "+mode+" custom/partial bindings retained.");return;}
                if(keep!=null&&(keep.cabViews.Length!=4||keep.halos.Length!=4||keep.ball==null||keep.ballShape==null||keep.chaosField==null))throw new InvalidOperationException("Original Keepaway ball, four cabs and chaos authority required.");
                if(mattress!=null&&(mattress.replay==null||mattress.world==null||mattress.replay.sources.Length!=9||mattress.replay.ghosts.Length!=9||mattress.replay.sources[8]!=mattress.world.transform||mattress.world.pillows.Length!=16))throw new InvalidOperationException("Original eight truck plus world replay roots and sixteen independent pillows required.");
                var keepInteriors=new Transform[keep!=null?4:0];if(keep!=null)for(int i=0;i<4;i++){var camera=keep.cabViews[i];var legacy=camera.view.GetComponent<SportsLegacyCabLook>();if(legacy==null||legacy.keepaway!=keep||legacy.view!=camera.view||legacy.viewIndex!=i||legacy.cockpit==null||legacy.cockpit.parent!=null||camera.cabSteering==null||camera.cabSteering.transform!=legacy.cockpit)throw new InvalidOperationException("Original detached cab must come from its actual legacy camera/steering binding.");keepInteriors[i]=legacy.cockpit;}
                var body=new Renderer[count][];var tires=new Renderer[count][];var equipment=new Transform[count][];
                for(int i=0;i<count;i++)
                {
                    var truck=trucks[i];if(truck.session!=match||truck.slot!=i||truck.visuals==null||truck.visuals==truck.transform||truck.visuals.name!="NWH GR3D visual geometry"||truck.visuals.GetComponentsInChildren<Rigidbody>(true).Length!=0||truck.visuals.GetComponentsInChildren<Collider>(true).Length!=0||truck.GetComponent<SportsFragmentRig>()!=null)throw new InvalidOperationException("Original clean nonphysical model subtree required.");
                    CheckVehicle(truck.transform,truck.visuals,truck.wheels);body[i]=ArenaOriginalRenderers(truck.visuals,source.transform,FacadeBodyPaths);tires[i]=ArenaOriginalRenderers(truck.visuals,source.transform,FacadeWheelPaths);var known=new HashSet<Renderer>(body[i].Concat(tires[i]));
                    if(truck.visuals.GetComponentsInChildren<Renderer>(true).Length!=33)throw new InvalidOperationException("Custom model rendering retained for inspection.");
                    if(keep!=null)
                    {
                        // Keepaway never ran FitTruck: its exact GR3D tires are
                        // static leaves under the model and the existing NWH
                        // rotating containers are empty. Bind those proven mesh
                        // refs without invoking a physics fitter or adding a binder.
                        if(truck.visuals.gameObject.layer!=22+i||truck.visuals.GetComponentsInChildren<Renderer>(true).Any(r=>r.gameObject.layer!=22+i)||truck.GetComponent<SportsWheelMeshBinding>()!=null||truck.wheels.Any(w=>w.wheel.rotatingContainer.GetComponentsInChildren<Renderer>(true).Length!=0)||tires[i].Any(r=>r.transform.parent!=truck.visuals))throw new InvalidOperationException("Original static GR3D wheel meshes, empty NWH visual containers and per-owner exterior layers required.");
                        equipment[i]=new[]{keep.ball.transform,keep.halos[i],keep.identities[i].transform}.Concat(keepInteriors).ToArray();
                    }
                    else
                    {
                        var racer=mattress.racers[i];if(racer.slot!=i||racer.match!=mattress||racer.cushion==null||racer.pad==null||racer.pad.gameObject!=truck.gameObject||racer.slideMaterial==null||racer.braceMaterial==null||truck.wheels.Any(w=>w.enabled)||truck.GetComponent<SportsWheelMeshBinding>()!=null||mattress.replay.sources[i]!=racer.transform)throw new InvalidOperationException("Original disabled wheels, cloth pad/cushion and full truck replay binding required.");
                        var cloth=racer.cushion.GetComponentsInChildren<Renderer>(true);if(cloth.Length!=2)throw new InvalidOperationException("Original real mattress and quilt required.");foreach(var renderer in cloth)known.Add(renderer);equipment[i]=new[]{racer.cushion,mattress.world.transform};
                    }
                    if(truck.GetComponentsInChildren<Renderer>(true).Any(r=>!known.Contains(r))||equipment[i].Any(part=>part==null||part==truck.visuals||part.IsChildOf(truck.visuals)||truck.visuals.IsChildOf(part)))throw new InvalidOperationException("Unknown rendering or overlapping required equipment retained.");
                }
                // Complete validation before adding any binding or accessory mount.
                if(keep!=null)for(int i=0;i<4;i++){var interior=keepInteriors[i];if(interior==null||interior.gameObject.layer!=26+i||interior.GetComponent<SportsTruckAppearanceCabBinding>()!=null)throw new InvalidOperationException("Original unmodified per-view cab required.");}
                var hosts=new SportsTruckAppearanceHost[count];for(int i=0;i<count;i++)
                {
                    var truck=trucks[i];var host=keep!=null?Create(truck.transform,truck.visuals,truck.wheels,tires[i],catalog):CreateStaticHost(truck.transform,truck.visuals,body[i],tires[i],catalog);host.participantSlot=i;host.protectedRoots=equipment[i];
                    if(mattress!=null)host.modeOwnedSlots=new[]{SportsTruckAppearanceSlots.Dashboard};foreach(var fit in host.compatibleBodies)fit.notice+=keep!=null?" Keepaway retains its original ball, contact, respawn and per-owner cab layers.":" Mattress retains its real cloth platform, gravity/lean contact and disabled NWH wheels.";
                    truck.appearanceHost=host;hosts[i]=host;EditorUtility.SetDirty(truck);EditorUtility.SetDirty(host);
                }
                if(keep!=null){keep.appearanceCabs=keepInteriors.Select(Cab).ToArray();foreach(var binding in keep.appearanceCabs)EditorUtility.SetDirty(binding);}
                match.appearanceHosts=hosts;match.appearanceFraming=(match.appearanceFraming??Array.Empty<SportsBroadcastFraming>()).Concat(roots.SelectMany(root=>root.GetComponentsInChildren<SportsBroadcastFraming>(true)).Where(f=>f.session==match)).Distinct().ToArray();EditorUtility.SetDirty(match);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                Debug.Log("SPORTS_KEEP_MATTRESS_HOSTS_BOUND "+mode+" fixedOwners="+count+" exact29+4 meshes; original cloth, ball, wheels, cabs and replay roots retained. Native proof pending.");
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
