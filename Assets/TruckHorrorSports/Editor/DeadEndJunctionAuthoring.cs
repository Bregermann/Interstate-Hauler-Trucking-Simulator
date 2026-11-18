#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EasyRoads3Dv3;
namespace LWS.TruckHorrorSports.Editor
{
    /// <summary>Explicit EasyRoads-built branch sector for Route Puzzle only.</summary>
    public static class DeadEndJunctionAuthoring
    {
        const string RootName="Authored Dead End branching light puzzle";
        const string Folder="TruckHorrorSports/DeadEnd";
        const string RoadNetwork="Assets/EasyRoads3D/Resources/ERRoadNetwork.prefab";
        const string RoadMaterial="Assets/TruckSnake/Materials/Connected world road asphalt.mat";
        const string Sign="Assets/RoadPropsPack/URP/Prefabs/RoadSign_URP.prefab";
        const string Barrier="Assets/RoadPropsPack/URP/Prefabs/RoadBlock02_URP.prefab";
        static readonly Vector3 Origin=new Vector3(-210,.14f,0);
        static Vector3 World(float x,float z,float y=0)=>Origin+Vector3.forward*x+Vector3.left*z+Vector3.up*y;
        [MenuItem("Truck Sports/Alpha 0.10/Upgrade active Dead End branching puzzle")]
        public static void UpgradeActiveVenue()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=HorrorAuthoring.Scene(HorrorSport.DeadEnd))throw new InvalidOperationException("Open the saved Dead End scene.");
            if(scene.GetRootGameObjects().Any(g=>g.name==RootName)){RepairFinalSignClearance();RepairExitRoadForestClearance();Debug.Log("DEAD_END_JUNCTION_PRESERVED");return;}
            var match=UnityEngine.Object.FindObjectsByType<HorrorMatch>(FindObjectsSortMode.None).Single(m=>m.gameObject.scene==scene);
            var serialized=new SerializedObject(match);if(serialized.FindProperty("junctionPuzzle")==null)throw new InvalidOperationException("Integrate the documented HorrorMatch junctionPuzzle hooks first.");
            Need<GameObject>(RoadNetwork);Need<Material>(RoadMaterial);Need<GameObject>(Sign);Need<GameObject>(Barrier);
            var root=new GameObject(RootName);var puzzle=root.AddComponent<DeadEndJunctionPuzzle>();puzzle.match=match;
            var geometry=Child(root.transform,"Route Puzzle physical roads and clues",Vector3.zero);puzzle.geometry=geometry.gameObject;
            puzzle.entry=Point(geometry,"Physical highway branch entry",0,0);
            var paths=new[]{
                new[]{World(0,-5),World(0,30)},
                new[]{World(0,30),World(24,30),World(24,90),World(0,90)},
                new[]{World(0,30),World(-24,30),World(-24,60),World(-40,60),World(-40,15),World(0,15)},
                new[]{World(0,90),World(0,105)},
                new[]{World(0,105),World(-24,105),World(-24,150),World(0,165)},
                new[]{World(0,105),World(24,105),World(24,135),World(40,135),World(40,90),World(0,90)},
                new[]{World(0,165),World(0,175)},
                new[]{World(0,175),World(24,185)},new[]{World(0,175),World(-24,185)}
            };
            BuildRoads(geometry,paths);
            puzzle.roads=paths.Select(points=>new DeadEndRoadLine{points=points}).ToArray();
            puzzle.gates=new[]{Point(geometry,"Headlight branch receipt",24,60),Point(geometry,"Dark merge receipt",0,90),Point(geometry,"Generator branch receipt",-24,135),Point(geometry,"Unlit final clue receipt",0,165)};
            puzzle.stagePaths=new[]{Path(geometry,"Fork1 approach",new[]{World(0,0),World(0,30),World(24,30),World(24,60)}),Path(geometry,"Merge1 approach",new[]{World(24,60),World(24,90),World(0,90)}),Path(geometry,"Fork2 approach",new[]{World(0,90),World(0,105),World(-24,105),World(-24,135)}),Path(geometry,"Merge2 approach",new[]{World(-24,135),World(-24,150),World(0,165)})};
            puzzle.dawnPath=Path(geometry,"Dawn exit approach",new[]{World(0,165),World(0,175),World(24,185)});puzzle.darkPath=Path(geometry,"Unlit exit approach",new[]{World(0,165),World(0,175),World(-24,185)});
            puzzle.dawnExit=Point(geometry,"Reachable dawn service ending",24,185);puzzle.darkExit=Point(geometry,"Reachable unlit escape ending",-24,185);
            puzzle.falseTurns=new[]{Point(geometry,"Wrong fork1 actual detour",-24,53),Point(geometry,"Wrong fork2 actual detour",24,128)};
            puzzle.cluePositions=new Transform[4];puzzle.clueViewpoints=new Transform[4];puzzle.litClues=new Renderer[4];puzzle.darkClues=new Renderer[4];
            var locations=new[]{new Vector2(29,42),new Vector2(8,85),new Vector2(-29,117),new Vector2(-8,160)};
            var viewpoints=new[]{new Vector2(24,35),new Vector2(18,90),new Vector2(-24,110),new Vector2(-18,154)};
            string[] bright={"SERVICE ROAD >\nFOLLOW THE REFLECTORS","NO EXIT\nKILL YOUR HEADLIGHTS","< GENERATOR ROUTE\nIGNORE THE OTHER HEADLIGHTS","DAWN EXIT >\nLIGHTS ON AT THE SERVICE GATE"};
            string[] dark={"ROAD CLOSED\nTURN YOUR LIGHTS ON","LIGHTS OUT\nTHE BARRIER REMEMBERS","THE CORN ROAD LIES\nTURN YOUR LIGHTS ON","< UNLIT EXIT\nLEAVE YOUR HEADLIGHTS OFF"};
            for(int gate=0;gate<4;gate++)
            {
                Vector3 sign=World(locations[gate].x,locations[gate].y);var holder=Child(geometry,"Physical clue sign "+(gate+1),sign);
                LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(holder,Sign,new Vector3(3,4,1),0,false,Folder);
                var post=holder.gameObject.AddComponent<BoxCollider>();post.center=Vector3.up*1.7f;post.size=new Vector3(.25f,3.4f,.25f);
                Vector3 view=World(viewpoints[gate].x,viewpoints[gate].y);var face=Child(holder,"Real light-dependent clue face",sign+Vector3.up*3.6f+(view-sign).normalized*.7f);face.rotation=Quaternion.LookRotation(sign-view);
                puzzle.cluePositions[gate]=face;puzzle.clueViewpoints[gate]=Child(geometry,"Clue observation approach "+gate,view);
                puzzle.litClues[gate]=Text(face,bright[gate],new Color(1,.88f,.4f));puzzle.darkClues[gate]=Text(face,dark[gate],new Color(.55f,.95f,.85f));
            }
            // Genuine moving closure, unlocked by the existing second objective receipt.
            var hinge=Child(geometry,"Reality-shifting generator-road barrier",World(-12,99));hinge.rotation=Quaternion.LookRotation(Vector3.back);puzzle.shiftingBarrier=hinge;
            for(int i=0;i<3;i++)
            {var section=Child(hinge,"Owned moving road closure "+i,hinge.position+hinge.right*(2+i*4));LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(section,Barrier,new Vector3(4,1.6f,1),0,true,Folder);}
            var closure=hinge.gameObject.AddComponent<BoxCollider>();closure.center=new Vector3(6,.8f,0);closure.size=new Vector3(12,1.6f,1);
            // Physical median blocks prevent bypassing the authored fork choices across grass.
            for(int fork=0;fork<2;fork++)for(int row=0;row<4;row++)
            {var median=Child(geometry,"Owned junction median",World(0,45+fork*75+row*6));LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(median,Barrier,new Vector3(10,2,6),0,true,Folder);var solid=median.gameObject.AddComponent<BoxCollider>();solid.center=Vector3.up;solid.size=new Vector3(10,2,6);median.rotation=Quaternion.LookRotation(Vector3.left);}
            geometry.gameObject.SetActive(false);serialized.FindProperty("junctionPuzzle").objectReferenceValue=puzzle;serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(match);EditorSceneManager.MarkSceneDirty(scene);
            RepairFinalSignClearance();RepairExitRoadForestClearance();
            Debug.Log("DEAD_END_BRANCH_SECTOR_AUTHORED real EasyRoads forks/detours, moving closure, light-dependent signs and two reachable exits; option2 only, no vehicle teleport. Caller saves and runs focused acceptance.");
        }
        [MenuItem("Truck Sports/Alpha 0.10/Repair active Dead End final sign clearance")]
        public static void RepairFinalSignClearance()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before repairing authored sign clearance.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=HorrorAuthoring.Scene(HorrorSport.DeadEnd))throw new InvalidOperationException("Open the saved Dead End scene.");
            var puzzle=UnityEngine.Object.FindObjectsByType<DeadEndJunctionPuzzle>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(p=>p.gameObject.scene==scene);
            if(puzzle.cluePositions==null||puzzle.cluePositions.Length!=4||puzzle.clueViewpoints==null||puzzle.clueViewpoints.Length!=4)throw new InvalidOperationException("The original four authored clue signs are required.");
            var face=puzzle.cluePositions[3];var post=face!=null?face.parent:null;var shape=post!=null?post.GetComponent<BoxCollider>():null;
            if(post==null||shape==null||post.name!="Physical clue sign 4"||Vector3.Distance(shape.size,new Vector3(.25f,3.4f,.25f))>.01f||Vector3.Distance(shape.center,Vector3.up*1.7f)>.01f)throw new InvalidOperationException("Customized final-sign hierarchy/collider preserved; inspect its actual road clearance before applying a tailored repair.");
            Vector3 old=World(-8,160),a=World(-24,150),b=World(0,165),view=World(-18,154);
            Vector3 forward=Vector3.ProjectOnPlane(b-a,Vector3.up).normalized;
            Vector3 shoulder=Vector3.Cross(Vector3.up,forward).normalized;
            Vector3 repaired=old+shoulder*9;
            if(Vector3.Distance(post.position,repaired)<.05f){Debug.Log("DEAD_END_FINAL_SIGN_ALREADY_CLEAR");return;}
            if(Vector3.Distance(post.position,old)>.05f||Vector3.Distance(puzzle.clueViewpoints[3].position,view)>.05f)throw new InvalidOperationException("Customized final sign/look position preserved; this repair only recognizes the original known on-centerline placement.");
            if(puzzle.stagePaths==null||puzzle.stagePaths.Length<4||puzzle.stagePaths[3].points.Length!=3||Vector3.Distance(puzzle.stagePaths[3].points[1].position,a)>.05f||Vector3.Distance(puzzle.stagePaths[3].points[2].position,b)>.05f)throw new InvalidOperationException("Customized final road approach preserved; review its sign clearance separately.");
            Vector3 newFace=repaired+Vector3.up*3.6f+(view-repaired).normalized*.7f;
            float visibleDistance=Vector3.Distance(view,newFace),facing=Vector3.Dot(forward,(newFace-view).normalized);
            if(visibleDistance>=31||facing<=.45f)throw new InvalidOperationException("The unchanged observation reach/facing must still admit the original look position.");
            // The twelve-metre road keeps its six-metre half-width. Nine metres of
            // shoulder offset clears that road plus the fitted cab and physical post.
            // Move the whole colliding post/model and both original glyph children.
            Undo.RecordObject(post,"Move final clue sign off the driving lane");Undo.RecordObject(face,"Keep final clue readable from its original look position");
            post.position=repaired;face.SetPositionAndRotation(newFace,Quaternion.LookRotation(repaired-view));
            EditorUtility.SetDirty(post);EditorUtility.SetDirty(face);EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("DEAD_END_FINAL_SIGN_CLEARANCE_REPAIRED post="+repaired.ToString("F2")+" unchangedLookDistance="+visibleDistance.ToString("F2")+" facing="+facing.ToString("F2")+"; actual gate, road, clue receipt, light parity, radii and AI gains unchanged. Caller saves and reruns the existing natural puzzle test.");
        }
        [MenuItem("Truck Sports/Alpha 0.10/Repair active Dead End exit-road forest clearance")]
        public static void RepairExitRoadForestClearance()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before authoring exit-road clearance.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=HorrorAuthoring.Scene(HorrorSport.DeadEnd))throw new InvalidOperationException("Open the saved Dead End scene.");
            var puzzle=UnityEngine.Object.FindObjectsByType<DeadEndJunctionPuzzle>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(p=>p.gameObject.scene==scene);
            if(puzzle.dawnPath==null||puzzle.darkPath==null||puzzle.dawnPath.points.Length<2||puzzle.darkPath.points.Length<2)throw new InvalidOperationException("Preserve both actual authored exit roads.");
            const string treePath="Assets/Toon Gas Station/Prefabs/Tree_6A.prefab";
            var source=Need<GameObject>(treePath);var meshes=new HashSet<Mesh>(source.GetComponentsInChildren<MeshFilter>(true).Where(m=>m.sharedMesh!=null).Select(m=>m.sharedMesh));
            if(meshes.Count==0)throw new InvalidOperationException("The original owned forest source must retain its mesh geometry.");
            float cabHalf=0;
            foreach(var truck in puzzle.match.trucks)if(truck!=null&&truck.shape!=null)cabHalf=Mathf.Max(cabHalf,truck.shape.size.x*Mathf.Abs(truck.shape.transform.lossyScale.x)*.5f);
            float halfCorridor=6+cabHalf+.75f;
            var roots=new HashSet<GameObject>();
            Physics.SyncTransforms();
            foreach(var shape in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(shape.gameObject.scene!=scene||!shape.enabled||shape.isTrigger||shape.attachedRigidbody!=null||shape is TerrainCollider||shape.gameObject.layer!=0||shape.transform.IsChildOf(puzzle.geometry.transform))continue;
                var bounds=shape.bounds;if(bounds.max.y<=.34f||bounds.min.y>6)continue;
                if(!CrossesExit(bounds,puzzle.dawnPath.points,halfCorridor)&&!CrossesExit(bounds,puzzle.darkPath.points,halfCorridor))continue;
                var root=shape.transform.root.gameObject;
                if(root.name!="Forest silhouette")throw new InvalidOperationException("Unrecognized/custom static exit-road obstruction preserved: "+root.name+" / "+shape.name+". Inspect it rather than masking arbitrary venue geometry.");
                var filters=root.GetComponentsInChildren<MeshFilter>(true);
                if(filters.Length==0||filters.Any(f=>f.sharedMesh!=null&&!meshes.Contains(f.sharedMesh))||root.GetComponentsInChildren<Rigidbody>(true).Length!=0)throw new InvalidOperationException("Customized forest instance preserved: "+root.name+" at "+root.transform.position+". Only the original owned static tree meshes may be bound.");
                roots.Add(root);
            }
            if(puzzle.exitRoadForest!=null)foreach(var tree in puzzle.exitRoadForest)
                if(tree!=null&&(tree.scene!=scene||tree.name!="Forest silhouette"))throw new InvalidOperationException("Designer-customized exit obstruction list preserved; it contains an unrecognized object.");
            var selected=roots.OrderBy(g=>g.transform.GetSiblingIndex()).ToArray();
            if(puzzle.exitRoadForest!=null&&puzzle.exitRoadForest.SequenceEqual(selected)){Debug.Log("DEAD_END_EXIT_FOREST_BINDING_PRESERVED count="+selected.Length);return;}
            Undo.RecordObject(puzzle,"Bind owned forest clearance for the new exit roads");puzzle.exitRoadForest=selected;EditorUtility.SetDirty(puzzle);EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("DEAD_END_EXIT_FOREST_CLEARANCE_BOUND count="+selected.Length+" halfCorridor="+halfCorridor.ToString("F2")+"; actual collider bounds select only known owned tree instances. No instance is deleted/moved/disabled by authoring. Route Puzzle masks them at its normal round boundary and restores exact authored states outside that mode. Caller saves.");
        }
        static bool CrossesExit(Bounds bounds,Transform[] points,float halfCorridor)
        {
            bounds.Expand(new Vector3(halfCorridor*2,0,halfCorridor*2));
            for(int i=1;i<points.Length;i++)
            {
                Vector3 a=points[i-1].position,b=points[i].position;a.y=b.y=Mathf.Clamp(2,bounds.min.y,bounds.max.y);
                Vector3 delta=b-a;float length=delta.magnitude;
                if(bounds.Contains(a)||bounds.Contains(b)||length>.001f&&bounds.IntersectRay(new Ray(a,delta/length),out float distance)&&distance<=length)return true;
            }
            return false;
        }
        static void BuildRoads(Transform parent,Vector3[][] paths)
        {
            var source=Need<GameObject>(RoadNetwork);var instance=(GameObject)PrefabUtility.InstantiatePrefab(source);instance.name="Owned Dead End EasyRoads branch network";instance.transform.SetParent(parent,true);
            var owner=instance.GetComponent<ERModularBase>();owner.RoadNetworkInit();var network=new ERRoadNetwork(owner);
            var material=Need<Material>(RoadMaterial);
            for(int i=0;i<paths.Length;i++)
            {
                var type=new ERRoadType{roadTypeName="Dead End physical fork "+i,roadWidth=12,hasMeshCollider=true,roadMaterial=material,layer=0};
                var road=network.CreateRoad("Night service branch "+i,type,paths[i],Enumerable.Repeat(ERMarkerControlType.StraightXZ,paths[i].Length).ToArray());
                road.SetTerrainDeformation(false,false);road.SnapToTerrain(false,false);road.Refresh();
            }
            network.BuildRoadNetwork(false,false,false);
            var shapes=instance.GetComponentsInChildren<MeshCollider>(true);if(shapes.Length==0)throw new InvalidOperationException("EasyRoads must generate actual drivable collision meshes.");
            int segment=0;foreach(var shape in shapes){shape.gameObject.layer=0;var surface=shape.GetComponent<LWS.InterstateHauler.LwsRoadSurface>()??shape.gameObject.AddComponent<LWS.InterstateHauler.LwsRoadSurface>();surface.Configure("HH10_DEAD_END_BRANCH",("HH10_BRANCH_"+(segment++)),LWS.InterstateHauler.LwsRoadSurfaceType.AsphaltInterstate,"Dead End service road");}
            owner.enabled=false; // Authored vendor meshes remain; no road generation during ordinary play.
        }
        static Renderer Text(Transform face,string caption,Color color)
        {
            var theme=AssetDatabase.LoadAssetAtPath<LWS.TruckSports.SportsBroadcastTheme>("Assets/TruckSports/Resources/SportsBroadcastTheme.asset");
            if(theme==null||theme.headingFont==null)throw new InvalidOperationException("The existing Sports heading font is required for the authored route clues.");
            var go=new GameObject("Authored route clue",typeof(TMPro.TextMeshPro));go.transform.SetParent(face,false);
            var text=go.GetComponent<TMPro.TextMeshPro>();text.font=theme.headingFont;text.text=caption;text.fontSize=8;
            text.alignment=TMPro.TextAlignmentOptions.Center;text.color=color;text.richText=false;text.rectTransform.sizeDelta=new Vector2(24,7);
            var renderer=text.GetComponent<Renderer>();renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;return renderer;
        }
        static DeadEndPuzzlePath Path(Transform root,string name,Vector3[] positions)=>new DeadEndPuzzlePath{points=positions.Select((point,index)=>Child(root,name+" "+index,point)).ToArray()};
        static Transform Point(Transform root,string name,float x,float z)=>Child(root,name,World(x,z,1));
        static Transform Child(Transform parent,string name,Vector3 position){var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=position;return go.transform;}
        static T Need<T>(string path)where T:UnityEngine.Object{var source=AssetDatabase.LoadAssetAtPath<T>(path);if(source==null)throw new InvalidOperationException("Existing owned road/sign asset missing: "+path);return source;}
    }
}
#endif
