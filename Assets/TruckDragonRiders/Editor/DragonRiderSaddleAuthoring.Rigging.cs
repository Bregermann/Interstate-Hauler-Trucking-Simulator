#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckDragonKingdom.Editor;
using LWS.TruckSports;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        const string RiggingName="LWS actual timber wheel cradles and animated leather harness";
        const string VillageRoot="Assets/TruckDragonKingdom/PrivateLicensedEnvironment/Plugins/Tidal Flask Studios/Fantastic Village Pack/";
        const string BeamPath=VillageRoot+"prefabs/buildings_modules/P_BLD_beam_small_v01_02.prefab";
        const string LeatherPath=VillageRoot+"materials/M_leather.mat";
        const string RopePath=VillageRoot+"materials/M_rope.mat";
        static readonly Dictionary<string,Material> harnessMaterialCache=new Dictionary<string,Material>(StringComparer.Ordinal);
        public static void UpgradeSavedSaddlePresentation()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled Editor required for explicit visual harness upgrade.");
            string path=DragonRidersAuthoring.ScenePath;for(int index=0;index<SceneManager.sceneCount;index++)if(SceneManager.GetSceneAt(index).path==path||SceneManager.GetSceneAt(index).isDirty)throw new InvalidOperationException("Close the saved Riders scene and preserve unsaved changes first.");
            var sourceFiles=new[]{BeamPath,LeatherPath,RopePath}.SelectMany(source=>AssetDatabase.GetDependencies(source,true)).Where(source=>source.StartsWith("Assets/",StringComparison.Ordinal)&&File.Exists(source)).Distinct().SelectMany(source=>new[]{source,source+".meta"}).ToDictionary(source=>source,Hash);
            var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;string guid=AssetDatabase.AssetPathToGUID(path);int changed=0;
            harnessMaterialCache.Clear();
            try
            {
                scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);var match=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<DragonRidersMatch>(true)).Single();string physics=PhysicsSignature(match);
                foreach(var racer in match.racers)foreach(var actor in racer.choices)
                {
                    var frame=actor.GetComponent<DragonRiderSaddle>();if(frame==null||frame.calibrationVersion!=7)throw new InvalidOperationException("Retain the fully gated schema7 saddle before adding visual fittings; presentation cannot make a rejected mount acceptable.");
                    if(frame.rigging!=null){if(frame.rigging.presentationVersion!=1||frame.rigging.frame!=frame||frame.rigging.truck!=racer.truck||frame.rigging.wheelCradles.Length!=4||frame.rigging.legs.Length!=4||frame.rigging.girths.Length!=2||frame.rigging.upperTies.Length!=2)throw new InvalidOperationException("Preserve customized/unknown saddle rigging; no implicit visual regeneration.");continue;}
                    if(actor.transform.Find(RiggingName)!=null)throw new InvalidOperationException("Preserve existing unrecognized visual harness.");
                    InstallActualRigging(frame,racer.truck);changed++;
                }
                if(physics!=PhysicsSignature(match)||sourceFiles.Any(pair=>Hash(pair.Key)!=pair.Value))throw new InvalidOperationException("Visual rigging changed original NWH physics or vendor source.");
                if(changed>0){EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Could not retain explicit visual fittings.");}
                if(AssetDatabase.AssetPathToGUID(path)!=guid)throw new InvalidOperationException("Visual upgrade changed original scene GUID.");Debug.Log("RIDERS_VISUAL_RIGGING_UPGRADE changed="+changed+" originalNwhPhysicsExact=true originalVendorSourcesExact=true sceneGuid="+guid);
            }
            finally{if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        static void InstallActualRigging(DragonRiderSaddle frame,SportsEventTruck originalTruck)
        {
            GameObject clone=null;try
            {
                frame.CaptureEvaluatedAnimationPose();var pose=frame.ReadPose();clone=UnityEngine.Object.Instantiate(originalTruck.gameObject);clone.hideFlags=HideFlags.HideAndDontSave;var truck=clone.GetComponent<SportsEventTruck>();foreach(var body in clone.GetComponentsInChildren<Rigidbody>(true))body.isKinematic=true;
                var footprint=OriginalFootprints(new[]{originalTruck}).Single();truck.transform.SetPositionAndRotation(pose.position+pose.rotation*Vector3.up*footprint.support,pose.rotation);
                var seating=MeasureActualDeckSeating(truck,frame.supportPresentation,pose.rotation*Vector3.up,"EXPLICIT_VISUAL_FITTING_FROM_ACTUAL_RENDERED_WHEELS_NO_NWH_POSE_CHANGE");if(seating.wheels.Length!=4||seating.wheels.Any(wheel=>!wheel.actualVisualFound||!wheel.deckFaceFound||wheel.actualGap<.05f||wheel.actualGap>.8f))throw new InvalidOperationException("Actual four-wheel/timber contact geometry required; do not fit from theoretical spring travel.");
                var root=new GameObject(RiggingName);root.transform.SetParent(frame.actor.transform,false);var rigging=root.AddComponent<DragonRiderSaddleRigging>();rigging.frame=frame;rigging.truck=originalTruck;rigging.wheelCradles=new Transform[4];
                for(int index=0;index<4;index++)
                {
                    var wheel=seating.wheels[index];var cradle=ActualVendorTimber(frame.supportPresentation,"Actual wheel cradle "+index,new Vector3(.68f,wheel.actualGap,1.15f));cradle.SetPositionAndRotation((wheel.actualRenderedBottom+wheel.actualDeckFace)*.5f,pose.rotation);rigging.wheelCradles[index]=cradle;
                }
                rigging.legs=new DragonRiderSaddleRigging.Leg[4];
                for(int index=0;index<4;index++)
                {
                    var anchor=frame.surfacePatchAnchors[index+1];Vector3 actual=frame.ReadActualSkinVertexWorld(anchor.a)*anchor.barycentric.x+frame.ReadActualSkinVertexWorld(anchor.b)*anchor.barycentric.y+frame.ReadActualSkinVertexWorld(anchor.c)*anchor.barycentric.z;var local=frame.supportPresentation.InverseTransformPoint(actual);local.y=-.15f;
                    rigging.legs[index]=new DragonRiderSaddleRigging.Leg{patch=index+1,deckEnd=local,timber=ActualVendorTimber(frame.supportPresentation,"Actual skin-to-deck timber leg "+index,new Vector3(.18f,1,.18f))};
                }
                Material leather=ActualHarnessMaterial(LeatherPath),rope=ActualHarnessMaterial(RopePath);var verts=ReadVisibleModelVertices(truck.transform).Select(point=>truck.transform.InverseTransformPoint(point)).ToArray();
                rigging.girths=new DragonRiderSaddleRigging.Girth[2];rigging.upperTies=new DragonRiderSaddleRigging.Tie[2];
                for(int index=0;index<2;index++)
                {
                    float z=truck.shape.center.z+truck.shape.size.z*(index==0?-.22f:.22f);var slice=verts.Where(point=>Mathf.Abs(point.z-z)<.45f).ToArray();if(slice.Length<4)throw new InvalidOperationException("Actual original truck cross-section missing for visible cargo tie.");float left=slice.Min(point=>point.x)-.04f,right=slice.Max(point=>point.x)+.04f,top=slice.Max(point=>point.y)+.04f;
                    Vector3 leftDeck=frame.supportPresentation.InverseTransformPoint(truck.transform.TransformPoint(new Vector3(left,0,z))),rightDeck=frame.supportPresentation.InverseTransformPoint(truck.transform.TransformPoint(new Vector3(right,0,z)));leftDeck.y=rightDeck.y=-.08f;
                    rigging.upperTies[index]=new DragonRiderSaddleRigging.Tie{line=ActualHarnessLine(root.transform,"Released-on-detachment upper cargo tie "+index,rope,.10f),leftDeckEnd=leftDeck,rightDeckEnd=rightDeck,leftTruckTop=new Vector3(left,top,z),rightTruckTop=new Vector3(right,top,z)};
                    var skinVertices=ActualTorsoGirthVertices(frame,index==0?-.08f:.08f);rigging.girths[index]=new DragonRiderSaddleRigging.Girth{line=ActualHarnessLine(root.transform,"Actual animated skin leather girth "+index,leather,.18f),skinVertices=skinVertices,leftDeckEnd=leftDeck,rightDeckEnd=rightDeck};
                }
                frame.rigging=rigging;rigging.truck=truck;rigging.RefreshAuthoredPresentation(true);rigging.truck=originalTruck;if(root.GetComponentsInChildren<Collider>(true).Length!=0||rigging.wheelCradles.Any(value=>value.GetComponentsInChildren<Collider>(true).Length!=0)||rigging.legs.Any(value=>value.timber.GetComponentsInChildren<Collider>(true).Length!=0))throw new InvalidOperationException("Visual fittings must not alter collision geometry.");
                var fitted=MeasureActualDeckSeating(truck,frame.supportPresentation,pose.rotation*Vector3.up,"EXPLICIT_ACTUAL_VENDOR_TIMBER_TOP_FACE_CONTACT_PROOF_NOT_LIVE_ACCEPTANCE");if(fitted.wheels.Any(wheel=>!wheel.deckFaceFound||Mathf.Abs(wheel.actualGap)>.03f))throw new InvalidOperationException("Actual vendor timber drive faces do not meet original rendered wheels within3cm; inspect real geometry before retaining.");EditorUtility.SetDirty(frame);
            }
            finally{if(clone!=null)UnityEngine.Object.DestroyImmediate(clone);frame.ReleaseGeometrySnapshot();}
        }
        static int[] ActualTorsoGirthVertices(DragonRiderSaddle frame,float offset)
        {
            var model=frame.actor.motor.transform;var source=frame.surfaceRenderer.sharedMesh;var weights=source.boneWeights;var bones=frame.surfaceRenderer.bones;float z=model.InverseTransformPoint(frame.sourceBone.position).z+offset;
            var vertices=Enumerable.Range(0,source.vertexCount).Where(index=>TorsoWeight(weights[index],bones)>.5f).Select(index=>new{index,point=model.InverseTransformPoint(frame.ReadActualSkinVertexWorld(index))}).Where(value=>Mathf.Abs(value.point.z-z)<.18f).ToArray();if(vertices.Length<12)throw new InvalidOperationException("Actual anatomical torso cross-section required for leather girth; no arbitrary body-envelope substitute.");
            var center=new Vector2((vertices.Min(value=>value.point.x)+vertices.Max(value=>value.point.x))*.5f,(vertices.Min(value=>value.point.y)+vertices.Max(value=>value.point.y))*.5f);var result=new int[9];
            for(int index=0;index<result.Length;index++){float angle=Mathf.Lerp(.85f*Mathf.PI,2.15f*Mathf.PI,index/(float)(result.Length-1));Vector2 direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));result[index]=vertices.OrderByDescending(value=>Vector2.Dot(new Vector2(value.point.x,value.point.y)-center,direction)).First().index;}if(result.Distinct().Count()<4)throw new InvalidOperationException("Actual torso girth cross-section degenerates; inspect source anatomy before fitting.");return result;
        }
        static LineRenderer ActualHarnessLine(Transform parent,string name,Material material,float width)
        {
            var line=new GameObject(name).AddComponent<LineRenderer>();line.transform.SetParent(parent,false);line.useWorldSpace=true;line.sharedMaterial=material;line.startWidth=line.endWidth=width;line.numCornerVertices=4;line.numCapVertices=4;line.textureMode=LineTextureMode.Tile;line.alignment=LineAlignment.View;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;line.receiveShadows=true;return line;
        }
        static Material ActualHarnessMaterial(string path)
        {
            if(harnessMaterialCache.TryGetValue(path,out var retained)&&retained!=null)return retained;var source=AssetDatabase.LoadAssetAtPath<Material>(path);if(source==null)throw new InvalidOperationException("Actual owned harness material missing: "+path);string folder="Assets/TruckDragonRiders/Materials";Directory.CreateDirectory(folder);AssetDatabase.Refresh();var material=DragonKingdomWorldVisualMigration.ConfigureOwnedMaterial(source,folder+"/Harness "+AssetDatabase.AssetPathToGUID(path)+" URP.mat",false,out _);AssetDatabase.SaveAssetIfDirty(material);harnessMaterialCache[path]=material;return material;
        }
        static Transform ActualVendorTimber(Transform parent,string name,Vector3 dimensions)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(BeamPath);if(prefab==null)throw new InvalidOperationException("Actual owned Tidal timber prefab missing.");var root=new GameObject(name).transform;var scale=new GameObject("Normalized actual vendor timber mesh").transform;scale.SetParent(root,false);var beam=(GameObject)PrefabUtility.InstantiatePrefab(prefab);beam.transform.SetParent(scale,false);PrefabUtility.UnpackPrefabInstance(beam,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            foreach(var collider in beam.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);foreach(var body in beam.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(body);
            var bounds=DragonKingdomWorldAuthoring.MeasureOwnedModelBounds(beam.transform);int axis=bounds.size.x>bounds.size.y?(bounds.size.x>bounds.size.z?0:2):(bounds.size.y>bounds.size.z?1:2);beam.transform.rotation=Quaternion.FromToRotation(axis==0?Vector3.right:axis==1?Vector3.up:Vector3.forward,Vector3.up);bounds=DragonKingdomWorldAuthoring.MeasureOwnedModelBounds(beam.transform);beam.transform.position-=bounds.center;scale.localScale=new Vector3(dimensions.x/bounds.size.x,dimensions.y/bounds.size.y,dimensions.z/bounds.size.z);
            foreach(var renderer in beam.GetComponentsInChildren<Renderer>(true)){var materials=renderer.sharedMaterials;for(int index=0;index<materials.Length;index++)materials[index]=ActualHarnessMaterial(AssetDatabase.GetAssetPath(materials[index]));renderer.sharedMaterials=materials;}root.SetParent(parent,false);return root;
        }
    }
}
#endif
