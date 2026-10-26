using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckLimbo;
using LWS.TruckKickball;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LWS.TruckSports.Editor
{
    public static partial class SportsTruckAppearanceHostAuthoring
    {
        [MenuItem("Truck Sports/Customizer/Bind Missing Limbo And Kickball Hosts")]
        public static void BindMissingLimboAndKickballHosts()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");var catalog=AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");var source=AssetDatabase.LoadAssetAtPath<GameObject>(FacadeSource);if(catalog==null||!catalog.Validate(out _)||source==null)throw new InvalidOperationException("Existing catalog and installed GR3D geometry required.");foreach(string mode in new[]{"TruckLimbo","TruckKickball"})BindLimboKickball(mode,catalog,source);
        }
        static void BindLimboKickball(string mode,SportsTruckAppearanceCatalog catalog,GameObject source)
        {
            string path="Assets/"+mode+"/Scenes/"+mode+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(scene.isLoaded&&scene.isDirty)throw new InvalidOperationException("Save original scene edits first.");if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var roots=scene.GetRootGameObjects();var limbo=mode=="TruckLimbo"?roots.SelectMany(r=>r.GetComponentsInChildren<LimboMatch>(true)).Single():null;var kick=limbo==null?roots.SelectMany(r=>r.GetComponentsInChildren<KickballMatch>(true)).Single():null;SportsEventSession match=limbo!=null?(SportsEventSession)limbo:kick;var trucks=limbo!=null?limbo.racers.Select(r=>r.truck).ToArray():kick.agents.Select(a=>a.truck).ToArray();int count=limbo!=null?8:12;var replay=limbo!=null?limbo.review?.motion:kick.replay;
                if(trucks.Length!=count||trucks.Any(t=>t==null)||trucks.Distinct().Count()!=count)throw new InvalidOperationException("Original distinct fixed owners required.");if((match.appearanceHosts?.Length??0)>0||trucks.Any(t=>t.appearanceHost!=null||t.GetComponent<SportsTruckAppearanceHost>()!=null)||limbo!=null&&(limbo.limboAppearanceHosts?.Length??0)>0||kick!=null&&(kick.presentation.appearanceCabs?.Length??0)>0){Debug.Log("SPORTS_LIMBO_KICK_HOSTS_PRESERVED "+mode+" custom/partial mapping retained.");return;}
                if(replay==null||replay.sources.Length!=(limbo!=null?16:13)||replay.ghosts.Length!=replay.sources.Length)throw new InvalidOperationException("Original exact replay roots/counts required.");if(kick!=null&&(kick.ball==null||replay.sources[12]!=kick.ball.transform||kick.presentation.cabs.Length!=4||kick.presentation.looks.Length!=4))throw new InvalidOperationException("Original physical kickball, thirteenth ball replay root and four cabs required.");
                var bodies=new Renderer[count][];var tires=new Renderer[count][];var equipment=new Transform[count][];var aux=new Renderer[count][];
                for(int i=0;i<count;i++)
                {
                    var truck=trucks[i];if(truck.session!=match||truck.slot!=i||truck.visuals==null||truck.visuals.name!="NWH GR3D visual geometry"||truck.GetComponent<SportsFragmentRig>()!=null||truck.visuals.GetComponentsInChildren<Collider>(true).Length!=0||truck.visuals.GetComponentsInChildren<Rigidbody>(true).Length!=0)throw new InvalidOperationException("Original clean separate rendering subtree required.");CheckVehicle(truck.transform,truck.visuals,truck.wheels);bodies[i]=ArenaOriginalRenderers(truck.visuals,source.transform,FacadeBodyPaths);tires[i]=ArenaOriginalRenderers(truck.visuals,source.transform,FacadeWheelPaths);if(!new HashSet<Renderer>(WheelRenderers(truck.transform,truck.wheels,truck.GetComponent<SportsWheelMeshBinding>())).SetEquals(tires[i]))throw new InvalidOperationException("Original four NWH wheel visual bindings required.");
                    if(limbo!=null)
                    {
                        var racer=limbo.racers[i];if(racer.slot!=i||racer.match!=limbo||racer.bar==null||racer.bar.body==null||racer.bar.racer!=racer||racer.bar.ledges.Length!=2||replay.sources[i*2]!=racer.transform||replay.sources[i*2+1]!=racer.bar.transform||truck.transform.localScale!=Vector3.one||truck.visuals.localScale==Vector3.zero||truck.GetComponents<BoxCollider>().Length!=5)throw new InvalidOperationException("Original five separate clearance shapes, standing truck, physical bar and interleaved review roots required.");
                        equipment[i]=new[]{racer.bar.transform}.Concat(racer.bar.ledges).ToArray();aux[i]=Array.Empty<Renderer>();
                    }
                    else
                    {
                        var agent=kick.agents[i];if(agent.match!=kick||agent.catchingCup==null||agent.catchingCup.parent!=truck.transform||agent.catchingCup.GetComponentsInChildren<Renderer>(true).Length!=3||agent.catchingCup.GetComponentsInChildren<Collider>(true).Length!=0||agent.catchingCup.GetComponentsInChildren<Rigidbody>(true).Length!=0||agent.teamMarker==null||agent.teamMarker.transform.parent!=truck.visuals||agent.replayTeamMarker==null||!agent.replayTeamMarker.transform.IsChildOf(replay.ghosts[i])||replay.sources[i]!=truck.visuals)throw new InvalidOperationException("Original external three-part cradle, team band and inner-model truck replay root required.");
                        equipment[i]=new[]{agent.catchingCup,agent.teamMarker.transform,kick.ball.transform}.Concat(kick.presentation.cabs).ToArray();aux[i]=new[]{agent.teamMarker};
                    }
                    var known=new HashSet<Renderer>(bodies[i].Concat(tires[i]).Concat(aux[i]));if(kick!=null)foreach(var renderer in kick.agents[i].catchingCup.GetComponentsInChildren<Renderer>(true))known.Add(renderer);if(truck.GetComponentsInChildren<Renderer>(true).Any(r=>!known.Contains(r))||truck.visuals.GetComponentsInChildren<Renderer>(true).Length!=(limbo!=null?33:34)||equipment[i].Any(part=>part==null)||bodies[i].Any(r=>equipment[i].Any(part=>r.transform==part||r.transform.IsChildOf(part))))throw new InvalidOperationException("Custom rendering or overlapping protected equipment retained for inspection.");
                }
                if(kick!=null)for(int i=0;i<4;i++)if(kick.presentation.cabs[i]==null||kick.presentation.cabs[i].gameObject.layer!=26+i||kick.presentation.cabs[i].GetComponent<SportsTruckAppearanceCabBinding>()!=null)throw new InvalidOperationException("Original unmodified per-view interior layer required.");
                var hosts=new SportsTruckAppearanceHost[count];for(int i=0;i<count;i++)
                {
                    var host=CreateFixedEquipmentHost(trucks[i],bodies[i],tires[i],catalog);host.participantSlot=i;host.protectedRoots=equipment[i];host.auxiliaryExteriorRenderers=aux[i];
                    if(limbo!=null)
                    {
                        host.modeOwnedSlots=new[]{SportsTruckAppearanceSlots.Hat,SportsTruckAppearanceSlots.Wheels,SportsTruckAppearanceSlots.RearOrnament,SportsTruckAppearanceSlots.Dashboard};
                        foreach(var fit in host.compatibleBodies){foreach(var group in fit.wheelGeometry)group.visualDiameter=trucks[i].wheels[group.physicalWheel].wheel.radius*2;fit.notice="Limbo retains its five original physical clearance shapes, real suspension compression and bar contacts. Roof hats, wheel cosmetics and rear ornaments are reserved; selected bodies use the original tire diameter so the visible clearance does not imply changed collision geometry.";}
                    }
                    else foreach(var fit in host.compatibleBodies)fit.notice+=" Kickball retains its original physical envelope, visible catching cradle and team band; replay retains the original inner-model truck and separate ball roots.";
                    trucks[i].appearanceHost=host;hosts[i]=host;EditorUtility.SetDirty(host);EditorUtility.SetDirty(trucks[i]);
                }
                if(limbo!=null)limbo.limboAppearanceHosts=hosts;else{kick.appearanceHosts=hosts;kick.presentation.appearanceCabs=kick.presentation.cabs.Select(Cab).ToArray();foreach(var cab in kick.presentation.appearanceCabs)EditorUtility.SetDirty(cab);EditorUtility.SetDirty(kick.presentation);}
                match.appearanceFraming=(match.appearanceFraming??Array.Empty<SportsBroadcastFraming>()).Concat(roots.SelectMany(r=>r.GetComponentsInChildren<SportsBroadcastFraming>(true)).Where(f=>f.session==match)).Distinct().ToArray();EditorUtility.SetDirty(match);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("SPORTS_LIMBO_KICK_HOSTS_BOUND "+mode+" owners="+count+" exact29+4 rendering; original clearance, gear, physics and replay roots retained. Native proof pending.");
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
