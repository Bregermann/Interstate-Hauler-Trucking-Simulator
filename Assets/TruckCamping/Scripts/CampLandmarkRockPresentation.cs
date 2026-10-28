using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckCamping
{
    // One render-only application per world. Original collider objects remain
    // active; no live geometry/vehicle/terrain writes and no update loop.
    public sealed class CampLandmarkRockPresentation : MonoBehaviour
    {
        public bool Applied {get;private set;}
        public string Diagnostic {get;private set;}="Optional landmark profile not applied.";
        public int ClimateDirtyCalls {get;private set;}
        public Transform VisualRoot=>visualRoot==null?null:visualRoot.transform;
        public int VisualCount=>visualRoot==null?0:visualRoot.GetComponentsInChildren<MeshRenderer>(true).Length;
        CampingWilderness owner;CampLandmarkRockProfile profile;GameObject visualRoot;
        readonly List<MeshRenderer> originals=new List<MeshRenderer>();
        public static void TryInstall(CampingWilderness world)
        {
            var settings=Resources.Load<CampLandmarkRockProfile>(CampLandmarkRockProfile.ResourceName);if(settings==null)return;
            var presentation=world.GetComponent<CampLandmarkRockPresentation>();if(presentation==null)presentation=world.gameObject.AddComponent<CampLandmarkRockPresentation>();presentation.Apply(world,settings);
        }
        public static Transform Resolve(Transform root,int[] path)
        {foreach(int sibling in path){if(root==null||sibling<0||sibling>=root.childCount)return null;root=root.GetChild(sibling);}return root;}
        public static Bounds TransformBounds(Bounds source,Quaternion rotation,float scale,Vector3 position)
        {
            Bounds result=default;
            for(int i=0;i<8;i++){Vector3 p=position+rotation*(source.center+Vector3.Scale(source.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)))*scale;if(i==0)result=new Bounds(p,Vector3.zero);else result.Encapsulate(p);}return result;
        }
        public static Bounds UnitBounds(CampLandmarkRockProfile settings)
        {
            var child=settings.boulderPrefab.GetComponentInChildren<MeshFilter>(true).transform;var matrix=Matrix4x4.TRS(child.localPosition,child.localRotation,child.localScale);var source=settings.originalRockAMesh.bounds;Bounds result=default;
            for(int i=0;i<8;i++){var p=matrix.MultiplyPoint3x4(source.center+Vector3.Scale(source.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));if(i==0)result=new Bounds(p,Vector3.zero);else result.Encapsulate(p);}return result;
        }
        public static Bounds LandmarkRenderEnvelope(CampingWilderness world,int landmark)
        {
            var original=world.landmarks[landmark].GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="Rock_Cluster_1A").ToArray();var bounds=LocalBounds(world,original[0].bounds);foreach(var renderer in original.Skip(1))bounds.Encapsulate(LocalBounds(world,renderer.bounds));
            // Art may embed in the existing terrain, never move the terrain.
            float terrainBottom=world.terrains.Min(t=>t.transform.position.y)-world.transform.position.y;bounds.SetMinMax(new Vector3(bounds.min.x,terrainBottom,bounds.min.z),bounds.max);return bounds;
        }
        static bool Near(Vector3 a,Vector3 b)=> (a-b).sqrMagnitude<1e-8f;
        static Bounds LocalBounds(CampingWilderness world,Bounds bounds)=>new Bounds(world.transform.InverseTransformPoint(bounds.center),bounds.size);
        static bool Inside(Bounds outer,Bounds inner)=>inner.min.x>=outer.min.x-.001f&&inner.min.y>=outer.min.y-.001f&&inner.min.z>=outer.min.z-.001f&&inner.max.x<=outer.max.x+.001f&&inner.max.y<=outer.max.y+.001f&&inner.max.z<=outer.max.z+.001f;
        static string BoundsRange(Bounds b)=>"min="+b.min.ToString("R")+" max="+b.max.ToString("R");
        static string EnvelopeViolations(Bounds source,Bounds envelope,float top)
        {
            var errors=new List<string>();
            if(Mathf.Abs(envelope.min.x-source.min.x)>.001f)errors.Add("minX delta="+(envelope.min.x-source.min.x).ToString("R"));if(Mathf.Abs(envelope.max.x-source.max.x)>.001f)errors.Add("maxX delta="+(envelope.max.x-source.max.x).ToString("R"));if(Mathf.Abs(envelope.min.z-source.min.z)>.001f)errors.Add("minZ delta="+(envelope.min.z-source.min.z).ToString("R"));if(Mathf.Abs(envelope.max.z-source.max.z)>.001f)errors.Add("maxZ delta="+(envelope.max.z-source.max.z).ToString("R"));if(envelope.max.y>top+.001f)errors.Add("maxY deltaAboveAllowedTop="+(envelope.max.y-top).ToString("R"));return string.Join(",",errors);
        }
        static string InsideViolations(Bounds allowed,Bounds actual)
        {
            var errors=new List<string>();for(int axis=0;axis<3;axis++){if(actual.min[axis]<allowed.min[axis]-.001f)errors.Add("min"+"XYZ"[axis]+" below="+(allowed.min[axis]-actual.min[axis]).ToString("R"));if(actual.max[axis]>allowed.max[axis]+.001f)errors.Add("max"+"XYZ"[axis]+" above="+(actual.max[axis]-allowed.max[axis]).ToString("R"));}return string.Join(",",errors);
        }
        public bool Validate(CampingWilderness world,CampLandmarkRockProfile settings,out string reason)
        {
            reason="";
            if(world==null||world.landmarks==null||world.landmarks.Length<=6||world.landmarks[2]==null||world.landmarks[6]==null||settings==null||settings.boulderPrefab==null||settings.originalRockAMesh==null||settings.ownedMaterial==null||settings.targets==null||settings.targets.Length!=5){reason="Incomplete optional owned profile.";return false;}
            if(world.transform.lossyScale!=Vector3.one||Quaternion.Angle(world.transform.rotation,Quaternion.identity)>.001f){reason="Customized world rotation/scale preserved.";return false;}
            var prefab=settings.boulderPrefab;var renders=prefab.GetComponentsInChildren<MeshRenderer>(true);var filters=prefab.GetComponentsInChildren<MeshFilter>(true);
            if(!Near(prefab.transform.localPosition,Vector3.zero)||!Near(prefab.transform.localScale,Vector3.one)||Quaternion.Angle(prefab.transform.localRotation,Quaternion.identity)>.001f){reason="Customized variant root frame preserved.";return false;}
            if(prefab.GetComponentsInChildren<Component>(true).Any(c=>!(c is Transform||c is MeshRenderer||c is MeshFilter))||renders.Length!=1||filters.Length!=1||filters[0].sharedMesh!=settings.originalRockAMesh||renders[0].sharedMaterial!=settings.ownedMaterial){reason="Unknown/custom boulder variant preserved.";return false;}
            if(renders[0].transform!=filters[0].transform||!renders[0].enabled||filters[0].transform.parent!=prefab.transform||!Near(UnitBounds(settings).center,Vector3.zero)){reason="Customized/unnormalized authored variant frame preserved.";return false;}
            if(settings.ownedMaterial.shader==null||settings.ownedMaterial.shader.name!="Universal Render Pipeline/Lit"||settings.ownedMaterial.GetFloat("_Surface")!=0){reason="Owned opaque URP rock material required.";return false;}
            Bounds unit=UnitBounds(settings);var seen=new HashSet<Transform>();
            foreach(var target in settings.targets)
            {
                if(target==null||target.siblingPath==null||target.sourceMaterials==null){reason="Incomplete target fingerprint preserved.";return false;}
                var t=Resolve(world.transform,target.siblingPath);if(t==null||!seen.Add(t)||target.landmark!=2&&target.landmark!=6||t.parent!=world.landmarks[target.landmark].transform){reason="Unknown four landmark targets.";return false;}
                var renderer=t.GetComponent<MeshRenderer>();var filter=t.GetComponent<MeshFilter>();var box=t.GetComponent<BoxCollider>();
                if(renderer==null||filter==null||box==null||box.attachedRigidbody!=null||t.name!="Rock_Cluster_1A"||!renderer.enabled||filter.sharedMesh!=target.sourceMesh||!renderer.sharedMaterials.SequenceEqual(target.sourceMaterials)||!Near(t.localPosition,target.sourcePosition)||!Near(t.localScale,target.sourceScale)||Mathf.Abs(Quaternion.Dot(t.localRotation,target.sourceRotation))<.999999f||!Near(box.center,target.colliderCenter)||!Near(box.size,target.colliderSize)){reason="Customized original rock binding/pose/collider preserved.";return false;}
                var current=LocalBounds(world,box.bounds);if(!Near(current.center,target.sourceBounds.center)||!Near(current.size,target.sourceBounds.size)){reason="Changed original physical envelope preserved.";return false;}
                var envelope=target.allowedEnvelope;
                var region=LandmarkRenderEnvelope(world,target.landmark);float allowedTop=region.max.y;string mismatch=InsideViolations(region,envelope);string context="landmark="+target.landmark+" sibling="+string.Join("/",target.siblingPath)+" original="+t.name+" sourceMesh="+filter.sharedMesh.name;
                if(mismatch.Length>0||target.boulders==null||target.boulders.Length<3||target.boulders.Length>5){reason="Designer placement outside bounded original footprint/top: "+context+" axes=["+mismatch+"] sourceColliderLocal{"+BoundsRange(current)+"} sourceRendererWorld{"+BoundsRange(renderer.bounds)+"} envelopeLocal{"+BoundsRange(envelope)+"} allowedTop="+allowedTop.ToString("R")+" count="+(target.boulders==null?-1:target.boulders.Length);return false;}
                for(int row=0;row<target.boulders.Length;row++)
                {var pose=target.boulders[row];if(pose==null||!float.IsFinite(pose.uniformScale)||pose.uniformScale<=0){reason="Invalid/nonuniform boulder pose: "+context+" row="+row;return false;}var actual=TransformBounds(unit,pose.rotation,pose.uniformScale,pose.position);if(!Inside(envelope,actual)){reason="Outside-envelope boulder placement: "+context+" row="+row+" axes=["+InsideViolations(envelope,actual)+"] actualLocal{"+BoundsRange(actual)+"} allowedLocal{"+BoundsRange(envelope)+"} unit="+unit.ToString("R")+" pose="+pose.position.ToString("R")+" scale="+pose.uniformScale.ToString("R");return false;}}
            }
            if(settings.targets.Count(t=>t.landmark==2)!=2||settings.targets.Count(t=>t.landmark==6)!=3||settings.targets.Count(t=>t.overheadCap&&t.landmark==6)!=1||settings.targets.Sum(t=>t.boulders.Length)!=17){reason="Exactly four grounded mass groups plus one cave cap and17 owned rocks required.";return false;}
            return true;
        }
        public bool Apply(CampingWilderness world,CampLandmarkRockProfile settings)
        {
            if(Applied)return owner==world&&profile==settings;
            if(!Validate(world,settings,out string reason)){Diagnostic=reason;return false;}
            owner=world;profile=settings;visualRoot=new GameObject("Owned four-landmark natural boulders");visualRoot.transform.SetParent(world.transform,false);
            try
            {
                foreach(var target in settings.targets)
                {
                    var original=Resolve(world.transform,target.siblingPath).GetComponent<MeshRenderer>();
                    foreach(var pose in target.boulders)
                    {
                        var instance=Instantiate(settings.boulderPrefab,visualRoot.transform);instance.name="Natural closed boulder · landmark "+target.landmark;instance.transform.localPosition=pose.position;instance.transform.localRotation=pose.rotation;instance.transform.localScale=Vector3.one*pose.uniformScale;instance.SetActive(true);
                    }
                    originals.Add(original);
                }
                foreach(var renderer in originals)renderer.enabled=false;Applied=true;Diagnostic="All five original collider objects retained; seventeen uniform genuine closed boulder visuals.";Enroll();return true;
            }
            catch(Exception error){Release();Diagnostic=error.ToString();return false;}
        }
        void Enroll()
        {
            if(owner==null||!owner.gameObject.scene.IsValid()||!owner.gameObject.scene.isLoaded)return;CampingMatch match=owner.session;
            if(match==null)foreach(var root in owner.gameObject.scene.GetRootGameObjects()){match=root.GetComponentsInChildren<CampingMatch>(true).FirstOrDefault(m=>m.world==owner);if(match!=null)break;}
            if(match==null)return;var snow=match.GetComponent<SportsSnowSurface>();if(snow!=null&&snow.surface!=null){snow.surface.MarkDirty();ClimateDirtyCalls++;}
        }
        public void Release()
        {
            if(!Applied&&visualRoot==null&&originals.Count==0)return;
            foreach(var renderer in originals)if(renderer!=null)renderer.enabled=true;originals.Clear();
            if(visualRoot!=null){visualRoot.SetActive(false);if(Application.isPlaying)Destroy(visualRoot);else DestroyImmediate(visualRoot);visualRoot=null;}
            Applied=false;Enroll();
        }
        void OnDisable(){Release();}
        void OnDestroy(){Release();}
        void OnEnable(){if(owner!=null&&profile!=null&&!Applied)Apply(owner,profile);}
    }
}
