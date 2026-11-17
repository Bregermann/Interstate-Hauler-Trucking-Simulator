using UnityEngine;
namespace LWS.TruckHorrorSports
{
    public enum HorrorAnomalyKind { DisappearingTruck, LoopSign, MazeGate, FalseHeadlights, SelfOperatingPump, Apparition }
    public sealed class HorrorAnomaly : MonoBehaviour
    {
        public HorrorAnomalyKind kind;
        public int stage = 1;
        public GameObject subject;
        public Transform pivot;
        public Light[] lights;
        public AudioSource spatialSound;
        public TextMesh sign;
        public string alternateCaption;
        Vector3 position;
        Quaternion rotation;
        string originalCaption;
        bool changed, initialized;
        float expires;
        Renderer[] truckRenderers;
        Collider[] truckColliders;
        Light[] truckLights;
        bool[] rendererStates, colliderStates, lightStates;
        void SetTruckVisible(bool visible)
        {
            if (subject == null) return;
            if (truckRenderers == null)
            {
                truckRenderers = subject.GetComponentsInChildren<Renderer>(true); rendererStates = new bool[truckRenderers.Length];
                truckColliders = subject.GetComponentsInChildren<Collider>(true); colliderStates = new bool[truckColliders.Length];
                truckLights = subject.GetComponentsInChildren<Light>(true); lightStates = new bool[truckLights.Length];
                for (int i = 0; i < truckRenderers.Length; i++) rendererStates[i] = truckRenderers[i].enabled;
                for (int i = 0; i < truckColliders.Length; i++) colliderStates[i] = truckColliders[i].enabled;
                for (int i = 0; i < truckLights.Length; i++) lightStates[i] = truckLights[i].enabled;
            }
            // The event and its spatial AudioSource can live on the truck root. Hide physical/visual parts only.
            for (int i = 0; i < truckRenderers.Length; i++) if (truckRenderers[i] != null) truckRenderers[i].enabled = visible && rendererStates[i];
            for (int i = 0; i < truckColliders.Length; i++) if (truckColliders[i] != null) truckColliders[i].enabled = visible && colliderStates[i];
            for (int i = 0; i < truckLights.Length; i++) if (truckLights[i] != null) truckLights[i].enabled = visible && lightStates[i];
        }
        public void ResetEvent()
        {
            if (!initialized) { position = transform.position; rotation = pivot != null ? pivot.localRotation : Quaternion.identity; originalCaption = sign != null ? sign.text : null; initialized = true; }
            changed = false;
            if (subject != null) subject.SetActive(kind != HorrorAnomalyKind.Apparition);
            if (kind == HorrorAnomalyKind.DisappearingTruck) SetTruckVisible(true);
            if (pivot != null) pivot.localRotation = rotation;
            if (sign != null) sign.text = originalCaption;
            if (lights != null) foreach (var light in lights) if (light != null) light.enabled = false;
            if (spatialSound != null) spatialSound.Stop();
        }
        public void TickEvent(HorrorMatch match, float dt)
        {
            if (changed) { if (kind == HorrorAnomalyKind.Apparition && match.Age > expires && subject != null) subject.SetActive(false); return; }
            if (match.Escalation < stage) return;
            if (kind == HorrorAnomalyKind.MazeGate)
            {
                for (int i = 0; i < match.setup.count; i++) if (Vector3.Distance(match.trucks[i].body.position, position) < 12) return;
                // An empty secondary clearing may close; the permanent outer escape corridor stays open.
                if (pivot != null) pivot.localRotation = rotation * Quaternion.Euler(0, 90, 0);
            }
            if (kind == HorrorAnomalyKind.DisappearingTruck) SetTruckVisible(false);
            if (kind == HorrorAnomalyKind.Apparition && subject != null) { subject.SetActive(true); expires = match.Age + 6; }
            if (kind == HorrorAnomalyKind.LoopSign && sign != null) sign.text = alternateCaption;
            if (lights != null) foreach (var light in lights) if (light != null) light.enabled = true;
            if (spatialSound != null && spatialSound.clip != null) { spatialSound.volume = match.settings.sfx * .55f; spatialSound.Play(); }
            changed = true;
        }
    }
}
