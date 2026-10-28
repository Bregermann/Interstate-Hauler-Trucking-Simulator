using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using LWS.TruckSports;
using LWS.TruckSports.Editor;

namespace LWS.TruckMoonMadness.Editor
{
    public static class MoonAuthoring
    {
        public const string Folder = "TruckMoonMadness", Scene = "Assets/TruckMoonMadness/Scenes/TruckMoonMadness.unity";
        public const string Rock = "Assets/PinwheelStudio/Vista/Personal/Samples/Prefabs/VistaSample_DesertRock.prefab";
        public const string Dust = "Assets/CoreCollapse/Demo/Prefabs/Particles/Dust.prefab", Explosion = "Assets/CoreCollapse/Demo/Prefabs/Particles/Explosion.prefab";
        static readonly Vector2[] Craters = { new Vector2(-45, 18), new Vector2(45, -18), new Vector2(-18, -45), new Vector2(18, 45) };
        [MenuItem("Truck Sports/Alpha 0.7/Create NEW Moon Madness")]
        public static void CreateNewScene()
        {
            if (File.Exists(Scene)) throw new InvalidOperationException("New-scene author refuses overwrite: " + Scene);
            var match = SportsEventAuthoring.Begin<MoonMatch>(Folder); match.setup.count = 8; match.option=0; match.NormalizeSetup();
            match.feedback.clips = Audio(); Install(match); SportsEventAuthoring.UI(match);
            match.drivingViews = SportsDrivingViewsAuthoring.Install(match, match.trucks, match.broadcast, Folder); match.drivingViews.independentViewModes = true;
            SportsEventAuthoring.Save(Folder);
            Debug.Log("MOON_NEW_SCENE authored; eight fixed owners/full original NWH. Lead installs common appearance/broadcast/weather bindings on this new scene only. Preserve lunar space sky and physical terrain.");
        }
        public static void Install(MoonMatch match)
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling || match == null ||
                (!string.IsNullOrEmpty(match.gameObject.scene.path) && match.gameObject.scene.path != Scene) || match.transform.Find("Lunar research reserve") != null)
                throw new InvalidOperationException("Install requires an unauthored new Moon scene.");
            var root = new GameObject("Lunar research reserve").transform; root.SetParent(match.transform, false);
            var groundRoot = new GameObject("Lunar navigable physical terrain").transform; groundRoot.SetParent(root, false);
            var regolith = SportsEventAuthoring.Mat(Folder, "Weathered lunar basalt", new Color(.43f, .44f, .47f), .05f);
            var rockTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers/Rock Base/rock_base_diff.jpg");
            if (rockTexture == null) throw new InvalidOperationException("Actual owned Vista rock albedo required; the sample rock material itself has no texture.");
            regolith.mainTexture = rockTexture; regolith.mainTextureScale = new Vector2(30, 30);
            var contact = SportsEventAuthoring.Contact(Folder, "Lunar rock tyre contact", .8f, .02f);
            Terrain(groundRoot, regolith, contact);
            var hull = SportsEventAuthoring.Mat(Folder, "Ivory habitat ceramic", new Color(.81f, .83f, .84f), .45f);
            var dark = SportsEventAuthoring.Mat(Folder, "Carbon radiation shutters", new Color(.055f, .08f, .12f), .6f);
            var metal = SportsEventAuthoring.Mat(Folder, "Titanium seams", new Color(.28f, .32f, .38f), .7f); metal.SetFloat("_Metallic", .8f);
            var cyan = Glow("Cyan airlock lighting", new Color(.15f, .8f, 1)); var amber = Glow("Meteor warning amber", new Color(1, .25f, .02f));
            var colours = new[] { Color.cyan, new Color(1,.4f,.2f), Color.green, Color.magenta, Color.yellow, new Color(.35f,.5f,1), new Color(1,.3f,.6f), Color.white };
            match.trucks = new SportsEventTruck[8]; match.pilots = new MoonTruck[8]; match.stations = new MoonStation[8];
            for (int i = 0; i < 8; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LWS.TruckFareFight.Editor.FareFightVehicleAuthoring.PrefabPath);
                if (prefab == null) throw new InvalidOperationException("Existing full NWH tractor required; never regenerate its prefab.");
                var rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab); rig.name = "Moon original full NWH fixed owner " + i;
                rig.transform.SetPositionAndRotation(match.Base(i), Quaternion.LookRotation(-Vector3.ProjectOnPlane(match.Base(i), Vector3.up)));
                var truck = rig.GetComponent<SportsEventTruck>(); if (truck == null || truck.tractorMotor == null) throw new InvalidOperationException("Existing NWH facade required.");
                truck.session = match; truck.slot = i; truck.tractorMotor.settings = match.settings; match.trucks[i] = truck;
                var pilot = rig.AddComponent<MoonTruck>(); pilot.match = match; pilot.truck = truck; pilot.jets = Jets(rig.transform, cyan); match.pilots[i] = pilot;
                foreach(var body in rig.GetComponentsInChildren<Rigidbody>(true))if(body!=truck.body){var relay=body.gameObject.AddComponent<MoonCollisionRelay>();relay.owner=pilot;relay.contactBody=body;}
                EditorUtility.SetDirty(truck); EditorUtility.SetDirty(truck.tractorMotor);
                var station = new GameObject("Actual flag delivery station " + (i + 1)); station.transform.SetParent(root, false); station.transform.position = match.Base(i) - Vector3.up * .7f;
                var v = station.AddComponent<BoxCollider>(); v.isTrigger = true; v.center = Vector3.up * 3; v.size = new Vector3(18, 7, 18);
                var s = station.AddComponent<MoonStation>(); s.owner = i; s.match = match; s.volume = v; match.stations[i] = s;
                var ink = Glow("Base " + (i + 1) + " light", colours[i]); Ring(station.transform, 8.5f, 9.1f, .06f, ink, "Numbered capture pad rim " + i);
                var label = SportsEventAuthoring.Label("Your actual flag station", "BASE " + (i + 1), station.transform.position + new Vector3(0, .1f, 1), .9f, colours[i]); label.transform.rotation = Quaternion.Euler(90, 0, 0); label.transform.SetParent(station.transform, true);
                Vector3 outward = station.transform.position.normalized; Habitat(root, station.transform.position + outward * 25, Quaternion.LookRotation(-outward), hull, dark, metal, cyan, i);
            }
            match.flags = new MoonFlag[5]; match.flagSeeds = new Transform[5];
            var pole = SportsEventAuthoring.Mat(Folder, "Flag polished alloy", new Color(.75f, .77f, .8f), .65f);
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * 2 / 5; Vector3 pos = new Vector3(Mathf.Sin(a) * 16, 1.7f, Mathf.Cos(a) * 16);
                var seed = new GameObject("Stable flag seed " + i).transform; seed.SetParent(root, false); seed.position = pos; match.flagSeeds[i] = seed;
                var g = new GameObject("Actual physical neutral flag " + i); g.transform.SetParent(root, false); g.transform.position = pos;
                var shape = g.AddComponent<BoxCollider>(); shape.size = new Vector3(.8f, 3, .8f); shape.sharedMaterial = contact;
                var body = g.AddComponent<Rigidbody>(); body.mass = 3; body.linearDamping = .08f; body.angularDamping = .5f; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; body.solverIterations = 16;
                var f = g.AddComponent<MoonFlag>(); f.match = match; f.id = i; f.body = body; f.shape = shape; match.flags[i] = f;
                Beam(g.transform, new Vector3(0, -1.5f, 0), new Vector3(0, 1.5f, 0), .055f, pole, "Physical flag pole");
                Shape(PrimitiveType.Cylinder, "Weighted flag foot", g.transform, new Vector3(0,-1.45f,0), new Vector3(.75f,.06f,.75f), pole, false);
                var cloth = Shape(PrimitiveType.Cube, "Folded flagcloth with luminous crest", g.transform, new Vector3(.64f,.85f,0), new Vector3(1.25f,.9f,.025f), cyan, false); f.banner = cloth.transform;
                Ring(root, 2.7f, 3, .05f, cyan, "Flag retrieval spot " + i, new Vector3(pos.x,0,pos.z));
            }
            match.meteors = new MoonMeteor[4];
            for (int i = 0; i < 4; i++)
            {
                var g = new GameObject("Bounded physical meteor " + i); g.transform.SetParent(root, false);
                var body = g.AddComponent<Rigidbody>(); body.mass = 90; body.isKinematic = true; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                var shape = g.AddComponent<SphereCollider>(); shape.radius = .9f; shape.enabled = false;
                var model = RenderCopy(Rock, g.transform); Fit(model, Vector3.one * 2); var rock = model.GetComponentInChildren<Renderer>();rock.sharedMaterial=regolith;
                var meteor = g.AddComponent<MoonMeteor>(); meteor.match = match; meteor.body = body; meteor.shape = shape; meteor.rock = rock;
                meteor.warningRing = Ring(root, 8.4f, 9, .08f, amber, "Three second meteor warning " + i).transform;
                meteor.explosion = Effect(Explosion, root, "Owned meteor explosion pool " + i); meteor.dust = Effect(Dust, root, "Owned lunar impact dust pool " + i);
                match.meteors[i] = meteor; meteor.ResetMeteor();
            }
            for (int i = 0; i < 36; i++)
            {
                float a = i * Mathf.PI * 2 / 36, r = 115 + i % 4 * 4; Vector3 p = new Vector3(Mathf.Sin(a)*r,0,Mathf.Cos(a)*r);
                var rock = RenderCopy(Rock, root); Fit(rock, new Vector3(8+i%3*3, 7+i%5*4, 7+i%4*2)); rock.transform.position = p; rock.name = "Real textured lunar rim mountain " + i;foreach(var renderer in rock.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=regolith;
                rock.AddComponent<BoxCollider>(); // These perimeter rocks remain outside the playable field.
            }
            Space(root, hull, cyan); ResearchDome(root, new Vector3(0, 0, 108), hull, dark, metal, cyan);
            var surface = groundRoot.gameObject.AddComponent<NavMeshSurface>(); surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders; surface.BuildNavMesh();
            if (surface.navMeshData == null) throw new InvalidOperationException("Real lunar terrain navigation bake required.");
            AssetDatabase.CreateAsset(surface.navMeshData, "Assets/TruckMoonMadness/Materials/Lunar actual terrain navigation.asset");
            match.broadcast = SportsEventAuthoring.Camera("Moon original Cinemachine field"); match.cameras = new[] { match.broadcast.view };
            match.broadcast.Pose(Vector3.up * 3, new Vector3(0,115,-115),60,true);
        }
        static float Height(float x, float z)
        {
            float h = 0;
            foreach (var c in Craters)
            {
                float d = Vector2.Distance(new Vector2(x,z),c);
                if(d<15) h += -3.5f * Mathf.Pow(Mathf.Max(0,1-d*d/225),2) + 1.2f * Mathf.Exp(-Mathf.Pow((d-13)/2.5f,2));
            }
            // Broad radial corridors are real terrain ramps, not invisible bridges.
            float angle = Mathf.Atan2(x,z)*Mathf.Rad2Deg, radial = Mathf.Sqrt(x*x+z*z);
            float nearest = Mathf.Abs(Mathf.DeltaAngle(angle,Mathf.Round(angle/45)*45));
            float lane = Mathf.Clamp01((Mathf.Sin(nearest*Mathf.Deg2Rad)*radial-6)/6);
            return h*lane;
        }
        static void Terrain(Transform root,Material material,PhysicsMaterial contact)
        {
            const int n=96;var vertices=new Vector3[(n+1)*(n+1)];var uv=new Vector2[vertices.Length];var triangles=new int[n*n*6];
            for(int z=0;z<=n;z++)for(int x=0;x<=n;x++){int i=z*(n+1)+x;float px=-128+x*256f/n,pz=-128+z*256f/n;vertices[i]=new Vector3(px,Height(px,pz),pz);uv[i]=new Vector2(x/(float)n,z/(float)n);}
            int at=0;for(int z=0;z<n;z++)for(int x=0;x<n;x++){int i=z*(n+1)+x;triangles[at++]=i;triangles[at++]=i+n+1;triangles[at++]=i+1;triangles[at++]=i+1;triangles[at++]=i+n+1;triangles[at++]=i+n+2;}
            var mesh=new Mesh{name="Real lunar crater and ramp collision terrain",vertices=vertices,uv=uv,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,"Assets/TruckMoonMadness/Materials/Lunar crater terrain.asset");
            var g=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));g.transform.SetParent(root,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=material;g.GetComponent<MeshCollider>().sharedMesh=mesh;g.GetComponent<MeshCollider>().sharedMaterial=contact;
        }
        static void Habitat(Transform parent,Vector3 p,Quaternion rotation,Material hull,Material dark,Material metal,Material light,int number)
        {
            var root=new GameObject("Purpose-authored lunar habitat " +(number+1)).transform;root.SetParent(parent,false);root.SetPositionAndRotation(p,rotation);
            var module=Shape(PrimitiveType.Cylinder,"Pressurized cylindrical crew module",root,new Vector3(0,5,0),new Vector3(10,9,10),hull,true);module.transform.localRotation=Quaternion.Euler(90,0,0);
            for(int k=-1;k<=1;k+=2){Shape(PrimitiveType.Cube,"Load-bearing habitat skid",root,new Vector3(k*4,.7f,0),new Vector3(.8f,1.4f,20),metal,true);for(int l=-1;l<=1;l+=2)Beam(root,new Vector3(k*4,1,l*6),new Vector3(k*3.5f,4,l*6),.25f,metal,"Actual landing strut");}
            for(int z=-3;z<=3;z++)Ring(root,4.9f,5.06f,z*2,metal,"Habitat pressure seam "+number+"-"+z,Vector3.zero,Quaternion.Euler(90,0,0),new Vector3(0,5,0));
            Shape(PrimitiveType.Cube,"Solid airlock frame",root,new Vector3(0,3.4f,9.2f),new Vector3(4,6.8f,1),metal,true);
            Shape(PrimitiveType.Cube,"Inset airlock door",root,new Vector3(0,3.5f,9.8f),new Vector3(3.1f,5.7f,.15f),dark,false);
            for(int x=-1;x<=1;x+=2)Shape(PrimitiveType.Cube,"Airlock luminous guide",root,new Vector3(x*1.8f,3.4f,9.9f),new Vector3(.12f,6,.08f),light,false);
            for(int k=-1;k<=1;k+=2)for(int z=-1;z<=1;z+=2){Shape(PrimitiveType.Cube,"Recessed radiation window",root,new Vector3(k*4.9f,5,z*4),new Vector3(.2f,2.1f,3.2f),dark,false);Shape(PrimitiveType.Cube,"Shielded blue observation glass",root,new Vector3(k*5.02f,5,z*4),new Vector3(.05f,1.6f,2.7f),light,false);}
            for(int i=0;i<5;i++)Shape(PrimitiveType.Cube,"Airlock approach stair",root,new Vector3(0,.2f+i*.23f,11+i*.8f),new Vector3(4,.4f+i*.46f,.8f),metal,true);
            for(int side=-1;side<=1;side+=2)for(int row=0;row<3;row++){var panel=Shape(PrimitiveType.Cube,"Solar cell array with structural frame",root,new Vector3(side*11,3+row*.08f,-5+row*5),new Vector3(9,.18f,4.6f),dark,false);for(int c=-3;c<=3;c++)Shape(PrimitiveType.Cube,"Solar cell conductor",root,panel.transform.localPosition+new Vector3(c*1.1f,.1f,0),new Vector3(.035f,.025f,4.4f),metal,false);}
            Beam(root,new Vector3(0,6,-10),new Vector3(0,17,-10),.18f,metal,"Communications mast");var dish=Shape(PrimitiveType.Cylinder,"Parabolic communications dish rim",root,new Vector3(0,17,-10),new Vector3(4,.2f,4),hull,false);dish.transform.localRotation=Quaternion.Euler(35,0,0);Beam(root,new Vector3(0,17,-10),new Vector3(0,20,-8),.07f,metal,"Dish receiver boom");
            var label=SportsEventAuthoring.Label("Habitat identity","LUNAR RESEARCH / "+(number+1),root.TransformPoint(new Vector3(0,8,10)),.38f,Color.white);label.transform.SetParent(root,true);label.transform.rotation=rotation;MoonPresentationAuthoring.ConfigureNewLabel(label);
        }
        static void ResearchDome(Transform parent,Vector3 p,Material hull,Material dark,Material metal,Material light)
        {
            var root=new GameObject("Detailed research observatory dome").transform;root.SetParent(parent,false);root.position=p;
            Shape(PrimitiveType.Cylinder,"Raised insulated research foundation",root,new Vector3(0,1,0),new Vector3(19,1,19),hull,true);
            Shape(PrimitiveType.Sphere,"Research dome pressure shell",root,new Vector3(0,1,0),new Vector3(18,18,18),dark,true);
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6;for(int s=0;s<8;s++){float t=s*Mathf.PI/16,u=(s+1)*Mathf.PI/16;Beam(root,new Vector3(Mathf.Cos(a)*9*Mathf.Cos(t),1+9*Mathf.Sin(t),Mathf.Sin(a)*9*Mathf.Cos(t)),new Vector3(Mathf.Cos(a)*9*Mathf.Cos(u),1+9*Mathf.Sin(u),Mathf.Sin(a)*9*Mathf.Cos(u)),.09f,metal,"Geodesic observatory rib");}}
            Shape(PrimitiveType.Cube,"Observatory lighted airlock",root,new Vector3(0,2.5f,-10),new Vector3(5,5,5),hull,true);Shape(PrimitiveType.Cube,"Observatory opaque door",root,new Vector3(0,2.5f,-12.6f),new Vector3(3,4,.1f),light,false);
        }
        static void Space(Transform parent,Material hull,Material glow)
        {
            var sky=SportsEventAuthoring.Mat(Folder,"Lunar airless star sky",Color.black);sky.shader=Shader.Find("Skybox/Procedural");sky.SetColor("_SkyTint",new Color(.005f,.008f,.015f));sky.SetColor("_GroundColor",new Color(.005f,.005f,.008f));sky.SetFloat("_AtmosphereThickness",0);sky.SetFloat("_Exposure",.5f);RenderSettings.skybox=sky;RenderSettings.ambientLight=new Color(.2f,.23f,.3f);
            var earthRoot=new GameObject("Purpose-authored recognizable Earth above horizon").transform;earthRoot.SetParent(parent,false);earthRoot.position=new Vector3(0,190,410);earthRoot.localScale=Vector3.one*74;
            var blue=SportsEventAuthoring.Mat(Folder,"Earth deep oceans",new Color(.04f,.22f,.55f),.3f);var land=SportsEventAuthoring.Mat(Folder,"Earth continent silhouette",new Color(.2f,.47f,.29f),.1f);var ice=SportsEventAuthoring.Mat(Folder,"Earth polar caps",new Color(.9f,.96f,1),.2f);
            Shape(PrimitiveType.Sphere,"Earth ocean globe",earthRoot,Vector3.zero,Vector3.one,blue,false);
            // Authored geographic silhouette polygons; no unlicensed image or pretend asset intake.
            float[][] continents={new[]{-165f,65,-140,70,-105,55,-80,50,-60,48,-82,25,-100,18,-115,30,-130,50},new[]{-80f,10,-65,10,-40,-5,-48,-25,-70,-55,-77,-20},new[]{-18f,34,10,37,35,28,48,10,35,-20,20,-35,5,-20,-10,5},new[]{-10f,38,0,58,40,70,100,73,150,58,170,50,130,30,105,5,80,10,65,25,30,35},new[]{112f,-12,135,-10,155,-25,145,-40,115,-35}};
            for(int i=0;i<continents.Length;i++)GeoPolygon(earthRoot,continents[i],land,"Earth continental coastline "+i);
            Shape(PrimitiveType.Sphere,"Earth north polar ice",earthRoot,new Vector3(0,.485f,0),new Vector3(.2f,.035f,.2f),ice,false);Shape(PrimitiveType.Sphere,"Earth south polar ice",earthRoot,new Vector3(0,-.485f,0),new Vector3(.24f,.035f,.24f),ice,false);earthRoot.localRotation=Quaternion.Euler(0,160,-23);
            var star=Glow("Distant white stars",new Color(.7f,.82f,1));for(int i=0;i<110;i++){float a=i*2.399963f,y=.18f+.75f*((i*37)%109)/109f;Vector3 d=new Vector3(Mathf.Cos(a)*Mathf.Sqrt(1-y*y),y,Mathf.Sin(a)*Mathf.Sqrt(1-y*y));Shape(PrimitiveType.Sphere,"Distant star "+i,parent,d*650,Vector3.one*(i%5==0?1.2f:.55f),star,false);}
        }
        static Vector3 Geo(float lon,float lat){float a=lon*Mathf.Deg2Rad,b=lat*Mathf.Deg2Rad;return new Vector3(Mathf.Cos(b)*Mathf.Sin(a),Mathf.Sin(b),Mathf.Cos(b)*Mathf.Cos(a))*.501f;}
        static void GeoPolygon(Transform root,float[] points,Material material,string name)
        {
            int n=points.Length/2;float lon=0,lat=0;for(int i=0;i<n;i++){lon+=points[i*2];lat+=points[i*2+1];}lon/=n;lat/=n;var vertices=new List<Vector3>();var indices=new List<int>();
            for(int e=0;e<n;e++){int next=(e+1)%n;const int subdivisions=12;for(int row=0;row<subdivisions;row++)for(int col=0;col<=row;col++){float u=row/(float)subdivisions,v=col/(float)subdivisions,w=(row+1)/(float)subdivisions;float a=points[e*2]-lon,b=points[next*2]-lon,c=points[e*2+1]-lat,d=points[next*2+1]-lat;int k=vertices.Count;vertices.Add(Geo(lon+a*(u-v)+b*v,lat+c*(u-v)+d*v));vertices.Add(Geo(lon+a*(w-v)+b*v,lat+c*(w-v)+d*v));vertices.Add(Geo(lon+a*(w-v-1f/subdivisions)+b*(v+1f/subdivisions),lat+c*(w-v-1f/subdivisions)+d*(v+1f/subdivisions)));indices.Add(k);indices.Add(k+2);indices.Add(k+1);if(col<row){k=vertices.Count;vertices.Add(Geo(lon+a*(u-v)+b*v,lat+c*(u-v)+d*v));vertices.Add(Geo(lon+a*(w-v-1f/subdivisions)+b*(v+1f/subdivisions),lat+c*(w-v-1f/subdivisions)+d*(v+1f/subdivisions)));vertices.Add(Geo(lon+a*(u-v-1f/subdivisions)+b*(v+1f/subdivisions),lat+c*(u-v-1f/subdivisions)+d*(v+1f/subdivisions)));indices.Add(k);indices.Add(k+2);indices.Add(k+1);}}}
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);for(int i=0;i<indices.Count;i+=3){Vector3 a=vertices[indices[i]],b=vertices[indices[i+1]],c=vertices[indices[i+2]];if(Vector3.Dot(Vector3.Cross(b-a,c-a),a+b+c)<0){int swap=indices[i+1];indices[i+1]=indices[i+2];indices[i+2]=swap;}}mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(root,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=material;AssetDatabase.CreateAsset(mesh,"Assets/TruckMoonMadness/Materials/"+name+".asset");
        }
        static Material Glow(string name,Color color){var m=SportsEventAuthoring.Mat(Folder,name,color,.4f);m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*1.6f);return m;}
        static GameObject Shape(PrimitiveType type,string name,Transform parent,Vector3 p,Vector3 size,Material material,bool collision){var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;if(!collision)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
        static void Beam(Transform parent,Vector3 a,Vector3 b,float r,Material mat,string name){var g=Shape(PrimitiveType.Cylinder,name,parent,(a+b)*.5f,new Vector3(r*2,Vector3.Distance(a,b)*.5f,r*2),mat,false);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
        static GameObject Ring(Transform root,float inner,float outer,float y,Material material,string name,Vector3 offset=default,Quaternion rotation=default,Vector3 origin=default)
        {
            const int n=96;var v=new Vector3[(n+1)*2];var triangles=new int[n*6];var uv=new Vector2[v.Length];for(int i=0;i<=n;i++){float a=i*Mathf.PI*2/n;v[i*2]=new Vector3(Mathf.Sin(a)*inner,y,Mathf.Cos(a)*inner);v[i*2+1]=new Vector3(Mathf.Sin(a)*outer,y,Mathf.Cos(a)*outer);uv[i*2]=new Vector2(i/(float)n,0);uv[i*2+1]=new Vector2(i/(float)n,1);if(i<n){int k=i*6,j=i*2;triangles[k]=j;triangles[k+1]=j+1;triangles[k+2]=j+2;triangles[k+3]=j+1;triangles[k+4]=j+3;triangles[k+5]=j+2;}}
            var mesh=new Mesh{name=name,vertices=v,uv=uv,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,"Assets/TruckMoonMadness/Materials/"+name+".asset");var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(root,false);g.transform.localPosition=offset+origin;if(rotation!=default)g.transform.localRotation=rotation;g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=material;return g;
        }
        static GameObject RenderCopy(string path,Transform parent){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab==null)throw new InvalidOperationException("Owned model required: "+path);var g=UnityEngine.Object.Instantiate(prefab,parent);foreach(var c in g.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(c);foreach(var c in g.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);foreach(var b in g.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(b);return g;}
        static void Fit(GameObject g,Vector3 size){var bounds=new Bounds();bool first=true;foreach(var r in g.GetComponentsInChildren<Renderer>(true)){if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}if(first||bounds.size.sqrMagnitude<.001f)throw new InvalidOperationException("Actual model bounds required.");g.transform.localScale=Vector3.Scale(g.transform.localScale,new Vector3(size.x/bounds.size.x,size.y/bounds.size.y,size.z/bounds.size.z));bounds=g.GetComponentsInChildren<Renderer>(true)[0].bounds;g.transform.position+=g.transform.parent.position-bounds.center;}
        static ParticleSystem Effect(string path,Transform root,string name){var g=RenderCopy(path,root);g.name=name;var particles=g.GetComponentsInChildren<ParticleSystem>(true);if(particles.Length==0)throw new InvalidOperationException("Actual installed particle effect required.");foreach(var p in particles){var main=p.main;main.playOnAwake=false;main.loop=false;main.stopAction=ParticleSystemStopAction.None;p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}return particles[0];}
        static ParticleSystem[] Jets(Transform parent,Material material){var result=new ParticleSystem[4];for(int i=0;i<4;i++){var g=new GameObject("Limited visible attitude thruster "+i);g.transform.SetParent(parent,false);g.transform.localPosition=new Vector3(i%2==0?-1.4f:1.4f,.9f,i<2?-2:2);g.transform.localRotation=Quaternion.Euler(90,0,0);var p=g.AddComponent<ParticleSystem>();var m=p.main;m.playOnAwake=false;m.startLifetime=.16f;m.startSpeed=4;m.startSize=.16f;m.startColor=new Color(.25f,.7f,1);m.maxParticles=32;m.simulationSpace=ParticleSystemSimulationSpace.World;var e=p.emission;e.rateOverTime=45;var sh=p.shape;sh.shapeType=ParticleSystemShapeType.Cone;sh.angle=12;sh.radius=.05f;g.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);result[i]=p;}return result;}
        static AudioClip[] Audio(){string[] paths={"Assets/Heat - Complete Modern UI/Audio/Click.wav","Assets/TruckBilliards/Audio/Ball.wav","Assets/NWH/Vehicle Physics 2/Sound/Horn/Horn.wav","Assets/TruckBilliards/Audio/Strike.wav","Assets/TruckHockey/Audio/Faceoff.wav","Assets/TruckHockey/Audio/GoalHorn.wav","Assets/TruckHockey/Audio/FinalWhistle.wav"};return paths.Select(p=>{var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(p);if(clip==null)throw new InvalidOperationException("Owned audio required: "+p);return clip;}).ToArray();}
    }
}
