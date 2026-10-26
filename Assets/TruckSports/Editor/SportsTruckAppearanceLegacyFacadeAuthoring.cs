using System;
using System.Collections.Generic;
using System.Linq;
using NWH.VehiclePhysics2.Modules.Trailer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckSports.Editor
{
    public static partial class SportsTruckAppearanceHostAuthoring
    {
        [MenuItem("Truck Sports/Customizer/Bind Missing Fare Fight And Snake Hosts")]
        public static void BindMissingFareAndSnakeHosts()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");
            var catalog=AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");var original=AssetDatabase.LoadAssetAtPath<GameObject>(FacadeSource);
            if(catalog==null||!catalog.Validate(out _)||original==null)throw new InvalidOperationException("Existing validated body catalog/livery and full NWH source required.");
            foreach(string mode in new[]{"TruckFareFight","TruckSnake"})BindLegacyFacade(mode,catalog,original);
        }
        static void BindLegacyFacade(string mode,SportsTruckAppearanceCatalog catalog,GameObject original)
        {
            string path="Assets/"+mode+"/Scenes/"+mode+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
            if(scene.isLoaded&&scene.isDirty)throw new InvalidOperationException("Save existing scene edits first.");if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var roots=scene.GetRootGameObjects();var match=roots.SelectMany(root=>root.GetComponentsInChildren<SportsEventSession>(true)).Single();SportsEventTruck[] trucks;Func<int,Transform[]> equipment;
                if(match is LWS.TruckFareFight.FareFightMatch fare)
                {
                    trucks=fare.trucks;
                    if(fare.interactions==null||fare.interactions.cabJugs.Length!=4||fare.interactions.jugs.Length!=4||fare.interactions.gestures.Length!=4)throw new InvalidOperationException("Preserve original four cab/jug/gesture ownership bindings.");
                    if(fare.interactions.gestures.Any(gesture=>gesture==null)||fare.interactions.blasts.Length!=4)throw new InvalidOperationException("Original live gesture controllers and jug blast pools required.");
                    equipment=i=>new[]{fare.pickupMarker,fare.destinationMarker,fare.carrierMarker,fare.passengerVisual.transform,fare.interactions.cabJugs[i],fare.interactions.jugs[i].transform,fare.interactions.blasts[i].transform};
                }
                else if(match is LWS.TruckSnake.SnakeMatch snake)
                {
                    trucks=snake.trucks;if(snake.convoys.Length!=4||snake.convoys.Any(convoy=>convoy==null))throw new InvalidOperationException("Original four independent convoys required.");
                    for(int i=0;i<4;i++)
                    {
                        var convoy=snake.convoys[i];var hitch=trucks[i].GetComponentInChildren<TrailerHitchModuleWrapper>(true);
                        if(convoy.slot!=i||convoy.match!=snake||convoy.tractor!=trucks[i]||convoy.trailerPrefab==null||convoy.firstTractorAttachment==null||hitch==null||hitch.module.attachmentPoint!=convoy.firstTractorAttachment||!convoy.firstTractorAttachment.IsChildOf(trucks[i].transform))throw new InvalidOperationException("Preserve the exact original NWH fifth-wheel/kingpin and path-backed trailer bindings.");
                    }
                    equipment=i=>new[]{snake.convoys[i].transform,snake.convoys[i].firstTractorAttachment};
                }
                else throw new InvalidOperationException("Only the two explicitly approved fixed-owner legacy facade families are supported.");
                if(trucks.Length!=4||trucks.Any(truck=>truck==null)||trucks.Distinct().Count()!=4)throw new InvalidOperationException("Four original fixed-owner physical trucks required.");
                if(trucks.Any(truck=>truck.appearanceHost!=null||truck.GetComponent<SportsTruckAppearanceHost>()!=null)){Debug.Log("SPORTS_LEGACY_FACADE_PRESERVED "+mode+" existing custom/partial hosts retained.");return;}
                var views=match.GetComponent<SportsDrivingViews>();if(views==null||!views.rally.trucks.SequenceEqual(trucks)||views.cabs.Length!=4||views.cabs.Any(cab=>cab==null))throw new InvalidOperationException("Original detached cabs and driving-view roster required.");
                var body=new Renderer[4][];var wheels=new Renderer[4][];var protectedRoots=new Transform[4][];var auxiliary=new Renderer[4][];
                for(int i=0;i<4;i++)
                {
                    var truck=trucks[i];if(truck.session!=match||truck.slot!=i||truck.visuals!=truck.transform||truck.tractorMotor==null||truck.wheels.Length!=4||truck.wheels.Any(wheel=>wheel==null||wheel.wheel.rotatingContainer==null)||truck.GetComponent<SportsFragmentRig>()!=null)throw new InvalidOperationException("Original nonfragmented full NWH fixed-owner facade required.");
                    body[i]=FacadeRenderers(truck.transform,original.transform,FacadeBodyPaths);wheels[i]=FacadeRenderers(truck.transform,original.transform,FacadeWheelPaths);protectedRoots[i]=equipment(i).Concat(views.cabs).Distinct().ToArray();
                    if(protectedRoots[i].Any(root=>root==null||body[i].Concat(wheels[i]).Any(renderer=>renderer.transform==root||renderer.transform.IsChildOf(root))))throw new InvalidOperationException("Required original gear overlaps body rendering; preserve custom assembly.");
                    var smoke=truck.transform.Find("Exhaust/ExhaustSmokeSemi")?.GetComponent<ParticleSystemRenderer>();if(smoke==null)throw new InvalidOperationException("Original NWH exhaust FX required.");
                    auxiliary[i]=protectedRoots[i].Where(root=>root.IsChildOf(truck.transform)).SelectMany(root=>root.GetComponentsInChildren<Renderer>(true)).Concat(new Renderer[]{smoke}).Distinct().ToArray();
                    var known=new HashSet<Renderer>(body[i].Concat(wheels[i]).Concat(auxiliary[i]));if(truck.GetComponentsInChildren<Renderer>(true).Any(renderer=>!known.Contains(renderer)))throw new InvalidOperationException("Unknown legacy equipment rendering retained for explicit inspection.");
                }
                for(int i=0;i<4;i++){var old=views.cabs[i].GetComponent<SportsTruckAppearanceCabBinding>();if(old!=null&&(old.drivingViews!=views||old.viewIndex!=i))throw new InvalidOperationException("Custom cab binding retained.");}
                var hosts=new List<SportsTruckAppearanceHost>(match.appearanceHosts??Array.Empty<SportsTruckAppearanceHost>());
                for(int i=0;i<4;i++)
                {
                    var truck=trucks[i];var host=truck.gameObject.AddComponent<SportsTruckAppearanceHost>();host.catalog=catalog;host.participantSlot=i;host.presentationParent=truck.transform;host.originalBodyRenderers=body[i];host.originalWheelRenderers=wheels[i];host.auxiliaryExteriorRenderers=auxiliary[i];host.protectedRoots=protectedRoots[i];host.wheels=truck.wheels;
                    var bounds=LocalBounds(truck.transform,body[i].Concat(wheels[i]));host.compatibleBodies=catalog.bodies.Select(item=>Fit(item,bounds,truck.transform,truck.wheels)).ToArray();foreach(var fit in host.compatibleBodies)fit.notice+=mode=="TruckSnake"?" Snake retains its original fifth-wheel anchor, cargo trailers and physical road-train envelope.":" Fare Fight retains its original cab, jug, passenger, damage and balanced physical envelope.";
                    truck.appearanceHost=host;hosts.Add(host);var cab=Cab(views.cabs[i]);cab.drivingViews=views;cab.viewIndex=i;EditorUtility.SetDirty(cab);EditorUtility.SetDirty(host);EditorUtility.SetDirty(truck);
                }
                match.appearanceHosts=hosts.ToArray();match.appearanceFraming=(match.appearanceFraming??Array.Empty<SportsBroadcastFraming>()).Concat(roots.SelectMany(root=>root.GetComponentsInChildren<SportsBroadcastFraming>(true)).Where(framing=>framing.session==match)).Distinct().ToArray();EditorUtility.SetDirty(match);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                Debug.Log("SPORTS_LEGACY_FIXED_FACADE_BOUND "+mode+" four explicit29+4 source meshes; original NWH physics/cabs/passenger/jug or fifth-wheel/convoy preserved. Native proof pending.");
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
