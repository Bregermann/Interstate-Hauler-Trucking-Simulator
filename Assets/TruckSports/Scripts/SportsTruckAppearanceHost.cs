using System;
using System.Collections.Generic;
using NWH.WheelController3D;
using UnityEngine;

namespace LWS.TruckSports
{
    // An opt-in rendering adapter. Explicit authored bindings protect equipment,
    // cab interiors and full tractor roots from whole-hierarchy replacement.
    [DefaultExecutionOrder(250)]
    public sealed class SportsTruckAppearanceHost : MonoBehaviour
    {
        public SportsTruckAppearanceCatalog catalog;
        public int participantSlot;
        public Transform presentationParent;
        public Renderer[] originalBodyRenderers = Array.Empty<Renderer>();
        public Renderer[] originalWheelRenderers = Array.Empty<Renderer>();
        // Camera-local hiding only; these explicit sport attachments are never
        // hidden, moved or replaced by body composition.
        public Renderer[] auxiliaryExteriorRenderers = Array.Empty<Renderer>();
        public Transform[] protectedRoots = Array.Empty<Transform>();
        public WheelController[] wheels = Array.Empty<WheelController>();
        public SportsTruckStaticWheelPose[] staticWheelPoses = Array.Empty<SportsTruckStaticWheelPose>();
        public SportsTruckAppearanceCompatibility[] compatibleBodies = Array.Empty<SportsTruckAppearanceCompatibility>();
        // A sport can retain required equipment without changing saved item IDs.
        public string[] modeOwnedSlots = Array.Empty<string>();
        public SportsTruckFragmentPresentation fragmentPresentation;
        public string ProfileId { get; private set; }
        public string EffectiveBodyId { get; private set; }
        public int AppearanceRevision { get; private set; }
        public string CompatibilityNotice { get; private set; }
        public Renderer[] ExteriorRenderers { get; private set; } = Array.Empty<Renderer>();
        public bool Applied => model != null;
        public Transform AppearanceRoot => model!=null?model.transform:null;
        public IReadOnlyList<Renderer> ComposedBodyRenderers => paintRenderers;
        GameObject model;
        Renderer[] paintRenderers = Array.Empty<Renderer>();
        SportsTruckAppearanceCompatibility.PaintSlot[] paintSlots = Array.Empty<SportsTruckAppearanceCompatibility.PaintSlot>();
        SportsTruckAppearanceCatalog.Cosmetic dashboard;
        bool[] bodyStates, wheelStates;
        string fingerprint;
        bool usesReplacementWheels;
        Renderer[] composedRenderers=Array.Empty<Renderer>();bool[] composedStates=Array.Empty<bool>();bool fragmented;
        readonly List<WheelVisual> wheelVisuals = new List<WheelVisual>();
        sealed class WheelVisual { public Transform visual, source; public Quaternion alignment; public float verticalOffset; public bool useLocalCenter; public Vector3 localCenter; }

        public bool Prepare(string profileId, Color color)
        {
            var draft = SportsParticipantRegistry.GetAppearance(profileId);
            if (catalog == null) catalog = Resources.Load<SportsTruckAppearanceCatalog>("SportsTruckAppearanceCatalog");
            var report=EvaluateDraft(draft);
            if(!report.canApply){RestoreOriginal();CompatibilityNotice=report.notice;return false;}
            var body=report.body;var fit=report.fit;
            var key = profileId + "|" + body.id + "|" + JsonUtility.ToJson(draft) + "|" + string.Join(",", modeOwnedSlots ?? Array.Empty<string>());
            if(StaticWheelSupport(out var ownerScale)){key+="|static:"+ownerScale.ToString("R",System.Globalization.CultureInfo.InvariantCulture);foreach(var pose in staticWheelPoses)key+="|"+pose.source.GetInstanceID()+":"+pose.center.ToString("R")+":"+pose.diameter.ToString("R",System.Globalization.CultureInfo.InvariantCulture);}
            if (model != null && key == fingerprint) { ReassertIntactPresentation(); ApplyEffectiveColor(color);CompatibilityNotice=report.notice;return PrepareFragments(); }
            CacheOriginal();
            var staged = new GameObject("Sports appearance — " + body.id); staged.SetActive(false);
            staged.transform.SetParent(presentationParent, false);
            staged.transform.SetPositionAndRotation(transform.position, transform.rotation);
            var parentScale = presentationParent.lossyScale; var vehicleScale = transform.lossyScale;
            staged.transform.localScale = new Vector3(vehicleScale.x / parentScale.x, vehicleScale.y / parentScale.y, vehicleScale.z / parentScale.z);
            var following = new List<WheelVisual>();
            var bodyRoot = new GameObject("Body and semantic mounts").transform; bodyRoot.SetParent(staged.transform, false);
            bodyRoot.localPosition = fit.position; bodyRoot.localRotation = Quaternion.Euler(fit.euler); bodyRoot.localScale = fit.scale;
            var canonical = new GameObject("Canonical body").transform; canonical.SetParent(bodyRoot, false);
            canonical.localRotation = Quaternion.Euler(body.presentationEuler);
            Renderer[] bodyParts;
            SportsTruckAppearanceCatalog.Cosmetic nextDashboard = null;
            bool customWheels = false;
            int unavailable = 0;
            try
            {
                var instance = Instantiate(body.visualPrefab, canonical, false);
                bodyParts = instance.GetComponentsInChildren<Renderer>(true);
                // Catalog tire geometry is rebound to existing NWH wheel poses;
                // controller positions, spring travel and tire forces stay intact.
                var tireNames = new HashSet<string>(body.wheelRendererNames ?? Array.Empty<string>(), StringComparer.Ordinal);
                foreach (var renderer in bodyParts) if (tireNames.Contains(renderer.name)) renderer.enabled = false;
                foreach (var selected in draft.selections)
                {
                    if (string.IsNullOrEmpty(selected.itemId)) continue;
                    if (Array.IndexOf(modeOwnedSlots ?? Array.Empty<string>(), selected.slotId) >= 0) { unavailable++; continue; }
                    var item = catalog.FindCosmetic(selected.itemId, selected.slotId, body.id);
                    // Variant IDs are reserved content IDs, never silently applied as another variant.
                    if (item == null || !string.IsNullOrEmpty(selected.variantId)) { unavailable++; continue; }
                    if (selected.slotId == SportsTruckAppearanceSlots.Dashboard) { nextDashboard = item; continue; }
                    if (selected.slotId == SportsTruckAppearanceSlots.Wheels)
                    {
                        if(StaticWheelSupport(out _)&&SupportsWheelItem(body,fit,item))
                        {
                            for(int i=0;i<4;i++)
                            {
                                var pose=staticWheelPoses[i];var pivot=new GameObject("Existing static appearance wheel "+i).transform;pivot.SetParent(staged.transform,false);var tag=pivot.gameObject.AddComponent<SportsTruckAppearanceMountTag>();tag.slotId=SportsTruckAppearanceSlots.Wheels;tag.wheelIndex=i;
                                var graphic=Instantiate(item.visualPrefab,pivot,false).transform;var geometry=Array.Find(fit.wheelGeometry,entry=>entry.physicalWheel==i);graphic.localPosition=item.position;graphic.localRotation=Quaternion.Euler(geometry.euler)*Quaternion.Euler(item.euler);graphic.localScale=item.scale*pose.diameter;
                                pivot.SetPositionAndRotation(pose.source.TransformPoint(pose.center),transform.rotation);following.Add(new WheelVisual{visual=pivot,source=pose.source,alignment=Quaternion.Inverse(pose.source.rotation)*transform.rotation,useLocalCenter=true,localCenter=pose.center});
                            }
                            customWheels=true;continue;
                        }
                        if (!item.rotatesWithWheel || wheels.Length != 4 || Array.Exists(wheels, wheel => wheel == null || wheel.wheel.rotatingContainer == null)) { unavailable++; continue; }
                        for (int i = 0; i < wheels.Length; i++)
                        {
                            var pivot = new GameObject("NWH appearance wheel " + i).transform; pivot.SetParent(staged.transform, false);
                            var wheelTag=pivot.gameObject.AddComponent<SportsTruckAppearanceMountTag>();wheelTag.slotId=SportsTruckAppearanceSlots.Wheels;wheelTag.wheelIndex=i;
                            var graphic = Instantiate(item.visualPrefab, pivot, false).transform;
                            var geometry = Array.Find(fit.wheelGeometry ?? Array.Empty<SportsTruckAppearanceCompatibility.WheelGeometry>(), entry => entry != null && entry.physicalWheel == i);
                            graphic.localPosition = item.position; graphic.localRotation = Quaternion.Euler(geometry != null ? geometry.euler : Vector3.zero) * Quaternion.Euler(item.euler); graphic.localScale = item.scale * (wheels[i].wheel.radius * 2);
                            var source = wheels[i].wheel.rotatingContainer;
                            // Cosmetic content's axle is local X. The existing
                            // controller supplies steering/suspension/spin poses.
                            pivot.SetPositionAndRotation(source.position, transform.rotation);
                            following.Add(new WheelVisual { visual = pivot, source = source, alignment = Quaternion.Inverse(source.rotation) * transform.rotation });
                        }
                        customWheels = true; continue;
                    }
                    var mount = Array.Find(body.mounts ?? Array.Empty<SportsTruckAppearanceCatalog.Mount>(), entry => entry != null && entry.slotId == selected.slotId);
                    if (mount == null) { unavailable++; continue; }
                    var anchor = new GameObject(selected.slotId + " mount").transform; anchor.SetParent(canonical, false);
                    anchor.gameObject.AddComponent<SportsTruckAppearanceMountTag>().slotId=selected.slotId;
                    anchor.localPosition = mount.position; anchor.localRotation = Quaternion.Euler(mount.euler); anchor.localScale = mount.scale;
                    var cosmetic = Instantiate(item.visualPrefab, anchor, false).transform;
                    cosmetic.localPosition = item.position; cosmetic.localRotation = Quaternion.Euler(item.euler); cosmetic.localScale = item.scale;
                }
                if (!customWheels && fit.wheelGeometry != null && fit.wheelGeometry.Length == wheels.Length && wheels.Length == 4)
                {
                    bool ready = true;
                    foreach (var group in fit.wheelGeometry) ready &= group != null && group.physicalWheel >= 0 && group.physicalWheel < wheels.Length &&
                        wheels[group.physicalWheel] != null && wheels[group.physicalWheel].wheel.rotatingContainer != null && group.rendererIndices != null && group.rendererIndices.Length > 0 && group.diameter > .001f && group.visualDiameter > .001f;
                    if (ready)
                    {
                        foreach (var group in fit.wheelGeometry)
                        {
                            var wheel = wheels[group.physicalWheel]; var source = wheel.wheel.rotatingContainer;
                            var pivot = new GameObject("NWH body wheel " + group.physicalWheel).transform; pivot.SetParent(staged.transform, false);
                            var wheelTag=pivot.gameObject.AddComponent<SportsTruckAppearanceMountTag>();wheelTag.slotId=SportsTruckAppearanceSlots.Wheels;wheelTag.wheelIndex=group.physicalWheel;
                            pivot.SetPositionAndRotation(canonical.TransformPoint(group.center), transform.rotation);
                            foreach (int index in group.rendererIndices)
                            {
                                if (index < 0 || index >= bodyParts.Length || !(bodyParts[index] is MeshRenderer renderer) || !renderer.TryGetComponent<MeshFilter>(out var filter) || filter.sharedMesh == null) throw new InvalidOperationException("Authored wheel mesh binding changed.");
                                var part = new GameObject(renderer.name); part.transform.SetParent(pivot, false);
                                part.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
                                var pivotScale = pivot.lossyScale; var size = renderer.transform.lossyScale;
                                part.transform.localScale = new Vector3(size.x / pivotScale.x, size.y / pivotScale.y, size.z / pivotScale.z);
                                part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                                var copy = part.AddComponent<MeshRenderer>(); copy.sharedMaterials = renderer.sharedMaterials; copy.shadowCastingMode = renderer.shadowCastingMode;
                            }
                            float ratio = group.visualDiameter / (group.diameter * Mathf.Abs(canonical.lossyScale.x)); pivot.localScale *= ratio;
                            following.Add(new WheelVisual { visual = pivot, source = source, alignment = Quaternion.Inverse(source.rotation) * transform.rotation, verticalOffset = (group.visualDiameter - wheel.wheel.radius * 2) * .5f });
                        }
                        customWheels = true;
                    }
                }
                if(!customWheels&&StaticWheelSupport(out ownerScale))
                {
                    foreach(var group in fit.wheelGeometry)
                    {
                        var pose=staticWheelPoses[group.physicalWheel];var pivot=new GameObject("Existing static body wheel "+group.physicalWheel).transform;pivot.SetParent(staged.transform,false);var tag=pivot.gameObject.AddComponent<SportsTruckAppearanceMountTag>();tag.slotId=SportsTruckAppearanceSlots.Wheels;tag.wheelIndex=group.physicalWheel;pivot.SetPositionAndRotation(canonical.TransformPoint(group.center),transform.rotation);
                        foreach(int index in group.rendererIndices)
                        {
                            var renderer=bodyParts[index];var filter=renderer.GetComponent<MeshFilter>();var part=new GameObject(renderer.name);part.transform.SetParent(pivot,false);part.transform.SetPositionAndRotation(renderer.transform.position,renderer.transform.rotation);var pivotScale=pivot.lossyScale;var size=renderer.transform.lossyScale;part.transform.localScale=new Vector3(size.x/pivotScale.x,size.y/pivotScale.y,size.z/pivotScale.z);part.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;var copy=part.AddComponent<MeshRenderer>();copy.sharedMaterials=renderer.sharedMaterials;copy.shadowCastingMode=renderer.shadowCastingMode;
                        }
                        if(!SportsTruckAppearanceWheelMath.TryFit(group.visualDiameter,pose.diameter,group.diameter,canonical.lossyScale.x,ownerScale,out var ratio,out var offset))throw new InvalidOperationException("Static wheel fit needs finite positive uniform geometry.");pivot.localScale*=ratio;following.Add(new WheelVisual{visual=pivot,source=pose.source,alignment=Quaternion.Inverse(pose.source.rotation)*transform.rotation,verticalOffset=offset,useLocalCenter=true,localCenter=pose.center});
                    }
                    customWheels=true;
                }
                SetLayer(staged.transform, presentationParent.gameObject.layer);
            }
            catch (Exception exception)
            {
                Dispose(staged); RestoreOriginal(); CompatibilityNotice = "The appearance binding needs compatible content."; Debug.LogWarning("Appearance composition refused: " + exception.Message, this); return false;
            }
            Dispose(model); model = staged; wheelVisuals.Clear(); wheelVisuals.AddRange(following);
            usesReplacementWheels = customWheels;
            foreach (var renderer in originalBodyRenderers) renderer.enabled = false;
            for (int i = 0; i < originalWheelRenderers.Length; i++) originalWheelRenderers[i].enabled = !customWheels && wheelStates[i];
            paintRenderers = bodyParts; paintSlots = fit.paintSlots ?? Array.Empty<SportsTruckAppearanceCompatibility.PaintSlot>();
            dashboard = nextDashboard; ProfileId = profileId; EffectiveBodyId = body.id; fingerprint = key; AppearanceRevision++;
            CompatibilityNotice=report.notice;composedRenderers=model.GetComponentsInChildren<Renderer>(true);composedStates=Array.ConvertAll(composedRenderers,renderer=>renderer.enabled);fragmented=false;
            model.SetActive(true); ApplyEffectiveColor(color); RefreshExteriorRenderers(); FollowWheels(); return PrepareFragments();
        }
        bool PrepareFragments(){if(fragmentPresentation==null||fragmentPresentation.PrepareRevision()){RefreshExteriorRenderers();return true;}RestoreOriginal();CompatibilityNotice="This sport is using its original truck because the selected wreck visuals could not be prepared. Your saved choices are retained.";return false;}
        public SportsTruckAppearanceCompatibilityReport EvaluateDraft(SportsTruckAppearanceData draft)
        {
            var result=new SportsTruckAppearanceCompatibilityReport{requestedBodyId=draft?.bodyId??""};var content=catalog!=null?catalog:Resources.Load<SportsTruckAppearanceCatalog>("SportsTruckAppearanceCatalog");
            if(draft==null||!draft.Validate()||presentationParent==null||content==null||!content.Validate(out _)||!SafeBindings()){result.notice="This sport uses its original truck; no compatible appearance binding is available. Saved choices are retained.";return result;}
            var body=content.ResolveBody(draft.bodyId);var fit=Fit(body?.id);string fragmentReason="";
            if(fit==null||!StaticBodyWheelSupport(body,fit)||fragmentPresentation!=null&&!fragmentPresentation.Supports(body,out fragmentReason)){body=content.FindBody(content.defaultBodyId);fit=Fit(body?.id);}
            if(body==null||fit==null||!StaticBodyWheelSupport(body,fit)||fragmentPresentation!=null&&!fragmentPresentation.Supports(body,out _)){result.notice="This sport uses its original truck while compatible body/wreck/wheel visuals are unavailable. Saved choices are retained.";return result;}
            result.canApply=true;result.body=body;result.fit=fit;result.effectiveBodyId=body.id;
            if(body.id!=draft.bodyId&&!string.IsNullOrEmpty(draft.bodyId))result.notice="A compatible body is used here; your saved choice is retained.";
            if(!string.IsNullOrEmpty(fragmentReason))result.notice+=" "+fragmentReason;
            var missing=new List<string>();foreach(var selected in draft.selections)
            {
                if(string.IsNullOrEmpty(selected.itemId))continue;var item=content.FindCosmetic(selected.itemId,selected.slotId,body.id);
                bool supported=Array.IndexOf(modeOwnedSlots??Array.Empty<string>(),selected.slotId)<0&&item!=null&&string.IsNullOrEmpty(selected.variantId);
                if(supported&&selected.slotId==SportsTruckAppearanceSlots.Wheels)supported=SupportsWheelItem(body,fit,item);
                else if(supported&&selected.slotId!=SportsTruckAppearanceSlots.Dashboard)supported=Array.Exists(body.mounts??Array.Empty<SportsTruckAppearanceCatalog.Mount>(),mount=>mount!=null&&mount.slotId==selected.slotId);
                if(!supported)missing.Add(selected.slotId);
            }
            result.unavailableSlots=missing.ToArray();if(missing.Count>0)result.notice+=" "+missing.Count+" saved accessories are unavailable here.";if(!string.IsNullOrEmpty(fit.notice))result.notice+=" "+fit.notice;result.notice=result.notice.Trim();return result;
        }
        public void SetFragmentedPresentation(bool value){fragmented=value;for(int i=0;i<composedRenderers.Length;i++)if(composedRenderers[i]!=null)composedRenderers[i].enabled=!value&&composedStates[i];}
        public void UseOriginalForUnavailableFragments(){RestoreOriginal();CompatibilityNotice="This sport is using its original truck because compatible wreck visuals became unavailable. Your saved choices are retained.";}
        SportsTruckAppearanceCompatibility Fit(string id) => Array.Find(compatibleBodies ?? Array.Empty<SportsTruckAppearanceCompatibility>(), entry => entry != null && entry.Valid && entry.bodyId == id);
        // Original fragment resets restore their original renderer states. An
        // explicitly bound intact host then restores only its rendering choice.
        public bool ReassertIntactPresentation()
        {
            if (model == null) return false;
            SetFragmentedPresentation(false);
            foreach (var renderer in originalBodyRenderers) if (renderer != null) renderer.enabled = false;
            for (int i = 0; i < originalWheelRenderers.Length; i++) if (originalWheelRenderers[i] != null) originalWheelRenderers[i].enabled = !usesReplacementWheels && wheelStates[i];
            model.SetActive(true); FollowWheels(); return true;
        }
        bool SafeBindings()
        {
            if (originalBodyRenderers == null || originalBodyRenderers.Length == 0 || originalWheelRenderers == null || wheels == null) return false;
            bool nwh=wheels.Length==4&&!Array.Exists(wheels,wheel=>wheel==null)&&(staticWheelPoses==null||staticWheelPoses.Length==0);if(!nwh&&!StaticWheelSupport(out _))return false;
            foreach (var renderer in originalBodyRenderers)
            {
                if (renderer == null || !renderer.transform.IsChildOf(transform)) return false;
                foreach (var root in protectedRoots ?? Array.Empty<Transform>()) if (root != null && (renderer.transform == root || renderer.transform.IsChildOf(root))) return false;
            }
            foreach (var renderer in originalWheelRenderers) if (renderer == null || !renderer.transform.IsChildOf(transform)) return false;
            return presentationParent == transform || presentationParent.IsChildOf(transform);
        }
        bool StaticWheelSupport(out float scale)
        {
            scale=0;if(fragmentPresentation!=null||wheels==null||wheels.Length!=0||staticWheelPoses==null||staticWheelPoses.Length!=4||originalWheelRenderers==null||originalWheelRenderers.Length!=4||!SportsTruckAppearanceWheelMath.TryUniformScale(transform.lossyScale,out scale))return false;
            for(int i=0;i<4;i++){var pose=staticWheelPoses[i];if(pose==null||!pose.ValidFor(transform)||Array.Find(originalWheelRenderers,renderer=>renderer!=null&&renderer.transform==pose.source)==null)return false;foreach(var root in protectedRoots??Array.Empty<Transform>())if(root!=null&&(pose.source==root||pose.source.IsChildOf(root)))return false;for(int j=0;j<i;j++)if(staticWheelPoses[j].source==pose.source)return false;}return true;
        }
        bool StaticBodyWheelSupport(SportsTruckAppearanceCatalog.Body body,SportsTruckAppearanceCompatibility fit)
        {
            if(staticWheelPoses==null||staticWheelPoses.Length==0)return true;
            if(!StaticWheelSupport(out var ownerScale)||body==null||body.visualPrefab==null||fit==null||!SportsTruckAppearanceWheelMath.TryUniformScale(fit.scale,out var fitScale)||fit.wheelGeometry==null||fit.wheelGeometry.Length!=4)return false;var renderers=body.visualPrefab.GetComponentsInChildren<Renderer>(true);var occupied=new bool[4];var indices=new HashSet<int>();
            foreach(var group in fit.wheelGeometry){if(group==null||group.physicalWheel<0||group.physicalWheel>=4||occupied[group.physicalWheel]||!SportsTruckAppearanceWheelMath.Finite(group.center)||!SportsTruckAppearanceWheelMath.Finite(group.euler)||!float.IsFinite(group.diameter)||group.diameter<=.0001f||!float.IsFinite(group.visualDiameter)||group.visualDiameter<=.0001f||group.rendererIndices==null||group.rendererIndices.Length==0||!SportsTruckAppearanceWheelMath.TryFit(group.visualDiameter,staticWheelPoses[group.physicalWheel].diameter,group.diameter,ownerScale*fitScale,ownerScale,out _,out _))return false;occupied[group.physicalWheel]=true;foreach(int index in group.rendererIndices)if(index<0||index>=renderers.Length||!indices.Add(index)||Array.IndexOf(body.wheelRendererNames??Array.Empty<string>(),renderers[index].name)<0||!(renderers[index] is MeshRenderer)||!renderers[index].TryGetComponent<MeshFilter>(out var filter)||filter.sharedMesh==null)return false;}return true;
        }
        bool SupportsWheelItem(SportsTruckAppearanceCatalog.Body body,SportsTruckAppearanceCompatibility fit,SportsTruckAppearanceCatalog.Cosmetic item)
        {
            if(item==null||!item.rotatesWithWheel)return false;if(StaticWheelSupport(out _))return StaticBodyWheelSupport(body,fit);return wheels!=null&&wheels.Length==4&&!Array.Exists(wheels,wheel=>wheel==null||wheel.wheel.rotatingContainer==null);
        }
        void CacheOriginal()
        {
            if (bodyStates != null) return;
            bodyStates = Array.ConvertAll(originalBodyRenderers, renderer => renderer.enabled);
            wheelStates = Array.ConvertAll(originalWheelRenderers, renderer => renderer.enabled);
        }
        public bool ApplyEffectiveColor(Color color)
        {
            if (model == null) return false;
            var block = new MaterialPropertyBlock();
            foreach (var slot in paintSlots)
            {
                if (slot.rendererIndex < 0 || slot.rendererIndex >= paintRenderers.Length) continue;
                var renderer = paintRenderers[slot.rendererIndex];
                if (renderer == null || slot.materialIndex < 0 || slot.materialIndex >= renderer.sharedMaterials.Length) continue;
                renderer.GetPropertyBlock(block, slot.materialIndex); block.SetColor("_BaseColor", color); block.SetColor("_Color", color); renderer.SetPropertyBlock(block, slot.materialIndex); block.Clear();
            }
            if(fragmentPresentation!=null)fragmentPresentation.RefreshColors();
            return true;
        }
        public GameObject CreateDashboard(Transform mount)
        {
            if (mount == null || model == null || dashboard == null) return null;
            var item = Instantiate(dashboard.visualPrefab, mount, false);
            item.name = "Profile dashboard — " + dashboard.id;
            item.transform.localPosition = dashboard.position; item.transform.localRotation = Quaternion.Euler(dashboard.euler); item.transform.localScale = dashboard.scale;
            SetLayer(item.transform, mount.gameObject.layer); return item;
        }
        public void RestoreOriginal()
        {
            if(fragmentPresentation!=null)fragmentPresentation.ClearPool();
            bool changed = model != null; Dispose(model); model = null; wheelVisuals.Clear(); dashboard = null; usesReplacementWheels = false;
            if (bodyStates != null)
            {
                for (int i = 0; i < originalBodyRenderers.Length; i++) if (originalBodyRenderers[i] != null) originalBodyRenderers[i].enabled = bodyStates[i];
                for (int i = 0; i < originalWheelRenderers.Length; i++) if (originalWheelRenderers[i] != null) originalWheelRenderers[i].enabled = wheelStates[i];
            }
            ProfileId = EffectiveBodyId = fingerprint = null; paintRenderers = Array.Empty<Renderer>();
            composedRenderers=Array.Empty<Renderer>();composedStates=Array.Empty<bool>();fragmented=false;
            if (changed) AppearanceRevision++;
            RefreshExteriorRenderers();
        }
        void RefreshExteriorRenderers()
        {
            var list = new List<Renderer>(); list.AddRange(originalBodyRenderers ?? Array.Empty<Renderer>()); list.AddRange(originalWheelRenderers ?? Array.Empty<Renderer>());
            foreach (var renderer in auxiliaryExteriorRenderers ?? Array.Empty<Renderer>()) if (renderer != null && !list.Contains(renderer)) list.Add(renderer);
            if(fragmentPresentation!=null)foreach(var renderer in fragmentPresentation.ExteriorRenderers)if(renderer!=null&&!list.Contains(renderer))list.Add(renderer);
            if (model != null) list.AddRange(model.GetComponentsInChildren<Renderer>(true)); ExteriorRenderers = list.ToArray();
        }
        void LateUpdate() => FollowWheels();
        void FollowWheels() { if(fragmented)return;foreach (var wheel in wheelVisuals) if (wheel.visual != null && wheel.source != null) wheel.visual.SetPositionAndRotation((wheel.useLocalCenter?wheel.source.TransformPoint(wheel.localCenter):wheel.source.position) + transform.up * wheel.verticalOffset, wheel.source.rotation * wheel.alignment); }
        void OnDestroy() { RestoreOriginal(); }
        static void SetLayer(Transform root, int layer) { foreach (var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = layer; }
        static void Dispose(GameObject target) { if (target == null) return; target.SetActive(false); target.transform.SetParent(null, true); if (Application.isPlaying) Destroy(target); else DestroyImmediate(target); }
    }
}
