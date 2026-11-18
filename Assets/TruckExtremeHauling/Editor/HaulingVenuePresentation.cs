using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LWS.TruckSports.Editor;

namespace LWS.TruckExtremeHauling.Editor
{
    // Explicit, additive finishing of the newly authored candidate venues.
    // Normal builds and entering Play Mode never invoke this migration.
    public static class HaulingVenuePresentation
    {
        public static void Upgrade()
        {
            Orbital(); CableGantry(); TossRamps(); RepairRingFrames(); RepairCableAnchors(); UpgradeFullSizePlinko(); RepairPlinkoPegContacts(); AddSavedPlinkoContainment(); AssetDatabase.SaveAssets();
        }
        static Material Owned(string name)
        { var asset=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckMoonMadness/Materials/"+name+".mat");if(asset==null)throw new InvalidOperationException("Existing owned space visual missing: "+name);return asset; }
        static GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        { var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g; }
        static void Orbital()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TruckExtremeHauling/Scenes/ZeroGHitchUp.unity");
            if(GameObject.Find("Authored orbital freight skyline")!=null)return;
            var match=UnityEngine.Object.FindAnyObjectByType<ZeroGHitchMatch>();if(match==null)throw new InvalidOperationException("Orbital authority required.");
            var support=GameObject.Find("Continuous venue support");if(support!=null)UnityEngine.Object.DestroyImmediate(support);
            RenderSettings.skybox=Owned("Lunar airless star sky");RenderSettings.ambientLight=new Color(.2f,.23f,.3f);RenderSettings.fog=false;
            var root=new GameObject("Authored orbital freight skyline").transform;
            var earth=new GameObject("Distant Earth · shared original visual art").transform;earth.SetParent(root,false);earth.position=new Vector3(120,180,410);earth.localScale=Vector3.one*100;earth.localRotation=Quaternion.Euler(0,160,-23);
            Shape("Ocean globe",PrimitiveType.Sphere,earth,Vector3.zero,Vector3.one,Owned("Earth deep oceans"));
            for(int i=0;i<5;i++)
            {
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TruckMoonMadness/Materials/Earth continental coastline "+i+".asset");if(mesh==null)throw new InvalidOperationException("Existing owned geographic visual required.");
                var land=new GameObject("Geographic silhouette "+i,typeof(MeshFilter),typeof(MeshRenderer));land.transform.SetParent(earth,false);land.GetComponent<MeshFilter>().sharedMesh=mesh;land.GetComponent<Renderer>().sharedMaterial=Owned("Earth continent silhouette");
            }
            Shape("North polar cap",PrimitiveType.Sphere,earth,new Vector3(0,.485f,0),new Vector3(.2f,.035f,.2f),Owned("Earth polar caps"));
            Shape("South polar cap",PrimitiveType.Sphere,earth,new Vector3(0,-.485f,0),new Vector3(.24f,.035f,.24f),Owned("Earth polar caps"));
            var stars=Owned("Distant white stars");
            for(int i=0;i<110;i++){float a=i*2.399963f,y=.18f+.75f*((i*37)%109)/109f;Vector3 d=new Vector3(Mathf.Cos(a)*Mathf.Sqrt(1-y*y),y,Mathf.Sin(a)*Mathf.Sqrt(1-y*y));Shape("Distant star "+i,PrimitiveType.Sphere,root,new Vector3(120,40,0)+d*650,Vector3.one*(i%5==0?1.2f:.55f),stars);}
            foreach(var g in scene.GetRootGameObjects())
                if(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g)=="Assets/ShipContainerPack/URP/Prefabs/ShipContainerGray_URP.prefab")g.transform.position+=Vector3.up*35;
            EditorSceneManager.SaveScene(scene);
        }
        static void CableGantry()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TruckExtremeHauling/Scenes/SkyhookSprint.unity");
            if(GameObject.Find("Freight cable gantries")!=null)return;
            var match=UnityEngine.Object.FindAnyObjectByType<SkyhookSprintMatch>();if(match==null)throw new InvalidOperationException("Cable authority required.");
            var root=new GameObject("Freight cable gantries").transform;
            var steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckExtremeHauling/Materials/Freight tower gunmetal.mat");
            var paint=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckExtremeHauling/Materials/Freight safety orange.mat");
            var contact=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/TruckExtremeHauling/Materials/Freight concrete.asset");
            for(int i=0;i<match.cableCars.Length;i++)
            {
                float x=match.cableCars[i].trolley.position.x;
                foreach(float z in new[]{-100f,245f})
                {
                    foreach(int side in new[]{-1,1})
                    {
                        SportsEventAuthoring.Box("Steel gantry column",new Vector3(x+side*15,33,z),new Vector3(2,66,2),steel,contact,root);
                        var brace=SportsEventAuthoring.Box("Diagonal gantry brace",new Vector3(x+side*7.5f,54,z),new Vector3(1,31,1),paint,null,root);brace.transform.rotation=Quaternion.Euler(0,0,side*-30);
                    }
                    SportsEventAuthoring.Box("Gantry crosshead",new Vector3(x,66,z),new Vector3(34,2,3),paint,null,root);
                }
                SportsEventAuthoring.Box("Visible overhead trolley rail",new Vector3(x,65,72.5f),new Vector3(.65f,.8f,345),steel,null,root);
                var trolley=Shape("Cable trolley housing",PrimitiveType.Cube,match.cableCars[i].trolley.transform,Vector3.zero,new Vector3(2.4f,1.4f,3),paint);
                trolley.transform.localRotation=Quaternion.identity;
            }
            EditorSceneManager.SaveScene(scene);
        }
        static void RepairCableAnchors()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TruckExtremeHauling/Scenes/SkyhookSprint.unity");
            var match=UnityEngine.Object.FindAnyObjectByType<SkyhookSprintMatch>();
            foreach(var car in match.cableCars)
            {
                // NWH's authored center-of-mass marker is the reference used at runtime.
                var marker=car.load.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="COM");
                Vector3 center=marker!=null?car.load.transform.InverseTransformPoint(marker.position):car.load.centerOfMass;
                // Center the lifting eye longitudinally; preserve a real suspension arm above the load COM.
                car.cable.anchor=center+Vector3.up*3;
                Physics.SyncTransforms();
                Vector3 top=car.load.transform.TransformPoint(car.cable.anchor)+Vector3.up*car.cable.maxDistance;
                car.trolley.position=top;car.trolley.transform.position=top;
                var rail=car.trolley.GetComponent<ConfigurableJoint>();rail.anchor=Vector3.zero;rail.connectedAnchor=top;
                car.routeEnd=new Vector3(match.objectives[Array.IndexOf(match.cableCars,car)].position.x,top.y,match.objectives[Array.IndexOf(match.cableCars,car)].position.z);
                EditorUtility.SetDirty(car);EditorUtility.SetDirty(car.cable);EditorUtility.SetDirty(car.trolley);EditorUtility.SetDirty(rail);
            }
            Physics.SyncTransforms();EditorSceneManager.SaveScene(scene);
            Debug.Log("HH10_CABLE_SETUP_REPAIRED actual saved trolley transforms and centered lifting eyes.");
        }
        static void UpgradeFullSizePlinko()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TruckExtremeHauling/Scenes/TrailerPlinko.unity");
            if(GameObject.Find("Full-size trailer Plinko spacing v2")!=null)return;
            var match=UnityEngine.Object.FindAnyObjectByType<TrailerPlinkoMatch>();var origins=match.boardOrigins;
            var old=origins.Select(t=>t.position).ToArray();
            string[] parts={"Board back","Catch tray","Board sidewall","Physical rebound peg","Bin separator"};
            foreach(var shape in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if(shape.transform.parent!=null||!parts.Contains(shape.name))continue;
                int nearest=0;for(int i=1;i<old.Length;i++)if(Mathf.Abs(shape.transform.position.x-old[i].x)<Mathf.Abs(shape.transform.position.x-old[nearest].x))nearest=i;
                shape.transform.SetParent(origins[nearest],true);
            }
            for(int i=0;i<origins.Length;i++)
            {
                origins[i].position=new Vector3(i*180,160,0);origins[i].localScale=Vector3.one;
                foreach(Transform part in origins[i]){part.localPosition*=2.5f;part.localScale*=2.5f;}
                match.trailers[i].transform.SetPositionAndRotation(origins[i].position,Quaternion.Euler(90,0,0));
                match.trucks[i].transform.position=new Vector3(i*180,1,-70);
            }
            var support=GameObject.Find("Continuous venue support");if(support!=null){support.transform.position=new Vector3(270,-1,80);support.transform.localScale=new Vector3(850,2,500);}
            match.boardScale=2.5f;new GameObject("Full-size trailer Plinko spacing v2");
            Physics.SyncTransforms();EditorUtility.SetDirty(match);EditorSceneManager.SaveScene(scene);
            Debug.Log("HH10_PLINKO_SPACING_REPAIRED full-size trailers, 25m peg intervals, 25m bins and separated physical boards.");
        }
        static void RepairPlinkoPegContacts()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TruckExtremeHauling/Scenes/TrailerPlinko.unity");
            var match=UnityEngine.Object.FindAnyObjectByType<TrailerPlinkoMatch>();int repaired=0;
            foreach(var peg in UnityEngine.Object.FindObjectsByType<SphereCollider>(FindObjectsSortMode.None))
            {
                if(peg.name!="Physical rebound peg")continue;
                var material=peg.sharedMaterial;var target=peg.gameObject;UnityEngine.Object.DestroyImmediate(peg);
                var pin=target.AddComponent<CapsuleCollider>();pin.direction=2;pin.radius=.5f;pin.height=1;pin.sharedMaterial=material;repaired++;
            }
            // SphereCollider scales to the largest axis, making a visually narrow
            // extruded peg an invisible 22.5m ball. A real rounded pin preserves
            // the visible in-plane radius and the physical board depth.
            foreach(var origin in match.boardOrigins)
                for(int bin=0;bin<match.binPoints.Length;bin++)
                {
                    if(origin.Find("Bin points "+bin)!=null)continue;
                    var label=new GameObject("Bin points "+bin).AddComponent<TMPro.TextMeshPro>();label.transform.SetParent(origin,false);
                    label.transform.localPosition=new Vector3((-20+bin*10)*match.boardScale,-54*match.boardScale,-6*match.boardScale);
                    label.text=match.binPoints[bin].ToString("0");label.font=TMPro.TMP_Settings.defaultFontAsset;label.fontSize=18;label.alignment=TMPro.TextAlignmentOptions.Center;
                    label.rectTransform.sizeDelta=new Vector2(18,7);label.color=bin==2?new Color(1,.85f,.15f):Color.white;
                }
            EditorSceneManager.SaveScene(scene);Debug.Log("HH10_PLINKO_PEG_CONTACTS_REPAIRED "+repaired+" visible rounded pins; supported bins labelled.");
        }
        public static void AddSavedPlinkoContainment()
        {
            var scene = EditorSceneManager.OpenScene("Assets/TruckExtremeHauling/Scenes/TrailerPlinko.unity");
            var match = UnityEngine.Object.FindAnyObjectByType<TrailerPlinkoMatch>();
            if (AddPlinkoContainment(match))
            {
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Could not save the Plinko containment screens.");
            }
            AssetDatabase.SaveAssets();
            Debug.Log("HH10_PLINKO_CONTAINMENT_SAVED four physical clear screens retain side rebounds without changing pegs or trays.");
        }

        internal static bool AddPlinkoContainment(TrailerPlinkoMatch match)
        {
            if (match == null || match.boardOrigins.Length != 4 || match.boardOrigins.Any(board => board == null))
                throw new InvalidOperationException("All four physical Plinko boards are required.");
            const string folder = "TruckExtremeHauling";
            const string screenName = "Clear physical rebound screen";
            if (match.boardOrigins.All(board => board.Find(screenName) != null)) return false;
            var glass = SportsEventAuthoring.Mat(folder, "Plinko clear blue rebound screen", new Color(.25f, .65f, .9f, .1f), .78f);
            glass.SetFloat("_Surface", 1);
            glass.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            glass.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            glass.SetFloat("_ZWrite", 0);
            glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            glass.SetOverrideTag("RenderType", "Transparent");
            glass.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(glass);
            var steel = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckExtremeHauling/Materials/Freight tower gunmetal.mat");
            if (steel == null) throw new InvalidOperationException("The authored board frame material is missing.");
            var screenContact = SportsEventAuthoring.Contact(folder, "Plinko rebound screen contact", .06f, .12f);
            float scale = match.boardScale;
            foreach (Transform board in match.boardOrigins)
            {
                if (board.Find(screenName) != null) continue;
                var screen = SportsEventAuthoring.Box(screenName, new Vector3(0, -26, -5.4f) * scale,
                    new Vector3(52, 62, .35f) * scale, glass, screenContact, board);
                screen.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                foreach (float edge in new[] { -25.8f, 25.8f })
                    SportsEventAuthoring.Box("Rebound screen frame upright", new Vector3(edge, -26, -5.65f) * scale,
                        new Vector3(.5f, 62, .45f) * scale, steel, null, board);
                foreach (float edge in new[] { -57f, 5f })
                    SportsEventAuthoring.Box("Rebound screen frame rail", new Vector3(0, edge, -5.65f) * scale,
                        new Vector3(52, .5f, .45f) * scale, steel, null, board);
            }
            return true;
        }

        static void RepairRingFrames()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TruckExtremeHauling/Scenes/HitchRingToss.unity");
            var match=UnityEngine.Object.FindAnyObjectByType<HitchRingTossMatch>();
            foreach(var frame in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if(frame.name!="Ring frame"||frame.parent!=null)continue;
                HaulingTargetRing nearest=null;float distance=float.MaxValue;
                foreach(var ring in match.rings){float d=(frame.position-ring.transform.position).sqrMagnitude;if(d<distance){distance=d;nearest=ring;}}
                if(nearest!=null&&distance<225)frame.SetParent(nearest.transform,true);
            }
            EditorSceneManager.SaveScene(scene);
        }
        public static void UpgradeActiveRingTossLaunchRamps()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before configuring launch equipment.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!="Assets/TruckExtremeHauling/Scenes/HitchRingToss.unity")throw new InvalidOperationException("Open the saved Hitch Ring Toss venue.");
            foreach(var ramp in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if(ramp.name=="Momentum launch ramp")
                {
                    var position=ramp.position;position.z=10;ramp.position=position;EditorUtility.SetDirty(ramp);
                }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        static void TossRamps()
        {
            var scene=EditorSceneManager.OpenScene("Assets/TruckExtremeHauling/Scenes/HitchRingToss.unity");
            if(GameObject.Find("Physical trailer launch ramps")!=null)return;
            var match=UnityEngine.Object.FindAnyObjectByType<HitchRingTossMatch>();if(match==null)throw new InvalidOperationException("Toss authority required.");
            var root=new GameObject("Physical trailer launch ramps").transform;
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckExtremeHauling/Materials/Freight safety orange.mat");
            var contact=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/TruckExtremeHauling/Materials/Freight concrete.asset");
            for(int i=0;i<match.trucks.Length;i++)
            {
                var ramp=SportsEventAuthoring.Box("Momentum launch ramp",new Vector3(match.trucks[i].transform.position.x,4,10),new Vector3(16,1,25),material,contact,root);
                ramp.transform.rotation=Quaternion.Euler(-20,0,0);
            }
            EditorSceneManager.SaveScene(scene);
        }
    }
}
