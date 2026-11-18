#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace LWS.TruckExtremeHauling.Editor
{
    /// <summary>Explicit additive alternative route; never rebuilds the saved Sprint scene.</summary>
    public static class SkyhookTransferAuthoring
    {
        const string RootName="Authored physical two-station cable transfer";
        const string Folder="Assets/TruckExtremeHauling";
        const string Reel="Assets/ConstructionPropsBundle/URP/Prefabs/SM_CableReel01 Variant.prefab";
        const string Floor="Assets/LeartesStudios/Garage Warehouse Environment/Art/Prefabs/SM_Floor_4x6m_01.prefab";
        const string Crane="Assets/TruckBungeeDemolition/Models/Lattice Crane.asset";
        [MenuItem("Truck Sports/Alpha 0.10/Upgrade active Skyhook physical transfer route")]
        public static void UpgradeActiveVenue()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!=Folder+"/Scenes/SkyhookSprint.unity")throw new InvalidOperationException("Open the saved Skyhook Sprint scene.");
            if(scene.GetRootGameObjects().Any(g=>g.name==RootName)){Debug.Log("SKYHOOK_TRANSFER_PRESERVED");return;}
            var match=UnityEngine.Object.FindObjectsByType<SkyhookSprintMatch>(FindObjectsSortMode.None).Single(m=>m.gameObject.scene==scene);
            Need<GameObject>(Reel);Need<GameObject>(Floor);var crane=Need<Mesh>(Crane);
            var steel=Need<Material>(Folder+"/Materials/Freight tower gunmetal.mat");var paint=Need<Material>(Folder+"/Materials/Freight safety orange.mat");var contact=Need<PhysicsMaterial>(Folder+"/Materials/Freight concrete.asset");
            if(!AssetDatabase.IsValidFolder(Folder+"/Models"))AssetDatabase.CreateFolder(Folder,"Models");
            var root=new GameObject(RootName);match.transferRoutes=new SkyhookTransferRoute[match.cableCars.Length];
            Mesh hookMesh=HookMesh();Mesh eyeMesh=EyeMesh();
            for(int i=0;i<match.cableCars.Length;i++)
            {
                var lane=Child(root.transform,"Physical precision transfer lane "+(i+1),Vector3.zero);var route=lane.gameObject.AddComponent<SkyhookTransferRoute>();match.transferRoutes[i]=route;
                route.first=match.cableCars[i];route.cargo=match.trailers[i];route.eyeLocal=match.cableCars[i].cable.anchor;route.receivingTarget=match.objectives[i];
                float x=route.cargo.Body.position.x;
                // The same native load starts behind the parked tractor, never overlapping it.
                Vector3 spawn=route.cargo.Body.position+Vector3.back*30;
                float bottom=float.PositiveInfinity;foreach(var shape in route.cargo.GetComponentsInChildren<Collider>(true))if(shape.enabled&&!shape.isTrigger)bottom=Mathf.Min(bottom,shape.bounds.min.y);
                spawn.y+=.04f-bottom;
                route.loadingPose=Child(lane,"Authored supported loading pose",spawn);route.loadingPose.rotation=route.cargo.transform.rotation;
                route.loadingPad=ExistingPad(scene,new Vector3(x,0,spawn.z-7));
                if(route.loadingPad==null)route.loadingPad=Pad(lane,"Visible alternate loading station",new Vector3(x,-1,spawn.z-5),new Vector3(44,2,50),contact);
                route.transferPad=Pad(lane,"Visible physical transfer and recovery station",new Vector3(x,-1,70),new Vector3(44,2,44),contact);
                route.receivingPad=ExistingPad(scene,new Vector3(match.objectives[i].position.x,0,match.objectives[i].position.z));
                if(route.receivingPad==null)route.receivingPad=Pad(lane,"Visible final receiving station",new Vector3(x,-1,match.objectives[i].position.z),new Vector3(44,2,50),contact);
                Vector3 eye=spawn+route.loadingPose.rotation*route.eyeLocal;
                route.liftingEyeVisual=Child(lane,"Visible original-load lifting eye",eye);route.liftingEyeVisual.gameObject.AddComponent<MeshFilter>().sharedMesh=eyeMesh;route.liftingEyeVisual.gameObject.AddComponent<MeshRenderer>().sharedMaterial=paint;
                route.slings=new LineRenderer[4];for(int sling=0;sling<4;sling++){var wire=Child(lane,"Actual load rigging sling "+sling,Vector3.zero).gameObject.AddComponent<LineRenderer>();wire.sharedMaterial=steel;wire.widthMultiplier=.07f;route.slings[sling]=wire;}
                route.firstTrolleyPose=Child(lane,"Authored first trolley start",new Vector3(x,22,eye.z));
                route.secondTrolleyPose=Child(lane,"Authored second trolley start",new Vector3(x+4,24,70));
                var secondRoot=Child(lane,"Second physical cable carrier",Vector3.zero);route.second=secondRoot.gameObject.AddComponent<PhysicalCableCar>();route.second.load=route.cargo.Body;
                route.second.trolley=Trolley(lane,route.secondTrolleyPose.position,new Vector3(x+4,24,match.objectives[i].position.z),steel);route.second.routeEnd=new Vector3(x+4,24,match.objectives[i].position.z);
                route.firstHook=Hook(lane,route.first,eye+Vector3.up*.05f,route.firstTrolleyPose.position,hookMesh,paint,steel);
                route.secondHook=Hook(lane,route.second,route.secondTrolleyPose.position+Vector3.down*4,route.secondTrolleyPose.position,hookMesh,paint,steel);
                foreach(var ends in new[]{new[]{route.firstTrolleyPose.position,new Vector3(x,22,70)},new[]{route.secondTrolleyPose.position,new Vector3(x+4,24,match.objectives[i].position.z)}})
                {var rail=Child(lane,"Visible physical trolley route",Vector3.zero).gameObject.AddComponent<LineRenderer>();rail.sharedMaterial=steel;rail.widthMultiplier=.28f;rail.positionCount=2;rail.SetPosition(0,ends[0]);rail.SetPosition(1,ends[1]);}
                foreach(var point in new[]{route.firstTrolleyPose.position,new Vector3(x,22,70),route.secondTrolleyPose.position,new Vector3(x+4,24,match.objectives[i].position.z)})
                    CraneModel(lane,point,crane,steel);
                foreach(var hook in new[]{route.firstHook,route.secondHook})
                {var model=Child(lane,"Owned physical winch equipment",hook==route.firstHook?route.firstTrolleyPose.position:route.secondTrolleyPose.position);LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(model,Reel,new Vector3(2.6f,1.5f,2.6f),0,false,"TruckExtremeHauling");hook.winchVisual=model;}
                lane.gameObject.SetActive(false); // Only the explicit alternate mode activates its geometry.
            }
            EditorUtility.SetDirty(match);EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("SKYHOOK_PHYSICAL_TRANSFER_AUTHORED option1 only; same native load, two real winch hooks, supported intermediate transfer and original final targets. Default Sprint geometry unchanged; caller saves and runs focused acceptance.");
        }
        static Collider ExistingPad(UnityEngine.SceneManagement.Scene scene,Vector3 point)
        {
            return UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None).FirstOrDefault(c=>c.gameObject.scene==scene&&c.enabled&&!c.isTrigger&&c.attachedRigidbody==null&&c.bounds.min.x<point.x-3&&c.bounds.max.x>point.x+3&&c.bounds.min.z<point.z-3&&c.bounds.max.z>point.z+3&&Mathf.Abs(c.bounds.max.y)<.15f);
        }
        static Collider Pad(Transform root,string name,Vector3 position,Vector3 size,PhysicsMaterial contact)
        {
            var pad=Child(root,name,position);var shape=pad.gameObject.AddComponent<BoxCollider>();shape.size=size;shape.sharedMaterial=contact;
            var model=Child(pad,"Owned structural receiving floor",position-Vector3.up);
            LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(model,Floor,size,0,true,"TruckExtremeHauling");return shape;
        }
        static Rigidbody Trolley(Transform root,Vector3 position,Vector3 end,Material steel)
        {
            var carrier=Child(root,"Physical second trolley",position);var body=carrier.gameObject.AddComponent<Rigidbody>();body.mass=1000;body.useGravity=false;body.isKinematic=true;body.solverIterations=20;body.solverVelocityIterations=8;
            var rail=carrier.gameObject.AddComponent<ConfigurableJoint>();rail.autoConfigureConnectedAnchor=false;rail.connectedAnchor=position;rail.axis=(end-position).normalized;rail.secondaryAxis=Vector3.up;rail.xMotion=ConfigurableJointMotion.Limited;rail.yMotion=rail.zMotion=ConfigurableJointMotion.Locked;rail.linearLimit=new SoftJointLimit{limit=300};rail.angularXMotion=rail.angularYMotion=rail.angularZMotion=ConfigurableJointMotion.Locked;
            return body;
        }
        static SuspendedFreightHook Hook(Transform parent,PhysicalCableCar carrier,Vector3 position,Vector3 trolleyPose,Mesh mesh,Material paint,Material ropeMaterial)
        {
            var root=Child(parent,"Physical suspended freight hook",position);root.gameObject.layer=2;
            var body=root.gameObject.AddComponent<Rigidbody>();body.mass=400;body.isKinematic=true;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.solverIterations=20;body.solverVelocityIterations=8;
            var shape=root.gameObject.AddComponent<SphereCollider>();shape.radius=.25f;
            root.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;root.gameObject.AddComponent<MeshRenderer>().sharedMaterial=paint;
            var cable=root.gameObject.AddComponent<SpringJoint>();cable.autoConfigureConnectedAnchor=false;cable.connectedBody=carrier.trolley;cable.connectedAnchor=Vector3.zero;cable.spring=160000;cable.damper=35000;cable.maxDistance=Vector3.Distance(position,trolleyPose);cable.enableCollision=false;
            var hook=root.gameObject.AddComponent<SuspendedFreightHook>();hook.body=body;hook.rope=cable;hook.carrier=carrier;hook.initialLength=cable.maxDistance;hook.resetPose=Child(parent,"Saved hook pose",position);
            hook.line=root.gameObject.AddComponent<LineRenderer>();hook.line.sharedMaterial=ropeMaterial;hook.line.widthMultiplier=.12f;return hook;
        }
        static void CraneModel(Transform root,Vector3 trolley,Mesh mesh,Material material)
        {
            var holder=Child(root,"Owned freight lattice support",new Vector3(trolley.x+18,0,trolley.z));
            var model=new GameObject("Existing demolition lattice crane",typeof(MeshFilter),typeof(MeshRenderer));model.transform.SetParent(holder,false);model.GetComponent<MeshFilter>().sharedMesh=mesh;model.GetComponent<MeshRenderer>().sharedMaterial=material;
            Bounds b=mesh.bounds;float factor=trolley.y/Mathf.Max(.1f,b.size.y);model.transform.localScale=Vector3.one*factor;model.transform.localPosition=-new Vector3(b.center.x,b.min.y,b.center.z)*factor;
        }
        static Mesh EyeMesh()
        {
            const string path=Folder+"/Models/Physical freight lifting eye.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing!=null)return existing;
            const int arcs=20,sides=6;var vertices=new Vector3[(arcs+1)*sides];var triangles=new int[arcs*sides*6];
            for(int a=0;a<=arcs;a++)for(int side=0;side<sides;side++){float angle=a*Mathf.PI*2/arcs,around=side*Mathf.PI*2/sides;Vector3 radial=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);vertices[a*sides+side]=radial*.28f+radial*Mathf.Cos(around)*.055f+Vector3.forward*Mathf.Sin(around)*.055f;}
            int n=0;for(int a=0;a<arcs;a++)for(int side=0;side<sides;side++){int u=a*sides+side,v=a*sides+(side+1)%sides,w=u+sides,x=v+sides;triangles[n++]=u;triangles[n++]=w;triangles[n++]=v;triangles[n++]=v;triangles[n++]=w;triangles[n++]=x;}
            var mesh=new Mesh{name="Original freight lifting eye"};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static Mesh HookMesh()
        {
            const string path=Folder+"/Models/Physical freight hook.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing!=null)return existing;
            const int arcs=18,sides=6;var vertices=new Vector3[(arcs+1)*sides];var triangles=new int[arcs*sides*6];
            for(int a=0;a<=arcs;a++)for(int s=0;s<sides;s++){float angle=(-120+a*280f/arcs)*Mathf.Deg2Rad,around=s*Mathf.PI*2/sides;Vector3 radial=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);vertices[a*sides+s]=radial*.37f+radial*Mathf.Cos(around)*.09f+Vector3.forward*Mathf.Sin(around)*.09f;}
            int n=0;for(int a=0;a<arcs;a++)for(int s=0;s<sides;s++){int u=a*sides+s,v=a*sides+(s+1)%sides,w=u+sides,x=v+sides;triangles[n++]=u;triangles[n++]=w;triangles[n++]=v;triangles[n++]=v;triangles[n++]=w;triangles[n++]=x;}
            var mesh=new Mesh{name="Original extruded freight lifting hook"};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static Transform Child(Transform root,string name,Vector3 position){var g=new GameObject(name);g.transform.SetParent(root,false);g.transform.position=position;return g.transform;}
        static T Need<T>(string path)where T:UnityEngine.Object{var value=AssetDatabase.LoadAssetAtPath<T>(path);if(value==null)throw new InvalidOperationException("Required existing cable venue asset missing: "+path);return value;}
    }
}
#endif
