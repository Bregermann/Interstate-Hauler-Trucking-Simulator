using Assets.CoreCollapse.Scripts.Structural;
using LWS.TruckSports;
using UnityEngine;

namespace LWS.TruckHoardHeist
{
    // CoreCollapse owns damage/collapse; this adapter activates the bounded
    // EzySlice-authored shard set and emits a real collision noise witness.
    public sealed class HoardPottery : Collapser
    {
        public HoardHeistMatch match;
        public Rigidbody body;
        public Collider intactCollider;
        public GameObject intactVisual;
        public Rigidbody[] shards;
        public AudioSource breakSound;
        public Vector3 seedPosition;
        public Quaternion seedRotation = Quaternion.identity;
        Vector3[] localPositions;
        Quaternion[] localRotations;
        int sourceSlot = -1;
        bool broken;
        void Awake() { startingHp = 1; }
        public void ResetPottery()
        {
            if (localPositions == null)
            {
                localPositions = new Vector3[shards.Length]; localRotations = new Quaternion[shards.Length];
                for (int i = 0; i < shards.Length; i++) { localPositions[i] = transform.InverseTransformPoint(shards[i].transform.position); localRotations[i] = Quaternion.Inverse(transform.rotation) * shards[i].transform.rotation; }
            }
            if (!body.isKinematic) body.linearVelocity = body.angularVelocity = Vector3.zero;
            body.isKinematic = false; body.position = seedPosition; body.rotation = seedRotation;
            transform.SetPositionAndRotation(seedPosition, seedRotation);
            for (int i = 0; i < shards.Length; i++)
            {
                if (!shards[i].isKinematic) shards[i].linearVelocity = shards[i].angularVelocity = Vector3.zero;
                shards[i].isKinematic = true; shards[i].position = transform.TransformPoint(localPositions[i]); shards[i].rotation = transform.rotation * localRotations[i]; shards[i].gameObject.SetActive(false);
                shards[i].transform.SetPositionAndRotation(transform.TransformPoint(localPositions[i]),transform.rotation*localRotations[i]);
            }
            currentHp = startingHp = 1; support = GetComponent<Support>(); sourceSlot = -1; broken = false;
            if (breakSound != null) breakSound.Stop();
            intactVisual.SetActive(true); intactCollider.enabled = true;
        }
        void OnCollisionEnter(Collision collision)
        {
            if (broken || match == null || !match.Live || match.Intro > 0) return;
            float speed = collision.impulse.magnitude / Mathf.Max(1, body.mass);
            if (speed < 3.5f) return;
            var truck = collision.collider.GetComponentInParent<SportsEventTruck>();
            if (truck != null) sourceSlot = truck.slot;
            ReceiveAllDamage();
        }
        protected override void Collapse()
        {
            if (broken) return;
            base.Collapse(); broken = true;
            Vector3 velocity = body.linearVelocity;
            Vector3 position=body.position;Quaternion rotation=body.rotation;
            body.linearVelocity = body.angularVelocity = Vector3.zero; body.isKinematic = true;
            transform.SetPositionAndRotation(position,rotation);
            intactVisual.SetActive(false); intactCollider.enabled = false;
            for(int i=0;i<shards.Length;i++)
            {
                var shard=shards[i];shard.transform.SetPositionAndRotation(transform.TransformPoint(localPositions[i]),transform.rotation*localRotations[i]);
                shard.gameObject.SetActive(true);shard.position=shard.transform.position;shard.rotation=shard.transform.rotation;shard.isKinematic=false;shard.useGravity=true;shard.linearVelocity=velocity;
            }
            if (breakSound != null) { breakSound.volume = .65f * match.settings.sfx; breakSound.Play(); }
            match.ReportNoiseEvent(sourceSlot, body.position, 4, "POTTERY SHATTERED");
        }
    }
}
