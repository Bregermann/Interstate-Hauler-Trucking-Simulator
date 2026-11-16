#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.TruckSports;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonRiders.Editor
{
    public static partial class DragonRiderSaddleAuthoring
    {
        [Serializable]sealed class FiniteContactProof
        {
            public bool projectedPointInsideOriginalBox,actualSourceTriangleIntersectsOriginalBox,requiresFiniteClearance;
            public Vector3 projectedWorldPoint,worldA,worldB,worldC;
            public List<FiniteBoxProof> boxes=new List<FiniteBoxProof>();
        }
        [Serializable]sealed class FiniteBoxProof{public string path;public bool pointInside,triangleSat;public Vector3 pointLocal,a,b,c,size;public Matrix4x4 actualLocalToWorld;}
        [Serializable]sealed class FiniteContactControls{public bool passed,nativeClosestPointPassed;public string source;public FiniteContactProof separatedOutside,crossingOutside,inside,allVerticesOutsideCrossing;}
        static FiniteContactProof MeasureFiniteOriginalContact(Transform truck,BoxCollider[] boxes,Vector3 projectedPoint,Vector3 worldA,Vector3 worldB,Vector3 worldC)
        {
            if(boxes.Length==0)throw new InvalidOperationException("Actual original enabled solid NWH boxes required; no missing-shape substitution.");
            var proof=new FiniteContactProof{projectedWorldPoint=projectedPoint,worldA=worldA,worldB=worldB,worldC=worldC};
            foreach(var box in boxes)
            {
                var matrix=box.transform.localToWorldMatrix;var x=(Vector3)matrix.GetColumn(0);var y=(Vector3)matrix.GetColumn(1);var z=(Vector3)matrix.GetColumn(2);
                if(Mathf.Abs(Vector3.Dot(x.normalized,y.normalized))>.0001f||Mathf.Abs(Vector3.Dot(x.normalized,z.normalized))>.0001f||Mathf.Abs(Vector3.Dot(y.normalized,z.normalized))>.0001f)throw new InvalidOperationException("Sheared original collider requires a genuine native query, not an OBB approximation.");
                var point=box.transform.InverseTransformPoint(projectedPoint)-box.center;var a=box.transform.InverseTransformPoint(worldA)-box.center;var b=box.transform.InverseTransformPoint(worldB)-box.center;var c=box.transform.InverseTransformPoint(worldC)-box.center;var half=box.size*.5f;
                bool inside=Mathf.Abs(point.x)<=half.x&&Mathf.Abs(point.y)<=half.y&&Mathf.Abs(point.z)<=half.z;
                bool intersects=ActualTriangleBoxSat(a,b,c,half,out _);
                proof.boxes.Add(new FiniteBoxProof{path=AnimationUtility.CalculateTransformPath(box.transform,truck),pointInside=inside,triangleSat=intersects,pointLocal=point,a=a,b=b,c=c,size=box.size,actualLocalToWorld=matrix});
                proof.projectedPointInsideOriginalBox|=inside;proof.actualSourceTriangleIntersectsOriginalBox|=intersects;
            }
            // A point beyond a tilted end face is not enough to discard a
            // crossing triangle. Either actual finite contact keeps the SAME
            // negative clearance gate. The independent whole-skin SAT remains.
            proof.requiresFiniteClearance=proof.projectedPointInsideOriginalBox||proof.actualSourceTriangleIntersectsOriginalBox;return proof;
        }
        static FiniteContactControls CheckFiniteOriginalContactControls(BoxCollider box,Transform truck)
        {
            var half=box.size*.5f;Vector3 W(float x,float y,float z)=>box.transform.TransformPoint(box.center+Vector3.Scale(half,new Vector3(x,y,z)));
            var one=new[]{box};var row=new FiniteContactControls{source=AnimationUtility.CalculateTransformPath(box.transform,truck)};
            row.separatedOutside=MeasureFiniteOriginalContact(truck,one,W(0,0,1.5f),W(-.3f,0,1.1f),W(.3f,0,1.1f),W(0,.3f,1.5f));
            row.crossingOutside=MeasureFiniteOriginalContact(truck,one,W(0,0,1.5f),W(-.5f,0,.5f),W(.5f,0,.5f),W(0,.5f,1.5f));
            row.inside=MeasureFiniteOriginalContact(truck,one,W(0,0,0),W(-.2f,0,0),W(.2f,0,0),W(0,.2f,0));
            row.allVerticesOutsideCrossing=MeasureFiniteOriginalContact(truck,one,W(0,0,1.5f),W(-3,-3,0),W(3,-3,0),W(0,3,0));
            row.nativeClosestPointPassed=Vector3.Distance(box.ClosestPoint(W(0,0,0)),W(0,0,0))<.0001f&&Vector3.Distance(box.ClosestPoint(W(0,0,3)),W(0,0,3))>.01f;
            row.passed=row.nativeClosestPointPassed&&!row.separatedOutside.requiresFiniteClearance&&!row.crossingOutside.projectedPointInsideOriginalBox&&row.crossingOutside.actualSourceTriangleIntersectsOriginalBox&&row.crossingOutside.requiresFiniteClearance&&row.inside.requiresFiniteClearance&&row.allVerticesOutsideCrossing.requiresFiniteClearance;
            if(!row.passed)throw new InvalidOperationException("Finite original-box contact oracle failed actual native inside/outside, separated projection, outside-point crossing or no-inside-vertex crossing controls.");return row;
        }
        public static string InspectFiniteOriginalContactControlsWithoutSavingAssets()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stopped native Editor required.");for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve unsaved scenes first.");
            string path=DragonRidersAuthoring.ScenePath,before=Hash(path);var setup=EditorSceneManager.GetSceneManagerSetup();Scene scene=default;var preview=new PreviewRenderUtility();var rows=new List<FiniteContactControls>();
            try{scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);var match=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<DragonRidersMatch>(true)).Single();foreach(var source in match.trucks){var copy=UnityEngine.Object.Instantiate(source.gameObject);preview.AddSingleGO(copy);copy.SetActive(true);foreach(var body in copy.GetComponentsInChildren<Rigidbody>(true))body.isKinematic=true;copy.transform.rotation=Quaternion.Euler(20,15,7)*copy.transform.rotation;Physics.SyncTransforms();var shapes=copy.GetComponentsInChildren<Collider>(true).Where(value=>value.enabled&&!value.isTrigger&&value.gameObject.activeInHierarchy).ToArray();if(shapes.Any(value=>value is not BoxCollider))throw new InvalidOperationException("Actual original box membership changed.");foreach(var box in shapes.Cast<BoxCollider>())rows.Add(CheckFiniteOriginalContactControls(box,copy.transform));}}
            finally{preview.Cleanup();if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);if(Hash(path)!=before)throw new InvalidOperationException("Read-only finite contact controls changed saved scene bytes.");}
            return "{\"controls\":["+string.Join(",",rows.Select(row=>JsonUtility.ToJson(row)))+"]}";
        }
    }
}
#endif
