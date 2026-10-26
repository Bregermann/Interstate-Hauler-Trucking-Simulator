using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckHockey;
using LWS.TruckAirHockey;
using LWS.TruckBilliards;
using LWS.TruckPoker;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckSports.Editor
{
    public static partial class SportsTruckAppearanceHostAuthoring
    {
        [MenuItem("Truck Sports/Customizer/Bind Missing Static Hockey Air Billiards And Poker Hosts")]
        public static void BindMissingStaticSportHosts()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required.");var catalog=AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");var source=AssetDatabase.LoadAssetAtPath<GameObject>(FacadeSource);if(catalog==null||!catalog.Validate(out _)||source==null)throw new InvalidOperationException("Existing validated catalog and installed GR3D source required.");
            foreach(string mode in new[]{"TruckHockey","TruckAirHockey","TruckBilliards","TruckPoker"})BindStaticSport(mode,catalog,source);
        }
        static void BindStaticSport(string mode,SportsTruckAppearanceCatalog catalog,GameObject source)
        {
            string path="Assets/"+mode+"/Scenes/"+mode+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(scene.isLoaded&&scene.isDirty)throw new InvalidOperationException("Save existing scene edits first.");if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var roots=scene.GetRootGameObjects();Component owner;Transform[] actors,models;Transform[][] equipment;Renderer[][] auxiliary;Action<SportsTruckAppearanceHost[]> bind;int count;
                if(mode=="TruckHockey")
                {
                    var match=roots.SelectMany(root=>root.GetComponentsInChildren<HockeyMatch>(true)).Single();owner=match;count=6;if(match.trucks.Length!=6||match.players.Length!=6||match.labels.Length!=6||match.puck==null||match.puckContact==null||match.impacts==null)throw new InvalidOperationException("Original six hockey owners/contact authority required.");if(match.appearanceHosts.Length>0){Debug.Log("SPORTS_STATIC_HOSTS_PRESERVED existing Hockey mapping.");return;}actors=match.trucks.Select(truck=>truck.transform).ToArray();models=actors.Select(actor=>actor.Find("NWH GR3D visual geometry")).ToArray();equipment=match.trucks.Select(truck=>{var stick=truck.stick;if(stick==null||stick.owner!=truck||stick.blade==null||stick.bladeBody==null||stick.bladeVisual==null||stick.shaft==null)throw new InvalidOperationException("Original articulated hockey stick authority required.");return new[]{stick.transform,stick.bladeBody.transform,stick.bladeVisual,stick.shaft.transform,match.puck.transform}.Concat(match.truckGoalBarriers.Select(shape=>shape.transform)).ToArray();}).ToArray();auxiliary=actors.Select(actor=>new[]{actor.Find("Team bumper")?.GetComponent<Renderer>(),actor.Find("Participant label")?.GetComponent<Renderer>()}).ToArray();bind=hosts=>match.appearanceHosts=hosts;
                }
                else if(mode=="TruckAirHockey")
                {
                    var match=roots.SelectMany(root=>root.GetComponentsInChildren<AirHockeyMatch>(true)).Single();owner=match;count=2;if(match.trucks.Length!=2||match.players.Length!=2||match.puck==null||match.puckContact==null)throw new InvalidOperationException("Original two physical mallets/puck required.");if(match.appearanceHosts.Length>0){Debug.Log("SPORTS_STATIC_HOSTS_PRESERVED existing Air Hockey mapping.");return;}actors=match.trucks.Select(truck=>truck.transform).ToArray();models=actors.Select(actor=>actor.Find("NWH GR3D visual geometry")).ToArray();equipment=actors.Select(actor=>new[]{match.puck.transform}.Concat(match.truckBarriers.Select(shape=>shape.transform)).ToArray()).ToArray();auxiliary=actors.Select(actor=>Array.Empty<Renderer>()).ToArray();bind=hosts=>match.appearanceHosts=hosts;
                }
                else if(mode=="TruckBilliards")
                {
                    var match=roots.SelectMany(root=>root.GetComponentsInChildren<BilliardsSession>(true)).Single();owner=match;count=2;if(match.players.Length!=2||match.match==null||match.match.player==null||match.match.ai==null)throw new InvalidOperationException("Original two collider-free billiards visual rigs required.");if(match.appearanceHosts.Length>0){Debug.Log("SPORTS_STATIC_HOSTS_PRESERVED existing Billiards mapping.");return;}actors=new[]{match.match.player.transform,match.match.ai.transform};models=actors.Select(actor=>actor.Find("NWH GR3D visual only")).ToArray();equipment=actors.Select(actor=>match.match.balls.Select(ball=>ball.transform).Concat(new[]{match.match.preview}).ToArray()).ToArray();auxiliary=actors.Select(actor=>new[]{actor.Find("Colored front bumper")?.GetComponent<Renderer>(),actor.Find("Truck label")?.GetComponent<Renderer>()}).ToArray();bind=hosts=>match.appearanceHosts=hosts;
                }
                else
                {
                    var match=roots.SelectMany(root=>root.GetComponentsInChildren<PokerMatch>(true)).Single();owner=match;count=8;if(match.avatars.Length!=8||match.presentation==null||match.presentation.expressions.Length!=8||match.dealerDrive==null||match.presentation.dealer==null||match.presentation.dealHand==null||match.cabAnchors.Length!=4)throw new InvalidOperationException("Original eight seated truck participants and protected dealer/cockpits required.");if((match.appearanceHosts?.Length??0)>0){Debug.Log("SPORTS_STATIC_HOSTS_PRESERVED existing Poker mapping.");return;}actors=match.avatars;models=match.presentation.expressions.Select(expression=>expression.Find("NWH GR3D visual geometry")).ToArray();var protectedPoker=new[]{match.presentation.dealer,match.presentation.dealHand,match.dealerDrive.vehicle,match.dealerDrive.hand}.Concat(match.cabAnchors).Concat(match.presentation.chips).Concat(match.presentation.board.Select(card=>card.transform)).Concat(match.presentation.seatCards.Select(card=>card.transform)).ToArray();equipment=actors.Select(actor=>protectedPoker).ToArray();auxiliary=actors.Select(actor=>Array.Empty<Renderer>()).ToArray();bind=hosts=>match.appearanceHosts=hosts;
                }
                if(actors.Length!=count||actors.Distinct().Count()!=count||actors.Any(actor=>actor==null)||actors.Any(actor=>actor.GetComponent<SportsTruckAppearanceHost>()!=null)){Debug.Log("SPORTS_STATIC_HOSTS_PRESERVED "+mode+" existing custom/partial actors retained.");return;}
                var body=new Renderer[count][];var tires=new Renderer[count][];
                for(int i=0;i<count;i++)
                {
                    var actor=actors[i];var model=models[i];if(model==null||model==actor||!model.IsChildOf(actor)||!SportsTruckAppearanceWheelMath.TryUniformScale(actor.lossyScale,out _)||actor.GetComponentsInChildren<NWH.WheelController3D.WheelController>(true).Length!=0||actor.GetComponent<SportsFragmentRig>()!=null||model.GetComponentsInChildren<Collider>(true).Length!=0||model.GetComponentsInChildren<Rigidbody>(true).Length!=0)throw new InvalidOperationException("Only original render-only static wheel models; no new physics/controllers supported.");
                    body[i]=ArenaOriginalRenderers(model,source.transform,FacadeBodyPaths);tires[i]=ArenaOriginalRenderers(model,source.transform,FacadeWheelPaths);var known=new HashSet<Renderer>(body[i].Concat(tires[i]).Concat(auxiliary[i]));if(auxiliary[i].Any(renderer=>renderer==null)||model.GetComponentsInChildren<Renderer>(true).Length!=33||actor.GetComponentsInChildren<Renderer>(true).Any(renderer=>!known.Contains(renderer))||equipment[i].Any(part=>part==null||part==model||part.IsChildOf(model)||model.IsChildOf(part)))throw new InvalidOperationException("Unknown/custom static model or equipment rendering retained for inspection.");
                    if((mode=="TruckBilliards"||mode=="TruckPoker")&&(actor.GetComponentsInChildren<Collider>(true).Length!=0||actor.GetComponentsInChildren<Rigidbody>(true).Length!=0))throw new InvalidOperationException("Collider-free original sport rigs must remain collider-free.");
                }
                var hosts=new SportsTruckAppearanceHost[count];for(int i=0;i<count;i++){hosts[i]=CreateStaticHost(actors[i],models[i],body[i],tires[i],catalog);hosts[i].participantSlot=i;hosts[i].protectedRoots=equipment[i];hosts[i].auxiliaryExteriorRenderers=auxiliary[i];hosts[i].modeOwnedSlots=new[]{SportsTruckAppearanceSlots.Dashboard};EditorUtility.SetDirty(hosts[i]);}
                bind(hosts);EditorUtility.SetDirty(owner);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("SPORTS_STATIC_HOSTS_BOUND "+mode+" owners="+count+" exact33 original meshes and four real source-transform wheel centers; zero added controllers/physics; native proof pending.");
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        static SportsTruckAppearanceHost CreateStaticHost(Transform root,Transform model,Renderer[] body,Renderer[] tires,SportsTruckAppearanceCatalog catalog)
        {
            var poses=tires.Select(renderer=>{var filter=renderer.GetComponent<MeshFilter>();var size=filter.sharedMesh.bounds.size;float diameter=new[]{Vector3.right*size.x,Vector3.up*size.y,Vector3.forward*size.z}.Max(axis=>root.InverseTransformVector(filter.transform.TransformVector(axis)).magnitude);return new SportsTruckStaticWheelPose{source=renderer.transform,center=filter.sharedMesh.bounds.center,diameter=diameter};}).ToArray();
            var bounds=LocalBounds(root,body.Concat(tires));var fits=catalog.bodies.Select(item=>FitStatic(item,bounds,root,poses)).ToArray();var host=root.gameObject.AddComponent<SportsTruckAppearanceHost>();host.catalog=catalog;host.presentationParent=model;host.originalBodyRenderers=body;host.originalWheelRenderers=tires;host.staticWheelPoses=poses;host.compatibleBodies=fits;return host;
        }
        static SportsTruckAppearanceCompatibility FitStatic(SportsTruckAppearanceCatalog.Body body,Bounds target,Transform root,SportsTruckStaticWheelPose[] poses)
        {
            var fit=Fit(body,target,root,Array.Empty<NWH.WheelController3D.WheelController>());if(body.wheelMounts.Length!=4||poses.Length!=4)throw new InvalidOperationException("Explicit four-wheel catalog/source geometry required.");var rotation=Quaternion.Euler(body.presentationEuler);var points=body.wheelMounts.Select(mount=>rotation*mount.position).ToArray();var actual=poses.Select(pose=>root.InverseTransformPoint(pose.source.TransformPoint(pose.center))).ToArray();var unused=new HashSet<int>(Enumerable.Range(0,4));var renderers=body.visualPrefab.GetComponentsInChildren<Renderer>(true);var names=new HashSet<string>(body.wheelRendererNames);var groups=new List<SportsTruckAppearanceCompatibility.WheelGeometry>();
            for(int i=0;i<4;i++){var goal=Normalize(actual[i],actual);int index=unused.OrderBy(candidate=>(Normalize(points[candidate],points)-goal).sqrMagnitude).First();unused.Remove(index);var mount=body.wheelMounts[index];var meshIndices=Enumerable.Range(0,renderers.Length).Where(renderer=>names.Contains(renderers[renderer].name)&&Enumerable.Range(0,4).OrderBy(candidate=>(body.wheelMounts[candidate].position-body.visualPrefab.transform.InverseTransformPoint(renderers[renderer].bounds.center)).sqrMagnitude).First()==index).ToArray();if(meshIndices.Length==0)throw new InvalidOperationException("Catalog has no real tire geometry for static wheel group: "+body.id);bool large=body.id=="truck.monster"||body.id=="truck.lifted";groups.Add(new SportsTruckAppearanceCompatibility.WheelGeometry{physicalWheel=i,rendererIndices=meshIndices,center=mount.position,euler=(rotation*Quaternion.Euler(mount.euler)).eulerAngles,diameter=mount.scale.x,visualDiameter=Mathf.Clamp(mount.scale.x*fit.scale.x,poses[i].diameter*(large?1.15f:.8f),poses[i].diameter*(large?1.5f:1.1f))});}
            fit.wheelGeometry=groups.ToArray();fit.notice="Existing render-only wheel transforms supply position and rotation; this sport retains its original physical envelope.";return fit;
        }
    }
}
