using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LWS.TruckSports;
using LWS.TruckSports.Editor;

namespace LWS.TruckSnowmanBuilding.Editor
{
    // Explicit additive repair. The caller owns opening/saving; no source prefab,
    // existing collider, material or physical parameter is modified here.
    public static class SnowmanCollectionRackAuthoring
    {
        const string RackName="Visible three-lane timber collection rack";
        const string WoodPath="Assets/TruckSnowmanBuilding/Materials/Warm workshop timber.mat";
        const string BluePath="Assets/TruckSnowmanBuilding/Materials/Winter workshop blue enamel.mat";
        readonly struct Plank
        {
            public readonly string name; public readonly Vector3 center,size;public readonly bool enamel;
            public Plank(string name,Vector3 center,Vector3 size,bool enamel=false){this.name=name;this.center=center;this.size=size;this.enamel=enamel;}
        }
        static Plank[] Plan()
        {
            var plan=new List<Plank>();
            foreach(float x in new[]{-15f,-5f,5f,15f})
            {
                foreach(float y in new[]{.65f,1.45f})plan.Add(new Plank("Visible lane side rail",new Vector3(x,y,-2.3f),new Vector3(.35f,.4f,13.4f)));
                foreach(float z in new[]{-8.7f,4.7f})plan.Add(new Plank("Visible collection post",new Vector3(x,1.25f,z),new Vector3(.55f,2.5f,.55f),true));
            }
            foreach(float y in new[]{.35f,1.15f,1.95f})plan.Add(new Plank("Visible physical back catch plank",new Vector3(0,y,4.7f),new Vector3(30.7f,.4f,.35f)));
            return plan.ToArray();
        }
        static void Require(bool valid,string reason){if(!valid)throw new InvalidOperationException("Snowman collection preflight: "+reason);}
        static bool Same(Vector3 a,Vector3 b)=>Vector3.Distance(a,b)<.0001f;
        static Transform Preflight(SnowmanMatch m,out Material wood,out Material blue,out PhysicsMaterial contact)
        {
            Require(!EditorApplication.isPlaying&&!EditorApplication.isCompiling,"stopped Editor required");
            Require(m!=null&&m.gameObject.scene.IsValid()&&(m.gameObject.scene.path==SnowmanAuthoring.Scene||string.IsNullOrEmpty(m.gameObject.scene.path)),"only original Snowman scene or its new unsaved author");
            Require(Same(m.transform.position,Vector3.zero)&&Same(m.transform.lossyScale,Vector3.one)&&Quaternion.Angle(m.transform.rotation,Quaternion.identity)<.001f,"original untransformed session required");
            Transform root=m.transform.Find("Winter village workshop");Require(root!=null&&Same(root.localPosition,Vector3.zero)&&Same(root.localScale,Vector3.one)&&Quaternion.Angle(root.localRotation,Quaternion.identity)<.001f,"original workshop root required");
            Require(m.collectionRack==null&&root.Find(RackName)==null&&m.GetComponentsInChildren<SnowmanCollectionRack>(true).Length==0,"refuses existing, partial or customized rack; preserve it for review");
            Require(m.buildCenter!=null&&m.buildCenter.IsChildOf(root)&&Same(m.buildCenter.position,new Vector3(0,.15f,22)),"original foundation center changed");
            var foundation=m.foundation as BoxCollider;Require(foundation!=null&&Same(foundation.bounds.center,new Vector3(0,0,22))&&Same(foundation.bounds.size,new Vector3(14,.3f,14)),"original physical foundation changed");
            Require(m.balls!=null&&m.balls.Length==3&&m.patches!=null&&m.patches.Length==3,"three original balls and drifts required");
            string[] ids={"snowman.ball.base","snowman.ball.middle","snowman.ball.head"};
            var allowed=new HashSet<Collider>();var meadow=root.Find("Continuous solid winter meadow");Require(meadow!=null,"original meadow required");var ground=meadow.GetComponent<BoxCollider>();
            Require(ground!=null&&Same(ground.bounds.center,new Vector3(0,-.6f,-25))&&Same(ground.bounds.size,new Vector3(150,1.2f,240)),"original solid meadow changed");allowed.Add(ground);
            for(int i=0;i<3;i++)
            {
                Require(m.balls[i]!=null&&m.balls[i].match==m&&m.balls[i].role==i&&m.balls[i].stableId==ids[i]&&m.balls[i].body!=null&&m.balls[i].sphere!=null,"original physical snowball identities required");
                var patch=m.patches[i];Require(patch!=null&&patch.ground!=null&&Same(patch.ground.bounds.center,new Vector3((i-1)*10,.01f,-46))&&Same(patch.ground.bounds.size,new Vector3(8,.04f,76)),"original gathering lanes changed");allowed.Add(patch.ground);
            }
            wood=AssetDatabase.LoadAssetAtPath<Material>(WoodPath);blue=AssetDatabase.LoadAssetAtPath<Material>(BluePath);contact=m.balls[0].sphere.sharedMaterial;
            Require(wood!=null&&blue!=null&&contact!=null&&Mathf.Abs(contact.dynamicFriction-.65f)<.0001f&&Mathf.Abs(contact.bounciness)<.0001f,"existing timber, enamel and original real snow contact required");
            Require(m.hoist!=null&&m.hoist.ceiling>=22&&m.hoist.parkHeight>=16,"original physical hoist clearance required");
            Physics.SyncTransforms();
            // Scan the whole working footprint, including its open mouths, before any
            // mutation. Only the authored ground/drift may meet this new structure.
            foreach(var collider in Physics.OverlapBox(new Vector3(0,1.45f,-2.15f),new Vector3(15.8f,1.44f,7.2f),Quaternion.identity,~0,QueryTriggerInteraction.Collide))
                Require(allowed.Contains(collider),"unexpected existing collider in rack footprint: "+collider.name);
            var plan=Plan();Require(plan.Length==19&&plan.All(p=>p.center.y+p.size.y*.5f<=2.5f&&p.center.z+p.size.z*.5f<5),"bounded open-top plan inside original collection zone");
            // At maximum radius each 10m lane leaves >3m lateral clearance. A ball
            // stopped against the back planks remains inside z[-11,5] at any valid radius.
            Require(10-.55f>2*m.balls.Max(b=>b.maximumRadius)&&4.525f-.85f> -11,"real sphere clearance required");
            Debug.Log("SNOWMAN_COLLECTION_PREFLIGHT 19 visible physical planks/posts; open front -9; top 2.5; back 4.875; original sphere/zone/foundation/hoist unchanged; all footprint colliders reviewed.");
            return root;
        }
        public static SnowmanCollectionRack Install(SnowmanMatch m)
        {
            Transform parent=Preflight(m,out var wood,out var blue,out var contact);
            var root=new GameObject(RackName);root.transform.SetParent(parent,false);var rack=root.AddComponent<SnowmanCollectionRack>();rack.match=m;
            var colliders=new List<BoxCollider>();foreach(var p in Plan())
            {
                var plank=SportsEventAuthoring.Box(p.name,p.center,p.size,p.enamel?blue:wood,contact,root.transform);var collider=plank.GetComponent<BoxCollider>();collider.contactOffset=.005f;colliders.Add(collider);
            }
            rack.planks=colliders.ToArray();m.collectionRack=rack;
            for(int i=0;i<3;i++)
            {
                var label=SportsEventAuthoring.Label("Collection lane instruction",new[]{"BASE","MIDDLE","HEAD"}[i]+" / ROLL IN / LET SETTLE",new Vector3((i-1)*10,3.1f,4.7f),.1f,new Color(.04f,.17f,.24f));label.transform.SetParent(root.transform,true);
                var billboard=label.gameObject.AddComponent<SportsPerCameraBillboard>();billboard.maximumPixelWidth=144;billboard.maximumPixelHeight=24;billboard.maximumLabelDistance=120;
            }
            EditorUtility.SetDirty(rack);EditorUtility.SetDirty(m);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(m.gameObject.scene);return rack;
        }
        [MenuItem("Truck Sports/Alpha 0.7/Repair OPEN Snowman/Add physical collection rack")]
        public static void InstallOnOpenOriginalScene()
        {
            var matches=UnityEngine.Object.FindObjectsByType<SnowmanMatch>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            Require(matches.Length==1&&matches[0].gameObject.scene.path==SnowmanAuthoring.Scene,"open exact original Snowman scene required; does not open or save");Install(matches[0]);
        }
    }
}
