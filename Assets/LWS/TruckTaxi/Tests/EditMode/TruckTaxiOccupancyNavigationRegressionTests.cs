using System.Collections.Generic;
using System.Reflection;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiOccupancyNavigation")]
    public sealed class TruckTaxiOccupancyNavigationRegressionTests
    {
        private readonly List<GameObject> objects=new List<GameObject>();
        private GameObject Make(string name)
        { var go=new GameObject(name); objects.Add(go); return go; }
        private TruckTaxiCabOccupantRig Starter()
        {
            var truck=Make("explicit starter");
            Make("Cab").transform.SetParent(truck.transform,false);
            var rig=truck.AddComponent<TruckTaxiCabOccupantRig>();
            Assert.IsTrue(TruckTaxiEightOccupantLayout.TryApplyToEmptyStarter(rig,out var reason),reason);
            return rig;
        }
        private TruckTaxiPassengerActor Actor(string id)
        {
            var actor=Make(id).AddComponent<TruckTaxiPassengerActor>(); actor.Bind(null);
            actor.SetLifecycle(TruckTaxiActorLifecycle.Boarding); return actor;
        }
        private TruckTaxiGPSAdapter GPS()
        {
            var gps=Make("GPS").AddComponent<TruckTaxiGPSAdapter>();
            typeof(TruckTaxiGPSAdapter).GetField("navigation",BindingFlags.Instance|BindingFlags.NonPublic)
                .SetValue(gps,new LwsNavigationService());
            typeof(TruckTaxiGPSAdapter).GetField("player",BindingFlags.Instance|BindingFlags.NonPublic)
                .SetValue(gps,gps.transform);
            return gps;
        }
        [TearDown] public void Cleanup()
        {
            for(int i=objects.Count-1;i>=0;i--) if(objects[i]!=null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }
        private static void InvokeDisable(MonoBehaviour component)
        {
            // Ordinary MonoBehaviours do not execute their PlayMode lifecycle in EditMode.
            // Invoke the actual callback, not a substitute cleanup path; repeat calls must be safe.
            var callback=component.GetType().GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.IsNotNull(callback);
            callback.Invoke(component,null);
        }

        [TestCase(0)]
        [TestCase(6)]
        public void DemoSceneConfiguredFallbackGetsEightRealSlotsBeforePassengerInitialization(int companionIndex)
        {
            var scene=EditorSceneManager.OpenPreviewScene("Assets/LWS/TruckTaxi/Scenes/TruckTaxi_DemoCity.unity");
            try
            {
                TruckTaxiBootstrap host=null;
                foreach(var root in scene.GetRootGameObjects())
                {
                    host=root.GetComponentInChildren<TruckTaxiBootstrap>(true);
                    if(host!=null) break;
                }
                Assert.IsNotNull(host); Assert.IsNotNull(host.spawner);
                var config=new SerializedObject(host.spawner);
                Assert.IsNull(config.FindProperty("truckDefinition").objectReferenceValue,
                    "This regression covers the actual Taxi fallback path, not the separate starter definition asset.");
                var resolve=typeof(LwsPlayerTruckSpawner).GetMethod("ResolveTruckPrefab",BindingFlags.Instance|BindingFlags.NonPublic);
                var prefab=(GameObject)resolve.Invoke(host.spawner,null);
                Assert.AreEqual("Assets/LWS/TruckTaxi/Prefabs/TruckTaxi_InterstateTractor.prefab",AssetDatabase.GetAssetPath(prefab));
                var authored=prefab.GetComponent<TruckTaxiCabOccupantRig>();
                Assert.IsNotNull(authored);
                Assert.AreEqual(1,authored.AuthoredCapacity,"Baseline must exercise the sparse authored prefab that failed in Build2.");
                var sourceCab=prefab.transform.Find("Cab"); Assert.IsNotNull(sourceCab);
                // Copy the configured prefab's occupancy data/mount only. Do not start NWH,
                // world generation or Bootstrap in an EditMode semantic regression test.
                var truck=Make("IH_PlayerTruck_NWH_Runtime");
                var cab=Make("Cab").transform; cab.SetParent(truck.transform,false);
                cab.localPosition=sourceCab.localPosition; cab.localRotation=sourceCab.localRotation; cab.localScale=sourceCab.localScale;
                var rig=truck.AddComponent<TruckTaxiCabOccupantRig>();
                EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(authored),rig);
                var original=rig.Anchors[0];
                var originalJson=JsonUtility.ToJson(original);
                var identity=truck.AddComponent<LwsVehicleIdentity>();
                var vehicleId=config.FindProperty("spawnedVehicleId").stringValue;
                identity.Configure(vehicleId,TruckTaxiEightOccupantLayout.FallbackDefinitionId,LwsVehicleRole.PlayerTractor,"Taxi fallback",true);
                var player=truck.AddComponent<LwsPlayerTruck>(); player.Configure(null,vehicleId);
                Assert.AreEqual("ih.truck.nwh.euro.semi",player.DefinitionId);
                Assert.IsTrue(TruckTaxiEightOccupantLayout.TryApplyToConfiguredStarter(player,out var reason),reason);
                Assert.AreEqual(8,rig.AuthoredCapacity); Assert.AreEqual(8,rig.Anchors.Count);
                Assert.AreSame(original,rig.Anchors[0]);
                Assert.AreEqual(authored.AuthoredSleeperCapability,rig.AuthoredSleeperCapability);
                var mixedRoles=new[] { TruckTaxiCabOccupantKind.Passenger,TruckTaxiCabOccupantKind.StolenPassenger,
                    TruckTaxiCabOccupantKind.BonusObjectiveCompanion,TruckTaxiCabOccupantKind.ExtraPassenger,
                    TruckTaxiCabOccupantKind.Passenger,TruckTaxiCabOccupantKind.ExtraPassenger };
                int nextRole=0;
                for(int i=0;i<7;i++)
                {
                    var actor=Actor("configured.person-"+i);
                    bool companion=i==companionIndex;
                    var kind=companion ? TruckTaxiCabOccupantKind.Companion : mixedRoles[nextRole++];
                    var type=companion ? TruckTaxiSeatType.CompanionPassenger : TruckTaxiSeatType.StandardPassenger;
                    if(companion)
                    {
                        if(i==6)
                        {
                            Assert.AreEqual(7,rig.UsedCapacity);
                            Assert.IsFalse(rig.HasSecondarySeat(TruckTaxiSeatType.StandardPassenger,null,TruckTaxiCabOccupantKind.Passenger),
                                "A companion-only free slot must not be advertised as a seventh arbitrary fare seat.");
                        }
                        Assert.IsTrue(rig.TryReserveSecondary(actor.transform,type,kind,"configured.companion"));
                        var reserved=new List<TruckTaxiOccupantSlotSnapshot>(); rig.CopyOccupancySnapshot(reserved);
                        var row=reserved.Find(s=>s.OccupantId=="configured.companion");
                        Assert.AreEqual(original.StableSlotId,row.SlotId,"Reserve the exclusive seat even when the front seat has higher priority.");
                        Assert.AreEqual(TruckTaxiOccupantSlotState.Reserved,row.State);
                    }
                    Assert.IsTrue(rig.TrySeatSecondary(actor.transform,type,actor.StandingWorldScale,kind),"Actual compatible slot "+i);
                    actor.CompleteSeating();
                    Assert.IsTrue(rig.TryGetSlot(actor.transform,out var slot));
                    if(companion) Assert.AreEqual(original.StableSlotId,slot);
                    Assert.IsTrue(actor.transform.IsChildOf(cab));
                    Assert.AreEqual(i+2,rig.UsedCapacity);
                }
                Assert.AreEqual(8,rig.UsedCapacity);
                Assert.IsFalse(rig.HasSecondarySeat(TruckTaxiSeatType.StandardPassenger,null,TruckTaxiCabOccupantKind.Companion));
                var ninth=Actor("ninth");
                Assert.IsFalse(rig.TrySeatSecondary(ninth.transform,TruckTaxiSeatType.StandardPassenger,ninth.StandingWorldScale,TruckTaxiCabOccupantKind.Passenger));
                Assert.IsNull(ninth.transform.parent);
                Assert.AreEqual(originalJson,JsonUtility.ToJson(original),"Preserve every original anchor field, not just its role mask.");
                Assert.IsTrue(TruckTaxiEightOccupantLayout.TryApplyToConfiguredStarter(player,out reason),reason);
                Assert.AreEqual(7,rig.OccupiedCount,"Repeated startup hook must not clear occupied slots.");
                Assert.AreEqual("ih.truck.nwh.euro.semi",identity.DefinitionId,"Do not migrate save/career identity to install seats.");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void CompanionExclusivePreferencePreservesExistingSharedSeatReservation()
        {
            var root=Make("reserved companion seats"); Make("Cab").transform.SetParent(root.transform,false);
            var rig=root.AddComponent<TruckTaxiCabOccupantRig>();
            var templates=TruckTaxiEightOccupantLayout.CreateInterstateTractorAnchors();
            var exclusive=templates[3]; exclusive.allowedOccupants=TruckTaxiCabOccupantKind.Companion;
            rig.Configure(false,new[] { templates[0],templates[1],exclusive });
            var first=Actor("first"); var second=Actor("second");
            Assert.IsTrue(rig.TryReserveSecondary(first.transform,TruckTaxiSeatType.CompanionPassenger,TruckTaxiCabOccupantKind.Companion,"first"));
            Assert.IsTrue(rig.TryReserveSecondary(second.transform,TruckTaxiSeatType.CompanionPassenger,TruckTaxiCabOccupantKind.Companion,"second"));
            var rows=new List<TruckTaxiOccupantSlotSnapshot>(); rig.CopyOccupancySnapshot(rows);
            Assert.AreEqual(exclusive.StableSlotId,rows.Find(s=>s.OccupantId=="first").SlotId);
            Assert.AreEqual(templates[1].StableSlotId,rows.Find(s=>s.OccupantId=="second").SlotId);
            rig.Release(first.transform);
            Assert.IsTrue(rig.TrySeatSecondary(second.transform,TruckTaxiSeatType.CompanionPassenger,second.StandingWorldScale));
            second.CompleteSeating();
            Assert.IsTrue(rig.TryGetSlot(second.transform,out var retained));
            Assert.AreEqual(templates[1].StableSlotId,retained,"A freed preferred seat must not override the actor's concrete reservation.");
            Assert.IsTrue(rig.TrySeatSecondary(first.transform,TruckTaxiSeatType.CompanionPassenger,first.StandingWorldScale));
            first.CompleteSeating();
            Assert.IsTrue(rig.TryGetSlot(first.transform,out var chosen)); Assert.AreEqual(exclusive.StableSlotId,chosen);
            Assert.IsFalse(rig.HasSecondarySeat(TruckTaxiSeatType.CompanionPassenger));
        }

        [Test]
        public void GenericFallbackIdentityWithoutTaxiAuthoredCabIsNotAnOptIn()
        {
            var truck=Make("IH_PlayerTruck_NWH_Runtime"); Make("Cab").transform.SetParent(truck.transform,false);
            var identity=truck.AddComponent<LwsVehicleIdentity>();
            identity.Configure("player.truck.starter",TruckTaxiEightOccupantLayout.FallbackDefinitionId,LwsVehicleRole.PlayerTractor,"Other truck",true);
            var player=truck.AddComponent<LwsPlayerTruck>(); player.Configure(null,identity.VehicleId);
            Assert.IsFalse(TruckTaxiEightOccupantLayout.TryApplyToConfiguredStarter(player,out var reason));
            Assert.IsNotEmpty(reason); Assert.IsNull(truck.GetComponent<TruckTaxiCabOccupantRig>());
        }

        [Test]
        public void AuthoredEightMeansPlayerPlusSevenMixedCompatiblePeopleNotEightPassengers()
        {
            var rig=Starter();
            Assert.AreEqual(8,rig.AuthoredCapacity);
            Assert.IsFalse(rig.HasSleeperCapability,"Travel layout must not invent private-event capability.");
            var roles=new[] { TruckTaxiCabOccupantKind.StolenPassenger,TruckTaxiCabOccupantKind.Companion,
                TruckTaxiCabOccupantKind.Passenger,TruckTaxiCabOccupantKind.ExtraPassenger,
                TruckTaxiCabOccupantKind.BonusObjectiveCompanion,TruckTaxiCabOccupantKind.Passenger,
                TruckTaxiCabOccupantKind.ExtraPassenger };
            for(int i=0;i<roles.Length;i++)
            {
                var actor=Actor("person-"+i);
                Assert.IsTrue(rig.TrySeatSecondary(actor.transform,TruckTaxiSeatType.StandardPassenger,
                    actor.StandingWorldScale,roles[i]),"slot "+i);
                actor.CompleteSeating();
                Assert.IsTrue(rig.SetOccupantIdentity(actor.transform,"person-"+i));
                Assert.AreEqual(i+2,rig.UsedCapacity);
            }
            Assert.IsFalse(rig.HasSecondarySeat(TruckTaxiSeatType.StandardPassenger,null,TruckTaxiCabOccupantKind.StolenPassenger));
            var snapshot=new List<TruckTaxiOccupantSlotSnapshot>(); rig.CopyOccupancySnapshot(snapshot);
            Assert.AreEqual(8,snapshot.Count);
            Assert.AreEqual("player",snapshot[0].OccupantId);
            Assert.AreEqual(TruckTaxiOccupantSlotState.Reserved,snapshot[0].State);
            Assert.AreEqual(7,rig.OccupiedCount);
            Assert.IsTrue(TruckTaxiEightOccupantLayout.TryApplyToEmptyStarter(rig,out _),"Already-eight layout is an idempotent no-op.");
            Assert.AreEqual(7,rig.OccupiedCount,"Idempotent application must not reset live ownership.");
        }

        [Test]
        public void LayoutMergePreservesDriverFrontPrivatePosesAndAuthoredSleeperCapability()
        {
            var root=Make("authored starter"); Make("Cab").transform.SetParent(root.transform,false);
            var rig=root.AddComponent<TruckTaxiCabOccupantRig>();
            var templates=TruckTaxiEightOccupantLayout.CreateInterstateTractorAnchors();
            var driver=templates[0]; driver.localEulerAngles=new Vector3(0,7,0);
            var front=templates[1]; front.localPosition.x=.5f;
            var original=templates[3]; original.slotId="original.private"; original.privateEventEligible=true;
            original.allowedOccupants=TruckTaxiCabOccupantKind.Companion;
            original.occupantRadius=.35f;
            original.pose=new[] { new TruckTaxiCabOccupantRig.PosePart { path="Head",localEulerAngles=Vector3.up*12 } };
            rig.Configure(true,new[] { driver,front,original }); rig.enabled=false;
            Assert.IsTrue(TruckTaxiEightOccupantLayout.TryApplyToEmptyStarter(rig,out var reason),reason);
            Assert.AreEqual(8,rig.AuthoredCapacity);
            Assert.IsTrue(rig.AuthoredSleeperCapability,"Disabled runtime state must not erase authored capability.");
            Assert.AreSame(driver,rig.Anchors[0]); Assert.AreSame(front,rig.Anchors[1]); Assert.AreSame(original,rig.Anchors[2]);
            Assert.AreEqual(7,driver.localEulerAngles.y); Assert.AreEqual(.5f,front.localPosition.x);
            Assert.IsTrue(original.privateEventEligible);
            Assert.AreEqual(TruckTaxiCabOccupantKind.Companion,original.allowedOccupants);
            Assert.AreEqual("Head",original.pose[0].path);
            Assert.IsTrue(TruckTaxiEightOccupantLayout.TryApplyToEmptyStarter(rig,out _));
            Assert.AreSame(original,rig.Anchors[2]);
        }

        [Test]
        public void UnsafeMergeFailsWithoutChangingExistingAuthorship()
        {
            var root=Make("crowded authored cab"); Make("Cab").transform.SetParent(root.transform,false);
            var rig=root.AddComponent<TruckTaxiCabOccupantRig>();
            var original=new TruckTaxiCabOccupantRig.Anchor { id=TruckTaxiCabAnchorId.SleeperRight,
                mountPath="Cab",slotId="private.original",occupantRadius=100,privateEventEligible=true };
            rig.Configure(true,new[] { original });
            Assert.IsFalse(TruckTaxiEightOccupantLayout.TryApplyToEmptyStarter(rig,out var reason));
            Assert.IsNotEmpty(reason); Assert.AreEqual(1,rig.Anchors.Count);
            Assert.AreSame(original,rig.Anchors[0]); Assert.IsTrue(rig.HasSleeperCapability);
        }

        [Test]
        public void NormalFarePairIdentityUsesExistingProfileIdsAndRebindsAfterReseat()
        {
            var rig=Starter(); var first=Actor("visual name is not identity"); var second=Actor("pair visual");
            var primary=ScriptableObject.CreateInstance<PassengerProfile>();
            var paired=ScriptableObject.CreateInstance<PassengerProfile>();
            try
            {
                primary.passengerId="fare.authored.primary"; paired.passengerId="fare.authored.pair";
                Assert.IsFalse(TruckTaxiPassengerRuntime.BindFareIdentity(first,rig.transform,primary),"No phantom occupant before placement.");
                Assert.IsTrue(rig.TrySeatPassengerPair(first,null,second,null));
                Assert.IsTrue(TruckTaxiPassengerRuntime.BindFareIdentity(first,rig.transform,primary));
                Assert.IsTrue(TruckTaxiPassengerRuntime.BindFareIdentity(second,rig.transform,paired));
                var rows=new List<TruckTaxiOccupantSlotSnapshot>(); rig.CopyOccupancySnapshot(rows);
                Assert.AreEqual(1,rows.FindAll(s=>s.OccupantId==primary.passengerId).Count);
                Assert.AreEqual(1,rows.FindAll(s=>s.OccupantId==paired.passengerId).Count);
                first.PrepareForWorld(rig.transform);
                first.PrepareForSeat();
                Assert.IsTrue(TruckTaxiSeatProfile.TryPlaceActor(first.transform,rig.transform,null,false,first.StandingWorldScale));
                first.CompleteSeating();
                Assert.IsTrue(TruckTaxiPassengerRuntime.BindFareIdentity(first,rig.transform,primary));
                rig.CopyOccupancySnapshot(rows);
                Assert.AreEqual(1,rows.FindAll(s=>s.OccupantId==primary.passengerId).Count);
                primary.passengerId="";
                Assert.IsFalse(TruckTaxiPassengerRuntime.BindFareIdentity(first,rig.transform,primary),"Never invent a missing authored ID.");
            }
            finally { Object.DestroyImmediate(primary); Object.DestroyImmediate(paired); }
        }

        [Test]
        public void BoardingReservationCommitsSameSlotAndCancelRestoresOnlyItsCapacity()
        {
            var rig=Starter(); var first=Actor("companion"); var second=Actor("stolen");
            Assert.IsTrue(rig.TryReserveSecondary(first.transform,TruckTaxiSeatType.CompanionPassenger,
                TruckTaxiCabOccupantKind.Companion,"companion.1"));
            var snapshot=new List<TruckTaxiOccupantSlotSnapshot>(); rig.CopyOccupancySnapshot(snapshot);
            string reserved=snapshot.Find(s=>s.OccupantId=="companion.1").SlotId;
            Assert.AreEqual(2,rig.UsedCapacity);
            Assert.IsNull(first.transform.parent,"Reserving must not teleport the boarding actor.");
            Assert.IsTrue(rig.TrySeatSecondary(second.transform,TruckTaxiSeatType.StandardPassenger,
                second.StandingWorldScale,TruckTaxiCabOccupantKind.StolenPassenger));
            Assert.IsTrue(rig.TrySeatSecondary(first.transform,TruckTaxiSeatType.CompanionPassenger,first.StandingWorldScale));
            Assert.IsTrue(rig.TryGetSlot(first.transform,out var committed)); Assert.AreEqual(reserved,committed);
            first.CompleteSeating(); second.CompleteSeating();
            rig.Release(first.transform);
            Assert.AreEqual(2,rig.UsedCapacity,"Released child visuals must not count as a second legacy passenger.");
            Assert.IsTrue(rig.TryGetSlot(second.transform,out _));
        }

        [Test]
        public void ExitingActorCannotLeaveStaleSeatOrClearanceButConcealedSeatedActorKeepsSeat()
        {
            var rig=Starter(); var actor=Actor("fare");
            Assert.IsTrue(rig.TrySeatSecondary(actor.transform,TruckTaxiSeatType.StandardPassenger,
                actor.StandingWorldScale,TruckTaxiCabOccupantKind.Passenger)); actor.CompleteSeating();
            Assert.IsTrue(rig.TryGetSlot(actor.transform,out var slot));
            actor.gameObject.SetActive(false);
            Assert.IsTrue(rig.IsOccupied(slot),"Visual concealment is not an exit.");
            actor.SetLifecycle(TruckTaxiActorLifecycle.Exiting);
            Assert.IsFalse(rig.IsOccupied(slot));
            Assert.AreEqual(1,rig.UsedCapacity);
            Assert.IsTrue(rig.HasSecondarySeat(TruckTaxiSeatType.StandardPassenger,null,TruckTaxiCabOccupantKind.StolenPassenger));
        }

        [Test]
        public void CompanionProjectionSurvivesPickupAndTokenGuardedCompletionRestoresFare()
        {
            var gps=GPS();
            var fare=gps.gameObject.AddComponent<TruckTaxiRideLocation>(); fare.locationId="fare";
            var companion=gps.gameObject.AddComponent<TruckTaxiStopObjectivePoint>();
            companion.stableId="motel"; companion.displayName="Companion motel";
            companion.category=TruckTaxiStopCategory.PrivateMeeting;
            gps.SetRideDestination(fare); Assert.IsTrue(gps.SetPrivateStopDestination(companion));
            long token=gps.CompanionLeaseToken;
            gps.SetPickupDestination(fare);
            var objectives=new List<TruckTaxiNavigationObjective>(); gps.CopyNavigationObjectives(objectives);
            Assert.That(objectives.Exists(o=>o.TargetId=="motel" && !o.IsForeground));
            Assert.AreEqual("Companion motel",gps.CompanionDestinationLabel);
            Assert.IsFalse(gps.ClearPrivateStopDestination("motel",token+1));
            gps.SetRideDestination(fare);
            Assert.AreEqual("motel",gps.TargetId);
            Assert.IsTrue(gps.ClearPrivateStopDestination("motel",token));
            Assert.AreEqual("fare",gps.TargetId);
        }

        [Test]
        public void BeamInitializationBeforeAwakeIsIdempotentAndRefreshBeforeInitializeIsSafe()
        {
            var gps=GPS();
            var root=Make("inactive beams"); root.SetActive(false);
            var beams=root.AddComponent<TruckTaxiObjectiveBeams>();
            Assert.DoesNotThrow(()=>beams.Refresh());
            Assert.AreEqual(0,beams.VisibleCount);
            Assert.DoesNotThrow(()=>beams.Initialize(gps));
            Assert.DoesNotThrow(()=>beams.Initialize(gps));
            LogAssert.NoUnexpectedReceived();
            root.SetActive(true);
            gps.SyncRaceCheckpoint(true,Vector3.zero,0,3);
            beams.Refresh();
            Assert.AreEqual(1,beams.VisibleCount,beams.Diagnostic);
            beams.Initialize(gps); beams.Refresh();
            Assert.AreEqual(1,root.GetComponentsInChildren<MeshRenderer>().Length);
            var block=new MaterialPropertyBlock();
            root.GetComponentInChildren<MeshRenderer>().GetPropertyBlock(block);
            Color expected=Color.cyan*3, actual=block.GetColor("_Color");
            const float nativeColorTolerance=.00001f;
            Assert.AreEqual(expected.r,actual.r,nativeColorTolerance,"red");
            Assert.AreEqual(expected.g,actual.g,nativeColorTolerance,"green");
            Assert.AreEqual(expected.b,actual.b,nativeColorTolerance,"blue");
            Assert.AreEqual(expected.a,actual.a,nativeColorTolerance,"alpha");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void BeamsUseTrueWorldPositionReuseRenderersAndReleaseWithLease()
        {
            var gps=GPS();
            var beams=gps.gameObject.AddComponent<TruckTaxiObjectiveBeams>(); beams.Initialize(gps);
            // Suspended under Emergency so this fixture tests presentation, not route finding.
            Assert.IsTrue(gps.TrySetNavigationIntent(TruckTaxiNavigationLayer.Emergency,"test","emergency",
                "Emergency",Vector3.zero,TruckTaxiMapMarkerType.ActiveObjective));
            var target=new Vector3(420,12,-900);
            Assert.IsTrue(gps.TrySetNavigationIntent(TruckTaxiNavigationLayer.PlayerService,"player-service","waypoint",
                "Waypoint",target,TruckTaxiMapMarkerType.Place));
            beams.Refresh(); Assert.AreEqual(2,beams.VisibleCount,beams.Diagnostic);
            var renderers=beams.GetComponentsInChildren<MeshRenderer>(); Assert.AreEqual(2,renderers.Length);
            Assert.AreEqual(target.x,renderers[1].bounds.center.x,.01);
            Assert.AreEqual(target.z,renderers[1].bounds.center.z,.01);
            Assert.AreEqual(target.y,renderers[1].bounds.min.y,.01);
            Assert.GreaterOrEqual(renderers[1].bounds.size.y,799);
            Assert.IsEmpty(beams.GetComponentsInChildren<Light>());
            Assert.IsTrue(gps.ReleaseNavigationIntent(TruckTaxiNavigationLayer.PlayerService,"player-service","waypoint"));
            beams.Refresh(); Assert.AreEqual(1,beams.VisibleCount);
            Assert.AreEqual(2,beams.GetComponentsInChildren<MeshRenderer>().Length,"Pool retains reusable geometry.");
            beams.enabled=false;
            InvokeDisable(beams); InvokeDisable(beams);
            Assert.AreEqual(0,beams.VisibleCount);
            foreach(var renderer in renderers) Assert.IsFalse(renderer.enabled);
            beams.Refresh();
            Assert.AreEqual(0,beams.VisibleCount,"Explicit refresh cannot revive a disabled presentation.");
            foreach(var renderer in renderers) Assert.IsFalse(renderer.enabled);
            beams.enabled=true; beams.Refresh();
            Assert.AreEqual(1,beams.VisibleCount);
            CollectionAssert.AreEqual(renderers,beams.GetComponentsInChildren<MeshRenderer>(),"Re-enable reuses the same pool.");
            Assert.IsTrue(renderers[0].enabled); Assert.IsFalse(renderers[1].enabled);
        }
        [Test]
        public void RaceCheckpointUsesLocalWorldPositionUpdatesAndRestoresUnderlyingFare()
        {
            var gps=GPS();
            var fare=gps.gameObject.AddComponent<TruckTaxiRideLocation>(); fare.locationId="fare";
            gps.SetRideDestination(fare);
            Vector3 checkpoint=new Vector3(321,7,-456);
            gps.SyncRaceCheckpoint(true,checkpoint,2,12);
            long token=gps.ActiveLeaseToken;
            Assert.AreEqual(TruckTaxiNavigationLayer.RaceCheckpoint,gps.ActiveLayer);
            Assert.AreEqual("RACE CHECKPOINT 3 / 12",gps.TargetLabel);
            var objectives=new List<TruckTaxiNavigationObjective>(); gps.CopyNavigationObjectives(objectives);
            Assert.AreEqual(checkpoint,objectives[0].Position,"Already local world; never transform again.");
            Assert.IsTrue(objectives.Exists(o=>o.TargetId=="fare"));
            gps.StopNavigation();
            gps.SyncRaceCheckpoint(true,checkpoint,2,12);
            Assert.AreEqual(token,gps.ActiveLeaseToken);
            Assert.IsTrue(gps.GuidanceSuppressed,"Unchanged checkpoint must not undo manual Stop Navigation.");
            gps.SyncRaceCheckpoint(true,checkpoint+Vector3.forward*30,3,12);
            Assert.Greater(gps.ActiveLeaseToken,token);
            Assert.AreEqual("RACE CHECKPOINT 4 / 12",gps.TargetLabel);
            gps.SyncRaceCheckpoint(false,Vector3.zero,0,0);
            Assert.AreEqual("fare",gps.TargetId);
            gps.CopyNavigationObjectives(objectives);
            Assert.IsFalse(objectives.Exists(o=>o.Layer==TruckTaxiNavigationLayer.RaceCheckpoint));
        }

        [Test]
        public void EmergencyCoversRaceAndRaceCancellationDoesNotPopEmergency()
        {
            var gps=GPS();
            gps.SyncRaceCheckpoint(true,new Vector3(40,0,20),0,5);
            Assert.IsTrue(gps.TrySetNavigationIntent(TruckTaxiNavigationLayer.Emergency,"recovery","safe",
                "Recovery",Vector3.zero,TruckTaxiMapMarkerType.ActiveObjective));
            long emergencyToken=gps.ActiveLeaseToken;
            gps.SyncRaceCheckpoint(true,new Vector3(60,0,20),1,5);
            Assert.AreEqual(emergencyToken,gps.ActiveLeaseToken);
            gps.SyncRaceCheckpoint(false,Vector3.zero,0,0);
            Assert.AreEqual("safe",gps.TargetId);
            Assert.AreEqual(emergencyToken,gps.ActiveLeaseToken);
        }

        [Test]
        public void DisablingGpsReleasesRaceProjectionAndBeamRefreshHidesIt()
        {
            var gps=GPS();
            var beams=gps.gameObject.AddComponent<TruckTaxiObjectiveBeams>(); beams.Initialize(gps);
            gps.SyncRaceCheckpoint(true,Vector3.zero,0,5);
            beams.Refresh(); Assert.AreEqual(1,beams.VisibleCount,beams.Diagnostic);
            gps.enabled=false;
            InvokeDisable(gps); InvokeDisable(gps);
            beams.Refresh(); Assert.AreEqual(0,beams.VisibleCount);
            Assert.IsFalse(gps.HasNavigationTarget);
            foreach(var renderer in beams.GetComponentsInChildren<MeshRenderer>()) Assert.IsFalse(renderer.enabled);
            gps.enabled=true; beams.Refresh();
            Assert.AreEqual(0,beams.VisibleCount,"Re-enable must not resurrect the cancelled race lease.");
        }

        [TestCase(TruckTaxiMapMarkerType.Hazard,false)]
        [TestCase(TruckTaxiMapMarkerType.RoadClosure,false)]
        [TestCase(TruckTaxiMapMarkerType.WorkArea,false)]
        [TestCase(TruckTaxiMapMarkerType.PrivateEventStop,true)]
        [TestCase(TruckTaxiMapMarkerType.Destination,true)]
        public void OnlyPointObjectivesUseEdgeTreatment(TruckTaxiMapMarkerType type,bool expected) =>
            Assert.AreEqual(expected,TruckTaxiNavigationObjective.IsPointObjective(type));
        [Test] public void EdgeArrowDirectionAndPulseAreBounded()
        {
            Assert.AreEqual(0,TruckTaxiObjectivePointPresentation.DirectionAngle(Vector2.up),.001);
            Assert.AreEqual(-90,TruckTaxiObjectivePointPresentation.DirectionAngle(Vector2.right),.001);
            Assert.IsFalse(TruckTaxiObjectivePointPresentation.AtMapEdge(new Vector2(.5f,.5f),.02f,false));
            Assert.IsTrue(TruckTaxiObjectivePointPresentation.AtMapEdge(new Vector2(.98f,.5f),.02f,false));
            for(int i=0;i<100;i++) Assert.That(TruckTaxiObjectivePointPresentation.Pulse(i*.03f),Is.InRange(.999f,1.301f));
        }
    }
}
