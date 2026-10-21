using DG.Tweening;
using UnityEngine;

namespace LWS.TruckFieldGoal
{
    // Render-only holder choreography. Confirmed match scoring starts it;
    // animation cannot touch the football, kick physics or score authority.
    public sealed class FieldGoalHolderPresentation : MonoBehaviour
    {
        public FieldGoalMatch match;
        public Transform holder;
        public Transform steadyingArm;
        public int authoredVersion;
        public int Celebrations { get; private set; }
        public bool CelebratedAttempt { get; private set; }
        Vector3 home;
        Quaternion rotation;
        bool initialized;
        Sequence dance;

        void Awake() { if (steadyingArm == null && holder != null) steadyingArm = holder.Find("Holder padded steadying arm"); RememberHome(); }
        void RememberHome()
        {
            if (initialized || holder == null) return;
            home = holder.localPosition; rotation = holder.localRotation; initialized = true;
        }
        public void ResetPresentation()
        {
            RememberHome(); dance?.Kill(); dance = null; CelebratedAttempt = false;
            if (!initialized || holder == null) return;
            holder.localPosition = home; holder.localRotation = rotation;
            if (steadyingArm != null) steadyingArm.gameObject.SetActive(true);
        }
        public void Celebrate()
        {
            ResetPresentation(); if (holder == null) return;
            Celebrations++; CelebratedAttempt = true;
            var angles = rotation.eulerAngles;
            dance = DOTween.Sequence().SetLink(gameObject)
                .Append(holder.DOLocalMoveY(home.y + .18f, .22f).SetEase(Ease.OutQuad))
                .Join(holder.DOLocalRotate(angles + new Vector3(-9, 0, 5), .22f).SetEase(Ease.OutSine))
                .Append(holder.DOLocalMoveY(home.y, .2f).SetEase(Ease.InQuad))
                .Join(holder.DOLocalRotate(angles, .2f))
                .Append(holder.DOLocalMoveY(home.y + .14f, .22f).SetEase(Ease.OutQuad))
                .Join(holder.DOLocalRotate(angles + new Vector3(-7, 0, -5), .22f))
                .Append(holder.DOLocalMoveY(home.y, .2f).SetEase(Ease.InQuad))
                .Join(holder.DOLocalRotate(angles + new Vector3(0, 7, 0), .2f))
                .Append(holder.DOLocalRotate(angles + new Vector3(0, -7, 0), .24f).SetEase(Ease.InOutSine))
                .Append(holder.DOLocalRotate(angles, .24f).SetEase(Ease.OutSine))
                .OnComplete(() => { holder.localPosition = home; holder.localRotation = rotation; dance = null; });
        }
        void Update()
        {
            if (steadyingArm != null && match != null)
            {
                bool visible = !match.Running || !match.football.Launched;
                if (steadyingArm.gameObject.activeSelf != visible) steadyingArm.gameObject.SetActive(visible);
            }
            if (dance == null || match == null) return;
            if (!match.Running) { ResetPresentation(); return; }
            if (match.Paused) dance.Pause(); else if (!dance.IsPlaying()) dance.Play();
        }
        void OnDisable() { ResetPresentation(); }
    }
}
