using UnityEngine;

namespace LWS.TruckHitchSports
{
    // Presentation reads the existing match's applied forces and real contacts.
    // It never supplies drag, trajectory, attachment or scoring authority.
    public sealed class RocketFlightPresentation : MonoBehaviour
    {
        public TrailerblazersMatch Match;
        public int Slot;
        public Transform Canopy;
        public LineRenderer[] Harness;
        public Vector3[] HarnessAnchors;
        public ParticleSystem Crash;
        public AudioSource OpeningSound;
        public float Opening { get; private set; }
        public bool HardImpactShown { get; private set; }
        bool heardOpening, paused, resumeCrash, resumeAudio;
        Renderer canopyRenderer;
        MaterialPropertyBlock color;

        public void ResetForRound(Color participantColor)
        {
            Opening = 0; HardImpactShown = heardOpening = paused = false;
            if (Canopy != null)
            {
                Canopy.gameObject.SetActive(false);
                if (canopyRenderer == null) canopyRenderer = Canopy.GetComponent<Renderer>();
                if (color == null) color = new MaterialPropertyBlock();
                color.SetColor("_BaseColor", participantColor); color.SetColor("_Color", participantColor);
                canopyRenderer.SetPropertyBlock(color, 0);
            }
            if (Harness != null) foreach (var line in Harness) if (line != null) line.enabled = false;
            if (OpeningSound != null) OpeningSound.Stop();
            if (Crash != null) Crash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        void LateUpdate()
        {
            if (Match == null || Slot < 0 || Slot >= Match.setup.count) return;
            if (!Match.Running) { if (Crash != null) Crash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); if (OpeningSound != null) OpeningSound.Stop(); Opening = 0; if (Canopy != null) Canopy.gameObject.SetActive(false); if (Harness != null) foreach (var line in Harness) if (line != null) line.enabled = false; return; }
            if (Match.Paused)
            {
                if (!paused)
                {
                    resumeCrash = Crash != null && Crash.isPlaying;
                    resumeAudio = OpeningSound != null && OpeningSound.isPlaying;
                    if (resumeCrash) Crash.Pause(true);
                    if (resumeAudio) OpeningSound.Pause();
                }
                paused = true; return;
            }
            if (paused)
            {
                if (resumeCrash) Crash.Play(true);
                if (resumeAudio) OpeningSound.UnPause();
                paused = false;
            }
            bool deploying = Match.Live && !Match.Completed[Slot] && Match.AppliedAirBrakeNewtons(Slot) > 0;
            Opening = Mathf.MoveTowards(Opening, deploying ? 1 : 0, Time.deltaTime * 1.5f);
            if (OpeningSound != null)
            {
                OpeningSound.volume = Match.settings != null ? Match.settings.sfx * .5f : 0;
                if (deploying && !heardOpening) { heardOpening = true; OpeningSound.Play(); }
                if (!Match.Live) OpeningSound.Stop();
            }
            if (Canopy == null) return;
            bool visible = Opening > .01f;
            Canopy.gameObject.SetActive(visible);
            if (Harness != null) foreach (var line in Harness) if (line != null) line.enabled = visible;
            if (!visible) return;
            var truck = Match.trucks[Slot]; var body = truck.body;
            Canopy.position = body.worldCenterOfMass + Vector3.up * (6.5f + 2 * Opening);
            Vector3 forward = Vector3.ProjectOnPlane(truck.transform.forward, Vector3.up);
            if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            Quaternion heading = Quaternion.LookRotation(forward, Vector3.up);
            Vector3 wind = Match.presentationWeather != null ? Match.presentationWeather.Flow : Vector3.zero;
            Vector3 sway = Vector3.ClampMagnitude((wind - body.linearVelocity) * .12f, 8);
            Canopy.rotation = Quaternion.Slerp(Canopy.rotation, Quaternion.Euler(-sway.z, heading.eulerAngles.y, sway.x), 1 - Mathf.Exp(-Time.deltaTime * 5));
            Canopy.localScale = Vector3.one * (Opening * 1.35f);
            if (Harness == null || HarnessAnchors == null) return;
            for (int i = 0; i < Mathf.Min(Harness.Length, HarnessAnchors.Length); i++)
            {
                if (Harness[i] == null) continue;
                Harness[i].SetPosition(0, truck.transform.TransformPoint(HarnessAnchors[i]));
                Harness[i].SetPosition(1, Canopy.TransformPoint(new Vector3(i % 2 == 0 ? -3.5f : 3.5f, 0, i < 2 ? -2.5f : 2.5f)));
            }
        }

        public void ObserveCollision(Collision collision)
        {
            if (Match == null || !Match.Live || !Match.HasIgnited(Slot) || Match.AirborneSeconds(Slot) <= .25f) return;
            var otherTruck = collision.collider.GetComponentInParent<LWS.TruckSports.SportsEventTruck>();
            var otherTrailer = collision.collider.GetComponentInParent<TrailerBody>();
            if (otherTruck == Match.trucks[Slot] || otherTrailer != null && otherTrailer == Match.couplers[Slot].Attached) return;
            float normalSpeed = 0; Vector3 point = Match.trucks[Slot].body.position;
            for (int i = 0; i < collision.contactCount; i++)
            {
                var contact = collision.GetContact(i);
                float speed = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, contact.normal));
                if (speed > normalSpeed) { normalSpeed = speed; point = contact.point; }
            }
            Match.ObserveRocketImpact(Slot, normalSpeed);
            if (normalSpeed < 9 || HardImpactShown) return;
            HardImpactShown = true;
            if (Crash != null) { Crash.transform.position = point; Crash.Play(true); }
            Match.feedback?.PlayAt(2, point, normalSpeed);
        }

        void OnDisable()
        {
            if (OpeningSound != null) OpeningSound.Stop();
            if (Crash != null) Crash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (Canopy != null) Canopy.gameObject.SetActive(false);
            if (Harness != null) foreach (var line in Harness) if (line != null) line.enabled = false;
        }
    }
}
