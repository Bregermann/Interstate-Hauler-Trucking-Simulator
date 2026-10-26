using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckWifeCarrying;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckSports.Editor
{
    public static partial class SportsTruckAppearanceHostAuthoring
    {
        [MenuItem("Truck Sports/Customizer/Bind Missing Wife Carrying Role Hosts")]
        public static void BindMissingWifeRoleHosts()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");var catalog=AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");var source=AssetDatabase.LoadAssetAtPath<GameObject>(FacadeSource);if(catalog==null||!catalog.Validate(out _)||source==null)throw new InvalidOperationException("Existing validated catalog and original GR3D source required.");const string path="Assets/TruckWifeCarrying/Scenes/TruckWifeCarrying.unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(scene.isLoaded&&scene.isDirty)throw new InvalidOperationException("Save existing scene edits first.");if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var roots=scene.GetRootGameObjects();var match=roots.SelectMany(root=>root.GetComponentsInChildren<WifeMatch>(true)).Single();if(match.pairs.Length!=4||match.views==null||match.views.trucks.Length!=4)throw new InvalidOperationException("Original four driver/passenger pairs and shared pair camera roster required.");
                var trucks=match.pairs.Select(pair=>pair.driver).Concat(match.pairs.Select(pair=>pair.passenger)).ToArray();if(trucks.Any(truck=>truck==null)||trucks.Distinct().Count()!=8)throw new InvalidOperationException("Eight original physical bodies required.");if(trucks.Any(truck=>truck.appearanceHost!=null||truck.GetComponent<SportsTruckAppearanceHost>()!=null)||(match.appearanceHosts?.Length??0)>0){Debug.Log("SPORTS_WIFE_HOSTS_PRESERVED existing custom/partial bindings retained.");return;}
                var body=new Renderer[8][];var wheels=new Renderer[8][];var gear=new Transform[8][];var auxiliary=new Renderer[8][];var views=match.GetComponent<SportsDrivingViews>();
                for(int i=0;i<8;i++)
                {
                    var pair=match.pairs[i%4];var truck=trucks[i];if(pair==null||pair.match!=match||pair.slot!=i%4||truck.session!=match||truck.slot!=i||truck.visuals==null||truck.visuals.name!="NWH GR3D visual geometry"||truck.visuals.parent!=truck.transform||truck.wheels.Length!=4||truck.wheels.Any(wheel=>wheel==null||wheel.wheel.rotatingContainer==null)||truck.GetComponent<SportsFragmentRig>()!=null||pair.cradle==null||pair.straps.Length!=2||pair.straps.Any(strap=>strap==null))throw new InvalidOperationException("Exact original pair/slot/inner visual/cradle/strap/NWH references required.");
                    if(i<4){if(truck.transform.localScale!=Vector3.one||match.views.trucks[i]!=truck||truck.GetComponent<SportsWheelMeshBinding>()==null)throw new InvalidOperationException("Original unit-scale powered driver bindings required.");}
                    else if((truck.transform.localScale-Vector3.one*.55f).sqrMagnitude>1e-10f||truck.wheels.Any(wheel=>wheel.enabled)||truck.GetComponent<SportsWheelMeshBinding>()!=null||truck.engine==null||truck.engine.enabled)throw new InvalidOperationException("Original smaller .55 passenger with disabled NWH/engine and removed mesh binding required; no physical repair is performed.");
                    body[i]=ArenaOriginalRenderers(truck.visuals,source.transform,FacadeBodyPaths);wheels[i]=ArenaOriginalRenderers(truck.visuals,source.transform,FacadeWheelPaths);gear[i]=new[]{pair.cradle}.Concat(pair.straps.Select(strap=>strap.transform)).Concat(views!=null?views.cabs:Array.Empty<Transform>()).Distinct().ToArray();auxiliary[i]=gear[i].Where(part=>part.IsChildOf(truck.transform)).SelectMany(part=>part.GetComponentsInChildren<Renderer>(true)).Distinct().ToArray();var known=new HashSet<Renderer>(body[i].Concat(wheels[i]).Concat(auxiliary[i]));
                    if(truck.visuals.GetComponentsInChildren<Renderer>(true).Length!=33||truck.visuals.GetComponentsInChildren<Collider>(true).Length!=0||truck.visuals.GetComponentsInChildren<Rigidbody>(true).Length!=0||truck.GetComponentsInChildren<Renderer>(true).Any(renderer=>!known.Contains(renderer))||gear[i].Any(part=>part==null||part==truck.visuals||part.IsChildOf(truck.visuals)||truck.visuals.IsChildOf(part)))throw new InvalidOperationException("Unknown/custom body/equipment overlap retained for inspection.");
                    if(i<4&&!new HashSet<Renderer>(WheelRenderers(truck.transform,truck.wheels,truck.GetComponent<SportsWheelMeshBinding>())).SetEquals(wheels[i]))throw new InvalidOperationException("Original driver wheel mesh reference mismatch.");
                }
                if(views!=null&&(views.rally!=match.views||views.cabs.Length!=4||views.cabs.Any(cab=>cab==null)))throw new InvalidOperationException("Existing owner cockpit roster changed; preserve designer configuration.");if(views!=null)for(int i=0;i<4;i++){var old=views.cabs[i].GetComponent<SportsTruckAppearanceCabBinding>();if(old!=null&&(old.drivingViews!=views||old.viewIndex!=i))throw new InvalidOperationException("Custom cockpit binding retained.");}
                var hosts=new SportsTruckAppearanceHost[8];for(int i=0;i<8;i++){var truck=trucks[i];var host=i<4?Create(truck.transform,truck.visuals,truck.wheels,wheels[i],catalog):CreateStaticHost(truck.transform,truck.visuals,body[i],wheels[i],catalog);host.participantSlot=i;host.protectedRoots=gear[i];host.auxiliaryExteriorRenderers=auxiliary[i];if(i>=4||views==null)host.modeOwnedSlots=new[]{SportsTruckAppearanceSlots.Dashboard};foreach(var fit in host.compatibleBodies)fit.notice+=" Wife Carrying retains its original roof cradle, smaller physical passenger, straps, pickup and driver/passenger roles.";truck.appearanceHost=host;hosts[i]=host;EditorUtility.SetDirty(host);EditorUtility.SetDirty(truck);}
                match.appearanceHosts=hosts;match.appearanceFraming=(match.appearanceFraming??Array.Empty<SportsBroadcastFraming>()).Concat(roots.SelectMany(root=>root.GetComponentsInChildren<SportsBroadcastFraming>(true)).Where(framing=>framing.session==match)).Distinct().ToArray();if(views!=null)for(int i=0;i<4;i++){var binding=Cab(views.cabs[i]);binding.drivingViews=views;binding.viewIndex=i;EditorUtility.SetDirty(binding);}
                EditorUtility.SetDirty(match);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("SPORTS_WIFE_ROLE_HOSTS_BOUND four original NWH drivers +four .55 static-pose passengers; role binding/inner-visual inactivity and exact physical cradle/straps/source refs retained. Native proof pending.");
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
