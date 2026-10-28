using System;
using System.Linq;
using UnityEngine;
using LWS.TruckCamping;
using LWS.TruckSports;
namespace LWS.TruckPetPals
{
    // A render-only miniature companion on a NavMeshAgent, not a new vehicle
    // controller. The owner's real full NWH tractor is unaffected by tricks.
    public sealed class PetTruckArt : MonoBehaviour
    {
        public Transform assembly;
        public Transform[] wheels = Array.Empty<Transform>();
        public Transform face, rearRibbon;
        Transform sleepFace;
        public AudioSource cuteHorn;
        public CampPose Pose { get; private set; }
        public float ActualLegLift { get; private set; }
        public float ActualContextAngle { get; private set; }
        public float ActualKickAngle { get; private set; }
        public float ActualLowering { get; private set; }
        public int AnimatedFrames { get; private set; }
        public string ResolvedBodyId { get; private set; }
        Vector3 restPosition, prior;
        Quaternion restRotation;
        Vector3[] wheelPositions;
        Quaternion[] wheelRotations;
        float progress, spin;
        bool initialized;
        bool capturedRest;
        GameObject currentModel;
        public GameObject CurrentModel => currentModel;
        int frontLeft;
        public void SelectBody(string requested, Color color)
        {
            if (!capturedRest) { restPosition=assembly.localPosition; restRotation=assembly.localRotation; capturedRest=true; }
            assembly.localPosition=restPosition; assembly.localRotation=restRotation;
            var catalog = Resources.Load<SportsTruckAppearanceCatalog>("SportsTruckAppearanceCatalog");
            if (catalog == null) throw new InvalidOperationException("Existing Sports render-only appearance catalog required.");
            var body = catalog.ResolveBody(requested); if (body == null || body.visualPrefab == null) throw new InvalidOperationException("The standard pet truck model is unavailable.");
            if (currentModel != null) { currentModel.SetActive(false); Destroy(currentModel); }
            currentModel = Instantiate(body.visualPrefab, assembly, false); currentModel.transform.localRotation = Quaternion.Euler(body.presentationEuler);
            if (currentModel.GetComponentsInChildren<Rigidbody>(true).Length > 0 || currentModel.GetComponentsInChildren<Collider>(true).Length > 0) throw new InvalidOperationException("Pet art must remain render-only.");
            var rendering = currentModel.GetComponentsInChildren<Renderer>(true); Bounds bounds = LocalRenderedBounds(assembly,rendering);
            float scale = 3.2f / Mathf.Max(bounds.size.x, bounds.size.z); currentModel.transform.localScale *= scale;
            bounds=LocalRenderedBounds(assembly,rendering);currentModel.transform.localPosition -= new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor", color); block.SetColor("_Color", color);
            foreach (var r in rendering) for (int i = 0; i < r.sharedMaterials.Length; i++) if (SportsTruckAppearanceCompatibility.IsPaintMaterial(body.id, r.sharedMaterials[i])) r.SetPropertyBlock(block, i);
            var mount = Array.Find(body.mounts ?? Array.Empty<SportsTruckAppearanceCatalog.Mount>(), m => m.slotId == "face.eyes");
            face = sleepFace = null;
            if (mount != null)
            {
                var anchor = new GameObject("Existing compatible pet eye mount").transform; anchor.SetParent(currentModel.transform, false); anchor.localPosition = mount.position; anchor.localRotation = Quaternion.Euler(mount.euler); anchor.localScale = mount.scale;
                face = Eyes(catalog.FindCosmetic("eyes.friendly", "face.eyes", body.id), anchor);
                sleepFace = Eyes(catalog.FindCosmetic("eyes.sleepy", "face.eyes", body.id), anchor);
                if (sleepFace != null) sleepFace.gameObject.SetActive(false);
            }
            Configure(body, currentModel);
        }
        public static Bounds LocalRenderedBounds(Transform space,Renderer[] rendering)
        {
            bool first=true;var result=new Bounds();
            foreach(var r in rendering)
            {
                var filter=r.GetComponent<MeshFilter>();var skin=r as SkinnedMeshRenderer;var mesh=filter!=null?filter.sharedMesh:skin!=null?skin.sharedMesh:null;if(mesh==null)continue;
                var b=mesh.bounds;var matrix=space.worldToLocalMatrix*r.localToWorldMatrix;
                for(int i=0;i<8;i++){var point=matrix.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));if(first){result=new Bounds(point,Vector3.zero);first=false;}else result.Encapsulate(point);}
            }
            if(first)throw new InvalidOperationException("Actual pet body mesh bounds are unavailable.");return result;
        }
        static Transform Eyes(SportsTruckAppearanceCatalog.Cosmetic item, Transform anchor)
        {
            if (item == null) return null; var art = Instantiate(item.visualPrefab, anchor, false).transform;
            art.localPosition = item.position; art.localRotation = Quaternion.Euler(item.euler); art.localScale = item.scale; return art;
        }
        public void Configure(SportsTruckAppearanceCatalog.Body body, GameObject model)
        {
            ResolvedBodyId = body.id;
            if (body.wheelMounts == null || body.wheelMounts.Length != 4 || body.wheelRendererNames == null) throw new InvalidOperationException("A pet truck needs the existing catalog's exact four rolling groups.");
            var all = model.GetComponentsInChildren<Renderer>(true);
            wheels = new Transform[4];
            var selected = all.Where(r => Array.IndexOf(body.wheelRendererNames, r.name) >= 0).ToArray();
            var centers = selected.Select(r => {var filter=r.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)throw new InvalidOperationException("Explicit pet rolling mesh missing.");return model.transform.InverseTransformPoint(r.transform.TransformPoint(filter.sharedMesh.bounds.center));}).ToArray();
            for (int group = 0; group < 4; group++)
            {
                int index = group; var mount = body.wheelMounts[group]; var pivot = new GameObject("Owned miniature rolling pivot " + mount.slotId).transform; pivot.SetParent(model.transform, false); pivot.localPosition = mount.position; pivot.localRotation = Quaternion.Euler(mount.euler);
                var members = Enumerable.Range(0, selected.Length).Where(r => Enumerable.Range(0, 4).OrderBy(m => (body.wheelMounts[m].position - centers[r]).sqrMagnitude).First() == index).ToArray();
                if (members.Length == 0) throw new InvalidOperationException("Missing actual pet tire/rim geometry for " + body.id + "/" + mount.slotId);
                foreach (int r in members) selected[r].transform.SetParent(pivot, true);
                wheels[group] = pivot;
            }
            var canonical = body.wheelMounts.Select(m => Quaternion.Euler(body.presentationEuler) * m.position).ToArray();
            frontLeft = Enumerable.Range(0, 4).OrderByDescending(i => canonical[i].z).Take(2).OrderBy(i => canonical[i].x).First();
            initialized = false; Initialize();
        }
        void Initialize()
        {
            if (initialized || assembly == null) return;
            initialized = true; if(!capturedRest){restPosition = assembly.localPosition; restRotation = assembly.localRotation;capturedRest=true;} prior = transform.position;
            wheelPositions = new Vector3[wheels.Length]; wheelRotations = new Quaternion[wheels.Length];
            for (int i = 0; i < wheels.Length; i++) { wheelPositions[i] = wheels[i].localPosition; wheelRotations[i] = wheels[i].localRotation; }
        }
        public void Request(CampPose pose, float phase) { Initialize(); Pose = pose; progress = phase; }
        public void Honk(float volume) { if (cuteHorn == null || cuteHorn.clip == null || cuteHorn.isPlaying) return; cuteHorn.pitch = 1.9f; cuteHorn.volume = Mathf.Clamp01(volume) * .18f; cuteHorn.Play(); }
        void LateUpdate()
        {
            Initialize(); if (!initialized || Time.deltaTime <= 0) return;
            AnimatedFrames++; ActualLegLift = ActualContextAngle = ActualKickAngle = 0;
            if (face != null) face.gameObject.SetActive(Pose != CampPose.Sleep);
            if (sleepFace != null) sleepFace.gameObject.SetActive(Pose == CampPose.Sleep);
            spin += Vector3.Distance(prior, transform.position) * 360 / 1.2f; prior = transform.position;
            Vector3 offset = Vector3.zero, euler = Vector3.zero;
            switch (Pose)
            {
                case CampPose.Sit: offset.y = -.16f; euler.x = -16; break;
                case CampPose.LieDown: case CampPose.Sleep: offset.y = -.24f; euler.x = 3; break;
                case CampPose.RollOver: euler.z = Mathf.SmoothStep(0, 1, Mathf.Clamp01(progress)) * 360; offset.y = Mathf.Sin(Mathf.PI * progress) * .65f; break;
                case CampPose.Shake: euler.z = -9; euler.y = Mathf.Sin(Time.time * 8) * 3; break;
                case CampPose.Dig: euler.x = 12 + Mathf.Sin(Time.time * 12) * 6; break;
                case CampPose.Pee: euler.z = -20 * Mathf.Clamp01(progress); euler.y = -8 * Mathf.Clamp01(progress); break;
                case CampPose.GrassKick: euler.x = Mathf.Sin(Time.time * 15) * 5; offset.z = -Mathf.Abs(Mathf.Sin(Time.time * 9)) * .15f; break;
                case CampPose.BathShake: euler.y = Mathf.Sin(Time.time * 20) * 14; euler.z = Mathf.Sin(Time.time * 20) * 6; break;
                case CampPose.Happy: offset.y = Mathf.Abs(Mathf.Sin(Time.time * 7)) * .18f; euler.y = Mathf.Sin(Time.time * 9) * 8; break;
                case CampPose.Sniff: euler.x = 9; euler.y = Mathf.Sin(Time.time * 4) * 12; break;
                case CampPose.Look: euler.y = Mathf.Sin(Time.time * 2) * 15; break;
            }
            assembly.localPosition = restPosition + offset; assembly.localRotation = restRotation * Quaternion.Euler(euler);
            ActualLowering = Mathf.Max(0, restPosition.y - assembly.localPosition.y);
            ActualContextAngle = Quaternion.Angle(restRotation, assembly.localRotation);
            for (int i = 0; i < wheels.Length; i++)
            {
                bool raised = i == frontLeft && (Pose == CampPose.Shake || Pose == CampPose.Pee);
                var lift = raised ? wheels[i].parent.InverseTransformVector(transform.up * .22f * (Pose == CampPose.Pee ? Mathf.Clamp01(progress) : 1)) : Vector3.zero;
                wheels[i].localPosition = wheelPositions[i] + lift;
                float rotation = Pose == CampPose.GrassKick || Pose == CampPose.Dig ? -Mathf.Sin(Time.time * 18 + i) * 70 : spin;
                wheels[i].localRotation = wheelRotations[i] * Quaternion.Euler(rotation, 0, 0);
                if (Pose == CampPose.GrassKick || Pose == CampPose.Dig) ActualKickAngle = Mathf.Max(ActualKickAngle, Quaternion.Angle(wheelRotations[i], wheels[i].localRotation));
                ActualLegLift = Mathf.Max(ActualLegLift, Vector3.Distance(wheels[i].parent.TransformPoint(wheelPositions[i]), wheels[i].position));
            }
            if (rearRibbon != null) rearRibbon.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * (Pose == CampPose.Happy ? 12 : 4)) * 24, 0);
        }
    }
}
