#if UNITY_EDITOR
using System;
using System.Linq;
using LWS.TruckCheeseRolling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckSports.Editor
{
    public static class SportsTruckCompatibilityCheeseAuthoring
    {
        public static SportsTruckAppearanceHost[] ProvenMap(CheeseMatch match)
        {
            if(match==null||match.setup?.players==null||match.setup.players.Length!=8||match.trucks==null||match.trucks.Length!=16||match.trucks.Any(t=>t==null)||match.trucks.Distinct().Count()!=16||match.appearanceHosts==null||match.appearanceHosts.Length!=4||match.appearanceHosts.Any(h=>h==null)||match.appearanceHosts.Distinct().Count()!=4)throw new InvalidOperationException("Run the explicit Cheese participant host author; four registered physical owners plus twelve untouched background trucks are required.");
            var result=new SportsTruckAppearanceHost[8];var catalog=match.appearanceHosts[0].catalog;if(catalog==null||!catalog.Validate(out _)||catalog.bodies.Length!=7)throw new InvalidOperationException("Original validated seven-body catalog required.");
            for(int i=0;i<16;i++)
            {
                var truck=match.trucks[i];if(truck.match!=match||truck.index!=i)throw new InvalidOperationException("Preserve original indexed Cheese actors.");
                if(i>=4){if(truck.GetComponent<SportsTruckAppearanceHost>()!=null)throw new InvalidOperationException("Background Cheese racers are not registered participant appearance owners.");continue;}
                var host=match.appearanceHosts[i];var model=truck.transform.Find("NWH GR3D visual geometry");
                if(host.transform!=truck.transform||host.gameObject.scene!=match.gameObject.scene||host.participantSlot!=i||truck.GetComponents<SportsTruckAppearanceHost>().Length!=1||model==null||host.presentationParent!=model||host.catalog!=catalog||host.fragmentPresentation!=null||host.wheels==null||host.wheels.Length!=4||!host.wheels.SequenceEqual(truck.wheels)||host.wheels.Any(w=>w==null)||(host.staticWheelPoses?.Length??0)!=0||host.originalBodyRenderers.Length!=29||host.originalWheelRenderers.Length!=4||host.compatibleBodies==null||host.compatibleBodies.Length!=7||host.compatibleBodies.Any(fit=>fit==null||!fit.Valid)||host.compatibleBodies.Select(fit=>fit.bodyId).Distinct().Count()!=7)throw new InvalidOperationException("Preserve unproven Cheese participant root, slot, wheel or body fit at "+i+".");
                foreach(var body in catalog.bodies){var report=host.EvaluateDraft(new SportsTruckAppearanceData{bodyId=body.id});if(!report.canApply||report.effectiveBodyId!=body.id)throw new InvalidOperationException("Actual Cheese participant cannot promise body "+body.id+" at "+i+".");}result[i]=host;
            }
            return result;
        }
        public static bool CanUpgrade(SportsTruckCompatibilitySource source,SportsTruckAppearanceHost[] target)
        {
            if(source==null||source.cheese==null)throw new InvalidOperationException("Explicit Cheese setup source required.");var match=source.cheese;var proven=ProvenMap(match);
            if(target==null||!target.SequenceEqual(proven)||source.name!="Truck fit setup adapter"||source.gameObject.scene!=match.gameObject.scene||match.menus==null||match.menus.match!=match||source.menu!=match.menus||source.setupPage!=match.menus.pages[(int)CheeseScreen.Setup]||source.setupInteraction!=source.setupPage.GetComponent<CanvasGroup>()||source.catalog!=proven[0].catalog||source.sharedTurnHost!=null||source.session!=null||source.bowling!=null||source.cornhole!=null||source.hockey!=null||source.airHockey!=null||source.wallball!=null||source.billiards!=null)throw new InvalidOperationException("Preserve custom Cheese setup source/menu/page/catalog or mixed bindings.");
            var current=source.participantHosts;if(current!=null&&current.SequenceEqual(target))return false;if(current!=null&&(current.Length!=0&&current.Length!=8||current.Any(host=>host!=null)))throw new InvalidOperationException("Only an owned all-null/default Cheese map can upgrade; partial/custom and background assignments are preserved/refused.");return true;
        }
        public static bool Upgrade(SportsTruckCompatibilitySource source)
        {
            var target=ProvenMap(source.cheese);if(!CanUpgrade(source,target))return false;source.participantHosts=target;EditorUtility.SetDirty(source);EditorSceneManager.MarkSceneDirty(source.gameObject.scene);Debug.Log("TRUCK_FIT_CHEESE4_MAP_UPGRADED; exact participant0..3 only, background12 untouched; no Prepare/profile/model/layout changes.");return true;
        }
        [MenuItem("Truck Sports/Customizer/Upgrade Open Owned Cheese Setup Report")]
        public static void UpgradeOpenScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stopped Editor required.");var source=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<SportsTruckCompatibilitySource>(true)).SingleOrDefault();if(source==null||source.cheese==null)throw new InvalidOperationException("Open an authored original Cheese setup report.");Upgrade(source);
        }
    }
}
#endif
