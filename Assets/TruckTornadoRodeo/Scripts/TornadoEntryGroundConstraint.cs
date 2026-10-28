using Cinemachine;
using UnityEngine;

namespace LWS.TruckTornadoRodeo
{
    // A ground-initialization floor for this sport's original MeshCollider.
    // CinemachineCollider keeps obstacle ownership; the same GroupComposer aims
    // after this Body correction. No camera Transform or physics body is written.
    [DefaultExecutionOrder(900)]
    public sealed class TornadoEntryGroundConstraint : CinemachineExtension
    {
        public TornadoMatch match;
        [Min(.5f)] public float minimumHeight = 3;
        CinemachineGroupComposer composer;
        CinemachineGroupComposer.AdjustmentMode originalAdjustment;
        bool captured;
        bool Active => match != null && match.GroundEntryShot;

        public override void PrePipelineMutateCameraStateCallback(CinemachineVirtualCameraBase vcam, ref CameraState state, float deltaTime)
        {
            var camera = vcam as CinemachineVirtualCamera;
            var group = camera != null ? camera.GetCinemachineComponent<CinemachineGroupComposer>() : null;
            if (group != composer) { RestoreComposer(); composer = group; captured = false; }
            if (Active && composer != null)
            {
                if (!captured) { originalAdjustment = composer.m_AdjustmentMode; captured = true; }
                // Aim-stage dolly can invalidate a Body-stage floor. Entry uses
                // the existing readability-derived Transposer distance and fixed
                // sport lens; vendor Aim still composes the full target group.
                composer.m_AdjustmentMode = CinemachineGroupComposer.AdjustmentMode.ZoomOnly;
            }
            else RestoreComposer();
        }

        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (!Active || stage != CinemachineCore.Stage.Body || match.ground == null) return;
            var position = state.CorrectedPosition; var ground = match.ground;
            float originY = Mathf.Max(position.y + 1, ground.bounds.max.y + 1);
            if (!ground.Raycast(new Ray(new Vector3(position.x, originY, position.z), Vector3.down), out var hit, originY - ground.bounds.min.y + 10)) return;
            float floor = hit.point.y + (float.IsFinite(minimumHeight) ? Mathf.Max(.5f, minimumHeight) : 3);
            if (position.y < floor) state.PositionCorrection += Vector3.up * (floor - position.y);
        }

        void RestoreComposer()
        {
            if (captured && composer != null) composer.m_AdjustmentMode = originalAdjustment;
            captured = false;
        }
        public void Detach()
        {
            RestoreComposer(); composer = null; match = null;
            if (VirtualCamera != null) VirtualCamera.RemoveExtension(this);
        }
        protected override void OnDestroy() { Detach(); base.OnDestroy(); }
    }
}
