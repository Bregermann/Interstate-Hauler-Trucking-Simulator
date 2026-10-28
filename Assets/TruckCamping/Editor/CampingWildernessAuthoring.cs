using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using Pinwheel.Vista;
using Pinwheel.Vista.UnityTerrain;
using LWS.TruckSports.Editor;
using LWS.TruckMechanicalBull.Editor;
namespace LWS.TruckCamping.Editor
{
    // A single saved world definition consumed by BOTH activity scenes. Vista
    // owns terrain grid/neighbours and EasyRoads owns physical trail geometry.
    // LWS owns the deliberately composed landmarks, woodland and interactions.
    public static partial class CampingWildernessAuthoring
    {
        public const string Folder = "TruckCamping";
        public static readonly Vector2[] Stops = { new Vector2(0,0), new Vector2(105,48), new Vector2(190,135), new Vector2(-110,120), new Vector2(-180,250), new Vector2(155,265), new Vector2(210,300), new Vector2(300,160), new Vector2(275,-75), new Vector2(70,-110), new Vector2(-155,-85), new Vector2(-275,40) };
        public static readonly Vector2[] Trail = { new Vector2(0,-55), new Vector2(105,48), new Vector2(102,125), new Vector2(190,135), new Vector2(210,300), new Vector2(155,265), new Vector2(-110,120), new Vector2(-180,250), new Vector2(-275,40), new Vector2(-155,-85), new Vector2(70,-110), new Vector2(275,-75), new Vector2(300,160), new Vector2(210,300), new Vector2(190,135), new Vector2(102,125), new Vector2(105,48), new Vector2(0,-55) };
        public static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b) { var d = b-a; return Vector2.Distance(p, a + d * Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.001f,d.sqrMagnitude))); }
        public static float HeightAt(Vector2 p)
        {
            float border = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(390, 940, p.magnitude));
            float terrain = 7.4f + border * (60 + Mathf.PerlinNoise((p.x+1200)*.003f,(p.y+1100)*.003f)*150) + Mathf.PerlinNoise((p.x+1200)*.012f,(p.y+1100)*.012f)*3;
            float trailDistance = float.MaxValue; for (int i=1;i<Trail.Length;i++) trailDistance=Mathf.Min(trailDistance,SegmentDistance(p,Trail[i-1],Trail[i]));
            foreach(var stop in Stops) trailDistance=Mathf.Min(trailDistance,Mathf.Max(0,Vector2.Distance(p,stop)-25));
            terrain = Mathf.Lerp(8,terrain,Mathf.SmoothStep(0,1,Mathf.InverseLerp(12,28,trailDistance)));
            float lake = new Vector2((p.x-145)/43,(p.y-83)/35).magnitude;
            if(lake<1.25f) terrain=Mathf.Lerp(1.7f,terrain,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.78f,1.25f,lake)));
            // The narrow stream follows its own bed. Road crossings get actual
            // authored bridge decks rather than an invisible collision plane.
            float streamX = 145 + Mathf.Sin((p.y-83)*.018f)*20;
            if(p.y>110 && p.y<285) terrain=Mathf.Lerp(3,terrain,Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,7,Mathf.Abs(p.x-streamX))));
            return terrain;
        }
        public static GameObject RequireSharedPrefab()
        {
            var prior=AssetDatabase.LoadAssetAtPath<GameObject>(CampingWilderness.SharedPrefab);
            if(prior!=null)
            {
                var data=prior.GetComponent<CampingWilderness>();
                if(data==null || data.terrains.Length!=4 || data.landmarks.Length!=Stops.Length || data.resources.Length<48 || data.hammocks.Length!=4 || prior.GetComponentInChildren<NavMeshSurface>(true)==null) throw new InvalidOperationException("Existing incomplete/custom shared wilderness preserved; review it explicitly.");
                return prior;
            }
            if(EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped compiled author required.");
            foreach(string directory in new[]{"Prefabs","Models","Materials"}) Directory.CreateDirectory("Assets/"+Folder+"/"+directory); AssetDatabase.Refresh();
            if(!resumingKnownPartial && AssetDatabase.FindAssets("Camp Haul",new[]{"Assets/TruckCamping/Models"}).Length>0) throw new InvalidOperationException("Owned partial world assets already exist. Preserve them before choosing an explicit repair.");
            WildernessAssetIntake.ImportReviewedModels();
            var root=new GameObject("CAMP HAUL · one shared mountain wilderness");
            var world=root.AddComponent<CampingWilderness>();
            try
            {
                TerrainGrid(world); Landmarks(world); Trails(world); Woodland(world); CampActivities(world); Water(world); Wildlife(world);world.animals=world.GetComponentsInChildren<CampWildlife>(true); Navigation(world);
                var ambience=root.AddComponent<AudioSource>(); ambience.playOnAwake=true; ambience.loop=true; ambience.spatialBlend=0; ambience.volume=.3f;
                ambience.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/WeatherMaker/Prefab/Sound/Ambient/WeatherMakerAmbientSound_MorningBirds.ogg");
                if(ambience.clip==null) throw new InvalidOperationException("Installed woodland ambience required.");
                AssetDatabase.SaveAssets();if(resumingKnownPartial)VerifyRetainedInputs(true);var prefab=PrefabUtility.SaveAsPrefabAsset(root,CampingWilderness.SharedPrefab); AssetDatabase.SaveAssets(); return prefab;
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        static void TerrainGrid(CampingWilderness world)
        {
            var parent=new GameObject("Vista connected mountain terrain grid").transform; parent.SetParent(world.transform,false); parent.localPosition=new Vector3(-1000,0,-1000);
            var grid=new UnityTerrainSystem().CreateTerrainGrid(new TerrainGridCreationContext(new Vector2Int(2,2),new Vector3(1000,260,1000),parent));
            string basePath="Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/";
            var layers=new[]{AssetDatabase.LoadAssetAtPath<TerrainLayer>(basePath+"Grass Light/Grass_Light_VistaSample.terrainlayer"),AssetDatabase.LoadAssetAtPath<TerrainLayer>(basePath+"Grass Dark/Grass_Dark_VistaSample.terrainlayer"),AssetDatabase.LoadAssetAtPath<TerrainLayer>(basePath+"Rock Base/Rock_Base_VistaSample.terrainlayer")};
            if(layers.Any(l=>l==null)) throw new InvalidOperationException("Installed Vista grass/rock layers required.");
            var material=ImpossibleBalanceArt.Material(Folder,"Camp Haul terrain surface",Color.white); material.shader=Shader.Find("Universal Render Pipeline/Terrain/Lit");
            world.terrains=new Terrain[4];
            for(int z=0;z<2;z++) for(int x=0;x<2;x++)
            {
                var terrain=grid[x,z].GetComponent<Terrain>(); var data=terrain.terrainData; data.name="Camp Haul Vista terrain "+x+" "+z;
                string dataPath="Assets/TruckCamping/Models/"+data.name+".asset";
                if(resumingKnownPartial)
                {
                    var retained=AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);if(retained==null)throw new InvalidOperationException("Known partial terrain missing: "+dataPath);
                    if(!ResumingCompletedStage){RepairKnownTerrainDimensions(retained);PaintOwnedTerrain(retained,x,z);AttachOwnedControlMaps(retained,dataPath);}
                    terrain.terrainData=retained;terrain.GetComponent<TerrainCollider>().terrainData=retained;UnityEngine.Object.DestroyImmediate(data);
                    terrain.drawInstanced=true;terrain.heightmapPixelError=6;terrain.basemapDistance=1200;terrain.materialTemplate=material;world.terrains[z*2+x]=terrain;continue;
                }
                ConfigureNewTerrainDimensions(data);data.terrainLayers=layers;
                var heights=new float[257,257];
                for(int j=0;j<257;j++) for(int i=0;i<257;i++) heights[j,i]=HeightAt(new Vector2(-1000+x*1000+i/256f*1000,-1000+z*1000+j/256f*1000))/260;
                data.SetHeights(0,0,heights);
                PaintOwnedTerrain(data,x,z);terrain.drawInstanced=true; terrain.heightmapPixelError=6; terrain.basemapDistance=1200; terrain.materialTemplate=material;
                AssetDatabase.CreateAsset(data,dataPath);AttachOwnedControlMaps(data,dataPath);world.terrains[z*2+x]=terrain;
            }
        }
        public static void ConfigureNewTerrainDimensions(TerrainData data)
        {
            // Unity retains horizontal sample pitch when resolution changes.
            // Reapply the desired tile size AFTER changing the Vista default33.
            data.heightmapResolution=257;data.alphamapResolution=128;data.size=new Vector3(1000,260,1000);
        }
        static Vector3 Ground(Vector2 p,float above=0)=>new Vector3(p.x,HeightAt(p)+above,p.y);
        public static Transform Point(string name,Transform parent,Vector3 position)
        {var result=new GameObject(name).transform;result.SetParent(parent,false);result.position=position;return result;}
        static void Landmarks(CampingWilderness world)
        {
            string[] names={"Cedar Lantern Camp","Willow Lake Shore","Silver Cascade","Foxglove Cabin","Eagle Ridge Overlook","Fern Creek Bridge","Moonlit Moss Grotto","Wildflower Meadow","Soft Soil Play Glade","Quiet Hammock Clearing","Pinecone Camp","Old Timber Shelter"};
            var kinds=new[]{CampPlaceKind.Campsite,CampPlaceKind.Lake,CampPlaceKind.Waterfall,CampPlaceKind.Cabin,CampPlaceKind.Overlook,CampPlaceKind.Bridge,CampPlaceKind.Cave,CampPlaceKind.Meadow,CampPlaceKind.DiggingGlade,CampPlaceKind.RestClearing,CampPlaceKind.Campsite,CampPlaceKind.Cabin};
            world.landmarks=new CampLandmark[Stops.Length];world.route=new Transform[Stops.Length];
            for(int i=0;i<Stops.Length;i++)
            {
                var root=Point(names[i],world.transform,Ground(Stops[i]));var place=root.gameObject.AddComponent<CampLandmark>();place.id="camp.landmark."+i;place.label=names[i];place.index=i;place.kind=kinds[i];
                place.parking=Point("Actual truck activity parking",root,root.position+Vector3.back*7);place.activity=Point("Activity focus",root,root.position+Vector3.forward*8);place.view=Point("Scenic photo subject",root,place.activity.position+Vector3.up*3);world.landmarks[i]=place;world.route[i]=place.parking;
                Sign(root,names[i],new Vector3(-14,0,-13));
                if(kinds[i]==CampPlaceKind.Cabin) Cabin(root);
            }
            world.landmarks[1].activity.position=new Vector3(128,5.55f,75);
            // Cave is a genuine drive-in void between bounded source-rock
            // outcrops with physical walls, not a painted portal.
            var cave=world.landmarks[6].transform;
            Rock(cave,new Vector3(-12,0,8),new Vector3(9,12,19));Rock(cave,new Vector3(12,0,8),new Vector3(9,12,19));Rock(cave,new Vector3(0,12,10),new Vector3(32,8,21));
            var floor=Part(cave,"Grotto worn limestone floor",new Vector3(0,.02f,9),new Vector3(19,.08f,26),Stone);Collision(floor);
            var light=Point("Warm grotto lantern",cave,cave.position+new Vector3(-7,3,4)).gameObject.AddComponent<Light>();light.type=LightType.Point;light.range=22;light.color=new Color(1,.72f,.4f);light.intensity=2;
        }
        public static Material Wood=>ImpossibleBalanceArt.Material(Folder,"Camp Haul warm cedar",new Color(.3f,.16f,.07f),.18f);
        public static Material Stone=>ImpossibleBalanceArt.Material(Folder,"Camp Haul river stone",new Color(.38f,.4f,.35f),.16f);
        public static Material Cloth=>ImpossibleBalanceArt.Material(Folder,"Camp Haul woven teal canvas",new Color(.08f,.35f,.35f),.2f);
        public static Mesh Block=>ImpossibleBalanceArt.Mesh(Folder,"Camp Haul bound rounded timber",ImpossibleBalanceArt.RoundedBlock);
        public static Mesh Round=>ImpossibleBalanceArt.Mesh(Folder,"Camp Haul smooth oval detail",ImpossibleBalanceArt.Ovoid);
        public static GameObject Part(Transform parent,string name,Vector3 local,Vector3 size,Material material,Quaternion rotation=default)=>ImpossibleBalanceArt.Part(parent,name,Block,material,local,size,rotation);
        public static void Collision(GameObject go) { go.AddComponent<BoxCollider>(); }
        static void Sign(Transform parent,string words,Vector3 local)
        {
            Part(parent,"Cedar wayfinding post",local+Vector3.up*1.5f,new Vector3(.2f,3,.2f),Wood);
            Part(parent,"Carved woodland trail sign",local+new Vector3(0,2.7f,0),new Vector3(5.5f,.9f,.18f),Wood);
            var label=SportsEventAuthoring.Label("Carved trail destination",words,parent.position+local+new Vector3(0,2.7f,-.12f),.11f,Color.white);label.transform.SetParent(parent,true);label.anchor=TextAnchor.MiddleCenter;
        }
        static void Cabin(Transform parent)
        {
            var root=Point("Cedar log cabin with open truck shelter",parent,parent.position+Vector3.forward*14);
            var floor=Part(root,"Raised plank cabin floor",new Vector3(0,.3f,0),new Vector3(18,.6f,16),Wood);Collision(floor);
            for(int row=0;row<12;row++)
            {
                for(int side=-1;side<=1;side+=2){var wall=Part(root,"Stacked cedar wall log",new Vector3(side*8.7f,.75f+row*.48f,0),new Vector3(.5f,.5f,16),Wood);Collision(wall);}
                var rear=Part(root,"Rear notched cedar log",new Vector3(0,.75f+row*.48f,7.5f),new Vector3(18,.5f,.6f),Wood);Collision(rear);
            }
            for(int side=-1;side<=1;side+=2){var roof=Part(root,"Bound cedar shingle roof",new Vector3(side*4.8f,8,0),new Vector3(11,.22f,18),Wood,Quaternion.Euler(0,0,side*-27));Collision(roof);}
            var ramp=Part(root,"Cabin accessible plank ramp",new Vector3(0,.1f,-10),new Vector3(12,.2f,5),Wood,Quaternion.Euler(-5,0,0));Collision(ramp);
            for(int side=-1;side<=1;side+=2)Part(root,"Warm cabin doorway trim",new Vector3(side*7.5f,3,-7.5f),new Vector3(.35f,6,.35f),Cloth);
        }
        static void Trails(CampingWilderness world)
        {
            var material=ImpossibleBalanceArt.Material(Folder,"Camp Haul packed earth trail",new Color(.27f,.21f,.12f),.1f);
            var markers=Trail.Select(p=>Ground(p,.09f)).ToArray();var road=ResumingCompletedStage?ReuseExactCompletedRoad(material):resumingKnownPartial?RegenerateExactReviewedRoad(material):SportsEasyRoadsAuthoring.Bake(Folder,"Camp Haul scenic woodland loop",markers,14,material,null);road.SetParent(world.transform,true);
            foreach(float z in new[]{135f,265f})
            {
                float x=145+Mathf.Sin((z-83)*.018f)*20;var bridge=Point("Actual timber bridge over flowing creek",world.transform,new Vector3(x,8,z));
                for(int row=0;row<26;row++){var plank=Part(bridge,"Bridge bound individual deck plank",new Vector3((row-12.5f)*.7f,.04f,0),new Vector3(.65f,.3f,17),Wood);Collision(plank);}
                for(int side=-1;side<=1;side+=2)for(int p=0;p<5;p++)Part(bridge,"Bridge cedar rail post",new Vector3((p-2)*4,1.2f,side*8),new Vector3(.3f,2.5f,.3f),Wood);
            }
        }
        static void Woodland(CampingWilderness world)
        {
            const string pinePath="Assets/Toby Fredson/Rocky Hills Environment - Whitebark Pine/RHEWP_Demo/Prefabs/Prefabs_Vegetation/PV_Trees/WhitebarkPineRHEP_B.prefab";
            var pine=AssetDatabase.LoadAssetAtPath<GameObject>(pinePath);if(pine==null)throw new InvalidOperationException("Installed Whitebark forest tree required.");
            foreach(var terrain in world.terrains)
            {
                terrain.treeDistance=1100;terrain.treeBillboardDistance=180;terrain.treeCrossFadeLength=30;terrain.treeMaximumFullLODCount=60;
                if(ResumingCompletedStage)continue; // Persisted exact trees/controlmaps reused; never clear/reseed them.
                if(!resumingKnownPartial || terrain.terrainData.treePrototypes.Length==0)terrain.terrainData.treePrototypes=new[]{new TreePrototype{prefab=pine,bendFactor=.2f}};
                var trees=new List<TreeInstance>();
                for(int i=0;i<950;i++)
                {
                    float x=Mathf.Repeat(i*.6180339887f,.999f),z=Mathf.Repeat(i*.41421356f+.173f,.999f);var p=new Vector2(terrain.transform.position.x+x*1000,terrain.transform.position.z+z*1000);
                    if(Stops.Any(s=>Vector2.Distance(p,s)<39)||Trail.Zip(Trail.Skip(1),(a,b)=>SegmentDistance(p,a,b)).Min()<23||HeightAt(p)<6||p.magnitude>900)continue;
                    float size=.65f+Mathf.Repeat(i*.37f,.7f);trees.Add(new TreeInstance{position=new Vector3(x,HeightAt(p)/260,z),prototypeIndex=0,heightScale=size,widthScale=size,color=Color.white,lightmapColor=Color.white,rotation=Mathf.Repeat(i*2.39996f,Mathf.PI*2)});
                }
                terrain.terrainData.SetTreeInstances(trees.ToArray(),false);EditorUtility.SetDirty(terrain.terrainData);
            }
            // Close trail edges use actual installed foliage with preserved
            // material/LOD/wind settings, bounded in footprint and quantity.
            var fern=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NatureManufacture Assets/Advanced Foliage Pack/Unity grass/Unity Grass Models/Prefabs/prefab_unity_grass_fern_01_0.prefab");
            if(fern==null)throw new InvalidOperationException("Installed animated foliage foundation required.");
            var undergrowth=Point("Trail-side fern and wildflower beds",world.transform,Vector3.zero);
            for(int i=0;i<180;i++)
            {
                var a=Trail[i%(Trail.Length-1)];var b=Trail[i%(Trail.Length-1)+1];var direction=(b-a).normalized;var p=Vector2.Lerp(a,b,Mathf.Repeat(i*.618f,1))+new Vector2(-direction.y,direction.x)*(i%2==0?20:-20);
                if(HeightAt(p)<6)continue;var instance=(GameObject)PrefabUtility.InstantiatePrefab(fern,undergrowth);instance.transform.position=Ground(p);Fit(instance,1.4f,true);instance.transform.rotation=Quaternion.Euler(0,i*137.5f,0);
            }
            for(int i=0;i<24;i++){var p=new Vector2(Mathf.Cos(i*2.4f)*(330+i%4*80),Mathf.Sin(i*2.4f)*(330+i%5*70));var rock=Rock(world.transform,Ground(p),new Vector3(6+i%3*2,4+i%2*2,7+i%4));rock.name="Bounded installed woodland outcrop "+i;}
            for(int i=0;i<9;i++)
            {
                var p=new Vector2(-70+i*34,335+i%2*25);var fallen=(GameObject)PrefabUtility.InstantiatePrefab(pine,world.transform);fallen.name="Actual fallen Whitebark pine "+i;fallen.transform.position=Ground(p);fallen.transform.rotation=Quaternion.Euler(86,i*53,0);Fit(fallen,15,true);
            }
            Wildflowers(world);
        }
        static void Wildflowers(CampingWilderness world)
        {
            var parent=Point("Meadow's original clustered wildflowers",world.transform,Vector3.zero);
            var materials=new[]{ImpossibleBalanceArt.Material(Folder,"Camp Haul wildflower cream petals",new Color(.94f,.87f,.65f)),ImpossibleBalanceArt.Material(Folder,"Camp Haul wildflower violet petals",new Color(.52f,.34f,.71f)),ImpossibleBalanceArt.Material(Folder,"Camp Haul wildflower green stems",new Color(.2f,.38f,.15f))};
            for(int cluster=0;cluster<8;cluster++)
            {
                var center=Stops[7]+new Vector2((cluster%4-1.5f)*10,15+cluster/4*10);var combinations=new List<CombineInstance>[3];for(int n=0;n<3;n++)combinations[n]=new List<CombineInstance>();
                for(int i=0;i<28;i++)
                {
                    float angle=i*2.39996f,radius=Mathf.Sqrt(i/28f)*5;var p=Ground(center+new Vector2(Mathf.Sin(angle),Mathf.Cos(angle))*radius);float height=.35f+i%4*.1f;
                    combinations[2].Add(new CombineInstance{mesh=Round,transform=Matrix4x4.TRS(p+Vector3.up*height*.5f,Quaternion.identity,new Vector3(.035f,height,.035f))});
                    for(int petal=0;petal<5;petal++){float a=petal*Mathf.PI*2/5;combinations[cluster%2].Add(new CombineInstance{mesh=Round,transform=Matrix4x4.TRS(p+new Vector3(Mathf.Sin(a)*.09f,height,Mathf.Cos(a)*.09f),Quaternion.Euler(0,a*Mathf.Rad2Deg,0),new Vector3(.12f,.04f,.2f))});}
                }
                for(int n=0;n<3;n++)if(combinations[n].Count>0){string name="Camp Haul wildflowers "+cluster+" palette "+n;string path="Assets/TruckCamping/Models/"+name+".asset";var mesh=resumingKnownPartial?AssetDatabase.LoadAssetAtPath<Mesh>(path):null;if(mesh==null){if(resumingKnownPartial)throw new InvalidOperationException("Known partial wildflower mesh missing: "+path);mesh=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(combinations[n].ToArray(),true,true);AssetDatabase.CreateAsset(mesh,path);}var go=new GameObject(mesh.name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=materials[n];}
            }
        }
        public static GameObject Rock(Transform parent,Vector3 local,Vector3 desired)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Toon Gas Station/Prefabs/Environment/Rock_Cluster_1A.prefab");
            if(source==null){string guid=AssetDatabase.FindAssets("Rock_Cluster_1A t:Prefab").FirstOrDefault();if(guid==null)throw new InvalidOperationException("Actual installed rock prefab required.");source=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));}
            var go=(GameObject)PrefabUtility.InstantiatePrefab(source,parent);go.transform.localPosition=local;Fit(go,Mathf.Max(desired.x,desired.z),true);
            var bounds=Bounds(go);float rise=desired.y/Mathf.Max(.01f,bounds.size.y);go.transform.localScale=new Vector3(go.transform.localScale.x,go.transform.localScale.y*rise,go.transform.localScale.z);bounds=Bounds(go);go.transform.position+=Vector3.up*(parent.TransformPoint(local).y-bounds.min.y);
            var coll=go.AddComponent<BoxCollider>();var inv=go.transform.worldToLocalMatrix;coll.center=inv.MultiplyPoint3x4(bounds.center);coll.size=Vector3.Scale(bounds.size,new Vector3(1/go.transform.lossyScale.x,1/go.transform.lossyScale.y,1/go.transform.lossyScale.z));return go;
        }
        public static Bounds Bounds(GameObject go){var all=go.GetComponentsInChildren<Renderer>(true);if(all.Length==0)throw new InvalidOperationException("Actual rendered asset required: "+go.name);var b=all[0].bounds;foreach(var r in all.Skip(1))b.Encapsulate(r.bounds);return b;}
        public static void Fit(GameObject go,float dimension,bool footprint)
        {var b=Bounds(go);float scale=dimension/(footprint?Mathf.Max(b.size.x,b.size.z):Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z)));var origin=go.transform.position;go.transform.localScale*=scale;b=Bounds(go);go.transform.position+=new Vector3(origin.x-b.center.x,origin.y-b.min.y,origin.z-b.center.z);}
        static void CampActivities(CampingWilderness world)
        {
            var sites=new List<CampSite>();foreach(int index in new[]{0,3,10,11})
            {
                var landmark=world.landmarks[index];var root=Point("Real cooking fire and shared camp furniture",landmark.transform,landmark.activity.position);var site=root.gameObject.AddComponent<CampSite>();site.id="camp.fire."+index;site.landmark=landmark;site.state=new CampfireState{id=site.id};var logs=new List<GameObject>();
                for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Part(root,"Ring of smooth river stones",new Vector3(Mathf.Cos(a)*1.6f,.18f,Mathf.Sin(a)*1.6f),new Vector3(.8f,.4f,.65f),Stone);}
                for(int i=0;i<5;i++)logs.Add(Part(root,"Arranged dry firewood",new Vector3(0,.22f+i*.08f,0),new Vector3(.28f,.28f,2.4f),Wood,Quaternion.Euler(0,i*47,0)));site.arrangedLogs=logs.ToArray();
                site.flame=Fire(root);site.smoke=Particles(root,"Gentle campfire smoke",new Color(.45f,.43f,.4f,.25f),.6f,4,1.5f);site.smoke.transform.localPosition=Vector3.up*1.2f;
                site.fireLight=Point("Actual campfire local light",root,root.position+Vector3.up*1.8f).gameObject.AddComponent<Light>();site.fireLight.type=LightType.Point;site.fireLight.color=new Color(1,.42f,.12f);site.fireLight.range=18;site.fireLight.intensity=0;
                site.pot=ImpossibleBalanceArt.Part(root,"Hanging cast iron cooking pot",ImpossibleBalanceArt.Mesh(Folder,"Camp Haul rolled cooking pot",()=>ImpossibleBalanceArt.Lathe(new[]{new Vector2(0,0),new Vector2(.65f,.1f),new Vector2(.8f,.85f),new Vector2(.82f,.95f),new Vector2(.72f,.98f),new Vector2(.65f,.2f)})),ImpossibleBalanceArt.Material(Folder,"Camp Haul dark iron",new Color(.07f,.075f,.07f),.32f,.6f),new Vector3(0,1.35f,0),Vector3.one).transform;
                for(int side=-1;side<=1;side+=2)Part(root,"Cooking tripod pole",new Vector3(side*1.1f,1.9f,0),new Vector3(.08f,4,.08f),Stone,Quaternion.Euler(0,0,side*25));
                Part(root,"Cooking crossbar",new Vector3(0,3.6f,0),new Vector3(3,.08f,.08f),Stone);
                for(int i=0;i<4;i++){float angle=i*Mathf.PI*.5f;var bench=Part(root,"Bound cedar camp bench",new Vector3(Mathf.Sin(angle)*7,.7f,Mathf.Cos(angle)*7),new Vector3(5,1.2f,1.1f),Wood,Quaternion.Euler(0,i*90,0));Collision(bench);}
                Tent(root,new Vector3(-14,0,8));sites.Add(site);
            }
            world.campsites=sites.ToArray();world.hammocks=new CampHammock[4];
            for(int i=0;i<4;i++)world.hammocks[i]=Hammock(world,new Vector2(45+i*17,-65));
            var resources=new List<CampResource>();
            for(int i=0;i<64;i++)
            {
                var stop=Stops[i%Stops.Length];float a=i*2.39996f;var p=stop+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(18+i%4*3);if(HeightAt(p)<6)continue;
                var root=Point("Gatherable woodland material "+i,world.transform,Ground(p,.35f));var item=root.gameObject.AddComponent<CampResource>();item.id="camp.resource."+i;item.kind=(CampSupply)(i%3);
                var visual=Part(root,i%3==2?"Cluster of safe cartoon woodland mushrooms":"Bound fallen woodland timber",Vector3.zero,i%3==2?new Vector3(.8f,.45f,.8f):new Vector3(.3f,.3f,i%3==0?1.6f:2.2f),i%3==2?Cloth:Wood,Quaternion.Euler(0,i*73,0));
                if(i%3==2){var cap=ImpossibleBalanceArt.Part(root,"Mushroom rounded cap",Round,ImpossibleBalanceArt.Material(Folder,"Camp Haul spotted mushroom cap",new Color(.62f,.23f,.12f)),Vector3.up*.3f,new Vector3(.8f,.25f,.8f));for(int dot=0;dot<4;dot++)ImpossibleBalanceArt.Part(root,"Mushroom cream cap spot",Round,Stone,new Vector3(Mathf.Sin(dot*1.8f)*.2f,.42f,Mathf.Cos(dot*1.8f)*.2f),Vector3.one*.1f);}
                item.body=root.gameObject.AddComponent<Rigidbody>();item.body.mass=i%3==1?12:3;var coll=root.gameObject.AddComponent<BoxCollider>();coll.size=i%3==2?new Vector3(.9f,.8f,.9f):new Vector3(.4f,.4f,2.4f);item.physicalPickup=coll;item.art=root.GetComponentsInChildren<Renderer>();item.home=root.position;item.homeRotation=root.rotation;resources.Add(item);
            }
            world.resources=resources.ToArray();
        }
        static void Tent(Transform parent,Vector3 local)
        {
            var root=Point("Finished teal canvas A-frame tent",parent,parent.TransformPoint(local));
            foreach(int side in new[]{-1,1}){var sheet=Part(root,"Stitched pitched canvas panel",new Vector3(side*1.5f,1.6f,0),new Vector3(4.4f,.06f,5.5f),Cloth,Quaternion.Euler(0,0,side*-46));for(int seam=0;seam<4;seam++)Part(root,"Canvas bound visible seam",new Vector3(side*1.5f,1.65f,-2.4f+seam*1.6f),new Vector3(4.4f,.035f,.03f),Stone,Quaternion.Euler(0,0,side*-46));}
            for(int side=-1;side<=1;side+=2)Part(root,"Tent ridge support",new Vector3(0,1.65f,side*2.5f),new Vector3(.08f,3.3f,.08f),Wood);
        }
        static CampHammock Hammock(CampingWilderness world,Vector2 point)
        {
            var root=Point("Truck-sized gently spring-supported hammock",world.transform,Ground(point));var h=root.gameObject.AddComponent<CampHammock>();h.wilderness=world;
            var platform=Part(root,"Woven suspended truck hammock",new Vector3(0,.9f,0),new Vector3(10,.35f,16),Cloth);h.platform=platform.AddComponent<Rigidbody>();h.platform.mass=35000;h.platform.linearDamping=1;h.platform.angularDamping=3;h.deck=platform.AddComponent<BoxCollider>();h.anchors=new Transform[4];h.ropes=new LineRenderer[4];
            for(int i=0;i<4;i++)
            {
                var local=new Vector3(i%2==0?-6:6,5,i<2?-8:8);Part(root,"Hammock carved timber mast",new Vector3(local.x,2.5f,local.z),new Vector3(.7f,5,.7f),Wood);h.anchors[i]=Point("Fixed rope anchor "+i,root,root.TransformPoint(local));
                var joint=platform.AddComponent<SpringJoint>();joint.autoConfigureConnectedAnchor=false;joint.anchor=new Vector3(i%2==0?-.45f:.45f,.2f,i<2?-.45f:.45f);joint.connectedAnchor=h.anchors[i].position;joint.minDistance=0;joint.maxDistance=4.5f;joint.spring=220000;joint.damper=45000;joint.tolerance=.02f;
                var rope=Point("Visible tensioned hammock rope",root,root.position).gameObject.AddComponent<LineRenderer>();rope.sharedMaterial=Wood;rope.positionCount=3;rope.widthMultiplier=.12f;h.ropes[i]=rope;
            }
            for(int side=-1;side<=1;side+=2){var ramp=Part(root,"Actual gently sloped hammock approach",new Vector3(0,.4f,side*11),new Vector3(10,.25f,7),Wood,Quaternion.Euler(side*7,0,0));Collision(ramp);}
            h.approach=Point("Actual hammock deck navigation target",root,root.TransformPoint(new Vector3(0,.9f,0)));return h;
        }
        public static ParticleSystem Particles(Transform parent,string name,Color color,float size,float rate,float life)
        {
            var fx=Point(name,parent,parent.position).gameObject.AddComponent<ParticleSystem>();fx.transform.rotation=Quaternion.Euler(-90,0,0);var main=fx.main;main.playOnAwake=false;main.startColor=color;main.startSize=size;main.startLifetime=life;main.startSpeed=.5f;main.maxParticles=80;main.simulationSpace=ParticleSystemSimulationSpace.World;var emission=fx.emission;emission.rateOverTime=rate;var shape=fx.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=12;shape.radius=.25f;
            var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckSports/Rendering/River splash alpha.mat");if(source==null)throw new InvalidOperationException("Existing River Modeler alpha particle foundation required.");string path="Assets/TruckCamping/Materials/Camp Haul soft "+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(source){name="Camp Haul soft "+name};material.SetColor("_BaseColor",Color.white);material.SetColor("_Color",Color.white);AssetDatabase.CreateAsset(material,path);}fx.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;var fade=fx.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.12f),new GradientAlphaKey(0,1)});fade.color=gradient;fx.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);return fx;
        }
        static ParticleSystem Fire(Transform parent)
        {
            // Same audited CoreCollapse masked fire rendering foundation used
            // by Ninja's genuine entrance flames. No unrelated actors copied.
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckPedestrianPlowing/Materials/Parade fire.mat");if(material==null||material.GetTexture("_BaseMap")==null)throw new InvalidOperationException("Existing CoreCollapse masked flame material required.");
            var fx=Point("Actual CoreCollapse masked campfire emission",parent,parent.position+Vector3.up*.35f).gameObject.AddComponent<ParticleSystem>();fx.transform.rotation=Quaternion.Euler(-90,0,0);fx.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=fx.main;main.loop=true;main.playOnAwake=false;main.startLifetime=new ParticleSystem.MinMaxCurve(.45f,1.1f);main.startSpeed=new ParticleSystem.MinMaxCurve(1,2.3f);main.startSize=new ParticleSystem.MinMaxCurve(.7f,1.25f);main.maxParticles=64;main.startColor=new ParticleSystem.MinMaxGradient(new Color(2,.3f,.03f,.65f),new Color(3,1.1f,.08f,.75f));var emission=fx.emission;emission.rateOverTime=25;var shape=fx.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.radius=.6f;shape.angle=10;var noise=fx.noise;noise.enabled=true;noise.strength=.18f;noise.frequency=.8f;fx.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;return fx;
        }
        static void Water(CampingWilderness world)
        {
            var material=ImpossibleBalanceArt.Material(Folder,"Camp Haul River Modeler flowing water",new Color(.15f,.46f,.43f,.88f),.92f);
            if(!ResumingCompletedStage)
            {
            var normal=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RiverModeler/Textures/Splash_Normals.png");var foam=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RiverModeler/Textures/WaterFoam.png");if(normal==null||foam==null)throw new InvalidOperationException("Installed River Modeler textures required.");material.SetTexture("_BumpMap",normal);material.SetTexture("_BaseMap",foam);material.EnableKeyword("_NORMALMAP");material.SetFloat("_BumpScale",.35f);material.SetTextureScale("_BaseMap",new Vector2(9,9));
            material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
                // Use the installed URP authority once after explicit new-water configuration.
                // Existing reviewed completed materials are reused without rewriting any field.
                BaseShaderGUI.SetMaterialKeywords(material,UnityEditor.Rendering.Universal.ShaderGUI.LitGUI.SetMaterialKeywords);
            }
            var water=Part(world.transform,"Flowing reflective Willow Lake",new Vector3(145,5.5f,83),new Vector3(79,.08f,63),material);water.AddComponent<LWS.TruckFishing.FishingWaterFlow>();
            var streamSound=water.AddComponent<AudioSource>();streamSound.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/RiverModeler/SFX/Shallow Creek Fast.wav");streamSound.loop=true;streamSound.spatialBlend=1;streamSound.maxDistance=100;streamSound.volume=.3f;
            for(int i=0;i<17;i++){float z=115+i*10,x=145+Mathf.Sin((z-83)*.018f)*20;var segment=Part(world.transform,"Flowing connected mountain creek "+i,new Vector3(x,5.45f,z),new Vector3(7,.06f,11),material,Quaternion.Euler(0,Mathf.Cos((z-83)*.018f)*15,0));segment.AddComponent<LWS.TruckFishing.FishingWaterFlow>();}
            var cascade=world.landmarks[2].transform;Rock(cascade,new Vector3(-5,0,12),new Vector3(10,13,12));Rock(cascade,new Vector3(6,0,12),new Vector3(9,15,11));
            var sheet=Part(cascade,"River Modeler flowing silver cascade",new Vector3(0,6.5f,8),new Vector3(5,13,.12f),material,Quaternion.Euler(-8,0,0));sheet.AddComponent<LWS.TruckFishing.FishingWaterFlow>();
            var sound=sheet.AddComponent<AudioSource>();sound.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/RiverModeler/SFX/River Cascade large.wav");sound.loop=true;sound.spatialBlend=1;sound.maxDistance=120;sound.volume=.35f;
            var spray=Particles(cascade,"Cascade mist",new Color(.7f,.88f,.9f,.3f),1.8f,12,2);spray.transform.localPosition=new Vector3(0,.5f,5);var main=spray.main;main.playOnAwake=true;spray.Play();
        }
        static void Wildlife(CampingWilderness world)
        {
            var types=new[]{CampOriginalWildlifeArt.Species.Bear,CampOriginalWildlifeArt.Species.Deer,CampOriginalWildlifeArt.Species.Rabbit};
            for(int i=0;i<9;i++)
            {
                var prefab=CampOriginalWildlifeArt.Prefab(types[i%3]);var root=Point("Animated original habitat "+i,world.transform,Ground(new Vector2(-60+i*21,190+i%3*15)));
                var art=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root);art.transform.localPosition=Vector3.zero;var wildlife=root.gameObject.AddComponent<CampWildlife>();wildlife.world=world;wildlife.living=art.GetComponent<CampLivingArt>();wildlife.pace=i%3==2?1.8f:1.2f;wildlife.agent=root.gameObject.AddComponent<NavMeshAgent>();wildlife.agent.radius=i%3==0?.7f:.4f;wildlife.agent.height=i%3==2?.6f:2;wildlife.route=HabitatRoute(root,world,i);
            }
            var wolf=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckFishing/CarnivalPresentation/Genuine animated ambient wolf.prefab");if(wolf==null)throw new InvalidOperationException("Existing genuine WolfLite animation foundation required.");
            for(int i=0;i<2;i++){var root=Point("Genuine animated WolfLite forest trail "+i,world.transform,Ground(new Vector2(-220+i*35,160)));var art=(GameObject)PrefabUtility.InstantiatePrefab(wolf,root);var life=root.gameObject.AddComponent<CampWildlife>();life.world=world;life.animator=art.GetComponentInChildren<Animator>(true);life.agent=root.gameObject.AddComponent<NavMeshAgent>();life.agent.radius=.6f;life.route=HabitatRoute(root,world,12+i);}
            var fish=AssetDatabase.LoadAssetAtPath<GameObject>(WildernessAssetIntake.Folder+"Fish.fbx");var controller=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(WildernessAssetIntake.Folder+"Fish locomotion.controller");
            var foxSource=AssetDatabase.LoadAssetAtPath<GameObject>(WildernessAssetIntake.Folder+"Red Fox.fbx");var foxController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(WildernessAssetIntake.Folder+"Red Fox locomotion.controller");
            for(int i=0;i<2;i++){var root=Point("Genuine animated fox glade "+i,world.transform,Ground(new Vector2(40+i*23,190)));var art=(GameObject)PrefabUtility.InstantiatePrefab(foxSource,root);BindAnimalPalette(art,"Fox");Fit(art,1.5f,true);var life=root.gameObject.AddComponent<CampWildlife>();life.world=world;life.animator=art.GetComponentInChildren<Animator>(true);if(life.animator==null||foxController==null)throw new InvalidOperationException("Actual fox rig and compatible clip required.");life.animator.runtimeAnimatorController=foxController;life.agent=root.gameObject.AddComponent<NavMeshAgent>();life.agent.radius=.45f;life.route=HabitatRoute(root,world,20+i);}
            for(int i=0;i<6;i++){var root=Point("Genuine animated lake fish "+i,world.transform,new Vector3(145,4,83));var art=(GameObject)PrefabUtility.InstantiatePrefab(fish,root);BindAnimalPalette(art,"Fish");Fit(art,.65f,false);var animator=art.GetComponent<Animator>();if(animator==null)animator=art.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;var life=root.gameObject.AddComponent<CampWaterLife>();life.world=world;life.animator=animator;life.center=new Vector3(145,5.5f,83);life.radii=new Vector2(7+i*2,7+i);life.phase=i;}
            // Existing CC0 birds keep their real compatible wing animation.
            const string birdFolder="Assets/TruckBaseJumping/ThirdParty/QuaterniusBirds/";
            for(int i=0;i<6;i++){bool eagle=i%2==0;var bird=AssetDatabase.LoadAssetAtPath<GameObject>(birdFolder+(eagle?"Eagle.fbx":"bird.fbx"));var clip=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(birdFolder+(eagle?"Eagle flight.controller":"Small bird flight.controller"));if(bird==null||clip==null)throw new InvalidOperationException("Existing genuine CC0 bird rig and compatible flight clips required.");var root=Point("Animated forest canopy bird "+i,world.transform,new Vector3(0,35+i*2,160));var visual=(GameObject)PrefabUtility.InstantiatePrefab(bird,root);Fit(visual,1.2f,false);var fly=root.gameObject.AddComponent<CampWaterLife>();fly.world=world;fly.animator=visual.GetComponentInChildren<Animator>(true);fly.center=root.position;fly.depth=0;fly.radii=new Vector2(35+i*8,28+i*5);fly.pace=.12f;fly.phase=i;if(fly.animator==null)throw new InvalidOperationException("Canopy birds need actual changing vendor bone poses.");fly.animator.runtimeAnimatorController=clip;fly.animator.applyRootMotion=false;}
        }
        static Transform[] HabitatRoute(Transform actor,CampingWilderness world,int seed)
        {var points=new Transform[4];for(int i=0;i<4;i++){float angle=i*Mathf.PI*.5f;var p=new Vector2(actor.position.x+Mathf.Cos(angle)*14,actor.position.z+Mathf.Sin(angle)*14);points[i]=Point("Habitat route "+seed+"/"+i,world.transform,Ground(p,.05f));}return points;}
        static void BindAnimalPalette(GameObject instance,string label)=>WildernessAssetIntake.BindOwnedURPPalette(instance,label);
        [Serializable]sealed class NavigationPointProof{public string landmark;public Vector3 desired,hit;public bool found;public float distance,terrainHeight;}
        [Serializable]sealed class NavigationProof{public string status;public int sources,vertices;public NavigationPointProof[] points;}
        static void ValidateNavigation(CampingWilderness world,NavMeshData data,int sourceCount)
        {
            var instance=NavMesh.AddNavMeshData(data);
            try
            {
                var points=world.route.Select(point=>{bool found=NavMesh.SamplePosition(point.position,out var hit,8,NavMesh.AllAreas);return new NavigationPointProof{landmark=point.parent.name,desired=point.position,found=found,hit=found?hit.position:Vector3.zero,distance=found?Vector3.Distance(point.position,hit.position):-1,terrainHeight=world.Height(point.position)};}).ToArray();
                string directory="Logs/TruckSports/Impossible07/CampingNavigationDiagnosis";Directory.CreateDirectory(directory);
                var report=new NavigationProof{status=points.All(p=>p.found)?"ALL_ORIGINAL_LANDMARK_SAMPLES_FOUND":"FAILED_ORIGINAL_LANDMARK_SAMPLES",sources=sourceCount,vertices=NavMesh.CalculateTriangulation().vertices.Length,points=points};
                File.WriteAllText(directory+"/complete-world-navigation.json",JsonUtility.ToJson(report,true));
                if(points.Any(p=>!p.found))throw new InvalidOperationException("A shared landmark is not on the real navigation surface (unchanged 8m bound): "+string.Join("; ",points.Where(p=>!p.found).Select(p=>p.landmark+" desired="+p.desired+" actualTerrainY="+p.terrainHeight)));
            }
            finally{instance.Remove();}
        }
        static void Navigation(CampingWilderness world)
        {
            Physics.SyncTransforms();var surface=new GameObject("Shared authored woodland navigation").AddComponent<NavMeshSurface>();surface.transform.SetParent(world.transform,false);surface.collectObjects=CollectObjects.Children;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.layerMask=1;surface.agentTypeID=0;
            var bounds=new Bounds(new Vector3(0,125,0),new Vector3(2000,300,2000));var sources=new List<NavMeshBuildSource>();NavMeshBuilder.CollectSources(bounds,1,NavMeshCollectGeometry.PhysicsColliders,0,new List<NavMeshBuildMarkup>(),sources);
            sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.ModifierBox,transform=Matrix4x4.TRS(new Vector3(145,3,83),Quaternion.identity,Vector3.one),size=new Vector3(79,5.5f,63),area=1});
            for(int i=0;i<17;i++){float z=115+i*10,x=145+Mathf.Sin((z-83)*.018f)*20;sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.ModifierBox,transform=Matrix4x4.TRS(new Vector3(x,3,z),Quaternion.identity,Vector3.one),size=new Vector3(7,5.5f,11),area=1});}
            var settings=NavMesh.GetSettingsByID(0);settings.agentRadius=1;settings.agentHeight=2.2f;settings.agentClimb=.45f;settings.agentSlope=35;settings.overrideVoxelSize=true;settings.voxelSize=.65f;
            if(ResumingCompletedStage)
            {
                var retained=AssetDatabase.LoadAssetAtPath<NavMeshData>(NavigationAsset);if(retained==null)throw new InvalidOperationException("Exact completed native navigation required.");
                ValidateNavigation(world,retained,sources.Count);surface.navMeshData=retained;surface.AddData();return;
            }
            var data=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);if(data==null)throw new InvalidOperationException("Actual shared wilderness navigation bake failed.");data.name="Camp Haul connected woodland navigation";
            try{ValidateNavigation(world,data,sources.Count);}catch{UnityEngine.Object.DestroyImmediate(data);throw;}
            if(resumingKnownPartial)
            {
                var prior=AssetDatabase.LoadAssetAtPath<NavMeshData>(NavigationAsset);if(prior==null){UnityEngine.Object.DestroyImmediate(data);throw new InvalidOperationException("Known persisted navigation GUID missing.");}
                UnityEngine.Object.DestroyImmediate(data);if(!NavMeshBuilder.UpdateNavMeshData(prior,settings,sources,bounds))throw new InvalidOperationException("Native navigation update refused; original backup retained.");data=prior;EditorUtility.SetDirty(data);
                ValidateNavigation(world,data,sources.Count);
            }
            else AssetDatabase.CreateAsset(data,NavigationAsset);
            surface.navMeshData=data;surface.AddData();
        }
    }
}
