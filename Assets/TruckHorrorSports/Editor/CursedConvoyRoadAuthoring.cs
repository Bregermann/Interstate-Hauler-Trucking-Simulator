#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EasyRoads3Dv3;
using TMPro;
using PixelCrushers.DialogueSystem;
using LWS.TruckSports;
namespace LWS.TruckHorrorSports.Editor
{
    public static class CursedConvoyRoadAuthoring
    {
        const string RootName="Authored haunted convoy roads and receivers";
        const string Folder="TruckHorrorSports/CursedCargo";
        const string Sign="Assets/RoadPropsPack/URP/Prefabs/RoadSign_URP.prefab";
        const string Warehouse="Assets/JC_LP_MegaCity/Prefabs/Buildings/SM_Buildings_Warehouse_01.prefab";
        const string Person="Assets/UTS_FullPack/Models/People/RagDoll Prefabs/Man_11.prefab";
        const string Controller="Assets/TruckHorrorSports/GraveyardShift/Models/Harvest watcher locomotion.controller";
        static Vector3 P(float x,float z)=>new Vector3(x,1,z);
        [MenuItem("Truck Sports/Alpha 0.10/Upgrade active Cursed haunted convoy roads")]
        public static void UpgradeActiveVenue()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=HorrorAuthoring.Scene(HorrorSport.CursedCargo))throw new InvalidOperationException("Open the saved Cursed Cargo scene.");
            var match=UnityEngine.Object.FindObjectsByType<HorrorMatch>(FindObjectsSortMode.None).Single(m=>m.gameObject.scene==scene);var serialized=new SerializedObject(match);var binding=serialized.FindProperty("convoyRoad");if(binding==null)throw new InvalidOperationException("Integrate the documented HorrorMatch convoyRoad hooks first.");
            var existing=scene.GetRootGameObjects().FirstOrDefault(g=>g.name==RootName);
            if(existing!=null){if(binding.objectReferenceValue!=existing.GetComponent<CursedConvoyRoad>())throw new InvalidOperationException("Existing convoy root has lost its match binding.");Debug.Log("CURSED_CONVOY_ROAD_PRESERVED");return;}
            Need<GameObject>(Sign);Need<GameObject>(Warehouse);Need<GameObject>(Person);Need<RuntimeAnimatorController>(Controller);Need<GameObject>("Assets/EasyRoads3D/Resources/ERRoadNetwork.prefab");Need<Material>("Assets/TruckSnake/Materials/Connected world road asphalt.mat");
            var theme=Need<SportsBroadcastTheme>("Assets/TruckSports/Resources/SportsBroadcastTheme.asset");if(theme.headingFont==null)throw new InvalidOperationException("Reuse the Sports heading font.");
            var root=new GameObject(RootName);var route=root.AddComponent<CursedConvoyRoad>();route.match=match;
            var geometry=Child(root.transform,"Option1 owned haunted roads and receiving depots",Vector3.zero);route.geometry=geometry.gameObject;
            Vector3[][] lines={new[]{P(0,-50),P(0,-15),P(0,20),P(0,45)},new[]{P(0,45),P(0,75),P(-35,105),P(-75,110),P(-75,140)},new[]{P(-75,140),P(-68,180),P(-68,235),P(-105,270),P(-170,285),P(-210,300),P(-250,300)},new[]{P(-250,300),P(-290,300),P(-325,300),P(-350,300)}};
            Roads(geometry,lines);route.legs=new CursedConvoyLeg[4];route.wards=new Transform[3];
            for(int leg=0;leg<4;leg++)
            {
                var points=new Transform[lines[leg].Length];for(int i=0;i<points.Length;i++){points[i]=Child(geometry,"Convoy leg "+leg+" steering station "+i,lines[leg][i]);Vector3 tangent=lines[leg][Mathf.Min(i+1,points.Length-1)]-lines[leg][Mathf.Max(0,i-1)];points[i].rotation=Quaternion.LookRotation(tangent);}
                route.legs[leg]=new CursedConvoyLeg{points=points};if(leg<3)route.wards[leg]=points[points.Length-1];else route.receivingGate=points[points.Length-1];
            }
            route.bayLabels=new TMP_Text[12];route.bayLights=new Light[12];
            for(int ward=0;ward<3;ward++)for(int slot=0;slot<4;slot++)
            {int index=ward*4+slot;Vector3 p=route.wards[ward].position+route.wards[ward].right*((slot-1.5f)*12);route.bayLabels[index]=Label(geometry,"Owned convoy receiver name "+index,p+Vector3.up*4,"CONTAIN ORIGINAL LOAD",theme,3);route.bayLabels[index].transform.rotation=route.wards[ward].rotation;route.bayLights[index]=Lamp(geometry,"Convoy receiving-bay signal "+index,p+Vector3.up*3,new Color(.35f,.8f,.7f),1.5f);}
            for(int ward=0;ward<3;ward++)
            {Vector3 p=route.wards[ward].position+route.wards[ward].right*38;var sign=Child(geometry,"Owned containment depot sign "+ward,p);Visual(sign,Sign,new Vector3(6,7,2));var caption=Label(geometry,"Roadside ward instruction "+ward,p+Vector3.up*5,"WARD "+(ward+1)+"\nEVERY ORIGINAL LOAD MUST STOP",theme,4);caption.transform.rotation=route.wards[ward].rotation;}
            var receiving=Child(geometry,"Owned final haunted receiving warehouse",new Vector3(-350,0,355));Visual(receiving,Warehouse,new Vector3(70,24,40));Label(geometry,"Final convoy receiving sign",new Vector3(-350,10,333),"FINAL RECEIVING\nDO NOT OPEN THE TRAILERS",theme,7);
            Vector3[] triggers={P(-35,105),P(-68,215),P(-210,300)};route.encounters=new Transform[3];route.apparitions=new GameObject[3];route.apparitionLights=new Light[3];route.sequences=new Sequencer[3];
            for(int i=0;i<3;i++)
            {
                route.encounters[i]=Child(geometry,"Physical haunted-road encounter "+i,triggers[i]);Vector3 position=triggers[i]+(i==2?Vector3.forward:Vector3.right)*40;position.y=0;
                var person=Child(geometry,"The same loading clerk at an impossible depot "+i,position);Visual(person,Person,new Vector3(1.1f,2.3f,1.1f));
                var animator=person.GetComponentInChildren<Animator>(true);if(animator==null)throw new InvalidOperationException("The owned apparition requires its original humanoid animator.");animator.runtimeAnimatorController=Need<RuntimeAnimatorController>(Controller);animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                person.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(triggers[i]-position,Vector3.up));route.apparitions[i]=person.gameObject;route.sequences[i]=person.gameObject.AddComponent<Sequencer>();person.gameObject.SetActive(false);
                route.apparitionLights[i]=Lamp(geometry,"Unnatural receiving lamp "+i,position+Vector3.up*3,new Color(.5f,.72f,.62f),2);route.apparitionLights[i].enabled=false;
            }
            route.unrest=Need<AudioClip>("Assets/WeatherMaker/Prefab/Sound/Ambient/WeatherMakerAmbientSound_EnergyHum.ogg");route.thump=Need<AudioClip>("Assets/NWH/Vehicle Physics 2/Sound/Suspension/SuspensionThump.wav");route.cargoSounds=new AudioSource[4];
            for(int i=0;i<4;i++)
            {
                var sound=Child(match.cargo[i].transform,"Inside original haunted load · owned spatial cue",match.cargo[i].transform.TransformPoint(new Vector3(0,2,-6))).gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.spatialBlend=1;sound.minDistance=3;sound.maxDistance=55;sound.rolloffMode=AudioRolloffMode.Linear;route.cargoSounds[i]=sound;
            }
            // Only the convoy variant opens this crossing through the existing forest.
            // Preserve the designer's original active states for ordinary delivery.
            route.defaultObjects=scene.GetRootGameObjects().Where(g=>g.name=="Containment freight circuit" || g.name=="Forest silhouette"&&DistanceToRoad(g.transform.position,lines)<39).ToArray();
            geometry.gameObject.SetActive(false);binding.objectReferenceValue=route;serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(match);EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("CURSED_CONVOY_ROAD_AUTHORED option1 only, original native rigs/wards rule authority preserved; new wide EasyRoads legs, lane bays, original-load spatial noise and finite animated encounters. Caller saves and tests.");
        }
        static void Roads(Transform parent,Vector3[][] lines)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(Need<GameObject>("Assets/EasyRoads3D/Resources/ERRoadNetwork.prefab"));go.name="Owned haunted convoy EasyRoads network";go.transform.SetParent(parent,true);var modular=go.GetComponent<ERModularBase>();modular.RoadNetworkInit();var network=new ERRoadNetwork(modular);
            for(int leg=0;leg<lines.Length;leg++)
            {var points=lines[leg].Select(p=>new Vector3(p.x,.14f,p.z)).ToArray();var type=new ERRoadType{roadTypeName="Wide haunted convoy leg "+leg,roadWidth=64,hasMeshCollider=true,roadMaterial=Need<Material>("Assets/TruckSnake/Materials/Connected world road asphalt.mat"),layer=0};var road=network.CreateRoad("Haunted freight road "+leg,type,points,Enumerable.Repeat(ERMarkerControlType.Spline,points.Length).ToArray());road.SetTerrainDeformation(false,false);road.SnapToTerrain(false,false);road.Refresh();}
            network.BuildRoadNetwork(false,false,false);modular.enabled=false;var shapes=go.GetComponentsInChildren<MeshCollider>(true);if(shapes.Length==0)throw new InvalidOperationException("Physical convoy roads require vendor-generated collision meshes.");int segment=0;foreach(var collider in shapes){var surface=collider.GetComponent<LWS.InterstateHauler.LwsRoadSurface>()??collider.gameObject.AddComponent<LWS.InterstateHauler.LwsRoadSurface>();surface.Configure("HH10_HAUNTED_CONVOY","HH10_CONVOY_"+(segment++),LWS.InterstateHauler.LwsRoadSurfaceType.AsphaltInterstate,"Haunted freight road");}
        }
        static float DistanceToRoad(Vector3 position,Vector3[][] lines)
        {float best=float.MaxValue;foreach(var line in lines)for(int i=1;i<line.Length;i++){Vector3 a=line[i-1],b=line[i];a.y=b.y=position.y;Vector3 d=b-a;float t=d.sqrMagnitude>.001f?Mathf.Clamp01(Vector3.Dot(position-a,d)/d.sqrMagnitude):0;best=Mathf.Min(best,Vector3.Distance(position,a+d*t));}return best;}
        static TMP_Text Label(Transform parent,string name,Vector3 p,string caption,SportsBroadcastTheme theme,float size){var label=Child(parent,name,p).gameObject.AddComponent<TextMeshPro>();label.font=theme.headingFont;label.text=caption;label.fontSize=size;label.alignment=TextAlignmentOptions.Center;label.richText=false;label.color=new Color(.72f,.85f,.67f);label.rectTransform.sizeDelta=new Vector2(24,7);return label;}
        static Light Lamp(Transform parent,string name,Vector3 p,Color color,float intensity){var light=Child(parent,name,p).gameObject.AddComponent<Light>();light.color=color;light.intensity=intensity;light.range=12;return light;}
        static Transform Child(Transform parent,string name,Vector3 position){var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=position;return go.transform;}
        static void Visual(Transform parent,string source,Vector3 size)=>LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(parent,source,size,0,false,Folder);
        static T Need<T>(string path)where T:UnityEngine.Object{var source=AssetDatabase.LoadAssetAtPath<T>(path);if(source==null)throw new InvalidOperationException("Existing owned convoy-road asset missing: "+path);return source;}
    }
}
#endif
