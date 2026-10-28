using System;
using UnityEngine;
using LWS.TruckSports;
namespace LWS.TruckNestBuilding
{
    public sealed class NestFamily : MonoBehaviour
    {
        public NestMatch match;
        public NestBuildSite site;
        public Rigidbody eggBody;
        public Collider eggCollider;
        public Renderer eggRenderer;
        public Transform[] babies;
        public AudioSource tinyHorn;
        public ParticleSystem shellBurst;
        public bool EggPresent { get; private set; }
        public bool Hatched { get; private set; }
        public int WiggleFrames { get; private set; }
        float contactAge;
        Collider liningContact;
        public bool EggStable => EggPresent && contactAge > 0 && LiningPairPresent() && eggBody.linearVelocity.sqrMagnitude < .025f && eggBody.angularVelocity.sqrMagnitude < .06f;
        public void Prepare(string profile, Color color)
        {
            var catalog = Resources.Load<SportsTruckAppearanceCatalog>("SportsTruckAppearanceCatalog");
            if (catalog == null) throw new InvalidOperationException("Existing appearance catalog required for the miniature family.");
            var body = catalog.ResolveBody(SportsParticipantRegistry.GetAppearance(profile).bodyId);
            foreach (var baby in babies)
            {
                for (int i = baby.childCount - 1; i >= 0; i--) Destroy(baby.GetChild(i).gameObject);
                var model = Instantiate(body.visualPrefab, baby, false); model.transform.localRotation = Quaternion.Euler(body.presentationEuler);
                if (model.GetComponentsInChildren<Rigidbody>(true).Length != 0 || model.GetComponentsInChildren<Collider>(true).Length != 0) throw new InvalidOperationException("Baby trucks use explicit render-only catalog models, never another vehicle controller.");
                var renderers = model.GetComponentsInChildren<Renderer>(true); Bounds bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                float scale = 1.4f / Mathf.Max(bounds.size.x, bounds.size.z); model.transform.localScale *= scale;
                model.transform.localPosition -= new Vector3(bounds.center.x - baby.position.x, bounds.min.y - baby.position.y, bounds.center.z - baby.position.z) * scale;
                var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor", color); block.SetColor("_Color", color);
                foreach (var renderer in renderers) for (int material = 0; material < renderer.sharedMaterials.Length; material++) if (SportsTruckAppearanceCompatibility.IsPaintMaterial(body.id, renderer.sharedMaterials[material])) renderer.SetPropertyBlock(block, material);
            }
            ResetFamily();
        }
        public void ResetFamily()
        {
            EggPresent = Hatched = false; contactAge = 0;liningContact=null; WiggleFrames = 0;
            eggBody.isKinematic = true; eggCollider.enabled = false; eggRenderer.enabled = false;
            foreach (var baby in babies) baby.gameObject.SetActive(false); if (tinyHorn != null) tinyHorn.Stop(); if (shellBurst != null) shellBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        public void ReceiveEgg()
        {
            if (!site.CoreReady || EggPresent || Hatched) return;
            var lining = site.slots[9]; eggBody.isKinematic = true; eggBody.position = lining.body.position + Vector3.up * .91f; eggBody.rotation = Quaternion.identity;
            eggBody.isKinematic = false; eggBody.linearVelocity = eggBody.angularVelocity = Vector3.zero; eggBody.WakeUp(); eggCollider.enabled = true; eggRenderer.enabled = true; EggPresent = true; contactAge = 0;liningContact=null;
        }
        public void LiningContact() {var lining=site.slots[9];if(!EggPresent||lining==null||!site.Rooted(lining))return;liningContact=lining.shape;contactAge=.12f;}
        public void LiningContactEnded(Collider collider){if(liningContact==collider){liningContact=null;contactAge=0;}}
        bool LiningPairPresent()
        {
            var lining=site.slots[9];if(lining==null||lining.shape!=liningContact||!site.Rooted(lining)||liningContact==null||!liningContact.enabled||!liningContact.gameObject.activeInHierarchy||!eggCollider.enabled||!eggCollider.gameObject.activeInHierarchy)return false;
            var bounds=eggCollider.bounds;bounds.Expand((eggCollider.contactOffset+liningContact.contactOffset+.005f)*2);return bounds.Intersects(liningContact.bounds);
        }
        public void Tick(float dt)
        {
            // The actual lining collision pair remains valid through PhysX
            // sleep, until Exit or real collider separation. No forced wake,
            // synthetic support or elapsed-time-only incubation is introduced.
            if(contactAge>0&&!LiningPairPresent()){liningContact=null;contactAge=0;}
            if (EggPresent && (eggBody.position.y < site.transform.position.y + 1 || Vector3.ProjectOnPlane(eggBody.position - site.transform.position, Vector3.up).magnitude > 4)) LoseEgg();
        }
        public void LoseEgg()
        {
            if (!EggPresent || Hatched) return;
            EggPresent = false; eggBody.isKinematic = true; eggCollider.enabled = false; eggRenderer.enabled = false; match.rules.EggLost(site.index);
            if (shellBurst != null) { shellBurst.transform.position = eggBody.position; shellBurst.Play(); }
        }
        public void Hatch(bool celebrate = true)
        {
            if (!EggPresent || !site.CoreReady) return;
            Hatched = true; EggPresent = false; eggBody.isKinematic = true; eggCollider.enabled = false; eggRenderer.enabled = false;
            if (celebrate && shellBurst != null) { shellBurst.transform.position = eggBody.position; shellBurst.Play(); }
            foreach (var baby in babies) baby.gameObject.SetActive(true);
            if (celebrate && tinyHorn != null && tinyHorn.clip != null) { tinyHorn.volume = match.settings.sfx * .18f; tinyHorn.pitch = 1.8f; tinyHorn.Play(); }
        }
        public void RestoreEgg(Vector3 position, Quaternion rotation, bool present, bool hatched)
        {
            ResetFamily(); if (!present && !hatched) return;
            ReceiveEgg(); eggBody.position = position; eggBody.rotation = rotation;
            if (hatched) Hatch(false);
        }
        void LateUpdate()
        {
            if (!Hatched || match.Paused) return;
            WiggleFrames++; float t = Time.time;
            for (int i = 0; i < babies.Length; i++)
            {
                babies[i].position = site.transform.TransformPoint(new Vector3(i == 0 ? -.7f : .7f, 2.3f + Mathf.Abs(Mathf.Sin(t * 2.3f + i)) * .14f, 0));
                babies[i].rotation = site.transform.rotation * Quaternion.Euler(Mathf.Sin(t * 3 + i) * 5, Mathf.Sin(t * 1.5f + i) * 18, Mathf.Sin(t * 2 + i) * 7);
            }
        }
        public void StopSound() { if (tinyHorn != null) tinyHorn.Stop(); }
    }
}
