using UnityEngine;
namespace LWS.TruckHorrorSports
{
    // The installed NOT_Lonely CharacterController rig is retained; Game Night owns per-seat input.
    public sealed class HorrorFootAgent : MonoBehaviour
    {
        public CharacterController controller;
        public Light flashlight;
        public HorrorMatch match;
        public int participant;
        public float walkSpeed = 3.8f, turnSpeed = 110;
        public bool Outside { get; private set; }
        float falling;
        public void LeaveCab(Vector3 position, Quaternion rotation)
        {
            gameObject.SetActive(true); controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation); controller.enabled = true; Outside = true; falling = 0;
        }
        public void EnterCab() { Outside = false; falling = 0; gameObject.SetActive(false); }
        public void Move(Vector2 input, float dt)
        {
            if (!Outside || !controller.enabled) return;
            transform.Rotate(0, input.x * turnSpeed * dt, 0);
            falling = controller.isGrounded ? -2 : falling - 9.81f * dt;
            controller.Move((transform.forward * input.y * walkSpeed + Vector3.up * falling) * dt);
        }
        void OnControllerColliderHit(ControllerColliderHit hit)
        { if (match != null && hit.collider.GetComponentInParent<HorrorPursuer>() != null) match.Hit(participant, 35); }
    }
}
