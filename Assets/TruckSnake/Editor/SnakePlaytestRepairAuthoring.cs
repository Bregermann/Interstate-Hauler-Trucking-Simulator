using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CompassNavigatorPro;
using LWS.TruckSports;
using LWS.InterstateHauler;

namespace LWS.TruckSnake.Editor
{
    public static class SnakePlaytestRepairAuthoring
    {
        const string Infill="Owned Snake roadside town infill";
        public static void UpgradeTraffic()
        {
            var scene=EditorSceneManager.OpenScene(SnakeAuthoring.Scene);var match=UnityEngine.Object.FindAnyObjectByType<SnakeMatch>();
            InstallTrafficLanes(match);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        public static void InstallTrafficLanes(SnakeMatch match)
        {
            if(match==null||match.traffic==null)throw new InvalidOperationException("Saved Snake UTS adapter required.");
            if(match.traffic.cityLanes!=null&&match.traffic.cityLanes.Length>0)return;
            // The interstate lane builder intentionally rejects LocalRoads;
            // Snake's old authoring replaced these valid Taxi district loops
            // with an empty result. Restore the actual reused district lanes,
            // rather than introducing another traffic/navigation system.
            var source=EditorSceneManager.OpenPreviewScene(LWS.TruckFareFight.Editor.FareFightAuthoring.Scene);
            try
            {
                var original=source.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<LWS.TruckFareFight.FareFightMatch>(true)).Single();
                var lanes=original.traffic.cityLanes.Where(lane=>lane?.centerline!=null&&lane.centerline.Length>=5&&lane.centerline.All(match.world.Contains)).Select(lane=>JsonUtility.FromJson<LwsTrafficLaneDefinition>(JsonUtility.ToJson(lane))).ToArray();
                if(lanes.Length==0)throw new InvalidOperationException("The original district has no usable authored UTS loops; inspect source references.");
                match.traffic.cityLanes=lanes;if(match.pedestrians.roadLanes==null||match.pedestrians.roadLanes.Length==0)match.pedestrians.roadLanes=lanes;
                EditorUtility.SetDirty(match.traffic);EditorUtility.SetDirty(match.pedestrians);Debug.Log("SNAKE_TRAFFIC restored "+lanes.Length+" original district UTS loops; existing prefabs and right-of-way logic retained.");
            }
            finally{EditorSceneManager.ClosePreviewScene(source);}
        }
        public static void ValidateSavedRepair()
        {
            EditorSceneManager.OpenScene(SnakeAuthoring.Scene);var match=UnityEngine.Object.FindAnyObjectByType<SnakeMatch>();var root=GameObject.Find(Infill);
            if(match==null||root==null||root.transform.childCount<16||match.GetComponent<SnakePickupGuidance>()==null)throw new InvalidOperationException("Saved Snake world/owned infill/CNP integration missing.");
            foreach(Transform child in root.transform)
            {
                if(child.GetComponentsInChildren<MeshRenderer>().Length==0)throw new InvalidOperationException("Actual owned model missing: "+child.name);
                if(LwsRoadGraphQuery.TryFindNearestRoad(match.world.graph,child.position,20,out _))throw new InvalidOperationException("Infill occupies the reserved long-train road corridor: "+child.name);
                if(Mathf.Abs(child.position.y-match.world.Height(child.position))>.1f)throw new InvalidOperationException("Roadside base is not terrain-grounded: "+child.name);
            }
            foreach(var truck in match.trucks)
            {
                var handling=truck.tractorMotor.handling;
                if(!Mathf.Approximately(handling.lowSpeedSteeringLock,50)||!Mathf.Approximately(handling.citySteeringLock,36)||!Mathf.Approximately(handling.launchPowerAtRest,4.2f))throw new InvalidOperationException("Expected stock-only Snake NWH instance overrides were not saved.");
                if(!Mathf.Approximately(truck.tractorMotor.vehicle.powertrain.transmission.shiftDuration,.18f))throw new InvalidOperationException("Mode-only NWH shift responsiveness was not saved.");
            }
            if(match.traffic.maxFullTraffic!=48||match.traffic.maxVisibleTraffic!=48||match.pedestrians.maxFullPedestrians!=100)throw new InvalidOperationException("Original live-actor budgets changed unexpectedly.");
            if(match.traffic.cityLanes==null||match.traffic.cityLanes.Length==0)throw new InvalidOperationException("Saved UTS lane definitions are empty.");
            if(match.traffic.densityProfile!=match.pedestrians.densityProfile||match.traffic.densityProfile.maximumActivePedestrians!=210||match.traffic.densityProfile.maximumActiveTraffic!=60)throw new InvalidOperationException("Sport-owned logical population configuration not shared by existing UTS adapters.");
            Debug.Log("SNAKE_SAVED_REPAIR native models/road clearance/terrain bases, four NWH overrides, unchanged live budgets and shared UTS profile validated.");
        }
        public static void UpgradeTown()
        {
            var scene=EditorSceneManager.OpenScene(SnakeAuthoring.Scene);var match=UnityEngine.Object.FindAnyObjectByType<SnakeMatch>();
            if(match==null)throw new InvalidOperationException("Saved Snake world required.");
            if(GameObject.Find(Infill)!=null){Debug.Log("SNAKE_TOWN preserve existing customized infill");return;}
            var root=new GameObject(Infill).transform;int trees=0,buildings=0;
            foreach(var edge in match.world.graph.edges)
            {
                if(edge.roadClass==LwsRoadClass.Ramp||edge.fromNodeId.StartsWith("taxi.stop")||edge.toNodeId.StartsWith("taxi.stop"))continue;
                for(int sample=2;sample<edge.samples.Count-2;sample+=4)
                {
                    var road=edge.samples[sample];if(Mathf.Abs(road.position.x)>650||Mathf.Abs(road.position.z)>650||Mathf.Abs(road.position.y-match.world.Height(road.position))>2)continue;
                    var forward=Vector3.ProjectOnPlane(road.forward,Vector3.up).normalized;if(forward.sqrMagnitude<.5f)continue;var right=Vector3.Cross(Vector3.up,forward);var rotation=Quaternion.LookRotation(forward);
                    foreach(int side in new[]{-1,1})
                    {
                        if(trees<32)
                        {
                            var point=road.position+right*side*(Mathf.Max(15,road.roadWidthMeters*.5f)+8);point.y=match.world.Height(point);
                            if(Clear(match,point,new Vector3(3,5,3),rotation))
                            {
                                var holder=new GameObject("Roadside shade tree "+trees).transform;holder.SetParent(root,false);holder.SetPositionAndRotation(point,rotation);
                                LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(holder,"Assets/JC_LP_MegaCity/Prefabs/Nature/SM_Nature_Tree_"+(trees%2==0?"03":"07")+".prefab",new Vector3(6,10,6),0,false,"TruckSnake");
                                var trunk=holder.gameObject.AddComponent<CapsuleCollider>();trunk.center=Vector3.up*2;trunk.height=4;trunk.radius=.35f;trees++;Physics.SyncTransforms();
                            }
                        }
                        if(buildings<12)
                        {
                            var point=road.position+right*side*(Mathf.Max(15,road.roadWidthMeters*.5f)+34);point.y=match.world.Height(point);
                            if(Clear(match,point,new Vector3(15,13,15),rotation))
                            {
                                var holder=new GameObject("Connected roadside property "+buildings).transform;holder.SetParent(root,false);holder.SetPositionAndRotation(point,rotation*Quaternion.Euler(0,side<0?90:-90,0));
                                string model=buildings%2==0?"SM_Buildings_Commercial_03":"SM_Buildings_House_04_V1";
                                LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(holder,"Assets/JC_LP_MegaCity/Prefabs/Buildings/"+model+".prefab",new Vector3(24,24,24),0,false,"TruckSnake");
                                foreach(var mesh in holder.GetComponentsInChildren<MeshFilter>())if(mesh.sharedMesh!=null)mesh.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh.sharedMesh;
                                buildings++;Physics.SyncTransforms();
                            }
                        }
                    }
                    if(trees==32&&buildings==12)break;
                }
                if(trees==32&&buildings==12)break;
            }
            if(trees<12||buildings<4)throw new InvalidOperationException("Road clearance rejected most infill; inspect placement before saving. trees="+trees+" buildings="+buildings);
            var profile=match.traffic.densityProfile;
            if(profile==null||match.pedestrians.densityProfile!=profile||AssetDatabase.GetAssetPath(profile)!="Assets/TruckSnake/Configuration/World population.asset")throw new InvalidOperationException("Only the existing sport-owned population profile may be changed.");
            if(Mathf.Approximately(profile.pedestrianDensityMultiplier,1.25f))profile.pedestrianDensityMultiplier=1.75f;
            if(Mathf.Approximately(profile.trafficDensityMultiplier,1.3f))profile.trafficDensityMultiplier=1.65f;
            if(profile.maximumActivePedestrians==150)profile.maximumActivePedestrians=210;
            if(profile.maximumActiveTraffic==48)profile.maximumActiveTraffic=60;
            // Logical population increases; existing full physics and visible
            // actor budgets, UTS lane rules and streaming radii stay intact.
            EditorUtility.SetDirty(profile);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/TruckSports/PlaytestRepair");File.WriteAllText("Logs/TruckSports/PlaytestRepair/snake-town-authoring.txt","Saved owned infill: trees="+trees+" buildings="+buildings+"; logical pedestrian target="+profile.PedestrianTarget(120)+" traffic="+profile.TrafficTarget(36)+"; live physics budgets unchanged.");
            Debug.Log("SNAKE_TOWN trees="+trees+" buildings="+buildings);
        }
        static bool Clear(SnakeMatch match,Vector3 point,Vector3 half,Quaternion rotation)
        {
            if(!match.world.Contains(point))return false;
            // Reserve both the road and the train's turn/runoff corridor.
            if(LwsRoadGraphQuery.TryFindNearestRoad(match.world.graph,point,Mathf.Max(20,half.x+18),out _))return false;
            foreach(var collider in Physics.OverlapBox(point+Vector3.up*half.y,half,rotation,~0,QueryTriggerInteraction.Ignore))
            {
                if(collider is TerrainCollider)continue;
                if(collider.bounds.max.y<=point.y+.6f)continue;
                return false;
            }
            foreach(var start in match.world.starts)if((start-point).sqrMagnitude<45*45)return false;
            return true;
        }
        public static void UpgradeHandling()
        {
            var scene=EditorSceneManager.OpenScene(SnakeAuthoring.Scene);var match=UnityEngine.Object.FindAnyObjectByType<SnakeMatch>();
            if(match==null||match.trucks.Length!=4)throw new InvalidOperationException("Four saved Snake NWH tractors required.");
            if(Mathf.Approximately(match.minimumSpeed,7))match.minimumSpeed=4.5f;EditorUtility.SetDirty(match);
            foreach(var truck in match.trucks)
            {
                var handling=truck.tractorMotor.handling;if(handling==null)throw new InvalidOperationException("Keep the existing public Taxi/NWH handling adapter.");
                // Migrate only known stock values. Every field stays editable,
                // and intentional designer alternatives remain untouched.
                if(Mathf.Approximately(handling.launchPowerAtRest,3.4f))handling.launchPowerAtRest=4.2f;
                if(Mathf.Approximately(handling.launchPowerAt15Mph,2.2f))handling.launchPowerAt15Mph=2.8f;
                if(Mathf.Approximately(handling.lowSpeedSteeringLock,70))handling.lowSpeedSteeringLock=50;
                if(Mathf.Approximately(handling.citySteeringLock,40))handling.citySteeringLock=36;
                if(Mathf.Approximately(handling.steeringResponse,400))handling.steeringResponse=500;
                if(Mathf.Approximately(handling.lowSpeedSmoothing,.025f))handling.lowSpeedSmoothing=.015f;
                PrefabUtility.RecordPrefabInstancePropertyModifications(handling);EditorUtility.SetDirty(handling);
                var vehicle=truck.tractorMotor.vehicle;if(Mathf.Approximately(vehicle.powertrain.transmission.shiftDuration,.35f))vehicle.powertrain.transmission.shiftDuration=.18f;
                PrefabUtility.RecordPrefabInstancePropertyModifications(vehicle);EditorUtility.SetDirty(vehicle);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("SNAKE_REPAIR mode-only public NWH handling values saved; shared Taxi and vendor assets untouched.");
        }
        public static void UpgradeGuidance()
        {
            var scene=EditorSceneManager.OpenScene(SnakeAuthoring.Scene);var match=UnityEngine.Object.FindAnyObjectByType<SnakeMatch>();
            if(match==null)throw new InvalidOperationException("Preserve and open the saved Snake world.");
            var guide=match.GetComponent<SnakePickupGuidance>();
            if(guide!=null){if(guide.compasses?.Length!=4||guide.targets?.Length!=4)throw new InvalidOperationException("Inspect customized Snake objective references.");return;}
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckSports/Resources/Broadcast objective compass.prefab");if(prefab==null)throw new InvalidOperationException("Existing owned CNP prefab required.");
            guide=match.gameObject.AddComponent<SnakePickupGuidance>();guide.match=match;guide.compasses=new SportsBroadcastCompass[4];guide.targets=new CompassProPOI[4];
            for(int view=0;view<4;view++)
            {
                var marker=new GameObject("Owned reachable trailer objective "+view);marker.transform.SetParent(match.transform,false);marker.SetActive(false);
                var target=marker.AddComponent<CompassProPOI>();target.compassGroupMask=1<<3;target.canBeVisited=false;target.hideWhenVisited=false;target.dontDestroyOnLoad=false;target.showOnScreenIndicator=true;target.showOffScreenIndicator=true;target.ignoreAreaOfInterest=true;target.playAudioClipWhenVisited=false;target.title="";target.iconNonVisited=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Plugins/Kronnect/CompassNavigatorPro/Resources/CNPro/Sprites/icon-arrow-white.png");guide.targets[view]=target;
                var root=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);root.name="Owned trailer objective compass "+view;root.transform.SetParent(match.transform,false);root.SetActive(false);
                var compass=root.GetComponent<SportsBroadcastCompass>();compass.permittedTargets=new[]{target};compass.showOnScreenIndicators=true;compass.showOffScreenIndicators=true;compass.dontDestroyOnLoad=false;guide.compasses[view]=compass;
                PrefabUtility.RecordPrefabInstancePropertyModifications(compass);EditorUtility.SetDirty(compass);
            }
            EditorUtility.SetDirty(guide);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("SNAKE_REPAIR four saved CNP objective views; existing world, handling, traffic and convoy retained.");
        }
    }
}
