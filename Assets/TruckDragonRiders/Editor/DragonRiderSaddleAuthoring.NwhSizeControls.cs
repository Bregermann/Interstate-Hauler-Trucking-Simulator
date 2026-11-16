#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        [Serializable]sealed class NwhDimensionControls
        {
            public string scope="ACTUAL_ORIGINAL_NWH_CLONE_AUTHORED_DIMENSION_MASS_AND_FINITE_WORLD_EDGE_CONTROLS_NOT_GAMEPLAY_OR_RELEASE_ACCEPTANCE";
            public bool ordinaryRootMotionAccepted,ordinaryFiveMillimeterCabArticulationAccepted,rootScaleChangeRejected,colliderSizeChangeRejected,colliderCenterChangeRejected,colliderScaleChangeRejected,bodyMassChangeRejected,sourceBytesExact,originalPhysicsExact;
            public float maximumPositiveWorldEdgeDrift;
        }
        public static string InspectOriginalNwhMotionAndSizeControlsWithoutSavingAssets()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped compiled native Editor required.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve unsaved scenes first.");
            var watched=Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories).ToDictionary(path=>path,Hash);var setup=EditorSceneManager.GetSceneManagerSetup();var report=new NwhDimensionControls();Scene scene=default;DragonRidersMatch match=null;string physics=null;var preview=new PreviewRenderUtility();
            try
            {
                scene=EditorSceneManager.OpenScene(DragonRidersAuthoring.ScenePath,OpenSceneMode.Additive);match=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<DragonRidersMatch>(true)).Single();physics=PhysicsSignature(match);
                var copy=UnityEngine.Object.Instantiate(match.trucks[0].gameObject);preview.AddSingleGO(copy);var truck=copy.GetComponent<LWS.TruckSports.SportsEventTruck>();foreach(var body in copy.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.detectCollisions=false;}
                var trucks=new[]{truck};string before=FixedOriginalNwhGeometry(trucks);var dimensions=CaptureFixedNwhFields(trucks);var cab=copy.GetComponentsInChildren<BoxCollider>(true).First(box=>box.transform!=truck.transform);
                bool Accepted(out float drift){bool edges=FiniteWorldBoxDimensionsAgree(dimensions,CaptureFixedNwhFields(trucks),out drift);return FixedOriginalNwhGeometry(trucks)==before&&edges;}
                truck.transform.SetPositionAndRotation(truck.transform.position+new Vector3(31,11,-7),Quaternion.Euler(17,42,-12)*truck.transform.rotation);report.ordinaryRootMotionAccepted=Accepted(out float moved);report.maximumPositiveWorldEdgeDrift=moved;
                cab.transform.localPosition+=Vector3.up*.005f;cab.transform.localRotation=Quaternion.Euler(.2f,.1f,0)*cab.transform.localRotation;report.ordinaryFiveMillimeterCabArticulationAccepted=Accepted(out moved);report.maximumPositiveWorldEdgeDrift=Mathf.Max(report.maximumPositiveWorldEdgeDrift,moved);
                var scale=truck.transform.localScale;truck.transform.localScale=scale*1.01f;report.rootScaleChangeRejected=!Accepted(out _);truck.transform.localScale=scale;
                var size=cab.size;cab.size=size+Vector3.right*.001f;report.colliderSizeChangeRejected=!Accepted(out _);cab.size=size;
                var center=cab.center;cab.center=center+Vector3.up*.001f;report.colliderCenterChangeRejected=!Accepted(out _);cab.center=center;
                var cabScale=cab.transform.localScale;cab.transform.localScale=cabScale*1.01f;report.colliderScaleChangeRejected=!Accepted(out _);cab.transform.localScale=cabScale;
                float mass=truck.body.mass;truck.body.mass=mass+1;report.bodyMassChangeRejected=!Accepted(out _);truck.body.mass=mass;
                if(!Accepted(out _))throw new InvalidOperationException("All dimension/mass negative controls must restore to the same valid moving/articulating original NWH clone.");
            }
            finally
            {
                preview.Cleanup();if(match!=null)report.originalPhysicsExact=PhysicsSignature(match)==physics;if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);report.sourceBytesExact=watched.Count==Directory.GetFiles("Assets/TruckDragonRiders","*",SearchOption.AllDirectories).Length&&watched.All(pair=>File.Exists(pair.Key)&&Hash(pair.Key)==pair.Value);
                var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-tsDragonOutput");if(at>=0&&at+1<args.Length){string folder=Path.Combine(args[at+1],"RiderSaddleAuthoring","MountedSize");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"original-nwh-dimension-and-motion-controls.json"),JsonUtility.ToJson(report,true));}
            }
            if(!report.sourceBytesExact||!report.originalPhysicsExact)throw new InvalidOperationException("Original NWH motion/dimension controls changed saved source or original physics.");return JsonUtility.ToJson(report,true);
        }
    }
}
#endif
