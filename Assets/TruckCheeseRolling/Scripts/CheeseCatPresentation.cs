using UnityEngine;

namespace LWS.TruckCheeseRolling
{
    // Ambient vendor animation only. Scenery placement/colliders and all race
    // physics remain owned by the existing CheeseScenery/Match systems.
    public sealed class CheeseCatPresentation : MonoBehaviour
    {
        public Animator animator;
        public CheeseMatch match;
        public int initialSeed;
        static readonly int[] States = {
            Animator.StringToHash("Base Layer.Idle"), Animator.StringToHash("Base Layer.Look"),
            Animator.StringToHash("Base Layer.Scratch"), Animator.StringToHash("Base Layer.Yawn")
        };
        System.Random random;
        RuntimeAnimatorController durationSource;
        readonly float[] durations = { 4, 4, 6, 6 };
        float next;
        public int Reactions { get; private set; }
        void OnEnable() { if (Application.isPlaying) ResetPose(initialSeed); }
        public void ResetPose(int seed)
        {
            if (animator == null) return;
            if (durationSource != animator.runtimeAnimatorController && animator.runtimeAnimatorController != null)
            {
                durationSource = animator.runtimeAnimatorController;
                foreach (var clip in durationSource.animationClips)
                {
                    int kind = clip.name.IndexOf("Scratch", System.StringComparison.OrdinalIgnoreCase) >= 0 ? 2 : clip.name.IndexOf("Yawn", System.StringComparison.OrdinalIgnoreCase) >= 0 ? 3 : clip.name.IndexOf("Look", System.StringComparison.OrdinalIgnoreCase) >= 0 ? 1 : 0;
                    durations[kind] = Mathf.Max(.5f, clip.length);
                }
            }
            random = new System.Random(seed); Reactions = 0; animator.applyRootMotion = false;
            animator.Rebind(); animator.Play(States[0], 0, (float)random.NextDouble()); animator.Update(0);
            next = Time.time + 2 + (float)random.NextDouble() * 4;
        }
        void Update()
        {
            if (animator == null || !animator.isActiveAndEnabled || Time.time < next || random == null) return;
            bool alert = false;
            if (match != null && match.InMatch) foreach (var truck in match.trucks)
                if (truck.gameObject.activeInHierarchy && (truck.body.position - transform.position).sqrMagnitude < 144) { alert = true; break; }
            int choice = random.Next(8); int state = alert ? 1 : choice < 4 ? 0 : choice < 6 ? 1 : choice == 6 ? 2 : 3;
            // Visible threats select the authored look-around, never a physics
            // response or an animation event that could change race state.
            animator.CrossFadeInFixedTime(States[state], .18f); Reactions++;
            next = Time.time + (state == 0 ? 4 : durations[state] + .25f) + (float)random.NextDouble() * 4;
        }
    }
}
