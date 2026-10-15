using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using NWH.VehiclePhysics2;
using NWH.VehiclePhysics2.Effects;
using UnityEngine;

namespace LWS.TruckTaxi
{
    [DisallowMultipleComponent]
    public sealed class TruckTaxiTruckFactory : MonoBehaviour
    {
        private TruckTaxiBootstrap host;
        private VehicleController vehicle;
        private Rigidbody body;
        private LwsPlayerTruck player;
        private RigSnapshot original;
        private bool applying;
        private bool rollbackFailed;
        private readonly List<LwsKeyboardGamepadTruckInputSource> faultInputs=new List<LwsKeyboardGamepadTruckInputSource>();
        public TruckTaxiCareerCatalog Catalog => host?.CareerEquipment?.Catalog;
        public Material[] CurrentPaintMaterials { get; private set; }=Array.Empty<Material>();
        public event Action ModelChanged;
        public string LastError { get; private set; }

        public void Initialize(TruckTaxiBootstrap owner)
        {
            if(host!=null)
            {
                if(host!=owner) throw new InvalidOperationException("Cannot rebind an initialized truck factory.");
                return;
            }
            if(owner==null || owner.Player==null || owner.Session==null || owner.Handling==null || owner.Configuration==null)
                throw new ArgumentException("Truck factory requires the initialized canonical rig and Career session.");
            var controller=owner.Player.GetComponent<VehicleController>();
            var rigidbody=owner.Player.GetComponent<Rigidbody>();
            if(controller==null || rigidbody==null || owner.Player.GetComponent<TruckTaxiCabOccupantRig>()==null)
                throw new ArgumentException("Canonical NWH vehicle, Rigidbody and occupant rig are required.");
            var snapshot=new RigSnapshot(owner);
            host=owner; player=host.Player; vehicle=controller; body=rigidbody;
            original=snapshot;
            CurrentPaintMaterials=host.Configuration.truckBodyMaterials;
            host.Session.CareerEquipmentChanged+=CareerChanged;
        }
        public bool CanSwitch(out string reason,bool ownedModal=false)
        {
            if(rollbackFailed) { reason=LastError; return false; }
            reason="Park on supported ground and finish the active fare before changing trucks.";
            if(!isActiveAndEnabled || host?.Ready!=true || body==null || vehicle==null || applying ||
                host.Player!=player || host.Session==null || host.Session.EquippedTruckId!=player.DefinitionId ||
                host.spawner==null || host.spawner.SpawnedTruck!=player || host.Persistence?.HasActiveRun!=true ||
                host.Persistence.IsBusy || host.Persistence.ResumeBlocked || TruckTaxiWorldTravel.IsBusy ||
                host.Session.CareerRunProfileId!=host.Persistence.RunProfileId || !vehicle.IsInitialized ||
                vehicle.powertrain.wheelCount==0 || !vehicle.IsFullyGrounded() ||
                host.Session.State!=TruckTaxiState.Available || host.Session.HasPassenger ||
                host.Races?.IsActive==true || host.WorldLife!=null && host.WorldLife.Mode!=TruckTaxiPlayerControlMode.Driving ||
                body.linearVelocity.sqrMagnitude>.04f || body.angularVelocity.sqrMagnitude>.01f ||
                Vector3.Dot(body.transform.up,Vector3.up)<.85f) return false;
            reason="Finish vehicle recovery, travel or the active interaction before changing trucks.";
            if(host.Kaiju?.IsCapturingPlayer==true || host.Fuel?.IsRescuing==true || host.Roadside?.IsRecovering==true ||
                host.FastTravel?.IsTravelling==true || host.Companions?.IsBusy==true || host.Companions?.ActivePrivateStop!=null ||
                host.CustomContent?.OwnsInputHold==true || host.DriverNeeds?.IsSleeping==true ||
                host.Companions?.HasOnboardCompanion==true || host.Companions?.SleeperSequenceActive==true ||
                host.Player.GetComponent<TruckTaxiCabOccupantRig>()?.PrivateTransitionActive==true ||
                !ownedModal && host.Paused) return false;
            reason="Uncouple and safely park the trailer before changing trucks.";
            if(host.Player.CouplingAdapter==null || host.Player.CouplingAdapter.IsTrailerAttached) return false;
            reason="Let passengers and companions leave the cab before changing trucks.";
            var occupants=host.Player.GetComponent<TruckTaxiCabOccupantRig>();
            if(occupants==null || occupants.UsedCapacity>1) return false;
            reason=null; return true;
        }
        public bool TryEquip(string id,out string reason,bool ownedModal=false)
        {
            if(!CanSwitch(out reason,ownedModal)) return false;
            var profile=host.Session.CaptureCareerProfile();
            var definition=Catalog?.FindTruck(id)?.truck;
            if(profile?.trucks.Exists(t=>t.truckId==id)!=true)
            { reason="The selected truck is not owned in this Career."; return false; }
            if(!TruckTaxiTruckModel.ValidatePlayerPrefab(definition,out reason)) return false;
            if(id==host.Player.DefinitionId) { reason=null; return true; }
            var model=definition.PlayerPrefab.GetComponent<TruckTaxiTruckModel>();
            if(model!=null && !HasClearance(model)) { reason="The new truck needs more clear space. Park in the open dealership bay."; return false; }
            var previous=host.Player.Definition;
            applying=true;
            try
            {
                if(!CommitSelection(host.Session,Catalog,id,()=>ApplyDefinition(definition),()=>RestorePrevious(previous),out reason))
                { LastError=reason; return false; }
                LastError=reason; reason=null;
                if(LastError!=null) Debug.LogWarning(LastError,this);
                RefreshCommittedSelection();
                return true;
            }
            finally { applying=false; }
        }
        // Session publishes after committing. A throwing subscriber must not roll back only the physical actor.
        private static bool CommitSelection(TruckTaxiSession session,TruckTaxiCareerCatalog catalog,string id,
            Action apply,Action rollback,out string reason)
        {
            try { apply(); }
            catch(Exception error) { return RollBack(rollback,"Truck preparation failed: "+error.Message,out reason); }
            try
            {
                if(session.TryEquipTruck(catalog,id,out reason)) return true;
            }
            catch(Exception error)
            {
                if(session.EquippedTruckId==id)
                { reason="Truck selection committed, but a Career observer failed: "+error.Message; return true; }
                reason="Career selection failed: "+error.Message;
            }
            return RollBack(rollback,reason,out reason);
        }
        private static bool RollBack(Action rollback,string failure,out string reason)
        {
            try { rollback(); reason=failure+" Previous truck restored."; }
            catch(Exception error) { reason=failure+" Rollback also failed: "+error.Message; }
            return false;
        }
        private void RestorePrevious(LwsTruckDefinition previous)
        {
            try { ApplyDefinition(previous); }
            catch
            {
                // An unrecoverable rig must not resume driving when its modal lease closes.
                rollbackFailed=true; body.isKinematic=true;
                foreach(var input in player.GetComponentsInChildren<LwsKeyboardGamepadTruckInputSource>(true))
                    if(!faultInputs.Contains(input)) { faultInputs.Add(input); input.AcquireDrivingInputSuppression(this); }
                throw;
            }
        }
        private void RefreshCommittedSelection()
        {
            RunPostCommit(()=>host.CareerEquipment.Refresh());
            RunPostCommit(()=>host.Player.GetComponent<TruckTaxiGaragePaint>()?.Refresh());
            RunPostCommit(()=>host.ComponentDamage?.ReapplyPhysicalState());
            RunPostCommit(()=>host.GPS?.RequestPresentationRefresh());
            if(ModelChanged!=null)
                foreach(Action observer in ModelChanged.GetInvocationList()) RunPostCommit(observer);
            RunPostCommit(()=>
            {
                var result=host.Persistence.RequestAutosave("Truck dealership equipment");
                if(!result.Succeeded) Debug.LogWarning("Truck equipped; autosave failed: "+result.Message,this);
            });
        }
        private void RunPostCommit(Action action)
        {
            try { action(); }
            catch(Exception error) { LastError="Truck remains equipped; presentation/save observer failed: "+error.Message; Debug.LogException(error,this); }
        }
        public bool RestoreSelected(out string reason)
        {
            reason="Truck factory is not initialized or another model transaction is active.";
            if(host==null || original==null || applying || rollbackFailed) return false;
            if(!host.Session.TryMigrateLegacyStarterIdentity(out reason)) return false;
            reason=null;
            var id=host?.Session?.EquippedTruckId;
            if(string.IsNullOrEmpty(id) || id==host.Player.DefinitionId) return true;
            var definition=Catalog?.FindTruck(id)?.truck;
            if(!TruckTaxiTruckModel.ValidatePlayerPrefab(definition,out reason)) return false;
            var previous=host.Player.Definition;
            applying=true;
            try
            {
                ApplyDefinition(definition);
                if(ModelChanged!=null) foreach(Action observer in ModelChanged.GetInvocationList()) RunPostCommit(observer);
                return true;
            }
            catch(Exception e)
            {
                RollBack(()=>RestorePrevious(previous),"Saved truck could not be applied: "+e.Message,out reason);
                LastError=reason; return false;
            }
            finally { applying=false; }
        }
        private void CareerChanged()
        {
            // Live selection is a validated factory transaction. Loading explicitly
            // applies here before component damage, fuel and final load clearance.
            if(!applying && host.Persistence?.IsBusy!=true && host.Session.EquippedTruckId==host.Player.DefinitionId)
                host.Player.GetComponent<TruckTaxiGaragePaint>()?.Refresh();
        }
        private bool HasClearance(TruckTaxiTruckModel model)
        {
            foreach(var bounds in new[]{model.cabinCollision,model.hoodCollision})
                foreach(var hit in Physics.OverlapBox(body.transform.TransformPoint(bounds.center),bounds.extents*.98f,body.rotation,~0,QueryTriggerInteraction.Ignore))
                    if(hit!=null && !hit.transform.IsChildOf(body.transform)) return false;
            return true;
        }
        private void ApplyDefinition(LwsTruckDefinition definition)
        {
            if(definition==null) throw new ArgumentException("Missing authored truck definition.");
            bool kinematic=body.isKinematic;
            Vector3 linear=body.linearVelocity,angular=body.angularVelocity;
            body.isKinematic=true;
            try
            {
                host.Player.GetComponent<TruckTaxiGaragePaint>()?.RestoreDefault();
                host.Handling.Restore();
                original.Restore(host);
                var model=definition.PlayerPrefab.GetComponent<TruckTaxiTruckModel>();
                original.TrackReplacedRenderers(host.Player.transform,model);
                if(model!=null && !model.retainsOriginalBody) TruckTaxiTruckRig.Apply(host.Player.gameObject,model);
                host.Player.Configure(definition,host.Player.VehicleId);
                host.Player.DashboardController?.Configure(definition.DashboardDefinition);
                if(model!=null && !model.retainsOriginalBody)
                {
                    host.Handling.lowSpeedSteeringLock=model.lowSpeedSteeringLock;
                    host.Handling.citySteeringLock=model.citySteeringLock;
                    host.Handling.highSpeedSteeringLock=model.highwaySteeringLock;
                    host.Handling.steeringResponse=model.steeringResponse;
                    host.Handling.launchPowerAtRest=model.launchPowerAtRest;
                    host.Handling.launchPowerAt15Mph=model.launchPowerAt15Mph;
                }
                host.Handling.Apply();
                vehicle.wheelbase=vehicle.steering.CalculateWheelbase();
                CurrentPaintMaterials=model!=null ? model.primaryPaintMaterials : host.Configuration.truckBodyMaterials;
                var paint=host.Player.GetComponent<TruckTaxiGaragePaint>();
                paint?.Configure(host.Player,CurrentPaintMaterials,host.Session,Catalog);
                ConfigureOccupants(model?.retainsOriginalBody==true ? null : model);
                host.GPS?.SetVehicleDashboardMount(model!=null && !model.retainsOriginalBody
                    ? TruckTaxiTruckRig.Find(host.Player.transform,LwsCabGpsController.DefaultGpsAnchorId) : null);
                host.ComponentDamage?.ReapplyPhysicalState();
                host.GPS?.RequestPresentationRefresh();
                Physics.SyncTransforms();
                host.GetComponent<TruckTaxiSnowSurface>()?.MarkDirty();
                LastError=null;
            }
            finally
            {
                body.isKinematic=kinematic;
                if(!kinematic) { body.linearVelocity=linear; body.angularVelocity=angular; }
            }
        }
        private void ConfigureOccupants(TruckTaxiTruckModel model)
        {
            var rig=host.Player.GetComponent<TruckTaxiCabOccupantRig>();
            if(rig==null || rig.UsedCapacity>1) throw new InvalidOperationException("Cab occupants could not be safely transferred.");
            var driver=host.Player.GetComponent<TruckTaxiDriverPresentation>();
            var actor=driver?.Occupant;
            if(actor!=null) rig.Release(actor);
            if(model==null) rig.Configure(original.Sleeper,original.Anchors);
            else
            {
                var kinds=TruckTaxiCabOccupantKind.Passenger|TruckTaxiCabOccupantKind.Companion|TruckTaxiCabOccupantKind.ExtraPassenger|TruckTaxiCabOccupantKind.StolenPassenger;
                var anchors=new List<TruckTaxiCabOccupantRig.Anchor>();
                anchors.Add(Seat(TruckTaxiCabAnchorId.DriverSeat,"driver",model.driverSeat,TruckTaxiCabOccupantKind.Driver,false));
                anchors.Add(Seat(TruckTaxiCabAnchorId.FrontPassengerSeat,"front-passenger",model.frontPassengerSeat,kinds,true));
                if(model.hasSleeper)
                {
                    var left=Seat(TruckTaxiCabAnchorId.SleeperLeft,"sleeper-driver",model.sleeperDriver,TruckTaxiCabOccupantKind.Driver,false);
                    left.privateEventEligible=true; anchors.Add(left);
                    var right=Seat(TruckTaxiCabAnchorId.SleeperRight,"sleeper-companion",model.companionSeat,kinds,true);
                    right.privateEventEligible=true; anchors.Add(right);
                }
                rig.Configure(model.hasSleeper,anchors.ToArray());
            }
            if(actor!=null)
            {
                if(model==null) original.RestoreDriver(rig,actor);
                else if(!rig.TrySeat(actor,TruckTaxiCabOccupantKind.Driver,TruckTaxiCabAnchorId.DriverSeat,
                    TruckTaxiSeatType.StandardPassenger,original.DriverWorldScale))
                    throw new InvalidOperationException("Driver could not return to the authored seat.");
            }
        }
        private static TruckTaxiCabOccupantRig.Anchor Seat(TruckTaxiCabAnchorId id,string slot,Vector3 position,TruckTaxiCabOccupantKind kinds,bool secondary) =>
            new TruckTaxiCabOccupantRig.Anchor{ id=id,slotId="factory."+slot,mountPath="",localPosition=position,
                localScale=kinds==TruckTaxiCabOccupantKind.Driver ? Vector3.one : Vector3.one*.88f,
                allowedOccupants=kinds,secondaryOccupantSeat=secondary,sharedPrimarySeat=id==TruckTaxiCabAnchorId.FrontPassengerSeat,
                occupantRadius=.32f,firstPersonVisible=id!=TruckTaxiCabAnchorId.DriverSeat,
                allowedSeatTypes=new[]{TruckTaxiSeatType.StandardPassenger,TruckTaxiSeatType.CompanionPassenger,TruckTaxiSeatType.TinyPassenger,TruckTaxiSeatType.RobotPassenger} };

        public GameObject CreatePreview(string id,Transform parent)
        {
            var entry=Catalog?.FindTruck(id);
            if(entry==null || !entry.HasContent) return null;
            var owned=host.Session.CaptureCareerProfile()?.trucks.Find(t=>t.truckId==id);
            var model=entry.truck.PlayerPrefab.GetComponent<TruckTaxiTruckModel>();
            return CopyPreview(entry.truck.PlayerPrefab,parent,model!=null ? model.primaryPaintMaterials : null,
                owned?.hasCustomPaint==true ? owned.paint : null);
        }
        private static GameObject CopyPreview(GameObject prefab,Transform parent,Material[] paintMaterials,TruckTaxiGaragePaintData paint)
        {
            var result=new GameObject("Truck dealership visual preview"); result.transform.SetParent(parent,false);
            try
            {
                var lod0=new HashSet<Renderer>(); var lower=new HashSet<Renderer>();
                foreach(var group in prefab.GetComponentsInChildren<LODGroup>(true))
                {
                    var levels=group.GetLODs();
                    for(int level=0;level<levels.Length;level++)
                        foreach(var renderer in levels[level].renderers)
                            if(renderer!=null) { if(level==0) lod0.Add(renderer); else lower.Add(renderer); }
                }
                foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    var source=filter.GetComponent<MeshRenderer>();
                    if(source==null || !source.enabled || !TruckTaxiTruckRig.AuthoredActive(filter.transform,prefab.transform) ||
                        filter.sharedMesh==null || lower.Contains(source) && !lod0.Contains(source)) continue;
                    // Copy geometry, not prefab behaviours. LOD membership and authored local activation are authoritative.
                    var node=new GameObject(filter.name); node.transform.SetParent(result.transform,false);
                    var relative=prefab.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                    node.transform.localPosition=relative.GetColumn(3); node.transform.localRotation=relative.rotation;
                    node.transform.localScale=relative.lossyScale;
                    node.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
                    var rendered=node.AddComponent<MeshRenderer>(); rendered.sharedMaterials=source.sharedMaterials;
                    var block=new MaterialPropertyBlock(); source.GetPropertyBlock(block); rendered.SetPropertyBlock(block);
                    var materials=source.sharedMaterials;
                    for(int i=0;i<materials.Length;i++)
                    {
                        block.Clear(); source.GetPropertyBlock(block,i);
                        if(block.isEmpty) source.GetPropertyBlock(block);
                        var material=materials[i];
                        if(paint!=null && paint.Validate() && material!=null && paintMaterials!=null && Array.IndexOf(paintMaterials,material)>=0)
                        {
                            string property=material.HasProperty("_BaseColor") ? "_BaseColor" : material.HasProperty("_Color") ? "_Color" : null;
                            if(property!=null) block.SetColor(property,paint.color);
                        }
                        rendered.SetPropertyBlock(block,i);
                    }
                }
                return result;
            }
            catch
            {
                if(Application.isPlaying) Destroy(result); else DestroyImmediate(result);
                throw;
            }
        }
        private void OnDestroy()
        {
            if(host?.Session!=null) host.Session.CareerEquipmentChanged-=CareerChanged;
            if(player!=null)
                foreach(var resources in player.GetComponentsInChildren<TruckTaxiRigLampMaterials>(true)) resources.Release();
            foreach(var input in faultInputs) if(input!=null) input.ReleaseDrivingInputSuppression(this);
        }

        private sealed class RigSnapshot
        {
            private readonly List<Action> restore=new List<Action>();
            private readonly Dictionary<MeshRenderer,bool> replacedRenderers=new Dictionary<MeshRenderer,bool>();
            public readonly bool Sleeper;
            public readonly TruckTaxiCabOccupantRig.Anchor[] Anchors;
            public readonly Vector3 DriverWorldScale;
            private readonly Transform driverParent;
            private readonly Vector3 driverPosition,driverScale;
            private readonly Quaternion driverRotation;
            private readonly TruckTaxiCabOccupantRig.Anchor driverAnchor;
            public RigSnapshot(TruckTaxiBootstrap owner)
            {
                var root=owner.Player;
                foreach(var t in root.GetComponentsInChildren<Transform>(true))
                {
                    var target=t; var p=t.localPosition; var r=t.localRotation;
                    // Never restore the tractor world pose or moving wheel visual poses.
                    bool wheel=t.name.StartsWith("Whl HD ") && t.name.EndsWith("_WheelController");
                    bool binding=t.name=="TrailerAttachmentPoint" || t.name=="InsideCamera" || t.name=="Vehicle Camera Driver" || t.GetComponent<Camera>()!=null ||
                        t==root.DashboardController?.SteeringWheelVisual || t.GetComponent<Light>()!=null || t.name=="IH_CabAnchor_GpsMount" || t.GetComponent<NWH.VehiclePhysics2.VehicleGUI.AnalogGauge>()!=null;
                    if(!wheel && !binding) continue;
                    bool rotates=t.GetComponent<NWH.VehiclePhysics2.VehicleGUI.AnalogGauge>()!=null;
                    restore.Add(()=>{ if(target!=null) { target.localPosition=p; if(rotates) target.localRotation=r; } });
                }
                foreach(var collider in root.GetComponentsInChildren<BoxCollider>(true))
                {
                    if(collider.transform!=root.transform.Find("Cab") && collider.transform!=TruckTaxiTruckRig.Find(root.transform,"Chassis")) continue;
                    var center=collider.center; var size=collider.size;
                    restore.Add(()=>{if(collider!=null){collider.center=center;collider.size=size;}});
                }
                float mass=root.GetComponent<Rigidbody>().mass; restore.Add(()=>root.GetComponent<Rigidbody>().mass=mass);
                var placeholder=TruckTaxiTruckRig.Find(root.transform,LwsTruckCabInteriorRecovery.SleeperPlaceholderRootName);
                if(placeholder!=null){bool active=placeholder.gameObject.activeSelf;restore.Add(()=>{if(placeholder!=null)placeholder.gameObject.SetActive(active);});}
                var mirrors=root.MirrorController;
                var left=TruckTaxiTruckRig.Find(root.transform,"MirrorGlassL")?.GetComponent<Renderer>();
                var right=TruckTaxiTruckRig.Find(root.transform,"MirrorGlassR")?.GetComponent<Renderer>();
                if(mirrors!=null && left!=null && right!=null) restore.Add(()=>mirrors.RebindSurfaces(left,right));
                foreach(var light in TruckTaxiTruckRig.LightGroups(root.GetComponent<VehicleController>()))
                {
                    if(light==null) continue;
                    var values=new List<LightSource>(light.lightSources);
                    restore.Add(()=>light.lightSources=new List<LightSource>(values));
                }
                var lod=root.GetComponent<LODGroup>();
                var levels=lod!=null ? lod.GetLODs() : Array.Empty<UnityEngine.LOD>();
                bool lodEnabled=lod!=null && lod.enabled;
                Vector3 lodCenter=lod!=null ? lod.localReferencePoint : Vector3.zero;
                float lodSize=lod!=null ? lod.size : 0;
                restore.Add(()=>
                {
                    var current=root.GetComponent<LODGroup>();
                    if(current==null) return;
                    current.SetLODs(levels); current.localReferencePoint=lodCenter; current.size=lodSize; current.enabled=lodEnabled;
                });
                var h=owner.Handling; float low=h.lowSpeedSteeringLock,city=h.citySteeringLock,high=h.highSpeedSteeringLock,response=h.steeringResponse,rest=h.launchPowerAtRest,at15=h.launchPowerAt15Mph;
                restore.Add(()=>{h.lowSpeedSteeringLock=low;h.citySteeringLock=city;h.highSpeedSteeringLock=high;h.steeringResponse=response;h.launchPowerAtRest=rest;h.launchPowerAt15Mph=at15;});
                var rig=root.GetComponent<TruckTaxiCabOccupantRig>(); Sleeper=rig.AuthoredSleeperCapability;
                Anchors=new List<TruckTaxiCabOccupantRig.Anchor>(rig.Anchors).ToArray();
                var actor=root.GetComponent<TruckTaxiDriverPresentation>()?.Occupant;
                DriverWorldScale=actor!=null ? actor.lossyScale : Vector3.one;
                if(actor!=null)
                {
                    driverParent=actor.parent; driverPosition=actor.localPosition; driverRotation=actor.localRotation; driverScale=actor.localScale;
                    if(rig.TryGetSlot(actor,out string slot)) driverAnchor=Array.Find(Anchors,a=>a.StableSlotId==slot);
                }
            }
            public void TrackReplacedRenderers(Transform root,TruckTaxiTruckModel model)
            {
                if(model==null || model.retainsOriginalBody || model.replacedSourceMeshes==null) return;
                foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
                    if(filter.sharedMesh!=null && Array.IndexOf(model.replacedSourceMeshes,filter.sharedMesh)>=0 &&
                        filter.TryGetComponent(out MeshRenderer renderer) && !replacedRenderers.ContainsKey(renderer))
                        replacedRenderers.Add(renderer,renderer.enabled);
            }
            public void RestoreDriver(TruckTaxiCabOccupantRig rig,Transform actor)
            {
                if(driverAnchor!=null)
                {
                    Vector3 scale=driverAnchor.localScale;
                    Vector3 standing=new Vector3(DriverWorldScale.x/Mathf.Max(.01f,scale.x),
                        DriverWorldScale.y/Mathf.Max(.01f,scale.y),DriverWorldScale.z/Mathf.Max(.01f,scale.z));
                    if(!rig.TrySeat(actor,TruckTaxiCabOccupantKind.Driver,driverAnchor.id,TruckTaxiSeatType.StandardPassenger,standing))
                        throw new InvalidOperationException("Driver could not return to the original seat.");
                }
                else
                {
                    actor.SetParent(driverParent,false); actor.SetLocalPositionAndRotation(driverPosition,driverRotation); actor.localScale=driverScale;
                }
            }
            public void Restore(TruckTaxiBootstrap owner)
            {
                TruckTaxiTruckRig.RemoveAuthored(owner.Player.transform);
                foreach(var entry in replacedRenderers) if(entry.Key!=null) entry.Key.enabled=entry.Value;
                foreach(var action in restore) action();
            }
        }
    }
}
