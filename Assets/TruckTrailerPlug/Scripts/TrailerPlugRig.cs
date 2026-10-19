using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckTrailerPlug
{
    public sealed class TrailerPlugRig : MonoBehaviour
    {
        public TrailerPlugMatch match;
        public SportsEventTruck truck;
        public Rigidbody trailer;
        public Transform anchor;
        public SpringJoint spring;
        public LineRenderer cable;
        public Vector3 truckStart = new Vector3(-7, 61, -22);
        public Vector3 trailerOffset = new Vector3(0, -.6f, -9);
        public Vector3 trailerHitch = new Vector3(0, 1, 7.5f);
        public Vector3 truckHitch = new Vector3(0, .4f, -1.5f);
        public float restLength = 48;
        public bool Released { get; private set; }
        public bool Supported => Time.fixedTime - lastSupport < .12f || trailer.IsSleeping() && lastSupport > 0;
        public Vector3 IncomingVelocity { get; private set; }
        public Vector3 IncomingAngular { get; private set; }
        public Vector3 IncomingCenter { get; private set; }
        public Vector3 ReleasedVelocity { get; private set; }
        public Vector3 ReleasedAngular { get; private set; }
        ConfigurableJoint hitch;
        float lastSupport = -100;
        public void Paint(Color color)
        {
            var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor", color); block.SetColor("_Color", color);
            truck.paint.SetPropertyBlock(block);
        }
        public void Prepare()
        {
            var obstacle=trailer.GetComponent<UnityEngine.AI.NavMeshObstacle>();
            if(obstacle==null){obstacle=trailer.gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();obstacle.shape=UnityEngine.AI.NavMeshObstacleShape.Box;obstacle.size=new Vector3(6.6f,4.6f,16.6f);obstacle.center=Vector3.zero;obstacle.carving=true;obstacle.carveOnlyStationary=false;}

            ClearHitch(); Released = false; lastSupport = -100;
            truck.Prepare(truckStart, Quaternion.identity, false);
            truck.body.isKinematic = true; truck.Power(false);
            trailer.gameObject.SetActive(true); trailer.isKinematic = true;
            trailer.position = truckStart + trailerOffset; trailer.rotation = Quaternion.identity;
            trailer.detectCollisions = true;
            spring.maxDistance = restLength;
            hitch = trailer.gameObject.AddComponent<ConfigurableJoint>();
            hitch.connectedBody = truck.body; hitch.autoConfigureConnectedAnchor = false;
            hitch.anchor = trailerHitch; hitch.connectedAnchor = truckHitch;
            hitch.xMotion = hitch.yMotion = hitch.zMotion = ConfigurableJointMotion.Locked;
            hitch.angularXMotion = hitch.angularYMotion = hitch.angularZMotion = ConfigurableJointMotion.Free;
            hitch.enableCollision = false; hitch.projectionMode = JointProjectionMode.PositionAndRotation;
            hitch.projectionDistance = .1f; hitch.breakForce = float.PositiveInfinity;
            foreach (var a in trailer.GetComponentsInChildren<Collider>())
                foreach (var b in truck.GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(a, b, true);
        }
        public void BeginSwing()
        {
            truck.body.isKinematic = trailer.isKinematic = false;
            truck.body.linearVelocity = truck.body.angularVelocity = Vector3.zero;
            trailer.linearVelocity = trailer.angularVelocity = Vector3.zero;
            truck.body.WakeUp(); trailer.WakeUp();
        }
        public void Pump(Vector2 input)
        {
            if (!Released) LWS.TruckBungeeDemolition.BungeeSwingForces.Apply(truck.body, input, 5.5f);
        }
        public bool Release()
        {
            if (Released || hitch == null) return false;
            Released = true; ReleasedVelocity = trailer.linearVelocity; ReleasedAngular = trailer.angularVelocity;
            ClearHitch(); return true;
        }
        void ClearHitch() { if (hitch != null) { Destroy(hitch); hitch = null; } }
        public void Hold()
        {
            truck.Power(false); truck.body.isKinematic = true; trailer.isKinematic = true;
        }
        void FixedUpdate()
        {
            IncomingVelocity = trailer.linearVelocity; IncomingAngular = trailer.angularVelocity;
            IncomingCenter = trailer.worldCenterOfMass;
        }
        public void GroundContact(Collision collision)
        {
            if (!Released || collision.collider.isTrigger || collision.rigidbody != null) return;
            for (int i = 0; i < collision.contactCount; i++)
                if (collision.GetContact(i).normal.y > .45f && collision.GetContact(i).point.y > -.3f)
                { lastSupport = Time.fixedTime; break; }
        }
        void LateUpdate()
        {
            if (cable == null) return;
            const int count = 32; cable.positionCount = count;
            var tip = truck.body.position + truck.body.rotation * new Vector3(0, .5f, -.5f);
            float slack = Mathf.Max(0, restLength - Vector3.Distance(anchor.position, tip));
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                cable.SetPosition(i, Vector3.Lerp(anchor.position, tip, t) + Vector3.down *
                    (Mathf.Sin(t * Mathf.PI) * Mathf.Min(5, slack * .15f)));
            }
        }
        void OnDestroy() { ClearHitch(); }
    }
}
