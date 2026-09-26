#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Opt-in test fixture shared by PlayMode tests and the development EXE smoke test.
    // Stages existing UTS pedestrians, then drives the actual NWH body into them.
    public static class TruckTaxiPedestrianRuntimeProbe
    {
        public static IEnumerator Run(TruckTaxiBootstrap host,Action<bool,string> check,Action<string> capture)
        {
            host.Session.EndShift(); host.StartShift();
            var profile=UnityEngine.Object.Instantiate(host.configuration.passengers[0]);
            var request=ScriptableObject.CreateInstance<PassengerRequestDefinition>();
            request.requestType=TaxiRequestType.HitPedestrian; request.target=2; request.timer=180;
            profile.possibleRequests=new[]{request}; profile.requestDifficultyRange=Vector2.one;
            profile.uniqueMechanics=Array.Empty<TruckTaxiMechanic>(); profile.chaosAffinity=1;
            profile.basePatience=300; profile.requestFrequency=300;
            check(host.Session.OfferRide(profile) && host.Session.AcceptRide(),"Test ride accepted");
            host.TeleportNear(host.Session.Pickup);
            var truck=host.Player.GetComponent<Rigidbody>(); truck.isKinematic=true;
            float deadline=Time.realtimeSinceStartup+10;
            while(!host.Session.HasPassenger && Time.realtimeSinceStartup<deadline) yield return null;
            check(host.Session.HasPassenger,"Passenger boarded using current ride flow");
            if(!host.Session.HasPassenger) yield break;
            check(host.Session.Requests.Count==1 && host.Session.Requests[0].Description=="Hit 2 pedestrians","Natural objective displayed");
            int count=host.pedestrians.ActiveCount, hitEvents=0, contextEvents=0;
            Action<TaxiEventType> eventListener=type=> { if(type==TaxiEventType.PedestrianHit) hitEvents++; };
            Action<TruckTaxiPedestrianImpact> contextListener=hit=>contextEvents++;
            host.Session.DrivingEvent+=eventListener; host.Session.PedestrianImpact+=contextListener;
            var retired=new TruckTaxiPedestrian[2];
            var ids=new string[2];
            float priorImpulse=0;
            var staging=new GameObject("Pedestrian test staging (opt-in only)").AddComponent<TruckTaxiRideLocation>();
            try
            {
                for(int run=0;run<2;run++)
                {
                    staging.transform.position=new Vector3(-160,.15f,-250+run*30);
                    truck.isKinematic=false; host.TeleportNear(staging);
                    yield return new WaitForSeconds(1);
                    var ped=host.pedestrians.People.First(p=>p!=null && !p.IsRagdoll);
                    check(ped.HasJointedRagdoll,"Existing UTS pedestrian has jointed ragdoll");
                    check(!ped.TryStrike(Vector3.forward*.5f,ped.transform.position),"Below-threshold contact leaves pedestrian walking");
                    retired[run]=ped; ids[run]=ped.PedestrianId;
                    float front=truck.position.z;
                    foreach(var col in truck.GetComponentsInChildren<Collider>())
                        if(col.enabled && !col.isTrigger && col.attachedRigidbody==truck) front=Mathf.Max(front,col.bounds.max.z);
                    var position=new Vector3(truck.position.x,0,front+2.5f);
                    if(Physics.Raycast(position+Vector3.up*20,Vector3.down,out var ground,40,~0,QueryTriggerInteraction.Ignore)) position.y=ground.point.y+.05f;
                    var walking=ped.GetComponent<Rigidbody>();
                    walking.position=position; walking.rotation=Quaternion.LookRotation(Vector3.left);
                    if(!walking.isKinematic) { walking.linearVelocity=Vector3.zero; walking.angularVelocity=Vector3.zero; }
                    ped.transform.SetPositionAndRotation(position,walking.rotation); Physics.SyncTransforms();
                    host.Session.DiscardTeleportDistance(); host.Passengers.Dialogue.Stop();
                    int chaos=host.Session.ChaosScore, spoken=host.Passengers.Dialogue.SpokenLines;
                    float satisfaction=host.Session.Satisfaction;
                    var firstBone=ped.RagdollBodies[0]; Vector3 boneBefore=firstBone.position;
                    capture?.Invoke("Pedestrian_Before_"+run);
                    float speed=run==0 ? 4 : 12;
                    deadline=Time.realtimeSinceStartup+4;
                    while(!ped.IsRagdoll && Time.realtimeSinceStartup<deadline)
                    {
                        truck.linearVelocity=Vector3.forward*speed;
                        yield return new WaitForFixedUpdate();
                    }
                    truck.linearVelocity=Vector3.zero; truck.angularVelocity=Vector3.zero; truck.isKinematic=true;
                    check(ped.IsRagdoll,"Real NWH tractor contact activated UTS ragdoll at "+speed+" m/s");
                    if(!ped.IsRagdoll) yield break;
                    check(ped.HitEventSent,"Collision emitted authoritative pedestrian hit");
                    check(hitEvents==run+1 && contextEvents==run+1,"Exactly one hit event and one context event per person");
                    check(host.Session.Requests[0].Progress==run+1,"Objective advanced exactly once");
                    check(host.Session.ChaosScore==chaos+host.configuration.pedestrianHitScore,"Exactly one Chaos award");
                    check(host.Session.Satisfaction>satisfaction,"Existing chaos-loving passenger preference reacted");
                    check(host.Passengers.Dialogue.SpokenLines>spoken,"Existing passenger dialogue/reaction pipeline responded");
                    check(ped.LastImpulse.magnitude>priorImpulse,"Higher-speed impact produces stronger launch");
                    priorImpulse=ped.LastImpulse.magnitude;
                    check(!ped.GetComponent<Animator>().enabled,"Animator released ragdoll bones");
                    foreach(var behaviour in ped.GetComponents<Behaviour>())
                        if(behaviour.GetType().Name=="Passersby" || behaviour.GetType().Name=="MovePath") check(!behaviour.enabled,"UTS movement stopped");
                    yield return new WaitForSeconds(1.2f);
                    check(Vector3.Distance(firstBone.position,boneBefore)>.1f,"Jointed bones physically moved after impact");
                    foreach(var bone in ped.RagdollBodies)
                        check(!bone.isKinematic && TruckTaxiPedestrianImpactSettings.Finite(bone.linearVelocity) && bone.linearVelocity.magnitude<40,"Dynamic bone velocity is finite and bounded");
                    check(!ped.TryStrike(Vector3.forward*30,position),"Residual contact cannot retrigger ragdoll");
                    check(hitEvents==run+1 && host.Session.ChaosScore==chaos+host.configuration.pedestrianHitScore,"Residual bone contacts did not duplicate scoring");
                    Debug.Log($"TAXI PEDESTRIAN CONTACT: {ped.PedestrianId} speed={ped.LastImpactSpeed:F2} impulse={ped.LastImpulse.magnitude:F2} bones={ped.RagdollBodies.Count} progress={host.Session.Requests[0].Progress} chaos={host.Session.ChaosScore} reaction={host.Passengers.Dialogue.Subtitle}");
                    capture?.Invoke("Pedestrian_Ragdoll_"+run);
                }
                check(host.Session.Requests[0].State==TaxiRequestState.Succeeded,"Two real impacts completed the two-person objective");
                deadline=Time.realtimeSinceStartup+host.configuration.pedestrianImpact.ragdollLifetime+3;
                while(retired.Any(p=>p!=null) && Time.realtimeSinceStartup<deadline) yield return null;
                check(retired.All(p=>p==null),"Ragdolls cleaned up after configured lifetime");
                check(host.pedestrians.ActiveCount==count,"Same UTS population replenished without accumulation");
                check(host.pedestrians.People.All(p=>p!=null && !p.IsRagdoll && !p.HitEventSent && !ids.Contains(p.PedestrianId)),"Replacement bodies have fresh IDs, animation and hit guards");
                check(host.pedestrians.People.All(p=>p.GetComponent<Animator>().enabled && p.HasJointedRagdoll),"Replacement UTS rigs initialized normally");
                capture?.Invoke("Pedestrian_Cleanup");
                Debug.Log("TAXI PEDESTRIAN PROBE COMPLETE: two real tractor impacts, UTS bone physics, event/score/objective/reaction and cleanup.");
            }
            finally
            {
                host.Session.DrivingEvent-=eventListener; host.Session.PedestrianImpact-=contextListener;
                host.Session.EndShift(); truck.isKinematic=false;
                UnityEngine.Object.Destroy(staging.gameObject); UnityEngine.Object.Destroy(profile); UnityEngine.Object.Destroy(request);
            }
        }
    }
}
#endif
