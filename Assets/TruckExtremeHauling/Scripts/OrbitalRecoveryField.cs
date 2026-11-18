using UnityEngine;

namespace LWS.TruckExtremeHauling
{
    // Scene-owned physical hazards. Native rigid bodies/colliders own motion;
    // only normal round boundaries restore the authored starting arrangement.
    public sealed class OrbitalRecoveryField : MonoBehaviour
    {
        public Rigidbody[] asteroids = System.Array.Empty<Rigidbody>();
        Vector3[] positions;
        Quaternion[] rotations;
        string preparedMatch;

        void Cache()
        {
            if (positions != null) return;
            if(asteroids==null||asteroids.Length==0)throw new System.InvalidOperationException("Orbital recovery needs at least one authored physical asteroid.");
            foreach(var body in asteroids)if(body==null)throw new System.InvalidOperationException("Orbital recovery contains a missing asteroid body; repair the saved venue.");
            positions = new Vector3[asteroids.Length]; rotations = new Quaternion[asteroids.Length];
            for (int i=0;i<asteroids.Length;i++)
            { positions[i]=asteroids[i].position; rotations[i]=asteroids[i].rotation; }
        }
        public void Prepare(bool active,string matchId)
        {
            Cache();
            if(preparedMatch==matchId)return;
            preparedMatch=matchId;
            for(int i=0;i<asteroids.Length;i++)
            {
                var body=asteroids[i];body.gameObject.SetActive(active);body.useGravity=false;
                body.isKinematic=false;body.position=positions[i];body.rotation=rotations[i];
                body.linearVelocity=body.angularVelocity=Vector3.zero;
                if(active)
                {
                    body.AddForce(new Vector3(i%2==0?.25f:-.25f,Mathf.Sin(i)*.07f,-.06f),ForceMode.VelocityChange);
                    body.AddTorque(new Vector3(.025f,.035f,i%2==0?.02f:-.02f),ForceMode.VelocityChange);
                }
                else body.isKinematic=true;
            }
        }
        public void Freeze()
        {
            foreach(var body in asteroids)if(body!=null)
            { if(!body.isKinematic)body.linearVelocity=body.angularVelocity=Vector3.zero;body.isKinematic=true; }
        }
    }
}
