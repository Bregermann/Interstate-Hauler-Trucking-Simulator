using UnityEngine;
namespace LWS.TruckBalloonBattle
{
    // Kinematic balloon collider follows a bounded damped tether. NWH alone
    // owns the tractor; this lightweight presentation cannot torque its cab.
    public sealed class BalloonTarget : MonoBehaviour
    {
        public BalloonMatch match;
        public int slot, index;
        public Rigidbody body;
        public Collider contact;
        public Transform[] cord;
        public Vector3 home;
        Vector3 velocity;
        public void Restore()
        {
            velocity = Vector3.zero; transform.localScale = Vector3.one; contact.enabled = true;
            // MaterialPropertyBlocks are runtime data, not serialized scene
            // overrides. Apply the participant palette on every round/reset.
            Color.RGBToHSV(match.setup.players[slot].color, out float hue, out _, out _);
            var colors = new MaterialPropertyBlock(); var tint = Color.HSVToRGB(Mathf.Repeat(hue + index * .10f, 1), .75f, 1);
            colors.SetColor("_BaseColor", tint); colors.SetColor("_Color", tint);
            GetComponentInChildren<Renderer>().SetPropertyBlock(colors);
            body.position = Home(); body.rotation = Quaternion.identity; transform.position = body.position;
            foreach (var part in cord) part.gameObject.SetActive(true);
            Stitch();
        }
        Quaternion Heading() { var forward = Vector3.ProjectOnPlane(match.trucks[slot].transform.forward, Vector3.up); return forward.sqrMagnitude > .01f ? Quaternion.LookRotation(forward) : Quaternion.identity; }
        Vector3 Home() => match.trucks[slot].body.position + Heading() * home;
        public void Follow(float dt)
        {
            if (!contact.enabled) return;
            var truck = match.trucks[slot]; var center = Home();
            var wind = match.atmosphere.Flow * .025f;
            var lag = Vector3.ClampMagnitude(-truck.body.linearVelocity * .065f + wind, 1.6f);
            var bob = new Vector3(Mathf.Sin(match.rules.Clock * 1.7f + slot + index), Mathf.Sin(match.rules.Clock * 2 + index * 2) * .5f, Mathf.Cos(match.rules.Clock + index)) * .3f;
            var next = Vector3.SmoothDamp(body.position, center + lag + bob, ref velocity, .26f, 55, dt);
            next = center + Vector3.ClampMagnitude(next - center, 2.2f);
            body.MovePosition(next); body.MoveRotation(Quaternion.Euler(Mathf.Sin(match.rules.Clock + slot) * 9, 0, -lag.x * 10));
            Stitch();
        }
        void Stitch()
        {
            var start = match.trucks[slot].body.position + Heading() * new Vector3(home.x * .18f, 2.5f, -1.5f);
            var end = body.position - Vector3.up * 1.7f;
            var middle = (start + end) * .5f + Vector3.right * Mathf.Sin(match.rules.Clock + index) * .18f;
            for (int i = 0; i < cord.Length; i++)
            {
                Vector3 a = i == 0 ? start : middle, b = i == 0 ? middle : end;
                var delta = b - a; cord[i].SetPositionAndRotation((a + b) * .5f, Quaternion.FromToRotation(Vector3.up, delta));
                cord[i].localScale = new Vector3(.045f, delta.magnitude * .5f, .045f);
            }
        }
        public void Hide()
        {
            contact.enabled = false; transform.localScale = Vector3.zero;
            foreach (var part in cord) part.localScale = Vector3.zero;
        }
        void OnTriggerEnter(Collider other) => Touch(other);
        void OnTriggerStay(Collider other) => Touch(other);
        void Touch(Collider other)
        {
            var spike = other.GetComponent<BalloonSpike>();
            if (spike != null && spike.match == match) match.Contact(spike.slot, slot, index);
        }
    }
}
