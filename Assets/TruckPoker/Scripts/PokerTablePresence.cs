using DG.Tweening;
using UnityEngine;
using LWS.TruckBowling;

namespace LWS.TruckPoker
{
    // Only render-only parent pivots move. Existing action/award tweens remain
    // on their original child pivots. No private cards or hand strength read.
    public sealed class PokerTablePresence : MonoBehaviour
    {
        public PokerMatch match;
        public Transform[] pivots;
        [Range(0, 3)] public float bodyTilt = 1.6f;
        [Range(0, 10)] public float lookYaw = 7;
        [Range(0, .04f)] public float suspension = .018f;
        Quaternion[] basis;
        Vector3[] positions;
        float[] gaze;
        Tween[] looks;
        int lastActor = int.MinValue, lastDealer = int.MinValue;
        public int ContextChanges { get; private set; }
        void Awake()
        {
            basis = new Quaternion[pivots.Length]; positions = new Vector3[pivots.Length]; gaze = new float[pivots.Length]; looks = new Tween[pivots.Length];
            for (int i = 0; i < pivots.Length; i++) { basis[i] = pivots[i].localRotation; positions[i] = pivots[i].localPosition; }
        }
        public void ResetPresence()
        {
            if (basis == null) return;
            for (int i = 0; i < pivots.Length; i++) { looks[i]?.Kill(); gaze[i] = 0; pivots[i].localPosition = positions[i]; pivots[i].localRotation = basis[i]; }
            lastActor = lastDealer = int.MinValue; ContextChanges = 0;
        }
        void LateUpdate()
        {
            if (match == null || !match.Running || match.Finished || match.Table == null || match.setup.reducedMotion)
            { ResetPose(); return; }
            if (match.Paused) return;
            int actor = match.Table.Actor, dealer = match.Dealing ? match.dealerDrive.CurrentSeat : -1;
            if (actor != lastActor || dealer != lastDealer)
            {
                ContextChanges++; lastActor = actor; lastDealer = dealer;
                for (int i = 0; i < pivots.Length; i++)
                {
                    int index = i;
                    var target = dealer >= 0 ? match.dealerDrive.vehicle.position : actor >= 0 && actor != i ? match.avatars[actor].position : Vector3.zero;
                    var local = match.avatars[i].InverseTransformDirection(target - match.avatars[i].position);
                    float yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -lookYaw, lookYaw);
                    looks[i]?.Kill(); looks[i] = DOTween.To(() => gaze[index], value => gaze[index] = value, yaw, .65f).SetEase(Ease.InOutSine).SetLink(gameObject);
                }
            }
            for (int i = 0; i < pivots.Length; i++)
            {
                if (i >= match.setup.count || match.setup.players[i].source != BowlingSource.AI) continue;
                float phase = match.Age * (actor == i ? 2.4f : 1.2f) + i * 1.73f;
                float active = actor == i ? 1 : .55f;
                float nervous = actor == i ? Mathf.Sin(phase * 3.1f) * .22f : 0;
                pivots[i].localPosition = positions[i] + Vector3.up * (Mathf.Sin(phase) * suspension);
                float lean = actor == i ? 2.2f : match.Table.HandDone && match.Table.IsInHand(i) && match.Table.LastPayout[i] == 0 ? 1.6f : 0;
                pivots[i].localRotation = basis[i] * Quaternion.Euler(lean + Mathf.Sin(phase * .7f) * bodyTilt * active, gaze[i] + Mathf.Sin(phase * .4f) * .6f, Mathf.Sin(phase) * bodyTilt * active + nervous);
            }
        }
        void ResetPose()
        {
            if (basis == null) return;
            for (int i = 0; i < pivots.Length; i++) if (pivots[i] != null) { looks[i]?.Kill(); pivots[i].localPosition = positions[i]; pivots[i].localRotation = basis[i]; }
            lastActor = lastDealer = int.MinValue;
        }
        void OnDisable() { ResetPose(); }
        void OnDestroy() { if (looks != null) foreach (var tween in looks) tween?.Kill(); }
    }
}
