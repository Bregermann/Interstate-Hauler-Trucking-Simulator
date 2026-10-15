using System;
using System.Collections.Generic;
using System.Reflection;
using LWS.InterstateHauler;
using NUnit.Framework;
using NWH.VehiclePhysics2;
using NWH.VehiclePhysics2.Effects;
using UnityEngine;
using Object=UnityEngine.Object;
using LOD=UnityEngine.LOD;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiCareer")]
    public sealed class TruckTaxiVehicleFactoryTests
    {
        private readonly List<Object> objects=new List<Object>();
        private T Asset<T>() where T:ScriptableObject
        { var value=ScriptableObject.CreateInstance<T>(); objects.Add(value); return value; }
        private GameObject Node(string name,Transform parent=null)
        { var go=new GameObject(name); go.transform.SetParent(parent,false); objects.Add(go); return go; }
        private GameObject ConfiguredVehicle(string name,out VehicleController vehicle)
        {
            var isolation=Node(name+" isolation"); isolation.SetActive(false);
            var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/LWS/TruckTaxi/Prefabs/TruckTaxi_InterstateTractor.prefab");
            Assert.IsNotNull(prefab,"The configured project tractor fixture is required.");
            // Serialized StateSettings exist before OnValidate; the inactive parent prevents Awake/Start.
            var root=Object.Instantiate(prefab,isolation.transform,false); objects.Add(root);
            root.name=name; root.SetActive(false);
            vehicle=root.GetComponent<VehicleController>();
            Assert.IsNotNull(vehicle); Assert.IsNotNull(vehicle.stateSettings);
            Assert.Greater(vehicle.stateSettings.LODs.Count,0);
            Assert.IsNotNull(root.GetComponent<Rigidbody>());
            Assert.IsFalse(root.activeInHierarchy,"Fixture must never start vehicle simulation.");
            return root;
        }
        private MeshRenderer MeshNode(string name,Transform parent,Mesh mesh=null)
        {
            var go=Node(name,parent); var filter=go.AddComponent<MeshFilter>();
            if(mesh==null) { mesh=new Mesh { name=name }; objects.Add(mesh); }
            filter.sharedMesh=mesh; return go.AddComponent<MeshRenderer>();
        }
        [TearDown] public void TearDown()
        { for(int i=objects.Count-1;i>=0;i--) if(objects[i]!=null) Object.DestroyImmediate(objects[i]); objects.Clear(); }

        private TruckTaxiSession Career(out TruckTaxiCareerCatalog catalog)
        {
            catalog=Asset<TruckTaxiCareerCatalog>();
            var prefab=Node("Content reference only");
            var current=Asset<LwsTruckDefinition>(); current.ConfigureForTests("truck.factory.current",prefab);
            var next=Asset<LwsTruckDefinition>(); next.ConfigureForTests("truck.factory.next",prefab);
            catalog.trucks=new[]{new TruckTaxiCareerTruckDefinition { truck=current },
                new TruckTaxiCareerTruckDefinition { truck=next,availableForPurchase=true }};
            var session=new TruckTaxiSession(Asset<TruckTaxiConfiguration>(),Array.Empty<TruckTaxiRideLocation>(),33);
            session.StartShift(); Assert.IsTrue(session.BindCareerRun("profile.factory",out _));
            Assert.IsTrue(session.TryInitializeCareer("Driver",current.StableId,out _));
            Assert.IsTrue(session.TryPurchaseTruck(catalog,next.StableId,out _)); return session;
        }
        private static bool Commit(TruckTaxiSession session,TruckTaxiCareerCatalog catalog,Action apply,Action rollback,out string reason)
        {
            var method=typeof(TruckTaxiTruckFactory).GetMethod("CommitSelection",BindingFlags.Static|BindingFlags.NonPublic);
            Assert.NotNull(method);
            object[] args={session,catalog,"truck.factory.next",apply,rollback,null};
            bool result=(bool)method.Invoke(null,args); reason=(string)args[5]; return result;
        }

        [Test] public void PreparationFailureRestoresActorAndLeavesCareerUntouched()
        {
            var session=Career(out var catalog); string actor=session.EquippedTruckId; int rollbacks=0;
            Assert.IsFalse(Commit(session,catalog,()=>{actor="partial";throw new InvalidOperationException("prepare");},
                ()=>{rollbacks++;actor="truck.factory.current";},out string reason));
            Assert.AreEqual(1,rollbacks); Assert.AreEqual(session.EquippedTruckId,actor);
            StringAssert.Contains("prepare",reason);
        }
        [Test] public void CareerRejectionRestoresPreparedActor()
        {
            var session=Career(out var catalog); string actor=session.EquippedTruckId; int rollbacks=0;
            catalog.trucks[1].truck=null;
            Assert.IsFalse(Commit(session,catalog,()=>actor="truck.factory.next",
                ()=>{rollbacks++;actor="truck.factory.current";},out _));
            Assert.AreEqual(1,rollbacks); Assert.AreEqual(session.EquippedTruckId,actor);
        }
        [Test] public void ThrowingCareerObserverDoesNotRollBackAlreadyCommittedActor()
        {
            var session=Career(out var catalog); string actor=session.EquippedTruckId; int rollbacks=0;
            session.CareerEquipmentChanged+=()=>throw new InvalidOperationException("observer");
            Assert.IsTrue(Commit(session,catalog,()=>actor="truck.factory.next",()=>rollbacks++,out string warning));
            Assert.AreEqual("truck.factory.next",session.EquippedTruckId);
            Assert.AreEqual(session.EquippedTruckId,actor); Assert.AreEqual(0,rollbacks);
            StringAssert.Contains("committed",warning);
        }
        [Test] public void RollbackFailureReportsBothErrorsWithoutEscaping()
        {
            var session=Career(out var catalog);
            Assert.IsFalse(Commit(session,catalog,()=>throw new InvalidOperationException("prepare"),
                ()=>throw new InvalidOperationException("restore"),out string reason));
            StringAssert.Contains("prepare",reason); StringAssert.Contains("restore",reason);
            Assert.AreEqual("truck.factory.current",session.EquippedTruckId);
        }

        private GameObject Preview(GameObject prefab,Material[] paint=null,TruckTaxiGaragePaintData color=null)
        {
            var method=typeof(TruckTaxiTruckFactory).GetMethod("CopyPreview",BindingFlags.Static|BindingFlags.NonPublic);
            Assert.NotNull(method);
            var preview=(GameObject)method.Invoke(null,new object[]{prefab,null,paint,color}); objects.Add(preview); return preview;
        }
        [Test] public void PreviewUsesLocalActivationEvenUnderInactivePrefabContainer()
        {
            var container=Node("Inactive asset isolation"); container.SetActive(false);
            var prefab=Node("Prefab",container.transform); MeshNode("Visible",prefab.transform);
            var hidden=Node("Authored hidden group",prefab.transform); hidden.SetActive(false);
            MeshNode("Hidden",hidden.transform);
            var disabled=MeshNode("Disabled renderer",prefab.transform); disabled.enabled=false;
            Assert.IsFalse(prefab.activeInHierarchy); Assert.IsTrue(prefab.activeSelf);
            var preview=Preview(prefab);
            Assert.AreEqual(1,preview.GetComponentsInChildren<MeshRenderer>(true).Length);
            Assert.AreEqual("Visible",preview.GetComponentInChildren<MeshRenderer>().name);
        }
        [Test] public void PreviewUsesLodMembershipNotRendererNameAndCopiesNoBehaviours()
        {
            var prefab=Node("LOD source");
            var first=MeshNode("Cab__Chrome",prefab.transform);
            var second=MeshNode("Cab__Chrome",prefab.transform);
            var third=MeshNode("Cab__Chrome",prefab.transform);
            var lod=prefab.AddComponent<LODGroup>();
            lod.SetLODs(new[]{new LOD(.5f,new Renderer[]{first}),new LOD(.2f,new Renderer[]{second}),new LOD(.01f,new Renderer[]{third})});
            prefab.AddComponent<Rigidbody>(); prefab.AddComponent<AudioSource>();
            var camera=Node("Camera",prefab.transform); camera.AddComponent<Camera>();
            var preview=Preview(prefab);
            Assert.AreEqual(1,preview.GetComponentsInChildren<MeshFilter>(true).Length);
            Assert.AreSame(first.GetComponent<MeshFilter>().sharedMesh,preview.GetComponentInChildren<MeshFilter>().sharedMesh);
            Assert.IsNull(preview.GetComponentInChildren<Rigidbody>(true));
            Assert.IsNull(preview.GetComponentInChildren<AudioSource>(true)); Assert.IsNull(preview.GetComponentInChildren<Camera>(true));
            Assert.IsNull(preview.GetComponentInChildren<LODGroup>(true));
        }
        [Test] public void PreviewDoesNotIncludePrefabRootScaleTwice()
        {
            var prefab=Node("Scaled prefab"); prefab.transform.localScale=Vector3.one*2;
            var mesh=MeshNode("Body",prefab.transform); mesh.transform.localPosition=new Vector3(1,2,3); mesh.transform.localScale=Vector3.one*.8f;
            var copy=Preview(prefab).GetComponentInChildren<MeshRenderer>();
            Assert.That(Vector3.Distance(mesh.transform.localPosition,copy.transform.localPosition),Is.LessThan(.0001f));
            Assert.That(Vector3.Distance(mesh.transform.localScale,copy.transform.localScale),Is.LessThan(.0001f));
        }
        [Test] public void PreviewPaintUsesExactDeclaredMaterialAndDoesNotMutateSharedAsset()
        {
            var shader=Shader.Find("Universal Render Pipeline/Lit"); if(shader==null) shader=Shader.Find("Standard");
            Assert.NotNull(shader); var paint=new Material(shader); var glass=new Material(shader); objects.Add(paint); objects.Add(glass);
            string property=paint.HasProperty("_BaseColor") ? "_BaseColor" : "_Color"; paint.SetColor(property,Color.white);
            var prefab=Node("Paint source"); var renderer=MeshNode("Body",prefab.transform);
            renderer.sharedMaterials=new[]{paint,glass};
            var copy=Preview(prefab,new[]{paint},new TruckTaxiGaragePaintData { color=Color.red }).GetComponentInChildren<MeshRenderer>();
            var block=new MaterialPropertyBlock(); copy.GetPropertyBlock(block,0);
            Assert.AreEqual(Color.red,block.GetColor(property)); block.Clear(); copy.GetPropertyBlock(block,1);
            Assert.IsFalse(block.HasColor(Shader.PropertyToID(property)));
            Assert.AreEqual(Color.white,paint.GetColor(property)); Assert.AreSame(paint,copy.sharedMaterials[0]);
        }

        [Test] public void RemoveAuthoredRemovesOnlyGeneratedLampBindingsAcrossRepeats()
        {
            var root=ConfiguredVehicle("Inactive NWH rig",out var vehicle);
            var original=MeshNode("Original light",root.transform);
            var originalSource=new LightSource { meshRenderer=original,type=LightSource.LightType.Mesh };
            var group=vehicle.effectsManager.lightsManager.lowBeamLights; group.lightSources.Add(originalSource);
            var baselineSources=new List<LightSource>(group.lightSources);
            int baselineRenderers=root.GetComponentsInChildren<MeshRenderer>(true).Length;
            for(int repeat=0;repeat<3;repeat++)
            {
                var generated=MeshNode(TruckTaxiTruckRig.Prefix+"LampHeadlamp",root.transform);
                group.lightSources.Add(new LightSource { meshRenderer=generated,type=LightSource.LightType.Mesh });
                TruckTaxiTruckRig.RemoveAuthored(root.transform);
                Assert.AreEqual(baselineSources.Count,group.lightSources.Count);
                CollectionAssert.AreEqual(baselineSources,group.lightSources);
                Assert.Contains(originalSource,group.lightSources);
                Assert.AreEqual(baselineRenderers,root.GetComponentsInChildren<MeshRenderer>(true).Length);
            }
        }
        [Test] public void SnapshotRestoresOnlyDeclaredSourceRenderersAndPreservesBaselineLods()
        {
            var root=ConfiguredVehicle("Canonical rig",out _);
            var player=root.GetComponent<LwsPlayerTruck>();
            if(player==null) player=root.AddComponent<LwsPlayerTruck>();
            if(root.GetComponent<TruckTaxiCabOccupantRig>()==null) root.AddComponent<TruckTaxiCabOccupantRig>();
            var handling=root.GetComponent<TruckTaxiVehicleHandlingOverride>();
            if(handling==null) handling=root.AddComponent<TruckTaxiVehicleHandlingOverride>();
            var original=MeshNode("Original body",root.transform); var unrelated=MeshNode("GPS or driver surface",root.transform);
            var lod=root.GetComponent<LODGroup>(); if(lod==null) lod=root.AddComponent<LODGroup>();
            lod.SetLODs(new[]{new LOD(.2f,new Renderer[]{original})}); lod.enabled=true;
            var host=Node("Inactive host"); host.SetActive(false); var bootstrap=host.AddComponent<TruckTaxiBootstrap>();
            typeof(TruckTaxiBootstrap).GetProperty("Player").SetValue(bootstrap,player);
            typeof(TruckTaxiBootstrap).GetProperty("Handling").SetValue(bootstrap,handling);
            var type=typeof(TruckTaxiTruckFactory).GetNestedType("RigSnapshot",BindingFlags.NonPublic);
            var snapshot=Activator.CreateInstance(type,new object[]{bootstrap});
            var model=Node("Model metadata").AddComponent<TruckTaxiTruckModel>();
            model.replacedSourceMeshes=new[]{original.GetComponent<MeshFilter>().sharedMesh};
            type.GetMethod("TrackReplacedRenderers").Invoke(snapshot,new object[]{root.transform,model});
            original.enabled=false; unrelated.enabled=false; lod.SetLODs(Array.Empty<LOD>()); lod.enabled=false;
            type.GetMethod("Restore").Invoke(snapshot,new object[]{bootstrap});
            Assert.IsTrue(original.enabled); Assert.IsFalse(unrelated.enabled);
            Assert.IsTrue(lod.enabled); Assert.AreSame(original,lod.GetLODs()[0].renderers[0]);
        }
        [Test] public void UninitializedFactoryFailsClosedWithoutThrowing()
        {
            var factory=Node("Uninitialized factory").AddComponent<TruckTaxiTruckFactory>();
            Assert.IsFalse(factory.TryEquip("truck.factory.next",out string reason)); Assert.IsNotEmpty(reason);
            Assert.IsFalse(factory.RestoreSelected(out reason)); Assert.IsNotEmpty(reason);
        }
        [Test] public void AuthoredCameraMountRemovalPreservesNativeCameraAndCachedLocalPose()
        {
            var root=Node("Tractor");var cab=Node("Cab",root.transform);
            var mount=Node(TruckTaxiTruckRig.Prefix+"CockpitMount",cab.transform);mount.transform.localPosition=Vector3.one;
            var camera=Node("Vehicle Camera Driver",mount.transform).AddComponent<Camera>();
            var local=new Vector3(-.5f,1.2f,.6f);camera.transform.localPosition=local;
            TruckTaxiTruckRig.RemoveAuthored(root.transform);
            Assert.IsNotNull(camera);Assert.AreSame(cab.transform,camera.transform.parent);
            Assert.AreEqual(local,camera.transform.localPosition);
            TruckTaxiTruckRig.RemoveAuthored(root.transform);
            Assert.AreEqual(1,root.GetComponentsInChildren<Camera>(true).Length);
        }
        [Test] public void DriverSeatDoesNotMultiplyAlreadySeatedScaleOnRepeatedPlacement()
        {
            var root=Node("Cab rig"); var rig=root.AddComponent<TruckTaxiCabOccupantRig>();
            var driver=Node("Driver",root.transform); Vector3 worldScale=Vector3.one*.88f;
            var method=typeof(TruckTaxiTruckFactory).GetMethod("Seat",BindingFlags.Static|BindingFlags.NonPublic);
            var anchor=(TruckTaxiCabOccupantRig.Anchor)method.Invoke(null,new object[]{TruckTaxiCabAnchorId.DriverSeat,
                "driver",Vector3.zero,TruckTaxiCabOccupantKind.Driver,false});
            for(int repeat=0;repeat<4;repeat++)
            {
                rig.Configure(false,new[]{anchor});
                Assert.IsTrue(rig.TrySeat(driver.transform,TruckTaxiCabOccupantKind.Driver,TruckTaxiCabAnchorId.DriverSeat,
                    TruckTaxiSeatType.StandardPassenger,worldScale));
                Assert.That(Vector3.Distance(worldScale,driver.transform.lossyScale),Is.LessThan(.0001f));
            }
        }
        [Test] public void AuthoredLampResourceReleaseDestroysOnlyOwnedMaterialsAndIsIdempotent()
        {
            var shader=Shader.Find("Universal Render Pipeline/Lit"); if(shader==null) shader=Shader.Find("Standard");
            Assert.NotNull(shader);
            var shared=new Material(shader); var owned=new Material(shared); objects.Add(shared); objects.Add(owned);
            var go=Node("Lamp resources");
            var type=typeof(TruckTaxiTruckRig).Assembly.GetType("LWS.TruckTaxi.TruckTaxiRigLampMaterials");
            Assert.NotNull(type); var resources=go.AddComponent(type);
            type.GetField("owned",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(resources,new[]{owned});
            type.GetMethod("Release").Invoke(resources,null);
            Assert.IsTrue(owned==null); Assert.IsTrue(shared!=null);
            Assert.DoesNotThrow(()=>type.GetMethod("Release").Invoke(resources,null));
        }

        private TruckTaxiSession MigrationCareer(string id)
        {
            var session=new TruckTaxiSession(Asset<TruckTaxiConfiguration>(),Array.Empty<TruckTaxiRideLocation>(),33);
            session.StartShift(); Assert.IsTrue(session.BindCareerRun("profile.factory.migration",out _));
            Assert.IsTrue(session.TryInitializeCareer("Legacy Driver",id,out _));
            session.RecordRaceResult(3,0,1700); return session;
        }
        [TestCase(false)]
        [TestCase(true)]
        public void KnownStarterMigrationPreservesOwnershipPaintUpgradesWalletAndOtherSelection(bool otherEquipped)
        {
            string legacy=TruckTaxiEightOccupantLayout.StarterDefinitionId;
            string canonical=TruckTaxiEightOccupantLayout.FallbackDefinitionId;
            var session=MigrationCareer(legacy);
            var state=session.CapturePersistentState(); var old=state.ownedProfile.trucks[0];
            old.hasCustomPaint=true; old.paint=new TruckTaxiGaragePaintData { color=Color.red };
            old.ownedUpgradeIds.Add("upgrade.legacy.engine");
            old.upgrades.Add(new TruckTaxiCareerSelection { slotId="engine",itemId="upgrade.legacy.engine" });
            old.ownedCosmeticIds.Add("cosmetic.legacy.wheels");
            old.cosmetics.Add(new TruckTaxiCareerSelection { slotId="wheels",itemId="cosmetic.legacy.wheels" });
            state.ownedProfile.trucks.Add(new TruckTaxiOwnedTruck { truckId="truck.factory.next" });
            if(otherEquipped) state.ownedProfile.equippedTruckId="truck.factory.next";
            Assert.IsTrue(session.TryRestoreCareerRun("profile.factory.migration",state,out string reason),reason);
            var expected=session.CaptureCareerProfile(); expected.trucks[0].truckId=canonical;
            if(expected.equippedTruckId==legacy) expected.equippedTruckId=canonical;
            long wallet=session.WalletBalanceCents,charges=session.PurchaseChargesCents;
            string statistics=JsonUtility.ToJson(session.CapturePersistentState().statistics);
            int published=0; session.CareerEquipmentChanged+=()=>published++;
            Assert.IsTrue(session.TryMigrateLegacyStarterIdentity(out reason),reason);
            Assert.AreEqual(JsonUtility.ToJson(expected),JsonUtility.ToJson(session.CaptureCareerProfile()));
            Assert.AreEqual(wallet,session.WalletBalanceCents); Assert.AreEqual(charges,session.PurchaseChargesCents);
            Assert.AreEqual(statistics,JsonUtility.ToJson(session.CapturePersistentState().statistics)); Assert.AreEqual(1,published);
            Assert.IsTrue(session.TryMigrateLegacyStarterIdentity(out reason),reason);
            Assert.AreEqual(1,published,"Repeated migration is a no-op, not another transaction.");
        }
        [Test] public void DuplicateHistoricalStartersFailWithoutDroppingOwnedData()
        {
            var session=MigrationCareer(TruckTaxiEightOccupantLayout.StarterDefinitionId);
            var state=session.CapturePersistentState();
            state.ownedProfile.trucks.Add(new TruckTaxiOwnedTruck { truckId=TruckTaxiEightOccupantLayout.FallbackDefinitionId });
            Assert.IsTrue(session.TryRestoreCareerRun("profile.factory.migration",state,out string reason),reason);
            string before=JsonUtility.ToJson(session.CapturePersistentState());
            int published=0; session.CareerEquipmentChanged+=()=>published++;
            Assert.IsFalse(session.TryMigrateLegacyStarterIdentity(out reason)); Assert.IsNotEmpty(reason);
            Assert.AreEqual(before,JsonUtility.ToJson(session.CapturePersistentState())); Assert.AreEqual(0,published);
        }
        [Test] public void SimilarLookingButUnknownStarterIdIsNotMigrated()
        {
            var session=MigrationCareer(TruckTaxiEightOccupantLayout.StarterDefinitionId+".custom");
            string before=JsonUtility.ToJson(session.CapturePersistentState());
            int published=0; session.CareerEquipmentChanged+=()=>published++;
            Assert.IsTrue(session.TryMigrateLegacyStarterIdentity(out string reason),reason);
            Assert.AreEqual(before,JsonUtility.ToJson(session.CapturePersistentState())); Assert.AreEqual(0,published);
        }
    }
}
