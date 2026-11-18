using UnityEngine;
namespace LWS.TruckHorrorSports
{
    /// <summary>One physical supply parcel per participant over the existing foot/input owner.</summary>
    public sealed class HorrorSupplyJob : MonoBehaviour
    {
        public HorrorMatch match;
        public int objectiveIndex;
        public Rigidbody[] parcels, carryingAnchors;
        public BoxCollider[] receivers;
        public HorrorFootAgent[] carriers;
        public Vector3 truckApproach;
        readonly bool[] delivered = new bool[12], depositing = new bool[12];
        readonly float[] rest = new float[12], depositAge = new float[12];
        readonly int[] cycles = new int[12];
        SpringJoint[] grips;
        readonly RaycastHit[] supportHits = new RaycastHit[8], approachHits = new RaycastHit[12];
        Vector3[] starts;
        Quaternion[] rotations;
        Vector3[] anchorVelocity;
        Collider[] parcelShapes;
        const float ShelfReach = 1.8f;
        public Vector3 TruckApproach(int slot) => truckApproach + Vector3.right * ((slot - 1.5f) * 4);
        public bool Delivered(int slot) => slot >= 0 && slot < parcels.Length && delivered[slot];
        public bool Carrying(int slot) => grips != null && slot >= 0 && slot < grips.Length && grips[slot] != null;
        public Vector3 Target(int slot)
        {
            if(!Carrying(slot)&&!depositing[slot]&&!delivered[slot])return parcels[slot].position;
            // Approach the front of the real tray, never its obstructed underside.
            var receiver=receivers[slot];Vector3 point=receiver.bounds.center-receiver.transform.forward*(receiver.size.z*.5f+.65f);
            point.y=carriers[slot].transform.position.y;return point;
        }
        public bool TryCabExit(int slot,Vector3 taskTarget,out Vector3 exitPosition)
        {
            var truck=match.trucks[slot];var controller=carriers[slot].controller;
            float toward=Vector3.Dot(Vector3.ProjectOnPlane(taskTarget-truck.body.position,Vector3.up),truck.transform.right)>=0?1:-1;
            var scale=controller.transform.lossyScale;float radius=controller.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z));float half=Mathf.Max(radius,controller.height*Mathf.Abs(scale.y)*.5f);
            Quaternion rotation=truck.transform.rotation;Vector3 axis=rotation*Vector3.up;
            for(int side=0;side<2;side++)
            {
                // This is the existing cab transition, choosing a real clear door on
                // the task's side instead of routing a character through the cab.
                Vector3 candidate=truck.body.position+truck.transform.right*(toward*(side==0?1:-1)*2.2f)+Vector3.up*.5f;
                Vector3 center=candidate+rotation*controller.center;
                if(!Physics.CheckCapsule(center+axis*(half-radius),center-axis*(half-radius),radius,~0,QueryTriggerInteraction.Ignore))
                {exitPosition=candidate;return true;}
            }
            exitPosition=Vector3.zero;return false;
        }
        public Vector2 FootCommand(int slot,Vector3 navigationTarget,bool refineToSupply=true)
        {
            var foot=carriers[slot];Vector3 interaction=refineToSupply?Target(slot):navigationTarget;
            if (refineToSupply && Carrying(slot))
            {
                // At the real hand-off distance, face the shelf and stop. Continuing
                // to face the approach waypoint can turn the parcel back into the aisle.
                Vector3 shelf = Vector3.ProjectOnPlane(receivers[slot].bounds.center - foot.transform.position, Vector3.up);
                if (shelf.sqrMagnitude < ShelfReach * ShelfReach)
                {
                    Vector3 facing = foot.transform.InverseTransformDirection(shelf);
                    float turn = Mathf.Atan2(facing.x, facing.z) * Mathf.Rad2Deg;
                    return new Vector2(Mathf.Clamp(turn / 45, -1, 1), 0);
                }
            }
            // Refine the last few metres around real parcels and shelving.
            // The baked route excludes moving rigs, so it may also lead through
            // a parked cab after a driver exits its opposite door.
            if (refineToSupply && Vector3.ProjectOnPlane(interaction - foot.transform.position, Vector3.up).sqrMagnitude < 16
                && ClearFootPath(slot, foot, interaction, out _))
                navigationTarget = interaction;
            if (!ClearFootPath(slot, foot, navigationTarget, out var blocker) && blocker != null
                && blocker.transform.IsChildOf(match.trucks[slot].transform))
                navigationTarget = CabBypass(slot, foot, blocker, navigationTarget);
            Vector3 delta=Vector3.ProjectOnPlane(navigationTarget-foot.transform.position,Vector3.up);
            Vector3 local=foot.transform.InverseTransformDirection(delta);float angle=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;
            // The existing CharacterController turns in place before stepping; it must
            // not run into/through shelving while turning toward a dropped parcel.
            return new Vector2(Mathf.Clamp(angle/45,-1,1),delta.magnitude>.4f?Mathf.Clamp01((delta.magnitude-.25f)/.8f)*Mathf.Clamp01(1-Mathf.Abs(angle)/60):0);
        }
        bool ClearFootPath(int slot, HorrorFootAgent foot, Vector3 destination, out Collider blocker)
        {
            blocker = null;
            Vector3 step = Vector3.ProjectOnPlane(destination - foot.transform.position, Vector3.up);
            if (step.sqrMagnitude < .001f) return true;
            Bounds capsule = foot.controller.bounds;
            float radius = foot.controller.radius * .95f;
            float shaft = Mathf.Max(0, capsule.extents.y - radius);
            int count = Physics.CapsuleCastNonAlloc(capsule.center + Vector3.up * shaft, capsule.center - Vector3.up * shaft,
                radius, step.normalized, approachHits, Mathf.Min(step.magnitude, 5), ~0, QueryTriggerInteraction.Ignore);
            float first = float.PositiveInfinity;
            for (int index = 0; index < count; index++)
            {
                var hit = approachHits[index];
                if (hit.rigidbody == parcels[slot] || hit.collider.transform.IsChildOf(foot.transform) || hit.normal.y >= .7f || hit.distance >= first)
                    continue;
                blocker = hit.collider;
                first = hit.distance;
            }
            return blocker == null && count < approachHits.Length;
        }

        Vector3 CabBypass(int slot, HorrorFootAgent foot, Collider blocker, Vector3 destination)
        {
            var truck = match.trucks[slot];
            Bounds box = truck.shape != null && truck.shape.enabled ? truck.shape.bounds : blocker.bounds;
            box.Encapsulate(blocker.bounds);
            Vector3 forward = Vector3.ProjectOnPlane(truck.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 relative = foot.transform.position - box.center;
            float side = Mathf.Sign(Vector3.Dot(relative, right));
            float halfWidth = Mathf.Abs(right.x) * box.extents.x + Mathf.Abs(right.z) * box.extents.z;
            float halfLength = Mathf.Abs(forward.x) * box.extents.x + Mathf.Abs(forward.z) * box.extents.z;
            Vector3 outside = box.center + right * (side * (halfWidth + .8f));
            outside.y = foot.transform.position.y;
            Vector3 front = outside + forward * (halfLength + 1.25f);
            Vector3 rear = outside - forward * (halfLength + 1.25f);
            bool frontClear = ClearFootPath(slot, foot, front, out _);
            bool rearClear = ClearFootPath(slot, foot, rear, out _);
            if (!frontClear && !rearClear)
                return outside + forward * Vector3.Dot(relative, forward);
            float frontCost = Vector3.Distance(foot.transform.position, front) + Vector3.Distance(destination, front);
            float rearCost = Vector3.Distance(foot.transform.position, rear) + Vector3.Distance(destination, rear);
            return frontClear && (!rearClear || frontCost < rearCost) ? front : rear;
        }

        public string Instruction(int slot) => delivered[slot] ? "SUPPLIES RESTOCKED" : depositing[slot] ? "LET THE PARCEL SETTLE ON THE SHELF" : Carrying(slot) ? "CARRY TO YOUR SHELF · HOLD ACTION TO PLACE" : "ON FOOT · HOLD ACTION BESIDE YOUR SUPPLY PARCEL";
        public void ResetJobs()
        {
            if(starts==null)
            {
                starts=new Vector3[parcels.Length];rotations=new Quaternion[parcels.Length];grips=new SpringJoint[parcels.Length];anchorVelocity=new Vector3[parcels.Length];parcelShapes=new Collider[parcels.Length];
                for(int i=0;i<parcels.Length;i++){starts[i]=parcels[i].position;rotations[i]=parcels[i].rotation;parcelShapes[i]=parcels[i].GetComponent<Collider>();}
            }
            for(int i=0;i<parcels.Length;i++)
            {
                cycles[i]=0;ResetParcel(i);
                if(i<match.setup.count)
                {
                    var block=new MaterialPropertyBlock();Color color=Color.Lerp(Color.white,match.setup.players[i].color,.65f);
                    block.SetColor("_BaseColor",color);block.SetColor("_Color",color);
                    foreach(var renderer in parcels[i].GetComponentsInChildren<Renderer>(true))renderer.SetPropertyBlock(block);
                    foreach(var renderer in receivers[i].GetComponentsInChildren<Renderer>(true))renderer.SetPropertyBlock(block);
                }
            }
        }
        void ResetParcel(int slot)
        {
            if(grips[slot]!=null)Destroy(grips[slot]);grips[slot]=null;
            var body=parcels[slot];body.isKinematic=false;body.position=starts[slot];body.rotation=rotations[slot];body.linearVelocity=body.angularVelocity=Vector3.zero;
            delivered[slot]=depositing[slot]=false;rest[slot]=depositAge[slot]=0;anchorVelocity[slot]=Vector3.zero;
        }
        bool SupportedByReceiver(Rigidbody parcel,BoxCollider receiver,Vector3 center)
        {
            int count=Physics.RaycastNonAlloc(center+Vector3.up*.5f,Vector3.down,supportHits,1.8f,1,QueryTriggerInteraction.Ignore);
            if(count==supportHits.Length)return false;
            Collider nearest=null;float distance=float.MaxValue;
            for(int hit=0;hit<count;hit++)
            {
                var shape=supportHits[hit].collider;
                // Parcels share scene collision layers with the shelf. Their own hull must
                // never hide the actual supporting receiver or masquerade as that support.
                if(shape==null||shape.attachedRigidbody==parcel||shape.transform.IsChildOf(parcel.transform))continue;
                if(supportHits[hit].distance<distance){nearest=shape;distance=supportHits[hit].distance;}
            }
            return nearest==receiver;
        }
        public string Diagnostic(int slot)
        {
            if(slot<0 || slot>=parcels.Length)return "invalid supply participant";
            var foot=carriers[slot];var body=parcels[slot];var destination=Target(slot);
            string route="no foot body";
            if(foot!=null)
            {
                bool a=UnityEngine.AI.NavMesh.SamplePosition(foot.transform.position,out var origin,8,UnityEngine.AI.NavMesh.AllAreas);
                bool b=UnityEngine.AI.NavMesh.SamplePosition(destination,out var goal,8,UnityEngine.AI.NavMesh.AllAreas);
                if(a&&b){var path=new UnityEngine.AI.NavMeshPath();UnityEngine.AI.NavMesh.CalculatePath(origin.position,goal.position,UnityEngine.AI.NavMesh.AllAreas,path);route=path.status+" projectedGoal="+goal.position.ToString("F2")+" corners="+string.Join(" / ",System.Array.ConvertAll(path.corners,c=>c.ToString("F2")));}
                else route="sample origin="+a+" goal="+b;
            }
            return "supply P"+(slot+1)+" outside="+(foot!=null&&foot.Outside)+" active="+(foot!=null&&foot.gameObject.activeInHierarchy)+" controller="+(foot!=null&&foot.controller.enabled)+" grounded="+(foot!=null&&foot.controller.isGrounded)+" foot="+(foot!=null?foot.transform.position.ToString("F2"):"none")+" forward="+(foot!=null?foot.transform.forward.ToString("F2"):"none")+" pickupDistance="+(foot!=null?Vector3.Distance(foot.transform.position,body.position).ToString("F2"):"none")+" target="+destination.ToString("F2")+" parcel="+body.position.ToString("F2")+" carrying="+Carrying(slot)+" depositing="+depositing[slot]+" rest="+rest[slot].ToString("F2")+" delivered="+delivered[slot]+" nav="+route;
        }
        public void FreezeJobs()
        {
            if(grips!=null)for(int i=0;i<grips.Length;i++){if(grips[i]!=null)Destroy(grips[i]);grips[i]=null;}
            foreach(var parcel in parcels)if(parcel!=null)parcel.isKinematic=true;
        }
        // Called by the existing match's fixed simulation, with that participant's actual ACTION.
        public void TickParticipant(int slot,bool held,float dt)
        {
            if(match==null||!match.Live||slot<0||slot>=match.setup.count||slot>=parcels.Length||grips==null)return;
            int cycle=match.rules.taskCycles[slot];
            if(cycle!=cycles[slot]){cycles[slot]=cycle;ResetParcel(slot);}
            if(delivered[slot]||match.rules.outcome[slot]!=HorrorOutcome.Active)return;
            var parcel=parcels[slot];var receiver=receivers[slot];
            if(depositing[slot])
            {
                depositAge[slot]+=dt;
                Vector3 point=parcel.worldCenterOfMass;Bounds area=receiver.bounds;
                bool above=Mathf.Abs(point.x-area.center.x)<area.extents.x&&Mathf.Abs(point.z-area.center.z)<area.extents.z&&point.y>area.max.y-.1f&&point.y<area.max.y+1;
                bool support=SupportedByReceiver(parcel,receiver,point);
                rest[slot]=above&&support&&parcel.linearVelocity.magnitude<.5f&&parcel.angularVelocity.magnitude<.7f?rest[slot]+dt:0;
                if(rest[slot]>=.7f)delivered[slot]=true;
                else if(depositAge[slot]>3&&!above)depositing[slot]=false;
                return;
            }
            var foot=carriers[slot];
            if(Carrying(slot))
            {
                if(!foot.Outside){Destroy(grips[slot]);grips[slot]=null;return;}
                // The vendor controller's root is at capsule center, not at its feet.
                // Carry at actual hand/waist height; the former +1.6m target was above
                // the character's head and pulled the real breakable grip violently.
                Vector3 hand=foot.transform.position+foot.transform.forward*.9f+Vector3.up*.65f;
                bool placing=held&&Vector3.ProjectOnPlane(foot.transform.position-receiver.bounds.center,Vector3.up).magnitude<ShelfReach
                    &&Vector3.Dot(foot.transform.forward,Vector3.ProjectOnPlane(receiver.bounds.center-foot.transform.position,Vector3.up).normalized)>.6f;
                if(placing)hand=new Vector3(receiver.bounds.center.x,receiver.bounds.max.y+parcelShapes[slot].bounds.extents.y+.15f,receiver.bounds.center.z);
                Vector3 requested=Vector3.ClampMagnitude((hand-carryingAnchors[slot].position)*4,5);
                anchorVelocity[slot]=Vector3.MoveTowards(anchorVelocity[slot],requested,dt*6);
                carryingAnchors[slot].MovePosition(carryingAnchors[slot].position+anchorVelocity[slot]*dt);
                if(placing&&Vector3.ProjectOnPlane(parcel.worldCenterOfMass-receiver.bounds.center,Vector3.up).magnitude<.8f
                    &&parcelShapes[slot].bounds.min.y>=receiver.bounds.max.y-.025f&&parcel.linearVelocity.magnitude<.6f)
                {Destroy(grips[slot]);grips[slot]=null;depositing[slot]=true;depositAge[slot]=rest[slot]=0;}
            }
            else if(held&&foot.Outside&&Vector3.Distance(foot.transform.position,parcel.position)<1.9f&&match.NextObjective(slot)==objectiveIndex)
            {
                carryingAnchors[slot].position=parcel.worldCenterOfMass;anchorVelocity[slot]=Vector3.zero;
                var grip=parcel.gameObject.AddComponent<SpringJoint>();grip.connectedBody=carryingAnchors[slot];grip.autoConfigureConnectedAnchor=false;
                grip.anchor=parcel.transform.InverseTransformPoint(parcel.worldCenterOfMass);grip.connectedAnchor=Vector3.zero;grip.spring=900;grip.damper=150;grip.maxDistance=.03f;grip.breakForce=650;grip.enableCollision=false;
                grips[slot]=grip;
            }
        }
    }
}
