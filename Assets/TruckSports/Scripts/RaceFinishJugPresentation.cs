using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckSports
{
    // Three native PhysX prop slots, isolated from every gameplay body. No Taxi blast
    // coordinator, vehicle motor, score callback or default-scene collision change.
    public sealed class RaceFinishJugPresentation:MonoBehaviour
    {
        [Serializable] public sealed class Slot
        {
            public Rigidbody jug,proxy;public Collider jugCollider;public BoxCollider hull,floor;
            public Transform ghost,actorRoot;public Animator actor;public ParticleSystem splash;public AudioSource impact;
            [NonSerialized] public bool busy,launched;[NonSerialized] public SportsRaceFinishReceipt receipt;
            [NonSerialized] public Sequence sequence;[NonSerialized] public Mesh[] baked;[NonSerialized]public bool staged;[NonSerialized]public Vector3 stagingPosition;[NonSerialized]public Quaternion stagingRotation;
        }
        public SportsRaceFinishSource source;public Transform poolRoot;public Slot[] slots;
        Scene scene;PhysicsScene physics;float accumulator;bool disposed,paused;
        readonly Queue<SportsRaceFinishReceipt> waiting=new Queue<SportsRaceFinishReceipt>();
        readonly HashSet<string> accepted=new HashSet<string>();
        public int Throws{get;private set;}public int Contacts{get;private set;}public int HullContacts{get;private set;}public int Approaches{get;private set;}
        public int Queued=>waiting.Count;public bool Isolated=>scene.IsValid()&&scene.IsAValidPhysicsScene();
        public int LiveJugs{get{int count=0;if(slots!=null)foreach(var slot in slots)if(slot.launched&&slot.jug.gameObject.activeSelf)count++;return count;}}
        void EnsureScene()
        {
            if(scene.IsValid())return;
            if(poolRoot==null||slots==null||slots.Length!=3)throw new InvalidOperationException("Author the three isolated finish jug/proxy slots before play.");
            scene=SceneManager.CreateScene("Race finish prop isolation "+Guid.NewGuid().ToString("N"),new CreateSceneParameters(LocalPhysicsMode.Physics3D));physics=scene.GetPhysicsScene();
            if(!physics.IsValid()||physics==Physics.defaultPhysicsScene)throw new InvalidOperationException("Finish props require a distinct local Physics3D scene; the gameplay world must never be stepped here.");
            poolRoot.SetParent(null,true);SceneManager.MoveGameObjectToScene(poolRoot.gameObject,scene);
        }
        public void Enqueue(SportsRaceFinishReceipt receipt)
        {
            if(disposed||!receipt.gag||receipt.winner||receipt.truck==null||!float.IsFinite(receipt.seconds)||source==null||accepted.Contains(receipt.Key)||!source.IsConfirmedLoser(receipt))return;
            if(waiting.Count<24){accepted.Add(receipt.Key);waiting.Enqueue(receipt);}
        }
        public void Tick(float delta,bool pause)
        {
            if(disposed)return;
            if(paused!=pause){paused=pause;if(slots!=null)foreach(var slot in slots){if(slot.sequence!=null){if(pause)slot.sequence.Pause();else slot.sequence.Play();}if(slot.actor!=null)slot.actor.speed=pause?0:1;if(slot.impact!=null){if(pause)slot.impact.Pause();else slot.impact.UnPause();}if(slot.splash!=null&&slot.splash.IsAlive(true)){if(pause)slot.splash.Pause(true);else slot.splash.Play(true);}}}
            if(pause)return;
            if(waiting.Count>0){EnsureScene();foreach(var slot in slots)if(!slot.busy&&waiting.Count>0)Begin(slot,waiting.Dequeue());}
            // A default-initialized PhysicsScene can be the valid default world.
            // Until our explicitly local scene exists there is nothing to step;
            // never simulate gameplay or change its global simulation mode.
            if(!scene.IsValid()||!physics.IsValid()||physics==Physics.defaultPhysicsScene)return;
            foreach(var slot in slots)if(slot.busy){FollowConfirmedHull(slot);if(!slot.launched){var hand=Hand(slot);slot.jug.position=hand!=null?hand.position:slot.actorRoot.position+Vector3.up*1.45f;slot.jug.rotation=hand!=null?hand.rotation:slot.actorRoot.rotation;}if(slot.impact!=null)slot.impact.volume=source.session.settings.sfx*.3f;}
            accumulator+=Mathf.Clamp(delta,0,.1f);int steps=0;
            while(accumulator>=.02f&&steps++<5){physics.Simulate(.02f);accumulator-=.02f;}
        }
        void Begin(Slot slot,SportsRaceFinishReceipt receipt)
        {
            slot.busy=true;slot.launched=false;slot.receipt=receipt;slot.jug.isKinematic=true;slot.jugCollider.enabled=false;slot.jug.gameObject.SetActive(true);slot.proxy.gameObject.SetActive(true);slot.floor.gameObject.SetActive(true);
            int index=Array.IndexOf(slots,slot);slot.staged=source.session is LWS.TruckFrogger.FrogMatch;
            source.GatePose(slot.staged?source.GateIndex:receipt.gate,out var finish,out var facing,out _);var right=facing*Vector3.right;
            // Earlier sequential trucks are inactive by design. Their recorded render-only
            // appearances meet here after the official comparison; real bodies stay untouched.
            slot.stagingPosition=finish+right*((index-1)*7)+facing*Vector3.forward*8;slot.stagingRotation=facing;
            Snapshot(slot);FollowConfirmedHull(slot);float side=index%2==0?-1:1;var target=slot.staged?slot.stagingPosition:receipt.position;
            var home=target+right*(side*14)+facing*Vector3.forward*8;var mark=target+right*(side*7)+facing*Vector3.forward*3;home.y=mark.y=slot.floor.bounds.max.y;
            slot.actorRoot.gameObject.SetActive(true);slot.actorRoot.SetPositionAndRotation(home,Quaternion.LookRotation(Vector3.ProjectOnPlane(target-home,Vector3.up).normalized));slot.actor.speed=1;slot.actor.Play("Run",0,0);Approaches++;
            slot.sequence=DOTween.Sequence().SetUpdate(true)
                .Append(slot.actorRoot.DOMove(mark,1.0f).SetEase(Ease.Linear))
                .AppendCallback(()=>{slot.actor.CrossFade("Throw",.12f);})
                .Append(slot.actorRoot.DOPunchRotation(new Vector3(-8,8,-10),.35f,1,.5f)).AppendCallback(()=>Launch(slot))
                .AppendInterval(2.4f).Append(slot.actorRoot.DOMove(home,.8f).SetEase(Ease.Linear))
                .OnComplete(()=>Retire(slot));
        }
        void Launch(Slot slot)
        {
            if(!slot.busy)return;slot.launched=true;slot.jug.gameObject.SetActive(true);slot.jug.isKinematic=false;slot.jugCollider.enabled=true;
            var hand=Hand(slot);var origin=hand!=null?hand.position:slot.actorRoot.position+Vector3.up*1.45f;var target=slot.hull.bounds.center;const float flight=.8f;
            if(!slot.staged&&source.Scope==slot.receipt.scope&&Visible(slot.receipt.truck)&&source.session.Live)target+=slot.receipt.truck.body.linearVelocity*(flight*Mathf.Clamp(Time.timeScale,0,2));
            slot.jug.position=origin;slot.jug.rotation=Quaternion.identity;slot.jug.linearVelocity=Vector3.ClampMagnitude((target-origin)/flight-Physics.gravity*(.5f*flight),25);slot.jug.angularVelocity=new Vector3(3,4,2);slot.jug.WakeUp();Throws++;
        }
        static Transform Hand(Slot slot)=>slot.actor!=null&&slot.actor.isHuman?slot.actor.GetBoneTransform(HumanBodyBones.RightHand):null;
        static bool Visible(SportsEventTruck truck)
        {
            if(!truck.gameObject.activeInHierarchy)return false;
            var renderers=truck.appearanceHost!=null&&truck.appearanceHost.Applied?truck.appearanceHost.ExteriorRenderers:truck.visuals.GetComponentsInChildren<Renderer>(true);
            foreach(var renderer in renderers)if(renderer!=null&&renderer.enabled&&renderer.gameObject.activeInHierarchy)return true;return false;
        }
        void FollowConfirmedHull(Slot slot)
        {
            var row=slot.receipt;var truck=row.truck;if(truck==null){Retire(slot);return;}
            bool current=!slot.staged&&source.session.MatchId==row.matchId&&source.Scope==row.scope&&Visible(truck);
            var pose=slot.staged?slot.stagingPosition:current?truck.body.position:row.position;var rotation=slot.staged?slot.stagingRotation:current?truck.body.rotation:row.rotation;var shape=truck.shape;
            var localPosition=Quaternion.Inverse(truck.body.rotation)*(shape.transform.position-truck.body.position);var localRotation=Quaternion.Inverse(truck.body.rotation)*shape.transform.rotation;
            slot.proxy.position=pose+rotation*localPosition;slot.proxy.rotation=rotation*localRotation;
            var scale=shape.transform.lossyScale;scale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));slot.hull.center=Vector3.Scale(shape.center,scale);slot.hull.size=Vector3.Scale(shape.size,scale);
            slot.ghost.gameObject.SetActive(!current);slot.ghost.SetPositionAndRotation(pose,rotation);
            slot.floor.attachedRigidbody.position=new Vector3(pose.x,slot.hull.bounds.min.y-.55f,pose.z);slot.floor.size=new Vector3(28,1,28);
        }
        void Snapshot(Slot slot)
        {
            ClearSnapshot(slot);var truck=slot.receipt.truck;
            var renderers=truck.appearanceHost!=null&&truck.appearanceHost.Applied?truck.appearanceHost.ExteriorRenderers:truck.visuals.GetComponentsInChildren<Renderer>(true);
            var inverse=Matrix4x4.TRS(truck.body.position,truck.body.rotation,Vector3.one).inverse;var baked=new List<Mesh>();
            foreach(var renderer in renderers)
            {
                if(renderer==null||!renderer.enabled)continue;Mesh mesh=null;
                if(renderer is SkinnedMeshRenderer skin){mesh=new Mesh{name="Recorded finish appearance"};skin.BakeMesh(mesh);baked.Add(mesh);}
                else if(renderer is MeshRenderer)mesh=renderer.GetComponent<MeshFilter>()?.sharedMesh;
                if(mesh==null)continue;
                var part=new GameObject("Recorded finish visual · "+renderer.name,typeof(MeshFilter),typeof(MeshRenderer));part.transform.SetParent(slot.ghost,false);var matrix=inverse*renderer.localToWorldMatrix;part.transform.localPosition=matrix.GetColumn(3);part.transform.localRotation=matrix.rotation;part.transform.localScale=matrix.lossyScale;part.GetComponent<MeshFilter>().sharedMesh=mesh;
                var copy=part.GetComponent<MeshRenderer>();copy.sharedMaterials=renderer.sharedMaterials;var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);copy.SetPropertyBlock(block);
                for(int i=0;i<renderer.sharedMaterials.Length;i++){block.Clear();renderer.GetPropertyBlock(block,i);copy.SetPropertyBlock(block,i);}
            }
            slot.baked=baked.ToArray();
        }
        void ClearSnapshot(Slot slot){if(slot.ghost!=null)for(int i=slot.ghost.childCount-1;i>=0;i--){var child=slot.ghost.GetChild(i);child.gameObject.SetActive(false);Destroy(child.gameObject);}if(slot.baked!=null)foreach(var mesh in slot.baked)if(mesh!=null)Destroy(mesh);slot.baked=null;}
        public void NativeContact(int index,Collision collision)
        {
            if(disposed||index<0||index>=slots.Length)return;var slot=slots[index];if(!slot.busy||!slot.launched)return;Contacts++;
            if(collision.collider==slot.hull){HullContacts++;if(slot.splash!=null){slot.splash.transform.position=collision.contactCount>0?collision.GetContact(0).point:slot.jug.position;slot.splash.Play(true);}if(slot.impact!=null)slot.impact.Play();}
        }
        void Retire(Slot slot)
        {
            slot.sequence=null;slot.busy=slot.launched=false;if(slot.jug!=null){slot.jug.isKinematic=true;slot.jugCollider.enabled=false;slot.jug.gameObject.SetActive(false);}if(slot.proxy!=null)slot.proxy.gameObject.SetActive(false);if(slot.floor!=null)slot.floor.gameObject.SetActive(false);if(slot.actorRoot!=null)slot.actorRoot.gameObject.SetActive(false);if(slot.ghost!=null)slot.ghost.gameObject.SetActive(false);ClearSnapshot(slot);
        }
        public void Clear()
        {
            waiting.Clear();accepted.Clear();accumulator=0;if(slots==null)return;foreach(var slot in slots){slot.sequence?.Kill();Retire(slot);}Throws=Contacts=HullContacts=Approaches=0;paused=false;
        }
        void OnDisable(){Clear();}
        void OnDestroy(){disposed=true;Clear();if(scene.IsValid()&&scene.isLoaded)SceneManager.UnloadSceneAsync(scene);}
    }
    internal static class RaceFinishPhysicsSceneCheck
    {
        public static bool IsAValidPhysicsScene(this Scene scene)=>scene.IsValid()&&scene.GetPhysicsScene().IsValid()&&scene.GetPhysicsScene()!=Physics.defaultPhysicsScene;
    }
}
