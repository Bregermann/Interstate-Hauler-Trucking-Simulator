#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Read-only witness around the ORIGINAL camera capture. No renderer, camera,
// transform, material, LOD, physics or simulation setting is changed here.
public sealed class DragonRiderMountedVisualObservation : IDisposable
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    readonly Component match;readonly string output;readonly Camera[] cameras;readonly Evidence evidence;
    [Serializable] sealed class Evidence {public string scope="Original mounted NWH runtime/render-time visibility witness only; no visual repair, synthetic props, state injection or human acceptance.";public string stage;public Frame before,after;public List<Frame> rendering=new List<Frame>();}
    [Serializable] sealed class Frame {public string context;public float clock;public View camera;public Broadcast broadcast;public Crew[] crews;}
    [Serializable] sealed class Broadcast {public int primary,actionPrimary;public Vector3 requestedFocus;public Bounds primaryBounds;public bool ownsShot,hasPreferredViewRotation;public Quaternion preferredViewRotation;public string shotReason;}
    [Serializable] sealed class View {public string path,mask;public bool enabled,occlusionCulling;public Vector3 position,euler;public float near,far,fov;public Rect rect;}
    [Serializable] sealed class Crew {public int slot;public string phase,profile,bodyId;public bool mounted,appearanceApplied;public PoseRecord truck,visuals,dragon,motor,saddle;public Vector3 physicsPosition,shapeCenter,shapeSize;public Body[] bodies;public Surface[] surfaces,saddleSurfaces,dragonSurfaces;public Lod[] lods;}
    [Serializable] sealed class PoseRecord {public string path;public bool active;public Vector3 position,euler,localPosition,localScale,lossyScale;}
    [Serializable] sealed class Body {public string path;public bool kinematic;public Vector3 physicsPosition,transformPosition;}
    [Serializable] sealed class Surface {public string path,type,mesh,attachedBody;public bool enabled,active,forceOff,isVisible,frustum,maskIncludesLayer;public int layer,vertices;public string renderingLayerMask;public Bounds bounds,localMeshBounds;public PoseRecord pose;public Rect viewportBounds;public float minViewDepth,maxViewDepth;public MaterialRecord[] materials;}
    [Serializable] sealed class MaterialRecord {public string path,guid,name,shader;public bool missing,supported,errorShader;public int passCount,renderQueue;}
    [Serializable] sealed class Lod {public string path;public bool enabled;public Vector3 localReferencePoint;public float size;public string[][] renderers;public float[] thresholds;}
    static object Read(object value,string name)=>value.GetType().GetField(name,Any) is FieldInfo field?field.GetValue(value):value.GetType().GetProperty(name,Any).GetValue(value);
    static string PathOf(Transform value)=>value==null?"none":value.parent==null?value.name:PathOf(value.parent)+"/"+value.name;
    static PoseRecord PoseOf(Transform value)=>value==null?null:new PoseRecord{path=PathOf(value),active=value.gameObject.activeInHierarchy,position=value.position,euler=value.eulerAngles,localPosition=value.localPosition,localScale=value.localScale,lossyScale=value.lossyScale};
    public DragonRiderMountedVisualObservation(Component match,string folder,string stage)
    {
        this.match=match??throw new ArgumentNullException(nameof(match));output=Path.Combine(folder,stage+"-mounted-visuals.json");cameras=(Camera[])Read(match,"cameras");evidence=new Evidence{stage=stage};
        evidence.before=Snapshot("before original camera capture",null);RenderPipelineManager.beginCameraRendering+=Rendering;
    }
    void Rendering(ScriptableRenderContext context,Camera camera)
    {
        if(cameras.Contains(camera)&&evidence.rendering.Count<8)evidence.rendering.Add(Snapshot("actual beginCameraRendering after existing presentation callbacks",camera));
    }
    Frame Snapshot(string context,Camera camera)
    {
        var frame=new Frame{context=context,clock=(float)Read(match,"Clock"),crews=((Array)Read(match,"racers")).Cast<object>().Select((racer,slot)=>SnapshotCrew(racer,slot,camera)).ToArray()};
        if(camera!=null)
        {
            frame.camera=new View{path=PathOf(camera.transform),mask=unchecked((uint)camera.cullingMask).ToString("X8"),enabled=camera.enabled,occlusionCulling=camera.useOcclusionCulling,position=camera.transform.position,euler=camera.transform.eulerAngles,near=camera.nearClipPlane,far=camera.farClipPlane,fov=camera.fieldOfView,rect=camera.rect};
            // These are the real public semantic inputs and completed framing
            // output at this render, not an inferred subject from truck color
            // or a guessed private adapter field. action.primary is the sport's
            // selected eligible subject; Primary may retain the existing shot
            // for its public minimum-shot interval.
            var rigType=Type.GetType("LWS.TruckSports.SportsEventCamera, Assembly-CSharp",true);var rig=camera.GetComponent(rigType);
            var framing=rig==null?null:Public(rig,"broadcastFraming");
            if(framing!=null)
            {
                var action=Public(framing,"action");
                frame.broadcast=new Broadcast{primary=(int)Public(framing,"Primary"),actionPrimary=(int)Public(action,"primary"),requestedFocus=(Vector3)Public(framing,"RequestedFocus"),primaryBounds=(Bounds)Public(framing,"PrimaryBounds"),ownsShot=(bool)Public(framing,"OwnsShot"),hasPreferredViewRotation=(bool)Public(action,"hasPreferredViewRotation"),preferredViewRotation=(Quaternion)Public(action,"preferredViewRotation"),shotReason=(string)Public(framing,"ShotReason")};
            }
        }
        return frame;
    }
    static object Public(object value,string name)
    {
        const BindingFlags flags=BindingFlags.Public|BindingFlags.Instance;
        return value.GetType().GetField(name,flags) is FieldInfo field?field.GetValue(value):value.GetType().GetProperty(name,flags).GetValue(value);
    }
    static Crew SnapshotCrew(object racer,int slot,Camera camera)
    {
        var truck=(Component)Read(racer,"truck");var dragon=Read(racer,"Dragon") as Component;var motor=dragon==null?null:(Component)Read(dragon,"motor");var body=(Rigidbody)Read(truck,"body");var shape=(BoxCollider)Read(truck,"shape");var visuals=(Transform)Read(truck,"visuals");var host=Read(truck,"appearanceHost");var saddle=dragon==null?null:(Transform)Read(dragon,"saddle");
        return new Crew{slot=slot,phase=Read(racer,"Phase").ToString(),mounted=(bool)Read(racer,"PhysicallyMounted"),appearanceApplied=host!=null&&(bool)Read(host,"Applied"),profile=host==null?null:Read(host,"ProfileId") as string,bodyId=host==null?null:Read(host,"EffectiveBodyId") as string,
            truck=PoseOf(truck.transform),visuals=PoseOf(visuals),dragon=PoseOf(dragon==null?null:dragon.transform),motor=PoseOf(motor==null?null:motor.transform),saddle=PoseOf(saddle),physicsPosition=body.position,shapeCenter=shape.bounds.center,shapeSize=shape.bounds.size,
            bodies=truck.GetComponentsInChildren<Rigidbody>(true).Select(value=>new Body{path=PathOf(value.transform),kinematic=value.isKinematic,physicsPosition=value.position,transformPosition=value.transform.position}).ToArray(),
            surfaces=truck.GetComponentsInChildren<Renderer>(true).Select(value=>SnapshotSurface(value,camera)).ToArray(),saddleSurfaces=saddle==null?Array.Empty<Surface>():saddle.GetComponentsInChildren<Renderer>(true).Select(value=>SnapshotSurface(value,camera)).ToArray(),dragonSurfaces=motor==null?Array.Empty<Surface>():motor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(value=>SnapshotSurface(value,camera)).ToArray(),
            lods=truck.GetComponentsInChildren<LODGroup>(true).Select(value=>new Lod{path=PathOf(value.transform),enabled=value.enabled,localReferencePoint=value.localReferencePoint,size=value.size,renderers=value.GetLODs().Select(lod=>lod.renderers.Select(renderer=>renderer==null?"null":PathOf(renderer.transform)).ToArray()).ToArray(),thresholds=value.GetLODs().Select(lod=>lod.screenRelativeTransitionHeight).ToArray()}).ToArray()};
    }
    static Surface SnapshotSurface(Renderer renderer,Camera camera)
    {
        var filter=renderer.GetComponent<MeshFilter>();var mesh=renderer is SkinnedMeshRenderer skin?skin.sharedMesh:filter==null?null:filter.sharedMesh;var attached=renderer.GetComponentInParent<Rigidbody>();
        var surface=new Surface{path=PathOf(renderer.transform),type=renderer.GetType().Name,enabled=renderer.enabled,active=renderer.gameObject.activeInHierarchy,forceOff=renderer.forceRenderingOff,isVisible=renderer.isVisible,layer=renderer.gameObject.layer,renderingLayerMask=renderer.renderingLayerMask.ToString("X8"),bounds=renderer.bounds,pose=PoseOf(renderer.transform),attachedBody=attached==null?"none":PathOf(attached.transform),mesh=mesh==null?"none":AssetDatabase.GetAssetPath(mesh)+"#"+mesh.name,vertices=mesh==null?0:mesh.vertexCount,localMeshBounds=mesh==null?default:mesh.bounds,
            materials=renderer.sharedMaterials.Select(material=>material==null?new MaterialRecord{missing=true}:new MaterialRecord{path=AssetDatabase.GetAssetPath(material),guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(material)),name=material.name,shader=material.shader==null?"none":material.shader.name,supported=material.shader!=null&&material.shader.isSupported,errorShader=material.shader==null||material.shader.name=="Hidden/InternalErrorShader",passCount=material.passCount,renderQueue=material.renderQueue}).ToArray()};
        if(camera!=null)
        {
            surface.frustum=GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera),renderer.bounds);surface.maskIncludesLayer=(camera.cullingMask&(1<<surface.layer))!=0;
            var min=new Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity);var max=new Vector3(float.NegativeInfinity,float.NegativeInfinity,float.NegativeInfinity);var bounds=renderer.bounds;
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2){var point=camera.WorldToViewportPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z)));min=Vector3.Min(min,point);max=Vector3.Max(max,point);}
            surface.viewportBounds=Rect.MinMaxRect(min.x,min.y,max.x,max.y);surface.minViewDepth=min.z;surface.maxViewDepth=max.z;
        }
        return surface;
    }
    public void Dispose()
    {
        RenderPipelineManager.beginCameraRendering-=Rendering;evidence.after=Snapshot("after original camera capture",null);Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,JsonUtility.ToJson(evidence,true)+"\n");
    }
}
#endif
