using UnityEngine;
namespace LWS.TruckWhackAMole
{
    public sealed class WhackMole : MonoBehaviour
    {
        public WhackMatch match;
        public int hole;
        public Collider target;
        public Transform[] models;
        public Animator[] animators;
        MolePhase prior; int generation = -1; int shown = -1;
        public void ResetActor()
        {
            prior = MolePhase.Underground; generation = -1; shown = -1; target.enabled = false;
            for (int i = 0; i < models.Length; i++) { models[i].localScale = Vector3.zero; animators[i].enabled = false; }
        }
        public void Tick()
        {
            var phase = match.rules.Phase(hole); int kind = (int)match.rules.kinds[hole]; int nextGeneration = match.rules.generations[hole];
            if (phase != prior || nextGeneration != generation || kind != shown)
            {
                if (shown >= 0) { models[shown].localScale = Vector3.zero; animators[shown].enabled = false; }
                shown = kind; generation = nextGeneration; prior = phase; target.enabled = phase == MolePhase.Exposed;
                if (phase == MolePhase.Underground) return;
                models[kind].localScale = Vector3.one; animators[kind].enabled = true;
                string state = phase == MolePhase.Emerging ? "Emerge" : phase == MolePhase.Exposed ? "Look" : phase == MolePhase.Retreating ? "Dive" : "Whack";
                animators[kind].Play(state, 0, 0);
            }
        }
        void OnTriggerEnter(Collider other) => Contact(other);
        void OnTriggerStay(Collider other) => Contact(other);
        void Contact(Collider other)
        {
            var bumper = other.GetComponent<WhackBumper>();
            if (bumper == null || bumper.match != match || other.attachedRigidbody == null) return;
            float speed = other.attachedRigidbody.GetPointVelocity(transform.position).magnitude;
            match.Contact(bumper.slot, hole, speed);
        }
    }
}
