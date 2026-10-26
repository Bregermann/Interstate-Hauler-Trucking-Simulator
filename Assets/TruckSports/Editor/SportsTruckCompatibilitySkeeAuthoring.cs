#if UNITY_EDITOR
using System;
using System.Linq;
using LWS.TruckSkeeBall;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckSports.Editor
{
    // Four lane actors serve up to eight logical hot-seat IDs. This is a pure
    // fit template, never an assignment of the lane's current participant.
    public static class SportsTruckCompatibilitySkeeAuthoring
    {
        public static SportsTruckAppearanceHost ProvenTemplate(SkeeMatch match)
        {
            if(match==null||match.setup?.players==null||match.setup.players.Length!=8||match.courses==null||match.courses.Length!=4||match.courses.Any(course=>course==null||course.truck==null)||match.courses.Select(course=>course.truck).Distinct().Count()!=4||(match.appearanceHosts?.Length??0)!=0)throw new InvalidOperationException("Original four Skee turn actors and deliberately empty Session host array required.");
            var hosts=new SportsTruckAppearanceHost[4];
            for(int i=0;i<4;i++)
            {
                var course=match.courses[i];var truck=course.truck;var host=truck.appearanceHost;var parent=truck.visuals!=null?truck.visuals.Find("Sports Skee lane appearance"):null;
                if(course.match!=match||course.lane!=i||truck.session!=match||host==null||host.transform!=truck.transform||host.gameObject.scene!=match.gameObject.scene||host.participantSlot!=-1||truck.GetComponents<SportsTruckAppearanceHost>().Length!=1||parent==null||host.presentationParent!=parent||parent.localPosition!=Vector3.zero||parent.localRotation!=Quaternion.identity||parent.localScale!=Vector3.one||host.fragmentPresentation!=null||host.wheels==null||host.wheels.Length!=4||!host.wheels.SequenceEqual(truck.wheels)||host.wheels.Any(wheel=>wheel==null)||(host.staticWheelPoses?.Length??0)!=0||course.appearanceCab==null||course.cabAnchor==null||!course.appearanceCab.transform.IsChildOf(course.cabAnchor)||host.compatibleBodies==null||host.compatibleBodies.Length!=7||host.compatibleBodies.Any(fit=>fit==null||!fit.Valid))throw new InvalidOperationException("Preserve unproven Skee lane root, fit, wheel, cab or turn ownership at "+i+".");
                hosts[i]=host;
            }
            var template=hosts[0];if(hosts.Distinct().Count()!=4||hosts.Any(host=>!SportsTruckCompatibilityAuthoring.EquivalentSetupFits(template,host)))throw new InvalidOperationException("Skee lane fits are not proven equivalent; preserve actual lane bindings instead of promising a shared setup template.");
            if(template.catalog.bodies.Length!=7)throw new InvalidOperationException("Original seven-body catalog required.");foreach(var host in hosts)foreach(var body in template.catalog.bodies){var report=host.EvaluateDraft(new SportsTruckAppearanceData{bodyId=body.id});if(!report.canApply||report.effectiveBodyId!=body.id)throw new InvalidOperationException("Actual Skee lane cannot promise body "+body.id+".");}
            return template;
        }
        public static bool CanUpgrade(SportsTruckCompatibilitySource source,SportsTruckAppearanceHost template)
        {
            if(source==null||!(source.session is SkeeMatch match))throw new InvalidOperationException("Explicit Skee setup source required.");var proven=ProvenTemplate(match);
            if(template!=proven||source.name!="Truck fit setup adapter"||source.gameObject.scene!=match.gameObject.scene||match.menus==null||match.menus.match!=match||source.menu!=match.menus||source.setupPage!=match.menus.pages[(int)SportsEventScreen.Setup]||source.setupInteraction!=source.setupPage.GetComponent<CanvasGroup>()||source.catalog!=template.catalog||source.bowling!=null||source.cornhole!=null||source.cheese!=null||source.hockey!=null||source.airHockey!=null||source.wallball!=null||source.billiards!=null||source.participantHosts!=null&&(source.participantHosts.Length!=0&&source.participantHosts.Length!=source.Participants.Length||source.participantHosts.Any(host=>host!=null))||source.sharedTurnHost!=null&&source.sharedTurnHost!=template)throw new InvalidOperationException("Preserve custom Skee setup source/menu/page/catalog or partial participant/template bindings.");
            return source.sharedTurnHost!=template;
        }
        public static bool Upgrade(SportsTruckCompatibilitySource source)
        {
            var template=ProvenTemplate((SkeeMatch)source.session);if(!CanUpgrade(source,template))return false;source.sharedTurnHost=template;EditorUtility.SetDirty(source);EditorSceneManager.MarkSceneDirty(source.gameObject.scene);Debug.Log("TRUCK_FIT_SKEE_TEMPLATE_UPGRADED; equivalent4 original lane hosts, active setup IDs in either option; no lane/profile assignment, preparation or layout changes.");return true;
        }
        [MenuItem("Truck Sports/Customizer/Upgrade Open Owned Skee Setup Template")]
        public static void UpgradeOpenScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stopped Editor required.");var source=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<SportsTruckCompatibilitySource>(true)).SingleOrDefault();if(source==null||!(source.session is SkeeMatch))throw new InvalidOperationException("Open an authored original Skee setup report.");Upgrade(source);
        }
    }
}
#endif
