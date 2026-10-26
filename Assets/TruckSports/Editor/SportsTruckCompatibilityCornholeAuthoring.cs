#if UNITY_EDITOR
using System;
using System.Linq;
using LWS.TruckCornhole;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckSports.Editor
{
    public static class SportsTruckCompatibilityCornholeAuthoring
    {
        public static SportsTruckAppearanceHost[] ProvenMap(CornholeMatch match)
        {
            if(match==null||match.setup?.players==null||match.setup.players.Length!=8||match.courses==null||match.courses.Length!=4||match.courses.Any(course=>course==null||course.trucks==null||course.trucks.Length!=4||course.trucks.Any(truck=>truck==null))||match.courses.Distinct().Count()!=4||match.courses.SelectMany(course=>course.trucks).Distinct().Count()!=16)throw new InvalidOperationException("Original four Cornhole owners with four distinct retained deliveries each required.");
            var result=new SportsTruckAppearanceHost[8];var allHosts=match.courses.SelectMany(course=>course.trucks).Select(truck=>truck.appearanceHost).ToArray();if(allHosts.Any(host=>host==null)||allHosts.Distinct().Count()!=16)throw new InvalidOperationException("Run the explicit sixteen-actor Cornhole host author first.");
            for(int owner=0;owner<4;owner++)
            {
                var course=match.courses[owner];if(course.match!=match||course.index!=owner)throw new InvalidOperationException("Preserve original fixed Cornhole course ownership.");var template=course.trucks[0].appearanceHost;
                foreach(var truck in course.trucks)
                {
                    var host=truck.appearanceHost;var model=truck.paint!=null?truck.paint.transform.parent:null;
                    if(truck.course!=course||host.transform!=truck.transform||host.gameObject.scene!=match.gameObject.scene||host.participantSlot!=owner||truck.GetComponents<SportsTruckAppearanceHost>().Length!=1||model==null||model==truck.transform||!model.IsChildOf(truck.transform)||host.presentationParent!=model||host.fragmentPresentation!=null||host.catalog==null||!host.catalog.Validate(out _)||host.catalog.bodies.Length!=7||host.compatibleBodies==null||host.compatibleBodies.Length!=7||host.compatibleBodies.Any(fit=>fit==null||!fit.Valid)||host.compatibleBodies.Select(fit=>fit.bodyId).Distinct().Count()!=7||host.wheels==null||host.wheels.Length!=4||!host.wheels.SequenceEqual(truck.wheels)||host.wheels.Any(wheel=>wheel==null)||(host.staticWheelPoses?.Length??0)!=0||host.originalWheelRenderers==null||host.originalWheelRenderers.Length!=8||!SportsTruckCompatibilityAuthoring.EquivalentSetupFits(template,host))throw new InvalidOperationException("Preserve unproven Cornhole owner/delivery root, wheel, catalog, slot policy or unequal fit for course "+owner+".");
                    foreach(var body in host.catalog.bodies){var report=host.EvaluateDraft(new SportsTruckAppearanceData{bodyId=body.id});if(!report.canApply||report.effectiveBodyId!=body.id)throw new InvalidOperationException("Actual Cornhole delivery cannot promise body "+body.id+" for course "+owner+".");}
                }
                if(owner>0&&template.catalog!=result[0].catalog)throw new InvalidOperationException("Preserve mixed Cornhole participant catalogs.");result[owner]=template;
            }
            return result;
        }
        public static bool CanUpgrade(SportsTruckCompatibilitySource source,SportsTruckAppearanceHost[] target)
        {
            if(source==null||source.cornhole==null)throw new InvalidOperationException("Explicit Cornhole setup source required.");var match=source.cornhole;var proven=ProvenMap(match);
            if(target==null||!target.SequenceEqual(proven)||source.name!="Truck fit setup adapter"||source.gameObject.scene!=match.gameObject.scene||match.menus==null||match.menus.match!=match||source.menu!=match.menus||source.setupPage!=match.menus.pages[(int)CornholeScreen.Setup]||source.setupInteraction!=source.setupPage.GetComponent<CanvasGroup>()||source.catalog!=proven[0].catalog||source.sharedTurnHost!=null||source.session!=null||source.bowling!=null||source.cheese!=null||source.hockey!=null||source.airHockey!=null||source.wallball!=null||source.billiards!=null)throw new InvalidOperationException("Preserve custom Cornhole setup source/menu/page/catalog/shared or mixed owner bindings.");
            var current=source.participantHosts;if(current!=null&&current.SequenceEqual(target))return false;if(current!=null&&(current.Length!=0&&current.Length!=8||current.Any(host=>host!=null)))throw new InvalidOperationException("Only an owned all-null/default Cornhole map can upgrade; partial/custom assignments are preserved/refused.");return true;
        }
        public static bool Upgrade(SportsTruckCompatibilitySource source)
        {
            var target=ProvenMap(source.cornhole);if(!CanUpgrade(source,target))return false;source.participantHosts=target;EditorUtility.SetDirty(source);EditorSceneManager.MarkSceneDirty(source.gameObject.scene);Debug.Log("TRUCK_FIT_CORNHOLE4_MAP_UPGRADED; four logical owners, proven equivalent four retained deliveries per owner/all7 bodies; no preparation, profile, rules or layout changes.");return true;
        }
        [MenuItem("Truck Sports/Customizer/Upgrade Open Owned Cornhole Setup Report")]
        public static void UpgradeOpenScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stopped Editor required.");var source=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<SportsTruckCompatibilitySource>(true)).SingleOrDefault();if(source==null||source.cornhole==null)throw new InvalidOperationException("Open the authored original Cornhole setup report.");Upgrade(source);
        }
    }
}
#endif
