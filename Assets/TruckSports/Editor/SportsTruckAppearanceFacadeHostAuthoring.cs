using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckSports.Editor
{
    public static partial class SportsTruckAppearanceHostAuthoring
    {
        const string FacadeSource = "Assets/TruckFareFight/Prefabs/Fare Fight Taxi Tractor.prefab";
        static readonly string[] FacadeBodyPaths = {
            "Cab", "Cab/door rt", "Cab/doors lft", "Cab/interior", "Cab/steering wheel", "Cab/mirror lft.001", "Cab/mirror rt.001", "Cab/MirrorGlassL", "Cab/MirrorGlassR",
            "Chassis/Chassis", "Chassis/license plate front", "Chassis/license plate rear", "Exhaust",
            "Lights/StopLightRearL", "Lights/StopLightR", "Lights/LightLowerL", "Lights/LightsGlass", "Lights/LightsExtra.001", "Lights/LightR", "Lights/BlinkerRearR", "Lights/LightL", "Lights/ReverseLightR", "Lights/LightVertR", "Lights/BlinkerL", "Lights/BlinkerR", "Lights/LightVertL", "Lights/LightLowerR", "Lights/ReverseLightL", "Lights/BlinkerRearL"
        };
        static readonly string[] FacadeWheelPaths = { "Wheels/Whl HD FL_WheelController/Rotating/Whl HD FL", "Wheels/Whl HD FR_WheelController/Rotating/Whl HD FR", "Wheels/Whl HD RL_WheelController/Rotating/Whl HD RL", "Wheels/Whl HD RR_WheelController/Rotating/Whl HD RR" };
        sealed class FacadePlan
        {
            public SportsEventSession match; public SportsEventTruck[] trucks; public SportsFragmentRig[] fragments; public int count; public Func<int, Transform[]> equipment;
        }
        [MenuItem("Truck Sports/Customizer/Bind Missing Fixed Owner Facade Hosts")]
        public static void BindMissingFixedOwnerFacadeHosts()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped, compiled Editor required.");
            var catalog = AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");
            var original = AssetDatabase.LoadAssetAtPath<GameObject>(FacadeSource);
            if (catalog == null || !catalog.Validate(out _) || original == null) throw new InvalidOperationException("Existing validated catalog and original full NWH facade prefab required.");
            foreach (string mode in new[] { "TruckBalloonBattle", "TruckWhackAMole", "TruckKingOfTheHill", "TruckTetherball", "TruckJousting", "TruckMagnetMayhem", "TruckVolleyball", "TruckFreezeTag", "TruckHungryHaulers", "TruckSheepHerding", "TruckHideAndSeek", "TruckFrogger", "TruckNinjaWarrior" })
                BindFacade("Assets/" + mode + "/Scenes/" + mode + ".unity", catalog, original);
        }
        static FacadePlan FacadeBindings(SportsEventSession match)
        {
            switch (match)
            {
                case LWS.TruckBalloonBattle.BalloonMatch m: return new FacadePlan { match=m,trucks=m.trucks,fragments=m.fragments,count=8,equipment=i=>new[]{m.spikes[i].transform.parent,m.balloonGroups[i]} };
                case LWS.TruckWhackAMole.WhackMatch m: return new FacadePlan { match=m,trucks=m.trucks,fragments=m.fragments,count=8,equipment=i=>new[]{m.moleField} };
                case LWS.TruckKingOfTheHill.HillMatch m: return new FacadePlan { match=m,trucks=m.trucks,fragments=m.fragments,count=8,equipment=i=>new[]{m.ring,m.nextRing,m.beacon} };
                case LWS.TruckTetherball.TetherMatch m: return new FacadePlan { match=m,trucks=m.trucks,fragments=m.fragments,count=2,equipment=i=>new[]{m.cable.transform,m.ropeGroup,m.ball.transform} };
                case LWS.TruckJousting.JoustMatch m: return new FacadePlan { match=m,trucks=m.trucks,fragments=m.fragments,count=2,equipment=i=>new[]{m.mounts[i],m.shields[i].transform,m.debrisGroup} };
                case LWS.TruckMagnetMayhem.MagnetMatch m: return new FacadePlan { match=m,trucks=m.trucks,fragments=m.fragments,count=8,equipment=i=>new[]{m.truckMetal[i].fieldPoint,m.scrapGroup} };
                case LWS.TruckVolleyball.VolleyMatch m: return new FacadePlan { match=m,trucks=m.trucks,fragments=m.fragments,count=8,equipment=i=>new[]{m.net,m.ball.transform} };
                case LWS.TruckFreezeTag.FreezeMatch m: return new FacadePlan { match=m,trucks=m.trucks,fragments=m.fragments,count=8,equipment=i=>FreezeFacadeEquipment(m,i) };
                case LWS.TruckHungryHaulers.HaulerMatch m: return new FacadePlan { match=m,trucks=m.trucks,fragments=m.fragments,count=4,equipment=i=>new[]{m.scoops[i].transform,FacadeAttachment(m.hoppers[i].bed,m.trucks[i].transform),m.banks[i],m.holes[i].transform,m.ballGroup} };
                case LWS.TruckSheepHerding.HerdMatch m: return new FacadePlan { match=m,trucks=m.trucks,fragments=m.fragments,count=4,equipment=i=>new[]{m.flock,m.pens[i].transform} };
                case LWS.TruckHideAndSeek.HideMatch m: return new FacadePlan { match=m,trucks=m.trucks,fragments=m.fragments,count=8,equipment=i=>m.world.terrain.Select(terrain=>terrain.transform).ToArray() };
                case LWS.TruckFrogger.FrogMatch m: return new FacadePlan { match=m,trucks=m.trucks,fragments=m.fragments,count=8,equipment=i=>m.world.courses.SelectMany(course=>new[]{course.root,course.actors}).ToArray() };
                case LWS.TruckNinjaWarrior.NinjaMatch m: return new FacadePlan { match=m,trucks=m.trucks,fragments=m.fragments,count=8,equipment=i=>new[]{m.course.motionGroup} };
                default: throw new InvalidOperationException("This explicit batch excludes actual Burst sports, legacy rigs and owner-swapping vehicles.");
            }
        }
        static Transform FacadeAttachment(Transform part,Transform truck)
        {
            if(part==null||part==truck||!part.IsChildOf(truck))throw new InvalidOperationException("Existing independent sport attachment required.");
            while(part.parent!=truck)part=part.parent;return part;
        }
        static Transform[] FreezeFacadeEquipment(LWS.TruckFreezeTag.FreezeMatch match,int slot)
        {
            if(match.shatter==null||match.shatter.Length!=8||match.shatter[slot]==null)throw new InvalidOperationException("Eight original typed Weatherade shatter pools required.");
            var effect=match.shatter[slot];var renderers=effect.GetComponentsInChildren<Renderer>(true);
            if(effect.name!="Pooled existing Weatherade ice shatter"||effect.transform.parent!=match.trucks[slot].transform||renderers.Length==0||renderers.Any(renderer=>!(renderer is ParticleSystemRenderer)||renderer.GetComponent<ParticleSystem>()==null)||effect.GetComponentsInChildren<Collider>(true).Length!=0||effect.GetComponentsInChildren<Rigidbody>(true).Length!=0)
                throw new InvalidOperationException("Preserve custom FX: exact nonphysical Weatherade shatter descendant renderers required.");
            return new[]{match.cannons[slot].transform,match.muzzles[slot],match.shells[slot].transform,match.sprays[slot].transform,effect.transform};
        }
        static Renderer[] FacadeRenderers(Transform truck,Transform original,string[] paths)
        {
            var renderers=new List<Renderer>();
            foreach(string path in paths)
            {
                var actual=truck.Find(path);var source=original.Find(path);
                var mesh=actual!=null?actual.GetComponent<MeshFilter>():null;var expected=source!=null?source.GetComponent<MeshFilter>():null;
                var renderer=actual!=null?actual.GetComponent<MeshRenderer>():null;
                if(renderer==null||mesh==null||expected==null||mesh.sharedMesh==null||mesh.sharedMesh!=expected.sharedMesh)throw new InvalidOperationException("Exact original NWH renderer path and installed mesh identity required: "+path);
                renderers.Add(renderer);
            }
            return renderers.ToArray();
        }
        static void BindFacade(string path,SportsTruckAppearanceCatalog catalog,GameObject original)
        {
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
            if(scene.isLoaded&&scene.isDirty)throw new InvalidOperationException("Save existing scene edits first: "+path);
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var roots=scene.GetRootGameObjects();var match=roots.SelectMany(root=>root.GetComponentsInChildren<SportsEventSession>(true)).Single();var plan=FacadeBindings(match);
                if(plan.trucks==null||plan.fragments==null||plan.trucks.Length!=plan.count||plan.fragments.Length!=plan.count||plan.trucks.Any(truck=>truck==null)||plan.trucks.Distinct().Count()!=plan.count)throw new InvalidOperationException("Preserve the complete original fixed-owner facade roster.");
                if(plan.trucks.Any(truck=>truck.appearanceHost!=null||truck.GetComponent<SportsTruckAppearanceHost>()!=null)||plan.fragments.Any(rig=>rig!=null&&rig.appearanceHost!=null))
                {Debug.Log("SPORTS_FACADE_APPEARANCE_PRESERVED "+path+" existing custom/partial bindings retained.");return;}
                var views=match.GetComponent<SportsDrivingViews>();
                if(views==null||views.rally==null||!views.rally.trucks.SequenceEqual(plan.trucks)||views.cabs==null||views.cabs.Length!=4||views.cabs.Any(cab=>cab==null))throw new InvalidOperationException("Original full driving-view roster and four detached live cabs required.");
                var body=new Renderer[plan.count][];var rolling=new Renderer[plan.count][];var equipment=new Transform[plan.count][];var auxiliary=new Renderer[plan.count][];
                for(int i=0;i<plan.count;i++)
                {
                    var truck=plan.trucks[i];var rig=plan.fragments[i];
                    if(truck.session!=match||truck.slot!=i||truck.visuals!=truck.transform||truck.tractorMotor==null||truck.wheels==null||truck.wheels.Length!=4||truck.wheels.Any(wheel=>wheel==null||wheel.wheel.rotatingContainer==null))throw new InvalidOperationException("Exact fixed owner full NWH facade and existing wheel containers required.");
                    body[i]=FacadeRenderers(truck.transform,original.transform,FacadeBodyPaths);rolling[i]=FacadeRenderers(truck.transform,original.transform,FacadeWheelPaths);
                    if(rig==null||rig.truck!=null||rig.sourceBody!=truck.body||rig.pieces==null||rig.pieces.Length!=12||rig.pieces.Any(piece=>piece.source==null||piece.body==null||piece.collider==null||piece.renderer==null)||rig.sourceRenderers==null||!new HashSet<Renderer>(rig.sourceRenderers).SetEquals(body[i].Concat(rolling[i])))throw new InvalidOperationException("Preserve the exact original twelve-piece sourceBody renderer-only reset rig.");
                    if(rolling[i].Any(renderer=>!truck.wheels.Any(wheel=>renderer.transform.IsChildOf(wheel.wheel.rotatingContainer))))throw new InvalidOperationException("Original four tire renderers must remain bound to the original NWH rotating containers.");
                    equipment[i]=plan.equipment(i).Concat(views.cabs).Distinct().ToArray();
                    if(equipment[i].Any(part=>part==null||body[i].Concat(rolling[i]).Any(renderer=>renderer.transform==part||renderer.transform.IsChildOf(part))))throw new InvalidOperationException("Preserve custom gear ancestry; a protected attachment overlaps original truck rendering.");
                    var smoke=truck.transform.Find("Exhaust/ExhaustSmokeSemi")?.GetComponent<ParticleSystemRenderer>();if(smoke==null)throw new InvalidOperationException("Original NWH exhaust particle renderer required.");
                    auxiliary[i]=equipment[i].Where(part=>part.IsChildOf(truck.transform)).SelectMany(part=>part.GetComponentsInChildren<Renderer>(true)).Concat(new Renderer[]{smoke}).Distinct().ToArray();
                    var known=new HashSet<Renderer>(body[i].Concat(rolling[i]).Concat(auxiliary[i]).Concat(rig.pieces.Select(piece=>piece.renderer)));
                    if(truck.GetComponentsInChildren<Renderer>(true).Any(renderer=>!known.Contains(renderer)))throw new InvalidOperationException("Unknown full-tractor rendering is preserved; inspect its actual attachment before extending the explicit allowlist.");
                }
                for(int v=0;v<4;v++){var old=views.cabs[v].GetComponent<SportsTruckAppearanceCabBinding>();if(old!=null&&(old.drivingViews!=views||old.viewIndex!=v))throw new InvalidOperationException("Custom cab appearance binding preserved.");}
                var hosts=new List<SportsTruckAppearanceHost>(match.appearanceHosts??Array.Empty<SportsTruckAppearanceHost>());
                for(int i=0;i<plan.count;i++)
                {
                    var truck=plan.trucks[i];var host=truck.gameObject.AddComponent<SportsTruckAppearanceHost>();host.catalog=catalog;host.participantSlot=i;host.presentationParent=truck.transform;host.originalBodyRenderers=body[i];host.originalWheelRenderers=rolling[i];host.auxiliaryExteriorRenderers=auxiliary[i];host.wheels=truck.wheels;host.protectedRoots=equipment[i];
                    var bounds=LocalBounds(truck.transform,body[i].Concat(rolling[i]));host.compatibleBodies=catalog.bodies.Select(item=>Fit(item,bounds,truck.transform,truck.wheels)).ToArray();
                    foreach(var fit in host.compatibleBodies)fit.notice+=" This sport retains its original NWH physical envelope, cab and required equipment.";
                    truck.appearanceHost=host;plan.fragments[i].appearanceHost=host;hosts.Add(host);EditorUtility.SetDirty(host);EditorUtility.SetDirty(truck);EditorUtility.SetDirty(plan.fragments[i]);
                }
                for(int v=0;v<4;v++){var cab=Cab(views.cabs[v]);cab.drivingViews=views;cab.viewIndex=v;EditorUtility.SetDirty(cab);}
                match.appearanceHosts=hosts.ToArray();match.appearanceFraming=(match.appearanceFraming??Array.Empty<SportsBroadcastFraming>()).Concat(roots.SelectMany(root=>root.GetComponentsInChildren<SportsBroadcastFraming>(true)).Where(framing=>framing.session==match)).Distinct().ToArray();
                EditorUtility.SetDirty(match);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                Debug.Log("SPORTS_FACADE_APPEARANCE_BOUND "+path+" hosts="+plan.count+" explicit29body+4wheel installed-mesh bindings; original physical roots/controllers/equipment/cabs/twelve-piece rigs retained; no actual-Burst mode included. Native proof pending.");
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
