using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using TMPro;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using LWS.TruckFareFight.Editor;
using LWS.InterstateHauler;
namespace LWS.TruckFloorIsLava.Editor
{
    public static class LavaAuthoring
    {
        public const string Folder="TruckFloorIsLava",Scene="Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity";
        public static void UpgradeSurfaceBindings()
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(Scene);var m=UnityEngine.Object.FindAnyObjectByType<LavaMatch>();
            foreach(var floor in m.platforms)
            {
                if(floor.GetComponent<MeshRenderer>()!=null)continue;var original=floor.plate;
                floor.gameObject.AddComponent<MeshFilter>().sharedMesh=original.GetComponent<MeshFilter>().sharedMesh;
                floor.plate=floor.gameObject.AddComponent<MeshRenderer>();floor.plate.sharedMaterials=original.sharedMaterials;
                UnityEngine.Object.DestroyImmediate(original.gameObject);EditorUtility.SetDirty(floor);
            }
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        public static void UpgradeAudio()
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(Scene);var m=UnityEngine.Object.FindAnyObjectByType<LavaMatch>();
            m.feedback.clips[3]=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/WeatherMaker/Prefab/Sound/Weather/WeatherMakerThunderIntense1.mp3");EditorUtility.SetDirty(m.feedback);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        public static void UpgradeGrade()
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(Scene);var m=UnityEngine.Object.FindAnyObjectByType<LavaMatch>();
            GradeCameras(m);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        static void GradeCameras(LavaMatch m)
        {
            var cameras=new[]{m.broadcast.view}.Concat(m.drivingViews.rally.views.Select(v=>v.view));
            foreach(var camera in cameras){camera.allowHDR=true;var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.volumeLayerMask|=1;EditorUtility.SetDirty(camera);EditorUtility.SetDirty(data);}
        }
        public static void Create()
        {
            if(File.Exists(Scene))throw new InvalidOperationException("Preserve the saved volcanic arena; use explicit targeted upgrades.");
            var m=SportsEventAuthoring.Begin<LavaMatch>(Folder);m.setup.count=8;m.option=1;m.NormalizeSetup();m.feedback.clips=LWS.TruckHockey.Editor.HockeyAudioAuthoring.Create();m.feedback.clips[3]=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/WeatherMaker/Prefab/Sound/Weather/WeatherMakerThunderIntense1.mp3");
            var rock=SportsEventAuthoring.Mat(Folder,"Weathered basalt platform strata",new Color(.20f,.24f,.27f),.1f);
            rock.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/NWH/Common/Textures/Ground/CliffAlbedo.png");rock.mainTextureScale=Vector2.one*4;
            rock.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/NWH/Common/Textures/Ground/CliffNormal.png"));rock.SetFloat("_BumpScale",.5f);rock.EnableKeyword("_NORMALMAP");rock.EnableKeyword("_EMISSION");
            var metal=SportsEventAuthoring.Mat(Folder,"Burnished volcanic event steel",new Color(.24f,.13f,.07f),.45f);
            var cracks=SportsEventAuthoring.Mat(Folder,"Luminous first-touch fracture seams",new Color(1,.22f,.025f),.1f);cracks.EnableKeyword("_EMISSION");cracks.SetColor("_EmissionColor",new Color(3,.35f,.01f));
            var contact=SportsEventAuthoring.Contact(Folder,"NWH basalt platform contact",.9f,.02f);
            var splash=Volcano(m);
            m.platformGroup=new GameObject("Finite physical collapsing platform network").transform;m.platforms=new LavaPlatform[25];
            for(int i=0;i<25;i++)
            {
                LavaPlatformKind kind=i==12?LavaPlatformKind.Wide:i%9==0?LavaPlatformKind.TemporarySafe:i%7==0?LavaPlatformKind.ShortWarning:i%6==0?LavaPlatformKind.Tilting:i%5==0?LavaPlatformKind.Moving:i%4==0?LavaPlatformKind.Narrow:LavaPlatformKind.Ordinary;
                var p=new GameObject("Physical "+kind+" platform "+i);p.transform.SetParent(m.platformGroup);p.transform.position=new Vector3((i%5-2)*48,42,(i/5-2)*48);
                var floor=p.AddComponent<LavaPlatform>();m.platforms[i]=floor;floor.kind=kind;floor.halfSize=new Vector2(kind==LavaPlatformKind.Narrow?17:kind==LavaPlatformKind.Wide?23:22,kind==LavaPlatformKind.Wide?23:22);
                floor.warningSeconds=kind==LavaPlatformKind.Wide?24:kind==LavaPlatformKind.TemporarySafe?30:kind==LavaPlatformKind.ShortWarning?5:kind==LavaPlatformKind.Tilting?9:12;
                floor.restingColor=kind==LavaPlatformKind.TemporarySafe?new Color(.18f,.36f,.3f):kind==LavaPlatformKind.ShortWarning?new Color(.42f,.24f,.08f):kind==LavaPlatformKind.Moving?new Color(.2f,.28f,.4f):new Color(.25f,.28f,.31f);
                p.AddComponent<MeshFilter>().sharedMesh=Slab(kind,floor.halfSize);floor.plate=p.AddComponent<MeshRenderer>();floor.plate.sharedMaterial=rock;
                var collider=p.AddComponent<BoxCollider>();collider.size=new Vector3(floor.halfSize.x*2,2,floor.halfSize.y*2);collider.center=Vector3.down;collider.sharedMaterial=contact;floor.surface=collider;
                var marker=p.AddComponent<LWS.TruckTaxi.TruckTaxiSurface>();marker.isRoad=false;
                floor.body=p.AddComponent<Rigidbody>();floor.body.mass=24000;floor.body.isKinematic=true;floor.body.interpolation=RigidbodyInterpolation.Interpolate;floor.body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;floor.body.solverIterations=16;floor.body.solverVelocityIterations=8;floor.body.maxAngularVelocity=.7f;
                if(kind==LavaPlatformKind.Tilting)
                {
                    var joint=p.AddComponent<ConfigurableJoint>();floor.tilt=joint;joint.autoConfigureConnectedAnchor=false;joint.connectedAnchor=p.transform.position;joint.xMotion=joint.yMotion=joint.zMotion=ConfigurableJointMotion.Locked;joint.angularYMotion=ConfigurableJointMotion.Locked;joint.angularXMotion=joint.angularZMotion=ConfigurableJointMotion.Limited;joint.lowAngularXLimit=new SoftJointLimit{limit=-6};joint.highAngularXLimit=joint.angularZLimit=new SoftJointLimit{limit=6};joint.angularXDrive=joint.angularYZDrive=new JointDrive{positionSpring=3500000,positionDamper=950000,maximumForce=float.MaxValue};
                }
                floor.cracks=new GameObject("Visible branching collapse fractures").transform;floor.cracks.SetParent(p.transform,false);
                for(int line=0;line<3;line++)
                {
                    var seam=new GameObject("Branching molten crack "+line).AddComponent<LineRenderer>();seam.transform.SetParent(floor.cracks,false);seam.useWorldSpace=false;seam.sharedMaterial=cracks;seam.widthMultiplier=.12f;seam.positionCount=7;
                    for(int k=0;k<7;k++){float x=Mathf.Lerp(-floor.halfSize.x+.8f,floor.halfSize.x-.8f,k/6f);seam.SetPosition(k,new Vector3(x,.055f,(line-1)*10+Mathf.Sin(k*2.1f+i)*2));}
                }
                floor.cracks.gameObject.SetActive(false);
                for(int edge=0;edge<4;edge++)
                {
                    bool vertical=edge<2;float side=edge%2==0?-1:1;var a=SportsEventAuthoring.Box("Riveted suspension edge rim",vertical?new Vector3(side*(floor.halfSize.x-.25f),-.6f,0):new Vector3(0,-.6f,side*(floor.halfSize.y-.25f)),vertical?new Vector3(.45f,1.2f,floor.halfSize.y*2):new Vector3(floor.halfSize.x*2,1.2f,.45f),metal,null,p.transform);
                    // Render-only edging stays below the matching flat collider.
                }
            }
            Graph(m);m.trucks=new SportsEventTruck[8];m.fragments=new SportsFragmentRig[8];m.blasts=new ParticleSystem[8];m.splashes=new ParticleSystem[8];
            for(int i=0;i<8;i++)
            {
                var truck=FareFightVehicleAuthoring.Spawn(m,i,m.Grid(i),Quaternion.Euler(0,i<4?0:180,0));m.trucks[i]=truck;foreach(var wheel in truck.wheels)wheel.otherBodyForceScale=1;
                var hit=truck.gameObject.AddComponent<LavaContacts>();hit.match=m;hit.slot=i;m.fragments[i]=SportsFragmentAuthoring.InstallRigid(truck.body,truck.visuals.GetComponentsInChildren<MeshFilter>(true),Folder);
                var fire=new GameObject("Pooled actual lava truck fireball "+i).AddComponent<ParticleSystem>();fire.transform.SetParent(m.transform);SportsChaosAuthoring.FixBlast(fire,SportsChaosAuthoring.Flare(Folder));var main=fire.main;main.startLifetime=new ParticleSystem.MinMaxCurve(.6f,1.5f);main.startSize=new ParticleSystem.MinMaxCurve(5,12);main.startSpeed=new ParticleSystem.MinMaxCurve(12,24);main.simulationSpace=ParticleSystemSimulationSpace.World;m.blasts[i]=fire;
                m.splashes[i]=UnityEngine.Object.Instantiate(splash,m.transform);m.splashes[i].name="Reused pooled molten splash and shockwave "+i;m.splashes[i].Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            m.broadcast=SportsEventAuthoring.Camera("Volcanic platform network broadcast");m.broadcast.view.farClipPlane=5000;m.broadcast.view.orthographic=true;m.framing=m.broadcast.gameObject.AddComponent<SportsCameraFit>();m.framing.cameraTarget=m.broadcast.view;m.framing.arenaFit=true;m.framing.topInset=.1f;m.framing.bottomInset=.24f;m.cameras=new[]{m.broadcast.view};
            SportsEventAuthoring.UI(m);SportsRallyAuthoring.CompactUI(m);m.drivingViews=SportsDrivingViewsAuthoring.Install(m,m.trucks,m.broadcast,Folder);m.drivingViews.independentViewModes=true;
            m.atmosphere=SportsChaosAuthoring.Weather(m,Folder);m.atmosphere.presets=SportsWeatherPresetsAuthoring.EnsureCatalog();m.drivingViews.rally.weather=m.atmosphere;SportsWeatherPickerAuthoring.Install(m,m.atmosphere.presets,new Vector2(350,-180));
            var hud=m.gameObject.AddComponent<SportsExpansionViewHud>();hud.match=m;hud.views=m.drivingViews;var spectator=m.gameObject.AddComponent<SportsExpansionSpectators>();spectator.match=m;spectator.views=m.drivingViews;
            SportsUIAuthoring.Button(m.menus.pages[6].transform,"WATCH THE FINAL ESCAPE","watchlava",new Vector2(0,-245),new Vector2(520,48));Artwork(m);
            SportsBroadcastAuthoring.ApplyScene();GradeCameras(m);SportsEventAuthoring.Save(Folder);Debug.Log("LAVA_AUTHORED25finite contact-activated NWH platforms and reused full crater; graph edges="+m.roads.edges.Count);
        }
        static ParticleSystem Volcano(LavaMatch m)
        {
            const string path="Assets/TruckTargetLaunch/Prefabs/Volcanic launch crater.prefab";var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(source==null)throw new InvalidOperationException("Existing completed volcanic environment required.");
            var root=(GameObject)PrefabUtility.InstantiatePrefab(source);PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);root.name="Reused complete volcanic crater and molten lake";
            var environment=root.GetComponent<LWS.TruckTargetLaunch.TargetVolcano>();m.lava=environment.lava;var splash=environment.splashes[0];root.transform.position-=m.lava.position;
            var terrain=environment.terrain;var data=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/TruckTargetLaunch/Models/Volcanic launch terrain.asset");terrain.terrainData=data;terrain.GetComponent<TerrainCollider>().terrainData=data;
            m.lava.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckTargetLaunch/Materials/Target molten channels.mat");
            foreach(var old in root.GetComponentsInChildren<LWS.TruckTargetLaunch.TargetLavaSurface>(true))UnityEngine.Object.DestroyImmediate(old);
            foreach(var old in root.GetComponentsInChildren<LWS.TruckTerrierRacing.TerrierLavaSurface>(true))UnityEngine.Object.DestroyImmediate(old);
            UnityEngine.Object.DestroyImmediate(environment);
            foreach(var old in m.lava.GetComponents<Collider>())UnityEngine.Object.DestroyImmediate(old);
            var volume=new GameObject("Actual molten lake elimination volume");volume.transform.position=new Vector3(0,-3,0);m.lavaVolume=volume.AddComponent<BoxCollider>();((BoxCollider)m.lavaVolume).size=new Vector3(640,6,640);m.lavaVolume.isTrigger=true;volume.AddComponent<LavaSurface>().match=m;
            root.SetActive(true);foreach(var effect in root.GetComponentsInChildren<ParticleSystem>(true))if(effect.name.Contains("splash"))effect.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var grade=new GameObject("Reused volcanic HDR grade").AddComponent<Volume>();grade.isGlobal=true;grade.priority=20;grade.sharedProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/TruckTargetLaunch/Materials/Volcanic HDR grade.asset");
            return splash;
        }
        static Mesh Slab(LavaPlatformKind kind,Vector2 half)
        {
            string path="Assets/"+Folder+"/Materials/Beveled basalt "+kind+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing!=null)return existing;
            var vertices=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();
            void Face(Vector3 a,Vector3 b,Vector3 c,Vector3 d){int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});uv.AddRange(new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right});triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            float x=half.x,z=half.y,b=.4f;Face(new Vector3(-x,0,-z),new Vector3(-x,0,z),new Vector3(x,0,z),new Vector3(x,0,-z));
            var top=new[]{new Vector3(-x,0,-z),new Vector3(x,0,-z),new Vector3(x,0,z),new Vector3(-x,0,z)};
            for(int i=0;i<4;i++){var a=top[i];var d=top[(i+1)%4];var lowerA=new Vector3(a.x-Mathf.Sign(a.x)*b,-2,a.z-Mathf.Sign(a.z)*b);var lowerD=new Vector3(d.x-Mathf.Sign(d.x)*b,-2,d.z-Mathf.Sign(d.z)*b);Face(a,lowerA,lowerD,d);}
            Face(new Vector3(-x+b,-2,z-b),new Vector3(-x+b,-2,-z+b),new Vector3(x-b,-2,-z+b),new Vector3(x-b,-2,z-b));
            var mesh=new Mesh{name="Original collision-matched beveled basalt "+kind};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static void Graph(LavaMatch m)
        {
            m.roads=LwsRoadGraph.CreateEmpty("lava-finite-jump-network-v1");var from=new List<int>();var to=new List<int>();
            for(int i=0;i<25;i++)m.roads.nodes.Add(new LwsRoadNode{nodeId="platform."+i,position=m.platforms[i].body.position,regionId="lava.arena",sceneChunkId=Folder});
            for(int i=0;i<25;i++)for(int j=i+1;j<25;j++)if(j==i+5||j==i+1&&j/5==i/5)
            {
                var a=m.platforms[i].body.position;var b=m.platforms[j].body.position;var forward=(b-a).normalized;string id="lava.jump."+i+"."+j;
                var edge=new LwsRoadEdge{edgeId=id,roadId=id,segmentId=id+".segment",fromNodeId="platform."+i,toNodeId="platform."+j,roadClass=LwsRoadClass.LocalRoad,direction=LwsRoadDirection.Bidirectional,surfaceType=LwsRoadSurfaceType.Gravel,oneWay=false,distanceMeters=48,travelCost=48,speedLimitMph=30,laneCount=2,laneWidthMeters=8,leftShoulderWidthMeters=2,rightShoulderWidthMeters=2,stateOrRegionId="lava.arena",sceneChunkId=Folder};
                edge.samples.Add(new LwsRoadSample{roadId=id,segmentId=edge.segmentId,position=a,forward=forward,distanceFromStartMeters=0,roadWidthMeters=20,laneWidthMeters=8,laneCount=2,speedLimitMph=30,direction=edge.direction});edge.samples.Add(new LwsRoadSample{roadId=id,segmentId=edge.segmentId,position=b,forward=forward,distanceFromStartMeters=48,roadWidthMeters=20,laneWidthMeters=8,laneCount=2,speedLimitMph=30,direction=edge.direction});m.roads.edges.Add(edge);from.Add(i);to.Add(j);
            }
            m.edgeFrom=from.ToArray();m.edgeTo=to.ToArray();if(!m.roads.Validate().IsValid)throw new InvalidOperationException("Invalid finite platform route graph.");
        }
        static void Artwork(LavaMatch m)
        {
            var logo=SportsRetroMenuAuthoring.LoadLogo(Folder);if(logo==null)throw new InvalidOperationException("Import the original grungy PNG before scene creation.");
            foreach(int index in new[]{0,4}){var page=m.menus.pages[index];var art=SportsUIAuthoring.Rect("Original sport title artwork",page.transform,new Vector2(260,160),new Vector2(-560,365)).AddComponent<UnityEngine.UI.Image>();art.sprite=logo;art.preserveAspect=true;art.raycastTarget=false;var title=page.transform.Find("Title").GetComponent<TMP_Text>();title.rectTransform.anchoredPosition+=Vector2.right*140;title.rectTransform.sizeDelta=new Vector2(1000,title.rectTransform.sizeDelta.y);}
            var material=SportsEventAuthoring.Mat(Folder,"Original Floor Is Lava venue artwork",Color.white);material.shader=Shader.Find("Universal Render Pipeline/Unlit");material.mainTexture=logo.texture;material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",5);material.SetFloat("_DstBlend",10);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
            for(int side=-1;side<=1;side+=2){var sign=GameObject.CreatePrimitive(PrimitiveType.Quad);sign.name="Original volcanic event title sign";UnityEngine.Object.DestroyImmediate(sign.GetComponent<Collider>());sign.transform.position=new Vector3(side*165,72,110);sign.transform.rotation=Quaternion.Euler(0,side<0?35:-35,0);sign.transform.localScale=new Vector3(70,42,1);sign.GetComponent<Renderer>().sharedMaterial=material;}
        }
    }
}
