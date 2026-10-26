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
        static readonly string[] BurstModes={"TruckRedLightGreenLight","TruckHotPotato","TruckSeesaw","TruckFloorIsLava","TruckAvalancheEscape"};
        sealed class BurstPlan{public SportsEventSession match;public SportsEventTruck[] trucks;public SportsFragmentRig[] rigs;public int count;public Func<int,Transform[]> equipment;}
        static BurstPlan BurstBindings(SportsEventSession match)
        {
            switch(match)
            {
                case LWS.TruckRedLightGreenLight.RedLightMatch m:return new BurstPlan{match=m,trucks=m.trucks,rigs=m.fragments,count=12,equipment=i=>new[]{m.master,m.explosions[i].transform}};
                case LWS.TruckHotPotato.HotPotatoMatch m:return new BurstPlan{match=m,trucks=m.trucks,rigs=m.fragments,count=8,equipment=i=>new[]{m.bomb,m.blasts[i].transform}};
                case LWS.TruckSeesaw.SeesawMatch m:return new BurstPlan{match=m,trucks=m.trucks,rigs=m.fragments,count=4,equipment=i=>new[]{m.platform.transform,m.impacts[i].transform}};
                case LWS.TruckFloorIsLava.LavaMatch m:return new BurstPlan{match=m,trucks=m.trucks,rigs=m.fragments,count=8,equipment=i=>new[]{m.platformGroup,m.lava,m.blasts[i].transform,m.splashes[i].transform}};
                case LWS.TruckAvalancheEscape.AvalancheMatch m:return new BurstPlan{match=m,trucks=m.trucks,rigs=m.fragments,count=12,equipment=i=>new[]{m.front.transform,m.front.debrisGroup,m.burialsRoot}};
                default:throw new InvalidOperationException("Only the five explicit full-NWH actual-Burst families are supported.");
            }
        }
        [MenuItem("Truck Sports/Customizer/Bind Missing Actual Burst Family Hosts")]
        public static void BindMissingBurstFamilyHosts()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");
            var catalog=AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");var original=AssetDatabase.LoadAssetAtPath<GameObject>(FacadeSource);
            if(catalog==null||!catalog.Validate(out _)||original==null)throw new InvalidOperationException("Existing catalog, livery and original full NWH source required.");
            var recipes=catalog.bodies.Select(body=>AssetDatabase.LoadAssetAtPath<SportsTruckFragmentRecipe>(SportsTruckFragmentRecipeAuthoring.Folder+"/"+body.id+".asset")).ToArray();
            for(int i=0;i<recipes.Length;i++)if(recipes[i]==null||!recipes[i].ValidFor(catalog.bodies[i],out _))throw new InvalidOperationException("Create/validate all private licensed body fragment recipes first.");
            foreach(string mode in BurstModes)BindBurst(mode,catalog,original,recipes);
        }
        static void BindBurst(string mode,SportsTruckAppearanceCatalog catalog,GameObject original,SportsTruckFragmentRecipe[] recipes)
        {
            string path="Assets/"+mode+"/Scenes/"+mode+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
            if(scene.isLoaded&&scene.isDirty)throw new InvalidOperationException("Save existing scene edits first: "+path);if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var roots=scene.GetRootGameObjects();var match=roots.SelectMany(root=>root.GetComponentsInChildren<SportsEventSession>(true)).Single();var plan=BurstBindings(match);
                if(plan.trucks.Length!=plan.count||plan.rigs.Length!=plan.count||plan.trucks.Any(truck=>truck==null)||plan.trucks.Distinct().Count()!=plan.count)throw new InvalidOperationException("Preserve the complete original fixed-owner Burst roster.");
                if(plan.trucks.Any(truck=>truck.appearanceHost!=null||truck.GetComponent<SportsTruckAppearanceHost>()!=null)||plan.rigs.Any(rig=>rig!=null&&(rig.appearanceHost!=null||rig.fragmentPresentation!=null))){Debug.Log("SPORTS_BURST_BINDINGS_PRESERVED "+mode+" existing custom/partial bindings retained.");return;}
                var views=match.GetComponent<SportsDrivingViews>();if(views==null||!views.rally.trucks.SequenceEqual(plan.trucks)||views.cabs.Length!=4||views.cabs.Any(cab=>cab==null))throw new InvalidOperationException("Preserve original full cabs and driving-view roster.");
                var bodies=new Renderer[plan.count][];var tires=new Renderer[plan.count][];var gear=new Transform[plan.count][];var auxiliary=new Renderer[plan.count][];
                for(int i=0;i<plan.count;i++)
                {
                    var truck=plan.trucks[i];var rig=plan.rigs[i];if(truck.session!=match||truck.slot!=i||truck.visuals!=truck.transform||truck.tractorMotor==null||truck.wheels.Length!=4||truck.wheels.Any(wheel=>wheel==null||wheel.wheel.rotatingContainer==null))throw new InvalidOperationException("Existing full-NWH fixed owner and four original wheel containers required.");
                    bodies[i]=FacadeRenderers(truck.transform,original.transform,FacadeBodyPaths);tires[i]=FacadeRenderers(truck.transform,original.transform,FacadeWheelPaths);
                    if(rig==null||rig.truck!=null||rig.sourceBody!=truck.body||rig.pieces.Length!=12||!new HashSet<Renderer>(rig.sourceRenderers).SetEquals(bodies[i].Concat(tires[i]))||rig.pieces.Any(piece=>piece.source==null||piece.body==null||piece.collider==null||piece.renderer==null))throw new InvalidOperationException("Original sourceBody/33renderer/twelve-piece physics rig required.");
                    foreach(string role in SportsTruckFragmentRecipe.BodyRoles.Concat(SportsTruckFragmentRecipe.WheelRoles))if(rig.pieces.Count(piece=>piece.source.name==role)!=1)throw new InvalidOperationException("Exact twelve original physical role names required.");
                    gear[i]=plan.equipment(i).Concat(views.cabs).Distinct().ToArray();if(gear[i].Any(part=>part==null||bodies[i].Concat(tires[i]).Any(renderer=>renderer.transform==part||renderer.transform.IsChildOf(part))))throw new InvalidOperationException("Required gear overlaps original body rendering; preserve custom assembly.");
                    var smoke=truck.transform.Find("Exhaust/ExhaustSmokeSemi")?.GetComponent<ParticleSystemRenderer>();if(smoke==null)throw new InvalidOperationException("Original NWH smoke required.");
                    auxiliary[i]=gear[i].Where(part=>part.IsChildOf(truck.transform)).SelectMany(part=>part.GetComponentsInChildren<Renderer>(true)).Concat(new Renderer[]{smoke}).Distinct().ToArray();
                    var known=new HashSet<Renderer>(bodies[i].Concat(tires[i]).Concat(auxiliary[i]).Concat(rig.pieces.Select(piece=>piece.renderer)));if(truck.GetComponentsInChildren<Renderer>(true).Any(renderer=>!known.Contains(renderer)))throw new InvalidOperationException("Unknown truck rendering retained; inspect exact additional equipment before extending this binding.");
                }
                for(int v=0;v<4;v++){var existing=views.cabs[v].GetComponent<SportsTruckAppearanceCabBinding>();if(existing!=null&&(existing.drivingViews!=views||existing.viewIndex!=v))throw new InvalidOperationException("Custom cab appearance binding retained.");}
                var hosts=new List<SportsTruckAppearanceHost>(match.appearanceHosts??Array.Empty<SportsTruckAppearanceHost>());
                for(int i=0;i<plan.count;i++)
                {
                    var truck=plan.trucks[i];var host=truck.gameObject.AddComponent<SportsTruckAppearanceHost>();host.catalog=catalog;host.participantSlot=i;host.presentationParent=truck.transform;host.originalBodyRenderers=bodies[i];host.originalWheelRenderers=tires[i];host.auxiliaryExteriorRenderers=auxiliary[i];host.wheels=truck.wheels;host.protectedRoots=gear[i];
                    var bounds=LocalBounds(truck.transform,bodies[i].Concat(tires[i]));host.compatibleBodies=catalog.bodies.Select(body=>Fit(body,bounds,truck.transform,truck.wheels)).ToArray();foreach(var fit in host.compatibleBodies)fit.notice+=" This sport retains its original physical truck and twelve wreck-piece envelopes.";
                    var fragment=truck.gameObject.AddComponent<SportsTruckFragmentPresentation>();fragment.authoredVersion=1;fragment.host=host;fragment.rig=plan.rigs[i];fragment.recipes=recipes.ToArray();fragment.recipeBodyIds=recipes.Select(recipe=>recipe.bodyId).ToArray();host.fragmentPresentation=fragment;truck.appearanceHost=host;plan.rigs[i].appearanceHost=host;plan.rigs[i].fragmentPresentation=fragment;hosts.Add(host);EditorUtility.SetDirty(fragment);EditorUtility.SetDirty(host);EditorUtility.SetDirty(truck);EditorUtility.SetDirty(plan.rigs[i]);
                }
                for(int i=0;i<4;i++){var cab=Cab(views.cabs[i]);if(cab.drivingViews!=null&&cab.drivingViews!=views)throw new InvalidOperationException("Custom cab binding retained.");cab.drivingViews=views;cab.viewIndex=i;EditorUtility.SetDirty(cab);}
                match.appearanceHosts=hosts.ToArray();match.appearanceFraming=(match.appearanceFraming??Array.Empty<SportsBroadcastFraming>()).Concat(roots.SelectMany(root=>root.GetComponentsInChildren<SportsBroadcastFraming>(true)).Where(framing=>framing.session==match)).Distinct().ToArray();EditorUtility.SetDirty(match);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                Debug.Log("SPORTS_ACTUAL_BURST_APPEARANCE_BOUND "+mode+" hosts="+plan.count+" seven private licensed render recipes; original twelve physical bodies/colliders/forces and replay masks preserved. Native proof pending.");
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        [MenuItem("Truck Sports/Customizer/Repair Missing Owned Private Fragment Recipe References")]
        public static void RepairMissingPrivateFragmentRecipeReferences()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");
            foreach(string mode in BurstModes)
            {
                string path="Assets/"+mode+"/Scenes/"+mode+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(scene.isLoaded&&scene.isDirty)throw new InvalidOperationException("Save existing scene edits first.");if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                try
                {
                    bool changed=false;foreach(var presenter in scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<SportsTruckFragmentPresentation>(true)))
                    {
                        if(presenter.authoredVersion!=1||presenter.host==null||presenter.rig==null||presenter.host.fragmentPresentation!=presenter||presenter.rig.fragmentPresentation!=presenter||presenter.recipes==null||presenter.recipeBodyIds==null||presenter.recipes.Length!=presenter.recipeBodyIds.Length)continue;
                        for(int i=0;i<presenter.recipes.Length;i++)if(presenter.recipes[i]==null){var recipe=AssetDatabase.LoadAssetAtPath<SportsTruckFragmentRecipe>(SportsTruckFragmentRecipeAuthoring.Folder+"/"+presenter.recipeBodyIds[i]+".asset");var body=presenter.host.catalog.FindBody(presenter.recipeBodyIds[i]);if(recipe==null||!recipe.ValidFor(body,out _))throw new InvalidOperationException("Regenerate the licensed private recipe before repairing its null reference.");presenter.recipes[i]=recipe;EditorUtility.SetDirty(presenter);changed=true;}
                    }
                    if(changed){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
                }
                finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            }
        }
    }
}
