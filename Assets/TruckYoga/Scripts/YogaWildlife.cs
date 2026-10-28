using UnityEngine;

namespace LWS.TruckYoga
{
    public sealed class YogaWildlife : MonoBehaviour
    {
        public YogaMatch match;
        public Animator animator;
        public Vector3[] route;
        public float speed = .8f;
        int next = 1;
        float rest = 3;
        string state;
        public int WalkSteps { get; private set; }
        void State(string name) { if (state == name) return; state = name; animator.CrossFadeInFixedTime(name, .25f); }
        void Update()
        {
            if (match == null || route == null || route.Length < 2) return;
            if (!match.Live) { if (!match.Paused) State("Idle"); return; }
            if (rest > 0) { rest -= Time.deltaTime; State("Idle"); return; }
            Vector3 delta = route[next] - transform.position; delta.y = 0;
            if (delta.magnitude < .1f) { next = (next + 1) % route.Length; rest = 4; return; }
            float angle = Vector3.SignedAngle(transform.forward, delta, Vector3.up);
            State(Mathf.Abs(angle) > 24 ? angle < 0 ? "Left" : "Right" : "Walk");
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(delta), 70 * Time.deltaTime);
            transform.position = Vector3.MoveTowards(transform.position, route[next], speed * Time.deltaTime * Mathf.Clamp01(1 - Mathf.Abs(angle) / 100));
            WalkSteps++;
        }
    }
}
