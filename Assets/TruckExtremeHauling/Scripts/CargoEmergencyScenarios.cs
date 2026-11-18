using System;
using UnityEngine;
using LWS.TruckHitchSports;
using LWS.TruckSports;

namespace LWS.TruckExtremeHauling
{
    [Serializable]
    public sealed class CargoEmergencySite
    {
        public string title;
        public GameObject environment;
        public TrailerBody[] loads;
        public Transform[] truckStarts, depots, heatSources;
        public SportsFloatingBody[] flotation;
        public Bounds[] floodedVolumes;
        public float[] dryBankX;
        [Min(0)] public float heatRadius = 24, conditionLossPerSecond = .008f;
        [Min(0)] public float cableReach = 22, bankApproach = 12;
    }

    /// <summary>
    /// Optional authored incidents, selected only by Cargo Crisis options 4/5.
    /// NWH owns all rig/contact/coupling forces; Archimeds owns displacement;
    /// Cargo Crisis owns the existing winch. This adapter owns incident rules.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CargoEmergencyScenarios : MonoBehaviour
    {
        public CargoCrisisMatch match;
        public GameObject defaultSupport;
        public GameObject[] floodedSiteReplacedRoadblocks = Array.Empty<GameObject>();
        public CargoEmergencySite industrialFire, floodedLoading;
        public CargoEmergencySite Active { get; private set; }
        public bool FloodActive => Active != null && Active == floodedLoading;
        public readonly float[] HeatSeconds = new float[4], ImmersedSeconds = new float[4], CableTowMetres = new float[4];
        public readonly bool[] Extracted = new bool[4];
        readonly Vector3[] lastPosition = new Vector3[4];
        readonly bool[] hadWater = new bool[4];
        readonly int[] observedFloatSteps = new int[4];
        readonly bool[] frontTow = new bool[4], originalLandingGear = new bool[4];
        NWH.WheelController3D.WheelController[][] floodWheels;
        bool capturedLandingGear;
        public bool FrontRecoveryTow(int slot)=>FloodActive&&frontTow[slot];
        ParticleSystem[] effects = Array.Empty<ParticleSystem>();
        bool originalSupportActive, capturedSupport;
        bool[] originalRoadblockActive;

        public string Status => FloodActive
            ? "FLOODED LOADING · STAY ON DRY RIGHT BANK · REEL LOAD UP SLOPE BEFORE HITCHING"
            : "INDUSTRIAL FIRE · BURNING FREIGHT DAMAGES CARGO · EXTRACT, HITCH AND DELIVER";
        public string PlayerStatus(int slot) => FloodActive
            ? (Extracted[slot] ? "DRY BANK RECOVERED" : (frontTow[slot]?"FRONT TOW · ":"RIGHTING · ")+"WET " + ImmersedSeconds[slot].ToString("0") + "s · WINCH " + CableTowMetres[slot].ToString("0") + "m")
            : "HEAT " + HeatSeconds[slot].ToString("0") + "s";

        public void Select(int option)
        {
            StopRound();
            if (!capturedSupport)
            {
                originalSupportActive = defaultSupport != null && defaultSupport.activeSelf;
                originalRoadblockActive = new bool[floodedSiteReplacedRoadblocks.Length];
                for(int i=0;i<originalRoadblockActive.Length;i++)originalRoadblockActive[i]=floodedSiteReplacedRoadblocks[i]!=null&&floodedSiteReplacedRoadblocks[i].activeSelf;
                capturedSupport = true;
            }
            if (industrialFire?.environment != null) industrialFire.environment.SetActive(false);
            if (floodedLoading?.environment != null) floodedLoading.environment.SetActive(false);
            if (defaultSupport != null) defaultSupport.SetActive(originalSupportActive);
            for(int i=0;i<originalRoadblockActive.Length;i++)if(floodedSiteReplacedRoadblocks[i]!=null)floodedSiteReplacedRoadblocks[i].SetActive(originalRoadblockActive[i]);
            Active = option == 4 ? industrialFire : option == 5 ? floodedLoading : null;
            if (option < 4) return;
            if (Active?.environment == null || Active.loads == null || Active.loads.Length != 4 || Active.truckStarts == null || Active.truckStarts.Length != 4 || Active.depots == null || Active.depots.Length != 4)
                throw new InvalidOperationException("The selected Cargo Crisis emergency must have four saved native loads, staging positions and depots.");
            if (FloodActive && (Active.flotation == null || Active.flotation.Length != 4 || Active.floodedVolumes == null || Active.floodedVolumes.Length != 4 || Active.dryBankX == null || Active.dryBankX.Length != 4 || defaultSupport == null))
                throw new InvalidOperationException("The saved flooded loading incident is incomplete.");
            Active.environment.SetActive(true);
            if (FloodActive)
            {
                defaultSupport.SetActive(false);
                foreach(var roadblock in floodedSiteReplacedRoadblocks)if(roadblock!=null)roadblock.SetActive(false);
            }
        }

        public void BeginRound()
        {
            Array.Clear(HeatSeconds, 0, 4); Array.Clear(ImmersedSeconds, 0, 4); Array.Clear(CableTowMetres, 0, 4);
            Array.Clear(Extracted, 0, 4); Array.Clear(hadWater, 0, 4);
            Array.Clear(frontTow,0,4);
            if (Active == null) return;
            if(FloodActive&&floodWheels==null){floodWheels=new NWH.WheelController3D.WheelController[4][];for(int i=0;i<4;i++)floodWheels[i]=Active.loads[i].GetComponentsInChildren<NWH.WheelController3D.WheelController>(true);}
            for (int i = 0; i < 4; i++)
            {
                if(FloodActive&&Active.loads[i].Module.module.trailerStand!=null)
                {
                    var stand=Active.loads[i].Module.module.trailerStand;
                    if(!capturedLandingGear)originalLandingGear[i]=stand.activeSelf;
                    stand.SetActive(originalLandingGear[i]);
                }
                lastPosition[i] = Active.loads[i].Body.position;
                if (Active.flotation != null && i < Active.flotation.Length && Active.flotation[i] != null)
                { Active.flotation[i].enabled = false; Active.flotation[i].Clock = 0; observedFloatSteps[i] = Active.flotation[i].Steps; }
            }
            if(FloodActive)capturedLandingGear=true;
            effects = Active.environment.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var effect in effects) { effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); effect.Play(true); }
            SportsSessionTelemetry.GameplayEvent(match.gameObject.scene.name, "cargo_emergency_selected", "", Active.title, match.option);
        }

        public void Step(int slot, TrailerBody load, SpringJoint cable, float dt)
        {
            if (Active == null || slot < 0 || slot >= match.setup.count || load == null || dt <= 0) return;
            Vector3 current = load.Body.position;
            if (cable != null && cable.connectedBody == load.Body && cable.currentForce.sqrMagnitude > 10000)
                CableTowMetres[slot] += Vector3.Distance(current, lastPosition[slot]);
            lastPosition[slot] = current;
            if (!FloodActive)
            {
                float heat = HeatAt(load.Body.worldCenterOfMass);
                if (heat > 0) { HeatSeconds[slot] += dt * heat; load.DamageCargo(dt * heat * Active.conditionLossPerSecond); }
                return;
            }
            var floating = Active.flotation[slot];
            floating.Clock += dt;
            // The public vendor adapter has a flat surface; restrict its authority to
            // the actual authored flooded bay instead of supporting cargo on dry land.
            bool within = Active.floodedVolumes[slot].Contains(current) && current.y < floating.waterHeight + 5;
            floating.enabled = within && !Extracted[slot];
            if (floating.enabled && floating.Steps > observedFloatSteps[slot] && floating.WetTriangles > 0)
            { ImmersedSeconds[slot] += dt; hadWater[slot] = true; }
            observedFloatSteps[slot] = floating.Steps;
            bool allWheelsDry=floodWheels[slot].Length>0;
            foreach(var wheel in floodWheels[slot])allWheelsDry&=wheel.IsGrounded&&wheel.transform.position.x>=Active.dryBankX[slot];
            // Native trailer origins are below the tyre contact plane. A load
            // standing on every real dry-bank wheel must not be rejected merely
            // because its authored root pivot is below world zero.
            if (!Extracted[slot] && hadWater[slot] && CableTowMetres[slot] >= 5 && allWheelsDry && current.x >= Active.dryBankX[slot] && Vector3.Dot(load.transform.up, Vector3.up) > .9f)
            {
                Extracted[slot] = true; floating.enabled = false;
                SportsSessionTelemetry.GameplayEvent(match.gameObject.scene.name, "cargo_flood_extracted", match.setup.players[slot].participantId,
                    "native cable tow, vendor immersion and dry-bank wheel support", CableTowMetres[slot]);
            }
        }
        public float HeatAt(Vector3 point)
        {
            if (Active == null || FloodActive || Active.heatSources == null || Active.heatRadius <= 0) return 0;
            float heat = 0;
            foreach (var source in Active.heatSources)
                if (source != null) heat = Mathf.Max(heat, Mathf.Clamp01(1 - Vector3.Distance(source.position, point) / Active.heatRadius));
            return heat;
        }
        public bool HitchReady(int slot, TrailerBody load) => !FloodActive || Extracted[slot] && load.WheelContact;
        public Vector3 FrontTowDestination(int slot)=>new Vector3(Active.dryBankX[slot]+35,0,Active.floodedVolumes[slot].max.z-10);
        public void SetFloodTowPhase(int slot,TrailerBody load,bool front)
        {
            if(!FloodActive||match.winches[slot]==null||match.winches[slot].connectedBody!=load.Body)return;
            frontTow[slot]=front;
            if(front&&load.Module.module.trailerStand!=null)load.Module.module.trailerStand.SetActive(false);
        }
        public void ParkExtractedLandingGear(int slot,TrailerBody load)
        {if(FloodActive&&Extracted[slot]&&load.Module.module.trailerStand!=null)load.Module.module.trailerStand.SetActive(originalLandingGear[slot]);}
        public Vector3 DryBankApproach(int slot, TrailerBody load)
        {
            if (!FloodActive) throw new InvalidOperationException("A dry-bank approach belongs only to the authored flooded loading site.");
            var bay = Active.floodedVolumes[slot];
            // Floating hull lean is not a reliable shore normal. The saved extraction
            // slope rises toward +X, and the service lane begins at dryBankX.
            return new Vector3(Active.dryBankX[slot] + 3, 0, Mathf.Clamp(load.Body.position.z - 2, bay.min.z + 7, bay.max.z - 7));
        }
        public bool DeliveryReady(int slot, TrailerBody load) => load.CargoCondition > 0 && (!FloodActive || Extracted[slot] && load.WheelContact);
        public string DiagnosticState(int slot)
        {
            if(Active==null)return "ordinary site";
            var load=Active.loads[slot];var body=load.Body;var truck=match.trucks[slot].body;
            var cable=match.winches[slot];int contacts=0;string wheels="";
            foreach(var wheel in load.GetComponentsInChildren<NWH.WheelController3D.WheelController>(true))if(wheel.IsGrounded){contacts++;wheels+=(wheel.HitCollider!=null?wheel.HitCollider.name:"unknown")+"@"+wheel.transform.position.ToString("F2")+";";}
            string state="cargoEuler="+body.rotation.eulerAngles.ToString("F1")+" cargoUp="+load.transform.up.ToString("F2")+" spin="+body.angularVelocity.ToString("F2")
                +" wheelContacts="+contacts+" ["+wheels+"] truckEuler="+truck.rotation.eulerAngles.ToString("F1");
            if(cable!=null)state+=" cableMax="+cable.maxDistance.ToString("F2")+" cableForce="+cable.currentForce.ToString("F1")+" anchor="+truck.transform.TransformPoint(cable.anchor).ToString("F2")+" eye="+(cable.connectedBody==body?body.transform.TransformPoint(cable.connectedAnchor).ToString("F2"):"not connected");
            if(FloodActive)state+=" frontTow="+frontTow[slot]+" jackDeployed="+(load.Module.module.trailerStand!=null&&load.Module.module.trailerStand.activeSelf)+" dryBankX="+Active.dryBankX[slot]+" hullMinY="+Active.flotation[slot].proxy.bounds.min.y+" wetTriangles="+Active.flotation[slot].WetTriangles;
            return state;
        }
        public void StopRound()
        {
            if (industrialFire?.flotation != null) foreach (var floating in industrialFire.flotation) if (floating != null) floating.enabled = false;
            if (floodedLoading?.flotation != null) foreach (var floating in floodedLoading.flotation) if (floating != null) floating.enabled = false;
            foreach (var effect in effects) if (effect != null) effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            // Keep the selected saved venue/results visible. The next round's explicit
            // selection restores original support and disables the previous incident.
        }
    }
}
