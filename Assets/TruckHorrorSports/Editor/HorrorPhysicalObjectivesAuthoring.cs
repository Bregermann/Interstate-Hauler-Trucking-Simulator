#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
namespace LWS.TruckHorrorSports.Editor
{
    /// <summary>Explicit additive supply/key upgrade. Existing course and objective IDs remain intact.</summary>
    public static class HorrorPhysicalObjectivesAuthoring
    {
        const string RootName="Physical night supplies and harvest keys";
        const string Crate="Assets/LeartesStudios/Garage Warehouse Environment/Art/Prefabs/SM_PK04_WoodenCrate_01.prefab";
        const string Shelf="Assets/LeartesStudios/Garage Warehouse Environment/Art/Prefabs/SM_MetalShelve_01.prefab";
        const string Floor="Assets/LeartesStudios/Garage Warehouse Environment/Art/Prefabs/SM_Floor_4x6m_01.prefab";
        const string Fence="Assets/JC_LP_MegaCity/Prefabs/FloorProps/SM_FloorProps_Fence_01.prefab";
        const string Village="Assets/TruckDragonKingdom/PrivateLicensedEnvironment/Plugins/Tidal Flask Studios/Fantastic Village Pack/prefabs/";
        const string Windmill="Assets/TruckDragonKingdom/PrivateLicensedEnvironment/Plugins/Ventuar3D/MedievalCastlePack/Prefabs/Structures/Houses/WindmillBase.prefab";
        [MenuItem("Truck Sports/Alpha 0.10/Upgrade active physical horror objectives")]
        public static void UpgradeActiveVenue()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before this saved-scene upgrade.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var match=UnityEngine.Object.FindObjectsByType<HorrorMatch>(FindObjectsSortMode.None).SingleOrDefault(m=>m.gameObject.scene==scene);
            if(match==null||(match.sport!=HorrorSport.GraveyardShift&&match.sport!=HorrorSport.ScarecrowSeason)||scene.path!=HorrorAuthoring.Scene(match.sport))throw new InvalidOperationException("Open the saved Graveyard Shift or Scarecrow Season scene.");
            if(scene.GetRootGameObjects().Any(g=>g.name==RootName)){if(match.sport==HorrorSport.GraveyardShift)ConfigureSupplyNavigation(match.supplyJob);Debug.Log("HORROR_PHYSICAL_OBJECTIVES_PRESERVED "+scene.path);EditorSceneManager.MarkSceneDirty(scene);return;}
            string field=match.sport==HorrorSport.GraveyardShift?"supplyJob":"harvestObjects";
            var serialized=new SerializedObject(match);if(serialized.FindProperty(field)==null)throw new InvalidOperationException("Integrate the documented HorrorMatch "+field+" hook before scene authoring.");
            foreach(string path in match.sport==HorrorSport.GraveyardShift?new[]{Crate,Shelf,Floor}:new[]{Fence,Windmill,Village+"buildings_modules/P_BLD_windmill_sail.prefab",Village+"props/P_PROP_hay_01.prefab",Village+"props/P_PROP_hay_05.prefab",Village+"props/P_PROP_food_pumpkin_01.prefab"})Need<GameObject>(path);
            string folder=HorrorAuthoring.Folders[(int)match.sport];
            var root=new GameObject(RootName);root.SetActive(false);
            if(match.sport==HorrorSport.GraveyardShift)Supplies(match,root.transform,folder,serialized);
            else Harvest(match,root.transform,folder,serialized);
            root.SetActive(true);Undo.RegisterCreatedObjectUndo(root,"Add physical horror objectives");EditorUtility.SetDirty(match);EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("HORROR_PHYSICAL_OBJECTIVES_ADDED "+scene.name+"; caller must save after inspection. This changes objective0 transport or a secondary key-locked route and requires targeted gameplay validation.");
        }
        static void Supplies(HorrorMatch match,Transform root,string folder,SerializedObject serialized)
        {
            if(match.footAgents==null||match.footAgents.Length<4||match.footAgents.Any(f=>f==null))throw new InvalidOperationException("Reuse all four existing participant foot rigs.");
            var job=root.gameObject.AddComponent<HorrorSupplyJob>();job.match=match;job.objectiveIndex=0;job.carriers=match.footAgents;
            job.parcels=new Rigidbody[4];job.carryingAnchors=new Rigidbody[4];job.receivers=new BoxCollider[4];
            Vector3 origin=match.objectives[0].transform.position;job.truckApproach=origin+Vector3.back*6;
            // Replace only the original prop-only delivery stack with the actual parcels.
            foreach(var existing in match.gameObject.scene.GetRootGameObjects())
                if(existing.name=="Undelivered supplies"&&Vector3.Distance(existing.transform.position,origin+new Vector3(0,-1,5))<.1f)Undo.DestroyObjectImmediate(existing);
            for(int slot=0;slot<4;slot++)
            {
                float x=(slot-1.5f)*2.5f;
                var parcel=Child(root,"Participant "+(slot+1)+" physical night supplies",origin+new Vector3(x,-.97f,-1));
                Visual(parcel,Crate,new Vector3(.6f,.6f,.6f),folder);
                var shape=parcel.gameObject.AddComponent<BoxCollider>();shape.center=Vector3.up*.3f;shape.size=Vector3.one*.6f;
                var body=parcel.gameObject.AddComponent<Rigidbody>();body.mass=8;body.isKinematic=true;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;job.parcels[slot]=body;
                var anchor=Child(root,"Participant "+(slot+1)+" physical carrying anchor",parcel.position+Vector3.up*.3f).gameObject.AddComponent<Rigidbody>();anchor.isKinematic=true;anchor.useGravity=false;job.carryingAnchors[slot]=anchor;
                var receiver=Child(root,"Participant "+(slot+1)+" restocking shelf",origin+new Vector3(x,-1,8));
                var shelf=Child(receiver,"Owned supply shelving",receiver.position+Vector3.forward*.65f);Visual(shelf,Shelf,new Vector3(1.8f,1.6f,.7f),folder);
                var tray=Child(receiver,"Visible supply receiving tray",receiver.position+Vector3.up*.94f);Visual(tray,Floor,new Vector3(1.5f,.12f,1.5f),folder,true);
                var support=receiver.gameObject.AddComponent<BoxCollider>();support.center=new Vector3(0,1,0);support.size=new Vector3(1.5f,.12f,1.5f);job.receivers[slot]=support;
            }
            // The original task0 remains first and commits through the original scoring owner.
            match.objectives[0].requiresFoot=true;match.objectives[0].truckApproach=job.truckApproach;
            match.objectives[0].caption="Collect and restock the night delivery";EditorUtility.SetDirty(match.objectives[0]);
            ConfigureSupplyNavigation(job);
            serialized.FindProperty("supplyJob").objectReferenceValue=job;serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        static void ConfigureSupplyNavigation(HorrorSupplyJob job)
        {
            if(job==null)throw new InvalidOperationException("The saved physical supply job is required.");
            foreach(var receiver in job.receivers)
            {
                // These trays were added after the original venue bake. Their real
                // overhead obstruction must also carve the installed navigation data.
                var obstacle=receiver.GetComponent<NavMeshObstacle>();
                if(obstacle==null)obstacle=receiver.gameObject.AddComponent<NavMeshObstacle>();
                obstacle.shape=NavMeshObstacleShape.Box;obstacle.center=new Vector3(0,.7f,.1f);obstacle.size=new Vector3(receiver.size.x,1.4f,1.75f);
                obstacle.carving=true;obstacle.carveOnlyStationary=true;EditorUtility.SetDirty(obstacle);
            }
        }
        static void Harvest(HorrorMatch match,Transform root,string folder,SerializedObject serialized)
        {
            var objects=root.gameObject.AddComponent<HorrorHarvestObjects>();objects.match=match;objects.keyObjective=2;
            // This closes one interior corridor only. The outer maze remains traversable,
            // and the original barn-key receipt unlocks the physical shortcut to the final ward.
            var hinge=Child(root,"Barn-key locked shortcut hinge",new Vector3(-82,0,88));objects.gateHinge=hinge;
            foreach(float x in new[]{5f,15f}){var leaf=Child(hinge,"Owned harvest gate section",hinge.position+Vector3.right*x);Visual(leaf,Fence,new Vector3(10,3,.5f),folder,true);}
            var shape=hinge.gameObject.AddComponent<BoxCollider>();shape.center=new Vector3(10,1.5f,0);shape.size=new Vector3(20,3,.5f);
            var obstacle=hinge.gameObject.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.center=shape.center;obstacle.size=shape.size;obstacle.carving=true;obstacle.carveOnlyStationary=false;objects.routeObstacle=obstacle;
            var effigy=UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).FirstOrDefault(m=>m.gameObject.scene==match.gameObject.scene&&m.name=="Handmade harvest ritual effigy");
            if(effigy==null)throw new InvalidOperationException("Reuse the scene's existing authored ritual effigy art.");
            objects.lockEffigy=Effigy(hinge,"Barn key lock effigy",effigy,new Vector3(19,1.6f,-.4f),.2f);
            objects.carriedEffigies=new GameObject[match.trucks.Length];
            for(int i=0;i<objects.carriedEffigies.Length;i++){objects.carriedEffigies[i]=Effigy(match.trucks[i].transform,"Participant "+(i+1)+" visibly carried ritual relic",effigy,new Vector3(0,2.4f,-.5f),.15f);objects.carriedEffigies[i].SetActive(false);}
            var windmill=Child(root,"Owned farm windmill",new Vector3(-185,0,95));Visual(windmill,Windmill,new Vector3(28,48,28),folder);
            var sail=Child(root,"Owned windmill sails",new Vector3(-185,30,80));Visual(sail,Village+"buildings_modules/P_BLD_windmill_sail.prefab",new Vector3(36,36,3),folder);
            for(int i=0;i<8;i++){var hay=Child(root,"Owned harvest hay bale "+i,new Vector3(175+(i%2)*10,0,35+(i/2)*17));hay.rotation=Quaternion.Euler(0,i*37,0);Visual(hay,Village+"props/P_PROP_hay_"+(i%2==0?"01":"05")+".prefab",new Vector3(8,6,8),folder);}
            for(int i=0;i<18;i++){var pumpkin=Child(root,"Owned harvest pumpkin "+i,new Vector3(-185+(i%6)*5,0,-40+(i/6)*7));pumpkin.rotation=Quaternion.Euler(0,i*59,0);Visual(pumpkin,Village+"props/P_PROP_food_pumpkin_01.prefab",Vector3.one*(1.2f+(i%3)*.35f),folder);}
            serialized.FindProperty("harvestObjects").objectReferenceValue=objects;serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        static GameObject Effigy(Transform parent,string name,MeshFilter source,Vector3 local,float scale)
        {
            var result=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));result.transform.SetParent(parent,false);result.transform.localPosition=local;result.transform.localScale=Vector3.one*scale;
            result.GetComponent<MeshFilter>().sharedMesh=source.sharedMesh;result.GetComponent<MeshRenderer>().sharedMaterial=source.GetComponent<Renderer>().sharedMaterial;return result;
        }
        static Transform Child(Transform parent,string name,Vector3 position){var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=position;return go.transform;}
        static void Visual(Transform root,string source,Vector3 size,string folder,bool stretch=false)
        {LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(root,source,size,0,stretch,folder);}
        static T Need<T>(string path)where T:UnityEngine.Object{var source=AssetDatabase.LoadAssetAtPath<T>(path);if(source==null)throw new InvalidOperationException("Existing owned physical-objective asset missing: "+path);return source;}
    }
}
#endif
