using System;
using Cinemachine;
using UnityEngine;
using LWS.TruckBowling;

namespace LWS.TruckSports
{
    [Serializable]
    public sealed class SportsBroadcastSettings
    {
        [Range(.04f,.2f)] public float minimumTruckHeight=.08f;
        [Range(.06f,.25f)] public float preferredTruckHeight=.11f;
        [Min(1)] public float maximumDistanceInTruckLengths=11;
        [Min(1)] public float maximumOrthoInTruckLengths=5;
        [Min(.5f)] public float minimumDistanceInTruckLengths=1.5f;
        [Range(35,75)] public float elevation=58;
        public float azimuth;
        [Range(35,75)] public float fieldOfView=58;
        [Min(0)] public float followDamping=.45f,zoomDamping=.7f;
        [Min(.1f)] public float minimumShotSeconds=1.5f;
        [Min(1)] public float nearbyTruckLengths=5;
        [Min(1)] public float objectiveRangeInTruckLengths=12;
        [Range(1,6)] public float objectiveWeight=2,primaryWeight=3;
        [Range(.5f,.85f)]public float groupFramingSize=.78f;
        [Range(.4f,.65f)]public float screenY=.5f;
        public bool offscreenObjective=true;
    }

    // Read-only semantic input to the existing Cinemachine rig, rebuilt without allocations.
    public sealed class SportsBroadcastAction
    {
        public int primary=-1;
        public bool hasObjective,preferHuman=true;
        public Bounds objective;
        public string reason="LOCAL ACTION";
        public void Reset(){primary=-1;hasObjective=false;preferHuman=true;reason="LOCAL ACTION";}
        public void Objective(Vector3 position,float radius=1){objective=new Bounds(position,Vector3.one*Mathf.Max(.1f,radius)*2);hasObjective=true;}
    }

    // LWS selects legitimate action and sets readability limits. Cinemachine retains
    // group composition, dolly/lens adjustment, damping and final rig evaluation.
    // Only explicitly bound shared views participate; cabs, precision views and replays do not.
    [DefaultExecutionOrder(850)]
    public sealed class SportsBroadcastFraming:MonoBehaviour
    {
        public SportsEventSession session;public SportsBroadcastSource source;
        public int ParticipantCount=>source!=null?source.Count:session==null?0:session.setup.count;
        public bool IsHuman(int slot)=>source!=null?source.IsHuman(slot):session.setup.players[slot].source!=BowlingSource.AI;
        public string ParticipantName(int slot)=>source!=null?source.ParticipantName(slot):session.setup.players[slot].name;
        public Color ParticipantColor(int slot)=>source!=null?source.ParticipantColor(slot):session.setup.players[slot].color;
        public SportsEventCamera cameraRig;
        public SportsEventTruck[] trucks;
        public Rigidbody[] bodies;public Transform[] modelRoots;public Collider[] bodyShapes;
        [Tooltip("Sport attachments excluded from tractor readability, e.g. a twelve-metre jousting lance.")]
        public Transform[] excludedSubjectRoots=Array.Empty<Transform>();
        public int SubjectCount=>trucks!=null&&trucks.Length>0?trucks.Length:bodies==null?0:bodies.Length;
        public Rigidbody Body(int slot)=>trucks!=null&&trucks.Length>0?trucks[slot].body:bodies[slot];
        Transform Model(int slot)=>trucks!=null&&trucks.Length>0?trucks[slot].visuals:modelRoots[slot];
        Collider Shape(int slot)=>trucks!=null&&trucks.Length>0?trucks[slot].shape:bodyShapes[slot];
        public SportsBroadcastProfile sharedDefaults;
        public bool useSharedDefaults=true;
        [Tooltip("Per-sport overrides; disable Use Shared Defaults to use these values.")]
        public SportsBroadcastSettings settings=new SportsBroadcastSettings();
        public SportsBroadcastSettings Configuration=>useSharedDefaults&&sharedDefaults!=null?sharedDefaults.settings:settings;
        public bool useOrthographic;
        [HideInInspector]public int authoringVersion;
        public int Primary{get;private set;}=-1;
        public float ProjectedTruckHeight{get;private set;}
        public int IncludedTrucks{get;private set;}
        public bool ObjectiveVisible{get;private set;}
        public string ShotReason=>action.reason;
        public Bounds PrimaryBounds{get;private set;}
        public Vector3 RequestedFocus{get;private set;}
        public bool OwnsShot=>enabled&&(source!=null?source.Playing:session!=null&&session.Running&&!session.Finished&&!session.ReplayControls&&session.BroadcastFramingApplicable)&&cameraRig!=null&&cameraRig.view!=null&&cameraRig.view.enabled;
        public readonly SportsBroadcastAction action=new SportsBroadcastAction();
        Renderer[][] renderers;
        Vector3[][] worldCorners;
        CinemachineTargetGroup group;
        CinemachineGroupComposer composer;
        CinemachineCollider occlusion;
        Transform[] proxies;
        bool initialized,wasActive;
        float nextShot;
        Vector3 focus;
        readonly RaycastHit[] contacts=new RaycastHit[32];

        public void Request(Vector3 point){RequestedFocus=point;}
        void Awake(){if(cameraRig==null)cameraRig=GetComponent<SportsEventCamera>();if(cameraRig!=null)cameraRig.broadcastFraming=this;}
        public bool Valid(int slot)=>slot>=0&&slot<SubjectCount&&slot<ParticipantCount&&Body(slot)!=null&&Body(slot).gameObject.activeInHierarchy&&(source!=null?source.Valid(slot):session.BroadcastParticipantActive(slot));
        public int Nearest(Vector3 point,bool humansOnly=false)
        {
            int best=-1;float distance=float.PositiveInfinity;
            if(SubjectCount==0||ParticipantCount==0)return best;
            for(int i=0;i<SubjectCount;i++)if(Valid(i)&&(!humansOnly||IsHuman(i)))
            {float d=(Body(i).position-point).sqrMagnitude;if(d<distance){distance=d;best=i;}}
            return best;
        }
        public Bounds TruckBounds(int slot)
        {
            Bounds result=default;bool found=false;
            if(renderers==null)CacheRenderers();
            foreach(var renderer in renderers[slot])if(renderer!=null&&renderer.enabled&&renderer.gameObject.activeInHierarchy&&renderer is not ParticleSystemRenderer&&renderer is not TrailRenderer&&renderer is not LineRenderer)
            {if(!found){result=renderer.bounds;found=true;}else result.Encapsulate(renderer.bounds);}
            return found?result:Shape(slot).bounds;
        }
        bool Excluded(Renderer renderer){foreach(var root in excludedSubjectRoots)if(root!=null&&renderer.transform.IsChildOf(root))return true;return false;}
        void CacheRenderers(){renderers=new Renderer[SubjectCount][];worldCorners=new Vector3[SubjectCount][];for(int i=0;i<SubjectCount;i++){var all=Model(i)!=null?Model(i).GetComponentsInChildren<Renderer>(true):Array.Empty<Renderer>();var meshes=new System.Collections.Generic.List<Renderer>(all.Length);foreach(var renderer in all)if((renderer is MeshRenderer||renderer is SkinnedMeshRenderer)&&!Excluded(renderer)&&renderer.GetComponent<TextMesh>()==null&&renderer.GetComponent<TMPro.TMP_Text>()==null)meshes.Add(renderer);renderers[i]=meshes.ToArray();worldCorners[i]=new Vector3[Mathf.Max(8,renderers[i].Length*8)];}}
        int SampleCorners(int slot)
        {
            if(renderers==null)CacheRenderers();int count=0;
            foreach(var renderer in renderers[slot])if(renderer!=null&&renderer.enabled&&renderer.gameObject.activeInHierarchy&&renderer is not ParticleSystemRenderer&&renderer is not TrailRenderer&&renderer is not LineRenderer)
            {
                var bounds=renderer.localBounds;var matrix=renderer.localToWorldMatrix;
                for(int i=0;i<8;i++)worldCorners[slot][count++]=matrix.MultiplyPoint3x4(bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
            }
            if(count==0){var bounds=Shape(slot).bounds;for(int i=0;i<8;i++)worldCorners[slot][count++]=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));}
            return count;
        }
        public Rect ProjectedTruckRect(Camera camera,int slot)
        {
            int count=SampleCorners(slot);Vector2 min=Vector2.one*float.MaxValue,max=Vector2.one*float.MinValue;
            for(int i=0;i<count;i++){var point=camera.WorldToViewportPoint(worldCorners[slot][i]);min=Vector2.Min(min,new Vector2(point.x,point.y));max=Vector2.Max(max,new Vector2(point.x,point.y));}
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        void Initialize()
        {
            CacheRenderers();
            var holder=new GameObject("Broadcast action targets (Cinemachine)");holder.transform.SetParent(transform.parent,false);
            group=holder.AddComponent<CinemachineTargetGroup>();group.m_PositionMode=CinemachineTargetGroup.PositionMode.GroupCenter;group.m_RotationMode=CinemachineTargetGroup.RotationMode.Manual;
            group.m_UpdateMethod=CinemachineTargetGroup.UpdateMethod.LateUpdate;group.enabled=false;
            proxies=new Transform[SubjectCount+1];group.m_Targets=new CinemachineTargetGroup.Target[proxies.Length];
            for(int i=0;i<proxies.Length;i++){proxies[i]=new GameObject("Action bound "+i).transform;proxies[i].SetParent(holder.transform,false);group.m_Targets[i].target=proxies[i];}
            // Adding another Aim-stage component replaces the old one through the public API.
            composer=cameraRig.rig.AddCinemachineComponent<CinemachineGroupComposer>();
            occlusion=cameraRig.rig.GetComponent<CinemachineCollider>();if(occlusion==null)occlusion=cameraRig.rig.gameObject.AddComponent<CinemachineCollider>();
            occlusion.m_CollideAgainst=~0;occlusion.m_AvoidObstacles=true;occlusion.m_CameraRadius=.45f;occlusion.m_Damping=.35f;occlusion.m_DampingWhenOccluded=.1f;occlusion.m_SmoothingTime=.15f;occlusion.m_MinimumOcclusionTime=.12f;
            composer.m_AdjustmentMode=CinemachineGroupComposer.AdjustmentMode.DollyOnly;
            composer.m_GroupFramingSize=.78f;composer.m_DeadZoneWidth=.1f;composer.m_DeadZoneHeight=.1f;
            initialized=true;
        }
        void Add(int index,Bounds bounds,float weight)
        {
            proxies[index].position=bounds.center;group.m_Targets[index].weight=weight;
            // The vendor target group takes spheres; the LWS hard limit below uses
            // all eight actual renderer-bounds corners rather than this approximation.
            group.m_Targets[index].radius=Mathf.Max(.1f,bounds.extents.magnitude*.65f);
        }
        void LateUpdate()
        {
            if(!OwnsShot){Release();return;}
            action.Reset();if(source!=null)source.Describe(action,this);else session.DescribeBroadcast(action,this);
            int candidate=Valid(action.primary)?action.primary:Nearest(action.hasObjective?action.objective.center:RequestedFocus,action.preferHuman);
            if(candidate<0)candidate=Nearest(action.hasObjective?action.objective.center:RequestedFocus);
            if(candidate<0)return;
            if(!initialized)Initialize();
            bool activate=!wasActive;wasActive=true;
            if(activate||!Valid(Primary)||Time.unscaledTime>=nextShot&&candidate!=Primary)
            {Primary=candidate;nextShot=Time.unscaledTime+Configuration.minimumShotSeconds;}
            var primary=TruckBounds(Primary);PrimaryBounds=primary;
            int pointCount=SampleCorners(Primary);var points=worldCorners[Primary];
            float length=Mathf.Max(2,Mathf.Max(primary.size.x,primary.size.z));
            occlusion.enabled=true;occlusion.m_MinimumDistanceFromTarget=length*.8f;
            float radius=length*Configuration.nearbyTruckLengths;
            Vector3 desired=primary.center;
            bool includeObjective=action.hasObjective&&Vector3.Distance(primary.center,action.objective.center)<length*Configuration.objectiveRangeInTruckLengths;
            if(includeObjective)desired=Vector3.Lerp(primary.center,action.objective.center,.35f);
            for(int i=0;i<group.m_Targets.Length;i++)group.m_Targets[i].weight=0;
            Add(Primary,primary,Configuration.primaryWeight);IncludedTrucks=1;
            for(int i=0;i<SubjectCount;i++)if(i!=Primary&&Valid(i)&&Vector3.Distance(Body(i).position,desired)<radius)
            {Add(i,TruckBounds(i),1);IncludedTrucks++;}
            if(includeObjective)Add(SubjectCount,action.objective,Configuration.objectiveWeight);
            group.DoUpdate();cameraRig.rig.LookAt=group.transform;
            // Follow and Aim must share the vendor-composed center. A separately
            // smoothed focus tilted the intended angle and cropped the objective;
            // the existing Transposer/Composer already own the damping.
            focus=group.transform.position;
            composer.m_GroupFramingSize=Configuration.groupFramingSize;composer.m_ScreenY=Configuration.screenY;
            var camera=cameraRig.view;camera.orthographic=useOrthographic;
            cameraRig.rig.m_Lens.ModeOverride=useOrthographic?LensSettings.OverrideModes.Orthographic:LensSettings.OverrideModes.Perspective;
            Quaternion rotation=Quaternion.Euler(Configuration.elevation,Configuration.azimuth,0);
            float minDistance=length*Configuration.minimumDistanceInTruckLengths;
            float hardDistance=Mathf.Min(length*Configuration.maximumDistanceInTruckLengths,MaximumReadableCornersDistance(points,pointCount,focus,rotation,Configuration.fieldOfView,Configuration.minimumTruckHeight));
            float preferred=Mathf.Min(hardDistance,MaximumReadableCornersDistance(points,pointCount,focus,rotation,Configuration.fieldOfView,Configuration.preferredTruckHeight));
            hardDistance=Mathf.Max(minDistance,hardDistance);preferred=Mathf.Clamp(preferred,minDistance,hardDistance);
            // Filling the group to78% must never enlarge one isolated truck to78%.
            // Cinemachine's inward bound uses the preferred renderer projection too.
            composer.m_MinimumDistance=preferred;composer.m_MaximumDistance=hardDistance;
            composer.m_MaxDollyIn=hardDistance;composer.m_MaxDollyOut=hardDistance;
            composer.m_MinimumFOV=composer.m_MaximumFOV=Configuration.fieldOfView;composer.m_FrameDamping=Configuration.zoomDamping;
            float span=CornerSpan(points,pointCount,rotation);
            composer.m_MinimumOrthoSize=span/Mathf.Max(Configuration.minimumTruckHeight,Configuration.preferredTruckHeight)*.5f;
            composer.m_MaximumOrthoSize=Mathf.Min(length*Configuration.maximumOrthoInTruckLengths,span/Mathf.Max(.04f,Configuration.minimumTruckHeight)*.5f);
            if(activate)cameraRig.rig.m_Lens.OrthographicSize=Mathf.Clamp(span/Configuration.preferredTruckHeight*.5f,composer.m_MinimumOrthoSize,composer.m_MaximumOrthoSize);
            cameraRig.follow.m_XDamping=cameraRig.follow.m_YDamping=cameraRig.follow.m_ZDamping=Configuration.followDamping;
            cameraRig.PoseRaw(focus,-(rotation*Vector3.forward)*preferred,Configuration.fieldOfView,activate);
            // Cinemachine's group framing may need to exceed a composition bound.
            // Read actual projected bounds and enforce readability after vendor evaluation.
            var projected=ProjectedTruckRect(camera,Primary);
            if(camera.orthographic)camera.orthographicSize=Mathf.Clamp(camera.orthographicSize,composer.m_MinimumOrthoSize,composer.m_MaximumOrthoSize);
            else if(projected.height<Configuration.minimumTruckHeight*.98f)
            {
                var delta=camera.transform.position-primary.center;
                float ratio=Mathf.Clamp(projected.height/Mathf.Max(.04f,Configuration.minimumTruckHeight),.25f,1);
                camera.transform.position=primary.center+delta*ratio;
            }
            SafePosition(primary.center);
            ProjectedTruckHeight=ProjectedTruckRect(camera,Primary).height;
            var objective=camera.WorldToViewportPoint(action.objective.center);
            ObjectiveVisible=!action.hasObjective||objective.z>camera.nearClipPlane&&objective.x>=.03f&&objective.x<=.97f&&objective.y>=.08f&&objective.y<=.92f;
        }
        public void Release(){if(wasActive&&cameraRig!=null)cameraRig.rig.LookAt=cameraRig.aim;if(occlusion!=null)occlusion.enabled=false;wasActive=false;}
        void SafePosition(Vector3 target)
        {
            var camera=cameraRig.view;var position=camera.transform.position;
            int count=Physics.RaycastNonAlloc(position+Vector3.up*200,Vector3.down,contacts,400,~0,QueryTriggerInteraction.Ignore);
            float floor=float.NegativeInfinity;
            for(int i=0;i<count;i++)if(contacts[i].collider is TerrainCollider&&contacts[i].point.y>floor)floor=contacts[i].point.y;
            if(float.IsFinite(floor)&&position.y<floor+2)position.y=floor+2;
            // CinemachineCollider owns obstacle resolution. This last terrain-height
            // guard also covers a shot initialized beneath a mountain, before its ray
            // to the target can reach a useful outside surface.
            camera.transform.position=position;
        }
        void OnDisable(){Release();}
        public static float ProjectedSpan(Bounds bounds,Quaternion rotation)
        {
            var up=rotation*Vector3.up;return Mathf.Abs(up.x)*bounds.size.x+Mathf.Abs(up.y)*bounds.size.y+Mathf.Abs(up.z)*bounds.size.z;
        }
        public static float MaximumReadableDistance(Bounds bounds,Vector3 focus,Quaternion rotation,float fov,float height)
        {
            var corners=new Vector3[8];for(int i=0;i<8;i++)corners[i]=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
            return MaximumReadableCornersDistance(corners,8,focus,rotation,fov,height);
        }
        public static float CornerSpan(Vector3[] points,int count,Quaternion rotation)
        {var up=rotation*Vector3.up;float min=float.PositiveInfinity,max=float.NegativeInfinity;for(int i=0;i<count;i++){float y=Vector3.Dot(up,points[i]);min=Mathf.Min(min,y);max=Mathf.Max(max,y);}return max-min;}
        public static float MaximumReadableCornersDistance(Vector3[] corners,int count,Vector3 focus,Quaternion rotation,float fov,float height)
        {
            float low=.5f,high=20000;Quaternion inverse=Quaternion.Inverse(rotation);float tangent=Mathf.Tan(Mathf.Clamp(fov,1,170)*Mathf.Deg2Rad*.5f);
            for(int step=0;step<20;step++)
            {
                float distance=(low+high)*.5f,min=float.PositiveInfinity,max=float.NegativeInfinity;bool behind=false;
                for(int i=0;i<count;i++)
                {var local=inverse*(corners[i]-focus);float depth=local.z+distance;if(depth<=.01f){behind=true;break;}float y=local.y/(2*depth*tangent);min=Mathf.Min(min,y);max=Mathf.Max(max,y);}
                if(behind||max-min>height)low=distance;else high=distance;
            }
            return low;
        }
        void OnDestroy(){if(group!=null)Destroy(group.gameObject);}
    }
}
