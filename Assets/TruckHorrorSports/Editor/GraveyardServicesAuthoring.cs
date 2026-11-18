#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using EasyRoads3Dv3;
using PixelCrushers.DialogueSystem;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using LWS.TruckHitchSports.Editor;
namespace LWS.TruckHorrorSports.Editor
{
    public static class GraveyardServicesAuthoring
    {
        const string RootName="Authored contextual night services";
        const string Folder="TruckHorrorSports/GraveyardShift";
        const string Cabinet="Assets/LeartesStudios/Garage Warehouse Environment/Art/Prefabs/SM_ToolCabinet_01.prefab";
        const string Reel="Assets/ConstructionPropsBundle/URP/Prefabs/SM_CableReel01 Variant.prefab";
        [MenuItem("Truck Sports/Alpha 0.10/Upgrade active Graveyard night services")]
        public static void UpgradeActiveVenue()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=HorrorAuthoring.Scene(HorrorSport.GraveyardShift))throw new InvalidOperationException("Open the saved Graveyard Shift scene.");
            var match=UnityEngine.Object.FindObjectsByType<HorrorMatch>(FindObjectsSortMode.None).Single(m=>m.gameObject.scene==scene);
            var serialized=new SerializedObject(match);if(serialized.FindProperty("nightServices")==null)throw new InvalidOperationException("Integrate the documented HorrorMatch nightServices hooks before authoring.");
            var existing=scene.GetRootGameObjects().FirstOrDefault(go=>go.name==RootName);
            if(existing!=null)
            {if(serialized.FindProperty("nightServices").objectReferenceValue!=existing.GetComponent<GraveyardNightServices>())throw new InvalidOperationException("The existing night-service root has lost its match binding; repair the binding rather than duplicating content.");Debug.Log("GRAVEYARD_SERVICES_PRESERVED");return;}
            Need<GameObject>(Cabinet);Need<GameObject>(Reel);Need<GameObject>(HitchAuthoring.TractorSource);
            Need<GameObject>("Assets/EasyRoads3D/Resources/ERRoadNetwork.prefab");Need<Material>("Assets/TruckSnake/Materials/Connected world road asphalt.mat");Need<SportsBroadcastTheme>("Assets/TruckSports/Resources/SportsBroadcastTheme.asset");
            foreach(string sound in new[]{"Assets/Heat - Complete Modern UI/Audio/Click.wav","Assets/WeatherMaker/Prefab/Sound/Ambient/WeatherMakerAmbientSound_EnergyHum.ogg","Assets/NWH/Vehicle Physics 2/Sound/Engine/Start/EngineStart.wav","Assets/NWH/Vehicle Physics 2/Sound/Engine/Running/EngineRunning - Truck1.wav"})Need<AudioClip>(sound);
            var breaker=scene.GetRootGameObjects().Single(g=>g.name=="Breaker service cabinet");var desk=scene.GetRootGameObjects().Single(g=>g.name=="CB radio desk");
            var root=new GameObject(RootName);var services=root.AddComponent<GraveyardNightServices>();services.match=match;
            var metal=SportsEventAuthoring.Mat(Folder,"Night service brushed metal",new Color(.15f,.2f,.18f),.35f);
            var red=SportsEventAuthoring.Mat(Folder,"Night service worn red grip",new Color(.48f,.08f,.045f),.12f);
            var lever=LeverMesh();
            services.animatedProps=new Transform[4];services.animations=new Animation[4];services.sequences=new Sequencer[4];
            services.animatedProps[0]=Control(root.transform,"Pump03 emergency nozzle and shutoff",new Vector3(9,2.6f,3.7f),lever,red,Vector3.one*.65f);
            services.animatedProps[1]=Control(root.transform,"Breaker reset handle",breaker.transform.position+new Vector3(0,2.3f,-1.1f),lever,red,Vector3.one*.6f);
            var radio=Child(root.transform,"Original compact CB receiver",desk.transform.position+new Vector3(0,2.1f,-.2f));
            Visual(radio,Cabinet,new Vector3(1.1f,.5f,.55f));
            services.animatedProps[2]=Control(radio,"CB19 handset",radio.position+new Vector3(.62f,.35f,0),lever,metal,new Vector3(.4f,.7f,.4f));
            var generator=Child(root.transform,"Owned auxiliary generator cabinet",new Vector3(52,0,56));Visual(generator,Cabinet,new Vector3(3,2.5f,2));
            services.animatedProps[3]=Child(root.transform,"Owned auxiliary generator flywheel",new Vector3(53.5f,1.2f,54.9f));Visual(services.animatedProps[3],Reel,new Vector3(1.3f,1.3f,.6f));
            string[] clips={"PumpCheck","BreakerOn","CBAnswer","GeneratorRun"};
            for(int i=0;i<4;i++)
            {
                var prop=services.animatedProps[i];var animation=services.animations[i]=prop.gameObject.AddComponent<Animation>();animation.playAutomatically=false;
                var clip=ServiceClip(clips[i],i);animation.AddClip(clip,clips[i]);animation.clip=clip;services.sequences[i]=prop.gameObject.AddComponent<Sequencer>();
            }
            services.pumpLight=Lamp(root.transform,"Pump03 check indicator",new Vector3(9,4,4),new Color(1,.62f,.16f),2);
            services.breakerLight=Lamp(root.transform,"Breaker current restored",breaker.transform.position+new Vector3(0,3.4f,-1.2f),new Color(.23f,1,.6f),1.5f);
            services.generatorLight=Lamp(root.transform,"Generator working lamp",new Vector3(52,3,55),new Color(.65f,.85f,1),2);
            services.pumpReadout=Label(root.transform,"Pump03 actual audit meter",new Vector3(9,4.5f,3.6f),"PUMP 03 · CHECK REQUIRED",4);
            services.cbReadout=Label(root.transform,"CB19 carrier display",radio.position+new Vector3(0,.65f,-.4f),"CB 19 · NO CARRIER",1.6f);
            services.cbInteraction=Child(root.transform,"Actual second-call interaction beside CB",radio.position+new Vector3(-.8f,-1.2f,-1));
            services.generatorReadout=Label(root.transform,"Auxiliary power readout",new Vector3(52,3.8f,54.8f),"AUXILIARY POWER OFF",3);
            string click="Assets/Heat - Complete Modern UI/Audio/Click.wav",hum="Assets/WeatherMaker/Prefab/Sound/Ambient/WeatherMakerAmbientSound_EnergyHum.ogg";
            string engineStart="Assets/NWH/Vehicle Physics 2/Sound/Engine/Start/EngineStart.wav",engineRun="Assets/NWH/Vehicle Physics 2/Sound/Engine/Running/EngineRunning - Truck1.wav";
            Vector3[] stations={match.objectives[0].transform.position,new Vector3(9,2,4),services.animatedProps[1].position,radio.position,match.objectives[4].transform.position,new Vector3(52,2,56)};
            services.stations=new AudioSource[6];for(int i=0;i<6;i++)services.stations[i]=Sound(root.transform,"Owned spatial service cue "+i,stations[i],i==5?engineStart:i==3?hum:click,false);
            services.pumpMotor=Sound(root.transform,"Pump03 self-operating motor",new Vector3(9,2,4),hum,true);services.generatorMotor=Sound(root.transform,"Auxiliary generator physical location",new Vector3(52,2,56),engineRun,true);
            services.visitor=Arrival(root.transform,match);
            serialized.FindProperty("nightServices").objectReferenceValue=services;serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(match);EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("GRAVEYARD_SERVICES_AUTHORED original task-driven pump/breaker/CB/generator, Pixel Crushers prop sequences, native physical repeat visitor, participant captions; caller saves and performs focused gameplay/audio acceptance.");
        }
        static GraveyardDriverlessArrival Arrival(Transform parent,HorrorMatch match)
        {
            var owner=Child(parent,"Same registration driverless arrivals",Vector3.zero);var arrival=owner.gameObject.AddComponent<GraveyardDriverlessArrival>();arrival.startPoint=48;arrival.arrivalPoint=0;arrival.road=new Transform[64];
            for(int i=0;i<64;i++){float angle=i*Mathf.PI*2/64;arrival.road[i]=Child(owner,"Physical exterior lane waypoint "+i,new Vector3(Mathf.Sin(angle)*115,1,-150+Mathf.Cos(angle)*60));}
            var truck=arrival.truck=HitchAuthoring.CreateTractor(match,0,arrival.road[48].position);truck.name="The same driverless visitor · native NWH";truck.transform.SetParent(owner,true);truck.transform.rotation=Quaternion.LookRotation(arrival.road[49].position-arrival.road[48].position);
            arrival.headlights=new Light[2];for(int side=0;side<2;side++){arrival.headlights[side]=Lamp(truck.transform,"Driverless visitor headlight "+side,truck.transform.TransformPoint(new Vector3(side==0?-.75f:.75f,1.8f,2)),new Color(.85f,.88f,.67f),5);arrival.headlights[side].type=LightType.Spot;arrival.headlights[side].range=45;arrival.headlights[side].spotAngle=58;arrival.headlights[side].transform.rotation=truck.transform.rotation*Quaternion.Euler(7,0,0);arrival.headlights[side].enabled=false;}
            var asset=Need<GameObject>("Assets/EasyRoads3D/Resources/ERRoadNetwork.prefab");var networkObject=(GameObject)PrefabUtility.InstantiatePrefab(asset);networkObject.name="Owned exterior EasyRoads arrival loop";networkObject.transform.SetParent(owner,true);
            var modular=networkObject.GetComponent<ERModularBase>();modular.RoadNetworkInit();var network=new ERRoadNetwork(modular);
            var points=arrival.road.Select(p=>new Vector3(p.position.x,.12f,p.position.z)).Concat(new[]{new Vector3(arrival.road[0].position.x,.12f,arrival.road[0].position.z)}).ToArray();
            var type=new ERRoadType{roadTypeName="Last Stop exterior service road",roadWidth=12,hasMeshCollider=true,roadMaterial=Need<Material>("Assets/TruckSnake/Materials/Connected world road asphalt.mat"),layer=0};
            var road=network.CreateRoad("Physical repeat arrival route",type,points,Enumerable.Repeat(ERMarkerControlType.StraightXZ,points.Length).ToArray());road.SetTerrainDeformation(false,false);road.SnapToTerrain(false,false);road.Refresh();network.BuildRoadNetwork(false,false,false);modular.enabled=false;
            var shapes=networkObject.GetComponentsInChildren<MeshCollider>(true);if(shapes.Length==0)throw new InvalidOperationException("The exterior arrival road needs real vendor-authored collision meshes.");
            int segment=0;foreach(var collider in shapes){var surface=collider.GetComponent<LWS.InterstateHauler.LwsRoadSurface>()??collider.gameObject.AddComponent<LWS.InterstateHauler.LwsRoadSurface>();surface.Configure("HH10_LAST_STOP_SERVICE_ROAD","HH10_LAST_STOP_"+(segment++),LWS.InterstateHauler.LwsRoadSurfaceType.AsphaltInterstate,"The Last Stop exterior road");}
            return arrival;
        }
        static AnimationClip ServiceClip(string name,int action)
        {
            string path="Assets/"+Folder+"/Models/"+name+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(clip!=null)return clip;
            clip=new AnimationClip{name=name,legacy=true,wrapMode=action==3?WrapMode.Loop:WrapMode.ClampForever};
            AnimationCurve curve=action==2?new AnimationCurve(new Keyframe(0,0),new Keyframe(.3f,12),new Keyframe(.9f,0)):AnimationCurve.Linear(0,0,action==3?1:.6f,action==3?360:action==0?-45:65);
            clip.SetCurve("",typeof(Transform),action==3||action==2?"localEulerAnglesRaw.z":"localEulerAnglesRaw.x",curve);AssetDatabase.CreateAsset(clip,path);return clip;
        }
        static Mesh LeverMesh()
        {
            string path="Assets/"+Folder+"/Models/Original service-control grip.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh!=null)return mesh;
            // Purpose-authored grip silhouette for owned service equipment, not replacement venue geometry.
            Vector2[] outline={new Vector2(-.1f,0),new Vector2(.1f,0),new Vector2(.1f,.5f),new Vector2(.35f,.5f),new Vector2(.35f,.7f),new Vector2(-.35f,.7f),new Vector2(-.35f,.5f),new Vector2(-.1f,.5f)};
            var vertices=new Vector3[outline.Length*2];for(int side=0;side<2;side++)for(int i=0;i<outline.Length;i++)vertices[side*outline.Length+i]=new Vector3(outline[i].x,outline[i].y,side==0?-.08f:.08f);
            int[] faces={0,1,2,0,2,7,7,2,3,7,3,4,7,4,5,7,5,6};var triangles=new System.Collections.Generic.List<int>();triangles.AddRange(faces);for(int i=0;i<faces.Length;i+=3){triangles.Add(faces[i]+8);triangles.Add(faces[i+2]+8);triangles.Add(faces[i+1]+8);}for(int i=0;i<8;i++){int j=(i+1)%8;triangles.AddRange(new[]{i,j,j+8,i,j+8,i+8});}
            mesh=new Mesh{name="Original worn service grip"};mesh.vertices=vertices;mesh.triangles=triangles.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static Transform Control(Transform parent,string name,Vector3 position,Mesh mesh,Material material,Vector3 scale){var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.position=position;go.transform.localScale=scale;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;return go.transform;}
        static Transform Child(Transform parent,string name,Vector3 position){var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=position;return go.transform;}
        static void Visual(Transform parent,string path,Vector3 size)=>LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(parent,path,size,0,false,Folder);
        static Light Lamp(Transform parent,string name,Vector3 p,Color color,float intensity){var light=Child(parent,name,p).gameObject.AddComponent<Light>();light.color=color;light.range=8;light.intensity=intensity;light.enabled=false;return light;}
        static AudioSource Sound(Transform parent,string name,Vector3 p,string clip,bool loop){var source=Child(parent,name,p).gameObject.AddComponent<AudioSource>();source.clip=Need<AudioClip>(clip);source.playOnAwake=false;source.loop=loop;source.spatialBlend=1;source.minDistance=2;source.maxDistance=38;source.rolloffMode=AudioRolloffMode.Linear;return source;}
        static TMP_Text Label(Transform parent,string name,Vector3 p,string text,float size){var label=Child(parent,name,p).gameObject.AddComponent<TextMeshPro>();var theme=Need<SportsBroadcastTheme>("Assets/TruckSports/Resources/SportsBroadcastTheme.asset");label.font=theme.headingFont;label.text=text;label.fontSize=size;label.alignment=TextAlignmentOptions.Center;label.color=new Color(.57f,1,.72f);label.richText=false;label.rectTransform.sizeDelta=new Vector2(9,3);return label;}
        static T Need<T>(string path)where T:UnityEngine.Object{var result=AssetDatabase.LoadAssetAtPath<T>(path);if(result==null)throw new InvalidOperationException("Existing owned night-service asset missing: "+path);return result;}
    }
}
#endif
