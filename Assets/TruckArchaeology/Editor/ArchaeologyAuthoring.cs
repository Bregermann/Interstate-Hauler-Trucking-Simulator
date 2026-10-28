using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using LWS.TruckJenga;
using Digger.Modules.Core.Sources;
using Digger.Modules.Core.Editor;
using Digger.Modules.Runtime.Sources;

namespace LWS.TruckArchaeology.Editor
{
    public static class ArchaeologyAuthoring
    {
        public const string Folder="TruckArchaeology",Scene="Assets/TruckArchaeology/Scenes/TruckArchaeology.unity";
        const string Human="Assets/LWS/TruckTaxi/Passengers/Animations/Humanoid/",Dirt="Assets/Digger/Misc/Textures/Ground_Dirt_005_";
        [MenuItem("Truck Sports/Alpha 0.7/Create NEW Archaeology")]
        public static void CreateNewScene()
        {
            if(File.Exists(Scene))throw new InvalidOperationException("New-scene author refuses overwrite: "+Scene);
            if(Shader.Find("Digger/Terrain/URP/Lit")==null)throw new InvalidOperationException("Lead must first intake actual Digger PRO+URP17 shaders and approved optional-assembly configuration; no fallback excavation.");
            var match=SportsEventAuthoring.Begin<ArchaeologyMatch>(Folder);match.setup.count=4;match.option=1;match.NormalizeSetup();match.feedback.clips=Audio();Install(match);SportsEventAuthoring.UI(match);
            for(int i=0;i<4;i++)SportsUIAuthoring.Button(match.menus.hud.transform,"TOOL / CREW "+(i+1),"arch.tool."+i,new Vector2(-600+i*400,-425),new Vector2(370,48));
            match.drivingViews=SportsDrivingViewsAuthoring.Install(match,match.trucks,match.broadcast,Folder);match.drivingViews.independentViewModes=true;SportsEventAuthoring.Save(Folder);
            Debug.Log("ARCHAEOLOGY_NEW_SCENE actual Digger terrain/voxel collider authority; preserve Assets/DiggerData including .internal and StreamingAssets/DiggerData dependency closure. Lead adds original fixed4 appearance/weather/workshop registry bindings. Do not replace Digger terrain shader.");
        }
        public static void Install(ArchaeologyMatch match)
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling||match==null||(!string.IsNullOrEmpty(match.gameObject.scene.path)&&match.gameObject.scene.path!=Scene)||match.transform.Find("Archaeological desert research preserve")!=null)throw new InvalidOperationException("Stopped new unauthored Archaeology scene required.");
            var root=new GameObject("Archaeological desert research preserve").transform;root.SetParent(match.transform,false);
            var sandstone=SportsEventAuthoring.Mat(Folder,"Weathered sandstone",new Color(.65f,.45f,.25f),.12f);sandstone.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/RockFace/rock_face_diff_1k.jpg");
            var fossil=SportsEventAuthoring.Mat(Folder,"Mineralized fossil bone",new Color(.75f,.66f,.47f),.12f);fossil.mainTexture=sandstone.mainTexture;
            var enamel=SportsEventAuthoring.Mat(Folder,"Excavation safety ochre",new Color(.93f,.55f,.07f),.45f);var steel=SportsEventAuthoring.Mat(Folder,"Brushed excavation steel",new Color(.22f,.28f,.31f),.7f);steel.SetFloat("_Metallic",.75f);
            var boneDark=SportsEventAuthoring.Mat(Folder,"Fossil cavity dark mineral",new Color(.16f,.12f,.08f),.08f);
            var woodSource=AssetDatabase.LoadAssetAtPath<Material>("Assets/RoadPropsPack/URP/Materials/M_WoodBox.mat");if(woodSource==null)throw new InvalidOperationException("Owned timber material required.");var wood=new Material(woodSource){name="Research bench timber"};AssetDatabase.CreateAsset(wood,"Assets/TruckArchaeology/Materials/Research bench timber.mat");
            var physics=SportsEventAuthoring.Contact(Folder,"Dry dig site physical contact",.7f,.01f);
            SetupActualDigger(match,root);
            match.trucks=new SportsEventTruck[4];match.equipment=new ArchaeologyEquipment[4];match.benches=new Transform[4];match.receptions=new BoxCollider[4];match.artifacts=new ArchaeologyArtifact[12];
            for(int i=0;i<4;i++)
            {
                Vector3 plot=match.Plot(i);var boundary=Line("Equal original dig plot "+i,enamel,.08f,root);boundary.positionCount=5;boundary.loop=false;boundary.SetPositions(new[]{plot+new Vector3(-15,.04f,-15),plot+new Vector3(-15,.04f,15),plot+new Vector3(15,.04f,15),plot+new Vector3(15,.04f,-15),plot+new Vector3(-15,.04f,-15)});
                var sign=SportsEventAuthoring.Label("Actual scan plot identity","SURVEY "+(i+1)+" / THREE FINDS",plot+new Vector3(0,2.5f,-16),.3f,Color.white);sign.transform.SetParent(root,true);
                match.trucks[i]=Rig(match,i,match.Park(i));match.equipment[i]=Equipment(match,i,root,enamel,steel,wood,physics);
                var bench=SportsEventAuthoring.Box("Physical recovered exhibit bench "+i,new Vector3(plot.x,1.1f,-40),new Vector3(14,.5f,7),wood,physics,root);match.benches[i]=bench.transform;
                for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)SportsEventAuthoring.Box("Exhibit bench real leg",bench.transform.position+new Vector3(x*6,-.6f,z*2.5f),new Vector3(.4f,1.2f,.4f),steel,physics,root);
                var receive=new GameObject("Actual research reception "+i);receive.transform.SetParent(root,false);receive.transform.position=new Vector3(plot.x,2.5f,-29);var volume=receive.AddComponent<BoxCollider>();volume.isTrigger=true;volume.size=new Vector3(12,8,12);match.receptions[i]=volume;
                var label=SportsEventAuthoring.Label("Research reception identity","RESEARCH / "+(i+1),new Vector3(plot.x,1.6f,-35),.35f,Color.white);label.transform.SetParent(root,true);
                Tent(root,new Vector3(plot.x,0,-44),wood,steel);
                for(int kind=0;kind<3;kind++)
                {
                    int id=i*3+kind;Vector3 size=new[]{new Vector3(3,1.6f,2.8f),new Vector3(1.5f,1.3f,4),new Vector3(1.9f,2.5f,1.9f)}[kind];Vector3 center=plot+new Vector3((kind-1)*5.5f,1.8f,0);
                    var g=new GameObject(new[]{"Buried theropod fossil skull","Buried giant dinosaur femur","Buried ancient ceramic amphora"}[kind]+" / plot"+i);g.transform.SetParent(root,false);g.transform.position=center;
                    var box=g.AddComponent<BoxCollider>();box.size=size;box.sharedMaterial=physics;var body=g.AddComponent<Rigidbody>();body.mass=kind==2?55:95;body.isKinematic=true;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.solverIterations=20;
                    var artifact=g.AddComponent<ArchaeologyArtifact>();artifact.match=match;artifact.index=id;artifact.stableId="archaeology.plot."+i+"."+new[]{"skull","femur","amphora"}[kind];artifact.value=new[]{180,140,220}[kind];artifact.body=body;artifact.shape=box;artifact.seedPosition=center;artifact.seedRotation=Quaternion.identity;
                    if(kind==0)Skull(g.transform,fossil,boneDark);else if(kind==1)Femur(g.transform,fossil);else Amphora(g.transform,sandstone,boneDark);
                    artifact.actualModels=g.GetComponentsInChildren<Renderer>(true);if(artifact.actualModels.Length<3)throw new InvalidOperationException("Actual detailed artifact render model required; no hidden placeholder.");match.artifacts[id]=artifact;
                }
                Prop(root,"Assets/LeartesStudios/Garage Warehouse Environment/Art/Prefabs/SM_ToolCabinet_01.prefab",new Vector3(plot.x+11,0,-37),new Vector3(3,2.6f,1.5f));
                Prop(root,"Assets/ConstructionPropsBundle/URP/Prefabs/SM_CableReel01 Variant.prefab",new Vector3(plot.x-10,0,-35),new Vector3(2.5f,2.5f,2.5f));
                Prop(root,"Assets/HandToolsPack/URP/Prefabs/Hammer_GameReady_LODs.prefab",new Vector3(plot.x-3,1.4f,-40),new Vector3(.7f,.6f,1));
                Prop(root,"Assets/HandToolsPack/URP/Prefabs/Vice_GameReady_LODs.prefab",new Vector3(plot.x+5,1.4f,-40),new Vector3(1.2f,.8f,1));
            }
            for(int i=0;i<32;i++){float a=i*Mathf.PI/16;Prop(root,"Assets/PinwheelStudio/Vista/Personal/Samples/Prefabs/VistaSample_DesertRock.prefab",new Vector3(Mathf.Cos(a)*112,0,Mathf.Sin(a)*110),new Vector3(8+i%3*2,9+i%5*3,8+i%4*2));}
            Ruins(root,new Vector3(-86,0,38),sandstone,physics);Ruins(root,new Vector3(80,0,40),sandstone,physics);
            match.workers=Workers(root);match.broadcast=SportsEventAuthoring.Camera("Archaeology original Cinemachine field");match.cameras=new[]{match.broadcast.view};match.broadcast.Pose(new Vector3(0,1,-5),new Vector3(0,90,-110),60,true);
        }
        static void SetupActualDigger(ArchaeologyMatch match,Transform parent)
        {
            const int n=513;var data=new TerrainData{name="Original equal archaeology earth",heightmapResolution=n,size=new Vector3(256,32,256)};var h=new float[n,n];
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){float wx=-128+x*.5f,wz=-128+z*.5f,value=8;for(int p=0;p<4;p++){Vector3 plot=match.Plot(p);float r=Vector2.Distance(new Vector2(wx,wz),new Vector2(plot.x,plot.z));if(r<14)value+=7*Mathf.Pow(Mathf.Max(0,1-r*r/196),1.2f);}float edge=Mathf.Max(Mathf.Abs(wx),Mathf.Abs(wz));if(edge>98)value+=Mathf.Clamp01((edge-98)/15)*(8+6*Mathf.PerlinNoise(wx*.04f,wz*.04f));h[z,x]=value/32;}
            data.SetHeights(0,0,h);var layer=new TerrainLayer{diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Dirt+"COLOR.png"),normalMapTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Dirt+"NORM.png"),tileSize=new Vector2(6,6)};
            if(layer.diffuseTexture==null||layer.normalMapTexture==null)throw new InvalidOperationException("Actual cached Digger textured+normal terrain layer required after lead intake.");
            AssetDatabase.CreateAsset(layer,"Assets/TruckArchaeology/Materials/Actual Digger desert terrain layer.terrainlayer");data.terrainLayers=new[]{layer};AssetDatabase.CreateAsset(data,"Assets/TruckArchaeology/Materials/Original equal archaeology TerrainData.asset");
            var ground=Terrain.CreateTerrainGameObject(data);ground.name="Actual Digger excavatable desert terrain";ground.transform.SetParent(parent,false);ground.transform.position=new Vector3(-128,-8,-128);var terrain=ground.GetComponent<Terrain>();terrain.drawInstanced=true;
            var master=new GameObject("Archaeology actual Digger Master").AddComponent<DiggerMaster>();master.SceneDataFolder="TruckArchaeology";master.ChunkSize=33;master.ResolutionMult=1;master.VoxelHeight=.25f;master.AutoVoxelHeight=false;master.ColliderLodIndex=0;master.CreateLODs=false;master.Layer=0;master.CreateDirs();
            var d=new GameObject("Actual Digger voxel/collider system");d.transform.SetParent(ground.transform,false);var system=d.AddComponent<DiggerSystem>();system.Guid="archaeology-desert-07-01";
            DiggerSystemEditor.Init(system,true);var runtime=new GameObject("Archaeology actual Digger runtime").AddComponent<DiggerMasterRuntime>();runtime.enablePersistence=true;runtime.BufferSize=1;
            match.earth=match.gameObject.AddComponent<ArchaeologyTerrain>();match.earth.runtime=runtime;match.earth.system=system;match.earth.terrain=terrain;
            ArchaeologyDiggerProviderAuthoring.Install(match);
            EditorUtility.SetDirty(master);EditorUtility.SetDirty(system);EditorUtility.SetDirty(runtime);
        }
        static SportsEventTruck Rig(ArchaeologyMatch match,int owner,Vector3 p)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(LWS.TruckFareFight.Editor.FareFightVehicleAuthoring.PrefabPath);if(prefab==null)throw new InvalidOperationException("Original full NWH tractor required, no prefab regeneration.");var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab);g.name="Archaeology original full NWH owner "+owner;g.transform.SetPositionAndRotation(p,Quaternion.identity);var truck=g.GetComponent<SportsEventTruck>();if(truck==null||truck.tractorMotor==null)throw new InvalidOperationException("Original full NWH facade required.");truck.session=match;truck.slot=owner;truck.tractorMotor.settings=match.settings;EditorUtility.SetDirty(truck);EditorUtility.SetDirty(truck.tractorMotor);return truck;
        }
        static ArchaeologyEquipment Equipment(ArchaeologyMatch match,int owner,Transform root,Material yellow,Material steel,Material wood,PhysicsMaterial contact)
        {
            var truck=match.trucks[owner];var eq=truck.gameObject.AddComponent<ArchaeologyEquipment>();eq.match=match;eq.truck=truck;
            var deck=SportsEventAuthoring.Box("Mode-owned physical artifact cargo deck",new Vector3(0,2.25f,-2.5f),new Vector3(4.2f,.3f,5),wood,contact,truck.transform);eq.rack=deck.GetComponent<BoxCollider>();var rack=deck.AddComponent<ArchaeologyRack>();rack.match=match;rack.owner=owner;
            var pivot=Shape(PrimitiveType.Cylinder,"Heavy articulated boom slew bearing",truck.transform,new Vector3(0,2.4f,1.2f),new Vector3(2,.4f,2),steel,false);
            eq.firstBoom=Shape(PrimitiveType.Cylinder,"First articulated excavation boom",root,Vector3.zero,Vector3.one,yellow,false).transform;eq.secondBoom=Shape(PrimitiveType.Cylinder,"Telescoping excavation forearm",root,Vector3.zero,Vector3.one,steel,false).transform;
            var g=new GameObject("Actual kinematic excavation head / crane spreader "+owner);g.transform.SetParent(root,false);g.transform.position=truck.transform.TransformPoint(new Vector3(0,6,5));eq.head=g.AddComponent<Rigidbody>();eq.head.isKinematic=true;eq.head.useGravity=false;var shape=g.AddComponent<SphereCollider>();shape.radius=.25f;ArchaeologySpreaderColliderAuthoring.ConfigureNewHead(shape);eq.headShape=shape;eq.toolVisual=new GameObject("Actual selected tool geometry").transform;eq.toolVisual.SetParent(g.transform,false);eq.toolHeads=new GameObject[6];
            for(int tool=0;tool<6;tool++){var t=new GameObject(((ArchaeologyTool)tool).ToString()+" real tool attachment");t.transform.SetParent(eq.toolVisual,false);eq.toolHeads[tool]=t;Tool(t.transform,tool,yellow,steel,wood);t.SetActive(tool==0);}
            eq.hoist=match.gameObject.AddComponent<JengaCrane>();eq.hoist.spreader=eq.head;eq.hoist.ceiling=28;eq.hoist.parkHeight=10;eq.hoist.travelSpeed=6;eq.hoist.liftSpeed=3;eq.hoist.hoist=Line("Actual research lifting cable "+owner,steel,.035f,root);eq.hoist.hoist.positionCount=2;
            eq.dust=Effect("Assets/CoreCollapse/Demo/Prefabs/Particles/Dust.prefab",root,"Actual Digger tool dust "+owner);return eq;
        }
        static void Tool(Transform root,int tool,Material yellow,Material metal,Material wood)
        {
            if(tool==0){Shape(PrimitiveType.Cube,"Ground penetrating scanner antenna",root,Vector3.zero,new Vector3(1.5f,.25f,1.2f),yellow,false);for(int i=0;i<5;i++)Beam(root,new Vector3(-.6f,.15f,-.45f+i*.22f),new Vector3(.6f,.15f,-.45f+i*.22f),.025f,metal,"Scanner conductive grid");}
            else if(tool==1){Shape(PrimitiveType.Cube,"Excavator curved bucket backing",root,new Vector3(0,.25f,0),new Vector3(2.4f,.5f,1.4f),yellow,false);for(int i=0;i<6;i++)Shape(PrimitiveType.Cube,"Actual bucket cutting tooth",root,new Vector3(-1+i*.4f,-.15f,.6f),new Vector3(.22f,.5f,.3f),metal,false);for(int side=-1;side<=1;side+=2)Shape(PrimitiveType.Cube,"Excavator bucket cheek plate",root,new Vector3(side*1.15f,.1f,0),new Vector3(.12f,.7f,1.5f),yellow,false);}
            else if(tool==2){Shape(PrimitiveType.Cylinder,"Rotating drill motor casing",root,new Vector3(0,.5f,0),new Vector3(.9f,.5f,.9f),yellow,false);for(int i=0;i<28;i++){float a=i*.7f;Beam(root,new Vector3(Mathf.Sin(a)*.24f,-i*.035f,Mathf.Cos(a)*.24f),new Vector3(Mathf.Sin(a+.7f)*.24f,-(i+1)*.035f,Mathf.Cos(a+.7f)*.24f),.06f,metal,"Actual helical drill flight");}}
            else if(tool==3){var nozzle=Shape(PrimitiveType.Cylinder,"Compressed air blower nozzle",root,new Vector3(0,.1f,0),new Vector3(.45f,.6f,.45f),metal,false);Shape(PrimitiveType.Cylinder,"Blower insulated compressor housing",root,new Vector3(0,.8f,0),new Vector3(.8f,.25f,.8f),yellow,false);}
            else if(tool==4){Beam(root,new Vector3(0,.8f,0),Vector3.zero,.12f,wood,"Precision brush handle");for(int x=-2;x<=2;x++)for(int z=-2;z<=2;z++)Beam(root,new Vector3(x*.08f,0,z*.08f),new Vector3(x*.09f,-.3f,z*.09f),.018f,metal,"Real individual brush bristle");}
            else{for(int side=-1;side<=1;side+=2)Beam(root,new Vector3(side*.7f,.4f,0),new Vector3(0,-.2f,0),.035f,metal,"Load sling chain");Shape(PrimitiveType.Cube,"Physical hoist spreader crossbar",root,new Vector3(0,.4f,0),new Vector3(1.7f,.16f,.22f),yellow,false);}
        }
        static void Femur(Transform root,Material material)
        {
            for(int i=0;i<12;i++){float z=-1.55f+i*.28f;Beam(root,new Vector3(Mathf.Sin(i*.25f)*.12f,0,z),new Vector3(Mathf.Sin((i+1)*.25f)*.12f,0,z+.28f),.18f+.07f*Mathf.Abs(z),material,"Mineralized femur curved shaft");}
            foreach(float end in new[]{-1.65f,1.65f})for(int side=-1;side<=1;side+=2)Shape(PrimitiveType.Sphere,"Authentic paired femoral condyle",root,new Vector3(side*.28f,0,end),new Vector3(.72f,.75f,.65f),material,false);
            Shape(PrimitiveType.Sphere,"Femoral head and neck",root,new Vector3(.35f,.2f,1.48f),new Vector3(.65f,.7f,.7f),material,false);
        }
        static void Skull(Transform root,Material bone,Material dark)
        {
            for(int side=-1;side<=1;side+=2)
            {
                for(int i=0;i<12;i++){float t=i/12f,u=(i+1)/12f;Vector3 a=new Vector3(side*(.3f+.65f*t),-.25f+Mathf.Sin(t*Mathf.PI)*.35f,-1.2f+t*2.4f),b=new Vector3(side*(.3f+.65f*u),-.25f+Mathf.Sin(u*Mathf.PI)*.35f,-1.2f+u*2.4f);Beam(root,a,b,.12f,bone,"Fossil jawline / open cranial fenestra");Beam(root,a+Vector3.up*.65f,b+Vector3.up*.65f,.15f,bone,"Theropod maxilla");if(i<9)Tooth(root,a+Vector3.up*.3f,.26f,bone);}
                for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;Beam(root,new Vector3(side*.95f,.28f+Mathf.Sin(a)*.4f,.55f+Mathf.Cos(a)*.45f),new Vector3(side*.95f,.28f+Mathf.Sin(b)*.4f,.55f+Mathf.Cos(b)*.45f),.075f,bone,"Real open orbital rim");}
            }
            for(int i=0;i<6;i++)Beam(root,new Vector3(-.45f+i*.18f,.45f,-1.05f),new Vector3(-.8f+i*.32f,.55f,1.05f),.08f,bone,"Skull roof nasal ridge");
            Beam(root,new Vector3(-.9f,-.2f,1.1f),new Vector3(.9f,-.2f,1.1f),.18f,bone,"Occipital bone bridge");
        }
        static void Tooth(Transform root,Vector3 p,float height,Material material)
        {
            // Tapered actual tooth mesh; a hollow skull stays visibly hollow after excavation.
            const int n=8;var v=new Vector3[n+2];for(int i=0;i<n;i++)v[i]=new Vector3(Mathf.Sin(i*Mathf.PI*2/n)*.075f,0,Mathf.Cos(i*Mathf.PI*2/n)*.075f);v[n]=Vector3.down*height;v[n+1]=Vector3.zero;var t=new List<int>();for(int i=0;i<n;i++){t.AddRange(new[]{i,n,(i+1)%n,i,(i+1)%n,n+1});}var mesh=new Mesh{name="Fossil tapered tooth",vertices=v,triangles=t.ToArray()};mesh.RecalculateNormals();mesh.RecalculateBounds();MeshObject(root,p,mesh,material);
        }
        static void Amphora(Transform root,Material clay,Material ink)
        {
            const int rings=24,sides=48;var v=new Vector3[(rings+1)*(sides+1)];var uv=new Vector2[v.Length];var tri=new List<int>();
            for(int y=0;y<=rings;y++){float t=y/(float)rings,r=t<.15f?Mathf.Lerp(.25f,.65f,t/.15f):t<.65f?.65f+Mathf.Sin((t-.15f)/.5f*Mathf.PI)*.16f:t<.85f?Mathf.Lerp(.65f,.25f,(t-.65f)/.2f):.25f;for(int i=0;i<=sides;i++){float a=i*Mathf.PI*2/sides;int k=y*(sides+1)+i;v[k]=new Vector3(Mathf.Sin(a)*r,-1.15f+t*2.3f,Mathf.Cos(a)*r);uv[k]=new Vector2(i/(float)sides,t);if(y<rings&&i<sides)tri.AddRange(new[]{k,k+1,k+sides+1,k+1,k+sides+2,k+sides+1});}}
            var mesh=new Mesh{name="Actual hollow turned ancient amphora",vertices=v,uv=uv,triangles=tri.ToArray()};mesh.RecalculateNormals();mesh.RecalculateTangents();MeshObject(root,Vector3.zero,mesh,clay);
            for(int side=-1;side<=1;side+=2)for(int i=0;i<16;i++){float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;Beam(root,new Vector3(side*(.45f+Mathf.Sin(a)*.35f),.45f+Mathf.Cos(a)*.5f,0),new Vector3(side*(.45f+Mathf.Sin(b)*.35f),.45f+Mathf.Cos(b)*.5f,0),.06f,clay,"Amphora actual curved handle");}
            for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;Beam(root,new Vector3(Mathf.Sin(a)*.67f,.55f,Mathf.Cos(a)*.67f),new Vector3(Mathf.Sin(b)*.67f,.55f,Mathf.Cos(b)*.67f),.025f,ink,"Faded painted pottery band");}
        }
        static GameObject MeshObject(Transform parent,Vector3 p,Mesh mesh,Material material)
        {
            mesh.name=mesh.name+" "+Guid.NewGuid().ToString("N");AssetDatabase.CreateAsset(mesh,"Assets/TruckArchaeology/Materials/"+mesh.name+".asset");var g=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent,false);g.transform.localPosition=p;g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=material;return g;
        }
        static void Tent(Transform parent,Vector3 p,Material timber,Material steel)
        {
            var root=new GameObject("Detailed field research canvas tent").transform;root.SetParent(parent,false);root.position=p;var canvas=SportsEventAuthoring.Mat(Folder,"Weathered ivory research canvas",new Color(.82f,.73f,.53f),.08f);canvas.SetFloat("_Cull",0);
            for(int side=-1;side<=1;side+=2){for(int z=-1;z<=1;z+=2){Beam(root,new Vector3(side*8,0,z*5),new Vector3(side*8,4,z*5),.12f,steel,"Tent working steel pole");Beam(root,new Vector3(0,7,z*5),new Vector3(side*8,4,z*5),.1f,steel,"Tensioned pitched tent ridge");}var roof=SportsEventAuthoring.Box("Actual pitched canvas roof",new Vector3(side*4,5.5f,0),new Vector3(8.6f,.07f,11),canvas,null,root);roof.transform.localRotation=Quaternion.Euler(0,0,-side*20.56f);for(int z=-1;z<=1;z+=2)Beam(root,new Vector3(side*8,4,z*5),new Vector3(side*10,0,z*7),.025f,steel,"Real tent guy rope");}
        }
        static void Ruins(Transform root,Vector3 p,Material stone,PhysicsMaterial physical)
        {
            for(int side=-1;side<=1;side+=2)for(int row=0;row<7;row++)SportsEventAuthoring.Box("Excavated ancient portal masonry",p+new Vector3(side*5,row*1.2f+.6f,0),new Vector3(2.8f,1.12f,3),stone,physical,root);
            for(int i=0;i<11;i++){float a=i*Mathf.PI/10;var b=SportsEventAuthoring.Box("Ancient vaulted arch wedge",p+new Vector3(Mathf.Cos(a)*5,8+Mathf.Sin(a)*4,0),new Vector3(1.8f,1.4f,3),stone,physical,root);b.transform.rotation=Quaternion.Euler(0,0,a*Mathf.Rad2Deg-90);}
        }
        static Animator[] Workers(Transform root)
        {
            string path="Assets/TruckArchaeology/Materials/Actual archaeology staff.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(path);var machine=controller.layers[0].stateMachine;
            foreach(var pair in new[]{("Idle","idle1"),("Walk","walk"),("Cheer","cheer")}){var clip=AssetDatabase.LoadAllAssetsAtPath(Human+pair.Item2+".FBX").OfType<AnimationClip>().FirstOrDefault(c=>c.humanMotion&&!c.name.StartsWith("__"));if(clip==null&&pair.Item1=="Walk")clip=AssetDatabase.LoadAllAssetsAtPath(Human+"walk.FBX").OfType<AnimationClip>().FirstOrDefault(c=>c.humanMotion&&!c.name.StartsWith("__"));if(clip==null)throw new InvalidOperationException("Actual owned humanoid clip required: "+pair.Item2);var state=machine.states.FirstOrDefault(s=>s.state.name==pair.Item1).state;if(state==null)state=machine.AddState(pair.Item1);if(state.motion==null)state.motion=clip;if(pair.Item1=="Idle")machine.defaultState=state;EditorUtility.SetDirty(state);}EditorUtility.SetDirty(machine);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
            var result=new List<Animator>();for(int i=0;i<12;i++){var source=AssetDatabase.LoadAssetAtPath<GameObject>(Human+(i%2==0?"business01_m":"business01_f")+".FBX");if(source==null)throw new InvalidOperationException("Actual owned Human model required.");var person=UnityEngine.Object.Instantiate(source,root);var animator=person.GetComponent<Animator>();if(animator==null||animator.avatar==null||!animator.avatar.isHuman)throw new InvalidOperationException("Genuine Human avatar required.");animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;person.transform.position=new Vector3(-65+i*12,0,-48);person.transform.rotation=Quaternion.identity;person.transform.localScale=Vector3.one*1.8f;result.Add(animator);}
            var staff=root.gameObject.AddComponent<ArchaeologyStaff>();staff.workers=result.ToArray();staff.match=root.GetComponentInParent<ArchaeologyMatch>();staff.match.staff=staff;return result.ToArray();
        }
        static GameObject Shape(PrimitiveType type,string name,Transform root,Vector3 p,Vector3 scale,Material material,bool collider){var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;if(!collider)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
        static void Beam(Transform root,Vector3 a,Vector3 b,float r,Material material,string name){var g=Shape(PrimitiveType.Cylinder,name,root,(a+b)*.5f,new Vector3(r*2,Vector3.Distance(a,b)*.5f,r*2),material,false);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
        static GameObject RenderOnly(string path,Transform root){var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(source==null)throw new InvalidOperationException("Actual owned asset required: "+path);var g=UnityEngine.Object.Instantiate(source,root);foreach(var script in g.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(script);foreach(var b in g.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(b);foreach(var c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);return g;}
        static void Prop(Transform parent,string path,Vector3 position,Vector3 size){var holder=new GameObject("Actual owned research prop "+Path.GetFileNameWithoutExtension(path)).transform;holder.SetParent(parent,false);var model=RenderOnly(path,holder);var all=model.GetComponentsInChildren<Renderer>(true);if(all.Length==0)throw new InvalidOperationException("Actual prop renderer required.");Bounds b=all[0].bounds;foreach(var r in all)b.Encapsulate(r.bounds);model.transform.localScale=Vector3.Scale(model.transform.localScale,new Vector3(size.x/Mathf.Max(.001f,b.size.x),size.y/Mathf.Max(.001f,b.size.y),size.z/Mathf.Max(.001f,b.size.z)));b=all[0].bounds;foreach(var r in all)b.Encapsulate(r.bounds);model.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);holder.position=position;}
        static LineRenderer Line(string name,Material material,float width,Transform root){var line=new GameObject(name).AddComponent<LineRenderer>();line.transform.SetParent(root,false);line.sharedMaterial=material;line.startWidth=line.endWidth=width;line.useWorldSpace=true;return line;}
        static ParticleSystem Effect(string path,Transform root,string name){var g=RenderOnly(path,root);g.name=name;var all=g.GetComponentsInChildren<ParticleSystem>(true);if(all.Length==0)throw new InvalidOperationException("Actual dust effect required.");foreach(var p in all){var m=p.main;m.playOnAwake=false;m.loop=false;m.stopAction=ParticleSystemStopAction.None;p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}return all[0];}
        static AudioClip[] Audio(){string[] paths={"Assets/Heat - Complete Modern UI/Audio/Click.wav","Assets/TruckBilliards/Audio/Ball.wav","Assets/NWH/Vehicle Physics 2/Sound/Horn/Horn.wav","Assets/TruckBilliards/Audio/Strike.wav","Assets/TruckHockey/Audio/Faceoff.wav","Assets/TruckHockey/Audio/GoalHorn.wav","Assets/TruckHockey/Audio/FinalWhistle.wav"};return paths.Select(p=>{var c=AssetDatabase.LoadAssetAtPath<AudioClip>(p);if(c==null)throw new InvalidOperationException("Actual audio required: "+p);return c;}).ToArray();}
    }
}
