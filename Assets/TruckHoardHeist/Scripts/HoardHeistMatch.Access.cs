using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using LWS.TruckBowling;
using UnityEngine;

namespace LWS.TruckHoardHeist
{
    public sealed partial class HoardHeistMatch
    {
        // AI target eligibility only. Original cargo, human interaction, crane
        // limits, network planner, live avoidance and scoring retain authority.
        readonly float[,] treasureAccessUntil=new float[4,64];
        readonly Vector3[,] treasureAccessPosition=new Vector3[4,64];
        readonly Vector3[,] treasureAccessParking=new Vector3[4,64];
        readonly bool[,] treasureAccessValid=new bool[4,64];
        readonly string[,] treasureAccessReason=new string[4,64];
        readonly Collider[] accessAttachedLoads=new Collider[64];
        readonly float[] roadOriginUntil=new float[4],roadOriginLostSeconds=new float[4];
        readonly Vector3[] roadOriginPosition=new Vector3[4],roadOriginRoadPoint=new Vector3[4];
        readonly bool[] roadOriginValid=new bool[4],roadOriginHolding=new bool[4];
        readonly string[] roadOriginReason=new string[4];
        int treasureAccessChecks;

        // Heist owns finite workspace/right-of-way semantics. The shared route
        // and NWH remain the only road planner and physical driving authority.
        readonly Collider[][] trafficRigShapes=new Collider[4][];
        readonly Bounds[] trafficRigBounds=new Bounds[4];
        readonly bool[] trafficHolding=new bool[4],trafficReverse=new bool[4];
        readonly int[] trafficBlocker=new int[4],trafficYieldPhase=new int[4],trafficYieldTo=new int[4],trafficYieldStarts=new int[4];
        readonly float[] trafficBlockedSince=new float[4],trafficRetryAt=new float[4],trafficYieldUntil=new float[4];
        readonly Vector3[] trafficYieldOrigin=new Vector3[4],trafficYieldRear=new Vector3[4],trafficYieldGoal=new Vector3[4];
        readonly int[] trafficEpisodeOther={-1,-1,-1,-1};readonly Vector3[] trafficEpisodeOrigin=new Vector3[4];readonly float[] trafficClearSince=new float[4];
        readonly Vector2[] trafficAttempt=new Vector2[4],trafficYieldDrive=new Vector2[4];
        readonly int[] trafficNoCourseOther={-1,-1,-1,-1};readonly float[] trafficNoCourseAt=new float[4];readonly bool[] trafficYieldBackwards=new bool[4];readonly string[] trafficYieldRejection=new string[4];
        float trafficBoundsAt=-1;
        void ResetRigTraffic()
        {
            trafficBoundsAt=-1;for(int i=0;i<4;i++){trafficRigShapes[i]=null;trafficYieldStarts[i]=0;ClearRigYield(i);}
        }
        void ClearRigYield(int slot)
        {trafficHolding[slot]=trafficReverse[slot]=false;trafficYieldPhase[slot]=0;trafficBlocker[slot]=trafficYieldTo[slot]=-1;trafficBlockedSince[slot]=trafficRetryAt[slot]=trafficYieldUntil[slot]=0;trafficAttempt[slot]=trafficYieldDrive[slot]=Vector2.zero;trafficYieldStarts[slot]=0;trafficEpisodeOther[slot]=-1;trafficClearSince[slot]=0;trafficNoCourseOther[slot]=-1;trafficNoCourseAt[slot]=0;trafficYieldBackwards[slot]=false;trafficYieldRejection[slot]=null;}
        void RefreshRigBounds()
        {
            if(trafficBoundsAt==Clock)return;trafficBoundsAt=Clock;
            for(int slot=0;slot<setup.count;slot++)
            {
                var truck=trucks[slot];if(trafficRigShapes[slot]==null)trafficRigShapes[slot]=truck.GetComponentsInChildren<Collider>(true);
                bool first=true;Bounds bounds=default;
                foreach(var shape in trafficRigShapes[slot])if(shape!=null&&shape.enabled&&!shape.isTrigger&&shape.gameObject.activeInHierarchy)
                {if(first){bounds=shape.bounds;first=false;}else bounds.Encapsulate(shape.bounds);}
                foreach(var item in treasure)if(item.CargoJoint!=null&&item.Carrier==slot&&item.CargoJoint.connectedBody==truck.body&&item.shape.enabled&&item.gameObject.activeInHierarchy)bounds.Encapsulate(item.shape.bounds);
                trafficRigBounds[slot]=first?truck.shape.bounds:bounds;
            }
        }
        Bounds RigAt(int slot,Vector3 position,Quaternion rotation)
        {
            RefreshRigBounds();var current=trafficRigBounds[slot];var body=trucks[slot].body;var turn=rotation*Quaternion.Inverse(body.rotation);Bounds result=default;bool first=true;
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
            {var point=position+turn*(current.center+Vector3.Scale(current.extents,new Vector3(x,y,z))-body.position);if(first){result=new Bounds(point,Vector3.zero);first=false;}else result.Encapsulate(point);}
            result.Expand(new Vector3(.5f,.1f,.5f));return result;
        }
        Bounds HoistWorkspace(int slot,int item)
        {
            var point=treasureAccessValid[slot,item]?treasureAccessParking[slot,item]:trucks[slot].body.position;
            var forward=Vector3.ProjectOnPlane(treasure[item].body.position-point,Vector3.up);var rotation=forward.sqrMagnitude>.01f?Quaternion.LookRotation(forward):trucks[slot].body.rotation;
            var bounds=RigAt(slot,point,rotation);bounds.Encapsulate(treasure[item].shape.bounds);return bounds;
        }
        bool HoistWorkspaceClear(int slot,int item)
        {
            RefreshRigBounds();var proposed=HoistWorkspace(slot,item);
            for(int other=0;other<setup.count;other++)if(other!=slot&&trucks[other].gameObject.activeInHierarchy)
            {
                if(proposed.Intersects(trafficRigBounds[other]))return false;
                int claimed=processing[other]>=0?processing[other]:other<slot&&IsDriving(other)&&!hiding[other]?Targets[other]:-1;
                if(claimed<0||claimed>=treasure.Length||processing[other]<0&&!Rules.Fits(claimed,other))continue;
                var occupied=processing[other]>=0?trafficRigBounds[other]:HoistWorkspace(other,claimed);occupied.Encapsulate(treasure[claimed].shape.bounds);
                if(proposed.Intersects(occupied))return false;
            }
            return true;
        }
        int RigTrafficRank(int slot)
        {
            int role=setup.players[slot].source!=BowlingSource.AI||!IsDriving(slot)||HoistBusy(slot)?-2:
                Rules.CargoValue(slot)>0&&(Alert.Awakened||Alert.Value>=55||Rules.LoadCount(slot)>=Rules.CapacitySlots||SecondsRemaining<55||Targets[slot]<0)?0:1;
            return role*8+(slot+(int)((uint)MatchSeed%4))%4;
        }
        Bounds RigSweep(int slot,Vector2 drive)
        {
            var truck=trucks[slot];var body=truck.body;var result=RigAt(slot,body.position,body.rotation);float speed=body.linearVelocity.magnitude;
            var drift=Vector3.ProjectOnPlane(body.linearVelocity,Vector3.up)*.75f;result.Encapsulate(RigAt(slot,body.position+drift,body.rotation));
            if(Mathf.Abs(drive.y)<.01f)return result;
            float distance=Mathf.Clamp(speed*.85f+1.5f,1.5f,7),yaw=Mathf.Clamp(body.angularVelocity.y*Mathf.Rad2Deg*.75f+drive.x*12*Mathf.Sign(drive.y),-35,35);
            var forward=Vector3.ProjectOnPlane(truck.transform.forward,Vector3.up).normalized*Mathf.Sign(drive.y);
            for(int step=1;step<=3;step++){float f=step/3f;result.Encapsulate(RigAt(slot,body.position+forward*(distance*f),Quaternion.AngleAxis(yaw*f,Vector3.up)*body.rotation));}
            return result;
        }
        bool RigTrafficConflict(int slot,Vector2 drive,out int blocker,bool incoming=true)
        {
            RefreshRigBounds();var swept=RigSweep(slot,drive);blocker=-1;
            for(int other=0;other<setup.count;other++)if(other!=slot&&trucks[other].gameObject.activeInHierarchy)
            {
                var otherDrive=trafficHolding[other]?trafficAttempt[other]:trucks[other].command;
                if(swept.Intersects(trafficRigBounds[other])||incoming&&RigTrafficRank(slot)>RigTrafficRank(other)&&RigSweep(other,otherDrive).Intersects(trafficRigBounds[slot]))
                {if(blocker<0||RigTrafficRank(other)<RigTrafficRank(blocker))blocker=other;}
            }
            return blocker>=0;
        }
        IEnumerable<Collider> RigPhysicalShapes(int slot)
        {
            RefreshRigBounds();foreach(var shape in trafficRigShapes[slot])if(shape!=null&&shape.enabled&&!shape.isTrigger&&shape.gameObject.activeInHierarchy)yield return shape;
            foreach(var item in treasure)if(item.CargoJoint!=null&&item.Carrier==slot&&item.CargoJoint.connectedBody==trucks[slot].body&&item.shape.enabled&&item.gameObject.activeInHierarchy)yield return item.shape;
        }
        static bool RigShapeResolved(Collider shape)=>shape is BoxCollider||shape is SphereCollider||shape is CapsuleCollider||shape is MeshCollider mesh&&mesh.convex&&mesh.sharedMesh!=null;
        static Pose RigShapePose(Collider shape,Rigidbody root,Pose pose)
        {var turn=pose.rotation*Quaternion.Inverse(root.rotation);return new Pose(pose.position+turn*(shape.transform.position-root.position),turn*shape.transform.rotation);}
        static Bounds RigShapeBounds(Collider shape,Rigidbody root,Pose pose)
        {
            var turn=pose.rotation*Quaternion.Inverse(root.rotation);Bounds result=default;bool first=true;
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2){var point=pose.position+turn*(shape.bounds.center+Vector3.Scale(shape.bounds.extents,new Vector3(x,y,z))-root.position);if(first){result=new Bounds(point,Vector3.zero);first=false;}else result.Encapsulate(point);}return result;
        }
        bool RigSeparatingPose(int slot,int other,Pose initial,Pose proposed,Dictionary<(int,int),float> depths,bool finish)
        {
            // Actual per-shape penetration, not aggregate-center direction, owns safe separation.
            foreach(var own in RigPhysicalShapes(slot))
            {
                var from=RigShapePose(own,trucks[slot].body,initial);var to=RigShapePose(own,trucks[slot].body,proposed);var fromBounds=RigShapeBounds(own,trucks[slot].body,initial);var toBounds=RigShapeBounds(own,trucks[slot].body,proposed);
                foreach(var shape in RigPhysicalShapes(other))
                {
                    if(!fromBounds.Intersects(shape.bounds)&&!toBounds.Intersects(shape.bounds))continue;
                    // No unsupported shape can acquire a separation exception.
                    if(!RigShapeResolved(own)||!RigShapeResolved(shape))return false;
                    bool existed=Physics.ComputePenetration(own,from.position,from.rotation,shape,shape.transform.position,shape.transform.rotation,out _,out float originalDepth);
                    bool future=Physics.ComputePenetration(own,to.position,to.rotation,shape,shape.transform.position,shape.transform.rotation,out _,out float depth);
                    if(!existed&&future)return false;if(!future)depth=0;var key=(own.GetInstanceID(),shape.GetInstanceID());float previous=depths.TryGetValue(key,out var last)?last:existed?originalDepth:0;
                    if(depth>previous+.00001f||finish&&existed&&future&&depth>=originalDepth-.001f)return false;depths[key]=depth;
                }
            }
            return true;
        }
        bool RigYieldCorridor(int slot,Vector3 from,Vector3 to,Quaternion fromRotation,Quaternion toRotation)
        {
            Array.Clear(accessAttachedLoads,0,accessAttachedLoads.Length);int loads=0;foreach(var item in treasure)if(item.CargoJoint!=null&&item.Carrier>=0&&loads<accessAttachedLoads.Length)accessAttachedLoads[loads++]=item.shape;
            int samples=Mathf.CeilToInt(Vector3.Distance(from,to));if(samples>10){trafficYieldRejection[slot]="COURSE_LENGTH";return false;}Pose previous=default;var initial=new Pose(from,fromRotation);var originalBounds=RigAt(slot,from,fromRotation);var depths=new Dictionary<(int,int),float>();
            for(int i=0;i<=samples;i++)
            {
                float f=i/(float)Mathf.Max(1,samples);var rotation=Quaternion.Slerp(fromRotation,toRotation,f);if(!TryGroundedPose(slot,Vector3.Lerp(from,to,f),rotation,out var pose,true,accessAttachedLoads)||i>0&&Mathf.Abs(pose.position.y-previous.position.y)>.65f){trafficYieldRejection[slot]="STATIC_OR_TERRAIN_POSE";return false;}
                var half=Vector3.Scale(trucks[slot].shape.size*.5f,trucks[slot].transform.lossyScale);for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)if(!TryFindSupportedSurface(pose.position+pose.rotation*new Vector3(x*half.x,0,z*half.z),out _,out _,out _)){trafficYieldRejection[slot]="WHEEL_SUPPORT";return false;}
                if(i==0)pose=initial;var bounds=RigAt(slot,pose.position,pose.rotation);
                for(int other=0;other<setup.count;other++)if(other!=slot&&trucks[other].gameObject.activeInHierarchy)
                {
                    bool beganOverlapping=originalBounds.Intersects(trafficRigBounds[other]);
                    if(!beganOverlapping){if(bounds.Intersects(trafficRigBounds[other])){trafficYieldRejection[slot]="NEW_RIG_OVERLAP_"+other;return false;}}
                    else if(!RigSeparatingPose(slot,other,initial,pose,depths,i==samples&&Vector3.Distance(from,to)>=2.5f)){trafficYieldRejection[slot]="NON_SEPARATING_RIG_"+other;return false;}
                }
                previous=pose;
            }
            return true;
        }
        void RigConflictEpisode(int slot,int other)
        {
            if(trafficEpisodeOther[slot]!=other||Vector3.ProjectOnPlane(trucks[slot].body.position-trafficEpisodeOrigin[slot],Vector3.up).magnitude>6)
            {trafficYieldStarts[slot]=0;trafficEpisodeOther[slot]=other;trafficEpisodeOrigin[slot]=trucks[slot].body.position;trafficNoCourseOther[slot]=-1;trafficNoCourseAt[slot]=0;}
            trafficClearSince[slot]=0;
        }
        void RigClearProgress(int slot)
        {
            if(trafficClearSince[slot]<=0)trafficClearSince[slot]=Clock;
            if(Clock-trafficClearSince[slot]>=1||Vector3.ProjectOnPlane(trucks[slot].body.position-trafficEpisodeOrigin[slot],Vector3.up).magnitude>6)
            {trafficYieldStarts[slot]=0;trafficEpisodeOther[slot]=-1;trafficEpisodeOrigin[slot]=trucks[slot].body.position;}
        }
        bool RigPairYieldActive(int slot,int other)
        {
            for(int i=0;i<setup.count;i++)if(trafficYieldPhase[i]!=0&&(i==slot||i==other||trafficYieldTo[i]==slot||trafficYieldTo[i]==other))return true;return false;
        }
        bool BeginRigYield(int slot,int other)
        {
            if(other<0||Clock<trafficRetryAt[slot]||trucks[slot].body.linearVelocity.magnitude>.7f||RigPairYieldActive(slot,other))return false;
            // The lower-priority rig gets first choice. Its actual failed course
            // search permits its partner to make room, never concurrent pair moves.
            if(RigTrafficRank(slot)<RigTrafficRank(other)&&(trafficNoCourseOther[other]!=slot||Clock-trafficNoCourseAt[other]>2.1f))return false;
            if(trafficYieldStarts[slot]>=4){trafficNoCourseOther[slot]=other;trafficNoCourseAt[slot]=Clock;trafficYieldRejection[slot]="EPISODE_ATTEMPTS_EXHAUSTED";return false;}
            var truck=trucks[slot];var origin=truck.body.position;var forward=Vector3.ProjectOnPlane(truck.transform.forward,Vector3.up).normalized;var right=Vector3.Cross(Vector3.up,forward);string rejected="";
            for(int leg=0;leg<2;leg++)for(int side=0;side<2;side++)
            {
                float travel=leg==0?-1:1,sign=((slot+trafficYieldStarts[slot]+side)&1)==0?1:-1;var first=origin+forward*(travel*(leg==0?3:1.5f));var goal=origin+forward*(travel*5)+right*(sign*5.5f);var rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane((goal-first)*travel,Vector3.up));
                if(!RigYieldCorridor(slot,origin,first,truck.body.rotation,truck.body.rotation)||!RigYieldCorridor(slot,first,goal,truck.body.rotation,rotation)){rejected+=(leg==0?"REAR":"FORWARD")+sign+":"+trafficYieldRejection[slot]+";";continue;}
                trafficYieldOrigin[slot]=origin;trafficYieldRear[slot]=first;trafficYieldGoal[slot]=goal;trafficYieldTo[slot]=other;trafficYieldPhase[slot]=1;trafficYieldUntil[slot]=Clock+20;trafficYieldStarts[slot]++;trafficYieldBackwards[slot]=leg==0;trafficReverse[slot]=leg==0;trafficNoCourseOther[slot]=-1;trafficYieldRejection[slot]=null;routes[slot].Reset();return true;
            }
            trafficNoCourseOther[slot]=other;trafficNoCourseAt[slot]=Clock;trafficYieldRejection[slot]=rejected;trafficRetryAt[slot]=Clock+2;return false;
        }
        void StepRigYield(int slot)
        {
            var truck=trucks[slot];trafficHolding[slot]=true;trafficYieldDrive[slot]=Vector2.zero;
            if(Clock>=trafficYieldUntil[slot]||trafficYieldTo[slot]<0){trafficYieldPhase[slot]=0;trafficYieldTo[slot]=-1;trafficReverse[slot]=false;trafficRetryAt[slot]=Clock+2;routes[slot].Reset();return;}
            if(trafficYieldPhase[slot]==1&&Vector3.ProjectOnPlane(truck.body.position-trafficYieldOrigin[slot],Vector3.up).magnitude>=Mathf.Max(.2f,Vector3.Distance(trafficYieldOrigin[slot],trafficYieldRear[slot])-.2f))trafficYieldPhase[slot]=2;
            if(trafficYieldPhase[slot]==2&&Vector3.ProjectOnPlane(truck.body.position-trafficYieldGoal[slot],Vector3.up).magnitude<1.6f)trafficYieldPhase[slot]=3;
            if(trafficYieldPhase[slot]==3)
            {if(Vector3.ProjectOnPlane(truck.body.position-trucks[trafficYieldTo[slot]].body.position,Vector3.up).magnitude>12){ClearRigYield(slot);routes[slot].Reset();}return;}
            var target=trafficYieldPhase[slot]==1?trafficYieldRear[slot]:trafficYieldGoal[slot];var delta=Vector3.ProjectOnPlane(target-truck.body.position,Vector3.up);float angle=Vector3.SignedAngle(Vector3.ProjectOnPlane(truck.transform.forward,Vector3.up),delta,Vector3.up);
            if(trafficYieldPhase[slot]==1)trafficReverse[slot]=trafficYieldBackwards[slot];else if(Mathf.Abs(angle)>128)trafficReverse[slot]=true;else if(Mathf.Abs(angle)<82)trafficReverse[slot]=false;
            float steer=trafficYieldPhase[slot]==1?0:Mathf.Clamp((trafficReverse[slot]?Mathf.DeltaAngle(angle,180):angle)/28,-1,1)*(trafficReverse[slot]?-1:1),desired=Mathf.Min(1.5f,Mathf.Max(.55f,delta.magnitude*.6f)),along=Vector3.Dot(truck.body.linearVelocity,truck.transform.forward);
            var drive=new Vector2(steer,Mathf.Clamp(((trafficReverse[slot]?-desired:desired)-along)*.65f,-1,1));var next=truck.body.position+Vector3.ProjectOnPlane(truck.transform.forward,Vector3.up).normalized*(trafficReverse[slot]?-1:1)*1.5f;
            var turn=Quaternion.AngleAxis(Mathf.Clamp(truck.body.angularVelocity.y*Mathf.Rad2Deg*.35f+drive.x*8*Mathf.Sign(drive.y),-15,15),Vector3.up)*truck.body.rotation;
            if(!RigYieldCorridor(slot,truck.body.position,next,truck.body.rotation,turn))return;
            trafficHolding[slot]=false;trafficYieldDrive[slot]=drive;routes[slot].DirectGoal(truck.body.position,target);
        }
        bool RigTrafficRouteHeld(int slot)
        {
            if(trafficYieldPhase[slot]!=0){StepRigYield(slot);return true;}
            var attempt=trafficHolding[slot]?trafficAttempt[slot]:Vector2.zero;
            if(!RigTrafficConflict(slot,attempt,out var other))
            {if(trafficHolding[slot])routes[slot].Reset();trafficHolding[slot]=false;trafficBlockedSince[slot]=0;RigClearProgress(slot);return false;}
            RigConflictEpisode(slot,other);trafficHolding[slot]=true;trafficBlocker[slot]=other;if(trafficBlockedSince[slot]<=0)trafficBlockedSince[slot]=Clock;
            if(Clock-trafficBlockedSince[slot]>=.4f&&BeginRigYield(slot,other)){StepRigYield(slot);}return true;
        }
        Vector2 FilterRigTraffic(int slot,Vector2 drive)
        {
            if(trafficYieldPhase[slot]!=0)return trafficYieldDrive[slot];trafficAttempt[slot]=drive;
            if(RigTrafficConflict(slot,drive,out var other)){RigConflictEpisode(slot,other);trafficHolding[slot]=true;trafficBlocker[slot]=other;if(trafficBlockedSince[slot]<=0)trafficBlockedSince[slot]=Clock;return Vector2.zero;}
            trafficHolding[slot]=false;trafficBlockedSince[slot]=0;RigClearProgress(slot);return drive;
        }

        void ResetTreasureAccess()
        {
            Array.Clear(treasureAccessUntil,0,treasureAccessUntil.Length);
            Array.Clear(treasureAccessValid,0,treasureAccessValid.Length);
            Array.Clear(treasureAccessReason,0,treasureAccessReason.Length);treasureAccessChecks=0;ResetRigTraffic();
            Array.Clear(roadOriginUntil,0,4);Array.Clear(roadOriginLostSeconds,0,4);
            Array.Clear(roadOriginValid,0,4);Array.Clear(roadOriginHolding,0,4);Array.Clear(roadOriginReason,0,4);
        }
        bool SupportedRoadOrigin(int slot)
        {
            var truck=trucks[slot];var origin=truck.body.position;
            if(roadOriginReason[slot]!=null&&Clock<roadOriginUntil[slot]&&(origin-roadOriginPosition[slot]).sqrMagnitude<1)return roadOriginValid[slot];
            roadOriginUntil[slot]=Clock+.75f;roadOriginPosition[slot]=origin;roadOriginValid[slot]=false;
            roadOriginRoadPoint[slot]=origin;roadOriginReason[slot]="NO SUPPORTED ROAD ENTRY";
            if(!LwsRoadGraphQuery.TryFindNearestRoad(liveGraph,origin,150,out var road))return false;
            roadOriginRoadPoint[slot]=road.NearestPosition;
            var leg=road.NearestPosition-origin;int samples=Mathf.Max(1,Mathf.CeilToInt(leg.magnitude/2));
            if(samples>80)return false;
            var heading=Vector3.ProjectOnPlane(leg,Vector3.up);
            var rotation=heading.sqrMagnitude>.01f?Quaternion.LookRotation(heading.normalized):truck.body.rotation;
            var half=Vector3.Scale(truck.shape.size*.5f,truck.transform.lossyScale);
            var center=Vector3.Scale(truck.shape.center,truck.transform.lossyScale);center.y=0;
            // Test the actual finite chassis footprint along the connector.
            // Nearby traffic/cargo remains the live driver's responsibility;
            // it must not be mistaken for missing terrain and cause recovery.
            for(int step=0;step<=samples;step++)
            {
                var point=Vector3.Lerp(origin,road.NearestPosition,step/(float)samples);
                if(!TryFindSupportedSurface(point,out _,out _,out _))return false;
                for(int side=-1;side<=1;side+=2)for(int end=-1;end<=1;end+=2)
                    if(!TryFindSupportedSurface(point+rotation*(center+new Vector3(side*half.x,0,end*half.z)),out _,out _,out _))return false;
            }
            roadOriginValid[slot]=true;roadOriginReason[slot]="ACTUAL SUPPORTED TRUCK TO ROAD FOOTING";return true;
        }
        protected override bool CanFollowRoute(int slot,float dt)
        {
            if(setup.players[slot].source!=BowlingSource.AI){ClearRigYield(slot);return true;}
            // Intentional crane holds are not failed driving progress. Do not
            // run the road driver's stall/reverse timer against our handbrake.
            if(HoistBusy(slot)){ClearRigYield(slot);return false;}
            if(SupportedRoadOrigin(slot))
            {
                roadOriginLostSeconds[slot]=0;
                if(roadOriginHolding[slot]){routes[slot].Reset();roadOriginHolding[slot]=false;}
                return !RigTrafficRouteHeld(slot);
            }
            ClearRigYield(slot);
            if(!roadOriginHolding[slot]){routes[slot].Reset();roadOriginHolding[slot]=true;}
            roadOriginLostSeconds[slot]+=dt;
            // Missing footing is independent of movable traffic. A grounded
            // stationary crew otherwise holds forever beside an unsupported
            // connector. Reuse delayed last-safe-road recovery, which releases
            // cargo first and retains its clearance check and two-use limit.
            // Airborne/moving crews still require the original actual drop.
            bool stranded=trucks[slot].tractorMotor.Grounded&&trucks[slot].body.linearVelocity.magnitude<.5f;
            if(roadOriginLostSeconds[slot]>2&&(TruckBelowRoad(slot)||stranded))
                BeginRecovery(slot,TruckBelowRoad(slot)?"OFF ROAD DROP":"NO SUPPORTED ROAD ENTRY");
            return false;
        }
        bool TruckBelowRoad(int slot)=>trucks[slot].body.position.y+8<roadOriginRoadPoint[slot].y;
        bool CanReachTreasure(int slot,int index)
        {
            // Origin evidence is checked before the independent cargo cache.
            // A still-reachable treasure cannot validate an impossible climb
            // from the truck's new position below its authored road.
            if(!SupportedRoadOrigin(slot))return false;
            var item=treasure[index];var position=item.body.position;
            if(Clock<treasureAccessUntil[slot,index]&&(position-treasureAccessPosition[slot,index]).sqrMagnitude<1)return treasureAccessValid[slot,index];
            treasureAccessUntil[slot,index]=Clock+3;treasureAccessPosition[slot,index]=position;
            treasureAccessValid[slot,index]=false;treasureAccessReason[slot,index]="NO SUPPORTED FINAL ACCESS";treasureAccessChecks++;
            if(!LwsRoadGraphQuery.TryFindNearestRoad(liveGraph,position,150,out var road))
            {treasureAccessReason[slot,index]="NO AUTHORED ROAD ACCESS";return false;}
            Array.Clear(accessAttachedLoads,0,accessAttachedLoads.Length);int attached=0;
            foreach(var load in treasure)if(load.CargoJoint!=null&&load.Carrier>=0&&attached<accessAttachedLoads.Length)accessAttachedLoads[attached++]=load.shape;
            var towardRoad=Vector3.ProjectOnPlane(road.NearestPosition-position,Vector3.up);
            if(towardRoad.sqrMagnitude<1)towardRoad=Vector3.ProjectOnPlane(trucks[slot].body.position-position,Vector3.up);
            if(towardRoad.sqrMagnitude<.01f)towardRoad=-trucks[slot].transform.forward;
            towardRoad.Normalize();float radius=Mathf.Max(3,pickupReach-3);
            for(int attempt=0;attempt<8;attempt++)
            {
                var direction=Quaternion.AngleAxis(attempt*45,Vector3.up)*towardRoad;
                var parking=position+direction*radius;parking.y=item.shape.bounds.min.y;
                var forward=Vector3.ProjectOnPlane(position-parking,Vector3.up);var finalRotation=Quaternion.LookRotation(forward.normalized);
                if(!TryGroundedPose(slot,parking,finalRotation,out var final,true,accessAttachedLoads)||Vector3.Distance(final.position,position)>=pickupReach-.5f)continue;
                var leg=final.position-road.NearestPosition;int samples=Mathf.Max(1,Mathf.CeilToInt(leg.magnitude/2));
                if(samples>80)continue;
                var heading=Vector3.ProjectOnPlane(leg,Vector3.up);var entryRotation=heading.sqrMagnitude>.01f?Quaternion.LookRotation(heading.normalized):finalRotation;
                bool supported=true;
                for(int step=0;step<=samples;step++)
                {
                    float fraction=step/(float)samples;var point=Vector3.Lerp(road.NearestPosition,final.position,fraction);
                    if(!TryGroundedPose(slot,point,Quaternion.Slerp(entryRotation,finalRotation,fraction),out _,true,accessAttachedLoads)){supported=false;break;}
                }
                if(!supported)continue;
                treasureAccessValid[slot,index]=true;treasureAccessParking[slot,index]=final.position;treasureAccessReason[slot,index]="ACTUAL FULL-TRUCK SUPPORTED ROAD CONNECTOR";return true;
            }
            return false;
        }
    }
}
