using UnityEngine;

namespace LWS.TruckMoonMadness
{
    public sealed class MoonMeteor : MonoBehaviour
    {
        public MoonMatch match; public Rigidbody body; public Collider shape; public Renderer rock;
        public Transform warningRing; public ParticleSystem explosion, dust;
        public Vector3 ImpactPoint { get; private set; }
        public float WarningSeconds { get; private set; }
        public bool Falling { get; private set; }
        public int Impacts { get; private set; }
        float remaining;
        public void ResetMeteor()
        {
            WarningSeconds = remaining = 0; Falling = false; Impacts = 0; body.isKinematic = true;
            shape.enabled = false; rock.enabled = false; warningRing.gameObject.SetActive(false);
            explosion.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        public void Warn(Vector3 point)
        {
            ImpactPoint = point; WarningSeconds = 3; warningRing.position = point + Vector3.up * .08f;
            warningRing.gameObject.SetActive(true);
        }
        public bool Busy => WarningSeconds > 0 || Falling || remaining > 0;
        public void Step(float dt)
        {
            if (WarningSeconds > 0)
            {
                WarningSeconds = Mathf.Max(0, WarningSeconds - dt);
                if (WarningSeconds == 0)
                {
                    body.position = ImpactPoint + new Vector3(-6, 28, 0); body.rotation = Quaternion.identity;
                    body.isKinematic = false; body.linearVelocity = new Vector3(2.5f, -12, 0); body.angularVelocity = new Vector3(.4f, .7f, .2f);
                    shape.enabled = rock.enabled = true; Falling = true;
                }
            }
            if (Falling) body.AddForce(Physics.gravity * (1f / 6 - 1), ForceMode.Acceleration);
            if (remaining > 0) { remaining -= dt; if (remaining <= 0) warningRing.gameObject.SetActive(false); }
        }
        void OnCollisionEnter(Collision collision)
        {
            if (!Falling || !match.Live) return;
            Falling = false; Impacts++; body.linearVelocity = body.angularVelocity = Vector3.zero; body.isKinematic = true;
            shape.enabled = rock.enabled = false; remaining = 3;
            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : body.position;
            explosion.transform.position = dust.transform.position = point; explosion.Play(true); dust.Play(true);
            match.MeteorImpact(point); // Actual collision; bounded effect on the original dynamic assemblies.
        }
    }
}
