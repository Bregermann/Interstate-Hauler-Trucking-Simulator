#if UNITY_EDITOR
using System;
using System.Linq;
using LWS.TruckKeepaway;
using LWS.TruckMattressRacing;
using LWS.TruckMinefield;
using LWS.TruckDodgeball;
using LWS.TruckLimbo;
using LWS.TruckKickball;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckSports.Editor
{
    // Setup reports borrow only these proven fixed physical owners. Limbo's
    // local reset-order exception must never fall through to Session hosts.
    public static class SportsTruckCompatibilityFixedOwnerAuthoring
    {
        public static bool Applies(SportsEventSession match)=>match is KeepawayMatch||match is MattressMatch||match is MinefieldMatch||match is DodgeMatch||match is LimboMatch||match is KickballMatch;
        public static SportsTruckAppearanceHost[] ProvenMap(SportsEventSession match)
        {
            SportsEventTruck[] trucks;SportsTruckAppearanceHost[] hosts;int count;
            if(match is KeepawayMatch keep){count=4;trucks=keep.trucks;hosts=keep.appearanceHosts;}
            else if(match is MattressMatch mattress){count=8;trucks=mattress.racers?.Select(r=>r!=null?r.truck:null).ToArray();hosts=mattress.appearanceHosts;}
            else if(match is MinefieldMatch mine){count=8;trucks=mine.racers?.Select(r=>r!=null?r.truck:null).ToArray();hosts=mine.appearanceHosts;}
            else if(match is DodgeMatch dodge){count=8;trucks=dodge.fighters?.Select(f=>f!=null?f.truck:null).ToArray();hosts=dodge.appearanceHosts;}
            else if(match is LimboMatch limbo){count=8;trucks=limbo.racers?.Select(r=>r!=null?r.truck:null).ToArray();hosts=limbo.limboAppearanceHosts;if((limbo.appearanceHosts?.Length??0)!=0)throw new InvalidOperationException("Limbo setup must preserve the deliberately empty Session host array and original standing-reset order.");}
            else if(match is KickballMatch kick){count=12;trucks=kick.agents?.Select(a=>a!=null?a.truck:null).ToArray();hosts=kick.appearanceHosts;}
            else throw new InvalidOperationException("Only the six reviewed fixed-owner sport families can upgrade here.");
            if(match.setup?.players==null||match.setup.players.Length<count||match.MaximumPlayers!=count||trucks==null||trucks.Length!=count||trucks.Any(t=>t==null)||trucks.Distinct().Count()!=count||hosts==null||hosts.Length!=count||hosts.Any(h=>h==null)||hosts.Distinct().Count()!=count)throw new InvalidOperationException("Run the explicit fixed-owner host author first; original counts, actors and host arrays required.");
            var result=new SportsTruckAppearanceHost[match.setup.players.Length];var catalog=hosts[0].catalog;
            if(catalog==null||!catalog.Validate(out _)||catalog.bodies==null||catalog.bodies.Length!=7)throw new InvalidOperationException("The original validated seven-body catalog is required for these setup reports.");
            for(int i=0;i<count;i++)
            {
                var truck=trucks[i];var host=hosts[i];
                if(truck.session!=match||truck.slot!=i||truck.appearanceHost!=host||host.transform!=truck.transform||host.gameObject.scene!=match.gameObject.scene||host.participantSlot!=i||truck.GetComponents<SportsTruckAppearanceHost>().Length!=1||host.presentationParent!=truck.visuals||truck.visuals==null||truck.visuals==truck.transform||!truck.visuals.IsChildOf(truck.transform)||host.catalog!=catalog||host.fragmentPresentation!=null||host.compatibleBodies==null||host.compatibleBodies.Length!=7||host.compatibleBodies.Any(fit=>fit==null||!fit.Valid)||host.compatibleBodies.Select(fit=>fit.bodyId).Distinct().Count()!=7)throw new InvalidOperationException("Preserve custom fixed-owner root, slot, catalog or fit at "+i+".");
                if(match is MattressMatch)
                {
                    if(host.wheels==null||host.wheels.Length!=0||host.staticWheelPoses==null||host.staticWheelPoses.Length!=4||host.staticWheelPoses.Any(pose=>pose==null||!pose.ValidFor(truck.transform))||truck.wheels==null||truck.wheels.Length!=4||truck.wheels.Any(wheel=>wheel==null||wheel.enabled))throw new InvalidOperationException("Mattress setup retains its actual static poses and disabled original NWH wheels.");
                }
                else if(host.wheels==null||host.wheels.Length!=4||truck.wheels==null||!host.wheels.SequenceEqual(truck.wheels)||host.wheels.Any(wheel=>wheel==null)||(host.staticWheelPoses?.Length??0)!=0)throw new InvalidOperationException("The exact original four NWH wheel references are required at "+i+".");
                foreach(var body in catalog.bodies){var report=host.EvaluateDraft(new SportsTruckAppearanceData{bodyId=body.id});if(!report.canApply||report.effectiveBodyId!=body.id)throw new InvalidOperationException("The actual fixed-owner host cannot promise body "+body.id+" at "+i+".");}
                result[i]=host;
            }
            return result;
        }
        public static bool CanUpgrade(SportsTruckCompatibilitySource source,SportsTruckAppearanceHost[] target)
        {
            if(source==null||!Applies(source.session))throw new InvalidOperationException("Only a reviewed fixed-owner setup source can upgrade.");
            var match=source.session;var proven=ProvenMap(match);
            if(target==null||!proven.SequenceEqual(target)||source.name!="Truck fit setup adapter"||source.gameObject.scene!=match.gameObject.scene||match.menus==null||match.menus.match!=match||source.menu!=match.menus||match.menus.pages==null||match.menus.pages.Length==0||source.setupPage!=match.menus.pages[(int)SportsEventScreen.Setup]||source.setupInteraction!=source.setupPage.GetComponent<CanvasGroup>()||source.sharedTurnHost!=null||source.catalog!=proven[0].catalog||source.bowling!=null||source.cornhole!=null||source.cheese!=null||source.hockey!=null||source.airHockey!=null||source.wallball!=null||source.billiards!=null)throw new InvalidOperationException("Preserve custom setup source, menu, page, catalog or mixed owner bindings.");
            var current=source.participantHosts;if(current!=null&&current.SequenceEqual(target))return false;
            if(current!=null&&(current.Length!=0&&current.Length!=target.Length||current.Any(host=>host!=null)))throw new InvalidOperationException("Only the owned all-null/default map can upgrade; preserve partial, custom or wrong-root mappings.");
            return true;
        }
        public static bool Upgrade(SportsTruckCompatibilitySource source)
        {
            var target=ProvenMap(source.session);if(!CanUpgrade(source,target))return false;source.participantHosts=target;EditorUtility.SetDirty(source);EditorSceneManager.MarkSceneDirty(source.gameObject.scene);Debug.Log("TRUCK_FIT_FIXED_OWNER_MAP_UPGRADED "+source.gameObject.scene.path+"; proven physical slots/roots/all7 fits; no Prepare, models, profiles or layout changes.");return true;
        }
        [MenuItem("Truck Sports/Customizer/Upgrade Open Owned Fixed Owner Setup Report")]
        public static void UpgradeOpenScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stopped Editor required for explicit setup binding authoring.");var scene=SceneManager.GetActiveScene();var source=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<SportsTruckCompatibilitySource>(true)).SingleOrDefault();if(source==null||!Applies(source.session))throw new InvalidOperationException("Open an authored Keepaway, Mattress, Minefield, Dodgeball, Limbo or Kickball setup report.");Upgrade(source);
        }
    }
}
#endif
