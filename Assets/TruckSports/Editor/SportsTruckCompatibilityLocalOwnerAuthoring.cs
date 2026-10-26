#if UNITY_EDITOR
using System;
using System.Linq;
using LWS.TruckTerrierRacing;
using LWS.TruckDockDiving;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckSports.Editor
{
    public static class SportsTruckCompatibilityLocalOwnerAuthoring
    {
        public static bool Applies(SportsEventSession match)=>match is TerrierMatch||match is DockMatch;
        static void ProveHost(SportsEventSession match,SportsEventTruck truck,SportsTruckAppearanceHost host)
        {
            if(truck==null||truck.session!=match||host==null||host.transform!=truck.transform||host.gameObject.scene!=match.gameObject.scene||truck.appearanceHost!=host||truck.GetComponents<SportsTruckAppearanceHost>().Length!=1||truck.visuals==null||host.presentationParent!=truck.visuals||host.fragmentPresentation!=null||host.originalBodyRenderers==null||host.originalBodyRenderers.Length!=29||host.originalWheelRenderers==null||host.originalWheelRenderers.Length!=4||host.catalog==null||!host.catalog.Validate(out _)||host.catalog.bodies.Length!=7||host.compatibleBodies==null||host.compatibleBodies.Length!=7||host.compatibleBodies.Any(fit=>fit==null||!fit.Valid)||host.compatibleBodies.Select(fit=>fit.bodyId).Distinct().Count()!=7||host.wheels==null||host.wheels.Length!=4||!host.wheels.SequenceEqual(truck.wheels)||host.wheels.Any(wheel=>wheel==null)||(host.staticWheelPoses?.Length??0)!=0)throw new InvalidOperationException("Preserve an unproven original local appearance actor/root/wheel/fit.");
            foreach(var body in host.catalog.bodies){var report=host.EvaluateDraft(new SportsTruckAppearanceData{bodyId=body.id});if(!report.canApply||report.effectiveBodyId!=body.id)throw new InvalidOperationException("Actual local actor cannot promise body "+body.id+".");}
        }
        public static SportsTruckAppearanceHost[] ProvenTerrierMap(TerrierMatch match)
        {
            if(match==null||match.setup?.players==null||match.setup.players.Length!=12||match.racers==null||match.racers.Length!=12||match.racers.Any(r=>r==null||r.truck==null)||match.racers.Select(r=>r.truck).Distinct().Count()!=12||(match.appearanceHosts?.Length??0)!=0||match.appearanceBridge==null||match.appearanceBridge.match!=match||match.appearanceBridge.owners==null||match.appearanceBridge.owners.Length!=12||match.appearanceBridge.recipes==null||match.appearanceBridge.recipes.Length!=7||match.replay==null||match.replay.ghosts.Length!=12)throw new InvalidOperationException("Original Terrier twelve local bridge owners and empty Session host array required.");
            var bridge=match.appearanceBridge;var result=new SportsTruckAppearanceHost[12];
            for(int i=0;i<12;i++)
            {
                var racer=match.racers[i];var binding=bridge.owners[i];var truck=racer.truck;var host=binding!=null?binding.host:null;ProveHost(match,truck,host);
                if(racer.match!=match||racer.slot!=i||truck.slot!=i||host.participantSlot!=i||binding.wheels==null||binding.wheels.racer!=racer||binding.wheels!=truck.GetComponent<TerrierWheelPresentation>()||binding.wheels.meshes==null||binding.wheels.meshes.Length!=4||!binding.wheels.meshes.SequenceEqual(host.originalWheelRenderers.Select(renderer=>renderer!=null?renderer.transform:null))||binding.originalParts==null||binding.originalParts.Length!=34||binding.originalParts[0]!=truck.visuals||binding.originalParts.Any(part=>part==null||part!=truck.visuals&&!part.IsChildOf(truck.visuals))||binding.originalGhostParts==null||binding.originalGhostParts.Length!=34||match.replay.ghosts[i]==null||match.replay.ghosts[i].childCount==0||binding.originalGhostParts[0]!=match.replay.ghosts[i].GetChild(0)||binding.originalGhostParts.Any(part=>part==null||!part.IsChildOf(match.replay.ghosts[i])))throw new InvalidOperationException("Preserve Terrier fixed owner/wheel/original replay channel mapping at "+i+".");
                result[i]=host;
            }
            if(result.Distinct().Count()!=12||result.Any(host=>host.catalog!=result[0].catalog))throw new InvalidOperationException("Terrier owner hosts/catalog must be exact and distinct.");
            foreach(var body in result[0].catalog.bodies){var recipes=bridge.recipes.Where(recipe=>recipe!=null&&recipe.bodyId==body.id).ToArray();if(recipes.Length!=1||!recipes[0].ValidFor(body,out _))throw new InvalidOperationException("Actual Terrier selected rendering recipe is unavailable for "+body.id+".");}
            return result;
        }
        public static SportsTruckAppearanceHost ProvenDockTemplate(DockMatch match)
        {
            if(match==null||match.setup?.players==null||match.setup.players.Length!=8||match.truck==null||match.staged==null||match.staged.Length!=8||match.staged.Any(truck=>truck==null)||match.staged.Distinct().Count()!=8||match.staged.Contains(match.truck)||match.appearanceSplit==null||match.appearanceSplit.match!=match||match.appearanceSplit.recipes==null||match.appearanceSplit.recipes.Length!=7)throw new InvalidOperationException("Original Dock turn truck, eight staged actors and selected split presenter required.");
            var trucks=new[]{match.truck}.Concat(match.staged).ToArray();var hosts=trucks.Select(truck=>truck.appearanceHost).ToArray();
            if(hosts.Any(host=>host==null)||hosts.Distinct().Count()!=9||match.appearanceHosts==null||match.appearanceHosts.Length!=9||!match.appearanceHosts.OrderBy(host=>host!=null?host.GetInstanceID():0).SequenceEqual(hosts.OrderBy(host=>host.GetInstanceID()))||match.appearanceSplit.host!=hosts[0])throw new InvalidOperationException("Exact nine Dock actor hosts, including the actual split turn host, required.");
            for(int i=0;i<9;i++){ProveHost(match,trucks[i],hosts[i]);if(i>0&&(trucks[i].slot!=i-1||hosts[i].participantSlot!=i-1))throw new InvalidOperationException("Preserve fixed Dock staged owner slots.");if(!SportsTruckCompatibilityAuthoring.EquivalentSetupFits(hosts[0],hosts[i]))throw new InvalidOperationException("Dock staged fits differ from the actual split turn truck; preserve authored bindings.");}
            var template=hosts[0];
            foreach(var body in template.catalog.bodies)
            {
                var recipes=match.appearanceSplit.recipes.Where(recipe=>recipe!=null&&recipe.bodyId==body.id).ToArray();if(recipes.Length!=1||!recipes[0].ValidFor(body))throw new InvalidOperationException("Reviewed persistent private Dock split recipe required for "+body.id+".");var fit=template.compatibleBodies.Single(item=>item.bodyId==body.id);var matrix=Matrix4x4.TRS(fit.position,Quaternion.Euler(fit.euler),fit.scale)*Matrix4x4.Rotate(Quaternion.Euler(body.presentationEuler));
                for(int row=0;row<4;row++)for(int col=0;col<4;col++)if(!float.IsFinite(recipes[0].canonicalToVehicle[row,col])||Mathf.Abs(matrix[row,col]-recipes[0].canonicalToVehicle[row,col])>2e-4f)throw new InvalidOperationException("Preserve Dock split fit/recipe mismatch for "+body.id+".");
            }
            return template;
        }
        public static bool CanUpgrade(SportsTruckCompatibilitySource source)
        {
            if(source==null||!Applies(source.session))throw new InvalidOperationException("Explicit Terrier/Dock setup source required.");var match=source.session;var target=match is TerrierMatch terrier?ProvenTerrierMap(terrier):null;var template=match is DockMatch dock?ProvenDockTemplate(dock):null;var catalog=target!=null?target[0].catalog:template.catalog;
            if(source.name!="Truck fit setup adapter"||source.gameObject.scene!=match.gameObject.scene||match.menus==null||match.menus.match!=match||source.menu!=match.menus||source.setupPage!=match.menus.pages[(int)SportsEventScreen.Setup]||source.setupInteraction!=source.setupPage.GetComponent<CanvasGroup>()||source.catalog!=catalog||source.bowling!=null||source.cornhole!=null||source.cheese!=null||source.hockey!=null||source.airHockey!=null||source.wallball!=null||source.billiards!=null||source.sharedTurnHost!=null&&source.sharedTurnHost!=template)throw new InvalidOperationException("Preserve custom local setup source/menu/page/catalog/template bindings.");
            var current=source.participantHosts;if(target!=null&&current!=null&&current.SequenceEqual(target))return false;
            if(current!=null&&(current.Length!=0&&current.Length!=source.Participants.Length||current.Any(host=>host!=null)))throw new InvalidOperationException("Only an owned all-null/default local setup mapping can upgrade; partial/custom maps are preserved/refused.");
            return target!=null||source.sharedTurnHost!=template;
        }
        public static bool Upgrade(SportsTruckCompatibilitySource source)
        {
            if(!CanUpgrade(source))return false;if(source.session is TerrierMatch terrier)source.participantHosts=ProvenTerrierMap(terrier);else source.sharedTurnHost=ProvenDockTemplate((DockMatch)source.session);EditorUtility.SetDirty(source);EditorSceneManager.MarkSceneDirty(source.gameObject.scene);Debug.Log("TRUCK_FIT_LOCAL_OWNER_MAP_UPGRADED "+source.gameObject.scene.path+"; original local owners and validated recipes; no preparation, rendering pools, profiles or layout changes.");return true;
        }
        [MenuItem("Truck Sports/Customizer/Upgrade Open Owned Terrier Or Dock Setup Report")]
        public static void UpgradeOpenScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stopped Editor required.");var source=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<SportsTruckCompatibilitySource>(true)).SingleOrDefault();if(source==null||!Applies(source.session))throw new InvalidOperationException("Open an authored original Terrier/Dock setup report after actor/recipe authoring.");Upgrade(source);
        }
    }
}
#endif
